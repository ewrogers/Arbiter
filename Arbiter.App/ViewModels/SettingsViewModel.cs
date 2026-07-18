using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Arbiter.App.Models.Settings;
using Arbiter.App.Services.Dialogs;
using Arbiter.App.Services.Settings;
using Arbiter.App.ViewModels.Filters;
using Arbiter.App.Views;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Arbiter.App.ViewModels;

public partial class SettingsViewModel : ViewModelBase, IDialogResult<ArbiterSettings>
{
    private static readonly FilePickerFileType ExecutableType = new("All Executables")
    {
        Patterns = ["*.exe"],
        MimeTypes = ["application/octet-stream"],
    };

    private readonly IDialogService _dialogService;
    private readonly ISettingsService _settingsService;
    private readonly IStorageProvider _storageProvider;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ClientExecutablePath))]
    [NotifyPropertyChangedFor(nameof(SkipIntroVideo))]
    [NotifyPropertyChangedFor(nameof(SuppressLoginNotice))]
    [NotifyPropertyChangedFor(nameof(ApplyModifiersKeyFix))]
    [NotifyPropertyChangedFor(nameof(SkipQuantityPromptInExchange))]
    [NotifyPropertyChangedFor(nameof(ShowItemQuantityInDialogs))]
    [NotifyPropertyChangedFor(nameof(LocalPort))]
    [NotifyPropertyChangedFor(nameof(RemoteServerAddress))]
    [NotifyPropertyChangedFor(nameof(RemoteServerPort))]
    [NotifyPropertyChangedFor(nameof(TraceOnStartup))]
    [NotifyPropertyChangedFor(nameof(TraceAutosave))]
    [NotifyPropertyChangedFor(nameof(TraceMaxHistory))]
    [NotifyPropertyChangedFor(nameof(DebugShowDialogId))]
    [NotifyPropertyChangedFor(nameof(DebugShowPursuitId))]
    [NotifyPropertyChangedFor(nameof(DebugShowDialogItemQuantity))]
    [NotifyPropertyChangedFor(nameof(DebugShowEquipmentDurability))]
    [NotifyPropertyChangedFor(nameof(DebugShowNpcId))]
    [NotifyPropertyChangedFor(nameof(DebugShowMonsterId))]
    [NotifyPropertyChangedFor(nameof(DebugShowMonsterClickId))]
    [NotifyPropertyChangedFor(nameof(DebugShowHiddenPlayers))]
    [NotifyPropertyChangedFor(nameof(DebugShowPlayerNames))]
    [NotifyPropertyChangedFor(nameof(DebugUseClassicEffects))]
    [NotifyPropertyChangedFor(nameof(DebugDisableBlind))]
    [NotifyPropertyChangedFor(nameof(DebugEnableTabMap))]
    [NotifyPropertyChangedFor(nameof(DebugEnableZoomedOutMap))]
    [NotifyPropertyChangedFor(nameof(DebugDisableWeatherEffects))]
    [NotifyPropertyChangedFor(nameof(DebugDisableDarkness))]
    [NotifyPropertyChangedFor(nameof(DebugIgnoreEmptyMessages))]
    [NotifyPropertyChangedFor(nameof(DebugEnableNpcModMenu))]
    [NotifyPropertyChangedFor(nameof(DebugEnableTrueLook))]
    [NotifyPropertyChangedFor(nameof(MessageFilterCount))]
    private ArbiterSettings _settings = new();

    [ObservableProperty] private int _selectedTabIndex;
    [ObservableProperty] private bool _hasChanges;

    public string MessageFilterCount => GetHumanizedFilterCount();

    public string VersionString
    {
        get
        {
            var version = Assembly.GetEntryAssembly()!.GetName().Version!;
            return $"v{version.Major}.{version.Minor}.{version.Build}";
        }
    }

    public string ClientExecutablePath
    {
        get => Settings.ClientExecutablePath;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ValidationException("Client executable path cannot be empty");
            }

            Settings.ClientExecutablePath = value;
            OnPropertyChanged();
            HasChanges = true;
        }
    }

    public bool SkipIntroVideo
    {
        get => Settings.SkipIntroVideo;
        set
        {
            Settings.SkipIntroVideo = value;
            OnPropertyChanged();
            HasChanges = true;
        }
    }

    public bool SuppressLoginNotice
    {
        get => Settings.SuppressLoginNotice;
        set
        {
            Settings.SuppressLoginNotice = value;
            OnPropertyChanged();
            HasChanges = true;
        }
    }

    public bool ApplyModifiersKeyFix
    {
        get => Settings.ApplyModifiersKeyFix;
        set
        {
            Settings.ApplyModifiersKeyFix = value;
            OnPropertyChanged();
            HasChanges = true;
        }
    }

    public bool SkipQuantityPromptInExchange
    {
        get => Settings.SkipQuantityPromptInExchange;
        set
        {
            Settings.SkipQuantityPromptInExchange = value;
            OnPropertyChanged();
            HasChanges = true;
        }
    }

    public bool ShowItemQuantityInDialogs
    {
        get => Settings.ShowItemQuantityInDialogs;
        set
        {
            Settings.ShowItemQuantityInDialogs = value;
            OnPropertyChanged();
            HasChanges = true;
        }
    }

    public int LocalPort
    {
        get => Settings.LocalPort;
        set
        {
            Settings.LocalPort = value;
            OnPropertyChanged();
            HasChanges = true;
        }
    }

    public string RemoteServerAddress
    {
        get => Settings.RemoteServerAddress;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ValidationException("Remote server address cannot be empty");
            }

            Settings.RemoteServerAddress = value;
            OnPropertyChanged();
            HasChanges = true;
        }
    }

    public int RemoteServerPort
    {
        get => Settings.RemoteServerPort;
        set
        {
            Settings.RemoteServerPort = value;
            OnPropertyChanged();
            HasChanges = true;
        }
    }

    public bool TraceOnStartup
    {
        get => Settings.TraceOnStartup;
        set
        {
            Settings.TraceOnStartup = value;
            OnPropertyChanged();
            HasChanges = true;
        }
    }

    public bool TraceAutosave
    {
        get => Settings.TraceAutosave;
        set
        {
            Settings.TraceAutosave = value;
            OnPropertyChanged();
            HasChanges = true;
        }
    }

    public int TraceMaxHistory
    {
        get => Settings.TraceMaxHistory;
        set
        {
            Settings.TraceMaxHistory = value;
            OnPropertyChanged();
            HasChanges = true;
        }
    }

    public bool DebugShowDialogId
    {
        get => Settings.Debug.ShowDialogId;
        set
        {
            Settings.Debug.ShowDialogId = value;
            OnPropertyChanged();
            HasChanges = true;
        }
    }

    public bool DebugShowPursuitId
    {
        get => Settings.Debug.ShowPursuitId;
        set
        {
            Settings.Debug.ShowPursuitId = value;
            OnPropertyChanged();
            HasChanges = true;
        }
    }

    public bool DebugShowDialogItemQuantity
    {
        get => Settings.Debug.ShowDialogItemQuantity;
        set
        {
            Settings.Debug.ShowDialogItemQuantity = value;
            OnPropertyChanged();
            HasChanges = true;
        }
    }

    public bool DebugShowEquipmentDurability
    {
        get => Settings.Debug.ShowEquipmentDurability;
        set
        {
            Settings.Debug.ShowEquipmentDurability = value;
            OnPropertyChanged();
            HasChanges = true;
        }
    }

    public bool DebugShowNpcId
    {
        get => Settings.Debug.ShowNpcId;
        set
        {
            Settings.Debug.ShowNpcId = value;
            OnPropertyChanged();
            HasChanges = true;
        }
    }

    public bool DebugShowMonsterId
    {
        get => Settings.Debug.ShowMonsterId;
        set
        {
            Settings.Debug.ShowMonsterId = value;
            OnPropertyChanged();
            HasChanges = true;
        }
    }

    public bool DebugShowMonsterClickId
    {
        get => Settings.Debug.ShowMonsterClickId;
        set
        {
            Settings.Debug.ShowMonsterClickId = value;
            OnPropertyChanged();
            HasChanges = true;
        }
    }

    public bool DebugShowHiddenPlayers
    {
        get => Settings.Debug.ShowHiddenPlayers;
        set
        {
            Settings.Debug.ShowHiddenPlayers = value;
            OnPropertyChanged();
            HasChanges = true;
        }
    }

    public bool DebugShowPlayerNames
    {
        get => Settings.Debug.ShowPlayerNames;
        set
        {
            Settings.Debug.ShowPlayerNames = value;
            OnPropertyChanged();
            HasChanges = true;
        }
    }

    public bool DebugUseClassicEffects
    {
        get => Settings.Debug.UseClassicEffects;
        set
        {
            Settings.Debug.UseClassicEffects = value;
            OnPropertyChanged();
            HasChanges = true;
        }
    }

    public bool DebugDisableBlind
    {
        get => Settings.Debug.DisableBlind;
        set
        {
            Settings.Debug.DisableBlind = value;
            OnPropertyChanged();
            HasChanges = true;
        }
    }

    public bool DebugEnableTabMap
    {
        get => Settings.Debug.EnableTabMap;
        set
        {
            Settings.Debug.EnableTabMap = value;
            OnPropertyChanged();
            HasChanges = true;
        }
    }

    public bool DebugEnableZoomedOutMap
    {
        get => Settings.Debug.EnableZoomedOutMap;
        set
        {
            Settings.Debug.EnableZoomedOutMap = value;
            OnPropertyChanged();
            HasChanges = true;
        }
    }

    public bool DebugDisableWeatherEffects
    {
        get => Settings.Debug.DisableWeatherEffects;
        set
        {
            Settings.Debug.DisableWeatherEffects = value;
            OnPropertyChanged();
            HasChanges = true;
        }
    }

    public bool DebugDisableDarkness
    {
        get => Settings.Debug.DisableDarkness;
        set
        {
            Settings.Debug.DisableDarkness = value;
            OnPropertyChanged();
            HasChanges = true;
        }
    }

    public bool DebugIgnoreEmptyMessages
    {
        get => Settings.Debug.IgnoreEmptyMessages;
        set
        {
            Settings.Debug.IgnoreEmptyMessages = value;
            OnPropertyChanged();
            HasChanges = true;
        }
    }

    public bool DebugEnableNpcModMenu
    {
        get => Settings.Debug.EnableNpcModMenu;
        set
        {
            Settings.Debug.EnableNpcModMenu = value;
            OnPropertyChanged();
            HasChanges = true;
        }
    }

    public bool DebugEnableTrueLook
    {
        get => Settings.Debug.EnableTrueLook;
        set
        {
            Settings.Debug.EnableTrueLook = value;
            OnPropertyChanged();
            HasChanges = true;
        }
    }

    public SettingsViewModel(IDialogService dialogService, ISettingsService settingsService,
        IStorageProvider storageProvider)
    {
        _dialogService = dialogService;
        _settingsService = settingsService;
        _storageProvider = storageProvider;

        _ = LoadSettingsAsync();
    }

    public event Action<ArbiterSettings?>? RequestClose;

    private async Task LoadSettingsAsync()
    {
        Settings = await _settingsService.LoadFromFileAsync();
        InitializeTraceDefaultCommands();
    }

    private string GetHumanizedFilterCount()
    {
        var totalCount = Settings.MessageFilters.Count;

        if (totalCount == 0)
        {
            return "No Filters";
        }

        var disabledCount = Settings.MessageFilters.Count(x => !x.IsEnabled);

        if (disabledCount == 0)
        {
            return totalCount == 1 ? "1 Filter" : $"{totalCount} Filters";
        }

        return disabledCount == totalCount
            ? $"{disabledCount} Disabled"
            : $"{totalCount} Filters ({disabledCount} Disabled)";
    }

    [RelayCommand]
    private async Task OnLocateClient()
    {
        var existingFolder =
            await _storageProvider.TryGetFolderFromPathAsync(
                Path.GetDirectoryName(ClientExecutablePath) ?? ArbiterSettings.DefaultPath);

        var files = await _storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select Client Executable",
            FileTypeFilter =
            [
                ExecutableType,
            ],
            SuggestedFileName = "Darkages.exe",
            SuggestedStartLocation = existingFolder,
            AllowMultiple = false
        });

        var selectedFile = files.Count > 0 ? files[0] : null;

        if (selectedFile is not null)
        {
            ClientExecutablePath = selectedFile.TryGetLocalPath() ?? string.Empty;
        }
    }

    [RelayCommand]
    private void HandleOk()
    {
        RequestClose?.Invoke(Settings);
    }

    [RelayCommand]
    private void HandleCancel()
    {
        RequestClose?.Invoke(null);
    }

    [RelayCommand]
    private void HandleResetDefaults()
    {
        Settings = new ArbiterSettings();
        InitializeTraceDefaultCommands();
        HasChanges = true;
    }

    [RelayCommand]
    private async Task EditMessageFilters()
    {
        // Load the filters into the list
        var vm = new MessageFilterListViewModel();
        foreach (var filter in Settings.MessageFilters)
        {
            vm.Filters.Add(new MessageFilterViewModel
            {
                IsEnabled = filter.IsEnabled,
                Pattern = filter.Pattern
            });
        }

        var newFilters =
            await _dialogService
                .ShowDialogAsync<MessageFiltersView, MessageFilterListViewModel, List<MessageFilter>>(vm);

        if (newFilters is null)
        {
            return;
        }

        Settings.MessageFilters = newFilters;
        HasChanges = true;

        OnPropertyChanged(nameof(MessageFilterCount));
    }

    [RelayCommand]
    private static void VisitProjectWebsite()
    {
        Process.Start(new ProcessStartInfo("https://github.com/ewrogers/Arbiter") { UseShellExecute = true });
    }
}
