using Arbiter.App.Models.Tracing;
using Arbiter.App.ViewModels.Tracing;
using Arbiter.Net.Filters;
using Arbiter.Net.Server;

namespace Arbiter.App.Tests.ViewModels.Tracing;

public sealed class TracePacketViewModelTests
{
    [Test]
    public void Should_Collapse_Empty_Data_But_Display_The_Raw_Packet()
    {
        var packet = new ServerPacket(0x13, []);
        var viewModel = new TracePacketViewModel(packet, packet, null);

        Assert.Multiple(() =>
        {
            Assert.That(viewModel.HasDisplayPayload, Is.False);
            Assert.That(viewModel.DisplayPayloadLines, Is.Empty);
        });

        viewModel.DisplayMode = PacketDisplayMode.Raw;

        Assert.Multiple(() =>
        {
            Assert.That(viewModel.HasDisplayPayload, Is.True);
            Assert.That(viewModel.DisplayPayloadLines, Has.Count.EqualTo(1));
            Assert.That(viewModel.DisplayPayloadLines[0].Hex, Is.EqualTo("AA 00 01 13"));
        });
    }

    [Test]
    public void Should_Display_Replacement_Payload_Bytes()
    {
        var input = new ServerPacket(0x13, [0x41]);
        var output = new ServerPacket(0x13, [0x42]);
        var filterResult = new NetworkFilterResult
        {
            Action = NetworkFilterAction.Replace,
            Input = input,
            Output = output
        };
        var viewModel = new TracePacketViewModel(input, input, filterResult);

        Assert.Multiple(() =>
        {
            Assert.That(viewModel.DisplayPayloadLines, Has.Count.EqualTo(1));
            Assert.That(viewModel.DisplayPayloadLines[0].Hex, Is.EqualTo("42"));
            Assert.That(viewModel.DisplayPayloadLines[0].Ascii, Is.EqualTo("B"));
        });
    }
}
