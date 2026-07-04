using System;
using System.Management.Automation;
using System.Threading;
using System.Threading.Tasks;
using KyberLibrary;

namespace KyberModule
{
    /// <summary>
    /// Cmdlet pour se connecter à un serveur de communication sécurisée
    /// </summary>
    [Obsolete("Utiliser KyberCLI pour les connexions.")]
    [Cmdlet(VerbsCommunications.Connect, "SecureClient")]
    public class ConnectSecureClientCommand : PSCmdlet
    {
        [Parameter(Mandatory = true, Position = 0, HelpMessage = "Adresse du serveur")]
        public string ServerHost { get; set; }

        [Parameter(Mandatory = true, Position = 1, HelpMessage = "Port du serveur")]
        [ValidateRange(1, 65535)]
        public int Port { get; set; }

        [Parameter(Mandatory = false, HelpMessage = "Utiliser ChaCha20-Poly1305 (par défaut) ou AES-GCM")]
        public SwitchParameter UseAESGCM { get; set; }

        [Parameter(Mandatory = false, HelpMessage = "Timeout de connexion en millisecondes")]
        [ValidateRange(1000, 300000)]
        public int Timeout { get; set; } = 30000;

        protected override void ProcessRecord()
        {
            WriteWarning("Connect-SecureClient est obsolète. Utilisez KyberCLI pour établir une session.");
            ThrowTerminatingError(new ErrorRecord(
                new NotSupportedException("Connect-SecureClient n'est plus pris en charge."),
                "ConnectSecureClientDeprecated",
                ErrorCategory.NotImplemented,
                null));
        }
    }
}

