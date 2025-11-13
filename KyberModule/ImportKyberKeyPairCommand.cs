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
    /// Cmdlet pour importer une paire de clés Kyber depuis un fichier
    /// </summary>
    [Cmdlet(VerbsData.Import, "KyberKeyPair")]
    [OutputType(typeof(KyberKeyPairResult))]
    public class ImportKyberKeyPairCommand : PSCmdlet
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

                KyberKeyPairResult keyPair;

                // Détecter le format
                if (content.IndexOf("-----BEGIN KYBER", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    // Format Base64/PEM
                    keyPair = ImportFromBase64(content);
                }
                else if (content.IndexOf("Version=", StringComparison.OrdinalIgnoreCase) >= 0 || 
                         content.IndexOf("ParameterSet=", StringComparison.OrdinalIgnoreCase) >= 0)
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
                WriteVerbose($"Paire de clés importée depuis: {resolvedPath}");
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(ex, "ImportError", ErrorCategory.ReadError, Path));
            }
        }

        private KyberKeyPairResult ImportFromTextFormat(string textContent)
        {
            // Parser le format texte simple (Key=Value)
            Dictionary<string, string> values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            
            using (StringReader reader = new StringReader(textContent))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    // Ignorer les commentaires et lignes vides
                    line = line.Trim();
                    if (string.IsNullOrEmpty(line) || line.StartsWith("#"))
                        continue;
                    
                    // Extraire Key=Value
                    int equalsIndex = line.IndexOf('=');
                    if (equalsIndex > 0 && equalsIndex < line.Length - 1)
                    {
                        string key = line.Substring(0, equalsIndex).Trim();
                        string value = line.Substring(equalsIndex + 1).Trim();
                        values[key] = value;
                    }
                }
            }

            // Extraire les valeurs nécessaires
            if (!values.ContainsKey("PublicKey") || !values.ContainsKey("PrivateKey"))
            {
                throw new FormatException("Format invalide: PublicKey ou PrivateKey manquants");
            }

            string parameterSet = values.ContainsKey("ParameterSet") 
                ? values["ParameterSet"] 
                : "Kyber768";

            byte[] publicKey = Convert.FromBase64String(values["PublicKey"]);
            byte[] privateKey = Convert.FromBase64String(values["PrivateKey"]);

            return new KyberKeyPairResult
            {
                PublicKey = publicKey,
                PrivateKey = privateKey,
                PublicKeyHex = values.ContainsKey("PublicKeyHex") 
                    ? values["PublicKeyHex"] 
                    : KyberWrapper.BytesToHex(publicKey),
                PrivateKeyHex = values.ContainsKey("PrivateKeyHex") 
                    ? values["PrivateKeyHex"] 
                    : KyberWrapper.BytesToHex(privateKey),
                ParameterSet = parameterSet
            };
        }

        private KyberKeyPairResult ImportFromBase64(string content)
        {
            // Extraire la clé publique
            var publicKeyMatch = Regex.Match(content, 
                @"-----BEGIN KYBER PUBLIC KEY-----\s*(.+?)\s*-----END KYBER PUBLIC KEY-----", 
                RegexOptions.Singleline | RegexOptions.IgnoreCase);
            
            if (!publicKeyMatch.Success)
            {
                throw new FormatException("Clé publique introuvable dans le fichier");
            }

            // Extraire la clé privée
            var privateKeyMatch = Regex.Match(content, 
                @"-----BEGIN KYBER PRIVATE KEY-----\s*(.+?)\s*-----END KYBER PRIVATE KEY-----", 
                RegexOptions.Singleline | RegexOptions.IgnoreCase);
            
            if (!privateKeyMatch.Success)
            {
                throw new FormatException("Clé privée introuvable dans le fichier");
            }

            // Extraire le ParameterSet si présent
            var paramSetMatch = Regex.Match(content, @"#\s*ParameterSet:\s*(\w+)", RegexOptions.IgnoreCase);
            string parameterSet = paramSetMatch.Success ? paramSetMatch.Groups[1].Value : "Kyber768";

            byte[] publicKey = Convert.FromBase64String(publicKeyMatch.Groups[1].Value.Replace("\r", "").Replace("\n", "").Replace(" ", ""));
            byte[] privateKey = Convert.FromBase64String(privateKeyMatch.Groups[1].Value.Replace("\r", "").Replace("\n", "").Replace(" ", ""));

            return new KyberKeyPairResult
            {
                PublicKey = publicKey,
                PrivateKey = privateKey,
                PublicKeyHex = KyberWrapper.BytesToHex(publicKey),
                PrivateKeyHex = KyberWrapper.BytesToHex(privateKey),
                ParameterSet = parameterSet
            };
        }
    }
}

