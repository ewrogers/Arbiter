using Arbiter.Net.Client;
using Arbiter.Net.Security;

namespace Arbiter.Net.Tests.Client;

public class ClientPacketEncryptorTests
{
    // A client 0x10 TransferServer packet (this packet is not encrypted)
    private static readonly byte[] AuthenticatePacketBytes =
    [
        0xAA, 0x00, 0x19, 0x10, 0x07, 0x09, 0x63, 0x3E, 0x5F, 0x41, 0x46, 0x78, 0x21, 0x68, 0x2C, 0x07, 0x4D, 0x6F,
        0x6E, 0x69, 0x74, 0x6F, 0x72, 0x00, 0x00, 0x0C, 0xD2, 0x00
    ];

    // A client 0x43 RequestObjectInfo packet (this packet is encrypted with static key)
    private static readonly byte[] InteractPacketBytes =
    [
        0xAA, 0x00, 0x0F, 0x43, 0x06, 0x6E, 0x32, 0x53, 0x56, 0x2C, 0x74, 0x02, 0x66, 0xD5, 0x03, 0xFD, 0x97, 0x15
    ];

    // Decrypted payload for the 0x43 RequestObjectInfo packet
    private static readonly byte[] InteractPacketPayload = [0x01, 0x00, 0x00, 0x1B, 0x66];

    // A client 0x4F Portrait/Bio packet (this packet is encrypted with hash key)
    private static readonly byte[] PortraitPacketBytes =
    [
        0xAA, 0x00, 0x8B, 0x4F, 0x01, 0x36, 0x48, 0x37, 0x32, 0x61, 0x41, 0x1F, 0x0C, 0x0D, 0x9A, 0xE9, 0xE8, 0xED,
        0xBE, 0xE4, 0xBB, 0xEE, 0xBE, 0x12, 0x12, 0x13, 0x16, 0x45, 0x1F, 0x40, 0x15, 0x45, 0x10, 0x10, 0x11, 0x4F,
        0x5A, 0x51, 0x0B, 0x3A, 0x47, 0x1E, 0x1E, 0x1F, 0x1A, 0x12, 0x0E, 0x09, 0x78, 0x05, 0x4B, 0x5D, 0x44, 0x4B,
        0x4B, 0x50, 0x1C, 0x54, 0x1E, 0x54, 0x5E, 0x36, 0x1E, 0x4D, 0x17, 0x48, 0x1D, 0x4D, 0x18, 0x43, 0x04, 0x55,
        0x18, 0x54, 0x1E, 0x5C, 0x07, 0x4F, 0x48, 0x40, 0x2F, 0x51, 0x0B, 0x54, 0x5A, 0x4C, 0x4E, 0x53, 0x44, 0x49,
        0x07, 0x40, 0x18, 0x44, 0x53, 0x52, 0x43, 0x57, 0x4F, 0x10, 0x41, 0x04, 0x49, 0x0C, 0x2D, 0x5B, 0x1C, 0x48,
        0x3B, 0x0D, 0x52, 0x07, 0x57, 0x0E, 0x0E, 0x0F, 0x0A, 0x59, 0x03, 0x5C, 0x09, 0x59, 0x0C, 0x0C, 0x0D, 0x08,
        0x5B, 0x01, 0x5E, 0x50, 0x46, 0x46, 0x45, 0x2B, 0x61, 0x11, 0x2B, 0x1D, 0xE3, 0xD9, 0xED, 0x8F
    ];

    // Decrypted payload for the 0x4F Portrait/Bio packet
    private static readonly byte[] PortraitPacketPayload =
    [
        0x00, 0x7E, 0x00, 0x00, 0x00, 0x7A, 0x7B, 0x3D, 0x6C, 0x53, 0x20, 0x20, 0x20, 0x20, 0x20,
        0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x7B, 0x3D, 0x6C,
        0x69, 0x0D, 0x20, 0x20, 0x20, 0x20, 0x20, 0x7B, 0x3D, 0x65, 0x41, 0x6C, 0x77, 0x61, 0x79, 0x73, 0x20, 0x61,
        0x72, 0x6F, 0x75, 0x6E, 0x64, 0x0D, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x7B, 0x3D, 0x69, 0x77, 0x61,
        0x74, 0x63, 0x68, 0x69, 0x6E, 0x67, 0x0D, 0x20, 0x20, 0x20, 0x7B, 0x3D, 0x6A, 0x77, 0x61, 0x69, 0x74, 0x69,
        0x6E, 0x67, 0x20, 0x70, 0x61, 0x74, 0x69, 0x65, 0x6E, 0x74, 0x6C, 0x79, 0x0D, 0x7B, 0x3D, 0x6C, 0x4C, 0x20,
        0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20,
        0x20, 0x7B, 0x3D, 0x6C, 0x6F
    ];

    // A client 0x39 Merchant packet (this packet is encrypted with hash key + dialog encryption)
    private static readonly byte[] DialogMenuChoicePacketBytes =
    [
        0xAA, 0x00, 0x18, 0x39, 0x0A, 0xB3, 0xB0, 0x55, 0x5F, 0x77, 0x41, 0x40, 0x43, 0x13, 0x5D, 0x70, 0x1C, 0xA0,
        0x27, 0x4B, 0xD4, 0x28, 0xCB, 0xFC, 0x1E, 0xC8, 0x5E
    ];

    // Decrypted payload for the 0x39 Merchant packet
    private static readonly byte[] DialogMenuChoicePayload = [0x01, 0x00, 0x00, 0x1B, 0x66, 0x04, 0xB9];

    // A client 0x3A Pursuit packet (this packet is encrypted with hash key + dialog encryption)
    private static readonly byte[] DialogChoicePacketBytes =
    [
        0xAA, 0x00, 0x19, 0x3A, 0x0B, 0x36, 0x9A, 0x5F, 0x4B, 0x3E, 0x1F, 0xF8, 0xB1, 0xEA, 0xBD, 0x9E, 0x98, 0x38,
        0x87, 0xE6, 0x35, 0x0C, 0x30, 0xDA, 0xC4, 0x37, 0x53, 0x55
    ];

    // Decrypted payload for the 0x3A Pursuit packet
    private static readonly byte[] DialogChoicePayload = [0x01, 0x00, 0x00, 0x1B, 0x66, 0x00, 0xB9, 0x00, 0x5C];

    // A client 0x62 Hello packet sent immediately after the server supplies its encryption key.
    private static readonly byte[] HelloPacketBytes =
    [
        0xAA, 0x00, 0x0E, 0x62, 0x00, 0x34, 0x00, 0x0A, 0x88, 0x6E, 0x23, 0x1E, 0xA4, 0x60, 0x7F, 0x4C, 0x6A
    ];

    private ClientPacketEncryptor _encryptor;

    [SetUp]
    public void Setup()
    {
        var parameters = new NetworkEncryptionParameters(0x07,
            new byte[] { 0x63, 0x3E, 0x5F, 0x41, 0x46, 0x78, 0x21, 0x68, 0x2C }, "Monitor");
        _encryptor = new ClientPacketEncryptor(parameters);
    }

    [Test]
    public void Should_Not_Decrypt_When_Not_Encrypted()
    {
        var rawBytes = AuthenticatePacketBytes;
        var command = rawBytes[3];
        var payload = rawBytes[4..];

        var clientPacket = new ClientPacket(command, payload.AsSpan());
        var decrypted = _encryptor.Decrypt(clientPacket);

        Assert.That(decrypted.Data, Has.Length.EqualTo(payload.Length));
        Assert.That(decrypted.Data, Is.EqualTo(payload));
    }

    [Test]
    public void Should_Retain_Command_When_Decrypting()
    {
        var rawBytes = InteractPacketBytes;
        var command = rawBytes[3];
        var payload = rawBytes[4..];

        var clientPacket = new ClientPacket(command, payload.AsSpan());
        var decrypted = _encryptor.Decrypt(clientPacket);

        Assert.That(decrypted.Command, Is.EqualTo(command));
    }

    [Test]
    public void Should_Retain_Checksum_When_Decrypting()
    {
        var rawBytes = InteractPacketBytes;
        var command = rawBytes[3];
        var payload = rawBytes[4..];

        var clientPacket = new ClientPacket(command, payload.AsSpan());
        var decrypted = _encryptor.Decrypt(clientPacket) as ClientPacket;

        var checksum = (uint)rawBytes[^7] << 24 | (uint)rawBytes[^6] << 16 | (uint)rawBytes[^5] << 8 | rawBytes[^4];
        Assert.That(decrypted!.Checksum, Is.EqualTo(checksum));
    }

    [Test]
    public void Should_Decrypt_With_Static_Key()
    {
        var rawBytes = InteractPacketBytes;
        var payload = rawBytes[4..];

        var clientPacket = new ClientPacket(rawBytes[3], payload.AsSpan());
        var decrypted = _encryptor.Decrypt(clientPacket);

        Assert.That(decrypted.Data, Is.EqualTo(InteractPacketPayload));
    }

    [Test]
    public void Should_Decrypt_Hello_With_Startup_Encryption_Parameters()
    {
        var encryptor = new ClientPacketEncryptor(NetworkEncryptionParameters.Default);
        var packet = new ClientPacket(HelloPacketBytes[3], HelloPacketBytes.AsSpan(4));

        var decrypted = encryptor.Decrypt(packet);

        Assert.That(decrypted.Data, Is.EqualTo(new byte[] { 0x61, 0x72, 0x61, 0x6D }));
    }

    [Test]
    public void Should_Decrypt_With_Hash_Key()
    {
        var rawBytes = PortraitPacketBytes;
        var payload = rawBytes[4..];

        var clientPacket = new ClientPacket(rawBytes[3], payload.AsSpan());
        var decrypted = _encryptor.Decrypt(clientPacket);

        Assert.That(decrypted.Data, Is.EqualTo(PortraitPacketPayload));
    }

    [Test]
    public void Should_Decrypt_Dialog_Menu_Choice()
    {
        var rawBytes = DialogMenuChoicePacketBytes;
        var payload = rawBytes[4..];

        var clientPacket = new ClientPacket(rawBytes[3], payload.AsSpan());
        var decrypted = _encryptor.Decrypt(clientPacket);

        Assert.That(decrypted.Data, Is.EqualTo(DialogMenuChoicePayload));
    }

    [Test]
    public void Should_Decrypt_Dialog_Choice()
    {
        var rawBytes = DialogChoicePacketBytes;
        var payload = rawBytes[4..];

        var clientPacket = new ClientPacket(rawBytes[3], payload.AsSpan());
        var decrypted = _encryptor.Decrypt(clientPacket);

        Assert.That(decrypted.Data, Is.EqualTo(DialogChoicePayload));
    }

    [Test]
    public void Should_Decrypt_Dialog_Key_Randoms()
    {
        List<(byte[], ushort)> testCases =
        [
            (DialogMenuChoicePacketBytes, 0xC391),
            (DialogChoicePacketBytes, 0x43B2),
        ];

        foreach (var (packet, expectedDialogRandom) in testCases)
        {
            var command = packet[3];
            var payload = packet[4..];

            var dialogRandom = _encryptor.DecryptDialogKey(command, payload.AsSpan());
            Assert.That(dialogRandom, Is.EqualTo(expectedDialogRandom));
        }
    }

    [Test]
    public void Should_Read_Hash_Key_Salt()
    {
        List<(byte[], ushort, byte)> testCases =
        [
            (InteractPacketBytes, 0x618D, 0xB4),
            (PortraitPacketBytes, 0xFBA9, 0xCE),
            (DialogMenuChoicePacketBytes, 0x2A6E, 0xEB),
            (DialogChoicePacketBytes, 0x2147, 0x70)
        ];

        foreach (var (packet, bRandExpected, sRandExpected) in testCases)
        {
            var (bRand, sRand) = ClientPacketEncryptor.ReadHashKeySalt(packet);
            Assert.Multiple(() =>
            {
                Assert.That(bRand, Is.EqualTo(bRandExpected));
                Assert.That(sRand, Is.EqualTo(sRandExpected));
            });
        }
    }

    [Test]
    public void Should_Not_Encrypt_When_Not_Encrypted()
    {
        var command = AuthenticatePacketBytes[3];
        var payload = AuthenticatePacketBytes[4..];

        var packet = new ClientPacket(command, payload.AsSpan());
        _encryptor.Encrypt(packet, 0x01);

        Assert.That(packet.Data, Is.EqualTo(payload));
    }

    [Test]
    public void Should_Encrypt_With_Static_Key()
    {
        var command = InteractPacketBytes[3];
        var sequence = InteractPacketBytes[4];
        var payload = InteractPacketPayload;

        var (bRand, sRand) = ClientPacketEncryptor.ReadHashKeySalt(InteractPacketBytes);

        var packet = new ClientPacket(command, payload.AsSpan());
        var encrypted = _encryptor.Encrypt(packet, sequence, bRand, sRand);

        var expectedEncrypted = InteractPacketBytes[4..];
        var actualEncrypted = encrypted.Data;

        Assert.That(actualEncrypted, Is.EqualTo(expectedEncrypted));
    }

    [Test]
    public void Should_Encrypt_With_Hash_Key()
    {
        var command = PortraitPacketBytes[3];
        var sequence = PortraitPacketBytes[4];
        var payload = PortraitPacketPayload;

        var (bRand, sRand) = ClientPacketEncryptor.ReadHashKeySalt(PortraitPacketBytes);

        var packet = new ClientPacket(command, payload.AsSpan());
        var encrypted = _encryptor.Encrypt(packet, sequence, bRand, sRand);

        Assert.That(encrypted.Data, Is.EqualTo(PortraitPacketBytes[4..]));
    }

    [Test]
    public void Should_Encrypt_Dialog_Menu_Choice()
    {
        var command = DialogMenuChoicePacketBytes[3];
        var sequence = DialogMenuChoicePacketBytes[4];
        var payload = DialogMenuChoicePayload;

        var (bRand, sRand) = ClientPacketEncryptor.ReadHashKeySalt(DialogMenuChoicePacketBytes);
        var dialogRandom = _encryptor.DecryptDialogKey(command, DialogMenuChoicePacketBytes.AsSpan(4));

        var packet = new ClientPacket(command, payload.AsSpan());
        var encrypted = _encryptor.Encrypt(packet, sequence, bRand, sRand, dialogRandom);

        Assert.That(encrypted.Data, Is.EqualTo(DialogMenuChoicePacketBytes[4..]));
    }

    [Test]
    public void Should_Encrypt_Dialog_Choice()
    {
        var command = DialogChoicePacketBytes[3];
        var sequence = DialogChoicePacketBytes[4];
        var payload = DialogChoicePayload;

        var (bRand, sRand) = ClientPacketEncryptor.ReadHashKeySalt(DialogChoicePacketBytes);
        var dialogRandom = _encryptor.DecryptDialogKey(command, DialogChoicePacketBytes.AsSpan(4));

        var packet = new ClientPacket(command, payload.AsSpan());
        var encrypted = _encryptor.Encrypt(packet, sequence, bRand, sRand, dialogRandom);

        Assert.That(encrypted.Data, Is.EqualTo(DialogChoicePacketBytes[4..]));
    }
    
    [Test]
    public void Should_Write_Hash_Key_Salt()
    {
        List<(byte[], ushort, byte)> testCases =
        [
            (InteractPacketBytes, 0x618D, 0xB4),
            (PortraitPacketBytes, 0xFBA9, 0xCE),
            (DialogMenuChoicePacketBytes, 0x2A6E, 0xEB),
            (DialogChoicePacketBytes, 0x2147, 0x70)
        ];

        var buffer = new byte[3];
        foreach (var (packet, bRandExpected, sRandExpected) in testCases)
        {
            var (bRand, sRand) = ClientPacketEncryptor.ReadHashKeySalt(packet);
            ClientPacketEncryptor.WriteHashKeySalt(buffer, bRand, sRand);

            Assert.Multiple(() =>
            {
                Assert.That(bRand, Is.EqualTo(bRandExpected));
                Assert.That(sRand, Is.EqualTo(sRandExpected));
                
                Assert.That(buffer[^1], Is.EqualTo(packet[^1]));
                Assert.That(buffer[^2], Is.EqualTo(packet[^2]));
                Assert.That(buffer[^3], Is.EqualTo(packet[^3]));
            });
        }
    }
    
    [Test]
    public void Should_Encrypt_And_Decrypt_Symmetrically()
    {
        List<(byte[], byte[])> testCases =
        [
            (InteractPacketPayload, InteractPacketBytes),
            (PortraitPacketPayload, PortraitPacketBytes),
            (DialogMenuChoicePayload, DialogMenuChoicePacketBytes),
            (DialogChoicePayload, DialogChoicePacketBytes)
        ];
        
        foreach(var (payload, expectedPacket) in testCases)
        {
            var command = expectedPacket[3];
            var sequence = expectedPacket[4];
            
            var (bRand, sRand) = ClientPacketEncryptor.ReadHashKeySalt(expectedPacket);
            var dialogRandom = _encryptor.DecryptDialogKey(command, expectedPacket.AsSpan(4));
            
            var encrypted = _encryptor.Encrypt(new ClientPacket(command, payload.AsSpan()), sequence, bRand, sRand, dialogRandom);
            var decrypted = _encryptor.Decrypt(encrypted);
            
            Assert.Multiple(() =>
            {
                Assert.That(encrypted.Data, Is.EqualTo(expectedPacket[4..]));
                Assert.That(decrypted.Data, Is.EqualTo(payload));
            });
        }
    }
}
