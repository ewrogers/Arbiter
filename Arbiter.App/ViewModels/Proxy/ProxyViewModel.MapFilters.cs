using Arbiter.App.Models.Settings;
using Arbiter.Net;
using Arbiter.Net.Filters;
using Arbiter.Net.Proxy;
using Arbiter.Net.Server.Messages;
using Arbiter.Net.Types;

namespace Arbiter.App.ViewModels.Proxy;

public partial class ProxyViewModel
{
    private NetworkFilterRef? _debugUserIdFilter;
    private NetworkFilterRef? _debugMapInfoFilter;

    private void AddDebugMapFilters(DebugSettings settings)
    {
        _debugUserIdFilter = _proxyServer.AddFilter<ServerUserAppearanceMessage>(HandleUserIdMessage,
            $"{FilterPrefix}_Map_ServerUserId", DebugFilterPriority, settings);

        _debugMapInfoFilter = _proxyServer.AddFilter<ServerMapSizeMessage>(HandleMapInfoMessage,
            $"{FilterPrefix}_Map_ServerMapInfo", DebugFilterPriority, settings);
    }

    private void RemoveDebugMapFilters()
    {
        _debugUserIdFilter?.Unregister();
        _debugMapInfoFilter?.Unregister();
    }

    private static NetworkPacket HandleUserIdMessage(ProxyConnection connection, ServerUserAppearanceMessage message,
        object? parameter, NetworkMessageFilterResult<ServerUserAppearanceMessage> result)
    {
        if (parameter is not DebugSettings filterSettings ||
            filterSettings is { EnableZoomedOutMap: false } ||
            !filterSettings.EnableZoomedOutMap || message.Class == CharacterClass.Rogue)
        {
            return result.Passthrough();
        }

        message.Class = CharacterClass.Rogue;
        return result.Replace(message);
    }

    private static NetworkPacket HandleMapInfoMessage(ProxyConnection connection, ServerMapSizeMessage message,
        object? parameter, NetworkMessageFilterResult<ServerMapSizeMessage> result)
    {
        if (parameter is not DebugSettings filterSettings || filterSettings is
                { EnableTabMap: false, DisableWeatherEffects: false, DisableDarkness: false })
        {
            return result.Passthrough();
        }

        var originalFlags = message.Flags;
        var newFlags = originalFlags;

        if (filterSettings.EnableTabMap)
        {
            newFlags &= ~MapFlags.NoMap;
        }

        if (filterSettings.DisableWeatherEffects)
        {
            newFlags &= ~(MapFlags.Rain | MapFlags.Snow | MapFlags.Winter);
        }

        if (filterSettings.DisableDarkness)
        {
            newFlags &= ~MapFlags.Darkness;
        }

        if (newFlags == originalFlags)
        {
            return result.Passthrough();
        }

        message.Flags = newFlags;
        return result.Replace(message);
    }
}