using System.Reflection;
using Microsoft.Win32;

namespace DesktopScroll.Startup;

public sealed class StartupRegistrationManager
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string StartupApprovedRunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run";
    private const string RunValueName = "DesktopScroll";
    private static readonly byte[] StartupApprovedEnabled = [0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00];
    private static readonly byte[] StartupApprovedDisabled = [0x03, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00];

    public bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
        var value = key?.GetValue(RunValueName);
        if (value is not string command || string.IsNullOrWhiteSpace(command))
        {
            return false;
        }

        using var approvedKey = Registry.CurrentUser.OpenSubKey(StartupApprovedRunKeyPath, writable: false);
        var approval = approvedKey?.GetValue(RunValueName) as byte[];
        return approval is not { Length: > 0 } || approval[0] != StartupApprovedDisabled[0];
    }

    public void Enable()
    {
        SetRunValue();
        SetStartupApprovedValue(StartupApprovedEnabled);
    }

    public void Disable()
    {
        SetRunValue();
        SetStartupApprovedValue(StartupApprovedDisabled);
    }

    private static void SetRunValue()
    {
        var command = BuildLauncherCommand();
        using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true)
            ?? throw new InvalidOperationException("Could not open the Run registry key.");
        key.SetValue(RunValueName, command, RegistryValueKind.String);
    }

    private static void SetStartupApprovedValue(byte[] value)
    {
        using var key = Registry.CurrentUser.CreateSubKey(StartupApprovedRunKeyPath, writable: true)
            ?? throw new InvalidOperationException("Could not open the StartupApproved registry key.");
        key.SetValue(RunValueName, value, RegistryValueKind.Binary);
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
