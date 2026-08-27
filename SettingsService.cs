using System.Text.Json;

namespace DesktopScroll;

public sealed class SettingsService
{
    private readonly string _settingsPath;

    public SettingsService(string? customPath = null)
    {
        var folder = customPath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "DesktopScroll");

        Directory.CreateDirectory(folder);
        _settingsPath = Path.Combine(folder, "settings.json");
    }

    public Settings Load()
    {
        if (!File.Exists(_settingsPath))
        {
            var settings = new Settings();
            Save(settings);
            return settings;
        }

        try
        {
            var json = File.ReadAllText(_settingsPath);
            var settings = JsonSerializer.Deserialize<Settings>(json) ?? new Settings();
            return settings;
        }
        catch
        {
            var settings = new Settings();
            Save(settings);
            return settings;
        }
    }

    public void Save(Settings settings)
    {
        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        File.WriteAllText(_settingsPath, json);
    }

    public static Settings Clone(Settings settings)
    {
        var json = JsonSerializer.Serialize(settings);
        return JsonSerializer.Deserialize<Settings>(json) ?? new Settings();
    }

    public static Point? GetLastTarget(Settings settings)
    {
        if (settings.LastTargetX is null || settings.LastTargetY is null)
        {
            return null;
        }

        return new Point(settings.LastTargetX.Value, settings.LastTargetY.Value);
    }

    public static void SetLastTarget(Settings settings, Point target)
    {
        settings.LastTargetX = target.X;
        settings.LastTargetY = target.Y;
    }
}
