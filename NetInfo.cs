// HSH Gamepad | Telegram: @HSHGamepad
using System;
using System.Net;
using System.Net.Sockets;

namespace GamepadServer;

internal static class NetInfo
{
    /// <summary>IP این کامپیوتر تو شبکه‌ی محلی؛ اگه به هیچ شبکه‌ای وصل نباشه null (نه 127.0.0.1)</summary>
    public static string? LanIp()
    {
        try
        {
            using (var s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            {
                s.Connect("10.255.255.255", 1);       // UDP: بسته‌ای فرستاده نمیشه، فقط ویندوز آداپتور مسیر رو انتخاب میکنه
                var ip = ((IPEndPoint)s.LocalEndPoint!).Address;
                if (IPAddress.IsLoopback(ip)) return null;
                var b = ip.GetAddressBytes();
                if (b[0] == 0) return null;
                if (b[0] == 169 && b[1] == 254) return null;   // APIPA: کارت شبکه وصله ولی DHCP جواب نداده
                return ip.ToString();
            }
        }
        catch
        {
            return null;
        }
    }

    public static string Url(string ip, int port) => port == 80 ? "http://" + ip : "http://" + ip + ":" + port;
}
