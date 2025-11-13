using System;
using System.Management.Automation;
using KyberLibrary;

namespace KyberModule
{
    /// <summary>
    /// Cmdlet pour vérifier une signature Ed25519
    /// </summary>
    [Cmdlet(VerbsDiagnostic.Test, "Ed25519Signature")]
    [OutputType(typeof(bool))]
    public class TestEd25519SignatureCommand : PSCmdlet
    {
        [Parameter(
            Mandatory = true,
            Position = 0,
            HelpMessage = "Données originales (byte[] ou chaîne)")]
        [AllowNull]
        public object Data { get; set; }

        [Parameter(
            Mandatory = true,
            Position = 1,
            HelpMessage = "Signature à vérifier (byte[] ou chaîne hexadécimale)")]
        [AllowNull]
        public object Signature { get; set; }

        [Parameter(
            Mandatory = true,
            Position = 2,
            HelpMessage = "Clé publique Ed25519 (byte[] ou chaîne hexadécimale)")]
        [AllowNull]
        public object PublicKey { get; set; }

        protected override void ProcessRecord()
        {
            try
            {
                byte[] dataBytes;
                byte[] signatureBytes;
                byte[] publicKeyBytes;

                // Convertir les données
                if (Data is byte[] dataBytesArray)
                {
                    dataBytes = dataBytesArray;
                }
                else if (Data is string dataString)
                {
                    dataBytes = System.Text.Encoding.UTF8.GetBytes(dataString);
                }
                else
                {
                    throw new ArgumentException("Les données doivent être un byte[] ou une chaîne");
                }

                // Convertir la signature
                if (Signature is byte[] signatureBytesArray)
                {
                    signatureBytes = signatureBytesArray;
                }
                else if (Signature is string signatureHex)
                {
                    signatureBytes = Ed25519Wrapper.HexToBytes(signatureHex);
                }
                else
                {
                    throw new ArgumentException("La signature doit être un byte[] ou une chaîne hexadécimale");
                }

                // Convertir la clé publique
                if (PublicKey is byte[] publicKeyBytesArray)
                {
                    publicKeyBytes = publicKeyBytesArray;
                }
                else if (PublicKey is string publicKeyHex)
                {
                    publicKeyBytes = Ed25519Wrapper.HexToBytes(publicKeyHex);
                }
                else
                {
                    throw new ArgumentException("La clé publique doit être un byte[] ou une chaîne hexadécimale");
                }

                // Créer le wrapper Ed25519
                var ed25519 = new Ed25519Wrapper();

                // Vérifier la signature
                bool isValid = ed25519.Verify(dataBytes, signatureBytes, publicKeyBytes);

                WriteObject(isValid);
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(ex, "Ed25519VerificationError", ErrorCategory.NotSpecified, null));
            }
        }
    }
}

