// HSH Gamepad | Telegram: @HSHGamepad
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace GamepadServer;

internal enum AccessKind { Page, Socket, Favicon, NotFound, Ignore, Deny }

internal static class AccessGuard
{
    public const int DenyDelayMs = 10000;
    public const int KeyLength = 5;
    public const int MaxKeyLength = 32;
    public const int DefaultTries = 10;
    public const int DefaultMinutes = 10;

    private const string RuleName = "HSH Gamepad Ban";
    private const string RandomAlphabet = "abcdefghijklmnopqrstuvwxyz0123456789";
    private const int MaxTracked = 4096;
    private const int MaxHeld = 128;

    private sealed class Track
    {
        public readonly Dictionary<string, DateTime> Paths = new Dictionary<string, DateTime>(StringComparer.Ordinal);
        public DateTime BannedUntil = DateTime.MinValue;
    }

    private static readonly object Sync = new object();
    private static readonly Dictionary<string, Track> Tracks = new Dictionary<string, Track>(StringComparer.Ordinal);

    private static bool _enabled;
    private static string _key = "";
    private static bool _ban = true;
    private static int _maxTries = DefaultTries;
    private static int _banMinutes = DefaultMinutes;
    private static int _port = 80;
    private static int _held;

    private static Timer? _timer;
    private static string _fwWanted = "";
    private static string _fwApplied = "?";
    private static bool _fwBusy;
    private static bool _shutdown;

    public static string PagePath
    {
        get { lock (Sync) return _enabled ? "/" + _key : ""; }
    }

    public static void Apply(AppSettings s)
    {
        Configure(s.KeyEnabled, s.Key, s.BanEnabled, s.BanTries, s.BanMinutes);
    }

    public static void Configure(bool enabled, string key, bool ban, int maxTries, int banMinutes)
    {
        lock (Sync)
        {
            string clean = Clean(key);
            _enabled = enabled && clean.Length > 0;
            _key = clean;
            _ban = ban;
            _maxTries = Math.Max(1, maxTries);
            _banMinutes = Math.Max(1, banMinutes);
            if (!_enabled || !_ban) Tracks.Clear();
            else PruneLocked(DateTime.UtcNow);
            if (_timer == null && !_shutdown) _timer = new Timer(_ => Prune(), null, 15000, 15000);
            QueueFirewallLocked();
        }
    }

    public static void SetPort(int port)
    {
        lock (Sync)
        {
            _port = port;
            QueueFirewallLocked();
        }
    }

    public static AccessKind Check(IPAddress? ip, string rawPath, bool exempt)
    {
        string path = Normalize(rawPath);
        lock (Sync)
        {
            if (!_enabled)
            {
                if (path == "/") return AccessKind.Page;
                if (path == "/ws") return AccessKind.Socket;
                if (path == "/favicon.ico") return AccessKind.Favicon;
                return AccessKind.NotFound;
            }

            string who = IpText(ip);
            var now = DateTime.UtcNow;
            if (!exempt && who.Length > 0 && Tracks.TryGetValue(who, out var t) && t.BannedUntil > now)
                return AccessKind.Deny;

            string page = "/" + _key;
            if (string.Equals(path, page, StringComparison.Ordinal)) return AccessKind.Page;
            if (string.Equals(path, page + "/ws", StringComparison.Ordinal)) return AccessKind.Socket;
            if (path == "/favicon.ico") return AccessKind.Ignore;

            if (!exempt && _ban && who.Length > 0) RecordLocked(who, Guess(path), now);
            return AccessKind.Deny;
        }
    }

    public static bool TryHold()
    {
        if (Interlocked.Increment(ref _held) <= MaxHeld) return true;
        Interlocked.Decrement(ref _held);
        return false;
    }

    public static void Release()
    {
        Interlocked.Decrement(ref _held);
    }

    public static string RandomKey()
    {
        var sb = new StringBuilder(KeyLength);
        var buf = new byte[1];
        using (var rng = RandomNumberGenerator.Create())
        {
            int limit = 256 - 256 % RandomAlphabet.Length;
            while (sb.Length < KeyLength)
            {
                rng.GetBytes(buf);
                if (buf[0] >= limit) continue;
                sb.Append(RandomAlphabet[buf[0] % RandomAlphabet.Length]);
            }
        }
        return sb.ToString();
    }

    public static bool IsKeyChar(char c)
    {
        return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '-' || c == '_';
    }

    public static string Clean(string? key)
    {
        if (string.IsNullOrEmpty(key)) return "";
        var sb = new StringBuilder(key!.Length);
        foreach (char c in key)
        {
            if (!IsKeyChar(c)) continue;
            sb.Append(c);
            if (sb.Length == MaxKeyLength) break;
        }
        return sb.ToString();
    }

    public static void Shutdown()
    {
        lock (Sync)
        {
            _shutdown = true;
            _timer?.Dispose();
            _timer = null;
            Tracks.Clear();
            _fwWanted = "";
        }
        int until = Environment.TickCount + 5000;
        while (Environment.TickCount - until < 0)
        {
            lock (Sync) if (!_fwBusy) break;
            Thread.Sleep(50);
        }
        lock (Sync)
        {
            if (_fwApplied.Length == 0) return;
            _fwApplied = "";
        }
        DeleteRule();
    }

    private static string Normalize(string rawPath)
    {
        string p = string.IsNullOrEmpty(rawPath) ? "/" : rawPath;
        if (p[0] != '/') p = "/" + p;
        while (p.Length > 1 && p[p.Length - 1] == '/') p = p.Substring(0, p.Length - 1);
        return p;
    }

    private static string Guess(string path)
    {
        return path.EndsWith("/ws", StringComparison.Ordinal) && path.Length > 3 ? path.Substring(0, path.Length - 3) : path;
    }

    private static string IpText(IPAddress? ip)
    {
        if (ip == null) return "";
        if (ip.IsIPv4MappedToIPv6) ip = ip.MapToIPv4();
        string s = ip.ToString();
        int scope = s.IndexOf('%');
        return scope < 0 ? s : s.Substring(0, scope);
    }

    private static void RecordLocked(string who, string guess, DateTime now)
    {
        if (!Tracks.TryGetValue(who, out var t))
        {
            if (Tracks.Count >= MaxTracked) PruneLocked(now);
            if (Tracks.Count >= MaxTracked) return;
            t = new Track();
            Tracks[who] = t;
        }
        t.Paths[guess] = now;
        if (t.Paths.Count < _maxTries) return;
        t.Paths.Clear();
        t.BannedUntil = now.AddMinutes(_banMinutes);
        QueueFirewallLocked();
    }

    private static void Prune()
    {
        lock (Sync)
        {
            if (_shutdown) return;
            PruneLocked(DateTime.UtcNow);
            QueueFirewallLocked();
        }
    }

    private static void PruneLocked(DateTime now)
    {
        var window = TimeSpan.FromMinutes(_banMinutes);
        var dead = new List<string>();
        foreach (var kv in Tracks)
        {
            var paths = kv.Value.Paths;
            foreach (var old in paths.Where(p => now - p.Value > window).Select(p => p.Key).ToList())
                paths.Remove(old);
            if (paths.Count == 0 && kv.Value.BannedUntil <= now) dead.Add(kv.Key);
        }
        foreach (var k in dead) Tracks.Remove(k);
    }

    private static void QueueFirewallLocked()
    {
        var now = DateTime.UtcNow;
        var ips = Tracks.Where(kv => kv.Value.BannedUntil > now).Select(kv => kv.Key).OrderBy(s => s, StringComparer.Ordinal).ToList();
        _fwWanted = ips.Count == 0 || _shutdown ? "" : _port + "|" + string.Join(",", ips);
        if (_fwBusy || _fwWanted == _fwApplied || _shutdown) return;
        _fwBusy = true;
        ThreadPool.QueueUserWorkItem(_ => FirewallLoop());
    }

    private static void FirewallLoop()
    {
        while (true)
        {
            string want;
            lock (Sync)
            {
                if (_shutdown || _fwWanted == _fwApplied)
                {
                    _fwBusy = false;
                    return;
                }
                want = _fwWanted;
            }
            ApplyRule(want);
            lock (Sync) _fwApplied = want;
        }
    }

    private static void ApplyRule(string want)
    {
        DeleteRule();
        if (want.Length == 0) return;
        int bar = want.IndexOf('|');
        string port = want.Substring(0, bar);
        string ips = want.Substring(bar + 1);
        Netsh("advfirewall firewall add rule name=\"" + RuleName + "\" dir=in action=block protocol=TCP localport=" + port +
              " remoteip=" + ips + " enable=yes profile=any");
    }

    private static void DeleteRule()
    {
        Netsh("advfirewall firewall delete rule name=\"" + RuleName + "\"");
    }

    private static void Netsh(string args)
    {
        try
        {
            var psi = new ProcessStartInfo("netsh.exe", args)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WindowStyle = ProcessWindowStyle.Hidden,
            };
            using (var p = Process.Start(psi))
            {
                if (p == null) return;
                p.StandardOutput.ReadToEnd();
                p.StandardError.ReadToEnd();
                p.WaitForExit(10000);
            }
        }
        catch { }
    }
}
