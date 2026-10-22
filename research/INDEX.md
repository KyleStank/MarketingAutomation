# MGT2 project research index

Generated from report frontmatter; do not edit directly.

Read [SCHEMA.md](../tools/mgt2-mod-toolkit/research/SCHEMA.md) for evidence/update rules and [SOURCES.yaml](SOURCES.yaml) for provenance.
Search this file for concepts, aliases, or exact symbols, then open the linked sections and follow related findings.
Coverage is bounded by each report. A missing topic is a research gap, not proof that the game lacks that mechanic.

Project implementation research is listed here. Consult the [shared game research index](../tools/mgt2-mod-toolkit/research/INDEX.md)
for native-game and toolchain findings. Shared records are referenced, not copied into this library.

## Coverage

| ID | Kind / scope | Domains | Report |
| --- | --- | --- | --- |
| `bootstrap-and-deployment` | integration / mod-specific | modding, deployment, compatibility | [Marketing Automation bootstrap and deployment](integrations/bootstrap-and-deployment.md) |
| `configuration-ui` | integration / mod-specific | marketing, ui, mod-integration | [Marketing Automation configuration UI integration](integrations/configuration-ui.md) |
| `persistence-and-recovery` | integration / mod-specific | persistence, marketing, recovery, integration | [Marketing Automation persistence and recovery](integrations/persistence-and-recovery.md) |
| `queue-and-room-integration` | integration / mod-specific | marketing, automation, rooms, integration | [Marketing Automation queue execution and native room integration](integrations/queue-and-room-integration.md) |

## bootstrap-and-deployment

Current project target/references, plugin startup/fallback/teardown and configuration/deployment scripts, separated from generic game and loader behavior.

Report: [Marketing Automation bootstrap and deployment](integrations/bootstrap-and-deployment.md). Kind: integration; scope: mod-specific.
Metadata updated: 2026-10-22. Source sets: marketing-automation-implementation, mgt2-toolchain-2026-10-22, mgt2-runtime-2026-10-22.

Keywords: build, netstandard2.1, BepInEx.Core, HarmonyX, DeployToGame, initialize, config, plugin GUID, MVID, UnpatchSelf

Symbols: `MarketingAutomation.MarketingAutomationPlugin`, `PluginMetadata`, `QueueIntegration.Install`, `DeployPlugin`, `Get-ProjectLocalConfig`, `Assert-BepInExInstallation`

| Entry point | Coverage |
| --- | --- |
| [Framework and references](integrations/bootstrap-and-deployment.md#f-001---this-project-targets-netstandard21-with-compile-only-game-references) | Actual project configuration; distinguish restored package graph from installed loader. |
| [Startup and fallback](integrations/bootstrap-and-deployment.md#behavior-and-call-flows) | MVID/signature gate, menu extension, queue hooks and cleanup. |
| [Deployment side effects](integrations/bootstrap-and-deployment.md#f-004---ordinary-builds-can-deploy-only-the-plugin-and-symbols) | Default post-build deploy, literal config, core-layout check and explicit skip. |
| [Validation limits](integrations/bootstrap-and-deployment.md#unknowns-and-validation) | Static inspection as of 2026-10-22; build, deployment and gameplay were not rerun. |

Related:

- [runtime-and-source-identity](../tools/mgt2-mod-toolkit/research/INDEX.md#runtime-and-source-identity): Native player/module identity is an input to this plugin's capability gate.
- [loader-and-patching-boundaries](../tools/mgt2-mod-toolkit/research/INDEX.md#loader-and-patching-boundaries): Third-party plugin and patch ownership behavior.
- [configuration-ui](#configuration-ui): UI extension attached by the menu Init postfix.
- [queue-and-room-integration](#queue-and-room-integration): Queue hooks installed during bootstrap and removed on destruction.
- [persistence-and-recovery](#persistence-and-recovery): Save/load hooks are installed through QueueIntegration.

## configuration-ui

Actual working-tree Automation entry, native visual adapters, two-page draft lifecycle, mixed product selection, independent display sorting, campaign/priority/threshold controls and queue confirmation boundary.

Report: [Marketing Automation configuration UI integration](integrations/configuration-ui.md). Kind: integration; scope: mod-specific.
Metadata updated: 2026-10-22. Source sets: marketing-automation-implementation, mgt2-managed-2026-10-22, mgt2-unity-export-2026-10-22.

Keywords: Automation entry, configuration draft, Select All, pooled rows, search, multipage, threshold, queue priority, display sorting, Escape, native control adapters, validation dialog

Symbols: `MarketingAutomationPlugin.AfterMarketingMenuInitialized`, `MarketingMenuExtension`, `AutomationScreen`, `AutomationFormState`, `AutomationCampaignPage`, `ProductListView`, `ProductRow`, `ProductSelection`, `ProductKey`, `ProductListSorting`, `NativeDropdown`, `NativeUi`, `SearchInputFocus`, `CampaignConfiguration`, `ProductSortOptions`, `NativeCampaignCatalog`, `MarketingQueue.TryStart`

| Entry point | Coverage |
| --- | --- |
| [Implementation ownership map](integrations/configuration-ui.md#architecture-and-state-ownership) | Form, product view, rows, selection model and visual adapters have distinct responsibilities. |
| [Open navigate close confirm](integrations/configuration-ui.md#behavior-and-call-flows) | Picker layering, root closure reset, freely navigable child pages and native error dialogs. |
| [Display sort versus queue priority](integrations/configuration-ui.md#f-003--display-order-and-queue-priority-share-labels-but-use-separate-state-and-rules) | Native descending list keys versus configurable dispatch directions and persisted enum mapping. |
| [Actual validation boundary](integrations/configuration-ui.md#unknowns-and-validation) | User acceptance predates current readability refactor; current evidence is static only. |

Related:

- [ui-modal-and-control-lifecycle](../tools/mgt2-mod-toolkit/research/INDEX.md#ui-modal-and-control-lifecycle): Primary native visual, modal, focus, animation and callback constraints.
- [marketing-product-catalog-and-ordering](../tools/mgt2-mod-toolkit/research/INDEX.md#marketing-product-catalog-and-ordering): Native membership and display-sort rules copied by the adapter.
- [marketing-campaign-configuration](../tools/mgt2-mod-toolkit/research/INDEX.md#marketing-campaign-configuration): Native campaign data/unlocks and original flows preserved by this separate form.
- [queue-and-room-integration](#queue-and-room-integration): Owns committed execution after UI validates and hands over copied configuration.
- [persistence-and-recovery](#persistence-and-recovery): Owns active-queue checkpoints; this form's drafts are transient.
- [bootstrap-and-deployment](#bootstrap-and-deployment): Owns plugin startup/compatibility checks and installed build identity.

## persistence-and-recovery

Current single-player Save/Load wrappers, immutable exact-byte checkpoint history, native paid-task adoption, transient waiter exclusion, numerical correction, diagnostics, and validation limits.

Report: [Marketing Automation persistence and recovery](integrations/persistence-and-recovery.md). Kind: integration; scope: mod-specific.
Metadata updated: 2026-10-22. Source sets: mgt2-managed-2026-10-22, marketing-automation-implementation.

Keywords: snapshot, immutable history, SHA-256, Stop, Waiting, admission, paid progress, G7, employee toggle, diagnostics, autosave

Symbols: `QueueIntegration.Install`, `QueuePersistence.BeginSave`, `QueuePersistence.FinishSave`, `QueuePersistence.BeginLoad`, `QueuePersistence.FinishLoad`, `QueuePersistence.RestoreRoom`, `QueueSnapshotStore`, `QueueSnapshotValidation`, `MarketingQueue.SuspendForSave`, `MarketingQueue.CaptureActive`, `MarketingQueue.Restore`, `NativeCampaignProgress`, `PersistenceDiagnostics.Record`

| Entry point | Coverage |
| --- | --- |
| [Checkpoint contract](integrations/persistence-and-recovery.md#checkpoint-model-and-storage) | Schema fields, typed identities, hash/payload addressing and immutable history |
| [Save wrapper](integrations/persistence-and-recovery.md#save-capture-publication-and-cleanup) | Capture, owned transient exclusion, native exception preservation and external publication |
| [Load reconciliation](integrations/persistence-and-recovery.md#load-and-native-paid-task-adoption) | Header/object/task validation and adoption without payment |
| [IO corrections](integrations/persistence-and-recovery.md#rounding-and-preference-corrections) | G7 checkpoint comparison, historical normalization, and silent load-time toggle initialization |
| [Validation evidence](integrations/persistence-and-recovery.md#unknowns-and-validation) | User-reported baseline versus isolated checks and still-unexecuted failure/autosave/MP cases |

Related:

- [native-save-lifecycle](../tools/mgt2-mod-toolkit/research/INDEX.md#native-save-lifecycle): Provides actual native Save/Load order, ES3 sharing/number representation, and preference callback constraints
- [save-identity-and-task-restoration](../tools/mgt2-mod-toolkit/research/INDEX.md#save-identity-and-task-restoration): Defines snapshot/object identity limits and native paid-task fields
- [queue-and-room-integration](#queue-and-room-integration): Owns active queue, admission, Stop, Waiting, room task and Support adaptations
- [configuration-ui](#configuration-ui): Drafts are discarded on closure and are not active checkpoint data
- [bootstrap-and-deployment](#bootstrap-and-deployment): Owns selected runtime/reference/patch compatibility and deployed plugin evidence
- [multiplayer-session-and-save-coordination](../tools/mgt2-mod-toolkit/research/INDEX.md#multiplayer-session-and-save-coordination): Documents native multiplayer research; this persistence implementation explicitly excludes MP restoration

## queue-and-room-integration

Current room-local cap-maintenance scheduler, one-shot paid native tasks, owned dormant Waiting, Stop/Cancel/Support adapters, transfer limits and persistence handoffs.

Report: [Marketing Automation queue execution and native room integration](integrations/queue-and-room-integration.md). Kind: integration; scope: mod-specific.
Metadata updated: 2026-10-22. Source sets: marketing-automation-implementation, mgt2-managed-2026-10-22.

Keywords: queue, inclusive threshold, admission, cash-only, cap maintenance, Stop Automation, Waiting for eligible products, dormant waiter, Support, ownership, deferred completion, task transfer, selected products

Symbols: `MarketingQueue`, `QueueScheduler.Choose`, `QueueTaskOwnership`, `MarketingQueue.StartCampaign`, `MarketingQueue.LateUpdate`, `MarketingQueue.Stop`, `MarketingQueue.Detach`, `QueueIntegration.Install`, `QueueIntegration.BeforeRoomPanelsPruned`, `QueueIntegration.BeforeCancelConfirmed`, `MarketingProduct.CanMarket`, `NativeCampaignCatalog.Read`

| Entry point | Coverage |
| --- | --- |
| [Admission and priority](integrations/queue-and-room-integration.md#selection-admission-and-current-product-retention) | Inclusive entry threshold, productive cap/cash guards, blocked skip and current product retention. |
| [Native paid unit](integrations/queue-and-room-integration.md#f-002---each-dispatched-campaign-is-one-cash-funded-native-one-shot) | Immediate FindScripts, native fields, one debit, false flags and deferred completion observation. |
| [Visible Waiting adapters](integrations/queue-and-room-integration.md#waiting-presentation-context-and-support) | Dormant art -1, retained slot26, owned text/context/Support patches. |
| [Stop and Cancel](integrations/queue-and-room-integration.md#stop-cancel-transfer-and-errors) | Stop discards configuration but preserves paid work; Cancel uses native refund. |
| [Transfer waiter limitation](integrations/queue-and-room-integration.md#f-005---task-replacement-stops-the-queue-but-transferred-waiter-cleanup-needs-runtime-review) | Paid transfer is preserved; Stop can destroy a still-bound owned waiter after ID mismatch. |
| [Validation limits](integrations/queue-and-room-integration.md#unknowns-and-validation) | Static inspection, prior user observations, isolated checks and untested Support/transfer edges are distinct. |

Related:

- [configuration-ui](#configuration-ui): Owns confirmed copied form configuration and independent display sorting.
- [marketing-task-completion](../tools/mgt2-mod-toolkit/research/INDEX.md#marketing-task-completion): Owns native paid completion/accounting that the queue preserves with automatic=false.
- [marketing-work-and-automation-flags](../tools/mgt2-mod-toolkit/research/INDEX.md#marketing-work-and-automation-flags): Explains native flags/work gates; custom queue policy deliberately uses one-shot tasks.
- [room-task-binding-and-status](../tools/mgt2-mod-toolkit/research/INDEX.md#room-task-binding-and-status): Owns native cache, context, presentation and cancellation gaps adapted here.
- [room-support](../tools/mgt2-mod-toolkit/research/INDEX.md#room-support): Native assistants follow leader/current-task; Waiting extends the leader idle predicate.
- [native-waiting-and-automation-patterns](../tools/mgt2-mod-toolkit/research/INDEX.md#native-waiting-and-automation-patterns): Establishes that taskWait art -1 is dormant and art 0 dispatches QA.
- [persistence-and-recovery](#persistence-and-recovery): Owns external snapshot association, paid-task adoption and native-save wrapping.
- [bootstrap-and-deployment](#bootstrap-and-deployment): Owns plugin startup, patch installation and compatibility gates.

## Source sets

These are recorded evidence sets, not claims that every artifact was revalidated.

- `marketing-automation-implementation`: Current Marketing Automation implementation, toolkit configuration dependency and documentation locations; separate from reusable native research.

Shared source definitions: [SOURCES.yaml](../tools/mgt2-mod-toolkit/research/SOURCES.yaml).

- `mgt2-managed-2026-10-22`: Installed managed inputs matched to the ILSpy inventory; source symbols inspected on 2026-10-22.
- `mgt2-runtime-2026-10-22`: Fresh read-only PE/CLI/file-version/hash observations for original installed player/managed inputs and the Auxiliary comparison.
- `mgt2-toolchain-2026-10-22`: Fresh installed loader/patch DLL identities and directly inspected clean BepInEx/HarmonyX source checkouts.
- `mgt2-unity-export-2026-10-22`: Reconstructed Unity scene/prefab/animation paths inspected on 2026-10-22; identifiers are export-local and paths are unhashed locators.
