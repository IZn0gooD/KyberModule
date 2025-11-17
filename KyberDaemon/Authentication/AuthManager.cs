using System.Security.Cryptography;
using System.Security;
using System.Text;
using KyberDaemon.Configuration;
using KyberDaemon.Logging;
using KyberShared.Protocol;
using KyberShared.Security;
using KyberLibrary;
using KyberLibrary.DomainAdapters;
using KyberDomain.Cryptography;

namespace KyberDaemon.Authentication;

public sealed class AuthManager : IDisposable
{
    private readonly DaemonConfiguration _config;
    private readonly DaemonLogger _logger;
    private readonly Dictionary<string, AuthEntry> _entries = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _supportedMethods = new(StringComparer.OrdinalIgnoreCase);
    private readonly KerberosAuthenticationService? _kerberosService;
    private readonly OAuthTokenValidator? _oauthValidator;

    public AuthManager(DaemonConfiguration config, DaemonLogger logger)
    {
        _config = config;
        _logger = logger;

        if (config.Kerberos.Enabled)
        {
            try
            {
                _kerberosService = new KerberosAuthenticationService(config.Kerberos, logger);
            }
            catch (Exception ex)
            {
                _logger.Warning("AuthManager", $"Kerberos désactivé : {ex.Message}");
            }
        }

        if (config.OAuth.Enabled)
        {
            try
            {
                _oauthValidator = new OAuthTokenValidator(config.OAuth);
            }
            catch (Exception ex)
            {
                _logger.Warning("AuthManager", $"OAuth désactivé : {ex.Message}");
            }
        }

        LoadConfiguration();
    }

    public IReadOnlyCollection<string> SupportedMethods => _supportedMethods;

    public async Task<AuthResult> AuthenticateAsync(AuthMessages.AuthResponsePayload response, byte[] challenge)
    {
        if (!_entries.TryGetValue(response.Username, out var entry))
        {
            _logger.Warning("AuthManager", $"Utilisateur inconnu: {response.Username}");
            return AuthResult.Fail("Utilisateur inconnu");
        }

        if (!string.Equals(entry.Method, response.Method, StringComparison.OrdinalIgnoreCase))
        {
            _logger.Warning("AuthManager", $"Méthode d'authentification invalide pour {response.Username}");
            return AuthResult.Fail("Méthode d'authentification invalide");
        }

        switch (entry)
        {
            case PasswordAuthEntry passwordEntry:
                return await AuthenticatePasswordAsync(passwordEntry, response, challenge);
            case KeyAuthEntry keyEntry:
                return await AuthenticateKeyAsync(keyEntry, response, challenge);
            case KerberosAuthEntry kerberosEntry:
                return await AuthenticateKerberosAsync(kerberosEntry, response);
            case OAuthAuthEntry oauthEntry:
                return AuthenticateOAuth(oauthEntry, response);
            default:
                _logger.Warning("AuthManager", $"Type d'entrée inconnu pour {entry.Username}");
                return AuthResult.Fail("Méthode non supportée");
        }
    }

    private Task<AuthResult> AuthenticatePasswordAsync(PasswordAuthEntry entry, AuthMessages.AuthResponsePayload response, byte[] challenge)
    {
        if (response.Data.Length != 32)
        {
            return Task.FromResult(AuthResult.Fail("Réponse d'authentification invalide"));
        }

        if (entry.Secret.Length != 32)
        {
            _logger.Error("AuthManager", $"Secret stocké invalide pour {entry.Username} (taille {entry.Secret.Length})");
            return Task.FromResult(AuthResult.Fail("Configuration invalide"));
        }

        var buffer = new byte[entry.Secret.Length + challenge.Length];
        Buffer.BlockCopy(entry.Secret, 0, buffer, 0, entry.Secret.Length);
        Buffer.BlockCopy(challenge, 0, buffer, entry.Secret.Length, challenge.Length);
        var expected = SHA3Wrapper.ComputeSHA3_256(buffer);

        if (!CryptographicOperations.FixedTimeEquals(expected, response.Data))
        {
            _logger.Warning("AuthManager", $"Échec d'authentification (mot de passe) pour {entry.Username}");
            return Task.FromResult(AuthResult.Fail("Mot de passe invalide"));
        }

        var metadata = entry.ArgonRecord is not null
            ? $" (argon2:{entry.ArgonRecord.Iterations}x m={entry.ArgonRecord.MemorySize})"
            : string.Empty;
        _logger.Info("AuthManager", $"Authentification réussie pour {entry.Username} (mot de passe){metadata}");
        return Task.FromResult(AuthResult.CreateSuccess(entry.Username));
    }

    private Task<AuthResult> AuthenticateKeyAsync(KeyAuthEntry entry, AuthMessages.AuthResponsePayload response, byte[] challenge)
    {
        try
        {
            var signatureService = new DilithiumSignatureService();
            var isValid = signatureService.Verify(challenge, response.Data, entry.PublicKey, SignatureStrength.Dilithium3);

            if (!isValid)
            {
                _logger.Warning("AuthManager", $"Signature invalide pour {entry.Username}");
                return Task.FromResult(AuthResult.Fail("Signature invalide"));
            }

            _logger.Info("AuthManager", $"Authentification réussie pour {entry.Username} (clé publique)");
            return Task.FromResult(AuthResult.CreateSuccess(entry.Username));
        }
        catch (Exception ex)
        {
            _logger.Error("AuthManager", "Erreur lors de la vérification de signature", ex);
            return Task.FromResult(AuthResult.Fail("Erreur interne"));
        }
    }

    private async Task<AuthResult> AuthenticateKerberosAsync(KerberosAuthEntry entry, AuthMessages.AuthResponsePayload response)
    {
        if (_kerberosService is null)
        {
            return AuthResult.Fail("Kerberos non configuré");
        }

        var token = Encoding.UTF8.GetString(response.Data);
        var principal = await _kerberosService.ValidateAsync(token);

        if (!string.Equals(principal, entry.Principal, StringComparison.OrdinalIgnoreCase))
        {
            _logger.Warning("AuthManager", $"Kerberos principal inattendu: {principal} (attendu {entry.Principal})");
            return AuthResult.Fail("Identité Kerberos invalide");
        }

        _logger.Info("AuthManager", $"Authentification réussie pour {entry.Username} (Kerberos)");
        return AuthResult.CreateSuccess(entry.Username);
    }

    private AuthResult AuthenticateOAuth(OAuthAuthEntry entry, AuthMessages.AuthResponsePayload response)
    {
        if (_oauthValidator is null)
        {
            return AuthResult.Fail("OAuth non configuré");
        }

        var token = Encoding.UTF8.GetString(response.Data);
        var principal = _oauthValidator.Validate(token);

        if (entry.ExpectedSubject != "*")
        {
            var subject = principal.FindFirst("sub")?.Value ?? principal.Identity?.Name;
            if (!string.Equals(subject, entry.ExpectedSubject, StringComparison.OrdinalIgnoreCase))
            {
                _logger.Warning("AuthManager", $"Sujet OAuth inattendu: {subject} (attendu {entry.ExpectedSubject})");
                return AuthResult.Fail("Identité OAuth invalide");
            }
        }

        _logger.Info("AuthManager", $"Authentification réussie pour {entry.Username} (OAuth)");
        return AuthResult.CreateSuccess(entry.Username);
    }

    private void LoadConfiguration()
    {
        var path = _config.AuthConfigPath;
        if (!File.Exists(path))
        {
            _logger.Warning("AuthManager", $"Fichier d'authentification introuvable: {path}");
            return;
        }

        foreach (var line in File.ReadAllLines(path))
        {
            var trimmed = line.Trim();
            if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith("#"))
            {
                continue;
            }

            var parts = trimmed.Split(':');
            if (parts.Length < 3)
            {
                _logger.Warning("AuthManager", $"Entrée invalide dans auth.conf: {line}");
                continue;
            }

            var username = parts[0].Trim();
            var method = parts[1].Trim().ToLowerInvariant();
            var data = parts[2].Trim();

            try
            {
                AuthEntry? entry = method switch
                {
                    "password" => CreatePasswordEntry(username, method, data),
                    "key" => new KeyAuthEntry(username, method, Convert.FromBase64String(data)),
                    "kerberos" => CreateKerberosEntry(username, method, data),
                    "oauth" => CreateOAuthEntry(username, method, data),
                    _ => throw new InvalidOperationException($"Méthode inconnue: {method}")
                };

                if (entry is not null)
                {
                    _entries[username] = entry;
                    _supportedMethods.Add(method);
                }
            }
            catch (Exception ex)
            {
                _logger.Warning("AuthManager", $"Impossible de charger l'entrée '{line}': {ex.Message}");
            }
        }
    }

    private PasswordAuthEntry CreatePasswordEntry(string username, string method, string data)
    {
        if (Argon2HashRecord.TryParse(data, out var record))
        {
            if (record!.Hash.Length != 32)
            {
                throw new InvalidOperationException("La sortie Argon2 doit faire 32 octets (SHA3_256)");
            }

            return new PasswordAuthEntry(username, method, record.Hash, record);
        }

        var secret = ParseHex(data);
        if (secret.Length != 32)
        {
            throw new InvalidOperationException("Le secret hexadécimal doit faire 32 octets");
        }

        return new PasswordAuthEntry(username, method, secret, null);
    }

    private KerberosAuthEntry? CreateKerberosEntry(string username, string method, string data)
    {
        if (_kerberosService is null)
        {
            _logger.Warning("AuthManager", "Entrée Kerberos ignorée: service non configuré");
            return null;
        }

        if (string.IsNullOrWhiteSpace(data))
        {
            throw new InvalidOperationException("Principal Kerberos manquant");
        }

        return new KerberosAuthEntry(username, method, data);
    }

    private OAuthAuthEntry? CreateOAuthEntry(string username, string method, string data)
    {
        if (_oauthValidator is null)
        {
            _logger.Warning("AuthManager", "Entrée OAuth ignorée: service non configuré");
            return null;
        }

        var subject = string.IsNullOrWhiteSpace(data) ? "*" : data;
        return new OAuthAuthEntry(username, method, subject);
    }

    private static byte[] ParseHex(string hex)
    {
        if (hex.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            hex = hex.Substring(2);
        }

        if (hex.Length % 2 != 0)
        {
            throw new FormatException("Longueur hex invalide");
        }

        var bytes = new byte[hex.Length / 2];
        for (int i = 0; i < bytes.Length; i++)
        {
            bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
        }
        return bytes;
    }

    public void Dispose()
    {
    }

    private abstract record AuthEntry(string Username, string Method);

    private sealed record PasswordAuthEntry(string Username, string Method, byte[] Secret, Argon2HashRecord? ArgonRecord) : AuthEntry(Username, Method);

    private sealed record KeyAuthEntry(string Username, string Method, byte[] PublicKey) : AuthEntry(Username, Method);

    private sealed record KerberosAuthEntry(string Username, string Method, string Principal) : AuthEntry(Username, Method);

    private sealed record OAuthAuthEntry(string Username, string Method, string ExpectedSubject) : AuthEntry(Username, Method);
}

public sealed record AuthResult(bool IsSuccess, string? Username, string? Reason)
{
    public static AuthResult Fail(string reason) => new(false, null, reason);
    public static AuthResult CreateSuccess(string username) => new(true, username, null);
}
