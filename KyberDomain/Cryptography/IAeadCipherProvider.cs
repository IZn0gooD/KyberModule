namespace KyberDomain.Cryptography;

public readonly struct AeadCipherResult
{
    public AeadCipherResult(byte[] ciphertextWithTag, byte[] nonce)
    {
        CiphertextWithTag = ciphertextWithTag;
        Nonce = nonce;
    }

    public byte[] CiphertextWithTag { get; }
    public byte[] Nonce { get; }
}

public interface IAeadCipherProvider
{
    AeadCipherResult Encrypt(byte[] key, byte[] plaintext, AeadAlgorithm algorithm);
    byte[] Decrypt(byte[] key, byte[] ciphertextWithTag, byte[] nonce, AeadAlgorithm algorithm);
}
