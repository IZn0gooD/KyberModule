using System;
using System.Linq;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using KyberIntegrationTests;

namespace KyberBenchmarks;

[MemoryDiagnoser]
public class ConcurrentCommandsBenchmark
{
    [Params(2, 4, 8)]
    public int ParallelClients { get; set; }

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

    [Benchmark(Description = "Commandes concurrentes")] 
    public async Task RunParallelCommands()
    {
        if (_host is null)
        {
            throw new InvalidOperationException("Host not initialized");
        }

        var tasks = Enumerable.Range(0, ParallelClients)
            .Select(_ => CliRunner.RunCommandAsync(_host, "echo Parallel", captureOutput: false));

        var results = await Task.WhenAll(tasks);
        foreach (var result in results)
        {
            if (result.ExitCode != 0)
            {
                throw new InvalidOperationException($"Commande échouée: {result.ExitCode}");
            }
        }
    }
}
