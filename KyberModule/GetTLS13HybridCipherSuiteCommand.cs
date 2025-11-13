using System;
using System.Management.Automation;
using KyberLibrary;

namespace KyberModule
{
    /// <summary>
    /// Cmdlet pour obtenir le nom du cipher suite TLS 1.3 hybride
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "TLS13HybridCipherSuite")]
    [OutputType(typeof(string))]
    public class GetTLS13HybridCipherSuiteCommand : PSCmdlet
    {
        [Parameter(
            Mandatory = true,
            Position = 0,
            HelpMessage = "Configuration TLS hybride")]
        [ValidateNotNull]
        public TLS13HybridWrapper.HybridTLSConfig Config { get; set; }

        protected override void ProcessRecord()
        {
            try
            {
                if (!TLS13HybridWrapper.ValidateConfig(Config))
                {
                    WriteError(new ErrorRecord(
                        new ArgumentException("La configuration TLS hybride n'est pas valide."),
                        "InvalidConfig",
                        ErrorCategory.InvalidArgument,
                        Config));
                    return;
                }

                string cipherSuite = TLS13HybridWrapper.GetCipherSuiteName(Config);
                WriteObject(cipherSuite);
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(ex, "TLS13HybridCipherSuiteError", ErrorCategory.NotSpecified, Config));
            }
        }
    }
}

