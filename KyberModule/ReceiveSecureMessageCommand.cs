using System;
using System.Management.Automation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using KyberLibrary;

namespace KyberModule
{
    /// <summary>
    /// Cmdlet pour recevoir un message sécurisé
    /// </summary>
    [Obsolete("Utiliser KyberCLI pour interagir avec KyberDaemon.")]
    [Cmdlet(VerbsCommunications.Receive, "SecureMessage")]
    public class ReceiveSecureMessageCommand : PSCmdlet
    {
        [Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true, HelpMessage = "Client de communication sécurisée")]
        public SecureCommunicationClient Client { get; set; }

        [Parameter(Mandatory = false, HelpMessage = "Retourner les données comme bytes au lieu de texte")]
        public SwitchParameter AsBytes { get; set; }

        [Parameter(Mandatory = false, HelpMessage = "Timeout en millisecondes")]
        [ValidateRange(1000, 300000)]
        public int Timeout { get; set; } = 30000;

        protected override void ProcessRecord()
        {
            WriteWarning("Receive-SecureMessage est obsolète. Utilisez KyberCLI pour recevoir les réponses.");
            ThrowTerminatingError(new ErrorRecord(
                new NotSupportedException("Receive-SecureMessage n'est plus pris en charge."),
                "ReceiveSecureMessageDeprecated",
                ErrorCategory.NotImplemented,
                null));
        }
    }
}

