using System;
using System.IO;
using System.Management.Automation;
using System.Text;

namespace KyberModule
{
    /// <summary>
    /// Cmdlet pour exporter une paire de clés Ed25519 vers un fichier
    /// </summary>
    [Cmdlet(VerbsData.Export, "Ed25519KeyPair")]
    [OutputType(typeof(string))]
    public class ExportEd25519KeyPairCommand : PSCmdlet
    {
        [Parameter(
            Mandatory = true,
            Position = 0,
            ValueFromPipeline = true,
            HelpMessage = "Objet Ed25519KeyPairResult contenant les clés à exporter")]
        public Ed25519KeyPairResult KeyPair { get; set; }

        [Parameter(
            Mandatory = true,
            Position = 1,
            HelpMessage = "Chemin du fichier où sauvegarder la paire de clés")]
        public string Path { get; set; }

        [Parameter(
            Mandatory = false,
            HelpMessage = "Format de sortie (Text ou Base64). Par défaut: Text")]
        [ValidateSet("Text", "Base64")]
        public string Format { get; set; } = "Text";

        [Parameter(
            Mandatory = false,
            HelpMessage = "Surcharger le fichier s'il existe déjà")]
        public SwitchParameter Force { get; set; }

        protected override void ProcessRecord()
        {
            try
            {
                string resolvedPath = GetUnresolvedProviderPathFromPSPath(Path);

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

                if (Format.Equals("Text", StringComparison.OrdinalIgnoreCase))
                {
                    // Format texte simple (Key=Value)
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine("# Ed25519 Key Pair Export");
                    sb.AppendLine("# Format: Simple Key-Value");
                    sb.AppendLine("# Version: 1.0");
                    sb.AppendLine("Version=1.0");
                    sb.AppendLine($"PublicKey={Convert.ToBase64String(KeyPair.PublicKey)}");
                    sb.AppendLine($"PrivateKey={Convert.ToBase64String(KeyPair.PrivateKey)}");
                    sb.AppendLine($"PublicKeyHex={KeyPair.PublicKeyHex}");
                    sb.AppendLine($"PrivateKeyHex={KeyPair.PrivateKeyHex}");
                    content = sb.ToString();
                }
                else
                {
                    // Format Base64/PEM
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine("-----BEGIN ED25519 PUBLIC KEY-----");
                    sb.AppendLine(Convert.ToBase64String(KeyPair.PublicKey));
                    sb.AppendLine("-----END ED25519 PUBLIC KEY-----");
                    sb.AppendLine();
                    sb.AppendLine("-----BEGIN ED25519 PRIVATE KEY-----");
                    sb.AppendLine(Convert.ToBase64String(KeyPair.PrivateKey));
                    sb.AppendLine("-----END ED25519 PRIVATE KEY-----");
                    content = sb.ToString();
                }

                // Créer le répertoire si nécessaire
                string directory = System.IO.Path.GetDirectoryName(resolvedPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(resolvedPath, content, Encoding.UTF8);

                WriteObject(resolvedPath);
                WriteVerbose($"Paire de clés Ed25519 exportée vers: {resolvedPath}");
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(ex, "ExportError", ErrorCategory.WriteError, Path));
            }
        }
    }
}

