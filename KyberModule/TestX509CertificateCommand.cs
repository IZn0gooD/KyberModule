using System;
using System.IO;
using System.Management.Automation;
using KyberLibrary;

namespace KyberModule
{
    /// <summary>
    /// Cmdlet pour vérifier un certificat X.509
    /// </summary>
    [Cmdlet(VerbsDiagnostic.Test, "X509Certificate")]
    [OutputType(typeof(bool))]
    public class TestX509CertificateCommand : PSCmdlet
    {
        [Parameter(
            Mandatory = true,
            Position = 0,
            HelpMessage = "Chemin du fichier contenant le certificat X.509")]
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

                byte[] certificateData = File.ReadAllBytes(resolvedPath);
                bool isValid = X509Wrapper.VerifyCertificate(certificateData);

                WriteObject(isValid);
                WriteVerbose($"Certificat X.509 {(isValid ? "valide" : "invalide")}: {resolvedPath}");
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(ex, "X509CertificateVerificationError", ErrorCategory.ReadError, Path));
            }
        }
    }
}

