using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Signers;
using Org.BouncyCastle.Security;
using System.Threading.Tasks;

namespace KyberLibrary
{
    /// <summary>
    /// Classe wrapper pour l'implémentation Dilithium (ML-DSA), algorithme de signature post-quantique
    /// Utilise BouncyCastle.Cryptography 2.6.2 avec les classes MLDsa*
    /// </summary>
    public class DilithiumWrapper
    {
        // Paramètres Dilithium standard (ML-DSA)
        public enum DilithiumParameterSet
        {
            Dilithium2,    // ML-DSA-44 (NIST niveau 1)
            Dilithium3,    // ML-DSA-65 (NIST niveau 2)
            Dilithium5     // ML-DSA-87 (NIST niveau 3)
        }

        private DilithiumParameterSet _parameterSet;
        private MLDsaParameters _mlDsaParameters;
        private static readonly ThreadLocal<SecureRandom> ThreadRandom = new(() => new SecureRandom());

        /// <summary>
        /// Initialise une nouvelle instance de DilithiumWrapper
        /// </summary>
        /// <param name="parameterSet">Paramètre de sécurité Dilithium (2, 3, ou 5)</param>
        public DilithiumWrapper(DilithiumParameterSet parameterSet = DilithiumParameterSet.Dilithium3)
        {
            _parameterSet = parameterSet;
            _mlDsaParameters = GetMLDsaParameters(parameterSet);
        }

        /// <summary>
        /// Convertit notre enum vers les paramètres ML-DSA BouncyCastle
        /// </summary>
        private MLDsaParameters GetMLDsaParameters(DilithiumParameterSet parameterSet)
        {
            return parameterSet switch
            {
                DilithiumParameterSet.Dilithium2 => MLDsaParameters.ml_dsa_44,
                DilithiumParameterSet.Dilithium3 => MLDsaParameters.ml_dsa_65,
                DilithiumParameterSet.Dilithium5 => MLDsaParameters.ml_dsa_87,
                _ => MLDsaParameters.ml_dsa_65
            };
        }

        /// <summary>
        /// Génère une paire de clés Dilithium (clé publique et clé privée)
        /// </summary>
        /// <returns>Tuple contenant la clé publique et la clé privée</returns>
        public (byte[] PublicKey, byte[] PrivateKey) GenerateKeyPair()
        {
            try
            {
                // Créer les paramètres de génération de clés
                var random = ThreadRandom.Value ?? new SecureRandom();
                MLDsaKeyGenerationParameters keyGenParams = new MLDsaKeyGenerationParameters(random, _mlDsaParameters);
                
                // Créer le générateur de paires de clés
                MLDsaKeyPairGenerator keyPairGenerator = new MLDsaKeyPairGenerator();
                keyPairGenerator.Init(keyGenParams);
                
                // Générer la paire de clés
                var keyPair = keyPairGenerator.GenerateKeyPair();
                var publicKey = (MLDsaPublicKeyParameters)keyPair.Public;
                var privateKey = (MLDsaPrivateKeyParameters)keyPair.Private;
                
                // Extraire les bytes des clés
                byte[] publicKeyBytes = publicKey.GetEncoded();
                byte[] privateKeyBytes = privateKey.GetEncoded();
                
                return (publicKeyBytes, privateKeyBytes);
            }
            catch (Exception ex)
            {
                throw new CryptographicException("Erreur lors de la génération de la paire de clés Dilithium", ex);
            }
        }

        /// <summary>
        /// Génère une paire de clés avec gestion sécurisée en mémoire
        /// Retourne un wrapper qui nettoie automatiquement la clé privée lors du Dispose
        /// </summary>
        /// <returns>Wrapper sécurisé contenant la paire de clés</returns>
        public SecureKeyPairWrapper GenerateKeyPairSecure()
        {
            try
            {
                var (publicKey, privateKey) = GenerateKeyPair();
                return new SecureKeyPairWrapper(publicKey, privateKey);
            }
            catch (Exception ex)
            {
                throw new CryptographicException("Erreur lors de la génération sécurisée de la paire de clés Dilithium", ex);
            }
        }

        /// <summary>
        /// Signe des données avec une clé privée Dilithium
        /// </summary>
        /// <param name="data">Les données à signer</param>
        /// <param name="privateKey">La clé privée Dilithium</param>
        /// <param name="preHash">Si true, pré-hache les données avec SHA3-256 avant de signer (recommandé pour ML-DSA)</param>
        /// <returns>La signature</returns>
        public byte[] Sign(byte[] data, byte[] privateKey, bool preHash = false)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data), "Les données ne peuvent pas être null");
            }

            if (privateKey == null)
            {
                throw new ArgumentNullException(nameof(privateKey), "La clé privée ne peut pas être null");
            }

            try
            {
                // Reconstruire la clé privée depuis les bytes
                MLDsaPrivateKeyParameters privateKeyParams = MLDsaPrivateKeyParameters.FromEncoding(_mlDsaParameters, privateKey);

                // Pré-hacher avec SHA3-256 si demandé (recommandé pour ML-DSA)
                byte[] dataToSign;
                int processedLength;
                if (preHash)
                {
                    dataToSign = SHA3Wrapper.ComputeSHA3_256(data);
                    processedLength = dataToSign.Length;
                }
                else
                {
                    dataToSign = new byte[data.Length];
                    Buffer.BlockCopy(data, 0, dataToSign, 0, data.Length);
                    processedLength = data.Length;
                }

                // Créer le signeur (le deuxième paramètre indique si les données sont pré-hachées)
                MLDsaSigner signer = new MLDsaSigner(_mlDsaParameters, preHash);
                signer.Init(true, privateKeyParams);

                // Signer les données
                try
                {
                    signer.BlockUpdate(dataToSign, 0, processedLength);
                    return signer.GenerateSignature();
                }
                finally
                {
                    SecureZero(dataToSign, processedLength);
                }
            }
            catch (Exception ex)
            {
                throw new CryptographicException($"Erreur lors de la signature Dilithium: {ex.Message}", ex);
            }
        }

        public byte[][] SignBatch(IReadOnlyList<byte[]> messages, byte[] privateKey, bool preHash = true, bool parallel = true)
        {
            if (messages == null)
            {
                throw new ArgumentNullException(nameof(messages));
            }

            if (privateKey == null)
            {
                throw new ArgumentNullException(nameof(privateKey));
            }

            if (messages.Count == 0)
            {
                return Array.Empty<byte[]>();
            }

            var privateKeyParams = MLDsaPrivateKeyParameters.FromEncoding(_mlDsaParameters, privateKey);
            var signatures = new byte[messages.Count][];

            void SignAt(int index)
            {
                var message = messages[index];
                byte[] buffer;
                int processedLength;
                if (preHash)
                {
                    var temp = new byte[message.Length];
                    Buffer.BlockCopy(message, 0, temp, 0, message.Length);
                    try
                    {
                        buffer = SHA3Wrapper.ComputeSHA3_256(temp);
                        processedLength = buffer.Length;
                    }
                    finally
                    {
                        SecureZero(temp, temp.Length);
                    }
                }
                else
                {
                    buffer = new byte[message.Length];
                    Buffer.BlockCopy(message, 0, buffer, 0, message.Length);
                    processedLength = message.Length;
                }

                try
                {
                    var signer = new MLDsaSigner(_mlDsaParameters, preHash);
                    signer.Init(true, privateKeyParams);
                    signer.BlockUpdate(buffer, 0, processedLength);
                    signatures[index] = signer.GenerateSignature();
                }
                finally
                {
                    SecureZero(buffer, processedLength);
                }
            }

            if (parallel && messages.Count > 1)
            {
                Parallel.For(0, messages.Count, SignAt);
            }
            else
            {
                for (int i = 0; i < messages.Count; i++)
                {
                    SignAt(i);
                }
            }

            return signatures;
        }

        /// <summary>
        /// Signe des données avec une clé privée Dilithium en utilisant SHA3-256 pour le pré-hachage
        /// </summary>
        /// <param name="data">Les données à signer</param>
        /// <param name="privateKey">La clé privée Dilithium</param>
        /// <param name="sha3Variant">Variant SHA3 à utiliser pour le pré-hachage (SHA3_256 ou SHA3_384)</param>
        /// <returns>La signature</returns>
        public byte[] SignWithSHA3(byte[] data, byte[] privateKey, SHA3Wrapper.SHA3Variant sha3Variant = SHA3Wrapper.SHA3Variant.SHA3_256)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data), "Les données ne peuvent pas être null");
            }

            if (privateKey == null)
            {
                throw new ArgumentNullException(nameof(privateKey), "La clé privée ne peut pas être null");
            }

            try
            {
                // Pré-hacher avec SHA3
                byte[] hashedData = SHA3Wrapper.ComputeHash(data, sha3Variant);

                // Signer les données hachées
                return Sign(hashedData, privateKey, true);
            }
            catch (Exception ex)
            {
                throw new CryptographicException($"Erreur lors de la signature Dilithium avec SHA3: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Vérifie une signature avec une clé publique Dilithium
        /// </summary>
        /// <param name="data">Les données originales</param>
        /// <param name="signature">La signature à vérifier</param>
        /// <param name="publicKey">La clé publique Dilithium</param>
        /// <param name="preHash">Si true, pré-hache les données avec SHA3-256 avant de vérifier (doit correspondre à la signature)</param>
        /// <returns>True si la signature est valide, False sinon</returns>
        public bool Verify(byte[] data, byte[] signature, byte[] publicKey, bool preHash = false)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data), "Les données ne peuvent pas être null");
            }

            if (signature == null)
            {
                throw new ArgumentNullException(nameof(signature), "La signature ne peut pas être null");
            }

            if (publicKey == null)
            {
                throw new ArgumentNullException(nameof(publicKey), "La clé publique ne peut pas être null");
            }

            try
            {
                // Reconstruire la clé publique depuis les bytes
                MLDsaPublicKeyParameters publicKeyParams = MLDsaPublicKeyParameters.FromEncoding(_mlDsaParameters, publicKey);

                // Pré-hacher avec SHA3-256 si demandé
                byte[] dataToVerify;
                int processedLength;
                if (preHash)
                {
                    dataToVerify = SHA3Wrapper.ComputeSHA3_256(data);
                    processedLength = dataToVerify.Length;
                }
                else
                {
                    dataToVerify = new byte[data.Length];
                    Buffer.BlockCopy(data, 0, dataToVerify, 0, data.Length);
                    processedLength = data.Length;
                }

                // Créer le vérificateur (le deuxième paramètre indique si les données sont pré-hachées)
                MLDsaSigner verifier = new MLDsaSigner(_mlDsaParameters, preHash);
                verifier.Init(false, publicKeyParams);

                // Vérifier la signature
                try
                {
                    verifier.BlockUpdate(dataToVerify, 0, processedLength);
                    return verifier.VerifySignature(signature);
                }
                finally
                {
                    SecureZero(dataToVerify, processedLength);
                }
            }
            catch (Exception ex)
            {
                throw new CryptographicException($"Erreur lors de la vérification de signature Dilithium: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Vérifie une signature avec protection contre les attaques par canaux auxiliaires
        /// Utilise des opérations à temps constant et nettoie le cache
        /// </summary>
        /// <param name="data">Les données originales</param>
        /// <param name="signature">La signature à vérifier</param>
        /// <param name="publicKey">La clé publique Dilithium</param>
        /// <param name="preHash">Si true, pré-hache les données avec SHA3-256 avant de vérifier</param>
        /// <returns>True si la signature est valide, False sinon</returns>
        public bool VerifySecure(byte[] data, byte[] signature, byte[] publicKey, bool preHash = false)
        {
            return SideChannelProtection.ProtectTiming(() => Verify(data, signature, publicKey, preHash));
        }

        /// <summary>
        /// Compare deux signatures en temps constant
        /// Protège contre les attaques par timing lors de la vérification
        /// </summary>
        /// <param name="signature1">Première signature</param>
        /// <param name="signature2">Deuxième signature</param>
        /// <returns>True si les signatures sont identiques</returns>
        public static bool ConstantTimeCompareSignatures(byte[] signature1, byte[] signature2)
        {
            return SideChannelProtection.SecureCompare(signature1, signature2);
        }

        /// <summary>
        /// Vérifie une signature avec une clé publique Dilithium en utilisant SHA3 pour le pré-hachage
        /// </summary>
        /// <param name="data">Les données originales</param>
        /// <param name="signature">La signature à vérifier</param>
        /// <param name="publicKey">La clé publique Dilithium</param>
        /// <param name="sha3Variant">Variant SHA3 à utiliser pour le pré-hachage (doit correspondre à celui utilisé pour la signature)</param>
        /// <returns>True si la signature est valide, False sinon</returns>
        public bool VerifyWithSHA3(byte[] data, byte[] signature, byte[] publicKey, SHA3Wrapper.SHA3Variant sha3Variant = SHA3Wrapper.SHA3Variant.SHA3_256)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data), "Les données ne peuvent pas être null");
            }

            if (signature == null)
            {
                throw new ArgumentNullException(nameof(signature), "La signature ne peut pas être null");
            }

            if (publicKey == null)
            {
                throw new ArgumentNullException(nameof(publicKey), "La clé publique ne peut pas être null");
            }

            try
            {
                // Pré-hacher avec SHA3
                byte[] hashedData = SHA3Wrapper.ComputeHash(data, sha3Variant);

                // Vérifier la signature avec les données hachées
                return Verify(hashedData, signature, publicKey, true);
            }
            catch (Exception ex)
            {
                throw new CryptographicException($"Erreur lors de la vérification de signature Dilithium avec SHA3: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Convertit un tableau de bytes en chaîne hexadécimale
        /// </summary>
        public static string BytesToHex(byte[] bytes)
        {
            return BitConverter.ToString(bytes).Replace("-", "");
        }

        /// <summary>
        /// Convertit une chaîne hexadécimale en tableau de bytes
        /// </summary>
        public static byte[] HexToBytes(string hex)
        {
            int length = hex.Length;
            byte[] bytes = new byte[length / 2];
            for (int i = 0; i < length; i += 2)
            {
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            }
            return bytes;
        }

        /// <summary>
        /// Obtient le paramètre de sécurité actuel
        /// </summary>
        public DilithiumParameterSet GetParameterSet()
        {
            return _parameterSet;
        }

        private static void SecureZero(byte[] buffer, int length)
        {
            if (buffer == null)
            {
                return;
            }

            var limit = Math.Min(length, buffer.Length);
            for (int i = 0; i < limit; i++)
            {
                buffer[i] = 0;
            }
        }
    }
}
