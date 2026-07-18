using System;

namespace Arbiter.App.Models.Settings;

public class DebugSettings : ICloneable
{
    public bool ShowDialogId { get; set; }
    public bool ShowPursuitId { get; set; }
    public bool ShowEquipmentDurability { get; set; }
    public bool ShowNpcId { get; set; }
    public bool ShowMonsterId { get; set; }
    public bool ShowMonsterClickId { get; set; }
    public bool ShowHiddenPlayers { get; set; }
    public bool ShowPlayerNames { get; set; }
    public bool UseClassicEffects { get; set; }
    public bool DisableBlind { get; set; }
    public bool DisableWeatherEffects { get; set; }
    public bool DisableDarkness { get; set; }
    public bool EnableTabMap { get; set; }
    public bool EnableZoomedOutMap { get; set; }
    public bool IgnoreEmptyMessages { get; set; }
    public bool EnableNpcModMenu { get; set; }
    public bool EnableTrueLook { get; set; }

    public object Clone() => new DebugSettings
    {
        ShowDialogId = ShowDialogId,
        ShowPursuitId = ShowPursuitId,
        ShowEquipmentDurability = ShowEquipmentDurability,
        ShowNpcId = ShowNpcId,
        ShowMonsterId = ShowMonsterId,
        ShowMonsterClickId = ShowMonsterClickId,
        ShowHiddenPlayers = ShowHiddenPlayers,
        ShowPlayerNames = ShowPlayerNames,
        UseClassicEffects = UseClassicEffects,
        DisableBlind = DisableBlind,
        EnableTabMap = EnableTabMap,
        EnableZoomedOutMap = EnableZoomedOutMap,
        DisableWeatherEffects = DisableWeatherEffects,
        DisableDarkness = DisableDarkness,
        IgnoreEmptyMessages = IgnoreEmptyMessages,
        EnableNpcModMenu = EnableNpcModMenu,
        EnableTrueLook = EnableTrueLook
    };
}
