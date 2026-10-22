using MarketingAutomation.UI;

namespace MarketingAutomation.Automation;

internal readonly struct QueueCandidate
{
    internal QueueCandidate(ProductKey key, float hype, int? releaseMonth, long price, bool eligible, string name = "")
    {
        Key = key;
        Hype = hype;
        ReleaseMonth = releaseMonth;
        Price = price;
        Eligible = eligible;
        Name = name ?? "";
    }

    internal ProductKey Key
    {
        get;
    }
    internal float Hype
    {
        get;
    }
    internal int? ReleaseMonth
    {
        get;
    }
    internal long Price
    {
        get;
    }
    internal bool Eligible
    {
        get;
    }
    internal string Name
    {
        get;
    }
}

// Chooses unpaid work only. The native task remains authoritative for paid progress.
internal sealed class QueueScheduler
{
    private readonly int threshold;
    private readonly QueuePriority priority;
    private readonly QueueDirection direction;
    private ProductKey? admitted;

    internal QueueScheduler(int threshold, QueuePriority priority, QueueDirection direction)
    {
        this.threshold = threshold;
        this.priority = priority;
        this.direction = direction;
    }

    internal void ResetAdmission() => admitted = null;
    internal int Threshold => threshold;
    internal QueuePriority Priority => priority;
    internal QueueDirection Direction => direction;
    internal ProductKey? Admitted => admitted;
    internal void RestoreAdmission(ProductKey? key) => admitted = key;

    internal ProductKey? Choose(IReadOnlyList<QueueCandidate> candidates, long cash, int cap)
    {
        if (admitted.HasValue)
        {
            foreach (var candidate in candidates)
            {
                if (candidate.Key.Equals(admitted.Value) && CanPurchase(candidate, cash, cap))
                {
                    return candidate.Key;
                }
            }

            admitted = null;
        }

        QueueCandidate? best = null;
        foreach (var candidate in candidates)
        {
            if (!CanPurchase(candidate, cash, cap) || candidate.Hype > threshold)
            {
                continue;
            }

            if (!best.HasValue || Compare(candidate, best.Value) < 0)
            {
                best = candidate;
            }
        }
        admitted = best?.Key;
        return admitted;
    }

    private static bool CanPurchase(QueueCandidate candidate, long cash, int cap) =>
        candidate.Eligible && !float.IsNaN(candidate.Hype) && !float.IsInfinity(candidate.Hype) &&
        candidate.Hype < cap && candidate.Price >= 0 && cash >= candidate.Price;

    private int Compare(QueueCandidate left, QueueCandidate right)
    {
        int comparison;
        if (priority == QueuePriority.Name)
        {
            comparison = left.Name.CompareTo(right.Name);
        }
        else if (priority == QueuePriority.ReleaseDate)
        {
            // Undated products go last in both directions.
            comparison = right.ReleaseMonth.HasValue.CompareTo(left.ReleaseMonth.HasValue);
            if (comparison != 0)
            {
                return comparison;
            }

            comparison = Nullable.Compare(left.ReleaseMonth, right.ReleaseMonth);
        }
        else
        {
            comparison = left.Hype.CompareTo(right.Hype);
        }

        if (direction == QueueDirection.Descending)
        {
            comparison = -comparison;
        }

        if (comparison != 0)
        {
            return comparison;
        }

        comparison = left.Key.Kind.CompareTo(right.Key.Kind);
        if (comparison != 0)
        {
            return comparison;
        }

        comparison = left.Key.NativeId.CompareTo(right.Key.NativeId);
        return comparison != 0 ? comparison : left.Key.InstanceId.CompareTo(right.Key.InstanceId);
    }
}
