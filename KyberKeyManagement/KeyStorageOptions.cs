namespace KyberKeyManagement;

public sealed record KeyStorageOptions
{
    public string KeysDirectory { get; init; } = string.Empty;
    public KeyEncryptionMode EncryptionMode { get; init; } = KeyEncryptionMode.Passphrase;
    public string? Passphrase { get; init; }
        = null;
    public int RotationDays { get; init; }
        = 0;
}

public enum KeyEncryptionMode
{
    Dpapi,
    Passphrase
}
