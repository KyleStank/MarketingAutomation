# Marketing Automation

Marketing Automation reduces Marketing room micromanagement in **Mad Games Tycoon 2**.
Choose games and consoles, a campaign, and a hype threshold. The room buys ordinary
paid campaigns to bring those products toward the campaign's hype cap, then waits
until they need marketing again.

The original Game and Game Console workflows remain available. Automation is for
single-player ordinary Marketing campaigns; it does not automate Special Marketing,
Employee Search, or Market Research.

## Installation

The repository provides source and build/deployment scripts. Install the loader before
building or copying the plugin:

1. Close the game.
2. Install **BepInEx 5 for Mono, x86** in the Windows game installation, following the
   [BepInEx installation guide](https://docs.bepinex.dev/v5.4.16/articles/user_guide/installation/index.html).
3. Build the plugin using [contributor setup](AGENTS.md#setup-and-build), which deploys
   it automatically. If you already have a built `MarketingAutomation.dll`, copy it into
   `BepInEx/plugins` under the game installation instead. The matching `.pdb` is optional.
   Do not copy game assemblies or another Harmony DLL.
4. Launch the game normally. Open a Marketing room's **Marketing Campaign** menu and
   look for **Automation** below Game and Game Console.

The inspected Windows installation uses Unity 2022.3.2f1 and BepInEx 5.4.23.5.
Other game versions are not guaranteed compatible. Linux/macOS gameplay is unverified;
multiplayer is outside the supported scope.

To remove the mod, close the game and remove `MarketingAutomation.dll` and its optional
`.pdb` from `BepInEx/plugins`. Keep the external automation data if you may reinstall.

## Using automation

1. Open **Marketing Campaign → Automation** in the room you want to use.
2. Select products on the first page. Search and list sorting help find them;
   **Select All** includes products hidden by the search filter.
3. Use the arrows or page dots to reach the second page. Choose queue priority,
   a campaign, and the hype threshold, then click the green checkmark.

The threshold controls when a product enters marketing. Once admitted, it keeps
priority toward the campaign's cap unless it becomes blocked. For example, with a
threshold of 20 and a cap of 60, a product at 20 hype can start and continue above 20
until it reaches 60. Later, it becomes eligible again when hype drops to 20 or below.

Every purchase requires enough cash; automation never uses available credit. It skips
blocked or unaffordable products and shows **Waiting for eligible products...** when
none can run. Products released later are not added automatically. Each room schedules
independently, and native Support can assist its current campaign.

**Stop Automation** prevents further purchases and discards the settings, while letting
an already-paid campaign finish. **Cancel Task** cancels immediately using the game's
normal refund. Starting again requires a new configuration. Closing the configuration
without confirming discards its draft; changing pages keeps it.

## Saves and limitations

Active automation, including Waiting, uses external records in
`MarketingAutomation/Snapshots` beside the native save files. The mod does not add fields
to native saves. Copy this folder with your saves when backing up or moving them;
Steam Cloud coverage for these records is not established.

Restoration requires a trustworthy record matching the exact saved game. Missing,
damaged, incompatible, or conflicting records leave automation off while the game
loads normally. An existing paid campaign is not purchased again. Older saves can
restore their matching automation state, including a state saved before Stop.

Manual save/load and restart continuity have user-reported validation. Autosave
restoration remains unverified. Detailed evidence and remaining runtime checks are in
[the persistence report](research/integrations/persistence-and-recovery.md#unknowns-and-validation).

## Troubleshooting and contributing

- **Automation is missing:** inspect `BepInEx/LogOutput.log` under the game installation
  for loader, plugin, or menu-binding errors. A game update may change the required bindings.
- **The room keeps waiting:** check selected products, hype threshold/cap, campaign
  unlocks, and cash. List sorting does not change queue priority.
- **Automation does not restore:** check the external records accompanied the save.
  Inspect `BepInEx/LogOutput.log` and, if present, `BepInEx/MarketingAutomation.IO.log`.
  Preserve the records rather than editing them to force a match.

For a bug report, include the mod/game versions, reproduction steps, and relevant log
messages. Contributors can start with [repository guidance](AGENTS.md), consult
[intended behavior](docs/behavior.md), and retrieve task-specific evidence from the
[research index](research/INDEX.md).
