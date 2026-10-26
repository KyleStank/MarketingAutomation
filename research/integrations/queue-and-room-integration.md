---
{
  "schema_version": 1,
  "id": "queue-and-room-integration",
  "kind": "integration",
  "title": "Marketing Automation queue execution and native room integration",
  "scope": "mod-specific",
  "summary": "Current room-local cap-maintenance scheduler, one-shot paid native tasks, owned dormant Waiting, Stop/Cancel/Support adapters, transfer limits and persistence handoffs.",
  "project": "MarketingAutomation",
  "domains": ["marketing", "automation", "rooms", "integration"],
  "keywords": ["queue", "inclusive threshold", "admission", "cash-only", "cap maintenance", "Stop Automation", "Waiting for eligible products", "dormant waiter", "Support", "ownership", "deferred completion", "task transfer", "selected products"],
  "symbols": ["MarketingQueue", "QueueScheduler.Choose", "QueueTaskOwnership", "MarketingQueue.PurchaseAndBindCampaign", "MarketingQueue.LateUpdate", "MarketingQueue.Stop", "MarketingQueue.StopAfterTaskReplacement", "QueueIntegration.Install", "QueueIntegration.BeforeRoomPanelsPruned", "QueueIntegration.BeforeCancelConfirmed", "MarketingProduct.CanMarket", "NativeCampaignCatalog.Read", "MarketingQueue.DispatchNextCampaignOrWait", "MarketingQueue.ReconcileTaskBeforeDispatch", "NativeMarketingTasks.PurchaseCampaign", "QueueSnapshotCapture.Capture", "QueuePersistenceIntegration.Install", "ProductKey", "NativeRoomControls", "NativeMarketingRoomWindow", "NativeWaitingRoomWindow", "QueueRoomPresentation", "MarketingQueueRegistry"],
  "source_sets": ["marketing-automation-implementation", "mgt2-managed"],
  "related": [
    {"id": "configuration-ui", "relationship": "Owns confirmed copied form configuration and independent display sorting."},
    {"id": "marketing-task-completion", "relationship": "Owns native paid completion/accounting that the queue preserves with automatic=false."},
    {"id": "marketing-work-and-automation-flags", "relationship": "Explains native flags/work gates; custom queue policy deliberately uses one-shot tasks."},
    {"id": "room-task-binding-and-status", "relationship": "Owns native cache, context, presentation and cancellation gaps adapted here."},
    {"id": "room-support", "relationship": "Native assistants follow leader/current-task; Waiting extends the leader idle predicate."},
    {"id": "native-waiting-and-automation-patterns", "relationship": "Establishes that taskWait art -1 is dormant and art 0 dispatches QA."},
    {"id": "persistence-and-recovery", "relationship": "Owns external snapshot association, paid-task adoption and native-save wrapping."},
    {"id": "bootstrap-and-deployment", "relationship": "Owns plugin startup, patch installation and compatibility gates."}
  ],
  "created": "2026-10-22",
  "updated": "2026-10-26",
  "entry_points": [
    {"label": "Admission and priority", "anchor": "selection-admission-and-current-product-retention", "summary": "Inclusive entry threshold, productive cap/cash guards, blocked skip and current product retention."},
    {"label": "Native paid unit", "anchor": "f-002---each-dispatched-campaign-is-one-cash-funded-native-one-shot", "summary": "Immediate FindScripts, native fields, one debit, false flags and deferred completion observation."},
    {"label": "Visible Waiting adapters", "anchor": "waiting-presentation-context-and-support", "summary": "Dormant art -1, retained slot26, owned text/context/Support patches."},
    {"label": "Stop and Cancel", "anchor": "stop-cancel-transfer-and-errors", "summary": "Stop discards configuration but preserves paid work; Cancel uses native refund."},
    {"label": "Transfer waiter limitation", "anchor": "f-005---task-replacement-stops-the-queue-but-transferred-waiter-cleanup-needs-runtime-review", "summary": "Paid transfer is preserved; Stop can destroy a still-bound owned waiter after ID mismatch."},
    {"label": "Validation limits", "anchor": "unknowns-and-validation", "summary": "Static inspection, prior user observations, isolated checks and untested Support/transfer edges are distinct."}
  ]
}
---

# Marketing Automation queue execution and native room integration

## Overview and Coverage

Room-local scheduling, native paid-task creation, dormant Waiting, ownership adapters and
Support integration. [Scheduling and control policy](../../docs/behavior.md#scheduling)
defines intended behavior. This report describes the implementation statically reviewed on 2026-10-26,
including the transferred-waiter discrepancy in F-005 and pause qualifications in F-006.
UI composition belongs to [configuration-ui](configuration-ui.md); exact save matching belongs to
[persistence-and-recovery](persistence-and-recovery.md).

## Source References and Applicability

Source set `marketing-automation-implementation` in [SOURCES.yaml](../SOURCES.yaml) locates the project sources. The implementation
was statically reviewed on 2026-10-26. Named methods identify the current responsibilities;
this review does not establish new in-game observations.
Native premises use `mgt2-managed` from the [shared registry](../../tools/mgt2-mod-toolkit/research/SOURCES.yaml).

`M/` below means `src/MarketingAutomation/`. Primary code:

| Path | Responsibility and inspected boundary |
| --- | --- |
| `M/Automation/Runtime/MarketingQueue.cs` | Room-local component, live task ownership, scheduling/binding, Stop and save/load handoff |
| `M/Automation/Runtime/MarketingQueueRegistry.cs` | Active queue membership, global shutdown and save/load enumeration |
| `M/Automation/QueueScheduler.cs` | Pure current-admission/candidate/cash/cap/priority decision |
| `M/Automation/QueueOrdering.cs` | Stable persisted priority and direction enum values |
| `M/UI/ProductSortOptions.cs` | Explicit dropdown display/index mapping |
| `M/GameIntegration/QueueIntegration.cs` | Harmony ownership/eligibility guards and task/room callbacks |
| `M/UI/QueueRoomPresentation.cs` | Queue room captions, indicators, waiting menu, restored window/icon and automation control label |
| `M/GameIntegration/QueuePersistenceIntegration.cs` | Harmony save/load and load-preference adapters |
| `M/GameIntegration/UI/NativeRoomControls.cs` | Named live access to roomButtonScript controls, context panels and window slots |
| `M/GameIntegration/UI/NativeMarketingRoomWindow.cs` | Named Marketing roomWindow indicator and hype-wait warning icon |
| `M/GameIntegration/UI/NativeWaitingRoomWindow.cs` | Named generic Waiting roomWindow status text and icon |
| `M/GameIntegration/Marketing/NativeMarketingTasks.cs` | Native paid task purchase, initialization and dormant waiter creation |
| `M/Persistence/QueueSnapshotCapture.cs` | Live queue data converted into the unchanged checkpoint schema |
| `M/Automation/Runtime/QueueTaskOwnership.cs` | Unity task ownership marker |
| `M/Automation/QueueCandidate.cs` | Selected product inputs for an unpaid scheduling decision |
| `M/GameIntegration/Marketing/MarketingProduct.cs` | Live native references, runtime typed key, marketability and release-date inputs |
| `M/GameIntegration/Marketing/NativeCampaignCatalog.cs` | Live arrays, prices/work/cap/gain validation and unlocks |
| `M/Automation/ProductKey.cs` | ProductKind and runtime ProductKey including instance ID |

## Architecture and State Ownership

`MarketingQueue : MonoBehaviour` in `Automation.Runtime` attaches to the native room object. It stores room/GUI references,
a copied list of `MarketingProduct` wrappers, typed persistent selection identities, one `QueueScheduler`,
campaign index and one bound native task object/ID. `Running` and `Configured` are different: after Stop
the paid native task can survive although scheduler/configuration have been cleared.

The pure scheduler stores threshold, priority, direction and a nullable admitted `ProductKey`.
The runtime key combines product kind, native ID and Unity instance ID. Instance identity prevents a
replacement object from inheriting live selection just because a numeric ID is reused; it is not a save
key. `ProductKey` and `ProductKind` belong to Automation rather than UI. External snapshots use typed
persistent identities and reconstruct live references separately.

`NativeMarketingTasks` in `GameIntegration.Marketing` owns native task construction and payment,
while `MarketingQueue` retains task
binding, completion observation and lifetime decisions. `QueueSnapshotCapture.Capture` maps live data to
the existing checkpoint schema; queue eligibility and save suspension remain `MarketingQueue` responsibilities.
`QueuePersistenceIntegration` owns save/load hooks and the silent load-preference seed, separately registered
by the plugin. `QueueIntegration` owns Harmony eligibility/ownership decisions and return semantics;
`UI.QueueRoomPresentation` owns room captions, indicators, context-menu presentation, restored-window
styling and automation labels after those guards pass. `NativeRoomControls` and the room-window bindings
live in `GameIntegration.UI`. `NativeRoomControls` owns the slot mapping for `roomButtonScript`;
the two room-window wrappers distinguish Marketing and generic Waiting layouts
of the same native `roomWindow` type. They resolve current entries on access, without caching prefab children.

`QueueTaskOwnership` is attached to every newly bound/adopted owned task. `ForTask(Component)` resolves
the marker and checks the owner still has that object and matching room task ID (`MarketingQueue.cs`).
`BindOwnedTask(GameObject,int)` records bound object/ID, resets completion state, adds the marker, and replaces
**both** `room.taskID` and `room.taskGameObject`. The marker owns no saved configuration.

`MarketingQueueRegistry` owns the active queue set and operations that enumerate it for shutdown,
load cleanup, capture or save suspension. Individual queues keep their task-lifetime guards and state
transitions. Destructive registry operations enumerate a copy because Stop unregisters queues.
There is no global selected-product reservation or shared campaign scheduler: each room's queue makes independent decisions using shared
native product hype. The current implementation explicitly rejects multiplayer startup.

## Behavior and Call Flows

### Selection, admission and current-product retention

`TryStart(...)` rejects missing/non-Marketing/multiplayer rooms, busy rooms, empty product lists, or an
invalid/locked campaign (`MarketingQueue.cs`). It reads native arrays before committing, attaches or
reuses a queue component, copies the selected list, captures persistent identities and clamps threshold
to 0..cap. It sets Running, registers the instance and **immediately calls DispatchNextCampaignOrWait**.
This initial user-confirmation scan is not gated by the later background pause/game-delta checks.

`DispatchNextCampaignOrWait()` rereads current campaign data, excludes vanished wrapper objects, and builds candidates with
current hype/name/date, separate product-type price, unlock and `CanMarket(companyId)`.
`MarketingProduct.CanMarket` uses native catalog conditions plus the live game-task
off-market/positive-sales cancellation boundary; consoles require ownership and non-withdrawal
(`MarketingProduct.cs`). This is stricter than merely reproducing the initial catalog list.

`QueueScheduler.Choose(candidates,cash,cap)` delegates retained admission to
`CanContinueAdmittedProduct` and new selection to `FindBestNewAdmission`:

1. If the admitted product still exists and `CanPurchase` holds, retain it even if hype is now above
   the entry threshold or another product would sort ahead.
2. Otherwise clear admission, then consider only purchasable candidates at `hype<=threshold`.
3. `CanPurchase` requires eligibility, finite raw hype, hype strictly below cap, nonnegative price and
   `cash>=price`. Equality cash and threshold are allowed; equality cap is blocked.
4. Choose the best candidate and record it as admitted. No candidate leaves admission empty.

`QueueScheduler.ComparePriority` uses Name via `.CompareTo` (the normal .NET string comparison semantics), raw hype, or
numeric release month. Descending reverses the main comparison. Missing dates are ordered last before
reversal and thus remain last in both directions. Ties use product kind, native ID and finally runtime
instance ID. ReleaseMonth is `year*12+month` only for a valid dated released/non-shelved game
or unlocked console (`MarketingProduct.cs`). UI dropdown order is mapped separately from stored
enum values: ReleaseDate=0, Hype=1, Name=2 (`Automation/QueueOrdering.cs`);
`UI/ProductSortOptions.cs` maps display indices independently.

### Paid campaign creation and completion

`MarketingQueue.PurchaseAndBindCampaign(product,option)` calls
`NativeMarketingTasks.PurchaseCampaign(room,manager,product,campaign,option)`. That factory reads the
game/console menu arrays at GUI indices 89/321, chooses price/work and rechecks positive work, price/cash,
and current marketability. `CreateInitializedCampaign` creates a native Marketing task and calls
`InitializeDependencies`, which invokes private `FindScripts` before Unity `Start`; initialization failure
deactivates and destroys the new object before rethrowing.

The factory initializes native ID, typed target/campaign, all three flags false, points/remaining points
and cached menu/product references. Only then it calls `Pay(price,12)` and returns the task. The queue
removes any waiter and binds the new task. Native workers and Support do normal PR work. Native completion
applies hype/accounting and requests destruction because A=false. `QueueIntegration.AfterCampaignCompleted`
calls `MarketingQueue.RecordCampaignCompletion`, which records the frame and resets the scan timer.
It does **not** purchase inside completion.

`MarketingQueue.LateUpdate` first calls `ReconcileTaskBeforeDispatch` to reconcile bound IDs/objects.
A still-live paid task prevents dispatch. An object that disappears after a recorded completion clears
binding and permits another scan; disappearance without completion stops the queue. This waits for
Unity's destroyed-object state instead of assuming other employees no longer hold this frame's task.
Paid work is never preempted just because another room raises hype or a higher-priority candidate appears.

Background dispatch then checks Running, multiplayer, own-room pause, native loading state and positive
game delta. Its timer uses `mainScript.GetDeltaTime()` and scans approximately each accumulated game
second while waiting, or immediately when unbound. Exceptions Stop the queue, retain paid work where
applicable, and log diagnostics.

### Waiting, presentation, context and Support

When no candidate is available and there is no bound task, `MarketingQueue.CreateWaitingTask` calls
`NativeMarketingTasks.CreateWaitingTask`, which calls native AddTask_Wait, initializes it and sets `art=-1`.
The queue binds the result. Native art -1 returns before QA dispatch; it produces no campaign/hype. Own-room generic waiter gates suppress employee work independently.

The slot wrappers were checked against `reference/decompiled/Assembly-CSharp/roomButtonScript.cs`
(`RemoveMenus`, `BUTTON_Main`), `roomScript.cs` (`UpdateWindowWait`), and `roomWindow.cs`
(`Window_Wait`, `Window_Marketing`), plus the reconstructed
`reference/extracted_unity_project/ExportedProject/Assets/GameObject/uiRoom.prefab` bindings.
The installed Assembly-CSharp SHA-256 matches the recorded decompilation input. These source/prefab checks
do not establish runtime behavior.

| Wrapper member | Native owner/slot | Reconstructed prefab binding |
| --- | --- | --- |
| `NativeRoomControls.SharedRoomOptions` | roomButtonScript uiObjects[2] | RoomDefaultOptions, GameObject 1330820015962899 |
| `NativeRoomControls.ActiveMarketingOptions` | roomButtonScript uiOptions[12] | OptionenMarketingAktiv (active Marketing options), GameObject 1447409744879641 |
| `NativeRoomControls.MarketingStatusWindow` | roomButtonScript uiWindows[4] | Window_Marketing, GameObject 1533803739292891 |
| `NativeRoomControls.WaitingStatusWindow` | roomButtonScript uiWindows[26] | Window_Wait, GameObject 1094283985748868 |
| `NativeMarketingRoomWindow.AutomationIndicator` | Marketing roomWindow uiObjects[4] | IconAuto, GameObject 1240571728440943 |
| `NativeMarketingRoomWindow.HypeWaitingWarningIcon` | Marketing roomWindow uiObjects[5], child Icon | Message, GameObject 1617365563186364; Icon child 1408345039935966 |
| `NativeWaitingRoomWindow.StatusText` | Waiting roomWindow uiObjects[0] | Text, GameObject 1433881075115309 |
| `NativeWaitingRoomWindow.StatusIcon` | Waiting roomWindow uiObjects[1] | Icon, GameObject 1838352681112328 |

`HasWaitingWindowSlot` retains the native-array null/length guard. `WaitingStatusWindow` reads and writes
the live array slot, so clearing it before `RemoveMenus` protects the window and restoring it afterwards
keeps the original binding. The wrappers do not reorder menu callbacks, activation or hierarchy changes.

`QueueIntegration` checks ownership and dispatches presentation to `QueueRoomPresentation`:
`ShowWaitingStatus`, `ShowCampaignAutomation`, `ToggleWaitingRoomMenu`, `RestoreWaitingStatusWindow`
and `ShowAutomationControl`. The waiting-menu method captures `wasOpen` before `CloseAllRoomButtons`,
then updates panel visibility, reparents the shared controls, raises the room UI and applies pause state.
The ownership checks and native prefix true/false decisions remain in `QueueIntegration`.

The adapters address separate native gaps:

- Before native `RemoveMenus`, temporarily remove window `[26]` from the array; restore it afterwards
  and copy the native Marketing warning sprite into its icon. The guard covers **all initialized
  single-player Marketing rooms**, not only rooms already running a queue. It preserves the panel
  availability needed for a later queue.
- After `Window_Wait`, an owned Running waiter gets text `Waiting for eligible products...` and an
  inclusive threshold caption. Normal room update/visibility/offscreen behavior still controls the panel.
- An owned Waiting room intercepts `BUTTON_Main` and opens active Marketing context `[12]`, supplying
  Stop/Cancel while preserving the native dropdown/shared-control/pause pattern.
- After `roomScript.WaitForMinimumHype`, an owned Waiting leader returns true. Native Marketing Support
  consults this predicate before attempting work, so assistants idle. Once the paid Marketing task is
  bound, they naturally follow its current task through the native room link.
- After paid `Window_Marketing`, the automation indicator reflects queue Running instead of native A,
  which is intentionally false. Native campaign/hype behavior itself stays in the native task.

The mod adds no new Support purchase, selected list or leader task type. Leader-only pause retains the
native uncertainty documented by [room-support](../../tools/mgt2-mod-toolkit/research/systems/room-support.md).

### Stop, Cancel, transfer and errors

`BeforeAutomationToggle` and `BeforeAutomationLabel` act only when an owned marker resolves. Stop calls
`queue.Stop`, closes the native room menu and unpauses instead of flipping A
(`QueueIntegration.cs`). The label is native End Automation while Running; after Stop it becomes
disabled “Automation stopped” for the surviving owned paid task, preventing resurrection of discarded
configuration through native Start Automation.

`Stop(string)` sets Running=false, removes an owned waiter, cleans an already-completed bound object,
nulls products/selection/scheduler, sets campaign=-1 and unregisters the queue. A completed native object
awaiting destruction is cleaned up separately. An unfinished paid native Marketing task remains intact;
its A/S/D are false, so it finishes once without another purchase (`MarketingQueue.cs`).

Native paid cancellation reaches `taskMarketing.Abbrechen`; its prefix Stops the owning queue while
the original refunds unfinished work and destroys the task. Generic waiter cancellation also has a
prefix. The generic cancel confirmation lacks a native taskWait dispatch, so `BeforeCancelConfirmed`
explicitly Stops owned Waiting before the native dispatcher clears the room
(`QueueIntegration.cs`). It is possible for native invalid-target cancellation to use the
same Marketing method; the queue stops rather than inferring user intent.

Bound-ID mismatch calls `StopAfterTaskReplacement` and Stops the queue (`MarketingQueue.cs`). A transferred
paid task is left to its new room; there is no attempt to refund, move it back or follow it. **Waiter
qualification:** `Stop -> RemoveWaitingTask` deactivates/destroys its bound waiter even after room-ID mismatch;
`ClearTaskBinding` only clears the old room when its ID still matches. The implementation therefore
does not establish preservation of a transferred owned waiter; runtime reachability and destination cleanup
remain unresolved (F-005).

OnDestroy Stops/unregisters the room queue. `MarketingQueue.RemoveAll` delegates global removal to
`MarketingQueueRegistry.StopAll`, which Stops and destroys all enumerated queues. Task markers
are not independent automation controllers and do not retain a configuration preset.

## Findings and Evidence

### F-001 - Admission is an unpaid purchase policy independent of paid progress

Statement: The current scheduler admits at h<=threshold, purchases only below cap with enough cash, retains a purchasable admitted product above its entry threshold, and drops admission when blocked before choosing another selected candidate.
Basis: static-observation
Standing: current
Applies to: marketing-automation-implementation
Evidence: `M/Automation/QueueScheduler.cs`, `Choose`, `CanContinueAdmittedProduct`, `FindBestNewAdmission`, `CanPurchase`; `M/Automation/Runtime/MarketingQueue.cs`, `DispatchNextCampaignOrWait`.
Explanation: This is ongoing cap maintenance, not a finite pass or native A/S/D behavior. A product above threshold remains selected but cannot newly enter. Newly available products are absent from the copied list. Reaching cap or losing affordability/eligibility clears current admission before future selection.

### F-002 - Each dispatched campaign is one cash-funded native one-shot

Statement: Dispatch initializes native dependencies and paid task fields, sets A/S/D false, debits once, and lets native workers/completion own paid progress; replacement dispatch waits until the completed object is gone.
Basis: static-observation
Standing: current
Applies to: marketing-automation-implementation
Evidence: `M/GameIntegration/Marketing/NativeMarketingTasks.cs`, `PurchaseCampaign`, `CreateInitializedCampaign`, `InitializeDependencies`; `M/Automation/Runtime/MarketingQueue.cs`, `PurchaseAndBindCampaign`, `BindOwnedTask`, `RecordCampaignCompletion`, `LateUpdate`, `ReconcileTaskBeforeDispatch`; `M/GameIntegration/QueueIntegration.cs`, `AfterCampaignCompleted`; native completion report F-001 through F-004.
Explanation: False automatic avoids native repeat's debit before a postfix could rotate. Immediate FindScripts handles a confirmation occurring before Unity Start. This preserves the native completion accounting asymmetry rather than silently correcting it. Static control flow is not proof of all Unity same-frame ordering.

### F-003 - Visible Waiting requires coordinated native-room adapters

Statement: The current mod represents Waiting by an owned art -1 taskWait, retains single-player Marketing window26 during initialization, supplies owned caption/context/automation indicators, and exposes Waiting through the leader idle predicate for Support.
Basis: static-observation
Standing: current
Applies to: marketing-automation-implementation
Evidence: `M/Automation/Runtime/MarketingQueue.cs`, `CreateWaitingTask`; `M/GameIntegration/Marketing/NativeMarketingTasks.cs`, `CreateWaitingTask`; `M/GameIntegration/QueueIntegration.cs`, `BeforeRoomPanelsPruned`, `AfterRoomPanelsPruned`, `AfterWaitingDisplay`, `BeforeRoomMenu`, `AfterRoomWaitingCheck`, `AfterCampaignDisplay`; `M/UI/QueueRoomPresentation.cs`, `ShowWaitingStatus`, `ShowCampaignAutomation`, `ToggleWaitingRoomMenu`, `RestoreWaitingStatusWindow`; named slot wrappers `M/GameIntegration/UI/NativeRoomControls.cs`, `NativeMarketingRoomWindow.cs`, `NativeWaitingRoomWindow.cs` and the prefab bindings above; native waiter and room-binding reports.
Explanation: Retention is room-type/single-player scoped before any queue exists; custom text/context is task/queue owned. It is more precise than saying every native patch is limited to an already-owned room. The waiter is excluded from native saves via separate persistence handoff.

### F-004 - Stop discards configuration while preserving unfinished paid work

Statement: The owned Stop adapter clears queue settings immediately, removes Waiting, leaves unfinished native paid work to finish, and disables native restart on that surviving owned task.
Basis: static-observation
Standing: current
Applies to: marketing-automation-implementation
Evidence: `M/Automation/Runtime/MarketingQueue.cs`, `Stop`; `M/GameIntegration/QueueIntegration.cs`, `BeforeAutomationToggle`, `BeforeAutomationLabel`, `BeforeCampaignCanceled`, `BeforeWaitingCanceled`, `BeforeCancelConfirmed`; `M/UI/QueueRoomPresentation.cs`, `ShowAutomationControl`.
Explanation: Stop is intentionally different from the unmanaged native A toggle. The native immediate Cancel route still invokes native paid refund. Stopped queue configuration is absent from subsequent active captures; historical snapshots are handled separately by persistence.

### F-005 - Task replacement stops the queue but transferred waiter cleanup needs runtime review

Statement: A changed room task ID detaches/stops execution and preserves a transferred unfinished paid task, but Stop can deactivate/destroy a bound owned waiter after transfer because RemoveWaitingTask has no room-ID ownership guard.
Basis: static-observation
Standing: current
Applies to: marketing-automation-implementation
Evidence: `M/Automation/Runtime/MarketingQueue.cs`, `ReconcileTaskBeforeDispatch`, `Stop`, `StopAfterTaskReplacement`, `RemoveWaitingTask`, `ClearTaskBinding`; native `Menu_Verschiebe_Task.TaskTauschen` in room-binding report.
Explanation: This is a statically identified implementation boundary; a fix has not been validated. Runtime reachability and destination cleanup for an owned waiter require targeted review. The intended product policy cannot serve as evidence that this edge already preserves the moved task.

### F-006 - Background spending gates do not describe initial confirmation or every Support contribution

Statement: LateUpdate gates dispatch on game delta/loading/room pause, while TryStart immediately scans after explicit confirmation; paid Support contributions still follow the native leader-pause behavior rather than these scheduler gates.
Basis: static-observation
Standing: current
Applies to: marketing-automation-implementation
Evidence: `M/Automation/Runtime/MarketingQueue.cs`, `TryStart`, `DispatchNextCampaignOrWait`, `LateUpdate`; native room-support F-003 and Marketing work report F-001.
Explanation: “Pause-aware queue” describes background purchasing, not a blanket claim that no confirmation can buy while a modal pauses the game or that pausing the leader prevents unpaused assistants advancing paid work.

## Connections to Other Systems

- The form copies a confirmed selection/configuration into TryStart; draft lifecycle and independent list
  display sorting remain UI responsibilities. Persisted enum values must not be inferred from dropdown order.
- Live native campaign arrays/unlocks are reread and validated; unequal game/console cap/gain arrays are
  rejected by `NativeCampaignCatalog.Read`, not silently reconciled.
- `MarketingQueueRegistry.SuspendForSave`, `CaptureActive` and `ClearForLoad` enumerate active queues.
  `MarketingQueue.SuspendForSave`, `RestoreAfterSave`, `CaptureSnapshot` and `Restore` own individual
  live-state transitions. Transient owned waiters/completed tasks are temporarily excluded; paid tasks
  remain native. Restore adopts matching paid progress or creates an unpaid waiter without buying during
  load. `CanCaptureTaskState` rejects replaced/canceled/transferred ownership, and
  `QueueSnapshotCapture.Capture` serializes accepted queue data. `QueuePersistenceIntegration` connects
  native save/load hooks to these handoffs. External association and numeric validation belong to the
  persistence report.
- Native user workflows remain available for unmanaged tasks. The patch list's broad target types do not
  imply all instances receive queue behavior; each adapter's actual guards must be inspected.

## Mod Integration Implications

Future work should preserve the boundary between pure unpaid selection, room/task binding, native paid
execution, owned status/control adaptation and external checkpoint restoration. Do not reintroduce
finite-pass bookkeeping or normalize native subordinate booleans as part of this queue model.

Keep ownership checks and deferred completion observation when changing scheduling. A native completion
postfix alone is insufficient if automatic repetition remains enabled. Changing queue priority must not
preempt paid work or discard admitted continuation above threshold. Architectural research provides
no evidence that changing native accounting or Support pause is required by the queue.

## Unknowns and Validation

The user-reported baseline documented with the 2026-10-22 inspection supports functional single-player queue/UI behavior;
manual persistence observations are recorded once in
[persistence F-004](persistence-and-recovery.md#f-004---user-acceptance-supports-manual-continuity-within-stated-limits).
Those observations do not validate subsequent source edits or every ownership/Support edge.
[QueueSchedulerTests.cs](../../tests/MarketingAutomation.Tests/QueueSchedulerTests.cs)
owns isolated scheduling cases; it does not execute Unity tasks or Harmony hooks.

- Q-001: Transferred owned waiter handling (F-005) needs runtime reachability and destination-cleanup review.
- Q-002: Same-frame completion with multiple employees/assistants, Stop/Cancel ordering and object-ID reuse need native checks.
- Q-003: Support idle/resume across Waiting/task switches is not individually runtime-verified. Leader-only pause retains the native uncertainty.
- Q-004: Autosave continuity remains unverified; see persistence Q-001.

### Runtime regression checks

Use disposable single-player sessions for changed integration paths:

- Start below threshold; observe paid continuation above threshold, cap rotation and re-entry after decay.
  Check mixed game/console prices and cash-blocked Waiting without an extra debit.
- While Waiting, verify caption/room-UI visibility, no spending or hype, and automatic resumption.
  Stop and Cancel must remove the waiter without leaving an orphan task.
- Stop during paid work; it should finish once without restart. Cancel paid work and compare the native
  unfinished-work refund. Transfer/replace paid work and verify the queue neither follows nor reclaims it.
- Use two independent rooms and Support assistants. Check Waiting idle/resume and completion under
  concurrent employee work; exercise transferred waiters specifically against F-005.

These scenarios target native integration; they are not individual pass claims.
