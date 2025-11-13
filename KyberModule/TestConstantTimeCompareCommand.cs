using System;
using System.Management.Automation;
using KyberLibrary;

namespace KyberModule
{
    /// <summary>
    /// Cmdlet pour comparer deux tableaux de bytes en temps constant
    /// Protège contre les attaques par timing
    /// </summary>
    [Cmdlet(VerbsDiagnostic.Test, "ConstantTimeCompare")]
    [OutputType(typeof(bool))]
    public class TestConstantTimeCompareCommand : PSCmdlet
    {
        [Parameter(
            Mandatory = true,
            Position = 0,
            HelpMessage = "Premier tableau de bytes (byte[] ou chaîne hexadécimale)")]
        public object Array1 { get; set; }

        [Parameter(
            Mandatory = true,
            Position = 1,
            HelpMessage = "Deuxième tableau de bytes (byte[] ou chaîne hexadécimale)")]
        public object Array2 { get; set; }

        protected override void ProcessRecord()
        {
            try
            {
                byte[] array1Bytes = ConvertToByteArray(Array1, "Array1");
                byte[] array2Bytes = ConvertToByteArray(Array2, "Array2");

                // Comparer en temps constant
                bool areEqual = ConstantTimeOperations.ConstantTimeEquals(array1Bytes, array2Bytes);

                WriteObject(areEqual);
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(ex, "ConstantTimeCompareError", ErrorCategory.NotSpecified, null));
            }
        }

        private byte[] ConvertToByteArray(object value, string parameterName)
        {
            if (value == null)
            {
                throw new ArgumentNullException(parameterName);
            }

            if (value is byte[] bytes)
            {
                return bytes;
            }

            if (value is string hexString)
            {
                return KyberWrapper.HexToBytes(hexString);
            }

            throw new ArgumentException($"Le paramètre {parameterName} doit être un byte[] ou une chaîne hexadécimale");
        }
    }
}

