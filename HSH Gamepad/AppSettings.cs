// HSH Gamepad | Telegram: @HSHGamepad
using System;
using System.IO;
using System.Text.Json;

namespace GamepadServer;

/// <summary>تنظیمات پنجره‌ی سرور؛ تو پوشه‌ی data کنار برنامه (settings.json) ذخیره میشه</summary>
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
        Path.Combine(AppPaths.DataDir, "settings.json");

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                var s = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath)) ?? new AppSettings();
                s.Seconds = Math.Clamp(s.Seconds, 1, 3600);
                if (string.IsNullOrWhiteSpace(s.Language)) s.Language = "fa";
                s.Normalize();
                s.ApplySetupChoice();
                return s;
            }
        }
        catch { }
        var fresh = new AppSettings();
        fresh.Normalize();
        fresh.ApplySetupChoice();
        return fresh;
    }

    public void Normalize()
    {
        Key = AccessGuard.Clean(Key);
        if (Key.Length == 0) Key = AccessGuard.RandomKey();
        BanTries = Math.Clamp(BanTries, 1, 1000);
        BanMinutes = Math.Clamp(BanMinutes, 1, 1440);
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
            File.WriteAllText(FilePath, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch { }
    }
}
