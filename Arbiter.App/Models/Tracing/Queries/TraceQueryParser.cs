using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Arbiter.Net.Client;
using Arbiter.Net.Server;

namespace Arbiter.App.Models.Tracing.Queries;

public static class TraceQueryParser
{
    private const int MaxQueryLength = 2048;
    private const int MaxRegexLength = 256;

    private static readonly string[] SupportedFields =
        ["server", "client", "text", "data", "raw", "name", "sequence", "seq"];

    public static TraceQueryParseResult Parse(string? queryText, bool isTextCaseSensitive = true)
    {
        var query = queryText ?? string.Empty;
        if (string.IsNullOrWhiteSpace(query))
        {
            return new TraceQueryParseResult(TraceQuery.Empty, null);
        }

        if (query.Length > MaxQueryLength)
        {
            return Invalid($"Queries cannot exceed {MaxQueryLength} characters.", MaxQueryLength, query.Length - MaxQueryLength);
        }

        return new ExpressionParser(query, isTextCaseSensitive).Parse();
    }

    private static TraceQueryParseResult ParseSimple(string query, bool isTextCaseSensitive)
    {

        if (!TrySplitClauses(query, out var clauseSpans, out var splitDiagnostic))
        {
            return new TraceQueryParseResult(null, splitDiagnostic);
        }

        HashSet<byte>? clientCommands = null;
        HashSet<byte>? serverCommands = null;
        var clauses = new List<ITraceQueryClause>();
        var negatePredicate = false;

        foreach (var clauseSpan in clauseSpans)
        {
            var start = clauseSpan.Start;
            var length = clauseSpan.Length;
            Trim(query, ref start, ref length);
            if (length == 0)
            {
                return Invalid("Expected a query clause after the comma.", clauseSpan.Start);
            }

            var clauseText = query.AsSpan(start, length);
            var equalsOffset = clauseText.IndexOf('=');
            if (equalsOffset < 0)
            {
                return Invalid("Expected '=' after the field name.", start + length, 0);
            }

            var isNotEquals = equalsOffset > 0 && clauseText[equalsOffset - 1] == '!';
            var fieldStart = start;
            var fieldLength = equalsOffset - (isNotEquals ? 1 : 0);
            Trim(query, ref fieldStart, ref fieldLength);
            if (fieldLength == 0)
            {
                return Invalid("Expected a field name before '='.", start, Math.Max(1, equalsOffset));
            }

            var field = query.Substring(fieldStart, fieldLength).ToLowerInvariant();
            var valueStart = start + equalsOffset + 1;
            var valueLength = start + length - valueStart;
            Trim(query, ref valueStart, ref valueLength);
            if (valueLength == 0)
            {
                return Invalid($"Expected a value for '{field}'.", valueStart, 0);
            }

            var value = query.Substring(valueStart, valueLength);
            TraceQueryDiagnostic? diagnostic;
            negatePredicate = isNotEquals;

            switch (field)
            {
                case "client":
                    clientCommands ??= [];
                    if (!TryParseCommandSet<ClientCommand>(value, valueStart, out var parsedClientCommands, out diagnostic))
                    {
                        return new TraceQueryParseResult(null, diagnostic);
                    }
                    clientCommands.UnionWith(parsedClientCommands);
                    break;

                case "server":
                    serverCommands ??= [];
                    if (!TryParseCommandSet<ServerCommand>(value, valueStart, out var parsedServerCommands, out diagnostic))
                    {
                        return new TraceQueryParseResult(null, diagnostic);
                    }
                    serverCommands.UnionWith(parsedServerCommands);
                    break;

                case "sequence":
                case "seq":
                    if (!TryParseHexSet(value, valueStart, out var sequences, out diagnostic))
                    {
                        return new TraceQueryParseResult(null, diagnostic);
                    }
                    clauses.Add(new SequenceQueryClause(sequences));
                    break;

                case "data":
                case "raw":
                    if (!TryParseByteSequence(value, valueStart, out var bytes, out diagnostic))
                    {
                        return new TraceQueryParseResult(null, diagnostic);
                    }
                    clauses.Add(new ByteSequenceQueryClause(
                        bytes,
                        field == "data" ? TraceQueryHighlightSource.Data : TraceQueryHighlightSource.Raw));
                    break;

                case "text":
                    if (!TryParseTextClause(
                            value,
                            valueStart,
                            isTextCaseSensitive,
                            out var textClause,
                            out diagnostic))
                    {
                        return new TraceQueryParseResult(null, diagnostic);
                    }
                    clauses.Add(textClause);
                    break;

                case "name":
                    if (!TryParseNameClause(value, valueStart, out var nameClause, out diagnostic))
                    {
                        return new TraceQueryParseResult(null, diagnostic);
                    }
                    clauses.Add(nameClause);
                    break;

                default:
                    var suggestion = GetFieldSuggestion(field);
                    var message = suggestion is null
                        ? $"Unknown query field '{field}'."
                        : $"Unknown query field '{field}'. Did you mean '{suggestion}'?";
                    return Invalid(message, fieldStart, fieldLength);
            }
        }

        var queryResult = new TraceQuery(clientCommands, serverCommands, clauses);
        return new TraceQueryParseResult(negatePredicate ? TraceQuery.Not(queryResult) : queryResult, null);
    }

    private static bool TryParseTextClause(
        string value,
        int position,
        bool isCaseSensitive,
        out ITraceQueryClause clause,
        out TraceQueryDiagnostic? diagnostic)
    {
        if (value.StartsWith('/'))
        {
            if (!TryCompileRegex(value, position, ignoreCase: !isCaseSensitive, out var regex, out diagnostic))
            {
                clause = null!;
                return false;
            }

            clause = new TextRegexQueryClause(regex);
            return true;
        }

        string text;
        if (value.StartsWith('"'))
        {
            if (!TryParseQuotedString(value, position, out text, out diagnostic))
            {
                clause = null!;
                return false;
            }
        }
        else
        {
            text = value.Trim();
            diagnostic = null;
        }

        if (text.Length == 0)
        {
            clause = null!;
            diagnostic = new TraceQueryDiagnostic("Text values cannot be empty.", position, value.Length);
            return false;
        }

        if (text.Any(character => character > 0x7F))
        {
            clause = null!;
            diagnostic = new TraceQueryDiagnostic("Text searches support ASCII characters only.", position, value.Length);
            return false;
        }

        clause = new TextQueryClause(Encoding.ASCII.GetBytes(text), ignoreCase: !isCaseSensitive);
        return true;
    }

    private static bool TryParseNameClause(
        string value,
        int position,
        out ITraceQueryClause clause,
        out TraceQueryDiagnostic? diagnostic)
    {
        if (value.StartsWith('/'))
        {
            if (!TryCompileRegex(value, position, ignoreCase: true, out var regex, out diagnostic))
            {
                clause = null!;
                return false;
            }

            clause = new NameRegexQueryClause(regex);
            return true;
        }

        string name;
        if (value.StartsWith('"'))
        {
            if (!TryParseQuotedString(value, position, out name, out diagnostic))
            {
                clause = null!;
                return false;
            }
        }
        else
        {
            name = value.Trim();
            diagnostic = null;
        }

        clause = new NameQueryClause(name);
        return true;
    }

    private static bool TryCompileRegex(
        string value,
        int position,
        bool ignoreCase,
        out Regex regex,
        out TraceQueryDiagnostic? diagnostic)
    {
        regex = null!;
        diagnostic = null;

        if (!TryReadRegex(value, position, out var pattern, out diagnostic))
        {
            return false;
        }

        if (pattern.Length == 0)
        {
            diagnostic = new TraceQueryDiagnostic("Regular expressions cannot be empty.", position, value.Length);
            return false;
        }

        if (pattern.Length > MaxRegexLength)
        {
            diagnostic = new TraceQueryDiagnostic(
                $"Regular expressions cannot exceed {MaxRegexLength} characters.",
                position + 1 + MaxRegexLength,
                pattern.Length - MaxRegexLength);
            return false;
        }

        var options = RegexOptions.CultureInvariant | RegexOptions.NonBacktracking;
        if (ignoreCase)
        {
            options |= RegexOptions.IgnoreCase;
        }

        try
        {
            regex = new Regex(pattern, options, TimeSpan.FromMilliseconds(100));
            return true;
        }
        catch (RegexParseException ex)
        {
            diagnostic = new TraceQueryDiagnostic(
                $"Invalid regular expression: {ex.Message}",
                position + 1 + Math.Max(0, ex.Offset),
                1);
            return false;
        }
        catch (NotSupportedException ex)
        {
            diagnostic = new TraceQueryDiagnostic(
                $"Unsupported regular expression: {ex.Message}",
                position,
                value.Length);
            return false;
        }
        catch (ArgumentException ex)
        {
            diagnostic = new TraceQueryDiagnostic(
                $"Invalid regular expression: {ex.Message}",
                position,
                value.Length);
            return false;
        }
    }

    private static bool TryReadRegex(
        string value,
        int position,
        out string pattern,
        out TraceQueryDiagnostic? diagnostic)
    {
        var builder = new StringBuilder();
        var escaped = false;

        for (var i = 1; i < value.Length; i++)
        {
            var character = value[i];
            if (escaped)
            {
                if (character == '/')
                {
                    builder.Append('/');
                }
                else
                {
                    builder.Append('\\');
                    builder.Append(character);
                }
                escaped = false;
                continue;
            }

            if (character == '\\')
            {
                escaped = true;
                continue;
            }

            if (character != '/')
            {
                builder.Append(character);
                continue;
            }

            if (!string.IsNullOrWhiteSpace(value[(i + 1)..]))
            {
                pattern = string.Empty;
                diagnostic = new TraceQueryDiagnostic(
                    "Regex flags are not supported. Casing is determined by the field.",
                    position + i + 1,
                    value.Length - i - 1);
                return false;
            }

            pattern = builder.ToString();
            diagnostic = null;
            return true;
        }

        pattern = string.Empty;
        diagnostic = new TraceQueryDiagnostic("Missing closing '/' for the regular expression.", position, value.Length);
        return false;
    }

    private static bool TryParseQuotedString(
        string value,
        int position,
        out string result,
        out TraceQueryDiagnostic? diagnostic)
    {
        result = string.Empty;
        diagnostic = null;
        if (!value.StartsWith('"'))
        {
            return false;
        }

        var builder = new StringBuilder();
        for (var i = 1; i < value.Length; i++)
        {
            var character = value[i];
            if (character == '"')
            {
                if (!string.IsNullOrWhiteSpace(value[(i + 1)..]))
                {
                    diagnostic = new TraceQueryDiagnostic(
                        "Unexpected characters after the closing quote.",
                        position + i + 1,
                        value.Length - i - 1);
                    return false;
                }

                result = builder.ToString();
                return true;
            }

            if (character != '\\')
            {
                builder.Append(character);
                continue;
            }

            if (++i >= value.Length)
            {
                diagnostic = new TraceQueryDiagnostic("Incomplete escape sequence.", position + i - 1);
                return false;
            }

            var escaped = value[i];
            builder.Append(escaped switch
            {
                '"' => '"',
                '\\' => '\\',
                'n' => '\n',
                'r' => '\r',
                't' => '\t',
                _ => escaped
            });
        }

        diagnostic = new TraceQueryDiagnostic("Missing closing quote.", position, value.Length);
        return false;
    }

    private static bool TryParseByteSequence(
        string value,
        int position,
        out byte[] bytes,
        out TraceQueryDiagnostic? diagnostic)
    {
        var parsed = new List<byte>();
        var offset = 0;

        while (offset < value.Length)
        {
            while (offset < value.Length && char.IsWhiteSpace(value[offset]))
            {
                offset++;
            }

            if (offset >= value.Length)
            {
                break;
            }

            var tokenStart = offset;
            while (offset < value.Length && !char.IsWhiteSpace(value[offset]))
            {
                offset++;
            }

            var token = value[tokenStart..offset];
            if (!TryParseHexByte(token, requireTwoDigits: true, out var parsedByte))
            {
                bytes = [];
                diagnostic = new TraceQueryDiagnostic(
                    $"Invalid data byte '{token}'. Expected a two-digit hexadecimal byte.",
                    position + tokenStart,
                    token.Length);
                return false;
            }

            parsed.Add(parsedByte);
        }

        if (parsed.Count == 0)
        {
            bytes = [];
            diagnostic = new TraceQueryDiagnostic("Expected at least one hexadecimal byte.", position, value.Length);
            return false;
        }

        bytes = [.. parsed];
        diagnostic = null;
        return true;
    }

    private static bool TryParseHexSet(
        string value,
        int position,
        out HashSet<byte> values,
        out TraceQueryDiagnostic? diagnostic) =>
        TryParseValueSet(value, position, null, out values, out diagnostic);

    private static bool TryParseCommandSet<TCommand>(
        string value,
        int position,
        out HashSet<byte> values,
        out TraceQueryDiagnostic? diagnostic)
        where TCommand : struct, Enum
    {
        bool TryParseName(string token, out byte parsed)
        {
            if (Enum.TryParse<TCommand>(token, ignoreCase: true, out var command) && Enum.IsDefined(command))
            {
                parsed = Convert.ToByte(command, CultureInfo.InvariantCulture);
                return true;
            }

            parsed = 0;
            return false;
        }

        return TryParseValueSet(value, position, TryParseName, out values, out diagnostic);
    }

    private static bool TryParseValueSet(
        string value,
        int position,
        TryParseNamedValue? tryParseNamedValue,
        out HashSet<byte> values,
        out TraceQueryDiagnostic? diagnostic)
    {
        values = [];
        diagnostic = null;
        var segmentStart = 0;

        while (segmentStart <= value.Length)
        {
            var separator = FindNextValueSeparator(value, segmentStart);
            var segmentEnd = separator >= 0 ? separator : value.Length;
            var tokenStart = segmentStart;
            var tokenLength = segmentEnd - segmentStart;
            Trim(value, ref tokenStart, ref tokenLength);

            if (tokenLength == 0)
            {
                diagnostic = new TraceQueryDiagnostic(
                    "Expected a value between command separators.",
                    position + segmentStart,
                    1);
                return false;
            }

            var token = value.Substring(tokenStart, tokenLength);
            var rangeSeparator = token.IndexOf('-');
            if (rangeSeparator >= 0)
            {
                if (rangeSeparator == 0 || rangeSeparator == token.Length - 1 ||
                    token.IndexOf('-', rangeSeparator + 1) >= 0)
                {
                    diagnostic = new TraceQueryDiagnostic(
                        $"Invalid hexadecimal range '{token}'.",
                        position + tokenStart,
                        tokenLength);
                    return false;
                }

                var startToken = token[..rangeSeparator].Trim();
                var endToken = token[(rangeSeparator + 1)..].Trim();
                if (!TryParseHexByte(startToken, requireTwoDigits: false, out var startValue) ||
                    !TryParseHexByte(endToken, requireTwoDigits: false, out var endValue))
                {
                    diagnostic = new TraceQueryDiagnostic(
                        $"Invalid hexadecimal range '{token}'.",
                        position + tokenStart,
                        tokenLength);
                    return false;
                }

                if (startValue > endValue)
                {
                    diagnostic = new TraceQueryDiagnostic(
                        "Range start must not exceed range end.",
                        position + tokenStart,
                        tokenLength);
                    return false;
                }

                for (var current = (int)startValue; current <= endValue; current++)
                {
                    values.Add((byte)current);
                }
            }
            else if (TryParseHexByte(token, requireTwoDigits: false, out var parsedValue) ||
                     tryParseNamedValue?.Invoke(token, out parsedValue) == true)
            {
                values.Add(parsedValue);
            }
            else
            {
                diagnostic = new TraceQueryDiagnostic(
                    $"Invalid hexadecimal value or command name '{token}'.",
                    position + tokenStart,
                    tokenLength);
                return false;
            }

            if (separator < 0)
            {
                break;
            }

            segmentStart = separator + 1;
        }

        return true;
    }

    private static bool TryParseHexByte(string value, bool requireTwoDigits, out byte parsed)
    {
        var token = value.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? value[2..] : value;
        if (token.Length is 0 or > 2 || requireTwoDigits && token.Length != 2)
        {
            parsed = 0;
            return false;
        }

        return byte.TryParse(token, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out parsed);
    }

    private static int FindNextValueSeparator(string value, int start)
    {
        var pipe = value.IndexOf('|', start);
        var comma = value.IndexOf(',', start);

        return (pipe, comma) switch
        {
            (< 0, < 0) => -1,
            (< 0, _) => comma,
            (_, < 0) => pipe,
            _ => Math.Min(pipe, comma)
        };
    }

    private static bool TrySplitClauses(
        string query,
        out List<QuerySpan> clauses,
        out TraceQueryDiagnostic? diagnostic)
    {
        clauses = [];
        diagnostic = null;
        var start = 0;
        var openingPosition = -1;
        var seenEquals = false;
        var valueStarted = false;
        var inQuote = false;
        var inRegex = false;
        var escaped = false;

        for (var i = 0; i < query.Length; i++)
        {
            var character = query[i];
            if (escaped)
            {
                escaped = false;
                continue;
            }

            if (inQuote)
            {
                if (character == '\\')
                {
                    escaped = true;
                }
                else if (character == '"')
                {
                    inQuote = false;
                }
                continue;
            }

            if (inRegex)
            {
                if (character == '\\')
                {
                    escaped = true;
                }
                else if (character == '/')
                {
                    inRegex = false;
                }
                continue;
            }

            if (!seenEquals)
            {
                if (character == '=')
                {
                    seenEquals = true;
                }
            }
            else if (!valueStarted && !char.IsWhiteSpace(character))
            {
                valueStarted = true;
                if (character == '"')
                {
                    inQuote = true;
                    openingPosition = i;
                }
                else if (character == '/')
                {
                    inRegex = true;
                    openingPosition = i;
                }
            }

            if (!seenEquals || !valueStarted)
            {
                continue;
            }

            var next = i + 1;
            if (character == ',')
            {
                while (next < query.Length && char.IsWhiteSpace(query[next]))
                {
                    next++;
                }

                if (!LooksLikeClauseStart(query, next))
                {
                    continue;
                }
            }
            else if (char.IsWhiteSpace(character))
            {
                while (next < query.Length && char.IsWhiteSpace(query[next]))
                {
                    next++;
                }

                if (!LooksLikeClauseStart(query, next))
                {
                    continue;
                }
            }
            else
            {
                continue;
            }

            clauses.Add(new QuerySpan(start, i - start));
            start = next;
            seenEquals = false;
            valueStarted = false;
            openingPosition = -1;
            i = next - 1;
        }

        if (inQuote)
        {
            diagnostic = new TraceQueryDiagnostic("Missing closing quote.", openingPosition, query.Length - openingPosition);
            return false;
        }

        if (inRegex)
        {
            diagnostic = new TraceQueryDiagnostic(
                "Missing closing '/' for the regular expression.",
                openingPosition,
                query.Length - openingPosition);
            return false;
        }

        clauses.Add(new QuerySpan(start, query.Length - start));
        return true;
    }

    private static bool LooksLikeClauseStart(string query, int start)
    {
        if (start >= query.Length || !char.IsLetter(query[start]))
        {
            return false;
        }

        var position = start + 1;
        while (position < query.Length && char.IsLetter(query[position]))
        {
            position++;
        }

        while (position < query.Length && char.IsWhiteSpace(query[position]))
        {
            position++;
        }

        return position < query.Length &&
               (query[position] == '=' ||
                position + 1 < query.Length && query[position] == '!' && query[position + 1] == '=');
    }

    private static string? GetFieldSuggestion(string field)
    {
        var commonSuggestion = field switch
        {
            "payload" => "data",
            "character" or "clientname" => "name",
            "servercommand" => "server",
            "clientcommand" => "client",
            _ => null
        };
        if (commonSuggestion is not null)
        {
            return commonSuggestion;
        }

        var suggestion = SupportedFields
            .Select(candidate => (Candidate: candidate, Distance: GetEditDistance(field, candidate)))
            .OrderBy(result => result.Distance)
            .First();

        return suggestion.Distance <= 3 ? suggestion.Candidate : null;
    }

    private static int GetEditDistance(string left, string right)
    {
        var previous = new int[right.Length + 1];
        var current = new int[right.Length + 1];
        for (var i = 0; i <= right.Length; i++)
        {
            previous[i] = i;
        }

        for (var leftIndex = 1; leftIndex <= left.Length; leftIndex++)
        {
            current[0] = leftIndex;
            for (var rightIndex = 1; rightIndex <= right.Length; rightIndex++)
            {
                var cost = left[leftIndex - 1] == right[rightIndex - 1] ? 0 : 1;
                current[rightIndex] = Math.Min(
                    Math.Min(current[rightIndex - 1] + 1, previous[rightIndex] + 1),
                    previous[rightIndex - 1] + cost);
            }

            (previous, current) = (current, previous);
        }

        return previous[right.Length];
    }

    private static void Trim(string value, ref int start, ref int length)
    {
        while (length > 0 && char.IsWhiteSpace(value[start]))
        {
            start++;
            length--;
        }

        while (length > 0 && char.IsWhiteSpace(value[start + length - 1]))
        {
            length--;
        }
    }

    private static TraceQueryParseResult Invalid(string message, int position, int length = 1) =>
        new(null, new TraceQueryDiagnostic(message, position, length));

    private sealed class ExpressionParser(string text, bool isTextCaseSensitive)
    {
        private TraceQueryDiagnostic? _diagnostic;
        private int _position;

        public TraceQueryParseResult Parse()
        {
            SkipWhitespace();
            if (_position >= text.Length)
            {
                return new TraceQueryParseResult(TraceQuery.Empty, null);
            }

            var query = ParseOr();
            if (_diagnostic is not null)
            {
                return new TraceQueryParseResult(null, _diagnostic);
            }

            SkipWhitespace();
            if (_position < text.Length)
            {
                var message = text[_position] == ')'
                    ? "Unexpected closing parenthesis."
                    : $"Unexpected token '{ReadUnexpectedToken()}'.";
                return Invalid(message, _position);
            }

            return new TraceQueryParseResult(query, null);
        }

        private TraceQuery? ParseOr()
        {
            var left = ParseAnd();
            while (_diagnostic is null)
            {
                SkipWhitespace();
                if (!TryConsumeKeyword("OR"))
                {
                    return left;
                }

                var operatorPosition = _position - 2;
                SkipWhitespace();
                if (!CanStartUnary(_position))
                {
                    SetDiagnostic("Expected a query expression after 'OR'.", operatorPosition, 2);
                    return null;
                }

                var right = ParseAnd();
                if (right is null)
                {
                    return null;
                }

                left = TraceQuery.Or(left!, right);
            }

            return null;
        }

        private TraceQuery? ParseAnd()
        {
            var left = ParseUnary();
            while (_diagnostic is null)
            {
                SkipWhitespace();
                if (_position >= text.Length || text[_position] == ')' || IsKeywordAt(_position, "OR"))
                {
                    return left;
                }

                if (TryConsumeKeyword("AND"))
                {
                    var operatorPosition = _position - 3;
                    SkipWhitespace();
                    if (!CanStartUnary(_position))
                    {
                        SetDiagnostic("Expected a query expression after 'AND'.", operatorPosition, 3);
                        return null;
                    }

                    var right = ParseUnary();
                    if (right is null)
                    {
                        return null;
                    }

                    left = TraceQuery.And(left!, right);
                    continue;
                }

                if (text[_position] == ',')
                {
                    var commaPosition = _position++;
                    SkipWhitespace();
                    if (!CanStartUnary(_position))
                    {
                        SetDiagnostic("Expected a query expression after the comma.", commaPosition);
                        return null;
                    }
                }
                else if (!CanStartUnary(_position))
                {
                    SetDiagnostic($"Unexpected token '{ReadUnexpectedToken()}'.", _position);
                    return null;
                }

                var implicitRight = ParseUnary();
                if (implicitRight is null)
                {
                    return null;
                }

                left = TraceQuery.CombineImplicit(left!, implicitRight);
            }

            return null;
        }

        private TraceQuery? ParseUnary()
        {
            SkipWhitespace();
            if (!TryConsumeKeyword("NOT"))
            {
                return ParsePrimary();
            }

            var operatorPosition = _position - 3;
            SkipWhitespace();
            if (!CanStartUnary(_position))
            {
                SetDiagnostic("Expected a query expression after 'NOT'.", operatorPosition, 3);
                return null;
            }

            var operand = ParseUnary();
            return operand is null ? null : TraceQuery.Not(operand);
        }

        private TraceQuery? ParsePrimary()
        {
            SkipWhitespace();
            if (_position >= text.Length)
            {
                SetDiagnostic("Expected a query expression.", _position, 0);
                return null;
            }

            if (text[_position] != '(')
            {
                return ParsePredicate();
            }

            var openingPosition = _position++;
            SkipWhitespace();
            if (_position < text.Length && text[_position] == ')')
            {
                SetDiagnostic("Parentheses cannot be empty.", openingPosition, 2);
                return null;
            }

            var expression = ParseOr();
            if (_diagnostic is not null)
            {
                return null;
            }

            SkipWhitespace();
            if (_position >= text.Length || text[_position] != ')')
            {
                SetDiagnostic("Missing closing parenthesis.", openingPosition, text.Length - openingPosition);
                return null;
            }

            _position++;
            return expression;
        }

        private TraceQuery? ParsePredicate()
        {
            var predicateStart = _position;
            if (!char.IsLetter(text[_position]))
            {
                SetDiagnostic("Expected a query field or '('.", _position);
                return null;
            }

            while (_position < text.Length && char.IsLetter(text[_position]))
            {
                _position++;
            }

            var field = text[predicateStart.._position].ToLowerInvariant();
            SkipWhitespace();
            if (_position + 1 < text.Length && text[_position] == '!' && text[_position + 1] == '=')
            {
                _position += 2;
            }
            else if (_position < text.Length && text[_position] == '=')
            {
                _position++;
            }
            else
            {
                SetDiagnostic("Expected '=' or '!=' after the field name.", _position, 0);
                return null;
            }

            SkipWhitespace();
            if (_position >= text.Length || text[_position] == ')' || text[_position] == ',')
            {
                SetDiagnostic($"Expected a value for '{field}'.", _position, 0);
                return null;
            }

            if (text[_position] == '"')
            {
                ReadDelimitedValue('"');
            }
            else if (text[_position] == '/')
            {
                ReadDelimitedValue('/');
                while (_position < text.Length &&
                       !char.IsWhiteSpace(text[_position]) &&
                       text[_position] is not ',' and not ')')
                {
                    _position++;
                }
            }
            else
            {
                ReadUnquotedValue(field is "text" or "name");
            }

            var predicateEnd = _position;
            while (predicateEnd > predicateStart && char.IsWhiteSpace(text[predicateEnd - 1]))
            {
                predicateEnd--;
            }

            var result = ParseSimple(text[predicateStart..predicateEnd], isTextCaseSensitive);
            if (result.Diagnostic is not null)
            {
                _diagnostic = result.Diagnostic with
                {
                    Position = predicateStart + result.Diagnostic.Position
                };
                return null;
            }

            return result.Query;
        }

        private void ReadDelimitedValue(char delimiter)
        {
            var escaped = false;
            _position++;
            while (_position < text.Length)
            {
                var character = text[_position++];
                if (escaped)
                {
                    escaped = false;
                }
                else if (character == '\\')
                {
                    escaped = true;
                }
                else if (character == delimiter)
                {
                    return;
                }
            }
        }

        private void ReadUnquotedValue(bool stopAtWhitespace)
        {
            while (_position < text.Length)
            {
                if (text[_position] == ')')
                {
                    return;
                }

                if (text[_position] == ',')
                {
                    var afterComma = SkipWhitespace(_position + 1);
                    if (CanStartUnary(afterComma))
                    {
                        return;
                    }
                }

                if (char.IsWhiteSpace(text[_position]))
                {
                    var afterWhitespace = SkipWhitespace(_position);
                    if (stopAtWhitespace || afterWhitespace >= text.Length ||
                        text[afterWhitespace] == ')' ||
                        IsKeywordAt(afterWhitespace, "AND") ||
                        IsKeywordAt(afterWhitespace, "OR") ||
                        CanStartUnary(afterWhitespace))
                    {
                        return;
                    }

                    _position = afterWhitespace;
                    continue;
                }

                _position++;
            }
        }

        private bool CanStartUnary(int position) =>
            position < text.Length &&
            (text[position] == '(' || IsKeywordAt(position, "NOT") || LooksLikePredicate(position));

        private bool LooksLikePredicate(int position)
        {
            if (position >= text.Length || !char.IsLetter(text[position]))
            {
                return false;
            }

            while (position < text.Length && char.IsLetter(text[position]))
            {
                position++;
            }

            position = SkipWhitespace(position);
            return position < text.Length &&
                   (text[position] == '=' ||
                    position + 1 < text.Length && text[position] == '!' && text[position + 1] == '=');
        }

        private bool TryConsumeKeyword(string keyword)
        {
            if (!IsKeywordAt(_position, keyword))
            {
                return false;
            }

            _position += keyword.Length;
            return true;
        }

        private bool IsKeywordAt(int position, string keyword)
        {
            if (position + keyword.Length > text.Length ||
                !text.AsSpan(position, keyword.Length).Equals(keyword, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var beforeIsWord = position > 0 && IsWordCharacter(text[position - 1]);
            var after = position + keyword.Length;
            var afterIsWord = after < text.Length && IsWordCharacter(text[after]);
            return !beforeIsWord && !afterIsWord;
        }

        private void SkipWhitespace()
        {
            _position = SkipWhitespace(_position);
        }

        private int SkipWhitespace(int position)
        {
            while (position < text.Length && char.IsWhiteSpace(text[position]))
            {
                position++;
            }

            return position;
        }

        private string ReadUnexpectedToken()
        {
            var start = _position;
            while (_position < text.Length &&
                   !char.IsWhiteSpace(text[_position]) &&
                   text[_position] is not '(' and not ')' and not ',')
            {
                _position++;
            }

            if (_position == start && _position < text.Length)
            {
                _position++;
            }

            return text[start.._position];
        }

        private void SetDiagnostic(string message, int position, int length = 1)
        {
            _diagnostic ??= new TraceQueryDiagnostic(message, position, length);
        }

        private static bool IsWordCharacter(char character) =>
            char.IsLetterOrDigit(character) || character == '_';
    }

    private delegate bool TryParseNamedValue(string value, out byte parsed);

    private readonly record struct QuerySpan(int Start, int Length);
}
