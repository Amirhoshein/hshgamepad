// HSH Gamepad | Telegram: @HSHGamepad
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace GamepadServer;

/// <summary>اطلاعات یک فایل زبان (بخش meta داخل json)</summary>
public sealed class LangInfo
{
    public string Code = "";        // مثلاً fa
    public string Name = "";        // اسم زبان به خود همون زبان، مثلاً «فارسی»
    public string English = "";     // اسم انگلیسی، مثلاً Persian
    public bool Rtl;                // راست‌به‌چپ؟
    public string Path = "";        // آدرس فایل (برای زبان داخل exe خالیه)
    public int Count;               // تعداد متن‌های فایل
    public int Missing;
    public bool Complete => Missing == 0;
}

public enum ImportResult { Ok, Invalid, Exists, Failed }

/// <summary>
/// سیستم زبان: همه‌ی متن‌های برنامه و صفحه‌ی گوشی از فایل‌های json داخل پوشه‌ی «lang» کنار exe خونده میشن.
/// اگه پوشه نبود یا هیچ زبانی توش نبود، فایل‌های پیش‌فرض (داخل خود exe) دوباره تو پوشه نوشته میشن.
/// اگه کلیدی تو یه زبان نبود، اول از انگلیسیِ داخل exe و بعد از فارسیِ داخل exe استفاده میشه (برنامه هیچ‌وقت متن خالی نشون نمیده).
/// همه‌ی زبان‌های داخل exe (فارسی، انگلیسی، آلمانی، ...) همیشه تو لیست هستن، حتی اگه فایلشون از پوشه‌ی lang پاک شده باشه.
/// </summary>
public static class L
{
    public const string DefaultCode = "fa";
    private const string ResPrefix = "lang.default.";
    private const string JsPrefix = "js.";

    private static readonly object Sync = new object();
    private static bool _loaded;
    private static Dictionary<string, string> _fallback = new Dictionary<string, string>();
    private static LangInfo _fallbackInfo = new LangInfo { Code = DefaultCode, Name = "فارسی", English = "Persian", Rtl = true };
    private static Dictionary<string, string> _fallbackEn = new Dictionary<string, string>();
    private static Dictionary<string, string> _cur = new Dictionary<string, string>();
    private static Dictionary<string, string> _curEmb = new Dictionary<string, string>();
    private static Dictionary<string, Dictionary<string, string>>? _embByCode;
    private static LangInfo _curInfo = new LangInfo { Code = DefaultCode, Name = "فارسی", English = "Persian", Rtl = true };
    private static int _version;
    private static string? _htmlCache;
    private static int _htmlVer = -1;
    private static string? _htmlSrc;

    /// <summary>وقتی زبان عوض شد (ممکنه روی هر threadی صدا زده بشه)</summary>
    public static event Action? Changed;

    // ---------------------------------------------------------------- مسیرها
    private static string AppDir()
    {
        try
        {
            string p = Application.ExecutablePath;
            if (!string.IsNullOrEmpty(p))
            {
                string? d = System.IO.Path.GetDirectoryName(p);
                if (!string.IsNullOrEmpty(d)) return d!;
            }
        }
        catch { }
        return AppDomain.CurrentDomain.BaseDirectory;
    }

    /// <summary>پوشه‌ی زبان‌ها: کنار exe</summary>
    public static string Dir
    {
        get { return System.IO.Path.Combine(AppDir(), "lang"); }
    }

    // ---------------------------------------------------------------- وضعیت فعلی
    public static string Code { get { EnsureLoaded(); return _curInfo.Code; } }
    public static string Name { get { EnsureLoaded(); return _curInfo.Name; } }
    public static bool Rtl { get { EnsureLoaded(); return _curInfo.Rtl; } }

    // ---------------------------------------------------------------- ترجمه
    /// <summary>متن یک کلید رو برمیگردونه (اگه تو زبان فعلی نبود از فارسی داخل exe، اگه اونم نبود خود کلید)</summary>
    public static string T(string key)
    {
        EnsureLoaded();
        string? v;
        var cur = _cur;
        if (cur.TryGetValue(key, out v) && !string.IsNullOrEmpty(v)) return v;
        var emb = _curEmb;
        if (emb.TryGetValue(key, out v) && !string.IsNullOrEmpty(v)) return v;
        var en = _fallbackEn;
        if (en.TryGetValue(key, out v) && !string.IsNullOrEmpty(v)) return v;
        var fb = _fallback;
        if (fb.TryGetValue(key, out v) && v != null) return v;
        return key;
    }

    /// <summary>مثل string.Format ولی با متن زبان فعلی؛ جای {0} و {1} و ...</summary>
    public static string F(string key, params object[] args)
    {
        string f = T(key);
        try { return string.Format(f, args); }
        catch (FormatException) { return f; }
    }

    // ---------------------------------------------------------------- بارگذاری
    private static void EnsureLoaded()
    {
        if (_loaded) return;
        lock (Sync)
        {
            if (_loaded) return;
            _loaded = true;
            LoadFallback();
            EnsureDefaultFiles();
            string code = DefaultCode;
            try { code = AppSettings.Load().Language; } catch { }
            LoadCode(code);
        }
    }

    private static Dictionary<string, string>? _embCache;
    private static HashSet<string>? _refKeys;

    private static HashSet<string> ReferenceKeys()
    {
        var cached = _refKeys;
        if (cached != null) return cached;
        var keys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var kv in EmbeddedDefaults())
        {
            if (!TryParse(kv.Value, out var info, out var strings)) continue;
            if (!info.Code.Equals("en", StringComparison.OrdinalIgnoreCase) && !info.Code.Equals(DefaultCode, StringComparison.OrdinalIgnoreCase)) continue;
            keys.UnionWith(strings.Keys);
        }
        _refKeys = keys;
        return keys;
    }

    private static Dictionary<string, string> EmbeddedStrings(string code)
    {
        var map = _embByCode;
        if (map == null)
        {
            map = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
            foreach (var kv in EmbeddedDefaults())
                if (TryParse(kv.Value, out var info, out var strings) && !map.ContainsKey(info.Code)) map[info.Code] = strings;
            _embByCode = map;
        }
        return map.TryGetValue(code, out var found) ? found : new Dictionary<string, string>();
    }

    private static int MissingCount(string code, Dictionary<string, string> strings)
    {
        var emb = EmbeddedStrings(code);
        int n = 0;
        foreach (var k in ReferenceKeys())
            if (!Has(strings, k) && !Has(emb, k)) n++;
        return n;
    }

    private static bool Has(Dictionary<string, string> d, string key)
    {
        return d.TryGetValue(key, out var v) && !string.IsNullOrWhiteSpace(v);
    }

    private static Dictionary<string, string> EmbeddedDefaults()
    {
        var cached = _embCache;
        if (cached != null) return cached;
        var res = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            var asm = Assembly.GetExecutingAssembly();
            foreach (var n in asm.GetManifestResourceNames())
            {
                if (!n.StartsWith(ResPrefix, StringComparison.OrdinalIgnoreCase) || !n.EndsWith(".json", StringComparison.OrdinalIgnoreCase)) continue;
                using (var s = asm.GetManifestResourceStream(n))
                {
                    if (s == null) continue;
                    using (var r = new StreamReader(s, Encoding.UTF8))
                        res[n.Substring(ResPrefix.Length)] = r.ReadToEnd();
                }
            }
        }
        catch { }
        if (res.Count > 0) _embCache = res;
        return res;
    }

    private static void LoadFallback()
    {
        foreach (var kv in EmbeddedDefaults())
        {
            LangInfo info; Dictionary<string, string> strings;
            if (!TryParse(kv.Value, out info, out strings)) continue;
            if (info.Code.Equals("en", StringComparison.OrdinalIgnoreCase)) _fallbackEn = strings;
            if (!info.Code.Equals(DefaultCode, StringComparison.OrdinalIgnoreCase)) continue;
            _fallback = strings;
            _fallbackInfo = info;
            _cur = strings;
            _curInfo = info;
        }
    }

    /// <summary>متن json یه زبانِ داخل exe (یا null)</summary>
    private static string? EmbeddedText(string code)
    {
        foreach (var kv in EmbeddedDefaults())
        {
            LangInfo i; Dictionary<string, string> s;
            if (TryParse(kv.Value, out i, out s) && i.Code.Equals(code, StringComparison.OrdinalIgnoreCase)) return kv.Value;
        }
        return null;
    }

    /// <summary>اگه پوشه‌ی lang نبود یا هیچ زبان سالمی توش نبود، فایل‌های پیش‌فرض رو دوباره مینویسه</summary>
    private static void EnsureDefaultFiles()
    {
        try
        {
            Directory.CreateDirectory(Dir);
            foreach (var f in Directory.GetFiles(Dir, "*.json"))
            {
                LangInfo i; Dictionary<string, string> s;
                if (TryParseFile(f, out i, out s)) return;      // حداقل یه زبان سالم هست
            }
            foreach (var kv in EmbeddedDefaults())
                File.WriteAllText(System.IO.Path.Combine(Dir, kv.Key), kv.Value, new UTF8Encoding(false));
        }
        catch { }
    }

    private static void LoadCode(string code)
    {
        foreach (var info in Languages())
        {
            if (!info.Code.Equals(code, StringComparison.OrdinalIgnoreCase)) continue;
            LangInfo i; Dictionary<string, string> s;
            if (info.Path.Length > 0)
            {
                if (!TryParseFile(info.Path, out i, out s)) continue;
            }
            else
            {
                string? txt = EmbeddedText(info.Code);      // زبان داخل exe که فایلش تو پوشه نیست
                if (txt == null || !TryParse(txt, out i, out s)) continue;
            }
            _cur = s; _curInfo = i;
            _curEmb = EmbeddedStrings(i.Code);
            _version++;
            return;
        }
        // زبان خواسته‌شده پیدا نشد: فارسی داخل exe
        _cur = _fallback; _curInfo = _fallbackInfo;
        _curEmb = new Dictionary<string, string>();
        _version++;
    }

    /// <summary>زبان فعلی رو عوض میکنه (ذخیره‌کردنش تو تنظیمات کار صدا زننده‌ست)</summary>
    public static void Select(string code)
    {
        EnsureLoaded();
        lock (Sync) { LoadCode(code); }
        var h = Changed;
        if (h != null) h();
    }

    /// <summary>
    /// همه‌ی زبان‌های سالم: هر فایل json با هر اسمی داخل پوشه‌ی lang + زبان‌های داخل exe که فایلشون تو پوشه نیست.
    /// ترتیب: فارسی، انگلیسی، بعد بقیه به ترتیب اسم انگلیسی.
    /// </summary>
    public static List<LangInfo> Languages()
    {
        var list = new List<LangInfo>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            string[] files = Directory.Exists(Dir) ? Directory.GetFiles(Dir, "*.json") : new string[0];
            Array.Sort(files, StringComparer.OrdinalIgnoreCase);
            foreach (var f in files)
            {
                LangInfo i; Dictionary<string, string> s;
                if (!TryParseFile(f, out i, out s)) continue;
                if (!seen.Add(i.Code)) continue;
                i.Path = f; i.Count = s.Count; i.Missing = MissingCount(i.Code, s);
                list.Add(i);
            }
        }
        catch { }
        try
        {
            foreach (var kv in EmbeddedDefaults())
            {
                LangInfo i; Dictionary<string, string> s;
                if (!TryParse(kv.Value, out i, out s)) continue;
                if (!seen.Add(i.Code)) continue;
                i.Path = ""; i.Count = s.Count; i.Missing = MissingCount(i.Code, s);
                list.Add(i);
            }
        }
        catch { }
        if (list.Count == 0)
        {
            var fi = new LangInfo { Code = _fallbackInfo.Code, Name = _fallbackInfo.Name, English = _fallbackInfo.English, Rtl = _fallbackInfo.Rtl, Count = _fallback.Count };
            list.Add(fi);
        }
        list.Sort(CompareLang);
        return list;
    }

    private static int Rank(LangInfo i)
    {
        if (i.Code.Equals("fa", StringComparison.OrdinalIgnoreCase)) return 0;
        if (i.Code.Equals("en", StringComparison.OrdinalIgnoreCase)) return 1;
        return 2;
    }

    private static int CompareLang(LangInfo a, LangInfo b)
    {
        int c = Rank(a).CompareTo(Rank(b));
        if (c != 0) return c;
        string an = a.English.Length > 0 ? a.English : a.Name;
        string bn = b.English.Length > 0 ? b.English : b.Name;
        c = string.Compare(an, bn, StringComparison.OrdinalIgnoreCase);
        if (c != 0) return c;
        return string.Compare(a.Code, b.Code, StringComparison.OrdinalIgnoreCase);
    }

    // ---------------------------------------------------------------- ایمپورت / اکسپورت
    private static readonly Regex CodeRx = new Regex("^[A-Za-z]{2,3}([_-][A-Za-z0-9]{2,8})?$");

    public static ImportResult Import(string srcPath, bool overwrite, out LangInfo info)
    {
        info = new LangInfo();
        try
        {
            string text = File.ReadAllText(srcPath, Encoding.UTF8);
            Dictionary<string, string> strings;
            if (!TryParse(text, out info, out strings)) return ImportResult.Invalid;
            Directory.CreateDirectory(Dir);
            string dest = System.IO.Path.Combine(Dir, info.Code + ".json");
            // اگه همین زبان قبلاً (با هر اسم فایلی) تو پوشه یا داخل exe هست، همونو جایگزین کن
            bool existing = false;
            foreach (var old in Languages())
            {
                if (!old.Code.Equals(info.Code, StringComparison.OrdinalIgnoreCase)) continue;
                existing = true;
                if (old.Path.Length > 0) dest = old.Path;
                break;
            }
            if ((existing || File.Exists(dest)) && !overwrite) return ImportResult.Exists;
            File.WriteAllText(dest, text, new UTF8Encoding(false));
            info.Path = dest; info.Count = strings.Count;
            if (info.Code.Equals(Code, StringComparison.OrdinalIgnoreCase)) Select(info.Code);   // زبان فعلی عوض شد: دوباره بخون
            return ImportResult.Ok;
        }
        catch { return ImportResult.Failed; }
    }

    public static bool Export(string code, string destPath)
    {
        try
        {
            foreach (var info in Languages())
            {
                if (!info.Code.Equals(code, StringComparison.OrdinalIgnoreCase)) continue;
                if (info.Path.Length > 0) { File.Copy(info.Path, destPath, true); return true; }
            }
            foreach (var kv in EmbeddedDefaults())
            {
                LangInfo i; Dictionary<string, string> s;
                if (TryParse(kv.Value, out i, out s) && i.Code.Equals(code, StringComparison.OrdinalIgnoreCase))
                { File.WriteAllText(destPath, kv.Value, new UTF8Encoding(false)); return true; }
            }
        }
        catch { }
        return false;
    }

    public static void OpenFolder()
    {
        try
        {
            Directory.CreateDirectory(Dir);
            Process.Start(new ProcessStartInfo("explorer.exe", "\"" + Dir + "\"") { UseShellExecute = true });
        }
        catch { }
    }

    // ---------------------------------------------------------------- صفحه‌ی گوشی
    /// <summary>
    /// index.html رو با زبان فعلی آماده میکنه: توکن‌های {{t:key}} (متن)، {{a:key}} (مقدار attribute)، {{c:key}} (رشته‌ی CSS)،
    /// {{meta:code}} و دیکشنری متن‌های جاوااسکریپت (جای /*__LANG__*/{}) جایگزین میشن.
    /// </summary>
    public static string RenderIndex(string raw)
    {
        EnsureLoaded();
        lock (Sync)
        {
            if (_htmlCache != null && _htmlVer == _version && ReferenceEquals(_htmlSrc, raw)) return _htmlCache;
        }
        string r = Regex.Replace(raw, @"\{\{([tac]):([A-Za-z0-9_.\-]+)\}\}", m =>
        {
            string v = T(m.Groups[2].Value);
            switch (m.Groups[1].Value)
            {
                case "a": return v.Replace("\"", "&quot;");
                case "c": return v.Replace("\\", "\\\\").Replace("'", "\\'").Replace("\r", "").Replace("\n", "\\A ");
                default: return v;
            }
        });
        r = r.Replace("{{meta:code}}", Code);
        r = r.Replace("{{meta:dir}}", Rtl ? "rtl" : "ltr");
        r = r.Replace("/*__LANG__*/{}", BuildJsDict());
        lock (Sync) { _htmlCache = r; _htmlVer = _version; _htmlSrc = raw; }
        return r;
    }

    private static string BuildJsDict()
    {
        var keys = new SortedDictionary<string, bool>(StringComparer.Ordinal);
        foreach (var k in _fallback.Keys) if (k.StartsWith(JsPrefix, StringComparison.Ordinal)) keys[k] = true;
        foreach (var k in _cur.Keys) if (k.StartsWith(JsPrefix, StringComparison.Ordinal)) keys[k] = true;
        foreach (var k in _fallbackEn.Keys) if (k.StartsWith(JsPrefix, StringComparison.Ordinal)) keys[k] = true;
        var sb = new StringBuilder("{");
        bool first = true;
        foreach (var k in keys.Keys)
        {
            if (!first) sb.Append(',');
            first = false;
            JsonLite.WriteString(sb, k);
            sb.Append(':');
            JsonLite.WriteString(sb, T(k));
        }
        sb.Append('}');
        return sb.ToString();
    }

    // ---------------------------------------------------------------- خوندن فایل
    private static bool TryParseFile(string path, out LangInfo info, out Dictionary<string, string> strings)
    {
        info = new LangInfo(); strings = new Dictionary<string, string>();
        try { return TryParse(File.ReadAllText(path, Encoding.UTF8), out info, out strings); }
        catch { return false; }
    }

    private static bool TryParse(string json, out LangInfo info, out Dictionary<string, string> strings)
    {
        info = new LangInfo(); strings = new Dictionary<string, string>();
        try
        {
            var root = JsonLite.Parse(json) as Dictionary<string, object?>;
            if (root == null) return false;
            object? mo; object? so;
            if (!root.TryGetValue("meta", out mo) || !root.TryGetValue("strings", out so)) return false;
            var meta = mo as Dictionary<string, object?>;
            var str = so as Dictionary<string, object?>;
            if (meta == null || str == null) return false;

            string code = GetStr(meta, "code");
            string name = GetStr(meta, "name");
            if (!CodeRx.IsMatch(code) || name.Length == 0) return false;
            info.Code = code;
            info.Name = name;
            info.English = GetStr(meta, "english");
            object? rtl;
            info.Rtl = meta.TryGetValue("rtl", out rtl) && rtl is bool && (bool)rtl;

            var d = new Dictionary<string, string>(StringComparer.Ordinal);
            Flatten(str, "", d);
            strings = d;
            info.Count = d.Count;
            return true;
        }
        catch { return false; }
    }

    /// <summary>بخش‌های تو در تو ("html": { "Save": "..." }) رو به کلید تخت "html.Save" تبدیل میکنه</summary>
    private static void Flatten(Dictionary<string, object?> node, string prefix, Dictionary<string, string> d)
    {
        foreach (var kv in node)
        {
            string key = prefix.Length == 0 ? kv.Key : prefix + "." + kv.Key;
            var s = kv.Value as string;
            if (s != null) { d[key] = s; continue; }
            var sub = kv.Value as Dictionary<string, object?>;
            if (sub != null) Flatten(sub, key, d);
        }
    }

    private static string GetStr(Dictionary<string, object?> d, string k)
    {
        object? v;
        if (d.TryGetValue(k, out v) && v is string) return ((string)v).Trim();
        return "";
    }
}

/// <summary>خوندن/نوشتن json خیلی ساده (بدون وابستگی؛ تو .NET Framework هم کار میکنه)</summary>
internal static class JsonLite
{
    public static object? Parse(string s)
    {
        if (s.Length > 0 && s[0] == '\uFEFF') s = s.Substring(1);
        int i = 0;
        object? v = ParseValue(s, ref i);
        SkipWs(s, ref i);
        if (i != s.Length) throw new FormatException("json");
        return v;
    }

    private static void SkipWs(string s, ref int i)
    {
        while (i < s.Length && char.IsWhiteSpace(s[i])) i++;
    }

    private static object? ParseValue(string s, ref int i)
    {
        SkipWs(s, ref i);
        if (i >= s.Length) throw new FormatException("json");
        char c = s[i];
        if (c == '{')
        {
            var d = new Dictionary<string, object?>(StringComparer.Ordinal);
            i++; SkipWs(s, ref i);
            if (i < s.Length && s[i] == '}') { i++; return d; }
            while (true)
            {
                SkipWs(s, ref i);
                string k = ParseString(s, ref i);
                SkipWs(s, ref i);
                if (i >= s.Length || s[i] != ':') throw new FormatException("json");
                i++;
                d[k] = ParseValue(s, ref i);
                SkipWs(s, ref i);
                if (i >= s.Length) throw new FormatException("json");
                if (s[i] == ',') { i++; continue; }
                if (s[i] == '}') { i++; return d; }
                throw new FormatException("json");
            }
        }
        if (c == '[')
        {
            var l = new List<object?>();
            i++; SkipWs(s, ref i);
            if (i < s.Length && s[i] == ']') { i++; return l; }
            while (true)
            {
                l.Add(ParseValue(s, ref i));
                SkipWs(s, ref i);
                if (i >= s.Length) throw new FormatException("json");
                if (s[i] == ',') { i++; continue; }
                if (s[i] == ']') { i++; return l; }
                throw new FormatException("json");
            }
        }
        if (c == '"') return ParseString(s, ref i);
        if (string.CompareOrdinal(s, i, "true", 0, 4) == 0) { i += 4; return true; }
        if (string.CompareOrdinal(s, i, "false", 0, 5) == 0) { i += 5; return false; }
        if (string.CompareOrdinal(s, i, "null", 0, 4) == 0) { i += 4; return null; }
        int st = i;
        while (i < s.Length && (char.IsDigit(s[i]) || s[i] == '-' || s[i] == '+' || s[i] == '.' || s[i] == 'e' || s[i] == 'E')) i++;
        if (i == st) throw new FormatException("json");
        return double.Parse(s.Substring(st, i - st), CultureInfo.InvariantCulture);
    }

    private static string ParseString(string s, ref int i)
    {
        if (i >= s.Length || s[i] != '"') throw new FormatException("json");
        i++;
        var sb = new StringBuilder();
        while (true)
        {
            if (i >= s.Length) throw new FormatException("json");
            char c = s[i++];
            if (c == '"') break;
            if (c != '\\') { sb.Append(c); continue; }
            if (i >= s.Length) throw new FormatException("json");
            char e = s[i++];
            switch (e)
            {
                case '"': sb.Append('"'); break;
                case '\\': sb.Append('\\'); break;
                case '/': sb.Append('/'); break;
                case 'b': sb.Append('\b'); break;
                case 'f': sb.Append('\f'); break;
                case 'n': sb.Append('\n'); break;
                case 'r': sb.Append('\r'); break;
                case 't': sb.Append('\t'); break;
                case 'u':
                    if (i + 4 > s.Length) throw new FormatException("json");
                    sb.Append((char)int.Parse(s.Substring(i, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                    i += 4;
                    break;
                default: throw new FormatException("json");
            }
        }
        return sb.ToString();
    }

    /// <summary>رشته‌ی json (برای گذاشتن داخل &lt;script&gt; هم امنه)</summary>
    public static void WriteString(StringBuilder sb, string v)
    {
        sb.Append('"');
        foreach (char c in v)
        {
            switch (c)
            {
                case '"': sb.Append("\\\""); break;
                case '\\': sb.Append("\\\\"); break;
                case '\n': sb.Append("\\n"); break;
                case '\r': sb.Append("\\r"); break;
                case '\t': sb.Append("\\t"); break;
                case '<': sb.Append("\\u003c"); break;
                case '\u2028': sb.Append("\\u2028"); break;
                case '\u2029': sb.Append("\\u2029"); break;
                default:
                    if (c < ' ') sb.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                    else sb.Append(c);
                    break;
            }
        }
        sb.Append('"');
    }
}

/// <summary>
/// زبانی که کاربر موقع نصب انتخاب کرده. نصب‌کننده فایل setup-language.txt رو کنار exe میذاره
/// (خط اول: کد زبان، خط دوم: یه مهر یکتا برای هر نصب). برنامه اولین باری که این مهر رو میبینه زبان رو اعمال میکنه
/// و مهر رو تو تنظیمات یادداشت میکنه؛ پس بعداً تغییر زبان از داخل برنامه دوباره بازنویسی نمیشه، ولی نصب مجدد با انتخاب جدید اعمال میشه.
/// </summary>
internal static class SetupChoice
{
    public static bool Read(out string code, out string stamp)
    {
        code = ""; stamp = "";
        try
        {
            string f = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "setup-language.txt");
            if (!File.Exists(f)) return false;
            var lines = File.ReadAllLines(f, Encoding.UTF8);
            if (lines.Length == 0) return false;
            string c = lines[0].Trim().TrimStart('\uFEFF');
            if (!Regex.IsMatch(c, @"^[A-Za-z0-9_\-]{1,32}$")) return false;
            code = c;
            stamp = lines.Length > 1 ? lines[1].Trim() : "";
            if (stamp.Length == 0) stamp = File.GetLastWriteTimeUtc(f).Ticks.ToString(CultureInfo.InvariantCulture);
            return true;
        }
        catch { return false; }
    }
}
