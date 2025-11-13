using System;
using System.IO;
using System.Management.Automation;
using System.Text;

namespace KyberModule
{
    /// <summary>
    /// Cmdlet pour exporter uniquement une clé publique Kyber vers un fichier
    /// </summary>
    [Cmdlet(VerbsData.Export, "KyberPublicKey")]
    [OutputType(typeof(string))]
    public class ExportKyberPublicKeyCommand : PSCmdlet
    {
        [Parameter(
            Mandatory = true,
            Position = 0,
            ValueFromPipeline = true,
            ValueFromPipelineByPropertyName = true,
            HelpMessage = "Clé publique à exporter (byte[] ou chaîne hexadécimale)")]
        public object PublicKey { get; set; }

        [Parameter(
            Mandatory = true,
            Position = 1,
            HelpMessage = "Chemin du fichier où sauvegarder la clé publique")]
        public string Path { get; set; }

        [Parameter(
            Mandatory = false,
            HelpMessage = "Format de sortie (Base64, Hex, ou Raw). Par défaut: Base64")]
        [ValidateSet("Base64", "Hex", "Raw")]
        public string Format { get; set; } = "Base64";

        [Parameter(
            Mandatory = false,
            HelpMessage = "Surcharger le fichier s'il existe déjà")]
        public SwitchParameter Force { get; set; }

        protected override void ProcessRecord()
        {
            try
            {
                byte[] publicKeyBytes;

                // Convertir la clé publique
                if (PublicKey is byte[] bytes)
                {
                    publicKeyBytes = bytes;
                }
                else if (PublicKey is string hexString)
                {
                    publicKeyBytes = KyberLibrary.KyberWrapper.HexToBytes(hexString);
                }
                else if (PublicKey is KyberKeyPairResult keyPair)
                {
                    publicKeyBytes = keyPair.PublicKey;
                }
                else
                {
                    throw new ArgumentException("La clé publique doit être un byte[], une chaîne hexadécimale, ou un KyberKeyPairResult");
                }

                string resolvedPath = GetUnresolvedProviderPathFromPSPath(Path);

                // Vérifier si le fichier existe
                if (File.Exists(resolvedPath) && !Force.IsPresent)
                {
                    WriteError(new ErrorRecord(
                        new IOException($"Le fichier '{resolvedPath}' existe déjà. Utilisez -Force pour le surcharger."),
                        "FileExists",
                        ErrorCategory.ResourceExists,
                        resolvedPath));
                    return;
                }

                string content;

                switch (Format.ToUpper())
                {
                    case "BASE64":
                        content = Convert.ToBase64String(publicKeyBytes);
                        break;
                    case "HEX":
                        content = KyberLibrary.KyberWrapper.BytesToHex(publicKeyBytes);
                        break;
                    case "RAW":
                        content = Encoding.UTF8.GetString(publicKeyBytes);
                        break;
                    default:
                        content = Convert.ToBase64String(publicKeyBytes);
                        break;
                }

                // Créer le répertoire si nécessaire
                string directory = System.IO.Path.GetDirectoryName(resolvedPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Écrire le fichier
                if (Format.Equals("RAW", StringComparison.OrdinalIgnoreCase))
                {
                    File.WriteAllBytes(resolvedPath, publicKeyBytes);
                }
                else
                {
                    File.WriteAllText(resolvedPath, content, Encoding.UTF8);
                }

                WriteObject(resolvedPath);
                WriteVerbose($"Clé publique exportée vers: {resolvedPath}");
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(ex, "ExportError", ErrorCategory.WriteError, Path));
            }
        }
    }
}

