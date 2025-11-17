using KyberDomain.Cryptography;

namespace KyberLibrary.DomainAdapters;

internal sealed class AeadCipherProvider : IAeadCipherProvider
{
    public AeadCipherResult Encrypt(byte[] key, byte[] plaintext, AeadAlgorithm algorithm)
    {
        return algorithm switch
        {
            AeadAlgorithm.AesGcm => Wrap(AESGCMWrapper.Encrypt(plaintext, key)),
            _ => Wrap(ChaCha20Poly1305Wrapper.Encrypt(plaintext, key))
        };
    }

    public byte[] Decrypt(byte[] key, byte[] ciphertextWithTag, byte[] nonce, AeadAlgorithm algorithm)
    {
        return algorithm switch
        {
            AeadAlgorithm.AesGcm => AESGCMWrapper.Decrypt(ciphertextWithTag, key, nonce),
            _ => ChaCha20Poly1305Wrapper.Decrypt(ciphertextWithTag, key, nonce)
        };
    }

    private static AeadCipherResult Wrap((byte[] CiphertextWithTag, byte[] Nonce) result)
        => new(result.CiphertextWithTag, result.Nonce);
}
