using MarketingAutomation.GameIntegration.Marketing;
using MarketingAutomation.Persistence;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MarketingAutomation.Automation.Runtime;

// Room-local live state. Persistent checkpoints are external to native saves.
// Native typ means type: room type 6 identifies a Marketing room.
public sealed class MarketingQueue : MonoBehaviour
{
    private roomScript _room;
    private GUI_Main _manager;
    private List<MarketingProduct> _products;
    private ProductIdentity[] _selectedIdentities;
    private QueueScheduler _scheduler;
    private int _campaign;
    private GameObject _boundTask;
    private int _boundId = -1;
    private int _completedFrame = -1;
    private float _scanTimer;
    private bool _saveSuspended;
    private int _resumeAfterFrame = -1;

    internal bool Running
    {
        get;
        private set;
    }

    internal bool Waiting => Running && _boundTask && _boundTask.GetComponent<taskWait>();

    internal bool Configured => _scheduler != null;

    internal GUI_Main Manager => _manager;

    internal roomScript Room => _room;

    internal int WaitingThreshold => _scheduler.Threshold;

    internal static MarketingQueue ForRoom(roomScript value) => value ? value.GetComponent<MarketingQueue>() : null;

    internal static MarketingQueue ForTask(Component value)
    {
        if (!value)
            return null;

        var marker = value.GetComponent<QueueTaskOwnership>();
        return marker && marker.Owner && marker.Owner.OwnsTask(value.gameObject) ? marker.Owner : null;
    }

    private bool OwnsTask(GameObject value) => _room && _boundTask == value && _room.taskID == _boundId;

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

        queue._room = room;
        queue._manager = manager;
        queue._products = new List<MarketingProduct>(products);
        queue._selectedIdentities = products.ConvertAll(product => ProductIdentity.From(product.Key)).ToArray();
        queue._campaign = campaign;
        queue._scheduler = new QueueScheduler(Math.Max(0, Math.Min(threshold, options[campaign].HypeCap)), priority,
            direction);
        queue.Running = true;
        MarketingQueueRegistry.Register(queue);

        try
        {
            queue.DispatchNextCampaignOrWait();
        }
        catch
        {
            queue.Stop("Starting the queue failed.");
            throw;
        }

        MarketingAutomationPlugin.Logger.LogInfo(
            $"Started Marketing queue in room {room.myID}: {products.Count} products, campaign {campaign}.");
        return true;
    }

    private void DispatchNextCampaignOrWait()
    {
        var option = NativeCampaignCatalog.Read(_manager, _room.mS_)[_campaign];
        var candidates = new List<QueueCandidate>(_products.Count);
        foreach (var product in _products)
        {
            if (!product.Exists)
            {
                continue;
            }

            var price = product.Key.Kind == ProductKind.Game ? option.GamePrice : option.ConsolePrice;
            candidates.Add(new QueueCandidate(product.Key, product.Hype, product.ReleaseMonth, price,
                option.Unlocked && product.CanMarket(_room.mS_.myID), product.SortName));
        }

        var next = _scheduler.Choose(candidates, _room.mS_.money, option.HypeCap);
        if (next.HasValue)
        {
            foreach (var product in _products)
            {
                if (!product.Key.Equals(next.Value))
                {
                    continue;
                }

                PurchaseAndBindCampaign(product, option);
                return;
            }
        }

        if (!_boundTask)
        {
            CreateWaitingTask();
        }
    }

    private void PurchaseAndBindCampaign(MarketingProduct product, NativeCampaignOption option)
    {
        var task = NativeMarketingTasks.PurchaseCampaign(_room, _manager, product, _campaign, option);
        RemoveWaitingTask();
        BindOwnedTask(task.gameObject, task.myID);

        var price = product.Key.Kind == ProductKind.Game ? option.GamePrice : option.ConsolePrice;
        MarketingAutomationPlugin.Logger.LogInfo(
            $"Marketing queue room {_room.myID}: purchased campaign {_campaign} for " +
            $"{product.Key.Kind} {product.Key.NativeId}, cost {price}.");
    }

    private void CreateWaitingTask()
    {
        var waiter = NativeMarketingTasks.CreateWaitingTask(_manager);
        BindOwnedTask(waiter.gameObject, waiter.myID);
        MarketingAutomationPlugin.Logger.LogInfo($"Marketing queue in room {_room.myID} is Waiting.");
    }

    private void BindOwnedTask(GameObject task, int id)
    {
        _boundTask = task;
        _boundId = id;
        _completedFrame = -1;

        task.AddComponent<QueueTaskOwnership>().Owner = this;
        _room.taskID = id;
        _room.taskGameObject = task; // Replace the native cache together with its ID.
    }

    private void LateUpdate()
    {
        if (!Configured || !_room || !_room.mS_ || _saveSuspended || Time.frameCount <= _resumeAfterFrame)
        {
            return;
        }

        try
        {
            if (!ReconcileTaskBeforeDispatch())
                return;

            if (!Running || _room.mS_.multiplayer || _room.pause ||
                (_room.mS_.save_ && _room.mS_.save_.loadingSavegame) || _room.mS_.GetDeltaTime() <= 0f)
            {
                return;
            }

            _scanTimer -= _room.mS_.GetDeltaTime();
            if (_boundId == -1 || _scanTimer <= 0f)
            {
                _scanTimer = 1f;
                DispatchNextCampaignOrWait();
            }
        }
        catch (Exception exception)
        {
            Stop("An execution error stopped automation; paid work is retained.");
            MarketingAutomationPlugin.Logger.LogError($"Marketing queue failed in room {_room.myID}: {exception}");
        }
    }

    private bool ReconcileTaskBeforeDispatch()
    {
        if (_boundId == -1)
            return true;

        var completedTaskWasRemoved = _room.taskID == -1 && !_boundTask && _completedFrame >= 0;
        if (_room.taskID != _boundId && !completedTaskWasRemoved)
        {
            StopAfterTaskReplacement("The room's task was replaced or transferred.");
            return false;
        }

        if (!_boundTask)
        {
            var completed = _completedFrame >= 0;
            ClearTaskBinding();
            if (!completed)
            {
                Stop("The queue's task was canceled or removed.");
                return false;
            }
        }
        else if (!_boundTask.GetComponent<taskWait>())
        {
            // Paid work finishes through native Work/Complete. Even a completed task
            // must disappear before another purchase: workers may still hold it this frame.
            return false;
        }

        return true;
    }

    internal void RecordCampaignCompletion()
    {
        // Native completion already applied hype/accounting and scheduled destruction.
        // Do not dispatch here: other employees/Support may still hold this frame's task.
        _completedFrame = Time.frameCount;
        _scanTimer = 0f;
    }

    internal void Stop(string reason)
    {
        var wasConfigured = Configured;
        Running = false;

        RemoveWaitingTask();
        if (_boundTask && _completedFrame >= 0)
        {
            _boundTask.SetActive(false);
            Object.Destroy(_boundTask);
            ClearTaskBinding();
        }

        _products = null;
        _selectedIdentities = null;
        _scheduler = null;
        _campaign = -1;
        _saveSuspended = false;
        MarketingQueueRegistry.Unregister(this);

        // Leave an already-paid native task intact. The marker holds no settings
        // and only prevents its native toggle from restarting a discarded queue.
        if (!_boundTask)
        {
            ClearTaskBinding();
        }

        if (wasConfigured)
        {
            MarketingAutomationPlugin.Logger.LogInfo(
                $"Marketing queue room {(_room ? _room.myID : -1)} stopped and cleared: {reason}");
        }
    }

    private void StopAfterTaskReplacement(string reason)
    {
        // A transferred paid task belongs to its new room. Never reclaim or refund it.
        Stop(reason);
        _boundTask = null;
        _boundId = -1;
        _completedFrame = -1;
    }

    private void RemoveWaitingTask()
    {
        if (!_boundTask || !_boundTask.GetComponent<taskWait>())
        {
            return;
        }

        _boundTask.SetActive(false); // Exclude deferred destruction from native SaveTasks scans.
        Object.Destroy(_boundTask);
        ClearTaskBinding();
    }

    private void ClearTaskBinding()
    {
        if (_room && _room.taskID == _boundId)
        {
            _room.taskID = -1;
            _room.taskGameObject = null;
        }

        _boundTask = null;
        _boundId = -1;
        _completedFrame = -1;
    }

    private void OnDestroy()
    {
        Stop("The room or mod was removed.");
        MarketingQueueRegistry.Unregister(this);
    }

    internal static void RemoveAll() => MarketingQueueRegistry.StopAll();

    internal bool BelongsToSave(savegameScript save) => _room && _room.mS_ && _room.mS_.save_ == save;

    // Keep transient waiters out of native saves. The live task is restored even
    // when native saving throws; this neither buys work nor alters paid progress.
    internal void SuspendForSave(savegameScript save, List<MarketingQueue> suspendedQueues)
    {
        if (!BelongsToSave(save) || !_boundTask || !OwnsTask(_boundTask) ||
            (!_boundTask.GetComponent<taskWait>() && _completedFrame < 0))
        {
            return;
        }

        _saveSuspended = true;
        // Record the cleanup obligation before SetActive can invoke native component callbacks.
        suspendedQueues.Add(this);

        _boundTask.SetActive(false);
        _room.taskID = -1;
        _room.taskGameObject = null;
    }

    internal void RestoreAfterSave()
    {
        if (!_saveSuspended)
        {
            return;
        }

        _saveSuspended = false;
        if (!_room || !_boundTask || _room.taskID != -1)
        {
            StopAfterTaskReplacement("The task changed during native saving.");
            return;
        }

        _boundTask.SetActive(true);
        _room.taskID = _boundId;
        _room.taskGameObject = _boundTask;
    }

    internal RoomQueueSnapshot CaptureSnapshot(savegameScript save)
    {
        if (!Running || !Configured || !BelongsToSave(save))
            return null;

        // A replaced/transferred/canceled task is never a resume checkpoint.
        if (!CanCaptureTaskState())
            return null;

        var paid = _boundTask ? _boundTask.GetComponent<taskMarketing>() : null;
        return QueueSnapshotCapture.Capture(
            _room.myID,
            _campaign,
            _selectedIdentities,
            _scheduler,
            paid,
            _completedFrame >= 0);
    }

    private bool CanCaptureTaskState()
    {
        if (_boundTask)
            return OwnsTask(_boundTask);

        // A destroyed task is checkpointable only after observed completion,
        // before native room cleanup or after it has cleared the room task ID.
        return _completedFrame >= 0 && (_room.taskID == -1 || _room.taskID == _boundId);
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

        queue._room = room;
        queue._manager = manager;
        queue._products = products;
        queue._selectedIdentities = record.Products;
        queue._campaign = record.Campaign;
        queue._scheduler = new QueueScheduler(Math.Min(record.Threshold, option.HypeCap),
            (QueuePriority)record.Priority, (QueueDirection)record.Direction);

        foreach (var product in products)
        {
            if (record.Admitted != null && record.Admitted.Matches(product.Key))
            {
                queue._scheduler.RestoreAdmission(product.Key);
            }
        }

        queue.Running = true;
        queue._scanTimer = 0f;
        queue._resumeAfterFrame = Time.frameCount;
        MarketingQueueRegistry.Register(queue);

        try
        {
            if (paid)
            {
                NativeMarketingTasks.InitializeDependencies(paid);
                foreach (var product in products)
                {
                    if (record.Paid.Target.Matches(product.Key))
                    {
                        paid.gS_ = product.NativeGame;
                        paid.pS_ = product.NativeConsole;
                    }
                }

                queue.BindOwnedTask(paid.gameObject, paid.myID); // Adopt paid progress; never purchase again.
            }
            else
            {
                queue.CreateWaitingTask(); // No work is purchased during loading or while paused.
            }
        }
        catch
        {
            queue.Stop("The saved queue could not be restored.");
            throw;
        }
    }
}
