using System;
using System.IO;
using System.Management.Automation;
using KyberLibrary;

namespace KyberModule
{
    /// <summary>
    /// Cmdlet pour importer une clé privée depuis un format PKCS#8
    /// </summary>
    [Cmdlet(VerbsData.Import, "PrivateKeyPKCS8")]
    [OutputType(typeof(byte[]))]
    public class ImportPrivateKeyPKCS8Command : PSCmdlet
    {
        [Parameter(
            Mandatory = true,
            Position = 0,
            HelpMessage = "Chemin du fichier contenant la clé PKCS#8")]
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

                byte[] pkcs8Data = File.ReadAllBytes(resolvedPath);
                byte[] privateKey = PKCS8Wrapper.ImportPrivateKey(pkcs8Data);

                WriteObject(privateKey);
                WriteVerbose($"Clé privée importée depuis PKCS#8: {resolvedPath}");
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(ex, "ImportPKCS8Error", ErrorCategory.ReadError, Path));
            }
        }
    }
}

