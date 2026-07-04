using System;
using System.Management.Automation;
using KyberLibrary;

namespace KyberModule
{
    /// <summary>
    /// Cmdlet pour signer des données avec une clé privée Ed25519
    /// </summary>
    [Cmdlet(VerbsLifecycle.Invoke, "Ed25519Sign")]
    [OutputType(typeof(Ed25519SignatureResult))]
    public class InvokeEd25519SignCommand : PSCmdlet
    {
        [Parameter(
            Mandatory = true,
            Position = 0,
            HelpMessage = "Données à signer (byte[] ou chaîne)")]
        [AllowNull]
        public object Data { get; set; }

        [Parameter(
            Mandatory = true,
            Position = 1,
            HelpMessage = "Clé privée Ed25519 (byte[] ou chaîne hexadécimale)")]
        [AllowNull]
        public object PrivateKey { get; set; }

        protected override void ProcessRecord()
        {
            try
            {
                byte[] dataBytes;
                byte[] privateKeyBytes;

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

                // Convertir la clé privée
                if (PrivateKey is byte[] privateKeyBytesArray)
                {
                    privateKeyBytes = privateKeyBytesArray;
                }
                else if (PrivateKey is string privateKeyHex)
                {
                    privateKeyBytes = Ed25519Wrapper.HexToBytes(privateKeyHex);
                }
                else
                {
                    throw new ArgumentException("La clé privée doit être un byte[] ou une chaîne hexadécimale");
                }

                // Créer le wrapper Ed25519
                var ed25519 = new Ed25519Wrapper();

                // Signer
                byte[] signature = ed25519.Sign(dataBytes, privateKeyBytes);

                // Créer l'objet résultat
                var result = new Ed25519SignatureResult
                {
                    Signature = signature,
                    SignatureHex = Ed25519Wrapper.BytesToHex(signature),
                    Data = dataBytes
                };

                WriteObject(result);
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(ex, "Ed25519SigningError", ErrorCategory.NotSpecified, null));
            }
        }
    }

    /// <summary>
    /// Résultat de la signature Ed25519
    /// </summary>
    public class Ed25519SignatureResult
    {
        public byte[] Signature { get; set; }
        public string SignatureHex { get; set; }
        public byte[] Data { get; set; }
    }
}

