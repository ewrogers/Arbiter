using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Arbiter.App.Collections;

namespace Arbiter.App.Tests.Collections;

public sealed class FilteredObservableCollectionTests
{
    [Test]
    public void Should_Reconcile_Filter_Changes_Without_Resetting_Stable_Items()
    {
        var first = new TestItem(1);
        var second = new TestItem(2);
        var third = new TestItem(3);
        var source = new ObservableCollection<TestItem> { first, second, third };
        var visible = new HashSet<int> { 1, 2 };
        using var filtered = new FilteredObservableCollection<TestItem>(
            source,
            item => visible.Contains(item.Id));
        var actions = new List<NotifyCollectionChangedAction>();
        filtered.CollectionChanged += (_, args) => actions.Add(args.Action);

        visible.Remove(2);
        visible.Add(3);
        filtered.Reconcile();

        Assert.Multiple(() =>
        {
            Assert.That(filtered, Is.EqualTo(new[] { first, third }));
            Assert.That(filtered[0], Is.SameAs(first));
            Assert.That(actions, Does.Contain(NotifyCollectionChangedAction.Remove));
            Assert.That(actions, Does.Contain(NotifyCollectionChangedAction.Add));
            Assert.That(actions, Does.Not.Contain(NotifyCollectionChangedAction.Reset));
        });
    }

    [Test]
    public void Should_Insert_Reconciled_Item_In_Source_Order()
    {
        var first = new TestItem(1);
        var second = new TestItem(2);
        var third = new TestItem(3);
        var source = new ObservableCollection<TestItem> { first, second, third };
        var visible = new HashSet<int> { 1, 3 };
        using var filtered = new FilteredObservableCollection<TestItem>(
            source,
            item => visible.Contains(item.Id));
        var actions = new List<NotifyCollectionChangedAction>();
        filtered.CollectionChanged += (_, args) => actions.Add(args.Action);

        visible.Add(2);
        filtered.ReconcileItem(second);

        Assert.Multiple(() =>
        {
            Assert.That(filtered, Is.EqualTo(new[] { first, second, third }));
            Assert.That(actions, Is.EqualTo(new[] { NotifyCollectionChangedAction.Add }));
        });
    }

    private sealed record TestItem(int Id);
}
