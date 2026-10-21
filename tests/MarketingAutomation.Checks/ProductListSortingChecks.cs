using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using MarketingAutomation;

internal static class ProductListSortingChecks
{
    internal static void Run()
    {
        Require(ProductSortOptions.Labels().SequenceEqual(new[] { "Name", "Hype", "Release Date" }), "Both dropdowns use native option order.");
        Require((int)QueuePriority.ReleaseDate == 0 && (int)QueuePriority.Hype == 1, "Existing persisted priority values remain compatible.");
        for (var index = 0; index < 3; index++)
        {
            Require(ProductSortOptions.IndexOf(ProductSortOptions.FromIndex(index)) == index, "Display options map independently of saved enum values.");
        }

        var originalCulture = CultureInfo.CurrentCulture;
        try
        {
            foreach (var culture in new[] { "en-US", "fr-FR" })
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
                var alpha = new ProductSortValue(new ProductKey(ProductKind.Console, 10, 100), "Alpha", 10.4f,
                    ProductListSorting.ReleaseKey(2020, 1, false));
                var beta = new ProductSortValue(new ProductKey(ProductKind.Game, 20, 200), "Beta", 10.49f,
                    ProductListSorting.ReleaseKey(2020, 12, false));
                var zebra = new ProductSortValue(new ProductKey(ProductKind.Game, 30, 300), "Zebra", 90,
                    ProductListSorting.ReleaseKey(2021, 1, false));
                var unreleased = new ProductSortValue(new ProductKey(ProductKind.Console, 40, 400), "Unreleased", 0,
                    ProductListSorting.ReleaseKey(0, 0, true));
                var values = new List<ProductSortValue> { zebra, beta, unreleased, alpha };
                values.Sort((left, right) => ProductListSorting.Compare(left, right, QueuePriority.Name));
                Require(values.Select(value => value.Name).SequenceEqual(new[] { "Alpha", "Beta", "Unreleased", "Zebra" }),
                    "Native Name sort is alphabetical across games and consoles.");
                values.Sort((left, right) => ProductListSorting.Compare(left, right, QueuePriority.Hype));
                Require(values.Select(value => value.Name).SequenceEqual(new[] { "Zebra", "Beta", "Alpha", "Unreleased" }),
                    "Native Hype sort is descending and uses raw, unrounded hype.");
                values.Sort((left, right) => ProductListSorting.Compare(left, right, QueuePriority.ReleaseDate));
                Require(values.Select(value => value.Name).SequenceEqual(new[] { "Unreleased", "Zebra", "Beta", "Alpha" }),
                    "Native date sort places undated products first, then newer years/months.");
                var selection = new ProductSelection();
                selection.Toggle(alpha.Key);
                selection.Toggle(zebra.Key);
                foreach (var field in new[] { QueuePriority.Name, QueuePriority.Hype, QueuePriority.ReleaseDate })
                {
                    values.Sort((left, right) => ProductListSorting.Compare(left, right, field));
                    var filtered = values.Where(value => ProductSelection.MatchesSearch(value.Name, "Beta")).ToArray();
                    Require(filtered.Length == 1 && selection.CountSelected(values.Select(value => value.Key)) == 2 &&
                        selection.Contains(alpha.Key) && selection.Contains(zebra.Key), "Sorting and filtering preserve hidden multi-selections.");
                }
            }
        }
        finally { CultureInfo.CurrentCulture = originalCulture; }
        Console.WriteLine("Native product-list sorting checks passed.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
