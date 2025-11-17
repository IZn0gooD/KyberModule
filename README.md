# KyberModule - Module PowerShell pour la Cryptographie Post-Quantique Kyber

## 📋 Table des matières

1. [Vue d'ensemble](#vue-densemble)
2. [Architecture du module](#architecture-du-module)
3. [KyberDaemon & KyberCLI](#-kyberdaemon--kybercli)
4. [Structure des fichiers](#structure-des-fichiers)
5. [Installation](#installation)
6. [Guide d'utilisation](#guide-dutilisation)
7. [Détails techniques](#détails-techniques)
8. [Dépannage](#dépannage)
9. [Ressources](#-ressources)

---

## 🎯 Vue d'ensemble

**KyberModule** est un module PowerShell binaire (Standard Library) qui implémente les algorithmes de cryptographie post-quantique **Kyber (ML-KEM)** et **Dilithium (ML-DSA)** conformes aux spécifications NIST, permettant de créer des protocoles sécurisés 100% post-quantiques. Le module inclut également **Ed25519** pour la signature numérique classique (compatibilité). Ce module permet de générer des paires de clés, d'encapsuler et de décapsuler des clés partagées pour des communications sécurisées résistantes aux ordinateurs quantiques, et de signer/vérifier des données avec Dilithium ou Ed25519.

### Caractéristiques principales

- ✅ **Module PowerShell binaire** : Implémentation native en C# pour des performances optimales
- ✅ **Conformité NIST** : Utilise BouncyCastle.Cryptography 2.6.2, implémentation officielle de ML-KEM et ML-DSA
- ✅ **Trois niveaux de sécurité** : Kyber512, Kyber768, Kyber1024
- ✅ **API PowerShell native** : Cmdlets intégrés dans PowerShell
- ✅ **Support cross-platform** : Compatible PowerShell Core et Windows PowerShell
- ✅ **Dilithium (ML-DSA)** : Support complet de la signature numérique post-quantique Dilithium (100% post-quantique)
  - Thread-local `SecureRandom`, nettoyage mémoire renforcé lors des signatures/vérifications
  - API `SignBatch` pour paralléliser des séries de signatures (utilisée par les benchmarks)
- ✅ **Ed25519** : Support complet de la signature numérique Ed25519 pour compatibilité
- ✅ **Fonctions de hachage modernes** : SHA3-256 et SHA3-384 avec pré-hachage pour ML-DSA
- ✅ **Chiffrement symétrique AEAD** : ChaCha20-Poly1305 et AES-GCM pour l'authenticated encryption
- ✅ **Conformité standards** : Support PKCS#8, X.509 (certificats auto-signés), CMS pour intégration PKI
- ✅ **Format sécurisé** : Format texte simple Key=Value, sans dépendances vulnérables
- ✅ **Protocoles combinés** : Scénarios Kyber + Dilithium pour une sécurité 100% post-quantique
- ✅ **BouncyCastle.Cryptography 2.6.2** : Version mise à jour avec support ML-KEM et ML-DSA
- ✅ **Service & CLI** : Démon `KyberDaemon` + client `KyberCLI` pour les communications chiffrées (style sshd)

---

## 🏗️ Architecture du module

Le projet suit désormais une séparation de responsabilités inspirée du **Domain-Driven Design** :

```
┌─────────────────────────────────────────┐
│   KyberModule (Module PowerShell)       │
│   - Cmdlets PowerShell en C#            │
└──────────────┬──────────────────────────┘
               │
               ▼
┌─────────────────────────────────────────┐
│   KyberLibrary (Infrastructure .NET)    │
│   - Adapters BouncyCastle / services    │
│   - SecureSessionFactory                │
└──────────────┬──────────────────────────┘
               │
               ▼
┌─────────────────────────────────────────┐
│   KyberDomain (Domaine métier)          │
│   - Interfaces KEM/Signature/AEAD       │
│   - SecureSession (métier)              │
└──────────────┬──────────────────────────┘
               │
               ▼
┌─────────────────────────────────────────┐
│   BouncyCastle.Cryptography            │
│   - Implémentation ML-KEM/Kyber        │
│   - Conforme NIST FIPS 203             │
└─────────────────────────────────────────┘
```

KyberShared (protocole réseau) et KyberDaemon/KyberCLI consomment la couche métier via les interfaces exposées dans `KyberDomain`. Le cœur `SecureSession` applique désormais une stratégie `ISideChannelDefense` (implémentation par défaut : `AdvancedSideChannelDefense`) qui nettoie le cache, réalise des accès mémoire uniformes et introduit un bruit temporel maîtrisé autour de chaque opération sensible (Kyber, Dilithium, AEAD).

### Flux de données

1. **Génération de clés** : `New-KyberKeyPair` → `KyberWrapper.GenerateKeyPair()` → BouncyCastle
2. **Encapsulation** : `Invoke-KyberEncapsulate` → `KyberWrapper.Encapsulate()` → BouncyCastle
3. **Décapsulation** : `Invoke-KyberDecapsulate` → `KyberWrapper.Decapsulate()` → BouncyCastle

---

## 🔐 KyberDaemon & KyberCLI

Depuis 2025, les communications distantes reposent sur le couple **KyberDaemon** (service style `sshd`) et **KyberCLI** (client natif). Les anciennes cmdlets réseau (`Start-SecureServer`, `Connect-SecureClient`, `Send/Receive-SecureMessage`) sont conservées pour compatibilité mais marquées *obsolètes*.

### KyberDaemon
- Service .NET 8 basé sur le **Generic Host** (`UseWindowsService` / `UseSystemd`)
- Handshake post-quantique : Kyber768 + Dilithium3, canal chiffré ChaCha20-Poly1305
- Gestion des sessions : PowerShell Core sur Windows (`pwsh.exe` avec fallback `powershell.exe`) ou shell Linux configurable
- Consomme `KyberDomain` via `SecureSessionFactory` (séparation métier/infrastructure)
- Journalisation structurée JSON (console + fichiers) avec rotation configurable (`log_directory`, `log_file_name`, `log_max_size_mb`, `log_max_files`)
- Stockage des clés serveur chiffré au repos (DPAPI sur Windows ou AES+PBKDF2 via `key_encryption_mode=passphrase`) avec rotation versionnée (`key_rotation_days`, fichiers `session.vN.*`). Les packages Linux génèrent automatiquement une passphrase aléatoire dans `config/kyberd.conf`.
- Quotas multi-sessions : contrôle global (`max_connections`) et par utilisateur (`max_sessions_per_user`) avec rejet des connexions excédentaires
- Mode strict (`strict_network_mode=true`) : KyberDaemon n'écoute plus que sur 127.0.0.1 et refuse toute connexion distante (à utiliser avec un reverse proxy TLS)
- Authentification mot de passe : les entrées `auth.conf` stockent désormais des enregistrements **Argon2id** salés (`argon2id$v=…$salt$hash`) ; la vérification repose sur un challenge SHA3 dérivé du secret Argon2
- Configuration texte `config/kyberd.conf` (copiée automatiquement dans `%PROGRAMDATA%\KyberDaemon` ou `/etc/kyberd`)
- Déploiement :
  - **Linux** : fichier `deploy/kyberd.service` (systemd) → `dotnet publish`, copie dans `/opt/kyberd`, configs `/etc/kyberd`, `systemctl enable --now kyberd`
  - **Windows** : `deploy\install-kyberd-service.ps1` (service Windows natif, exécute `KyberDaemon.exe` ; nécessite PowerShell admin)

#### Exposition via reverse proxy (TLS / HSTS)
- Limiter l'écoute côté daemon : `listen_address = 127.0.0.1` et `listen_port = 8443` (trafic interne uniquement)
- Exemple **nginx stream** :
  ```nginx
  stream {
      upstream kyberd_backend { server 127.0.0.1:8443; }

      server {
          listen 443 ssl;
          ssl_certificate     /etc/ssl/certs/kyberd.crt;
          ssl_certificate_key /etc/ssl/private/kyberd.key;
          proxy_pass kyberd_backend;
      }
  }
  ```
- Activer HSTS côté frontal HTTP (si vous exposez également une API REST) : `add_header Strict-Transport-Security "max-age=63072000; includeSubDomains" always;`
- **KyberCLI** côté client : `--tls` active le canal chiffré, `--tls-cert-fingerprint "sha256:ABCD…"` épingle le certificat présenté par le reverse proxy
- Empreinte SHA-256 : `openssl x509 -in cert.pem -noout -fingerprint -sha256`
- Option `--tls-skip-verify` réservée aux environnements de test (accepte les certificats auto-signés)

### KyberCLI
- Client `dotnet` (net8.0) exposant la commande `connect`
- Auto-complétion locale (Tab) alimentée par la liste des commandes PowerShell (`Get-Command`) et historique persisté
- Alias utilisateurs persistants (`:alias`, `:unalias`, `:aliases`) stockés dans `%APPDATA%\KyberCLI`, disponibles dans l'auto-complétion
- Rendu couleur des sorties (stdout gris, stderr rouge) pour différencier les flux en temps réel
- Commande d'administration `KyberCLI keys <list|rotate|export>` pour gérer les clés serveur (lecture de `kyberd.conf`, rotation, export)
- Assistant interactif intégré (`:help`, `:examples`, `:run <alias>`, `:history`, `:validate <commande>`, `:clear`) avec bibliothèque de scénarios prêts à l'emploi adaptés à la plateforme
- Validation temps réel du premier jet : avertissement si la commande n'est pas connue côté serveur (confirmation utilisateur avant exécution)
- Authentification :
  - `--password-hex` accepte l'ancien secret hexadécimal (compatibilité)
  - `--password` + `--password-record` dérivent automatiquement le secret via **Argon2id** (enregistrement identique à celui stocké dans `auth.conf`)
  - `--kerberos-token` transmet un ticket Kerberos AP-REQ (Base64) pour validation via keytab côté serveur
  - `--oauth-token` transmet un jeton OAuth2/JWT, validé côté serveur via JWKS `oauth_jwks`
  - `--key` prend toujours une clé privée Dilithium (base64)
  - `--trace-level <none|handshake|auth|full>` affiche les étapes clés du handshake et/ou de l'authentification côté client
- Usage :
  ```powershell
  # Mot de passe dérivé via Argon2 (enregistrement récupéré lors de la création du compte)
  dotnet KyberCLI.dll connect --host serveur --user alice --password "ChangeMe!123" \
     --password-record "argon2id$v=19$m=65536,t=3,p=2$pad2ZtQRdzTOr2zecPUMng==$wnzKuHLwtQSOloqyRr5XMN671g6c18w2UbIaPOu9Zkw="

  # Authentification Kerberos (ticket AP-REQ préalablement obtenu via kinit)
  dotnet KyberCLI.dll connect --host serveur --user charlie \
     --kerberos-token $(./get-kerberos-token.sh)

  # Authentification OAuth (jeton JWT signé)
  dotnet KyberCLI.dll connect --host serveur --user diane \
     --oauth-token $(cat access_token.txt)

  # Secret hexadécimal historique (toujours supporté)
  dotnet KyberCLI.dll connect --host serveur --user legacy --password-hex 0xABCDEF...

  # Signature Dilithium (clé privée base64)
  dotnet KyberCLI.dll connect --host serveur --user bob --key bob.key --command "ls -la"
  ```
- Mode interactif : omettre `--command` pour entrer dans une session type SSH (PowerShell Core sur Windows)
- Génération d'enregistrement Argon2 :
  ```powershell
  dotnet KyberCLI.dll passhash --username alice --password "ChangeMe!123"
  ```
  produit :
  ```
  alice:password:argon2id$v=19$m=65536,t=3,p=2$pad2ZtQRdzTOr2zecPUMng==$wnzKuHLwtQSOloqyRr5XMN671g6c18w2UbIaPOu9Zkw=
  ```
  À copier dans `auth.conf`.

- Kerberos : générer un ticket applicatif AP-REQ (ex: `kvno host/kyberd.local`) et passer le résultat Base64 avec `--kerberos-token`. Activer côté serveur (`kerberos_enabled=true`) et fournir le keytab (`kerberos_keytab`).
- OAuth : fournir un jeton JWT (`--oauth-token` ou `--oauth-token-file`). Côté serveur, configurer `oauth_issuer`, `oauth_audience`, `oauth_jwks` (fichier JWKS) et éventuellement `oauth_required_scopes`.

### Intégration PowerShell
- `Invoke-SecureCommand` appelle KyberCLI en arrière-plan. Paramètres : `-ServerHost`, `-Port`, `-Username`, `-PasswordHex`/`-KeyPath`, `-Command`, `-CliPath`, `-RebuildCli`
- Les cmdlets de génération de clés (`New-KyberKeyPair`, `New-DilithiumKeyPair`, ...) restent inchangées

---

## 📁 Structure des fichiers

### Répertoire racine : `KyberModule/`

```
KyberModule/
├── KyberModule/              # Projet module PowerShell
│   ├── KyberModule.csproj    # Fichier projet .NET
│   ├── KyberModule.psd1      # Manifeste PowerShell
│   ├── KyberModule.dll       # DLL compilée du module
│   ├── GetKyberKeyPairCommand.cs      # Cmdlet New-KyberKeyPair
│   ├── InvokeKyberEncapsulateCommand.cs # Cmdlet Invoke-KyberEncapsulate
│   ├── InvokeKyberDecapsulateCommand.cs  # Cmdlet Invoke-KyberDecapsulate
│   ├── Examples.ps1          # Script d'exemples
│   ├── Test.ps1              # Script de tests
│   └── DebugFields.ps1       # Script de débogage
│
├── KyberLibrary/             # Projet bibliothèque .NET
│   ├── KyberLibrary.csproj   # Fichier projet .NET
│   ├── KyberLibrary.dll      # DLL compilée de la bibliothèque
│   └── KyberWrapper.cs       # Classe wrapper principale
│
└── README.md                 # Cette documentation
```

### Fichiers principaux

#### 1. **KyberModule.csproj** 
**Localisation** : `KyberModule/KyberModule.csproj`

**Description** : Fichier projet .NET pour le module PowerShell Standard Library.

**Contenu clé** :
- Framework cible : `netstandard2.0`
- Dépendances :
  - `PowerShellStandard.Library` : Bibliothèque pour créer des cmdlets PowerShell en C#
  - `BouncyCastle.NetCore` : Implémentation cryptographique Kyber
- Référence au projet `KyberLibrary`

**Rôle** : Définit la structure, les dépendances et les paramètres de compilation du module PowerShell.

---

#### 2. **KyberLibrary.csproj**
**Localisation** : `KyberModule/KyberLibrary/KyberLibrary.csproj`

**Description** : Fichier projet .NET pour la bibliothèque de logique métier.

**Contenu clé** :
- Framework cible : `netstandard2.0`
- Dépendance : `BouncyCastle.NetCore`

**Rôle** : Définit la bibliothèque .NET qui encapsule la logique Kyber.

---

#### 3. **KyberModule.psd1**
**Localisation** : `KyberModule/KyberModule/KyberModule.psd1`

**Description** : Manifeste PowerShell qui décrit le module.

**Informations importantes** :
- `RootModule` : `KyberModule.dll` - Point d'entrée du module
- `RequiredAssemblies` : Liste les DLLs nécessaires (`KyberLibrary.dll`, `BouncyCastle.Crypto.dll`)
- `CmdletsToExport` : Liste les cmdlets exportés
  - `New-KyberKeyPair`
  - `Invoke-KyberEncapsulate`
  - `Invoke-KyberDecapsulate`
- `ModuleVersion` : Version du module
- `CompatiblePSEditions` : Core et Desktop

**Rôle** : Permet à PowerShell de charger correctement le module et ses dépendances.

---

#### 4. **KyberWrapper.cs**
**Localisation** : `KyberModule/KyberLibrary/KyberWrapper.cs`

**Description** : Classe principale qui encapsule les opérations Kyber de BouncyCastle.

**Méthodes principales** :

| Méthode | Description |
|---------|-------------|
| `GenerateKeyPair()` | Génère une paire de clés (publique/privée) |
| `Encapsulate(byte[] publicKey)` | Encapsule une clé partagée avec une clé publique |
| `Decapsulate(byte[] ciphertext, byte[] privateKey)` | Décapsule une clé partagée avec une clé privée |
| `BytesToHex(byte[] bytes)` | Convertit bytes en hexadécimal |
| `HexToBytes(string hex)` | Convertit hexadécimal en bytes |

**Détails techniques** :

- **Sérialisation personnalisée** : La clé privée est sérialisée dans un format personnalisé avec magic number "KYBE" pour éviter les problèmes avec le format `GetEncoded()` de BouncyCastle.

- **Format de clé privée** :
  ```
  [Magic(4 bytes: "KYBE")] 
  [sLength(4)]s[hpkLength(4)]hpk[nonceLength(4)]nonce[tLength(4)]t[rhoLength(4)]rho
  ```

- **Paramètres Kyber** : Support de Kyber512, Kyber768, Kyber1024 via l'enum `KyberParameterSet`.

**Rôle** : Abstraction de la complexité de BouncyCastle et fournit une API simple et fiable.

---

#### 5. **GetKyberKeyPairCommand.cs**
**Localisation** : `KyberModule/KyberModule/GetKyberKeyPairCommand.cs`

**Description** : Cmdlet PowerShell `New-KyberKeyPair` pour générer des paires de clés.

**Paramètres** :
- `-ParameterSet` : Niveau de sécurité (Kyber512, Kyber768, Kyber1024) - Par défaut : Kyber768

**Retour** : Objet `KyberKeyPairResult` avec :
- `PublicKey` : byte[] de la clé publique
- `PrivateKey` : byte[] de la clé privée
- `PublicKeyHex` : Clé publique en hexadécimal
- `PrivateKeyHex` : Clé privée en hexadécimal
- `ParameterSet` : Paramètre utilisé

**Exemple** :
```powershell
$keys = New-KyberKeyPair -ParameterSet Kyber768
```

**Rôle** : Interface PowerShell pour la génération de clés.

---

#### 6. **InvokeKyberEncapsulateCommand.cs**
**Localisation** : `KyberModule/KyberModule/InvokeKyberEncapsulateCommand.cs`

**Description** : Cmdlet PowerShell `Invoke-KyberEncapsulate` pour encapsuler une clé partagée.

**Paramètres** :
- `-PublicKey` : Clé publique (byte[] ou chaîne hexadécimale) - **Obligatoire**
- `-ParameterSet` : Niveau de sécurité (Kyber512, Kyber768, Kyber1024) - Par défaut : Kyber768

**Retour** : Objet `KyberEncapsulationResult` avec :
- `Ciphertext` : byte[] du ciphertext
- `SharedSecret` : byte[] de la clé partagée
- `CiphertextHex` : Ciphertext en hexadécimal
- `SharedSecretHex` : Clé partagée en hexadécimal

**Exemple** :
```powershell
$encapsulated = Invoke-KyberEncapsulate -PublicKey $keys.PublicKey -ParameterSet Kyber768
```

**Rôle** : Interface PowerShell pour l'encapsulation (côté Bob dans un échange de clés).

---

#### 7. **InvokeKyberDecapsulateCommand.cs**
**Localisation** : `KyberModule/KyberModule/InvokeKyberDecapsulateCommand.cs`

**Description** : Cmdlet PowerShell `Invoke-KyberDecapsulate` pour décapsuler une clé partagée.

**Paramètres** :
- `-Ciphertext` : Ciphertext (byte[] ou chaîne hexadécimale) - **Obligatoire**
- `-PrivateKey` : Clé privée (byte[] ou chaîne hexadécimale) - **Obligatoire**
- `-ParameterSet` : Niveau de sécurité (Kyber512, Kyber768, Kyber1024) - Par défaut : Kyber768

**Retour** : Objet `KyberDecapsulationResult` avec :
- `SharedSecret` : byte[] de la clé partagée
- `SharedSecretHex` : Clé partagée en hexadécimal

**Exemple** :
```powershell
$sharedSecret = Invoke-KyberDecapsulate -Ciphertext $encapsulated.Ciphertext -PrivateKey $keys.PrivateKey -ParameterSet Kyber768
```

**Rôle** : Interface PowerShell pour la décapsulation (côté Alice dans un échange de clés).

---

#### 8. **Examples.ps1**
**Localisation** : `KyberModule/KyberModule/Examples.ps1`

**Description** : Script PowerShell contenant des exemples d'utilisation du module.

**Contenu** :
- Exemple 1 : Génération basique de clés
- Exemple 2 : Échange de clés entre Alice et Bob
- Exemple 3 : Utilisation avec des chaînes hexadécimales
- Exemple 4 : Comparaison des paramètres de sécurité

**Rôle** : Documentation interactive et exemples pratiques.

---

#### 11. **ExportKyberKeyPairCommand.cs**
**Localisation** : `KyberModule/KyberModule/ExportKyberKeyPairCommand.cs`

**Description** : Cmdlet `Export-KyberKeyPair` pour exporter une paire de clés vers un fichier.

**Paramètres** :
- `-KeyPair` : Objet `KyberKeyPairResult` à exporter - **Obligatoire**
- `-Path` : Chemin du fichier de sortie - **Obligatoire**
- `-Format` : Format de sortie (JSON ou Base64) - Par défaut : JSON
- `-Force` : Surcharger le fichier s'il existe

**Formats supportés** :
- **Text** : Format texte simple Key=Value (sécurisé, sans dépendances externes) - **Recommandé**
- **Base64** : Format PEM-like compatible avec SSH et autres protocoles

**Exemple** :
```powershell
$keys = New-KyberKeyPair
Export-KyberKeyPair -KeyPair $keys -Path "keys.txt" -Format Text
Export-KyberKeyPair -KeyPair $keys -Path "keys.pem" -Format Base64
```

**Rôle** : Permet de sauvegarder les clés pour une utilisation ultérieure ou un partage sécurisé.

---

#### 12. **ImportKyberKeyPairCommand.cs**
**Localisation** : `KyberModule/KyberModule/ImportKyberKeyPairCommand.cs`

**Description** : Cmdlet `Import-KyberKeyPair` pour importer une paire de clés depuis un fichier.

**Paramètres** :
- `-Path` : Chemin du fichier contenant la paire de clés - **Obligatoire**

**Formats supportés** :
- Text (format Key=Value exporté par `Export-KyberKeyPair`)
- Base64/PEM (format compatible SSH)

**Exemple** :
```powershell
$keys = Import-KyberKeyPair -Path "keys.txt"
```

**Rôle** : Permet de charger des clés sauvegardées précédemment.

---

#### 13. **ExportKyberPublicKeyCommand.cs**
**Localisation** : `KyberModule/KyberModule/ExportKyberPublicKeyCommand.cs`

**Description** : Cmdlet `Export-KyberPublicKey` pour exporter uniquement une clé publique.

**Paramètres** :
- `-PublicKey` : Clé publique à exporter (byte[] ou hex) - **Obligatoire**
- `-Path` : Chemin du fichier de sortie - **Obligatoire**
- `-Format` : Format (Base64, Hex, ou Raw) - Par défaut : Base64
- `-Force` : Surcharger le fichier s'il existe

**Exemple** :
```powershell
$keys = New-KyberKeyPair
Export-KyberPublicKey -PublicKey $keys.PublicKey -Path "public.pem" -Format Base64
```

**Rôle** : Permet de partager uniquement la clé publique (sécurisé, la clé privée reste secrète).

---

#### 14. **ImportKyberPublicKeyCommand.cs**
**Localisation** : `KyberModule/KyberModule/ImportKyberPublicKeyCommand.cs`

**Description** : Cmdlet `Import-KyberPublicKey` pour importer une clé publique depuis un fichier.

**Paramètres** :
- `-Path` : Chemin du fichier contenant la clé publique - **Obligatoire**
- `-Format` : Format (Base64, Hex, ou Auto) - Par défaut : Auto (détection automatique)

**Formats supportés** :
- PEM (avec marqueurs BEGIN/END)
- Base64 simple
- Hexadécimal
- Binaire (Raw)

**Exemple** :
```powershell
$publicKey = Import-KyberPublicKey -Path "server_public.pem"
$encapsulated = Invoke-KyberEncapsulate -PublicKey $publicKey
```

**Rôle** : Permet de charger la clé publique d'un serveur ou d'un pair pour établir une communication sécurisée.

---

#### 15. **ExportImportExamples.ps1**
**Localisation** : `KyberModule/KyberModule/ExportImportExamples.ps1`

**Description** : Script PowerShell contenant des exemples d'exportation et d'importation de clés Kyber.

**Contenu** :
- Exemple 1 : Export/Import en format Text (Key=Value)
- Exemple 2 : Export/Import en format Base64/PEM
- Exemple 3 : Export uniquement de la clé publique
- Exemple 4 : Workflow complet pour un protocole type SSH

**Rôle** : Exemples pratiques pour l'utilisation des fonctionnalités d'export/import Kyber.

---

#### 9. **Test.ps1**
**Localisation** : `KyberModule/KyberModule/Test.ps1`

**Description** : Script de tests unitaires pour valider le fonctionnement du module.

**Tests effectués** :
- Génération de clés
- Encapsulation
- Décapsulation
- Vérification de correspondance des clés partagées

**Rôle** : Validation que le module fonctionne correctement.

---

#### 20. **Ed25519Examples.ps1**
**Localisation** : `KyberModule/KyberModule/Ed25519Examples.ps1`

**Description** : Script PowerShell contenant des exemples d'utilisation d'Ed25519.

**Contenu** :
- Exemple 1 : Génération de clés Ed25519
- Exemple 2 : Signature et vérification
- Exemple 3 : Test avec données modifiées
- Exemple 4 : Export/Import de clés Ed25519
- Exemple 5 : Utilisation combinée Kyber + Ed25519

**Rôle** : Exemples pratiques pour l'utilisation d'Ed25519.

---

#### 21. **TestKyberEd25519.ps1**
**Localisation** : `KyberModule/KyberModule/TestKyberEd25519.ps1`

**Description** : Script de test complet pour la combinaison Kyber + Ed25519.

**Contenu** :
- Génération de clés Kyber et Ed25519
- Encapsulation d'une clé partagée avec Kyber
- Signature de la clé partagée avec Ed25519
- Vérification de la signature
- Test avec données modifiées (sécurité)
- Décapsulation et vérification
- Export/Import de clés et utilisation avec clés rechargées

**Rôle** : Démonstration complète d'un protocole sécurisé combinant les deux algorithmes. ⭐ **Recommandé**

---

#### 22. **QuickTestEd25519.ps1**
**Localisation** : `KyberModule/KyberModule/QuickTestEd25519.ps1`

**Description** : Script de test rapide pour vérifier le fonctionnement d'Ed25519.

**Rôle** : Validation rapide des fonctionnalités Ed25519.

---

#### 23. **KyberEd25519CompleteExample.ps1**
**Localisation** : `KyberModule/KyberModule/KyberEd25519CompleteExample.ps1`

**Description** : Exemple complet simulant un scénario serveur/client avec Kyber + Ed25519.

**Rôle** : Démonstration d'un protocole complet avec export/import de clés.

---

#### 24. **DilithiumExamples.ps1**
**Localisation** : `KyberModule/KyberModule/DilithiumExamples.ps1`

**Description** : Script PowerShell contenant des exemples d'utilisation de Dilithium (ML-DSA).

**Contenu** :
- Exemple 1 : Génération de clés Dilithium
- Exemple 2 : Signature et vérification
- Exemple 3 : Test avec données modifiées
- Exemple 4 : Export/Import de clés Dilithium
- Exemple 5 : Différents paramètres de sécurité (Dilithium2, Dilithium3, Dilithium5)

**Rôle** : Exemples pratiques pour l'utilisation de Dilithium (100% post-quantique).

---

#### 25. **TestKyberDilithium.ps1**
**Localisation** : `KyberModule/KyberModule/TestKyberDilithium.ps1`

**Description** : Script de test complet pour la combinaison Kyber + Dilithium (100% post-quantique).

**Contenu** :
- Génération de clés Kyber et Dilithium
- Encapsulation d'une clé partagée avec Kyber
- Signature de la clé partagée avec Dilithium
- Vérification de la signature
- Décapsulation et vérification

**Rôle** : Démonstration complète d'un protocole sécurisé 100% post-quantique. ⭐ **Recommandé pour sécurité post-quantique**

---

#### 26. **KyberDilithiumCompleteExample.ps1**
**Localisation** : `KyberModule/KyberModule/KyberDilithiumCompleteExample.ps1`

**Description** : Exemple complet simulant un scénario serveur/client avec Kyber + Dilithium (100% post-quantique).

**Rôle** : Démonstration d'un protocole complet post-quantique avec export/import de clés.

---

#### 27. **TLS13HybridExamples.ps1**
**Localisation** : `KyberModule/KyberModule/TLS13HybridExamples.ps1`

**Description** : Script PowerShell contenant des exemples d'utilisation de TLS 1.3 post-quantique hybride.

**Contenu** :
- Exemple 1 : Génération de configuration hybride par défaut (ECDSA-P256 + Kyber768 + Dilithium3)
- Exemple 2 : Configuration avec RSA-2048
- Exemple 3 : Configuration haute sécurité (ECDSA-P384 + Kyber1024 + Dilithium5)
- Exemple 4 : Export de configuration pour intégration TLS
- Exemple 5 : Comparaison de différentes configurations hybrides

**Rôle** : Exemples pratiques pour créer des configurations TLS 1.3 hybrides combinant algorithmes classiques et post-quantiques.

---

#### 28. **PerformanceBenchmark.ps1**
**Localisation** : `KyberModule/KyberModule/PerformanceBenchmark.ps1`

**Description** : Script PowerShell de benchmark de performance haute précision pour tous les algorithmes cryptographiques du module.

**Contenu** :
- Benchmark Kyber (ML-KEM) : Génération, encapsulation, décapsulation pour Kyber512/768/1024
- Benchmark Dilithium (ML-DSA) : Génération, signature, vérification pour Dilithium2/3/5
- Benchmark Ed25519 : Génération, signature, vérification (1000 itérations pour précision)
- Benchmark SHA3 : Hachage SHA3-256 et SHA3-384 (1000 itérations pour précision)
- Benchmark Chiffrement AEAD : ChaCha20-Poly1305 et AES-GCM (chiffrement/déchiffrement, 500 itérations pour précision)
- Métriques détaillées : Moyenne, médiane, minimum, maximum, écart-type en millisecondes et microsecondes
- Warm-up automatique : Exécution préalable pour éviter les effets de cache/JIT
- Gestion d'erreurs robuste : Retourne toujours des valeurs exploitables même en cas d'erreur
- Export CSV : Toutes les données au format CSV pour analyse approfondie

**Caractéristiques** :
- **Haute précision** : Mesures en microsecondes (µs) et millisecondes (ms)
- **Statistiques avancées** : Moyenne, médiane, écart-type pour analyse complète
- **Itérations adaptatives** : 10 itérations pour opérations lentes (Kyber/Dilithium), 500-1000 pour opérations rapides (Ed25519/SHA3/AEAD)
- **Précision** : 4 décimales pour ms, 2 décimales pour µs
- **Export exploitable** : Format CSV avec toutes les métriques pour analyse externe

**Rôle** : Mesure et comparaison précise des performances de tous les algorithmes pour aider au choix des paramètres de sécurité et à l'optimisation.

---

#### 10. **DebugFields.ps1**
**Localisation** : `KyberModule/KyberModule/DebugFields.ps1`

**Description** : Script de débogage pour inspecter la structure interne de BouncyCastle.

**Rôle** : Aide au développement et au dépannage.

---

#### 26. **TLS13HybridWrapper.cs**
**Localisation** : `KyberModule/KyberLibrary/TLS13HybridWrapper.cs`

**Description** : Classe wrapper pour le support TLS 1.3 post-quantique hybride combinant algorithmes classiques et post-quantiques.

**Méthodes principales** :

| Méthode | Description |
|---------|-------------|
| `GenerateHybridConfig()` | Génère une configuration TLS 1.3 hybride complète |
| `ExportConfigToText()` | Exporte une configuration en format texte |
| `GetCipherSuiteName()` | Obtient le nom du cipher suite TLS hybride |
| `ValidateConfig()` | Valide une configuration TLS hybride |
**Algorithmes supportés** :
- **Classiques** : ECDSA_P256, ECDSA_P384, RSA_2048, RSA_3072
- **Post-quantiques KEM** : Kyber512, Kyber768, Kyber1024 (ML-KEM)
- **Post-quantiques Signature** : Dilithium2, Dilithium3, Dilithium5 (ML-DSA)

**Rôle** : Permet la création de configurations TLS hybrides pour une transition progressive vers la cryptographie post-quantique.

---

#### 29. **SecureKeyManager.cs**
**Localisation** : `KyberModule/KyberLibrary/SecureKeyManager.cs`

**Description** : Classe utilitaire statique pour la gestion sécurisée des clés en mémoire (zeroization).

**Méthodes principales** :
- `Zeroize(byte[])` : Nettoie un tableau de bytes avec Array.Clear()
- `ZeroizeMultiplePasses(byte[], int)` : Nettoie avec plusieurs passes pour sécurité renforcée
- `IsZeroized(byte[])` : Vérifie si un tableau est complètement nettoyé
- `SecureCopy(byte[])` : Crée une copie sécurisée d'un tableau

**Rôle** : Fournit des utilitaires pour le nettoyage sécurisé de la mémoire.

---

#### 30. **SecureKeyWrapper.cs**
**Localisation** : `KyberModule/KyberLibrary/SecureKeyWrapper.cs`

**Description** : Wrapper sécurisé pour stocker des clés cryptographiques en mémoire avec nettoyage automatique.

**Classes** :
- `SecureKeyWrapper` : Wrapper pour une seule clé (IDisposable)
- `SecureKeyPairWrapper` : Wrapper pour une paire de clés (publique + privée)

**Caractéristiques** :
- Implémente `IDisposable` pour nettoyage automatique
- Thread-safe avec verrous
- Finalizer pour nettoyage même si Dispose() n'est pas appelé
- Nettoyage multi-passes disponible

**Rôle** : Gestion automatique du nettoyage des clés en mémoire.

---

#### 31. **NewSecureKyberKeyPairCommand.cs**
**Localisation** : `KyberModule/KyberModule/NewSecureKyberKeyPairCommand.cs`

**Description** : Cmdlet `New-SecureKyberKeyPair` pour générer une paire de clés Kyber avec gestion sécurisée.

**Paramètres** :
- `-ParameterSet` : Niveau de sécurité (Kyber512, Kyber768, Kyber1024) - Par défaut : Kyber768
- `-ZeroizeImmediately` : Nettoyer immédiatement la clé privée (pour tests)

**Retour** : Objet `SecureKyberKeyPairResult` avec méthodes de nettoyage.

**Rôle** : Interface PowerShell pour la génération sécurisée de clés Kyber.

---

#### 32. **ClearSecureKeyCommand.cs**
**Localisation** : `KyberModule/KyberModule/ClearSecureKeyCommand.cs`

**Description** : Cmdlet `Clear-SecureKey` pour nettoyer manuellement une clé en mémoire.

**Paramètres** :
- `-Key` : Clé à nettoyer (byte[]) - **Obligatoire**
- `-Passes` : Nombre de passes de nettoyage (défaut: 1, recommandé: 3)
- `-Nullify` : Mettre la référence à null après nettoyage

**Rôle** : Permet le nettoyage manuel des clés pour un contrôle immédiat.

---

#### 33. **TestZeroizedKeyCommand.cs**
**Localisation** : `KyberModule/KyberModule/TestZeroizedKeyCommand.cs`

**Description** : Cmdlet `Test-ZeroizedKey` pour vérifier si une clé a été nettoyée.

**Paramètres** :
- `-Key` : Clé à vérifier (byte[]) - **Obligatoire**

**Retour** : `bool` indiquant si la clé est nettoyée (contient uniquement des zéros).

**Rôle** : Validation du nettoyage des clés.

---

#### 34. **ConstantTimeOperations.cs**
**Localisation** : `KyberModule/KyberLibrary/ConstantTimeOperations.cs`

**Description** : Classe utilitaire statique pour les opérations à temps constant (protection contre timing attacks).

**Méthodes principales** :
- `ConstantTimeEquals(byte[], byte[])` : Compare deux tableaux en temps constant
- `ConstantTimeSelect(bool, T, T)` : Sélection conditionnelle sans branche
- `ConstantTimeCopy(bool, byte[], byte[])` : Copie conditionnelle
- `ConstantTimeIsZero(byte)` : Vérifie si un byte est zéro
- `ConstantTimeMask(bool, byte)` : Masque conditionnel

**Rôle** : Fournit des opérations cryptographiques à temps constant pour protéger contre les timing attacks.

---

#### 35. **SideChannelProtection.cs**
**Localisation** : `KyberModule/KyberLibrary/SideChannelProtection.cs`

**Description** : Classe utilitaire statique pour la protection contre les attaques par canaux auxiliaires.

**Méthodes principales** :
- `FlushCache()` : Nettoie le cache du processeur
- `SecureCompare(byte[], byte[])` : Compare avec nettoyage du cache
- `ProtectTiming<T>(Func<T>)` : Protège une opération contre les timing attacks
- `UniformMemoryAccess(byte[], int)` : Accès mémoire uniforme
- `SecureCopy(byte[], byte[], int)` : Copie sécurisée

**Rôle** : Fournit des protections contre les attaques par timing et cache side-channel.

---

#### 36. **InvokeKyberDecapsulateSecureCommand.cs**
**Localisation** : `KyberModule/KyberModule/InvokeKyberDecapsulateSecureCommand.cs`

**Description** : Cmdlet `Invoke-KyberDecapsulateSecure` pour décapsuler avec protection side-channel.

**Paramètres** :
- `-Ciphertext` : Ciphertext (byte[] ou hex) - **Obligatoire**
- `-PrivateKey` : Clé privée (byte[] ou hex) - **Obligatoire**
- `-ParameterSet` : Paramètre de sécurité (Kyber512, Kyber768, Kyber1024)

**Rôle** : Interface PowerShell pour la décapsulation sécurisée.

---

#### 37. **TestDilithiumSignatureSecureCommand.cs**
**Localisation** : `KyberModule/KyberModule/TestDilithiumSignatureSecureCommand.cs`

**Description** : Cmdlet `Test-DilithiumSignatureSecure` pour vérifier une signature avec protection side-channel.

**Paramètres** :
- `-Data` : Données originales (byte[] ou hex) - **Obligatoire**
- `-Signature` : Signature à vérifier (byte[] ou hex) - **Obligatoire**
- `-PublicKey` : Clé publique (byte[] ou hex) - **Obligatoire**
- `-ParameterSet` : Paramètre de sécurité (Dilithium2, Dilithium3, Dilithium5)
- `-PreHash` : Pré-hacher avec SHA3 (optionnel)

**Rôle** : Interface PowerShell pour la vérification sécurisée de signatures.

---
#### 38. **TestConstantTimeCompareCommand.cs**
**Localisation** : `KyberModule/KyberModule/TestConstantTimeCompareCommand.cs`
**Description** : Cmdlet `Test-ConstantTimeCompare` pour comparer deux tableaux en temps constant.

**Paramètres** :
- `-Array1` : Premier tableau (byte[] ou hex) - **Obligatoire**
- `-Array2` : Deuxième tableau (byte[] ou hex) - **Obligatoire**

**Retour** : `bool` indiquant si les tableaux sont identiques.

**Rôle** : Interface PowerShell pour les comparaisons en temps constant.

---

#### 39. **CryptographicConformanceTests.cs**
**Localisation** : `KyberModule/KyberLibrary/CryptographicConformanceTests.cs`

**Description** : Classe utilitaire statique pour les tests de conformité cryptographique selon les standards NIST.

**Méthodes principales** :
- `ValidateKyberKeySizes()` : Valide les tailles de clés Kyber selon NIST FIPS 203
- `ValidateKyberEncapsulationSizes()` : Valide les tailles de ciphertexts et clés partagées
- `ValidateDilithiumKeySizes()` : Valide les tailles de clés Dilithium selon NIST ML-DSA
- `ValidateDilithiumSignatureSize()` : Valide la taille de signature Dilithium
- `ValidateEd25519KeySizes()` : Valide les tailles de clés Ed25519 selon RFC 8032
- `ValidateNonDeterminism()` : Valide le non-déterminisme des générations de clés
- `ValidateSharedSecretMatch()` : Valide la correspondance des clés partagées
- `ValidateKeyPairCorrespondence()` : Valide la correspondance des paires de clés

**Rôle** : Fournit des tests de conformité pour valider les implémentations selon les standards NIST.

---

#### 40. **CryptographicFuzzing.cs**
**Localisation** : `KyberModule/KyberLibrary/CryptographicFuzzing.cs`

**Description** : Classe utilitaire statique pour les tests de fuzzing cryptographique.

**Méthodes principales** :
- `GenerateRandomData()` : Génère des données aléatoires
- `CorruptByte()` : Corrompt un byte dans un tableau
- `CorruptMultipleBytes()` : Corrompt plusieurs bytes
- `GenerateInvalidSizedData()` : Génère des données avec tailles invalides
- `GeneratePatternData()` : Génère des données avec patterns suspects
- `GenerateCorruptedCiphertexts()` : Génère des ciphertexts corrompus
- `GenerateCorruptedSignatures()` : Génère des signatures corrompues
- `GenerateCorruptedKeys()` : Génère des clés corrompues
- `TestRobustness()` : Teste la robustesse d'une fonction avec données fuzzées

**Rôle** : Fournit des outils de fuzzing pour tester la robustesse des implémentations.

---

#### 41. **TestCryptographicConformanceCommand.cs**
**Localisation** : `KyberModule/KyberModule/TestCryptographicConformanceCommand.cs`

**Description** : Cmdlet `Test-CryptographicConformance` pour tester la conformité cryptographique selon les standards NIST.

**Paramètres** :
- `-Algorithm` : Algorithme à tester (Kyber, Dilithium, Ed25519, All)
- `-KyberParameterSet` : Paramètre de sécurité pour Kyber (Kyber512, Kyber768, Kyber1024)
- `-DilithiumParameterSet` : Paramètre de sécurité pour Dilithium (Dilithium2, Dilithium3, Dilithium5)
- `-ShowOnlyFailed` : Afficher uniquement les tests échoués

**Retour** : Liste de `ConformanceTestResult` avec les résultats des tests.

**Rôle** : Interface PowerShell pour les tests de conformité NIST.

---

#### 42. **InvokeCryptographicFuzzingCommand.cs**
**Localisation** : `KyberModule/KyberModule/InvokeCryptographicFuzzingCommand.cs`

**Description** : Cmdlet `Invoke-CryptographicFuzzing` pour exécuter des tests de fuzzing cryptographique.

**Paramètres** :
- `-Algorithm` : Algorithme à tester (Kyber, Dilithium, Ed25519, All)
- `-KyberParameterSet` : Paramètre de sécurité pour Kyber
- `-DilithiumParameterSet` : Paramètre de sécurité pour Dilithium
- `-Iterations` : Nombre d'itérations de fuzzing par test
- `-ShowOnlyIssues` : Afficher uniquement les tests avec crashes ou échecs

**Retour** : Liste de `FuzzingTestResult` avec les résultats des tests de fuzzing.

**Rôle** : Interface PowerShell pour les tests de fuzzing.

---

## 📦 Installation

### Prérequis
- .NET SDK 8.0 (pour compiler le service et le client CLI)
- PowerShell 5.1 ou PowerShell 7+
- Windows, Linux ou macOS (PowerShell Core)

> 📖 **Guide de compilation complet** : Voir [`COMPILATION.md`](COMPILATION.md) pour les instructions détaillées de compilation depuis zéro après avoir cloné le dépôt GitHub.

### 1. Module PowerShell
```powershell
cd KyberModule\KyberModule
# Compiler le module
 dotnet build -c Release

# Copier les DLLs utiles dans le dossier du module
Copy-Item ".\bin\Release\netstandard2.0\KyberModule.dll" -Destination "." -Force
Copy-Item "..\KyberLibrary\bin\Release\netstandard2.0\KyberLibrary.dll" -Destination "." -Force

# Copier BouncyCastle.Cryptography 2.6.2
$bcDll = Get-ChildItem "$env:USERPROFILE\.nuget\packages\bouncycastle.cryptography\2.6.2\lib\netstandard2.0\BouncyCastle.Cryptography.dll"
Copy-Item $bcDll.FullName -Destination ".\BouncyCastle.Cryptography.dll" -Force

# Import du module
Import-Module "C:\Program Files\KyberModule\KyberModule\KyberModule.psd1" -Force
```

> ℹ️ Les cmdlets réseau historiques (`Start/Connect/Send/Receive-Secure*`) sont conservées mais obsolètes. Utilisez KyberDaemon/KyberCLI pour les communications.

### 2. Service KyberDaemon (optionnel)

**Linux (systemd)**
```bash
cd KyberModule/KyberDaemon
dotnet publish -c Release -r linux-x64 --self-contained false -o ./publish/linux-x64
sudo mkdir -p /opt/kyberd /etc/kyberd
sudo cp -r publish/linux-x64/* /opt/kyberd/
sudo cp config/kyberd.conf /etc/kyberd/
sudo cp config/auth.conf /etc/kyberd/
sudo cp deploy/kyberd.service /etc/systemd/system/
sudo systemctl daemon-reload
sudo systemctl enable --now kyberd
```
- `kyberd.conf` et `auth.conf` sont lus depuis `/etc/kyberd/` (chemins relatifs résolus automatiquement).

**Windows (service SCM)**
```powershell
cd KyberModule/KyberDaemon/deploy
# publie dans ../publish/win-x64 (dotnet publish -r win-x64)
./install-kyberd-service.ps1 -Runtime win-x64
Start-Service KyberDaemon
```
- Installation depuis la console **Administrateur** requise.
- La configuration est copiée dans `C:\ProgramData\KyberDaemon\`. Modifier `kyberd.conf` / `auth.conf` puis `Restart-Service KyberDaemon`.

### 3. Client KyberCLI
```powershell
cd KyberModule/KyberCLI
dotnet publish -c Release -o .\bin\Release\net8.0\publish
```
Le binaire (Windows : `KyberCLI.exe`, Linux/macOS : `KyberCLI` ou `dotnet KyberCLI.dll`) peut être copié avec KyberDaemon ou utilisé directement depuis PowerShell via `Invoke-SecureCommand`.

### 4. Packages prêts à l'emploi

Les scripts suivants génèrent des archives contenant : KyberDaemon (dossier `publish/<runtime>` + `config` + `deploy`), KyberCLI (`publish/<runtime>`) et le module PowerShell.

- Script de build global (PowerShell) : `scripts/build-all.ps1 [-Configuration Release|Debug]`
  - Compile séquentiellement KyberDomain, KyberLibrary, KyberShared, KyberDaemon et KyberCLI
  - Arrête l'exécution dès la première erreur et affiche un récapitulatif des chemins de sortie

#### Windows (PowerShell)
```powershell
cd KyberModule/deploy
./build-windows-package.ps1 -OutputDirectory dist/windows -Runtime win-x64
```
Archive produite : `dist/windows/KyberModule-win-x64.zip`

#### Linux (bash)
```bash
cd KyberModule/deploy
./build-linux-package.sh dist/linux linux-x64
```
Archive produite : `dist/linux/KyberModule-linux-x64.tar.gz`

Dans l'archive :
- `KyberDaemon/publish/<runtime>/` : binaire + dépendances (`KyberDaemon.exe`, `KyberLibrary.dll`, `KyberShared.dll`, …)
- `KyberDaemon/deploy/install-kyberd-service.ps1` : installe le service Windows (`publish/<runtime>` utilisé par défaut).
- `KyberCLI/publish/<runtime>/KyberCLI.exe` : client CLI + dépendances
- `KyberModule/` : module PowerShell (KyberModule.dll, KyberLibrary.dll, BouncyCastle.Cryptography.dll)

---

## 📚 Scripts d'exemples

Le module inclut plusieurs scripts d'exemples pour vous aider à démarrer :

| Script | Description |
|--------|-------------|
| `Examples.ps1` | Exemples de base pour Kyber (génération, encapsulation, décapsulation) |
| `ExportImportExamples.ps1` | Exemples d'export/import de clés Kyber |
| `Ed25519Examples.ps1` | Exemples complets pour Ed25519 (génération, signature, vérification) |
| `DilithiumExamples.ps1` | Exemples complets pour Dilithium (génération, signature, vérification) |
| `TestKyberEd25519.ps1` | **Test combiné Kyber + Ed25519** avec export/import |
| `TestKyberDilithium.ps1` | **Test combiné Kyber + Dilithium** (100% post-quantique) |
| `SHA3Examples.ps1` | Exemples d'utilisation de SHA3-256/384 avec ML-DSA |
| `AuthenticatedEncryptionExamples.ps1` | Exemples de chiffrement ChaCha20-Poly1305 et AES-GCM |
| `PKCSX509Examples.ps1` | Exemples d'utilisation de PKCS#8 et X.509 |
| `TLS13HybridExamples.ps1` | Exemples de configuration TLS 1.3 post-quantique hybride |
| `PerformanceBenchmark.ps1` | Script de benchmark de performance pour tous les algorithmes |
| `SecureKeyManagementExamples.ps1` | Exemples de gestion sécurisée des clés en mémoire |
| `SideChannelProtectionTests.ps1` | Tests de protection contre les canaux auxiliaires |
| `QuickTestEd25519.ps1` | Test rapide pour vérifier Ed25519 |
| `Test.ps1` | Tests unitaires pour valider le fonctionnement du module |
| `CONTRE_MESURES_SIDE_CHANNEL.md` | Documentation complète des contre-mesures side-channel |

**Pour exécuter les exemples** :
```powershell
cd KyberModule\KyberModule
.\TestKyberEd25519.ps1  # Test combiné recommandé
```

---

## 📖 Guide d'utilisation

### Scénario 1 : Génération de clés

```powershell
# Générer une paire de clés avec Kyber768 (par défaut)
$keys = New-KyberKeyPair

# Spécifier le niveau de sécurité
$keys512 = New-KyberKeyPair -ParameterSet Kyber512
$keys768 = New-KyberKeyPair -ParameterSet Kyber768
$keys1024 = New-KyberKeyPair -ParameterSet Kyber1024

# Accéder aux clés
Write-Host "Clé publique (hex): $($keys.PublicKeyHex)"
Write-Host "Clé privée (hex): $($keys.PrivateKeyHex)"
```

### Scénario 2 : Échange de clés (Alice et Bob)

```powershell
# Alice génère sa paire de clés
$aliceKeys = New-KyberKeyPair -ParameterSet Kyber768

# Bob utilise la clé publique d'Alice pour créer une clé partagée
$bobEncapsulated = Invoke-KyberEncapsulate -PublicKey $aliceKeys.PublicKey

# Bob envoie le ciphertext à Alice
# $ciphertext = $bobEncapsulated.Ciphertext

# Alice décapsule pour obtenir la même clé partagée
$aliceSharedSecret = Invoke-KyberDecapsulate -Ciphertext $bobEncapsulated.Ciphertext -PrivateKey $aliceKeys.PrivateKey

# Vérifier que les clés correspondent
$match = $bobEncapsulated.SharedSecret -eq $aliceSharedSecret.SharedSecret
```

### Scénario 3 : Utilisation avec des chaînes hexadécimales

```powershell
# Générer des clés
$keys = New-KyberKeyPair

# Utiliser directement les chaînes hex
$encapsulated = Invoke-KyberEncapsulate -PublicKey $keys.PublicKeyHex

# Décapsuler avec les chaînes hex
$sharedSecret = Invoke-KyberDecapsulate -Ciphertext $encapsulated.CiphertextHex -PrivateKey $keys.PrivateKeyHex
```

### Scénario 4 : Sauvegarder et charger des clés

#### Format Text (recommandé pour sauvegarder les paires de clés - sécurisé)
```powershell
# Générer et sauvegarder
$keys = New-KyberKeyPair
Export-KyberKeyPair -KeyPair $keys -Path "keys.txt" -Format Text

# Charger plus tard
$keys = Import-KyberKeyPair -Path "keys.txt"

# Utiliser
$encapsulated = Invoke-KyberEncapsulate -PublicKey $keys.PublicKey
$sharedSecret = Invoke-KyberDecapsulate -Ciphertext $encapsulated.Ciphertext -PrivateKey $keys.PrivateKey
```

#### Format Base64/PEM (compatible avec SSH et autres protocoles)
```powershell
# Exporter en format PEM
$keys = New-KyberKeyPair
Export-KyberKeyPair -KeyPair $keys -Path "keys.pem" -Format Base64

# Exporter uniquement la clé publique (pour la partager)
Export-KyberPublicKey -PublicKey $keys.PublicKey -Path "public_key.pem" -Format Base64

# Importer
$keys = Import-KyberKeyPair -Path "keys.pem"
$publicKey = Import-KyberPublicKey -Path "public_key.pem"
```

### Scénario 5 : Protocole type SSH (serveur/client)

```powershell
# === Côté SERVEUR ===
# 1. Générer les clés du serveur
$serverKeys = New-KyberKeyPair -ParameterSet Kyber768

# 2. Sauvegarder la clé privée (sécurisée)
Export-KyberKeyPair -KeyPair $serverKeys -Path "server_private.txt" -Format Text

# 3. Partager la clé publique avec les clients
Export-KyberPublicKey -PublicKey $serverKeys.PublicKey -Path "server_public.pem" -Format Base64

# 4. Quand un client se connecte, décapsuler la clé partagée
$serverKeys = Import-KyberKeyPair -Path "server_private.txt"
$sharedSecret = Invoke-KyberDecapsulate -Ciphertext $clientCiphertext -PrivateKey $serverKeys.PrivateKey

# === Côté CLIENT ===
# 1. Récupérer la clé publique du serveur
$serverPublicKey = Import-KyberPublicKey -Path "server_public.pem"

# 2. Encapsuler une clé partagée
$encapsulated = Invoke-KyberEncapsulate -PublicKey $serverPublicKey -ParameterSet Kyber768

# 3. Envoyer le ciphertext au serveur
# $encapsulated.Ciphertext

# 4. Utiliser la clé partagée pour la communication
# $encapsulated.SharedSecret
```

### Scénario 6 : Signature numérique avec Dilithium (100% Post-Quantum) ⭐

```powershell
# Générer une paire de clés Dilithium (ML-DSA)
$signingKeys = New-DilithiumKeyPair -ParameterSet Dilithium3

# Signer un message
$message = "Important message"
$messageBytes = [System.Text.Encoding]::UTF8.GetBytes($message)
$signature = Invoke-DilithiumSign -Data $messageBytes -PrivateKey $signingKeys.PrivateKey -ParameterSet Dilithium3

# Vérifier la signature
$isValid = Test-DilithiumSignature -Data $messageBytes -Signature $signature.Signature -PublicKey $signingKeys.PublicKey -ParameterSet Dilithium3

# Sauvegarder les clés
Export-DilithiumKeyPair -KeyPair $signingKeys -Path "signing_keys.txt" -Format Text
```

### Scénario 7 : Signature numérique avec Ed25519 (Compatibilité)

```powershell
# Générer une paire de clés Ed25519
$signingKeys = New-Ed25519KeyPair

# Signer un message
$message = "Important message"
$signature = Invoke-Ed25519Sign -Data $message -PrivateKey $signingKeys.PrivateKey

# Vérifier la signature
$isValid = Test-Ed25519Signature -Data $message -Signature $signature.Signature -PublicKey $signingKeys.PublicKey

# Sauvegarder les clés
Export-Ed25519KeyPair -KeyPair $signingKeys -Path "signing_keys.txt" -Format Text
```

### Scénario 8 : Protocole sécurisé 100% Post-Quantum : Kyber + Dilithium ⭐

Ce scénario combine les deux algorithmes post-quantiques pour créer un protocole sécurisé 100% résistant aux ordinateurs quantiques :
- **Kyber (ML-KEM)** : Pour l'échange de clés résistant aux ordinateurs quantiques
- **Dilithium (ML-DSA)** : Pour l'authentification et l'intégrité des données (post-quantique)

```powershell
# 1. Générer les clés post-quantiques
$kyberKeys = New-KyberKeyPair -ParameterSet Kyber768
$dilithiumKeys = New-DilithiumKeyPair -ParameterSet Dilithium3

# 2. Encapsuler une clé partagée avec Kyber
$encapsulated = Invoke-KyberEncapsulate -PublicKey $kyberKeys.PublicKey -ParameterSet Kyber768

# 3. Signer la clé partagée avec Dilithium
$signature = Invoke-DilithiumSign -Data $encapsulated.SharedSecret -PrivateKey $dilithiumKeys.PrivateKey -ParameterSet Dilithium3

# 4. Vérifier la signature
$isValid = Test-DilithiumSignature -Data $encapsulated.SharedSecret -Signature $signature.Signature -PublicKey $dilithiumKeys.PublicKey -ParameterSet Dilithium3

# 5. Décapsuler la clé partagée
$sharedSecret = Invoke-KyberDecapsulate -Ciphertext $encapsulated.Ciphertext -PrivateKey $kyberKeys.PrivateKey -ParameterSet Kyber768
```

**Avantages de cette combinaison** :
- 🔐 **Kyber (ML-KEM)** : Échange de clés résistant aux ordinateurs quantiques
- ✍️ **Dilithium (ML-DSA)** : Authentification et intégrité post-quantiques
- 🔑 **Clé partagée** : 32 bytes sécurisée et authentifiée
- 🛡️ **100% Post-Quantum** : Résistant aux attaques classiques et quantiques

**Scripts d'exemples** :
- `TestKyberDilithium.ps1` : Test rapide de l'intégration
- `KyberDilithiumCompleteExample.ps1` : Scénario serveur/client complet

### Scénario 9 : Hachage SHA3 avec ML-DSA

```powershell
# Calculer un hachage SHA3-256
$data = "Données à hacher"
$hash256 = Get-SHA3Hash -Data $data -Variant SHA3_256

# Calculer un hachage SHA3-384
$hash384 = Get-SHA3Hash -Data $data -Variant SHA3_384

# Signer avec Dilithium en utilisant SHA3 pré-hachage (recommandé)
$keys = New-DilithiumKeyPair -ParameterSet Dilithium3
$signature = Invoke-DilithiumSign -Data $dataBytes -PrivateKey $keys.PrivateKey -PreHash -SHA3Variant SHA3_256
```

### Scénario 10 : Chiffrement Authenticated Encryption

#### ChaCha20-Poly1305
```powershell
# Chiffrer avec ChaCha20-Poly1305
$plaintext = "Message secret"
$key = New-Object byte[] 32
# Générer une clé (ou utiliser une clé partagée Kyber)
$encrypted = Protect-WithChaCha20Poly1305 -Data $plaintext -Key $key

# Déchiffrer
$decrypted = Unprotect-ChaCha20Poly1305 -EncryptedData $encrypted.EncryptedData -Key $key -Nonce $encrypted.Nonce -Tag $encrypted.Tag
```

#### AES-GCM
```powershell
# Chiffrer avec AES-GCM
$plaintext = "Message secret"
$key = New-Object byte[] 32
$encrypted = Protect-WithAESGCM -Data $plaintext -Key $key

# Déchiffrer
$decrypted = Unprotect-AESGCM -EncryptedData $encrypted.EncryptedData -Key $key -Nonce $encrypted.Nonce -Tag $encrypted.Tag
```

### Scénario 11 : Export/Import PKCS#8 et Certificats X.509

```powershell
# Exporter une clé privée Dilithium en format PKCS#8
$keys = New-DilithiumKeyPair -ParameterSet Dilithium3
Export-PrivateKeyPKCS8 -PrivateKey $keys.PrivateKey -Path "dilithium_key.pem" -Format PEM -KeyType Dilithium -ParameterSet Dilithium3

# Importer une clé privée PKCS#8
$imported = Import-PrivateKeyPKCS8 -Path "dilithium_key.pem" -KeyType Dilithium

# Créer un certificat X.509 auto-signé avec Dilithium
$cert = New-X509Certificate -SubjectName "CN=Test Post-Quantum Certificate" -PrivateKey $keys.PrivateKey -ParameterSet Dilithium3 -Path "certificate.pem" -Format PEM

# Vérifier un certificat X.509
$isValid = Test-X509Certificate -Path "certificate.pem"
```

### Scénario 12 : Configuration TLS 1.3 Post-Quantum Hybride ⭐

Ce scénario permet de créer des configurations TLS 1.3 hybrides combinant des algorithmes classiques (ECDSA, RSA) avec des algorithmes post-quantiques (Kyber, Dilithium) pour une transition progressive vers la cryptographie post-quantique.

```powershell
# Générer une configuration TLS hybride par défaut (ECDSA-P256 + Kyber768 + Dilithium3)
$hybridConfig = New-TLS13HybridConfig

# Obtenir le nom du cipher suite
$cipherSuite = Get-TLS13HybridCipherSuite -Config $hybridConfig
Write-Host "Cipher Suite: $cipherSuite"  # TLS13-ECDSA-P256+ML-KEM-768+ML-DSA-65

# Générer une configuration avec RSA
$rsaConfig = New-TLS13HybridConfig -ClassicalAlgorithm RSA_2048 -PostQuantumKemAlgorithm Kyber768 -PostQuantumSigAlgorithm Dilithium3

# Générer une configuration haute sécurité
$highSecConfig = New-TLS13HybridConfig -ClassicalAlgorithm ECDSA_P384 -PostQuantumKemAlgorithm Kyber1024 -PostQuantumSigAlgorithm Dilithium5

# Exporter la configuration pour intégration TLS
Export-TLS13HybridConfig -Config $hybridConfig -Path "tls13_hybrid_config.txt" -Force
```

**Algorithmes classiques supportés** :
- `ECDSA_P256` : ECDSA avec courbe P-256 (NIST)
- `ECDSA_P384` : ECDSA avec courbe P-384 (NIST)
- `RSA_2048` : RSA 2048 bits
- `RSA_3072` : RSA 3072 bits

**Algorithmes post-quantiques supportés** :
- **KEM** : Kyber512, Kyber768, Kyber1024 (ML-KEM)
- **Signature** : Dilithium2, Dilithium3, Dilithium5 (ML-DSA)

**Avantages de l'hybridation** :
- 🔐 Sécurité classique éprouvée (compatibilité avec infrastructure existante)
- 🛡️ Sécurité post-quantique (résistance aux ordinateurs quantiques)
- ✅ Transition progressive vers la cryptographie post-quantique
- 🔄 Compatibilité avec bibliothèques TLS supportant les extensions post-quantiques

**Exemple complet** : Voir `TLS13HybridExamples.ps1` pour des scénarios complets.

### Scénario 13 : Benchmark de Performance Haute Précision

Le script `PerformanceBenchmark.ps1` permet de mesurer avec haute précision les performances de tous les algorithmes cryptographiques du module :

```powershell
# Exécuter le benchmark complet
.\PerformanceBenchmark.ps1
```
Le script mesure :
- ✅ Temps de génération de clés (Kyber, Dilithium, Ed25519)
- ✅ Temps d'encapsulation/décapsulation (Kyber)
- ✅ Temps de signature/vérification (Dilithium, Ed25519)
- ✅ Temps de hachage (SHA3-256, SHA3-384)
- ✅ Temps de chiffrement/déchiffrement (ChaCha20-Poly1305, AES-GCM)

**Métriques affichées** :
- **Moyenne** : Temps moyen en µs et ms (4 décimales pour ms, 2 pour µs)
- **Médiane** : Temps médian (moins sensible aux valeurs aberrantes)
- **Minimum et Maximum** : Plage de variation des temps
- **Écart-type** : Mesure de la variabilité des performances
- **Statut** : SUCCESS (toutes réussies), PARTIAL (certaines échouées), FAILED (toutes échouées)
- **Itérations** : Nombre d'itérations réussies vs total

**Caractéristiques de précision** :
- **Opérations lentes** (Kyber/Dilithium) : 10 itérations
- **Opérations rapides** (Ed25519/SHA3/AEAD) : 500-1000 itérations pour précision
- **Warm-up automatique** : Exécution préalable pour éviter les effets de cache/JIT
- **Mesures en microsecondes** : Précision maximale pour opérations rapides
- **Export CSV** : Toutes les données exportables pour analyse approfondie

**Exemple de sortie** :
```
✅ Génération de clés Ed25519:
   Moyenne: 125.50 µs (0.1255 ms)
   Médiane: 124.20 µs (0.1242 ms)
   Min: 120.10 µs (0.1201 ms) | Max: 135.30 µs (0.1353 ms)
   Écart-type: 3.45 µs (0.0035 ms)
   Itérations: 1000/1000 réussies
```

**Export CSV** :
Le script génère également un export CSV avec toutes les métriques :
- Operation, Status, Average_ms, Average_us, Median_ms, Median_us, Min_ms, Min_us, Max_ms, Max_us, StdDev_ms, StdDev_us, SuccessfulIterations, TotalIterations, Errors

### Scénario 14 : Gestion Sécurisée des Clés en Mémoire ⭐

Ce scénario démontre l'utilisation de la gestion sécurisée des clés pour protéger les clés privées en mémoire.

```powershell
# Générer une paire de clés avec gestion sécurisée
$secureKeys = New-SecureKyberKeyPair -ParameterSet Kyber768

# Utiliser les clés
$publicKey = $secureKeys.PublicKey
$privateKey = $secureKeys.PrivateKey

# Encapsuler et décapsuler
$encapsulated = Invoke-KyberEncapsulate -PublicKey $publicKey -ParameterSet Kyber768
$decapsulated = Invoke-KyberDecapsulate -Ciphertext $encapsulated.Ciphertext -PrivateKey $privateKey -ParameterSet Kyber768

# Nettoyer immédiatement la clé privée après utilisation
$secureKeys.ZeroizePrivateKey()

# Vérifier que la clé est nettoyée
$isZeroized = Test-ZeroizedKey -Key $privateKey
Write-Host "Clé nettoyée: $isZeroized"  # True
```

**Nettoyage manuel avec Clear-SecureKey** :
```powershell
# Générer des clés normales
$keys = New-KyberKeyPair -ParameterSet Kyber768

# Utiliser les clés...

# Nettoyer manuellement avec une seule passe
Clear-SecureKey -Key $keys.PrivateKey -Passes 1

# Ou avec plusieurs passes pour sécurité renforcée (protection cold boot)
Clear-SecureKey -Key $keys.PrivateKey -Passes 3 -Nullify

# Vérifier le nettoyage
$isClean = Test-ZeroizedKey -Key $keys.PrivateKey
```

**Pattern try-finally pour nettoyage garanti** :
```powershell
$keys = New-KyberKeyPair -ParameterSet Kyber768
try {
    # Utiliser les clés...
    $encapsulated = Invoke-KyberEncapsulate -PublicKey $keys.PublicKey
}
finally {
    # Nettoyer dans tous les cas (même en cas d'exception)
    Clear-SecureKey -Key $keys.PrivateKey -Passes 3 | Out-Null
}
```

**Avantages** :
- 🔐 Nettoyage automatique des clés privées
- 🛡️ Protection contre les attaques de récupération de mémoire
- ✅ Nettoyage multi-passes pour sécurité renforcée
- 🔄 Pattern Dispose pour nettoyage garanti

**Exemple complet** : Voir `SecureKeyManagementExamples.ps1` pour des scénarios complets.

### Scénario 15 : Protection contre les Canaux Auxiliaires (Side-Channel Attacks) ⭐

Ce scénario démontre l'utilisation des contre-mesures contre les attaques par canaux auxiliaires (timing attacks, cache side-channel attacks).

#### Vue d'ensemble

Les attaques par canaux auxiliaires exploitent des informations indirectes (temps d'exécution, accès au cache) pour extraire des secrets cryptographiques. Le module implémente des contre-mesures robustes pour protéger contre ces attaques.

#### Cmdlets disponibles

**1. Décapsulation sécurisée Kyber** :
```powershell
# Décapsulation avec protection side-channel
$keys = New-KyberKeyPair -ParameterSet Kyber768
$encapsulated = Invoke-KyberEncapsulate -PublicKey $keys.PublicKey -ParameterSet Kyber768

# Méthode normale
$decapsulatedNormal = Invoke-KyberDecapsulate -Ciphertext $encapsulated.Ciphertext -PrivateKey $keys.PrivateKey -ParameterSet Kyber768

# Méthode sécurisée (protection side-channel)
$decapsulatedSecure = Invoke-KyberDecapsulateSecure -Ciphertext $encapsulated.Ciphertext -PrivateKey $keys.PrivateKey -ParameterSet Kyber768

# Les résultats sont identiques, mais la méthode sécurisée protège contre les attaques
```

**2. Vérification de signature sécurisée Dilithium** :
```powershell
# Génération et signature
$keys = New-DilithiumKeyPair -ParameterSet Dilithium3
$message = "Message important à signer"
$messageBytes = [System.Text.Encoding]::UTF8.GetBytes($message)
$signature = Invoke-DilithiumSign -Data $messageBytes -PrivateKey $keys.PrivateKey -ParameterSet Dilithium3

# Vérification normale
$isValidNormal = Test-DilithiumSignature -Data $messageBytes -Signature $signature.Signature -PublicKey $keys.PublicKey -ParameterSet Dilithium3

# Vérification sécurisée (protection side-channel)
$isValidSecure = Test-DilithiumSignatureSecure -Data $messageBytes -Signature $signature.Signature -PublicKey $keys.PublicKey -ParameterSet Dilithium3
```

**3. Comparaison en temps constant** :
```powershell
# Comparaison de clés partagées en temps constant
$key1 = [byte[]]::new(32)
$key2 = [byte[]]::new(32)
# ... remplir les clés ...

# Comparaison sécurisée (protège contre timing attacks)
$areEqual = Test-ConstantTimeCompare -Array1 $key1 -Array2 $key2

# Supporte aussi les chaînes hexadécimales
$hex1 = "0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF"
$hex2 = "0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF"
$areEqual = Test-ConstantTimeCompare -Array1 $hex1 -Array2 $hex2
```

#### Protections implémentées

**1. Opérations à temps constant** :
- ✅ Pas de branches conditionnelles basées sur des données secrètes
- ✅ Comparaisons qui prennent toujours le même temps
- ✅ Masquage des variations de timing

**2. Protection contre timing attacks** :
- ✅ Toutes les comparaisons utilisent `ConstantTimeEquals()`
- ✅ Pas de sortie anticipée lors des comparaisons
- ✅ Temps d'exécution indépendant du contenu des données

**3. Protection contre cache side-channel attacks** :
- ✅ Nettoyage du cache après opérations sensibles
- ✅ Accès mémoire uniformes pour masquer les patterns
- ✅ Préchargement du cache pour éviter les variations

**4. Méthodes sécurisées** :
- `DecapsulateSecure()` : Décapsulation Kyber avec protection side-channel
- `VerifySecure()` : Vérification Dilithium avec protection side-channel
- `ConstantTimeCompareSharedSecrets()` : Comparaison de clés partagées en temps constant
- `ConstantTimeCompareSignatures()` : Comparaison de signatures en temps constant

#### Exemple d'intégration complète

```powershell
# Protocole complet avec protections side-channel
$kyberKeys = New-KyberKeyPair -ParameterSet Kyber768
$dilithiumKeys = New-DilithiumKeyPair -ParameterSet Dilithium3

# Encapsulation
$encapsulated = Invoke-KyberEncapsulate -PublicKey $kyberKeys.PublicKey -ParameterSet Kyber768

# Décapsulation sécurisée
$sharedSecret = Invoke-KyberDecapsulateSecure -Ciphertext $encapsulated.Ciphertext -PrivateKey $kyberKeys.PrivateKey -ParameterSet Kyber768

# Signature de la clé partagée
$signature = Invoke-DilithiumSign -Data $sharedSecret.SharedSecret -PrivateKey $dilithiumKeys.PrivateKey -ParameterSet Dilithium3

# Vérification sécurisée
$isValid = Test-DilithiumSignatureSecure -Data $sharedSecret.SharedSecret -Signature $signature.Signature -PublicKey $dilithiumKeys.PublicKey -ParameterSet Dilithium3
```

#### Tests de validation

Un script de tests complet est disponible : `SideChannelProtectionTests.ps1`

```powershell
# Exécuter tous les tests de protection side-channel
.\SideChannelProtectionTests.ps1
```

Les tests valident :
- ✅ Comparaisons en temps constant
- ✅ Décapsulation sécurisée Kyber
- ✅ Vérification sécurisée Dilithium
- ✅ Tests de timing (vérification que les opérations prennent un temps similaire)
- ✅ Détection de différences à toutes les positions
- ✅ Intégration complète avec protections side-channel
- ✅ Support de différentes tailles de données

**Exemple complet** : Voir `CONTRE_MESURES_SIDE_CHANNEL.md` pour la documentation technique complète.

### Scénario 16 : Audit et Validation Cryptographique ⭐

Ce scénario démontre l'utilisation des outils d'audit et de validation cryptographique pour vérifier la conformité aux standards NIST et la robustesse des implémentations.

#### Vue d'ensemble

Le module fournit des outils complets pour :
- **Tests de conformité** : Validation des tailles, formats et comportements selon les standards NIST (FIPS 203, ML-DSA)
- **Tests de fuzzing** : Génération de données corrompues pour tester la robustesse
- **Tests de validation cryptographique** : Vérification des propriétés cryptographiques

#### Cmdlets disponibles

**1. Tests de conformité NIST** :
```powershell
# Tester la conformité de tous les algorithmes
$results = Test-CryptographicConformance -Algorithm All

# Tester uniquement Kyber
$kyberResults = Test-CryptographicConformance -Algorithm Kyber -KyberParameterSet Kyber768

# Tester uniquement Dilithium
$dilithiumResults = Test-CryptographicConformance -Algorithm Dilithium -DilithiumParameterSet Dilithium3

# Afficher uniquement les tests échoués
$failed = Test-CryptographicConformance -Algorithm All -ShowOnlyFailed
```

**2. Tests de fuzzing** :
```powershell
# Exécuter des tests de fuzzing sur tous les algorithmes
$fuzzingResults = Invoke-CryptographicFuzzing -Algorithm All -Iterations 100

# Fuzzing Kyber uniquement
$kyberFuzzing = Invoke-CryptographicFuzzing -Algorithm Kyber -KyberParameterSet Kyber768 -Iterations 50

# Afficher uniquement les problèmes (crashes ou échecs)
$issues = Invoke-CryptographicFuzzing -Algorithm All -ShowOnlyIssues
```

**3. Script d'audit complet** :
```powershell
# Exécuter tous les tests d'audit
.\CryptographicAuditTests.ps1
```

#### Tests de conformité

Les tests de conformité valident :

**1. Tailles de clés et ciphertexts** :
- ✅ Clés publiques/privées Kyber selon NIST FIPS 203
- ✅ Ciphertexts et clés partagées Kyber
- ✅ Clés publiques/privées Dilithium selon NIST ML-DSA
- ✅ Signatures Dilithium
- ✅ Clés et signatures Ed25519 selon RFC 8032

**2. Propriétés cryptographiques** :
- ✅ Non-déterminisme des générations de clés
- ✅ Correspondance des clés partagées après encapsulation/décapsulation
- ✅ Correspondance des signatures avec les clés publiques
- ✅ Rejet correct de données invalides

**3. Formats et sérialisation** :
- ✅ Formats de clés conformes
- ✅ Reconstruction correcte des clés privées

#### Tests de fuzzing

Les tests de fuzzing génèrent et testent :

**1. Données corrompues** :
- Corruption de bytes individuels
- Corruption multiple de bytes
- Tailles invalides (trop courtes, trop longues)
- Patterns suspects (tous zéros, tous FF, etc.)

**2. Ciphertexts corrompus** :
- Test de décapsulation avec ciphertexts invalides
- Vérification du rejet correct des données corrompues

**3. Signatures corrompues** :
- Test de vérification avec signatures invalides
- Vérification du rejet correct des signatures corrompues

**4. Clés corrompues** :
- Test avec clés publiques/privées corrompues
- Vérification de la robustesse face aux clés invalides
#### Exemple d'utilisation complète
```powershell
# 1. Tests de conformité
Write-Host "=== Tests de Conformité ===" -ForegroundColor Cyan
$conformanceResults = Test-CryptographicConformance -Algorithm All

foreach ($result in $conformanceResults) {
    if ($result.Passed) {
        Write-Host "✅ $($result.TestName)" -ForegroundColor Green
    } else {
        Write-Host "❌ $($result.TestName)" -ForegroundColor Red
        foreach ($error in $result.Errors) {
            Write-Host "   - $error" -ForegroundColor Yellow
        }
    }
}

# 2. Tests de fuzzing
Write-Host "`n=== Tests de Fuzzing ===" -ForegroundColor Cyan
$fuzzingResults = Invoke-CryptographicFuzzing -Algorithm All -Iterations 50

foreach ($result in $fuzzingResults) {
    Write-Host "$($result.TestName):" -ForegroundColor Cyan
    Write-Host "  Total: $($result.TotalTests) | Passed: $($result.Passed) | Failed: $($result.Failed) | Crashed: $($result.Crashed)" -ForegroundColor Gray
    if ($result.Crashed -gt 0) {
        Write-Host "  ⚠️  $($result.Crashed) crash(es) détecté(s)" -ForegroundColor Yellow
    }
}

# 3. Script d'audit complet
.\CryptographicAuditTests.ps1
```

#### Résultats attendus

**Tests de conformité** :
- ✅ Tous les tests doivent passer
- ✅ Toutes les tailles doivent correspondre aux spécifications NIST
- ✅ Toutes les propriétés cryptographiques doivent être validées

**Tests de fuzzing** :
- ✅ Les données corrompues doivent être rejetées correctement
- ✅ Aucun crash ne doit se produire (exceptions gérées)
- ⚠️  Les "Failed" sont attendus car ils indiquent le rejet correct de données invalides

#### Tests de validation cryptographique

Le script `CryptographicAuditTests.ps1` inclut également :

**1. Tests d'intégrité** :
- Vérification que les clés partagées sont identiques après encapsulation/décapsulation

**2. Tests d'authentification** :
- Vérification que les signatures sont valides avec les clés publiques correspondantes

**3. Tests de rejet** :
- Vérification que les données corrompues sont correctement rejetées

**4. Tests de non-déterminisme** :
- Vérification que les clés générées sont différentes à chaque génération

**5. Tests de stress** :
- Génération multiple de clés
- Encapsulation/décapsulation multiple
- Mesure des performances sous charge

#### Interprétation des résultats

**Tests de conformité** :
- `Passed = true` : Le test est conforme aux standards
- `Passed = false` : Le test a échoué, vérifier les erreurs dans `Errors`

**Tests de fuzzing** :
- `Crashed = 0` : Aucune exception non gérée (bon signe)
- `Failed > 0` : Normal, indique le rejet correct de données invalides
- `Crashed > 0` : Problème potentiel, exceptions non gérées détectées

**Exemple complet** : Voir `CryptographicAuditTests.ps1` pour un script complet d'audit et de validation.

### Scénario 17 : Protocole sécurisé combinant Kyber + Ed25519

Ce scénario combine les deux algorithmes pour créer un protocole sécurisé complet :
- **Kyber (ML-KEM)** : Pour l'échange de clés résistant aux ordinateurs quantiques
- **Ed25519** : Pour l'authentification et l'intégrité des données

```powershell
# === Étape 1: Générer les clés ===
# Générer des clés Kyber pour l'échange de clés
$kyberKeys = New-KyberKeyPair -ParameterSet Kyber768

# Générer des clés Ed25519 pour la signature
$ed25519Keys = New-Ed25519KeyPair

# === Étape 2: Encapsuler une clé partagée avec Kyber ===
$encapsulated = Invoke-KyberEncapsulate -PublicKey $kyberKeys.PublicKey -ParameterSet Kyber768

# === Étape 3: Signer la clé partagée avec Ed25519 ===
$signature = Invoke-Ed25519Sign -Data $encapsulated.SharedSecret -PrivateKey $ed25519Keys.PrivateKey

# === Étape 4: Vérifier la signature ===
$isValid = Test-Ed25519Signature -Data $encapsulated.SharedSecret -Signature $signature.Signature -PublicKey $ed25519Keys.PublicKey

# === Étape 5: Décapsuler et vérifier ===
$decapsulated = Invoke-KyberDecapsulate -Ciphertext $encapsulated.Ciphertext -PrivateKey $kyberKeys.PrivateKey

# Vérifier que les clés partagées correspondent
if ($encapsulated.SharedSecretHex -eq $decapsulated.SharedSecretHex) {
    Write-Host "✓ Clés partagées identiques !"
}

# Vérifier la signature avec la clé décapsulée
$isValidDecapsulated = Test-Ed25519Signature -Data $decapsulated.SharedSecret -Signature $signature.Signature -PublicKey $ed25519Keys.PublicKey
```

**Exemple complet** : Voir `TestKyberEd25519.ps1` pour un scénario complet avec export/import de clés.

---

## 🔧 Détails techniques

### Tailles des clés et ciphertexts

#### Kyber (ML-KEM)

| Paramètre | Clé publique | Clé privée | Ciphertext | Clé partagée |
|-----------|-------------|------------|------------|--------------|
| Kyber512  | 800 bytes   | 1632 bytes | 768 bytes  | 32 bytes     |
| Kyber768  | 1184 bytes  | 2400 bytes | 1088 bytes | 32 bytes     |
| Kyber1024 | 1568 bytes  | 3168 bytes | 1568 bytes | 32 bytes     |

#### Dilithium (ML-DSA) - 100% Post-Quantum ⭐

| Paramètre | Clé publique | Clé privée | Signature | Niveau NIST |
|-----------|--------------|------------|-----------|-------------|
| Dilithium2 (ML-DSA-44) | ~1312 bytes | ~2560 bytes | ~2420 bytes | Niveau 1 |
| Dilithium3 (ML-DSA-65) | ~1952 bytes | ~4032 bytes | ~3309 bytes | Niveau 2 |
| Dilithium5 (ML-DSA-87) | ~2592 bytes | ~4864 bytes | ~4627 bytes | Niveau 3 |

#### Ed25519 (Compatibilité)

| Type | Taille |
|------|--------|
| Clé publique | 32 bytes |
| Clé privée | 32 bytes |
| Signature | 64 bytes |

**Remarque** : Ed25519 est un algorithme de signature numérique classique (non post-quantique) mais très performant et largement utilisé. Il est recommandé pour l'authentification et l'intégrité des données, tandis que Kyber est utilisé pour l'échange de clés résistant aux ordinateurs quantiques.

### Sérialisation de la clé privée

Le module utilise un format de sérialisation personnalisé pour éviter les problèmes avec `GetEncoded()` de BouncyCastle :

```
Format: [Magic(4)] [sLength(4)]s [hpkLength(4)]hpk [nonceLength(4)]nonce [tLength(4)]t [rhoLength(4)]rho

Magic = 0x4B594245 ("KYBE" en ASCII)
```

**Avantages** :
- Format fiable et reproductible
- Reconstruction correcte des composants
- Compatible avec tous les paramètres Kyber

### Architecture de sécurité

- **Kyber512** : Niveau de sécurité équivalent à AES-128 (résistant aux attaques classiques et quantiques)
- **Kyber768** : Niveau de sécurité équivalent à AES-192 (recommandé pour la plupart des applications)
- **Kyber1024** : Niveau de sécurité équivalent à AES-256 (niveau de sécurité maximal)

### Gestion de la mémoire

- Les clés sont stockées en mémoire comme `byte[]`
- Les conversions hexadécimales sont effectuées à la demande
- **Gestion sécurisée disponible** : Utilisez `New-Secure*KeyPair` pour un nettoyage automatique des clés privées
- **Nettoyage manuel** : Utilisez `Clear-SecureKey` pour nettoyer manuellement les clés après utilisation
- **Vérification** : Utilisez `Test-ZeroizedKey` pour vérifier si une clé a été nettoyée

#### Gestion Sécurisée des Clés ⭐

Le module fournit des cmdlets et classes pour la gestion sécurisée des clés en mémoire :

**Cmdlets sécurisés** :
- `New-SecureKyberKeyPair` : Génère une paire de clés Kyber avec nettoyage automatique
- `New-SecureDilithiumKeyPair` : Génère une paire de clés Dilithium avec nettoyage automatique
- `New-SecureEd25519KeyPair` : Génère une paire de clés Ed25519 avec nettoyage automatique
- `Clear-SecureKey` : Nettoie manuellement une clé en mémoire (zeroization)
- `Test-ZeroizedKey` : Vérifie si une clé a été nettoyée

**Caractéristiques** :
- ✅ Nettoyage automatique lors de la destruction de l'objet (IDisposable)
- ✅ Nettoyage multi-passes (protection contre cold boot attacks)
- ✅ Thread-safe avec verrous
- ✅ Finalizer pour nettoyage même si Dispose() n'est pas appelé

**Exemple** :
```powershell
# Génération sécurisée
$secureKeys = New-SecureKyberKeyPair -ParameterSet Kyber768

# Utiliser les clés
$publicKey = $secureKeys.PublicKey
$privateKey = $secureKeys.PrivateKey

# Nettoyer immédiatement après utilisation
$secureKeys.ZeroizePrivateKey()

# Ou laisser le nettoyage automatique (garbage collector)
```

**Voir** : `GESTION_SECURISEE_CLES.md` pour la documentation complète.

#### Protection contre les Canaux Auxiliaires ⭐

Le module fournit des contre-mesures contre les attaques par canaux auxiliaires :

**Classes de protection** :
- `ConstantTimeOperations` : Opérations à temps constant (protection timing attacks)
- `SideChannelProtection` : Protection contre cache side-channel attacks

**Cmdlets sécurisés** :
- `Invoke-KyberDecapsulateSecure` : Décapsulation avec protection side-channel
- `Test-DilithiumSignatureSecure` : Vérification de signature avec protection side-channel
- `Test-ConstantTimeCompare` : Comparaison en temps constant

**Caractéristiques** :
- ✅ Opérations à temps constant (pas de branches conditionnelles basées sur données secrètes)
- ✅ Nettoyage du cache après opérations sensibles
- ✅ Accès mémoire uniformes pour masquer les patterns
- ✅ Protection contre timing attacks et cache side-channel attacks

**Exemple** :
```powershell
# Décapsulation sécurisée
$sharedSecret = Invoke-KyberDecapsulateSecure -Ciphertext $ciphertext -PrivateKey $privateKey

# Vérification sécurisée
$isValid = Test-DilithiumSignatureSecure -Data $data -Signature $signature -PublicKey $publicKey

# Comparaison en temps constant
$areEqual = Test-ConstantTimeCompare -Array1 $key1 -Array2 $key2
```

**Voir** : `CONTRE_MESURES_SIDE_CHANNEL.md` pour la documentation complète.

---

## 🐛 Dépannage

### Problème : Module non trouvé

**Erreur** : `The specified module 'KyberModule' was not loaded`

**Solution** :
```powershell
# Vérifier que le module est dans le bon répertoire
Test-Path ".\KyberModule.psd1"

# Importer avec le chemin complet
Import-Module ".\KyberModule.psd1" -Force
```

### Problème : DLL manquante

**Erreur** : `Could not load file or assembly 'BouncyCastle.Crypto'`

**Solution** :
1. Vérifier que `BouncyCastle.Crypto.dll` est dans le même répertoire que `KyberModule.dll`
2. Vérifier que la version est 2.2.1 ou compatible

### Problème : Clés partagées ne correspondent pas

**Symptôme** : Les clés partagées après encapsulation/décapsulation sont différentes

**Causes possibles** :
- Mauvais paramètre `-ParameterSet` entre encapsulation et décapsulation
- Clé publique et clé privée ne correspondent pas
- Ciphertext corrompu

**Solution** :
```powershell
# Vérifier que les paramètres correspondent
$keys = New-KyberKeyPair -ParameterSet Kyber768
$encapsulated = Invoke-KyberEncapsulate -PublicKey $keys.PublicKey -ParameterSet Kyber768
$decapsulated = Invoke-KyberDecapsulate -Ciphertext $encapsulated.Ciphertext -PrivateKey $keys.PrivateKey -ParameterSet Kyber768
```

### Problème : Erreur de compilation

**Erreur** : `error CS8370: La fonctionnalité 'modèles récursifs' n'est pas disponible`

**Solution** : Vérifier que `LangVersion` est défini à `latest` dans les fichiers `.csproj`

---

## 📚 Ressources

- **NIST FIPS 203** : Standard ML-KEM (Module-Lattice-Based Key-Encapsulation Mechanism)
- **BouncyCastle Documentation** : https://www.bouncycastle.org/documentation.html
- **PowerShell Standard Library** : https://github.com/PowerShell/PowerShellStandard

### 📖 Documentation Technique

Pour une documentation complète sur la recherche et le développement du projet, consultez :
- **`RECHERCHE_ET_DEVELOPPEMENT.md`** : Document annexe détaillant toutes les étapes, recherches, problèmes rencontrés et solutions trouvées lors du développement du module.
- **`ANALYSE_DILITHIUM.md`** : Analyse complète de l'intégration de CRYSTALS-Dilithium (ML-DSA) dans le projet.
- **`MIGRATION_BOUNCYCASTLE.md`** : Guide de migration de BouncyCastle.NetCore vers BouncyCastle.Cryptography 2.6.2.

### 🎯 Fonctionnalités Avancées

#### Fonctions de Hachage SHA3
- **SHA3-256** : Hachage 256 bits recommandé pour ML-DSA
- **SHA3-384** : Hachage 384 bits pour sécurité renforcée
- **Pré-hachage optionnel** : Intégration dans les workflows Dilithium

#### Chiffrement Authenticated Encryption (AEAD)
- **ChaCha20-Poly1305** : Algorithme moderne et performant
- **AES-GCM** : Standard industrie pour chiffrement authentifié
- **Authentification intégrée** : Garantit l'intégrité et l'authenticité des données

#### Conformité Standards PKI
- **PKCS#8** : Export/import de clés privées en format standard (DER/PEM)
- **X.509** : Création et vérification de certificats auto-signés avec clés post-quantiques
- **CMS** : Support des enveloppes chiffrées (signatures en développement)

---

## 📝 Licence

Ce module utilise BouncyCastle.NetCore qui est sous licence MIT.

---

## 👥 Contribution

Pour contribuer au projet :
1. Fork le repository
2. Créer une branche pour votre fonctionnalité
3. Faire vos modifications
4. Tester avec `Test.ps1`
5. Créer une pull request

---
## 📧 Support

Pour toute question ou problème, consultez :
- La documentation BouncyCastle
- Les exemples dans `Examples.ps1`
- Les tests dans `Test.ps1`

---

**Version** : 1.1.0  
**Dernière mise à jour** : Décembre 2025

---

## 📖 Guide d'utilisation

### Communications distantes (KyberDaemon + KyberCLI)
1. **S'assurer que le service est démarré** (voir section Installation).
2. **Utiliser KyberCLI directement** :
   ```powershell
   dotnet KyberCLI.dll connect --host 192.168.1.10 --user alice --password 0x09AF... --command "hostname"
   dotnet KyberCLI.dll connect --host 192.168.1.10 --user bob --key C:\secrets\bob.key
   ```
3. **Depuis PowerShell** avec la cmdlet `Invoke-SecureCommand` (wrapper KyberCLI) :
   ```powershell
   Invoke-SecureCommand \
       -ServerHost 192.168.1.10 \
       -Port 8443 \
       -Username alice \
       -PasswordHex 0x09AF... \
       -Command "Get-Process | Select-Object -First 5 Name,Id"
   ```
   Paramètres supplémentaires :
   - `-KeyPath` : clé privée Dilithium (base64) pour signature
   - `-CliPath` : binaire/dll KyberCLI personnalisé
   - `-RebuildCli` : force `dotnet publish` avant exécution

> ⚠️ `Start-SecureServer`, `Connect-SecureClient`, `Send/Receive-SecureMessage` sont conservés pour compatibilité mais renvoient désormais une erreur explicite. Utilisez KyberDaemon/KyberCLI pour toute communication réseau.

### Communication

| Cmdlet | Statut | Description |
|--------|--------|-------------|
| `Invoke-SecureCommand` | ✅ Actif | Exécute une commande distante via KyberCLI (KyberDaemon requis) |
| `Start-SecureServer` | ⚠️ Obsolète | Ancienne implémentation serveur (renvoie une erreur guidant vers KyberDaemon) |
| `Connect-SecureClient` | ⚠️ Obsolète | Ancien client PowerShell (renvoie une erreur guidant vers KyberCLI) |
| `Send-SecureMessage` | ⚠️ Obsolète | Ancienne commande d'envoi (renvoie une erreur guidant vers KyberCLI) |
| `Receive-SecureMessage` | ⚠️ Obsolète | Ancienne commande de réception (renvoie une erreur guidant vers KyberCLI) |

---

## 📖 Outils d'administration

- `KyberCLI keys list --config /etc/kyberd/kyberd.conf` : affiche les versions de clés stockées (répertoires `session.vN.*`)
- `KyberCLI keys rotate --name session --days 90` : force la rotation immédiate (respecte la passphrase du `kyberd.conf`)
- `KyberCLI keys export --output metadata.json` : extrait les métadonnées (versions, modes de chiffrement)
- `New-KyberKeyBundle` : génère automatiquement un bundle de clés Dilithium/Kyber (client, serveur ou les deux), peut mettre à jour `auth.conf` et déclencher la rotation Kyber via KyberCLI
- Script PowerShell dédié (`KyberDaemon/tools/Invoke-KyberKeyRotation.ps1`) enveloppant `KyberCLI keys` pour Windows / PowerShell Core (`-Action List|Rotate|Export`, `-Config`, `-Name`, `-Days`, `-CliPath`...)

### Tests & Qualité
- Tests unitaires/crypto historiques (scripts PowerShell dans `KyberModule/`)
- Tests d'intégration end-to-end (`dotnet test tests/KyberIntegrationTests`) : démarre un daemon éphémère, exécute KyberCLI et injecte du trafic corrompu pour valider la robustesse réseau
- Fuzzer réseau simple (`NetworkFuzzer.SendRandomHandshakeAsync`) utilisable depuis les tests ou un harness personnalisé pour enrichir votre campagne fuzz

### Benchmarks
- Projet `tests/KyberBenchmarks` (BenchmarkDotNet)
  - `CommandLatencyBenchmark` mesure la latence moyenne d'une commande `echo`
  - `HandshakeBenchmark` quantifie le coût handshake/fermeture d'une session complète
  - `ConcurrentCommandsBenchmark` ouvre plusieurs clients en parallèle (`Params(2,4,8)`) pour évaluer la tenue aux connexions simultanées
- Exécution : `dotnet run -c Release -p tests/KyberBenchmarks`
- Résultats générés (fichier markdown/csv) dans `BenchmarkDotNet.Artifacts`

---

## 📖 Guide d'utilisation

### Scénario 1 : Génération de clés

```powershell
# Générer une paire de clés avec Kyber768 (par défaut)
$keys = New-KyberKeyPair

# Spécifier le niveau de sécurité
$keys512 = New-KyberKeyPair -ParameterSet Kyber512
$keys768 = New-KyberKeyPair -ParameterSet Kyber768
$keys1024 = New-KyberKeyPair -ParameterSet Kyber1024

# Accéder aux clés
Write-Host "Clé publique (hex): $($keys.PublicKeyHex)"
Write-Host "Clé privée (hex): $($keys.PrivateKeyHex)"
```

### Scénario 2 : Échange de clés (Alice et Bob)

```powershell
# Alice génère sa paire de clés
$aliceKeys = New-KyberKeyPair -ParameterSet Kyber768

# Bob utilise la clé publique d'Alice pour créer une clé partagée
$bobEncapsulated = Invoke-KyberEncapsulate -PublicKey $aliceKeys.PublicKey

# Bob envoie le ciphertext à Alice
# $ciphertext = $bobEncapsulated.Ciphertext

# Alice décapsule pour obtenir la même clé partagée
$aliceSharedSecret = Invoke-KyberDecapsulate -Ciphertext $bobEncapsulated.Ciphertext -PrivateKey $aliceKeys.PrivateKey

# Vérifier que les clés correspondent
$match = $bobEncapsulated.SharedSecret -eq $aliceSharedSecret.SharedSecret
```

### Scénario 3 : Utilisation avec des chaînes hexadécimales

```powershell
# Générer des clés
$keys = New-KyberKeyPair

# Utiliser directement les chaînes hex
$encapsulated = Invoke-KyberEncapsulate -PublicKey $keys.PublicKeyHex

# Décapsuler avec les chaînes hex
$sharedSecret = Invoke-KyberDecapsulate -Ciphertext $encapsulated.CiphertextHex -PrivateKey $keys.PrivateKeyHex
```

### Scénario 4 : Sauvegarder et charger des clés

#### Format Text (recommandé pour sauvegarder les paires de clés - sécurisé)
```powershell
# Générer et sauvegarder
$keys = New-KyberKeyPair
Export-KyberKeyPair -KeyPair $keys -Path "keys.txt" -Format Text

# Charger plus tard
$keys = Import-KyberKeyPair -Path "keys.txt"

# Utiliser
$encapsulated = Invoke-KyberEncapsulate -PublicKey $keys.PublicKey
$sharedSecret = Invoke-KyberDecapsulate -Ciphertext $encapsulated.Ciphertext -PrivateKey $keys.PrivateKey
```

#### Format Base64/PEM (compatible avec SSH et autres protocoles)
```powershell
# Exporter en format PEM
$keys = New-KyberKeyPair
Export-KyberKeyPair -KeyPair $keys -Path "keys.pem" -Format Base64

# Exporter uniquement la clé publique (pour la partager)
Export-KyberPublicKey -PublicKey $keys.PublicKey -Path "public_key.pem" -Format Base64

# Importer
$keys = Import-KyberKeyPair -Path "keys.pem"
$publicKey = Import-KyberPublicKey -Path "public_key.pem"
```

### Scénario 5 : Protocole type SSH (serveur/client)

```powershell
# === Côté SERVEUR ===
# 1. Générer les clés du serveur
$serverKeys = New-KyberKeyPair -ParameterSet Kyber768

# 2. Sauvegarder la clé privée (sécurisée)
Export-KyberKeyPair -KeyPair $serverKeys -Path "server_private.txt" -Format Text

# 3. Partager la clé publique avec les clients
Export-KyberPublicKey -PublicKey $serverKeys.PublicKey -Path "server_public.pem" -Format Base64

# 4. Quand un client se connecte, décapsuler la clé partagée
$serverKeys = Import-KyberKeyPair -Path "server_private.txt"
$sharedSecret = Invoke-KyberDecapsulate -Ciphertext $clientCiphertext -PrivateKey $serverKeys.PrivateKey

# === Côté CLIENT ===
# 1. Récupérer la clé publique du serveur
$serverPublicKey = Import-KyberPublicKey -Path "server_public.pem"

# 2. Encapsuler une clé partagée
$encapsulated = Invoke-KyberEncapsulate -PublicKey $serverPublicKey -ParameterSet Kyber768

# 3. Envoyer le ciphertext au serveur
# $encapsulated.Ciphertext

# 4. Utiliser la clé partagée pour la communication
# $encapsulated.SharedSecret
```

### Scénario 6 : Signature numérique avec Dilithium (100% Post-Quantum) ⭐

```powershell
# Générer une paire de clés Dilithium (ML-DSA)
$signingKeys = New-DilithiumKeyPair -ParameterSet Dilithium3

# Signer un message
$message = "Important message"
$messageBytes = [System.Text.Encoding]::UTF8.GetBytes($message)
$signature = Invoke-DilithiumSign -Data $messageBytes -PrivateKey $signingKeys.PrivateKey -ParameterSet Dilithium3

# Vérifier la signature
$isValid = Test-DilithiumSignature -Data $messageBytes -Signature $signature.Signature -PublicKey $signingKeys.PublicKey -ParameterSet Dilithium3

# Sauvegarder les clés
Export-DilithiumKeyPair -KeyPair $signingKeys -Path "signing_keys.txt" -Format Text
```

### Scénario 7 : Signature numérique avec Ed25519 (Compatibilité)

```powershell
# Générer une paire de clés Ed25519
$signingKeys = New-Ed25519KeyPair

# Signer un message
$message = "Important message"
$signature = Invoke-Ed25519Sign -Data $message -PrivateKey $signingKeys.PrivateKey

# Vérifier la signature
$isValid = Test-Ed25519Signature -Data $message -Signature $signature.Signature -PublicKey $signingKeys.PublicKey

# Sauvegarder les clés
Export-Ed25519KeyPair -KeyPair $signingKeys -Path "signing_keys.txt" -Format Text
```

### Scénario 8 : Protocole sécurisé 100% Post-Quantum : Kyber + Dilithium ⭐

Ce scénario combine les deux algorithmes post-quantiques pour créer un protocole sécurisé 100% résistant aux ordinateurs quantiques :
- **Kyber (ML-KEM)** : Pour l'échange de clés résistant aux ordinateurs quantiques
- **Dilithium (ML-DSA)** : Pour l'authentification et l'intégrité des données (post-quantique)

```powershell
# 1. Générer les clés post-quantiques
$kyberKeys = New-KyberKeyPair -ParameterSet Kyber768
$dilithiumKeys = New-DilithiumKeyPair -ParameterSet Dilithium3

# 2. Encapsuler une clé partagée avec Kyber
$encapsulated = Invoke-KyberEncapsulate -PublicKey $kyberKeys.PublicKey -ParameterSet Kyber768

# 3. Signer la clé partagée avec Dilithium
$signature = Invoke-DilithiumSign -Data $encapsulated.SharedSecret -PrivateKey $dilithiumKeys.PrivateKey -ParameterSet Dilithium3

# 4. Vérifier la signature
$isValid = Test-DilithiumSignature -Data $encapsulated.SharedSecret -Signature $signature.Signature -PublicKey $dilithiumKeys.PublicKey -ParameterSet Dilithium3

# 5. Décapsuler la clé partagée
$sharedSecret = Invoke-KyberDecapsulate -Ciphertext $encapsulated.Ciphertext -PrivateKey $kyberKeys.PrivateKey -ParameterSet Kyber768
```

**Avantages de cette combinaison** :
- 🔐 **Kyber (ML-KEM)** : Échange de clés résistant aux ordinateurs quantiques
- ✍️ **Dilithium (ML-DSA)** : Authentification et intégrité post-quantiques
- 🔑 **Clé partagée** : 32 bytes sécurisée et authentifiée
- 🛡️ **100% Post-Quantum** : Résistant aux attaques classiques et quantiques

**Scripts d'exemples** :
- `TestKyberDilithium.ps1` : Test rapide de l'intégration
- `KyberDilithiumCompleteExample.ps1` : Scénario serveur/client complet

### Scénario 9 : Hachage SHA3 avec ML-DSA

```powershell
# Calculer un hachage SHA3-256
$data = "Données à hacher"
$hash256 = Get-SHA3Hash -Data $data -Variant SHA3_256

# Calculer un hachage SHA3-384
$hash384 = Get-SHA3Hash -Data $data -Variant SHA3_384

# Signer avec Dilithium en utilisant SHA3 pré-hachage (recommandé)
$keys = New-DilithiumKeyPair -ParameterSet Dilithium3
$signature = Invoke-DilithiumSign -Data $dataBytes -PrivateKey $keys.PrivateKey -PreHash -SHA3Variant SHA3_256
```

### Scénario 10 : Chiffrement Authenticated Encryption

#### ChaCha20-Poly1305
```powershell
# Chiffrer avec ChaCha20-Poly1305
$plaintext = "Message secret"
$key = New-Object byte[] 32
# Générer une clé (ou utiliser une clé partagée Kyber)
$encrypted = Protect-WithChaCha20Poly1305 -Data $plaintext -Key $key

# Déchiffrer
$decrypted = Unprotect-ChaCha20Poly1305 -EncryptedData $encrypted.EncryptedData -Key $key -Nonce $encrypted.Nonce -Tag $encrypted.Tag
```

#### AES-GCM
```powershell
# Chiffrer avec AES-GCM
$plaintext = "Message secret"
$key = New-Object byte[] 32
$encrypted = Protect-WithAESGCM -Data $plaintext -Key $key

# Déchiffrer
$decrypted = Unprotect-AESGCM -EncryptedData $encrypted.EncryptedData -Key $key -Nonce $encrypted.Nonce -Tag $encrypted.Tag
```

### Scénario 11 : Export/Import PKCS#8 et Certificats X.509

```powershell
# Exporter une clé privée Dilithium en format PKCS#8
$keys = New-DilithiumKeyPair -ParameterSet Dilithium3
Export-PrivateKeyPKCS8 -PrivateKey $keys.PrivateKey -Path "dilithium_key.pem" -Format PEM -KeyType Dilithium -ParameterSet Dilithium3

# Importer une clé privée PKCS#8
$imported = Import-PrivateKeyPKCS8 -Path "dilithium_key.pem" -KeyType Dilithium

# Créer un certificat X.509 auto-signé avec Dilithium
$cert = New-X509Certificate -SubjectName "CN=Test Post-Quantum Certificate" -PrivateKey $keys.PrivateKey -ParameterSet Dilithium3 -Path "certificate.pem" -Format PEM

# Vérifier un certificat X.509
$isValid = Test-X509Certificate -Path "certificate.pem"
```

### Scénario 12 : Configuration TLS 1.3 Post-Quantum Hybride ⭐

Ce scénario permet de créer des configurations TLS 1.3 hybrides combinant des algorithmes classiques (ECDSA, RSA) avec des algorithmes post-quantiques (Kyber, Dilithium) pour une transition progressive vers la cryptographie post-quantique.

```powershell
# Générer une configuration TLS hybride par défaut (ECDSA-P256 + Kyber768 + Dilithium3)
$hybridConfig = New-TLS13HybridConfig

# Obtenir le nom du cipher suite
$cipherSuite = Get-TLS13HybridCipherSuite -Config $hybridConfig
Write-Host "Cipher Suite: $cipherSuite"  # TLS13-ECDSA-P256+ML-KEM-768+ML-DSA-65

# Générer une configuration avec RSA
$rsaConfig = New-TLS13HybridConfig -ClassicalAlgorithm RSA_2048 -PostQuantumKemAlgorithm Kyber768 -PostQuantumSigAlgorithm Dilithium3

# Générer une configuration haute sécurité
$highSecConfig = New-TLS13HybridConfig -ClassicalAlgorithm ECDSA_P384 -PostQuantumKemAlgorithm Kyber1024 -PostQuantumSigAlgorithm Dilithium5

# Exporter la configuration pour intégration TLS
Export-TLS13HybridConfig -Config $hybridConfig -Path "tls13_hybrid_config.txt" -Force
```

**Algorithmes classiques supportés** :
- `ECDSA_P256` : ECDSA avec courbe P-256 (NIST)
- `ECDSA_P384` : ECDSA avec courbe P-384 (NIST)
- `RSA_2048` : RSA 2048 bits
- `RSA_3072` : RSA 3072 bits

**Algorithmes post-quantiques supportés** :
- **KEM** : Kyber512, Kyber768, Kyber1024 (ML-KEM)
- **Signature** : Dilithium2, Dilithium3, Dilithium5 (ML-DSA)

**Avantages de l'hybridation** :
- 🔐 Sécurité classique éprouvée (compatibilité avec infrastructure existante)
- 🛡️ Sécurité post-quantique (résistance aux ordinateurs quantiques)
- ✅ Transition progressive vers la cryptographie post-quantique
- 🔄 Compatibilité avec bibliothèques TLS supportant les extensions post-quantiques

**Exemple complet** : Voir `TLS13HybridExamples.ps1` pour des scénarios complets.

### Scénario 13 : Benchmark de Performance Haute Précision

Le script `PerformanceBenchmark.ps1` permet de mesurer avec haute précision les performances de tous les algorithmes cryptographiques du module :

```powershell
# Exécuter le benchmark complet
.\PerformanceBenchmark.ps1
```

Le script mesure :
- ✅ Temps de génération de clés (Kyber, Dilithium, Ed25519)
- ✅ Temps d'encapsulation/décapsulation (Kyber)
- ✅ Temps de signature/vérification (Dilithium, Ed25519)
- ✅ Temps de hachage (SHA3-256, SHA3-384)
- ✅ Temps de chiffrement/déchiffrement (ChaCha20-Poly1305, AES-GCM)

**Métriques affichées** :
- **Moyenne** : Temps moyen en µs et ms (4 décimales pour ms, 2 pour µs)
- **Médiane** : Temps médian (moins sensible aux valeurs aberrantes)
- **Minimum et Maximum** : Plage de variation des temps
- **Écart-type** : Mesure de la variabilité des performances
- **Statut** : SUCCESS (toutes réussies), PARTIAL (certaines échouées), FAILED (toutes échouées)
- **Itérations** : Nombre d'itérations réussies vs total

**Caractéristiques de précision** :
- **Opérations lentes** (Kyber/Dilithium) : 10 itérations
- **Opérations rapides** (Ed25519/SHA3/AEAD) : 500-1000 itérations pour précision
- **Warm-up automatique** : Exécution préalable pour éviter les effets de cache/JIT
- **Mesures en microsecondes** : Précision maximale pour opérations rapides
- **Export CSV** : Toutes les données exportables pour analyse approfondie

**Exemple de sortie** :
```
✅ Génération de clés Ed25519:
   Moyenne: 125.50 µs (0.1255 ms)
   Médiane: 124.20 µs (0.1242 ms)
   Min: 120.10 µs (0.1201 ms) | Max: 135.30 µs (0.1353 ms)
   Écart-type: 3.45 µs (0.0035 ms)
   Itérations: 1000/1000 réussies
```

**Export CSV** :
Le script génère également un export CSV avec toutes les métriques :
- Operation, Status, Average_ms, Average_us, Median_ms, Median_us, Min_ms, Min_us, Max_ms, Max_us, StdDev_ms, StdDev_us, SuccessfulIterations, TotalIterations, Errors

### Scénario 14 : Gestion Sécurisée des Clés en Mémoire ⭐

Ce scénario démontre l'utilisation de la gestion sécurisée des clés pour protéger les clés privées en mémoire.

```powershell
# Générer une paire de clés avec gestion sécurisée
$secureKeys = New-SecureKyberKeyPair -ParameterSet Kyber768

# Utiliser les clés
$publicKey = $secureKeys.PublicKey
$privateKey = $secureKeys.PrivateKey

# Encapsuler et décapsuler
$encapsulated = Invoke-KyberEncapsulate -PublicKey $publicKey -ParameterSet Kyber768
$decapsulated = Invoke-KyberDecapsulate -Ciphertext $encapsulated.Ciphertext -PrivateKey $privateKey -ParameterSet Kyber768

# Nettoyer immédiatement la clé privée après utilisation
$secureKeys.ZeroizePrivateKey()

# Vérifier que la clé est nettoyée
$isZeroized = Test-ZeroizedKey -Key $privateKey
Write-Host "Clé nettoyée: $isZeroized"  # True
```

**Nettoyage manuel avec Clear-SecureKey** :
```powershell
# Générer des clés normales
$keys = New-KyberKeyPair -ParameterSet Kyber768

# Utiliser les clés...

# Nettoyer manuellement avec une seule passe
Clear-SecureKey -Key $keys.PrivateKey -Passes 1

# Ou avec plusieurs passes pour sécurité renforcée (protection cold boot)
Clear-SecureKey -Key $keys.PrivateKey -Passes 3 -Nullify

# Vérifier le nettoyage
$isClean = Test-ZeroizedKey -Key $keys.PrivateKey
```

**Pattern try-finally pour nettoyage garanti** :
```powershell
$keys = New-KyberKeyPair -ParameterSet Kyber768
try {
    # Utiliser les clés...
    $encapsulated = Invoke-KyberEncapsulate -PublicKey $keys.PublicKey
}
finally {
    # Nettoyer dans tous les cas (même en cas d'exception)
    Clear-SecureKey -Key $keys.PrivateKey -Passes 3 | Out-Null
}
```

**Avantages** :
- 🔐 Nettoyage automatique des clés privées
- 🛡️ Protection contre les attaques de récupération de mémoire
- ✅ Nettoyage multi-passes pour sécurité renforcée
- 🔄 Pattern Dispose pour nettoyage garanti
**Exemple complet** : Voir `SecureKeyManagementExamples.ps1` pour des scénarios complets.
### Scénario 15 : Protection contre les Canaux Auxiliaires (Side-Channel Attacks) ⭐

Ce scénario démontre l'utilisation des contre-mesures contre les attaques par canaux auxiliaires (timing attacks, cache side-channel attacks).

#### Vue d'ensemble

Les attaques par canaux auxiliaires exploitent des informations indirectes (temps d'exécution, accès au cache) pour extraire des secrets cryptographiques. Le module implémente des contre-mesures robustes pour protéger contre ces attaques.

#### Cmdlets disponibles

**1. Décapsulation sécurisée Kyber** :
```powershell
# Décapsulation avec protection side-channel
$keys = New-KyberKeyPair -ParameterSet Kyber768
$encapsulated = Invoke-KyberEncapsulate -PublicKey $keys.PublicKey -ParameterSet Kyber768

# Méthode normale
$decapsulatedNormal = Invoke-KyberDecapsulate -Ciphertext $encapsulated.Ciphertext -PrivateKey $keys.PrivateKey -ParameterSet Kyber768

# Méthode sécurisée (protection side-channel)
$decapsulatedSecure = Invoke-KyberDecapsulateSecure -Ciphertext $encapsulated.Ciphertext -PrivateKey $keys.PrivateKey -ParameterSet Kyber768

# Les résultats sont identiques, mais la méthode sécurisée protège contre les attaques
```

**2. Vérification de signature sécurisée Dilithium** :
```powershell
# Génération et signature
$keys = New-DilithiumKeyPair -ParameterSet Dilithium3
$message = "Message important à signer"
$messageBytes = [System.Text.Encoding]::UTF8.GetBytes($message)
$signature = Invoke-DilithiumSign -Data $messageBytes -PrivateKey $keys.PrivateKey -ParameterSet Dilithium3

# Vérification normale
$isValidNormal = Test-DilithiumSignature -Data $messageBytes -Signature $signature.Signature -PublicKey $keys.PublicKey -ParameterSet Dilithium3

# Vérification sécurisée (protection side-channel)
$isValidSecure = Test-DilithiumSignatureSecure -Data $messageBytes -Signature $signature.Signature -PublicKey $keys.PublicKey -ParameterSet Dilithium3
```

**3. Comparaison en temps constant** :
```powershell
# Comparaison de clés partagées en temps constant
$key1 = [byte[]]::new(32)
$key2 = [byte[]]::new(32)
# ... remplir les clés ...

# Comparaison sécurisée (protège contre timing attacks)
$areEqual = Test-ConstantTimeCompare -Array1 $key1 -Array2 $key2

# Supporte aussi les chaînes hexadécimales
$hex1 = "0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF"
$hex2 = "0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF"
$areEqual = Test-ConstantTimeCompare -Array1 $hex1 -Array2 $hex2
```

#### Protections implémentées

**1. Opérations à temps constant** :
- ✅ Pas de branches conditionnelles basées sur des données secrètes
- ✅ Comparaisons qui prennent toujours le même temps
- ✅ Masquage des variations de timing

**2. Protection contre timing attacks** :
- ✅ Toutes les comparaisons utilisent `ConstantTimeEquals()`
- ✅ Pas de sortie anticipée lors des comparaisons
- ✅ Temps d'exécution indépendant du contenu des données

**3. Protection contre cache side-channel attacks** :
- ✅ Nettoyage du cache après opérations sensibles
- ✅ Accès mémoire uniformes pour masquer les patterns
- ✅ Préchargement du cache pour éviter les variations

**4. Méthodes sécurisées** :
- `DecapsulateSecure()` : Décapsulation Kyber avec protection side-channel
- `VerifySecure()` : Vérification Dilithium avec protection side-channel
- `ConstantTimeCompareSharedSecrets()` : Comparaison de clés partagées en temps constant
- `ConstantTimeCompareSignatures()` : Comparaison de signatures en temps constant

#### Exemple d'intégration complète

```powershell
# Protocole complet avec protections side-channel
$kyberKeys = New-KyberKeyPair -ParameterSet Kyber768
$dilithiumKeys = New-DilithiumKeyPair -ParameterSet Dilithium3

# Encapsulation
$encapsulated = Invoke-KyberEncapsulate -PublicKey $kyberKeys.PublicKey -ParameterSet Kyber768

# Décapsulation sécurisée
$sharedSecret = Invoke-KyberDecapsulateSecure -Ciphertext $encapsulated.Ciphertext -PrivateKey $kyberKeys.PrivateKey -ParameterSet Kyber768

# Signature de la clé partagée
$signature = Invoke-DilithiumSign -Data $sharedSecret.SharedSecret -PrivateKey $dilithiumKeys.PrivateKey -ParameterSet Dilithium3

# Vérification sécurisée
$isValid = Test-DilithiumSignatureSecure -Data $sharedSecret.SharedSecret -Signature $signature.Signature -PublicKey $dilithiumKeys.PublicKey -ParameterSet Dilithium3
```

#### Tests de validation

Un script de tests complet est disponible : `SideChannelProtectionTests.ps1`

```powershell
# Exécuter tous les tests de protection side-channel
.\SideChannelProtectionTests.ps1
```

Les tests valident :
- ✅ Comparaisons en temps constant
- ✅ Décapsulation sécurisée Kyber
- ✅ Vérification sécurisée Dilithium
- ✅ Tests de timing (vérification que les opérations prennent un temps similaire)
- ✅ Détection de différences à toutes les positions
- ✅ Intégration complète avec protections side-channel
- ✅ Support de différentes tailles de données

**Exemple complet** : Voir `CONTRE_MESURES_SIDE_CHANNEL.md` pour la documentation technique complète.

### Scénario 16 : Audit et Validation Cryptographique ⭐

Ce scénario démontre l'utilisation des outils d'audit et de validation cryptographique pour vérifier la conformité aux standards NIST et la robustesse des implémentations.

#### Vue d'ensemble

Le module fournit des outils complets pour :
- **Tests de conformité** : Validation des tailles, formats et comportements selon les standards NIST (FIPS 203, ML-DSA)
- **Tests de fuzzing** : Génération de données corrompues pour tester la robustesse
- **Tests de validation cryptographique** : Vérification des propriétés cryptographiques

#### Cmdlets disponibles

**1. Tests de conformité NIST** :
```powershell
# Tester la conformité de tous les algorithmes
$results = Test-CryptographicConformance -Algorithm All

# Tester uniquement Kyber
$kyberResults = Test-CryptographicConformance -Algorithm Kyber -KyberParameterSet Kyber768

# Tester uniquement Dilithium
$dilithiumResults = Test-CryptographicConformance -Algorithm Dilithium -DilithiumParameterSet Dilithium3

# Afficher uniquement les tests échoués
$failed = Test-CryptographicConformance -Algorithm All -ShowOnlyFailed
```

**2. Tests de fuzzing** :
```powershell
# Exécuter des tests de fuzzing sur tous les algorithmes
$fuzzingResults = Invoke-CryptographicFuzzing -Algorithm All -Iterations 100

# Fuzzing Kyber uniquement
$kyberFuzzing = Invoke-CryptographicFuzzing -Algorithm Kyber -KyberParameterSet Kyber768 -Iterations 50

# Afficher uniquement les problèmes (crashes ou échecs)
$issues = Invoke-CryptographicFuzzing -Algorithm All -ShowOnlyIssues
```

**3. Script d'audit complet** :
```powershell
# Exécuter tous les tests d'audit
.\CryptographicAuditTests.ps1
```

#### Tests de conformité

Les tests de conformité valident :

**1. Tailles de clés et ciphertexts** :
- ✅ Clés publiques/privées Kyber selon NIST FIPS 203
- ✅ Ciphertexts et clés partagées Kyber
- ✅ Clés publiques/privées Dilithium selon NIST ML-DSA
- ✅ Signatures Dilithium
- ✅ Clés et signatures Ed25519 selon RFC 8032

**2. Propriétés cryptographiques** :
- ✅ Non-déterminisme des générations de clés
- ✅ Correspondance des clés partagées après encapsulation/décapsulation
- ✅ Correspondance des signatures avec les clés publiques
- ✅ Rejet correct de données invalides

**3. Formats et sérialisation** :
- ✅ Formats de clés conformes
- ✅ Reconstruction correcte des clés privées

#### Tests de fuzzing

Les tests de fuzzing génèrent et testent :
**1. Données corrompues** :
- Corruption de bytes individuels
- Corruption multiple de bytes
- Tailles invalides (trop courtes, trop longues)
- Patterns suspects (tous zéros, tous FF, etc.)

**2. Ciphertexts corrompus** :
- Test de décapsulation avec ciphertexts invalides
- Vérification du rejet correct des données corrompues

**3. Signatures corrompues** :
- Test de vérification avec signatures invalides
- Vérification du rejet correct des signatures corrompues

**4. Clés corrompues** :
- Test avec clés publiques/privées corrompues
- Vérification de la robustesse face aux clés invalides

#### Exemple d'utilisation complète

```powershell
# 1. Tests de conformité
Write-Host "=== Tests de Conformité ===" -ForegroundColor Cyan
$conformanceResults = Test-CryptographicConformance -Algorithm All

foreach ($result in $conformanceResults) {
    if ($result.Passed) {
        Write-Host "✅ $($result.TestName)" -ForegroundColor Green
    } else {
        Write-Host "❌ $($result.TestName)" -ForegroundColor Red
        foreach ($error in $result.Errors) {
            Write-Host "   - $error" -ForegroundColor Yellow
        }
    }
}

# 2. Tests de fuzzing
Write-Host "`n=== Tests de Fuzzing ===" -ForegroundColor Cyan
$fuzzingResults = Invoke-CryptographicFuzzing -Algorithm All -Iterations 50

foreach ($result in $fuzzingResults) {
    Write-Host "$($result.TestName):" -ForegroundColor Cyan
    Write-Host "  Total: $($result.TotalTests) | Passed: $($result.Passed) | Failed: $($result.Failed) | Crashed: $($result.Crashed)" -ForegroundColor Gray
    if ($result.Crashed -gt 0) {
        Write-Host "  ⚠️  $($result.Crashed) crash(es) détecté(s)" -ForegroundColor Yellow
    }
}

# 3. Script d'audit complet
.\CryptographicAuditTests.ps1
```

#### Résultats attendus

**Tests de conformité** :
- ✅ Tous les tests doivent passer
- ✅ Toutes les tailles doivent correspondre aux spécifications NIST
- ✅ Toutes les propriétés cryptographiques doivent être validées

**Tests de fuzzing** :
- ✅ Les données corrompues doivent être rejetées correctement
- ✅ Aucun crash ne doit se produire (exceptions gérées)
- ⚠️  Les "Failed" sont attendus car ils indiquent le rejet correct de données invalides

#### Tests de validation cryptographique

Le script `CryptographicAuditTests.ps1` inclut également :

**1. Tests d'intégrité** :
- Vérification que les clés partagées sont identiques après encapsulation/décapsulation

**2. Tests d'authentification** :
- Vérification que les signatures sont valides avec les clés publiques correspondantes

**3. Tests de rejet** :
- Vérification que les données corrompues sont correctement rejetées

**4. Tests de non-déterminisme** :
- Vérification que les clés générées sont différentes à chaque génération

**5. Tests de stress** :
- Génération multiple de clés
- Encapsulation/décapsulation multiple
- Mesure des performances sous charge

#### Interprétation des résultats

**Tests de conformité** :
- `Passed = true` : Le test est conforme aux standards
- `Passed = false` : Le test a échoué, vérifier les erreurs dans `Errors`

**Tests de fuzzing** :
- `Crashed = 0` : Aucune exception non gérée (bon signe)
- `Failed > 0` : Normal, indique le rejet correct de données invalides
- `Crashed > 0` : Problème potentiel, exceptions non gérées détectées

**Exemple complet** : Voir `CryptographicAuditTests.ps1` pour un script complet d'audit et de validation.

### Scénario 17 : Protocole sécurisé combinant Kyber + Ed25519

Ce scénario combine les deux algorithmes pour créer un protocole sécurisé complet :
- **Kyber (ML-KEM)** : Pour l'échange de clés résistant aux ordinateurs quantiques
- **Ed25519** : Pour l'authentification et l'intégrité des données

```powershell
# === Étape 1: Générer les clés ===
# Générer des clés Kyber pour l'échange de clés
$kyberKeys = New-KyberKeyPair -ParameterSet Kyber768

# Générer des clés Ed25519 pour la signature
$ed25519Keys = New-Ed25519KeyPair

# === Étape 2: Encapsuler une clé partagée avec Kyber ===
$encapsulated = Invoke-KyberEncapsulate -PublicKey $kyberKeys.PublicKey -ParameterSet Kyber768

# === Étape 3: Signer la clé partagée avec Ed25519 ===
$signature = Invoke-Ed25519Sign -Data $encapsulated.SharedSecret -PrivateKey $ed25519Keys.PrivateKey

# === Étape 4: Vérifier la signature ===
$isValid = Test-Ed25519Signature -Data $encapsulated.SharedSecret -Signature $signature.Signature -PublicKey $ed25519Keys.PublicKey

# === Étape 5: Décapsuler et vérifier ===
$decapsulated = Invoke-KyberDecapsulate -Ciphertext $encapsulated.Ciphertext -PrivateKey $kyberKeys.PrivateKey

# Vérifier que les clés partagées correspondent
if ($encapsulated.SharedSecretHex -eq $decapsulated.SharedSecretHex) {
    Write-Host "✓ Clés partagées identiques !"
}

# Vérifier la signature avec la clé décapsulée
$isValidDecapsulated = Test-Ed25519Signature -Data $decapsulated.SharedSecret -Signature $signature.Signature -PublicKey $ed25519Keys.PublicKey
```

**Exemple complet** : Voir `TestKyberEd25519.ps1` pour un scénario complet avec export/import de clés.

---

## 🔧 Détails techniques

### Tailles des clés et ciphertexts

#### Kyber (ML-KEM)

| Paramètre | Clé publique | Clé privée | Ciphertext | Clé partagée |
|-----------|-------------|------------|------------|--------------|
| Kyber512  | 800 bytes   | 1632 bytes | 768 bytes  | 32 bytes     |
| Kyber768  | 1184 bytes  | 2400 bytes | 1088 bytes | 32 bytes     |
| Kyber1024 | 1568 bytes  | 3168 bytes | 1568 bytes | 32 bytes     |

#### Dilithium (ML-DSA) - 100% Post-Quantum ⭐

| Paramètre | Clé publique | Clé privée | Signature | Niveau NIST |
|-----------|--------------|------------|-----------|-------------|
| Dilithium2 (ML-DSA-44) | ~1312 bytes | ~2560 bytes | ~2420 bytes | Niveau 1 |
| Dilithium3 (ML-DSA-65) | ~1952 bytes | ~4032 bytes | ~3309 bytes | Niveau 2 |
| Dilithium5 (ML-DSA-87) | ~2592 bytes | ~4864 bytes | ~4627 bytes | Niveau 3 |

#### Ed25519 (Compatibilité)

| Type | Taille |
|------|--------|
| Clé publique | 32 bytes |
| Clé privée | 32 bytes |
| Signature | 64 bytes |

**Remarque** : Ed25519 est un algorithme de signature numérique classique (non post-quantique) mais très performant et largement utilisé. Il est recommandé pour l'authentification et l'intégrité des données, tandis que Kyber est utilisé pour l'échange de clés résistant aux ordinateurs quantiques.

### Sérialisation de la clé privée

Le module utilise un format de sérialisation personnalisé pour éviter les problèmes avec `GetEncoded()` de BouncyCastle :

```
Format: [Magic(4)] [sLength(4)]s [hpkLength(4)]hpk [nonceLength(4)]nonce [tLength(4)]t [rhoLength(4)]rho

Magic = 0x4B594245 ("KYBE" en ASCII)
```

**Avantages** :
- Format fiable et reproductible
- Reconstruction correcte des composants
- Compatible avec tous les paramètres Kyber

### Architecture de sécurité

- **Kyber512** : Niveau de sécurité équivalent à AES-128 (résistant aux attaques classiques et quantiques)
- **Kyber768** : Niveau de sécurité équivalent à AES-192 (recommandé pour la plupart des applications)
- **Kyber1024** : Niveau de sécurité équivalent à AES-256 (niveau de sécurité maximal)

### Gestion de la mémoire

- Les clés sont stockées en mémoire comme `byte[]`
- Les conversions hexadécimales sont effectuées à la demande
- **Gestion sécurisée disponible** : Utilisez `New-Secure*KeyPair` pour un nettoyage automatique des clés privées
- **Nettoyage manuel** : Utilisez `Clear-SecureKey` pour nettoyer manuellement les clés après utilisation
- **Vérification** : Utilisez `Test-ZeroizedKey` pour vérifier si une clé a été nettoyée

#### Gestion Sécurisée des Clés ⭐

Le module fournit des cmdlets et classes pour la gestion sécurisée des clés en mémoire :

**Cmdlets sécurisés** :
- `New-SecureKyberKeyPair` : Génère une paire de clés Kyber avec nettoyage automatique
- `New-SecureDilithiumKeyPair` : Génère une paire de clés Dilithium avec nettoyage automatique
- `New-SecureEd25519KeyPair` : Génère une paire de clés Ed25519 avec nettoyage automatique
- `Clear-SecureKey` : Nettoie manuellement une clé en mémoire (zeroization)
- `Test-ZeroizedKey` : Vérifie si une clé a été nettoyée

**Caractéristiques** :
- ✅ Nettoyage automatique lors de la destruction de l'objet (IDisposable)
- ✅ Nettoyage multi-passes (protection contre cold boot attacks)
- ✅ Thread-safe avec verrous
- ✅ Finalizer pour nettoyage même si Dispose() n'est pas appelé

**Exemple** :
```powershell
# Génération sécurisée
$secureKeys = New-SecureKyberKeyPair -ParameterSet Kyber768

# Utiliser les clés
$publicKey = $secureKeys.PublicKey
$privateKey = $secureKeys.PrivateKey

# Nettoyer immédiatement après utilisation
$secureKeys.ZeroizePrivateKey()

# Ou laisser le nettoyage automatique (garbage collector)
```

**Voir** : `GESTION_SECURISEE_CLES.md` pour la documentation complète.

#### Protection contre les Canaux Auxiliaires ⭐

Le module fournit des contre-mesures contre les attaques par canaux auxiliaires :

**Classes de protection** :
- `ConstantTimeOperations` : Opérations à temps constant (protection timing attacks)
- `SideChannelProtection` : Protection contre cache side-channel attacks

**Cmdlets sécurisés** :
- `Invoke-KyberDecapsulateSecure` : Décapsulation avec protection side-channel
- `Test-DilithiumSignatureSecure` : Vérification de signature avec protection side-channel
- `Test-ConstantTimeCompare` : Comparaison en temps constant

**Caractéristiques** :
- ✅ Opérations à temps constant (pas de branches conditionnelles basées sur données secrètes)
- ✅ Nettoyage du cache après opérations sensibles
- ✅ Accès mémoire uniformes pour masquer les patterns
- ✅ Protection contre timing attacks et cache side-channel attacks

**Exemple** :
```powershell
# Décapsulation sécurisée
$sharedSecret = Invoke-KyberDecapsulateSecure -Ciphertext $ciphertext -PrivateKey $privateKey

# Vérification sécurisée
$isValid = Test-DilithiumSignatureSecure -Data $data -Signature $signature -PublicKey $publicKey

# Comparaison en temps constant
$areEqual = Test-ConstantTimeCompare -Array1 $key1 -Array2 $key2
```

**Voir** : `CONTRE_MESURES_SIDE_CHANNEL.md` pour la documentation complète.

---

## 🐛 Dépannage

### Problème : Module non trouvé

**Erreur** : `The specified module 'KyberModule' was not loaded`

**Solution** :
```powershell
# Vérifier que le module est dans le bon répertoire
Test-Path ".\KyberModule.psd1"

# Importer avec le chemin complet
Import-Module ".\KyberModule.psd1" -Force
```

### Problème : DLL manquante

**Erreur** : `Could not load file or assembly 'BouncyCastle.Crypto'`

**Solution** :
1. Vérifier que `BouncyCastle.Crypto.dll` est dans le même répertoire que `KyberModule.dll`
2. Vérifier que la version est 2.2.1 ou compatible

### Problème : Clés partagées ne correspondent pas

**Symptôme** : Les clés partagées après encapsulation/décapsulation sont différentes

**Causes possibles** :
- Mauvais paramètre `-ParameterSet` entre encapsulation et décapsulation
- Clé publique et clé privée ne correspondent pas
- Ciphertext corrompu

**Solution** :
```powershell
# Vérifier que les paramètres correspondent
$keys = New-KyberKeyPair -ParameterSet Kyber768
$encapsulated = Invoke-KyberEncapsulate -PublicKey $keys.PublicKey -ParameterSet Kyber768
$decapsulated = Invoke-KyberDecapsulate -Ciphertext $encapsulated.Ciphertext -PrivateKey $keys.PrivateKey -ParameterSet Kyber768
```

### Problème : Erreur de compilation

**Erreur** : `error CS8370: La fonctionnalité 'modèles récursifs' n'est pas disponible`

**Solution** : Vérifier que `LangVersion` est défini à `latest` dans les fichiers `.csproj`

---

## 📚 Ressources

- **NIST FIPS 203** : Standard ML-KEM (Module-Lattice-Based Key-Encapsulation Mechanism)
- **BouncyCastle Documentation** : https://www.bouncycastle.org/documentation.html
- **PowerShell Standard Library** : https://github.com/PowerShell/PowerShellStandard

### 📖 Documentation Technique

Pour une documentation complète sur la recherche et le développement du projet, consultez :
- **`RECHERCHE_ET_DEVELOPPEMENT.md`** : Document annexe détaillant toutes les étapes, recherches, problèmes rencontrés et solutions trouvées lors du développement du module.
- **`ANALYSE_DILITHIUM.md`** : Analyse complète de l'intégration de CRYSTALS-Dilithium (ML-DSA) dans le projet.
- **`MIGRATION_BOUNCYCASTLE.md`** : Guide de migration de BouncyCastle.NetCore vers BouncyCastle.Cryptography 2.6.2.

### 🎯 Fonctionnalités Avancées

#### Fonctions de Hachage SHA3
- **SHA3-256** : Hachage 256 bits recommandé pour ML-DSA
- **SHA3-384** : Hachage 384 bits pour sécurité renforcée
- **Pré-hachage optionnel** : Intégration dans les workflows Dilithium

#### Chiffrement Authenticated Encryption (AEAD)
- **ChaCha20-Poly1305** : Algorithme moderne et performant
- **AES-GCM** : Standard industrie pour chiffrement authentifié
- **Authentification intégrée** : Garantit l'intégrité et l'authenticité des données

#### Conformité Standards PKI
- **PKCS#8** : Export/import de clés privées en format standard (DER/PEM)
- **X.509** : Création et vérification de certificats auto-signés avec clés post-quantiques
- **CMS** : Support des enveloppes chiffrées (signatures en développement)

---

## 📝 Licence

Ce module utilise BouncyCastle.NetCore qui est sous licence MIT.

---

## 👥 Contribution

Pour contribuer au projet :
1. Fork le repository
2. Créer une branche pour votre fonctionnalité
3. Faire vos modifications
4. Tester avec `Test.ps1`
5. Créer une pull request

---

## 📧 Support

Pour toute question ou problème, consultez :
- La documentation BouncyCastle
- Les exemples dans `Examples.ps1`
- Les tests dans `Test.ps1`

---

**Version** : 1.1.0  
**Dernière mise à jour** : Décembre 2025

---

## 📖 Guide d'utilisation

### Communications distantes (KyberDaemon + KyberCLI)
1. **S'assurer que le service est démarré** (voir section Installation).
2. **Utiliser KyberCLI directement** :
   ```powershell
   dotnet KyberCLI.dll connect --host 192.168.1.10 --user alice --password 0x09AF... --command "hostname"
   dotnet KyberCLI.dll connect --host 192.168.1.10 --user bob --key C:\secrets\bob.key
   ```
3. **Depuis PowerShell** avec la cmdlet `Invoke-SecureCommand` (wrapper KyberCLI) :
   ```powershell
   Invoke-SecureCommand \
       -ServerHost 192.168.1.10 \
       -Port 8443 \
       -Username alice \
       -PasswordHex 0x09AF... \
       -Command "Get-Process | Select-Object -First 5 Name,Id"
   ```
   Paramètres supplémentaires :
   - `-KeyPath` : clé privée Dilithium (base64) pour signature
   - `-CliPath` : binaire/dll KyberCLI personnalisé
   - `-RebuildCli` : force `dotnet publish` avant exécution

> ⚠️ `Start-SecureServer`, `Connect-SecureClient`, `Send/Receive-SecureMessage` sont conservés pour compatibilité mais renvoient désormais une erreur explicite. Utilisez KyberDaemon/KyberCLI pour toute communication réseau.

### Communication

| Cmdlet | Statut | Description |
|--------|--------|-------------|
| `Invoke-SecureCommand` | ✅ Actif | Exécute une commande distante via KyberCLI (KyberDaemon requis) |
| `Start-SecureServer` | ⚠️ Obsolète | Ancienne implémentation serveur (renvoie une erreur guidant vers KyberDaemon) |
| `Connect-SecureClient` | ⚠️ Obsolète | Ancien client PowerShell (renvoie une erreur guidant vers KyberCLI) |
| `Send-SecureMessage` | ⚠️ Obsolète | Ancienne commande d'envoi (renvoie une erreur guidant vers KyberCLI) |
| `Receive-SecureMessage` | ⚠️ Obsolète | Ancienne commande de réception (renvoie une erreur guidant vers KyberCLI) |

---

## 📖 Outils d'administration

- `KyberCLI keys list --config /etc/kyberd/kyberd.conf` : affiche les versions de clés stockées (répertoires `session.vN.*`)
- `KyberCLI keys rotate --name session --days 90` : force la rotation immédiate (respecte la passphrase du `kyberd.conf`)
- `KyberCLI keys export --output metadata.json` : extrait les métadonnées (versions, modes de chiffrement)
- Script PowerShell dédié (`KyberDaemon/tools/Invoke-KyberKeyRotation.ps1`) enveloppant `KyberCLI keys` pour Windows / PowerShell Core (`-Action List|Rotate|Export`, `-Config`, `-Name`, `-Days`, `-CliPath`...)

### Tests & Qualité
- Tests unitaires/crypto historiques (scripts PowerShell dans `KyberModule/`)
- Tests d'intégration end-to-end (`dotnet test tests/KyberIntegrationTests`) : démarre un daemon éphémère, exécute KyberCLI et injecte du trafic corrompu pour valider la robustesse réseau
- Fuzzer réseau simple (`NetworkFuzzer.SendRandomHandshakeAsync`) utilisable depuis les tests ou un harness personnalisé pour enrichir votre campagne fuzz

### Benchmarks
- Projet `tests/KyberBenchmarks` (BenchmarkDotNet)
  - `CommandLatencyBenchmark` mesure la latence moyenne d'une commande `echo`
  - `HandshakeBenchmark` quantifie le coût handshake/fermeture d'une session complète
  - `ConcurrentCommandsBenchmark` ouvre plusieurs clients en parallèle (`Params(2,4,8)`) pour évaluer la tenue aux connexions simultanées
- Exécution : `dotnet run -c Release -p tests/KyberBenchmarks`
- Résultats générés (fichier markdown/csv) dans `BenchmarkDotNet.Artifacts`

---

## 📖 Guide d'utilisation

### Scénario 1 : Génération de clés

```powershell
# Générer une paire de clés avec Kyber768 (par défaut)
$keys = New-KyberKeyPair

# Spécifier le niveau de sécurité
$keys512 = New-KyberKeyPair -ParameterSet Kyber512
$keys768 = New-KyberKeyPair -ParameterSet Kyber768
$keys1024 = New-KyberKeyPair -ParameterSet Kyber1024

# Accéder aux clés
Write-Host "Clé publique (hex): $($keys.PublicKeyHex)"
Write-Host "Clé privée (hex): $($keys.PrivateKeyHex)"
```

### Scénario 2 : Échange de clés (Alice et Bob)

```powershell
# Alice génère sa paire de clés
$aliceKeys = New-KyberKeyPair -ParameterSet Kyber768

# Bob utilise la clé publique d'Alice pour créer une clé partagée
$bobEncapsulated = Invoke-KyberEncapsulate -PublicKey $aliceKeys.PublicKey

# Bob envoie le ciphertext à Alice
# $ciphertext = $bobEncapsulated.Ciphertext

# Alice décapsule pour obtenir la même clé partagée
$aliceSharedSecret = Invoke-KyberDecapsulate -Ciphertext $bobEncapsulated.Ciphertext -PrivateKey $aliceKeys.PrivateKey

# Vérifier que les clés correspondent
$match = $bobEncapsulated.SharedSecret -eq $aliceSharedSecret.SharedSecret
```

### Scénario 3 : Utilisation avec des chaînes hexadécimales

```powershell
# Générer des clés
$keys = New-KyberKeyPair

# Utiliser directement les chaînes hex
$encapsulated = Invoke-KyberEncapsulate -PublicKey $keys.PublicKeyHex

# Décapsuler avec les chaînes hex
$sharedSecret = Invoke-KyberDecapsulate -Ciphertext $encapsulated.CiphertextHex -PrivateKey $keys.PrivateKeyHex
```

### Scénario 4 : Sauvegarder et charger des clés

#### Format Text (recommandé pour sauvegarder les paires de clés - sécurisé)
```powershell
# Générer et sauvegarder
$keys = New-KyberKeyPair
Export-KyberKeyPair -KeyPair $keys -Path "keys.txt" -Format Text

# Charger plus tard
$keys = Import-KyberKeyPair -Path "keys.txt"

# Utiliser
$encapsulated = Invoke-KyberEncapsulate -PublicKey $keys.PublicKey
$sharedSecret = Invoke-KyberDecapsulate -Ciphertext $encapsulated.Ciphertext -PrivateKey $keys.PrivateKey
```

#### Format Base64/PEM (compatible avec SSH et autres protocoles)
```powershell
# Exporter en format PEM
$keys = New-KyberKeyPair
Export-KyberKeyPair -KeyPair $keys -Path "keys.pem" -Format Base64

# Exporter uniquement la clé publique (pour la partager)
Export-KyberPublicKey -PublicKey $keys.PublicKey -Path "public_key.pem" -Format Base64

# Importer
$keys = Import-KyberKeyPair -Path "keys.pem"
$publicKey = Import-KyberPublicKey -Path "public_key.pem"
```

### Scénario 5 : Protocole type SSH (serveur/client)

```powershell
# === Côté SERVEUR ===
# 1. Générer les clés du serveur
$serverKeys = New-KyberKeyPair -ParameterSet Kyber768

# 2. Sauvegarder la clé privée (sécurisée)
Export-KyberKeyPair -KeyPair $serverKeys -Path "server_private.txt" -Format Text

# 3. Partager la clé publique avec les clients
Export-KyberPublicKey -PublicKey $serverKeys.PublicKey -Path "server_public.pem" -Format Base64

# 4. Quand un client se connecte, décapsuler la clé partagée
$serverKeys = Import-KyberKeyPair -Path "server_private.txt"
$sharedSecret = Invoke-KyberDecapsulate -Ciphertext $clientCiphertext -PrivateKey $serverKeys.PrivateKey

# === Côté CLIENT ===
# 1. Récupérer la clé publique du serveur
$serverPublicKey = Import-KyberPublicKey -Path "server_public.pem"

# 2. Encapsuler une clé partagée
$encapsulated = Invoke-KyberEncapsulate -PublicKey $serverPublicKey -ParameterSet Kyber768

# 3. Envoyer le ciphertext au serveur
# $encapsulated.Ciphertext

# 4. Utiliser la clé partagée pour la communication
# $encapsulated.SharedSecret
```

### Scénario 6 : Signature numérique avec Dilithium (100% Post-Quantum) ⭐

```powershell
# Générer une paire de clés Dilithium (ML-DSA)
$signingKeys = New-DilithiumKeyPair -ParameterSet Dilithium3

# Signer un message
$message = "Important message"
$messageBytes = [System.Text.Encoding]::UTF8.GetBytes($message)
$signature = Invoke-DilithiumSign -Data $messageBytes -PrivateKey $signingKeys.PrivateKey -ParameterSet Dilithium3

# Vérifier la signature
$isValid = Test-DilithiumSignature -Data $messageBytes -Signature $signature.Signature -PublicKey $signingKeys.PublicKey -ParameterSet Dilithium3

# Sauvegarder les clés
Export-DilithiumKeyPair -KeyPair $signingKeys -Path "signing_keys.txt" -Format Text
```

### Scénario 7 : Signature numérique avec Ed25519 (Compatibilité)

```powershell
# Générer une paire de clés Ed25519
$signingKeys = New-Ed25519KeyPair

# Signer un message
$message = "Important message"
$signature = Invoke-Ed25519Sign -Data $message -PrivateKey $signingKeys.PrivateKey

# Vérifier la signature
$isValid = Test-Ed25519Signature -Data $message -Signature $signature.Signature -PublicKey $signingKeys.PublicKey

# Sauvegarder les clés
Export-Ed25519KeyPair -KeyPair $signingKeys -Path "signing_keys.txt" -Format Text
```

### Scénario 8 : Protocole sécurisé 100% Post-Quantum : Kyber + Dilithium ⭐

Ce scénario combine les deux algorithmes post-quantiques pour créer un protocole sécurisé 100% résistant aux ordinateurs quantiques :
- **Kyber (ML-KEM)** : Pour l'échange de clés résistant aux ordinateurs quantiques
- **Dilithium (ML-DSA)** : Pour l'authentification et l'intégrité des données (post-quantique)

```powershell
# 1. Générer les clés post-quantiques
$kyberKeys = New-KyberKeyPair -ParameterSet Kyber768
$dilithiumKeys = New-DilithiumKeyPair -ParameterSet Dilithium3

# 2. Encapsuler une clé partagée avec Kyber
$encapsulated = Invoke-KyberEncapsulate -PublicKey $kyberKeys.PublicKey -ParameterSet Kyber768

# 3. Signer la clé partagée avec Dilithium
$signature = Invoke-DilithiumSign -Data $encapsulated.SharedSecret -PrivateKey $dilithiumKeys.PrivateKey -ParameterSet Dilithium3

# 4. Vérifier la signature
$isValid = Test-DilithiumSignature -Data $encapsulated.SharedSecret -Signature $signature.Signature -PublicKey $dilithiumKeys.PublicKey -ParameterSet Dilithium3

# 5. Décapsuler la clé partagée
$sharedSecret = Invoke-KyberDecapsulate -Ciphertext $encapsulated.Ciphertext -PrivateKey $kyberKeys.PrivateKey -ParameterSet Kyber768
```
**Avantages de cette combinaison** :
- 🔐 **Kyber (ML-KEM)** : Échange de clés résistant aux ordinateurs quantiques
- ✍️ **Dilithium (ML-DSA)** : Authentification et intégrité post-quantiques
- 🔑 **Clé partagée** : 32 bytes sécurisée et authentifiée
- 🛡️ **100% Post-Quantum** : Résistant aux attaques classiques et quantiques
**Scripts d'exemples** :
- `TestKyberDilithium.ps1` : Test rapide de l'intégration
- `KyberDilithiumCompleteExample.ps1` : Scénario serveur/client complet

### Scénario 9 : Hachage SHA3 avec ML-DSA

```powershell
# Calculer un hachage SHA3-256
$data = "Données à hacher"
$hash256 = Get-SHA3Hash -Data $data -Variant SHA3_256

# Calculer un hachage SHA3-384
$hash384 = Get-SHA3Hash -Data $data -Variant SHA3_384

# Signer avec Dilithium en utilisant SHA3 pré-hachage (recommandé)
$keys = New-DilithiumKeyPair -ParameterSet Dilithium3
$signature = Invoke-DilithiumSign -Data $dataBytes -PrivateKey $keys.PrivateKey -PreHash -SHA3Variant SHA3_256
```

### Scénario 10 : Chiffrement Authenticated Encryption

#### ChaCha20-Poly1305
```powershell
# Chiffrer avec ChaCha20-Poly1305
$plaintext = "Message secret"
$key = New-Object byte[] 32
# Générer une clé (ou utiliser une clé partagée Kyber)
$encrypted = Protect-WithChaCha20Poly1305 -Data $plaintext -Key $key

# Déchiffrer
$decrypted = Unprotect-ChaCha20Poly1305 -EncryptedData $encrypted.EncryptedData -Key $key -Nonce $encrypted.Nonce -Tag $encrypted.Tag
```
#### AES-GCM
```powershell
# Chiffrer avec AES-GCM
$plaintext = "Message secret"
$key = New-Object byte[] 32
$encrypted = Protect-WithAESGCM -Data $plaintext -Key $key

# Déchiffrer
$decrypted = Unprotect-AESGCM -EncryptedData $encrypted.EncryptedData -Key $key -Nonce $encrypted.Nonce -Tag $encrypted.Tag
```

### Scénario 11 : Export/Import PKCS#8 et Certificats X.509

```powershell
# Exporter une clé privée Dilithium en format PKCS#8
$keys = New-DilithiumKeyPair -ParameterSet Dilithium3
Export-PrivateKeyPKCS8 -PrivateKey $keys.PrivateKey -Path "dilithium_key.pem" -Format PEM -KeyType Dilithium -ParameterSet Dilithium3

# Importer une clé privée PKCS#8
$imported = Import-PrivateKeyPKCS8 -Path "dilithium_key.pem" -KeyType Dilithium

# Créer un certificat X.509 auto-signé avec Dilithium
$cert = New-X509Certificate -SubjectName "CN=Test Post-Quantum Certificate" -PrivateKey $keys.PrivateKey -ParameterSet Dilithium3 -Path "certificate.pem" -Format PEM

# Vérifier un certificat X.509
$isValid = Test-X509Certificate -Path "certificate.pem"
```

### Scénario 12 : Configuration TLS 1.3 Post-Quantum Hybride ⭐

Ce scénario permet de créer des configurations TLS 1.3 hybrides combinant des algorithmes classiques (ECDSA, RSA) avec des algorithmes post-quantiques (Kyber, Dilithium) pour une transition progressive vers la cryptographie post-quantique.

```powershell
# Générer une configuration TLS hybride par défaut (ECDSA-P256 + Kyber768 + Dilithium3)
$hybridConfig = New-TLS13HybridConfig

# Obtenir le nom du cipher suite
$cipherSuite = Get-TLS13HybridCipherSuite -Config $hybridConfig
Write-Host "Cipher Suite: $cipherSuite"  # TLS13-ECDSA-P256+ML-KEM-768+ML-DSA-65

# Générer une configuration avec RSA
$rsaConfig = New-TLS13HybridConfig -ClassicalAlgorithm RSA_2048 -PostQuantumKemAlgorithm Kyber768 -PostQuantumSigAlgorithm Dilithium3

# Générer une configuration haute sécurité
$highSecConfig = New-TLS13HybridConfig -ClassicalAlgorithm ECDSA_P384 -PostQuantumKemAlgorithm Kyber1024 -PostQuantumSigAlgorithm Dilithium5

# Exporter la configuration pour intégration TLS
Export-TLS13HybridConfig -Config $hybridConfig -Path "tls13_hybrid_config.txt" -Force
```

**Algorithmes classiques supportés** :
- `ECDSA_P256` : ECDSA avec courbe P-256 (NIST)
- `ECDSA_P384` : ECDSA avec courbe P-384 (NIST)
- `RSA_2048` : RSA 2048 bits
- `RSA_3072` : RSA 3072 bits

**Algorithmes post-quantiques supportés** :
- **KEM** : Kyber512, Kyber768, Kyber1024 (ML-KEM)
- **Signature** : Dilithium2, Dilithium3, Dilithium5 (ML-DSA)

**Avantages de l'hybridation** :
- 🔐 Sécurité classique éprouvée (compatibilité avec infrastructure existante)
- 🛡️ Sécurité post-quantique (résistance aux ordinateurs quantiques)
- ✅ Transition progressive vers la cryptographie post-quantique
- 🔄 Compatibilité avec bibliothèques TLS supportant les extensions post-quantiques

**Exemple complet** : Voir `TLS13HybridExamples.ps1` pour des scénarios complets.

### Scénario 13 : Benchmark de Performance Haute Précision

Le script `PerformanceBenchmark.ps1` permet de mesurer avec haute précision les performances de tous les algorithmes cryptographiques du module :

```powershell
# Exécuter le benchmark complet
.\PerformanceBenchmark.ps1
```

Le script mesure :
- ✅ Temps de génération de clés (Kyber, Dilithium, Ed25519)
- ✅ Temps d'encapsulation/décapsulation (Kyber)
- ✅ Temps de signature/vérification (Dilithium, Ed25519)
- ✅ Temps de hachage (SHA3-256, SHA3-384)
- ✅ Temps de chiffrement/déchiffrement (ChaCha20-Poly1305, AES-GCM)

**Métriques affichées** :
- **Moyenne** : Temps moyen en µs et ms (4 décimales pour ms, 2 pour µs)
- **Médiane** : Temps médian (moins sensible aux valeurs aberrantes)
- **Minimum et Maximum** : Plage de variation des temps
- **Écart-type** : Mesure de la variabilité des performances
- **Statut** : SUCCESS (toutes réussies), PARTIAL (certaines échouées), FAILED (toutes échouées)
- **Itérations** : Nombre d'itérations réussies vs total

**Caractéristiques de précision** :
- **Opérations lentes** (Kyber/Dilithium) : 10 itérations
- **Opérations rapides** (Ed25519/SHA3/AEAD) : 500-1000 itérations pour précision
- **Warm-up automatique** : Exécution préalable pour éviter les effets de cache/JIT
- **Mesures en microsecondes** : Précision maximale pour opérations rapides
- **Export CSV** : Toutes les données exportables pour analyse approfondie

**Exemple de sortie** :
```
✅ Génération de clés Ed25519:
   Moyenne: 125.50 µs (0.1255 ms)
   Médiane: 124.20 µs (0.1242 ms)
   Min: 120.10 µs (0.1201 ms) | Max: 135.30 µs (0.1353 ms)
   Écart-type: 3.45 µs (0.0035 ms)
   Itérations: 1000/1000 réussies
```

**Export CSV** :
Le script génère également un export CSV avec toutes les métriques :
- Operation, Status, Average_ms, Average_us, Median_ms, Median_us, Min_ms, Min_us, Max_ms, Max_us, StdDev_ms, StdDev_us, SuccessfulIterations, TotalIterations, Errors

### Scénario 14 : Gestion Sécurisée des Clés en Mémoire ⭐

Ce scénario démontre l'utilisation de la gestion sécurisée des clés pour protéger les clés privées en mémoire.

```powershell
# Générer une paire de clés avec gestion sécurisée
$secureKeys = New-SecureKyberKeyPair -ParameterSet Kyber768

# Utiliser les clés
$publicKey = $secureKeys.PublicKey
$privateKey = $secureKeys.PrivateKey

# Encapsuler et décapsuler
$encapsulated = Invoke-KyberEncapsulate -PublicKey $publicKey -ParameterSet Kyber768
$decapsulated = Invoke-KyberDecapsulate -Ciphertext $encapsulated.Ciphertext -PrivateKey $privateKey -ParameterSet Kyber768

# Nettoyer immédiatement la clé privée après utilisation
$secureKeys.ZeroizePrivateKey()

# Vérifier que la clé est nettoyée
$isZeroized = Test-ZeroizedKey -Key $privateKey
Write-Host "Clé nettoyée: $isZeroized"  # True
```

**Nettoyage manuel avec Clear-SecureKey** :
```powershell
# Générer des clés normales
$keys = New-KyberKeyPair -ParameterSet Kyber768

# Utiliser les clés...

# Nettoyer manuellement avec une seule passe
Clear-SecureKey -Key $keys.PrivateKey -Passes 1

# Ou avec plusieurs passes pour sécurité renforcée (protection cold boot)
Clear-SecureKey -Key $keys.PrivateKey -Passes 3 -Nullify

# Vérifier le nettoyage
$isClean = Test-ZeroizedKey -Key $keys.PrivateKey
```

**Pattern try-finally pour nettoyage garanti** :
```powershell
$keys = New-KyberKeyPair -ParameterSet Kyber768
try {
    # Utiliser les clés...
    $encapsulated = Invoke-KyberEncapsulate -PublicKey $keys.PublicKey
}
finally {
    # Nettoyer dans tous les cas (même en cas d'exception)
    Clear-SecureKey -Key $keys.PrivateKey -Passes 3 | Out-Null
}
```

**Avantages** :
- 🔐 Nettoyage automatique des clés privées
- 🛡️ Protection contre les attaques de récupération de mémoire
- ✅ Nettoyage multi-passes pour sécurité renforcée
- 🔄 Pattern Dispose pour nettoyage garanti

**Exemple complet** : Voir `SecureKeyManagementExamples.ps1` pour des scénarios complets.

### Scénario 15 : Protection contre les Canaux Auxiliaires (Side-Channel Attacks) ⭐

Ce scénario démontre l'utilisation des contre-mesures contre les attaques par canaux auxiliaires (timing attacks, cache side-channel attacks).

#### Vue d'ensemble

Les attaques par canaux auxiliaires exploitent des informations indirectes (temps d'exécution, accès au cache) pour extraire des secrets cryptographiques. Le module implémente des contre-mesures robustes pour protéger contre ces attaques.

#### Cmdlets disponibles

**1. Décapsulation sécurisée Kyber** :
```powershell
# Décapsulation avec protection side-channel
$keys = New-KyberKeyPair -ParameterSet Kyber768
$encapsulated = Invoke-KyberEncapsulate -PublicKey $keys.PublicKey -ParameterSet Kyber768

# Méthode normale
$decapsulatedNormal = Invoke-KyberDecapsulate -Ciphertext $encapsulated.Ciphertext -PrivateKey $keys.PrivateKey -ParameterSet Kyber768

# Méthode sécurisée (protection side-channel)
$decapsulatedSecure = Invoke-KyberDecapsulateSecure -Ciphertext $encapsulated.Ciphertext -PrivateKey $keys.PrivateKey -ParameterSet Kyber768

# Les résultats sont identiques, mais la méthode sécurisée protège contre les attaques
```

**2. Vérification de signature sécurisée Dilithium** :
```powershell
# Génération et signature
$keys = New-DilithiumKeyPair -ParameterSet Dilithium3
$message = "Message important à signer"
$messageBytes = [System.Text.Encoding]::UTF8.GetBytes($message)
$signature = Invoke-DilithiumSign -Data $messageBytes -PrivateKey $keys.PrivateKey -ParameterSet Dilithium3

# Vérification normale
$isValidNormal = Test-DilithiumSignature -Data $messageBytes -Signature $signature.Signature -PublicKey $keys.PublicKey -ParameterSet Dilithium3

# Vérification sécurisée (protection side-channel)
$isValidSecure = Test-DilithiumSignatureSecure -Data $messageBytes -Signature $signature.Signature -PublicKey $keys.PublicKey -ParameterSet Dilithium3
```

**3. Comparaison en temps constant** :
```powershell
# Comparaison de clés partagées en temps constant
$key1 = [byte[]]::new(32)
$key2 = [byte[]]::new(32)
# ... remplir les clés ...

# Comparaison sécurisée (protège contre timing attacks)
$areEqual = Test-ConstantTimeCompare -Array1 $key1 -Array2 $key2

# Supporte aussi les chaînes hexadécimales
$hex1 = "0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF"
$hex2 = "0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF"
$areEqual = Test-ConstantTimeCompare -Array1 $hex1 -Array2 $hex2
```

#### Protections implémentées

**1. Opérations à temps constant** :
- ✅ Pas de branches conditionnelles basées sur des données secrètes
- ✅ Comparaisons qui prennent toujours le même temps
- ✅ Masquage des variations de timing

**2. Protection contre timing attacks** :
- ✅ Toutes les comparaisons utilisent `ConstantTimeEquals()`
- ✅ Pas de sortie anticipée lors des comparaisons
- ✅ Temps d'exécution indépendant du contenu des données

**3. Protection contre cache side-channel attacks** :
- ✅ Nettoyage du cache après opérations sensibles
- ✅ Accès mémoire uniformes pour masquer les patterns
- ✅ Préchargement du cache pour éviter les variations

**4. Méthodes sécurisées** :
- `DecapsulateSecure()` : Décapsulation Kyber avec protection side-channel
- `VerifySecure()` : Vérification Dilithium avec protection side-channel
- `ConstantTimeCompareSharedSecrets()` : Comparaison de clés partagées en temps constant
- `ConstantTimeCompareSignatures()` : Comparaison de signatures en temps constant

#### Exemple d'intégration complète

```powershell
# Protocole complet avec protections side-channel
$kyberKeys = New-KyberKeyPair -ParameterSet Kyber768
$dilithiumKeys = New-DilithiumKeyPair -ParameterSet Dilithium3

# Encapsulation
$encapsulated = Invoke-KyberEncapsulate -PublicKey $kyberKeys.PublicKey -ParameterSet Kyber768

# Décapsulation sécurisée
$sharedSecret = Invoke-KyberDecapsulateSecure -Ciphertext $encapsulated.Ciphertext -PrivateKey $kyberKeys.PrivateKey -ParameterSet Kyber768

# Signature de la clé partagée
$signature = Invoke-DilithiumSign -Data $sharedSecret.SharedSecret -PrivateKey $dilithiumKeys.PrivateKey -ParameterSet Dilithium3

# Vérification sécurisée
$isValid = Test-DilithiumSignatureSecure -Data $sharedSecret.SharedSecret -Signature $signature.Signature -PublicKey $dilithiumKeys.PublicKey -ParameterSet Dilithium3
```

#### Tests de validation

Un script de tests complet est disponible : `SideChannelProtectionTests.ps1`

```powershell
# Exécuter tous les tests de protection side-channel
.\SideChannelProtectionTests.ps1
```

Les tests valident :
- ✅ Comparaisons en temps constant
- ✅ Décapsulation sécurisée Kyber
- ✅ Vérification sécurisée Dilithium
- ✅ Tests de timing (vérification que les opérations prennent un temps similaire)
- ✅ Détection de différences à toutes les positions
- ✅ Intégration complète avec protections side-channel
- ✅ Support de différentes tailles de données

**Exemple complet** : Voir `CONTRE_MESURES_SIDE_CHANNEL.md` pour la documentation technique complète.

### Scénario 16 : Audit et Validation Cryptographique ⭐

Ce scénario démontre l'utilisation des outils d'audit et de validation cryptographique pour vérifier la conformité aux standards NIST et la robustesse des implémentations.

#### Vue d'ensemble

Le module fournit des outils complets pour :
- **Tests de conformité** : Validation des tailles, formats et comportements selon les standards NIST (FIPS 203, ML-DSA)
- **Tests de fuzzing** : Génération de données corrompues pour tester la robustesse
- **Tests de validation cryptographique** : Vérification des propriétés cryptographiques

#### Cmdlets disponibles

**1. Tests de conformité NIST** :
```powershell
# Tester la conformité de tous les algorithmes
$results = Test-CryptographicConformance -Algorithm All

# Tester uniquement Kyber
$kyberResults = Test-CryptographicConformance -Algorithm Kyber -KyberParameterSet Kyber768

# Tester uniquement Dilithium
$dilithiumResults = Test-CryptographicConformance -Algorithm Dilithium -DilithiumParameterSet Dilithium3

# Afficher uniquement les tests échoués
$failed = Test-CryptographicConformance -Algorithm All -ShowOnlyFailed
```

**2. Tests de fuzzing** :
```powershell
# Exécuter des tests de fuzzing sur tous les algorithmes
$fuzzingResults = Invoke-CryptographicFuzzing -Algorithm All -Iterations 100

# Fuzzing Kyber uniquement
$kyberFuzzing = Invoke-CryptographicFuzzing -Algorithm Kyber -KyberParameterSet Kyber768 -Iterations 50

# Afficher uniquement les problèmes (crashes ou échecs)
$issues = Invoke-CryptographicFuzzing -Algorithm All -ShowOnlyIssues
```

**3. Script d'audit complet** :
```powershell
# Exécuter tous les tests d'audit
.\CryptographicAuditTests.ps1
```

#### Tests de conformité

Les tests de conformité valident :

**1. Tailles de clés et ciphertexts** :
- ✅ Clés publiques/privées Kyber selon NIST FIPS 203
- ✅ Ciphertexts et clés partagées Kyber
- ✅ Clés publiques/privées Dilithium selon NIST ML-DSA
- ✅ Signatures Dilithium
- ✅ Clés et signatures Ed25519 selon RFC 8032

**2. Propriétés cryptographiques** :
- ✅ Non-déterminisme des générations de clés
- ✅ Correspondance des clés partagées après encapsulation/décapsulation
- ✅ Correspondance des signatures avec les clés publiques
- ✅ Rejet correct de données invalides

**3. Formats et sérialisation** :
- ✅ Formats de clés conformes
- ✅ Reconstruction correcte des clés privées

#### Tests de fuzzing

Les tests de fuzzing génèrent et testent :

**1. Données corrompues** :
- Corruption de bytes individuels
- Corruption multiple de bytes
- Tailles invalides (trop courtes, trop longues)
- Patterns suspects (tous zéros, tous FF, etc.)

**2. Ciphertexts corrompus** :
- Test de décapsulation avec ciphertexts invalides
- Vérification du rejet correct des données corrompues

**3. Signatures corrompues** :
- Test de vérification avec signatures invalides
- Vérification du rejet correct des signatures corrompues

**4. Clés corrompues** :
- Test avec clés publiques/privées corrompues
- Vérification de la robustesse face aux clés invalides

#### Exemple d'utilisation complète

```powershell
# 1. Tests de conformité
Write-Host "=== Tests de Conformité ===" -ForegroundColor Cyan
$conformanceResults = Test-CryptographicConformance -Algorithm All

foreach ($result in $conformanceResults) {
    if ($result.Passed) {
        Write-Host "✅ $($result.TestName)" -ForegroundColor Green
    } else {
        Write-Host "❌ $($result.TestName)" -ForegroundColor Red
        foreach ($error in $result.Errors) {
            Write-Host "   - $error" -ForegroundColor Yellow
        }
    }
}

# 2. Tests de fuzzing
Write-Host "`n=== Tests de Fuzzing ===" -ForegroundColor Cyan
$fuzzingResults = Invoke-CryptographicFuzzing -Algorithm All -Iterations 50

foreach ($result in $fuzzingResults) {
    Write-Host "$($result.TestName):" -ForegroundColor Cyan
    Write-Host "  Total: $($result.TotalTests) | Passed: $($result.Passed) | Failed: $($result.Failed) | Crashed: $($result.Crashed)" -ForegroundColor Gray
    if ($result.Crashed -gt 0) {
        Write-Host "  ⚠️  $($result.Crashed) crash(es) détecté(s)" -ForegroundColor Yellow
    }
}

# 3. Script d'audit complet
.\CryptographicAuditTests.ps1
```

#### Résultats attendus

**Tests de conformité** :
- ✅ Tous les tests doivent passer
- ✅ Toutes les tailles doivent correspondre aux spécifications NIST
- ✅ Toutes les propriétés cryptographiques doivent être validées

**Tests de fuzzing** :
- ✅ Les données corrompues doivent être rejetées correctement
- ✅ Aucun crash ne doit se produire (exceptions gérées)
- ⚠️  Les "Failed" sont attendus car ils indiquent le rejet correct de données invalides

#### Tests de validation cryptographique

Le script `CryptographicAuditTests.ps1` inclut également :

**1. Tests d'intégrité** :
- Vérification que les clés partagées sont identiques après encapsulation/décapsulation

**2. Tests d'authentification** :
- Vérification que les signatures sont valides avec les clés publiques correspondantes

**3. Tests de rejet** :
- Vérification que les données corrompues sont correctement rejetées

**4. Tests de non-déterminisme** :
- Vérification que les clés générées sont différentes à chaque génération

**5. Tests de stress** :
- Génération multiple de clés
- Encapsulation/décapsulation multiple
- Mesure des performances sous charge

#### Interprétation des résultats

**Tests de conformité** :
- `Passed = true` : Le test est conforme aux standards
- `Passed = false` : Le test a échoué, vérifier les erreurs dans `Errors`

**Tests de fuzzing** :
- `Crashed = 0` : Aucune exception non gérée (bon signe)
- `Failed > 0` : Normal, indique le rejet correct de données invalides
- `Crashed > 0` : Problème potentiel, exceptions non gérées détectées

**Exemple complet** : Voir `CryptographicAuditTests.ps1` pour un script complet d'audit et de validation.

### Scénario 17 : Protocole sécurisé combinant Kyber + Ed25519

Ce scénario combine les deux algorithmes pour créer un protocole sécurisé complet :
- **Kyber (ML-KEM)** : Pour l'échange de clés résistant aux ordinateurs quantiques
- **Ed25519** : Pour l'authentification et l'intégrité des données

```powershell
# === Étape 1: Générer les clés ===
# Générer des clés Kyber pour l'échange de clés
$kyberKeys = New-KyberKeyPair -ParameterSet Kyber768

# Générer des clés Ed25519 pour la signature
$ed25519Keys = New-Ed25519KeyPair

# === Étape 2: Encapsuler une clé partagée avec Kyber ===
$encapsulated = Invoke-KyberEncapsulate -PublicKey $kyberKeys.PublicKey -ParameterSet Kyber768

# === Étape 3: Signer la clé partagée avec Ed25519 ===
$signature = Invoke-Ed25519Sign -Data $encapsulated.SharedSecret -PrivateKey $ed25519Keys.PrivateKey

# === Étape 4: Vérifier la signature ===
$isValid = Test-Ed25519Signature -Data $encapsulated.SharedSecret -Signature $signature.Signature -PublicKey $ed25519Keys.PublicKey

# === Étape 5: Décapsuler et vérifier ===
$decapsulated = Invoke-KyberDecapsulate -Ciphertext $encapsulated.Ciphertext -PrivateKey $kyberKeys.PrivateKey

# Vérifier que les clés partagées correspondent
if ($encapsulated.SharedSecretHex -eq $decapsulated.SharedSecretHex) {
    Write-Host "✓ Clés partagées identiques !"
}

# Vérifier la signature avec la clé décapsulée
$isValidDecapsulated = Test-Ed25519Signature -Data $decapsulated.SharedSecret -Signature $signature.Signature -PublicKey $ed25519Keys.PublicKey
```

**Exemple complet** : Voir `TestKyberEd25519.ps1` pour un scénario complet avec export/import de clés.

---

## 🔧 Détails techniques

### Tailles des clés et ciphertexts

#### Kyber (ML-KEM)

| Paramètre | Clé publique | Clé privée | Ciphertext | Clé partagée |
|-----------|-------------|------------|------------|--------------|
| Kyber512  | 800 bytes   | 1632 bytes | 768 bytes  | 32 bytes     |
| Kyber768  | 1184 bytes  | 2400 bytes | 1088 bytes | 32 bytes     |
| Kyber1024 | 1568 bytes  | 3168 bytes | 1568 bytes | 32 bytes     |

#### Dilithium (ML-DSA) - 100% Post-Quantum ⭐

| Paramètre | Clé publique | Clé privée | Signature | Niveau NIST |
|-----------|--------------|------------|-----------|-------------|
| Dilithium2 (ML-DSA-44) | ~1312 bytes | ~2560 bytes | ~2420 bytes | Niveau 1 |
| Dilithium3 (ML-DSA-65) | ~1952 bytes | ~4032 bytes | ~3309 bytes | Niveau 2 |
| Dilithium5 (ML-DSA-87) | ~2592 bytes | ~4864 bytes | ~4627 bytes | Niveau 3 |

#### Ed25519 (Compatibilité)

| Type | Taille |
|------|--------|
| Clé publique | 32 bytes |
| Clé privée | 32 bytes |
| Signature | 64 bytes |

**Remarque** : Ed25519 est un algorithme de signature numérique classique (non post-quantique) mais très performant et largement utilisé. Il est recommandé pour l'authentification et l'intégrité des données, tandis que Kyber est utilisé pour l'échange de clés résistant aux ordinateurs quantiques.

### Sérialisation de la clé privée

Le module utilise un format de sérialisation personnalisé pour éviter les problèmes avec `GetEncoded()` de BouncyCastle :

```
Format: [Magic(4)] [sLength(4)]s [hpkLength(4)]hpk [nonceLength(4)]nonce [tLength(4)]t [rhoLength(4)]rho

Magic = 0x4B594245 ("KYBE" en ASCII)
```

**Avantages** :
- Format fiable et reproductible
- Reconstruction correcte des composants
- Compatible avec tous les paramètres Kyber

### Architecture de sécurité

- **Kyber512** : Niveau de sécurité équivalent à AES-128 (résistant aux attaques classiques et quantiques)
- **Kyber768** : Niveau de sécurité équivalent à AES-192 (recommandé pour la plupart des applications)
- **Kyber1024** : Niveau de sécurité équivalent à AES-256 (niveau de sécurité maximal)

### Gestion de la mémoire

- Les clés sont stockées en mémoire comme `byte[]`
- Les conversions hexadécimales sont effectuées à la demande
- **Gestion sécurisée disponible** : Utilisez `New-Secure*KeyPair` pour un nettoyage automatique des clés privées
- **Nettoyage manuel** : Utilisez `Clear-SecureKey` pour nettoyer manuellement les clés après utilisation
- **Vérification** : Utilisez `Test-ZeroizedKey` pour vérifier si une clé a été nettoyée

#### Gestion Sécurisée des Clés ⭐

Le module fournit des cmdlets et classes pour la gestion sécurisée des clés en mémoire :

**Cmdlets sécurisés** :
- `New-SecureKyberKeyPair` : Génère une paire de clés Kyber avec nettoyage automatique
- `New-SecureDilithiumKeyPair` : Génère une paire de clés Dilithium avec nettoyage automatique
- `New-SecureEd25519KeyPair` : Génère une paire de clés Ed25519 avec nettoyage automatique
- `Clear-SecureKey` : Nettoie manuellement une clé en mémoire (zeroization)
- `Test-ZeroizedKey` : Vérifie si une clé a été nettoyée

**Caractéristiques** :
- ✅ Nettoyage automatique lors de la destruction de l'objet (IDisposable)
- ✅ Nettoyage multi-passes (protection contre cold boot attacks)
- ✅ Thread-safe avec verrous
- ✅ Finalizer pour nettoyage même si Dispose() n'est pas appelé

**Exemple** :
```powershell
# Génération sécurisée
$secureKeys = New-SecureKyberKeyPair -ParameterSet Kyber768

# Utiliser les clés
$publicKey = $secureKeys.PublicKey
$privateKey = $secureKeys.PrivateKey

# Nettoyer immédiatement après utilisation
$secureKeys.ZeroizePrivateKey()

# Ou laisser le nettoyage automatique (garbage collector)
```

**Voir** : `GESTION_SECURISEE_CLES.md` pour la documentation complète.

#### Protection contre les Canaux Auxiliaires ⭐

Le module fournit des contre-mesures contre les attaques par canaux auxiliaires :

**Classes de protection** :
- `ConstantTimeOperations` : Opérations à temps constant (protection timing attacks)
- `SideChannelProtection` : Protection contre cache side-channel attacks

**Cmdlets sécurisés** :
- `Invoke-KyberDecapsulateSecure` : Décapsulation avec protection side-channel
- `Test-DilithiumSignatureSecure` : Vérification de signature avec protection side-channel
- `Test-ConstantTimeCompare` : Comparaison en temps constant

**Caractéristiques** :
- ✅ Opérations à temps constant (pas de branches conditionnelles basées sur données secrètes)
- ✅ Nettoyage du cache après opérations sensibles
- ✅ Accès mémoire uniformes pour masquer les patterns
- ✅ Protection contre timing attacks et cache side-channel attacks

**Exemple** :
```powershell
# Décapsulation sécurisée
$sharedSecret = Invoke-KyberDecapsulateSecure -Ciphertext $ciphertext -PrivateKey $privateKey

# Vérification sécurisée
$isValid = Test-DilithiumSignatureSecure -Data $data -Signature $signature -PublicKey $publicKey

# Comparaison en temps constant
$areEqual = Test-ConstantTimeCompare -Array1 $key1 -Array2 $key2
```

**Voir** : `CONTRE_MESURES_SIDE_CHANNEL.md` pour la documentation complète.

---

## 🐛 Dépannage

### Problème : Module non trouvé

**Erreur** : `The specified module 'KyberModule' was not loaded`

**Solution** :
```powershell
# Vérifier que le module est dans le bon répertoire
Test-Path ".\KyberModule.psd1"

# Importer avec le chemin complet
Import-Module ".\KyberModule.psd1" -Force
```

### Problème : DLL manquante

**Erreur** : `Could not load file or assembly 'BouncyCastle.Crypto'`
**Solution** :
1. Vérifier que `BouncyCastle.Crypto.dll` est dans le même répertoire que `KyberModule.dll`
2. Vérifier que la version est 2.2.1 ou compatible

### Problème : Clés partagées ne correspondent pas

**Symptôme** : Les clés partagées après encapsulation/décapsulation sont différentes

**Causes possibles** :
- Mauvais paramètre `-ParameterSet` entre encapsulation et décapsulation
- Clé publique et clé privée ne correspondent pas
- Ciphertext corrompu

**Solution** :
```powershell
# Vérifier que les paramètres correspondent
$keys = New-KyberKeyPair -ParameterSet Kyber768
$encapsulated = Invoke-KyberEncapsulate -PublicKey $keys.PublicKey -ParameterSet Kyber768
$decapsulated = Invoke-KyberDecapsulate -Ciphertext $encapsulated.Ciphertext -PrivateKey $keys.PrivateKey -ParameterSet Kyber768
```

### Problème : Erreur de compilation

**Erreur** : `error CS8370: La fonctionnalité 'modèles récursifs' n'est pas disponible`

**Solution** : Vérifier que `LangVersion` est défini à `latest` dans les fichiers `.csproj`

---

## 📚 Ressources

- **NIST FIPS 203** : Standard ML-KEM (Module-Lattice-Based Key-Encapsulation Mechanism)
- **BouncyCastle Documentation** : https://www.bouncycastle.org/documentation.html
- **PowerShell Standard Library** : https://github.com/PowerShell/PowerShellStandard

### 📖 Documentation Technique

Pour une documentation complète sur la recherche et le développement du projet, consultez :
- **`RECHERCHE_ET_DEVELOPPEMENT.md`** : Document annexe détaillant toutes les étapes, recherches, problèmes rencontrés et solutions trouvées lors du développement du module.
- **`ANALYSE_DILITHIUM.md`** : Analyse complète de l'intégration de CRYSTALS-Dilithium (ML-DSA) dans le projet.
- **`MIGRATION_BOUNCYCASTLE.md`** : Guide de migration de BouncyCastle.NetCore vers BouncyCastle.Cryptography 2.6.2.

### 🎯 Fonctionnalités Avancées

#### Fonctions de Hachage SHA3
- **SHA3-256** : Hachage 256 bits recommandé pour ML-DSA
- **SHA3-384** : Hachage 384 bits pour sécurité renforcée
- **Pré-hachage optionnel** : Intégration dans les workflows Dilithium

#### Chiffrement Authenticated Encryption (AEAD)
- **ChaCha20-Poly1305** : Algorithme moderne et performant
- **AES-GCM** : Standard industrie pour chiffrement authentifié
- **Authentification intégrée** : Garantit l'intégrité et l'authenticité des données

#### Conformité Standards PKI
- **PKCS#8** : Export/import de clés privées en format standard (DER/PEM)
- **X.509** : Création et vérification de certificats auto-signés avec clés post-quantiques
- **CMS** : Support des enveloppes chiffrées (signatures en développement)

---

## 📝 Licence

Ce module utilise BouncyCastle.NetCore qui est sous licence MIT.

---

## 👥 Contribution

Pour contribuer au projet :
1. Fork le repository
2. Créer une branche pour votre fonctionnalité
3. Faire vos modifications
4. Tester avec `Test.ps1`
5. Créer une pull request

---

## 📧 Support

Pour toute question ou problème, consultez :
- La documentation BouncyCastle
- Les exemples dans `Examples.ps1`
- Les tests dans `Test.ps1`

---

**Version** : 1.1.0  
**Dernière mise à jour** : Décembre 2025

---

## 📖 Guide d'utilisation

### Communications distantes (KyberDaemon + KyberCLI)
1. **S'assurer que le service est démarré** (voir section Installation).
2. **Utiliser KyberCLI directement** :
   ```powershell
   dotnet KyberCLI.dll connect --host 192.168.1.10 --user alice --password 0x09AF... --command "hostname"
   dotnet KyberCLI.dll connect --host 192.168.1.10 --user bob --key C:\secrets\bob.key
   ```
3. **Depuis PowerShell** avec la cmdlet `Invoke-SecureCommand` (wrapper KyberCLI) :
   ```powershell
   Invoke-SecureCommand \
       -ServerHost 192.168.1.10 \
       -Port 8443 \
       -Username alice \
       -PasswordHex 0x09AF... \
       -Command "Get-Process | Select-Object -First 5 Name,Id"
   ```
   Paramètres supplémentaires :
   - `-KeyPath` : clé privée Dilithium (base64) pour signature
   - `-CliPath` : binaire/dll KyberCLI personnalisé
   - `-RebuildCli` : force `dotnet publish` avant exécution
> ⚠️ `Start-SecureServer`, `Connect-SecureClient`, `Send/Receive-SecureMessage` sont conservés pour compatibilité mais renvoient désormais une erreur explicite. Utilisez KyberDaemon/KyberCLI pour toute communication réseau.

### Communication
| Cmdlet | Statut | Description |
|--------|--------|-------------|
| `Invoke-SecureCommand` | ✅ Actif | Exécute une commande distante via KyberCLI (KyberDaemon requis) |
| `Start-SecureServer` | ⚠️ Obsolète | Ancienne implémentation serveur (renvoie une erreur guidant vers KyberDaemon) |
| `Connect-SecureClient` | ⚠️ Obsolète | Ancien client PowerShell (renvoie une erreur guidant vers KyberCLI) |
| `Send-SecureMessage` | ⚠️ Obsolète | Ancienne commande d'envoi (renvoie une erreur guidant vers KyberCLI) |
| `Receive-SecureMessage` | ⚠️ Obsolète | Ancienne commande de réception (renvoie une erreur guidant vers KyberCLI) |

---

## 📖 Outils d'administration

- `KyberCLI keys list --config /etc/kyberd/kyberd.conf` : affiche les versions de clés stockées (répertoires `session.vN.*`)
- `KyberCLI keys rotate --name session --days 90` : force la rotation immédiate (respecte la passphrase du `kyberd.conf`)
- `KyberCLI keys export --output metadata.json` : extrait les métadonnées (versions, modes de chiffrement)
- Script PowerShell dédié (`KyberDaemon/tools/Invoke-KyberKeyRotation.ps1`) enveloppant `KyberCLI keys` pour Windows / PowerShell Core (`-Action List|Rotate|Export`, `-Config`, `-Name`, `-Days`, `-CliPath`...)

### Tests & Qualité
- Tests unitaires/crypto historiques (scripts PowerShell dans `KyberModule/`)
- Tests d'intégration end-to-end (`dotnet test tests/KyberIntegrationTests`) : démarre un daemon éphémère, exécute KyberCLI et injecte du trafic corrompu pour valider la robustesse réseau
- Fuzzer réseau simple (`NetworkFuzzer.SendRandomHandshakeAsync`) utilisable depuis les tests ou un harness personnalisé pour enrichir votre campagne fuzz

### Benchmarks
- Projet `tests/KyberBenchmarks` (BenchmarkDotNet)
  - `CommandLatencyBenchmark` mesure la latence moyenne d'une commande `echo`
  - `HandshakeBenchmark` quantifie le coût handshake/fermeture d'une session complète
  - `ConcurrentCommandsBenchmark` ouvre plusieurs clients en parallèle (`Params(2,4,8)`) pour évaluer la tenue aux connexions simultanées
- Exécution : `dotnet run -c Release -p tests/KyberBenchmarks`
- Résultats générés (fichier markdown/csv) dans `BenchmarkDotNet.Artifacts`

---

## 📖 Guide d'utilisation

### Scénario 1 : Génération de clés

```powershell
# Générer une paire de clés avec Kyber768 (par défaut)
$keys = New-KyberKeyPair

# Spécifier le niveau de sécurité
$keys512 = New-KyberKeyPair -ParameterSet Kyber512
$keys768 = New-KyberKeyPair -ParameterSet Kyber768
$keys1024 = New-KyberKeyPair -ParameterSet Kyber1024

# Accéder aux clés
Write-Host "Clé publique (hex): $($keys.PublicKeyHex)"
Write-Host "Clé privée (hex): $($keys.PrivateKeyHex)"
```

### Scénario 2 : Échange de clés (Alice et Bob)

```powershell
# Alice génère sa paire de clés
$aliceKeys = New-KyberKeyPair -ParameterSet Kyber768

# Bob utilise la clé publique d'Alice pour créer une clé partagée
$bobEncapsulated = Invoke-KyberEncapsulate -PublicKey $aliceKeys.PublicKey

# Bob envoie le ciphertext à Alice
# $ciphertext = $bobEncapsulated.Ciphertext

# Alice décapsule pour obtenir la même clé partagée
$aliceSharedSecret = Invoke-KyberDecapsulate -Ciphertext $bobEncapsulated.Ciphertext -PrivateKey $aliceKeys.PrivateKey

# Vérifier que les clés correspondent
$match = $bobEncapsulated.SharedSecret -eq $aliceSharedSecret.SharedSecret
```

### Scénario 3 : Utilisation avec des chaînes hexadécimales

```powershell
# Générer des clés
$keys = New-KyberKeyPair

# Utiliser directement les chaînes hex
$encapsulated = Invoke-KyberEncapsulate -PublicKey $keys.PublicKeyHex

# Décapsuler avec les chaînes hex
$sharedSecret = Invoke-KyberDecapsulate -Ciphertext $encapsulated.CiphertextHex -PrivateKey $keys.PrivateKeyHex
```

### Scénario 4 : Sauvegarder et charger des clés

#### Format Text (recommandé pour sauvegarder les paires de clés - sécurisé)
```powershell
# Générer et sauvegarder
$keys = New-KyberKeyPair
Export-KyberKeyPair -KeyPair $keys -Path "keys.txt" -Format Text

# Charger plus tard
$keys = Import-KyberKeyPair -Path "keys.txt"

# Utiliser
$encapsulated = Invoke-KyberEncapsulate -PublicKey $keys.PublicKey
$sharedSecret = Invoke-KyberDecapsulate -Ciphertext $encapsulated.Ciphertext -PrivateKey $keys.PrivateKey
```

#### Format Base64/PEM (compatible avec SSH et autres protocoles)
```powershell
# Exporter en format PEM
$keys = New-KyberKeyPair
Export-KyberKeyPair -KeyPair $keys -Path "keys.pem" -Format Base64

# Exporter uniquement la clé publique (pour la partager)
Export-KyberPublicKey -PublicKey $keys.PublicKey -Path "public_key.pem" -Format Base64

# Importer
$keys = Import-KyberKeyPair -Path "keys.pem"
$publicKey = Import-KyberPublicKey -Path "public_key.pem"
```

### Scénario 5 : Protocole type SSH (serveur/client)

```powershell
# === Côté SERVEUR ===
# 1. Générer les clés du serveur
$serverKeys = New-KyberKeyPair -ParameterSet Kyber768

# 2. Sauvegarder la clé privée (sécurisée)
Export-KyberKeyPair -KeyPair $serverKeys -Path "server_private.txt" -Format Text

# 3. Partager la clé publique avec les clients
Export-KyberPublicKey -PublicKey $serverKeys.PublicKey -Path "server_public.pem" -Format Base64

# 4. Quand un client se connecte, décapsuler la clé partagée
$serverKeys = Import-KyberKeyPair -Path "server_private.txt"
$sharedSecret = Invoke-KyberDecapsulate -Ciphertext $clientCiphertext -PrivateKey $serverKeys.PrivateKey

# === Côté CLIENT ===
# 1. Récupérer la clé publique du serveur
$serverPublicKey = Import-KyberPublicKey -Path "server_public.pem"

# 2. Encapsuler une clé partagée
$encapsulated = Invoke-KyberEncapsulate -PublicKey $serverPublicKey -ParameterSet Kyber768

# 3. Envoyer le ciphertext au serveur
# $encapsulated.Ciphertext

# 4. Utiliser la clé partagée pour la communication
# $encapsulated.SharedSecret
```

### Scénario 6 : Signature numérique avec Dilithium (100% Post-Quantum) ⭐

```powershell
# Générer une paire de clés Dilithium (ML-DSA)
$signingKeys = New-DilithiumKeyPair -ParameterSet Dilithium3

# Signer un message
$message = "Important message"
$messageBytes = [System.Text.Encoding]::UTF8.GetBytes($message)
$signature = Invoke-DilithiumSign -Data $messageBytes -PrivateKey $signingKeys.PrivateKey -ParameterSet Dilithium3

# Vérifier la signature
$isValid = Test-DilithiumSignature -Data $messageBytes -Signature $signature.Signature -PublicKey $signingKeys.PublicKey -ParameterSet Dilithium3

# Sauvegarder les clés
Export-DilithiumKeyPair -KeyPair $signingKeys -Path "signing_keys.txt" -Format Text
```

### Scénario 7 : Signature numérique avec Ed25519 (Compatibilité)

```powershell
# Générer une paire de clés Ed25519
$signingKeys = New-Ed25519KeyPair

# Signer un message
$message = "Important message"
$signature = Invoke-Ed25519Sign -Data $message -PrivateKey $signingKeys.PrivateKey

# Vérifier la signature
$isValid = Test-Ed25519Signature -Data $message -Signature $signature.Signature -PublicKey $signingKeys.PublicKey

# Sauvegarder les clés
Export-Ed25519KeyPair -KeyPair $signingKeys -Path "signing_keys.txt" -Format Text
```

### Scénario 8 : Protocole sécurisé 100% Post-Quantum : Kyber + Dilithium ⭐

Ce scénario combine les deux algorithmes post-quantiques pour créer un protocole sécurisé 100% résistant aux ordinateurs quantiques :
- **Kyber (ML-KEM)** : Pour l'échange de clés résistant aux ordinateurs quantiques
- **Dilithium (ML-DSA)** : Pour l'authentification et l'intégrité des données (post-quantique)

```powershell
# 1. Générer les clés post-quantiques
$kyberKeys = New-KyberKeyPair -ParameterSet Kyber768
$dilithiumKeys = New-DilithiumKeyPair -ParameterSet Dilithium3

# 2. Encapsuler une clé partagée avec Kyber
$encapsulated = Invoke-KyberEncapsulate -PublicKey $kyberKeys.PublicKey -ParameterSet Kyber768

# 3. Signer la clé partagée avec Dilithium
$signature = Invoke-DilithiumSign -Data $encapsulated.SharedSecret -PrivateKey $dilithiumKeys.PrivateKey -ParameterSet Dilithium3

# 4. Vérifier la signature
$isValid = Test-DilithiumSignature -Data $encapsulated.SharedSecret -Signature $signature.Signature -PublicKey $dilithiumKeys.PublicKey -ParameterSet Dilithium3

# 5. Décapsuler la clé partagée
$sharedSecret = Invoke-KyberDecapsulate -Ciphertext $encapsulated.Ciphertext -PrivateKey $kyberKeys.PrivateKey -ParameterSet Kyber768
```

**Avantages de cette combinaison** :
- 🔐 **Kyber (ML-KEM)** : Échange de clés résistant aux ordinateurs quantiques
- ✍️ **Dilithium (ML-DSA)** : Authentification et intégrité post-quantiques
- 🔑 **Clé partagée** : 32 bytes sécurisée et authentifiée
- 🛡️ **100% Post-Quantum** : Résistant aux attaques classiques et quantiques

**Scripts d'exemples** :
- `TestKyberDilithium.ps1` : Test rapide de l'intégration
- `KyberDilithiumCompleteExample.ps1` : Scénario serveur/client complet

### Scénario 9 : Hachage SHA3 avec ML-DSA

```powershell
# Calculer un hachage SHA3-256
$data = "Données à hacher"
$hash256 = Get-SHA3Hash -Data $data -Variant SHA3_256

# Calculer un hachage SHA3-384
$hash384 = Get-SHA3Hash -Data $data -Variant SHA3_384

# Signer avec Dilithium en utilisant SHA3 pré-hachage (recommandé)
$keys = New-DilithiumKeyPair -ParameterSet Dilithium3
$signature = Invoke-DilithiumSign -Data $dataBytes -PrivateKey $keys.PrivateKey -PreHash -SHA3Variant SHA3_256
```

### Scénario 10 : Chiffrement Authenticated Encryption

#### ChaCha20-Poly1305
```powershell
# Chiffrer avec ChaCha20-Poly1305
$plaintext = "Message secret"
$key = New-Object byte[] 32
# Générer une clé (ou utiliser une clé partagée Kyber)
$encrypted = Protect-WithChaCha20Poly1305 -Data $plaintext -Key $key

# Déchiffrer
$decrypted = Unprotect-ChaCha20Poly1305 -EncryptedData $encrypted.EncryptedData -Key $key -Nonce $encrypted.Nonce -Tag $encrypted.Tag
```

#### AES-GCM
```powershell
# Chiffrer avec AES-GCM
$plaintext = "Message secret"
$key = New-Object byte[] 32
$encrypted = Protect-WithAESGCM -Data $plaintext -Key $key

# Déchiffrer
$decrypted = Unprotect-AESGCM -EncryptedData $encrypted.EncryptedData -Key $key -Nonce $encrypted.Nonce -Tag $encrypted.Tag
```

### Scénario 11 : Export/Import PKCS#8 et Certificats X.509

```powershell
# Exporter une clé privée Dilithium en format PKCS#8
$keys = New-DilithiumKeyPair -ParameterSet Dilithium3
Export-PrivateKeyPKCS8 -PrivateKey $keys.PrivateKey -Path "dilithium_key.pem" -Format PEM -KeyType Dilithium -ParameterSet Dilithium3

# Importer une clé privée PKCS#8
$imported = Import-PrivateKeyPKCS8 -Path "dilithium_key.pem" -KeyType Dilithium

# Créer un certificat X.509 auto-signé avec Dilithium
$cert = New-X509Certificate -SubjectName "CN=Test Post-Quantum Certificate" -PrivateKey $keys.PrivateKey -ParameterSet Dilithium3 -Path "certificate.pem" -Format PEM

# Vérifier un certificat X.509
$isValid = Test-X509Certificate -Path "certificate.pem"
```

### Scénario 12 : Configuration TLS 1.3 Post-Quantum Hybride ⭐

Ce scénario permet de créer des configurations TLS 1.3 hybrides combinant des algorithmes classiques (ECDSA, RSA) avec des algorithmes post-quantiques (Kyber, Dilithium) pour une transition progressive vers la cryptographie post-quantique.

```powershell
# Générer une configuration TLS hybride par défaut (ECDSA-P256 + Kyber768 + Dilithium3)
$hybridConfig = New-TLS13HybridConfig

# Obtenir le nom du cipher suite
$cipherSuite = Get-TLS13HybridCipherSuite -Config $hybridConfig
Write-Host "Cipher Suite: $cipherSuite"  # TLS13-ECDSA-P256+ML-KEM-768+ML-DSA-65

# Générer une configuration avec RSA
$rsaConfig = New-TLS13HybridConfig -ClassicalAlgorithm RSA_2048 -PostQuantumKemAlgorithm Kyber768 -PostQuantumSigAlgorithm Dilithium3

# Générer une configuration haute sécurité
$highSecConfig = New-TLS13HybridConfig -ClassicalAlgorithm ECDSA_P384 -PostQuantumKemAlgorithm Kyber1024 -PostQuantumSigAlgorithm Dilithium5

# Exporter la configuration pour intégration TLS
Export-TLS13HybridConfig -Config $hybridConfig -Path "tls13_hybrid_config.txt" -Force
```

**Algorithmes classiques supportés** :
- `ECDSA_P256` : ECDSA avec courbe P-256 (NIST)
- `ECDSA_P384` : ECDSA avec courbe P-384 (NIST)
- `RSA_2048` : RSA 2048 bits
- `RSA_3072` : RSA 3072 bits

**Algorithmes post-quantiques supportés** :
- **KEM** : Kyber512, Kyber768, Kyber1024 (ML-KEM)
- **Signature** : Dilithium2, Dilithium3, Dilithium5 (ML-DSA)

**Avantages de l'hybridation** :
- 🔐 Sécurité classique éprouvée (compatibilité avec infrastructure existante)
- 🛡️ Sécurité post-quantique (résistance aux ordinateurs quantiques)
- ✅ Transition progressive vers la cryptographie post-quantique
- 🔄 Compatibilité avec bibliothèques TLS supportant les extensions post-quantiques

**Exemple complet** : Voir `TLS13HybridExamples.ps1` pour des scénarios complets.

### Scénario 13 : Benchmark de Performance Haute Précision

Le script `PerformanceBenchmark.ps1` permet de mesurer avec haute précision les performances de tous les algorithmes cryptographiques du module :

```powershell
# Exécuter le benchmark complet
.\PerformanceBenchmark.ps1
```

Le script mesure :
- ✅ Temps de génération de clés (Kyber, Dilithium, Ed25519)
- ✅ Temps d'encapsulation/décapsulation (Kyber)
- ✅ Temps de signature/vérification (Dilithium, Ed25519)
- ✅ Temps de hachage (SHA3-256, SHA3-384)
- ✅ Temps de chiffrement/déchiffrement (ChaCha20-Poly1305, AES-GCM)

**Métriques affichées** :
- **Moyenne** : Temps moyen en µs et ms (4 décimales pour ms, 2 pour µs)
- **Médiane** : Temps médian (moins sensible aux valeurs aberrantes)
- **Minimum et Maximum** : Plage de variation des temps
- **Écart-type** : Mesure de la variabilité des performances
- **Statut** : SUCCESS (toutes réussies), PARTIAL (certaines échouées), FAILED (toutes échouées)
- **Itérations** : Nombre d'itérations réussies vs total

**Caractéristiques de précision** :
- **Opérations lentes** (Kyber/Dilithium) : 10 itérations
- **Opérations rapides** (Ed25519/SHA3/AEAD) : 500-1000 itérations pour précision
- **Warm-up automatique** : Exécution préalable pour éviter les effets de cache/JIT
- **Mesures en microsecondes** : Précision maximale pour opérations rapides
- **Export CSV** : Toutes les données exportables pour analyse approfondie

**Exemple de sortie** :
```
✅ Génération de clés Ed25519:
   Moyenne: 125.50 µs (0.1255 ms)
   Médiane: 124.20 µs (0.1242 ms)
   Min: 120.10 µs (0.1201 ms) | Max: 135.30 µs (0.1353 ms)
   Écart-type: 3.45 µs (0.0035 ms)
   Itérations: 1000/1000 réussies
```

**Export CSV** :
Le script génère également un export CSV avec toutes les métriques :
- Operation, Status, Average_ms, Average_us, Median_ms, Median_us, Min_ms, Min_us, Max_ms, Max_us, StdDev_ms, StdDev_us, SuccessfulIterations, TotalIterations, Errors

### Scénario 14 : Gestion Sécurisée des Clés en Mémoire ⭐

Ce scénario démontre l'utilisation de la gestion sécurisée des clés pour protéger les clés privées en mémoire.

```powershell
# Générer une paire de clés avec gestion sécurisée
$secureKeys = New-SecureKyberKeyPair -ParameterSet Kyber768

# Utiliser les clés
$publicKey = $secureKeys.PublicKey
$privateKey = $secureKeys.PrivateKey

# Encapsuler et décapsuler
$encapsulated = Invoke-KyberEncapsulate -PublicKey $publicKey -ParameterSet Kyber768
$decapsulated = Invoke-KyberDecapsulate -Ciphertext $encapsulated.Ciphertext -PrivateKey $privateKey -ParameterSet Kyber768

# Nettoyer immédiatement la clé privée après utilisation
$secureKeys.ZeroizePrivateKey()

# Vérifier que la clé est nettoyée
$isZeroized = Test-ZeroizedKey -Key $privateKey
Write-Host "Clé nettoyée: $isZeroized"  # True
```

**Nettoyage manuel avec Clear-SecureKey** :
```powershell
# Générer des clés normales
$keys = New-KyberKeyPair -ParameterSet Kyber768

# Utiliser les clés...

# Nettoyer manuellement avec une seule passe
Clear-SecureKey -Key $keys.PrivateKey -Passes 1

# Ou avec plusieurs passes pour sécurité renforcée (protection cold boot)
Clear-SecureKey -Key $keys.PrivateKey -Passes 3 -Nullify

# Vérifier le nettoyage
$isClean = Test-ZeroizedKey -Key $keys.PrivateKey
```

**Pattern try-finally pour nettoyage garanti** :
```powershell
$keys = New-KyberKeyPair -ParameterSet Kyber768
try {
    # Utiliser les clés...
    $encapsulated = Invoke-KyberEncapsulate -PublicKey $keys.PublicKey
}
finally {
    # Nettoyer dans tous les cas (même en cas d'exception)
    Clear-SecureKey -Key $keys.PrivateKey -Passes 3 | Out-Null
}
```

**Avantages** :
- 🔐 Nettoyage automatique des clés privées
- 🛡️ Protection contre les attaques de récupération de mémoire
- ✅ Nettoyage multi-passes pour sécurité renforcée
- 🔄 Pattern Dispose pour nettoyage garanti

**Exemple complet** : Voir `SecureKeyManagementExamples.ps1` pour des scénarios complets.

### Scénario 15 : Protection contre les Canaux Auxiliaires (Side-Channel Attacks) ⭐

Ce scénario démontre l'utilisation des contre-mesures contre les attaques par canaux auxiliaires (timing attacks, cache side-channel attacks).

#### Vue d'ensemble

Les attaques par canaux auxiliaires exploitent des informations indirectes (temps d'exécution, accès au cache) pour extraire des secrets cryptographiques. Le module implémente des contre-mesures robustes pour protéger contre ces attaques.

#### Cmdlets disponibles

**1. Décapsulation sécurisée Kyber** :
```powershell
# Décapsulation avec protection side-channel
$keys = New-KyberKeyPair -ParameterSet Kyber768
$encapsulated = Invoke-KyberEncapsulate -PublicKey $keys.PublicKey -ParameterSet Kyber768

# Méthode normale
$decapsulatedNormal = Invoke-KyberDecapsulate -Ciphertext $encapsulated.Ciphertext -PrivateKey $keys.PrivateKey -ParameterSet Kyber768

# Méthode sécurisée (protection side-channel)
$decapsulatedSecure = Invoke-KyberDecapsulateSecure -Ciphertext $encapsulated.Ciphertext -PrivateKey $keys.PrivateKey -ParameterSet Kyber768

# Les résultats sont identiques, mais la méthode sécurisée protège contre les attaques
```

**2. Vérification de signature sécurisée Dilithium** :
```powershell
# Génération et signature
$keys = New-DilithiumKeyPair -ParameterSet Dilithium3
$message = "Message important à signer"
$messageBytes = [System.Text.Encoding]::UTF8.GetBytes($message)
$signature = Invoke-DilithiumSign -Data $messageBytes -PrivateKey $keys.PrivateKey -ParameterSet Dilithium3

# Vérification normale
$isValidNormal = Test-DilithiumSignature -Data $messageBytes -Signature $signature.Signature -PublicKey $keys.PublicKey -ParameterSet Dilithium3

# Vérification sécurisée (protection side-channel)
$isValidSecure = Test-DilithiumSignatureSecure -Data $messageBytes -Signature $signature.Signature -PublicKey $keys.PublicKey -ParameterSet Dilithium3
```

**3. Comparaison en temps constant** :
```powershell
# Comparaison de clés partagées en temps constant
$key1 = [byte[]]::new(32)
$key2 = [byte[]]::new(32)
# ... remplir les clés ...

# Comparaison sécurisée (protège contre timing attacks)
$areEqual = Test-ConstantTimeCompare -Array1 $key1 -Array2 $key2

# Supporte aussi les chaînes hexadécimales
$hex1 = "0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF"
$hex2 = "0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF"
$areEqual = Test-ConstantTimeCompare -Array1 $hex1 -Array2 $hex2
```

#### Protections implémentées

**1. Opérations à temps constant** :
- ✅ Pas de branches conditionnelles basées sur des données secrètes
- ✅ Comparaisons qui prennent toujours le même temps
- ✅ Masquage des variations de timing

**2. Protection contre timing attacks** :
- ✅ Toutes les comparaisons utilisent `ConstantTimeEquals()`
- ✅ Pas de sortie anticipée lors des comparaisons
- ✅ Temps d'exécution indépendant du contenu des données

**3. Protection contre cache side-channel attacks** :
- ✅ Nettoyage du cache après opérations sensibles
- ✅ Accès mémoire uniformes pour masquer les patterns
- ✅ Préchargement du cache pour éviter les variations

**4. Méthodes sécurisées** :
- `DecapsulateSecure()` : Décapsulation Kyber avec protection side-channel
- `VerifySecure()` : Vérification Dilithium avec protection side-channel
- `ConstantTimeCompareSharedSecrets()` : Comparaison de clés partagées en temps constant
- `ConstantTimeCompareSignatures()` : Comparaison de signatures en temps constant

#### Exemple d'intégration complète

```powershell
# Protocole complet avec protections side-channel
$kyberKeys = New-KyberKeyPair -ParameterSet Kyber768
$dilithiumKeys = New-DilithiumKeyPair -ParameterSet Dilithium3

# Encapsulation
$encapsulated = Invoke-KyberEncapsulate -PublicKey $kyberKeys.PublicKey -ParameterSet Kyber768

# Décapsulation sécurisée
$sharedSecret = Invoke-KyberDecapsulateSecure -Ciphertext $encapsulated.Ciphertext -PrivateKey $kyberKeys.PrivateKey -ParameterSet Kyber768

# Signature de la clé partagée
$signature = Invoke-DilithiumSign -Data $sharedSecret.SharedSecret -PrivateKey $dilithiumKeys.PrivateKey -ParameterSet Dilithium3

# Vérification sécurisée
$isValid = Test-DilithiumSignatureSecure -Data $sharedSecret.SharedSecret -Signature $signature.Signature -PublicKey $dilithiumKeys.PublicKey -ParameterSet Dilithium3
```

#### Tests de validation

Un script de tests complet est disponible : `SideChannelProtectionTests.ps1`

```powershell
# Exécuter tous les tests de protection side-channel
.\SideChannelProtectionTests.ps1
```
Les tests valident :
- ✅ Comparaisons en temps constant
- ✅ Décapsulation sécurisée Kyber
- ✅ Vérification sécurisée Dilithium
- ✅ Tests de timing (vérification que les opérations prennent un temps similaire)
- ✅ Détection de différences à toutes les positions
- ✅ Intégration complète avec protections side-channel
- ✅ Support de différentes tailles de données

**Exemple complet** : Voir `CONTRE_MESURES_SIDE_CHANNEL.md` pour la documentation technique complète.

### Scénario 16 : Audit et Validation Cryptographique ⭐

Ce scénario démontre l'utilisation des outils d'audit et de validation cryptographique pour vérifier la conformité aux standards NIST et la robustesse des implémentations.

#### Vue d'ensemble

Le module fournit des outils complets pour :
- **Tests de conformité** : Validation des tailles, formats et comportements selon les standards NIST (FIPS 203, ML-DSA)
- **Tests de fuzzing** : Génération de données corrompues pour tester la robustesse
- **Tests de validation cryptographique** : Vérification des propriétés cryptographiques

#### Cmdlets disponibles

**1. Tests de conformité NIST** :
```powershell
# Tester la conformité de tous les algorithmes
$results = Test-CryptographicConformance -Algorithm All

# Tester uniquement Kyber
$kyberResults = Test-CryptographicConformance -Algorithm Kyber -KyberParameterSet Kyber768

# Tester uniquement Dilithium
$dilithiumResults = Test-CryptographicConformance -Algorithm Dilithium -DilithiumParameterSet Dilithium3

# Afficher uniquement les tests échoués
$failed = Test-CryptographicConformance -Algorithm All -ShowOnlyFailed
```

**2. Tests de fuzzing** :
```powershell
# Exécuter des tests de fuzzing sur tous les algorithmes
$fuzzingResults = Invoke-CryptographicFuzzing -Algorithm All -Iterations 100

# Fuzzing Kyber uniquement
$kyberFuzzing = Invoke-CryptographicFuzzing -Algorithm Kyber -KyberParameterSet Kyber768 -Iterations 50

# Afficher uniquement les problèmes (crashes ou échecs)
$issues = Invoke-CryptographicFuzzing -Algorithm All -ShowOnlyIssues
```

**3. Script d'audit complet** :
```powershell
# Exécuter tous les tests d'audit
.\CryptographicAuditTests.ps1
```

#### Tests de conformité

Les tests de conformité valident :

**1. Tailles de clés et ciphertexts** :
- ✅ Clés publiques/privées Kyber selon NIST FIPS 203
- ✅ Ciphertexts et clés partagées Kyber
- ✅ Clés publiques/privées Dilithium selon NIST ML-DSA
- ✅ Signatures Dilithium
- ✅ Clés et signatures Ed25519 selon RFC 8032

**2. Propriétés cryptographiques** :
- ✅ Non-déterminisme des générations de clés
- ✅ Correspondance des clés partagées après encapsulation/décapsulation
- ✅ Correspondance des signatures avec les clés publiques
- ✅ Rejet correct de données invalides

**3. Formats et sérialisation** :
- ✅ Formats de clés conformes
- ✅ Reconstruction correcte des clés privées

#### Tests de fuzzing

Les tests de fuzzing génèrent et testent :

**1. Données corrompues** :
- Corruption de bytes individuels
- Corruption multiple de bytes
- Tailles invalides (trop courtes, trop longues)
- Patterns suspects (tous zéros, tous FF, etc.)

**2. Ciphertexts corrompus** :
- Test de décapsulation avec ciphertexts invalides
- Vérification du rejet correct des données corrompues

**3. Signatures corrompues** :
- Test de vérification avec signatures invalides
- Vérification du rejet correct des signatures corrompues

**4. Clés corrompues** :
- Test avec clés publiques/privées corrompues
- Vérification de la robustesse face aux clés invalides

#### Exemple d'utilisation complète

```powershell
# 1. Tests de conformité
Write-Host "=== Tests de Conformité ===" -ForegroundColor Cyan
$conformanceResults = Test-CryptographicConformance -Algorithm All

foreach ($result in $conformanceResults) {
    if ($result.Passed) {
        Write-Host "✅ $($result.TestName)" -ForegroundColor Green
    } else {
        Write-Host "❌ $($result.TestName)" -ForegroundColor Red
        foreach ($error in $result.Errors) {
            Write-Host "   - $error" -ForegroundColor Yellow
        }
    }
}

# 2. Tests de fuzzing
Write-Host "`n=== Tests de Fuzzing ===" -ForegroundColor Cyan
$fuzzingResults = Invoke-CryptographicFuzzing -Algorithm All -Iterations 50

foreach ($result in $fuzzingResults) {
    Write-Host "$($result.TestName):" -ForegroundColor Cyan
    Write-Host "  Total: $($result.TotalTests) | Passed: $($result.Passed) | Failed: $($result.Failed) | Crashed: $($result.Crashed)" -ForegroundColor Gray
    if ($result.Crashed -gt 0) {
        Write-Host "  ⚠️  $($result.Crashed) crash(es) détecté(s)" -ForegroundColor Yellow
    }
}

# 3. Script d'audit complet
.\CryptographicAuditTests.ps1
```

#### Résultats attendus

**Tests de conformité** :
- ✅ Tous les tests doivent passer
- ✅ Toutes les tailles doivent correspondre aux spécifications NIST
- ✅ Toutes les propriétés cryptographiques doivent être validées

**Tests de fuzzing** :
- ✅ Les données corrompues doivent être rejetées correctement
- ✅ Aucun crash ne doit se produire (exceptions gérées)
- ⚠️  Les "Failed" sont attendus car ils indiquent le rejet correct de données invalides

#### Tests de validation cryptographique

Le script `CryptographicAuditTests.ps1` inclut également :

**1. Tests d'intégrité** :
- Vérification que les clés partagées sont identiques après encapsulation/décapsulation

**2. Tests d'authentification** :
- Vérification que les signatures sont valides avec les clés publiques correspondantes

**3. Tests de rejet** :
- Vérification que les données corrompues sont correctement rejetées

**4. Tests de non-déterminisme** :
- Vérification que les clés générées sont différentes à chaque génération

**5. Tests de stress** :
- Génération multiple de clés
- Encapsulation/décapsulation multiple
- Mesure des performances sous charge

#### Interprétation des résultats

**Tests de conformité** :
- `Passed = true` : Le test est conforme aux standards
- `Passed = false` : Le test a échoué, vérifier les erreurs dans `Errors`

**Tests de fuzzing** :
- `Crashed = 0` : Aucune exception non gérée (bon signe)
- `Failed > 0` : Normal, indique le rejet correct de données invalides
- `Crashed > 0` : Problème potentiel, exceptions non gérées détectées

**Exemple complet** : Voir `CryptographicAuditTests.ps1` pour un script complet d'audit et de validation.

### Scénario 17 : Protocole sécurisé combinant Kyber + Ed25519

Ce scénario combine les deux algorithmes pour créer un protocole sécurisé complet :
- **Kyber (ML-KEM)** : Pour l'échange de clés résistant aux ordinateurs quantiques
- **Ed25519** : Pour l'authentification et l'intégrité des données

```powershell
# === Étape 1: Générer les clés ===
# Générer des clés Kyber pour l'échange de clés
$kyberKeys = New-KyberKeyPair -ParameterSet Kyber768

# Générer des clés Ed25519 pour la signature
$ed25519Keys = New-Ed25519KeyPair

# === Étape 2: Encapsuler une clé partagée avec Kyber ===
$encapsulated = Invoke-KyberEncapsulate -PublicKey $kyberKeys.PublicKey -ParameterSet Kyber768

# === Étape 3: Signer la clé partagée avec Ed25519 ===
$signature = Invoke-Ed25519Sign -Data $encapsulated.SharedSecret -PrivateKey $ed25519Keys.PrivateKey

# === Étape 4: Vérifier la signature ===
$isValid = Test-Ed25519Signature -Data $encapsulated.SharedSecret -Signature $signature.Signature -PublicKey $ed25519Keys.PublicKey

# === Étape 5: Décapsuler et vérifier ===
$decapsulated = Invoke-KyberDecapsulate -Ciphertext $encapsulated.Ciphertext -PrivateKey $kyberKeys.PrivateKey

# Vérifier que les clés partagées correspondent
if ($encapsulated.SharedSecretHex -eq $decapsulated.SharedSecretHex) {
    Write-Host "✓ Clés partagées identiques !"
}

# Vérifier la signature avec la clé décapsulée
$isValidDecapsulated = Test-Ed25519Signature -Data $decapsulated.SharedSecret -Signature $signature.Signature -PublicKey $ed25519Keys.PublicKey
```

**Exemple complet** : Voir `TestKyberEd25519.ps1` pour un scénario complet avec export/import de clés.

---

## 🔧 Détails techniques

### Tailles des clés et ciphertexts

#### Kyber (ML-KEM)

| Paramètre | Clé publique | Clé privée | Ciphertext | Clé partagée |
|-----------|-------------|------------|------------|--------------|
| Kyber512  | 800 bytes   | 1632 bytes | 768 bytes  | 32 bytes     |
| Kyber768  | 1184 bytes  | 2400 bytes | 1088 bytes | 32 bytes     |
| Kyber1024 | 1568 bytes  | 3168 bytes | 1568 bytes | 32 bytes     |

#### Dilithium (ML-DSA) - 100% Post-Quantum ⭐

| Paramètre | Clé publique | Clé privée | Signature | Niveau NIST |
|-----------|--------------|------------|-----------|-------------|
| Dilithium2 (ML-DSA-44) | ~1312 bytes | ~2560 bytes | ~2420 bytes | Niveau 1 |
| Dilithium3 (ML-DSA-65) | ~1952 bytes | ~4032 bytes | ~3309 bytes | Niveau 2 |
| Dilithium5 (ML-DSA-87) | ~2592 bytes | ~4864 bytes | ~4627 bytes | Niveau 3 |

#### Ed25519 (Compatibilité)

| Type | Taille |
|------|--------|
| Clé publique | 32 bytes |
| Clé privée | 32 bytes |
| Signature | 64 bytes |

**Remarque** : Ed25519 est un algorithme de signature numérique classique (non post-quantique) mais très performant et largement utilisé. Il est recommandé pour l'authentification et l'intégrité des données, tandis que Kyber est utilisé pour l'échange de clés résistant aux ordinateurs quantiques.

### Sérialisation de la clé privée

Le module utilise un format de sérialisation personnalisé pour éviter les problèmes avec `GetEncoded()` de BouncyCastle :

```
Format: [Magic(4)] [sLength(4)]s [hpkLength(4)]hpk [nonceLength(4)]nonce [tLength(4)]t [rhoLength(4)]rho

Magic = 0x4B594245 ("KYBE" en ASCII)
```

**Avantages** :
- Format fiable et reproductible
- Reconstruction correcte des composants
- Compatible avec tous les paramètres Kyber

### Architecture de sécurité

- **Kyber512** : Niveau de sécurité équivalent à AES-128 (résistant aux attaques classiques et quantiques)
- **Kyber768** : Niveau de sécurité équivalent à AES-192 (recommandé pour la plupart des applications)
- **Kyber1024** : Niveau de sécurité équivalent à AES-256 (niveau de sécurité maximal)
### Gestion de la mémoire
- Les clés sont stockées en mémoire comme `byte[]`
- Les conversions hexadécimales sont effectuées à la demande
- **Gestion sécurisée disponible** : Utilisez `New-Secure*KeyPair` pour un nettoyage automatique des clés privées
- **Nettoyage manuel** : Utilisez `Clear-SecureKey` pour nettoyer manuellement les clés après utilisation
- **Vérification** : Utilisez `Test-ZeroizedKey` pour vérifier si une clé a été nettoyée

#### Gestion Sécurisée des Clés ⭐

Le module fournit des cmdlets et classes pour la gestion sécurisée des clés en mémoire :

**Cmdlets sécurisés** :
- `New-SecureKyberKeyPair` : Génère une paire de clés Kyber avec nettoyage automatique
- `New-SecureDilithiumKeyPair` : Génère une paire de clés Dilithium avec nettoyage automatique
- `New-SecureEd25519KeyPair` : Génère une paire de clés Ed25519 avec nettoyage automatique
- `Clear-SecureKey` : Nettoie manuellement une clé en mémoire (zeroization)
- `Test-ZeroizedKey` : Vérifie si une clé a été nettoyée

**Caractéristiques** :
- ✅ Nettoyage automatique lors de la destruction de l'objet (IDisposable)
- ✅ Nettoyage multi-passes (protection contre cold boot attacks)
- ✅ Thread-safe avec verrous
- ✅ Finalizer pour nettoyage même si Dispose() n'est pas appelé

**Exemple** :
```powershell
# Génération sécurisée
$secureKeys = New-SecureKyberKeyPair -ParameterSet Kyber768

# Utiliser les clés
$publicKey = $secureKeys.PublicKey
$privateKey = $secureKeys.PrivateKey

# Nettoyer immédiatement après utilisation
$secureKeys.ZeroizePrivateKey()

# Ou laisser le nettoyage automatique (garbage collector)
```

**Voir** : `GESTION_SECURISEE_CLES.md` pour la documentation complète.

#### Protection contre les Canaux Auxiliaires ⭐

Le module fournit des contre-mesures contre les attaques par canaux auxiliaires :

**Classes de protection** :
- `ConstantTimeOperations` : Opérations à temps constant (protection timing attacks)
- `SideChannelProtection` : Protection contre cache side-channel attacks

**Cmdlets sécurisés** :
- `Invoke-KyberDecapsulateSecure` : Décapsulation avec protection side-channel
- `Test-DilithiumSignatureSecure` : Vérification de signature avec protection side-channel
- `Test-ConstantTimeCompare` : Comparaison en temps constant

**Caractéristiques** :
- ✅ Opérations à temps constant (pas de branches conditionnelles basées sur données secrètes)
- ✅ Nettoyage du cache après opérations sensibles
- ✅ Accès mémoire uniformes pour masquer les patterns
- ✅ Protection contre timing attacks et cache side-channel attacks

**Exemple** :
```powershell
# Décapsulation sécurisée
$sharedSecret = Invoke-KyberDecapsulateSecure -Ciphertext $ciphertext -PrivateKey $privateKey

# Vérification sécurisée
$isValid = Test-DilithiumSignatureSecure -Data $data -Signature $signature -PublicKey $publicKey

# Comparaison en temps constant
$areEqual = Test-ConstantTimeCompare -Array1 $key1 -Array2 $key2
```

**Voir** : `CONTRE_MESURES_SIDE_CHANNEL.md` pour la documentation complète.

---

## 🐛 Dépannage

### Problème : Module non trouvé

**Erreur** : `The specified module 'KyberModule' was not loaded`

**Solution** :
```powershell
# Vérifier que le module est dans le bon répertoire
Test-Path ".\KyberModule.psd1"

# Importer avec le chemin complet
Import-Module ".\KyberModule.psd1" -Force
```

### Problème : DLL manquante

**Erreur** : `Could not load file or assembly 'BouncyCastle.Crypto'`

**Solution** :
1. Vérifier que `BouncyCastle.Crypto.dll` est dans le même répertoire que `KyberModule.dll`
2. Vérifier que la version est 2.2.1 ou compatible

### Problème : Clés partagées ne correspondent pas

**Symptôme** : Les clés partagées après encapsulation/décapsulation sont différentes

**Causes possibles** :
- Mauvais paramètre `-ParameterSet` entre encapsulation et décapsulation
- Clé publique et clé privée ne correspondent pas
- Ciphertext corrompu

**Solution** :
```powershell
# Vérifier que les paramètres correspondent
$keys = New-KyberKeyPair -ParameterSet Kyber768
$encapsulated = Invoke-KyberEncapsulate -PublicKey $keys.PublicKey -ParameterSet Kyber768
$decapsulated = Invoke-KyberDecapsulate -Ciphertext $encapsulated.Ciphertext -PrivateKey $keys.PrivateKey -ParameterSet Kyber768
```

### Problème : Erreur de compilation

**Erreur** : `error CS8370: La fonctionnalité 'modèles récursifs' n'est pas disponible`

**Solution** : Vérifier que `LangVersion` est défini à `latest` dans les fichiers `.csproj`

---

## 📚 Ressources

- **NIST FIPS 203** : Standard ML-KEM (Module-Lattice-Based Key-Encapsulation Mechanism)
- **BouncyCastle Documentation** : https://www.bouncycastle.org/documentation.html
- **PowerShell Standard Library** : https://github.com/PowerShell/PowerShellStandard

### 📖 Documentation Technique

Pour une documentation complète sur la recherche et le développement du projet, consultez :
- **`RECHERCHE_ET_DEVELOPPEMENT.md`** : Document annexe détaillant toutes les étapes, recherches, problèmes rencontrés et solutions trouvées lors du développement du module.
- **`ANALYSE_DILITHIUM.md`** : Analyse complète de l'intégration de CRYSTALS-Dilithium (ML-DSA) dans le projet.
- **`MIGRATION_BOUNCYCASTLE.md`** : Guide de migration de BouncyCastle.NetCore vers BouncyCastle.Cryptography 2.6.2.

### 🎯 Fonctionnalités Avancées

#### Fonctions de Hachage SHA3
- **SHA3-256** : Hachage 256 bits recommandé pour ML-DSA
- **SHA3-384** : Hachage 384 bits pour sécurité renforcée
- **Pré-hachage optionnel** : Intégration dans les workflows Dilithium

#### Chiffrement Authenticated Encryption (AEAD)
- **ChaCha20-Poly1305** : Algorithme moderne et performant
- **AES-GCM** : Standard industrie pour chiffrement authentifié
- **Authentification intégrée** : Garantit l'intégrité et l'authenticité des données

#### Conformité Standards PKI
- **PKCS#8** : Export/import de clés privées en format standard (DER/PEM)
- **X.509** : Création et vérification de certificats auto-signés avec clés post-quantiques
- **CMS** : Support des enveloppes chiffrées (signatures en développement)

---

## 📝 Licence

Ce module utilise BouncyCastle.NetCore qui est sous licence MIT.

---

## 👥 Contribution

Pour contribuer au projet :
1. Fork le repository
2. Créer une branche pour votre fonctionnalité
3. Faire vos modifications
4. Tester avec `Test.ps1`
5. Créer une pull request

---

## 📧 Support

Pour toute question ou problème, consultez :
- La documentation BouncyCastle
- Les exemples dans `Examples.ps1`
- Les tests dans `Test.ps1`

---

**Version** : 1.1.0  
**Dernière mise à jour** : Décembre 2025

---

## 📖 Guide d'utilisation

### Communications distantes (KyberDaemon + KyberCLI)
1. **S'assurer que le service est démarré** (voir section Installation).
2. **Utiliser KyberCLI directement** :
   ```powershell
   dotnet KyberCLI.dll connect --host 192.168.1.10 --user alice --password 0x09AF... --command "hostname"
   dotnet KyberCLI.dll connect --host 192.168.1.10 --user bob --key C:\secrets\bob.key
   ```
3. **Depuis PowerShell** avec la cmdlet `Invoke-SecureCommand` (wrapper KyberCLI) :
   ```powershell
   Invoke-SecureCommand \
       -ServerHost 192.168.1.10 \
       -Port 8443 \
       -Username alice \
       -PasswordHex 0x09AF... \
       -Command "Get-Process | Select-Object -First 5 Name,Id"
   ```
   Paramètres supplémentaires :
   - `-KeyPath` : clé privée Dilithium (base64) pour signature
   - `-CliPath` : binaire/dll KyberCLI personnalisé
   - `-RebuildCli` : force `dotnet publish` avant exécution

> ⚠️ `Start-SecureServer`, `Connect-SecureClient`, `Send/Receive-SecureMessage` sont conservés pour compatibilité mais renvoient désormais une erreur explicite. Utilisez KyberDaemon/KyberCLI pour toute communication réseau.

### Communication

| Cmdlet | Statut | Description |
|--------|--------|-------------|
| `Invoke-SecureCommand` | ✅ Actif | Exécute une commande distante via KyberCLI (KyberDaemon requis) |
| `Start-SecureServer` | ⚠️ Obsolète | Ancienne implémentation serveur (renvoie une erreur guidant vers KyberDaemon) |
| `Connect-SecureClient` | ⚠️ Obsolète | Ancien client PowerShell (renvoie une erreur guidant vers KyberCLI) |
| `Send-SecureMessage` | ⚠️ Obsolète | Ancienne commande d'envoi (renvoie une erreur guidant vers KyberCLI) |
| `Receive-SecureMessage` | ⚠️ Obsolète | Ancienne commande de réception (renvoie une erreur guidant vers KyberCLI) |

---

## 📖 Outils d'administration

- `KyberCLI keys list --config /etc/kyberd/kyberd.conf` : affiche les versions de clés stockées (répertoires `session.vN.*`)
- `KyberCLI keys rotate --name session --days 90` : force la rotation immédiate (respecte la passphrase du `kyberd.conf`)
- `KyberCLI keys export --output metadata.json` : extrait les métadonnées (versions, modes de chiffrement)
- Script PowerShell dédié (`KyberDaemon/tools/Invoke-KyberKeyRotation.ps1`) enveloppant `KyberCLI keys` pour Windows / PowerShell Core (`-Action List|Rotate|Export`, `-Config`, `-Name`, `-Days`, `-CliPath`...)

### Tests & Qualité
- Tests unitaires/crypto historiques (scripts PowerShell dans `KyberModule/`)
- Tests d'intégration end-to-end (`dotnet test tests/KyberIntegrationTests`) : démarre un daemon éphémère, exécute KyberCLI et injecte du trafic corrompu pour valider la robustesse réseau
- Fuzzer réseau simple (`NetworkFuzzer.SendRandomHandshakeAsync`) utilisable depuis les tests ou un harness personnalisé pour enrichir votre campagne fuzz

### Benchmarks
- Projet `tests/KyberBenchmarks` (BenchmarkDotNet)
  - `CommandLatencyBenchmark` mesure la latence moyenne d'une commande `echo`
  - `HandshakeBenchmark` quantifie le coût handshake/fermeture d'une session complète
  - `ConcurrentCommandsBenchmark` ouvre plusieurs clients en parallèle (`Params(2,4,8)`) pour évaluer la tenue aux connexions simultanées
- Exécution : `dotnet run -c Release -p tests/KyberBenchmarks`
- Résultats générés (fichier markdown/csv) dans `BenchmarkDotNet.Artifacts`

---

## 📖 Guide d'utilisation

### Scénario 1 : Génération de clés

```powershell
# Générer une paire de clés avec Kyber768 (par défaut)
$keys = New-KyberKeyPair

# Spécifier le niveau de sécurité
$keys512 = New-KyberKeyPair -ParameterSet Kyber512
$keys768 = New-KyberKeyPair -ParameterSet Kyber768
$keys1024 = New-KyberKeyPair -ParameterSet Kyber1024

# Accéder aux clés
Write-Host "Clé publique (hex): $($keys.PublicKeyHex)"
Write-Host "Clé privée (hex): $($keys.PrivateKeyHex)"
```

### Scénario 2 : Échange de clés (Alice et Bob)

```powershell
# Alice génère sa paire de clés
$aliceKeys = New-KyberKeyPair -ParameterSet Kyber768

# Bob utilise la clé publique d'Alice pour créer une clé partagée
$bobEncapsulated = Invoke-KyberEncapsulate -PublicKey $aliceKeys.PublicKey

# Bob envoie le ciphertext à Alice
# $ciphertext = $bobEncapsulated.Ciphertext

# Alice décapsule pour obtenir la même clé partagée
$aliceSharedSecret = Invoke-KyberDecapsulate -Ciphertext $bobEncapsulated.Ciphertext -PrivateKey $aliceKeys.PrivateKey

# Vérifier que les clés correspondent
$match = $bobEncapsulated.SharedSecret -eq $aliceSharedSecret.SharedSecret
```

### Scénario 3 : Utilisation avec des chaînes hexadécimales

```powershell
# Générer des clés
$keys = New-KyberKeyPair

# Utiliser directement les chaînes hex
$encapsulated = Invoke-KyberEncapsulate -PublicKey $keys.PublicKeyHex

# Décapsuler avec les chaînes hex
$sharedSecret = Invoke-KyberDecapsulate -Ciphertext $encapsulated.CiphertextHex -PrivateKey $keys.PrivateKeyHex
```

### Scénario 4 : Sauvegarder et charger des clés

#### Format Text (recommandé pour sauvegarder les paires de clés - sécurisé)
```powershell
# Générer et sauvegarder
$keys = New-KyberKeyPair
Export-KyberKeyPair -KeyPair $keys -Path "keys.txt" -Format Text

# Charger plus tard
$keys = Import-KyberKeyPair -Path "keys.txt"

# Utiliser
$encapsulated = Invoke-KyberEncapsulate -PublicKey $keys.PublicKey
$sharedSecret = Invoke-KyberDecapsulate -Ciphertext $encapsulated.Ciphertext -PrivateKey $keys.PrivateKey
```

#### Format Base64/PEM (compatible avec SSH et autres protocoles)
```powershell
# Exporter en format PEM
$keys = New-KyberKeyPair
Export-KyberKeyPair -KeyPair $keys -Path "keys.pem" -Format Base64

# Exporter uniquement la clé publique (pour la partager)
Export-KyberPublicKey -PublicKey $keys.PublicKey -Path "public_key.pem" -Format Base64

# Importer
$keys = Import-KyberKeyPair -Path "keys.pem"
$publicKey = Import-KyberPublicKey -Path "public_key.pem"
```

### Scénario 5 : Protocole type SSH (serveur/client)

```powershell
# === Côté SERVEUR ===
# 1. Générer les clés du serveur
$serverKeys = New-KyberKeyPair -ParameterSet Kyber768

# 2. Sauvegarder la clé privée (sécurisée)
Export-KyberKeyPair -KeyPair $serverKeys -Path "server_private.txt" -Format Text

# 3. Partager la clé publique avec les clients
Export-KyberPublicKey -PublicKey $serverKeys.PublicKey -Path "server_public.pem" -Format Base64

# 4. Quand un client se connecte, décapsuler la clé partagée
$serverKeys = Import-KyberKeyPair -Path "server_private.txt"
$sharedSecret = Invoke-KyberDecapsulate -Ciphertext $clientCiphertext -PrivateKey $serverKeys.PrivateKey

# === Côté CLIENT ===
# 1. Récupérer la clé publique du serveur
$serverPublicKey = Import-KyberPublicKey -Path "server_public.pem"

# 2. Encapsuler une clé partagée
$encapsulated = Invoke-KyberEncapsulate -PublicKey $serverPublicKey -ParameterSet Kyber768

# 3. Envoyer le ciphertext au serveur
# $encapsulated.Ciphertext

# 4. Utiliser la clé partagée pour la communication
# $encapsulated.SharedSecret
```

### Scénario 6 : Signature numérique avec Dilithium (100% Post-Quantum) ⭐

```powershell
# Générer une paire de clés Dilithium (ML-DSA)
$signingKeys = New-DilithiumKeyPair -ParameterSet Dilithium3

# Signer un message
$message = "Important message"
$messageBytes = [System.Text.Encoding]::UTF8.GetBytes($message)
$signature = Invoke-DilithiumSign -Data $messageBytes -PrivateKey $signingKeys.PrivateKey -ParameterSet Dilithium3

# Vérifier la signature
$isValid = Test-DilithiumSignature -Data $messageBytes -Signature $signature.Signature -PublicKey $signingKeys.PublicKey -ParameterSet Dilithium3

# Sauvegarder les clés
Export-DilithiumKeyPair -KeyPair $signingKeys -Path "signing_keys.txt" -Format Text
```

### Scénario 7 : Signature numérique avec Ed25519 (Compatibilité)
```powershell
# Générer une paire de clés Ed25519
$signingKeys = New-Ed25519KeyPair

# Signer un message
$message = "Important message"
$signature = Invoke-Ed25519Sign -Data $message -PrivateKey $signingKeys.PrivateKey

# Vérifier la signature
$isValid = Test-Ed25519Signature -Data $message -Signature $signature.Signature -PublicKey $signingKeys.PublicKey

# Sauvegarder les clés
Export-Ed25519KeyPair -KeyPair $signingKeys -Path "signing_keys.txt" -Format Text
```

### Scénario 8 : Protocole sécurisé 100% Post-Quantum : Kyber + Dilithium ⭐

Ce scénario combine les deux algorithmes post-quantiques pour créer un protocole sécurisé 100% résistant aux ordinateurs quantiques :
- **Kyber (ML-KEM)** : Pour l'échange de clés résistant aux ordinateurs quantiques
- **Dilithium (ML-DSA)** : Pour l'authentification et l'intégrité des données (post-quantique)

```powershell
# 1. Générer les clés post-quantiques
$kyberKeys = New-KyberKeyPair -ParameterSet Kyber768
$dilithiumKeys = New-DilithiumKeyPair -ParameterSet Dilithium3

# 2. Encapsuler une clé partagée avec Kyber
$encapsulated = Invoke-KyberEncapsulate -PublicKey $kyberKeys.PublicKey -ParameterSet Kyber768

# 3. Signer la clé partagée avec Dilithium
$signature = Invoke-DilithiumSign -Data $encapsulated.SharedSecret -PrivateKey $dilithiumKeys.PrivateKey -ParameterSet Dilithium3

# 4. Vérifier la signature
$isValid = Test-DilithiumSignature -Data $encapsulated.SharedSecret -Signature $signature.Signature -PublicKey $dilithiumKeys.PublicKey -ParameterSet Dilithium3

# 5. Décapsuler la clé partagée
$sharedSecret = Invoke-KyberDecapsulate -Ciphertext $encapsulated.Ciphertext -PrivateKey $kyberKeys.PrivateKey -ParameterSet Kyber768
```

**Avantages de cette combinaison** :
- 🔐 **Kyber (ML-KEM)** : Échange de clés résistant aux ordinateurs quantiques
- ✍️ **Dilithium (ML-DSA)** : Authentification et intégrité post-quantiques
- 🔑 **Clé partagée** : 32 bytes sécurisée et authentifiée
- 🛡️ **100% Post-Quantum** : Résistant aux attaques classiques et quantiques

**Scripts d'exemples** :
- `TestKyberDilithium.ps1` : Test rapide de l'intégration
- `KyberDilithiumCompleteExample.ps1` : Scénario serveur/client complet

### Scénario 9 : Hachage SHA3 avec ML-DSA

```powershell
# Calculer un hachage SHA3-256
$data = "Données à hacher"
$hash256 = Get-SHA3Hash -Data $data -Variant SHA3_256

# Calculer un hachage SHA3-384
$hash384 = Get-SHA3Hash -Data $data -Variant SHA3_384

# Signer avec Dilithium en utilisant SHA3 pré-hachage (recommandé)
$keys = New-DilithiumKeyPair -ParameterSet Dilithium3
$signature = Invoke-DilithiumSign -Data $dataBytes -PrivateKey $keys.PrivateKey -PreHash -SHA3Variant SHA3_256
```

### Scénario 10 : Chiffrement Authenticated Encryption

#### ChaCha20-Poly1305
```powershell
# Chiffrer avec ChaCha20-Poly1305
$plaintext = "Message secret"
$key = New-Object byte[] 32
# Générer une clé (ou utiliser une clé partagée Kyber)
$encrypted = Protect-WithChaCha20Poly1305 -Data $plaintext -Key $key

# Déchiffrer
$decrypted = Unprotect-ChaCha20Poly1305 -EncryptedData $encrypted.EncryptedData -Key $key -Nonce $encrypted.Nonce -Tag $encrypted.Tag
```

#### AES-GCM
```powershell
# Chiffrer avec AES-GCM
$plaintext = "Message secret"
$key = New-Object byte[] 32
$encrypted = Protect-WithAESGCM -Data $plaintext -Key $key

# Déchiffrer
$decrypted = Unprotect-AESGCM -EncryptedData $encrypted.EncryptedData -Key $key -Nonce $encrypted.Nonce -Tag $encrypted.Tag
```

### Scénario 11 : Export/Import PKCS#8 et Certificats X.509

```powershell
# Exporter une clé privée Dilithium en format PKCS#8
$keys = New-DilithiumKeyPair -ParameterSet Dilithium3
Export-PrivateKeyPKCS8 -PrivateKey $keys.PrivateKey -Path "dilithium_key.pem" -Format PEM -KeyType Dilithium -ParameterSet Dilithium3

# Importer une clé privée PKCS#8
$imported = Import-PrivateKeyPKCS8 -Path "dilithium_key.pem" -KeyType Dilithium

# Créer un certificat X.509 auto-signé avec Dilithium
$cert = New-X509Certificate -SubjectName "CN=Test Post-Quantum Certificate" -PrivateKey $keys.PrivateKey -ParameterSet Dilithium3 -Path "certificate.pem" -Format PEM

# Vérifier un certificat X.509
$isValid = Test-X509Certificate -Path "certificate.pem"
```

### Scénario 12 : Configuration TLS 1.3 Post-Quantum Hybride ⭐

Ce scénario permet de créer des configurations TLS 1.3 hybrides combinant des algorithmes classiques (ECDSA, RSA) avec des algorithmes post-quantiques (Kyber, Dilithium) pour une transition progressive vers la cryptographie post-quantique.

```powershell
# Générer une configuration TLS hybride par défaut (ECDSA-P256 + Kyber768 + Dilithium3)
$hybridConfig = New-TLS13HybridConfig

# Obtenir le nom du cipher suite
$cipherSuite = Get-TLS13HybridCipherSuite -Config $hybridConfig
Write-Host "Cipher Suite: $cipherSuite"  # TLS13-ECDSA-P256+ML-KEM-768+ML-DSA-65

# Générer une configuration avec RSA
$rsaConfig = New-TLS13HybridConfig -ClassicalAlgorithm RSA_2048 -PostQuantumKemAlgorithm Kyber768 -PostQuantumSigAlgorithm Dilithium3

# Générer une configuration haute sécurité
$highSecConfig = New-TLS13HybridConfig -ClassicalAlgorithm ECDSA_P384 -PostQuantumKemAlgorithm Kyber1024 -PostQuantumSigAlgorithm Dilithium5

# Exporter la configuration pour intégration TLS
Export-TLS13HybridConfig -Config $hybridConfig -Path "tls13_hybrid_config.txt" -Force
```

**Algorithmes classiques supportés** :
- `ECDSA_P256` : ECDSA avec courbe P-256 (NIST)
- `ECDSA_P384` : ECDSA avec courbe P-384 (NIST)
- `RSA_2048` : RSA 2048 bits
- `RSA_3072` : RSA 3072 bits

**Algorithmes post-quantiques supportés** :
- **KEM** : Kyber512, Kyber768, Kyber1024 (ML-KEM)
- **Signature** : Dilithium2, Dilithium3, Dilithium5 (ML-DSA)

**Avantages de l'hybridation** :
- 🔐 Sécurité classique éprouvée (compatibilité avec infrastructure existante)
- 🛡️ Sécurité post-quantique (résistance aux ordinateurs quantiques)
- ✅ Transition progressive vers la cryptographie post-quantique
- 🔄 Compatibilité avec bibliothèques TLS supportant les extensions post-quantiques

**Exemple complet** : Voir `TLS13HybridExamples.ps1` pour des scénarios complets.

### Scénario 13 : Benchmark de Performance Haute Précision

Le script `PerformanceBenchmark.ps1` permet de mesurer avec haute précision les performances de tous les algorithmes cryptographiques du module :

```powershell
# Exécuter le benchmark complet
.\PerformanceBenchmark.ps1
```

Le script mesure :
- ✅ Temps de génération de clés (Kyber, Dilithium, Ed25519)
- ✅ Temps d'encapsulation/décapsulation (Kyber)
- ✅ Temps de signature/vérification (Dilithium, Ed25519)
- ✅ Temps de hachage (SHA3-256, SHA3-384)
- ✅ Temps de chiffrement/déchiffrement (ChaCha20-Poly1305, AES-GCM)

**Métriques affichées** :
- **Moyenne** : Temps moyen en µs et ms (4 décimales pour ms, 2 pour µs)
- **Médiane** : Temps médian (moins sensible aux valeurs aberrantes)
- **Minimum et Maximum** : Plage de variation des temps
- **Écart-type** : Mesure de la variabilité des performances
- **Statut** : SUCCESS (toutes réussies), PARTIAL (certaines échouées), FAILED (toutes échouées)
- **Itérations** : Nombre d'itérations réussies vs total

**Caractéristiques de précision** :
- **Opérations lentes** (Kyber/Dilithium) : 10 itérations
- **Opérations rapides** (Ed25519/SHA3/AEAD) : 500-1000 itérations pour précision
- **Warm-up automatique** : Exécution préalable pour éviter les effets de cache/JIT
- **Mesures en microsecondes** : Précision maximale pour opérations rapides
- **Export CSV** : Toutes les données exportables pour analyse approfondie

**Exemple de sortie** :
```
✅ Génération de clés Ed25519:
   Moyenne: 125.50 µs (0.1255 ms)
   Médiane: 124.20 µs (0.1242 ms)
   Min: 120.10 µs (0.1201 ms) | Max: 135.30 µs (0.1353 ms)
   Écart-type: 3.45 µs (0.0035 ms)
   Itérations: 1000/1000 réussies
```

**Export CSV** :
Le script génère également un export CSV avec toutes les métriques :
- Operation, Status, Average_ms, Average_us, Median_ms, Median_us, Min_ms, Min_us, Max_ms, Max_us, StdDev_ms, StdDev_us, SuccessfulIterations, TotalIterations, Errors

### Scénario 14 : Gestion Sécurisée des Clés en Mémoire ⭐

Ce scénario démontre l'utilisation de la gestion sécurisée des clés pour protéger les clés privées en mémoire.

```powershell
# Générer une paire de clés avec gestion sécurisée
$secureKeys = New-SecureKyberKeyPair -ParameterSet Kyber768

# Utiliser les clés
$publicKey = $secureKeys.PublicKey
$privateKey = $secureKeys.PrivateKey

# Encapsuler et décapsuler
$encapsulated = Invoke-KyberEncapsulate -PublicKey $publicKey -ParameterSet Kyber768
$decapsulated = Invoke-KyberDecapsulate -Ciphertext $encapsulated.Ciphertext -PrivateKey $privateKey -ParameterSet Kyber768

# Nettoyer immédiatement la clé privée après utilisation
$secureKeys.ZeroizePrivateKey()

# Vérifier que la clé est nettoyée
$isZeroized = Test-ZeroizedKey -Key $privateKey
Write-Host "Clé nettoyée: $isZeroized"  # True
```

**Nettoyage manuel avec Clear-SecureKey** :
```powershell
# Générer des clés normales
$keys = New-KyberKeyPair -ParameterSet Kyber768

# Utiliser les clés...

# Nettoyer manuellement avec une seule passe
Clear-SecureKey -Key $keys.PrivateKey -Passes 1

# Ou avec plusieurs passes pour sécurité renforcée (protection cold boot)
Clear-SecureKey -Key $keys.PrivateKey -Passes 3 -Nullify

# Vérifier le nettoyage
$isClean = Test-ZeroizedKey -Key $keys.PrivateKey
```

**Pattern try-finally pour nettoyage garanti** :
```powershell
$keys = New-KyberKeyPair -ParameterSet Kyber768
try {
    # Utiliser les clés...
    $encapsulated = Invoke-KyberEncapsulate -PublicKey $keys.PublicKey
}
finally {
    # Nettoyer dans tous les cas (même en cas d'exception)
    Clear-SecureKey -Key $keys.PrivateKey -Passes 3 | Out-Null
}
```

**Avantages** :
- 🔐 Nettoyage automatique des clés privées
- 🛡️ Protection contre les attaques de récupération de mémoire
- ✅ Nettoyage multi-passes pour sécurité renforcée
- 🔄 Pattern Dispose pour nettoyage garanti

**Exemple complet** : Voir `SecureKeyManagementExamples.ps1` pour des scénarios complets.

### Scénario 15 : Protection contre les Canaux Auxiliaires (Side-Channel Attacks) ⭐

Ce scénario démontre l'utilisation des contre-mesures contre les attaques par canaux auxiliaires (timing attacks, cache side-channel attacks).

#### Vue d'ensemble

Les attaques par canaux auxiliaires exploitent des informations indirectes (temps d'exécution, accès au cache) pour extraire des secrets cryptographiques. Le module implémente des contre-mesures robustes pour protéger contre ces attaques.

#### Cmdlets disponibles

**1. Décapsulation sécurisée Kyber** :
```powershell
# Décapsulation avec protection side-channel
$keys = New-KyberKeyPair -ParameterSet Kyber768
$encapsulated = Invoke-KyberEncapsulate -PublicKey $keys.PublicKey -ParameterSet Kyber768

# Méthode normale
$decapsulatedNormal = Invoke-KyberDecapsulate -Ciphertext $encapsulated.Ciphertext -PrivateKey $keys.PrivateKey -ParameterSet Kyber768

# Méthode sécurisée (protection side-channel)
$decapsulatedSecure = Invoke-KyberDecapsulateSecure -Ciphertext $encapsulated.Ciphertext -PrivateKey $keys.PrivateKey -ParameterSet Kyber768

# Les résultats sont identiques, mais la méthode sécurisée protège contre les attaques
```

**2. Vérification de signature sécurisée Dilithium** :
```powershell
# Génération et signature
$keys = New-DilithiumKeyPair -ParameterSet Dilithium3
$message = "Message important à signer"
$messageBytes = [System.Text.Encoding]::UTF8.GetBytes($message)
$signature = Invoke-DilithiumSign -Data $messageBytes -PrivateKey $keys.PrivateKey -ParameterSet Dilithium3

# Vérification normale
$isValidNormal = Test-DilithiumSignature -Data $messageBytes -Signature $signature.Signature -PublicKey $keys.PublicKey -ParameterSet Dilithium3

# Vérification sécurisée (protection side-channel)
$isValidSecure = Test-DilithiumSignatureSecure -Data $messageBytes -Signature $signature.Signature -PublicKey $keys.PublicKey -ParameterSet Dilithium3
```

**3. Comparaison en temps constant** :
```powershell
# Comparaison de clés partagées en temps constant
$key1 = [byte[]]::new(32)
$key2 = [byte[]]::new(32)
# ... remplir les clés ...

# Comparaison sécurisée (protège contre timing attacks)
$areEqual = Test-ConstantTimeCompare -Array1 $key1 -Array2 $key2

# Supporte aussi les chaînes hexadécimales
$hex1 = "0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF"
$hex2 = "0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF"
$areEqual = Test-ConstantTimeCompare -Array1 $hex1 -Array2 $hex2
```

#### Protections implémentées

**1. Opérations à temps constant** :
- ✅ Pas de branches conditionnelles basées sur des données secrètes
- ✅ Comparaisons qui prennent toujours le même temps
- ✅ Masquage des variations de timing

**2. Protection contre timing attacks** :
- ✅ Toutes les comparaisons utilisent `ConstantTimeEquals()`
- ✅ Pas de sortie anticipée lors des comparaisons
- ✅ Temps d'exécution indépendant du contenu des données

**3. Protection contre cache side-channel attacks** :
- ✅ Nettoyage du cache après opérations sensibles
- ✅ Accès mémoire uniformes pour masquer les patterns
- ✅ Préchargement du cache pour éviter les variations

**4. Méthodes sécurisées** :
- `DecapsulateSecure()` : Décapsulation Kyber avec protection side-channel
- `VerifySecure()` : Vérification Dilithium avec protection side-channel
- `ConstantTimeCompareSharedSecrets()` : Comparaison de clés partagées en temps constant
- `ConstantTimeCompareSignatures()` : Comparaison de signatures en temps constant

#### Exemple d'intégration complète

```powershell
# Protocole complet avec protections side-channel
$kyberKeys = New-KyberKeyPair -ParameterSet Kyber768
$dilithiumKeys = New-DilithiumKeyPair -ParameterSet Dilithium3

# Encapsulation
$encapsulated = Invoke-KyberEncapsulate -PublicKey $kyberKeys.PublicKey -ParameterSet Kyber768

# Décapsulation sécurisée
$sharedSecret = Invoke-KyberDecapsulateSecure -Ciphertext $encapsulated.Ciphertext -PrivateKey $kyberKeys.PrivateKey -ParameterSet Kyber768

# Signature de la clé partagée
$signature = Invoke-DilithiumSign -Data $sharedSecret.SharedSecret -PrivateKey $dilithiumKeys.PrivateKey -ParameterSet Dilithium3

# Vérification sécurisée
$isValid = Test-DilithiumSignatureSecure -Data $sharedSecret.SharedSecret -Signature $signature.Signature -PublicKey $dilithiumKeys.PublicKey -ParameterSet Dilithium3
```

#### Tests de validation

Un script de tests complet est disponible : `SideChannelProtectionTests.ps1`

```powershell
# Exécuter tous les tests de protection side-channel
.\SideChannelProtectionTests.ps1
```

Les tests valident :
- ✅ Comparaisons en temps constant
- ✅ Décapsulation sécurisée Kyber
- ✅ Vérification sécurisée Dilithium
- ✅ Tests de timing (vérification que les opérations prennent un temps similaire)
- ✅ Détection de différences à toutes les positions
- ✅ Intégration complète avec protections side-channel
- ✅ Support de différentes tailles de données

**Exemple complet** : Voir `CONTRE_MESURES_SIDE_CHANNEL.md` pour la documentation technique complète.

### Scénario 16 : Audit et Validation Cryptographique ⭐

Ce scénario démontre l'utilisation des outils d'audit et de validation cryptographique pour vérifier la conformité aux standards NIST et la robustesse des implémentations.

#### Vue d'ensemble

Le module fournit des outils complets pour :
- **Tests de conformité** : Validation des tailles, formats et comportements selon les standards NIST (FIPS 203, ML-DSA)
- **Tests de fuzzing** : Génération de données corrompues pour tester la robustesse
- **Tests de validation cryptographique** : Vérification des propriétés cryptographiques

#### Cmdlets disponibles

**1. Tests de conformité NIST** :
```powershell
# Tester la conformité de tous les algorithmes
$results = Test-CryptographicConformance -Algorithm All

# Tester uniquement Kyber
$kyberResults = Test-CryptographicConformance -Algorithm Kyber -KyberParameterSet Kyber768

# Tester uniquement Dilithium
$dilithiumResults = Test-CryptographicConformance -Algorithm Dilithium -DilithiumParameterSet Dilithium3

# Afficher uniquement les tests échoués
$failed = Test-CryptographicConformance -Algorithm All -ShowOnlyFailed
```

**2. Tests de fuzzing** :
```powershell
# Exécuter des tests de fuzzing sur tous les algorithmes
$fuzzingResults = Invoke-CryptographicFuzzing -Algorithm All -Iterations 100

# Fuzzing Kyber uniquement
$kyberFuzzing = Invoke-CryptographicFuzzing -Algorithm Kyber -KyberParameterSet Kyber768 -Iterations 50

# Afficher uniquement les problèmes (crashes ou échecs)
$issues = Invoke-CryptographicFuzzing -Algorithm All -ShowOnlyIssues
```

**3. Script d'audit complet** :
```powershell
# Exécuter tous les tests d'audit
.\CryptographicAuditTests.ps1
```

#### Tests de conformité
Les tests de conformité valident :
**1. Tailles de clés et ciphertexts** :
- ✅ Clés publiques/privées Kyber selon NIST FIPS 203
- ✅ Ciphertexts et clés partagées Kyber
- ✅ Clés publiques/privées Dilithium selon NIST ML-DSA
- ✅ Signatures Dilithium
- ✅ Clés et signatures Ed25519 selon RFC 8032

**2. Propriétés cryptographiques** :
- ✅ Non-déterminisme des générations de clés
- ✅ Correspondance des clés partagées après encapsulation/décapsulation
- ✅ Correspondance des signatures avec les clés publiques
- ✅ Rejet correct de données invalides

**3. Formats et sérialisation** :
- ✅ Formats de clés conformes
- ✅ Reconstruction correcte des clés privées

#### Tests de fuzzing

Les tests de fuzzing génèrent et testent :

**1. Données corrompues** :
- Corruption de bytes individuels
- Corruption multiple de bytes
- Tailles invalides (trop courtes, trop longues)
- Patterns suspects (tous zéros, tous FF, etc.)

**2. Ciphertexts corrompus** :
- Test de décapsulation avec ciphertexts invalides
- Vérification du rejet correct des données corrompues

**3. Signatures corrompues** :
- Test de vérification avec signatures invalides
- Vérification du rejet correct des signatures corrompues

**4. Clés corrompues** :
- Test avec clés publiques/privées corrompues
- Vérification de la robustesse face aux clés invalides

#### Exemple d'utilisation complète

```powershell
# 1. Tests de conformité
Write-Host "=== Tests de Conformité ===" -ForegroundColor Cyan
$conformanceResults = Test-CryptographicConformance -Algorithm All

foreach ($result in $conformanceResults) {
    if ($result.Passed) {
        Write-Host "✅ $($result.TestName)" -ForegroundColor Green
    } else {
        Write-Host "❌ $($result.TestName)" -ForegroundColor Red
        foreach ($error in $result.Errors) {
            Write-Host "   - $error" -ForegroundColor Yellow
        }
    }
}

# 2. Tests de fuzzing
Write-Host "`n=== Tests de Fuzzing ===" -ForegroundColor Cyan
$fuzzingResults = Invoke-CryptographicFuzzing -Algorithm All -Iterations 50

foreach ($result in $fuzzingResults) {
    Write-Host "$($result.TestName):" -ForegroundColor Cyan
    Write-Host "  Total: $($result.TotalTests) | Passed: $($result.Passed) | Failed: $($result.Failed) | Crashed: $($result.Crashed)" -ForegroundColor Gray
    if ($result.Crashed -gt 0) {
        Write-Host "  ⚠️  $($result.Crashed) crash(es) détecté(s)" -ForegroundColor Yellow
    }
}

# 3. Script d'audit complet
.\CryptographicAuditTests.ps1
```

#### Résultats attendus

**Tests de conformité** :
- ✅ Tous les tests doivent passer
- ✅ Toutes les tailles doivent correspondre aux spécifications NIST
- ✅ Toutes les propriétés cryptographiques doivent être validées

**Tests de fuzzing** :
- ✅ Les données corrompues doivent être rejetées correctement
- ✅ Aucun crash ne doit se produire (exceptions gérées)
- ⚠️  Les "Failed" sont attendus car ils indiquent le rejet correct de données invalides

#### Tests de validation cryptographique

Le script `CryptographicAuditTests.ps1` inclut également :

**1. Tests d'intégrité** :
- Vérification que les clés partagées sont identiques après encapsulation/décapsulation

**2. Tests d'authentification** :
- Vérification que les signatures sont valides avec les clés publiques correspondantes

**3. Tests de rejet** :
- Vérification que les données corrompues sont correctement rejetées

**4. Tests de non-déterminisme** :
- Vérification que les clés générées sont différentes à chaque génération

**5. Tests de stress** :
- Génération multiple de clés
- Encapsulation/décapsulation multiple
- Mesure des performances sous charge

#### Interprétation des résultats

**Tests de conformité** :
- `Passed = true` : Le test est conforme aux standards
- `Passed = false` : Le test a échoué, vérifier les erreurs dans `Errors`

**Tests de fuzzing** :
- `Crashed = 0` : Aucune exception non gérée (bon signe)
- `Failed > 0` : Normal, indique le rejet correct de données invalides
- `Crashed > 0` : Problème potentiel, exceptions non gérées détectées

**Exemple complet** : Voir `CryptographicAuditTests.ps1` pour un script complet d'audit et de validation.

### Scénario 17 : Protocole sécurisé combinant Kyber + Ed25519

Ce scénario combine les deux algorithmes pour créer un protocole sécurisé complet :
- **Kyber (ML-KEM)** : Pour l'échange de clés résistant aux ordinateurs quantiques
- **Ed25519** : Pour l'authentification et l'intégrité des données

```powershell
# === Étape 1: Générer les clés ===
# Générer des clés Kyber pour l'échange de clés
$kyberKeys = New-KyberKeyPair -ParameterSet Kyber768

# Générer des clés Ed25519 pour la signature
$ed25519Keys = New-Ed25519KeyPair

# === Étape 2: Encapsuler une clé partagée avec Kyber ===
$encapsulated = Invoke-KyberEncapsulate -PublicKey $kyberKeys.PublicKey -ParameterSet Kyber768

# === Étape 3: Signer la clé partagée avec Ed25519 ===
$signature = Invoke-Ed25519Sign -Data $encapsulated.SharedSecret -PrivateKey $ed25519Keys.PrivateKey

# === Étape 4: Vérifier la signature ===
$isValid = Test-Ed25519Signature -Data $encapsulated.SharedSecret -Signature $signature.Signature -PublicKey $ed25519Keys.PublicKey

# === Étape 5: Décapsuler et vérifier ===
$decapsulated = Invoke-KyberDecapsulate -Ciphertext $encapsulated.Ciphertext -PrivateKey $kyberKeys.PrivateKey

# Vérifier que les clés partagées correspondent
if ($encapsulated.SharedSecretHex -eq $decapsulated.SharedSecretHex) {
    Write-Host "✓ Clés partagées identiques !"
}

# Vérifier la signature avec la clé décapsulée
$isValidDecapsulated = Test-Ed25519Signature -Data $decapsulated.SharedSecret -Signature $signature.Signature -PublicKey $ed25519Keys.PublicKey
```

**Exemple complet** : Voir `TestKyberEd25519.ps1` pour un scénario complet avec export/import de clés.

---

## 🔧 Détails techniques

### Tailles des clés et ciphertexts

#### Kyber (ML-KEM)

| Paramètre | Clé publique | Clé privée | Ciphertext | Clé partagée |
|-----------|-------------|------------|------------|--------------|
| Kyber512  | 800 bytes   | 1632 bytes | 768 bytes  | 32 bytes     |
| Kyber768  | 1184 bytes  | 2400 bytes | 1088 bytes | 32 bytes     |
| Kyber1024 | 1568 bytes  | 3168 bytes | 1568 bytes | 32 bytes     |

#### Dilithium (ML-DSA) - 100% Post-Quantum ⭐

| Paramètre | Clé publique | Clé privée | Signature | Niveau NIST |
|-----------|--------------|------------|-----------|-------------|
| Dilithium2 (ML-DSA-44) | ~1312 bytes | ~2560 bytes | ~2420 bytes | Niveau 1 |
| Dilithium3 (ML-DSA-65) | ~1952 bytes | ~4032 bytes | ~3309 bytes | Niveau 2 |
| Dilithium5 (ML-DSA-87) | ~2592 bytes | ~4864 bytes | ~4627 bytes | Niveau 3 |

#### Ed25519 (Compatibilité)

| Type | Taille |
|------|--------|
| Clé publique | 32 bytes |
| Clé privée | 32 bytes |
| Signature | 64 bytes |

**Remarque** : Ed25519 est un algorithme de signature numérique classique (non post-quantique) mais très performant et largement utilisé. Il est recommandé pour l'authentification et l'intégrité des données, tandis que Kyber est utilisé pour l'échange de clés résistant aux ordinateurs quantiques.

### Sérialisation de la clé privée

Le module utilise un format de sérialisation personnalisé pour éviter les problèmes avec `GetEncoded()` de BouncyCastle :

```
Format: [Magic(4)] [sLength(4)]s [hpkLength(4)]hpk [nonceLength(4)]nonce [tLength(4)]t [rhoLength(4)]rho

Magic = 0x4B594245 ("KYBE" en ASCII)
```

**Avantages** :
- Format fiable et reproductible
- Reconstruction correcte des composants
- Compatible avec tous les paramètres Kyber

### Architecture de sécurité

- **Kyber512** : Niveau de sécurité équivalent à AES-128 (résistant aux attaques classiques et quantiques)
- **Kyber768** : Niveau de sécurité équivalent à AES-192 (recommandé pour la plupart des applications)
- **Kyber1024** : Niveau de sécurité équivalent à AES-256 (niveau de sécurité maximal)

### Gestion de la mémoire

- Les clés sont stockées en mémoire comme `byte[]`
- Les conversions hexadécimales sont effectuées à la demande
- **Gestion sécurisée disponible** : Utilisez `New-Secure*KeyPair` pour un nettoyage automatique des clés privées
- **Nettoyage manuel** : Utilisez `Clear-SecureKey` pour nettoyer manuellement les clés après utilisation
- **Vérification** : Utilisez `Test-ZeroizedKey` pour vérifier si une clé a été nettoyée

#### Gestion Sécurisée des Clés ⭐

Le module fournit des cmdlets et classes pour la gestion sécurisée des clés en mémoire :

**Cmdlets sécurisés** :
- `New-SecureKyberKeyPair` : Génère une paire de clés Kyber avec nettoyage automatique
- `New-SecureDilithiumKeyPair` : Génère une paire de clés Dilithium avec nettoyage automatique
- `New-SecureEd25519KeyPair` : Génère une paire de clés Ed25519 avec nettoyage automatique
- `Clear-SecureKey` : Nettoie manuellement une clé en mémoire (zeroization)
- `Test-ZeroizedKey` : Vérifie si une clé a été nettoyée
**Caractéristiques** :
- ✅ Nettoyage automatique lors de la destruction de l'objet (IDisposable)
- ✅ Nettoyage multi-passes (protection contre cold boot attacks)
- ✅ Thread-safe avec verrous
- ✅ Finalizer pour nettoyage même si Dispose() n'est pas appelé

**Exemple** :
```powershell
# Génération sécurisée
$secureKeys = New-SecureKyberKeyPair -ParameterSet Kyber768

# Utiliser les clés
$publicKey = $secureKeys.PublicKey
$privateKey = $secureKeys.PrivateKey

# Nettoyer immédiatement après utilisation
$secureKeys.ZeroizePrivateKey()

# Ou laisser le nettoyage automatique (garbage collector)
```

**Voir** : `GESTION_SECURISEE_CLES.md` pour la documentation complète.

#### Protection contre les Canaux Auxiliaires ⭐

Le module fournit des contre-mesures contre les attaques par canaux auxiliaires :

**Classes de protection** :
- `ConstantTimeOperations` : Opérations à temps constant (protection timing attacks)
- `SideChannelProtection` : Protection contre cache side-channel attacks

**Cmdlets sécurisés** :
- `Invoke-KyberDecapsulateSecure` : Décapsulation avec protection side-channel
- `Test-DilithiumSignatureSecure` : Vérification de signature avec protection side-channel
- `Test-ConstantTimeCompare` : Comparaison en temps constant

**Caractéristiques** :
- ✅ Opérations à temps constant (pas de branches conditionnelles basées sur données secrètes)
- ✅ Nettoyage du cache après opérations sensibles
- ✅ Accès mémoire uniformes pour masquer les patterns
- ✅ Protection contre timing attacks et cache side-channel attacks

**Exemple** :
```powershell
# Décapsulation sécurisée
$sharedSecret = Invoke-KyberDecapsulateSecure -Ciphertext $ciphertext -PrivateKey $privateKey

# Vérification sécurisée
$isValid = Test-DilithiumSignatureSecure -Data $data -Signature $signature -PublicKey $publicKey

# Comparaison en temps constant
$areEqual = Test-ConstantTimeCompare -Array1 $key1 -Array2 $key2
```

**Voir** : `CONTRE_MESURES_SIDE_CHANNEL.md` pour la documentation complète.

---

## 🐛 Dépannage

### Problème : Module non trouvé

**Erreur** : `The specified module 'KyberModule' was not loaded`

**Solution** :
```powershell
# Vérifier que le module est dans le bon répertoire
Test-Path ".\KyberModule.psd1"

# Importer avec le chemin complet
Import-Module ".\KyberModule.psd1" -Force
```

### Problème : DLL manquante

**Erreur** : `Could not load file or assembly 'BouncyCastle.Crypto'`

**Solution** :
1. Vérifier que `BouncyCastle.Crypto.dll` est dans le même répertoire que `KyberModule.dll`
2. Vérifier que la version est 2.2.1 ou compatible

### Problème : Clés partagées ne correspondent pas

**Symptôme** : Les clés partagées après encapsulation/décapsulation sont différentes

**Causes possibles** :
- Mauvais paramètre `-ParameterSet` entre encapsulation et décapsulation
- Clé publique et clé privée ne correspondent pas
- Ciphertext corrompu

**Solution** :
```powershell
# Vérifier que les paramètres correspondent
$keys = New-KyberKeyPair -ParameterSet Kyber768
$encapsulated = Invoke-KyberEncapsulate -PublicKey $keys.PublicKey -ParameterSet Kyber768
$decapsulated = Invoke-KyberDecapsulate -Ciphertext $encapsulated.Ciphertext -PrivateKey $keys.PrivateKey -ParameterSet Kyber768
```

### Problème : Erreur de compilation

**Erreur** : `error CS8370: La fonctionnalité 'modèles récursifs' n'est pas disponible`

**Solution** : Vérifier que `LangVersion` est défini à `latest` dans les fichiers `.csproj`

---

## 📚 Ressources

- **NIST FIPS 203** : Standard ML-KEM (Module-Lattice-Based Key-Encapsulation Mechanism)
- **BouncyCastle Documentation** : https://www.bouncycastle.org/documentation.html
- **PowerShell Standard Library** : https://github.com/PowerShell/PowerShellStandard

### 📖 Documentation Technique

Pour une documentation complète sur la recherche et le développement du projet, consultez :
- **`RECHERCHE_ET_DEVELOPPEMENT.md`** : Document annexe détaillant toutes les étapes, recherches, problèmes rencontrés et solutions trouvées lors du développement du module.
- **`ANALYSE_DILITHIUM.md`** : Analyse complète de l'intégration de CRYSTALS-Dilithium (ML-DSA) dans le projet.
- **`MIGRATION_BOUNCYCASTLE.md`** : Guide de migration de BouncyCastle.NetCore vers BouncyCastle.Cryptography 2.6.2.

### 🎯 Fonctionnalités Avancées

#### Fonctions de Hachage SHA3
- **SHA3-256** : Hachage 256 bits recommandé pour ML-DSA
- **SHA3-384** : Hachage 384 bits pour sécurité renforcée
- **Pré-hachage optionnel** : Intégration dans les workflows Dilithium

#### Chiffrement Authenticated Encryption (AEAD)
- **ChaCha20-Poly1305** : Algorithme moderne et performant
- **AES-GCM** : Standard industrie pour chiffrement authentifié
- **Authentification intégrée** : Garantit l'intégrité et l'authenticité des données

#### Conformité Standards PKI
- **PKCS#8** : Export/import de clés privées en format standard (DER/PEM)
- **X.509** : Création et vérification de certificats auto-signés avec clés post-quantiques
- **CMS** : Support des enveloppes chiffrées (signatures en développement)

---

## 📝 Licence

Ce module utilise BouncyCastle.NetCore qui est sous licence MIT.

---

## 👥 Contribution

Pour contribuer au projet :
1. Fork le repository
2. Créer une branche pour votre fonctionnalité
3. Faire vos modifications
4. Tester avec `Test.ps1`
5. Créer une pull request

---

## 📧 Support

Pour toute question ou problème, consultez :
- La documentation BouncyCastle
- Les exemples dans `Examples.ps1`
- Les tests dans `Test.ps1`

---

**Version** : 1.1.0  
**Dernière mise à jour** : Décembre 2025

---

## 📖 Guide d'utilisation

### Communications distantes (KyberDaemon + KyberCLI)
1. **S'assurer que le service est démarré** (voir section Installation).
2. **Utiliser KyberCLI directement** :
   ```powershell
   dotnet KyberCLI.dll connect --host 192.168.1.10 --user alice --password 0x09AF... --command "hostname"
   dotnet KyberCLI.dll connect --host 192.168.1.10 --user bob --key C:\secrets\bob.key
   ```
3. **Depuis PowerShell** avec la cmdlet `Invoke-SecureCommand` (wrapper KyberCLI) :
   ```powershell
   Invoke-SecureCommand \
       -ServerHost 192.168.1.10 \
       -Port 8443 \
       -Username alice \
       -PasswordHex 0x09AF... \
       -Command "Get-Process | Select-Object -First 5 Name,Id"
   ```
   Paramètres supplémentaires :
   - `-KeyPath` : clé privée Dilithium (base64) pour signature
   - `-CliPath` : binaire/dll KyberCLI personnalisé
   - `-RebuildCli` : force `dotnet publish` avant exécution

> ⚠️ `Start-SecureServer`, `Connect-SecureClient`, `Send/Receive-SecureMessage` sont conservés pour compatibilité mais renvoient désormais une erreur explicite. Utilisez KyberDaemon/KyberCLI pour toute communication réseau.

### Communication

| Cmdlet | Statut | Description |
|--------|--------|-------------|
| `Invoke-SecureCommand` | ✅ Actif | Exécute une commande distante via KyberCLI (KyberDaemon requis) |
| `Start-SecureServer` | ⚠️ Obsolète | Ancienne implémentation serveur (renvoie une erreur guidant vers KyberDaemon) |
| `Connect-SecureClient` | ⚠️ Obsolète | Ancien client PowerShell (renvoie une erreur guidant vers KyberCLI) |
| `Send-SecureMessage` | ⚠️ Obsolète | Ancienne commande d'envoi (renvoie une erreur guidant vers KyberCLI) |
| `Receive-SecureMessage` | ⚠️ Obsolète | Ancienne commande de réception (renvoie une erreur guidant vers KyberCLI) |

---

## 📖 Outils d'administration
- `KyberCLI keys list --config /etc/kyberd/kyberd.conf` : affiche les versions de clés stockées (répertoires `session.vN.*`)
- `KyberCLI keys rotate --name session --days 90` : force la rotation immédiate (respecte la passphrase du `kyberd.conf`)
- `KyberCLI keys export --output metadata.json` : extrait les métadonnées (versions, modes de chiffrement)
- Script PowerShell dédié (`KyberDaemon/tools/Invoke-KyberKeyRotation.ps1`) enveloppant `KyberCLI keys` pour Windows / PowerShell Core (`-Action List|Rotate|Export`, `-Config`, `-Name`, `-Days`, `-CliPath`...)

### Tests & Qualité
- Tests unitaires/crypto historiques (scripts PowerShell dans `KyberModule/`)
- Tests d'intégration end-to-end (`dotnet test tests/KyberIntegrationTests`) : démarre un daemon éphémère, exécute KyberCLI et injecte du trafic corrompu pour valider la robustesse réseau
- Fuzzer réseau simple (`NetworkFuzzer.SendRandomHandshakeAsync`) utilisable depuis les tests ou un harness personnalisé pour enrichir votre campagne fuzz

### Benchmarks
- Projet `tests/KyberBenchmarks` (BenchmarkDotNet)
  - `CommandLatencyBenchmark` mesure la latence moyenne d'une commande `echo`
  - `HandshakeBenchmark` quantifie le coût handshake/fermeture d'une session complète
  - `ConcurrentCommandsBenchmark` ouvre plusieurs clients en parallèle (`Params(2,4,8)`) pour évaluer la tenue aux connexions simultanées
- Exécution : `dotnet run -c Release -p tests/KyberBenchmarks`
- Résultats générés (fichier markdown/csv) dans `BenchmarkDotNet.Artifacts`

---

## 📖 Guide d'utilisation

### Scénario 1 : Génération de clés

```powershell
# Générer une paire de clés avec Kyber768 (par défaut)
$keys = New-KyberKeyPair

# Spécifier le niveau de sécurité
$keys512 = New-KyberKeyPair -ParameterSet Kyber512
$keys768 = New-KyberKeyPair -ParameterSet Kyber768
$keys1024 = New-KyberKeyPair -ParameterSet Kyber1024

# Accéder aux clés
Write-Host "Clé publique (hex): $($keys.PublicKeyHex)"
Write-Host "Clé privée (hex): $($keys.PrivateKeyHex)"
```

### Scénario 2 : Échange de clés (Alice et Bob)

```powershell
# Alice génère sa paire de clés
$aliceKeys = New-KyberKeyPair -ParameterSet Kyber768

# Bob utilise la clé publique d'Alice pour créer une clé partagée
$bobEncapsulated = Invoke-KyberEncapsulate -PublicKey $aliceKeys.PublicKey

# Bob envoie le ciphertext à Alice
# $ciphertext = $bobEncapsulated.Ciphertext

# Alice décapsule pour obtenir la même clé partagée
$aliceSharedSecret = Invoke-KyberDecapsulate -Ciphertext $bobEncapsulated.Ciphertext -PrivateKey $aliceKeys.PrivateKey

# Vérifier que les clés correspondent
$match = $bobEncapsulated.SharedSecret -eq $aliceSharedSecret.SharedSecret
```

### Scénario 3 : Utilisation avec des chaînes hexadécimales

```powershell
# Générer des clés
$keys = New-KyberKeyPair

# Utiliser directement les chaînes hex
$encapsulated = Invoke-KyberEncapsulate -PublicKey $keys.PublicKeyHex

# Décapsuler avec les chaînes hex
$sharedSecret = Invoke-KyberDecapsulate -Ciphertext $encapsulated.CiphertextHex -PrivateKey $keys.PrivateKeyHex
```

### Scénario 4 : Sauvegarder et charger des clés

#### Format Text (recommandé pour sauvegarder les paires de clés - sécurisé)
```powershell
# Générer et sauvegarder
$keys = New-KyberKeyPair
Export-KyberKeyPair -KeyPair $keys -Path "keys.txt" -Format Text

# Charger plus tard
$keys = Import-KyberKeyPair -Path "keys.txt"

# Utiliser
$encapsulated = Invoke-KyberEncapsulate -PublicKey $keys.PublicKey
$sharedSecret = Invoke-KyberDecapsulate -Ciphertext $encapsulated.Ciphertext -PrivateKey $keys.PrivateKey
```

#### Format Base64/PEM (compatible avec SSH et autres protocoles)
```powershell
# Exporter en format PEM
$keys = New-KyberKeyPair
Export-KyberKeyPair -KeyPair $keys -Path "keys.pem" -Format Base64

# Exporter uniquement la clé publique (pour la partager)
Export-KyberPublicKey -PublicKey $keys.PublicKey -Path "public_key.pem" -Format Base64

# Importer
$keys = Import-KyberKeyPair -Path "keys.pem"
$publicKey = Import-KyberPublicKey -Path "public_key.pem"
```

### Scénario 5 : Protocole type SSH (serveur/client)

```powershell
# === Côté SERVEUR ===
# 1. Générer les clés du serveur
$serverKeys = New-KyberKeyPair -ParameterSet Kyber768

# 2. Sauvegarder la clé privée (sécurisée)
Export-KyberKeyPair -KeyPair $serverKeys -Path "server_private.txt" -Format Text

# 3. Partager la clé publique avec les clients
Export-KyberPublicKey -PublicKey $serverKeys.PublicKey -Path "server_public.pem" -Format Base64

# 4. Quand un client se connecte, décapsuler la clé partagée
$serverKeys = Import-KyberKeyPair -Path "server_private.txt"
$sharedSecret = Invoke-KyberDecapsulate -Ciphertext $clientCiphertext -PrivateKey $serverKeys.PrivateKey

# === Côté CLIENT ===
# 1. Récupérer la clé publique du serveur
$serverPublicKey = Import-KyberPublicKey -Path "server_public.pem"

# 2. Encapsuler une clé partagée
$encapsulated = Invoke-KyberEncapsulate -PublicKey $serverPublicKey -ParameterSet Kyber768

# 3. Envoyer le ciphertext au serveur
# $encapsulated.Ciphertext

# 4. Utiliser la clé partagée pour la communication
# $encapsulated.SharedSecret
```

### Scénario 6 : Signature numérique avec Dilithium (100% Post-Quantum) ⭐

```powershell
# Générer une paire de clés Dilithium (ML-DSA)
$signingKeys = New-DilithiumKeyPair -ParameterSet Dilithium3

# Signer un message
$message = "Important message"
$messageBytes = [System.Text.Encoding]::UTF8.GetBytes($message)
$signature = Invoke-DilithiumSign -Data $messageBytes -PrivateKey $signingKeys.PrivateKey -ParameterSet Dilithium3

# Vérifier la signature
$isValid = Test-DilithiumSignature -Data $messageBytes -Signature $signature.Signature -PublicKey $signingKeys.PublicKey -ParameterSet Dilithium3

# Sauvegarder les clés
Export-DilithiumKeyPair -KeyPair $signingKeys -Path "signing_keys.txt" -Format Text
```

### Scénario 7 : Signature numérique avec Ed25519 (Compatibilité)

```powershell
# Générer une paire de clés Ed25519
$signingKeys = New-Ed25519KeyPair

# Signer un message
$message = "Important message"
$signature = Invoke-Ed25519Sign -Data $message -PrivateKey $signingKeys.PrivateKey

# Vérifier la signature
$isValid = Test-Ed25519Signature -Data $message -Signature $signature.Signature -PublicKey $signingKeys.PublicKey

# Sauvegarder les clés
Export-Ed25519KeyPair -KeyPair $signingKeys -Path "signing_keys.txt" -Format Text
```

### Scénario 8 : Protocole sécurisé 100% Post-Quantum : Kyber + Dilithium ⭐

Ce scénario combine les deux algorithmes post-quantiques pour créer un protocole sécurisé 100% résistant aux ordinateurs quantiques :
- **Kyber (ML-KEM)** : Pour l'échange de clés résistant aux ordinateurs quantiques
- **Dilithium (ML-DSA)** : Pour l'authentification et l'intégrité des données (post-quantique)

```powershell
# 1. Générer les clés post-quantiques
$kyberKeys = New-KyberKeyPair -ParameterSet Kyber768
$dilithiumKeys = New-DilithiumKeyPair -ParameterSet Dilithium3

# 2. Encapsuler une clé partagée avec Kyber
$encapsulated = Invoke-KyberEncapsulate -PublicKey $kyberKeys.PublicKey -ParameterSet Kyber768

# 3. Signer la clé partagée avec Dilithium
$signature = Invoke-DilithiumSign -Data $encapsulated.SharedSecret -PrivateKey $dilithiumKeys.PrivateKey -ParameterSet Dilithium3

# 4. Vérifier la signature
$isValid = Test-DilithiumSignature -Data $encapsulated.SharedSecret -Signature $signature.Signature -PublicKey $dilithiumKeys.PublicKey -ParameterSet Dilithium3

# 5. Décapsuler la clé partagée
$sharedSecret = Invoke-KyberDecapsulate -Ciphertext $encapsulated.Ciphertext -PrivateKey $kyberKeys.PrivateKey -ParameterSet Kyber768
```

**Avantages de cette combinaison** :
- 🔐 **Kyber (ML-KEM)** : Échange de clés résistant aux ordinateurs quantiques
- ✍️ **Dilithium (ML-DSA)** : Authentification et intégrité post-quantiques
- 🔑 **Clé partagée** : 32 bytes sécurisée et authentifiée
- 🛡️ **100% Post-Quantum** : Résistant aux attaques classiques et quantiques

**Scripts d'exemples** :
- `TestKyberDilithium.ps1` : Test rapide de l'intégration
- `KyberDilithiumCompleteExample.ps1` : Scénario serveur/client complet

### Scénario 9 : Hachage SHA3 avec ML-DSA

```powershell
# Calculer un hachage SHA3-256
$data = "Données à hacher"
$hash256 = Get-SHA3Hash -Data $data -Variant SHA3_256

# Calculer un hachage SHA3-384
$hash384 = Get-SHA3Hash -Data $data -Variant SHA3_384

# Signer avec Dilithium en utilisant SHA3 pré-hachage (recommandé)
$keys = New-DilithiumKeyPair -ParameterSet Dilithium3
$signature = Invoke-DilithiumSign -Data $dataBytes -PrivateKey $keys.PrivateKey -PreHash -SHA3Variant SHA3_256
```

### Scénario 10 : Chiffrement Authenticated Encryption

#### ChaCha20-Poly1305
```powershell
# Chiffrer avec ChaCha20-Poly1305
$plaintext = "Message secret"
$key = New-Object byte[] 32
# Générer une clé (ou utiliser une clé partagée Kyber)
$encrypted = Protect-WithChaCha20Poly1305 -Data $plaintext -Key $key

# Déchiffrer
$decrypted = Unprotect-ChaCha20Poly1305 -EncryptedData $encrypted.EncryptedData -Key $key -Nonce $encrypted.Nonce -Tag $encrypted.Tag
```

#### AES-GCM
```powershell
# Chiffrer avec AES-GCM
$plaintext = "Message secret"
$key = New-Object byte[] 32
$encrypted = Protect-WithAESGCM -Data $plaintext -Key $key

# Déchiffrer
$decrypted = Unprotect-AESGCM -EncryptedData $encrypted.EncryptedData -Key $key -Nonce $encrypted.Nonce -Tag $encrypted.Tag
```

### Scénario 11 : Export/Import PKCS#8 et Certificats X.509

```powershell
# Exporter une clé privée Dilithium en format PKCS#8
$keys = New-DilithiumKeyPair -ParameterSet Dilithium3
Export-PrivateKeyPKCS8 -PrivateKey $keys.PrivateKey -Path "dilithium_key.pem" -Format PEM -KeyType Dilithium -ParameterSet Dilithium3

# Importer une clé privée PKCS#8
$imported = Import-PrivateKeyPKCS8 -Path "dilithium_key.pem" -KeyType Dilithium

# Créer un certificat X.509 auto-signé avec Dilithium
$cert = New-X509Certificate -SubjectName "CN=Test Post-Quantum Certificate" -PrivateKey $keys.PrivateKey -ParameterSet Dilithium3 -Path "certificate.pem" -Format PEM

# Vérifier un certificat X.509
$isValid = Test-X509Certificate -Path "certificate.pem"
```

### Scénario 12 : Configuration TLS 1.3 Post-Quantum Hybride ⭐

Ce scénario permet de créer des configurations TLS 1.3 hybrides combinant des algorithmes classiques (ECDSA, RSA) avec des algorithmes post-quantiques (Kyber, Dilithium) pour une transition progressive vers la cryptographie post-quantique.

```powershell
# Générer une configuration TLS hybride par défaut (ECDSA-P256 + Kyber768 + Dilithium3)
$hybridConfig = New-TLS13HybridConfig

# Obtenir le nom du cipher suite
$cipherSuite = Get-TLS13HybridCipherSuite -Config $hybridConfig
Write-Host "Cipher Suite: $cipherSuite"  # TLS13-ECDSA-P256+ML-KEM-768+ML-DSA-65

# Générer une configuration avec RSA
$rsaConfig = New-TLS13HybridConfig -ClassicalAlgorithm RSA_2048 -PostQuantumKemAlgorithm Kyber768 -PostQuantumSigAlgorithm Dilithium3

# Générer une configuration haute sécurité
$highSecConfig = New-TLS13HybridConfig -ClassicalAlgorithm ECDSA_P384 -PostQuantumKemAlgorithm Kyber1024 -PostQuantumSigAlgorithm Dilithium5

# Exporter la configuration pour intégration TLS
Export-TLS13HybridConfig -Config $hybridConfig -Path "tls13_hybrid_config.txt" -Force
```

**Algorithmes classiques supportés** :
- `ECDSA_P256` : ECDSA avec courbe P-256 (NIST)
- `ECDSA_P384` : ECDSA avec courbe P-384 (NIST)
- `RSA_2048` : RSA 2048 bits
- `RSA_3072` : RSA 3072 bits

**Algorithmes post-quantiques supportés** :
- **KEM** : Kyber512, Kyber768, Kyber1024 (ML-KEM)
- **Signature** : Dilithium2, Dilithium3, Dilithium5 (ML-DSA)

**Avantages de l'hybridation** :
- 🔐 Sécurité classique éprouvée (compatibilité avec infrastructure existante)
- 🛡️ Sécurité post-quantique (résistance aux ordinateurs quantiques)
- ✅ Transition progressive vers la cryptographie post-quantique
- 🔄 Compatibilité avec bibliothèques TLS supportant les extensions post-quantiques

**Exemple complet** : Voir `TLS13HybridExamples.ps1` pour des scénarios complets.

### Scénario 13 : Benchmark de Performance Haute Précision

Le script `PerformanceBenchmark.ps1` permet de mesurer avec haute précision les performances de tous les algorithmes cryptographiques du module :

```powershell
# Exécuter le benchmark complet
.\PerformanceBenchmark.ps1
```

Le script mesure :
- ✅ Temps de génération de clés (Kyber, Dilithium, Ed25519)
- ✅ Temps d'encapsulation/décapsulation (Kyber)
- ✅ Temps de signature/vérification (Dilithium, Ed25519)
- ✅ Temps de hachage (SHA3-256, SHA3-384)
- ✅ Temps de chiffrement/déchiffrement (ChaCha20-Poly1305, AES-GCM)

**Métriques affichées** :
- **Moyenne** : Temps moyen en µs et ms (4 décimales pour ms, 2 pour µs)
- **Médiane** : Temps médian (moins sensible aux valeurs aberrantes)
- **Minimum et Maximum** : Plage de variation des temps
- **Écart-type** : Mesure de la variabilité des performances
- **Statut** : SUCCESS (toutes réussies), PARTIAL (certaines échouées), FAILED (toutes échouées)
- **Itérations** : Nombre d'itérations réussies vs total

**Caractéristiques de précision** :
- **Opérations lentes** (Kyber/Dilithium) : 10 itérations
- **Opérations rapides** (Ed25519/SHA3/AEAD) : 500-1000 itérations pour précision
- **Warm-up automatique** : Exécution préalable pour éviter les effets de cache/JIT
- **Mesures en microsecondes** : Précision maximale pour opérations rapides
- **Export CSV** : Toutes les données exportables pour analyse approfondie

**Exemple de sortie** :
```
✅ Génération de clés Ed25519:
   Moyenne: 125.50 µs (0.1255 ms)
   Médiane: 124.20 µs (0.1242 ms)
   Min: 120.10 µs (0.1201 ms) | Max: 135.30 µs (0.1353 ms)
   Écart-type: 3.45 µs (0.0035 ms)
   Itérations: 1000/1000 réussies
```

**Export CSV** :
Le script génère également un export CSV avec toutes les métriques :
- Operation, Status, Average_ms, Average_us, Median_ms, Median_us, Min_ms, Min_us, Max_ms, Max_us, StdDev_ms, StdDev_us, SuccessfulIterations, TotalIterations, Errors

### Scénario 14 : Gestion Sécurisée des Clés en Mémoire ⭐

Ce scénario démontre l'utilisation de la gestion sécurisée des clés pour protéger les clés privées en mémoire.

```powershell
# Générer une paire de clés avec gestion sécurisée
$secureKeys = New-SecureKyberKeyPair -ParameterSet Kyber768

# Utiliser les clés
$publicKey = $secureKeys.PublicKey
$privateKey = $secureKeys.PrivateKey

# Encapsuler et décapsuler
$encapsulated = Invoke-KyberEncapsulate -PublicKey $publicKey -ParameterSet Kyber768
$decapsulated = Invoke-KyberDecapsulate -Ciphertext $encapsulated.Ciphertext -PrivateKey $privateKey -ParameterSet Kyber768

# Nettoyer immédiatement la clé privée après utilisation
$secureKeys.ZeroizePrivateKey()
# Vérifier que la clé est nettoyée
$isZeroized = Test-ZeroizedKey -Key $privateKey
Write-Host "Clé nettoyée: $isZeroized"  # True