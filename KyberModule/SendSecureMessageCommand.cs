using System;
using System.Management.Automation;
using System.Text;
using System.Threading;
using KyberLibrary;

namespace KyberModule
{
    /// <summary>
    /// Cmdlet pour envoyer un message sécurisé
    /// </summary>
    [Obsolete("Utiliser KyberCLI pour communiquer avec KyberDaemon.")]
    [Cmdlet(VerbsCommunications.Send, "SecureMessage")]
    public class SendSecureMessageCommand : PSCmdlet
    {
        [Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true, HelpMessage = "Client de communication sécurisée")]
        public SecureCommunicationClient Client { get; set; }

        [Parameter(Mandatory = true, Position = 1, HelpMessage = "Message à envoyer")]
        public string Message { get; set; }

        [Parameter(Mandatory = false, HelpMessage = "Envoyer les données comme bytes au lieu de texte")]
        public byte[] Data { get; set; }

        protected override void ProcessRecord()
        {
            WriteWarning("Send-SecureMessage est obsolète. Utilisez KyberCLI pour envoyer des messages.");
            ThrowTerminatingError(new ErrorRecord(
                new NotSupportedException("Send-SecureMessage n'est plus pris en charge."),
                "SendSecureMessageDeprecated",
                ErrorCategory.NotImplemented,
                null));
        }
    }
}

