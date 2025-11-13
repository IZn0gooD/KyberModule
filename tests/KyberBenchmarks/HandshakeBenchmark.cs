using System;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using KyberCLI.Commands;
using KyberCLI.Protocol;
using KyberDomain.Cryptography;
using KyberIntegrationTests;

namespace KyberBenchmarks;

[MemoryDiagnoser]
public class HandshakeBenchmark
{
    private DaemonTestHost? _host;

    [GlobalSetup]
    public async Task Setup()
    {
        _host = await DaemonTestHost.StartAsync();
    }

    [GlobalCleanup]
    public async Task Cleanup()
    {
        if (_host is not null)
        {
            await _host.DisposeAsync();
        }
    }

    [Benchmark(Description = "Handshake complet (connexion + fermeture)")]
    public async Task PerformHandshake()
    {
        if (_host is null)
        {
            throw new InvalidOperationException("Host not initialized");
        }

        var options = new ConnectOptions
        {
            Host = "127.0.0.1",
            Port = _host.Port,
            Username = _host.Username,
            PasswordPlain = _host.Password,
            PasswordRecord = _host.PasswordRecord,
            Command = string.Empty
        };

        var client = new KyberDaemonClient(options);
        var exitCode = await client.RunAsync();
        if (exitCode != 0)
        {
            throw new InvalidOperationException($"Handshake échoué: {exitCode}");
        }
    }
}
