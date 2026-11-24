/*
 By HSH Gamepad (Telegram: @HSHGamepad)

 Phone -> Virtual Xbox 360 / DualShock 4 controller (تا ۹ پلیر)      [نسخه‌ی .NET Framework 4.8]

 یه پنجره باز میشه که آدرس و وضعیت پلیرها رو نشون میده؛ آدرس رو تو مرورگر گوشی باز کن.
 پیش‌نیاز: درایور ViGEmBus  ->  https://github.com/nefarius/ViGEmBus/releases

 هر شماره‌ی پلیر فقط یه گوشی میتونه داشته باشه.
 محدودیت ویندوز: XInput (Xbox 360) فقط ۴ اسلات داره => پلیر ۵ تا ۹ باید PS4 باشن.
 اگه Xbox ساخته نشه سرور خودش PS4 میسازه.
*/
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
    private const int PreferredPort = 80;      // پورت ۸۰ => تو مرورگر گوشی لازم نیست پورت نوشته بشه
    private static int Port = PreferredPort;      // پورت واقعی؛ اگه ۸۰ پر بود PortGuard یکی دیگه (1111, 2222, ...) انتخاب میکنه
    private static WebServer? _server;
    private static Hub? _hub;
    private static string _html = "";

    private const string ShowEventName = @"Local\GamepadServer.Show";
    private const string ExitEventName = @"Local\GamepadServer.Exit";    // «خروج از برنامه» تو منوی راست‌کلیک taskbar

    // mutex و event ها با دسترسی برای کاربر فعلی ساخته میشن؛ وگرنه نسخه‌ی ادمین فقط به ادمین‌ها اجازه میده و
    // «خروج از برنامه» / اجرای دوباره‌ی بدون ادمین بهشون نمیرسه (Access denied).
    private static System.Security.Principal.SecurityIdentifier CurrentUserSid()
    {
        using var id = System.Security.Principal.WindowsIdentity.GetCurrent();
        return id.User!;
    }

    private static bool _mutexDenied;
    private static Mutex? _single;     // تا آخر برنامه نگه داشته میشه (GC نباید آزادش کنه)

    private static Mutex CreateSingleMutex(out bool created)
    {
        var sec = new System.Security.AccessControl.MutexSecurity();
        sec.AddAccessRule(new System.Security.AccessControl.MutexAccessRule(CurrentUserSid(),
            System.Security.AccessControl.MutexRights.FullControl, System.Security.AccessControl.AccessControlType.Allow));
        try { return new Mutex(true, @"Local\GamepadServer.Single", out created, sec); }
        catch (UnauthorizedAccessException)
        {
            // نسخه‌ای با دسترسی محدودتر ساخته بودش؛ یعنی یه نسخه در حال اجراست
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
        try { Console.OutputEncoding = Encoding.UTF8; } catch { }   // WinExe: کنسول نداریم
        try { System.Diagnostics.Process.GetCurrentProcess().PriorityClass = System.Diagnostics.ProcessPriorityClass.High; } catch { }   // وقتی بازی CPU رو میگیره، سرور عقب نمونه
        // DPI از app.manifest (dpiAware) میاد
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        // «خروج از برنامه» تو منوی راست‌کلیک taskbar: فقط به نسخه‌ی در حال اجرا خبر میده؛ هیچوقت درخواست ادمین نمیکنه
        bool exitRequest = args.Contains(JumpListHelper.ExitArg);
        if (exitRequest)
        {
            try { EventWaitHandle.OpenExisting(ExitEventName, System.Security.AccessControl.EventWaitHandleRights.Modify).Set(); } catch { }
            return 0;
        }

        // فقط یک نسخه: چون بستن پنجره برنامه رو میبره tray، اجرای دوباره باید همون پنجره رو نشون بده
        bool restarting = args.Contains(Elevation.RestartArg);
        var single = CreateSingleMutex(out bool firstInstance);
        if (!firstInstance && restarting && !_mutexDenied)
        {
            // نسخه‌ی قبلی (بدون ادمین) داره بسته میشه؛ چند ثانیه صبر کن تا قفل آزاد بشه
            try { if (single.WaitOne(5000)) firstInstance = true; }
            catch (AbandonedMutexException) { firstInstance = true; }
        }
        if (!firstInstance)
        {
            try { EventWaitHandle.OpenExisting(ShowEventName, System.Security.AccessControl.EventWaitHandleRights.Modify).Set(); } catch { }
            return 0;
        }

        // برنامه باز شد؛ حالا چک میکنیم ادمینه یا نه. نبود => با UAC دوباره اجرا میشه (ادمین لازمه تا ماوس/کیبورد به برنامه‌های ادمین مثل Task Manager برسه)
        if (!Elevation.IsAdmin())
        {
            try { single.ReleaseMutex(); } catch { }
            single.Dispose();
            if (Elevation.Relaunch(args, true)) return 0;
            // کاربر UAC رو رد کرد: برنامه بدون ادمین ادامه میده (فقط ورودی به برنامه‌های ادمین کار نمیکنه)
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

        // ---------- ViGEm ----------
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

        // ---------- وب‌سرور (پشت صحنه) ----------
        // اگه پورت ۸۰ پر باشه از کاربر اجازه‌ی بستن برنامه‌ی پورت‌گیر رو میپرسه؛ اگه نشد با 1111 (یا بعدی‌ها) ادامه میدیم
        // و busy اطلاعات برنامه‌ی پورت‌گیر رو برای پنجره‌ی راهنما نگه میداره
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
                int next = PortGuard.NextPort(Port);     // همون لحظه یکی پورت رو گرفته؛ پورت بعدی (1111, 2222, ...)
                if (next != 0) { Port = next; continue; }
                string owner = PortGuard.OwnerText(Port);      // اسم و آدرس برنامه‌ی پورت‌گیر (اگه پیدا شد)
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

        // پورت ۸۰ آزاد نشد: برنامه‌ی اصلی با 1111 باز میشه و کنارش پنجره‌ی راهنما (برنامه‌ی پورت‌گیر + آموزش + «بررسی دوباره»)
        if (busy != null && !busy.Declined && Port != PreferredPort)
        {
            int usedPort = Port;
            var b = busy;
            form.StartHidden();     // برنامه‌ی اصلی فقط با «باز کردن برنامه» یا آزاد شدن پورت ۸۰ پیدا میشه
            bool helpShown = false;
            form.HandleCreated += (_, _) =>
            {
                if (helpShown) return;
                helpShown = true;
                form.BeginInvoke(new Action(() => PortGuard.ShowBusyHelp(null, b, usedPort, SwitchPort,
                    newPort =>
                    {
                        form.SetPort(newPort);      // آدرس بالای پنجره‌ی اصلی بدون پورت میشه
                        form.ShowFromTray();        // پورت ۸۰ آزاد شد => برنامه خودکار باز میشه
                    },
                    () => form.ShowFromTray())));   // دکمه‌ی «باز کردن برنامه» (یا بستن پنجره‌ی راهنما)
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
        JumpListHelper.Register();      // راست‌کلیک روی آیکون برنامه تو taskbar: «خروج از برنامه»
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

        // ---------- خاموش کردن ----------
        try { _server?.Stop(); } catch { }
        AccessGuard.Shutdown();
        hub.Dispose();      // همه‌ی دسته‌های مجازی از ویندوز برداشته میشن
        client.Dispose();
        return 0;
    }

    /// <summary>
    /// سرور رو (بدون بستن برنامه) روی پورت دیگه‌ای میاره: اول سرور جدید بالا میاد، بعد قدیمی بسته میشه.
    /// از thread دیگه صدا زده میشه (دکمه‌ی «بررسی دوباره» تو PortGuard).
    /// </summary>
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
