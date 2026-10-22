# Marketing Automation

BepInEx 5 mod for Mad Games Tycoon 2's Mono runtime. Gameplay is currently
verified on Windows; development setup/deployment scripts are provided for
Windows, Linux, and macOS. Native Linux/macOS runtime compatibility remains to
be verified, including the startup assembly fingerprint check.
Product requirements are in [REQUIREMENTS.md](REQUIREMENTS.md); implementation
evidence and historical proposals are in [research/](research/).

## Working version-one baseline

The single-player mod is functional and user-accepted as of 2026-10-21. The user
reports that the UI, automation, and manual save/load behavior work without
observed issues after the IO fixes. This is the baseline for further changes.
Autosave restoration remains unverified because autosaving has not triggered
during testing; the user reports that this also occurred before installing the
mod. Its cause has not been established.

The Marketing Campaign picker gains an **Automation** button below Game and
Game Console. It opens a searchable mixed list of games and consoles for that room.
The modal copies the native Marketing product selector's actual window frame,
inset background, title bar, close control, scrollbar, and opening animation.
It leaves the
picker open underneath. X and Escape/right-click close the form from either page
immediately, returning to the picker. Closing the picker then ends the modal
through `GUI_Main`, just as in the original campaign workflows.
The original Game and Game Console callbacks are retained.
Automation's entry copies the Game button's gloss overlay, shadow, outline,
label layout, hover colors, and icon placement. Its icon is the existing native
`iconAutomatic` repeat arrow, resolved from the room-control prefab's images.

Click anywhere on a product row to check/uncheck it. Each row shows the native
product-type icon, name, release date/development status, and rounded hype.
Checkboxes use the native 32-pixel background and 29-pixel checkmark.
Select All above the list operates on
the whole catalog, including search-hidden products: none/partial becomes all,
and all becomes none. It is unchecked for partial selections and disabled when
the catalog is empty.
Search filters names immediately without clearing hidden selections. A selected
count includes selected products outside the filter. Empty catalogs and empty
search results have separate messages. The viewport has a fixed height, and a
small recycled row pool keeps UI object count independent of catalog size.
The native-style dropdown beside search sorts the list by Name, Hype, or Release
Date, in that order. Name is alphabetical; Hype is highest first; Release Date
places unreleased/shelved products first, then newest releases first, matching the
original Marketing selectors. Sorting preserves checkmarks and hidden selections.
The list defaults to Name and retains its choice while navigating between pages.
Each new form opens with the list and scrollbar at the top; the initial reset
is applied again once the active canvas has usable layout bounds.

The footer copies Develop a Game's native left/right arrows, two clickable page
dots, and circular green confirmation button. The arrows reflect page boundaries;
they do not require selected products. The frame, header, and footer stay open
while only the product/campaign content changes, with no animation between pages.
Selections, search, and scroll position survive page navigation. Only the initial
form opening animates.

The campaign page starts with native-style queue-priority dropdowns: Name, Hype,
Release Date in that order, plus Ascending or Descending. A new form defaults to Hype / Ascending.
An explanation updates for all six combinations and describes skipping blocked
products and retaining the current product until its cap unless blocked. These
draft settings survive page/campaign changes and configure the execution queue.
The product-list display
order is unchanged by these controls.

The six campaign cards are grouped over the native campaign menus' light
PanelButtons background, outline,
and shadow. The panel is decorative and does not intercept button input.
Campaign cards retain their native icons, selection colors, lock overlays,
localized names, and hype gain/cap. Each card
shows game and console prices separately, read from the live native campaign
menus. Internet Ads and Top Streamer respect unlocks 56/57. The original campaign
menus are never initialized or modified by this screen.

The native-style threshold slider uses minimum 0 and whole-number 1-point steps.
It stays disabled until a campaign is selected, then defaults to that campaign's
hype cap. Lowering the campaign cap immediately clamps value/range/thumb;
increasing the cap preserves a lower chosen threshold. Campaign/threshold choices
survive page navigation and reset when the entire form closes.

The green confirmation button is always visible and clickable. It invokes the
game's native message box: no selected products reports that requirement first;
otherwise it reports a missing campaign when none is selected and shows the
corresponding page. Valid settings close the form and picker and start a room-local
queue, buying one ordinary native campaign or entering visible Waiting if no
selected product is eligible and affordable. There is no success-preview modal.
The native dialog temporarily renders above the mod form and restores the
native canvas ordering when dismissed. Escape dismisses the error without closing
the configuration. Closing the form from either page discards its draft.

The catalog uses the original game and console eligibility predicates. It is
refreshed on opening; no products are selected automatically. Draft selections
belong only to the open form. Closing Automation with
X, Escape, or configured right-click clears the entire selection, including hidden
products. The common root's OnDisable also handles other deactivation/cleanup
paths. Reopening always starts empty on the product page. Navigation between pages
preserves the draft because it hides only a child window, not the form root.
No draft is stored on the room or in a static collection. Only confirmed, active
automation is eligible for saving.

The scheduler admits products at or below the threshold and retains the current
product until the campaign cap unless blocked. Every purchase requires actual
cash, with separate native game/console prices. At the cap it re-sorts the remaining
eligible work; no campaign is bought at/above the cap. Missing release dates sort
last in either direction; ties use typed product IDs. Newly available products are
not automatically added. Removed/ineligible products are skipped. Canceled,
replaced, or transferred tasks stop their queue rather than being reclaimed.

Waiting uses a dormant native task, with a visible Marketing caption and the
native yellow exclamation icon above the room: "Waiting for eligible products..." followed
by "Hype at or below N", showing the configured threshold in native blue text.
The generic native Waiting panel is retained during Marketing room initialization
so this status can appear. Its normal room visibility and offscreen rules apply.
Waiting also keeps the
native active Marketing context panel for Stop/Cancel. Stop prevents additional
purchases, clears its configuration, and lets paid work finish. A new automation
requires a fresh configuration. Cancel retains native immediate refund
behavior. Support continues through the native leader/current-task link and idles
while the queue waits. The core workflow is user-accepted; the regression checklist
below also covers individual lifecycle and Support edge cases.

Active automation, including Waiting, is saved as immutable records under
`MarketingAutomation/Snapshots` beside native save files. Records match the native
file's SHA-256 and length and retain previous snapshots for older saves. Exact
copies can match the same records. Missing, conflicting, damaged, incompatible,
or unsupported records do not start automation. Copy this mod-data folder as well
when transferring saves; Steam Cloud coverage is not established.

The mod wraps the game's shared `savegameScript.Save(int)` method with code before
and after the original method; the original native serializer still writes the
normal save, and native errors are preserved. This hook covers manual saves and
autosave slot zero. The mod does not patch the autosave scheduler or change its
setting/countdown. After a successful native save, the mod reads its bytes to
calculate the fingerprint and writes only its separate automation record.

The save guard temporarily excludes owned transient waiters/completed tasks while writing and restores
them even if saving fails. Paid campaigns retain native save data. Loading adopts
the matching paid task and its progress without another purchase and restores
admission above the threshold. Waiting resumes through a new dormant waiter;
dispatch starts only after the load frame and when game time advances. No native
save fields are added. Stop removes the queue from future checkpoints while old
saved game states keep their matching history.

## Build and deployment

Source layout under `src/MarketingAutomation`:

- `MarketingAutomation.cs`: loader startup and shutdown.
- `PluginMetadata.cs`: hand-maintained BepInEx identity/version constants; no
  `MyPluginInfo` generation. Keep its version aligned with the project version.
- `UI`: the Automation form, campaign page, product list, rows, and selection models.
  Start at `AutomationScreen` to follow opening, navigation, confirmation, and
  closure. `ProductListView` handles search/sort/selection/scrolling; `ProductRow`
  renders one recycled entry. The form forwards activation/layout/reset calls.
- `UI/Controls`: native visual copying and reusable input/control construction.
- `Automation`: queue lifecycle, campaign configuration, and scheduling policy.
- `GameIntegration`: Harmony hooks and adapters for native products/campaigns.
- `Persistence`: external save checkpoints, validation, storage, and diagnostics.

Formatting follows `.editorconfig`, including mandatory control-flow braces.
This first refactoring pass reorganizes the source and separates product-list
presentation from form orchestration. Queue and persistence responsibilities remain
targets for later focused refactoring. Save contracts and gameplay rules are unchanged.

After cloning, fetch the shared toolkit submodule before running setup scripts:

```text
git submodule update --init --recursive
```

The toolkit uses GitHub SSH access. Then install the platform-appropriate BepInEx 5 Mono loader in
your game installation. Launch the game normally at least once so its save
directory exists. Initialize this checkout from a terminal:

```powershell
# Windows (Windows PowerShell 5.1 or newer)
powershell -NoProfile -ExecutionPolicy Bypass -File tools/mgt2-mod-toolkit/initialize.ps1 -ProjectRoot .
```

```bash
# Linux / macOS (Bash; no PowerShell or Python required)
bash tools/mgt2-mod-toolkit/initialize.sh --project-root .
```

Run these commands from the mod root. The toolkit accepts `-ProjectRoot` / `--project-root`
(relative or absolute); the default is the current directory. Configuration and generated
`reference/` data belong to that project, never the toolkit.
Both initializers ask for two existing, absolute directories: the game installation
and the game's save directory. Enter literal paths without surrounding quotes or
shell escaping at the prompts. On macOS, choose the folder containing the game's
`.app`, where BepInEx is installed, rather than the app bundle itself.
See the [BepInEx installation guide](https://docs.bepinex.dev/v5.4.16/articles/user_guide/installation/index.html).

The initializer checks for `BepInEx/core/BepInEx.dll` and
`BepInEx/core/BepInEx.Preloader.dll`. This checks loader file presence, not whether
the loader successfully starts. It writes only the ignored root `.config.local`:

```text
game_directory=<absolute installation directory>
save_directory=<absolute save directory>
```

Values are UTF-8 literal text; neither script executes the configuration. No
additional path settings are needed yet. The save directory is recorded for future
development tools; initialization/deployment never reads or changes personal saves,
and the running mod continues resolving save paths through the game.
Valid existing configuration is reused. Rerun with `-Force` (PowerShell) or `--force` (Bash) to replace
recorded paths. Use `-ConfigOnly` / `--config-only` for deployment-only config; research skills also require references.

The initializer calls the toolkit's `scripts/setup-references.ps1` / `.sh` coordinator automatically,
then `scripts/install.ps1` / `.sh` to link the skills after reference setup succeeds.
Config-only initialization skips reference preparation and skill installation.
Select `-Transport ssh` / `--transport ssh` on initialization, use `https` instead, or omit transport to see a prompt.
After a failed setup, rerun initialization without new path arguments to reuse config and retry the reference steps.
The coordinator validates `.config.local`, then clones the reference repositories,
downloads AssetRipper, extracts the Unity project, and decompiles the installed
game assemblies. It stops on the first failure. It does not build or deploy the mod.
Reruns replace the two source checkouts and generated Unity/decompiled outputs.
The downloader deletes the current platform/architecture's AssetRipper directory
and downloads a fresh pinned copy each time. Other platform installations remain.
Both coordinators passed syntax and disposable-stub checks for step order and
stopping on failure. The full workflow was not run during implementation.

The generated reference layout is:

```text
reference/
    BepInEx/                         # v5.4.23.5 source and submodules
    HarmonyX/                        # v2.9.0 source and submodules
    bin/AssetRipper-{platform}-{arch}/
    extracted_unity_project/
        ExportedProject/             # Unity assets and project settings
        AuxiliaryFiles/              # Export metadata and reconstructed DLLs
    decompiled/
        {assembly-name}/             # ILSpy sources from installed game DLLs
        assemblies.tsv               # Original input paths, hashes, and results
        dotnet-tools.json            # Tool version used for this export
    logs/
        AssetRipper/
        ILSpy/
```

Research C# citations point into the ILSpy output. Namespaced libraries use
ILSpy's dotted namespace folders, such as `UnityEngine.CoreModule/UnityEngine.Events`.
Installed game DLLs remain the authoritative inputs; exported/decompiled projects
are reference material and must not be built or deployed as replacement game code.

Each step can also be run separately. To download the pinned AssetRipper **2.0.0** binary:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/mgt2-mod-toolkit/scripts/asset-ripper/download.ps1 -ProjectRoot .
```

```bash
bash tools/mgt2-mod-toolkit/scripts/asset-ripper/download.sh --project-root .
```

The scripts detect Windows/macOS/Linux and x64/arm64, download the matching GitHub
release, and extract it into `reference/bin/AssetRipper-{platform}-{arch}`.
Platform names are `windows`, `mac`, and `linux`. PowerShell uses ZIP extraction
on Windows and `tar`/`chmod` on Unix; Bash requires `curl` plus `unzip` on Windows
or `tar` with xz support on Unix. Each run deletes and replaces the matching
platform/architecture installation; linked directories are refused.
This downloads the tool only; it does not launch AssetRipper or export game assets.
Downloader syntax is checked, but download/extraction has not been run as part of
this implementation.

After downloading, export the configured game's Unity assets with one command:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/mgt2-mod-toolkit/scripts/asset-ripper/extract.ps1 -ProjectRoot .
```

```bash
bash tools/mgt2-mod-toolkit/scripts/asset-ripper/extract.sh --project-root .
```

Each entry point starts its own headless AssetRipper server on an automatically
assigned localhost port, waits for readiness, and sends the configured game path
to its load/export API. It stops only its own server when finished or on failure.
The export root is `reference/extracted_unity_project`; AssetRipper places the
Unity project in its `ExportedProject` child and auxiliary material alongside it.
Rerunning automatically deletes and replaces `reference/extracted_unity_project`
after game loading succeeds. Any manual edits inside that export are discarded.
Deletion is restricted to that exact folder; linked destinations are rejected.
Failed exports retain partial output and logs under `reference/logs/AssetRipper`.
The server must start within 60 seconds; load and export each allow two hours
(PowerShell accepts `-TimeoutSeconds` to change that limit).
The scripts do not launch Unity/the game or inspect personal saves. The user
confirmed PowerShell extraction succeeds. Automatic replacement has only been
syntax-checked during implementation; the user will test the next export run.

To decompile the original installed game assemblies instead of AssetRipper's
reconstructed copies:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/mgt2-mod-toolkit/scripts/ilspy/decompile.ps1 -ProjectRoot .
```

```bash
bash tools/mgt2-mod-toolkit/scripts/ilspy/decompile.sh --project-root .
```

Both scripts restore the pinned ILSpy CLI from toolkit `tools/mgt2-mod-toolkit/dotnet-tools.json` and require
the .NET 10 SDK/runtime for the current version. They locate the game's Managed
directory through `.config.local`, skip native/unrecognized DLLs, and export each
managed DLL into `reference/decompiled/{assembly-name}`. Reruns replace that output
folder, preserving unrelated reference material. `assemblies.tsv` records source
paths, SHA-256 hashes, and results; a copy of the tool manifest records the version.
Per-assembly logs remain under `reference/logs/ILSpy`. Any decompilation failures
are reported after processing the remaining assemblies and return an error.
Bash additionally requires `file` and either `sha256sum` or `shasum`; Git Bash
can read the Windows paths written by the PowerShell initializer.
Syntax, metadata identification, and the installed tool's version command were
checked during implementation. The user confirmed a full decompilation and a
subsequent rerun both completed successfully.

To clone the matching loader and patch-engine sources for reference, explicitly
choose `ssh` or `https`:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/mgt2-mod-toolkit/scripts/clone-references.ps1 -Transport ssh -ProjectRoot .
```

```bash
bash tools/mgt2-mod-toolkit/scripts/clone-references.sh ssh --project-root .
```

Replace `ssh` with `https` to use HTTPS. Omit the argument to be prompted with
the valid choices, `https` and `ssh`. Git must be on PATH; SSH additionally
requires working GitHub SSH authentication. No local game configuration is needed.
The scripts clone BepInEx tag `v5.4.23.5` into `reference/BepInEx` and HarmonyX tag
`v2.9.0` into `reference/HarmonyX`. Recursive submodules use their recorded commits
and the selected transport, without changing global Git configuration.

Each run deletes existing `reference/BepInEx` and `reference/HarmonyX` directories
before cloning fresh copies, including any local changes in those checkouts.
Deletion is confined to those two paths; linked directories and files are refused.
Failed clones retain partial output for inspection. These are reference sources, not additional DLLs to
deploy. Unrelated reference directories remain untouched. Syntax and URL rewriting
were checked locally; the clone scripts were not run against GitHub.

Then build from the workspace root using the .NET SDK:

```powershell
dotnet build MarketingAutomation.sln
```

The small post-build target calls `scripts/deploy.ps1` on Windows or
`scripts/deploy.sh` on Linux/macOS. These scripts revalidate the configured paths
and BepInEx core files, create `BepInEx/plugins` if needed, and copy only
`MarketingAutomation.dll` and its PDB when present. They never prompt during a build.
Without `.config.local`, the build fails at the deployment step with initialization
instructions. Invalid configured paths or copy failures also fail that step.
For a build that deliberately performs no deployment (including CI):

```text
dotnet build MarketingAutomation.sln -p:DeployToGame=false
```

Run isolated model and disposable-file persistence checks (no Unity/game execution or deployment):

```powershell
dotnet run --project tests/MarketingAutomation.Checks/MarketingAutomation.Checks.csproj
```

The checks link production selection, form, configuration, scheduling, snapshot,
and persistence code and use .NET 8 without test packages. Persistence checks use
disposable files under the test output directory and do not inspect personal saves.

Target: `netstandard2.1`. Assembly-CSharp, Assembly-CSharp-firstpass (ES3), and
UnityEngine.UI are compile-only references in the repository's relative `lib/`
directory (`Private=false`); they are not copied to the output or deployed.
The existing BepInEx.Core package resolves to 5.4.21 and HarmonyX 2.7.0 for
compilation; installed runtime inspection found BepInEx 5.4.23.5 and bundled
HarmonyX 2.9.0. No additional patch engine is deployed.

Restart the game after deployment to load the new plugin. Removing those two
plugin files while the game is closed removes the mod. The mod deploys no original
game binaries or assets and adds no fields to native saves. Ordinary native saving
continues to save paid campaign progress; active queue settings live in the
separate mod-data folder.

## Validation status (2026-10-21)

- Development setup: initialization/config validation/deployment checks pass on
  Windows PowerShell 5.1 and Bash through Git Bash, using disposable fake game
  directories. They cover literal paths with spaces/punctuation, config overwrite
  protection, CRLF input, missing/duplicate/unknown keys, missing loader files,
  DLL replacement, and optional symbols. A normal Windows build deploys through
  the script with matching DLL/PDB hashes. Missing configuration now fails the
  deployment target; explicit build-only mode remains available.
  Native Linux/macOS execution and gameplay have not been tested.
- First readability refactor: source organization, explicit braces/formatting,
  and product-list/row extraction pass build, existing model/file checks, and
  formatter verification. The refactored UI still needs an in-game smoke test;
  earlier gameplay acceptance applies to the preceding baseline.
- User confirmed that the original scaffold builds, deploys, and loads in-game.
- User verified loading a pre-mod save, starting automation, saving through the
  pause menu into a new slot, and loading that slot with automation in the same
  observed state. Repeated loads of that new save consistently restore automation.
  Full application restarts and Waiting restoration were subsequently confirmed.
  Overwriting restored the expected configuration, and Stop remained stopped.
  After corrections for native float rounding and the load-time employee-selection
  confirmation, the user reported no further save/load issues and no issues with
  the suggested manual-save scenarios. The previously reported intermittent
  restoration warning has not recurred in their subsequent testing.
- User accepts the mod as functional and the UI as complete for this baseline.
  This is user-reported gameplay validation, not an exhaustive record of every
  individual acceptance check below.
- Autosave has not triggered during testing, including after selecting a one-month
  interval and restarting. The user reports a similar absence before the mod.
  Source inspection confirms the mod does not change the native autosave scheduler;
  no cause has been established, and autosave restoration remains unverified.
- User accepted the product list, Select All, shared Development-style navigation,
  campaign/threshold controls, queue dropdowns, and native campaign background.
- The queue milestone compiles with zero warnings/errors. Deployment hashes are
  checked against the build output.
- Installed Assembly-CSharp SHA256 still matches the research input:
  `38E32C3372F9D8D9F1CC472C43B62168F3D4003007ECD13E6A854108D076EA17`.
- Menu hierarchy, callbacks, and layout were inspected statically against the
  decompiled sources and reconstructed scene. No personal save contents were inspected.
- Search/selection checks passed for case-insensitive filtering, hidden selections,
  room isolation, matching game/console IDs, replaced product instances, and a
  10,000-product catalog. These do not validate Unity rendering/input performance.
- Expanded checks passed for all three requested Select All transitions, bulk
  selection with a filtered view, empty catalogs, freely ordered page navigation,
  reset, and missing-product/missing-campaign validation priority.
- Static scene checks passed for the native header/close, search Text/Placeholder,
  scroll viewport/content/handle, checkbox, and marketing row field bindings.
- Campaign checks passed for locked selection, initial cap default, zero minimum,
  upper bound, lower-cap clamping, retaining a lower value when increasing cap,
  page navigation, and reset. Static bindings passed for all six card labels/icons,
  both lock overlays, and slider fill/handle. These are not in-game UI tests.
- All six queue-priority explanations, default/reset behavior, preservation across
  page/campaign changes pass model checks. Native dropdown
  caption/item/template/scrollbar bindings were checked statically. The user has
  accepted the dropdown controls and page layout in-game.
- Scheduling checks pass for threshold equality, admitted continuation above the
  threshold, cap rotation, waiting/re-entry, blocked/removed products, cash equality,
  unaffordable products, invalid hype, all priority directions, missing dates, and
  deterministic ties. These checks do not execute native work or Harmony patches.
- Disposable-file persistence checks pass for complete configuration/paid-progress
  serialization, restored admission, typed identities, native-byte preservation,
  repeated saves, exact copies, slot reuse, older snapshots, empty stopped-state
  checkpoints, conflicts, corruption, interrupted records, and unsupported schemas.
  These checks do not execute native save/load callbacks.
- Product-list sorting checks pass for mixed product types, alphabetical names,
  raw descending hype, native release ordering, and selection preservation through
  filtering/sorting, including English/French number formats. Name queue priority
  passes both directions, affordability/continuation rules, and saved-record
  round-trip checks. Existing serialized Hype/Release Date values are unchanged.
- IO regression checks reproduce native Mono/ES3 float rounding and verify that
  it no longer rejects matching paid work or causes false historical conflicts.
  Real progress/settings differences still fail validation. During native load,
  restoring the employee-selection preference uses no notification, preventing
  the native confirmation callback while preserving ordinary manual clicks.
  Subsequent user testing reported no further save/load issues. The original
  intermittent failure's log was unavailable, so the numeric regression evidence
  does not prove the cause of that specific occurrence.

IO failures are also appended to `BepInEx/MarketingAutomation.IO.log` so a restart
does not discard the failure reason. Diagnostics distinguish missing/conflicting
records, incompatible headers, task identity/flags, and paid-progress mismatch.

Development-script checks use only disposable fixture directories:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tests/DevelopmentScripts/check-development-scripts.ps1
```

```bash
bash tests/DevelopmentScripts/check-development-scripts.sh
```

### In-game regression and extended acceptance checks

Use a disposable single-player test save with Marketing rooms. This checklist
includes the accepted core workflows and extended edge cases; it is not a list
of current defects or a claim that every item has been individually verified.

1. Check `BepInEx/LogOutput.log` for the startup fingerprint and
   `Marketing Automation UI and queue patches installed.`
2. Open Marketing Campaign. Verify Game, Game Console, and Automation appear
   without overlaps. Compare Automation's gloss, shadow, outline, label, and hover
   state with the original buttons; verify the repeat arrow appears at the left.
   The log should report creation once per picker instance.
3. Open Automation. Compare its border, background, header, icon, close button,
   and animation with the native Marketing product selector. Compare its products
   with both original selectors, including in-development/shelved/published games
   and unreleased consoles where available.
4. Compare Automation's opening animation with Game/Game Console, including
   reopening and opening while paused. X, Escape, and configured right-click
   close should return immediately to the picker without replaying its animation.
   One Escape press must close only the child; a second closes the picker.
   Check that closing the picker restores normal controls and pause behavior.
5. Select multiple games and consoles, search so some selected rows disappear,
   toggle another row, then clear the search. Check that all intended checkmarks
   and the total selected count survive. Check mixed-case search and no matches.
   Scroll with the wheel and scrollbar to the first/last row of a large catalog.
   Compare Name/Hype/Release Date list sorting with the original selectors. Check
   unreleased/shelved items, tied values, and hidden selections. Navigate away and
   back: retain the list sort. Change queue priority on page two and verify it
   does not change the display order. Confirm Name queue priority in both directions.
6. Select products, close with X, then reopen and verify no products are selected.
   Repeat with Escape, right-click when enabled, and after
   closing the entire Marketing picker. Repeat in a second room; every new opening
   starts empty. Navigating between pages must preserve selections.
   Type into search and verify game hotkeys are suppressed, then close the screen
   and verify the game input flag clears. Check long names and product tooltips.
7. Open and cancel both original Game and Game Console workflows. Verify they
   retain their original products, controls, navigation, and campaign behavior.
8. Check small/large UI scales and return to the main menu/load another test
   session. Verify layout, fresh room binding, and absence of orphaned panels or
   new log errors.
9. Compare the larger row checkboxes with native settings/campaign controls.
   With zero, two, and all products selected, click Select All and verify the
   requested none-to-all, partial-to-all, and all-to-none transitions. Repeat with
   a search filter and with a filter yielding no results: bulk actions still use
   the whole catalog. The header is checked only for a full selection.
10. Compare arrows, dots, current-page highlight, and green check with Develop a
    Game. Navigate with no products selected using both arrows and dots; only
    boundary arrows should disable. The window/header/footer must not animate or
    disappear between pages. Check search, scroll position, and checkmarks after
    returning to Products. Confirmation must remain visible on both pages.
11. With no products selected, click the green check from each page and verify
    the native missing-product dialog and Products page. Select products (also
    test search-hidden selections), confirm, and verify the missing-campaign
    dialog and Campaign page. Dismiss via the native dialog's buttons/Escape;
    the form and draft must remain intact. X/Escape on the form itself must close
    it from either page, and reopening must start empty on Products. Repeat with
    an empty catalog. Verify no game-development controls or tasks are affected.

12. Compare all six campaign names, icons, prices, gains, and caps with the original
    game/console menus. Check locked Internet Ads/Top Streamer controls. The slider
    must be disabled before selection and default to the first chosen cap. Select
    TV, lower the threshold to 69, select Internet Ads (cap 60), and check that the
    maximum, value, label, and thumb all become 60. Return to TV and verify the
    value stays 60. Check 0 and 1-point dragging/key steps.
13. Configure campaign/threshold before products, then choose products. Confirm
    from each page: both form and picker close, and the room starts a paid native
    campaign or visibly waits. Verify the exact native price is charged once and
    staffing, progress, hype gain/cap, completion news, and marketing costs match
    the original campaign. Page closure must not clear the committed queue.
14. Start below threshold and verify repeated campaigns keep that product past
    the threshold until cap. Verify rotation, re-entry after decay, all six queue sort
    modes, unavailable products, cash equal to price, insufficient cash, mixed
    game/console prices, and no purchase at cap. With no eligible work, check
    visible Waiting, zero spending/hype, and automatic resumption after eligibility.
    Check the waiting caption above the room for both hype-blocked and cash-blocked
    queues, including after loading. It must disappear when work starts or Stop
    is pressed, and follow the normal room-UI visibility toggle.
15. Stop during paid work: it finishes without a further purchase and discards
    the queue configuration. The stopped task cannot restart the discarded queue;
    configure a fresh queue after paid work ends. Stop while
    Waiting: it ends immediately. Cancel paid work: native partial refund and no
    queue continuation. Cancel Waiting: no orphan task. Transfer/replace a task:
    queue stops and does not reclaim paid work. Use two independent rooms and
    Support; verify assistants idle during Waiting and resume with the new task.
16. Use disposable saves to check saving during work/Waiting and loading again.
    Verify selected products, campaign, threshold/order, admitted continuation,
    remaining paid progress, zero duplicate charge, and automatic Waiting resume.
    Save after Stop and reload: the campaign may finish, but no queue restarts.
    Test autosaves, another slot, an older save, a byte-identical copy, missing or
    damaged mod records, a failed save if feasible, and removal of the mod. Native
    saves must contain no dormant Marketing waiter. Conflicting records must
    leave automation off rather than selecting a record arbitrarily.

## Integration evidence

- Display sorting follows `Menu_Marketing_SelectGame.DROPDOWN_Sort` and
  `Menu_Marketing_SelectKonsole.DROPDOWN_Sort`: name uses native simple game/console
  names; hype descends; release keys reproduce the native year/month text key and
  undated sentinel. `mainScript.SortChildrenByName` uses current-culture comparison.
  Pooled-row instance IDs are replaced with stable typed-product ties. Queue Name
  sorting uses the same name comparison with the chosen direction. Dropdown order
  is mapped separately from persisted enum values to preserve existing snapshots.

- Queue controls use `Menu_Marketing_SelectGame.uiObjects[1]` (native Dropdown
  GO15155/component101101). Caption Text88716, item Text92142, template Rect59747,
  and template scrollbar Handle59682 were checked against the scene. Only visuals
  and fresh uGUI Dropdown/Toggle/ScrollRect/Scrollbar components are copied and
  internally rebound; native sorting callbacks and PlayerPrefs writes are excluded.

- `Menu_Marketing_GameKampagne.Init` and its console equivalent at line 103 provide
  native serialized prices/caps/gains and unlock handling. `SetButtonColor` at
  line 169 uses `GUI_Main.colors[7]` for selection. The UI adapter reads arrays
  without invoking either menu's Init or BUTTON_OK.
- Six card roots GO2749/14532/589/15120/16865/10861 supply the campaign visuals;
  LockM4 GO6357 and LockM5 GO3746 supply native locked states. Card height is
  expanded to show both product-type prices. Slider GO13247, sourced through
  `Menu_Abo_Preis.uiObjects[2]`, supplies visuals only; a fresh Slider binds the
  mod threshold and excludes the native price callback.

- `Menu_DevGame.OpenSide(int)` at line 1013 switches child content and changes the
  page-dot colors; `NextSide(int)` at line 1096 changes pages without animating the
  frame. `BUTTON_Start()` at line 1163 validates missing settings through
  `GUI_Main.MessageBox(message, false)` and reveals the offending page.
- Native Development `GUI_Main.uiObjects[56]` is GO19970/component111489. Its
  WindowMain GO12978 contains ButtonL GO12527, ButtonR GO4345, PanelSeiten GO19152,
  and green Button_Yes GO8627. Their visuals are copied with fresh callbacks;
  two of the native page dots are retained. The source PanelSeiten Image is
  disabled; visual copying preserves that flag.
- `GUI_Main.MessageBox` at line 2909 uses uiObjects[40], native Menu_MessageBox
  GO18808. Automation passes `closeMenu=false`, temporarily raises the dialog in
  the canvas sibling order, and suspends its own Escape handler until dismissal.

- `reference/decompiled/Assembly-CSharp/Menu_Marketing_Main.cs:48–54`:
  `Init(roomScript)` is the picker-entry hook, using a postfix to refresh the
  extension's room. Original Game/Console handlers remain intact at lines 63/70.
  Additional queue, room UI, preference-restoration, and save/load hooks are
  installed through `QueueIntegration.Install`.
- `reference/decompiled/Assembly-CSharp/GUI_Main.cs:448–477` and `reference/decompiled/Assembly-CSharp/GUI_Main.cs:479–502`:
  existing open/close modal behavior owns pause and main-button restoration.
- `reference/decompiled/Assembly-CSharp/tooltip.cs:70–108` and
  `reference/decompiled/Assembly-CSharp/GUI_Main.cs:3118–3124`: native tooltip/hotkey dispatch supplies the close shortcut.
- Native selector `WindowMain` GO14106/Rect59441 (614 x 720) supplies the current
  modal shell and automatic playback of clip GUID
  `85107ce177ea55f488bb702cf8461de5`, without cloning campaign scripts.
  The reconstructed `Assets/AnimationClip/openMenu.anim` shows a root-scale
  transition from zero to one over 0.16666667 seconds and no animation events.
  Picker keyboard shortcuts are suspended while the child is open and restored
  after native Update calls to avoid closing two layers with one Escape release.
- `reference/extracted_unity_project/ExportedProject/Assets/Scenes/scene01.unity`:
  Marketing picker GO11143/component92398, window Rect56422, Game Rect48802,
  Console Rect65135, description Rect48068. The hierarchy paths are validated
  before creating the extension. Native visual properties are copied into fresh
  controls; serialized listeners and text writers are not cloned. The native
  visual-only `setFont` component is retained where present for language fonts.
- `Menu_Marketing_SelectGame.CheckGameData` at line 140 and
  `Menu_Marketing_SelectKonsole.SetData` at line 116 supply the catalog predicates.
  Their `uiPrefabs[0]` marketing row visuals and the campaign `ToggleAuto/Background`
  artwork supply rows/checkmarks, without the native single-select row scripts.
- `Menu_Arcadepreise.uiObjects[6]` supplies search visuals; a fresh InputField and
  focus adapter replace its original callbacks. No native selector Init is called.

Startup logs the game MVID and Unity/loader/Harmony versions and rejects a game
MVID other than `540f0e40-727e-4ed5-a8b6-1d988c4ec8e4`. A failed hierarchy binding
cleans up partial UI and logs once for that picker. Plugin destruction removes
its patches and controls and restores the picker dimensions. Scene destruction
cleans up the custom panel without calling into a partly destroyed game GUI.
Core gameplay is user-accepted; failure paths and unsupported-version safeguards
are not all individually runtime-tested.


## Research libraries

- [Project research](research/INDEX.md) describes Marketing Automation's implementation.
- [Shared game research](tools/mgt2-mod-toolkit/research/INDEX.md) describes reusable game and toolchain behavior.
- [Toolkit documentation](tools/mgt2-mod-toolkit/README.md) explains setup, research readiness and project ownership.

The toolkit is a Git submodule at `tools/mgt2-mod-toolkit`, sourced from
[KyleStank/mgt2-mod-toolkit](https://github.com/KyleStank/mgt2-mod-toolkit) over SSH. This mod records a specific
toolkit commit. Full initialization installs both skills after successful reference setup.
To install or relink them separately:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/mgt2-mod-toolkit/scripts/install.ps1 -ProjectRoot .
```

```bash
bash tools/mgt2-mod-toolkit/scripts/install.sh --project-root .
```

This creates links under `.agents/skills/` to the submodule's skill folders. These local links are ignored
by Git; rerun installation in each fresh checkout. Installation does not run reference setup.
