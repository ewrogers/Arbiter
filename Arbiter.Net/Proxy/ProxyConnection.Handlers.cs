using System.Net;
using Arbiter.Net.Security;
using Arbiter.Net.Serialization;

namespace Arbiter.Net.Proxy;

public partial class ProxyConnection
{
    private void HandleClientTransferServer(NetworkPacket packet)
    {
        CompleteTransfer();

        var reader = new NetworkPacketReader(packet);
        var seed = reader.ReadByte();
        var keyLength = reader.ReadByte();
        var key = reader.ReadBytes(keyLength);
        var name = reader.ReadString8();
        var connectionId = reader.ReadUInt32();

        // Update connection name based on client request
        if (IsValidCharacterName(name))
        {
            Name = name;
        }

        SetEncryptionParameters(seed, key, name);
        
        ClientAuthenticated?.Invoke(this, EventArgs.Empty);
    }
    
    private void HandleServerSetEncryption(NetworkPacket packet)
    {
        var reader = new NetworkPacketReader(packet);
        var _ = reader.ReadByte();
        var serverTableCrc = reader.ReadUInt32();
        var seed = reader.ReadByte();
        var keyLength = reader.ReadByte();
        var key = reader.ReadBytes(keyLength);

        SetEncryptionParameters(seed, key);
    }

    private NetworkRedirectEventArgs HandleServerRedirect(NetworkPacket packet)
    {
        BeginTransfer();

        var reader = new NetworkPacketReader(packet);
        var remoteIpAddress = reader.ReadIPv4Address();
        var remotePort = reader.ReadUInt16();

        reader.ReadByte();

        var seed = reader.ReadByte();
        var keyLength = reader.ReadByte();
        var key = reader.ReadBytes(keyLength);

        var name = reader.ReadString8();
        var clientId = reader.ReadUInt32();

        // Redirect the client to the local endpoint instead
        var localIp = LocalEndpoint!.Address.GetAddressBytes();
        packet.Data[0] = localIp[3];
        packet.Data[1] = localIp[2];
        packet.Data[2] = localIp[1];
        packet.Data[3] = localIp[0];

        var localPort = LocalEndpoint!.Port;
        packet.Data[4] = (byte)((localPort >> 8) & 0xFF);
        packet.Data[5] = (byte)(localPort & 0xFF);

        // Update connection name based on server response
        if (IsValidCharacterName(name))
        {
            Name = name;
        }

        SetEncryptionParameters(seed, key, name);

        // Reset sequence counters
        Interlocked.Exchange(ref _clientSequence, 0);
        Interlocked.Exchange(ref _serverSequence, 0);
        
        var remoteEndpoint = new IPEndPoint(remoteIpAddress, remotePort);
        return new NetworkRedirectEventArgs(name, remoteEndpoint);
    }

    private void HandleServerSetUserId(NetworkPacket packet)
    {
        var reader = new NetworkPacketReader(packet);
        var userId = reader.ReadUInt32();

        IsLoggedIn = true;

        // Update the user ID based on server response
        UserId = userId;

        ClientLoggedIn?.Invoke(this, EventArgs.Empty);
    }

    private void HandleServerExitResponse(NetworkPacket packet)
    {
        var reader = new NetworkPacketReader(packet);
        var result = reader.ReadByte();

        IsLoggedIn = false;
        HasAuthenticated = false;
        UserId = null;

        ClientLoggedOut?.Invoke(this, EventArgs.Empty);
    }

    private void SetEncryptionParameters(int seed, ReadOnlySpan<byte> privateKey, string? name = null)
    {
        var encryptionParameters = new NetworkEncryptionParameters(seed, privateKey, name);

        // Create new encryptors based on the new encryption parameters
        _clientEncryptor.Parameters = encryptionParameters;
        _serverEncryptor.Parameters = encryptionParameters;
    }

    internal void BeginTransfer() => Interlocked.Exchange(ref _isTransferring, 1);

    internal void CompleteTransfer() => Interlocked.Exchange(ref _isTransferring, 0);

    private static bool IsValidCharacterName(string name) =>
        !string.IsNullOrWhiteSpace(name) && name.All(char.IsAsciiLetter);
}
