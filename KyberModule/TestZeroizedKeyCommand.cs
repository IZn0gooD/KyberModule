using System;
using System.Management.Automation;
using KyberLibrary;

namespace KyberModule
{
    /// <summary>
    /// Cmdlet pour vérifier si une clé a été nettoyée (zeroized)
    /// </summary>
    [Cmdlet(VerbsDiagnostic.Test, "ZeroizedKey")]
    [OutputType(typeof(bool))]
    public class TestZeroizedKeyCommand : PSCmdlet
    {
        [Parameter(
            Mandatory = true,
            Position = 0,
            ValueFromPipeline = true,
            HelpMessage = "Clé à vérifier (byte[])")]
        [AllowNull]
        public byte[] Key { get; set; }

        protected override void ProcessRecord()
        {
            try
            {
                if (Key == null)
                {
                    WriteVerbose("La clé est null. Considérée comme nettoyée.");
                    WriteObject(true);
                    return;
                }

                if (Key.Length == 0)
                {
                    WriteVerbose("La clé est vide. Considérée comme nettoyée.");
                    WriteObject(true);
                    return;
                }

                bool isZeroized = SecureKeyManager.IsZeroized(Key);
                
                if (isZeroized)
                {
                    WriteVerbose("La clé est complètement nettoyée (contient uniquement des zéros).");
                }
                else
                {
                    WriteVerbose("La clé n'est pas nettoyée (contient des données non nulles).");
                }

                WriteObject(isZeroized);
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(ex, "ZeroizedKeyTestError", ErrorCategory.NotSpecified, Key));
            }
        }
    }
}

