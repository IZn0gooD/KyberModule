using System;
using System.IO;
using System.Management.Automation;
using System.Text;
using System.Globalization;

namespace KyberModule
{
    /// <summary>
    /// Cmdlet pour exporter une paire de clés Kyber vers un fichier
    /// </summary>
    [Cmdlet(VerbsData.Export, "KyberKeyPair")]
    [OutputType(typeof(string))]
    public class ExportKyberKeyPairCommand : PSCmdlet
    {
        [Parameter(
            Mandatory = true,
            Position = 0,
            ValueFromPipeline = true,
            HelpMessage = "Objet KyberKeyPairResult contenant les clés à exporter")]
        public KyberKeyPairResult KeyPair { get; set; }

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

                if (Format.Equals("Text", StringComparison.OrdinalIgnoreCase))
                {
                    // Format texte simple (évite les vulnérabilités de System.Text.Json)
                    // Format: Key=Value par ligne
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine("# Kyber Key Pair Export");
                    sb.AppendLine("# Format: Simple Key-Value");
                    sb.AppendLine("# Version: 1.0");
                    sb.AppendLine($"Version=1.0");
                    sb.AppendLine($"ParameterSet={KeyPair.ParameterSet}");
                    sb.AppendLine($"PublicKey={Convert.ToBase64String(KeyPair.PublicKey)}");
                    sb.AppendLine($"PrivateKey={Convert.ToBase64String(KeyPair.PrivateKey)}");
                    sb.AppendLine($"PublicKeyHex={KeyPair.PublicKeyHex}");
                    sb.AppendLine($"PrivateKeyHex={KeyPair.PrivateKeyHex}");
                    sb.AppendLine($"GeneratedDate={DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)}");
                    content = sb.ToString();
                }
                else
                {
                    // Format Base64 simple (clé publique + clé privée séparées)
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine("-----BEGIN KYBER PUBLIC KEY-----");
                    sb.AppendLine(Convert.ToBase64String(KeyPair.PublicKey));
                    sb.AppendLine("-----END KYBER PUBLIC KEY-----");
                    sb.AppendLine();
                    sb.AppendLine("-----BEGIN KYBER PRIVATE KEY-----");
                    sb.AppendLine(Convert.ToBase64String(KeyPair.PrivateKey));
                    sb.AppendLine("-----END KYBER PRIVATE KEY-----");
                    sb.AppendLine();
                    sb.AppendLine($"# ParameterSet: {KeyPair.ParameterSet}");
                    sb.AppendLine($"# Generated: {DateTime.UtcNow:O}");

                    content = sb.ToString();
                }

                // Créer le répertoire si nécessaire
                string directory = System.IO.Path.GetDirectoryName(resolvedPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Écrire le fichier
                File.WriteAllText(resolvedPath, content, Encoding.UTF8);

                WriteObject(resolvedPath);
                WriteVerbose($"Paire de clés exportée vers: {resolvedPath}");
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(ex, "ExportError", ErrorCategory.WriteError, Path));
            }
        }
    }
}

