using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace KyberLibrary
{
    /// <summary>
    /// Session shell interactive persistante (style SSH)
    /// Maintient un shell ouvert et permet l'exécution de commandes dans le même contexte
    /// </summary>
    public class InteractiveShellSession : IDisposable
    {
        private Process _shellProcess;
        private StreamWriter _stdin;
        private StreamReader _stdout;
        private StreamReader _stderr;
        private readonly ConcurrentQueue<string> _outputQueue;
        private readonly ConcurrentQueue<string> _errorQueue;
        private readonly ManualResetEventSlim _outputReady;
        private readonly ManualResetEventSlim _errorReady;
        private readonly object _lockObject = new object();
        private bool _disposed = false;
        private string _currentWorkingDirectory;
        private readonly CommandType _shellType;

        public string SessionId { get; }
        public bool IsActive => _shellProcess != null && !_shellProcess.HasExited;
        public string CurrentWorkingDirectory 
        { 
            get => _currentWorkingDirectory ?? Environment.CurrentDirectory;
            private set => _currentWorkingDirectory = value;
        }

        public InteractiveShellSession(CommandType shellType, string initialWorkingDirectory = null)
        {
            SessionId = Guid.NewGuid().ToString();
            _shellType = shellType;
            _currentWorkingDirectory = initialWorkingDirectory ?? Environment.CurrentDirectory;
            _outputQueue = new ConcurrentQueue<string>();
            _errorQueue = new ConcurrentQueue<string>();
            _outputReady = new ManualResetEventSlim(false);
            _errorReady = new ManualResetEventSlim(false);
        }

        /// <summary>
        /// Démarre la session shell interactive
        /// </summary>
        public void Start()
        {
            if (IsActive)
            {
                throw new InvalidOperationException("La session shell est déjà active");
            }

            ProcessStartInfo startInfo;

            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                if (_shellType == CommandType.ExecutePowerShell)
                {
                    // PowerShell interactif
                    startInfo = new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = "-NoProfile -NoExit",
                        UseShellExecute = false,
                        RedirectStandardInput = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true,
                        WorkingDirectory = CurrentWorkingDirectory
                    };
                }
                else
                {
                    // CMD interactif
                    startInfo = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = "/k",
                        UseShellExecute = false,
                        RedirectStandardInput = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true,
                        WorkingDirectory = CurrentWorkingDirectory
                    };
                }
            }
            else
            {
                // Bash interactif
                startInfo = new ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    Arguments = "-i",
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    WorkingDirectory = CurrentWorkingDirectory
                };
            }

            _shellProcess = new Process { StartInfo = startInfo };
            _shellProcess.OutputDataReceived += (sender, e) =>
            {
                if (e.Data != null)
                {
                    _outputQueue.Enqueue(e.Data);
                    _outputReady.Set();
                }
            };

            _shellProcess.ErrorDataReceived += (sender, e) =>
            {
                if (e.Data != null)
                {
                    _errorQueue.Enqueue(e.Data);
                    _errorReady.Set();
                }
            };

            _shellProcess.Start();
            _stdin = _shellProcess.StandardInput;
            _stdout = _shellProcess.StandardOutput;
            _stderr = _shellProcess.StandardError;

            _shellProcess.BeginOutputReadLine();
            _shellProcess.BeginErrorReadLine();

            // Attendre que le shell soit prêt et vider les messages initiaux
            Thread.Sleep(1000);
            
            // Vider les queues des messages initiaux (prompts, messages de bienvenue, etc.)
            lock (_lockObject)
            {
                while (_outputQueue.TryDequeue(out _)) { }
                while (_errorQueue.TryDequeue(out _)) { }
            }
        }

        /// <summary>
        /// Exécute une commande dans la session shell persistante
        /// </summary>
        public async Task<CommandResult> ExecuteCommandAsync(string command, int timeoutMs = 30000, CancellationToken cancellationToken = default)
        {
            if (!IsActive)
            {
                throw new InvalidOperationException("La session shell n'est pas active");
            }

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var result = new CommandResult
            {
                CommandId = Guid.NewGuid().ToString(),
                ExecutedAt = DateTime.UtcNow
            };

            try
            {
                // Générer un marqueur de fin unique pour cette commande
                var endMarker = $"__END_MARKER_{Guid.NewGuid().ToString("N").Substring(0, 8)}__";
                
                lock (_lockObject)
                {
                    // Vider les queues avant d'exécuter la commande
                    while (_outputQueue.TryDequeue(out _)) { }
                    while (_errorQueue.TryDequeue(out _)) { }
                    _outputReady.Reset();
                    _errorReady.Reset();

                    // Construire la commande avec marqueur de fin
                    string fullCommand;
                    if (_shellType == CommandType.ExecutePowerShell)
                    {
                        // Pour PowerShell, utiliser Write-Host pour le marqueur
                        // Échapper les guillemets simples dans la commande si nécessaire
                        var escapedCommand = command.Replace("'", "''");
                        fullCommand = $"{escapedCommand}; Write-Host '{endMarker}'";
                    }
                    else if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                    {
                        // Pour CMD, utiliser echo (sans guillemets pour éviter les problèmes)
                        fullCommand = $"{command} & echo {endMarker}";
                    }
                    else
                    {
                        // Pour Bash, utiliser echo avec guillemets simples
                        fullCommand = $"{command}; echo '{endMarker}'";
                    }

                    // Envoyer la commande au shell
                    _stdin.WriteLine(fullCommand);
                    _stdin.Flush();
                }

                // Attendre la sortie avec timeout
                var outputBuilder = new StringBuilder();
                var errorBuilder = new StringBuilder();
                var startTime = DateTime.UtcNow;
                var timeout = TimeSpan.FromMilliseconds(timeoutMs);
                bool endMarkerFound = false;

                // Lire la sortie jusqu'à ce qu'on trouve le marqueur de fin ou timeout
                while ((DateTime.UtcNow - startTime) < timeout && !cancellationToken.IsCancellationRequested && !endMarkerFound)
                {
                    // Lire stdout
                    while (_outputQueue.TryDequeue(out string line))
                    {
                        if (line != null && line.Contains(endMarker))
                        {
                            // Marqueur trouvé, on a fini
                            endMarkerFound = true;
                            break;
                        }
                        if (line != null && !line.Contains(endMarker))
                        {
                            outputBuilder.AppendLine(line);
                        }
                        startTime = DateTime.UtcNow; // Réinitialiser le timeout si on reçoit des données
                    }

                    if (endMarkerFound)
                    {
                        break;
                    }

                    // Lire stderr
                    while (_errorQueue.TryDequeue(out string line))
                    {
                        if (line != null)
                        {
                            errorBuilder.AppendLine(line);
                        }
                        startTime = DateTime.UtcNow;
                    }

                    // Attendre un peu avant de vérifier à nouveau
                    await Task.Delay(50, cancellationToken);
                }

                stopwatch.Stop();
                result.Output = outputBuilder.ToString().TrimEnd();
                result.Error = errorBuilder.ToString().TrimEnd();
                result.ExecutionTimeMs = stopwatch.ElapsedMilliseconds;

                // Mettre à jour le répertoire courant si c'est une commande cd
                if (command.TrimStart().StartsWith("cd ", StringComparison.OrdinalIgnoreCase))
                {
                    UpdateWorkingDirectory(command);
                }

                if (endMarkerFound)
                {
                    result.Status = string.IsNullOrEmpty(result.Error) ? CommandStatus.Success : CommandStatus.Error;
                }
                else if ((DateTime.UtcNow - startTime) >= timeout)
                {
                    result.Status = CommandStatus.Timeout;
                    result.Error = "Timeout lors de l'exécution de la commande";
                }
                else
                {
                    result.Status = string.IsNullOrEmpty(result.Error) ? CommandStatus.Success : CommandStatus.Error;
                }

                result.ExitCode = 0; // Les shells interactifs ne retournent pas toujours de code de sortie
            }
            catch (OperationCanceledException)
            {
                stopwatch.Stop();
                result.Status = CommandStatus.Timeout;
                result.Error = "Commande annulée ou timeout";
                result.ExecutionTimeMs = stopwatch.ElapsedMilliseconds;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                result.Status = CommandStatus.Error;
                result.Error = ex.Message;
                result.ExecutionTimeMs = stopwatch.ElapsedMilliseconds;
            }

            return result;
        }

        /// <summary>
        /// Met à jour le répertoire courant basé sur la commande cd
        /// </summary>
        private void UpdateWorkingDirectory(string cdCommand)
        {
            try
            {
                var parts = cdCommand.Trim().Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2)
                {
                    var newPath = parts[1].Trim('"', '\'');
                    if (Path.IsPathRooted(newPath))
                    {
                        CurrentWorkingDirectory = newPath;
                    }
                    else
                    {
                        CurrentWorkingDirectory = Path.Combine(CurrentWorkingDirectory, newPath);
                    }
                    CurrentWorkingDirectory = Path.GetFullPath(CurrentWorkingDirectory);
                }
            }
            catch
            {
                // Ignorer les erreurs de parsing
            }
        }

        /// <summary>
        /// Ferme la session shell
        /// </summary>
        public void Close()
        {
            if (_shellProcess != null && !_shellProcess.HasExited)
            {
                try
                {
                    // Envoyer exit au shell
                    _stdin?.WriteLine(_shellType == CommandType.ExecutePowerShell ? "exit" : "exit");
                    _stdin?.Flush();
                    _stdin?.Close();

                    // Attendre un peu puis tuer si nécessaire
                    if (!_shellProcess.WaitForExit(2000))
                    {
                        _shellProcess.Kill();
                    }
                }
                catch
                {
                    // Ignorer les erreurs lors de la fermeture
                }
                finally
                {
                    _shellProcess?.Dispose();
                    _shellProcess = null;
                }
            }

            _stdin?.Dispose();
            _stdout?.Dispose();
            _stderr?.Dispose();
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                Close();
                _outputReady?.Dispose();
                _errorReady?.Dispose();
                _disposed = true;
            }
        }
    }
}

