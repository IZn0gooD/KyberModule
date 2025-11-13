using System;
using System.Management.Automation;
using KyberLibrary;

namespace KyberModule
{
    /// <summary>
    /// Cmdlet pour chiffrer des données avec AES-GCM (Authenticated Encryption)
    /// </summary>
    [Cmdlet(VerbsSecurity.Protect, "AESGCM")]
    [OutputType(typeof(AESGCMEncryptionResult))]
    public class ProtectAESGCMCommand : PSCmdlet
    {
        [Parameter(Mandatory = true, HelpMessage = "Données en clair à chiffrer (byte[] ou chaîne)")]
        [AllowNull]
        public object Plaintext { get; set; }

        [Parameter(Mandatory = false, HelpMessage = "Clé de chiffrement (16, 24, ou 32 bytes pour AES-128/192/256). Si non fournie, générée automatiquement (AES-256)")]
        public object Key { get; set; }

        [Parameter(Mandatory = false, HelpMessage = "Taille de la clé (AES128, AES192, AES256). Par défaut: AES256")]
        [ValidateSet("AES128", "AES192", "AES256")]
        public string KeySize { get; set; } = "AES256";

        [Parameter(Mandatory = false, HelpMessage = "Nonce (12 bytes). Si non fourni, généré automatiquement")]
        public object Nonce { get; set; }

        [Parameter(Mandatory = false, HelpMessage = "Données associées (AAD) pour l'authentification (optionnel)")]
        public object AssociatedData { get; set; }

        protected override void ProcessRecord()
        {
            try
            {
                // Convertir le plaintext
                byte[] plaintextBytes = ConvertToByteArray(Plaintext, "Plaintext", true);

                // Convertir la clé
                byte[] keyBytes;
                if (Key == null)
                {
                    AESGCMWrapper.AESKeySize keySizeEnum = KeySize.ToUpper() switch
                    {
                        "AES128" => AESGCMWrapper.AESKeySize.AES128,
                        "AES192" => AESGCMWrapper.AESKeySize.AES192,
                        _ => AESGCMWrapper.AESKeySize.AES256
                    };
                    keyBytes = AESGCMWrapper.GenerateKey(keySizeEnum);
                    WriteVerbose($"Clé générée automatiquement ({keyBytes.Length} bytes pour {KeySize})");
                }
                else
                {
                    keyBytes = ConvertToByteArray(Key, "Key");
                    if (keyBytes.Length != 16 && keyBytes.Length != 24 && keyBytes.Length != 32)
                    {
                        throw new ArgumentException("La clé doit faire 16, 24 ou 32 bytes (AES-128, AES-192, ou AES-256)");
                    }
                }

                // Convertir le nonce
                byte[] nonceBytes;
                if (Nonce == null)
                {
                    nonceBytes = AESGCMWrapper.GenerateNonce();
                    WriteVerbose("Nonce généré automatiquement (12 bytes)");
                }
                else
                {
                    nonceBytes = ConvertToByteArray(Nonce, "Nonce");
                    if (nonceBytes.Length != 12)
                    {
                        throw new ArgumentException("Le nonce doit faire exactement 12 bytes (96 bits)");
                    }
                }

                // Convertir les données associées (optionnel)
                byte[] aadBytes = null;
                if (AssociatedData != null)
                {
                    aadBytes = ConvertToByteArray(AssociatedData, "AssociatedData", true);
                }

                // Chiffrer
                var (ciphertextWithTag, nonceUsed) = AESGCMWrapper.Encrypt(
                    plaintextBytes, 
                    keyBytes, 
                    nonceBytes, 
                    aadBytes);

                // Créer l'objet résultat
                var result = new AESGCMEncryptionResult
                {
                    CiphertextWithTag = ciphertextWithTag,
                    CiphertextWithTagHex = AESGCMWrapper.BytesToHex(ciphertextWithTag),
                    Key = keyBytes,
                    KeyHex = AESGCMWrapper.BytesToHex(keyBytes),
                    Nonce = nonceUsed,
                    NonceHex = AESGCMWrapper.BytesToHex(nonceUsed),
                    PlaintextLength = plaintextBytes.Length,
                    CiphertextLength = ciphertextWithTag.Length,
                    KeySize = keyBytes.Length * 8
                };

                WriteObject(result);
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(ex, "AESGCMEncryptionError", ErrorCategory.NotSpecified, null));
            }
        }

        private byte[] ConvertToByteArray(object value, string parameterName, bool allowString = false)
        {
            if (value == null && allowString)
            {
                return null;
            }

            if (value == null)
            {
                throw new ArgumentNullException(parameterName);
            }

            if (value is byte[] bytes)
            {
                return bytes;
            }

            if (value is string str && allowString)
            {
                return System.Text.Encoding.UTF8.GetBytes(str);
            }

            if (value is string hexString && !allowString)
            {
                return AESGCMWrapper.HexToBytes(hexString);
            }

            throw new ArgumentException($"Le paramètre {parameterName} doit être un byte[] ou une chaîne");
        }
    }

    /// <summary>
    /// Résultat du chiffrement AES-GCM
    /// </summary>
    public class AESGCMEncryptionResult
    {
        public byte[] CiphertextWithTag { get; set; }
        public string CiphertextWithTagHex { get; set; }
        public byte[] Key { get; set; }
        public string KeyHex { get; set; }
        public byte[] Nonce { get; set; }
        public string NonceHex { get; set; }
        public int PlaintextLength { get; set; }
        public int CiphertextLength { get; set; }
        public int KeySize { get; set; }
    }
}

