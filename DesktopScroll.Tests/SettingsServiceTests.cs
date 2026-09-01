namespace DesktopScroll.Tests;

public sealed class SettingsServiceTests : IDisposable
{
    private readonly string _settingsDirectory = Path.Combine(Path.GetTempPath(), "DesktopScroll.Tests", Guid.NewGuid().ToString("N"));

    [Fact]
    public void Load_CreatesDefaultSettingsWhenFileDoesNotExist()
    {
        var service = new SettingsService(_settingsDirectory);

        var settings = service.Load();

        Assert.True(settings.Enabled);
        Assert.Equal("Win+Enter", settings.Hotkeys.Activate);
        Assert.True(File.Exists(Path.Combine(_settingsDirectory, "settings.json")));
    }

    [Fact]
    public void SaveAndLoad_RoundTripsNestedSettingsAndLastTarget()
    {
        var service = new SettingsService(_settingsDirectory);
        var settings = new Settings
        {
            Enabled = false,
            Hotkeys = new HotkeySettings { Activate = "Alt+F8", Resume = "Ctrl+F8" },
            Grid = new GridSettings { Rows = 5, Columns = 7, MinLabelLength = 2, MaxLabelLength = 4 },
            Scrolling = new ScrollSettings { VerticalStep = 240, HorizontalStep = 360, RepeatDelayMs = 40, RepeatIntervalMs = 50 },
            ScrollKeys = new ScrollKeySettings { Up = "I", Down = "K", Left = "J", Right = "L" },
            Visuals = new VisualSettings { ShowCursorDot = false, CursorDotSize = 12, CursorDotOpacity = 0.5 }
        };
        SettingsService.SetLastTarget(settings, new Point(-150, 480));

        service.Save(settings);
        var loaded = service.Load();

        Assert.False(loaded.Enabled);
        Assert.Equal("Alt+F8", loaded.Hotkeys.Activate);
        Assert.Equal(7, loaded.Grid.Columns);
        Assert.Equal(360, loaded.Scrolling.HorizontalStep);
        Assert.Equal("L", loaded.ScrollKeys.Right);
        Assert.Equal(0.5, loaded.Visuals.CursorDotOpacity);
        Assert.Equal(new Point(-150, 480), SettingsService.GetLastTarget(loaded));
    }

    [Fact]
    public void Load_ReplacesInvalidJsonWithDefaults()
    {
        Directory.CreateDirectory(_settingsDirectory);
        File.WriteAllText(Path.Combine(_settingsDirectory, "settings.json"), "not valid json");
        var service = new SettingsService(_settingsDirectory);

        var settings = service.Load();

        Assert.True(settings.Enabled);
        Assert.Equal("Win+Enter", settings.Hotkeys.Activate);
    }

    [Fact]
    public void Clone_CreatesIndependentCopy()
    {
        var source = new Settings();
        var clone = SettingsService.Clone(source);

        clone.Grid.Rows = 12;
        clone.Hotkeys.Activate = "Alt+Enter";

        Assert.Equal(8, source.Grid.Rows);
        Assert.Equal("Win+Enter", source.Hotkeys.Activate);
    }

    [Fact]
    public void GetLastTarget_ReturnsNullWhenCoordinateIsMissing()
    {
        var settings = new Settings { LastTargetX = 100 };

        Assert.Null(SettingsService.GetLastTarget(settings));
    }

    public void Dispose()
    {
        if (Directory.Exists(_settingsDirectory))
        {
            Directory.Delete(_settingsDirectory, recursive: true);
        }
    }
}