using System.Drawing;

namespace DesktopScroll;

public sealed class GridCell
{
    public required int Index { get; init; }

    public required string Label { get; init; }

    public required Rectangle Bounds { get; init; }

    public required Point Center { get; init; }
}
