using System;
using System.Security.Cryptography;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;

namespace KyberLibrary
{
    /// <summary>
    /// Classe wrapper pour le chiffrement symétrique authentifié AES-GCM
    /// AES (Advanced Encryption Standard) en mode GCM (Galois/Counter Mode)
    /// Mode recommandé pour AES avec authentification intégrée
    /// </summary>
    public class AESGCMWrapper
    {
        /// <summary>
        /// Tailles de clés supportées pour AES
        /// </summary>
        public enum AESKeySize
        {
            AES128 = 128,  // 16 bytes
            AES192 = 192,  // 24 bytes
            AES256 = 256   // 32 bytes
        }

        private const int NonceSize = 12; // 96 bits (recommandé pour GCM)
        private const int TagSize = 16; // 128 bits (taille du tag GCM)

        /// <summary>
        /// Génère une clé aléatoire pour AES
        /// </summary>
        /// <param name="keySize">Taille de la clé (128, 192, ou 256 bits)</param>
        /// <returns>Clé générée</returns>
        public static byte[] GenerateKey(AESKeySize keySize = AESKeySize.AES256)
        {
            int keySizeBytes = (int)keySize / 8;
            byte[] key = new byte[keySizeBytes];
            var rng = new SecureRandom();
            rng.NextBytes(key);
            return key;
        }

        /// <summary>
        /// Génère un nonce aléatoire de 12 bytes (96 bits)
        /// </summary>
        /// <returns>Nonce généré</returns>
        public static byte[] GenerateNonce()
        {
            byte[] nonce = new byte[NonceSize];
            var rng = new SecureRandom();
            rng.NextBytes(nonce);
            return nonce;
        }

        /// <summary>
        /// Chiffre et authentifie des données avec AES-GCM
        /// </summary>
        /// <param name="plaintext">Données en clair à chiffrer</param>
        /// <param name="key">Clé de chiffrement (16, 24, ou 32 bytes pour AES-128/192/256)</param>
        /// <param name="nonce">Nonce (12 bytes / 96 bits). Si null, généré automatiquement</param>
        /// <param name="associatedData">Données associées (AAD) pour l'authentification (optionnel)</param>
        /// <returns>Tuple contenant: (ciphertext avec tag, nonce utilisé)</returns>
        public static (byte[] CiphertextWithTag, byte[] Nonce) Encrypt(
            byte[] plaintext, 
            byte[] key, 
            byte[] nonce = null, 
            byte[] associatedData = null)
        {
            if (plaintext == null)
            {
                throw new ArgumentNullException(nameof(plaintext), "Le plaintext ne peut pas être null");
            }

            if (key == null || (key.Length != 16 && key.Length != 24 && key.Length != 32))
            {
                throw new ArgumentException("La clé doit faire 16, 24 ou 32 bytes (AES-128, AES-192, ou AES-256)", nameof(key));
            }

            // Générer un nonce si non fourni
            if (nonce == null)
            {
                nonce = GenerateNonce();
            }
            else if (nonce.Length != NonceSize)
            {
                throw new ArgumentException($"Le nonce doit faire exactement {NonceSize} bytes (96 bits)", nameof(nonce));
            }

            try
            {
                // Créer le moteur AES
                var aesEngine = new AesEngine();

                // Créer le mode GCM avec authentification
                var gcmCipher = new GcmBlockCipher(aesEngine);

                // Préparer les paramètres AEAD
                AeadParameters parameters = new AeadParameters(
                    new KeyParameter(key),
                    TagSize * 8, // Tag size en bits (128 bits pour GCM)
                    nonce,
                    associatedData);

                // Initialiser pour le chiffrement
                gcmCipher.Init(true, parameters); // true = mode chiffrement

                // Chiffrer
                byte[] ciphertextWithTag = new byte[gcmCipher.GetOutputSize(plaintext.Length)];
                int len = gcmCipher.ProcessBytes(plaintext, 0, plaintext.Length, ciphertextWithTag, 0);
                gcmCipher.DoFinal(ciphertextWithTag, len);

                return (ciphertextWithTag, nonce);
            }
            catch (Exception ex)
            {
                throw new CryptographicException($"Erreur lors du chiffrement AES-GCM: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Déchiffre et vérifie l'authenticité de données avec AES-GCM
        /// </summary>
        /// <param name="ciphertextWithTag">Données chiffrées avec tag (ciphertext + tag de 16 bytes)</param>
        /// <param name="key">Clé de chiffrement (16, 24, ou 32 bytes pour AES-128/192/256)</param>
        /// <param name="nonce">Nonce utilisé lors du chiffrement (12 bytes / 96 bits)</param>
        /// <param name="associatedData">Données associées (AAD) pour l'authentification (optionnel, doit correspondre au chiffrement)</param>
        /// <returns>Données en clair</returns>
        public static byte[] Decrypt(
            byte[] ciphertextWithTag, 
            byte[] key, 
            byte[] nonce, 
            byte[] associatedData = null)
        {
            if (ciphertextWithTag == null)
            {
                throw new ArgumentNullException(nameof(ciphertextWithTag), "Le ciphertext ne peut pas être null");
            }

            if (key == null || (key.Length != 16 && key.Length != 24 && key.Length != 32))
            {
                throw new ArgumentException("La clé doit faire 16, 24 ou 32 bytes (AES-128, AES-192, ou AES-256)", nameof(key));
            }

            if (nonce == null || nonce.Length != NonceSize)
            {
                throw new ArgumentException($"Le nonce doit faire exactement {NonceSize} bytes (96 bits)", nameof(nonce));
            }

            if (ciphertextWithTag.Length < TagSize)
            {
                throw new ArgumentException($"Le ciphertext doit contenir au moins {TagSize} bytes pour le tag", nameof(ciphertextWithTag));
            }

            try
            {
                // Créer le moteur AES
                var aesEngine = new AesEngine();

                // Créer le mode GCM avec authentification
                var gcmCipher = new GcmBlockCipher(aesEngine);

                // Préparer les paramètres AEAD
                AeadParameters parameters = new AeadParameters(
                    new KeyParameter(key),
                    TagSize * 8, // Tag size en bits (128 bits pour GCM)
                    nonce,
                    associatedData);

                // Initialiser pour le déchiffrement
                gcmCipher.Init(false, parameters); // false = mode déchiffrement

                // Déchiffrer
                byte[] plaintext = new byte[gcmCipher.GetOutputSize(ciphertextWithTag.Length)];
                int len = gcmCipher.ProcessBytes(ciphertextWithTag, 0, ciphertextWithTag.Length, plaintext, 0);
                gcmCipher.DoFinal(plaintext, len);

                return plaintext;
            }
            catch (Exception ex)
            {
                throw new CryptographicException($"Erreur lors du déchiffrement AES-GCM: {ex.Message}. Le tag d'authentification est peut-être invalide.", ex);
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
    }
}

