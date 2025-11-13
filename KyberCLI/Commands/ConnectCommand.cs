using System;
using System.IO;
using System.Text;
using KyberCLI.Protocol;

namespace KyberCLI.Commands;

public sealed class ConnectCommand : IKyberCommand
{
    private ConnectCommand(ConnectOptions options)
    {
        Options = options;
    }

    public ConnectOptions Options { get; }

    public static ConnectCommand Parse(string[] args)
    {
        var options = new ConnectOptions();
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--host":
                    options.Host = RequireArgument(args, ++i, "--host");
                    break;
                case "--port":
                    options.Port = int.Parse(RequireArgument(args, ++i, "--port"));
                    break;
                case "--user":
                    options.Username = RequireArgument(args, ++i, "--user");
                    break;
                case "--password":
                {
                    var value = RequireArgument(args, ++i, "--password");
                    if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase) || IsHex(value))
                    {
                        options.PasswordHex = value;
                    }
                    else
                    {
                        options.PasswordPlain = value;
                    }
                    break;
                }
                case "--password-hex":
                    options.PasswordHex = RequireArgument(args, ++i, "--password-hex");
                    break;
                case "--password-record":
                    options.PasswordRecord = RequireArgument(args, ++i, "--password-record");
                    break;
                case "--password-file":
                {
                    var path = RequireArgument(args, ++i, "--password-file");
                    options.PasswordPlain = File.ReadAllText(path).Trim();
                    break;
                }
                case "--password-record-file":
                {
                    var path = RequireArgument(args, ++i, "--password-record-file");
                    options.PasswordRecord = File.ReadAllText(path).Trim();
                    break;
                }
                case "--kerberos-token":
                    options.KerberosToken = RequireArgument(args, ++i, "--kerberos-token");
                    break;
                case "--kerberos-token-file":
                {
                    var path = RequireArgument(args, ++i, "--kerberos-token-file");
                    options.KerberosToken = File.ReadAllText(path).Trim();
                    break;
                }
                case "--oauth-token":
                    options.OAuthToken = RequireArgument(args, ++i, "--oauth-token");
                    break;
                case "--oauth-token-file":
                {
                    var path = RequireArgument(args, ++i, "--oauth-token-file");
                    options.OAuthToken = File.ReadAllText(path).Trim();
                    break;
                }
                case "--key":
                    options.KeyPath = RequireArgument(args, ++i, "--key");
                    break;
                case "--command":
                    options.Command = RequireArgument(args, ++i, "--command");
                    break;
                case "--tls":
                    options.UseTls = true;
                    break;
                case "--tls-server-name":
                    options.UseTls = true;
                    options.TlsServerName = RequireArgument(args, ++i, "--tls-server-name");
                    break;
                case "--tls-skip-verify":
                    options.UseTls = true;
                    options.TlsAllowInsecure = true;
                    break;
                case "--tls-cert-fingerprint":
                    options.UseTls = true;
                    options.TlsCertFingerprint = NormalizeFingerprint(RequireArgument(args, ++i, "--tls-cert-fingerprint"));
                    break;
                case "--trace-level":
                {
                    var value = RequireArgument(args, ++i, "--trace-level").ToLowerInvariant();
                    options.DiagnosticsLevel = value switch
                    {
                        "none" => DiagnosticsLevel.None,
                        "handshake" => DiagnosticsLevel.Handshake,
                        "auth" or "authentication" => DiagnosticsLevel.Authentication,
                        "full" or "verbose" => DiagnosticsLevel.Full,
                        _ => throw new CommandLineException("--trace-level doit être l'un de: none, handshake, auth, full")
                    };
                    break;
                }
                default:
                    throw new CommandLineException($"Option inconnue: {args[i]}");
            }
        }

        if (string.IsNullOrWhiteSpace(options.Host) || string.IsNullOrWhiteSpace(options.Username))
        {
            throw new CommandLineException("--host et --user sont obligatoires");
        }

        var methodsSelected = CountSelectedMethods(options);
        if (methodsSelected == 0)
        {
            throw new CommandLineException("Une méthode d'authentification est requise (--password/--kerberos-token/--oauth-token ou --key)");
        }

        if (methodsSelected > 1)
        {
            throw new CommandLineException("Choisir une seule méthode d'authentification à la fois");
        }

        if (!string.IsNullOrEmpty(options.PasswordPlain) && string.IsNullOrEmpty(options.PasswordRecord))
        {
            throw new CommandLineException("Un enregistrement Argon2 (--password-record) est requis pour utiliser --password");
        }

        if (!options.UseTls && (options.TlsAllowInsecure || !string.IsNullOrEmpty(options.TlsCertFingerprint) || !string.IsNullOrEmpty(options.TlsServerName)))
        {
            throw new CommandLineException("Utiliser --tls lorsque des options TLS sont spécifiées");
        }

        if (options.UseTls && string.IsNullOrWhiteSpace(options.TlsServerName))
        {
            options.TlsServerName = options.Host;
        }

        return new ConnectCommand(options);
    }

    public async Task<int> ExecuteAsync()
    {
        var client = new KyberDaemonClient(Options);
        return await client.RunAsync();
    }

    private static int CountSelectedMethods(ConnectOptions options)
    {
        var count = 0;
        if (!string.IsNullOrEmpty(options.PasswordHex) || !string.IsNullOrEmpty(options.PasswordPlain)) count++;
        if (!string.IsNullOrEmpty(options.KerberosToken)) count++;
        if (!string.IsNullOrEmpty(options.OAuthToken)) count++;
        if (!string.IsNullOrEmpty(options.KeyPath)) count++;
        return count;
    }

    private static string RequireArgument(string[] args, int index, string option)
    {
        if (index >= args.Length)
        {
            throw new CommandLineException($"Argument manquant pour {option}");
        }
        return args[index];
    }

    private static bool IsHex(string value)
    {
        foreach (var c in value)
        {
            if (!Uri.IsHexDigit(c))
            {
                return false;
            }
        }
        return value.Length > 0;
    }

    private static string NormalizeFingerprint(string fingerprint)
    {
        if (string.IsNullOrWhiteSpace(fingerprint))
        {
            throw new CommandLineException("Empreinte TLS vide");
        }

        var span = fingerprint.AsSpan().Trim();
        if (span.StartsWith("sha256:", StringComparison.OrdinalIgnoreCase))
        {
            span = span[7..];
        }

        var builder = new StringBuilder(span.Length);
        foreach (var ch in span)
        {
            if (ch == ':' || ch == '-' || char.IsWhiteSpace(ch))
            {
                continue;
            }

            if (!Uri.IsHexDigit(ch))
            {
                throw new CommandLineException("Empreinte TLS invalide (attendu SHA-256 en hexadécimal)");
            }

            builder.Append(char.ToUpperInvariant(ch));
        }

        if (builder.Length != 64)
        {
            throw new CommandLineException("Empreinte TLS invalide (longueur attendue: 64 caractères hexadécimaux)");
        }

        return builder.ToString();
    }
}

public sealed class ConnectOptions
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 8443;
    public string Username { get; set; } = string.Empty;
    public string? PasswordHex { get; set; }
        = null;
    public string? PasswordPlain { get; set; }
        = null;
    public string? PasswordRecord { get; set; }
        = null;
    public string? KerberosToken { get; set; }
        = null;
    public string? OAuthToken { get; set; }
        = null;
    public string? KeyPath { get; set; }
        = null;
    public string? Command { get; set; }
        = null;
    public bool UseTls { get; set; }
        = false;
    public string? TlsServerName { get; set; }
        = null;
    public bool TlsAllowInsecure { get; set; }
        = false;
    public string? TlsCertFingerprint { get; set; }
        = null;
    public bool TlsSkipVerify { get; set; } = false;
    public DiagnosticsLevel DiagnosticsLevel { get; set; } = DiagnosticsLevel.None;
}

public enum DiagnosticsLevel
{
    None = 0,
    Handshake = 1,
    Authentication = 2,
    Full = 3
}
