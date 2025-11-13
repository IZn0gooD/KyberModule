using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace KyberKeyManagement;

public sealed class KeyEncryptionService
{
    private readonly KeyEncryptionMode _mode;
    private readonly byte[]? _entropy;
    private readonly string? _passphrase;
    private readonly int _iterations;
    private readonly int _tagSize;

    public KeyEncryptionService(KeyStorageOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _iterations = 200_000;
        _tagSize = 16;

        switch (options.EncryptionMode)
        {
            case KeyEncryptionMode.Dpapi:
                if (!OperatingSystem.IsWindows())
                {
                    throw new InvalidOperationException("DPAPI n'est disponible que sous Windows");
                }
                _mode = KeyEncryptionMode.Dpapi;
                _entropy = string.IsNullOrWhiteSpace(options.Passphrase) ? null : Encoding.UTF8.GetBytes(options.Passphrase);
                break;
            case KeyEncryptionMode.Passphrase:
                if (string.IsNullOrWhiteSpace(options.Passphrase))
                {
                    throw new InvalidOperationException("Une passphrase est requise lorsque key_encryption_mode=passphrase");
                }
                _mode = KeyEncryptionMode.Passphrase;
                _passphrase = options.Passphrase;
                break;
            default:
                throw new InvalidOperationException($"Mode de chiffrement non supporté: {options.EncryptionMode}");
        }
    }

    public string Mode => _mode == KeyEncryptionMode.Dpapi ? "dpapi" : "passphrase";

    public byte[] Encrypt(byte[] plaintext)
    {
        if (plaintext is null)
        {
            throw new ArgumentNullException(nameof(plaintext));
        }

        var envelope = new KeyEnvelope
        {
            Mode = Mode
        };

        switch (_mode)
        {
            case KeyEncryptionMode.Dpapi:
            {
                if (!OperatingSystem.IsWindows())
                {
                    throw new SecurityException("DPAPI n'est pas disponible sur cette plateforme");
                }
                var cipher = ProtectedData.Protect(plaintext, _entropy, DataProtectionScope.LocalMachine);
                envelope.Data = Convert.ToBase64String(cipher);
                break;
            }
            case KeyEncryptionMode.Passphrase:
            {
                var salt = RandomNumberGenerator.GetBytes(16);
                var iv = RandomNumberGenerator.GetBytes(12);
                var key = DeriveKey(_passphrase!, salt, _iterations);
                var ciphertext = new byte[plaintext.Length];
                var tag = new byte[_tagSize];
                using var aes = new AesGcm(key, _tagSize);
                aes.Encrypt(iv, plaintext, ciphertext, tag);

                envelope.Salt = Convert.ToBase64String(salt);
                envelope.Iv = Convert.ToBase64String(iv);
                envelope.Tag = Convert.ToBase64String(tag);
                envelope.Iterations = _iterations;
                envelope.Data = Convert.ToBase64String(ciphertext);
                break;
            }
            default:
                throw new SecurityException("Mode de chiffrement des clés non supporté");
        }

        return JsonSerializer.SerializeToUtf8Bytes(envelope);
    }

    public byte[] Decrypt(byte[] blob)
    {
        if (blob is null)
        {
            throw new ArgumentNullException(nameof(blob));
        }

        var envelope = JsonSerializer.Deserialize<KeyEnvelope>(blob) ?? throw new SecurityException("Enveloppe de clé invalide");
        var mode = (envelope.Mode ?? string.Empty).Trim().ToLowerInvariant();

        if (mode == "dpapi")
        {
            if (!OperatingSystem.IsWindows())
            {
                throw new SecurityException("Impossible de déchiffrer une clé protégée via DPAPI sur cette plateforme");
            }
            var cipher = Convert.FromBase64String(envelope.Data ?? throw new SecurityException("Données chiffrées manquantes"));
            return ProtectedData.Unprotect(cipher, _entropy, DataProtectionScope.LocalMachine);
        }

        if (mode == "passphrase")
        {
            if (string.IsNullOrWhiteSpace(_passphrase))
            {
                throw new SecurityException("Aucune passphrase configurée pour déchiffrer la clé");
            }

            var salt = Convert.FromBase64String(envelope.Salt ?? throw new SecurityException("Sel manquant"));
            var iv = Convert.FromBase64String(envelope.Iv ?? throw new SecurityException("IV manquant"));
            var tag = Convert.FromBase64String(envelope.Tag ?? throw new SecurityException("Tag manquant"));
            var data = Convert.FromBase64String(envelope.Data ?? throw new SecurityException("Données chiffrées manquantes"));
            var iterations = envelope.Iterations ?? _iterations;

            var key = DeriveKey(_passphrase, salt, iterations);
            var plaintext = new byte[data.Length];
            using var aes = new AesGcm(key, tag.Length);
            aes.Decrypt(iv, data, tag, plaintext);
            return plaintext;
        }

        throw new SecurityException($"Mode de chiffrement inconnu dans l'enveloppe: {envelope.Mode}");
    }

    private static byte[] DeriveKey(string passphrase, byte[] salt, int iterations)
    {
        using var pbkdf = new Rfc2898DeriveBytes(passphrase, salt, iterations, HashAlgorithmName.SHA256);
        return pbkdf.GetBytes(32);
    }

    private sealed class KeyEnvelope
    {
        [JsonPropertyName("mode")]
        public string Mode { get; set; } = string.Empty;

        [JsonPropertyName("data")]
        public string? Data { get; set; }

        [JsonPropertyName("salt")]
        public string? Salt { get; set; }

        [JsonPropertyName("iv")]
        public string? Iv { get; set; }

        [JsonPropertyName("tag")]
        public string? Tag { get; set; }

        [JsonPropertyName("iterations")]
        public int? Iterations { get; set; }
    }
}
