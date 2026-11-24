// HSH Gamepad | Telegram: @HSHGamepad
using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace GamepadServer;

/// <summary>
/// فایروال ویندوز قانون «اجازه‌ی ورودی» رو برای هر نوع شبکه (Domain / Private / Public) جدا نگه میداره.
/// وقتی کاربر شبکه رو عوض کنه (مثلاً وای‌فای دیگه‌ای که Public حساب میشه) IP جدید درست نشون داده میشه ولی گوشی بهش وصل نمیشه.
/// این کلاس چک میکنه برای شبکه‌ی فعلی قانونی هست یا نه، و (با اجازه‌ی کاربر) یه قانون برای همه‌ی شبکه‌ها اضافه میکنه.
/// </summary>
internal static class FirewallGuard
{
    private const string RuleName = "HSH Gamepad";

    private static object? Get(object o, string name, params object[] args)
    {
        return o.GetType().InvokeMember(name, BindingFlags.GetProperty, null, o, args);
    }

    /// <summary>true = فایروال برای شبکه‌ی فعلی روشنه و هیچ قانون «اجازه‌ی ورودی»ای برای این برنامه / این پورت نیست</summary>
    public static bool NeedsRule(int port, out string key)
    {
        key = "";
        try
        {
            Type? t = Type.GetTypeFromProgID("HNetCfg.FwPolicy2");
            if (t == null) return false;
            object? pol = Activator.CreateInstance(t);
            if (pol == null) return false;

            int cur = Convert.ToInt32(Get(pol, "CurrentProfileTypes"));
            key = cur.ToString();

            bool enabled = false;
            foreach (int bit in new[] { 1, 2, 4 })
                if ((cur & bit) != 0 && Convert.ToBoolean(Get(pol, "FirewallEnabled", bit))) enabled = true;
            if (!enabled) return false;

            string exe = Path.GetFullPath(Application.ExecutablePath);
            var rules = Get(pol, "Rules") as IEnumerable;
            if (rules == null) return false;

            foreach (object r in rules)
            {
                try
                {
                    if (!Convert.ToBoolean(Get(r, "Enabled"))) continue;
                    if (Convert.ToInt32(Get(r, "Direction")) != 1) continue;     // ورودی
                    if (Convert.ToInt32(Get(r, "Action")) != 1) continue;        // اجازه
                    if ((Convert.ToInt32(Get(r, "Profiles")) & cur) == 0) continue;

                    string app = Convert.ToString(Get(r, "ApplicationName")) ?? "";
                    if (app.Length > 0)
                    {
                        string full = Environment.ExpandEnvironmentVariables(app);
                        if (string.Equals(full, exe, StringComparison.OrdinalIgnoreCase)) return false;
                        continue;       // قانون برنامه‌ی دیگه‌ای
                    }

                    int proto = Convert.ToInt32(Get(r, "Protocol"));
                    if (proto != 6 && proto != 256) continue;                    // TCP یا هر پروتکلی
                    string ports = Convert.ToString(Get(r, "LocalPorts")) ?? "";
                    if (PortListed(ports, port)) return false;
                }
                catch { }
            }
            return true;
        }
        catch
        {
            return false;       // نمیدونیم؛ مزاحم کاربر نشو
        }
    }

    private static bool PortListed(string ports, int port)
    {
        if (ports.Length == 0 || ports == "*") return true;
        foreach (string part in ports.Split(','))
        {
            string p = part.Trim();
            int dash = p.IndexOf('-');
            int a, b;
            if (dash > 0)
            {
                if (int.TryParse(p.Substring(0, dash), out a) && int.TryParse(p.Substring(dash + 1), out b) && port >= a && port <= b) return true;
            }
            else if (int.TryParse(p, out a) && a == port) return true;
        }
        return false;
    }

    /// <summary>
    /// قانون‌های قدیمیِ همین برنامه (مثلاً «Block» که ویندوز وقتی کاربر Cancel میزنه میسازه) رو پاک میکنه و یه قانون
    /// «اجازه‌ی ورودی» برای همه‌ی شبکه‌ها اضافه میکنه. ویندوز خودش دسترسی ادمین (UAC) میخواد.
    /// خروجی: 1 = انجام شد، 0 = کاربر UAC رو رد کرد، -1 = شکست
    /// </summary>
    public static int AddRule()
    {
        try
        {
            string exe = Path.GetFullPath(Application.ExecutablePath);
            string cmd =
                "netsh advfirewall firewall delete rule name=all program=\"" + exe + "\" & " +
                "netsh advfirewall firewall add rule name=\"" + RuleName + "\" dir=in action=allow program=\"" + exe + "\" enable=yes profile=any";
            var psi = new ProcessStartInfo("cmd.exe", "/c " + cmd)
            {
                UseShellExecute = true,
                Verb = "runas",
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true,
            };
            using (var p = Process.Start(psi))
            {
                if (p == null) return -1;
                p.WaitForExit(30000);
                return p.HasExited && p.ExitCode == 0 ? 1 : -1;
            }
        }
        catch (System.ComponentModel.Win32Exception ex) when (ex.NativeErrorCode == 1223)
        {
            return 0;
        }
        catch
        {
            return -1;
        }
    }
}
