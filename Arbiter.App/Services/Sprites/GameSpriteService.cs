using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arbiter.Imaging.Sprites;
using Avalonia.Media;
using Avalonia.Threading;
using Microsoft.Extensions.Logging;

namespace Arbiter.App.Services.Sprites;

public sealed class GameSpriteService : IGameSpriteService, IDisposable
{
    private readonly ILogger<GameSpriteService> _logger;
    private readonly SemaphoreSlim _loadGate = new(1, 1);
    private readonly Dictionary<ushort, AvaloniaSpriteAtlas> _creatureAtlases = [];
    private readonly HashSet<ushort> _failedCreatureSprites = [];
    private readonly Dictionary<(uint FirstItemId, byte Color), AvaloniaSpriteAtlas> _itemAtlases = [];
    private readonly HashSet<(uint FirstItemId, byte Color)> _failedItemAtlases = [];

    private GameSpriteData? _data;
    private AvaloniaSpriteAtlas? _skills;
    private AvaloniaSpriteAtlas? _skillsOnCooldown;
    private AvaloniaSpriteAtlas? _spells;
    private AvaloniaSpriteAtlas? _spellsOnCooldown;
    private CreatureSpriteLoader? _creatures;
    private string? _loadedDirectory;

    public GameSpriteService(ILogger<GameSpriteService> logger)
    {
        _logger = logger;
    }

    public event EventHandler? SpritesChanged;

    public async Task LoadAsync(string clientExecutablePath, CancellationToken cancellationToken = default)
    {
        var directory = Path.GetDirectoryName(clientExecutablePath);
        await _loadGate.WaitAsync(cancellationToken);
        try
        {
            if (string.Equals(directory, _loadedDirectory, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            GameSpriteData? data = null;
            Exception? loadException = null;
            if (string.IsNullOrWhiteSpace(directory))
            {
                loadException = new DirectoryNotFoundException("The configured client path does not have a directory.");
            }
            else
            {
                try
                {
                    data = await Task.Run(() => GameSpriteDataLoader.Load(directory), cancellationToken);
                }
                catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or
                                                   InvalidDataException or OverflowException)
                {
                    loadException = exception;
                }
            }

            await Dispatcher.UIThread.InvokeAsync(() => ApplyLoadedData(directory, data, loadException));
        }
        finally
        {
            _loadGate.Release();
        }
    }

    public IImage? GetItem(ushort sprite, byte color)
    {
        var itemAtlas = _data?.Items?.Find(sprite);
        if (itemAtlas is null)
        {
            return null;
        }

        var key = (itemAtlas.FirstItemId, color);
        if (_failedItemAtlases.Contains(key))
        {
            return null;
        }

        if (!_itemAtlases.TryGetValue(key, out var atlas))
        {
            try
            {
                var source = itemAtlas.BuildColorVariant(color);
                atlas = new AvaloniaSpriteAtlas(source);
                _itemAtlases.Add(key, atlas);
            }
            catch (Exception exception) when (exception is ArgumentException or InvalidDataException or OverflowException)
            {
                _failedItemAtlases.Add(key);
                _logger.LogWarning(
                    exception,
                    "Failed to build item sprite atlas starting at {FirstItemId} with color {Color}",
                    itemAtlas.FirstItemId,
                    color);
                return null;
            }
        }

        var frameIndex = checked((int)(sprite - itemAtlas.FirstItemId));
        return atlas.GetFrame(frameIndex);
    }

    public IImage? GetCreature(ushort sprite)
    {
        if (_creatures is null || sprite == 0 || _failedCreatureSprites.Contains(sprite))
        {
            return null;
        }

        if (!_creatureAtlases.TryGetValue(sprite, out var atlas))
        {
            try
            {
                var source = _creatures.LoadPreview(sprite);
                if (source is null)
                {
                    _failedCreatureSprites.Add(sprite);
                    return null;
                }

                atlas = new AvaloniaSpriteAtlas(source);
                _creatureAtlases.Add(sprite, atlas);
            }
            catch (Exception exception) when (exception is ArgumentException or IOException or
                                               UnauthorizedAccessException or InvalidDataException or OverflowException)
            {
                _failedCreatureSprites.Add(sprite);
                _logger.LogWarning(exception, "Failed to build creature sprite {Sprite}", sprite);
                return null;
            }
        }

        return atlas.GetFrame(0);
    }

    public IImage? GetSkill(ushort sprite, bool isOnCooldown) =>
        (isOnCooldown ? _skillsOnCooldown : _skills)?.GetIcon(sprite);

    public IImage? GetSpell(ushort sprite, bool isOnCooldown) =>
        (isOnCooldown ? _spellsOnCooldown : _spells)?.GetIcon(sprite);

    public void Dispose()
    {
        DisposeAtlases();
        _loadGate.Dispose();
    }

    private void ApplyLoadedData(string? directory, GameSpriteData? data, Exception? loadException)
    {
        var oldSkills = _skills;
        var oldSkillsOnCooldown = _skillsOnCooldown;
        var oldSpells = _spells;
        var oldSpellsOnCooldown = _spellsOnCooldown;
        var oldCreatures = _creatureAtlases.Values.ToArray();
        var oldItems = _itemAtlases.Values.ToArray();

        _data = data;
        _skills = CreateAtlas(data?.Skills, "skills");
        _skillsOnCooldown = CreateAtlas(data?.SkillsOnCooldown, "skills cooldown");
        _spells = CreateAtlas(data?.Spells, "spells");
        _spellsOnCooldown = CreateAtlas(data?.SpellsOnCooldown, "spells cooldown");
        _creatures = data?.Creatures;
        _creatureAtlases.Clear();
        _failedCreatureSprites.Clear();
        _itemAtlases.Clear();
        _failedItemAtlases.Clear();
        _loadedDirectory = directory;

        if (loadException is not null)
        {
            _logger.LogWarning(loadException, "Failed to load game sprites from {Directory}", directory);
        }

        if (data is not null)
        {
            foreach (var issue in data.Issues)
            {
                _logger.LogWarning(issue.Exception, "Failed to load {Category} sprites", issue.Category);
            }
        }

        try
        {
            SpritesChanged?.Invoke(this, EventArgs.Empty);
        }
        finally
        {
            oldSkills?.Dispose();
            oldSkillsOnCooldown?.Dispose();
            oldSpells?.Dispose();
            oldSpellsOnCooldown?.Dispose();
            foreach (var atlas in oldCreatures)
            {
                atlas.Dispose();
            }

            foreach (var atlas in oldItems)
            {
                atlas.Dispose();
            }
        }
    }

    private AvaloniaSpriteAtlas? CreateAtlas(SpriteAtlas? atlas, string category)
    {
        if (atlas is null)
        {
            return null;
        }

        try
        {
            return new AvaloniaSpriteAtlas(atlas);
        }
        catch (Exception exception) when (exception is ArgumentException or OverflowException)
        {
            _logger.LogWarning(exception, "Failed to create the Avalonia {Category} atlas", category);
            return null;
        }
    }

    private void DisposeAtlases()
    {
        _skills?.Dispose();
        _skillsOnCooldown?.Dispose();
        _spells?.Dispose();
        _spellsOnCooldown?.Dispose();
        foreach (var atlas in _creatureAtlases.Values)
        {
            atlas.Dispose();
        }

        foreach (var atlas in _itemAtlases.Values)
        {
            atlas.Dispose();
        }

        _creatureAtlases.Clear();
        _itemAtlases.Clear();
    }
}
