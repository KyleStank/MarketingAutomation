---
{
  "schema_version": 1,
  "id": "persistence-and-recovery",
  "kind": "integration",
  "title": "Marketing Automation persistence and recovery",
  "summary": "Current single-player Save/Load wrappers, immutable exact-byte checkpoint history, native paid-task adoption, transient waiter exclusion, numerical correction, diagnostics, and validation limits.",
  "scope": "mod-specific",
  "project": "MarketingAutomation",
  "code_revision": "Working source inspected on 2026-10-22; includes uncommitted changes; citations identify the relevant files and symbols",
  "domains": ["persistence", "marketing", "recovery", "integration"],
  "keywords": ["snapshot", "immutable history", "SHA-256", "Stop", "Waiting", "admission", "paid progress", "G7", "employee toggle", "diagnostics", "autosave"],
  "symbols": ["QueueIntegration.Install", "QueuePersistence.BeginSave", "QueuePersistence.FinishSave", "QueuePersistence.BeginLoad", "QueuePersistence.FinishLoad", "QueuePersistence.RestoreRoom", "QueueSnapshotStore", "QueueSnapshotValidation", "MarketingQueue.SuspendForSave", "MarketingQueue.CaptureActive", "MarketingQueue.Restore", "NativeCampaignProgress", "PersistenceDiagnostics.Record"],
  "source_sets": ["mgt2-managed-2026-10-22", "marketing-automation-implementation"],
  "related": [
    {"id": "native-save-lifecycle", "relationship": "Provides actual native Save/Load order, ES3 sharing/number representation, and preference callback constraints"},
    {"id": "save-identity-and-task-restoration", "relationship": "Defines snapshot/object identity limits and native paid-task fields"},
    {"id": "queue-and-room-integration", "relationship": "Owns active queue, admission, Stop, Waiting, room task and Support adaptations"},
    {"id": "configuration-ui", "relationship": "Drafts are discarded on closure and are not active checkpoint data"},
    {"id": "bootstrap-and-deployment", "relationship": "Owns selected runtime/reference/patch compatibility and deployed plugin evidence"},
    {"id": "multiplayer-session-and-save-coordination", "relationship": "Documents native multiplayer research; this persistence implementation explicitly excludes MP restoration"}
  ],
  "created": "2026-10-22",
  "updated": "2026-10-22",
  "entry_points": [
    {"label": "Checkpoint contract", "anchor": "checkpoint-model-and-storage", "summary": "Schema fields, typed identities, hash/payload addressing and immutable history"},
    {"label": "Save wrapper", "anchor": "save-capture-publication-and-cleanup", "summary": "Capture, owned transient exclusion, native exception preservation and external publication"},
    {"label": "Load reconciliation", "anchor": "load-and-native-paid-task-adoption", "summary": "Header/object/task validation and adoption without payment"},
    {"label": "IO corrections", "anchor": "rounding-and-preference-corrections", "summary": "G7 checkpoint comparison, historical normalization, and silent load-time toggle initialization"},
    {"label": "Validation evidence", "anchor": "unknowns-and-validation", "summary": "User-reported baseline versus isolated checks and still-unexecuted failure/autosave/MP cases"}
  ]
}
---

# Marketing Automation persistence and recovery

## Overview and Coverage

This report describes the implemented single-player checkpoint integration, not a generic native save feature.
It preserves active selected-product automation across native saves without adding fields to `.sav` files.
Matching restored paid campaigns are adopted; missing, incompatible, damaged or conflicting associations leave
automation off while ordinary native paid work remains available.

Product policy is authoritative in [REQUIREMENTS.md](../../REQUIREMENTS.md), particularly Persistence and I/O,
Stop, Waiting, and multiplayer exclusions. Current policy persists only active queues, including Waiting. Stop
immediately discards configuration; stopped settings are not presets. Historical snapshots of earlier native
states remain retained. Open configuration-form drafts are not durable queue settings.

The code inspected here is the working tree recorded in metadata. Validation as of 2026-10-22 is static source
inspection; native save execution and gameplay acceptance scenarios were not rerun. Personal saves were not read.

## Source References and Applicability

Mod source root: `src/MarketingAutomation/`, abbreviated `M/` below. Source set
`marketing-automation-implementation` locates the project sources inspected on the report's research date. Native
constraints use `mgt2-managed-2026-10-22` and the related standalone native reports.

| Area | Inspected source |
| --- | --- |
| Patch registration and wrappers | `M/GameIntegration/QueueIntegration.cs:12–30,180–205` |
| Capture, publication, load and room validation | `M/Persistence/QueuePersistence.cs:25–350` |
| Serialized model | `M/Persistence/QueueSnapshot.cs:7–69` |
| Structural/header validation | `M/Persistence/QueueSnapshotValidation.cs:7–41` |
| Store, envelopes and conflict matching | `M/Persistence/QueueSnapshotStore.cs:8–176` |
| Queue capture/suspension/adoption | `M/Automation/MarketingQueue.cs:348–507`; scheduling gates `:106–155` |
| Representation comparison | `M/GameIntegration/NativeCampaignProgress.cs:6–37` |
| Persistent IO diagnostics | `M/Persistence/PersistenceDiagnostics.cs:5–21` |
| Isolated checks | `tests/MarketingAutomation.Checks/QueuePersistenceChecks.cs`; `NativeCampaignProgressChecks.cs` |

Runtime observations are project-recorded user reports, summarized in README's Working version-one baseline
and the active project instructions. No additional runtime observation supports this report as of 2026-10-22. They describe
the accepted build around 2026-10-21, without a captured binary hash per individual scenario. Avoid converting
those reports into exact financial comparisons or a claim that every listed regression case was exercised.

## Architecture and State Ownership

### Checkpoint model and storage

`QueueSaveSnapshot` is schema 1 with game MVID string, native format, company ID and room records. Each
`RoomQueueSnapshot` stores room ID, native campaign index, integer threshold, persisted priority/direction enum
values, selected typed product identities, optional admitted product, and optional paid-task checkpoint.
`PaidCampaignSnapshot` stores task ID, typed target, total points and remaining points. ProductIdentity is
kind/native ID; Unity runtime instance IDs are deliberately absent from persisted identity.

`QueueSnapshotValidation.ValidRoom` rejects invalid room/campaign/enums, empty/duplicate selections, invalid
typed products, admission outside selection, inconsistent paid target/admission, and nonfinite or impossible
work (`M/Persistence/QueueSnapshotValidation.cs:11–41`). Threshold nonnegativity is structural; restoration
clamps to the current selected campaign's cap. Header checks require schema, game MVID, native format and
company to match exactly (`:7–10`). No migration from an unknown newer schema is attempted.

`QueuePersistence.NativePath` creates native ES3 settings, requires File location and resolves the same filename
to an absolute effective path. The store root is the native file's parent plus `MarketingAutomation/Snapshots`
(`M/Persistence/QueuePersistence.cs:41–53`). This is resolved from effective settings, not a hardcoded machine path.

Storage layout is `<root>/<native SHA-256>/<payload SHA-256>.json`. Each envelope contains schema, native
digest/length, payload digest and Base64 JSON payload. Records are limited to 16 MiB. Native files are opened
for reading only; their bytes are not rewritten by the store. A complete record is written to a unique `.tmp`,
flushed with `Flush(true)`, closed and moved to the immutable destination. Identical existing payload records
are integrity-checked and reused (`M/Persistence/QueueSnapshotStore.cs:14–77`).

There is no mutable per-slot index, automatic history purge, or latest-record preference in this implementation.
Path changes can match identical bytes. Another process/native operation changing bytes may invalidate matching.
Checksums detect integrity mismatch; they are not authentication against an intentional writer.

## Behavior and Call Flows

### Save capture, publication, and cleanup

1. `QueueIntegration.Install` patches native `savegameScript.Save(int)` with a prefix and finalizer. The prefix
   calls `QueuePersistence.BeginSave`; the finalizer calls `FinishSave` with the actual native writer and
   exception, then returns the same exception (`M/GameIntegration/QueueIntegration.cs:26–30,197–205`).
   There is no replacement serializer or suppression of the original Save.
2. `BeginSave` resolves the company and returns early for absent company/MP. In SP it resolves the path and
   captures active queues with MVID/format/company header. Capture failures are recorded but do not become
   native Save success. A finally block calls `MarketingQueue.SuspendForSave(save)`
   (`M/Persistence/QueuePersistence.cs:55–85`).
3. Capture includes only Running, Configured queues with valid room/save ownership and an owned current task
   or recognized completion boundary. Selected identities/rooms are deterministically sorted. Paid work is
   included only for an unfinished ordinary task; total/left are normalized using the saved numeric representation
   (`M/Automation/MarketingQueue.cs:402–449`). Replaced/transferred/canceled work is not a valid resume checkpoint.
4. Suspension targets only owned transient `taskWait` or completed tasks awaiting cleanup. It marks suspension,
   deactivates that task, clears the room's task ID/cache, and leaves unfinished paid tasks native
   (`M/Automation/MarketingQueue.cs:348–366`). This keeps the transient object out of native tagged enumeration
   and the room's temporary binding out of `SaveRooms`.
5. Original native Save performs serialization/commit. If it throws, the finalizer records failure and publishes
   no new checkpoint. If it returns normally, `FinishSave` requires the same File destination in actual writer
   settings, then publishes the captured snapshot against the completed file
   (`M/Persistence/QueuePersistence.cs:88–150`).
6. Cleanup runs in finally. `RestoreAfterSave` reactivates/rebinds only if the room/task still exist and the room
   remains taskless; conflicting changes detach the queue rather than reclaim another task. Cleanup failure
   stops automation and is logged. Native exceptions remain native exceptions.

A native commit followed by a later native exception causes no external publication under this implementation.
That is a conservative failure result, not proof that native bytes stayed unchanged. A crash between native and
external publication can leave a valid native file without matching automation. The two files are not atomic.

### Load and native paid-task adoption

1. Load prefix invokes `BeginLoad`. It first clears old queues/references for the native save owner, then skips
   SP restoration for multiplayer. In SP it reads the exact native digest/length and matching record candidates
   (`M/Persistence/QueuePersistence.cs:152–173`). The read handle is disposed before native loading, respecting
   ES3's `FileShare.None` constraint.
2. `QueueSnapshotStore.Read` examines only `.json` records under that digest. It validates envelope/schema/
   native length/payload checksum, deserializes, normalizes historical paid floats for comparable payloads, and
   rejects genuinely differing payloads as Conflicting. A damaged candidate returns Damaged rather than silently
   choosing another candidate. `.tmp` records are ignored (`M/Persistence/QueueSnapshotStore.cs:79–139`).
3. Original native Load reconstructs paid tasks. The mod's postfix runs only on successful normal return.
   `FinishLoad` requires SP and a cleared native loading flag, rehashes bytes to detect change across loading,
   then requires matching header and valid unique room records before resolving live objects
   (`M/Persistence/QueuePersistence.cs:175–247`). Before/after equality is an association check, not an atomic
   proof against all possible external replace-and-restore races.
4. `RestoreRoom` requires a unique matching Marketing room (type 6), the same company, and unique matching
   selected products. Missing selected products are omitted from currently resolved products; saved identities
   remain in the queue's captured selection set. A missing paid target rejects that room's restore.
5. Paid checkpoints must match room taskID, a unique named native Task object, task ID, product kind/ID,
   campaign and all three false native flags. Total/left must match the representation rule. No second room may
   reference the same paid task. A waiting checkpoint instead requires `room.taskID == -1`
   (`M/Persistence/QueuePersistence.cs:249–339`).
6. `MarketingQueue.Restore` rebuilds the scheduler, clamps threshold, restores admission when its product is
   resolved, and delays execution past the current frame. For paid state it initializes references and binds
   the existing paid object; otherwise it creates a dormant owned waiter. Neither branch purchases a campaign
   (`M/Automation/MarketingQueue.cs:452–507`). Background scanning additionally requires Running, SP, unpaused
   room, cleared loading and positive game delta (`:106–155`). Native Load itself ends paused.

Header/duplicate-record failures reject the snapshot. Live binding failures are caught per room, so other
independently valid room records can restore. Errors produce warnings and a deferred native message box; missing
records merely log that native paid campaigns remain unchanged. This is not a whole-snapshot transactional restore.

### Rounding and preference corrections

`NativeCampaignProgress.SerializedValue(float)` explicitly converts through invariant `G7` text and float.Parse.
`Matches` accepts exact equality or equality after checkpoint serialization, with finite guards. Normalization
touches only deserialized checkpoint values (`M/GameIntegration/NativeCampaignProgress.cs:6–37`). It does not
write `paid.points` or `paid.pointsLeft`. Historical records are normalized for conflict comparison, so excess
pre-save digits alone do not produce a false conflicting-settings result.

The native writer uses invariant default float formatting, not an explicit `G7`. This mod encodes the recorded
installed legacy-Mono behavior; its isolated checks emulate that representation on modern .NET. Source inspection
alone does not prove every runtime/version formats identically. Task/product/campaign/flag checks remain strict,
and nonfinite or materially different progress is rejected.

`BeforePreferencesRestored` applies only while native loading is active in SP. It seeds only the employee-select
toggle (`uiObjects[2]`) using `SetIsOnWithoutNotify` from the stored preference before native `InitToggles`
(`M/GameIntegration/QueueIntegration.cs:180–190`). It does not change PlayerPrefs or bypass normal user-click
confirmation. The native callback path is documented in native-save-lifecycle F-005.

### Diagnostics

`PersistenceDiagnostics.Record` appends UTC timestamp, stage/reason and exception to
`BepInEx/MarketingAutomation.IO.log`; failure to append falls back to a loader warning
(`M/Persistence/PersistenceDiagnostics.cs:5–21`). Errors distinguish capture, native Save, publication, cleanup,
read, snapshot validation and per-room restoration, including expected/loaded work when progress differs.
Append-only diagnostics survive normal per-session log replacement; they do not guarantee successful logging
under disk or permission failure.

## Findings and Evidence

### F-001 - External association is exact content with immutable history

Statement: The store associates records with native SHA-256 and length, retains historical payloads, and rejects conflicting valid settings for identical native bytes instead of selecting the latest.

Basis: static-observation

Standing: current

Applies to: marketing-automation-implementation

Evidence: `M/Persistence/QueueSnapshotStore.cs:21–139`, `Fingerprint`, `Publish`, `Read`, `ReadEnvelope`.

Explanation: Exact copies can match history, but the store cannot infer copy provenance. A corrupted candidate poisons matching for that content rather than being silently ignored. There is no automatic retention cleanup.

### F-002 - The wrapper preserves native execution and failure

Statement: Save uses an ordinary native method with capture prefix and exception-preserving cleanup finalizer; only owned transient waiters/completed bindings are excluded temporarily.

Basis: static-observation

Standing: current

Applies to: marketing-automation-implementation

Evidence: `M/GameIntegration/QueueIntegration.cs:26–30,197–205`; `M/Persistence/QueuePersistence.cs:55–150`; `M/Automation/MarketingQueue.cs:348–385`.

Explanation: Native paid work remains in the native file. External publication failure is reported separately and cannot make native Save atomic with the record. The autosave scheduler itself is not patched.

### F-003 - Paid restoration adopts rather than purchases

Statement: A matched checkpoint binds an already-restored paid native task only after header, identity, flags, assignment and representation-aware progress validation; the restore path does not Pay or create a duplicate paid campaign.

Basis: static-observation

Standing: current

Applies to: marketing-automation-implementation

Evidence: `M/Persistence/QueuePersistence.cs:175–339`; `M/Automation/MarketingQueue.cs:452–507`; `M/GameIntegration/NativeCampaignProgress.cs:6–37`.

Explanation: Admission survives loading above the entry threshold. Threshold clamp changes custom settings only; native work remains authoritative. Waiting creates an owned dormant waiter after native load, with no purchase while paused.

### F-004 - User acceptance supports manual continuity within stated limits

Statement: The project records user-reported successful repeated manual save/load, full restart and Waiting restoration, with no further reported manual-save issues after IO corrections.

Basis: runtime-observation

Standing: current

Applies to: marketing-automation-implementation

Evidence: `README.md`, Working version-one baseline and native acceptance checklist; current project working instructions, build/validation evidence recorded for 2026-10-21.

Explanation: Observer is the user; this finding relies on the previously recorded observations. Initial scenario loaded a pre-mod save,
started automation, saved to a new slot and repeatedly observed restored state. Later reports include restart,
Waiting, stopped queues/overwrites and no remaining manual-save problems. Exact per-scenario binary hashes,
cash/work comparisons and every proposed corruption/failure/removal edge were not independently recorded.
Autosave never triggered in reported testing, also reportedly before installing the mod; no cause is established.

## Connections to Other Systems

Native [save lifecycle](../../tools/mgt2-mod-toolkit/research/systems/native-save-lifecycle.md) owns serializer order, exclusive file sharing,
numeric text and UI preference side effects. Native [identity/restoration](../../tools/mgt2-mod-toolkit/research/systems/save-identity-and-task-restoration.md)
owns the snapshot-local identity limit. [Queue/room integration](queue-and-room-integration.md) owns lifecycle,
admission, Stop and dormant waiter semantics. [Configuration UI](configuration-ui.md) owns draft cancellation.

[Native multiplayer coordination](../../tools/mgt2-mod-toolkit/research/systems/multiplayer-session-and-save-coordination.md) remains research
for future scope: BeginSave/BeginLoad/FinishLoad explicitly skip MP checkpoint restore/publication, and background
queue scanning rejects MP. The presence of native MP hooks through the shared method is not a support promise.

## Mod Integration Implications

Future maintainability work must preserve serialized type/member names, enum values, content/payload envelope
contracts and equivalent historical progress normalization. A new schema needs explicit compatibility handling;
unknown versions currently stop restoration. Do not coerce malformed settings into apparently valid running work.

Stop is not a dormant preset: stopped queues do not enter `CaptureActive`. The already-paid native task may
still finish after Stop without any queue restart. Previously saved active snapshots remain historical state;
a subsequent stopped snapshot contains no active record. Conflicting different external configurations against
the exact same native bytes deliberately disable matching rather than guessing.

External record transfer must accompany native snapshots to another machine; Cloud coverage is unestablished.
There is no automatic repair, deletion/history purge or fuzzy adoption policy. Adding one would be a product
change. None of those features should be inferred from the presence of checksummed files.

## Unknowns and Validation

| Evidence class | What exists | Practical limit |
| --- | --- | --- |
| Static inspection on 2026-10-22 | Reopened production wrappers/store/model/validation/queue/progress/diagnostic code and native constraints | Does not execute Unity, Harmony, real Save/Load or filesystem failure modes. |
| Isolated checks present in source | Disposable fake-native files cover roundtrip, native-byte preservation, copies, slot reuse/history, empty/stopped snapshots, conflicts, corruption, temporary records, schema/header rejection, typed IDs, restored admission and rounded legacy records | Files contain test text, not native game data; checks do not prove Unity task adoption or native writer compatibility. They were read, not rerun for this report. |
| Numeric checks present in source | `NativeCampaignProgressChecks` uses `123.45678f`, finite/changed-progress rejection, en-US/fr-FR and idempotent G7 normalization | Emulates installed-formatter behavior on the check runtime; not a new installed-Mono experiment. |
| User runtime evidence | Accepted manual-save/restart/Waiting baseline, no further issues reported after IO corrections | Does not establish all proposed acceptance/failure/removal cases or exact finance comparisons. |

- **Q-001 — Autosave:** static Save integration covers the shared method, but timer-triggered restoration is
  unverified because no trigger was observed. The mod does not modify scheduler/preferences/countdown.
- **Q-002 — Failure/removal:** failed native writes, disk-full/power loss, interrupted publication, corrupt/missing
  histories and mod removal need their own disposable native scenarios before being called runtime-passed.
- **Q-003 — Initial intermittent warning:** one reported mid-campaign failure was not reproducible and its exact
  earlier logged cause was unavailable. Numeric and toggle paths explain concrete defects; they do not prove the
  unknown intermittent warning was reproduced or had one of those exact causes.
- **Q-004 — Cross-runtime/Cloud:** G7 compatibility on other native runtimes and external-directory Cloud
  synchronization remain unverified. Native MP support remains deferred.
- **Q-005 — Restore atomicity:** per-room restore may partially succeed; native and external files are separate
  commits. Documentation must retain these limits rather than promising transaction-wide rollback.

## Revision Notes

- **2026-10-22:** Documented the current implementation separately from native game architecture. Preserved
  active-only persistence, no-preset Stop, strict adoption, immutable history/conflict policy, numeric/UI
  corrections and attributable user evidence. Earlier checkbox/finite-pass/all-configured retention proposals
  are not described as the implementation. No runtime test, game file or personal save was changed.
