using System.Runtime.InteropServices;

namespace DesktopScroll;

public enum HotkeyModifiers : uint
{
    None = 0,
    Alt = 0x0001,
    Control = 0x0002,
    Shift = 0x0004,
    Win = 0x0008
}

public sealed class HotkeyService : IDisposable
{
    private readonly Dictionary<int, Action> _handlers = new();
    private readonly HashSet<int> _registered = new();
    private readonly HotkeyWindow _window;
    private int _nextId;

    public HotkeyService()
    {
        _window = new HotkeyWindow(this);
        _window.CreateHandle(new CreateParams
        {
            Caption = "DesktopScrollHotkeyWindow",
            X = 0,
            Y = 0,
            Width = 0,
            Height = 0,
            Style = 0
        });
    }

    public void RegisterHotkey(Keys key, HotkeyModifiers modifiers, Action handler)
    {
        var id = ++_nextId;
        if (!RegisterHotKey(_window.Handle, id, (uint)modifiers, (uint)key))
        {
            throw new InvalidOperationException($"Failed to register hotkey for {modifiers}+{key}.");
        }

        _handlers[id] = handler;
        _registered.Add(id);
    }

    public void UnregisterAll()
    {
        foreach (var id in _registered)
        {
            UnregisterHotKey(_window.Handle, id);
        }

        _registered.Clear();
        _handlers.Clear();
    }

    private void ProcessMessage(Message message)
    {
        if (message.Msg != 0x0312)
        {
            return;
        }

        var id = message.WParam.ToInt32();
        if (_handlers.TryGetValue(id, out var handler))
        {
            handler();
        }
    }

    public void Dispose()
    {
        UnregisterAll();
        _window.DestroyHandle();
    }

    private sealed class HotkeyWindow : NativeWindow
    {
        private readonly HotkeyService _service;

        public HotkeyWindow(HotkeyService service)
        {
            _service = service;
        }

        protected override void WndProc(ref Message m)
        {
            _service.ProcessMessage(m);
            base.WndProc(ref m);
        }
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
}
