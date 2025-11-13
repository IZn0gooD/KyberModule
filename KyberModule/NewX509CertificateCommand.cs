using System;
using System.IO;
using System.Management.Automation;
using KyberLibrary;
using Org.BouncyCastle.Crypto.Parameters;

namespace KyberModule
{
    /// <summary>
    /// Cmdlet pour créer un certificat X.509 auto-signé avec clé post-quantique
    /// </summary>
    [Cmdlet(VerbsCommon.New, "X509Certificate")]
    [OutputType(typeof(string))]
    public class NewX509CertificateCommand : PSCmdlet
    {
        [Parameter(
            Mandatory = true,
            Position = 0,
            HelpMessage = "Nom du sujet du certificat (ex: 'CN=Test Certificate, O=My Organization')")]
        public string SubjectName { get; set; }

        [Parameter(
            Mandatory = true,
            Position = 1,
            HelpMessage = "Clé publique (Kyber ou Dilithium)")]
        public byte[] PublicKey { get; set; }

        [Parameter(
            Mandatory = true,
            Position = 2,
            HelpMessage = "Clé privée correspondante (pour signer le certificat)")]
        public byte[] PrivateKey { get; set; }

        [Parameter(
            Mandatory = true,
            Position = 3,
            HelpMessage = "Chemin du fichier où sauvegarder le certificat")]
        public string Path { get; set; }

        [Parameter(
            Mandatory = false,
            HelpMessage = "Type de clé (Kyber ou Dilithium). Pour Kyber, une clé Dilithium séparée est nécessaire pour signer")]
        [ValidateSet("Kyber", "Dilithium")]
        public string KeyType { get; set; } = "Dilithium";

        [Parameter(
            Mandatory = false,
            HelpMessage = "Paramètre de sécurité (Kyber512/768/1024 ou Dilithium2/3/5)")]
        public string ParameterSet { get; set; } = "Dilithium3";

        [Parameter(
            Mandatory = false,
            HelpMessage = "Nombre de jours de validité du certificat")]
        public int ValidityDays { get; set; } = 365;

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

                byte[] certificateData;

                if (KeyType.Equals("Dilithium", StringComparison.OrdinalIgnoreCase))
                {
                    // Créer un certificat X.509 avec clé Dilithium
                    MLDsaParameters mlDsaParams = GetMLDsaParameters(ParameterSet);
                    X509Wrapper.X509Format formatEnum = Format.Equals("PEM", StringComparison.OrdinalIgnoreCase) 
                        ? X509Wrapper.X509Format.PEM 
                        : X509Wrapper.X509Format.DER;
                    
                    certificateData = X509Wrapper.CreateSelfSignedCertificate(
                        SubjectName,
                        PublicKey,
                        PrivateKey,
                        mlDsaParams,
                        ValidityDays,
                        formatEnum);
                }
                else // Kyber
                {
                    // Pour Kyber, on ne peut pas créer directement un certificat auto-signé
                    // car Kyber est un KEM, pas un algorithme de signature
                    // Il faudrait une clé Dilithium séparée pour signer
                    WriteError(new ErrorRecord(
                        new NotSupportedException("Les certificats X.509 avec clé Kyber nécessitent une clé Dilithium séparée pour la signature. Utilisez KeyType='Dilithium' pour créer un certificat auto-signé."),
                        "KyberCertificateNotSupported",
                        ErrorCategory.NotImplemented,
                        null));
                    return;
                }

                // Créer le répertoire si nécessaire
                string directory = System.IO.Path.GetDirectoryName(resolvedPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Écrire le fichier
                File.WriteAllBytes(resolvedPath, certificateData);

                WriteObject(resolvedPath);
                WriteVerbose($"Certificat X.509 créé et sauvegardé vers: {resolvedPath}");
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(ex, "X509CertificateCreationError", ErrorCategory.WriteError, Path));
            }
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

