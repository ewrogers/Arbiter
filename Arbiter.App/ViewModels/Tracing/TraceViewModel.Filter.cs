using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using Arbiter.App.Collections;
using Arbiter.App.Models.Tracing;
using Arbiter.App.Threading;
using Arbiter.Net.Client;
using Arbiter.Net.Server;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Arbiter.App.ViewModels.Tracing;

public partial class TraceViewModel
{
    private readonly Debouncer _filterRefreshDebouncer = new(TimeSpan.FromMilliseconds(50), Dispatcher.UIThread);

    [ObservableProperty] private bool _showFilterBar;

    public FilteredObservableCollection<TracePacketViewModel> FilteredPackets { get; }
    public TraceFilterViewModel FilterParameters { get; } = new();
    public bool IsFilterActive => FilterParameters.Commands.Any(command => !command.IsSelected) ||
                                  FilterParameters.Clients.Any(client => !client.IsSelected);

    private void OnFilterParametersChanged(object? sender, PropertyChangedEventArgs e)
    {
        OnPropertyChanged(nameof(IsFilterActive));
        RequestFilterRefresh();
    }

    private void OnFilterClientsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(IsFilterActive));
    }

    private void RequestFilterRefresh()
    {
        // This is debounced to avoid excessive refreshing when multiple changes are made rapidly
        _filterRefreshDebouncer.Execute(() => { FilteredPackets.Refresh(); });
    }

    private bool MatchesFilter(TracePacketViewModel vm)
    {
        if (vm.Direction == PacketDirection.Client)
        {
            var packet = vm.DecryptedPacket as ClientPacket;
            var clientCommands = FilterParameters.SelectedClientCommands;
            if (clientCommands.All(cmd => cmd != (byte)packet!.Command))
            {
                return false;
            }
        }
        else if (vm.Direction == PacketDirection.Server)
        {
            var packet = vm.DecryptedPacket as ServerPacket;
            var serverCommands = FilterParameters.SelectedServerCommands;
            if (serverCommands.All(cmd => cmd != (byte)packet!.Command))
            {
                return false;
            }
        }

        // Filter by client name matches
        if (FilterParameters.Clients.Count > 0)
        {
            // Unauthenticated clients use the empty string as their name (ex: `connection 1`)
            var nameMatches = FilterParameters.Clients.Any(client =>
                client.IsSelected &&
                string.Equals(client.DisplayName, vm.ClientName ?? string.Empty, StringComparison.OrdinalIgnoreCase));

            if (!nameMatches)
            {
                return false;
            }
        }

        return true;
    }

    [RelayCommand]
    private void SelectAllCommands()
    {
        foreach (var command in FilterParameters.Commands)
        {
            command.IsSelected = true;
        }
    }

    [RelayCommand]
    private void SelectAllClients()
    {
        foreach (var client in FilterParameters.Clients)
        {
            client.IsSelected = true;
        }
    }

    // Prune any client entries from filter parameters that are no longer present in the packet list
    private void PruneClientsNotInPackets()
    {
        // Build case-insensitive set of remaining client names from all packets
        var remaining = _allPackets
            .Select(p => p.ClientName ?? string.Empty)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (FilterParameters.Clients.Count == 0)
        {
            return;
        }

        // Determine which client filter entries are no longer present
        var toRemove = FilterParameters.Clients
            .Where(c => !remaining.Contains(c.DisplayName ?? string.Empty))
            .Select(c => c.DisplayName ?? string.Empty)
            .ToList();

        foreach (var name in toRemove)
        {
            FilterParameters.TryRemoveClient(name);
        }

        if (toRemove.Count > 0)
        {
            // Trigger a refresh of the filtered view since client filters changed
            RequestFilterRefresh();
        }
    }
}
