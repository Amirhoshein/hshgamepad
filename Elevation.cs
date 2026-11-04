// HSH Gamepad | Telegram: @HSHGamepad
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Windows.Forms;

namespace GamepadServer;

/// <summary>
/// اجرا با دسترسی ادمین. ویندوز (UIPI) اجازه نمیده برنامه‌ی معمولی به پنجره‌ی برنامه‌ی ادمین (مثل Task Manager) ماوس/کیبورد بفرسته؛
/// پس اگه تو تنظیمات «اجرا با دسترسی ادمین» روشن باشه، برنامه موقع اجرا خودش رو با UAC دوباره باز میکنه.
/// </summary>
public static class Elevation
{
    /// <summary>به نسخه‌ی تازه میگه نسخه‌ی قبلی داره بسته میشه؛ چند ثانیه برای آزاد شدن قفل «فقط یک نسخه» صبر کن</summary>
    public const string RestartArg = "--restart";

    public static bool IsAdmin()
    {
        try
        {
            using var id = WindowsIdentity.GetCurrent();
            return new WindowsPrincipal(id).IsInRole(WindowsBuiltInRole.Administrator);
        }
        catch { return false; }
    }

    private static string Quote(string a) => "\"" + a.Replace("\"", "\\\"") + "\"";

    /// <summary>خودش رو با «Run as administrator» دوباره اجرا میکنه. اگه کاربر UAC رو رد کنه false برمیگردونه.</summary>
    public static bool Relaunch(string[] args, bool restart)
    {
        try
        {
            string exe = Application.ExecutablePath;
            var list = args.Where(a => a != RestartArg).Select(Quote).ToList();
            if (restart) list.Add(RestartArg);
            var psi = new ProcessStartInfo(exe)
            {
                UseShellExecute = true,
                Verb = "runas",
                Arguments = string.Join(" ", list),
                WorkingDirectory = Path.GetDirectoryName(exe) ?? "",
            };
            Process.Start(psi);
            return true;
        }
        catch (Win32Exception) { return false; }   // کاربر تو پنجره‌ی UAC «No» زد
        catch { return false; }
    }
}
