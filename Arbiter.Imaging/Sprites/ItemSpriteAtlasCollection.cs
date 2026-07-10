namespace Arbiter.Imaging.Sprites;

public sealed class ItemSpriteAtlasCollection
{
    public IReadOnlyList<ItemSpriteAtlas> Atlases { get; }

    internal ItemSpriteAtlasCollection(IReadOnlyList<ItemSpriteAtlas> atlases)
    {
        Atlases = atlases;
    }

    public ItemSpriteAtlas? Find(ushort itemId) =>
        Atlases.FirstOrDefault(atlas => itemId >= atlas.FirstItemId && itemId <= atlas.LastItemId);
}
