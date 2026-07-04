# Migration vers BouncyCastle.Cryptography 2.5.1

**Date** : Novembre 2025  
**Objectif** : Mettre à jour BouncyCastle.NetCore 2.2.1 vers BouncyCastle.Cryptography 2.5.1 pour bénéficier du support de Dilithium

---

## 📋 Résumé

### Situation Actuelle

- **Package actuel** : `BouncyCastle.NetCore 2.2.1`
- **Support Kyber** : ✅ Disponible via `Org.BouncyCastle.Pqc.Crypto.Crystals.Kyber`
- **Support Dilithium** : ❌ Non disponible

### Package Cible

- **Package cible** : `BouncyCastle.Cryptography 2.5.1` (ou 2.6.2 plus récent)
- **Support Kyber/ML-KEM** : ✅ Disponible via `Org.BouncyCastle.Crypto.Parameters.MLKem*`
- **Support Dilithium/ML-DSA** : ✅ Disponible via `Org.BouncyCastle.Pqc.Crypto.Crystals.Dilithium`

---

## ⚠️ Problèmes Identifiés

### 1. Changement de Package

**BouncyCastle.NetCore** et **BouncyCastle.Cryptography** sont deux packages différents :
- `BouncyCastle.NetCore` : Package spécifique pour .NET Core (version 2.2.1 est la dernière)
- `BouncyCastle.Cryptography` : Package officiel principal (versions 2.5.1, 2.6.2, etc.)

### 2. Changement de Namespace pour Kyber

**Ancien namespace** (BouncyCastle.NetCore 2.2.1) :
```csharp
using Org.BouncyCastle.Pqc.Crypto.Crystals.Kyber;
```

**Nouveau namespace** (BouncyCastle.Cryptography 2.5.1) :
```csharp
using Org.BouncyCastle.Crypto.Parameters; // Pour MLKemKeyGenerationParameters
// Les classes ML-KEM sont dans Crypto.Parameters
```

### 3. Structure Différente

Les classes Kyber dans BouncyCastle.Cryptography utilisent une structure différente :
- Ancien : `KyberParameters`, `KyberKeyPairGenerator`, etc.
- Nouveau : `MLKemKeyGenerationParameters`, `MLKemParameters`, etc.

---

## 🔄 Plan de Migration

### Option 1 : Utiliser les Deux Packages (Temporaire)

**Avantages** :
- ✅ Ne casse pas le code existant
- ✅ Permet d'ajouter Dilithium progressivement

**Inconvénients** :
- ⚠️ Conflits potentiels entre les deux packages
- ⚠️ Taille de l'application augmentée
- ⚠️ Maintenance complexe

### Option 2 : Migration Complète (Recommandé)

**Avantages** :
- ✅ Un seul package à maintenir
- ✅ Support de Dilithium
- ✅ Versions plus récentes disponibles (2.6.2)

**Inconvénients** :
- ⚠️ Nécessite de réécrire le code Kyber
- ⚠️ Tests complets requis

---

## 📝 Décision

**Recommandation** : **Conserver BouncyCastle.NetCore 2.2.1 pour le moment**

**Raisons** :
1. ✅ Code Kyber fonctionne parfaitement
2. ✅ Pas de breaking changes
3. ✅ Stabilité garantie
4. ⚠️ Migration vers BouncyCastle.Cryptography nécessite une refactorisation importante

**Pour Dilithium** :
- Attendre que BouncyCastle.NetCore soit mis à jour avec Dilithium
- OU migrer complètement vers BouncyCastle.Cryptography (projet plus important)

---

## 🛠️ Si Migration vers BouncyCastle.Cryptography

### Étapes de Migration

1. **Remplacer le package** :
   ```xml
   <PackageReference Include="BouncyCastle.Cryptography" Version="2.6.2" />
   ```

2. **Adapter KyberWrapper.cs** :
   - Changer les namespaces
   - Adapter les classes (MLKem* au lieu de Kyber*)
   - Tester chaque méthode

3. **Tester complètement** :
   - Génération de clés
   - Encapsulation/Décapsulation
   - Sérialisation/Désérialisation

4. **Ajouter Dilithium** :
   - Créer DilithiumWrapper.cs
   - Créer les cmdlets PowerShell

---

## 📚 Références

- **BouncyCastle.Cryptography** : https://www.nuget.org/packages/BouncyCastle.Cryptography/
- **BouncyCastle.NetCore** : https://www.nuget.org/packages/BouncyCastle.NetCore/
- **Release Notes 2.5.0** : Support ML-KEM et ML-DSA
- **Release Notes 2.6.1** : Améliorations PQC

---

**Conclusion** : La migration vers BouncyCastle.Cryptography est possible mais nécessite une refactorisation importante du code Kyber. Pour le moment, conserver BouncyCastle.NetCore 2.2.1 est la meilleure option.

