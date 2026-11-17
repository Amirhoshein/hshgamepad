using System;
using System.Globalization;
using System.IO;

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
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GamepadServer", "settings.txt");

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
                "KeyEnabled=" + (KeyEnabled ? "true" : "false") + "\r\n" +
                "Key=" + Key + "\r\n" +
                "BanEnabled=" + (BanEnabled ? "true" : "false") + "\r\n" +
                "BanTries=" + BanTries.ToString(CultureInfo.InvariantCulture) + "\r\n" +
                "BanMinutes=" + BanMinutes.ToString(CultureInfo.InvariantCulture) + "\r\n");
        }
        catch { }
    }
}
