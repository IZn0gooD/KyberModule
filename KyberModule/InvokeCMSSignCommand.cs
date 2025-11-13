using System;
using System.Management.Automation;
using KyberLibrary;
using Org.BouncyCastle.Crypto.Parameters;

namespace KyberModule
{
    /// <summary>
    /// Cmdlet pour créer un message CMS signé avec Dilithium
    /// </summary>
    [Cmdlet(VerbsLifecycle.Invoke, "CMSSign")]
    [OutputType(typeof(byte[]))]
    public class InvokeCMSSignCommand : PSCmdlet
    {
        [Parameter(
            Mandatory = true,
            Position = 0,
            ValueFromPipeline = true,
            HelpMessage = "Données à signer")]
        [ValidateNotNull]
        public byte[] Data { get; set; }

        [Parameter(
            Mandatory = true,
            HelpMessage = "Clé privée Dilithium")]
        [ValidateNotNull]
        public byte[] PrivateKey { get; set; }

        [Parameter(
            Mandatory = true,
            HelpMessage = "Clé publique Dilithium")]
        [ValidateNotNull]
        public byte[] PublicKey { get; set; }

        [Parameter(
            Mandatory = false,
            HelpMessage = "Paramètre Dilithium")]
        [ValidateSet("Dilithium2", "Dilithium3", "Dilithium5")]
        public string ParameterSet { get; set; } = "Dilithium3";

        [Parameter(
            Mandatory = false,
            HelpMessage = "Pré-hacher les données avec SHA3-256 (recommandé pour ML-DSA)")]
        public SwitchParameter PreHash { get; set; }

        protected override void ProcessRecord()
        {
            try
            {
                MLDsaParameters mlDsaParams = ParameterSet switch
                {
                    "Dilithium2" => MLDsaParameters.ml_dsa_44,
                    "Dilithium3" => MLDsaParameters.ml_dsa_65,
                    "Dilithium5" => MLDsaParameters.ml_dsa_87,
                    _ => MLDsaParameters.ml_dsa_65
                };

                byte[] cmsSigned = CMSWrapper.CreateSignedData(
                    Data,
                    PrivateKey,
                    PublicKey,
                    mlDsaParams,
                    PreHash.IsPresent);

                WriteVerbose($"Message CMS signé créé: {cmsSigned.Length} bytes");
                WriteObject(cmsSigned);
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(ex, "CMSSignError", ErrorCategory.NotSpecified, Data));
            }
        }
    }

    /// <summary>
    /// Cmdlet pour vérifier un message CMS signé
    /// </summary>
    [Cmdlet(VerbsLifecycle.Invoke, "CMSVerify")]
    [OutputType(typeof(bool))]
    public class InvokeCMSVerifyCommand : PSCmdlet
    {
        [Parameter(
            Mandatory = true,
            Position = 0,
            ValueFromPipeline = true,
            HelpMessage = "Message CMS signé")]
        [ValidateNotNull]
        public byte[] CmsData { get; set; }

        [Parameter(
            Mandatory = false,
            HelpMessage = "Clé publique pour vérification (optionnel, peut être extraite du message)")]
        public byte[] PublicKey { get; set; }

        [Parameter(
            Mandatory = false,
            HelpMessage = "Paramètre Dilithium (optionnel, peut être détecté)")]
        [ValidateSet("Dilithium2", "Dilithium3", "Dilithium5")]
        public string ParameterSet { get; set; }

        protected override void ProcessRecord()
        {
            try
            {
                MLDsaParameters? mlDsaParams = null;
                if (!string.IsNullOrEmpty(ParameterSet))
                {
                    mlDsaParams = ParameterSet switch
                    {
                        "Dilithium2" => MLDsaParameters.ml_dsa_44,
                        "Dilithium3" => MLDsaParameters.ml_dsa_65,
                        "Dilithium5" => MLDsaParameters.ml_dsa_87,
                        _ => (MLDsaParameters?)null
                    };
                }

                bool isValid = CMSWrapper.VerifySignedData(CmsData, PublicKey, mlDsaParams);

                if (isValid)
                {
                    WriteVerbose("Signature CMS valide");
                }
                else
                {
                    WriteWarning("Signature CMS invalide");
                }

                WriteObject(isValid);
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(ex, "CMSVerifyError", ErrorCategory.NotSpecified, CmsData));
            }
        }
    }
}




