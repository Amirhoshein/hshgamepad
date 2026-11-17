using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace GamepadServer;

public sealed class MainForm : Form
{
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

    private const int ColPlayer = 0, ColStatus = 1, ColName = 2, ColType = 3, ColDevice = 4, ColIp = 5;

    private readonly float _k;
    private readonly Hub _hub;
    private readonly AppSettings _settings;
    private readonly DataGridView _grid;
    private readonly Label _summary;
    private readonly CheckBox _autoChk;
    private readonly CheckBox _inputChk;
    private readonly NumericUpDown _autoNum;
    private readonly CheckBox _keyChk;
    private readonly KeyBox _keyBox;
    private readonly CheckBox _banChk;
    private readonly NumericUpDown _triesNum;
    private readonly NumericUpDown _minutesNum;
    private readonly Label _triesLbl;
    private readonly Label _minutesLbl;
    private int _removeW;
    private readonly System.Windows.Forms.Timer _timer;
    private readonly Action _onChanged;
    private int _port;
    private readonly TextBox _box;
    private readonly Button _copy;
    private readonly Button _qrBtn;
    private QrForm? _qrForm;
    private readonly Button _settingsBtn;
    private readonly SettingsForm _settingsForm;
    private Label _lblAddr = null!;
    private Font? _topBtnFont;
    private Label _secLbl = null!;
    private ToolStripMenuItem _openItem = null!;
    private ToolStripMenuItem _exitItem = null!;
    private LanguageForm? _langForm;
    private readonly Action _onLangChanged;
    private readonly NotifyIcon _tray;
    private readonly Font _monoFont = new("Consolas", 11f);
    private string? _ip;
    private string? _url;
    private bool _addrInit;
    private int _ipTick;

    private bool _fwPending = true;
    private int _fwDelay = 6;
    private int _fwBusy;
    private volatile string? _fwAsked;
    private NetworkAddressChangedEventHandler? _onAddrChanged;
    private NetworkAvailabilityChangedEventHandler? _onAvailChanged;
    private bool _exiting;
    private bool _tipShown;
    private const string ChannelUrl = "https://t.me/HSHGamepad";
    private const string GithubUrl = "https://github.com/Amirhoshein/hshgamepad";
    private readonly ToolTip _tip = new();
    private const int IpRefreshSeconds = 3;
    private readonly Font _btnFont = new("Segoe UI Semibold", 9f);
    private readonly bool[] _removable = new bool[9];
    private readonly bool[] _owned = new bool[9];
    private readonly Dictionary<int, GamepadTesterForm> _testers = new Dictionary<int, GamepadTesterForm>();
    private readonly string?[] _btnText = new string?[9];

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
            bool on = Enabled;
            Color accent = on ? _accent : _border;
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
                using (var fill = new SolidBrush(Checked ? accent : _bg)) g.FillPath(fill, path);
                using (var pen = new Pen(Checked ? accent : _border, 2f)) g.DrawPath(pen, path);
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
            TextRenderer.DrawText(g, Text, Font, textRect, on ? ForeColor : _border, flags);
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

        var wa = Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 1920, 1080);
        ClientSize = new Size(Math.Min(S(930), wa.Width - 20), Math.Min(S(527), wa.Height - 60));
        MinimumSize = new Size(S(780), S(480));

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
            Text = L.T("main.EnterTheAddressBelowIn"),
            AutoSize = false,
            AutoEllipsis = true,
            Left = S(14),
            Top = S(31),
            Height = S(22),
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
        _copy = new Button { Text = L.T("main.Copy"), Left = S(402), Top = S(54), Width = S(84), Height = S(30) };
        StyleButton(_copy, cCell, cAccent, cFg);
        _copy.Click += (_, _) =>
        {
            if (_url == null) return;
            try { Clipboard.SetText(_url); _copy.Text = L.T("main.Copied"); } catch { }
        };
        _qrBtn = new Button { Text = L.T("ui.qr"), Left = S(402), Top = S(18), Width = S(84), Height = S(30) };
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
        top.Resize += (_, _) => { credit.Left = top.Width - credit.Width - S(14); LayoutTop(); };
        top.Controls.AddRange(new Control[] { title, lbl, _box, _qrBtn, _copy, _summary, credit });
        _lblAddr = lbl;

        var bottomBar = new Panel { Dock = DockStyle.Bottom, Height = S(60), BackColor = cPanel };
        _autoChk = new BigCheck(S(22), S(8), cCell, cMuted, cAccent, cBg)
        {
            Text = L.T("main.AutoRemoveAnAbandonedController"),
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
        var sec = new Label { Text = L.T("main.Seconds"), AutoSize = true, ForeColor = cFg, Margin = new Padding(S(6), S(4), S(6), 0) };
        _inputChk = new BigCheck(S(22), S(8), cCell, cMuted, cAccent, cBg)
        {
            Text = L.T("main.MouseAndKeyboardAccessFrom"),
            AutoSize = true,
            ForeColor = cFg,
            Checked = settings.AllowInput,
            Margin = new Padding(S(16), S(2), S(6), 0),
        };
        _tip.SetToolTip(_inputChk, L.T("main.OnlyWorksWhilePlayer1s"));
        _secLbl = sec;
        var rowAuto = SettingsRow(S(40));
        rowAuto.Controls.AddRange(new Control[] { _autoChk, _autoNum, sec });
        var rowInput = SettingsRow(S(40));
        _inputChk.Margin = new Padding(S(6), S(2), S(6), 0);
        rowInput.Controls.Add(_inputChk);

        _keyChk = new BigCheck(S(22), S(8), cCell, cMuted, cAccent, cBg)
        {
            Text = L.T("main.AccessKey"),
            AutoSize = true,
            ForeColor = cFg,
            Checked = settings.KeyEnabled,
            Margin = new Padding(S(6), S(5), S(6), 0),
        };
        _keyBox = new KeyBox(_k, cCell, cFg, cMuted, Color.FromArgb(40, 48, 70), _monoFont)
        {
            Key = settings.Key,
            Margin = new Padding(S(6), S(1), S(6), 0),
        };
        var rowKey = SettingsRow(S(40));
        rowKey.Padding = new Padding(0, S(4), 0, 0);
        rowKey.Controls.AddRange(new Control[] { _keyChk, _keyBox });

        _banChk = new BigCheck(S(22), S(8), cCell, cMuted, cAccent, cBg)
        {
            Text = L.T("main.BlockWrongTries"),
            AutoSize = true,
            ForeColor = cFg,
            Checked = settings.BanEnabled,
            Margin = new Padding(S(6), S(2), S(6), 0),
        };
        var rowBan = SettingsRow(S(40));
        rowBan.Controls.Add(_banChk);

        _triesLbl = SettingsLabel(L.T("main.MaxTries"), S(42));
        _triesNum = SettingsNumber(1, 1000, settings.BanTries);
        var rowTries = SettingsRow(S(36));
        rowTries.Controls.AddRange(new Control[] { _triesLbl, _triesNum });

        _minutesLbl = SettingsLabel(L.T("main.BlockMinutes"), S(42));
        _minutesNum = SettingsNumber(1, 1440, settings.BanMinutes);
        var rowMinutes = SettingsRow(S(36));
        rowMinutes.Controls.AddRange(new Control[] { _minutesLbl, _minutesNum });

        var separator = new Panel { Height = Math.Max(1, S(1)), BackColor = cGrid, Margin = new Padding(0, S(8), 0, S(4)) };

        _settingsForm = new SettingsForm(_k, Icon,
            new Control[] { rowAuto, rowInput, separator, rowKey, rowBan, rowTries, rowMinutes }, ShowLanguage);

        int ib = S(32), ig = S(6), ic = S(28);
        int itop = S(30) - ib / 2;
        var tgHost = new Panel { Dock = DockStyle.Left, Width = S(10) + ib + ig + ib + S(8), BackColor = cPanel };
        var tg = MakeIconButton("telegram.png", ic, ib, S(10), itop, ChannelUrl, Color.FromArgb(42, 171, 238));
        var gh = MakeIconButton("github.png", ic, ib, S(10) + ib + ig, itop, GithubUrl, cFg);
        tgHost.Controls.Add(tg);
        tgHost.Controls.Add(gh);
        int sbH = S(34);
        var setHost = new Panel { Dock = DockStyle.Right, Width = S(12) + S(120) + S(14), BackColor = cPanel };
        _settingsBtn = new Button { Text = "Settings", Left = S(12), Top = S(30) - sbH / 2, Width = S(120), Height = sbH, Font = _btnFont, TabStop = false };
        StyleButton(_settingsBtn, cCell, cAccent, cFg);
        Fit.Auto(_settingsBtn);
        _settingsBtn.Click += (_, _) => ShowSettings();
        setHost.Controls.Add(_settingsBtn);
        bottomBar.Controls.Add(tgHost);
        bottomBar.Controls.Add(setHost);

        _autoChk.CheckedChanged += (_, _) => ApplySettings();
        _autoNum.ValueChanged += (_, _) => ApplySettings();
        _inputChk.CheckedChanged += (_, _) => ApplySettings();
        _keyChk.CheckedChanged += (_, _) =>
        {
            if (_keyChk.Checked) _banChk.Checked = true;
            ApplySettings();
        };
        _keyBox.KeyChanged += ApplySettings;
        _banChk.CheckedChanged += (_, _) => ApplySettings();
        _triesNum.ValueChanged += (_, _) => ApplySettings();
        _minutesNum.ValueChanged += (_, _) => ApplySettings();
        UpdateSettingsState();

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
            WrapMode = DataGridViewTriState.False,
        };
        _grid.DefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = cCell,
            ForeColor = cFg,
            SelectionBackColor = cCell,
            SelectionForeColor = cFg,
            Padding = new Padding(S(6), 0, S(6), 0),
            WrapMode = DataGridViewTriState.False,
        };

        AddText(L.T("main.Player"), 70);
        AddText(L.T("main.Status"), 235);
        AddText(L.T("main.Name"), 130);
        AddText(L.T("main.ControllerType"), 150);
        AddText(L.T("main.Device"), 170);
        AddText("IP", 150);

        for (int i = 0; i < 9; i++)
        {
            int idx = _grid.Rows.Add();
            _grid.Rows[idx].Cells[ColPlayer].Value = L.F("main.Player2", (i + 1));
        }

        _grid.CellPainting += (_, e) =>
        {
            if (e.RowIndex < 0 || e.RowIndex >= 9 || e.ColumnIndex != ColStatus || !_removable[e.RowIndex]) return;

            e.Paint(e.CellBounds, e.PaintParts);
            var r = ButtonRect(e.CellBounds);
            using (var fill = new SolidBrush(cDanger)) e.Graphics!.FillRectangle(fill, r);
            using (var pen = new Pen(cDangerBorder)) e.Graphics!.DrawRectangle(pen, r.X, r.Y, r.Width - 1, r.Height - 1);
            TextRenderer.DrawText(e.Graphics, _btnText[e.RowIndex] ?? L.T("main.Remove"), _btnFont, r, cDangerFg,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis);
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

        Controls.Add(_grid);
        Controls.Add(bottomBar);
        Controls.Add(top);

        var menu = new ContextMenuStrip { RightToLeft = RightToLeft.Yes };
        var openItem = new ToolStripMenuItem(L.T("main.Open")) { Font = new Font(Font, FontStyle.Bold) };
        var exitItem = new ToolStripMenuItem(L.T("main.Exit"));
        _openItem = openItem; _exitItem = exitItem;
        menu.ImageScalingSize = new Size(S(16), S(16));
        var exitImg = LoadResourceIcon("exit-dark.ico", new Size(S(16), S(16)));
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
            _hub.Tick();
            RefreshList();

            if (_fwPending && Visible && WindowState != FormWindowState.Minimized && --_fwDelay <= 0)
            {
                _fwPending = false;
                _ipTick = 0;
                UpdateAddress();
                CheckFirewall();
            }

            if (Visible && WindowState != FormWindowState.Minimized && ++_ipTick >= IpRefreshSeconds)
            {
                _ipTick = 0;
                UpdateAddress();
            }
        };
        _timer.Start();

        _onAddrChanged = (_, _) => NetworkChangedSoon();
        _onAvailChanged = (_, _) => NetworkChangedSoon();
        NetworkChange.NetworkAddressChanged += _onAddrChanged;
        NetworkChange.NetworkAvailabilityChanged += _onAvailChanged;

        FormClosed += (_, _) =>
        {
            if (_onAddrChanged != null) NetworkChange.NetworkAddressChanged -= _onAddrChanged;
            if (_onAvailChanged != null) NetworkChange.NetworkAvailabilityChanged -= _onAvailChanged;
            _timer.Stop();
            _timer.Dispose();
            _hub.Changed -= _onChanged;
            L.Changed -= _onLangChanged;
            _tray.Visible = false;
            _tray.Dispose();
        };

        _onLangChanged = () =>
        {
            try { if (IsHandleCreated && !IsDisposed) BeginInvoke(new Action(ApplyLanguage)); }
            catch { }
        };
        L.Changed += _onLangChanged;
        ApplyLanguage();

        RefreshList();
        LayoutTop();
        _grid.ClearSelection();
    }

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
            try { Clipboard.SetText(url); _tip.Show(L.T("main.LinkCopied"), c, 0, -c.Height, 1500); } catch { }
        }
    }

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

    public void ShowFromTray()
    {
        if (!_allowShow)
        {
            _allowShow = true;
            if (!Visible) CenterToScreen();
        }
        if (!Visible) Show();
        if (WindowState == FormWindowState.Minimized) WindowState = FormWindowState.Normal;
        Activate();
    }

    private bool _allowShow = true;

    public void StartHidden() { _allowShow = false; }

    protected override void SetVisibleCore(bool value)
    {
        if (!_allowShow)
        {
            if (!IsHandleCreated) CreateHandle();
            value = false;
        }
        base.SetVisibleCore(value);
    }

    public void SetPort(int port)
    {
        _port = port;
        _addrInit = false;
        UpdateAddress();
    }

    public void ExitApp()
    {
        _exiting = true;
        Close();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        base.OnFormClosing(e);
        if (e.Cancel || _exiting || e.CloseReason != CloseReason.UserClosing) return;

        e.Cancel = true;
        Hide();
        if (!_tipShown)
        {
            _tipShown = true;
            try
            {
                _tray.ShowBalloonTip(3000, "HSH Gamepad", L.T("main.TheAppIsStillRunning"), ToolTipIcon.Info);
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
            UpdateAddress();
        }
    }

    private void NetworkChangedSoon()
    {
        try
        {
            if (!IsHandleCreated || IsDisposed) return;
            BeginInvoke(new Action(() =>
            {
                _ipTick = 0;
                UpdateAddress();
                _fwPending = true;
                _fwDelay = 4;
            }));
        }
        catch { }
    }

    private void CheckFirewall()
    {
        if (Interlocked.CompareExchange(ref _fwBusy, 1, 0) != 0) return;
        int port = _port;
        ThreadPool.QueueUserWorkItem(_ =>
        {
            string key = "";
            bool need = false;
            try { need = FirewallGuard.NeedsRule(port, out key); } catch { }
            try
            {
                if (!need || key == _fwAsked) { Volatile.Write(ref _fwBusy, 0); return; }
                BeginInvoke(new Action(() => AskFirewall(key)));
            }
            catch { Volatile.Write(ref _fwBusy, 0); }
        });
    }

    private void AskFirewall(string key)
    {
        _fwAsked = key;
        MessageBoxOptions opt = L.Rtl ? (MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign) : default(MessageBoxOptions);
        bool yes = false;
        try
        {
            yes = MessageBox.Show(this, L.T("main.FirewallAsk"), "HSH Gamepad", MessageBoxButtons.YesNo, MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button1, opt) == DialogResult.Yes;
        }
        catch { }
        if (!yes) { Volatile.Write(ref _fwBusy, 0); return; }

        ThreadPool.QueueUserWorkItem(_ =>
        {
            int r = FirewallGuard.AddRule();
            try
            {
                BeginInvoke(new Action(() =>
                {
                    Volatile.Write(ref _fwBusy, 0);
                    if (r < 0)
                        MessageBox.Show(this, L.T("main.FirewallFail"), "HSH Gamepad", MessageBoxButtons.OK, MessageBoxIcon.Error,
                            MessageBoxDefaultButton.Button1, opt);
                }));
            }
            catch { Volatile.Write(ref _fwBusy, 0); }
        });
    }

    private void UpdateAddress()
    {
        string? ip = NetInfo.LanIp();
        if (_addrInit && ip == _ip) return;
        _addrInit = true;
        _ip = ip;
        _url = ip == null ? null : NetInfo.Url(ip, _port) + AccessGuard.PagePath;

        if (_url == null)
        {
            _box.ForeColor = cMuted;
            _box.Text = L.T("main.WaitingForNetworkConnection");
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
        _copy.Text = L.T("main.Copy");
        if (_qrForm != null && !_qrForm.IsDisposed) _qrForm.SetUrl(_url);
        LayoutTop();
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
        int pad = S(8);
        int w = Math.Max(S(40), Math.Min(Math.Max(S(74), _removeW), cell.Width / 2));
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

    private FlowLayoutPanel SettingsRow(int height)
    {
        return new FlowLayoutPanel
        {
            Width = S(470), Height = height, BackColor = cPanel, Margin = Padding.Empty,
            RightToLeft = L.Rtl ? RightToLeft.Yes : RightToLeft.No, FlowDirection = FlowDirection.LeftToRight, WrapContents = false,
            Padding = new Padding(0, S(6), 0, 0),
        };
    }

    private Label SettingsLabel(string text, int indent)
    {
        return new Label { Text = text, AutoSize = true, ForeColor = cFg, Margin = new Padding(indent, S(4), S(6), 0) };
    }

    private NumericUpDown SettingsNumber(int min, int max, int value)
    {
        return new NumericUpDown
        {
            Minimum = min,
            Maximum = max,
            Value = Math.Max(min, Math.Min(max, value)),
            Width = S(70),
            BackColor = cCell,
            ForeColor = cFg,
            BorderStyle = BorderStyle.FixedSingle,
            RightToLeft = RightToLeft.No,
            Margin = new Padding(S(6), S(2), S(6), 0),
        };
    }

    private void UpdateSettingsState()
    {
        bool keyOn = _keyChk.Checked;
        bool banOn = keyOn && _banChk.Checked;
        _keyBox.SetActive(keyOn);
        _banChk.Enabled = keyOn;
        _triesNum.Enabled = banOn;
        _minutesNum.Enabled = banOn;
        _triesLbl.ForeColor = banOn ? cFg : cMuted;
        _minutesLbl.ForeColor = banOn ? cFg : cMuted;
    }

    private void LayoutTop()
    {
        if (_lblAddr == null || _summary == null || _summary.Parent == null || _grid == null) return;
        int need = Math.Max(Fit.TextWidth(_qrBtn.Text, Font),
                   Math.Max(Fit.TextWidth(L.T("main.Copy"), Font), Fit.TextWidth(L.T("main.Copied"), Font))) + S(22);
        int bw = Math.Min(S(140), Math.Max(S(84), need));
        _qrBtn.Width = bw;
        _copy.Width = bw;

        int boxW = Fit.TextWidth(_box.Text, _box.Font) + S(14);
        _box.Width = boxW;

        int gap = S(12);
        int summaryW = Fit.TextWidth(L.F("main.ConnectedOf9", 9), _summary.Font);
        int maxRight = Math.Max(S(402) + bw, ((Control)_summary.Parent!).ClientSize.Width - S(14) - summaryW - S(16));
        int labelW = Fit.TextWidth(_lblAddr.Text, Font) + S(6);
        int leftW = Math.Max(boxW, Math.Min(labelW, maxRight - bw - gap - _lblAddr.Left));
        int btnLeft = _lblAddr.Left + leftW + gap;
        _qrBtn.Left = btnLeft;
        _copy.Left = btnLeft;
        _box.Width = Math.Max(boxW, btnLeft - gap - _box.Left);
        _summary.Left = _copy.Right + S(16);
        _lblAddr.Width = Math.Max(S(100), _qrBtn.Left - S(10) - _lblAddr.Left);
        var oldFont = _lblAddr.Font;
        var fitted = Fit.Shrink(_lblAddr.Text, Font, _lblAddr.Width, _lblAddr.Height, false, 8f);
        _lblAddr.Font = fitted;
        if (!ReferenceEquals(oldFont, Font) && !ReferenceEquals(oldFont, fitted)) oldFont.Dispose();

        var oldBtnFont = _topBtnFont;
        _topBtnFont = new Font(fitted.FontFamily, fitted.Size, FontStyle.Regular, fitted.Unit);
        _qrBtn.Font = _topBtnFont;
        _copy.Font = _topBtnFont;
        oldBtnFont?.Dispose();
        _tip.SetToolTip(_lblAddr, _lblAddr.Text);
        _removeW = Math.Max(Fit.TextWidth(L.T("main.Remove"), _btnFont), Fit.TextWidth(L.F("main.Remove2", 3600), _btnFont)) + S(14);
        FitColumns();
    }

    private void FitColumns()
    {
        if (_grid == null || _grid.Columns.Count == 0) return;
        var headerFont = _grid.ColumnHeadersDefaultCellStyle.Font ?? _grid.Font;
        var cellFont = _grid.DefaultCellStyle.Font ?? _grid.Font;
        int pad = S(6) * 2 + S(6);
        for (int c = 0; c < _grid.Columns.Count; c++)
        {
            int need = Fit.TextWidth(_grid.Columns[c].HeaderText, headerFont);
            foreach (DataGridViewRow r in _grid.Rows)
            {
                string? t = r.Cells[c].Value as string;
                if (string.IsNullOrEmpty(t)) continue;
                int w = Fit.TextWidth(t, cellFont);
                if (c == ColStatus && _removable[r.Index]) w += _removeW + S(16);
                if (w > need) need = w;
            }
            int min = need + pad;
            if (_grid.Columns[c].MinimumWidth != min) _grid.Columns[c].MinimumWidth = Math.Max(S(20), min);
        }
    }

    private void ApplySettings()
    {
        UpdateSettingsState();
        string key = AccessGuard.Clean(_keyBox.Key);
        if (key.Length > 0) _settings.Key = key;
        _settings.KeyEnabled = _keyChk.Checked;
        _settings.BanEnabled = _banChk.Checked;
        _settings.BanTries = (int)_triesNum.Value;
        _settings.BanMinutes = (int)_minutesNum.Value;
        AccessGuard.Apply(_settings);
        _addrInit = false;
        UpdateAddress();
        _autoNum.Enabled = _autoChk.Checked;
        _settings.AutoKick = _autoChk.Checked;
        _settings.Seconds = (int)_autoNum.Value;
        _hub.AutoKickSeconds = _settings.AutoKick ? _settings.Seconds : 0;
        _settings.AllowInput = _inputChk.Checked;
        _hub.AllowInput = _settings.AllowInput;
        if (!_settings.AllowInput) InputInjector.ReleaseAll();
        _settings.Save();
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

    private void ShowLanguage()
    {
        if (_langForm != null && !_langForm.IsDisposed) { _langForm.Activate(); return; }
        var f = new LanguageForm(_k, Icon, _settings);
        f.FormClosed += (_, _) => { if (ReferenceEquals(_langForm, f)) _langForm = null; };
        _langForm = f;
        f.Show(_settingsForm);
    }

    private void ApplyLanguage()
    {
        var dir = L.Rtl ? RightToLeft.Yes : RightToLeft.No;
        _lblAddr.Text = L.T("main.EnterTheAddressBelowIn");
        _qrBtn.Text = L.T("ui.qr");
        _autoChk.Text = L.T("main.AutoRemoveAnAbandonedController");
        _secLbl.Text = L.T("main.Seconds");
        _inputChk.Text = L.T("main.MouseAndKeyboardAccessFrom");
        _tip.SetToolTip(_inputChk, L.T("main.OnlyWorksWhilePlayer1s"));
        _keyChk.Text = L.T("main.AccessKey");
        _tip.SetToolTip(_keyChk, L.T("main.AccessKeyTip"));
        _tip.SetToolTip(_keyBox.Input, L.T("main.AccessKeyTip"));
        _tip.SetToolTip(_keyBox.Dice, L.T("main.RandomKey"));
        _banChk.Text = L.T("main.BlockWrongTries");
        _tip.SetToolTip(_banChk, L.T("main.BlockWrongTriesTip"));
        _triesLbl.Text = L.T("main.MaxTries");
        _minutesLbl.Text = L.T("main.BlockMinutes");
        _grid.Columns[ColPlayer].HeaderText = L.T("main.Player");
        _grid.Columns[ColStatus].HeaderText = L.T("main.Status");
        _grid.Columns[ColName].HeaderText = L.T("main.Name");
        _grid.Columns[ColType].HeaderText = L.T("main.ControllerType");
        _grid.Columns[ColDevice].HeaderText = L.T("main.Device");
        for (int i = 0; i < 9; i++) _grid.Rows[i].Cells[ColPlayer].Value = L.F("main.Player2", i + 1);
        _openItem.Text = L.T("main.Open");
        _exitItem.Text = L.T("main.Exit");
        _settingsForm.Relabel(dir);
        LayoutTop();
        _addrInit = false;
        UpdateAddress();
        RefreshList();
        _grid.Invalidate();
    }

    private void ShowSettings()
    {
        if (_settingsForm.Visible) { _settingsForm.Activate(); return; }
        _settingsForm.Show(this);
    }

    private void ShowQr()
    {
        if (_url == null) return;
        if (_qrForm != null && !_qrForm.IsDisposed) { _qrForm.SetUrl(_url); _qrForm.Activate(); return; }
        var f = new QrForm(_url, _k) { Icon = Icon };
        f.FormClosed += (_, _) => { if (ReferenceEquals(_qrForm, f)) _qrForm = null; };
        _qrForm = f;
        f.Show(this);
    }

    private sealed class SettingsForm : Form
    {
        private readonly float _k;
        private readonly Button _language;
        private readonly FlowLayoutPanel _body;
        private readonly Control[] _rows;

        private static readonly Size Unbounded = new Size(10000, 10000);

        private int S(int px) => (int)Math.Round(px * _k);

        public SettingsForm(float k, Icon? icon, Control[] rows, Action onLanguage)
        {
            _k = k;
            _rows = rows;
            Text = "Settings";
            if (icon != null) Icon = icon;
            BackColor = cPanel;
            ForeColor = cFg;
            Font = new Font("Segoe UI", 10f);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            AutoScaleMode = AutoScaleMode.None;

            _language = new Button
            {
                Text = "Language",
                Width = S(160),
                Height = S(34),
                RightToLeft = RightToLeft.No,
            };
            StyleButton(_language, cCell, cAccent, cFg);
            _language.Click += (_, _) => onLanguage();

            _body = new FlowLayoutPanel
            {
                Left = S(16),
                Top = S(16),
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                BackColor = cPanel,
                RightToLeft = RightToLeft.No,
            };
            _body.Controls.Add(_language);
            _body.Controls.AddRange(rows);
            Controls.Add(_body);
            Relayout();
        }

        public void Relabel(RightToLeft dir)
        {
            foreach (var r in _rows) r.RightToLeft = dir;
            Relayout();
        }

        private void Relayout()
        {
            int w = S(470);
            foreach (var r in _rows)
                if (r is FlowLayoutPanel row) w = Math.Max(w, row.GetPreferredSize(Unbounded).Width + S(4));
            var screen = Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 1920, 1080);
            w = Math.Min(w, screen.Width - S(80));
            foreach (var r in _rows) r.Width = w;
            _language.Margin = new Padding(Math.Max(0, w - _language.Width), 0, 0, S(6));
            _body.PerformLayout();
            var size = _body.GetPreferredSize(Unbounded);
            ClientSize = new Size(w + 2 * S(16), size.Height + 2 * S(16));
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            if (e.CloseReason == CloseReason.UserClosing) { e.Cancel = true; Hide(); }
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
            Text = L.T("ui.qr");
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
            _copyBtn = new Button { Text = L.T("main.Copy"), Left = pad + side - btnW, Top = pad + side + pad, Width = btnW, Height = btnH };
            StyleButton(_copyBtn, cCell, cAccent, cFg);
            Fit.Auto(_copyBtn);
            _copyBtn.Click += (_, _) =>
            {
                if (_u == null) return;
                try { Clipboard.SetText(_u); _copyBtn.Text = L.T("main.Copied"); } catch { }
            };
            var hint = new Label
            {
                Text = L.T("main.ScanTheQRCodeWith"),
                Left = pad,
                Top = hintTop,
                Width = side,
                Height = hintH,
                ForeColor = cMuted,
                RightToLeft = RightToLeft.Yes,
                TextAlign = ContentAlignment.TopCenter,
            };
            Controls.AddRange(new Control[] { _qr, _addr, _copyBtn, hint });
            Fit.Auto(hint, true);
            SetUrl(url);
        }

        public void SetUrl(string? url)
        {
            _u = url;
            _addr.ForeColor = url == null ? cMuted : cFg;
            _addr.Text = url ?? L.T("main.WaitingForNetworkConnection");
            _copyBtn.Enabled = url != null;
            _copyBtn.Text = L.T("main.Copy");
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
            "screen" => "Touch Screen",
            "ps4" => s.FellBack ? L.T("main.PS4XboxFallback") : "PS4 (DualShock 4)",
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
                status = L.T("main.Connected");
                back = cOwned;
                fore = cFg;
            }
            else if (s.HasPad)
            {
                status = L.T("main.Abandoned");
                back = cCell;
                fore = cMuted;
                _btnText[i] = (auto > 0 && s.IdleSeconds is double idle)
                    ? L.F("main.Remove2", Math.Max(0, auto - (int)idle))
                    : L.T("main.Remove");
            }
            else
            {
                status = L.T("main.Free");
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

        _summary.Text = L.F("main.ConnectedOf9", connected);
        FitColumns();
        _grid.InvalidateColumn(ColStatus);
    }
}


internal sealed class QrCode
{
    private static readonly int[] EccPerBlock = { -1, 10, 16, 26, 18, 24, 16, 18, 22, 22, 26 };
    private static readonly int[] NumBlocks = { -1, 1, 1, 1, 2, 2, 4, 4, 4, 5, 5 };

    public int Size { get; }
    private readonly bool[,] _mod;
    private readonly bool[,] _fn;

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
            ApplyMask(m);
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
        int data = mask;
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
        int n = _qr.Size, q = 2, total = n + 2 * q;
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
