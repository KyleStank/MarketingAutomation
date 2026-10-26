using MarketingAutomation.Persistence;
using Object = UnityEngine.Object;

namespace MarketingAutomation.Automation.Runtime;

// Owns the set of active room queues and operations that span them.
// Individual queues retain exclusive control over their task bindings and live state.
internal static class MarketingQueueRegistry
{
    private static readonly HashSet<MarketingQueue> Queues = [];

    internal static void Register(MarketingQueue queue) => Queues.Add(queue);

    internal static void Unregister(MarketingQueue queue) => Queues.Remove(queue);

    internal static void StopAll()
    {
        // Stop unregisters each queue, so destructive operations enumerate a copy.
        foreach (var queue in new List<MarketingQueue>(Queues))
        {
            if (!queue)
                continue;

            queue.Stop("The mod is shutting down.");
            Object.Destroy(queue);
        }

        Queues.Clear();
    }

    internal static void ClearForLoad(savegameScript save)
    {
        foreach (var queue in new List<MarketingQueue>(Queues))
        {
            if (!queue || !queue.BelongsToSave(save))
                continue;

            queue.Stop("A native save is being loaded.");
            Unregister(queue);
            Object.Destroy(queue);
        }
    }

    internal static RoomQueueSnapshot[] CaptureActive(savegameScript save)
    {
        var records = new List<RoomQueueSnapshot>();
        foreach (var queue in Queues)
        {
            if (!queue)
                continue;

            var record = queue.CaptureSnapshot(save);
            if (record != null)
                records.Add(record);
        }

        records.Sort((left, right) => left.RoomId.CompareTo(right.RoomId));
        return records.ToArray();
    }

    internal static List<MarketingQueue> SuspendForSave(savegameScript save)
    {
        var suspended = new List<MarketingQueue>();
        foreach (var queue in Queues)
        {
            if (queue)
                queue.SuspendForSave(save, suspended);
        }

        return suspended;
    }
}
