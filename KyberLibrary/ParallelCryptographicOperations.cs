using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace KyberLibrary
{
    /// <summary>
    /// Opérations cryptographiques parallélisées avec multi-threading natif
    /// Optimise les opérations batch et les traitements en masse
    /// </summary>
    public static class ParallelCryptographicOperations
    {
        /// <summary>
        /// Nombre optimal de threads à utiliser (basé sur le nombre de cœurs CPU)
        /// </summary>
        public static int OptimalThreadCount => Math.Max(1, Environment.ProcessorCount);

        /// <summary>
        /// Génère plusieurs paires de clés en parallèle
        /// </summary>
        /// <typeparam name="T">Type du wrapper cryptographique</typeparam>
        /// <param name="count">Nombre de paires de clés à générer</param>
        /// <param name="keyGenerator">Fonction de génération de clés</param>
        /// <param name="cancellationToken">Token d'annulation</param>
        /// <returns>Liste des paires de clés générées</returns>
        public static List<(byte[] PublicKey, byte[] PrivateKey)> GenerateKeyPairsParallel<T>(
            int count,
            Func<(byte[] PublicKey, byte[] PrivateKey)> keyGenerator,
            CancellationToken cancellationToken = default)
        {
            if (count <= 0)
                throw new ArgumentException("Le nombre doit être supérieur à 0", nameof(count));
            
            if (keyGenerator == null)
                throw new ArgumentNullException(nameof(keyGenerator));

            var results = new (byte[] PublicKey, byte[] PrivateKey)[count];
            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = OptimalThreadCount,
                CancellationToken = cancellationToken
            };

            Parallel.For(0, count, parallelOptions, i =>
            {
                try
                {
                    results[i] = keyGenerator();
                }
                catch (Exception ex)
                {
                    throw new CryptographicException($"Erreur lors de la génération de la paire de clés #{i}", ex);
                }
            });

            return results.ToList();
        }

        /// <summary>
        /// Traite plusieurs opérations d'encapsulation en parallèle
        /// </summary>
        /// <param name="publicKeys">Liste des clés publiques</param>
        /// <param name="encapsulator">Fonction d'encapsulation</param>
        /// <param name="cancellationToken">Token d'annulation</param>
        /// <returns>Liste des résultats d'encapsulation</returns>
        public static List<TResult> EncapsulateParallel<TResult>(
            IList<byte[]> publicKeys,
            Func<byte[], TResult> encapsulator,
            CancellationToken cancellationToken = default)
        {
            if (publicKeys == null || publicKeys.Count == 0)
                throw new ArgumentException("La liste des clés publiques ne peut pas être vide", nameof(publicKeys));
            
            if (encapsulator == null)
                throw new ArgumentNullException(nameof(encapsulator));

            var results = new TResult[publicKeys.Count];
            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = OptimalThreadCount,
                CancellationToken = cancellationToken
            };

            Parallel.For(0, publicKeys.Count, parallelOptions, i =>
            {
                try
                {
                    results[i] = encapsulator(publicKeys[i]);
                }
                catch (Exception ex)
                {
                    throw new CryptographicException($"Erreur lors de l'encapsulation #{i}", ex);
                }
            });

            return results.ToList();
        }

        /// <summary>
        /// Traite plusieurs opérations de décapsulation en parallèle
        /// </summary>
        /// <param name="ciphertexts">Liste des ciphertexts</param>
        /// <param name="privateKeys">Liste des clés privées correspondantes</param>
        /// <param name="decapsulator">Fonction de décapsulation</param>
        /// <param name="cancellationToken">Token d'annulation</param>
        /// <returns>Liste des résultats de décapsulation</returns>
        public static List<TResult> DecapsulateParallel<TResult>(
            IList<byte[]> ciphertexts,
            IList<byte[]> privateKeys,
            Func<byte[], byte[], TResult> decapsulator,
            CancellationToken cancellationToken = default)
        {
            if (ciphertexts == null || ciphertexts.Count == 0)
                throw new ArgumentException("La liste des ciphertexts ne peut pas être vide", nameof(ciphertexts));
            
            if (privateKeys == null || privateKeys.Count != ciphertexts.Count)
                throw new ArgumentException("Le nombre de clés privées doit correspondre au nombre de ciphertexts", nameof(privateKeys));
            
            if (decapsulator == null)
                throw new ArgumentNullException(nameof(decapsulator));

            var results = new TResult[ciphertexts.Count];
            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = OptimalThreadCount,
                CancellationToken = cancellationToken
            };

            Parallel.For(0, ciphertexts.Count, parallelOptions, i =>
            {
                try
                {
                    results[i] = decapsulator(ciphertexts[i], privateKeys[i]);
                }
                catch (Exception ex)
                {
                    throw new CryptographicException($"Erreur lors de la décapsulation #{i}", ex);
                }
            });

            return results.ToList();
        }

        /// <summary>
        /// Traite plusieurs signatures en parallèle
        /// </summary>
        /// <param name="dataList">Liste des données à signer</param>
        /// <param name="privateKeys">Liste des clés privées correspondantes</param>
        /// <param name="signer">Fonction de signature</param>
        /// <param name="cancellationToken">Token d'annulation</param>
        /// <returns>Liste des signatures</returns>
        public static List<TResult> SignParallel<TResult>(
            IList<byte[]> dataList,
            IList<byte[]> privateKeys,
            Func<byte[], byte[], TResult> signer,
            CancellationToken cancellationToken = default)
        {
            if (dataList == null || dataList.Count == 0)
                throw new ArgumentException("La liste des données ne peut pas être vide", nameof(dataList));
            
            if (privateKeys == null || privateKeys.Count != dataList.Count)
                throw new ArgumentException("Le nombre de clés privées doit correspondre au nombre de données", nameof(privateKeys));
            
            if (signer == null)
                throw new ArgumentNullException(nameof(signer));

            var results = new TResult[dataList.Count];
            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = OptimalThreadCount,
                CancellationToken = cancellationToken
            };

            Parallel.For(0, dataList.Count, parallelOptions, i =>
            {
                try
                {
                    results[i] = signer(dataList[i], privateKeys[i]);
                }
                catch (Exception ex)
                {
                    throw new CryptographicException($"Erreur lors de la signature #{i}", ex);
                }
            });

            return results.ToList();
        }

        /// <summary>
        /// Vérifie plusieurs signatures en parallèle
        /// </summary>
        /// <param name="dataList">Liste des données</param>
        /// <param name="signatures">Liste des signatures</param>
        /// <param name="publicKeys">Liste des clés publiques correspondantes</param>
        /// <param name="verifier">Fonction de vérification</param>
        /// <param name="cancellationToken">Token d'annulation</param>
        /// <returns>Liste des résultats de vérification (true = valide, false = invalide)</returns>
        public static List<bool> VerifyParallel(
            IList<byte[]> dataList,
            IList<byte[]> signatures,
            IList<byte[]> publicKeys,
            Func<byte[], byte[], byte[], bool> verifier,
            CancellationToken cancellationToken = default)
        {
            if (dataList == null || dataList.Count == 0)
                throw new ArgumentException("La liste des données ne peut pas être vide", nameof(dataList));
            
            if (signatures == null || signatures.Count != dataList.Count)
                throw new ArgumentException("Le nombre de signatures doit correspondre au nombre de données", nameof(signatures));
            
            if (publicKeys == null || publicKeys.Count != dataList.Count)
                throw new ArgumentException("Le nombre de clés publiques doit correspondre au nombre de données", nameof(publicKeys));
            
            if (verifier == null)
                throw new ArgumentNullException(nameof(verifier));

            var results = new bool[dataList.Count];
            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = OptimalThreadCount,
                CancellationToken = cancellationToken
            };

            Parallel.For(0, dataList.Count, parallelOptions, i =>
            {
                try
                {
                    results[i] = verifier(dataList[i], signatures[i], publicKeys[i]);
                }
                catch
                {
                    // En cas d'erreur, considérer la signature comme invalide
                    results[i] = false;
                }
            });

            return results.ToList();
        }

        /// <summary>
        /// Traite une liste d'opérations génériques en parallèle
        /// </summary>
        /// <typeparam name="T">Type des éléments d'entrée</typeparam>
        /// <typeparam name="TResult">Type des résultats</typeparam>
        /// <param name="items">Liste des éléments à traiter</param>
        /// <param name="processor">Fonction de traitement</param>
        /// <param name="cancellationToken">Token d'annulation</param>
        /// <returns>Liste des résultats</returns>
        public static List<TResult> ProcessParallel<T, TResult>(
            IList<T> items,
            Func<T, TResult> processor,
            CancellationToken cancellationToken = default)
        {
            if (items == null || items.Count == 0)
                throw new ArgumentException("La liste ne peut pas être vide", nameof(items));
            
            if (processor == null)
                throw new ArgumentNullException(nameof(processor));

            var results = new TResult[items.Count];
            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = OptimalThreadCount,
                CancellationToken = cancellationToken
            };

            Parallel.For(0, items.Count, parallelOptions, i =>
            {
                try
                {
                    results[i] = processor(items[i]);
                }
                catch (Exception ex)
                {
                    throw new CryptographicException($"Erreur lors du traitement de l'élément #{i}", ex);
                }
            });

            return results.ToList();
        }

        /// <summary>
        /// Calcule le nombre optimal de partitions pour le traitement parallèle
        /// </summary>
        /// <param name="totalItems">Nombre total d'éléments</param>
        /// <returns>Nombre optimal de partitions</returns>
        public static int GetOptimalPartitionCount(int totalItems)
        {
            if (totalItems <= 0)
                return 1;

            // Utiliser le nombre de cœurs CPU comme base
            int cores = OptimalThreadCount;
            
            // Pour de petits lots, utiliser moins de threads
            if (totalItems < cores)
                return totalItems;
            
            // Pour de grands lots, utiliser tous les cœurs disponibles
            return cores;
        }
    }
}

