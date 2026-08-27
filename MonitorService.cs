using System.Drawing;

namespace DesktopScroll;

public sealed class MonitorService
{
    public IReadOnlyList<MonitorInfo> GetMonitors()
    {
        return Screen.AllScreens
            .Select(screen => new MonitorInfo
            {
                DeviceName = screen.DeviceName,
                Bounds = screen.Bounds,
                IsPrimary = screen.Primary
            })
            .ToList();
    }
}
