using KyberDomain.Cryptography;

namespace KyberLibrary.DomainAdapters;

internal sealed class KyberKeyEncapsulationService : IKeyEncapsulationService
{
    public (byte[] PublicKey, byte[] PrivateKey) GenerateKeyPair(KemStrength strength)
    {
        var wrapper = new KyberWrapper(Map(strength));
        return wrapper.GenerateKeyPair();
    }

    public (byte[] Ciphertext, byte[] SharedSecret) Encapsulate(byte[] peerPublicKey, KemStrength strength)
    {
        var wrapper = new KyberWrapper(Map(strength));
        return wrapper.Encapsulate(peerPublicKey);
    }

    public byte[] Decapsulate(byte[] ciphertext, byte[] privateKey, KemStrength strength)
    {
        var wrapper = new KyberWrapper(Map(strength));
        return wrapper.Decapsulate(ciphertext, privateKey);
    }

    private static KyberWrapper.KyberParameterSet Map(KemStrength strength) => strength switch
    {
        KemStrength.Kyber512 => KyberWrapper.KyberParameterSet.Kyber512,
        KemStrength.Kyber1024 => KyberWrapper.KyberParameterSet.Kyber1024,
        _ => KyberWrapper.KyberParameterSet.Kyber768,
    };
}
