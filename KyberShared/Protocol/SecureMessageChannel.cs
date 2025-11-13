using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using KyberDomain.Cryptography;

namespace KyberShared.Protocol;

public static class SecureMessageChannel
{
    public static async Task SendAsync(Stream stream, SecureSession session, bool isClient, DaemonMessage message, CancellationToken cancellationToken)
    {
        var plain = message.Serialize();
        var (ciphertext, nonce) = session.EncryptData(plain, isClient);

        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);
        writer.Write(nonce.Length);
        writer.Write(nonce);
        writer.Write(ciphertext.Length);
        writer.Write(ciphertext);

        var envelope = new DaemonMessage(DaemonMessageType.EncryptedPayload, ms.ToArray());
        await envelope.SendAsync(stream, cancellationToken);
    }

    public static async Task<DaemonMessage?> ReceiveAsync(Stream stream, SecureSession session, bool isClient, CancellationToken cancellationToken)
    {
        var envelope = await DaemonMessage.ReceiveAsync(stream, cancellationToken);
        if (envelope == null)
        {
            return null;
        }

        if (envelope.Type != DaemonMessageType.EncryptedPayload)
        {
            throw new InvalidDataException("Message non chiffré reçu après l'établissement de la session sécurisée");
        }

        using var ms = new MemoryStream(envelope.Payload, writable: false);
        using var reader = new BinaryReader(ms);

        var nonceLength = reader.ReadInt32();
        var nonce = reader.ReadBytes(nonceLength);
        var cipherLength = reader.ReadInt32();
        var ciphertext = reader.ReadBytes(cipherLength);

        var plain = session.DecryptData(ciphertext, nonce, isClient);
        return DaemonMessage.Deserialize(plain);
    }
}
