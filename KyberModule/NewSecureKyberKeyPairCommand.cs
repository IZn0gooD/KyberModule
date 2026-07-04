using System;
using System.Management.Automation;
using KyberLibrary;

namespace KyberModule
{
    /// <summary>
    /// Cmdlet pour générer une paire de clés Kyber avec gestion sécurisée en mémoire
    /// La clé privée est automatiquement nettoyée lors de la destruction de l'objet
    /// </summary>
    [Cmdlet(VerbsCommon.New, "SecureKyberKeyPair")]
    [OutputType(typeof(SecureKyberKeyPairResult))]
    public class NewSecureKyberKeyPairCommand : PSCmdlet
    {
        [Parameter(
            Mandatory = false,
            Position = 0,
            HelpMessage = "Paramètre de sécurité Kyber (Kyber512, Kyber768, Kyber1024)")]
        [ValidateSet("Kyber512", "Kyber768", "Kyber1024")]
        public string ParameterSet { get; set; } = "Kyber768";

        [Parameter(
            Mandatory = false,
            HelpMessage = "Nettoyer immédiatement la clé privée après génération (pour tests uniquement)")]
        public SwitchParameter ZeroizeImmediately { get; set; }

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

                // Générer la paire de clés sécurisée
                var secureKeyPair = kyber.GenerateKeyPairSecure();

                // Obtenir les clés (copies)
                byte[] publicKey = secureKeyPair.GetPublicKey();
                byte[] privateKey = secureKeyPair.GetPrivateKey();

                // Créer l'objet résultat qui gère le Dispose
                var result = new SecureKyberKeyPairResult
                {
                    PublicKey = publicKey,
                    PrivateKey = privateKey,
                    PublicKeyHex = KyberWrapper.BytesToHex(publicKey),
                    PrivateKeyHex = KyberWrapper.BytesToHex(privateKey),
                    ParameterSet = ParameterSet,
                    SecureKeyPairWrapper = secureKeyPair
                };

                // Si demandé, nettoyer immédiatement (pour tests)
                if (ZeroizeImmediately)
                {
                    secureKeyPair.ZeroizePrivateKey();
                    WriteWarning("La clé privée a été nettoyée immédiatement. Elle ne peut plus être utilisée.");
                }

                WriteObject(result);
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(ex, "SecureKyberKeyGenerationError", ErrorCategory.NotSpecified, null));
            }
        }
    }

    /// <summary>
    /// Résultat de la génération de clés sécurisée
    /// Contient une référence au wrapper sécurisé pour gestion du nettoyage
    /// </summary>
    public class SecureKyberKeyPairResult
    {
        public byte[] PublicKey { get; set; }
        public byte[] PrivateKey { get; set; }
        public string PublicKeyHex { get; set; }
        public string PrivateKeyHex { get; set; }
        public string ParameterSet { get; set; }
        
        // Référence interne au wrapper (pour nettoyage automatique)
        // Ne pas exposer directement pour éviter les fuites
        internal SecureKeyPairWrapper SecureKeyPairWrapper { get; set; }

        /// <summary>
        /// Nettoie immédiatement la clé privée
        /// </summary>
        public void ZeroizePrivateKey()
        {
            SecureKeyPairWrapper?.ZeroizePrivateKey();
            // Vider aussi la copie locale
            if (PrivateKey != null)
            {
                SecureKeyManager.Zeroize(PrivateKey);
            }
        }
    }
}

