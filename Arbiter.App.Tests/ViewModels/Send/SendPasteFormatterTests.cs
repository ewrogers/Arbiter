using Arbiter.App.ViewModels.Send;

namespace Arbiter.App.Tests.ViewModels.Send;

public sealed class SendPasteFormatterTests
{
    [TestCase("0x121", "01 21")]
    [TestCase("0Xabcdef", "AB CD EF")]
    [TestCase("0x1", "01")]
    [TestCase("0x0001", "00 01")]
    [TestCase(" 0x121 ", "01 21")]
    public void Should_Format_Prefixed_Hex_As_Padded_Bytes(string input, string expected)
    {
        Assert.That(SendPasteFormatter.Format(input), Is.EqualTo(expected));
    }

    [TestCase("13BBFF", "13 BB FF")]
    [TestCase("abcdef", "AB CD EF")]
    [TestCase("ABC", "0A BC")]
    [TestCase(" FF ", "FF")]
    public void Should_Format_Unprefixed_Hex_With_Letters_As_Padded_Bytes(string input, string expected)
    {
        Assert.That(SendPasteFormatter.Format(input), Is.EqualTo(expected));
    }

    [TestCase("12", "#12")]
    [TestCase("00012", "#12")]
    [TestCase("-12", "#-12")]
    [TestCase("+12", "#12")]
    [TestCase("4294967295", "#4294967295")]
    public void Should_Format_Decimal_As_Number_Token(string input, string expected)
    {
        Assert.That(SendPasteFormatter.Format(input), Is.EqualTo(expected));
    }

    [TestCase("01 21")]
    [TestCase("#12")]
    [TestCase("0x")]
    [TestCase("0x12ZZ")]
    [TestCase("4294967296")]
    [TestCase("hello")]
    [TestCase("12\n13")]
    public void Should_Leave_Non_Scalar_Or_Already_Formatted_Text_Unchanged(string input)
    {
        Assert.That(SendPasteFormatter.Format(input), Is.EqualTo(input));
    }
}
