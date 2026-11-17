using System;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Windows.Forms;

namespace GamepadServer;

internal static class Fit
{
    private sealed class State
    {
        public Font Base = null!;
        public Font? Made;
        public bool Wrap;
        public float Min;
        public bool Busy;
    }

    private const TextFormatFlags OneLine = TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding;
    private const TextFormatFlags Wrapped = TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix | TextFormatFlags.TextBoxControl;
    private static readonly Size Unbounded = new Size(int.MaxValue, int.MaxValue);
    private static readonly ConditionalWeakTable<Control, State> States = new ConditionalWeakTable<Control, State>();

    public static int TextWidth(string text, Font font)
    {
        return TextRenderer.MeasureText(text, font, Unbounded, OneLine).Width;
    }

    public static int TextHeight(string text, Font font, int width)
    {
        return TextRenderer.MeasureText(text, font, new Size(Math.Max(1, width), int.MaxValue), Wrapped).Height;
    }

    public static Font Shrink(string text, Font baseFont, int width, int height, bool wrap, float minSize)
    {
        float size = baseFont.Size;
        Font f = baseFont;
        while (!Fits(text, f, width, height, wrap) && size - 0.5f >= minSize)
        {
            if (!ReferenceEquals(f, baseFont)) f.Dispose();
            size -= 0.5f;
            f = new Font(baseFont.FontFamily, size, baseFont.Style, baseFont.Unit);
        }
        return f;
    }

    public static T Auto<T>(T c, bool wrap = false, float minSize = 7f) where T : Control
    {
        if (!States.TryGetValue(c, out var st))
        {
            st = new State { Base = c.Font, Wrap = wrap, Min = minSize };
            States.Add(c, st);
            c.TextChanged += (_, _) => Apply(c);
            c.SizeChanged += (_, _) => Apply(c);
            c.Disposed += (_, _) => { if (States.TryGetValue(c, out var s) && s.Made != null) s.Made.Dispose(); };
        }
        Apply(c);
        return c;
    }

    public static Font ForGraphics(Graphics g, string text, string family, float size, FontStyle style, float width, float minSize = 6.5f)
    {
        while (true)
        {
            var f = new Font(family, size, style);
            if (size - 0.5f < minSize || g.MeasureString(text, f).Width <= width) return f;
            f.Dispose();
            size -= 0.5f;
        }
    }

    private static bool Fits(string text, Font f, int width, int height, bool wrap)
    {
        if (string.IsNullOrEmpty(text)) return true;
        if (!wrap)
        {
            var one = TextRenderer.MeasureText(text, f, Unbounded, OneLine);
            return one.Width <= width && one.Height <= height;
        }
        var many = TextRenderer.MeasureText(text, f, new Size(Math.Max(1, width), int.MaxValue), Wrapped);
        return many.Width <= width && many.Height <= height;
    }

    private static void Apply(Control c)
    {
        if (!States.TryGetValue(c, out var st) || st.Busy || c.IsDisposed) return;
        float k = c.DeviceDpi / 96f;
        bool button = c is ButtonBase;
        int w = c.ClientSize.Width - (button ? (int)Math.Round(12 * k) : 0);
        int h = c.ClientSize.Height - (button ? (int)Math.Round(4 * k) : 0);
        if (w <= 0 || h <= 0) return;

        var f = Shrink(c.Text, st.Base, w, h, st.Wrap, st.Min);
        var cur = c.Font;
        if (Math.Abs(cur.Size - f.Size) < 0.01f && cur.FontFamily.Equals(f.FontFamily) && cur.Style == f.Style)
        {
            if (!ReferenceEquals(f, st.Base) && !ReferenceEquals(f, cur)) f.Dispose();
            return;
        }

        st.Busy = true;
        try
        {
            c.Font = f;
            if (st.Made != null && !ReferenceEquals(st.Made, f)) st.Made.Dispose();
            st.Made = ReferenceEquals(f, st.Base) ? null : f;
        }
        finally { st.Busy = false; }
    }
}
