using Arbiter.Net.Annotations;

namespace Arbiter.Net.Client.Messages;

[NetworkCommand(ClientCommand.Look)]
public class ClientLookMessage : ClientMessage
{
    // No additional data
}
