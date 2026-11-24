// HSH Gamepad | Telegram: @HSHGamepad
using System;
using System.IO;
using System.Windows.Forms;

namespace GamepadServer;

/// <summary>
/// هرچی برنامه ذخیره میکنه (تنظیمات، چیدمان و تم‌های ذخیره‌شده، لاگ، آیکون‌ها) تو پوشه‌ی «data» کنار exe میره (مثل پوشه‌ی lang).
/// اگه اون پوشه قابل نوشتن نباشه (مثلاً نصب تو Program Files و اجرای بدون ادمین)، مثل قبل %AppData%\GamepadServer استفاده میشه.
/// بار اول فایل‌های قدیمیِ %AppData%\GamepadServer یه بار به پوشه‌ی جدید کپی میشن (پوشه‌ی قدیمی دست نمیخوره).
/// </summary>
internal static class AppPaths
{
    private static readonly object Sync = new object();
    private static string? _dir;

    /// <summary>جای قدیمیِ داده‌ها (و جای جایگزین وقتی کنار exe نمیشه نوشت)</summary>
    public static string Legacy => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GamepadServer");

    private static string AppDir()
    {
        try
        {
            string p = Application.ExecutablePath;
            if (!string.IsNullOrEmpty(p))
            {
                string? d = Path.GetDirectoryName(p);
                if (!string.IsNullOrEmpty(d)) return d!;
            }
        }
        catch { }
        return AppDomain.CurrentDomain.BaseDirectory;
    }

    /// <summary>پوشه‌ی داده‌ها؛ همیشه وجود داره (ساخته میشه)</summary>
    public static string DataDir
    {
        get
        {
            lock (Sync)
            {
                if (_dir != null) return _dir;
                string want = Path.Combine(AppDir(), "data");
                string use = Writable(want) ? want : Legacy;
                if (!string.Equals(use, Legacy, StringComparison.OrdinalIgnoreCase)) Migrate(Legacy, use);
                try { Directory.CreateDirectory(use); } catch { }
                _dir = use;
                return use;
            }
        }
    }

    private static bool Writable(string dir)
    {
        try
        {
            Directory.CreateDirectory(dir);
            string t = Path.Combine(dir, ".w" + Guid.NewGuid().ToString("N"));
            File.WriteAllText(t, "x");
            File.Delete(t);
            return true;
        }
        catch { return false; }
    }

    private static void Migrate(string from, string to)
    {
        try
        {
            string marker = Path.Combine(to, ".migrated");
            if (File.Exists(marker)) return;
            if (Directory.Exists(from)) CopyDir(from, to);
            File.WriteAllText(marker, "1");       // فقط یه بار؛ اگه بعداً داده‌ها رو پاک کنی دوباره از قدیمی برنمیگرده
        }
        catch { }
    }

    private static void CopyDir(string from, string to)
    {
        Directory.CreateDirectory(to);
        foreach (string f in Directory.GetFiles(from))
        {
            try
            {
                string dst = Path.Combine(to, Path.GetFileName(f));
                if (!File.Exists(dst)) File.Copy(f, dst);
            }
            catch { }
        }
        foreach (string d in Directory.GetDirectories(from))
        {
            try { CopyDir(d, Path.Combine(to, Path.GetFileName(d))); } catch { }
        }
    }
}
