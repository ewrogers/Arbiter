using Arbiter.Net.Annotations;
using Arbiter.Net.Serialization;
using Arbiter.Net.Server.Types;
using Arbiter.Net.Types;

namespace Arbiter.Net.Server.Messages;

[NetworkCommand(ServerCommand.SelfLook)]
public class ServerSelfProfileMessage : ServerMessage
{
    public NationFlag Nation { get; set; }
    public string GuildRank { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string GroupMembers { get; set; } = string.Empty;
    public bool IsGroupOpen { get; set; }
    public bool IsRecruiting { get; set; }
    public ServerGroupBox? GroupBox { get; set; }
    public CharacterClass Class { get; set; }
    public bool ShowMasterMetadata { get; set; }
    public bool ShowAbilityMetadata { get; set; }
    public string DisplayClass { get; set; } = string.Empty;
    public string Guild { get; set; } = string.Empty;
    public List<ServerLegendMark> LegendMarks { get; set; } = [];

    public override void Deserialize(NetworkPacketReader reader)
    {
        base.Deserialize(reader);

        Nation = (NationFlag)reader.ReadByte();
        GuildRank = reader.ReadString8();
        Title = reader.ReadString8();
        GroupMembers = reader.ReadString8();
        IsGroupOpen = reader.ReadBoolean();
        IsRecruiting = reader.ReadBoolean();

        if (IsRecruiting)
        {
            // Yes the ordering of this is different from the client group box
            GroupBox = new ServerGroupBox
            {
                Leader = reader.ReadString8(),
                Name = reader.ReadString8(),
                Note = reader.ReadString8(),
                MinLevel = reader.ReadByte(),
                MaxLevel = reader.ReadByte(),
                MaxWarriors = reader.ReadByte(),
                CurrentWarriors = reader.ReadByte(),
                MaxWizards = reader.ReadByte(),
                CurrentWizards = reader.ReadByte(),
                MaxMonks = reader.ReadByte(),
                CurrentRogues = reader.ReadByte(),
                MaxPriests = reader.ReadByte(),
                CurrentPriests = reader.ReadByte(),
                MaxRogues = reader.ReadByte(),
                CurrentMonks = reader.ReadByte(),
            };
        }

        Class = (CharacterClass)reader.ReadByte();
        ShowAbilityMetadata = reader.ReadBoolean();
        ShowMasterMetadata = reader.ReadBoolean();
        DisplayClass = reader.ReadString8();
        Guild = reader.ReadString8();

        var legendMarkCount = reader.ReadByte();
        for (var i = 0; i < legendMarkCount; i++)
        {
            LegendMarks.Add(new ServerLegendMark
            {
                Icon = (LegendMarkIcon)reader.ReadByte(),
                Color = (LegendMarkColor)reader.ReadByte(),
                Key = reader.ReadString8(),
                Text = reader.ReadString8()
            });
        }
    }

    public override void Serialize(ref NetworkPacketBuilder builder)
    {
        base.Serialize(ref builder);

        builder.AppendByte((byte)Nation);
        builder.AppendString8(GuildRank);
        builder.AppendString8(Title);
        builder.AppendString8(GroupMembers);
        builder.AppendBoolean(IsGroupOpen);
        builder.AppendBoolean(IsRecruiting);

        if (IsRecruiting && GroupBox is not null)
        {
            builder.AppendString8(GroupBox.Leader);
            builder.AppendString8(GroupBox.Name); 
            builder.AppendString8(GroupBox.Note);
            builder.AppendByte(GroupBox.MinLevel);
            builder.AppendByte(GroupBox.MaxLevel);
            builder.AppendByte(GroupBox.MaxWarriors);
            builder.AppendByte(GroupBox.CurrentWarriors);
            builder.AppendByte(GroupBox.MaxWizards);
            builder.AppendByte(GroupBox.CurrentWizards);
            builder.AppendByte(GroupBox.MaxMonks);
            builder.AppendByte(GroupBox.CurrentRogues);
            builder.AppendByte(GroupBox.MaxPriests);
            builder.AppendByte(GroupBox.CurrentPriests);
            builder.AppendByte(GroupBox.MaxRogues);
            builder.AppendByte(GroupBox.CurrentMonks);
        }

        builder.AppendByte((byte)Class);
        builder.AppendBoolean(ShowAbilityMetadata);
        builder.AppendBoolean(ShowMasterMetadata);
        builder.AppendString8(DisplayClass);
        builder.AppendString8(Guild);

        builder.AppendByte((byte)LegendMarks.Count);
        foreach (var mark in LegendMarks)
        {
            builder.AppendByte((byte)mark.Icon);
            builder.AppendByte((byte)mark.Color);
            builder.AppendString8(mark.Key);
            builder.AppendString8(mark.Text);
        }
    }
}
