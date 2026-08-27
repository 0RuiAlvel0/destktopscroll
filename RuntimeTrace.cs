namespace DesktopScroll;

public static class RuntimeTrace
{
    private static readonly object SyncRoot = new();
    private static readonly string LogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "DesktopScroll",
        "logs",
        "scroll-trace.log");

    public static void Write(string message)
    {
        try
        {
            lock (SyncRoot)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(LogPath)!);
                File.AppendAllText(LogPath, $"[{DateTime.UtcNow:O}] {message}{Environment.NewLine}");
            }
        }
        catch
        {
            // Diagnostic logging must never interfere with scrolling.
        }
    }
}
