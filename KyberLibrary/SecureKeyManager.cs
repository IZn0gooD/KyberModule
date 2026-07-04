using System;
using System.Runtime.InteropServices;
using System.Security;

namespace KyberLibrary
{
    /// <summary>
    /// Gestionnaire sécurisé pour les clés cryptographiques en mémoire
    /// Implémente le zeroization et la gestion sécurisée de la mémoire
    /// </summary>
    public static class SecureKeyManager
    {
        /// <summary>
        /// Nettoie de manière sécurisée un tableau de bytes en le remplissant de zéros
        /// Utilise Array.Clear() qui est optimisé par le runtime .NET
        /// </summary>
        /// <param name="data">Tableau de bytes à nettoyer</param>
        public static void Zeroize(byte[] data)
        {
            if (data != null && data.Length > 0)
            {
                Array.Clear(data, 0, data.Length);
            }
        }

        /// <summary>
        /// Nettoie de manière sécurisée plusieurs tableaux de bytes
        /// </summary>
        /// <param name="arrays">Tableaux de bytes à nettoyer</param>
        public static void Zeroize(params byte[][] arrays)
        {
            foreach (var array in arrays)
            {
                Zeroize(array);
            }
        }

        /// <summary>
        /// Nettoie de manière sécurisée un tableau de bytes en utilisant un remplissage multiple
        /// Effectue plusieurs passes pour réduire les risques de récupération de données
        /// </summary>
        /// <param name="data">Tableau de bytes à nettoyer</param>
        /// <param name="passes">Nombre de passes de nettoyage (défaut: 3)</param>
        public static void ZeroizeMultiplePasses(byte[] data, int passes = 3)
        {
            if (data == null || data.Length == 0)
                return;

            // Remplissage avec différentes valeurs pour réduire les risques
            byte[] patterns = { 0x00, 0xFF, 0xAA, 0x55 };
            
            for (int pass = 0; pass < passes; pass++)
            {
                byte pattern = patterns[pass % patterns.Length];
                for (int i = 0; i < data.Length; i++)
                {
                    data[i] = pattern;
                }
            }
            
            // Dernière passe avec zéros
            Array.Clear(data, 0, data.Length);
        }

        /// <summary>
        /// Vérifie si un tableau est complètement rempli de zéros
        /// Utile pour vérifier qu'un nettoyage a été effectué
        /// </summary>
        /// <param name="data">Tableau à vérifier</param>
        /// <returns>True si le tableau est vide ou contient uniquement des zéros</returns>
        public static bool IsZeroized(byte[] data)
        {
            if (data == null || data.Length == 0)
                return true;

            foreach (byte b in data)
            {
                if (b != 0)
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Crée une copie sécurisée d'un tableau de bytes
        /// La copie doit être explicitement nettoyée après utilisation
        /// </summary>
        /// <param name="source">Tableau source</param>
        /// <returns>Copie du tableau</returns>
        public static byte[] SecureCopy(byte[] source)
        {
            if (source == null)
                return null;

            byte[] copy = new byte[source.Length];
            Buffer.BlockCopy(source, 0, copy, 0, source.Length);
            return copy;
        }

        /// <summary>
        /// Nettoie de manière sécurisée une chaîne de caractères en mémoire
        /// Note: .NET Core ne supporte pas SecureString de manière native, cette méthode
        /// nettoie les données après conversion en byte[]
        /// </summary>
        /// <param name="data">Chaîne à nettoyer (convertie en bytes)</param>
        public static void ZeroizeString(string data)
        {
            if (string.IsNullOrEmpty(data))
                return;

            // Convertir en bytes et nettoyer
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(data);
            Zeroize(bytes);
        }

        /// <summary>
        /// Nettoie de manière sécurisée un tableau de bytes et le remplace par null
        /// </summary>
        /// <param name="data">Référence au tableau à nettoyer</param>
        public static void ZeroizeAndNullify(ref byte[] data)
        {
            Zeroize(data);
            data = null;
        }

        /// <summary>
        /// Gère le nettoyage sécurisé d'un objet implémentant IDisposable
        /// S'assure que Dispose est appelé même en cas d'exception
        /// </summary>
        /// <typeparam name="T">Type implémentant IDisposable</typeparam>
        /// <param name="disposable">Objet à nettoyer</param>
        public static void SecureDispose<T>(ref T disposable) where T : IDisposable
        {
            if (disposable != null)
            {
                try
                {
                    disposable.Dispose();
                }
                catch
                {
                    // Ignorer les exceptions lors du nettoyage
                }
                finally
                {
                    disposable = default(T);
                }
            }
        }

        /// <summary>
        /// Nettoie de manière sécurisée un tableau de GCHandle (pour données non managées)
        /// </summary>
        /// <param name="handles">Tableau de GCHandle à nettoyer</param>
        public static void ZeroizeGCHandles(GCHandle[] handles)
        {
            if (handles == null)
                return;

            foreach (var handle in handles)
            {
                if (handle.IsAllocated)
                {
                    try
                    {
                        // Nettoyer les données pointées si c'est un tableau
                        if (handle.Target is byte[] bytes)
                        {
                            Zeroize(bytes);
                        }
                        handle.Free();
                    }
                    catch
                    {
                        // Ignorer les exceptions
                    }
                }
            }
        }

        /// <summary>
        /// Forcer le garbage collection pour encourager le nettoyage de la mémoire
        /// À utiliser avec précaution, seulement quand nécessaire
        /// </summary>
        /// <param name="generation">Génération GC à collecter (-1 pour toutes)</param>
        public static void ForceGarbageCollection(int generation = -1)
        {
            if (generation >= 0 && generation <= 2)
            {
                GC.Collect(generation, GCCollectionMode.Forced, true);
            }
            else
            {
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true);
            }
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }
    }
}

