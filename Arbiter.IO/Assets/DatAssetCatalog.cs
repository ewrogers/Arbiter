using Arbiter.IO.Archives;
using System.Diagnostics.CodeAnalysis;

namespace Arbiter.IO.Assets;

public sealed class DatAssetCatalog
{
    private readonly Dictionary<string, DatAsset> _assets;
    private readonly Dictionary<string, IReadOnlyList<DatAsset>> _variants;

    public string DirectoryPath { get; }
    public IReadOnlyCollection<string> Names => _assets.Keys;
    public IReadOnlyList<DatArchive> Archives { get; }

    private DatAssetCatalog(
        string directoryPath,
        IReadOnlyList<DatArchive> archives,
        Dictionary<string, DatAsset> assets,
        Dictionary<string, IReadOnlyList<DatAsset>> variants)
    {
        DirectoryPath = directoryPath;
        Archives = archives;
        _assets = assets;
        _variants = variants;
    }

    public static DatAssetCatalog Load(string directoryPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directoryPath);

        var fullPath = Path.GetFullPath(directoryPath);
        var archivePaths = Directory.EnumerateFiles(fullPath)
            .Where(path => string.Equals(Path.GetExtension(path), ".dat", StringComparison.OrdinalIgnoreCase))
            .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var archives = archivePaths.Select(DatArchive.Open).ToArray();
        var assets = new Dictionary<string, DatAsset>(StringComparer.OrdinalIgnoreCase);
        var variantLists = new Dictionary<string, List<DatAsset>>(StringComparer.OrdinalIgnoreCase);

        foreach (var archive in archives)
        {
            foreach (var entry in archive.Entries)
            {
                var asset = new DatAsset(archive, entry);
                assets[entry.Name] = asset;

                if (!variantLists.TryGetValue(entry.Name, out var variants))
                {
                    variants = [];
                    variantLists.Add(entry.Name, variants);
                }

                variants.Add(asset);
            }
        }

        var readOnlyVariants = variantLists.ToDictionary(
            pair => pair.Key,
            pair => (IReadOnlyList<DatAsset>)pair.Value,
            StringComparer.OrdinalIgnoreCase);

        return new DatAssetCatalog(fullPath, archives, assets, readOnlyVariants);
    }

    public bool TryGet(string name, [NotNullWhen(true)] out DatAsset? asset)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return _assets.TryGetValue(name, out asset);
    }

    public IReadOnlyList<DatAsset> GetAll(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return _variants.GetValueOrDefault(name) ?? [];
    }
}
