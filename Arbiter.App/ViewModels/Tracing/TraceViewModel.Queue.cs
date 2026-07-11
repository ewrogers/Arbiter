using System;
using System.Collections.Generic;
using System.Threading;
using Arbiter.App.Models.Tracing;
using Arbiter.App.Threading;
using Arbiter.Net;
using Arbiter.Net.Filters;
using Arbiter.Net.Proxy;

namespace Arbiter.App.ViewModels.Tracing;

public partial class TraceViewModel
{
    private sealed record QueuedTracePacket(long Generation, NetworkPacket Encrypted, NetworkPacket Decrypted,
        NetworkFilterResult? FilterResult, string? ClientName, int ConnectionId, PacketDisplayMode DisplayMode);

    private readonly DispatcherBatchQueue<QueuedTracePacket> _packetQueue;
    private long _packetGeneration;
    private int _isAcceptingPackets;

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

        var displayName = connection.IsLoggedIn ? name : null;
        _packetQueue.Enqueue(new QueuedTracePacket(generation, e.Encrypted, e.Decrypted, e.FilterResult,
            displayName, connection.Id, _packetDisplayMode));
    }

    private void ApplyPacketBatch(IReadOnlyList<QueuedTracePacket> packets)
    {
        var generation = Volatile.Read(ref _packetGeneration);
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
        }
    }
}
