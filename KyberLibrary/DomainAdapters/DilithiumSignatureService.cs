using KyberDomain.Cryptography;

namespace KyberLibrary.DomainAdapters;

public sealed class DilithiumSignatureService : ISignatureService
{
    public (byte[] PublicKey, byte[] PrivateKey) GenerateKeyPair(SignatureStrength strength)
    {
        var wrapper = new DilithiumWrapper(Map(strength));
        return wrapper.GenerateKeyPair();
    }

    public byte[] Sign(byte[] data, byte[] privateKey, SignatureStrength strength)
    {
        var wrapper = new DilithiumWrapper(Map(strength));
        return wrapper.Sign(data, privateKey, preHash: true);
    }

    public bool Verify(byte[] data, byte[] signature, byte[] publicKey, SignatureStrength strength)
    {
        var wrapper = new DilithiumWrapper(Map(strength));
        return wrapper.Verify(data, signature, publicKey, preHash: true);
    }

    private static DilithiumWrapper.DilithiumParameterSet Map(SignatureStrength strength) => strength switch
    {
        SignatureStrength.Dilithium2 => DilithiumWrapper.DilithiumParameterSet.Dilithium2,
        SignatureStrength.Dilithium5 => DilithiumWrapper.DilithiumParameterSet.Dilithium5,
        _ => DilithiumWrapper.DilithiumParameterSet.Dilithium3,
    };
}
