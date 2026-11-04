// HSH Gamepad | Telegram: @HSHGamepad
// انتخاب پورت سرور: اول پورت 80؛ اگه یه برنامه‌ی دیگه (XAMPP / WAMP / IIS / ...) گرفته باشدش از کاربر اجازه‌ی بستنش رو میپرسه؛
// اگه کاربر نخواست یا آزاد نشد، خود برنامه 1111 بعد 2222 بعد 3333 ... رو امتحان میکنه و برنامه با همون پورت باز میشه.
// بعدش (از طرف Program) یه پنجره‌ی راهنما کنار برنامه باز میشه: اسم و آدرس برنامه‌ی پورت‌گیر + آموزش بستنش از Task Manager
// + دکمه‌ی «بررسی دوباره»؛ اگه پورت آزاد شده باشه خودکار سرور میره رو پورت 80.
// (این فایل عمداً ساده نوشته شده تا بدون تغییر تو نسخه‌ی net48 هم کپی بشه)
#nullable disable
using System;
using System.Diagnostics;
using System.Drawing;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GamepadServer
{
    internal enum BusyKind { Process, HttpSys, Unknown }

    /// <summary>اطلاعات برنامه‌ای که پورت رو گرفته</summary>
    internal sealed class PortBusy
    {
        public int Port;
        public BusyKind Kind;
        public int Pid;
        public string Name;         // null = اسمش پیدا نشد
        public string Path;         // null = آدرس فایلش پیدا نشد
        public bool IisRunning;     // فقط برای http.sys
        public bool Declined;       // کاربر تو پنجره‌ی اجازه «خیر» زد
    }

    internal static class PortGuard
    {
        private const int FallbackStep = 1111;     // 1111, 2222, 3333, ... 9999
        private const int FreeTimeoutMs = 3000;    // آزاد کردن پورت بیشتر از ۵ ثانیه طول نمیکشه

        /// <summary>روش قدیمی (سازگار با نسخه‌ی net48): پورت رو انتخاب میکنه و اگه ۸۰ آزاد نشد یه پیام (مودال) نشون میده</summary>
        public static int Choose(int preferred)
        {
            PortBusy busy;
            int p = Choose(preferred, out busy);
            if (busy != null && !busy.Declined && p != preferred)
                Info("پورت " + preferred + " آزاد نشد",
                     "برای همین از پورت " + p + " استفاده میشود." + Environment.NewLine + Environment.NewLine +
                     "آدرس بالای پنجره‌ی برنامه حالا آخرش «:" + p + "» داره؛ همون رو تو مرورگر گوشی باز کن.");
            return p;
        }

        /// <summary>
        /// پورتی که سرور باید باهاش شروع کنه (اگه همه‌ی پورت‌ها پر بودن همون preferred برمیگرده تا خطای معمولی نمایش داده بشه).
        /// busy = null یعنی preferred آزاد بود؛ وگرنه اطلاعات برنامه‌ی پورت‌گیر (برای پنجره‌ی راهنما) توشه.
        /// </summary>
        public static int Choose(int preferred, out PortBusy busy)
        {
            busy = null;
            if (IsFree(preferred)) return preferred;

            bool declined;
            TryFreeWithPermission(preferred, out declined);
            if (IsFree(preferred)) return preferred;

            busy = Describe(preferred);         // بعد از تلاش دوباره میپرسیم؛ شاید پورت‌گیر عوض شده باشه
            busy.Declined = declined;

            for (int p = FallbackStep; p <= 9999; p += FallbackStep)
            {
                if (IsFree(p)) return p;
            }
            return preferred;
        }

        /// <summary>اگه شروع سرور با یه پورت شکست خورد (مثلاً همون لحظه یکی گرفتش) پورت بعدی؛ 0 = دیگه پورتی نمونده</summary>
        public static int NextPort(int current)
        {
            int n = current < FallbackStep ? FallbackStep : current + FallbackStep;
            return n <= 9999 ? n : 0;
        }

        public static bool IsFree(int port)
        {
            try
            {
                var l = new TcpListener(IPAddress.Any, port);
                l.ExclusiveAddressUse = true;
                l.Start();
                l.Stop();
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>یه خط متن انگلیسی درباره‌ی برنامه‌ی پورت‌گیر (برای پیام خطای آخر)؛ خالی اگه چیزی پیدا نشد</summary>
        public static string OwnerText(int port)
        {
            try
            {
                PortBusy b = Describe(port);
                if (b.Kind == BusyKind.Process)
                    return "Port owner: " + (b.Name ?? "unknown program") + " (PID " + b.Pid + ")" +
                           (b.Path != null ? Environment.NewLine + "Path: " + b.Path : "");
                if (b.Kind == BusyKind.HttpSys)
                    return "Port owner: a Windows system service (http.sys - usually IIS)";
            }
            catch { }
            return "";
        }

        /// <summary>
        /// پنجره‌ی راهنمای «پورت آزاد نشد» رو کنار برنامه باز میکنه (مودال نیست، برنامه‌ی اصلی کار میکنه).
        /// switchTo(port): سرور رو روی اون پورت میاره (تو thread دیگه صدا زده میشه) و true برمیگردونه اگه شد.
        /// onSwitched(port): بعد از موفقیت تو thread رابط کاربری صدا زده میشه (آپدیت آدرس + نمایش پنجره‌ی اصلی).
        /// onOpen(): کاربر دکمه‌ی «باز کردن برنامه» رو زد (یا پنجره‌ی راهنما رو بست) و پورت ۸۰ آزاد نشده بود.
        /// </summary>
        public static void ShowBusyHelp(IWin32Window owner, PortBusy busy, int usedPort, Func<int, bool> switchTo, Action<int> onSwitched, Action onOpen)
        {
            var f = new BusyForm(owner, busy, usedPort, switchTo, onSwitched, onOpen);
            f.FormClosed += (s, e) => f.Dispose();
            if (owner != null) f.Show(owner);
            else f.Show();
        }

        // ------------------------------------------------------------------

        /// <summary>از کاربر اجازه میگیره و برنامه‌ی پورت‌گیر رو میبنده. declined = کاربر صراحتاً «خیر» زد</summary>
        private static bool TryFreeWithPermission(int port, out bool declined)
        {
            declined = false;
            PortBusy b = Describe(port);

            if (b.Kind == BusyKind.HttpSys)                 // System = http.sys (IIS و چند سرویس دیگه)
                return b.IisRunning && FreeIis(port, out declined);

            if (b.Kind == BusyKind.Unknown)                 // نمیشه فهمید کیه؛ راهنمای دستی تو پنجره‌ی راهنما نشون داده میشه
                return false;

            bool yes = Ask("پورت " + port + " رزرو شده",
                           "این برنامه پورت " + port + " رو رزرو کرده." + Environment.NewLine +
                           "برنامه به این پورت نیاز داره تا گوشی بدون نیاز به وارد کردن پورت داخل مرورگر متصل بشه. اجازه میدی برنامه زیر رو ببندیم؟",
                           (b.Name ?? "Unknown") + "   (PID " + b.Pid + ")", b.Path,
                           "اگه دکمه «خیر» رو بزنی، از پورت " + FallbackStep + " استفاده میشه.",
                           "اره ببندش");
            if (!yes)
            {
                declined = true;
                return false;
            }

            var sw = Stopwatch.StartNew();
            CloseProcess(b.Pid, sw);
            return WaitFree(port, sw);
        }

        private static bool FreeIis(int port, out bool declined)
        {
            declined = false;
            bool yes = Ask("پورت " + port + " رزرو شده",
                           "IIS این پورت رو رزرو کرده." + Environment.NewLine +
                           "برنامه به این پورت نیاز داره تا گوشی بدون نیاز به وارد کردن پورت داخل مرورگر متصل بشه. اجازه میدی این برنامه چرتی که فقط مال ویندوز 11 هست رو ببندیم؟",
                           "IIS  (World Wide Web Publishing)", null,
                           "اگه دکمه «خیر» رو بزنی، از پورت " + FallbackStep + " استفاده میشه.",
                           "اره ببندش");
            if (!yes)
            {
                declined = true;
                return false;
            }
            var sw = Stopwatch.StartNew();
            Run("net.exe", "stop W3SVC /y", Left(sw));     // نیاز به دسترسی ادمین داره؛ اگه نشد، میریم سراغ پورت بعدی
            return WaitFree(port, sw);
        }

        private static bool IisRunning()
        {
            string o;
            int rc = Run("sc.exe", "query W3SVC", 4000, out o);
            // STATE : 4 RUNNING  (عدد 4 به زبان ویندوز بستگی نداره)
            return rc == 0 && o != null && Regex.IsMatch(o, @"(?m)^\s*\w+\s*:\s*4\s+\w+");
        }

        private static int Left(Stopwatch sw)
        {
            return (int)Math.Max(500, FreeTimeoutMs - sw.ElapsedMilliseconds);
        }

        private static void CloseProcess(int pid, Stopwatch sw)
        {
            try
            {
                using (var p = Process.GetProcessById(pid))
                {
                    // اول مؤدبانه (برنامه‌هایی که پنجره دارن)، بعدش اجباری با همه‌ی زیرپردازش‌هاش (Apache معمولاً چندتا httpd داره)
                    try { if (p.MainWindowHandle != IntPtr.Zero) { p.CloseMainWindow(); p.WaitForExit(1000); } } catch { }
                }
            }
            catch { }
            Run("taskkill.exe", "/PID " + pid + " /T /F", Left(sw));
        }

        private static bool WaitFree(int port, Stopwatch sw)
        {
            while (sw.ElapsedMilliseconds < FreeTimeoutMs)
            {
                if (IsFree(port)) return true;
                Thread.Sleep(250);
            }
            return IsFree(port);
        }

        private static int Run(string file, string args, int timeoutMs)
        {
            string o;
            return Run(file, args, timeoutMs, out o);
        }

        private static int Run(string file, string args, int timeoutMs, out string output)
        {
            output = null;
            try
            {
                var psi = new ProcessStartInfo(file, args)
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                using (var p = Process.Start(psi))
                {
                    if (!p.WaitForExit(timeoutMs)) { try { p.Kill(); } catch { } return -1; }
                    output = p.StandardOutput.ReadToEnd();
                    return p.ExitCode;
                }
            }
            catch
            {
                return -1;
            }
        }

        // ---------- پیدا کردن برنامه‌ای که پورت رو گرفته ----------

        /// <summary>اسم + PID + آدرس فایل برنامه‌ی پورت‌گیر</summary>
        private static PortBusy Describe(int port)
        {
            var b = new PortBusy { Port = port };
            int pid = OwnerPid(port);
            b.Pid = pid;

            if (pid == 4)
            {
                b.Kind = BusyKind.HttpSys;
                b.IisRunning = IisRunning();
                return b;
            }
            if (pid <= 0 || pid == Process.GetCurrentProcess().Id)
            {
                b.Kind = BusyKind.Unknown;
                return b;
            }

            b.Kind = BusyKind.Process;
            try
            {
                using (var p = Process.GetProcessById(pid)) b.Name = p.ProcessName;
            }
            catch { }
            b.Path = ImagePath(pid);
            return b;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(uint access, bool inherit, int pid);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr h);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool QueryFullProcessImageName(IntPtr h, int flags, StringBuilder sb, ref int size);

        /// <summary>آدرس کامل فایل exe (با QueryFullProcessImageName؛ برخلاف MainModule برای برنامه‌های ۶۴ بیتی و با دسترسی بالاتر هم کار میکنه)</summary>
        private static string ImagePath(int pid)
        {
            IntPtr h = IntPtr.Zero;
            try
            {
                h = OpenProcess(0x1000, false, pid);        // PROCESS_QUERY_LIMITED_INFORMATION
                if (h != IntPtr.Zero)
                {
                    var sb = new StringBuilder(1024);
                    int size = sb.Capacity;
                    if (QueryFullProcessImageName(h, 0, sb, ref size)) return sb.ToString();
                }
            }
            catch { }
            finally
            {
                if (h != IntPtr.Zero) CloseHandle(h);
            }

            try
            {
                using (var p = Process.GetProcessById(pid)) return p.MainModule.FileName;
            }
            catch { }
            return null;
        }

        [DllImport("iphlpapi.dll", SetLastError = true)]
        private static extern uint GetExtendedTcpTable(IntPtr table, ref int size, bool sort, int af, int tableClass, uint reserved);

        private static int OwnerPid(int port)
        {
            foreach (int af in new[] { 2, 23 })                 // AF_INET, AF_INET6
            {
                int size = 0;
                GetExtendedTcpTable(IntPtr.Zero, ref size, false, af, 3, 0);     // 3 = TCP_TABLE_OWNER_PID_LISTENER
                if (size <= 0) continue;
                IntPtr buf = Marshal.AllocHGlobal(size);
                try
                {
                    if (GetExtendedTcpTable(buf, ref size, false, af, 3, 0) != 0) continue;
                    int n = Marshal.ReadInt32(buf);
                    int rowSize = af == 2 ? 24 : 56, portOff = af == 2 ? 8 : 20, pidOff = af == 2 ? 20 : 52;
                    for (int i = 0; i < n; i++)
                    {
                        IntPtr row = IntPtr.Add(buf, 4 + i * rowSize);
                        int p = Marshal.ReadInt32(row, portOff);
                        int hostPort = ((p & 0xFF) << 8) | ((p >> 8) & 0xFF);       // پورت به ترتیب بایت شبکه ذخیره شده
                        if (hostPort == port) return Marshal.ReadInt32(row, pidOff);
                    }
                }
                catch { }
                finally { Marshal.FreeHGlobal(buf); }
            }
            return -1;
        }

        // ---------- متن راهنما ----------

        private static string GuideText(PortBusy b)
        {
            string nl = Environment.NewLine;

            if (b.Kind == BusyKind.Process)
            {
                string who = b.Name != null ? " (" + b.Name + ".exe)" : "";
                return "۱. کلیدهای Ctrl+Shift+Esc رو با هم بزن تا Task Manager باز بشه." + nl +
                       "۲. اگه ساده باز شد، پایین دکمه «More details» (اگه بود) رو بزن." + nl +
                       "۳. از بالا Details رو باز کن و تو ستون PID دنبال شماره‌ی " + b.Pid + who + " بگرد." + nl +
                       "۴. روش راست‌کلیک کن و End task (یا End process tree) رو بزن." + nl +
                       "۵. برگرد همین‌جا و «بررسی دوباره» رو بزن.";
            }

            if (b.Kind == BusyKind.HttpSys)
            {
                string head = b.IisRunning
                    ? "IIS این پورت رو گرفته." + nl
                    : "این پورت رو یه سرویس سیستمی ویندوز (http.sys) گرفته؛ معمولاً IIS، SQL Server Reporting Services یا برنامه های تحت وب دیگه." + nl;
                return head +
                       "۱. کلیدهای Win+R رو بزن و بنویس services.msc و بعد Enter رو بزن." + nl +
                       "۲. سرویس World Wide Web Publishing Service (یا SQL Server Reporting Services) رو پیدا کن." + nl +
                       "۳. روش راست‌کلیک کن و Stop رو بزن." + nl +
                       "۴. برگرد همین‌جا و «بررسی دوباره» رو بزن.";
            }

            return "اسم برنامه‌ی پورت‌گیر پیدا نشد؛ یا یه برنامه‌ی وب‌سرور باز هست یا ویندوز پورت رو گرفته." + nl +
                   "۱. Ctrl+Shift+Esc رو بزن و برنامه‌هایی مثل XAMPP، Apache، nginx، Skype یا هر وب‌سرور دیگه رو (تو تب Processes و Details) ببند." + nl +
                   "۲. اگه چیزی پیدا نشد، شاید Hyper-V / WSL / Docker پورت رو گرفته باشن: Command Prompt رو با Run as administrator باز کن و بزن net stop winnat." + nl +
                   "۳. ریستارت ویندوز هم معمولاً جواب میده." + nl +
                   "۴. بعدش «بررسی دوباره» رو بزن.";
        }

        // ---------- پنجره‌های پیام هم‌تم با برنامه ----------
        private static bool Ask(string heading, string body, string procName, string procPath, string hint, string yesText)
        {
            using (var f = new DialogForm(heading, body, procName, procPath, hint, yesText, "خیر", false))
                return f.ShowDialog() == DialogResult.OK;
        }

        private static void Info(string heading, string body)
        {
            using (var f = new DialogForm(heading, body, null, null, null, "باشه", null, false))
                f.ShowDialog();
        }

        private static void ShowSuccess(IWin32Window owner, int port)
        {
            string nl = Environment.NewLine;
            using (var f = new DialogForm("✓ پورت " + port + " با موفقیت آزاد شد",
                "تبریک، از استفاده از برنامه لذت ببرید :)",
                null, null, null, "باشه", null, true))
            {
                if (owner != null) f.ShowDialog(owner);
                else f.ShowDialog();
            }
        }

        /// <summary>پایه‌ی همه‌ی پنجره‌های این فایل: تم تیره، DPI، نوار عنوان تیره</summary>
        private abstract class ThemedForm : Form
        {
            protected static readonly Color cBg = Color.FromArgb(18, 21, 29);
            protected static readonly Color cPanel = Color.FromArgb(27, 32, 48);
            protected static readonly Color cCell = Color.FromArgb(24, 28, 41);
            protected static readonly Color cFg = Color.FromArgb(230, 233, 242);
            protected static readonly Color cMuted = Color.FromArgb(141, 151, 179);
            protected static readonly Color cAccent = Color.FromArgb(91, 146, 255);
            protected static readonly Color cGrid = Color.FromArgb(59, 90, 153);
            protected static readonly Color cOk = Color.FromArgb(120, 214, 150);
            protected static readonly Color cWarn = Color.FromArgb(255, 200, 120);

            [DllImport("dwmapi.dll")]
            private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

            private readonly float _k;
            protected int S(int px) { return (int)Math.Round(px * _k); }

            protected ThemedForm()
            {
                _k = DeviceDpi / 96f;
                Text = "HSH Gamepad";                        // عنوان همیشه بالا-چپ (فرم LTR)
                RightToLeft = RightToLeft.No;
                ShowIcon = false;
                ShowInTaskbar = true;
                TopMost = true;
                FormBorderStyle = FormBorderStyle.FixedDialog;
                MaximizeBox = false;
                MinimizeBox = false;
                StartPosition = FormStartPosition.CenterScreen;
                BackColor = cBg;
                ForeColor = cFg;
                Font = new Font("Segoe UI", 10.5f);
            }

            protected int AddLabel(string text, Font font, Color color, bool rtl, int x, int y, int w)
            {
                var flags = TextFormatFlags.WordBreak | TextFormatFlags.NoPadding | TextFormatFlags.TextBoxControl;
                if (rtl) flags |= TextFormatFlags.RightToLeft;
                var sz = TextRenderer.MeasureText(text, font, new Size(w, int.MaxValue), flags);
                int h = sz.Height + S(6);
                Controls.Add(new Label
                {
                    Text = text, Font = font, ForeColor = color, AutoSize = false, UseMnemonic = false,
                    Left = x, Top = y, Width = w, Height = h,
                    RightToLeft = rtl ? RightToLeft.Yes : RightToLeft.No      // با RTL متن راست‌چین میشه
                });
                return y + h;
            }

            protected static Button MakeButton(string text, Color back, Color border, Color fore)
            {
                var b = new Button { Text = text, Font = new Font("Segoe UI Semibold", 10.5f), Cursor = Cursors.Hand, UseVisualStyleBackColor = false };
                b.FlatStyle = FlatStyle.Flat;
                b.FlatAppearance.BorderColor = border;
                b.FlatAppearance.BorderSize = 1;
                b.BackColor = back;
                b.ForeColor = fore;
                return b;
            }

            // نوار عنوان تیره (مثل پنجره‌ی اصلی)
            protected override void OnHandleCreated(EventArgs e)
            {
                base.OnHandleCreated(e);
                try
                {
                    int on = 1;
                    if (DwmSetWindowAttribute(Handle, 20, ref on, 4) != 0) DwmSetWindowAttribute(Handle, 19, ref on, 4);
                    int cap = cBg.R | (cBg.G << 8) | (cBg.B << 16);
                    DwmSetWindowAttribute(Handle, 35, ref cap, 4);
                }
                catch { }
            }
        }

        private sealed class DialogForm : ThemedForm
        {
            public DialogForm(string heading, string body, string procName, string procPath, string hint, string yesText, string noText, bool success)
            {
                int pad = S(24), w = S(440), y = pad;

                y = AddLabel(heading, new Font("Segoe UI Semibold", 14f), success ? cOk : cFg, true, pad, y, w) + S(12);
                y = AddLabel(body, new Font("Segoe UI", 11f), cFg, true, pad, y, w) + S(14);

                if (procName != null)
                {
                    int ch = procPath != null ? S(66) : S(46);
                    var card = new Panel { Left = pad, Top = y, Width = w, Height = ch, BackColor = cPanel };
                    var nm = new Label
                    {
                        Text = procName, ForeColor = cAccent, Font = new Font("Segoe UI Semibold", 12f),
                        AutoSize = false, Left = S(14), Top = S(10), Width = w - S(28), Height = S(26),
                        RightToLeft = RightToLeft.No, UseMnemonic = false
                    };
                    card.Controls.Add(nm);
                    if (procPath != null)
                    {
                        card.Controls.Add(new Label
                        {
                            Text = procPath, ForeColor = cMuted, Font = new Font("Consolas", 9f),
                            AutoSize = false, AutoEllipsis = true, Left = S(14), Top = S(38), Width = w - S(28), Height = S(20),
                            RightToLeft = RightToLeft.No, UseMnemonic = false
                        });
                    }
                    Controls.Add(card);
                    y += ch + S(14);
                }

                if (hint != null)
                    y = AddLabel(hint, new Font("Segoe UI", 10f), cMuted, true, pad, y, w) + S(18);
                else
                    y += S(6);

                // دکمه‌ها سمت راست: «بله» آخر سمت راست، «خیر» کنارش
                int bw = S(128), bh = S(40), gap = S(10);
                var yes = MakeButton(yesText, cAccent, cAccent, cBg);
                yes.Size = new Size(bw, bh);
                yes.Location = new Point(pad + w - bw, y);
                yes.DialogResult = DialogResult.OK;
                Controls.Add(yes);
                AcceptButton = yes;
                if (noText != null)
                {
                    var no = MakeButton(noText, cCell, cGrid, cFg);
                    no.Size = new Size(bw, bh);
                    no.Location = new Point(yes.Left - gap - bw, y);
                    no.DialogResult = DialogResult.Cancel;
                    Controls.Add(no);
                    CancelButton = no;
                }
                else CancelButton = yes;

                ClientSize = new Size(w + 2 * pad, y + bh + pad);
            }
        }

        /// <summary>پنجره‌ی راهنمای «پورت 80 آزاد نشد»: برنامه‌ی پورت‌گیر + آموزش بستنش + دکمه‌ی بررسی دوباره</summary>
        private sealed class BusyForm : ThemedForm
        {
            private readonly IWin32Window _owner;
            private readonly PortBusy _busy;
            private readonly Func<int, bool> _switchTo;
            private readonly Action<int> _onSwitched;
            private readonly Action _onOpen;
            private bool _switched;
            private readonly Button _check;
            private readonly Label _status;
            private readonly string _checkText;

            public BusyForm(IWin32Window owner, PortBusy busy, int usedPort, Func<int, bool> switchTo, Action<int> onSwitched, Action onOpen)
            {
                _owner = owner;
                _onOpen = onOpen;
                _busy = busy;
                _switchTo = switchTo;
                _onSwitched = onSwitched;

                string nl = Environment.NewLine;
                int pad = S(24), w = S(520), y = pad;

                y = AddLabel("پورت " + busy.Port + " آزاد نشد", new Font("Segoe UI Semibold", 14f), cFg, true, pad, y, w) + S(10);
                y = AddLabel("هنوز پورت " + busy.Port + " آزاد نشده. برای باز شدن پورت " + busy.Port + " طبق راهنمای پایین اون برنامه رو ببند و «بررسی دوباره» رو بزن.",
                             new Font("Segoe UI", 10.5f), cFg, true, pad, y, w) + S(12);

                // ---- کارت برنامه‌ی پورت‌گیر: اسم + PID و آدرس فایل (قابل انتخاب و کپی) ----
                string title, path;
                bool realPath = false;
                if (busy.Kind == BusyKind.Process)
                {
                    title = (busy.Name ?? "برنامه‌ی ناشناس") + "   (PID " + busy.Pid + ")";
                    if (busy.Path != null) { path = busy.Path; realPath = true; }
                    else path = "آدرس برنامه پیدا نشد (احتمالاً با دسترسی ادمین اجرا شده)";
                }
                else if (busy.Kind == BusyKind.HttpSys)
                {
                    title = "سرویس سیستمی ویندوز (http.sys)";
                    path = busy.IisRunning ? "IIS  (World Wide Web Publishing Service)" : "معمولاً IIS یا یه سرویس وب دیگه‌ی ویندوز";
                }
                else
                {
                    title = "برنامه‌ی ناشناس";
                    path = "اسم و آدرس برنامه پیدا نشد";
                }

                int ch = S(94);
                var card = new Panel { Left = pad, Top = y, Width = w, Height = ch, BackColor = cPanel };
                card.Controls.Add(new Label
                {
                    Text = title, ForeColor = cAccent, Font = new Font("Segoe UI Semibold", 12f),
                    AutoSize = false, Left = S(14), Top = S(10), Width = w - S(28), Height = S(26),
                    RightToLeft = RightToLeft.No, UseMnemonic = false
                });
                card.Controls.Add(new Label
                {
                    Text = "آدرس برنامه:", ForeColor = cMuted, Font = new Font("Segoe UI", 9f),
                    AutoSize = false, Left = S(14), Top = S(38), Width = w - S(28), Height = S(18),
                    RightToLeft = RightToLeft.Yes, UseMnemonic = false
                });
                card.Controls.Add(new TextBox
                {
                    Text = path, ReadOnly = true, Multiline = true, WordWrap = true, BorderStyle = BorderStyle.None,
                    BackColor = cPanel, ForeColor = realPath ? cFg : cMuted,
                    Font = realPath ? new Font("Consolas", 9f) : new Font("Segoe UI", 9.5f),
                    RightToLeft = realPath ? RightToLeft.No : RightToLeft.Yes,
                    Left = S(14), Top = S(58), Width = w - S(28), Height = S(layoutPathHeight(path, realPath)),
                    TabStop = false, ScrollBars = ScrollBars.None
                });
                Controls.Add(card);
                y += ch + S(14);

                // ---- راهنما ----
                y = AddLabel("چطوری آزادش کنم؟", new Font("Segoe UI Semibold", 11.5f), cAccent, true, pad, y, w) + S(2);
                y = AddLabel(GuideText(busy), new Font("Segoe UI", 10.5f), cFg, true, pad, y, w) + S(8);
                if (busy.Kind == BusyKind.Process)
                    y = AddLabel("اگه برنامه بعد از بسته شدن دوباره باز شد دنبال برنامه های واسطه بگرد، اگه بعد از ریستارت کامپیوتر باز این برنامه اجرا میشد، از بالا قسمت Startup تو Task Manager پیداش کن و غیرفعالش کن. اگه Access denied گرفتی از هوش مصنوعی بپرس که چیکار کنی.",
                                 new Font("Segoe UI", 9.5f), cMuted, true, pad, y, w) + S(8);

                // ---- وضعیت (بعد از زدن «بررسی دوباره» عوض میشه) ----
                _status = new Label
                {
                    Text = "", ForeColor = cMuted, Font = new Font("Segoe UI", 10f),
                    AutoSize = false, UseMnemonic = false, Left = pad, Top = y, Width = w, Height = S(44),
                    RightToLeft = RightToLeft.Yes
                };
                Controls.Add(_status);
                y += S(44) + S(8);

                // ---- دکمه‌ها: «بررسی دوباره» سمت راست، «باشه» کنارش ----
                int bh = S(40), gap = S(10);
                int cw = S(220), bw = S(150);
                _checkText = "بررسی دوباره‌ی پورت " + busy.Port;
                _check = MakeButton(_checkText, cAccent, cAccent, cBg);
                _check.Size = new Size(cw, bh);
                _check.Location = new Point(pad + w - cw, y);
                _check.Click += (s, e) => CheckClicked();
                Controls.Add(_check);
                AcceptButton = _check;

                var open = MakeButton("باز کردن برنامه", cCell, cGrid, cFg);
                open.Size = new Size(bw, bh);
                open.Location = new Point(_check.Left - gap - bw, y);
                open.Click += (s, e) => Close();        // بسته شدن پنجره => OnFormClosed برنامه رو باز میکنه
                Controls.Add(open);
                CancelButton = open;

                // صفحه‌ی کوچیک: پنجره از صفحه بلندتر نشه (اسکرول میخوره)
                int ch2 = y + bh + pad;
                var wa = Screen.PrimaryScreen != null ? Screen.PrimaryScreen.WorkingArea : new Rectangle(0, 0, 1920, 1080);
                int maxH = wa.Height - S(60);
                if (ch2 > maxH)
                {
                    AutoScroll = true;
                    ch2 = maxH;
                }
                ClientSize = new Size(w + 2 * pad + (ch2 == maxH ? S(20) : 0), ch2);
            }

            protected override void OnFormClosed(FormClosedEventArgs e)
            {
                base.OnFormClosed(e);
                // پورت ۸۰ آزاد نشد و کاربر پنجره رو بست (یا «باز کردن برنامه» رو زد): برنامه‌ی اصلی بدون پنجره تو tray نمونه
                if (!_switched && _onOpen != null)
                {
                    try { _onOpen(); } catch { }
                }
            }

            // آدرس‌های بلند تو دو خط جا میشن؛ پیام‌های کوتاه تو یه خط
            private static int layoutPathHeight(string path, bool realPath)
            {
                return (realPath && path.Length > 62) ? 30 : 18;
            }

            private async void CheckClicked()
            {
                _check.Enabled = false;
                _check.Text = "در حال بررسی ...";
                _status.ForeColor = cMuted;
                _status.Text = "";

                int port = _busy.Port;
                int result;      // 0 = هنوز پره ، 1 = آزاد شد و سرور رفت روش ، 2 = آزاد بود ولی سرور نتونست روش بیاد
                try
                {
                    result = await Task.Run(() =>
                    {
                        if (!IsFree(port)) return 0;
                        return _switchTo(port) ? 1 : 2;
                    });
                }
                catch
                {
                    result = 2;
                }

                if (result == 1)
                {
                    _switched = true;
                    try { _onSwitched(port); } catch { }       // آدرس پنجره‌ی اصلی آپدیت میشه و میاد جلو
                    if (!IsDisposed) Close();
                    ShowSuccess(_owner, port);
                    return;
                }

                if (IsDisposed) return;
                _check.Enabled = true;
                _check.Text = _checkText;
                _status.ForeColor = cWarn;
                _status.Text = result == 0
                    ? "هنوز پورت " + port + " آزاد نشده؛ مطمئن شو برنامه ای که پورت رو گرفته کامل بسته شده و اسم برنامه تو Task Manager هم نباشه."
                    : "پورت " + port + "انگار همین ی لحظه باز بود ولی بسته شد یا نمیدونم، دوباره تلاش کن";
            }
        }
    }
}
