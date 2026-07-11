using System;
using System.Collections.Generic;
using System.Linq;
using Arbiter.App.Models.Tracing.Queries;

namespace Arbiter.App.Models.Tracing;

public static class TracePayloadFormatter
{
    public const int BytesPerLine = 16;

    public static IReadOnlyList<TracePayloadLine> Format(
        ReadOnlySpan<byte> bytes,
        IReadOnlyList<TraceQueryHighlight>? highlights = null)
    {
        if (bytes.IsEmpty)
        {
            return [];
        }

        var lines = new List<TracePayloadLine>((bytes.Length + BytesPerLine - 1) / BytesPerLine);
        for (var offset = 0; offset < bytes.Length; offset += BytesPerLine)
        {
            var byteCount = Math.Min(BytesPerLine, bytes.Length - offset);
            var lineBytes = bytes.Slice(offset, byteCount);
            var lineHighlights = GetLineHighlights(highlights, offset, byteCount);

            lines.Add(new TracePayloadLine(
                string.Join(' ', lineBytes.ToArray().Select(value => value.ToString("X2"))),
                ByteDisplayFormatter.ToAscii(lineBytes),
                lineHighlights));
        }

        return lines;
    }

    private static IReadOnlyList<TraceQueryHighlight> GetLineHighlights(
        IReadOnlyList<TraceQueryHighlight>? highlights,
        int lineOffset,
        int lineLength)
    {
        if (highlights is not { Count: > 0 })
        {
            return [];
        }

        var lineEnd = lineOffset + lineLength;
        return highlights
            .Select(highlight =>
            {
                var start = Math.Max(highlight.Offset, lineOffset);
                var end = Math.Min(highlight.Offset + highlight.Length, lineEnd);
                return end > start
                    ? new TraceQueryHighlight(start - lineOffset, end - start, highlight.Source)
                    : (TraceQueryHighlight?)null;
            })
            .Where(highlight => highlight.HasValue)
            .Select(highlight => highlight!.Value)
            .ToList();
    }
}

public sealed record TracePayloadLine(
    string Hex,
    string Ascii,
    IReadOnlyList<TraceQueryHighlight> Highlights);
