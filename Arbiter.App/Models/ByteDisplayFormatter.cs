using System;

namespace Arbiter.App.Models;

public static class ByteDisplayFormatter
{
    public static string ToAscii(ReadOnlySpan<byte> bytes)
    {
        return string.Create(bytes.Length, bytes.ToArray(), static (characters, values) =>
        {
            for (var i = 0; i < values.Length; i++)
            {
                var value = values[i];
                characters[i] = value is >= 0x21 and <= 0x7E ? (char)value : '.';
            }
        });
    }
}
