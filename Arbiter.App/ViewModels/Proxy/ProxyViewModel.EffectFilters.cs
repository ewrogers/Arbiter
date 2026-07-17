using System.Collections.Generic;
using Arbiter.App.Models.Settings;
using Arbiter.Net;
using Arbiter.Net.Filters;
using Arbiter.Net.Proxy;
using Arbiter.Net.Server.Messages;

namespace Arbiter.App.ViewModels.Proxy;

public partial class ProxyViewModel
{
    private static readonly Dictionary<int, (ushort Effect, ushort? DurationOverride)> ClassicEffectReplacements = new()
    {
        [279] = (2, null), // modern ao puin => classic ao puin
        [232] = (2, null), // modern ao dall/suain => classic ao dall/suain
        [267] = (4, null), // modern ioc => classic ioc
        [244] = (6, null), // modern dion => classic dion
        [271] = (6, null), // modern deireas faileas => classic deireas faileas
        [245] = (8, null), // modern ao curse => classic ao curse
        [234] = (9, null), // modern sal => classic sal
        [235] = (10, null), // modern mor sal => classic mor sal
        [236] = (11, null), // modern ard sal => classic ard sal
        [237] = (12, null), // modern srad => classic srad
        [238] = (13, null), // modern mor srad => classic mor srad
        [239] = (14, null), // modern ard srad => classic ard srad
        [240] = (15, null), // modern athar => classic athar
        [241] = (16, null), // modern mor athar => classic mor athar
        [242] = (17, null), // modern ard athar => classic ard athar
        [243] = (18, null), // modern mor cradh => classic mor cradh
        [280] = (19, null), // modern beannaich => classic beannaich
        [231] = (21, null), // modern aite => classic aite
        [273] = (23, null), // modern deo saighead / fas nadur => classic deo saighead / fas nadur
        [247] = (25, null), // modern poison bubble => classic pink swirl
        [248] = (26, null), // modern windblade => classic windblade
        [283] = (26, null), // modern stab and twist => classic stab and twist
        [274] = (27, null), // modern kick / rescue => classic kick / rescue
        [250] = (29, null), // modern creag => classic creag
        [251] = (30, null), // modern mor creag => classic mor creag
        [252] = (31, null), // modern ard creag => classic ard creag
        [257] = (43, null), // modern ard cradh => classic ard cradh
        [258] = (44, null), // modern cradh => classic cradh
        [259] = (45, null), // modern beag creag => classic beag creag
        [254] = (52, null), // modern mor strioch pian gar => classic mor strioch pian gar
        [380] = (71, null), // modern nuadhaich => classic nuadhaich
        [263] = (81, null), // modern pian na dion => classic pian na dion
        [278] = (195, null), // modern mor pian na dion => classic mor pian na dion
    };

    private NetworkFilterRef? _debugClassicEffectsFilter;
    private NetworkFilterRef? _debugNoBlindFilter;

    private void AddDebugEffectsFilters(DebugSettings settings)
    {
        _debugClassicEffectsFilter = _proxyServer.AddFilter<ServerEffectLayerMessage>(HandleShowEffectMessage,
            $"{FilterPrefix}_Effect_ServerShowEffect", DebugFilterPriority, settings);

        _debugNoBlindFilter = _proxyServer.AddFilter<ServerStatusMessage>(HandleUpdateStatsMessage,
            $"{FilterPrefix}_Effect_ServerUpdateStats", DebugFilterPriority, settings);
    }

    private void RemoveDebugEffectsFilters()
    {
        _debugClassicEffectsFilter?.Unregister();
        _debugNoBlindFilter?.Unregister();
    }

    private static NetworkPacket HandleShowEffectMessage(ProxyConnection connection, ServerEffectLayerMessage message,
        object? parameter, NetworkMessageFilterResult<ServerEffectLayerMessage> result)
    {
        if (parameter is not DebugSettings { UseClassicEffects: true })
        {
            return result.Passthrough();
        }

        var hasReplacement = false;

        // Replace the target animation if it exists
        if (ClassicEffectReplacements.TryGetValue(message.TargetAnimation, out var targetReplacement))
        {
            var (newEffect, animationDurationOverride) = targetReplacement;
            message.TargetAnimation = newEffect;
            message.AnimationDuration = animationDurationOverride ?? message.AnimationDuration;

            hasReplacement = true;
        }

        // Replace the source animation if it exists
        if (message.SourceAnimation.HasValue &&
            ClassicEffectReplacements.TryGetValue(message.SourceAnimation.Value, out var sourceReplacement))
        {
            var (newEffect, animationDurationOverride) = sourceReplacement;
            message.SourceAnimation = newEffect;
            message.AnimationDuration = animationDurationOverride ?? message.AnimationDuration;

            hasReplacement = true;
        }

        return hasReplacement ? result.Replace(message) : result.Passthrough();
    }

    private static NetworkPacket HandleUpdateStatsMessage(ProxyConnection connection, ServerStatusMessage message,
        object? parameter, NetworkMessageFilterResult<ServerStatusMessage> result)
    {
        if (message.IsBlinded is not true || parameter is not DebugSettings { DisableBlind: true })
        {
            return result.Passthrough();
        }

        message.IsBlinded = false;
        return result.Replace(message);
    }
}