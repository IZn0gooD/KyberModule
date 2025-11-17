using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace KyberLibrary
{
    /// <summary>
    /// Types de commandes supportées
    /// </summary>
    public enum CommandType : byte
    {
        ExecuteShell = 0x01,        // Exécuter une commande shell (PowerShell, CMD, Bash)
        ExecutePowerShell = 0x02,    // Exécuter une commande PowerShell spécifique
        ExecuteCommand = 0x03,       // Exécuter une commande système
        ListDirectory = 0x04,       // Lister un répertoire
        ReadFile = 0x05,            // Lire un fichier
        WriteFile = 0x06,           // Écrire un fichier
        GetSystemInfo = 0x07,       // Obtenir des informations système
        Ping = 0x08,                // Ping (test de connexion)
        Custom = 0xFF              // Commande personnalisée
    }

    /// <summary>
    /// Statut d'exécution d'une commande
    /// </summary>
    public enum CommandStatus : byte
    {
        Success = 0x00,             // Commande exécutée avec succès
        Error = 0x01,               // Erreur lors de l'exécution
        Timeout = 0x02,             // Timeout
        AccessDenied = 0x03,        // Accès refusé
        NotFound = 0x04,            // Commande non trouvée
        Invalid = 0x05              // Commande invalide
    }

    /// <summary>
    /// Structure d'une commande à exécuter
    /// </summary>
    [Obsolete("Utilisé uniquement par l'ancien module PowerShell.")]
    [DataContract]
    public class SecureCommand
    {
        [DataMember]
        public string CommandId { get; set; }      // ID unique de la commande
        [DataMember]
        public CommandType Type { get; set; }      // Type de commande
        [DataMember]
        public string Command { get; set; }        // Commande à exécuter
        [DataMember]
        public string[] Arguments { get; set; }    // Arguments de la commande
        [DataMember]
        public string WorkingDirectory { get; set; } // Répertoire de travail
        [DataMember]
        public int Timeout { get; set; }            // Timeout en millisecondes (0 = infini)
        [DataMember]
        public byte[] AdditionalData { get; set; }  // Données supplémentaires (pour WriteFile, etc.)

        public SecureCommand()
        {
            CommandId = Guid.NewGuid().ToString();
            Timeout = 30000; // 30 secondes par défaut
        }

        /// <summary>
        /// Sérialise la commande en JSON puis en bytes
        /// </summary>
        public byte[] Serialize()
        {
            using (var ms = new MemoryStream())
            {
                var serializer = new DataContractJsonSerializer(typeof(SecureCommand));
                serializer.WriteObject(ms, this);
                return ms.ToArray();
            }
        }

        /// <summary>
        /// Désérialise une commande depuis des bytes
        /// </summary>
        public static SecureCommand Deserialize(byte[] data)
        {
            using (var ms = new MemoryStream(data))
            {
                var serializer = new DataContractJsonSerializer(typeof(SecureCommand));
                return (SecureCommand)serializer.ReadObject(ms);
            }
        }
    }

    /// <summary>
    /// Résultat d'exécution d'une commande
    /// </summary>
    [Obsolete("Utilisé uniquement par l'ancien module PowerShell.")]
    [DataContract]
    public class CommandResult
    {
        [DataMember]
        public string CommandId { get; set; }       // ID de la commande
        [DataMember]
        public CommandStatus Status { get; set; }  // Statut d'exécution
        [DataMember]
        public string Output { get; set; }         // Sortie standard
        [DataMember]
        public string Error { get; set; }           // Sortie d'erreur
        [DataMember]
        public int ExitCode { get; set; }           // Code de sortie
        [DataMember]
        public long ExecutionTimeMs { get; set; }   // Temps d'exécution en millisecondes
        [DataMember]
        public DateTime ExecutedAt { get; set; }    // Date/heure d'exécution

        public CommandResult()
        {
            ExecutedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Sérialise le résultat en JSON puis en bytes
        /// </summary>
        public byte[] Serialize()
        {
            using (var ms = new MemoryStream())
            {
                var serializer = new DataContractJsonSerializer(typeof(CommandResult));
                serializer.WriteObject(ms, this);
                return ms.ToArray();
            }
        }

        /// <summary>
        /// Désérialise un résultat depuis des bytes
        /// </summary>
        public static CommandResult Deserialize(byte[] data)
        {
            using (var ms = new MemoryStream(data))
            {
                var serializer = new DataContractJsonSerializer(typeof(CommandResult));
                return (CommandResult)serializer.ReadObject(ms);
            }
        }

        /// <summary>
        /// Crée un résultat de succès
        /// </summary>
        public static CommandResult CreateSuccess(string commandId, string output, int exitCode = 0, long executionTimeMs = 0)
        {
            return new CommandResult
            {
                CommandId = commandId,
                Status = CommandStatus.Success,
                Output = output,
                Error = null,
                ExitCode = exitCode,
                ExecutionTimeMs = executionTimeMs
            };
        }

        /// <summary>
        /// Crée un résultat d'erreur
        /// </summary>
        public static CommandResult CreateError(string commandId, string error, int exitCode = 1, long executionTimeMs = 0)
        {
            return new CommandResult
            {
                CommandId = commandId,
                Status = CommandStatus.Error,
                Output = null,
                Error = error,
                ExitCode = exitCode,
                ExecutionTimeMs = executionTimeMs
            };
        }
    }
}

