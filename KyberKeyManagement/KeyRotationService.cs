using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using KyberDomain.Cryptography;
using KyberLibrary.DomainAdapters;

namespace KyberKeyManagement;

public sealed class KeyRotationService
{
    private readonly KeyStorageOptions _options;
    private readonly IKeyLogger _logger;
    private readonly KeyMetadataStore _metadataStore;
    private readonly KeyEncryptionService _encryptionService;
    private readonly ISecureSessionFactory _sessionFactory;

    public KeyRotationService(KeyStorageOptions options, IKeyLogger? logger = null, ISecureSessionFactory? sessionFactory = null)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? new NullKeyLogger();

        if (string.IsNullOrWhiteSpace(options.KeysDirectory))
        {
            throw new ArgumentException("Le répertoire de clés est requis", nameof(options));
        }

        _metadataStore = new KeyMetadataStore(options.KeysDirectory);
        _encryptionService = new KeyEncryptionService(options);
        _sessionFactory = sessionFactory ?? new SecureSessionFactory();
    }

    public IReadOnlyList<KeyMetadataRecord> ListKeys()
        => _metadataStore.Load().Keys.OrderByDescending(k => k.CreatedAt).ToList();

    public bool TryLoadKeys(string name, out KeyMaterial material, out KeyMetadataRecord? metadataRecord)
    {
        material = default!;
        metadataRecord = null;

        var metadata = _metadataStore.Load();
        var record = metadata.Find(name);
        if (record is null)
        {
            if (TryLoadLegacy(name, out material))
            {
                metadataRecord = null;
                return true;
            }
            return false;
        }

        metadataRecord = record;
        var baseName = BuildBaseName(name, record.Version);
        var kyberPrivPath = baseName + ".kyber.priv";
        var kyberPubPath = baseName + ".kyber.pub";
        var dilPrivPath = baseName + ".dilithium.priv";
        var dilPubPath = baseName + ".dilithium.pub";

        if (!File.Exists(kyberPrivPath) || !File.Exists(kyberPubPath) ||
            !File.Exists(dilPrivPath) || !File.Exists(dilPubPath))
        {
            _logger.Warning("KeyRotation", $"Fichiers de clés manquants pour '{name}' v{record.Version}");
            return false;
        }

        try
        {
            if (!string.IsNullOrWhiteSpace(record.Encryption) &&
                !string.Equals(record.Encryption, _encryptionService.Mode, StringComparison.OrdinalIgnoreCase))
            {
                _logger.Warning("KeyRotation", $"Mode de chiffrement attendu '{record.Encryption}' différent du mode courant '{_encryptionService.Mode}' pour '{name}'");
            }

            var kyberPriv = _encryptionService.Decrypt(File.ReadAllBytes(kyberPrivPath));
            var kyberPub = File.ReadAllBytes(kyberPubPath);
            var dilPriv = _encryptionService.Decrypt(File.ReadAllBytes(dilPrivPath));
            var dilPub = File.ReadAllBytes(dilPubPath);

            material = new KeyMaterial(kyberPriv, kyberPub, dilPriv, dilPub);
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error("KeyRotation", $"Impossible de déchiffrer les clés '{name}': {ex.Message}", ex);
            return false;
        }
    }

    public bool ShouldRotate(KeyMetadataRecord? record)
        => record is not null && record.ExpiresAt is not null && DateTimeOffset.UtcNow >= record.ExpiresAt.Value;

    public KeyMaterial RotateKeys(string name, TimeSpan? validity = null)
    {
        var session = _sessionFactory.Create(new SecureSessionOptions());
        session.GenerateKyberKeys();
        session.GenerateDilithiumKeys();

        var material = new KeyMaterial(
            (byte[])session.KyberPrivateKey!.Clone(),
            (byte[])session.KyberPublicKey!.Clone(),
            (byte[])session.DilithiumPrivateKey!.Clone(),
            (byte[])session.DilithiumPublicKey!.Clone());

        var record = Persist(name, material, validity);
        _logger.Info("KeyRotation", $"Nouvelle version des clés '{name}' générée (v{record.Version}, mode {record.Encryption ?? _encryptionService.Mode})");
        return material;
    }

    public byte[] ExportMetadata()
    {
        var metadata = _metadataStore.Load();
        return System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(metadata, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });
    }

    private KeyMetadataRecord Persist(string name, KeyMaterial material, TimeSpan? validity)
    {
        Directory.CreateDirectory(_options.KeysDirectory);
        var metadata = _metadataStore.Load();
        var previous = metadata.Find(name);
        var version = (previous?.Version ?? 0) + 1;
        var created = DateTimeOffset.UtcNow;
        var expires = validity.HasValue ? created.Add(validity.Value) : (DateTimeOffset?)null;

        var baseName = BuildBaseName(name, version);
        File.WriteAllBytes(baseName + ".kyber.pub", material.KyberPublic);
        File.WriteAllBytes(baseName + ".dilithium.pub", material.DilithiumPublic);
        File.WriteAllBytes(baseName + ".kyber.priv", _encryptionService.Encrypt(material.KyberPrivate));
        File.WriteAllBytes(baseName + ".dilithium.priv", _encryptionService.Encrypt(material.DilithiumPrivate));

        DeleteLegacyFiles(name);

        var record = new KeyMetadataRecord
        {
            Name = name,
            Version = version,
            CreatedAt = created,
            ExpiresAt = expires,
            Description = "Kyber/Dilithium key material",
            Encryption = _encryptionService.Mode
        };

        metadata.Upsert(record);
        _metadataStore.Save(metadata);
        return record;
    }

    private bool TryLoadLegacy(string name, out KeyMaterial material)
    {
        material = default!;
        var kyberPriv = Path.Combine(_options.KeysDirectory, $"{name}.kyber.priv");
        var kyberPub = Path.Combine(_options.KeysDirectory, $"{name}.kyber.pub");
        var dilPriv = Path.Combine(_options.KeysDirectory, $"{name}.dilithium.priv");
        var dilPub = Path.Combine(_options.KeysDirectory, $"{name}.dilithium.pub");

        if (!File.Exists(kyberPriv) || !File.Exists(kyberPub) || !File.Exists(dilPriv) || !File.Exists(dilPub))
        {
            return false;
        }

        var legacy = new KeyMaterial(
            File.ReadAllBytes(kyberPriv),
            File.ReadAllBytes(kyberPub),
            File.ReadAllBytes(dilPriv),
            File.ReadAllBytes(dilPub));

        Persist(name, legacy, null);
        material = legacy;
        return true;
    }

    private void DeleteLegacyFiles(string name)
    {
        foreach (var extension in new[] { ".kyber.priv", ".kyber.pub", ".dilithium.priv", ".dilithium.pub" })
        {
            var path = Path.Combine(_options.KeysDirectory, name + extension);
            if (File.Exists(path))
            {
                try
                {
                    File.Delete(path);
                }
                catch
                {
                    // best effort
                }
            }
        }
    }

    private string BuildBaseName(string name, int version)
        => Path.Combine(_options.KeysDirectory, $"{name}.v{version}");
}

public sealed record KeyMaterial(byte[] KyberPrivate, byte[] KyberPublic, byte[] DilithiumPrivate, byte[] DilithiumPublic);
