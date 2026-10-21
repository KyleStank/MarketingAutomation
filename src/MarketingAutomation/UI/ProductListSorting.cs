namespace MarketingAutomation;

internal readonly struct ProductSortValue
{
    internal ProductSortValue(ProductKey key, string name, float hype, float releaseKey)
    {
        Key = key;
        Name = name ?? "";
        Hype = hype;
        ReleaseKey = releaseKey;
    }

    internal ProductKey Key
    {
        get;
    }
    internal string Name
    {
        get;
    }
    internal float Hype
    {
        get;
    }
    internal float ReleaseKey
    {
        get;
    }
}

internal static class ProductListSorting
{
    // Match the native selectors' date key, including their undated sentinel
    // and culture-dependent text construction. Queue dates remain numeric.
    internal static float ReleaseKey(int year, int month, bool undated)
    {
        if (undated)
        {
            return 999999f;
        }

        float.TryParse(year.ToString() + ((float)month / 13f).ToString(), out var key);
        return key;
    }

    internal static int Compare(ProductSortValue left, ProductSortValue right, QueuePriority field)
    {
        var comparison = field == QueuePriority.Name ? left.Name.CompareTo(right.Name)
            : field == QueuePriority.Hype ? right.Hype.CompareTo(left.Hype)
            : right.ReleaseKey.CompareTo(left.ReleaseKey);
        if (comparison != 0)
        {
            return comparison;
        }
        // Pooled row instance IDs cannot identify products. Stable typed IDs
        // resolve equal values without changing checkmarks as rows are recycled.
        comparison = left.Key.Kind.CompareTo(right.Key.Kind);
        if (comparison != 0)
        {
            return comparison;
        }

        comparison = left.Key.NativeId.CompareTo(right.Key.NativeId);
        return comparison != 0 ? comparison : left.Key.InstanceId.CompareTo(right.Key.InstanceId);
    }
}
