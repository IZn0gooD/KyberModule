using System;
using System.IO;
using System.Security;

namespace KyberShared.Protocol;

public static class HandshakeMessages
{
    public const byte CurrentVersion = 1;

    public sealed record ClientHello(byte Version, byte[] KyberPublicKey, byte[] DilithiumPublicKey);
    public sealed record ServerHello(byte Version, byte[] KyberPublicKey, byte[] DilithiumPublicKey);
    public sealed record KeyExchange(byte[] Ciphertext, byte[] Signature);

    public static ClientHello ReadClientHello(DaemonMessage message)
    {
        if (message.Type != DaemonMessageType.ClientHello)
        {
            throw new InvalidDataException("Message ClientHello attendu");
        }

        using var ms = new MemoryStream(message.Payload, writable: false);
        using var reader = new BinaryReader(ms);

        var version = reader.ReadByte();
        var kyberLength = reader.ReadInt32();
        var kyberKey = reader.ReadBytes(kyberLength);
        var dilithiumLength = reader.ReadInt32();
        var dilithiumKey = reader.ReadBytes(dilithiumLength);

        return new ClientHello(version, kyberKey, dilithiumKey);
    }

    public static DaemonMessage CreateServerHello(byte[] kyberPublicKey, byte[] dilithiumPublicKey)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        writer.Write(CurrentVersion);
        writer.Write(kyberPublicKey.Length);
        writer.Write(kyberPublicKey);
        writer.Write(dilithiumPublicKey.Length);
        writer.Write(dilithiumPublicKey);

        return new DaemonMessage(DaemonMessageType.ServerHello, ms.ToArray());
    }

    public static KeyExchange ReadKeyExchange(DaemonMessage message)
    {
        if (message.Type != DaemonMessageType.KeyExchange)
        {
            throw new InvalidDataException("Message KeyExchange attendu");
        }

        using var ms = new MemoryStream(message.Payload, writable: false);
        using var reader = new BinaryReader(ms);

        var cipherLength = reader.ReadInt32();
        var ciphertext = reader.ReadBytes(cipherLength);
        var signatureLength = reader.ReadInt32();
        var signature = reader.ReadBytes(signatureLength);

        return new KeyExchange(ciphertext, signature);
    }

    public static ServerHello ReadServerHello(DaemonMessage message)
    {
        if (message.Type != DaemonMessageType.ServerHello)
        {
            throw new InvalidDataException("Message ServerHello attendu");
        }

        using var ms = new MemoryStream(message.Payload, writable: false);
        using var reader = new BinaryReader(ms);

        var version = reader.ReadByte();
        var kyberLength = reader.ReadInt32();
        var kyberKey = reader.ReadBytes(kyberLength);
        var dilithiumLength = reader.ReadInt32();
        var dilithiumKey = reader.ReadBytes(dilithiumLength);

        return new ServerHello(version, kyberKey, dilithiumKey);
    }

    public static DaemonMessage CreateHandshakeAck()
    {
        return new DaemonMessage(DaemonMessageType.HandshakeAck, Array.Empty<byte>());
    }
}
