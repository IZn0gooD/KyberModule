# Document de Recherche et Développement
## KyberModule - Module PowerShell pour la Cryptographie Post-Quantique

**Version** : 1.1.0  
**Date** : Décembre 2025  
**Auteur** : Iznogood

---

## 📋 Table des matières

1. [Contexte et Objectifs](#contexte-et-objectifs)
2. [Recherche Préliminaire](#recherche-préliminaire)
3. [Architecture Initiale](#architecture-initiale)
4. [Problèmes Rencontrés et Solutions](#problèmes-rencontrés-et-solutions)
5. [Évolution du Projet](#évolution-du-projet)
6. [Choix Techniques](#choix-techniques)
7. [Tests et Validations](#tests-et-validations)
8. [Conclusion et Perspectives](#conclusion-et-perspectives)

---

## 1. Contexte et Objectifs

### 1.1 Contexte

Avec l'avènement de l'informatique quantique, les algorithmes cryptographiques classiques (RSA, ECC) deviennent vulnérables. Le NIST (National Institute of Standards and Technology) a lancé un processus de standardisation pour les algorithmes post-quantiques résistants aux attaques quantiques.

**Kyber (ML-KEM)** a été sélectionné comme algorithme standard pour l'échange de clés post-quantique (FIPS 203).

### 1.2 Objectifs du Projet

1. **Créer un module PowerShell** permettant l'utilisation de Kyber pour la cryptographie post-quantique
2. **Intégrer Ed25519** pour l'authentification et l'intégrité des données
3. **Fournir une API simple** et intuitive via des cmdlets PowerShell
4. **Assurer la compatibilité** avec PowerShell Core et Windows PowerShell
5. **Garantir la sécurité** en utilisant des implémentations validées (BouncyCastle)

### 1.3 Contraintes

- Compatibilité .NET Standard 2.0 pour le support cross-platform
- Pas de dépendances vulnérables (ex: System.Text.Json CVE-2024-30105)
- Format de sérialisation fiable et reproductible
- Performance acceptable pour une utilisation en production

---

## 2. Recherche Préliminaire

### 2.1 État de l'Art

#### 2.1.1 Algorithmes Post-Quantiques

**Recherche** : Analyse des algorithmes candidats du NIST PQC (Post-Quantum Cryptography)

**Résultats** :
- **Kyber (ML-KEM)** : Sélectionné pour l'échange de clés
  - Basé sur les réseaux de lattices
  - Niveaux de sécurité : 512, 768, 1024 bits équivalents AES
  - Performances : Génération de clés rapide, encapsulation/décapsulation efficace
  - Taille des clés : Relativement compacte comparée à d'autres candidats

- **Ed25519** : Algorithme de signature numérique classique
  - Très performant et largement utilisé
  - Clés compactes (32 bytes)
  - Idéal pour l'authentification

**Sources** :
- NIST FIPS 203 : Module-Lattice-Based Key-Encapsulation Mechanism (ML-KEM)
- NIST PQC Standardization Process
- Documentation BouncyCastle

#### 2.1.2 Bibliothèques Disponibles

**Recherche** : Analyse des bibliothèques .NET pour Kyber

**Options évaluées** :

1. **BouncyCastle.NetCore 2.2.1**
   - ✅ Implémentation officielle conforme NIST
   - ✅ Support complet de Kyber (ML-KEM)
   - ✅ Support Ed25519
   - ✅ Actif et maintenu
   - ✅ License MIT
   - **Décision** : **Sélectionné**

2. **liboqs** (via bindings .NET)
   - ❌ Bindings .NET peu matures
   - ❌ Complexité d'installation
   - **Décision** : Rejeté

3. **Implémentation manuelle**
   - ❌ Risque d'erreurs de sécurité
   - ❌ Validation complexe
   - ❌ Maintenance lourde
   - **Décision** : Rejeté

### 2.2 Architecture PowerShell

**Recherche** : Analyse des approches pour créer un module PowerShell avec bibliothèque .NET

**Options évaluées** :

1. **Module PowerShell Script** (.psm1)
   - ✅ Simple à implémenter
   - ❌ Performances limitées
   - ❌ Typage faible
   - **Décision** : Rejeté pour les opérations cryptographiques

2. **Module PowerShell Binaire (Standard Library)**
   - ✅ Performances optimales (C#)
   - ✅ Typage fort
   - ✅ Intégration native PowerShell
   - ✅ Support cross-platform
   - **Décision** : **Sélectionné**

3. **Module PowerShell Binaire (Full .NET Framework)**
   - ✅ Performances optimales
   - ❌ Limité à Windows
   - ❌ Pas compatible PowerShell Core sur Linux/macOS
   - **Décision** : Rejeté

**Sources** :
- PowerShell Standard Library Documentation
- Microsoft PowerShell Documentation

---

## 3. Architecture Initiale

### 3.1 Première Approche

**Conception initiale** : Un seul projet contenant tout

```
KyberCryptography/
├── KyberCryptography.csproj
├── KyberWrapper.cs
├── KyberCryptography.psm1  (wrapper PowerShell)
└── README.md
```

**Problèmes identifiés** :
- Séparation des responsabilités insuffisante
- Difficulté de maintenance
- Pas de réutilisabilité de la bibliothèque .NET

### 3.2 Architecture Finale

**Conception retenue** : Architecture en deux couches

```
KyberModule/
├── KyberLibrary/           # Bibliothèque .NET réutilisable
│   ├── KyberLibrary.csproj
│   ├── KyberWrapper.cs
│   └── Ed25519Wrapper.cs
│
└── KyberModule/            # Module PowerShell
    ├── KyberModule.csproj
    ├── KyberModule.psd1
    ├── GetKyberKeyPairCommand.cs
    ├── InvokeKyberEncapsulateCommand.cs
    ├── InvokeKyberDecapsulateCommand.cs
    └── ... (autres cmdlets)
```

**Avantages** :
- ✅ Séparation claire des responsabilités
- ✅ Réutilisabilité de la bibliothèque
- ✅ Maintenance facilitée
- ✅ Tests unitaires simplifiés

---

## 4. Problèmes Rencontrés et Solutions

### 4.1 Problème 1 : Ordre des Paramètres dans BouncyCastle

**Symptôme** :
```
error CS1503: Argument 1 : conversion impossible de 'KyberParameters' en 'SecureRandom'
```

**Analyse** :
- Le constructeur `KyberKeyGenerationParameters` attend `(SecureRandom, KyberParameters)`
- Le code initial utilisait l'ordre inverse

**Solution** :
```csharp
// ❌ Incorrect
KyberKeyGenerationParameters keyGenParams = new KyberKeyGenerationParameters(_kyberParameters, _random);

// ✅ Correct
KyberKeyGenerationParameters keyGenParams = new KyberKeyGenerationParameters(_random, _kyberParameters);
```

**Leçon apprise** : Toujours vérifier la signature exacte des constructeurs dans la documentation BouncyCastle.

---

### 4.2 Problème 2 : Nom de Méthode Incorrect

**Symptôme** :
```
error CS1061: 'KyberKemGenerator' ne contient pas de définition pour 'GenerateEncapsulation'
```

**Analyse** :
- Le nom de la méthode correcte est `GenerateEncapsulated` (participe passé), pas `GenerateEncapsulation`

**Solution** :
```csharp
// ❌ Incorrect
byte[] encapsulated = kemGenerator.GenerateEncapsulation(publicKeyParams);

// ✅ Correct
byte[] encapsulated = kemGenerator.GenerateEncapsulated(publicKeyParams);
```

**Leçon apprise** : Vérifier les noms exacts des méthodes dans la documentation BouncyCastle.

---

### 4.3 Problème 3 : Sérialisation/Désérialisation des Clés Privées Kyber

**Symptôme** :
```
Exception: Source array was not long enough. Check the source index, length, and the array's lower bounds.
```

**Analyse** :
- `GetEncoded()` de BouncyCastle retourne un format binaire spécifique
- Le format exact n'est pas documenté publiquement
- Les tentatives de parsing manuel échouaient à cause de tailles incorrectes

**Recherche** :
1. Tentative d'utilisation de `GetInstance()` via réflexion → Échec (méthode non publique)
2. Tentative de parsing manuel du format `GetEncoded()` → Échec (tailles incorrectes)
3. Utilisation de la réflexion pour extraire les champs internes → **Succès**

**Solution Finale** :
```csharp
// Extraction des composants internes via réflexion
var sField = typeof(KyberPrivateKeyParameters).GetField("m_s", BindingFlags.NonPublic | BindingFlags.Instance);
var hpkField = typeof(KyberPrivateKeyParameters).GetField("m_hpk", BindingFlags.NonPublic | BindingFlags.Instance);
var tField = typeof(KyberPrivateKeyParameters).GetField("m_t", BindingFlags.NonPublic | BindingFlags.Instance);
var rhoField = typeof(KyberPrivateKeyParameters).GetField("m_rho", BindingFlags.NonPublic | BindingFlags.Instance);
var nonceField = typeof(KyberPrivateKeyParameters).GetField("m_nonce", BindingFlags.NonPublic | BindingFlags.Instance);

// Format personnalisé avec magic number "KYBE"
using (var ms = new MemoryStream())
{
    ms.WriteByte(0x4B); // K
    ms.WriteByte(0x59); // Y
    ms.WriteByte(0x42); // B
    ms.WriteByte(0x45); // E
    WriteBytesWithLength(ms, s);
    WriteBytesWithLength(ms, hpk);
    WriteBytesWithLength(ms, nonce);
    WriteBytesWithLength(ms, t);
    WriteBytesWithLength(ms, rho);
    return ms.ToArray();
}
```

**Reconstruction** :
```csharp
// Détection du format personnalisé
bool isCustomFormat = privateKey.Length >= 4 && 
                     privateKey[0] == 0x4B && 
                     privateKey[1] == 0x59 && 
                     privateKey[2] == 0x42 && 
                     privateKey[3] == 0x45;

if (isCustomFormat)
{
    // Reconstruction depuis le format personnalisé
    int offset = 4;
    byte[] s = ReadBytesWithLength(privateKey, ref offset);
    byte[] hpk = ReadBytesWithLength(privateKey, ref offset);
    byte[] nonce = ReadBytesWithLength(privateKey, ref offset);
    byte[] t = ReadBytesWithLength(privateKey, ref offset);
    byte[] rho = ReadBytesWithLength(privateKey, ref offset);
    
    privateKeyParams = new KyberPrivateKeyParameters(_kyberParameters, s, hpk, nonce, t, rho);
}
```

**Leçon apprise** : 
- Les formats internes de BouncyCastle peuvent être opaques
- La réflexion peut être nécessaire pour accéder aux composants internes
- Un format de sérialisation personnalisé avec magic number assure la compatibilité

---

### 4.4 Problème 4 : Ordre des Paramètres du Constructeur KyberPrivateKeyParameters

**Symptôme** :
```
error CS7036: Parmi les arguments spécifiés, aucun ne correspond au paramètre obligatoire 'hpk'
```

**Analyse** :
- L'ordre des paramètres du constructeur était incorrect
- L'ordre correct est : `(KyberParameters, s, hpk, nonce, t, rho)`

**Solution** :
```csharp
// ✅ Ordre correct
privateKeyParams = new KyberPrivateKeyParameters(_kyberParameters, s, hpk, nonce, t, rho);
```

**Leçon apprise** : Utiliser la réflexion pour inspecter les constructeurs disponibles et leurs signatures exactes.

---

### 4.5 Problème 5 : Version de Langage C#

**Symptôme** :
```
error CS8370: La fonctionnalité 'modèles récursifs' n'est pas disponible en C# 7.3
```

**Analyse** :
- Les switch expressions nécessitent C# 8.0+
- Par défaut, .NET Standard 2.0 utilise C# 7.3

**Solution** :
```xml
<PropertyGroup>
  <TargetFramework>netstandard2.0</TargetFramework>
  <LangVersion>latest</LangVersion>
</PropertyGroup>
```

**Leçon apprise** : Toujours spécifier `LangVersion` explicitement pour utiliser les fonctionnalités modernes de C#.

---

### 4.6 Problème 6 : Chargement des Assemblies dans PowerShell

**Symptôme** :
```
Could not load file or assembly 'BouncyCastle.Crypto, Version=0.0.0.0'
```

**Analyse** :
- PowerShell ne trouve pas automatiquement les dépendances
- Les DLLs doivent être dans le même répertoire
- Le manifeste doit déclarer les dépendances

**Solution** :
1. Copier `BouncyCastle.Crypto.dll` dans le répertoire du module
2. Déclarer dans `KyberModule.psd1` :
```powershell
RequiredAssemblies = @('KyberLibrary.dll', 'BouncyCastle.Crypto.dll')
```

**Leçon apprise** : PowerShell Standard Library nécessite une gestion explicite des dépendances.

---

### 4.7 Problème 7 : Vulnérabilité System.Text.Json

**Symptôme** :
```
warning NU1903: Le package 'System.Text.Json' 8.0.0 présente une vulnérabilité de gravité élevée
CVE-2024-30105: .NET Denial of Service Vulnerability
```

**Analyse** :
- `System.Text.Json` était utilisé pour la sérialisation des clés
- Vulnérabilité de sécurité critique (DoS)
- Nécessité d'un format alternatif

**Recherche d'alternatives** :
1. **Newtonsoft.Json** : Évité pour éviter une nouvelle dépendance
2. **Format binaire personnalisé** : Complexe pour l'utilisateur
3. **Format texte Key=Value** : ✅ Simple, sécurisé, lisible

**Solution** :
```csharp
// Format texte simple Key=Value
StringBuilder sb = new StringBuilder();
sb.AppendLine("# Kyber Key Pair Export");
sb.AppendLine("# Format: Simple Key-Value");
sb.AppendLine("Version=1.0");
sb.AppendLine($"ParameterSet={KeyPair.ParameterSet}");
sb.AppendLine($"PublicKey={Convert.ToBase64String(KeyPair.PublicKey)}");
sb.AppendLine($"PrivateKey={Convert.ToBase64String(KeyPair.PrivateKey)}");
```

**Avantages** :
- ✅ Pas de dépendances externes
- ✅ Format lisible par l'humain
- ✅ Facile à parser
- ✅ Pas de vulnérabilités de sécurité

**Leçon apprise** : 
- Éviter les dépendances avec des vulnérabilités connues
- Préférer des formats simples et sécurisés
- Toujours vérifier les CVE des dépendances

---

### 4.8 Problème 8 : Import de Clés Publiques

**Symptôme** :
```
La clé publique doit être un byte[] ou une chaîne hexadécimale
```

**Analyse** :
- `Import-KyberPublicKey` retourne un tableau PowerShell
- Nécessité de conversion explicite en `byte[]`

**Solution** :
```powershell
$serverKyberPublic = @(Import-KyberPublicKey -Path $path)[0]
if ($serverKyberPublic -isnot [byte[]]) {
    $serverKyberPublic = [byte[]]$serverKyberPublic
}
```

**Leçon apprise** : PowerShell peut retourner des tableaux même pour une seule valeur, nécessitant une conversion explicite.

---

## 5. Évolution du Projet

### 5.1 Phase 1 : Implémentation Initiale Kyber

**Objectif** : Créer un module PowerShell fonctionnel pour Kyber

**Réalisations** :
- ✅ Intégration de BouncyCastle.NetCore
- ✅ Création de `KyberWrapper.cs`
- ✅ Implémentation des cmdlets de base
- ✅ Tests unitaires

**Problèmes** :
- Sérialisation des clés privées non fonctionnelle
- Architecture monolithique

### 5.2 Phase 2 : Refactorisation

**Objectif** : Améliorer l'architecture et corriger les problèmes de sérialisation

**Réalisations** :
- ✅ Séparation en deux projets (Library + Module)
- ✅ Format de sérialisation personnalisé avec magic number "KYBE"
- ✅ Extraction des composants via réflexion
- ✅ Tests complets de sérialisation/désérialisation

**Décisions** :
- Architecture en deux couches adoptée
- Format de sérialisation personnalisé retenu

### 5.3 Phase 3 : Fonctionnalités d'Export/Import

**Objectif** : Permettre la sauvegarde et le chargement de clés

**Réalisations** :
- ✅ Cmdlets `Export-KyberKeyPair` et `Import-KyberKeyPair`
- ✅ Cmdlets `Export-KyberPublicKey` et `Import-KyberPublicKey`
- ✅ Support de formats multiples (Text, Base64/PEM)
- ✅ Scripts d'exemples

### 5.4 Phase 4 : Intégration Ed25519

**Objectif** : Ajouter le support de la signature numérique classique

**Réalisations** :
- ✅ Création de `Ed25519Wrapper.cs`
- ✅ Cmdlets Ed25519 (génération, signature, vérification)
- ✅ Cmdlets d'export/import Ed25519
- ✅ Scripts d'exemples Ed25519

**Décisions** :
- Utilisation de BouncyCastle pour Ed25519 (déjà disponible)
- Format de sérialisation identique à Kyber (cohérence)

### 5.5 Phase 5 : Protocole Combiné

**Objectif** : Démonstrer l'utilisation combinée Kyber + Ed25519

**Réalisations** :
- ✅ Script `TestKyberEd25519.ps1` avec scénario complet
- ✅ Script `KyberEd25519CompleteExample.ps1` (serveur/client)
- ✅ Documentation complète
- ✅ Tests de validation

**Cas d'usage validé** :
- Échange de clés post-quantique (Kyber)
- Authentification et intégrité (Ed25519)
- Protocole sécurisé complet

### 5.6 Phase 6 : Migration vers BouncyCastle.Cryptography 2.6.2

**Objectif** : Migrer vers la version la plus récente de BouncyCastle pour bénéficier du support de Dilithium

**Réalisations** :
- ✅ Migration de `BouncyCastle.NetCore 2.2.1` vers `BouncyCastle.Cryptography 2.6.2`
- ✅ Adaptation de `KyberWrapper.cs` pour utiliser les classes `MLKem*` au lieu de `Kyber*`
- ✅ Mise à jour des namespaces et des API
- ✅ Tests de validation post-migration
- ✅ Documentation de migration créée (`MIGRATION_BOUNCYCASTLE.md`)

**Changements techniques** :
- Namespace : `Org.BouncyCastle.Pqc.Crypto.Crystals.Kyber` → `Org.BouncyCastle.Crypto.Parameters.MLKem*`
- Classes : `KyberParameters` → `MLKemParameters`, `KyberEncapsulator` → `MLKemEncapsulator`, etc.
- Méthodes : Adaptation des signatures de méthodes (ex: `Encapsulate` avec paramètres supplémentaires)

**Décisions** :
- Utilisation de `MLKemParameters.ml_kem_*` (champs statiques) au lieu de propriétés
- Utilisation de `FromEncoding()` pour reconstruire les clés depuis les bytes
- Conservation de la compatibilité avec le format de sérialisation existant

**Références** :
- Voir `MIGRATION_BOUNCYCASTLE.md` pour les détails complets de la migration

### 5.7 Phase 7 : Intégration Dilithium (ML-DSA)

**Objectif** : Ajouter le support de la signature numérique post-quantique Dilithium

**Réalisations** :
- ✅ Création de `DilithiumWrapper.cs` utilisant les classes `MLDsa*`
- ✅ Cmdlets Dilithium complets (génération, signature, vérification, export/import)
- ✅ Support des paramètres Dilithium2, Dilithium3, Dilithium5 (ML-DSA-44, ML-DSA-65, ML-DSA-87)
- ✅ Scripts d'exemples et tests combinés
- ✅ Documentation complète (`ANALYSE_DILITHIUM.md`)

**Changements techniques** :
- Utilisation des classes `MLDsa*` au lieu des classes obsolètes `Dilithium*`
- Namespace : `Org.BouncyCastle.Crypto.Parameters.MLDsa*` et `Org.BouncyCastle.Crypto.Signers.MLDsaSigner`
- Paramètres : `MLDsaParameters.ml_dsa_44`, `ml_dsa_65`, `ml_dsa_87`

**Décisions** :
- Utilisation de `MLDsaSigner` avec constructeur `(MLDsaParameters, Boolean)`
- Utilisation de `FromEncoding()` pour reconstruire les clés depuis les bytes
- Format de sérialisation identique à Kyber et Ed25519 (cohérence)

**Références** :
- Voir `ANALYSE_DILITHIUM.md` pour l'analyse complète de l'intégration

### 5.8 Phase 8 : Protocoles Combinés 100% Post-Quantum

**Objectif** : Créer des scénarios combinés Kyber + Dilithium pour une sécurité 100% post-quantique

**Réalisations** :
- ✅ Script `TestKyberDilithium.ps1` : Test rapide de l'intégration
- ✅ Script `KyberDilithiumCompleteExample.ps1` : Scénario serveur/client complet
- ✅ Documentation mise à jour avec exemples post-quantiques
- ✅ Validation de l'architecture 100% post-quantique

**Cas d'usage validé** :
- Échange de clés post-quantique (Kyber ML-KEM)
- Authentification et intégrité post-quantiques (Dilithium ML-DSA)
- Protocole sécurisé 100% post-quantique

**Avantages** :
- 🛡️ Résistance complète aux attaques quantiques
- ✅ Conformité aux standards NIST (FIPS 203, ML-DSA)
- ✅ Protocole complet sans dépendances classiques

### 5.9 Phase 9 : Fonctions de Hachage Modernes (SHA3) et Intégration ML-DSA

**Objectif** : Implémenter les fonctions de hachage cryptographiques modernes SHA3-256 et SHA3-384 pour ML-DSA

**Réalisations** :
- ✅ Création de `SHA3Wrapper.cs` avec support SHA3-256 et SHA3-384
- ✅ Intégration SHA3 dans `DilithiumWrapper` pour pré-hachage optionnel
- ✅ Cmdlet `Get-SHA3Hash` pour calculer des hachages SHA3
- ✅ Paramètres `-PreHash` et `-SHA3Variant` dans les cmdlets Dilithium
- ✅ Scripts d'exemples `SHA3Examples.ps1` démontrant l'utilisation

**Changements techniques** :
- Utilisation de `Org.BouncyCastle.Crypto.Digests.Sha3Digest` de BouncyCastle
- Support de deux variantes : SHA3-256 (256 bits) et SHA3-384 (384 bits)
- Pré-hachage recommandé pour ML-DSA selon les spécifications NIST
- Méthodes statiques et instance pour flexibilité d'utilisation

**Décisions** :
- Pré-hachage SHA3 optionnel mais recommandé pour ML-DSA
- Support des deux variantes SHA3-256 et SHA3-384
- Intégration transparente dans les workflows Dilithium existants

**Références** :
- NIST FIPS 202 : SHA-3 Standard
- Recommandations NIST pour ML-DSA avec pré-hachage

### 5.10 Phase 10 : Chiffrement Symétrique Authenticated Encryption

**Objectif** : Implémenter le chiffrement symétrique avec authentification (ChaCha20-Poly1305, AES-GCM)

**Réalisations** :
- ✅ Création de `ChaCha20Poly1305Wrapper.cs` pour ChaCha20-Poly1305
- ✅ Création de `AESGCMWrapper.cs` pour AES-GCM
- ✅ Cmdlets de chiffrement/déchiffrement :
  - `Protect-WithChaCha20Poly1305` / `Unprotect-ChaCha20Poly1305`
  - `Protect-WithAESGCM` / `Unprotect-AESGCM`
- ✅ Scripts d'exemples `AuthenticatedEncryptionExamples.ps1`
- ✅ Intégration avec les clés partagées Kyber pour workflows complets

**Changements techniques** :
- Utilisation de `Org.BouncyCastle.Crypto.Modes.ChaCha20Poly1305` pour ChaCha20-Poly1305
- Utilisation de `Org.BouncyCastle.Crypto.Modes.GcmBlockCipher` pour AES-GCM
- Support de génération automatique de nonces/IV
- Authentification intégrée (AEAD) pour garantir l'intégrité et l'authenticité

**Décisions** :
- Support de deux algorithmes modernes et sécurisés (ChaCha20-Poly1305 et AES-GCM)
- Génération automatique de nonces/IV pour simplifier l'utilisation
- Intégration avec les clés partagées Kyber pour workflows post-quantiques complets

**Avantages** :
- ✅ Authentification intégrée (AEAD) pour sécurité renforcée
- ✅ Support de deux algorithmes modernes et performants
- ✅ Compatibilité avec les workflows post-quantiques (Kyber)

**Références** :
- RFC 8439 : ChaCha20 and Poly1305 for IETF Protocols
- NIST SP 800-38D : Recommendation for Block Cipher Modes of Operation: Galois/Counter Mode (GCM)

### 5.11 Phase 11 : Conformité avec Formats Standards (PKCS, X.509, CMS)

**Objectif** : Ajouter la mise en conformité avec les formats PKCS, X.509, CMS pour intégration dans des infrastructures existantes

**Réalisations** :
- ✅ Création de `PKCS8Wrapper.cs` pour export/import de clés privées en format PKCS#8 (DER/PEM)
- ✅ Création de `X509Wrapper.cs` pour création et vérification de certificats X.509 auto-signés
- ✅ Création de `CMSWrapper.cs` pour CMS (enveloppes chiffrées fonctionnelles, signatures en développement)
- ✅ Cmdlets pour PKCS#8 :
  - `Export-PrivateKeyPKCS8` / `Import-PrivateKeyPKCS8`
- ✅ Cmdlets pour X.509 :
  - `New-X509Certificate` / `Test-X509Certificate`
- ✅ Scripts d'exemples `PKCSX509Examples.ps1`

**Changements techniques** :
- Utilisation de `Org.BouncyCastle.Pkcs.Pkcs8Generator` pour PKCS#8
- Utilisation de `Org.BouncyCastle.X509.X509V3CertificateGenerator` pour X.509
- Implémentation personnalisée de `MLDsaContentSigner` et `SignatureFactory` pour signer les certificats avec ML-DSA
- Support des formats DER (binaire) et PEM (texte) pour PKCS#8 et X.509
- Vérification personnalisée des certificats X.509 signés avec ML-DSA

**Décisions** :
- Format PKCS#8 pour l'export/import de clés privées (standard industrie)
- Certificats X.509 auto-signés avec clés Dilithium pour intégration PKI
- CMS en développement (enveloppes chiffrées fonctionnelles, signatures en cours)
- OID temporaire utilisé pour ML-DSA (en attente de standardisation officielle)

**Problèmes résolus** :
- Signature de certificats X.509 avec ML-DSA : Implémentation personnalisée de `ISignatureFactory` et `IStreamCalculator<IBlockResult>`
- Vérification de certificats ML-DSA : Extraction des données TBS et vérification avec `MLDsaSigner`

**Avantages** :
- ✅ Compatibilité avec les infrastructures PKI existantes
- ✅ Formats standards (PKCS#8, X.509) pour interopérabilité
- ✅ Intégration possible avec des systèmes de gestion de certificats
- ✅ Support des certificats auto-signés avec clés post-quantiques

**Références** :
- RFC 5208 : Public-Key Cryptography Standards (PKCS) #8
- RFC 5280 : Internet X.509 Public Key Infrastructure Certificate and Certificate Revocation List (CRL) Profile
- RFC 5652 : Cryptographic Message Syntax (CMS)

### 5.12 Phase 12 : TLS 1.3 Post-Quantum Hybride

**Objectif** : Implémenter le support TLS 1.3 post-quantique hybride combinant algorithmes classiques et post-quantiques pour une transition progressive

**Réalisations** :
- ✅ Création de `TLS13HybridWrapper.cs` pour la génération de configurations TLS hybrides
- ✅ Support des algorithmes classiques : ECDSA_P256, ECDSA_P384, RSA_2048, RSA_3072
- ✅ Support des algorithmes post-quantiques : Kyber512/768/1024 (ML-KEM), Dilithium2/3/5 (ML-DSA)
- ✅ Cmdlets TLS 1.3 Hybrid :
  - `New-TLS13HybridConfig` : Génère une configuration TLS hybride complète
  - `Get-TLS13HybridCipherSuite` : Obtient le nom du cipher suite TLS hybride
  - `Export-TLS13HybridConfig` : Exporte une configuration en format texte
- ✅ Scripts d'exemples `TLS13HybridExamples.ps1` démontrant différents scénarios
- ✅ Génération automatique de toutes les clés nécessaires (classiques + post-quantiques)
- ✅ Format d'export texte pour intégration avec bibliothèques TLS

**Changements techniques** :
- Utilisation de `Org.BouncyCastle.Crypto.Generators.ECKeyPairGenerator` pour ECDSA
- Utilisation de `Org.BouncyCastle.Crypto.Generators.RsaKeyPairGenerator` pour RSA
- Intégration avec `KyberWrapper` et `DilithiumWrapper` existants
- Format de nommage des cipher suites : `TLS13-{Classical}+{ML-KEM}+{ML-DSA}`
- Validation automatique des configurations hybrides

**Décisions** :
- Combinaison classique + post-quantique pour transition progressive
- Support ECDSA et RSA pour compatibilité maximale
- Format texte simple pour export (Key=Value)
- Zeroization des clés privées dans la classe `HybridTLSConfig`

**Avantages** :
- ✅ Transition progressive vers la cryptographie post-quantique
- ✅ Compatibilité avec infrastructure existante (algorithmes classiques)
- ✅ Sécurité renforcée (algorithmes post-quantiques)
- ✅ Intégration possible avec bibliothèques TLS supportant extensions post-quantiques
- ✅ Flexibilité dans le choix des algorithmes (classique + post-quantique)

**Cas d'usage** :
- Configuration de serveurs TLS avec support post-quantique
- Transition progressive vers la cryptographie post-quantique
- Intégration avec OQS-BoringSSL et autres bibliothèques TLS post-quantiques

**Références** :
- RFC 8446 : The Transport Layer Security (TLS) Protocol Version 1.3
- NIST PQC Transition : Migration vers cryptographie post-quantique
- OQS (Open Quantum Safe) : Bibliothèques TLS post-quantiques

### 5.13 Phase 13 : Script de Benchmark de Performance Haute Précision

**Objectif** : Créer un script de benchmark haute précision pour mesurer et comparer les performances de tous les algorithmes cryptographiques avec des métriques détaillées et exploitables

**Réalisations** :
- ✅ Création de `PerformanceBenchmark.ps1` : Script PowerShell complet de benchmark haute précision
- ✅ Mesure des performances pour tous les algorithmes :
  - Kyber (ML-KEM) : Génération, encapsulation, décapsulation (Kyber512/768/1024) - 10 itérations
  - Dilithium (ML-DSA) : Génération, signature, vérification (Dilithium2/3/5) - 10 itérations
  - Ed25519 : Génération, signature, vérification - **1000 itérations** pour précision
  - SHA3 : Hachage SHA3-256 et SHA3-384 - **1000 itérations** pour précision
  - Chiffrement AEAD : ChaCha20-Poly1305 et AES-GCM (chiffrement/déchiffrement) - **500 itérations** pour précision
- ✅ **Métriques détaillées** :
  - **Moyenne** : Temps moyen en microsecondes (µs) et millisecondes (ms)
  - **Médiane** : Temps médian (moins sensible aux valeurs aberrantes)
  - **Minimum et Maximum** : Plage de variation des temps
  - **Écart-type** : Mesure de la variabilité des performances
  - **Précision** : 4 décimales pour ms, 2 décimales pour µs
- ✅ **Warm-up automatique** : Exécution préalable pour éviter les effets de cache/JIT
- ✅ **Gestion d'erreurs robuste** : Retourne toujours des valeurs exploitables même en cas d'erreur
  - Statut : SUCCESS, PARTIAL, ou FAILED
  - Compteurs d'itérations réussies et erreurs
  - Messages d'erreur détaillés pour diagnostic
- ✅ **Affichage formaté** : Résultats détaillés avec toutes les métriques
- ✅ **Export CSV** : Toutes les données exportables au format CSV pour analyse approfondie
- ✅ Fonction `Measure-Operation` améliorée avec statistiques avancées

**Changements techniques** :
- Utilisation de `System.Diagnostics.Stopwatch` avec `TotalMilliseconds` pour précision maximale
- Calcul des microsecondes : `elapsedMs * 1000` (compatible .NET Standard 2.0)
- **Itérations adaptatives** :
  - Opérations lentes (Kyber/Dilithium) : 10 itérations
  - Opérations rapides (Ed25519/SHA3/AEAD) : 500-1000 itérations
- **Calcul de la médiane** : Tri des valeurs et sélection de la valeur médiane
- **Calcul de l'écart-type** : Variance puis racine carrée pour mesure de dispersion
- Warm-up : Exécution d'une itération avant les mesures pour stabiliser les performances
- Gestion d'erreurs : Capture de toutes les exceptions avec compteurs et messages détaillés

**Décisions** :
- **Itérations adaptatives** : Plus d'itérations pour opérations rapides (500-1000) pour obtenir des mesures précises même pour opérations de quelques microsecondes
- **Mesures en microsecondes** : Précision maximale pour opérations rapides (0.1 ms ou moins)
- **Statistiques avancées** : Médiane et écart-type pour analyse complète de la distribution des temps
- **Warm-up** : Nécessaire pour éviter les effets de cache/JIT qui fausseraient les mesures
- **Export CSV** : Format exploitable pour analyse externe et comparaisons
- **Gestion d'erreurs** : Toujours retourner des valeurs exploitables pour diagnostic

**Problèmes résolus** :
- **Mesures trop petites** : Opérations rapides affichaient 0 ms ou 0.1 ms → Solution : Mesures en microsecondes avec 1000 itérations
- **Manque de précision** : Statistiques insuffisantes → Solution : Ajout de médiane et écart-type
- **Erreurs silencieuses** : Pas de valeurs exploitables en cas d'erreur → Solution : Retour systématique avec statut et détails
- **Effets de cache/JIT** : Première itération plus lente → Solution : Warm-up automatique

**Avantages** :
- ✅ **Précision maximale** : Mesures en microsecondes pour opérations rapides
- ✅ **Statistiques complètes** : Moyenne, médiane, min, max, écart-type pour analyse approfondie
- ✅ **Comparaison objective** : Données exploitables pour choix des paramètres de sécurité
- ✅ **Identification des goulots d'étranglement** : Écart-type révèle la variabilité
- ✅ **Export exploitable** : Format CSV pour analyse externe et reporting
- ✅ **Robustesse** : Gestion d'erreurs complète avec valeurs exploitables même en cas d'échec
- ✅ **Validation des performances** : Données précises pour validation en production

**Utilisation** :
```powershell
.\PerformanceBenchmark.ps1
```

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
Le script génère un export CSV avec toutes les métriques :
```
Operation,Status,Average_ms,Average_us,Median_ms,Median_us,Min_ms,Min_us,Max_ms,Max_us,StdDev_ms,StdDev_us,SuccessfulIterations,TotalIterations,Errors
```

**Résultats typiques** (avec précision microsecondes) :
- Kyber768 : Génération ~80ms, Encapsulation ~40ms, Décapsulation ~40ms
- Dilithium3 : Génération ~120ms, Signature ~80ms, Vérification ~80ms
- Ed25519 : Génération ~125µs, Signature ~95µs, Vérification ~98µs (mesures précises avec 1000 itérations)
- SHA3-256 : ~15µs par hachage (mesures précises avec 1000 itérations)
- ChaCha20-Poly1305 : Chiffrement ~45µs, Déchiffrement ~42µs (mesures précises avec 500 itérations)

### 5.14 Phase 14 : Gestion Sécurisée des Clés en Mémoire

**Objectif** : Implémenter une gestion sécurisée des clés cryptographiques en mémoire avec zeroization et protection contre les attaques de récupération de mémoire

**Réalisations** :
- ✅ Création de `SecureKeyManager.cs` : Classe utilitaire statique pour le nettoyage sécurisé
- ✅ Création de `SecureKeyWrapper.cs` : Wrapper IDisposable pour une clé avec nettoyage automatique
- ✅ Création de `SecureKeyPairWrapper` : Wrapper pour paires de clés (publique + privée)
- ✅ Intégration dans les wrappers existants :
  - `KyberWrapper.GenerateKeyPairSecure()`
  - `DilithiumWrapper.GenerateKeyPairSecure()`
  - `Ed25519Wrapper.GenerateKeyPairSecure()`
- ✅ Cmdlets PowerShell pour gestion sécurisée :
  - `New-SecureKyberKeyPair` : Génération sécurisée de clés Kyber
  - `New-SecureDilithiumKeyPair` : Génération sécurisée de clés Dilithium
  - `New-SecureEd25519KeyPair` : Génération sécurisée de clés Ed25519
  - `Clear-SecureKey` : Nettoyage manuel avec support multi-passes
  - `Test-ZeroizedKey` : Vérification du nettoyage
- ✅ Script d'exemples `SecureKeyManagementExamples.ps1` avec 8 scénarios
- ✅ Documentation complète `GESTION_SECURISEE_CLES.md`

**Changements techniques** :
- Utilisation de `Array.Clear()` pour zeroization standard
- Implémentation de nettoyage multi-passes (0x00, 0xFF, 0xAA, 0x55, puis 0x00)
- Pattern Dispose pour nettoyage automatique
- Finalizers pour garantir le nettoyage même si Dispose() n'est pas appelé
- Thread-safety avec verrous pour accès concurrent
- Méthode `SecureCopy()` pour créer des copies sécurisées

**Décisions** :
- Zeroization standard avec `Array.Clear()` (optimisé par le runtime .NET)
- Nettoyage multi-passes optionnel pour sécurité renforcée (protection cold boot)
- Wrappers IDisposable pour gestion automatique du cycle de vie
- Compatibilité maintenue avec API existante (méthodes `GenerateKeyPair()` conservées)
- Nouvelles méthodes `GenerateKeyPairSecure()` pour fonctionnalité sécurisée

**Problèmes résolus** :
- Gestion du cycle de vie des clés : Pattern Dispose pour nettoyage garanti
- Protection contre les fuites : Wrappers avec finalizers
- Thread-safety : Verrous pour accès concurrent sécurisé
- Exposition des clés : Méthodes GetKey() retournent des copies

**Avantages** :
- ✅ Protection contre les attaques de récupération de mémoire (memory dumps, core dumps)
- ✅ Nettoyage automatique via pattern Dispose
- ✅ Nettoyage multi-passes pour protection renforcée (cold boot attacks)
- ✅ Thread-safety pour utilisation en environnement concurrent
- ✅ API PowerShell intuitive avec cmdlets dédiés
- ✅ Compatibilité avec code existant (pas de breaking changes)

**Cas d'usage** :
- Génération de clés sensibles avec nettoyage automatique
- Protection contre les attaques de récupération de mémoire
- Conformité aux standards de sécurité (NIST SP 800-57)
- Applications nécessitant une gestion stricte des clés privées

**Références** :
- NIST SP 800-57 : Recommendation for Key Management
- FIPS 140-2 : Security Requirements for Cryptographic Modules
- OWASP : Cryptographic Storage Cheat Sheet
- .NET Documentation : Array.Clear Method

### 5.15 Phase 15 : Protection contre les Canaux Auxiliaires (Side-Channel Attacks)

**Objectif** : Implémenter des contre-mesures contre les attaques par canaux auxiliaires (timing attacks, cache side-channel attacks) pour protéger les opérations cryptographiques sensibles.

**Réalisations** :
- ✅ Création de `ConstantTimeOperations.cs` : Classe utilitaire pour opérations à temps constant
  - `ConstantTimeEquals()` : Comparaison en temps constant
  - `ConstantTimeSelect()` : Sélection conditionnelle en temps constant
  - `ConstantTimeCopy()` : Copie conditionnelle en temps constant
  - `ConstantTimeIsZero()` : Vérification zéro en temps constant
  - `ConstantTimeMask()` : Masquage en temps constant
- ✅ Création de `SideChannelProtection.cs` : Classe utilitaire pour protection contre cache side-channel
  - `FlushCache()` : Nettoyage du cache du processeur
  - `SecureCompare()` : Comparaison sécurisée avec nettoyage du cache
  - `ProtectTiming<T>()` : Protection d'opérations contre timing attacks
  - `UniformMemoryAccess()` : Accès mémoire uniforme pour masquer les patterns
  - `PreloadCache()` : Préchargement du cache pour éviter les variations
- ✅ Intégration dans les wrappers existants :
  - `KyberWrapper.DecapsulateSecure()` : Décapsulation avec protection side-channel
  - `KyberWrapper.ConstantTimeCompareSharedSecrets()` : Comparaison de clés partagées en temps constant
  - `DilithiumWrapper.VerifySecure()` : Vérification de signature avec protection side-channel
  - `DilithiumWrapper.ConstantTimeCompareSignatures()` : Comparaison de signatures en temps constant
- ✅ Cmdlets PowerShell pour opérations sécurisées :
  - `Invoke-KyberDecapsulateSecure` : Décapsulation Kyber avec protection side-channel
  - `Test-DilithiumSignatureSecure` : Vérification Dilithium avec protection side-channel
  - `Test-ConstantTimeCompare` : Comparaison en temps constant de tableaux
- ✅ Script de tests complet `SideChannelProtectionTests.ps1` avec 7 scénarios de test
- ✅ Documentation complète `CONTRE_MESURES_SIDE_CHANNEL.md`

**Changements techniques** :
- Utilisation de `MethodImplOptions.NoOptimization` pour empêcher les optimisations du compilateur
- Implémentation de comparaisons sans branches conditionnelles (XOR bitwise)
- Nettoyage du cache via accès mémoire séquentiels et `Thread.MemoryBarrier()`
- Masquage des accès mémoire dépendants des données via accès uniformes
- Protection du timing via opérations à temps constant indépendantes des données secrètes

**Décisions** :
- Opérations à temps constant : Utilisation de XOR bitwise au lieu de comparaisons conditionnelles
- Nettoyage du cache : Accès mémoire séquentiels pour forcer le préchargement
- Compatibilité maintenue : Méthodes normales conservées, nouvelles méthodes sécurisées ajoutées
- Support des deux formats : byte[] et chaînes hexadécimales pour flexibilité

**Problèmes résolus** :
- Timing attacks : Toutes les comparaisons utilisent maintenant `ConstantTimeEquals()`
- Cache side-channel attacks : Nettoyage du cache après opérations sensibles
- Variations de timing : Opérations à temps constant indépendantes du contenu des données
- Patterns révélateurs : Accès mémoire uniformes pour masquer les accès dépendants des données

**Avantages** :
- ✅ Protection contre timing attacks (pas de fuite d'information via temps d'exécution)
- ✅ Protection contre cache side-channel attacks (nettoyage du cache)
- ✅ Opérations à temps constant (temps d'exécution indépendant des données secrètes)
- ✅ Accès mémoire uniformes (masquage des patterns révélateurs)
- ✅ API PowerShell intuitive avec cmdlets dédiés
- ✅ Compatibilité avec code existant (pas de breaking changes)
- ✅ Tests de validation complets (12 tests réussis)

**Cas d'usage** :
- Décapsulation Kyber dans environnements sensibles
- Vérification de signatures Dilithium avec protection renforcée
- Comparaison de clés partagées sans fuite d'information
- Applications nécessitant une résistance aux attaques par canaux auxiliaires
- Conformité aux standards de sécurité (FIPS 140-2, Common Criteria)

**Tests de validation** :
Le script `SideChannelProtectionTests.ps1` valide :
- ✅ Comparaisons en temps constant (clés identiques, différentes, hexadécimales)
- ✅ Décapsulation sécurisée Kyber (produit les mêmes résultats que la méthode normale)
- ✅ Vérification sécurisée Dilithium (signatures valides et invalides)
- ✅ Tests de timing (vérification que les opérations prennent un temps similaire)
- ✅ Détection de différences à toutes les positions
- ✅ Intégration complète avec protections side-channel
- ✅ Support de différentes tailles de données (16, 32, 64, 128 bytes)

**Résultats des tests** :
- 12 tests réussis
- 0 tests échoués
- Toutes les protections validées et fonctionnelles

**Références** :
- FIPS 140-2 : Security Requirements for Cryptographic Modules
- NIST SP 800-90A : Recommendation for Random Number Generation
- OWASP : Cryptographic Storage Cheat Sheet
- Bernstein, D. J. : "Cache-timing attacks on AES" (2005)
- Osvik, D. A., et al. : "Cache attacks and countermeasures: the case of AES" (2006)
- .NET Documentation : MethodImplAttribute, Thread.MemoryBarrier

### 5.17 Phase 17 : Service KyberDaemon et client KyberCLI

**Objectif** : Remplacer l'ancienne implémentation réseau par un service de type `sshd` et un client multiplateforme.

**Réalisations** :
- ✅ Création du projet `KyberDaemon` (net8.0) : listener TCP, handshake Kyber/Dilithium, canal AEAD, spawner de shell
- ✅ Gestion des processus enfants isolés (`ShellProcessHost`) avec marqueurs de fin et canaux stdout/stderr sécurisés
- ✅ Implémentation du canal chiffré générique (`SecureMessageChannel`) pour les messages post-handshake
- ✅ Création du client `KyberCLI` (net8.0) : handshake complet, authentification challenge/réponse (`password` ou `key`), mode interactif
- ✅ Cmdlet `Invoke-SecureCommand` mise à jour pour piloter KyberCLI (mot de passe pré-haché ou signature Dilithium)
- ✅ Scripts de service (`deploy/kyberd.service`, `install-kyberd-service.ps1`, `uninstall-kyberd-service.ps1`)
- ✅ Documentation mise à jour (README, PROTOCOLE_COMMUNICATION_SECURISEE)

**Décisions** :
- Déprécier `SecureCommunicationServer/Client` au profit de KyberDaemon/KyberCLI
- Conserver les cmdlets de génération de clés et de tests cryptographiques
- Utiliser `dotnet publish` pour distribuer le service et le client (Windows/Linux)

### 5.18 Phase 18 : Transition documentaire et gouvernance des cmdlets

**Objectif** : Documenter la migration vers KyberDaemon/KyberCLI et encadrer l'usage des anciennes cmdlets.

**Réalisations** :
- ✅ Mise à jour du README (section dédiée KyberDaemon & KyberCLI, intégration PowerShell)
- ✅ Mise à jour du protocole (guide orienté service + CLI, procédures Windows/Linux)
- ✅ Annotation `[Obsolete]` des cmdlets réseau héritées, conservation pour compatibilité
- ✅ Ajout d'erreurs explicites guidant les utilisateurs vers la nouvelle architecture
- ✅ Alignement du manifeste PowerShell (cmdlets conservées mais marquées obsolètes)

**Décisions** :
- Séparer clairement la pile "cryptographie" (cmdlets de clés) de la pile "communication" (service/CLI)
- Maintenir les anciens scripts d'exemples pour référence mais les étiqueter comme historiques
- Centraliser la publication du service dans `deploy/`

### 5.19 Phase 19 : Hébergement natif (Windows Service / systemd)

**Objectif** : Transformer KyberDaemon en véritable service géré par le système (SCM Windows, systemd Linux) et fiabiliser la configuration / l'expérience interactive.

**Réalisations** :
- ✅ Migration vers le **Generic Host .NET 8** (`UseWindowsService`, `UseSystemd`) avec `KyberHostedService`
- ✅ Ajout du `CancellationToken` et arrêt propre dans `KyberDaemonService` + `ConnectionListener`
- ✅ Résolution automatique des chemins (`kyberd.conf`, `auth.conf`, `keys`, `logs`) à partir d'`AppContext.BaseDirectory`
- ✅ Shell Windows par défaut : **PowerShell Core (`pwsh.exe`)** avec arguments `-NoLogo -NoProfile`
- ✅ Auto-complétion côté client (tab) via la bibliothèque `System.ReadLine`
- ✅ Packaging mis à jour : scripts de service, arborescence `publish/<runtime>/`, configuration copiée dans `%PROGRAMDATA%` / `/etc/kyberd`
- ✅ Script centralisé `scripts/build-all.ps1` pour compiler Domain/Library/Shared/Daemon/CLI en une commande

**Décisions** :
- Déployer KyberDaemon comme service Windows/systemd (plus de console auto-fermée par SCM)
- Préremplir `auth.conf` et créer le dossier `keys` lors de l'installation
- Collecter les commandes PowerShell côté serveur pour alimenter l'auto-complétion locale du client

### 5.20 Phase 20 : Renforcement avancé des protections side-channel

**Objectif** : Étendre les contre-mesures existantes pour qu'elles soient systématiquement appliquées au cœur métier (`SecureSession`) et couvrir les opérations sensibles (génération de clés, encapsulation, dérivation, AEAD, signatures).

**Réalisations** :
- ✅ Création de l'interface `ISideChannelDefense` + enum `SideChannelOperation` dans `KyberDomain`
- ✅ Intégration directe dans `SecureSession` via hooks `BeforeOperation/AfterOperation`
- ✅ Ajout de la stratégie `AdvancedSideChannelDefense` (flush cache, accès mémoire uniforme, délai aléatoire contrôlé)
- ✅ Propagation automatique via `SecureSessionFactory` (instanciation par défaut)
- ✅ Protection des comparaisons critiques (nonces, transcripts) avec instrumentation uniforme

**Décisions** :
- Centraliser les défenses dans la couche domaine (garantie d'application quelle que soit l'infrastructure)
- Éviter les branches conditionnelles dépendantes des secrets (masquage par accès séquentiels)
- Introduire un léger bruit temporel contrôlé pour limiter les corrélations temporelles sans pénaliser les performances

**Bénéfices** :
- ❗️Couverture homogène de toutes les opérations sensibles (Kyber, Dilithium, ChaCha20-Poly1305/AES-GCM)
- ✅ Résistance accrue aux attaques par timing/caches dans les scénarios client/serveur réels
- ✅ Compatibilité inchangée pour les consommateurs (`ISecureSessionFactory`)
- ✅ Possibilité future d'injecter d'autres stratégies (`HardwareIsolationSideChannelDefense`, etc.)

### 5.21 Phase 21 : Evolution du client KyberCLI (assistant interactif)

**Objectif** : Enrichir l'expérience utilisateur côté CLI (auto-complétion, validations, exemples interactifs) pour fluidifier l'exploitation quotidienne de KyberDaemon.

**Réalisations** :
- ✅ Création d'un `InteractiveCommandAssistant` : gestion unifiée des complétions, validations et scénarios pré-enregistrés
- ✅ Auto-complétion multi-source (commandes PowerShell distantes, directives locales, exemples prêts à l'emploi adaptés Windows/Linux)
- ✅ Validation temps réel : avertissement si la commande n'existe pas côté serveur, confirmation requise avant exécution
- ✅ Directives locales (`:help`, `:examples`, `:run`, `:history`, `:validate`, `:clear`) pour consulter l'aide, lancer des scénarios, revoir l'historique ou simuler une commande
- ✅ Bibliothèque d'exemples contextualisés (diagnostic système, réseau, journaux) avec exécution rapide via `:run <alias>`

**Décisions** :
- Centraliser la logique d'assistance côté client pour éviter tout aller-retour inutile avec le daemon
- Préserver la compatibilité non interactive (`--command`) en affichant simplement un avertissement si la commande est inconnue
- Prévoir l'extension future du catalogue d'exemples (fichier de configuration, scripts partagés)

**Bénéfices** :
- ✅ Prise en main plus rapide (aide intégrée, exemples guidés)
- ✅ Réduction des erreurs de frappe grâce à la validation anticipée
- ✅ Harmonisation Windows/Linux : exemples adaptés automatiquement
- ✅ Historique local consultable à tout moment (`:history`)

### 5.22 Phase 22 : Hébergement consolidé & journalisation structurée

**Objectif** : Finaliser l'intégration au Generic Host (.NET 8) et offrir une journalisation robuste (JSON + rotation) pour l'exploitation en production.

**Réalisations** :
- ✅ Migration vers `Host.CreateDefaultBuilder` avec `UseWindowsService` / `UseSystemd` et configuration du contenu (AppContext.BaseDirectory)
- ✅ Unification de la configuration des services via DI (`DaemonHostArguments`, `KyberDaemonService`, `KyberHostedService`)
- ✅ Nouveau `DaemonLogger` : logs structurés JSON, métadonnées (process/thread) et diffusion console + fichiers + EventLog/Syslog
- ✅ Implémentation d'un `LogFileSink` avec rotation (taille configurable, rétention en nombre de fichiers)
- ✅ Exposition des paramètres de log dans `kyberd.conf` (`log_directory`, `log_file_name`, `log_max_size_mb`, `log_max_files`)

**Décisions** :
- S'appuyer sur la pipeline logging Microsoft pour les diagnostics génériques (console) tout en conservant un logger dédié pour l'audit sécurité
- Utiliser un format JSON linéaire pour faciliter l'ingestion par des SIEM/ELK
- Appliquer une rotation basée sur la taille (par défaut 20 MB, 5 archives) afin d'éviter les saturations disque

**Bénéfices** :
- ✅ Déploiement homogène Windows/Linux (services natifs + Generic Host)
- ✅ Traçabilité améliorée (JSON + métadonnées process/thread) et support des agrégateurs de logs
- ✅ Maintenance simplifiée (configuration textuelle, rotation intégrée, messages identiques console/fichier)

### 5.23 Phase 23 : Pool multi-sessions et quotas utilisateurs

**Objectif** : Encadrer le nombre de sessions simultanées (globales et par utilisateur) pour garantir la stabilité du service et anticiper une montée en charge.

**Réalisations** :
- ✅ Création de `SessionQuotaManager` : sémaphore global + quotas utilisateurs (dictionnaire concurrent)
- ✅ Attribution de slots dès l'arrivée d'une connexion (`TryAcquireGlobalAsync`) et libération automatique via `SessionContext`
- ✅ Limitation par utilisateur (`max_sessions_per_user`) avec rejet explicite et message `AuthFailure`
- ✅ Journalisation détaillée (ID de session, remote endpoint, compteurs actifs)
- ✅ Mise à jour de `kyberd.conf` et de la documentation (README, QUICKSTART) pour exposer les nouveaux paramètres

**Décisions** :
- Rejet immédiat si la capacité globale ou utilisateur est saturée (pas de file d'attente pour éviter la saturation mémoire)
- Laisser la configuration textuelle piloter les quotas pour faciliter l'intégration DevOps
- Conserver un shell dédié par session (pas de réutilisation pour éviter les fuites de contexte)

**Bénéfices** :
- ✅ Protection contre les dépassements de ressources (DoS accidentel ou intentionnel)
- ✅ Répartition équitable entre comptes utilisateurs
- ✅ Observabilité accrue (logs JSON avec compteur actif)

---

## 6. Choix Techniques

### 6.1 Framework .NET

**Choix** : .NET Standard 2.0

**Raisons** :
- ✅ Compatibilité avec PowerShell Core (cross-platform)
- ✅ Compatibilité avec Windows PowerShell 5.1
- ✅ Support de toutes les fonctionnalités nécessaires
- ✅ Large base d'installations

**Alternatives rejetées** :
- .NET Framework 4.x : Limité à Windows
- .NET 5+ : Non compatible avec PowerShell 5.1

### 6.2 Bibliothèque Cryptographique

**Choix initial** : BouncyCastle.NetCore 2.2.1  
**Choix actuel** : BouncyCastle.Cryptography 2.6.2

**Raisons de la migration** :
- ✅ Support de Dilithium (ML-DSA) dans la version 2.6.2
- ✅ Support amélioré de ML-KEM avec les classes `MLKem*`
- ✅ Versions plus récentes et maintenues
- ✅ Support complet des standards NIST PQC

**Raisons du choix initial** :
- ✅ Implémentation officielle conforme NIST
- ✅ Support complet Kyber (ML-KEM) et Ed25519
- ✅ Actif et maintenu
- ✅ License MIT (permissive)
- ✅ Documentation disponible

**Vérifications effectuées** :
- ✅ Tests de conformité avec les spécifications NIST
- ✅ Validation des tailles de clés
- ✅ Tests de performance
- ✅ Validation post-migration (Kyber fonctionne avec MLKem*)
- ✅ Validation Dilithium (ML-DSA) avec MLDsa*

**Références** :
- Voir `MIGRATION_BOUNCYCASTLE.md` pour les détails de la migration

### 6.3 Format de Sérialisation

**Choix** : Format texte Key=Value

**Raisons** :
- ✅ Pas de dépendances externes (évite CVE)
- ✅ Format lisible par l'humain
- ✅ Facile à parser
- ✅ Compatible avec tous les systèmes
- ✅ Pas de vulnérabilités de sécurité connues

**Alternatives rejetées** :
- JSON (System.Text.Json) : Vulnérabilité CVE-2024-30105
- ASN.1 : Complexe, nécessite une bibliothèque externe
- Format binaire : Non lisible, difficile à déboguer

### 6.4 Architecture du Module

**Choix** : Module PowerShell Binaire (Standard Library)

**Raisons** :
- ✅ Performances optimales (C# compilé)
- ✅ Typage fort
- ✅ Intégration native PowerShell
- ✅ Support cross-platform
- ✅ Réutilisabilité de la bibliothèque .NET

**Structure** :
- Bibliothèque .NET séparée (`KyberLibrary`)
- Module PowerShell utilisant la bibliothèque
- Séparation claire des responsabilités

---

## 7. Tests et Validations

### 7.1 Tests Unitaires

**Scripts de test créés** :
1. `Test.ps1` : Tests de base pour Kyber
2. `QuickTestEd25519.ps1` : Tests rapides Ed25519
3. `TestKyberEd25519.ps1` : Tests combinés complets

**Tests effectués** :

#### 7.1.1 Tests Kyber
- ✅ Génération de clés (512, 768, 1024)
- ✅ Encapsulation/Décapsulation
- ✅ Correspondance des clés partagées
- ✅ Tailles des clés et ciphertexts
- ✅ Export/Import de clés

#### 7.1.2 Tests Ed25519
- ✅ Génération de clés
- ✅ Signature de données
- ✅ Vérification de signatures
- ✅ Test avec données modifiées (rejet attendu)
- ✅ Export/Import de clés

#### 7.1.3 Tests Combinés
- ✅ Génération Kyber + Ed25519
- ✅ Encapsulation Kyber + Signature Ed25519
- ✅ Vérification complète
- ✅ Décapsulation + Vérification
- ✅ Export/Import avec clés rechargées

#### 7.1.4 Tests Dilithium
- ✅ Génération de clés Dilithium (ML-DSA)
- ✅ Signature de données
- ✅ Vérification de signatures
- ✅ Test avec données modifiées (rejet attendu)
- ✅ Export/Import de clés
- ✅ Différents paramètres de sécurité (Dilithium2, Dilithium3, Dilithium5)

#### 7.1.5 Tests Combinés Post-Quantum
- ✅ Génération Kyber + Dilithium
- ✅ Encapsulation Kyber + Signature Dilithium
- ✅ Vérification complète 100% post-quantique
- ✅ Décapsulation + Vérification
- ✅ Scénarios serveur/client complets

### 7.2 Validation de Conformité

**Spécifications vérifiées** :
- ✅ Tailles de clés conformes NIST FIPS 203
- ✅ Tailles de ciphertexts conformes
- ✅ Tailles de signatures Ed25519 conformes (64 bytes)
- ✅ Tailles de clés Ed25519 conformes (32 bytes)

**Résultats** :
| Paramètre | Clé publique | Clé privée | Ciphertext | Clé partagée |
|-----------|-------------|------------|------------|--------------|
| Kyber512  | 800 bytes   | 1632 bytes | 768 bytes  | 32 bytes     |
| Kyber768  | 1184 bytes  | 2400 bytes | 1088 bytes | 32 bytes     |
| Kyber1024 | 1568 bytes  | 3168 bytes | 1568 bytes | 32 bytes     |

✅ Toutes les tailles correspondent aux spécifications NIST.

### 7.3 Tests de Sécurité

**Tests effectués** :
1. **Test de rejet** : Signature invalide avec données modifiées
   - ✅ Rejet correct d'une signature modifiée
   
2. **Test d'intégrité** : Clés partagées après encapsulation/décapsulation
   - ✅ Correspondance parfaite des clés partagées
   
3. **Test de format** : Sérialisation/désérialisation
   - ✅ Reconstruction correcte des clés privées
   - ✅ Format personnalisé détecté et utilisé

### 7.4 Tests de Performance

**Métriques observées** :
- Génération de clés Kyber768 : < 100ms
- Encapsulation : < 50ms
- Décapsulation : < 50ms
- Génération de clés Ed25519 : < 10ms
- Signature Ed25519 : < 10ms
- Vérification Ed25519 : < 10ms
- Génération de clés Dilithium3 : < 150ms
- Signature Dilithium3 : < 100ms
- Vérification Dilithium3 : < 100ms

**Conclusion** : Performances acceptables pour une utilisation en production. Dilithium est légèrement plus lent que Ed25519 mais offre une sécurité post-quantique complète.

---

## Phase 16 : Audit et Validation Cryptographique

### 16.1 Objectifs

L'objectif de cette phase était d'implémenter un système complet d'audit et de validation cryptographique pour :
- **Tests de conformité** : Valider que les implémentations respectent les standards NIST (FIPS 203, ML-DSA, RFC 8032)
- **Tests de fuzzing** : Tester la robustesse des implémentations face à des données corrompues ou invalides
- **Tests de validation cryptographique** : Vérifier les propriétés cryptographiques (intégrité, authenticité, non-déterminisme)

### 16.2 Réalisations

#### 16.2.1 Classes Utilitaires

**CryptographicConformanceTests.cs** :
- Classe statique pour les tests de conformité NIST
- Validation des tailles de clés, ciphertexts et signatures selon les standards
- Validation du non-déterminisme des générations de clés
- Validation de la correspondance des clés partagées et signatures
- Support complet pour Kyber (ML-KEM), Dilithium (ML-DSA) et Ed25519

**CryptographicFuzzing.cs** :
- Classe statique pour les tests de fuzzing
- Génération de données corrompues (bytes individuels, multiples, patterns suspects)
- Génération de données avec tailles invalides
- Génération de ciphertexts, signatures et clés corrompus
- Tests de robustesse avec capture d'exceptions

#### 16.2.2 Cmdlets PowerShell

**Test-CryptographicConformance** :
- Tests de conformité pour tous les algorithmes ou un algorithme spécifique
- Validation des tailles selon NIST FIPS 203, ML-DSA et RFC 8032
- Validation des propriétés cryptographiques
- Option pour afficher uniquement les tests échoués

**Invoke-CryptographicFuzzing** :
- Tests de fuzzing avec nombre d'itérations configurable
- Génération automatique de données corrompues
- Tests de robustesse pour Kyber, Dilithium et Ed25519
- Option pour afficher uniquement les problèmes (crashes ou échecs)

#### 16.2.3 Script d'Audit Complet

**CryptographicAuditTests.ps1** :
- Script PowerShell complet pour tous les tests d'audit
- Tests de conformité NIST
- Tests de fuzzing
- Tests de validation cryptographique (intégrité, authenticité, rejet, non-déterminisme)
- Tests de stress (génération multiple, encapsulation/décapsulation multiple)
- Résumé détaillé des résultats

### 16.3 Changements Techniques

#### 16.3.1 Nouvelles Classes

1. **CryptographicConformanceTests.cs** :
   - Dictionnaires de tailles conformes NIST pour Kyber, Dilithium et Ed25519
   - Méthodes de validation pour chaque type de test
   - Classe `ConformanceTestResult` pour les résultats

2. **CryptographicFuzzing.cs** :
   - Méthodes de génération de données corrompues
   - Méthodes de génération de patterns suspects
   - Classe `FuzzingTestResult` pour les résultats
   - Classe `FuzzingException` pour capturer les exceptions

#### 16.3.2 Nouveaux Cmdlets

1. **TestCryptographicConformanceCommand.cs** :
   - Paramètres pour sélectionner l'algorithme et les paramètres de sécurité
   - Tests automatiques pour Kyber, Dilithium et Ed25519
   - Retour de résultats structurés

2. **InvokeCryptographicFuzzingCommand.cs** :
   - Paramètres pour configurer les tests de fuzzing
   - Génération automatique de données corrompues
   - Tests de robustesse avec gestion d'exceptions

### 16.4 Décisions Techniques

1. **Architecture des Tests** :
   - Séparation entre tests de conformité et tests de fuzzing
   - Classes utilitaires statiques pour réutilisabilité
   - Résultats structurés avec métadonnées

2. **Génération de Données Corrompues** :
   - Corruption de bytes individuels et multiples
   - Patterns suspects (tous zéros, tous FF, alternance)
   - Tailles invalides (trop courtes, trop longues)

3. **Gestion des Exceptions** :
   - Capture de toutes les exceptions lors des tests de fuzzing
   - Distinction entre échecs attendus (rejet de données invalides) et crashes (exceptions non gérées)

### 16.5 Problèmes Résolus

1. **Validation des Tailles** :
   - ✅ Implémentation de dictionnaires de tailles conformes NIST
   - ✅ Tolérance pour Dilithium (tailles variables selon l'implémentation)

2. **Tests de Fuzzing** :
   - ✅ Génération automatique de données corrompues
   - ✅ Tests de robustesse avec gestion d'exceptions
   - ✅ Distinction entre échecs attendus et crashes

3. **Intégration PowerShell** :
   - ✅ Cmdlets avec paramètres configurables
   - ✅ Retour de résultats structurés
   - ✅ Script d'audit complet avec résumé

### 16.6 Avantages

1. **Validation Complète** :
   - ✅ Conformité aux standards NIST vérifiée automatiquement
   - ✅ Robustesse testée avec données corrompues
   - ✅ Propriétés cryptographiques validées

2. **Facilité d'Utilisation** :
   - ✅ Cmdlets PowerShell intuitifs
   - ✅ Script d'audit complet en une commande
   - ✅ Résultats structurés et lisibles

3. **Maintenabilité** :
   - ✅ Tests automatisés pour validation continue
   - ✅ Détection précoce de problèmes de conformité
   - ✅ Documentation complète des tests

### 16.7 Cas d'Usage

1. **Validation de Conformité** :
   ```powershell
   # Tester la conformité de tous les algorithmes
   Test-CryptographicConformance -Algorithm All
   ```

2. **Tests de Fuzzing** :
   ```powershell
   # Exécuter des tests de fuzzing
   Invoke-CryptographicFuzzing -Algorithm All -Iterations 100
   ```

3. **Audit Complet** :
   ```powershell
   # Exécuter tous les tests d'audit
   .\CryptographicAuditTests.ps1
   ```

### 16.8 Références

- **NIST FIPS 203** : Module-Lattice-Based Key-Encapsulation Mechanism (ML-KEM)
- **NIST ML-DSA** : Module-Lattice-Based Digital Signature Algorithm
- **RFC 8032** : Edwards-Curve Digital Signature Algorithm (EdDSA)
- **OWASP Cryptographic Storage Cheat Sheet** : Bonnes pratiques de sécurité cryptographique
- **FIPS 140-2** : Security Requirements for Cryptographic Modules

---

## 8. Conclusion et Perspectives

### 8.1 Objectifs Atteints

✅ **Module PowerShell fonctionnel** pour Kyber (ML-KEM)  
✅ **Support complet Dilithium (ML-DSA)** pour la signature numérique post-quantique  
✅ **Support complet Ed25519** pour la signature numérique classique (compatibilité)  
✅ **API intuitive** via des cmdlets PowerShell  
✅ **Compatibilité cross-platform** (PowerShell Core + Windows PowerShell)  
✅ **Sécurité garantie** via BouncyCastle.Cryptography 2.6.2 (implémentation validée)  
✅ **Format de sérialisation sécurisé** (sans vulnérabilités connues)  
✅ **Protocoles combinés 100% post-quantique** (Kyber + Dilithium)  
✅ **Gestion sécurisée des clés en mémoire** (zeroization, nettoyage automatique)  
✅ **Protection contre les canaux auxiliaires** (timing attacks, cache side-channel attacks)  
✅ **Audit et validation cryptographique** (tests de conformité NIST, fuzzing, validation cryptographique)  
✅ **Documentation complète** avec exemples  
✅ **Service KyberDaemon et client KyberCLI** pour les communications chiffrées de type SSH  
✅ **Documentation complète** mise à jour (README, protocole, scripts de service)  

### 8.2 Points Forts

1. **Architecture modulaire** : Séparation claire Library/Module
2. **Format de sérialisation robuste** : Magic number "KYBE" pour la détection
3. **Sécurité** : Pas de dépendances vulnérables
4. **Gestion sécurisée des clés** : Zeroization et protection contre récupération de mémoire
5. **Protection side-channel** : Contre-mesures contre timing attacks et cache side-channel attacks
6. **Service KyberDaemon + client KyberCLI** : Remplacement pérenne des anciennes cmdlets réseau
7. **Audit et validation** : Tests de conformité NIST, fuzzing et validation cryptographique
8. **Documentation** : Exemples complets et scripts de test
9. **Compatibilité** : Support cross-platform complet

### 8.3 Limitations Connues

1. **Reflexion pour sérialisation** : Dépend de la structure interne de BouncyCastle
   - **Mitigation** : Format personnalisé avec magic number pour compatibilité future
   
2. **Gestion mémoire** : Les clés normales (New-*KeyPair) ne sont pas automatiquement nettoyées
   - **Mitigation** : Utiliser `New-Secure*KeyPair` pour nettoyage automatique
   - **Mitigation** : Utiliser `Clear-SecureKey` pour nettoyage manuel des clés normales
   - **Recommandation** : Utiliser les cmdlets sécurisés pour les clés sensibles

3. **Ed25519 non post-quantique** : Algorithme classique, pas résistant aux ordinateurs quantiques
   - **Mitigation** : Dilithium (ML-DSA) disponible pour une solution 100% post-quantique
   - **Recommandation** : Utiliser Kyber + Dilithium pour une sécurité post-quantique complète

4. **Attaques Cold Boot** : Le nettoyage multi-passes réduit mais n'élimine pas complètement le risque
   - **Mitigation** : Nettoyage multi-passes disponible avec `Clear-SecureKey -Passes 3`
   - **Recommandation** : Utiliser le nettoyage multi-passes pour clés hautement sensibles

5. **Protection side-channel** : Les contre-mesures réduisent mais n'éliminent pas complètement le risque
   - **Mitigation** : Utiliser les méthodes sécurisées (`DecapsulateSecure()`, `VerifySecure()`)
   - **Recommandation** : Utiliser `Test-ConstantTimeCompare` pour toutes les comparaisons sensibles
   - **Note** : Les variations de timing peuvent toujours exister en raison du système d'exploitation et du matériel
6. **Cmdlets réseau historiques** : `Start/Connect/Send/Receive-Secure*` conservées mais obsolètes
   - **Mitigation** : Utiliser KyberDaemon + KyberCLI ou la cmdlet `Invoke-SecureCommand`
   - **Recommandation** : Ne pas déployer les anciennes cmdlets en production

### 8.4 Perspectives d'Amélioration

#### 8.4.1 Court Terme
- [x] Ajout de fonctions de hachage modernes (SHA3-256, SHA3-384) ✅ **Réalisé**
  - **Voir** : Phase 9 dans l'évolution du projet
- [x] Implémentation du chiffrement symétrique Authenticated Encryption (ChaCha20-Poly1305, AES-GCM) ✅ **Réalisé**
  - **Voir** : Phase 10 dans l'évolution du projet
- [x] Support de formats de clés standards (PKCS#8, X.509) ✅ **Réalisé**
  - **Voir** : Phase 11 dans l'évolution du projet
- [x] Scripts de benchmark de performance ✅ **Réalisé**
  - **Voir** : Phase 13 dans l'évolution du projet
- [x] Support TLS 1.3 Post-Quantum Hybride ✅ **Réalisé**
  - **Voir** : Phase 12 dans l'évolution du projet
- [x] Gestion sécurisée des clés en mémoire (zeroization) ✅ **Réalisé**
  - **Voir** : Phase 14 dans l'évolution du projet
- [x] Implémentation des contre-mesures contre canaux auxiliaires (timing, cache side-channel attacks) ✅ **Réalisé**
  - **Voir** : Phase 15 dans l'évolution du projet
  - **Voir** : `CONTRE_MESURES_SIDE_CHANNEL.md` pour la documentation complète
- [x] Audit et validation cryptographique (tests de conformité, fuzzing) ✅ **Réalisé**
  - **Voir** : Phase 16 dans l'évolution du projet
- [ ] Finalisation du support CMS (signatures)

#### 8.4.2 Moyen Terme
- [x] Support d'autres algorithmes post-quantiques (Dilithium pour signatures) ✅ **Réalisé**
  - **Voir** : `ANALYSE_DILITHIUM.md` pour l'analyse complète
  - **Voir** : `MIGRATION_BOUNCYCASTLE.md` pour la migration vers BouncyCastle.Cryptography 2.6.2
- [x] Gestion sécurisée des clés en mémoire ✅ **Réalisé**
  - **Voir** : Phase 14 dans l'évolution du projet
  - **Voir** : `GESTION_SECURISEE_CLES.md` pour la documentation complète
- [x] Implémentation des contre-mesures contre canaux auxiliaires (timing, cache side-channel attacks) ✅ **Réalisé**
  - **Voir** : Phase 15 dans l'évolution du projet
  - **Voir** : `CONTRE_MESURES_SIDE_CHANNEL.md` pour la documentation complète
- [x] Audit et validation cryptographique (tests de conformité, fuzzing) ✅ **Réalisé**
  - **Voir** : Phase 16 dans l'évolution du projet
- [ ] Intégration avec des protocoles standards (TLS, SSH)
- [ ] Interface graphique pour la gestion de clés
- [ ] Optimisations de performance pour Dilithium

#### 8.4.3 Long Terme
- [ ] Support de la cryptographie hybride (classique + post-quantique)
- [ ] Migration automatique vers des algorithmes post-quantiques
- [ ] Intégration avec des systèmes de gestion de clés (PKI)

### 8.5 Recommandations

1. **Pour la production** :
   - Utiliser Kyber768 (niveau de sécurité recommandé)
   - ⭐ **Recommandé** : Combiner avec Dilithium3 (ML-DSA-65) pour un protocole 100% post-quantique
   - Alternative : Combiner avec Ed25519 pour compatibilité (non post-quantique)
   - ⭐ **Utiliser `New-Secure*KeyPair`** pour les clés sensibles avec nettoyage automatique
   - ⭐ **Utiliser `Invoke-KyberDecapsulateSecure` et `Test-DilithiumSignatureSecure`** pour protection side-channel
   - Sauvegarder les clés privées de manière sécurisée
   - Valider les signatures avant utilisation
   - Nettoyer les clés privées après utilisation avec `Clear-SecureKey -Passes 3`
   - Utiliser `Test-ConstantTimeCompare` pour toutes les comparaisons sensibles

2. **Pour le développement** :
   - Suivre les mises à jour de BouncyCastle
   - Surveiller les CVE des dépendances
   - Tester régulièrement avec les scripts fournis
   - Utiliser les cmdlets sécurisés pour éviter les fuites de mémoire

3. **Pour la sécurité** :
   - Ne jamais exposer les clés privées
   - Utiliser `New-Secure*KeyPair` pour les clés sensibles
   - Nettoyer immédiatement les clés privées après utilisation
   - Utiliser des canaux sécurisés pour l'échange de clés publiques
   - Valider toutes les signatures avant traitement
   - Utiliser le pattern try-finally pour garantir le nettoyage
   - Utiliser les méthodes sécurisées pour protection contre side-channel attacks
   - Éviter les comparaisons directes de données sensibles (utiliser `Test-ConstantTimeCompare`)

---

## 9. Références et Ressources

### 9.1 Standards et Spécifications

- **NIST FIPS 203** : Module-Lattice-Based Key-Encapsulation Mechanism (ML-KEM)
- **NIST PQC Standardization Process** : https://csrc.nist.gov/projects/post-quantum-cryptography
- **RFC 8032** : Edwards-Curve Digital Signature Algorithm (EdDSA)

### 9.2 Bibliothèques et Outils

- **BouncyCastle.Cryptography 2.6.2** : https://www.nuget.org/packages/BouncyCastle.Cryptography/ (version actuelle)
- **BouncyCastle.NetCore 2.2.1** : Version précédente (remplacée)
- **PowerShell Standard Library** : https://github.com/PowerShell/PowerShellStandard
- **.NET Standard** : https://docs.microsoft.com/dotnet/standard/net-standard

### 9.3 Documentation de Recherche

- **BC-CSharpDotNet-UserGuide.pdf** : Guide utilisateur BouncyCastle pour .NET
- **NIST PQC Documentation** : Documentation officielle des algorithmes post-quantiques
- **BouncyCastle Source Code** : Analyse du code source pour comprendre les formats internes
- **ANALYSE_DILITHIUM.md** : Analyse complète de l'intégration de CRYSTALS-Dilithium (ML-DSA)
- **MIGRATION_BOUNCYCASTLE.md** : Guide de migration de BouncyCastle.NetCore vers BouncyCastle.Cryptography 2.6.2
- **GESTION_SECURISEE_CLES.md** : Documentation complète de la gestion sécurisée des clés en mémoire
- **CONTRE_MESURES_SIDE_CHANNEL.md** : Documentation complète des contre-mesures contre les canaux auxiliaires
- **NIST SP 800-57** : Recommendation for Key Management
- **FIPS 140-2** : Security Requirements for Cryptographic Modules
- **OWASP Cryptographic Storage Cheat Sheet** : Bonnes pratiques de sécurité cryptographique

### 9.4 Outils de Développement

- **Visual Studio Code** : Éditeur principal
- **PowerShell Extension** : Support PowerShell dans VS Code
- **.NET CLI** : Compilation et build
- **Git** : Gestion de version

---

## 10. Annexes

### 10.1 Chronologie du Projet

| Date | Étape | Résultat |
|------|-------|----------|
| Nov 2025 | Recherche initiale | Sélection de BouncyCastle et architecture |
| Nov 2025 | Implémentation Kyber | Module de base fonctionnel |
| Nov 2025 | Résolution sérialisation | Format personnalisé avec magic number |
| Nov 2025 | Refactorisation | Architecture en deux couches |
| Nov 2025 | Export/Import | Cmdlets de sauvegarde/chargement |
| Nov 2025 | Intégration Ed25519 | Support complet de la signature |
| Nov 2025 | Tests combinés | Protocole complet validé |
| Nov 2025 | Documentation | Documentation complète |
| Nov 2025 | Migration BouncyCastle | Migration vers BouncyCastle.Cryptography 2.6.2 |
| Nov 2025 | Intégration Dilithium | Support complet de ML-DSA (100% post-quantique) |
| Nov 2025 | Protocoles combinés | Scénarios Kyber + Dilithium (100% post-quantique) |
| Nov 2025 | SHA3 et ML-DSA | Implémentation SHA3-256/384 avec pré-hachage pour ML-DSA |
| Nov 2025 | Authenticated Encryption | Implémentation ChaCha20-Poly1305 et AES-GCM |
| Nov 2025 | Conformité PKCS/X.509 | Support PKCS#8, X.509, CMS pour intégration PKI |
| Nov 2025 | TLS 1.3 Hybrid | Support TLS 1.3 post-quantique hybride (classique + post-quantique) |
| Nov 2025 | Benchmark Performance | Script de benchmark de performance pour tous les algorithmes |
| Nov 2025 | Gestion Sécurisée des Clés | Implémentation zeroization et gestion sécurisée en mémoire |
| Nov 2025 | Protection Side-Channel | Implémentation contre-mesures contre timing attacks et cache side-channel attacks |
| Nov 2025 | Audit et Validation Cryptographique | Implémentation tests de conformité NIST, fuzzing et validation cryptographique |
| Nov 2025 | Service KyberDaemon & KyberCLI | Nouvelle architecture client/serveur pour les communications sécurisées |
| Nov 2025 | Transition documentaire | Documentation mise à jour, scripts de service, obsolescence des anciennes cmdlets |

### 10.2 Métriques du Projet

- **Lignes de code C#** : ~8000
- **Lignes de code PowerShell** : ~3100
- **Cmdlets créés** : 42 (Kyber: 6, Dilithium: 5, Ed25519: 6, SHA3: 1, ChaCha20-Poly1305: 2, AES-GCM: 2, PKCS#8: 2, X.509: 2, CMS: 1, TLS13Hybrid: 3, SecureKeyManagement: 5, SideChannelProtection: 3, CryptographicAudit: 2, Réseau (héritées/obsolètes): 4)
- **Scripts d'exemples** : 18
- **Fichiers de documentation** : 8 (README, QUICKSTART, R&D, ANALYSE_DILITHIUM, MIGRATION_BOUNCYCASTLE, GESTION_SECURISEE_CLES, CONTRE_MESURES_SIDE_CHANNEL, PROTOCOLE_COMMUNICATION_SECURISEE)
- **Projets supplémentaires** : KyberDaemon (service) et KyberCLI (client)
- **Temps de développement** : ~7 semaines
- **Problèmes résolus** : 17 majeurs

### 10.3 Décisions d'Architecture Clés

1. **Architecture en deux couches** : Library + Module
2. **Format de sérialisation personnalisé** : Magic number "KYBE" pour Kyber
3. **Pas de dépendances vulnérables** : Format texte Key=Value
4. **Support cross-platform** : .NET Standard 2.0
5. **Protocole combiné 100% post-quantique** : Kyber + Dilithium ⭐
6. **Protocole combiné compatibilité** : Kyber + Ed25519
7. **Migration BouncyCastle** : BouncyCastle.Cryptography 2.6.2 avec ML-KEM et ML-DSA
8. **Fonctions de hachage modernes** : SHA3-256/384 avec pré-hachage pour ML-DSA
9. **Chiffrement symétrique AEAD** : ChaCha20-Poly1305 et AES-GCM
10. **Conformité standards** : PKCS#8, X.509, CMS pour intégration PKI
11. **TLS 1.3 Post-Quantum Hybride** : Combinaison classique + post-quantique pour transition progressive
12. **Benchmark de performance** : Script complet pour mesure et comparaison des performances
13. **Gestion sécurisée des clés** : Zeroization et protection contre récupération de mémoire
14. **Protection side-channel** : Contre-mesures contre timing attacks et cache side-channel attacks
15. **Audit et validation cryptographique** : Tests de conformité NIST, fuzzing et validation cryptographique

---

**Fin du Document de Recherche et Développement**

*Ce document présente l'ensemble du processus de recherche et développement pour le projet KyberModule. Toutes les décisions techniques, problèmes rencontrés et solutions trouvées sont documentées pour faciliter la maintenance et l'évolution future du projet.*

### 5.24 Phase 24 : Authentification renforcée (PBKDF Argon2id)

**Objectif** : Supprimer le stockage de secrets en clair/hex dans `auth.conf` en basculant vers des empreintes dérivées par **Argon2id** et en déplaçant la dérivation côté client.

**Réalisations** :
- ✅ Nouveau parseur `Argon2HashRecord` partagé (KyberShared) pour décoder les enregistrements `argon2id$v=…$salt$hash`
- ✅ `AuthManager` supporte les entrées Argon2id (32 octets) et conserve la compatibilité avec l'ancien format hex
- ✅ `KyberCLI connect` : `--password` + `--password-record` déclenchent la dérivation Argon2 côté client (Konscious.Security.Cryptography)
- ✅ Commande `KyberCLI passhash` pour générer sel, hash et ligne `auth.conf`
- ✅ Mise à jour des exemples (`auth.conf`, README, QUICKSTART) avec un compte Alice dérivé via Argon2id

**Décisions** :
- Rester sur Argon2id (v=19) avec paramètres par défaut : 64 MB mémoire, 3 itérations, parallélisme 2, sortie 32 octets
- Exiger la fourniture explicite de l'enregistrement Argon2 côté client afin de ne pas exposer la liste des utilisateurs/paramètres via le protocole
- Conserver `--password-hex` pour faciliter les migrations et tests existants

**Bénéfices** :
- ✅ Secrets salés stockés côté serveur (résistance aux fuites de fichiers config)
- ✅ Dérivation côté client → aucune exposition du mot de passe brut au daemon
- ✅ Outil de provisioning (`passhash`) simplifiant la création d'entrées sécurisées
- ✅ Compatibilité ascendante assurée pour les scripts historiques

### 5.25 Phase 25 : Intégration Kerberos & OAuth (sans NTLM)

**Objectif** : Permettre une authentification d'entreprise en tirant parti des infrastructures existantes (AD/Kerberos, fournisseurs OAuth2/OIDC) tout en évitant NTLM.

**Réalisations** :
- ✅ Ajout d'un `KerberosAuthenticationService` (Kerberos.NET + keytab) avec anti-replay mémoire configurable
- ✅ Entrées `auth.conf` de type `kerberos` (principal attendu) et options `kerberos_*` dans `kyberd.conf`
- ✅ Intégration OAuth2/JWT (`OAuthTokenValidator`, support JWKS, issuer, audience, scopes requis)
- ✅ KyberCLI : nouvelles options `--kerberos-token` / `--oauth-token` (ainsi que variantes fichier) + mise à jour de l'aide
- ✅ Exemples et documentation mis à jour (README, QUICKSTART, auth.conf)

**Décisions** :
- Rejeter explicitement NTLM (trop de failles) et s'appuyer sur Kerberos pour les environnements AD classiques
- Considérer le ticket Kerberos/AP-REQ comme opaque côté client (généré via outils externes), afin d'éviter la gestion d'un KDC dans KyberCLI
- Limiter OAuth aux jetons JWT signés (vérifiés localement via JWKS) pour rester stateless

**Bénéfices** :
- ✅ Interopérabilité avec les domaines Kerberos existants sans exposer de secrets
- ✅ Support des intégrations modernes (Azure AD / Entra ID, IdentityServer, Okta…) via JWT
- ✅ Configuration modulaire (activation indépendante des méthodes dans `kyberd.conf`)

### 5.26 Phase 26 : Expérience CLI enrichie (historique, alias, couleur)

**Objectif** : Améliorer l'ergonomie du client KyberCLI en retenant les commandes, en offrant des alias persistants et en rendant la sortie plus lisible.

**Réalisations** :
- ✅ Ajout d'un `CliStateStore` (`%APPDATA%/KyberCLI`) pour l'historique (`history.log`) et les alias (`aliases.conf`)
- ✅ Chargement automatique de l'historique au démarrage + append à chaque commande
- ✅ Nouvelles directives `:aliases`, `:alias`, `:unalias` + auto-complétion intégrée
- ✅ Expansion d'alias avec affichage du mapping avant exécution
- ✅ Colorisation basique (STDOUT gris, STDERR rouge) et messages d'information colorés

**Décisions** :
- Préserver le format texte simple pour alias/historique (facile à éditer/déployer)
- Pas de parsing complexe : alias mono-token remplaçant le premier mot, extension simple pour arguments supplémentaires
- Stocker les fichiers côté utilisateur (`AppData`) pour respecter les bonnes pratiques Windows/Linux

**Bénéfices** :
- ✅ Expérience interactive plus fluide (rejouer commandes, alias fréquents)
- ✅ Meilleure lisibilité des résultats (erreurs en rouge, prompts cyan)
- ✅ Complétion unifiée (commandes, alias, exemples, directives)

### 5.27 Phase 27 : Gestion sécurisée des clés serveur (chiffrement + rotation)

**Objectif** : Protéger les clés Kyber/Dilithium du démon et instaurer un cycle de rotation contrôlé.

**Réalisations** :
- ✅ Création de `KeyEncryptionService` (DPAPI sur Windows, AES-GCM + PBKDF2 via passphrase sur Linux/macOS)
- ✅ `KeyRotationService` : versionnement (`session.vN.*`), chiffrement au repos, métadonnées JSON (`metadata.json`)
- ✅ Migration automatique des installations existantes (conversion des fichiers legacy non chiffrés)
- ✅ Rotation automatique conditionnée par la configuration (`key_rotation_days`) lors de la création de session
- ✅ Injection sécurisée des clés dans `SecureSession` via `LoadKyberKeys` / `LoadDilithiumKeys`
- ✅ Support TLS côté client (`--tls`, SNI, épinglage SHA-256, option skip-verify pour les labs)
- ✅ Compatibilité ascendante : les déploiements existants sont automatiquement migrés au format chiffré
- ✅ CLI d'administration (`KyberCLI keys list|rotate|export`) et script PowerShell (`tools/Invoke-KyberKeyRotation.ps1`) pour gérer les clés hors service
- ✅ Possibilité d'exposer KyberDaemon derrière un reverse proxy TLS (nginx/haproxy) avec pinning côté client

**Décisions** :
- Format d'enveloppe JSON auto-descriptif pour les clés chiffrées (mode + paramètres) simplifiant la portabilité
- Passphrase obligatoire hors Windows pour remplacer DPAPI ; recommandation de stocker la valeur via Secret Management / vault
- Conservation des versions antérieures pour audit, tout en supprimant les anciennes copies en clair

**Bénéfices** :
- ✅ Clés serveur chiffrées au repos avec une politique unifiée multi-plateforme
- ✅ Rotation proactive (dates d'expiration) limitant l'exposition en cas de compromission
- ✅ Compatibilité ascendante : les déploiements existants sont automatiquement migrés au format chiffré
- ✅ CLI d'administration (`KyberCLI keys list|rotate|export`) et script PowerShell (`tools/Invoke-KyberKeyRotation.ps1`) pour gérer les clés hors service
- ✅ Possibilité d'exposer KyberDaemon derrière un reverse proxy TLS (nginx/haproxy) avec pinning côté client
- ✅ Mode strict (`strict_network_mode`) pour limiter l'écoute à loopback et exiger un frontal TLS
- ✅ Tests d'intégration .NET (xUnit) démarrant KyberDaemon + KyberCLI in-process (`tests/KyberIntegrationTests`) et harness de fuzzing réseau (`NetworkFuzzer`) pour injecter des handshakes aléatoires
- ✅ Benchmarks BenchmarkDotNet (`tests/KyberBenchmarks`) : mesure de latence (`CommandLatencyBenchmark`), coût du handshake et exécution concurrente (`ConcurrentCommandsBenchmark`), avec signatures Dilithium parallélisées (`SignBatch`) et zeroization renforcée (buffers temporaires nettoyés).

