using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using Avalonia.Threading;

namespace Arbiter.App.Collections;

public class FilteredObservableCollection<T> : ObservableCollection<T>, IDisposable
{
    private bool _isDisposed;
    private int _deferLevel;
    private bool _refreshRequired;

    private readonly ObservableCollection<T> _sourceCollection;
    private Func<T, bool> _predicate;

    public Func<T, bool> Predicate
    {
        get => _predicate;
        set
        {
            _predicate = value;
            Refresh();
        }
    }

    public FilteredObservableCollection(ObservableCollection<T> sourceCollection, Func<T, bool> predicate)
    {
        _sourceCollection = sourceCollection ?? throw new ArgumentNullException(nameof(sourceCollection));
        _predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));

        _sourceCollection.CollectionChanged += OnSourceCollectionChanged;
        Refresh();
    }

    public void Refresh()
    {
        Dispatcher.UIThread.VerifyAccess();

        var copy = _sourceCollection.Where(Predicate).ToList();

        CheckReentrancy();
        Items.Clear();
        foreach (var item in copy)
        {
            Items.Add(item);
        }

        OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
        OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    public void Reconcile()
    {
        Dispatcher.UIThread.VerifyAccess();

        for (var index = Count - 1; index >= 0; index--)
        {
            var item = this[index];
            if (!_sourceCollection.Contains(item) || !Predicate(item))
            {
                RemoveAt(index);
            }
        }

        var targetIndex = 0;
        foreach (var item in _sourceCollection)
        {
            if (!Predicate(item))
            {
                continue;
            }

            var currentIndex = IndexOf(item);
            if (currentIndex < 0)
            {
                Insert(targetIndex, item);
            }
            else if (currentIndex != targetIndex)
            {
                Move(currentIndex, targetIndex);
            }

            targetIndex++;
        }
    }

    public void ReconcileItem(T item)
    {
        Dispatcher.UIThread.VerifyAccess();

        var sourceIndex = _sourceCollection.IndexOf(item);
        var currentIndex = IndexOf(item);
        if (sourceIndex < 0 || !Predicate(item))
        {
            if (currentIndex >= 0)
            {
                RemoveAt(currentIndex);
            }

            return;
        }

        if (currentIndex >= 0)
        {
            return;
        }

        var targetIndex = Math.Min(GetFilteredIndexForSourceIndex(sourceIndex), Count);
        Insert(targetIndex, item);
    }

    public IDisposable DeferRefresh()
    {
        Dispatcher.UIThread.VerifyAccess();
        _deferLevel++;
        return new RefreshDeferral(this);
    }

    private void OnSourceCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        Dispatcher.UIThread.VerifyAccess();

        if (_deferLevel > 0)
        {
            _refreshRequired = true;
            return;
        }

        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add when e.NewItems is not null:
                {
                    // Insert new items at the correct relative positions based on the source index
                    var newIndex = e.NewStartingIndex;
                    var i = 0;
                    foreach (var item in e.NewItems.OfType<T>())
                    {
                        if (Predicate(item))
                        {
                            var targetIndex = GetFilteredIndexForSourceIndex(newIndex + i);
                            targetIndex = Math.Min(targetIndex, Count);
                            Insert(targetIndex, item);
                        }
                        i++;
                    }
                    break;
                }

            case NotifyCollectionChangedAction.Move when e.OldItems is not null:
                {
                    // Move items to match the source order when the source moves them
                    var newIndex = e.NewStartingIndex;
                    var i = 0;
                    foreach (var item in e.OldItems.OfType<T>())
                    {
                        if (Predicate(item))
                        {
                            var currentIndex = IndexOf(item);
                            if (currentIndex >= 0)
                            {
                                var targetIndex = GetFilteredIndexForSourceIndex(newIndex + i);
                                targetIndex = Math.Min(targetIndex, Count - 1);
                                if (currentIndex != targetIndex)
                                {
                                    Move(currentIndex, targetIndex);
                                }
                            }
                        }
                        i++;
                    }
                    break;
                }

            case NotifyCollectionChangedAction.Remove when e.OldItems is not null:
                foreach (var item in e.OldItems.OfType<T>())
                {
                    Remove(item);
                }

                break;

            case NotifyCollectionChangedAction.Replace when e.OldItems is not null && e.NewItems is not null:
                foreach (var item in e.OldItems.OfType<T>())
                {
                    Remove(item);
                }

                {
                    var newIndex = e.NewStartingIndex;
                    var i = 0;
                    foreach (var item in e.NewItems.OfType<T>())
                    {
                        if (Predicate(item))
                        {
                            var targetIndex = GetFilteredIndexForSourceIndex(newIndex + i);
                            targetIndex = Math.Min(targetIndex, Count);
                            Insert(targetIndex, item);
                        }
                        i++;
                    }
                }

                break;

            case NotifyCollectionChangedAction.Reset:
                Refresh();
                break;
        }
    }

    private int GetFilteredIndexForSourceIndex(int sourceIndex)
    {
        // Count how many items in the source match the predicate up to the source index
        var count = 0;
        for (var i = 0; i < Math.Min(sourceIndex, _sourceCollection.Count); i++)
        {
            var srcItem = _sourceCollection[i];
            if (Predicate(srcItem))
            {
                count++;
            }
        }
        return count;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool isDisposing)
    {
        if (_isDisposed)
        {
            return;
        }

        if (isDisposing)
        {
            _sourceCollection.CollectionChanged -= OnSourceCollectionChanged;
        }

        _isDisposed = true;
    }

    private void EndDefer()
    {
        if (_deferLevel == 0)
        {
            return;
        }

        _deferLevel--;
        if (_deferLevel > 0 || !_refreshRequired)
        {
            return;
        }

        _refreshRequired = false;
        Refresh();
    }

    private sealed class RefreshDeferral(FilteredObservableCollection<T> collection) : IDisposable
    {
        private FilteredObservableCollection<T>? _collection = collection;

        public void Dispose()
        {
            var owner = _collection;
            _collection = null;
            owner?.EndDefer();
        }
    }
}
