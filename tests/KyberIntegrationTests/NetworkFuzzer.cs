using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;

namespace KyberIntegrationTests;

internal static class NetworkFuzzer
{
    public static async Task SendRandomHandshakeAsync(int port, int size = 256)
    {
        using var client = new TcpClient();
        await client.ConnectAsync(IPAddress.Loopback, port);
        using var stream = client.GetStream();
        var buffer = new byte[size];
        RandomNumberGenerator.Fill(buffer);
        await stream.WriteAsync(buffer, 0, buffer.Length);
        await stream.FlushAsync();
    }
}
