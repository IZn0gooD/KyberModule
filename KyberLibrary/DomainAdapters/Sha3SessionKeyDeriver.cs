using System;
using KyberDomain.Cryptography;

namespace KyberLibrary.DomainAdapters;

public sealed class Sha3SessionKeyDeriver : ISessionKeyDeriver
{
    public byte[] Derive(byte[] sharedSecret)
    {
        if (sharedSecret is null)
        {
            throw new ArgumentNullException(nameof(sharedSecret));
        }

        return SHA3Wrapper.ComputeSHA3_256(sharedSecret);
    }
}
