namespace Arbiter.Imaging.Formats;

internal sealed class DyeTable
{
    private readonly Dictionary<byte, IReadOnlyList<RgbColor>> _entries;

    private DyeTable(Dictionary<byte, IReadOnlyList<RgbColor>> entries)
    {
        _entries = entries;
    }

    public static DyeTable Empty { get; } = new([]);

    public static DyeTable Parse(Stream stream)
    {
        using var reader = new StreamReader(stream, leaveOpen: true);
        var entries = new Dictionary<byte, IReadOnlyList<RgbColor>>();
        if (!int.TryParse(reader.ReadLine()?.Trim(), out var colorsPerEntry) || colorsPerEntry <= 0)
        {
            return new DyeTable(entries);
        }

        while (reader.ReadLine() is { } indexLine)
        {
            if (!byte.TryParse(indexLine.Trim(), out var colorIndex))
            {
                continue;
            }

            var colors = new List<RgbColor>(PaletteDye.ColorCount);
            var valid = true;
            for (var index = 0; index < colorsPerEntry; index++)
            {
                var line = reader.ReadLine();
                if (line is null || !TryParseColor(line, out var color))
                {
                    valid = false;
                    break;
                }

                if (index < PaletteDye.ColorCount)
                {
                    colors.Add(color);
                }
            }

            if (!valid)
            {
                break;
            }

            if (colors.Count == PaletteDye.ColorCount)
            {
                entries[colorIndex] = colors;
            }
        }

        return new DyeTable(entries);
    }

    public IReadOnlyList<RgbColor>? GetColors(byte colorIndex) => _entries.GetValueOrDefault(colorIndex);

    private static bool TryParseColor(string text, out RgbColor color)
    {
        color = default;
        var channels = text.Split(',', StringSplitOptions.TrimEntries);
        if (channels.Length != 3 ||
            !byte.TryParse(channels[0], out var red) ||
            !byte.TryParse(channels[1], out var green) ||
            !byte.TryParse(channels[2], out var blue))
        {
            return false;
        }

        color = new RgbColor(red, green, blue);
        return true;
    }
}
