namespace MarketingAutomation.UI;

internal enum ProductKind
{
    Game, Console
}

// Runtime instance identity prevents a replaced product from inheriting a draft
// selection merely because its native numeric ID was reused. This is not a save key.
internal readonly struct ProductKey : IEquatable<ProductKey>
{
    internal ProductKey(ProductKind kind, int nativeId, int instanceId)
    {
        Kind = kind;
        NativeId = nativeId;
        InstanceId = instanceId;
    }

    internal ProductKind Kind
    {
        get;
    }
    internal int NativeId
    {
        get;
    }
    internal int InstanceId
    {
        get;
    }
    public bool Equals(ProductKey other) => Kind == other.Kind && NativeId == other.NativeId && InstanceId == other.InstanceId;
    public override bool Equals(object other) => other is ProductKey key && Equals(key);
    public override int GetHashCode() => unchecked(((int)Kind * 397 ^ NativeId) * 397 ^ InstanceId);
}

internal sealed class ProductSelection
{
    private readonly HashSet<ProductKey> selected = new();
    internal void Clear() => selected.Clear();
    internal bool Contains(ProductKey key) => selected.Contains(key);
    internal void Toggle(ProductKey key)
    {
        if (!selected.Add(key))
        {
            selected.Remove(key);
        }
    }

    internal int CountSelected(IEnumerable<ProductKey> products)
    {
        var count = 0;
        foreach (var product in products)
        {
            if (selected.Contains(product))
            {
                count++;
            }
        }
        return count;
    }

    internal void ToggleAll(IEnumerable<ProductKey> products)
    {
        var candidates = new HashSet<ProductKey>(products);
        if (candidates.Count == 0)
        {
            return;
        }

        if (CountSelected(candidates) == candidates.Count)
        {
            selected.Clear();
        }
        else
        {
            selected.UnionWith(candidates);
        }
    }

    internal static bool MatchesSearch(string name, string query) =>
        string.IsNullOrWhiteSpace(query) || (name ?? "").IndexOf(query.Trim(), StringComparison.OrdinalIgnoreCase) >= 0;
}
