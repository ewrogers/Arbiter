using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Arbiter.Imaging.Sprites;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace Arbiter.App.Services.Sprites;

internal sealed class AvaloniaSpriteAtlas : IDisposable
{
    private readonly SpriteAtlas _atlas;
    private readonly WriteableBitmap _bitmap;
    private readonly Dictionary<int, CroppedBitmap> _frames = [];

    public AvaloniaSpriteAtlas(SpriteAtlas atlas)
    {
        _atlas = atlas;
        var rgba = atlas.Pixels.Span;
        var bgra = new byte[rgba.Length];
        for (var index = 0; index < rgba.Length; index += 4)
        {
            bgra[index] = rgba[index + 2];
            bgra[index + 1] = rgba[index + 1];
            bgra[index + 2] = rgba[index];
            bgra[index + 3] = rgba[index + 3];
        }

        var handle = GCHandle.Alloc(bgra, GCHandleType.Pinned);
        try
        {
            _bitmap = new WriteableBitmap(
                PixelFormat.Bgra8888,
                AlphaFormat.Unpremul,
                handle.AddrOfPinnedObject(),
                new PixelSize(atlas.Width, atlas.Height),
                new Vector(96, 96),
                checked(atlas.Width * 4));
        }
        finally
        {
            handle.Free();
        }
    }

    public IImage? GetIcon(ushort icon)
    {
        return _atlas.TryResolveIcon(icon, out var frameIndex, out var region)
            ? GetFrame(frameIndex, region)
            : null;
    }

    public IImage? GetFrame(int frameIndex)
    {
        return _atlas.TryGetFrame(frameIndex, out var region)
            ? GetFrame(frameIndex, region)
            : null;
    }

    public void Dispose()
    {
        _frames.Clear();
        _bitmap.Dispose();
    }

    private IImage GetFrame(int frameIndex, SpriteAtlasRegion region)
    {
        if (_frames.TryGetValue(frameIndex, out var frame))
        {
            return frame;
        }

        frame = new CroppedBitmap
        {
            Source = _bitmap,
            SourceRect = new PixelRect(region.X, region.Y, region.Width, region.Height)
        };
        _frames.Add(frameIndex, frame);
        return frame;
    }
}
