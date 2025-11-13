# ============================================
# Script de Benchmark de Performance
# Mesure les performances des algorithmes cryptographiques
# ============================================

$ErrorActionPreference = "Continue"

# Importer le module
$ModulePath = Split-Path -Parent $MyInvocation.MyCommand.Path
$ModuleManifest = Join-Path $ModulePath "KyberModule.psd1"

Write-Host "=== Benchmark de Performance - KyberModule ===" -ForegroundColor Cyan
Write-Host ""

if (-not (Test-Path $ModuleManifest)) {
    Write-Host "❌ ERREUR: Le manifeste du module est introuvable: $ModuleManifest" -ForegroundColor Red
    exit 1
}

try {
    Import-Module $ModuleManifest -Force -ErrorAction Stop
    Write-Host "✅ Module chargé" -ForegroundColor Green
} catch {
    Write-Host "❌ ERREUR lors du chargement du module: $_" -ForegroundColor Red
    exit 1
}

# Fonction pour mesurer le temps d'exécution avec haute précision
function Measure-Operation {
    param(
        [string]$OperationName,
        [scriptblock]$ScriptBlock,
        [int]$Iterations = 10
    )
    
    $timesMs = @()      # Temps en millisecondes
    $timesUs = @()      # Temps en microsecondes (plus précis)
    $timesTicks = @()   # Temps en ticks (précision maximale)
    $errors = 0
    $lastError = $null
    
    # Warm-up: exécuter une fois pour éviter les effets de cache/JIT
    try {
        $null = & $ScriptBlock
    } catch {
        # Ignorer les erreurs de warm-up
    }
    
    for ($i = 1; $i -le $Iterations; $i++) {
        $sw = [System.Diagnostics.Stopwatch]::StartNew()
        try {
            $null = & $ScriptBlock
            $sw.Stop()
            
            # Mesures avec différentes précisions
            $elapsedMs = $sw.Elapsed.TotalMilliseconds
            # Calculer les microsecondes (TotalMicroseconds n'existe pas en .NET Standard 2.0)
            $elapsedUs = $elapsedMs * 1000
            $elapsedTicks = $sw.ElapsedTicks
            
            $timesMs += $elapsedMs
            $timesUs += $elapsedUs
            $timesTicks += $elapsedTicks
        } catch {
            $sw.Stop()
            $errors++
            $lastError = $_.Exception.Message
            Write-Warning "  Erreur lors de l'itération $i/$Iterations : $($_.Exception.Message)"
        }
    }
    
    # Toujours retourner un résultat, même en cas d'erreur
    if ($timesMs.Count -eq 0) {
        # Aucune itération réussie
        Write-Host "    ❌ ÉCHEC: Aucune itération réussie" -ForegroundColor Red
        if ($lastError) {
            Write-Host "    Dernière erreur: $lastError" -ForegroundColor Yellow
        }
        return @{
            Operation = $OperationName
            Average = 0
            AverageUs = 0
            Min = 0
            MinUs = 0
            Max = 0
            MaxUs = 0
            Median = 0
            MedianUs = 0
            StdDev = 0
            StdDevUs = 0
            Iterations = 0
            SuccessfulIterations = 0
            Errors = $errors
            Status = "FAILED"
            ErrorMessage = $lastError
        }
    } elseif ($timesMs.Count -lt $Iterations) {
        # Certaines itérations ont échoué
        Write-Host "    ⚠️  PARTIEL: $($timesMs.Count)/$Iterations itérations réussies" -ForegroundColor Yellow
        
        # Calculer les statistiques
        $avgMs = ($timesMs | Measure-Object -Average).Average
        $avgUs = ($timesUs | Measure-Object -Average).Average
        $minMs = ($timesMs | Measure-Object -Minimum).Minimum
        $minUs = ($timesUs | Measure-Object -Minimum).Minimum
        $maxMs = ($timesMs | Measure-Object -Maximum).Maximum
        $maxUs = ($timesUs | Measure-Object -Maximum).Maximum
        
        # Médiane
        $sortedMs = $timesMs | Sort-Object
        $sortedUs = $timesUs | Sort-Object
        $medianMs = if ($sortedMs.Count % 2 -eq 0) {
            ($sortedMs[($sortedMs.Count / 2) - 1] + $sortedMs[$sortedMs.Count / 2]) / 2
        } else {
            $sortedMs[[math]::Floor($sortedMs.Count / 2)]
        }
        $medianUs = if ($sortedUs.Count % 2 -eq 0) {
            ($sortedUs[($sortedUs.Count / 2) - 1] + $sortedUs[$sortedUs.Count / 2]) / 2
        } else {
            $sortedUs[[math]::Floor($sortedUs.Count / 2)]
        }
        
        # Écart-type
        $varianceMs = ($timesMs | ForEach-Object { [math]::Pow($_ - $avgMs, 2) } | Measure-Object -Average).Average
        $varianceUs = ($timesUs | ForEach-Object { [math]::Pow($_ - $avgUs, 2) } | Measure-Object -Average).Average
        $stdDevMs = [math]::Sqrt($varianceMs)
        $stdDevUs = [math]::Sqrt($varianceUs)
        
        return @{
            Operation = $OperationName
            Average = [math]::Round($avgMs, 4)
            AverageUs = [math]::Round($avgUs, 2)
            Min = [math]::Round($minMs, 4)
            MinUs = [math]::Round($minUs, 2)
            Max = [math]::Round($maxMs, 4)
            MaxUs = [math]::Round($maxUs, 2)
            Median = [math]::Round($medianMs, 4)
            MedianUs = [math]::Round($medianUs, 2)
            StdDev = [math]::Round($stdDevMs, 4)
            StdDevUs = [math]::Round($stdDevUs, 2)
            Iterations = $Iterations
            SuccessfulIterations = $timesMs.Count
            Errors = $errors
            Status = "PARTIAL"
            ErrorMessage = $lastError
        }
    } else {
        # Toutes les itérations ont réussi
        # Calculer les statistiques
        $avgMs = ($timesMs | Measure-Object -Average).Average
        $avgUs = ($timesUs | Measure-Object -Average).Average
        $minMs = ($timesMs | Measure-Object -Minimum).Minimum
        $minUs = ($timesUs | Measure-Object -Minimum).Minimum
        $maxMs = ($timesMs | Measure-Object -Maximum).Maximum
        $maxUs = ($timesUs | Measure-Object -Maximum).Maximum
        
        # Médiane
        $sortedMs = $timesMs | Sort-Object
        $sortedUs = $timesUs | Sort-Object
        $medianMs = if ($sortedMs.Count % 2 -eq 0) {
            ($sortedMs[($sortedMs.Count / 2) - 1] + $sortedMs[$sortedMs.Count / 2]) / 2
        } else {
            $sortedMs[[math]::Floor($sortedMs.Count / 2)]
        }
        $medianUs = if ($sortedUs.Count % 2 -eq 0) {
            ($sortedUs[($sortedUs.Count / 2) - 1] + $sortedUs[$sortedUs.Count / 2]) / 2
        } else {
            $sortedUs[[math]::Floor($sortedUs.Count / 2)]
        }
        
        # Écart-type
        $varianceMs = ($timesMs | ForEach-Object { [math]::Pow($_ - $avgMs, 2) } | Measure-Object -Average).Average
        $varianceUs = ($timesUs | ForEach-Object { [math]::Pow($_ - $avgUs, 2) } | Measure-Object -Average).Average
        $stdDevMs = [math]::Sqrt($varianceMs)
        $stdDevUs = [math]::Sqrt($varianceUs)
        
        return @{
            Operation = $OperationName
            Average = [math]::Round($avgMs, 4)
            AverageUs = [math]::Round($avgUs, 2)
            Min = [math]::Round($minMs, 4)
            MinUs = [math]::Round($minUs, 2)
            Max = [math]::Round($maxMs, 4)
            MaxUs = [math]::Round($maxUs, 2)
            Median = [math]::Round($medianMs, 4)
            MedianUs = [math]::Round($medianUs, 2)
            StdDev = [math]::Round($stdDevMs, 4)
            StdDevUs = [math]::Round($stdDevUs, 2)
            Iterations = $Iterations
            SuccessfulIterations = $timesMs.Count
            Errors = 0
            Status = "SUCCESS"
            ErrorMessage = $null
        }
    }
}

Write-Host "📊 Mesure des performances avec haute précision..." -ForegroundColor Yellow
Write-Host "   - Opérations lentes (Kyber/Dilithium): 10 itérations" -ForegroundColor Gray
Write-Host "   - Opérations rapides (Ed25519/SHA3/AEAD): 500-1000 itérations pour précision" -ForegroundColor Gray
Write-Host "   - Précision: microsecondes (µs) et millisecondes (ms)" -ForegroundColor Gray
Write-Host "   - Statistiques: moyenne, médiane, min, max, écart-type" -ForegroundColor Gray
Write-Host ""

# ============================================
# Benchmark Kyber
# ============================================
Write-Host "=== Benchmark Kyber (ML-KEM) ===" -ForegroundColor Cyan

$kyber512Results = @()
$kyber768Results = @()
$kyber1024Results = @()

# Kyber512
Write-Host "  Kyber512..." -ForegroundColor Gray
$kyber512Results += Measure-Operation "Génération de clés Kyber512" {
    $null = New-KyberKeyPair -ParameterSet Kyber512
} -Iterations 10

$kyber512Keys = New-KyberKeyPair -ParameterSet Kyber512
$kyber512Results += Measure-Operation "Encapsulation Kyber512" {
    $null = Invoke-KyberEncapsulate -PublicKey $kyber512Keys.PublicKey -ParameterSet Kyber512
} -Iterations 10

$encapsulated512 = Invoke-KyberEncapsulate -PublicKey $kyber512Keys.PublicKey -ParameterSet Kyber512
$kyber512Results += Measure-Operation "Décapsulation Kyber512" {
    $null = Invoke-KyberDecapsulate -Ciphertext $encapsulated512.Ciphertext -PrivateKey $kyber512Keys.PrivateKey -ParameterSet Kyber512
} -Iterations 10

# Kyber768
Write-Host "  Kyber768..." -ForegroundColor Gray
$kyber768Results += Measure-Operation "Génération de clés Kyber768" {
    $null = New-KyberKeyPair -ParameterSet Kyber768
} -Iterations 10

$kyber768Keys = New-KyberKeyPair -ParameterSet Kyber768
$kyber768Results += Measure-Operation "Encapsulation Kyber768" {
    $null = Invoke-KyberEncapsulate -PublicKey $kyber768Keys.PublicKey -ParameterSet Kyber768
} -Iterations 10

$encapsulated768 = Invoke-KyberEncapsulate -PublicKey $kyber768Keys.PublicKey -ParameterSet Kyber768
$kyber768Results += Measure-Operation "Décapsulation Kyber768" {
    $null = Invoke-KyberDecapsulate -Ciphertext $encapsulated768.Ciphertext -PrivateKey $kyber768Keys.PrivateKey -ParameterSet Kyber768
} -Iterations 10

# Kyber1024
Write-Host "  Kyber1024..." -ForegroundColor Gray
$kyber1024Results += Measure-Operation "Génération de clés Kyber1024" {
    $null = New-KyberKeyPair -ParameterSet Kyber1024
} -Iterations 10

$kyber1024Keys = New-KyberKeyPair -ParameterSet Kyber1024
$kyber1024Results += Measure-Operation "Encapsulation Kyber1024" {
    $null = Invoke-KyberEncapsulate -PublicKey $kyber1024Keys.PublicKey -ParameterSet Kyber1024
} -Iterations 10

$encapsulated1024 = Invoke-KyberEncapsulate -PublicKey $kyber1024Keys.PublicKey -ParameterSet Kyber1024
$kyber1024Results += Measure-Operation "Décapsulation Kyber1024" {
    $null = Invoke-KyberDecapsulate -Ciphertext $encapsulated1024.Ciphertext -PrivateKey $kyber1024Keys.PrivateKey -ParameterSet Kyber1024
} -Iterations 10

# ============================================
# Benchmark Dilithium
# ============================================
Write-Host "`n=== Benchmark Dilithium (ML-DSA) ===" -ForegroundColor Cyan

# Dilithium2
Write-Host "  Dilithium2..." -ForegroundColor Gray
$dilithium2Results = @()
$dilithium2Results += Measure-Operation "Génération de clés Dilithium2" {
    $null = New-DilithiumKeyPair -ParameterSet Dilithium2
} -Iterations 10

$dilithium2Keys = New-DilithiumKeyPair -ParameterSet Dilithium2
$testData = [System.Text.Encoding]::UTF8.GetBytes("Test message for signature")
$dilithium2Results += Measure-Operation "Signature Dilithium2" {
    $null = Invoke-DilithiumSign -Data $testData -PrivateKey $dilithium2Keys.PrivateKey -ParameterSet Dilithium2
} -Iterations 10

$dilithium2Signature = Invoke-DilithiumSign -Data $testData -PrivateKey $dilithium2Keys.PrivateKey -ParameterSet Dilithium2
$dilithium2Results += Measure-Operation "Vérification Dilithium2" {
    $null = Test-DilithiumSignature -Data $testData -Signature $dilithium2Signature.Signature -PublicKey $dilithium2Keys.PublicKey -ParameterSet Dilithium2
} -Iterations 10

# Dilithium3
Write-Host "  Dilithium3..." -ForegroundColor Gray
$dilithium3Results = @()
$dilithium3Results += Measure-Operation "Génération de clés Dilithium3" {
    $null = New-DilithiumKeyPair -ParameterSet Dilithium3
} -Iterations 10

$dilithium3Keys = New-DilithiumKeyPair -ParameterSet Dilithium3
$dilithium3Results += Measure-Operation "Signature Dilithium3" {
    $null = Invoke-DilithiumSign -Data $testData -PrivateKey $dilithium3Keys.PrivateKey -ParameterSet Dilithium3
} -Iterations 10

$dilithium3Signature = Invoke-DilithiumSign -Data $testData -PrivateKey $dilithium3Keys.PrivateKey -ParameterSet Dilithium3
$dilithium3Results += Measure-Operation "Vérification Dilithium3" {
    $null = Test-DilithiumSignature -Data $testData -Signature $dilithium3Signature.Signature -PublicKey $dilithium3Keys.PublicKey -ParameterSet Dilithium3
} -Iterations 10

# Dilithium5
Write-Host "  Dilithium5..." -ForegroundColor Gray
$dilithium5Results = @()
$dilithium5Results += Measure-Operation "Génération de clés Dilithium5" {
    $null = New-DilithiumKeyPair -ParameterSet Dilithium5
} -Iterations 10

$dilithium5Keys = New-DilithiumKeyPair -ParameterSet Dilithium5
$dilithium5Results += Measure-Operation "Signature Dilithium5" {
    $null = Invoke-DilithiumSign -Data $testData -PrivateKey $dilithium5Keys.PrivateKey -ParameterSet Dilithium5
} -Iterations 10

$dilithium5Signature = Invoke-DilithiumSign -Data $testData -PrivateKey $dilithium5Keys.PrivateKey -ParameterSet Dilithium5
$dilithium5Results += Measure-Operation "Vérification Dilithium5" {
    $null = Test-DilithiumSignature -Data $testData -Signature $dilithium5Signature.Signature -PublicKey $dilithium5Keys.PublicKey -ParameterSet Dilithium5
} -Iterations 10

# ============================================
# Benchmark Ed25519
# ============================================
Write-Host "`n=== Benchmark Ed25519 ===" -ForegroundColor Cyan

$ed25519Results = @()
Write-Host "  Génération de clés Ed25519..." -ForegroundColor Gray
$ed25519Results += Measure-Operation "Génération de clés Ed25519" {
    $null = New-Ed25519KeyPair
} -Iterations 1000

Write-Host "  Signature Ed25519..." -ForegroundColor Gray
$ed25519Keys = New-Ed25519KeyPair
$ed25519Results += Measure-Operation "Signature Ed25519" {
    $null = Invoke-Ed25519Sign -Data $testData -PrivateKey $ed25519Keys.PrivateKey
} -Iterations 1000

Write-Host "  Vérification Ed25519..." -ForegroundColor Gray
$ed25519Signature = Invoke-Ed25519Sign -Data $testData -PrivateKey $ed25519Keys.PrivateKey
$ed25519Results += Measure-Operation "Vérification Ed25519" {
    $null = Test-Ed25519Signature -Data $testData -Signature $ed25519Signature.Signature -PublicKey $ed25519Keys.PublicKey
} -Iterations 1000

# ============================================
# Benchmark SHA3
# ============================================
Write-Host "`n=== Benchmark SHA3 ===" -ForegroundColor Cyan

$sha3Results = @()
Write-Host "  SHA3-256..." -ForegroundColor Gray
$sha3Results += Measure-Operation "SHA3-256" {
    $null = Get-SHA3Hash -Data $testData -Variant SHA3_256
} -Iterations 1000

Write-Host "  SHA3-384..." -ForegroundColor Gray
$sha3Results += Measure-Operation "SHA3-384" {
    $null = Get-SHA3Hash -Data $testData -Variant SHA3_384
} -Iterations 1000

# ============================================
# Benchmark Chiffrement AEAD
# ============================================
Write-Host "`n=== Benchmark Chiffrement Authenticated Encryption ===" -ForegroundColor Cyan

$aeadKeyBytes = New-Object byte[] 32
[System.Security.Cryptography.RandomNumberGenerator]::Fill($aeadKeyBytes)
# Convertir en hexadécimal pour le passage aux cmdlets PowerShell
$aeadKeyHex = ($aeadKeyBytes | ForEach-Object { $_.ToString("X2") }) -join ""

$aeadResults = @()
Write-Host "  Chiffrement ChaCha20-Poly1305..." -ForegroundColor Gray
$aeadResults += Measure-Operation "Chiffrement ChaCha20-Poly1305" {
    $null = Protect-ChaCha20Poly1305 -Plaintext $testData -Key $aeadKeyHex
} -Iterations 500

Write-Host "  Déchiffrement ChaCha20-Poly1305..." -ForegroundColor Gray
$chachaEncrypted = Protect-ChaCha20Poly1305 -Plaintext $testData -Key $aeadKeyHex
if ($chachaEncrypted -and $chachaEncrypted.Nonce) {
    $aeadResults += Measure-Operation "Déchiffrement ChaCha20-Poly1305" {
        $null = Unprotect-ChaCha20Poly1305 -CiphertextWithTag $chachaEncrypted.CiphertextWithTag -Key $aeadKeyHex -Nonce $chachaEncrypted.Nonce
    } -Iterations 500
} else {
    Write-Warning "Impossible de chiffrer avec ChaCha20-Poly1305 pour le benchmark de déchiffrement"
    $aeadResults += @{
        Operation = "Déchiffrement ChaCha20-Poly1305"
        Average = 0
        Min = 0
        Max = 0
        Iterations = 0
    }
}

Write-Host "  Chiffrement AES-GCM..." -ForegroundColor Gray
$aeadResults += Measure-Operation "Chiffrement AES-GCM" {
    $null = Protect-AESGCM -Plaintext $testData -Key $aeadKeyHex
} -Iterations 500

Write-Host "  Déchiffrement AES-GCM..." -ForegroundColor Gray
$aesEncrypted = Protect-AESGCM -Plaintext $testData -Key $aeadKeyHex
if ($aesEncrypted -and $aesEncrypted.Nonce) {
    $aeadResults += Measure-Operation "Déchiffrement AES-GCM" {
        $null = Unprotect-AESGCM -CiphertextWithTag $aesEncrypted.CiphertextWithTag -Key $aeadKeyHex -Nonce $aesEncrypted.Nonce
    } -Iterations 500
} else {
    Write-Warning "Impossible de chiffrer avec AES-GCM pour le benchmark de déchiffrement"
    $aeadResults += @{
        Operation = "Déchiffrement AES-GCM"
        Average = 0
        Min = 0
        Max = 0
        Iterations = 0
    }
}

# ============================================
# Affichage des résultats
# ============================================
Write-Host "`n=== Résultats du Benchmark ===" -ForegroundColor Cyan
Write-Host ""

# Tableau Kyber
Write-Host "KYBER (ML-KEM) - Temps moyen en ms:" -ForegroundColor Yellow
Write-Host "┌─────────────────┬──────────────┬──────────────┬──────────────┐" -ForegroundColor Gray
Write-Host "│ Opération       │ Kyber512     │ Kyber768     │ Kyber1024    │" -ForegroundColor Gray
Write-Host "├─────────────────┼──────────────┼──────────────┼──────────────┤" -ForegroundColor Gray

$opNames = @("Génération", "Encapsulation", "Décapsulation")
for ($i = 0; $i -lt 3; $i++) {
    $op = $opNames[$i]
    $k512 = $kyber512Results[$i]
    $k768 = $kyber768Results[$i]
    $k1024 = $kyber1024Results[$i]
    Write-Host "│ $($op.PadRight(15)) │ $($k512.Average.ToString().PadLeft(10)) ms │ $($k768.Average.ToString().PadLeft(10)) ms │ $($k1024.Average.ToString().PadLeft(10)) ms │" -ForegroundColor White
}
Write-Host "└─────────────────┴──────────────┴──────────────┴──────────────┘" -ForegroundColor Gray

# Tableau Dilithium
Write-Host "`nDILITHIUM (ML-DSA) - Temps moyen en ms:" -ForegroundColor Yellow
Write-Host "┌─────────────────┬──────────────┬──────────────┬──────────────┐" -ForegroundColor Gray
Write-Host "│ Opération       │ Dilithium2  │ Dilithium3  │ Dilithium5  │" -ForegroundColor Gray
Write-Host "├─────────────────┼──────────────┼──────────────┼──────────────┤" -ForegroundColor Gray

$opNames = @("Génération", "Signature", "Vérification")
for ($i = 0; $i -lt 3; $i++) {
    $op = $opNames[$i]
    $d2 = $dilithium2Results[$i]
    $d3 = $dilithium3Results[$i]
    $d5 = $dilithium5Results[$i]
    Write-Host "│ $($op.PadRight(15)) │ $($d2.Average.ToString().PadLeft(10)) ms │ $($d3.Average.ToString().PadLeft(10)) ms │ $($d5.Average.ToString().PadLeft(10)) ms │" -ForegroundColor White
}
Write-Host "└─────────────────┴──────────────┴──────────────┴──────────────┘" -ForegroundColor Gray

# Ed25519
Write-Host "`nED25519 - Métriques détaillées:" -ForegroundColor Yellow
if ($ed25519Results -and $ed25519Results.Count -gt 0) {
    foreach ($result in $ed25519Results) {
        if ($result) {
            if ($result.Status -eq "SUCCESS") {
                Write-Host "  ✅ $($result.Operation):" -ForegroundColor Green
                Write-Host "     Moyenne: $($result.AverageUs) µs ($($result.Average) ms)" -ForegroundColor White
                Write-Host "     Médiane: $($result.MedianUs) µs ($($result.Median) ms)" -ForegroundColor White
                Write-Host "     Min: $($result.MinUs) µs ($($result.Min) ms) | Max: $($result.MaxUs) µs ($($result.Max) ms)" -ForegroundColor White
                Write-Host "     Écart-type: $($result.StdDevUs) µs ($($result.StdDev) ms)" -ForegroundColor Gray
                Write-Host "     Itérations: $($result.SuccessfulIterations)/$($result.Iterations) réussies" -ForegroundColor Gray
            } elseif ($result.Status -eq "PARTIAL") {
                Write-Host "  ⚠️  $($result.Operation):" -ForegroundColor Yellow
                Write-Host "     Moyenne: $($result.AverageUs) µs ($($result.Average) ms)" -ForegroundColor White
                Write-Host "     Médiane: $($result.MedianUs) µs ($($result.Median) ms)" -ForegroundColor White
                Write-Host "     Min: $($result.MinUs) µs ($($result.Min) ms) | Max: $($result.MaxUs) µs ($($result.Max) ms)" -ForegroundColor White
                Write-Host "     Écart-type: $($result.StdDevUs) µs ($($result.StdDev) ms)" -ForegroundColor Gray
                Write-Host "     Itérations: $($result.SuccessfulIterations)/$($result.Iterations) réussies, $($result.Errors) erreur(s)" -ForegroundColor Yellow
                if ($result.ErrorMessage) {
                    Write-Host "     Erreur: $($result.ErrorMessage)" -ForegroundColor DarkYellow
                }
            } else {
                Write-Host "  ❌ $($result.Operation): ÉCHEC - $($result.Errors) erreur(s)" -ForegroundColor Red
                if ($result.ErrorMessage) {
                    Write-Host "     Erreur: $($result.ErrorMessage)" -ForegroundColor DarkRed
                }
            }
        }
    }
} else {
    Write-Host "  ❌ Aucun résultat disponible pour Ed25519" -ForegroundColor Red
}

# SHA3
Write-Host "`nSHA3 - Métriques détaillées (1000 itérations):" -ForegroundColor Yellow
if ($sha3Results -and $sha3Results.Count -gt 0) {
    foreach ($result in $sha3Results) {
        if ($result) {
            if ($result.Status -eq "SUCCESS") {
                Write-Host "  ✅ $($result.Operation):" -ForegroundColor Green
                Write-Host "     Moyenne: $($result.AverageUs) µs ($($result.Average) ms)" -ForegroundColor White
                Write-Host "     Médiane: $($result.MedianUs) µs ($($result.Median) ms)" -ForegroundColor White
                Write-Host "     Min: $($result.MinUs) µs ($($result.Min) ms) | Max: $($result.MaxUs) µs ($($result.Max) ms)" -ForegroundColor White
                Write-Host "     Écart-type: $($result.StdDevUs) µs ($($result.StdDev) ms)" -ForegroundColor Gray
                Write-Host "     Itérations: $($result.SuccessfulIterations)/$($result.Iterations) réussies" -ForegroundColor Gray
            } elseif ($result.Status -eq "PARTIAL") {
                Write-Host "  ⚠️  $($result.Operation):" -ForegroundColor Yellow
                Write-Host "     Moyenne: $($result.AverageUs) µs ($($result.Average) ms)" -ForegroundColor White
                Write-Host "     Médiane: $($result.MedianUs) µs ($($result.Median) ms)" -ForegroundColor White
                Write-Host "     Min: $($result.MinUs) µs ($($result.Min) ms) | Max: $($result.MaxUs) µs ($($result.Max) ms)" -ForegroundColor White
                Write-Host "     Écart-type: $($result.StdDevUs) µs ($($result.StdDev) ms)" -ForegroundColor Gray
                Write-Host "     Itérations: $($result.SuccessfulIterations)/$($result.Iterations) réussies, $($result.Errors) erreur(s)" -ForegroundColor Yellow
                if ($result.ErrorMessage) {
                    Write-Host "     Erreur: $($result.ErrorMessage)" -ForegroundColor DarkYellow
                }
            } else {
                Write-Host "  ❌ $($result.Operation): ÉCHEC - $($result.Errors) erreur(s)" -ForegroundColor Red
                if ($result.ErrorMessage) {
                    Write-Host "     Erreur: $($result.ErrorMessage)" -ForegroundColor DarkRed
                }
            }
        }
    }
} else {
    Write-Host "  ❌ Aucun résultat disponible pour SHA3" -ForegroundColor Red
}

# AEAD
Write-Host "`nAUTHENTICATED ENCRYPTION - Métriques détaillées:" -ForegroundColor Yellow
if ($aeadResults -and $aeadResults.Count -gt 0) {
    foreach ($result in $aeadResults) {
        if ($result) {
            # Gérer les résultats qui sont des hashtables simples (pour compatibilité)
            if ($result.ContainsKey("Status")) {
                if ($result.Status -eq "SUCCESS") {
                    Write-Host "  ✅ $($result.Operation):" -ForegroundColor Green
                    Write-Host "     Moyenne: $($result.AverageUs) µs ($($result.Average) ms)" -ForegroundColor White
                    Write-Host "     Médiane: $($result.MedianUs) µs ($($result.Median) ms)" -ForegroundColor White
                    Write-Host "     Min: $($result.MinUs) µs ($($result.Min) ms) | Max: $($result.MaxUs) µs ($($result.Max) ms)" -ForegroundColor White
                    Write-Host "     Écart-type: $($result.StdDevUs) µs ($($result.StdDev) ms)" -ForegroundColor Gray
                    Write-Host "     Itérations: $($result.SuccessfulIterations)/$($result.Iterations) réussies" -ForegroundColor Gray
                } elseif ($result.Status -eq "PARTIAL") {
                    Write-Host "  ⚠️  $($result.Operation):" -ForegroundColor Yellow
                    Write-Host "     Moyenne: $($result.AverageUs) µs ($($result.Average) ms)" -ForegroundColor White
                    Write-Host "     Médiane: $($result.MedianUs) µs ($($result.Median) ms)" -ForegroundColor White
                    Write-Host "     Min: $($result.MinUs) µs ($($result.Min) ms) | Max: $($result.MaxUs) µs ($($result.Max) ms)" -ForegroundColor White
                    Write-Host "     Écart-type: $($result.StdDevUs) µs ($($result.StdDev) ms)" -ForegroundColor Gray
                    Write-Host "     Itérations: $($result.SuccessfulIterations)/$($result.Iterations) réussies, $($result.Errors) erreur(s)" -ForegroundColor Yellow
                    if ($result.ErrorMessage) {
                        Write-Host "     Erreur: $($result.ErrorMessage)" -ForegroundColor DarkYellow
                    }
                } else {
                    Write-Host "  ❌ $($result.Operation): ÉCHEC - $($result.Errors) erreur(s)" -ForegroundColor Red
                    if ($result.ErrorMessage) {
                        Write-Host "     Erreur: $($result.ErrorMessage)" -ForegroundColor DarkRed
                    }
                }
            } else {
                # Format ancien (hashtable simple)
                if ($result.Average -and $result.Average -gt 0) {
                    Write-Host "  ✅ $($result.Operation): $($result.Average) ms (min: $($result.Min), max: $($result.Max))" -ForegroundColor Green
                } else {
                    Write-Host "  ❌ $($result.Operation): Aucune donnée (0 ms)" -ForegroundColor Red
                }
            }
        }
    }
} else {
    Write-Host "  ❌ Aucun résultat disponible pour Authenticated Encryption" -ForegroundColor Red
}

# ============================================
# Résumé final avec statistiques exploitables
# ============================================
Write-Host "`n=== Résumé Final ===" -ForegroundColor Cyan
Write-Host ""

$totalOperations = 0
$successfulOperations = 0
$partialOperations = 0
$failedOperations = 0

# Compter les opérations
$allResults = @()
$allResults += $kyber512Results
$allResults += $kyber768Results
$allResults += $kyber1024Results
$allResults += $dilithium2Results
$allResults += $dilithium3Results
$allResults += $dilithium5Results
$allResults += $ed25519Results
$allResults += $sha3Results
$allResults += $aeadResults

foreach ($result in $allResults) {
    if ($result) {
        $totalOperations++
        if ($result.ContainsKey("Status")) {
            if ($result.Status -eq "SUCCESS") {
                $successfulOperations++
            } elseif ($result.Status -eq "PARTIAL") {
                $partialOperations++
            } else {
                $failedOperations++
            }
        } elseif ($result.Average -and $result.Average -gt 0) {
            $successfulOperations++
        } else {
            $failedOperations++
        }
    }
}

Write-Host "Statistiques globales:" -ForegroundColor Yellow
Write-Host "  Total d'opérations testées: $totalOperations" -ForegroundColor White
Write-Host "  ✅ Réussies: $successfulOperations" -ForegroundColor Green
Write-Host "  ⚠️  Partielles: $partialOperations" -ForegroundColor Yellow
Write-Host "  ❌ Échouées: $failedOperations" -ForegroundColor Red
Write-Host ""

# Export des données exploitables (format CSV-like pour analyse)
Write-Host "Données exploitables (format CSV):" -ForegroundColor Yellow
Write-Host "Operation,Status,Average_ms,Average_us,Median_ms,Median_us,Min_ms,Min_us,Max_ms,Max_us,StdDev_ms,StdDev_us,SuccessfulIterations,TotalIterations,Errors" -ForegroundColor Gray

foreach ($result in $allResults) {
    if ($result) {
        $opName = $result.Operation -replace ",", ";"
        if ($result.ContainsKey("Status")) {
            $status = $result.Status
            $avgMs = $result.Average
            $avgUs = if ($result.AverageUs) { $result.AverageUs } else { $result.Average * 1000 }
            $medianMs = if ($result.Median) { $result.Median } else { $result.Average }
            $medianUs = if ($result.MedianUs) { $result.MedianUs } else { $medianMs * 1000 }
            $minMs = $result.Min
            $minUs = if ($result.MinUs) { $result.MinUs } else { $result.Min * 1000 }
            $maxMs = $result.Max
            $maxUs = if ($result.MaxUs) { $result.MaxUs } else { $result.Max * 1000 }
            $stdDevMs = if ($result.StdDev) { $result.StdDev } else { 0 }
            $stdDevUs = if ($result.StdDevUs) { $result.StdDevUs } else { $stdDevMs * 1000 }
            $success = $result.SuccessfulIterations
            $total = $result.Iterations
            $errors = $result.Errors
        } else {
            $status = if ($result.Average -and $result.Average -gt 0) { "SUCCESS" } else { "FAILED" }
            $avgMs = if ($result.Average) { $result.Average } else { 0 }
            $avgUs = $avgMs * 1000
            $medianMs = $avgMs
            $medianUs = $avgUs
            $minMs = if ($result.Min) { $result.Min } else { 0 }
            $minUs = $minMs * 1000
            $maxMs = if ($result.Max) { $result.Max } else { 0 }
            $maxUs = $maxMs * 1000
            $stdDevMs = 0
            $stdDevUs = 0
            $success = if ($result.Iterations) { $result.Iterations } else { 0 }
            $total = if ($result.Iterations) { $result.Iterations } else { 0 }
            $errors = 0
        }
        Write-Host "$opName,$status,$avgMs,$avgUs,$medianMs,$medianUs,$minMs,$minUs,$maxMs,$maxUs,$stdDevMs,$stdDevUs,$success,$total,$errors" -ForegroundColor White
    }
}

Write-Host "`n=== Benchmark terminé ===" -ForegroundColor Cyan


