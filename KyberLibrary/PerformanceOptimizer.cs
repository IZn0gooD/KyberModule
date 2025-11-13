using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace KyberLibrary
{
    /// <summary>
    /// Gestionnaire centralisé pour toutes les optimisations de performance
    /// Coordonne l'utilisation du multi-threading, de l'accélération matérielle et des HSM
    /// </summary>
    public static class PerformanceOptimizer
    {
        private static bool _optimizationsEnabled = true;
        private static int _maxParallelism = Environment.ProcessorCount;

        /// <summary>
        /// Active ou désactive toutes les optimisations
        /// </summary>
        public static bool OptimizationsEnabled
        {
            get => _optimizationsEnabled;
            set => _optimizationsEnabled = value;
        }

        /// <summary>
        /// Nombre maximum de threads parallèles à utiliser
        /// </summary>
        public static int MaxParallelism
        {
            get => _maxParallelism;
            set => _maxParallelism = Math.Max(1, Math.Min(value, Environment.ProcessorCount * 2));
        }

        /// <summary>
        /// Obtient un résumé complet des optimisations disponibles
        /// </summary>
        public static string GetOptimizationsSummary()
        {
            var summary = "=== Optimisations de Performance ===\n";
            summary += $"Optimisations activées: {_optimizationsEnabled}\n";
            summary += $"Max parallélisme: {_maxParallelism} threads\n";
            summary += $"CPU cores: {Environment.ProcessorCount}\n";
            summary += $"{HardwareAcceleration.GetCapabilitiesSummary()}\n";
            summary += $"{HSMManager.GetHSMSummary()}\n";
            return summary;
        }

        /// <summary>
        /// Mesure les performances d'une opération avec et sans optimisations
        /// </summary>
        /// <param name="operation">Opération à mesurer</param>
        /// <param name="iterations">Nombre d'itérations</param>
        /// <returns>Résultats de performance</returns>
        public static PerformanceBenchmarkResult BenchmarkOperation(
            Action operation,
            int iterations = 100)
        {
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));

            var result = new PerformanceBenchmarkResult
            {
                Iterations = iterations
            };

            // Mesure avec optimisations
            _optimizationsEnabled = true;
            var sw = Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
            {
                operation();
            }
            sw.Stop();
            result.TimeWithOptimizations = sw.ElapsedMilliseconds;
            result.AverageTimeWithOptimizations = (double)sw.ElapsedMilliseconds / iterations;

            // Mesure sans optimisations
            _optimizationsEnabled = false;
            sw.Restart();
            for (int i = 0; i < iterations; i++)
            {
                operation();
            }
            sw.Stop();
            result.TimeWithoutOptimizations = sw.ElapsedMilliseconds;
            result.AverageTimeWithoutOptimizations = (double)sw.ElapsedMilliseconds / iterations;

            // Calcul de l'amélioration
            if (result.TimeWithoutOptimizations > 0)
            {
                result.Speedup = (double)result.TimeWithoutOptimizations / result.TimeWithOptimizations;
                result.ImprovementPercent = ((result.TimeWithoutOptimizations - result.TimeWithOptimizations) / (double)result.TimeWithoutOptimizations) * 100;
            }

            // Réactiver les optimisations
            _optimizationsEnabled = true;

            return result;
        }

        /// <summary>
        /// Mesure les performances d'une opération asynchrone
        /// </summary>
        public static async Task<PerformanceBenchmarkResult> BenchmarkOperationAsync(
            Func<Task> operation,
            int iterations = 100)
        {
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));

            var result = new PerformanceBenchmarkResult
            {
                Iterations = iterations
            };

            // Mesure avec optimisations
            _optimizationsEnabled = true;
            var sw = Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
            {
                await operation();
            }
            sw.Stop();
            result.TimeWithOptimizations = sw.ElapsedMilliseconds;
            result.AverageTimeWithOptimizations = (double)sw.ElapsedMilliseconds / iterations;

            // Mesure sans optimisations
            _optimizationsEnabled = false;
            sw.Restart();
            for (int i = 0; i < iterations; i++)
            {
                await operation();
            }
            sw.Stop();
            result.TimeWithoutOptimizations = sw.ElapsedMilliseconds;
            result.AverageTimeWithoutOptimizations = (double)sw.ElapsedMilliseconds / iterations;

            // Calcul de l'amélioration
            if (result.TimeWithoutOptimizations > 0)
            {
                result.Speedup = (double)result.TimeWithoutOptimizations / result.TimeWithOptimizations;
                result.ImprovementPercent = ((result.TimeWithoutOptimizations - result.TimeWithOptimizations) / (double)result.TimeWithoutOptimizations) * 100;
            }

            // Réactiver les optimisations
            _optimizationsEnabled = true;

            return result;
        }

        /// <summary>
        /// Vérifie si les optimisations doivent être utilisées pour une opération donnée
        /// </summary>
        public static bool ShouldUseOptimizations(int dataSize)
        {
            if (!_optimizationsEnabled)
                return false;

            // Pour de très petites opérations, l'overhead peut être plus important que le gain
            // Seuil minimum pour activer les optimisations
            const int minimumSizeForOptimization = 1024; // 1 KB

            return dataSize >= minimumSizeForOptimization;
        }
    }

    /// <summary>
    /// Résultat d'un benchmark de performance
    /// </summary>
    public class PerformanceBenchmarkResult
    {
        public int Iterations { get; set; }
        public long TimeWithOptimizations { get; set; }
        public long TimeWithoutOptimizations { get; set; }
        public double AverageTimeWithOptimizations { get; set; }
        public double AverageTimeWithoutOptimizations { get; set; }
        public double Speedup { get; set; }
        public double ImprovementPercent { get; set; }

        public override string ToString()
        {
            return $"Iterations: {Iterations}\n" +
                   $"Avec optimisations: {TimeWithOptimizations}ms (moyenne: {AverageTimeWithOptimizations:F2}ms)\n" +
                   $"Sans optimisations: {TimeWithoutOptimizations}ms (moyenne: {AverageTimeWithoutOptimizations:F2}ms)\n" +
                   $"Amélioration: {Speedup:F2}x ({ImprovementPercent:F2}%)";
        }
    }
}

