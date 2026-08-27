using System.Diagnostics;
using System.Runtime.InteropServices;

namespace DesktopScroll;

public sealed class ScrollingService
{
    private const int CursorTargetDwellMs = 30;
    private const int CursorRestoreDelayMs = 20;

    private readonly AppStateMachine _stateMachine;
    private readonly SettingsService _settingsService;
    private readonly object _scrollLock = new();
    private Point _target;

    public ScrollingService(AppStateMachine stateMachine, SettingsService settingsService)
    {
        _stateMachine = stateMachine;
        _settingsService = settingsService;
    }

    public void BeginScrollMode(Point target)
    {
        _target = target;
        _stateMachine.EnterScrollMode(target);
        RuntimeTrace.Write($"ScrollMode entered. Target=({target.X},{target.Y})");
        TraceTargetProbe(target);
    }

    public void StartScrollKey(Keys key)
    {
        RuntimeTrace.Write($"Scroll key received. Key={key}");
        StartScrollSession(key);
    }

    public void StopScrollKey(Keys key)
    {
        RuntimeTrace.Write($"Scroll key released. Key={key}");
    }

    public void ExitScrollMode()
    {
        _stateMachine.ExitScrollMode();
        RuntimeTrace.Write("ScrollMode exited.");
    }

    private void StartScrollSession(Keys key)
    {
        if (_stateMachine.CurrentMode != AppMode.ScrollMode)
        {
            RuntimeTrace.Write($"Scroll rejected. Mode={_stateMachine.CurrentMode}, Key={key}");
            return;
        }

        var settings = _settingsService.Load();
        var direction = key switch
        {
            Keys.W => 1,
            Keys.S => -1,
            Keys.A => -1,
            Keys.D => 1,
            _ => 0
        };

        if (direction == 0)
        {
            RuntimeTrace.Write($"Scroll rejected. Unsupported key={key}");
            return;
        }

        var verticalDelta = key is Keys.W or Keys.S
            ? (short)(direction * Math.Max(1, settings.Scrolling.VerticalStep))
            : (short)0;
        var horizontalDelta = key is Keys.A or Keys.D
            ? (short)(direction * Math.Max(1, settings.Scrolling.HorizontalStep))
            : (short)0;

        lock (_scrollLock)
        {
            SendWheelViaCursorEmulation(_target, horizontalDelta, verticalDelta);
            RuntimeTrace.Write($"Scroll tick dispatched. Key={key}, Target=({_target.X},{_target.Y}), DeltaX={horizontalDelta}, DeltaY={verticalDelta}");
        }
    }

    public static void SendWheelAtPoint(Point target, short deltaX, short deltaY)
    {
        SendWheelViaCursorEmulation(target, deltaX, deltaY);
    }

    private static void SendWheelViaCursorEmulation(Point target, short deltaX, short deltaY)
    {
        if (deltaX == 0 && deltaY == 0)
        {
            RuntimeTrace.Write("Cursor wheel emulation skipped: no wheel delta.");
            return;
        }

        if (!NativeMethods.GetCursorPos(out var originalPoint))
        {
            RuntimeTrace.Write($"Cursor wheel emulation failed: GetCursorPos error={Marshal.GetLastWin32Error()}");
            return;
        }

        var virtualLeft = NativeMethods.GetSystemMetrics(NativeMethods.SM_XVIRTUALSCREEN);
        var virtualTop = NativeMethods.GetSystemMetrics(NativeMethods.SM_YVIRTUALSCREEN);
        var virtualWidth = NativeMethods.GetSystemMetrics(NativeMethods.SM_CXVIRTUALSCREEN);
        var virtualHeight = NativeMethods.GetSystemMetrics(NativeMethods.SM_CYVIRTUALSCREEN);
        if (virtualWidth <= 0 || virtualHeight <= 0)
        {
            RuntimeTrace.Write($"Cursor wheel emulation failed: invalid virtual desktop. Left={virtualLeft}, Top={virtualTop}, Width={virtualWidth}, Height={virtualHeight}");
            return;
        }

        var original = new Point(originalPoint.X, originalPoint.Y);
        var movedToTarget = NativeMethods.SetCursorPos(target.X, target.Y);
        if (!movedToTarget)
        {
            RuntimeTrace.Write($"Cursor wheel emulation failed: SetCursorPos target=({target.X},{target.Y}) error={Marshal.GetLastWin32Error()}");
            return;
        }

        Thread.Sleep(CursorTargetDwellMs);

        var inputs = new List<NativeMethods.INPUT>();

        if (deltaY != 0)
        {
            inputs.Add(NativeMethods.CreateMouseWheelInput(NativeMethods.MOUSEEVENTF_WHEEL, deltaY));
        }

        if (deltaX != 0)
        {
            inputs.Add(NativeMethods.CreateMouseWheelInput(NativeMethods.MOUSEEVENTF_HWHEEL, deltaX));
        }

        var inputSize = Marshal.SizeOf<NativeMethods.INPUT>();
        var sent = NativeMethods.SendInput((uint)inputs.Count, inputs.ToArray(), inputSize);
        Thread.Sleep(CursorRestoreDelayMs);

        var restored = NativeMethods.SetCursorPos(original.X, original.Y);
        RuntimeTrace.Write($"Cursor wheel emulation sent. Target=({target.X},{target.Y}), Original=({original.X},{original.Y}), Virtual=({virtualLeft},{virtualTop},{virtualWidth},{virtualHeight}), DeltaX={deltaX}, DeltaY={deltaY}, Sent={sent}/{inputs.Count}, InputSize={inputSize}, Restored={restored}, Error={Marshal.GetLastWin32Error()}");
    }

    private static void TraceTargetProbe(Point target)
    {
        try
        {
            var hWnd = ResolveTargetWindow(target);
            if (hWnd == IntPtr.Zero)
            {
                RuntimeTrace.Write($"Target probe: no window at ({target.X},{target.Y}).");
                return;
            }

            NativeMethods.GetWindowThreadProcessId(hWnd, out var processId);
            var processName = SafeProcessName(processId);
            var className = SafeWindowClass(hWnd);
            var title = SafeWindowText(hWnd);
            RuntimeTrace.Write($"Target probe: Handle=0x{hWnd.ToInt64():X}, Process={processName}({processId}), Class='{className}', Title='{title}'");
        }
        catch (Exception ex)
        {
            RuntimeTrace.Write($"Target probe failed. {ex.GetType().Name}: {ex.Message}");
        }
    }

    private static string SafeProcessName(int processId)
    {
        try
        {
            using var process = Process.GetProcessById(processId);
            return process.ProcessName;
        }
        catch
        {
            return "unknown";
        }
    }

    private static string SafeWindowClass(IntPtr hWnd)
    {
        var builder = new System.Text.StringBuilder(256);
        return NativeMethods.GetClassName(hWnd, builder, builder.Capacity) > 0 ? builder.ToString() : string.Empty;
    }

    private static string SafeWindowText(IntPtr hWnd)
    {
        var builder = new System.Text.StringBuilder(512);
        return NativeMethods.GetWindowText(hWnd, builder, builder.Capacity) > 0 ? builder.ToString() : string.Empty;
    }

    private static IntPtr WindowFromPoint(Point point) => NativeMethods.WindowFromPoint(new NativeMethods.POINT { X = point.X, Y = point.Y });

    private static IntPtr ResolveTargetWindow(Point point)
    {
        var topWindow = WindowFromPoint(point);
        if (topWindow == IntPtr.Zero)
        {
            return IntPtr.Zero;
        }

        topWindow = SkipOwnWindows(topWindow);
        if (topWindow == IntPtr.Zero)
        {
            return IntPtr.Zero;
        }

        var clientPoint = new NativeMethods.POINT { X = point.X, Y = point.Y };
        NativeMethods.ScreenToClient(topWindow, ref clientPoint);
        var child = NativeMethods.ChildWindowFromPointEx(topWindow, clientPoint, NativeMethods.CWP_SKIPINVISIBLE | NativeMethods.CWP_SKIPDISABLED | NativeMethods.CWP_SKIPTRANSPARENT);
        if (child != IntPtr.Zero)
        {
            child = SkipOwnWindows(child);
            if (child != IntPtr.Zero)
            {
                return child;
            }
        }

        return topWindow;
    }

    private static IntPtr SkipOwnWindows(IntPtr hWnd)
    {
        var current = hWnd;
        while (current != IntPtr.Zero)
        {
            NativeMethods.GetWindowThreadProcessId(current, out var processId);
            if (processId != Environment.ProcessId)
            {
                return current;
            }

            current = NativeMethods.GetWindow(current, NativeMethods.GW_HWNDNEXT);
        }

        return IntPtr.Zero;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr WindowFromPoint(NativeMethods.POINT point);

    private static class NativeMethods
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
        }

        public const uint CWP_SKIPINVISIBLE = 0x0001;
        public const uint CWP_SKIPDISABLED = 0x0002;
        public const uint CWP_SKIPTRANSPARENT = 0x0004;
        public const uint GW_HWNDNEXT = 2;
        public const int INPUT_MOUSE = 0;
        public const uint MOUSEEVENTF_MOVE = 0x0001;
        public const uint MOUSEEVENTF_WHEEL = 0x0800;
        public const uint MOUSEEVENTF_HWHEEL = 0x01000;
        public const uint MOUSEEVENTF_ABSOLUTE = 0x8000;
        public const uint MOUSEEVENTF_VIRTUALDESK = 0x4000;
        public const int SM_XVIRTUALSCREEN = 76;
        public const int SM_YVIRTUALSCREEN = 77;
        public const int SM_CXVIRTUALSCREEN = 78;
        public const int SM_CYVIRTUALSCREEN = 79;

        [DllImport("user32.dll")]
        public static extern IntPtr WindowFromPoint(POINT point);

        [DllImport("user32.dll")]
        public static extern bool ScreenToClient(IntPtr hWnd, ref POINT lpPoint);

        [DllImport("user32.dll")]
        public static extern IntPtr ChildWindowFromPointEx(IntPtr hwnd, POINT pt, uint flags);

        [DllImport("user32.dll")]
        public static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool SetCursorPos(int x, int y);

        [DllImport("user32.dll")]
        public static extern int GetSystemMetrics(int nIndex);

        [DllImport("user32.dll")]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern int GetClassName(IntPtr hWnd, System.Text.StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder lpString, int nMaxCount);

        [StructLayout(LayoutKind.Sequential)]
        public struct INPUT
        {
            public int type;
            public InputUnion union;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct InputUnion
        {
            [FieldOffset(0)]
            public MOUSEINPUT mi;

            [FieldOffset(0)]
            public KEYBDINPUT ki;

            [FieldOffset(0)]
            public HARDWAREINPUT hi;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct HARDWAREINPUT
        {
            public uint uMsg;
            public ushort wParamL;
            public ushort wParamH;
        }

        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint SendInput(uint cInputs, INPUT[] pInputs, int cbSize);

        public static INPUT CreateMouseWheelInput(uint flags, short delta)
        {
            return new INPUT
            {
                type = INPUT_MOUSE,
                union = new InputUnion
                {
                    mi = new MOUSEINPUT
                    {
                        mouseData = unchecked((uint)(int)delta),
                        dwFlags = flags
                    }
                }
            };
        }

        public static INPUT CreateAbsoluteMoveInput(Point point, int virtualLeft, int virtualTop, int virtualWidth, int virtualHeight)
        {
            return new INPUT
            {
                type = INPUT_MOUSE,
                union = new InputUnion
                {
                    mi = new MOUSEINPUT
                    {
                        dx = ToAbsoluteCoordinate(point.X, virtualLeft, virtualWidth),
                        dy = ToAbsoluteCoordinate(point.Y, virtualTop, virtualHeight),
                        dwFlags = MOUSEEVENTF_MOVE | MOUSEEVENTF_ABSOLUTE | MOUSEEVENTF_VIRTUALDESK
                    }
                }
            };
        }

        private static int ToAbsoluteCoordinate(int coordinate, int virtualOrigin, int virtualSize)
        {
            return (int)Math.Round((coordinate - virtualOrigin) * 65535d / Math.Max(1, virtualSize - 1));
        }
    }
}
