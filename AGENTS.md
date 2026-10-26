# Working on Marketing Automation

This is a single-player Mad Games Tycoon 2 mod that maintains selected games and consoles through
native paid Marketing campaigns. Preserve the working implementation and unrelated user edits.
[README.md](README.md) is for players; [docs/behavior.md](docs/behavior.md) owns intended behavior.
Later explicit user decisions take precedence. Raise consequential contradictions before changing
behavior, and continue independent work where possible.

## Find the context for your change

Read only the material relevant to the task:

| Change | Supporting evidence |
| --- | --- |
| Form, selection, sorting or native controls | [Configuration UI](research/integrations/configuration-ui.md) |
| Dispatch, Waiting, Stop/Cancel, task ownership or Support | [Queue and room integration](research/integrations/queue-and-room-integration.md) |
| Save/load, checkpoint compatibility or paid-task adoption | [Persistence and recovery](research/integrations/persistence-and-recovery.md) |
| Loader, dependencies, startup or deployment | [Bootstrap and deployment](research/integrations/bootstrap-and-deployment.md) |
| Native behavior or a new integration point | [Shared research index](tools/mgt2-mod-toolkit/research/INDEX.md) and [project index](research/INDEX.md) |

For game integration, search both libraries by concept and exact native symbol; follow relevant
findings and verify consequential hooks against matching sources. Prefer original installed binaries
and matching decompiled code for behavior, and reconstructed Unity assets for UI hierarchy. Exports
are evidence, not an original Unity project. Check source applicability; static findings do not prove
runtime correctness. Research proposals do not override intended behavior.

Keep shared native/toolchain evidence and project implementation evidence in their separate libraries.
When updating research, follow the shared [schema](tools/mgt2-mod-toolkit/research/SCHEMA.md), retain
useful finding/source identities, and rebuild/validate only affected indexes with its tools.

## Setup and build

Run commands from the mod root. Fetch the pinned toolkit with
`git submodule update --init --recursive` (the configured remote requires GitHub SSH access).
Follow the toolkit's [setup instructions](tools/mgt2-mod-toolkit/README.md#project-selection-and-configuration)
for human-run initialization and reference preparation. The plugin targets `netstandard2.1` with
BepInEx 5; isolated checks target .NET 8 and need its runtime. Inspect the existing project before
changing dependencies or setup.

```text
dotnet build MarketingAutomation.sln
dotnet build MarketingAutomation.sln -p:DeployToGame=false
```

**An ordinary successful build deploys** `MarketingAutomation.dll` and an optional PDB into
`game_directory/BepInEx/plugins`; the second command builds without deployment. Invalid configuration
or a failed copy fails the deployment step. Restart the game to load a deployed build. Game DLLs in
`lib/` are compile-only references; never deploy them or an additional `0Harmony` assembly.

Read ignored `.config.local` as literal UTF-8 `key=value` data with exactly `game_directory` and
`save_directory`. Never execute it, commit it, or copy its values into documentation. Use repository-relative
paths and configuration keys plus relative suffixes for external files.

## Validation

Run checks appropriate to the change; distinguish compilation, isolated checks, static inspection and
in-game observations. Prior user acceptance does not automatically cover later source edits. Relevant
integration reports record runtime gaps and focused manual checks. Do not claim unexecuted checks passed.

```text
dotnet run --project tests/MarketingAutomation.Tests/MarketingAutomation.Tests.csproj
powershell -NoProfile -ExecutionPolicy Bypass -File tests/DevelopmentScripts/check-development-scripts.ps1
```

The first command checks linked production models and disposable-file persistence without Unity,
game execution or deployment. The second checks development scripts using disposable fixtures;
its Bash equivalent is `bash tests/DevelopmentScripts/check-development-scripts.sh`.
For toolkit setup changes, use
`python -m unittest discover -s tools/mgt2-mod-toolkit/tests -p 'test_*.py'`.

For C# changes:

```text
dotnet format whitespace MarketingAutomation.sln --no-restore
dotnet format style MarketingAutomation.sln --no-restore --diagnostics IDE0011 --severity info
```

Use the equivalent commands against `tests/MarketingAutomation.Tests/MarketingAutomation.Tests.csproj`
when editing checks. Never target `reference/` with formatting, builds or dependency restoration.

## Working boundaries

- Preserve unrelated files and existing edits. Scope patches to mod-owned UI/tasks and retain native workflows.
- Never rewrite original installed game binaries or assets. Deploy mod/loader files and launch the game
  only within the user's requested implementation or testing workflow; report affected files.
- Do not inspect personal save contents without explicit authorization. Never rewrite native saves or
  add mod fields to their format. Use disposable saves for runtime checks; ordinary native saving during
  authorized gameplay is expected. Custom checkpoints remain external.
- Treat `reference/` as read-only during implementation. Initialization and reference preparation are
  human-run toolkit operations; do not run them to satisfy a missing setup check. Never build or launch
  the reconstructed Unity project. Its editor may share personal save storage.
- When changing reference tooling, preserve its exact replacement boundaries and refusal of linked
  destinations; test with disposable stubs instead of real downloads/exports. Do not alter global Git settings.
- For research readiness, use only the read-only
  `python tools/mgt2-mod-toolkit/scripts/check-project-setup.py --project-root .` check.
  Follow the selected skill's missing-setup workflow if it fails.

## Code conventions

Follow [.editorconfig](.editorconfig). Use clear English for mod-owned
names, comments, diagnostics and default UI text. Preserve exact native symbols, reflection strings,
serialized identifiers and asset keys. Call the assisting-room mechanic **Support**.

Optimize source for human review: use descriptive method names, separate conceptual steps with
whitespace, and place helpers directly below their callers where practical, regardless of visibility.
Group statements by purpose (validation, state changes, native binding, presentation), leaving a
blank line between those steps. A formatter cannot supply this organization; review it explicitly.
Brace-free single statements are allowed; retain braces for multiline bodies. Explain German native
symbols and asset names in English once per relevant file. Comment non-obvious constraints and
decisions, rather than restating the code. Aim for files under 500 lines by extracting cohesive
responsibilities into separate classes, not partial files.

Choose folders and namespaces by responsibility, and keep dependencies directed toward shared
models rather than presentation code. Separate pure scheduling decisions, native game adaptation,
UI construction/lifecycle and checkpoint storage/recovery. Keep each piece of mutable state with
one clear owner; use small concrete collaborators instead of generic frameworks or partial classes.
Keep helpers near their workflow and explain native constraints rather than obvious code.
Expose native UI array slots through named adapter properties, with bindings verified against native
sources; keep numeric slot knowledge at that integration boundary rather than in calling workflows.
Preserve persisted type/member names and enum values during refactoring; namespace moves must
also preserve serialized contract identities. Treat the plugin bootstrap and `BaseIntegration`
as the user's reference style; ask about specific changes before refactoring those two files.

`PluginMetadata.cs` owns the BepInEx GUID/name/version. Keep the GUID stable and its version aligned
with the project version; do not regenerate plugin metadata. Keep these instructions current when
build, deployment or validation conventions change.
