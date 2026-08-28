using System.Runtime.InteropServices;

namespace DesktopScroll;

public sealed class TrayApplicationContext : ApplicationContext
{
    private readonly NotifyIcon _notifyIcon;
    private readonly SettingsService _settingsService;
    private readonly Startup.StartupRegistrationManager _startupManager;
    private readonly AppStateMachine _stateMachine;
    private readonly OverlayService _overlayService;
    private readonly CursorService _cursorService;
    private readonly TargetSelectionService _targetSelectionService;
    private readonly ScrollingService _scrollingService;
    private readonly HotkeyService _hotkeyService;
    private readonly GlobalKeyboardHookService _keyboardHook;
    private readonly KeyBindingResolver _keyBindingResolver;
    private readonly Control _uiInvoker;
    private Settings _settings;
    private bool _registeredHotkeysAvailable;
    private Keys _activationKey = Keys.Enter;
    private Keys _resumeKey = Keys.Enter;
    private HotkeyModifiers _activationModifiers = HotkeyModifiers.Win;
    private HotkeyModifiers _resumeModifiers = HotkeyModifiers.Win | HotkeyModifiers.Control;
    private Keys _scrollUpKey;
    private Keys _scrollDownKey;
    private Keys _scrollLeftKey;
    private Keys _scrollRightKey;
    private readonly HashSet<Keys> _pressedScrollKeys = new();
    private readonly object _pressedScrollKeysLock = new();

    public TrayApplicationContext()
    {
        _settingsService = new SettingsService();
        _startupManager = new Startup.StartupRegistrationManager();
        _stateMachine = new AppStateMachine();
        _overlayService = new OverlayService(_settingsService);
        _cursorService = new CursorService(_settingsService, _stateMachine);
        _targetSelectionService = new TargetSelectionService(_overlayService, _stateMachine);
        _scrollingService = new ScrollingService(_stateMachine, _settingsService);
        _hotkeyService = new HotkeyService();
        _keyboardHook = new GlobalKeyboardHookService();
        _keyBindingResolver = new KeyBindingResolver();
        _uiInvoker = new Control();
        _uiInvoker.CreateControl();
        _settings = _settingsService.Load();
        _settings.StartWithWindows = _startupManager.IsEnabled();
        _settingsService.Save(_settings);

        var persistedTarget = SettingsService.GetLastTarget(_settings);
        if (persistedTarget is not null)
        {
            _stateMachine.SetLastTarget(persistedTarget.Value);
        }

        ResolveScrollKeys();

        _notifyIcon = new NotifyIcon
        {
            Icon = ResolveTrayIcon(),
            Visible = true,
            Text = "DesktopScroll"
        };

        var contextMenu = new ContextMenuStrip();
        contextMenu.Items.Add("Enable", null, (_, _) => ToggleEnabled(true));
        contextMenu.Items.Add("Disable", null, (_, _) => ToggleEnabled(false));
        contextMenu.Items.Add("Settings", null, (_, _) => OpenSettings());
        contextMenu.Items.Add("About", null, (_, _) => ShowAbout());
        contextMenu.Items.Add("Exit", null, (_, _) => Exit());

        _notifyIcon.ContextMenuStrip = contextMenu;
        _notifyIcon.DoubleClick += (_, _) => OpenSettings();

        RegisterHotkeys();
        _keyboardHook.KeyDown += HandleKeyboardHookKeyDown;
        _keyboardHook.KeyUp += HandleKeyboardHookKeyUp;
        _keyboardHook.Start();

        UpdateNotifyIconState();
    }

    private void RegisterHotkeys()
    {
        _hotkeyService.UnregisterAll();
        _registeredHotkeysAvailable = false;

        if (_keyBindingResolver.TryParseHotkey(_settings.Hotkeys.Activate, out var activateKey, out var activateModifiers))
        {
            _activationKey = activateKey;
            _activationModifiers = activateModifiers;
            TryRegisterHotkey(activateKey, activateModifiers, EnterTargetSelectionMode);
        }

        if (_keyBindingResolver.TryParseHotkey(_settings.Hotkeys.Resume, out var resumeKey, out var resumeModifiers))
        {
            _resumeKey = resumeKey;
            _resumeModifiers = resumeModifiers;
            TryRegisterHotkey(resumeKey, resumeModifiers, ResumeLastTarget);
        }
    }

    private void EnterTargetSelectionMode()
    {
        if (!IsEnabled())
        {
            return;
        }

        _pressedScrollKeys.Clear();
        _scrollingService.ExitScrollMode();
        _targetSelectionService.EnterSelectionMode();
        UpdateNotifyIconState();
    }

    private void ResumeLastTarget()
    {
        if (!IsEnabled())
        {
            return;
        }

        var target = _stateMachine.GetLastTarget();
        if (target is null)
        {
            return;
        }

        _pressedScrollKeys.Clear();
        _scrollingService.BeginScrollMode(target.Value);
        _cursorService.ShowCursorDot(target.Value);
        UpdateNotifyIconState();
    }

    private bool IsEnabled()
    {
        return _settings.Enabled;
    }

    private bool HandleKeyboardHookKeyDown(Keys key)
    {
        if (_stateMachine.CurrentMode == AppMode.ScrollMode
            && key != Keys.Escape
            && !HasNonShiftSystemModifierPressed()
            && TryMapToScrollDirection(key, out var directionKey))
        {
            lock (_pressedScrollKeysLock)
            {
                if (!_pressedScrollKeys.Add(directionKey))
                {
                    RuntimeTrace.Write($"Scroll key repeat ignored. Key={directionKey}");
                    return true;
                }
            }

            RunOnUiThread(() => ExecuteScrollKey(directionKey));
            return true;
        }

        return RunOnUiThread(() => OnGlobalKeyDown(key));
    }

    private bool HandleKeyboardHookKeyUp(Keys key)
    {
        if (_stateMachine.CurrentMode == AppMode.ScrollMode
            && !HasNonShiftSystemModifierPressed()
            && TryMapToScrollDirection(key, out var directionKey))
        {
            lock (_pressedScrollKeysLock)
            {
                _pressedScrollKeys.Remove(directionKey);
            }

            RunOnUiThread(() => _scrollingService.StopScrollKey(directionKey));
            return true;
        }

        return RunOnUiThread(() => OnGlobalKeyUp(key));
    }

    private bool OnGlobalKeyDown(Keys key)
    {
        if (!_registeredHotkeysAvailable && HandleHotkeysFromHook(key))
        {
            return true;
        }

        if (_stateMachine.CurrentMode == AppMode.TargetSelection)
        {
            if (HasNonShiftSystemModifierPressed())
            {
                return false;
            }

            if (key == Keys.Escape)
            {
                _targetSelectionService.ExitSelectionMode();
                UpdateNotifyIconState();
                return true;
            }

            if (_targetSelectionService.HandleKeyDown(key, out var target) && target is not null)
            {
                _scrollingService.BeginScrollMode(target.Value);
                _cursorService.ShowCursorDot(target.Value);
                PersistLastTarget(target.Value);
                UpdateNotifyIconState();
                return true;
            }

            return IsSelectionKey(key);
        }

        if (_stateMachine.CurrentMode != AppMode.ScrollMode)
        {
            return false;
        }

        if (key == Keys.Escape)
        {
            _pressedScrollKeys.Clear();
            _scrollingService.ExitScrollMode();
            _cursorService.HideCursorDot();
            UpdateNotifyIconState();
            return true;
        }

        if (HasNonShiftSystemModifierPressed())
        {
            return true;
        }

        if (TryMapToScrollDirection(key, out var directionKey))
        {
            lock (_pressedScrollKeysLock)
            {
                if (!_pressedScrollKeys.Add(directionKey))
                {
                    RuntimeTrace.Write($"Scroll key repeat ignored. Key={directionKey}");
                    return true;
                }
            }

            ExecuteScrollKey(directionKey);
            return true;
        }

        return true;
    }

    private bool HandleHotkeysFromHook(Keys key)
    {
        if (key == _resumeKey && MatchesModifiers(_resumeModifiers))
        {
            ResumeLastTarget();
            return true;
        }

        if (key == _activationKey && MatchesModifiers(_activationModifiers))
        {
            EnterTargetSelectionMode();
            return true;
        }

        return false;
    }

    private bool OnGlobalKeyUp(Keys key)
    {
        if (_stateMachine.CurrentMode != AppMode.ScrollMode)
        {
            return false;
        }

        if (IsSystemModifierKey(key))
        {
            return false;
        }

        if (HasNonShiftSystemModifierPressed())
        {
            return true;
        }

        if (TryMapToScrollDirection(key, out var directionKey))
        {
            lock (_pressedScrollKeysLock)
            {
                _pressedScrollKeys.Remove(directionKey);
            }

            _scrollingService.StopScrollKey(directionKey);
            return true;
        }

        return true;
    }

    private void ExecuteScrollKey(Keys directionKey)
    {
        var target = _stateMachine.GetLastTarget();
        _cursorService.HideCursorDot();
        try
        {
            _scrollingService.StartScrollKey(directionKey);
        }
        finally
        {
            if (_stateMachine.CurrentMode == AppMode.ScrollMode && target is not null)
            {
                _cursorService.ShowCursorDot(target.Value);
            }
        }
    }

    private void ToggleEnabled(bool enabled)
    {
        _settings.Enabled = enabled;
        _settings.StartWithWindows = _startupManager.IsEnabled();
        _settingsService.Save(_settings);

        if (!enabled)
        {
            _pressedScrollKeys.Clear();
            _targetSelectionService.ExitSelectionMode();
            _scrollingService.ExitScrollMode();
            _cursorService.HideCursorDot();
        }

        UpdateNotifyIconState();
    }

    private void OpenSettings()
    {
        using var form = new SettingsForm(_settings);
        if (form.ShowDialog() != DialogResult.OK)
        {
            return;
        }

        _settings = form.EditedSettings;
        _settingsService.Save(_settings);

        ApplyStartupSetting();
        ResolveScrollKeys();
        RegisterHotkeys();

        var lastTarget = SettingsService.GetLastTarget(_settings);
        if (lastTarget is not null)
        {
            _stateMachine.SetLastTarget(lastTarget.Value);
        }

        UpdateNotifyIconState();
    }

    private void ShowAbout()
    {
        MessageBox.Show("DesktopScroll\n\nKeyboard-driven screen scrolling utility.", "About DesktopScroll", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void UpdateNotifyIconState()
    {
        _notifyIcon.Text = _stateMachine.CurrentMode switch
        {
            AppMode.TargetSelection => "DesktopScroll (Selecting Target)",
            AppMode.ScrollMode => "DesktopScroll (Scroll Mode)",
            _ => _settings.Enabled ? "DesktopScroll (Enabled)" : "DesktopScroll (Disabled)"
        };
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _keyboardHook.Stop();
            _keyboardHook.Dispose();
            _hotkeyService.Dispose();
            _overlayService.Dispose();
            _cursorService.HideCursorDot();
            _uiInvoker.Dispose();
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
        }

        base.Dispose(disposing);
    }

    private void Exit()
    {
        Application.Exit();
    }

    private void ResolveScrollKeys()
    {
        _scrollUpKey = ResolveOrDefault(_settings.ScrollKeys.Up, Keys.W);
        _scrollDownKey = ResolveOrDefault(_settings.ScrollKeys.Down, Keys.S);
        _scrollLeftKey = ResolveOrDefault(_settings.ScrollKeys.Left, Keys.A);
        _scrollRightKey = ResolveOrDefault(_settings.ScrollKeys.Right, Keys.D);
    }

    private bool IsScrollKey(Keys key)
    {
        return key == _scrollUpKey
            || key == _scrollDownKey
            || key == _scrollLeftKey
            || key == _scrollRightKey
            || key is Keys.Up or Keys.Down or Keys.Left or Keys.Right;
    }

    private bool TryMapToScrollDirection(Keys key, out Keys directionKey)
    {
        directionKey = Keys.None;
        if (!IsScrollKey(key))
        {
            return false;
        }

        directionKey = key switch
        {
            Keys.Up => Keys.W,
            Keys.Down => Keys.S,
            Keys.Left => Keys.A,
            Keys.Right => Keys.D,
            var k when k == _scrollUpKey => Keys.W,
            var k when k == _scrollDownKey => Keys.S,
            var k when k == _scrollLeftKey => Keys.A,
            _ => Keys.D
        };

        return true;
    }

    private Keys ResolveOrDefault(string value, Keys fallback)
    {
        return _keyBindingResolver.TryParseSingleKey(value, out var key) ? key : fallback;
    }

    private void PersistLastTarget(Point target)
    {
        SettingsService.SetLastTarget(_settings, target);
        _settingsService.Save(_settings);
    }

    private void ApplyStartupSetting()
    {
        if (_settings.StartWithWindows)
        {
            _startupManager.Enable();
        }
        else
        {
            _startupManager.Disable();
        }
    }

    private void RunOnUiThread(Action action)
    {
        if (_uiInvoker.IsDisposed)
        {
            return;
        }

        if (_uiInvoker.InvokeRequired)
        {
            _uiInvoker.BeginInvoke(action);
            return;
        }

        action();
    }

    private bool RunOnUiThread(Func<bool> action)
    {
        if (_uiInvoker.IsDisposed)
        {
            return false;
        }

        if (_uiInvoker.InvokeRequired)
        {
            return (bool)_uiInvoker.Invoke(action);
        }

        return action();
    }

    private static bool IsSelectionKey(Keys key)
    {
        if (key is Keys.Left or Keys.Right or Keys.Up or Keys.Down or Keys.Escape)
        {
            return true;
        }

        return key is >= Keys.A and <= Keys.Z;
    }

    private static bool IsSystemModifierKey(Keys key)
    {
        return key is Keys.ControlKey or Keys.LControlKey or Keys.RControlKey
            or Keys.ShiftKey or Keys.LShiftKey or Keys.RShiftKey
            or Keys.Menu or Keys.LMenu or Keys.RMenu
            or Keys.LWin or Keys.RWin;
    }

    private static Icon ResolveTrayIcon()
    {
        var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "DesktopTieLogo.ico");
        if (File.Exists(iconPath))
        {
            return new Icon(iconPath);
        }

        return SystemIcons.Application;
    }

    private void TryRegisterHotkey(Keys key, HotkeyModifiers modifiers, Action handler)
    {
        try
        {
            _hotkeyService.RegisterHotkey(key, modifiers, handler);
            _registeredHotkeysAvailable = true;
        }
        catch
        {
            // fall back to hook-based detection when registration is blocked by OS-reserved combinations
        }
    }

    private static bool MatchesModifiers(HotkeyModifiers modifiers)
    {
        return HasModifier(modifiers, HotkeyModifiers.Control, Keys.ControlKey)
            && HasModifier(modifiers, HotkeyModifiers.Shift, Keys.ShiftKey)
            && HasModifier(modifiers, HotkeyModifiers.Alt, Keys.Menu)
            && HasModifier(modifiers, HotkeyModifiers.Win, Keys.LWin, Keys.RWin);
    }

    private static bool HasModifier(HotkeyModifiers expected, HotkeyModifiers flag, params Keys[] keys)
    {
        var required = expected.HasFlag(flag);
        var pressed = keys.Any(IsKeyDown);
        return required == pressed;
    }

    private static bool IsKeyDown(Keys key)
    {
        return (GetAsyncKeyState((int)key) & 0x8000) != 0;
    }

    private static bool HasNonShiftSystemModifierPressed()
    {
        return IsKeyDown(Keys.LWin)
            || IsKeyDown(Keys.RWin)
            || IsKeyDown(Keys.ControlKey)
            || IsKeyDown(Keys.Menu);
    }

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);
}
