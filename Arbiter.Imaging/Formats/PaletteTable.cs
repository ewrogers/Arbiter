namespace Arbiter.Imaging.Formats;

internal sealed class PaletteTable
{
    private readonly Dictionary<uint, uint> _ranges = [];
    private readonly Dictionary<uint, uint> _overrides = [];

    public void Merge(Stream stream)
    {
        using var reader = new StreamReader(stream, leaveOpen: true);
        while (reader.ReadLine() is { } line)
        {
            var values = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            if (values.Length == 2 &&
                uint.TryParse(values[0], out var itemId) &&
                uint.TryParse(values[1], out var paletteId))
            {
                _overrides[itemId] = paletteId;
                continue;
            }

            if (values.Length != 3 ||
                !uint.TryParse(values[0], out var minimum) ||
                !uint.TryParse(values[1], out var maximum) ||
                !int.TryParse(values[2], out var rangePaletteId) ||
                rangePaletteId < 0)
            {
                continue;
            }

            for (var id = minimum; id <= maximum; id++)
            {
                _ranges[id] = (uint)rangePaletteId;
                if (id == uint.MaxValue)
                {
                    break;
                }
            }
        }
    }

    public uint GetPaletteId(uint itemId) =>
        _overrides.GetValueOrDefault(itemId, _ranges.GetValueOrDefault(itemId));
}
