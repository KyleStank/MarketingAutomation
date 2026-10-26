namespace MarketingAutomation.Automation;

internal sealed class CampaignConfiguration
{
    internal QueuePriority Priority
    {
        get;
        private set;
    } = QueuePriority.Hype;

    internal QueueDirection Direction
    {
        get;
        private set;
    } = QueueDirection.Ascending;

    internal int SelectedCampaign
    {
        get;
        private set;
    } = -1;

    internal int HypeCap
    {
        get;
        private set;
    }

    internal int? Threshold
    {
        get;
        private set;
    }

    internal bool HasSelection => SelectedCampaign >= 0;

    internal void SetPriority(QueuePriority priority, QueueDirection direction)
    {
        Priority = priority;
        Direction = direction;
    }

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
