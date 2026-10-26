---
{
  "schema_version": 1,
  "id": "persistence-and-recovery",
  "kind": "integration",
  "title": "Marketing Automation persistence and recovery",
  "summary": "Single-player Save/Load wrappers, immutable exact-byte checkpoints, native paid-task adoption, transient exclusion, numerical compatibility, diagnostics and validation limits.",
  "scope": "mod-specific",
  "project": "MarketingAutomation",
  "domains": ["persistence", "marketing", "recovery", "integration"],
  "keywords": ["snapshot", "immutable history", "SHA-256", "Stop", "Waiting", "admission", "paid progress", "G7", "employee toggle", "diagnostics", "autosave"],
  "symbols": ["QueuePersistenceIntegration.Install", "QueuePersistence.BeginSave", "QueuePersistence.FinishSave", "QueuePersistence.BeginLoad", "QueuePersistence.FinishLoad", "RoomQueueRestorer.Restore", "QueueSnapshotCapture.Capture", "QueuePersistenceNotice", "QueueSaveCheckpoint", "QueueLoadCheckpoint", "QueueSnapshotStore", "QueueSnapshotValidation", "MarketingQueue.SuspendForSave", "MarketingQueueRegistry.CaptureActive", "MarketingQueueRegistry.SuspendForSave", "MarketingQueueRegistry.ClearForLoad", "MarketingQueue.CaptureSnapshot", "MarketingQueue.Restore", "NativeCampaignProgress", "QueueSnapshotStore.NormalizePaidProgressForComparison", "NativeGameUi.EmployeeSelectionToggle", "NativeGameUi.MessageDialog", "PersistenceDiagnostics.Record"],
  "source_sets": ["mgt2-managed", "marketing-automation-implementation"],
  "related": [
    {"id": "native-save-lifecycle", "relationship": "Provides actual native Save/Load order, ES3 sharing/number representation, and preference callback constraints"},
    {"id": "save-identity-and-task-restoration", "relationship": "Defines snapshot/object identity limits and native paid-task fields"},
    {"id": "queue-and-room-integration", "relationship": "Owns active queue, admission, Stop, Waiting, room task and Support adaptations"},
    {"id": "configuration-ui", "relationship": "Drafts are discarded on closure and are not active checkpoint data"},
    {"id": "bootstrap-and-deployment", "relationship": "Owns selected runtime/reference/patch compatibility and deployed plugin evidence"},
    {"id": "multiplayer-session-and-save-coordination", "relationship": "Documents native multiplayer research; this persistence implementation explicitly excludes MP restoration"}
  ],
  "created": "2026-10-22",
  "updated": "2026-10-26",
  "entry_points": [
    {"label": "Checkpoint contract", "anchor": "checkpoint-model-and-storage", "summary": "Schema fields, typed identities, hash/payload addressing and immutable history"},
    {"label": "Save wrapper", "anchor": "save-capture-publication-and-cleanup", "summary": "Capture, owned transient exclusion, native exception preservation and external publication"},
    {"label": "Load reconciliation", "anchor": "load-and-native-paid-task-adoption", "summary": "Header/object/task validation and adoption without payment"},
    {"label": "Native representation and preferences", "anchor": "native-representation-and-preferences",
   "summary": "G7 comparison, legacy checkpoint normalization and silent load-time toggle initialization."},
    {"label": "Validation evidence", "anchor": "unknowns-and-validation", "summary": "User-reported baseline versus isolated checks and still-unexecuted failure/autosave/MP cases"}
  ]
}
---

# Marketing Automation persistence and recovery

## Overview and Coverage

Single-player checkpoint storage, native Save/Load hooks, paid-task adoption and recovery failure
boundaries. [Saving and restoration policy](../../docs/behavior.md#saving-and-restoring) defines
intended behavior. This report records implementation mechanisms and their evidence limits.
Native serialization and task reconstruction remain owned by the game.

## Source References and Applicability

Mod source root: `src/MarketingAutomation/`, abbreviated `M/` below. Source set
`marketing-automation-implementation` locates the project sources inspected on the report's research date. Native
constraints use `mgt2-managed` and the related standalone native reports.

| Area | Inspected source |
| --- | --- |
| Patch registration and wrappers | `M/GameIntegration/QueuePersistenceIntegration.cs`: `Install`, `BeforeSave`, `AfterSave`, `BeforeLoad`, `AfterLoad`, `BeforePreferencesRestored` |
| Save/load orchestration | `M/Persistence/QueuePersistence.cs`: `BeginSave`, `FinishSave`, `RestoreSuspendedQueues`, `BeginLoad`, `FinishLoad`, `ValidateLoadedSnapshot`, `RestoreMatchingRooms` |
| Live identity and paid-task validation | `M/Persistence/RoomQueueRestorer.cs`: `Restore`, `FindMarketingRoom`, `ResolveSelectedProducts`, `ResolvePaidCampaign` |
| Transient hook state and notification | `M/Persistence/QueuePersistenceCheckpoint.cs`: `QueueSaveCheckpoint`, `QueueLoadCheckpoint`; `M/Persistence/QueuePersistenceNotice.cs`: `Show`, `Display` |
| Serialized model | `M/Persistence/QueueSnapshot.cs`: `QueueSaveSnapshot`, `RoomQueueSnapshot`, `PaidCampaignSnapshot`, `ProductIdentity` |
| Structural/header validation | `M/Persistence/QueueSnapshotValidation.cs`: `HeaderMatches`, `ValidRoom`, `HasValidPaidCampaign` |
| Store, envelopes and conflict matching | `M/Persistence/QueueSnapshotStore.cs`: `Publish`, `WriteNewEnvelope`, `Read`, `ReadEnvelope`, `Fingerprint` |
| Queue capture/suspension/adoption | `M/Automation/Runtime/MarketingQueue.cs`: `CaptureSnapshot`, `CanCaptureTaskState`, `SuspendForSave`, `RestoreAfterSave`, `Restore`, `LateUpdate`; `M/Persistence/QueueSnapshotCapture.cs`: `Capture` |
| Cross-room queue lifetime | `M/Automation/Runtime/MarketingQueueRegistry.cs`: `Register`, `Unregister`, `StopAll`, `ClearForLoad`, `CaptureActive`, `SuspendForSave`; `M/Automation/Runtime/QueueTaskOwnership.cs`: `Owner` |
| Representation comparison | `M/GameIntegration/Marketing/NativeCampaignProgress.cs`: `SerializedValue`, `Matches`; `M/Persistence/QueueSnapshotStore.cs`: `NormalizePaidProgressForComparison` |
| Native UI bindings | `M/GameIntegration/UI/NativeGameUi.cs`: `EmployeeSelectionToggle`, `ShowMessage`, `MessageDialog` |
| Persistent IO diagnostics | `M/Persistence/PersistenceDiagnostics.cs`: `Record` |
| Isolated checks | `tests/MarketingAutomation.Tests/QueuePersistenceTests.cs`; `NativeCampaignProgressTests.cs` |

F-004 records user runtime observations documented with the 2026-10-22 inspection.
No per-scenario observation date or binary hash was captured; those observations do not validate later source edits.

## Architecture and State Ownership

`QueuePersistenceIntegration` owns the Harmony save/load and preference hooks; the bootstrap installs it
separately from room/task presentation hooks. `QueuePersistence` coordinates capture, publication, cleanup,
and restoration. `QueueSaveCheckpoint` and `QueueLoadCheckpoint` are transient hook-call contexts, not serialized
contracts. `RoomQueueRestorer` resolves native rooms/products/tasks and validates paid-task ownership before
calling `MarketingQueue.Restore`. `QueueSnapshotCapture` converts live queue state to the existing snapshot
schema. `MarketingQueue` in `MarketingAutomation.Automation.Runtime` retains private room state, task bindings,
save-suspension mutations and capture-eligibility decisions. `MarketingQueueRegistry` in the same namespace owns
the active-queue set and cross-room shutdown, load clearing, capture and suspension enumeration. Destructive
shutdown/load operations enumerate a copy; capture/suspension retain live-set enumeration. The runtime
`QueueTaskOwnership` marker can remain on paid work after Stop without retaining queue configuration.

`NativeCampaignProgress` in `MarketingAutomation.GameIntegration.Marketing` accepts scalar values only;
checkpoint traversal and normalization belong to `QueueSnapshotStore`. Native UI slots used by persistence
are resolved through `MarketingAutomation.GameIntegration.UI.NativeGameUi`.
`QueuePersistenceNotice` owns the deferred native message-box coroutine. These boundaries are documented from
the 2026-10-26 static refactor review; they do not establish runtime equivalence.

### Checkpoint model and storage

`QueueSaveSnapshot` is schema 1 with game MVID string, native format, company ID and room records. Each
`RoomQueueSnapshot` stores room ID, native campaign index, integer threshold, persisted priority/direction enum
values, selected typed product identities, optional admitted product, and optional paid-task checkpoint.
`PaidCampaignSnapshot` stores task ID, typed target, total points and remaining points. ProductIdentity is
kind/native ID; Unity runtime instance IDs are deliberately absent from persisted identity. These DTOs remain
in `MarketingAutomation.Persistence`, and the nested `QueueSnapshotStore.SnapshotEnvelope` retains its identity.
Runtime and native-adapter namespace moves do not change serialized type/member names, order or enum values.

`QueueSnapshotValidation.ValidRoom` rejects invalid room/campaign/enums, empty/duplicate selections, invalid
typed products, admission outside selection, inconsistent paid target/admission, and nonfinite or impossible
work (`M/Persistence/QueueSnapshotValidation.cs`, `ValidRoom` and `HasValidPaidCampaign`). Threshold nonnegativity
is structural; restoration
clamps to the current selected campaign's cap. Header checks require schema, game MVID, native format and
company to match exactly (`QueueSnapshotValidation.HeaderMatches`). No migration from an unknown newer schema is attempted.

`QueuePersistence.ResolveNativeSavePath` creates native ES3 settings, requires File location and resolves the same filename
to an absolute effective path. The store root is the native file's parent plus `MarketingAutomation/Snapshots`
(`M/Persistence/QueuePersistence.cs`, `ResolveNativeSavePath` and `OpenSnapshotStore`). This is resolved from
effective settings, not a hardcoded machine path.

Storage layout is `<root>/<native SHA-256>/<payload SHA-256>.json`. Each envelope contains schema, native
digest/length, payload digest and Base64 JSON payload. Records are limited to 16 MiB. Native files are opened
for reading only; their bytes are not rewritten by the store. A complete record is written to a unique `.tmp`,
flushed with `Flush(true)`, closed and moved to the immutable destination. Identical existing payload records
are integrity-checked and reused (`M/Persistence/QueueSnapshotStore.cs`, `Publish` and `WriteNewEnvelope`).

There is no mutable per-slot index, automatic history purge, or latest-record preference in this implementation.
Path changes can match identical bytes. Another process/native operation changing bytes may invalidate matching.
Checksums detect integrity mismatch; they are not authentication against an intentional writer.

## Behavior and Call Flows

### Save capture, publication, and cleanup

1. `QueuePersistenceIntegration.Install` patches native `savegameScript.Save(int)` with a prefix and finalizer. The prefix
   calls `QueuePersistence.BeginSave`; the finalizer calls `FinishSave` with the actual native writer and
   exception, then returns the same exception (`M/GameIntegration/QueuePersistenceIntegration.cs`, `BeforeSave` and
   `AfterSave`).
   There is no replacement serializer or suppression of the original Save.
2. `BeginSave` resolves the company and returns early for absent company/MP. In SP it resolves the path and
   captures active queues with MVID/format/company header. Capture failures are recorded but do not become
   native Save success. A finally block calls `MarketingQueueRegistry.SuspendForSave(save)`
   (`M/Persistence/QueuePersistence.cs`, `BeginSave`).
3. Capture includes only Running, Configured queues with valid room/save ownership and an owned current task
   or recognized completion boundary. Selected identities/rooms are deterministically sorted. Paid work is
   included only for an unfinished ordinary task; total/left are normalized using the saved numeric representation
   (`M/Automation/Runtime/MarketingQueueRegistry.cs`, `CaptureActive`;
   `M/Automation/Runtime/MarketingQueue.cs`, `CaptureSnapshot` and `CanCaptureTaskState`;
   `M/Persistence/QueueSnapshotCapture.cs`, `Capture`). Replaced/transferred/canceled work is not a valid resume
   checkpoint.
4. Registry suspension delegates to each queue, targeting only owned transient `taskWait` or completed tasks
   awaiting cleanup. Each queue marks suspension and records itself in the cleanup list before deactivating the
   task, then clears the room's task ID/cache. This preserves the obligation to restore bindings before native
   `SetActive(false)` callbacks can run and leaves unfinished paid tasks native
   (`M/Automation/Runtime/MarketingQueue.cs`, `SuspendForSave`). This keeps the transient object out of native tagged enumeration
   and the room's temporary binding out of `SaveRooms`.
5. Original native Save performs serialization/commit. If it throws, the finalizer records failure and publishes
   no new checkpoint. If it returns normally, `FinishSave` requires the same File destination in actual writer
   settings, then publishes the captured snapshot against the completed file
   (`M/Persistence/QueuePersistence.cs`, `FinishSave`).
6. Cleanup runs in finally through `QueuePersistence.RestoreSuspendedQueues`. `MarketingQueue.RestoreAfterSave`
   reactivates/rebinds only if the room/task still exist and the room
   remains taskless; conflicting changes detach the queue rather than reclaim another task. Cleanup failure
   stops automation and is logged. Native exceptions remain native exceptions.

A native commit followed by a later native exception causes no external publication under this implementation.
That is a conservative failure result, not proof that native bytes stayed unchanged. A crash between native and
external publication can leave a valid native file without matching automation. The two files are not atomic.

### Load and native paid-task adoption

1. Load prefix invokes `BeginLoad`. `MarketingQueueRegistry.ClearForLoad` first clears old queues/references
   for the native save owner, then the coordinator skips
   SP restoration for multiplayer. In SP it reads the exact native digest/length and matching record candidates
   (`M/Persistence/QueuePersistence.cs`, `BeginLoad`). The read handle is disposed before native loading, respecting
   ES3's `FileShare.None` constraint.
2. `QueueSnapshotStore.Read` examines only `.json` records under that digest. It validates envelope/schema/
   native length/payload checksum, deserializes, normalizes historical paid floats for comparable payloads, and
   rejects genuinely differing payloads as Conflicting. A damaged candidate returns Damaged rather than silently
   choosing another candidate. `.tmp` records are ignored (`M/Persistence/QueueSnapshotStore.cs`, `Read`).
3. Original native Load reconstructs paid tasks. The mod's postfix runs only on successful normal return.
   `FinishLoad` requires SP and a cleared native loading flag, rehashes bytes to detect change across loading,
   then requires matching header and valid unique room records before resolving live objects
   (`M/Persistence/QueuePersistence.cs`, `FinishLoad` and `ValidateLoadedSnapshot`). Before/after equality is an
   association check, not an atomic
   proof against all possible external replace-and-restore races.
4. `RoomQueueRestorer.Restore` requires a unique matching Marketing room (type 6), the same company, and unique matching
   selected products. Missing selected products are omitted from currently resolved products; saved identities
   remain in the queue's captured selection set. A missing paid target rejects that room's restore.
5. Paid checkpoints must match room taskID, a unique named native Task object, task ID, product kind/ID,
   campaign and all three false native flags. Total/left must match the representation rule. No second room may
   reference the same paid task. A waiting checkpoint instead requires `room.taskID == -1`
   (`M/Persistence/RoomQueueRestorer.cs`, `ResolvePaidCampaign`, `FindMatchingPaidCampaign`, `ValidatePaidProgress`
   and `ValidateExclusiveRoomAssignment`).
6. `MarketingQueue.Restore` rebuilds the scheduler, clamps threshold, restores admission when its product is
   resolved, and delays execution past the current frame. For paid state it initializes references and binds
   the existing paid object; otherwise it creates a dormant owned waiter. Neither branch purchases a campaign
   (`M/Automation/Runtime/MarketingQueue.cs`, `Restore`). Background scanning additionally requires Running, SP, unpaused
   room, cleared loading and positive game delta (`MarketingQueue.LateUpdate`). Native Load itself ends paused.

Header/duplicate-record failures reject the snapshot. `QueuePersistence.RestoreMatchingRooms` catches live
binding failures per room, so other independently valid room records can restore. Errors produce warnings and a
deferred native message box through
`QueuePersistenceNotice.Display`; missing records merely log that native paid campaigns remain unchanged. This is
not a whole-snapshot transactional restore.

### Native representation and preferences

`NativeCampaignProgress.SerializedValue(float)` explicitly converts through invariant `G7` text and float.Parse.
`Matches` accepts exact equality or equality after checkpoint serialization, with finite guards
(`M/GameIntegration/Marketing/NativeCampaignProgress.cs`, `SerializedValue` and `Matches`).
`QueueSnapshotStore.NormalizePaidProgressForComparison` traverses only deserialized checkpoint values and calls
the scalar converter. It does not write `paid.points` or `paid.pointsLeft`. Historical records are normalized
for conflict comparison, so excess pre-save digits alone do not produce a false conflicting-settings result.

The native writer uses invariant default float formatting, not an explicit `G7`. This mod encodes the recorded
installed legacy-Mono behavior; its isolated checks emulate that representation on modern .NET. Source inspection
alone does not prove every runtime/version formats identically. Task/product/campaign/flag checks remain strict,
and nonfinite or materially different progress is rejected.

`QueuePersistenceIntegration.BeforePreferencesRestored` applies only while native loading is active in SP.
It seeds `NativeGameUi.EmployeeSelectionToggle` (native `GUI_Main.uiObjects[2]`) using `SetIsOnWithoutNotify`
from the stored preference before native `InitToggles`. It does not change PlayerPrefs or bypass normal
user-click confirmation. `QueuePersistenceNotice.Display` similarly uses `NativeGameUi.ShowMessage` and
`MessageDialog` (native slot 40) after yielding one frame, then raises that dialog above other menus.
These named bindings are in `M/GameIntegration/UI/NativeGameUi.cs`; their call sites remain in
`M/GameIntegration/QueuePersistenceIntegration.cs` and `M/Persistence/QueuePersistenceNotice.cs`.
The native preference callback path is documented in native-save-lifecycle F-005.

### Diagnostics

`PersistenceDiagnostics.Record` appends UTC timestamp, stage/reason and exception to
`game_directory` + `BepInEx/MarketingAutomation.IO.log`; failure to append falls back to a loader warning
(`M/Persistence/PersistenceDiagnostics.cs`, `Record`). Errors distinguish capture, native Save, publication, cleanup,
read, snapshot validation and per-room restoration, including expected/loaded work when progress differs.
Append-only diagnostics survive normal per-session log replacement; they do not guarantee successful logging
under disk or permission failure.

## Findings and Evidence

### F-001 - External association is exact content with immutable history

Statement: The store associates records with native SHA-256 and length, retains historical payloads, and rejects conflicting valid settings for identical native bytes instead of selecting the latest.

Basis: static-observation

Standing: current

Applies to: marketing-automation-implementation

Evidence: `M/Persistence/QueueSnapshotStore.cs`, `Fingerprint`, `Publish`, `Read`, `ReadEnvelope`.

Explanation: Exact copies can match history, but the store cannot infer copy provenance. A corrupted candidate poisons matching for that content rather than being silently ignored. There is no automatic retention cleanup.

### F-002 - The wrapper preserves native execution and failure

Statement: Save uses an ordinary native method with capture prefix and exception-preserving cleanup finalizer; only owned transient waiters/completed bindings are excluded temporarily.

Basis: static-observation

Standing: current

Applies to: marketing-automation-implementation

Evidence: `M/GameIntegration/QueuePersistenceIntegration.cs`, `BeforeSave` and `AfterSave`; `M/Persistence/QueuePersistence.cs`, `BeginSave` and `FinishSave`; `M/Automation/Runtime/MarketingQueue.cs`, `SuspendForSave` and `RestoreAfterSave`.

Explanation: Native paid work remains in the native file. External publication failure is reported separately and cannot make native Save atomic with the record. The autosave scheduler itself is not patched.

### F-003 - Paid restoration adopts rather than purchases

Statement: A matched checkpoint binds an already-restored paid native task only after header, identity, flags, assignment and representation-aware progress validation; the restore path does not Pay or create a duplicate paid campaign.

Basis: static-observation

Standing: current

Applies to: marketing-automation-implementation

Evidence: `M/Persistence/QueuePersistence.cs`, `FinishLoad` and `ValidateLoadedSnapshot`; `M/Persistence/RoomQueueRestorer.cs`, `Restore`; `M/Automation/Runtime/MarketingQueue.cs`, `Restore`; `M/GameIntegration/Marketing/NativeCampaignProgress.cs`, `SerializedValue` and `Matches`.

Explanation: Admission survives loading above the entry threshold. Threshold clamp changes custom settings only; native work remains authoritative. Waiting creates an owned dormant waiter after native load, with no purchase while paused.

### F-004 - User acceptance supports manual continuity within stated limits

Statement: User observations support repeated manual save/load, full restart, Waiting restoration, overwrites and stopped-state persistence for the accepted baseline.

Basis: runtime-observation

Standing: current

Applies to: marketing-automation-implementation

Evidence: User-reported gameplay documented with the 2026-10-22 inspection, source set
`marketing-automation-implementation`; per-scenario observation dates, build fingerprints and independent replays
were not recorded.

Explanation: The user started automation from a pre-mod save, saved into a new slot and repeatedly loaded
its observed state. Reported coverage also includes full restart, Waiting, overwrites and Stop remaining
stopped. Exact cash/work comparisons and proposed corruption/failure/removal cases were not individually
recorded. Autosave did not trigger in the user's testing; its cause is unknown. Acceptance applies to that
baseline, not subsequent readability or UI donor-copy edits.

## Connections to Other Systems

Native [save lifecycle](../../tools/mgt2-mod-toolkit/research/systems/native-save-lifecycle.md) owns serializer
order, exclusive file sharing,
numeric text and UI preference side effects. Native
[identity/restoration](../../tools/mgt2-mod-toolkit/research/systems/save-identity-and-task-restoration.md)
owns the snapshot-local identity limit. [Queue/room integration](queue-and-room-integration.md) owns lifecycle,
admission, Stop and dormant waiter semantics. [Configuration UI](configuration-ui.md) owns draft cancellation.

[Native multiplayer
coordination](../../tools/mgt2-mod-toolkit/research/systems/multiplayer-session-and-save-coordination.md) remains
research
for future scope: BeginSave/BeginLoad/FinishLoad explicitly skip MP checkpoint restore/publication, and background
queue scanning rejects MP. The presence of native MP hooks through the shared method is not a support promise.

## Mod Integration Implications

Preserve serialized type/member names, enum values, content/payload envelopes and equivalent legacy
progress normalization. An unknown schema disables restoration; compatibility work needs explicit handling.
Do not coerce malformed settings into running work or rewrite loaded native progress to make it match.

A save after Stop can contain unfinished paid work but no active queue record. If different external
configurations match identical native bytes, conflict rejection prevents selecting either by recency.
Native and external commits are separate; a valid native save may exist without a matching checkpoint.

## Unknowns and Validation

| Evidence | Scope and limit |
| --- | --- |
| Source inspection, 2026-10-22 | Wrappers, store, model, validation, queue adoption and native constraints; no Unity/Harmony execution. |
| Refactor inspection, 2026-10-26 | Extracted hook registration, registry enumeration, room restoration, snapshot capture, scalar progress conversion and native UI bindings; compared control flow, exception cleanup, payment/adoption boundaries and serialized contracts. Static review only; no game execution. |
| [QueuePersistenceTests.cs](../../tests/MarketingAutomation.Tests/QueuePersistenceTests.cs) | Disposable files and model validation; not native saves, real writer compatibility or Unity task adoption. |
| [NativeCampaignProgressTests.cs](../../tests/MarketingAutomation.Tests/NativeCampaignProgressTests.cs) | Representation/legacy-normalization checks on modern .NET; not an installed-Mono experiment. |
| User reports in F-004 | Manual continuity of the accepted baseline; no exhaustive recovery or subsequent-source acceptance. |

- **Q-001 — Autosave:** the common Save method is hooked, but timer-triggered restoration remains unverified.
  The mod does not modify the scheduler, preferences or countdown; the missing trigger's cause is unknown.
- **Q-002 — Failure/removal:** native write failure, disk-full/power loss, interrupted publication, damaged/missing
  histories and mod removal need disposable native scenarios before being called runtime-passed.
- **Q-003 — Unattributed warning:** a nonreproducible mid-campaign warning lacks its original diagnostic log;
  its cause cannot be assigned to the known numeric or preference-callback defects.
- **Q-004 — Cross-runtime/Cloud:** G7 compatibility on other native runtimes and external-directory Cloud
  synchronization are unverified. Multiplayer remains outside the supported scope.
- **Q-005 — Restore atomicity:** independently valid rooms may restore when another room fails; native and
  external files commit separately. There is no whole-restore rollback guarantee.

### Runtime regression checks

Use disposable single-player saves for changed persistence paths:

- Save/load during paid work and Waiting. Check settings, admitted continuation above threshold,
  remaining work, no duplicate charge and automatic Waiting resume. Save after Stop; no queue should restart.
- Compare another slot, overwrite, older state and byte-identical copy with their exact matching records.
  Missing, damaged or conflicting records must leave automation off while native work loads normally.
- Exercise timer-triggered autosave, failed native writing/publication and mod removal when feasible.
  Check transient Marketing waiters are excluded and live bindings recover after saving or failure.
- Check employee-toggle preferences load silently while normal manual clicks retain native confirmation.

Inspect `game_directory` + `BepInEx/MarketingAutomation.IO.log` and `BepInEx/LogOutput.log`.
These are proposed native integration checks, not individual pass claims.
