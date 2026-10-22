using System.Collections;
using MarketingAutomation.Automation;
using MarketingAutomation.GameIntegration;
using UnityEngine;

namespace MarketingAutomation.Persistence;

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

internal static class QueuePersistence
{
    private static string GameVersion => typeof(taskMarketing).Assembly.ManifestModule.ModuleVersionId.ToString();

    private static mainScript Company(savegameScript save)
    {
        var company = save.GetComponent<mainScript>();
        if (company)
        {
            return company;
        }

        var main = GameObject.FindGameObjectWithTag("Main");
        return main ? main.GetComponent<mainScript>() : null;
    }

    private static string NativePath(mainScript company, int slot)
    {
        var settings = new ES3Settings(company.GetSavegameTitle() + slot + ".sav");
        if (settings.location != ES3.Location.File)
        {
            throw new NotSupportedException("Automation persistence requires native file-based saves.");
        }

        return System.IO.Path.GetFullPath(settings.FullPath);
    }

    private static QueueSnapshotStore Store(string nativePath) =>
        new(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(nativePath), "MarketingAutomation", "Snapshots"));

    internal static QueueSaveCheckpoint BeginSave(savegameScript save, int slot)
    {
        var state = new QueueSaveCheckpoint();
        try
        {
            state.Company = Company(save);
            if (!state.Company || state.Company.multiplayer)
            {
                return state;
            }

            state.Path = NativePath(state.Company, slot);
            state.Snapshot = new QueueSaveSnapshot
            {
                GameVersion = GameVersion,
                NativeFormat = save.saveGameVersion,
                CompanyId = state.Company.myID,
                Rooms = MarketingQueue.CaptureActive(save)
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
            state.Suspended = MarketingQueue.SuspendForSave(save);
        }
        return state;
    }

    internal static void FinishSave(savegameScript save, ES3Writer writer, QueueSaveCheckpoint state, Exception nativeError)
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
                !string.Equals(state.Path, System.IO.Path.GetFullPath(writer.settings.FullPath), StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException("The native save destination differs from the captured checkpoint.");
            }

            Store(state.Path).Publish(state.Path, state.Snapshot);
            MarketingAutomationPlugin.Logger.LogInfo($"Saved {state.Snapshot.Rooms.Length} active Marketing queues for slot snapshot {System.IO.Path.GetFileName(state.Path)}.");
        }
        catch (Exception exception)
        {
            MarketingAutomationPlugin.Logger.LogError($"The native game saved, but Marketing queue data could not be saved: {exception}");
            PersistenceDiagnostics.Record($"Checkpoint publication failed for {state.Path}.", exception);
            Notify(save, "The game was saved, but its Marketing automation settings could not be saved. " +
                "Automation may not resume when this save is loaded. Check the plugin log for details.");
        }
        finally
        {
            if (state.Suspended != null)
            {
                foreach (var queue in state.Suspended)
                {
                    if (queue)
                    {
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
            }
        }
    }

    internal static QueueLoadCheckpoint BeginLoad(savegameScript save, int slot)
    {
        MarketingQueue.ClearForLoad(save);
        var company = Company(save);
        if (!company || company.multiplayer)
        {
            return null;
        }

        try
        {
            var state = new QueueLoadCheckpoint { Path = NativePath(company, slot) };
            state.Match = Store(state.Path).Read(state.Path, out state.Snapshot, out state.Fingerprint);
            return state;
        }
        catch (Exception exception)
        {
            MarketingAutomationPlugin.Logger.LogWarning($"Marketing automation data is unavailable for this load: {exception}");
            PersistenceDiagnostics.Record($"Checkpoint read failed for slot {slot}.", exception);
            return new QueueLoadCheckpoint { Match = SnapshotMatch.Damaged };
        }
    }

    internal static void FinishLoad(savegameScript save, QueueLoadCheckpoint state)
    {
        var company = Company(save);
        if (state == null || !company || company.multiplayer || save.loadingSavegame)
        {
            return;
        }

        try
        {
            if (state.Match == SnapshotMatch.Missing)
            {
                MarketingAutomationPlugin.Logger.LogInfo("No matching Marketing automation snapshot. Native paid campaigns remain unchanged.");
                return;
            }
            if (state.Match != SnapshotMatch.Matched)
            {
                throw new InvalidDataException($"Automation snapshot match result: {state.Match}.");
            }

            if (QueueSnapshotStore.Fingerprint(state.Path, out _) != state.Fingerprint)
            {
                throw new InvalidDataException("The native save changed during loading.");
            }

            if (!QueueSnapshotValidation.HeaderMatches(state.Snapshot, GameVersion, save.saveGameVersion, company.myID))
            {
                throw new InvalidDataException("Automation snapshot game version, native format, or company does not match.");
            }

            var seen = new HashSet<int>();
            foreach (var record in state.Snapshot.Rooms)
            {
                if (!QueueSnapshotValidation.ValidRoom(record) || !seen.Add(record.RoomId))
                {
                    throw new InvalidDataException("Automation settings contain invalid or duplicated room records.");
                }
            }

            var allProducts = MarketingProduct.ReadAll();
            var rooms = UnityEngine.Object.FindObjectsOfType<roomScript>();
            var tasks = GameObject.FindGameObjectsWithTag("Task");
            var restored = 0;
            var failed = 0;
            foreach (var record in state.Snapshot.Rooms)
            {
                try
                {
                    RestoreRoom(company, record, allProducts, rooms, tasks);
                    restored++;
                }
                catch (Exception exception)
                {
                    failed++;
                    MarketingAutomationPlugin.Logger.LogWarning($"Marketing queue for room {record.RoomId} was not restored: {exception.Message}");
                    PersistenceDiagnostics.Record($"Room {record.RoomId} restoration failed for {state.Path}.", exception);
                }
            }
            MarketingAutomationPlugin.Logger.LogInfo($"Restored {restored} Marketing queues without purchasing campaigns; {failed} could not be matched.");
            if (failed > 0)
            {
                Notify(save, "Some Marketing automation could not be matched to this save and was left off. " +
                    "Existing paid campaigns are unchanged. Configure automation again for the affected rooms.");
            }
        }
        catch (Exception exception)
        {
            MarketingAutomationPlugin.Logger.LogWarning($"Marketing automation was not restored: {exception}");
            PersistenceDiagnostics.Record($"Restoration failed for {state.Path}; snapshot match={state.Match}.", exception);
            Notify(save, "Marketing automation was not restored because its saved settings are missing, damaged, or conflicting. " +
                "Existing paid campaigns are unchanged. Configure automation again to continue.");
        }
    }

    private static void RestoreRoom(mainScript company, RoomQueueSnapshot record, List<MarketingProduct> allProducts,
        roomScript[] rooms, GameObject[] tasks)
    {
        roomScript room = null;
        foreach (var candidate in rooms)
        {
            if (candidate.myID == record.RoomId)
            {
                if (room)
                {
                    throw new InvalidDataException("Room identity is ambiguous.");
                }

                room = candidate;
            }
        }

        if (!room || room.typ != 6 || room.mS_ != company)
        {
            throw new InvalidDataException("Marketing room no longer exists.");
        }

        var selected = new List<MarketingProduct>();
        foreach (var identity in record.Products)
        {
            MarketingProduct match = null;
            foreach (var product in allProducts)
            {
                if (identity.Matches(product.Key))
                {
                    if (match != null)
                    {
                        throw new InvalidDataException("Product identity is ambiguous.");
                    }

                    match = product;
                }
            }

            if (match != null)
            {
                selected.Add(match);
            }
        }
        taskMarketing paid = null;
        if (record.Paid != null)
        {
            if (room.taskID != record.Paid.TaskId || !selected.Exists(product => record.Paid.Target.Matches(product.Key)))
            {
                throw new InvalidDataException("The paid campaign binding does not match.");
            }

            var matches = 0;
            foreach (var task in tasks)
            {
                if (task.name == "Task_" + record.Paid.TaskId)
                {
                    matches++;
                    paid = task.GetComponent<taskMarketing>();
                }
            }

            if (matches != 1 || !paid || paid.myID != record.Paid.TaskId || paid.typ != record.Paid.Target.Kind ||
                paid.targetID != record.Paid.Target.Id || paid.kampagne != record.Campaign || paid.automatic ||
                paid.stopAutomatic || paid.disableWarten)
            {
                throw new InvalidDataException($"Native campaign identity/flags differ from checkpoint task {record.Paid.TaskId}; matches={matches}.");
            }

            if (!NativeCampaignProgress.Matches(record.Paid.Points, paid.points) ||
                !NativeCampaignProgress.Matches(record.Paid.PointsLeft, paid.pointsLeft))
            {
                throw new InvalidDataException($"Native campaign progress differs for task {paid.myID}: " +
                    $"checkpoint total/left={record.Paid.Points:R}/{record.Paid.PointsLeft:R}, " +
                    $"loaded total/left={paid.points:R}/{paid.pointsLeft:R}.");
            }

            foreach (var candidate in rooms)
            {
                if (candidate != room && candidate.taskID == paid.myID)
                {
                    throw new InvalidDataException("The campaign is assigned to more than one room.");
                }
            }
        }
        else if (room.taskID != -1)
        {
            throw new InvalidDataException("The waiting room now has an unrelated native task.");
        }

        MarketingQueue.Restore(room, company.guiMain_, selected, record, paid);
    }

    private static void Notify(savegameScript save, string message)
    {
        var company = Company(save);
        if (!company || !company.guiMain_)
        {
            return;
        }

        save.gameObject.AddComponent<QueuePersistenceNotice>().Show(company.guiMain_, message);
    }
}

public sealed class QueuePersistenceNotice : MonoBehaviour
{
    internal void Show(GUI_Main manager, string message) => StartCoroutine(Display(manager, message));

    private IEnumerator Display(GUI_Main manager, string message)
    {
        yield return null; // Let the native save/load UI finish its own callbacks.
        if (manager)
        {
            manager.MessageBox(message, false);
            manager.uiObjects[40].transform.SetAsLastSibling();
        }
        Destroy(this);
    }
}
