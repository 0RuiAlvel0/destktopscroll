using System.Drawing;
using System.Runtime.InteropServices;

namespace DesktopScroll;

public sealed class CursorService
{
    private readonly SettingsService _settingsService;
    private readonly AppStateMachine _stateMachine;
    private DotForm? _dotForm;

    public CursorService(SettingsService settingsService, AppStateMachine stateMachine)
    {
        _settingsService = settingsService;
        _stateMachine = stateMachine;
    }

    public Point? GetLastTarget() => _stateMachine.GetLastTarget();

    public void SetLastTarget(Point target)
    {
        _stateMachine.SetLastTarget(target);
    }

    public void ShowCursorDot(Point location)
    {
        var settings = _settingsService.Load();
        if (!settings.Visuals.ShowCursorDot)
        {
            HideCursorDot();
            return;
        }

        var size = settings.Visuals.CursorDotSize;
        if (_dotForm is null || _dotForm.IsDisposed)
        {
            _dotForm = new DotForm();
            _dotForm.Show();
        }

        _dotForm.UpdateAppearance(location, size, settings.Visuals.CursorDotOpacity);
    }

    public void HideCursorDot()
    {
        if (_dotForm is null)
        {
            return;
        }

        _dotForm.Close();
        _dotForm.Dispose();
        _dotForm = null;
    }

    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_LAYERED = 0x80000;
    private const int WS_EX_TRANSPARENT = 0x20;
    private const int WS_EX_NOACTIVATE = 0x08000000;
    private const int LWA_ALPHA = 0x2;

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll")]
    private static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);

    private sealed class DotForm : Form
    {
        private const int MinimumDotSize = 4;
        private const double HaloScale = 5.0;
        private int _dotSize = 8;

        public DotForm()
        {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            TopMost = true;
            BackColor = Color.Black;
            TransparencyKey = Color.Black;
            AllowTransparency = true;
            DoubleBuffered = true;
        }

        protected override bool ShowWithoutActivation => true;

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            var exStyle = GetWindowLong(Handle, GWL_EXSTYLE);
            SetWindowLong(Handle, GWL_EXSTYLE, exStyle | WS_EX_LAYERED | WS_EX_TRANSPARENT | WS_EX_NOACTIVATE);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var haloInset = (Width - _dotSize) / 2f;
            var dotBounds = new RectangleF(haloInset, haloInset, _dotSize - 1, _dotSize - 1);

            using var haloBrush = new SolidBrush(Color.FromArgb(55, 25, 255, 25));
            using var brush = new SolidBrush(Color.FromArgb(220, 25, 255, 25));
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            e.Graphics.FillEllipse(haloBrush, 0, 0, Width - 1, Height - 1);
            e.Graphics.FillEllipse(brush, dotBounds);
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

        public void UpdateAppearance(Point center, int size, double opacity)
        {
            _dotSize = Math.Max(MinimumDotSize, size);
            var haloSize = (int)Math.Ceiling(_dotSize * HaloScale);
            Size = new Size(haloSize, haloSize);
            Location = new Point(center.X - (Width / 2), center.Y - (Height / 2));
            Opacity = Math.Clamp(opacity, 0.1, 1.0);
            SetLayeredWindowAttributes(Handle, 0, (byte)(Opacity * 255), LWA_ALPHA);
            Invalidate();
        }

        private const int WM_NCHITTEST = 0x0084;
        private const int HTTRANSPARENT = -1;
    }
}
