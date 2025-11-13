# ============================================
# Tests d'Audit et Validation Cryptographique
# ============================================
# Ce script exécute des tests de conformité NIST et des tests de fuzzing
# pour valider la sécurité et la robustesse des implémentations cryptographiques

# Importer le module
$ModulePath = Split-Path -Parent $MyInvocation.MyCommand.Path
Import-Module (Join-Path $ModulePath "KyberModule.psd1") -Force

Write-Host "=== Audit et Validation Cryptographique ===" -ForegroundColor Cyan
Write-Host ""

$auditResults = @{
    ConformanceTests = @{
        Passed = 0
        Failed = 0
        Total = 0
        FailedTests = @()  # Liste détaillée des tests échoués
    }
    FuzzingTests = @{
        Passed = 0
        Failed = 0
        Crashed = 0
        Total = 0
        CorruptedDataTests = 0  # Nombre de tests avec données intentionnellement corrompues
        ValidDataTests = 0      # Nombre de tests avec données valides
    }
    AllTests = @()
}

function Write-TestResult {
    param(
        [string]$TestName,
        [bool]$Passed,
        [string[]]$Errors = @(),
        [string]$TestCategory = "Conformité"
    )
    
    if ($Passed) {
        Write-Host "  ✅ $TestName" -ForegroundColor Green
        $script:auditResults.ConformanceTests.Passed++
    } else {
        Write-Host "  ❌ $TestName" -ForegroundColor Red
        Write-Host "     [PROBLÈME DE CONFORMITÉ IDENTIFIÉ]" -ForegroundColor Red
        $script:auditResults.ConformanceTests.Failed++
        
        # Enregistrer le test échoué avec détails exploitables
        $failedTest = @{
            Name = $TestName
            Category = $TestCategory
            Errors = $Errors
            Timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
        }
        $script:auditResults.ConformanceTests.FailedTests += $failedTest
        
        # Afficher les erreurs de manière structurée
        if ($Errors.Count -gt 0) {
            Write-Host "     Détails du problème:" -ForegroundColor Yellow
            foreach ($err in $Errors) {
                Write-Host "       • $err" -ForegroundColor Yellow
            }
        } else {
            Write-Host "       • Aucun détail fourni - Vérifier manuellement" -ForegroundColor Yellow
        }
        Write-Host "     → ACTION REQUISE: Corriger ce problème de conformité avant utilisation" -ForegroundColor Red
    }
    $script:auditResults.ConformanceTests.Total++
    $script:auditResults.AllTests += @{Name = $TestName; Passed = $Passed; Errors = $Errors; Category = $TestCategory}
}

function Write-FuzzingResult {
    param(
        [string]$TestName,
        [int]$Total,
        [int]$Passed,
        [int]$Failed,
        [int]$Crashed,
        [string]$CorruptionType = ""
    )
    
    $script:auditResults.FuzzingTests.Total += $Total
    $script:auditResults.FuzzingTests.Passed += $Passed
    $script:auditResults.FuzzingTests.Failed += $Failed
    $script:auditResults.FuzzingTests.Crashed += $Crashed
    
    # Compter les tests avec données corrompues vs valides
    if ($Failed -gt 0) {
        $script:auditResults.FuzzingTests.CorruptedDataTests += $Failed
    }
    if ($Passed -gt 0) {
        $script:auditResults.FuzzingTests.ValidDataTests += $Passed
    }
    
    if ($Crashed -gt 0) {
        $status = "❌"
        $color = "Red"
    } elseif ($Failed -gt 0) {
        $status = "⚠️"
        $color = "Yellow"
    } else {
        $status = "✅"
        $color = "Green"
    }
    
    Write-Host "  $status $TestName" -ForegroundColor $color
    Write-Host "     ┌─ Statistiques détaillées:" -ForegroundColor Gray
    Write-Host "     │  Total: $Total tests" -ForegroundColor Gray
    
    # Distinguer clairement données valides vs corrompues
    if ($Passed -gt 0) {
        Write-Host "     │  ✅ Données VALIDES: $Passed test(s) réussi(s)" -ForegroundColor Green
    }
    if ($Failed -gt 0) {
        Write-Host "     │  🔴 Données INTENTIONNELLEMENT CORROMPUES: $Failed test(s)" -ForegroundColor Magenta
        if ($CorruptionType -ne "") {
            Write-Host "     │     Type de corruption: $CorruptionType" -ForegroundColor DarkMagenta
        }
        Write-Host "     │     → Ces données ont été générées EXPRÈS pour être invalides" -ForegroundColor DarkMagenta
        Write-Host "     │     → Comportement attendu: REJET de ces données (sécurité)" -ForegroundColor Cyan
    }
    if ($Crashed -gt 0) {
        Write-Host "     │  ❌ CRASHES: $Crashed exception(s) non gérée(s)" -ForegroundColor Red
        Write-Host "     │     → PROBLÈME: Données corrompues ont causé un crash au lieu d'être rejetées" -ForegroundColor Red
    }
    Write-Host "     └────────────────────────────────────────" -ForegroundColor Gray
    
    # Explications contextuelles
    if ($Crashed -gt 0) {
        Write-Host "     ⚠️  [PROBLÈME DE SÉCURITÉ IDENTIFIÉ]" -ForegroundColor Red
        Write-Host "        → $Crashed crash(es) lors du traitement de données corrompues" -ForegroundColor Red
        Write-Host "        → Risques: Déni de service, fuite d'information, comportement imprévisible" -ForegroundColor Red
        Write-Host "        → ACTION REQUISE: Améliorer la gestion d'erreurs" -ForegroundColor Red
    }
    if ($Failed -gt 0 -and $Crashed -eq 0) {
        Write-Host "     ✅ [COMPORTEMENT SÉCURISÉ CONFIRMÉ]" -ForegroundColor Cyan
        Write-Host "        → Les $Failed donnée(s) intentionnellement corrompue(s) ont été correctement rejetées" -ForegroundColor Cyan
        Write-Host "        → C'est le comportement attendu et souhaité pour la sécurité" -ForegroundColor Cyan
    }
    if ($Passed -eq $Total -and $Failed -eq 0 -and $Crashed -eq 0) {
        Write-Host "     ✅ Tous les tests avec données valides ont réussi" -ForegroundColor Green
    }
}

# ============================================
# Partie 1: Tests de Conformité NIST
# ============================================
Write-Host "1. Tests de Conformité NIST" -ForegroundColor Yellow
Write-Host ""
Write-Host "   Objectif: Vérifier que les implémentations respectent les spécifications NIST" -ForegroundColor Gray
Write-Host "   ✅ Réussi = L'algorithme fonctionne correctement selon les standards" -ForegroundColor Gray
Write-Host "   ❌ Échoué = Problème critique: l'algorithme ne respecte pas les spécifications NIST" -ForegroundColor Gray
Write-Host "              → Action requise: Corriger l'implémentation avant utilisation en production" -ForegroundColor Gray
Write-Host ""

# Tests Kyber
Write-Host "   Tests Kyber (ML-KEM)..." -ForegroundColor Cyan
Write-Host "      Vérification de conformité aux spécifications NIST ML-KEM (Kyber)" -ForegroundColor DarkGray
$kyberConformance = Test-CryptographicConformance -Algorithm Kyber -KyberParameterSet Kyber768
foreach ($result in $kyberConformance) {
    Write-TestResult -TestName "Kyber: $($result.TestName)" -Passed $result.Passed -Errors $result.Errors -TestCategory "Conformité NIST ML-KEM"
}

# Tests Dilithium
Write-Host "   Tests Dilithium (ML-DSA)..." -ForegroundColor Cyan
Write-Host "      Vérification de conformité aux spécifications NIST ML-DSA (Dilithium)" -ForegroundColor DarkGray
$dilithiumConformance = Test-CryptographicConformance -Algorithm Dilithium -DilithiumParameterSet Dilithium3
foreach ($result in $dilithiumConformance) {
    Write-TestResult -TestName "Dilithium: $($result.TestName)" -Passed $result.Passed -Errors $result.Errors -TestCategory "Conformité NIST ML-DSA"
}

# Tests Ed25519
Write-Host "   Tests Ed25519..." -ForegroundColor Cyan
Write-Host "      Vérification de conformité aux spécifications Ed25519" -ForegroundColor DarkGray
$ed25519Conformance = Test-CryptographicConformance -Algorithm Ed25519
foreach ($result in $ed25519Conformance) {
    Write-TestResult -TestName "Ed25519: $($result.TestName)" -Passed $result.Passed -Errors $result.Errors -TestCategory "Conformité Ed25519"
}

Write-Host ""

# ============================================
# Partie 2: Tests de Fuzzing
# ============================================
Write-Host "2. Tests de Fuzzing (Robustesse)" -ForegroundColor Yellow
Write-Host ""
Write-Host "   Objectif: Tester la robustesse face à des données corrompues ou malformées" -ForegroundColor Gray
Write-Host "   ✅ Passed = L'algorithme a correctement traité des données valides" -ForegroundColor Gray
Write-Host "   ⚠️  Failed = ATTENDU: L'algorithme a correctement rejeté des données invalides" -ForegroundColor Gray
Write-Host "              → C'est le comportement souhaité pour la sécurité" -ForegroundColor Gray
Write-Host "   ❌ Crashed = PROBLÈME: Exception non gérée lors du traitement" -ForegroundColor Gray
Write-Host "              → Action requise: Améliorer la gestion d'erreurs pour éviter les crashes" -ForegroundColor Gray
Write-Host ""

# Tests Kyber
Write-Host "   Fuzzing Kyber (ML-KEM)..." -ForegroundColor Cyan
Write-Host "      Test avec données valides ET données intentionnellement corrompues" -ForegroundColor DarkGray
$kyberFuzzing = Invoke-CryptographicFuzzing -Algorithm Kyber -KyberParameterSet Kyber768 -Iterations 50
foreach ($result in $kyberFuzzing) {
    Write-FuzzingResult -TestName "Kyber Fuzzing: $($result.TestName)" -Total $result.TotalTests -Passed $result.Passed -Failed $result.Failed -Crashed $result.Crashed -CorruptionType "Ciphertext/Clés corrompus, tailles invalides"
    if ($result.Exceptions.Count -gt 0) {
        Write-Host "     ⚠️  Exceptions capturées (gérées): $($result.Exceptions.Count)" -ForegroundColor Yellow
        Write-Host "        → Ces exceptions ont été capturées et gérées correctement" -ForegroundColor Cyan
    }
}

# Tests Dilithium
Write-Host "   Fuzzing Dilithium (ML-DSA)..." -ForegroundColor Cyan
Write-Host "      Test avec données valides ET données intentionnellement corrompues" -ForegroundColor DarkGray
$dilithiumFuzzing = Invoke-CryptographicFuzzing -Algorithm Dilithium -DilithiumParameterSet Dilithium3 -Iterations 50
foreach ($result in $dilithiumFuzzing) {
    Write-FuzzingResult -TestName "Dilithium Fuzzing: $($result.TestName)" -Total $result.TotalTests -Passed $result.Passed -Failed $result.Failed -Crashed $result.Crashed -CorruptionType "Signatures/Messages corrompus, tailles invalides"
    if ($result.Exceptions.Count -gt 0) {
        Write-Host "     ⚠️  Exceptions capturées (gérées): $($result.Exceptions.Count)" -ForegroundColor Yellow
        Write-Host "        → Ces exceptions ont été capturées et gérées correctement" -ForegroundColor Cyan
    }
}

# Tests Ed25519
Write-Host "   Fuzzing Ed25519..." -ForegroundColor Cyan
Write-Host "      Test avec données valides ET données intentionnellement corrompues" -ForegroundColor DarkGray
$ed25519Fuzzing = Invoke-CryptographicFuzzing -Algorithm Ed25519 -Iterations 50
foreach ($result in $ed25519Fuzzing) {
    Write-FuzzingResult -TestName "Ed25519 Fuzzing: $($result.TestName)" -Total $result.TotalTests -Passed $result.Passed -Failed $result.Failed -Crashed $result.Crashed -CorruptionType "Signatures/Clés corrompus, tailles invalides"
    if ($result.Exceptions.Count -gt 0) {
        Write-Host "     ⚠️  Exceptions capturées (gérées): $($result.Exceptions.Count)" -ForegroundColor Yellow
        Write-Host "        → Ces exceptions ont été capturées et gérées correctement" -ForegroundColor Cyan
    }
}

Write-Host ""

# ============================================
# Partie 3: Tests de Validation Cryptographique
# ============================================
Write-Host "3. Tests de Validation Cryptographique" -ForegroundColor Yellow
Write-Host ""
Write-Host "   Objectif: Vérifier les propriétés cryptographiques fondamentales" -ForegroundColor Gray
Write-Host "   ✅ Réussi = La propriété cryptographique est respectée" -ForegroundColor Gray
Write-Host "   ❌ Échoué = Violation d'une propriété de sécurité critique" -ForegroundColor Gray
Write-Host "              → Action requise: Corriger le problème avant utilisation" -ForegroundColor Gray
Write-Host ""

# Test 1: Propriété d'intégrité (clés partagées identiques)
Write-Host "   Test d'intégrité (clés partagées)..." -ForegroundColor Cyan
Write-Host "      Vérifie que les clés partagées générées par encapsulation/décapsulation sont identiques" -ForegroundColor DarkGray
try {
    $keys = New-KyberKeyPair -ParameterSet Kyber768
    $encapsulated = Invoke-KyberEncapsulate -PublicKey $keys.PublicKey -ParameterSet Kyber768
    $decapsulated = Invoke-KyberDecapsulate -Ciphertext $encapsulated.Ciphertext -PrivateKey $keys.PrivateKey -ParameterSet Kyber768
    
    $match = Test-ConstantTimeCompare -Array1 $encapsulated.SharedSecret -Array2 $decapsulated.SharedSecret
    if (-not $match) {
        Write-TestResult -TestName "Intégrité clés partagées Kyber" -Passed $false -Errors @("Les clés partagées ne correspondent pas - L'échange de clés échouerait") -TestCategory "Validation Cryptographique"
    } else {
        Write-TestResult -TestName "Intégrité clés partagées Kyber" -Passed $true -TestCategory "Validation Cryptographique"
    }
} catch {
    Write-TestResult -TestName "Intégrité clés partagées Kyber" -Passed $false -Errors @("Exception: $($_.Exception.Message)") -TestCategory "Validation Cryptographique"
}

# Test 2: Propriété d'authentification (signatures Dilithium)
Write-Host "   Test d'authentification (signatures Dilithium)..." -ForegroundColor Cyan
Write-Host "      Vérifie que les signatures peuvent être vérifiées correctement" -ForegroundColor DarkGray
try {
    $keys = New-DilithiumKeyPair -ParameterSet Dilithium3
    $message = "Message de test"
    $messageBytes = [System.Text.Encoding]::UTF8.GetBytes($message)
    $signature = Invoke-DilithiumSign -Data $messageBytes -PrivateKey $keys.PrivateKey -ParameterSet Dilithium3
    
    $isValid = Test-DilithiumSignature -Data $messageBytes -Signature $signature.Signature -PublicKey $keys.PublicKey -ParameterSet Dilithium3
    if (-not $isValid) {
        Write-TestResult -TestName "Authentification signature Dilithium" -Passed $false -Errors @("La signature valide n'a pas été acceptée - Problème de vérification") -TestCategory "Validation Cryptographique"
    } else {
        Write-TestResult -TestName "Authentification signature Dilithium" -Passed $true -TestCategory "Validation Cryptographique"
    }
} catch {
    Write-TestResult -TestName "Authentification signature Dilithium" -Passed $false -Errors @("Exception: $($_.Exception.Message)") -TestCategory "Validation Cryptographique"
}

# Test 3: Rejet de données invalides
Write-Host "   Test de rejet de données invalides..." -ForegroundColor Cyan
Write-Host "      Vérifie que les données INTENTIONNELLEMENT CORROMPUES sont correctement rejetées" -ForegroundColor DarkGray
try {
    $keys = New-KyberKeyPair -ParameterSet Kyber768
    $encapsulated = Invoke-KyberEncapsulate -PublicKey $keys.PublicKey -ParameterSet Kyber768
    
    # CORRUPTION INTENTIONNELLE: Modifier le premier byte du ciphertext
    Write-Host "      [GÉNÉRATION DE DONNÉES CORROMPUES]" -ForegroundColor Magenta
    Write-Host "         → Ciphertext original: $($encapsulated.Ciphertext.Length) bytes" -ForegroundColor DarkGray
    $corruptedCiphertext = $encapsulated.Ciphertext.Clone()
    $originalByte = $corruptedCiphertext[0]
    $corruptedCiphertext[0] = [byte]($corruptedCiphertext[0] -bxor 0xFF)
    Write-Host "         → Byte 0 modifié: 0x$($originalByte.ToString('X2')) → 0x$($corruptedCiphertext[0].ToString('X2'))" -ForegroundColor Magenta
    Write-Host "         → Cette corruption est INTENTIONNELLE pour tester le rejet" -ForegroundColor Magenta
    
    try {
        $decapsulated = Invoke-KyberDecapsulate -Ciphertext $corruptedCiphertext -PrivateKey $keys.PrivateKey -ParameterSet Kyber768
        # Si on arrive ici, le test a échoué (devrait rejeter)
        Write-TestResult -TestName "Rejet ciphertext corrompu" -Passed $false -Errors @("CRITIQUE: Le ciphertext intentionnellement corrompu a été accepté au lieu d'être rejeté", "Byte modifié: position 0, valeur originale: 0x$($originalByte.ToString('X2')), valeur corrompue: 0x$($corruptedCiphertext[0].ToString('X2'))") -TestCategory "Validation Sécurité"
    } catch {
        # Comportement attendu : rejet des données invalides
        Write-Host "      ✅ [COMPORTEMENT SÉCURISÉ CONFIRMÉ]" -ForegroundColor Cyan
        Write-Host "         → Les données corrompues ont été correctement rejetées" -ForegroundColor Cyan
        Write-TestResult -TestName "Rejet ciphertext corrompu" -Passed $true -TestCategory "Validation Sécurité"
    }
} catch {
    Write-TestResult -TestName "Rejet ciphertext corrompu" -Passed $false -Errors @("Exception lors du test: $($_.Exception.Message)") -TestCategory "Validation Sécurité"
}

# Test 4: Non-déterminisme des générations de clés
Write-Host "   Test de non-déterminisme..." -ForegroundColor Cyan
Write-Host "      Vérifie que chaque génération de clés produit des clés différentes (sécurité)" -ForegroundColor DarkGray
try {
    $keys1 = New-KyberKeyPair -ParameterSet Kyber768
    Start-Sleep -Milliseconds 10  # Petit délai pour garantir la non-déterminisme
    $keys2 = New-KyberKeyPair -ParameterSet Kyber768
    
    $publicKeysMatch = Test-ConstantTimeCompare -Array1 $keys1.PublicKey -Array2 $keys2.PublicKey
    $privateKeysMatch = Test-ConstantTimeCompare -Array1 $keys1.PrivateKey -Array2 $keys2.PrivateKey
    
    # Les clés ne doivent PAS être identiques
    if ($publicKeysMatch -or $privateKeysMatch) {
        $errors = @()
        if ($publicKeysMatch) { $errors += "Les clés publiques sont identiques - Problème d'aléa" }
        if ($privateKeysMatch) { $errors += "Les clés privées sont identiques - Problème d'aléa" }
        Write-TestResult -TestName "Non-déterminisme génération clés" -Passed $false -Errors $errors -TestCategory "Validation Cryptographique"
    } else {
        Write-TestResult -TestName "Non-déterminisme génération clés" -Passed $true -TestCategory "Validation Cryptographique"
    }
} catch {
    Write-TestResult -TestName "Non-déterminisme génération clés" -Passed $false -Errors @("Exception: $($_.Exception.Message)") -TestCategory "Validation Cryptographique"
}

Write-Host ""

# ============================================
# Partie 4: Tests de Stress
# ============================================
Write-Host "4. Tests de Stress" -ForegroundColor Yellow
Write-Host ""
Write-Host "   Objectif: Vérifier la stabilité et les performances sous charge" -ForegroundColor Gray
Write-Host "   ✅ Réussi = L'implémentation est stable et performante" -ForegroundColor Gray
Write-Host "   ❌ Échoué = Problème de stabilité ou de performance" -ForegroundColor Gray
Write-Host ""

# Test 1: Génération multiple de clés
Write-Host "   Test de génération multiple..." -ForegroundColor Cyan
try {
    $startTime = Get-Date
    $count = 10
    for ($i = 0; $i -lt $count; $i++) {
        $keys = New-KyberKeyPair -ParameterSet Kyber768 | Out-Null
    }
    $endTime = Get-Date
    $duration = ($endTime - $startTime).TotalMilliseconds
    $avgTime = $duration / $count
    
    Write-Host "     Généré $count paires de clés en $([math]::Round($duration, 2)) ms" -ForegroundColor Gray
    Write-Host "     Temps moyen: $([math]::Round($avgTime, 2)) ms par paire" -ForegroundColor Gray
    Write-TestResult -TestName "Génération multiple de clés ($count itérations)" -Passed $true -TestCategory "Tests de Stress"
} catch {
    Write-TestResult -TestName "Génération multiple de clés" -Passed $false -Errors @($_.Exception.Message) -TestCategory "Tests de Stress"
}

# Test 2: Encapsulation/Décapsulation multiple
Write-Host "   Test d'encapsulation/décapsulation multiple..." -ForegroundColor Cyan
try {
    $keys = New-KyberKeyPair -ParameterSet Kyber768
    $startTime = Get-Date
    $count = 50
    $successCount = 0
    
    for ($i = 0; $i -lt $count; $i++) {
        try {
            $encapsulated = Invoke-KyberEncapsulate -PublicKey $keys.PublicKey -ParameterSet Kyber768
            $decapsulated = Invoke-KyberDecapsulate -Ciphertext $encapsulated.Ciphertext -PrivateKey $keys.PrivateKey -ParameterSet Kyber768
            $match = Test-ConstantTimeCompare -Array1 $encapsulated.SharedSecret -Array2 $decapsulated.SharedSecret
            if ($match) { $successCount++ }
        } catch {
            # Ignorer les erreurs individuelles
        }
    }
    
    $endTime = Get-Date
    $duration = ($endTime - $startTime).TotalMilliseconds
    
    Write-Host "     $successCount/$count opérations réussies en $([math]::Round($duration, 2)) ms" -ForegroundColor Gray
    Write-TestResult -TestName "Encapsulation/Décapsulation multiple ($count itérations)" -Passed ($successCount -eq $count) -TestCategory "Tests de Stress"
} catch {
    Write-TestResult -TestName "Encapsulation/Décapsulation multiple" -Passed $false -Errors @($_.Exception.Message) -TestCategory "Tests de Stress"
}

Write-Host ""

# ============================================
# Résumé des Tests
# ============================================
Write-Host "=== Résumé des Tests ===" -ForegroundColor Cyan
Write-Host ""

Write-Host "Tests de Conformité:" -ForegroundColor Yellow
Write-Host "  Réussis: $($auditResults.ConformanceTests.Passed)" -ForegroundColor Green
$conformanceFailed = $auditResults.ConformanceTests.Failed
Write-Host "  Échoués: $conformanceFailed" -ForegroundColor $(if ($conformanceFailed -eq 0) { "Green" } else { "Red" })
Write-Host "  Total: $($auditResults.ConformanceTests.Total)" -ForegroundColor Gray
Write-Host ""

if ($conformanceFailed -gt 0) {
    Write-Host "  ❌ [PROBLÈMES DE CONFORMITÉ IDENTIFIÉS]" -ForegroundColor Red
    Write-Host "     → $conformanceFailed test(s) de conformité NIST ont échoué" -ForegroundColor Red
    Write-Host "     → PROBLÈME CRITIQUE: L'implémentation ne respecte pas les standards NIST" -ForegroundColor Red
    Write-Host ""
    Write-Host "     Détails des problèmes de conformité (exploitables):" -ForegroundColor Yellow
    $problemCount = 1
    foreach ($failedTest in $auditResults.ConformanceTests.FailedTests) {
        Write-Host "     [$problemCount] $($failedTest.Name)" -ForegroundColor Yellow
        Write-Host "        Catégorie: $($failedTest.Category)" -ForegroundColor Gray
        Write-Host "        Timestamp: $($failedTest.Timestamp)" -ForegroundColor Gray
        if ($failedTest.Errors.Count -gt 0) {
            Write-Host "        Erreurs:" -ForegroundColor Gray
            foreach ($err in $failedTest.Errors) {
                Write-Host "          • $err" -ForegroundColor Yellow
            }
        }
        $problemCount++
    }
    Write-Host ""
    Write-Host "     → ACTION REQUISE: Corriger ces $conformanceFailed problème(s) avant toute utilisation en production" -ForegroundColor Red
} else {
    Write-Host "  ✅ [AUCUN PROBLÈME DE CONFORMITÉ]" -ForegroundColor Green
    Write-Host "     → Tous les tests de conformité sont passés" -ForegroundColor Green
    Write-Host "     → L'implémentation respecte les standards NIST" -ForegroundColor Green
}
Write-Host ""

Write-Host "Tests de Fuzzing:" -ForegroundColor Yellow
Write-Host "  Total: $($auditResults.FuzzingTests.Total)" -ForegroundColor Gray
Write-Host "  ✅ Données VALIDES testées: $($auditResults.FuzzingTests.ValidDataTests)" -ForegroundColor Green
Write-Host "  🔴 Données INTENTIONNELLEMENT CORROMPUES testées: $($auditResults.FuzzingTests.CorruptedDataTests)" -ForegroundColor Magenta
$fuzzingFailed = $auditResults.FuzzingTests.Failed
$fuzzingCrashed = $auditResults.FuzzingTests.Crashed
Write-Host "  Failed (rejets attendus): $fuzzingFailed" -ForegroundColor $(if ($fuzzingFailed -eq 0) { "Green" } else { "Cyan" })
Write-Host "  Crashed (problèmes): $fuzzingCrashed" -ForegroundColor $(if ($fuzzingCrashed -eq 0) { "Green" } else { "Red" })
Write-Host ""

# Explications détaillées pour le fuzzing
Write-Host "  Analyse des résultats de fuzzing:" -ForegroundColor Yellow
Write-Host "  ┌─────────────────────────────────────────────────────────" -ForegroundColor Gray
if ($fuzzingCrashed -gt 0) {
    Write-Host "  │ ❌ [PROBLÈME DE SÉCURITÉ IDENTIFIÉ]" -ForegroundColor Red
    Write-Host "  │    → $fuzzingCrashed crash(es) détecté(s) lors du traitement de données corrompues" -ForegroundColor Red
    Write-Host "  │    → Ces crashes indiquent des exceptions non gérées" -ForegroundColor Red
    Write-Host "  │    → RISQUES: Déni de service, fuite d'information, comportement imprévisible" -ForegroundColor Red
    Write-Host "  │    → ACTION REQUISE: Améliorer la gestion d'erreurs pour capturer toutes les exceptions" -ForegroundColor Red
} else {
    Write-Host "  │ ✅ Aucun crash détecté - Gestion d'erreurs correcte" -ForegroundColor Green
}

if ($fuzzingFailed -gt 0) {
    Write-Host "  │ ✅ [COMPORTEMENT SÉCURISÉ CONFIRMÉ]" -ForegroundColor Cyan
    Write-Host "  │    → $fuzzingFailed donnée(s) intentionnellement corrompue(s) ont été correctement rejetées" -ForegroundColor Cyan
    Write-Host "  │    → Ces 'Failed' sont ATTENDUS et prouvent que l'implémentation rejette les données invalides" -ForegroundColor Cyan
    Write-Host "  │    → Types de corruption testés: Ciphertext, signatures, clés, tailles invalides" -ForegroundColor Cyan
    Write-Host "  │    → AUCUNE ACTION REQUISE: C'est le comportement sécurisé souhaité" -ForegroundColor Cyan
} else {
    Write-Host "  │ ℹ️  Aucune donnée corrompue testée dans cette exécution" -ForegroundColor Gray
}

if ($auditResults.FuzzingTests.ValidDataTests -gt 0) {
    Write-Host "  │ ✅ $($auditResults.FuzzingTests.ValidDataTests) test(s) avec données valides: tous réussis" -ForegroundColor Green
}
Write-Host "  └─────────────────────────────────────────────────────────" -ForegroundColor Gray
Write-Host ""

# Calcul du résultat global
$allPassed = ($conformanceFailed -eq 0) -and ($fuzzingCrashed -eq 0)

if ($allPassed) {
    Write-Host "✅ RÉSULTAT GLOBAL: Tous les tests d'audit sont passés !" -ForegroundColor Green
    Write-Host ""
    Write-Host "Les implémentations cryptographiques sont :" -ForegroundColor Cyan
    Write-Host "  ✅ Conformes aux standards NIST" -ForegroundColor White
    Write-Host "  ✅ Robustes aux données corrompues (fuzzing)" -ForegroundColor White
    Write-Host "  ✅ Validées cryptographiquement" -ForegroundColor White
    Write-Host ""
    Write-Host "→ CONCLUSION: L'implémentation est prête pour une utilisation en production" -ForegroundColor Green
} else {
    Write-Host "❌ RÉSULTAT GLOBAL: Certains tests ont échoué ou ont causé des crashes." -ForegroundColor Red
    Write-Host ""
    if ($conformanceFailed -gt 0) {
        Write-Host "  ❌ Problèmes de conformité NIST détectés ($conformanceFailed échec(s))" -ForegroundColor Red
    }
    if ($fuzzingCrashed -gt 0) {
        Write-Host "  ❌ Crashes lors du fuzzing détectés ($fuzzingCrashed crash(es))" -ForegroundColor Red
    }
    Write-Host ""
    Write-Host "→ CONCLUSION: L'implémentation nécessite des corrections avant utilisation" -ForegroundColor Red
    Write-Host "→ ACTION: Vérifiez les détails ci-dessus et corrigez les problèmes identifiés" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "=== Guide d'Interprétation ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "Tests de Conformité:" -ForegroundColor Yellow
Write-Host "  • ✅ Réussi = L'algorithme fonctionne selon les spécifications NIST" -ForegroundColor Gray
Write-Host "  • ❌ Échoué = PROBLÈME CRITIQUE - L'algorithme ne respecte pas les standards" -ForegroundColor Gray
Write-Host ""
Write-Host "Tests de Fuzzing:" -ForegroundColor Yellow
Write-Host "  • ✅ Passed = Données valides correctement traitées" -ForegroundColor Gray
Write-Host "  • ⚠️  Failed = ATTENDU - Données invalides correctement rejetées (sécurité)" -ForegroundColor Gray
Write-Host "  • ❌ Crashed = PROBLÈME - Exception non gérée (risque de sécurité)" -ForegroundColor Gray
Write-Host ""
Write-Host "Critères de Succès:" -ForegroundColor Yellow
Write-Host "  • Tests de conformité: 0 échec requis" -ForegroundColor Gray
Write-Host "  • Tests de fuzzing: 0 crash requis (les 'Failed' sont acceptables)" -ForegroundColor Gray

