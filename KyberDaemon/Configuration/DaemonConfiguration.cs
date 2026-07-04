using System;

namespace KyberDaemon.Configuration;

public sealed class DaemonConfiguration
{
    public string ListeningAddress { get; init; } = "0.0.0.0";
    public int ListeningPort { get; init; } = 8443;

    public string AuthConfigPath { get; init; } = "config/auth.conf";
    public string KeysDirectory { get; init; } = "config/keys";

    public TimeSpan SessionTimeout { get; init; } = TimeSpan.FromMinutes(30);
    public int MaxConcurrentConnections { get; init; } = 32;
    public int MaxSessionsPerUser { get; init; } = 4;

    public bool StrictNetworkMode { get; init; }
        = false;

    public string LogDirectory { get; init; } = "logs";
    public string LogFileName { get; init; } = "kyberd.log";
    public int LogMaxFileSizeMb { get; init; } = 10;
    public int LogRetentionCount { get; init; } = 5;

    public string ShellPathWindows { get; init; } = "cmd.exe";
    public string ShellArgsWindows { get; init; } = "/K";
    public string ShellPathLinux { get; init; } = "/bin/bash";
    public string ShellArgsLinux { get; init; } = "-i";

    public string KeyEncryptionMode { get; init; } = OperatingSystem.IsWindows() ? "dpapi" : "passphrase";
    public string? KeyEncryptionPassphrase { get; init; } = string.Empty;
    public int KeyRotationDays { get; init; } = 90;

    public KerberosConfiguration Kerberos { get; init; } = new();
    public OAuthConfiguration OAuth { get; init; } = new();

    public static DaemonConfiguration Default => new();
}

public sealed class KerberosConfiguration
{
    public bool Enabled { get; init; }
    public string KeytabPath { get; init; } = string.Empty;
    public string ServicePrincipal { get; init; } = string.Empty;
    public TimeSpan ReplayCacheDuration { get; init; } = TimeSpan.FromMinutes(5);
}

public sealed class OAuthConfiguration
{
    public bool Enabled { get; init; }
    public string Issuer { get; init; } = string.Empty;
    public string Audience { get; init; } = string.Empty;
    public string JwksPath { get; init; } = string.Empty;
    public string[] RequiredScopes { get; init; } = Array.Empty<string>();
    public TimeSpan CacheDuration { get; init; } = TimeSpan.FromMinutes(10);
}
