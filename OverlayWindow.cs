using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace DesktopScroll;

public sealed class OverlayWindow : Form
{
    private readonly List<GridCell> _cells = new();
    private string _prefix = string.Empty;
    private int? _selectedIndex;

    public OverlayWindow(Rectangle bounds)
    {
        Bounds = bounds;
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        ShowInTaskbar = false;
        TopMost = true;
        TopLevel = true;
        BackColor = Color.Black;
        Opacity = 0.72;
        DoubleBuffered = true;
    }

    protected override bool ShowWithoutActivation => true;

    public void SetCells(IEnumerable<GridCell> cells)
    {
        _cells.Clear();
        _cells.AddRange(cells);
        Invalidate();
    }

    public void SetFilter(string prefix, int? selectedIndex)
    {
        _prefix = prefix;
        _selectedIndex = selectedIndex;
        Invalidate();
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        MakeWindowClickThrough(Handle);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        var normalizedPrefix = _prefix.Trim().ToLowerInvariant();
        using var cellPen = new Pen(Color.FromArgb(130, 140, 150), 1f);
        using var areaFill = new SolidBrush(Color.FromArgb(35, 70, 80));
        using var labelFill = new SolidBrush(Color.FromArgb(30, 170, 70));
        using var labelTextBrush = new SolidBrush(Color.White);
        using var selectedFill = new SolidBrush(Color.FromArgb(50, 210, 80));
        using var font = new Font("Segoe UI", 11f, FontStyle.Bold);

        foreach (var cell in _cells)
        {
            if (!string.IsNullOrEmpty(normalizedPrefix) && !cell.Label.StartsWith(normalizedPrefix, StringComparison.Ordinal))
            {
                continue;
            }

            var localBounds = new Rectangle(
                cell.Bounds.Left - Bounds.Left,
                cell.Bounds.Top - Bounds.Top,
                cell.Bounds.Width,
                cell.Bounds.Height);

            e.Graphics.FillRectangle(areaFill, localBounds);
            e.Graphics.DrawRectangle(cellPen, localBounds);

            if (_selectedIndex.HasValue && _selectedIndex.Value == cell.Index)
            {
                e.Graphics.FillRectangle(selectedFill, localBounds);
                e.Graphics.DrawRectangle(Pens.White, localBounds);
            }

            var textSize = e.Graphics.MeasureString(cell.Label, font);
            var badgePaddingX = 8;
            var badgePaddingY = 4;
            var badgeRect = new Rectangle(
                localBounds.Left + (localBounds.Width / 2) - ((int)textSize.Width / 2) - badgePaddingX,
                localBounds.Top + (localBounds.Height / 2) - ((int)textSize.Height / 2) - badgePaddingY,
                (int)textSize.Width + (badgePaddingX * 2),
                (int)textSize.Height + (badgePaddingY * 2));

            e.Graphics.FillRectangle(labelFill, badgeRect);
            e.Graphics.DrawRectangle(Pens.White, badgeRect);
            e.Graphics.DrawString(cell.Label, font, labelTextBrush, badgeRect.Left + badgePaddingX, badgeRect.Top + badgePaddingY);
        }
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WM_NCHITTEST)
        {
            m.Result = (IntPtr)HTTRANSPARENT;
            return;
        }

        base.WndProc(ref m);
    }

    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_LAYERED = 0x00080000;
    private const int WS_EX_TRANSPARENT = 0x00000020;
    private const int WS_EX_NOACTIVATE = 0x08000000;
    private const int WM_NCHITTEST = 0x0084;
    private const int HTTRANSPARENT = -1;

    private static void MakeWindowClickThrough(IntPtr hwnd)
    {
        var exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
        SetWindowLong(hwnd, GWL_EXSTYLE, exStyle | WS_EX_LAYERED | WS_EX_TRANSPARENT | WS_EX_NOACTIVATE);
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
}
