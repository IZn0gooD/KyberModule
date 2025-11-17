using System;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace KyberLibrary
{
    /// <summary>
    /// Classe utilitaire pour les tests de fuzzing cryptographique
    /// Génère des données aléatoires/corrompues pour tester la robustesse
    /// </summary>
    public static class CryptographicFuzzing
    {
        private static readonly RandomNumberGenerator _rng = RandomNumberGenerator.Create();

        /// <summary>
        /// Génère des données aléatoires de taille spécifiée
        /// </summary>
        public static byte[] GenerateRandomData(int size)
        {
            byte[] data = new byte[size];
            _rng.GetBytes(data);
            return data;
        }

        /// <summary>
        /// Corrompt un tableau de bytes en modifiant un byte aléatoire
        /// </summary>
        public static byte[] CorruptByte(byte[] data, int? position = null)
        {
            if (data == null || data.Length == 0)
                return data;

            byte[] corrupted = new byte[data.Length];
            Array.Copy(data, corrupted, data.Length);

            int pos = position ?? new Random().Next(0, data.Length);
            corrupted[pos] = (byte)(corrupted[pos] ^ 0xFF); // Inverser tous les bits

            return corrupted;
        }

        /// <summary>
        /// Corrompt un tableau de bytes en modifiant plusieurs bytes
        /// </summary>
        public static byte[] CorruptMultipleBytes(byte[] data, int count)
        {
            if (data == null || data.Length == 0)
                return data;

            byte[] corrupted = new byte[data.Length];
            Array.Copy(data, corrupted, data.Length);

            Random random = new Random();
            HashSet<int> positions = new HashSet<int>();

            while (positions.Count < count && positions.Count < data.Length)
            {
                positions.Add(random.Next(0, data.Length));
            }

            foreach (int pos in positions)
            {
                corrupted[pos] = (byte)(corrupted[pos] ^ 0xFF);
            }

            return corrupted;
        }

        /// <summary>
        /// Génère des données avec des tailles invalides (trop courtes, trop longues)
        /// </summary>
        public static List<byte[]> GenerateInvalidSizedData(int expectedSize)
        {
            var invalidData = new List<byte[]>();

            // Taille 0
            invalidData.Add(new byte[0]);

            // Taille trop courte
            if (expectedSize > 1)
            {
                invalidData.Add(GenerateRandomData(expectedSize - 1));
            }

            // Taille trop longue
            invalidData.Add(GenerateRandomData(expectedSize + 1));
            invalidData.Add(GenerateRandomData(expectedSize * 2));

            // Tailles limites
            invalidData.Add(GenerateRandomData(1));
            invalidData.Add(GenerateRandomData(1024 * 1024)); // 1 MB

            return invalidData;
        }

        /// <summary>
        /// Génère des données avec des patterns spécifiques (tous zéros, tous FF, etc.)
        /// </summary>
        public static List<byte[]> GeneratePatternData(int size)
        {
            var patterns = new List<byte[]>();

            // Tous zéros
            patterns.Add(new byte[size]);

            // Tous 0xFF
            byte[] allFF = new byte[size];
            for (int i = 0; i < size; i++)
                allFF[i] = 0xFF;
            patterns.Add(allFF);

            // Alternance 0x00/0xFF
            byte[] alternating = new byte[size];
            for (int i = 0; i < size; i++)
                alternating[i] = (byte)((i % 2 == 0) ? 0x00 : 0xFF);
            patterns.Add(alternating);

            // Alternance 0xAA/0x55
            byte[] altAA55 = new byte[size];
            for (int i = 0; i < size; i++)
                altAA55[i] = (byte)((i % 2 == 0) ? 0xAA : 0x55);
            patterns.Add(altAA55);

            // Séquence croissante
            byte[] increasing = new byte[size];
            for (int i = 0; i < size; i++)
                increasing[i] = (byte)(i % 256);
            patterns.Add(increasing);

            return patterns;
        }

        /// <summary>
        /// Génère des données avec des valeurs limites (boundary testing)
        /// </summary>
        public static List<byte[]> GenerateBoundaryData(int size)
        {
            var boundaryData = new List<byte[]>();

            // Minimum (1 byte)
            if (size > 0)
            {
                boundaryData.Add(new byte[] { 0x00 });
                boundaryData.Add(new byte[] { 0xFF });
            }

            // Maximum pour différents types
            boundaryData.Add(GenerateRandomData(Math.Min(size, 255)));
            boundaryData.Add(GenerateRandomData(Math.Min(size, 256)));
            boundaryData.Add(GenerateRandomData(Math.Min(size, 65535)));

            return boundaryData;
        }

        /// <summary>
        /// Teste la robustesse d'une fonction avec des données fuzzées
        /// </summary>
        public static FuzzingTestResult TestRobustness(Func<byte[], bool> testFunction, List<byte[]> fuzzedData, string testName)
        {
            var result = new FuzzingTestResult
            {
                TestName = testName,
                TotalTests = fuzzedData.Count,
                Passed = 0,
                Failed = 0,
                Crashed = 0
            };

            foreach (var data in fuzzedData)
            {
                try
                {
                    bool testResult = testFunction(data);
                    if (testResult)
                        result.Passed++;
                    else
                        result.Failed++;
                }
                catch (Exception ex)
                {
                    result.Crashed++;
                    result.Exceptions.Add(new FuzzingException
                    {
                        Data = data,
                        Exception = ex
                    });
                }
            }

            return result;
        }

        /// <summary>
        /// Génère des ciphertexts corrompus pour tester la décapsulation
        /// </summary>
        public static List<byte[]> GenerateCorruptedCiphertexts(byte[] originalCiphertext, int count = 10)
        {
            var corrupted = new List<byte[]>();

            if (originalCiphertext == null || originalCiphertext.Length == 0)
                return corrupted;

            // Corruption à différentes positions
            for (int i = 0; i < Math.Min(count, originalCiphertext.Length); i++)
            {
                corrupted.Add(CorruptByte(originalCiphertext, i));
            }

            // Corruption multiple
            corrupted.Add(CorruptMultipleBytes(originalCiphertext, 3));
            corrupted.Add(CorruptMultipleBytes(originalCiphertext, 5));

            // Tailles invalides
            corrupted.AddRange(GenerateInvalidSizedData(originalCiphertext.Length));

            return corrupted;
        }

        /// <summary>
        /// Génère des signatures corrompues pour tester la vérification
        /// </summary>
        public static List<byte[]> GenerateCorruptedSignatures(byte[] originalSignature, int count = 10)
        {
            var corrupted = new List<byte[]>();

            if (originalSignature == null || originalSignature.Length == 0)
                return corrupted;

            // Corruption à différentes positions
            for (int i = 0; i < Math.Min(count, originalSignature.Length); i++)
            {
                corrupted.Add(CorruptByte(originalSignature, i));
            }

            // Corruption multiple
            corrupted.Add(CorruptMultipleBytes(originalSignature, 3));
            corrupted.Add(CorruptMultipleBytes(originalSignature, 5));

            // Tailles invalides
            corrupted.AddRange(GenerateInvalidSizedData(originalSignature.Length));

            return corrupted;
        }

        /// <summary>
        /// Génère des clés corrompues pour tester la robustesse
        /// </summary>
        public static List<byte[]> GenerateCorruptedKeys(byte[] originalKey, int count = 10)
        {
            var corrupted = new List<byte[]>();

            if (originalKey == null || originalKey.Length == 0)
                return corrupted;

            // Corruption à différentes positions
            for (int i = 0; i < Math.Min(count, originalKey.Length); i++)
            {
                corrupted.Add(CorruptByte(originalKey, i));
            }

            // Corruption multiple
            corrupted.Add(CorruptMultipleBytes(originalKey, 3));
            corrupted.Add(CorruptMultipleBytes(originalKey, 5));

            // Patterns suspects
            corrupted.AddRange(GeneratePatternData(originalKey.Length));

            return corrupted;
        }
    }

    /// <summary>
    /// Résultat d'un test de fuzzing
    /// </summary>
    public class FuzzingTestResult
    {
        public string TestName { get; set; }
        public int TotalTests { get; set; }
        public int Passed { get; set; }
        public int Failed { get; set; }
        public int Crashed { get; set; }
        public List<FuzzingException> Exceptions { get; set; } = new List<FuzzingException>();
    }

    /// <summary>
    /// Exception capturée lors d'un test de fuzzing
    /// </summary>
    public class FuzzingException
    {
        public byte[] Data { get; set; }
        public Exception Exception { get; set; }
    }
}

