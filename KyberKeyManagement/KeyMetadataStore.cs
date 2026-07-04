using System.Text.Json;
using System.Text.Json.Serialization;

namespace KyberKeyManagement;

public sealed class KeyMetadataStore
{
    private readonly string _metadataFile;
    private readonly object _sync = new();

    public KeyMetadataStore(string keysDirectory)
    {
        if (string.IsNullOrWhiteSpace(keysDirectory))
        {
            throw new ArgumentException("Le répertoire de clés est requis", nameof(keysDirectory));
        }

        Directory.CreateDirectory(keysDirectory);
        _metadataFile = Path.Combine(keysDirectory, "metadata.json");
    }

    public KeyMetadataCollection Load()
    {
        lock (_sync)
        {
            if (!File.Exists(_metadataFile))
            {
                return new KeyMetadataCollection();
            }

            try
            {
                var json = File.ReadAllText(_metadataFile);
                var metadata = JsonSerializer.Deserialize<KeyMetadataCollection>(json);
                return metadata ?? new KeyMetadataCollection();
            }
            catch
            {
                return new KeyMetadataCollection();
            }
        }
    }

    public void Save(KeyMetadataCollection metadata)
    {
        lock (_sync)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            var json = JsonSerializer.Serialize(metadata, options);
            File.WriteAllText(_metadataFile, json);
        }
    }
}

public sealed class KeyMetadataCollection
{
    [JsonPropertyName("keys")]
    public List<KeyMetadataRecord> Keys { get; set; } = new();

    public KeyMetadataRecord? Find(string name)
        => Keys.FirstOrDefault(k => string.Equals(k.Name, name, StringComparison.OrdinalIgnoreCase));

    public void Upsert(KeyMetadataRecord record)
    {
        var existing = Find(record.Name);
        if (existing is not null)
        {
            existing.Version = record.Version;
            existing.CreatedAt = record.CreatedAt;
            existing.ExpiresAt = record.ExpiresAt;
            existing.Description = record.Description;
            existing.Encryption = record.Encryption;
        }
        else
        {
            Keys.Add(record);
        }
    }
}

public sealed class KeyMetadataRecord
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("version")]
    public int Version { get; set; } = 1;

    [JsonPropertyName("created_at")]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    [JsonPropertyName("expires_at")]
    public DateTimeOffset? ExpiresAt { get; set; }
        = null;

    [JsonPropertyName("description")]
    public string? Description { get; set; }
        = null;

    [JsonPropertyName("encryption")]
    public string? Encryption { get; set; }
        = null;
}
