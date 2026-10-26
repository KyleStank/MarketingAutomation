using System.Globalization;

namespace MarketingAutomation.GameIntegration.Marketing;

internal static class NativeCampaignProgress
{
    // This game's Mono float formatter writes seven significant digits through
    // ES3JSONWriter.WritePrimitive(float). Compare the saved representation rather
    // than requiring all bits of the pre-save in-memory value to survive.
    internal static float SerializedValue(float value) =>
        float.Parse(value.ToString("G7", CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);

    internal static bool Matches(float checkpoint, float loaded) =>
        IsFinite(checkpoint) && IsFinite(loaded) && (checkpoint == loaded || SerializedValue(checkpoint) == loaded);

    private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
}
