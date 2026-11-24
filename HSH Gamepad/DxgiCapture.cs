// HSH Gamepad | Telegram: @HSHGamepad
using System;
using System.Drawing;
using System.Drawing.Imaging;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using Device = SharpDX.Direct3D11.Device;
using MapFlags = SharpDX.Direct3D11.MapFlags;

namespace GamepadServer;

/// <summary>
/// ضبط صفحه با Desktop Duplication (DXGI): ویندوز خودش فریم نهایی دسکتاپ رو از کارت گرافیک میده و میگه کی عوض شده.
/// چندین برابر سریع‌تر از BitBlt/GDI هست و تمام‌صفحه‌ی بازی‌ها رو هم میگیره. ویندوز 8 به بالا؛ تو ویندوز 7 (یا اگه نشد) TryCreate
/// null برمیگردونه و ScreenCast خودش با GDI ادامه میده.
/// </summary>
internal sealed class DxgiCapture : IDisposable
{
    private Device? _dev;
    private OutputDuplication? _dup;
    private Texture2D? _stage;
    private bool _gotFirst;

    public int Width { get; private set; }
    public int Height { get; private set; }

    /// <summary>deviceName مثل \\.\DISPLAY1 (همون Screen.DeviceName)</summary>
    public static DxgiCapture? TryCreate(string deviceName)
    {
        DxgiCapture? c = null;
        try
        {
            c = new DxgiCapture();
            if (c.Init(deviceName)) return c;
        }
        catch { }
        try { c?.Dispose(); } catch { }
        return null;
    }

    private bool Init(string deviceName)
    {
        using (var factory = new Factory1())
        {
            int na = factory.GetAdapterCount1();
            for (int ai = 0; ai < na; ai++)
            {
                using (var adapter = factory.GetAdapter1(ai))
                {
                    int no = adapter.GetOutputCount();
                    for (int oi = 0; oi < no; oi++)
                    {
                        using (var output = adapter.GetOutput(oi))
                        {
                            var d = output.Description;
                            if (!string.Equals(d.DeviceName, deviceName, StringComparison.OrdinalIgnoreCase)) continue;

                            using (var output1 = output.QueryInterface<Output1>())
                            {
                                var dev = new Device(adapter, DeviceCreationFlags.BgraSupport);
                                try { _dup = output1.DuplicateOutput(dev); }
                                catch { dev.Dispose(); throw; }
                                _dev = dev;
                            }

                            Width = d.DesktopBounds.Right - d.DesktopBounds.Left;
                            Height = d.DesktopBounds.Bottom - d.DesktopBounds.Top;
                            if (Width < 2 || Height < 2) return false;

                            _stage = new Texture2D(_dev, new Texture2DDescription
                            {
                                CpuAccessFlags = CpuAccessFlags.Read,
                                BindFlags = BindFlags.None,
                                Format = Format.B8G8R8A8_UNorm,
                                Width = Width,
                                Height = Height,
                                OptionFlags = ResourceOptionFlags.None,
                                MipLevels = 1,
                                ArraySize = 1,
                                SampleDescription = new SampleDescription(1, 0),
                                Usage = ResourceUsage.Staging
                            });
                            return true;
                        }
                    }
                }
            }
        }
        return false;
    }

    /// <summary>
    /// منتظر فریم جدید میمونه (حداکثر timeoutMs) و اگه اومد تو dst (Format32bppRgb هم‌اندازه‌ی Width×Height) میریزه.
    /// 1 = فریم جدید تو dst ریخته شد ؛ 0 = تغییری نبود ؛ -1 = دوپلیکیت از دست رفت (تغییر رزولوشن، UAC، قفل...) و باید دوباره ساخته بشه
    /// </summary>
    public int Grab(Bitmap dst, int timeoutMs)
    {
        var dup = _dup; var dev = _dev; var stage = _stage;
        if (dup == null || dev == null || stage == null) return -1;

        SharpDX.DXGI.Resource? res = null;
        bool acquired = false;
        try
        {
            OutputDuplicateFrameInformation info;
            try
            {
                dup.AcquireNextFrame(timeoutMs, out info, out res);
                acquired = true;
            }
            catch (SharpDXException ex)
            {
                if (ex.ResultCode.Code == SharpDX.DXGI.ResultCode.WaitTimeout.Result.Code) return 0;
                return -1;
            }

            // LastPresentTime == 0 یعنی فقط نشانگر ماوس عوض شده (خود تصویر نه)؛ ولی فریم اول رو همیشه میگیریم
            if (_gotFirst && info.LastPresentTime == 0) return 0;

            var ctx = dev.ImmediateContext;
            using (var tex = res!.QueryInterface<Texture2D>())
                ctx.CopyResource(tex, stage);
            dup.ReleaseFrame();
            acquired = false;

            var box = ctx.MapSubresource(stage, 0, MapMode.Read, MapFlags.None);
            try
            {
                int w = Math.Min(dst.Width, Width), h = Math.Min(dst.Height, Height);
                var bd = dst.LockBits(new System.Drawing.Rectangle(0, 0, dst.Width, dst.Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppRgb);
                try
                {
                    for (int y = 0; y < h; y++)
                        Utilities.CopyMemory(IntPtr.Add(bd.Scan0, y * bd.Stride), IntPtr.Add(box.DataPointer, y * box.RowPitch), w * 4);
                }
                finally { dst.UnlockBits(bd); }
            }
            finally { ctx.UnmapSubresource(stage, 0); }

            _gotFirst = true;
            return 1;
        }
        catch { return -1; }
        finally
        {
            try { res?.Dispose(); } catch { }
            if (acquired) { try { dup.ReleaseFrame(); } catch { } }
        }
    }

    public void Dispose()
    {
        try { _stage?.Dispose(); } catch { }
        try { _dup?.Dispose(); } catch { }
        try { _dev?.Dispose(); } catch { }
        _stage = null; _dup = null; _dev = null;
    }
}
