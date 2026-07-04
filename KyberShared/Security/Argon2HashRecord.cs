using System;
using System.Globalization;

namespace KyberShared.Security;

public sealed record Argon2HashRecord(
    string Algorithm,
    int Version,
    int MemorySize,
    int Iterations,
    int Parallelism,
    byte[] Salt,
    byte[] Hash)
{
    public static bool TryParse(string? record, out Argon2HashRecord? result)
    {
        result = null;
        if (string.IsNullOrWhiteSpace(record))
        {
            return false;
        }

        var parts = record.Split('$', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length < 5)
        {
            return false;
        }

        var algorithm = parts[0];
        if (!algorithm.StartsWith("argon2", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var versionPart = parts[1];
        var memoryPart = parts[2];
        var saltPart = parts[3];
        var hashPart = parts[4];

        if (!versionPart.StartsWith("v=", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!int.TryParse(versionPart[2..], out var version))
        {
            return false;
        }

        var parameters = memoryPart.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        int memory = 0, iterations = 0, parallelism = 0;
        foreach (var parameter in parameters)
        {
            var kv = parameter.Split('=', 2, StringSplitOptions.TrimEntries);
            if (kv.Length != 2)
            {
                continue;
            }

            switch (kv[0])
            {
                case "m":
                    int.TryParse(kv[1], out memory);
                    break;
                case "t":
                    int.TryParse(kv[1], out iterations);
                    break;
                case "p":
                    int.TryParse(kv[1], out parallelism);
                    break;
            }
        }

        if (memory <= 0 || iterations <= 0 || parallelism <= 0)
        {
            return false;
        }

        try
        {
            var salt = Convert.FromBase64String(saltPart);
            var hash = Convert.FromBase64String(hashPart);
            result = new Argon2HashRecord(algorithm, version, memory, iterations, parallelism, salt, hash);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public override string ToString()
    {
        var salt = Convert.ToBase64String(Salt);
        var hash = Convert.ToBase64String(Hash);
        return string.Format(CultureInfo.InvariantCulture, "{0}$v={1}$m={2},t={3},p={4}${5}${6}", Algorithm, Version, MemorySize, Iterations, Parallelism, salt, hash);
    }
}
