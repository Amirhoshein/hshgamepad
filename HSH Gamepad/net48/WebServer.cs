using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace GamepadServer;

internal sealed class WebServer
{
    private const string WsGuid = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";

    private static string HostBlockedPage =>
        "<!DOCTYPE html><html lang=\"" + L.Code + "\" dir=\"" + (L.Rtl ? "rtl" : "ltr") + "\"><head><meta charset=\"utf-8\">" +
        "<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\"><title>Gamepad</title>" +
        "<style>html,body{height:100%;margin:0;background:#12151d;color:#e6e9f2;font-family:system-ui,Tahoma,sans-serif}" +
        "body{display:flex;align-items:center;justify-content:center;text-align:center;padding:24px}" +
        "p{color:#8d97b3;margin:8px 0 0}</style></head><body><div><h2>" + L.T("app.hostBlockedTitle") + "</h2>" +
        "<p>" + L.T("app.hostBlockedBody") + "</p></div></body></html>";

    private sealed class Request
    {
        public string Method = "";
        public string Path = "/";
        public string Query = "";
        public readonly Dictionary<string, string> Headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }

    private readonly Hub _hub;
    private readonly string _html;
    private readonly int _port;
    private TcpListener? _listener;
    private volatile bool _stopped;

    public WebServer(Hub hub, string html, int port)
    {
        _hub = hub;
        _html = html;
        _port = port;
    }

    public void Start()
    {
        _listener = new TcpListener(IPAddress.Any, _port);
        _listener.Start();
        _ = Task.Run(AcceptLoop);
    }

    public void Stop()
    {
        _stopped = true;
        try { _listener?.Stop(); } catch { }
    }

    private async Task AcceptLoop()
    {
        var l = _listener!;
        while (!_stopped)
        {
            TcpClient c;
            try
            {
                c = await l.AcceptTcpClientAsync();
            }
            catch
            {
                if (_stopped) break;
                await Task.Delay(50);
                continue;
            }
            _ = Task.Run(() => HandleClient(c));
        }
    }

    private static void EnableKeepAlive(Socket s)
    {
        try
        {
            var v = new byte[12];
            BitConverter.GetBytes(1).CopyTo(v, 0);
            BitConverter.GetBytes(20000).CopyTo(v, 4);
            BitConverter.GetBytes(5000).CopyTo(v, 8);
            s.IOControl(IOControlCode.KeepAliveValues, v, null);
        }
        catch { }
    }

    private async Task HandleClient(TcpClient tcp)
    {
        try
        {
            using (tcp)
            {
                tcp.NoDelay = true;
                EnableKeepAlive(tcp.Client);
                var stream = tcp.GetStream();

                var req = await ReadRequest(stream);
                if (req == null) return;

                bool host = IsHost(tcp);
                switch (AccessGuard.Check(RemoteIp(tcp), req.Path, host))
                {
                    case AccessKind.Socket:
                        await HandleWebSocket(tcp, stream, req);
                        return;
                    case AccessKind.Favicon:
                        await Respond(stream, 204, "No Content", "text/plain", new byte[0]);
                        return;
                    case AccessKind.Ignore:
                        return;
                    case AccessKind.Deny:
                        await Vanish(tcp);
                        return;
                    case AccessKind.Page when req.Method == "GET" || req.Method == "HEAD":
                        if (host)
                            await Respond(stream, 403, "Forbidden", "text/html; charset=utf-8", Encoding.UTF8.GetBytes(HostBlockedPage));
                        else
                            await Respond(stream, 200, "OK", "text/html; charset=utf-8", Encoding.UTF8.GetBytes(L.RenderIndex(_html)));
                        return;
                }

                await Respond(stream, 404, "Not Found", "text/plain", Encoding.ASCII.GetBytes("Not found"));
            }
        }
        catch
        {
        }
    }

    private static async Task Vanish(TcpClient tcp)
    {
        if (AccessGuard.TryHold())
        {
            try { await Task.Delay(AccessGuard.DenyDelayMs); }
            finally { AccessGuard.Release(); }
        }
        try { tcp.Client.LingerState = new LingerOption(true, 0); } catch { }
        try { tcp.Client.Close(); } catch { }
    }

    private static IPAddress? RemoteIp(TcpClient tcp)
    {
        try { return ((IPEndPoint)tcp.Client.RemoteEndPoint!).Address; }
        catch { return null; }
    }

    private async Task HandleWebSocket(TcpClient tcp, NetworkStream stream, Request req)
    {
        if (IsHost(tcp))
        {
            await Respond(stream, 403, "Forbidden", "text/plain", new byte[0]);
            return;
        }

        if (!req.Headers.TryGetValue("Sec-WebSocket-Key", out var key) ||
            !req.Headers.TryGetValue("Upgrade", out var upgrade) ||
            !upgrade.Equals("websocket", StringComparison.OrdinalIgnoreCase))
        {
            await Respond(stream, 400, "Bad Request", "text/plain", new byte[0]);
            return;
        }

        string accept;
        using (var sha = SHA1.Create())
            accept = Convert.ToBase64String(sha.ComputeHash(Encoding.ASCII.GetBytes(key.Trim() + WsGuid)));

        var head = Encoding.ASCII.GetBytes(
            "HTTP/1.1 101 Switching Protocols\r\nUpgrade: websocket\r\nConnection: Upgrade\r\n" +
            "Sec-WebSocket-Accept: " + accept + "\r\n\r\n");
        await stream.WriteAsync(head, 0, head.Length);

        var q = ParseQuery(req.Query);
        q.TryGetValue("id", out var id);
        q.TryGetValue("name", out var name);
        req.Headers.TryGetValue("User-Agent", out var ua);

        string ipText = "?";
        try
        {
            var ip = ((IPEndPoint)tcp.Client.RemoteEndPoint!).Address;
            if (ip.IsIPv4MappedToIPv6) ip = ip.MapToIPv4();
            ipText = ip.ToString();
        }
        catch { }

        var ws = new WsStream(tcp, stream);
        var conn = new Conn(ws, id ?? "", name ?? "", DeviceInfo.FromUserAgent(ua), ipText);
        _hub.Add(conn);

        try
        {
            while (true)
            {
                string? text = await ws.ReceiveTextAsync();
                if (text == null) break;

                foreach (var line in text.Split('\n'))
                    Protocol.Handle(conn, _hub, line);

                conn.Slot?.Submit();
            }
        }
        catch
        {
        }
        finally
        {
            _hub.Remove(conn);
            ws.Abort();
        }
    }


    private static async Task<Request?> ReadRequest(NetworkStream stream)
    {
        var buf = new byte[8192];
        int len = 0;
        while (true)
        {
            if (len == buf.Length) return null;
            var read = stream.ReadAsync(buf, len, buf.Length - len);
            if (await Task.WhenAny(read, Task.Delay(10000)) != read) return null;
            int n = read.Result;
            if (n <= 0) return null;
            len += n;

            int end = HeaderEnd(buf, len);
            if (end < 0) continue;
            return Parse(Encoding.UTF8.GetString(buf, 0, end));
        }
    }

    private static int HeaderEnd(byte[] b, int len)
    {
        for (int i = 3; i < len; i++)
            if (b[i - 3] == '\r' && b[i - 2] == '\n' && b[i - 1] == '\r' && b[i] == '\n') return i - 3;
        return -1;
    }

    private static Request? Parse(string text)
    {
        var lines = text.Split(new[] { "\r\n" }, StringSplitOptions.None);
        var first = lines[0].Split(' ');
        if (first.Length < 2) return null;

        var r = new Request { Method = first[0].ToUpperInvariant() };
        string target = first[1];
        int qi = target.IndexOf('?');
        r.Path = qi < 0 ? target : target.Substring(0, qi);
        r.Query = qi < 0 ? "" : target.Substring(qi + 1);

        for (int i = 1; i < lines.Length; i++)
        {
            int c = lines[i].IndexOf(':');
            if (c <= 0) continue;
            r.Headers[lines[i].Substring(0, c).Trim()] = lines[i].Substring(c + 1).Trim();
        }
        return r;
    }

    private static Dictionary<string, string> ParseQuery(string query)
    {
        var d = new Dictionary<string, string>();
        foreach (var part in query.Split(new[] { '&' }, StringSplitOptions.RemoveEmptyEntries))
        {
            int eq = part.IndexOf('=');
            string k = eq < 0 ? part : part.Substring(0, eq);
            string v = eq < 0 ? "" : part.Substring(eq + 1);
            d[WebUtility.UrlDecode(k)] = WebUtility.UrlDecode(v);
        }
        return d;
    }

    private static async Task Respond(Stream s, int code, string reason, string contentType, byte[] body)
    {
        var head = Encoding.ASCII.GetBytes(
            "HTTP/1.1 " + code + " " + reason + "\r\n" +
            "Content-Type: " + contentType + "\r\n" +
            "Content-Length: " + body.Length + "\r\n" +
            "Cache-Control: no-store\r\n" +
            "Connection: close\r\n\r\n");
        var all = new byte[head.Length + body.Length];
        Buffer.BlockCopy(head, 0, all, 0, head.Length);
        Buffer.BlockCopy(body, 0, all, head.Length, body.Length);
        await s.WriteAsync(all, 0, all.Length);
    }

    private static bool IsHost(TcpClient tcp)
    {
        try
        {
            var ip = ((IPEndPoint)tcp.Client.RemoteEndPoint!).Address;
            if (ip.IsIPv4MappedToIPv6) ip = ip.MapToIPv4();
            if (IPAddress.IsLoopback(ip)) return true;

            var local = ((IPEndPoint)tcp.Client.LocalEndPoint!).Address;
            if (local.IsIPv4MappedToIPv6) local = local.MapToIPv4();
            if (ip.Equals(local)) return true;

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
