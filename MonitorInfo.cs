using System.Drawing;

namespace DesktopScroll;

public sealed class MonitorInfo
{
    public required string DeviceName { get; init; }

    public required Rectangle Bounds { get; init; }

    public bool IsPrimary { get; init; }
}
