namespace MarketingAutomation.GameIntegration.Marketing;

internal static class NativeProductOrdering
{
    // Match the native selectors' date key, including their undated sentinel
    // and culture-dependent text construction. Queue dates remain numeric.
    internal static float ReleaseDateKey(int year, int month, bool undated)
    {
        if (undated)
            return 999999f;

        float.TryParse(year.ToString() + ((float)month / 13f).ToString(), out var key);
        return key;
    }
}
