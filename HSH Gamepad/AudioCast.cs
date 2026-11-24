// HSH Gamepad | Telegram: @HSHGamepad
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace GamepadServer;

/// <summary>
/// صدای کامپیوتر برای گوشی (دکمه‌ی بلندگو تو نمایشگر لمسی).
/// صدا از خروجی انتخاب‌شده (یا خروجی پیش‌فرض ویندوز) با WASAPI loopback گرفته میشه، به 32kHz استریو کم میشه و با IMA-ADPCM (۴ برابر کوچیک‌تر از PCM)
/// تو بسته‌های ۲۰ میلی‌ثانیه‌ای میره. وقتی صدایی پخش نمیشه (سکوت) هیچ چیزی فرستاده نمیشه، و تا گوشی «au 1» نفرستاده اصلاً ضبطی انجام نمیشه.
/// بسته‌ی باینری: [3][کانال][rate/10 u16][frames u16] + برای هر کانال [pred i16][index u8][0] + برای هر کانال frames/2 بایت نیبل (اولی پایین)
/// </summary>
internal static class AudioCast
{
    // ---------------- تنظیم (از پنجره‌ی Settings) ----------------
    private static string _deviceId = "";
    private static int _cfgVer;

    /// <summary>شناسه‌ی خروجی انتخاب‌شده؛ خالی = خروجی پیش‌فرض ویندوز (با عوض شدنش خودکار دنبالش میره)</summary>
    public static string DeviceId => Volatile.Read(ref _deviceId);
    public static int CfgVer => Volatile.Read(ref _cfgVer);

    public static void SetDevice(string? id)
    {
        Volatile.Write(ref _deviceId, id ?? "");
        Interlocked.Increment(ref _cfgVer);
    }

    public sealed class DeviceInfo
    {
        public string Id = "";
        public string Name = "";
    }

    /// <summary>خروجی‌های صدای فعال (برای لیست پنجره‌ی Settings)</summary>
    public static List<DeviceInfo> ListDevices()
    {
        var list = new List<DeviceInfo>();
        IMMDeviceEnumerator? en = null;
        IMMDeviceCollection? col = null;
        try
        {
            en = (IMMDeviceEnumerator)new MMDeviceEnumeratorCom();
            if (en.EnumAudioEndpoints(0, 1, out col) != 0 || col == null) return list;
            col.GetCount(out uint n);
            for (uint i = 0; i < n; i++)
            {
                IMMDevice? d = null;
                try
                {
                    if (col.Item(i, out d) != 0 || d == null) continue;
                    d.GetId(out string id);
                    string name = FriendlyName(d);
                    list.Add(new DeviceInfo { Id = id, Name = name.Length > 0 ? name : id });
                }
                catch { }
                finally { Rel(d); }
            }
        }
        catch { }
        finally { Rel(col); Rel(en); }
        return list;
    }

    private static string FriendlyName(IMMDevice d)
    {
        IPropertyStore? ps = null;
        try
        {
            if (d.OpenPropertyStore(0, out ps) != 0 || ps == null) return "";
            var key = new PropertyKey { fmtid = new Guid("a45c254e-df1c-4efd-8020-67d146a850e0"), pid = 14 };
            if (ps.GetValue(ref key, out PropVariant pv) != 0) return "";
            try { return pv.vt == 31 && pv.ptr != IntPtr.Zero ? (Marshal.PtrToStringUni(pv.ptr) ?? "") : ""; }
            finally { PropVariantClear(ref pv); }
        }
        catch { return ""; }
        finally { Rel(ps); }
    }

    private static void Rel(object? o)
    {
        try { if (o != null && Marshal.IsComObject(o)) Marshal.ReleaseComObject(o); } catch { }
    }

    private static void Chk(int hr)
    {
        if (hr < 0) Marshal.ThrowExceptionForHR(hr);
    }

    // ---------------- دستور گوشی: au 1 / au 0 ----------------
    public static void Command(Conn conn, Hub hub, string[] p)
    {
        if (p.Length < 2) return;
        if (p[1] != "1") { conn.AudioStop(); return; }
        if (!hub.AllowInput || conn.Mode != "screen" || conn.Slot == null || conn.Slot.N != 1) return;
        conn.AudioStart(hub);
    }

    // ---------------- ضبط و ارسال (یک گوشی = یک ترد) ----------------
    internal sealed class Caster
    {
        private const int DstRate = 32000;
        private const int Block = DstRate / 50;          // ۲۰ میلی‌ثانیه (عدد زوج)
        private readonly Conn _conn;
        private readonly Hub _hub;
        private Thread? _th, _txTh;
        private volatile bool _stop;
        private volatile bool _opened, _errSent;
        private readonly BlockingCollection<byte[]> _q = new BlockingCollection<byte[]>(12);

        public Caster(Conn conn, Hub hub) { _conn = conn; _hub = hub; }

        public bool Running => _th != null && _th.IsAlive && !_stop;

        public void Start()
        {
            _txTh = new Thread(TxLoop) { IsBackground = true, Name = "AudioTx" };
            _txTh.Start();
            _th = new Thread(Run) { IsBackground = true, Name = "AudioCast", Priority = ThreadPriority.AboveNormal };
            _th.Start();
        }

        public void Stop() { _stop = true; }

        private bool Alive() => !_stop && _hub.AllowInput && _conn.Mode == "screen" && _conn.IsOpen;

        private void TxLoop()
        {
            try
            {
                foreach (var m in _q.GetConsumingEnumerable())
                {
                    if (!Alive()) continue;
                    try { _conn.SendBinaryAsync(m).Wait(2000); } catch { }
                }
            }
            catch { }
        }

        private void Enqueue(byte[] m)
        {
            try
            {
                if (_q.IsAddingCompleted) return;
                if (!_q.TryAdd(m)) { _q.TryTake(out _); _q.TryAdd(m); }     // صف پره (شبکه کنده): قدیمی‌ترین پیدا میشه دور تا تاخیر بالا نره
            }
            catch { }
        }

        private void ReportError(string t)
        {
            if (_errSent || !_conn.IsOpen) return;
            _errSent = true;
            var sb = new StringBuilder();
            foreach (char c in t) sb.Append(c == '"' || c == '\\' || c < 0x20 ? ' ' : c);
            string s = sb.Length > 160 ? sb.ToString(0, 160) : sb.ToString();
            _ = _conn.SendAsync("{\"t\":\"aus\",\"err\":\"" + s + "\"}");
        }

        private void Run()
        {
            try
            {
                while (Alive())
                {
                    _opened = false;
                    try { Session(); }
                    catch (Exception ex)
                    {
                        if (!_opened) ReportError(ex.Message);
                    }
                    for (int i = 0; i < 10 && Alive(); i++) Thread.Sleep(50);       // مکث کوتاه قبل از تلاش دوباره (مثلاً بعد از کشیدن هدفون)
                }
            }
            finally
            {
                try { _q.CompleteAdding(); } catch { }
            }
        }

        private void Session()
        {
            int cfg = CfgVer;
            using (var cap = new Loopback(DeviceId))
            {
                var enc = new Encoder(cap.Rate, cap.Channels, cap.Bits, cap.IsFloat, Enqueue);
                cap.Start();
                _opened = true;
                _errSent = false;
                string curId = cap.Id;
                int lastCheck = Environment.TickCount;
                while (Alive())
                {
                    if (CfgVer != cfg) return;
                    if (Environment.TickCount - lastCheck >= 1000)
                    {
                        lastCheck = Environment.TickCount;
                        string want = DeviceId;
                        if (want.Length == 0)
                        {
                            if (Loopback.DefaultId() != curId) return;                  // خروجی پیش‌فرض عوض شد (مثلاً هدفون درآمد)
                        }
                        else if (want != curId && Loopback.IsActive(want)) return;     // خروجی انتخابی دوباره وصل شد
                    }
                    int got = cap.Drain(enc);
                    Thread.Sleep(got > 0 ? 4 : 10);
                }
            }
        }

        // ---------------- تبدیل: هر فرمتی -> استریو -> 32kHz -> ADPCM ----------------
        internal sealed class Encoder
        {
            private static readonly int[] IndexTable = { -1, -1, -1, -1, 2, 4, 6, 8 };
            private static readonly int[] StepTable =
            {
                7, 8, 9, 10, 11, 12, 13, 14, 16, 17, 19, 21, 23, 25, 28, 31, 34, 37, 41, 45,
                50, 55, 60, 66, 73, 80, 88, 97, 107, 118, 130, 143, 157, 173, 190, 209, 230, 253, 279, 307,
                337, 371, 408, 449, 494, 544, 598, 658, 724, 796, 876, 963, 1060, 1166, 1282, 1411, 1552, 1707, 1878, 2066,
                2272, 2499, 2749, 3024, 3327, 3660, 4026, 4428, 4871, 5358, 5894, 6484, 7132, 7845, 8630, 9493, 10442, 11487, 12635, 13899,
                15289, 16818, 18500, 20350, 22385, 24623, 27086, 29794, 32767
            };

            private readonly int _ch, _bits;
            private readonly bool _float;
            private readonly double _step;
            private readonly Action<byte[]> _emit;
            private double _accL, _accR, _remain;
            private readonly short[] _bl = new short[Block], _br = new short[Block];
            private int _n;
            private int _pl, _il, _pr, _ir;
            private int _quiet;

            public Encoder(int rate, int channels, int bits, bool isFloat, Action<byte[]> emit)
            {
                _ch = Math.Max(1, channels); _bits = bits; _float = isFloat; _emit = emit;
                _step = rate / (double)DstRate;
                _remain = _step;
            }

            public unsafe void Feed(IntPtr data, int frames, bool silent)
            {
                byte* b = (byte*)data;
                for (int f = 0; f < frames; f++)
                {
                    float l = 0, r = 0;
                    if (!silent)
                    {
                        int o = f * _ch;
                        if (_ch == 1) { l = r = Smp(b, o); }
                        else if (_ch == 2) { l = Smp(b, o); r = Smp(b, o + 1); }
                        else
                        {
                            // 5.1 / 7.1: FL FR FC LFE BL BR SL SR -> استریو
                            float c = Smp(b, o + 2) * 0.707f;
                            l = Smp(b, o) + c; r = Smp(b, o + 1) + c;
                            if (_ch > 5) { l += Smp(b, o + 4) * 0.5f; r += Smp(b, o + 5) * 0.5f; }
                            if (_ch > 7) { l += Smp(b, o + 6) * 0.5f; r += Smp(b, o + 7) * 0.5f; }
                            l *= 0.7f; r *= 0.7f;
                        }
                    }
                    Push(l, r);
                }
            }

            private unsafe float Smp(byte* b, int idx)
            {
                if (_float) return ((float*)b)[idx];
                switch (_bits)
                {
                    case 16: return ((short*)b)[idx] / 32768f;
                    case 24:
                        {
                            int o = idx * 3;
                            int v = (b[o + 2] << 24) | (b[o + 1] << 16) | (b[o] << 8);
                            return (v >> 8) / 8388608f;
                        }
                    case 32: return ((int*)b)[idx] / 2147483648f;
                    case 8: return (b[idx] - 128) / 128f;
                    default: return 0f;
                }
            }

            // میانگین‌گیری وزنی (box filter) از نرخ منبع به ۳۲kHz
            private void Push(float l, float r)
            {
                double w = 1.0;
                while (w > 1e-9)
                {
                    double take = w < _remain ? w : _remain;
                    _accL += l * take; _accR += r * take; _remain -= take; w -= take;
                    if (_remain <= 1e-9)
                    {
                        Put((float)(_accL / _step), (float)(_accR / _step));
                        _accL = 0; _accR = 0; _remain = _step;
                    }
                }
            }

            private static short Cv(float v)
            {
                int i = (int)(v * 32767f);
                return (short)(i > 32767 ? 32767 : (i < -32768 ? -32768 : i));
            }

            private void Put(float l, float r)
            {
                _bl[_n] = Cv(l); _br[_n] = Cv(r); _n++;
                if (_n >= Block) { _n = 0; Flush(); }
            }

            private int Enc(int sample, ref int pred, ref int idx)
            {
                int step = StepTable[idx];
                int diff = sample - pred, nib = 0;
                if (diff < 0) { nib = 8; diff = -diff; }
                int vp = step >> 3;
                if (diff >= step) { nib |= 4; diff -= step; vp += step; }
                step >>= 1;
                if (diff >= step) { nib |= 2; diff -= step; vp += step; }
                step >>= 1;
                if (diff >= step) { nib |= 1; vp += step; }
                pred += (nib & 8) != 0 ? -vp : vp;
                if (pred > 32767) pred = 32767; else if (pred < -32768) pred = -32768;
                idx += IndexTable[nib & 7];
                if (idx < 0) idx = 0; else if (idx > 88) idx = 88;
                return nib;
            }

            private void Flush()
            {
                int peak = 0;
                for (int i = 0; i < Block; i++)
                {
                    int a = Math.Abs((int)_bl[i]); if (a > peak) peak = a;
                    a = Math.Abs((int)_br[i]); if (a > peak) peak = a;
                }
                if (peak <= 8)
                {
                    // سکوت: بعد از ~۰٫۲ ثانیه سکوت دیگه چیزی نمیفرستیم (شبکه خالی میمونه)؛ مبدل هم صفر میشه تا شروع بعدی تمیز باشه
                    if (++_quiet > 10) { _pl = _il = _pr = _ir = 0; return; }
                }
                else _quiet = 0;

                int half = Block / 2;
                var pkt = new byte[6 + 8 + 2 * half];
                pkt[0] = 3; pkt[1] = 2;
                int rate10 = DstRate / 10;
                pkt[2] = (byte)(rate10 & 255); pkt[3] = (byte)(rate10 >> 8);
                pkt[4] = (byte)(Block & 255); pkt[5] = (byte)(Block >> 8);
                pkt[6] = (byte)(_pl & 255); pkt[7] = (byte)((_pl >> 8) & 255); pkt[8] = (byte)_il; pkt[9] = 0;
                pkt[10] = (byte)(_pr & 255); pkt[11] = (byte)((_pr >> 8) & 255); pkt[12] = (byte)_ir; pkt[13] = 0;
                int o = 14;
                for (int i = 0; i < Block; i += 2)
                    pkt[o++] = (byte)(Enc(_bl[i], ref _pl, ref _il) | (Enc(_bl[i + 1], ref _pl, ref _il) << 4));
                for (int i = 0; i < Block; i += 2)
                    pkt[o++] = (byte)(Enc(_br[i], ref _pr, ref _ir) | (Enc(_br[i + 1], ref _pr, ref _ir) << 4));
                _emit(pkt);
            }
        }
    }

    // ---------------- WASAPI loopback ----------------
    private sealed class Loopback : IDisposable
    {
        private IMMDeviceEnumerator? _en;
        private IMMDevice? _dev;
        private IAudioClient? _cl;
        private IAudioCaptureClient? _cap;
        private IntPtr _fmt;
        public string Id = "";
        public int Rate, Channels, Bits;
        public bool IsFloat;

        public static string DefaultId()
        {
            IMMDeviceEnumerator? en = null; IMMDevice? d = null;
            try
            {
                en = (IMMDeviceEnumerator)new MMDeviceEnumeratorCom();
                if (en.GetDefaultAudioEndpoint(0, 0, out d) != 0 || d == null) return "";
                d.GetId(out string id);
                return id;
            }
            catch { return ""; }
            finally { Rel(d); Rel(en); }
        }

        public static bool IsActive(string id)
        {
            IMMDeviceEnumerator? en = null; IMMDevice? d = null;
            try
            {
                en = (IMMDeviceEnumerator)new MMDeviceEnumeratorCom();
                if (en.GetDevice(id, out d) != 0 || d == null) return false;
                return d.GetState(out int st) == 0 && st == 1;
            }
            catch { return false; }
            finally { Rel(d); Rel(en); }
        }

        public Loopback(string wantId)
        {
            try
            {
                _en = (IMMDeviceEnumerator)new MMDeviceEnumeratorCom();
                if (wantId.Length > 0 && IsActive(wantId) && _en.GetDevice(wantId, out _dev) == 0 && _dev != null) { }
                else
                {
                    Rel(_dev); _dev = null;
                    Chk(_en.GetDefaultAudioEndpoint(0, 0, out _dev));         // خروجی انتخاب‌شده نبود => پیش‌فرض ویندوز
                }
                _dev!.GetId(out Id);

                Guid iidClient = new Guid("1CB9AD4C-DBFA-4C32-B178-C2F568A703B2");
                Chk(_dev.Activate(ref iidClient, 23, IntPtr.Zero, out object o));
                _cl = (IAudioClient)o;

                Chk(_cl.GetMixFormat(out _fmt));
                int tag = (ushort)Marshal.ReadInt16(_fmt, 0);
                Channels = Marshal.ReadInt16(_fmt, 2);
                Rate = Marshal.ReadInt32(_fmt, 4);
                Bits = Marshal.ReadInt16(_fmt, 14);
                IsFloat = tag == 3;
                if (tag == 0xFFFE && Marshal.ReadInt16(_fmt, 16) >= 22)
                    IsFloat = Marshal.ReadInt32(_fmt, 24) == 3;                // SubFormat.Data1: 3 = IEEE float, 1 = PCM
                if (Rate < 8000 || Channels < 1) throw new InvalidOperationException("Unsupported audio format");

                // AUDCLNT_SHAREMODE_SHARED = 0, AUDCLNT_STREAMFLAGS_LOOPBACK = 0x20000، بافر ۱ ثانیه
                Chk(_cl.Initialize(0, 0x00020000, 10000000L, 0, _fmt, IntPtr.Zero));
                Guid iidCap = new Guid("C8ADBD64-E71E-48A0-A4DE-185C395CD317");
                Chk(_cl.GetService(ref iidCap, out object c));
                _cap = (IAudioCaptureClient)c;
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        public void Start() { Chk(_cl!.Start()); }

        /// <summary>همه‌ی بسته‌های آماده رو میخونه و به انکودر میده؛ تعداد فریم‌های خونده‌شده رو برمیگردونه</summary>
        public int Drain(Caster.Encoder enc)
        {
            int total = 0;
            Chk(_cap!.GetNextPacketSize(out uint pkt));
            while (pkt != 0)
            {
                Chk(_cap.GetBuffer(out IntPtr data, out uint frames, out uint flags, out ulong dp, out ulong qp));
                try
                {
                    if (frames > 0) { enc.Feed(data, (int)frames, (flags & 2) != 0); total += (int)frames; }   // 2 = AUDCLNT_BUFFERFLAGS_SILENT
                }
                finally { _cap.ReleaseBuffer(frames); }
                Chk(_cap.GetNextPacketSize(out pkt));
            }
            return total;
        }

        public void Dispose()
        {
            try { _cl?.Stop(); } catch { }
            if (_fmt != IntPtr.Zero) { try { Marshal.FreeCoTaskMem(_fmt); } catch { } _fmt = IntPtr.Zero; }
            Rel(_cap); _cap = null;
            Rel(_cl); _cl = null;
            Rel(_dev); _dev = null;
            Rel(_en); _en = null;
        }
    }

    // ---------------- COM ----------------
    [DllImport("ole32.dll")]
    private static extern int PropVariantClear(ref PropVariant pv);

    [StructLayout(LayoutKind.Sequential)]
    private struct PropertyKey { public Guid fmtid; public uint pid; }

    [StructLayout(LayoutKind.Explicit, Size = 24)]
    private struct PropVariant
    {
        [FieldOffset(0)] public ushort vt;
        [FieldOffset(8)] public IntPtr ptr;
    }

    [ComImport, Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
    private class MMDeviceEnumeratorCom { }

    [ComImport, Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMDeviceEnumerator
    {
        [PreserveSig] int EnumAudioEndpoints(int dataFlow, int stateMask, out IMMDeviceCollection devices);
        [PreserveSig] int GetDefaultAudioEndpoint(int dataFlow, int role, out IMMDevice device);
        [PreserveSig] int GetDevice([MarshalAs(UnmanagedType.LPWStr)] string id, out IMMDevice device);
        [PreserveSig] int RegisterEndpointNotificationCallback(IntPtr client);
        [PreserveSig] int UnregisterEndpointNotificationCallback(IntPtr client);
    }

    [ComImport, Guid("0BD7A1BE-7A1A-44DB-8397-CC5392387B5E"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMDeviceCollection
    {
        [PreserveSig] int GetCount(out uint count);
        [PreserveSig] int Item(uint index, out IMMDevice device);
    }

    [ComImport, Guid("D666063F-1587-4E43-81F1-B948E807363F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMDevice
    {
        [PreserveSig] int Activate(ref Guid iid, int clsCtx, IntPtr activationParams, [MarshalAs(UnmanagedType.IUnknown)] out object iface);
        [PreserveSig] int OpenPropertyStore(int access, out IPropertyStore props);
        [PreserveSig] int GetId([MarshalAs(UnmanagedType.LPWStr)] out string id);
        [PreserveSig] int GetState(out int state);
    }

    [ComImport, Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IPropertyStore
    {
        [PreserveSig] int GetCount(out uint count);
        [PreserveSig] int GetAt(uint index, out PropertyKey key);
        [PreserveSig] int GetValue(ref PropertyKey key, out PropVariant value);
        [PreserveSig] int SetValue(ref PropertyKey key, IntPtr value);
        [PreserveSig] int Commit();
    }

    [ComImport, Guid("1CB9AD4C-DBFA-4C32-B178-C2F568A703B2"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IAudioClient
    {
        [PreserveSig] int Initialize(int shareMode, uint streamFlags, long hnsBufferDuration, long hnsPeriodicity, IntPtr format, IntPtr audioSessionGuid);
        [PreserveSig] int GetBufferSize(out uint frames);
        [PreserveSig] int GetStreamLatency(out long latency);
        [PreserveSig] int GetCurrentPadding(out uint padding);
        [PreserveSig] int IsFormatSupported(int shareMode, IntPtr format, out IntPtr closest);
        [PreserveSig] int GetMixFormat(out IntPtr format);
        [PreserveSig] int GetDevicePeriod(out long defaultPeriod, out long minPeriod);
        [PreserveSig] int Start();
        [PreserveSig] int Stop();
        [PreserveSig] int Reset();
        [PreserveSig] int SetEventHandle(IntPtr eventHandle);
        [PreserveSig] int GetService(ref Guid riid, [MarshalAs(UnmanagedType.IUnknown)] out object service);
    }

    [ComImport, Guid("C8ADBD64-E71E-48A0-A4DE-185C395CD317"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IAudioCaptureClient
    {
        [PreserveSig] int GetBuffer(out IntPtr data, out uint frames, out uint flags, out ulong devicePosition, out ulong qpcPosition);
        [PreserveSig] int ReleaseBuffer(uint frames);
        [PreserveSig] int GetNextPacketSize(out uint frames);
    }
}
