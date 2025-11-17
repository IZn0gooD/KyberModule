# Script de nettoyage des fichiers de test et anciens fichiers
# Nettoie les fichiers générés par les tests, les certificats, les clés PKCS#8, etc.

param(
    [switch]$KeepEd25519Scripts = $false,
    [switch]$KeepCertificates = $false,
    [switch]$KeepKeys = $false
)

Write-Host "=== Nettoyage du projet KyberModule ===" -ForegroundColor Cyan
Write-Host ""

$cleaned = 0
$kept = 0

$modulePath = "KyberModule"

# 1. Nettoyer les fichiers de clés générées
if (-not $KeepKeys) {
    Write-Host "1. Nettoyage des fichiers de clés générées..." -ForegroundColor Yellow
    $keyPatterns = @(
        "*.txt",
        "*_keys.txt",
        "*_keypair.txt",
        "ed25519_*.txt",
        "kyber_*.txt",
        "dilithium_*.txt",
        "server_*.txt",
        "client_*.txt",
        "*_public_keys.txt",
        "*_private_key.*",
        "*_public_key.*",
        "shared_secret.*"
    )

    foreach ($pattern in $keyPatterns) {
        $files = Get-ChildItem -Path $modulePath -Filter $pattern -File -ErrorAction SilentlyContinue
        foreach ($file in $files) {
            try {
                Remove-Item $file.FullName -Force -ErrorAction Stop
                Write-Host "   ✅ Supprimé: $($file.Name)" -ForegroundColor Green
                $cleaned++
            } catch {
                Write-Host "   ⚠️  Impossible de supprimer: $($file.Name)" -ForegroundColor Yellow
            }
        }
    }
} else {
    Write-Host "1. Conservation des fichiers de clés (option -KeepKeys)" -ForegroundColor Gray
    $kept++
}

# 2. Nettoyer les certificats X.509 et fichiers PKCS#8
if (-not $KeepCertificates) {
    Write-Host "`n2. Nettoyage des certificats et fichiers PKCS#8..." -ForegroundColor Yellow
    $certPatterns = @(
        "*.pem",
        "*.der",
        "*.cer",
        "*.crt",
        "*_certificate.*",
        "*_cert.*",
        "*_pkcs8.*",
        "*_private_pkcs8.*",
        "*_public_pkcs8.*"
    )
    
    foreach ($pattern in $certPatterns) {
        $files = Get-ChildItem -Path $modulePath -Filter $pattern -File -ErrorAction SilentlyContinue
        foreach ($file in $files) {
            try {
                Remove-Item $file.FullName -Force -ErrorAction Stop
                Write-Host "   ✅ Supprimé: $($file.Name)" -ForegroundColor Green
                $cleaned++
            } catch {
                Write-Host "   ⚠️  Impossible de supprimer: $($file.Name)" -ForegroundColor Yellow
            }
        }
    }
} else {
    Write-Host "`n2. Conservation des certificats (option -KeepCertificates)" -ForegroundColor Gray
    $kept++
}

# 3. Nettoyer les anciens scripts de test Ed25519 (si demandé)
if (-not $KeepEd25519Scripts) {
    Write-Host "`n3. Nettoyage des anciens scripts de test Ed25519..." -ForegroundColor Yellow
    $ed25519Scripts = @(
        "QuickTestEd25519.ps1",
        "TestEd25519.ps1",
        "TestKyberEd25519.ps1",
        "KyberEd25519CompleteExample.ps1"
    )
    
    foreach ($script in $ed25519Scripts) {
        $scriptPath = Join-Path $modulePath $script
        if (Test-Path $scriptPath) {
            try {
                Remove-Item $scriptPath -Force -ErrorAction Stop
                Write-Host "   ✅ Supprimé: $script" -ForegroundColor Green
                $cleaned++
            } catch {
                Write-Host "   ⚠️  Impossible de supprimer: $script" -ForegroundColor Yellow
                $kept++
            }
        }
    }
} else {
    Write-Host "`n3. Conservation des scripts Ed25519 (option -KeepEd25519Scripts)" -ForegroundColor Gray
    $kept++
}

# 4. Nettoyer les fichiers chiffrés générés par les tests d'encryption
Write-Host "`n4. Nettoyage des fichiers chiffrés de test..." -ForegroundColor Yellow
$encryptedPatterns = @(
    "*_encrypted.*",
    "*_ciphertext.*",
    "*_encrypted_chacha20.*",
    "*_encrypted_aesgcm.*",
    "*_protected.*"
)

foreach ($pattern in $encryptedPatterns) {
    $files = Get-ChildItem -Path $modulePath -Filter $pattern -File -ErrorAction SilentlyContinue
    foreach ($file in $files) {
        try {
            Remove-Item $file.FullName -Force -ErrorAction Stop
            Write-Host "   ✅ Supprimé: $($file.Name)" -ForegroundColor Green
            $cleaned++
        } catch {
            # Ignorer les erreurs
        }
    }
}

# 5. Nettoyer les fichiers temporaires et logs
Write-Host "`n5. Nettoyage des fichiers temporaires..." -ForegroundColor Yellow
$tempPatterns = @("*.tmp", "*.log", "*.bak", "*.cache")
foreach ($pattern in $tempPatterns) {
    $files = Get-ChildItem -Path $modulePath -Filter $pattern -File -Recurse -ErrorAction SilentlyContinue
    foreach ($file in $files) {
        # Ignorer les fichiers dans obj/ et bin/
        if ($file.FullName -notmatch "\\obj\\" -and $file.FullName -notmatch "\\bin\\") {
            try {
                Remove-Item $file.FullName -Force -ErrorAction Stop
                Write-Host "   ✅ Supprimé: $($file.Name)" -ForegroundColor Green
                $cleaned++
            } catch {
                # Ignorer les erreurs
            }
        }
    }
}

Write-Host ""
Write-Host "=== Résumé du nettoyage ===" -ForegroundColor Cyan
Write-Host "✅ $cleaned fichier(s) supprimé(s)" -ForegroundColor Green
if ($kept -gt 0) {
    Write-Host "⚠️  $kept fichier(s) conservé(s)" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Options disponibles:" -ForegroundColor Cyan
Write-Host "  -KeepEd25519Scripts  : Conserver les anciens scripts Ed25519" -ForegroundColor Gray
Write-Host "  -KeepCertificates    : Conserver les certificats X.509 et fichiers PKCS#8" -ForegroundColor Gray
Write-Host "  -KeepKeys            : Conserver tous les fichiers de clés" -ForegroundColor Gray
Write-Host ""
Write-Host "Note: Les cmdlets C# Ed25519 sont conservés pour compatibilité." -ForegroundColor Gray
Write-Host "      Utilisez Dilithium pour une solution 100% post-quantique." -ForegroundColor Gray

