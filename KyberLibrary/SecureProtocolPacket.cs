using System;
using System.IO;
using System.Text;
using Org.BouncyCastle.Security;

namespace KyberLibrary
{
    /// <summary>
    /// Types de paquets du protocole sécurisé
    /// </summary>
    public enum PacketType : byte
    {
        // Établissement de session
        KeyExchangeRequest = 0x01,      // Demande d'échange de clés (client → serveur)
        KeyExchangeResponse = 0x02,     // Réponse d'échange de clés (serveur → client)
        KeyExchangeAck = 0x03,          // Accusé de réception de l'échange (client → serveur)
        
        // Authentification
        AuthRequest = 0x10,              // Demande d'authentification
        AuthResponse = 0x11,             // Réponse d'authentification
        AuthChallenge = 0x12,            // Défi d'authentification
        AuthSuccess = 0x13,              // Authentification réussie
        
        // Communication chiffrée
        EncryptedData = 0x20,            // Données chiffrées
        EncryptedCommand = 0x21,         // Commande chiffrée
        EncryptedResponse = 0x22,        // Réponse chiffrée
        
        // Gestion de session
        Heartbeat = 0x30,                // Heartbeat pour maintenir la session
        SessionClose = 0x31,             // Fermeture de session
        Error = 0xFF                     // Erreur
    }

    /// <summary>
    /// Flags pour les paquets
    /// </summary>
    [Flags]
    public enum PacketFlags : byte
    {
        None = 0x00,
        RequiresResponse = 0x01,         // Le paquet nécessite une réponse
        IsResponse = 0x02,               // Ce paquet est une réponse
        HasSignature = 0x04,             // Le paquet contient une signature
        IsCompressed = 0x08,             // Les données sont compressées
        IsFinal = 0x10                   // Dernier paquet d'une séquence
    }

    /// <summary>
    /// En-tête d'un paquet du protocole sécurisé
    /// </summary>
    public class PacketHeader
    {
        public PacketType Type { get; set; }
        public PacketFlags Flags { get; set; }
        public uint SequenceNumber { get; set; }
        public uint PayloadLength { get; set; }
        public ushort SignatureLength { get; set; }
        public byte[] Nonce { get; set; }  // 12 bytes pour ChaCha20-Poly1305 ou AES-GCM

        public const int HeaderSize = 1 + 1 + 4 + 4 + 2 + 12; // 24 bytes

        public byte[] Serialize()
        {
            using (var ms = new MemoryStream())
            {
                ms.WriteByte((byte)Type);
                ms.WriteByte((byte)Flags);
                WriteUInt32(ms, SequenceNumber);
                WriteUInt32(ms, PayloadLength);
                WriteUInt16(ms, SignatureLength);
                ms.Write(Nonce, 0, Nonce.Length);
                return ms.ToArray();
            }
        }

        public static PacketHeader Deserialize(byte[] data)
        {
            if (data == null || data.Length < HeaderSize)
            {
                throw new ArgumentException("Données d'en-tête invalides", nameof(data));
            }

            using (var ms = new MemoryStream(data))
            {
                var header = new PacketHeader
                {
                    Type = (PacketType)ms.ReadByte(),
                    Flags = (PacketFlags)ms.ReadByte(),
                    SequenceNumber = ReadUInt32(ms),
                    PayloadLength = ReadUInt32(ms),
                    SignatureLength = ReadUInt16(ms),
                    Nonce = new byte[12]
                };
                ms.Read(header.Nonce, 0, 12);
                return header;
            }
        }

        private static void WriteUInt32(MemoryStream ms, uint value)
        {
            ms.WriteByte((byte)(value >> 24));
            ms.WriteByte((byte)(value >> 16));
            ms.WriteByte((byte)(value >> 8));
            ms.WriteByte((byte)value);
        }

        private static uint ReadUInt32(MemoryStream ms)
        {
            return ((uint)ms.ReadByte() << 24) |
                   ((uint)ms.ReadByte() << 16) |
                   ((uint)ms.ReadByte() << 8) |
                   (uint)ms.ReadByte();
        }

        private static void WriteUInt16(MemoryStream ms, ushort value)
        {
            ms.WriteByte((byte)(value >> 8));
            ms.WriteByte((byte)value);
        }

        private static ushort ReadUInt16(MemoryStream ms)
        {
            return (ushort)((ms.ReadByte() << 8) | ms.ReadByte());
        }
    }

    /// <summary>
    /// Paquet complet du protocole sécurisé
    /// </summary>
    public class SecureProtocolPacket
    {
        public PacketHeader Header { get; set; }
        public byte[] Payload { get; set; }
        public byte[] Signature { get; set; }

        /// <summary>
        /// Sérialise le paquet complet
        /// </summary>
        public byte[] Serialize()
        {
            using (var ms = new MemoryStream())
            {
                // En-tête
                var headerBytes = Header.Serialize();
                ms.Write(headerBytes, 0, headerBytes.Length);

                // Payload
                if (Payload != null && Payload.Length > 0)
                {
                    ms.Write(Payload, 0, Payload.Length);
                }

                // Signature (si présente)
                if (Signature != null && Signature.Length > 0)
                {
                    ms.Write(Signature, 0, Signature.Length);
                }

                return ms.ToArray();
            }
        }

        /// <summary>
        /// Désérialise un paquet depuis des bytes
        /// </summary>
        public static SecureProtocolPacket Deserialize(byte[] data)
        {
            if (data == null || data.Length < PacketHeader.HeaderSize)
            {
                throw new ArgumentException("Données de paquet invalides", nameof(data));
            }

            // Lire l'en-tête
            var headerBytes = new byte[PacketHeader.HeaderSize];
            Array.Copy(data, 0, headerBytes, 0, PacketHeader.HeaderSize);
            var header = PacketHeader.Deserialize(headerBytes);

            var packet = new SecureProtocolPacket { Header = header };

            int offset = PacketHeader.HeaderSize;

            // Lire le payload
            if (header.PayloadLength > 0)
            {
                packet.Payload = new byte[header.PayloadLength];
                Array.Copy(data, offset, packet.Payload, 0, header.PayloadLength);
                offset += (int)header.PayloadLength;
            }

            // Lire la signature
            if (header.SignatureLength > 0)
            {
                packet.Signature = new byte[header.SignatureLength];
                Array.Copy(data, offset, packet.Signature, 0, header.SignatureLength);
            }

            return packet;
        }

        /// <summary>
        /// Crée un paquet d'erreur
        /// </summary>
        public static SecureProtocolPacket CreateErrorPacket(uint sequenceNumber, string errorMessage)
        {
            var errorBytes = Encoding.UTF8.GetBytes(errorMessage);
            var nonce = new byte[12];
            var rng = new SecureRandom();
            rng.NextBytes(nonce);

            return new SecureProtocolPacket
            {
                Header = new PacketHeader
                {
                    Type = PacketType.Error,
                    Flags = PacketFlags.None,
                    SequenceNumber = sequenceNumber,
                    PayloadLength = (uint)errorBytes.Length,
                    SignatureLength = 0,
                    Nonce = nonce
                },
                Payload = errorBytes,
                Signature = null
            };
        }
    }
}

