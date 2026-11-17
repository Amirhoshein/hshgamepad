using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Windows.Forms;

namespace GamepadServer;

public static class Elevation
{
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
        catch (Win32Exception) { return false; }
        catch { return false; }
    }
}
