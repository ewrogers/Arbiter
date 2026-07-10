using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Arbiter.App.Models.Tracing.Queries;

public sealed class TraceQuery
{
    private readonly HashSet<byte>? _clientCommands;
    private readonly HashSet<byte>? _serverCommands;
    private readonly IReadOnlyList<ITraceQueryClause> _clauses;
    private readonly TraceQuery? _left;
    private readonly TraceQuery? _right;
    private readonly TraceQueryOperation _operation;

    internal TraceQuery(
        HashSet<byte>? clientCommands,
        HashSet<byte>? serverCommands,
        IReadOnlyList<ITraceQueryClause> clauses)
    {
        _clientCommands = clientCommands;
        _serverCommands = serverCommands;
        _clauses = clauses;
        _operation = TraceQueryOperation.Predicate;
    }

    private TraceQuery(TraceQueryOperation operation, TraceQuery left, TraceQuery? right = null)
    {
        _clientCommands = null;
        _serverCommands = null;
        _clauses = [];
        _left = left;
        _right = right;
        _operation = operation;
    }

    public static TraceQuery Empty { get; } = new(null, null, []);

    public bool IsEmpty => _operation == TraceQueryOperation.Predicate &&
                           _clientCommands is null &&
                           _serverCommands is null &&
                           _clauses.Count == 0;

    internal bool IsDirectionalGroup => _operation switch
    {
        TraceQueryOperation.Predicate =>
            (_clientCommands is not null || _serverCommands is not null) && _clauses.Count == 0,
        TraceQueryOperation.Or => _left!.IsDirectionalGroup && _right!.IsDirectionalGroup,
        _ => false
    };

    internal static TraceQuery And(TraceQuery left, TraceQuery right) =>
        new(TraceQueryOperation.And, left, right);

    internal static TraceQuery Or(TraceQuery left, TraceQuery right) =>
        new(TraceQueryOperation.Or, left, right);

    internal static TraceQuery Not(TraceQuery operand) =>
        new(TraceQueryOperation.Not, operand);

    internal static TraceQuery CombineImplicit(TraceQuery left, TraceQuery right)
    {
        if (right.IsDirectionalGroup && left.TryMergeDirectionalGroup(right, out var merged))
        {
            return merged;
        }

        return new TraceQuery(TraceQueryOperation.ImplicitAnd, left, right);
    }

    public TraceQueryMatch Match(TraceQueryContext context)
    {
        if (_operation != TraceQueryOperation.Predicate)
        {
            return MatchExpression(context);
        }

        if (!MatchesCommand(context))
        {
            return TraceQueryMatch.NoMatch;
        }

        List<TraceQueryHighlight>? highlights = null;
        foreach (var clause in _clauses)
        {
            highlights ??= [];
            if (!clause.TryMatch(context, highlights))
            {
                return TraceQueryMatch.NoMatch;
            }
        }

        return highlights is { Count: > 0 }
            ? new TraceQueryMatch(true, MergeHighlights(highlights))
            : TraceQueryMatch.MatchWithoutHighlights;
    }

    private TraceQueryMatch MatchExpression(TraceQueryContext context)
    {
        var leftMatch = _left!.Match(context);
        if (_operation == TraceQueryOperation.Not)
        {
            return leftMatch.IsMatch ? TraceQueryMatch.NoMatch : TraceQueryMatch.MatchWithoutHighlights;
        }

        if (_operation is TraceQueryOperation.And or TraceQueryOperation.ImplicitAnd && !leftMatch.IsMatch)
        {
            return TraceQueryMatch.NoMatch;
        }

        var rightMatch = _right!.Match(context);
        if (_operation is TraceQueryOperation.And or TraceQueryOperation.ImplicitAnd && !rightMatch.IsMatch ||
            _operation == TraceQueryOperation.Or && !leftMatch.IsMatch && !rightMatch.IsMatch)
        {
            return TraceQueryMatch.NoMatch;
        }

        var highlights = new List<TraceQueryHighlight>();
        if (leftMatch.IsMatch)
        {
            highlights.AddRange(leftMatch.Highlights);
        }

        if (rightMatch.IsMatch)
        {
            highlights.AddRange(rightMatch.Highlights);
        }

        return highlights.Count > 0
            ? new TraceQueryMatch(true, MergeHighlights(highlights))
            : TraceQueryMatch.MatchWithoutHighlights;
    }

    private bool TryMergeDirectionalGroup(TraceQuery direction, out TraceQuery merged)
    {
        if (IsDirectionalGroup)
        {
            merged = Or(this, direction);
            return true;
        }

        if (_operation == TraceQueryOperation.ImplicitAnd &&
            _left!.TryMergeDirectionalGroup(direction, out var mergedLeft))
        {
            merged = new TraceQuery(TraceQueryOperation.ImplicitAnd, mergedLeft, _right);
            return true;
        }

        if (_operation == TraceQueryOperation.ImplicitAnd &&
            _right!.TryMergeDirectionalGroup(direction, out var mergedRight))
        {
            merged = new TraceQuery(TraceQueryOperation.ImplicitAnd, _left!, mergedRight);
            return true;
        }

        merged = null!;
        return false;
    }

    private bool MatchesCommand(TraceQueryContext context)
    {
        if (_clientCommands is null && _serverCommands is null)
        {
            return true;
        }

        return context.Direction switch
        {
            PacketDirection.Client => _clientCommands?.Contains(context.Command) == true,
            PacketDirection.Server => _serverCommands?.Contains(context.Command) == true,
            _ => false
        };
    }

    private static IReadOnlyList<TraceQueryHighlight> MergeHighlights(List<TraceQueryHighlight> highlights)
    {
        var ordered = highlights
            .Where(highlight => highlight is { Offset: >= 0, Length: > 0 })
            .OrderBy(highlight => highlight.Source)
            .ThenBy(highlight => highlight.Offset)
            .ToList();

        if (ordered.Count < 2)
        {
            return ordered;
        }

        var merged = new List<TraceQueryHighlight>(ordered.Count);
        var current = ordered[0];

        for (var i = 1; i < ordered.Count; i++)
        {
            var next = ordered[i];
            var currentEnd = current.Offset + current.Length;
            if (next.Source == current.Source && next.Offset <= currentEnd)
            {
                var nextEnd = next.Offset + next.Length;
                current = current with { Length = Math.Max(currentEnd, nextEnd) - current.Offset };
                continue;
            }

            merged.Add(current);
            current = next;
        }

        merged.Add(current);
        return merged;
    }
}

internal enum TraceQueryOperation
{
    Predicate,
    And,
    ImplicitAnd,
    Or,
    Not
}

internal interface ITraceQueryClause
{
    bool TryMatch(TraceQueryContext context, List<TraceQueryHighlight> highlights);
}

internal sealed class SequenceQueryClause(HashSet<byte> sequences) : ITraceQueryClause
{
    public bool TryMatch(TraceQueryContext context, List<TraceQueryHighlight> highlights) =>
        context.Sequence is byte sequence && sequences.Contains(sequence);
}

internal sealed class NameQueryClause(string value) : ITraceQueryClause
{
    public bool TryMatch(TraceQueryContext context, List<TraceQueryHighlight> highlights) =>
        string.Equals(context.ClientName ?? string.Empty, value, StringComparison.OrdinalIgnoreCase);
}

internal sealed class NameRegexQueryClause(Regex regex) : ITraceQueryClause
{
    public bool TryMatch(TraceQueryContext context, List<TraceQueryHighlight> highlights)
    {
        try
        {
            return regex.IsMatch(context.ClientName ?? string.Empty);
        }
        catch (RegexMatchTimeoutException)
        {
            return false;
        }
    }
}

internal sealed class ByteSequenceQueryClause(
    byte[] value,
    TraceQueryHighlightSource source) : ITraceQueryClause
{
    public bool TryMatch(TraceQueryContext context, List<TraceQueryHighlight> highlights)
    {
        var data = source == TraceQueryHighlightSource.Data ? context.Data.Span : context.Raw.Span;
        return QueryMatchHelper.FindByteMatches(data, value, source, highlights);
    }
}

internal sealed class TextQueryClause(byte[] value, bool ignoreCase) : ITraceQueryClause
{
    public bool TryMatch(TraceQueryContext context, List<TraceQueryHighlight> highlights) =>
        QueryMatchHelper.FindByteMatches(
            context.Data.Span,
            value,
            TraceQueryHighlightSource.Data,
            highlights,
            ignoreAsciiCase: ignoreCase);
}

internal sealed class TextRegexQueryClause(Regex regex) : ITraceQueryClause
{
    public bool TryMatch(TraceQueryContext context, List<TraceQueryHighlight> highlights)
    {
        var data = context.Data.Span;
        var characters = new char[data.Length];
        for (var i = 0; i < data.Length; i++)
        {
            characters[i] = (char)data[i];
        }

        try
        {
            var matches = regex.Matches(new string(characters));
            if (matches.Count == 0)
            {
                return false;
            }

            foreach (Match match in matches)
            {
                if (match.Length > 0)
                {
                    highlights.Add(new TraceQueryHighlight(
                        match.Index,
                        match.Length,
                        TraceQueryHighlightSource.Data));
                }
            }

            return true;
        }
        catch (RegexMatchTimeoutException)
        {
            return false;
        }
    }
}

internal static class QueryMatchHelper
{
    public static bool FindByteMatches(
        ReadOnlySpan<byte> data,
        ReadOnlySpan<byte> value,
        TraceQueryHighlightSource source,
        List<TraceQueryHighlight> highlights,
        bool ignoreAsciiCase = false)
    {
        if (value.Length == 0 || value.Length > data.Length)
        {
            return false;
        }

        var found = false;
        for (var offset = 0; offset <= data.Length - value.Length; offset++)
        {
            var candidate = data.Slice(offset, value.Length);
            if (ignoreAsciiCase
                    ? !EqualsIgnoringAsciiCase(candidate, value)
                    : !candidate.SequenceEqual(value))
            {
                continue;
            }

            highlights.Add(new TraceQueryHighlight(offset, value.Length, source));
            found = true;
        }

        return found;
    }

    private static bool EqualsIgnoringAsciiCase(ReadOnlySpan<byte> left, ReadOnlySpan<byte> right)
    {
        for (var i = 0; i < left.Length; i++)
        {
            if (ToUpperAscii(left[i]) != ToUpperAscii(right[i]))
            {
                return false;
            }
        }

        return true;
    }

    private static byte ToUpperAscii(byte value) =>
        value is >= (byte)'a' and <= (byte)'z' ? (byte)(value - 32) : value;
}
