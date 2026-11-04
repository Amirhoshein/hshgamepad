// HSH Gamepad | Telegram: @HSHGamepad
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Nefarius.ViGEm.Client;

namespace GamepadServer;

/// <summary>یک گوشی که وصله (یک WebSocket)</summary>
public sealed class Conn
{
    private readonly SemaphoreSlim _sendLock = new(1, 1);

    public WebSocket Ws { get; }
    public string ClientId { get; }
    public string Device { get; }
    public string Ip { get; }

    /// <summary>اسمی که کاربر خودش گذاشته (فقط برای نمایش)</summary>
    public string Name { get; set; }
    public string Display => Name.Length > 0 ? Name : Device;

    /// <summary>پلیری که این گوشی گرفته (null = هنوز هیچی نگرفته)</summary>
    public Slot? Slot { get; set; }
    public string Mode { get; set; } = "ps4";

    /// <summary>چیدمان دکمه‌های این گوشی (JSON معتبر، همون که گوشی با "lay" فرستاده)؛ خالی = هنوز نفرستاده.
    /// برای "کپی چیدمان از بقیه" نگه داشته میشه.</summary>
    public volatile string LayoutJson = "";

    /// <summary>گوشی ژیروسکوپ رو روشن کرده ("gyr 1")؛ تا روشن نباشه پیام‌های "gy" نادیده گرفته میشن</summary>
    public volatile bool GyroOn;

    public Conn(WebSocket ws, string clientId, string name, string device, string ip)
    {
        Ws = ws;
        ClientId = clientId;
        Name = CleanName(name);
        Device = device;
        Ip = ip;
    }

    public static string CleanName(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return "";
        var sb = new StringBuilder();
        foreach (var ch in s)
            if (!char.IsControl(ch)) sb.Append(ch);
        var t = sb.ToString().Trim();
        return t.Length > 24 ? t.Substring(0, 24) : t;
    }

    public async Task SendAsync(string text)
    {
        try
        {
            if (Ws.State != WebSocketState.Open) return;
            await _sendLock.WaitAsync();
            try
            {
                await Ws.SendAsync(Encoding.UTF8.GetBytes(text), WebSocketMessageType.Text, true, CancellationToken.None);
            }
            finally
            {
                _sendLock.Release();
            }
        }
        catch
        {
            // گوشی رفته؛ حلقه‌ی دریافت خودش جمعش میکنه
        }
    }

    public void Kick()
    {
        try { Ws.Abort(); } catch { }
    }
}

public sealed record SlotInfo(
    int N, bool HasPad, string? Mode, bool FellBack,
    string? Name, string? Device, string? Ip, double? IdleSeconds);

/// <summary>مدیریت ۹ اسلات و گوشی‌های وصل. هر پلیر فقط یک گوشی میتونه داشته باشه.</summary>
public sealed class Hub : IDisposable
{
    private readonly Dictionary<int, Slot> _slots = new();
    private readonly List<Conn> _conns = new();

    public object Sync { get; } = new();

    /// <summary>بعد از چند ثانیه بی‌گوشی ماندن، دسته‌ی مجازی حذف بشه (0 = هیچ وقت)</summary>
    public int AutoKickSeconds { get; set; }

    /// <summary>اجازه‌ی کنترل ماوس و کیبورد PC از گوشی (فقط پلیر ۱). مقدار اولیه‌ش از تنظیمات میاد (پیش‌فرض روشن)؛ از تیک پایین پنجره‌ی سرور عوض میشه.</summary>
    public volatile bool AllowInput;

    /// <summary>هر وقت وضعیت عوض بشه (از هر ترد) صدا زده میشه</summary>
    public event Action? Changed;

    public Hub(ViGEmClient client)
    {
        for (int i = 1; i <= 9; i++) _slots[i] = new Slot(client, i);
    }

    public void Add(Conn c)
    {
        lock (Sync)
        {
            // همون گوشی (با همون ClientId) دوباره وصل شده؛ اتصال قبلی‌ش احتمالاً نیمه‌مرده‌ست => کنار بره
            if (c.ClientId.Length > 0)
            {
                foreach (var old in _conns.Where(x => x.ClientId == c.ClientId).ToList())
                {
                    RemoveLocked(old);
                    old.Kick();
                }
            }
            _conns.Add(c);
        }
        Notify(null);
    }

    public void Remove(Conn c)
    {
        bool removed;
        lock (Sync) removed = RemoveLocked(c);
        if (removed) Notify(null);
    }

    private bool RemoveLocked(Conn c)
    {
        if (!_conns.Remove(c)) return false;
        var s = c.Slot;
        if (s != null && ReferenceEquals(s.Owner, c))
        {
            s.Release();
            s.Owner = null;
            s.IdleSince = DateTime.UtcNow;
        }
        c.Slot = null;
        return true;
    }

    /// <summary>گرفتن پلیر n با نوع دسته‌ی mode. اگه یکی دیگه ازش استفاده میکنه false برمیگردونه.</summary>
    public bool Claim(Conn c, int n, string mode)
    {
        bool ok;
        lock (Sync)
        {
            if (!_slots.TryGetValue(n, out var next)) return false;

            if (next.Owner != null && !ReferenceEquals(next.Owner, c))
            {
                ok = false;
            }
            else
            {
                var prev = c.Slot;
                if (prev != null && !ReferenceEquals(prev, next))
                {
                    prev.Release();          // پلیر قبلی این گوشی ول بشه
                    prev.Owner = null;
                    prev.IdleSince = DateTime.UtcNow;
                }
                next.Owner = c;
                next.IdleSince = null;
                c.Slot = next;
                c.Mode = next.SetMode(mode) ?? mode;
                ok = true;
            }
        }
        Notify(c);
        return ok;
    }

    /// <summary>تست لرزش: به گوشیِ پلیر n دستور لرزش میفرسته (۰ و ۰ = قطع). اگه گوشی وصل نباشه false.</summary>
    public bool TestRumble(int n, byte large, byte small)
    {
        Slot? s;
        lock (Sync)
        {
            if (!_slots.TryGetValue(n, out s)) return false;
        }
        if (s.Owner == null) return false;
        s.TestRumble(large, small);
        return true;
    }

    /// <summary>عوض کردن اسم نمایشی یه گوشی</summary>
    public void Rename(Conn c, string name)
    {
        c.Name = Conn.CleanName(name);
        Notify(null);
    }

    /// <summary>چیدمانِ گوشیِ پلیر n (JSON). اگه پلیر خالیه یا گوشی هنوز چیدمانش رو نفرستاده null برمیگردونه.</summary>
    public string? LayoutOf(int n)
    {
        lock (Sync)
        {
            if (!_slots.TryGetValue(n, out var s)) return null;
            var j = s.Owner?.LayoutJson;
            return string.IsNullOrEmpty(j) ? null : j;
        }
    }

    /// <summary>دسته‌ی مجازی یه پلیر آزاد رو از ویندوز برمیداره. اگه گوشی پشتشه کاری نمیکنه.</summary>
    public bool RemovePad(int n)
    {
        bool done = false;
        lock (Sync)
        {
            if (_slots.TryGetValue(n, out var s) && s.Owner == null && s.HasPad)
            {
                s.RemovePad();
                done = true;
            }
        }
        if (done) Notify(null);
        return done;
    }

    /// <summary>هر ثانیه صدا زده میشه؛ دسته‌های آزادِ قدیمی رو (اگه حذف خودکار روشنه) برمیداره</summary>
    public void Tick()
    {
        lock (Sync)
        {
            foreach (var s in _slots.Values) s.GyroWatchdog();   // گوشی وسط چرخش ساکت شد => سرعت چرخش صفر
        }

        int secs = AutoKickSeconds;
        if (secs <= 0) return;

        bool changed = false;
        var now = DateTime.UtcNow;
        lock (Sync)
        {
            foreach (var s in _slots.Values)
            {
                if (s.Owner == null && s.HasPad && s.IdleSince != null && (now - s.IdleSince.Value).TotalSeconds >= secs)
                {
                    s.RemovePad();
                    changed = true;
                }
            }
        }
        if (changed) Notify(null);
    }

        public PadState GetPadState(int n)
    {
        lock (Sync)
        {
            return _slots.TryGetValue(n, out var s) ? s.GetVisualState() : new PadState { N = n, Mode = "ps4" };
        }
    }

    public List<SlotInfo> Snapshot()
    {
        lock (Sync)
        {
            var now = DateTime.UtcNow;
            return _slots.Values
                .OrderBy(s => s.N)
                .Select(s => new SlotInfo(
                    s.N, s.HasPad, s.Mode, s.FellBack,
                    s.Owner?.Name, s.Owner?.Device, s.Owner?.Ip,
                    s.IdleSince == null ? null : (now - s.IdleSince.Value).TotalSeconds))
                .ToList();
        }
    }

    /// <summary>به همه‌ی گوشی‌ها وضعیت پلیرها رو میفرسته. گوشی ack فقط جواب درخواست خودشه.</summary>
    private void Notify(Conn? ack)
    {
        List<(Conn, string)> msgs;
        lock (Sync)
        {
            var taken = _slots.Values
                .Where(s => s.Owner != null)
                .ToDictionary(s => s.N.ToString(), s => s.Owner!.Display);

            msgs = _conns.Select(c => (c, JsonSerializer.Serialize(new
            {
                t = "state",
                lay = 1,                // این سرور کپی چیدمان ("lay" / "getlay") رو پشتیبانی میکنه
                taken,
                you = c.Slot?.N ?? 0,
                mode = c.Slot != null ? c.Mode : null,
                ack = ReferenceEquals(c, ack),
            }))).ToList();
        }

        foreach (var (c, m) in msgs) _ = c.SendAsync(m);
        Changed?.Invoke();
    }

    public void Dispose()
    {
        lock (Sync)
        {
            foreach (var s in _slots.Values) s.Dispose();
        }
    }
}

/// <summary>اسم تقریبی دستگاه از روی User-Agent مرورگر (مرورگر اسم واقعی گوشی رو نمیده)</summary>
public static class DeviceInfo
{
    public static string FromUserAgent(string? ua)
    {
        if (string.IsNullOrWhiteSpace(ua)) return "Unknown device";

        if (ua.Contains("Android", StringComparison.OrdinalIgnoreCase))
        {
            var m = Regex.Match(ua, @"Android\s[\d.]+;\s*([^;)]+)");
            if (m.Success)
            {
                var model = Regex.Replace(m.Groups[1].Value, @"\s*Build/.*$", "").Trim();
                // کروم مدل رو مخفی میکنه و فقط "K" میذاره
                if (model.Length > 1) return "Android " + model;
            }
            return "Android phone";
        }
        if (ua.Contains("iPhone", StringComparison.OrdinalIgnoreCase)) return "iPhone";
        if (ua.Contains("iPad", StringComparison.OrdinalIgnoreCase)) return "iPad";
        if (ua.Contains("Windows NT", StringComparison.OrdinalIgnoreCase)) return "Windows PC";
        if (ua.Contains("Macintosh", StringComparison.OrdinalIgnoreCase)) return "Mac";
        if (ua.Contains("Linux", StringComparison.OrdinalIgnoreCase)) return "Linux";
        return "Unknown device";
    }
}
