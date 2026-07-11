using System.Text;
using Arbiter.App.Models.Tracing;
using Arbiter.App.Models.Tracing.Queries;
using Arbiter.App.ViewModels.Tracing;

namespace Arbiter.App.Tests.Models.Tracing.Queries;

public class TraceQueryParserTests
{
    [Test]
    public void Should_Combine_Client_And_Server_Commands_As_Alternatives()
    {
        var query = Parse("server=13, client=45-47");

        Assert.Multiple(() =>
        {
            Assert.That(query.Match(Context(PacketDirection.Server, 0x13)).IsMatch, Is.True);
            Assert.That(query.Match(Context(PacketDirection.Client, 0x46)).IsMatch, Is.True);
            Assert.That(query.Match(Context(PacketDirection.Client, 0x44)).IsMatch, Is.False);
            Assert.That(query.Match(Context(PacketDirection.Server, 0x46)).IsMatch, Is.False);
        });
    }

    [Test]
    public void Should_Accept_Comma_Separated_Commands_And_Whitespace_Separated_Clauses()
    {
        const string text = "server=05,15 client=12";
        var query = Parse(text);
        var keywords = TraceQuerySyntax.GetKeywordSpans(text)
            .Select(span => text.Substring(span.Start, span.Length));

        Assert.Multiple(() =>
        {
            Assert.That(query.Match(Context(PacketDirection.Server, 0x05)).IsMatch, Is.True);
            Assert.That(query.Match(Context(PacketDirection.Server, 0x15)).IsMatch, Is.True);
            Assert.That(query.Match(Context(PacketDirection.Client, 0x12)).IsMatch, Is.True);
            Assert.That(query.Match(Context(PacketDirection.Server, 0x12)).IsMatch, Is.False);
            Assert.That(keywords, Is.EqualTo(new[] { "server", "client" }));
        });
    }

    [Test]
    public void Should_Mix_Individual_Commands_And_Ascending_Ranges_In_Implicit_Queries()
    {
        var query = Parse("server=05, 12-15,19 client=45-47, text=Test");

        Assert.Multiple(() =>
        {
            Assert.That(query.Match(Context(PacketDirection.Server, 0x05, "Test")).IsMatch, Is.True);
            Assert.That(query.Match(Context(PacketDirection.Server, 0x13, "Test")).IsMatch, Is.True);
            Assert.That(query.Match(Context(PacketDirection.Server, 0x19, "Test")).IsMatch, Is.True);
            Assert.That(query.Match(Context(PacketDirection.Client, 0x46, "Test")).IsMatch, Is.True);
            Assert.That(query.Match(Context(PacketDirection.Server, 0x16, "Test")).IsMatch, Is.False);
            Assert.That(query.Match(Context(PacketDirection.Client, 0x46, "test")).IsMatch, Is.False);
        });
    }

    [Test]
    public void Should_Apply_And_Before_Or()
    {
        var query = Parse("server=13 or server=15 and text=Test");

        Assert.Multiple(() =>
        {
            Assert.That(query.Match(Context(PacketDirection.Server, 0x13)).IsMatch, Is.True);
            Assert.That(query.Match(Context(PacketDirection.Server, 0x15, "Test")).IsMatch, Is.True);
            Assert.That(query.Match(Context(PacketDirection.Server, 0x15, "test")).IsMatch, Is.False);
        });
    }

    [Test]
    public void Should_Respect_Parenthesized_Expressions()
    {
        var query = Parse("(server=13 OR client=45) AND text=Test");

        Assert.Multiple(() =>
        {
            Assert.That(query.Match(Context(PacketDirection.Server, 0x13, "Test")).IsMatch, Is.True);
            Assert.That(query.Match(Context(PacketDirection.Client, 0x45, "Test")).IsMatch, Is.True);
            Assert.That(query.Match(Context(PacketDirection.Client, 0x45, "test")).IsMatch, Is.False);
            Assert.That(query.Match(Context(PacketDirection.Server, 0x14, "Test")).IsMatch, Is.False);
        });
    }

    [Test]
    public void Should_Support_Not_And_Not_Equals()
    {
        var notQuery = Parse("NOT (server=12 OR text=error)");
        var notEqualsQuery = Parse("server!=12");

        Assert.Multiple(() =>
        {
            Assert.That(notQuery.Match(Context(PacketDirection.Server, 0x13, "okay")).IsMatch, Is.True);
            Assert.That(notQuery.Match(Context(PacketDirection.Server, 0x12, "okay")).IsMatch, Is.False);
            Assert.That(notQuery.Match(Context(PacketDirection.Client, 0x45, "error")).IsMatch, Is.False);
            Assert.That(notEqualsQuery.Match(Context(PacketDirection.Server, 0x13)).IsMatch, Is.True);
            Assert.That(notEqualsQuery.Match(Context(PacketDirection.Server, 0x12)).IsMatch, Is.False);
            Assert.That(notEqualsQuery.Match(Context(PacketDirection.Client, 0x12)).IsMatch, Is.True);
        });
    }

    [Test]
    public void Should_Allow_Unquoted_Single_Token_Strings()
    {
        var query = Parse("text=Test AND name=SiLo");

        Assert.Multiple(() =>
        {
            Assert.That(query.Match(Context(PacketDirection.Server, 0x13, "Test", "silo")).IsMatch, Is.True);
            Assert.That(query.Match(Context(PacketDirection.Server, 0x13, "test", "SILO")).IsMatch, Is.False);
        });
    }

    [Test]
    public void Should_Preserve_Implicit_Directional_Alternatives_Around_Other_Predicates()
    {
        var query = Parse("server=13 text=Test client=45");

        Assert.Multiple(() =>
        {
            Assert.That(query.Match(Context(PacketDirection.Server, 0x13, "Test")).IsMatch, Is.True);
            Assert.That(query.Match(Context(PacketDirection.Client, 0x45, "Test")).IsMatch, Is.True);
            Assert.That(query.Match(Context(PacketDirection.Client, 0x45, "test")).IsMatch, Is.False);
        });
    }

    [Test]
    public void Should_Accept_Command_Names_And_Hexadecimal_Lists()
    {
        var query = Parse("server=HealthBar|29, client=Heartbeat");

        Assert.Multiple(() =>
        {
            Assert.That(query.Match(Context(PacketDirection.Server, 0x13)).IsMatch, Is.True);
            Assert.That(query.Match(Context(PacketDirection.Server, 0x29)).IsMatch, Is.True);
            Assert.That(query.Match(Context(PacketDirection.Client, 0x45)).IsMatch, Is.True);
        });
    }

    [Test]
    public void Should_Require_Every_Non_Directional_Clause()
    {
        var query = Parse("server=13, text=\"Test\", data=54 65");

        Assert.Multiple(() =>
        {
            Assert.That(query.Match(Context(PacketDirection.Server, 0x13, "Test")).IsMatch, Is.True);
            Assert.That(query.Match(Context(PacketDirection.Server, 0x13, "test")).IsMatch, Is.False);
            Assert.That(query.Match(Context(PacketDirection.Server, 0x14, "Test")).IsMatch, Is.False);
        });
    }

    [Test]
    public void Should_Search_Text_Case_Sensitively_And_Return_Byte_Ranges()
    {
        var query = Parse("text=\"Test\"");
        var match = query.Match(Context(PacketDirection.Server, 0x13, "xxTesttest"));

        Assert.Multiple(() =>
        {
            Assert.That(match.IsMatch, Is.True);
            Assert.That(match.Highlights, Is.EqualTo(
                new[] { new TraceQueryHighlight(2, 4, TraceQueryHighlightSource.Data) }));
        });
    }

    [Test]
    public void Should_Toggle_Case_Sensitivity_For_Text_And_Text_Regex_Searches()
    {
        var literal = Parse("text=Test", isTextCaseSensitive: false);
        var regex = Parse("text=/error/", isTextCaseSensitive: false);

        Assert.Multiple(() =>
        {
            Assert.That(literal.Match(Context(PacketDirection.Server, 0x13, "xxTESTxx")).Highlights,
                Is.EqualTo(new[] { new TraceQueryHighlight(2, 4, TraceQueryHighlightSource.Data) }));
            Assert.That(regex.Match(Context(PacketDirection.Server, 0x13, "ERROR")).IsMatch, Is.True);
        });
    }

    [Test]
    public void Should_Default_The_Search_View_Model_To_Case_Insensitive_Text()
    {
        var search = new TraceSearchViewModel { QueryText = "text=Test" };

        Assert.Multiple(() =>
        {
            Assert.That(search.IsTextCaseSensitive, Is.False);
            Assert.That(search.Query.Match(Context(PacketDirection.Server, 0x13, "test")).IsMatch, Is.True);
        });

        search.IsTextCaseSensitive = true;

        Assert.That(search.Query.Match(Context(PacketDirection.Server, 0x13, "test")).IsMatch, Is.False);
    }

    [Test]
    public void Should_Keep_The_Last_Valid_Query_When_Validation_Fails()
    {
        var search = new TraceSearchViewModel { QueryText = "server=13" };
        var validQuery = search.Query;

        search.QueryText = "server=GG";

        Assert.Multiple(() =>
        {
            Assert.That(search.HasQueryError, Is.True);
            Assert.That(search.Query, Is.SameAs(validQuery));
            Assert.That(search.Query.Match(Context(PacketDirection.Server, 0x13)).IsMatch, Is.True);
        });
    }

    [Test]
    public void Should_Search_Character_Names_Case_Insensitively()
    {
        var literal = Parse("name=\"SiLo\"", isTextCaseSensitive: false);
        var regex = Parse("name=/^silo$/", isTextCaseSensitive: true);
        var context = Context(PacketDirection.Server, 0x13, name: "SILO");

        Assert.Multiple(() =>
        {
            Assert.That(literal.Match(context).IsMatch, Is.True);
            Assert.That(regex.Match(context).IsMatch, Is.True);
        });
    }

    [Test]
    public void Should_Return_All_Text_Regex_Highlights()
    {
        var query = Parse("text=/error|warning/");
        var match = query.Match(Context(PacketDirection.Server, 0x13, "error warning"));

        Assert.That(match.Highlights, Is.EqualTo(new[]
        {
            new TraceQueryHighlight(0, 5, TraceQueryHighlightSource.Data),
            new TraceQueryHighlight(6, 7, TraceQueryHighlightSource.Data)
        }));
    }

    [Test]
    public void Should_Merge_Overlapping_Byte_Highlights()
    {
        var query = Parse("data=65 65");
        var match = query.Match(Context(PacketDirection.Server, 0x13, [0x65, 0x65, 0x65]));

        Assert.That(match.Highlights, Is.EqualTo(
            new[] { new TraceQueryHighlight(0, 3, TraceQueryHighlightSource.Data) }));
    }

    [Test]
    public void Should_Distinguish_Raw_And_Data_Highlights()
    {
        var query = Parse("raw=AA 00, data=65 01");
        var context = Context(
            PacketDirection.Server,
            0x13,
            [0x65, 0x01],
            raw: [0xAA, 0x00, 0x02, 0x13]);
        var match = query.Match(context);

        Assert.That(match.Highlights, Is.EqualTo(new[]
        {
            new TraceQueryHighlight(0, 2, TraceQueryHighlightSource.Data),
            new TraceQueryHighlight(0, 2, TraceQueryHighlightSource.Raw)
        }));
    }

    [Test]
    public void Should_Support_Sequence_Values_And_Ranges()
    {
        var query = Parse("sequence=02|04-06");

        Assert.Multiple(() =>
        {
            Assert.That(query.Match(Context(PacketDirection.Client, 0x45, sequence: 0x02)).IsMatch, Is.True);
            Assert.That(query.Match(Context(PacketDirection.Client, 0x45, sequence: 0x05)).IsMatch, Is.True);
            Assert.That(query.Match(Context(PacketDirection.Client, 0x45, sequence: 0x03)).IsMatch, Is.False);
        });
    }

    [TestCase("data=65 0", "Invalid data byte")]
    [TestCase("server=47-45", "Range start")]
    [TestCase("server=17-11", "Range start")]
    [TestCase("text=\"missing", "Missing closing quote")]
    [TestCase("payload=65", "Did you mean 'data'")]
    [TestCase("text=/test/i", "Regex flags are not supported")]
    [TestCase("text=/(?=test)/", "Unsupported regular expression")]
    [TestCase("text=\"café\"", "ASCII characters only")]
    [TestCase("(server=13", "Missing closing parenthesis")]
    [TestCase("server=13 AND", "after 'AND'")]
    [TestCase("NOT", "after 'NOT'")]
    public void Should_Return_Actionable_Validation_Diagnostics(string text, string expectedMessage)
    {
        var result = TraceQueryParser.Parse(text);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Diagnostic?.Message, Does.Contain(expectedMessage));
            Assert.That(result.Diagnostic?.Position, Is.GreaterThanOrEqualTo(0));
        });
    }

    [Test]
    public void Should_Allow_Commas_Inside_Quoted_And_Regex_Values()
    {
        var quoted = Parse("text=\"hello, world\"");
        var regex = Parse("text=/hello, world/");
        var context = Context(PacketDirection.Server, 0x13, "hello, world");

        Assert.Multiple(() =>
        {
            Assert.That(quoted.Match(context).IsMatch, Is.True);
            Assert.That(regex.Match(context).IsMatch, Is.True);
        });
    }

    [Test]
    public void Should_Identify_Only_Supported_Query_Keywords()
    {
        const string text = "server=13, unknown=1, text=/name=foo/, name=\"client=x\"";
        var spans = TraceQuerySyntax.GetKeywordSpans(text);
        var keywords = spans.Select(span => text.Substring(span.Start, span.Length));

        Assert.That(keywords, Is.EqualTo(new[] { "server", "text", "name" }));
    }

    [Test]
    public void Should_Identify_Boolean_Keywords_And_Grouping_Syntax()
    {
        const string text = "(server=13 OR NOT client!=12) AND text=\"Test Value\" OR name=/silo/";
        var syntax = TraceQuerySyntax.GetSpans(text)
            .Select(span => (text.Substring(span.Start, span.Length), span.Kind));

        Assert.That(syntax, Is.EqualTo(new[]
        {
            ("(", TraceQuerySyntaxKind.Grouping),
            ("server", TraceQuerySyntaxKind.Keyword),
            ("OR", TraceQuerySyntaxKind.Keyword),
            ("NOT", TraceQuerySyntaxKind.Keyword),
            ("client", TraceQuerySyntaxKind.Keyword),
            (")", TraceQuerySyntaxKind.Grouping),
            ("AND", TraceQuerySyntaxKind.Keyword),
            ("text", TraceQuerySyntaxKind.Keyword),
            ("OR", TraceQuerySyntaxKind.Keyword),
            ("name", TraceQuerySyntaxKind.Keyword)
        }));
    }

    private static TraceQuery Parse(string text, bool isTextCaseSensitive = true)
    {
        var result = TraceQueryParser.Parse(text, isTextCaseSensitive);
        Assert.That(result.Diagnostic, Is.Null, result.Diagnostic?.Message);
        return result.Query!;
    }

    private static TraceQueryContext Context(
        PacketDirection direction,
        byte command,
        string data = "",
        string? name = null,
        byte? sequence = null,
        byte[]? raw = null) =>
        Context(direction, command, Encoding.ASCII.GetBytes(data), name, sequence, raw);

    private static TraceQueryContext Context(
        PacketDirection direction,
        byte command,
        byte[] data,
        string? name = null,
        byte? sequence = null,
        byte[]? raw = null) =>
        new(direction, command, name, sequence, data, raw ?? []);
}
