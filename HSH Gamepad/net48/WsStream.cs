// HSH Gamepad | Telegram: @HSHGamepad
using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GamepadServer;

/// <summary>
/// یه WebSocket سمت سرور (RFC 6455) روی یه اتصال TCP؛ فقط پیام متنی (دستورهای دسته).
/// چون .NET Framework تابع WebSocket.CreateFromStream نداره، فریم‌ها رو خودمون میخونیم و میفرستیم.
/// </summary>
public sealed class WsStream
{
    private const int MaxMessage = 64 * 1024;

    private readonly TcpClient _tcp;
    private readonly NetworkStream _s;
    private readonly SemaphoreSlim _sendLock = new SemaphoreSlim(1, 1);
    private volatile bool _closed;

    public WsStream(TcpClient tcp, NetworkStream stream)
    {
        _tcp = tcp;
        _s = stream;
    }

    public bool IsOpen => !_closed;

    public Task SendTextAsync(string text) => SendFrameAsync(0x1, Encoding.UTF8.GetBytes(text));

    /// <summary>پیام باینری (فریم تصویر نمایشگر لمسی)</summary>
    public Task SendBinaryAsync(byte[] data) => SendFrameAsync(0x2, data);

    /// <summary>اتصال رو فوراً میبنده (حلقه‌ی دریافت با exception تموم میشه)</summary>
    public void Abort()
    {
        _closed = true;
        try { _tcp.Close(); } catch { }
    }

    private async Task SendFrameAsync(int opcode, byte[] payload)
    {
        if (_closed) return;

        int n = payload.Length;
        byte[] header;
        if (n < 126)
        {
            header = new byte[] { (byte)(0x80 | opcode), (byte)n };
        }
        else if (n <= 0xFFFF)
        {
            header = new byte[] { (byte)(0x80 | opcode), 126, (byte)(n >> 8), (byte)n };
        }
        else
        {
            header = new byte[10];
            header[0] = (byte)(0x80 | opcode);
            header[1] = 127;                       // ۸ بایت طول؛ چهار بایت بالا صفره
            header[6] = (byte)(n >> 24);
            header[7] = (byte)(n >> 16);
            header[8] = (byte)(n >> 8);
            header[9] = (byte)n;
        }

        var frame = new byte[header.Length + n];
        Buffer.BlockCopy(header, 0, frame, 0, header.Length);
        Buffer.BlockCopy(payload, 0, frame, header.Length, n);

        await _sendLock.WaitAsync();
        try
        {
            await _s.WriteAsync(frame, 0, frame.Length);
        }
        finally
        {
            _sendLock.Release();
        }
    }

    private async Task<bool> ReadExactAsync(byte[] buf, int count)
    {
        int off = 0;
        while (off < count)
        {
            int n = await _s.ReadAsync(buf, off, count - off);
            if (n <= 0) return false;
            off += n;
        }
        return true;
    }

    /// <summary>پیام متنی بعدی؛ null = اتصال بسته شد. ping رو خودش جواب میده.</summary>
    public async Task<string?> ReceiveTextAsync()
    {
        MemoryStream? msg = null;
        var hdr = new byte[2];

        while (true)
        {
            if (!await ReadExactAsync(hdr, 2)) return null;

            bool fin = (hdr[0] & 0x80) != 0;
            int op = hdr[0] & 0x0F;
            bool masked = (hdr[1] & 0x80) != 0;
            long len = hdr[1] & 0x7F;

            if (len == 126)
            {
                var b = new byte[2];
                if (!await ReadExactAsync(b, 2)) return null;
                len = (b[0] << 8) | b[1];
            }
            else if (len == 127)
            {
                var b = new byte[8];
                if (!await ReadExactAsync(b, 8)) return null;
                len = 0;
                for (int i = 0; i < 8; i++) len = (len << 8) | b[i];
            }

            if (len < 0 || len > MaxMessage) return null;     // پیام عجیب/خیلی بزرگ => قطع

            var mask = new byte[4];
            if (masked && !await ReadExactAsync(mask, 4)) return null;

            var data = new byte[len];
            if (len > 0 && !await ReadExactAsync(data, (int)len)) return null;
            if (masked)
                for (int i = 0; i < data.Length; i++) data[i] ^= mask[i & 3];

            switch (op)
            {
                case 0x8:   // close
                    try { await SendFrameAsync(0x8, data.Length >= 2 ? new[] { data[0], data[1] } : new byte[0]); } catch { }
                    return null;

                case 0x9:   // ping
                    await SendFrameAsync(0xA, data);
                    break;

                case 0x1:   // text (شروع پیام)
                    msg = new MemoryStream();
                    goto case 0x0;
                case 0x0:   // continuation
                    if (msg == null) break;
                    msg.Write(data, 0, data.Length);
                    if (msg.Length > MaxMessage) return null;
                    if (fin)
                        return Encoding.UTF8.GetString(msg.GetBuffer(), 0, (int)msg.Length);
                    break;

                default:    // pong و binary => نادیده
                    break;
            }
        }
    }
}
