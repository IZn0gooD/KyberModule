using System;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using KyberIntegrationTests;

namespace KyberBenchmarks;

[MemoryDiagnoser]
public class CommandLatencyBenchmark
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

    [Benchmark(Description = "Latence commande echo")]
    public async Task CommandLatency()
    {
        if (_host is null)
        {
            throw new InvalidOperationException("Host not initialized");
        }

        var result = await CliRunner.RunCommandAsync(_host, "echo BenchLatency", captureOutput: false);
        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException($"Commande échouée: {result.ExitCode}");
        }
    }
}
