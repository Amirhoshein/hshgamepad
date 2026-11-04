// HSH Gamepad | Telegram: @HSHGamepad
using System;
using System.IO;
using System.Text.Json;

namespace GamepadServer;

/// <summary>تنظیمات پنجره‌ی سرور؛ تو %AppData%\GamepadServer\settings.json ذخیره میشه</summary>
public sealed class AppSettings
{
    /// <summary>حذف خودکار دسته‌ی آزاد (پیش‌فرض: خاموش = هیچ وقت حذف نشن)</summary>
    public bool AutoKick { get; set; } = false;
    public int Seconds { get; set; } = 10;

    /// <summary>اجازه‌ی ماوس و کیبورد از گوشی (فقط پلیر ۱؛ پیش‌فرض روشن)</summary>
    public bool AllowInput { get; set; } = true;

    private static string FilePath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GamepadServer", "settings.json");

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                var s = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath)) ?? new AppSettings();
                s.Seconds = Math.Clamp(s.Seconds, 1, 3600);
                return s;
            }
        }
        catch { }
        return new AppSettings();
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
