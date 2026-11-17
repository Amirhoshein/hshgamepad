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

public sealed class Conn
{
    private readonly SemaphoreSlim _sendLock = new(1, 1);

    public WebSocket Ws { get; }
    public string ClientId { get; }
    public string Device { get; }
    public string Ip { get; }

    public string Name { get; set; }
    public string Display => Name.Length > 0 ? Name : Device;

    public Slot? Slot { get; set; }
    public string Mode { get; set; } = "ps4";

    public volatile string LayoutJson = "";

    public volatile bool GyroOn;

    private readonly object _castLock = new object();
    private ScreenCast.Caster? _cast;
    public int ScreenMon, ScrX, ScrY, ScrW, ScrH;
    public bool IsOpen => Ws.State == WebSocketState.Open;

    public void CastConfigure(Hub hub, int mon, int height, int fps, bool cursor, int buf, bool h264 = false)
    {
        lock (_castLock)
        {
            if (_cast == null) _cast = new ScreenCast.Caster(this, hub);
            _cast.Configure(mon, height, fps, cursor, buf, h264);
        }
    }

    public void CastAck()
    {
        ScreenCast.Caster? c;
        lock (_castLock) c = _cast;
        c?.Ack();
    }

    public void CastStop()
    {
        lock (_castLock)
        {
            _cast?.Stop();
            _cast = null;
        }
    }

    public async Task SendBinaryAsync(byte[] data)
    {
        try
        {
            if (Ws.State != WebSocketState.Open) return;
            await _sendLock.WaitAsync();
            try
            {
                await Ws.SendAsync(new ArraySegment<byte>(data), WebSocketMessageType.Binary, true, CancellationToken.None);
            }
            finally
            {
                _sendLock.Release();
            }
        }
        catch
        {
        }
    }

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

public sealed class Hub : IDisposable
{
    private readonly Dictionary<int, Slot> _slots = new();
    private readonly List<Conn> _conns = new();

    public object Sync { get; } = new();

    public int AutoKickSeconds { get; set; }

    public volatile bool AllowInput;

    public event Action? Changed;

    public Hub(ViGEmClient client)
    {
        for (int i = 1; i <= 9; i++) _slots[i] = new Slot(client, i);
    }

    public void Add(Conn c)
    {
        lock (Sync)
        {
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
        c.CastStop();
        return true;
    }

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
                    prev.Release();
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

    public void Rename(Conn c, string name)
    {
        c.Name = Conn.CleanName(name);
        Notify(null);
    }

    public string? LayoutOf(int n)
    {
        lock (Sync)
        {
            if (!_slots.TryGetValue(n, out var s)) return null;
            var j = s.Owner?.LayoutJson;
            return string.IsNullOrEmpty(j) ? null : j;
        }
    }

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

    public void Tick()
    {
        lock (Sync)
        {
            foreach (var s in _slots.Values) s.GyroWatchdog();
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
                lay = 1,
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
