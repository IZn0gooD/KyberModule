using System;
using System.Security.Cryptography;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;

namespace KyberLibrary
{
    /// <summary>
    /// Classe wrapper pour le chiffrement symétrique authentifié ChaCha20-Poly1305
    /// ChaCha20 est un chiffrement de flux moderne, Poly1305 est un MAC (Message Authentication Code)
    /// Combinaison recommandée pour la sécurité moderne et les performances
    /// </summary>
    public class ChaCha20Poly1305Wrapper
    {
        private const int KeySize = 32; // 256 bits
        private const int NonceSize = 12; // 96 bits (recommandé pour ChaCha20-Poly1305)
        private const int TagSize = 16; // 128 bits (taille du tag Poly1305)

        /// <summary>
        /// Génère une clé aléatoire de 32 bytes (256 bits)
        /// </summary>
        /// <returns>Clé générée</returns>
        public static byte[] GenerateKey()
        {
            byte[] key = new byte[KeySize];
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
        /// Chiffre et authentifie des données avec ChaCha20-Poly1305
        /// </summary>
        /// <param name="plaintext">Données en clair à chiffrer</param>
        /// <param name="key">Clé de chiffrement (32 bytes / 256 bits)</param>
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

            if (key == null || key.Length != KeySize)
            {
                throw new ArgumentException($"La clé doit faire exactement {KeySize} bytes (256 bits)", nameof(key));
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
                // Créer le moteur ChaCha20 (ChaCha7539Engine = ChaCha20 avec 20 rounds)
                var chacha20 = new ChaCha7539Engine();

                // Créer le mode AEAD (Authenticated Encryption with Associated Data) avec Poly1305
                // ChaCha20Poly1305 est un mode AEAD combinant ChaCha20 et Poly1305
                var aeadCipher = new Org.BouncyCastle.Crypto.Modes.ChaCha20Poly1305();

                // Préparer les paramètres AEAD
                AeadParameters parameters = new AeadParameters(
                    new KeyParameter(key),
                    TagSize * 8, // Tag size en bits (128 bits pour Poly1305)
                    nonce,
                    associatedData);

                // Initialiser pour le chiffrement
                aeadCipher.Init(true, parameters); // true = mode chiffrement

                // Chiffrer
                byte[] ciphertextWithTag = new byte[aeadCipher.GetOutputSize(plaintext.Length)];
                int len = aeadCipher.ProcessBytes(plaintext, 0, plaintext.Length, ciphertextWithTag, 0);
                aeadCipher.DoFinal(ciphertextWithTag, len);

                return (ciphertextWithTag, nonce);
            }
            catch (Exception ex)
            {
                throw new CryptographicException($"Erreur lors du chiffrement ChaCha20-Poly1305: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Déchiffre et vérifie l'authenticité de données avec ChaCha20-Poly1305
        /// </summary>
        /// <param name="ciphertextWithTag">Données chiffrées avec tag (ciphertext + tag de 16 bytes)</param>
        /// <param name="key">Clé de chiffrement (32 bytes / 256 bits)</param>
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

            if (key == null || key.Length != KeySize)
            {
                throw new ArgumentException($"La clé doit faire exactement {KeySize} bytes (256 bits)", nameof(key));
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
                // Créer le moteur ChaCha20 (ChaCha7539Engine = ChaCha20 avec 20 rounds)
                var chacha20 = new ChaCha7539Engine();

                // Créer le mode AEAD (Authenticated Encryption with Associated Data) avec Poly1305
                var aeadCipher = new Org.BouncyCastle.Crypto.Modes.ChaCha20Poly1305();

                // Préparer les paramètres AEAD
                AeadParameters parameters = new AeadParameters(
                    new KeyParameter(key),
                    TagSize * 8, // Tag size en bits (128 bits pour Poly1305)
                    nonce,
                    associatedData);

                // Initialiser pour le déchiffrement
                aeadCipher.Init(false, parameters); // false = mode déchiffrement

                // Déchiffrer
                byte[] plaintext = new byte[aeadCipher.GetOutputSize(ciphertextWithTag.Length)];
                int len = aeadCipher.ProcessBytes(ciphertextWithTag, 0, ciphertextWithTag.Length, plaintext, 0);
                aeadCipher.DoFinal(plaintext, len);

                return plaintext;
            }
            catch (Exception ex)
            {
                throw new CryptographicException($"Erreur lors du déchiffrement ChaCha20-Poly1305: {ex.Message}. Le tag d'authentification est peut-être invalide.", ex);
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

