using MarketingAutomation.Automation;

namespace MarketingAutomation.UI;

internal sealed class ProductSelection
{
    private readonly HashSet<ProductKey> _selected = [];

    internal void Clear() => _selected.Clear();

    internal bool Contains(ProductKey key) => _selected.Contains(key);

    internal void Toggle(ProductKey key)
    {
        if (!_selected.Add(key))
            _selected.Remove(key);
    }

    internal int CountSelected(IEnumerable<ProductKey> products)
    {
        var count = 0;
        foreach (var product in products)
        {
            if (_selected.Contains(product))
                count++;
        }

        return count;
    }

    internal void ToggleAll(IEnumerable<ProductKey> products)
    {
        var candidates = new HashSet<ProductKey>(products);
        if (candidates.Count == 0)
            return;

        if (CountSelected(candidates) == candidates.Count)
            _selected.Clear();
        else
            _selected.UnionWith(candidates);
    }

    internal static bool MatchesSearch(string name, string query) =>
        string.IsNullOrWhiteSpace(query) || (name ?? "").IndexOf(query.Trim(), StringComparison.OrdinalIgnoreCase) >= 0;
}
