using System;
using System.Management.Automation;
using KyberLibrary;

namespace KyberModule
{
    /// <summary>
    /// Cmdlet pour tester la conformité cryptographique selon les standards NIST
    /// </summary>
    [Cmdlet(VerbsDiagnostic.Test, "CryptographicConformance")]
    [OutputType(typeof(ConformanceTestResult))]
    public class TestCryptographicConformanceCommand : PSCmdlet
    {
        [Parameter(
            Mandatory = false,
            HelpMessage = "Algorithme à tester (Kyber, Dilithium, Ed25519, All)")]
        [ValidateSet("Kyber", "Dilithium", "Ed25519", "All")]
        public string Algorithm { get; set; } = "All";

        [Parameter(
            Mandatory = false,
            HelpMessage = "Paramètre de sécurité pour Kyber (Kyber512, Kyber768, Kyber1024)")]
        [ValidateSet("Kyber512", "Kyber768", "Kyber1024")]
        public string KyberParameterSet { get; set; } = "Kyber768";

        [Parameter(
            Mandatory = false,
            HelpMessage = "Paramètre de sécurité pour Dilithium (Dilithium2, Dilithium3, Dilithium5)")]
        [ValidateSet("Dilithium2", "Dilithium3", "Dilithium5")]
        public string DilithiumParameterSet { get; set; } = "Dilithium3";

        [Parameter(
            Mandatory = false,
            HelpMessage = "Afficher uniquement les tests échoués")]
        public SwitchParameter ShowOnlyFailed { get; set; }

        protected override void ProcessRecord()
        {
            try
            {
                var results = new System.Collections.Generic.List<ConformanceTestResult>();

                if (Algorithm == "All" || Algorithm == "Kyber")
                {
                    results.AddRange(RunKyberConformanceTests());
                }

                if (Algorithm == "All" || Algorithm == "Dilithium")
                {
                    results.AddRange(RunDilithiumConformanceTests());
                }

                if (Algorithm == "All" || Algorithm == "Ed25519")
                {
                    results.AddRange(RunEd25519ConformanceTests());
                }

                foreach (var result in results)
                {
                    if (!ShowOnlyFailed || !result.Passed)
                    {
                        WriteObject(result);
                    }
                }
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(ex, "ConformanceTestError", ErrorCategory.NotSpecified, null));
            }
        }

        private System.Collections.Generic.List<ConformanceTestResult> RunKyberConformanceTests()
        {
            var results = new System.Collections.Generic.List<ConformanceTestResult>();

            KyberWrapper.KyberParameterSet paramSet = KyberParameterSet switch
            {
                "Kyber512" => KyberWrapper.KyberParameterSet.Kyber512,
                "Kyber768" => KyberWrapper.KyberParameterSet.Kyber768,
                "Kyber1024" => KyberWrapper.KyberParameterSet.Kyber1024,
                _ => KyberWrapper.KyberParameterSet.Kyber768
            };

            // Test 1: Génération de clés et validation des tailles
            var kyber = new KyberWrapper(paramSet);
            var (publicKey, privateKey) = kyber.GenerateKeyPair();
            results.Add(CryptographicConformanceTests.ValidateKyberKeySizes(paramSet, publicKey, privateKey));

            // Test 2: Encapsulation et validation des tailles
            var encapsulated = kyber.Encapsulate(publicKey);
            results.Add(CryptographicConformanceTests.ValidateKyberEncapsulationSizes(paramSet, encapsulated.Ciphertext, encapsulated.SharedSecret));

            // Test 3: Décapsulation et correspondance des clés partagées
            var decapsulated = kyber.Decapsulate(encapsulated.Ciphertext, privateKey);
            results.Add(CryptographicConformanceTests.ValidateSharedSecretMatch(encapsulated.SharedSecret, decapsulated));

            // Test 4: Non-déterminisme (génération de deux paires de clés)
            var (publicKey2, privateKey2) = kyber.GenerateKeyPair();
            results.Add(CryptographicConformanceTests.ValidateNonDeterminism(publicKey, publicKey2, "clé publique Kyber"));
            results.Add(CryptographicConformanceTests.ValidateNonDeterminism(privateKey, privateKey2, "clé privée Kyber"));

            // Test 5: Correspondance paire de clés
            var encapsulated2 = kyber.Encapsulate(publicKey);
            results.Add(CryptographicConformanceTests.ValidateKeyPairCorrespondence(
                paramSet, publicKey, privateKey, encapsulated2.Ciphertext, encapsulated2.SharedSecret));

            return results;
        }

        private System.Collections.Generic.List<ConformanceTestResult> RunDilithiumConformanceTests()
        {
            var results = new System.Collections.Generic.List<ConformanceTestResult>();

            DilithiumWrapper.DilithiumParameterSet paramSet = DilithiumParameterSet switch
            {
                "Dilithium2" => DilithiumWrapper.DilithiumParameterSet.Dilithium2,
                "Dilithium3" => DilithiumWrapper.DilithiumParameterSet.Dilithium3,
                "Dilithium5" => DilithiumWrapper.DilithiumParameterSet.Dilithium5,
                _ => DilithiumWrapper.DilithiumParameterSet.Dilithium3
            };

            // Test 1: Génération de clés et validation des tailles
            var dilithium = new DilithiumWrapper(paramSet);
            var (publicKey, privateKey) = dilithium.GenerateKeyPair();
            results.Add(CryptographicConformanceTests.ValidateDilithiumKeySizes(paramSet, publicKey, privateKey));

            // Test 2: Signature et validation de la taille
            byte[] testData = System.Text.Encoding.UTF8.GetBytes("Test data for Dilithium");
            byte[] signature = dilithium.Sign(testData, privateKey);
            results.Add(CryptographicConformanceTests.ValidateDilithiumSignatureSize(paramSet, signature));

            // Test 3: Vérification de signature
            results.Add(CryptographicConformanceTests.ValidateDilithiumSignatureCorrespondence(
                paramSet, publicKey, privateKey, testData, signature));

            // Test 4: Non-déterminisme
            var (publicKey2, privateKey2) = dilithium.GenerateKeyPair();
            results.Add(CryptographicConformanceTests.ValidateNonDeterminism(publicKey, publicKey2, "clé publique Dilithium"));
            results.Add(CryptographicConformanceTests.ValidateNonDeterminism(privateKey, privateKey2, "clé privée Dilithium"));

            return results;
        }

        private System.Collections.Generic.List<ConformanceTestResult> RunEd25519ConformanceTests()
        {
            var results = new System.Collections.Generic.List<ConformanceTestResult>();

            // Test 1: Génération de clés et validation des tailles
            var ed25519 = new Ed25519Wrapper();
            var (publicKey, privateKey) = ed25519.GenerateKeyPair();
            results.Add(CryptographicConformanceTests.ValidateEd25519KeySizes(publicKey, privateKey));

            // Test 2: Signature et validation de la taille
            byte[] testData = System.Text.Encoding.UTF8.GetBytes("Test data for Ed25519");
            byte[] signature = ed25519.Sign(testData, privateKey);
            results.Add(CryptographicConformanceTests.ValidateEd25519SignatureSize(signature));

            // Test 3: Vérification de signature
            results.Add(CryptographicConformanceTests.ValidateEd25519SignatureCorrespondence(
                publicKey, privateKey, testData, signature));

            // Test 4: Non-déterminisme
            var (publicKey2, privateKey2) = ed25519.GenerateKeyPair();
            results.Add(CryptographicConformanceTests.ValidateNonDeterminism(publicKey, publicKey2, "clé publique Ed25519"));
            results.Add(CryptographicConformanceTests.ValidateNonDeterminism(privateKey, privateKey2, "clé privée Ed25519"));

            return results;
        }
    }
}

