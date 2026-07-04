using System;
using System.Management.Automation;
using KyberLibrary;

namespace KyberModule
{
    /// <summary>
    /// Cmdlet pour calculer un hachage SHA3 (SHA-3) de données
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "SHA3Hash")]
    [OutputType(typeof(SHA3HashResult))]
    public class GetSHA3HashCommand : PSCmdlet
    {
        [Parameter(Mandatory = true, HelpMessage = "Les données à hacher (byte[] ou chaîne)")]
        [AllowEmptyString]
        public object Data { get; set; }

        [Parameter(Mandatory = false, HelpMessage = "Variant SHA3 à utiliser (SHA3_256 ou SHA3_384). Par défaut: SHA3_256")]
        [ValidateSet("SHA3_256", "SHA3_384")]
        public string Variant { get; set; } = "SHA3_256";

        [Parameter(Mandatory = false, HelpMessage = "Si les données sont une chaîne, utiliser l'encodage UTF-8")]
        public SwitchParameter AsString { get; set; }

        protected override void ProcessRecord()
        {
            try
            {
                byte[] dataBytes;

                // Convertir les données en byte[]
                if (Data is byte[] bytes)
                {
                    dataBytes = bytes;
                }
                else if (Data is string str)
                {
                    if (AsString.IsPresent || !IsHexString(str))
                    {
                        // Traiter comme une chaîne UTF-8
                        dataBytes = System.Text.Encoding.UTF8.GetBytes(str);
                    }
                    else
                    {
                        // Traiter comme une chaîne hexadécimale
                        dataBytes = SHA3Wrapper.HexToBytes(str);
                    }
                }
                else
                {
                    throw new ArgumentException("Les données doivent être un byte[] ou une chaîne");
                }

                // Convertir le variant string en enum
                SHA3Wrapper.SHA3Variant variant = Variant.ToUpper() == "SHA3_384" 
                    ? SHA3Wrapper.SHA3Variant.SHA3_384 
                    : SHA3Wrapper.SHA3Variant.SHA3_256;

                // Calculer le hachage
                byte[] hash = SHA3Wrapper.ComputeHash(dataBytes, variant);

                // Créer l'objet résultat
                var result = new SHA3HashResult
                {
                    Hash = hash,
                    HashHex = SHA3Wrapper.BytesToHex(hash),
                    Variant = variant.ToString(),
                    HashSize = hash.Length,
                    HashSizeBits = variant == SHA3Wrapper.SHA3Variant.SHA3_256 ? 256 : 384
                };

                WriteObject(result);
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(ex, "SHA3HashError", ErrorCategory.NotSpecified, null));
            }
        }

        private bool IsHexString(string str)
        {
            if (string.IsNullOrEmpty(str))
                return false;

            // Vérifier si la chaîne contient uniquement des caractères hexadécimaux
            foreach (char c in str)
            {
                if (!((c >= '0' && c <= '9') || (c >= 'A' && c <= 'F') || (c >= 'a' && c <= 'f')))
                {
                    return false;
                }
            }
            return str.Length % 2 == 0; // La longueur doit être paire pour être un hex valide
        }
    }

    /// <summary>
    /// Résultat du hachage SHA3
    /// </summary>
    public class SHA3HashResult
    {
        public byte[] Hash { get; set; }
        public string HashHex { get; set; }
        public string Variant { get; set; }
        public int HashSize { get; set; }
        public int HashSizeBits { get; set; }
    }
}

