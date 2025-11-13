using System;
using System.Security.Cryptography;
using System.Text;
using System.IO;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Security;

namespace KyberLibrary
{
    /// <summary>
    /// Classe wrapper pour le support TLS 1.3 post-quantique hybride
    /// Combine des algorithmes classiques (ECDSA, RSA) avec des algorithmes post-quantiques (Kyber, Dilithium)
    /// </summary>
    public class TLS13HybridWrapper
    {
        /// <summary>
        /// Types d'algorithmes classiques pour l'hybridation
        /// </summary>
        public enum ClassicalAlgorithm
        {
            ECDSA_P256,    // ECDSA avec courbe P-256 (NIST)
            ECDSA_P384,    // ECDSA avec courbe P-384 (NIST)
            RSA_2048,      // RSA 2048 bits
            RSA_3072       // RSA 3072 bits
        }

        /// <summary>
        /// Types d'algorithmes post-quantiques pour l'hybridation
        /// </summary>
        public enum PostQuantumAlgorithm
        {
            Kyber512,      // ML-KEM-512
            Kyber768,      // ML-KEM-768
            Kyber1024,     // ML-KEM-1024
            Dilithium2,    // ML-DSA-44
            Dilithium3,    // ML-DSA-65
            Dilithium5     // ML-DSA-87
        }

        /// <summary>
        /// Configuration TLS 1.3 hybride
        /// </summary>
        public class HybridTLSConfig
        {
            public ClassicalAlgorithm ClassicalAlgo { get; set; }
            public PostQuantumAlgorithm PostQuantumKemAlgo { get; set; }  // Pour l'échange de clés (Kyber)
            public PostQuantumAlgorithm PostQuantumSigAlgo { get; set; } // Pour les signatures (Dilithium)

            // Clés classiques
            public byte[] ClassicalPrivateKey { get; set; }
            public byte[] ClassicalPublicKey { get; set; }

            // Clés post-quantiques (KEM)
            public byte[] PostQuantumKemPrivateKey { get; set; }
            public byte[] PostQuantumKemPublicKey { get; set; }

            // Clés post-quantiques (Signature)
            public byte[] PostQuantumSigPrivateKey { get; set; }
            public byte[] PostQuantumSigPublicKey { get; set; }


            /// <summary>
            /// Nettoie les clés privées en mémoire (zeroization)
            /// </summary>
            public void ZeroizePrivateKeys()
            {
                if (ClassicalPrivateKey != null)
                {
                    Array.Clear(ClassicalPrivateKey, 0, ClassicalPrivateKey.Length);
                }
                if (PostQuantumKemPrivateKey != null)
                {
                    Array.Clear(PostQuantumKemPrivateKey, 0, PostQuantumKemPrivateKey.Length);
                }
                if (PostQuantumSigPrivateKey != null)
                {
                    Array.Clear(PostQuantumSigPrivateKey, 0, PostQuantumSigPrivateKey.Length);
                }
            }
        }


        /// <summary>
        /// Génère une configuration TLS 1.3 hybride complète
        /// </summary>
        /// <param name="classicalAlgo">Algorithme classique (ECDSA ou RSA)</param>
        /// <param name="pqKemAlgo">Algorithme post-quantique pour l'échange de clés (Kyber)</param>
        /// <param name="pqSigAlgo">Algorithme post-quantique pour les signatures (Dilithium)</param>
        /// <returns>Configuration TLS hybride</returns>
        public static HybridTLSConfig GenerateHybridConfig(
            ClassicalAlgorithm classicalAlgo = ClassicalAlgorithm.ECDSA_P256,
            PostQuantumAlgorithm pqKemAlgo = PostQuantumAlgorithm.Kyber768,
            PostQuantumAlgorithm pqSigAlgo = PostQuantumAlgorithm.Dilithium3)
        {
            var config = new HybridTLSConfig
            {
                ClassicalAlgo = classicalAlgo,
                PostQuantumKemAlgo = pqKemAlgo,
                PostQuantumSigAlgo = pqSigAlgo
            };

            // Générer les clés classiques
            byte[] classicalPrivate, classicalPublic;
            GenerateClassicalKeys(classicalAlgo, out classicalPrivate, out classicalPublic);
            config.ClassicalPrivateKey = classicalPrivate;
            config.ClassicalPublicKey = classicalPublic;

            // Générer les clés post-quantiques KEM (Kyber)
            var kyberWrapper = CreateKyberWrapper(pqKemAlgo);
            var kyberKeys = kyberWrapper.GenerateKeyPair();
            config.PostQuantumKemPrivateKey = kyberKeys.PrivateKey;
            config.PostQuantumKemPublicKey = kyberKeys.PublicKey;

            // Générer les clés post-quantiques Signature (Dilithium)
            var dilithiumWrapper = CreateDilithiumWrapper(pqSigAlgo);
            var dilithiumKeys = dilithiumWrapper.GenerateKeyPair();
            config.PostQuantumSigPrivateKey = dilithiumKeys.PrivateKey;
            config.PostQuantumSigPublicKey = dilithiumKeys.PublicKey;

            return config;
        }

        /// <summary>
        /// Génère des clés classiques (ECDSA ou RSA)
        /// </summary>
        private static void GenerateClassicalKeys(ClassicalAlgorithm algo, out byte[] privateKey, out byte[] publicKey)
        {
            try
            {
                byte[] privKey, pubKey;
                switch (algo)
                {
                    case ClassicalAlgorithm.ECDSA_P256:
                        GenerateECDSAKeys("P-256", out privKey, out pubKey);
                        break;
                    case ClassicalAlgorithm.ECDSA_P384:
                        GenerateECDSAKeys("P-384", out privKey, out pubKey);
                        break;
                    case ClassicalAlgorithm.RSA_2048:
                        GenerateRSAKeys(2048, out privKey, out pubKey);
                        break;
                    case ClassicalAlgorithm.RSA_3072:
                        GenerateRSAKeys(3072, out privKey, out pubKey);
                        break;
                    default:
                        throw new ArgumentException($"Algorithme classique non supporté: {algo}");
                }
                privateKey = privKey;
                publicKey = pubKey;
            }
            catch (Exception ex)
            {
                throw new CryptographicException($"Erreur lors de la génération des clés classiques: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Génère des clés ECDSA avec BouncyCastle
        /// </summary>
        private static void GenerateECDSAKeys(string curveName, out byte[] privateKey, out byte[] publicKey)
        {
            Org.BouncyCastle.Asn1.X9.X9ECParameters ecParams;
            if (curveName == "P-256")
            {
                ecParams = Org.BouncyCastle.Crypto.EC.CustomNamedCurves.GetByName("P-256");
                if (ecParams == null)
                {
                    ecParams = Org.BouncyCastle.Asn1.X9.ECNamedCurveTable.GetByName("P-256");
                }
            }
            else if (curveName == "P-384")
            {
                ecParams = Org.BouncyCastle.Crypto.EC.CustomNamedCurves.GetByName("P-384");
                if (ecParams == null)
                {
                    ecParams = Org.BouncyCastle.Asn1.X9.ECNamedCurveTable.GetByName("P-384");
                }
            }
            else
            {
                throw new ArgumentException($"Courbe non supportée: {curveName}");
            }

            if (ecParams == null)
            {
                throw new CryptographicException($"Impossible de charger les paramètres de courbe pour {curveName}");
            }

            var keyGen = new Org.BouncyCastle.Crypto.Generators.ECKeyPairGenerator();
            var keyGenParams = new Org.BouncyCastle.Crypto.Parameters.ECKeyGenerationParameters(
                new Org.BouncyCastle.Crypto.Parameters.ECDomainParameters(ecParams.Curve, ecParams.G, ecParams.N, ecParams.H, ecParams.GetSeed()),
                new SecureRandom());
            keyGen.Init(keyGenParams);
            var keyPair = keyGen.GenerateKeyPair();

            privateKey = ((Org.BouncyCastle.Crypto.Parameters.ECPrivateKeyParameters)keyPair.Private).D.ToByteArrayUnsigned();
            publicKey = ((Org.BouncyCastle.Crypto.Parameters.ECPublicKeyParameters)keyPair.Public).Q.GetEncoded(false);
        }

        /// <summary>
        /// Génère des clés RSA avec BouncyCastle
        /// </summary>
        private static void GenerateRSAKeys(int keySize, out byte[] privateKey, out byte[] publicKey)
        {
            var keyGen = new Org.BouncyCastle.Crypto.Generators.RsaKeyPairGenerator();
            var keyGenParams = new Org.BouncyCastle.Crypto.Parameters.RsaKeyGenerationParameters(
                Org.BouncyCastle.Math.BigInteger.ValueOf(65537),
                new SecureRandom(),
                keySize,
                100);
            keyGen.Init(keyGenParams);
            var keyPair = keyGen.GenerateKeyPair();

            var rsaPrivate = (Org.BouncyCastle.Crypto.Parameters.RsaPrivateCrtKeyParameters)keyPair.Private;
            var rsaPublic = (Org.BouncyCastle.Crypto.Parameters.RsaKeyParameters)keyPair.Public;

            // Exporter en format simple (modulus, exponent pour public; p, q, d pour private)
            using (var ms = new System.IO.MemoryStream())
            {
                var modulus = rsaPublic.Modulus.ToByteArrayUnsigned();
                var exponent = rsaPublic.Exponent.ToByteArrayUnsigned();
                ms.Write(BitConverter.GetBytes(modulus.Length), 0, 4);
                ms.Write(modulus, 0, modulus.Length);
                ms.Write(BitConverter.GetBytes(exponent.Length), 0, 4);
                ms.Write(exponent, 0, exponent.Length);
                publicKey = ms.ToArray();
            }

            using (var ms = new System.IO.MemoryStream())
            {
                var modulus = rsaPrivate.Modulus.ToByteArrayUnsigned();
                var p = rsaPrivate.P.ToByteArrayUnsigned();
                var q = rsaPrivate.Q.ToByteArrayUnsigned();
                var d = rsaPrivate.Exponent.ToByteArrayUnsigned();
                ms.Write(BitConverter.GetBytes(modulus.Length), 0, 4);
                ms.Write(modulus, 0, modulus.Length);
                ms.Write(BitConverter.GetBytes(p.Length), 0, 4);
                ms.Write(p, 0, p.Length);
                ms.Write(BitConverter.GetBytes(q.Length), 0, 4);
                ms.Write(q, 0, q.Length);
                ms.Write(BitConverter.GetBytes(d.Length), 0, 4);
                ms.Write(d, 0, d.Length);
                privateKey = ms.ToArray();
            }
        }

        /// <summary>
        /// Crée un wrapper Kyber pour l'algorithme post-quantique KEM spécifié
        /// </summary>
        private static KyberWrapper CreateKyberWrapper(PostQuantumAlgorithm algo)
        {
            return algo switch
            {
                PostQuantumAlgorithm.Kyber512 => new KyberWrapper(KyberWrapper.KyberParameterSet.Kyber512),
                PostQuantumAlgorithm.Kyber768 => new KyberWrapper(KyberWrapper.KyberParameterSet.Kyber768),
                PostQuantumAlgorithm.Kyber1024 => new KyberWrapper(KyberWrapper.KyberParameterSet.Kyber1024),
                _ => throw new ArgumentException($"Algorithme KEM post-quantique non valide: {algo}. Utilisez Kyber512, Kyber768 ou Kyber1024.")
            };
        }

        /// <summary>
        /// Crée un wrapper Dilithium pour l'algorithme post-quantique de signature spécifié
        /// </summary>
        private static DilithiumWrapper CreateDilithiumWrapper(PostQuantumAlgorithm algo)
        {
            return algo switch
            {
                PostQuantumAlgorithm.Dilithium2 => new DilithiumWrapper(DilithiumWrapper.DilithiumParameterSet.Dilithium2),
                PostQuantumAlgorithm.Dilithium3 => new DilithiumWrapper(DilithiumWrapper.DilithiumParameterSet.Dilithium3),
                PostQuantumAlgorithm.Dilithium5 => new DilithiumWrapper(DilithiumWrapper.DilithiumParameterSet.Dilithium5),
                _ => throw new ArgumentException($"Algorithme de signature post-quantique non valide: {algo}. Utilisez Dilithium2, Dilithium3 ou Dilithium5.")
            };
        }

        /// <summary>
        /// Exporte une configuration TLS hybride en format texte
        /// </summary>
        public static string ExportConfigToText(HybridTLSConfig config)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# TLS 1.3 Hybrid Configuration");
            sb.AppendLine($"ClassicalAlgorithm={config.ClassicalAlgo}");
            sb.AppendLine($"PostQuantumKemAlgorithm={config.PostQuantumKemAlgo}");
            sb.AppendLine($"PostQuantumSigAlgorithm={config.PostQuantumSigAlgo}");
            sb.AppendLine($"ClassicalPrivateKey={Convert.ToBase64String(config.ClassicalPrivateKey)}");
            sb.AppendLine($"ClassicalPublicKey={Convert.ToBase64String(config.ClassicalPublicKey)}");
            sb.AppendLine($"PostQuantumKemPrivateKey={Convert.ToBase64String(config.PostQuantumKemPrivateKey)}");
            sb.AppendLine($"PostQuantumKemPublicKey={Convert.ToBase64String(config.PostQuantumKemPublicKey)}");
            sb.AppendLine($"PostQuantumSigPrivateKey={Convert.ToBase64String(config.PostQuantumSigPrivateKey)}");
            sb.AppendLine($"PostQuantumSigPublicKey={Convert.ToBase64String(config.PostQuantumSigPublicKey)}");
            return sb.ToString();
        }

        /// <summary>
        /// Obtient le nom du cipher suite TLS hybride
        /// </summary>
        public static string GetCipherSuiteName(HybridTLSConfig config)
        {
            string classical = config.ClassicalAlgo switch
            {
                ClassicalAlgorithm.ECDSA_P256 => "ECDSA-P256",
                ClassicalAlgorithm.ECDSA_P384 => "ECDSA-P384",
                ClassicalAlgorithm.RSA_2048 => "RSA-2048",
                ClassicalAlgorithm.RSA_3072 => "RSA-3072",
                _ => "UNKNOWN"
            };

            string pqKem = config.PostQuantumKemAlgo switch
            {
                PostQuantumAlgorithm.Kyber512 => "ML-KEM-512",
                PostQuantumAlgorithm.Kyber768 => "ML-KEM-768",
                PostQuantumAlgorithm.Kyber1024 => "ML-KEM-1024",
                _ => "UNKNOWN"
            };

            string pqSig = config.PostQuantumSigAlgo switch
            {
                PostQuantumAlgorithm.Dilithium2 => "ML-DSA-44",
                PostQuantumAlgorithm.Dilithium3 => "ML-DSA-65",
                PostQuantumAlgorithm.Dilithium5 => "ML-DSA-87",
                _ => "UNKNOWN"
            };

            return $"TLS13-{classical}+{pqKem}+{pqSig}";
        }

        /// <summary>
        /// Valide une configuration TLS hybride
        /// </summary>
        public static bool ValidateConfig(HybridTLSConfig config)
        {
            if (config == null) return false;
            if (config.ClassicalPrivateKey == null || config.ClassicalPublicKey == null) return false;
            if (config.PostQuantumKemPrivateKey == null || config.PostQuantumKemPublicKey == null) return false;
            if (config.PostQuantumSigPrivateKey == null || config.PostQuantumSigPublicKey == null) return false;

            // Vérifier que les algorithmes KEM sont des variantes Kyber
            if (config.PostQuantumKemAlgo != PostQuantumAlgorithm.Kyber512 &&
                config.PostQuantumKemAlgo != PostQuantumAlgorithm.Kyber768 &&
                config.PostQuantumKemAlgo != PostQuantumAlgorithm.Kyber1024)
            {
                return false;
            }

            // Vérifier que les algorithmes de signature sont des variantes Dilithium
            if (config.PostQuantumSigAlgo != PostQuantumAlgorithm.Dilithium2 &&
                config.PostQuantumSigAlgo != PostQuantumAlgorithm.Dilithium3 &&
                config.PostQuantumSigAlgo != PostQuantumAlgorithm.Dilithium5)
            {
                return false;
            }

            return true;
        }
    }
}

