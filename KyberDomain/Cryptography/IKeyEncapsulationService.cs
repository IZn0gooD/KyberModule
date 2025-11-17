using System;

namespace KyberDomain.Cryptography;

public interface IKeyEncapsulationService
{
    (byte[] PublicKey, byte[] PrivateKey) GenerateKeyPair(KemStrength strength);
    (byte[] Ciphertext, byte[] SharedSecret) Encapsulate(byte[] peerPublicKey, KemStrength strength);
    byte[] Decapsulate(byte[] ciphertext, byte[] privateKey, KemStrength strength);
}
