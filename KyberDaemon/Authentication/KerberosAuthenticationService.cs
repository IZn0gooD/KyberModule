using System.IO;
using System.Security;
using Kerberos.NET;
using Kerberos.NET.Crypto;
using Kerberos.NET.Entities;
using KyberDaemon.Configuration;
using KyberDaemon.Logging;

namespace KyberDaemon.Authentication;

public sealed class KerberosAuthenticationService
{
    private readonly KerberosValidator _validator;
    private readonly DaemonLogger _logger;

    public KerberosAuthenticationService(KerberosConfiguration config, DaemonLogger logger)
    {
        if (!config.Enabled)
        {
            throw new InvalidOperationException("Kerberos n'est pas activé dans la configuration");
        }

        if (string.IsNullOrWhiteSpace(config.KeytabPath) || !File.Exists(config.KeytabPath))
        {
            throw new FileNotFoundException($"Keytab introuvable: {config.KeytabPath}");
        }

        _logger = logger;
        var keytabBytes = File.ReadAllBytes(config.KeytabPath);
        var keyTable = new KeyTable(keytabBytes);
        _validator = new KerberosValidator(keyTable);
    }

    public async Task<string> ValidateAsync(string tokenBase64)
    {
        if (string.IsNullOrWhiteSpace(tokenBase64))
        {
            throw new SecurityException("Token Kerberos vide");
        }

        try
        {
            var raw = Convert.FromBase64String(tokenBase64);
            var result = await _validator.Validate(raw);
            var principal = result?.Ticket?.CName?.FullyQualifiedName ?? result?.Authenticator?.CName?.FullyQualifiedName;
            if (string.IsNullOrEmpty(principal))
            {
                throw new SecurityException("Ticket Kerberos invalide");
            }

            return principal;
        }
        catch (FormatException ex)
        {
            _logger.Warning("Kerberos", $"Token Kerberos invalide: {ex.Message}");
            throw new SecurityException("Token Kerberos invalide", ex);
        }
        catch (KerberosValidationException ex)
        {
            _logger.Warning("Kerberos", $"Échec de validation Kerberos: {ex.Message}");
            throw new SecurityException("Ticket Kerberos invalide", ex);
        }
    }
}
