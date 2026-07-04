using System;
using System.Threading.Tasks;

namespace KyberIntegrationTests;

public class EndToEndTests : IAsyncLifetime
{
    private DaemonTestHost? _host;

    public async Task InitializeAsync()
    {
        _host = await DaemonTestHost.StartAsync();
    }

    public async Task DisposeAsync()
    {
        if (_host is not null)
        {
            await _host.DisposeAsync();
        }
    }

    [Fact]
    public async Task Cli_command_returns_expected_output()
    {
        Assert.NotNull(_host);
        var result = await CliRunner.RunCommandAsync(_host!, "echo IntegrationOK");

        Assert.True(result.ExitCode == 0, $"Exit {result.ExitCode}\nSTDOUT:{result.StandardOutput}\nSTDERR:{result.StandardError}");
        Assert.Contains("IntegrationOK", result.StandardOutput, StringComparison.OrdinalIgnoreCase);
        Assert.True(string.IsNullOrWhiteSpace(result.StandardError));
    }

    [Fact]
    public async Task Daemon_survives_random_handshake_fuzz()
    {
        Assert.NotNull(_host);

        await NetworkFuzzer.SendRandomHandshakeAsync(_host!.Port);
        await Task.Delay(200);

        var result = await CliRunner.RunCommandAsync(_host, "echo AfterFuzz");

        Assert.True(result.ExitCode == 0, $"Exit {result.ExitCode}\nSTDOUT:{result.StandardOutput}\nSTDERR:{result.StandardError}");
        Assert.Contains("AfterFuzz", result.StandardOutput, StringComparison.OrdinalIgnoreCase);
    }
}
