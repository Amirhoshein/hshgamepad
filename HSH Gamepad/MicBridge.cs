// HSH Gamepad | Telegram: @HSHGamepad
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

namespace GamepadServer;

/// <summary>
/// میکروفون گوشی => میکروفون مجازی ویندوز (فقط پلیر ۱).
/// گوشی صدا رو به‌صورت PCM16 مونو 16kHz تو متن base64 میفرسته:  mic 1 | mic 0 | mic d &lt;base64&gt;
/// کامپیوتر:
///   ۱) وقتی روشن شد، دستگاه میکروفونِ درایور صوتی مجازی (پوشه‌ی vad کنار برنامه، نصب با pnputil) رو «وصل» میکنه
///      (= Enable تو لیست Recording ویندوز) و وقتی خاموش شد «در میاره» (= Disable)؛
///   ۲) صدای گوشی رو با WASAPI تو خروجیِ همون درایور مجازی میریزه تا از طرف میکروفونش در بیاد.
/// تنظیم «همیشه وصل» (MicAlways): دستگاه هیچ وقت Disable نمیشه، پس بعد از بستن برنامه و ریستارت هم تو ویندوز میمونه.
/// اسم دستگاه‌ها (قسمتی از اسم، چند مورد با | جدا میشن) تو settings قابل تغییره: MicIn / MicOut
/// </summary>
internal static class MicBridge
{
    public const int SrcRate = 16000;

    private static readonly object Sync = new object();
    private static Conn? _owner;
    private static Renderer? _r;
    private static bool _always;
    private static string _inName = "Virtual Mic|Virtual Audio Driver|CABLE Output";
    private static string _outName = "Virtual Audio Driver|Virtual Mic|CABLE Input";

    public static void Configure(AppSettings s)
    {
        lock (Sync)
        {
            _always = s.MicAlways;
            if (!string.IsNullOrWhiteSpace(s.MicIn)) _inName = s.MicIn;
            if (!string.IsNullOrWhiteSpace(s.MicOut)) _outName = s.MicOut;
        }
        Reconcile();
    }

    public static void SetAlways(bool on)
    {
        lock (Sync) _always = on;
        Reconcile();
    }

    /// <summary>تطبیق وضعیت دستگاه با تنظیمات (شروع برنامه / تغییر تیک): همیشه وصل => Enable ، وگرنه تا وقتی کسی استفاده نمیکنه Disable</summary>
    public static void Reconcile()
    {
        try
        {
            lock (Sync)
            {
                var ep = Find(1, _inName);
                if (ep == null || IsCable(ep)) return;      // VB-Cable رو هیچ وقت Disable نمیکنیم (ممکنه برنامه‌های دیگه ازش استفاده کنن)
                bool want = _always || (_owner != null && _owner.IsOpen);
                if (want != (ep.State == 1)) SetVisible(ep.Id, want);
            }
        }
        catch { }
    }

    /// <summary>درایور نصبه؟ (دستگاه میکروفونش تو ویندوز هست)</summary>
    public static bool DriverPresent()
    {
        try { return Find(1, _inName) != null; } catch { return false; }
    }

    /// <summary>برمیگردونه: "" = موفق ، "nodriver" ، "busy" (گوشی دیگه‌ای داره استفاده میکنه) ، "fail"</summary>
    public static string Start(Conn c)
    {
        try
        {
            lock (Sync)
            {
                if (_owner != null && !ReferenceEquals(_owner, c) && _owner.IsOpen) return "busy";
                var mic = Find(1, _inName);
                if (mic == null) { Dump(); return "nodriver"; }
                if (mic.State != 1 && !SetVisible(mic.Id, true)) return "fail";

                if (_r == null)
                {
                    var spk = Find(0, _outName);
                    if (spk == null || spk.State != 1) return "fail";
                    _r = new Renderer(spk.Id);
                }
                _owner = c;
                return "";
            }
        }
        catch
        {
            lock (Sync) { _r?.Dispose(); _r = null; }
            return "fail";
        }
    }

    /// <summary>خاموش شدن (c = null یعنی بی‌قیدوشرط، مثل خروج از برنامه)</summary>
    public static void Stop(Conn? c)
    {
        try
        {
            lock (Sync)
            {
                if (c != null && !ReferenceEquals(_owner, c)) return;
                _r?.Dispose(); _r = null;
                _owner = null;
                if (!_always)
                {
                    var ep = Find(1, _inName);
                    if (ep != null && ep.State == 1 && !IsCable(ep)) SetVisible(ep.Id, false);
                }
            }
        }
        catch { }
    }

    /// <summary>خروج از برنامه: اگه «همیشه وصل» خاموشه دستگاه در میاد</summary>
    public static void Shutdown() { Stop(null); }

    public static void Push(Conn c, string b64)
    {
        Renderer? r;
        lock (Sync) r = ReferenceEquals(_owner, c) ? _r : null;
        if (r == null || b64.Length == 0 || b64.Length > 16384) return;
        try
        {
            byte[] raw = Convert.FromBase64String(b64);
            int n = raw.Length / 2;
            if (n == 0) return;
            var s = new short[n];
            Buffer.BlockCopy(raw, 0, s, 0, n * 2);
            r.Feed(s, n);
        }
        catch { }
    }

    // ---------------- پیدا کردن دستگاه‌ها ----------------

    private static bool IsCable(Endpoint ep) { return ep.Name.IndexOf("CABLE", StringComparison.OrdinalIgnoreCase) >= 0; }

    private sealed class Endpoint { public string Id = ""; public string Name = ""; public int State; }

    /// <summary>dataFlow: 0 = خروجی (Render) ، 1 = ورودی (Capture)؛ همه‌ی حالت‌ها (Disable هم) دیده میشن. فعال‌ها اولویت دارن.</summary>
    private static Endpoint? Find(int dataFlow, string names)
    {
        var keys = names.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
        Endpoint? best = null;
        IMMDeviceEnumerator? en = null; IMMDeviceCollection? col = null;
        try
        {
            en = (IMMDeviceEnumerator)new MMDeviceEnumeratorCom();
            if (en.EnumAudioEndpoints(dataFlow, 0xF, out col) != 0 || col == null) return null;
            col.GetCount(out uint n);
            for (uint i = 0; i < n; i++)
            {
                IMMDevice? d = null;
                try
                {
                    if (col.Item(i, out d) != 0 || d == null) continue;
                    d.GetId(out string id);
                    d.GetState(out int st);
                    string nm = FriendlyName(d);
                    bool hit = false;
                    foreach (var k in keys)
                        if (nm.IndexOf(k.Trim(), StringComparison.OrdinalIgnoreCase) >= 0) { hit = true; break; }
                    if (!hit) continue;
                    if (st == 4) continue;      // NOTPRESENT: درایور حذف شده
                    var ep = new Endpoint { Id = id, Name = nm, State = st };
                    if (best == null || (best.State != 1 && st == 1)) best = ep;
                }
                catch { }
                finally { Rel(d); }
            }
        }
        catch { }
        finally { Rel(col); Rel(en); }
        return best;
    }

    /// <summary>عیب‌یابی: لیست همه‌ی دستگاه‌های صوتیِ دیده‌شده توسط ویندوز رو تو data\\mic-debug.txt مینویسه (وقتی میکروفون مجازی پیدا نشد)</summary>
    private static void Dump()
    {
        try
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Time: " + DateTime.Now);
            sb.AppendLine("Looking for MIC  : " + _inName);
            sb.AppendLine("Looking for SPEAK: " + _outName);
            for (int flow = 1; flow >= 0; flow--)
            {
                sb.AppendLine(flow == 1 ? "--- Capture (Recording) ---" : "--- Render (Playback) ---");
                IMMDeviceEnumerator? en = null; IMMDeviceCollection? col = null;
                try
                {
                    en = (IMMDeviceEnumerator)new MMDeviceEnumeratorCom();
                    if (en.EnumAudioEndpoints(flow, 0xF, out col) != 0 || col == null) continue;
                    col.GetCount(out uint n);
                    for (uint i = 0; i < n; i++)
                    {
                        IMMDevice? d = null;
                        try
                        {
                            if (col.Item(i, out d) != 0 || d == null) continue;
                            d.GetState(out int st);
                            sb.AppendLine("state=" + st + "  " + FriendlyName(d));
                        }
                        catch { }
                        finally { Rel(d); }
                    }
                }
                catch { }
                finally { Rel(col); Rel(en); }
            }
            System.IO.File.WriteAllText(System.IO.Path.Combine(AppPaths.DataDir, "mic-debug.txt"), sb.ToString());
        }
        catch { }
    }

    private static string FriendlyName(IMMDevice d)
    {
        IPropertyStore? ps = null;
        try
        {
            if (d.OpenPropertyStore(0, out ps) != 0 || ps == null) return "";
            var key = new PropertyKey { fmtid = new Guid("a45c254e-df1c-4efd-8020-67d146a850e0"), pid = 14 };
            if (ps.GetValue(ref key, out PropVariant pv) != 0) return "";
            try { return pv.vt == 31 && pv.ptr != IntPtr.Zero ? (Marshal.PtrToStringUni(pv.ptr) ?? "") : ""; }
            finally { PropVariantClear(ref pv); }
        }
        catch { return ""; }
        finally { Rel(ps); }
    }

    /// <summary>Enable / Disable دستگاه صوتی (همون کاری که تو Sound control panel میشه)؛ نیاز به دسترسی ادمین داره</summary>
    private static bool SetVisible(string id, bool visible)
    {
        object? o = null;
        try
        {
            o = new PolicyConfigClient();
            var pc = (IPolicyConfig)o;
            return pc.SetEndpointVisibility(id, visible ? 1 : 0) >= 0;
        }
        catch { return false; }
        finally { Rel(o); }
    }

    private static void Rel(object? o)
    {
        try { if (o != null && Marshal.IsComObject(o)) Marshal.ReleaseComObject(o); } catch { }
    }

    private static void Chk(int hr)
    {
        if (hr < 0) Marshal.ThrowExceptionForHR(hr);
    }

    // ---------------- پخش صدا تو خروجیِ درایور مجازی (WASAPI shared) ----------------

    private sealed class Renderer : IDisposable
    {
        private IMMDeviceEnumerator? _en;
        private IMMDevice? _dev;
        private IAudioClient? _cl;
        private IAudioRenderClient? _rc;
        private IntPtr _fmt;
        private int _rate, _ch;
        private bool _float;
        private uint _bufFrames;
        private Thread? _th;
        private volatile bool _run;

        // صف نمونه‌های ورودی (۲ ثانیه)؛ اگه از ۱۵۰ms بیشتر جمع بشه قدیمی‌ها ریخته میشن تا تاخیر زیاد نشه
        private readonly object _q = new object();
        private readonly short[] _ring = new short[SrcRate * 2];
        private int _rd, _cnt;
        private double _pos;
        private readonly float[] _one = new float[1];

        public Renderer(string id)
        {
            try
            {
                _en = (IMMDeviceEnumerator)new MMDeviceEnumeratorCom();
                Chk(_en.GetDevice(id, out _dev));
                Guid iid = new Guid("1CB9AD4C-DBFA-4C32-B178-C2F568A703B2");
                Chk(_dev!.Activate(ref iid, 23, IntPtr.Zero, out object o));
                _cl = (IAudioClient)o;
                Chk(_cl.GetMixFormat(out _fmt));
                int tag = (ushort)Marshal.ReadInt16(_fmt, 0);
                _ch = Marshal.ReadInt16(_fmt, 2);
                _rate = Marshal.ReadInt32(_fmt, 4);
                int bits = Marshal.ReadInt16(_fmt, 14);
                _float = tag == 3;
                if (tag == 0xFFFE && Marshal.ReadInt16(_fmt, 16) >= 22)
                    _float = Marshal.ReadInt32(_fmt, 24) == 3;
                if (_rate < 8000 || _ch < 1 || !(( _float && bits == 32) || (!_float && bits == 16)))
                    throw new InvalidOperationException("Unsupported audio format");

                Chk(_cl.Initialize(0, 0, 2000000L, 0, _fmt, IntPtr.Zero));     // شیر مود، بافر ۲۰۰ms
                Chk(_cl.GetBufferSize(out _bufFrames));
                Guid irc = new Guid("F294ACFC-3146-4483-A7BF-ADDCA7C260E2");
                Chk(_cl.GetService(ref irc, out object r));
                _rc = (IAudioRenderClient)r;
                Chk(_cl.Start());

                _run = true;
                _th = new Thread(Loop) { IsBackground = true, Name = "MicBridge" };
                _th.Start();
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        public void Feed(short[] s, int n)
        {
            lock (_q)
            {
                int cap = _ring.Length, keep = SrcRate * 15 / 100;
                if (n > keep) { s = Sub(s, n - keep, keep); n = keep; }
                if (_cnt + n > keep)                // ورودی بیش از حد جلو افتاده: قدیمی‌ها رو بریز
                {
                    int drop = _cnt + n - keep;
                    _rd = (_rd + drop) % cap; _cnt -= drop;
                    _pos = 0;
                }
                int wr = (_rd + _cnt) % cap;
                for (int i = 0; i < n; i++) { _ring[wr] = s[i]; wr = (wr + 1) % cap; }
                _cnt += n;
            }
        }

        private static short[] Sub(short[] s, int from, int len)
        {
            var r = new short[len];
            Array.Copy(s, from, r, 0, len);
            return r;
        }

        private void Loop()
        {
            double step = (double)SrcRate / _rate;
            int target = _rate * 30 / 1000;         // نگه داشتن حدود ۳۰ms تو بافر دستگاه (تاخیر کم)
            while (_run)
            {
                try
                {
                    _cl!.GetCurrentPadding(out uint pad);
                    int need = target - (int)pad;
                    if (need > 0 && (uint)need <= _bufFrames - pad)
                    {
                        Chk(_rc!.GetBuffer((uint)need, out IntPtr buf));
                        Fill(buf, need, step);
                        Chk(_rc.ReleaseBuffer((uint)need, 0));
                    }
                }
                catch { }
                Thread.Sleep(3);
            }
        }

        private void Fill(IntPtr buf, int frames, double step)
        {
            lock (_q)
            {
                int cap = _ring.Length;
                for (int f = 0; f < frames; f++)
                {
                    float v = 0f;
                    if (_cnt >= 2)
                    {
                        short a = _ring[_rd], b = _ring[(_rd + 1) % cap];
                        v = (a + (b - a) * (float)_pos) / 32768f;
                        _pos += step;
                        int adv = (int)_pos;
                        if (adv > 0)
                        {
                            if (adv > _cnt - 1) adv = _cnt - 1;
                            _rd = (_rd + adv) % cap; _cnt -= adv; _pos -= (int)_pos;
                        }
                    }
                    for (int c = 0; c < _ch; c++)
                    {
                        if (_float) { _one[0] = v; Marshal.Copy(_one, 0, IntPtr.Add(buf, (f * _ch + c) * 4), 1); }
                        else
                        {
                            int off = (f * _ch + c) * 2;
                            int x = (int)(v * 32767f); if (x > 32767) x = 32767; else if (x < -32768) x = -32768;
                            Marshal.WriteInt16(buf, off, (short)x);
                        }
                    }
                }
            }
        }

        public void Dispose()
        {
            _run = false;
            try { _th?.Join(300); } catch { }
            try { _cl?.Stop(); } catch { }
            if (_fmt != IntPtr.Zero) { try { Marshal.FreeCoTaskMem(_fmt); } catch { } _fmt = IntPtr.Zero; }
            Rel(_rc); _rc = null;
            Rel(_cl); _cl = null;
            Rel(_dev); _dev = null;
            Rel(_en); _en = null;
        }
    }

    // ---------------- COM ----------------

    [DllImport("ole32.dll")]
    private static extern int PropVariantClear(ref PropVariant pv);

    [StructLayout(LayoutKind.Sequential)]
    private struct PropertyKey { public Guid fmtid; public uint pid; }

    [StructLayout(LayoutKind.Explicit, Size = 24)]
    private struct PropVariant
    {
        [FieldOffset(0)] public ushort vt;
        [FieldOffset(8)] public IntPtr ptr;
    }

    [ComImport, Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
    private class MMDeviceEnumeratorCom { }

    [ComImport, Guid("870AF99C-171D-4F9E-AF0D-E63DF40C2BC9")]
    private class PolicyConfigClient { }

    [ComImport, Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMDeviceEnumerator
    {
        [PreserveSig] int EnumAudioEndpoints(int dataFlow, int stateMask, out IMMDeviceCollection devices);
        [PreserveSig] int GetDefaultAudioEndpoint(int dataFlow, int role, out IMMDevice device);
        [PreserveSig] int GetDevice([MarshalAs(UnmanagedType.LPWStr)] string id, out IMMDevice device);
        [PreserveSig] int RegisterEndpointNotificationCallback(IntPtr client);
        [PreserveSig] int UnregisterEndpointNotificationCallback(IntPtr client);
    }

    [ComImport, Guid("0BD7A1BE-7A1A-44DB-8397-CC5392387B5E"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMDeviceCollection
    {
        [PreserveSig] int GetCount(out uint count);
        [PreserveSig] int Item(uint index, out IMMDevice device);
    }

    [ComImport, Guid("D666063F-1587-4E43-81F1-B948E807363F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMDevice
    {
        [PreserveSig] int Activate(ref Guid iid, int clsCtx, IntPtr activationParams, [MarshalAs(UnmanagedType.IUnknown)] out object iface);
        [PreserveSig] int OpenPropertyStore(int access, out IPropertyStore props);
        [PreserveSig] int GetId([MarshalAs(UnmanagedType.LPWStr)] out string id);
        [PreserveSig] int GetState(out int state);
    }

    [ComImport, Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IPropertyStore
    {
        [PreserveSig] int GetCount(out uint count);
        [PreserveSig] int GetAt(uint index, out PropertyKey key);
        [PreserveSig] int GetValue(ref PropertyKey key, out PropVariant value);
        [PreserveSig] int SetValue(ref PropertyKey key, IntPtr value);
        [PreserveSig] int Commit();
    }

    [ComImport, Guid("1CB9AD4C-DBFA-4C32-B178-C2F568A703B2"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IAudioClient
    {
        [PreserveSig] int Initialize(int shareMode, uint streamFlags, long hnsBufferDuration, long hnsPeriodicity, IntPtr format, IntPtr audioSessionGuid);
        [PreserveSig] int GetBufferSize(out uint frames);
        [PreserveSig] int GetStreamLatency(out long latency);
        [PreserveSig] int GetCurrentPadding(out uint padding);
        [PreserveSig] int IsFormatSupported(int shareMode, IntPtr format, out IntPtr closest);
        [PreserveSig] int GetMixFormat(out IntPtr format);
        [PreserveSig] int GetDevicePeriod(out long defaultPeriod, out long minPeriod);
        [PreserveSig] int Start();
        [PreserveSig] int Stop();
        [PreserveSig] int Reset();
        [PreserveSig] int SetEventHandle(IntPtr eventHandle);
        [PreserveSig] int GetService(ref Guid riid, [MarshalAs(UnmanagedType.IUnknown)] out object service);
    }

    [ComImport, Guid("F294ACFC-3146-4483-A7BF-ADDCA7C260E2"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IAudioRenderClient
    {
        [PreserveSig] int GetBuffer(uint frames, out IntPtr data);
        [PreserveSig] int ReleaseBuffer(uint frames, uint flags);
    }

    /// <summary>IPolicyConfig (غیرمستند ولی پایدار از ویندوز 7 تا 11)؛ فقط SetEndpointVisibility استفاده میشه، بقیه جای‌نگهدارِ ترتیب vtable هستن</summary>
    [ComImport, Guid("F8679F50-850A-41CF-9C72-430F290290C8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IPolicyConfig
    {
        [PreserveSig] int GetMixFormat([MarshalAs(UnmanagedType.LPWStr)] string id, IntPtr format);
        [PreserveSig] int GetDeviceFormat([MarshalAs(UnmanagedType.LPWStr)] string id, int def, IntPtr format);
        [PreserveSig] int ResetDeviceFormat([MarshalAs(UnmanagedType.LPWStr)] string id);
        [PreserveSig] int SetDeviceFormat([MarshalAs(UnmanagedType.LPWStr)] string id, IntPtr endpointFormat, IntPtr mixFormat);
        [PreserveSig] int GetProcessingPeriod([MarshalAs(UnmanagedType.LPWStr)] string id, int def, IntPtr defPeriod, IntPtr minPeriod);
        [PreserveSig] int SetProcessingPeriod([MarshalAs(UnmanagedType.LPWStr)] string id, IntPtr period);
        [PreserveSig] int GetShareMode([MarshalAs(UnmanagedType.LPWStr)] string id, IntPtr mode);
        [PreserveSig] int SetShareMode([MarshalAs(UnmanagedType.LPWStr)] string id, IntPtr mode);
        [PreserveSig] int GetPropertyValue([MarshalAs(UnmanagedType.LPWStr)] string id, int fx, IntPtr key, IntPtr value);
        [PreserveSig] int SetPropertyValue([MarshalAs(UnmanagedType.LPWStr)] string id, int fx, IntPtr key, IntPtr value);
        [PreserveSig] int SetDefaultEndpoint([MarshalAs(UnmanagedType.LPWStr)] string id, int role);
        [PreserveSig] int SetEndpointVisibility([MarshalAs(UnmanagedType.LPWStr)] string id, int visible);
    }
}
