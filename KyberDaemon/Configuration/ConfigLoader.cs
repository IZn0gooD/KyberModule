using KyberDaemon.Logging;
using System.Linq;

namespace KyberDaemon.Configuration;

public static class ConfigLoader
{
    private const string DefaultConfigPath = "config/kyberd.conf";

    public static DaemonConfiguration Load(string[] args, DaemonLogger logger)
    {
        var baseDirectory = AppContext.BaseDirectory;
        var path = ResolveConfigPath(args);
        if (!Path.IsPathRooted(path))
        {
            path = Path.GetFullPath(Path.Combine(baseDirectory, path));
        }
        logger.Info("ConfigLoader", $"Chargement de la configuration: {path}");

        if (!File.Exists(path))
        {
            logger.Warning("ConfigLoader", "Fichier de configuration introuvable, utilisation des valeurs par défaut");
            return CreateDefaultConfiguration(baseDirectory);
        }

        var keyValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in File.ReadAllLines(path))
        {
            var trimmed = line.Trim();
            if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith("#"))
            {
                continue;
            }

            var separatorIndex = trimmed.IndexOf('=');
            if (separatorIndex <= 0)
            {
                continue;
            }

            var key = trimmed[..separatorIndex].Trim();
            var value = trimmed[(separatorIndex + 1)..].Trim();
            keyValues[key] = value;
        }

        var configDirectory = Path.GetDirectoryName(Path.GetFullPath(path)) ?? Directory.GetCurrentDirectory();

        string ResolvePath(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return value;
            }

            if (Path.IsPathRooted(value))
            {
                return value;
            }

            return Path.GetFullPath(Path.Combine(configDirectory, value));
        }

        var logDirectory = ResolvePath(keyValues.GetValueOrDefault("log_directory", "logs"));
        var logFileName = keyValues.GetValueOrDefault("log_file_name", "kyberd.log");
        if (string.IsNullOrWhiteSpace(logFileName))
        {
            logFileName = "kyberd.log";
        }
        else
        {
            logFileName = Path.GetFileName(logFileName);
        }

        int EnsurePositive(string key, int value, int fallback)
        {
            if (value <= 0)
            {
                logger.Warning("ConfigLoader", $"Valeur non positive pour '{key}', utilisation de {fallback}");
                return fallback;
            }

            return value;
        }

        var logMaxSize = EnsurePositive("log_max_size_mb", ParseInt(keyValues, "log_max_size_mb", 10, logger), 10);
        var logRetention = EnsurePositive("log_max_files", ParseInt(keyValues, "log_max_files", 5, logger), 5);
        var maxConnections = EnsurePositive("max_connections", ParseInt(keyValues, "max_connections", 32, logger), 32);
        var perUserLimit = EnsurePositive("max_sessions_per_user", ParseInt(keyValues, "max_sessions_per_user", 4, logger), 4);
        var keyRotationDays = Math.Max(0, ParseInt(keyValues, "key_rotation_days", 90, logger));
        var keyEncryptionMode = keyValues.GetValueOrDefault("key_encryption_mode", OperatingSystem.IsWindows() ? "dpapi" : "passphrase");
        var keyEncryptionPassphrase = keyValues.GetValueOrDefault("key_encryption_passphrase", string.Empty);
        var strictNetwork = ParseBool(keyValues, "strict_network_mode", false);

        var kerberosConfig = BuildKerberosConfig(keyValues, ResolvePath, logger);
        var oauthConfig = BuildOAuthConfig(keyValues, ResolvePath, logger);

        return new DaemonConfiguration
        {
            ListeningAddress = keyValues.GetValueOrDefault("listen_address", "0.0.0.0"),
            ListeningPort = ParseInt(keyValues, "listen_port", 8443, logger),
            AuthConfigPath = ResolvePath(keyValues.GetValueOrDefault("auth_config", "config/auth.conf")),
            KeysDirectory = ResolvePath(keyValues.GetValueOrDefault("keys_directory", "config/keys")),
            SessionTimeout = TimeSpan.FromSeconds(ParseInt(keyValues, "session_timeout_seconds", (int)TimeSpan.FromMinutes(30).TotalSeconds, logger)),
            MaxConcurrentConnections = maxConnections,
            MaxSessionsPerUser = perUserLimit,
            StrictNetworkMode = strictNetwork,
            LogDirectory = logDirectory,
            LogFileName = logFileName,
            LogMaxFileSizeMb = logMaxSize,
            LogRetentionCount = logRetention,
            ShellPathWindows = keyValues.GetValueOrDefault("shell_path_windows", "cmd.exe"),
            ShellArgsWindows = keyValues.GetValueOrDefault("shell_args_windows", "/K"),
            ShellPathLinux = keyValues.GetValueOrDefault("shell_path_linux", "/bin/bash"),
            ShellArgsLinux = keyValues.GetValueOrDefault("shell_args_linux", "-i"),
            KeyEncryptionMode = keyEncryptionMode,
            KeyEncryptionPassphrase = keyEncryptionPassphrase,
            KeyRotationDays = keyRotationDays,
            Kerberos = kerberosConfig,
            OAuth = oauthConfig
        };
    }

    private static KerberosConfiguration BuildKerberosConfig(
        IReadOnlyDictionary<string, string> keyValues,
        Func<string, string> resolvePath,
        DaemonLogger logger)
    {
        var enabled = ParseBool(keyValues, "kerberos_enabled", false);
        if (!enabled)
        {
            return new KerberosConfiguration();
        }

        var keytab = keyValues.GetValueOrDefault("kerberos_keytab", string.Empty);
        var principal = keyValues.GetValueOrDefault("kerberos_service_principal", string.Empty);
        if (string.IsNullOrWhiteSpace(keytab) || string.IsNullOrWhiteSpace(principal))
        {
            logger.Warning("ConfigLoader", "Kerberos activé mais keytab ou service_principal non défini");
            return new KerberosConfiguration();
        }

        var replaySeconds = ParseInt(keyValues, "kerberos_replay_seconds", (int)TimeSpan.FromMinutes(5).TotalSeconds, logger);

        return new KerberosConfiguration
        {
            Enabled = true,
            KeytabPath = resolvePath(keytab),
            ServicePrincipal = principal,
            ReplayCacheDuration = TimeSpan.FromSeconds(Math.Max(30, replaySeconds))
        };
    }

    private static OAuthConfiguration BuildOAuthConfig(
        IReadOnlyDictionary<string, string> keyValues,
        Func<string, string> resolvePath,
        DaemonLogger logger)
    {
        var enabled = ParseBool(keyValues, "oauth_enabled", false);
        if (!enabled)
        {
            return new OAuthConfiguration();
        }

        var issuer = keyValues.GetValueOrDefault("oauth_issuer", string.Empty);
        var audience = keyValues.GetValueOrDefault("oauth_audience", string.Empty);
        var jwks = keyValues.GetValueOrDefault("oauth_jwks", string.Empty);

        if (string.IsNullOrWhiteSpace(issuer) || string.IsNullOrWhiteSpace(audience) || string.IsNullOrWhiteSpace(jwks))
        {
            logger.Warning("ConfigLoader", "OAuth activé mais issuer/audience/jwks non définis");
            return new OAuthConfiguration();
        }

        var scopesRaw = keyValues.GetValueOrDefault("oauth_required_scopes", string.Empty);
        var scopes = scopesRaw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var cacheSeconds = ParseInt(keyValues, "oauth_cache_seconds", (int)TimeSpan.FromMinutes(10).TotalSeconds, logger);

        return new OAuthConfiguration
        {
            Enabled = true,
            Issuer = issuer,
            Audience = audience,
            JwksPath = resolvePath(jwks),
            RequiredScopes = scopes,
            CacheDuration = TimeSpan.FromSeconds(Math.Max(60, cacheSeconds))
        };
    }

    private static DaemonConfiguration CreateDefaultConfiguration(string baseDirectory)
    {
        string Combine(params string[] parts) => Path.GetFullPath(Path.Combine(new[] { baseDirectory }.Concat(parts).ToArray()));

        return new DaemonConfiguration
        {
            AuthConfigPath = Combine("config", "auth.conf"),
            KeysDirectory = Combine("config", "keys"),
            LogDirectory = Combine("logs")
        };
    }

    private static string ResolveConfigPath(IEnumerable<string> args)
    {
        const string flag = "--config";
        var array = args.ToArray();
        for (var i = 0; i < array.Length; i++)
        {
            if (string.Equals(array[i], flag, StringComparison.OrdinalIgnoreCase) && i + 1 < array.Length)
            {
                return array[i + 1];
            }
        }

        return DefaultConfigPath;
    }

    private static int ParseInt(IReadOnlyDictionary<string, string> keyValues, string key, int defaultValue, DaemonLogger logger)
    {
        if (keyValues.TryGetValue(key, out var value) && int.TryParse(value, out var parsed))
        {
            return parsed;
        }

        if (keyValues.ContainsKey(key))
        {
            logger.Warning("ConfigLoader", $"Valeur invalide pour '{key}', utilisation de {defaultValue}");
        }
        return defaultValue;
    }

    private static bool ParseBool(IReadOnlyDictionary<string, string> keyValues, string key, bool defaultValue)
    {
        if (keyValues.TryGetValue(key, out var value) && bool.TryParse(value, out var parsed))
        {
            return parsed;
        }

        return defaultValue;
    }
}
