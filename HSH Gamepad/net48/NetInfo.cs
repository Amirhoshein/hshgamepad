using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace GamepadServer;

internal static class NetInfo
{
    public static string? LanIp()
    {
        IPAddress? routed = RoutedIp();
        try
        {
            if (routed != null && !IsVirtualIp(routed)) return routed.ToString();
            IPAddress? real = BestRealIp();
            if (real != null) return real.ToString();
        }
        catch { }
        return routed?.ToString();
    }

    private static bool Usable(IPAddress ip)
    {
        if (ip.AddressFamily != AddressFamily.InterNetwork) return false;
        if (IPAddress.IsLoopback(ip)) return false;
        var b = ip.GetAddressBytes();
        if (b[0] == 0) return false;
        if (b[0] == 169 && b[1] == 254) return false;
        return true;
    }

    private static IPAddress? RoutedIp()
    {
        try
        {
            using (var s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            {
                s.Connect("10.255.255.255", 1);
                var ip = ((IPEndPoint)s.LocalEndPoint!).Address;
                return Usable(ip) ? ip : null;
            }
        }
        catch
        {
            return null;
        }
    }

    private static bool IsVirtualNic(NetworkInterface n)
    {
        if (n.NetworkInterfaceType == NetworkInterfaceType.Tunnel || n.NetworkInterfaceType == NetworkInterfaceType.Loopback) return true;
        string d = (n.Description + " " + n.Name).ToLowerInvariant();
        string[] bad = { "virtual", "vmware", "vmnet", "vbox", "hyper-v", "vethernet", "wsl", "tap-", "tap ", "tun", "zerotier", "hamachi", "vpn", "wireguard", "npcap", "pseudo", "bluetooth" };
        foreach (var w in bad) if (d.Contains(w)) return true;
        return false;
    }

    private static bool IsVirtualIp(IPAddress ip)
    {
        try
        {
            foreach (var n in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (n.OperationalStatus != OperationalStatus.Up) continue;
                foreach (var u in n.GetIPProperties().UnicastAddresses)
                    if (u.Address.Equals(ip)) return IsVirtualNic(n);
            }
        }
        catch { }
        return false;
    }

    private static IPAddress? BestRealIp()
    {
        IPAddress? any = null;
        foreach (var n in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (n.OperationalStatus != OperationalStatus.Up || IsVirtualNic(n)) continue;
            var props = n.GetIPProperties();
            bool hasGateway = false;
            foreach (var g in props.GatewayAddresses)
                if (g.Address.AddressFamily == AddressFamily.InterNetwork && !g.Address.Equals(IPAddress.Any)) hasGateway = true;
            foreach (var u in props.UnicastAddresses)
            {
                if (!Usable(u.Address)) continue;
                if (hasGateway) return u.Address;
                if (any == null) any = u.Address;
            }
        }
        return any;
    }

    public static string Url(string ip, int port) => port == 80 ? "http://" + ip : "http://" + ip + ":" + port;
}
