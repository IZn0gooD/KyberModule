# 🔨 Guide de Compilation - KyberModule

Ce guide explique comment reconstruire et compiler le projet KyberModule depuis zéro après avoir cloné le dépôt GitHub.

## 📋 Prérequis

### 1. .NET SDK

Le projet nécessite le **.NET SDK 8.0** ou supérieur.

**Windows :**
- Télécharger depuis : https://dotnet.microsoft.com/download
- Installer le SDK (pas seulement le runtime)
- Vérifier l'installation :
  ```powershell
  dotnet --version
  # Doit afficher 8.0.x ou supérieur
  ```

**Linux :**
```bash
# Ubuntu/Debian
wget https://dot.net/v1/dotnet-install.sh
chmod +x dotnet-install.sh
./dotnet-install.sh --channel 8.0

# Ajouter au PATH
export PATH="$HOME/.dotnet:$PATH"
```

### 2. PowerShell (pour les scripts de build)

**Windows :** PowerShell 5.1+ ou PowerShell Core 7+

**Linux :** PowerShell Core 7+
```bash
# Installation PowerShell Core sur Linux
curl -sSL https://aka.ms/install-powershell.sh | sudo bash
```

---

## 🚀 Compilation Rapide

### Méthode 1 : Script PowerShell (Recommandé)

```powershell
# Depuis la racine du projet
cd C:\Users\mlopoukhine\VSCODE\KyberModule

# Compiler le module PowerShell
.\BUILD.ps1 -Configuration Release
```

Ce script :
1. Compile `KyberLibrary`
2. Compile `KyberModule`
3. Copie automatiquement les DLLs nécessaires dans `KyberModule/`
4. Copie `BouncyCastle.Cryptography.dll` depuis le cache NuGet

### Méthode 2 : dotnet CLI (Manuel)

```powershell
# 1. Restaurer les dépendances NuGet
dotnet restore

# 2. Compiler tous les projets
dotnet build -c Release

# 3. Pour le module PowerShell, copier les DLLs manuellement
cd KyberModule
Copy-Item ".\bin\Release\netstandard2.0\KyberModule.dll" -Destination "." -Force
Copy-Item "..\KyberLibrary\bin\Release\netstandard2.0\KyberLibrary.dll" -Destination "." -Force
Copy-Item "..\KyberDomain\bin\Release\netstandard2.0\KyberDomain.dll" -Destination "." -Force

# 4. Copier BouncyCastle depuis le cache NuGet
$bcDll = Get-ChildItem "$env:USERPROFILE\.nuget\packages\bouncycastle.cryptography\2.6.2\lib\netstandard2.0\BouncyCastle.Cryptography.dll"
Copy-Item $bcDll.FullName -Destination ".\BouncyCastle.Cryptography.dll" -Force
```

---

## 📦 Compilation des Applications (KyberDaemon & KyberCLI)

### Compilation avec scripts de déploiement

#### Windows

```powershell
cd deploy
.\build-windows-package.ps1 -OutputDirectory dist\windows -Runtime win-x64
```

Ce script :
- Compile tous les projets (.NET 8.0)
- Crée des binaires **self-contained** (contiennent le runtime .NET)
- Génère l'archive `dist\windows\KyberModule-win-x64.zip`
- Inclut les fichiers de configuration (`auth.conf`, `kyberd.conf`)

#### Linux

```bash
cd deploy
./build-linux-package.sh
```

### Compilation manuelle

#### KyberDaemon (Service)

```powershell
cd KyberDaemon
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

#### KyberCLI (Client)

```powershell
cd KyberCLI
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

---

## 🧪 Compilation des Tests

### Tests d'intégration

```powershell
cd tests\KyberIntegrationTests
dotnet build -c Release
dotnet test -c Release
```

### Benchmarks

```powershell
cd tests\KyberBenchmarks
dotnet build -c Release
dotnet run -c Release
```

---

## 📁 Structure des Fichiers de Projet

Tous les fichiers `.csproj` sont inclus dans le dépôt :

- `KyberDomain/KyberDomain.csproj` - Domaine métier (.NET Standard 2.0)
- `KyberLibrary/KyberLibrary.csproj` - Infrastructure .NET (.NET Standard 2.0)
- `KyberModule/KyberModule.csproj` - Module PowerShell (.NET Standard 2.0)
- `KyberShared/KyberShared.csproj` - Protocole réseau (.NET 8.0)
- `KyberKeyManagement/KyberKeyManagement.csproj` - Gestion des clés (.NET Standard 2.0)
- `KyberDaemon/KyberDaemon.csproj` - Service daemon (.NET 8.0)
- `KyberCLI/KyberCLI.csproj` - Client CLI (.NET 8.0)
- `tests/KyberIntegrationTests/KyberIntegrationTests.csproj` - Tests (.NET 8.0)
- `tests/KyberBenchmarks/KyberBenchmarks.csproj` - Benchmarks (.NET 8.0)

---

## 🔄 Restauration des Dépendances NuGet

Lors du premier clonage, restaurer les packages NuGet :

```powershell
# Depuis la racine du projet
dotnet restore

# Ou pour un projet spécifique
cd KyberModule
dotnet restore
```

Les dépendances principales sont :
- `BouncyCastle.Cryptography` (2.6.2+) - ML-KEM et ML-DSA
- `Microsoft.Extensions.*` - Pour KyberDaemon (Hosting, Logging, etc.)
- `xunit` - Pour les tests
- `BenchmarkDotNet` - Pour les benchmarks

---

## ✅ Vérification de la Compilation

### Module PowerShell

```powershell
cd KyberModule
Import-Module .\KyberModule.psd1 -Force
Get-Command -Module KyberModule | Select-Object Name
```

### KyberDaemon

```powershell
cd KyberDaemon\publish\win-x64
.\KyberDaemon.exe --help
```

### KyberCLI

```powershell
cd KyberCLI\publish\win-x64
.\KyberCLI.exe --help
```

---

## 🐛 Dépannage

### Erreur : "dotnet n'est pas reconnu"

**Solution :**
1. Vérifier que .NET SDK est installé : https://dotnet.microsoft.com/download
2. Redémarrer PowerShell après l'installation
3. Vérifier le PATH : `$env:PATH` doit contenir le chemin vers `dotnet.exe`

### Erreur : "BouncyCastle.Cryptography.dll introuvable"

**Solution :**
```powershell
# Restaurer les packages NuGet
dotnet restore

# Vérifier que le package est dans le cache
Get-ChildItem "$env:USERPROFILE\.nuget\packages\bouncycastle.cryptography" -Recurse -Filter "*.dll"
```

### Erreur : "Le projet ne peut pas être chargé"

**Solution :**
```powershell
# Nettoyer et restaurer
dotnet clean
dotnet restore
dotnet build
```

### Erreur de compilation : "CS0246: Le nom de type est introuvable"

**Solution :**
- Vérifier que tous les projets sont compilés dans le bon ordre
- Utiliser `dotnet build` depuis la racine pour compiler tous les projets
- Ou utiliser le script `BUILD.ps1` qui gère l'ordre de compilation

---

## 📝 Notes Importantes

1. **Ordre de compilation** : Les projets ont des dépendances entre eux :
   - `KyberDomain` (aucune dépendance)
   - `KyberLibrary` (dépend de `KyberDomain`)
   - `KyberModule` (dépend de `KyberLibrary` et `KyberDomain`)
   - `KyberShared` (dépend de `KyberDomain`)
   - `KyberDaemon` et `KyberCLI` (dépendent de `KyberShared`, `KyberDomain`, etc.)

2. **Fichiers générés** : Les dossiers `bin/` et `obj/` sont exclus par `.gitignore` car ils sont régénérés à chaque compilation.

3. **Self-contained vs Framework-dependent** :
   - **Self-contained** : Binaires incluent le runtime .NET (plus lourd, ~100 MB)
   - **Framework-dependent** : Nécessite .NET installé sur la machine (plus léger, ~5 MB)

4. **Configuration Release vs Debug** :
   - `Release` : Optimisé, sans symboles de débogage (production)
   - `Debug` : Avec symboles, optimisations désactivées (développement)

---

## 🔗 Ressources

- Documentation .NET : https://docs.microsoft.com/dotnet/
- Guide de publication : https://docs.microsoft.com/dotnet/core/deploying/
- BouncyCastle : https://www.bouncycastle.org/

---

*Dernière mise à jour : 2025-01-13*

