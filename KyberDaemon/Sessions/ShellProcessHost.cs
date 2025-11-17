using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Channels;
using KyberDaemon.Configuration;
using KyberDaemon.Logging;
using System.Threading;

namespace KyberDaemon.Sessions;

public sealed class ShellProcessHost : IDisposable
{
    private readonly DaemonConfiguration _config;
    private readonly DaemonLogger _logger;
    private readonly Channel<string> _stdoutChannel;
    private readonly Channel<string> _stderrChannel;
    private readonly SemaphoreSlim _writeLock = new(1, 1);
    private Process? _process;
    private StreamWriter? _stdin;
    private bool _disposed;
    private bool _isPowerShell;

    public ShellProcessHost(DaemonConfiguration config, DaemonLogger logger)
    {
        _config = config;
        _logger = logger;
        _stdoutChannel = Channel.CreateUnbounded<string>(new UnboundedChannelOptions { SingleWriter = false, SingleReader = false });
        _stderrChannel = Channel.CreateUnbounded<string>(new UnboundedChannelOptions { SingleWriter = false, SingleReader = false });
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (_process != null)
        {
            throw new InvalidOperationException("Le processus shell est déjà démarré");
        }

        var (fileName, arguments, isPwsh) = GetShellInfo();
        _isPowerShell = isPwsh;

        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        _process = new Process
        {
            StartInfo = psi,
            EnableRaisingEvents = true
        };

        _process.OutputDataReceived += (_, e) =>
        {
            if (e.Data != null)
            {
                _stdoutChannel.Writer.TryWrite(e.Data);
            }
        };

        _process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data != null)
            {
                _stderrChannel.Writer.TryWrite(e.Data);
            }
        };

        if (!_process.Start())
        {
            throw new InvalidOperationException("Impossible de démarrer le processus shell");
        }

        _stdin = _process.StandardInput;
        _process.BeginOutputReadLine();
        _process.BeginErrorReadLine();

        // Attendre que le shell soit prêt
        await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken);
    }

    public async Task<CommandExecutionResult> ExecuteCommandAsync(string command, CancellationToken cancellationToken)
    {
        if (_process == null || _process.HasExited || _stdin == null)
        {
            throw new InvalidOperationException("Le processus shell n'est pas actif");
        }

        var marker = $"__KYBER_DONE_{Guid.NewGuid():N}";
        var commandWithMarker = BuildCommandWithMarker(command, marker);

        await _writeLock.WaitAsync(cancellationToken);
        try
        {
            await _stdin.WriteLineAsync(commandWithMarker);
            await _stdin.FlushAsync();
        }
        finally
        {
            _writeLock.Release();
        }

        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!await _stdoutChannel.Reader.WaitToReadAsync(cancellationToken))
            {
                break;
            }

            while (_stdoutChannel.Reader.TryRead(out var line))
            {
                if (line == marker)
                {
                    DrainErrors(errorBuilder);
                    return new CommandExecutionResult(outputBuilder.ToString().TrimEnd(), errorBuilder.ToString().TrimEnd());
                }

                outputBuilder.AppendLine(line);
            }

            DrainErrors(errorBuilder);
        }

        throw new IOException("Flux standard du shell fermé de manière inattendue");
    }

    private void DrainErrors(StringBuilder errorBuilder)
    {
        while (_stderrChannel.Reader.TryRead(out var line))
        {
            errorBuilder.AppendLine(line);
        }
    }

    private (string FileName, string Arguments, bool IsPowerShell) GetShellInfo()
    {
        if (OperatingSystem.IsWindows())
        {
            var requestedPath = string.IsNullOrWhiteSpace(_config.ShellPathWindows)
                ? "pwsh.exe"
                : _config.ShellPathWindows;

            var resolvedPath = ResolveExecutable(requestedPath) ?? ResolveExecutable("powershell.exe");
            if (resolvedPath == null)
            {
                throw new FileNotFoundException($"Impossible de localiser l'exécutable du shell Windows (configuré: {requestedPath})");
            }

            var args = string.IsNullOrWhiteSpace(_config.ShellArgsWindows)
                ? "-NoLogo -NoProfile"
                : _config.ShellArgsWindows;

            var isPwsh = Path.GetFileName(resolvedPath).Equals("pwsh.exe", StringComparison.OrdinalIgnoreCase) ||
                         Path.GetFileName(resolvedPath).Equals("powershell.exe", StringComparison.OrdinalIgnoreCase);

            return (resolvedPath, args, isPwsh);
        }

        var linuxPath = string.IsNullOrWhiteSpace(_config.ShellPathLinux)
            ? "/bin/bash"
            : _config.ShellPathLinux;
        var resolvedLinux = ResolveExecutable(linuxPath) ?? linuxPath;
        var linuxArgs = string.IsNullOrWhiteSpace(_config.ShellArgsLinux)
            ? "-i"
            : _config.ShellArgsLinux;
        return (resolvedLinux, linuxArgs, false);
    }

    private static string? ResolveExecutable(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        if (Path.IsPathRooted(path))
        {
            return File.Exists(path) ? path : null;
        }

        if (File.Exists(path))
        {
            return Path.GetFullPath(path);
        }

        var values = Environment.GetEnvironmentVariable("PATH")?.Split(Path.PathSeparator) ?? Array.Empty<string>();
        foreach (var value in values)
        {
            try
            {
                var candidate = Path.Combine(value.Trim(), path);
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }
            catch
            {
                // ignore invalid PATH entries
            }
        }

        return null;
    }

    private string BuildCommandWithMarker(string command, string marker)
    {
        if (_isPowerShell)
        {
            return string.IsNullOrWhiteSpace(command)
                ? $"Write-Output '{marker}'"
                : $"{command}; Write-Output '{marker}'";
        }

        if (OperatingSystem.IsWindows())
        {
            return string.IsNullOrWhiteSpace(command)
                ? $"echo {marker}"
                : $"{command} & echo {marker}";
        }

        return string.IsNullOrWhiteSpace(command)
            ? $"echo '{marker}'"
            : $"{command}; echo '{marker}'";
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        try
        {
            _stdoutChannel.Writer.TryComplete();
            _stderrChannel.Writer.TryComplete();

            if (_stdin != null)
            {
                try
                {
                    if (_stdin.BaseStream.CanWrite)
                    {
                        _stdin.WriteLine("exit");
                        _stdin.Flush();
                    }
                }
                catch
                {
                    // Ignorer les erreurs de fermeture
                }
                finally
                {
                    _stdin.Dispose();
                }
            }

            if (_process != null && !_process.HasExited)
            {
                _process.Kill(true);
                _process.WaitForExit(2000);
            }

            _process?.Dispose();
        }
        catch (Exception ex)
        {
            _logger.Warning("ShellProcessHost", $"Erreur lors de la fermeture du shell: {ex.Message}");
        }
        finally
        {
            _writeLock.Dispose();
        }
    }
}

public sealed record CommandExecutionResult(string Output, string Error);
