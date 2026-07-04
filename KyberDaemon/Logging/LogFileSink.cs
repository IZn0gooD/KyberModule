using System;
using System.IO;
using System.Linq;
using System.Text;

namespace KyberDaemon.Logging;

internal sealed class LogFileSink
{
    private readonly object _sync = new();
    private string? _directory;
    private string _fileName = "kyberd.log";
    private long _maxBytes = 10 * 1024 * 1024;
    private int _retention = 5;
    private bool _isConfigured;

    public void Configure(string? directory, string? fileName, int maxFileSizeMb, int retentionCount)
    {
        if (string.IsNullOrWhiteSpace(directory))
        {
            return;
        }

        lock (_sync)
        {
            _directory = directory;
            _fileName = string.IsNullOrWhiteSpace(fileName) ? "kyberd.log" : fileName.Trim();
            _maxBytes = Math.Max(1, maxFileSizeMb) * 1024L * 1024L;
            _retention = Math.Max(1, retentionCount);

            Directory.CreateDirectory(_directory);
            _isConfigured = true;
        }
    }

    public void Write(string line)
    {
        if (!_isConfigured || _directory is null)
        {
            return;
        }

        lock (_sync)
        {
            try
            {
                Directory.CreateDirectory(_directory);
                var currentPath = Path.Combine(_directory, _fileName);
                RotateIfNeeded(currentPath, line.Length);
                File.AppendAllText(currentPath, line + Environment.NewLine, Encoding.UTF8);
            }
            catch
            {
                // Best effort: ne pas interrompre le flux de logs en cas d'erreur disque
            }
        }
    }

    private void RotateIfNeeded(string currentPath, int incomingLength)
    {
        var fileInfo = new FileInfo(currentPath);
        if (fileInfo.Exists && fileInfo.Length + incomingLength > _maxBytes)
        {
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
            var baseName = Path.GetFileNameWithoutExtension(_fileName);
            var extension = Path.GetExtension(_fileName);
            var archivePath = Path.Combine(_directory!, $"{baseName}-{timestamp}{extension}");

            try
            {
                fileInfo.MoveTo(archivePath, overwrite: true);
            }
            catch
            {
                // Si la rotation échoue, tenter d'écrire dans le fichier courant malgré tout
                return;
            }

            PruneOldFiles(baseName, extension);
        }
    }

    private void PruneOldFiles(string baseName, string extension)
    {
        try
        {
            var pattern = $"{baseName}-*{extension}";
            var archives = Directory.GetFiles(_directory!, pattern)
                .Select(path => new FileInfo(path))
                .OrderByDescending(info => info.LastWriteTimeUtc)
                .ToList();

            for (var i = _retention - 1; i < archives.Count; i++)
            {
                try
                {
                    archives[i].Delete();
                }
                catch
                {
                    // Ignorer les erreurs de suppression
                }
            }
        }
        catch
        {
            // Ignorer les erreurs liées au nettoyage
        }
    }
}
