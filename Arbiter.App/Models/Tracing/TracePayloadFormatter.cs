using System;
using System.Collections.Generic;
using System.Linq;
using Arbiter.App.Models.Tracing.Queries;

namespace Arbiter.App.Models.Tracing;

public static class TracePayloadFormatter
{
    public const int MinimumBytesPerLine = 16;
    public const int BytesPerLineStep = 8;
    public const double AsciiDividerWidth = 9;

    public static IReadOnlyList<TracePayloadLine> Format(
        ReadOnlySpan<byte> bytes,
        IReadOnlyList<TraceQueryHighlight>? highlights = null,
        int bytesPerLine = MinimumBytesPerLine)
    {
        if (bytes.IsEmpty)
        {
            return [];
        }

        bytesPerLine = NormalizeBytesPerLine(bytesPerLine);
        var lines = new List<TracePayloadLine>((bytes.Length + bytesPerLine - 1) / bytesPerLine);
        for (var offset = 0; offset < bytes.Length; offset += bytesPerLine)
        {
            var byteCount = Math.Min(bytesPerLine, bytes.Length - offset);
            var lineBytes = bytes.Slice(offset, byteCount);
            var lineHighlights = GetLineHighlights(highlights, offset, byteCount);

            lines.Add(new TracePayloadLine(
                string.Join(' ', lineBytes.ToArray().Select(value => value.ToString("X2"))),
                ByteDisplayFormatter.ToAscii(lineBytes),
                lineHighlights));
        }

        return lines;
    }

    public static int CalculateBytesPerLine(double availableWidth, double characterWidth)
    {
        if (!double.IsFinite(availableWidth) || !double.IsFinite(characterWidth) || characterWidth <= 0)
        {
            return MinimumBytesPerLine;
        }

        var capacity = (int)Math.Floor(
            (availableWidth - AsciiDividerWidth + characterWidth) / (characterWidth * 4));
        return NormalizeBytesPerLine(capacity);
    }

    public static double CalculateHexColumnWidth(int bytesPerLine, double characterWidth)
    {
        bytesPerLine = NormalizeBytesPerLine(bytesPerLine);
        return Math.Max(0, (bytesPerLine * 3 - 1) * characterWidth);
    }

    private static int NormalizeBytesPerLine(int bytesPerLine)
    {
        var normalized = bytesPerLine / BytesPerLineStep * BytesPerLineStep;
        return Math.Max(MinimumBytesPerLine, normalized);
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
