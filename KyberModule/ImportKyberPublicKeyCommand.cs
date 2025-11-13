using System;
using System.IO;
using System.Management.Automation;
using System.Text;
using System.Text.RegularExpressions;
using KyberLibrary;

namespace KyberModule
{
    /// <summary>
    /// Cmdlet pour importer une clé publique Kyber depuis un fichier
    /// </summary>
    [Cmdlet(VerbsData.Import, "KyberPublicKey")]
    [OutputType(typeof(byte[]))]
    public class ImportKyberPublicKeyCommand : PSCmdlet
    {
        [Parameter(
            Mandatory = true,
            Position = 0,
            HelpMessage = "Chemin du fichier contenant la clé publique")]
        [ValidateNotNullOrEmpty]
        public string Path { get; set; }

        [Parameter(
            Mandatory = false,
            HelpMessage = "Format du fichier (Base64, Hex, ou Auto). Par défaut: Auto")]
        [ValidateSet("Base64", "Hex", "Auto")]
        public string Format { get; set; } = "Auto";

        protected override void ProcessRecord()
        {
            try
            {
                string resolvedPath = GetUnresolvedProviderPathFromPSPath(Path);

                if (!File.Exists(resolvedPath))
                {
                    WriteError(new ErrorRecord(
                        new FileNotFoundException($"Le fichier '{resolvedPath}' est introuvable."),
                        "FileNotFound",
                        ErrorCategory.ObjectNotFound,
                        resolvedPath));
                    return;
                }

                byte[] publicKey;

                if (Format.Equals("Auto", StringComparison.OrdinalIgnoreCase))
                {
                    // Détecter automatiquement le format
                    string content = File.ReadAllText(resolvedPath, Encoding.UTF8);
                    
                    if (content.IndexOf("-----BEGIN KYBER PUBLIC KEY-----", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        // Format PEM
                        publicKey = ImportFromPem(content);
                    }
                    else if (content.Length > 0 && IsBase64(content.Trim()))
                    {
                        // Format Base64 simple
                        publicKey = Convert.FromBase64String(content.Trim());
                    }
                    else if (IsHex(content.Trim()))
                    {
                        // Format Hexadécimal
                        publicKey = KyberWrapper.HexToBytes(content.Trim());
                    }
                    else
                    {
                        // Essayer de lire comme binaire
                        publicKey = File.ReadAllBytes(resolvedPath);
                    }
                }
                else if (Format.Equals("Base64", StringComparison.OrdinalIgnoreCase))
                {
                    string content = File.ReadAllText(resolvedPath, Encoding.UTF8);
                    publicKey = Convert.FromBase64String(content.Trim());
                }
                else if (Format.Equals("Hex", StringComparison.OrdinalIgnoreCase))
                {
                    string content = File.ReadAllText(resolvedPath, Encoding.UTF8);
                    publicKey = KyberWrapper.HexToBytes(content.Trim());
                }
                else
                {
                    publicKey = File.ReadAllBytes(resolvedPath);
                }

                WriteObject(publicKey);
                WriteVerbose($"Clé publique importée depuis: {resolvedPath}");
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(ex, "ImportError", ErrorCategory.ReadError, Path));
            }
        }

        private byte[] ImportFromPem(string content)
        {
            var match = Regex.Match(content,
                @"-----BEGIN KYBER PUBLIC KEY-----\s*(.+?)\s*-----END KYBER PUBLIC KEY-----",
                RegexOptions.Singleline | RegexOptions.IgnoreCase);

            if (!match.Success)
            {
                throw new FormatException("Format PEM invalide: marqueurs BEGIN/END introuvables");
            }

            string base64Data = match.Groups[1].Value.Replace("\r", "").Replace("\n", "").Replace(" ", "");
            return Convert.FromBase64String(base64Data);
        }

        private bool IsBase64(string str)
        {
            try
            {
                Convert.FromBase64String(str);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private bool IsHex(string str)
        {
            if (string.IsNullOrWhiteSpace(str) || str.Length % 2 != 0)
                return false;

            foreach (char c in str)
            {
                if (!((c >= '0' && c <= '9') || (c >= 'A' && c <= 'F') || (c >= 'a' && c <= 'f')))
                    return false;
            }
            return true;
        }
    }
}

