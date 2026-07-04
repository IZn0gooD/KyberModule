namespace KyberDomain.Cryptography;

public sealed class SecureSessionOptions
{
    public KemStrength KemStrength { get; set; } = KemStrength.Kyber768;
    public SignatureStrength SignatureStrength { get; set; } = SignatureStrength.Dilithium3;
    public AeadAlgorithm AeadAlgorithm { get; set; } = AeadAlgorithm.ChaCha20Poly1305;
}
