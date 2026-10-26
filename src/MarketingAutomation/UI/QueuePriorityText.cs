using MarketingAutomation.Automation;

namespace MarketingAutomation.UI;

internal static class QueuePriorityText
{
    internal static string Describe(QueuePriority priority, QueueDirection direction)
    {
        var order = DescribeOrder(priority, direction);

        return order + " Ineligible or unaffordable products are skipped.\n" +
               "A product being marketed keeps priority until its campaign cap unless blocked.";
    }

    private static string DescribeOrder(QueuePriority priority, QueueDirection direction)
    {
        var ascending = direction == QueueDirection.Ascending;

        switch (priority)
        {
            case QueuePriority.Name:
                return ascending ? "Names A to Z." : "Names Z to A.";
            case QueuePriority.Hype:
                return ascending ? "Lower hype first (10 before 85)." : "Higher hype first (85 before 10).";
            default:
                return ascending ? "Older releases first." : "Newer releases first.";
        }
    }

}
