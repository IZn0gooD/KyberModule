using System;
using System.Management.Automation;
using KyberLibrary;

namespace KyberModule
{
    /// <summary>
    /// Cmdlet pour démarrer un serveur de communication sécurisée
    /// </summary>
    [Obsolete("Utiliser le service KyberDaemon.")]
    [Cmdlet(VerbsLifecycle.Start, "SecureServer")]
    public class StartSecureServerCommand : PSCmdlet
    {
        [Parameter(Mandatory = true, Position = 0, HelpMessage = "Port d'écoute du serveur")]
        [ValidateRange(1, 65535)]
        public int Port { get; set; }

        [Parameter(Mandatory = false, HelpMessage = "Utiliser ChaCha20-Poly1305 (par défaut) ou AES-GCM")]
        public SwitchParameter UseAESGCM { get; set; }

        [Parameter(Mandatory = false, HelpMessage = "Nombre maximum de connexions simultanées")]
        [ValidateRange(1, 1000)]
        public int MaxConnections { get; set; } = 100;

        protected override void ProcessRecord()
        {
            WriteWarning("Start-SecureServer est obsolète. Utilisez KyberDaemon pour lancer le service.");
            ThrowTerminatingError(new ErrorRecord(
                new NotSupportedException("Start-SecureServer n'est plus pris en charge."),
                "StartSecureServerDeprecated",
                ErrorCategory.NotImplemented,
                null));
        }
    }
}

