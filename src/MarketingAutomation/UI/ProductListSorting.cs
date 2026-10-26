using MarketingAutomation.Automation;

namespace MarketingAutomation.UI;

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
    internal static int Compare(ProductSortValue left, ProductSortValue right, QueuePriority field)
    {
        var comparison = field switch
        {
            QueuePriority.Name => left.Name.CompareTo(right.Name),
            QueuePriority.Hype => right.Hype.CompareTo(left.Hype),
            _ => right.ReleaseKey.CompareTo(left.ReleaseKey)
        };
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
