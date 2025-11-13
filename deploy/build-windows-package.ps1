param(
    [string]$OutputDirectory = "dist/windows",
    [string]$Runtime = "win-x64",
    [switch]$FrameworkDependent
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$solutionRoot = Resolve-Path (Join-Path $root "..")

Write-Host "==> Nettoyage de la sortie existante" -ForegroundColor Cyan
Remove-Item -Recurse -Force $OutputDirectory -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Path $OutputDirectory | Out-Null

$publishArgs = @("publish", "-c", "Release", "-r", $Runtime)
$publishArgs += "--self-contained"
$publishArgs += ($FrameworkDependent.IsPresent ? "false" : "true")

# 1. Publier KyberDaemon
Write-Host "==> Publication de KyberDaemon ($Runtime)" -ForegroundColor Cyan
$daemonPublishSource = Join-Path $solutionRoot "KyberDaemon/publish/$Runtime"
Push-Location (Join-Path $solutionRoot "KyberDaemon")
dotnet @publishArgs -o $daemonPublishSource
Pop-Location

# 2. Publier KyberCLI
Write-Host "==> Publication de KyberCLI ($Runtime)" -ForegroundColor Cyan
$cliPublishSource = Join-Path $solutionRoot "KyberCLI/publish/$Runtime"
Push-Location (Join-Path $solutionRoot "KyberCLI")
dotnet @publishArgs -o $cliPublishSource
Pop-Location

# 3. Construire le module PowerShell (KyberModule)
Write-Host "==> Compilation du module PowerShell" -ForegroundColor Cyan
Push-Location (Join-Path $solutionRoot "KyberModule")
dotnet build -c Release
Pop-Location

$packageRoot = Join-Path $OutputDirectory "KyberModule"
New-Item -ItemType Directory -Path $packageRoot | Out-Null

# Copier module PowerShell
Copy-Item "$solutionRoot/KyberModule/KyberModule.psd1" $packageRoot
Copy-Item "$solutionRoot/KyberModule/bin/Release/netstandard2.0/KyberModule.dll" $packageRoot
Copy-Item "$solutionRoot/KyberLibrary/bin/Release/netstandard2.0/KyberLibrary.dll" $packageRoot

# Copier dépendances BouncyCastle
$bcPath = "$env:USERPROFILE/.nuget/packages/bouncycastle.cryptography/2.6.2/lib/netstandard2.0/BouncyCastle.Cryptography.dll"
if (-not (Test-Path $bcPath)) {
    throw "BouncyCastle.Cryptography.dll introuvable. Merci d'exécuter 'dotnet restore'."
}
Copy-Item $bcPath (Join-Path $packageRoot "BouncyCastle.Cryptography.dll")

# Copier exemples/scripts utiles
Copy-Item "$solutionRoot/KyberModule/Examples.ps1" $packageRoot -ErrorAction SilentlyContinue
Copy-Item "$solutionRoot/KyberModule/DilithiumExamples.ps1" $packageRoot -ErrorAction SilentlyContinue

# Copier KyberDaemon
Write-Host "==> Copie de KyberDaemon" -ForegroundColor Cyan
$daemonTargetRoot = Join-Path $OutputDirectory "KyberDaemon"
$daemonPublishTarget = Join-Path $daemonTargetRoot "publish/$Runtime"
New-Item -ItemType Directory -Path $daemonPublishTarget -Force | Out-Null
Copy-Item (Join-Path $daemonPublishSource '*') $daemonPublishTarget -Recurse
Copy-Item "$solutionRoot/KyberDaemon/config" (Join-Path $daemonTargetRoot "config") -Recurse -Force
Copy-Item "$solutionRoot/KyberDaemon/deploy" (Join-Path $daemonTargetRoot "deploy") -Recurse
Copy-Item "$solutionRoot/KyberDaemon/tools" (Join-Path $daemonTargetRoot "tools") -Recurse

# Copier KyberCLI (binaire + config client)
Write-Host "==> Copie de KyberCLI" -ForegroundColor Cyan
$cliTargetRoot = Join-Path $OutputDirectory "KyberCLI"
$cliPublishTarget = Join-Path $cliTargetRoot "publish/$Runtime"
New-Item -ItemType Directory -Path $cliPublishTarget -Force | Out-Null
Copy-Item (Join-Path $cliPublishSource '*') $cliPublishTarget -Recurse
$cliConfigDir = Join-Path $cliTargetRoot "config"
New-Item -ItemType Directory -Path $cliConfigDir -Force | Out-Null
Copy-Item "$solutionRoot/KyberDaemon/config/auth.conf" (Join-Path $cliConfigDir "auth.conf") -Force
Copy-Item "$solutionRoot/KyberDaemon/config/kyberd.conf" (Join-Path $cliConfigDir "kyberd.conf") -Force

# Générer un zip
$zipPath = Join-Path $OutputDirectory "KyberModule-${Runtime}.zip"
if (Test-Path $zipPath) { Remove-Item $zipPath }
Write-Host "==> Création de l'archive $zipPath" -ForegroundColor Cyan
Compress-Archive -Path (Join-Path $OutputDirectory '*') -DestinationPath $zipPath

Write-Host "✅ Package Windows disponible dans $zipPath" -ForegroundColor Green
