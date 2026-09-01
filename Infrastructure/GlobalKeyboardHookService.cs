using System.Diagnostics;
using System.Runtime.InteropServices;

namespace DesktopScroll;

public sealed class GlobalKeyboardHookService : IDisposable
{
    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_KEYUP = 0x0101;
    private const int WM_SYSKEYDOWN = 0x0104;
    private const int WM_SYSKEYUP = 0x0105;
    private const uint LLKHF_INJECTED = 0x00000010;

    private readonly HookProc _hookProc;
    private IntPtr _hookHandle;

    public event Func<Keys, bool>? KeyDown;

    public event Func<Keys, bool>? KeyUp;

    public GlobalKeyboardHookService()
    {
        _hookProc = HookCallback;
    }

    public void Start()
    {
        if (_hookHandle != IntPtr.Zero)
        {
            return;
        }

        var moduleHandle = GetCurrentModuleHandle();
        _hookHandle = SetWindowsHookEx(WH_KEYBOARD_LL, _hookProc, moduleHandle, 0);
        if (_hookHandle == IntPtr.Zero)
        {
            var error = Marshal.GetLastWin32Error();
            throw new InvalidOperationException($"Failed to install global keyboard hook. Win32Error={error}");
        }
    }

    public void Stop()
    {
        if (_hookHandle == IntPtr.Zero)
        {
            return;
        }

        UnhookWindowsHookEx(_hookHandle);
        _hookHandle = IntPtr.Zero;
    }

    public void Dispose()
    {
        Stop();
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        try 
        {
            if (nCode >= 0)
            {
                var message = wParam.ToInt32();
                var data = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
                var key = (Keys)data.vkCode;
                var handled = false;

                if ((data.flags & LLKHF_INJECTED) != 0)
                {
                    return CallNextHookEx(_hookHandle, nCode, wParam, lParam);
                }

                if (message == WM_KEYDOWN || message == WM_SYSKEYDOWN)
                {
                    handled = InvokeHandlers(KeyDown, key);
                }
                else if (message == WM_KEYUP || message == WM_SYSKEYUP)
                {
                    handled = InvokeHandlers(KeyUp, key);
                }

                if (handled)
                {
                    return (IntPtr)1;
                }
            }
        }
        catch (Exception ex)
        {
            RuntimeTrace.Write($"Error in global keyboard hook: {ex}");
        }

        return CallNextHookEx(_hookHandle, nCode, wParam, lParam);
    }

    private delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    private struct KBDLLHOOKSTRUCT
    {
        public uint vkCode;
        public uint scanCode;
        public uint flags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);

    private static bool InvokeHandlers(Func<Keys, bool>? handlers, Keys key)
    {
        if (handlers is null)
        {
            return false;
        }

        foreach (var @delegate in handlers.GetInvocationList())
        {
            if (@delegate is Func<Keys, bool> callback && callback(key))
            {
                return true;
            }
        }

        return false;
    }

    private static IntPtr GetCurrentModuleHandle()
    {
        using var process = Environment.ProcessId > 0 ? Process.GetCurrentProcess() : null;
        var moduleName = process?.MainModule?.ModuleName;
        return string.IsNullOrWhiteSpace(moduleName) ? IntPtr.Zero : GetModuleHandle(moduleName);
    }
}
