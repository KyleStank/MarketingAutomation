using MarketingAutomation.Automation.Runtime;
using MarketingAutomation.GameIntegration.Marketing;
using UnityEngine;

namespace MarketingAutomation.Persistence;

internal static class QueuePersistence
{
    private static string GameVersion => typeof(taskMarketing).Assembly.ManifestModule.ModuleVersionId.ToString();

    internal static QueueSaveCheckpoint BeginSave(savegameScript save, int slot)
    {
        var state = new QueueSaveCheckpoint();
        try
        {
            state.Company = FindCompany(save);
            if (!state.Company || state.Company.multiplayer)
            {
                return state;
            }

            state.Path = ResolveNativeSavePath(state.Company, slot);
            state.Snapshot = new QueueSaveSnapshot
            {
                GameVersion = GameVersion,
                NativeFormat = save.saveGameVersion,
                CompanyId = state.Company.myID,
                Rooms = MarketingQueueRegistry.CaptureActive(save)
            };
        }
        catch (Exception exception)
        {
            state.CaptureError = exception;
            MarketingAutomationPlugin.Logger.LogError($"Unable to capture Marketing queues: {exception}");
            PersistenceDiagnostics.Record($"Capture failed for slot {slot}.", exception);
        }
        finally
        {
            // Transient waiting/completed tasks must be hidden even if checkpoint capture failed.
            state.Suspended = MarketingQueueRegistry.SuspendForSave(save);
        }

        return state;
    }

    private static mainScript FindCompany(savegameScript save)
    {
        var company = save.GetComponent<mainScript>();
        if (company)
        {
            return company;
        }

        var main = GameObject.FindGameObjectWithTag("Main");
        return main ? main.GetComponent<mainScript>() : null;
    }

    private static string ResolveNativeSavePath(mainScript company, int slot)
    {
        var settings = new ES3Settings(company.GetSavegameTitle() + slot + ".sav");
        if (settings.location != ES3.Location.File)
        {
            throw new NotSupportedException("Automation persistence requires native file-based saves.");
        }

        return System.IO.Path.GetFullPath(settings.FullPath);
    }

    private static QueueSnapshotStore OpenSnapshotStore(string nativePath) =>
        new(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(nativePath), "MarketingAutomation", "Snapshots"));

    internal static void FinishSave(savegameScript save, ES3Writer writer, QueueSaveCheckpoint state,
        Exception nativeError)
    {
        if (state == null)
        {
            return;
        }

        try
        {
            if (nativeError != null)
            {
                PersistenceDiagnostics.Record("Native save failed; no new queue checkpoint published.", nativeError);
                return;
            }

            if (!state.Company || state.Company.multiplayer)
            {
                return;
            }

            if (state.CaptureError != null)
            {
                throw new InvalidOperationException("Queue capture failed before native saving.", state.CaptureError);
            }

            if (state.Snapshot == null || writer == null || writer.settings.location != ES3.Location.File ||
                !string.Equals(state.Path, System.IO.Path.GetFullPath(writer.settings.FullPath),
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException("The native save destination differs from the captured checkpoint.");
            }

            OpenSnapshotStore(state.Path).Publish(state.Path, state.Snapshot);
            MarketingAutomationPlugin.Logger.LogInfo(
                $"Saved {state.Snapshot.Rooms.Length} active Marketing queues for slot snapshot {System.IO.Path.GetFileName(state.Path)}.");
        }
        catch (Exception exception)
        {
            MarketingAutomationPlugin.Logger.LogError(
                $"The native game saved, but Marketing queue data could not be saved: {exception}");
            PersistenceDiagnostics.Record($"Checkpoint publication failed for {state.Path}.", exception);
            Notify(save, "The game was saved, but its Marketing automation settings could not be saved. " +
                         "Automation may not resume when this save is loaded. Check the plugin log for details.");
        }
        finally
        {
            // Native saving may throw; the live room bindings still need to be restored.
            RestoreSuspendedQueues(state.Suspended);
        }
    }

    private static void RestoreSuspendedQueues(List<MarketingQueue> suspendedQueues)
    {
        if (suspendedQueues == null)
        {
            return;
        }

        foreach (var queue in suspendedQueues)
        {
            if (!queue)
            {
                continue;
            }

            try
            {
                queue.RestoreAfterSave();
            }
            catch (Exception exception)
            {
                queue.Stop("Save cleanup failed.");
                MarketingAutomationPlugin.Logger.LogError($"Marketing queue save cleanup failed: {exception}");
                PersistenceDiagnostics.Record("Queue cleanup after saving failed.", exception);
            }
        }
    }

    internal static QueueLoadCheckpoint BeginLoad(savegameScript save, int slot)
    {
        MarketingQueueRegistry.ClearForLoad(save);

        var company = FindCompany(save);
        if (!company || company.multiplayer)
        {
            return null;
        }

        try
        {
            var state = new QueueLoadCheckpoint { Path = ResolveNativeSavePath(company, slot) };
            state.Match = OpenSnapshotStore(state.Path).Read(state.Path, out state.Snapshot, out state.Fingerprint);
            return state;
        }
        catch (Exception exception)
        {
            MarketingAutomationPlugin.Logger.LogWarning(
                $"Marketing automation data is unavailable for this load: {exception}");
            PersistenceDiagnostics.Record($"Checkpoint read failed for slot {slot}.", exception);
            return new QueueLoadCheckpoint { Match = SnapshotMatch.Damaged };
        }
    }

    internal static void FinishLoad(savegameScript save, QueueLoadCheckpoint state)
    {
        var company = FindCompany(save);
        if (state == null || !company || company.multiplayer || save.loadingSavegame)
        {
            return;
        }

        try
        {
            if (state.Match == SnapshotMatch.Missing)
            {
                MarketingAutomationPlugin.Logger.LogInfo(
                    "No matching Marketing automation snapshot. Native paid campaigns remain unchanged.");
                return;
            }

            if (state.Match != SnapshotMatch.Matched)
            {
                throw new InvalidDataException($"Automation snapshot match result: {state.Match}.");
            }

            ValidateLoadedSnapshot(save, state, company);

            RestoreMatchingRooms(save, state, company);
        }
        catch (Exception exception)
        {
            MarketingAutomationPlugin.Logger.LogWarning($"Marketing automation was not restored: {exception}");
            PersistenceDiagnostics.Record($"Restoration failed for {state.Path}; snapshot match={state.Match}.",
                exception);
            Notify(save,
                "Marketing automation was not restored because its saved settings are missing, damaged, or conflicting. " +
                "Existing paid campaigns are unchanged. Configure automation again to continue.");
        }
    }

    private static void ValidateLoadedSnapshot(savegameScript save, QueueLoadCheckpoint state, mainScript company)
    {
        if (QueueSnapshotStore.Fingerprint(state.Path, out _) != state.Fingerprint)
        {
            throw new InvalidDataException("The native save changed during loading.");
        }

        if (!QueueSnapshotValidation.HeaderMatches(state.Snapshot, GameVersion, save.saveGameVersion, company.myID))
        {
            throw new InvalidDataException(
                "Automation snapshot game version, native format, or company does not match.");
        }

        // Validate every record before restoring any room. Live-object failures are isolated per room later.
        var seen = new HashSet<int>();
        foreach (var record in state.Snapshot.Rooms)
        {
            if (!QueueSnapshotValidation.ValidRoom(record) || !seen.Add(record.RoomId))
            {
                throw new InvalidDataException("Automation settings contain invalid or duplicated room records.");
            }
        }
    }

    private static void RestoreMatchingRooms(savegameScript save, QueueLoadCheckpoint state, mainScript company)
    {
        var allProducts = MarketingProduct.ReadAll();
        var rooms = UnityEngine.Object.FindObjectsOfType<roomScript>();
        var tasks = GameObject.FindGameObjectsWithTag("Task");

        var restored = 0;
        var failed = 0;
        foreach (var record in state.Snapshot.Rooms)
        {
            try
            {
                RoomQueueRestorer.Restore(company, record, allProducts, rooms, tasks);
                restored++;
            }
            catch (Exception exception)
            {
                failed++;
                MarketingAutomationPlugin.Logger.LogWarning(
                    $"Marketing queue for room {record.RoomId} was not restored: {exception.Message}");
                PersistenceDiagnostics.Record($"Room {record.RoomId} restoration failed for {state.Path}.",
                    exception);
            }
        }

        MarketingAutomationPlugin.Logger.LogInfo(
            $"Restored {restored} Marketing queues without purchasing campaigns; {failed} could not be matched.");
        if (failed > 0)
        {
            Notify(save, "Some Marketing automation could not be matched to this save and was left off. " +
                         "Existing paid campaigns are unchanged. Configure automation again for the affected rooms.");
        }
    }

    private static void Notify(savegameScript save, string message)
    {
        var company = FindCompany(save);
        if (!company || !company.guiMain_)
        {
            return;
        }

        save.gameObject.AddComponent<QueuePersistenceNotice>().Show(company.guiMain_, message);
    }
}
