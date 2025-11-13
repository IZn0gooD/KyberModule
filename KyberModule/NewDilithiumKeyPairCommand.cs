using System;
using System.Management.Automation;
using KyberLibrary;

namespace KyberModule
{
    /// <summary>
    /// Cmdlet pour générer une paire de clés Dilithium (ML-DSA)
    /// </summary>
    [Cmdlet(VerbsCommon.New, "DilithiumKeyPair")]
    [OutputType(typeof(DilithiumKeyPairResult))]
    public class NewDilithiumKeyPairCommand : PSCmdlet
    {
        [Parameter(Mandatory = false, HelpMessage = "Paramètre de sécurité Dilithium (Dilithium2, Dilithium3, Dilithium5). Par défaut: Dilithium3")]
        [ValidateSet("Dilithium2", "Dilithium3", "Dilithium5")]
        public string ParameterSet { get; set; } = "Dilithium3";

        protected override void ProcessRecord()
        {
            try
            {
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

                // Générer la paire de clés
                var (publicKey, privateKey) = dilithium.GenerateKeyPair();

                // Créer l'objet résultat
                var result = new DilithiumKeyPairResult
                {
                    PublicKey = publicKey,
                    PrivateKey = privateKey,
                    PublicKeyHex = DilithiumWrapper.BytesToHex(publicKey),
                    PrivateKeyHex = DilithiumWrapper.BytesToHex(privateKey),
                    ParameterSet = ParameterSet
                };

                WriteObject(result);
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(ex, "DilithiumKeyGenerationError", ErrorCategory.NotSpecified, null));
            }
        }
    }

    /// <summary>
    /// Résultat de la génération de clés Dilithium
    /// </summary>
    public class DilithiumKeyPairResult
    {
        public byte[] PublicKey { get; set; }
        public byte[] PrivateKey { get; set; }
        public string PublicKeyHex { get; set; }
        public string PrivateKeyHex { get; set; }
        public string ParameterSet { get; set; }
    }
}

