using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Avalonia.Threading;

namespace Arbiter.App.Threading;

public sealed class DispatcherBatchQueue<T>
{
    private readonly ConcurrentQueue<T> _queue = new();
    private readonly Action<IReadOnlyList<T>> _applyBatch;
    private readonly Dispatcher _dispatcher;
    private readonly DispatcherPriority _priority;
    private readonly int _maxBatchSize;

    private int _isScheduled;

    public DispatcherBatchQueue(Action<IReadOnlyList<T>> applyBatch, int maxBatchSize = 256,
        Dispatcher? dispatcher = null, DispatcherPriority? priority = null)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxBatchSize, 1);

        _applyBatch = applyBatch ?? throw new ArgumentNullException(nameof(applyBatch));
        _maxBatchSize = maxBatchSize;
        _dispatcher = dispatcher ?? Dispatcher.UIThread;
        _priority = priority ?? DispatcherPriority.Background;
    }

    public void Enqueue(T item)
    {
        _queue.Enqueue(item);
        ScheduleDrain();
    }

    public void Clear() => _queue.Clear();

    public void DrainAll()
    {
        _dispatcher.VerifyAccess();

        while (!_queue.IsEmpty)
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
            if (!_queue.IsEmpty)
            {
                ScheduleDrain();
            }
        }
    }

    private void ApplyNextBatch()
    {
        var batch = new List<T>(_maxBatchSize);
        while (batch.Count < _maxBatchSize && _queue.TryDequeue(out var item))
        {
            batch.Add(item);
        }

        if (batch.Count > 0)
        {
            _applyBatch(batch);
        }
    }
}
