namespace KyberDomain.Cryptography;

public enum KemStrength
{
    Kyber512,
    Kyber768,
    Kyber1024
}

public enum SignatureStrength
{
    Dilithium2,
    Dilithium3,
    Dilithium5
}

public enum AeadAlgorithm
{
    ChaCha20Poly1305,
    AesGcm
}
