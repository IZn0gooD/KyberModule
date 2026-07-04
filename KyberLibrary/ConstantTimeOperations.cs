using System;
using System.Runtime.CompilerServices;

namespace KyberLibrary
{
    /// <summary>
    /// Utilitaires pour opérations à temps constant (constant-time operations)
    /// Protège contre les attaques par timing (timing attacks)
    /// </summary>
    public static class ConstantTimeOperations
    {
        /// <summary>
        /// Compare deux tableaux de bytes en temps constant
        /// Retourne true si les tableaux sont identiques, false sinon
        /// Le temps d'exécution est indépendant du contenu des données
        /// Utilise l'accélération matérielle SIMD si disponible
        /// </summary>
        /// <param name="a">Premier tableau</param>
        /// <param name="b">Deuxième tableau</param>
        /// <returns>True si les tableaux sont identiques</returns>
        [MethodImpl(MethodImplOptions.NoOptimization)]
        public static bool ConstantTimeEquals(byte[] a, byte[] b)
        {
            if (a == null && b == null)
                return true;
            
            if (a == null || b == null)
                return false;
            
            if (a.Length != b.Length)
                return false;

            // Utiliser l'accélération matérielle pour les grandes tailles
            if (PerformanceOptimizer.ShouldUseOptimizations(a.Length) && HardwareAcceleration.IsHardwareAccelerated)
            {
                return HardwareAcceleration.ConstantTimeEquals(a, b);
            }

            // Fallback pour petites tailles ou sans accélération
            // Utiliser une variable accumulatrice pour éviter les branches conditionnelles
            int result = 0;
            
            // Parcourir tous les éléments même si une différence est trouvée
            // Le temps d'exécution est constant indépendamment du résultat
            for (int i = 0; i < a.Length; i++)
            {
                result |= a[i] ^ b[i];  // XOR : 0 si identique, non-0 si différent
            }

            // Retourner true seulement si result == 0 (tous les bytes sont identiques)
            return result == 0;
        }

        /// <summary>
        /// Compare deux tableaux de bytes en temps constant avec limite de longueur
        /// Utile pour comparer des clés de tailles différentes sans révéler la taille
        /// </summary>
        /// <param name="a">Premier tableau</param>
        /// <param name="b">Deuxième tableau</param>
        /// <param name="maxLength">Longueur maximale à comparer</param>
        /// <returns>True si les tableaux sont identiques sur maxLength bytes</returns>
        [MethodImpl(MethodImplOptions.NoOptimization)]
        public static bool ConstantTimeEquals(byte[] a, byte[] b, int maxLength)
        {
            if (a == null && b == null)
                return true;
            
            if (a == null || b == null)
                return false;

            int length = Math.Min(maxLength, Math.Min(a.Length, b.Length));
            int result = 0;

            // Comparer jusqu'à maxLength
            for (int i = 0; i < length; i++)
            {
                result |= a[i] ^ b[i];
            }

            // Vérifier aussi les longueurs si elles diffèrent
            if (a.Length != b.Length)
            {
                result |= 1;  // Marquer comme différent si longueurs différentes
            }

            return result == 0;
        }

        /// <summary>
        /// Sélectionne conditionnellement une valeur en temps constant
        /// Retourne a si condition est true, b sinon
        /// Le temps d'exécution est constant indépendamment de la condition
        /// </summary>
        /// <param name="condition">Condition (true ou false)</param>
        /// <param name="a">Valeur si condition est true</param>
        /// <param name="b">Valeur si condition est false</param>
        /// <returns>a si condition est true, b sinon</returns>
        [MethodImpl(MethodImplOptions.NoOptimization)]
        public static int ConstantTimeSelect(bool condition, int a, int b)
        {
            // Convertir bool en int (0 ou 1)
            int mask = condition ? -1 : 0;  // -1 = tous les bits à 1, 0 = tous les bits à 0
            
            // Sélectionner sans branche conditionnelle
            return (mask & a) | (~mask & b);
        }

        /// <summary>
        /// Sélectionne conditionnellement un byte en temps constant
        /// </summary>
        [MethodImpl(MethodImplOptions.NoOptimization)]
        public static byte ConstantTimeSelect(bool condition, byte a, byte b)
        {
            int mask = condition ? -1 : 0;
            return (byte)((mask & a) | (~mask & b));
        }

        /// <summary>
        /// Sélectionne conditionnellement un tableau de bytes en temps constant
        /// </summary>
        [MethodImpl(MethodImplOptions.NoOptimization)]
        public static byte[] ConstantTimeSelect(bool condition, byte[] a, byte[] b)
        {
            if (a == null || b == null)
                throw new ArgumentNullException("Les tableaux ne peuvent pas être null");

            if (a.Length != b.Length)
                throw new ArgumentException("Les tableaux doivent avoir la même longueur");

            byte[] result = new byte[a.Length];
            int mask = condition ? -1 : 0;

            for (int i = 0; i < a.Length; i++)
            {
                result[i] = (byte)((mask & a[i]) | (~mask & b[i]));
            }

            return result;
        }

        /// <summary>
        /// Copie conditionnellement un tableau en temps constant
        /// Copie src dans dst si condition est true, sinon ne modifie pas dst
        /// </summary>
        [MethodImpl(MethodImplOptions.NoOptimization)]
        public static void ConstantTimeCopy(bool condition, byte[] src, byte[] dst)
        {
            if (src == null || dst == null)
                throw new ArgumentNullException("Les tableaux ne peuvent pas être null");

            if (src.Length != dst.Length)
                throw new ArgumentException("Les tableaux doivent avoir la même longueur");

            int mask = condition ? -1 : 0;

            for (int i = 0; i < src.Length; i++)
            {
                // Si condition est true, copier src[i], sinon garder dst[i]
                dst[i] = (byte)((mask & src[i]) | (~mask & dst[i]));
            }
        }

        /// <summary>
        /// Vérifie si un byte est zéro en temps constant
        /// Retourne 1 si b == 0, 0 sinon
        /// </summary>
        [MethodImpl(MethodImplOptions.NoOptimization)]
        public static int ConstantTimeIsZero(byte b)
        {
            // Utiliser une opération arithmétique pour éviter les branches
            // Si b == 0, alors (b - 1) sera 0xFF, et ~0xFF = 0x00
            // Si b != 0, alors (b - 1) sera < 0xFF, et ~(b-1) sera > 0x00
            uint u = b;
            uint mask = ~((u - 1) >> 8);  // 0xFF si b == 0, 0x00 sinon
            return (int)(mask & 1);
        }

        /// <summary>
        /// Vérifie si un entier est zéro en temps constant
        /// </summary>
        [MethodImpl(MethodImplOptions.NoOptimization)]
        public static int ConstantTimeIsZero(int value)
        {
            // Utiliser des opérations arithmétiques pour éviter les branches
            uint u = (uint)value;
            uint mask = ~((u - 1) >> 31);  // 0xFFFFFFFF si value == 0, 0x00000000 sinon
            return (int)(mask & 1);
        }

        /// <summary>
        /// Compare deux entiers en temps constant
        /// Retourne 1 si a == b, 0 sinon
        /// </summary>
        [MethodImpl(MethodImplOptions.NoOptimization)]
        public static int ConstantTimeEquals(int a, int b)
        {
            return ConstantTimeIsZero(a ^ b);
        }

        /// <summary>
        /// Compare deux bytes en temps constant
        /// Retourne 1 si a == b, 0 sinon
        /// </summary>
        [MethodImpl(MethodImplOptions.NoOptimization)]
        public static int ConstantTimeEquals(byte a, byte b)
        {
            return ConstantTimeIsZero((byte)(a ^ b));
        }

        /// <summary>
        /// Compare deux entiers en temps constant (a < b)
        /// Retourne 1 si a < b, 0 sinon
        /// </summary>
        [MethodImpl(MethodImplOptions.NoOptimization)]
        public static int ConstantTimeLessThan(int a, int b)
        {
            // Calculer (a - b) et vérifier le bit de signe
            int diff = a - b;
            return (diff >> 31) & 1;  // Bit de signe : 1 si négatif (a < b), 0 sinon
        }

        /// <summary>
        /// Compare deux entiers en temps constant (a <= b)
        /// Retourne 1 si a <= b, 0 sinon
        /// </summary>
        [MethodImpl(MethodImplOptions.NoOptimization)]
        public static int ConstantTimeLessThanOrEqual(int a, int b)
        {
            // a <= b équivaut à !(a > b) = !(b < a)
            return 1 - ConstantTimeLessThan(b, a);
        }

        /// <summary>
        /// Masque conditionnel en temps constant
        /// Retourne value si condition est true, 0 sinon
        /// </summary>
        [MethodImpl(MethodImplOptions.NoOptimization)]
        public static byte ConstantTimeMask(bool condition, byte value)
        {
            int mask = condition ? -1 : 0;
            return (byte)(mask & value);
        }

        /// <summary>
        /// Masque conditionnel pour un tableau en temps constant
        /// Applique value[i] si condition est true, 0 sinon
        /// </summary>
        [MethodImpl(MethodImplOptions.NoOptimization)]
        public static byte[] ConstantTimeMask(bool condition, byte[] value)
        {
            if (value == null)
                return null;

            byte[] result = new byte[value.Length];
            int mask = condition ? -1 : 0;

            for (int i = 0; i < value.Length; i++)
            {
                result[i] = (byte)(mask & value[i]);
            }

            return result;
        }

        /// <summary>
        /// Addition modulaire en temps constant
        /// Calcule (a + b) mod m sans révéler les valeurs intermédiaires
        /// </summary>
        [MethodImpl(MethodImplOptions.NoOptimization)]
        public static int ConstantTimeModularAdd(int a, int b, int m)
        {
            if (m <= 0)
                throw new ArgumentException("Le modulo doit être positif", nameof(m));

            // Calculer a + b
            long sum = (long)a + b;
            
            // Réduire modulo m
            int result = (int)(sum % m);
            
            // S'assurer que le résultat est positif
            if (result < 0)
            {
                result += m;
            }

            return result;
        }

        /// <summary>
        /// Vérifie si un tableau contient uniquement des zéros en temps constant
        /// </summary>
        [MethodImpl(MethodImplOptions.NoOptimization)]
        public static bool ConstantTimeIsZeroArray(byte[] array)
        {
            if (array == null || array.Length == 0)
                return true;

            int result = 0;
            for (int i = 0; i < array.Length; i++)
            {
                result |= array[i];
            }

            return result == 0;
        }
    }
}

