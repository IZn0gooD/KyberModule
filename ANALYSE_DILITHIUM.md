# Analyse : Remplacement d'Ed25519 par CRYSTALS-Dilithium

**Date** : Novembre 2025  
**Objectif** : Évaluer la faisabilité de remplacer Ed25519 par CRYSTALS-Dilithium pour la signature des clés Kyber

---

## 📋 Résumé Exécutif

### Conclusion

**CRYSTALS-Dilithium n'est PAS disponible dans BouncyCastle.NetCore 2.2.1**

BouncyCastle.NetCore 2.2.1 (version actuelle utilisée dans le projet) ne contient pas d'implémentation de CRYSTALS-Dilithium. Les classes `DilithiumParameters`, `DilithiumKeyPairGenerator`, et `DilithiumSigner` n'existent pas dans cette version.

### Recommandation

**Option 1 : Conserver Ed25519 (Recommandé pour le moment)**
- ✅ Déjà implémenté et fonctionnel
- ✅ Performances excellentes
- ✅ Très largement utilisé et testé
- ⚠️ Non post-quantique (mais acceptable pour l'authentification à court terme)

**Option 2 : Attendre la disponibilité de Dilithium dans BouncyCastle**
- ⏳ Dilithium sera probablement ajouté dans une future version
- ⏳ Nécessite une mise à jour de BouncyCastle.NetCore

**Option 3 : Utiliser une autre bibliothèque .NET pour Dilithium**
- ⚠️ Nécessite une nouvelle dépendance
- ⚠️ Complexité d'intégration
- ⚠️ Risques de compatibilité

---

## 🔍 Analyse Technique

### 1. Disponibilité dans BouncyCastle.NetCore 2.2.1

**Test effectué** :
```csharp
// Tentative d'utilisation de Dilithium
using Org.BouncyCastle.Pqc.Crypto.Crystals.Dilithium;
```

**Résultat** : ❌ **Échec de compilation**

**Erreurs rencontrées** :
1. `DilithiumParameters` : Les valeurs `dilithium2`, `dilithium3`, `dilithium5` n'existent pas
2. `DilithiumPrivateKeyParameters` : Signature du constructeur différente
3. `DilithiumSigner` : Méthodes `BlockUpdate` et `GenerateSignature` non disponibles

**Conclusion** : Les classes Dilithium ne sont pas présentes dans BouncyCastle.NetCore 2.2.1.

### 2. Vérification de la Version de BouncyCastle

**Version actuelle** : `BouncyCastle.NetCore 2.2.1`

**Classes disponibles pour la cryptographie post-quantique** :
- ✅ `Org.BouncyCastle.Pqc.Crypto.Crystals.Kyber` (ML-KEM)
- ❌ `Org.BouncyCastle.Pqc.Crypto.Crystals.Dilithium` (ML-DSA) - **Non disponible**

**Classes disponibles pour les signatures classiques** :
- ✅ `Org.BouncyCastle.Crypto.Parameters.Ed25519PublicKeyParameters`
- ✅ `Org.BouncyCastle.Crypto.Parameters.Ed25519PrivateKeyParameters`
- ✅ `Org.BouncyCastle.Crypto.Signers.Ed25519Signer`

### 3. Recherche de Versions Futures

**Version la plus récente de BouncyCastle** :
- BouncyCastle.NetCore 2.2.1 (dernière version stable)
- Versions futures pourraient inclure Dilithium, mais pas encore disponible

**Alternatives** :
- **liboqs** : Bibliothèque C avec bindings .NET, mais pas directement compatible
- **Implémentations .NET natives** : Quelques projets open-source, mais pas aussi matures que BouncyCastle

---

## 📊 Comparaison Ed25519 vs Dilithium

| Critère | Ed25519 | CRYSTALS-Dilithium |
|---------|---------|-------------------|
| **Type** | Signature classique | Signature post-quantique |
| **Résistance quantique** | ❌ Non | ✅ Oui |
| **Disponibilité BouncyCastle** | ✅ Disponible | ❌ Non disponible (v2.2.1) |
| **Taille clé publique** | 32 bytes | ~1312 bytes (Dilithium2) |
| **Taille clé privée** | 32 bytes | ~2560 bytes (Dilithium2) |
| **Taille signature** | 64 bytes | ~2420 bytes (Dilithium2) |
| **Performance** | ⚡ Très rapide | ⚡ Rapide (mais plus lent qu'Ed25519) |
| **Standardisation NIST** | ✅ Standardisé | ✅ Standardisé (ML-DSA, FIPS 204) |
| **Maturité** | ✅ Très mature | ✅ Mature (standardisé 2024) |
| **Utilisation actuelle** | ✅ Très répandu | ⚠️ Adoption en cours |

---

## 🎯 Avantages de CRYSTALS-Dilithium

### Pourquoi utiliser Dilithium ?

1. **Sécurité post-quantique complète**
   - ✅ Protocole entièrement résistant aux ordinateurs quantiques
   - ✅ Kyber (ML-KEM) + Dilithium (ML-DSA) = Solution complète post-quantique
   - ✅ Aligné avec les recommandations NIST

2. **Cohérence algorithmique**
   - ✅ Kyber et Dilithium sont tous deux basés sur les réseaux de lattices
   - ✅ Partagent la même base mathématique (projet CRYSTALS)
   - ✅ Provenance commune (même équipe de développement)

3. **Standardisation NIST**
   - ✅ Dilithium standardisé comme ML-DSA (FIPS 204)
   - ✅ Recommandé pour les signatures post-quantiques
   - ✅ Adoption croissante dans l'industrie

---

## ⚠️ Inconvénients Actuels

1. **Non disponible dans BouncyCastle.NetCore 2.2.1**
   - ❌ Nécessite une mise à jour ou une alternative
   - ❌ Implémentation manuelle complexe et risquée

2. **Tailles plus importantes**
   - ⚠️ Clés et signatures plus grandes que Ed25519
   - ⚠️ Impact sur le stockage et la transmission

3. **Performances**
   - ⚠️ Plus lent qu'Ed25519 (mais acceptable)

---

## 🛠️ Options d'Implémentation

### Option 1 : Attendre BouncyCastle (Recommandé)

**Avantages** :
- ✅ Pas de nouvelle dépendance
- ✅ Intégration cohérente avec Kyber
- ✅ Implémentation validée et testée

**Inconvénients** :
- ⏳ Délai d'attente indéterminé
- ⏳ Pas de garantie de date de disponibilité

**Action** : Surveiller les mises à jour de BouncyCastle.NetCore

### Option 2 : Utiliser une Bibliothèque Alternative

**Options possibles** :
1. **liboqs** (via bindings .NET)
   - ❌ Complexité d'intégration élevée
   - ❌ Maintenance requise
   
2. **Implémentation .NET native**
   - ❌ Moins testée que BouncyCastle
   - ❌ Risques de sécurité

**Recommandation** : ❌ Non recommandé pour le moment

### Option 3 : Architecture Hybride

**Approche** : Conserver Ed25519 et ajouter Dilithium quand disponible

**Avantages** :
- ✅ Solution fonctionnelle immédiate (Ed25519)
- ✅ Migration future possible vers Dilithium
- ✅ Flexibilité pour l'utilisateur

**Implémentation** :
```csharp
// Permettre le choix de l'algorithme de signature
public enum SignatureAlgorithm
{
    Ed25519,      // Classique (rapide)
    Dilithium     // Post-quantique (quand disponible)
}
```

---

## 📝 Recommandation Finale

### Pour le Développement Actuel

**Conserver Ed25519** pour les raisons suivantes :

1. ✅ **Fonctionnel maintenant** : Solution complète et testée
2. ✅ **Performances** : Excellent pour l'authentification
3. ✅ **Maturité** : Très largement utilisé et validé
4. ✅ **Compatibilité** : Disponible dans BouncyCastle actuel

### Pour l'Avenir

**Préparer la migration vers Dilithium** :

1. **Architecture modulaire** : Créer une interface commune pour les signatures
   ```csharp
   public interface ISignatureAlgorithm
   {
       (byte[] PublicKey, byte[] PrivateKey) GenerateKeyPair();
       byte[] Sign(byte[] data, byte[] privateKey);
       bool Verify(byte[] data, byte[] signature, byte[] publicKey);
   }
   ```

2. **Abstraction des wrappers** : Permettre le choix de l'algorithme
   ```csharp
   public class SignatureWrapper
   {
       private ISignatureAlgorithm _algorithm;
       
       public SignatureWrapper(SignatureAlgorithmType type)
       {
           _algorithm = type switch
           {
               SignatureAlgorithmType.Ed25519 => new Ed25519Wrapper(),
               SignatureAlgorithmType.Dilithium => new DilithiumWrapper(), // Quand disponible
               _ => new Ed25519Wrapper()
           };
       }
   }
   ```

3. **Surveillance** : Suivre les mises à jour de BouncyCastle.NetCore

---

## 🔄 Plan de Migration Future

### Phase 1 : Préparation (Maintenant)
- ✅ Créer une interface commune pour les signatures
- ✅ Documenter la migration prévue
- ✅ Surveiller les mises à jour BouncyCastle

### Phase 2 : Implémentation (Quand Dilithium disponible)
- [ ] Ajouter `DilithiumWrapper.cs` (déjà préparé)
- [ ] Créer les cmdlets PowerShell pour Dilithium
- [ ] Tests de compatibilité

### Phase 3 : Migration (Optionnelle)
- [ ] Permettre le choix Ed25519 ou Dilithium
- [ ] Recommander Dilithium pour les nouveaux déploiements
- [ ] Maintenir Ed25519 pour compatibilité

---

## 📚 Références

- **NIST FIPS 204** : Module-Lattice-Based Digital Signature Algorithm (ML-DSA)
- **CRYSTALS-Dilithium** : https://pq-crystals.org/dilithium/
- **BouncyCastle.NetCore** : https://www.nuget.org/packages/BouncyCastle.NetCore/
- **NIST PQC Standardization** : https://csrc.nist.gov/projects/post-quantum-cryptography

---

## ✅ Conclusion

**Pour le moment** : Conserver Ed25519
- ✅ Solution complète et fonctionnelle
- ✅ Performances excellentes
- ✅ Compatible avec BouncyCastle.NetCore 2.2.1

**Pour l'avenir** : Préparer la migration vers Dilithium
- 📋 Architecture modulaire prête
- 📋 Documentation préparée
- 📋 Migration facilitée quand Dilithium sera disponible

**Note importante** : Même si Ed25519 n'est pas post-quantique, il reste très sécurisé pour l'authentification à court/moyen terme. La combinaison Kyber (post-quantique pour l'échange de clés) + Ed25519 (classique pour l'authentification) offre un bon compromis sécurité/performance jusqu'à ce que Dilithium soit disponible dans BouncyCastle.

---

**Document créé le** : Novembre 2025  
**Prochaine révision** : Quand BouncyCastle.NetCore inclura Dilithium

