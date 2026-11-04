// HSH Gamepad | Telegram: @HSHGamepad
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace GamepadServer;

/// <summary>
/// ماوس و کیبورد مجازی برای پلیر ۱ (وقتی گوشی روی «ماوس» یا «کیبورد» باشه). با SendInput ویندوز کار میکنه، درایور اضافه نمیخواد.
/// کلیدها با اسکن‌کد فرستاده میشن (مثل کیبورد واقعی)، پس تو بازی‌ها هم کار میکنن و با زبان کیبورد ویندوز درست ترجمه میشن.
/// نکته: ویندوز اجازه نمیده برنامه‌ی عادی به پنجره‌ی «Run as administrator» ورودی بفرسته؛ برای اون HSHGamepad رو هم Run as administrator کن.
/// پیام‌های گوشی:  mm dx dy | mw d (120 = یک تیک، مثبت = بالا) | mb L/R/M 0/1 | kb KeyboardEvent.code 0/1
/// </summary>
public static class InputInjector
{
    public static bool IsInputMode(string? mode) => mode == "mouse" || mode == "keyboard";

    private const uint INPUT_MOUSE = 0, INPUT_KEYBOARD = 1;
    private const uint MOUSEEVENTF_MOVE = 0x0001, MOUSEEVENTF_LEFTDOWN = 0x0002, MOUSEEVENTF_LEFTUP = 0x0004,
                       MOUSEEVENTF_RIGHTDOWN = 0x0008, MOUSEEVENTF_RIGHTUP = 0x0010,
                       MOUSEEVENTF_MIDDLEDOWN = 0x0020, MOUSEEVENTF_MIDDLEUP = 0x0040, MOUSEEVENTF_WHEEL = 0x0800;
    private const uint KEYEVENTF_EXTENDEDKEY = 0x0001, KEYEVENTF_KEYUP = 0x0002, KEYEVENTF_SCANCODE = 0x0008;

    [StructLayout(LayoutKind.Sequential)]
    private struct MOUSEINPUT { public int dx; public int dy; public uint mouseData; public uint dwFlags; public uint time; public UIntPtr dwExtraInfo; }

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT { public ushort wVk; public ushort wScan; public uint dwFlags; public uint time; public UIntPtr dwExtraInfo; }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)] public MOUSEINPUT mi;
        [FieldOffset(0)] public KEYBDINPUT ki;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT { public uint type; public InputUnion u; }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    // اسکن‌کد (Set 1)؛ بیت 0x100 = کلید extended
    private static readonly Dictionary<string, int> Keys = BuildKeys();

    private static Dictionary<string, int> BuildKeys()
    {
        var k = new Dictionary<string, int>
        {
            ["Escape"] = 0x01, ["Minus"] = 0x0C, ["Equal"] = 0x0D, ["Backspace"] = 0x0E, ["Tab"] = 0x0F,
            ["BracketLeft"] = 0x1A, ["BracketRight"] = 0x1B, ["Enter"] = 0x1C, ["ControlLeft"] = 0x1D,
            ["Semicolon"] = 0x27, ["Quote"] = 0x28, ["Backquote"] = 0x29, ["ShiftLeft"] = 0x2A, ["Backslash"] = 0x2B,
            ["Comma"] = 0x33, ["Period"] = 0x34, ["Slash"] = 0x35, ["ShiftRight"] = 0x36, ["AltLeft"] = 0x38,
            ["Space"] = 0x39, ["CapsLock"] = 0x3A, ["F11"] = 0x57, ["F12"] = 0x58,
            ["ControlRight"] = 0x11D, ["AltRight"] = 0x138, ["MetaLeft"] = 0x15B, ["MetaRight"] = 0x15C,
            ["Delete"] = 0x153, ["Insert"] = 0x152, ["Home"] = 0x147, ["End"] = 0x14F, ["PageUp"] = 0x149, ["PageDown"] = 0x151,
            ["ArrowUp"] = 0x148, ["ArrowLeft"] = 0x14B, ["ArrowRight"] = 0x14D, ["ArrowDown"] = 0x150,
        };
        const string row1 = "QWERTYUIOP", row2 = "ASDFGHJKL", row3 = "ZXCVBNM";
        for (int i = 0; i < row1.Length; i++) k["Key" + row1[i]] = 0x10 + i;
        for (int i = 0; i < row2.Length; i++) k["Key" + row2[i]] = 0x1E + i;
        for (int i = 0; i < row3.Length; i++) k["Key" + row3[i]] = 0x2C + i;
        for (int i = 1; i <= 9; i++) k["Digit" + i] = 0x01 + i;
        k["Digit0"] = 0x0B;
        for (int i = 1; i <= 10; i++) k["F" + i] = 0x3A + i;
        return k;
    }

    private static readonly object _l = new object();
    private static readonly HashSet<string> _held = new HashSet<string>();
    private static readonly HashSet<char> _btns = new HashSet<char>();

    private static void Send(INPUT i)
    {
        try { SendInput(1, new[] { i }, Marshal.SizeOf(typeof(INPUT))); } catch { }
    }

    private static void SendMouse(uint flags, int dx = 0, int dy = 0, uint data = 0)
    {
        var i = new INPUT { type = INPUT_MOUSE };
        i.u.mi = new MOUSEINPUT { dx = dx, dy = dy, mouseData = data, dwFlags = flags };
        Send(i);
    }

    /// <summary>حرکت نسبی ماوس (پیکسل)</summary>
    public static void MouseMove(int dx, int dy)
    {
        if (dx == 0 && dy == 0) return;
        SendMouse(MOUSEEVENTF_MOVE, dx, dy);
    }

    /// <summary>چرخ ماوس: 120 = یک تیک؛ مثبت = بالا</summary>
    public static void Wheel(int delta)
    {
        if (delta == 0) return;
        SendMouse(MOUSEEVENTF_WHEEL, 0, 0, unchecked((uint)delta));
    }

    /// <summary>b = L / R / M</summary>
    public static void Button(char b, bool down)
    {
        uint flag;
        switch (b)
        {
            case 'L': flag = down ? MOUSEEVENTF_LEFTDOWN : MOUSEEVENTF_LEFTUP; break;
            case 'R': flag = down ? MOUSEEVENTF_RIGHTDOWN : MOUSEEVENTF_RIGHTUP; break;
            case 'M': flag = down ? MOUSEEVENTF_MIDDLEDOWN : MOUSEEVENTF_MIDDLEUP; break;
            default: return;
        }
        lock (_l)
        {
            if (down) { if (!_btns.Add(b)) return; }
            else if (!_btns.Remove(b)) return;
            SendMouse(flag);
        }
    }

    /// <summary>code = KeyboardEvent.code (مثل KeyA، Space، ShiftLeft، ArrowUp)</summary>
    public static void Key(string code, bool down)
    {
        if (!Keys.TryGetValue(code, out int v)) return;
        lock (_l)
        {
            if (down) { if (!_held.Add(code)) return; }
            else if (!_held.Remove(code)) return;
            SendKey(v, down);
        }
    }

    private static void SendKey(int v, bool down)
    {
        uint flags = KEYEVENTF_SCANCODE;
        if ((v & 0x100) != 0) flags |= KEYEVENTF_EXTENDEDKEY;
        if (!down) flags |= KEYEVENTF_KEYUP;
        var i = new INPUT { type = INPUT_KEYBOARD };
        i.u.ki = new KEYBDINPUT { wScan = (ushort)(v & 0xFF), dwFlags = flags };
        Send(i);
    }

    /// <summary>هر کلید و دکمه‌ی ماوسی که گیر کرده آزاد بشه (قطع شدن گوشی، عوض شدن حالت، خاموش کردن اجازه)</summary>
    public static void ReleaseAll()
    {
        lock (_l)
        {
            foreach (var c in _held)
                if (Keys.TryGetValue(c, out int v)) SendKey(v, false);
            _held.Clear();
            foreach (var b in _btns)
                SendMouse(b == 'L' ? MOUSEEVENTF_LEFTUP : b == 'R' ? MOUSEEVENTF_RIGHTUP : MOUSEEVENTF_MIDDLEUP);
            _btns.Clear();
        }
    }
}
