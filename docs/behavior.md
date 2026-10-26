# Intended behavior

This document owns the consequential product decisions for Marketing Automation.
Research describes implementation evidence and validation limits; it does not override
these decisions. Later explicit user decisions take precedence.

## Scope

Version 1 maintains explicitly selected games and consoles through ordinary paid
Marketing campaigns in single-player. Preserve native costs, staffing, progress,
duration, gains, unlocks, and product restrictions. Independent Marketing rooms remain
independent. Multiplayer, Special Marketing automation, and changes to Employee Search
or Market Research are outside this scope.

Keep **Automation** below Game and Game Console in the existing Marketing Campaign
picker. Preserve those original workflows and their native repeat/stop/wait checkboxes.
The separate Automation form has no such checkboxes, finite-pass mode, Manage screen,
or midway settings editing.

## Configuration

- Use one native-styled, two-page form: products first, campaign settings second.
  Keep Development-style arrows, clickable page dots, and the green confirmation
  control available on both pages. Either page is reachable before selecting products.
  Only initial opening animates; child closure returns immediately to the picker.
- Draft state belongs to the open form. Navigation preserves selections, search,
  scroll, display sort, campaign, threshold, and priority. Closing from either page
  discards it all; reopening starts empty on Products, sorted by Name, at the top.
- Match the original product catalogs. Search and sorting retain hidden selections.
  Select All acts on the whole catalog: none/partial selects all, all clears all;
  it is checked only for full selection and disabled for an empty catalog.
- Display sorting is separate from execution priority and is not saved with automation.
  Name is alphabetical; Hype is highest first; Release Date puts unreleased/shelved
  products first, then newer releases. Both sort-field controls list Name, Hype,
  Release Date in that order.
- Campaign settings show the six native choices with game/console prices, gains, caps,
  and unlocks. Queue priority defaults to **Hype / Ascending**, with an explanation
  that updates as its field/direction changes.
- The threshold is an integer from 0 through the selected campaign's cap. It is disabled
  until a campaign is chosen and first defaults to that cap. A lower cap clamps the
  value; a higher cap preserves an already chosen lower threshold.
- Confirmation checks missing products before missing campaign, shows the relevant
  page and a native error dialog, and retains the draft. Valid confirmation closes
  both form and picker and starts paid work or visible Waiting, without a preview.

See [UI integration evidence](../research/integrations/configuration-ui.md) and
[form/selection checks](../tests/MarketingAutomation.Tests/Program.cs).

## Scheduling

Only selected products belong to a queue. Above-threshold products remain selected;
newly available products are never added automatically. Skip removed, ineligible,
locked, or unaffordable products so they cannot block other work.

Admission requires **hype ≤ threshold**, **hype < campaign cap**, and enough actual
cash for that product type's campaign price. Every purchase, including the first and
all continuations, is cash-only; available credit does not count. Never buy a new
campaign at or above its cap, even when the threshold equals the cap.

An admitted product keeps priority until its campaign cap unless blocked. Its rising
hype may exceed the entry threshold without interrupting continuation. Re-sort when
choosing another product, not after each campaign. Blocking ends admission; any later
entry again requires hype at or below the threshold. If another room raises hype to
cap during paid work, let that campaign finish and recheck before another purchase.

Priority supports Name, Hype, or Release Date, ascending or descending. Release dates
use older/newer ordering; undated products go last in both directions. Ties use stable
typed product IDs. Priority never overrides eligibility or preempts paid work.

When nothing can run, keep automation active in visible **Waiting**, showing the
threshold and respecting native room-UI visibility. Waiting spends nothing and adds no
hype. Resume when selected work becomes eligible and affordable. This is ongoing
maintenance, not a finite pass.

See [queue integration evidence](../research/integrations/queue-and-room-integration.md)
and [scheduler checks](../tests/MarketingAutomation.Tests/QueueSchedulerTests.cs).

## Stop, Cancel, and Support

**Stop Automation** immediately prevents further purchases and clears all queue
configuration and admission. Already-paid work may finish once. Stop during Waiting
ends it without a purchase. Stopped settings are never a preset: starting again
requires fresh configuration.

**Cancel Task** immediately cancels using native refund behavior and stops the queue.
Room disappearance, task replacement, or transfer also stops the queue. Preserve
unrelated or reassigned native work; do not reclaim it. The
[transferred-waiter finding](../research/integrations/queue-and-room-integration.md#f-005---task-replacement-stops-the-queue-but-transferred-waiter-cleanup-needs-runtime-review)
records an implementation discrepancy requiring review.

Rooms schedule independently, without shared reservations or added coordination.
Native Support assists the leader's current campaign, idles while it waits, and resumes
with new work. This is integration with Support, not a separate Support redesign.

## Saving and restoring

Persist only confirmed active queues, including Waiting: selected products, campaign,
threshold, priority/direction, and execution state needed to resume. Do not save drafts
or stopped settings. Saving after Stop must not restart automation, even if paid work
remains. Older saved active states retain their matching history.

Keep mod state external to native saves. Preserve native save execution, format, and
failures, and restore live queue bindings even if saving fails. Do not change the
autosave scheduler, preferences, or countdown.

Associate records with exact native save content, never just a slot or the newest
settings. Preserve history for older saves and exact copies; do not automatically purge
it. Missing, conflicting, damaged, unsupported, or incompatible records must leave
automation off while normal game loading continues. Never guess or repair an association
by changing native progress.

Restore reliable active work without another campaign or charge. Preserve native paid
progress and admission above the entry threshold; clamp restored thresholds to campaign
caps. Keep compatibility with existing checkpoints, including native numeric rounding,
without weakening task/product/campaign identity checks. Loading preferences must not
act like a manual employee-toggle click.

See [persistence evidence and validation limits](../research/integrations/persistence-and-recovery.md)
and [checkpoint checks](../tests/MarketingAutomation.Tests/QueuePersistenceTests.cs).
