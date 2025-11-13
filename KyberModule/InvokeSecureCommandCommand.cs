using System;
using System.Diagnostics;
using System.IO;
using System.Management.Automation;
using System.Runtime.InteropServices;
using System.Text;

namespace KyberModule
{
    /// <summary>
    /// Cmdlet pour exécuter une commande distante via KyberCLI
    /// </summary>
    [Cmdlet(VerbsLifecycle.Invoke, "SecureCommand", DefaultParameterSetName = ParameterSetPassword)]
    [OutputType(typeof(KyberCLIResult))]
    public class InvokeSecureCommandCommand : PSCmdlet
    {
        private const string ParameterSetPassword = "Password";
        private const string ParameterSetKey = "Key";

        [Parameter(Mandatory = true, Position = 0)]
        public string ServerHost { get; set; }

        [Parameter(Mandatory = false)]
        public int Port { get; set; } = 8443;

        [Parameter(Mandatory = true)]
        public string Username { get; set; }

        [Parameter(Mandatory = true, ParameterSetName = ParameterSetPassword)]
        public string PasswordHex { get; set; }

        [Parameter(Mandatory = true, ParameterSetName = ParameterSetKey)]
        public string KeyPath { get; set; }

        [Parameter(Mandatory = true)]
        public string Command { get; set; }

        [Parameter(Mandatory = false, HelpMessage = "Chemin explicite vers l'exécutable KyberCLI (dll ou exe)")]
        public string CliPath { get; set; }

        [Parameter(Mandatory = false)]
        public SwitchParameter RebuildCli { get; set; }

        protected override void ProcessRecord()
        {
            try
            {
                var cliExecutable = ResolveCliBinary();
                if (RebuildCli.IsPresent)
                {
                    BuildCli(cliExecutable.IsDotNet); // rebuild avant utilisation
                }
                else if (!File.Exists(cliExecutable.Path))
                {
                    WriteVerbose("KyberCLI introuvable, tentative de compilation en Release");
                    BuildCli(cliExecutable.IsDotNet);
                }

                if (!File.Exists(cliExecutable.Path))
                {
                    throw new FileNotFoundException("Impossible de localiser KyberCLI. Utilisez --RebuildCli pour forcer la compilation.", cliExecutable.Path);
                }

                var processInfo = CreateProcessStartInfo(cliExecutable);
                WriteVerbose($"Appel de {processInfo.FileName} {processInfo.Arguments}");

                using var process = Process.Start(processInfo);
                if (process == null)
                {
                    throw new InvalidOperationException("Impossible de démarrer KyberCLI");
                }

                var stdout = process.StandardOutput.ReadToEnd();
                var stderr = process.StandardError.ReadToEnd();
                process.WaitForExit();

                var result = new KyberCLIResult
                {
                    ExitCode = process.ExitCode,
                    Output = stdout.Trim(),
                    Error = stderr.Trim()
                };

                if (!string.IsNullOrEmpty(result.Output))
                {
                    WriteObject(result.Output);
                }

                if (!string.IsNullOrEmpty(result.Error))
                {
                    WriteWarning(result.Error);
                }

                if (process.ExitCode != 0)
                {
                    WriteError(new ErrorRecord(
                        new InvalidOperationException(result.Error.Length > 0 ? result.Error : "Exécution KyberCLI échouée"),
                        "KyberCLIExecutionFailed",
                        ErrorCategory.InvalidResult,
                        Command));
                }

                WriteObject(result);
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(ex, "InvokeSecureCommandError", ErrorCategory.InvalidOperation, Command));
            }
        }

        private (string Path, bool IsDotNet) ResolveCliBinary()
        {
            if (!string.IsNullOrEmpty(CliPath))
            {
                return (Path.GetFullPath(CliPath), Path.GetExtension(CliPath).Equals(".dll", StringComparison.OrdinalIgnoreCase));
            }

            var moduleBase = this.MyInvocation.MyCommand.Module?.ModuleBase
                             ?? Path.GetDirectoryName(typeof(InvokeSecureCommandCommand).Assembly.Location)
                             ?? Environment.CurrentDirectory;

            bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            string[] candidates = isWindows
                ? new[]
                {
                    Path.Combine(moduleBase, "..", "KyberCLI", "bin", "Release", "net8.0", "publish", "KyberCLI.exe"),
                    Path.Combine(moduleBase, "..", "KyberCLI", "bin", "Release", "net8.0", "KyberCLI.exe"),
                    Path.Combine(moduleBase, "..", "KyberCLI", "bin", "Release", "net8.0", "KyberCLI.dll")
                }
                : new[]
                {
                    Path.Combine(moduleBase, "..", "KyberCLI", "bin", "Release", "net8.0", "publish", "KyberCLI"),
                    Path.Combine(moduleBase, "..", "KyberCLI", "bin", "Release", "net8.0", "KyberCLI"),
                    Path.Combine(moduleBase, "..", "KyberCLI", "bin", "Release", "net8.0", "KyberCLI.dll")
                };

            foreach (var candidate in candidates)
            {
                var fullPath = Path.GetFullPath(candidate);
                if (File.Exists(fullPath))
                {
                    return (fullPath, Path.GetExtension(fullPath).Equals(".dll", StringComparison.OrdinalIgnoreCase));
                }
            }

            var fallback = candidates[candidates.Length - 1];
            return (Path.GetFullPath(fallback), Path.GetExtension(fallback).Equals(".dll", StringComparison.OrdinalIgnoreCase));
        }

        private void BuildCli(bool targetDll)
        {
            var moduleBase = this.MyInvocation.MyCommand.Module?.ModuleBase
                             ?? Path.GetDirectoryName(typeof(InvokeSecureCommandCommand).Assembly.Location)
                             ?? Environment.CurrentDirectory;
            var cliProject = Path.GetFullPath(Path.Combine(moduleBase, "..", "KyberCLI", "KyberCLI.csproj"));

            var arguments = targetDll
                ? $"publish \"{cliProject}\" -c Release"
                : $"publish \"{cliProject}\" -c Release";

            var startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = arguments,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process == null)
            {
                throw new InvalidOperationException("Impossible de lancer 'dotnet publish' pour KyberCLI");
            }

            var stdout = process.StandardOutput.ReadToEnd();
            var stderr = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException($"dotnet publish a échoué: {stderr}\n{stdout}");
            }
        }

        private ProcessStartInfo CreateProcessStartInfo((string Path, bool IsDotNet) cliExecutable)
        {
            var arguments = new StringBuilder();
            arguments.Append("connect ");
            arguments.Append("--host \"").Append(ServerHost).Append("\" ");
            arguments.Append("--port ").Append(Port).Append(' ');
            arguments.Append("--user \"").Append(Username).Append("\" ");

            if (!string.IsNullOrEmpty(PasswordHex))
            {
                arguments.Append("--password \"").Append(PasswordHex).Append("\" ");
            }
            if (!string.IsNullOrEmpty(KeyPath))
            {
                arguments.Append("--key \"").Append(Path.GetFullPath(KeyPath)).Append("\" ");
            }
            arguments.Append("--command \"").Append(Command).Append("\"");

            if (cliExecutable.IsDotNet)
            {
                return new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = $"\"{cliExecutable.Path}\" {arguments}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
            }

            return new ProcessStartInfo
            {
                FileName = cliExecutable.Path,
                Arguments = arguments.ToString(),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
        }
    }

    public sealed class KyberCLIResult
    {
        public int ExitCode { get; set; }
        public string Output { get; set; }
        public string Error { get; set; }
    }
}

