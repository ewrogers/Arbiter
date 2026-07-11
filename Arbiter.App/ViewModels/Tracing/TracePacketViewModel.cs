using System;
using System.Collections.Generic;
using System.Linq;
using Arbiter.App.Models.Tracing;
using Arbiter.App.Models.Tracing.Queries;
using Arbiter.Net;
using Arbiter.Net.Client;
using Arbiter.Net.Filters;
using Arbiter.Net.Server;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Arbiter.App.ViewModels.Tracing;

public partial class TracePacketViewModel(
    NetworkPacket encrypted,
    NetworkPacket decrypted,
    NetworkFilterResult? filterResult,
    string? clientName = null,
    int? connectionId = null)
    : ViewModelBase
{

    [ObservableProperty] private long _index;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayValue))]
    [NotifyPropertyChangedFor(nameof(DisplaySearchHighlights))]
    [NotifyPropertyChangedFor(nameof(DisplayPayloadLines))]
    [NotifyPropertyChangedFor(nameof(HasDisplayPayload))]
    private PacketDisplayMode _displayMode = PacketDisplayMode.Decrypted;

    [ObservableProperty] private double _opacity = 1;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplaySearchHighlights))]
    [NotifyPropertyChangedFor(nameof(DisplayPayloadLines))]
    private IReadOnlyList<TraceQueryHighlight> _searchHighlights = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayClientName))]
    [NotifyPropertyChangedFor(nameof(IsConnectionLabel))]
    private string? _clientName = clientName;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayClientName))]
    [NotifyPropertyChangedFor(nameof(IsConnectionLabel))]
    private int? _connectionId = connectionId;

    [ObservableProperty] private bool _isDetailedView = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayPayloadLines))]
    private int _detailedBytesPerLine = TracePayloadFormatter.MinimumBytesPerLine;

    [ObservableProperty] private double _detailedHexColumnWidth = 365;

    [ObservableProperty]
    private PacketDirection _direction = decrypted is ClientPacket ? PacketDirection.Client : PacketDirection.Server;

    [ObservableProperty] private byte _command = encrypted.Command;

    [ObservableProperty] private string _commandName = GetCommandName(encrypted);

    [ObservableProperty] private byte? _sequence = encrypted switch
    {
        ClientPacket clientPacket => clientPacket.Sequence,
        ServerPacket serverPacket => serverPacket.Sequence,
        _ => null
    };

    [ObservableProperty]
    private uint? _checksum = encrypted is ClientPacket clientPacket ? clientPacket.Checksum : null;

    [ObservableProperty] private string _formattedEncrypted = string.Join(' ', encrypted.Select(x => x.ToString("X2")));

    [ObservableProperty]
    private string _formattedDecrypted = string.Join(' ', decrypted.Data.Select(x => x.ToString("X2")));

    [ObservableProperty] private string? _formattedFiltered = filterResult?.Output is not null
        ? string.Join(' ', filterResult.Output.Data.Select(x => x.ToString("X2")))
        : null;

    public DateTime Timestamp { get; private init; } = DateTime.Now;
    public NetworkPacket EncryptedPacket { get; } = encrypted;
    public NetworkPacket DecryptedPacket { get; } = decrypted;
    public byte[] RawData { get; } = encrypted.ToArray();

    public NetworkPacket? FilteredPacket { get; } = filterResult?.Output;
    public NetworkFilterAction FilterAction { get; } = filterResult?.Action ?? NetworkFilterAction.Allow;
    public bool WasBlocked => FilterAction == NetworkFilterAction.Block;
    public bool WasReplaced => FilterAction == NetworkFilterAction.Replace;

    public string DisplayValue => DisplayMode switch
    {
        PacketDisplayMode.Decrypted => WasReplaced && FormattedFiltered is not null ? FormattedFiltered : FormattedDecrypted,
        _ => FormattedEncrypted
    };

    public IReadOnlyList<TraceQueryHighlight> DisplaySearchHighlights => SearchHighlights
        .Where(highlight => highlight.Source == (DisplayMode == PacketDisplayMode.Raw
            ? TraceQueryHighlightSource.Raw
            : TraceQueryHighlightSource.Data))
        .ToList();

    public IReadOnlyList<TracePayloadLine> DisplayPayloadLines =>
        TracePayloadFormatter.Format(GetDisplayBytes(), DisplaySearchHighlights, DetailedBytesPerLine);

    public bool HasDisplayPayload => !GetDisplayBytes().IsEmpty;
    public string? DisplayClientName => !string.IsNullOrWhiteSpace(ClientName)
        ? ClientName
        : ConnectionId is not null ? $"conn[{ConnectionId}]" : null;
    public bool IsConnectionLabel => string.IsNullOrWhiteSpace(ClientName) && ConnectionId is not null;

    public bool IsClient => DecryptedPacket is ClientPacket;
    public bool IsServer => DecryptedPacket is ServerPacket;

    private ReadOnlySpan<byte> GetDisplayBytes()
    {
        if (DisplayMode == PacketDisplayMode.Raw)
        {
            return RawData;
        }

        return WasReplaced && FilteredPacket is not null
            ? FilteredPacket.Data
            : DecryptedPacket.Data;
    }

    public TracePacket ToTracePacket()
    {
        var tracePacket = new TracePacket
        {
            Timestamp = Timestamp,
            Direction = Direction,
            ClientName = ClientName,
            ConnectionId = ConnectionId,
            Command = Command,
            Sequence = Sequence,
            RawData = EncryptedPacket.ToList(),
            Payload = DecryptedPacket.Data,
            FilterAction = FilterAction,
            FilteredPayload = FilterAction switch
            {
                // Only need to store the filtered payload if we're replacing it
                NetworkFilterAction.Replace => FilteredPacket?.Data,
                _ => null
            },
            Checksum = Checksum
        };
        return tracePacket;
    }

    public static TracePacketViewModel FromTracePacket(TracePacket tracePacket, PacketDisplayMode displayMode)
    {
        var command = tracePacket.Command;
        var encryptedPayload = tracePacket.RawData.Skip(4).ToArray();
        var decryptedPayload = tracePacket.Payload.ToArray();
        var filteredPayload = tracePacket.FilteredPayload?.ToArray();

        NetworkPacket decryptedPacket = tracePacket.Direction switch
        {
            PacketDirection.Client => new ClientPacket(command, decryptedPayload, tracePacket.Checksum)
                { Sequence = tracePacket.Sequence },
            PacketDirection.Server => new ServerPacket(command, decryptedPayload)
                { Sequence = tracePacket.Sequence },
            _ => throw new InvalidOperationException("Invalid packet direction")
        };

        NetworkPacket? filteredPacket = null;
        if (filteredPayload is not null)
        {
            filteredPacket = tracePacket.Direction switch
            {
                PacketDirection.Client => new ClientPacket(command, filteredPayload)
                    { Sequence = tracePacket.Sequence },
                PacketDirection.Server => new ServerPacket(command, filteredPayload)
                    { Sequence = tracePacket.Sequence },
                _ => throw new InvalidOperationException("Invalid packet direction")
            };
        }

        NetworkPacket encryptedPacket = tracePacket.Direction switch
        {
            PacketDirection.Client => new ClientPacket(command, encryptedPayload, tracePacket.Checksum)
            {
                Sequence = tracePacket.Sequence
            },
            PacketDirection.Server => new ServerPacket(command, encryptedPayload)
            {
                Sequence = tracePacket.Sequence
            },
            _ => throw new InvalidOperationException("Invalid packet direction")
        };

        var filterResult = new NetworkFilterResult
        {
            Timestamp = tracePacket.Timestamp,
            Action = tracePacket.FilterAction,
            Input = encryptedPacket,
            Output = filteredPacket
        };

        return new TracePacketViewModel(encryptedPacket, decryptedPacket, filterResult, tracePacket.ClientName,
            tracePacket.ConnectionId)
        {
            Timestamp = tracePacket.Timestamp,
            DisplayMode = displayMode,
        };
    }

    private static string GetCommandName(NetworkPacket packet)
    {
        return packet switch
        {
            ClientPacket clientPacket => $"{clientPacket.Command}",
            ServerPacket serverPacket => $"{serverPacket.Command}",
            _ => $"Unknown {packet.Command:X2}"
        };
    }
}
