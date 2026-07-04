using System;
using System.Management.Automation;
using KyberLibrary;

namespace KyberModule
{
    /// <summary>
    /// Cmdlet pour générer une paire de clés Kyber
    /// </summary>
    [Cmdlet(VerbsCommon.New, "KyberKeyPair")]
    [OutputType(typeof(KyberKeyPairResult))]
    public class GetKyberKeyPairCommand : PSCmdlet
    {
        [Parameter(
            Mandatory = false,
            Position = 0,
            HelpMessage = "Paramètre de sécurité Kyber (Kyber512, Kyber768, Kyber1024)")]
        [ValidateSet("Kyber512", "Kyber768", "Kyber1024")]
        public string ParameterSet { get; set; } = "Kyber768";

        protected override void ProcessRecord()
        {
            try
            {
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

                // Générer la paire de clés
                var (publicKey, privateKey) = kyber.GenerateKeyPair();

                // Créer l'objet résultat
                var result = new KyberKeyPairResult
                {
                    PublicKey = publicKey,
                    PrivateKey = privateKey,
                    PublicKeyHex = KyberWrapper.BytesToHex(publicKey),
                    PrivateKeyHex = KyberWrapper.BytesToHex(privateKey),
                    ParameterSet = ParameterSet
                };

                WriteObject(result);
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(ex, "KyberKeyGenerationError", ErrorCategory.NotSpecified, null));
            }
        }
    }

    /// <summary>
    /// Résultat de la génération de clés
    /// </summary>
    public class KyberKeyPairResult
    {
        public byte[] PublicKey { get; set; }
        public byte[] PrivateKey { get; set; }
        public string PublicKeyHex { get; set; }
        public string PrivateKeyHex { get; set; }
        public string ParameterSet { get; set; }
    }
}

