using System;
using System.Linq;
using Arbiter.App.Collections;
using Arbiter.App.Models.Player;
using Arbiter.App.Models.Settings;
using Arbiter.Net;
using Arbiter.Net.Filters;
using Arbiter.Net.Proxy;
using Arbiter.Net.Server.Messages;
using Arbiter.Net.Types;

namespace Arbiter.App.ViewModels.Proxy;

public partial class ProxyViewModel
{
    private NetworkFilterRef? _debugDialogFilter;
    private NetworkFilterRef? _debugDialogMenuFilter;
    private NetworkFilterRef? _debugDialogMenuItemQuantityFilter;

    private void AddDebugDialogFilters(DebugSettings settings)
    {
        _debugDialogFilter = _proxyServer.AddFilter<ServerPursuitMessage>(HandleDialogMessage,
            $"{FilterPrefix}_Dialog_ServerShowDialog", DebugFilterPriority, settings);

        _debugDialogMenuFilter = _proxyServer.AddFilter<ServerScreenMenuMessage>(HandleDialogMenuMessage,
            $"{FilterPrefix}_Dialog_ServerShowDialogMenu", DebugFilterPriority, settings);

        _debugDialogMenuItemQuantityFilter = _proxyServer.AddFilter<ServerScreenMenuMessage>(
            HandleDialogItemQuantityMenuMessage,
            $"{FilterPrefix}_Dialog_ItemQuantity_ServerShowDialogMenu", int.MinValue, settings);
    }

    private void RemoveDebugDialogFilters()
    {
        _debugDialogFilter?.Unregister();
        _debugDialogMenuFilter?.Unregister();
        _debugDialogMenuItemQuantityFilter?.Unregister();
    }

    private static NetworkPacket HandleDialogMessage(ProxyConnection connection, ServerPursuitMessage message,
        object? parameter, NetworkMessageFilterResult<ServerPursuitMessage> result)
    {
        if (parameter is not DebugSettings settings || settings is { ShowDialogId: false, ShowPursuitId: false })
        {
            return result.Passthrough();
        }

        var hasChanges = false;

        if (settings.ShowDialogId)
        {
            var name = !string.IsNullOrWhiteSpace(message.Name) ? message.Name : message.EntityType.ToString();
            message.Name = $"{name} {{=h[0x{message.EntityId:X4}]";
            hasChanges = true;
        }

        if (settings.ShowPursuitId)
        {
            var pursuitText = $"{{=ePursuit {message.PursuitId} => Step {message.StepId}";
            message.Name = $"{message.Name} {pursuitText}";
            hasChanges = true;

        }

        return hasChanges ? result.Replace(message) : result.Passthrough();
    }

    private static NetworkPacket HandleDialogMenuMessage(ProxyConnection connection,
        ServerScreenMenuMessage message, object? parameter,
        NetworkMessageFilterResult<ServerScreenMenuMessage> result)
    {
        if (parameter is not DebugSettings settings || settings is { ShowDialogId: false, ShowPursuitId: false })
        {
            return result.Passthrough();
        }

        var hasChanges = false;

        if (settings.ShowDialogId)
        {
            var name = !string.IsNullOrWhiteSpace(message.Name) ? message.Name : message.EntityType.ToString();
            message.Name = $"{name} {{=h[0x{message.EntityId:X4}]";
            hasChanges = true;
        }

        if (settings.ShowPursuitId)
        {
            if (message.PursuitId is > 0)
            {
                var pursuitText = $"{{=ePursuit {message.PursuitId}";
                message.Name = $"{message.Name} - {pursuitText}";
                hasChanges = true;
            }

            if (message.MenuChoices.Count > 0)
            {
                foreach (var choice in message.MenuChoices)
                {
                    var menuPursuitText = $"{{=j[{choice.PursuitId}]";
                    var maxChoiceLength = 50 - menuPursuitText.Length;
                    var choiceText = choice.Text.Length > maxChoiceLength
                        ? string.Concat(choice.Text.AsSpan(0, maxChoiceLength - 3), "...")
                        : choice.Text;

                    choice.Text = $"{choiceText} {menuPursuitText}";
                }

                hasChanges = true;
            }
        }

        return hasChanges ? result.Replace(message) : result.Passthrough();
    }

    private NetworkPacket HandleDialogItemQuantityMenuMessage(ProxyConnection connection,
        ServerScreenMenuMessage message, object? parameter,
        NetworkMessageFilterResult<ServerScreenMenuMessage> result)
    {
        if (parameter is not DebugSettings settings || settings is { ShowDialogItemQuantity: false } ||
            message.MenuType != DialogMenuType.UserInventory)
        {
            return result.Passthrough();
        }

        // If unable to get the player OR no stackable items just passthrough
        if (!_playerService.TryGetState(connection.Id, out var player) ||
            !player.Inventory.Any(i => i.Value.IsStackable))
        {
            return result.Passthrough();
        }

        SendItemQuantityOverrides(connection, player.Inventory);
        return result.Passthrough();
    }

    private static void SendItemQuantityOverrides(ProxyConnection connection,
        ISlottedCollection<InventoryItem> inventory)
    {
        // First we update the client with the stack size in the name
        var addItemMessages = inventory.Where(i => i.Value.IsStackable)
            .Select(item =>
            {
                var baseName = InventoryItem.GetBaseName(item.Value.Name);
                return new ServerAddInventoryMessage
                {
                    Slot = (byte)item.Slot,
                    Name = $"{baseName} [{item.Value.Quantity}]",
                    Sprite = item.Value!.Sprite,
                    Color = (DyeColor)item.Value.Color,
                    Quantity = (uint)item.Value.Quantity,
                    IsStackable = item.Value.IsStackable,
                    Durability = (uint)(item.Value.Durability ?? 0),
                    MaxDurability = (uint)(item.Value.MaxDurability ?? 0)
                };
            });
        connection.EnqueueMessages(addItemMessages, NetworkPriority.High);

        // Then we revert it after a short delay which will happen after the dialog is shown
        // This happens very quickly so its not noticeable to the user
        var revertItemMessages = inventory.Where(i => i.Value.IsStackable)
            .Select(item =>
            {
                var baseName = InventoryItem.GetBaseName(item.Value.Name);
                return new ServerAddInventoryMessage
                {
                    Slot = (byte)item.Slot,
                    Name = baseName,
                    Sprite = item.Value!.Sprite,
                    Color = (DyeColor)item.Value.Color,
                    Quantity = (uint)item.Value.Quantity,
                    IsStackable = item.Value.IsStackable,
                    Durability = (uint)(item.Value.Durability ?? 0),
                    MaxDurability = (uint)(item.Value.MaxDurability ?? 0)
                };
            });
        connection.EnqueueMessagesAfter(revertItemMessages, TimeSpan.FromMilliseconds(100), NetworkPriority.High);
    }
}
