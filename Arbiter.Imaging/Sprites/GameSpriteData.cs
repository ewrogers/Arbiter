namespace Arbiter.Imaging.Sprites;

public sealed class GameSpriteData
{
    public SpriteAtlas? Skills { get; }
    public SpriteAtlas? SkillsOnCooldown { get; }
    public SpriteAtlas? Spells { get; }
    public SpriteAtlas? SpellsOnCooldown { get; }
    public ItemSpriteAtlasCollection? Items { get; }
    public IReadOnlyList<GameSpriteLoadIssue> Issues { get; }

    internal GameSpriteData(
        SpriteAtlas? skills,
        SpriteAtlas? skillsOnCooldown,
        SpriteAtlas? spells,
        SpriteAtlas? spellsOnCooldown,
        ItemSpriteAtlasCollection? items,
        IReadOnlyList<GameSpriteLoadIssue> issues)
    {
        Skills = skills;
        SkillsOnCooldown = skillsOnCooldown;
        Spells = spells;
        SpellsOnCooldown = spellsOnCooldown;
        Items = items;
        Issues = issues;
    }
}
