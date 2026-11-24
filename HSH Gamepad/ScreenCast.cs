// HSH Gamepad | Telegram: @HSHGamepad
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GamepadServer;

/// <summary>یکی از نمایشگرهای کامپیوتر (برای انتخاب نمایشگر و محدود کردن کیفیت/هرتز)</summary>
public sealed class MonInfo
{
    public int Index, X, Y, W, H, Hz;
    public bool Primary;
    public string Name = "";        // مثل \\.\DISPLAY1 (برای پیدا کردنش تو DXGI)
}

/// <summary>
/// حالت «نمایشگر لمسی» (فقط پلیر ۱ و فقط وقتی اجازه‌ی ماوس/کیبورد تو پنجره‌ی سرور روشنه):
///   گوشی  ->  sc &lt;نمایشگر&gt; &lt;ارتفاع&gt; &lt;fps&gt; &lt;نشانگر 0|1&gt; [&lt;بافر 1..12&gt;]   شروع / عوض کردن پخش تصویر (بافر بیشتر = روان‌تر و fps بالاتر ولی با کمی تاخیر)
///   گوشی  ->  scx                                    توقف پخش
///   گوشی  ->  sca                                    «این فریم نمایش داده شد» (فریم بعدی بیاد)
///   گوشی  ->  scinfo                                 لیست نمایشگرها  (جواب: {"t":"scr","mons":[...]})
///   گوشی  ->  tp &lt;x 0..1&gt; &lt;y 0..1&gt;                  جای انگشت روی تصویر (مثل نمایشگر لمسی) => نشانگر ماوس اونجا میره
///   گوشی  ->  tx &lt;متن URL-encode شده&gt;                 تایپ متن (یونیکد؛ فارسی هم میشه)
///   سرور  ->  فریم JPEG به‌صورت پیام باینری WebSocket  ،  {"t":"scs",...} وضعیت اعمال‌شده
/// کلیک/اسکرول/کیبورد از همون mb / mw / kb معمولی میان (InputInjector).
/// تصویر با GDI گرفته میشه (درایور اضافه نمیخواد، روی ویندوز 7 هم کار میکنه). رزولوشن واقعی کامپیوتر هیچ‌وقت عوض نمیشه؛
/// فقط تصویری که برای گوشی فرستاده میشه کوچیک میشه (مثل کیفیت 480p / 720p / 1080p تو ویدیو).
/// </summary>
public static class ScreenCast
{
    // ---------------- نمایشگرها ----------------

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    private struct DEVMODE
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string dmDeviceName;
        public ushort dmSpecVersion, dmDriverVersion, dmSize, dmDriverExtra;
        public uint dmFields;
        public int dmPositionX, dmPositionY;
        public uint dmDisplayOrientation, dmDisplayFixedOutput;
        public short dmColor, dmDuplex, dmYResolution, dmTTOption, dmCollate;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string dmFormName;
        public ushort dmLogPixels;
        public uint dmBitsPerPel, dmPelsWidth, dmPelsHeight, dmDisplayFlags, dmDisplayFrequency;
        public uint dmICMMethod, dmICMIntent, dmMediaType, dmDitherType, dmReserved1, dmReserved2, dmPanningWidth, dmPanningHeight;
    }

    [DllImport("user32.dll", CharSet = CharSet.Ansi)]
    private static extern bool EnumDisplaySettings(string? deviceName, int modeNum, ref DEVMODE devMode);

    private static int RefreshOf(string deviceName)
    {
        try
        {
            var dm = new DEVMODE();
            dm.dmSize = (ushort)Marshal.SizeOf(typeof(DEVMODE));
            if (EnumDisplaySettings(deviceName, -1, ref dm))      // -1 = ENUM_CURRENT_SETTINGS
            {
                int hz = (int)dm.dmDisplayFrequency;
                if (hz > 1) return hz;
            }
        }
        catch { }
        return 60;      // 0 یا 1 یعنی «پیش‌فرض درایور»؛ ۶۰ فرض میکنیم
    }

    public static List<MonInfo> Monitors()
    {
        var res = new List<MonInfo>();
        try
        {
            var all = Screen.AllScreens;
            for (int i = 0; i < all.Length; i++)
            {
                var b = all[i].Bounds;
                res.Add(new MonInfo { Index = i, X = b.X, Y = b.Y, W = b.Width, H = b.Height, Hz = RefreshOf(all[i].DeviceName), Primary = all[i].Primary, Name = all[i].DeviceName });
            }
        }
        catch { }
        return res;
    }

    private static string InfoJson(bool allowed)
    {
        var sb = new StringBuilder();
        sb.Append("{\"t\":\"scr\",\"ok\":").Append(allowed ? "1" : "0").Append(",\"mons\":[");
        if (allowed)
        {
            bool first = true;
            foreach (var m in Monitors())
            {
                if (!first) sb.Append(',');
                first = false;
                sb.Append("{\"i\":").Append(m.Index).Append(",\"w\":").Append(m.W).Append(",\"h\":").Append(m.H)
                  .Append(",\"hz\":").Append(m.Hz).Append(",\"p\":").Append(m.Primary ? 1 : 0).Append('}');
            }
        }
        sb.Append("]}");
        return sb.ToString();
    }

    // ---------------- دستورهای گوشی ----------------

    private static bool I(string s, out int v) => int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out v);
    private static bool F(string s, out float v) => float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out v);

    /// <summary>sc / scx / scinfo / tp / tx</summary>
    public static void Command(Conn conn, Hub hub, string[] p)
    {
        string cmd = p[0];

        if (cmd == "scinfo")
        {
            _ = conn.SendAsync(InfoJson(hub.AllowInput));
            return;
        }

        // بقیه فقط تو حالت «نمایشگر لمسی» برای پلیر ۱ و وقتی سرور اجازه داده
        if (!hub.AllowInput || conn.Mode != "screen" || conn.Slot == null || conn.Slot.N != 1) return;

        if (cmd == "sca")
        {
            conn.CastAck();
            return;
        }

        if (cmd == "scx")
        {
            conn.CastStop();
            return;
        }

        if (cmd == "sc" && p.Length >= 5 && I(p[1], out int mon) && I(p[2], out int h) && I(p[3], out int fps) && I(p[4], out int cur))
        {
            var mons = Monitors();
            if (mons.Count == 0) return;
            if (mon < 0 || mon >= mons.Count)
            {
                mon = 0;
                for (int i = 0; i < mons.Count; i++) if (mons[i].Primary) { mon = i; break; }
            }
            var m = mons[mon];

            // سقف: نه بالاتر از رزولوشن نمایشگر، نه بالاتر از هرتز نمایشگر (سقف گوشی رو خود صفحه رعایت میکنه)
            h = Math.Max(120, Math.Min(h, m.H));
            fps = Math.Max(5, Math.Min(fps, Math.Max(5, m.Hz)));

            conn.ScreenMon = mon;
            conn.ScrX = m.X; conn.ScrY = m.Y; conn.ScrW = m.W; conn.ScrH = m.H;
            int buf = 4;
            if (p.Length >= 6) I(p[5], out buf);
            buf = Math.Max(1, Math.Min(12, buf));
            bool h264 = p.Length >= 7 && p[6] == "h264";
            conn.CastConfigure(hub, mon, h, fps, cur != 0, buf, h264);
            _ = conn.SendAsync("{\"t\":\"scs\",\"mon\":" + mon + ",\"h\":" + h + ",\"fps\":" + fps + "}");
            return;
        }

        if (cmd == "tp" && p.Length == 3 && F(p[1], out float nx) && F(p[2], out float ny))
        {
            if (conn.ScrW <= 0 || conn.ScrH <= 0) return;
            nx = Math.Max(0f, Math.Min(1f, nx));
            ny = Math.Max(0f, Math.Min(1f, ny));
            int x = conn.ScrX + (int)Math.Round(nx * (conn.ScrW - 1));
            int y = conn.ScrY + (int)Math.Round(ny * (conn.ScrH - 1));
            InputInjector.MoveTo(x, y);
            return;
        }

        if (cmd == "tx" && p.Length == 2)
        {
            string text;
            try { text = Uri.UnescapeDataString(p[1]); } catch { return; }
            InputInjector.TypeText(text);
            return;
        }
    }

    // ---------------- ضبط تصویر ----------------

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT { public int x, y; }

    [StructLayout(LayoutKind.Sequential)]
    private struct CURSORINFO { public int cbSize; public int flags; public IntPtr hCursor; public POINT ptScreenPos; }

    [StructLayout(LayoutKind.Sequential)]
    private struct ICONINFO { public bool fIcon; public int xHotspot; public int yHotspot; public IntPtr hbmMask; public IntPtr hbmColor; }

    [DllImport("user32.dll")] private static extern bool GetCursorInfo(ref CURSORINFO pci);
    [DllImport("user32.dll")] private static extern bool GetIconInfo(IntPtr hIcon, out ICONINFO piconinfo);
    [DllImport("user32.dll")] private static extern bool DrawIconEx(IntPtr hdc, int xLeft, int yTop, IntPtr hIcon, int cxWidth, int cyHeight, uint istepIfAniCur, IntPtr hbrFlickerFreeDraw, uint diFlags);
    [DllImport("user32.dll")] private static extern int GetSystemMetrics(int nIndex);
    [DllImport("gdi32.dll")] private static extern bool DeleteObject(IntPtr hObject);
    [DllImport("winmm.dll")] private static extern uint timeBeginPeriod(uint uPeriod);
    [DllImport("winmm.dll")] private static extern uint timeEndPeriod(uint uPeriod);

    [DllImport("user32.dll")] private static extern IntPtr GetDC(IntPtr hWnd);
    [DllImport("user32.dll")] private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);
    [DllImport("gdi32.dll")] private static extern bool BitBlt(IntPtr hdcDest, int xDest, int yDest, int w, int h, IntPtr hdcSrc, int xSrc, int ySrc, int rop);

    /// <summary>
    /// گرفتن تصویر نمایشگر روی g. Graphics.CopyFromScreen ترکیب SourceCopy|CaptureBlt رو قبول نمیکنه (InvalidEnumArgumentException)،
    /// پس مستقیم BitBlt با CAPTUREBLT صدا زده میشه (پنجره‌های نیمه‌شفاف هم تو تصویر میان)؛ اگه نشد، CopyFromScreen ساده.
    /// </summary>
    // CAPTUREBLT کُنده و باعث چشمک زدن نشانگر میشه؛ از ویندوز 8 به بعد (DWM همیشه روشنه) لازم نیست
    private static readonly bool UseCaptureBlt = Environment.OSVersion.Version < new Version(6, 2);

    private static void CaptureScreen(Graphics g, Rectangle b)
    {
        bool ok = false;
        IntPtr src = GetDC(IntPtr.Zero);
        if (src != IntPtr.Zero)
        {
            IntPtr dst = g.GetHdc();
            try { ok = BitBlt(dst, 0, 0, b.Width, b.Height, src, b.X, b.Y, UseCaptureBlt ? (0x00CC0020 | 0x40000000) : 0x00CC0020); }      // SRCCOPY (| CAPTUREBLT فقط ویندوز 7)
            finally
            {
                g.ReleaseHdc(dst);
                ReleaseDC(IntPtr.Zero, src);
            }
        }
        if (!ok) g.CopyFromScreen(b.X, b.Y, 0, 0, new Size(b.Width, b.Height), CopyPixelOperation.SourceCopy);
    }

    private static void DrawCursor(Graphics g, Rectangle b, double scale)
    {
        try
        {
            var ci = new CURSORINFO { cbSize = Marshal.SizeOf(typeof(CURSORINFO)) };
            if (!GetCursorInfo(ref ci) || (ci.flags & 1) == 0 || ci.hCursor == IntPtr.Zero) return;   // 1 = CURSOR_SHOWING

            int hx = 0, hy = 0;
            if (GetIconInfo(ci.hCursor, out ICONINFO ii))
            {
                hx = ii.xHotspot; hy = ii.yHotspot;
                if (ii.hbmMask != IntPtr.Zero) DeleteObject(ii.hbmMask);
                if (ii.hbmColor != IntPtr.Zero) DeleteObject(ii.hbmColor);
            }

            int size = Math.Max(14, (int)Math.Round(GetSystemMetrics(13) * scale));      // 13 = SM_CXCURSOR
            int x = (int)Math.Round((ci.ptScreenPos.x - b.X - hx) * scale);
            int y = (int)Math.Round((ci.ptScreenPos.y - b.Y - hy) * scale);

            IntPtr hdc = g.GetHdc();
            try { DrawIconEx(hdc, x, y, ci.hCursor, size, size, 0, IntPtr.Zero, 3); }      // 3 = DI_NORMAL
            finally { g.ReleaseHdc(hdc); }
        }
        catch { }
    }

    /// <summary>
    /// کوچیک کردن سریع (میانگین‌گیری جعبه‌ای، چندهسته‌ای). جایگزین Graphics.DrawImage با HighQualityBilinear که روی CPU خیلی کنده
    /// (همون دلیل اینکه رزولوشن پایین‌تر fps کمتر میداد). dst باید کوچیک‌تر یا مساوی src باشه.
    /// </summary>
    private static unsafe void FastScale(Bitmap srcBmp, Bitmap dstBmp)
    {
        int sw = srcBmp.Width, sh = srcBmp.Height, dw = dstBmp.Width, dh = dstBmp.Height;
        var sd = srcBmp.LockBits(new Rectangle(0, 0, sw, sh), ImageLockMode.ReadOnly, PixelFormat.Format32bppRgb);
        BitmapData? dd = null;
        try
        {
            dd = dstBmp.LockBits(new Rectangle(0, 0, dw, dh), ImageLockMode.WriteOnly, PixelFormat.Format32bppRgb);
            IntPtr s0 = sd.Scan0, d0 = dd.Scan0;
            int sStride = sd.Stride, dStride = dd.Stride;

            var xa = new int[dw]; var xb = new int[dw];
            for (int x = 0; x < dw; x++)
            {
                int a = (int)((long)x * sw / dw);
                int b = (int)(((long)(x + 1) * sw + dw - 1) / dw);
                if (b <= a) b = a + 1;
                if (b > sw) b = sw;
                xa[x] = a; xb[x] = b;
            }

            Parallel.For(0, dh, y =>
            {
                byte* sBase = (byte*)s0.ToPointer();
                byte* drow = (byte*)d0.ToPointer() + (long)y * dStride;
                int ya = (int)((long)y * sh / dh);
                int yb = (int)(((long)(y + 1) * sh + dh - 1) / dh);
                if (yb <= ya) yb = ya + 1;
                if (yb > sh) yb = sh;
                int ny = yb - ya;
                for (int x = 0; x < dw; x++)
                {
                    int a = xa[x], b = xb[x], n = (b - a) * ny;
                    int cb = 0, cg = 0, cr = 0;
                    for (int yy = ya; yy < yb; yy++)
                    {
                        byte* p = sBase + (long)yy * sStride + a * 4;
                        for (int xx = a; xx < b; xx++, p += 4) { cb += p[0]; cg += p[1]; cr += p[2]; }
                    }
                    byte* o = drow + x * 4;
                    o[0] = (byte)(cb / n); o[1] = (byte)(cg / n); o[2] = (byte)(cr / n); o[3] = 255;
                }
            });
        }
        finally
        {
            if (dd != null) dstBmp.UnlockBits(dd);
            srcBmp.UnlockBits(sd);
        }
    }

    /// <summary>امضای نشانگر ماوس (جا + شکل)؛ وقتی فقط ماوس تکون خورده تصویر رو دوباره بسازیم</summary>
    private static long CursorSig()
    {
        try
        {
            var ci = new CURSORINFO { cbSize = Marshal.SizeOf(typeof(CURSORINFO)) };
            if (!GetCursorInfo(ref ci) || (ci.flags & 1) == 0) return 1;
            return ((long)ci.ptScreenPos.x * 73856093L) ^ ((long)ci.ptScreenPos.y * 19349663L) ^ ci.hCursor.ToInt64();
        }
        catch { return 0; }
    }

    private static readonly ImageCodecInfo? JpegCodec = FindJpeg();

    private static ImageCodecInfo? FindJpeg()
    {
        foreach (var c in ImageCodecInfo.GetImageEncoders())
            if (c.MimeType == "image/jpeg") return c;
        return null;
    }

    /// <summary>هش سریع تصویر (برای اینکه وقتی صفحه عوض نشده فریم تکراری نفرستیم)</summary>
    private static uint HashBitmap(Bitmap bmp, ref int[]? row)
    {
        int w = bmp.Width, h = bmp.Height;
        if (row == null || row.Length < w) row = new int[w];
        var bd = bmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format32bppRgb);
        uint hash = 2166136261;
        try
        {
            for (int y = 0; y < h; y++)
            {
                Marshal.Copy(IntPtr.Add(bd.Scan0, y * bd.Stride), row, 0, w);
                unchecked
                {
                    for (int x = 0; x < w; x++) hash = (hash ^ (uint)row[x]) * 16777619u;
                }
            }
        }
        finally { bmp.UnlockBits(bd); }
        return hash;
    }

    /// <summary>یک گوشی => یک ترد ضبط. فریم‌ها JPEG و پیام باینری WebSocket هستن.</summary>
    internal sealed class Caster
    {
        private readonly Conn _conn;
        private readonly Hub _hub;
        private Thread? _th;
        private volatile bool _stop;
        private volatile int _mon, _height = 720, _fps = 60;
        private volatile bool _cursor = true, _force = true;
        private int _ver;
        private int _pending;           // فریم‌هایی که فرستاده شده ولی گوشی هنوز نمایششون رو تایید نکرده
        private int _lastAckTick;      // آخرین باری که پیشرفتی بود (ارسال یا تایید)، برای رها کردن قفل اگه گوشی تایید نفرستاد
        private volatile int _maxPending = 4;
        private volatile bool _wantH264;          // گوشی H.264 خواسته
        private volatile bool _h264Failed;        // انکودر H.264 راه نیفتاد/خطا داد => JPEG
        private bool _jobH264;
        private int _jobFps, _jobBps;
        private readonly AutoResetEvent _wake = new AutoResetEvent(false);
        private BlockingCollection<byte[]>? _txq;     // صف ارسال H.264: ترد انکود منتظر نوشتن رو سوکت نمیمونه و فریم بعدی رو همزمان انکود میکنه (ترتیب حفظ میشه)

        private void TxLoop(BlockingCollection<byte[]> q)
        {
            try { foreach (var m in q.GetConsumingEnumerable()) { try { _conn.SendBinaryAsync(m).Wait(3000); } catch { } } }
            catch { }
        }

        private void EnqueueTx(byte[] m)
        {
            var q = _txq;
            if (q == null || q.IsAddingCompleted) return;
            try { q.TryAdd(m, 2000); } catch { }
        }

        public Caster(Conn conn, Hub hub)
        {
            _conn = conn;
            _hub = hub;
        }

        public void Configure(int mon, int height, int fps, bool cursor, int buf, bool h264 = false)
        {
            _wantH264 = h264; _h264Failed = false;
            _mon = mon; _height = height; _fps = fps; _cursor = cursor; _maxPending = Math.Max(1, Math.Min(12, buf));
            _force = true;
            Interlocked.Exchange(ref _pending, 0);
            Interlocked.Increment(ref _ver);
            if (_th == null || !_th.IsAlive)
            {
                _stop = false;
                _th = new Thread(Run) { IsBackground = true, Name = "ScreenCast", Priority = ThreadPriority.AboveNormal };
                _th.Start();
            }
        }

        public void Stop() { _stop = true; }

        /// <summary>گوشی نمایش یک فریم رو تایید کرد</summary>
        public void Ack()
        {
            if (Interlocked.Decrement(ref _pending) < 0) Interlocked.Exchange(ref _pending, 0);
            Volatile.Write(ref _lastAckTick, Environment.TickCount);
            try { _wake.Set(); } catch { }
        }

        private static string Esc(string t)
        {
            var sb = new StringBuilder();
            foreach (char c in t)
                sb.Append(c == '"' || c == '\\' || c < 0x20 ? ' ' : c);
            return sb.Length > 160 ? sb.ToString(0, 160) : sb.ToString();
        }

        private bool _errSent;
        private void ReportError(string t)
        {
            if (_errSent || !_conn.IsOpen) return;
            _errSent = true;
            _ = _conn.SendAsync("{\"t\":\"scs\",\"err\":\"" + Esc(t) + "\"}");
        }

        private bool Alive() => !_stop && _hub.AllowInput && _conn.Mode == "screen" && _conn.IsOpen;

        // ---- خط لوله: ترد ضبط (Run) فریم بعدی رو میگیره/کوچیک میکنه، همزمان ترد انکود (EncodeLoop) فریم قبلی رو JPEG میکنه و میفرسته ----
        // هیچ آبجکت GDI+ بین دو ترد مشترک نیست (قبلاً خطای «Object is currently in use elsewhere» میداد)؛ فقط پیکسل‌های خام (byte[]) رد و بدل میشن
        private byte[]? _jobBuf;
        private int _jobW, _jobH;
        private long _jobQ;
        private int _jobBusy;                 // 1 = ترد انکود هنوز پیکسل‌های _jobBuf رو برنداشته
        private int _tEncUs;                  // میانگین زمان انکود (میکروثانیه)
        private volatile bool _encStop;
        private readonly AutoResetEvent _jobEvt = new AutoResetEvent(false);
        private readonly AutoResetEvent _freeEvt = new AutoResetEvent(false);

        private void WaitEncIdle()
        {
            while (Volatile.Read(ref _jobBusy) == 1 && Alive()) _freeEvt.WaitOne(5);
        }

        /// <summary>کپی پیکسل‌های بیت‌مپ به آرایه (ترد ضبط)</summary>
        private static void CopyOut(Bitmap bmp, byte[] dst)
        {
            int w = bmp.Width, h = bmp.Height;
            var bd = bmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format32bppRgb);
            try
            {
                for (int y = 0; y < h; y++) Marshal.Copy(IntPtr.Add(bd.Scan0, y * bd.Stride), dst, y * w * 4, w * 4);
            }
            finally { bmp.UnlockBits(bd); }
        }

        /// <summary>کپی آرایه تو بیت‌مپ مخصوص ترد انکود</summary>
        private static void CopyIn(byte[] src, Bitmap bmp)
        {
            int w = bmp.Width, h = bmp.Height;
            var bd = bmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.WriteOnly, PixelFormat.Format32bppRgb);
            try
            {
                for (int y = 0; y < h; y++) Marshal.Copy(src, y * w * 4, IntPtr.Add(bd.Scan0, y * bd.Stride), w * 4);
            }
            finally { bmp.UnlockBits(bd); }
        }

        private void EncodeLoop()
        {
            var ms = new MemoryStream(1 << 18);
            var sw = new Stopwatch();
            Bitmap? eb = null;          // بیت‌مپ مخصوص همین ترد (JPEG)
            H264Encoder? h264 = null;
            var packets = new List<H264Packet>();
            bool initSent = false;
            uint seq = 0;
            long frameNo = 0;
            try
            {
                while (!_encStop)
                {
                    if (!_jobEvt.WaitOne(100)) continue;
                    byte[]? data = null;
                    try
                    {
                        var buf = _jobBuf;
                        int w = _jobW, h = _jobH;
                        long q = _jobQ;
                        bool want264 = _jobH264;
                        if (!want264 && h264 != null) { h264.Dispose(); h264 = null; }

                        if (want264 && buf != null && w > 0 && h > 0)
                        {
                            // ---------- H.264 ----------
                            sw.Restart();
                            if (h264 == null || h264.Width != w || h264.Height != h || h264.Fps != _jobFps)
                            {
                                h264?.Dispose();
                                h264 = H264Encoder.TryCreate(w, h, _jobFps, _jobBps, out string herr);
                                initSent = false; seq = 0; frameNo = 0;
                                if (h264 == null) { _h264Failed = true; ReportError(L.F("cast.FellBackToJPEG", herr)); }
                            }
                            if (h264 != null)
                            {
                                h264.SetFrame(buf);                    // BGRA -> NV12
                                Volatile.Write(ref _jobBusy, 0);       // پیکسل‌ها برداشته شد
                                _freeEvt.Set();
                                packets.Clear();
                                h264.Encode(packets);
                                int us = (int)(sw.Elapsed.TotalMilliseconds * 1000);
                                int old = Volatile.Read(ref _tEncUs);
                                Volatile.Write(ref _tEncUs, old == 0 ? us : (old * 3 + us) / 4);

                                foreach (var pk in packets)
                                {
                                    if (!initSent)
                                    {
                                        if (!pk.Key || h264.Sps == null || h264.Pps == null) continue;       // دیکودر باید با کلیدفریم شروع کنه
                                        SendInit(h264);
                                        initSent = true;
                                    }
                                    long dur = h264.DurationTicks;
                                    var frag = Fmp4.Fragment(++seq, (ulong)(frameNo++ * dur), (uint)dur, pk.Key, pk.Avcc);
                                    var msg = new byte[frag.Length + 2];
                                    msg[0] = 2; msg[1] = (byte)(pk.Key ? 1 : 0);
                                    Buffer.BlockCopy(frag, 0, msg, 2, frag.Length);
                                    if (Interlocked.Increment(ref _pending) == 1) Volatile.Write(ref _lastAckTick, Environment.TickCount);
                                    EnqueueTx(msg);
                                }
                            }
                        }
                        if (!(want264 && h264 != null) && buf != null && w > 0 && h > 0 && JpegCodec != null)
                        {
                            // ---------- JPEG ----------
                            sw.Restart();
                            if (eb == null || eb.Width != w || eb.Height != h)
                            {
                                eb?.Dispose();
                                eb = new Bitmap(w, h, PixelFormat.Format32bppRgb);
                            }
                            CopyIn(buf, eb);
                            Volatile.Write(ref _jobBusy, 0);       // پیکسل‌ها برداشته شد؛ ترد ضبط میتونه فریم بعدی رو بسازه
                            _freeEvt.Set();
                            using (var ep = new EncoderParameters(1))
                            {
                                ep.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, q);
                                ms.SetLength(0);
                                eb.Save(ms, JpegCodec, ep);
                                data = ms.ToArray();
                            }
                            int us = (int)(sw.Elapsed.TotalMilliseconds * 1000);
                            int old = Volatile.Read(ref _tEncUs);
                            Volatile.Write(ref _tEncUs, old == 0 ? us : (old * 3 + us) / 4);
                        }
                    }
                    catch (Exception ex)
                    {
                        if (_jobH264)
                        {
                            _h264Failed = true;                    // از این به بعد JPEG
                            try { h264?.Dispose(); } catch { }
                            h264 = null;
                            ReportError(L.F("cast.H264FellBackTo", ex.GetType().Name, ex.Message));
                        }
                        else
                        {
                            try { eb?.Dispose(); } catch { }
                            eb = null;
                            ReportError(L.F("cast.JPEGEncode", ex.GetType().Name, ex.Message));
                        }
                    }
                    finally
                    {
                        Volatile.Write(ref _jobBusy, 0);
                        _freeEvt.Set();
                    }

                    if (data != null)
                    {
                        try { _conn.SendBinaryAsync(data).Wait(3000); } catch { }      // تا نوشتن رو سوکت تموم نشه فریم بعدی انکود نمیشه
                    }
                }
            }
            finally { h264?.Dispose(); eb?.Dispose(); ms.Dispose(); }
        }

        /// <summary>init segment H.264 برای گوشی: [1][طول کدک][کدک][عرض 2بایت][ارتفاع 2بایت][init]</summary>
        private void SendInit(H264Encoder enc)
        {
            var init = Fmp4.Init(enc.Sps!, enc.Pps!, enc.Width, enc.Height);
            var codec = System.Text.Encoding.ASCII.GetBytes(enc.CodecString());
            var msg = new byte[2 + codec.Length + 4 + init.Length];
            msg[0] = 1; msg[1] = (byte)codec.Length;
            Buffer.BlockCopy(codec, 0, msg, 2, codec.Length);
            int o = 2 + codec.Length;
            msg[o] = (byte)(enc.Width >> 8); msg[o + 1] = (byte)enc.Width; msg[o + 2] = (byte)(enc.Height >> 8); msg[o + 3] = (byte)enc.Height;
            Buffer.BlockCopy(init, 0, msg, o + 4, init.Length);
            EnqueueTx(msg);
        }

        private void Run()
        {
            try { timeBeginPeriod(1); } catch { }
            Bitmap? src = null;
            Bitmap? cur = null;
            DxgiCapture? dx = null;           // ضبط سریع با DXGI (اگه نشد: GDI)
            bool haveSrc = false;             // تو حالت DXGI: src آخرین تصویر کامل رو داره
            int dxFails = 0; bool dxOff = false, dxWorked = false; long dxTryAt = -100000;
            bool wasDx = false;
            bool dirty = false;               // DXGI: فریم جدیدی گرفتیم ولی هنوز نوبتش نشده بود بفرستیم
            int qAdj = 0; bool sawBlock = false; long qAdjAt = 0;      // کیفیت خودکار: وقتی شبکه/گوشی عقبه، JPEG کم‌حجم‌تر
            string devName = "";
            long lastCs = 0;
            int[]? rowBuf = null;
            var clock = Stopwatch.StartNew();
            var sw = new Stopwatch();
            double nextAt = 0;
            uint lastHash = 0;
            bool haveHash = false;
            Rectangle b = Rectangle.Empty;
            long boundsAt = -100000;
            int seenVer = -1;
            bool notifyOff = false;
            int fails = 0;          // چند بار پشت‌سرهم ضبط صفحه خطا داده
            string? errText = null;
            long lastSentAt = 0;
            double lastSentTicks = 0;      // مثل lastSentAt ولی دقیق‌تر (ms اعشاری)
            double nextDue = 0;            // موعد قبول فریم بعدی (ms)
            int monHz = 0;                 // هرتز نمایشگر انتخابی
            long wdAt = 0;                 // آخرین بار که نگهبان ضبط رو از نو ساخت
            double lastAcceptTicks = 0;    // لحظه‌ی رسیدن آخرین فریمی که قبول کردیم (نه بعد از پردازشش)؛ ریتم فریم‌ها از همین حساب میشه
            long tmAt = 0;
            double capMs = 0, sclMs = 0;
            int unchanged = 0;      // چند فریم پشت‌سرهم صفحه عوض نشده (وقتی صفحه ساکنه CPU کمتر مصرف بشه)

            Volatile.Write(ref _jobBusy, 0);
            _encStop = false;
            var txq = new BlockingCollection<byte[]>(64);
            _txq = txq;
            var tx = new Thread(() => TxLoop(txq)) { IsBackground = true, Name = "ScreenSend" };
            tx.Start();
            var enc = new Thread(EncodeLoop) { IsBackground = true, Name = "ScreenEncode", Priority = ThreadPriority.AboveNormal };
            enc.Start();

            try
            {
                while (Alive())
                {
                    // زمان‌های ضبط / کوچیک کردن / انکود (ms) برای نمایش تو آمار گوشی
                    if (clock.ElapsedMilliseconds - tmAt >= 1000)
                    {
                        tmAt = clock.ElapsedMilliseconds;
                        _ = _conn.SendAsync("{\"t\":\"scs\",\"tm\":[" + capMs.ToString("0.0", CultureInfo.InvariantCulture) + "," +
                            sclMs.ToString("0.0", CultureInfo.InvariantCulture) + "," +
                            (Volatile.Read(ref _tEncUs) / 1000.0).ToString("0.0", CultureInfo.InvariantCulture) + "]}");
                    }

                    double now = clock.Elapsed.TotalMilliseconds;
                    int fps = Math.Max(1, _fps);
                    double interval = 1000.0 / fps;
                    if (unchanged > fps * 2) interval = Math.Max(interval, 50);      // صفحه ۲ ثانیه ساکنه: ۲۰ بار در ثانیه فقط چک میکنیم

                    // گوشی هنوز فریم‌های قبلی رو تایید نکرده => صبر میکنیم (ولی نوبت این فریم هدر نمیره)
                    // وای‌فای کند: ویندوز فریم‌ها رو تو بافر سوکت جمع میکنه و گوشی چند ثانیه عقب میافته (تصویر مثل عکس ثابت میمونه).
                    // پس بیشتر از MaxPending فریم بدون تایید گوشی نمیفرستیم. اگه تایید نیومد (مثلاً گوشی قدیمی) بعد از ۱.۵ ثانیه قفل باز میشه. «بافر» = همین سقف؛ از تنظیمات گوشی عوض میشه.
                    if (Interlocked.CompareExchange(ref _pending, 0, 0) >= _maxPending)
                    {
                        if (unchecked(Environment.TickCount - Volatile.Read(ref _lastAckTick)) > 1500) Interlocked.Exchange(ref _pending, 0);
                        else
                        {
                            sawBlock = true;
                            _wake.WaitOne(2);          // با رسیدن تایید فوراً بیدار میشه
                            continue;
                        }
                    }

                    // نگهبان: بیشتر از ۳ ثانیه هیچ فریمی نرفته (مثلاً بعد از Win+P / روشن و خاموش شدن مانیتور دوم) => ضبط رو از نو میسازه
                    // (همون کاری که عوض کردن نمایشگر تو گوشی میکرد). صفحه‌ی ساکن هم هر ۱ ثانیه فریم میفرسته، پس ۳ ثانیه بدون فریم یعنی گیر کرده.
                    if (clock.ElapsedMilliseconds - lastSentAt > 3000 && clock.ElapsedMilliseconds - wdAt > 3000)
                    {
                        wdAt = clock.ElapsedMilliseconds;
                        try { dx?.Dispose(); } catch { }
                        dx = null; haveSrc = false; dirty = false; haveHash = false;
                        dxFails = 0; dxOff = false; dxWorked = false; dxTryAt = -100000;
                        seenVer = -1;                 // لیست نمایشگرها و مرزها دوباره خونده میشه
                        _force = true;
                    }

                    if (!wasDx && now < nextAt)                // هنوز وقت فریم بعدی نشده (فقط GDI؛ تو DXGI خود Grab منتظر میمونه)
                    {
                        double left = nextAt - now;
                        if (left > 2.5) Thread.Sleep(1); else Thread.Sleep(0);
                        continue;
                    }
                    nextAt = Math.Max(nextAt + interval, now);

                    int ver = _ver;
                    if (ver != seenVer || clock.ElapsedMilliseconds - boundsAt > 1000)
                    {
                        var mons = Monitors();
                        if (mons.Count == 0) { Thread.Sleep(200); continue; }
                        int mi = _mon;
                        if (mi < 0 || mi >= mons.Count) mi = 0;
                        var m = mons[mi];
                        b = new Rectangle(m.X, m.Y, m.W, m.H);
                        _conn.ScrX = m.X; _conn.ScrY = m.Y; _conn.ScrW = m.W; _conn.ScrH = m.H;      // نگاشت لمس با تغییر چیدمان نمایشگرها هماهنگ بمونه
                        boundsAt = clock.ElapsedMilliseconds;
                        monHz = m.Hz;
                        seenVer = ver;
                        if (m.Name != devName)                 // نمایشگر عوض شده: دوپلیکیت جدید
                        {
                            devName = m.Name;
                            try { dx?.Dispose(); } catch { }
                            dx = null; haveSrc = false; dxFails = 0; dxOff = false; dxWorked = false; dxTryAt = -100000;
                        }
                    }
                    if (b.Width < 2 || b.Height < 2) { Thread.Sleep(200); continue; }

                    if (dx == null && !dxOff && clock.ElapsedMilliseconds - dxTryAt > 1000)
                    {
                        dxTryAt = clock.ElapsedMilliseconds;
                        dx = DxgiCapture.TryCreate(devName);
                        if (dx == null) { if (!dxWorked && ++dxFails >= 3) dxOff = true; }      // ۳ بار نشد و هیچ‌وقت کار نکرده (مثلاً ویندوز 7) => همیشه GDI؛ اگه قبلاً کار کرده (UAC، قفل...) هر ثانیه دوباره تلاش میکنه
                        else haveSrc = false;
                    }
                    bool useDx = dx != null;
                    if (useDx != wasDx) { wasDx = useDx; haveHash = false; haveSrc = false; }

                    int srcW = useDx ? dx!.Width : b.Width, srcH = useDx ? dx!.Height : b.Height;
                    int dstH = Math.Max(2, Math.Min(_height, srcH));
                    int dstW = (int)Math.Round(srcW * (double)dstH / srcH);
                    if (dstH != srcH) { dstH &= ~1; dstW &= ~1; }
                    if (dstW < 2) dstW = 2;
                    if (dstH < 2) dstH = 2;
                    bool want264 = _wantH264 && !_h264Failed;
                    if (want264) { dstW &= ~1; dstH &= ~1; if (dstW < 2) dstW = 2; if (dstH < 2) dstH = 2; }      // H.264 اندازه‌ی زوج میخواد
                    bool direct = dstW == srcW && dstH == srcH;
                    bool drawCur = _cursor;

                    if (cur == null || cur.Width != dstW || cur.Height != dstH)
                    {
                        cur?.Dispose();
                        cur = new Bitmap(dstW, dstH, PixelFormat.Format32bppRgb);
                        haveHash = false;
                    }
                    if (direct && !useDx)
                    {
                        if (src != null) { src.Dispose(); src = null; }
                    }
                    else if (src == null || src.Width != srcW || src.Height != srcH)
                    {
                        src?.Dispose();
                        src = new Bitmap(srcW, srcH, PixelFormat.Format32bppRgb);
                        haveSrc = false;
                    }

                    try
                    {
                        sw.Restart();
                        double t1, t2 = 0;
                        if (useDx)
                        {
                            // DXGI: منتظر فریم جدید میمونیم؛ تغییر رو خود ویندوز میگه (هش لازم نیست)
                            double minGap = 1000.0 / fps;                  // (interval وقتی صفحه ساکنه بلندتر میشه؛ اینجا لازم نیست)
                            double sinceSent = clock.Elapsed.TotalMilliseconds - lastAcceptTicks;
                            int wait = dirty ? (int)Math.Max(1, Math.Min(minGap - sinceSent, 16)) : (int)Math.Max(2, Math.Min(minGap, 16));
                            int rc = dx!.Grab(src!, wait);
                            if (rc < 0)
                            {
                                try { dx.Dispose(); } catch { }
                                dx = null; dxTryAt = clock.ElapsedMilliseconds;
                                if (!dxWorked && ++dxFails >= 3) dxOff = true;
                                continue;
                            }
                            dxFails = 0; dxWorked = true;
                            if (rc == 1) haveSrc = true;
                            if (!haveSrc) continue;
                            double tArr = clock.Elapsed.TotalMilliseconds;      // لحظه‌ی رسیدن فریم (قبل از کپی/کوچیک کردن)

                            long cs = drawCur ? CursorSig() : 0;
                            bool due = clock.ElapsedMilliseconds - lastSentAt >= 1000;     // هر ۱ ثانیه یک فریم حتی اگه چیزی عوض نشده
                            bool changed = rc == 1 || cs != lastCs;
                            if (changed) dirty = true;
                            if (!dirty && !_force && !due) { unchanged++; continue; }
                            // قبلاً فاصله از «بعد از پردازش فریم قبلی» حساب میشد؛ پس هر چی پردازش کندتر (مثلاً کوچیک کردن) فریم‌ها «زود» حساب میشدن و نصف میافتادن (۶۰ => ۳۰).
                            // حالا از لحظه‌ی رسیدن فریم‌ها حساب میشه، پس ریتم میشه همون ریتم خود نمایشگر.
                            // زمان‌بندی تجمعی: فریم‌های نمایشگر رو شبکه‌ی ثابتی (مثلاً هر ۱۰ms برای ۱۰۰ هرتز) میرسن، ولی fps خواسته شده (مثلاً ۹۰ => هر ۱۱.۱ms) رو این شبکه نمیخونه.
                            // قبلاً «حداقل فاصله از فریم قبلی» چک میشد و فریم‌های کمی زود (با لرزش زمانی) میافتادن (۹۰ => ۸۳). حالا «موعد» هر فریم جمع میشه و
                            // فریمی که تا نصف دوره‌ی نمایشگر زودتر از موعدش برسه قبول میشه؛ پس میانگین دقیقاً به fps خواسته شده میرسه.
                            double halfV = monHz > 0 ? 500.0 / monHz : minGap * 0.5;
                            if (!_force && !due && tArr + halfV < nextDue) continue;   // هنوز موعدش نشده؛ dirty میمونه تا نوبتش بشه
                            nextDue = Math.Max(nextDue + minGap, tArr - halfV);          // (اگه مدتی بیکار بودیم موعد عقب نمیمونه)
                            lastAcceptTicks = tArr;
                            dirty = false;
                            lastCs = cs;
                            t1 = sw.Elapsed.TotalMilliseconds;

                            double scale = (double)dstH / b.Height;
                            if (!direct) FastScale(src!, cur);
                            using (var g2 = Graphics.FromImage(cur))
                            {
                                if (direct)
                                {
                                    g2.CompositingMode = CompositingMode.SourceCopy;
                                    g2.InterpolationMode = InterpolationMode.NearestNeighbor;
                                    g2.PixelOffsetMode = PixelOffsetMode.Half;
                                    g2.DrawImage(src!, new Rectangle(0, 0, dstW, dstH), 0, 0, srcW, srcH, GraphicsUnit.Pixel);
                                }
                                if (drawCur) DrawCursor(g2, b, scale);
                            }
                            t2 = sw.Elapsed.TotalMilliseconds - t1;
                        }
                        else
                        {
                            using (var g = Graphics.FromImage(direct ? cur : src!))
                            {
                                CaptureScreen(g, b);
                                if (direct && drawCur) DrawCursor(g, b, 1.0);
                            }
                            t1 = sw.Elapsed.TotalMilliseconds;
                            if (!direct)
                            {
                                double scale = (double)dstH / srcH;
                                FastScale(src!, cur);
                                using (var g2 = Graphics.FromImage(cur))
                                {
                                    if (drawCur) DrawCursor(g2, b, scale);
                                }
                                t2 = sw.Elapsed.TotalMilliseconds - t1;
                            }
                        }
                        capMs = capMs == 0 ? t1 : capMs * 0.8 + t1 * 0.2;
                        sclMs = sclMs == 0 ? t2 : sclMs * 0.8 + t2 * 0.2;
                    }
                    catch (Exception ex)
                    {
                        // قفل بودن ویندوز / صفحه‌ی UAC / عوض شدن رزولوشن: چند لحظه صبر و دوباره
                        // اگه چند بار پشت‌سرهم خطا بده، متنش به گوشی هم میرسه (تا بی‌صدا گیر نکنه)
                        if (++fails == 4) ReportError(L.F("cast.ScreenCapture", ex.GetType().Name, ex.Message));
                        // بیت‌مپ‌ها دور ریخته میشن تا اگه یکیشون خراب شده باشه، دوباره از نو ساخته بشن
                        try { src?.Dispose(); } catch { }
                        try { cur?.Dispose(); } catch { }
                        src = null; cur = null;
                        Thread.Sleep(150);
                        seenVer = -1;
                        continue;
                    }
                    fails = 0;

                    if (!useDx)
                    {
                        uint hash = HashBitmap(cur, ref rowBuf);
                        bool force = _force;
                        if (!force && haveHash && hash == lastHash)                                   // صفحه عوض نشده
                        {
                            unchanged++;
                            if (clock.ElapsedMilliseconds - lastSentAt < 1000) continue;             // ولی هر ۱ ثانیه یک فریم میفرستیم (اگه هش اشتباه کرده باشه تصویر هیچ‌وقت بیشتر از ۱ ثانیه کهنه نمیمونه)
                        }
                        lastHash = hash;
                        haveHash = true;
                    }
                    unchanged = 0;
                    _force = false;

                    if (JpegCodec == null) { ReportError(L.T("cast.JPEGEncoderNotFound")); Thread.Sleep(2000); continue; }

                    double waitFrom = clock.Elapsed.TotalMilliseconds;
                    WaitEncIdle();                             // ترد انکود هنوز پیکسل‌های فریم قبلی رو برنداشته
                    if (!Alive()) break;
                    if (clock.Elapsed.TotalMilliseconds - waitFrom > 4) sawBlock = true;      // انکود/ارسال فریم قبلی طول کشیده (شبکه کند) => کیفیت پایین‌تر

                    int need = dstW * dstH * 4;
                    if (_jobBuf == null || _jobBuf.Length < need) _jobBuf = new byte[need];
                    try { CopyOut(cur, _jobBuf); }
                    catch (Exception ex)
                    {
                        ReportError(L.F("cast.ImageCopy", ex.GetType().Name, ex.Message));
                        try { cur.Dispose(); } catch { }
                        cur = null;
                        Thread.Sleep(50);
                        continue;
                    }
                    _jobW = dstW; _jobH = dstH;
                    _jobH264 = want264;
                    _jobFps = fps;
                    _jobBps = (int)Math.Max(2000000, Math.Min(20000000, dstW * (double)dstH * fps * 0.08));
                    if (lastSentAt - qAdjAt > 300)
                    {
                        qAdjAt = lastSentAt;
                        qAdj = sawBlock ? Math.Max(-40, qAdj - 6) : Math.Min(0, qAdj + 2);     // وقتی صف پر شده (شبکه کند) کیفیت کم میشه تا فریم کوچیک‌تر و fps بالاتر بشه
                        sawBlock = false;
                    }
                    _jobQ = Math.Max(30, (dstH <= 540 ? 62 : dstH <= 800 ? 56 : 50) + qAdj);
                    lastSentAt = clock.ElapsedMilliseconds;
                    lastSentTicks = clock.Elapsed.TotalMilliseconds;
                    if (!want264 && Interlocked.Increment(ref _pending) == 1) Volatile.Write(ref _lastAckTick, Environment.TickCount);      // H.264: تو ترد انکود، به‌ازای هر بسته‌ی ارسالی
                    Volatile.Write(ref _jobBusy, 1);
                    _jobEvt.Set();
                }

                notifyOff = !_stop;
            }
            catch (Exception ex)
            {
                errText = ex.GetType().Name + " - " + ex.Message;
                notifyOff = !_stop;
            }
            finally
            {
                try { timeEndPeriod(1); } catch { }
                _encStop = true;
                try { enc.Join(1500); } catch { }
                try { txq.CompleteAdding(); tx.Join(1500); } catch { }
                try { dx?.Dispose(); } catch { }
                src?.Dispose();
                cur?.Dispose();
            }

            // خودش تموم شد (نه با دستور توقف): مثلاً اجازه‌ی ماوس/کیبورد تو پنجره‌ی سرور خاموش شد
            if (notifyOff && _conn.IsOpen)
                _ = _conn.SendAsync("{\"t\":\"scs\",\"off\":1" + (errText != null ? ",\"err\":\"" + Esc(errText) + "\"" : "") + "}");
        }
    }
}
