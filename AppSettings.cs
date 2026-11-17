using System;
using System.IO;
using System.Text.Json;

namespace GamepadServer;

public sealed class AppSettings
{
    public bool AutoKick { get; set; } = false;
    public int Seconds { get; set; } = 10;

    public bool AllowInput { get; set; } = true;

    public string Language { get; set; } = "fa";

    public string SetupStamp { get; set; } = "";

    public bool KeyEnabled { get; set; } = false;
    public string Key { get; set; } = "";
    public bool BanEnabled { get; set; } = true;
    public int BanTries { get; set; } = AccessGuard.DefaultTries;
    public int BanMinutes { get; set; } = AccessGuard.DefaultMinutes;

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
