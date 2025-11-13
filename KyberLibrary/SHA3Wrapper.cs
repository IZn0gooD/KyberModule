using System;
using System.Security.Cryptography;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto;

namespace KyberLibrary
{
    /// <summary>
    /// Classe wrapper pour les fonctions de hachage SHA3 (SHA-3)
    /// Implémente SHA3-256 et SHA3-384 conformes aux standards NIST
    /// Utilise BouncyCastle.Cryptography pour l'implémentation
    /// </summary>
    public class SHA3Wrapper
    {
        /// <summary>
        /// Types de hachage SHA3 supportés
        /// </summary>
        public enum SHA3Variant
        {
            SHA3_256,   // SHA3-256 (256 bits / 32 bytes)
            SHA3_384    // SHA3-384 (384 bits / 48 bytes)
        }

        private readonly SHA3Variant _variant;
        private readonly IDigest _digest;

        /// <summary>
        /// Initialise une nouvelle instance de SHA3Wrapper
        /// </summary>
        /// <param name="variant">Type de SHA3 à utiliser (SHA3_256 ou SHA3_384)</param>
        public SHA3Wrapper(SHA3Variant variant = SHA3Variant.SHA3_256)
        {
            _variant = variant;
            _digest = GetDigest(variant);
        }

        /// <summary>
        /// Obtient l'implémentation du digest BouncyCastle selon le variant
        /// </summary>
        private IDigest GetDigest(SHA3Variant variant)
        {
            return variant switch
            {
                SHA3Variant.SHA3_256 => new Sha3Digest(256),
                SHA3Variant.SHA3_384 => new Sha3Digest(384),
                _ => new Sha3Digest(256)
            };
        }

        /// <summary>
        /// Calcule le hachage SHA3 des données fournies
        /// </summary>
        /// <param name="data">Les données à hacher</param>
        /// <returns>Le hachage calculé</returns>
        public byte[] ComputeHash(byte[] data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data), "Les données ne peuvent pas être null");
            }

            try
            {
                // Réinitialiser le digest
                _digest.Reset();

                // Mettre à jour avec les données
                _digest.BlockUpdate(data, 0, data.Length);

                // Obtenir la taille du résultat
                int digestLength = _digest.GetDigestSize();
                byte[] hash = new byte[digestLength];

                // Calculer le digest final
                _digest.DoFinal(hash, 0);

                return hash;
            }
            catch (Exception ex)
            {
                throw new CryptographicException($"Erreur lors du calcul du hachage SHA3: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Calcule le hachage SHA3 des données fournies (méthode statique)
        /// </summary>
        /// <param name="data">Les données à hacher</param>
        /// <param name="variant">Type de SHA3 à utiliser</param>
        /// <returns>Le hachage calculé</returns>
        public static byte[] ComputeHash(byte[] data, SHA3Variant variant = SHA3Variant.SHA3_256)
        {
            var sha3 = new SHA3Wrapper(variant);
            return sha3.ComputeHash(data);
        }

        /// <summary>
        /// Calcule le hachage SHA3-256 des données fournies (méthode statique)
        /// </summary>
        /// <param name="data">Les données à hacher</param>
        /// <returns>Le hachage SHA3-256 calculé (32 bytes)</returns>
        public static byte[] ComputeSHA3_256(byte[] data)
        {
            return ComputeHash(data, SHA3Variant.SHA3_256);
        }

        /// <summary>
        /// Calcule le hachage SHA3-384 des données fournies (méthode statique)
        /// </summary>
        /// <param name="data">Les données à hacher</param>
        /// <returns>Le hachage SHA3-384 calculé (48 bytes)</returns>
        public static byte[] ComputeSHA3_384(byte[] data)
        {
            return ComputeHash(data, SHA3Variant.SHA3_384);
        }

        /// <summary>
        /// Obtient la taille du hachage en bytes
        /// </summary>
        public int HashSize
        {
            get
            {
                return _variant switch
                {
                    SHA3Variant.SHA3_256 => 32,
                    SHA3Variant.SHA3_384 => 48,
                    _ => 32
                };
            }
        }

        /// <summary>
        /// Obtient la taille du hachage en bits
        /// </summary>
        public int HashSizeBits
        {
            get
            {
                return _variant switch
                {
                    SHA3Variant.SHA3_256 => 256,
                    SHA3Variant.SHA3_384 => 384,
                    _ => 256
                };
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
        /// Obtient le variant SHA3 actuel
        /// </summary>
        public SHA3Variant GetVariant()
        {
            return _variant;
        }
    }
}

