// HSH Gamepad | Telegram: @HSHGamepad
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace GamepadServer;

/// <summary>وضعیت لحظه‌ای دسته برای نمایش زنده در پنجره‌ی تستر</summary>
public struct PadState
{
    public int N;
    public bool Connected;
    public string Mode;     // "ps4" ، "xbox" ، "wheel" (فرمون) ، "keyboard" یا "mouse"
    public bool FellBack;   // آیا از Xbox به PS4 تغییر خودکار داشته؟
    public string Name;
    public string Device;
    public string Ip;

    public bool A, B, X, Y;
    public bool LB, RB, LT, RT;
    public bool BACK, START, GUIDE, TP;
    public bool L3, R3;
    public bool DU, DD, DL, DR;
    public float LX, LY, RX, RY;
    public float LTV, RTV;          // مقدار آنالوگ ماشه‌ها ۰..۱ (حالت فرمون: LTV = پدال ترمز ، RTV = پدال گاز)

    // ژیروسکوپ گوشی (برای لاگ زنده‌ی تستر)
    public int GyroState;           // 0 = خاموش/داده‌ای نمیاد، 1 = داره به دسته‌ی PS4 میره، 2 = دسته Xbox هست (ژیروسکوپ نداره)، 3 = درایور ViGEmBus ریپورت خام رو قبول نمیکنه
    public float GyroHz;            // نرخ نمونه‌ها (تعداد در ثانیه)
    public long GyroCount;          // تعداد کل نمونه‌های رسیده
    public float GX, GY, GZ;        // سرعت چرخش °/s (محورهای صفحه‌ی گوشی: x راست، y بالا، z سمت کاربر)
    public float AX, AY, AZ;        // شتاب m/s²
}

/// <summary>پنجره‌ی گرافیکی سه‌بعدی و وکتور تست دسته (PS4 DualShock 4 / Xbox) با نورپردازی حجمی و حس لمس واقعی در GDI+</summary>
public sealed class GamepadTesterForm : Form
{
    // رنگ‌های تم اصلی برنامه (MainForm) و استودیوی سه‌بعدی
    private static readonly Color cBg = Color.FromArgb(10, 14, 21);
    private static readonly Color cPanel = Color.FromArgb(18, 23, 34);
    private static readonly Color cCell = Color.FromArgb(24, 28, 41);
    private static readonly Color cOwned = Color.FromArgb(27, 56, 44);
    private static readonly Color cFg = Color.FromArgb(234, 238, 248);
    private static readonly Color cMuted = Color.FromArgb(138, 150, 176);
    private static readonly Color cGrid = Color.FromArgb(34, 42, 60);

    // رنگ‌های پایه و متریال سه‌بعدی بدنه و خطوط وکتور
    private static readonly Color cPadBody = Color.FromArgb(22, 28, 42);
    private static readonly Color cPadGrip = Color.FromArgb(14, 18, 28);
    private static readonly Color cPadBorder = Color.FromArgb(156, 172, 198);
    private static readonly Color cBtnIdle = Color.FromArgb(26, 33, 48);
    private static readonly Color cVecOuterWhite = Color.FromArgb(232, 238, 250);
    private static readonly Color cVecLine = Color.FromArgb(128, 144, 170);
    private static readonly Color cVecSubtle = Color.FromArgb(84, 98, 122);
    private static readonly Color cBadgeIdle = Color.FromArgb(234, 238, 246);
    private static readonly Color cBadgeDarkText = Color.FromArgb(14, 18, 26);

    // رنگ‌های جواهری و روانشناختی دکمه‌های Xbox (درخشش نئونی و کریستالی)
    private static readonly Color cXboxY = Color.FromArgb(255, 204, 68);
    private static readonly Color cXboxB = Color.FromArgb(255, 92, 100);
    private static readonly Color cXboxA = Color.FromArgb(79, 224, 116);
    private static readonly Color cXboxX = Color.FromArgb(78, 168, 255);

    // رنگ‌های نمادهای هندسی DualShock 4 (مطابق تصویر دوم با درخشش کریستالی)
    private static readonly Color cPsTriangle = Color.FromArgb(68, 212, 188);
    private static readonly Color cPsCircle = Color.FromArgb(242, 104, 108);
    private static readonly Color cPsCross = Color.FromArgb(108, 162, 242);
    private static readonly Color cPsSquare = Color.FromArgb(222, 136, 208);

    // رنگ‌های اختصاصی هر پلیر (دقیقاً مطابق index.html در گوشی)
    private static readonly Color[] PlayerAccents = new[]
    {
        Color.FromArgb(91, 146, 255),   // P1 آبی
        Color.FromArgb(239, 90, 90),    // P2 قرمز
        Color.FromArgb(79, 207, 106),   // P3 سبز
        Color.FromArgb(229, 139, 208),  // P4 صورتی
        Color.FromArgb(166, 120, 255),  // P5 بنفش
        Color.FromArgb(255, 159, 67),   // P6 نارنجی
        Color.FromArgb(72, 219, 251),   // P7 فیروزه‌ای
        Color.FromArgb(242, 196, 65),   // P8 زرد
        Color.FromArgb(200, 204, 216),  // P9 نقره‌ای
    };

    private static readonly Color cXboxGreen = Color.FromArgb(79, 207, 106);

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

    private readonly Hub _hub;
    private readonly int _playerN;
    private readonly float _k;
    private readonly System.Windows.Forms.Timer _timer;
    private PadState _s;
    private readonly Button _btnRumble;                      // دکمه‌ی «تست لرزش» بالای پنجره
    private readonly System.Windows.Forms.Timer _rumTestTimer;   // بعد از ~۱.۲ ثانیه لرزش رو قطع میکنه
    private bool _rumTesting;

    private float S(float px) => px * _k;
    private int Si(int px) => (int)Math.Round(px * _k);

    public GamepadTesterForm(Hub hub, int playerN, Icon? appIcon)
    {
        _hub = hub;
        _playerN = playerN;
        _k = DeviceDpi / 96f;

        SetStyle(ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.UserPaint |
                 ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.ResizeRedraw, true);
        DoubleBuffered = true;

        if (appIcon != null) Icon = appIcon;
        BackColor = cBg;
        ForeColor = cFg;
        Font = new Font("Segoe UI", 9.5f);
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(Si(780), Si(560));
        MinimumSize = new Size(Si(680), Si(490));

        _s = _hub.GetPadState(_playerN);
        UpdateWindowTitle();

        _btnRumble = new Button
        {
            Text = "تست لرزش",
            FlatStyle = FlatStyle.Flat,
            UseVisualStyleBackColor = false,
            BackColor = cCell,
            ForeColor = cFg,
            Font = new Font("Segoe UI Semibold", 9f),
            Cursor = Cursors.Hand,
            TabStop = false,
        };
        _btnRumble.FlatAppearance.BorderColor = cPadBorder;
        _btnRumble.FlatAppearance.MouseOverBackColor = Color.FromArgb(40, 48, 70);
        _btnRumble.FlatAppearance.MouseDownBackColor = Color.FromArgb(52, 62, 90);
        _btnRumble.Click += OnRumbleTestClick;
        Controls.Add(_btnRumble);
        LayoutTopButton();
        Resize += (_, _) => LayoutTopButton();

        _rumTestTimer = new System.Windows.Forms.Timer { Interval = 1200 };
        _rumTestTimer.Tick += (_, _) =>
        {
            _rumTestTimer.Stop();
            _hub.TestRumble(_playerN, 0, 0);
            _rumTesting = false;
            _btnRumble.Text = _btnIsClear ? "پاک کردن" : "تست لرزش";
            _btnRumble.Enabled = _btnIsClear || _s.Connected;
        };
        _btnRumble.Enabled = _s.Connected;

        _timer = new System.Windows.Forms.Timer { Interval = 16 }; // ~60 FPS
        _timer.Tick += (_, _) =>
        {
            var next = _hub.GetPadState(_playerN);
            bool modeChanged = next.Mode != _s.Mode || next.Connected != _s.Connected || next.Name != _s.Name;
            _s = next;
            PollMk();
            bool mk = IsMkMode;
            if (mk != _btnIsClear) { _btnIsClear = mk; if (!_rumTesting) _btnRumble.Text = mk ? "پاک کردن" : "تست لرزش"; }
            if (!_rumTesting) _btnRumble.Enabled = mk || next.Connected;   // بدون گوشی نمیشه تست کرد
            if (modeChanged) UpdateWindowTitle();
            Invalidate();
        };
        _timer.Start();
    }

    /// <summary>دکمه‌ی تست لرزش رو سمت چپِ نشانِ XBOX/PS4 تو نوار بالا میذاره</summary>
    private void LayoutTopButton()
    {
        int w = Si(112), h = Si(32);
        int x = ClientSize.Width - Si(208) - Si(16) - Si(10) - w;
        _btnRumble.SetBounds(Math.Max(Si(8), x), Si(12), w, h);
    }

    /// <summary>لرزش کامل (۲۵۵ / ۲۵۵) رو مثل یه بازی به گوشی میفرسته و بعد از ~۱.۲ ثانیه قطع میکنه</summary>
    private void OnRumbleTestClick(object? sender, EventArgs e)
    {
        if (IsMkMode) { ClearMkTested(); return; }   // تو حالت کیبرد/ماوس این دکمه «پاک کردن» ـه
        if (_rumTesting) return;
        if (!_hub.TestRumble(_playerN, 255, 255)) return;   // گوشی وصل نیست
        _rumTesting = true;
        _btnRumble.Enabled = false;
        _btnRumble.Text = "در حال لرزش…";
        _rumTestTimer.Start();
    }

    private void UpdateWindowTitle()
    {
        string modeTitle = _s.Mode == "xbox" ? "Xbox 360" : _s.Mode == "wheel" ? "Steering Wheel" : _s.Mode == "keyboard" ? "Keyboard" : _s.Mode == "mouse" ? "Mouse" : "PS4 (DualShock 4)";
        string who = !string.IsNullOrEmpty(_s.Name) ? _s.Name : (!string.IsNullOrEmpty(_s.Device) ? _s.Device : "");
        Text = string.IsNullOrEmpty(who)
            ? $"تستر دسته — پلیر {_playerN} ({modeTitle})"
            : $"تستر دسته — پلیر {_playerN}: {who} ({modeTitle})";
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        try
        {
            int on = 1;
            if (DwmSetWindowAttribute(Handle, 20, ref on, 4) != 0)
                DwmSetWindowAttribute(Handle, 19, ref on, 4);
            int cap = cBg.R | (cBg.G << 8) | (cBg.B << 16);
            DwmSetWindowAttribute(Handle, 35, ref cap, 4);
        }
        catch { }
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _timer.Stop();
        _timer.Dispose();
        _rumTestTimer.Stop();
        _rumTestTimer.Dispose();
        RemoveMouseHook();
        if (_rumTesting) _hub.TestRumble(_playerN, 0, 0);
        base.OnFormClosed(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        bool isWheel = _s.Mode == "wheel";
        bool isXbox = _s.Mode == "xbox" || isWheel;      // فرمون هم پشت صحنه همون دسته‌ی Xbox 360 مجازیه
        Color playerColor = PlayerAccents[(_playerN - 1) % PlayerAccents.Length];
        Color accent = isWheel ? playerColor : (isXbox ? cXboxGreen : playerColor);

        DrawTopBar(g, isXbox, accent);
        DrawGyroStrip(g);
        DrawBottomBar(g, isXbox, accent);

        float topH = S(80);
        float botH = S(54);
        float availW = ClientSize.Width - S(20);
        float availH = ClientSize.Height - topH - botH - S(12);
        float cx = ClientSize.Width / 2f;
        float cy = topH + availH / 2f + S(4);

        float scale = Math.Min(availW / 650f, availH / 410f);
        scale = Math.Max(0.55f, Math.Min(1.55f, scale));

        var state = g.Save();
        g.TranslateTransform(cx, cy);
        g.ScaleTransform(scale, scale);

        if (_s.Mode == "keyboard")
            DrawKeyboardTester(g, accent);
        else if (_s.Mode == "mouse")
            DrawMouseTester(g, accent);
        else if (isWheel)
            DrawWheelController(g, accent);
        else if (isXbox)
            DrawXboxVectorController(g, accent);
        else
            DrawPs4VectorController(g, playerColor);

        if (!_s.Connected)
            DrawDisconnectedBanner(g);

        g.Restore(state);
    }

    // ==================== نوار وضعیت بالا (با عمق و درخشش سه‌بعدی) ====================
    private void DrawTopBar(Graphics g, bool isXbox, Color accent)
    {
        var r = new RectangleF(0, 0, Math.Max(1f, ClientSize.Width), S(56));
        using (var b = new LinearGradientBrush(r, Color.FromArgb(24, 30, 44), Color.FromArgb(15, 19, 28), LinearGradientMode.Vertical))
            g.FillRectangle(b, r);
        using (var p = new Pen(cGrid, 1f))
            g.DrawLine(p, 0, r.Bottom, r.Width, r.Bottom);

        Color dotCol = _s.Connected ? Color.FromArgb(79, 207, 106) : Color.FromArgb(239, 90, 90);
        using (var glow = new SolidBrush(Color.FromArgb(65, dotCol)))
            g.FillEllipse(glow, S(12), S(18), S(20), S(20));
        using (var db = new SolidBrush(dotCol))
            g.FillEllipse(db, S(16), S(22), S(12), S(12));
        using (var spec = new SolidBrush(Color.FromArgb(160, Color.White)))
            g.FillEllipse(spec, S(18.5f), S(24f), S(4.5f), S(4.5f));

        string pTitle = $"پلیر {_playerN}";
        if (!string.IsNullOrEmpty(_s.Name)) pTitle += $"  •  {_s.Name}";
        else if (!string.IsNullOrEmpty(_s.Device)) pTitle += $"  •  {_s.Device}";

        using (var fTitle = new Font("Segoe UI Semibold", 11f))
        using (var fb = new SolidBrush(cFg))
            g.DrawString(pTitle, fTitle, fb, S(36), S(8));

        string subInfo = _s.Connected
            ? $"متصل  |  دستگاه: {_s.Device ?? "—"}  |  آی‌پی: {_s.Ip ?? "—"}"
            : "قطع شده (در انتظار اتصال مجدد گوشی...)";
        using (var fSub = new Font("Segoe UI", 8.5f))
        using (var sb = new SolidBrush(cMuted))
            g.DrawString(subInfo, fSub, sb, S(36), S(31));

        string badgeText = _s.Mode == "keyboard"
            ? "KEYBOARD TESTER"
            : _s.Mode == "mouse"
            ? "MOUSE TESTER"
            : _s.Mode == "wheel"
            ? "STEERING WHEEL"
            : isXbox
            ? "XBOX CONTROLLER"
            : (_s.FellBack ? "PS4 (جایگزین خودکار Xbox)" : "PS4 DUALSHOCK 4");
        float bw = S(208), bh = S(32);
        var br = new RectangleF(ClientSize.Width - bw - S(16), S(12), bw, bh);
        using (var path = RoundRect(br, S(8)))
        using (var fill = new LinearGradientBrush(br, Color.FromArgb(52, accent), Color.FromArgb(20, accent), LinearGradientMode.Vertical))
        using (var pen = new Pen(Color.FromArgb(210, accent), 1.5f))
        using (var fBadge = new Font("Segoe UI Semibold", 9f))
        using (var tb = new SolidBrush(accent))
        {
            g.FillPath(fill, path);
            g.DrawPath(pen, path);
            using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.DrawString(badgeText, fBadge, tb, br, sf);
        }
    }

    // ==================== خط لاگ ژیروسکوپ (زیر نوار بالا) ====================
    private void DrawGyroStrip(Graphics g)
    {
        var inv = System.Globalization.CultureInfo.InvariantCulture;
        string text;
        Color col = cMuted;
        if (_s.Mode == "keyboard" || _s.Mode == "mouse")
        {
            text = _s.Mode == "keyboard"
                ? string.Format(inv, "Keyboard: {0} key(s) down,  last: {1},  tested {2}/{3}", _kbHeld.Count, _kbLast, _kbTested.Count, KbTotal())
                : string.Format(inv, "Mouse: pos ({0}, {1}),  distance {2:0} px,  scroll {3:+0.#;-0.#;0}", _msPos.X, _msPos.Y, _msDist, _msWheelTotal);
            using (var kf = new Font("Consolas", 9f))
            using (var kb = new SolidBrush(_s.Connected ? Color.FromArgb(79, 207, 106) : cMuted))
                g.DrawString(text, kf, kb, S(16), S(62));
            return;
        }
        if (_s.Mode == "wheel")
        {
            // فرمون: زاویه رو خود گوشی از ژیروسکوپ حساب میکنه و فقط مقدار فرمون (محور X استیک چپ) و پدال‌ها رو میفرسته
            text = string.Format(inv, "Wheel mode: steering = left stick X ({0:+0.00;-0.00;0.00}),  gas = RT ({1:0}%),  brake = LT ({2:0}%)",
                _s.LX, _s.RTV * 100f, _s.LTV * 100f);
            using (var wf = new Font("Consolas", 9f))
            using (var wb = new SolidBrush(_s.Connected ? Color.FromArgb(79, 207, 106) : cMuted))
                g.DrawString(text, wf, wb, S(16), S(62));
            return;
        }
        switch (_s.GyroState)
        {
            case 1:
                text = string.Format(inv,
                    "Gyro: ON  {0,3:0} Hz   rot(deg/s) {1,7:+0.0;-0.0;0.0} {2,7:+0.0;-0.0;0.0} {3,7:+0.0;-0.0;0.0}   accel(m/s2) {4,6:+0.00;-0.00;0.00} {5,6:+0.00;-0.00;0.00} {6,6:+0.00;-0.00;0.00}   n={7}",
                    _s.GyroHz, _s.GX, _s.GY, _s.GZ, _s.AX, _s.AY, _s.AZ, _s.GyroCount);
                col = Color.FromArgb(79, 207, 106);
                break;
            case 2:
                text = "Gyro: phone is sending, but this player is an Xbox pad (no gyro) - use PS4 mode";
                col = Color.FromArgb(242, 196, 65);
                break;
            case 3:
                text = "Gyro: phone is sending, but ViGEmBus rejected the raw DS4 report (driver too old) - update ViGEmBus";
                col = Color.FromArgb(239, 90, 90);
                break;
            default:
                text = _s.GyroCount > 0 ? "Gyro: no data (off, or page in background)" : "Gyro: off";
                break;
        }

        using (var f = new Font("Consolas", 9f))
        using (var br = new SolidBrush(col))
            g.DrawString(text, f, br, S(16), S(62));
    }

    // ==================== نوار اطلاعات پایین ====================
    private void DrawBottomBar(Graphics g, bool isXbox, Color accent)
    {
        float h = S(52);
        var r = new RectangleF(0, ClientSize.Height - h, Math.Max(1f, ClientSize.Width), h);
        using (var b = new LinearGradientBrush(r, Color.FromArgb(16, 20, 30), Color.FromArgb(22, 28, 42), LinearGradientMode.Vertical))
            g.FillRectangle(b, r);
        using (var p = new Pen(cGrid, 1f))
            g.DrawLine(p, 0, r.Top, r.Width, r.Top);

        string sticks = string.Format(System.Globalization.CultureInfo.InvariantCulture,
            "L-Stick: ({0:+0.00;-0.00;0.00}, {1:+0.00;-0.00;0.00})    R-Stick: ({2:+0.00;-0.00;0.00}, {3:+0.00;-0.00;0.00})",
            _s.LX, _s.LY, _s.RX, _s.RY);
        if (_s.Mode == "wheel")
            sticks = string.Format(System.Globalization.CultureInfo.InvariantCulture,
                "Steering: {0:+0.00;-0.00;0.00}    Gas: {1:0}%    Brake: {2:0}%", _s.LX, _s.RTV * 100f, _s.LTV * 100f);

        if (_s.Mode == "keyboard")
            sticks = string.Format(System.Globalization.CultureInfo.InvariantCulture, "Keys down: {0}    Tested: {1}/{2}", _kbHeld.Count, _kbTested.Count, KbTotal());
        else if (_s.Mode == "mouse")
            sticks = string.Format(System.Globalization.CultureInfo.InvariantCulture, "Speed: {0:0} px/s    Distance: {1:0} px", Math.Sqrt(_msVx * _msVx + _msVy * _msVy), _msDist);

        using (var fMono = new Font("Consolas", 9.5f))
        using (var mb = new SolidBrush(cMuted))
            g.DrawString(sticks, fMono, mb, S(16), r.Top + S(17));

        var pressed = IsMkMode ? GetMkPressedList() : GetPressedList(isXbox);
        bool kbMode = _s.Mode == "keyboard";
        string btnsText = pressed.Count > 0
            ? (kbMode ? "کلیدهای فشرده شده:  " : "دکمه‌های فشرده شده:  ") + string.Join(" ، ", pressed)
            : (kbMode ? "هیچ کلیدی فشرده نشده است" : "هیچ دکمه‌ای فشرده نشده است");

        var textRect = new Rectangle(Si(390), (int)r.Top, ClientSize.Width - Si(406), (int)h);
        using (var fBtns = new Font("Segoe UI Semibold", 9.5f))
        {
            TextRenderer.DrawText(g, btnsText, fBtns, textRect,
                pressed.Count > 0 ? accent : cMuted,
                TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.RightToLeft | TextFormatFlags.EndEllipsis);
        }
    }

    // ==================== داده‌های هندسی دقیق کنترلر رسمی Xbox Wireless Controller (Series X|S / One) ====================
    private static readonly float[][] VecXboxBody =
    {
        new float[] { -82, -138 },
        new float[] { -72, -146 },
        new float[] { -36, -150, 36, -150, 72, -146 },
        new float[] { 82, -138 },
        new float[] { 124, -138, 166, -128, 194, -106 },
        new float[] { 216, -88, 232, -46, 244, 14 },
        new float[] { 252, 58, 256, 102, 248, 136 },
        new float[] { 240, 168, 210, 182, 182, 170 },
        new float[] { 156, 158, 138, 122, 118, 86 },
        new float[] { 110, 72, 96, 68, 76, 68 },
        new float[] { -76, 68 },
        new float[] { -96, 68, -110, 72, -118, 86 },
        new float[] { -138, 122, -156, 158, -182, 170 },
        new float[] { -210, 182, -240, 168, -248, 136 },
        new float[] { -256, 102, -252, 58, -244, 14 },
        new float[] { -232, -46, -216, -88, -194, -106 },
        new float[] { -166, -128, -124, -138, -82, -138 },
    };

    private static readonly float[][] VecXboxBumperZone =
    {
        new float[] { -82, -138 },
        new float[] { -90, -154, -124, -154, -158, -140 },
        new float[] { -180, -130, -192, -118, -194, -106 },
        new float[] { -166, -128, -124, -138, -82, -138 },
    };

    private static readonly float[][] VecXboxTriggerZone =
    {
        new float[] { -104, -152 },
        new float[] { -112, -176, -138, -178, -162, -164 },
        new float[] { -174, -156, -180, -142, -176, -132 },
        new float[] { -154, -144, -126, -152, -104, -152 },
    };

    private static readonly float[][] VecXboxNexusSeam =
    {
        new float[] { -194, -106 },
        new float[] { -156, -118, -116, -122, -82, -138 },
    };

    private static readonly float[][] VecXboxOuterGripSeam =
    {
        new float[] { -224, -36 },
        new float[] { -236, 18, -240, 84, -228, 148 },
    };

    private static readonly float[][] VecXboxInnerGripSeam =
    {
        new float[] { -112, 76 },
        new float[] { -130, 112, -148, 150, -174, 166 },
    };

    // ==================== داده‌های هندسی وکتور کنترلر دوم (تصویر ۲: PS4 DualShock 4) ====================
    private static readonly float[][] VecPs4Body =
    {
        new float[] { -132, -144 },
        new float[] { -92, -144 },
        new float[] { -92, -148 },
        new float[] { 92, -148 },
        new float[] { 92, -144 },
        new float[] { 132, -144 },
        new float[] { 160, -144, 188, -144, 204, -134 },
        new float[] { 220, -122, 232, -92, 238, -50 },
        new float[] { 244, -8, 250, 48, 252, 98 },
        new float[] { 254, 144, 232, 172, 198, 170 },
        new float[] { 168, 168, 152, 132, 134, 62 },
        new float[] { 130, 50 },
        new float[] { -130, 50 },
        new float[] { -134, 62 },
        new float[] { -152, 132, -168, 168, -198, 170 },
        new float[] { -232, 172, -254, 144, -252, 98 },
        new float[] { -250, 48, -244, -8, -238, -50 },
        new float[] { -232, -92, -220, -122, -204, -134 },
        new float[] { -188, -144, -160, -144, -132, -144 },
    };

    private static readonly float[][] VecPs4Bumper =
    {
        new float[] { -198, -138 },
        new float[] { -196, -154, -182, -160, -164, -160 },
        new float[] { -146, -160, -134, -154, -132, -144 },
        new float[] { -160, -144, -182, -143, -198, -138 },
    };

    private static readonly float[][] VecPs4Trigger =
    {
        new float[] { -190, -154 },
        new float[] { -188, -168, -176, -172, -162, -172 },
        new float[] { -148, -172, -138, -166, -136, -154 },
        new float[] { -152, -159, -174, -159, -190, -154 },
    };

    private static readonly float[][] VecPs4InnerGripSeam =
    {
        new float[] { -132, 54 },
        new float[] { -142, 96, -156, 146, -176, 166 },
    };

    private static readonly float[][] VecPs4OuterGripSeam =
    {
        new float[] { -235, -24 },
        new float[] { -242, 24, -246, 88, -240, 148 },
    };

    private static GraphicsPath MakePath(float[][] segs, bool mirror, bool close = true)
    {
        var p = new GraphicsPath();
        float m = mirror ? -1f : 1f;
        float cx = segs[0][0] * m, cy = segs[0][1];
        for (int i = 1; i < segs.Length; i++)
        {
            var s = segs[i];
            if (s.Length == 2)
            {
                p.AddLine(cx, cy, s[0] * m, s[1]);
                cx = s[0] * m;
                cy = s[1];
            }
            else
            {
                p.AddBezier(cx, cy, s[0] * m, s[1], s[2] * m, s[3], s[4] * m, s[5]);
                cx = s[4] * m;
                cy = s[5];
            }
        }
        if (close) p.CloseFigure();
        return p;
    }

    // ==================== موتور نورپردازی و سایه‌زنی سه‌بعدی بدنه (3D Studio Shell Engine) ====================
    private void Draw3DAmbientAndShadow(Graphics g, float[][] bodySegs, Color accent)
    {
        // ۱. هاله‌ی نوری پس‌زمینه (Ambient Backlight Aura) متناسب با رنگ پلیر
        using (var auraPath = new GraphicsPath())
        {
            auraPath.AddEllipse(-310f, -210f, 620f, 420f);
            using var pgb = new PathGradientBrush(auraPath)
            {
                CenterPoint = new PointF(0f, -15f),
                CenterColor = Color.FromArgb(_s.Connected ? 34 : 12, accent),
                SurroundColors = new[] { Color.FromArgb(0, accent) }
            };
            g.FillPath(pgb, auraPath);
        }

        // ۲. سایه‌ی حجمی چندلایه زیر دسته (Multi-Layer 3D Drop Shadow) برای ایجاد حس شناوری
        (float dy, int alpha)[] shadowLayers =
        {
            (18f, 18),
            (13f, 30),
            (8.5f, 46),
            (4f, 70)
        };
        foreach (var (dy, alpha) in shadowLayers)
        {
            using var sh = MakePath(bodySegs, mirror: false, close: true);
            using var m = new Matrix();
            m.Translate(0f, dy);
            sh.Transform(m);
            using var sb = new SolidBrush(Color.FromArgb(alpha, 2, 4, 9));
            g.FillPath(sb, sh);
        }
    }

    private void Draw3DBodyShell(Graphics g, GraphicsPath body, bool isXbox, Color accent)
    {
        // ۱. گرادیان سه‌بعدی متالیک-مات بدنه (از تاج روشن بالا تا دستگیره‌های تیره پایین)
        var bounds = new RectangleF(-260f, -185f, 520f, 375f);
        using (var bodyGrad = new LinearGradientBrush(bounds, Color.Black, Color.Black, LinearGradientMode.Vertical))
        {
            bodyGrad.InterpolationColors = new ColorBlend
            {
                Positions = new[] { 0f, 0.36f, 0.72f, 1f },
                Colors = new[]
                {
                    Color.FromArgb(48, 58, 82),   // تاج بالایی روشن زیر نور استودیو
                    Color.FromArgb(26, 33, 48),   // صفحه میانی ساتن
                    Color.FromArgb(16, 21, 32),   // بخش پایینی بدنه
                    Color.FromArgb(10, 14, 22)    // نوک پایینی دستگیره‌ها در سایه
                }
            };
            g.FillPath(bodyGrad, body);
        }

        // ۲. حجم‌پردازی ارگونومیک دستگیره‌های چپ و راست + بافت ضدلغزش (3D Grip Swells)
        var oldClip = g.Clip;
        g.SetClip(body, CombineMode.Intersect);

        float gx = isXbox ? 186f : 192f;
        float gy = isXbox ? 42f : 48f;
        foreach (float dir in new[] { -1f, 1f })
        {
            using var swellPath = new GraphicsPath();
            swellPath.AddEllipse(dir * gx - 62f, gy - 105f, 124f, 210f);
            using var pgb = new PathGradientBrush(swellPath)
            {
                CenterPoint = new PointF(dir * (gx - 8f), gy - 18f),
                CenterColor = Color.FromArgb(44, 120, 145, 185),
                SurroundColors = new[] { Color.FromArgb(0, 26, 33, 48) }
            };
            g.FillPath(pgb, swellPath);

            // شیارهای ظریف بافت گریپ (Micro-Textured Grip Ribs) روی لبه‌ی بیرونی دستگیره‌ها
            using var ribPen = new Pen(Color.FromArgb(26, 190, 210, 240), 1.1f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            for (int i = 0; i < 8; i++)
            {
                float ry = 18f + i * 12f;
                float rxOut = dir * (isXbox ? 242f : 242f);
                float rxIn = dir * (isXbox ? 224f : 224f);
                g.DrawLine(ribPen, rxIn, ry, rxOut, ry + 5f);
            }
        }

        // بازتاب ظریف نور از بالا روی تاج دسته (Top Crown Specular Glaze)
        using (var crownPath = new GraphicsPath())
        {
            crownPath.AddEllipse(-185f, -195f, 370f, 110f);
            using var crownGrad = new PathGradientBrush(crownPath)
            {
                CenterPoint = new PointF(0f, -155f),
                CenterColor = Color.FromArgb(42, 230, 240, 255),
                SurroundColors = new[] { Color.FromArgb(0, 230, 240, 255) }
            };
            g.FillPath(crownGrad, crownPath);
        }

        // لبه‌ی پخ‌خورده‌ی داخلی (3D Inner Bevel Rim)
        using (var innerBevel = new Pen(Color.FromArgb(55, 140, 168, 210), 6f) { LineJoin = LineJoin.Round })
            g.DrawPath(innerBevel, body);

        g.Clip = oldClip;
    }

    // ==================== ۱. رسم کنترلر سه‌بعدی رسمی Xbox Wireless Controller (بدون هیچ دکمه اضافه) ====================
    private void DrawXboxVectorController(Graphics g, Color accent)
    {
        // ۱. سایه و هاله‌ی نوری سه‌بعدی پشت دسته
        Draw3DAmbientAndShadow(g, VecXboxBody, accent);

        // ۲. تریگرهای سه‌بعدی LT و RT در پشت شانه
        DrawXboxShoulderZone(g, VecXboxTriggerZone, mirror: false, _s.LT, accent, isTrigger: true);
        DrawXboxShoulderZone(g, VecXboxTriggerZone, mirror: true, _s.RT, accent, isTrigger: true);

        // ۳. بامپرهای سه‌بعدی LB و RB روی شانه‌های شیب‌دار Xbox
        DrawXboxShoulderZone(g, VecXboxBumperZone, mirror: false, _s.LB, accent, isTrigger: false);
        DrawXboxShoulderZone(g, VecXboxBumperZone, mirror: true, _s.RB, accent, isTrigger: false);

        // ۴. بدنه‌ی اصلی ارگونومیک Xbox با حجم‌پردازی سه‌بعدی و درزهای واقعی بدنه
        using (var body = MakePath(VecXboxBody, mirror: false, close: true))
        {
            Draw3DBodyShell(g, body, isXbox: true, accent);

            // خطوط درز معروف یقه بالایی Xbox (Nexus Collar) و درزهای ارگونومیک دستگیره‌ها
            using (var shadowPen = new Pen(Color.FromArgb(145, 5, 8, 14), 2.4f) { LineJoin = LineJoin.Round })
            using (var nexusPen = new Pen(Color.FromArgb(205, 138, 158, 190), 1.5f) { LineJoin = LineJoin.Round })
            using (var gripPen = new Pen(Color.FromArgb(170, 104, 122, 152), 1.3f) { LineJoin = LineJoin.Round })
            {
                using (var nsL = MakePath(VecXboxNexusSeam, mirror: false, close: false)) { g.DrawPath(shadowPen, nsL); g.DrawPath(nexusPen, nsL); }
                using (var nsR = MakePath(VecXboxNexusSeam, mirror: true, close: false)) { g.DrawPath(shadowPen, nsR); g.DrawPath(nexusPen, nsR); }
                using (var ogL = MakePath(VecXboxOuterGripSeam, mirror: false, close: false)) { g.DrawPath(shadowPen, ogL); g.DrawPath(gripPen, ogL); }
                using (var ogR = MakePath(VecXboxOuterGripSeam, mirror: true, close: false)) { g.DrawPath(shadowPen, ogR); g.DrawPath(gripPen, ogR); }
                using (var igL = MakePath(VecXboxInnerGripSeam, mirror: false, close: false)) { g.DrawPath(shadowPen, igL); g.DrawPath(gripPen, igL); }
                using (var igR = MakePath(VecXboxInnerGripSeam, mirror: true, close: false)) { g.DrawPath(shadowPen, igR); g.DrawPath(gripPen, igR); }
            }

            // خط کروم/نقره‌ای دور تا دور بدنه با گرادیان نور از بالا (3D Chrome Contour)
            var rimBounds = new RectangleF(-260f, -165f, 520f, 355f);
            using (var rimBrush = new LinearGradientBrush(rimBounds, Color.FromArgb(236, 242, 252), Color.FromArgb(132, 150, 180), LinearGradientMode.Vertical))
            using (var outerPen = new Pen(rimBrush, 2.6f) { LineJoin = LineJoin.Round })
                g.DrawPath(outerPen, body);
        }

        // ۵. دکمه مرکزی رسمی Xbox Guide در بالای مرکز دسته با لوگوی کروی X
        DrawXboxNexusGuideButton(g, 0, -108, 19.5f, _s.GUIDE, accent);

        // ۶. فقط دو دکمه دایره‌ای رسمی View (BACK) و Menu (START) در مرکز دسته
        DrawXboxSystemCircleButton(g, -44, -56, 11.2f, _s.BACK, accent, isMenu: false);
        DrawXboxSystemCircleButton(g, 44, -56, 11.2f, _s.START, accent, isMenu: true);

        // ۷. چیدمان نامتقارن اصیل Xbox:
        // استیک چپ (بالا-چپ)، D-Pad هیبریدی چندوجهی (پایین-چپ)، استیک راست (پایین-راست) و دکمه‌های Y B A X (بالا-راست)
        DrawXboxVectorThumbstick(g, -134, -56, 38.5f, 30.5f, 23.5f, _s.LX, _s.LY, _s.L3, accent);
        DrawXboxHybridDpad(g, -74, 14, 37.5f, accent);
        DrawXboxVectorThumbstick(g, 74, 14, 38.5f, 30.5f, 23.5f, _s.RX, _s.RY, _s.R3, accent);
        DrawXboxVectorActionButtons(g, 134, -56, 34.5f, 17.2f);

        // ۸. ستون نشانگرهای کناری (LB / LT / BACK / L3 در چپ و RB / RT / START / R3 در راست)
        DrawXboxSideHud(g, leftSide: true, accent);
        DrawXboxSideHud(g, leftSide: false, accent);
    }

    private void DrawXboxShoulderZone(Graphics g, float[][] segs, bool mirror, bool pressed, Color accent, bool isTrigger)
    {
        if (isTrigger && !pressed) return;
        using var path = MakePath(segs, mirror, close: true);
        var b = path.GetBounds();
        if (b.Width < 1f || b.Height < 1f) return;

        if (isTrigger)
        {
            using var glow = new Pen(Color.FromArgb(95, accent), 6f) { LineJoin = LineJoin.Round };
            using var trigFill = new LinearGradientBrush(b, accent, Color.FromArgb(160, accent), LinearGradientMode.Vertical);
            using var trigPen = new Pen(Color.White, 1.9f) { LineJoin = LineJoin.Round };
            g.DrawPath(glow, path);
            g.FillPath(trigFill, path);
            g.DrawPath(trigPen, path);
            return;
        }

        if (pressed)
        {
            using var glow = new Pen(Color.FromArgb(95, accent), 6f) { LineJoin = LineJoin.Round };
            using var fill = new LinearGradientBrush(b, Color.FromArgb(245, accent), Color.FromArgb(155, accent), LinearGradientMode.Vertical);
            using var border = new Pen(Color.White, 1.9f) { LineJoin = LineJoin.Round };
            g.DrawPath(glow, path);
            g.FillPath(fill, path);
            g.DrawPath(border, path);
        }
        else
        {
            using var fill = new LinearGradientBrush(b, Color.FromArgb(52, 64, 88), Color.FromArgb(26, 33, 48), LinearGradientMode.Vertical);
            using var border = new Pen(cPadBorder, 1.6f) { LineJoin = LineJoin.Round };
            g.FillPath(fill, path);
            g.DrawPath(border, path);
        }
    }

    private void DrawXboxNexusGuideButton(Graphics g, float cx, float cy, float r, bool pressed, Color accent)
    {
        float py = pressed ? cy + 1.6f : cy;

        if (!pressed)
        {
            using var sh = new SolidBrush(Color.FromArgb(200, 3, 5, 10));
            g.FillEllipse(sh, cx - r, cy - r + 3.5f, r * 2, r * 2);
        }
        else
        {
            using var glow = new SolidBrush(Color.FromArgb(110, accent));
            g.FillEllipse(glow, cx - r - 6, py - r - 6, (r + 6) * 2, (r + 6) * 2);
        }

        var rect = new RectangleF(cx - r, py - r, r * 2, r * 2);
        using (var fill = new LinearGradientBrush(rect,
            pressed ? Color.FromArgb(255, accent) : Color.FromArgb(50, 62, 86),
            pressed ? Color.FromArgb(170, accent) : Color.FromArgb(16, 20, 30),
            LinearGradientMode.ForwardDiagonal))
        using (var outerPen = new Pen(pressed ? Color.White : cVecOuterWhite, 1.8f))
        using (var innerPen = new Pen(pressed ? Color.White : Color.FromArgb(110, accent), 1.1f))
        {
            g.FillEllipse(fill, rect);
            g.DrawEllipse(outerPen, rect);
            float ir = r - 2.6f;
            g.DrawEllipse(innerPen, cx - ir, py - ir, ir * 2, ir * 2);
        }

        // بازتاب عدسی شیشه‌ای بالای دکمه Xbox
        using (var specPen = new Pen(Color.FromArgb(pressed ? 195 : 130, Color.White), 1.6f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            g.DrawArc(specPen, cx - r + 3.2f, py - r + 2.2f, (r - 3.2f) * 2, (r - 3.2f) * 2, 200f, 140f);

        // رسم نشان کروی X رسمی Xbox با خطوط منحنی
        Color xCol = pressed ? cBadgeDarkText : accent;
        if (!pressed)
        {
            using var xGlow = new Pen(Color.FromArgb(65, accent), 5f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            g.DrawBezier(xGlow, cx - 9.5f, py - 8.5f, cx - 2f, py - 2f, cx + 3f, py + 2f, cx + 10f, py + 9.5f);
            g.DrawBezier(xGlow, cx + 9.5f, py - 8.5f, cx + 2f, py - 2f, cx - 3f, py + 2f, cx - 10f, py + 9.5f);
        }
        using (var xPen = new Pen(xCol, 2.7f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
        {
            g.DrawBezier(xPen, cx - 9.5f, py - 8.5f, cx - 2f, py - 2f, cx + 3f, py + 2f, cx + 10f, py + 9.5f);
            g.DrawBezier(xPen, cx + 9.5f, py - 8.5f, cx + 2f, py - 2f, cx - 3f, py + 2f, cx - 10f, py + 9.5f);
        }
    }

    private void DrawXboxSystemCircleButton(Graphics g, float cx, float cy, float r, bool pressed, Color accent, bool isMenu)
    {
        float py = pressed ? cy + 1.4f : cy;

        if (!pressed)
        {
            using var sh = new SolidBrush(Color.FromArgb(185, 3, 5, 10));
            g.FillEllipse(sh, cx - r, cy - r + 2.8f, r * 2, r * 2);
        }
        else
        {
            using var glow = new SolidBrush(Color.FromArgb(100, accent));
            g.FillEllipse(glow, cx - r - 4, py - r - 4, (r + 4) * 2, (r + 4) * 2);
        }

        var rect = new RectangleF(cx - r, py - r, r * 2, r * 2);
        using (var fill = new LinearGradientBrush(rect,
            pressed ? accent : Color.FromArgb(46, 56, 78),
            pressed ? Color.FromArgb(175, accent) : Color.FromArgb(18, 23, 34),
            LinearGradientMode.ForwardDiagonal))
        using (var pen = new Pen(pressed ? Color.White : cPadBorder, 1.5f))
        {
            g.FillEllipse(fill, rect);
            g.DrawEllipse(pen, rect);
        }

        Color fg = pressed ? cBadgeDarkText : cVecOuterWhite;
        using var symPen = new Pen(fg, 1.4f) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };

        if (isMenu)
        {
            // آیکون رسمی Menu (سه خط افقی)
            g.DrawLine(symPen, cx - 4.5f, py - 3.2f, cx + 4.5f, py - 3.2f);
            g.DrawLine(symPen, cx - 4.5f, py, cx + 4.5f, py);
            g.DrawLine(symPen, cx - 4.5f, py + 3.2f, cx + 4.5f, py + 3.2f);
        }
        else
        {
            // آیکون رسمی View (دو مستطیل روی هم)
            g.DrawLines(symPen, new[]
            {
                new PointF(cx - 5f, py + 1f),
                new PointF(cx - 5f, py - 4.2f),
                new PointF(cx + 1f, py - 4.2f)
            });
            g.DrawRectangle(symPen, cx - 2f, py - 1.5f, 6.5f, 5.5f);
        }
    }

    // ==================== استیک آنالوگ سه‌بعدی با گودی واقعی، میله‌ی متحرک و بافت لاستیکی ====================
    private void DrawXboxVectorThumbstick(Graphics g, float cx, float cy, float rOuter, float rMid, float rCap,
        float lx, float ly, bool pressed, Color accent)
    {
        float dx = Math.Max(-1f, Math.Min(1f, lx));
        float dy = Math.Max(-1f, Math.Min(1f, -ly));
        float len = (float)Math.Sqrt(dx * dx + dy * dy);
        if (len > 1f) { dx /= len; dy /= len; }
        bool moved = len > 0.08f;

        // ۱. حفره عمیق کروی استیک (Recessed Socket با گرادیان معکوس)
        var outerRect = new RectangleF(cx - rOuter, cy - rOuter, rOuter * 2, rOuter * 2);
        var midRect = new RectangleF(cx - rMid, cy - rMid, rMid * 2, rMid * 2);
        using (var wellGrad = new LinearGradientBrush(outerRect, Color.FromArgb(7, 9, 15), Color.FromArgb(26, 33, 48), LinearGradientMode.Vertical))
        using (var innerWellGrad = new LinearGradientBrush(midRect, Color.FromArgb(5, 7, 12), Color.FromArgb(18, 24, 36), LinearGradientMode.Vertical))
        using (var outerPen = new Pen(moved || pressed ? Color.FromArgb(190, accent) : cVecLine, 1.7f))
        using (var midPen = new Pen(cVecSubtle, 1.3f))
        {
            g.FillEllipse(wellGrad, outerRect);
            g.DrawEllipse(outerPen, outerRect);
            g.FillEllipse(innerWellGrad, midRect);
            g.DrawEllipse(midPen, midRect);
        }

        float maxTravel = 11.5f;
        float kx = cx + dx * maxTravel;
        float ky = cy + dy * maxTravel;

        // ۲. سایه‌ی متحرک کلاهک داخل حفره و میله‌ی فلزی استیک (3D Shaft & Cast Shadow)
        using (var castShadow = new SolidBrush(Color.FromArgb(185, 2, 4, 8)))
            g.FillEllipse(castShadow, kx - rCap + 2f, ky - rCap + 5.5f, rCap * 2, rCap * 2);

        if (moved)
        {
            using var shaftPen = new Pen(Color.FromArgb(180, 70, 84, 110), 7f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            g.DrawLine(shaftPen, cx, cy, kx, ky);
        }

        if (pressed)
        {
            using var glow = new SolidBrush(Color.FromArgb(105, accent));
            g.FillEllipse(glow, kx - rCap - 5, ky - rCap - 5, (rCap + 5) * 2, (rCap + 5) * 2);
        }

        // ۳. کلاهک گنبدی لاستیکی + گودی مقعر وسط شست + ۱۶ نقطه برجسته ضدلغزش
        var capRect = new RectangleF(kx - rCap, ky - rCap, rCap * 2, rCap * 2);
        float rDish = rCap - 5.5f;
        var dishRect = new RectangleF(kx - rDish, ky - rDish, rDish * 2, rDish * 2);

        Color ringCol = pressed ? Color.White : (moved ? accent : cVecOuterWhite);
        using (var capGrad = new LinearGradientBrush(capRect,
            pressed ? accent : Color.FromArgb(52, 64, 88),
            pressed ? Color.FromArgb(165, accent) : Color.FromArgb(18, 23, 34),
            LinearGradientMode.ForwardDiagonal))
        using (var dishGrad = new LinearGradientBrush(dishRect,
            pressed ? Color.FromArgb(150, accent) : Color.FromArgb(12, 16, 24),
            pressed ? accent : Color.FromArgb(38, 48, 66),
            LinearGradientMode.ForwardDiagonal))
        using (var capPen = new Pen(ringCol, pressed || moved ? 2.1f : 1.7f))
        using (var dishPen = new Pen(pressed ? cBadgeDarkText : cVecLine, 1.3f))
        {
            g.FillEllipse(capGrad, capRect);
            g.DrawEllipse(capPen, capRect);
            g.FillEllipse(dishGrad, dishRect);
            g.DrawEllipse(dishPen, dishRect);
        }

        // نقاط برجسته لاستیکی دور کلاهک (Knurled Tactile Grip Dots)
        float rKnurl = rCap - 2.7f;
        using var dotBrush = new SolidBrush(pressed ? Color.FromArgb(160, cBadgeDarkText) : Color.FromArgb(155, 195, 212, 238));
        for (int a = 0; a < 360; a += 24)
        {
            double rad = a * Math.PI / 180.0;
            float gx = kx + (float)Math.Cos(rad) * rKnurl;
            float gy = ky + (float)Math.Sin(rad) * rKnurl;
            g.FillEllipse(dotBrush, gx - 1f, gy - 1f, 2f, 2f);
        }
    }

    // ==================== D-Pad هیبریدی چندوجهی سه‌بعدی رسمی Xbox Series X|S ====================
    private void DrawXboxHybridDpad(Graphics g, float cx, float cy, float dishR, Color accent)
    {
        var dishRect = new RectangleF(cx - dishR, cy - dishR, dishR * 2, dishR * 2);
        using (var dishGrad = new LinearGradientBrush(dishRect, Color.FromArgb(8, 11, 18), Color.FromArgb(28, 36, 52), LinearGradientMode.Vertical))
        using (var dishPen = new Pen(cPadBorder, 1.6f))
        {
            g.FillEllipse(dishGrad, dishRect);
            g.DrawEllipse(dishPen, dishRect);
        }

        // خطوط چندوجهی (Faceted Octagonal Dish) داخل بشقاب D-Pad هیبریدی Xbox
        var octPts = new PointF[8];
        float rOct = dishR - 4.5f;
        for (int i = 0; i < 8; i++)
        {
            double rad = (22.5 + i * 45.0) * Math.PI / 180.0;
            octPts[i] = new PointF(cx + (float)Math.Cos(rad) * rOct, cy + (float)Math.Sin(rad) * rOct);
        }
        using (var facetPen = new Pen(Color.FromArgb(120, 88, 104, 134), 1.1f))
        {
            g.DrawPolygon(facetPen, octPts);
            for (int i = 0; i < 8; i++)
                g.DrawLine(facetPen, cx, cy, octPts[i].X, octPts[i].Y);
        }

        float w = 10.2f, L = 29f;
        // سایه زیر صلیب D-Pad
        using (var shCross = MakeRoundedCross(cx, cy + 3.2f, w, L, 3.2f))
        using (var shBrush = new SolidBrush(Color.FromArgb(195, 3, 5, 10)))
            g.FillPath(shBrush, shCross);

        var crossBounds = new RectangleF(cx - L, cy - L, L * 2, L * 2);
        using (var cross = MakeRoundedCross(cx, cy, w, L, 3.2f))
        using (var fill = new LinearGradientBrush(crossBounds, Color.FromArgb(46, 56, 78), Color.FromArgb(20, 26, 38), LinearGradientMode.Vertical))
        using (var pen = new Pen(cVecOuterWhite, 1.7f) { LineJoin = LineJoin.Round })
        {
            g.FillPath(fill, cross);
            g.DrawPath(pen, cross);
        }

        // گودی دایره‌ای ظریف در مرکز صلیب (Tactile Center Pivot)
        var pivotRect = new RectangleF(cx - 7f, cy - 7f, 14f, 14f);
        using (var pivotGrad = new LinearGradientBrush(pivotRect, Color.FromArgb(12, 16, 24), Color.FromArgb(34, 42, 58), LinearGradientMode.ForwardDiagonal))
            g.FillEllipse(pivotGrad, pivotRect);

        DrawXboxDpadArmHighlight(g, cx, cy, w, L, 0, -1, _s.DU, accent);
        DrawXboxDpadArmHighlight(g, cx, cy, w, L, 0, 1, _s.DD, accent);
        DrawXboxDpadArmHighlight(g, cx, cy, w, L, -1, 0, _s.DL, accent);
        DrawXboxDpadArmHighlight(g, cx, cy, w, L, 1, 0, _s.DR, accent);
    }

    private static GraphicsPath MakeRoundedCross(float cx, float cy, float w, float L, float r)
    {
        var p = new GraphicsPath();
        float d = r * 2f;
        p.AddArc(cx - w, cy - L, d, d, 180, 90);
        p.AddArc(cx + w - d, cy - L, d, d, 270, 90);
        p.AddLine(cx + w, cy - L + r, cx + w, cy - w);
        p.AddArc(cx + L - d, cy - w, d, d, 270, 90);
        p.AddArc(cx + L - d, cy + w - d, d, d, 0, 90);
        p.AddLine(cx + L - r, cy + w, cx + w, cy + w);
        p.AddArc(cx + w - d, cy + L - d, d, d, 0, 90);
        p.AddArc(cx - w, cy + L - d, d, d, 90, 90);
        p.AddLine(cx - w, cy + L - r, cx - w, cy + w);
        p.AddArc(cx - L, cy + w - d, d, d, 90, 90);
        p.AddArc(cx - L, cy - w, d, d, 180, 90);
        p.AddLine(cx - L + r, cy - w, cx - w, cy - w);
        p.CloseFigure();
        return p;
    }

    private void DrawXboxDpadArmHighlight(Graphics g, float cx, float cy, float w, float L, int dx, int dy, bool pressed, Color accent)
    {
        float len = L - w;
        if (pressed)
        {
            RectangleF r = dx == 0
                ? new RectangleF(cx - w + 1.2f, dy < 0 ? cy - L + 1.2f : cy + w - 1f, (w - 1.2f) * 2, len)
                : new RectangleF(dx < 0 ? cx - L + 1.2f : cx + w - 1f, cy - w + 1.2f, len, (w - 1.2f) * 2);
            using var path = RoundRect(r, 3f);
            using var glow = new Pen(Color.FromArgb(100, accent), 5f);
            using var fill = new LinearGradientBrush(r, Color.FromArgb(255, accent), Color.FromArgb(175, accent), LinearGradientMode.ForwardDiagonal);
            using var border = new Pen(Color.White, 1.4f);
            g.DrawPath(glow, path);
            g.FillPath(fill, path);
            g.DrawPath(border, path);
        }

        // خط برجسته لمسی روی هر بازوی D-Pad
        using var ridgePen = new Pen(pressed ? cBadgeDarkText : Color.FromArgb(130, cVecOuterWhite), 1.6f)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round
        };
        float rStart = w + 5f, rEnd = L - 5.5f;
        g.DrawLine(ridgePen, cx + dx * rStart, cy + dy * rStart, cx + dx * rEnd, cy + dy * rEnd);
    }

    // ==================== دکمه‌های اکشن کریستالی سه‌بعدی (3D Glassmorphic Action Buttons) ====================
    private void DrawXboxVectorActionButtons(Graphics g, float cx, float cy, float dist, float r)
    {
        DrawXboxVectorLetterBtn(g, cx, cy - dist, r, "Y", _s.Y, cXboxY);
        DrawXboxVectorLetterBtn(g, cx + dist, cy, r, "B", _s.B, cXboxB);
        DrawXboxVectorLetterBtn(g, cx, cy + dist, r, "A", _s.A, cXboxA);
        DrawXboxVectorLetterBtn(g, cx - dist, cy, r, "X", _s.X, cXboxX);
    }

    private void DrawXboxVectorLetterBtn(Graphics g, float x, float y, float r, string letter, bool pressed, Color jewelCol)
    {
        // فیزیک فشرده شدن دکمه: در حالت عادی برجسته است و با فشردن ۲ پیکسل به داخل حفره فرو می‌رود
        float py = pressed ? y + 2.0f : y;

        // ۱. حفره و سایه سه‌بعدی زیر دکمه
        using (var socketBrush = new SolidBrush(Color.FromArgb(220, 4, 6, 11)))
            g.FillEllipse(socketBrush, x - r, y - r + 3.6f, r * 2, r * 2);

        if (pressed)
        {
            using var glowOuter = new SolidBrush(Color.FromArgb(75, jewelCol));
            using var glowInner = new SolidBrush(Color.FromArgb(130, jewelCol));
            g.FillEllipse(glowOuter, x - r - 7, py - r - 7, (r + 7) * 2, (r + 7) * 2);
            g.FillEllipse(glowInner, x - r - 3, py - r - 3, (r + 3) * 2, (r + 3) * 2);
        }

        // ۲. گنبد کریستالی دکمه با گرادیان سه‌بعدی
        var btnRect = new RectangleF(x - r, py - r, r * 2, r * 2);
        using (var domeGrad = new LinearGradientBrush(btnRect,
            pressed ? Color.FromArgb(255, jewelCol) : Color.FromArgb(46, 56, 78),
            pressed ? Color.FromArgb(180, jewelCol) : Color.FromArgb(16, 20, 30),
            LinearGradientMode.ForwardDiagonal))
        using (var rimPen = new Pen(pressed ? Color.White : Color.FromArgb(215, jewelCol), pressed ? 2.0f : 1.7f))
        {
            g.FillEllipse(domeGrad, btnRect);
            g.DrawEllipse(rimPen, btnRect);
        }

        // ۳. هاله‌ی نوری زیرسطحی (Subsurface Jewel Glow) در حالت عادی
        if (!pressed)
        {
            using var subPath = new GraphicsPath();
            subPath.AddEllipse(btnRect);
            using var pgb = new PathGradientBrush(subPath)
            {
                CenterPoint = new PointF(x, py + r * 0.35f),
                CenterColor = Color.FromArgb(45, jewelCol),
                SurroundColors = new[] { Color.FromArgb(0, jewelCol) }
            };
            g.FillPath(pgb, subPath);
        }

        // ۴. بازتاب هلالی شیشه‌ای بالای عدسی دکمه (Top Specular Lens Reflection)
        using (var specPen = new Pen(Color.FromArgb(pressed ? 210 : 135, Color.White), 1.8f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            g.DrawArc(specPen, x - r + 3.2f, py - r + 2.4f, (r - 3.2f) * 2, (r - 3.2f) * 2, 200f, 140f);

        // ۵. حرف وسط دکمه با سایه ظریف
        using var f = new Font("Segoe UI", 11f, FontStyle.Bold);
        using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        if (!pressed)
        {
            using var shText = new SolidBrush(Color.FromArgb(160, 0, 0, 0));
            g.DrawString(letter, f, shText, new RectangleF(x - r, py - r + 1.8f, r * 2, r * 2), sf);
        }
        using var tb = new SolidBrush(pressed ? cBadgeDarkText : cVecOuterWhite);
        g.DrawString(letter, f, tb, new RectangleF(x - r, py - r + 0.5f, r * 2, r * 2), sf);
    }

    private void DrawXboxSideHud(Graphics g, bool leftSide, Color accent)
    {
        float x = leftSide ? -284f : 284f;
        bool b1 = leftSide ? _s.LB : _s.RB;
        bool b2 = leftSide ? _s.LT : _s.RT;
        bool bMenu = leftSide ? _s.BACK : _s.START;
        bool bStick = leftSide ? _s.L3 : _s.R3;

        if (leftSide || b1)
        {
            var hlRect = leftSide ? new RectangleF(x - 36, -86, 62, 34) : new RectangleF(x - 26, -86, 62, 34);
            using var hlPath = RoundRect(hlRect, 6f);
            using var hlFill = new SolidBrush(b1 ? Color.FromArgb(70, accent) : Color.FromArgb(42, 50, 66));
            g.FillPath(hlFill, hlPath);
        }
        DrawHudBumperBadge(g, x, -69, leftSide ? "LB" : "RB", b1, accent);
        DrawHudTriggerSquareBadge(g, x, -31, leftSide ? "LT" : "RT", b2, accent);
        DrawHudCapsuleLabelBadge(g, x, 6, leftSide ? "BACK" : "START", bMenu, accent);
        DrawHudStickIcon(g, x, 42, leftSide ? "L3" : "R3", bStick, accent);
    }

    // ==================== ۲. رسم کنترلر سه‌بعدی/وکتور منطبق با تصویر دوم (PS4 DualShock 4) ====================
    private void DrawPs4VectorController(Graphics g, Color accent)
    {
        // ۱. سایه و هاله‌ی نوری سه‌بعدی پشت دسته
        Draw3DAmbientAndShadow(g, VecPs4Body, accent);

        // ۲. تریگرهای سه‌بعدی L2 و R2 پشت شانه‌ها
        DrawPs4ShoulderPart(g, VecPs4Trigger, mirror: false, _s.LT, accent, isTrigger: true);
        DrawPs4ShoulderPart(g, VecPs4Trigger, mirror: true, _s.RT, accent, isTrigger: true);

        // ۳. بامپرهای سه‌بعدی L1 و R1 بالای شانه‌ها
        DrawPs4ShoulderPart(g, VecPs4Bumper, mirror: false, _s.LB, accent, isTrigger: false);
        DrawPs4ShoulderPart(g, VecPs4Bumper, mirror: true, _s.RB, accent, isTrigger: false);

        // ۴. بدنه‌ی اصلی سه‌بعدی DualShock 4
        using (var body = MakePath(VecPs4Body, mirror: false, close: true))
        {
            Draw3DBodyShell(g, body, isXbox: false, accent);

            var rimBounds = new RectangleF(-260f, -165f, 520f, 345f);
            using (var rimBrush = new LinearGradientBrush(rimBounds, Color.FromArgb(220, 230, 248), Color.FromArgb(118, 134, 162), LinearGradientMode.Vertical))
            using (var pen = new Pen(rimBrush, 2.1f) { LineJoin = LineJoin.Round })
                g.DrawPath(pen, body);
        }

        // ۵. خطوط درز و انحنای سه‌بعدی داخلی دستگیره‌های چپ و راست (با سایه و هایلایت پخ‌خورده)
        using (var seamShadow = new Pen(Color.FromArgb(145, 5, 8, 14), 2.4f) { LineJoin = LineJoin.Round })
        using (var seamPen = new Pen(Color.FromArgb(195, 118, 136, 165), 1.4f) { LineJoin = LineJoin.Round })
        {
            using (var igL = MakePath(VecPs4InnerGripSeam, mirror: false, close: false)) { g.DrawPath(seamShadow, igL); g.DrawPath(seamPen, igL); }
            using (var igR = MakePath(VecPs4InnerGripSeam, mirror: true, close: false)) { g.DrawPath(seamShadow, igR); g.DrawPath(seamPen, igR); }
            using (var ogL = MakePath(VecPs4OuterGripSeam, mirror: false, close: false)) { g.DrawPath(seamShadow, ogL); g.DrawPath(seamPen, ogL); }
            using (var ogR = MakePath(VecPs4OuterGripSeam, mirror: true, close: false)) { g.DrawPath(seamShadow, ogR); g.DrawPath(seamPen, ogR); }
        }

        // ۶. دو قاب دایره‌ای فرورفته‌ی سه‌بعدی چپ (D-Pad) و راست (Action Buttons) + زبانه هلالی بالای آن‌ها
        DrawPs4FaceDish(g, -168, -64, 62f);
        DrawPs4FaceDish(g, 168, -64, 62f);

        // ۷. تاچ‌پد مرکزی سه‌بعدی (با بافت نقطه‌ای ظریف، درخشش لایت‌بار و واکنش به کلید TP)
        DrawPs4VectorTouchpad(g, accent);

        // ۸. دکمه‌های کپسولی سه‌بعدی عمودی SHARE و OPTIONS کنار تاچ‌پد
        DrawPs4ShareOptionsCapsule(g, -114, -126, 14f, 30f, _s.BACK, accent);
        DrawPs4ShareOptionsCapsule(g, 114, -126, 14f, 30f, _s.START, accent);

        // ۹. سوراخ‌های اسپیکر سه‌بعدی وسط و دکمه کریستالی PS Home زیر آن
        DrawPs4SpeakerGrille(g, 0, -32);
        DrawPs4HomeButton(g, 0, 14, 14f, _s.GUIDE, accent);

        // ۱۰. D-Pad چهار تکه‌ی سه‌بعدی پلی‌استیشن با مثلث‌های جهت بیرونی (سمت چپ)
        DrawPs4SplitDpad(g, -168, -64, accent);

        // ۱۱. دکمه‌های کریستالی چهارگانه پلی‌استیشن (△ ○ ✕ □) با حلقه راهنمای میانی (سمت راست)
        DrawPs4VectorActionButtons(g, 168, -64);

        // ۱۲. دو آنالوگ استیک سه‌بعدی چپ و راست با ۴ حلقه هم‌مرکز که از پایین بدنه بیرون زده‌اند
        DrawPs4VectorThumbstick(g, -86, 12, _s.LX, _s.LY, _s.L3, accent);
        DrawPs4VectorThumbstick(g, 86, 12, _s.RX, _s.RY, _s.R3, accent);

        // ۱۳. ستون نشانگرهای سه‌بعدی سمت چپ و راست (L1 / L2 / SHARE و R1 / R2 / OPTIONS)
        DrawPs4SideHud(g, leftSide: true, accent);
        DrawPs4SideHud(g, leftSide: false, accent);
    }

    private void DrawPs4ShoulderPart(Graphics g, float[][] segs, bool mirror, bool pressed, Color accent, bool isTrigger)
    {
        if (isTrigger && !pressed) return;
        using var path = MakePath(segs, mirror, close: true);
        var b = path.GetBounds();
        if (b.Width < 1f || b.Height < 1f) return;

        if (pressed)
        {
            using var glow = new Pen(Color.FromArgb(95, accent), 5.5f) { LineJoin = LineJoin.Round };
            g.DrawPath(glow, path);
        }

        using var fill = new LinearGradientBrush(b,
            pressed ? Color.FromArgb(245, accent) : Color.FromArgb(46, 56, 78),
            pressed ? Color.FromArgb(165, accent) : Color.FromArgb(20, 25, 38),
            LinearGradientMode.Vertical);
        using var pen = new Pen(pressed ? Color.White : cPadBorder, pressed ? 1.9f : 1.5f) { LineJoin = LineJoin.Round };
        g.FillPath(fill, path);
        g.DrawPath(pen, path);
    }

    private void DrawPs4FaceDish(Graphics g, float cx, float cy, float r)
    {
        var rect = new RectangleF(cx - r, cy - r, r * 2, r * 2);
        using (var dishGrad = new LinearGradientBrush(rect, Color.FromArgb(12, 16, 25), Color.FromArgb(28, 35, 50), LinearGradientMode.Vertical))
        using (var pen = new Pen(Color.FromArgb(200, 108, 124, 152), 1.5f))
        using (var hiPen = new Pen(Color.FromArgb(75, 200, 218, 245), 1.1f))
        {
            g.FillEllipse(dishGrad, rect);
            g.DrawEllipse(pen, rect);
            g.DrawArc(hiPen, cx - r + 1.5f, cy - r + 1.5f, (r - 1.5f) * 2, (r - 1.5f) * 2, 20f, 140f);
            // هلال کوچک بالای قاب دایره‌ای مطابق تصویر دوم
            g.DrawArc(pen, cx - 20f, cy - r - 7f, 40f, 14f, 195f, 150f);
        }
    }

    private void DrawPs4VectorTouchpad(Graphics g, Color accent)
    {
        var outer = new RectangleF(-92, -148, 184, 104);
        var inner = new RectangleF(-89, -145, 178, 98);
        using var outerPath = RoundRect(outer, 8f);
        using var innerPath = RoundRect(inner, 6f);

        // سایه زیر تاچ‌پد
        var shRect = new RectangleF(-92, -145, 184, 104);
        using (var shPath = RoundRect(shRect, 8f))
        using (var shBrush = new SolidBrush(Color.FromArgb(160, 4, 6, 11)))
            g.FillPath(shBrush, shPath);

        if (_s.TP)
        {
            using var glow = new Pen(Color.FromArgb(100, accent), 6f);
            g.DrawPath(glow, outerPath);
        }

        using (var fill = new LinearGradientBrush(outer,
            _s.TP ? Color.FromArgb(165, accent) : Color.FromArgb(30, 38, 54),
            _s.TP ? Color.FromArgb(95, accent) : Color.FromArgb(15, 20, 30),
            LinearGradientMode.Vertical))
        using (var outerPen = new Pen(_s.TP ? Color.White : cPadBorder, 1.7f))
        using (var innerPen = new Pen(_s.TP ? Color.White : cVecSubtle, 1.1f))
        {
            g.FillPath(fill, outerPath);

            // بازتاب نوری لایت‌بار روی بخش بالایی تاچ‌پد
            var glowRect = new RectangleF(-76, -148, 152, 28);
            using (var lbGlow = new LinearGradientBrush(glowRect, Color.FromArgb(_s.Connected ? 70 : 20, accent), Color.FromArgb(0, accent), LinearGradientMode.Vertical))
                g.FillRectangle(lbGlow, glowRect);

            // شبکه‌ی نقاط ظریف خازنی روی سطح تاچ‌پد (Capacitive Micro-Dot Matrix)
            using (var dotBrush = new SolidBrush(Color.FromArgb(_s.TP ? 95 : 55, 150, 170, 205)))
            {
                for (float gx = -72f; gx <= 72f; gx += 12f)
                {
                    for (float gy = -130f; gy <= -58f; gy += 12f)
                        g.FillEllipse(dotBrush, gx - 0.9f, gy - 0.9f, 1.8f, 1.8f);
                }
            }

            g.DrawPath(outerPen, outerPath);
            g.DrawPath(innerPen, innerPath);
        }

        // نوار درخشان لایت‌بار بالای تاچ‌پد با هاله‌ی نئونی چندلایه
        int lbAlpha = _s.Connected ? 245 : 100;
        using (var bloom1 = new Pen(Color.FromArgb(lbAlpha / 4, accent), 7f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
        using (var bloom2 = new Pen(Color.FromArgb(lbAlpha / 2, accent), 4f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
        using (var corePen = new Pen(Color.FromArgb(lbAlpha, accent), 2.2f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
        using (var whiteCore = new Pen(Color.FromArgb(lbAlpha, Color.White), 1.0f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
        {
            g.DrawLine(bloom1, -58, -148, 58, -148);
            g.DrawLine(bloom2, -56, -148, 56, -148);
            g.DrawLine(corePen, -54, -148, 54, -148);
            g.DrawLine(whiteCore, -38, -148, 38, -148);
        }
    }

    private void DrawPs4ShareOptionsCapsule(Graphics g, float cx, float cy, float w, float h, bool pressed, Color accent)
    {
        float py = pressed ? cy + 1.2f : cy;
        var outer = new RectangleF(cx - w / 2f, py - h / 2f, w, h);
        var inner = new RectangleF(cx - (w - 4f) / 2f, py - (h - 4f) / 2f, w - 4f, h - 4f);

        if (!pressed)
        {
            var shRect = new RectangleF(outer.X, outer.Y + 2.5f, outer.Width, outer.Height);
            using var shPath = RoundRect(shRect, w / 2f);
            using var shBrush = new SolidBrush(Color.FromArgb(180, 4, 6, 11));
            g.FillPath(shBrush, shPath);
        }

        using var outerPath = RoundRect(outer, w / 2f);
        using var innerPath = RoundRect(inner, (w - 4f) / 2f);

        if (pressed)
        {
            using var glow = new Pen(Color.FromArgb(95, accent), 4.5f);
            g.DrawPath(glow, outerPath);
        }

        using var fill = new LinearGradientBrush(outer,
            pressed ? accent : Color.FromArgb(46, 56, 78),
            pressed ? Color.FromArgb(175, accent) : Color.FromArgb(18, 23, 34),
            LinearGradientMode.Horizontal);
        using var outerPen = new Pen(pressed ? Color.White : cVecLine, 1.4f);
        using var innerPen = new Pen(pressed ? cBadgeDarkText : cVecSubtle, 1.0f);
        g.FillPath(fill, outerPath);
        g.DrawPath(outerPen, outerPath);
        g.DrawPath(innerPen, innerPath);
    }

    private void DrawPs4SpeakerGrille(Graphics g, float cx, float topY)
    {
        int[] counts = { 5, 4, 2 };
        using var holeFill = new SolidBrush(Color.FromArgb(230, 5, 7, 12));
        using var rimPen = new Pen(Color.FromArgb(150, 100, 116, 142), 1.0f);
        for (int row = 0; row < counts.Length; row++)
        {
            int n = counts[row];
            float y = topY + row * 5f;
            for (int i = 0; i < n; i++)
            {
                float x = cx + (i - (n - 1) / 2f) * 6.5f;
                g.FillEllipse(holeFill, x - 1.6f, y - 1.6f, 3.2f, 3.2f);
                g.DrawEllipse(rimPen, x - 1.6f, y - 1.6f, 3.2f, 3.2f);
            }
        }
    }

    private void DrawPs4HomeButton(Graphics g, float cx, float cy, float r, bool pressed, Color accent)
    {
        float py = pressed ? cy + 1.5f : cy;

        if (!pressed)
        {
            using var sh = new SolidBrush(Color.FromArgb(200, 3, 5, 10));
            g.FillEllipse(sh, cx - r, cy - r + 3.2f, r * 2, r * 2);
        }
        else
        {
            using var glow = new SolidBrush(Color.FromArgb(110, accent));
            g.FillEllipse(glow, cx - r - 5, py - r - 5, (r + 5) * 2, (r + 5) * 2);
        }

        var rect = new RectangleF(cx - r, py - r, r * 2, r * 2);
        using (var fill = new LinearGradientBrush(rect,
            pressed ? accent : Color.FromArgb(158, 170, 192),
            pressed ? Color.FromArgb(175, accent) : Color.FromArgb(88, 98, 118),
            LinearGradientMode.ForwardDiagonal))
        using (var pen = new Pen(pressed ? Color.White : cVecOuterWhite, 1.4f))
        {
            g.FillEllipse(fill, rect);
            g.DrawEllipse(pen, rect);
        }

        using (var specPen = new Pen(Color.FromArgb(150, Color.White), 1.4f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            g.DrawArc(specPen, cx - r + 2.5f, py - r + 2f, (r - 2.5f) * 2, (r - 2.5f) * 2, 200f, 140f);

        using var fPs = new Font("Georgia", 8.5f, FontStyle.Bold | FontStyle.Italic);
        using var bPs = new SolidBrush(cBadgeDarkText);
        using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        g.DrawString("PS", fPs, bPs, new RectangleF(cx - r, py - r + 0.5f, r * 2, r * 2), sf);
    }

    private void DrawPs4SplitDpad(Graphics g, float cx, float cy, Color accent)
    {
        DrawPs4DpadPiece(g, cx, cy, 0, -1, _s.DU, accent);
        DrawPs4DpadPiece(g, cx, cy, 0, 1, _s.DD, accent);
        DrawPs4DpadPiece(g, cx, cy, -1, 0, _s.DL, accent);
        DrawPs4DpadPiece(g, cx, cy, 1, 0, _s.DR, accent);
    }

    private void DrawPs4DpadPiece(Graphics g, float cx, float cy, int dx, int dy, bool pressed, Color accent)
    {
        float px = -dy, py = dx;
        float offY = pressed ? 1.8f : 0f;

        // ۱. سایه سه‌بعدی زیر هر قطعه D-Pad
        if (!pressed)
        {
            var shPts = new[]
            {
                new PointF(cx + dx * 6.5f, cy + dy * 6.5f + 3.2f),
                new PointF(cx + dx * 18f + px * 11.5f, cy + dy * 18f + py * 11.5f + 3.2f),
                new PointF(cx + dx * 37f + px * 11.5f, cy + dy * 37f + py * 11.5f + 3.2f),
                new PointF(cx + dx * 37f - px * 11.5f, cy + dy * 37f - py * 11.5f + 3.2f),
                new PointF(cx + dx * 18f - px * 11.5f, cy + dy * 18f - py * 11.5f + 3.2f),
            };
            using var shPath = new GraphicsPath();
            shPath.AddPolygon(shPts);
            using var shBrush = new SolidBrush(Color.FromArgb(195, 3, 5, 10));
            g.FillPath(shBrush, shPath);
        }

        var pts = new[]
        {
            new PointF(cx + dx * 6.5f, cy + dy * 6.5f + offY),
            new PointF(cx + dx * 18f + px * 11.5f, cy + dy * 18f + py * 11.5f + offY),
            new PointF(cx + dx * 37f + px * 11.5f, cy + dy * 37f + py * 11.5f + offY),
            new PointF(cx + dx * 37f - px * 11.5f, cy + dy * 37f - py * 11.5f + offY),
            new PointF(cx + dx * 18f - px * 11.5f, cy + dy * 18f - py * 11.5f + offY),
        };

        using (var path = new GraphicsPath())
        {
            path.AddPolygon(pts);
            var b = path.GetBounds();
            if (pressed)
            {
                using var glow = new Pen(Color.FromArgb(100, accent), 5.5f) { LineJoin = LineJoin.Round };
                g.DrawPath(glow, path);
            }
            using var fill = new LinearGradientBrush(b,
                pressed ? Color.FromArgb(255, accent) : Color.FromArgb(46, 56, 78),
                pressed ? Color.FromArgb(175, accent) : Color.FromArgb(18, 24, 36),
                LinearGradientMode.ForwardDiagonal);
            using var pen = new Pen(pressed ? Color.White : cPadBorder, 1.6f) { LineJoin = LineJoin.Round };
            g.FillPath(fill, path);
            g.DrawPath(pen, path);
        }

        // مثلث جهت کوچک بیرون هر بازو (با درخشش هنگام فشرده شدن)
        var tri = new[]
        {
            new PointF(cx + dx * 51f, cy + dy * 51f),
            new PointF(cx + dx * 43f + px * 5.5f, cy + dy * 43f + py * 5.5f),
            new PointF(cx + dx * 43f - px * 5.5f, cy + dy * 43f - py * 5.5f),
        };
        if (pressed)
        {
            using var triFill = new SolidBrush(accent);
            g.FillPolygon(triFill, tri);
        }
        using var triPen = new Pen(pressed ? Color.White : cVecSubtle, 1.2f) { LineJoin = LineJoin.Round };
        g.DrawPolygon(triPen, tri);
    }

    private void DrawPs4VectorActionButtons(Graphics g, float cx, float cy)
    {
        float dist = 35.5f, r = 18.5f;

        // حلقه‌های راهنمای ظریف بین ۴ دکمه (در پس‌زمینه‌ی دکمه‌ها، مطابق تصویر دوم)
        using (var ringPen = new Pen(cVecSubtle, 1.2f))
        {
            g.DrawEllipse(ringPen, cx - 25f, cy - 25f, 50f, 50f);
            g.DrawEllipse(ringPen, cx - 46f, cy - 46f, 92f, 92f);
        }

        DrawPs4VectorShapeBtn(g, cx, cy - dist, r, 'Y', _s.Y, cPsTriangle); // △ بالا
        DrawPs4VectorShapeBtn(g, cx + dist, cy, r, 'B', _s.B, cPsCircle);   // ○ راست
        DrawPs4VectorShapeBtn(g, cx, cy + dist, r, 'A', _s.A, cPsCross);    // ✕ پایین
        DrawPs4VectorShapeBtn(g, cx - dist, cy, r, 'X', _s.X, cPsSquare);   // □ چپ
    }

    private void DrawPs4VectorShapeBtn(Graphics g, float x, float y, float r, char kind, bool pressed, Color symColor)
    {
        float py = pressed ? y + 2.0f : y;

        // ۱. سایه سه‌بعدی زیر دکمه
        using (var socketBrush = new SolidBrush(Color.FromArgb(220, 4, 6, 11)))
            g.FillEllipse(socketBrush, x - r, y - r + 3.6f, r * 2, r * 2);

        if (pressed)
        {
            using var glowOuter = new SolidBrush(Color.FromArgb(75, symColor));
            using var glowInner = new SolidBrush(Color.FromArgb(130, symColor));
            g.FillEllipse(glowOuter, x - r - 7, py - r - 7, (r + 7) * 2, (r + 7) * 2);
            g.FillEllipse(glowInner, x - r - 3, py - r - 3, (r + 3) * 2, (r + 3) * 2);
        }

        // ۲. گنبد کریستالی دکمه
        var btnRect = new RectangleF(x - r, py - r, r * 2, r * 2);
        using (var domeGrad = new LinearGradientBrush(btnRect,
            pressed ? Color.FromArgb(255, symColor) : Color.FromArgb(46, 56, 78),
            pressed ? Color.FromArgb(180, symColor) : Color.FromArgb(16, 20, 30),
            LinearGradientMode.ForwardDiagonal))
        using (var outerPen = new Pen(pressed ? Color.White : cPadBorder, 1.7f))
        using (var innerPen = new Pen(pressed ? Color.White : Color.FromArgb(110, symColor), 1.1f))
        {
            g.FillEllipse(domeGrad, btnRect);
            g.DrawEllipse(outerPen, btnRect);
            float ir = r - 2.5f;
            g.DrawEllipse(innerPen, x - ir, py - ir, ir * 2, ir * 2);
        }

        // ۳. بازتاب هلالی شیشه‌ای بالای دکمه
        using (var specPen = new Pen(Color.FromArgb(pressed ? 210 : 135, Color.White), 1.7f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            g.DrawArc(specPen, x - r + 3.2f, py - r + 2.4f, (r - 3.2f) * 2, (r - 3.2f) * 2, 200f, 140f);

        // ۴. نماد هندسی پلی‌استیشن با درخشش ظریف نئونی
        Color fg = pressed ? cBadgeDarkText : symColor;
        using var glowSymPen = new Pen(Color.FromArgb(pressed ? 0 : 55, symColor), 4.5f)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round,
            LineJoin = LineJoin.Round
        };
        using var symPen = new Pen(fg, 2.3f)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round,
            LineJoin = LineJoin.Round
        };

        float s = r * 0.48f;
        switch (kind)
        {
            case 'Y': // مثلث △
                var tri = new[]
                {
                    new PointF(x, py - s),
                    new PointF(x - s, py + s * 0.82f),
                    new PointF(x + s, py + s * 0.82f)
                };
                if (!pressed) g.DrawPolygon(glowSymPen, tri);
                g.DrawPolygon(symPen, tri);
                break;
            case 'B': // دایره ○
                if (!pressed) g.DrawEllipse(glowSymPen, x - s, py - s, s * 2, s * 2);
                g.DrawEllipse(symPen, x - s, py - s, s * 2, s * 2);
                break;
            case 'A': // ضربدر ✕
                if (!pressed)
                {
                    g.DrawLine(glowSymPen, x - s, py - s, x + s, py + s);
                    g.DrawLine(glowSymPen, x + s, py - s, x - s, py + s);
                }
                g.DrawLine(symPen, x - s, py - s, x + s, py + s);
                g.DrawLine(symPen, x + s, py - s, x - s, py + s);
                break;
            case 'X': // مربع □
                if (!pressed) g.DrawRectangle(glowSymPen, x - s, py - s, s * 2, s * 2);
                g.DrawRectangle(symPen, x - s, py - s, s * 2, s * 2);
                break;
        }
    }

    private void DrawPs4VectorThumbstick(Graphics g, float cx, float cy, float lx, float ly, bool pressed, Color accent)
    {
        float rHousing = 49f;
        float rWell = 35f;
        float rCap = 26f;
        float rInner = 17.5f;

        float dx = Math.Max(-1f, Math.Min(1f, lx));
        float dy = Math.Max(-1f, Math.Min(1f, -ly));
        float len = (float)Math.Sqrt(dx * dx + dy * dy);
        if (len > 1f) { dx /= len; dy /= len; }
        bool moved = len > 0.08f;

        // ۱. سایه پایینی محفظه استیک + بدنه‌ی برجسته‌ی محفظه + حفره مقعر داخلی
        using (var housingSh = new SolidBrush(Color.FromArgb(175, 3, 5, 10)))
            g.FillEllipse(housingSh, cx - rHousing, cy - rHousing + 5.5f, rHousing * 2, rHousing * 2);

        var housingRect = new RectangleF(cx - rHousing, cy - rHousing, rHousing * 2, rHousing * 2);
        var wellRect = new RectangleF(cx - rWell, cy - rWell, rWell * 2, rWell * 2);
        using (var housingGrad = new LinearGradientBrush(housingRect, Color.FromArgb(34, 42, 60), Color.FromArgb(14, 18, 28), LinearGradientMode.Vertical))
        using (var wellGrad = new LinearGradientBrush(wellRect, Color.FromArgb(6, 8, 14), Color.FromArgb(24, 30, 44), LinearGradientMode.Vertical))
        using (var housingPen = new Pen(cPadBorder, 1.6f))
        using (var wellPen = new Pen(moved || pressed ? Color.FromArgb(190, accent) : cVecLine, 1.5f))
        {
            g.FillEllipse(housingGrad, housingRect);
            g.DrawEllipse(housingPen, housingRect);
            g.DrawArc(housingPen, cx - 52f, cy - 52f, 104f, 104f, 20f, 140f);
            g.FillEllipse(wellGrad, wellRect);
            g.DrawEllipse(wellPen, wellRect);
        }

        // ۲. سایه متحرک، میله‌ی استیک و کلاهک لاستیکی سه‌بعدی با گودی وسط
        float maxTravel = 11.5f;
        float kx = cx + dx * maxTravel;
        float ky = cy + dy * maxTravel;

        using (var castShadow = new SolidBrush(Color.FromArgb(190, 2, 4, 8)))
            g.FillEllipse(castShadow, kx - rCap + 2f, ky - rCap + 5.5f, rCap * 2, rCap * 2);

        if (moved)
        {
            using var shaftPen = new Pen(Color.FromArgb(180, 70, 84, 110), 7.5f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            g.DrawLine(shaftPen, cx, cy, kx, ky);
        }

        if (pressed)
        {
            using var glow = new SolidBrush(Color.FromArgb(105, accent));
            g.FillEllipse(glow, kx - rCap - 5, ky - rCap - 5, (rCap + 5) * 2, (rCap + 5) * 2);
        }

        var capRect = new RectangleF(kx - rCap, ky - rCap, rCap * 2, rCap * 2);
        var inRect = new RectangleF(kx - rInner, ky - rInner, rInner * 2, rInner * 2);
        Color capCol = pressed ? Color.White : (moved ? accent : cVecOuterWhite);

        using (var capGrad = new LinearGradientBrush(capRect,
            pressed ? accent : Color.FromArgb(52, 64, 88),
            pressed ? Color.FromArgb(165, accent) : Color.FromArgb(18, 23, 34),
            LinearGradientMode.ForwardDiagonal))
        using (var inGrad = new LinearGradientBrush(inRect,
            pressed ? Color.FromArgb(150, accent) : Color.FromArgb(12, 16, 24),
            pressed ? accent : Color.FromArgb(38, 48, 66),
            LinearGradientMode.ForwardDiagonal))
        using (var capPen = new Pen(capCol, pressed || moved ? 2.1f : 1.7f))
        using (var inPen = new Pen(pressed ? cBadgeDarkText : cVecLine, 1.4f))
        {
            g.FillEllipse(capGrad, capRect);
            g.DrawEllipse(capPen, capRect);
            g.FillEllipse(inGrad, inRect);
            g.DrawEllipse(inPen, inRect);
        }

        // ۱۶ نقطه برجسته ضدلغزش روی لبه‌ی کلاهک استیک
        float rKnurl = rCap - 2.8f;
        using var dotBrush = new SolidBrush(pressed ? Color.FromArgb(160, cBadgeDarkText) : Color.FromArgb(155, 195, 212, 238));
        for (int a = 0; a < 360; a += 24)
        {
            double rad = a * Math.PI / 180.0;
            float gx = kx + (float)Math.Cos(rad) * rKnurl;
            float gy = ky + (float)Math.Sin(rad) * rKnurl;
            g.FillEllipse(dotBrush, gx - 1f, gy - 1f, 2f, 2f);
        }
    }

    private void DrawPs4SideHud(Graphics g, bool leftSide, Color accent)
    {
        float x = leftSide ? -286f : 286f;
        bool b1 = leftSide ? _s.LB : _s.RB;
        bool b2 = leftSide ? _s.LT : _s.RT;
        bool bMenu = leftSide ? _s.BACK : _s.START;

        if (leftSide || b1)
        {
            var hlRect = leftSide ? new RectangleF(x - 36, -84, 64, 38) : new RectangleF(x - 28, -84, 64, 38);
            using var hlPath = RoundRect(hlRect, 6f);
            using var hlFill = new SolidBrush(b1 ? Color.FromArgb(70, accent) : Color.FromArgb(44, 52, 68));
            g.FillPath(hlFill, hlPath);
        }
        DrawHudBumperBadge(g, x, -65, leftSide ? "L1" : "R1", b1, accent);
        DrawHudTriggerShieldBadge(g, x, -26, leftSide ? "L2" : "R2", b2, accent);
        DrawHudCapsuleLabelBadge(g, x, 14, leftSide ? "SHARE" : "OPTIONS", bMenu, accent);
    }

    // ==================== ابزارهای مشترک رسم بج‌های کناری سه‌بعدی (3D HUD Badges) ====================
    private void DrawHudBumperBadge(Graphics g, float cx, float cy, string label, bool pressed, Color accent)
    {
        var r = new RectangleF(cx - 14, cy - 9, 28, 18);
        using var path = new GraphicsPath();
        path.AddArc(r.X, r.Y, 12, 12, 180, 90);
        path.AddArc(r.Right - 12, r.Y, 12, 12, 270, 90);
        path.AddLine(r.Right, r.Bottom - 3, r.Right - 3, r.Bottom);
        path.AddLine(r.X + 3, r.Bottom, r.X, r.Bottom - 3);
        path.CloseFigure();

        using (var m = new Matrix())
        {
            m.Translate(0, 2.2f);
            using var sh = (GraphicsPath)path.Clone();
            sh.Transform(m);
            using var sb = new SolidBrush(Color.FromArgb(150, 3, 5, 10));
            g.FillPath(sb, sh);
        }

        using var fill = new LinearGradientBrush(r,
            pressed ? accent : Color.White,
            pressed ? Color.FromArgb(190, accent) : Color.FromArgb(205, 214, 230),
            LinearGradientMode.Vertical);
        g.FillPath(fill, path);

        using var f = new Font("Segoe UI", 7.8f, FontStyle.Bold);
        using var tb = new SolidBrush(cBadgeDarkText);
        using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        g.DrawString(label, f, tb, r, sf);
    }

    private void DrawHudTriggerSquareBadge(Graphics g, float cx, float cy, string label, bool pressed, Color accent)
    {
        var r = new RectangleF(cx - 12, cy - 10, 24, 20);
        var shRect = new RectangleF(r.X, r.Y + 2.2f, r.Width, r.Height);
        using (var shPath = RoundRect(shRect, 4f))
        using (var sb = new SolidBrush(Color.FromArgb(150, 3, 5, 10)))
            g.FillPath(sb, shPath);

        using var path = RoundRect(r, 4f);
        using var fill = new LinearGradientBrush(r,
            pressed ? accent : Color.White,
            pressed ? Color.FromArgb(190, accent) : Color.FromArgb(205, 214, 230),
            LinearGradientMode.Vertical);
        g.FillPath(fill, path);

        using var f = new Font("Segoe UI", 7.5f, FontStyle.Bold);
        using var tb = new SolidBrush(cBadgeDarkText);
        using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        g.DrawString(label, f, tb, r, sf);
    }

    private void DrawHudTriggerShieldBadge(Graphics g, float cx, float cy, string label, bool pressed, Color accent)
    {
        using var path = new GraphicsPath();
        path.AddLines(new[]
        {
            new PointF(cx - 13, cy - 10),
            new PointF(cx + 13, cy - 10),
            new PointF(cx + 13, cy + 2),
            new PointF(cx + 7, cy + 11),
            new PointF(cx - 7, cy + 11),
            new PointF(cx - 13, cy + 2),
        });
        path.CloseFigure();

        using (var m = new Matrix())
        {
            m.Translate(0, 2.2f);
            using var sh = (GraphicsPath)path.Clone();
            sh.Transform(m);
            using var sb = new SolidBrush(Color.FromArgb(150, 3, 5, 10));
            g.FillPath(sb, sh);
        }

        var r = new RectangleF(cx - 14, cy - 10, 28, 21);
        using var fill = new LinearGradientBrush(r,
            pressed ? accent : Color.White,
            pressed ? Color.FromArgb(190, accent) : Color.FromArgb(205, 214, 230),
            LinearGradientMode.Vertical);
        g.FillPath(fill, path);

        using var f = new Font("Segoe UI", 7.5f, FontStyle.Bold);
        using var tb = new SolidBrush(cBadgeDarkText);
        using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        g.DrawString(label, f, tb, new RectangleF(cx - 14, cy - 10, 28, 20), sf);
    }

    private void DrawHudCapsuleLabelBadge(Graphics g, float cx, float cy, string label, bool pressed, Color accent)
    {
        var r = new RectangleF(cx - 16, cy - 6, 32, 12);
        var shRect = new RectangleF(r.X, r.Y + 2f, r.Width, r.Height);
        using (var shPath = RoundRect(shRect, 6f))
        using (var sb = new SolidBrush(Color.FromArgb(150, 3, 5, 10)))
            g.FillPath(sb, shPath);

        using var path = RoundRect(r, 6f);
        using var fill = new LinearGradientBrush(r,
            pressed ? accent : Color.White,
            pressed ? Color.FromArgb(190, accent) : Color.FromArgb(205, 214, 230),
            LinearGradientMode.Vertical);
        g.FillPath(fill, path);

        using var f = new Font("Segoe UI", 4.5f, FontStyle.Bold);
        using var tb = new SolidBrush(cBadgeDarkText);
        using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        g.DrawString(label, f, tb, r, sf);
    }

    private void DrawHudStickIcon(Graphics g, float cx, float cy, string label, bool pressed, Color accent)
    {
        var r = new RectangleF(cx - 10, cy - 10, 20, 21);
        using (var sh = new SolidBrush(Color.FromArgb(150, 3, 5, 10)))
            g.FillEllipse(sh, cx - 9, cy - 8, 18, 14);

        using (var fill = new LinearGradientBrush(r,
            pressed ? accent : Color.White,
            pressed ? Color.FromArgb(190, accent) : Color.FromArgb(205, 214, 230),
            LinearGradientMode.Vertical))
        {
            g.FillEllipse(fill, cx - 9, cy - 10, 18, 14);
            g.FillPolygon(fill, new[]
            {
                new PointF(cx - 5, cy + 3),
                new PointF(cx + 5, cy + 3),
                new PointF(cx + 7, cy + 10),
                new PointF(cx - 7, cy + 10),
            });
        }
        using var f = new Font("Segoe UI", 5.5f, FontStyle.Bold);
        using var tb = new SolidBrush(cBadgeDarkText);
        using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        g.DrawString(label, f, tb, new RectangleF(cx - 10, cy - 10, 20, 14), sf);
    }

    // ==================== بنر قطع اتصال و توابع کمکی ====================
    private void DrawDisconnectedBanner(Graphics g)
    {
        var br = new RectangleF(-190, -28, 380, 56);
        var shRect = new RectangleF(-190, -22, 380, 56);
        using (var shPath = RoundRect(shRect, 12f))
        using (var shBrush = new SolidBrush(Color.FromArgb(170, 0, 0, 0)))
            g.FillPath(shBrush, shPath);

        using var path = RoundRect(br, 12f);
        using var fill = new LinearGradientBrush(br, Color.FromArgb(238, 22, 28, 42), Color.FromArgb(238, 12, 16, 25), LinearGradientMode.Vertical);
        using var pen = new Pen(Color.FromArgb(239, 90, 90), 2f);
        using var f = new Font("Segoe UI Semibold", 11f);
        using var tb = new SolidBrush(cFg);
        g.FillPath(fill, path);
        g.DrawPath(pen, path);
        using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        g.DrawString("گوشی این پلیر قطع شده است", f, tb, br, sf);
    }

    // ==================== حالت فرمون (Steering Wheel) ====================
    // فرمون با مقدار LX میچرخه (همونی که گوشی از روی ژیروسکوپ حساب کرده و میفرسته)؛
    // پدال گاز = ماشه‌ی راست (RTV)، پدال ترمز = ماشه‌ی چپ (LTV)، پدال‌های شیفت = LB / RB، بوق = L3 (وسط فرمون)، دوربین = R3
    private const float WheelShowLock = 270f;    // چرخش نمایشی فرمون در قفل کامل (درجه)؛ با پیش‌فرض index.html یکیه

    private void DrawWheelController(Graphics g, Color accent)
    {
        const float f = 0.47f;                   // هندسه‌ی فرمون (شعاع حدود ۲۵۰) × این = اندازه‌ی نهایی
        const float wx = 0f, wy = -14f;
        float lx = Math.Max(-1f, Math.Min(1f, _s.LX));
        float angle = lx * WheelShowLock;

        // ---- قاب ثابت دور فرمون + تیک‌های زاویه + نقطه‌ی زاویه‌ی فعلی ----
        float bzR = 325f * f;
        using (var glow = new Pen(Color.FromArgb(_s.Connected ? 60 : 20, accent), 9f))
            g.DrawEllipse(glow, wx - bzR, wy - bzR, bzR * 2, bzR * 2);
        using (var bz = new Pen(Color.FromArgb(190, accent), 2.4f))
            g.DrawEllipse(bz, wx - bzR, wy - bzR, bzR * 2, bzR * 2);
        for (int d = -180; d <= 180; d += 45)
        {
            bool big = d % 90 == 0;
            double ta = d * Math.PI / 180.0;
            float sn = (float)Math.Sin(ta), cs = (float)Math.Cos(ta);
            float r0 = bzR - (big ? 14f : 8f), r1 = bzR - 2f;
            using var tp = new Pen(Color.FromArgb(big ? 200 : 120, cVecLine), big ? 2f : 1.4f);
            g.DrawLine(tp, wx + sn * r0, wy - cs * r0, wx + sn * r1, wy - cs * r1);
        }
        {
            double da = angle * Math.PI / 180.0;
            float dx = wx + (float)Math.Sin(da) * bzR, dy = wy - (float)Math.Cos(da) * bzR;
            using var dg = new SolidBrush(Color.FromArgb(90, accent));
            using var db = new SolidBrush(accent);
            g.FillEllipse(dg, dx - 9f, dy - 9f, 18f, 18f);
            g.FillEllipse(db, dx - 5f, dy - 5f, 10f, 10f);
        }

        // ---- خود فرمون (میچرخه) ----
        var saved = g.Save();
        g.TranslateTransform(wx, wy);
        g.RotateTransform(angle);
        g.ScaleTransform(f, f);

        using (var rim = new Pen(cPadBorder, 13f) { LineJoin = LineJoin.Round, StartCap = LineCap.Round, EndCap = LineCap.Round })
        using (var body = new SolidBrush(Color.FromArgb(230, cPadBody)))
        {
            for (int side = -1; side <= 1; side += 2)
            {
                float m = side;
                var spoke = new[] { new PointF(m * 44f, -50f), new PointF(m * 188f, -75f), new PointF(m * 198f, 0f), new PointF(m * 62f, 32f) };
                g.FillPolygon(body, spoke);
                g.DrawLines(rim, spoke);

                using (var lobe = new GraphicsPath())
                {
                    lobe.AddLine(m * 198f, 0f, m * 240f, 2f);
                    lobe.AddLine(m * 240f, 2f, m * 252f, 14f);
                    if (side < 0) lobe.AddArc(-252.6f, -258f, 505.2f, 505.2f, 175.6f, -70.6f);
                    else lobe.AddArc(-252.6f, -258f, 505.2f, 505.2f, 4.4f, 70.6f);
                    lobe.AddLine(m * 65.4f, 238.6f, m * 49f, 232f);
                    lobe.AddLine(m * 49f, 232f, m * 41f, 172f);
                    g.DrawPath(rim, lobe);
                }

                g.DrawLine(rim, m * 34f, 62f, m * 30f, 172f);
            }
            g.DrawLine(rim, -41f, 172f, 41f, 172f);

            using (var top = new GraphicsPath())
            {
                top.AddLine(-188f, -75f, -220f, -93f);
                top.AddLine(-220f, -93f, -226f, -115f);
                top.AddArc(-252.6f, -258f, 505.2f, 505.2f, 205.9f, 128.2f);
                top.AddLine(226f, -115f, 220f, -93f);
                top.AddLine(220f, -93f, 188f, -75f);
                g.DrawPath(rim, top);
            }

            using (var hubFill = new SolidBrush(cPadBody))
                g.FillEllipse(hubFill, -66f, -66f, 132f, 132f);
            g.DrawEllipse(rim, -66f, -66f, 132f, 132f);
            using (var ring = new Pen(cPadBorder, 10f))
                g.DrawEllipse(ring, -32f, -32f, 64f, 64f);
            using (var horn = new SolidBrush(_s.L3 ? accent : cBtnIdle))
                g.FillEllipse(horn, -14f, -14f, 28f, 28f);

            using (var mk = new Pen(accent, 22f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                g.DrawLine(mk, 0f, -258f, 0f, -224f);
        }
        g.Restore(saved);

        // ---- پدال‌های شیفت (پشت فرمون): چپ = LB (دنده کم) ، راست = RB (دنده زیاد) ----
        DrawWheelPaddle(g, -190f, -46f, 24f, 84f, _s.LB, "-", accent);
        DrawWheelPaddle(g, 166f, -46f, 24f, 84f, _s.RB, "+", accent);

        // ---- پدال‌های ترمز و گاز (پر شدنشون = مقدار آنالوگ) ----
        DrawWheelPedal(g, -300f, -46f, 54f, 170f, _s.LTV, "BRAKE", cXboxB);
        DrawWheelPedal(g, 246f, -46f, 54f, 170f, _s.RTV, "GAS", cXboxA);

        // ---- دکمه‌ها ----
        DrawXboxHybridDpad(g, -262f, -118f, 37.5f, accent);
        DrawXboxVectorActionButtons(g, 262f, -118f, 34.5f, 17.2f);
        DrawXboxSystemCircleButton(g, -52f, -186f, 11.2f, _s.BACK, accent, isMenu: false);
        DrawXboxSystemCircleButton(g, 52f, -186f, 11.2f, _s.START, accent, isMenu: true);
        DrawXboxNexusGuideButton(g, 0f, 172f, 16f, _s.GUIDE, accent);

        // R3 (دوربین) کنار پدال گاز
        {
            float rx = 206f, ry = -186f, rr = 12f;
            using var bb = new SolidBrush(_s.R3 ? accent : cBtnIdle);
            using var bp = new Pen(_s.R3 ? Color.White : cPadBorder, 1.5f);
            g.FillEllipse(bb, rx - rr, ry - rr, rr * 2, rr * 2);
            g.DrawEllipse(bp, rx - rr, ry - rr, rr * 2, rr * 2);
            using var tf = new Font("Segoe UI Semibold", 7.5f);
            using var tb = new SolidBrush(_s.R3 ? cBadgeDarkText : cMuted);
            using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.DrawString("R3", tf, tb, new RectangleF(rx - rr, ry - rr, rr * 2, rr * 2), sf);
        }
    }

    private void DrawWheelPedal(Graphics g, float x, float y, float w, float h, float level, string label, Color col)
    {
        level = Math.Max(0f, Math.Min(1f, level));
        var r = new RectangleF(x, y, w, h);
        using (var path = RoundRect(r, 9f))
        {
            using (var bg = new LinearGradientBrush(r, Color.FromArgb(34, 42, 60), Color.FromArgb(16, 20, 30), LinearGradientMode.Vertical))
                g.FillPath(bg, path);

            if (level > 0.001f)
            {
                var st = g.Save();
                g.SetClip(path, CombineMode.Intersect);
                float fh = h * level;
                using (var fb = new LinearGradientBrush(r, Color.FromArgb(235, col), Color.FromArgb(120, col), LinearGradientMode.Vertical))
                    g.FillRectangle(fb, new RectangleF(x, y + h - fh, w, fh));
                g.Restore(st);
            }

            using (var rib = new Pen(Color.FromArgb(70, 0, 0, 0), 1.6f))
                for (float yy = y + 14f; yy < y + h - 6f; yy += 14f)
                    g.DrawLine(rib, x + 7f, yy, x + w - 7f, yy);

            using (var bp = new Pen(level > 0.02f ? col : cPadBorder, 1.8f))
                g.DrawPath(bp, path);
        }

        using (var fnt = new Font("Segoe UI Semibold", 9f))
        using (var tb = new SolidBrush(level > 0.02f ? col : cMuted))
        using (var sf = new StringFormat { Alignment = StringAlignment.Center })
        {
            g.DrawString(label, fnt, tb, x + w / 2f, y + h + 4f, sf);
            g.DrawString(((int)Math.Round(level * 100f)).ToString() + "%", fnt, tb, x + w / 2f, y + h + 19f, sf);
        }
    }

    private void DrawWheelPaddle(Graphics g, float x, float y, float w, float h, bool pressed, string sym, Color accent)
    {
        var r = new RectangleF(x, y, w, h);
        using (var path = RoundRect(r, 7f))
        {
            using (var fill = new LinearGradientBrush(r,
                pressed ? accent : Color.FromArgb(46, 56, 78),
                pressed ? Color.FromArgb(170, accent) : Color.FromArgb(18, 23, 34),
                LinearGradientMode.Vertical))
                g.FillPath(fill, path);
            using (var pen = new Pen(pressed ? Color.White : cPadBorder, 1.6f))
                g.DrawPath(pen, path);
        }
        using (var fnt = new Font("Segoe UI Semibold", 14f))
        using (var tb = new SolidBrush(pressed ? cBadgeDarkText : cVecOuterWhite))
        using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            g.DrawString(sym, fnt, tb, r, sf);
    }

    // ==================== تستر کیبرد و ماوس ====================
    // وقتی گوشی روی «کیبرد» یا «ماوس» هست دسته‌ای در کار نیست و ورودی‌ها مستقیم تو ویندوز تزریق میشن.
    // پس تستر وضعیت کلیدها و دکمه‌های ماوس رو از خود ویندوز میخونه (GetAsyncKeyState) و برای چرخ ماوس یه هوک سبک میذاره.
    // یعنی ورودی واقعیِ همین کامپیوتر هم نشون داده میشه، نه فقط ورودی گوشی.

    [DllImport("user32.dll")] private static extern short GetAsyncKeyState(int vKey);
    [DllImport("user32.dll")] private static extern bool GetCursorPos(out MkPoint p);
    [DllImport("user32.dll", SetLastError = true)] private static extern IntPtr SetWindowsHookEx(int idHook, MkHookProc fn, IntPtr hMod, uint threadId);
    [DllImport("user32.dll")] private static extern bool UnhookWindowsHookEx(IntPtr hHook);
    [DllImport("user32.dll")] private static extern IntPtr CallNextHookEx(IntPtr hHook, int code, IntPtr wParam, IntPtr lParam);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] private static extern IntPtr GetModuleHandle(string? name);

    [StructLayout(LayoutKind.Sequential)] private struct MkPoint { public int X, Y; }
    [StructLayout(LayoutKind.Sequential)] private struct MsLlHook { public MkPoint Pt; public uint MouseData, Flags, Time; public IntPtr ExtraInfo; }
    private delegate IntPtr MkHookProc(int code, IntPtr wParam, IntPtr lParam);
    private struct MsStep { public float Dx, Dy; public long T; }

    private sealed class KbKey
    {
        public string Code = "";
        public string Label = "";
        public float W = 1f;
        public int Vk;
    }

    // چیدمان دقیقاً مثل کیبرد داخل index.html (۶ ردیف ، عرض هر ردیف = ۱۵ واحد)
    private static readonly KbKey[][] KbRows = BuildKbRows();
    private static KbKey Kk(string code, string label, int vk, float w = 1f) => new KbKey { Code = code, Label = label, Vk = vk, W = w };

    private static KbKey[][] BuildKbRows()
    {
        var rows = new List<KbKey[]>();
        var r = new List<KbKey> { Kk("Escape", "Esc", 0x1B, 1.2f) };
        for (int i = 1; i <= 12; i++) r.Add(Kk("F" + i, "F" + i, 0x6F + i));
        r.Add(Kk("Delete", "Del", 0x2E, 1.8f));
        rows.Add(r.ToArray());

        r = new List<KbKey> { Kk("Backquote", "`", 0xC0) };
        for (int i = 1; i <= 10; i++) { int d = i % 10; r.Add(Kk("Digit" + d, d.ToString(), 0x30 + d)); }
        r.Add(Kk("Minus", "-", 0xBD)); r.Add(Kk("Equal", "=", 0xBB)); r.Add(Kk("Backspace", "Back", 0x08, 2f));
        rows.Add(r.ToArray());

        r = new List<KbKey> { Kk("Tab", "Tab", 0x09, 1.5f) };
        foreach (char c in "QWERTYUIOP") r.Add(Kk("Key" + c, c.ToString(), c));
        r.Add(Kk("BracketLeft", "[", 0xDB)); r.Add(Kk("BracketRight", "]", 0xDD)); r.Add(Kk("Backslash", "\\", 0xDC, 1.5f));
        rows.Add(r.ToArray());

        r = new List<KbKey> { Kk("CapsLock", "Caps", 0x14, 1.8f) };
        foreach (char c in "ASDFGHJKL") r.Add(Kk("Key" + c, c.ToString(), c));
        r.Add(Kk("Semicolon", ";", 0xBA)); r.Add(Kk("Quote", "'", 0xDE)); r.Add(Kk("Enter", "Enter", 0x0D, 2.2f));
        rows.Add(r.ToArray());

        r = new List<KbKey> { Kk("ShiftLeft", "Shift", 0xA0, 2.3f) };
        foreach (char c in "ZXCVBNM") r.Add(Kk("Key" + c, c.ToString(), c));
        r.Add(Kk("Comma", ",", 0xBC)); r.Add(Kk("Period", ".", 0xBE)); r.Add(Kk("Slash", "/", 0xBF)); r.Add(Kk("ShiftRight", "Shift", 0xA1, 2.7f));
        rows.Add(r.ToArray());

        r = new List<KbKey>
        {
            Kk("ControlLeft", "Ctrl", 0xA2, 1.4f), Kk("MetaLeft", "Win", 0x5B, 1.2f), Kk("AltLeft", "Alt", 0xA4, 1.2f),
            Kk("Space", "", 0x20, 5.4f), Kk("AltRight", "Alt", 0xA5, 1.2f),
            Kk("ArrowLeft", "←", 0x25, 1.15f), Kk("ArrowDown", "↓", 0x28, 1.15f), Kk("ArrowUp", "↑", 0x26, 1.15f), Kk("ArrowRight", "→", 0x27, 1.15f),
        };
        rows.Add(r.ToArray());
        return rows.ToArray();
    }

    private static string KbName(KbKey k) => k.Code switch
    {
        "Space" => "Space",
        "ArrowUp" => "↑",
        "ArrowDown" => "↓",
        "ArrowLeft" => "←",
        "ArrowRight" => "→",
        "ShiftLeft" => "Shift (L)",
        "ShiftRight" => "Shift (R)",
        "AltLeft" => "Alt (L)",
        "AltRight" => "Alt (R)",
        "Back" => "Backspace",
        "Backspace" => "Backspace",
        "Delete" => "Delete",
        _ => k.Label,
    };

    private static int KbTotal()
    {
        int n = 0;
        foreach (var row in KbRows) n += row.Length;
        return n;
    }

    // وضعیت لحظه‌ای کیبرد
    private readonly HashSet<string> _kbHeld = new();
    private readonly HashSet<string> _kbPrev = new();
    private readonly HashSet<string> _kbTested = new();
    private readonly List<string> _kbNames = new();
    private string _kbLast = "—";

    // وضعیت لحظه‌ای ماوس
    private bool _msL, _msR, _msM, _msX1, _msX2;
    private readonly HashSet<string> _msTested = new();
    private readonly List<MsStep> _msTrail = new();
    private bool _msInit;
    private MkPoint _msLast, _msPos;
    private long _msLastTick;
    private float _msVx, _msVy, _msDist;
    private int _msDx, _msDy;
    private int _wheelAccum;            // جمع دلتای چرخ که هوک میریزه ، تو تیک تایمر مصرف میشه
    private float _msWheelTotal;        // جمع کل ، بر حسب «تیک» (هر تیک = 120)
    private int _msWheelDir;
    private long _msWheelTick;
    private IntPtr _mouseHook = IntPtr.Zero;
    private MkHookProc? _mouseProc;
    private bool _hookFailed;
    private bool _btnIsClear;           // دکمه‌ی بالای پنجره تو حالت کیبرد/ماوس «پاک کردن» میشه

    private bool IsMkMode => _s.Mode == "keyboard" || _s.Mode == "mouse";

    private static bool VkDown(int vk) => (GetAsyncKeyState(vk) & 0x8000) != 0;

    private void ClearMkTested()
    {
        _kbTested.Clear();
        _msTested.Clear();
        _msDist = 0;
        _msWheelTotal = 0;
        _msTrail.Clear();
        _kbLast = "—";
    }

    private void PollMk()
    {
        bool kb = _s.Mode == "keyboard", ms = _s.Mode == "mouse";
        if (ms) EnsureMouseHook(); else RemoveMouseHook();
        if (kb) PollKeyboard(); else if (_kbHeld.Count > 0) { _kbHeld.Clear(); _kbNames.Clear(); }
        if (ms) PollMouse(); else _msInit = false;
    }

    private void PollKeyboard()
    {
        _kbPrev.Clear();
        _kbPrev.UnionWith(_kbHeld);
        _kbHeld.Clear();
        _kbNames.Clear();
        bool shiftAny = VkDown(0x10), ctrlAny = VkDown(0x11), altAny = VkDown(0x12);
        foreach (var row in KbRows)
            foreach (var k in row)
            {
                bool d = VkDown(k.Vk);
                if (!d)
                {
                    // بعضی برنامه‌ها شیفت/کنترل/آلت رو بدون چپ و راست تزریق میکنن
                    if (k.Code == "ShiftLeft") d = shiftAny && !VkDown(0xA1);
                    else if (k.Code == "ControlLeft") d = ctrlAny && !VkDown(0xA3);
                    else if (k.Code == "AltLeft") d = altAny && !VkDown(0xA5);
                }
                if (!d) continue;
                _kbHeld.Add(k.Code);
                _kbTested.Add(k.Code);
                string nm = KbName(k);
                _kbNames.Add(nm);
                if (!_kbPrev.Contains(k.Code)) _kbLast = nm;
            }
    }

    private void PollMouse()
    {
        long now = Clock.Ms;
        GetCursorPos(out var p);
        _msL = VkDown(0x01); _msR = VkDown(0x02); _msM = VkDown(0x04); _msX1 = VkDown(0x05); _msX2 = VkDown(0x06);
        if (_msL) _msTested.Add("L");
        if (_msR) _msTested.Add("R");
        if (_msM) _msTested.Add("M");
        if (_msX1) _msTested.Add("X1");
        if (_msX2) _msTested.Add("X2");

        if (!_msInit) { _msInit = true; _msLast = p; _msVx = _msVy = 0; _msLastTick = now; _msTrail.Clear(); }
        int dx = p.X - _msLast.X, dy = p.Y - _msLast.Y;
        _msLast = p; _msPos = p;
        float dt = Math.Max(0.001f, (now - _msLastTick) / 1000f);
        _msLastTick = now;
        _msDx = dx; _msDy = dy;
        _msVx = _msVx * 0.7f + (dx / dt) * 0.3f;
        _msVy = _msVy * 0.7f + (dy / dt) * 0.3f;
        if (Math.Abs(_msVx) < 1f && Math.Abs(_msVy) < 1f) { _msVx = 0; _msVy = 0; }
        if (dx != 0 || dy != 0)
        {
            _msTrail.Add(new MsStep { Dx = dx, Dy = dy, T = now });
            _msDist += (float)Math.Sqrt((double)dx * dx + (double)dy * dy);
        }
        while (_msTrail.Count > 0 && now - _msTrail[0].T > 1500) _msTrail.RemoveAt(0);

        if (_wheelAccum != 0)
        {
            _msWheelTotal += _wheelAccum / 120f;
            _msWheelDir = _wheelAccum > 0 ? 1 : -1;
            _msWheelTick = now;
            _wheelAccum = 0;
        }
    }

    private void EnsureMouseHook()
    {
        if (_mouseHook != IntPtr.Zero || _hookFailed) return;
        try
        {
            _mouseProc = MouseHookCb;
            _mouseHook = SetWindowsHookEx(14, _mouseProc, GetModuleHandle(null), 0);   // 14 = WH_MOUSE_LL
            if (_mouseHook == IntPtr.Zero) _hookFailed = true;
        }
        catch { _hookFailed = true; }
    }

    private void RemoveMouseHook()
    {
        if (_mouseHook == IntPtr.Zero) return;
        try { UnhookWindowsHookEx(_mouseHook); } catch { }
        _mouseHook = IntPtr.Zero;
    }

    private IntPtr MouseHookCb(int code, IntPtr wParam, IntPtr lParam)
    {
        try
        {
            if (code >= 0 && (int)wParam == 0x020A)   // WM_MOUSEWHEEL
            {
                var d = Marshal.PtrToStructure<MsLlHook>(lParam);
                _wheelAccum += unchecked((short)(d.MouseData >> 16));
            }
        }
        catch { }
        return CallNextHookEx(_mouseHook, code, wParam, lParam);
    }

    private List<string> GetMkPressedList()
    {
        if (_s.Mode == "keyboard") return new List<string>(_kbNames);
        var list = new List<string>();
        if (_msL) list.Add("کلیک چپ");
        if (_msR) list.Add("کلیک راست");
        if (_msM) list.Add("کلیک وسط (چرخ)");
        if (_msX1) list.Add("X1 (عقب)");
        if (_msX2) list.Add("X2 (جلو)");
        if (Clock.Ms - _msWheelTick < 400 && _msWheelDir != 0) list.Add(_msWheelDir > 0 ? "اسکرول ▲" : "اسکرول ▼");
        return list;
    }

    // ---------- رسم کیبرد ----------
    private void DrawKeyboardTester(Graphics g, Color accent)
    {
        const float U = 40f, H = 38f, G = 4f;
        float totalW = 15f * U;
        float x0 = -totalW / 2f, y0 = -146f;
        float plateH = 5f * (H + G) + 8f + H + 28f;

        var plate = new RectangleF(x0 - 14f, y0 - 14f, totalW + 28f, plateH);
        using (var shP = RoundRect(new RectangleF(plate.X, plate.Y + 8f, plate.Width, plate.Height), 16f))
        using (var shB = new SolidBrush(Color.FromArgb(90, 0, 0, 0)))
            g.FillPath(shB, shP);
        using (var pp = RoundRect(plate, 16f))
        {
            using (var pb = new LinearGradientBrush(plate, Color.FromArgb(26, 32, 47), Color.FromArgb(14, 18, 28), LinearGradientMode.Vertical))
                g.FillPath(pb, pp);
            using (var pen = new Pen(cGrid, 1.5f))
                g.DrawPath(pen, pp);
        }

        using var fSmall = new Font("Segoe UI Semibold", 8f);
        using var fBig = new Font("Segoe UI Semibold", 10.5f);
        using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        Color idleText = Color.FromArgb(196, 206, 226);

        for (int ri = 0; ri < KbRows.Length; ri++)
        {
            float x = x0;
            float y = y0 + ri * (H + G) + (ri > 0 ? 8f : 0f);
            foreach (var k in KbRows[ri])
            {
                float w = k.W * U;
                var r = new RectangleF(x + G / 2f, y, w - G, H);
                x += w;
                bool down = _kbHeld.Contains(k.Code);
                bool tested = _kbTested.Contains(k.Code);

                using (var path = RoundRect(r, 6f))
                {
                    if (!down)
                    {
                        using (var shB = new SolidBrush(Color.FromArgb(80, 0, 0, 0)))
                        using (var shP = RoundRect(new RectangleF(r.X, r.Y + 2.5f, r.Width, r.Height), 6f))
                            g.FillPath(shB, shP);
                    }
                    using (var fill = new LinearGradientBrush(r,
                        down ? accent : Color.FromArgb(42, 51, 72),
                        down ? Color.FromArgb(175, accent) : Color.FromArgb(24, 30, 44),
                        LinearGradientMode.Vertical))
                        g.FillPath(fill, path);
                    if (!down && tested)
                        using (var tb = new SolidBrush(Color.FromArgb(58, accent)))
                            g.FillPath(tb, path);
                    using (var pen = new Pen(down ? Color.White : (tested ? Color.FromArgb(170, accent) : cVecSubtle), down ? 1.8f : 1.2f))
                        g.DrawPath(pen, path);
                }

                if (k.Label.Length > 0)
                {
                    bool arrow = k.Label.Length == 1 && k.Label[0] > 0x2000;
                    using (var tbr = new SolidBrush(down ? cBadgeDarkText : (tested ? cFg : idleText)))
                        g.DrawString(k.Label, (k.Label.Length > 1 && !arrow) ? fSmall : fBig, tbr, r, sf);
                }
            }
        }

        string cap = string.Format(System.Globalization.CultureInfo.InvariantCulture,
            "کلیدهایی که تست شدن کم‌رنگ می‌مونن  |  {0} / {1}  |  با دکمه‌ی «پاک کردن» ریست میشه", _kbTested.Count, KbTotal());
        using (var fc = new Font("Segoe UI", 9f))
        using (var cb = new SolidBrush(cMuted))
            g.DrawString(cap, fc, cb, new RectangleF(x0 - 14f, plate.Bottom + 6f, totalW + 28f, 22f), sf);
    }

    // ---------- رسم ماوس ----------
    private void DrawMouseBtnZone(Graphics g, RectangleF zone, bool pressed, bool tested, Color accent)
    {
        if (pressed)
        {
            using (var fb = new LinearGradientBrush(zone, accent, Color.FromArgb(160, accent), LinearGradientMode.Vertical))
                g.FillRectangle(fb, zone);
        }
        else if (tested)
        {
            using (var tb = new SolidBrush(Color.FromArgb(52, accent)))
                g.FillRectangle(tb, zone);
        }
    }

    private void DrawMouseSideBtn(Graphics g, RectangleF r, bool pressed, bool tested, Color accent)
    {
        using (var path = RoundRect(r, 4f))
        {
            using (var fb = new LinearGradientBrush(r,
                pressed ? accent : Color.FromArgb(48, 58, 80),
                pressed ? Color.FromArgb(170, accent) : Color.FromArgb(22, 28, 40),
                LinearGradientMode.Vertical))
                g.FillPath(fb, path);
            if (!pressed && tested)
                using (var tb = new SolidBrush(Color.FromArgb(70, accent)))
                    g.FillPath(tb, path);
            using (var pen = new Pen(pressed ? Color.White : cPadBorder, 1.4f))
                g.DrawPath(pen, path);
        }
    }

    private void DrawMouseTester(Graphics g, Color accent)
    {
        var inv = System.Globalization.CultureInfo.InvariantCulture;
        long now = Clock.Ms;
        using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };

        // ===== بدنه‌ی ماوس =====
        var st = g.Save();
        g.TranslateTransform(-190f, -14f);
        var bodyR = new RectangleF(-78f, -118f, 156f, 236f);
        using (var shP = RoundRect(new RectangleF(-78f, -108f, 156f, 236f), 74f))
        using (var shB = new SolidBrush(Color.FromArgb(100, 0, 0, 0)))
            g.FillPath(shB, shP);
        using (var body = RoundRect(bodyR, 74f))
        {
            using (var bg = new LinearGradientBrush(bodyR, Color.FromArgb(44, 53, 74), Color.FromArgb(16, 21, 32), LinearGradientMode.Vertical))
                g.FillPath(bg, body);
            var cs = g.Save();
            g.SetClip(body, CombineMode.Intersect);
            DrawMouseBtnZone(g, new RectangleF(-80f, -120f, 69f, 112f), _msL, _msTested.Contains("L"), accent);
            DrawMouseBtnZone(g, new RectangleF(11f, -120f, 69f, 112f), _msR, _msTested.Contains("R"), accent);
            using (var seam = new Pen(Color.FromArgb(150, cVecLine), 1.4f))
            {
                g.DrawLine(seam, -11f, -120f, -11f, -8f);
                g.DrawLine(seam, 11f, -120f, 11f, -8f);
                g.DrawLine(seam, -80f, -8f, 80f, -8f);
            }
            g.Restore(cs);
            using (var pen = new Pen(cPadBorder, 1.8f))
                g.DrawPath(pen, body);
        }

        using (var fl = new Font("Segoe UI Semibold", 13f))
        {
            using (var tb = new SolidBrush(_msL ? cBadgeDarkText : Color.FromArgb(150, cMuted)))
                g.DrawString("L", fl, tb, new RectangleF(-78f, -96f, 66f, 60f), sf);
            using (var tb = new SolidBrush(_msR ? cBadgeDarkText : Color.FromArgb(150, cMuted)))
                g.DrawString("R", fl, tb, new RectangleF(12f, -96f, 66f, 60f), sf);
        }

        // چرخ وسط (با خط‌های متحرک موقع اسکرول)
        bool wFlash = now - _msWheelTick < 220;
        var wr = new RectangleF(-8f, -98f, 16f, 44f);
        using (var wp = RoundRect(wr, 8f))
        {
            using (var wb = new LinearGradientBrush(wr,
                _msM ? accent : Color.FromArgb(58, 68, 92),
                _msM ? Color.FromArgb(170, accent) : Color.FromArgb(20, 26, 38),
                LinearGradientMode.Vertical))
                g.FillPath(wb, wp);
            if (!_msM && _msTested.Contains("M"))
                using (var tb = new SolidBrush(Color.FromArgb(60, accent)))
                    g.FillPath(tb, wp);
            if (wFlash)
                using (var fb = new SolidBrush(Color.FromArgb(70, accent)))
                    g.FillPath(fb, wp);
            var cs2 = g.Save();
            g.SetClip(wp, CombineMode.Intersect);
            float off = ((_msWheelTotal * 5f) % 10f + 10f) % 10f;
            using (var tk = new Pen(Color.FromArgb(120, 200, 210, 230), 1.2f))
                for (float ty = wr.Y - 10f + off; ty < wr.Bottom; ty += 10f)
                    g.DrawLine(tk, wr.X + 2f, ty, wr.Right - 2f, ty);
            g.Restore(cs2);
            using (var pen = new Pen(_msM ? Color.White : cPadBorder, 1.4f))
                g.DrawPath(pen, wp);
        }
        if (wFlash && _msWheelDir != 0)
        {
            using (var fa = new Font("Segoe UI Symbol", 8f))
            using (var ab = new SolidBrush(accent))
                g.DrawString(_msWheelDir > 0 ? "▲" : "▼", fa, ab, new RectangleF(-12f, _msWheelDir > 0 ? -114f : -52f, 24f, 14f), sf);
        }

        // دکمه‌های کناری X1 / X2
        DrawMouseSideBtn(g, new RectangleF(-88f, -6f, 16f, 28f), _msX1, _msTested.Contains("X1"), accent);
        DrawMouseSideBtn(g, new RectangleF(-88f, 28f, 16f, 28f), _msX2, _msTested.Contains("X2"), accent);

        using (var fs = new Font("Consolas", 9f))
        using (var sb = new SolidBrush(cMuted))
            g.DrawString(string.Format(inv, "Scroll: {0:+0.#;-0.#;0}", _msWheelTotal), fs, sb, new RectangleF(-100f, 128f, 200f, 18f), sf);
        g.Restore(st);

        // ===== رادار حرکت ماوس =====
        st = g.Save();
        g.TranslateTransform(120f, -14f);
        const float R = 140f;
        var disk = new RectangleF(-R, -R, 2 * R, 2 * R);
        using (var db = new SolidBrush(Color.FromArgb(14, 18, 28)))
            g.FillEllipse(db, disk);
        using (var ringPen = new Pen(cGrid, 1.2f))
        {
            g.DrawEllipse(ringPen, -R / 2f, -R / 2f, R, R);
            g.DrawLine(ringPen, -R, 0f, R, 0f);
            g.DrawLine(ringPen, 0f, -R, 0f, R);
        }
        using (var bp = new Pen(cPadBorder, 1.8f))
            g.DrawEllipse(bp, disk);

        // مسیر 1.5 ثانیه‌ی اخیر؛ سر مسیر (جای فعلی ماوس) وسط رادار میمونه
        int n = _msTrail.Count;
        if (n > 0)
        {
            var pts = new PointF[n + 1];
            float px = 0f, py = 0f, ext = 1f;
            pts[0] = new PointF(0f, 0f);
            for (int i = n - 1, j = 1; i >= 0; i--, j++)
            {
                px -= _msTrail[i].Dx; py -= _msTrail[i].Dy;
                pts[j] = new PointF(px, py);
                ext = Math.Max(ext, Math.Max(Math.Abs(px), Math.Abs(py)));
            }
            float sc = Math.Min(0.5f, (R - 14f) / ext);
            var cs3 = g.Save();
            using (var clipP = new GraphicsPath())
            {
                clipP.AddEllipse(disk);
                g.SetClip(clipP, CombineMode.Intersect);
            }
            for (int j = 0; j < n; j++)
            {
                float age = Math.Min(1f, (now - _msTrail[n - 1 - j].T) / 1500f);
                int a = (int)(230 * (1f - age));
                if (a < 6) continue;
                using (var tp = new Pen(Color.FromArgb(a, accent), 2.4f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                    g.DrawLine(tp, pts[j].X * sc, pts[j].Y * sc, pts[j + 1].X * sc, pts[j + 1].Y * sc);
            }
            g.Restore(cs3);
        }

        float speed = (float)Math.Sqrt(_msVx * _msVx + _msVy * _msVy);
        if (speed > 30f)
        {
            float len = Math.Min(R - 12f, speed * 0.06f);
            float ux = _msVx / speed, uy = _msVy / speed;
            using (var ap = new Pen(Color.White, 2.2f) { CustomEndCap = new AdjustableArrowCap(4f, 5f) })
                g.DrawLine(ap, 0f, 0f, ux * len, uy * len);
        }
        using (var hb = new SolidBrush(accent))
        using (var hp = new Pen(Color.White, 1.6f))
        {
            g.FillEllipse(hb, -7f, -7f, 14f, 14f);
            g.DrawEllipse(hp, -7f, -7f, 14f, 14f);
        }

        using (var fm = new Font("Consolas", 9.5f))
        using (var tb = new SolidBrush(cFg))
            g.DrawString(string.Format(inv, "{0,6:0} px/s    dx {1,4}  dy {2,4}", speed, _msDx, _msDy), fm, tb, new RectangleF(-R - 20f, R + 6f, 2 * R + 40f, 18f), sf);
        using (var fc = new Font("Segoe UI", 9f))
        using (var cb = new SolidBrush(cMuted))
            g.DrawString("مسیر حرکت ماوس (1.5 ثانیه‌ی اخیر)", fc, cb, new RectangleF(-R - 20f, R + 26f, 2 * R + 40f, 18f), sf);
        g.Restore(st);
    }

    private List<string> GetPressedList(bool isXbox)
    {
        var list = new List<string>();
        if (isXbox)
        {
            if (_s.A) list.Add("A");
            if (_s.B) list.Add("B");
            if (_s.X) list.Add("X");
            if (_s.Y) list.Add("Y");
            if (_s.LB) list.Add("LB");
            if (_s.RB) list.Add("RB");
            if (_s.LT) list.Add("LT");
            if (_s.RT) list.Add("RT");
            if (_s.BACK) list.Add("BACK");
            if (_s.START) list.Add("START");
            if (_s.GUIDE) list.Add("Xbox");
        }
        else
        {
            if (_s.A) list.Add("✕ (ضربدر)");
            if (_s.B) list.Add("○ (دایره)");
            if (_s.X) list.Add("□ (مربع)");
            if (_s.Y) list.Add("△ (مثلث)");
            if (_s.LB) list.Add("L1");
            if (_s.RB) list.Add("R1");
            if (_s.LT) list.Add("L2");
            if (_s.RT) list.Add("R2");
            if (_s.BACK) list.Add("SHARE");
            if (_s.START) list.Add("OPTIONS");
            if (_s.GUIDE) list.Add("PS");
            if (_s.TP) list.Add("Touchpad");
        }

        if (_s.L3) list.Add("L3");
        if (_s.R3) list.Add("R3");
        if (_s.DU) list.Add("بالا");
        if (_s.DD) list.Add("پایین");
        if (_s.DL) list.Add("چپ");
        if (_s.DR) list.Add("راست");
        return list;
    }

    private static GraphicsPath RoundRect(RectangleF r, float rad)
    {
        float d = Math.Min(rad * 2f, Math.Min(r.Width, r.Height));
        var path = new GraphicsPath();
        path.AddArc(r.X, r.Y, d, d, 180, 90);
        path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
}