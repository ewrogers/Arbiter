using Arbiter.Net.Security;

namespace Arbiter.Net.Tests.Security;

public sealed class NetworkEncryptionParametersTests
{
    [Test]
    public void Should_Use_Documented_Startup_Key()
    {
        Assert.That(NetworkEncryptionParameters.Default.PrivateKey.ToArray(), Is.EqualTo(new byte[]
        {
            0x55, 0x72, 0x6B, 0xE5, 0x6E, 0x49, 0x74, 0xA3, 0x49
        }));
    }
}
