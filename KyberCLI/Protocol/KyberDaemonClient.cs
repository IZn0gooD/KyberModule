using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Security;
using Konscious.Security.Cryptography;
using KyberCLI.Commands;
using KyberCLI.State;
using KyberDomain.Cryptography;
using KyberLibrary.DomainAdapters;
using KyberShared.Protocol;
using KyberShared.Security;
using DiagnosticsLevel = KyberCLI.Commands.DiagnosticsLevel;

namespace KyberCLI.Protocol;

public sealed class KyberDaemonClient
{
    private readonly ConnectOptions _options;
    private readonly InteractiveCommandAssistant _assistant = new();
    private readonly CliStateStore _stateStore = new();
    private readonly List<string> _history = new();
    private readonly string? _pinnedFingerprint;

    private static readonly ISecureSessionFactory SessionFactory = new SecureSessionFactory();
    private static readonly ISessionKeyDeriver HashDeriver = new Sha3SessionKeyDeriver();
    private static readonly ISignatureService ExternalSignatureService = new DilithiumSignatureService();

    public KyberDaemonClient(ConnectOptions options)
    {
        _options = options;
        _pinnedFingerprint = options.TlsCertFingerprint;
    }

    private void Trace(DiagnosticsLevel level, string message)
    {
        if (_options.DiagnosticsLevel < level)
        {
            return;
        }

        var previousColor = Console.ForegroundColor;
        try
        {
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine($"[diag:{level}] {message}");
        }
        finally
        {
            Console.ForegroundColor = previousColor;
        }
    }

    private void TracePayload(DiagnosticsLevel level, string label, byte[] payload)
    {
        if (_options.DiagnosticsLevel < level)
        {
            return;
        }

        var preview = Convert.ToBase64String(payload);
        if (preview.Length > 256)
        {
            preview = preview[..256] + "...";
        }

        Trace(level, $"{label} (len={payload.Length}) => {preview}");
    }

    public async Task<int> RunAsync()
    {
        return await RunAsyncInternal();
    }

    private async Task<int> RunAsyncInternal()
    {
        InitializeClientState();

        using var client = new TcpClient();
        await client.ConnectAsync(_options.Host, _options.Port).ConfigureAwait(false);

        var transport = await CreateStreamAsync(client).ConfigureAwait(false);
        using var stream = transport;
        var secureSession = CreateSecureSession();

        await PerformHandshakeAsync(stream, secureSession);
        await PerformAuthenticationAsync(stream, secureSession);
        await InitializeAutoCompletionAsync(stream, secureSession);

        if (!string.IsNullOrEmpty(_options.Command))
        {
            var validation = _assistant.Validate(_options.Command);
            if (!validation.IsValid)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine($"Commande invalide: {validation.Message}");
                Console.ResetColor();
                return 2;
            }

            if (validation.IsWarning && validation.Message is not null)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Error.WriteLine($"Avertissement: {validation.Message}");
                Console.ResetColor();
            }

            await SendCommandAsync(stream, secureSession, _options.Command);
            return 0;
        }

        await InteractiveLoopAsync(stream, secureSession);
        return 0;
    }

    private void InitializeClientState()
    {
        _stateStore.LoadHistory();
        var aliases = _stateStore.LoadAliases();
        _assistant.LoadAliases(aliases);
    }

    private SecureSession CreateSecureSession()
    {
        var options = new SecureSessionOptions();
        var session = SessionFactory.Create(options);
        session.GenerateKyberKeys();
        session.GenerateDilithiumKeys();
        return session;
    }

    private async Task<Stream> CreateStreamAsync(TcpClient client)
    {
        Stream baseStream = client.GetStream();
        if (!_options.UseTls)
        {
            return baseStream;
        }

        var targetHost = string.IsNullOrWhiteSpace(_options.TlsServerName) ? _options.Host : _options.TlsServerName!;
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine($"TLS : négociation avec {targetHost}:{_options.Port}");
        Console.ResetColor();

        var sslStream = new SslStream(baseStream, leaveInnerStreamOpen: false, ValidateServerCertificate);
        var authenticationOptions = new SslClientAuthenticationOptions
        {
            TargetHost = targetHost,
            EnabledSslProtocols = SslProtocols.Tls13 | SslProtocols.Tls12,
            CertificateRevocationCheckMode = _options.TlsAllowInsecure ? X509RevocationMode.NoCheck : X509RevocationMode.Online
        };

        try
        {
            await sslStream.AuthenticateAsClientAsync(authenticationOptions).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            sslStream.Dispose();
            throw new SecurityException($"Échec de l'authentification TLS : {ex.Message}", ex);
        }

        if (_options.TlsAllowInsecure)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("TLS : validation du certificat désactivée (--tls-skip-verify)");
            Console.ResetColor();
        }

        if (_pinnedFingerprint is not null)
        {
            var remote = sslStream.RemoteCertificate;
            if (remote is not null)
            {
                var actual = ComputeFingerprint(remote);
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine($"TLS : certificat épinglé {actual}");
                Console.ResetColor();
            }
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"TLS : canal sécurisé établi ({sslStream.SslProtocol})");
            Console.ResetColor();
        }

        return sslStream;
    }

    private bool ValidateServerCertificate(object sender, X509Certificate? certificate, X509Chain? chain, SslPolicyErrors errors)
    {
        if (_options.TlsAllowInsecure)
        {
            return true;
        }

        if (_pinnedFingerprint is not null)
        {
            if (certificate is null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("TLS : aucun certificat présenté par le serveur");
                Console.ResetColor();
                return false;
            }

            var actual = ComputeFingerprint(certificate);
            if (!actual.Equals(_pinnedFingerprint, StringComparison.OrdinalIgnoreCase))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"TLS : empreinte inattendue (attendu {_pinnedFingerprint}, reçu {actual})");
                Console.ResetColor();
                return false;
            }

            return true;
        }

        if (errors == SslPolicyErrors.None)
        {
            return true;
        }

        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"TLS : validation du certificat échouée ({errors})");
        Console.ResetColor();
        return false;
    }

    private static string ComputeFingerprint(X509Certificate certificate)
    {
        var raw = certificate.GetRawCertData();
        var hash = SHA256.HashData(raw);
        return Convert.ToHexString(hash);
    }

    private async Task PerformHandshakeAsync(Stream stream, SecureSession session)
    {
        Trace(DiagnosticsLevel.Handshake, "Envoi du ClientHello (Kyber + Dilithium)");
        var clientHello = CreateClientHello(session);
        TracePayload(DiagnosticsLevel.Full, "ClientHello", clientHello.Payload);
        await clientHello.SendAsync(stream, CancellationToken.None);

        var serverHello = await DaemonMessage.ReceiveAsync(stream, CancellationToken.None)
            ?? throw new IOException("Serveur a fermé la connexion durant le handshake");
        TracePayload(DiagnosticsLevel.Full, "ServerHello", serverHello.Payload);
        var helloPayload = HandshakeMessages.ReadServerHello(serverHello);
        Trace(DiagnosticsLevel.Handshake, $"ServerHello reçu (KyberPub={helloPayload.KyberPublicKey.Length} o, DilithiumPub={helloPayload.DilithiumPublicKey.Length} o)");

        session.PeerKyberPublicKey = helloPayload.KyberPublicKey;
        session.PeerDilithiumPublicKey = helloPayload.DilithiumPublicKey;

        var keyExchange = CreateKeyExchange(session);
        TracePayload(DiagnosticsLevel.Full, "Client KeyExchange", keyExchange.Payload);
        await keyExchange.SendAsync(stream, CancellationToken.None);
        Trace(DiagnosticsLevel.Handshake, "Client KeyExchange envoyé (encapsulation Kyber + signature Dilithium)");

        var ack = await DaemonMessage.ReceiveAsync(stream, CancellationToken.None)
            ?? throw new IOException("Handshake incomplet");
        if (ack.Type != DaemonMessageType.HandshakeAck)
        {
            throw new InvalidOperationException("HandshakeAck attendu");
        }
        Trace(DiagnosticsLevel.Handshake, "Handshake terminé : clé de session partagée établie");
    }

    private DaemonMessage CreateClientHello(SecureSession session)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        writer.Write(HandshakeMessages.CurrentVersion);
        writer.Write(session.KyberPublicKey!.Length);
        writer.Write(session.KyberPublicKey);
        writer.Write(session.DilithiumPublicKey!.Length);
        writer.Write(session.DilithiumPublicKey);

        return new DaemonMessage(DaemonMessageType.ClientHello, ms.ToArray());
    }

    private DaemonMessage CreateKeyExchange(SecureSession session)
    {
        var (ciphertext, _) = session.EncapsulateKey(session.PeerKyberPublicKey!);
        var transcriptHash = BuildTranscriptHash(session, ciphertext);

        var signature = session.SignData(transcriptHash);

        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);
        writer.Write(ciphertext.Length);
        writer.Write(ciphertext);
        writer.Write(signature.Length);
        writer.Write(signature);

        return new DaemonMessage(DaemonMessageType.KeyExchange, ms.ToArray());
    }

    private byte[] BuildTranscriptHash(SecureSession session, byte[] ciphertext)
    {
        using var ms = new MemoryStream();
        ms.WriteByte(HandshakeMessages.CurrentVersion);
        ms.Write(session.KyberPublicKey!, 0, session.KyberPublicKey!.Length);
        ms.Write(session.DilithiumPublicKey!, 0, session.DilithiumPublicKey!.Length);
        ms.Write(session.PeerKyberPublicKey!, 0, session.PeerKyberPublicKey!.Length);
        ms.Write(ciphertext, 0, ciphertext.Length);
        var hash = HashDeriver.Derive(ms.ToArray());
        return hash;
    }

    private async Task PerformAuthenticationAsync(Stream stream, SecureSession session)
    {
        var request = await SecureMessageChannel.ReceiveAsync(stream, session, isClient: true, CancellationToken.None)
            ?? throw new IOException("Connexion fermée pendant l'authentification");

        if (request.Type == DaemonMessageType.AuthFailure)
        {
            var reason = AuthMessages.ReadAuthFailure(request);
            throw new SecurityException($"Authentification refusée par le serveur: {reason}");
        }

        if (request.Type != DaemonMessageType.AuthRequest)
        {
            throw new InvalidDataException($"Message inattendu pendant l'authentification: {request.Type}");
        }

        var authRequest = AuthMessages.ReadAuthRequest(request);
        Trace(DiagnosticsLevel.Authentication, $"Méthodes proposées par le serveur: {string.Join(", ", authRequest.Methods)}");
        TracePayload(DiagnosticsLevel.Full, "AuthRequest", request.Payload);

        var response = await BuildAuthResponseAsync(authRequest, session);
        await SecureMessageChannel.SendAsync(stream, session, isClient: true, response, CancellationToken.None);

        var result = await SecureMessageChannel.ReceiveAsync(stream, session, isClient: true, CancellationToken.None)
            ?? throw new IOException("Connexion fermée après l'authentification");

        if (result.Type == DaemonMessageType.AuthFailure)
        {
            var reason = AuthMessages.ReadAuthFailure(result);
            throw new SecurityException($"Authentification refusée par le serveur: {reason}");
        }

        if (result.Type != DaemonMessageType.AuthSuccess)
        {
            throw new SecurityException("Authentification refusée");
        }

        Trace(DiagnosticsLevel.Authentication, "Authentification validée par le serveur");
    }

    private async Task<DaemonMessage> BuildAuthResponseAsync(AuthMessages.AuthRequestPayload request, SecureSession session)
    {
        var method = ChooseMethod(request.Methods);
        Trace(DiagnosticsLevel.Authentication, $"Méthode d'authentification choisie: {method}");
        byte[] data = method switch
        {
            "password" => BuildPasswordResponse(request.Challenge),
            "kerberos" => BuildKerberosResponse(),
            "oauth" => BuildOAuthResponse(),
            "key" => await BuildKeyResponseAsync(request.Challenge, session.SignatureStrength),
            _ => throw new SecurityException("Méthode d'authentification non supportée")
        };

        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms, Encoding.UTF8, leaveOpen: false);
        WriteString(writer, _options.Username);
        WriteString(writer, method);
        writer.Write(data.Length);
        writer.Write(data);
        TracePayload(DiagnosticsLevel.Full, "AuthResponse", ms.ToArray());
        Trace(DiagnosticsLevel.Full, $"Écriture de la réponse d'authentification (taille {data.Length} o)");

        return new DaemonMessage(DaemonMessageType.AuthResponse, ms.ToArray());
    }

    private string ChooseMethod(IReadOnlyList<string> methods)
    {
        if (!string.IsNullOrEmpty(_options.KerberosToken) && Contains(methods, "kerberos"))
        {
            return "kerberos";
        }

        if (!string.IsNullOrEmpty(_options.OAuthToken) && Contains(methods, "oauth"))
        {
            return "oauth";
        }

        if ((!string.IsNullOrEmpty(_options.PasswordHex) || !string.IsNullOrEmpty(_options.PasswordPlain)) && Contains(methods, "password"))
        {
            return "password";
        }

        if (!string.IsNullOrEmpty(_options.KeyPath) && Contains(methods, "key"))
        {
            return "key";
        }

        Trace(DiagnosticsLevel.Authentication, $"Méthodes côté client: password={(!string.IsNullOrEmpty(_options.PasswordHex) || !string.IsNullOrEmpty(_options.PasswordPlain))}, key={(!string.IsNullOrEmpty(_options.KeyPath))}, kerberos={(!string.IsNullOrEmpty(_options.KerberosToken))}, oauth={(!string.IsNullOrEmpty(_options.OAuthToken))}");
        throw new SecurityException($"Aucune méthode d'authentification commune disponible (serveur: {string.Join(", ", methods)})");
    }

    private static bool Contains(IReadOnlyList<string> methods, string value)
        => methods.Any(m => string.Equals(m, value, StringComparison.OrdinalIgnoreCase));

    private byte[] BuildKerberosResponse()
    {
        var token = ResolveKerberosToken();
        TracePayload(DiagnosticsLevel.Full, "Kerberos token", Encoding.UTF8.GetBytes(token));
        return Encoding.UTF8.GetBytes(token);
    }

    private byte[] BuildOAuthResponse()
    {
        var token = ResolveOAuthToken();
        TracePayload(DiagnosticsLevel.Full, "OAuth token", Encoding.UTF8.GetBytes(token));
        return Encoding.UTF8.GetBytes(token);
    }

    private string ResolveKerberosToken()
    {
        var token = _options.KerberosToken?.Trim();
        if (string.IsNullOrEmpty(token))
        {
            throw new SecurityException("Token Kerberos non fourni");
        }
        return token;
    }

    private string ResolveOAuthToken()
    {
        var token = _options.OAuthToken?.Trim();
        if (string.IsNullOrEmpty(token))
        {
            throw new SecurityException("Token OAuth non fourni");
        }
        return token;
    }

    private byte[] BuildPasswordResponse(byte[] challenge)
    {
        var secret = ResolvePasswordSecret();
        var buffer = new byte[secret.Length + challenge.Length];
        Buffer.BlockCopy(secret, 0, buffer, 0, secret.Length);
        Buffer.BlockCopy(challenge, 0, buffer, secret.Length, challenge.Length);
        TracePayload(DiagnosticsLevel.Full, "Concat(secret||challenge)", buffer);
        return HashDeriver.Derive(buffer);
    }

    private async Task<byte[]> BuildKeyResponseAsync(byte[] challenge, SignatureStrength strength)
    {
        if (string.IsNullOrEmpty(_options.KeyPath))
        {
            throw new SecurityException("Clé publique non fournie");
        }

        var keyData = await File.ReadAllBytesAsync(_options.KeyPath);
        var privateKeyBytes = Convert.FromBase64String(Encoding.UTF8.GetString(keyData));
        Trace(DiagnosticsLevel.Full, $"Signature du challenge via Dilithium (clé privée {_options.KeyPath}, longueur {privateKeyBytes.Length} o)");
        TracePayload(DiagnosticsLevel.Full, "Challenge à signer", challenge);
        var signature = ExternalSignatureService.Sign(challenge, privateKeyBytes, strength);
        TracePayload(DiagnosticsLevel.Full, "Signature générée", signature);
        return signature;
    }

    private async Task InitializeAutoCompletionAsync(Stream stream, SecureSession session)
    {
        try
        {
            var psCommand = "Get-Command | Select-Object -ExpandProperty Name";
            var result = await ExecuteCommandRawAsync(stream, session, psCommand, CancellationToken.None).ConfigureAwait(false);
            if (!string.IsNullOrEmpty(result.Error))
            {
                return;
            }

            var completions = result.Output
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (completions.Count > 0)
            {
                _assistant.UpdateRemoteCommands(completions);
            }
        }
        catch
        {
            // Ignorer les erreurs de complétion (conserver la liste par défaut)
        }
    }

    private async Task<SessionMessages.SessionOutputPayload> ExecuteCommandRawAsync(Stream stream, SecureSession session, string command, CancellationToken cancellationToken)
    {
        await SecureMessageChannel.SendAsync(stream, session, isClient: true,
            SessionMessages.CreateSessionCommand(command), cancellationToken).ConfigureAwait(false);

        var response = await SecureMessageChannel.ReceiveAsync(stream, session, isClient: true, cancellationToken)
            ?? throw new IOException("Aucune réponse pour la commande");

        if (response.Type != DaemonMessageType.SessionOutput)
        {
            throw new InvalidOperationException("SessionOutput attendu");
        }

        return SessionMessages.ReadSessionOutput(response);
    }

    private async Task SendCommandAsync(Stream stream, SecureSession session, string command)
    {
        var result = await ExecuteCommandRawAsync(stream, session, command, CancellationToken.None).ConfigureAwait(false);
        RenderCommandResult(result);
    }

    private void RenderCommandResult(SessionMessages.SessionOutputPayload result)
    {
        if (!string.IsNullOrEmpty(result.Output))
        {
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine(result.Output.TrimEnd());
            Console.ResetColor();
        }

        if (!string.IsNullOrEmpty(result.Error))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine(result.Error.TrimEnd());
            Console.ResetColor();
        }
    }

    private async Task InteractiveLoopAsync(Stream stream, SecureSession session)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("Session interactive démarrée. Tapez 'exit' pour quitter.");
        Console.WriteLine("Directives locales : :help, :examples, :run <alias>, :history, :validate <commande>, :aliases, :alias <nom> <cmd>, :unalias <nom>, :clear");
        Console.ResetColor();

        global::System.ReadLine.HistoryEnabled = true;
        global::System.ReadLine.AutoCompletionHandler = new PowerShellAutoCompleteHandler(() => _assistant.GetCompletionCandidates());

        while (true)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            var command = global::System.ReadLine.Read(" > ");
            Console.ResetColor();

            if (command is null)
            {
                continue;
            }

            if (command.Equals("exit", StringComparison.OrdinalIgnoreCase))
            {
                await SecureMessageChannel.SendAsync(stream, session, isClient: true,
                    SessionMessages.CreateSessionClose("Client exit"), CancellationToken.None);
                return;
            }

            if (string.IsNullOrWhiteSpace(command))
            {
                continue;
            }

            if (_assistant.IsDirective(command, out var directive))
            {
                if (await HandleDirectiveAsync(directive, stream, session).ConfigureAwait(false))
                {
                    continue;
                }

                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"Directive inconnue ou usage incorrect: {command}");
                Console.ResetColor();
                continue;
            }

            var validation = _assistant.Validate(command);
            if (!validation.IsValid)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"❌ {validation.Message}");
                Console.ResetColor();
                continue;
            }

            if (validation.IsWarning && validation.Message is not null)
            {
                if (!PromptConfirmation(validation.Message))
                {
                    continue;
                }
            }

            string expanded = command;
            if (_assistant.TryExpandAlias(command, out var aliasExpanded, out var aliasName))
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine($"→ Alias '{aliasName}' ⇢ {aliasExpanded}");
                Console.ResetColor();
                expanded = aliasExpanded;
            }

            global::System.ReadLine.AddHistory(command);
            _stateStore.AppendHistory(command);
            _history.Add(command);

            await SendCommandAsync(stream, session, expanded);
        }
    }

    private async Task<bool> HandleDirectiveAsync(CommandDirective directive, Stream stream, SecureSession session)
    {
        switch (directive.Type)
        {
            case CommandDirectiveType.Help:
                PrintHelp();
                return true;
            case CommandDirectiveType.Examples:
                PrintExamples();
                return true;
            case CommandDirectiveType.History:
                PrintHistory();
                return true;
            case CommandDirectiveType.ClearScreen:
                Console.Clear();
                return true;
            case CommandDirectiveType.Validate:
                RunValidation(directive.Argument);
                return true;
            case CommandDirectiveType.RunExample:
                return await RunExampleAsync(directive.Argument, stream, session).ConfigureAwait(false);
            case CommandDirectiveType.ListAliases:
                PrintAliases();
                return true;
            case CommandDirectiveType.SetAlias:
                if (directive.Argument is null || directive.Value is null)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("Usage : :alias <nom> <commande>");
                    Console.ResetColor();
                    return true;
                }
                if (_assistant.SetAlias(directive.Argument, directive.Value))
                {
                    _stateStore.SaveAliases(_assistant.GetAliases());
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"Alias '{directive.Argument}' enregistré vers '{directive.Value}'");
                    Console.ResetColor();
                }
                return true;
            case CommandDirectiveType.RemoveAlias:
                if (string.IsNullOrWhiteSpace(directive.Argument))
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("Usage : :unalias <nom>");
                    Console.ResetColor();
                    return true;
                }
                if (_assistant.RemoveAlias(directive.Argument))
                {
                    _stateStore.SaveAliases(_assistant.GetAliases());
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"Alias '{directive.Argument}' supprimé");
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"Alias '{directive.Argument}' introuvable");
                }
                Console.ResetColor();
                return true;
            case CommandDirectiveType.Invalid:
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Directive invalide. Utilisez :alias <nom> <commande> ou :unalias <nom>");
                Console.ResetColor();
                return true;
            default:
                return false;
        }
    }

    private void PrintHelp()
    {
        Console.WriteLine();
        Console.WriteLine("Directives locales :");
        Console.WriteLine("  :help                     Affiche cette aide");
        Console.WriteLine("  :examples                 Liste des scénarios prêts à l'emploi");
        Console.WriteLine("  :run <alias>              Exécute un exemple (voir :examples)");
        Console.WriteLine("  :history                  Affiche les dernières commandes envoyées");
        Console.WriteLine("  :validate <commande>      Analyse la commande sans l'exécuter");
        Console.WriteLine("  :aliases                  Liste les alias définis");
        Console.WriteLine("  :alias <nom> <commande>   Crée ou met à jour un alias");
        Console.WriteLine("  :unalias <nom>            Supprime un alias");
        Console.WriteLine("  :clear                    Efface l'écran");
        Console.WriteLine("  exit                      Ferme la session distante\n");
    }

    private void PrintExamples()
    {
        Console.WriteLine("\nExemples disponibles :");
        foreach (var example in _assistant.GetExamples())
        {
            Console.WriteLine($"  {example.Alias,-12} {example.Description}");
        }
        Console.WriteLine("Utilisez :run <alias> pour exécuter un exemple.");
    }

    private void PrintHistory()
    {
        if (_history.Count == 0)
        {
            Console.WriteLine("Aucune commande envoyée pour l'instant.");
            return;
        }

        Console.WriteLine("\nHistorique des commandes envoyées :");
        for (var i = 0; i < _history.Count; i++)
        {
            Console.WriteLine($"  {i + 1:00}: {_history[i]}");
        }
    }

    private void PrintAliases()
    {
        var aliases = _assistant.EnumerateAliases().ToList();
        if (aliases.Count == 0)
        {
            Console.WriteLine("Aucun alias enregistré. Utilisez :alias <nom> <commande> pour en créer.");
            return;
        }

        Console.WriteLine("\nAlias enregistrés :");
        foreach (var pair in aliases)
        {
            Console.WriteLine($"  {pair.Key,-12} = {pair.Value}");
        }
    }

    private void RunValidation(string? command)
    {
        if (string.IsNullOrWhiteSpace(command))
        {
            Console.WriteLine("Usage : :validate <commande>");
            return;
        }

        var validation = _assistant.Validate(command);
        if (!validation.IsValid)
        {
            Console.WriteLine($"❌ {validation.Message}");
            return;
        }

        if (validation.IsWarning && validation.Message is not null)
        {
            Console.WriteLine($"⚠️ {validation.Message}");
        }
        else
        {
            Console.WriteLine("✅ La commande semble valide selon le catalogue du serveur.");
        }
    }

    private async Task<bool> RunExampleAsync(string? alias, Stream stream, SecureSession session)
    {
        if (string.IsNullOrWhiteSpace(alias))
        {
            Console.WriteLine("Usage : :run <alias>. Tapez :examples pour la liste des alias.");
            return true;
        }

        if (!_assistant.TryGetExample(alias.Trim(), out var example))
        {
            Console.WriteLine($"Alias d'exemple inconnu: {alias}");
            return true;
        }

        Console.WriteLine($"→ Exécution de '{example.Alias}' : {example.Description}");
        Console.WriteLine($"Commande envoyée: {example.Command}");
        global::System.ReadLine.AddHistory(example.Command);
        _stateStore.AppendHistory(example.Command);
        _history.Add(example.Command);
        await SendCommandAsync(stream, session, example.Command);
        return true;
    }

    private static bool PromptConfirmation(string warningMessage)
    {
        Console.WriteLine($"⚠️ {warningMessage}");
        Console.Write("Poursuivre malgré tout ? (o/N) : ");
        var response = Console.ReadLine();
        return response != null && response.StartsWith("o", StringComparison.OrdinalIgnoreCase);
    }

    private static void WriteString(BinaryWriter writer, string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        writer.Write(bytes.Length);
        writer.Write(bytes);
    }

    private byte[] ResolvePasswordSecret()
    {
        byte[] secret;
        if (!string.IsNullOrEmpty(_options.PasswordHex))
        {
            secret = ParseHexSecret(_options.PasswordHex!);
        }
        else if (!string.IsNullOrEmpty(_options.PasswordPlain) && !string.IsNullOrEmpty(_options.PasswordRecord))
        {
            var record = LoadArgonRecord(_options.PasswordRecord!);
            secret = DeriveArgonSecret(_options.PasswordPlain!, record);
        }
        else
        {
            throw new SecurityException("Mot de passe non fourni");
        }

        if (secret.Length != 32)
        {
            throw new SecurityException("Le secret dérivé doit faire 32 octets");
        }

        return secret;
    }

    private static byte[] ParseHexSecret(string value)
    {
        var hex = value.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? value[2..] : value;
        if (hex.Length == 0 || hex.Length % 2 != 0)
        {
            throw new SecurityException("Secret hexadécimal invalide");
        }

        var bytes = new byte[hex.Length / 2];
        for (int i = 0; i < bytes.Length; i++)
        {
            bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
        }
        return bytes;
    }

    private static Argon2HashRecord LoadArgonRecord(string recordOrPath)
    {
        string source = recordOrPath;
        if (File.Exists(recordOrPath))
        {
            source = File.ReadAllText(recordOrPath).Trim();
        }

        if (!Argon2HashRecord.TryParse(source, out var record) || record is null)
        {
            throw new SecurityException("Enregistrement Argon2 invalide");
        }

        return record;
    }

    private static byte[] DeriveArgonSecret(string password, Argon2HashRecord record)
    {
        var passwordBytes = Encoding.UTF8.GetBytes(password);
        using var argon = new Argon2id(passwordBytes)
        {
            Salt = record.Salt,
            DegreeOfParallelism = Math.Max(1, record.Parallelism),
            Iterations = Math.Max(1, record.Iterations),
            MemorySize = Math.Max(8, record.MemorySize)
        };

        return argon.GetBytes(record.Hash.Length);
    }
}

internal sealed class PowerShellAutoCompleteHandler : System.IAutoCompleteHandler
{
    private readonly Func<IReadOnlyList<string>> _getCandidates;
    public char[] Separators { get; set; } = new[] { ' ', '\t', '.', ':', '\\', '/', '-' };

    public PowerShellAutoCompleteHandler(Func<IReadOnlyList<string>> getCandidates)
    {
        _getCandidates = getCandidates;
    }

    public string[] GetSuggestions(string text, int index)
    {
        var candidates = _getCandidates?.Invoke() ?? Array.Empty<string>();
        if (candidates.Count == 0)
        {
            return Array.Empty<string>();
        }

        var normalized = text ?? string.Empty;
        var currentToken = ExtractCurrentToken(normalized);
        if (string.IsNullOrEmpty(currentToken))
        {
            return candidates.ToArray();
        }

        return candidates
            .Where(c => c.StartsWith(currentToken, StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private string ExtractCurrentToken(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        var index = text.LastIndexOfAny(Separators);
        return index >= 0 ? text[(index + 1)..] : text;
    }
}
