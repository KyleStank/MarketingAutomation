using System;
using MarketingAutomation;

internal static class QueueSchedulerChecks
{
    internal static void Run()
    {
        var game = new ProductKey(ProductKind.Game, 10, 100);
        var console = new ProductKey(ProductKind.Console, 10, 200);
        var other = new ProductKey(ProductKind.Game, 20, 300);
        QueueCandidate Item(ProductKey key, float hype, long price = 100, bool eligible = true, int? date = null, string name = "") =>
            new(key, hype, date, price, eligible, name);
        QueueScheduler Queue(int threshold = 20, QueuePriority priority = QueuePriority.Hype,
            QueueDirection direction = QueueDirection.Ascending) => new(threshold, priority, direction);

        var queue = Queue();
        Require(queue.Choose(new[] { Item(game, 20), Item(console, 10) }, 100, 60), console,
            "Lowest hype is admitted first.");
        Require(queue.Choose(new[] { Item(game, 5), Item(console, 45) }, 100, 60), console,
            "Admitted work continues above its entry threshold without preemption.");
        Require(queue.Choose(new[] { Item(game, 5), Item(console, 60) }, 100, 60), game,
            "At cap, rotate to another eligible selected product.");
        Require(queue.Choose(new[] { Item(game, 60), Item(console, 59) }, 100, 60), null,
            "Above-threshold products remain waiting after admission ends.");
        Require(queue.Choose(new[] { Item(console, 20) }, 100, 60), console,
            "Inclusive threshold permits re-entry.");
        Require(queue.Choose(new[] { Item(game, 0), Item(console, 40, 101) }, 100, 60), game,
            "An unaffordable admitted product cannot block affordable work.");
        Require(queue.Choose(new[] { Item(game, 25, eligible: false), Item(console, 40) }, 100, 60), null,
            "Losing eligibility drops admission; the old threshold applies on future entry.");
        Require(Queue(60).Choose(new[] { Item(game, 60) }, 100, 60), null,
            "Threshold equal to cap never buys a zero-gain campaign.");
        Require(Queue().Choose(new[] { Item(game, 0, 101), Item(console, 10, 100) }, 100, 60), console,
            "Actual product-type costs are checked against cash, including equality.");
        Require(Queue().Choose(new[] { Item(game, 0) }, 99, 60), null,
            "Available credit is never used.");
        Require(Queue().Choose(new[] { Item(game, float.NaN), Item(console, float.PositiveInfinity) }, 100, 60), null,
            "Invalid hype cannot trigger a purchase.");
        Require(Queue().Choose(Array.Empty<QueueCandidate>(), 100, 60), null,
            "Missing products leave the queue Waiting.");
        Require(Queue(60, direction: QueueDirection.Descending).Choose(new[] { Item(game, 85), Item(console, 20) }, 100, 60), console,
            "Descending priority skips products above the cap.");
        Require(Queue().Choose(new[] { Item(other, 5), Item(console, 5), Item(game, 5) }, 100, 60), game,
            "Ties use typed product ID independently of input order.");

        foreach (var direction in new[] { QueueDirection.Ascending, QueueDirection.Descending })
        {
            var dated = Queue(priority: QueuePriority.ReleaseDate, direction: direction);
            var expected = direction == QueueDirection.Ascending ? game : other;
            Require(dated.Choose(new[] { Item(console, 1), Item(game, 5, date: 24001), Item(other, 10, date: 24002) }, 100, 60), expected,
                "Release dates sort numerically; missing dates stay last in both directions.");
        }

        queue = Queue();
        var named = new[] { Item(console, 5, name: "Zebra"), Item(game, 15, name: "Alpha"), Item(other, 10, name: "Beta") };
        var alphabetical = Queue(priority: QueuePriority.Name);
        Require(alphabetical.Choose(named, 100, 60), game, "Name ascending selects alphabetically before considering hype order.");
        Require(Queue(priority: QueuePriority.Name, direction: QueueDirection.Descending).Choose(named, 100, 60), console,
            "Name descending selects reverse alphabetical order.");
        Require(alphabetical.Choose(new[] { Item(game, 40, name: "Zebra"), Item(other, 5, name: "Alpha") }, 100, 60), game,
            "Name priority cannot preempt an admitted product before cap.");
        Require(Queue(priority: QueuePriority.Name).Choose(new[] { Item(game, 0, 101, name: "Alpha"), Item(console, 5, name: "Beta") }, 100, 60), console,
            "Alphabetical priority still skips unaffordable products.");
        Require(queue.Choose(new[] { Item(game, 20) }, 100, 60), game, "Session starts at threshold.");
        queue.ResetAdmission();
        Require(queue.Choose(new[] { Item(game, 30) }, 100, 60), null,
            "Restart after Stop requires fresh admission.");
        Console.WriteLine("Queue scheduling checks passed.");
    }

    private static void Require(ProductKey? actual, ProductKey? expected, string message)
    {
        if (!Nullable.Equals(actual, expected))
        {
            throw new InvalidOperationException(message);
        }
    }
}
