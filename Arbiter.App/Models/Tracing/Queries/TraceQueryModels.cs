using System;
using System.Collections.Generic;

namespace Arbiter.App.Models.Tracing.Queries;

public enum TraceQueryHighlightSource
{
    Data,
    Raw
}

public readonly record struct TraceQueryHighlight(
    int Offset,
    int Length,
    TraceQueryHighlightSource Source);

public sealed record TraceQueryDiagnostic(string Message, int Position, int Length = 1);

public sealed record TraceQueryContext(
    PacketDirection Direction,
    byte Command,
    string? ClientName,
    byte? Sequence,
    ReadOnlyMemory<byte> Data,
    ReadOnlyMemory<byte> Raw);

public sealed record TraceQueryMatch(bool IsMatch, IReadOnlyList<TraceQueryHighlight> Highlights)
{
    public static TraceQueryMatch NoMatch { get; } = new(false, []);
    public static TraceQueryMatch MatchWithoutHighlights { get; } = new(true, []);
}

public sealed record TraceQueryParseResult(TraceQuery? Query, TraceQueryDiagnostic? Diagnostic)
{
    public bool IsValid => Query is not null && Diagnostic is null;
}
