using BepInEx;

namespace MarketingAutomation.Persistence;

internal static class PersistenceDiagnostics
{
    // BepInEx's normal session log is replaced on restart. Keep IO failures in
    // a separate append-only mod log so an intermittent load can be investigated.
    internal static void Record(string message, Exception exception)
    {
        try
        {
            var path = Path.Combine(Paths.BepInExRootPath, "MarketingAutomation.IO.log");
            File.AppendAllText(path, $"{DateTime.UtcNow:O} {message}{Environment.NewLine}{exception}{Environment.NewLine}");
        }
        catch (Exception logError)
        {
            MarketingAutomationPlugin.Logger.LogWarning($"Unable to retain Marketing IO diagnostics: {logError.Message}");
        }
    }
}
