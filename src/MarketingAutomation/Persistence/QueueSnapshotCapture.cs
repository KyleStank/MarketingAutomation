using MarketingAutomation.Automation;
using MarketingAutomation.GameIntegration.Marketing;

namespace MarketingAutomation.Persistence;

// Converts live queue data to the existing checkpoint schema; it never changes native progress.
// Native typ means product type (0 = game, 1 = console).
internal static class QueueSnapshotCapture
{
    internal static RoomQueueSnapshot Capture(
        int roomId,
        int campaign,
        ProductIdentity[] selectedProducts,
        QueueScheduler scheduler,
        taskMarketing paidTask,
        bool campaignCompleted)
    {
        var selected = new List<ProductIdentity>(selectedProducts);
        selected.Sort((left, right) =>
            left.Kind != right.Kind ? left.Kind.CompareTo(right.Kind) : left.Id.CompareTo(right.Id));

        var snapshot = new RoomQueueSnapshot
        {
            RoomId = roomId,
            Campaign = campaign,
            Threshold = scheduler.Threshold,
            Priority = (int)scheduler.Priority,
            Direction = (int)scheduler.Direction,
            Products = selected.ToArray(),
            Admitted = scheduler.Admitted.HasValue ? ProductIdentity.From(scheduler.Admitted.Value) : null
        };

        if (paidTask && !campaignCompleted && paidTask.pointsLeft > 0f)
        {
            snapshot.Paid = new PaidCampaignSnapshot
            {
                TaskId = paidTask.myID,
                Target = new ProductIdentity { Kind = paidTask.typ, Id = paidTask.targetID },
                Points = NativeCampaignProgress.SerializedValue(paidTask.points),
                PointsLeft = NativeCampaignProgress.SerializedValue(paidTask.pointsLeft)
            };
        }

        if (!QueueSnapshotValidation.ValidRoom(snapshot))
            throw new InvalidOperationException("A running Marketing queue has an invalid checkpoint.");

        return snapshot;
    }
}
