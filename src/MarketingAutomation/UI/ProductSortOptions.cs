using MarketingAutomation.Automation;

namespace MarketingAutomation.UI;

internal static class ProductSortOptions
{
    private static readonly QueuePriority[] DisplayOrder =
        { QueuePriority.Name, QueuePriority.Hype, QueuePriority.ReleaseDate };

    internal static List<string> Labels() => ["Name", "Hype", "Release Date"];

    internal static QueuePriority FromIndex(int index) => DisplayOrder[index];

    internal static int IndexOf(QueuePriority priority) => Array.IndexOf(DisplayOrder, priority);

}
