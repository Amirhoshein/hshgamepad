// HSH Gamepad | Telegram: @HSHGamepad
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace GamepadServer;

// =====================================================================================================
//  انکود H.264 با Media Foundation (بدون هیچ کتابخونه‌ی اضافه) + بسته‌بندی fMP4 برای پخش زنده تو مرورگر (MSE)
//  مرحله‌ی ۱: انکودر نرم‌افزاری خود ویندوز (CMSH264EncoderMFT). مرحله‌ی ۲ (بعداً): انکودر سخت‌افزاری کارت گرافیک.
//  اگه هر مرحله‌ای خطا بده، ScreenCast خودش به JPEG برمیگرده.
// =====================================================================================================

#region COM
[StructLayout(LayoutKind.Sequential)]
internal struct MFT_OUTPUT_DATA_BUFFER { public uint dwStreamID; public IntPtr pSample; public uint dwStatus; public IntPtr pEvents; }
[StructLayout(LayoutKind.Sequential)]
internal struct MFT_OUTPUT_STREAM_INFO { public uint dwFlags; public uint cbSize; public uint cbAlignment; }
[StructLayout(LayoutKind.Sequential)]
internal struct MFT_INPUT_STREAM_INFO { public long hnsMaxLatency; public uint dwFlags; public uint cbSize; public uint cbMaxLookahead; public uint cbAlignment; }

[ComImport, Guid("045fa593-8799-42b8-bc8d-8968c6453507"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IMFMediaBuffer
{
    [PreserveSig] int Lock(out IntPtr buffer, out int maxLength, out int currentLength);
    [PreserveSig] int Unlock();
    [PreserveSig] int GetCurrentLength(out int length);
    [PreserveSig] int SetCurrentLength(int length);
    [PreserveSig] int GetMaxLength(out int length);
}

[ComImport, Guid("44ae0fa8-ea31-4109-8d2e-4cae4997c555"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IMFMediaType
{
    [PreserveSig] int GetItem();
    [PreserveSig] int GetItemType();
    [PreserveSig] int CompareItem();
    [PreserveSig] int Compare();
    [PreserveSig] int GetUINT32();
    [PreserveSig] int GetUINT64();
    [PreserveSig] int GetDouble();
    [PreserveSig] int GetGUID();
    [PreserveSig] int GetStringLength();
    [PreserveSig] int GetString();
    [PreserveSig] int GetAllocatedString();
    [PreserveSig] int GetBlobSize(ref Guid key, out uint size);
    [PreserveSig] int GetBlob(ref Guid key, [Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] byte[] buf, uint cb, out uint size);
    [PreserveSig] int GetAllocatedBlob();
    [PreserveSig] int GetUnknown();
    [PreserveSig] int SetItem();
    [PreserveSig] int DeleteItem();
    [PreserveSig] int DeleteAllItems();
    [PreserveSig] int SetUINT32(ref Guid key, uint value);
    [PreserveSig] int SetUINT64(ref Guid key, ulong value);
    [PreserveSig] int SetDouble();
    [PreserveSig] int SetGUID(ref Guid key, ref Guid value);
    [PreserveSig] int SetString();
    [PreserveSig] int SetBlob();
    [PreserveSig] int SetUnknown();
    [PreserveSig] int LockStore();
    [PreserveSig] int UnlockStore();
    [PreserveSig] int GetCount();
    [PreserveSig] int GetItemByIndex();
    [PreserveSig] int CopyAllItems();
    [PreserveSig] int GetMajorType(out Guid type);
    [PreserveSig] int IsCompressedFormat(out bool compressed);
    [PreserveSig] int IsEqual(IntPtr other, out uint flags);
    [PreserveSig] int GetRepresentation(Guid guid, out IntPtr rep);
    [PreserveSig] int FreeRepresentation(Guid guid, IntPtr rep);
}

[ComImport, Guid("c40a00f2-b93a-4d80-ae8c-5a1c634f58e4"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IMFSample
{
    [PreserveSig] int GetItem();
    [PreserveSig] int GetItemType();
    [PreserveSig] int CompareItem();
    [PreserveSig] int Compare();
    [PreserveSig] int GetUINT32();
    [PreserveSig] int GetUINT64();
    [PreserveSig] int GetDouble();
    [PreserveSig] int GetGUID();
    [PreserveSig] int GetStringLength();
    [PreserveSig] int GetString();
    [PreserveSig] int GetAllocatedString();
    [PreserveSig] int GetBlobSize(ref Guid key, out uint size);
    [PreserveSig] int GetBlob(ref Guid key, [Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] byte[] buf, uint cb, out uint size);
    [PreserveSig] int GetAllocatedBlob();
    [PreserveSig] int GetUnknown();
    [PreserveSig] int SetItem();
    [PreserveSig] int DeleteItem();
    [PreserveSig] int DeleteAllItems();
    [PreserveSig] int SetUINT32(ref Guid key, uint value);
    [PreserveSig] int SetUINT64(ref Guid key, ulong value);
    [PreserveSig] int SetDouble();
    [PreserveSig] int SetGUID(ref Guid key, ref Guid value);
    [PreserveSig] int SetString();
    [PreserveSig] int SetBlob();
    [PreserveSig] int SetUnknown();
    [PreserveSig] int LockStore();
    [PreserveSig] int UnlockStore();
    [PreserveSig] int GetCount();
    [PreserveSig] int GetItemByIndex();
    [PreserveSig] int CopyAllItems();
    [PreserveSig] int GetSampleFlags(out uint flags);
    [PreserveSig] int SetSampleFlags(uint flags);
    [PreserveSig] int GetSampleTime(out long time);
    [PreserveSig] int SetSampleTime(long time);
    [PreserveSig] int GetSampleDuration(out long duration);
    [PreserveSig] int SetSampleDuration(long duration);
    [PreserveSig] int GetBufferCount(out uint count);
    [PreserveSig] int GetBufferByIndex(uint index, out IMFMediaBuffer buffer);
    [PreserveSig] int ConvertToContiguousBuffer(out IMFMediaBuffer buffer);
    [PreserveSig] int AddBuffer(IMFMediaBuffer buffer);
    [PreserveSig] int RemoveBufferByIndex(uint index);
    [PreserveSig] int RemoveAllBuffers();
    [PreserveSig] int GetTotalLength(out uint length);
    [PreserveSig] int CopyToBuffer(IMFMediaBuffer buffer);
}

[ComImport, Guid("bf94c121-5b05-4e6f-8000-ba598961414d"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IMFTransform
{
    [PreserveSig] int GetStreamLimits(out uint inMin, out uint inMax, out uint outMin, out uint outMax);
    [PreserveSig] int GetStreamCount(out uint inputs, out uint outputs);
    [PreserveSig] int GetStreamIDs(uint inSize, IntPtr inIds, uint outSize, IntPtr outIds);
    [PreserveSig] int GetInputStreamInfo(uint id, out MFT_INPUT_STREAM_INFO info);
    [PreserveSig] int GetOutputStreamInfo(uint id, out MFT_OUTPUT_STREAM_INFO info);
    [PreserveSig] int GetAttributes(out IntPtr attributes);
    [PreserveSig] int GetInputStreamAttributes(uint id, out IntPtr attributes);
    [PreserveSig] int GetOutputStreamAttributes(uint id, out IntPtr attributes);
    [PreserveSig] int DeleteInputStream(uint id);
    [PreserveSig] int AddInputStreams(uint count, IntPtr ids);
    [PreserveSig] int GetInputAvailableType(uint id, uint index, out IMFMediaType type);
    [PreserveSig] int GetOutputAvailableType(uint id, uint index, out IMFMediaType type);
    [PreserveSig] int SetInputType(uint id, IMFMediaType type, uint flags);
    [PreserveSig] int SetOutputType(uint id, IMFMediaType type, uint flags);
    [PreserveSig] int GetInputCurrentType(uint id, out IMFMediaType type);
    [PreserveSig] int GetOutputCurrentType(uint id, out IMFMediaType type);
    [PreserveSig] int GetInputStatus(uint id, out uint flags);
    [PreserveSig] int GetOutputStatus(out uint flags);
    [PreserveSig] int SetOutputBounds(long lower, long upper);
    [PreserveSig] int ProcessEvent(uint id, IntPtr evt);
    [PreserveSig] int ProcessMessage(uint message, IntPtr param);
    [PreserveSig] int ProcessInput(uint id, IMFSample sample, uint flags);
    [PreserveSig] int ProcessOutput(uint flags, uint count, ref MFT_OUTPUT_DATA_BUFFER samples, out uint status);
}

[ComImport, Guid("901db4c7-31ce-41a2-85dc-8fa0bf41b8da"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface ICodecAPI
{
    [PreserveSig] int IsSupported(ref Guid api);
    [PreserveSig] int IsModifiable(ref Guid api);
    [PreserveSig] int GetParameterRange(ref Guid api, IntPtr min, IntPtr max, IntPtr step);
    [PreserveSig] int GetParameterValues(ref Guid api, IntPtr values, IntPtr count);
    [PreserveSig] int GetDefaultValue(ref Guid api, IntPtr value);
    [PreserveSig] int GetValue(ref Guid api, IntPtr value);
    [PreserveSig] int SetValue(ref Guid api, [In, MarshalAs(UnmanagedType.Struct)] ref object value);
}
#endregion

/// <summary>یک فریم انکودشده: AVCC (طول‌دار) + کلیدفریم بودن</summary>
internal sealed class H264Packet
{
    public byte[] Avcc = Array.Empty<byte>();
    public bool Key;
}

internal sealed class H264Encoder : IDisposable
{
    [DllImport("mfplat.dll")] private static extern int MFStartup(uint version, uint flags);
    [DllImport("mfplat.dll")] private static extern int MFCreateMediaType(out IMFMediaType type);
    [DllImport("mfplat.dll")] private static extern int MFCreateSample(out IMFSample sample);
    [DllImport("mfplat.dll")] private static extern int MFCreateMemoryBuffer(int cb, out IMFMediaBuffer buffer);

    private static Guid MF_MT_MAJOR_TYPE = new Guid("48eba18e-f8c9-4687-bf11-0a74c9f96a8f");
    private static Guid MF_MT_SUBTYPE = new Guid("f7e34c9a-42e8-4714-b74b-cb29d72c35e5");
    private static Guid MF_MT_AVG_BITRATE = new Guid("20332624-fb0d-4d9e-bd0d-cbf6786c102e");
    private static Guid MF_MT_FRAME_SIZE = new Guid("1652c33d-d6b2-4012-b834-72030849a37d");
    private static Guid MF_MT_FRAME_RATE = new Guid("c459a2e8-3d2c-4e44-b132-fee5156c7bb0");
    private static Guid MF_MT_PIXEL_ASPECT_RATIO = new Guid("c6376a1e-8d0a-4027-be45-6d9a0ad39bb6");
    private static Guid MF_MT_INTERLACE_MODE = new Guid("e2724bb8-e676-4806-b4b2-a8d6efb44ccd");
    private static Guid MF_MT_MPEG2_PROFILE = new Guid("ad76a80b-2d5c-4e0b-b375-64e520137036");
    private static Guid MF_MT_MPEG_SEQUENCE_HEADER = new Guid("3c036de7-3ad0-4c9e-9216-ee6d6ac21cb3");
    private static Guid MFMediaType_Video = new Guid("73646976-0000-0010-8000-00aa00389b71");
    private static Guid MFVideoFormat_H264 = new Guid("34363248-0000-0010-8000-00aa00389b71");
    private static Guid MFVideoFormat_NV12 = new Guid("3231564e-0000-0010-8000-00aa00389b71");
    private static readonly Guid CLSID_CMSH264EncoderMFT = new Guid("6ca50344-051a-4ded-9779-a43305165e35");

    // ICodecAPI (همه‌شون اختیاری‌ان؛ اگه انکودر نپذیرفت مهم نیست)
    private static Guid CODECAPI_RateControlMode = new Guid("1c0608e9-370c-4710-8a58-cb6181c42423");
    private static Guid CODECAPI_MeanBitRate = new Guid("f7222374-2144-4815-b550-a37f8e12ee52");
    private static Guid CODECAPI_GOPSize = new Guid("95f31b26-95a4-41aa-9303-246a7fc6eef1");
    private static Guid CODECAPI_LowLatency = new Guid("9c27891a-ed7a-40e1-88e8-b22727a024ee");
    private static Guid CODECAPI_BFrames = new Guid("8d390aac-dc5c-4200-b57f-814d04babab2");
    private static Guid CODECAPI_QualityVsSpeed = new Guid("98332df8-03cd-476b-89fa-3f9e442dec9f");

    private const int MF_E_TRANSFORM_NEED_MORE_INPUT = unchecked((int)0xC00D6D72);
    private const int MF_E_NOTACCEPTING = unchecked((int)0xC00D36B5);

    private static readonly object MfLock = new object();
    private static bool _mfStarted;

    private IMFTransform? _mft;
    private bool _provides;
    private int _outSize;
    private byte[] _nv12 = Array.Empty<byte>();
    private long _idx;
    private readonly long _dur;      // 100ns

    public int Width { get; }
    public int Height { get; }
    public int Fps { get; }
    public byte[]? Sps { get; private set; }
    public byte[]? Pps { get; private set; }

    private H264Encoder(int w, int h, int fps) { Width = w; Height = h; Fps = fps; _dur = 10000000L / Math.Max(1, fps); }

    private static void Check(int hr, string what)
    {
        if (hr < 0) throw new InvalidOperationException(what + " (0x" + hr.ToString("X8") + ")");
    }

    public static H264Encoder? TryCreate(int w, int h, int fps, int bitrate, out string error)
    {
        error = "";
        H264Encoder? e = null;
        try
        {
            lock (MfLock)
            {
                if (!_mfStarted) { Check(MFStartup(0x00020070, 0), "MFStartup"); _mfStarted = true; }
            }
            e = new H264Encoder(w, h, fps);
            e.Init(bitrate);
            return e;
        }
        catch (Exception ex)
        {
            error = "H.264: " + ex.GetType().Name + " - " + ex.Message;
            try { e?.Dispose(); } catch { }
            return null;
        }
    }

    private IMFMediaType MakeType(Guid subtype, bool output, int bitrate)
    {
        Check(MFCreateMediaType(out var t), "MFCreateMediaType");
        Check(t.SetGUID(ref MF_MT_MAJOR_TYPE, ref MFMediaType_Video), "major");
        Check(t.SetGUID(ref MF_MT_SUBTYPE, ref subtype), "subtype");
        Check(t.SetUINT64(ref MF_MT_FRAME_SIZE, ((ulong)(uint)Width << 32) | (uint)Height), "size");
        Check(t.SetUINT64(ref MF_MT_FRAME_RATE, ((ulong)(uint)Fps << 32) | 1u), "rate");
        Check(t.SetUINT64(ref MF_MT_PIXEL_ASPECT_RATIO, (1UL << 32) | 1u), "aspect");
        Check(t.SetUINT32(ref MF_MT_INTERLACE_MODE, 2), "interlace");       // MFVideoInterlace_Progressive
        if (output)
        {
            Check(t.SetUINT32(ref MF_MT_AVG_BITRATE, (uint)bitrate), "bitrate");
            Check(t.SetUINT32(ref MF_MT_MPEG2_PROFILE, 66), "profile");     // Baseline: بدون B-frame، همه‌ی گوشی‌ها دیکدش میکنن
        }
        return t;
    }

    private static void Codec(ICodecAPI? api, Guid key, object value)
    {
        if (api == null) return;
        try { api.SetValue(ref key, ref value); } catch { }
    }

    private void Init(int bitrate)
    {
        var type = Type.GetTypeFromCLSID(CLSID_CMSH264EncoderMFT) ?? throw new InvalidOperationException(L.T("enc.TheWindowsH264Encoder"));
        _mft = (IMFTransform)Activator.CreateInstance(type)!;

        // تنظیمات کم‌تاخیر (قبل از نوع‌ها)
        ICodecAPI? api = null;
        try { api = (ICodecAPI)_mft; } catch { }
        Codec(api, CODECAPI_LowLatency, true);
        Codec(api, CODECAPI_RateControlMode, 0u);                 // CBR
        Codec(api, CODECAPI_MeanBitRate, (uint)bitrate);
        Codec(api, CODECAPI_GOPSize, (uint)(Fps * 2));            // هر ۲ ثانیه یک کلیدفریم
        Codec(api, CODECAPI_BFrames, 0u);
        Codec(api, CODECAPI_QualityVsSpeed, 0u);                  // سریع‌ترین

        var outType = MakeType(MFVideoFormat_H264, true, bitrate);
        Check(_mft.SetOutputType(0, outType, 0), "SetOutputType");
        var inType = MakeType(MFVideoFormat_NV12, false, 0);
        Check(_mft.SetInputType(0, inType, 0), "SetInputType");
        try { Marshal.ReleaseComObject(outType); Marshal.ReleaseComObject(inType); } catch { }

        Check(_mft.GetOutputStreamInfo(0, out var info), "GetOutputStreamInfo");
        _provides = (info.dwFlags & 0x100) != 0 || (info.dwFlags & 0x200) != 0;      // PROVIDES_SAMPLES / CAN_PROVIDE_SAMPLES
        _outSize = (int)Math.Max(info.cbSize, (uint)(Width * Height));
        _nv12 = new byte[Width * Height * 3 / 2];

        _mft.ProcessMessage(0x10000000, IntPtr.Zero);             // NOTIFY_BEGIN_STREAMING
        _mft.ProcessMessage(0x10000003, IntPtr.Zero);             // NOTIFY_START_OF_STREAM
        FetchSequenceHeader();
    }

    /// <summary>SPS/PPS از نوع خروجی (اگه انکودر همون اول بده)</summary>
    private void FetchSequenceHeader()
    {
        if ((Sps != null && Pps != null) || _mft == null) return;
        try
        {
            if (_mft.GetOutputCurrentType(0, out var t) < 0 || t == null) return;
            try
            {
                if (t.GetBlobSize(ref MF_MT_MPEG_SEQUENCE_HEADER, out uint sz) >= 0 && sz > 0)
                {
                    var buf = new byte[sz];
                    if (t.GetBlob(ref MF_MT_MPEG_SEQUENCE_HEADER, buf, sz, out _) >= 0) ParseAnnexB(buf, null);
                }
            }
            finally { Marshal.ReleaseComObject(t); }
        }
        catch { }
    }

    /// <summary>مرحله‌ی ۱: BGRA (Width*Height*4) رو به NV12 تبدیل میکنه (بعد از این، آرایه‌ی ورودی دیگه لازم نیست)</summary>
    public void SetFrame(byte[] bgra) => BgraToNv12(bgra, Width, Height, _nv12);

    /// <summary>مرحله‌ی ۲: فریم آخرِ SetFrame رو انکود میکنه؛ بسته‌ها رو تو output میریزه (ممکنه صفر یا چندتا باشه)</summary>
    public void Encode(List<H264Packet> output)
    {
        if (_mft == null) throw new ObjectDisposedException("H264Encoder");

        Check(MFCreateSample(out var sample), "MFCreateSample");
        Check(MFCreateMemoryBuffer(_nv12.Length, out var buf), "MFCreateMemoryBuffer");
        try
        {
            Check(buf.Lock(out var p, out _, out _), "Lock");
            try { Marshal.Copy(_nv12, 0, p, _nv12.Length); }
            finally { buf.Unlock(); }
            Check(buf.SetCurrentLength(_nv12.Length), "SetCurrentLength");
            Check(sample.AddBuffer(buf), "AddBuffer");
            sample.SetSampleTime(_idx * _dur);
            sample.SetSampleDuration(_dur);
            _idx++;

            int hr = _mft.ProcessInput(0, sample, 0);
            if (hr == MF_E_NOTACCEPTING)
            {
                Drain(output);
                hr = _mft.ProcessInput(0, sample, 0);
            }
            Check(hr, "ProcessInput");
        }
        finally
        {
            try { Marshal.ReleaseComObject(buf); Marshal.ReleaseComObject(sample); } catch { }
        }
        Drain(output);
        if (Sps == null || Pps == null) FetchSequenceHeader();
    }

    private void Drain(List<H264Packet> output)
    {
        if (_mft == null) return;
        for (int guard = 0; guard < 16; guard++)
        {
            var odb = new MFT_OUTPUT_DATA_BUFFER { dwStreamID = 0 };
            IMFSample? mine = null; IMFMediaBuffer? mineBuf = null; IntPtr mineP = IntPtr.Zero;
            if (!_provides)
            {
                Check(MFCreateSample(out mine), "MFCreateSample(out)");
                Check(MFCreateMemoryBuffer(_outSize, out mineBuf), "MFCreateMemoryBuffer(out)");
                Check(mine.AddBuffer(mineBuf), "AddBuffer(out)");
                mineP = Marshal.GetComInterfaceForObject(mine, typeof(IMFSample));
                odb.pSample = mineP;
            }
            int hr = _mft.ProcessOutput(0, 1, ref odb, out _);
            IMFSample? got = null;
            try
            {
                if (hr == MF_E_TRANSFORM_NEED_MORE_INPUT) return;
                Check(hr, "ProcessOutput");
                got = _provides ? (IMFSample)Marshal.GetObjectForIUnknown(odb.pSample) : mine;
                if (got == null) return;

                Check(got.ConvertToContiguousBuffer(out var cb), "ConvertToContiguousBuffer");
                try
                {
                    Check(cb.Lock(out var ptr, out _, out int cur), "Lock(out)");
                    byte[] data;
                    try { data = new byte[cur]; Marshal.Copy(ptr, data, 0, cur); }
                    finally { cb.Unlock(); }
                    ParseAnnexB(data, output);
                }
                finally { Marshal.ReleaseComObject(cb); }
            }
            finally
            {
                try
                {
                    if (odb.pSample != IntPtr.Zero) Marshal.Release(odb.pSample);
                    if (odb.pEvents != IntPtr.Zero) Marshal.Release(odb.pEvents);
                    if (_provides && got != null) Marshal.ReleaseComObject(got);
                    if (mineBuf != null) Marshal.ReleaseComObject(mineBuf);
                    if (mine != null) Marshal.ReleaseComObject(mine);
                }
                catch { }
            }
        }
    }

    /// <summary>Annex-B -> AVCC. SPS/PPS رو نگه میداره و از جریان بیرون میکشه؛ کلیدفریم = وجود NAL نوع 5</summary>
    private void ParseAnnexB(byte[] d, List<H264Packet>? output)
    {
        var nals = new List<int[]>();       // {offset, length}
        int n = d.Length, start = -1, i = 0;
        while (i + 2 < n)
        {
            if (d[i] == 0 && d[i + 1] == 0 && (d[i + 2] == 1 || (d[i + 2] == 0 && i + 3 < n && d[i + 3] == 1)))
            {
                int sc = d[i + 2] == 1 ? 3 : 4;
                if (start >= 0) nals.Add(new[] { start, i - start });
                start = i + sc; i += sc;
                continue;
            }
            i++;
        }
        if (start >= 0 && start < n) nals.Add(new[] { start, n - start });

        bool key = false;
        var ms = new MemoryStream(n + 16);
        foreach (var nal in nals)
        {
            int off = nal[0], len = nal[1];
            while (len > 0 && d[off + len - 1] == 0) len--;          // صفرهای انتهایی
            if (len <= 0) continue;
            int type = d[off] & 0x1F;
            if (type == 7) { Sps = Slice(d, off, len); continue; }
            if (type == 8) { Pps = Slice(d, off, len); continue; }
            if (type == 9 || type == 6) continue;                    // AUD / SEI لازم نیست
            if (type == 5) key = true;
            ms.WriteByte((byte)(len >> 24)); ms.WriteByte((byte)(len >> 16)); ms.WriteByte((byte)(len >> 8)); ms.WriteByte((byte)len);
            ms.Write(d, off, len);
        }
        if (output != null && ms.Length > 0) output.Add(new H264Packet { Avcc = ms.ToArray(), Key = key });
    }

    private static byte[] Slice(byte[] d, int off, int len) { var r = new byte[len]; Buffer.BlockCopy(d, off, r, 0, len); return r; }

    /// <summary>BGRA -> NV12 (BT.601). ردیف‌های رنگ نصفه‌ان (۲×۲ => یک نمونه)</summary>
    private static unsafe void BgraToNv12(byte[] src, int w, int h, byte[] dst)
    {
        // چندهسته‌ای + بدون بررسی محدوده (قبلاً تک‌هسته و مدیریت‌شده بود: تو 1080p حدود ۱۰ میلی‌ثانیه فقط همین)
        int ySize = w * h;
        int pairs = h / 2;
        fixed (byte* sp0 = src, dp0 = dst)
        {
            IntPtr sP = (IntPtr)sp0, dP = (IntPtr)dp0;
            Parallel.For(0, pairs, cy =>
            {
                byte* s = (byte*)sP.ToPointer();
                byte* d = (byte*)dP.ToPointer();
                int y0 = cy * 2;
                for (int yy = y0; yy < y0 + 2; yy++)
                {
                    byte* sr = s + (long)yy * w * 4;
                    byte* dr = d + (long)yy * w;
                    for (int x = 0; x < w; x++, sr += 4)
                        dr[x] = (byte)(((66 * sr[2] + 129 * sr[1] + 25 * sr[0] + 128) >> 8) + 16);
                }
                byte* cs = s + (long)y0 * w * 4;
                byte* cd = d + ySize + (long)cy * w;
                for (int x = 0; x < w; x += 2, cs += 8, cd += 2)
                {
                    int b = cs[0], g = cs[1], r = cs[2];
                    cd[0] = (byte)(((-38 * r - 74 * g + 112 * b + 128) >> 8) + 128);
                    cd[1] = (byte)(((112 * r - 94 * g - 18 * b + 128) >> 8) + 128);
                }
            });
        }
    }

    public string CodecString()
    {
        var s = Sps;
        if (s == null || s.Length < 4) return "avc1.42E01F";
        return "avc1." + s[1].ToString("X2") + s[2].ToString("X2") + s[3].ToString("X2");
    }

    public long DurationTicks => 90000L / Math.Max(1, Fps);

    public void Dispose()
    {
        var m = _mft;
        _mft = null;
        if (m == null) return;
        try { m.ProcessMessage(0x10000001, IntPtr.Zero); m.ProcessMessage(0x00000000, IntPtr.Zero); } catch { }
        try { Marshal.ReleaseComObject(m); } catch { }
    }
}

/// <summary>fMP4 (یک فریم = یک fragment) برای MediaSource تو مرورگر</summary>
internal static class Fmp4
{
    private const uint Timescale = 90000;

    private static byte[] U8(params int[] v) { var r = new byte[v.Length]; for (int i = 0; i < v.Length; i++) r[i] = (byte)v[i]; return r; }
    private static byte[] U16(int v) => new[] { (byte)(v >> 8), (byte)v };
    private static byte[] U32(uint v) => new[] { (byte)(v >> 24), (byte)(v >> 16), (byte)(v >> 8), (byte)v };
    private static byte[] U64(ulong v) => new[] { (byte)(v >> 56), (byte)(v >> 48), (byte)(v >> 40), (byte)(v >> 32), (byte)(v >> 24), (byte)(v >> 16), (byte)(v >> 8), (byte)v };
    private static byte[] Cat(params byte[][] parts)
    {
        int n = 0; foreach (var p in parts) n += p.Length;
        var r = new byte[n]; int o = 0;
        foreach (var p in parts) { Buffer.BlockCopy(p, 0, r, o, p.Length); o += p.Length; }
        return r;
    }
    private static byte[] Box(string type, params byte[][] parts)
    {
        var body = Cat(parts);
        return Cat(U32((uint)(body.Length + 8)), System.Text.Encoding.ASCII.GetBytes(type), body);
    }
    private static byte[] Full(string type, int version, uint flags, params byte[][] parts)
        => Box(type, Cat(U32(((uint)version << 24) | (flags & 0xFFFFFF)), Cat(parts)));

    private static byte[] Matrix() => Cat(U32(0x00010000), U32(0), U32(0), U32(0), U32(0x00010000), U32(0), U32(0), U32(0), U32(0x40000000));

    /// <summary>init segment (ftyp + moov)</summary>
    public static byte[] Init(byte[] sps, byte[] pps, int w, int h)
    {
        var ftyp = Box("ftyp", System.Text.Encoding.ASCII.GetBytes("isom"), U32(0x200),
            System.Text.Encoding.ASCII.GetBytes("isom"), System.Text.Encoding.ASCII.GetBytes("iso2"),
            System.Text.Encoding.ASCII.GetBytes("avc1"), System.Text.Encoding.ASCII.GetBytes("mp41"));

        var mvhd = Full("mvhd", 0, 0, U32(0), U32(0), U32(1000), U32(0), U32(0x00010000), U16(0x0100), U16(0), new byte[8], Matrix(), new byte[24], U32(2));
        var tkhd = Full("tkhd", 0, 7, U32(0), U32(0), U32(1), U32(0), U32(0), new byte[8], U16(0), U16(0), U16(0), U16(0), Matrix(), U32((uint)w << 16), U32((uint)h << 16));
        var mdhd = Full("mdhd", 0, 0, U32(0), U32(0), U32(Timescale), U32(0), U16(0x55C4), U16(0));
        var hdlr = Full("hdlr", 0, 0, U32(0), System.Text.Encoding.ASCII.GetBytes("vide"), new byte[12], System.Text.Encoding.ASCII.GetBytes("VideoHandler\0"));
        var vmhd = Full("vmhd", 0, 1, U16(0), new byte[6]);
        var dinf = Box("dinf", Full("dref", 0, 0, U32(1), Full("url ", 0, 1)));

        var avcC = Box("avcC", U8(1, sps[1], sps[2], sps[3], 0xFF, 0xE1), U16(sps.Length), sps, U8(1), U16(pps.Length), pps);
        var avc1 = Box("avc1", new byte[6], U16(1), U16(0), U16(0), new byte[12], U16(w), U16(h), U32(0x00480000), U32(0x00480000), U32(0), U16(1), new byte[32], U16(0x0018), U16(0xFFFF), avcC);
        var stsd = Full("stsd", 0, 0, U32(1), avc1);
        var stbl = Box("stbl", stsd, Full("stts", 0, 0, U32(0)), Full("stsc", 0, 0, U32(0)), Full("stsz", 0, 0, U32(0), U32(0)), Full("stco", 0, 0, U32(0)));
        var minf = Box("minf", vmhd, dinf, stbl);
        var mdia = Box("mdia", mdhd, hdlr, minf);
        var trak = Box("trak", tkhd, mdia);
        var mvex = Box("mvex", Full("trex", 0, 0, U32(1), U32(1), U32(0), U32(0), U32(0)));
        var moov = Box("moov", mvhd, trak, mvex);
        return Cat(ftyp, moov);
    }

    /// <summary>یک فریم = moof + mdat</summary>
    public static byte[] Fragment(uint seq, ulong decodeTime, uint duration, bool key, byte[] avcc)
    {
        byte[] Build(int dataOffset)
        {
            var mfhd = Full("mfhd", 0, 0, U32(seq));
            var tfhd = Full("tfhd", 0, 0x020000, U32(1));                       // default-base-is-moof
            var tfdt = Full("tfdt", 1, 0, U64(decodeTime));
            var trun = Full("trun", 0, 0x000701, U32(1), U32((uint)dataOffset), U32(duration), U32((uint)avcc.Length), U32(key ? 0x02000000u : 0x01010000u));
            var traf = Box("traf", tfhd, tfdt, trun);
            return Box("moof", mfhd, traf);
        }
        var moof = Build(0);
        moof = Build(moof.Length + 8);
        return Cat(moof, Box("mdat", avcc));
    }
}
