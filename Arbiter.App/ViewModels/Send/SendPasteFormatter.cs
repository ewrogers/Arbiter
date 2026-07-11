using System;
using System.Globalization;
using System.Text;

namespace Arbiter.App.ViewModels.Send;

public static class SendPasteFormatter
{
    public static string Format(string text)
    {
        var value = text.Trim();
        if (TryFormatHex(value, out var formattedHex))
        {
            return formattedHex;
        }

        if (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var decimalValue) &&
            decimalValue is >= int.MinValue and <= uint.MaxValue)
        {
            return $"#{decimalValue.ToString(CultureInfo.InvariantCulture)}";
        }

        return text;
    }

    private static bool TryFormatHex(string value, out string formatted)
    {
        formatted = string.Empty;
        if (!value.StartsWith("0x", StringComparison.OrdinalIgnoreCase) || value.Length == 2)
        {
            return false;
        }

        var digits = value.AsSpan(2);
        foreach (var digit in digits)
        {
            if (!char.IsAsciiHexDigit(digit))
            {
                return false;
            }
        }

        var normalizedDigits = digits.Length % 2 == 0 ? digits.ToString() : $"0{digits}";
        var builder = new StringBuilder(normalizedDigits.Length + normalizedDigits.Length / 2 - 1);
        for (var i = 0; i < normalizedDigits.Length; i += 2)
        {
            if (i > 0)
            {
                builder.Append(' ');
            }

            builder.Append(char.ToUpperInvariant(normalizedDigits[i]));
            builder.Append(char.ToUpperInvariant(normalizedDigits[i + 1]));
        }

        formatted = builder.ToString();
        return true;
    }
}
