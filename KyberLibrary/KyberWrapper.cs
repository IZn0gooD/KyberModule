using System;
using System.Security.Cryptography;
using System.IO;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Kems;
using Org.BouncyCastle.Security;

namespace KyberLibrary
{
    /// <summary>
    /// Classe wrapper pour l'implémentation de Kyber (ML-KEM), algorithme de cryptographie post-quantique
    /// Utilise BouncyCastle.Cryptography 2.6.2 avec les classes MLKem*
    /// </summary>
    public class KyberWrapper
    {
        // Paramètres Kyber standard (ML-KEM)
        public enum KyberParameterSet
        {
            Kyber512,   // ML-KEM-512
            Kyber768,   // ML-KEM-768
            Kyber1024   // ML-KEM-1024
        }

        private KyberParameterSet _parameterSet;
        private MLKemParameters _mlKemParameters;
        private SecureRandom _random;

        /// <summary>
        /// Initialise une nouvelle instance de KyberWrapper
        /// </summary>
        /// <param name="parameterSet">Paramètre de sécurité Kyber (512, 768, ou 1024)</param>
        public KyberWrapper(KyberParameterSet parameterSet = KyberParameterSet.Kyber768)
        {
            _parameterSet = parameterSet;
            _mlKemParameters = GetMLKemParameters(parameterSet);
            _random = new SecureRandom();
        }

        /// <summary>
        /// Convertit notre enum vers les paramètres ML-KEM BouncyCastle
        /// </summary>
        private MLKemParameters GetMLKemParameters(KyberParameterSet parameterSet)
        {
            return parameterSet switch
            {
                KyberParameterSet.Kyber512 => MLKemParameters.ml_kem_512,
                KyberParameterSet.Kyber768 => MLKemParameters.ml_kem_768,
                KyberParameterSet.Kyber1024 => MLKemParameters.ml_kem_1024,
                _ => MLKemParameters.ml_kem_768
            };
        }

        /// <summary>
        /// Génère une paire de clés (clé publique et clé privée)
        /// </summary>
        /// <returns>Tuple contenant la clé publique et la clé privée</returns>
        public (byte[] PublicKey, byte[] PrivateKey) GenerateKeyPair()
        {
            try
            {
                // Créer les paramètres de génération de clés
                MLKemKeyGenerationParameters keyGenParams = new MLKemKeyGenerationParameters(_random, _mlKemParameters);
                
                // Créer le générateur de paires de clés
                MLKemKeyPairGenerator keyPairGenerator = new MLKemKeyPairGenerator();
                keyPairGenerator.Init(keyGenParams);
                
                // Générer la paire de clés
                var keyPair = keyPairGenerator.GenerateKeyPair();
                var publicKey = (MLKemPublicKeyParameters)keyPair.Public;
                var privateKey = (MLKemPrivateKeyParameters)keyPair.Private;
                
                // Extraire les bytes des clés
                byte[] publicKeyBytes = publicKey.GetEncoded();
                
                // Pour la clé privée, utiliser GetEncoded() qui retourne le format correct de BouncyCastle
                byte[] privateKeyBytes = privateKey.GetEncoded();
                
                return (publicKeyBytes, privateKeyBytes);
            }
            catch (Exception ex)
            {
                throw new CryptographicException("Erreur lors de la génération de la paire de clés Kyber", ex);
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
                throw new CryptographicException("Erreur lors de la génération sécurisée de la paire de clés Kyber", ex);
            }
        }

        /// <summary>
        /// Encapsule une clé partagée à partir d'une clé publique
        /// </summary>
        /// <param name="publicKey">La clé publique</param>
        /// <returns>Tuple contenant le ciphertext et la clé partagée</returns>
        public (byte[] Ciphertext, byte[] SharedSecret) Encapsulate(byte[] publicKey)
        {
            if (publicKey == null)
            {
                throw new ArgumentNullException(nameof(publicKey), "La clé publique ne peut pas être null");
            }

            try
            {
                // Reconstruire la clé publique depuis les bytes
                MLKemPublicKeyParameters publicKeyParams = MLKemPublicKeyParameters.FromEncoding(_mlKemParameters, publicKey);
                
                // Créer l'encapsulateur
                MLKemEncapsulator encapsulator = new MLKemEncapsulator(_mlKemParameters);
                encapsulator.Init(publicKeyParams);
                
                // Préparer les buffers pour le ciphertext et le secret
                int encapsulationLength = encapsulator.EncapsulationLength;
                int secretLength = encapsulator.SecretLength;
                
                byte[] ciphertext = new byte[encapsulationLength];
                byte[] sharedSecret = new byte[secretLength];
                
                // Encapsuler: Encapsulate(byte[] ctBuf, int ctOff, int ctLen, byte[] ssBuf, int ssOff, int ssLen)
                encapsulator.Encapsulate(ciphertext, 0, encapsulationLength, sharedSecret, 0, secretLength);
                
                return (ciphertext, sharedSecret);
            }
            catch (Exception ex)
            {
                throw new CryptographicException("Erreur lors de l'encapsulation Kyber", ex);
            }
        }

        /// <summary>
        /// Décapsule une clé partagée à partir d'un ciphertext et d'une clé privée
        /// </summary>
        /// <param name="ciphertext">Le ciphertext</param>
        /// <param name="privateKey">La clé privée</param>
        /// <returns>La clé partagée</returns>
        public byte[] Decapsulate(byte[] ciphertext, byte[] privateKey)
        {
            if (ciphertext == null)
            {
                throw new ArgumentNullException(nameof(ciphertext), "Le ciphertext ne peut pas être null");
            }
            
            if (privateKey == null)
            {
                throw new ArgumentNullException(nameof(privateKey), "La clé privée ne peut pas être null");
            }

            try
            {
                // Reconstruire la clé privée depuis les bytes
                MLKemPrivateKeyParameters privateKeyParams = MLKemPrivateKeyParameters.FromEncoding(_mlKemParameters, privateKey);
                
                // Créer le décapsulateur
                MLKemDecapsulator decapsulator = new MLKemDecapsulator(_mlKemParameters);
                decapsulator.Init(privateKeyParams);
                
                // Préparer le buffer pour le secret
                int secretLength = decapsulator.SecretLength;
                byte[] sharedSecret = new byte[secretLength];
                
                // Décapsuler: Decapsulate(byte[] ctBuf, int ctOff, int ctLen, byte[] ssBuf, int ssOff, int ssLen)
                decapsulator.Decapsulate(ciphertext, 0, ciphertext.Length, sharedSecret, 0, secretLength);
                
                return sharedSecret;
            }
            catch (Exception ex)
            {
                throw new CryptographicException($"Erreur lors de la décapsulation Kyber: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Décapsule une clé partagée avec protection contre les attaques par canaux auxiliaires
        /// Utilise des opérations à temps constant et nettoie le cache
        /// </summary>
        /// <param name="ciphertext">Le ciphertext</param>
        /// <param name="privateKey">La clé privée</param>
        /// <returns>La clé partagée</returns>
        public byte[] DecapsulateSecure(byte[] ciphertext, byte[] privateKey)
        {
            return SideChannelProtection.ProtectTiming(() => Decapsulate(ciphertext, privateKey));
        }

        /// <summary>
        /// Compare deux clés partagées en temps constant
        /// Protège contre les attaques par timing lors de la vérification
        /// </summary>
        /// <param name="secret1">Première clé partagée</param>
        /// <param name="secret2">Deuxième clé partagée</param>
        /// <returns>True si les clés sont identiques</returns>
        public static bool ConstantTimeCompareSharedSecrets(byte[] secret1, byte[] secret2)
        {
            return SideChannelProtection.SecureCompare(secret1, secret2);
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
        public KyberParameterSet GetParameterSet()
        {
            return _parameterSet;
        }
    }
}
