using System;
using System.Collections.Generic;
using System.Linq;

namespace KyberCLI.Protocol;

internal sealed class InteractiveCommandAssistant
{
    private static readonly string[] BuiltInDirectives = new[]
    {
        ":help",
        ":examples",
        ":run",
        ":history",
        ":clear",
        ":validate",
        ":alias",
        ":unalias",
        ":aliases"
    };

    private static readonly ExampleCommand[] CommonExamples = new[]
    {
        new ExampleCommand("sys:hostname", "Nom de la machine", "hostname"),
        new ExampleCommand("ps:top", "Processus les plus consommateurs", "Get-Process | Sort-Object CPU -Descending | Select-Object -First 5 Name,CPU,Id"),
        new ExampleCommand("fs:list", "Lister les 10 premiers fichiers du dossier courant", "Get-ChildItem -Path . | Select-Object -First 10 Mode,Name,Length"),
        new ExampleCommand("diag:env", "Variables d'environnement principales", "Get-ChildItem Env: | Select-Object -First 10 Name,Value"),
        new ExampleCommand("diag:time", "Heure locale et UTC", "[PSCustomObject]@{ Local = (Get-Date); UTC = (Get-Date).ToUniversalTime() }")
    };

    private static readonly ExampleCommand[] WindowsExamples = new[]
    {
        new ExampleCommand("net:ip", "Adresse IP et passerelle", "Get-NetIPAddress | Where-Object { $_.AddressFamily -eq 'IPv4' } | Select-Object InterfaceAlias,IPAddress"),
        new ExampleCommand("svc:failed", "Services arrêtés", "Get-Service | Where-Object { $_.Status -eq 'Stopped' } | Select-Object -First 10 DisplayName,Status"),
        new ExampleCommand("logs:tail", "Dernières lignes du journal KyberDaemon", "Get-Content -Path $env:PROGRAMDATA\\KyberDaemon\\logs\\kyberd.log -Tail 20"),
        new ExampleCommand("sys:uptime", "Durée depuis le dernier redémarrage", "(Get-CimInstance Win32_OperatingSystem).LastBootUpTime")
    };

    private static readonly ExampleCommand[] UnixExamples = new[]
    {
        new ExampleCommand("net:ip", "Adresse IP et interface", "ip -brief address"),
        new ExampleCommand("svc:failed", "Unités systemd en échec", "systemctl --failed"),
        new ExampleCommand("logs:tail", "Derniers événements KyberDaemon", "journalctl -u kyberd.service -n 20"),
        new ExampleCommand("sys:uptime", "Durée depuis le dernier démarrage", "uptime -p")
    };

    private readonly HashSet<string> _remoteCommands = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, ExampleCommand> _examples = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _aliases = new(StringComparer.OrdinalIgnoreCase);

    public InteractiveCommandAssistant()
    {
        foreach (var example in CommonExamples)
        {
            _examples[example.Alias] = example;
        }

        var platformExamples = OperatingSystem.IsWindows() ? WindowsExamples : UnixExamples;
        foreach (var example in platformExamples)
        {
            _examples[example.Alias] = example;
        }
    }

    public void UpdateRemoteCommands(IEnumerable<string> commands)
    {
        _remoteCommands.Clear();
        foreach (var command in commands)
        {
            if (!string.IsNullOrWhiteSpace(command))
            {
                _remoteCommands.Add(command.Trim());
            }
        }
    }

    public void LoadAliases(IDictionary<string, string> aliases)
    {
        _aliases.Clear();
        foreach (var pair in aliases)
        {
            if (!string.IsNullOrWhiteSpace(pair.Key) && !string.IsNullOrWhiteSpace(pair.Value))
            {
                _aliases[pair.Key.Trim()] = pair.Value.Trim();
            }
        }
    }

    public bool SetAlias(string name, string value)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        _aliases[name.Trim()] = value.Trim();
        return true;
    }

    public bool RemoveAlias(string name)
    {
        return _aliases.Remove(name);
    }

    public IReadOnlyDictionary<string, string> GetAliases() => _aliases;

    public bool TryExpandAlias(string command, out string expanded, out string aliasName)
    {
        aliasName = string.Empty;
        expanded = command;
        if (string.IsNullOrWhiteSpace(command))
        {
            return false;
        }

        var firstToken = ExtractFirstToken(command);
        if (string.IsNullOrEmpty(firstToken))
        {
            return false;
        }

        if (_aliases.TryGetValue(firstToken, out var baseCommand))
        {
            var remainder = command.Length > firstToken.Length
                ? command[firstToken.Length..].TrimStart()
                : string.Empty;
            expanded = string.IsNullOrEmpty(remainder) ? baseCommand : $"{baseCommand} {remainder}";
            aliasName = firstToken;
            return true;
        }

        return false;
    }

    public IReadOnlyList<string> GetCompletionCandidates()
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "exit"
        };

        foreach (var directive in BuiltInDirectives)
        {
            set.Add(directive);
        }

        foreach (var alias in _aliases.Keys)
        {
            set.Add(alias);
        }

        foreach (var aliasCommand in _aliases.Values)
        {
            set.Add(aliasCommand);
        }

        foreach (var exampleAlias in _examples.Keys)
        {
            set.Add(exampleAlias);
        }

        foreach (var example in _examples.Values)
        {
            if (!string.IsNullOrWhiteSpace(example.Command))
            {
                set.Add(example.Command);
            }
        }

        foreach (var command in _remoteCommands)
        {
            set.Add(command);
        }

        return set.OrderBy(s => s, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    public CommandValidationResult Validate(string? command)
    {
        if (string.IsNullOrWhiteSpace(command))
        {
            return CommandValidationResult.Invalid("Commande vide");
        }

        var trimmed = command.Trim();
        if (trimmed.StartsWith(":", StringComparison.Ordinal))
        {
            if (IsDirective(trimmed, out _))
            {
                return CommandValidationResult.Valid();
            }

            return CommandValidationResult.Warning($"Directive inconnue '{trimmed}'. Tapez :help pour la liste.");
        }

        if (string.Equals(trimmed, "exit", StringComparison.OrdinalIgnoreCase))
        {
            return CommandValidationResult.Valid();
        }

        var firstToken = ExtractFirstToken(trimmed);
        if (_aliases.ContainsKey(firstToken))
        {
            return CommandValidationResult.Valid();
        }

        if (_remoteCommands.Contains(firstToken))
        {
            return CommandValidationResult.Valid();
        }

        if (_examples.ContainsKey(firstToken))
        {
            return CommandValidationResult.Valid();
        }

        return CommandValidationResult.Warning($"Commande inconnue côté serveur: '{firstToken}'.");
    }

    public bool IsDirective(string command, out CommandDirective directive)
    {
        directive = default;
        if (string.IsNullOrWhiteSpace(command) || !command.StartsWith(":", StringComparison.Ordinal))
        {
            return false;
        }

        var tokens = command.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var keyword = tokens[0].ToLowerInvariant();
        var argument = tokens.Length > 1 ? tokens[1] : string.Empty;

        switch (keyword)
        {
            case ":help":
                directive = CommandDirective.Help();
                return true;
            case ":examples":
                directive = CommandDirective.Examples();
                return true;
            case ":run":
                directive = CommandDirective.Run(argument);
                return true;
            case ":history":
                directive = CommandDirective.History();
                return true;
            case ":clear":
                directive = CommandDirective.Clear();
                return true;
            case ":validate":
                directive = CommandDirective.Validate(argument);
                return true;
            case ":aliases":
                directive = CommandDirective.ListAliases();
                return true;
            case ":alias":
            {
                if (string.IsNullOrWhiteSpace(argument))
                {
                    directive = CommandDirective.ListAliases();
                    return true;
                }

                var aliasParts = argument.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (aliasParts.Length < 2)
                {
                    directive = CommandDirective.Invalid();
                    return true;
                }

                directive = CommandDirective.SetAlias(aliasParts[0], aliasParts[1]);
                return true;
            }
            case ":unalias":
            {
                if (string.IsNullOrWhiteSpace(argument))
                {
                    directive = CommandDirective.Invalid();
                    return true;
                }

                directive = CommandDirective.RemoveAlias(argument);
                return true;
            }
            default:
                return false;
        }
    }

    public IEnumerable<ExampleCommand> GetExamples() => _examples.Values.OrderBy(e => e.Alias, StringComparer.OrdinalIgnoreCase);

    public bool TryGetExample(string alias, out ExampleCommand example) => _examples.TryGetValue(alias, out example);

    public IEnumerable<KeyValuePair<string, string>> EnumerateAliases() => _aliases.OrderBy(a => a.Key, StringComparer.OrdinalIgnoreCase);

    private static string ExtractFirstToken(string value)
    {
        var separators = new[] { ' ', '\t', '\r', '\n' };
        var index = value.IndexOfAny(separators);
        return index >= 0 ? value[..index] : value;
    }
}

internal readonly record struct ExampleCommand(string Alias, string Description, string Command);

internal readonly record struct CommandValidationResult(bool IsValid, bool IsWarning, string? Message)
{
    public static CommandValidationResult Valid() => new(true, false, null);
    public static CommandValidationResult Warning(string message) => new(true, true, message);
    public static CommandValidationResult Invalid(string message) => new(false, false, message);
}

internal readonly record struct CommandDirective(CommandDirectiveType Type, string? Argument, string? Value)
{
    public static CommandDirective Help() => new(CommandDirectiveType.Help, null, null);
    public static CommandDirective Examples() => new(CommandDirectiveType.Examples, null, null);
    public static CommandDirective Run(string alias) => new(CommandDirectiveType.RunExample, alias, null);
    public static CommandDirective History() => new(CommandDirectiveType.History, null, null);
    public static CommandDirective Clear() => new(CommandDirectiveType.ClearScreen, null, null);
    public static CommandDirective Validate(string command) => new(CommandDirectiveType.Validate, command, null);
    public static CommandDirective ListAliases() => new(CommandDirectiveType.ListAliases, null, null);
    public static CommandDirective SetAlias(string name, string value) => new(CommandDirectiveType.SetAlias, name, value);
    public static CommandDirective RemoveAlias(string name) => new(CommandDirectiveType.RemoveAlias, name, null);
    public static CommandDirective Invalid() => new(CommandDirectiveType.Invalid, null, null);
}

internal enum CommandDirectiveType
{
    Help,
    Examples,
    RunExample,
    History,
    ClearScreen,
    Validate,
    ListAliases,
    SetAlias,
    RemoveAlias,
    Invalid
}
