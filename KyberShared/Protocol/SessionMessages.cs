using System;
using System.IO;
using System.Text;

namespace KyberShared.Protocol;

public static class SessionMessages
{
    public sealed record SessionCommandPayload(string Command);
    public sealed record SessionOutputPayload(bool Success, string Output, string Error);
    public sealed record SessionClosePayload(string Reason);

    public static DaemonMessage CreateSessionCommand(string command)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms, Encoding.UTF8, leaveOpen: false);
        WriteString(writer, command);
        return new DaemonMessage(DaemonMessageType.SessionCommand, ms.ToArray());
    }

    public static SessionCommandPayload ReadSessionCommand(DaemonMessage message)
    {
        if (message.Type != DaemonMessageType.SessionCommand)
        {
            throw new InvalidDataException("Message SessionCommand attendu");
        }

        using var ms = new MemoryStream(message.Payload, writable: false);
        using var reader = new BinaryReader(ms, Encoding.UTF8, leaveOpen: false);
        return new SessionCommandPayload(ReadString(reader));
    }

    public static DaemonMessage CreateSessionOutput(bool success, string output, string error)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms, Encoding.UTF8, leaveOpen: false);
        writer.Write(success ? (byte)1 : (byte)0);
        WriteString(writer, output ?? string.Empty);
        WriteString(writer, error ?? string.Empty);
        return new DaemonMessage(DaemonMessageType.SessionOutput, ms.ToArray());
    }

    public static SessionOutputPayload ReadSessionOutput(DaemonMessage message)
    {
        if (message.Type != DaemonMessageType.SessionOutput)
        {
            throw new InvalidDataException("Message SessionOutput attendu");
        }

        using var ms = new MemoryStream(message.Payload, writable: false);
        using var reader = new BinaryReader(ms, Encoding.UTF8, leaveOpen: false);

        var success = reader.ReadByte() != 0;
        var output = ReadString(reader);
        var error = ReadString(reader);
        return new SessionOutputPayload(success, output, error);
    }

    public static DaemonMessage CreateSessionClose(string reason)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms, Encoding.UTF8, leaveOpen: false);
        WriteString(writer, reason);
        return new DaemonMessage(DaemonMessageType.SessionClose, ms.ToArray());
    }

    public static SessionClosePayload ReadSessionClose(DaemonMessage message)
    {
        if (message.Type != DaemonMessageType.SessionClose)
        {
            throw new InvalidDataException("Message SessionClose attendu");
        }

        using var ms = new MemoryStream(message.Payload, writable: false);
        using var reader = new BinaryReader(ms, Encoding.UTF8, leaveOpen: false);
        return new SessionClosePayload(ReadString(reader));
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
