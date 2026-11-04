// HSH Gamepad | Telegram: @HSHGamepad
using System;
using System.Globalization;
using System.Text.Json;

namespace GamepadServer;

/// <summary>
/// پروتکل (متن کوتاه روی WebSocket، هر خط یک دستور):
///   cfg &lt;1-9&gt; &lt;xbox|ps4|wheel|mouse|keyboard&gt; (ماوس/کیبورد فقط پلیر ۱)  /  b &lt;KEY&gt; &lt;0|1&gt;  /  l &lt;x&gt; &lt;y&gt;  /  r &lt;x&gt; &lt;y&gt;
///   ping &lt;t&gt;             (جواب: pong &lt;t&gt; برای نمایش تاخیر)
///   tr &lt;l|r&gt; &lt;0..1&gt;    (پدال آنالوگ فرمون: l = ترمز/LT ، r = گاز/RT)
///   gyr &lt;0|1&gt;            (ژیروسکوپ گوشی روشن/خاموش)
///   gy &lt;gx&gt; &lt;gy&gt; &lt;gz&gt; &lt;ax&gt; &lt;ay&gt; &lt;az&gt;   (نمونه‌ی ژیروسکوپ °/s و شتاب m/s² — جزئیات تو Slot.cs)
///   mm &lt;dx&gt; &lt;dy&gt; / mw &lt;d&gt; / mb &lt;L|R|M&gt; &lt;0|1&gt; / kb &lt;code&gt; &lt;0|1&gt;   (ماوس و کیبورد؛ جزئیات تو InputInjector.cs)
///   nm &lt;name&gt;              (عوض کردن اسم)
///   lay &lt;json&gt;             (گوشی چیدمان ذخیره‌شده‌ی خودش رو میفرسته)
///   getlay &lt;1-9&gt;           (چیدمان گوشیِ اون پلیر رو بده) -> جواب: {"t":"lay","p":n,"d":json|null}
/// </summary>
public static class Protocol
{
    private const int MaxLayoutChars = 8192;

    private static bool F(string s, out float v) =>
        float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out v);

    /// <summary>جواب getlay: JSON چیدمان همون‌طور که هست (قبلاً اعتبارش چک شده)، یا d = null</summary>
    private static string LayReply(int n, string? json)
    {
        if (string.IsNullOrEmpty(json))
            return JsonSerializer.Serialize(new { t = "lay", p = n, d = (object?)null });
        using var doc = JsonDocument.Parse(json);
        return JsonSerializer.Serialize(new { t = "lay", p = n, d = doc.RootElement });
    }

    public static void Handle(Conn conn, Hub hub, string line)
    {
        var p = line.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (p.Length == 0) return;

        try
        {
            string cmd = p[0];

            if (cmd == "ping" && p.Length == 2)   // اندازه‌گیری تاخیر: همون عدد رو برمیگردونیم
            {
                _ = conn.SendAsync("pong " + p[1]);
                return;
            }

            if (cmd == "nm")                    // عوض کردن اسم نمایشی (خیلی کم پیش میاد)
            {
                int sp = line.IndexOf(' ');
                hub.Rename(conn, sp < 0 ? "" : line[(sp + 1)..]);
                return;
            }

            if (cmd == "lay")                   // چیدمان این گوشی؛ فقط اگه JSON معتبر و کوچیک باشه نگه داشته میشه
            {
                int sp = line.IndexOf(' ');
                if (sp < 0) return;
                var json = line[(sp + 1)..].Trim();
                if (json.Length == 0 || json.Length > MaxLayoutChars) return;
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.ValueKind != JsonValueKind.Object) return;
                conn.LayoutJson = json;
                return;
            }

            if (cmd == "getlay" && p.Length == 2)
            {
                if (!int.TryParse(p[1], out int n) || n < 1 || n > 9) return;
                _ = conn.SendAsync(LayReply(n, hub.LayoutOf(n)));
                return;
            }

            if (cmd == "cfg" && p.Length == 3)
            {
                if (!int.TryParse(p[1], out int n)) return;
                string mode = p[2];
                if (InputInjector.IsInputMode(mode))      // ماوس/کیبورد: فقط پلیر ۱ و فقط وقتی تو پنجره‌ی سرور اجازه داده شده؛ وگرنه PS4 میشه
                {
                    if (n != 1 || !hub.AllowInput) mode = "ps4";
                }
                else if (mode != "xbox" && mode != "ps4" && mode != "wheel") return;   // wheel = فرمون
                hub.Claim(conn, n, mode);      // جواب (قبول/رد) از طریق پیام state به گوشی میرسه
                return;
            }

            if (cmd == "gyr" && p.Length == 2)
            {
                bool on = p[1] == "1";
                conn.GyroOn = on;
                if (!on) conn.Slot?.StopMotion();
                return;
            }

            var slot = conn.Slot;
            if (slot == null) return;           // تا پلیر نگرفته، ورودی‌ها نادیده گرفته میشن

            if (cmd == "gy")                    // نمونه‌ی ژیروسکوپ؛ ساده و سریع (۶۰ بار در ثانیه)، بدون ساختن دسته
            {
                if (conn.GyroOn && p.Length == 7 &&
                    F(p[1], out var gx) && F(p[2], out var gy) && F(p[3], out var gz) &&
                    F(p[4], out var ax) && F(p[5], out var ay) && F(p[6], out var az))
                    slot.Motion(gx, gy, gz, ax, ay, az);
                return;
            }

            if (cmd == "mm" || cmd == "mw" || cmd == "mb" || cmd == "kb")      // ماوس و کیبورد (فقط پلیر ۱)
            {
                if (!hub.AllowInput || slot.N != 1 || !InputInjector.IsInputMode(conn.Mode)) return;
                int a, b;
                if (cmd == "mm" && p.Length == 3 && int.TryParse(p[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out a) &&
                    int.TryParse(p[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out b))
                    InputInjector.MouseMove(Math.Max(-2000, Math.Min(2000, a)), Math.Max(-2000, Math.Min(2000, b)));
                else if (cmd == "mw" && p.Length == 2 && int.TryParse(p[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out a))
                    InputInjector.Wheel(Math.Max(-12000, Math.Min(12000, a)));
                else if (cmd == "mb" && p.Length == 3 && p[1].Length == 1)
                    InputInjector.Button(p[1][0], p[2] == "1");
                else if (cmd == "kb" && p.Length == 3)
                    InputInjector.Key(p[1], p[2] == "1");
                return;
            }
            if (InputInjector.IsInputMode(conn.Mode)) return;      // تو حالت ماوس/کیبورد دسته‌ای وجود نداره

            if (!slot.HasPad)
            {
                var actual = slot.SetMode(conn.Mode);
                if (actual != null) conn.Mode = actual;
            }

            if (cmd == "b" && p.Length == 3)
            {
                slot.Button(p[1], p[2] == "1");
            }
            else if ((cmd == "l" || cmd == "r") && p.Length == 3)
            {
                if (float.TryParse(p[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var x) &&
                    float.TryParse(p[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var y))
                    slot.Stick(cmd[0], x, y);
            }
            else if (cmd == "tr" && p.Length == 3 && (p[1] == "l" || p[1] == "r"))
            {
                // پدال آنالوگ فرمون: tr <l|r> <0..1>
                if (float.TryParse(p[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var tv))
                    slot.Trigger(p[1][0], tv);
            }
        }
        catch
        {
            // یه دستور خراب نباید کل اتصال رو بندازه
        }
    }
}
