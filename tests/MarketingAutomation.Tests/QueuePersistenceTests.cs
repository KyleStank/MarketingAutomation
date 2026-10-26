using System;
using System.IO;
using System.Linq;
using MarketingAutomation.Automation;
using MarketingAutomation.GameIntegration.Marketing;
using MarketingAutomation.Persistence;

internal static class QueuePersistenceTests
{
    internal static void Run()
    {
        var testParent = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "persistence-checks"));
        var testRoot = Path.GetFullPath(Path.Combine(testParent, Guid.NewGuid().ToString("N")));
        if (!testRoot.StartsWith(testParent + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Test directory escaped its intended parent.");
        }

        Directory.CreateDirectory(testRoot);
        try
        {
            var nativePath = Path.Combine(testRoot, "fake-native.sav");
            var copyPath = Path.Combine(testRoot, "fake-copy.sav");
            var root = Path.Combine(testRoot, "mod-snapshots");
            var store = new QueueSnapshotStore(root);
            File.WriteAllText(nativePath, "disposable native snapshot A");
            var before = File.ReadAllBytes(nativePath);
            var active = Snapshot();
            Require(QueueSnapshotValidation.ValidRoom(active.Rooms[0]), "A paid active checkpoint validates.");
            store.Publish(nativePath, active);
            Require(before.SequenceEqual(File.ReadAllBytes(nativePath)), "Publishing never changes native save bytes.");
            Require(store.Read(nativePath, out var loaded, out var hashA) == SnapshotMatch.Matched, "Exact snapshot matches.");
            Require(QueueSnapshotValidation.HeaderMatches(loaded, "test-game-version", 17, 100000), "Version/company header matches.");
            Require(!QueueSnapshotValidation.HeaderMatches(loaded, "different-version", 17, 100000) &&
                !QueueSnapshotValidation.HeaderMatches(loaded, "test-game-version", 18, 100000) &&
                !QueueSnapshotValidation.HeaderMatches(loaded, "test-game-version", 17, 7), "Other versions, formats, and companies are rejected.");
            var queue = loaded.Rooms[0];
            Require(queue.Products.Length == 2 && queue.Products[0].Kind != queue.Products[1].Kind &&
                queue.Products[0].Id == queue.Products[1].Id, "Typed IDs survive serialization without runtime instance IDs.");
            Require(queue.Campaign == 3 && queue.Threshold == 20 && queue.Priority == (int)QueuePriority.ReleaseDate &&
                queue.Direction == (int)QueueDirection.Descending && queue.Paid.TaskId == 123 &&
                queue.Paid.PointsLeft == 734.125f && queue.Paid.Points == 1000f, "Settings and native paid progress round-trip exactly.");
            var restoredKey = new ProductKey(ProductKind.Game, 42, 9999);
            var scheduler = new QueueScheduler(queue.Threshold, (QueuePriority)queue.Priority, (QueueDirection)queue.Direction);
            scheduler.RestoreAdmission(restoredKey);
            Require(scheduler.Choose(new[] { new QueueCandidate(restoredKey, 45, 24000, 10, true) }, 10, 60)?.Equals(restoredKey) == true,
                "Restored admission continues above the entry threshold after loading.");

            store.Publish(nativePath, active);
            Require(Directory.GetFiles(Path.Combine(root, hashA), "*.json").Length == 1, "Identical checkpoints do not multiply records.");
            File.Copy(nativePath, copyPath);
            Require(store.Read(copyPath, out _, out _) == SnapshotMatch.Matched, "An exact copied save can match its unchanged settings.");
            File.WriteAllText(nativePath, "disposable native snapshot B");
            Require(store.Read(nativePath, out _, out _) == SnapshotMatch.Missing, "Slot reuse never attaches an older snapshot's queue.");
            var stopped = Snapshot();
            stopped.Rooms = Array.Empty<RoomQueueSnapshot>();
            store.Publish(nativePath, stopped);
            Require(store.Read(nativePath, out loaded, out var hashB) == SnapshotMatch.Matched && loaded.Rooms.Length == 0,
                "Saving after Stop records no queue settings.");
            File.WriteAllBytes(nativePath, before);
            Require(store.Read(nativePath, out loaded, out _) == SnapshotMatch.Matched && loaded.Rooms.Length == 1,
                "Retained history matches an older save without using the latest slot's settings.");
            active.Rooms[0].Threshold = 10;
            store.Publish(nativePath, active);
            Require(store.Read(nativePath, out loaded, out _) == SnapshotMatch.Conflicting && loaded == null,
                "Identical native bytes with conflicting configurations disable restoration.");
            Require(store.Read(copyPath, out _, out _) == SnapshotMatch.Conflicting, "Copies inherit the same conflict safeguards.");

            File.WriteAllText(nativePath, "disposable native snapshot B");
            var record = Directory.GetFiles(Path.Combine(root, hashB), "*.json").Single();
            File.WriteAllText(record, "broken incomplete JSON");
            Require(store.Read(nativePath, out loaded, out _) == SnapshotMatch.Damaged && loaded == null,
                "A damaged record cannot be used as configuration.");
            File.WriteAllText(nativePath, "disposable native snapshot C");
            var hashC = QueueSnapshotStore.Fingerprint(nativePath, out _);
            Directory.CreateDirectory(Path.Combine(root, hashC));
            File.WriteAllText(Path.Combine(root, hashC, "interrupted.tmp"), "incomplete write");
            Require(store.Read(nativePath, out _, out _) == SnapshotMatch.Missing, "Interrupted temporary records are ignored.");
            File.WriteAllText(nativePath, "disposable native snapshot Name");
            var named = Snapshot();
            named.Rooms[0].Priority = (int)QueuePriority.Name;
            store.Publish(nativePath, named);
            Require(store.Read(nativePath, out var namedLoaded, out _) == SnapshotMatch.Matched &&
                namedLoaded.Rooms[0].Priority == (int)QueuePriority.Name && QueueSnapshotValidation.ValidRoom(namedLoaded.Rooms[0]),
                "Name priority is saved and restored as a valid queue setting.");
            File.WriteAllText(nativePath, "disposable native snapshot rounded progress");
            var legacyProgress = Snapshot();
            legacyProgress.Rooms[0].Paid.PointsLeft = 123.45678f;
            store.Publish(nativePath, legacyProgress);
            var roundedProgress = Snapshot();
            roundedProgress.Rooms[0].Paid.PointsLeft = NativeCampaignProgress.SerializedValue(123.45678f);
            store.Publish(nativePath, roundedProgress);
            Require(store.Read(nativePath, out var roundedLoaded, out _) == SnapshotMatch.Matched &&
                roundedLoaded.Rooms[0].Paid.PointsLeft == roundedProgress.Rooms[0].Paid.PointsLeft,
                "Equivalent legacy/native-rounded checkpoints are not false conflicts.");
            roundedProgress.Rooms[0].Threshold = 19;
            store.Publish(nativePath, roundedProgress);
            Require(store.Read(nativePath, out _, out _) == SnapshotMatch.Conflicting,
                "Normalizing native float rounding never hides genuinely conflicting settings.");
            File.WriteAllText(nativePath, "disposable native snapshot C");
            var unsupported = Snapshot();
            unsupported.Schema = 99;
            store.Publish(nativePath, unsupported);
            Require(store.Read(nativePath, out _, out _) == SnapshotMatch.Damaged, "Unsupported snapshot schemas never resume automation.");

            var invalid = Snapshot().Rooms[0];
            invalid.Paid.PointsLeft = 1001;
            Require(!QueueSnapshotValidation.ValidRoom(invalid), "Impossible paid progress is rejected.");
            invalid.Paid.PointsLeft = float.NaN;
            Require(!QueueSnapshotValidation.ValidRoom(invalid), "Non-finite paid progress is rejected.");
            invalid = Snapshot().Rooms[0];
            invalid.Admitted = null;
            Require(!QueueSnapshotValidation.ValidRoom(invalid), "Paid work must bind to the admitted selected product.");
            invalid = Snapshot().Rooms[0];
            invalid.Products[1] = invalid.Products[0];
            Require(!QueueSnapshotValidation.ValidRoom(invalid), "Duplicate selected identities are rejected.");
            invalid = Snapshot().Rooms[0];
            invalid.Priority = 100;
            Require(!QueueSnapshotValidation.ValidRoom(invalid), "Unknown priority values are rejected.");
            Console.WriteLine("Queue persistence checks passed (disposable files only).");
        }
        finally
        {
            // testRoot was resolved and verified within this executable's test directory above.
            Directory.Delete(testRoot, true);
        }
    }

    private static QueueSaveSnapshot Snapshot()
    {
        var game = new ProductIdentity { Kind = (int)ProductKind.Game, Id = 42 };
        return new QueueSaveSnapshot
        {
            GameVersion = "test-game-version",
            NativeFormat = 17,
            CompanyId = 100000,
            Rooms = new[] { new RoomQueueSnapshot
            {
                RoomId = 55, Campaign = 3, Threshold = 20,
                Priority = (int)QueuePriority.ReleaseDate, Direction = (int)QueueDirection.Descending,
                Products = new[] { game, new ProductIdentity { Kind = (int)ProductKind.Console, Id = 42 } },
                Admitted = game,
                Paid = new PaidCampaignSnapshot { TaskId = 123, Target = game, Points = 1000f, PointsLeft = 734.125f }
            } }
        };
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
