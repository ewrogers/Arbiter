using Arbiter.Net.Server;

namespace Arbiter.Net.Tests;

public class NetworkPacketBufferTests
{
    // A server 0x7E Server Hello packet (this packet is not encrypted)
    private static readonly byte[] ServerHelloPacket =
    [
        0xAA, 0x00, 0x13, 0x7E, 0x1B, 0x43, 0x4F, 0x4E, 0x4E, 0x45, 0x43, 0x54, 0x45, 0x44, 0x20, 0x53, 0x45, 0x52,
        0x56, 0x45, 0x52, 0x0A
    ];
    
    // A server 0x00 Server List packet (this packet is not encrypted)
    private static readonly byte[] ServerListPacket =
    [
        0xAA, 0x00, 0x11, 0x00, 0x00, 0x4B, 0xDA, 0x85, 0x42, 0x08, 0x09, 0x7A, 0x5D, 0x4B, 0x67, 0x53, 0x7E, 0x42,
        0x48, 0x5A
    ];

    // A server 0x03 TransferServer packet (this packet is not encrypted)
    private static readonly byte[] ServerRedirectPacket =
    [
        0xAA, 0x00, 0x23, 0x03, 0x01, 0x00, 0x00, 0x7F, 0x0A, 0x32, 0x1B, 0x08, 0x09, 0x7A, 0x5D, 0x4B, 0x67, 0x53,
        0x7E, 0x42, 0x48, 0x5A, 0x0B, 0x73, 0x6F, 0x63, 0x6B, 0x65, 0x74, 0x5B, 0x32, 0x36, 0x39, 0x5D, 0x00, 0x00,
        0x0E, 0x47
    ];

    private NetworkPacketBuffer _serverBuffer;

    [SetUp]
    public void Setup()
    {
        _serverBuffer = new NetworkPacketBuffer((command, data) => new ServerPacket(command, data));
    }

    [Test]
    public void Should_Not_Dequeue_Until_Packet_Is_Complete()
    {
        foreach (var singleByte in ServerHelloPacket)
        {
            Assert.That(_serverBuffer.TryTakePacket(out _), Is.False);
            _serverBuffer.Append(singleByte);
        }

        Assert.That(_serverBuffer.TryTakePacket(out _), Is.True);
    }

    [Test]
    public void Should_Pass_Command_And_Payload_To_Factory()
    {
        var rawPacket = ServerHelloPacket;

        var wasCalled = false;
        NetworkPacketFactory factory = (command, data) =>
        {
            Assert.That(command, Is.EqualTo(rawPacket[3]));

            var dataBytes = data.ToArray();
            Assert.That(dataBytes, Is.EqualTo(rawPacket[4..]));

            wasCalled = true;
            return new ServerPacket(command, data);
        };

        var buffer = new NetworkPacketBuffer(factory);

        buffer.Append(rawPacket);
        buffer.TryTakePacket(out _);

        Assert.That(wasCalled, Is.True);
    }

    [Test]
    public void Should_Dequeue_Packet_When_Complete()
    {
        _serverBuffer.Append(ServerHelloPacket);
        var didTake = _serverBuffer.TryTakePacket(out var packet);

        Assert.Multiple(() =>
        {
            Assert.That(didTake, Is.True);
            Assert.That(packet, Is.Not.Null);
        });

        Assert.Multiple(() =>
        {
            Assert.That(packet.Command, Is.EqualTo(ServerHelloPacket[3]));
            Assert.That(packet.Data, Is.EqualTo(ServerHelloPacket[4..]));
        });
    }

    [Test]
    public void Should_Dequeue_Packets_In_Order()
    {
        List<byte[]> packets = [ServerHelloPacket, ServerListPacket, ServerRedirectPacket];

        foreach (var t in packets)
        {
            _serverBuffer.Append(t);
        }
        
        foreach (var t in packets)
        {
            Assert.Multiple(() =>
            {
                Assert.That(_serverBuffer.TryTakePacket(out var packet), Is.True);
                Assert.That(packet.Command, Is.EqualTo(t[3]));
                Assert.That(packet.Data, Is.EqualTo(t[4..]));
            });
        }
    }
}
