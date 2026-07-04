using KyberDaemon.Authentication;
using KyberDaemon.Configuration;
using KyberDaemon.Logging;
using KyberDaemon.Security;
using KyberDomain.Cryptography;
using KyberKeyManagement;
using KyberLibrary.DomainAdapters;
using KyberShared.Protocol;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Security;
using Org.BouncyCastle.Security;
using System;

namespace KyberDaemon.Sessions;

public sealed class SessionManager : IDisposable
{
    private readonly DaemonLogger _logger;
    private readonly DaemonConfiguration _config;
    private readonly AuthManager _authManager;
    private readonly ISecureSessionFactory _sessionFactory;
    private readonly ISessionKeyDeriver _hashDeriver;
    private readonly SessionQuotaManager _quotaManager;
    private readonly KeyRotationService _keyRotationService;
    private readonly KeyStorageOptions _keyOptions;

    public SessionManager(DaemonLogger logger, DaemonConfiguration config)
    {
        _logger = logger;
        _config = config;
        _authManager = new AuthManager(config, logger);
        _sessionFactory = new SecureSessionFactory();
        _hashDeriver = new Sha3SessionKeyDeriver();
        _quotaManager = new SessionQuotaManager(_config.MaxConcurrentConnections, _config.MaxSessionsPerUser);
        _keyOptions = new KeyStorageOptions
        {
            KeysDirectory = config.KeysDirectory,
            EncryptionMode = ResolveMode(config.KeyEncryptionMode),
            Passphrase = string.IsNullOrWhiteSpace(config.KeyEncryptionPassphrase) ? null : config.KeyEncryptionPassphrase,
            RotationDays = config.KeyRotationDays
        };
        _keyRotationService = new KeyRotationService(_keyOptions, new KeyManagementLoggerAdapter(logger), _sessionFactory);
    }

    public async Task HandleNewConnectionAsync(TcpClient client)
    {
        using var context = new SessionContext(client, _logger);

        _logger.Info("SessionManager", $"[{context.SessionId}] Session ouverte depuis {context.RemoteEndPoint}");

        var globalLease = await _quotaManager.TryAcquireGlobalAsync(CancellationToken.None).ConfigureAwait(false);
        if (globalLease is null)
        {
            _logger.Warning("SessionManager", $"Capacité globale atteinte ({_quotaManager.ActiveSessions}/{_quotaManager.MaxSessions}). Rejet de la session depuis {context.RemoteEndPoint}");
            return;
        }

        context.AttachGlobalQuotaLease(globalLease);
        _logger.Info("SessionManager", $"[{context.SessionId}] Slot global alloué ({_quotaManager.ActiveSessions}/{_quotaManager.MaxSessions})");

        using var cancellation = new CancellationTokenSource(_config.SessionTimeout);
        var secureSession = CreateSecureSession();
        context.AttachSecureSession(secureSession);

        var clientHelloMessage = await DaemonMessage.ReceiveAsync(context.Stream, cancellation.Token)
            ?? throw new IOException("Connexion fermée pendant le handshake (ClientHello)");

        var clientHello = HandshakeMessages.ReadClientHello(clientHelloMessage);
        if (clientHello.Version != HandshakeMessages.CurrentVersion)
        {
            throw new InvalidDataException($"Version de protocole incompatible: {clientHello.Version}");
        }

        secureSession.PeerKyberPublicKey = clientHello.KyberPublicKey;
        secureSession.PeerDilithiumPublicKey = clientHello.DilithiumPublicKey;

        _logger.Info("SessionManager", $"[{context.SessionId}] ClientHello reçu, envoi ServerHello");

        var serverHello = HandshakeMessages.CreateServerHello(
            secureSession.KyberPublicKey!,
            secureSession.DilithiumPublicKey!);
        await serverHello.SendAsync(context.Stream, cancellation.Token);

        var keyExchangeMessage = await DaemonMessage.ReceiveAsync(context.Stream, cancellation.Token)
            ?? throw new IOException("Connexion fermée pendant le handshake (KeyExchange)");
        var keyExchange = HandshakeMessages.ReadKeyExchange(keyExchangeMessage);

        secureSession.DecapsulateKey(keyExchange.Ciphertext);

        var transcriptHash = BuildHandshakeTranscriptHash(clientHello, secureSession.KyberPublicKey!, keyExchange.Ciphertext);
        if (!secureSession.VerifySignature(transcriptHash, keyExchange.Signature, secureSession.PeerDilithiumPublicKey!))
        {
            throw new SecurityException("Signature Dilithium du client invalide");
        }

        await HandshakeMessages.CreateHandshakeAck().SendAsync(context.Stream, cancellation.Token);
        _logger.Info("SessionManager", $"[{context.SessionId}] Handshake Kyber/Dilithium terminé");

        if (_authManager.SupportedMethods.Count == 0)
        {
            await SecureMessageChannel.SendAsync(context.Stream, secureSession, isClient: false,
                AuthMessages.CreateAuthFailure("Aucune méthode d'authentification disponible"), cancellation.Token);
            throw new SecurityException("Aucune méthode d'authentification disponible");
        }

        var challenge = new byte[32];
        var rng = new SecureRandom();
        rng.NextBytes(challenge);

        var authRequest = AuthMessages.CreateAuthRequest(challenge, _authManager.SupportedMethods);
        await SecureMessageChannel.SendAsync(context.Stream, secureSession, isClient: false, authRequest, cancellation.Token);

        var authResponseMessage = await SecureMessageChannel.ReceiveAsync(context.Stream, secureSession, isClient: false, cancellation.Token)
            ?? throw new IOException("Connexion fermée pendant l'authentification");

        if (authResponseMessage.Type != DaemonMessageType.AuthResponse)
        {
            throw new SecurityException("Réponse d'authentification invalide");
        }

        var authResponse = AuthMessages.ReadAuthResponse(authResponseMessage);
        var authResult = await _authManager.AuthenticateAsync(authResponse, challenge);

        if (!authResult.IsSuccess)
        {
            var reason = authResult.Reason ?? "Authentification échouée";
            await SecureMessageChannel.SendAsync(context.Stream, secureSession, isClient: false,
                AuthMessages.CreateAuthFailure(reason), cancellation.Token);
            throw new SecurityException(reason);
        }

        var userLease = await _quotaManager.TryAcquireUserAsync(authResult.Username!, cancellation.Token).ConfigureAwait(false);
        if (userLease is null)
        {
            var quotaReason = $"Quota utilisateur atteint ({authResult.Username}, max {_quotaManager.MaxSessionsPerUser})";
            await SecureMessageChannel.SendAsync(context.Stream, secureSession, isClient: false,
                AuthMessages.CreateAuthFailure(quotaReason), cancellation.Token);
            _logger.Warning("SessionManager", $"[{context.SessionId}] {quotaReason}");
            return;
        }

        context.AttachUserQuotaLease(userLease);
        context.SetAuthenticatedUser(authResult.Username!);

        await SecureMessageChannel.SendAsync(context.Stream, secureSession, isClient: false,
            AuthMessages.CreateAuthSuccess(), cancellation.Token);

        _logger.Info("SessionManager", $"[{context.SessionId}] Utilisateur {context.AuthenticatedUsername} authentifié ({_quotaManager.GetActiveSessions(context.AuthenticatedUsername!)}/{_quotaManager.MaxSessionsPerUser})");

        var shellHost = new ShellProcessHost(_config, _logger);
        await shellHost.StartAsync(cancellation.Token);
        context.AttachShellHost(shellHost);

        _logger.Info("SessionManager", $"[{context.SessionId}] Shell de session lancé, attente des commandes");
        await RunSessionLoopAsync(context, secureSession, cancellation.Token);
        _logger.Info("SessionManager", $"[{context.SessionId}] Session terminée");
    }

    private SecureSession CreateSecureSession()
    {
        var options = new SecureSessionOptions();
        var secureSession = _sessionFactory.Create(options);

        var validity = _keyOptions.RotationDays > 0 ? TimeSpan.FromDays(_keyOptions.RotationDays) : (TimeSpan?)null;
        if (!_keyRotationService.TryLoadKeys("session", out var material, out var metadata) || _keyRotationService.ShouldRotate(metadata))
        {
            material = _keyRotationService.RotateKeys("session", validity);
            metadata = null;
        }
        else if (metadata is not null)
        {
            _logger.Info("KeyRotation", $"Clés 'session' chargées (v{metadata.Version})");
        }

        secureSession.LoadKyberKeys(material.KyberPublic, material.KyberPrivate);
        secureSession.LoadDilithiumKeys(material.DilithiumPublic, material.DilithiumPrivate);
        Array.Clear(material.KyberPrivate, 0, material.KyberPrivate.Length);
        Array.Clear(material.DilithiumPrivate, 0, material.DilithiumPrivate.Length);
        return secureSession;
    }

    private byte[] BuildHandshakeTranscriptHash(HandshakeMessages.ClientHello clientHello, byte[] serverKyberPublicKey, byte[] ciphertext)
    {
        using var ms = new MemoryStream();
        ms.WriteByte(clientHello.Version);
        ms.Write(clientHello.KyberPublicKey, 0, clientHello.KyberPublicKey.Length);
        ms.Write(clientHello.DilithiumPublicKey, 0, clientHello.DilithiumPublicKey.Length);
        ms.Write(serverKyberPublicKey, 0, serverKyberPublicKey.Length);
        ms.Write(ciphertext, 0, ciphertext.Length);
        return _hashDeriver.Derive(ms.ToArray());
    }

    private async Task RunSessionLoopAsync(SessionContext context, SecureSession secureSession, CancellationToken cancellationToken)
    {
        while (true)
        {
            var message = await SecureMessageChannel.ReceiveAsync(context.Stream, secureSession, isClient: false, cancellationToken);
            if (message == null)
            {
                _logger.Info("SessionManager", $"[{context.SessionId}] Connexion fermée par le client");
                return;
            }

            switch (message.Type)
            {
                case DaemonMessageType.SessionCommand:
                    await HandleSessionCommandAsync(context, secureSession, message, cancellationToken);
                    break;

                case DaemonMessageType.SessionClose:
                {
                    var close = SessionMessages.ReadSessionClose(message);
                    _logger.Info("SessionManager", $"[{context.SessionId}] Fermeture de session demandée: {close.Reason}");
                    return;
                }

                case DaemonMessageType.KeepAlive:
                    await SecureMessageChannel.SendAsync(context.Stream, secureSession, isClient: false,
                        new DaemonMessage(DaemonMessageType.KeepAlive, Array.Empty<byte>()), cancellationToken);
                    break;

                default:
                    _logger.Warning("SessionManager", $"[{context.SessionId}] Type de message non supporté: {message.Type}");
                    await SecureMessageChannel.SendAsync(context.Stream, secureSession, isClient: false,
                        new DaemonMessage(DaemonMessageType.Error, Array.Empty<byte>()), cancellationToken);
                    break;
            }
        }
    }

    private async Task HandleSessionCommandAsync(SessionContext context, SecureSession secureSession, DaemonMessage message, CancellationToken cancellationToken)
    {
        if (context.ShellHost == null)
        {
            await SecureMessageChannel.SendAsync(context.Stream, secureSession, isClient: false,
                SessionMessages.CreateSessionOutput(false, string.Empty, "Shell non initialisé"), cancellationToken);
            return;
        }

        var command = SessionMessages.ReadSessionCommand(message);
        try
        {
            var result = await context.ShellHost.ExecuteCommandAsync(command.Command, cancellationToken);
            await SecureMessageChannel.SendAsync(context.Stream, secureSession, isClient: false,
                SessionMessages.CreateSessionOutput(true, result.Output, result.Error), cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.Error("SessionManager", "Erreur lors de l'exécution de la commande", ex);
            await SecureMessageChannel.SendAsync(context.Stream, secureSession, isClient: false,
                SessionMessages.CreateSessionOutput(false, string.Empty, ex.Message), cancellationToken);
        }
    }

    private static KeyEncryptionMode ResolveMode(string? mode)
    {
        if (string.Equals(mode, "dpapi", StringComparison.OrdinalIgnoreCase))
        {
            return KeyEncryptionMode.Dpapi;
        }

        return KeyEncryptionMode.Passphrase;
    }

    public void Dispose()
    {
        _authManager.Dispose();
        _quotaManager.Dispose();
    }
}

public sealed class SessionContext : IDisposable
{
    public TcpClient Client { get; }
    public NetworkStream Stream { get; }
    public SecureSession? SecureSession { get; private set; }
    public string? AuthenticatedUsername { get; private set; }
    public ShellProcessHost? ShellHost { get; private set; }
    public Guid SessionId { get; } = Guid.NewGuid();
    public string RemoteEndPoint { get; }

    private readonly DaemonLogger _logger;
    private IDisposable? _globalQuotaLease;
    private IDisposable? _userQuotaLease;

    public SessionContext(TcpClient client, DaemonLogger logger)
    {
        Client = client;
        Stream = client.GetStream();
        _logger = logger;
        RemoteEndPoint = client.Client.RemoteEndPoint?.ToString() ?? "inconnu";
    }

    public void AttachSecureSession(SecureSession secureSession)
    {
        SecureSession = secureSession;
    }

    public void AttachShellHost(ShellProcessHost shellHost)
    {
        ShellHost = shellHost;
    }

    public void AttachGlobalQuotaLease(IDisposable lease)
    {
        _globalQuotaLease = lease;
    }

    public void AttachUserQuotaLease(IDisposable lease)
    {
        _userQuotaLease = lease;
    }

    public void SetAuthenticatedUser(string username)
    {
        AuthenticatedUsername = username;
    }

    public void Dispose()
    {
        var userLease = _userQuotaLease;
        var globalLease = _globalQuotaLease;
        _userQuotaLease = null;
        _globalQuotaLease = null;

        userLease?.Dispose();
        globalLease?.Dispose();

        try
        {
            Stream.Dispose();
            Client.Dispose();
            SecureSession?.Dispose();
            SecureSession = null;
            ShellHost?.Dispose();
            ShellHost = null;
        }
        catch (Exception ex)
        {
            _logger.Warning("SessionContext", $"Erreur lors de la fermeture de la session: {ex.Message}");
        }
    }
}
