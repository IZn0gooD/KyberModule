# 🚀 Guide de démarrage rapide - KyberModule

## Installation du module en 3 étapes

### 1. Compiler le module
```powershell
cd KyberModule\KyberModule
# La solution est désormais découpée selon un modèle DDD :
# - KyberDomain (interfaces métier)
# - KyberLibrary (adapters BouncyCastle)
# - KyberShared (protocole réseau)
dotnet build -c Release
```

### 2. Copier les DLLs nécessaires
```powershell
Copy-Item ".\bin\Release\netstandard2.0\KyberModule.dll" -Destination "." -Force
Copy-Item "..\KyberLibrary\bin\Release\netstandard2.0\KyberLibrary.dll" -Destination "." -Force
Copy-Item "..\KyberDomain\bin\Release\netstandard2.0\KyberDomain.dll" -Destination "." -Force
$bcDll = Get-ChildItem "$env:USERPROFILE\.nuget\packages\bouncycastle.cryptography\2.6.2\lib\netstandard2.0\BouncyCastle.Cryptography.dll"
Copy-Item $bcDll.FullName -Destination ".\BouncyCastle.Cryptography.dll" -Force
```

### 3. Importer le module
```powershell
Import-Module .\KyberModule.psd1 -Force
```

> ℹ️ Les cmdlets réseau historiques (`Start/Connect/Send/Receive-Secure*`) sont conservées mais obsolètes. Utilisez KyberDaemon/KyberCLI pour les communications.

## Déploiement de KyberDaemon (service)

### Windows (Service SCM)
```powershell
# Archive générée via deploy/build-windows-package.ps1
Expand-Archive KyberModule-win-x64.zip C:\opt\KyberModule -Force
cd C:\opt\KyberModule\KyberDaemon\deploy
# nécessite PowerShell Administrateur
./install-kyberd-service.ps1 -Runtime win-x64
Start-Service KyberDaemon
```
- Les fichiers de config sont copiés dans `C:\ProgramData\KyberDaemon\` (`kyberd.conf`, `auth.conf`, dossier `keys`).
- Pour modifier la config : éditez `kyberd.conf` puis redémarrez le service (`Restart-Service KyberDaemon`).
- Sécurité des clés : `key_encryption_mode=dpapi` par défaut sur Windows. Pour un stockage chiffré portable, définir `key_encryption_mode=passphrase` et `key_encryption_passphrase`. La rotation automatique se règle via `key_rotation_days` (0 = désactivé).
- Reverse proxy : fixer `listen_address = 127.0.0.1` ou activer `strict_network_mode = true` et déléguer TLS (nginx/haproxy). Utiliser `KyberCLI --tls ...` côté client (voir README).
- Administration rapide : `KyberDaemon\tools\Invoke-KyberKeyRotation.ps1 -Action Rotate -Config C:\ProgramData\KyberDaemon\kyberd.conf`

### Linux (systemd)
```bash
# Archive générée via deploy/build-linux-package.sh
sudo tar -xzf KyberModule-linux-x64.tar.gz -C /opt/kybermodule
sudo cp /opt/kybermodule/KyberDaemon/deploy/kyberd.service /etc/systemd/system/
sudo systemctl daemon-reload
sudo systemctl enable --now kyberd
```
- Configuration : `/etc/kyberd/kyberd.conf`, authentification : `/etc/kyberd/auth.conf`.
- Journalisation : fichiers JSON `kyberd.log` dans `log_directory` avec rotation (`log_max_size_mb`, `log_max_files`).
- Sécurité des clés : Linux/macOS requièrent `key_encryption_mode=passphrase` + `key_encryption_passphrase` pour chiffrer les clés stockées. Adapter `key_rotation_days` selon la politique de rotation souhaitée. Le package génère automatiquement une passphrase aléatoire lors du build.
- Reverse proxy : laisser `listen_address = 127.0.0.1`, ou poser `strict_network_mode = true`, configurer un frontal TLS (nginx stream / HAProxy) avec HSTS et utiliser `KyberCLI --tls` côté client.
- Quotas : `max_connections` (global) et `max_sessions_per_user` pour limiter les sessions simultanées.

### Rotation / Administration des clés

```powershell
# Lister les versions de clé (Depuis la machine hébergeant le daemon)
KyberCLI keys list --config C:\ProgramData\KyberDaemon\kyberd.conf

# Rotation forcée (nouvelle version session.vN.*)
KyberCLI keys rotate --name session --config C:\ProgramData\KyberDaemon\kyberd.conf

# Export des métadonnées vers un fichier
KyberCLI keys export --config /etc/kyberd/kyberd.conf --output /tmp/metadata.json
```

> Pour PowerShell/Windows, le script `KyberDaemon/tools/Invoke-KyberKeyRotation.ps1` encapsule ces commandes (`-Action List|Rotate|Export`).

> 📦 Besoin d'une installation prête à l'emploi ? Utilisez `deploy/build-windows-package.ps1` ou `deploy/build-linux-package.sh`. Le script global `scripts/build-all.ps1` (cf. section Installation) automatise la compilation de **tous** les projets (KyberDomain, KyberLibrary, KyberShared, KyberDaemon, KyberCLI).

## Utilisation rapide de KyberCLI

```powershell
cd C:\opt\KyberModule\KyberCLI\publish\win-x64
# Mot de passe SHA3-256 pré-haché en hex (0x… accepté)
./KyberCLI.exe connect --host 127.0.0.1 --user alice --password 0xF1A2... --command "hostname"

# Session interactive (PowerShell distant + auto-complétion locale sur Tab)
./KyberCLI.exe connect --host 127.0.0.1 --user alice --password 0xF1A2...

# Connexion via reverse proxy TLS (ex. proxy.example.com:443)
./KyberCLI.exe connect --host proxy.example.com --port 443 --user alice \
    --password "ChangeMe!123" --password-record "argon2id$v=19$m=65536,t=3,p=2$..." \
    --tls --tls-cert-fingerprint "sha256:ABCD1234..." --tls-server-name proxy.example.com
```

> Empreinte TLS : `openssl x509 -in certificat.pem -noout -fingerprint -sha256`

Modes interactifs :
- `Tab` → auto-complétion enrichie (`Get-Command`, exemples prêts à l'emploi)
- Historique persisté dans `%APPDATA%\KyberCLI\history.log` (rejoué au démarrage)
- Directives locales : `:help`, `:examples`, `:run <alias>`, `:history`, `:validate <commande>`, `:aliases`, `:alias <nom> <commande>`, `:unalias <nom>`, `:clear`, `exit`
- Alias utilisateurs sauvegardés dans `%APPDATA%\KyberCLI\aliases.conf`
- Validation à la volée : un avertissement s'affiche si la commande saisie est inconnue côté serveur (confirmation requise)
- Mot de passe Argon2 : fournir `--password <secret>` + `--password-record <enregistrement>` (identique à l'entrée `auth.conf`)
- Kerberos : fournir un ticket AP-REQ encodé Base64 via `--kerberos-token` (keytab côté serveur requis)
- OAuth : fournir un jeton JWT via `--oauth-token` (JWKS/issuer/audience configurés côté serveur)

Générer une entrée `auth.conf` Argon2 :
```powershell
dotnet KyberCLI.dll passhash --username alice --password "ChangeMe!123"
```
```text
alice:password:argon2id$v=19$m=65536,t=3,p=2$pad2ZtQRdzTOr2zecPUMng==$wnzKuHLwtQSOloqyRr5XMN671g6c18w2UbIaPOu9Zkw=
```
Coller la ligne dans `KyberDaemon/config/auth.conf` côté serveur.

Depuis PowerShell :
```powershell
Invoke-SecureCommand \
    -ServerHost 127.0.0.1 \
    -Port 8443 \
    -Username alice \
    -PasswordHex 0xF1A2... \
    -Command "Get-Process | Select-Object -First 5 Name,Id"
```
Paramètres : `-KeyPath` (clé privée Dilithium), `-CliPath` (binaire KyberCLI), `-RebuildCli` (force `dotnet publish`).

## Ressources complémentaires
- `README.md` : documentation complète et scénarios détaillés
- `PROTOCOLE_COMMUNICATION_SECURISEE.md` : description du protocole KyberDaemon/KyberCLI
- `RECHERCHE_ET_DEVELOPPEMENT.md` : historique et choix techniques du projet


## Génération rapide des clés (cmdlet New-KyberKeyBundle)

```powershell
# Tout-en-un : clés client + serveur, mise à jour auth.conf et rotation Kyber
Import-Module "C:\Program Files\KyberModule\KyberModule\KyberModule.psd1" -Force
New-KyberKeyBundle -Target Both -Username alice -IncludeKyber -Force -Verbose
Restart-Service KyberDaemon
```

Résultats :
- Fichiers Base64 écrits sous `%USERPROFILE%\KyberClient\keys` (ou `-ClientDestination`)
- `auth.conf` enrichi automatiquement (sauf `-SkipAuthUpdate`)
- Rotation Kyber appelée via `KyberCLI keys rotate` (désactivable avec `-SkipKyberRotation`)

Pour analyser le handshake côté client :

```powershell
.\KyberCLI.exe connect --host serveur --user alice --key C:\secrets\alice.prv --trace-level full
```
