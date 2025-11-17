# Script de build pour KyberModule
# Ce script compile le projet et copie toutes les dépendances nécessaires

param(
    [string]$Configuration = "Release"
)

Write-Host "=== Build KyberModule ===" -ForegroundColor Cyan
Write-Host ""

# 1. Compiler KyberLibrary
Write-Host "1. Compilation de KyberLibrary..." -ForegroundColor Yellow
Push-Location "KyberLibrary"
try {
    dotnet build -c $Configuration
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ Erreur lors de la compilation de KyberLibrary" -ForegroundColor Red
        exit 1
    }
    Write-Host "✅ KyberLibrary compilé avec succès" -ForegroundColor Green
}
finally {
    Pop-Location
}

# 2. Compiler KyberModule
Write-Host "2. Compilation de KyberModule..." -ForegroundColor Yellow
Push-Location "KyberModule"
try {
    dotnet build -c $Configuration
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ Erreur lors de la compilation de KyberModule" -ForegroundColor Red
        exit 1
    }
    Write-Host "✅ KyberModule compilé avec succès" -ForegroundColor Green
}
finally {
    Pop-Location
}

# 3. Copier les DLLs compilées et dépendances dans KyberModule
Write-Host "3. Copie des DLLs et dépendances..." -ForegroundColor Yellow

$kyberModulePath = "KyberModule"
$kyberModuleDll = "KyberModule\bin\$Configuration\netstandard2.0\KyberModule.dll"
$kyberLibraryDll = "KyberLibrary\bin\$Configuration\netstandard2.0\KyberLibrary.dll"
$bouncyCastleDll = "$env:USERPROFILE\.nuget\packages\bouncycastle.cryptography\2.6.2\lib\netstandard2.0\BouncyCastle.Cryptography.dll"

# Décharger le module si déjà chargé pour éviter le verrouillage
Write-Host "   Déchargement du module KyberModule si présent..." -ForegroundColor Gray
try {
    Remove-Module KyberModule -ErrorAction SilentlyContinue
    Start-Sleep -Milliseconds 500
} catch {
    # Ignorer les erreurs de déchargement
}

# Copier KyberModule.dll
if (Test-Path $kyberModuleDll) {
    $maxRetries = 3
    $retryCount = 0
    $copied = $false
    
    while ($retryCount -lt $maxRetries -and -not $copied) {
        try {
            Copy-Item $kyberModuleDll -Destination "$kyberModulePath\KyberModule.dll" -Force -ErrorAction Stop
            Write-Host "   ✅ KyberModule.dll copié" -ForegroundColor Green
            $copied = $true
        } catch {
            $retryCount++
            if ($retryCount -lt $maxRetries) {
                Write-Host "   ⚠️  Tentative $retryCount/$maxRetries : DLL verrouillée, nouvelle tentative..." -ForegroundColor Yellow
                Start-Sleep -Milliseconds 1000
                # Essayer de décharger à nouveau
                Remove-Module KyberModule -ErrorAction SilentlyContinue
            } else {
                Write-Host "   ❌ KyberModule.dll verrouillé après $maxRetries tentatives" -ForegroundColor Red
                Write-Host "      Fermez toutes les sessions PowerShell et réessayez" -ForegroundColor Yellow
                Write-Host "      Ou utilisez: Remove-Module KyberModule -Force" -ForegroundColor Yellow
            }
        }
    }
} else {
    Write-Host "   ❌ KyberModule.dll introuvable: $kyberModuleDll" -ForegroundColor Red
}

# Copier KyberLibrary.dll
if (Test-Path $kyberLibraryDll) {
    Copy-Item $kyberLibraryDll -Destination "$kyberModulePath\KyberLibrary.dll" -Force
    Write-Host "   ✅ KyberLibrary.dll copié" -ForegroundColor Green
} else {
    Write-Host "   ❌ KyberLibrary.dll introuvable: $kyberLibraryDll" -ForegroundColor Red
}

# Copier BouncyCastle.Cryptography.dll
if (Test-Path $bouncyCastleDll) {
    Copy-Item $bouncyCastleDll -Destination "$kyberModulePath\BouncyCastle.Cryptography.dll" -Force
    Write-Host "   ✅ BouncyCastle.Cryptography.dll copié" -ForegroundColor Green
} else {
    Write-Host "   ⚠️  BouncyCastle.Cryptography.dll introuvable dans NuGet cache" -ForegroundColor Yellow
    Write-Host "      Chercher dans d'autres emplacements..." -ForegroundColor Yellow
    
    # Essayer de trouver BouncyCastle.Cryptography.dll dans le cache NuGet
    $nugetPath = "$env:USERPROFILE\.nuget\packages\bouncycastle.cryptography"
    if (Test-Path $nugetPath) {
        $latestVersion = Get-ChildItem $nugetPath -Directory | Sort-Object Name -Descending | Select-Object -First 1
        if ($latestVersion) {
            $bcDll = Join-Path $latestVersion.FullName "lib\netstandard2.0\BouncyCastle.Cryptography.dll"
            if (Test-Path $bcDll) {
                Copy-Item $bcDll -Destination "$kyberModulePath\BouncyCastle.Cryptography.dll" -Force
                Write-Host "   ✅ BouncyCastle.Cryptography.dll copié depuis $($latestVersion.Name)" -ForegroundColor Green
            }
        }
    }
    
    if (-not (Test-Path "$kyberModulePath\BouncyCastle.Cryptography.dll")) {
        Write-Host "   ❌ BouncyCastle.Cryptography.dll introuvable" -ForegroundColor Red
        Write-Host "      Exécutez: dotnet restore" -ForegroundColor Yellow
    }
}

Write-Host ""
Write-Host "=== Build terminé ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "Pour tester le module:" -ForegroundColor Yellow
Write-Host "  cd KyberModule" -ForegroundColor White
Write-Host "  Import-Module .\KyberModule.psd1" -ForegroundColor White
Write-Host "  .\DilithiumExamples.ps1" -ForegroundColor White

