---
{
  "schema_version": 1,
  "id": "configuration-ui",
  "kind": "integration",
  "title": "Marketing Automation configuration UI integration",
  "scope": "mod-specific",
  "summary": "Automation menu bindings, draft ownership, pooled selection, independent sorting, control callbacks and queue handoff, with runtime coverage limits.",
  "project": "MarketingAutomation",
  "domains": ["marketing", "ui", "mod-integration"],
  "keywords": ["Automation entry", "configuration draft", "Select All", "pooled rows", "search", "multipage", "threshold", "queue priority", "display sorting", "Escape", "native control adapters", "validation dialog"],
  "symbols": ["MarketingMenuIntegration.PatchInitializeMenu", "MarketingMenuExtension", "AutomationScreen", "AutomationFormState", "AutomationCampaignPage", "ProductListView", "ProductRow", "ProductSelection", "ProductKey", "ProductListSorting", "NativeDropdown", "NativeUi", "SearchInputFocus", "CampaignConfiguration", "ProductSortOptions", "NativeCampaignCatalog", "MarketingQueue.TryStart"],
  "source_sets": ["marketing-automation-implementation", "mgt2-managed", "mgt2-unity-export"],
  "related": [
    {"id": "ui-modal-and-control-lifecycle", "relationship": "Primary native visual, modal, focus, animation and callback constraints."},
    {"id": "marketing-product-catalog-and-ordering", "relationship": "Native membership and display-sort rules copied by the adapter."},
    {"id": "marketing-campaign-configuration", "relationship": "Native campaign data/unlocks and original flows preserved by this separate form."},
    {"id": "queue-and-room-integration", "relationship": "Owns committed execution after UI validates and hands over copied configuration."},
    {"id": "persistence-and-recovery", "relationship": "Owns active-queue checkpoints; this form's drafts are transient."},
    {"id": "bootstrap-and-deployment", "relationship": "Owns plugin startup/compatibility checks and installed build identity."}
  ],
  "created": "2026-10-22",
  "updated": "2026-10-26",
  "entry_points": [
    {"label": "Implementation ownership map", "anchor": "architecture-and-state-ownership", "summary": "Form, product view, rows, selection model and visual adapters have distinct responsibilities."},
    {"label": "Open navigate close confirm", "anchor": "behavior-and-call-flows", "summary": "Picker layering, root closure reset, freely navigable child pages and native error dialogs."},
    {"label": "Display sort versus queue priority", "anchor": "f-003--display-order-and-queue-priority-share-labels-but-use-separate-state-and-rules", "summary": "Native descending list keys versus configurable dispatch directions and persisted enum mapping."},
    {"label": "Actual validation boundary", "anchor": "unknowns-and-validation", "summary": "User acceptance predates current readability refactor; current evidence is static only."},
    {"label": "Visual binding locators", "anchor": "native-visual-bindings", "summary": "Mod donor use and export-local identities requiring binding revalidation."}
  ]
}
---

# Marketing Automation configuration UI integration

## Overview and Coverage

The inspected UI adds Automation to the native Marketing picker, adapts native visuals,
owns a two-page draft and passes confirmed configuration to a room queue.
[Configuration policy](../../docs/behavior.md#configuration) defines intended behavior;
this report records bindings, lifecycle constraints and implementation limitations.
Queue execution and native saving have separate [queue](queue-and-room-integration.md)
and [persistence](persistence-and-recovery.md) reports.

## Source References and Applicability

Source set: `marketing-automation-implementation`, with native dependencies
`mgt2-managed` and `mgt2-unity-export`.
The implementation was inspected on 2026-10-22.
Later donor-copy edits in `NativeUi.cs`, `UiFactory.cs` and `MarketingMenuExtension.cs`
are not covered by that inspection or by prior runtime acceptance.

`U/` below denotes `src/MarketingAutomation/UI/`. `G/` denotes
`src/MarketingAutomation/GameIntegration/`. `A/` denotes
`src/MarketingAutomation/Automation/`. Cited paths and line ranges refer to this
source inspection. Native donor identities are owned by the linked system
reports; exported GUIDs are not runtime identity guarantees.

### Native visual bindings

The table identifies export-local donors used by the mod. Native provenance and constraints
are in the linked shared UI/campaign/catalog reports. These locators require revalidation
against a regenerated export; they are not installed-game identity checks.

| Donor / binding | Recorded adaptation |
| --- | --- |
| Marketing picker GO11143/component92398; Window Rect56422, Game Rect48802, Console Rect65135, description Rect48068 | Validate required hierarchy before extension creation; preserve original callbacks. |
| Selector WindowMain GO14106/Rect59441, 614 x 720; openMenu clip GUID `85107ce177ea55f488bb702cf8461de5` | Modal shell and scale animation, zero to one over 0.16666667 seconds with no animation events. |
| Selector `uiObjects[1]`, GO15155/Dropdown101101; caption Text88716, item Text92142, template Rect59747, scrollbar Handle59682 | Queue dropdown visuals with fresh internally rebound controls, excluding native sorting callbacks and PlayerPrefs writes. |
| Campaign cards GO2749/14532/589/15120/16865/10861; locks GO6357/GO3746 | Expanded cards show both product-type prices; selected color comes from `GUI_Main.colors[7]`. |
| `Menu_Abo_Preis.uiObjects[2]`, slider GO13247 | Visual donor only; fresh threshold Slider excludes native subscription-price callback. |
| Development GUI index 56, GO19970/component111489; WindowMain GO12978; arrows GO12527/GO4345; PanelSeiten GO19152; green check GO8627 | Fresh callbacks, two retained page dots; preserve disabled source PanelSeiten Image. |
| Message box GUI index 40, GO18808 | `MessageBox(message,false)`, temporary sibling-order ownership, suspend form Escape until dismissed. |
| `Menu_Arcadepreise.uiObjects[6]`; marketing `uiPrefabs[0]`; `ToggleAuto/Background` | Search visuals with fresh InputField/focus adapter; row/checkbox visuals without single-select row scripts. |

The visual-only `setFont` preserves language fonts; serialized business listeners and
text writers require separate handling. See F-004 for the inspected copy boundary.

## Architecture and State Ownership

| Owner | Responsibility and source |
| --- | --- |
| `MarketingAutomationPlugin` / `MarketingMenuIntegration` | Startup logs MVID and warns on mismatch; the integration validates Init signature and its postfix creates/binds the extension. Plugin teardown removes it. Rechecked on 2026-10-26 in `MarketingAutomationPlugin.Awake/OnDestroy` and `GameIntegration/MarketingMenuIntegration.Install/PatchInitializeMenu`; see bootstrap F-002. |
| `MarketingMenuExtension` | One component per native picker; binds room, adds entry, retains picker underneath child form, suspends/restores shortcuts, restores native layout on cleanup; `U/MarketingMenuExtension.cs:9-254` |
| `AutomationScreen` | Shared frame, persistent arrows/dots/confirm, room binding, child-page switching, validation-dialog ownership, queue handoff and root-close draft reset; `U/AutomationScreen.cs:8-261` |
| `AutomationFormState` | Pure page state and missing-products-before-missing-campaign validation; `U/AutomationFormState.cs:3-33` |
| `ProductListView` | Catalog snapshot, search/sort, selection model, full-catalog Select All, scrollbar and bounded row pool; regular C# class forwarded by the form, `U/ProductListView.cs:9-314` |
| `ProductRow` | Render/bind one current product at an offset, fresh click action, type icon, checkbox, native labels/tooltip; `U/ProductRow.cs:8-115` |
| `ProductSelection` / `ProductKey` | HashSet membership independent of visible rows; identity includes kind/nativeID/runtime instanceID; `U/ProductSelection.cs:3-82` |
| `AutomationCampaignPage` | Six cards, live native data/unlocks, slider, draft priority and explanation; `U/AutomationCampaignPage.cs:10-257` |
| `CampaignConfiguration` | Selected campaign, nullable first threshold, clamp/reset, priority/direction and explanation; `A/CampaignConfiguration.cs:22-89` |
| Visual adapters | `NativeUi`, `UiFactory`, `NativeDropdown` build fresh visuals/controls; `SearchInputFocus` owns focus handoff without native search callbacks |

## Behavior and Call Flows

### Opening, navigating and closing

1. Native picker Init postfix calls `MarketingMenuExtension.Bind(menu,room)`.
   Build validates required native paths and creates the entry/form once. It
   extends picker height/description placement and copies Game button visuals
   including effects and children (`Bind/Build:28-101`; `UiFactory.CreateMenuEntry:48-88`).
2. Entry `Open()` checks room/form state, calls `AutomationScreen.Show(room)`,
   raises the child panel, disables underlying picker tooltip shortcuts and
   uses `ActivateMenu(panel)` (`extension:103-134`). `iconAutomatic` is found
   by sprite name among room-control prefab Images (`:187-201`).
3. Show resets form to Products, resets campaign configuration, reads catalog,
   clears selection/search and defaults display sort to Name (`screen:212-229`,
   `ProductListView.Show:72-81`, `AutomationCampaignPage.Show:150-166`).
4. Page arrows/dots call OpenPage then RefreshPages; only ProductsPage and
   CampaignPage content objects change active state. The WindowMain animation
   is retained and is not restarted by normal navigation (`screen:62-137`).
5. X/Escape calls extension Back: hide the common panel and restore picker
   shortcuts in LateUpdate, leaving global modal state open (`:136-144,167-173`).
   Screen root OnDisable clears selection, cancels pending scroll reset, resets
   form/campaign and releases any owned validation dialog (`screen:241-261`).
   Hiding a child page does not execute the screen root's OnDisable.

### Product selection, search and rendering

`MarketingProduct.ReadCatalog(companyId)` reproduces native game/console predicates
without calling singleton menu Init/SetData (`G/MarketingProduct.cs:63-87`). It
builds typed keys using nativeID and `GetInstanceID` (`:46-58`). Search compares a
trimmed query using `OrdinalIgnoreCase` against cached simple names
(`ProductSelection.MatchesSearch:80-81`). Filtering creates a visible list but
does not delete selected keys. Select All uses the full current available-key
list, including search-hidden products; partial/empty becomes all, full becomes
none, and empty catalog disables the control (`ProductListView:119-156,274-296`).

The view is 456 units tall with 64-unit row stride; pool size is
`ceil(viewportHeight/64)+2` (`:58-62,189-220`). Rows are 56 units high and positions
are changed as scrolling selects a new first visible index (`ProductRow:10,28-72`;
`ProductListView.RenderRows:298-313`). The original native row script is not copied,
so hiding a pooled row does not invoke its destructive OnDisable callback.

Filter snapshots sort values from live product getters, changes content height,
resets scrolling and schedules a one-shot layout correction. `UpdateLayout`
waits for a later frame, active ScrollRect and nonzero scale, forces canvas layout,
then sets normalized top (`ProductListView:104-117,222-259`). Page navigation does
not rerun Show/Filter, preserving the draft and scroll position. This is actual
adapter code; native ScrollRect visual behavior still needs its own runtime check.

The catalog is read at opening rather than continuously rebuilt while open.
Existence checks protect selection/row binding, but do not amount to a fresh full
catalog-membership test on every keystroke. Dispatch performs its own revalidation.
ProductRow updates its text when the bound product object changes, not on every
render of the same object (`ProductRow.Bind:74-93`). These are refresh boundaries,
not a claim that displayed hype always updates while time passes.

### Campaigns, threshold and priority

The page copies the native campaign grouping root's visual components while
excluding its children/grid; creates six 270x88 cards with separate game/console
prices, gain/cap and native lock overlays; and uses selection color colors[7]
(`campaign page:35-92,178-189,239-255`). `NativeCampaignCatalog.Read` validates
six-array lengths, nonnegative prices, positive work/hype data, shared gain/cap
equality, and unlocks 56/57 (`G/NativeCampaignCatalog.cs:16-58`). It does not call
native campaign Init to obtain those values.

The threshold Slider is fresh with minimum 0, integer steps and disabled until a
campaign exists (`campaign page:120-148`). First selection defaults threshold to
cap; lowering cap clamps it, raising cap retains a lower prior threshold;
SetThreshold clamps 0..cap (`CampaignConfiguration:56-89`). Refresh updates
max/value, range, thumb and explanation together. The UI describes inclusive
admission and continuing toward cap; execution enforcement belongs to the queue.

Priority UI indices map explicitly to persisted enum values; see F-003. Display sorting
and execution priority do not share mutable state.

### Confirmation and error dialogs

`Confirm()` first validates selected count, then selected/unlocked campaign. On
failure it reveals the relevant page and opens `MessageBox(message,false)`.
The dialog's original sibling position is saved, raised above the appended mod
panel, and restored after dismissal in LateUpdate. The form's Escape shortcut
is disabled while it owns that dialog (`screen:139-210`). Draft state remains.

For a complete form it obtains a new selected-product List and calls
`MarketingQueue.TryStart(room,manager,selected,campaign,threshold,priority,direction)`.
That method copies the List and derives selected typed identities before scanning
(`A/MarketingQueue.cs:53-104`). On success extension Started hides form/picker,
restores shortcuts and calls CloseMenu (`extension:146-152`). Root draft reset
therefore does not erase the committed queue copy. Start errors are logged and
shown without closing the form.

## Findings and Evidence

### F-001 — Draft ownership follows the form root and is separate from committed execution

Statement: Navigation preserves a screen-owned draft; root closure clears it, and successful start transfers copied selected products/configuration into room-local execution.
Basis: static-observation
Standing: current
Applies to: marketing-automation-implementation
Evidence: `U/AutomationScreen.cs:115-186,212-261`; `U/ProductListView.cs:72-100`; `U/AutomationCampaignPage.cs:150-168`; `A/MarketingQueue.cs:53-104`.
Explanation: No unconfirmed draft is stored on the room or reused as a preset. Queue adoption/restoration is a separate persistence path; UI root reset must not be mistaken for Stop on an already committed queue.

### F-002 — Selection keys are independent from pooled visible rows

Statement: Selection is keyed by product kind, nativeID and runtime instanceID, while filtering/sorting/pooling change only presentation.
Basis: static-observation
Standing: current
Applies to: marketing-automation-implementation
Evidence: `U/ProductSelection.cs:3-81`; `G/MarketingProduct.cs:46-87`; `U/ProductListView.cs:119-156,222-249,274-313`; `U/ProductRow.cs:74-93`.
Explanation: Identical integer IDs across game/console types or replacement runtime objects do not share draft membership. Runtime instanceID is deliberately a session draft discriminator, not a save identity. Select All includes hidden selections because it operates on full available keys.

### F-003 — Display order and queue priority share labels but use separate state and rules

Statement: Display sort starts Name and matches native descending hype/date key conventions; queue priority defaults Hype/Ascending and is mapped independently from serialized enum values.
Basis: static-observation
Standing: current
Applies to: marketing-automation-implementation
Evidence: `U/ProductListSorting.cs:33-68`; `G/MarketingProduct.cs:18-21,35-44`; `U/ProductListView.cs:72-81,222-249`; `A/CampaignConfiguration.cs:3-54`.
Explanation: `QueuePriority.ReleaseDate=0`, `Hype=1`, `Name=2`; UI order Name/Hype/Release Date uses explicit ProductSortOptions mapping. Display date keys reproduce culture-sensitive native text construction and 999999 sentinel. Ties use product kind/nativeID/runtime identity, since recycled row IDs cannot represent products. Queue dates use separate numeric ReleaseMonth data. Display choice is transient form state and does not change dispatch priority.

### F-004 — Visual copying excludes original business callbacks and recreates control bindings

Statement: The adapters whitelist visual components, then create fresh Button/InputField/Slider/Dropdown and internal template controls rather than cloning native menu behavior.
Basis: static-observation
Standing: current
Applies to: marketing-automation-implementation
Evidence: `U/Controls/NativeUi.cs:10-100`; `U/Controls/NativeDropdown.cs:9-72`; `U/Controls/UiFactory.cs:33-110`; `U/ProductListView.cs:158-220`; `U/AutomationCampaignPage.cs:120-148`.
Explanation: CopyTree copies RectTransforms, Images/Text/effects, masks, language font component and native animation clip; it does not copy setText, row scripts or persistent game listeners. NativeDropdown maps references within each fresh tree, including template Toggles, scrollbars, ScrollRects, captions and items. This is inspected implementation intent and structure, not proof of every runtime Unity event outcome.

### F-005 — Layer input and error-dialog ordering have explicit temporary ownership

Statement: The extension suspends underlying picker shortcuts while open, restores after Back in LateUpdate, and the form separately owns its native validation dialog's ordering and Escape suppression.
Basis: static-observation
Standing: current
Applies to: marketing-automation-implementation
Evidence: `U/MarketingMenuExtension.cs:103-173`; `U/AutomationScreen.cs:139-210`; `U/Controls/SearchInputFocus.cs:14-51`.
Explanation: These scopes address competing native tooltip shortcuts, native dialog layering and input focus. Cleanup restores layout/listeners and destroys mod UI. Scene destruction avoids a modal callback into a partly destroyed native GUI. Same-frame keyboard/focus correctness remains a runtime validation boundary.

## Connections to Other Systems

The entry depends on the [bootstrap](bootstrap-and-deployment.md) patch and
supported native menu signature. Startup/entry ownership was rechecked on 2026-10-26;
other implementation citations retain their stated 2026-10-22 scope. Native constraints are documented in
[UI lifecycle](../../tools/mgt2-mod-toolkit/research/systems/ui-modal-and-control-lifecycle.md),
[catalog/ordering](../../tools/mgt2-mod-toolkit/research/systems/marketing-product-catalog-and-ordering.md) and
[campaign configuration](../../tools/mgt2-mod-toolkit/research/systems/marketing-campaign-configuration.md).
The UI's successful handoff enters [queue/room integration](queue-and-room-integration.md).
Only committed active queues enter [persistence](persistence-and-recovery.md);
transient search/display-sort/page selection and canceled drafts are not checkpoint
settings. No UI-side manipulation of native saves occurs in these classes.

## Mod Integration Implications

Preserve the common-root versus child-page boundary when refactoring. A new
lifecycle callback on ProductListView or a page could accidentally erase selection
or search during navigation. Keep explicit display-order mapping independent from
persisted enum numeric values. Reuse the copy/adaptation helpers for real visual
responsibilities rather than introducing a second menu framework.

Changes to native scene bindings need revalidation against the current source
set and installed hierarchy. Readability refactors should preserve entry placement,
frame, callbacks, draft lifetime and queue-copy boundary. An unsupported control
binding is logged and partial UI is cleaned up while retaining native choices
(`MarketingMenuExtension.Bind/Cleanup`). This is a safeguard path, not evidence
that every unsupported-version case has been runtime exercised.

## Unknowns and Validation

The user accepted the UI/automation baseline, including selection, navigation and campaign controls,
as documented with the 2026-10-22 inspection; per-scenario observation dates were not recorded. The subsequent readability and donor-copy edits lack
recorded in-game coverage. Source inspection and isolated model checks do not validate
Unity rendering, input focus, callbacks or scene lifetime.

Maintained isolated checks live in [Program.cs](../../tests/MarketingAutomation.Checks/Program.cs)
and [ProductListSortingChecks.cs](../../tests/MarketingAutomation.Checks/ProductListSortingChecks.cs).
Consult them for pure selection, form, campaign and sort cases rather than inferring runtime
coverage from their assertions.
- **Q-001:** Current UI startup, X/Escape/dialog focus, page preservation, scroll reset and UI scaling need an in-game smoke check.
- **Q-002:** Large-catalog rendering/scrolling performance is unmeasured; pure selection checks do not measure Unity.
- **Q-003:** Open-form catalog membership and displayed values have the refresh boundaries described above.
- **Q-004:** Unsupported bindings and scene teardown have static safeguards but no exhaustive runtime coverage.

### Runtime regression checks

Use disposable single-player sessions when testing affected UI behavior:

- Compare the Automation entry, modal and controls with native Marketing at supported UI scales;
  verify original Game/Game Console flows remain usable.
- Navigate both pages with search-hidden selections, scroll position and campaign settings.
  Close/reopen from either page; test X, Escape, configured right-click, and search focus release.
- Trigger each missing-setting dialog from both pages. Check layering, single-press Escape ownership,
  retained draft and restored game hotkeys after closing.
- Exercise locked campaigns and a cap reduction; verify slider bounds, thumb and labels agree.
  Confirm a mixed selection and check that the queue survives form closure and charges once.
- Return to the main menu/load another disposable session; check fresh room binding and no orphaned panels.

These are proposed native integration checks, not a list of individually observed passes.
