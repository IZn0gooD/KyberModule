using System;
using System.Collections.Generic;
using System.Management.Automation;
using KyberLibrary;

namespace KyberModule
{
    /// <summary>
    /// Cmdlet pour exécuter des tests de fuzzing cryptographique
    /// </summary>
    [Cmdlet(VerbsLifecycle.Invoke, "CryptographicFuzzing")]
    [OutputType(typeof(FuzzingTestResult))]
    public class InvokeCryptographicFuzzingCommand : PSCmdlet
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
            HelpMessage = "Nombre d'itérations de fuzzing par test")]
        public int Iterations { get; set; } = 100;

        [Parameter(
            Mandatory = false,
            HelpMessage = "Afficher uniquement les tests avec crashes ou échecs")]
        public SwitchParameter ShowOnlyIssues { get; set; }

        protected override void ProcessRecord()
        {
            try
            {
                var results = new List<FuzzingTestResult>();

                if (Algorithm == "All" || Algorithm == "Kyber")
                {
                    results.AddRange(RunKyberFuzzingTests());
                }

                if (Algorithm == "All" || Algorithm == "Dilithium")
                {
                    results.AddRange(RunDilithiumFuzzingTests());
                }

                if (Algorithm == "All" || Algorithm == "Ed25519")
                {
                    results.AddRange(RunEd25519FuzzingTests());
                }

                foreach (var result in results)
                {
                    if (!ShowOnlyIssues || result.Crashed > 0 || result.Failed > 0)
                    {
                        WriteObject(result);
                    }
                }
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(ex, "FuzzingTestError", ErrorCategory.NotSpecified, null));
            }
        }

        private List<FuzzingTestResult> RunKyberFuzzingTests()
        {
            var results = new List<FuzzingTestResult>();

            KyberWrapper.KyberParameterSet paramSet = KyberParameterSet switch
            {
                "Kyber512" => KyberWrapper.KyberParameterSet.Kyber512,
                "Kyber768" => KyberWrapper.KyberParameterSet.Kyber768,
                "Kyber1024" => KyberWrapper.KyberParameterSet.Kyber1024,
                _ => KyberWrapper.KyberParameterSet.Kyber768
            };

            var kyber = new KyberWrapper(paramSet);
            var (publicKey, privateKey) = kyber.GenerateKeyPair();
            var encapsulated = kyber.Encapsulate(publicKey);

            // Test 1: Fuzzing de la décapsulation avec ciphertexts corrompus
            var corruptedCiphertexts = CryptographicFuzzing.GenerateCorruptedCiphertexts(encapsulated.Ciphertext, Iterations);
            var fuzzingResult1 = CryptographicFuzzing.TestRobustness(
                (ciphertext) =>
                {
                    try
                    {
                        kyber.Decapsulate(ciphertext, privateKey);
                        return false; // Devrait échouer avec des données corrompues
                    }
                    catch
                    {
                        return true; // Comportement attendu : rejet des données invalides
                    }
                },
                corruptedCiphertexts,
                $"Fuzzing décapsulation Kyber {paramSet} (ciphertexts corrompus)"
            );
            results.Add(fuzzingResult1);

            // Test 2: Fuzzing avec clés privées corrompues
            var corruptedPrivateKeys = CryptographicFuzzing.GenerateCorruptedKeys(privateKey, Iterations);
            var fuzzingResult2 = CryptographicFuzzing.TestRobustness(
                (corruptedKey) =>
                {
                    try
                    {
                        kyber.Decapsulate(encapsulated.Ciphertext, corruptedKey);
                        return false;
                    }
                    catch
                    {
                        return true; // Comportement attendu
                    }
                },
                corruptedPrivateKeys,
                $"Fuzzing décapsulation Kyber {paramSet} (clés privées corrompues)"
            );
            results.Add(fuzzingResult2);

            // Test 3: Fuzzing avec clés publiques corrompues pour encapsulation
            var corruptedPublicKeys = CryptographicFuzzing.GenerateCorruptedKeys(publicKey, Iterations);
            var fuzzingResult3 = CryptographicFuzzing.TestRobustness(
                (corruptedKey) =>
                {
                    try
                    {
                        kyber.Encapsulate(corruptedKey);
                        return false;
                    }
                    catch
                    {
                        return true; // Comportement attendu
                    }
                },
                corruptedPublicKeys,
                $"Fuzzing encapsulation Kyber {paramSet} (clés publiques corrompues)"
            );
            results.Add(fuzzingResult3);

            return results;
        }

        private List<FuzzingTestResult> RunDilithiumFuzzingTests()
        {
            var results = new List<FuzzingTestResult>();

            DilithiumWrapper.DilithiumParameterSet paramSet = DilithiumParameterSet switch
            {
                "Dilithium2" => DilithiumWrapper.DilithiumParameterSet.Dilithium2,
                "Dilithium3" => DilithiumWrapper.DilithiumParameterSet.Dilithium3,
                "Dilithium5" => DilithiumWrapper.DilithiumParameterSet.Dilithium5,
                _ => DilithiumWrapper.DilithiumParameterSet.Dilithium3
            };

            var dilithium = new DilithiumWrapper(paramSet);
            var (publicKey, privateKey) = dilithium.GenerateKeyPair();
            byte[] testData = System.Text.Encoding.UTF8.GetBytes("Test data for fuzzing");
            byte[] signature = dilithium.Sign(testData, privateKey);

            // Test 1: Fuzzing de la vérification avec signatures corrompues
            var corruptedSignatures = CryptographicFuzzing.GenerateCorruptedSignatures(signature, Iterations);
            var fuzzingResult1 = CryptographicFuzzing.TestRobustness(
                (corruptedSig) =>
                {
                    try
                    {
                        bool isValid = dilithium.Verify(testData, corruptedSig, publicKey);
                        return !isValid; // Devrait rejeter les signatures corrompues
                    }
                    catch
                    {
                        return true; // Comportement acceptable
                    }
                },
                corruptedSignatures,
                $"Fuzzing vérification Dilithium {paramSet} (signatures corrompues)"
            );
            results.Add(fuzzingResult1);

            // Test 2: Fuzzing avec données corrompues
            var corruptedData = CryptographicFuzzing.GenerateCorruptedKeys(testData, Iterations);
            var fuzzingResult2 = CryptographicFuzzing.TestRobustness(
                (corruptedDataBytes) =>
                {
                    try
                    {
                        bool isValid = dilithium.Verify(corruptedDataBytes, signature, publicKey);
                        return !isValid; // Devrait rejeter les données corrompues
                    }
                    catch
                    {
                        return true;
                    }
                },
                corruptedData,
                $"Fuzzing vérification Dilithium {paramSet} (données corrompues)"
            );
            results.Add(fuzzingResult2);

            // Test 3: Fuzzing avec clés publiques corrompues
            var corruptedPublicKeys = CryptographicFuzzing.GenerateCorruptedKeys(publicKey, Iterations);
            var fuzzingResult3 = CryptographicFuzzing.TestRobustness(
                (corruptedKey) =>
                {
                    try
                    {
                        dilithium.Verify(testData, signature, corruptedKey);
                        return false;
                    }
                    catch
                    {
                        return true;
                    }
                },
                corruptedPublicKeys,
                $"Fuzzing vérification Dilithium {paramSet} (clés publiques corrompues)"
            );
            results.Add(fuzzingResult3);

            return results;
        }

        private List<FuzzingTestResult> RunEd25519FuzzingTests()
        {
            var results = new List<FuzzingTestResult>();

            var ed25519 = new Ed25519Wrapper();
            var (publicKey, privateKey) = ed25519.GenerateKeyPair();
            byte[] testData = System.Text.Encoding.UTF8.GetBytes("Test data for fuzzing");
            byte[] signature = ed25519.Sign(testData, privateKey);

            // Test 1: Fuzzing de la vérification avec signatures corrompues
            var corruptedSignatures = CryptographicFuzzing.GenerateCorruptedSignatures(signature, Iterations);
            var fuzzingResult1 = CryptographicFuzzing.TestRobustness(
                (corruptedSig) =>
                {
                    try
                    {
                        bool isValid = ed25519.Verify(testData, corruptedSig, publicKey);
                        return !isValid;
                    }
                    catch
                    {
                        return true;
                    }
                },
                corruptedSignatures,
                "Fuzzing vérification Ed25519 (signatures corrompues)"
            );
            results.Add(fuzzingResult1);

            // Test 2: Fuzzing avec données corrompues
            var corruptedData = CryptographicFuzzing.GenerateCorruptedKeys(testData, Iterations);
            var fuzzingResult2 = CryptographicFuzzing.TestRobustness(
                (corruptedDataBytes) =>
                {
                    try
                    {
                        bool isValid = ed25519.Verify(corruptedDataBytes, signature, publicKey);
                        return !isValid;
                    }
                    catch
                    {
                        return true;
                    }
                },
                corruptedData,
                "Fuzzing vérification Ed25519 (données corrompues)"
            );
            results.Add(fuzzingResult2);

            return results;
        }
    }
}

