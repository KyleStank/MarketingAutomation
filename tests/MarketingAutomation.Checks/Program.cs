using System;
using System.Collections.Generic;
using System.Linq;
using MarketingAutomation;

QueueSchedulerChecks.Run();
ProductListSortingChecks.Run();
QueuePersistenceChecks.Run();
NativeCampaignProgressChecks.Run();

var game = new ProductKey(ProductKind.Game, 42, 1001);
var console = new ProductKey(ProductKind.Console, 42, 2001);
var replacement = new ProductKey(ProductKind.Game, 42, 3001);
var firstRoom = new ProductSelection();
var secondRoom = new ProductSelection();

Check(!firstRoom.Contains(game), "Products start unselected.");
firstRoom.Toggle(game);
firstRoom.Toggle(console);
Check(firstRoom.Contains(game) && firstRoom.Contains(console), "Games/consoles sharing a numeric ID can both be selected.");
Check(!firstRoom.Contains(replacement), "A replacement native instance cannot inherit an old draft selection.");
Check(!secondRoom.Contains(game) && !secondRoom.Contains(console), "Room drafts remain independent.");

var catalog = new List<(ProductKey Key, string Name)> { (game, "Space Adventure"), (console, "Space Station Console") };
var filtered = catalog.Where(item => ProductSelection.MatchesSearch(item.Name, "  CONSOLE  ")).ToList();
Check(filtered.Count == 1 && filtered[0].Key.Equals(console), "Search ignores case and surrounding spaces.");
firstRoom.Toggle(filtered[0].Key);
Check(firstRoom.Contains(game) && !firstRoom.Contains(console), "Toggling a filtered row leaves hidden selections unchanged.");
Check(catalog.Where(item => ProductSelection.MatchesSearch(item.Name, "")).Any(item => firstRoom.Contains(item.Key)),
    "Clearing search reveals the retained hidden selection.");
Check(!ProductSelection.MatchesSearch("Space Adventure", "missing"), "A missing search term yields no match.");
Check(ProductSelection.MatchesSearch("Café Tycoon", "CAFÉ"), "Search supports case-insensitive non-ASCII names.");
Check(ProductSelection.MatchesSearch(null, " "), "Blank search includes unnamed products.");

// Exercise a late-game catalog through repeated searches without rebuilding its selections.
for (var index = 0; index < 10000; index++)
{
    var key = new ProductKey(ProductKind.Game, index, index + 4000);
    catalog.Add((key, $"Catalog product {index}"));
    if (index % 2 == 0)
    {
        firstRoom.Toggle(key);
    }
}
foreach (var query in new[] { "9999", "no matches", "station", "product", "" })
{
    _ = catalog.Count(item => ProductSelection.MatchesSearch(item.Name, query));
    Check(catalog.Count(item => firstRoom.Contains(item.Key)) == 5001, "Search preserves all selected products in a large catalog.");
}
firstRoom.Toggle(game);
Check(!firstRoom.Contains(game), "Clicking a selected product unchecks it.");
var allProducts = new[] { game, console, replacement };
var bulk = new ProductSelection();
Check(bulk.CountSelected(allProducts) == 0, "The draft starts empty.");
bulk.ToggleAll(allProducts);
Check(bulk.CountSelected(allProducts) == 3, "Select All selects everything when nothing is selected.");
bulk.ToggleAll(allProducts);
Check(bulk.CountSelected(allProducts) == 0, "Select All clears everything when all products are selected.");
bulk.Toggle(game);
bulk.Toggle(console);
bulk.ToggleAll(allProducts);
Check(bulk.CountSelected(allProducts) == 3, "With two selected products, Select All adds the remaining product.");
bulk.Toggle(console);
var matchingProducts = new[] { console };
Check(bulk.CountSelected(matchingProducts) == 0 && bulk.CountSelected(allProducts) == 2,
    "Selected products hidden by search still count toward validation.");
bulk.ToggleAll(allProducts);
Check(bulk.CountSelected(allProducts) == 3, "Select All uses the full catalog even when the search only shows an unselected console.");
bulk.ToggleAll(allProducts);
Check(bulk.CountSelected(allProducts) == 0, "Clearing all selections restores missing-products validation.");
bulk.ToggleAll(Array.Empty<ProductKey>());
Check(bulk.CountSelected(Array.Empty<ProductKey>()) == 0, "An empty catalog cannot satisfy product validation.");
Check(!secondRoom.Contains(game), "Bulk selection does not change another room's draft.");
bulk.ToggleAll(allProducts);
bulk.Clear();
Check(bulk.CountSelected(allProducts) == 0 && !bulk.Contains(game) && !bulk.Contains(console),
    "Closing a configuration clears both visible and search-hidden selections.");
bulk.Clear();
Check(bulk.CountSelected(allProducts) == 0, "Repeated closing/reset calls leave the draft empty.");
firstRoom.Clear();
Check(catalog.All(item => !firstRoom.Contains(item.Key)), "Closing clears an entire large-catalog draft.");
var form = new AutomationFormState();
Check(!form.CanGoBack && form.CanGoForward, "Page arrows reflect the initial products-page boundary.");
form.OpenPage(AutomationPage.Campaign);
Check(form.Page == AutomationPage.Campaign && form.CanGoBack && !form.CanGoForward,
    "The campaign page is reachable with no selected products.");
Check(AutomationFormState.Validate(0, false) == AutomationValidation.MissingProducts,
    "Confirmation reports missing products first when both settings are missing.");
Check(AutomationFormState.Validate(0, true) == AutomationValidation.MissingProducts,
    "Choosing a campaign first still requires products at confirmation.");
Check(AutomationFormState.Validate(2, false) == AutomationValidation.MissingCampaign,
    "Confirmation reports a missing campaign after selecting products.");
Check(AutomationFormState.Validate(2, true) == AutomationValidation.None,
    "A complete future configuration passes validation independent of page order.");
bulk.Toggle(game);
form.OpenPage(AutomationPage.Products);
form.OpenPage(AutomationPage.Campaign);
Check(bulk.Contains(game), "Page navigation does not clear the draft.");
form.Reset();
Check(form.Page == AutomationPage.Products, "Reopening/reset returns to the first page.");
var campaign = new CampaignConfiguration();
campaign.SetThreshold(10);
Check(!campaign.HasSelection && campaign.Threshold == null, "Threshold is unavailable until a campaign is selected.");
campaign.Select(5, 100, unlocked: false);
Check(!campaign.HasSelection, "A locked campaign cannot enter the draft.");
campaign.Select(5, 100, unlocked: true);
Check(campaign.Threshold == 100, "First campaign selection defaults the threshold to its cap.");
campaign.SetThreshold(69);
campaign.Select(3, 60, unlocked: true);
Check(campaign.Threshold == 60 && campaign.HypeCap == 60, "Changing to a lower cap clamps the existing threshold immediately.");
campaign.Select(5, 100, unlocked: true);
Check(campaign.Threshold == 60, "Changing to a higher cap preserves the lower chosen threshold.");
campaign.SetThreshold(-1);
Check(campaign.Threshold == 0, "The threshold supports the confirmed zero-hype minimum.");
campaign.SetThreshold(101);
Check(campaign.Threshold == 100, "A threshold cannot exceed the selected campaign's cap.");
campaign.Select(0, 15, unlocked: true);
campaign.SetThreshold(9);
form.OpenPage(AutomationPage.Products);
form.OpenPage(AutomationPage.Campaign);
Check(campaign.SelectedCampaign == 0 && campaign.Threshold == 9, "Page navigation preserves campaign and threshold choices.");
Check(AutomationFormState.Validate(1, campaign.HasSelection) == AutomationValidation.None,
    "Selecting a product and unlocked campaign enables the success-preview path.");
campaign.Reset();
Check(!campaign.HasSelection && campaign.Threshold == null, "Closing/reopening discards the campaign draft as well.");
campaign.Select(0, 15, unlocked: true);
Check(campaign.Threshold == 15, "A new form defaults its first campaign threshold to the cap again.");
Check(campaign.Priority == QueuePriority.Hype && campaign.Direction == QueueDirection.Ascending,
    "A new form defaults to Hype / Ascending.");
Check(campaign.PriorityExplanation.Contains("10 before 85"), "Hype ascending explains lower hype first.");
campaign.SetPriority(QueuePriority.Hype, QueueDirection.Descending);
Check(campaign.PriorityExplanation.Contains("85 before 10"), "Hype descending explains higher hype first.");
campaign.SetPriority(QueuePriority.Name, QueueDirection.Ascending);
Check(campaign.PriorityExplanation.Contains("A to Z") && campaign.PrioritySummary == "Name / Ascending",
    "Name ascending explains alphabetical queue priority.");
campaign.SetPriority(QueuePriority.Name, QueueDirection.Descending);
Check(campaign.PriorityExplanation.Contains("Z to A"), "Name descending explains reverse alphabetical queue priority.");
campaign.SetPriority(QueuePriority.ReleaseDate, QueueDirection.Ascending);
Check(campaign.PriorityExplanation.Contains("Older releases first"), "Release-date ascending explains older releases first.");
campaign.SetPriority(QueuePriority.ReleaseDate, QueueDirection.Descending);
Check(campaign.PriorityExplanation.Contains("Newer releases first"), "Release-date descending explains newer releases first.");
Check(campaign.PriorityExplanation.Contains("Ineligible or unaffordable products are skipped") &&
    campaign.PriorityExplanation.Contains("unless blocked"), "Priority explanation preserves blocker and current-product rules.");
campaign.Select(5, 100, unlocked: true);
form.OpenPage(AutomationPage.Products);
form.OpenPage(AutomationPage.Campaign);
Check(campaign.Priority == QueuePriority.ReleaseDate && campaign.Direction == QueueDirection.Descending,
    "Campaign selection and page navigation preserve queue-priority choices.");
Check(campaign.PrioritySummary == "Release Date / Descending", "The success preview summarizes the current combo.");
campaign.Reset();
Check(campaign.Priority == QueuePriority.Hype && campaign.Direction == QueueDirection.Ascending,
    "Closing/reopening resets the queue-priority default.");
Console.WriteLine("PASS: search, selection, 10,000 products, Select All, navigation, validation, campaign/threshold configuration, and all queue-priority combinations.");

static void Check(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}
