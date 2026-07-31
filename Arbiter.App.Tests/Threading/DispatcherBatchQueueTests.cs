using Arbiter.App.Threading;

namespace Arbiter.App.Tests.Threading;

public sealed class DispatcherBatchQueueTests
{
    [Test]
    public void Should_Drop_Oldest_Pending_Items_When_Queue_Reaches_Its_Limit()
    {
        var appliedItems = new List<int>();
        var queue = new DispatcherBatchQueue<int>(items => appliedItems.AddRange(items),
            maxPendingItems: 3);

        var droppedItems = Enumerable.Range(1, 5).Sum(queue.Enqueue);
        queue.DrainAll();

        Assert.Multiple(() =>
        {
            Assert.That(droppedItems, Is.EqualTo(2));
            Assert.That(queue.DroppedItemCount, Is.EqualTo(2));
            Assert.That(queue.PendingCount, Is.Zero);
            Assert.That(appliedItems, Is.EqualTo(new[] { 3, 4, 5 }));
        });
    }

    [Test]
    public void Should_Keep_Pending_Items_Bounded_Under_Sustained_Producer_Load()
    {
        const int pendingLimit = 4096;
        const int producedItems = 100_000;
        var appliedItems = new List<int>();
        var queue = new DispatcherBatchQueue<int>(items => appliedItems.AddRange(items),
            maxPendingItems: pendingLimit);

        for (var item = 1; item <= producedItems; item++)
        {
            queue.Enqueue(item);
        }

        Assert.That(queue.PendingCount, Is.EqualTo(pendingLimit));
        queue.DrainAll();

        Assert.Multiple(() =>
        {
            Assert.That(queue.DroppedItemCount, Is.EqualTo(producedItems - pendingLimit));
            Assert.That(appliedItems, Has.Count.EqualTo(pendingLimit));
            Assert.That(appliedItems[0], Is.EqualTo(producedItems - pendingLimit + 1));
            Assert.That(appliedItems[^1], Is.EqualTo(producedItems));
        });
    }
}
