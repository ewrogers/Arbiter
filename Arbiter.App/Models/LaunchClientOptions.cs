namespace Arbiter.App.Models;

public record LaunchClientOptions(int LocalPort = 2610, bool SkipIntroVideo = true, bool SuppressLoginNotice = true,
    bool ApplyModifiersKeyFix = true);
