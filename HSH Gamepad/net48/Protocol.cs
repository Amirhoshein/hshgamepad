using System;
using System.Globalization;

namespace GamepadServer;

public static class Protocol
{
    private const int MaxLayoutChars = 8192;

    private static bool F(string s, out float v) =>
        float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out v);

    private static bool LooksLikeJsonObject(string s)
    {
        if (s.Length < 2 || s[0] != '{' || s[s.Length - 1] != '}') return false;
        int depth = 0;
        bool inStr = false;
        for (int i = 0; i < s.Length; i++)
        {
            char c = s[i];
            if (inStr)
            {
                if (c == '\\') i++;
                else if (c == '"') inStr = false;
                else if (c < 0x20) return false;
            }
            else if (c == '"') inStr = true;
            else if (c == '{' || c == '[') depth++;
            else if (c == '}' || c == ']')
            {
                depth--;
                if (depth < 0) return false;
                if (depth == 0 && i != s.Length - 1) return false;
            }
        }
        return depth == 0 && !inStr;
    }

    private static string LayReply(int n, string? json)
    {
        return "{\"t\":\"lay\",\"p\":" + n.ToString(CultureInfo.InvariantCulture) +
               ",\"d\":" + (string.IsNullOrEmpty(json) ? "null" : json) + "}";
    }

    public static void Handle(Conn conn, Hub hub, string line)
    {
        line = line.Trim();
        var p = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (p.Length == 0) return;

        try
        {
            string cmd = p[0];

            if (cmd == "ping" && p.Length == 2)
            {
                _ = conn.SendAsync("pong " + p[1]);
                return;
            }

            if (cmd == "lay")
            {
                int sp0 = line.IndexOf(' ');
                if (sp0 < 0) return;
                var json = line.Substring(sp0 + 1).Trim();
                if (json.Length == 0 || json.Length > MaxLayoutChars) return;
                if (!LooksLikeJsonObject(json)) return;
                conn.LayoutJson = json;
                return;
            }

            if (cmd == "getlay" && p.Length == 2)
            {
                if (!int.TryParse(p[1], out int ln) || ln < 1 || ln > 9) return;
                _ = conn.SendAsync(LayReply(ln, hub.LayoutOf(ln)));
                return;
            }

            if (cmd == "nm")
            {
                int sp = line.IndexOf(' ');
                hub.Rename(conn, sp < 0 ? "" : line.Substring(sp + 1));
                return;
            }

            if (cmd == "cfg" && p.Length == 3)
            {
                if (!int.TryParse(p[1], out int n)) return;
                string mode = p[2];
                if (InputInjector.IsInputMode(mode))
                {
                    if (n != 1 || !hub.AllowInput) mode = "ps4";
                }
                else if (mode != "xbox" && mode != "ps4" && mode != "wheel") return;
                hub.Claim(conn, n, mode);
                return;
            }

            if (cmd == "gyr" && p.Length == 2)
            {
                bool on = p[1] == "1";
                conn.GyroOn = on;
                if (!on) conn.Slot?.StopMotion();
                return;
            }

            if (cmd == "scinfo" || cmd == "sc" || cmd == "scx" || cmd == "sca" || cmd == "tp" || cmd == "tx")
            {
                ScreenCast.Command(conn, hub, p);
                return;
            }

            var slot = conn.Slot;
            if (slot == null) return;

            if (cmd == "gy")
            {
                if (conn.GyroOn && p.Length == 7 &&
                    F(p[1], out var gx) && F(p[2], out var gy) && F(p[3], out var gz) &&
                    F(p[4], out var ax) && F(p[5], out var ay) && F(p[6], out var az))
                    slot.Motion(gx, gy, gz, ax, ay, az);
                return;
            }

            if (cmd == "mm" || cmd == "mw" || cmd == "mb" || cmd == "kb")
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
            if (InputInjector.IsInputMode(conn.Mode)) return;

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
                if (float.TryParse(p[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var tv))
                    slot.Trigger(p[1][0], tv);
            }
        }
        catch
        {
        }
    }
}
