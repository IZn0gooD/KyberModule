param(
    [Parameter(Mandatory = $true)]
    [string]$PackagePath,

    [string]$InstallRoot = "C:\Program Files\KyberModule",

    [ValidateSet("win-x64", "win-x86", "win-arm64")]
    [string]$Runtime = "win-x64",

    [ValidateSet("server", "client")]
    [string]$PackageType = "server",

    [string]$ServiceName = "KyberDaemon",

    [switch]$NoStart,

    [switch]$UninstallOnly
)

<#
.SYNOPSIS
    Déploie une archive KyberModule (serveur ou client) à partir du package compilé.

.DESCRIPTION
    - Décompresse l’archive vers $InstallRoot
    - Sauvegarde/restaure les fichiers .conf existants
    - Mode serveur : publie KyberDaemon, met à jour kyberd.conf/auth.conf, crée le service Windows
    - Mode client  : copie simplement les binaires sans créer de service

.PARAMETER PackagePath
    Chemin vers l’archive générée (ex. C:\opt\KyberModule-win-x64.zip)

.PARAMETER InstallRoot
    Dossier d’installation cible (par défaut C:\Program Files\KyberModule)

.PARAMETER Runtime
    Runtime .NET ciblé (win-x64, win-x86, win-arm64)

.PARAMETER PackageType
    server -> installe le service KyberDaemon
    client -> copie uniquement les binaires (pas de service)

.PARAMETER ServiceName
    Nom du service Windows à créer (par défaut KyberDaemon)

.PARAMETER NoStart
    Ne pas démarrer le service après installation (mode serveur)

.PARAMETER UninstallOnly
    Supprime le service/dossier d’installation sans recopier le package

.EXAMPLE
    .\deploy-windows-package.ps1 -PackagePath C:\opt\KyberModule-win-x64.zip -PackageType server

.EXAMPLE
    .\deploy-windows-package.ps1 -PackagePath C:\opt\KyberModule-win-x64.zip -PackageType client -InstallRoot "C:\Program Files\KyberModuleClient"

.NOTES
    À exécuter dans une console PowerShell administrateur.
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Assert-Administrator {
    $currentIdentity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($currentIdentity)
    if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
        throw "Ce script doit être exécuté avec des privilèges administrateur."
    }
}

function Stop-And-DeleteService {
    param([string]$Name)

    $svc = Get-Service -Name $Name -ErrorAction SilentlyContinue
    if ($null -eq $svc) {
        return
    }

    if ($svc.Status -ne 'Stopped') {
        Write-Host "Arrêt du service $Name" -ForegroundColor Yellow
        Stop-Service -Name $Name -Force -ErrorAction SilentlyContinue
        $svc.WaitForStatus('Stopped', [TimeSpan]::FromSeconds(20))
    }

    Write-Host "Suppression du service $Name" -ForegroundColor Yellow
    sc.exe delete $Name | Out-Null
    Start-Sleep -Seconds 1
}

Assert-Administrator

if (-not (Test-Path $PackagePath)) {
    throw "Package introuvable : $PackagePath"
}

$packageFullPath = (Resolve-Path $PackagePath).Path
$tmpRoot = Join-Path ([IO.Path]::GetTempPath()) ("KyberDeploy_" + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $tmpRoot | Out-Null

Write-Host "Décompression du package dans $tmpRoot" -ForegroundColor Cyan
Expand-Archive -Path $packageFullPath -DestinationPath $tmpRoot

$backupConfigDir = Join-Path $tmpRoot "config-backup"
$existingConfigDir = Join-Path $InstallRoot "KyberDaemon\config"
if (Test-Path $existingConfigDir) {
    Write-Host "Sauvegarde de l'ancienne configuration" -ForegroundColor Cyan
    New-Item -ItemType Directory -Path $backupConfigDir | Out-Null
    Get-ChildItem $existingConfigDir -Filter *.conf -ErrorAction SilentlyContinue | ForEach-Object {
        Copy-Item $_.FullName -Destination (Join-Path $backupConfigDir $_.Name) -Force
    }
}

Write-Host "Arrêt et suppression de l'ancien service (si présent)" -ForegroundColor Cyan
Stop-And-DeleteService -Name $ServiceName

if (Test-Path $InstallRoot) {
    Write-Host "Suppression de l'ancienne installation dans $InstallRoot" -ForegroundColor Cyan
    Remove-Item -Path $InstallRoot -Recurse -Force
}

Write-Host "Copie des nouveaux fichiers dans $InstallRoot" -ForegroundColor Cyan
New-Item -ItemType Directory -Path $InstallRoot | Out-Null
Get-ChildItem $tmpRoot | ForEach-Object {
    Copy-Item $_.FullName -Destination (Join-Path $InstallRoot $_.Name) -Recurse -Force
}

if (Test-Path $backupConfigDir) {
    Write-Host "Restauration de la configuration" -ForegroundColor Cyan
    $targetConfigDir = Join-Path $InstallRoot "KyberDaemon\config"
    foreach ($file in Get-ChildItem $backupConfigDir -Filter *.conf) {
        Copy-Item $file.FullName -Destination (Join-Path $targetConfigDir $file.Name) -Force
    }
}

$installScript = Join-Path $InstallRoot "KyberDaemon\deploy\install-kyberd-service.ps1"
if (-not (Test-Path $installScript)) {
    throw "Script d'installation du service introuvable: $installScript"
}

if ($UninstallOnly) {
    Write-Host "Suppression de l'installation existante (mode désinstallation)" -ForegroundColor Yellow
    Stop-And-DeleteService -Name $ServiceName
    if (Test-Path $InstallRoot) {
        Write-Host "Suppression du dossier $InstallRoot" -ForegroundColor Cyan
        Remove-Item -Path $InstallRoot -Recurse -Force -ErrorAction SilentlyContinue
    }
    Write-Host "Désinstallation terminée" -ForegroundColor Green
    return
}

if ($PackageType -eq "server") {
    Write-Host "Installation / mise à jour du service Windows" -ForegroundColor Cyan

    $daemonRoot    = Join-Path $InstallRoot "KyberDaemon"
    $deployFolder  = Join-Path $daemonRoot   "deploy"
    $publishFolder = Join-Path $daemonRoot   "publish\$Runtime"
    $configuration = "C:\ProgramData\KyberDaemon\kyberd.conf"

    if (-not (Test-Path $deployFolder)) {
        throw "Dossier de déploiement KyberDaemon introuvable: $deployFolder"
    }

    if (-not (Test-Path $publishFolder)) {
        Write-Host "Compilation de KyberDaemon en Release ($Runtime)..." -ForegroundColor Cyan
        Push-Location $deployFolder
        dotnet publish "..\KyberDaemon.csproj" -c Release -r $Runtime --self-contained false -o $publishFolder | Out-Null
        Pop-Location
    }

    $dllPath = Join-Path $publishFolder "KyberDaemon.dll"
    $exePath = Join-Path $publishFolder "KyberDaemon.exe"

    if (Test-Path $exePath) {
        $binaryPath = "`"$exePath`""
    } elseif (Test-Path $dllPath) {
        try {
            $dotnetPath = (Get-Command dotnet -ErrorAction Stop).Source
        } catch {
            throw "dotnet introuvable. Installez le runtime .NET ou publiez en mode self-contained."
        }
        $binaryPath = "`"$dotnetPath`" `"$dllPath`""
    } else {
        throw "Aucun binaire KyberDaemon trouvé dans $publishFolder"
    }

    if (-not (Test-Path $configuration)) {
        Write-Host "Création du dossier de configuration et copie des fichiers par défaut" -ForegroundColor Yellow
        $configDir = Split-Path $configuration -Parent
        if (-not (Test-Path $configDir)) {
            New-Item -ItemType Directory -Path $configDir -Force | Out-Null
        }

        $kyberdTemplate = Join-Path $daemonRoot "config\kyberd.conf"
        if (-not (Test-Path $kyberdTemplate)) {
            throw "Template kyberd.conf introuvable: $kyberdTemplate"
        }

        Get-Content $kyberdTemplate | ForEach-Object {
            $_ -replace 'auth_config\s*=\s*.*', "auth_config = $(Join-Path $configDir 'auth.conf')" `
               -replace 'keys_directory\s*=\s*.*', "keys_directory = $(Join-Path $configDir 'keys')" `
               -replace 'key_encryption_mode\s*=\s*.*', 'key_encryption_mode = dpapi          # dpapi (Windows) ou passphrase' `
               -replace 'key_encryption_passphrase\s*=\s*.*', 'key_encryption_passphrase =                   # obligatoire si key_encryption_mode=passphrase'
        } | Set-Content $configuration

        $authSource = Join-Path $daemonRoot "config\auth.conf"
        if (Test-Path $authSource) {
            Copy-Item $authSource -Destination (Join-Path $configDir "auth.conf") -Force
        }

        $keysDir = Join-Path $configDir "keys"
        if (-not (Test-Path $keysDir)) {
            New-Item -ItemType Directory -Path $keysDir -Force | Out-Null
        }
    }

    Write-Host "Binaire utilisé : $binaryPath" -ForegroundColor DarkGray
    Write-Host "Configuration : $configuration" -ForegroundColor DarkGray

    $binPath = ('{0} --config "{1}"' -f $binaryPath, $configuration)
    Write-Host "Command line : $binPath" -ForegroundColor DarkGray

    $serviceDisplayName = "$ServiceName Post-Quantum Service"
    Write-Host "Création du service Windows $ServiceName" -ForegroundColor Cyan
    sc.exe create $ServiceName binPath= "$binPath" DisplayName= "$serviceDisplayName" start= auto | Out-Null

    if (-not $NoStart) {
        Write-Host "Démarrage du service $ServiceName" -ForegroundColor Cyan
        Start-Service -Name $ServiceName -ErrorAction SilentlyContinue
    }
}
else {
    Write-Host "Mode client : installation du service ignorée" -ForegroundColor Yellow
}

Write-Host "Déploiement terminé. Installation dans $InstallRoot" -ForegroundColor Green

if (Test-Path $tmpRoot) {
    Remove-Item -Recurse -Force $tmpRoot -ErrorAction SilentlyContinue
}
