using System;

namespace Arbiter.App.ViewModels.Player;

public static class CooldownFormatter
{
    public static string Format(DateTimeOffset? cooldownUntil, DateTimeOffset now)
    {
        return cooldownUntil.HasValue ? Format(cooldownUntil.Value - now) : string.Empty;
    }

    public static string Format(TimeSpan remaining)
    {
        var seconds = (long)Math.Ceiling(remaining.TotalSeconds);
        if (seconds <= 0)
        {
            return string.Empty;
        }

        return seconds >= 60
            ? $"{(seconds + 59) / 60}m"
            : $"{seconds}s";
    }
}
