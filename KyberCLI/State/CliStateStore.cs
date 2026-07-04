using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace KyberCLI.State;

internal sealed class CliStateStore
{
    private readonly string _baseDirectory;
    private readonly string _historyPath;
    private readonly string _aliasPath;

    public CliStateStore()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        _baseDirectory = Path.Combine(appData, "KyberCLI");
        Directory.CreateDirectory(_baseDirectory);
        _historyPath = Path.Combine(_baseDirectory, "history.log");
        _aliasPath = Path.Combine(_baseDirectory, "aliases.conf");
    }

    public void LoadHistory()
    {
        if (!File.Exists(_historyPath))
        {
            return;
        }

        try
        {
            foreach (var line in File.ReadLines(_historyPath))
            {
                if (!string.IsNullOrWhiteSpace(line))
                {
                    global::System.ReadLine.AddHistory(line);
                }
            }
        }
        catch
        {
            // ignore history loading errors
        }
    }

    public void AppendHistory(string command)
    {
        try
        {
            File.AppendAllText(_historyPath, command + Environment.NewLine, Encoding.UTF8);
        }
        catch
        {
            // best effort
        }
    }

    public Dictionary<string, string> LoadAliases()
    {
        var aliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (!File.Exists(_aliasPath))
        {
            return aliases;
        }

        try
        {
            foreach (var line in File.ReadLines(_aliasPath))
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

                var name = trimmed[..idx].Trim();
                var value = trimmed[(idx + 1)..].Trim();
                if (!string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(value))
                {
                    aliases[name] = value;
                }
            }
        }
        catch
        {
            // ignore alias loading errors
        }

        return aliases;
    }

    public void SaveAliases(IReadOnlyDictionary<string, string> aliases)
    {
        try
        {
            using var writer = new StreamWriter(_aliasPath, append: false, Encoding.UTF8);
            foreach (var pair in aliases.OrderBy(p => p.Key, StringComparer.OrdinalIgnoreCase))
            {
                writer.Write(pair.Key);
                writer.Write('=');
                writer.WriteLine(pair.Value);
            }
        }
        catch
        {
            // ignore alias saving errors
        }
    }
}
