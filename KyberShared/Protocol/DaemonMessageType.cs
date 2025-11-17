namespace KyberShared.Protocol;

public enum DaemonMessageType : byte
{
    ClientHello = 0x01,
    ServerHello = 0x02,
    KeyExchange = 0x03,
    HandshakeAck = 0x04,

    AuthRequest = 0x10,
    AuthResponse = 0x11,
    AuthSuccess = 0x12,
    AuthFailure = 0x13,

    SessionCommand = 0x20,
    SessionOutput = 0x21,
    SessionClose = 0x22,

    KeepAlive = 0x30,
    EncryptedPayload = 0x40,

    Error = 0xFF
}
