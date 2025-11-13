# Protocole de Communication Sécurisée Post-Quantique

## 📋 Table des matières

1. [Vue d'ensemble](#vue-densemble)
2. [Architecture du protocole](#architecture-du-protocole)
3. [Établissement de session](#établissement-de-session)
4. [Authentification mutuelle](#authentification-mutuelle)
5. [Communication chiffrée](#communication-chiffrée)
6. [Exécution de commandes à distance](#exécution-de-commandes-à-distance)
7. [Format des paquets](#format-des-paquets)
8. [Guide d'utilisation](#guide-dutilisation)
9. [Sécurité](#sécurité)
10. [Références](#références)

---

## 🎯 Vue d'ensemble

Le **Protocole de Communication Sécurisée Post-Quantique** est un système complet permettant à deux machines de communiquer de manière sécurisée en utilisant exclusivement des algorithmes cryptographiques post-quantiques conformes aux standards NIST.

### Caractéristiques principales

- ✅ **100% Post-Quantique** : Utilise uniquement des algorithmes résistants aux ordinateurs quantiques
- ✅ **Échange de clés Kyber (ML-KEM)** : Établissement sécurisé de clés partagées
- ✅ **Authentification Dilithium (ML-DSA)** : Authentification mutuelle post-quantique
- ✅ **Chiffrement authentifié** : ChaCha20-Poly1305 ou AES-GCM pour la communication
- ✅ **Exécution de commandes à distance** : Système de commandes sécurisées (style SSH)
- ✅ **Session persistante** : Connexion maintenue ouverte pour plusieurs commandes
- ✅ **Protection side-channel** : Contre-mesures contre les attaques par canaux auxiliaires
- ✅ **Gestion sécurisée des clés** : Zeroization automatique des clés sensibles
- ✅ **Conformité NIST** : Respect des standards FIPS 203 (ML-KEM) et ML-DSA

### Algorithmes utilisés

| Composant | Algorithme | Standard | Niveau de sécurité |
|-----------|-----------|----------|-------------------|
| Échange de clés | Kyber768 (ML-KEM) | NIST FIPS 203 | Niveau 2 (équivalent AES-192) |
| Authentification | Dilithium3 (ML-DSA-65) | NIST ML-DSA | Niveau 2 |
| Chiffrement | ChaCha20-Poly1305 | RFC 8439 | 256 bits |
| Alternative | AES-GCM | NIST SP 800-38D | 256 bits |
| Hachage | SHA3-256 | NIST FIPS 202 | 256 bits |

---

## 🏗️ Architecture du protocole

### Structure en couches

```
┌─────────────────────────────────────────────────────────┐
│   Application (Cmdlets PowerShell)                      │
│   - Start-SecureServer                                  │
│   - Connect-SecureClient                                │
│   - Send-SecureMessage                                  │
│   - Receive-SecureMessage                               │
│   - Invoke-SecureCommand                                │
└─────────────────┬───────────────────────────────────────┘
                  │
                  ▼
┌─────────────────────────────────────────────────────────┐
│   Protocole de Communication                            │
│   - SecureCommunicationClient                           │
│   - SecureCommunicationServer                           │
│   - SecureProtocolPacket                                │
│   - SecureCommandProtocol                               │
│   - CommandExecutor                                     │
└─────────────────┬───────────────────────────────────────┘
                  │
                  ▼
┌─────────────────────────────────────────────────────────┐
│   Session Sécurisée                                     │
│   - SecureSession                                       │
│   - Gestion des clés (Kyber + Dilithium)                │
│   - Chiffrement/Déchiffrement                          │
└─────────────────┬───────────────────────────────────────┘
                  │
                  ▼
┌─────────────────────────────────────────────────────────┐
│   Cryptographie Post-Quantique                          │
│   - KyberWrapper (ML-KEM)                               │
│   - DilithiumWrapper (ML-DSA)                           │
│   - ChaCha20Poly1305Wrapper / AESGCMWrapper             │
└─────────────────┬───────────────────────────────────────┘
                  │
                  ▼
┌─────────────────────────────────────────────────────────┐
│   BouncyCastle.Cryptography 2.6.2                      │
│   - Implémentation NIST conforme                       │
└─────────────────────────────────────────────────────────┘
```

### Flux de communication

```
CLIENT                                    SERVEUR
  │                                         │
  │  1. KeyExchangeRequest                 │
  │     (Clé publique Kyber client)        │
  ├───────────────────────────────────────>│
  │                                         │
  │  2. KeyExchangeResponse                │
  │     (Clé publique Kyber serveur)       │
  │<───────────────────────────────────────┤
  │                                         │
  │  3. KeyExchangeAck                     │
  │     (Ciphertext Kyber)                 │
  ├───────────────────────────────────────>│
  │     [Échange de clés terminé]          │
  │                                         │
  │  4. AuthRequest                        │
  │     (Challenge + Signature Dilithium)  │
  ├───────────────────────────────────────>│
  │                                         │
  │  5. AuthResponse                       │
  │     (Challenge serveur + Signature)    │
  │<───────────────────────────────────────┤
  │                                         │
  │  6. AuthSuccess                        │
  │     (Accusé d'authentification)       │
  ├───────────────────────────────────────>│
  │     [Authentification terminée]        │
  │                                         │
  │  7. EncryptedData                      │
  │     (Message chiffré)                 │
  ├───────────────────────────────────────>│
  │                                         │
  │  8. EncryptedResponse                  │
  │     (Réponse chiffrée)                │
  │<───────────────────────────────────────┤
  │                                         │
  │  9. EncryptedCommand                   │
  │     (Commande chiffrée)                │
  ├───────────────────────────────────────>│
  │                                         │
  │  10. EncryptedResponse                │
  │      (Résultat chiffré)                │
  │<───────────────────────────────────────┤
  │     [Session reste ouverte]            │
```

---

## 🔐 Établissement de session

### Phase 1 : Échange de clés Kyber (ML-KEM)

L'échange de clés utilise **Kyber (ML-KEM)** pour établir une clé partagée de 32 bytes de manière sécurisée.

#### Côté Client

1. **Génération des clés** :
   ```csharp
   session.GenerateKyberKeys();
   // Génère KyberPublicKey et KyberPrivateKey
   ```

2. **Envoi de la clé publique** :
   - Le client envoie sa clé publique Kyber au serveur dans un paquet `KeyExchangeRequest`

3. **Réception de la clé publique du serveur** :
   - Le serveur répond avec sa clé publique Kyber dans un paquet `KeyExchangeResponse`

4. **Encapsulation** :
   ```csharp
   var (ciphertext, sharedSecret) = session.EncapsulateKey(serverPublicKey);
   ```
   - Le client encapsule une clé partagée avec la clé publique du serveur
   - Génère un ciphertext et une clé partagée de 32 bytes

5. **Envoi du ciphertext** :
   - Le client envoie le ciphertext dans un paquet `KeyExchangeAck`

#### Côté Serveur

1. **Génération des clés** :
   ```csharp
   session.GenerateKyberKeys();
   ```

2. **Réception de la clé publique du client** :
   - Le serveur reçoit la clé publique du client

3. **Envoi de la clé publique** :
   - Le serveur envoie sa clé publique au client

4. **Décapsulation** :
   ```csharp
   var sharedSecret = session.DecapsulateKey(ciphertext);
   ```
   - Le serveur décapsule la clé partagée avec sa clé privée
   - Obtient la même clé partagée de 32 bytes que le client

#### Dérivation de la clé de session

La clé partagée Kyber (32 bytes) est utilisée pour dériver la clé de session de chiffrement :

```csharp
private byte[] DeriveSessionKey(byte[] sharedSecret)
{
    // Utiliser SHA3-256 pour dériver une clé de 32 bytes
    return SHA3Wrapper.ComputeSHA3_256(sharedSecret);
}
```

**Résultat** : Les deux parties possèdent maintenant la même clé de session de 32 bytes pour le chiffrement.

---

## 🔑 Authentification mutuelle

### Phase 2 : Authentification Dilithium (ML-DSA)

L'authentification utilise **Dilithium (ML-DSA)** pour authentifier mutuellement le client et le serveur.

#### Protocole d'authentification

1. **Client → Serveur : AuthRequest**
   - Le client génère un challenge aléatoire de 32 bytes
   - Le client signe le challenge avec sa clé privée Dilithium
   - Le client envoie : `[Challenge (32 bytes) | Clé publique Dilithium | Signature]`

2. **Serveur → Client : AuthResponse**
   - Le serveur vérifie la signature du client avec la clé publique reçue
   - Le serveur génère son propre challenge de 32 bytes
   - Le serveur signe son challenge avec sa clé privée Dilithium
   - Le serveur envoie : `[Challenge serveur (32 bytes) | Clé publique Dilithium serveur | Signature]`

3. **Client → Serveur : AuthSuccess**
   - Le client vérifie la signature du serveur
   - Le client envoie un accusé de réception

#### Vérification des signatures

```csharp
// Vérification côté serveur (signature du client)
bool isValid = session.VerifySignature(clientChallenge, clientSignature, clientPublicKey);

// Vérification côté client (signature du serveur)
bool isValid = session.VerifySignature(serverChallenge, serverSignature, serverPublicKey);
```

**Résultat** : Les deux parties sont authentifiées mutuellement et possèdent les clés publiques Dilithium de l'autre.

---

## 🔒 Communication chiffrée

### Phase 3 : Chiffrement authentifié

Une fois l'échange de clés et l'authentification terminés, toutes les communications sont chiffrées avec **ChaCha20-Poly1305** ou **AES-GCM**.

#### Chiffrement d'un message

```csharp
// Côté expéditeur
var (ciphertext, nonce) = session.EncryptData(plaintext, isClient: true);
```

**Processus** :
1. Génération d'un nonce unique de 12 bytes
2. Chiffrement du plaintext avec la clé de session
3. Génération d'un tag d'authentification (16 bytes)
4. Retour : `(ciphertext + tag, nonce)`

#### Déchiffrement d'un message

```csharp
// Côté récepteur
var plaintext = session.DecryptData(ciphertext, nonce, isClient: false);
```

**Processus** :
1. Vérification que le nonce n'a pas été réutilisé
2. Vérification du tag d'authentification
3. Déchiffrement du ciphertext
4. Retour : `plaintext`

#### Protection contre la réutilisation de nonce

Le protocole maintient un registre des nonces utilisés pour chaque numéro de séquence :

```csharp
// Vérification avant déchiffrement
if (_usedNonces.ContainsKey(seqNum) && 
    ConstantTimeOperations.ConstantTimeEquals(_usedNonces[seqNum], nonce))
{
    throw new SecurityException("Réutilisation de nonce détectée");
}
```

---

## 💻 Exécution de commandes à distance

### Vue d'ensemble

Le protocole supporte l'exécution de commandes à distance de manière sécurisée, similaire à SSH, mais avec une protection post-quantique complète. 

**IMPORTANT : Session Interactive Persistante**

Les commandes `ExecuteShell` et `ExecutePowerShell` utilisent une **session shell interactive persistante** :
- ✅ **Un shell reste ouvert** pour chaque client connecté
- ✅ **État partagé** : Les commandes partagent le même contexte (répertoire courant, variables d'environnement, etc.)
- ✅ **Persistance** : L'état persiste entre les commandes (comme avec SSH)
- ✅ **Même processus** : Toutes les commandes sont exécutées dans le même processus shell
- ✅ **Session maintenue** : La session reste ouverte jusqu'à la déconnexion du client

Cela permet un comportement identique à SSH où les commandes successives partagent le même environnement.

### Types de commandes supportées

| Type | Description | Exemple |
|------|-------------|---------|
| `ExecuteShell` | Exécute une commande shell (CMD sur Windows, Bash sur Linux) | `dir /b`, `ls -la` |
| `ExecutePowerShell` | Exécute une commande PowerShell | `Get-Date`, `Get-Process` |
| `ListDirectory` | Liste le contenu d'un répertoire | `.`, `C:\Users` |
| `ReadFile` | Lit le contenu d'un fichier (max 1 MB) | `C:\file.txt` |
| `GetSystemInfo` | Obtient des informations système | (aucun paramètre) |
| `Ping` | Test de connexion | (réservé pour usage futur) |

### Protocole d'exécution de commandes

```
CLIENT                                    SERVEUR
  │                                         │
  │  1. EncryptedCommand                    │
  │     (Commande sérialisée et chiffrée)  │
  ├───────────────────────────────────────>│
  │                                         │
  │     [Serveur déchiffre la commande]    │
  │     [Serveur utilise/crée session shell]│
  │     [Commande envoyée via stdin]       │
  │     [Sortie lue via stdout/stderr]     │
  │                                         │
  │  2. EncryptedResponse                  │
  │     (Résultat sérialisé et chiffré)    │
  │<───────────────────────────────────────┤
  │                                         │
  │     [Session shell reste ouverte]      │
  │     [État persiste (répertoire, vars)] │
  │     [Client peut envoyer d'autres      │
  │      commandes dans le même contexte] │
```

### Structure d'une commande

```csharp
public class SecureCommand
{
    public string CommandId { get; set; }        // ID unique (GUID)
    public CommandType Type { get; set; }         // Type de commande
    public string Command { get; set; }           // Commande à exécuter
    public string[] Arguments { get; set; }      // Arguments (optionnel)
    public string WorkingDirectory { get; set; }  // Répertoire de travail
    public int Timeout { get; set; }              // Timeout en ms (défaut: 30000)
    public byte[] AdditionalData { get; set; }    // Données supplémentaires
}
```

### Structure d'un résultat

```csharp
public class CommandResult
{
    public string CommandId { get; set; }        // ID de la commande
    public CommandStatus Status { get; set; }     // Statut (Success, Error, etc.)
    public string Output { get; set; }            // Sortie standard
    public string Error { get; set; }             // Sortie d'erreur
    public int ExitCode { get; set; }             // Code de sortie
    public long ExecutionTimeMs { get; set; }     // Temps d'exécution
    public DateTime ExecutedAt { get; set; }     // Date/heure d'exécution
}
```

### Statuts de commande

| Statut | Valeur | Description |
|--------|--------|-------------|
| `Success` | 0x00 | Commande exécutée avec succès |
| `Error` | 0x01 | Erreur lors de l'exécution |
| `Timeout` | 0x02 | Timeout dépassé |
| `AccessDenied` | 0x03 | Accès refusé |
| `NotFound` | 0x04 | Commande non trouvée |
| `Invalid` | 0x05 | Commande invalide |

### Sérialisation

Les commandes et résultats sont sérialisés en JSON à l'aide de `DataContractJsonSerializer` (compatible .NET Standard 2.0, évite les vulnérabilités CVE de System.Text.Json), puis chiffrés avec la clé de session avant transmission.

### Exécution côté serveur

Le serveur :
1. Reçoit le paquet `EncryptedCommand`
2. Déchiffre la commande avec la clé de session
3. Désérialise la commande depuis JSON
4. Exécute la commande selon son type :
   - **ExecuteShell/ExecutePowerShell** : 
     - Vérifie si une session shell interactive existe pour ce client
     - Si non, crée une nouvelle session shell persistante (`InteractiveShellSession`)
     - Envoie la commande au shell via stdin
     - Lit la sortie via stdout/stderr
     - **L'état persiste** : répertoire courant, variables d'environnement, etc.
   - **ListDirectory** : Liste le contenu du répertoire (pas de session shell)
   - **ReadFile** : Lit le fichier (avec limite de taille, pas de session shell)
   - **GetSystemInfo** : Retourne les informations système (pas de session shell)
5. Sérialise le résultat en JSON
6. Chiffre le résultat avec la clé de session
7. Envoie le paquet `EncryptedResponse` au client

**Session Shell Interactive** :
- Une session shell (`InteractiveShellSession`) est créée automatiquement lors de la première commande `ExecuteShell` ou `ExecutePowerShell`
- La session reste ouverte jusqu'à la déconnexion du client
- Toutes les commandes suivantes utilisent la même session, partagée entre toutes les commandes
- La session est nettoyée automatiquement lors de la déconnexion

### Sécurité des commandes

- ✅ **Chiffrement** : Toutes les commandes et résultats sont chiffrés
- ✅ **Authentification** : Seuls les clients authentifiés peuvent exécuter des commandes
- ✅ **Isolation par client** : Chaque client a sa propre session shell isolée
- ✅ **Session persistante** : Une session shell par client, partagée entre toutes les commandes
- ✅ **Timeouts** : Protection contre les commandes bloquantes
- ✅ **Limites** : Limite de taille pour la lecture de fichiers (1 MB par défaut)
- ✅ **Validation** : Vérification des types de commandes avant exécution
- ✅ **Nettoyage automatique** : Les sessions shell sont fermées lors de la déconnexion

### Multi-plateforme

Le système détecte automatiquement l'OS et utilise le shell approprié :
- **Windows** : PowerShell ou CMD selon le type de commande
- **Linux/Unix** : Bash

---

## 📦 Format des paquets

### Structure d'un paquet

```
┌─────────────────────────────────────────────────────────┐
│   En-tête (24 bytes)                                     │
├─────────────────────────────────────────────────────────┤
│   Type (1 byte)          : PacketType                   │
│   Flags (1 byte)         : PacketFlags                  │
│   SequenceNumber (4 bytes): uint                        │
│   PayloadLength (4 bytes) : uint                        │
│   SignatureLength (2 bytes): ushort                     │
│   Nonce (12 bytes)       : byte[12]                    │
├─────────────────────────────────────────────────────────┤
│   Payload (variable)                                    │
│   - Données selon le type de paquet                     │
├─────────────────────────────────────────────────────────┤
│   Signature (variable, si présente)                     │
│   - Signature Dilithium pour authentification           │
└─────────────────────────────────────────────────────────┘
```

### Types de paquets

| Type | Valeur | Description |
|------|--------|-------------|
| `KeyExchangeRequest` | 0x01 | Demande d'échange de clés (client → serveur) |
| `KeyExchangeResponse` | 0x02 | Réponse d'échange de clés (serveur → client) |
| `KeyExchangeAck` | 0x03 | Accusé d'échange de clés (client → serveur) |
| `AuthRequest` | 0x10 | Demande d'authentification |
| `AuthResponse` | 0x11 | Réponse d'authentification |
| `AuthChallenge` | 0x12 | Défi d'authentification |
| `AuthSuccess` | 0x13 | Authentification réussie |
| `EncryptedData` | 0x20 | Données chiffrées |
| `EncryptedCommand` | 0x21 | Commande chiffrée (exécution à distance) |
| `EncryptedResponse` | 0x22 | Réponse chiffrée (résultat de commande) |
| `Heartbeat` | 0x30 | Heartbeat pour maintenir la session |
| `SessionClose` | 0x31 | Fermeture de session |
| `Error` | 0xFF | Erreur |

### Flags des paquets

| Flag | Valeur | Description |
|------|--------|-------------|
| `RequiresResponse` | 0x01 | Le paquet nécessite une réponse |
| `IsResponse` | 0x02 | Ce paquet est une réponse |
| `HasSignature` | 0x04 | Le paquet contient une signature |
| `IsCompressed` | 0x08 | Les données sont compressées |
| `IsFinal` | 0x10 | Dernier paquet d'une séquence |

### Format de transmission

Les paquets sont transmis sur le réseau avec le format suivant :

```
[Longueur (4 bytes)][Paquet (variable)]
```

- **Longueur** : Entier 32 bits (big-endian) indiquant la taille du paquet en bytes
- **Paquet** : Données sérialisées du paquet

---

## 📖 Guide d'utilisation

### 1. Publication du service

```bash
# Linux
cd KyberModule/KyberDaemon
DOTNET_CLI_TELEMETRY_OPTOUT=1 dotnet publish -c Release -o ./publish
sudo mkdir -p /opt/kyberd /etc/kyberd
sudo cp publish/KyberDaemon.dll /opt/kyberd/
sudo cp config/kyberd.conf /etc/kyberd/
sudo cp deploy/kyberd.service /etc/systemd/system/
sudo systemctl daemon-reload
sudo systemctl enable --now kyberd
```

```powershell
# Windows (PowerShell administrateur)
Set-Location KyberModule/KyberDaemon/deploy
./install-kyberd-service.ps1
Start-Service KyberDaemon
```

`kyberd.conf` contrôle l'adresse d'écoute, les méthodes d’authentification (`auth.conf`) et le shell lancé côté serveur.

### 2. Authentification côté serveur

`config/auth.conf` (format `utilisateur:méthode:données`) :

```
alice:password:0x3F1A...
bob:key:LS0tLURJTElUSGl1bT...
```

- `password` : SHA3-256 du mot de passe (hex) stocké côté serveur. Le client renvoie `SHA3(password || challenge)`.
- `key` : Clé **privée** Dilithium (base64) utilisée pour signer le challenge.

### 3. Utilisation de KyberCLI

> Les exemples suivants gardent la session ouverte (pas d’option `--command`). Sous Windows, `KyberCLI.exe` est publié dans `KyberModule\KyberCLI\publish\win-x64`.

- **Mot de passe + enregistrement Argon2id**
  ```powershell
  .\KyberCLI.exe connect `
      --host 192.168.1.10 `
      --port 8443 `
      --user alice `
      --password 'ChangeMe!123' `
      --password-record 'argon2id$v=19$m=65536,t=3,p=2$pad2ZtQRdzTOr2zecPUMng==$wnzKuHLwtQSOloqyRr5XMN671g6c18w2UbIaPOu9Zkw='
  ```

- **Secrets stockés dans des fichiers**
  ```powershell
  $pwdFile    = 'C:\Users\alice\Documents\alice.pass'
  $recordFile = 'C:\Users\alice\Documents\alice.record'
  .\KyberCLI.exe connect `
      --host 192.168.1.10 `
      --user alice `
      --password-file $pwdFile `
      --password-record-file $recordFile
  ```

- **Secret pré-haché (SHA3-256 en hex)**
  ```powershell
  .\KyberCLI.exe connect `
      --host 192.168.1.10 `
      --user legacy `
      --password-hex '0xF84B7C9E30F3D6A1C75E0D8B2C44E19A7FFE5C0D5A6B9E3B4C2D1E0F3A6B9C1D'
  ```

- **Authentification Dilithium**
  ```powershell
  .\KyberCLI.exe connect `
      --host 192.168.1.10 `
      --user signer `
      --key 'C:\secrets\dilithium-private.key'
  ```

- **Authentification Kerberos**
  ```powershell
  $token = Get-Content 'C:\Users\alice\Documents\alice.krb' -Raw
  .\KyberCLI.exe connect `
      --host 192.168.1.10 `
      --user alice `
      --kerberos-token $token
  ```

- **Authentification OAuth / JWT**
  ```powershell
  .\KyberCLI.exe connect `
      --host 192.168.1.10 `
      --user alice `
      --oauth-token-file 'C:\Users\alice\Documents\alice.jwt'
  ```

- **Connexion TLS (SNI + pinning)**
  ```powershell
  .\KyberCLI.exe connect `
      --host gw.entreprise.local `
      --port 9443 `
      --user alice `
      --password 'ChangeMe!123' `
      --password-record 'argon2id$...' `
      --tls `
      --tls-server-name 'kyberd.entreprise.local' `
      --tls-cert-fingerprint 'AB:CD:EF:01:23:45:67:89:AB:CD:EF:01:23:45:67:89:AB:CD:EF:01:23:45:67:89:AB:CD:EF:01:23:45:67:89'
  ```

- **TLS sans validation (LAB uniquement)**
  ```powershell
  .\KyberCLI.exe connect `
      --host 192.168.1.10 `
      --user alice `
      --password 'ChangeMe!123' `
      --password-record 'argon2id$...' `
      --tls `
      --tls-skip-verify
  ```

- **Loopback / mode strict activé**
  ```powershell
  .\KyberCLI.exe connect `
      --host 127.0.0.1 `
      --user admin `
      --password 'Admin!234' `
      --password-record 'argon2id$...'
  ```

#### Génération des secrets et des clés

- **Enregistrement Argon2id** (à copier dans `auth.conf`)
  ```powershell
  dotnet KyberCLI.dll passhash --username alice --password "ChangeMe!123"
  ```

- **Paire de clés Dilithium**
  ```powershell
  Import-Module KyberModule
  $pair = New-DilithiumKeyPair -ParameterSet Dilithium3
  Set-Content 'C:\keys\dilithium.pub' -Value $pair.PublicKeyBase64
  Set-Content 'C:\keys\dilithium.prv' -Value $pair.PrivateKeyBase64
  ```

- **Paire de clés Kyber**
  ```powershell
  $pair = New-KyberKeyPair -ParameterSet Kyber768
  Set-Content 'C:\keys\kyber.pub' -Value $pair.PublicKeyBase64
  Set-Content 'C:\keys\kyber.prv' -Value $pair.PrivateKeyBase64
  ```

- **Gestion des clés serveur**
  ```powershell
  .\KyberCLI.exe keys list   --config "C:\ProgramData\KyberDaemon\kyberd.conf"
  .\KyberCLI.exe keys rotate --config "C:\ProgramData\KyberDaemon\kyberd.conf" --name session
  .\KyberCLI.exe keys export --config "C:\ProgramData\KyberDaemon\kyberd.conf" --output "C:\Temp\metadata.json"
  ```

### 4. Intégration PowerShell

La cmdlet `Invoke-SecureCommand` encapsule KyberCLI :

```powershell
Import-Module .\KyberModule.psd1 -Force

Invoke-SecureCommand \
    -ServerHost 192.168.1.10 \
    -Username alice \
    -PasswordHex 0x09AF... \
    -Command "Get-Process | Select-Object -First 5 Name,Id"
```

Paramètres utiles :
- `-KeyPath` : chemin vers la clé privée Dilithium (authentification par signature)
- `-CliPath` : chemin explicite vers l'exécutable/dll KyberCLI
- `-RebuildCli` : force `dotnet publish` avant l’exécution

> Pour diagnostiquer une connexion (handshake, authentification), lancez `KyberCLI` avec `--trace-level full` afin d’afficher les étapes détaillées côté client.

### 5. Scripts utiles

- `deploy/install-kyberd-service.ps1` : installe le service Windows
- `deploy/uninstall-kyberd-service.ps1` : supprime le service Windows
- `deploy/kyberd.service` : template `systemd`

Les anciennes cmdlets réseau (`Start-SecureServer`, `Connect-SecureClient`, `Send/Receive-SecureMessage`) sont conservées pour compatibilité mais marquées *obsolètes*.

---

## 🛡️ Sécurité

### Mesures de sécurité implémentées

#### 1. Échange de clés post-quantique
- ✅ **Kyber (ML-KEM)** : Résistant aux attaques quantiques
- ✅ **Clé partagée unique** : Chaque session génère une nouvelle clé
- ✅ **Dérivation sécurisée** : SHA3-256 pour dériver la clé de session

#### 2. Authentification mutuelle
- ✅ **Dilithium (ML-DSA)** : Signatures post-quantiques
- ✅ **Challenges aléatoires** : Prévention des attaques par rejeu
- ✅ **Vérification des signatures** : Validation de l'identité des deux parties

#### 3. Chiffrement authentifié
- ✅ **ChaCha20-Poly1305** ou **AES-GCM** : Chiffrement avec authentification intégrée
- ✅ **Nonces uniques** : Prévention de la réutilisation de nonces
- ✅ **Tags d'authentification** : Détection de modifications

#### 4. Exécution de commandes sécurisée
- ✅ **Chiffrement des commandes** : Toutes les commandes sont chiffrées avant transmission
- ✅ **Isolation des processus** : Chaque commande s'exécute dans un processus séparé
- ✅ **Timeouts** : Protection contre les commandes bloquantes
- ✅ **Limites de sécurité** : Limite de taille pour les fichiers lus
- ✅ **Validation** : Vérification des types de commandes avant exécution

#### 5. Protection contre les attaques
- ✅ **Protection side-channel** : Opérations à temps constant
- ✅ **Zeroization** : Nettoyage automatique des clés sensibles
- ✅ **Gestion des nonces** : Registre des nonces utilisés
- ✅ **Validation des paquets** : Vérification de l'intégrité

#### 6. Gestion sécurisée des clés
- ✅ **Nettoyage automatique** : Zeroization lors de la destruction des objets
- ✅ **Protection mémoire** : Clés privées nettoyées après utilisation
- ✅ **Thread-safety** : Accès concurrent sécurisé

### Recommandations de sécurité

1. **Utiliser des clés sécurisées** :
   - Utiliser `New-Secure*KeyPair` pour les clés sensibles
   - Nettoyer les clés après utilisation avec `Clear-SecureKey`

2. **Protection contre les attaques** :
   - Utiliser les méthodes sécurisées (`DecapsulateSecure`, `VerifySecure`)
   - Éviter les comparaisons directes de données sensibles

3. **Validation des certificats** :
   - Vérifier les certificats X.509 avant utilisation
   - Utiliser PKCS#8 pour l'export/import de clés

4. **Exécution de commandes** :
   - Limiter les types de commandes autorisées côté serveur
   - Utiliser des timeouts appropriés pour éviter les blocages
   - Valider les chemins de fichiers avant lecture
   - Surveiller l'exécution des commandes pour détecter les abus

5. **Surveillance** :
   - Utiliser les outils d'audit (`Test-CryptographicConformance`)
   - Effectuer des tests de fuzzing réguliers
   - Logger les commandes exécutées pour audit

---

## 📚 Références

### Standards et spécifications

- **NIST FIPS 203** : Module-Lattice-Based Key-Encapsulation Mechanism (ML-KEM)
- **NIST ML-DSA** : Module-Lattice-Based Digital Signature Algorithm
- **RFC 8439** : ChaCha20 and Poly1305 for IETF Protocols
- **NIST SP 800-38D** : Recommendation for Block Cipher Modes of Operation: Galois/Counter Mode (GCM)
- **NIST FIPS 202** : SHA-3 Standard

### Documentation technique

- **README.md** : Documentation principale du module
- **RECHERCHE_ET_DEVELOPPEMENT.md** : Historique et détails techniques
- **CONTRE_MESURES_SIDE_CHANNEL.md** : Protection contre les canaux auxiliaires
- **GESTION_SECURISEE_CLES.md** : Gestion sécurisée des clés en mémoire

### Fichiers du protocole

- **SecureProtocolPacket.cs** : Structure des paquets
- **SecureSession.cs** : Gestion de session sécurisée
- **SecureCommunicationClient.cs** : Client de communication
- **SecureCommunicationServer.cs** : Serveur de communication
- **SecureCommandProtocol.cs** : Protocole de commandes (SecureCommand, CommandResult)
- **CommandExecutor.cs** : Exécuteur de commandes sécurisé (commandes isolées)
- **InteractiveShellSession.cs** : Session shell interactive persistante (style SSH)
- **InvokeSecureCommandCommand.cs** : Cmdlet PowerShell pour exécuter des commandes

---

**Version** : 1.1.0  
**Dernière mise à jour** : Novembre 2025

### Historique des versions

- **v1.1.0** (Novembre 2025) : Ajout de l'exécution de commandes à distance avec session persistante
- **v1.0.0** (Novembre 2025) : Version initiale avec échange de clés, authentification et communication chiffrée


### Génération automatisée des clés (cmdlet New-KyberKeyBundle)

Pour préparer rapidement un serveur KyberDaemon et un poste client :

```powershell
Import-Module "C:\Program Files\KyberModule\KyberModule\KyberModule.psd1" -Force
New-KyberKeyBundle -Target Both -Username alice -IncludeKyber -Force
Restart-Service KyberDaemon
```

La sortie indique :
- les fichiers créés (clés Dilithium/Kyber encodées en Base64)
- l’entrée prête pour `C:\ProgramData\KyberDaemon\auth.conf`
- le résultat de la rotation `KyberCLI keys rotate` (si activée)

Paramètres utiles : `-Target`, `-ClientDestination`, `-ServerConfigPath`, `-ServerAuthPath`, `-IncludeKyber`, `-SkipAuthUpdate`, `-SkipKyberRotation`, `-KyberCliPath`.

> Pour diagnostiquer une connexion (handshake, authentification), lancez `KyberCLI` avec `--trace-level full` afin d’afficher les étapes détaillées côté client.
