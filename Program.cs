/*
 By HSH Gamepad (Telegram: @HSHGamepad)

 Phone -> Virtual Xbox 360 / DualShock 4 controller (تا ۹ پلیر)

 اجرا:   dotnet run -c Release
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
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using GamepadServer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nefarius.ViGEm.Client;

internal static class Program
{
    private const int PreferredPort = 80;    // پورت ۸۰ => تو مرورگر گوشی لازم نیست پورت نوشته بشه
    private static int Port = PreferredPort;   // پورت واقعی؛ اگه ۸۰ پر بود PortGuard یکی دیگه (1111, 2222, ...) انتخاب میکنه

    // برای عوض کردن پورت سرور وسط کار (وقتی کاربر پورت ۸۰ رو آزاد کرد)
    private static WebApplication? _app;
    private static Hub? _hub;
    private static string _html = "";
    private static string[] _args = Array.Empty<string>();

    private const string ShowEventName = @"Local\GamepadServer.Show";
    private const string ExitEventName = @"Local\GamepadServer.Exit";    // «خروج از برنامه» تو منوی راست‌کلیک taskbar

    [STAThread]
    private static int Main(string[] args)
    {
        try { Console.OutputEncoding = Encoding.UTF8; } catch { }   // WinExe: کنسول نداریم
        try { System.Diagnostics.Process.GetCurrentProcess().PriorityClass = System.Diagnostics.ProcessPriorityClass.High; } catch { }   // وقتی بازی CPU رو میگیره، سرور عقب نمونه
        Application.SetHighDpiMode(HighDpiMode.SystemAware);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        // فقط یک نسخه: چون بستن پنجره برنامه رو میبره تو tray، اجرای دوباره باید همون پنجره رو نشون بده
        using var single = new Mutex(true, @"Local\GamepadServer.Single", out bool firstInstance);
        bool exitRequest = args.Contains(JumpListHelper.ExitArg);     // از منوی راست‌کلیک taskbar: «خروج از برنامه»
        if (!firstInstance)
        {
            try { EventWaitHandle.OpenExisting(exitRequest ? ExitEventName : ShowEventName).Set(); } catch { }
            return 0;
        }
        if (exitRequest) return 0;      // برنامه‌ای در حال اجرا نیست که ببندیمش
        var showEvent = new EventWaitHandle(false, EventResetMode.AutoReset, ShowEventName);
        var exitEvent = new EventWaitHandle(false, EventResetMode.AutoReset, ExitEventName);

        // ---------- ViGEm ----------
        ViGEmClient client;
        try
        {
            client = new ViGEmClient();
        }
        catch (Exception ex)
        {
            const string help = "Install it from: https://github.com/nefarius/ViGEmBus/releases";
            Console.WriteLine("ViGEmBus driver not found or failed to start: " + ex.Message);
            Console.WriteLine(help);
            MessageBox.Show("ViGEmBus driver not found or failed to start:\n" + ex.Message + "\n\n" + help,
                "HSH Gamepad", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return 1;
        }

        var settings = AppSettings.Load();
        var hub = new Hub(client) { AutoKickSeconds = settings.AutoKick ? settings.Seconds : 0, AllowInput = settings.AllowInput };
        string html = LoadHtml();
        _args = args;
        _hub = hub;
        _html = html;

        // ---------- وب‌سرور (پشت صحنه) ----------
        // اگه پورت ۸۰ پر باشه از کاربر اجازه‌ی بستن برنامه‌ی پورت‌گیر رو میپرسه؛ اگه نشد با 1111 (یا بعدی‌ها) ادامه میدیم
        // و busy اطلاعات برنامه‌ی پورت‌گیر رو برای پنجره‌ی راهنما نگه میداره
        PortBusy? busy;
        Port = PortGuard.Choose(PreferredPort, out busy);
        WebApplication app;
        while (true)
        {
            app = BuildApp(args, hub, html);
            try
            {
                app.StartAsync().GetAwaiter().GetResult();
                break;
            }
            catch (Exception ex)
            {
                try { app.DisposeAsync().AsTask().GetAwaiter().GetResult(); } catch { }
                int next = PortGuard.NextPort(Port);     // همون لحظه یکی پورت رو گرفته؛ پورت بعدی (1111, 2222, ...)
                if (next != 0) { Port = next; continue; }
                string owner = PortGuard.OwnerText(Port);      // اسم و آدرس برنامه‌ی پورت‌گیر (اگه پیدا شد)
                var msg = $"Could not listen on port {Port}:\n{ex.Message}\n\nProbably another program is using it (IIS, Skype, XAMPP/Apache, the old python server, or another copy of this app)."
                          + (owner.Length > 0 ? "\n\n" + owner : "");
                Console.WriteLine(msg);
                MessageBox.Show(msg, "HSH Gamepad", MessageBoxButtons.OK, MessageBoxIcon.Error);
                hub.Dispose();
                client.Dispose();
                return 1;
            }
        }

        _app = app;

        var lan = NetInfo.LanIp();
        string url = lan == null ? "(waiting for network)" : NetInfo.Url(lan, Port);
        Console.WriteLine();
        Console.WriteLine($"  Gamepad ready ->  {url}");
        Console.WriteLine("  (close the window or Ctrl+C to stop)");
        Console.WriteLine();

        var form = new MainForm(hub, Port, settings);

        // پورت ۸۰ آزاد نشد: برنامه‌ی اصلی با 1111 باز میشه و کنارش پنجره‌ی راهنما (برنامه‌ی پورت‌گیر + آموزش + «بررسی دوباره»)
        if (busy != null && !busy.Declined && Port != PreferredPort)
        {
            int usedPort = Port;
            var b = busy;
            // برنامه‌ی اصلی پشت صحنه باز نمیشه؛ فقط تو دو حالت پیدا میشه: دکمه‌ی «باز کردن برنامه» یا آزاد شدن پورت ۸۰
            form.StartHidden();
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
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            try { form.BeginInvoke(new Action(form.ExitApp)); } catch { }
        };

        Application.Run(form);

        // ---------- خاموش کردن ----------
        try { _app!.StopAsync(TimeSpan.FromSeconds(3)).GetAwaiter().GetResult(); } catch { }
        hub.Dispose();      // همه‌ی دسته‌های مجازی از ویندوز برداشته میشن
        client.Dispose();
        return 0;
    }

    /// <summary>
    /// سرور رو (بدون بستن برنامه) روی پورت دیگه‌ای میاره: اول سرور جدید بالا میاد، بعد قدیمی بسته میشه.
    /// از thread دیگه صدا زده میشه (دکمه‌ی «بررسی دوباره» تو PortGuard)؛ گوشی‌هایی که با پورت قبلی وصل بودن قطع میشن.
    /// </summary>
    private static bool SwitchPort(int newPort)
    {
        var old = _app;
        if (old == null || _hub == null) return false;
        int oldPort = Port;
        if (newPort == oldPort) return true;

        Port = newPort;                      // BuildApp از همین مقدار استفاده میکنه
        var next = BuildApp(_args, _hub, _html);
        try
        {
            next.StartAsync().GetAwaiter().GetResult();
        }
        catch
        {
            Port = oldPort;
            try { next.DisposeAsync().AsTask().GetAwaiter().GetResult(); } catch { }
            return false;
        }

        _app = next;
        try { old.StopAsync(TimeSpan.FromSeconds(3)).GetAwaiter().GetResult(); } catch { }
        try { old.DisposeAsync().AsTask().GetAwaiter().GetResult(); } catch { }
        return true;
    }

    private static string LoadHtml()
    {
        var asm = Assembly.GetExecutingAssembly();
        var name = asm.GetManifestResourceNames().First(n => n.EndsWith("index.html", StringComparison.OrdinalIgnoreCase));
        using var s = asm.GetManifestResourceStream(name)!;
        using var r = new StreamReader(s, Encoding.UTF8);
        return r.ReadToEnd();
    }

    private static WebApplication BuildApp(string[] args, Hub hub, string html)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.WebHost.ConfigureKestrel(o => o.ListenAnyIP(Port));   // NoDelay به‌صورت پیش‌فرض روشنه
        builder.Logging.SetMinimumLevel(LogLevel.Warning);

        var app = builder.Build();
        app.UseWebSockets(new WebSocketOptions { KeepAliveInterval = TimeSpan.FromSeconds(20) });

        app.MapGet("/", (HttpContext c) =>
        {
            c.Response.Headers.CacheControl = "no-store";
            if (IsHost(c))
            {
                c.Response.StatusCode = 403;     // .NET 6: Results.Content آرگومان status نداره
                return Results.Content(HostBlockedPage, "text/html; charset=utf-8", Encoding.UTF8);
            }
            return Results.Content(html, "text/html; charset=utf-8");
        });
        app.MapGet("/favicon.ico", () => Results.NoContent());

        app.MapGet("/ws", async (HttpContext ctx) =>
        {
            if (!ctx.WebSockets.IsWebSocketRequest)
            {
                ctx.Response.StatusCode = 400;
                return;
            }

            // خود کامپیوتر سرور (هاست) اجازه‌ی وصل شدن به دسته رو نداره
            if (IsHost(ctx))
            {
                ctx.Response.StatusCode = 403;
                return;
            }

            using var ws = await ctx.WebSockets.AcceptWebSocketAsync();

            var ip = ctx.Connection.RemoteIpAddress;
            string ipText = ip == null ? "?" : (ip.IsIPv4MappedToIPv6 ? ip.MapToIPv4() : ip).ToString();
            var conn = new Conn(
                ws,
                ctx.Request.Query["id"].ToString(),
                ctx.Request.Query["name"].ToString(),
                DeviceInfo.FromUserAgent(ctx.Request.Headers.UserAgent.ToString()),
                ipText);
            hub.Add(conn);

            var buf = new byte[4096];
            var ms = new MemoryStream();
            try
            {
                while (ws.State == WebSocketState.Open)
                {
                    ms.SetLength(0);
                    ValueWebSocketReceiveResult r;
                    do
                    {
                        r = await ws.ReceiveAsync(buf.AsMemory(), ctx.RequestAborted);
                        if (r.MessageType == WebSocketMessageType.Close) break;
                        ms.Write(buf, 0, r.Count);
                    } while (!r.EndOfMessage);

                    if (r.MessageType == WebSocketMessageType.Close) break;
                    if (r.MessageType != WebSocketMessageType.Text) continue;

                    string text = Encoding.UTF8.GetString(ms.GetBuffer(), 0, (int)ms.Length);
                    foreach (var line in text.Split('\n'))          // چند دستور میتونه تو یک پیام بیاد
                        Protocol.Handle(conn, hub, line);

                    conn.Slot?.Submit();
                }
            }
            catch (OperationCanceledException) { }
            catch (WebSocketException) { }
            finally
            {
                hub.Remove(conn);   // قطع شدن اتصال => همه‌ی دکمه‌های اون پلیر ول میشه و پلیر آزاد میشه
                try
                {
                    if (ws.State == WebSocketState.CloseReceived || ws.State == WebSocketState.Open)
                        await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "bye", CancellationToken.None);
                }
                catch { }
            }
        });

        return app;
    }

    private const string HostBlockedPage =
        "<!DOCTYPE html><html lang=\"fa\" dir=\"rtl\"><head><meta charset=\"utf-8\">" +
        "<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\"><title>Gamepad</title>" +
        "<style>html,body{height:100%;margin:0;background:#12151d;color:#e6e9f2;font-family:system-ui,Tahoma,sans-serif}" +
        "body{display:flex;align-items:center;justify-content:center;text-align:center;padding:24px}" +
        "p{color:#8d97b3;margin:8px 0 0}</style></head><body><div><h2>این صفحه فقط برای گوشیه</h2>" +
        "<p>از خود کامپیوتر سرور نمیشه به دسته وصل شد؛ آدرس رو تو مرورگر گوشی باز کن.</p></div></body></html>";

    /// <summary>درخواست از خود همین کامپیوتره؟ (localhost یا یکی از IPهای خود سیستم)</summary>
    private static bool IsHost(HttpContext c)
    {
        try
        {
            var ip = c.Connection.RemoteIpAddress;
            if (ip == null) return false;
            if (ip.IsIPv4MappedToIPv6) ip = ip.MapToIPv4();
            if (IPAddress.IsLoopback(ip)) return true;

            var local = c.Connection.LocalIpAddress;
            if (local != null)
            {
                if (local.IsIPv4MappedToIPv6) local = local.MapToIPv4();
                if (ip.Equals(local)) return true;      // مثلاً خود سیستم با IP شبکه‌اش (192.168.x.x) باز کرده
            }

            foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                foreach (var ua in nic.GetIPProperties().UnicastAddresses)
                {
                    var a = ua.Address;
                    if (a.IsIPv4MappedToIPv6) a = a.MapToIPv4();
                    if (a.Equals(ip)) return true;
                }
            }
        }
        catch { }
        return false;
    }
}
