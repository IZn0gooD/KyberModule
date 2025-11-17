using System;
using System.Collections.Generic;
using System.Security;
using KyberDomain.Security;

namespace KyberDomain.Cryptography;

public sealed class SecureSession : IDisposable
{
    private readonly IKeyEncapsulationService _kemService;
    private readonly ISignatureService _signatureService;
    private readonly IAeadCipherProvider _aeadProvider;
    private readonly INonceGenerator _nonceGenerator;
    private readonly ISessionKeyDeriver _sessionKeyDeriver;
    private readonly IKeyMaterialProtector _keyProtector;
    private readonly ISideChannelDefense _sideChannelDefense;
    private readonly SecureSessionOptions _options;
    private readonly object _sync = new();

    private readonly Dictionary<uint, byte[]> _usedNonces = new();
    private byte[]? _sharedSecret;
    private byte[]? _sessionKey;
    private bool _disposed;
    private uint _clientSequence;
    private uint _serverSequence;

    public SecureSession(
        SecureSessionOptions options,
        IKeyEncapsulationService kemService,
        ISignatureService signatureService,
        IAeadCipherProvider aeadProvider,
        INonceGenerator nonceGenerator,
        ISessionKeyDeriver sessionKeyDeriver,
        IKeyMaterialProtector keyProtector,
        ISideChannelDefense sideChannelDefense)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _kemService = kemService ?? throw new ArgumentNullException(nameof(kemService));
        _signatureService = signatureService ?? throw new ArgumentNullException(nameof(signatureService));
        _aeadProvider = aeadProvider ?? throw new ArgumentNullException(nameof(aeadProvider));
        _nonceGenerator = nonceGenerator ?? throw new ArgumentNullException(nameof(nonceGenerator));
        _sessionKeyDeriver = sessionKeyDeriver ?? throw new ArgumentNullException(nameof(sessionKeyDeriver));
        _keyProtector = keyProtector ?? throw new ArgumentNullException(nameof(keyProtector));
        _sideChannelDefense = sideChannelDefense ?? new NoopSideChannelDefense();

        SessionStartTime = DateTime.UtcNow;
        LastActivityTime = SessionStartTime;
    }

    public byte[]? KyberPublicKey { get; private set; }
    public byte[]? KyberPrivateKey { get; private set; }
    public byte[]? PeerKyberPublicKey { get; set; }

    public byte[]? DilithiumPublicKey { get; private set; }
    public byte[]? DilithiumPrivateKey { get; private set; }
    public byte[]? PeerDilithiumPublicKey { get; set; }

    public bool IsKeyExchangeComplete { get; private set; }
    public bool IsAuthenticated { get; set; }
    public DateTime SessionStartTime { get; }
    public DateTime LastActivityTime { get; private set; }

    public KemStrength KemStrength => _options.KemStrength;
    public SignatureStrength SignatureStrength => _options.SignatureStrength;
    public AeadAlgorithm AeadAlgorithm => _options.AeadAlgorithm;

    public uint ClientSequenceNumber
    {
        get { lock (_sync) { return _clientSequence; } }
        set { lock (_sync) { _clientSequence = value; } }
    }

    public uint ServerSequenceNumber
    {
        get { lock (_sync) { return _serverSequence; } }
        set { lock (_sync) { _serverSequence = value; } }
    }

    public void GenerateKyberKeys()
    {
        _sideChannelDefense.BeforeOperation(SideChannelOperation.KeyGeneration);
        var (pub, priv) = _kemService.GenerateKeyPair(_options.KemStrength);
        KyberPublicKey = pub;
        KyberPrivateKey = priv;
        _sideChannelDefense.AfterOperation(SideChannelOperation.KeyGeneration, KyberPublicKey!, KyberPrivateKey!);
        Touch();
    }

    public void LoadKyberKeys(byte[] publicKey, byte[] privateKey)
    {
        if (publicKey is null)
        {
            throw new ArgumentNullException(nameof(publicKey));
        }

        if (privateKey is null)
        {
            throw new ArgumentNullException(nameof(privateKey));
        }

        _sideChannelDefense.BeforeOperation(SideChannelOperation.KeyGeneration, publicKey, privateKey);
        KyberPublicKey = (byte[])publicKey.Clone();
        KyberPrivateKey = (byte[])privateKey.Clone();
        _sideChannelDefense.AfterOperation(SideChannelOperation.KeyGeneration, KyberPublicKey!, KyberPrivateKey!);
        Touch();
    }

    public void GenerateDilithiumKeys()
    {
        _sideChannelDefense.BeforeOperation(SideChannelOperation.KeyGeneration);
        var (pub, priv) = _signatureService.GenerateKeyPair(_options.SignatureStrength);
        DilithiumPublicKey = pub;
        DilithiumPrivateKey = priv;
        _sideChannelDefense.AfterOperation(SideChannelOperation.KeyGeneration, DilithiumPublicKey!, DilithiumPrivateKey!);
        Touch();
    }

    public void LoadDilithiumKeys(byte[] publicKey, byte[] privateKey)
    {
        if (publicKey is null)
        {
            throw new ArgumentNullException(nameof(publicKey));
        }

        if (privateKey is null)
        {
            throw new ArgumentNullException(nameof(privateKey));
        }

        _sideChannelDefense.BeforeOperation(SideChannelOperation.KeyGeneration, publicKey, privateKey);
        DilithiumPublicKey = (byte[])publicKey.Clone();
        DilithiumPrivateKey = (byte[])privateKey.Clone();
        _sideChannelDefense.AfterOperation(SideChannelOperation.KeyGeneration, DilithiumPublicKey!, DilithiumPrivateKey!);
        Touch();
    }

    public (byte[] Ciphertext, byte[] SharedSecret) EncapsulateKey(byte[] peerPublicKey)
    {
        if (peerPublicKey is null)
        {
            throw new ArgumentNullException(nameof(peerPublicKey));
        }

        _sideChannelDefense.BeforeOperation(SideChannelOperation.Encapsulation, peerPublicKey);
        var (ciphertext, sharedSecret) = _kemService.Encapsulate(peerPublicKey, _options.KemStrength);
        PeerKyberPublicKey = peerPublicKey;
        _sharedSecret = sharedSecret;
        _sideChannelDefense.BeforeOperation(SideChannelOperation.KeyDerivation, sharedSecret);
        _sessionKey = _sessionKeyDeriver.Derive(sharedSecret);
        _sideChannelDefense.AfterOperation(SideChannelOperation.KeyDerivation, _sessionKey);
        IsKeyExchangeComplete = true;
        Touch();
        _sideChannelDefense.AfterOperation(SideChannelOperation.Encapsulation, ciphertext, sharedSecret, _sessionKey);
        return (ciphertext, sharedSecret);
    }

    public byte[] DecapsulateKey(byte[] ciphertext)
    {
        if (ciphertext is null)
        {
            throw new ArgumentNullException(nameof(ciphertext));
        }

        if (KyberPrivateKey is null)
        {
            throw new InvalidOperationException("Clé privée Kyber non initialisée");
        }

        _sideChannelDefense.BeforeOperation(SideChannelOperation.Decapsulation, ciphertext, KyberPrivateKey!);
        var sharedSecret = _kemService.Decapsulate(ciphertext, KyberPrivateKey, _options.KemStrength);
        _sharedSecret = sharedSecret;
        _sideChannelDefense.BeforeOperation(SideChannelOperation.KeyDerivation, sharedSecret);
        _sessionKey = _sessionKeyDeriver.Derive(sharedSecret);
        _sideChannelDefense.AfterOperation(SideChannelOperation.KeyDerivation, _sessionKey);
        IsKeyExchangeComplete = true;
        Touch();
        _sideChannelDefense.AfterOperation(SideChannelOperation.Decapsulation, sharedSecret, _sessionKey);
        return sharedSecret;
    }

    public byte[] SignData(byte[] data)
    {
        if (data is null)
        {
            throw new ArgumentNullException(nameof(data));
        }

        if (DilithiumPrivateKey is null)
        {
            throw new InvalidOperationException("Clé privée Dilithium non initialisée");
        }

        _sideChannelDefense.BeforeOperation(SideChannelOperation.Signature, data, DilithiumPrivateKey!);
        var signature = _signatureService.Sign(data, DilithiumPrivateKey, _options.SignatureStrength);
        _sideChannelDefense.AfterOperation(SideChannelOperation.Signature, signature);
        Touch();
        return signature;
    }

    public bool VerifySignature(byte[] data, byte[] signature, byte[] publicKey)
    {
        if (data is null || signature is null || publicKey is null)
        {
            return false;
        }

        _sideChannelDefense.BeforeOperation(SideChannelOperation.Verification, data, signature, publicKey);
        var isValid = _signatureService.Verify(data, signature, publicKey, _options.SignatureStrength);
        _sideChannelDefense.AfterOperation(SideChannelOperation.Verification);
        Touch();
        return isValid;
    }

    public (byte[] Ciphertext, byte[] Nonce) EncryptData(byte[] plaintext, bool isClient)
    {
        if (!IsKeyExchangeComplete)
        {
            throw new InvalidOperationException("Échange de clés non terminé");
        }

        if (plaintext is null || plaintext.Length == 0)
        {
            throw new ArgumentException("Données à chiffrer invalides", nameof(plaintext));
        }

        var key = _sessionKey ?? throw new InvalidOperationException("Clé de session non dérivée");
        _sideChannelDefense.BeforeOperation(SideChannelOperation.Encryption, plaintext, key);
        var result = _aeadProvider.Encrypt(key, plaintext, _options.AeadAlgorithm);

        lock (_sync)
        {
            var seq = isClient ? ++_clientSequence : ++_serverSequence;
            _usedNonces[seq] = result.Nonce;
        }

        Touch();
        _sideChannelDefense.AfterOperation(SideChannelOperation.Encryption, result.CiphertextWithTag, result.Nonce);
        return (result.CiphertextWithTag, result.Nonce);
    }

    public byte[] DecryptData(byte[] ciphertext, byte[] nonce, bool isClient)
    {
        if (!IsKeyExchangeComplete)
        {
            throw new InvalidOperationException("Échange de clés non terminé");
        }

        if (ciphertext is null || ciphertext.Length == 0)
        {
            throw new ArgumentException("Données chiffrées invalides", nameof(ciphertext));
        }

        if (nonce is null)
        {
            throw new ArgumentNullException(nameof(nonce));
        }

        var key = _sessionKey ?? throw new InvalidOperationException("Clé de session non dérivée");
        _sideChannelDefense.BeforeOperation(SideChannelOperation.Decryption, ciphertext, nonce, key);

        lock (_sync)
        {
            var seq = isClient ? _clientSequence : _serverSequence;
            if (_usedNonces.TryGetValue(seq, out var previousNonce))
            {
                _sideChannelDefense.BeforeOperation(SideChannelOperation.Comparison, previousNonce ?? Array.Empty<byte>(), nonce);
                var reuse = previousNonce != null && ConstantTimeEquals(previousNonce, nonce);
                _sideChannelDefense.AfterOperation(SideChannelOperation.Comparison, nonce);
                if (reuse)
                {
                    throw new SecurityException("Réutilisation de nonce détectée");
                }
            }
        }

        var plaintext = _aeadProvider.Decrypt(key, ciphertext, nonce, _options.AeadAlgorithm);
        _sideChannelDefense.AfterOperation(SideChannelOperation.Decryption, plaintext);
        Touch();
        return plaintext;
    }

    public byte[] GenerateNonce(int size = 12)
    {
        _sideChannelDefense.BeforeOperation(SideChannelOperation.NonceGeneration);
        var nonce = _nonceGenerator.Generate(size);
        _sideChannelDefense.AfterOperation(SideChannelOperation.NonceGeneration, nonce);
        Touch();
        return nonce;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (_sharedSecret is not null)
        {
            _keyProtector.Zeroize(_sharedSecret);
            _sharedSecret = null;
        }

        if (_sessionKey is not null)
        {
            _keyProtector.Zeroize(_sessionKey);
            _sessionKey = null;
        }

        if (KyberPrivateKey is not null)
        {
            _keyProtector.Zeroize(KyberPrivateKey);
            KyberPrivateKey = null;
        }

        if (DilithiumPrivateKey is not null)
        {
            _keyProtector.Zeroize(DilithiumPrivateKey);
            DilithiumPrivateKey = null;
        }

        _usedNonces.Clear();
    }

    private void Touch()
    {
        LastActivityTime = DateTime.UtcNow;
    }

    private bool ConstantTimeEquals(byte[] a, byte[] b)
    {
        _sideChannelDefense.BeforeOperation(SideChannelOperation.Comparison, a, b);
        if (a.Length != b.Length)
        {
            _sideChannelDefense.AfterOperation(SideChannelOperation.Comparison);
            return false;
        }

        var diff = 0;
        for (var i = 0; i < a.Length; i++)
        {
            diff |= a[i] ^ b[i];
        }

        var equal = diff == 0;
        _sideChannelDefense.AfterOperation(SideChannelOperation.Comparison);
        return equal;
    }
}
