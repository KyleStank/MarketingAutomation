namespace MarketingAutomation.Automation;

// A fresh snapshot of one selected product, evaluated before any money is spent.
internal readonly struct QueueCandidate
{
    internal QueueCandidate(ProductKey key, float hype, int? releaseMonth, long price, bool eligible, string name = "")
    {
        Key = key;
        Hype = hype;
        ReleaseMonth = releaseMonth;
        Price = price;
        Eligible = eligible;
        Name = name ?? "";
    }

    internal ProductKey Key
    {
        get;
    }

    internal float Hype
    {
        get;
    }

    internal int? ReleaseMonth
    {
        get;
    }

    internal long Price
    {
        get;
    }

    internal bool Eligible
    {
        get;
    }

    internal string Name
    {
        get;
    }
}

