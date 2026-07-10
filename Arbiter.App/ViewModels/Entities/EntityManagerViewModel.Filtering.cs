using System;
using System.Collections.Generic;
using Arbiter.App.Collections;
using Arbiter.App.Models.Entities;
using Arbiter.App.Threading;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Arbiter.App.ViewModels.Entities;

public partial class EntityManagerViewModel
{
    private readonly Debouncer _filterDebouncer = new(TimeSpan.FromMilliseconds(100), Dispatcher.UIThread);

    [ObservableProperty] private EntityFilterMode _filterMode = EntityFilterMode.Nearby;

    [ObservableProperty] private bool _includePlayers = true;
    [ObservableProperty] private bool _includeNpcs = true;
    [ObservableProperty] private bool _includeMonsters;
    [ObservableProperty] private bool _includeItems;
    [ObservableProperty] private bool _includeReactors;

    public List<EntityFilterMode> AvailableFilterModes =>
        [EntityFilterMode.All, EntityFilterMode.Map, EntityFilterMode.Nearby];

    public FilteredObservableCollection<EntityViewModel> FilteredEntities { get; }

    partial void OnFilterModeChanged(EntityFilterMode oldValue, EntityFilterMode newValue) =>
        _filterDebouncer.Execute(ReconcileFilter);

    partial void OnIncludePlayersChanged(bool oldValue, bool newValue) =>
        _filterDebouncer.Execute(ReconcileFilter);

    partial void OnIncludeNpcsChanged(bool oldValue, bool newValue) =>
        _filterDebouncer.Execute(ReconcileFilter);

    partial void OnIncludeMonstersChanged(bool oldValue, bool newValue) =>
        _filterDebouncer.Execute(ReconcileFilter);

    partial void OnIncludeItemsChanged(bool oldValue, bool newValue) =>
        _filterDebouncer.Execute(ReconcileFilter);

    partial void OnIncludeReactorsChanged(bool oldValue, bool newValue) =>
        _filterDebouncer.Execute(ReconcileFilter);

    private bool MatchesFilter(EntityViewModel entity)
    {
        // Determine if the entity type is allowed by toggles
        var typeAllowed = (IncludePlayers || !entity.Flags.HasFlag(EntityFlags.Player)) &&
                          (IncludeNpcs || !entity.Flags.HasFlag(EntityFlags.Mundane)) &&
                          (IncludeMonsters || !entity.Flags.HasFlag(EntityFlags.Monster)) &&
                          (IncludeItems || !entity.Flags.HasFlag(EntityFlags.Item)) &&
                          (IncludeReactors || !entity.Flags.HasFlag(EntityFlags.Reactor));
        
        if (FilterMode is not (EntityFilterMode.Map or EntityFilterMode.Nearby))
        {
            return typeAllowed;
        }
        
        // If no client or no player state, do not show anything
        if (SelectedClient is null || !_playerService.TryGetState(SelectedClient.Id, out var player))
        {
            return false;
        }

        // Always include self regardless of toggles
        if (player.UserId == entity.Id)
        {
            return true;
        }

        // Check if the entity is within range
        var isWithinRange = FilterMode switch
        {
            EntityFilterMode.Map => player.MapId == entity.MapId,
            EntityFilterMode.Nearby => player.MapId == entity.MapId && player is { MapX: not null, MapY: not null }
                                                                    && Math.Abs(player.MapX.Value - entity.X) < 15
                                                                    && Math.Abs(player.MapY.Value - entity.Y) < 15,
            _ => true
        };

        // Both spatial condition and type toggle must pass
        return isWithinRange && typeAllowed;
    }

    private void ReconcileFilter() => FilteredEntities.Reconcile();
}
