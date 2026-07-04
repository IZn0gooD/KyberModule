using System;
using System.Management.Automation;
using KyberLibrary;

namespace KyberModule
{
    /// <summary>
    /// Cmdlet pour déchiffrer des données avec AES-GCM (Authenticated Encryption)
    /// </summary>
    [Cmdlet(VerbsSecurity.Unprotect, "AESGCM")]
    [OutputType(typeof(byte[]))]
    public class UnprotectAESGCMCommand : PSCmdlet
    {
        [Parameter(Mandatory = true, HelpMessage = "Données chiffrées avec tag (ciphertext + tag de 16 bytes)")]
        public object CiphertextWithTag { get; set; }

        [Parameter(Mandatory = true, HelpMessage = "Clé de chiffrement (16, 24, ou 32 bytes pour AES-128/192/256)")]
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

                if (keyBytes.Length != 16 && keyBytes.Length != 24 && keyBytes.Length != 32)
                {
                    throw new ArgumentException("La clé doit faire 16, 24 ou 32 bytes (AES-128, AES-192, ou AES-256)");
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
                byte[] plaintext = AESGCMWrapper.Decrypt(
                    ciphertextBytes, 
                    keyBytes, 
                    nonceBytes, 
                    aadBytes);

                WriteObject(plaintext);
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(ex, "AESGCMDecryptionError", ErrorCategory.NotSpecified, null));
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
}

