# DesktopScroll

DesktopScroll is a keyboard-driven Windows utility for scrolling at a chosen point on one or more screens.

> **Note:** The Repeat Delay and Repeat Interval settings are saved with the application configuration. They are intended to control the timing of repeated scrolling actions, but repeat-timing behavior has not been implemented yet. Changing these values currently has no effect.

## Quick Usage

1. Start DesktopScroll. It runs in the notification area (system tray).
2. Press `Win+Enter` to enter target-selection mode.
3. A labeled grid appears across your monitors. Type the letters shown in the area where you want to scroll.
4. When the label is complete, DesktopScroll selects that point and enters scroll mode. The selected area is marked with a cursor dot when that option is enabled.
5. Use the configured scroll keys to scroll at the selected point. The default keys are `W`, `A`, `S`, and `D`.
6. Press `Esc` to leave scroll mode.

You can press `Ctrl+Win+Enter` later to resume scrolling at the last selected point.

## Current Status

There is currently no installer. An installer is planned for a future release.

For now, run DesktopScroll from the source code with the .NET SDK installed.

## Requirements

- Windows
- .NET 8 SDK
- A Windows Forms-compatible desktop environment

## Build and Run

Open PowerShell in the repository folder:

```powershell
dotnet restore .\DesktopScroll.csproj
dotnet build .\DesktopScroll.csproj -c Release -nologo
dotnet run --project .\DesktopScroll.csproj -c Release -nologo
```

The application is a tray application, so it may not display a normal console window. Look for the DesktopScroll icon in the notification area.

To build a self-contained publish output without an installer:

```powershell
dotnet publish .\DesktopScroll.csproj -c Release -r win-x64 --self-contained true -o .\artifacts\publish
```

Run the published application with:

```powershell
.\artifacts\publish\DesktopScroll.exe
```

## Default Keyboard Shortcuts

| Shortcut or key | Action |
|---|---|
| `Win+Enter` | Open the labeled screen grid and select a target point |
| `Ctrl+Win+Enter` | Resume scroll mode at the last selected point |
| `W` | Scroll up |
| `S` | Scroll down |
| `A` | Scroll left |
| `D` | Scroll right |
| `Esc` | Exit target-selection mode or scroll mode |
| Arrow keys | Move among matching targets while selecting, when available |

The scroll keys can also be changed in Settings. The arrow keys remain available for directional scrolling and target navigation.

## Notification-Area Menu

Right-click the DesktopScroll icon in the notification area to open the menu.

- **Enable**: Enables DesktopScroll so its global shortcuts and scrolling behavior are active.
- **Disable**: Disables DesktopScroll and exits any active selection or scroll mode. The application remains in the notification area.
- **Settings**: Opens the configuration window. Double-clicking the tray icon opens this window too.
- **About**: Shows a short description of DesktopScroll.
- **Exit**: Closes DesktopScroll and removes its tray icon.

The tray tooltip indicates whether DesktopScroll is enabled, disabled, selecting a target, or in scroll mode.

## Settings

Changes are saved to the application settings file and applied when you press **Save**. Press **Cancel** to close the window without applying changes.

### General

- **Enabled**: Turns the utility on or off.
- **Start with Windows**: Registers DesktopScroll to start automatically when you sign in to Windows.

### Hotkeys

- **Activation Hotkey**: The global shortcut used to open the labeled target-selection grid. Default: `Win+Enter`.
- **Resume Hotkey**: The global shortcut used to resume scrolling at the last selected point. Default: `Ctrl+Win+Enter`.

### Scroll Keys

- **Scroll Up Key**: Default `W`.
- **Scroll Down Key**: Default `S`.
- **Scroll Left Key**: Default `A`.
- **Scroll Right Key**: Default `D`.

### Grid

- **Grid Rows**: Number of rows shown on each monitor. Default: `8`.
- **Grid Columns**: Number of columns shown on each monitor. Default: `16`.

The grid labels identify the screen areas. Type a label to select its center point.

### Scrolling

- **Vertical Step**: Amount of vertical scrolling sent for each scroll action. Default: `120`.
- **Horizontal Step**: Amount of horizontal scrolling sent for each scroll action. Default: `120`.
- **Repeat Delay (ms)**: Stores the delay value intended for repeated scroll input. Default: `30` milliseconds. The current scrolling engine does not use this value yet.
- **Repeat Interval (ms)**: Stores the interval value intended between repeated scroll actions. Default: `30` milliseconds. The current scrolling engine does not use this value yet.

### Cursor Dot

- **Show Cursor Dot**: Shows or hides the marker at the selected scroll point. Default: enabled.
- **Dot Size**: Sets the marker size. Default: `8`.
- **Dot Opacity (%)**: Sets the marker opacity. Default: `75%`.

## Startup Command Options

The application also supports these command-line options:

```powershell
.\DesktopScroll.exe --startup-enable
.\DesktopScroll.exe --startup-disable
```

They enable or disable the Windows startup registration directly.

## Project Layout

```text
Application/       Application lifecycle and orchestration
Configuration/     Settings models and persistence
Features/          Target selection, overlays, labels, and scrolling
Infrastructure/    Windows and system integration services
Models/            Core application models
UI/                Windows Forms UI
Startup/           Windows startup registration
Assets/            Application assets
Installer/         Planned WiX installer project
```
