using Arbiter.Net.Annotations;

namespace Arbiter.Net.Client.Messages;

[NetworkCommand(ClientCommand.Look)]
public class ClientLookAheadMessage : ClientMessage
{
    // No additional data
}
