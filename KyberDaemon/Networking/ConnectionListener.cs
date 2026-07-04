using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using KyberDaemon.Configuration;
using KyberDaemon.Logging;
using KyberDaemon.Sessions;

namespace KyberDaemon.Networking;

public sealed class ConnectionListener : IDisposable
{
    private readonly DaemonConfiguration _config;
    private readonly DaemonLogger _logger;
    private readonly SessionManager _sessionManager;
    private TcpListener? _listener;

    public ConnectionListener(DaemonConfiguration config, DaemonLogger logger)
    {
        _config = config;
        _logger = logger;
        _sessionManager = new SessionManager(logger, config);
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var listenAddress = _config.StrictNetworkMode ? IPAddress.Loopback : IPAddress.Parse(_config.ListeningAddress);
        if (_config.StrictNetworkMode && !IPAddress.IsLoopback(listenAddress))
        {
            _logger.Warning("Listener", "Mode strict activé : l'adresse d'écoute est forcée sur 127.0.0.1");
            listenAddress = IPAddress.Loopback;
        }

        var listener = new TcpListener(listenAddress, _config.ListeningPort);
        _listener = listener;
        listener.Start();

        _logger.Info("Listener", $"En attente de connexions entrantes sur {listenAddress}:{_config.ListeningPort}...");

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                TcpClient client;
                try
                {
                    client = await listener.AcceptTcpClientAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                catch (ObjectDisposedException) when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                _ = Task.Run(() => HandleClientAsync(client), cancellationToken);
            }
        }
        finally
        {
            listener.Stop();
            _logger.Info("Listener", "Arrêt du listener TCP");
        }
    }

    public Task StopAsync()
    {
        try
        {
            _listener?.Stop();
        }
        catch
        {
            // ignore
        }

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _sessionManager.Dispose();
    }

    private async Task HandleClientAsync(TcpClient client)
    {
        var remote = client.Client.RemoteEndPoint?.ToString() ?? "inconnu";
        _logger.Info("Listener", $"Connexion acceptée depuis {remote}");

        if (_config.StrictNetworkMode && client.Client.RemoteEndPoint is IPEndPoint ipEndPoint && !IPAddress.IsLoopback(ipEndPoint.Address))
        {
            _logger.Warning("Listener", $"Mode strict : connexion refusée ({remote})");
            client.Dispose();
            return;
        }

        try
        {
            await _sessionManager.HandleNewConnectionAsync(client).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.Error("Listener", $"Erreur durant la connexion {remote}", ex);
        }
        finally
        {
            client.Dispose();
            _logger.Info("Listener", $"Connexion fermée {remote}");
        }
    }
}
