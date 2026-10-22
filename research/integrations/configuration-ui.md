---
{
  "schema_version": 1,
  "id": "configuration-ui",
  "kind": "integration",
  "title": "Marketing Automation configuration UI integration",
  "scope": "mod-specific",
  "summary": "Actual working-tree Automation entry, native visual adapters, two-page draft lifecycle, mixed product selection, independent display sorting, campaign/priority/threshold controls and queue confirmation boundary.",
  "project": "MarketingAutomation",
  "code_revision": "Working source inspected on 2026-10-22; includes uncommitted changes; citations identify the relevant files and symbols",
  "domains": ["marketing", "ui", "mod-integration"],
  "keywords": ["Automation entry", "configuration draft", "Select All", "pooled rows", "search", "multipage", "threshold", "queue priority", "display sorting", "Escape", "native control adapters", "validation dialog"],
  "symbols": ["MarketingAutomationPlugin.AfterMarketingMenuInitialized", "MarketingMenuExtension", "AutomationScreen", "AutomationFormState", "AutomationCampaignPage", "ProductListView", "ProductRow", "ProductSelection", "ProductKey", "ProductListSorting", "NativeDropdown", "NativeUi", "SearchInputFocus", "CampaignConfiguration", "ProductSortOptions", "NativeCampaignCatalog", "MarketingQueue.TryStart"],
  "source_sets": ["marketing-automation-implementation", "mgt2-managed-2026-10-22", "mgt2-unity-export-2026-10-22"],
  "related": [
    {"id": "ui-modal-and-control-lifecycle", "relationship": "Primary native visual, modal, focus, animation and callback constraints."},
    {"id": "marketing-product-catalog-and-ordering", "relationship": "Native membership and display-sort rules copied by the adapter."},
    {"id": "marketing-campaign-configuration", "relationship": "Native campaign data/unlocks and original flows preserved by this separate form."},
    {"id": "queue-and-room-integration", "relationship": "Owns committed execution after UI validates and hands over copied configuration."},
    {"id": "persistence-and-recovery", "relationship": "Owns active-queue checkpoints; this form's drafts are transient."},
    {"id": "bootstrap-and-deployment", "relationship": "Owns plugin startup/compatibility checks and installed build identity."}
  ],
  "created": "2026-10-22",
  "updated": "2026-10-22",
  "entry_points": [
    {"label": "Implementation ownership map", "anchor": "architecture-and-state-ownership", "summary": "Form, product view, rows, selection model and visual adapters have distinct responsibilities."},
    {"label": "Open navigate close confirm", "anchor": "behavior-and-call-flows", "summary": "Picker layering, root closure reset, freely navigable child pages and native error dialogs."},
    {"label": "Display sort versus queue priority", "anchor": "f-003--display-order-and-queue-priority-share-labels-but-use-separate-state-and-rules", "summary": "Native descending list keys versus configurable dispatch directions and persisted enum mapping."},
    {"label": "Actual validation boundary", "anchor": "unknowns-and-validation", "summary": "User acceptance predates current readability refactor; current evidence is static only."}
  ]
}
---

# Marketing Automation configuration UI integration

## Overview and Coverage

This report describes the actual MarketingAutomation UI working tree inspected on
2026-10-22. It separates mod draft behavior from native mechanics and from earlier
design alternatives. The product has a new Automation entry under Marketing
Campaign, a mixed multiselect catalog, two-page configuration and a queue-start
handoff. Original Game/Game Console campaigns remain separate native flows.

The authoritative product decisions are in [REQUIREMENTS.md](../../REQUIREMENTS.md:183),
especially its UI section. The report documents implementation and validation,
not a replacement specification. Queue execution, Stop/Waiting/Support, native
save integration and build/deployment have separate owners and are not duplicated
here. There is no custom finite-pass checkbox mode, room-owned unconfirmed preset,
Manage screen or success-preview transaction in this inspected UI.

## Source References and Applicability

Source set: `marketing-automation-implementation`, with native dependencies
`mgt2-managed-2026-10-22` and `mgt2-unity-export-2026-10-22`.
The implementation was inspected on 2026-10-22, including uncommitted changes.
The cited classes, methods and paths identify the inspected UI behavior. No
working-tree inventory or UI-source fingerprint is required.

`U/` below denotes `src/MarketingAutomation/UI/`. `G/` denotes
`src/MarketingAutomation/GameIntegration/`. `A/` denotes
`src/MarketingAutomation/Automation/`. Cited paths and line ranges refer to this
source inspection. Native donor identities are owned by the linked system
reports; exported GUIDs are not runtime identity guarantees.

## Architecture and State Ownership

| Owner | Responsibility and source |
| --- | --- |
| `MarketingAutomationPlugin` | Validates native assembly MVID/signature, patches picker Init, creates/binds extension and removes it on destruction; `src/MarketingAutomation/MarketingAutomationPlugin.cs:16-72` |
| `MarketingMenuExtension` | One component per native picker; binds room, adds entry, retains picker underneath child form, suspends/restores shortcuts, restores native layout on cleanup; `U/MarketingMenuExtension.cs:9-254` |
| `AutomationScreen` | Shared frame, persistent arrows/dots/confirm, room binding, child-page switching, validation-dialog ownership, queue handoff and root-close draft reset; `U/AutomationScreen.cs:8-261` |
| `AutomationFormState` | Pure page state and missing-products-before-missing-campaign validation; `U/AutomationFormState.cs:3-33` |
| `ProductListView` | Catalog snapshot, search/sort, selection model, full-catalog Select All, scrollbar and bounded row pool; regular C# class forwarded by the form, `U/ProductListView.cs:9-314` |
| `ProductRow` | Render/bind one current product at an offset, fresh click action, type icon, checkbox, native labels/tooltip; `U/ProductRow.cs:8-115` |
| `ProductSelection` / `ProductKey` | HashSet membership independent of visible rows; identity includes kind/nativeID/runtime instanceID; `U/ProductSelection.cs:3-82` |
| `AutomationCampaignPage` | Six cards, live native data/unlocks, slider, draft priority and explanation; `U/AutomationCampaignPage.cs:10-257` |
| `CampaignConfiguration` | Selected campaign, nullable first threshold, clamp/reset, priority/direction and explanation; `A/CampaignConfiguration.cs:22-89` |
| Visual adapters | `NativeUi`, `UiFactory`, `NativeDropdown` build fresh visuals/controls; `SearchInputFocus` owns focus handoff without native search callbacks |

Actual code uses namespaces `MarketingAutomation.UI`, `.UI.Controls`,
`.GameIntegration`, `.Automation` and root `.MarketingAutomationPlugin`; it is
not documented as an earlier flat-namespace layout.

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

The view is456 units tall with64-unit row stride; pool size is
`ceil(viewportHeight/64)+2` (`:58-62,189-220`). Rows are56 units high and positions
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
excluding its children/grid; creates six270x88 cards with separate game/console
prices, gain/cap and native lock overlays; and uses selection color colors[7]
(`campaign page:35-92,178-189,239-255`). `NativeCampaignCatalog.Read` validates
six-array lengths, nonnegative prices, positive work/hype data, shared gain/cap
equality, and unlocks56/57 (`G/NativeCampaignCatalog.cs:16-58`). It does not call
native campaign Init to obtain those values.

The threshold Slider is fresh with minimum0, integer steps and disabled until a
campaign exists (`campaign page:120-148`). First selection defaults threshold to
cap; lowering cap clamps it, raising cap retains a lower prior threshold;
SetThreshold clamps0..cap (`CampaignConfiguration:56-89`). Refresh updates
max/value, range, thumb and explanation together. The UI describes inclusive
admission and continuing toward cap; execution enforcement belongs to the queue.

Priority controls use Name/Hype/Release Date plus Ascending/Descending; a new
draft defaults Hype/Ascending. Explanations update on either control and say
blocked products are skipped while the admitted product keeps priority until cap
unless blocked. These are mod policy statements tied to queue behavior, not native
checkbox semantics. The three original native repeat/stop/wait checkboxes are
absent from this custom screen.

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
shown without closing the form; this is not a success-preview message box.

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
Explanation: `QueuePriority.ReleaseDate=0`, `Hype=1`, `Name=2`; UI order Name/Hype/Release Date uses explicit ProductSortOptions mapping. Display date keys reproduce culture-sensitive native text construction and999999 sentinel. Ties use product kind/nativeID/runtime identity, since recycled row IDs cannot represent products. Queue dates use separate numeric ReleaseMonth data. Display choice is transient form state and does not change dispatch priority.

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
supported native menu signature. Native constraints are documented in
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

The project [README validation record](../../README.md:381) attributes earlier
UI/automation acceptance to user gameplay, including product list, Select All,
Development navigation, campaign/threshold/priority controls and group appearance.
It also states the first readability refactor passed build/model/file checks but
the refactored UI still needs an in-game smoke test (`README.md:391-395`). Those
historical checks were not rerun for this report, and earlier acceptance is not
promoted into new runtime validation of this working tree.

- **Q-001:** Confirm current refactored UI startup, X/Escape/dialog focus, page
  preservation, scroll reset and supported UI-scale behavior in-game.
- **Q-002:** Profile rendering/scrolling at large catalogs. Existing pure checks
  for10000 selection records do not establish Unity performance.
- **Q-003:** Catalog mutation/display refresh while open has the bounded behavior
  described above. No additional live-refresh feature is inferred.
- **Q-004:** Unsupported bindings, scene teardown and cleanup failure paths have
  static safeguards but no exhaustive native runtime coverage documented here.

Validation as of 2026-10-22: source/requirements/documentation inspection and
cross-checking the cited native donor reports. Checks, build, deployment and
game/editor workflows were not rerun; saves were not inspected.

## Revision Notes

- 2026-10-22: Created from the actual refactored working-tree classes, replacing
  historical prototype descriptions with current source evidence. Distinguished
  prior user acceptance from outstanding
  post-refactor gameplay validation. No new UI behavior or runtime test was added.
