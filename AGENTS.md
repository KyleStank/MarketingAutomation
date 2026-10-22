# Project working instructions

## Stage and source of truth

- This project has a working single-player version-one baseline of the Marketing
  automation mod for Mad Games Tycoon 2. The user has accepted the UI and reported
  functional automation and reliable manual save/load after the IO fixes.
  The scaffold, BepInEx bootstrap, build, and deployment are established.
  Inspect the existing implementation before changing project setup; do not
  replace it with an assumed framework, layout, dependency graph, or build command.
- Read REQUIREMENTS.md before planning or implementing features. It defines the
  actual product, confirmed decisions, exclusions, and remaining product questions.
  Keep product requirements there rather than duplicating them in this file.
- Subsequent explicit user decisions take precedence. Flag consequential
  contradictions and resolve affected open product decisions before implementing
  their behavior. Continue independent work where possible.
- The initial five-agent research phase is complete. Its assignment file,
  PROMPT.md, has been retired. Reading these documents does not initiate another
  research phase or require another set of research agents.

## Use the research during implementation

- Consult both research libraries actively when integrating with the game.
  - Shared native/toolchain library: tools/mgt2-mod-toolkit/research/INDEX.md.
  - This mod's implementation library: research/INDEX.md and research/integrations/.
  - Shared evidence contract: tools/mgt2-mod-toolkit/research/SCHEMA.md.
  - Source registries: each library's own SOURCES.yaml. Project reports may reference
    shared source/finding IDs; do not duplicate shared records into project research.
  Search concepts and exact native symbols, read linked findings, and follow related
  reports. Mod requirements govern product behavior. Multiplayer remains deferred
  beyond version 1.
  Run shared tools from the mod root. For project index generation/validation use
  python tools/mgt2-mod-toolkit/research/tools/rebuild_index.py --library research --shared-library tools/mgt2-mod-toolkit/research
  and the equivalent validate_library.py command. Shared generation/validation uses
  --library tools/mgt2-mod-toolkit/research without --shared-library. Optional source
  checks take --workspace . and --game-directory <installed-game>; toolkit location
  is not the source-owner workspace. Toolkit scripts/install.ps1 / scripts/install.sh links both
  skills into this project's .agents/skills; it does not initialize config or references.
- These reports contain architectural evidence and proposed designs, not an
  alternative requirements specification. Historical proposals and questions
  superseded by REQUIREMENTS.md must not be reintroduced. Source aliases in the
  reports resolve from the workspace root, not from research/.
- Follow cited code and asset references when implementing a hook. Static
  findings do not establish runtime correctness. Record new evidence and any
  consequential discrepancies without rewriting historical findings as if they
  had already been tested.

## Local paths and evidence priorities

Workspace: `C:\Users\gamem\Documents\_Development\mgt2-mod`.
The reference paths below are relative to this workspace.

| Source | Path | Use |
| --- | --- | --- |
| Installed Steam game | `F:\SteamLibrary\steamapps\common\Mad Games Tycoon 2` | Actual game installation and runtime version |
| Original managed binaries | `F:\SteamLibrary\steamapps\common\Mad Games Tycoon 2\Mad Games Tycoon 2_Data\Managed` | Authoritative assembly inputs, especially `Assembly-CSharp.dll` and `Assembly-CSharp-firstpass.dll` |
| Personal save storage | `C:\Users\gamem\AppData\LocalLow\Eggcode\Mad Games Tycoon 2` | Native storage location; see save protection rules below |
| Decompiled game sources | `reference/decompiled/Assembly-CSharp` | Readable game behavior, methods, and patch candidates |
| Decompiled supporting sources | `reference/decompiled/Assembly-CSharp-firstpass` | Supporting libraries, including ES3 persistence |
| Reconstructed Unity project | `reference/extracted_unity_project/ExportedProject` | UI hierarchy, scenes, prefabs, serialized references, and assets |
| Auxiliary export material | `reference/extracted_unity_project/AuxiliaryFiles` | Corroborating export metadata and assemblies; not authoritative originals |
| BepInEx source | `reference/BepInEx` | Loader/API reference |
| HarmonyX source | `reference/HarmonyX` | Matching bundled patch-engine reference, pinned to 2.9.0 |
| Decompilation inventory | `reference/decompiled/assemblies.tsv` | Original input paths, SHA-256 hashes, and per-assembly results |
| Downloaded tools | `reference/bin/AssetRipper-{platform}-{arch}` | Pinned AssetRipper 2.0.0 executable |
| Tool logs | `reference/logs/AssetRipper` and `reference/logs/ILSpy` | Per-run diagnostics |

- Prioritize installed binaries and matching decompiled sources for behavior;
  prioritize the reconstructed Unity assets for tracing UI structure. The export
  is not the original Unity project or a replacement game build. Verify binary
  versions/fingerprints against the reports when selecting references or hooks.
- The installed game uses x86 Mono with Unity 2022.3.2f1. The established loader
  is BepInEx 5 x86 with its bundled HarmonyX; plugin loading is confirmed.
  Do not ship a second independent `0Harmony` from reference/HarmonyX. Inspect the
  actual selected distribution rather than assuming source versions prove runtime
  compatibility.

## Current scaffold and build

- Solution: `MarketingAutomation.sln`; plugin project:
  `src/MarketingAutomation/MarketingAutomation.csproj`.
- Target `netstandard2.1` with BepInEx 5. The referenced Unity 2022.3.2 modules
  require netstandard 2.1, and the installed game's netstandard assembly is
  version 2.1.0.0. This supersedes the earlier netstandard2.0 research candidate.
- Build from the workspace root with `dotnet build MarketingAutomation.sln`.
  The scaffold built successfully on 2026-10-21 with zero warnings/errors after
  this framework correction. The user confirmed on 2026-10-21 that the initial
  plugin loads in the installed game's logs and later confirmed that the initial
  Automation UI works in-game and accepted the styled entry and rebuilt mixed
  product selector, checkbox/Select All styling, and Development-style multipage
  layout, campaign controls, threshold slider, queue controls, and campaign panel.
  The user verified starting automation from a pre-mod save, saving into a new
  slot, and consistently restoring its observed state on repeated loads.
  Full application restart and Waiting restoration also work in user tests.
  After the paid-progress rounding and load-time employee-toggle fixes, the user
  reported no further save/load issues and no issues with the suggested manual-save
  scenarios. Preserve this working baseline. Autosave has not triggered in their
  testing; it reportedly also failed before the mod. Its cause is undetermined,
  and autosave integration remains unverified. See README.md for validation scope.
- Developers first run `tools/mgt2-mod-toolkit/initialize.ps1` (Windows) or `bash tools/mgt2-mod-toolkit/initialize.sh`
  (Linux/macOS) from the mod root, or select it using -ProjectRoot / --project-root.
  Tooling and its pinned manifest live in the toolkit; .config.local and reference/
  outputs stay in the selected mod. They supply absolute game-installation and save-directory
  paths. The ignored `.config.local` stores the two literal UTF-8 `key=value`
  entries `game_directory` and `save_directory`. Never execute its contents or
  commit machine-specific settings. Valid config is reused; `-Force`/`--force` permits replacement.
  Initialization calls scripts/setup-references in the toolkit, then scripts/install
  only after reference setup succeeds. -ConfigOnly / --config-only writes/validates
  config without preparing references or installing skills. Skills run only the
  read-only scripts/check-project-setup.py check and direct the human to initialize when
  setup is missing. They never run initialization or reference preparation themselves.
- Successful ordinary builds run `DeployPlugin`, which calls `scripts/deploy.ps1`
  on Windows and `scripts/deploy.sh` elsewhere. Scripts validate BepInEx 5 core
  file presence and copy only the plugin DLL and optional PDB into the configured
  game's `BepInEx/plugins`. Missing or invalid config and deployment failures
  fail the build when deployment is enabled.
  `dotnet build MarketingAutomation.sln -p:DeployToGame=false` explicitly skips
  deployment. Design-time builds skip it as well. DLL/PDB deployment hashes were
  verified on Windows. Never add machine-specific paths back into the project.
- Script checks: `powershell -NoProfile -ExecutionPolicy Bypass -File tests/DevelopmentScripts/check-development-scripts.ps1`
  or `bash tests/DevelopmentScripts/check-development-scripts.sh`. These tests
  create and clean up bounded disposable fixtures, not real saves. Windows
  PowerShell and Git Bash execution are verified; native Linux/macOS execution
  and runtime assembly compatibility remain unverified. This setup does not
  restore or regenerate decompiled/reference assets. Toolkit setup checks run with
  python -m unittest discover -s tools/mgt2-mod-toolkit/tests -p 'test_*.py'
  and use only disposable inputs with stubbed external tools.
- Toolkit `tools/mgt2-mod-toolkit/scripts/setup-references.ps1` / `tools/mgt2-mod-toolkit/scripts/setup-references.sh` orchestrate the existing
  clone, AssetRipper download/export, and ILSpy scripts after validating local
  configuration. They stop on failure and forward an optional transport choice.
  The downloader deletes only the current platform/architecture's installation
  before downloading a fresh pinned copy. Do not run the long workflow during implementation;
  validate orchestration with disposable stubs instead.
- Installed loader inspection on 2026-10-21 found BepInEx 5.4.23.5 and its bundled
  HarmonyX 2.9.0. The existing BepInEx.Core package resolves to 5.4.21 with
  HarmonyX 2.7.0 compile references. Use compatible APIs from that scaffold;
  deploy only the plugin DLL/PDB, never the package's patch engine or game DLLs.
- Assembly-CSharp, Assembly-CSharp-firstpass (ES3 path resolution), and
  UnityEngine.UI are compile-only references under relative `lib/`
  (`Private=false`). The UI patch checks the researched game
  MVID at startup and validates its menu hierarchy on first use. An unsupported
  binding logs a diagnostic and leaves the original Marketing choices available.
- The current UI milestone has the Automation entry, native selector modal shell,
  searchable mixed catalog with native list sorting, native-size checkboxes, Select All, multi-selection,
  and native Development-style arrows/page dots/green confirmation on a shared
  multipage frame. The campaign page has six choices with native prices, caps,
  gains, unlocks, a 0-to-campaign-cap integer threshold slider, and native queue
  priority/order dropdowns above the campaign heading. Both sort-field dropdowns
  show Name, Hype, Release Date. Keep display ordering separate from execution
  priority, and preserve existing serialized enum values through explicit UI mapping.
  A new form's queue defaults to
  Hype / Ascending; the combo explanation updates live. Missing-settings errors
  retain the form. Valid confirmation closes it and transfers a copied configuration
  to a room-local MarketingQueue, which dispatches ordinary native paid campaigns.
  Core queue/UI behavior is user-accepted in-game; isolated edge checks remain
  distinct from runtime evidence. Active queues are checkpointed
  externally beside native saves under MarketingAutomation/Snapshots. Match exact
  native bytes, reject conflicting/damaged records, preserve history, and adopt
  native paid progress without another purchase. Stop clears all configuration;
  it is never retained as a preset. Draft selections belong to the open form only; closing
  its root from either page clears them. Navigation preserves them without
  animating the frame. Both pages are reachable with no selections; the green
  check validates missing products first, then the missing campaign. Draft campaign
  and threshold settings also reset on closure and survive page navigation.
  Product keys use type/native ID/runtime instance identity.
- Persistence wraps the native `savegameScript.Save(int)` with a prefix/finalizer
  and `Load` with a prefix/postfix. Preserve execution and exceptions of the original
  native save method. Do not replace its serializer, alter its file format, or
  write mod fields into native saves. Temporarily exclude only owned transient
  waiters/completed tasks and restore live bindings after saving; paid work remains
  native. The mod does not patch the autosave scheduler or modify its preferences
  or countdown. Do not attribute the untriggered autosave to either game or mod
  without evidence.
- Run `dotnet run --project tests/MarketingAutomation.Checks/MarketingAutomation.Checks.csproj`
  for isolated model behavior checks. This .NET 8 executable links the production
  selection, form, campaign configuration, scheduling, snapshot validation, and
  persistence code. Persistence checks use disposable files under the test output
  directory; they never inspect personal saves. The checks do not load Unity, run
  the game, or deploy the plugin.
  Follow README.md for native integration evidence and in-game acceptance checks.

## English code and documentation

- Follow `.editorconfig`: four spaces, Allman braces on every control-flow body,
  one statement per line, and consistent whitespace. Wrap long calls at meaningful
  argument boundaries, aiming for 120 columns. Use descriptive names and keep
  helpers near the workflow they support; comments explain reasons or native-game
  constraints rather than narrating obvious statements.
- Source folders are `UI` (form/pages/selection), `UI/Controls` (reusable native
  visual adapters), `Automation` (queue execution/policy), `GameIntegration`
  (native hooks/catalogs), and `Persistence` (external checkpoints). `MarketingAutomation.cs`
  remains the bootstrap. Existing types share the `MarketingAutomation` namespace;
  preserve persisted type/member names and enum values during refactoring.
- `PluginMetadata.cs` owns the explicit BepInEx GUID/name/version constants.
  Keep the GUID stable and the version aligned with the project version.
  `BepInEx.PluginInfoProps` is intentionally removed; do not regenerate metadata.
- `UI/AutomationScreen.cs` owns the modal lifecycle/navigation/confirmation.
  Its regular C# `ProductListView` owns search/sort/selection/scrolling, with pooled
  `ProductRow` renderers. Forward lifecycle operations from the form; changing
  pages must not reset the draft. Avoid introducing independent lifecycle callbacks
  or abstractions unless they have a concrete responsibility.
- Formatting commands: `dotnet format whitespace MarketingAutomation.sln --no-restore`
  and `dotnet format style MarketingAutomation.sln --no-restore --diagnostics IDE0011 --severity info`.
  Run the equivalent commands against the checks project when editing tests.
  Format only these actual projects; never target `reference/`.
- Write all new mod-owned C# names in clear English: namespaces, types, methods,
  properties, fields, parameters, locals, constants, and files. Use English for
  comments, XML documentation, diagnostics, and new default UI text. Follow the
  conventions above.
- Do not copy German naming from the decompiled game into the mod's own domain
  model. Give adapters and wrappers descriptive English names.
- Preserve exact external game symbols where required: calls, overrides, patch
  targets, reflection strings, serialized identifiers, and asset lookup names
  must still match the game even when they are German. Explain unfamiliar native
  names in English where helpful; do not rename external APIs or lookup keys.
- Use Support for the assisting-room mechanic. Verify references using Follow
  rather than assuming those labels identify the same or separate mechanics.

## Implementation boundaries and validation

- Preserve user edits and unrelated files. The user-authorized AssetRipper
  installers in `tools/mgt2-mod-toolkit/scripts/asset-ripper/download.ps1` and `download.sh` may create
  tool installations under `reference/bin/AssetRipper-{platform}-{arch}` when run.
  They pin version 2.0.0 and delete the existing platform/architecture destination
  before downloading again. Validate the exact deletion boundary, refuse linked
  directories, and preserve other installations. Downloaders do not export assets.
  Download/extraction was not run during implementation; only syntax was checked.
  The user also authorizes `tools/mgt2-mod-toolkit/scripts/asset-ripper/extract.ps1` and `extract.sh`
  to run the downloaded tool headlessly and export the configured game into
  `reference/extracted_unity_project`, with logs under `reference/logs/AssetRipper`.
  These scripts replace only `reference/extracted_unity_project`, validating the
  deletion boundary and refusing linked destinations. The previous export remains
  until game loading succeeds; failed exports retain partial output and logs.
  They stop only their own server. The user confirmed PowerShell extraction works.
  Do not run the long export during implementation;
  the user will test it. Existing historical reference exports remain read-only.
  `tools/mgt2-mod-toolkit/scripts/ilspy/decompile.ps1` and `decompile.sh` may regenerate only
  `reference/decompiled` from the original installed Managed DLLs, using the pinned
  toolkit `tools/mgt2-mod-toolkit/dotnet-tools.json` manifest and `dotnet tool restore`. They record hashes,
  tool metadata, and per-assembly logs under `reference/logs/ILSpy`; failures return
  an error. Native/unrecognized DLLs are skipped. Research C# citations use the
  current ILSpy output. Keep them aligned with the named source symbols when
  regenerating references. Implementation
  checks covered syntax/tool discovery; the user confirmed full decompilation
  and a successful rerun.
  `tools/mgt2-mod-toolkit/scripts/clone-references.ps1` and `clone-references.sh` may create
  `reference/BepInEx` at tag `v5.4.23.5` and `reference/HarmonyX` at tag `v2.9.0`,
  including recursive submodules at recorded commits. An explicit `https` or `ssh`
  transport choice is required via argument or a prompt showing both valid values,
  and applied to GitHub submodules through command-scoped
  URL rewrites. The user authorizes deletion of existing BepInEx/HarmonyX destination
  directories before fresh clones, including local changes. Validate the exact
  deletion boundaries and refuse files or linked destinations/root directories.
  Do not modify global Git settings. Preserve unrelated reference directories.
  Syntax and local URL rewriting were checked; actual clones were not run.
  Otherwise treat reference/ as read-only: do not
  edit, regenerate, format, build, or restore dependencies there. Nested reference
  instructions apply to those libraries, not to this mod's project conventions.
- Never rewrite original installed game binaries or assets. Build in the actual
  mod project. Deploy mod/loader files and run the game only within the user's
  requested implementation or testing workflow; document the files affected.
- Do not build or launch the reconstructed Unity project as a shortcut to testing
  the mod. The editor can share the personal save storage above.
- Do not inspect personal save contents without explicit authorization. Do not
  rewrite original saves or add mod data to their format. Keep custom state
  external as required by REQUIREMENTS.md. Use disposable test saves for runtime
  checks; ordinary native saving during authorized gameplay remains expected.
- Scope patches to mod-owned UI/tasks and preserve original workflows. Consult
  the research on task ownership, deferred destruction, room caches, Waiting,
  Stop Automation, Support, and save/load before changing those integration points.
- Use the scaffold's established build and test commands once available. Run
  checks appropriate to each change and report what was actually validated.
  Distinguish compilation, static inspection, and in-game tests. Never describe an
  unexecuted runtime check as passed.
- Support important architectural claims with source paths, types, method
  signatures, and line numbers where useful; identify asset objects/components
  precisely. Use local evidence first and official, versioned documentation when
  external evidence is needed. Do not invent symbols or compatibility guarantees.
- Update these instructions when the scaffold establishes concrete build,
  dependency, deployment, and test conventions.
