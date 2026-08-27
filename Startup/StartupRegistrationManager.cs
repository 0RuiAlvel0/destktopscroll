using System.Reflection;
using Microsoft.Win32;

namespace DesktopScroll.Startup;

public sealed class StartupRegistrationManager
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string RunValueName = "DesktopScroll";

    public bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
        var value = key?.GetValue(RunValueName);
        return value is string command && !string.IsNullOrWhiteSpace(command);
    }

    public void Enable()
    {
        var command = BuildLauncherCommand();
        using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true)
            ?? throw new InvalidOperationException("Could not open the Run registry key.");
        key.SetValue(RunValueName, command, RegistryValueKind.String);
    }

    public void Disable()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
        key?.DeleteValue(RunValueName, false);
    }

    private static string BuildLauncherCommand()
    {
        var processPath = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(processPath) || !File.Exists(processPath))
        {
            throw new InvalidOperationException("Could not resolve the current process path for startup registration.");
        }

        var exePath = processPath;
        if (Path.GetFileNameWithoutExtension(exePath).Equals("dotnet", StringComparison.OrdinalIgnoreCase))
        {
            var entryAssemblyName = Assembly.GetEntryAssembly()?.GetName().Name;
            if (!string.IsNullOrWhiteSpace(entryAssemblyName))
            {
                var candidateExe = Path.Combine(AppContext.BaseDirectory, $"{entryAssemblyName}.exe");
                if (File.Exists(candidateExe))
                {
                    exePath = candidateExe;
                }
            }
        }

        return $"\"{exePath}\" --tray";
    }
}
