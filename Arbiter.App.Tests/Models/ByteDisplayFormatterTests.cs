using Arbiter.App.Models;

namespace Arbiter.App.Tests.Models;

public sealed class ByteDisplayFormatterTests
{
    [Test]
    public void Should_Replace_Non_Printable_Ascii_Bytes_With_Dots()
    {
        byte[] bytes = [0x41, 0x20, 0x00, 0x21, 0x7E, 0x7F, 0xFF];

        var result = ByteDisplayFormatter.ToAscii(bytes);

        Assert.That(result, Is.EqualTo("A..!~.."));
    }

    [Test]
    public void Should_Return_Empty_Text_For_Empty_Bytes()
    {
        Assert.That(ByteDisplayFormatter.ToAscii([]), Is.Empty);
    }
}
