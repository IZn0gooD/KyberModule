# 🌳 Arborescence du Projet KyberModule

Ce document décrit la structure complète du projet et explique le rôle de chaque composant.

## 📁 Structure Générale

```
KyberModule/
├── 📄 Documentation
├── 🔧 Scripts de build et déploiement
├── 🧩 Projets .NET (bibliothèques et applications)
├── 🧪 Tests et benchmarks
└── 📦 Fichiers de configuration
```

---

## 📚 Documentation

### Fichiers Markdown principaux

- **`README.md`** : Documentation principale du projet, guide d'installation, utilisation et architecture
- **`QUICKSTART.md`** : Guide de démarrage rapide pour générer des clés et établir une session
- **`PROTOCOLE_COMMUNICATION_SECURISEE.md`** : Documentation détaillée du protocole de communication sécurisée
- **`DEPLOIEMENT_WINDOWS_GUIDE.md`** : Guide complet de déploiement sur Windows
- **`ARBORESCENCE.md`** : Ce fichier - description de l'organisation du projet

### Documentation technique

- **`ANALYSE_DILITHIUM.md`** : Analyse technique de l'implémentation Dilithium
- **`CONTRE_MESURES_SIDE_CHANNEL.md`** : Documentation sur les protections contre les attaques par canaux auxiliaires
- **`GESTION_SECURISEE_CLES.md`** : Guide de gestion sécurisée des clés cryptographiques
- **`MIGRATION_BOUNCYCASTLE.md`** : Guide de migration vers BouncyCastle
- **`RECHERCHE_ET_DEVELOPPEMENT.md`** : Notes de recherche et développement

---

## 🔧 Scripts de Build et Déploiement

### Scripts principaux

- **`BUILD.ps1`** : Script principal de compilation du projet
- **`CLEANUP.ps1`** : Script de nettoyage des fichiers de build
- **`REBUILD_AND_TEST.ps1`** : Script de reconstruction complète et exécution des tests

### Dossier `deploy/`

- **`build-windows-package.ps1`** : Compilation et création de l'archive Windows (`KyberModule-win-x64.zip`)
- **`build-linux-package.sh`** : Compilation et création du package Linux
- **`deploy-windows-package.ps1`** : Script de déploiement automatique sur Windows (installation service, configs, etc.)

### Dossier `scripts/`

- **`build-all.ps1`** : Script de compilation de tous les projets

---

## 🧩 Projets .NET

### 🎯 KyberModule (Module PowerShell)

**Rôle** : Module PowerShell binaire exposant les cmdlets pour la cryptographie post-quantique.

**Fichiers clés** :
- **`KyberModule.psd1`** : Manifeste PowerShell définissant les cmdlets exportés
- **`NewKyberKeyPairCommand.cs`** : Cmdlet `New-KyberKeyPair` pour générer des paires de clés Kyber
- **`NewDilithiumKeyPairCommand.cs`** : Cmdlet `New-DilithiumKeyPair` pour générer des paires de clés Dilithium
- **`NewKyberKeyBundleCommand.cs`** : Cmdlet `New-KyberKeyBundle` pour générer toutes les clés nécessaires (client/serveur)
- **`InvokeKyberEncapsulateCommand.cs`** : Cmdlet `Invoke-KyberEncapsulate` pour l'encapsulation de clés
- **`InvokeKyberDecapsulateCommand.cs`** : Cmdlet `Invoke-KyberDecapsulate` pour la décapsulation de clés
- **`InvokeDilithiumSignCommand.cs`** : Cmdlet `Invoke-DilithiumSign` pour signer avec Dilithium
- **`NewX509CertificateCommand.cs`** : Cmdlet `New-X509Certificate` pour créer des certificats X.509
- **`*.ps1`** : Scripts d'exemples PowerShell pour chaque fonctionnalité

**Dépendances** : `KyberLibrary`, `KyberDomain`

---

### 📚 KyberLibrary (Infrastructure .NET)

**Rôle** : Couche d'abstraction entre le domaine métier et BouncyCastle, implémentations des wrappers cryptographiques.

**Fichiers clés** :
- **`KyberWrapper.cs`** : Wrapper pour les opérations Kyber (ML-KEM) - génération, encapsulation, décapsulation
- **`DilithiumWrapper.cs`** : Wrapper pour les opérations Dilithium (ML-DSA) - génération, signature, vérification, `SignBatch` pour parallélisation
- **`Ed25519Wrapper.cs`** : Wrapper pour Ed25519 (signature classique, compatibilité)
- **`SHA3Wrapper.cs`** : Wrapper pour SHA3-256 et SHA3-384
- **`ChaCha20Poly1305Wrapper.cs`** : Wrapper pour le chiffrement AEAD ChaCha20-Poly1305
- **`AESGCMWrapper.cs`** : Wrapper pour le chiffrement AEAD AES-GCM
- **`PKCS8Wrapper.cs`** : Import/export de clés au format PKCS#8
- **`X509Wrapper.cs`** : Gestion des certificats X.509
- **`CMSWrapper.cs`** : Support CMS (Cryptographic Message Syntax)
- **`TLS13HybridWrapper.cs`** : Support TLS 1.3 hybride (post-quantique + classique)
- **`SecureSession.cs`** : Gestion des sessions sécurisées (obsolète, remplacé par `KyberDomain`)
- **`SecureKeyManager.cs`** : Gestion sécurisée des clés en mémoire
- **`SideChannelProtection.cs`** : Protections contre les attaques par canaux auxiliaires
- **`HardwareAcceleration.cs`** : Détection et utilisation de l'accélération matérielle
- **`PerformanceOptimizer.cs`** : Optimisations de performance
- **`ParallelCryptographicOperations.cs`** : Opérations cryptographiques parallélisées
- **`ConstantTimeOperations.cs`** : Opérations à temps constant pour éviter les fuites d'information
- **`DomainAdapters/`** : Adaptateurs entre `KyberDomain` et `KyberLibrary`

**Dépendances** : `KyberDomain`, `BouncyCastle.Cryptography`

---

### 🏛️ KyberDomain (Domaine Métier)

**Rôle** : Couche métier définissant les interfaces et abstractions pour la cryptographie post-quantique (Domain-Driven Design).

**Fichiers clés** :
- **`Cryptography/IKeyEncapsulationService.cs`** : Interface pour l'encapsulation de clés (Kyber)
- **`Cryptography/ISignatureService.cs`** : Interface pour la signature numérique (Dilithium, Ed25519)
- **`Cryptography/IAeadCipherProvider.cs`** : Interface pour le chiffrement AEAD (ChaCha20-Poly1305, AES-GCM)
- **`Cryptography/ISecureSessionFactory.cs`** : Factory pour créer des sessions sécurisées
- **`Cryptography/ISessionKeyDeriver.cs`** : Interface pour la dérivation de clés de session
- **`Cryptography/SecureSession.cs`** : Classe principale représentant une session sécurisée (métier)
- **`Cryptography/SecureSessionOptions.cs`** : Options de configuration pour les sessions
- **`Cryptography/CryptoEnums.cs`** : Énumérations pour les algorithmes et paramètres
- **`Security/ISideChannelDefense.cs`** : Interface pour les défenses contre les attaques par canaux auxiliaires
- **`Security/NoopSideChannelDefense.cs`** : Implémentation vide (pas de protection)
- **`Security/IKeyMaterialProtector.cs`** : Interface pour la protection des matériaux de clés
- **`Security/INonceGenerator.cs`** : Interface pour la génération de nonces

**Dépendances** : Aucune (couche métier pure)

---

### 🔗 KyberShared (Protocole Réseau)

**Rôle** : Types partagés pour le protocole de communication entre `KyberDaemon` et `KyberCLI`.

**Fichiers clés** :
- **`Protocol/DaemonMessage.cs`** : Classe de base pour les messages du protocole
- **`Protocol/DaemonMessageType.cs`** : Types de messages (ClientHello, ServerHello, AuthRequest, etc.)
- **`Protocol/HandshakeMessages.cs`** : Messages du handshake Kyber/Dilithium
- **`Protocol/AuthMessages.cs`** : Messages d'authentification (challenge/response)
- **`Protocol/SessionMessages.cs`** : Messages de session (commandes, résultats)
- **`Protocol/SecureMessageChannel.cs`** : Canal de communication sécurisé
- **`Security/Argon2HashRecord.cs`** : Format d'enregistrement Argon2id pour les mots de passe

**Dépendances** : `KyberDomain`

---

### 🔐 KyberKeyManagement (Gestion des Clés)

**Rôle** : Service de gestion des clés avec rotation, chiffrement au repos, et métadonnées.

**Fichiers clés** :
- **`KeyRotationService.cs`** : Service de rotation automatique des clés
- **`KeyEncryptionService.cs`** : Service de chiffrement des clés au repos (DPAPI, passphrase)
- **`KeyMetadataStore.cs`** : Stockage des métadonnées des clés (versions, dates)
- **`KeyStorageOptions.cs`** : Options de stockage des clés
- **`IKeyLogger.cs`** : Interface de journalisation pour les opérations sur les clés

**Dépendances** : `KyberDomain`

---

### 🖥️ KyberDaemon (Service Serveur)

**Rôle** : Service Windows/Linux qui écoute les connexions et exécute des commandes dans des sessions sécurisées.

**Structure** :
- **`Program.cs`** : Point d'entrée du service (.NET Generic Host)
- **`Core/KyberDaemonService.cs`** : Service principal
- **`Core/KyberHostedService.cs`** : Service hébergé pour la gestion du cycle de vie
- **`Core/DaemonHostArguments.cs`** : Arguments de ligne de commande

**Réseau** :
- **`Networking/ConnectionListener.cs`** : Écoute des connexions TCP entrantes

**Sessions** :
- **`Sessions/SessionManager.cs`** : Gestion des sessions clientes (handshake, authentification, exécution)
- **`Sessions/`** : Autres fichiers de gestion de session

**Authentification** :
- **`Authentication/AuthManager.cs`** : Gestionnaire d'authentification (mot de passe Argon2id, clés Dilithium)
- **`Authentication/KerberosAuthenticationService.cs`** : Support Kerberos (optionnel)
- **`Authentication/OAuthTokenValidator.cs`** : Support OAuth (optionnel)

**Configuration** :
- **`Configuration/ConfigLoader.cs`** : Chargement de la configuration depuis `kyberd.conf`
- **`Configuration/DaemonConfiguration.cs`** : Classe de configuration typée

**Journalisation** :
- **`Logging/DaemonLogger.cs`** : Logger structuré JSON (console + fichiers)
- **`Logging/LogFileSink.cs`** : Sink pour l'écriture dans les fichiers avec rotation

**Sécurité** :
- **`Security/KeyManagementLoggerAdapter.cs`** : Adaptateur pour la journalisation des opérations sur les clés

**Configuration** :
- **`config/kyberd.conf`** : Fichier de configuration principal (port, clés, authentification, etc.)
- **`config/auth.conf`** : Fichier d'authentification (utilisateurs, mots de passe Argon2id, clés publiques Dilithium)

**Déploiement** :
- **`deploy/install-kyberd-service.ps1`** : Script d'installation du service Windows
- **`deploy/uninstall-kyberd-service.ps1`** : Script de désinstallation du service
- **`deploy/kyberd.service`** : Fichier systemd pour Linux
- **`tools/Invoke-KyberKeyRotation.ps1`** : Script PowerShell pour forcer la rotation des clés

**Dépendances** : `KyberShared`, `KyberDomain`, `KyberKeyManagement`

---

### 💻 KyberCLI (Client en Ligne de Commande)

**Rôle** : Client CLI pour se connecter à `KyberDaemon` et exécuter des commandes dans une session sécurisée.

**Structure** :
- **`Program.cs`** : Point d'entrée du CLI
- **`Commands/IKyberCommand.cs`** : Interface pour les commandes
- **`Commands/CommandLineParser.cs`** : Parser de ligne de commande
- **`Commands/ConnectCommand.cs`** : Commande `connect` pour établir une session persistante
- **`Commands/HelpCommand.cs`** : Commande `help` pour l'aide
- **`Commands/KeysCommand.cs`** : Commande `keys` pour la gestion des clés
- **`Commands/PasswordHashCommand.cs`** : Commande `password-hash` pour générer des hash Argon2id

**Protocole** :
- **`Protocol/KyberDaemonClient.cs`** : Client du protocole (handshake, authentification, envoi de commandes)
- **`Protocol/InteractiveCommandAssistant.cs`** : Assistant interactif pour l'exécution de commandes

**État** :
- **`State/CliStateStore.cs`** : Stockage de l'état du CLI (sessions, clés)

**Dépendances** : `KyberShared`, `KyberDomain`

---

## 🧪 Tests et Benchmarks

### `tests/KyberIntegrationTests/`

**Rôle** : Tests d'intégration end-to-end pour valider le fonctionnement complet du système.

**Fichiers clés** :
- **`EndToEndTests.cs`** : Tests end-to-end (CLI, daemon, sessions)
- **`DaemonTestHost.cs`** : Hôte de test isolé pour le daemon
- **`CliRunner.cs`** : Utilitaire pour exécuter le CLI dans les tests
- **`NetworkFuzzer.cs`** : Utilitaire pour le fuzzing réseau (tests de robustesse)
- **`DilithiumOptimizationsTests.cs`** : Tests des optimisations Dilithium (`SignBatch`, etc.)

### `tests/KyberBenchmarks/`

**Rôle** : Benchmarks de performance avec BenchmarkDotNet.

**Fichiers clés** :
- **`Program.cs`** : Point d'entrée des benchmarks
- **`CommandLatencyBenchmark.cs`** : Mesure de la latence des commandes
- **`HandshakeBenchmark.cs`** : Mesure du coût du handshake et de la terminaison de session
- **`ConcurrentCommandsBenchmark.cs`** : Mesure des performances sous charge concurrente

---

## 📦 Fichiers de Configuration

### `.gitignore`

Fichier Git ignorant les artefacts de build, les clés privées, et autres fichiers sensibles :
- `bin/`, `obj/` (artefacts de compilation)
- `*.key`, `*.prv`, `*.pem` (clés privées)
- `dist/`, `publish/` (artefacts de déploiement)
- `*.log` (fichiers de logs)

### Fichiers de configuration du daemon

- **`KyberDaemon/config/kyberd.conf`** : Configuration principale (port, adresses, clés, quotas, logs)
- **`KyberDaemon/config/auth.conf`** : Configuration d'authentification (utilisateurs, mots de passe, clés publiques)

---

## 🔄 Flux de Données

### Génération de clés

```
PowerShell → New-KyberKeyPair → KyberModule → KyberLibrary → KyberWrapper → BouncyCastle
```

### Session sécurisée (KyberDaemon + KyberCLI)

```
KyberCLI → KyberShared (protocole) → KyberDaemon → KyberDomain → SecureSession → KyberLibrary → BouncyCastle
```

### Architecture en couches

```
┌─────────────────────────────────────┐
│  KyberModule (Cmdlets PowerShell)   │
└──────────────┬──────────────────────┘
               │
┌──────────────▼──────────────────────┐
│  KyberCLI / KyberDaemon (Apps)       │
└──────────────┬──────────────────────┘
               │
┌──────────────▼──────────────────────┐
│  KyberShared (Protocole réseau)      │
└──────────────┬──────────────────────┘
               │
┌──────────────▼──────────────────────┐
│  KyberLibrary (Infrastructure .NET)  │
└──────────────┬──────────────────────┘
               │
┌──────────────▼──────────────────────┐
│  KyberDomain (Domaine métier)       │
└──────────────┬──────────────────────┘
               │
┌──────────────▼──────────────────────┐
│  BouncyCastle.Cryptography          │
│  (ML-KEM, ML-DSA)                   │
└─────────────────────────────────────┘
```

---

## 📝 Notes Importantes

### Séparation des responsabilités

- **KyberDomain** : Définit **QUOI** (interfaces, abstractions métier)
- **KyberLibrary** : Définit **COMMENT** (implémentations, wrappers BouncyCastle)
- **KyberModule** : Expose **POUR QUI** (cmdlets PowerShell)
- **KyberDaemon/KyberCLI** : Utilisent **KyberDomain** via les interfaces

### Fichiers obsolètes

Certains fichiers dans `KyberLibrary` (comme `SecureSession.cs`) sont marqués obsolètes car remplacés par les équivalents dans `KyberDomain`. Ils sont conservés pour compatibilité.

### Dépendances

- **BouncyCastle.Cryptography 2.6.2+** : Implémentation ML-KEM et ML-DSA conforme NIST
- **.NET 8.0** : Pour `KyberDaemon`, `KyberCLI`, `KyberShared`
- **.NET Standard 2.0** : Pour `KyberModule`, `KyberLibrary`, `KyberDomain` (compatibilité PowerShell)

---

## 🚀 Points d'Entrée Principaux

1. **Module PowerShell** : `Import-Module KyberModule` → Utilisation des cmdlets
2. **Service Daemon** : `KyberDaemon.exe` → Démarrage du service serveur
3. **Client CLI** : `KyberCLI.exe connect --host ...` → Connexion à un daemon
4. **Tests** : `dotnet test` → Exécution des tests d'intégration
5. **Benchmarks** : `dotnet run --project tests/KyberBenchmarks` → Exécution des benchmarks

---

*Dernière mise à jour : 2025-01-13*

