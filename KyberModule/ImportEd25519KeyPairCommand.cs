using System;
using System.IO;
using System.Management.Automation;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using KyberLibrary;

namespace KyberModule
{
    /// <summary>
    /// Cmdlet pour importer une paire de clés Ed25519 depuis un fichier
    /// </summary>
    [Cmdlet(VerbsData.Import, "Ed25519KeyPair")]
    [OutputType(typeof(Ed25519KeyPairResult))]
    public class ImportEd25519KeyPairCommand : PSCmdlet
    {
        [Parameter(
            Mandatory = true,
            Position = 0,
            HelpMessage = "Chemin du fichier contenant la paire de clés")]
        [ValidateNotNullOrEmpty]
        public string Path { get; set; }

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

                string content = File.ReadAllText(resolvedPath, Encoding.UTF8);

                Ed25519KeyPairResult keyPair;

                // Détecter le format
                if (content.IndexOf("-----BEGIN ED25519", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    // Format Base64/PEM
                    keyPair = ImportFromBase64(content);
                }
                else if (content.IndexOf("Version=", StringComparison.OrdinalIgnoreCase) >= 0 || 
                         content.IndexOf("PublicKey=", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    // Format texte simple (Key=Value)
                    keyPair = ImportFromTextFormat(content);
                }
                else
                {
                    WriteError(new ErrorRecord(
                        new FormatException("Format de fichier non reconnu. Formats supportés: Text (Key=Value), Base64/PEM"),
                        "InvalidFormat",
                        ErrorCategory.InvalidData,
                        resolvedPath));
                    return;
                }

                WriteObject(keyPair);
                WriteVerbose($"Paire de clés Ed25519 importée depuis: {resolvedPath}");
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(ex, "ImportError", ErrorCategory.ReadError, Path));
            }
        }

        private Ed25519KeyPairResult ImportFromTextFormat(string textContent)
        {
            Dictionary<string, string> values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            
            using (StringReader reader = new StringReader(textContent))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    line = line.Trim();
                    if (string.IsNullOrEmpty(line) || line.StartsWith("#"))
                        continue;
                    
                    int equalsIndex = line.IndexOf('=');
                    if (equalsIndex > 0 && equalsIndex < line.Length - 1)
                    {
                        string key = line.Substring(0, equalsIndex).Trim();
                        string value = line.Substring(equalsIndex + 1).Trim();
                        values[key] = value;
                    }
                }
            }

            if (!values.ContainsKey("PublicKey") || !values.ContainsKey("PrivateKey"))
            {
                throw new FormatException("Format invalide: PublicKey ou PrivateKey manquants");
            }

            byte[] publicKey = Convert.FromBase64String(values["PublicKey"]);
            byte[] privateKey = Convert.FromBase64String(values["PrivateKey"]);

            return new Ed25519KeyPairResult
            {
                PublicKey = publicKey,
                PrivateKey = privateKey,
                PublicKeyHex = values.ContainsKey("PublicKeyHex") 
                    ? values["PublicKeyHex"] 
                    : Ed25519Wrapper.BytesToHex(publicKey),
                PrivateKeyHex = values.ContainsKey("PrivateKeyHex") 
                    ? values["PrivateKeyHex"] 
                    : Ed25519Wrapper.BytesToHex(privateKey)
            };
        }

        private Ed25519KeyPairResult ImportFromBase64(string content)
        {
            var publicKeyMatch = Regex.Match(content, 
                @"-----BEGIN ED25519 PUBLIC KEY-----\s*(.+?)\s*-----END ED25519 PUBLIC KEY-----", 
                RegexOptions.Singleline | RegexOptions.IgnoreCase);
            
            if (!publicKeyMatch.Success)
            {
                throw new FormatException("Clé publique introuvable dans le fichier");
            }

            var privateKeyMatch = Regex.Match(content, 
                @"-----BEGIN ED25519 PRIVATE KEY-----\s*(.+?)\s*-----END ED25519 PRIVATE KEY-----", 
                RegexOptions.Singleline | RegexOptions.IgnoreCase);
            
            if (!privateKeyMatch.Success)
            {
                throw new FormatException("Clé privée introuvable dans le fichier");
            }

            byte[] publicKey = Convert.FromBase64String(publicKeyMatch.Groups[1].Value.Replace("\r", "").Replace("\n", "").Replace(" ", ""));
            byte[] privateKey = Convert.FromBase64String(privateKeyMatch.Groups[1].Value.Replace("\r", "").Replace("\n", "").Replace(" ", ""));

            return new Ed25519KeyPairResult
            {
                PublicKey = publicKey,
                PrivateKey = privateKey,
                PublicKeyHex = Ed25519Wrapper.BytesToHex(publicKey),
                PrivateKeyHex = Ed25519Wrapper.BytesToHex(privateKey)
            };
        }
    }
}

