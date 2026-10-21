using System;

namespace MarketingAutomation;

// Keep persisted numeric values stable; dropdown order is mapped separately.
internal enum QueuePriority
{
    ReleaseDate = 0, Hype = 1, Name = 2
}
internal enum QueueDirection
{
    Ascending, Descending
}

internal static class ProductSortOptions
{
    private static readonly QueuePriority[] DisplayOrder = { QueuePriority.Name, QueuePriority.Hype, QueuePriority.ReleaseDate };
    internal static System.Collections.Generic.List<string> Labels() => new() { "Name", "Hype", "Release Date" };
    internal static QueuePriority FromIndex(int index) => DisplayOrder[index];
    internal static int IndexOf(QueuePriority priority) => Array.IndexOf(DisplayOrder, priority);
    internal static string Label(QueuePriority priority) => Labels()[IndexOf(priority)];
}

internal sealed class CampaignConfiguration
{
    internal QueuePriority Priority { get; private set; } = QueuePriority.Hype;
    internal QueueDirection Direction { get; private set; } = QueueDirection.Ascending;
    internal string PrioritySummary => $"{ProductSortOptions.Label(Priority)} / {Direction}";
    internal string PriorityExplanation
    {
        get
        {
            var order = Priority == QueuePriority.Name
                ? Direction == QueueDirection.Ascending ? "Names A to Z." : "Names Z to A."
                : Priority == QueuePriority.Hype
                ? Direction == QueueDirection.Ascending ? "Lower hype first (10 before 85)." : "Higher hype first (85 before 10)."
                : Direction == QueueDirection.Ascending ? "Older releases first." : "Newer releases first.";
            return order + " Ineligible or unaffordable products are skipped.\n" +
                "A product being marketed keeps priority until its campaign cap unless blocked.";
        }
    }
    internal void SetPriority(QueuePriority priority, QueueDirection direction)
    {
        Priority = priority;
        Direction = direction;
    }
    internal int SelectedCampaign { get; private set; } = -1;
    internal int HypeCap
    {
        get; private set;
    }
    internal int? Threshold
    {
        get; private set;
    }
    internal bool HasSelection => SelectedCampaign >= 0;

    internal void Select(int campaign, int hypeCap, bool unlocked)
    {
        if (!unlocked)
        {
            return;
        }

        if (campaign < 0 || hypeCap < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(campaign));
        }

        SelectedCampaign = campaign;
        HypeCap = hypeCap;
        Threshold = Threshold.HasValue ? Math.Min(Threshold.Value, hypeCap) : hypeCap;
    }

    internal void SetThreshold(int value)
    {
        if (HasSelection)
        {
            Threshold = Math.Max(0, Math.Min(value, HypeCap));
        }
    }

    internal void Reset()
    {
        SelectedCampaign = -1;
        HypeCap = 0;
        Threshold = null;
        Priority = QueuePriority.Hype;
        Direction = QueueDirection.Ascending;
    }
}
