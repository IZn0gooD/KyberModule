using System;
using KyberDaemon.Logging;
using KyberKeyManagement;

namespace KyberDaemon.Security;

internal sealed class KeyManagementLoggerAdapter : IKeyLogger
{
    private readonly DaemonLogger _logger;

    public KeyManagementLoggerAdapter(DaemonLogger logger)
    {
        _logger = logger;
    }

    public void Info(string source, string message) => _logger.Info(source, message);

    public void Warning(string source, string message) => _logger.Warning(source, message);

    public void Error(string source, string message, Exception? ex = null) => _logger.Error(source, message, ex);
}
