using System.Runtime.Serialization;
using MarketingAutomation.UI;

namespace MarketingAutomation.Persistence;

[DataContract]
internal sealed class ProductIdentity : IEquatable<ProductIdentity>
{
    [DataMember(Order = 0)]
    internal int Kind;
    [DataMember(Order = 1)]
    internal int Id;

    internal static ProductIdentity From(ProductKey key) => new() { Kind = (int)key.Kind, Id = key.NativeId };
    internal bool Matches(ProductKey key) => Kind == (int)key.Kind && Id == key.NativeId;
    public bool Equals(ProductIdentity other) => other != null && Kind == other.Kind && Id == other.Id;
    public override bool Equals(object value) => value is ProductIdentity other && Equals(other);
    public override int GetHashCode() => unchecked(Kind * 397 ^ Id);
    internal bool IsValid => (Kind == (int)ProductKind.Game || Kind == (int)ProductKind.Console) && Id > 0;
}

[DataContract]
internal sealed class PaidCampaignSnapshot
{
    [DataMember(Order = 0)]
    internal int TaskId;
    [DataMember(Order = 1)]
    internal ProductIdentity Target;
    [DataMember(Order = 2)]
    internal float Points;
    [DataMember(Order = 3)]
    internal float PointsLeft;
}

[DataContract]
internal sealed class RoomQueueSnapshot
{
    [DataMember(Order = 0)]
    internal int RoomId;
    [DataMember(Order = 1)]
    internal int Campaign;
    [DataMember(Order = 2)]
    internal int Threshold;
    [DataMember(Order = 3)]
    internal int Priority;
    [DataMember(Order = 4)]
    internal int Direction;
    [DataMember(Order = 5)]
    internal ProductIdentity[] Products;
    [DataMember(Order = 6)]
    internal ProductIdentity Admitted;
    [DataMember(Order = 7)]
    internal PaidCampaignSnapshot Paid;
}

[DataContract]
internal sealed class QueueSaveSnapshot
{
    [DataMember(Order = 0)]
    internal int Schema = 1;
    [DataMember(Order = 1)]
    internal string GameVersion;
    [DataMember(Order = 2)]
    internal int NativeFormat;
    [DataMember(Order = 3)]
    internal int CompanyId;
    [DataMember(Order = 4)]
    internal RoomQueueSnapshot[] Rooms;
}
