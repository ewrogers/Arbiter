using System;
using System.Linq;
using System.Threading.Tasks;
using Arbiter.App.Extensions;
using Arbiter.App.Models.Tracing;
using Arbiter.Net.Client;
using Arbiter.Net.Server;
using Avalonia;
using CommunityToolkit.Mvvm.Input;

namespace Arbiter.App.ViewModels.Tracing;

public partial class TraceViewModel
{
    private bool HasSelection() => SelectedPackets.Count > 0;
    private bool HasSingleSelection() => SelectedPackets.Count == 1;

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private async Task CopyToClipboard()
    {
        var clipboard = Application.Current?.TryGetClipboard();
        if (clipboard is null || SelectedPackets.Count == 0)
        {
            return;
        }

        var sortedSelection = SelectedPackets.OrderBy(p => p.Index);
        var packetStrings = sortedSelection.Select(vm => vm.DisplayMode switch
        {
            PacketDisplayMode.Decrypted =>
                $"{(vm.DecryptedPacket is ClientPacket ? ">" : "<")} {vm.Command:X2} {vm.FormattedDecrypted}",
            _ => vm.FormattedEncrypted,
        });

        var lines = string.Join(Environment.NewLine, packetStrings);
        await clipboard.SetTextAsync(lines);
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void DeleteSelected()
    {
        if (SelectedPackets.Count == 0)
        {
            return;
        }
        
        var selectedPackets = SelectedPackets.ToList();
        foreach (var packet in selectedPackets)
        {
            _allPackets.Remove(packet);
        }
        
        SelectedPackets.Clear();
        RefreshSearchResults();
        
        if (selectedPackets.Count > 0)
        {
            IsDirty = true;
        }
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void ExcludeSelected()
    {
        if (SelectedPackets.Count == 0)
        {
            return;
        }

        var clientCommandsToExclude = SelectedPackets.Where(vm => vm.Direction == PacketDirection.Client)
            .Select(vm => (ClientCommand)vm.DecryptedPacket.Command).ToList();

        var serverCommandsToExclude = SelectedPackets.Where(vm => vm.Direction == PacketDirection.Server)
            .Select(vm => (ServerCommand)vm.DecryptedPacket.Command).ToList();

        foreach (var clientCommand in clientCommandsToExclude)
        {
            FilterParameters.UnselectCommand(clientCommand);
        }

        foreach (var serverCommand in serverCommandsToExclude)
        {
            FilterParameters.UnselectCommand(serverCommand);
        }

        ShowFilterBar = true;
    }

    [RelayCommand(CanExecute = nameof(HasSingleSelection))]
    private void FindSelected()
    {
        if (SelectedPackets.Count != 1)
        {
            return;
        }

        var packet = SelectedPackets[0];
        SearchParameters.SetCommand(packet.Direction, packet.Command);

        ShowSearchBar = true;
    }
}
