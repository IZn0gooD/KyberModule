using System.IO;
using System.Text;

namespace KyberShared.Protocol;

public static class AuthMessages
{
    public sealed record AuthRequestPayload(byte[] Challenge, IReadOnlyList<string> Methods);
    public sealed record AuthResponsePayload(string Username, string Method, byte[] Data);

    public static DaemonMessage CreateAuthRequest(byte[] challenge, IEnumerable<string> methods)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms, Encoding.UTF8, leaveOpen: false);

        writer.Write(challenge.Length);
        writer.Write(challenge);

        var methodsList = methods.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        writer.Write(methodsList.Count);
        foreach (var method in methodsList)
        {
            WriteString(writer, method);
        }

        return new DaemonMessage(DaemonMessageType.AuthRequest, ms.ToArray());
    }

    public static AuthResponsePayload ReadAuthResponse(DaemonMessage message)
    {
        if (message.Type != DaemonMessageType.AuthResponse)
        {
            throw new InvalidDataException("Message AuthResponse attendu");
        }

        using var ms = new MemoryStream(message.Payload, writable: false);
        using var reader = new BinaryReader(ms, Encoding.UTF8, leaveOpen: false);

        var username = ReadString(reader);
        var method = ReadString(reader);
        var dataLength = reader.ReadInt32();
        var data = reader.ReadBytes(dataLength);

        return new AuthResponsePayload(username, method, data);
    }

    public static DaemonMessage CreateAuthSuccess()
    {
        return new DaemonMessage(DaemonMessageType.AuthSuccess, Array.Empty<byte>());
    }

    public static DaemonMessage CreateAuthFailure(string reason)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms, Encoding.UTF8, leaveOpen: false);
        WriteString(writer, reason);
        return new DaemonMessage(DaemonMessageType.AuthFailure, ms.ToArray());
    }

    public static string ReadAuthFailure(DaemonMessage message)
    {
        if (message.Type != DaemonMessageType.AuthFailure)
        {
            throw new InvalidDataException("Message AuthFailure attendu");
        }

        using var ms = new MemoryStream(message.Payload, writable: false);
        using var reader = new BinaryReader(ms, Encoding.UTF8, leaveOpen: false);
        return ReadString(reader);
    }

    public static AuthRequestPayload ReadAuthRequest(DaemonMessage message)
    {
        if (message.Type != DaemonMessageType.AuthRequest)
        {
            throw new InvalidDataException("Message AuthRequest attendu");
        }

        using var ms = new MemoryStream(message.Payload, writable: false);
        using var reader = new BinaryReader(ms, Encoding.UTF8, leaveOpen: false);

        var challengeLength = reader.ReadInt32();
        var challenge = reader.ReadBytes(challengeLength);
        var methodCount = reader.ReadInt32();
        var methods = new List<string>(methodCount);
        for (int i = 0; i < methodCount; i++)
        {
            methods.Add(ReadString(reader));
        }

        return new AuthRequestPayload(challenge, methods);
    }

    private static void WriteString(BinaryWriter writer, string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        writer.Write(bytes.Length);
        writer.Write(bytes);
    }

    private static string ReadString(BinaryReader reader)
    {
        var length = reader.ReadInt32();
        var bytes = reader.ReadBytes(length);
        return Encoding.UTF8.GetString(bytes);
    }
}
