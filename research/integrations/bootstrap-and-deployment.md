---
{
  "schema_version": 1,
  "id": "bootstrap-and-deployment",
  "kind": "integration",
  "scope": "mod-specific",
  "project": "MarketingAutomation",
  "title": "Marketing Automation bootstrap and deployment",
  "summary": "Current project target/references, plugin startup/fallback/teardown and configuration/deployment scripts, separated from generic game and loader behavior.",
  "domains": ["modding", "deployment", "compatibility"],
  "keywords": ["build", "netstandard2.1", "BepInEx.Core", "HarmonyX", "DeployToGame", "initialize", "config", "plugin GUID", "MVID", "UnpatchSelf"],
  "symbols": ["MarketingAutomation.MarketingAutomationPlugin", "PluginMetadata", "QueueIntegration.Install", "DeployPlugin", "Get-ProjectLocalConfig", "Assert-BepInExInstallation"],
  "source_sets": ["marketing-automation-implementation", "mgt2-toolchain", "mgt2-runtime"],
  "related": [
    {"id": "runtime-and-source-identity", "relationship": "Native player/module identity bounds applicability of startup checks."},
    {"id": "loader-and-patching-boundaries", "relationship": "Third-party plugin and patch ownership behavior."},
    {"id": "configuration-ui", "relationship": "UI extension attached by the menu Init postfix."},
    {"id": "queue-and-room-integration", "relationship": "Queue hooks installed during bootstrap and removed on destruction."},
    {"id": "persistence-and-recovery", "relationship": "Save/load hooks are installed through QueueIntegration."}
  ],
  "created": "2026-10-22",
  "updated": "2026-10-26",
  "entry_points": [
    {"label": "Framework and references", "anchor": "f-001---this-project-targets-netstandard21-with-compile-only-game-references", "summary": "Actual project configuration; distinguish restored package graph from installed loader."},
    {"label": "Startup and fallback", "anchor": "behavior-and-call-flows", "summary": "MVID warning, signature validation, menu extension, queue hooks and cleanup."},
    {"label": "Deployment side effects", "anchor": "f-004---ordinary-builds-can-deploy-only-the-plugin-and-symbols", "summary": "Default post-build deploy, literal config, core-layout check and explicit skip."},
    {"label": "Validation limits", "anchor": "unknowns-and-validation", "summary": "Recorded Windows evidence, unverified platform/cleanup cases and current runtime coverage limits."},
    {"label": "Current startup identity checks", "anchor": "f-002---startup-warns-on-module-mismatch-and-validates-the-menu-signature", "summary": "Warn-only MVID check and separate menu signature validation, inspected 2026-10-26."}
  ]
}
---

# Marketing Automation bootstrap and deployment

## Overview and Coverage

Project target/references, plugin startup, patch ownership, and deployment side effects.
These are Marketing Automation integration findings. Generic setup belongs to the
[toolkit guide](../../tools/mgt2-mod-toolkit/README.md), contributor commands to
[AGENTS.md](../../AGENTS.md#setup-and-build), and intended behavior to [docs/behavior.md](../../docs/behavior.md).

## Source References and Applicability

- `src/MarketingAutomation/MarketingAutomation.csproj`: current target, package/reference settings and post-build target.
- `src/MarketingAutomation/MarketingAutomationPlugin.cs`: BepInPlugin, `Awake`, fallback and `OnDestroy`.
- `src/MarketingAutomation/GameIntegration/MarketingMenuIntegration.cs`: `Install(Harmony)` and `PatchInitializeMenu`.
- `src/MarketingAutomation/PluginMetadata.cs:3-8`: explicit identity/version constants.
- `src/MarketingAutomation/GameIntegration/QueueIntegration.cs:12-42`: game hook registration.
- `tools/mgt2-mod-toolkit/scripts/project-config.ps1`, `tools/mgt2-mod-toolkit/initialize.ps1`, `scripts/deploy.ps1:1-37`, and their shell equivalents:
  inspect the checked-in scripts for exact platform-specific behavior before running them.
- The existing generated `src/MarketingAutomation/obj/project.assets.json` resolves BepInEx.Core 5.4.21 and
  HarmonyX 2.7.0. This is an inspection of the existing package graph; package restore was not rerun.

Installed loader identity is independently recorded by the toolchain/runtime source sets. A package version in
the current restore graph is not a description of the DLL selected by the game's loader.

## Architecture and State Ownership

`MarketingAutomationPlugin : BaseUnityPlugin` lives in namespace `MarketingAutomation`. It owns a Harmony instance,
records its logger, installs the additive Marketing menu hook, and delegates native task/room/save hooks to
`GameIntegration.QueueIntegration`. Current startup installs the menu hook through
`GameIntegration.MarketingMenuIntegration`; both adapters extend `BaseIntegration`.
`PluginMetadata` owns the BepInEx identity constants.

The build project owns references and deployment invocation; the toolkit owns initialization/config parsing. Local `.config.local` owns only two literal absolute
directory values; it is neither executable configuration nor a portable repository artifact. Deployment scripts
own copying the built plugin/PDB into the configured installation. They do not own loader/game assemblies.

UI state, queue scheduling and save checkpoints have separate owners described by the related integration reports.
Bootstrap does not store a queue preset or serialize room/product state.

## Behavior and Call Flows

Runtime startup traced in source:

1. `Awake()` records loader logger and reports plugin loading.
2. It reads the gameplay assembly's `ManifestModule.ModuleVersionId`, logs Unity and loader/Harmony versions,
   and warns on an unexpected MVID, then continues.
3. It creates a Harmony owner using `PluginMetadata.Id` and installs `MarketingMenuIntegration` and
   `QueueIntegration` through `InstallIntegration<T>()` / `BaseIntegration.Install(Harmony)`.
4. `MarketingMenuIntegration.Install` resolves declared `Menu_Marketing_Main.Init(roomScript)`, verifies
   a void return and patches its postfix `PatchInitializeMenu` to add/bind the picker extension.
5. Any exception enters the catch, unpatches that owner and logs `MarketingAutomation failed to load: <exception>`.
6. The menu postfix rejects missing menu/room objects, reuses or adds `MarketingMenuExtension`, and binds it to the room.
7. `OnDestroy()` removes queues, removes mod menu extensions, then unpatches this owner.

The deployment target calls the project scripts with this mod's root; toolkit config parsing does not
infer that root from the toolkit location. The scripts validate the configured loader layout and copy
only the plugin DLL and optional PDB. See F-004/F-005 for failure boundaries.

Startup/menu integration was rechecked on 2026-10-26; other source traces retain their 2026-10-22 scope.

## Findings and Evidence

### F-001 - This project targets netstandard2.1 with compile only game references

Statement: The current project targets netstandard2.1, references Unity 2022.3.2 modules for compilation and marks
the installed-game references non-private.
Basis: static-observation
Standing: current
Applies to: marketing-automation-implementation
Evidence: `src/MarketingAutomation/MarketingAutomation.csproj:3-48`, PropertyGroup, PackageReference and Reference items.
Explanation: `Assembly-CSharp`, `Assembly-CSharp-firstpass` and `UnityEngine.UI` resolve through relative `lib/`
HintPaths and set `Private=false`. BepInEx.Core and Analyzers use floating package ranges. The existing restore
graph selects Core 5.4.21/HarmonyX 2.7.0; installed binary inspection separately reports loader 5.4.23.5/HarmonyX 2.9.0.
Do not replace this project's target or ship compile dependencies merely because another loader tutorial uses a
different target. A clean future restore may select different floating-package inputs and needs inspection.

### F-002 - Startup warns on module mismatch and validates the menu signature

Statement: Current startup warns and continues on a game MVID mismatch; the menu integration requires the declared Init(roomScript) signature and void return before installing its postfix.
Basis: static-observation
Standing: current
Applies to: marketing-automation-implementation
Evidence: `src/MarketingAutomation/MarketingAutomationPlugin.cs`, `Awake()` and `InstallIntegration<T>()`;
`src/MarketingAutomation/GameIntegration/MarketingMenuIntegration.cs`, `Install(Harmony)`; inspected on 2026-10-26.
Explanation: Expected MVID is `540f0e40-727e-4ed5-a8b6-1d988c4ec8e4`. The mismatch branch logs a warning without
returning; this does not establish unsupported-build compatibility.
The signature search uses `AccessTools.DeclaredMethod` with a `roomScript` parameter and void return check.
Later UI donor/array checks remain needed. Catch cleanup calls `UnpatchSelf`, not a global unpatch.

### F-003 - Identity and teardown belong to the plugin instance

Statement: Plugin identity/version constants are explicit, and teardown removes this plugin's queues, patches and menu extensions.
Basis: static-observation
Standing: current
Applies to: marketing-automation-implementation
Evidence: `src/MarketingAutomation/PluginMetadata.cs`; `MarketingAutomationPlugin.cs`, BepInPlugin attribute,
`Awake()` and `OnDestroy()`; rechecked on 2026-10-26.
Explanation: On 2026-10-26, `PluginMetadata.Id` is `com.kstank.madgamestycoon2.marketingautomation`, name is
`MarketingAutomation`, and version `1.0.0` matches the inspected project Version. BepInPlugin and Harmony
both use Id. Cleanup source intent is not proof all Unity lifetime/removal
cases were exercised. Current UI and queue implementations remain independently responsible for their owned state.

### F-004 - Ordinary builds can deploy only the plugin and symbols

Statement: With default deployment enabled, successful ordinary builds invoke scripts that copy the plugin DLL and optional PDB.
Basis: static-observation
Standing: current
Applies to: marketing-automation-implementation
Evidence: `src/MarketingAutomation/MarketingAutomation.csproj:50-65`, target `DeployPlugin`; `scripts/deploy.ps1:13-37`;
`scripts/deploy.sh`, deployment validation and copy commands.
Explanation: Design-time builds skip deployment. Missing configuration or deployment failures fail the enabled
target. The PowerShell script validates exact DLL basename and absolute existing file path; it revalidates the
configured loader layout and copies to `BepInEx/plugins`. Core-file presence establishes expected layout only,
not successful loader execution. Inspect the shell script for its corresponding checks on another platform.

### F-005 - Local directory configuration is parsed as literal data

Statement: The PowerShell configuration reader recognizes only game_directory and save_directory and validates existing absolute paths.
Basis: static-observation
Standing: current
Applies to: marketing-automation-implementation
Evidence: `tools/mgt2-mod-toolkit/scripts/project-config.ps1`, `Assert-AbsoluteDirectory`, `Assert-BepInExInstallation`, and
`Get-ProjectLocalConfig`; `tools/mgt2-mod-toolkit/initialize.ps1`, initialization and literal UTF-8 write.
Explanation: The reader splits at the first equals sign, rejects unknown/duplicate/missing keys, and does not execute
the file. Project-owned deployment supplies this mod's root from its script location, so toolkit placement and the
caller's working directory do not select another project's config. Save-directory validation does not read saves.
Initialization options and reference preparation are documented in the [toolkit guide](../../tools/mgt2-mod-toolkit/README.md#project-selection-and-configuration).

## Connections to Other Systems

Native runtime identity and loader API semantics are reusable research. This integration selects particular
compile inputs, startup checks and deployment side effects. The menu extension connects to [configuration UI](configuration-ui.md).
`QueueIntegration.Install` connects to [queue/room behavior](queue-and-room-integration.md) and
[persistence](persistence-and-recovery.md), which own the detailed native hook scopes.

## Mod Integration Implications

Keep the plugin GUID stable: it identifies both BepInEx registration and Harmony patch ownership.
Use APIs compatible with the compile references and installed loader; deploy no independent patch engine.
A matching MVID is not a byte-identity or compatibility guarantee. Native hierarchy checks remain necessary
when adapting UI, and bootstrap failure must clean up only this plugin's patches.

## Unknowns and Validation

Recorded development checks cover Windows build/deployment and disposable scripts on
Windows PowerShell and Git Bash. Native Linux/macOS execution and runtime compatibility are unverified.
The baseline loaded in the user's Windows game; subsequent source edits lack a recorded in-game smoke
check. Compilation, fixture checks and static inspection do not establish native lifecycle behavior.
See [persistence F-004](persistence-and-recovery.md#f-004---user-acceptance-supports-manual-continuity-within-stated-limits)
for the attributed gameplay baseline. Source/package inspection does not establish runtime compatibility.

- Q-001: Floating-package restores can change compile inputs; inspect the resulting graph before claiming equivalence.
- Q-002: Cross-platform runtime compatibility and behavior with different game fingerprints remain unverified.
- Q-003: Compatibility-failure cleanup, mixed patch owners and full removal need scenario-specific runtime evidence.

For a startup smoke check, use a disposable single-player session and inspect
`game_directory` + `BepInEx/LogOutput.log` for MVID/Unity/loader/Harmony reporting and
`MarketingAutomation finished loading.` after both integration installs. Exercise failure and teardown
separately; successful startup alone does not validate those branches. Follow the relevant UI, queue or
persistence report for changed integration behavior.
