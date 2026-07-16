using Arbiter.Net.Annotations;
using Arbiter.Net.Serialization;
using Arbiter.Net.Server.Types;
using Arbiter.Net.Types;

namespace Arbiter.Net.Server.Messages;

[NetworkCommand(ServerCommand.Bulletin)]
public class ServerBoardResultMessage : ServerMessage
{
    public MessageBoardResult ResultType { get; set; }
    public List<ServerMessageBoardInfo> Boards { get; set; } = [];
    public MessageBoardType? BoardType { get; set; }
    public ushort? BoardId { get; set; }
    public string? BoardName { get; set; }
    public List<ServerMessageBoardPostListing> Posts { get; set; } = [];
    public ServerMessageBoardPost? Post { get; set; }
    public bool? CanNavigatePrev { get; set; }
    public bool? ResultSuccess { get; set; }
    public string? ResultMessage { get; set; }

    public override void Deserialize(NetworkPacketReader reader)
    {
        base.Deserialize(reader);

        ResultType = (MessageBoardResult)reader.ReadByte();

        if (ResultType == MessageBoardResult.BoardList)
        {
            var boardCount = reader.ReadUInt16();
            for (var i = 0; i < boardCount; i++)
            {
                Boards.Add(new ServerMessageBoardInfo
                {
                    Id = reader.ReadUInt16(),
                    Name = reader.ReadString8()
                });
            }
        }
        else if (ResultType is MessageBoardResult.Board or MessageBoardResult.Mailbox)
        {
            BoardType = (MessageBoardType)reader.ReadByte();
            BoardId = reader.ReadUInt16();
            BoardName = reader.ReadString8();

            var postCount = reader.ReadByte();
            for (var i = 0; i < postCount; i++)
            {
                Posts.Add(new ServerMessageBoardPostListing
                {
                    IsHighlighted = reader.ReadBoolean(),
                    Id = reader.ReadInt16(),
                    Author = reader.ReadString8(),
                    Month = reader.ReadByte(),
                    Day = reader.ReadByte(),
                    Subject = reader.ReadString8()
                });
            }
        }
        else if (ResultType is MessageBoardResult.Post or MessageBoardResult.MailLetter)
        {
            CanNavigatePrev = reader.ReadBoolean();

            Post = new ServerMessageBoardPost
            {
                IsHighlighted = reader.ReadBoolean(),
                Id = reader.ReadInt16(),
                Author = reader.ReadString8(),
                Month = reader.ReadByte(),
                Day = reader.ReadByte(),
                Subject = reader.ReadString8(),
                Body = reader.ReadString16()
            };
        }
        else if (ResultType is MessageBoardResult.PostSubmitted or MessageBoardResult.PostDeleted
                 or MessageBoardResult.PostHighlighted)
        {
            ResultSuccess = reader.ReadBoolean();
            ResultMessage = reader.ReadString8();
        }
    }

    public override void Serialize(ref NetworkPacketBuilder builder)
    {
        base.Serialize(ref builder);

        builder.AppendByte((byte)ResultType);

        if (ResultType == MessageBoardResult.BoardList)
        {
            builder.AppendUInt16((ushort)Boards.Count);
            foreach (var board in Boards)
            {
                builder.AppendUInt16(board.Id);
                builder.AppendString8(board.Name);
            }
        }
        else if (ResultType is MessageBoardResult.Board or MessageBoardResult.Mailbox)
        {
            builder.AppendByte((byte)Source!);
            builder.AppendUInt16(BoardId!.Value);
            builder.AppendString8(BoardName ?? string.Empty);

            builder.AppendByte((byte)Posts.Count);
            foreach (var post in Posts)
            {
                builder.AppendBoolean(post.IsHighlighted);
                builder.AppendInt16(post.Id);
                builder.AppendString8(post.Author);
                builder.AppendByte(post.Month);
                builder.AppendByte(post.Day);
                builder.AppendString8(post.Subject);
            }
        }
        else if (ResultType is MessageBoardResult.Post or MessageBoardResult.MailLetter)
        {
            builder.AppendBoolean(CanNavigatePrev!.Value);

            builder.AppendBoolean(Post!.IsHighlighted);
            builder.AppendInt16(Post.Id);
            builder.AppendString8(Post.Author);
            builder.AppendByte(Post.Month);
            builder.AppendByte(Post.Day);
            builder.AppendString8(Post.Subject);
            builder.AppendString16(Post.Body);
        }
        else if (ResultType is MessageBoardResult.PostSubmitted or MessageBoardResult.PostDeleted
                 or MessageBoardResult.PostHighlighted)
        {
            builder.AppendBoolean(ResultSuccess!.Value);
            builder.AppendString8(ResultMessage!);
        }
    }
}
