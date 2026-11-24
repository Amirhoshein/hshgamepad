// HSH Gamepad | Telegram: @HSHGamepad
using System;
using System.Drawing;
using System.Windows.Forms;

namespace GamepadServer;

internal sealed class KeyBox : Panel
{
    private readonly TextBox _text;
    private readonly Button _dice;
    private readonly Label _slash;
    private readonly Color _fore;
    private readonly Color _muted;
    private readonly Font _diceFont;

    public event Action? KeyChanged;

    public KeyBox(float k, Color back, Color fore, Color muted, Color hover, Font mono)
    {
        int S(int px) => (int)Math.Round(px * k);
        _fore = fore;
        _muted = muted;

        BorderStyle = BorderStyle.FixedSingle;
        BackColor = back;
        RightToLeft = RightToLeft.No;
        Size = new Size(S(220), S(30));

        _diceFont = new Font("Segoe UI Emoji", 11f);
        _dice = new Button
        {
            Dock = DockStyle.Right,
            Width = S(30),
            Text = "\U0001F3B2",
            Font = _diceFont,
            FlatStyle = FlatStyle.Flat,
            UseVisualStyleBackColor = false,
            BackColor = back,
            ForeColor = fore,
            Cursor = Cursors.Hand,
            TabStop = false,
            Padding = Padding.Empty,
            TextAlign = ContentAlignment.MiddleCenter,
        };
        _dice.FlatAppearance.BorderSize = 0;
        _dice.FlatAppearance.MouseOverBackColor = hover;
        _dice.FlatAppearance.MouseDownBackColor = hover;
        _dice.Click += (_, _) => Key = AccessGuard.RandomKey();

        _slash = new Label
        {
            Dock = DockStyle.Left,
            AutoSize = false,
            Width = S(14),
            Text = "/",
            ForeColor = muted,
            Font = mono,
            TextAlign = ContentAlignment.MiddleRight,
        };

        _text = new TextBox
        {
            BorderStyle = BorderStyle.None,
            BackColor = back,
            ForeColor = fore,
            Font = mono,
            MaxLength = AccessGuard.MaxKeyLength,
            RightToLeft = RightToLeft.No,
        };
        _text.KeyPress += (_, e) =>
        {
            if (!char.IsControl(e.KeyChar) && !AccessGuard.IsKeyChar(e.KeyChar)) e.Handled = true;
        };
        _text.TextChanged += (_, _) => OnTextChanged();
        _text.Leave += (_, _) =>
        {
            if (_text.Text.Length == 0) Key = AccessGuard.RandomKey();
        };

        var mid = new Panel { Dock = DockStyle.Fill, BackColor = back };
        mid.Controls.Add(_text);
        mid.Resize += (_, _) => _text.SetBounds(S(2), Math.Max(0, (mid.Height - _text.Height) / 2), Math.Max(1, mid.Width - S(4)), _text.Height);

        Controls.Add(mid);
        Controls.Add(_slash);
        Controls.Add(_dice);
    }

    public Button Dice => _dice;

    public TextBox Input => _text;

    public string Key
    {
        get => _text.Text;
        set => _text.Text = AccessGuard.Clean(value);
    }

    public void SetActive(bool on)
    {
        _text.ReadOnly = !on;
        _text.TabStop = on;
        _text.ForeColor = on ? _fore : _muted;
        _slash.ForeColor = _muted;
        _dice.Enabled = on;
    }

    private void OnTextChanged()
    {
        string raw = _text.Text;
        string clean = AccessGuard.Clean(raw);
        if (clean != raw)
        {
            int caret = Math.Max(0, Math.Min(clean.Length, _text.SelectionStart - (raw.Length - clean.Length)));
            _text.Text = clean;
            _text.SelectionStart = caret;
            return;
        }
        KeyChanged?.Invoke();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _diceFont.Dispose();
        base.Dispose(disposing);
    }
}
