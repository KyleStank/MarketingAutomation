using System;
using System.Globalization;
using MarketingAutomation.GameIntegration.Marketing;

internal static class NativeCampaignProgressTests
{
    internal static void Run()
    {
        var inMemory = 123.45678f;
        // The installed Mono ES3 writer uses legacy Single.ToString's G7
        // precision, unlike the modern runtime used by this test executable.
        var nativeLoaded = float.Parse(inMemory.ToString("G7", CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
        Require(nativeLoaded != inMemory, "This case reproduces the previous exact-float rejection.");
        Require(NativeCampaignProgress.Matches(inMemory, nativeLoaded), "Legacy checkpoints accept native serialization rounding.");
        Require(NativeCampaignProgress.Matches(nativeLoaded, nativeLoaded), "Canonical new checkpoints accept loaded native progress.");
        Require(!NativeCampaignProgress.Matches(inMemory, nativeLoaded - 0.01f), "Different paid progress remains rejected.");
        Require(!NativeCampaignProgress.Matches(inMemory, nativeLoaded + 0.01f), "Extra paid progress remains rejected.");
        Require(!NativeCampaignProgress.Matches(float.NaN, nativeLoaded) &&
            !NativeCampaignProgress.Matches(inMemory, float.PositiveInfinity), "Non-finite progress cannot be adopted.");
        var originalCulture = CultureInfo.CurrentCulture;
        try
        {
            foreach (var culture in new[] { "en-US", "fr-FR" })
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
                foreach (var remaining in new[] { 0.00012345678f, 1.2345678f, 734.125f, 2345.6789f, 12345678f })
                {
                    var serialized = NativeCampaignProgress.SerializedValue(remaining);
                    Require(NativeCampaignProgress.Matches(remaining, serialized), "Native numeric matching is culture independent.");
                    Require(serialized == NativeCampaignProgress.SerializedValue(serialized), "Repeated save/load normalization is stable.");
                }
            }
        }
        finally { CultureInfo.CurrentCulture = originalCulture; }
        Console.WriteLine("Native campaign progress rounding regression checks passed.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
