namespace DesktopScroll;

internal static class Program
{
    private const string SingleInstanceMutexName = "DesktopScroll.SingleInstance";
    private const string LaunchAfterSetupArgument = "--launch-after-setup";
    private const string PostInstallStartupPromptArgument = "--post-install-startup-prompt";
    private const string StartupDisableArgument = "--startup-disable";
    private const string StartupEnableArgument = "--startup-enable";

    [STAThread]
    private static void Main(string[] args)
    {
        try
        {
            ApplicationConfiguration.Initialize();

            if (args.Contains(StartupEnableArgument, StringComparer.OrdinalIgnoreCase))
            {
                var startup = new Startup.StartupRegistrationManager();
                startup.Enable();
                return;
            }

            if (args.Contains(StartupDisableArgument, StringComparer.OrdinalIgnoreCase))
            {
                var startup = new Startup.StartupRegistrationManager();
                startup.Disable();
                return;
            }

            if (args.Contains(PostInstallStartupPromptArgument, StringComparer.OrdinalIgnoreCase)
                && !PromptForStartupRegistration())
            {
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

    private static bool PromptForStartupRegistration()
    {
        var startupManager = new Startup.StartupRegistrationManager();
        var settingsService = new SettingsService();
        var settings = settingsService.Load();

        var result = MessageBox.Show(
            "Start DesktopScroll automatically when you sign in to Windows?",
            "DesktopScroll Startup",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question,
            MessageBoxDefaultButton.Button1);

        settings.StartWithWindows = result == DialogResult.Yes;
        if (settings.StartWithWindows)
        {
            startupManager.Enable();
        }
        else
        {
            startupManager.Disable();
        }

        settingsService.Save(settings);
        return Environment.GetCommandLineArgs().Contains(LaunchAfterSetupArgument, StringComparer.OrdinalIgnoreCase);
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