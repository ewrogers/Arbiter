using System;
using System.Collections.Generic;
using System.Threading;
using Arbiter.App.Models.Tracing;
using Arbiter.App.Threading;
using Arbiter.Net;
using Arbiter.Net.Filters;
using Arbiter.Net.Proxy;
using Microsoft.Extensions.Logging;

namespace Arbiter.App.ViewModels.Tracing;

public partial class TraceViewModel
{
    private const int MaxPendingTracePackets = 4096;

    private sealed record QueuedTracePacket(long Generation, NetworkPacket Encrypted, NetworkPacket Decrypted,
        NetworkFilterResult? FilterResult, string? ClientName, int ConnectionId, PacketDisplayMode DisplayMode);

    private readonly DispatcherBatchQueue<QueuedTracePacket> _packetQueue;
    private long _packetGeneration;
    private int _isAcceptingPackets;
    private int _hasReportedPacketOverflow;
    private bool _isApplyingPacketBatch;

    private void QueuePacket(ProxyConnectionDataEventArgs e)
    {
        var generation = Volatile.Read(ref _packetGeneration);
        if (Volatile.Read(ref _isAcceptingPackets) == 0)
        {
            return;
        }

        // Ignore packets from other clients if a client is selected
        var connection = e.Connection;
        var name = connection.Name;
        if (!string.IsNullOrWhiteSpace(TraceClientName) &&
            !string.Equals(TraceClientName, name, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var droppedItems = _packetQueue.Enqueue(new QueuedTracePacket(generation, e.Encrypted, e.Decrypted,
            e.FilterResult, name, connection.Id, _packetDisplayMode));
        if (droppedItems > 0 && Interlocked.CompareExchange(ref _hasReportedPacketOverflow, 1, 0) == 0)
        {
            _logger.LogWarning("Trace processing fell behind; oldest pending packets will be dropped");
        }
    }

    private void ApplyPacketBatch(IReadOnlyList<QueuedTracePacket> packets)
    {
        var generation = Volatile.Read(ref _packetGeneration);
        var removedPackets = false;

        _isApplyingPacketBatch = true;
        try
        {
            using (FilteredPackets.DeferRefresh())
            {
                foreach (var queued in packets)
                {
                    if (queued.Generation != generation)
                    {
                        continue;
                    }

                    var vm = new TracePacketViewModel(queued.Encrypted, queued.Decrypted, queued.FilterResult,
                        queued.ClientName, queued.ConnectionId)
                    {
                        DisplayMode = queued.DisplayMode
                    };

                    AddPacketToTrace(vm, false);
                }

                while (_allPackets.Count > MaxTraceHistory)
                {
                    _allPackets.RemoveAt(0);
                    removedPackets = true;
                }
            }
        }
        finally
        {
            _isApplyingPacketBatch = false;
        }

        if (removedPackets)
        {
            RefreshAfterPacketRemoval();
        }
    }
}
