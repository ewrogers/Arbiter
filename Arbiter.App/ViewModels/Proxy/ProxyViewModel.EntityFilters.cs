using System;
using System.Collections.Concurrent;
using Arbiter.App.Models.Entities;
using Arbiter.App.Models.Settings;
using Arbiter.Net;
using Arbiter.Net.Client.Messages;
using Arbiter.Net.Filters;
using Arbiter.Net.Observers;
using Arbiter.Net.Proxy;
using Arbiter.Net.Server.Messages;
using Arbiter.Net.Server.Types;
using Arbiter.Net.Types;

namespace Arbiter.App.ViewModels.Proxy;

public partial class ProxyViewModel
{
    private static readonly TimeSpan InteractRequestTimeout = TimeSpan.FromSeconds(2);

    // Used to track interaction queries for a client to map back to the correct entity
    private readonly ConcurrentDictionary<int, ConcurrentQueue<(uint, DateTime)>> _interactRequests = [];

    private NetworkFilterRef? _debugAddEntityFilter;
    private NetworkObserverRef? _debugInteractObserver;
    private NetworkFilterRef? _debugInteractResponseFilter;

    private void AddDebugEntityFilters(DebugSettings settings)
    {
        _debugAddEntityFilter = _proxyServer.AddFilter<ServerDrawObjectsMessage>(HandleAddEntityMessage,
            $"{FilterPrefix}_Entity_ServerAddEntity", DebugFilterPriority, settings);

        _debugInteractObserver = _proxyServer.AddObserver<ClientRequestObjectInfoMessage>(OnClientInteractMessage, parameter: settings);

        _debugInteractResponseFilter =
            _proxyServer.AddFilter<ServerSystemMessage>(HandleInteractResponseMessage,
                $"{FilterPrefix}_Entity_OnServerInteractMessage", DebugFilterPriority - 10, settings);
    }

    private void RemoveDebugEntityFilters()
    {
        _debugAddEntityFilter?.Unregister();
        _debugInteractObserver?.Unregister();
        _debugInteractResponseFilter?.Unregister();
    }

    private static NetworkPacket HandleAddEntityMessage(ProxyConnection connection, ServerDrawObjectsMessage message,
        object? parameter, NetworkMessageFilterResult<ServerDrawObjectsMessage> result)
    {
        if (parameter is not DebugSettings filterSettings ||
            filterSettings is { ShowMonsterId: false, ShowNpcId: false })
        {
            return result.Passthrough();
        }

        var hasChanges = false;

        // Inject NPC IDs into the entity names
        if (filterSettings.ShowNpcId)
        {
            foreach (var entity in message.Entities)
            {
                if (entity is not ServerCreatureEntity { CreatureType: CreatureType.Mundane } npcEntity)
                {
                    continue;
                }

                var name = npcEntity.Name ?? npcEntity.CreatureType.ToString();
                npcEntity.Name = $"{name} 0x{npcEntity.Id:X4}";
                hasChanges = true;
            }
        }

        // Inject monster IDs into the entity names
        if (filterSettings.ShowMonsterId)
        {
            foreach (var entity in message.Entities)
            {
                if (entity is not ServerCreatureEntity { CreatureType: CreatureType.Monster } monsterEntity)
                {
                    continue;
                }

                var name = monsterEntity.Name ?? monsterEntity.CreatureType.ToString();

                // Need to set the creature type to Mundane to display hover name
                monsterEntity.CreatureType = CreatureType.Mundane;
                monsterEntity.Name = $"{name} 0x{monsterEntity.Id:X4}";
                hasChanges = true;
            }
        }

        return hasChanges ? result.Replace(message) : result.Passthrough();
    }

    private void OnClientInteractMessage(ProxyConnection connection, ClientRequestObjectInfoMessage message, object? parameter)
    {
        if (parameter is not DebugSettings filterSettings || filterSettings is { ShowMonsterClickId: false })
        {
            return;
        }

        // If the interaction type is Entity, queue the entity ID for later lookup when receiving the response
        if (message is { InteractionType: InteractionType.Entity, TargetId: not null })
        {
            // The entity in question should be a monster for us to queue it
            var isMonster = false;
            if (_entityStore.TryGetEntity(message.TargetId.Value, out var entity))
            {
                isMonster = entity.Flags == EntityFlags.Monster;
            }

            if (isMonster && _interactRequests.TryGetValue(connection.Id, out var queue))
            {
                queue.Enqueue((message.TargetId.Value, DateTime.Now));
            }
        }
    }

    private NetworkPacket HandleInteractResponseMessage(ProxyConnection connection, ServerSystemMessage message,
        object? parameter, NetworkMessageFilterResult<ServerSystemMessage> result)
    {
        if (parameter is not DebugSettings filterSettings || filterSettings is { ShowMonsterClickId: false })
        {
            return result.Passthrough();
        }

        var trimmedMessage = message.Message.Trim();

        // If the message is empty it's not a monster name
        if (string.IsNullOrWhiteSpace(trimmedMessage))
        {
            return result.Passthrough();
        }

        // If the message ends with a period, question mark, or exclamation mark then it's not a monster name
        if (trimmedMessage.EndsWith('.') || trimmedMessage.EndsWith('!') || trimmedMessage.EndsWith('?'))
        {
            return result.Passthrough();
        }

        // We can safely ignore any message that contains any of the following characters
        foreach (var c in trimmedMessage)
        {
            if (c is '[' or ']' or '(' or ')' or '<' or '>' or ':' or '!' or '?' or '"' or ',')
            {
                return result.Passthrough();
            }
        }

        // If there is no queue or it is empty then not pending interactions
        if (!_interactRequests.TryGetValue(connection.Id, out var queue) || queue.IsEmpty)
        {
            return result.Passthrough();
        }

        // Look for a request that has not timed out
        var foundRequest = false;
        while (queue.TryDequeue(out var pair))
        {
            var (entityId, timestamp) = pair;
            var elapsed = DateTime.Now - timestamp;

            if (elapsed > InteractRequestTimeout)
            {
                continue;
            }

            message.Message = $"{trimmedMessage} 0x{entityId:X4}";

            // Also append the sprite index if we can find the entity
            if (_entityStore.TryGetEntity(entityId, out var entity))
            {
                message.Message = $"{message.Message} - Sprite {entity.Sprite}";
            }

            foundRequest = true;
        }

        return foundRequest ? result.Replace(message) : result.Passthrough();
    }
}
