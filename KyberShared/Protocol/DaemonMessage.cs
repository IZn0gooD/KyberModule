using System;
using System.Buffers.Binary;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace KyberShared.Protocol;

public sealed class DaemonMessage
{
    public DaemonMessageType Type { get; }
    public byte[] Payload { get; }

    public DaemonMessage(DaemonMessageType type, byte[] payload)
    {
        Type = type;
        Payload = payload;
    }

    public byte[] Serialize()
    {
        var payload = Payload ?? Array.Empty<byte>();
        var payloadLength = payload.Length;
        var buffer = new byte[1 + payloadLength];
        buffer[0] = (byte)Type;
        if (payloadLength > 0)
        {
            Buffer.BlockCopy(payload, 0, buffer, 1, payloadLength);
        }
        return buffer;
    }

    public static DaemonMessage Deserialize(ReadOnlySpan<byte> buffer)
    {
        if (buffer.Length == 0)
        {
            throw new InvalidDataException("Impossible de désérialiser un message vide");
        }

        var type = (DaemonMessageType)buffer[0];
        var payloadLength = buffer.Length - 1;
        byte[] payload = payloadLength > 0 ? buffer[1..].ToArray() : Array.Empty<byte>();
        return new DaemonMessage(type, payload);
    }

    public static async Task<DaemonMessage?> ReceiveAsync(Stream stream, CancellationToken cancellationToken)
    {
        var lengthBuffer = new byte[4];
        if (!await ReadExactAsync(stream, lengthBuffer, cancellationToken))
        {
            return null;
        }

        var totalLength = BinaryPrimitives.ReadInt32BigEndian(lengthBuffer);
        if (totalLength <= 0 || totalLength > 16 * 1024 * 1024)
        {
            throw new InvalidDataException($"Longueur de message invalide: {totalLength}");
        }

        var buffer = new byte[totalLength];
        if (!await ReadExactAsync(stream, buffer, cancellationToken))
        {
            return null;
        }

        return Deserialize(buffer);
    }

    public async Task SendAsync(Stream stream, CancellationToken cancellationToken)
    {
        var data = Serialize();
        var lengthBuffer = new byte[4];
        BinaryPrimitives.WriteInt32BigEndian(lengthBuffer, data.Length);

        await stream.WriteAsync(lengthBuffer, cancellationToken);
        await stream.WriteAsync(data, cancellationToken);
        await stream.FlushAsync(cancellationToken);
    }

    private static async Task<bool> ReadExactAsync(Stream stream, byte[] buffer, CancellationToken cancellationToken)
    {
        var offset = 0;
        while (offset < buffer.Length)
        {
            var read = await stream.ReadAsync(buffer.AsMemory(offset, buffer.Length - offset), cancellationToken);
            if (read == 0)
            {
                return false;
            }

            offset += read;
        }

        return true;
    }
}
