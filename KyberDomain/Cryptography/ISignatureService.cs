namespace KyberDomain.Cryptography;

public interface ISignatureService
{
    (byte[] PublicKey, byte[] PrivateKey) GenerateKeyPair(SignatureStrength strength);
    byte[] Sign(byte[] data, byte[] privateKey, SignatureStrength strength);
    bool Verify(byte[] data, byte[] signature, byte[] publicKey, SignatureStrength strength);
}
