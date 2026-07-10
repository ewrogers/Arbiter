using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using Arbiter.App.ViewModels.Tracing;
using Arbiter.Net.Client;
using Arbiter.Net.Server;
using CommunityToolkit.Mvvm.Input;

namespace Arbiter.App.ViewModels;

public partial class SettingsViewModel
{
    public ObservableCollection<CommandFilterViewModel> TraceDefaultClientCommands { get; } = [];
    public ObservableCollection<CommandFilterViewModel> TraceDefaultServerCommands { get; } = [];

    private void InitializeTraceDefaultCommands()
    {
        ClearTraceDefaultCommands(TraceDefaultClientCommands);
        ClearTraceDefaultCommands(TraceDefaultServerCommands);

        var defaultClientCommands = Settings.TraceDefaultClientCommands?.ToHashSet();
        var defaultServerCommands = Settings.TraceDefaultServerCommands?.ToHashSet();

        foreach (var command in Enum.GetValues<ClientCommand>()
                     .OrderBy(command => command == ClientCommand.Unknown ? 1 : 0)
                     .ThenBy(command => command.ToString()))
        {
            AddTraceDefaultCommand(
                TraceDefaultClientCommands,
                new CommandFilterViewModel(command, defaultClientCommands?.Contains((byte)command) ?? true));
        }

        foreach (var command in Enum.GetValues<ServerCommand>()
                     .OrderBy(command => command == ServerCommand.Unknown ? 1 : 0)
                     .ThenBy(command => command.ToString()))
        {
            AddTraceDefaultCommand(
                TraceDefaultServerCommands,
                new CommandFilterViewModel(command, defaultServerCommands?.Contains((byte)command) ?? true));
        }
    }

    private void AddTraceDefaultCommand(
        ObservableCollection<CommandFilterViewModel> commands,
        CommandFilterViewModel command)
    {
        command.PropertyChanged += OnTraceDefaultCommandPropertyChanged;
        commands.Add(command);
    }

    private void ClearTraceDefaultCommands(ObservableCollection<CommandFilterViewModel> commands)
    {
        foreach (var command in commands)
        {
            command.PropertyChanged -= OnTraceDefaultCommandPropertyChanged;
        }

        commands.Clear();
    }

    private void OnTraceDefaultCommandPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(CommandFilterViewModel.IsSelected))
        {
            return;
        }

        UpdateTraceDefaultSettings();
        HasChanges = true;
    }

    private void UpdateTraceDefaultSettings()
    {
        Settings.TraceDefaultClientCommands = TraceDefaultClientCommands
            .Where(command => command.IsSelected && command.Value is not null)
            .Select(command => command.Value!.Value)
            .ToList();
        Settings.TraceDefaultServerCommands = TraceDefaultServerCommands
            .Where(command => command.IsSelected && command.Value is not null)
            .Select(command => command.Value!.Value)
            .ToList();
    }

    private static void SetTraceDefaultCommandSelection(
        IEnumerable<CommandFilterViewModel> commands,
        bool isSelected)
    {
        foreach (var command in commands)
        {
            command.IsSelected = isSelected;
        }
    }

    [RelayCommand]
    private void SelectAllDefaultClientCommands() =>
        SetTraceDefaultCommandSelection(TraceDefaultClientCommands, true);

    [RelayCommand]
    private void SelectNoDefaultClientCommands() =>
        SetTraceDefaultCommandSelection(TraceDefaultClientCommands, false);

    [RelayCommand]
    private void SelectAllDefaultServerCommands() =>
        SetTraceDefaultCommandSelection(TraceDefaultServerCommands, true);

    [RelayCommand]
    private void SelectNoDefaultServerCommands() =>
        SetTraceDefaultCommandSelection(TraceDefaultServerCommands, false);
}
