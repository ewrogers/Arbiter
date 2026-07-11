using System;
using System.Buffers.Binary;
using System.Linq;
using System.Threading.Tasks;
using Arbiter.App.Extensions;
using Arbiter.App.Models.Entities;
using Arbiter.Net.Client.Messages;
using Arbiter.Net.Types;
using Avalonia;
using Avalonia.Input.Platform;
using CommunityToolkit.Mvvm.Input;

namespace Arbiter.App.ViewModels.Entities;

public partial class EntityManagerViewModel
{
    private bool CanInteract() => SelectedClient is not null && SelectedEntities.Count == 1 &&
                                  !SelectedEntities[0].Flags.HasFlag(EntityFlags.Item);

    [RelayCommand(CanExecute = nameof(CanInteract))]
    private void Interact()
    {
        if (SelectedClient is null || SelectedEntities.Count == 0)
        {
            return;
        }

        var entityId = (uint)SelectedEntities[0].Id;
        var clientInteract = new ClientInteractMessage
        {
            InteractionType = InteractionType.Entity,
            TargetId = entityId
        };

        QueueInteractionRequest(SelectedClient.Id, entityId);
        SelectedClient.EnqueueMessage(clientInteract);
    }

    private bool CanCopyToClipboard() => SelectedEntities.Count == 1;
    
    [RelayCommand(CanExecute = nameof(CanCopyToClipboard))]
    private async Task CopyIdToClipboard()
    {
        var clipboard = Application.Current?.TryGetClipboard();
        if (clipboard is null || SelectedEntities.Count == 0)
        {
            return;
        }

        await clipboard.SetTextAsync(SelectedEntities[0].Id.ToString("X"));
    }
    
    [RelayCommand(CanExecute = nameof(CanCopyToClipboard))]
    private async Task CopyHexToClipboard()
    {
        var clipboard = Application.Current?.TryGetClipboard();
        if (clipboard is null || SelectedEntities.Count == 0)
        {
            return;
        }

        Span<byte> buffer = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(buffer, (uint)SelectedEntities[0].Id);
        await clipboard.SetTextAsync(buffer.ToHexString(" "));
    }
    
    [RelayCommand(CanExecute = nameof(CanCopyToClipboard))]
    private async Task CopySpriteToClipboard()
    {
        var clipboard = Application.Current?.TryGetClipboard();
        if (clipboard is null || SelectedEntities.Count == 0)
        {
            return;
        }

        await clipboard.SetTextAsync(SelectedEntities[0].Sprite.ToString());
    }
    
    private bool CanDeleteSelected() => SelectedEntities.Count > 0;

    [RelayCommand(CanExecute = nameof(CanDeleteSelected))]
    private void DeleteSelected()
    {
        if (SelectedEntities.Count == 0)
        {
            return;
        }
        
        var entities = SelectedEntities.ToList();
        foreach (var entity in entities)
        {
            _entityStore.RemoveEntity(entity.Id, out _);
            _allEntities.Remove(entity);
        }

        SelectedEntities.Clear();
    }
}
