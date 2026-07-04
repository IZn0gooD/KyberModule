using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using KyberKeyManagement;

namespace KyberCLI.Commands;

public sealed class KeysCommand : IKyberCommand
{
    private readonly KeysCommandOptions _options;

    private KeysCommand(KeysCommandOptions options)
    {
        _options = options;
    }

    public static KeysCommand Parse(string[] args)
    {
        if (args.Length == 0)
        {
            throw new CommandLineException("Sous-commande requise: list, rotate ou export");
        }

        var options = new KeysCommandOptions
        {
            Action = args[0].ToLowerInvariant()
        };

        if (!options.IsValidAction)
        {
            throw new CommandLineException($"Sous-commande inconnue: {args[0]}");
        }

        for (int i = 1; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--config":
                    options.ConfigPath = RequireArgument(args, ++i, "--config");
                    break;
                case "--name":
                    options.KeyName = RequireArgument(args, ++i, "--name");
                    break;
                case "--passphrase":
                    options.PassphraseOverride = RequireArgument(args, ++i, "--passphrase");
                    break;
                case "--mode":
                    options.ModeOverride = RequireArgument(args, ++i, "--mode");
                    break;
                case "--days":
                    options.ValidityOverride = int.Parse(RequireArgument(args, ++i, "--days"), CultureInfo.InvariantCulture);
                    break;
                case "--output":
                    options.ExportPath = RequireArgument(args, ++i, "--output");
                    break;
                default:
                    throw new CommandLineException($"Option inconnue: {args[i]}");
            }
        }

        return new KeysCommand(options);
    }

    public Task<int> ExecuteAsync()
    {
        var snapshot = ConfigSnapshot.Load(_options.ConfigPath);

        if (!string.IsNullOrWhiteSpace(_options.ModeOverride))
        {
            snapshot.EncryptionMode = _options.ModeOverride!;
        }

        if (!string.IsNullOrWhiteSpace(_options.PassphraseOverride))
        {
            snapshot.Passphrase = _options.PassphraseOverride;
        }

        if (_options.ValidityOverride.HasValue)
        {
            snapshot.RotationDays = _options.ValidityOverride.Value;
        }

        var options = snapshot.ToKeyStorageOptions();
        var logger = new ConsoleKeyLogger();
        var rotation = new KeyRotationService(options, logger);

        switch (_options.Action)
        {
            case "list":
                ExecuteList(rotation, snapshot);
                break;
            case "rotate":
                ExecuteRotate(rotation, snapshot);
                break;
            case "export":
                ExecuteExport(rotation, snapshot);
                break;
        }

        return Task.FromResult(0);
    }

    private void ExecuteList(KeyRotationService rotation, ConfigSnapshot snapshot)
    {
        var keys = rotation.ListKeys();
        if (keys.Count == 0)
        {
            Console.WriteLine("Aucune clé référencée dans metadata.json.");
            return;
        }

        Console.WriteLine($"Clés stockées dans '{snapshot.KeysDirectory}':\n");
        foreach (var key in keys)
        {
            Console.WriteLine($"- {key.Name} v{key.Version} | créé le {key.CreatedAt:u} " +
                              (key.ExpiresAt.HasValue ? $"| expire le {key.ExpiresAt.Value:u}" : "| pas d'expiration"));
            if (!string.IsNullOrWhiteSpace(key.Encryption))
            {
                Console.WriteLine($"  chiffrement: {key.Encryption}");
            }
        }
    }

    private void ExecuteRotate(KeyRotationService rotation, ConfigSnapshot snapshot)
    {
        var validity = snapshot.RotationDays > 0 ? TimeSpan.FromDays(snapshot.RotationDays) : (TimeSpan?)null;
        var material = rotation.RotateKeys(_options.KeyName, validity);
        Console.WriteLine($"Nouvelle version générée pour '{_options.KeyName}'." +
                          (validity.HasValue ? $" Prochaine rotation automatique dans {snapshot.RotationDays} jours." : string.Empty));
        Array.Clear(material.KyberPrivate, 0, material.KyberPrivate.Length);
        Array.Clear(material.DilithiumPrivate, 0, material.DilithiumPrivate.Length);
    }

    private void ExecuteExport(KeyRotationService rotation, ConfigSnapshot snapshot)
    {
        var target = string.IsNullOrWhiteSpace(_options.ExportPath)
            ? Path.Combine(snapshot.ConfigDirectory, "metadata-export.json")
            : Path.GetFullPath(_options.ExportPath!);

        var bytes = rotation.ExportMetadata();
        File.WriteAllBytes(target, bytes);
        Console.WriteLine($"Métadonnées exportées vers {target}.");
    }

    private static string RequireArgument(string[] args, int index, string option)
    {
        if (index >= args.Length)
        {
            throw new CommandLineException($"Argument manquant pour {option}");
        }
        return args[index];
    }

    private sealed class KeysCommandOptions
    {
        public string Action { get; set; } = string.Empty;
        public string ConfigPath { get; set; } = "config/kyberd.conf";
        public string KeyName { get; set; } = "session";
        public string? PassphraseOverride { get; set; }
            = null;
        public string? ModeOverride { get; set; }
            = null;
        public int? ValidityOverride { get; set; }
            = null;
        public string? ExportPath { get; set; }
            = null;

        public bool IsValidAction => Action is "list" or "rotate" or "export";
    }

    private sealed class ConfigSnapshot
    {
        public string ConfigDirectory { get; private set; } = Directory.GetCurrentDirectory();
        public string KeysDirectory { get; private set; } = string.Empty;
        public string EncryptionMode { get; set; } = OperatingSystem.IsWindows() ? "dpapi" : "passphrase";
        public string? Passphrase { get; set; }
            = null;
        public int RotationDays { get; set; }
            = 0;

        public KeyStorageOptions ToKeyStorageOptions()
        {
            return new KeyStorageOptions
            {
                KeysDirectory = KeysDirectory,
                EncryptionMode = string.Equals(EncryptionMode, "dpapi", StringComparison.OrdinalIgnoreCase)
                    ? KeyEncryptionMode.Dpapi
                    : KeyEncryptionMode.Passphrase,
                Passphrase = Passphrase,
                RotationDays = RotationDays
            };
        }

        public static ConfigSnapshot Load(string? path)
        {
            var snapshot = new ConfigSnapshot();

            if (string.IsNullOrWhiteSpace(path))
            {
                path = "config/kyberd.conf";
            }

            if (!Path.IsPathRooted(path))
            {
                path = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), path));
            }

            snapshot.ConfigDirectory = Path.GetDirectoryName(path) ?? Directory.GetCurrentDirectory();

            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (File.Exists(path))
            {
                foreach (var line in File.ReadAllLines(path, Encoding.UTF8))
                {
                    var trimmed = line.Trim();
                    if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith("#"))
                    {
                        continue;
                    }

                    var idx = trimmed.IndexOf('=');
                    if (idx <= 0)
                    {
                        continue;
                    }

                    var key = trimmed[..idx].Trim();
                    var value = trimmed[(idx + 1)..].Trim();
                    var commentIndex = value.IndexOf('#');
                    if (commentIndex >= 0)
                    {
                        value = value[..commentIndex].Trim();
                    }
                    map[key] = value;
                }
            }

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

                return Path.GetFullPath(Path.Combine(snapshot.ConfigDirectory, value));
            }

            string? ReadValue(string key, string? defaultValue = null)
            {
                return map.TryGetValue(key, out var value) ? value : defaultValue;
            }

            snapshot.KeysDirectory = ResolvePath(ReadValue("keys_directory", "config/keys") ?? "config/keys");
            snapshot.EncryptionMode = ReadValue("key_encryption_mode", snapshot.EncryptionMode) ?? snapshot.EncryptionMode;
            snapshot.Passphrase = ReadValue("key_encryption_passphrase", snapshot.Passphrase);
            snapshot.RotationDays = int.TryParse(ReadValue("key_rotation_days"), out var days) && days > 0 ? days : snapshot.RotationDays;

            return snapshot;
        }
    }

    private sealed class ConsoleKeyLogger : IKeyLogger
    {
        public void Info(string source, string message)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"[{source}] {message}");
            Console.ResetColor();
        }

        public void Warning(string source, string message)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"[{source}] {message}");
            Console.ResetColor();
        }

        public void Error(string source, string message, Exception? ex = null)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[{source}] {message}");
            if (ex is not null)
            {
                Console.WriteLine(ex);
            }
            Console.ResetColor();
        }
    }
}
