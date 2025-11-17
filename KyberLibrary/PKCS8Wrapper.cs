using System;
using System.Security.Cryptography;
using System.IO;
using System.Text;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.Pkcs;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Operators;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Utilities;
using Org.BouncyCastle.Utilities.Encoders;
using Org.BouncyCastle.X509;

namespace KyberLibrary
{
    /// <summary>
    /// Classe wrapper pour l'export/import de clés au format PKCS#8 (PrivateKeyInfo)
    /// PKCS#8 est le standard pour l'encodage des clés privées
    /// Supporte les formats DER (binaire) et PEM (texte)
    /// </summary>
    public class PKCS8Wrapper
    {
        /// <summary>
        /// Exporte une clé privée Kyber au format PKCS#8
        /// </summary>
        /// <param name="privateKey">Clé privée Kyber (bytes)</param>
        /// <param name="mlKemParameters">Paramètres ML-KEM utilisés</param>
        /// <param name="format">Format de sortie (DER ou PEM)</param>
        /// <returns>Clé privée encodée en PKCS#8</returns>
        public static byte[] ExportPrivateKey(byte[] privateKey, Org.BouncyCastle.Crypto.Parameters.MLKemParameters mlKemParameters, PKCSFormat format = PKCSFormat.DER)
        {
            if (privateKey == null)
            {
                throw new ArgumentNullException(nameof(privateKey), "La clé privée ne peut pas être null");
            }

            try
            {
                // Reconstruire la clé privée depuis les bytes
                var privateKeyParams = MLKemPrivateKeyParameters.FromEncoding(mlKemParameters, privateKey);

                // Créer un PrivateKeyInfo PKCS#8
                PrivateKeyInfo privateKeyInfo = PrivateKeyInfoFactory.CreatePrivateKeyInfo(privateKeyParams);

                // Encoder en DER
                byte[] derBytes = privateKeyInfo.GetEncoded();

                if (format == PKCSFormat.PEM)
                {
                    // Convertir en PEM (Base64 avec en-têtes)
                    return ConvertToPEM(derBytes, "PRIVATE KEY");
                }

                return derBytes;
            }
            catch (Exception ex)
            {
                throw new CryptographicException($"Erreur lors de l'export PKCS#8 de la clé privée Kyber: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Exporte une clé privée Dilithium au format PKCS#8
        /// </summary>
        /// <param name="privateKey">Clé privée Dilithium (bytes)</param>
        /// <param name="mlDsaParameters">Paramètres ML-DSA utilisés</param>
        /// <param name="format">Format de sortie (DER ou PEM)</param>
        /// <returns>Clé privée encodée en PKCS#8</returns>
        public static byte[] ExportPrivateKey(byte[] privateKey, Org.BouncyCastle.Crypto.Parameters.MLDsaParameters mlDsaParameters, PKCSFormat format = PKCSFormat.DER)
        {
            if (privateKey == null)
            {
                throw new ArgumentNullException(nameof(privateKey), "La clé privée ne peut pas être null");
            }

            try
            {
                // Reconstruire la clé privée depuis les bytes
                var privateKeyParams = MLDsaPrivateKeyParameters.FromEncoding(mlDsaParameters, privateKey);

                // Créer un PrivateKeyInfo PKCS#8
                PrivateKeyInfo privateKeyInfo = PrivateKeyInfoFactory.CreatePrivateKeyInfo(privateKeyParams);

                // Encoder en DER
                byte[] derBytes = privateKeyInfo.GetEncoded();

                if (format == PKCSFormat.PEM)
                {
                    // Convertir en PEM (Base64 avec en-têtes)
                    return ConvertToPEM(derBytes, "PRIVATE KEY");
                }

                return derBytes;
            }
            catch (Exception ex)
            {
                throw new CryptographicException($"Erreur lors de l'export PKCS#8 de la clé privée Dilithium: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Importe une clé privée depuis un format PKCS#8
        /// </summary>
        /// <param name="pkcs8Data">Données PKCS#8 (DER ou PEM)</param>
        /// <returns>Clé privée décodée (bytes)</returns>
        public static byte[] ImportPrivateKey(byte[] pkcs8Data)
        {
            if (pkcs8Data == null)
            {
                throw new ArgumentNullException(nameof(pkcs8Data), "Les données PKCS#8 ne peuvent pas être null");
            }

            try
            {
                // Détecter si c'est du PEM
                string dataStr = System.Text.Encoding.UTF8.GetString(pkcs8Data);
                byte[] derData = pkcs8Data;

                if (dataStr.Contains("-----BEGIN"))
                {
                    // C'est du PEM, convertir en DER
                    derData = ConvertFromPEM(dataStr);
                }

                // Parser le PrivateKeyInfo
                PrivateKeyInfo privateKeyInfo = PrivateKeyInfo.GetInstance(Asn1Object.FromByteArray(derData));

                // Extraire la clé privée
                AsymmetricKeyParameter keyParameter = PrivateKeyFactory.CreateKey(privateKeyInfo);

                // Obtenir l'encodage de la clé
                if (keyParameter is MLKemPrivateKeyParameters mlKemPrivateKey)
                {
                    return mlKemPrivateKey.GetEncoded();
                }
                else if (keyParameter is MLDsaPrivateKeyParameters mlDsaPrivateKey)
                {
                    return mlDsaPrivateKey.GetEncoded();
                }
                else
                {
                    throw new CryptographicException("Type de clé privée non supporté. Seules les clés Kyber (ML-KEM) et Dilithium (ML-DSA) sont supportées.");
                }
            }
            catch (Exception ex)
            {
                throw new CryptographicException($"Erreur lors de l'import PKCS#8 de la clé privée: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Format de sortie PKCS
        /// </summary>
        public enum PKCSFormat
        {
            DER,  // Format binaire DER (Distinguished Encoding Rules)
            PEM   // Format texte PEM (Privacy-Enhanced Mail) avec Base64
        }

        /// <summary>
        /// Convertit des données DER en format PEM
        /// </summary>
        private static byte[] ConvertToPEM(byte[] derData, string keyType)
        {
            string base64 = Convert.ToBase64String(derData);
            string pem = $"-----BEGIN {keyType}-----\n";
            
            // Ajouter des retours à la ligne tous les 64 caractères (standard PEM)
            for (int i = 0; i < base64.Length; i += 64)
            {
                int length = Math.Min(64, base64.Length - i);
                pem += base64.Substring(i, length) + "\n";
            }
            
            pem += $"-----END {keyType}-----\n";
            return System.Text.Encoding.UTF8.GetBytes(pem);
        }

        /// <summary>
        /// Convertit un format PEM en données DER
        /// </summary>
        private static byte[] ConvertFromPEM(string pemData)
        {
            // Extraire le contenu Base64 entre les marqueurs BEGIN/END
            string[] lines = pemData.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            StringBuilder base64Content = new StringBuilder();

            bool inKeyBlock = false;
            foreach (string line in lines)
            {
                if (line.Contains("-----BEGIN"))
                {
                    inKeyBlock = true;
                    continue;
                }
                if (line.Contains("-----END"))
                {
                    break;
                }
                if (inKeyBlock)
                {
                    base64Content.Append(line.Trim());
                }
            }

            return Convert.FromBase64String(base64Content.ToString());
        }
    }
}

