---
{
  "schema_version": 1,
  "id": "bootstrap-and-deployment",
  "kind": "integration",
  "scope": "mod-specific",
  "project": "MarketingAutomation",
  "code_revision": "Working source inspected on 2026-10-22; includes uncommitted changes; citations identify the relevant files and symbols",
  "title": "Marketing Automation bootstrap and deployment",
  "summary": "Current project target/references, plugin startup/fallback/teardown and configuration/deployment scripts, separated from generic game and loader behavior.",
  "domains": ["modding", "deployment", "compatibility"],
  "keywords": ["build", "netstandard2.1", "BepInEx.Core", "HarmonyX", "DeployToGame", "initialize", "config", "plugin GUID", "MVID", "UnpatchSelf"],
  "symbols": ["MarketingAutomation.MarketingAutomationPlugin", "PluginMetadata", "QueueIntegration.Install", "DeployPlugin", "Get-ProjectLocalConfig", "Assert-BepInExInstallation"],
  "source_sets": ["marketing-automation-implementation", "mgt2-toolchain-2026-10-22", "mgt2-runtime-2026-10-22"],
  "related": [{"id": "runtime-and-source-identity", "relationship": "Native player/module identity is an input to this plugin's capability gate."}, {"id": "loader-and-patching-boundaries", "relationship": "Third-party plugin and patch ownership behavior."}, {"id": "configuration-ui", "relationship": "UI extension attached by the menu Init postfix."}, {"id": "queue-and-room-integration", "relationship": "Queue hooks installed during bootstrap and removed on destruction."}, {"id": "persistence-and-recovery", "relationship": "Save/load hooks are installed through QueueIntegration."}],
  "created": "2026-10-22",
  "updated": "2026-10-22",
  "entry_points": [
    {"label": "Framework and references", "anchor": "f-001---this-project-targets-netstandard21-with-compile-only-game-references", "summary": "Actual project configuration; distinguish restored package graph from installed loader."},
    {"label": "Startup and fallback", "anchor": "behavior-and-call-flows", "summary": "MVID/signature gate, menu extension, queue hooks and cleanup."},
    {"label": "Deployment side effects", "anchor": "f-004---ordinary-builds-can-deploy-only-the-plugin-and-symbols", "summary": "Default post-build deploy, literal config, core-layout check and explicit skip."},
    {"label": "Validation limits", "anchor": "unknowns-and-validation", "summary": "Static inspection as of 2026-10-22; build, deployment and gameplay were not rerun."}
  ]
}
---

# Marketing Automation bootstrap and deployment

## Overview and Coverage

This report describes the actual Marketing Automation working tree, not a starter architecture for every MGT2
mod. It covers project target/references, loader entry, capability gates, patch-owner lifecycle, local configuration
and deployment scripts. It does not redefine the product requirements or establish new gameplay validation.

The current bootstrap source is `MarketingAutomationPlugin.cs`; an older filename in narrative documentation is
not an alternative entry implementation. The implementation was inspected on 2026-10-22, including uncommitted
changes. Source citations identify the relevant files and symbols; no working-tree inventory is required.

## Source References and Applicability

- `src/MarketingAutomation/MarketingAutomation.csproj`: current target, package/reference settings and post-build target.
- `src/MarketingAutomation/MarketingAutomationPlugin.cs:9-73`: BepInPlugin, startup, fallback and teardown.
- `src/MarketingAutomation/PluginMetadata.cs:3-8`: explicit identity/version constants.
- `src/MarketingAutomation/GameIntegration/QueueIntegration.cs:12-42`: game hook registration.
- `tools/mgt2-mod-toolkit/scripts/project-config.ps1`, `tools/mgt2-mod-toolkit/initialize.ps1`, `scripts/deploy.ps1:1-37`, and their shell equivalents:
  inspect the checked-in scripts for exact platform-specific behavior before running them.
- `REQUIREMENTS.md` and `README.md` supply current user-reported acceptance/validation boundaries. They are product
  context; the implementation claims here are checked against the working source.
- The existing generated `src/MarketingAutomation/obj/project.assets.json` resolves BepInEx.Core 5.4.21 and
  HarmonyX 2.7.0. This is an inspection of the existing package graph; package restore was not rerun.

Installed loader identity is independently recorded by the toolchain/runtime source sets. A package version in
the current restore graph is not a description of the DLL selected by the game's loader.

## Architecture and State Ownership

`MarketingAutomationPlugin : BaseUnityPlugin` lives in namespace `MarketingAutomation`. It owns a Harmony instance,
records its logger, installs the additive Marketing menu hook, and delegates native task/room/save hooks to
`GameIntegration.QueueIntegration`. `PluginMetadata` owns the BepInEx identity constants.

The build project owns references and deployment invocation; the toolkit owns initialization/config parsing. Local `.config.local` owns only two literal absolute
directory values; it is neither executable configuration nor a portable repository artifact. Deployment scripts
own copying the built plugin/PDB into the configured installation. They do not own loader/game assemblies.

UI state, queue scheduling and save checkpoints have separate owners described by the related integration reports.
Bootstrap does not store a queue preset or serialize room/product state.

## Behavior and Call Flows

Runtime startup traced in source:

1. `Awake()` records loader logger and reports plugin loading.
2. It reads the gameplay assembly's `ManifestModule.ModuleVersionId`, logs that with Unity and loader/Harmony
   assembly versions, and rejects an unexpected MVID.
3. It resolves declared `Menu_Marketing_Main.Init(roomScript)` and verifies a void return before mutation.
4. It creates a Harmony owner using `PluginMetadata.Guid`, patches menu initialization with a postfix, then calls
   `QueueIntegration.Install(patches)`.
5. Any exception enters the catch, unpatches that owner and logs the UI-disabled error.
6. The menu postfix rejects missing menu/room objects, reuses or adds `MarketingMenuExtension`, and binds it to the room.
7. `OnDestroy()` removes queues, unpatches this owner and removes mod menu extensions.

Build/deployment flow traced in source:

1. Developers run the toolkit initializer with this mod as `ProjectRoot`, recording literal game/save locations
   in this mod's `.config.local`. Valid config can be reused; initialization then calls the toolkit
   reference coordinator by default, then installs skill links after reference setup succeeds.
   Config-only mode supports deployment setup without reference preparation or skill installation.
   Toolkit location does not select the project root.
2. An ordinary non-design-time build invokes `DeployPlugin` when `DeployToGame == true`.
3. Project-owned deployment imports the toolkit config helper, resolves `.config.local` from this mod's root
   regardless of caller directory, and validates configuration and the expected BepInEx core-file layout.
4. It validates a built `MarketingAutomation.dll` path and copies only that DLL and an available sibling PDB.
5. `-p:DeployToGame=false` prevents the project target from deploying; it does not change plugin behavior.

These are static source traces as of 2026-10-22; the listed build/deployment commands were not rerun.

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

### F-002 - Startup gates patch installation on the expected game module and menu signature

Statement: The plugin checks the game MVID and declared Init signature before installing its menu/queue patches.
Basis: static-observation
Standing: current
Applies to: marketing-automation-implementation
Evidence: `src/MarketingAutomation/MarketingAutomationPlugin.cs:16-48`, `private void Awake()`.
Explanation: Expected MVID is `540f0e40-727e-4ed5-a8b6-1d988c4ec8e4`. The signature search uses `AccessTools.DeclaredMethod`
with a `roomScript` parameter and void return check. MVID equality remains weaker than complete byte/capability
validation; later UI donor/array checks remain needed. Catch cleanup calls `UnpatchSelf`, not a global unpatch.

### F-003 - Identity and teardown belong to the plugin instance

Statement: Plugin identity/version constants are explicit, and teardown removes this plugin's queues, patches and menu extensions.
Basis: static-observation
Standing: current
Applies to: marketing-automation-implementation
Evidence: `src/MarketingAutomation/PluginMetadata.cs:3-8`; BepInPlugin attribute at `MarketingAutomationPlugin.cs:10`;
`private void OnDestroy()` at `:67-72`.
Explanation: GUID/name are `MarketingAutomation`, version `1.0.0`, matching the inspected project Version. This report
does not prescribe those identities for another mod. Cleanup source intent is not proof all Unity lifetime/removal
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
the file. Initialization reuses valid existing settings without new path arguments, requires Force to replace them,
and writes literal UTF-8 values. Its default workflow prepares references and then installs skills; ConfigOnly skips both steps.
The shared helper accepts an explicit project root, defaulting to the current directory for toolkit entry points;
project-owned deployment supplies this mod's root from its script location. Merely validating the save-directory path does not inspect save contents. Do not commit the local file or use it
to store research settings.

## Connections to Other Systems

Native runtime identity and loader API semantics are reusable research. This integration selects particular
compile inputs, startup checks and deployment side effects. The menu extension connects to [configuration UI](configuration-ui.md).
`QueueIntegration.Install` connects to [queue/room behavior](queue-and-room-integration.md) and
[persistence](persistence-and-recovery.md), which own the detailed native hook scopes.

## Mod Integration Implications

Preserve the current project while extending it. A research/documentation task does not require building or
deploying the mod. When compilation is appropriate without deployment, use the established explicit skip flag.
Do not run reference-export/decompilation/clone workflows as a substitute for inspecting existing sources.

Keep the inspection date and relevant file/symbol citations with integration evidence. A README filename or historical
scaffold proposal is not sufficient evidence of the live entry class, package graph, target framework or hook set.

## Unknowns and Validation

Performed for this report: current source inspection, generated package-graph inspection and installed binary
metadata comparison. Configuration and deployment checks use disposable fake installations and plugin files;
those checks do not establish real game integration. No mod build, package restore, actual-game deployment,
game/editor launch or personal save inspection was executed.

The current project documents user acceptance of Windows single-player UI/automation/manual saving, with autosave
not triggering and its cause undetermined. That is attributed product/runtime evidence; it does not prove every
bootstrap failure, patch conflict, removal, cross-platform script or multiplayer scenario passed.
The documented acceptance predates the current readability refactor; the current working tree still needs its
post-refactor in-game smoke check. That runtime check remains outstanding as of 2026-10-22.

- Q-001: Future floating-package restores can change compile inputs; inspect the resulting graph before claiming equivalence.
- Q-002: Cross-platform runtime compatibility and game-fingerprint behavior remain unverified here.
- Q-003: Compatibility-failure cleanup, mixed patch owners and full removal require scenario-specific evidence.

## Revision Notes

- 2026-10-22: Inspected current bootstrap/project/configuration/deployment sources and existing restore graph.
  Distinguished inspected source behavior from runtime tests.
