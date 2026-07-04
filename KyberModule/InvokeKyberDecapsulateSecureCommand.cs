using System;
using System.Management.Automation;
using KyberLibrary;

namespace KyberModule
{
    /// <summary>
    /// Cmdlet pour décapsuler une clé partagée avec protection contre les attaques par canaux auxiliaires
    /// </summary>
    [Cmdlet(VerbsLifecycle.Invoke, "KyberDecapsulateSecure")]
    [OutputType(typeof(KyberDecapsulationResult))]
    public class InvokeKyberDecapsulateSecureCommand : PSCmdlet
    {
        [Parameter(
            Mandatory = true,
            Position = 0,
            HelpMessage = "Ciphertext (byte[] ou chaîne hexadécimale)")]
        [AllowNull]
        public object Ciphertext { get; set; }

        [Parameter(
            Mandatory = true,
            Position = 1,
            HelpMessage = "Clé privée (byte[] ou chaîne hexadécimale)")]
        [AllowNull]
        public object PrivateKey { get; set; }

        [Parameter(
            Mandatory = false,
            Position = 2,
            HelpMessage = "Paramètre de sécurité Kyber (Kyber512, Kyber768, Kyber1024)")]
        [ValidateSet("Kyber512", "Kyber768", "Kyber1024")]
        public string ParameterSet { get; set; } = "Kyber768";

        protected override void ProcessRecord()
        {
            try
            {
                byte[] ciphertextBytes;
                byte[] privateKeyBytes;

                // Convertir le ciphertext
                if (Ciphertext is string ciphertextHex)
                {
                    ciphertextBytes = KyberWrapper.HexToBytes(ciphertextHex);
                }
                else if (Ciphertext is byte[] ciphertextBytesArray)
                {
                    ciphertextBytes = ciphertextBytesArray;
                }
                else
                {
                    throw new ArgumentException("Le ciphertext doit être un byte[] ou une chaîne hexadécimale");
                }

                // Convertir la clé privée
                if (PrivateKey is string privateKeyHex)
                {
                    privateKeyBytes = KyberWrapper.HexToBytes(privateKeyHex);
                }
                else if (PrivateKey is byte[] privateKeyBytesArray)
                {
                    privateKeyBytes = privateKeyBytesArray;
                }
                else
                {
                    throw new ArgumentException("La clé privée doit être un byte[] ou une chaîne hexadécimale");
                }

                // Convertir le paramètre string en enum
                KyberWrapper.KyberParameterSet paramSet = ParameterSet switch
                {
                    "Kyber512" => KyberWrapper.KyberParameterSet.Kyber512,
                    "Kyber768" => KyberWrapper.KyberParameterSet.Kyber768,
                    "Kyber1024" => KyberWrapper.KyberParameterSet.Kyber1024,
                    _ => KyberWrapper.KyberParameterSet.Kyber768
                };

                // Créer le wrapper Kyber
                var kyber = new KyberWrapper(paramSet);

                // Décapsuler avec protection side-channel
                byte[] sharedSecret = kyber.DecapsulateSecure(ciphertextBytes, privateKeyBytes);

                // Créer l'objet résultat
                var result = new KyberDecapsulationResult
                {
                    SharedSecret = sharedSecret,
                    SharedSecretHex = KyberWrapper.BytesToHex(sharedSecret)
                };

                WriteObject(result);
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(ex, "KyberDecapsulationSecureError", ErrorCategory.NotSpecified, null));
            }
        }
    }
}

