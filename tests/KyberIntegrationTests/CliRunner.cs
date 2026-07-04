using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.IO;
using KyberCLI.Commands;
using KyberCLI.Protocol;

namespace KyberIntegrationTests;

internal static class CliRunner
{
    public static async Task<CliResult> RunCommandAsync(DaemonTestHost host, string command, bool captureOutput = true)
    {
        var options = new ConnectOptions
        {
            Host = "127.0.0.1",
            Port = host.Port,
            Username = host.Username,
            PasswordPlain = host.Password,
            PasswordRecord = host.PasswordRecord,
            Command = command
        };

        if (!captureOutput)
        {
            var client = new KyberDaemonClient(options);
            var exitCode = await client.RunAsync();
            return new CliResult(exitCode, string.Empty, string.Empty);
        }

        var stdout = new StringWriter();
        var stderr = new StringWriter();
        var originalOut = Console.Out;
        var originalErr = Console.Error;

        try
        {
            Console.SetOut(stdout);
            Console.SetError(stderr);
            var client = new KyberDaemonClient(options);
            var exitCode = await client.RunAsync();
            return new CliResult(exitCode, stdout.ToString(), stderr.ToString());
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalErr);
        }
    }

    private static string Quote(string value)
        => value.Contains(' ') ? $"\"{value}\"" : value;

    private static string QuoteIfNeeded(string value)
        => value.Contains(' ') && !value.StartsWith('"') ? $"\"{value}\"" : value;
}

internal readonly record struct CliResult(int ExitCode, string StandardOutput, string StandardError);
