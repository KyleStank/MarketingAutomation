namespace MarketingAutomation.Automation;

// Chooses unpaid work only. The native task remains authoritative for paid progress.
internal sealed class QueueScheduler
{
    private readonly int _threshold;
    private readonly QueuePriority _priority;
    private readonly QueueDirection _direction;
    private ProductKey? _admitted;

    internal QueueScheduler(int threshold, QueuePriority priority, QueueDirection direction)
    {
        _threshold = threshold;
        _priority = priority;
        _direction = direction;
    }

    internal int Threshold => _threshold;

    internal QueuePriority Priority => _priority;

    internal QueueDirection Direction => _direction;

    internal ProductKey? Admitted => _admitted;

    internal ProductKey? Choose(IReadOnlyList<QueueCandidate> candidates, long cash, int cap)
    {
        // Entry threshold only applies when admitting a product. Once admitted,
        // it keeps its turn above that threshold until capped or blocked.
        if (CanContinueAdmittedProduct(candidates, cash, cap))
            return _admitted;

        _admitted = FindBestNewAdmission(candidates, cash, cap);
        return _admitted;
    }

    private bool CanContinueAdmittedProduct(IReadOnlyList<QueueCandidate> candidates, long cash, int cap)
    {
        if (!_admitted.HasValue)
            return false;

        foreach (var candidate in candidates)
        {
            if (candidate.Key.Equals(_admitted.Value) && CanPurchase(candidate, cash, cap))
                return true;
        }

        _admitted = null;
        return false;
    }

    private ProductKey? FindBestNewAdmission(IReadOnlyList<QueueCandidate> candidates, long cash, int cap)
    {
        QueueCandidate? best = null;
        foreach (var candidate in candidates)
        {
            if (!CanPurchase(candidate, cash, cap) || candidate.Hype > _threshold)
                continue;

            if (!best.HasValue || ComparePriority(candidate, best.Value) < 0)
                best = candidate;
        }

        return best?.Key;
    }

    private static bool CanPurchase(QueueCandidate candidate, long cash, int cap) =>
        candidate.Eligible && !float.IsNaN(candidate.Hype) && !float.IsInfinity(candidate.Hype) &&
        candidate.Hype < cap && candidate.Price >= 0 && cash >= candidate.Price;

    private int ComparePriority(QueueCandidate left, QueueCandidate right)
    {
        int comparison;
        if (_priority == QueuePriority.Name)
        {
            comparison = left.Name.CompareTo(right.Name);
        }
        else if (_priority == QueuePriority.ReleaseDate)
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

        if (_direction == QueueDirection.Descending)
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

    internal void ResetAdmission() => _admitted = null;

    internal void RestoreAdmission(ProductKey? key) => _admitted = key;

}
