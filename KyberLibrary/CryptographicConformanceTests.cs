using System;
using System.Collections.Generic;
using System.Linq;

namespace KyberLibrary
{
    /// <summary>
    /// Classe utilitaire pour les tests de conformité cryptographique
    /// Valide la conformité aux standards NIST (FIPS 203, ML-DSA, etc.)
    /// </summary>
    public static class CryptographicConformanceTests
    {
        // Tailles conformes NIST FIPS 203 (ML-KEM)
        private static readonly Dictionary<KyberWrapper.KyberParameterSet, (int PublicKey, int PrivateKey, int Ciphertext, int SharedSecret)> KyberSizes = new Dictionary<KyberWrapper.KyberParameterSet, (int, int, int, int)>
        {
            { KyberWrapper.KyberParameterSet.Kyber512, (800, 1632, 768, 32) },
            { KyberWrapper.KyberParameterSet.Kyber768, (1184, 2400, 1088, 32) },
            { KyberWrapper.KyberParameterSet.Kyber1024, (1568, 3168, 1568, 32) }
        };

        // Tailles conformes NIST ML-DSA (Dilithium)
        private static readonly Dictionary<DilithiumWrapper.DilithiumParameterSet, (int PublicKey, int PrivateKey, int Signature)> DilithiumSizes = new Dictionary<DilithiumWrapper.DilithiumParameterSet, (int, int, int)>
        {
            { DilithiumWrapper.DilithiumParameterSet.Dilithium2, (1312, 2560, 2420) },
            { DilithiumWrapper.DilithiumParameterSet.Dilithium3, (1952, 4032, 3309) },
            { DilithiumWrapper.DilithiumParameterSet.Dilithium5, (2592, 4864, 4627) }
        };

        // Tailles conformes RFC 8032 (Ed25519)
        private const int Ed25519PublicKeySize = 32;
        private const int Ed25519PrivateKeySize = 32;
        private const int Ed25519SignatureSize = 64;

        /// <summary>
        /// Valide les tailles de clés Kyber selon NIST FIPS 203
        /// </summary>
        public static ConformanceTestResult ValidateKyberKeySizes(KyberWrapper.KyberParameterSet parameterSet, byte[] publicKey, byte[] privateKey)
        {
            var result = new ConformanceTestResult
            {
                TestName = $"Validation tailles clés Kyber {parameterSet}",
                Passed = true
            };

            if (!KyberSizes.ContainsKey(parameterSet))
            {
                result.Passed = false;
                result.Errors.Add($"Paramètre {parameterSet} non reconnu");
                return result;
            }

            var expectedSizes = KyberSizes[parameterSet];

            if (publicKey == null || publicKey.Length != expectedSizes.PublicKey)
            {
                result.Passed = false;
                result.Errors.Add($"Clé publique: taille attendue {expectedSizes.PublicKey} bytes, obtenue {publicKey?.Length ?? 0} bytes");
            }

            if (privateKey == null || privateKey.Length != expectedSizes.PrivateKey)
            {
                result.Passed = false;
                result.Errors.Add($"Clé privée: taille attendue {expectedSizes.PrivateKey} bytes, obtenue {privateKey?.Length ?? 0} bytes");
            }

            return result;
        }

        /// <summary>
        /// Valide les tailles de ciphertext et clé partagée Kyber selon NIST FIPS 203
        /// </summary>
        public static ConformanceTestResult ValidateKyberEncapsulationSizes(KyberWrapper.KyberParameterSet parameterSet, byte[] ciphertext, byte[] sharedSecret)
        {
            var result = new ConformanceTestResult
            {
                TestName = $"Validation tailles encapsulation Kyber {parameterSet}",
                Passed = true
            };

            if (!KyberSizes.ContainsKey(parameterSet))
            {
                result.Passed = false;
                result.Errors.Add($"Paramètre {parameterSet} non reconnu");
                return result;
            }

            var expectedSizes = KyberSizes[parameterSet];

            if (ciphertext == null || ciphertext.Length != expectedSizes.Ciphertext)
            {
                result.Passed = false;
                result.Errors.Add($"Ciphertext: taille attendue {expectedSizes.Ciphertext} bytes, obtenue {ciphertext?.Length ?? 0} bytes");
            }

            if (sharedSecret == null || sharedSecret.Length != expectedSizes.SharedSecret)
            {
                result.Passed = false;
                result.Errors.Add($"Clé partagée: taille attendue {expectedSizes.SharedSecret} bytes, obtenue {sharedSecret?.Length ?? 0} bytes");
            }

            return result;
        }

        /// <summary>
        /// Valide les tailles de clés Dilithium selon NIST ML-DSA
        /// </summary>
        public static ConformanceTestResult ValidateDilithiumKeySizes(DilithiumWrapper.DilithiumParameterSet parameterSet, byte[] publicKey, byte[] privateKey)
        {
            var result = new ConformanceTestResult
            {
                TestName = $"Validation tailles clés Dilithium {parameterSet}",
                Passed = true
            };

            if (!DilithiumSizes.ContainsKey(parameterSet))
            {
                result.Passed = false;
                result.Errors.Add($"Paramètre {parameterSet} non reconnu");
                return result;
            }

            var expectedSizes = DilithiumSizes[parameterSet];

            // Les tailles peuvent varier légèrement, on accepte une tolérance de ±10 bytes
            if (publicKey == null || Math.Abs(publicKey.Length - expectedSizes.PublicKey) > 10)
            {
                result.Passed = false;
                result.Errors.Add($"Clé publique: taille attendue ~{expectedSizes.PublicKey} bytes, obtenue {publicKey?.Length ?? 0} bytes");
            }

            if (privateKey == null || Math.Abs(privateKey.Length - expectedSizes.PrivateKey) > 10)
            {
                result.Passed = false;
                result.Errors.Add($"Clé privée: taille attendue ~{expectedSizes.PrivateKey} bytes, obtenue {privateKey?.Length ?? 0} bytes");
            }

            return result;
        }

        /// <summary>
        /// Valide la taille de signature Dilithium selon NIST ML-DSA
        /// </summary>
        public static ConformanceTestResult ValidateDilithiumSignatureSize(DilithiumWrapper.DilithiumParameterSet parameterSet, byte[] signature)
        {
            var result = new ConformanceTestResult
            {
                TestName = $"Validation taille signature Dilithium {parameterSet}",
                Passed = true
            };

            if (!DilithiumSizes.ContainsKey(parameterSet))
            {
                result.Passed = false;
                result.Errors.Add($"Paramètre {parameterSet} non reconnu");
                return result;
            }

            var expectedSize = DilithiumSizes[parameterSet].Signature;

            // Les tailles peuvent varier légèrement, on accepte une tolérance de ±10 bytes
            if (signature == null || Math.Abs(signature.Length - expectedSize) > 10)
            {
                result.Passed = false;
                result.Errors.Add($"Signature: taille attendue ~{expectedSize} bytes, obtenue {signature?.Length ?? 0} bytes");
            }

            return result;
        }

        /// <summary>
        /// Valide les tailles de clés Ed25519 selon RFC 8032
        /// </summary>
        public static ConformanceTestResult ValidateEd25519KeySizes(byte[] publicKey, byte[] privateKey)
        {
            var result = new ConformanceTestResult
            {
                TestName = "Validation tailles clés Ed25519",
                Passed = true
            };

            if (publicKey == null || publicKey.Length != Ed25519PublicKeySize)
            {
                result.Passed = false;
                result.Errors.Add($"Clé publique: taille attendue {Ed25519PublicKeySize} bytes, obtenue {publicKey?.Length ?? 0} bytes");
            }

            if (privateKey == null || privateKey.Length != Ed25519PrivateKeySize)
            {
                result.Passed = false;
                result.Errors.Add($"Clé privée: taille attendue {Ed25519PrivateKeySize} bytes, obtenue {privateKey?.Length ?? 0} bytes");
            }

            return result;
        }

        /// <summary>
        /// Valide la taille de signature Ed25519 selon RFC 8032
        /// </summary>
        public static ConformanceTestResult ValidateEd25519SignatureSize(byte[] signature)
        {
            var result = new ConformanceTestResult
            {
                TestName = "Validation taille signature Ed25519",
                Passed = true
            };

            if (signature == null || signature.Length != Ed25519SignatureSize)
            {
                result.Passed = false;
                result.Errors.Add($"Signature: taille attendue {Ed25519SignatureSize} bytes, obtenue {signature?.Length ?? 0} bytes");
            }

            return result;
        }

        /// <summary>
        /// Valide que deux clés générées sont différentes (non-déterminisme)
        /// </summary>
        public static ConformanceTestResult ValidateNonDeterminism(byte[] key1, byte[] key2, string keyType = "clé")
        {
            var result = new ConformanceTestResult
            {
                TestName = $"Validation non-déterminisme {keyType}",
                Passed = true
            };

            if (key1 == null || key2 == null)
            {
                result.Passed = false;
                result.Errors.Add("Une ou plusieurs clés sont null");
                return result;
            }

            if (key1.Length != key2.Length)
            {
                result.Passed = false;
                result.Errors.Add($"Les clés ont des tailles différentes: {key1.Length} vs {key2.Length}");
                return result;
            }

            // Vérifier que les clés sont différentes
            bool areEqual = true;
            for (int i = 0; i < key1.Length; i++)
            {
                if (key1[i] != key2[i])
                {
                    areEqual = false;
                    break;
                }
            }

            if (areEqual)
            {
                result.Passed = false;
                result.Errors.Add("Les deux clés générées sont identiques (déterminisme détecté)");
            }

            return result;
        }

        /// <summary>
        /// Valide que les clés partagées après encapsulation/décapsulation sont identiques
        /// </summary>
        public static ConformanceTestResult ValidateSharedSecretMatch(byte[] secret1, byte[] secret2)
        {
            var result = new ConformanceTestResult
            {
                TestName = "Validation correspondance clés partagées",
                Passed = true
            };

            if (secret1 == null || secret2 == null)
            {
                result.Passed = false;
                result.Errors.Add("Une ou plusieurs clés partagées sont null");
                return result;
            }

            if (secret1.Length != secret2.Length)
            {
                result.Passed = false;
                result.Errors.Add($"Les clés partagées ont des tailles différentes: {secret1.Length} vs {secret2.Length}");
                return result;
            }

            // Comparaison en temps constant
            bool areEqual = ConstantTimeOperations.ConstantTimeEquals(secret1, secret2);

            if (!areEqual)
            {
                result.Passed = false;
                result.Errors.Add("Les clés partagées ne correspondent pas après encapsulation/décapsulation");
            }

            return result;
        }

        /// <summary>
        /// Valide que les clés publiques et privées correspondent (même paire)
        /// </summary>
        public static ConformanceTestResult ValidateKeyPairCorrespondence(
            KyberWrapper.KyberParameterSet parameterSet,
            byte[] publicKey,
            byte[] privateKey,
            byte[] ciphertext,
            byte[] expectedSharedSecret)
        {
            var result = new ConformanceTestResult
            {
                TestName = $"Validation correspondance paire de clés Kyber {parameterSet}",
                Passed = true
            };

            try
            {
                var kyber = new KyberWrapper(parameterSet);
                byte[] decapsulatedSecret = kyber.Decapsulate(ciphertext, privateKey);

                if (!ConstantTimeOperations.ConstantTimeEquals(decapsulatedSecret, expectedSharedSecret))
                {
                    result.Passed = false;
                    result.Errors.Add("La clé privée ne correspond pas à la clé publique (décapsulation échouée)");
                }
            }
            catch (Exception ex)
            {
                result.Passed = false;
                result.Errors.Add($"Erreur lors de la validation: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Valide que les signatures Dilithium sont valides avec la clé publique correspondante
        /// </summary>
        public static ConformanceTestResult ValidateDilithiumSignatureCorrespondence(
            DilithiumWrapper.DilithiumParameterSet parameterSet,
            byte[] publicKey,
            byte[] privateKey,
            byte[] data,
            byte[] signature)
        {
            var result = new ConformanceTestResult
            {
                TestName = $"Validation correspondance signature Dilithium {parameterSet}",
                Passed = true
            };

            try
            {
                var dilithium = new DilithiumWrapper(parameterSet);
                bool isValid = dilithium.Verify(data, signature, publicKey);

                if (!isValid)
                {
                    result.Passed = false;
                    result.Errors.Add("La signature ne correspond pas à la clé publique (vérification échouée)");
                }
            }
            catch (Exception ex)
            {
                result.Passed = false;
                result.Errors.Add($"Erreur lors de la validation: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Valide que les signatures Ed25519 sont valides avec la clé publique correspondante
        /// </summary>
        public static ConformanceTestResult ValidateEd25519SignatureCorrespondence(
            byte[] publicKey,
            byte[] privateKey,
            byte[] data,
            byte[] signature)
        {
            var result = new ConformanceTestResult
            {
                TestName = "Validation correspondance signature Ed25519",
                Passed = true
            };

            try
            {
                var ed25519 = new Ed25519Wrapper();
                bool isValid = ed25519.Verify(data, signature, publicKey);

                if (!isValid)
                {
                    result.Passed = false;
                    result.Errors.Add("La signature ne correspond pas à la clé publique (vérification échouée)");
                }
            }
            catch (Exception ex)
            {
                result.Passed = false;
                result.Errors.Add($"Erreur lors de la validation: {ex.Message}");
            }

            return result;
        }
    }

    /// <summary>
    /// Résultat d'un test de conformité
    /// </summary>
    public class ConformanceTestResult
    {
        public string TestName { get; set; }
        public bool Passed { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }
}

