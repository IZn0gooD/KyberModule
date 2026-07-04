# Contre-mesures contre les Canaux Auxiliaires (Side-Channel Attacks)

## Vue d'ensemble

Ce document décrit les contre-mesures implémentées dans le module KyberModule pour protéger contre les attaques par canaux auxiliaires (side-channel attacks). Ces attaques exploitent des informations révélées indirectement par l'implémentation cryptographique, telles que le temps d'exécution, les accès mémoire, ou la consommation d'énergie.

## Types d'attaques protégées

### 1. Timing Attacks (Attaques par temps d'exécution)

Les attaques par timing exploitent les variations de temps d'exécution pour déduire des informations sur les données secrètes (clés privées, secrets).

**Exemple d'attaque** :
- Comparaison de clés avec `==` qui s'arrête à la première différence
- Branches conditionnelles basées sur des données secrètes
- Accès mémoire dépendants des valeurs secrètes

**Protection implémentée** :
- Opérations à temps constant (`ConstantTimeOperations`)
- Comparaisons qui parcourent toujours tous les éléments
- Pas de branches conditionnelles basées sur des données secrètes

### 2. Cache Side-Channel Attacks (Attaques par cache)

Les attaques par cache exploitent les patterns d'accès mémoire révélés par le cache du processeur (L1, L2, L3).

**Exemple d'attaque** :
- Accès mémoire dépendants des données secrètes
- Patterns révélateurs dans les accès au cache
- Fuites d'information via les lignes de cache

**Protection implémentée** :
- Nettoyage du cache (`FlushCache()`)
- Accès mémoire uniformes (`UniformMemoryAccess`)
- Préchargement du cache pour masquer les patterns

### 3. Power Analysis (Analyse de consommation)

Les attaques par analyse de consommation exploitent les variations de consommation d'énergie. Moins applicable en software, mais mentionné pour complétude.

## Architecture des contre-mesures

### Classes principales

#### 1. `ConstantTimeOperations`

Classe utilitaire statique pour les opérations à temps constant.

**Méthodes principales** :

- `ConstantTimeEquals(byte[] a, byte[] b)` : Compare deux tableaux en temps constant
- `ConstantTimeSelect(bool condition, T a, T b)` : Sélection conditionnelle sans branche
- `ConstantTimeCopy(bool condition, byte[] src, byte[] dst)` : Copie conditionnelle
- `ConstantTimeIsZero(byte b)` : Vérifie si un byte est zéro
- `ConstantTimeMask(bool condition, byte value)` : Masque conditionnel

**Exemple d'utilisation** :
```csharp
// Comparaison sécurisée
bool areEqual = ConstantTimeOperations.ConstantTimeEquals(key1, key2);

// Sélection conditionnelle
byte result = ConstantTimeOperations.ConstantTimeSelect(condition, value1, value2);
```

#### 2. `SideChannelProtection`

Classe utilitaire statique pour la protection contre les attaques par canaux auxiliaires.

**Méthodes principales** :

- `FlushCache()` : Nettoie le cache du processeur
- `SecureCompare(byte[] a, byte[] b)` : Compare avec nettoyage du cache
- `ProtectTiming<T>(Func<T> operation)` : Protège une opération contre les timing attacks
- `UniformMemoryAccess(byte[] array, int actualIndex)` : Accès mémoire uniforme
- `SecureCopy(byte[] source, byte[] destination, int length)` : Copie sécurisée

**Exemple d'utilisation** :
```csharp
// Protéger une opération sensible
byte[] secret = SideChannelProtection.ProtectTiming(() => Decapsulate(ciphertext, privateKey));

// Comparaison sécurisée
bool isValid = SideChannelProtection.SecureCompare(computedHash, expectedHash);
```

## Intégration dans les wrappers

### KyberWrapper

**Méthodes sécurisées** :

- `DecapsulateSecure(byte[] ciphertext, byte[] privateKey)` : Décapsulation avec protection side-channel
- `ConstantTimeCompareSharedSecrets(byte[] secret1, byte[] secret2)` : Comparaison de clés partagées en temps constant

**Exemple** :
```csharp
var kyber = new KyberWrapper(KyberParameterSet.Kyber768);

// Décapsulation sécurisée
byte[] sharedSecret = kyber.DecapsulateSecure(ciphertext, privateKey);

// Comparaison sécurisée
bool match = KyberWrapper.ConstantTimeCompareSharedSecrets(secret1, secret2);
```

### DilithiumWrapper

**Méthodes sécurisées** :

- `VerifySecure(byte[] data, byte[] signature, byte[] publicKey, bool preHash)` : Vérification avec protection side-channel
- `ConstantTimeCompareSignatures(byte[] signature1, byte[] signature2)` : Comparaison de signatures en temps constant

**Exemple** :
```csharp
var dilithium = new DilithiumWrapper(DilithiumParameterSet.Dilithium3);

// Vérification sécurisée
bool isValid = dilithium.VerifySecure(data, signature, publicKey, false);

// Comparaison sécurisée
bool match = DilithiumWrapper.ConstantTimeCompareSignatures(sig1, sig2);
```

## Cmdlets PowerShell

### 1. `Invoke-KyberDecapsulateSecure`

Décapsule une clé partagée avec protection contre les attaques par canaux auxiliaires.

**Paramètres** :
- `-Ciphertext` : Ciphertext (byte[] ou hex)
- `-PrivateKey` : Clé privée (byte[] ou hex)
- `-ParameterSet` : Paramètre de sécurité (Kyber512, Kyber768, Kyber1024)

**Exemple** :
```powershell
$sharedSecret = Invoke-KyberDecapsulateSecure -Ciphertext $ciphertext -PrivateKey $privateKey -ParameterSet Kyber768
```

### 2. `Test-DilithiumSignatureSecure`

Vérifie une signature Dilithium avec protection contre les attaques par canaux auxiliaires.

**Paramètres** :
- `-Data` : Données originales (byte[] ou hex)
- `-Signature` : Signature à vérifier (byte[] ou hex)
- `-PublicKey` : Clé publique (byte[] ou hex)
- `-ParameterSet` : Paramètre de sécurité (Dilithium2, Dilithium3, Dilithium5)
- `-PreHash` : Pré-hacher avec SHA3 (optionnel)

**Exemple** :
```powershell
$isValid = Test-DilithiumSignatureSecure -Data $data -Signature $signature -PublicKey $publicKey -ParameterSet Dilithium3
```

### 3. `Test-ConstantTimeCompare`

Compare deux tableaux de bytes en temps constant.

**Paramètres** :
- `-Array1` : Premier tableau (byte[] ou hex)
- `-Array2` : Deuxième tableau (byte[] ou hex)

**Exemple** :
```powershell
$areEqual = Test-ConstantTimeCompare -Array1 $key1 -Array2 $key2
```

## Bonnes pratiques

### 1. Utiliser les méthodes sécurisées

**Recommandé** :
```csharp
// Décapsulation sécurisée
byte[] secret = kyber.DecapsulateSecure(ciphertext, privateKey);

// Vérification sécurisée
bool isValid = dilithium.VerifySecure(data, signature, publicKey);
```

**À éviter** :
```csharp
// Décapsulation non sécurisée (peut révéler des informations via timing)
byte[] secret = kyber.Decapsulate(ciphertext, privateKey);

// Comparaison non sécurisée (s'arrête à la première différence)
bool match = secret1 == secret2;  // ❌ DANGEREUX
```

### 2. Comparaisons de clés secrètes

**Recommandé** :
```csharp
// Comparaison en temps constant
bool match = ConstantTimeOperations.ConstantTimeEquals(key1, key2);
bool match = SideChannelProtection.SecureCompare(key1, key2);
```

**À éviter** :
```csharp
// Comparaison standard (révèle des informations via timing)
bool match = key1.SequenceEqual(key2);  // ❌ DANGEREUX
```

### 3. Opérations conditionnelles

**Recommandé** :
```csharp
// Sélection conditionnelle sans branche
byte result = ConstantTimeOperations.ConstantTimeSelect(condition, value1, value2);
```

**À éviter** :
```csharp
// Branche conditionnelle (révèle la condition via timing)
byte result = condition ? value1 : value2;  // ❌ DANGEREUX si condition est secrète
```

### 4. Nettoyage du cache

**Recommandé** :
```csharp
// Protéger une opération sensible
byte[] result = SideChannelProtection.ProtectTiming(() => SensitiveOperation());
```

## Limitations et considérations

### 1. Limitations du software

Les protections en software ne peuvent pas éliminer complètement toutes les fuites d'information :
- Le processeur peut toujours révéler des informations via le cache
- Les optimisations du compilateur peuvent introduire des fuites
- Les attaques matérielles (power analysis, EM emissions) nécessitent des protections matérielles

### 2. Performance

Les opérations à temps constant peuvent être légèrement plus lentes :
- Comparaisons parcourent toujours tous les éléments
- Nettoyage du cache ajoute une surcharge
- Accès mémoire uniformes peuvent être moins efficaces

**Recommandation** : Utiliser les méthodes sécurisées pour les opérations sensibles, les méthodes normales pour les opérations non sensibles.

### 3. Compilateur et optimisations

Les optimisations du compilateur peuvent introduire des fuites :
- Utilisation de `[MethodImpl(MethodImplOptions.NoOptimization)]` pour désactiver certaines optimisations
- Vérification avec des outils d'analyse statique

## Tests et validation

### Tests de timing

Pour valider les protections contre les timing attacks, mesurer le temps d'exécution avec différentes valeurs :

```csharp
// Test de comparaison en temps constant
var sw = Stopwatch.StartNew();
bool result = ConstantTimeOperations.ConstantTimeEquals(key1, key2);
sw.Stop();

// Le temps devrait être similaire indépendamment de la position de la différence
```

### Tests de cache

Pour valider les protections contre les cache side-channel attacks :
- Utiliser des outils de profiling du cache
- Mesurer les accès mémoire avec différents patterns
- Vérifier que les accès sont uniformes

## Références

- **NIST SP 800-90A** : Recommendation for Random Number Generation
- **NIST SP 800-57** : Recommendation for Key Management
- **FIPS 140-2** : Security Requirements for Cryptographic Modules
- **OWASP Cryptographic Storage Cheat Sheet** : Bonnes pratiques de sécurité cryptographique
- **Constant-Time Crypto** : https://www.bearssl.org/constanttime.html
- **Side-Channel Attacks** : https://en.wikipedia.org/wiki/Side-channel_attack

## Conclusion

Les contre-mesures implémentées dans KyberModule fournissent une protection significative contre les attaques par canaux auxiliaires courantes. Cependant, pour une sécurité maximale, il est recommandé de :

1. Utiliser les méthodes sécurisées pour les opérations sensibles
2. Combiner avec d'autres protections (gestion sécurisée des clés, zeroization)
3. Effectuer des audits de sécurité réguliers
4. Surveiller les nouvelles vulnérabilités et contre-mesures

