using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using KyberDaemon.Logging;

namespace KyberDaemon.Core;

public sealed class KyberHostedService : BackgroundService
{
    private readonly KyberDaemonService _daemonService;
    private readonly DaemonHostArguments _arguments;
    private readonly DaemonLogger _logger;

    public KyberHostedService(KyberDaemonService daemonService, DaemonHostArguments arguments, DaemonLogger logger)
    {
        _daemonService = daemonService;
        _arguments = arguments;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await _daemonService.RunAsync(_arguments.Args, stoppingToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.Info("KyberHostedService", "Arrêt demandé par le système");
        }
        catch (Exception ex)
        {
            _logger.Fatal("KyberHostedService", "Erreur critique", ex);
            throw;
        }
    }
}
