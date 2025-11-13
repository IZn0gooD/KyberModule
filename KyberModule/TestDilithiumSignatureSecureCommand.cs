using System;
using System.Management.Automation;
using KyberLibrary;

namespace KyberModule
{
    /// <summary>
    /// Cmdlet pour vérifier une signature Dilithium avec protection contre les attaques par canaux auxiliaires
    /// </summary>
    [Cmdlet(VerbsDiagnostic.Test, "DilithiumSignatureSecure")]
    [OutputType(typeof(bool))]
    public class TestDilithiumSignatureSecureCommand : PSCmdlet
    {
        [Parameter(Mandatory = true, HelpMessage = "Les données originales (byte[] ou chaîne hexadécimale)")]
        public object Data { get; set; }

        [Parameter(Mandatory = true, HelpMessage = "La signature à vérifier (byte[] ou chaîne hexadécimale)")]
        public object Signature { get; set; }

        [Parameter(Mandatory = true, HelpMessage = "La clé publique Dilithium (byte[] ou chaîne hexadécimale)")]
        public object PublicKey { get; set; }

        [Parameter(Mandatory = false, HelpMessage = "Paramètre de sécurité Dilithium (Dilithium2, Dilithium3, Dilithium5). Par défaut: Dilithium3")]
        [ValidateSet("Dilithium2", "Dilithium3", "Dilithium5")]
        public string ParameterSet { get; set; } = "Dilithium3";

        [Parameter(Mandatory = false, HelpMessage = "Pré-hacher les données avec SHA3 avant de vérifier (doit correspondre à la signature)")]
        public SwitchParameter PreHash { get; set; }

        [Parameter(Mandatory = false, HelpMessage = "Variant SHA3 à utiliser pour le pré-hachage (SHA3_256 ou SHA3_384). Par défaut: SHA3_256")]
        [ValidateSet("SHA3_256", "SHA3_384")]
        public string SHA3Variant { get; set; } = "SHA3_256";

        protected override void ProcessRecord()
        {
            try
            {
                // Convertir les données
                byte[] dataBytes = ConvertToByteArray(Data, "Data");
                byte[] signatureBytes = ConvertToByteArray(Signature, "Signature");
                byte[] publicKeyBytes = ConvertToByteArray(PublicKey, "PublicKey");

                // Convertir le paramètre string en enum
                DilithiumWrapper.DilithiumParameterSet dilithiumParamSet;
                switch (ParameterSet.ToLower())
                {
                    case "dilithium2":
                        dilithiumParamSet = DilithiumWrapper.DilithiumParameterSet.Dilithium2;
                        break;
                    case "dilithium3":
                        dilithiumParamSet = DilithiumWrapper.DilithiumParameterSet.Dilithium3;
                        break;
                    case "dilithium5":
                        dilithiumParamSet = DilithiumWrapper.DilithiumParameterSet.Dilithium5;
                        break;
                    default:
                        dilithiumParamSet = DilithiumWrapper.DilithiumParameterSet.Dilithium3;
                        break;
                }

                // Créer le wrapper Dilithium
                var dilithium = new DilithiumWrapper(dilithiumParamSet);

                // Vérifier la signature avec protection side-channel
                bool isValid;
                if (PreHash.IsPresent)
                {
                    // Utiliser SHA3 pour le pré-hachage
                    SHA3Wrapper.SHA3Variant sha3Variant = SHA3Variant.ToUpper() == "SHA3_384"
                        ? SHA3Wrapper.SHA3Variant.SHA3_384
                        : SHA3Wrapper.SHA3Variant.SHA3_256;
                    // Note: VerifySecure utilise Verify en interne, donc on doit pré-hacher manuellement
                    byte[] hashedData = SHA3Wrapper.ComputeHash(dataBytes, sha3Variant);
                    isValid = dilithium.VerifySecure(hashedData, signatureBytes, publicKeyBytes, true);
                }
                else
                {
                    // Vérifier sans pré-hachage
                    isValid = dilithium.VerifySecure(dataBytes, signatureBytes, publicKeyBytes, false);
                }

                WriteObject(isValid);
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(ex, "DilithiumVerifySecureError", ErrorCategory.NotSpecified, null));
            }
        }

        private byte[] ConvertToByteArray(object value, string parameterName)
        {
            if (value == null)
            {
                throw new ArgumentNullException(parameterName);
            }

            if (value is byte[] bytes)
            {
                return bytes;
            }

            if (value is string hexString)
            {
                return DilithiumWrapper.HexToBytes(hexString);
            }

            throw new ArgumentException($"Le paramètre {parameterName} doit être un byte[] ou une chaîne hexadécimale");
        }
    }
}

