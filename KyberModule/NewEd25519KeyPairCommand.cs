using System;
using System.Management.Automation;
using KyberLibrary;

namespace KyberModule
{
    /// <summary>
    /// Cmdlet pour générer une paire de clés Ed25519
    /// </summary>
    [Cmdlet(VerbsCommon.New, "Ed25519KeyPair")]
    [OutputType(typeof(Ed25519KeyPairResult))]
    public class NewEd25519KeyPairCommand : PSCmdlet
    {
        protected override void ProcessRecord()
        {
            try
            {
                // Créer le wrapper Ed25519
                var ed25519 = new Ed25519Wrapper();

                // Générer la paire de clés
                var (publicKey, privateKey) = ed25519.GenerateKeyPair();

                // Créer l'objet résultat
                var result = new Ed25519KeyPairResult
                {
                    PublicKey = publicKey,
                    PrivateKey = privateKey,
                    PublicKeyHex = Ed25519Wrapper.BytesToHex(publicKey),
                    PrivateKeyHex = Ed25519Wrapper.BytesToHex(privateKey)
                };

                WriteObject(result);
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(ex, "Ed25519KeyGenerationError", ErrorCategory.NotSpecified, null));
            }
        }
    }

    /// <summary>
    /// Résultat de la génération de clés Ed25519
    /// </summary>
    public class Ed25519KeyPairResult
    {
        public byte[] PublicKey { get; set; }
        public byte[] PrivateKey { get; set; }
        public string PublicKeyHex { get; set; }
        public string PrivateKeyHex { get; set; }
    }
}

