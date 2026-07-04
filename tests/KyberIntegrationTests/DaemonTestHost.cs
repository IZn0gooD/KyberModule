using System.Globalization;
using System.Net;
using System.Net.Sockets;
using KyberDaemon.Core;
using KyberDaemon.Logging;

namespace KyberIntegrationTests;

internal sealed class DaemonTestHost : IAsyncDisposable
{
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _runTask;
    private readonly DaemonLogger _logger = new();

    public string RootDirectory { get; }
    public string ConfigPath { get; }
    public string AuthPath { get; }
    public string KeysDirectory { get; }
    public int Port { get; }

    public string Username => "alice";
    public string Password => "ChangeMe!123";
    public string PasswordRecord => "argon2id$v=19$m=65536,t=3,p=2$pad2ZtQRdzTOr2zecPUMng==$wnzKuHLwtQSOloqyRr5XMN671g6c18w2UbIaPOu9Zkw=";

    private DaemonTestHost()
    {
        RootDirectory = Path.Combine(Path.GetTempPath(), "kyberd-tests", Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture));
        Directory.CreateDirectory(RootDirectory);

        ConfigPath = Path.Combine(RootDirectory, "kyberd.conf");
        AuthPath = Path.Combine(RootDirectory, "auth.conf");
        KeysDirectory = Path.Combine(RootDirectory, "keys");
        Directory.CreateDirectory(KeysDirectory);
        Directory.CreateDirectory(Path.Combine(RootDirectory, "logs"));

        File.WriteAllText(AuthPath, $"{Username}:password:{PasswordRecord}{Environment.NewLine}");

        Port = GetFreeTcpPort();
        File.WriteAllText(ConfigPath, BuildConfiguration());

        _runTask = Task.Run(() =>
        {
            var service = new KyberDaemonService(_logger);
            return service.RunAsync(new[] { "--config", ConfigPath }, _cts.Token);
        });
    }

    public static async Task<DaemonTestHost> StartAsync()
    {
        var host = new DaemonTestHost();
        await host.WaitForPortAsync();
        return host;
    }

    private string BuildConfiguration()
    {
        return $"listen_address = 127.0.0.1{Environment.NewLine}" +
               $"listen_port = {Port}{Environment.NewLine}" +
               "auth_config = auth.conf" + Environment.NewLine +
               "keys_directory = keys" + Environment.NewLine +
               "strict_network_mode = false" + Environment.NewLine +
               "session_timeout_seconds = 900" + Environment.NewLine +
               "max_connections = 16" + Environment.NewLine +
               "max_sessions_per_user = 4" + Environment.NewLine +
               $"log_directory = {Path.Combine(RootDirectory, "logs")}" + Environment.NewLine +
               "log_file_name = kyberd.log" + Environment.NewLine +
               "log_max_size_mb = 5" + Environment.NewLine +
               "log_max_files = 3" + Environment.NewLine +
               "key_encryption_mode = passphrase" + Environment.NewLine +
               "key_encryption_passphrase = IntegrationTestPassphrase!" + Environment.NewLine +
               "key_rotation_days = 0" + Environment.NewLine;
    }

    private async Task WaitForPortAsync()
    {
        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (DateTime.UtcNow < deadline)
        {
            if (_runTask.IsFaulted)
            {
                throw new InvalidOperationException("Le service KyberDaemon s'est arrêté durant le démarrage", _runTask.Exception);
            }

            try
            {
                using var client = new TcpClient();
                var connectTask = client.ConnectAsync(IPAddress.Loopback, Port);
                var completed = await Task.WhenAny(connectTask, Task.Delay(200));
                if (completed == connectTask && client.Connected)
                {
                    return;
                }
            }
            catch
            {
                // ignore tentative échouée
            }

            await Task.Delay(200);
        }

        throw new TimeoutException($"Le service KyberDaemon n'a pas démarré sur le port {Port}.");
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            _cts.Cancel();
            var delayTask = Task.Delay(2000);
            await Task.WhenAny(_runTask, delayTask);
        }
        finally
        {
            _cts.Dispose();
            TryDeleteDirectory(RootDirectory);
        }
    }

    private static int GetFreeTcpPort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
        catch
        {
            // ignore cleanup errors
        }
    }
}
