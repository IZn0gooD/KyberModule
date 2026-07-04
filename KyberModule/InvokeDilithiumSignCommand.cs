using System;
using System.Management.Automation;
using KyberLibrary;

namespace KyberModule
{
    /// <summary>
    /// Cmdlet pour signer des données avec une clé privée Dilithium
    /// </summary>
    [Cmdlet(VerbsLifecycle.Invoke, "DilithiumSign")]
    [OutputType(typeof(DilithiumSignatureResult))]
    public class InvokeDilithiumSignCommand : PSCmdlet
    {
        [Parameter(Mandatory = true, HelpMessage = "Les données à signer (byte[] ou chaîne hexadécimale)")]
        public object Data { get; set; }

        [Parameter(Mandatory = true, HelpMessage = "La clé privée Dilithium (byte[] ou chaîne hexadécimale)")]
        public object PrivateKey { get; set; }

        [Parameter(Mandatory = false, HelpMessage = "Paramètre de sécurité Dilithium (Dilithium2, Dilithium3, Dilithium5). Par défaut: Dilithium3")]
        [ValidateSet("Dilithium2", "Dilithium3", "Dilithium5")]
        public string ParameterSet { get; set; } = "Dilithium3";

        [Parameter(Mandatory = false, HelpMessage = "Pré-hacher les données avec SHA3 avant de signer (recommandé pour ML-DSA)")]
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
                byte[] privateKeyBytes = ConvertToByteArray(PrivateKey, "PrivateKey");

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

                // Signer les données
                byte[] signature;
                if (PreHash.IsPresent)
                {
                    // Utiliser SHA3 pour le pré-hachage
                    SHA3Wrapper.SHA3Variant sha3Variant = SHA3Variant.ToUpper() == "SHA3_384"
                        ? SHA3Wrapper.SHA3Variant.SHA3_384
                        : SHA3Wrapper.SHA3Variant.SHA3_256;
                    signature = dilithium.SignWithSHA3(dataBytes, privateKeyBytes, sha3Variant);
                }
                else
                {
                    // Signer sans pré-hachage
                    signature = dilithium.Sign(dataBytes, privateKeyBytes, false);
                }

                // Créer l'objet résultat
                var result = new DilithiumSignatureResult
                {
                    Signature = signature,
                    SignatureHex = DilithiumWrapper.BytesToHex(signature)
                };

                WriteObject(result);
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(ex, "DilithiumSignError", ErrorCategory.NotSpecified, null));
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

    /// <summary>
    /// Résultat de la signature Dilithium
    /// </summary>
    public class DilithiumSignatureResult
    {
        public byte[] Signature { get; set; }
        public string SignatureHex { get; set; }
    }
}

