// HSH Gamepad | Telegram: @HSHGamepad
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace GamepadServer;

/// <summary>پنجره‌ی وضعیت (تم تیره مثل صفحه‌ی گوشی): پلیرها، اسم گوشی‌ها، دکمه‌ی حذف دسته‌ی آزاد و تنظیمات</summary>
public sealed class MainForm : Form
{
    // رنگ‌ها (هماهنگ با صفحه‌ی گوشی)
    private static readonly Color cBg = Color.FromArgb(18, 21, 29);
    private static readonly Color cPanel = Color.FromArgb(27, 32, 48);
    private static readonly Color cCell = Color.FromArgb(24, 28, 41);
    private static readonly Color cOwned = Color.FromArgb(27, 56, 44);
    private static readonly Color cFg = Color.FromArgb(230, 233, 242);
    private static readonly Color cMuted = Color.FromArgb(141, 151, 179);
    private static readonly Color cAccent = Color.FromArgb(91, 146, 255);
    private static readonly Color cGrid = Color.FromArgb(43, 50, 71);
    private static readonly Color cDanger = Color.FromArgb(96, 44, 54);
    private static readonly Color cDangerBorder = Color.FromArgb(150, 70, 80);
    private static readonly Color cDangerFg = Color.FromArgb(255, 200, 200);

    // ترتیب ستون‌ها از چپ به راست
    private const int ColPlayer = 0, ColStatus = 1, ColName = 2, ColType = 3, ColDevice = 4, ColIp = 5;

    private readonly float _k;      // ضریب DPI (۱.۰ = ۹۶ دی‌پی‌آی)
    private readonly Hub _hub;
    private readonly AppSettings _settings;
    private readonly DataGridView _grid;
    private readonly Label _summary;
    private readonly CheckBox _autoChk;
    private readonly CheckBox _inputChk;
    private readonly NumericUpDown _autoNum;
    private readonly System.Windows.Forms.Timer _timer;
    private readonly Action _onChanged;
    private int _port;
    private readonly TextBox _box;
    private readonly Button _copy;
    private readonly Button _qrBtn;
    private QrForm? _qrForm;
    private readonly NotifyIcon _tray;
    private readonly Font _monoFont = new("Consolas", 11f);
    private string? _ip;
    private string? _url;
    private bool _addrInit;
    private int _ipTick;
    private bool _exiting;
    private bool _tipShown;
    private const string ChannelUrl = "https://t.me/HSHGamepad";
    private const string GithubUrl = "https://github.com/Amirhoshein/hshgamepad";
    private readonly ToolTip _tip = new();
    private const int IpRefreshSeconds = 3;     // هر چند ثانیه IP دوباره چک بشه (فقط وقتی پنجره پیداست)
    private readonly Font _btnFont = new("Segoe UI Semibold", 9f);
    private readonly bool[] _removable = new bool[9];
    private readonly bool[] _owned = new bool[9];
    private readonly Dictionary<int, GamepadTesterForm> _testers = new Dictionary<int, GamepadTesterForm>();
    private readonly string?[] _btnText = new string?[9];

    /// <summary>چک‌باکس بزرگ با تم تیره (چک‌باکس معمولی ویندوز اندازه‌اش رو نمیشه عوض کرد)</summary>
    private sealed class BigCheck : CheckBox
    {
        private readonly int _box, _gap;
        private readonly Color _bg, _border, _accent, _tick;

        public BigCheck(int box, int gap, Color bg, Color border, Color accent, Color tick)
        {
            _box = box; _gap = gap; _bg = bg; _border = border; _accent = accent; _tick = tick;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Cursor = Cursors.Hand;
        }

        public override Size GetPreferredSize(Size proposedSize)
        {
            var t = TextRenderer.MeasureText(Text, Font, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);
            return new Size(_box + _gap + t.Width + 4, Math.Max(_box, t.Height) + 2);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Parent?.BackColor ?? BackColor);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            bool rtl = RightToLeft == RightToLeft.Yes;
            int y = (Height - _box) / 2;
            int x = rtl ? Width - _box - 1 : 1;
            var r = new Rectangle(x, y, _box - 1, _box - 1);

            int rad = Math.Max(3, _box / 5);
            using (var path = new GraphicsPath())
            {
                path.AddArc(r.X, r.Y, rad * 2, rad * 2, 180, 90);
                path.AddArc(r.Right - rad * 2, r.Y, rad * 2, rad * 2, 270, 90);
                path.AddArc(r.Right - rad * 2, r.Bottom - rad * 2, rad * 2, rad * 2, 0, 90);
                path.AddArc(r.X, r.Bottom - rad * 2, rad * 2, rad * 2, 90, 90);
                path.CloseFigure();
                using (var fill = new SolidBrush(Checked ? _accent : _bg)) g.FillPath(fill, path);
                using (var pen = new Pen(Checked ? _accent : _border, 2f)) g.DrawPath(pen, path);
            }

            if (Checked)
            {
                float w = r.Width, h = r.Height;
                using var pen = new Pen(_tick, Math.Max(2.5f, _box / 7f)) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
                g.DrawLines(pen, new[]
                {
                    new PointF(r.X + w * .24f, r.Y + h * .53f),
                    new PointF(r.X + w * .43f, r.Y + h * .72f),
                    new PointF(r.X + w * .78f, r.Y + h * .29f),
                });
            }

            var textRect = rtl
                ? new Rectangle(0, 0, Width - _box - _gap, Height)
                : new Rectangle(_box + _gap, 0, Width - _box - _gap, Height);
            var flags = TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix
                        | (rtl ? TextFormatFlags.Right | TextFormatFlags.RightToLeft : TextFormatFlags.Left);
            TextRenderer.DrawText(g, Text, Font, textRect, ForeColor, flags);
        }
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

    private int S(int px) => (int)Math.Round(px * _k);

    public MainForm(Hub hub, int port, AppSettings settings)
    {
        _k = DeviceDpi / 96f;
        _port = port;
        _hub = hub;
        _settings = settings;

        Text = "HSH Gamepad";
        try
        {
            var asm = System.Reflection.Assembly.GetExecutingAssembly();
            foreach (var n in asm.GetManifestResourceNames())
            {
                if (!n.EndsWith("icon.ico", StringComparison.OrdinalIgnoreCase)) continue;
                using var st = asm.GetManifestResourceStream(n)!;
                Icon = new Icon(st);
                break;
            }
        }
        catch { }
        Font = new Font("Segoe UI", 10f);
        BackColor = cBg;
        ForeColor = cFg;
        StartPosition = FormStartPosition.CenterScreen;

        // اندازه‌ی اولیه: ۹۳۰×۵۷۰ (با توجه به DPI) تا هر ۹ پلیر و IP کامل دیده بشه و اسکرول نخواد
        var wa = Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 1920, 1080);
        ClientSize = new Size(Math.Min(S(930), wa.Width - 20), Math.Min(S(527), wa.Height - 60));
        MinimumSize = new Size(S(780), S(480));

        // ---------- بالا: عنوان + آدرس + کپی + خلاصه ----------
        var top = new Panel { Dock = DockStyle.Top, Height = S(88), BackColor = cPanel };
        var title = new Label
        {
            Text = "HSH Gamepad",
            AutoSize = true,
            Left = S(14),
            Top = S(4),
            ForeColor = cAccent,
            Font = new Font("Segoe UI Semibold", 13f),
        };
        var lbl = new Label
        {
            Text = "برای اتصال ایپی زیر رو داخل مرورگر گوشی خود وارد کنید",
            AutoSize = true,
            Left = S(14),
            Top = S(31),
            ForeColor = cMuted,
        };
        _box = new TextBox
        {
            ReadOnly = true,
            Left = S(14),
            Top = S(57),
            Width = S(380),
            BackColor = cCell,
            ForeColor = cFg,
            BorderStyle = BorderStyle.FixedSingle,
            Font = _monoFont,
            RightToLeft = RightToLeft.No,
        };
        _copy = new Button { Text = "کپی", Left = S(402), Top = S(54), Width = S(84), Height = S(30) };
        StyleButton(_copy, cCell, cAccent, cFg);
        _copy.Click += (_, _) =>
        {
            if (_url == null) return;
            try { Clipboard.SetText(_url); _copy.Text = "کپی شد ✓"; } catch { }
        };
        // دکمه‌ی QR Code دقیقاً بالای دکمه‌ی کپی
        _qrBtn = new Button { Text = "QR Code", Left = S(402), Top = S(18), Width = S(84), Height = S(30) };
        StyleButton(_qrBtn, cCell, cAccent, cFg);
        _qrBtn.Click += (_, _) => ShowQr();
        UpdateAddress();
        _summary = new Label
        {
            AutoSize = true,
            Left = S(502),
            Top = S(59),
            ForeColor = cFg,
            Font = new Font("Segoe UI", 11f, FontStyle.Bold),
        };
        var credit = new Label
        {
            Text = "@amir_hoshein",
            AutoSize = true,
            Top = S(8),
            ForeColor = Color.FromArgb(76, 85, 110),
            Font = new Font("Segoe UI", 8f),
            RightToLeft = RightToLeft.No,
        };
        credit.Left = Math.Max(S(500), S(930) - credit.PreferredWidth - S(16));
        top.Resize += (_, _) => credit.Left = top.Width - credit.Width - S(14);
        top.Controls.AddRange(new Control[] { title, lbl, _box, _qrBtn, _copy, _summary, credit });

        // ---------- پایین: تنظیمات ----------
        var bottomBar = new Panel { Dock = DockStyle.Bottom, Height = S(60), BackColor = cPanel };
        var bottom = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = cPanel,
            RightToLeft = RightToLeft.Yes,
            FlowDirection = FlowDirection.LeftToRight,   // با RTL از راست شروع میشه
            WrapContents = true,
            Padding = new Padding(S(12), S(16), S(12), 0),
        };
        _autoChk = new BigCheck(S(22), S(8), cCell, cMuted, cAccent, cBg)
        {
            Text = "حذف خودکار دسته‌ی رها شده بعد از",
            AutoSize = true,
            ForeColor = cFg,
            Checked = settings.AutoKick,
            Margin = new Padding(S(6), S(2), S(6), 0),
        };
        _autoNum = new NumericUpDown
        {
            Minimum = 1,
            Maximum = 3600,
            Value = Math.Max(1, Math.Min(3600, settings.Seconds)),
            Width = S(70),
            BackColor = cCell,
            ForeColor = cFg,
            BorderStyle = BorderStyle.FixedSingle,
            RightToLeft = RightToLeft.No,
            Enabled = settings.AutoKick,
            Margin = new Padding(S(6), S(2), S(6), 0),
        };
        var sec = new Label { Text = "ثانیه", AutoSize = true, ForeColor = cFg, Margin = new Padding(S(6), S(4), S(6), 0) };
        _inputChk = new BigCheck(S(22), S(8), cCell, cMuted, cAccent, cBg)
        {
            Text = "دسترسی ماوس و کیبورد از گوشی",
            AutoSize = true,
            ForeColor = cFg,
            Checked = settings.AllowInput,
            Margin = new Padding(S(16), S(2), S(6), 0),
        };
        _tip.SetToolTip(_inputChk, "فقط وقتی روشنه که گوشیِ پلیر ۱ روی «ماوس» یا «کیبورد» باشه؛ اون‌وقت میتونه کل PC رو کنترل کنه. تا لازم نداری خاموش نگهش دار.");
        bottom.Controls.AddRange(new Control[] { _autoChk, _autoNum, sec, _inputChk });

        // دو دکمه‌ی آیکونی پایین سمت چپ: تلگرام (چپ) و گیت‌هاب (سمت راستش)
        int ib = S(32), ig = S(6), ic = S(28);      // دکمه ۳۲، آیکون ۲۸ پیکسل؛ وسط‌چینِ عمودی با ردیف چک‌باکس
        int itop = S(30) - ib / 2;
        var tgHost = new Panel { Dock = DockStyle.Left, Width = S(10) + ib + ig + ib + S(8), BackColor = cPanel };
        var tg = MakeIconButton("telegram.png", ic, ib, S(10), itop, ChannelUrl, Color.FromArgb(42, 171, 238));
        var gh = MakeIconButton("github.png", ic, ib, S(10) + ib + ig, itop, GithubUrl, cFg);
        tgHost.Controls.Add(tg);
        tgHost.Controls.Add(gh);
        bottomBar.Controls.Add(bottom);     // اول Fill بعد لبه‌ی چپ
        bottomBar.Controls.Add(tgHost);

        _autoChk.CheckedChanged += (_, _) => ApplySettings();
        _autoNum.ValueChanged += (_, _) => ApplySettings();
        _inputChk.CheckedChanged += (_, _) => ApplySettings();

        // ---------- وسط: جدول ۹ پلیر ----------
        _grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            AllowUserToResizeColumns = false,
            RowHeadersVisible = false,
            MultiSelect = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            BorderStyle = BorderStyle.None,
            CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
            BackgroundColor = cBg,
            GridColor = cGrid,
            EnableHeadersVisualStyles = false,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
            ColumnHeadersHeight = S(36),
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            ScrollBars = ScrollBars.None,
            TabStop = false,
        };
        _grid.RowTemplate.Height = S(38);
        _grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = cPanel,
            ForeColor = cMuted,
            SelectionBackColor = cPanel,
            SelectionForeColor = cMuted,
            Font = new Font("Segoe UI Semibold", 10f),
            Alignment = DataGridViewContentAlignment.MiddleLeft,
        };
        _grid.DefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = cCell,
            ForeColor = cFg,
            SelectionBackColor = cCell,
            SelectionForeColor = cFg,
            Padding = new Padding(S(6), 0, S(6), 0),
        };

        // پلیر ، وضعیت ، اسم ، نوع دسته ، دستگاه ، IP
        AddText("پلیر", 70);
        AddText("وضعیت", 235);
        AddText("اسم", 130);
        AddText("نوع دسته", 150);
        AddText("دستگاه", 170);
        AddText("IP", 150);

        for (int i = 0; i < 9; i++)
        {
            int idx = _grid.Rows.Add();
            _grid.Rows[idx].Cells[ColPlayer].Value = "پلیر " + (i + 1);
        }

        // دکمه‌ی «حذف» داخل خود ستون وضعیت کشیده میشه (فقط وقتی دسته‌ی آزاد هنوز مجازی هست)
        _grid.CellPainting += (_, e) =>
        {
            if (e.RowIndex < 0 || e.RowIndex >= 9 || e.ColumnIndex != ColStatus || !_removable[e.RowIndex]) return;

            e.Paint(e.CellBounds, e.PaintParts);
            var r = ButtonRect(e.CellBounds);
            using (var fill = new SolidBrush(cDanger)) e.Graphics!.FillRectangle(fill, r);
            using (var pen = new Pen(cDangerBorder)) e.Graphics!.DrawRectangle(pen, r.X, r.Y, r.Width - 1, r.Height - 1);
            TextRenderer.DrawText(e.Graphics, _btnText[e.RowIndex] ?? "حذف", _btnFont, r, cDangerFg,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
            e.Handled = true;
        };
        _grid.CellMouseClick += (_, e) =>
        {
            if (HitButton(e.RowIndex, e.ColumnIndex, e.Location)) _hub.RemovePad(e.RowIndex + 1);
        };
        _grid.CellMouseMove += (_, e) =>
        {
            _grid.Cursor = HitButton(e.RowIndex, e.ColumnIndex, e.Location) ? Cursors.Hand : Cursors.Default;
        };
        _grid.CellMouseLeave += (_, _) => _grid.Cursor = Cursors.Default;
        _grid.CellMouseDoubleClick += (_, e) =>
        {
            if (e.RowIndex < 0 || e.RowIndex >= 9) return;
            if (HitButton(e.RowIndex, e.ColumnIndex, e.Location)) return;
            if (!_owned[e.RowIndex]) return;
            OpenTester(e.RowIndex + 1);
        };

        Controls.Add(_grid);     // اول Fill بعد لبه‌ها
        Controls.Add(bottomBar);
        Controls.Add(top);

        // ---------- آیکون کنار ساعت (tray): با بستن پنجره برنامه همین‌جا میمونه ----------
        var menu = new ContextMenuStrip { RightToLeft = RightToLeft.Yes };
        var openItem = new ToolStripMenuItem("باز کردن") { Font = new Font(Font, FontStyle.Bold) };
        var exitItem = new ToolStripMenuItem("خروج");
        menu.ImageScalingSize = new Size(S(16), S(16));
        var exitImg = LoadResourceIcon("exit-dark.ico", new Size(S(16), S(16)));     // منوی tray روشنه => آیکون تیره
        if (exitImg != null) exitItem.Image = exitImg.ToBitmap();
        openItem.Click += (_, _) => ShowFromTray();
        exitItem.Click += (_, _) => ExitApp();
        menu.Items.Add(openItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(exitItem);
        _tray = new NotifyIcon
        {
            Text = "HSH Gamepad",
            Icon = LoadIcon(SystemInformation.SmallIconSize) ?? Icon ?? SystemIcons.Application,
            ContextMenuStrip = menu,
            Visible = true,
        };
        _tray.MouseClick += (_, e) =>
        {
            if (e.Button == MouseButtons.Left) ShowFromTray();
        };

        // ---------- به‌روزرسانی ----------
        _onChanged = () =>
        {
            try
            {
                if (IsHandleCreated && !IsDisposed) BeginInvoke(new Action(RefreshList));
            }
            catch { }
        };
        _hub.Changed += _onChanged;

        _timer = new System.Windows.Forms.Timer { Interval = 1000 };
        _timer.Tick += (_, _) =>
        {
            _hub.Tick();        // حذف خودکار دسته‌های آزاد (حتی وقتی پنجره تو tray پنهانه)
            RefreshList();      // شمارش معکوس

            // IP فقط وقتی پنجره پیداست هر چند ثانیه یه بار بروز میشه
            if (Visible && WindowState != FormWindowState.Minimized && ++_ipTick >= IpRefreshSeconds)
            {
                _ipTick = 0;
                UpdateAddress();
            }
        };
        _timer.Start();

        FormClosed += (_, _) =>
        {
            _timer.Stop();
            _timer.Dispose();
            _hub.Changed -= _onChanged;
            _tray.Visible = false;      // وگرنه آیکون تا وقتی ماوس روش نره تو tray میمونه
            _tray.Dispose();
        };

        RefreshList();
        _grid.ClearSelection();
    }

    /// <summary>دکمه‌ی فقط-آیکون (بدون کادر): معمولاً هم‌رنگ متن‌های کم‌رنگ پنجره‌ست و با رفتن ماوس روش، رنگ خودش (hover) رو میگیره</summary>
    private Button MakeIconButton(string resource, int iconSize, int size, int left, int top, string url, Color hover)
    {
        var b = new Button { Left = left, Top = top, Width = size, Height = size, Text = "", TabStop = false };
        StyleButton(b, cPanel, cPanel, cFg);
        b.FlatAppearance.BorderSize = 0;
        b.FlatAppearance.MouseOverBackColor = cPanel;
        b.FlatAppearance.MouseDownBackColor = cPanel;
        var idle = LoadResourceImage(resource, iconSize, cMuted);
        var lit = LoadResourceImage(resource, iconSize, hover);
        if (idle != null && lit != null)
        {
            b.Image = idle; b.ImageAlign = ContentAlignment.MiddleCenter;
            b.MouseEnter += (_, _) => b.Image = lit;
            b.MouseLeave += (_, _) => b.Image = idle;
        }
        _tip.SetToolTip(b, url);
        b.Click += (_, _) => OpenUrl(url, b);
        return b;
    }

    private void OpenUrl(string url, Control c)
    {
        try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
        catch
        {
            // مرورگر پیش‌فرض نبود: لینک رو کپی کن
            try { Clipboard.SetText(url); _tip.Show("لینک کپی شد ✓", c, 0, -c.Height, 1500); } catch { }
        }
    }

    /// <summary>PNG جاسازی‌شده رو با کیفیت بالا به اندازه‌ی دلخواه (پیکسل) برمیگردونه</summary>
    private static Bitmap? LoadResourceImage(string name, int size, Color tint)
    {
        try
        {
            var asm = System.Reflection.Assembly.GetExecutingAssembly();
            foreach (var n in asm.GetManifestResourceNames())
            {
                if (!n.EndsWith(name, StringComparison.OrdinalIgnoreCase)) continue;
                using var st = asm.GetManifestResourceStream(n)!;
                using var src = new Bitmap(st);
                var bmp = new Bitmap(size, size, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                using var g = Graphics.FromImage(bmp);
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                using var attr = new System.Drawing.Imaging.ImageAttributes();
                // PNG سفیدِ تک‌رنگه؛ فقط شکلش (آلفا) مهمه و رنگش با tint عوض میشه
                var cm = new System.Drawing.Imaging.ColorMatrix(new float[][]
                {
                    new float[] { 0, 0, 0, 0, 0 },
                    new float[] { 0, 0, 0, 0, 0 },
                    new float[] { 0, 0, 0, 0, 0 },
                    new float[] { 0, 0, 0, 1, 0 },
                    new float[] { tint.R / 255f, tint.G / 255f, tint.B / 255f, 0, 1 },
                });
                attr.SetColorMatrix(cm, System.Drawing.Imaging.ColorMatrixFlag.Default, System.Drawing.Imaging.ColorAdjustType.Bitmap);
                g.DrawImage(src, new Rectangle(0, 0, size, size), 0, 0, src.Width, src.Height, GraphicsUnit.Pixel, attr);
                return bmp;
            }
        }
        catch { }
        return null;
    }

    /// <summary>پنجره رو از tray برمیگردونه (کلیک روی آیکون، منوی «باز کردن»، یا اجرای دوباره‌ی برنامه)</summary>
    public void ShowFromTray()
    {
        if (!_allowShow)
        {
            _allowShow = true;
            if (!Visible) CenterToScreen();     // اولین نمایش بعد از شروع مخفی: وسط صفحه بیاد
        }
        if (!Visible) Show();
        if (WindowState == FormWindowState.Minimized) WindowState = FormWindowState.Normal;
        Activate();
    }

    private bool _allowShow = true;

    /// <summary>برنامه بدون نمایش پنجره بالا بیاد (فقط آیکون tray)؛ تا ShowFromTray صدا زده نشه پنجره پیدا نمیشه</summary>
    public void StartHidden() { _allowShow = false; }

    protected override void SetVisibleCore(bool value)
    {
        if (!_allowShow)
        {
            if (!IsHandleCreated) CreateHandle();     // وگرنه BeginInvoke و بقیه‌ی کارها قبل از اولین نمایش کار نمیکنن
            value = false;
        }
        base.SetVisibleCore(value);
    }

    /// <summary>پورت سرور عوض شد (مثلاً از 1111 رفت رو 80): آدرس بالای پنجره فوراً آپدیت میشه</summary>
    public void SetPort(int port)
    {
        _port = port;
        _addrInit = false;      // وگرنه UpdateAddress چون IP عوض نشده چیزی رو بروز نمیکنه
        UpdateAddress();
    }

    /// <summary>خروج واقعی از برنامه (بستن پنجره فقط پنهانش میکنه)</summary>
    public void ExitApp()
    {
        _exiting = true;
        Close();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        base.OnFormClosing(e);
        if (e.Cancel || _exiting || e.CloseReason != CloseReason.UserClosing) return;   // خاموش شدن ویندوز و ... عادی بسته میشه

        e.Cancel = true;
        Hide();
        if (!_tipShown)
        {
            _tipShown = true;
            try
            {
                _tray.ShowBalloonTip(3000, "HSH Gamepad", "برنامه هنوز در حال اجراست. برای بستنش روی آیکون کنار ساعت راست‌کلیک کن.", ToolTipIcon.Info);
            }
            catch { }
        }
    }

    protected override void OnVisibleChanged(EventArgs e)
    {
        base.OnVisibleChanged(e);
        if (Visible)
        {
            _ipTick = 0;
            UpdateAddress();    // موقع برگشتن از tray فوراً IP رو چک کن
        }
    }

    /// <summary>آدرس بالای پنجره: IP شبکه یا «منتظر اتصال به شبکه» اگه شبکه‌ای نیست</summary>
    private void UpdateAddress()
    {
        string? ip = NetInfo.LanIp();
        if (_addrInit && ip == _ip) return;
        _addrInit = true;
        _ip = ip;
        _url = ip == null ? null : NetInfo.Url(ip, _port);

        if (_url == null)
        {
            _box.ForeColor = cMuted;
            _box.Text = "منتظر اتصال به شبکه";
            _copy.Enabled = false;
            _qrBtn.Enabled = false;
        }
        else
        {
            _box.ForeColor = cFg;
            _box.Text = _url;
            _copy.Enabled = true;
            _qrBtn.Enabled = true;
        }
        _copy.Text = "کپی";
        if (_qrForm != null && !_qrForm.IsDisposed) _qrForm.SetUrl(_url);      // IP عوض شد: QR باز هم بروز بشه
    }

    private void OpenTester(int playerN)
    {
        if (_testers.TryGetValue(playerN, out var existing) && !existing.IsDisposed)
        {
            if (existing.WindowState == FormWindowState.Minimized)
                existing.WindowState = FormWindowState.Normal;
            existing.Activate();
            return;
        }

        var tester = new GamepadTesterForm(_hub, playerN, Icon);
        tester.FormClosed += (_, _) => _testers.Remove(playerN);
        _testers[playerN] = tester;
        tester.Show(this);
    }

    private static Icon? LoadResourceIcon(string name, Size size)
    {
        try
        {
            var asm = System.Reflection.Assembly.GetExecutingAssembly();
            foreach (var n in asm.GetManifestResourceNames())
            {
                if (!n.EndsWith(name, StringComparison.OrdinalIgnoreCase)) continue;
                using var st = asm.GetManifestResourceStream(n)!;
                return new Icon(st, size);
            }
        }
        catch { }
        return null;
    }

    private static Icon? LoadIcon(Size size)
    {
        try
        {
            var asm = System.Reflection.Assembly.GetExecutingAssembly();
            foreach (var n in asm.GetManifestResourceNames())
            {
                if (!n.EndsWith("icon.ico", StringComparison.OrdinalIgnoreCase)) continue;
                using var st = asm.GetManifestResourceStream(n)!;
                return new Icon(st, size);
            }
        }
        catch { }
        return null;
    }

    private Rectangle ButtonRect(Rectangle cell)
    {
        int w = S(74), pad = S(8);
        return new Rectangle(cell.Right - w - pad, cell.Top + S(6), w, cell.Height - S(12));
    }

    private bool HitButton(int row, int col, Point p)
    {
        if (row < 0 || row >= 9 || col != ColStatus || !_removable[row]) return false;
        var cell = new Rectangle(0, 0, _grid.Columns[ColStatus].Width, _grid.Rows[row].Height);
        return ButtonRect(cell).Contains(p);
    }

    private void AddText(string header, int weight)
    {
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = header,
            FillWeight = weight,
            SortMode = DataGridViewColumnSortMode.NotSortable,
        });
    }

    private static void StyleButton(Button b, Color back, Color border, Color fore)
    {
        b.FlatStyle = FlatStyle.Flat;
        b.FlatAppearance.BorderColor = border;
        b.FlatAppearance.BorderSize = 1;
        b.UseVisualStyleBackColor = false;
        b.BackColor = back;
        b.ForeColor = fore;
        b.Cursor = Cursors.Hand;
    }

    private void ApplySettings()
    {
        _autoNum.Enabled = _autoChk.Checked;
        _settings.AutoKick = _autoChk.Checked;
        _settings.Seconds = (int)_autoNum.Value;
        _hub.AutoKickSeconds = _settings.AutoKick ? _settings.Seconds : 0;
        _settings.AllowInput = _inputChk.Checked;
        _hub.AllowInput = _settings.AllowInput;
        if (!_settings.AllowInput) InputInjector.ReleaseAll();
        _settings.Save();
    }

    // نوار عنوان تیره (ویندوز ۱۰ به بالا)
    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        try
        {
            int on = 1;
            if (DwmSetWindowAttribute(Handle, 20, ref on, 4) != 0) DwmSetWindowAttribute(Handle, 19, ref on, 4);
            int cap = cBg.R | (cBg.G << 8) | (cBg.B << 16);          // رنگ نوار عنوان (ویندوز ۱۱)
            DwmSetWindowAttribute(Handle, 35, ref cap, 4);
        }
        catch { }
    }

    /// <summary>پنجره‌ی QR Code: خود QR (۳۲۰×۳۲۰)، زیرش همون آدرس + دکمه‌ی کپی و یه توضیح کوتاه</summary>
    private void ShowQr()
    {
        if (_url == null) return;
        if (_qrForm != null && !_qrForm.IsDisposed) { _qrForm.SetUrl(_url); _qrForm.Activate(); return; }
        var f = new QrForm(_url, _k) { Icon = Icon };
        f.FormClosed += (_, _) => { if (ReferenceEquals(_qrForm, f)) _qrForm = null; };
        _qrForm = f;
        f.Show(this);
    }

    private sealed class QrForm : Form
    {
        private readonly TextBox _addr;
        private readonly Button _copyBtn;
        private readonly QrBox _qr;
        private readonly Font _mono = new Font("Consolas", 11f);
        private string? _u;

        public QrForm(string? url, float k)
        {
            int side = (int)Math.Round(320 * k), pad = (int)Math.Round(16 * k);
            int btnW = (int)Math.Round(84 * k), btnH = (int)Math.Round(30 * k), gap = (int)Math.Round(8 * k);
            Text = "QR Code";
            BackColor = cPanel;
            ForeColor = cFg;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            int hintTop = pad + side + pad + btnH + (int)Math.Round(10 * k), hintH = (int)Math.Round(46 * k);
            ClientSize = new Size(side + 2 * pad, hintTop + hintH + pad);

            _qr = new QrBox { Left = pad, Top = pad, Width = side, Height = side };
            _addr = new TextBox
            {
                ReadOnly = true,
                Left = pad,
                Top = pad + side + pad + (int)Math.Round(3 * k),
                Width = side - btnW - gap,
                BackColor = cCell,
                ForeColor = cFg,
                BorderStyle = BorderStyle.FixedSingle,
                Font = _mono,
                RightToLeft = RightToLeft.No,
            };
            _copyBtn = new Button { Text = "کپی", Left = pad + side - btnW, Top = pad + side + pad, Width = btnW, Height = btnH };
            StyleButton(_copyBtn, cCell, cAccent, cFg);
            _copyBtn.Click += (_, _) =>
            {
                if (_u == null) return;
                try { Clipboard.SetText(_u); _copyBtn.Text = "کپی شد ✓"; } catch { }
            };
            var hint = new Label
            {
                Text = "با گوشی خود QR Code رو اسکن کنید و لینک رو تو مرورگر باز کنید.\nگوشی و کامپیوتر باید به یک شبکه‌ی وای‌فای وصل باشن.",
                Left = pad,
                Top = hintTop,
                Width = side,
                Height = hintH,
                ForeColor = cMuted,
                RightToLeft = RightToLeft.Yes,
                TextAlign = ContentAlignment.TopCenter,
            };
            Controls.AddRange(new Control[] { _qr, _addr, _copyBtn, hint });
            SetUrl(url);
        }

        public void SetUrl(string? url)
        {
            _u = url;
            _addr.ForeColor = url == null ? cMuted : cFg;
            _addr.Text = url ?? "منتظر اتصال به شبکه";
            _copyBtn.Enabled = url != null;
            _copyBtn.Text = "کپی";
            _qr.SetText(url);
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            try
            {
                int on = 1;
                if (DwmSetWindowAttribute(Handle, 20, ref on, 4) != 0) DwmSetWindowAttribute(Handle, 19, ref on, 4);
                int cap = cBg.R | (cBg.G << 8) | (cBg.B << 16);
                DwmSetWindowAttribute(Handle, 35, ref cap, 4);
            }
            catch { }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _mono.Dispose();
            base.Dispose(disposing);
        }
    }

    private static string ModeText(SlotInfo s)
    {
        return s.Mode switch
        {
            "xbox" => "Xbox 360",
            "wheel" => "Steering Wheel (Xbox 360)",
            "mouse" => "Mouse",
            "keyboard" => "Keyboard",
            "ps4" => s.FellBack ? "PS4 (جایگزین Xbox)" : "PS4 (DualShock 4)",
            _ => "—",
        };
    }

    private static void SetCell(DataGridViewRow row, int col, string text)
    {
        if ((row.Cells[col].Value as string) != text) row.Cells[col].Value = text;
    }

    private void RefreshList()
    {
        List<SlotInfo> snap = _hub.Snapshot();
        int auto = _hub.AutoKickSeconds;
        int connected = 0;

        foreach (var s in snap)
        {
            int i = s.N - 1;
            var row = _grid.Rows[i];
            bool owned = s.Device != null;
            _owned[i] = owned;
            _removable[i] = !owned && s.HasPad;

            string status;
            Color back, fore;
            if (owned)
            {
                connected++;
                status = "متصل";
                back = cOwned;
                fore = cFg;
            }
            else if (s.HasPad)
            {
                status = "رها شده";
                back = cCell;
                fore = cMuted;
                // اگه حذف خودکار روشنه، شمارش معکوس روی خود دکمه نشون داده میشه
                _btnText[i] = (auto > 0 && s.IdleSeconds is double idle)
                    ? $"حذف ({Math.Max(0, auto - (int)idle)})"
                    : "حذف";
            }
            else
            {
                status = "آزاد";
                back = cCell;
                fore = cMuted;
            }

            SetCell(row, ColStatus, status);
            SetCell(row, ColName, owned && !string.IsNullOrEmpty(s.Name) ? s.Name! : "—");
            SetCell(row, ColType, (owned || s.HasPad) ? ModeText(s) : "—");
            SetCell(row, ColDevice, owned ? s.Device! : "—");
            SetCell(row, ColIp, owned ? (s.Ip ?? "") : "—");

            var st = row.DefaultCellStyle;
            st.BackColor = back;
            st.ForeColor = fore;
            st.SelectionBackColor = back;
            st.SelectionForeColor = fore;
        }

        _summary.Text = $"متصل: {connected} از 9";
        _grid.InvalidateColumn(ColStatus);
    }
}


/// <summary>
/// QR Code (حالت بایت، سطح تصحیح خطا M، نسخه‌ی ۱ تا ۱۰) بدون هیچ کتابخونه‌ی بیرونی.
/// الگوریتم بر اساس پیاده‌سازی دامنه‌ی عمومی Nayuki (Project Nayuki QR Code generator).
/// </summary>
internal sealed class QrCode
{
    private static readonly int[] EccPerBlock = { -1, 10, 16, 26, 18, 24, 16, 18, 22, 22, 26 };
    private static readonly int[] NumBlocks = { -1, 1, 1, 1, 2, 2, 4, 4, 4, 5, 5 };

    public int Size { get; }
    private readonly bool[,] _mod;      // [y, x]
    private readonly bool[,] _fn;       // جزو الگوهای ثابت (قابل ماسک شدن نیست)

    public bool Get(int x, int y) => x >= 0 && x < Size && y >= 0 && y < Size && _mod[y, x];

    private static int RawModules(int ver)
    {
        int r = (16 * ver + 128) * ver + 64;
        if (ver >= 2)
        {
            int na = ver / 7 + 2;
            r -= (25 * na - 10) * na - 55;
            if (ver >= 7) r -= 36;
        }
        return r;
    }

    private static int DataCodewords(int ver) => RawModules(ver) / 8 - EccPerBlock[ver] * NumBlocks[ver];

    /// <summary>null = متن برای QR خیلی بلنده</summary>
    public static QrCode? Encode(string text)
    {
        byte[] data = System.Text.Encoding.UTF8.GetBytes(text);
        int ver = 0, cc = 8;
        for (int v = 1; v <= 10; v++)
        {
            cc = v < 10 ? 8 : 16;
            if (4 + cc + 8 * data.Length <= DataCodewords(v) * 8) { ver = v; break; }
        }
        if (ver == 0) return null;

        var bits = new List<int>();
        void Put(int val, int n) { for (int i = n - 1; i >= 0; i--) bits.Add((val >> i) & 1); }

        int cap = DataCodewords(ver) * 8;
        Put(4, 4);
        Put(data.Length, cc);
        foreach (byte b in data) Put(b, 8);
        Put(0, Math.Min(4, cap - bits.Count));
        Put(0, (8 - bits.Count % 8) % 8);
        for (int pad = 0xEC; bits.Count < cap; pad ^= 0xEC ^ 0x11) Put(pad, 8);

        var cw = new byte[bits.Count / 8];
        for (int i = 0; i < bits.Count; i++) cw[i >> 3] |= (byte)(bits[i] << (7 - (i & 7)));
        return new QrCode(ver, cw);
    }

    private QrCode(int ver, byte[] data)
    {
        Size = ver * 4 + 17;
        _mod = new bool[Size, Size];
        _fn = new bool[Size, Size];
        DrawFunctionPatterns(ver);
        byte[] all = AddEccAndInterleave(ver, data);
        DrawCodewords(all);

        int best = 0, minPenalty = int.MaxValue;
        for (int m = 0; m < 8; m++)
        {
            ApplyMask(m);
            DrawFormatBits(m);
            int p = Penalty();
            if (p < minPenalty) { best = m; minPenalty = p; }
            ApplyMask(m);       // برگردوندن ماسک
        }
        ApplyMask(best);
        DrawFormatBits(best);
    }

    private void SetFn(int x, int y, bool dark) { _mod[y, x] = dark; _fn[y, x] = true; }

    private static bool GetBit(int x, int i) => ((x >> i) & 1) != 0;

    private void DrawFunctionPatterns(int ver)
    {
        for (int i = 0; i < Size; i++) { SetFn(6, i, i % 2 == 0); SetFn(i, 6, i % 2 == 0); }
        DrawFinder(3, 3); DrawFinder(Size - 4, 3); DrawFinder(3, Size - 4);

        int[] ap = AlignPositions(ver);
        int na = ap.Length;
        for (int i = 0; i < na; i++)
        {
            for (int j = 0; j < na; j++)
            {
                if ((i == 0 && j == 0) || (i == 0 && j == na - 1) || (i == na - 1 && j == 0)) continue;
                for (int dy = -2; dy <= 2; dy++)
                    for (int dx = -2; dx <= 2; dx++)
                        SetFn(ap[i] + dx, ap[j] + dy, Math.Max(Math.Abs(dx), Math.Abs(dy)) != 1);
            }
        }
        DrawFormatBits(0);
        DrawVersion(ver);
    }

    private int[] AlignPositions(int ver)
    {
        if (ver == 1) return new int[0];
        int na = ver / 7 + 2;
        int step = ver == 32 ? 26 : (ver * 4 + na * 2 + 1) / (na * 2 - 2) * 2;
        var r = new int[na];
        r[0] = 6;
        for (int i = na - 1, pos = Size - 7; i >= 1; i--, pos -= step) r[i] = pos;
        return r;
    }

    private void DrawFinder(int x, int y)
    {
        for (int dy = -4; dy <= 4; dy++)
        {
            for (int dx = -4; dx <= 4; dx++)
            {
                int d = Math.Max(Math.Abs(dx), Math.Abs(dy));
                int xx = x + dx, yy = y + dy;
                if (xx >= 0 && xx < Size && yy >= 0 && yy < Size) SetFn(xx, yy, d != 2 && d != 4);
            }
        }
    }

    private void DrawFormatBits(int mask)
    {
        int data = mask;                    // سطح M = بیت‌های 00
        int rem = data;
        for (int i = 0; i < 10; i++) rem = (rem << 1) ^ ((rem >> 9) * 0x537);
        int bits = ((data << 10) | rem) ^ 0x5412;

        for (int i = 0; i <= 5; i++) SetFn(8, i, GetBit(bits, i));
        SetFn(8, 7, GetBit(bits, 6));
        SetFn(8, 8, GetBit(bits, 7));
        SetFn(7, 8, GetBit(bits, 8));
        for (int i = 9; i < 15; i++) SetFn(14 - i, 8, GetBit(bits, i));

        for (int i = 0; i < 8; i++) SetFn(Size - 1 - i, 8, GetBit(bits, i));
        for (int i = 8; i < 15; i++) SetFn(8, Size - 15 + i, GetBit(bits, i));
        SetFn(8, Size - 8, true);
    }

    private void DrawVersion(int ver)
    {
        if (ver < 7) return;
        int rem = ver;
        for (int i = 0; i < 12; i++) rem = (rem << 1) ^ ((rem >> 11) * 0x1F25);
        int bits = (ver << 12) | rem;
        for (int i = 0; i < 18; i++)
        {
            bool bit = GetBit(bits, i);
            int a = Size - 11 + i % 3, b = i / 3;
            SetFn(a, b, bit);
            SetFn(b, a, bit);
        }
    }

    private static byte Multiply(int x, int y)
    {
        int z = 0;
        for (int i = 7; i >= 0; i--)
        {
            z = (z << 1) ^ ((z >> 7) * 0x11D);
            z ^= ((y >> i) & 1) * x;
        }
        return (byte)z;
    }

    private static byte[] RsDivisor(int degree)
    {
        var res = new byte[degree];
        res[degree - 1] = 1;
        int root = 1;
        for (int i = 0; i < degree; i++)
        {
            for (int j = 0; j < degree; j++)
            {
                res[j] = Multiply(res[j], root);
                if (j + 1 < degree) res[j] ^= res[j + 1];
            }
            root = Multiply(root, 2);
        }
        return res;
    }

    private static byte[] RsRemainder(byte[] data, byte[] div)
    {
        var res = new byte[div.Length];
        foreach (byte b in data)
        {
            int f = (b ^ res[0]) & 0xFF;
            Array.Copy(res, 1, res, 0, res.Length - 1);
            res[res.Length - 1] = 0;
            for (int i = 0; i < res.Length; i++) res[i] ^= Multiply(div[i], f);
        }
        return res;
    }

    private static byte[] AddEccAndInterleave(int ver, byte[] data)
    {
        int nb = NumBlocks[ver], el = EccPerBlock[ver], raw = RawModules(ver) / 8;
        int ns = nb - raw % nb, sl = raw / nb;
        byte[] dv = RsDivisor(el);

        var blocks = new List<byte[]>();
        for (int i = 0, k = 0; i < nb; i++)
        {
            int len = sl - el + (i < ns ? 0 : 1);
            var dat = new byte[len];
            Array.Copy(data, k, dat, 0, len);
            k += len;
            byte[] ecc = RsRemainder(dat, dv);
            var blk = new byte[sl + 1];
            Array.Copy(dat, blk, len);
            Array.Copy(ecc, 0, blk, blk.Length - el, el);
            blocks.Add(blk);
        }

        var res = new List<byte>();
        for (int i = 0; i < blocks[0].Length; i++)
            for (int j = 0; j < blocks.Count; j++)
                if (i != sl - el || j >= ns) res.Add(blocks[j][i]);
        return res.ToArray();
    }

    private void DrawCodewords(byte[] data)
    {
        int i = 0;
        for (int right = Size - 1; right >= 1; right -= 2)
        {
            if (right == 6) right = 5;
            for (int vert = 0; vert < Size; vert++)
            {
                for (int j = 0; j < 2; j++)
                {
                    int x = right - j;
                    bool upward = ((right + 1) & 2) == 0;
                    int y = upward ? Size - 1 - vert : vert;
                    if (!_fn[y, x] && i < data.Length * 8)
                    {
                        _mod[y, x] = GetBit(data[i >> 3], 7 - (i & 7));
                        i++;
                    }
                }
            }
        }
    }

    private void ApplyMask(int m)
    {
        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                bool inv;
                switch (m)
                {
                    case 0: inv = (x + y) % 2 == 0; break;
                    case 1: inv = y % 2 == 0; break;
                    case 2: inv = x % 3 == 0; break;
                    case 3: inv = (x + y) % 3 == 0; break;
                    case 4: inv = (x / 3 + y / 2) % 2 == 0; break;
                    case 5: inv = x * y % 2 + x * y % 3 == 0; break;
                    case 6: inv = (x * y % 2 + x * y % 3) % 2 == 0; break;
                    default: inv = ((x + y) % 2 + x * y % 3) % 2 == 0; break;
                }
                _mod[y, x] ^= inv && !_fn[y, x];
            }
        }
    }

    private int Penalty()
    {
        int r = 0;
        for (int y = 0; y < Size; y++)
        {
            bool col = false; int run = 0; var h = new int[7];
            for (int x = 0; x < Size; x++)
            {
                if (_mod[y, x] == col) { run++; if (run == 5) r += 3; else if (run > 5) r++; }
                else { HistAdd(run, h); if (!col) r += CountPatterns(h) * 40; col = _mod[y, x]; run = 1; }
            }
            r += Terminate(col, run, h) * 40;
        }
        for (int x = 0; x < Size; x++)
        {
            bool col = false; int run = 0; var h = new int[7];
            for (int y = 0; y < Size; y++)
            {
                if (_mod[y, x] == col) { run++; if (run == 5) r += 3; else if (run > 5) r++; }
                else { HistAdd(run, h); if (!col) r += CountPatterns(h) * 40; col = _mod[y, x]; run = 1; }
            }
            r += Terminate(col, run, h) * 40;
        }
        for (int y = 0; y < Size - 1; y++)
            for (int x = 0; x < Size - 1; x++)
            {
                bool c = _mod[y, x];
                if (c == _mod[y, x + 1] && c == _mod[y + 1, x] && c == _mod[y + 1, x + 1]) r += 3;
            }
        int dark = 0;
        foreach (bool v in _mod) if (v) dark++;
        int total = Size * Size;
        int k = (Math.Abs(dark * 20 - total * 10) + total - 1) / total - 1;
        return r + k * 10;
    }

    private void HistAdd(int run, int[] h)
    {
        if (h[0] == 0) run += Size;
        Array.Copy(h, 0, h, 1, 6);
        h[0] = run;
    }

    private static int CountPatterns(int[] h)
    {
        int n = h[1];
        bool core = n > 0 && h[2] == n && h[3] == n * 3 && h[4] == n && h[5] == n;
        return (core && h[0] >= n * 4 && h[6] >= n ? 1 : 0) + (core && h[6] >= n * 4 && h[0] >= n ? 1 : 0);
    }

    private int Terminate(bool color, int run, int[] h)
    {
        if (color) { HistAdd(run, h); run = 0; }
        run += Size;
        HistAdd(run, h);
        return CountPatterns(h);
    }
}

/// <summary>کنترل سفید با QR مشکی؛ حاشیه‌ی سفید ۲ ماژول و ماژول‌ها کل کنترل رو پر میکنن (مرز ماژول‌ها به پیکسل گرد میشه)</summary>
internal sealed class QrBox : Control
{
    private QrCode? _qr;

    public QrBox()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        BackColor = Color.White;
    }

    public void SetText(string? text)
    {
        _qr = string.IsNullOrEmpty(text) ? null : QrCode.Encode(text!);
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(Color.White);
        if (_qr == null) return;
        int n = _qr.Size, q = 2, total = n + 2 * q;      // حاشیه‌ی سفید ۲ ماژول؛ QR کل کنترل رو پر میکنه
        int w = Width, h = Height;
        g.SmoothingMode = SmoothingMode.None;
        using (var br = new SolidBrush(Color.Black))
        {
            for (int y = 0; y < n; y++)
            {
                int y0 = (int)Math.Round((y + q) * (double)h / total), y1 = (int)Math.Round((y + q + 1) * (double)h / total);
                for (int x = 0; x < n; x++)
                {
                    if (!_qr.Get(x, y)) continue;
                    int x0 = (int)Math.Round((x + q) * (double)w / total), x1 = (int)Math.Round((x + q + 1) * (double)w / total);
                    g.FillRectangle(br, x0, y0, x1 - x0, y1 - y0);
                }
            }
        }
    }
}
