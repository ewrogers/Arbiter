using System;
using System.Collections.Concurrent;
using Arbiter.App.Models.Entities;
using Arbiter.Net.Client.Messages;
using Arbiter.Net.Proxy;
using Arbiter.Net.Server.Messages;
using Arbiter.Net.Server.Types;
using Arbiter.Net.Types;

namespace Arbiter.App.ViewModels.Entities;

public partial class EntityManagerViewModel
{
    private static readonly TimeSpan InteractRequestTimeout = TimeSpan.FromSeconds(2);

    // Used to track interaction queries for a client to map back to the correct entity
    private readonly ConcurrentDictionary<int, ConcurrentQueue<(uint, DateTime)>> _interactRequests = [];

    private void AddObservers(ProxyServer proxyServer)
    {
        const int observerPriority = int.MinValue;
        
        proxyServer.AddObserver<ClientRequestObjectInfoMessage>(OnClientInteractMessage, observerPriority);

        proxyServer.AddObserver<ServerDrawObjectsMessage>(OnAddEntityMessage, observerPriority);
        proxyServer.AddObserver<ServerRemoveObjectsMessage>(OnRemoveEntityMessage, observerPriority);
        proxyServer.AddObserver<ServerDrawHumanObjectsMessage>(OnShowUserMessage, observerPriority);
        proxyServer.AddObserver<ServerObjectInfoMessage>(OnUserProfileMessage, observerPriority);
        proxyServer.AddObserver<ServerSayMessage>(OnPublicMessage, observerPriority);
        proxyServer.AddObserver<ServerPursuitMessage>(OnShowDialogMessage, observerPriority);
        proxyServer.AddObserver<ServerScreenMenuMessage>(OnShowDialogMenuMessage, observerPriority);
        proxyServer.AddObserver<ServerMoveObjectMessage>(OnEntityWalkMessage, observerPriority);
        proxyServer.AddObserver<ServerMoveMessage>(OnSelfWalkMessage, observerPriority);
        proxyServer.AddObserver<ServerUserPositionMessage>(OnServerMapLocationMessage, observerPriority);
        proxyServer.AddObserver<ServerSystemMessage>(OnInteractResponse, observerPriority);

        proxyServer.ClientConnected += OnClientConnected;
        proxyServer.ClientDisconnected += OnClientDisconnected;
    }

    private void OnClientConnected(object? sender, ProxyConnectionEventArgs e)
        => _interactRequests.AddOrUpdate(e.Connection.Id, [], (_, _) => []);

    private void OnClientDisconnected(object? sender, ProxyConnectionEventArgs e)
        => _interactRequests.TryRemove(e.Connection.Id, out _);

    private void QueueInteractionRequest(int connectionId, uint entityId)
    {
        if (_interactRequests.TryGetValue(connectionId, out var queue))
        {
            queue.Enqueue((entityId, DateTime.Now));
        }
    }

    private void OnAddEntityMessage(ProxyConnection connection, ServerDrawObjectsMessage message, object? parameter)
    {
        foreach (var entity in message.Entities)
        {
            var flags = entity switch
            {
                ServerCreatureEntity creature => creature.CreatureType == CreatureType.Mundane
                    ? EntityFlags.Mundane
                    : EntityFlags.Monster,
                _ => EntityFlags.Item
            };

            var name = entity switch
            {
                ServerCreatureEntity creature => creature.Name,
                _ => null
            };

            // Try to get the player so we can get map context
            _playerService.TryGetState(connection.Id, out var player);

            var gameEntity = new GameEntity
            {
                Flags = flags,
                Id = entity.Id,
                Name = name,
                Sprite = entity.Sprite,
                Color = entity is ServerItemEntity item ? (byte)item.Color : null,
                MapId = player?.MapId,
                MapName = player?.MapName,
                X = entity.X,
                Y = entity.Y,
            };

            _entityStore.AddOrUpdateEntity(gameEntity, out _);
        }
    }
    
    private void OnRemoveEntityMessage(ProxyConnection connection, ServerRemoveObjectsMessage message, object? parameter)
    {
        // Only remove monster and item entities since they are more ephemeral than player/npc entities
        if (_entityStore.TryGetEntity(message.EntityId, out var existing) && existing.Flags == EntityFlags.Monster ||
            existing.Flags == EntityFlags.Item)
        {
            _entityStore.RemoveEntity(message.EntityId, out _);
        }
    }

    private void OnShowUserMessage(ProxyConnection connection, ServerDrawHumanObjectsMessage message, object? parameter)
    {
        // Try to get the player so we can get map context
        _playerService.TryGetState(connection.Id, out var player);

        var entity = new GameEntity
        {
            Flags = EntityFlags.Player,
            Id = message.EntityId,
            Name = message.Name,
            Sprite = message.BodySprite.HasValue
                ? (ushort)message.BodySprite.Value
                : message.MonsterSprite ?? 0,
            MapId = player?.MapId,
            MapName = player?.MapName,
            X = message.X,
            Y = message.Y
        };

        _entityStore.AddOrUpdateEntity(entity, out _);
    }

    private void OnUserProfileMessage(ProxyConnection connection, ServerObjectInfoMessage message, object? parameter)
    {
        // Try to get the player so we can get map context
        _playerService.TryGetState(connection.Id, out var player);

        var entity = new GameEntity
        {
            Flags = EntityFlags.Player,
            Id = message.EntityId,
            Name = message.Name,
            MapId = player?.MapId,
            MapName = player?.MapName
        };

        _entityStore.AddOrUpdateEntity(entity, out _);
    }

    private void OnPublicMessage(ProxyConnection connection, ServerSayMessage message, object? parameter)
    {
        // Ignore world shouts and spell chants
        if (message.SenderId == 0 || message.MessageType == PublicMessageType.Chant)
        {
            return;
        }

        // Try to get the player so we can get map context
        _playerService.TryGetState(connection.Id, out var player);

        // Assume it might be a player ghost
        var entityFlags = EntityFlags.Player;
        var entitySprite = (ushort)BodySprite.MaleGhost;

        // Try to get the existing entity, this should rule out monsters/npcs that talk
        if (_entityStore.TryGetEntity(message.SenderId, out var existing))
        {
            if (!existing.Flags.HasFlag(EntityFlags.Player))
            {
                return;
            }
            
            entityFlags = existing.Flags;
            entitySprite = existing.Sprite ?? entitySprite;
        }

        // The name should come before the symbol, so split on the first symbol (chat or shout)
        var sections = message.Message.Split(':', '!',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (sections.Length < 2 || sections[0].StartsWith('[') || sections[0].EndsWith(']'))
        {
            return;
        }

        var senderName = sections[0];
        var entity = new GameEntity
        {
            Flags = entityFlags,
            Id = message.SenderId,
            Sprite = entitySprite,
            Name = senderName,
            MapId = player?.MapId,
            MapName = player?.MapName
        };

        _entityStore.AddOrUpdateEntity(entity, out _);
    }

    private void OnShowDialogMessage(ProxyConnection connection, ServerPursuitMessage message, object? parameter)
    {
        // Skip on invalid entity ID
        if (message.EntityId is null or 0)
        {
            return;
        }

        var wasFound = _entityStore.TryGetEntity(message.EntityId.Value, out _);
        if (wasFound)
        {
            return;
        }

        var flags = message.EntityType switch
        {
            EntityTypeFlags.Creature => EntityFlags.Mundane,
            EntityTypeFlags.Item => EntityFlags.Item,
            EntityTypeFlags.Reactor => EntityFlags.Reactor,
            _ => EntityFlags.Monster
        };

        // Try to get the player so we can get map context
        _playerService.TryGetState(connection.Id, out var player);

        var entity = new GameEntity
        {
            Flags = flags,
            Id = message.EntityId.Value,
            Name = !string.IsNullOrWhiteSpace(message.Name) ? message.Name : message.EntityType.ToString(),
            Sprite = message.Sprite ?? 0,
            MapId = player?.MapId,
            MapName = player?.MapName,
            // Reactors should assume the player's location when encountering
            X = flags.HasFlag(EntityFlags.Reactor) ? player?.MapX ?? 0 : 0,
            Y = flags.HasFlag(EntityFlags.Reactor) ? player?.MapY ?? 0 : 0,
        };

        _entityStore.AddOrUpdateEntity(entity, out _);
    }

    private void OnShowDialogMenuMessage(ProxyConnection connection, ServerScreenMenuMessage message, object? parameter)
    {
        // Skip on invalid entity ID
        if (message.EntityId is null or 0)
        {
            return;
        }

        var wasFound = _entityStore.TryGetEntity(message.EntityId.Value, out _);
        if (wasFound)
        {
            return;
        }

        var flags = message.EntityType switch
        {
            EntityTypeFlags.Creature => EntityFlags.Mundane,
            EntityTypeFlags.Item => EntityFlags.Item,
            EntityTypeFlags.Reactor => EntityFlags.Reactor,
            _ => EntityFlags.Monster
        };

        // Try to get the player so we can get map context
        _playerService.TryGetState(connection.Id, out var player);

        var entity = new GameEntity
        {
            Flags = flags,
            Id = message.EntityId.Value,
            Name = !string.IsNullOrWhiteSpace(message.Name) ? message.Name : message.EntityType.ToString(),
            Sprite = message.Sprite ?? 0,
            MapId = player?.MapId,
            MapName = player?.MapName
        };

        _entityStore.AddOrUpdateEntity(entity, out _);
    }

    private void OnEntityWalkMessage(ProxyConnection connection, ServerMoveObjectMessage message, object? parameter)
    {
        var existing = _entityStore.TryGetEntity(message.EntityId, out _);
        if (!existing)
        {
            return;
        }

        var newX = message.Direction switch
        {
            WorldDirection.Left => Math.Max(0, message.OriginX - 1),
            WorldDirection.Right => message.OriginX + 1,
            _ => message.OriginX
        };

        var newY = message.Direction switch
        {
            WorldDirection.Up => Math.Max(0, message.OriginY - 1),
            WorldDirection.Down => message.OriginY + 1,
            _ => message.OriginY
        };

        _entityStore.TrySetEntityLocation(message.EntityId, newX, newY);
    }

    private void OnSelfWalkMessage(ProxyConnection connection, ServerMoveMessage message, object? parameter)
    {
        // If no active player, ignore
        if (!_playerService.TryGetState(connection.Id, out var player) || player.UserId is null)
        {
            return;
        }

        if (!_entityStore.TryGetEntity(player.UserId.Value, out var selfEntity))
        {
            return;
        }

        var newEntity = selfEntity with
        {
            Name = player.Name,
            MapId = player.MapId,
            MapName = player.MapName,
            X = message.Direction switch
            {
                WorldDirection.Left => Math.Max(0, message.PreviousX - 1),
                WorldDirection.Right => message.PreviousX + 1,
                _ => message.PreviousX
            },
            Y = message.Direction switch
            {
                WorldDirection.Up => Math.Max(0, message.PreviousY - 1),
                WorldDirection.Down => message.PreviousX + 1,
                _ => message.PreviousY
            }
        };
        _entityStore.AddOrUpdateEntity(newEntity, out _);

        if (SelectedClient is not null)
        {
            _filterDebouncer.Execute(ReconcileFilter);
        }
    }

    private void OnServerMapLocationMessage(ProxyConnection connection, ServerUserPositionMessage message, object? parameter)
    {
        if (SelectedClient is not null && FilterMode is not EntityFilterMode.All)
        {
            _filterDebouncer.Execute(ReconcileFilter);
        }
    }

    private void OnClientInteractMessage(ProxyConnection connection, ClientRequestObjectInfoMessage message,
        object? parameter)
    {
        // Ensure there is a valid entity interaction request
        if (message.InteractionType != InteractionType.Entity || !message.TargetId.HasValue)
        {
            return;
        }

        // Determine which entity type we are interacting with
        var entityFlags = EntityFlags.None;
        if (_entityStore.TryGetEntity(message.TargetId.Value, out var entity))
        {
            entityFlags = entity.Flags;
        }

        // If it's a player, npc, or item we can ignore it
        if (entityFlags.HasFlag(EntityFlags.Player) || entityFlags.HasFlag(EntityFlags.Mundane) ||
            entityFlags.HasFlag(EntityFlags.Item))
        {
            return;
        }

        QueueInteractionRequest(connection.Id, message.TargetId.Value);
    }

    private void OnInteractResponse(ProxyConnection connection, ServerSystemMessage message, object? parameter)
    {
        var trimmedMessage = message.Message.Trim();

        // If the message is empty it's not a monster name
        if (string.IsNullOrWhiteSpace(trimmedMessage))
        {
            return;
        }

        // If the message ends with a period, question mark, or exclamation mark then it's not a monster name
        if (trimmedMessage.EndsWith('.') || trimmedMessage.EndsWith('!') || trimmedMessage.EndsWith('?'))
        {
            return;
        }

        // We can safely ignore any message that contains any of the following characters
        foreach (var c in trimmedMessage)
        {
            if (c is '[' or ']' or '(' or ')' or '<' or '>' or ':' or '!' or '?' or '"' or ',')
            {
                return;
            }
        }

        // If there is no queue or it is empty then not pending interactions
        if (!_interactRequests.TryGetValue(connection.Id, out var queue) || queue.IsEmpty)
        {
            return;
        }

        // Look for a request that has not timed out
        var now = DateTime.Now;
        while (queue.TryDequeue(out var pair))
        {
            var (entityId, timestamp) = pair;
            var elapsed = now - timestamp;

            if (elapsed > InteractRequestTimeout)
            {
                continue;
            }

            _entityStore.TrySetEntityName(entityId, trimmedMessage);
        }
    }
}
