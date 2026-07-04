using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.Cms;
using Org.BouncyCastle.Asn1.Pkcs;
using Org.BouncyCastle.Cms;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;
using Org.BouncyCastle.X509.Store;

namespace KyberLibrary
{
    /// <summary>
    /// Classe wrapper pour CMS (Cryptographic Message Syntax)
    /// Supporte les enveloppes chiffrées (EnvelopedData) et les messages signés (SignedData)
    /// </summary>
    public class CMSWrapper
    {
        /// <summary>
        /// Crée une enveloppe CMS chiffrée (EnvelopedData)
        /// Chiffre les données avec une clé partagée Kyber
        /// </summary>
        /// <param name="data">Données à chiffrer</param>
        /// <param name="sharedSecret">Clé partagée (générée via Kyber)</param>
        /// <param name="algorithm">Algorithme de chiffrement symétrique (AES-GCM ou ChaCha20-Poly1305)</param>
        /// <returns>Enveloppe CMS encodée</returns>
        public static byte[] CreateEnvelopedData(byte[] data, byte[] sharedSecret, string algorithm = "AES-GCM")
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data), "Les données ne peuvent pas être null");
            }
            if (sharedSecret == null)
            {
                throw new ArgumentNullException(nameof(sharedSecret), "La clé partagée ne peut pas être null");
            }

            try
            {
                // Note: CMS EnvelopedData nécessite normalement des certificats X.509
                // pour le chiffrement de la clé de contenu (Key Encryption Key)
                // Pour simplifier et utiliser directement la clé partagée Kyber,
                // on peut créer une enveloppe CMS simplifiée ou utiliser un format hybride
                
                // Pour l'instant, on crée une structure CMS basique
                // Dans une implémentation complète, on utiliserait:
                // - KeyTransRecipientInfo avec certificat Kyber
                // - KeyAgreementRecipientInfo pour KEM
                
                // Version simplifiée: on chiffre les données avec la clé partagée
                // et on encapsule dans une structure CMS
                
                // Utiliser AES-GCM pour chiffrer les données
                byte[] encryptedData;
                byte[] nonce;
                
                if (algorithm == "AES-GCM")
                {
                    // Générer un nonce pour AES-GCM
                    nonce = new byte[12];
                    new SecureRandom().NextBytes(nonce);
                    
                    // Chiffrer avec AES-GCM (utiliser la clé partagée comme clé AES)
                    // Note: Dans une implémentation complète, on dériverait une clé AES
                    // de la clé partagée Kyber avec HKDF
                    byte[] aesKey = DeriveKey(sharedSecret, 32); // 32 bytes = AES-256
                    var result = AESGCMWrapper.Encrypt(data, aesKey, nonce);
                    encryptedData = result.CiphertextWithTag;
                }
                else
                {
                    throw new ArgumentException("Algorithme non supporté. Utilisez 'AES-GCM' ou 'ChaCha20-Poly1305'", nameof(algorithm));
                }

                // Créer une structure CMS simplifiée
                // Note: Pour une implémentation complète conforme CMS, il faudrait utiliser
                // les classes BouncyCastle CmsEnvelopedDataGenerator
                
                // Version simplifiée pour démonstration
                // Structure: [Version][Algorithm][EncryptedData][Nonce]
                var cmsData = new CMSEnvelopedDataSimple
                {
                    Version = 0,
                    Algorithm = algorithm,
                    EncryptedData = encryptedData,
                    Nonce = nonce
                };

                // Encoder en ASN.1 (simplifié)
                // Dans une implémentation complète, utiliser EnvelopedData ASN.1
                return EncodeCMSEnvelopedData(cmsData);
            }
            catch (Exception ex)
            {
                throw new CryptographicException($"Erreur lors de la création de l'enveloppe CMS: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Crée un message CMS signé (SignedData)
        /// Signe les données avec Dilithium
        /// </summary>
        /// <param name="data">Données à signer</param>
        /// <param name="privateKey">Clé privée Dilithium</param>
        /// <param name="publicKey">Clé publique Dilithium (pour inclusion dans le message)</param>
        /// <param name="mlDsaParameters">Paramètres ML-DSA utilisés</param>
        /// <param name="preHash">Si true, pré-hache les données avec SHA3-256 (recommandé pour ML-DSA)</param>
        /// <returns>Message CMS signé encodé</returns>
        public static byte[] CreateSignedData(
            byte[] data,
            byte[] privateKey,
            byte[] publicKey,
            Org.BouncyCastle.Crypto.Parameters.MLDsaParameters mlDsaParameters,
            bool preHash = true)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data), "Les données ne peuvent pas être null");
            }
            if (privateKey == null)
            {
                throw new ArgumentNullException(nameof(privateKey), "La clé privée ne peut pas être null");
            }
            if (publicKey == null)
            {
                throw new ArgumentNullException(nameof(publicKey), "La clé publique ne peut pas être null");
            }

            try
            {
                // Utiliser DilithiumWrapper pour signer les données
                var dilithiumWrapper = CreateDilithiumWrapper(mlDsaParameters);
                byte[] signature = dilithiumWrapper.Sign(data, privateKey, preHash);

                // Créer une structure CMS SignedData simplifiée
                // Format: [Version][Algorithm][DataLength][Data][PublicKeyLength][PublicKey][SignatureLength][Signature]
                var cmsSigned = new CMSSignedDataSimple
                {
                    Version = 0,
                    Algorithm = GetMLDsaAlgorithmName(mlDsaParameters),
                    Data = data,
                    PublicKey = publicKey,
                    Signature = signature,
                    PreHash = preHash
                };

                return EncodeCMSSignedData(cmsSigned);
            }
            catch (Exception ex)
            {
                throw new CryptographicException($"Erreur lors de la création du message CMS signé: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Crée un wrapper Dilithium pour les paramètres ML-DSA spécifiés
        /// </summary>
        private static DilithiumWrapper CreateDilithiumWrapper(Org.BouncyCastle.Crypto.Parameters.MLDsaParameters mlDsaParameters)
        {
            if (mlDsaParameters == Org.BouncyCastle.Crypto.Parameters.MLDsaParameters.ml_dsa_44)
            {
                return new DilithiumWrapper(DilithiumWrapper.DilithiumParameterSet.Dilithium2);
            }
            else if (mlDsaParameters == Org.BouncyCastle.Crypto.Parameters.MLDsaParameters.ml_dsa_65)
            {
                return new DilithiumWrapper(DilithiumWrapper.DilithiumParameterSet.Dilithium3);
            }
            else if (mlDsaParameters == Org.BouncyCastle.Crypto.Parameters.MLDsaParameters.ml_dsa_87)
            {
                return new DilithiumWrapper(DilithiumWrapper.DilithiumParameterSet.Dilithium5);
            }
            else
            {
                return new DilithiumWrapper(DilithiumWrapper.DilithiumParameterSet.Dilithium3);
            }
        }

        /// <summary>
        /// Vérifie un message CMS signé
        /// </summary>
        /// <param name="cmsData">Message CMS signé</param>
        /// <param name="publicKey">Clé publique pour vérification (optionnel, peut être extraite du message CMS)</param>
        /// <param name="mlDsaParameters">Paramètres ML-DSA utilisés (optionnel, peut être détecté)</param>
        /// <returns>True si la signature est valide, false sinon</returns>
        public static bool VerifySignedData(byte[] cmsData, byte[] publicKey = null, Org.BouncyCastle.Crypto.Parameters.MLDsaParameters? mlDsaParameters = null)
        {
            if (cmsData == null)
            {
                throw new ArgumentNullException(nameof(cmsData), "Les données CMS ne peuvent pas être null");
            }

            try
            {
                // Décoder la structure CMS SignedData
                var cmsSigned = DecodeCMSSignedData(cmsData);

                // Utiliser la clé publique du message si aucune n'est fournie
                byte[] keyToUse = publicKey ?? cmsSigned.PublicKey;

                // Détecter les paramètres ML-DSA si non fournis
                Org.BouncyCastle.Crypto.Parameters.MLDsaParameters parameters = mlDsaParameters ?? 
                    (cmsSigned.Algorithm == "MLDSA44" ? Org.BouncyCastle.Crypto.Parameters.MLDsaParameters.ml_dsa_44 :
                     cmsSigned.Algorithm == "MLDSA65" ? Org.BouncyCastle.Crypto.Parameters.MLDsaParameters.ml_dsa_65 :
                     Org.BouncyCastle.Crypto.Parameters.MLDsaParameters.ml_dsa_87);

                // Utiliser DilithiumWrapper pour vérifier la signature
                var dilithiumWrapper = CreateDilithiumWrapper(parameters);
                return dilithiumWrapper.Verify(cmsSigned.Data, cmsSigned.Signature, keyToUse, cmsSigned.PreHash);
            }
            catch (Exception ex)
            {
                throw new CryptographicException($"Erreur lors de la vérification du message CMS signé: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Déchiffre une enveloppe CMS
        /// </summary>
        /// <param name="cmsData">Enveloppe CMS chiffrée</param>
        /// <param name="sharedSecret">Clé partagée (générée via Kyber)</param>
        /// <returns>Données déchiffrées</returns>
        public static byte[] DecryptEnvelopedData(byte[] cmsData, byte[] sharedSecret)
        {
            if (cmsData == null)
            {
                throw new ArgumentNullException(nameof(cmsData), "Les données CMS ne peuvent pas être null");
            }
            if (sharedSecret == null)
            {
                throw new ArgumentNullException(nameof(sharedSecret), "La clé partagée ne peut pas être null");
            }

            try
            {
                // Décoder la structure CMS simplifiée
                var cmsEnveloped = DecodeCMSEnvelopedData(cmsData);

                // Déchiffrer les données
                byte[] aesKey = DeriveKey(sharedSecret, 32);
                
                if (cmsEnveloped.Algorithm == "AES-GCM")
                {
                    return AESGCMWrapper.Decrypt(cmsEnveloped.EncryptedData, aesKey, cmsEnveloped.Nonce);
                }
                else
                {
                    throw new CryptographicException($"Algorithme non supporté: {cmsEnveloped.Algorithm}");
                }
            }
            catch (Exception ex)
            {
                throw new CryptographicException($"Erreur lors du déchiffrement de l'enveloppe CMS: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Structure simplifiée pour CMS EnvelopedData
        /// </summary>
        private class CMSEnvelopedDataSimple
        {
            public int Version { get; set; }
            public string Algorithm { get; set; }
            public byte[] EncryptedData { get; set; }
            public byte[] Nonce { get; set; }
        }

        /// <summary>
        /// Structure simplifiée pour CMS SignedData
        /// </summary>
        private class CMSSignedDataSimple
        {
            public int Version { get; set; }
            public string Algorithm { get; set; }
            public byte[] Data { get; set; }
            public byte[] PublicKey { get; set; }
            public byte[] Signature { get; set; }
            public bool PreHash { get; set; }
        }

        /// <summary>
        /// Encode une structure CMS EnvelopedData simplifiée
        /// </summary>
        private static byte[] EncodeCMSEnvelopedData(CMSEnvelopedDataSimple data)
        {
            // Version simplifiée: encoder en format simple
            // Dans une implémentation complète, utiliser EnvelopedData ASN.1
            using (var ms = new System.IO.MemoryStream())
            {
                ms.Write(BitConverter.GetBytes(data.Version), 0, 4);
                byte[] algorithmBytes = Encoding.UTF8.GetBytes(data.Algorithm);
                ms.Write(BitConverter.GetBytes(algorithmBytes.Length), 0, 4);
                ms.Write(algorithmBytes, 0, algorithmBytes.Length);
                ms.Write(BitConverter.GetBytes(data.EncryptedData.Length), 0, 4);
                ms.Write(data.EncryptedData, 0, data.EncryptedData.Length);
                ms.Write(BitConverter.GetBytes(data.Nonce.Length), 0, 4);
                ms.Write(data.Nonce, 0, data.Nonce.Length);
                return ms.ToArray();
            }
        }

        /// <summary>
        /// Décode une structure CMS EnvelopedData simplifiée
        /// </summary>
        private static CMSEnvelopedDataSimple DecodeCMSEnvelopedData(byte[] data)
        {
            using (var ms = new System.IO.MemoryStream(data))
            {
                byte[] versionBytes = new byte[4];
                ms.Read(versionBytes, 0, 4);
                int version = BitConverter.ToInt32(versionBytes, 0);

                byte[] lengthBytes = new byte[4];
                ms.Read(lengthBytes, 0, 4);
                int algorithmLength = BitConverter.ToInt32(lengthBytes, 0);
                byte[] algorithmBytes = new byte[algorithmLength];
                ms.Read(algorithmBytes, 0, algorithmLength);
                string algorithm = Encoding.UTF8.GetString(algorithmBytes);

                ms.Read(lengthBytes, 0, 4);
                int encryptedLength = BitConverter.ToInt32(lengthBytes, 0);
                byte[] encryptedData = new byte[encryptedLength];
                ms.Read(encryptedData, 0, encryptedLength);

                ms.Read(lengthBytes, 0, 4);
                int nonceLength = BitConverter.ToInt32(lengthBytes, 0);
                byte[] nonce = new byte[nonceLength];
                ms.Read(nonce, 0, nonceLength);

                return new CMSEnvelopedDataSimple
                {
                    Version = version,
                    Algorithm = algorithm,
                    EncryptedData = encryptedData,
                    Nonce = nonce
                };
            }
        }

        /// <summary>
        /// Encode une structure CMS SignedData simplifiée
        /// </summary>
        private static byte[] EncodeCMSSignedData(CMSSignedDataSimple data)
        {
            using (var ms = new System.IO.MemoryStream())
            {
                ms.Write(BitConverter.GetBytes(data.Version), 0, 4);
                
                byte[] algorithmBytes = Encoding.UTF8.GetBytes(data.Algorithm);
                ms.Write(BitConverter.GetBytes(algorithmBytes.Length), 0, 4);
                ms.Write(algorithmBytes, 0, algorithmBytes.Length);
                
                ms.Write(BitConverter.GetBytes(data.Data.Length), 0, 4);
                ms.Write(data.Data, 0, data.Data.Length);
                
                ms.Write(BitConverter.GetBytes(data.PublicKey.Length), 0, 4);
                ms.Write(data.PublicKey, 0, data.PublicKey.Length);
                
                ms.Write(BitConverter.GetBytes(data.Signature.Length), 0, 4);
                ms.Write(data.Signature, 0, data.Signature.Length);
                
                ms.Write(BitConverter.GetBytes(data.PreHash ? 1 : 0), 0, 4);
                
                return ms.ToArray();
            }
        }

        /// <summary>
        /// Décode une structure CMS SignedData simplifiée
        /// </summary>
        private static CMSSignedDataSimple DecodeCMSSignedData(byte[] data)
        {
            using (var ms = new System.IO.MemoryStream(data))
            {
                byte[] versionBytes = new byte[4];
                ms.Read(versionBytes, 0, 4);
                int version = BitConverter.ToInt32(versionBytes, 0);

                byte[] lengthBytes = new byte[4];
                ms.Read(lengthBytes, 0, 4);
                int algorithmLength = BitConverter.ToInt32(lengthBytes, 0);
                byte[] algorithmBytes = new byte[algorithmLength];
                ms.Read(algorithmBytes, 0, algorithmLength);
                string algorithm = Encoding.UTF8.GetString(algorithmBytes);

                ms.Read(lengthBytes, 0, 4);
                int dataLength = BitConverter.ToInt32(lengthBytes, 0);
                byte[] messageData = new byte[dataLength];
                ms.Read(messageData, 0, dataLength);

                ms.Read(lengthBytes, 0, 4);
                int publicKeyLength = BitConverter.ToInt32(lengthBytes, 0);
                byte[] publicKey = new byte[publicKeyLength];
                ms.Read(publicKey, 0, publicKeyLength);

                ms.Read(lengthBytes, 0, 4);
                int signatureLength = BitConverter.ToInt32(lengthBytes, 0);
                byte[] signature = new byte[signatureLength];
                ms.Read(signature, 0, signatureLength);

                ms.Read(lengthBytes, 0, 4);
                bool preHash = BitConverter.ToInt32(lengthBytes, 0) == 1;

                return new CMSSignedDataSimple
                {
                    Version = version,
                    Algorithm = algorithm,
                    Data = messageData,
                    PublicKey = publicKey,
                    Signature = signature,
                    PreHash = preHash
                };
            }
        }

        /// <summary>
        /// Déduit une clé de la clé partagée Kyber (HKDF simplifié)
        /// </summary>
        private static byte[] DeriveKey(byte[] sharedSecret, int keyLength)
        {
            // Utiliser SHA-256 pour dériver une clé de la longueur souhaitée
            // Note: Dans une implémentation complète, utiliser HKDF (RFC 5869)
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                if (keyLength <= 32)
                {
                    return sha256.ComputeHash(sharedSecret).Take(keyLength).ToArray();
                }
                else
                {
                    // Pour des clés plus longues, concaténer plusieurs hachages
                    var key = new List<byte>();
                    int iterations = (keyLength + 31) / 32;
                    for (int i = 0; i < iterations; i++)
                    {
                        var hash = sha256.ComputeHash(sharedSecret.Concat(BitConverter.GetBytes(i)).ToArray());
                        key.AddRange(hash);
                    }
                    return key.Take(keyLength).ToArray();
                }
            }
        }

        /// <summary>
        /// Obtient le nom d'algorithme ML-DSA
        /// </summary>
        private static string GetMLDsaAlgorithmName(Org.BouncyCastle.Crypto.Parameters.MLDsaParameters mlDsaParameters)
        {
            if (mlDsaParameters == Org.BouncyCastle.Crypto.Parameters.MLDsaParameters.ml_dsa_44)
            {
                return "MLDSA44";
            }
            else if (mlDsaParameters == Org.BouncyCastle.Crypto.Parameters.MLDsaParameters.ml_dsa_65)
            {
                return "MLDSA65";
            }
            else if (mlDsaParameters == Org.BouncyCastle.Crypto.Parameters.MLDsaParameters.ml_dsa_87)
            {
                return "MLDSA87";
            }
            else
            {
                return "MLDSA65";
            }
        }
    }
}

