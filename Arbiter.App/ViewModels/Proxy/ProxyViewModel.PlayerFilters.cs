using System;
using Arbiter.App.Models.Settings;
using Arbiter.Net;
using Arbiter.Net.Filters;
using Arbiter.Net.Proxy;
using Arbiter.Net.Server.Messages;
using Arbiter.Net.Types;

namespace Arbiter.App.ViewModels.Proxy;

public partial class ProxyViewModel
{
    private NetworkFilterRef? _debugShowUserFilter;
    private NetworkFilterRef? _debugShowEquipmentDurabilityFilter;
    private void AddDebugPlayerFilters(DebugSettings settings)
    {
        _debugShowUserFilter = _proxyServer.AddFilter<ServerDrawHumanObjectsMessage>(HandleShowUserMessage,
            $"{FilterPrefix}_Player_ServerShowUser", DebugFilterPriority, settings);

        _debugShowEquipmentDurabilityFilter = _proxyServer.AddFilter<ServerAddEquipMessage>(
            HandleSetEquipmentMessage, $"{FilterPrefix}_Player_ServerSetEquipment", DebugFilterPriority, settings);
    }

    private void RemoveDebugPlayerFilters()
    {
        _debugShowUserFilter?.Unregister();
        _debugShowEquipmentDurabilityFilter?.Unregister();
    }

    private static NetworkPacket HandleShowUserMessage(ProxyConnection connection, ServerDrawHumanObjectsMessage message,
        object? parameter, NetworkMessageFilterResult<ServerDrawHumanObjectsMessage> result)
    {
        if (parameter is not DebugSettings filterSettings || filterSettings is
                { ShowHiddenPlayers: false, ShowPlayerNames: false })
        {
            return result.Passthrough();
        }

        var hasChanges = false;
        if (message.IsHidden)
        {
            message.Name = "[Hidden]";
            message.NameStyle = NameTagStyle.Hostile;
            message.BodySprite = BodySprite.MaleInvisible;
            message.IsTranslucent = true;
            message.IsHidden = false;

            hasChanges = true;
        }
        else if (filterSettings.ShowPlayerNames)
        {
            // Always show names instead of mouse over
            message.NameStyle = NameTagStyle.Neutral;
            hasChanges = true;
        }

        return hasChanges ? result.Replace(message) : result.Passthrough();
    }

    private static NetworkPacket HandleSetEquipmentMessage(ProxyConnection connection,
        ServerAddEquipMessage message,
        object? parameter, NetworkMessageFilterResult<ServerAddEquipMessage> result)
    {
        if (parameter is not DebugSettings filterSettings || filterSettings is
                { ShowEquipmentDurability: false })
        {
            return result.Passthrough();
        }

        // Calculate durability percentage
        var percent = (int)Math.Floor(message.Durability * 100.0 / Math.Max(1, message.MaxDurability));
        percent = Math.Clamp(percent, 0, 100);

        // Add durability to name
        var durability = $"{message.Durability} / {message.MaxDurability} ({percent}%)";
        message.Name = $"{message.Name}\n{durability}";

        return result.Replace(message);
    }
}