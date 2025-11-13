using System;
using System.Management.Automation;
using KyberLibrary;

namespace KyberModule
{
    /// <summary>
    /// Cmdlet pour encapsuler une clé partagée avec une clé publique Kyber
    /// </summary>
    [Cmdlet(VerbsLifecycle.Invoke, "KyberEncapsulate")]
    [OutputType(typeof(KyberEncapsulationResult))]
    public class InvokeKyberEncapsulateCommand : PSCmdlet
    {
        [Parameter(
            Mandatory = true,
            Position = 0,
            HelpMessage = "Clé publique (byte[] ou chaîne hexadécimale)")]
        [AllowNull]
        public object PublicKey { get; set; }

        [Parameter(
            Mandatory = false,
            Position = 1,
            HelpMessage = "Paramètre de sécurité Kyber (Kyber512, Kyber768, Kyber1024)")]
        [ValidateSet("Kyber512", "Kyber768", "Kyber1024")]
        public string ParameterSet { get; set; } = "Kyber768";

        protected override void ProcessRecord()
        {
            try
            {
                byte[] publicKeyBytes;

                // Convertir la clé publique si c'est une chaîne hex
                if (PublicKey is string hexString)
                {
                    publicKeyBytes = KyberWrapper.HexToBytes(hexString);
                }
                else if (PublicKey is byte[] bytes)
                {
                    publicKeyBytes = bytes;
                }
                else
                {
                    throw new ArgumentException("La clé publique doit être un byte[] ou une chaîne hexadécimale");
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

                // Encapsuler
                var (ciphertext, sharedSecret) = kyber.Encapsulate(publicKeyBytes);

                // Créer l'objet résultat
                var result = new KyberEncapsulationResult
                {
                    Ciphertext = ciphertext,
                    SharedSecret = sharedSecret,
                    CiphertextHex = KyberWrapper.BytesToHex(ciphertext),
                    SharedSecretHex = KyberWrapper.BytesToHex(sharedSecret)
                };

                WriteObject(result);
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(ex, "KyberEncapsulationError", ErrorCategory.NotSpecified, null));
            }
        }
    }

    /// <summary>
    /// Résultat de l'encapsulation
    /// </summary>
    public class KyberEncapsulationResult
    {
        public byte[] Ciphertext { get; set; }
        public byte[] SharedSecret { get; set; }
        public string CiphertextHex { get; set; }
        public string SharedSecretHex { get; set; }
    }
}

