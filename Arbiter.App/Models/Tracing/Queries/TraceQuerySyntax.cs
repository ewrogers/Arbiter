using System;
using System.Collections.Generic;
using System.Linq;

namespace Arbiter.App.Models.Tracing.Queries;

public static class TraceQuerySyntax
{
    private static readonly HashSet<string> FieldKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "server",
        "client",
        "text",
        "data",
        "raw",
        "name",
        "sequence",
        "seq"
    };

    private static readonly HashSet<string> BooleanKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "AND",
        "OR",
        "NOT"
    };

    public static IReadOnlyList<TraceQuerySyntaxSpan> GetSpans(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return [];
        }

        var spans = new List<TraceQuerySyntaxSpan>();
        var inQuote = false;
        var inRegex = false;
        var escaped = false;

        for (var i = 0; i < text.Length; i++)
        {
            var character = text[i];
            if (escaped)
            {
                escaped = false;
                continue;
            }

            if ((inQuote || inRegex) && character == '\\')
            {
                escaped = true;
                continue;
            }

            if (inQuote)
            {
                if (character == '"')
                {
                    inQuote = false;
                }
                continue;
            }

            if (inRegex)
            {
                if (character == '/')
                {
                    inRegex = false;
                }
                continue;
            }

            if (character == '"')
            {
                inQuote = true;
                continue;
            }

            if (character == '/')
            {
                inRegex = true;
                continue;
            }

            if (character is '(' or ')')
            {
                spans.Add(new TraceQuerySyntaxSpan(i, 1, TraceQuerySyntaxKind.Grouping));
                continue;
            }

            if (!char.IsLetter(character) || i > 0 && IsWordCharacter(text[i - 1]))
            {
                continue;
            }

            var wordEnd = i + 1;
            while (wordEnd < text.Length && char.IsLetter(text[wordEnd]))
            {
                wordEnd++;
            }

            var word = text.Substring(i, wordEnd - i);
            if (IsFieldKeyword(text, wordEnd, word) || IsBooleanKeyword(text, i, word))
            {
                spans.Add(new TraceQuerySyntaxSpan(i, wordEnd - i, TraceQuerySyntaxKind.Keyword));
            }

            i = wordEnd - 1;
        }

        return spans;
    }

    public static IReadOnlyList<TraceQuerySyntaxSpan> GetKeywordSpans(string text) =>
        GetSpans(text).Where(span => span.Kind == TraceQuerySyntaxKind.Keyword).ToList();

    private static bool IsFieldKeyword(string text, int wordEnd, string word)
    {
        if (!FieldKeywords.Contains(word))
        {
            return false;
        }

        var operatorPosition = wordEnd;
        while (operatorPosition < text.Length && char.IsWhiteSpace(text[operatorPosition]))
        {
            operatorPosition++;
        }

        return operatorPosition < text.Length &&
               (text[operatorPosition] == '=' ||
                operatorPosition + 1 < text.Length &&
                text[operatorPosition] == '!' &&
                text[operatorPosition + 1] == '=');
    }

    private static bool IsBooleanKeyword(string text, int start, string word)
    {
        if (!BooleanKeywords.Contains(word))
        {
            return false;
        }

        var previous = start - 1;
        while (previous >= 0 && char.IsWhiteSpace(text[previous]))
        {
            previous--;
        }

        return previous < 0 || text[previous] is not '=' and not '!';
    }

    private static bool IsWordCharacter(char character) =>
        char.IsLetterOrDigit(character) || character == '_';
}

public enum TraceQuerySyntaxKind
{
    Keyword,
    Grouping
}

public readonly record struct TraceQuerySyntaxSpan(
    int Start,
    int Length,
    TraceQuerySyntaxKind Kind);
