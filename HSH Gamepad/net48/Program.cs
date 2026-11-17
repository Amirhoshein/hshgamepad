
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using GamepadServer;
using Nefarius.ViGEm.Client;

internal static class Program
{
    private const int PreferredPort = 80;
    private static int Port = PreferredPort;
    private static WebServer? _server;
    private static Hub? _hub;
    private static string _html = "";

    private const string ShowEventName = @"Local\GamepadServer.Show";
    private const string ExitEventName = @"Local\GamepadServer.Exit";

    private static System.Security.Principal.SecurityIdentifier CurrentUserSid()
    {
        using var id = System.Security.Principal.WindowsIdentity.GetCurrent();
        return id.User!;
    }

    private static bool _mutexDenied;
    private static Mutex? _single;

    private static Mutex CreateSingleMutex(out bool created)
    {
        var sec = new System.Security.AccessControl.MutexSecurity();
        sec.AddAccessRule(new System.Security.AccessControl.MutexAccessRule(CurrentUserSid(),
            System.Security.AccessControl.MutexRights.FullControl, System.Security.AccessControl.AccessControlType.Allow));
        try { return new Mutex(true, @"Local\GamepadServer.Single", out created, sec); }
        catch (UnauthorizedAccessException)
        {
            created = false;
            _mutexDenied = true;
            return new Mutex(false);
        }
    }

    private static EventWaitHandle CreateEvent(string name)
    {
        var sec = new System.Security.AccessControl.EventWaitHandleSecurity();
        sec.AddAccessRule(new System.Security.AccessControl.EventWaitHandleAccessRule(CurrentUserSid(),
            System.Security.AccessControl.EventWaitHandleRights.FullControl, System.Security.AccessControl.AccessControlType.Allow));
        return new EventWaitHandle(false, EventResetMode.AutoReset, name, out _, sec);
    }

    [STAThread]
    private static int Main(string[] args)
    {
        try { Console.OutputEncoding = Encoding.UTF8; } catch { }
        try { System.Diagnostics.Process.GetCurrentProcess().PriorityClass = System.Diagnostics.ProcessPriorityClass.High; } catch { }
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        bool exitRequest = args.Contains(JumpListHelper.ExitArg);
        if (exitRequest)
        {
            try { EventWaitHandle.OpenExisting(ExitEventName, System.Security.AccessControl.EventWaitHandleRights.Modify).Set(); } catch { }
            return 0;
        }

        bool restarting = args.Contains(Elevation.RestartArg);
        var single = CreateSingleMutex(out bool firstInstance);
        if (!firstInstance && restarting && !_mutexDenied)
        {
            try { if (single.WaitOne(5000)) firstInstance = true; }
            catch (AbandonedMutexException) { firstInstance = true; }
        }
        if (!firstInstance)
        {
            try { EventWaitHandle.OpenExisting(ShowEventName, System.Security.AccessControl.EventWaitHandleRights.Modify).Set(); } catch { }
            return 0;
        }

        if (!Elevation.IsAdmin())
        {
            try { single.ReleaseMutex(); } catch { }
            single.Dispose();
            if (Elevation.Relaunch(args, true)) return 0;
            single = CreateSingleMutex(out firstInstance);
            if (!firstInstance)
            {
                try { EventWaitHandle.OpenExisting(ShowEventName, System.Security.AccessControl.EventWaitHandleRights.Modify).Set(); } catch { }
                return 0;
            }
        }
        _single = single;
        var showEvent = CreateEvent(ShowEventName);
        var exitEvent = CreateEvent(ExitEventName);

        ViGEmClient client;
        try
        {
            client = new ViGEmClient();
        }
        catch (Exception ex)
        {
            string help = L.T("app.vigemHelp");
            MessageBox.Show(L.F("app.vigem", ex.Message, help),
                "HSH Gamepad", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return 1;
        }

        var settings = AppSettings.Load();
        AccessGuard.Apply(settings);
        var hub = new Hub(client) { AutoKickSeconds = settings.AutoKick ? settings.Seconds : 0, AllowInput = settings.AllowInput };
        string html = LoadHtml();

        _hub = hub; _html = html;
        PortBusy? busy;
        Port = PortGuard.Choose(PreferredPort, out busy);
        while (true)
        {
            try
            {
                var s = new WebServer(hub, html, Port);
                s.Start();
                _server = s;
                break;
            }
            catch (Exception ex)
            {
                int next = PortGuard.NextPort(Port);
                if (next != 0) { Port = next; continue; }
                string owner = PortGuard.OwnerText(Port);
                var msg = L.F("app.listenFail", Port, ex.Message)
                          + (owner.Length > 0 ? "\n\n" + owner : "");
                MessageBox.Show(msg, "HSH Gamepad", MessageBoxButtons.OK, MessageBoxIcon.Error);
                hub.Dispose();
                client.Dispose();
                return 1;
            }
        }

        AccessGuard.SetPort(Port);
        var lan = NetInfo.LanIp();
        string url = lan == null ? "(waiting for network)" : NetInfo.Url(lan, Port) + AccessGuard.PagePath;
        Console.WriteLine();
        Console.WriteLine($"  Gamepad ready ->  {url}");
        Console.WriteLine();

        var form = new MainForm(hub, Port, settings);

        if (busy != null && !busy.Declined && Port != PreferredPort)
        {
            int usedPort = Port;
            var b = busy;
            form.StartHidden();
            bool helpShown = false;
            form.HandleCreated += (_, _) =>
            {
                if (helpShown) return;
                helpShown = true;
                form.BeginInvoke(new Action(() => PortGuard.ShowBusyHelp(null, b, usedPort, SwitchPort,
                    newPort =>
                    {
                        form.SetPort(newPort);
                        form.ShowFromTray();
                    },
                    () => form.ShowFromTray())));
            };
        }
        var showThread = new Thread(() =>
        {
            var waits = new WaitHandle[] { showEvent, exitEvent };
            while (true)
            {
                int which = WaitHandle.WaitAny(waits);
                try { form.BeginInvoke(which == 0 ? new Action(form.ShowFromTray) : new Action(form.ExitApp)); } catch { }
            }
        }) { IsBackground = true };
        showThread.Start();
        JumpListHelper.Register();
        try
        {
            Console.CancelKeyPress += (_, e) =>
            {
                e.Cancel = true;
                try { form.BeginInvoke(new Action(form.ExitApp)); } catch { }
            };
        }
        catch { }

        Application.Run(form);

        try { _server?.Stop(); } catch { }
        AccessGuard.Shutdown();
        hub.Dispose();
        client.Dispose();
        return 0;
    }

    private static bool SwitchPort(int newPort)
    {
        var old = _server;
        if (old == null || _hub == null) return false;
        if (newPort == Port) return true;
        WebServer next;
        try { next = new WebServer(_hub, _html, newPort); next.Start(); }
        catch { return false; }
        _server = next;
        Port = newPort;
        AccessGuard.SetPort(newPort);
        try { old.Stop(); } catch { }
        return true;
    }

    private static string LoadHtml()
    {
        var asm = Assembly.GetExecutingAssembly();
        var name = asm.GetManifestResourceNames().First(n => n.EndsWith("index.html", StringComparison.OrdinalIgnoreCase));
        using (var s = asm.GetManifestResourceStream(name)!)
        using (var r = new StreamReader(s, Encoding.UTF8))
            return r.ReadToEnd();
    }
}
