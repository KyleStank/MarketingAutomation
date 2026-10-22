# Mad Games Tycoon 2 marketing automation mod: requirements

## Status and source of truth

This is the consolidated product specification for the working single-player
version-one baseline. Research is
complete, and the scaffold, BepInEx bootstrap, build, and deployment are established.
Subsequent explicit user decisions take precedence. Native integration and
recovery behavior still require the validation listed below.
Follow AGENTS.md for working instructions and the
user's current task for the scope of implementation and testing.

Project research starts at research/INDEX.md; its shared game-research link leads
to tools/mgt2-mod-toolkit/research/INDEX.md. Native observations are
static evidence unless explicitly runtime-tested. The product uses
a separate Automation workflow without the three native automation checkboxes;
the original Game and Game Console workflows keep their existing behavior.

Current implementation includes the styled Automation entry, native modal shell,
searchable mixed-product list with native sorting, multi-selection, native-size checkboxes, Select All,
and Development-style arrows/page dots with an always-visible confirmation button.
The campaign page contains six native-style campaign choices, separate game/console
prices, hype gain/cap information, native unlock restrictions, queue-priority/order
dropdowns with a live explanation, and a hype-threshold slider. Valid confirmation
now closes the form and starts a room-local queue through ordinary paid native
Marketing tasks. Queue scheduling, visible Waiting, Stop, Support integration,
and external save/load checkpoints are implemented. Stop discards the queue's
configuration; only active automation is saved for continuation on load. Build,
isolated scheduling checks, and disposable-file persistence checks pass; native
edge checks remain separate from native runtime evidence. The user confirmed
new-slot saving from a pre-mod game and repeated loads restoring the same observed
automation state; this does not establish the remaining lifecycle/recovery cases.
Full application restarts and Waiting restoration are also user-confirmed.
Following corrections for native float serialization and load-time toggle
notifications, the user reported no further save/load issues and no issues with
the suggested manual-save scenarios. The UI and functional automation are accepted
as the current working baseline. Autosave has not triggered during testing and
reportedly also failed before the mod; its cause is undetermined. Autosave
integration is therefore unverified, rather than a confirmed mod defect.
The requirements below describe the current behavior and validation boundaries.

## Background and existing workflows

Mad Games Tycoon 2 is a Unity game. Static research identified the installed game
as x86 Mono with Unity 2022.3.2f1; see tools/mgt2-mod-toolkit/research/systems/runtime-and-source-identity.md for evidence and
fingerprints. The user has over 200 hours
of play experience, has inspected the game's managed assemblies, and has exported a
Unity project using AssetRipper that opens to the main menu in the Unity editor.
These are useful starting observations, not proof that every reconstructed asset
or behavior matches the installed game.

Marketing becomes cumbersome for a large publisher with many products. Other
rooms already offer useful automation, while repeatedly configuring marketing
for individual products creates excessive clicking and micromanagement. The
mod's central feature is to consider a selected set of products, skip those that
cannot currently be marketed, and keep eligible work moving automatically.

The user describes the current Marketing Campaign workflow as:

1. Click the Marketing room's circular icon in the game world.
2. Choose Marketing Campaign from its context menu.
3. Choose Game or Game Console.
4. Choose one actual product and one marketing campaign.
5. Configure the three existing automation checkboxes as desired.
6. Click the green confirmation checkmark to close the menu and begin marketing.

The Marketing context menu includes Special Marketing, Market Research, Employee
Search, and Support, as traced in tools/mgt2-mod-toolkit/research/systems/room-task-binding-and-status.md. No new room-context
action is requested. Reuse/adapt the existing automation control for the custom
queue's working and Waiting states as specified below.

For Special Marketing, the user chooses that context-menu action, selects a game
and a special-marketing type, then confirms. Some types depend on whether the
game has released. Understand these mechanics for a future mod version.

For Employee Search, the user chooses that action, configures its form, and
confirms. The existing automation is adequate for now; no improvements are
requested for the initial version. Market Research and Support are not requested
implementation targets either.

The Workshop room's arcade-cabinet production queue and Production room's game
production queue are reference experiences for processing multiple selected
products. They are comparison targets, not an instruction to copy their internals
without checking suitability. Automated contract work in Development, QA,
Graphics, Sound, and Motion Capture, plus Research automation for genres, topics,
and gameplay features, provide additional patterns to investigate selectively.

Native checkbox behavior is documented in tools/mgt2-mod-toolkit/research/systems/marketing-work-and-automation-flags.md.
Those native mechanics remain reference material for the original workflows.
Their eight flag combinations are not product modes for the new Automation screen.

## Technical reference material

Workspace: C:\Users\gamem\Documents\_Development\mgt2-mod

- Steam installation: F:\SteamLibrary\steamapps\common\Mad Games Tycoon 2
- Original managed binaries: F:\SteamLibrary\steamapps\common\Mad Games Tycoon 2\Mad Games Tycoon 2_Data\Managed
- AssetRipper Unity project: reference/extracted_unity_project/ExportedProject
- Additional export material: reference/extracted_unity_project/AuxiliaryFiles
- ILSpy exports: reference/decompiled, especially Assembly-CSharp and Assembly-CSharp-firstpass
- BepInEx source: reference/BepInEx, inspected version 5.4.23.5
- Matching patch-engine source: reference/HarmonyX, pinned version 2.9.0

The installed loader is BepInEx 5.4.23.5 x86 with bundled HarmonyX 2.9.0, and
plugin loading is confirmed. The HarmonyX source is reference material,
not an additional patch engine to ship. Follow AGENTS.md and the existing scaffold
for target framework, compile dependencies, build, deployment, and checks.
The exported Unity project
is for investigation; the deliverable remains a mod for the installed Steam game,
not a replacement game build.

The local save folder has been confirmed to exist at
C:\Users\gamem\AppData\LocalLow\Eggcode\Mad Games Tycoon 2.
Save/load research starts at tools/mgt2-mod-toolkit/research/systems/native-save-lifecycle.md and its related identity report;
the implemented hooks and association policies follow the requirements below.
The exported project displayed the installed game's saves in its menu; treat
editor execution as potentially sharing that storage. Do not launch that project
for mod testing or inspect personal save contents without explicit authorization.
Use disposable test saves for runtime checks and keep custom data outside native saves.

## Goal and scope

Reduce Marketing room micromanagement by letting a team market a selected set of
games and consoles through ordinary game tasks, and maintain hype according to
the chosen campaign and automation settings.

- Initial implementation scope: a separate Automation entry and custom screen
  within Marketing Campaign, with ongoing selected-product automation.
- Research future support for Special Marketing, without implementing it initially.
- Employee Search improvements are outside initial implementation scope.
- Version 1 targets single-player. Multiplayer support is deferred to a later
  phase; its research remains reference material for future scope and validation.
- Target the installed Steam game. Use the exported Unity project as research
  material. Validate the BepInEx bootstrap and chosen patching dependencies against
  that installed game before relying on them for feature integration.
- Development setup and deployment must work across Windows, Linux, and macOS
  without committed machine-specific paths. The shared toolkit owns PowerShell/Bash
  initializers and literal config parsing; project-owned deployment consumes them.
  Accept an explicit project root, defaulting to the caller's current directory; keep
  .config.local and generated reference/ outputs in that mod, outside the toolkit.
  Initializers ask for absolute game-installation and save-directory paths,
  stored as local data in ignored `.config.local`. Require an existing BepInEx 5
  installation; do not install the loader or modify saves. A post-build hook may
  call repository-owned deployment scripts to copy only mod DLL/symbols. Permit
  an explicit build-only opt-out. Missing local configuration must fail an
  ordinary build when deployment is enabled.
  Provide PowerShell/Bash ILSpy decompilation scripts that restore the pinned local
  tool from toolkit `tools/mgt2-mod-toolkit/dotnet-tools.json`, discover the original installed game's Managed
  directory through `.config.local`, and decompile its managed DLLs into separate
  folders under `reference/decompiled`. Replace only that generated output on rerun,
  preserve unrelated reference material, record source hashes/tool metadata and logs,
  and report failures. Do not execute game assemblies or decompile AssetRipper's
  reconstructed copies as substitutes for the originals.
  Provide `tools/mgt2-mod-toolkit/scripts/clone-references.ps1` and `clone-references.sh` with a required
  HTTPS/SSH transport choice, supplied as an argument or through a prompt that
  displays both valid values. Clone BepInEx tag `v5.4.23.5` into
  `reference/BepInEx` and HarmonyX tag `v2.9.0` into `reference/HarmonyX`, including
  recursive submodules at recorded commits using the selected transport. Do not
  change global Git configuration. Delete existing destination directories before
  cloning, validating that deletion stays within these two exact paths and refusing
  linked directories. Preserve other reference checkouts and retain partial clones
  after failure.
  Provide separate PowerShell/Bash downloaders pinned to AssetRipper 2.0.0;
  select the host platform and x64/arm64 archive and extract under
  `reference/bin/AssetRipper-{platform}-{arch}` (`windows`, `mac`, or `linux`).
  Do not select the latest release. Delete the current platform/architecture's
  existing installation directory before downloading a fresh copy, validating the
  deletion boundary and refusing linked directories. Preserve other installations.
  Downloading the tool must not launch it or perform an asset export.
  Provide `extract.ps1` and `extract.sh` as single coordinating entry points:
  start the downloaded AssetRipper headlessly, await readiness, load the configured
  game directory, export into `reference/extracted_unity_project`, and stop the
  server started by that run. After loading the game, delete any previous export
  at that exact output path and create a fresh export without a confirmation prompt.
  Validate the deletion boundary and refuse linked destinations. Retain logs and
  partial output after failure. Do not launch Unity/the game or inspect personal saves.
  Provide toolkit `tools/mgt2-mod-toolkit/scripts/setup-references.ps1` and `tools/mgt2-mod-toolkit/scripts/setup-references.sh` coordinators that
  validate existing local configuration, then run reference cloning, AssetRipper
  download, Unity extraction, and assembly decompilation in that order. Forward an
  optional transport argument to the clone script, which prompts when omitted.
  The toolkit initializer creates or reuses local config and calls this coordinator by
  default, then installs the two skill links only after reference setup succeeds. Keep
  scripts/install.ps1 and scripts/install.sh independently callable. Config-only
  initialization skips both reference setup and skill installation.
  Research skills check readiness without generating references and direct the human
  to initialize missing setup. Stop on the first failure. Use the downloader's normal fresh-install behavior
  on every run. Do not build or deploy the mod.
  This tooling requirement does not establish native Mac/Linux gameplay support;
  platform assembly identities and runtime behavior still need verification.

## UI

Keep the existing Marketing Campaign entry in the room context menu. Preserve
its Game and Game Console choices and their original workflows. Add an Automation
button below those two choices; clicking it opens a separate custom screen.
Open that screen as a new menu over the retained picker, matching the original
Game/Game Console flow and its opening animation. Closing the child with X or
Escape returns immediately to the picker without a closing animation.
The Automation modal must match the game's native modal design, including its
window frame, inset background, title bar, close control, and product-list styling.
Reuse the existing automation start/stop interaction for mod-owned work, including
while Waiting. Adapting that control to the queue is required; unrelated native
flows must retain their behavior. No Manage screen or midway settings editing is
required. Stop Automation prevents all further campaign purchases while allowing
the already-paid campaign to finish. Clear the queue's selections, campaign,
threshold, priority, and admission state immediately; starting automation again
requires a new configuration. Existing Cancel Task
remains available for immediate cancellation with the native refund. Stop while
Waiting ends automation without buying a campaign.

The configuration uses the Development Studio's Develop a Game multipage form
pattern: one animated modal window, persistent left/right arrows and clickable
page dots, and a persistent circular green-check confirmation button at the
bottom right. Navigation uses those arrows and page dots.
The first page contains the searchable product list; the second page contains
queue priority, campaign choices, and threshold settings. Either page can be visited
before selecting products, so navigation is never gated by missing settings.
Only page contents change during navigation, without an opening/closing animation.
The modal itself still plays its normal opening animation on initial entry.
The second page contains the six campaign choices and the hype-threshold slider.

The green-check control remains visible and clickable on both pages. On click,
validate the whole configuration and use the game's native message-box modal
to explain missing settings. Missing products are reported first; once products
are selected, report a missing campaign. Show the page containing the missing
setting. Validation must retain the draft and keep Automation open.
A complete configuration closes the form and Marketing picker and starts
automation. Confirmation creates a paid native campaign when work is eligible
and affordable, or enters visible Waiting otherwise. Validation errors retain
the form and its draft. There is no success-preview modal.

X, Escape, or equivalent menu closure from either page closes the whole form
and discards unconfirmed product selections. Reopening Automation starts empty
on the product page. Navigating between pages preserves selections, search,
and scroll position because that does not close the configuration.
This draft-cancellation rule does not affect confirmed automation that is still
active when the game is saved.

The form contains:

1. Two queue-priority dropdowns on the second page, above the "Choose a marketing
   campaign" label: Name, Hype, Release Date in that order, and Ascending or Descending. Default a
   new form to Hype / Ascending. Explain the currently selected combination in
   nearby text that updates immediately when either control changes. The explanation
   must distinguish priority from eligibility: skip ineligible/unaffordable products,
   and retain the currently marketed product until its cap unless blocked.
   These settings survive page/campaign changes and reset when the form closes.
2. A searchable, scrollable multi-select product list with a bounded height for
   large late-game catalogs. Clicking a product checks/unchecks it. Each entry
   shows product type using existing game/console icons, name, release date, and
   hype. Combine the products available through the original marketing menus;
   do not broaden their underlying product eligibility rules.
   Use full native-size checkboxes. A Select All checkbox above the list operates
   on the full eligible catalog, including products hidden by search. If none or
   some are selected, clicking it selects all; if all are selected, it clears the
   selection. It is checked only when all available products are selected and
   disabled for an empty catalog.
   Beside search, add the native-style list-sort dropdown with options Name,
   Hype, Release Date in that order. Match the original selectors: Name is
   alphabetical, Hype is highest first, Release Date puts unreleased/shelved
   products first and then newer releases before older releases. Default this
   display control to Name. Preserve checkmarks, hidden selections, and Select All
   behavior when sorting or searching. A newly opened form starts with the product
   list and scrollbar at the top after the opening layout/animation initializes.
   Retain the display choice across page
   navigation and reset it on a new form. List sorting is independent of queue
   priority and is not persisted with active automation.
3. The six existing marketing campaign options on the final step, using their
   original mechanics and restrictions. Group all six buttons over the same
   light background panel used by the original Game/Game Console campaign menus.
4. A hype-threshold slider below the campaigns. Its minimum is 0 hype, with
   whole-number, 1-point steps. Its maximum is the selected campaign's hype cap.
   Disable the slider until a campaign is selected.
   Display the hype range without a step-size suffix.
   It gates eligibility both initially and when revisiting a product. Once admitted,
   the intended target is the campaign's maximum, not the slider value.
   Eligibility includes equality:
   current hype <= selected threshold, subject to base-game campaign restrictions.
   When the selected campaign changes, immediately update the slider's maximum
   to the new campaign cap. Clamp its current value down if necessary, and update
   the slider position and any displayed value/range to reflect the change.
   A new form defaults to the selected campaign's maximum. When restoring active
   automation from a save, restore its threshold subject to the campaign's maximum.
5. The always-visible green confirmation control at the bottom right validates
   and starts automation once the entire configuration is complete. Omit Automatic Repeat,
   Stop Marketing As Soon As Hype Is At Its Maximum, and Do Not Wait from this
   custom screen: the entire workflow is automated. The native screens retain
   their existing checkboxes.

Queue priority determines which selected product is considered first for marketing:

| Sort | Direction | Priority |
| --- | --- | --- |
| Name | Ascending | Alphabetical names, A to Z |
| Name | Descending | Reverse alphabetical names, Z to A |
| Hype | Ascending | Lower hype first: 10 before 85 |
| Hype | Descending | Higher hype first: 85 before 10, subject to campaign rules |
| Release Date | Ascending | Older releases before newer releases |
| Release Date | Descending | Newer releases before older releases |

## Task scheduling and game mechanics

- Perform campaigns individually for selected products through base-game tasks.
  Preserve normal costs, duration, staffing, progress, hype gains, and restrictions.
- The maintenance goal is to market selected products up to the maximum permitted
  by the chosen campaign, then revisit products when their hype drops to or below the
  applicable threshold while automation remains active.
- The slider controls eligibility for the initial pass as well as later passes:
  selected products at or below its threshold can enter marketing. After reaching
  the campaign cap, re-entry requires hype at or below the configured threshold
  and satisfaction of applicable campaign restrictions. Do not start a new
  automated campaign at or above its cap even when threshold equals cap. Research
  found that native confirmation does not universally prohibit such purchases;
  this productive-work rule is desired queue behavior, not a native hard gate.
- Skip a selected product when it cannot currently be marketed and consider the
  next selected product. Do not let one blocked product stall eligible work.
- Every automated campaign purchase requires cash >= that product type's campaign
  price; automation must not spend into available credit. Skip unaffordable
  products and wait if no selected work can run. This applies to the first
  automated purchase and every continuation. Original native workflows keep their
  existing affordability behavior. Respect actual campaign unlocks and product
  eligibility rather than bypassing them.
- Example: with descending hype priority, a product at 85 hype is skipped if the
  selected campaign has a cap of 60, allowing another eligible
  selected product to be considered.
- This is ongoing automation, not a checkbox-selected finite pass. Keep an
  admitted product until its campaign cap unless blocked. Re-sort when selecting
  another product; do not reconsider all products merely because one campaign
  completed. The entry threshold does not interrupt an admitted session as hype
  rises toward cap.
- If another room raises the current product to its cap during an already-paid
  campaign, let that campaign finish even if it adds no hype. Recheck eligibility
  before another purchase; do not buy a new campaign at/above cap.
- When every selected product is skipped, the room must enter a real, visible
  Waiting state while automation stays active. Resume when selected work becomes
  eligible and affordable. Display a native-style status panel above the Marketing
  room, with the native yellow exclamation icon, "Waiting for eligible products...", and the
  configured threshold as "Hype at or below N" with the number highlighted blue,
  matching native Marketing styling and the inclusive threshold rule.
  Hide it when a campaign starts or automation stops, and respect the
  game's room-UI visibility controls.
  Stop Automation must work while Waiting. Waiting itself
  must not purchase campaigns or generate hype. Use a dormant native `taskWait`
  with mod-owned presentation, cancellation, and scheduling. Exclude this transient
  waiter from native saves and reconstruct it from matching external settings.
  Do not run its native QA dispatch behavior. An invisible scan in an apparently
  idle room is insufficient.
- Sorting does not override campaign eligibility or other base-game restrictions.
- All selected products belong to the automation. Products above the threshold
  remain selected; the threshold controls when marketing becomes eligible, not
  membership in the automation.
- Only explicitly selected products belong to the queue. Newly available products
  are not automatically selected. Skip products that disappear or become ineligible.
- Break priority ties by stable typed product ID. Products without release dates
  sort last in both directions; dated products use numeric year/month ordering.
- Stop the queue if its room disappears or its task is canceled, replaced, or
  transferred. Preserve unrelated/reassigned native tasks; never reclaim them.

## Multiple rooms and Support

Preserve base-game behavior for independent Marketing rooms. Each queue schedules
independently, without shared product reservations or new coordination rules.

Support must continue working: a Marketing room supporting an automated Marketing
room assists with whatever campaign the supported room is currently performing.
Use the native leader/current-task link. Assistants idle while the leader is
Waiting and resume when it receives a new campaign.

## Persistence and I/O

Persist selections and configuration for automation that is still active when
the game is saved, including Waiting. Stop clears its configuration and excludes
that queue from subsequent saves. Previously saved historical game states retain
their matching active-automation snapshots. Do not retain stopped configuration
as a preset or offer direct restart with discarded settings.
Unconfirmed form drafts are discarded on closure and are not persisted. Do not add mod
fields to, rewrite, or change the format of original game saves. Ordinary
base-game saving of normal game state remains expected.

Wrap the shared native `savegameScript.Save(int)` method before and after execution;
the original method must still perform the actual save. Preserve native failures.
Capture queue state before saving, temporarily exclude owned transient waiters or
completed tasks, then restore live bindings even after failure. After a successful
native write, fingerprint its bytes read-only and publish the external record.
Do not patch the autosave scheduler or change its preferences/countdown. Manual
saves and native autosave slot zero use the same save hook.

Custom data must belong to the exact individual save, including slot reuse,
overwrite, Save As/copies, autosaves, and loading an older save. Use the external
fingerprint association below without modifying the native file.

Keep immutable external snapshot records in `MarketingAutomation/Snapshots`
beside the native save files, keyed by the native file's SHA-256 and length.
Keep previous snapshots for older saves; do not automatically delete history.
All custom configuration for each active
Marketing room must persist: selected product identities, selected campaign,
sort field and direction, and custom hype threshold. There are no custom checkbox
values to persist in the new workflow. Restore these settings on load so users
need not reconstruct them.
Automatically resume saved running or Waiting automation when its saved
association and native task binding are reliable. Saves made after Stop contain
no configuration for that queue and must not restart it.
Preserve the native active campaign's paid progress without creating or charging
for a duplicate. A saved Stop request must still prevent subsequent purchases
even if its already-paid campaign has work left to finish. Preserve enough
execution state, including admission and paid task identity/progress, to support
this behavior in the external checkpoint.
Validate paid progress using the native save writer's numeric representation,
including its float rounding. Do not change loaded work progress to force a match
or relax task/product/campaign identity checks. Loading UI preferences must not
trigger the employee-selection confirmation as if the user clicked its toggle;
preserve the preference and normal manual confirmation behavior.
An exact copied or older save may use its matching snapshot. If no record matches,
or settings for identical native bytes conflict, load the game normally with
automation off. Do not choose the latest record or guess an association. Reject
damaged, unsupported, or incompatible records. Native paid progress remains
authoritative; preserve the admitted product so loading above the entry threshold
does not interrupt an ongoing session toward its cap.

Investigate save/load timing, atomicity, failed saves, corrupted/missing/stale mod
data, room/product identity, version migrations, Steam Cloud behavior, and
multiplayer ownership/transfer. Do not assume custom files synchronize with Steam
Cloud or can be matched reliably using only a slot number.

## Multiplayer

Multiplayer support is deferred beyond the single-player version 1. Use
tools/mgt2-mod-toolkit/research/systems/multiplayer-authority-and-product-sync.md as architecture evidence for a
later phase. Supported installation arrangements, synchronization behavior,
reconnect/load handling and any custom messages require later decisions and tests;
the research does not constitute a multiplayer support promise.

## Remaining validation

No product decision blocks the current single-player queue implementation.
The user accepts the functional queue and UI and reports successful manual-save
testing after the IO fixes. Autosave continuity remains unverified because native
autosaving has not triggered. Keep the README acceptance list as a regression
checklist; acceptance of the working baseline does not establish every recovery,
failed-write, removal, or Support edge case as individually runtime-tested.
Hashes identify exact snapshots rather than proving copy provenance. External
mod records must accompany transferred saves; Steam Cloud synchronization is
not established.

## Implementation handoff and research references

Maintainability work must preserve the accepted UI, queue rules, native save
execution, and compatibility with existing external checkpoints. Organize code
so a human can follow workflows through descriptive methods and cohesive types;
do not make research documentation a prerequisite for understanding mod-owned
logic. Keep engineering conventions in AGENTS.md and the concise source map in
README.md rather than duplicating them here.

Reusable architecture research lives in tools/mgt2-mod-toolkit/research/. This
mod's implementation research lives in research/integrations/. Consult both indexes
during implementation, follow source evidence, and respect each finding's scope
and validation limits. Research documents describe evidence and implementation;
this document remains the product source of truth.

The completed research assignment file PROMPT.md has been retired. Its obsolete
assignments are not implementation instructions. AGENTS.md now provides the
implementation working rules, local source paths, save protections, and requirement
to use English names for new mod-owned C# code. Exact external game API names must
remain unchanged where integration requires them.

Preserve the established scaffold and BepInEx setup. Build, deployment, and plugin
loading are confirmed; runtime validation of each feature remains separate.
Keep AGENTS.md's build/test conventions and validation status current. Preserve
the accepted baseline and resolve consequential new product decisions before
changing affected behavior.

