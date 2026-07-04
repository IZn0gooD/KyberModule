using System;
using System.Management.Automation;
using KyberLibrary;

namespace KyberModule
{
    /// <summary>
    /// Cmdlet pour nettoyer de manière sécurisée une clé en mémoire (zeroization)
    /// </summary>
    [Cmdlet(VerbsCommon.Clear, "SecureKey")]
    [OutputType(typeof(bool))]
    public class ClearSecureKeyCommand : PSCmdlet
    {
        [Parameter(
            Mandatory = true,
            Position = 0,
            ValueFromPipeline = true,
            HelpMessage = "Clé à nettoyer (byte[])")]
        [ValidateNotNull]
        public byte[] Key { get; set; }

        [Parameter(
            Mandatory = false,
            HelpMessage = "Nombre de passes de nettoyage (défaut: 1, recommandé: 3 pour sécurité renforcée)")]
        [ValidateRange(1, 10)]
        public int Passes { get; set; } = 1;

        [Parameter(
            Mandatory = false,
            HelpMessage = "Mettre la référence à null après nettoyage")]
        public SwitchParameter Nullify { get; set; }

        protected override void ProcessRecord()
        {
            try
            {
                if (Key == null || Key.Length == 0)
                {
                    WriteWarning("La clé est null ou vide. Aucun nettoyage nécessaire.");
                    WriteObject(false);
                    return;
                }

                WriteVerbose($"Nettoyage de la clé avec {Passes} passe(s)...");

                // Nettoyer selon le nombre de passes
                if (Passes == 1)
                {
                    SecureKeyManager.Zeroize(Key);
                }
                else
                {
                    SecureKeyManager.ZeroizeMultiplePasses(Key, Passes);
                }

                // Vérifier le nettoyage
                bool isZeroized = SecureKeyManager.IsZeroized(Key);
                
                if (isZeroized)
                {
                    WriteVerbose("La clé a été nettoyée avec succès.");
                }
                else
                {
                    WriteWarning("La clé n'a pas été complètement nettoyée.");
                }

                // Mettre à null si demandé
                if (Nullify)
                {
                    Key = null;
                    WriteVerbose("La référence a été mise à null.");
                }

                WriteObject(isZeroized);
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(ex, "SecureKeyClearError", ErrorCategory.NotSpecified, Key));
            }
        }
    }
}

