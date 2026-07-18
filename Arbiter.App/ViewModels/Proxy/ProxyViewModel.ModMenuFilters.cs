using System.Collections.Generic;
using System.Linq;
using Arbiter.App.Collections;
using Arbiter.App.Models.Entities;
using Arbiter.App.Models.Player;
using Arbiter.App.Models.Settings;
using Arbiter.Net;
using Arbiter.Net.Client.Messages;
using Arbiter.Net.Filters;
using Arbiter.Net.Proxy;
using Arbiter.Net.Server.Messages;
using Arbiter.Net.Server.Types;
using Arbiter.Net.Types;

namespace Arbiter.App.ViewModels.Proxy;

public partial class ProxyViewModel
{
    private const int ModMenuFilterPriority = int.MinValue + 100;
    
    private static readonly List<(string Text, ushort PursuitId)> ModMenuChoices =
    [
        ("Interact", 0xFF00),
        ("Buy", 64),
        ("Sell", 65),
        ("Destroy Item", 0xFF01),
        ("Fix Item", 71),
        ("Fix All Items", 72),
        ("Deposit Money", 66),
        ("Deposit Item", 67),
        ("Withdraw Money", 68),
        ("Withdraw Item", 69),
        ("Send Parcel", 96),
        ("Receive Parcel", 97),
    ];

    private NetworkFilterRef? _modMenuInteractFilter;

    private void AddModMenuFilters(DebugSettings settings)
    {
        _modMenuInteractFilter = _proxyServer.AddFilter<ClientRequestObjectInfoMessage>(HandleModMenuInteractMessage,
            $"{FilterPrefix}_ModMenu_ClientInteract", ModMenuFilterPriority, settings);

        _modMenuInteractFilter = _proxyServer.AddFilter<ClientMerchantMessage>(HandleModMenuDialogChoiceMessage,
            $"{FilterPrefix}_ModMenu_ClientInteract", ModMenuFilterPriority, settings);
    }

    private void RemoveModMenuFilters()
    {
        _modMenuInteractFilter?.Unregister();
    }

    private NetworkPacket? HandleModMenuInteractMessage(ProxyConnection connection, ClientRequestObjectInfoMessage message,
        object? parameter, NetworkMessageFilterResult<ClientRequestObjectInfoMessage> result)
    {
        if (parameter is not DebugSettings { EnableNpcModMenu: true })
        {
            return result.Passthrough();
        }

        // If the interaction is not a entity interaction, ignore it
        if (message.InteractionType != InteractionType.Entity || message.TargetId is null)
        {
            return result.Passthrough();
        }

        // If the entity is not found or not a NPC, ignore it
        if (!_entityStore.TryGetEntity(message.TargetId.Value, out var entity) ||
            !entity.Flags.HasFlag(EntityFlags.Mundane))
        {
            return result.Passthrough();
        }

        // Build a new dialog menu instead and send it
        var dialog = GetModMenuDialogForEntity(entity);
        connection.EnqueueMessage(dialog);

        return result.Block();
    }

    private NetworkPacket? HandleModMenuDialogChoiceMessage(ProxyConnection connection,
        ClientMerchantMessage message,
        object? parameter, NetworkMessageFilterResult<ClientMerchantMessage> result)
    {
        if (parameter is not DebugSettings { EnableNpcModMenu: true })
        {
            return result.Passthrough();
        }

        // Treat as interact request
        if (message.PursuitId == 0xFF00)
        {
            var interactMessage = new ClientRequestObjectInfoMessage
            {
                InteractionType = InteractionType.Entity,
                TargetId = message.EntityId
            };
            connection.EnqueueMessage(interactMessage);
            return result.Block();
        }

        // Handle destroy item custom dialog menu
        if (message.PursuitId == 0xFF01 && _entityStore.TryGetEntity(message.EntityId, out var entity) &&
            entity.Flags.HasFlag(EntityFlags.Mundane))
        {
            // Get the player so we can get their inventory
            _playerService.TryGetState(connection.Id, out var player);
            var inventory = player?.Inventory ?? new SlottedCollection<InventoryItem>(1);
            
            var destroyItemMenu = GetDestroyItemDialogForEntity(entity, inventory);
            connection.EnqueueMessage(destroyItemMenu);
            return result.Block();
        }

        // Only block our virtual menu pursuits
        return message.PursuitId >= 0xFF00 ? result.Block() : result.Passthrough();
    }

    private static ServerScreenMenuMessage GetModMenuDialogForEntity(GameEntity entity)
    {
        var dialog = new ServerScreenMenuMessage
        {
            MenuType = DialogMenuType.Menu,
            EntityType = entity.Flags switch
            {
                EntityFlags.Item => EntityTypeFlags.Item,
                EntityFlags.Reactor => EntityTypeFlags.Reactor,
                _ => EntityTypeFlags.Creature
            },
            EntityId = (uint)entity.Id,
            Sprite = entity.Sprite,
            SpriteType = SpriteType.Monster,
            ShowGraphic = true,
            PursuitId = 0xFFFF,
            Name = entity.Name,
            Content =
                "{=gThis is the NPC mod menu injected by {=eArbiter{=g.\n\n{=hYou can disable it in Settings -> NPCs."
        };

        foreach (var choice in ModMenuChoices)
        {
            dialog.MenuChoices.Add(new ServerDialogMenuChoice
            {
                Text = choice.Text,
                PursuitId = choice.PursuitId
            });
        }

        return dialog;
    }

    private static ServerScreenMenuMessage GetDestroyItemDialogForEntity(GameEntity entity,
        ISlottedCollection<InventoryItem> inventory)
    {
        var dialog = new ServerScreenMenuMessage
        {
            MenuType = DialogMenuType.UserInventory,
            EntityType = entity.Flags switch
            {
                EntityFlags.Item => EntityTypeFlags.Item,
                EntityFlags.Reactor => EntityTypeFlags.Reactor,
                _ => EntityTypeFlags.Creature
            },
            EntityId = (uint)entity.Id,
            Sprite = entity.Sprite,
            SpriteType = SpriteType.Monster,
            ShowGraphic = true,
            PursuitId = 77,
            Name = entity.Name,
            Content =
                "Which item do you wish to destroy?\n{=hThis may not work on items which cannot be dropped.\n{=sBe careful! This cannot be undone.\n"
        };
        
        // Add all used slots to the dialog
        dialog.InventorySlots.AddRange(inventory.GetNonEmptySlots().Select(x => (byte)x));

        return dialog;
    }
}
