using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Arbiter.App.Models.Entities;

namespace Arbiter.App.Services.Entities;

public sealed class EntityStore : IEntityStore
{
    private readonly Lock _lock = new();
    private readonly Dictionary<long, GameEntity> _entities = [];
    private readonly Dictionary<string, GameEntity> _playerEntities = new (StringComparer.OrdinalIgnoreCase);
    
    public event Action<GameEntity>? EntityAdded;
    public event Action<GameEntity>? EntityUpdated;
    public event Action<GameEntity>? EntityRemoved;

    public IEnumerable<GameEntity> Entities
    {
        get
        {
            using var _ = _lock.EnterScope();
            return _entities.Values.ToList();
        }
    }

    public bool IsEmpty => Count == 0;

    public int Count
    {
        get
        {
            using var _ = _lock.EnterScope();
            return _entities.Count;
        }
    }

    public IReadOnlyList<GameEntity> GetNearbyEntities(int mapId, int x, int y, int distance,
        EntityFlags flags = EntityFlags.None)
    {
        using var _ = _lock.EnterScope();
        var matches = _entities.Values
            .Where(e => flags == EntityFlags.None || e.Flags.HasFlag(flags) && e.MapId == mapId &&
                Math.Abs(e.X - x) <= distance && Math.Abs(e.Y - y) <= distance)
            .OrderBy(e => Math.Abs(e.X - x) + Math.Abs(e.Y - y));

        return matches.ToList();
    }

    public bool TryGetEntity(long id, out GameEntity entity)
    {
        using var _ = _lock.EnterScope();
        return _entities.TryGetValue(id, out entity);
    }

    public bool TrySetEntityName(long id, string name)
    {
        using var _ = _lock.EnterScope();
        if (!_entities.TryGetValue(id, out var entity))
        {
            return false;
        }

        if (string.Equals(name, entity.Name, StringComparison.Ordinal))
        {
            return false;
        }

        entity = entity with { Name = name };
        AddOrUpdateEntity(entity, out var _);
        return true;
    }

    public bool TrySetEntityLocation(long id, int x, int y)
    {
        using var _ = _lock.EnterScope();
        if (!_entities.TryGetValue(id, out var entity))
        {
            return false;
        }

        if (entity.X == x && entity.Y == y)
        {
            return false;
        }

        entity = entity with { X = x, Y = y };
        AddOrUpdateEntity(entity, out var _);
        return true;
    }

    public void AddOrUpdateEntity(GameEntity entity, out bool wasUpdated)
    {
        wasUpdated = false;

        using var _ = _lock.EnterScope();

        // Remove existing player entity with same name to avoid duplicates (relogging gives a new ID)
        GameEntity? existingPlayer = null;
        if (entity.Flags.HasFlag(EntityFlags.Player))
        {
            if (_playerEntities.TryGetValue(entity.Name ?? string.Empty, out var player))
            {
                existingPlayer = player;
            }

            if (existingPlayer is not null && existingPlayer.Value.Id != entity.Id)
            {
                _playerEntities.Remove(existingPlayer.Value.Name ?? string.Empty);
                RemoveEntity(existingPlayer.Value.Id, out var _);
            }
        }

        // If adding a new entity, add it to the dictionary
        if (_entities.TryAdd(entity.Id, entity))
        {
            // If adding a player entity, add it to the player dictionary
            if (entity.Flags.HasFlag(EntityFlags.Player) && !string.IsNullOrWhiteSpace(entity.Name))
            {
                _playerEntities[entity.Name] = entity;
            }

            OnEntityAdded(entity);
            return;
        }

        if (!_entities.TryGetValue(entity.Id, out var existingEntity))
        {
            return;
        }

        // Preserve existing map data values
        var updatedEntity = existingEntity with
        {
            Name = !string.IsNullOrWhiteSpace(entity.Name) ? entity.Name : existingEntity.Name,
            Sprite = entity.Sprite ?? existingEntity.Sprite,
            Color = entity.Color ?? existingEntity.Color,
            MapId = entity.MapId ?? existingEntity.MapId,
            MapName = entity.MapName ?? existingEntity.MapName,
            X = entity.X,
            Y = entity.Y
        };

        _entities[entity.Id] = updatedEntity;
        OnEntityUpdated(updatedEntity);
        wasUpdated = true;
    }

    public bool RemoveEntity(long id, out GameEntity entity)
    {
        using var _ = _lock.EnterScope();
        
        var wasRemoved = _entities.Remove(id, out entity);
        if (wasRemoved)
        {
            OnEntityRemoved(entity);
        }

        return wasRemoved;
    }

    private void OnEntityAdded(GameEntity entity) => EntityAdded?.Invoke(entity);
    private void OnEntityUpdated(GameEntity entity) => EntityUpdated?.Invoke(entity);
    private void OnEntityRemoved(GameEntity entity) => EntityRemoved?.Invoke(entity);
}
