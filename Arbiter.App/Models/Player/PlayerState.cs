using Arbiter.App.Collections;

namespace Arbiter.App.Models.Player;

public sealed class PlayerState
{
    public const int MaxInventorySlots = 60;
    public const int MaxTemuairSkills = 36;
    public const int MaxMedeniaSkills = 36;
    public const int MaxWorldSkills = 18;
    public const int MaxTemuairSpells = 36;
    public const int MaxMedeniaSpells = 36;
    public const int MaxWorldSpells = 18;

    public int ConnectionId { get; }
    public long? UserId { get; set; }

    public string? Name { get; set; }

    public string? Class { get; set; }
    public int Level { get; set; }
    public int AbilityLevel { get; set; }
    public string? MapName { get; set; }
    public int? MapId { get; set; }
    public int? MapX { get; set; }
    public int? MapY { get; set; }

    public long CurrentHealth { get; set; }
    public long MaxHealth { get; set; }
    public long CurrentMana { get; set; }
    public long MaxMana { get; set; }
    public uint Gold { get; set; }

    public ISlottedCollection<InventoryItem> Inventory { get; } =
        new ConcurrentSlottedCollection<InventoryItem>(MaxInventorySlots);

    public ISlottedCollection<SkillbookItem> Skills { get; } =
        new ConcurrentSlottedCollection<SkillbookItem>(MaxTemuairSkills + MaxMedeniaSkills + MaxWorldSkills);

    public ISlottedCollection<SpellbookItem> Spells { get; } =
        new ConcurrentSlottedCollection<SpellbookItem>(MaxTemuairSpells + MaxMedeniaSpells + MaxWorldSpells);

    public PlayerState(int connectionId, string? name)
    {
        ConnectionId = connectionId;
        Name = name;
    }
}
