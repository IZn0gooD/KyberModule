using System;
using System.IO;
using System.Management.Automation;
using System.Text;
using KyberLibrary;
using Org.BouncyCastle.Crypto.Parameters;

namespace KyberModule
{
    /// <summary>
    /// Cmdlet pour exporter une clé privée au format PKCS#8
    /// </summary>
    [Cmdlet(VerbsData.Export, "PrivateKeyPKCS8")]
    [OutputType(typeof(string))]
    public class ExportPrivateKeyPKCS8Command : PSCmdlet
    {
        [Parameter(
            Mandatory = true,
            Position = 0,
            HelpMessage = "Clé privée à exporter (bytes)")]
        public byte[] PrivateKey { get; set; }

        [Parameter(
            Mandatory = true,
            Position = 1,
            HelpMessage = "Chemin du fichier où sauvegarder la clé PKCS#8")]
        public string Path { get; set; }

        [Parameter(
            Mandatory = false,
            HelpMessage = "Type de clé (Kyber ou Dilithium)")]
        [ValidateSet("Kyber", "Dilithium")]
        public string KeyType { get; set; } = "Kyber";

        [Parameter(
            Mandatory = false,
            HelpMessage = "Paramètre de sécurité (Kyber512/768/1024 ou Dilithium2/3/5)")]
        public string ParameterSet { get; set; } = "Kyber768";

        [Parameter(
            Mandatory = false,
            HelpMessage = "Format de sortie (DER ou PEM). Par défaut: PEM")]
        [ValidateSet("DER", "PEM")]
        public string Format { get; set; } = "PEM";

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

                byte[] pkcs8Data;

                if (KeyType.Equals("Kyber", StringComparison.OrdinalIgnoreCase))
                {
                    // Obtenir les paramètres ML-KEM
                    MLKemParameters mlKemParams = GetMLKemParameters(ParameterSet);
                    PKCS8Wrapper.PKCSFormat formatEnum = Format.Equals("PEM", StringComparison.OrdinalIgnoreCase) 
                        ? PKCS8Wrapper.PKCSFormat.PEM 
                        : PKCS8Wrapper.PKCSFormat.DER;
                    
                    pkcs8Data = PKCS8Wrapper.ExportPrivateKey(PrivateKey, mlKemParams, formatEnum);
                }
                else // Dilithium
                {
                    // Obtenir les paramètres ML-DSA
                    MLDsaParameters mlDsaParams = GetMLDsaParameters(ParameterSet);
                    PKCS8Wrapper.PKCSFormat formatEnum = Format.Equals("PEM", StringComparison.OrdinalIgnoreCase) 
                        ? PKCS8Wrapper.PKCSFormat.PEM 
                        : PKCS8Wrapper.PKCSFormat.DER;
                    
                    pkcs8Data = PKCS8Wrapper.ExportPrivateKey(PrivateKey, mlDsaParams, formatEnum);
                }

                // Créer le répertoire si nécessaire
                string directory = System.IO.Path.GetDirectoryName(resolvedPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Écrire le fichier
                File.WriteAllBytes(resolvedPath, pkcs8Data);

                WriteObject(resolvedPath);
                WriteVerbose($"Clé privée exportée en PKCS#8 vers: {resolvedPath}");
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(ex, "ExportPKCS8Error", ErrorCategory.WriteError, Path));
            }
        }

        private MLKemParameters GetMLKemParameters(string parameterSet)
        {
            return parameterSet.ToUpper() switch
            {
                "KYBER512" => MLKemParameters.ml_kem_512,
                "KYBER768" => MLKemParameters.ml_kem_768,
                "KYBER1024" => MLKemParameters.ml_kem_1024,
                _ => MLKemParameters.ml_kem_768
            };
        }

        private MLDsaParameters GetMLDsaParameters(string parameterSet)
        {
            return parameterSet.ToUpper() switch
            {
                "DILITHIUM2" or "DILITHIUM44" or "MLDSA44" => MLDsaParameters.ml_dsa_44,
                "DILITHIUM3" or "DILITHIUM65" or "MLDSA65" => MLDsaParameters.ml_dsa_65,
                "DILITHIUM5" or "DILITHIUM87" or "MLDSA87" => MLDsaParameters.ml_dsa_87,
                _ => MLDsaParameters.ml_dsa_65
            };
        }
    }
}

