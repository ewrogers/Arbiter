using System;
using System.Collections.Generic;
using System.ComponentModel;
using Arbiter.App.Models.Tracing;
using Arbiter.App.Threading;
using Arbiter.Net.Client;
using Arbiter.Net.Server;
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
        SearchParameters.SelectedCommand is not null
            ? SearchResultCount > 0 ? $"{Math.Max(1, SelectedSearchIndex)} of {SearchResultCount}" : "no matches"
            : null;

    public bool HasSearchResults => SearchResultCount > 0;
    public bool IsSearchActive => SearchParameters.SelectedCommand?.Value is not null;

    [RelayCommand]
    private void ToggleSearchBar()
    {
        ShowSearchBar = !ShowSearchBar;
    }

    private void OnSearchParametersChanged(object? sender, PropertyChangedEventArgs e)
    {
        OnPropertyChanged(nameof(IsSearchActive));

        // Do not filter on this, wait for actual command byte to change
        if (e.PropertyName == nameof(SearchParameters.SelectedCommand))
        {

        }

        RefreshSearchResults();
    }

    private void RefreshSearchResults()
    {
        ShowSearchResults = false;
        
        _searchRefreshDebouncer.Execute(() =>
        {
            _searchResultIndexes.Clear();
            SearchResultCount = 0;
            
            for (var i = 0; i < FilteredPackets.Count; i++)
            {
                var packet = FilteredPackets[i];
                var isMatch = MatchesSearch(packet);
                if (isMatch)
                {
                    AddSearchResultIndex(i);
                }

                packet.Opacity = isMatch ? 1 : 0.5;
            }

            SelectedSearchIndex = 0;
            ShowSearchResults = SearchParameters.SelectedCommand?.Value is not null;
        });
    }

    private void AddSearchResultIndex(int index)
    {
        _searchResultIndexes.Add(index);
        SearchResultCount = _searchResultIndexes.Count;
    }

    private bool MatchesSearch(TracePacketViewModel vm)
    {
        if (SearchParameters.SelectedCommand?.Value is null)
        {
            return true;
        }

        if (vm.Direction != SearchParameters.SelectedCommand.Direction)
        {
            return false;
        }

        if (vm.Direction == PacketDirection.Client)
        {
            var command = (ClientCommand)vm.DecryptedPacket.Command;
            return (byte)command == SearchParameters.SelectedCommand.Value;
        }

        if (vm.Direction == PacketDirection.Server)
        {
            var command = (ServerCommand)vm.DecryptedPacket.Command;
            return (byte)command == SearchParameters.SelectedCommand.Value;
        }

        return false;
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
