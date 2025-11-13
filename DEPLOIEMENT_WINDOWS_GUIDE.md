# Guide rapide de déploiement Windows (KyberModule)

Ce document décrit comment :
1. Compiler l’ensemble du projet en archive (`KyberModule-win-x64.zip`).
2. Déployer l’archive sur un serveur et un client Windows (structure `C:\opt`).
3. Générer/configurer automatiquement les clés Dilithium/Kyber.
4. Établir une session persistante entre `KyberDaemon` et `KyberCLI`.

> Toutes les commandes suivantes sont à exécuter dans une console PowerShell **élevée** (mode administrateur) sur les machines concernées.

---

## 1. Compilation de l’archive de déploiement

Sur la machine de build (où se trouve le dépôt `KyberModule`) :

```powershell
cd C:\Users\mlopoukhine\VSCODE\KyberModule

# Nettoyage + build du package Windows (runtime win-x64)
.\deploy\build-windows-package.ps1 -OutputDirectory dist\windows -Runtime win-x64
```

Résultat : `dist\windows\KyberModule-win-x64.zip`

Copiez ce fichier sur les machines serveur **et** client dans `C:\opt` (par exemple via SMB ou WinSCP).

---

## 2. Déploiement sur le serveur (`C:\opt`)

Sur la machine serveur, exécutez :

```powershell
cd C:\opt

$zip  = "C:\opt\KyberModule-win-x64.zip"
$dest = "C:\opt\KyberModule"

Remove-Item -Recurse -Force $dest -ErrorAction SilentlyContinue
Expand-Archive -Path $zip -DestinationPath $dest -Force
Remove-Item -Recurse -Force C:\ProgramData\KyberDaemon -ErrorAction SilentlyContinue

cd .\KyberModule\KyberDaemon\deploy
.\install-kyberd-service.ps1 -Runtime win-x64

Restart-Service KyberDaemon
```

> Ce script copie les binaires, crée le service Windows `KyberDaemon` et l’associe à la configuration par défaut (`C:\ProgramData\KyberDaemon`).

---

## 3. Déploiement côté client (`C:\opt`)

Utilisez la même archive :

```powershell
cd C:\opt

$zip  = "C:\opt\KyberModule-win-x64.zip"
$dest = "C:\opt\KyberModule"

Remove-Item -Recurse -Force $dest -ErrorAction SilentlyContinue
Expand-Archive -Path $zip -DestinationPath $dest -Force
```

Le binaire client se trouve ensuite dans `C:\opt\KyberModule\KyberCLI\publish\win-x64`.

---

## 4. Génération et configuration des clés (serveur + client)

Grâce à la nouvelle cmdlet `New-KyberKeyBundle`, on peut automatiser la préparation des clés Dilithium/Kyber et la mise à jour d’`auth.conf`.

### 4.1. Sur le serveur

```powershell
Import-Module "C:\Program Files\KyberModule\KyberModule\KyberModule.psd1" -Force

New-KyberKeyBundle -Target Both -Username alice -IncludeKyber -Force -Verbose
Restart-Service KyberDaemon
```

- Les clés serveur sont écrites dans `C:\ProgramData\KyberDaemon\keys\dilithium` (chemin absolu configuré par défaut dans `kyberd.conf`).
- Les clés client sont générées dans `%USERPROFILE%\KyberClient\keys` (ou `-ClientDestination`).
- L’entrée `alice:key:<Base64>` est ajoutée/à jour dans `C:\ProgramData\KyberDaemon\auth.conf` (également référencé en absolu).
- La cmdlet déclenche également `KyberCLI.exe keys rotate` (rotation du jeu de clés Kyber serveur). Utilisez `-SkipKyberRotation` si vous voulez l’ignorer.

> Vérifiez la sortie de la cmdlet : elle liste les fichiers créés (`CreatedFiles`) et fournit l’entrée ajoutée dans `auth.conf` (`AuthEntry`).

### 4.2. Sur un poste client uniquement

Si vous préférez générer les clés localement :

```powershell
Import-Module "C:\Program Files\KyberModule\KyberModule\KyberModule.psd1" -Force
New-KyberKeyBundle -Target Client -Username alice -ClientDestination "C:\opt\KyberClient\keys" -IncludeKyber -Force
```

La clé publique (`alice.pub`) doit alors être copiée manuellement dans `C:\ProgramData\KyberDaemon\auth.conf` sur le serveur si l’on n’utilise pas `-Target Both`.

---

## 5. Vérification et diagnostics

Après génération :

```powershell
Get-Content C:\ProgramData\KyberDaemon\auth.conf
Get-ChildItem C:\ProgramData\KyberDaemon\keys -Recurse
Get-Content C:\ProgramData\KyberDaemon\logs\kyberd.log -Tail 30
```

Assurez-vous que :
- Le service écoute sur `0.0.0.0:8443` (`Test-NetConnection 127.0.0.1 -Port 8443`).
- `auth.conf` comporte l’entrée de l’utilisateur souhaité (`alice`).

---

## 6. Établissement d’une session persistante

Sur le client :

```powershell
cd C:\opt\KyberModule\KyberCLI\publish\win-x64
.\KyberCLI.exe connect `
    --host 192.168.10.10 `
    --port 8443 `
    --user alice `
    --key "C:\opt\KyberClient\keys\alice.prv"
```

Ne pas spécifier `--command` pour rester en mode interactif. Tapez `exit` pour fermer la session.

> Besoin de suivre le handshake pas à pas ? Ajoutez `--trace-level full` à la commande ci-dessus.

Pour un accès via mot de passe (Argon2id) : 

```powershell
KyberCLI.exe passhash --username alice --password "ChangeMe!123"
# Copier la ligne produite dans auth.conf, puis :
KyberCLI.exe connect --host 192.168.10.10 --port 8443 --user alice --password "ChangeMe!123" --password-record "argon2id$..."
```

---

## 7. Déploiement ultérieur via `deploy-windows-package.ps1` (optionnel)

Le script `deploy\deploy-windows-package.ps1` peut également automatiser la mise à jour d’un serveur (`-PackageType server`) ou préparer un environnement client (`-PackageType client`). Il suffit de pointer sur la nouvelle archive :

```powershell
cd C:\opt\KyberModule\deploy

# Installation / mise à jour du serveur (service Windows)
.\deploy-windows-package.ps1 -PackagePath C:\opt\KyberModule-win-x64.zip -PackageType server -InstallRoot "C:\Program Files\KyberModule"

# Préparation d’un poste client sans service
.\deploy-windows-package.ps1 -PackagePath C:\opt\KyberModule-win-x64.zip -PackageType client -InstallRoot "C:\Program Files\KyberModuleClient"
```

Ce script effectue la décompression, sauvegarde/restaure les fichiers `*.conf` existants, installe le service (mode serveur) et nettoie les fichiers temporaires.

---

## 8. Résumé

1. **Compiler** l’archive `KyberModule-win-x64.zip` avec `deploy\build-windows-package.ps1`.
2. **Déployer** l’archive sur serveur et client (`C:\opt`).
3. **Installer** le service (`install-kyberd-service.ps1` ou `deploy-windows-package.ps1`).
4. **Générer les clés** automatiquement via `New-KyberKeyBundle` (ou manuellement selon les besoins).
5. **Vérifier** `auth.conf`, les logs et l’écoute sur le port 8443.
6. **Établir** la session persistante avec `KyberCLI.exe connect`.

En suivant ces étapes, le serveur KyberDaemon et le client KyberCLI sont prêts à opérer en mode post-quantique avec un minimum de manipulations répétitives.
