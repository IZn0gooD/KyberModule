using System;
using System.Security.Cryptography;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Crypto.Generators;

namespace KyberLibrary
{
    /// <summary>
    /// Classe wrapper pour l'implémentation Ed25519, algorithme de signature cryptographique
    /// </summary>
    public class Ed25519Wrapper
    {
        private SecureRandom _random;

        /// <summary>
        /// Initialise une nouvelle instance de Ed25519Wrapper
        /// </summary>
        public Ed25519Wrapper()
        {
            _random = new SecureRandom();
        }

        /// <summary>
        /// Génère une paire de clés Ed25519 (clé publique et clé privée)
        /// </summary>
        /// <returns>Tuple contenant la clé publique et la clé privée</returns>
        public (byte[] PublicKey, byte[] PrivateKey) GenerateKeyPair()
        {
            try
            {
                // Créer le générateur de clés Ed25519
                Ed25519KeyPairGenerator keyPairGenerator = new Ed25519KeyPairGenerator();
                keyPairGenerator.Init(new Ed25519KeyGenerationParameters(_random));

                // Générer la paire de clés
                var keyPair = keyPairGenerator.GenerateKeyPair();
                var publicKey = (Ed25519PublicKeyParameters)keyPair.Public;
                var privateKey = (Ed25519PrivateKeyParameters)keyPair.Private;

                // Extraire les bytes des clés
                byte[] publicKeyBytes = publicKey.GetEncoded();
                byte[] privateKeyBytes = privateKey.GetEncoded();

                return (publicKeyBytes, privateKeyBytes);
            }
            catch (Exception ex)
            {
                throw new CryptographicException("Erreur lors de la génération de la paire de clés Ed25519", ex);
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
                throw new CryptographicException("Erreur lors de la génération sécurisée de la paire de clés Ed25519", ex);
            }
        }

        /// <summary>
        /// Signe des données avec une clé privée Ed25519
        /// </summary>
        /// <param name="data">Les données à signer</param>
        /// <param name="privateKey">La clé privée Ed25519</param>
        /// <returns>La signature</returns>
        public byte[] Sign(byte[] data, byte[] privateKey)
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
                Ed25519PrivateKeyParameters privateKeyParams = new Ed25519PrivateKeyParameters(privateKey, 0);

                // Créer le signeur
                Ed25519Signer signer = new Ed25519Signer();
                signer.Init(true, privateKeyParams);

                // Signer les données
                signer.BlockUpdate(data, 0, data.Length);
                byte[] signature = signer.GenerateSignature();

                return signature;
            }
            catch (Exception ex)
            {
                throw new CryptographicException($"Erreur lors de la signature Ed25519: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Vérifie une signature avec une clé publique Ed25519
        /// </summary>
        /// <param name="data">Les données originales</param>
        /// <param name="signature">La signature à vérifier</param>
        /// <param name="publicKey">La clé publique Ed25519</param>
        /// <returns>True si la signature est valide, False sinon</returns>
        public bool Verify(byte[] data, byte[] signature, byte[] publicKey)
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
                Ed25519PublicKeyParameters publicKeyParams = new Ed25519PublicKeyParameters(publicKey, 0);

                // Créer le vérificateur
                Ed25519Signer verifier = new Ed25519Signer();
                verifier.Init(false, publicKeyParams);

                // Vérifier la signature
                verifier.BlockUpdate(data, 0, data.Length);
                bool isValid = verifier.VerifySignature(signature);

                return isValid;
            }
            catch (Exception ex)
            {
                throw new CryptographicException($"Erreur lors de la vérification de signature Ed25519: {ex.Message}", ex);
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

