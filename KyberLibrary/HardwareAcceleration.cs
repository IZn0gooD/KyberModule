using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace KyberLibrary
{
    /// <summary>
    /// Détection et utilisation de l'accélération matérielle (AVX2, AVX512)
    /// Optimise les opérations cryptographiques avec instructions SIMD
    /// </summary>
    public static class HardwareAcceleration
    {
        private static readonly int _vectorSize;
        private static readonly bool _isHardwareAccelerated;

        static HardwareAcceleration()
        {
            // Détection des capacités CPU via Vector<T>
            // Vector<T> utilise automatiquement les instructions SIMD disponibles (AVX2, AVX512, etc.)
            _isHardwareAccelerated = Vector.IsHardwareAccelerated;
            
            // Taille du vecteur SIMD optimal
            // Vector<byte> utilise la taille de vecteur native du CPU
            if (_isHardwareAccelerated)
            {
                // Vector<byte>.Count retourne le nombre d'éléments dans un vecteur
                // Pour bytes, cela correspond à la taille en bytes
                _vectorSize = Vector<byte>.Count;
            }
            else
            {
                _vectorSize = 8;  // Fallback sans accélération
            }
        }

        /// <summary>
        /// Indique si l'accélération matérielle SIMD est disponible
        /// </summary>
        public static bool IsHardwareAccelerated => _isHardwareAccelerated;

        /// <summary>
        /// Taille optimale du vecteur SIMD en bytes
        /// </summary>
        public static int VectorSize => _vectorSize;

        /// <summary>
        /// Obtient un résumé des capacités matérielles détectées
        /// </summary>
        public static string GetCapabilitiesSummary()
        {
            return $"SIMD: HardwareAccelerated={_isHardwareAccelerated}, VectorSize={_vectorSize} bytes (Vector<byte>.Count={Vector<byte>.Count})";
        }

        /// <summary>
        /// XOR de deux tableaux de bytes avec accélération SIMD
        /// Utilise Vector&lt;byte&gt; pour l'accélération matérielle automatique
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void XorArrays(byte[] a, byte[] b, byte[] result)
        {
            if (a == null || b == null || result == null)
                throw new ArgumentNullException();
            
            if (a.Length != b.Length || a.Length != result.Length)
                throw new ArgumentException("Les tableaux doivent avoir la même longueur");

            int length = a.Length;
            int vectorSize = Vector<byte>.Count;
            int i = 0;

            if (_isHardwareAccelerated && length >= vectorSize)
            {
                // Traiter par vecteurs SIMD
                int vectorizedLength = length - (length % vectorSize);
                for (; i < vectorizedLength; i += vectorSize)
                {
                    var va = new Vector<byte>(a, i);
                    var vb = new Vector<byte>(b, i);
                    var vresult = va ^ vb;
                    vresult.CopyTo(result, i);
                }
            }

            // Traiter les bytes restants
            for (; i < length; i++)
            {
                result[i] = (byte)(a[i] ^ b[i]);
            }
        }

        /// <summary>
        /// Compare deux tableaux de bytes en temps constant avec accélération SIMD
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ConstantTimeEquals(byte[] a, byte[] b)
        {
            if (a == null && b == null)
                return true;
            
            if (a == null || b == null)
                return false;
            
            if (a.Length != b.Length)
                return false;

            int length = a.Length;
            int vectorSize = Vector<byte>.Count;
            int i = 0;
            Vector<byte> accumulator = Vector<byte>.Zero;

            if (_isHardwareAccelerated && length >= vectorSize)
            {
                // Traiter par vecteurs SIMD
                int vectorizedLength = length - (length % vectorSize);
                for (; i < vectorizedLength; i += vectorSize)
                {
                    var va = new Vector<byte>(a, i);
                    var vb = new Vector<byte>(b, i);
                    var diff = va ^ vb;
                    accumulator |= diff;
                }
                
                // Vérifier si accumulator contient des bits non-nuls
                if (!Vector.EqualsAll(accumulator, Vector<byte>.Zero))
                    return false;
            }

            // Traiter les bytes restants
            int result = 0;
            for (; i < length; i++)
            {
                result |= a[i] ^ b[i];
            }

            return result == 0;
        }

        /// <summary>
        /// Remplit un tableau avec une valeur en utilisant SIMD
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void FillArray(byte[] array, byte value)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));

            int length = array.Length;
            int vectorSize = Vector<byte>.Count;
            int i = 0;

            if (_isHardwareAccelerated && length >= vectorSize)
            {
                // Remplir par vecteurs SIMD
                var fillVector = new Vector<byte>(value);
                int vectorizedLength = length - (length % vectorSize);
                for (; i < vectorizedLength; i += vectorSize)
                {
                    fillVector.CopyTo(array, i);
                }
            }

            // Remplir les bytes restants
            for (; i < length; i++)
            {
                array[i] = value;
            }
        }

        /// <summary>
        /// Copie un tableau avec accélération SIMD
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CopyArray(byte[] source, byte[] destination, int length)
        {
            if (source == null || destination == null)
                throw new ArgumentNullException();
            
            if (length > source.Length || length > destination.Length)
                throw new ArgumentException("La longueur dépasse la taille des tableaux");

            int vectorSize = Vector<byte>.Count;
            int i = 0;

            if (_isHardwareAccelerated && length >= vectorSize)
            {
                // Copier par vecteurs SIMD
                int vectorizedLength = length - (length % vectorSize);
                for (; i < vectorizedLength; i += vectorSize)
                {
                    var v = new Vector<byte>(source, i);
                    v.CopyTo(destination, i);
                }
            }

            // Copier les bytes restants
            for (; i < length; i++)
            {
                destination[i] = source[i];
            }
        }
    }
}

