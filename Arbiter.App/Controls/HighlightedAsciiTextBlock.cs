namespace Arbiter.App.Controls;

public sealed class HighlightedAsciiTextBlock : HighlightedByteTextBlock
{
    protected override CharacterRange GetCharacterRange(string text, int byteOffset, int byteLength)
    {
        var length = System.Math.Min(byteLength, text.Length - byteOffset);
        return new CharacterRange(byteOffset, length);
    }

    protected override int GetByteCount(string text) => text.Length;
}
