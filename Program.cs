namespace DesktopScroll;

internal static class Program
{
    private const string SingleInstanceMutexName = "DesktopScroll.SingleInstance";

    [STAThread]
    private static void Main(string[] args)
    {
        try
        {
            ApplicationConfiguration.Initialize();

            if (args.Contains("--startup-enable", StringComparer.OrdinalIgnoreCase))
            {
                var startup = new Startup.StartupRegistrationManager();
                startup.Enable();
                return;
            }

            if (args.Contains("--startup-disable", StringComparer.OrdinalIgnoreCase))
            {
                var startup = new Startup.StartupRegistrationManager();
                startup.Disable();
                return;
            }

            using var singleInstanceMutex = new Mutex(initiallyOwned: true, SingleInstanceMutexName, out var createdNew);
            if (!createdNew)
            {
                MessageBox.Show(
                    "DesktopScroll is already running. Check the notification area (system tray).",
                    "DesktopScroll",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            Application.Run(new TrayApplicationContext());
        }
        catch (Exception ex)
        {
            WriteCrashLog(ex, args);
            throw;
        }
    }

    private static void WriteCrashLog(Exception ex, string[] args)
    {
        try
        {
            var logDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DesktopScroll", "logs");
            Directory.CreateDirectory(logDir);
            var logPath = Path.Combine(logDir, "startup-crash.log");
            var payload = $"[{DateTime.UtcNow:O}] Startup crash\nArgs: {string.Join(" ", args)}\n{ex}\n\n";
            File.AppendAllText(logPath, payload);
        }
        catch
        {
            // avoid secondary failures during crash logging
        }
    }
}