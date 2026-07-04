# Script pour recompiler et tester le module
# Ferme automatiquement les sessions PowerShell si nécessaire

Write-Host "=== Recompilation et Test du Module KyberModule ===" -ForegroundColor Cyan
Write-Host ""

# 1. Décharger le module si chargé
Write-Host "1. Déchargement du module..." -ForegroundColor Yellow
try {
    Remove-Module KyberModule -Force -ErrorAction Stop
    Write-Host "   ✅ Module déchargé" -ForegroundColor Green
    Start-Sleep -Milliseconds 1000
} catch {
    Write-Host "   ℹ️  Module non chargé ou déjà déchargé" -ForegroundColor Gray
}

# 2. Compiler
Write-Host "`n2. Compilation du module..." -ForegroundColor Yellow
Push-Location $PSScriptRoot
try {
    & .\BUILD.ps1
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ Erreur lors de la compilation" -ForegroundColor Red
        exit 1
    }
} finally {
    Pop-Location
}

# 3. Attendre un peu pour que les fichiers soient libérés
Write-Host "`n3. Attente de la libération des fichiers..." -ForegroundColor Yellow
Start-Sleep -Seconds 2

# 4. Copier les DLLs manuellement si nécessaire
Write-Host "`n4. Vérification des DLLs..." -ForegroundColor Yellow
$kyberModuleDll = "KyberModule\bin\Release\netstandard2.0\KyberModule.dll"
$kyberLibraryDll = "KyberLibrary\bin\Release\netstandard2.0\KyberLibrary.dll"
$targetPath = "KyberModule\KyberModule"

if (Test-Path $kyberModuleDll) {
    $maxRetries = 5
    $retryCount = 0
    $copied = $false
    
    while ($retryCount -lt $maxRetries -and -not $copied) {
        try {
            Copy-Item $kyberModuleDll -Destination "$targetPath\KyberModule.dll" -Force -ErrorAction Stop
            Copy-Item $kyberLibraryDll -Destination "$targetPath\KyberLibrary.dll" -Force -ErrorAction Stop
            Write-Host "   ✅ DLLs copiées avec succès" -ForegroundColor Green
            $copied = $true
        } catch {
            $retryCount++
            if ($retryCount -lt $maxRetries) {
                Write-Host "   ⚠️  Tentative $retryCount/$maxRetries : DLL verrouillée, nouvelle tentative..." -ForegroundColor Yellow
                Start-Sleep -Milliseconds 2000
                Remove-Module KyberModule -Force -ErrorAction SilentlyContinue
            } else {
                Write-Host "   ❌ Impossible de copier les DLLs après $maxRetries tentatives" -ForegroundColor Red
                Write-Host "   Veuillez fermer toutes les sessions PowerShell et réessayer" -ForegroundColor Yellow
                Write-Host "   Ou exécutez manuellement:" -ForegroundColor Yellow
                Write-Host "     Copy-Item '$kyberModuleDll' -Destination '$targetPath\KyberModule.dll' -Force" -ForegroundColor White
                Write-Host "     Copy-Item '$kyberLibraryDll' -Destination '$targetPath\KyberLibrary.dll' -Force" -ForegroundColor White
            }
        }
    }
} else {
    Write-Host "   ❌ DLLs de compilation introuvables" -ForegroundColor Red
}

# 5. Tester le module
Write-Host "`n5. Test du module..." -ForegroundColor Yellow
Push-Location $targetPath
try {
    # Décharger à nouveau pour être sûr
    Remove-Module KyberModule -Force -ErrorAction SilentlyContinue
    Start-Sleep -Milliseconds 500
    
    # Charger le module
    Import-Module .\KyberModule.psd1 -Force -ErrorAction Stop
    Write-Host "   ✅ Module chargé" -ForegroundColor Green
    
    # Vérifier les cmdlets
    $requiredCmdlets = @(
        'New-KyberKeyPair',
        'Invoke-KyberEncapsulate',
        'Invoke-KyberDecapsulate',
        'New-DilithiumKeyPair',
        'Invoke-DilithiumSign',
        'Test-DilithiumSignature'
    )
    
    $missing = @()
    foreach ($cmdlet in $requiredCmdlets) {
        if (-not (Get-Command $cmdlet -ErrorAction SilentlyContinue)) {
            $missing += $cmdlet
        }
    }
    
    if ($missing.Count -eq 0) {
        Write-Host "   ✅ Toutes les cmdlets sont disponibles" -ForegroundColor Green
        Write-Host "`n=== Test réussi ! ===" -ForegroundColor Green
        Write-Host "Vous pouvez maintenant exécuter:" -ForegroundColor Yellow
        Write-Host "  .\KyberExamples.ps1" -ForegroundColor White
    } else {
        Write-Host "   ❌ Cmdlets manquantes:" -ForegroundColor Red
        foreach ($cmdlet in $missing) {
            Write-Host "     - $cmdlet" -ForegroundColor Red
        }
        Write-Host "`n   Le module doit être recompilé." -ForegroundColor Yellow
    }
} catch {
    Write-Host "   ❌ Erreur lors du test: $_" -ForegroundColor Red
} finally {
    Pop-Location
}

Write-Host ""


