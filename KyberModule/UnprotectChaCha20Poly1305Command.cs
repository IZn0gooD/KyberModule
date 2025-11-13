using System;
using System.Management.Automation;
using KyberLibrary;

namespace KyberModule
{
    /// <summary>
    /// Cmdlet pour déchiffrer des données avec ChaCha20-Poly1305 (Authenticated Encryption)
    /// </summary>
    [Cmdlet(VerbsSecurity.Unprotect, "ChaCha20Poly1305")]
    [OutputType(typeof(byte[]))]
    public class UnprotectChaCha20Poly1305Command : PSCmdlet
    {
        [Parameter(Mandatory = true, HelpMessage = "Données chiffrées avec tag (ciphertext + tag de 16 bytes)")]
        public object CiphertextWithTag { get; set; }

        [Parameter(Mandatory = true, HelpMessage = "Clé de chiffrement (32 bytes / 256 bits)")]
        public object Key { get; set; }

        [Parameter(Mandatory = true, HelpMessage = "Nonce utilisé lors du chiffrement (12 bytes / 96 bits)")]
        public object Nonce { get; set; }

        [Parameter(Mandatory = false, HelpMessage = "Données associées (AAD) pour l'authentification (optionnel, doit correspondre au chiffrement)")]
        public object AssociatedData { get; set; }

        protected override void ProcessRecord()
        {
            try
            {
                // Convertir les paramètres
                byte[] ciphertextBytes = ConvertToByteArray(CiphertextWithTag, "CiphertextWithTag");
                byte[] keyBytes = ConvertToByteArray(Key, "Key");
                byte[] nonceBytes = ConvertToByteArray(Nonce, "Nonce");

                if (keyBytes.Length != 32)
                {
                    throw new ArgumentException("La clé doit faire exactement 32 bytes (256 bits)");
                }

                if (nonceBytes.Length != 12)
                {
                    throw new ArgumentException("Le nonce doit faire exactement 12 bytes (96 bits)");
                }

                // Convertir les données associées (optionnel)
                byte[] aadBytes = null;
                if (AssociatedData != null)
                {
                    aadBytes = ConvertToByteArray(AssociatedData, "AssociatedData", true);
                }

                // Déchiffrer
                byte[] plaintext = ChaCha20Poly1305Wrapper.Decrypt(
                    ciphertextBytes, 
                    keyBytes, 
                    nonceBytes, 
                    aadBytes);

                WriteObject(plaintext);
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(ex, "ChaCha20Poly1305DecryptionError", ErrorCategory.NotSpecified, null));
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
}

