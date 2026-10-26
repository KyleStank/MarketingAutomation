using MarketingAutomation.Automation.Runtime;

namespace MarketingAutomation.Persistence;

// These objects live only across a native save/load call; they are not serialized.
internal sealed class QueueSaveCheckpoint
{
    internal mainScript Company;
    internal string Path;
    internal QueueSaveSnapshot Snapshot;
    internal List<MarketingQueue> Suspended;
    internal Exception CaptureError;
}

internal sealed class QueueLoadCheckpoint
{
    internal string Path;
    internal string Fingerprint;
    internal QueueSaveSnapshot Snapshot;
    internal SnapshotMatch Match;
}

