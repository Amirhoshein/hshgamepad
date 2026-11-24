// HSH Gamepad | Telegram: @HSHGamepad
using System;
using System.Globalization;
using System.IO;

namespace GamepadServer;

/// <summary>تنظیمات پنجره‌ی سرور؛ تو پوشه‌ی data کنار برنامه (settings.txt) ذخیره میشه (دو خط: AutoKick و Seconds)</summary>
public sealed class AppSettings
{
    /// <summary>حذف خودکار دسته‌ی آزاد (پیش‌فرض: خاموش = هیچ وقت حذف نشن)</summary>
    public bool AutoKick { get; set; } = false;
    public int Seconds { get; set; } = 10;

    /// <summary>اجازه‌ی ماوس و کیبورد از گوشی (فقط پلیر ۱؛ پیش‌فرض روشن)</summary>
    public bool AllowInput { get; set; } = true;

    /// <summary>میکروفون مجازی همیشه تو ویندوز وصل بمونه (حتی با برنامه‌ی بسته و بعد از ریستارت)؛ خاموش = فقط وقتی گوشی (پلیر ۱) روشنش کرده</summary>
    public bool MicAlways { get; set; } = false;

    /// <summary>قسمتی از اسم دستگاه ورودی/خروجیِ درایور میکروفون مجازی (چند مورد با | جدا میشه)؛ خالی = پیش‌فرض</summary>
    public string MicIn { get; set; } = "";
    public string MicOut { get; set; } = "";

    /// <summary>کد زبان انتخاب‌شده (اسم فایل داخل پوشه‌ی lang)</summary>
    public string Language { get; set; } = "fa";

    /// <summary>مهر آخرین انتخاب زبانِ نصب‌کننده که اعمال شده (setup-language.txt)</summary>
    public string SetupStamp { get; set; } = "";

    /// <summary>خروجی صدایی که برای گوشی ضبط میشه (شناسه‌ی دستگاه ویندوز)؛ خالی = خروجی پیش‌فرض ویندوز</summary>
    public string AudioDevice { get; set; } = "";

    public bool KeyEnabled { get; set; } = false;
    public string Key { get; set; } = "";
    public bool BanEnabled { get; set; } = true;
    public int BanTries { get; set; } = AccessGuard.DefaultTries;
    public int BanMinutes { get; set; } = AccessGuard.DefaultMinutes;

    private static string FilePath =>
        Path.Combine(AppPaths.DataDir, "settings.txt");

    public static AppSettings Load()
    {
        var s = new AppSettings();
        try
        {
            if (File.Exists(FilePath))
            {
                foreach (var raw in File.ReadAllLines(FilePath))
                {
                    int eq = raw.IndexOf('=');
                    if (eq < 0) continue;
                    string k = raw.Substring(0, eq).Trim();
                    string v = raw.Substring(eq + 1).Trim();
                    if (k.Equals("AutoKick", StringComparison.OrdinalIgnoreCase))
                        s.AutoKick = v.Equals("true", StringComparison.OrdinalIgnoreCase) || v == "1";
                    else if (k.Equals("AllowInput", StringComparison.OrdinalIgnoreCase))
                        s.AllowInput = v.Equals("true", StringComparison.OrdinalIgnoreCase) || v == "1";
                    else if (k.Equals("Language", StringComparison.OrdinalIgnoreCase) && v.Length > 0)
                        s.Language = v;
                    else if (k.Equals("SetupStamp", StringComparison.OrdinalIgnoreCase))
                        s.SetupStamp = v;
                    else if (k.Equals("Seconds", StringComparison.OrdinalIgnoreCase) &&
                             int.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out int n))
                        s.Seconds = n;
                    else if (k.Equals("AudioDevice", StringComparison.OrdinalIgnoreCase))
                        s.AudioDevice = v;
                    else if (k.Equals("MicAlways", StringComparison.OrdinalIgnoreCase))
                        s.MicAlways = Flag(v);
                    else if (k.Equals("MicIn", StringComparison.OrdinalIgnoreCase))
                        s.MicIn = v;
                    else if (k.Equals("MicOut", StringComparison.OrdinalIgnoreCase))
                        s.MicOut = v;
                    else if (k.Equals("KeyEnabled", StringComparison.OrdinalIgnoreCase))
                        s.KeyEnabled = Flag(v);
                    else if (k.Equals("Key", StringComparison.OrdinalIgnoreCase))
                        s.Key = v;
                    else if (k.Equals("BanEnabled", StringComparison.OrdinalIgnoreCase))
                        s.BanEnabled = Flag(v);
                    else if (k.Equals("BanTries", StringComparison.OrdinalIgnoreCase) &&
                             int.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out int tries))
                        s.BanTries = tries;
                    else if (k.Equals("BanMinutes", StringComparison.OrdinalIgnoreCase) &&
                             int.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out int minutes))
                        s.BanMinutes = minutes;
                }
            }
        }
        catch { }
        s.Seconds = Math.Min(3600, Math.Max(1, s.Seconds));
        s.Normalize();
        s.ApplySetupChoice();
        return s;
    }

    private static bool Flag(string v)
    {
        return v.Equals("true", StringComparison.OrdinalIgnoreCase) || v == "1";
    }

    public void Normalize()
    {
        Key = AccessGuard.Clean(Key);
        if (Key.Length == 0) Key = AccessGuard.RandomKey();
        BanTries = Math.Min(1000, Math.Max(1, BanTries));
        BanMinutes = Math.Min(1440, Math.Max(1, BanMinutes));
    }

    /// <summary>زبانی که تو نصب‌کننده انتخاب شده (اگه جدید باشه) به‌عنوان زبان برنامه ذخیره میشه</summary>
    private void ApplySetupChoice()
    {
        if (SetupChoice.Read(out string code, out string stamp) && stamp != SetupStamp)
        {
            Language = code;
            SetupStamp = stamp;
            Save();
        }
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.WriteAllText(FilePath,
                "AutoKick=" + (AutoKick ? "true" : "false") + "\r\n" +
                "Seconds=" + Seconds.ToString(CultureInfo.InvariantCulture) + "\r\n" +
                "AllowInput=" + (AllowInput ? "true" : "false") + "\r\n" +
                "Language=" + Language + "\r\n" +
                "SetupStamp=" + SetupStamp + "\r\n" +
                "AudioDevice=" + AudioDevice + "\r\n" +
                "MicAlways=" + (MicAlways ? "true" : "false") + "\r\n" +
                "MicIn=" + MicIn + "\r\n" +
                "MicOut=" + MicOut + "\r\n" +
                "KeyEnabled=" + (KeyEnabled ? "true" : "false") + "\r\n" +
                "Key=" + Key + "\r\n" +
                "BanEnabled=" + (BanEnabled ? "true" : "false") + "\r\n" +
                "BanTries=" + BanTries.ToString(CultureInfo.InvariantCulture) + "\r\n" +
                "BanMinutes=" + BanMinutes.ToString(CultureInfo.InvariantCulture) + "\r\n");
        }
        catch { }
    }
}
