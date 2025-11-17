using System;
using KyberDomain.Security;
using Org.BouncyCastle.Security;

namespace KyberLibrary.DomainAdapters;

internal sealed class SecureRandomNonceGenerator : INonceGenerator
{
    private readonly SecureRandom _secureRandom = new();

    public byte[] Generate(int size)
    {
        if (size <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(size));
        }

        var buffer = new byte[size];
        _secureRandom.NextBytes(buffer);
        return buffer;
    }
}
