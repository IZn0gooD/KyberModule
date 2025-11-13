using System;
using KyberDomain.Cryptography;
using KyberDomain.Security;

namespace KyberLibrary.DomainAdapters;

public sealed class SecureSessionFactory : ISecureSessionFactory
{
    private readonly IKeyEncapsulationService _kem;
    private readonly ISignatureService _signature;
    private readonly IAeadCipherProvider _aead;
    private readonly INonceGenerator _nonceGenerator;
    private readonly ISessionKeyDeriver _keyDeriver;
    private readonly IKeyMaterialProtector _keyProtector;
    private readonly ISideChannelDefense _sideChannelDefense;

    public SecureSessionFactory()
        : this(new KyberKeyEncapsulationService(),
               new DilithiumSignatureService(),
               new AeadCipherProvider(),
               new SecureRandomNonceGenerator(),
               new Sha3SessionKeyDeriver(),
               new KeyMaterialProtector(),
               new AdvancedSideChannelDefense())
    {
    }

    public SecureSessionFactory(
        IKeyEncapsulationService kem,
        ISignatureService signature,
        IAeadCipherProvider aead,
        INonceGenerator nonceGenerator,
        ISessionKeyDeriver keyDeriver,
        IKeyMaterialProtector keyProtector,
        ISideChannelDefense sideChannelDefense)
    {
        _kem = kem ?? throw new ArgumentNullException(nameof(kem));
        _signature = signature ?? throw new ArgumentNullException(nameof(signature));
        _aead = aead ?? throw new ArgumentNullException(nameof(aead));
        _nonceGenerator = nonceGenerator ?? throw new ArgumentNullException(nameof(nonceGenerator));
        _keyDeriver = keyDeriver ?? throw new ArgumentNullException(nameof(keyDeriver));
        _keyProtector = keyProtector ?? throw new ArgumentNullException(nameof(keyProtector));
        _sideChannelDefense = sideChannelDefense ?? new NoopSideChannelDefense();
    }

    public SecureSession Create(SecureSessionOptions options)
    {
        return new SecureSession(options,
            _kem,
            _signature,
            _aead,
            _nonceGenerator,
            _keyDeriver,
            _keyProtector,
            _sideChannelDefense);
    }
}
