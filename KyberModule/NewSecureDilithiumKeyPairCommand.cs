using System;
using System.Management.Automation;
using KyberLibrary;

namespace KyberModule
{
    /// <summary>
    /// Cmdlet pour générer une paire de clés Dilithium avec gestion sécurisée en mémoire
    /// La clé privée est automatiquement nettoyée lors de la destruction de l'objet
    /// </summary>
    [Cmdlet(VerbsCommon.New, "SecureDilithiumKeyPair")]
    [OutputType(typeof(SecureDilithiumKeyPairResult))]
    public class NewSecureDilithiumKeyPairCommand : PSCmdlet
    {
        [Parameter(
            Mandatory = false,
            Position = 0,
            HelpMessage = "Paramètre de sécurité Dilithium (Dilithium2, Dilithium3, Dilithium5)")]
        [ValidateSet("Dilithium2", "Dilithium3", "Dilithium5")]
        public string ParameterSet { get; set; } = "Dilithium3";

        [Parameter(
            Mandatory = false,
            HelpMessage = "Nettoyer immédiatement la clé privée après génération (pour tests uniquement)")]
        public SwitchParameter ZeroizeImmediately { get; set; }

        protected override void ProcessRecord()
        {
            try
            {
                // Convertir le paramètre string en enum
                DilithiumWrapper.DilithiumParameterSet paramSet = ParameterSet switch
                {
                    "Dilithium2" => DilithiumWrapper.DilithiumParameterSet.Dilithium2,
                    "Dilithium3" => DilithiumWrapper.DilithiumParameterSet.Dilithium3,
                    "Dilithium5" => DilithiumWrapper.DilithiumParameterSet.Dilithium5,
                    _ => DilithiumWrapper.DilithiumParameterSet.Dilithium3
                };

                // Créer le wrapper Dilithium
                var dilithium = new DilithiumWrapper(paramSet);

                // Générer la paire de clés sécurisée
                var secureKeyPair = dilithium.GenerateKeyPairSecure();

                // Obtenir les clés (copies)
                byte[] publicKey = secureKeyPair.GetPublicKey();
                byte[] privateKey = secureKeyPair.GetPrivateKey();

                // Créer l'objet résultat
                var result = new SecureDilithiumKeyPairResult
                {
                    PublicKey = publicKey,
                    PrivateKey = privateKey,
                    PublicKeyHex = BitConverter.ToString(publicKey).Replace("-", ""),
                    PrivateKeyHex = BitConverter.ToString(privateKey).Replace("-", ""),
                    ParameterSet = ParameterSet,
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
                WriteError(new ErrorRecord(ex, "SecureDilithiumKeyGenerationError", ErrorCategory.NotSpecified, null));
            }
        }
    }

    /// <summary>
    /// Résultat de la génération de clés Dilithium sécurisée
    /// </summary>
    public class SecureDilithiumKeyPairResult
    {
        public byte[] PublicKey { get; set; }
        public byte[] PrivateKey { get; set; }
        public string PublicKeyHex { get; set; }
        public string PrivateKeyHex { get; set; }
        public string ParameterSet { get; set; }
        
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

