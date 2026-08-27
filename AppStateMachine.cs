namespace DesktopScroll;

public sealed class AppStateMachine
{
    private readonly AppState _state = new();

    public AppMode CurrentMode => _state.Mode;

    public bool IsTargetSelectionVisible => _state.IsOverlayVisible && _state.Mode == AppMode.TargetSelection;

    public void EnterTargetSelection()
    {
        _state.Mode = AppMode.TargetSelection;
        _state.IsOverlayVisible = true;
        _state.IsScrollActive = false;
    }

    public void EnterScrollMode(Point target)
    {
        _state.Mode = AppMode.ScrollMode;
        _state.LastTarget = target;
        _state.IsOverlayVisible = false;
        _state.IsScrollActive = true;
    }

    public void ExitScrollMode()
    {
        _state.Mode = AppMode.Idle;
        _state.IsOverlayVisible = false;
        _state.IsScrollActive = false;
    }

    public Point? GetLastTarget() => _state.LastTarget;

    public void SetLastTarget(Point target)
    {
        _state.LastTarget = target;
    }
}
