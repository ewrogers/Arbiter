using System;
using System.Collections.Generic;
using System.ComponentModel;
using Arbiter.App.Models.Tracing.Queries;
using Arbiter.App.Threading;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Arbiter.App.ViewModels.Tracing;

public partial class TraceViewModel
{
    private readonly Debouncer _searchRefreshDebouncer = new(TimeSpan.FromMilliseconds(50), Dispatcher.UIThread);
    private readonly List<int> _searchResultIndexes = [];

    [ObservableProperty] private bool _showSearchBar;
    [ObservableProperty] private bool _showSearchResults;
    
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FormattedSearchResultsText))]
    [NotifyPropertyChangedFor(nameof(HasSearchResults))]
    private int _selectedSearchIndex;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FormattedSearchResultsText))]
    [NotifyPropertyChangedFor(nameof(HasSearchResults))]
    private int _searchResultCount;
    
    public TraceSearchViewModel SearchParameters { get; } = new();

    public string? FormattedSearchResultsText =>
        !SearchParameters.Query.IsEmpty
            ? SearchResultCount > 0 ? $"{Math.Max(1, SelectedSearchIndex)} of {SearchResultCount}" : "no matches"
            : null;

    public bool HasSearchResults => SearchResultCount > 0;
    public bool IsSearchActive => !SearchParameters.Query.IsEmpty;

    [RelayCommand]
    private void ToggleSearchBar()
    {
        ShowSearchBar = !ShowSearchBar;
    }

    private void OnSearchParametersChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(TraceSearchViewModel.Query))
        {
            return;
        }

        OnPropertyChanged(nameof(IsSearchActive));
        OnPropertyChanged(nameof(FormattedSearchResultsText));
        RefreshSearchResults();
    }

    private void RefreshSearchResults()
    {
        ShowSearchResults = false;
        var query = SearchParameters.Query;
        
        _searchRefreshDebouncer.Execute(() =>
        {
            _searchResultIndexes.Clear();
            SearchResultCount = 0;
            
            for (var i = 0; i < FilteredPackets.Count; i++)
            {
                var packet = FilteredPackets[i];
                var match = MatchSearch(packet, query);
                ApplySearchMatch(packet, match);
                if (match.IsMatch && !query.IsEmpty)
                {
                    AddSearchResultIndex(i);
                }
            }

            SelectedSearchIndex = 0;
            ShowSearchResults = !query.IsEmpty;
        });
    }

    private void AddSearchResultIndex(int index)
    {
        _searchResultIndexes.Add(index);
        SearchResultCount = _searchResultIndexes.Count;
    }

    private static TraceQueryMatch MatchSearch(TracePacketViewModel vm, TraceQuery query)
    {
        if (query.IsEmpty)
        {
            return TraceQueryMatch.MatchWithoutHighlights;
        }

        var data = vm.WasReplaced && vm.FilteredPacket is not null
            ? vm.FilteredPacket.Data
            : vm.DecryptedPacket.Data;

        return query.Match(new TraceQueryContext(
            vm.Direction,
            vm.Command,
            vm.ClientName,
            vm.Sequence,
            data,
            vm.RawData));
    }

    private static void ApplySearchMatch(TracePacketViewModel vm, TraceQueryMatch match)
    {
        vm.SearchHighlights = match.Highlights;
        vm.Opacity = match.IsMatch ? 1 : 0.5;
    }

    [RelayCommand]
    private void GotoPreviousSearchResult()
    {
        if (_searchResultIndexes.Count == 0)
        {
            return;
        }

        var currentIndex = SelectedIndex;
        var pos = _searchResultIndexes.FindLastIndex(x => x < currentIndex);
        if (pos == -1)
        {
            pos = _searchResultIndexes.Count - 1;
        }

        SelectedSearchIndex = pos + 1;
        var packetIndex = _searchResultIndexes[pos];
        SelectItemByIndex(packetIndex);
    }

    [RelayCommand]
    private void GotoNextSearchResult()
    {
        if (_searchResultIndexes.Count == 0)
        {
            return;
        }

        var currentIndex = SelectedIndex;
        var pos = _searchResultIndexes.FindIndex(x => x > currentIndex);
        if (pos == -1)
        {
            pos = 0;
        }

        SelectedSearchIndex = pos + 1;
        var packetIndex = _searchResultIndexes[pos];
        SelectItemByIndex(packetIndex);
    }
}
