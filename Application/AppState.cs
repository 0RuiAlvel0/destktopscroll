namespace DesktopScroll;

public sealed class AppState
{
    public AppMode Mode { get; set; } = AppMode.Idle;

    public Point? LastTarget { get; set; }

    public bool IsOverlayVisible { get; set; }

    public bool IsScrollActive { get; set; }
}
