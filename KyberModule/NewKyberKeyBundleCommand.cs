using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Runtime.InteropServices;
using KyberLibrary;

namespace KyberModule;

#nullable enable

/// <summary>
/// Cmdlet permettant de générer automatiquement un bundle de clés Kyber/Dilithium
/// pour un client, un serveur, ou les deux. Les clés sont persistées en Base64.
/// </summary>
[Cmdlet(VerbsCommon.New, "KyberKeyBundle", SupportsShouldProcess = true)]
[OutputType(typeof(KyberKeyBundleResult))]
public sealed class NewKyberKeyBundleCommand : PSCmdlet
{
    private readonly List<string> _createdFiles = new();
    private string? _clientPublicKeyBase64;
    private string? _authEntry;

    [Parameter(Mandatory = false)]
    [ValidateSet("Client", "Server", "Both")]
    public string Target { get; set; } = "Client";

    [Parameter(Mandatory = false)]
    public string? ClientDestination { get; set; }

    [Parameter(Mandatory = false)]
    [ValidateSet("Dilithium2", "Dilithium3", "Dilithium5")]
    public string DilithiumParameterSet { get; set; } = "Dilithium3";

    [Parameter(Mandatory = false)]
    [ValidateSet("Kyber512", "Kyber768", "Kyber1024")]
    public string KyberParameterSet { get; set; } = "Kyber768";

    [Parameter(Mandatory = false)]
    public string Username { get; set; } = "alice";

    [Parameter(Mandatory = false)]
    public string? ServerConfigPath { get; set; }

    [Parameter(Mandatory = false)]
    public string? ServerAuthPath { get; set; }

    [Parameter(Mandatory = false)]
    public SwitchParameter IncludeKyber { get; set; }

    [Parameter(Mandatory = false)]
    public SwitchParameter Force { get; set; }

    [Parameter(Mandatory = false)]
    public SwitchParameter SkipAuthUpdate { get; set; }

    [Parameter(Mandatory = false)]
    public string AuthMethod { get; set; } = "key";

    [Parameter(Mandatory = false)]
    public string? KyberCliPath { get; set; }

    [Parameter(Mandatory = false)]
    public string KyberKeyName { get; set; } = "session";

    [Parameter(Mandatory = false)]
    public SwitchParameter SkipKyberRotation { get; set; }

    protected override void BeginProcessing()
    {
        if (string.IsNullOrWhiteSpace(ClientDestination))
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (string.IsNullOrWhiteSpace(home))
            {
                home = Directory.GetCurrentDirectory();
            }
            ClientDestination = Path.Combine(home, "KyberClient", "keys");
        }

        if (string.IsNullOrWhiteSpace(ServerConfigPath))
        {
            ServerConfigPath = GetDefaultServerConfigPath();
        }

        if (string.IsNullOrWhiteSpace(ServerAuthPath))
        {
            ServerAuthPath = GetDefaultServerAuthPath();
        }
    }

    protected override void ProcessRecord()
    {
        var dilithiumParam = ParseDilithiumParameter(DilithiumParameterSet);
        var kyberParam = ParseKyberParameter(KyberParameterSet);

        var handleClient = Target.Equals("Client", StringComparison.OrdinalIgnoreCase) ||
                           Target.Equals("Both", StringComparison.OrdinalIgnoreCase);
        var handleServer = Target.Equals("Server", StringComparison.OrdinalIgnoreCase) ||
                           Target.Equals("Both", StringComparison.OrdinalIgnoreCase);

        if (handleClient)
        {
            HandleClient(dilithiumParam, kyberParam);
        }

        bool authUpdated = false;
        string? rotationOutput = null;

        if (handleServer)
        {
            HandleServer(dilithiumParam, ref authUpdated, ref rotationOutput);
        }

        var result = new KyberKeyBundleResult
        {
            Target = Target,
            Username = Username,
            CreatedFiles = _createdFiles.ToArray(),
            ClientPublicKeyBase64 = _clientPublicKeyBase64,
            AuthEntry = authUpdated ? _authEntry : null,
            AuthEntryUpdated = authUpdated,
            KyberRotationOutput = rotationOutput
        };

        WriteObject(result);
    }

    private void HandleClient(DilithiumWrapper.DilithiumParameterSet dilithiumParam,
                              KyberWrapper.KyberParameterSet kyberParam)
    {
        if (string.IsNullOrWhiteSpace(ClientDestination))
        {
            WriteWarning("Destination client non définie. Génération ignorée.");
            return;
        }

        var safeUserFragment = SanitizeFileNameFragment(Username);
        Directory.CreateDirectory(ClientDestination);

        var dilithium = new DilithiumWrapper(dilithiumParam);
        var (publicKey, privateKey) = dilithium.GenerateKeyPair();
        _clientPublicKeyBase64 = Convert.ToBase64String(publicKey);

        var clientPublicPath = Path.Combine(ClientDestination, $"{safeUserFragment}.pub");
        var clientPrivatePath = Path.Combine(ClientDestination, $"{safeUserFragment}.prv");

        if (ShouldProcess(clientPublicPath, "Écrire la clé publique cliente (Dilithium)"))
        {
            WriteBase64File(clientPublicPath, publicKey);
        }

        if (ShouldProcess(clientPrivatePath, "Écrire la clé privée cliente (Dilithium)"))
        {
            WriteBase64File(clientPrivatePath, privateKey);
        }

        if (IncludeKyber)
        {
            var kyber = new KyberWrapper(kyberParam);
            var (kyberPub, kyberPrv) = kyber.GenerateKeyPair();

            var kyberPubPath = Path.Combine(ClientDestination, $"{safeUserFragment}_kyber.pub");
            var kyberPrvPath = Path.Combine(ClientDestination, $"{safeUserFragment}_kyber.prv");

            if (ShouldProcess(kyberPubPath, "Écrire la clé publique cliente (Kyber)"))
            {
                WriteBase64File(kyberPubPath, kyberPub);
            }

            if (ShouldProcess(kyberPrvPath, "Écrire la clé privée cliente (Kyber)"))
            {
                WriteBase64File(kyberPrvPath, kyberPrv);
            }
        }
    }

    private void HandleServer(DilithiumWrapper.DilithiumParameterSet dilithiumParam,
                              ref bool authUpdated,
                              ref string? rotationOutput)
    {
        if (string.IsNullOrWhiteSpace(ServerConfigPath))
        {
            WriteWarning("Chemin de configuration serveur inconnu. Génération des clés serveur ignorée.");
            return;
        }

        var configDirectory = Path.GetDirectoryName(ServerConfigPath);
        if (string.IsNullOrWhiteSpace(configDirectory))
        {
            configDirectory = Directory.GetCurrentDirectory();
        }

        var dilithiumDirectory = Path.Combine(configDirectory, "keys", "dilithium");
        Directory.CreateDirectory(dilithiumDirectory);

        var dilithium = new DilithiumWrapper(dilithiumParam);
        var (publicKey, privateKey) = dilithium.GenerateKeyPair();

        var serverPublicPath = Path.Combine(dilithiumDirectory, "server.pub");
        var serverPrivatePath = Path.Combine(dilithiumDirectory, "server.prv");

        if (ShouldProcess(serverPublicPath, "Écrire la clé publique serveur (Dilithium)"))
        {
            WriteBase64File(serverPublicPath, publicKey);
        }

        if (ShouldProcess(serverPrivatePath, "Écrire la clé privée serveur (Dilithium)"))
        {
            WriteBase64File(serverPrivatePath, privateKey);
        }

        if (!SkipAuthUpdate && !string.IsNullOrEmpty(ServerAuthPath) && !string.IsNullOrEmpty(_clientPublicKeyBase64))
        {
            authUpdated = UpdateAuthFile(ServerAuthPath!, Username, AuthMethod, _clientPublicKeyBase64!);
        }
        else if (!SkipAuthUpdate && string.IsNullOrEmpty(_clientPublicKeyBase64))
        {
            WriteVerbose("Aucune clé cliente générée dans ce bundle, auth.conf n'a pas été modifié.");
        }

        if (!SkipKyberRotation)
        {
            var resolvedCliPath = ResolveKyberCliPath();
            if (!string.IsNullOrWhiteSpace(resolvedCliPath))
            {
                rotationOutput = RotateKyberKeys(resolvedCliPath!, ServerConfigPath!, KyberKeyName);
            }
            else
            {
                WriteVerbose("KyberCLI introuvable – rotation des clés Kyber non exécutée.");
            }
        }
    }

    private void WriteBase64File(string path, byte[] data)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        if (!Force && File.Exists(path))
        {
            throw new IOException($"Le fichier {path} existe déjà. Utilisez -Force pour l'écraser.");
        }

        File.WriteAllText(path, Convert.ToBase64String(data));
        _createdFiles.Add(path);
        WriteVerbose($"Fichier écrit : {path}");
    }

    private bool UpdateAuthFile(string path, string username, string method, string data)
    {
        var entry = $"{username}:{method}:{data}";
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        if (!File.Exists(path))
        {
            File.WriteAllText(path, entry + Environment.NewLine);
            _authEntry = entry;
            WriteVerbose($"Fichier auth créé et entrée ajoutée pour {username}.");
            return true;
        }

        var lines = new List<string>(File.ReadAllLines(path));
        var updated = false;
        for (var i = 0; i < lines.Count; i++)
        {
            var trimmed = lines[i].Trim();
            if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#", StringComparison.Ordinal))
            {
                continue;
            }

            var separatorIndex = trimmed.IndexOf(':');
            if (separatorIndex <= 0)
            {
                continue;
            }

            var existingUser = trimmed.Substring(0, separatorIndex);
            if (existingUser.Equals(username, StringComparison.OrdinalIgnoreCase))
            {
                lines[i] = entry;
                updated = true;
                break;
            }
        }

        if (!updated)
        {
            lines.Add(entry);
        }

        File.WriteAllLines(path, lines);
        _authEntry = entry;
        WriteVerbose(updated
            ? $"Entrée auth mise à jour pour {username}."
            : $"Entrée auth ajoutée pour {username}.");
        return true;
    }

    private string? ResolveKyberCliPath()
    {
        if (!string.IsNullOrWhiteSpace(KyberCliPath))
        {
            if (File.Exists(KyberCliPath))
            {
                return KyberCliPath;
            }

            WriteWarning($"KyberCLI introuvable à l'emplacement '{KyberCliPath}'.");
            return null;
        }

        if (SkipKyberRotation)
        {
            return null;
        }

        var baseDirectory = AppContext.BaseDirectory;
        var candidates = new List<string>();

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            candidates.Add(Path.Combine(baseDirectory, "KyberCLI.exe"));
            candidates.Add(Path.Combine(baseDirectory, "publish", "win-x64", "KyberCLI.exe"));
            var parent = Path.GetDirectoryName(baseDirectory);
            if (!string.IsNullOrEmpty(parent))
            {
                candidates.Add(Path.Combine(parent, "KyberCLI", "publish", "win-x64", "KyberCLI.exe"));
            }
            candidates.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "KyberModule", "KyberCLI", "publish", "win-x64", "KyberCLI.exe"));
        }
        else
        {
            candidates.Add(Path.Combine(baseDirectory, "KyberCLI"));
            candidates.Add(Path.Combine(baseDirectory, "publish", "linux-x64", "KyberCLI"));
            candidates.Add("/opt/kybercli/KyberCLI");
        }

        return candidates.FirstOrDefault(candidate => !string.IsNullOrWhiteSpace(candidate) && File.Exists(candidate));
    }

    private string? RotateKyberKeys(string cliPath, string configPath, string keyName)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = cliPath,
                Arguments = $"keys rotate --config \"{configPath}\" --name {keyName}",
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process == null)
            {
                WriteWarning("Impossible de lancer KyberCLI pour la rotation des clés.");
                return null;
            }

            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                WriteWarning($"Rotation Kyber échouée (code {process.ExitCode}). {error}");
            }
            else
            {
                WriteVerbose("Rotation des clés Kyber effectuée via KyberCLI.");
            }

            return string.IsNullOrWhiteSpace(error) ? output : output + Environment.NewLine + error;
        }
        catch (Exception ex)
        {
            WriteWarning($"Échec de la rotation des clés Kyber : {ex.Message}");
            return null;
        }
    }

    private static DilithiumWrapper.DilithiumParameterSet ParseDilithiumParameter(string parameter)
    {
        return parameter switch
        {
            "Dilithium2" => DilithiumWrapper.DilithiumParameterSet.Dilithium2,
            "Dilithium3" => DilithiumWrapper.DilithiumParameterSet.Dilithium3,
            "Dilithium5" => DilithiumWrapper.DilithiumParameterSet.Dilithium5,
            _ => DilithiumWrapper.DilithiumParameterSet.Dilithium3
        };
    }

    private static KyberWrapper.KyberParameterSet ParseKyberParameter(string parameter)
    {
        return parameter switch
        {
            "Kyber512" => KyberWrapper.KyberParameterSet.Kyber512,
            "Kyber768" => KyberWrapper.KyberParameterSet.Kyber768,
            "Kyber1024" => KyberWrapper.KyberParameterSet.Kyber1024,
            _ => KyberWrapper.KyberParameterSet.Kyber768
        };
    }

    private static string GetDefaultServerConfigPath()
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? @"C:\ProgramData\KyberDaemon\kyberd.conf"
            : "/etc/kyberd/kyberd.conf";
    }

    private static string GetDefaultServerAuthPath()
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? @"C:\ProgramData\KyberDaemon\auth.conf"
            : "/etc/kyberd/auth.conf";
    }

    private static string SanitizeFileNameFragment(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return new string(value.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray());
    }
}

/// <summary>
/// Résultat retourné par New-KyberKeyBundle
/// </summary>
public sealed class KyberKeyBundleResult
{
    public string Target { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string[] CreatedFiles { get; set; } = Array.Empty<string>();
    public string? ClientPublicKeyBase64 { get; set; }
    public string? AuthEntry { get; set; }
    public bool AuthEntryUpdated { get; set; }
    public string? KyberRotationOutput { get; set; }
}
