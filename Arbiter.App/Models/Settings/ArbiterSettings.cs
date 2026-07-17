using System;
using System.Collections.Generic;
using System.Linq;
using Arbiter.App.Models.Entities;

namespace Arbiter.App.Models.Settings;

public class ArbiterSettings : ICloneable
{
    public static readonly string DefaultPath = System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "KRU", "Dark Ages", "Darkages.exe");

    public string ClientExecutablePath { get; set; } = DefaultPath;
    public bool SkipIntroVideo { get; set; } = true;
    public bool SuppressLoginNotice { get; set; } = true;
    public bool ApplyModifiersKeyFix { get; set; } = true;

    public int LocalPort { get; set; } = 2610;

    public string RemoteServerAddress { get; set; } = "da0.kru.com";
    public int RemoteServerPort { get; set; } = 2610;

    public bool TraceOnStartup { get; set; }
    public bool TraceAutosave { get; set; }
    public bool TraceDetailedView { get; set; } = true;
    public int TraceMaxHistory { get; set; } = 1000;
    public List<byte>? TraceDefaultClientCommands { get; set; }
    public List<byte>? TraceDefaultServerCommands { get; set; }

    public DebugSettings Debug { get; set; } = new();

    public WindowRect? StartupLocation { get; set; }

    public InterfacePanelState? LeftPanel { get; set; }
    public InterfacePanelState? RightPanel { get; set; }
    public InterfacePanelState? BottomPanel { get; set; }
    public int SettingsPanelIndex { get; set; }

    public List<MessageFilter> MessageFilters { get; set; } = [];

    public EntitySortOrder EntitySorting { get; set; } = EntitySortOrder.FirstSeen;
    
    public object Clone() => new ArbiterSettings
    {
        ClientExecutablePath = ClientExecutablePath,
        SkipIntroVideo = SkipIntroVideo,
        SuppressLoginNotice = SuppressLoginNotice,
        ApplyModifiersKeyFix = ApplyModifiersKeyFix,
        LocalPort = LocalPort,
        RemoteServerAddress = RemoteServerAddress,
        RemoteServerPort = RemoteServerPort,
        TraceOnStartup = TraceOnStartup,
        TraceAutosave = TraceAutosave,
        TraceDetailedView = TraceDetailedView,
        TraceMaxHistory = Math.Clamp(TraceMaxHistory, 10, 1_000_000),
        TraceDefaultClientCommands = TraceDefaultClientCommands is null ? null : [.. TraceDefaultClientCommands],
        TraceDefaultServerCommands = TraceDefaultServerCommands is null ? null : [.. TraceDefaultServerCommands],
        Debug = Debug.Clone() as DebugSettings ?? new DebugSettings(),
        StartupLocation = StartupLocation?.Clone() as WindowRect,
        LeftPanel = LeftPanel?.Clone() as InterfacePanelState,
        RightPanel = RightPanel?.Clone() as InterfacePanelState,
        BottomPanel = BottomPanel?.Clone() as InterfacePanelState,
        SettingsPanelIndex = SettingsPanelIndex,
        MessageFilters = MessageFilters.Select(x => new MessageFilter { Pattern = x.Pattern }).ToList(),
        EntitySorting = EntitySorting
    };
}
