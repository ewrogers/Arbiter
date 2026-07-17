using Arbiter.Net;
using Arbiter.Net.Filters;
using Arbiter.Net.Proxy;
using Arbiter.Net.Server.Messages;
using Arbiter.Net.Types;

namespace Arbiter.App.ViewModels.Client;

public partial class ClientViewModel
{
    private NetworkFilterRef? _redirectFilter;
    
    private void RegisterFilters()
    {
        _redirectFilter = _connection.AddFilter<ServerTransferServerMessage>(HandleServerRedirectMessage, "Client_RedirectFilter", int.MaxValue);
    }
    
    private void UnregisterFilters()
    {
        _redirectFilter?.Unregister();
    }

    private NetworkPacket? HandleServerRedirectMessage(ProxyConnection connection, ServerTransferServerMessage message,
        object? parameter, NetworkMessageFilterResult<ServerTransferServerMessage> result)
    {
        if (!ShouldBlockNextRedirect)
        {
            return result.Passthrough();
        }

        // Queue a notice to the client that a redirect has been blocked
        var notificationMessage = new ServerSystemMessage
        {
            MessageType = WorldMessageType.BarMessage,
            Message = "Server redirect has been blocked. You are now in limbo."
        };
        connection.EnqueueMessage(notificationMessage);

        ShouldBlockNextRedirect = false;
        return result.Block();
    }
}
