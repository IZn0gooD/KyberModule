using System;
using System.Management.Automation;
using KyberLibrary;

namespace KyberModule
{
    /// <summary>
    /// Cmdlet pour générer une configuration TLS 1.3 post-quantique hybride
    /// </summary>
    [Cmdlet(VerbsCommon.New, "TLS13HybridConfig")]
    [OutputType(typeof(TLS13HybridWrapper.HybridTLSConfig))]
    public class NewTLS13HybridConfigCommand : PSCmdlet
    {
        [Parameter(
            Mandatory = false,
            HelpMessage = "Algorithme classique (ECDSA ou RSA)")]
        [ValidateSet("ECDSA_P256", "ECDSA_P384", "RSA_2048", "RSA_3072")]
        public TLS13HybridWrapper.ClassicalAlgorithm ClassicalAlgorithm { get; set; } = TLS13HybridWrapper.ClassicalAlgorithm.ECDSA_P256;

        [Parameter(
            Mandatory = false,
            HelpMessage = "Algorithme post-quantique pour l'échange de clés (Kyber)")]
        [ValidateSet("Kyber512", "Kyber768", "Kyber1024")]
        public TLS13HybridWrapper.PostQuantumAlgorithm PostQuantumKemAlgorithm { get; set; } = TLS13HybridWrapper.PostQuantumAlgorithm.Kyber768;

        [Parameter(
            Mandatory = false,
            HelpMessage = "Algorithme post-quantique pour les signatures (Dilithium)")]
        [ValidateSet("Dilithium2", "Dilithium3", "Dilithium5")]
        public TLS13HybridWrapper.PostQuantumAlgorithm PostQuantumSigAlgorithm { get; set; } = TLS13HybridWrapper.PostQuantumAlgorithm.Dilithium3;

        protected override void ProcessRecord()
        {
            try
            {
                WriteVerbose($"Génération d'une configuration TLS 1.3 hybride...");
                WriteVerbose($"  Algorithme classique: {ClassicalAlgorithm}");
                WriteVerbose($"  KEM post-quantique: {PostQuantumKemAlgorithm}");
                WriteVerbose($"  Signature post-quantique: {PostQuantumSigAlgorithm}");

                var config = TLS13HybridWrapper.GenerateHybridConfig(
                    ClassicalAlgorithm,
                    PostQuantumKemAlgorithm,
                    PostQuantumSigAlgorithm);

                string cipherSuite = TLS13HybridWrapper.GetCipherSuiteName(config);
                WriteVerbose($"  Cipher suite: {cipherSuite}");

                WriteObject(config);
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(ex, "TLS13HybridConfigGenerationError", ErrorCategory.NotSpecified, null));
            }
        }
    }
}

