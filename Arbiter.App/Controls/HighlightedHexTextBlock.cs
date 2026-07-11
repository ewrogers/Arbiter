namespace Arbiter.App.Controls;

public sealed class HighlightedHexTextBlock : HighlightedByteTextBlock
{
    protected override CharacterRange GetCharacterRange(string text, int byteOffset, int byteLength)
    {
        var start = byteOffset * 3;
        var length = System.Math.Min(byteLength * 3 - 1, text.Length - start);
        return new CharacterRange(start, length);
    }

    protected override int GetByteCount(string text) => (text.Length + 1) / 3;
}
