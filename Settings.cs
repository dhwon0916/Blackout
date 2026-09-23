using System.Text.Json;
using Microsoft.Win32;

namespace Blackout;

public sealed class MonitorSetting
{
    public string Device { get; set; } = "";
    public string Hotkey { get; set; } = "";
}

public sealed class Settings
{
    public List<MonitorSetting> Monitors { get; set; } = [];
    public string RestoreHotkey { get; set; } = "Ctrl+Alt+R";
    public static string FilePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Blackout", "settings.json");

    public static Settings Load(string? path = null)
    {
        path ??= FilePath;
        if (!File.Exists(path)) return new();
        var value = JsonSerializer.Deserialize<Settings>(File.ReadAllText(path)) ?? throw new InvalidDataException("Settings are empty.");
        if (value.Monitors == null || value.Monitors.Any(m => m == null || string.IsNullOrEmpty(m.Device)) || string.IsNullOrWhiteSpace(value.RestoreHotkey))
            throw new InvalidDataException("Settings contain invalid values.");
        return value;
    }

    public void Save(string? path = null)
    {
        path ??= FilePath;
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        string temp = path + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
        File.Move(temp, path, true);
    }

    public void Reconcile(Screen[] screens)
    {
        for (int i = 0; i < screens.Length; i++)
        {
            if (Monitors.Any(m => m.Device == screens[i].DeviceName)) continue;
            string candidate = i < 9 ? $"Ctrl+Alt+{i + 1}" : "";
            if (Monitors.Any(m => m.Hotkey == candidate) || RestoreHotkey == candidate) candidate = "";
            Monitors.Add(new() { Device = screens[i].DeviceName, Hotkey = candidate });
        }
    }
}

internal static class Startup
{
    private const string Key = @"Software\Microsoft\Windows\CurrentVersion\Run";
    public static bool Enabled
    {
        get { using var key = Registry.CurrentUser.OpenSubKey(Key); return key?.GetValue("Blackout") is string; }
    }
    public static void Set(bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(Key);
        if (enabled) key.SetValue("Blackout", $"\"{Environment.ProcessPath}\" --startup");
        else key.DeleteValue("Blackout", false);
    }
}
