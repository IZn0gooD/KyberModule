using System;
using System.Management.Automation;
using KyberLibrary;

namespace KyberModule
{
    /// <summary>
    /// Cmdlet pour générer une paire de clés Ed25519 avec gestion sécurisée en mémoire
    /// La clé privée est automatiquement nettoyée lors de la destruction de l'objet
    /// </summary>
    [Cmdlet(VerbsCommon.New, "SecureEd25519KeyPair")]
    [OutputType(typeof(SecureEd25519KeyPairResult))]
    public class NewSecureEd25519KeyPairCommand : PSCmdlet
    {
        [Parameter(
            Mandatory = false,
            HelpMessage = "Nettoyer immédiatement la clé privée après génération (pour tests uniquement)")]
        public SwitchParameter ZeroizeImmediately { get; set; }

        protected override void ProcessRecord()
        {
            try
            {
                // Créer le wrapper Ed25519
                var ed25519 = new Ed25519Wrapper();

                // Générer la paire de clés sécurisée
                var secureKeyPair = ed25519.GenerateKeyPairSecure();

                // Obtenir les clés (copies)
                byte[] publicKey = secureKeyPair.GetPublicKey();
                byte[] privateKey = secureKeyPair.GetPrivateKey();

                // Créer l'objet résultat
                var result = new SecureEd25519KeyPairResult
                {
                    PublicKey = publicKey,
                    PrivateKey = privateKey,
                    PublicKeyHex = BitConverter.ToString(publicKey).Replace("-", ""),
                    PrivateKeyHex = BitConverter.ToString(privateKey).Replace("-", ""),
                    SecureKeyPairWrapper = secureKeyPair
                };

                // Si demandé, nettoyer immédiatement
                if (ZeroizeImmediately)
                {
                    secureKeyPair.ZeroizePrivateKey();
                    WriteWarning("La clé privée a été nettoyée immédiatement. Elle ne peut plus être utilisée.");
                }

                WriteObject(result);
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(ex, "SecureEd25519KeyGenerationError", ErrorCategory.NotSpecified, null));
            }
        }
    }

    /// <summary>
    /// Résultat de la génération de clés Ed25519 sécurisée
    /// </summary>
    public class SecureEd25519KeyPairResult
    {
        public byte[] PublicKey { get; set; }
        public byte[] PrivateKey { get; set; }
        public string PublicKeyHex { get; set; }
        public string PrivateKeyHex { get; set; }
        
        internal SecureKeyPairWrapper SecureKeyPairWrapper { get; set; }

        /// <summary>
        /// Nettoie immédiatement la clé privée
        /// </summary>
        public void ZeroizePrivateKey()
        {
            SecureKeyPairWrapper?.ZeroizePrivateKey();
            if (PrivateKey != null)
            {
                SecureKeyManager.Zeroize(PrivateKey);
            }
        }
    }
}

