// HSH Gamepad | Telegram: @HSHGamepad
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Nefarius.ViGEm.Client;
using Nefarius.ViGEm.Client.Targets;
using Nefarius.ViGEm.Client.Targets.DualShock4;
using Nefarius.ViGEm.Client.Targets.Xbox360;

namespace GamepadServer;

/// <summary>یک پلیر = یک دسته‌ی مجازی (Xbox 360 یا DualShock 4). دسته فقط وقتی ساخته میشه که لازم باشه.</summary>
public sealed class Slot : IDisposable
{
    private static readonly Dictionary<string, Xbox360Button> XboxButtons = new()
    {
        ["A"] = Xbox360Button.A,
        ["B"] = Xbox360Button.B,
        ["X"] = Xbox360Button.X,
        ["Y"] = Xbox360Button.Y,
        ["LB"] = Xbox360Button.LeftShoulder,
        ["RB"] = Xbox360Button.RightShoulder,
        ["BACK"] = Xbox360Button.Back,
        ["START"] = Xbox360Button.Start,
        ["GUIDE"] = Xbox360Button.Guide,
        ["L3"] = Xbox360Button.LeftThumb,
        ["R3"] = Xbox360Button.RightThumb,
        ["DU"] = Xbox360Button.Up,
        ["DD"] = Xbox360Button.Down,
        ["DL"] = Xbox360Button.Left,
        ["DR"] = Xbox360Button.Right,
    };

    private static readonly Dictionary<string, DualShock4Button> Ds4Buttons = new()
    {
        ["A"] = DualShock4Button.Cross,
        ["B"] = DualShock4Button.Circle,
        ["X"] = DualShock4Button.Square,
        ["Y"] = DualShock4Button.Triangle,
        ["LB"] = DualShock4Button.ShoulderLeft,
        ["RB"] = DualShock4Button.ShoulderRight,
        ["BACK"] = DualShock4Button.Share,
        ["START"] = DualShock4Button.Options,
        ["L3"] = DualShock4Button.ThumbLeft,
        ["R3"] = DualShock4Button.ThumbRight,
    };

    private readonly ViGEmClient _client;
    private readonly object _lock = new();

    private IXbox360Controller? _x;
    private IDualShock4Controller? _d;
    private bool _fellBack;               // Xbox جا نداشت و خودکار PS4 شد
    private bool _du, _dd, _dl, _dr;      // وضعیت D-Pad برای PS4

    private bool _bA, _bB, _bX, _bY, _bLB, _bRB, _bLT, _bRT, _bBack, _bStart, _bGuide, _bTp, _bL3, _bR3;
    private float _lx, _ly, _rx, _ry;
    private byte _lt8, _rt8;            // مقدار آنالوگ ماشه‌ها (۰-۲۵۵): دکمه‌ی LT/RT = ۰ یا ۲۵۵، پدال فرمون = هر مقداری بین این دو

    public int N { get; }
    public string? Mode { get; private set; }
    public bool FellBack => _fellBack;
    public Conn? Owner { get; set; }
    public DateTime? IdleSince { get; set; }
    public bool HasPad { get { lock (_lock) return _x != null || _d != null; } }

    public Slot(ViGEmClient client, int n)
    {
        _client = client;
        N = n;
    }

    private void ClearVisualInputs()
    {
        _bA = _bB = _bX = _bY = _bLB = _bRB = _bLT = _bRT = _bBack = _bStart = _bGuide = _bTp = _bL3 = _bR3 = false;
        _lx = _ly = _rx = _ry = 0f;
        _lt8 = _rt8 = 0;
    }

    public PadState GetVisualState()
    {
        lock (_lock)
        {
            var owner = Owner;
            int gyAge = _gyroLive ? AgeMs(_gyTs) : -1;
            int gyMode = 0;                                   // 0 = خاموش / داده‌ای نمیاد
            if (_gyroLive && gyAge >= 0 && gyAge < 1500)
                gyMode = _d == null && _x != null ? 2 : (_rawUnsupported ? 3 : 1);
            return new PadState
            {
                N = N,
                Connected = owner != null,
                Mode = Mode ?? owner?.Mode ?? "ps4",
                FellBack = _fellBack,
                Name = owner?.Name ?? "",
                Device = owner?.Device ?? "",
                Ip = owner?.Ip ?? "",
                A = _bA, B = _bB, X = _bX, Y = _bY,
                LB = _bLB, RB = _bRB, LT = _bLT, RT = _bRT,
                BACK = _bBack, START = _bStart, GUIDE = _bGuide, TP = _bTp,
                L3 = _bL3, R3 = _bR3,
                DU = _du, DD = _dd, DL = _dl, DR = _dr,
                LX = _lx, LY = _ly, RX = _rx, RY = _ry,
                LTV = _lt8 / 255f, RTV = _rt8 / 255f,
                GyroState = gyMode,
                GyroHz = gyMode == 0 ? 0f : _hz,
                GyroCount = _gyCount,
                GX = _gx, GY = _gy, GZ = _gz,
                AX = _ax, AY = _ay, AZ = _az,
            };
        }
    }

    public string? SetMode(string mode)
    {
        lock (_lock)
        {
            if (InputInjector.IsInputMode(mode))      // ماوس/کیبورد: دسته‌ی مجازی نمیخواد
            {
                if (Mode == mode && _x == null && _d == null) return Mode;
                DisposePad();
                _fellBack = false;
                _du = _dd = _dl = _dr = false;
                ClearVisualInputs();
                Mode = mode;
                Console.WriteLine($"  player {N} -> {mode}");
                return Mode;
            }
            if (InputInjector.IsInputMode(Mode)) InputInjector.ReleaseAll();      // از ماوس/کیبورد رفت روی دسته: کلید گیرکرده آزاد بشه
            if (mode != "xbox" && mode != "ps4" && mode != "wheel") return Mode;   // wheel = فرمون (همون دسته‌ی Xbox 360 مجازی، فقط نمایشش تو تستر فرق داره)

            bool has = _x != null || _d != null;
            if (mode == Mode && has) return Mode;
            if (_x != null && ((mode == "wheel" && Mode == "xbox") || (mode == "xbox" && Mode == "wheel")))
            {
                Mode = mode;      // همون دسته‌ی Xbox مجازیه؛ فقط بین «فرمون» و «دسته» عوض شد، دوباره ساخته نمیشه
                return Mode;
            }
            if ((mode == "xbox" || mode == "wheel") && _fellBack && Mode == "ps4" && has) return Mode;
            if (mode == "ps4") _fellBack = false;

            DisposePad();
            _du = _dd = _dl = _dr = false;
            ClearVisualInputs();

            if (mode == "xbox" || mode == "wheel")
            {
                if (TryCreateXbox()) { Mode = mode; }
                else if (TryCreatePs4()) { Mode = "ps4"; _fellBack = true; }
            }
            else
            {
                if (TryCreatePs4()) Mode = "ps4";
            }

            if (Mode == null) Console.WriteLine($"  player {N}: no gamepad available");
            else Console.WriteLine($"  player {N} -> {Mode}{(Mode != mode ? " (fallback from xbox)" : "")}");
            return Mode;
        }
    }

    private bool TryCreateXbox()
    {
        try
        {
            var pad = _client.CreateXbox360Controller();
            pad.AutoSubmitReport = false;
            pad.FeedbackReceived += OnXboxFeedback;     // لرزش بازی -> گوشی
            pad.Connect();
            _x = pad;
            return true;
        }
        catch { return false; }
    }

    private bool TryCreatePs4()
    {
        try
        {
            var pad = _client.CreateDualShock4Controller();
            pad.AutoSubmitReport = false;
#pragma warning disable CS0618   // رویداد DS4 در ViGEm قدیمی (deprecated) علامت خورده ولی هنوز کار میکنه
            pad.FeedbackReceived += OnDs4Feedback;      // لرزش بازی -> گوشی
#pragma warning restore CS0618
            pad.Connect();
            _d = pad;
            StartDs4Pump(pad);      // خوندن گزارش خروجی خام (لرزش) از بازی
            return true;
        }
        catch { return false; }
    }

    // ---------- لرزش (Rumble): بازی -> ViGEm -> گوشی ----------
    // فرمت پیام: "rum <موتور سنگین ۰-۲۵۵> <موتور سبک ۰-۲۵۵>"  ("rum 0 0" = قطع)
    // این هندلرها روی ترد خود ViGEm صدا زده میشن؛ عمداً _lock رو نمیگیرن تا با Dispose گیر نکنن.
    private readonly object _rumLock = new();
    private int _rumSent;   // آخرین مقدار فرستاده‌شده (بزرگ<<8 | کوچک)؛ ۰ = خاموش

    private void OnXboxFeedback(object sender, Xbox360FeedbackReceivedEventArgs e)
    {
        PushRumble(e.LargeMotor, e.SmallMotor);
    }

    private void OnDs4Feedback(object sender, DualShock4FeedbackReceivedEventArgs e)
    {
        if (_rawActive) return;     // مسیر خام فعاله؛ رویداد قدیمی (deprecated) رو نادیده بگیر
        PushRumble(e.LargeMotor, e.SmallMotor);
    }

    // ---- DS4: خوندن گزارش خروجی خام (AwaitRawOutputReport) ----
    // رویداد FeedbackReceived برای DS4 قدیمی شده و ممکنه لرزش بازی رو نرسونه.
    // گزارش HID خروجی (report 0x05):  [0]=0x05  [1]=flags(bit0=لرزش)  [4]=موتور سبک  [5]=موتور سنگین  [6..8]=RGB
    // نیاز به ViGEmBus >= 1.19 داره؛ اگه نبود، خودکار به رویداد قدیمی برمیگرده.
    private sealed class Ds4Pump { public volatile bool Stop; }
    private Ds4Pump? _pump;
    private volatile bool _rawActive;

    private void StartDs4Pump(IDualShock4Controller pad)
    {
        var pump = new Ds4Pump();
        _pump = pump;
        _rawActive = false;
        var th = new Thread(() => Ds4PumpLoop(pad, pump)) { IsBackground = true, Name = "ds4-rumble-" + N };
        th.Start();
    }

    private void Ds4PumpLoop(IDualShock4Controller pad, Ds4Pump pump)
    {
        RumbleLog.Write($"  player {N}: DS4 rumble listener started");
        int fails = 0;
        while (!pump.Stop)
        {
            try
            {
                var report = pad.AwaitRawOutputReport(250, out bool timedOut);
                if (timedOut) continue;
                fails = 0;

                byte[] b = report.ToArray();
                if (b.Length < 6) continue;

                byte small, large;
                if (b[0] == 0x05)
                {
                    if ((b[1] & 0x01) == 0) continue;   // این گزارش لرزش نداره (مثلاً فقط رنگ لایت‌بار)
                    small = b[4]; large = b[5];
                }
                else { small = b[3]; large = b[4]; }    // اگه report ID حذف شده باشه

                _rawActive = true;
                PushRumble(large, small);
            }
            catch (Exception ex)
            {
                if (pump.Stop) break;
                if (++fails >= 3)
                {
                    RumbleLog.Write($"  player {N}: raw DS4 rumble unavailable ({ex.GetType().Name}); using legacy event");
                    break;
                }
                Thread.Sleep(500);
            }
        }
    }

    private bool _rumLogOn;   // برای لاگ کنسول: فقط وقتی لرزش شروع/تموم میشه چاپ کن

    /// <summary>تست دستی (از دکمه‌ی تستر): مثل بازی به گوشیِ این پلیر دستور لرزش میفرسته</summary>
    public void TestRumble(byte large, byte small) => PushRumble(large, small, true);

    private void PushRumble(byte large, byte small, bool test = false)
    {
        var owner = Owner;
        bool on = large != 0 || small != 0;
        if (on != _rumLogOn)
        {
            _rumLogOn = on;
            string note = owner == null ? " - no phone connected" : "";
            string src = test ? "test rumble" : "game rumble";
            if (on) RumbleLog.Write($"  player {N}: {src} ON  (large={large}, small={small}){note}");
            else RumbleLog.Write($"  player {N}: {src} OFF");
        }
        if (owner == null) return;
        lock (_rumLock)
        {
            int v = (large << 8) | small;
            if (v == _rumSent) return;      // تکراری؛ نفرست
            _rumSent = v;
        }
        _ = owner.SendAsync("rum " + large + " " + small);
    }

    private void StopRumble()
    {
        var owner = Owner;
        int prev;
        lock (_rumLock) { prev = _rumSent; _rumSent = 0; }
        if (owner != null && prev != 0) _ = owner.SendAsync("rum 0 0");
    }

    private void DisposePad()
    {
        if (_pump != null) { _pump.Stop = true; _pump = null; }
        try
        {
            if (_x != null) { _x.FeedbackReceived -= OnXboxFeedback; _x.ResetReport(); _x.SubmitReport(); _x.Disconnect(); (_x as IDisposable)?.Dispose(); }
        }
        catch { }
        try
        {
#pragma warning disable CS0618
            if (_d != null) { _d.FeedbackReceived -= OnDs4Feedback; _d.ResetReport(); _d.SubmitReport(); _d.Disconnect(); (_d as IDisposable)?.Dispose(); }
#pragma warning restore CS0618
        }
        catch { }
        _x = null;
        _d = null;
        Mode = null;
    }

    public void Button(string key, bool on)
    {
        lock (_lock)
        {
            switch (key)
            {
                case "A": _bA = on; break;
                case "B": _bB = on; break;
                case "X": _bX = on; break;
                case "Y": _bY = on; break;
                case "LB": _bLB = on; break;
                case "RB": _bRB = on; break;
                case "LT": _bLT = on; _lt8 = on ? (byte)255 : (byte)0; break;
                case "RT": _bRT = on; _rt8 = on ? (byte)255 : (byte)0; break;
                case "BACK": _bBack = on; break;
                case "START": _bStart = on; break;
                case "GUIDE": _bGuide = on; break;
                case "TP": _bTp = on; break;
                case "L3": _bL3 = on; break;
                case "R3": _bR3 = on; break;
                case "DU": _du = on; break;
                case "DD": _dd = on; break;
                case "DL": _dl = on; break;
                case "DR": _dr = on; break;
            }

            if (_x != null)
            {
                if (key == "LT") _x.SetSliderValue(Xbox360Slider.LeftTrigger, on ? (byte)255 : (byte)0);
                else if (key == "RT") _x.SetSliderValue(Xbox360Slider.RightTrigger, on ? (byte)255 : (byte)0);
                else if (XboxButtons.TryGetValue(key, out var b)) _x.SetButtonState(b, on);
                return;
            }

            var d = _d;
            if (d == null) return;

            switch (key)
            {
                case "LT":
                    d.SetSliderValue(DualShock4Slider.LeftTrigger, on ? (byte)255 : (byte)0);
                    d.SetButtonState(DualShock4Button.TriggerLeft, on);
                    break;
                case "RT":
                    d.SetSliderValue(DualShock4Slider.RightTrigger, on ? (byte)255 : (byte)0);
                    d.SetButtonState(DualShock4Button.TriggerRight, on);
                    break;
                case "GUIDE":
                    d.SetButtonState(DualShock4SpecialButton.Ps, on);
                    break;
                case "TP":
                    d.SetButtonState(DualShock4SpecialButton.Touchpad, on);
                    break;
                case "DU":
                case "DD":
                case "DL":
                case "DR":
                    ApplyDpad(d);
                    break;
                default:
                    if (Ds4Buttons.TryGetValue(key, out var b)) d.SetButtonState(b, on);
                    break;
            }
        }
    }

    private DualShock4DPadDirection Ds4Dir()
    {
        int v = (_du ? 1 : 0) - (_dd ? 1 : 0);
        int h = (_dr ? 1 : 0) - (_dl ? 1 : 0);
        return (v, h) switch
        {
            (1, 0) => DualShock4DPadDirection.North,
            (1, 1) => DualShock4DPadDirection.Northeast,
            (0, 1) => DualShock4DPadDirection.East,
            (-1, 1) => DualShock4DPadDirection.Southeast,
            (-1, 0) => DualShock4DPadDirection.South,
            (-1, -1) => DualShock4DPadDirection.Southwest,
            (0, -1) => DualShock4DPadDirection.West,
            (1, -1) => DualShock4DPadDirection.Northwest,
            _ => DualShock4DPadDirection.None,
        };
    }

    private void ApplyDpad(IDualShock4Controller d) => d.SetDPadDirection(Ds4Dir());

    public void Stick(char ch, float x, float y)
    {
        lock (_lock)
        {
            bool left = ch == 'l';
            if (left) { _lx = Clamp1(x); _ly = Clamp1(y); }
            else { _rx = Clamp1(x); _ry = Clamp1(y); }

            if (_x != null)
            {
                _x.SetAxisValue(left ? Xbox360Axis.LeftThumbX : Xbox360Axis.RightThumbX, ToShort(x));
                _x.SetAxisValue(left ? Xbox360Axis.LeftThumbY : Xbox360Axis.RightThumbY, ToShort(y));
            }
            else if (_d != null)
            {
                _d.SetAxisValue(left ? DualShock4Axis.LeftThumbX : DualShock4Axis.RightThumbX, ToByte(x));
                _d.SetAxisValue(left ? DualShock4Axis.LeftThumbY : DualShock4Axis.RightThumbY, ToByte(-y));
            }
        }
    }

    /// <summary>پدال آنالوگ (حالت فرمون): "tr l|r مقدار" — مقدار ۰ تا ۱؛ l = ترمز (ماشه‌ی چپ)، r = گاز (ماشه‌ی راست)</summary>
    public void Trigger(char ch, float v)
    {
        if (!Ok(v)) return;
        lock (_lock)
        {
            bool left = ch == 'l';
            byte b = (byte)Math.Round((v < 0f ? 0f : (v > 1f ? 1f : v)) * 255f);
            bool on = b > 12;                       // برای نمایش تو تستر و دکمه‌ی دیجیتال ماشه‌ی PS4
            if (left) { _lt8 = b; _bLT = on; } else { _rt8 = b; _bRT = on; }

            if (_x != null)
            {
                _x.SetSliderValue(left ? Xbox360Slider.LeftTrigger : Xbox360Slider.RightTrigger, b);
                return;
            }

            var d = _d;
            if (d == null) return;
            d.SetSliderValue(left ? DualShock4Slider.LeftTrigger : DualShock4Slider.RightTrigger, b);
            d.SetButtonState(left ? DualShock4Button.TriggerLeft : DualShock4Button.TriggerRight, on);
        }
    }

    private static float Clamp1(float v) => v < -1f ? -1f : (v > 1f ? 1f : v);
    private static short ToShort(float v) => (short)Math.Round(Clamp1(v) * 32767f);
    private static byte ToByte(float v) => (byte)Math.Round((Clamp1(v) + 1f) * 127.5f);

    public void Submit()
    {
        lock (_lock)
        {
            try
            {
                _x?.SubmitReport();
                var d = _d;
                if (d != null)
                {
                    if (_gyroLive && !_rawUnsupported) SubmitDs4Raw(d);
                    else d.SubmitReport();
                }
            }
            catch { }
        }
    }

    // ---------- ژیروسکوپ / شتاب‌سنج (فقط دسته‌ی PS4) ----------
    // پروتکل (از گوشی):  "gyr 1|0"  و  "gy gx gy gz ax ay az"
    //   gx,gy,gz = سرعت چرخش (درجه بر ثانیه) دور محورهای صفحه‌ی گوشی: x = راستِ صفحه، y = بالای صفحه، z = عمود بر صفحه (سمت کاربر)
    //   ax,ay,az = شتاب با گرانش (m/s²) روی همون محورها
    // گوشی وقتی مثل دسته (افقی، صفحه رو به کاربر) گرفته میشه با محورهای DualShock 4 اینطور جور درمیاد:
    //   DS4.X = x   |   DS4.Y (رو به بالای دسته) = z   |   DS4.Z (رو به بازیکن) = -y
    // اگه تو بازی یه محور برعکس بود فقط علامت‌های پایین رو عوض کن.
    private const float GyroSignX = 1f, GyroSignY = 1f, GyroSignZ = 1f;
    private const float Ds4GyroLsbPerDps = 16f;       // ±2048 °/s  <->  ±32767
    private const float Ds4AccelLsbPerG = 8192f;      // ±4 g
    private const float Gravity = 9.80665f;
    private const int GyroStaleMs = 250;              // اگه این‌قدر نمونه‌ای نیومد، سرعت چرخش صفر حساب میشه (که دسته نچرخه)

    private bool _gyroLive;                           // گوشی ژیروسکوپ رو روشن کرده و نمونه فرستاده
    private bool _rawUnsupported;                     // ViGEmBus ریپورت خام DS4 رو قبول نکرد (درایور قدیمی)
    private int _rawFails;
    private float _gx, _gy, _gz, _ax, _ay, _az;       // آخرین نمونه (محورهای گوشی)
    private long _gyTs, _gyCount;
    private long _hzT0; private int _hzN; private float _hz;
    private byte _pkt;
    private readonly byte[] _raw = new byte[63];
    private static readonly long Clock0 = Stopwatch.GetTimestamp();

    private static int AgeMs(long ts) => (int)Math.Min(int.MaxValue, (Stopwatch.GetTimestamp() - ts) * 1000L / Stopwatch.Frequency);
    private static bool Ok(float v) => !float.IsNaN(v) && !float.IsInfinity(v);
    private static float Lim(float v, float m) => v < -m ? -m : (v > m ? m : v);

    public void Motion(float gx, float gy, float gz, float ax, float ay, float az)
    {
        if (!Ok(gx) || !Ok(gy) || !Ok(gz) || !Ok(ax) || !Ok(ay) || !Ok(az)) return;
        lock (_lock)
        {
            _gx = Lim(gx, 2000f); _gy = Lim(gy, 2000f); _gz = Lim(gz, 2000f);
            _ax = Lim(ax, 78f); _ay = Lim(ay, 78f); _az = Lim(az, 78f);
            long now = Stopwatch.GetTimestamp();
            _gyTs = now; _gyCount++; _gyroLive = true;

            if (_hzT0 == 0) { _hzT0 = now; _hzN = 0; }
            _hzN++;
            double dt = (now - _hzT0) / (double)Stopwatch.Frequency;
            if (dt >= 0.5) { _hz = (float)(_hzN / dt); _hzT0 = now; _hzN = 0; }
        }
    }

    /// <summary>گوشی ژیروسکوپ رو خاموش کرد (یا قطع شد)</summary>
    public void StopMotion()
    {
        lock (_lock)
        {
            ResetMotion();
            // یه ریپورت عادی بفرست تا مقدار ژیروسکوپ تو بازی صفر بشه
            try { _d?.SubmitReport(); } catch { }
        }
    }

    private void ResetMotion()
    {
        _gyroLive = false;
        _gx = _gy = _gz = _ax = _ay = _az = 0f;
        _hz = 0f; _hzT0 = 0; _hzN = 0;
    }

    /// <summary>هر ثانیه از Hub.Tick: اگه گوشی وسط چرخش ساکت شد (مثلاً صفحه بسته شد) سرعت چرخش رو صفر میکنه</summary>
    public void GyroWatchdog()
    {
        lock (_lock)
        {
            if (!_gyroLive || _d == null || _rawUnsupported) return;
            if (AgeMs(_gyTs) < GyroStaleMs) return;
            if (_gx == 0f && _gy == 0f && _gz == 0f) return;
            _gx = _gy = _gz = 0f;
            try { SubmitDs4Raw(_d); } catch { }
        }
    }

    private static short Q(float v, float scale)
    {
        float t = v * scale;
        if (t > 32767f) t = 32767f; else if (t < -32768f) t = -32768f;
        return (short)Math.Round(t);
    }

    private static void Put16(byte[] b, int i, short v) { b[i] = (byte)(v & 0xFF); b[i + 1] = (byte)((v >> 8) & 0xFF); }

    /// <summary>کل ریپورت ورودی DS4 (۶۳ بایت، فرمت ViGEm DS4_REPORT_EX) رو از وضعیت فعلی میسازه و با ژیروسکوپ میفرسته. باید زیر _lock صدا زده بشه.</summary>
    private void SubmitDs4Raw(IDualShock4Controller d)
    {
        var b = _raw;
        Array.Clear(b, 0, b.Length);

        b[0] = ToByte(_lx); b[1] = ToByte(-_ly); b[2] = ToByte(_rx); b[3] = ToByte(-_ry);

        ushort btn = Ds4Dir().Value;
        if (_bA) btn |= DualShock4Button.Cross.Value;
        if (_bB) btn |= DualShock4Button.Circle.Value;
        if (_bX) btn |= DualShock4Button.Square.Value;
        if (_bY) btn |= DualShock4Button.Triangle.Value;
        if (_bLB) btn |= DualShock4Button.ShoulderLeft.Value;
        if (_bRB) btn |= DualShock4Button.ShoulderRight.Value;
        if (_bLT) btn |= DualShock4Button.TriggerLeft.Value;
        if (_bRT) btn |= DualShock4Button.TriggerRight.Value;
        if (_bBack) btn |= DualShock4Button.Share.Value;
        if (_bStart) btn |= DualShock4Button.Options.Value;
        if (_bL3) btn |= DualShock4Button.ThumbLeft.Value;
        if (_bR3) btn |= DualShock4Button.ThumbRight.Value;
        b[4] = (byte)(btn & 0xFF); b[5] = (byte)(btn >> 8);

        _pkt = (byte)((_pkt + 1) & 0x3F);
        byte sp = (byte)(_pkt << 2);
        if (_bGuide) sp |= (byte)DualShock4SpecialButton.Ps.Value;
        if (_bTp) sp |= (byte)DualShock4SpecialButton.Touchpad.Value;
        b[6] = sp;

        b[7] = _lt8;
        b[8] = _rt8;

        // زمان (واحد ۵.۳۳ میکروثانیه)؛ بعضی بازی‌ها/Steam برای انتگرال گرفتن از ژیروسکوپ ازش استفاده میکنن
        double us = (Stopwatch.GetTimestamp() - Clock0) * 1e6 / Stopwatch.Frequency;
        ushort ts = unchecked((ushort)(long)(us / 5.333));
        b[9] = (byte)(ts & 0xFF); b[10] = (byte)(ts >> 8);

        bool stale = AgeMs(_gyTs) >= GyroStaleMs;
        float gx = stale ? 0f : _gx, gy = stale ? 0f : _gy, gz = stale ? 0f : _gz;
        Put16(b, 12, Q(GyroSignX * gx, Ds4GyroLsbPerDps));        // pitch  (دور X)
        Put16(b, 14, Q(GyroSignY * gz, Ds4GyroLsbPerDps));        // yaw    (دور Y دسته = عمود بر صفحه‌ی گوشی)
        Put16(b, 16, Q(GyroSignZ * -gy, Ds4GyroLsbPerDps));       // roll   (دور Z دسته = بالای صفحه‌ی گوشی)
        float k = Ds4AccelLsbPerG / Gravity;
        Put16(b, 18, Q(_ax, k));
        Put16(b, 20, Q(_az, k));
        Put16(b, 22, Q(-_ay, k));

        b[29] = 0x1B;                                              // کابل وصل + باتری پر
        b[32] = 1;                                                 // یه بسته‌ی لمسی؛ هیچ انگشتی روی تاچ‌پد نیست (بیت ۷ = بالا)
        b[33] = _pkt;
        b[34] = 0x80; b[38] = 0x80; b[43] = 0x80; b[47] = 0x80; b[52] = 0x80; b[56] = 0x80;

        try
        {
            d.SubmitRawReport(b);
            _rawFails = 0;
        }
        catch (Exception ex)
        {
            // ViGEmBus قدیمی (مثلاً 1.16 تو ویندوز 7) ریپورت خام رو قبول نمیکنه؛ بعد از چند بار شکست ژیروسکوپ رو کنار میذاریم
            if (++_rawFails >= 3)
            {
                _rawUnsupported = true;
                Console.WriteLine($"  player {N}: gyro unavailable (raw DS4 report rejected: {ex.GetType().Name})");
            }
            d.SubmitReport();
        }
    }

    public void Release()
    {
        if (InputInjector.IsInputMode(Mode)) { InputInjector.ReleaseAll(); lock (_lock) Mode = null; }      // گوشی ماوس/کیبورد رفت
        StopRumble();
        lock (_lock)
        {
            _du = _dd = _dl = _dr = false;
            ClearVisualInputs();
            ResetMotion();
            try
            {
                _x?.ResetReport(); _x?.SubmitReport();
                _d?.ResetReport(); _d?.SubmitReport();
            }
            catch { }
        }
    }

    public void RemovePad()
    {
        lock (_lock)
        {
            DisposePad();
            _fellBack = false;
            _du = _dd = _dl = _dr = false;
            ClearVisualInputs();
            IdleSince = null;
            Console.WriteLine($"  player {N}: virtual pad removed");
        }
    }

    public void Dispose()
    {
        lock (_lock) DisposePad();
    }
}


/// <summary>لاگ لرزش: چون برنامه WinExe هست و کنسول نداره، تو فایل %AppData%\\GamepadServer\\rumble.log هم نوشته میشه (حداکثر ۴۰۰ خط تو هر اجرا)</summary>
internal static class RumbleLog
{
    private static int _n;
    private static readonly object _l = new();

    public static string FilePath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GamepadServer", "rumble.log");

    public static void Write(string line)
    {
        try
        {
            Console.WriteLine(line);
            if (Interlocked.Increment(ref _n) > 400) return;
            lock (_l)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
                File.AppendAllText(FilePath, DateTime.Now.ToString("HH:mm:ss.fff") + " " + line + "\r\n");
            }
        }
        catch { }
    }
}
