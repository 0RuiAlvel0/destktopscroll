namespace DesktopScroll;

public sealed class Settings
{
    public bool StartWithWindows { get; set; } = false;

    public bool Enabled { get; set; } = true;

    public int? LastTargetX { get; set; }

    public int? LastTargetY { get; set; }

    public HotkeySettings Hotkeys { get; set; } = new();

    public GridSettings Grid { get; set; } = new();

    public ScrollSettings Scrolling { get; set; } = new();

    public ScrollKeySettings ScrollKeys { get; set; } = new();

    public VisualSettings Visuals { get; set; } = new();
}

public sealed class HotkeySettings
{
    public string Activate { get; set; } = "Win+Enter";

    public string Resume { get; set; } = "Ctrl+Win+Enter";
}

public sealed class GridSettings
{
    public int Rows { get; set; } = 8;

    public int Columns { get; set; } = 16;

    public int MinLabelLength { get; set; } = 2;

    public int MaxLabelLength { get; set; } = 3;
}

public sealed class ScrollSettings
{
    public int VerticalStep { get; set; } = 120;

    public int HorizontalStep { get; set; } = 120;

    public int RepeatDelayMs { get; set; } = 30;

    public int RepeatIntervalMs { get; set; } = 30;
}

public sealed class ScrollKeySettings
{
    public string Up { get; set; } = "W";

    public string Down { get; set; } = "S";

    public string Left { get; set; } = "A";

    public string Right { get; set; } = "D";
}

public sealed class VisualSettings
{
    public bool ShowCursorDot { get; set; } = true;

    public int CursorDotSize { get; set; } = 8;

    public double CursorDotOpacity { get; set; } = 0.75;
}
