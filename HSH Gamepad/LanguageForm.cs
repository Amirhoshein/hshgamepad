// HSH Gamepad | Telegram: @HSHGamepad
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace GamepadServer;

/// <summary>پنجره‌ی انتخاب زبان: لیست زبان‌های پوشه‌ی lang (هر کدوم یه کارت) + Import / Export / باز کردن پوشه. همه‌ی متن‌هاش از json زبان میاد.</summary>
internal sealed class LanguageForm : Form
{
    private static readonly Color cBg = Color.FromArgb(18, 21, 29);
    private static readonly Color cPanel = Color.FromArgb(27, 32, 48);
    private static readonly Color cCell = Color.FromArgb(24, 28, 41);
    private static readonly Color cFg = Color.FromArgb(230, 233, 242);
    private static readonly Color cMuted = Color.FromArgb(141, 151, 179);
    private static readonly Color cAccent = Color.FromArgb(91, 146, 255);
    private static readonly Color cGrid = Color.FromArgb(43, 50, 71);

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

    private readonly float _k;
    private readonly AppSettings _settings;
    private readonly Label _title;
    private readonly Label _hint;
    private readonly Label _path;
    private readonly LangList _list;
    private readonly Button _import;
    private readonly Button _export;
    private readonly Button _folder;
    private readonly Action _onLangChanged;
    private readonly Panel _head;
    private readonly Font _hintFont = new Font("Segoe UI", 9f);
    private readonly int _baseHeight;

    private int S(int px) { return (int)Math.Round(px * _k); }

    public LanguageForm(float k, Icon? icon, AppSettings settings)
    {
        _k = k;
        _settings = settings;

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
        ClientSize = new Size(S(460), S(480));
        _baseHeight = ClientSize.Height;
        AllowDrop = true;       // فایل json زبان رو بندازی روی پنجره، اضافه میشه
        DragEnter += (_, e) => { e.Effect = DroppedFiles(e).Length > 0 ? DragDropEffects.Copy : DragDropEffects.None; };
        DragDrop += (_, e) => { var f = DroppedFiles(e); if (f.Length > 0) ImportFiles(f); };

        // ---------- بالا ----------
        var head = new Panel { Dock = DockStyle.Top, Height = S(104), BackColor = cPanel };
        _head = head;
        _title = new Label
        {
            AutoSize = false,
            Left = S(20),
            Top = S(14),
            Width = S(420),
            Height = S(30),
            ForeColor = cFg,
            Font = new Font("Segoe UI Semibold", 15f),
        };
        _hint = new Label
        {
            AutoSize = false,
            Left = S(20),
            Top = S(48),
            Width = S(420),
            Height = S(36),
            ForeColor = cMuted,
            Font = _hintFont,
        };
        _path = new Label
        {
            AutoSize = false,
            Left = S(20),
            Top = S(84),
            Width = S(420),
            Height = S(16),
            ForeColor = Color.FromArgb(94, 104, 135),
            Font = new Font("Consolas", 8f),
            RightToLeft = RightToLeft.No,
            AutoEllipsis = true,
            Text = L.Dir,
        };
        head.Controls.AddRange(new Control[] { _title, _hint, _path });
        Fit.Auto(_title, false, 10f);

        // ---------- پایین: دکمه‌ها ----------
        var foot = new Panel { Dock = DockStyle.Bottom, Height = S(68), BackColor = cPanel };
        int bw = S(120), bh = S(36), gap = S(10), by = S(16);
        _import = MakeButton(S(20), by, bw, bh);
        _export = MakeButton(S(20) + bw + gap, by, bw, bh);
        _folder = MakeButton(S(20) + 2 * (bw + gap), by, bw, bh);
        _import.Click += (_, _) => DoImport();
        _export.Click += (_, _) => DoExport();
        _folder.Click += (_, _) => L.OpenFolder();
        foot.Controls.AddRange(new Control[] { _import, _export, _folder });

        // ---------- وسط: لیست زبان‌ها ----------
        var mid = new Panel { Dock = DockStyle.Fill, BackColor = cPanel, Padding = new Padding(S(20), S(4), S(20), S(8)) };
        _list = new LangList(_k) { Dock = DockStyle.Fill, AllowDrop = true };
        _list.DragEnter += (_, e) => { e.Effect = DroppedFiles(e).Length > 0 ? DragDropEffects.Copy : DragDropEffects.None; };
        _list.DragDrop += (_, e) => { var f = DroppedFiles(e); if (f.Length > 0) ImportFiles(f); };
        _list.Picked += info => Pick(info);
        mid.Controls.Add(_list);

        Controls.Add(mid);      // اول Fill بعد لبه‌ها
        Controls.Add(foot);
        Controls.Add(head);

        Relabel();

        _onLangChanged = () =>
        {
            try { if (IsHandleCreated && !IsDisposed) BeginInvoke(new Action(Relabel)); }
            catch { }
        };
        L.Changed += _onLangChanged;
        FormClosed += (_, _) => L.Changed -= _onLangChanged;
    }

    private Button MakeButton(int left, int top, int w, int h)
    {
        var b = new Button { Left = left, Top = top, Width = w, Height = h, TabStop = false, Font = new Font("Segoe UI Semibold", 9.5f), RightToLeft = RightToLeft.No };
        b.FlatStyle = FlatStyle.Flat;
        b.FlatAppearance.BorderColor = cAccent;
        b.FlatAppearance.BorderSize = 1;
        b.UseVisualStyleBackColor = false;
        b.BackColor = cCell;
        b.ForeColor = cFg;
        b.Cursor = Cursors.Hand;
        return Fit.Auto(b);
    }

    /// <summary>همه‌ی متن‌ها رو از زبان فعلی دوباره میذاره</summary>
    private void Relabel()
    {
        Text = L.T("lang.title");
        _title.Text = L.T("lang.title");
        _hint.Text = L.T("lang.hint");
        var rtl = L.Rtl ? RightToLeft.Yes : RightToLeft.No;
        _hint.RightToLeft = rtl;
        _title.RightToLeft = rtl;
        _import.Text = L.T("lang.import");
        _export.Text = L.T("lang.export");
        _folder.Text = L.T("lang.folder");
        LayoutHint();
        _list.SetItems(L.Languages(), L.Code);
    }

    private void LayoutHint()
    {
        int maxH = S(86);
        var oldFont = _hint.Font;
        var font = Fit.Shrink(_hint.Text, _hintFont, _hint.Width, maxH, true, 7.5f);
        _hint.Font = font;
        if (!ReferenceEquals(oldFont, _hintFont) && !ReferenceEquals(oldFont, font)) oldFont.Dispose();
        _hint.Height = Math.Min(maxH, Fit.TextHeight(_hint.Text, font, _hint.Width) + S(2));
        _path.Top = _hint.Bottom + S(2);
        int headH = _path.Bottom + S(6);
        int grow = Math.Max(0, headH - S(104));
        _head.Height = Math.Max(S(104), headH);
        ClientSize = new Size(ClientSize.Width, _baseHeight + grow);
    }

    private void Pick(LangInfo info)
    {
        if (info.Code.Equals(L.Code, StringComparison.OrdinalIgnoreCase)) return;
        _settings.Language = info.Code;
        _settings.Save();
        L.Select(info.Code);        // L.Changed -> همه‌ی پنجره‌ها متن‌هاشون رو عوض میکنن
    }

    private DialogResult Msg(string text, MessageBoxButtons buttons, MessageBoxIcon icon)
    {
        MessageBoxOptions opt = L.Rtl ? (MessageBoxOptions.RtlReading | MessageBoxOptions.RightAlign) : default(MessageBoxOptions);
        return MessageBox.Show(this, text, L.T("lang.title"), buttons, icon, MessageBoxDefaultButton.Button1, opt);
    }

    private void DoImport()
    {
        using (var dlg = new OpenFileDialog())
        {
            dlg.Filter = "JSON (*.json)|*.json|*.*|*.*";
            dlg.Title = L.T("lang.import");
            dlg.Multiselect = true;
            if (dlg.ShowDialog(this) != DialogResult.OK) return;
            ImportFiles(dlg.FileNames);
        }
    }

    /// <summary>هر فایل json با هر اسمی (anything.json) قبول میشه، به شرطی که ساختارش (meta + strings) درست باشه</summary>
    private void ImportFiles(string[] files)
    {
        var added = new List<string>();
        foreach (var file in files)
        {
            LangInfo info;
            ImportResult r = L.Import(file, false, out info);
            if (r == ImportResult.Exists)
            {
                if (Msg(L.F("lang.importExists", info.Name), MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) continue;
                r = L.Import(file, true, out info);
            }
            if (r == ImportResult.Ok) added.Add(info.Name);
            else
            {
                string name = System.IO.Path.GetFileName(file);
                Msg(name + "\n\n" + L.T(r == ImportResult.Invalid ? "lang.importBad" : "lang.importFail"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        if (added.Count > 0)
        {
            _list.SetItems(L.Languages(), L.Code);
            Msg(L.F("lang.importOk", string.Join(", ", added.ToArray())), MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    private static string[] DroppedFiles(DragEventArgs e)
    {
        try
        {
            var data = e.Data;
            if (data != null && data.GetDataPresent(DataFormats.FileDrop))
            {
                var arr = data.GetData(DataFormats.FileDrop) as string[];
                if (arr != null) return arr;
            }
        }
        catch { }
        return new string[0];
    }

    private void DoExport()
    {
        LangInfo? sel = _list.Selected;
        string code = sel != null ? sel.Code : L.Code;
        using (var dlg = new SaveFileDialog())
        {
            dlg.Filter = "JSON (*.json)|*.json";
            dlg.Title = L.T("lang.export");
            dlg.FileName = code + ".json";
            dlg.DefaultExt = "json";
            dlg.OverwritePrompt = true;
            if (dlg.ShowDialog(this) != DialogResult.OK) return;
            if (L.Export(code, dlg.FileName)) Msg(L.T("lang.exportOk"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            else Msg(L.T("lang.exportFail"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _hintFont.Dispose();
        base.Dispose(disposing);
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

    /// <summary>لیست کارتی زبان‌ها (خودِ ویندوز لیستی با این ظاهر نداره)</summary>
    private sealed class LangList : Control
    {
        private readonly float _k;
        private readonly Font _nameFont = new Font("Segoe UI Semibold", 12f);
        private readonly Font _subFont = new Font("Segoe UI", 8.5f);
        private readonly Font _badgeFont = new Font("Segoe UI Semibold", 8f);
        private readonly ToolTip _tip = new ToolTip();
        private static readonly Color cWarn = Color.FromArgb(240, 180, 70);
        private List<LangInfo> _items = new List<LangInfo>();
        private string _active = "";
        private int _hover = -1;
        private int _sel = -1;
        private int _scroll;

        public event Action<LangInfo>? Picked;

        public LangList(float k)
        {
            _k = k;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = cPanel;
            TabStop = false;
        }

        private int S(int px) { return (int)Math.Round(px * _k); }
        private int RowH { get { return S(58); } }

        public LangInfo? Selected
        {
            get { return (_sel >= 0 && _sel < _items.Count) ? _items[_sel] : null; }
        }

        public void SetItems(List<LangInfo> items, string active)
        {
            string? keep = Selected != null ? Selected.Code : null;
            _items = items;
            _active = active;
            _sel = -1;
            for (int i = 0; i < _items.Count; i++)
            {
                if (keep != null && _items[i].Code.Equals(keep, StringComparison.OrdinalIgnoreCase)) _sel = i;
            }
            if (_sel < 0)
            {
                for (int i = 0; i < _items.Count; i++)
                    if (_items[i].Code.Equals(active, StringComparison.OrdinalIgnoreCase)) _sel = i;
            }
            ClampScroll();
            Invalidate();
        }

        private void ClampScroll()
        {
            int max = Math.Max(0, _items.Count * RowH - Height);
            if (_scroll > max) _scroll = max;
            if (_scroll < 0) _scroll = 0;
        }

        private int HitTest(Point p)
        {
            int idx = (p.Y + _scroll) / RowH;
            if (p.Y < 0 || idx < 0 || idx >= _items.Count) return -1;
            return idx;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(cPanel);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            int rh = RowH, gapY = S(8), rad = S(10), pad = S(14), dot = S(20);
            var flags = TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix;

            for (int i = 0; i < _items.Count; i++)
            {
                var info = _items[i];
                var r = new Rectangle(0, i * rh - _scroll, Width - 1, rh - gapY);
                if (r.Bottom < 0 || r.Top > Height) continue;
                bool active = info.Code.Equals(_active, StringComparison.OrdinalIgnoreCase);

                Color fill = i == _hover ? Color.FromArgb(33, 40, 60) : cCell;
                Color border = active ? cAccent : cGrid;
                using (var path = RoundRect(r, rad))
                {
                    using (var br = new SolidBrush(fill)) g.FillPath(br, path);
                    using (var pen = new Pen(border, active ? 1.8f : 1f)) g.DrawPath(pen, path);
                }

                // دایره‌ی انتخاب (پر = زبان فعلی)
                int cy = r.Top + r.Height / 2;
                var circle = new Rectangle(r.Left + pad, cy - dot / 2, dot, dot);
                using (var pen = new Pen(active ? cAccent : cMuted, 2f)) g.DrawEllipse(pen, circle);
                if (active)
                {
                    int inner = dot - S(10);
                    using (var br = new SolidBrush(cAccent))
                        g.FillEllipse(br, circle.Left + (dot - inner) / 2, circle.Top + (dot - inner) / 2, inner, inner);
                }

                // برچسب RTL / LTR سمت راست
                string badge = info.Rtl ? "RTL" : "LTR";
                var bsz = TextRenderer.MeasureText(g, badge, _badgeFont, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding);
                var brect = new Rectangle(r.Right - pad - bsz.Width - S(14), cy - (bsz.Height + S(6)) / 2, bsz.Width + S(14), bsz.Height + S(6));
                using (var bp = RoundRect(brect, S(6)))
                using (var pen = new Pen(cGrid, 1f))
                    g.DrawPath(pen, bp);
                TextRenderer.DrawText(g, badge, _badgeFont, brect, cMuted, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);

                int textEnd = brect.Left;
                if (!info.Complete)
                {
                    string warn = "\u26A0 " + L.F("lang.incomplete", info.Missing);
                    var wsz = TextRenderer.MeasureText(g, warn, _badgeFont, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
                    int ww = Math.Min(wsz.Width + S(14), Math.Max(S(40), (brect.Left - circle.Right) / 2));
                    var wrect = new Rectangle(brect.Left - S(6) - ww, brect.Top, ww, brect.Height);
                    using (var wp = RoundRect(wrect, S(6)))
                    using (var pen = new Pen(cWarn, 1f))
                        g.DrawPath(pen, wp);
                    TextRenderer.DrawText(g, warn, _badgeFont, wrect, cWarn,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis);
                    textEnd = wrect.Left;
                }

                int tx = circle.Right + S(14);
                int tw = textEnd - tx - S(10);
                var nameRect = new Rectangle(tx, r.Top + S(7), Math.Max(10, tw), S(26));
                TextRenderer.DrawText(g, info.Name, _nameFont, nameRect, cFg, flags);

                string sub = (info.English.Length > 0 ? info.English + "  ·  " : "") + info.Code;
                var subRect = new Rectangle(tx, r.Top + S(7) + S(26), Math.Max(10, tw), S(18));
                TextRenderer.DrawText(g, sub, _subFont, subRect, cMuted, flags);
            }
        }

        private static GraphicsPath RoundRect(Rectangle r, int rad)
        {
            int d = rad * 2;
            var p = new GraphicsPath();
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            int h = HitTest(e.Location);
            if (h != _hover)
            {
                _hover = h;
                Cursor = h >= 0 ? Cursors.Hand : Cursors.Default;
                var info = h >= 0 ? _items[h] : null;
                _tip.SetToolTip(this, info != null && !info.Complete ? L.F("lang.incompleteTip", info.Missing) : "");
                Invalidate();
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            if (_hover != -1) { _hover = -1; Invalidate(); }
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            if (e.Button != MouseButtons.Left) return;
            int h = HitTest(e.Location);
            if (h < 0) return;
            _sel = h;
            Invalidate();
            var cb = Picked;
            if (cb != null) cb(_items[h]);
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            _scroll -= e.Delta / 2;
            ClampScroll();
            Invalidate();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            ClampScroll();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _nameFont.Dispose();
                _subFont.Dispose();
                _badgeFont.Dispose();
                _tip.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
