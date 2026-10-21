using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MarketingAutomation;

// Room-local live state. Persistent checkpoints are external to native saves.
public sealed class MarketingQueue : MonoBehaviour
{
    private static readonly HashSet<MarketingQueue> Instances = new();
    private static readonly MethodInfo InitializeNativeTask = AccessTools.DeclaredMethod(typeof(taskMarketing), "FindScripts");
    private roomScript room;
    private GUI_Main manager;
    private List<MarketingProduct> products;
    private ProductIdentity[] selectedIdentities;
    private QueueScheduler scheduler;
    private int campaign;
    private GameObject boundTask;
    private int boundId = -1;
    private int completedFrame = -1;
    private float scanTimer;
    private bool saveSuspended;
    private int resumeAfterFrame = -1;
    internal bool Running
    {
        get; private set;
    }
    internal bool Waiting => Running && boundTask && boundTask.GetComponent<taskWait>();
    internal bool Configured => scheduler != null;
    internal GUI_Main Manager => manager;
    internal roomScript Room => room;
    internal int WaitingThreshold => scheduler.Threshold;

    internal static MarketingQueue ForRoom(roomScript value) => value ? value.GetComponent<MarketingQueue>() : null;

    internal static MarketingQueue ForTask(Component value)
    {
        if (!value)
        {
            return null;
        }

        var marker = value.GetComponent<QueueTaskOwnership>();
        return marker && marker.Owner && marker.Owner.Owns(value.gameObject) ? marker.Owner : null;
    }

    private bool Owns(GameObject value) => room && boundTask == value && room.taskID == boundId;

    internal static bool TryStart(roomScript room, GUI_Main manager, List<MarketingProduct> products,
        int campaign, int threshold, QueuePriority priority, QueueDirection direction, out string error)
    {
        error = null;
        if (!room || !room.mS_ || room.typ != 6 || room.mS_.multiplayer)
        {
            error = "Marketing automation is available in single-player Marketing rooms only.";
            return false;
        }
        if (room.taskID != -1)
        {
            error = "This room already has a task. Finish or cancel it before starting a new automation queue.";
            return false;
        }
        if (products.Count == 0)
        {
            error = "Please select at least one available product.";
            return false;
        }
        // Validate native arrays/unlocks before committing the draft or spending anything.
        var options = NativeCampaignCatalog.Read(manager, room.mS_);
        if (campaign < 0 || campaign >= options.Length || !options[campaign].Unlocked)
        {
            error = "The selected marketing campaign is unavailable.";
            return false;
        }
        var queue = ForRoom(room);
        if (!queue)
        {
            queue = room.gameObject.AddComponent<MarketingQueue>();
        }

        queue.room = room;
        queue.manager = manager;
        queue.products = new List<MarketingProduct>(products);
        queue.selectedIdentities = products.ConvertAll(product => ProductIdentity.From(product.Key)).ToArray();
        queue.campaign = campaign;
        queue.scheduler = new QueueScheduler(Math.Max(0, Math.Min(threshold, options[campaign].HypeCap)), priority, direction);
        queue.Running = true;
        Instances.Add(queue);
        try
        {
            queue.Scan();
        }
        catch
        {
            queue.Stop("Starting the queue failed.");
            throw;
        }
        MarketingAutomation.Logger.LogInfo($"Started Marketing queue in room {room.myID}: {products.Count} products, campaign {campaign}.");
        return true;
    }

    private void LateUpdate()
    {
        if (!Configured || !room || !room.mS_ || saveSuspended || Time.frameCount <= resumeAfterFrame)
        {
            return;
        }

        try
        {
            if (boundId != -1)
            {
                if (room.taskID != boundId && !(room.taskID == -1 && !boundTask && completedFrame >= 0))
                {
                    Detach("The room's task was replaced or transferred.");
                    return;
                }
                if (!boundTask)
                {
                    var completed = completedFrame >= 0;
                    ClearBinding();
                    if (!completed)
                    {
                        Stop("The queue's task was canceled or removed.");
                        return;
                    }
                }
                else if (!boundTask.GetComponent<taskWait>())
                {
                    return; // Paid work always finishes through native Work/Complete.
                }
            }
            if (!Running || room.mS_.multiplayer || room.pause ||
                (room.mS_.save_ && room.mS_.save_.loadingSavegame) || room.mS_.GetDeltaTime() <= 0f)
            {
                return;
            }

            scanTimer -= room.mS_.GetDeltaTime();
            if (boundId == -1 || scanTimer <= 0f)
            {
                scanTimer = 1f;
                Scan();
            }
        }
        catch (Exception exception)
        {
            Stop("An execution error stopped automation; paid work is retained.");
            MarketingAutomation.Logger.LogError($"Marketing queue failed in room {room.myID}: {exception}");
        }
    }

    private void Scan()
    {
        var option = NativeCampaignCatalog.Read(manager, room.mS_)[campaign];
        var candidates = new List<QueueCandidate>(products.Count);
        foreach (var product in products)
        {
            if (!product.Exists)
            {
                continue;
            }

            var price = product.Key.Kind == ProductKind.Game ? option.GamePrice : option.ConsolePrice;
            candidates.Add(new QueueCandidate(product.Key, product.Hype, product.ReleaseMonth, price,
                option.Unlocked && product.CanMarket(room.mS_.myID), product.SortName));
        }
        var next = scheduler.Choose(candidates, room.mS_.money, option.HypeCap);
        if (next.HasValue)
        {
            foreach (var product in products)
            {
                if (!product.Key.Equals(next.Value))
                {
                    continue;
                }

                StartCampaign(product, option);
                return;
            }
        }
        if (!boundTask)
        {
            CreateWaiter();
        }
    }

    private void CreateWaiter()
    {
        var waiter = manager.AddTask_Wait();
        waiter.Init(false);
        waiter.art = -1; // Dormant: never dispatch QA work.
        Bind(waiter.gameObject, waiter.myID);
        MarketingAutomation.Logger.LogInfo($"Marketing queue in room {room.myID} is Waiting.");
    }

    private void StartCampaign(MarketingProduct product, NativeCampaignOption option)
    {
        var games = manager.uiObjects[89].GetComponent<Menu_Marketing_GameKampagne>();
        var consoles = manager.uiObjects[321].GetComponent<Menu_Marketing_KonsoleKampagne>();
        var isGame = product.Key.Kind == ProductKind.Game;
        var price = isGame ? option.GamePrice : option.ConsolePrice;
        var work = isGame ? games.workPoints[campaign] : consoles.workPoints[campaign];
        if (price < 0 || work <= 0 || room.mS_.money < price || !product.CanMarket(room.mS_.myID))
        {
            throw new InvalidOperationException("Native campaign data or eligibility changed before purchase.");
        }

        var task = manager.AddTask_Marketing();
        try
        {
            // Initialize native dependencies now: confirmation can run partway
            // through a frame, before Unity gets to this new task's Start().
            InitializeNativeTask.Invoke(task, null);
        }
        catch
        {
            task.gameObject.SetActive(false);
            Object.Destroy(task.gameObject);
            throw;
        }
        task.Init(false);
        task.targetID = product.Key.NativeId;
        task.typ = isGame ? 0 : 1;
        task.kampagne = campaign;
        task.automatic = task.stopAutomatic = task.disableWarten = false;
        task.points = task.pointsLeft = work;
        task.gS_ = product.NativeGame;
        task.pS_ = product.NativeConsole;
        task.scriptMarketing_ = games;
        task.scriptMarketingKonsole_ = consoles;
        // All checks precede payment, and each new native task is purchased exactly once.
        room.mS_.Pay(price, 12);
        ReleaseWaiter();
        Bind(task.gameObject, task.myID);
        MarketingAutomation.Logger.LogInfo($"Marketing queue room {room.myID}: purchased campaign {campaign} for " +
            $"{product.Key.Kind} {product.Key.NativeId}, cost {price}.");
    }

    private void Bind(GameObject task, int id)
    {
        boundTask = task;
        boundId = id;
        completedFrame = -1;
        task.AddComponent<QueueTaskOwnership>().Owner = this;
        room.taskID = id;
        room.taskGameObject = task; // Replace the native cache together with its ID.
    }

    internal void Completed(taskMarketing task)
    {
        // Native completion already applied hype/accounting and scheduled destruction.
        // Do not dispatch here: other employees/Support may still hold this frame's task.
        completedFrame = Time.frameCount;
        scanTimer = 0f;
    }

    internal void Stop(string reason)
    {
        var wasConfigured = Configured;
        Running = false;
        ReleaseWaiter();
        if (boundTask && completedFrame >= 0)
        {
            boundTask.SetActive(false);
            Object.Destroy(boundTask);
            ClearBinding();
        }
        products = null;
        selectedIdentities = null;
        scheduler = null;
        campaign = -1;
        saveSuspended = false;
        Instances.Remove(this);
        // Leave an already-paid native task intact. The marker holds no settings
        // and only prevents its native toggle from restarting a discarded queue.
        if (!boundTask)
        {
            ClearBinding();
        }

        if (wasConfigured)
        {
            MarketingAutomation.Logger.LogInfo($"Marketing queue room {(room ? room.myID : -1)} stopped and cleared: {reason}");
        }
    }

    private void Detach(string reason)
    {
        // A transferred paid task belongs to its new room. Never reclaim or refund it.
        Stop(reason);
        boundTask = null;
        boundId = -1;
        completedFrame = -1;
    }

    private void ReleaseWaiter()
    {
        if (!boundTask || !boundTask.GetComponent<taskWait>())
        {
            return;
        }

        boundTask.SetActive(false); // Exclude deferred destruction from native SaveTasks scans.
        Object.Destroy(boundTask);
        ClearBinding();
    }

    private void ClearBinding()
    {
        if (room && room.taskID == boundId)
        {
            room.taskID = -1;
            room.taskGameObject = null;
        }
        boundTask = null;
        boundId = -1;
        completedFrame = -1;
    }

    private void OnDestroy()
    {
        Stop("The room or mod was removed.");
        Instances.Remove(this);
    }

    internal static void RemoveAll()
    {
        foreach (var queue in new List<MarketingQueue>(Instances))
        {
            if (!queue)
            {
                continue;
            }

            queue.Stop("The mod is shutting down.");
            Object.Destroy(queue);
        }
        Instances.Clear();
    }

    // Keep transient waiters out of native saves. The live task is restored even
    // when native saving throws; this neither buys work nor alters paid progress.
    internal static List<MarketingQueue> SuspendForSave(savegameScript save)
    {
        var suspended = new List<MarketingQueue>();
        foreach (var queue in Instances)
        {
            if (!queue || !queue.room || !queue.room.mS_ || queue.room.mS_.save_ != save ||
                !queue.boundTask || !queue.Owns(queue.boundTask) ||
                (!queue.boundTask.GetComponent<taskWait>() && queue.completedFrame < 0))
            {
                continue;
            }

            queue.saveSuspended = true;
            suspended.Add(queue);
            queue.boundTask.SetActive(false);
            queue.room.taskID = -1;
            queue.room.taskGameObject = null;
        }
        return suspended;
    }

    internal void RestoreAfterSave()
    {
        if (!saveSuspended)
        {
            return;
        }

        saveSuspended = false;
        if (!room || !boundTask || room.taskID != -1)
        {
            Detach("The task changed during native saving.");
            return;
        }
        boundTask.SetActive(true);
        room.taskID = boundId;
        room.taskGameObject = boundTask;
    }

    internal static void ClearForLoad(savegameScript save)
    {
        foreach (var queue in new List<MarketingQueue>(Instances))
        {
            if (!queue || !queue.room || !queue.room.mS_ || queue.room.mS_.save_ != save)
            {
                continue;
            }

            queue.Stop("A native save is being loaded.");
            Instances.Remove(queue);
            Object.Destroy(queue);
        }
    }

    internal static RoomQueueSnapshot[] CaptureActive(savegameScript save)
    {
        var records = new List<RoomQueueSnapshot>();
        foreach (var queue in Instances)
        {
            if (!queue || !queue.Running || !queue.Configured || !queue.room || !queue.room.mS_ || queue.room.mS_.save_ != save)
            {
                continue;
            }

            if (queue.boundTask ? !queue.Owns(queue.boundTask) : queue.completedFrame < 0 || queue.room.taskID != -1 && queue.room.taskID != queue.boundId)
            {
                continue; // A replaced/transferred/canceled task is never a resume checkpoint.
            }

            var selected = new List<ProductIdentity>(queue.selectedIdentities);
            selected.Sort((left, right) => left.Kind != right.Kind ? left.Kind.CompareTo(right.Kind) : left.Id.CompareTo(right.Id));
            var paid = queue.boundTask ? queue.boundTask.GetComponent<taskMarketing>() : null;
            var record = new RoomQueueSnapshot
            {
                RoomId = queue.room.myID,
                Campaign = queue.campaign,
                Threshold = queue.scheduler.Threshold,
                Priority = (int)queue.scheduler.Priority,
                Direction = (int)queue.scheduler.Direction,
                Products = selected.ToArray(),
                Admitted = queue.scheduler.Admitted.HasValue ? ProductIdentity.From(queue.scheduler.Admitted.Value) : null
            };
            if (paid && queue.completedFrame < 0 && paid.pointsLeft > 0f)
            {
                record.Paid = new PaidCampaignSnapshot
                {
                    TaskId = paid.myID,
                    Target = new ProductIdentity { Kind = paid.typ, Id = paid.targetID },
                    Points = NativeCampaignProgress.SerializedValue(paid.points),
                    PointsLeft = NativeCampaignProgress.SerializedValue(paid.pointsLeft)
                };
            }

            if (!QueueSnapshotValidation.ValidRoom(record))
            {
                throw new InvalidOperationException("A running Marketing queue has an invalid checkpoint.");
            }

            records.Add(record);
        }
        records.Sort((left, right) => left.RoomId.CompareTo(right.RoomId));
        return records.ToArray();
    }

    internal static void Restore(roomScript room, GUI_Main manager, List<MarketingProduct> products,
        RoomQueueSnapshot record, taskMarketing paid)
    {
        var option = NativeCampaignCatalog.Read(manager, room.mS_)[record.Campaign];
        var queue = ForRoom(room);
        if (!queue)
        {
            queue = room.gameObject.AddComponent<MarketingQueue>();
        }

        queue.room = room;
        queue.manager = manager;
        queue.products = products;
        queue.selectedIdentities = record.Products;
        queue.campaign = record.Campaign;
        queue.scheduler = new QueueScheduler(Math.Min(record.Threshold, option.HypeCap),
            (QueuePriority)record.Priority, (QueueDirection)record.Direction);
        foreach (var product in products)
        {
            if (record.Admitted != null && record.Admitted.Matches(product.Key))
            {
                queue.scheduler.RestoreAdmission(product.Key);
            }
        }

        queue.Running = true;
        queue.scanTimer = 0f;
        queue.resumeAfterFrame = Time.frameCount;
        Instances.Add(queue);
        try
        {
            if (paid)
            {
                InitializeNativeTask.Invoke(paid, null);
                foreach (var product in products)
                {
                    if (record.Paid.Target.Matches(product.Key))
                    {
                        paid.gS_ = product.NativeGame;
                        paid.pS_ = product.NativeConsole;
                    }
                }

                queue.Bind(paid.gameObject, paid.myID); // Adopt paid progress; never purchase again.
            }
            else
            {
                queue.CreateWaiter(); // No work is purchased during loading or while paused.
            }
        }
        catch
        {
            queue.Stop("The saved queue could not be restored.");
            throw;
        }
    }
}

public sealed class QueueTaskOwnership : MonoBehaviour
{
    internal MarketingQueue Owner;
}
