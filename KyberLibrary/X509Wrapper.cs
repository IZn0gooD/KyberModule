using System;
using System.Collections;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Operators;
using Org.BouncyCastle.Crypto.Signers;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Utilities;
using Org.BouncyCastle.X509;
using Org.BouncyCastle.X509.Extension;

namespace KyberLibrary
{
    /// <summary>
    /// Classe wrapper pour la création et la gestion de certificats X.509
    /// Supporte les certificats auto-signés avec clés post-quantiques (Kyber, Dilithium)
    /// </summary>
    public class X509Wrapper
    {
        /// <summary>
        /// Crée un certificat X.509 auto-signé avec une clé publique Kyber
        /// </summary>
        /// <param name="subjectName">Nom du sujet (ex: "CN=Test Certificate, O=My Organization")</param>
        /// <param name="publicKey">Clé publique Kyber</param>
        /// <param name="privateKey">Clé privée Kyber (pour signer le certificat)</param>
        /// <param name="mlKemParameters">Paramètres ML-KEM utilisés</param>
        /// <param name="validityDays">Nombre de jours de validité du certificat</param>
        /// <param name="format">Format de sortie (DER ou PEM)</param>
        /// <returns>Certificat X.509 encodé</returns>
        public static byte[] CreateSelfSignedCertificate(
            string subjectName,
            byte[] publicKey,
            byte[] privateKey,
            Org.BouncyCastle.Crypto.Parameters.MLKemParameters mlKemParameters,
            int validityDays = 365,
            X509Format format = X509Format.DER)
        {
            if (string.IsNullOrEmpty(subjectName))
            {
                throw new ArgumentException("Le nom du sujet ne peut pas être vide", nameof(subjectName));
            }
            if (publicKey == null)
            {
                throw new ArgumentNullException(nameof(publicKey), "La clé publique ne peut pas être null");
            }
            if (privateKey == null)
            {
                throw new ArgumentNullException(nameof(privateKey), "La clé privée ne peut pas être null");
            }

            try
            {
                // Reconstruire les clés
                var publicKeyParams = MLKemPublicKeyParameters.FromEncoding(mlKemParameters, publicKey);
                var privateKeyParams = MLKemPrivateKeyParameters.FromEncoding(mlKemParameters, privateKey);

                // Créer le générateur de certificat
                var certGenerator = new X509V3CertificateGenerator();

                // Sujet et émetteur (auto-signé, donc identiques)
                X509Name subject = new X509Name(subjectName);
                X509Name issuer = new X509Name(subjectName);

                // Numéro de série aléatoire
                BigInteger serialNumber = BigInteger.ProbablePrime(120, new SecureRandom());

                // Dates de validité
                DateTime notBefore = DateTime.UtcNow;
                DateTime notAfter = notBefore.AddDays(validityDays);

                // Configurer le certificat
                certGenerator.SetSerialNumber(serialNumber);
                certGenerator.SetIssuerDN(issuer);
                certGenerator.SetSubjectDN(subject);
                certGenerator.SetNotBefore(notBefore);
                certGenerator.SetNotAfter(notAfter);
                certGenerator.SetPublicKey(publicKeyParams);

                // Créer le signateur (pour ML-KEM, on utilise MLDsa pour signer le certificat)
                // Note: Pour un certificat Kyber, il faut une signature Dilithium ou Ed25519
                // Ici, on suppose que le certificat est signé avec Dilithium (à fournir séparément)
                // Pour simplifier, on crée un certificat sans signature pour l'instant
                // (Dans un cas réel, il faudrait une clé de signature séparée)

                // Signature algorithm: ML-DSA (Dilithium) - nécessite une clé de signature séparée
                // Pour cet exemple, on crée un certificat auto-signé conceptuel
                // En production, il faudrait utiliser une clé Dilithium pour signer

                // Note: BouncyCastle supporte les certificats post-quantiques mais nécessite
                // des algorithmes de signature compatibles pour signer le certificat lui-même

                // Pour l'instant, retourner un certificat basique
                // (Dans une implémentation complète, il faudrait utiliser un algorithme de signature)
                
                // Créer un certificat avec signature Dilithium (nécessite une clé Dilithium)
                // Pour simplifier, on retourne un certificat non signé pour l'instant
                throw new NotImplementedException("La création de certificats X.509 avec clés post-quantiques nécessite une clé de signature Dilithium. Cette fonctionnalité sera implémentée dans une version future.");
            }
            catch (Exception ex)
            {
                throw new CryptographicException($"Erreur lors de la création du certificat X.509: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Crée un certificat X.509 auto-signé avec une clé publique Dilithium
        /// Le certificat est signé avec la clé privée Dilithium correspondante
        /// </summary>
        /// <param name="subjectName">Nom du sujet</param>
        /// <param name="publicKey">Clé publique Dilithium</param>
        /// <param name="privateKey">Clé privée Dilithium (pour signer le certificat)</param>
        /// <param name="mlDsaParameters">Paramètres ML-DSA utilisés</param>
        /// <param name="validityDays">Nombre de jours de validité</param>
        /// <param name="format">Format de sortie (DER ou PEM)</param>
        /// <returns>Certificat X.509 encodé</returns>
        public static byte[] CreateSelfSignedCertificate(
            string subjectName,
            byte[] publicKey,
            byte[] privateKey,
            Org.BouncyCastle.Crypto.Parameters.MLDsaParameters mlDsaParameters,
            int validityDays = 365,
            X509Format format = X509Format.DER)
        {
            if (string.IsNullOrEmpty(subjectName))
            {
                throw new ArgumentException("Le nom du sujet ne peut pas être vide", nameof(subjectName));
            }
            if (publicKey == null)
            {
                throw new ArgumentNullException(nameof(publicKey), "La clé publique ne peut pas être null");
            }
            if (privateKey == null)
            {
                throw new ArgumentNullException(nameof(privateKey), "La clé privée ne peut pas être null");
            }

            try
            {
                // Reconstruire les clés
                var publicKeyParams = MLDsaPublicKeyParameters.FromEncoding(mlDsaParameters, publicKey);
                var privateKeyParams = MLDsaPrivateKeyParameters.FromEncoding(mlDsaParameters, privateKey);

                // Créer le générateur de certificat
                var certGenerator = new X509V3CertificateGenerator();

                // Sujet et émetteur (auto-signé)
                X509Name subject = new X509Name(subjectName);
                X509Name issuer = new X509Name(subjectName);

                // Numéro de série aléatoire
                BigInteger serialNumber = BigInteger.ProbablePrime(120, new SecureRandom());

                // Dates de validité
                DateTime notBefore = DateTime.UtcNow;
                DateTime notAfter = notBefore.AddDays(validityDays);

                // Configurer le certificat
                certGenerator.SetSerialNumber(serialNumber);
                certGenerator.SetIssuerDN(issuer);
                certGenerator.SetSubjectDN(subject);
                certGenerator.SetNotBefore(notBefore);
                certGenerator.SetNotAfter(notAfter);
                certGenerator.SetPublicKey(publicKeyParams);

                // Créer le signateur ML-DSA personnalisé
                // Utiliser un ContentSigner personnalisé avec MLDsaSigner
                MLDsaContentSigner contentSigner = new MLDsaContentSigner(mlDsaParameters, privateKeyParams);
                ISignatureFactory signatureFactory = new SignatureFactory(contentSigner);

                // Générer le certificat
                Org.BouncyCastle.X509.X509Certificate certificate = certGenerator.Generate(signatureFactory);

                // Encoder en DER
                byte[] derBytes = certificate.GetEncoded();

                if (format == X509Format.PEM)
                {
                    // Convertir en PEM
                    return ConvertToPEM(derBytes, "CERTIFICATE");
                }

                return derBytes;
            }
            catch (Exception ex)
            {
                throw new CryptographicException($"Erreur lors de la création du certificat X.509: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Vérifie un certificat X.509
        /// </summary>
        /// <param name="certificateData">Données du certificat (DER ou PEM)</param>
        /// <returns>True si le certificat est valide, false sinon</returns>
        public static bool VerifyCertificate(byte[] certificateData)
        {
            if (certificateData == null)
            {
                throw new ArgumentNullException(nameof(certificateData), "Les données du certificat ne peuvent pas être null");
            }

            try
            {
                // Détecter si c'est du PEM
                string dataStr = System.Text.Encoding.UTF8.GetString(certificateData);
                byte[] derData = certificateData;

                if (dataStr.Contains("-----BEGIN"))
                {
                    derData = ConvertFromPEM(dataStr);
                }

                // Parser le certificat
                Org.BouncyCastle.X509.X509Certificate certificate = new X509CertificateParser().ReadCertificate(derData);

                // Vérifier la validité temporelle
                DateTime now = DateTime.UtcNow;
                if (now < certificate.NotBefore || now > certificate.NotAfter)
                {
                    return false;
                }

                // Vérifier la signature (nécessite la clé publique de l'émetteur)
                // Pour un certificat auto-signé, utiliser la clé publique du certificat
                try
                {
                    AsymmetricKeyParameter publicKey = certificate.GetPublicKey();
                    
                    // Vérifier si c'est une clé ML-DSA
                    if (publicKey is MLDsaPublicKeyParameters mlDsaPublicKey)
                    {
                        // Vérification personnalisée avec MLDsaSigner
                        return VerifyMLDsaCertificate(certificate, mlDsaPublicKey);
                    }
                    else
                    {
                        // Pour les autres types de clés, utiliser la méthode standard
                        certificate.Verify(publicKey);
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    // Logger l'exception pour le débogage
                    System.Diagnostics.Debug.WriteLine($"Erreur de vérification de certificat: {ex.Message}");
                    return false;
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Format de sortie X.509
        /// </summary>
        public enum X509Format
        {
            DER,  // Format binaire DER
            PEM   // Format texte PEM
        }

        /// <summary>
        /// Vérifie un certificat X.509 signé avec ML-DSA
        /// </summary>
        private static bool VerifyMLDsaCertificate(Org.BouncyCastle.X509.X509Certificate certificate, MLDsaPublicKeyParameters publicKey)
        {
            try
            {
                // Obtenir les paramètres ML-DSA de la clé publique
                MLDsaParameters mlDsaParameters = publicKey.Parameters;

                // Extraire les données TBS (To Be Signed) du certificat
                // Le certificat X.509 contient: TBSCertificate + SignatureAlgorithm + Signature
                // Nous devons obtenir les données TBS pour vérifier la signature
                byte[] tbsCertificate;
                
                // Essayer d'utiliser GetTbsCertificate() si disponible
                try
                {
                    tbsCertificate = certificate.GetTbsCertificate();
                }
                catch
                {
                    // Alternative: extraire depuis la structure ASN.1
                    // Le certificat complet est encodé en DER, nous devons extraire le TBS
                    // Structure: Certificate ::= SEQUENCE { tbsCertificate TBSCertificate, signatureAlgorithm AlgorithmIdentifier, signature BIT STRING }
                    byte[] certBytes = certificate.GetEncoded();
                    
                    // Parser ASN.1 pour extraire le TBS
                    using (Asn1InputStream asn1In = new Asn1InputStream(certBytes))
                    {
                        Asn1Sequence certSeq = (Asn1Sequence)asn1In.ReadObject();
                        
                        // Le premier élément est le TBSCertificate
                        Asn1Encodable tbsCert = certSeq[0];
                        tbsCertificate = tbsCert.GetEncoded();
                    }
                }
                
                // Extraire la signature du certificat
                byte[] signature = certificate.GetSignature();

                // Créer un vérificateur ML-DSA
                MLDsaSigner verifier = new MLDsaSigner(mlDsaParameters, false); // false = pas de pre-hash (comme lors de la création)
                verifier.Init(false, publicKey); // false = mode vérification

                // Vérifier la signature
                verifier.BlockUpdate(tbsCertificate, 0, tbsCertificate.Length);
                bool isValid = verifier.VerifySignature(signature);

                return isValid;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors de la vérification ML-DSA: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Obtient le nom d'algorithme ML-DSA pour les paramètres donnés
        /// </summary>
        private static string GetMLDsaAlgorithmName(Org.BouncyCastle.Crypto.Parameters.MLDsaParameters mlDsaParameters)
        {
            // Les identifiants d'algorithmes ML-DSA selon NIST
            if (mlDsaParameters == Org.BouncyCastle.Crypto.Parameters.MLDsaParameters.ml_dsa_44)
            {
                return "MLDSA44"; // ML-DSA-44 (NIST Level 2)
            }
            else if (mlDsaParameters == Org.BouncyCastle.Crypto.Parameters.MLDsaParameters.ml_dsa_65)
            {
                return "MLDSA65"; // ML-DSA-65 (NIST Level 3)
            }
            else if (mlDsaParameters == Org.BouncyCastle.Crypto.Parameters.MLDsaParameters.ml_dsa_87)
            {
                return "MLDSA87"; // ML-DSA-87 (NIST Level 5)
            }
            else
            {
                return "MLDSA65"; // Par défaut
            }
        }

        /// <summary>
        /// Convertit des données DER en format PEM
        /// </summary>
        private static byte[] ConvertToPEM(byte[] derData, string certType)
        {
            string base64 = Convert.ToBase64String(derData);
            string pem = $"-----BEGIN {certType}-----\n";
            
            for (int i = 0; i < base64.Length; i += 64)
            {
                int length = Math.Min(64, base64.Length - i);
                pem += base64.Substring(i, length) + "\n";
            }
            
            pem += $"-----END {certType}-----\n";
            return System.Text.Encoding.UTF8.GetBytes(pem);
        }

        /// <summary>
        /// Convertit un format PEM en données DER
        /// </summary>
        private static byte[] ConvertFromPEM(string pemData)
        {
            string[] lines = pemData.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            StringBuilder base64Content = new StringBuilder();

            bool inCertBlock = false;
            foreach (string line in lines)
            {
                if (line.Contains("-----BEGIN"))
                {
                    inCertBlock = true;
                    continue;
                }
                if (line.Contains("-----END"))
                {
                    break;
                }
                if (inCertBlock)
                {
                    base64Content.Append(line.Trim());
                }
            }

            return Convert.FromBase64String(base64Content.ToString());
        }

        /// <summary>
        /// ContentSigner personnalisé pour ML-DSA utilisant MLDsaSigner
        /// </summary>
        private class MLDsaContentSigner : IStreamCalculator<IBlockResult>
        {
            private readonly MLDsaSigner _signer;
            private readonly MemoryStream _stream = new MemoryStream();
            private readonly MLDsaParameters _mlDsaParameters;

            public MLDsaContentSigner(MLDsaParameters mlDsaParameters, MLDsaPrivateKeyParameters privateKey)
            {
                _mlDsaParameters = mlDsaParameters;
                _signer = new MLDsaSigner(mlDsaParameters, false); // false = pas de pre-hash
                _signer.Init(true, privateKey); // true = mode signature
            }

            public Stream Stream => _stream;

            public IBlockResult GetResult()
            {
                byte[] data = _stream.ToArray();
                _signer.BlockUpdate(data, 0, data.Length);
                byte[] signature = _signer.GenerateSignature();
                return new SimpleBlockResult(signature);
            }
        }

        /// <summary>
        /// Résultat de signature simple implémentant IBlockResult
        /// </summary>
        private class SimpleBlockResult : IBlockResult
        {
            private readonly byte[] _signature;

            public SimpleBlockResult(byte[] signature)
            {
                _signature = signature;
            }

            public byte[] Collect()
            {
                return _signature;
            }

            public int Collect(byte[] buf, int off)
            {
                Array.Copy(_signature, 0, buf, off, _signature.Length);
                return _signature.Length;
            }

            public int GetMaxResultLength()
            {
                return _signature.Length;
            }
        }

        /// <summary>
        /// SignatureFactory personnalisée pour ML-DSA
        /// </summary>
        private class SignatureFactory : ISignatureFactory
        {
            private readonly MLDsaContentSigner _contentSigner;

            public SignatureFactory(MLDsaContentSigner contentSigner)
            {
                _contentSigner = contentSigner;
            }

            public object AlgorithmDetails
            {
                get
                {
                    // Retourner un AlgorithmIdentifier avec l'OID ML-DSA
                    // Pour l'instant, utiliser un OID générique (à adapter selon les standards)
                    // Les OIDs ML-DSA sont en cours de standardisation
                    DerObjectIdentifier oid = new DerObjectIdentifier("1.3.6.1.4.1.2.267.12.7.5"); // OID temporaire pour ML-DSA-65
                    return new AlgorithmIdentifier(oid);
                }
            }

            public IStreamCalculator<IBlockResult> CreateCalculator()
            {
                return _contentSigner;
            }
        }
    }
}

