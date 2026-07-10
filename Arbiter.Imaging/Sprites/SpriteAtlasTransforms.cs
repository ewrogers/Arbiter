namespace Arbiter.Imaging.Sprites;

public static class SpriteAtlasTransforms
{
    public static SpriteAtlas Grayscale(SpriteAtlas atlas)
    {
        ArgumentNullException.ThrowIfNull(atlas);
        var pixels = atlas.Pixels.ToArray();
        for (var index = 0; index < pixels.Length; index += 4)
        {
            var luminance = (pixels[index] * 77 + pixels[index + 1] * 150 + pixels[index + 2] * 29 + 128) / 256;
            pixels[index] = (byte)luminance;
            pixels[index + 1] = (byte)luminance;
            pixels[index + 2] = (byte)luminance;
        }

        return atlas.WithPixels(pixels);
    }

    public static SpriteAtlas Tint(SpriteAtlas atlas, byte red, byte green, byte blue)
    {
        ArgumentNullException.ThrowIfNull(atlas);
        var pixels = atlas.Pixels.ToArray();
        for (var index = 0; index < pixels.Length; index += 4)
        {
            pixels[index] = Multiply(pixels[index], red);
            pixels[index + 1] = Multiply(pixels[index + 1], green);
            pixels[index + 2] = Multiply(pixels[index + 2], blue);
        }

        return atlas.WithPixels(pixels);
    }

    private static byte Multiply(byte left, byte right) => (byte)((left * right + 127) / 255);
}
