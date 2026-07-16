using Arbiter.Net.Annotations;
using Arbiter.Net.Client.Types;
using Arbiter.Net.Serialization;
using Arbiter.Net.Types;

namespace Arbiter.Net.Client.Messages;

[NetworkCommand(ClientCommand.Group)]
public class ClientGroupInviteMessage : ClientMessage
{
    public ClientGroupAction Action { get; set; }
    public string TargetName { get; set; } = string.Empty;
    
    public ClientGroupBox? GroupBox { get; set; }

    public override void Deserialize(NetworkPacketReader reader)
    {
        base.Deserialize(reader);

        Action = (ClientGroupAction)reader.ReadByte();
        TargetName = reader.ReadString8();

        if (Action == ClientGroupAction.RecruitStart)
        {
            GroupBox = new ClientGroupBox
            {
                Name = reader.ReadString8(),
                Note = reader.ReadString8(),
                MinLevel = reader.ReadByte(),
                MaxLevel = reader.ReadByte(),
                MaxWarriors = reader.ReadByte(),
                MaxWizards = reader.ReadByte(),
                MaxRogues = reader.ReadByte(),
                MaxPriests = reader.ReadByte(),
                MaxMonks = reader.ReadByte()
            };
        }
    }

    public override void Serialize(ref NetworkPacketBuilder builder)
    {
        base.Serialize(ref builder);
        
        builder.AppendByte((byte)Action);
        builder.AppendString8(TargetName);

        if (Action != ClientGroupAction.RecruitStart || GroupBox == null)
        {
            return;
        }
        
        builder.AppendString8(GroupBox.Name);
        builder.AppendString8(GroupBox.Note);
        builder.AppendByte(GroupBox.MinLevel);
        builder.AppendByte(GroupBox.MaxLevel);
        builder.AppendByte(GroupBox.MaxWarriors);
        builder.AppendByte(GroupBox.MaxWizards);
        builder.AppendByte(GroupBox.MaxRogues);
        builder.AppendByte(GroupBox.MaxPriests);
        builder.AppendByte(GroupBox.MaxMonks);
    }
}
