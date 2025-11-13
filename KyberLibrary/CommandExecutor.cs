using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace KyberLibrary
{
    /// <summary>
    /// Exécuteur de commandes sécurisé
    /// </summary>
    [Obsolete("Utilisé uniquement par l'ancienne implémentation.")]
    public class CommandExecutor
    {
        /// <summary>
        /// Exécute une commande shell
        /// </summary>
        public static async Task<CommandResult> ExecuteCommandAsync(SecureCommand command, CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = new CommandResult
            {
                CommandId = command.CommandId,
                ExecutedAt = DateTime.UtcNow
            };

            try
            {
                ProcessStartInfo startInfo;

                // Déterminer le shell selon l'OS
                if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                {
                    // Windows : PowerShell ou CMD
                    if (command.Type == CommandType.ExecutePowerShell)
                    {
                        startInfo = new ProcessStartInfo
                        {
                            FileName = "powershell.exe",
                            Arguments = $"-NoProfile -NonInteractive -Command \"{command.Command}\"",
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            CreateNoWindow = true,
                            WorkingDirectory = string.IsNullOrEmpty(command.WorkingDirectory) ? Environment.CurrentDirectory : command.WorkingDirectory
                        };
                    }
                    else
                    {
                        // CMD
                        startInfo = new ProcessStartInfo
                        {
                            FileName = "cmd.exe",
                            Arguments = $"/c \"{command.Command}\"",
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            CreateNoWindow = true,
                            WorkingDirectory = string.IsNullOrEmpty(command.WorkingDirectory) ? Environment.CurrentDirectory : command.WorkingDirectory
                        };
                    }
                }
                else
                {
                    // Linux/Unix : Bash
                    startInfo = new ProcessStartInfo
                    {
                        FileName = "/bin/bash",
                        Arguments = $"-c \"{command.Command}\"",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true,
                        WorkingDirectory = string.IsNullOrEmpty(command.WorkingDirectory) ? Environment.CurrentDirectory : command.WorkingDirectory
                    };
                }

                using (var process = new Process { StartInfo = startInfo })
                {
                    var outputBuilder = new StringBuilder();
                    var errorBuilder = new StringBuilder();

                    process.OutputDataReceived += (sender, e) =>
                    {
                        if (e.Data != null)
                        {
                            outputBuilder.AppendLine(e.Data);
                        }
                    };

                    process.ErrorDataReceived += (sender, e) =>
                    {
                        if (e.Data != null)
                        {
                            errorBuilder.AppendLine(e.Data);
                        }
                    };

                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    // Attendre la fin avec timeout
                    var timeout = command.Timeout > 0 ? command.Timeout : Timeout.Infinite;
                    var completed = await Task.Run(() => process.WaitForExit(timeout), cancellationToken);

                    if (!completed)
                    {
                        process.Kill();
                        stopwatch.Stop();
                        result.Status = CommandStatus.Timeout;
                        result.Error = $"Commande timeout après {timeout}ms";
                        result.ExecutionTimeMs = stopwatch.ElapsedMilliseconds;
                        return result;
                    }

                    await Task.Run(() => process.WaitForExit(), cancellationToken);

                    stopwatch.Stop();
                    result.ExitCode = process.ExitCode;
                    result.Output = outputBuilder.ToString();
                    result.Error = errorBuilder.ToString();
                    result.ExecutionTimeMs = stopwatch.ElapsedMilliseconds;

                    if (process.ExitCode == 0 && string.IsNullOrEmpty(result.Error))
                    {
                        result.Status = CommandStatus.Success;
                    }
                    else
                    {
                        result.Status = CommandStatus.Error;
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                stopwatch.Stop();
                result.Status = CommandStatus.AccessDenied;
                result.Error = "Accès refusé pour exécuter cette commande";
                result.ExecutionTimeMs = stopwatch.ElapsedMilliseconds;
            }
            catch (FileNotFoundException)
            {
                stopwatch.Stop();
                result.Status = CommandStatus.NotFound;
                result.Error = "Commande non trouvée";
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
        /// Exécute une commande de liste de répertoire
        /// </summary>
        public static CommandResult ListDirectory(string path)
        {
            try
            {
                if (!Directory.Exists(path))
                {
                    return CommandResult.CreateError(Guid.NewGuid().ToString(), $"Répertoire non trouvé: {path}");
                }

                var files = Directory.GetFiles(path);
                var directories = Directory.GetDirectories(path);
                var output = new StringBuilder();

                output.AppendLine($"Répertoire: {path}");
                output.AppendLine("");

                output.AppendLine("Répertoires:");
                foreach (var dir in directories)
                {
                    var dirInfo = new DirectoryInfo(dir);
                    output.AppendLine($"  [DIR]  {dirInfo.Name} ({dirInfo.CreationTime:yyyy-MM-dd HH:mm:ss})");
                }

                output.AppendLine("");
                output.AppendLine("Fichiers:");
                foreach (var file in files)
                {
                    var fileInfo = new FileInfo(file);
                    output.AppendLine($"  [FILE] {fileInfo.Name} ({fileInfo.Length} bytes, {fileInfo.LastWriteTime:yyyy-MM-dd HH:mm:ss})");
                }

                return CommandResult.CreateSuccess(Guid.NewGuid().ToString(), output.ToString());
            }
            catch (Exception ex)
            {
                return CommandResult.CreateError(Guid.NewGuid().ToString(), ex.Message);
            }
        }

        /// <summary>
        /// Lit un fichier
        /// </summary>
        public static CommandResult ReadFile(string path, int maxSize = 1024 * 1024) // 1 MB par défaut
        {
            try
            {
                if (!File.Exists(path))
                {
                    return CommandResult.CreateError(Guid.NewGuid().ToString(), $"Fichier non trouvé: {path}");
                }

                var fileInfo = new FileInfo(path);
                if (fileInfo.Length > maxSize)
                {
                    return CommandResult.CreateError(Guid.NewGuid().ToString(), $"Fichier trop volumineux: {fileInfo.Length} bytes (max: {maxSize} bytes)");
                }

                var content = File.ReadAllText(path);
                return CommandResult.CreateSuccess(Guid.NewGuid().ToString(), content);
            }
            catch (Exception ex)
            {
                return CommandResult.CreateError(Guid.NewGuid().ToString(), ex.Message);
            }
        }

        /// <summary>
        /// Obtient des informations système
        /// </summary>
        public static CommandResult GetSystemInfo()
        {
            try
            {
                var output = new StringBuilder();
                output.AppendLine("=== Informations Système ===");
                output.AppendLine($"OS: {Environment.OSVersion}");
                output.AppendLine($"Version: {Environment.OSVersion.Version}");
                output.AppendLine($"Machine: {Environment.MachineName}");
                output.AppendLine($"Utilisateur: {Environment.UserName}");
                output.AppendLine($"Répertoire courant: {Environment.CurrentDirectory}");
                output.AppendLine($"Processeurs: {Environment.ProcessorCount}");
                output.AppendLine($"Version .NET: {Environment.Version}");
                output.AppendLine($"64-bit OS: {Environment.Is64BitOperatingSystem}");
                output.AppendLine($"64-bit Process: {Environment.Is64BitProcess}");

                return CommandResult.CreateSuccess(Guid.NewGuid().ToString(), output.ToString());
            }
            catch (Exception ex)
            {
                return CommandResult.CreateError(Guid.NewGuid().ToString(), ex.Message);
            }
        }
    }
}

