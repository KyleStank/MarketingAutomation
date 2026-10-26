using MarketingAutomation.Automation;

namespace MarketingAutomation.Persistence;

internal static class QueueSnapshotValidation
{
    internal static bool HeaderMatches(QueueSaveSnapshot snapshot, string gameVersion, int nativeFormat,
        int companyId) =>
        snapshot != null && snapshot.Schema == 1 && snapshot.GameVersion == gameVersion &&
        snapshot.NativeFormat == nativeFormat && snapshot.CompanyId == companyId && snapshot.Rooms != null;

    internal static bool ValidRoom(RoomQueueSnapshot room)
    {
        if (room == null || room.RoomId <= 0 || room.Campaign < 0 || room.Campaign >= 6 || room.Threshold < 0 ||
            !Enum.IsDefined(typeof(QueuePriority), room.Priority) ||
            !Enum.IsDefined(typeof(QueueDirection), room.Direction) ||
            room.Products == null || room.Products.Length == 0)
        {
            return false;
        }

        var selected = new HashSet<ProductIdentity>();
        foreach (var product in room.Products)
        {
            if (product == null || !product.IsValid || !selected.Add(product))
            {
                return false;
            }
        }

        if (room.Admitted != null && !selected.Contains(room.Admitted))
        {
            return false;
        }

        var paid = room.Paid;
        if (paid == null)
        {
            return true;
        }

        return HasValidPaidCampaign(paid, room.Admitted, selected);
    }

    private static bool HasValidPaidCampaign(PaidCampaignSnapshot paid, ProductIdentity admitted,
        HashSet<ProductIdentity> selected)
    {
        // Paid work must belong to the same admitted product whose continuation the scheduler will resume.
        return paid.TaskId > 0 && paid.Target != null && selected.Contains(paid.Target) &&
               paid.Target.Equals(admitted) && IsFinite(paid.Points) && IsFinite(paid.PointsLeft) &&
               paid.Points > 0 && paid.PointsLeft > 0 && paid.PointsLeft <= paid.Points;
    }

    private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
}
