using System.Drawing;

namespace DesktopScroll;

public sealed class OverlayService : IDisposable
{
    private readonly List<OverlayWindow> _overlayForms = new();
    private readonly List<GridCell> _cells = new();
    private readonly SettingsService _settingsService;
    private string _activePrefix = string.Empty;
    private int? _selectedCellIndex;

    public int LabelLength { get; private set; }

    public OverlayService(SettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    public void ShowOverlays()
    {
        CloseOverlays();

        var settings = _settingsService.Load();
        var monitors = new MonitorService().GetMonitors();
        var totalCellCount = Math.Max(1, monitors.Count * settings.Grid.Rows * settings.Grid.Columns);
        var labels = LabelGenerator.GenerateLabels(totalCellCount, settings.Grid.MinLabelLength, settings.Grid.MaxLabelLength);
        LabelLength = labels.FirstOrDefault()?.Length ?? settings.Grid.MinLabelLength;

        var index = 0;

        foreach (var monitor in monitors)
        {
            var form = new OverlayWindow(monitor.Bounds);

            var cellWidth = Math.Max(1, monitor.Bounds.Width / settings.Grid.Columns);
            var cellHeight = Math.Max(1, monitor.Bounds.Height / settings.Grid.Rows);

            var monitorCells = new List<GridCell>();

            for (var row = 0; row < settings.Grid.Rows; row++)
            {
                for (var col = 0; col < settings.Grid.Columns; col++)
                {
                    var labelText = labels[index % labels.Length];
                    var cellRect = new Rectangle(col * cellWidth, row * cellHeight, cellWidth, cellHeight);
                    var center = new Point(
                        monitor.Bounds.Left + cellRect.Left + (cellRect.Width / 2),
                        monitor.Bounds.Top + cellRect.Top + (cellRect.Height / 2));
                    var model = new GridCell
                    {
                        Index = index,
                        Label = labelText,
                        Bounds = new Rectangle(monitor.Bounds.Left + cellRect.Left, monitor.Bounds.Top + cellRect.Top, cellRect.Width, cellRect.Height),
                        Center = center
                    };

                    monitorCells.Add(model);
                    _cells.Add(model);
                    index++;
                }
            }

            form.SetCells(monitorCells);
            form.Show();
            _overlayForms.Add(form);
        }
    }

    public IReadOnlyList<GridCell> ApplyPrefix(string prefix)
    {
        var normalized = prefix.Trim().ToLowerInvariant();
        _activePrefix = normalized;
        var candidates = _cells
            .Where(c => c.Label.StartsWith(normalized, StringComparison.Ordinal))
            .OrderBy(c => c.Index)
            .ToList();

        foreach (var overlay in _overlayForms)
        {
            overlay.SetFilter(_activePrefix, _selectedCellIndex);
        }

        return candidates;
    }

    public bool TryResolveLabel(string label, out GridCell cell)
    {
        var normalized = label.Trim().ToLowerInvariant();
        var match = _cells.FirstOrDefault(c => string.Equals(c.Label, normalized, StringComparison.Ordinal));
        if (match is null)
        {
            cell = default!;
            return false;
        }

        cell = match;
        return true;
    }

    public void HighlightCell(GridCell? cell)
    {
        _selectedCellIndex = cell?.Index;
        foreach (var overlay in _overlayForms)
        {
            overlay.SetFilter(_activePrefix, _selectedCellIndex);
        }
    }

    public void CloseOverlays()
    {
        foreach (var form in _overlayForms)
        {
            form.Close();
            form.Dispose();
        }

        _overlayForms.Clear();
        _cells.Clear();
        _activePrefix = string.Empty;
        _selectedCellIndex = null;
    }

    public void Dispose()
    {
        CloseOverlays();
    }

}
