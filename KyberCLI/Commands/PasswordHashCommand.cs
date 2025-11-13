using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Konscious.Security.Cryptography;
using KyberShared.Security;

namespace KyberCLI.Commands;

public sealed class PasswordHashCommand : IKyberCommand
{
    private readonly Options _options;

    private PasswordHashCommand(Options options)
    {
        _options = options;
    }

    public static PasswordHashCommand Parse(string[] args)
    {
        var options = new Options();
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--username":
                    options.Username = RequireArgument(args, ++i, "--username");
                    break;
                case "--password":
                    options.Password = RequireArgument(args, ++i, "--password");
                    break;
                case "--password-file":
                    options.Password = File.ReadAllText(RequireArgument(args, ++i, "--password-file")).Trim();
                    break;
                case "--salt":
                    options.Salt = ParseSalt(RequireArgument(args, ++i, "--salt"));
                    break;
                case "--memory":
                    options.MemoryKb = int.Parse(RequireArgument(args, ++i, "--memory"));
                    break;
                case "--iterations":
                    options.Iterations = int.Parse(RequireArgument(args, ++i, "--iterations"));
                    break;
                case "--parallelism":
                    options.Parallelism = int.Parse(RequireArgument(args, ++i, "--parallelism"));
                    break;
                case "--length":
                    options.HashLength = int.Parse(RequireArgument(args, ++i, "--length"));
                    break;
                default:
                    throw new CommandLineException($"Option inconnue: {args[i]}");
            }
        }

        if (string.IsNullOrEmpty(options.Password))
        {
            throw new CommandLineException("--password est requis (ou --password-file)");
        }

        return new PasswordHashCommand(options);
    }

    public Task<int> ExecuteAsync()
    {
        var salt = _options.Salt ?? GenerateSalt(16);
        var hash = DeriveArgon2(_options.Password!, salt, _options);
        var record = new Argon2HashRecord("argon2id", 19, _options.MemoryKb, _options.Iterations, _options.Parallelism, salt, hash);

        Console.WriteLine("Enregistrement Argon2id :");
        Console.WriteLine(record.ToString());
        Console.WriteLine();

        Console.WriteLine("Secret dérivé (hex) :");
        Console.WriteLine("0x" + Convert.ToHexString(hash));
        Console.WriteLine();

        if (!string.IsNullOrEmpty(_options.Username))
        {
            Console.WriteLine("Entrée auth.conf :");
            Console.WriteLine($"{_options.Username}:password:{record}");
        }

        return Task.FromResult(0);
    }

    private static string RequireArgument(string[] args, int index, string option)
    {
        if (index >= args.Length)
        {
            throw new CommandLineException($"Argument manquant pour {option}");
        }
        return args[index];
    }

    private static byte[] ParseSalt(string value)
    {
        if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            value = value[2..];
        }

        if (value.Length % 2 == 0 && value.All(Uri.IsHexDigit))
        {
            var bytes = new byte[value.Length / 2];
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = Convert.ToByte(value.Substring(i * 2, 2), 16);
            }
            return bytes;
        }

        try
        {
            return Convert.FromBase64String(value);
        }
        catch (FormatException)
        {
            throw new CommandLineException("Sel invalide: fournir hex (0x...) ou Base64");
        }
    }

    private static byte[] GenerateSalt(int length)
    {
        var salt = new byte[length];
        RandomNumberGenerator.Fill(salt);
        return salt;
    }

    private static byte[] DeriveArgon2(string password, byte[] salt, Options options)
    {
        var passwordBytes = Encoding.UTF8.GetBytes(password);
        using var argon = new Argon2id(passwordBytes)
        {
            Salt = salt,
            DegreeOfParallelism = Math.Max(1, options.Parallelism),
            Iterations = Math.Max(1, options.Iterations),
            MemorySize = Math.Max(8, options.MemoryKb)
        };

        return argon.GetBytes(options.HashLength);
    }

    private sealed class Options
    {
        public string? Username { get; set; }
        public string? Password { get; set; }
        public byte[]? Salt { get; set; }
        public int MemoryKb { get; set; } = 65536;
        public int Iterations { get; set; } = 3;
        public int Parallelism { get; set; } = Environment.ProcessorCount > 1 ? 2 : 1;
        public int HashLength { get; set; } = 32;
    }
}
