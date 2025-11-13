using System;
using System.IO;
using System.Management.Automation;
using KyberLibrary;

namespace KyberModule
{
    /// <summary>
    /// Cmdlet pour exporter une configuration TLS 1.3 hybride
    /// </summary>
    [Cmdlet(VerbsData.Export, "TLS13HybridConfig")]
    public class ExportTLS13HybridConfigCommand : PSCmdlet
    {
        [Parameter(
            Mandatory = true,
            Position = 0,
            HelpMessage = "Configuration TLS hybride à exporter")]
        [ValidateNotNull]
        public TLS13HybridWrapper.HybridTLSConfig Config { get; set; }

        [Parameter(
            Mandatory = true,
            Position = 1,
            HelpMessage = "Chemin du fichier de sortie")]
        [ValidateNotNullOrEmpty]
        public string Path { get; set; }

        [Parameter(
            Mandatory = false,
            HelpMessage = "Surcharger le fichier s'il existe")]
        public SwitchParameter Force { get; set; }

        protected override void ProcessRecord()
        {
            try
            {
                string resolvedPath = GetUnresolvedProviderPathFromPSPath(Path);

                if (File.Exists(resolvedPath) && !Force)
                {
                    WriteError(new ErrorRecord(
                        new IOException($"Le fichier '{resolvedPath}' existe déjà. Utilisez -Force pour le surcharger."),
                        "FileExists",
                        ErrorCategory.ResourceExists,
                        resolvedPath));
                    return;
                }

                if (!TLS13HybridWrapper.ValidateConfig(Config))
                {
                    WriteError(new ErrorRecord(
                        new ArgumentException("La configuration TLS hybride n'est pas valide."),
                        "InvalidConfig",
                        ErrorCategory.InvalidArgument,
                        Config));
                    return;
                }

                string configText = TLS13HybridWrapper.ExportConfigToText(Config);
                File.WriteAllText(resolvedPath, configText, System.Text.Encoding.UTF8);

                WriteVerbose($"Configuration TLS hybride exportée: {resolvedPath}");
                WriteObject(new { Path = resolvedPath, CipherSuite = TLS13HybridWrapper.GetCipherSuiteName(Config) });
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(ex, "TLS13HybridConfigExportError", ErrorCategory.WriteError, Path));
            }
        }
    }
}

