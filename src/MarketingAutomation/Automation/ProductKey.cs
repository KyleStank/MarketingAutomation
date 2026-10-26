namespace MarketingAutomation.Automation;

internal enum ProductKind
{
    Game,
    Console
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

    public bool Equals(ProductKey other) =>
        Kind == other.Kind && NativeId == other.NativeId && InstanceId == other.InstanceId;

    public override bool Equals(object other) => other is ProductKey key && Equals(key);
    public override int GetHashCode() => unchecked(((int)Kind * 397 ^ NativeId) * 397 ^ InstanceId);
}

