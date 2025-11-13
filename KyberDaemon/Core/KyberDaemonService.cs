using KyberDaemon.Configuration;
using KyberDaemon.Logging;
using KyberDaemon.Networking;

namespace KyberDaemon.Core;

public sealed class KyberDaemonService
{
    private readonly DaemonLogger _logger;
    private DaemonConfiguration? _configuration;
    private ConnectionListener? _listener;

    public KyberDaemonService(DaemonLogger logger)
    {
        _logger = logger;
    }

    public async Task RunAsync(string[] args, CancellationToken cancellationToken)
    {
        _configuration = ConfigLoader.Load(args, _logger);
        _logger.Configure(_configuration);
        _listener = new ConnectionListener(_configuration, _logger);

        _logger.Info("KyberDaemon", $"Service démarré sur {_configuration.ListeningAddress}:{_configuration.ListeningPort}");

        try
        {
            await _listener.StartAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.Info("KyberDaemon", "Arrêt demandé");
        }
        finally
        {
            if (_listener is not null)
            {
                await _listener.StopAsync().ConfigureAwait(false);
                _listener.Dispose();
            }
        }
    }
}
