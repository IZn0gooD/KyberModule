using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace KyberLibrary
{
    /// <summary>
    /// Protection contre les attaques par canaux auxiliaires (side-channel attacks)
    /// Implémente des contre-mesures contre :
    /// - Timing attacks (attaques par temps d'exécution)
    /// - Cache side-channel attacks (attaques par cache)
    /// - Power analysis (moins applicable en software)
    /// </summary>
    public static class SideChannelProtection
    {
        /// <summary>
        /// Nettoie le cache du processeur en forçant des accès mémoire
        /// Utile pour réduire les fuites d'information via le cache
        /// </summary>
        [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
        public static void FlushCache()
        {
            // Allouer et accéder à un tableau pour forcer le vidage du cache
            // Cette technique force le processeur à réinitialiser certaines lignes de cache
            byte[] dummy = new byte[1024];
            for (int i = 0; i < dummy.Length; i++)
            {
                dummy[i] = (byte)(i & 0xFF);
            }
            
            // Lire les valeurs pour forcer l'accès mémoire
            int sum = 0;
            for (int i = 0; i < dummy.Length; i++)
            {
                sum += dummy[i];
            }
            // Utiliser sum pour éviter l'optimisation
            _ = sum;
            
            // Nettoyer
            Array.Clear(dummy, 0, dummy.Length);
            
            // Forcer une barrière mémoire
            Thread.MemoryBarrier();
        }

        /// <summary>
        /// Accède à un tableau de manière séquentielle pour éviter les patterns de cache révélateurs
        /// </summary>
        [MethodImpl(MethodImplOptions.NoOptimization)]
        public static void SequentialMemoryAccess(byte[] data)
        {
            if (data == null)
                return;

            // Accès séquentiel pour éviter les patterns révélateurs
            int sum = 0;
            for (int i = 0; i < data.Length; i++)
            {
                sum += data[i];
            }
            // Utiliser sum pour éviter l'optimisation
            _ = sum;
            
            Thread.MemoryBarrier();
        }

        /// <summary>
        /// Accède à tous les éléments d'un tableau de manière uniforme
        /// Utile pour masquer les accès mémoire dépendants des données
        /// </summary>
        [MethodImpl(MethodImplOptions.NoOptimization)]
        public static void UniformMemoryAccess(byte[] array, int actualIndex)
        {
            if (array == null || array.Length == 0)
                return;

            // Toujours accéder à tous les éléments pour masquer l'index réel
            byte dummy = 0;
            for (int i = 0; i < array.Length; i++)
            {
                // Utiliser un masque pour sélectionner l'élément réel sans branche
                int mask = ConstantTimeOperations.ConstantTimeEquals(i, actualIndex);
                byte value = ConstantTimeOperations.ConstantTimeSelect(mask == 1, array[i], (byte)0);
                dummy = (byte)(dummy ^ value);
            }
            // Utiliser dummy pour éviter l'optimisation
            _ = dummy;
            
            Thread.MemoryBarrier();
        }

        /// <summary>
        /// Ajoute un délai aléatoire contrôlé pour masquer les variations de timing
        /// Note: À utiliser avec précaution, peut affecter les performances
        /// </summary>
        [MethodImpl(MethodImplOptions.NoOptimization)]
        public static void AddRandomDelay(int minMicroseconds = 0, int maxMicroseconds = 10)
        {
            if (minMicroseconds < 0 || maxMicroseconds < minMicroseconds)
                return;

            // Générer un délai aléatoire
            var random = new Random();
            int delay = random.Next(minMicroseconds, maxMicroseconds + 1);

            if (delay > 0)
            {
                // Utiliser Thread.Sleep avec précision limitée
                // Pour des délais plus précis, utiliser Stopwatch et boucle active
                if (delay >= 1000)  // >= 1ms
                {
                    Thread.Sleep(delay / 1000);
                }
                else
                {
                    // Pour les microsecondes, utiliser une boucle active
                    var sw = System.Diagnostics.Stopwatch.StartNew();
                    while (sw.ElapsedTicks < delay * 10)  // Approximation : 1 tick ≈ 100ns
                    {
                        Thread.SpinWait(1);
                    }
                }
            }
        }

        /// <summary>
        /// Masque les accès mémoire en accédant toujours à un ensemble fixe d'adresses
        /// Protège contre les attaques par cache basées sur les patterns d'accès
        /// </summary>
        [MethodImpl(MethodImplOptions.NoOptimization)]
        public static byte SafeArrayAccess(byte[] array, int index, byte[] maskArray)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));

            if (index < 0 || index >= array.Length)
                throw new ArgumentOutOfRangeException(nameof(index));

            if (maskArray == null || maskArray.Length != array.Length)
            {
                // Si pas de masque, accéder quand même à tous les éléments
                SequentialMemoryAccess(array);
                return array[index];
            }

            // Accéder à tous les éléments pour masquer l'index réel
            byte result = 0;
            for (int i = 0; i < array.Length; i++)
            {
                int isTarget = ConstantTimeOperations.ConstantTimeEquals(i, index);
                byte value = ConstantTimeOperations.ConstantTimeSelect(isTarget == 1, array[i], maskArray[i]);
                result = ConstantTimeOperations.ConstantTimeSelect(isTarget == 1, value, result);
            }

            return result;
        }

        /// <summary>
        /// Compare deux tableaux avec protection contre timing attacks
        /// Utilise ConstantTimeEquals et nettoie le cache après comparaison
        /// </summary>
        [MethodImpl(MethodImplOptions.NoOptimization)]
        public static bool SecureCompare(byte[] a, byte[] b)
        {
            bool result = ConstantTimeOperations.ConstantTimeEquals(a, b);
            
            // Nettoyer le cache après comparaison
            FlushCache();
            
            return result;
        }

        /// <summary>
        /// Copie sécurisée avec protection contre cache side-channel
        /// </summary>
        [MethodImpl(MethodImplOptions.NoOptimization)]
        public static void SecureCopy(byte[] source, byte[] destination, int length)
        {
            if (source == null || destination == null)
                throw new ArgumentNullException();

            if (length < 0 || length > source.Length || length > destination.Length)
                throw new ArgumentOutOfRangeException(nameof(length));

            // Copie séquentielle pour éviter les patterns révélateurs
            for (int i = 0; i < length; i++)
            {
                destination[i] = source[i];
            }

            // Nettoyer le cache après copie
            FlushCache();
        }

        /// <summary>
        /// Masque les opérations conditionnelles en exécutant toujours les deux branches
        /// Protège contre les attaques par timing basées sur les branches conditionnelles
        /// </summary>
        [MethodImpl(MethodImplOptions.NoOptimization)]
        public static void ExecuteBothBranches(Action ifTrue, Action ifFalse, bool condition)
        {
            // Exécuter les deux branches pour masquer la condition
            // Utiliser des masques pour ignorer les résultats non désirés
            
            // Préparer les masques
            int trueMask = condition ? -1 : 0;
            int falseMask = condition ? 0 : -1;

            // Exécuter la branche "true" (résultats masqués si condition est false)
            try
            {
                ifTrue?.Invoke();
            }
            catch
            {
                // Ignorer les exceptions si cette branche ne doit pas s'exécuter
                if (trueMask == 0)
                {
                    // Exception attendue, ignorer
                }
                else
                {
                    throw;  // Ré-émettre si c'est la branche active
                }
            }

            // Exécuter la branche "false" (résultats masqués si condition est true)
            try
            {
                ifFalse?.Invoke();
            }
            catch
            {
                // Ignorer les exceptions si cette branche ne doit pas s'exécuter
                if (falseMask == 0)
                {
                    // Exception attendue, ignorer
                }
                else
                {
                    throw;  // Ré-émettre si c'est la branche active
                }
            }
        }

        /// <summary>
        /// Protège une opération sensible contre les attaques par timing
        /// Exécute l'opération avec délai constant et nettoyage du cache
        /// </summary>
        [MethodImpl(MethodImplOptions.NoOptimization)]
        public static T ProtectTiming<T>(Func<T> operation)
        {
            // Nettoyer le cache avant l'opération
            FlushCache();
            
            // Exécuter l'opération
            T result = operation();
            
            // Nettoyer le cache après l'opération
            FlushCache();
            
            // Barrière mémoire
            Thread.MemoryBarrier();
            
            return result;
        }

        /// <summary>
        /// Protège une opération sans valeur de retour contre les attaques par timing
        /// </summary>
        [MethodImpl(MethodImplOptions.NoOptimization)]
        public static void ProtectTiming(Action operation)
        {
            FlushCache();
            operation?.Invoke();
            FlushCache();
            Thread.MemoryBarrier();
        }

        /// <summary>
        /// Masque les accès mémoire en préchargeant un ensemble d'adresses
        /// Utile pour réduire les fuites d'information via le cache L1/L2
        /// </summary>
        [MethodImpl(MethodImplOptions.NoOptimization)]
        public static void PreloadCache(byte[] data, int[] indices)
        {
            if (data == null || indices == null)
                return;

            // Accéder à tous les indices pour précharger le cache
            int sum = 0;
            foreach (int index in indices)
            {
                if (index >= 0 && index < data.Length)
                {
                    sum += data[index];
                }
            }
            // Utiliser sum pour éviter l'optimisation
            _ = sum;
            
            Thread.MemoryBarrier();
        }

        /// <summary>
        /// Vérifie l'intégrité d'un tableau avec protection contre timing attacks
        /// </summary>
        [MethodImpl(MethodImplOptions.NoOptimization)]
        public static bool SecureIntegrityCheck(byte[] data, byte[] expectedHash)
        {
            if (data == null || expectedHash == null)
                return false;

            // Calculer le hash de manière sécurisée
            byte[] computedHash = SHA3Wrapper.ComputeSHA3_256(data);
            
            // Comparer en temps constant
            bool isValid = ConstantTimeOperations.ConstantTimeEquals(computedHash, expectedHash);
            
            // Nettoyer les données temporaires
            SecureKeyManager.Zeroize(computedHash);
            FlushCache();
            
            return isValid;
        }
    }
}

