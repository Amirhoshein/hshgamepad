// HSH Gamepad | Telegram: @HSHGamepad
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace GamepadServer;

/// <summary>
/// چیدمان‌ها و تم‌هایی که کاربر از گوشی رو کامپیوتر ذخیره میکنه: پوشه‌ی data کنار برنامه: data\presets\layouts و data\presets\themes
/// هر مورد یه فایل .json ئه. فایل‌هایی که خودت (یا یه دوست) تو این پوشه بذاری هم تو لیست گوشی میاد (نمونه‌ی آماده).
/// دستورهای گوشی (اسم با encodeURIComponent):  pl kind | pg kind name | ps kind name json | pd kind name
/// جواب‌ها: {"t":"pre","op":"list|get|save|del","kind":..,...}
/// </summary>
internal static class Presets
{
    private const int MaxChars = 48 * 1024;       // سقف حجم یه مورد
    private const int MaxItems = 200;             // سقف تعداد هر نوع
    private static readonly object Sync = new object();

    private static readonly string[] Reserved =
    {
        "CON", "PRN", "AUX", "NUL", "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
    };

    public static string Root => Path.Combine(AppPaths.DataDir, "presets");

    /// <summary>دکمه‌ی Presets تو پنجره‌ی اصلی: پوشه رو (با دو زیرپوشه) میسازه و تو Explorer باز میکنه</summary>
    public static void OpenFolder()
    {
        try
        {
            Directory.CreateDirectory(Path.Combine(Root, "layouts"));
            Directory.CreateDirectory(Path.Combine(Root, "themes"));
            Process.Start(new ProcessStartInfo("explorer.exe", "\"" + Root + "\"") { UseShellExecute = true });
        }
        catch { }
    }

    private static string? KindDir(string kind)
    {
        if (kind != "layouts" && kind != "themes") return null;
        return Path.Combine(Root, kind);
    }

    /// <summary>اسم امن برای فایل: کاراکترهای ممنوع => _ ، فاصله/نقطه‌ی اول و آخر حذف، حداکثر ۴۸ کاراکتر؛ نامعتبر => خالی</summary>
    public static string CleanName(string? s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        var bad = Path.GetInvalidFileNameChars();
        var sb = new StringBuilder();
        foreach (char c in s!)
            sb.Append(c < 32 || Array.IndexOf(bad, c) >= 0 ? '_' : c);
        string r = sb.ToString().Trim(' ', '.');
        if (r.Length > 48) r = r.Substring(0, 48).Trim(' ', '.');
        if (r.Length == 0) return "";
        int dot = r.IndexOf('.');
        string stem = (dot < 0 ? r : r.Substring(0, dot)).ToUpperInvariant();
        if (Array.IndexOf(Reserved, stem) >= 0) return "";
        return r;
    }

    private static List<string> ListNames(string dir)
    {
        var l = new List<string>();
        try
        {
            if (Directory.Exists(dir))
                foreach (var f in Directory.GetFiles(dir, "*.json"))
                {
                    string n = Path.GetFileNameWithoutExtension(f);
                    if (CleanName(n) == n) l.Add(n);       // اسم‌هایی که گوشی نتونه ازشون استفاده کنه نشون داده نمیشه
                }
        }
        catch { }
        l.Sort(StringComparer.CurrentCultureIgnoreCase);
        if (l.Count > MaxItems) l.RemoveRange(MaxItems, l.Count - MaxItems);
        return l;
    }

    private static string? ReadFile(string dir, string name)
    {
        try
        {
            string path = Path.Combine(dir, name + ".json");
            var fi = new FileInfo(path);
            if (!fi.Exists || fi.Length > MaxChars * 4L) return null;
            string t = File.ReadAllText(path, Encoding.UTF8);
            if (t.Length > MaxChars) return null;
            t = t.Trim().TrimStart('\uFEFF');
            return t.Length > 1 && t[0] == '{' ? t : null;
        }
        catch { return null; }
    }

    private static bool WriteFile(string dir, string name, string json)
    {
        try
        {
            Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, name + ".json");
            if (!File.Exists(path) && Directory.GetFiles(dir, "*.json").Length >= MaxItems) return false;
            string tmp = path + ".tmp";
            File.WriteAllText(tmp, json, new UTF8Encoding(false));
            if (File.Exists(path)) File.Delete(path);
            File.Move(tmp, path);
            return true;
        }
        catch { return false; }
    }

    private static bool DeleteFile(string dir, string name)
    {
        try
        {
            string path = Path.Combine(dir, name + ".json");
            if (!File.Exists(path)) return false;
            File.Delete(path);
            return true;
        }
        catch { return false; }
    }

    // ---------------- JSON دستی (net48 کتابخونه‌ی JSON نداره) ----------------
    private static void Q(StringBuilder sb, string s)
    {
        sb.Append('"');
        foreach (char c in s)
        {
            switch (c)
            {
                case '"': sb.Append("\\\""); break;
                case '\\': sb.Append("\\\\"); break;
                case '\n': sb.Append("\\n"); break;
                case '\r': sb.Append("\\r"); break;
                case '\t': sb.Append("\\t"); break;
                default:
                    if (c < 32) sb.Append("\\u").Append(((int)c).ToString("x4"));
                    else sb.Append(c);
                    break;
            }
        }
        sb.Append('"');
    }

    private static void ItemsJson(StringBuilder sb, List<string> items)
    {
        sb.Append("\"items\":[");
        for (int i = 0; i < items.Count; i++)
        {
            if (i > 0) sb.Append(',');
            Q(sb, items[i]);
        }
        sb.Append(']');
    }

    private static string Reply(string op, string kind, string? name, string? data, int ok, List<string>? items)
    {
        var sb = new StringBuilder("{\"t\":\"pre\",\"op\":\"").Append(op).Append("\",\"kind\":\"").Append(kind).Append('"');
        if (name != null) { sb.Append(",\"name\":"); Q(sb, name); }
        if (op == "get")
        {
            sb.Append(",\"d\":");
            if (data == null) sb.Append("null"); else Q(sb, data);        // به‌صورت رشته: فایل دستی خراب هم کل پیام رو خراب نمیکنه
        }
        if (op == "save" || op == "del") sb.Append(",\"ok\":").Append(ok);
        if (items != null) { sb.Append(','); ItemsJson(sb, items); }
        return sb.Append('}').ToString();
    }

    // ---------------- دستور گوشی ----------------
    public static void Command(Conn conn, string line)
    {
        try
        {
            // line = "<cmd> <kind> [<name>] [<json>]"
            int i1 = line.IndexOf(' ');
            if (i1 < 0) return;
            string cmd = line.Substring(0, i1).Trim();
            string rest = line.Substring(i1 + 1);
            int i2 = rest.IndexOf(' ');
            string kind = (i2 < 0 ? rest : rest.Substring(0, i2)).Trim();
            string rest2 = i2 < 0 ? "" : rest.Substring(i2 + 1);
            int i3 = rest2.IndexOf(' ');
            string encName = (i3 < 0 ? rest2 : rest2.Substring(0, i3)).Trim();
            string payload = i3 < 0 ? "" : rest2.Substring(i3 + 1).Trim();

            string? dir = KindDir(kind);
            if (dir == null) return;

            string name = "";
            if (encName.Length > 0)
            {
                string dec;
                try { dec = Uri.UnescapeDataString(encName); } catch { return; }
                name = CleanName(dec);
            }

            lock (Sync)
            {
                if (cmd == "pl")
                {
                    _ = conn.SendAsync(Reply("list", kind, null, null, 0, ListNames(dir)));
                }
                else if (cmd == "pg")
                {
                    string? d = name.Length == 0 ? null : ReadFile(dir, name);
                    _ = conn.SendAsync(Reply("get", kind, name, d, 0, null));
                }
                else if (cmd == "ps")
                {
                    bool ok = name.Length > 0
                        && payload.Length > 1 && payload.Length <= MaxChars
                        && payload[0] == '{' && payload[payload.Length - 1] == '}'
                        && payload.IndexOf('\0') < 0
                        && WriteFile(dir, name, payload);
                    _ = conn.SendAsync(Reply("save", kind, name, null, ok ? 1 : 0, ListNames(dir)));
                }
                else if (cmd == "pd")
                {
                    bool ok = name.Length > 0 && DeleteFile(dir, name);
                    _ = conn.SendAsync(Reply("del", kind, name, null, ok ? 1 : 0, ListNames(dir)));
                }
            }
        }
        catch { }
    }
}
