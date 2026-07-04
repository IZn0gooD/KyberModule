using System;
using System.Management.Automation;
using KyberLibrary;

namespace KyberModule
{
    /// <summary>
    /// Cmdlet pour chiffrer des données avec ChaCha20-Poly1305 (Authenticated Encryption)
    /// </summary>
    [Cmdlet(VerbsSecurity.Protect, "ChaCha20Poly1305")]
    [OutputType(typeof(ChaCha20Poly1305EncryptionResult))]
    public class ProtectChaCha20Poly1305Command : PSCmdlet
    {
        [Parameter(Mandatory = true, HelpMessage = "Données en clair à chiffrer (byte[] ou chaîne)")]
        [AllowNull]
        public object Plaintext { get; set; }

        [Parameter(Mandatory = false, HelpMessage = "Clé de chiffrement (32 bytes). Si non fournie, générée automatiquement")]
        public object Key { get; set; }

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
                    keyBytes = ChaCha20Poly1305Wrapper.GenerateKey();
                    WriteVerbose("Clé générée automatiquement (32 bytes)");
                }
                else
                {
                    keyBytes = ConvertToByteArray(Key, "Key");
                    if (keyBytes.Length != 32)
                    {
                        throw new ArgumentException("La clé doit faire exactement 32 bytes (256 bits)");
                    }
                }

                // Convertir le nonce
                byte[] nonceBytes;
                if (Nonce == null)
                {
                    nonceBytes = ChaCha20Poly1305Wrapper.GenerateNonce();
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
                var (ciphertextWithTag, nonceUsed) = ChaCha20Poly1305Wrapper.Encrypt(
                    plaintextBytes, 
                    keyBytes, 
                    nonceBytes, 
                    aadBytes);

                // Créer l'objet résultat
                var result = new ChaCha20Poly1305EncryptionResult
                {
                    CiphertextWithTag = ciphertextWithTag,
                    CiphertextWithTagHex = ChaCha20Poly1305Wrapper.BytesToHex(ciphertextWithTag),
                    Key = keyBytes,
                    KeyHex = ChaCha20Poly1305Wrapper.BytesToHex(keyBytes),
                    Nonce = nonceUsed,
                    NonceHex = ChaCha20Poly1305Wrapper.BytesToHex(nonceUsed),
                    PlaintextLength = plaintextBytes.Length,
                    CiphertextLength = ciphertextWithTag.Length
                };

                WriteObject(result);
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(ex, "ChaCha20Poly1305EncryptionError", ErrorCategory.NotSpecified, null));
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
                return ChaCha20Poly1305Wrapper.HexToBytes(hexString);
            }

            throw new ArgumentException($"Le paramètre {parameterName} doit être un byte[] ou une chaîne");
        }
    }

    /// <summary>
    /// Résultat du chiffrement ChaCha20-Poly1305
    /// </summary>
    public class ChaCha20Poly1305EncryptionResult
    {
        public byte[] CiphertextWithTag { get; set; }
        public string CiphertextWithTagHex { get; set; }
        public byte[] Key { get; set; }
        public string KeyHex { get; set; }
        public byte[] Nonce { get; set; }
        public string NonceHex { get; set; }
        public int PlaintextLength { get; set; }
        public int CiphertextLength { get; set; }
    }
}

