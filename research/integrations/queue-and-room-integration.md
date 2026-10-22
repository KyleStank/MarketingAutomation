---
{
  "schema_version": 1,
  "id": "queue-and-room-integration",
  "kind": "integration",
  "title": "Marketing Automation queue execution and native room integration",
  "scope": "mod-specific",
  "summary": "Current room-local cap-maintenance scheduler, one-shot paid native tasks, owned dormant Waiting, Stop/Cancel/Support adapters, transfer limits and persistence handoffs.",
  "project": "MarketingAutomation",
  "code_revision": "Working source inspected on 2026-10-22; includes uncommitted changes; citations identify the relevant files and symbols",
  "domains": ["marketing", "automation", "rooms", "integration"],
  "keywords": ["queue", "inclusive threshold", "admission", "cash-only", "cap maintenance", "Stop Automation", "Waiting for eligible products", "dormant waiter", "Support", "ownership", "deferred completion", "task transfer", "selected products"],
  "symbols": ["MarketingQueue", "QueueScheduler.Choose", "QueueTaskOwnership", "MarketingQueue.StartCampaign", "MarketingQueue.LateUpdate", "MarketingQueue.Stop", "MarketingQueue.Detach", "QueueIntegration.Install", "QueueIntegration.BeforeRoomPanelsPruned", "QueueIntegration.BeforeCancelConfirmed", "MarketingProduct.CanMarket", "NativeCampaignCatalog.Read"],
  "source_sets": ["marketing-automation-implementation", "mgt2-managed-2026-10-22"],
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
  "updated": "2026-10-22",
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

This report describes the inspected **current working-tree implementation**, rather than an earlier
finite-pass or checkbox-driven queue proposal. The implementation repeatedly maintains explicitly
selected games/consoles toward one campaign cap, admits work at an inclusive configured threshold,
requires cash for every purchase, and uses one ordinary native task per paid campaign. No file in the
mod implementation was changed while preparing this report.

The product authority remains [REQUIREMENTS.md](../../REQUIREMENTS.md), especially Task scheduling,
Multiple rooms and Support, and Persistence. This report explains what code currently does and where
native adapters/validation boundaries lie; it is not another requirements specification.
UI composition belongs to [configuration-ui](configuration-ui.md); exact save matching belongs to
[persistence-and-recovery](persistence-and-recovery.md).

## Source References and Applicability

Source set `marketing-automation-implementation` locates the current project sources. The implementation
was inspected on 2026-10-22, including uncommitted changes. File and symbol citations identify the relevant
behavior; routine development changes do not require a new source set or file inventory.
Native premises use `mgt2-managed-2026-10-22`; resolve sets through [SOURCES.yaml](../../tools/mgt2-mod-toolkit/research/SOURCES.yaml).

`M/` below means `src/MarketingAutomation/`. Primary code:

| Path | Responsibility and inspected boundary |
| --- | --- |
| `M/Automation/MarketingQueue.cs:12-513` | Room-local component, live task ownership, scheduling/binding, Stop and save/load handoff |
| `M/Automation/QueueScheduler.cs:43-143` | Pure current-admission/candidate/cash/cap/priority decision |
| `M/Automation/CampaignConfiguration.cs:3-23` | Stable persisted priority values and explicit UI index mapping |
| `M/GameIntegration/QueueIntegration.cs:10-205` | Harmony task/room/status/control and persistence adapters |
| `M/GameIntegration/MarketingProduct.cs:7-110` | Live native references, runtime typed key, marketability and release-date inputs |
| `M/GameIntegration/NativeCampaignCatalog.cs:15-59` | Live arrays, prices/work/cap/gain validation and unlocks |
| `M/UI/ProductSelection.cs:3-40` | ProductKind and runtime ProductKey including instance ID |

Names are in current `MarketingAutomation.Automation`, `.GameIntegration`, `.Persistence` and `.UI`
namespaces. Exact native names remain external bindings. Report line references describe inspected text;
verify method symbols if later refactoring moves them.

## Architecture and State Ownership

`MarketingQueue : MonoBehaviour` attaches to the native room object. It stores room/GUI references,
a copied list of `MarketingProduct` wrappers, typed persistent selection identities, one `QueueScheduler`,
campaign index and one bound native task object/ID. `Running` and `Configured` are different: after Stop
the paid native task can survive although scheduler/configuration have been cleared.

The pure scheduler stores threshold, priority, direction and a nullable admitted `ProductKey`.
The runtime key combines product kind, native ID and Unity instance ID. Instance identity prevents a
replacement object from inheriting live selection just because a numeric ID is reused; it is not a save
key. External snapshots use typed persistent identities and reconstruct live references separately.

`QueueTaskOwnership` is attached to every newly bound/adopted owned task. `ForTask(Component)` resolves
the marker and checks the owner still has that object and matching room task ID (`MarketingQueue.cs:40-51`).
`Bind(GameObject,int)` records bound object/ID, resets completion state, adds the marker, and replaces
**both** `room.taskID` and `room.taskGameObject` (`:244-252`). The marker owns no saved configuration.

Static `Instances` supports shutdown and persistence enumeration. There is no global selected-product
reservation or shared campaign scheduler: each room's queue makes independent decisions using shared
native product hype. The current implementation explicitly rejects multiplayer startup.

## Behavior and Call Flows

### Selection, admission and current-product retention

`TryStart(...)` rejects missing/non-Marketing/multiplayer rooms, busy rooms, empty product lists, or an
invalid/locked campaign (`MarketingQueue.cs:53-85`). It reads native arrays before committing, attaches or
reuses a queue component, copies the selected list, captures persistent identities and clamps threshold
to 0..cap. It sets Running, registers the instance and **immediately calls Scan** (`:86-103`).
This initial user-confirmation scan is not gated by the later background pause/game-delta checks.

`Scan()` rereads current campaign data, excludes vanished wrapper objects, and builds candidates with
current hype/name/date, separate product-type price, unlock and `CanMarket(companyId)`
(`:157-190`). `MarketingProduct.CanMarket` uses native catalog conditions plus the live game-task
off-market/positive-sales cancellation boundary; consoles require ownership and non-withdrawal
(`MarketingProduct.cs:29-43`). This is stricter than merely reproducing the initial catalog list.

`QueueScheduler.Choose(candidates,cash,cap)` (`QueueScheduler.cs:65-99`):

1. If the admitted product still exists and `CanPurchase` holds, retain it even if hype is now above
   the entry threshold or another product would sort ahead.
2. Otherwise clear admission, then consider only purchasable candidates at `hype<=threshold`.
3. `CanPurchase` requires eligibility, finite raw hype, hype strictly below cap, nonnegative price and
   `cash>=price`. Equality cash and threshold are allowed; equality cap is blocked.
4. Choose the best candidate and record it as admitted. No candidate leaves admission empty.

Comparison uses Name via `.CompareTo` (the normal .NET string comparison semantics), raw hype, or
numeric release month. Descending reverses the main comparison. Missing dates are ordered last before
reversal and thus remain last in both directions. Ties use product kind, native ID and finally runtime
instance ID (`:101-143`). ReleaseMonth is `year*12+month` only for a valid dated released/non-shelved game
or unlocked console (`MarketingProduct.cs:35-43`). UI dropdown order is mapped separately from stored
enum values: ReleaseDate=0, Hype=1, Name=2 (`CampaignConfiguration.cs:3-23`).

### Paid campaign creation and completion

`StartCampaign(product,option)` gets game/console menu arrays from GUI indices 89/321, chooses the
appropriate price/work, and rechecks positive work, price/cash and current product marketability
(`MarketingQueue.cs:201-213`). It creates a native Marketing task and immediately invokes its private
`FindScripts` by reflection before normal Unity `Start`, destroying the new object if initialization fails
(`:214-228`). It then initializes native ID, typed target/campaign, all three flags false, points/remaining
points and cached menu/product references (`:229-237`).

Only then it calls `Pay(price,12)`, releases any waiter and binds the new task (`:238-242`). Native workers
and Support do normal PR work. Native completion applies hype/accounting and requests destruction because
A=false; `QueueIntegration.AfterCampaignCompleted` observes that completion and records the frame
(`QueueIntegration.cs:45`; `MarketingQueue.Completed():254-260`). It does **not** purchase inside completion.

`LateUpdate()` first reconciles bound IDs/objects. A still-live paid task returns without scanning. An
object that disappears after a recorded completion clears binding and permits another scan; disappearance
without completion stops the queue. This waits for Unity's destroyed-object state instead of assuming
other employees no longer hold this frame's task (`MarketingQueue.cs:106-136`). Paid work is never
preempted just because another room raises hype or a higher-priority candidate appears.

Background dispatch then checks Running, multiplayer, own-room pause, native loading state and positive
game delta. Its timer uses `mainScript.GetDeltaTime()` and scans approximately each accumulated game
second while waiting, or immediately when unbound (`:137-150`). Exceptions Stop the queue, retain paid work
where applicable, and log diagnostics (`:151-155`).

### Waiting, presentation, context and Support

When no candidate is available and there is no bound task, `CreateWaiter()` calls native AddTask_Wait,
initializes it, sets `art=-1` and binds it (`MarketingQueue.cs:192-199`). Native art -1 returns before QA
dispatch; it produces no campaign/hype. Own-room generic waiter gates suppress employee work independently.

The adapters address separate native gaps (`QueueIntegration.cs:53-129`):

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
(`QueueIntegration.cs:132-169`). The label is native End Automation while Running; after Stop it becomes
disabled “Automation stopped” for the surviving owned paid task, preventing resurrection of discarded
configuration through native Start Automation.

`Stop(string)` sets Running=false, removes an owned waiter, cleans an already-completed bound object,
nulls products/selection/scheduler, sets campaign=-1 and unregisters the queue. A completed native object
awaiting destruction is cleaned up separately. An unfinished paid native Marketing task remains intact;
its A/S/D are false, so it finishes once without another purchase (`MarketingQueue.cs:262-290`).

Native paid cancellation reaches `taskMarketing.Abbrechen`; its prefix Stops the owning queue while
the original refunds unfinished work and destroys the task. Generic waiter cancellation also has a
prefix. The generic cancel confirmation lacks a native taskWait dispatch, so `BeforeCancelConfirmed`
explicitly Stops owned Waiting before the native dispatcher clears the room
(`QueueIntegration.cs:47-51,171-178`). It is possible for native invalid-target cancellation to use the
same Marketing method; the queue stops rather than inferring user intent.

Bound-ID mismatch calls `Detach` and Stops the queue (`MarketingQueue.cs:115-120,292-299`). A transferred
paid task is left to its new room; there is no attempt to refund, move it back or follow it. **Waiter
qualification:** `Stop -> ReleaseWaiter` deactivates/destroys its bound waiter even after room-ID mismatch;
`ClearBinding` only clears the old room when its ID still matches (`:301-323`). The implementation therefore
does not establish preservation of a transferred owned waiter. This edge needs review/runtime evidence;
do not replace that fact with the broader intended “preserve reassigned tasks” claim.

OnDestroy Stops/unregisters the room queue. Global removal Stops all enumerated queues. Task markers
are not independent automation controllers and do not retain a configuration preset.

## Findings and Evidence

### F-001 - Admission is an unpaid purchase policy independent of paid progress

Statement: The current scheduler admits at h<=threshold, purchases only below cap with enough cash, retains a purchasable admitted product above its entry threshold, and drops admission when blocked before choosing another selected candidate.
Basis: static-observation
Standing: current
Applies to: marketing-automation-implementation
Evidence: `M/Automation/QueueScheduler.cs`, `internal ProductKey? Choose(...):65-95`, `private static bool CanPurchase(...):97-99`; `M/Automation/MarketingQueue.cs`, `private void Scan():157-190`.
Explanation: This is ongoing cap maintenance, not a finite pass or native A/S/D behavior. A product above threshold remains selected but cannot newly enter. Newly available products are absent from the copied list. Reaching cap or losing affordability/eligibility clears current admission before future selection.

### F-002 - Each dispatched campaign is one cash-funded native one-shot

Statement: Dispatch initializes native dependencies and paid task fields, sets A/S/D false, debits once, and lets native workers/completion own paid progress; replacement dispatch waits until the completed object is gone.
Basis: static-observation
Standing: current
Applies to: marketing-automation-implementation
Evidence: `M/Automation/MarketingQueue.cs`, `StartCampaign(MarketingProduct,NativeCampaignOption):201-242`, `Bind(GameObject,int):244-252`, `Completed(taskMarketing):254-260`, `LateUpdate():106-155`; `M/GameIntegration/QueueIntegration.cs:14,45`; native completion report F-001 through F-004.
Explanation: False automatic avoids native repeat's debit before a postfix could rotate. Immediate FindScripts handles a confirmation occurring before Unity Start. This preserves the native completion accounting asymmetry rather than silently correcting it. Static control flow is not proof of all Unity same-frame ordering.

### F-003 - Visible Waiting requires coordinated native-room adapters

Statement: The current mod represents Waiting by an owned art -1 taskWait, retains single-player Marketing window26 during initialization, supplies owned caption/context/automation indicators, and exposes Waiting through the leader idle predicate for Support.
Basis: static-observation
Standing: current
Applies to: marketing-automation-implementation
Evidence: `M/Automation/MarketingQueue.cs`, `CreateWaiter():192-199`; `M/GameIntegration/QueueIntegration.cs`, `BeforeRoomPanelsPruned/AfterRoomPanelsPruned:53-79`, display/room predicate/context adapters `:81-129`; native waiter and room-binding reports.
Explanation: Retention is room-type/single-player scoped before any queue exists; custom text/context is task/queue owned. It is more precise than saying every native patch is limited to an already-owned room. The waiter is excluded from native saves via separate persistence handoff.

### F-004 - Stop discards configuration while preserving unfinished paid work

Statement: The owned Stop adapter clears queue settings immediately, removes Waiting, leaves unfinished native paid work to finish, and disables native restart on that surviving owned task.
Basis: static-observation
Standing: current
Applies to: marketing-automation-implementation
Evidence: `M/Automation/MarketingQueue.cs`, `internal void Stop(string):262-290`; `M/GameIntegration/QueueIntegration.cs`, `BeforeAutomationToggle:132-146`, `BeforeAutomationLabel:148-169`, cancel prefixes `:47-51,171-178`.
Explanation: Stop is intentionally different from the unmanaged native A toggle. The native immediate Cancel route still invokes native paid refund. Stopped queue configuration is absent from subsequent active captures; historical snapshots are handled separately by persistence.

### F-005 - Task replacement stops the queue but transferred waiter cleanup needs runtime review

Statement: A changed room task ID detaches/stops execution and preserves a transferred unfinished paid task, but Stop can deactivate/destroy a bound owned waiter after transfer because ReleaseWaiter has no room-ID ownership guard.
Basis: static-observation
Standing: current
Applies to: marketing-automation-implementation
Evidence: `M/Automation/MarketingQueue.cs`, `LateUpdate():115-120`, `Stop():262-290`, `Detach():292-299`, `ReleaseWaiter():301-311`, `ClearBinding():313-323`; native `Menu_Verschiebe_Task.TaskTauschen` in room-binding report.
Explanation: This is a statically identified implementation boundary; a fix has not been validated. Runtime reachability and destination cleanup for an owned waiter require targeted review. The intended product policy cannot serve as evidence that this edge already preserves the moved task.

### F-006 - Background spending gates do not describe initial confirmation or every Support contribution

Statement: LateUpdate gates dispatch on game delta/loading/room pause, while TryStart immediately scans after explicit confirmation; paid Support contributions still follow the native leader-pause behavior rather than these scheduler gates.
Basis: static-observation
Standing: current
Applies to: marketing-automation-implementation
Evidence: `M/Automation/MarketingQueue.cs`, `TryStart(...):86-103`, `LateUpdate():137-150`; native room-support F-003 and Marketing work report F-001.
Explanation: “Pause-aware queue” describes background purchasing, not a blanket claim that no confirmation can buy while a modal pauses the game or that pausing the leader prevents unpaused assistants advancing paid work.

## Connections to Other Systems

- The form copies a confirmed selection/configuration into TryStart; draft lifecycle and independent list
  display sorting remain UI responsibilities. Persisted enum values must not be inferred from dropdown order.
- Live native campaign arrays/unlocks are reread and validated; unequal game/console cap/gain arrays are
  rejected by `NativeCampaignCatalog.Read`, not silently reconciled.
- `SuspendForSave`, `RestoreAfterSave`, `CaptureActive` and `Restore` bridge persistence. Transient owned
  waiters/completed tasks are temporarily excluded; paid tasks remain native. Restore adopts matching
  paid progress or creates an unpaid waiter without buying during load (`MarketingQueue.cs:348-506`).
  External association and numeric validation belong to the persistence report.
- Native user workflows remain available for unmanaged tasks. The patch list's broad target types do not
  imply all instances receive queue behavior; each adapter's actual guards must be inspected.

## Mod Integration Implications

Future work should preserve the boundary between pure unpaid selection, room/task binding, native paid
execution, owned status/control adaptation and external checkpoint restoration. Do not reintroduce
finite-pass bookkeeping or normalize native subordinate booleans as part of this queue model.

Keep ownership checks and deferred completion observation when changing scheduling. A native completion
postfix alone is insufficient if automatic repetition remains enabled. Changing queue priority must not
preempt paid work or discard admitted continuation above threshold. Architectural research provides
no authorization to fix native accounting, change Support pause, or add multiplayer scheduling.

## Unknowns and Validation

- Q-001: Transferred owned waiter handling in F-005 needs review before claiming all transfer cases
  preserve reassigned tasks. No implementation change or runtime experiment was performed here.
- Q-002: Same-frame completion with critical work and several employees/assistants, Stop/Cancel ordering,
  and object-ID reuse require disposable runtime checks.
- Q-003: Support leader-only pause retains native inference; assistant idle/resume during custom Waiting
  and task switching is an intended integrated path, not an individually documented test outcome here.
- Q-004: Native autosave continuity remains unverified; the recorded user reports say autosave has not
  triggered, including before the mod. No cause is attributed by this report.
- Prior evidence, recorded in `README.md:381-419` on 2026-10-21: the user accepted the functional queue/UI
  and reported successful manual new-slot, repeated-load, restart, Waiting restoration, overwrite and
  Stop persistence scenarios after IO corrections. These are user-reported observations of the accepted
  baseline, not exhaustive current-refactor runtime validation or evidence for every edge above.
- `tests/MarketingAutomation.Checks/QueueSchedulerChecks.cs:5-78` contains isolated scheduler scenarios
  for admission, caps, cash, priority, missing dates, invalid hype and blocked continuation. Test existence
  is distinct from running them. As of 2026-10-22, validation for this report did not rerun the .NET checks, compile,
  deploy, launch the game, or inspect personal saves.

## Revision Notes

- 2026-10-22: Established current-code integration account with honest working-tree revision. Separated
  mod policy from native flags, identified initialization-wide window retention, distinguished immediate
  confirmation from background pause gates, and recorded the transferred-waiter cleanup limitation.
