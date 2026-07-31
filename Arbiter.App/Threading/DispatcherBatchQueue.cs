using System;
using System.Collections.Generic;
using System.Threading;
using Avalonia.Threading;

namespace Arbiter.App.Threading;

public sealed class DispatcherBatchQueue<T>
{
    private readonly Lock _queueLock = new();
    private readonly Queue<T> _queue = [];
    private readonly Action<IReadOnlyList<T>> _applyBatch;
    private readonly Dispatcher _dispatcher;
    private readonly DispatcherPriority _priority;
    private readonly int _maxBatchSize;
    private readonly int? _maxPendingItems;

    private int _isScheduled;
    private long _droppedItemCount;

    public int PendingCount
    {
        get
        {
            using var _ = _queueLock.EnterScope();
            return _queue.Count;
        }
    }

    public long DroppedItemCount => Interlocked.Read(ref _droppedItemCount);

    public DispatcherBatchQueue(Action<IReadOnlyList<T>> applyBatch, int maxBatchSize = 256,
        Dispatcher? dispatcher = null, DispatcherPriority? priority = null, int? maxPendingItems = null)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxBatchSize, 1);
        if (maxPendingItems.HasValue)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(maxPendingItems.Value, 1);
        }

        _applyBatch = applyBatch ?? throw new ArgumentNullException(nameof(applyBatch));
        _maxBatchSize = maxBatchSize;
        _maxPendingItems = maxPendingItems;
        _dispatcher = dispatcher ?? Dispatcher.UIThread;
        _priority = priority ?? DispatcherPriority.Background;
    }

    public int Enqueue(T item)
    {
        var droppedItems = 0;
        {
            using var _ = _queueLock.EnterScope();
            _queue.Enqueue(item);

            while (_maxPendingItems.HasValue && _queue.Count > _maxPendingItems.Value)
            {
                _queue.Dequeue();
                droppedItems++;
            }
        }

        if (droppedItems > 0)
        {
            Interlocked.Add(ref _droppedItemCount, droppedItems);
        }

        ScheduleDrain();
        return droppedItems;
    }

    public void Clear()
    {
        using var _ = _queueLock.EnterScope();
        _queue.Clear();
    }

    public void ResetDroppedItemCount() => Interlocked.Exchange(ref _droppedItemCount, 0);

    public void DrainAll()
    {
        _dispatcher.VerifyAccess();

        while (HasPendingItems())
        {
            ApplyNextBatch();
        }
    }

    private void ScheduleDrain()
    {
        if (Interlocked.CompareExchange(ref _isScheduled, 1, 0) != 0)
        {
            return;
        }

        _dispatcher.Post(Drain, _priority);
    }

    private void Drain()
    {
        try
        {
            ApplyNextBatch();
        }
        finally
        {
            Volatile.Write(ref _isScheduled, 0);
            if (HasPendingItems())
            {
                ScheduleDrain();
            }
        }
    }

    private void ApplyNextBatch()
    {
        var batch = new List<T>(_maxBatchSize);
        {
            using var _ = _queueLock.EnterScope();
            while (batch.Count < _maxBatchSize && _queue.TryDequeue(out var item))
            {
                batch.Add(item);
            }
        }

        if (batch.Count > 0)
        {
            _applyBatch(batch);
        }
    }

    private bool HasPendingItems()
    {
        using var _ = _queueLock.EnterScope();
        return _queue.Count > 0;
    }
}
