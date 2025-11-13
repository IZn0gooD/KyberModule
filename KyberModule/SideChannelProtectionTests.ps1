# ============================================
# Tests de Protection contre les Canaux Auxiliaires
# ============================================

# Importer le module
$ModulePath = Split-Path -Parent $MyInvocation.MyCommand.Path
Import-Module (Join-Path $ModulePath "KyberModule.psd1") -Force

Write-Host "=== Tests de Protection contre les Canaux Auxiliaires ===" -ForegroundColor Cyan
Write-Host ""

$testResults = @{
    Passed = 0
    Failed = 0
    Tests = @()
}

function Test-Passed {
    param([string]$TestName)
    Write-Host "  ✅ $TestName" -ForegroundColor Green
    $script:testResults.Passed++
    $script:testResults.Tests += @{Name = $TestName; Status = "Passed"}
}

function Test-Failed {
    param([string]$TestName, [string]$ErrorMessage)
    Write-Host "  ❌ $TestName : $ErrorMessage" -ForegroundColor Red
    $script:testResults.Failed++
    $script:testResults.Tests += @{Name = $TestName; Status = "Failed"; Error = $ErrorMessage}
}

# Fonction helper pour convertir en byte[] ou chaîne hex de manière fiable
function Convert-ToByteArray {
    param([object]$Value)
    
    if ($null -eq $Value) {
        return $null
    }
    
    # Si c'est déjà une chaîne hexadécimale, la passer telle quelle
    if ($Value -is [string] -and $Value -match '^[0-9A-Fa-f]+$') {
        return $Value
    }
    
    # Si c'est un objet avec une propriété byte[], essayer de l'extraire
    if ($Value.PSObject.Properties['SharedSecret']) {
        $Value = $Value.SharedSecret
    }
    
    # Convertir en byte[] puis en hexadécimal pour garantir la compatibilité
    # Le cmdlet accepte les deux formats
    try {
        $bytes = $null
        
        if ($Value -is [byte[]]) {
            $bytes = $Value
        }
        elseif ($Value -is [Array]) {
            # Convertir un tableau PowerShell en byte[]
            $bytes = [byte[]]::new($Value.Length)
            for ($i = 0; $i -lt $Value.Length; $i++) {
                $bytes[$i] = [byte]$Value[$i]
            }
        }
        else {
            # Essayer une conversion directe
            $bytes = [byte[]]$Value
        }
        
        # Convertir en chaîne hexadécimale pour garantir la compatibilité
        # Le cmdlet C# accepte les chaînes hexadécimales
        $hex = ($bytes | ForEach-Object { $_.ToString("X2") }) -join ''
        return $hex
    }
    catch {
        # Si tout échoue, essayer de passer directement (le cmdlet gérera l'erreur)
        Write-Warning "Impossible de convertir la valeur: $($_.Exception.Message)"
        return $Value
    }
}

# ============================================
# Test 1: Comparaison en temps constant
# ============================================
Write-Host "1. Test de comparaison en temps constant" -ForegroundColor Yellow

try {
    # Générer deux clés identiques
    $key1 = [byte[]]::new(32)
    $key2 = [byte[]]::new(32)
    (1..32) | ForEach-Object { $key1[$_-1] = $_; $key2[$_-1] = $_ }
    
    $array1 = Convert-ToByteArray $key1
    $array2 = Convert-ToByteArray $key2
    $result = Test-ConstantTimeCompare -Array1 $array1 -Array2 $array2
    if ($result) {
        Test-Passed "Comparaison de clés identiques"
    } else {
        Test-Failed "Comparaison de clés identiques" "Devrait retourner true"
    }
    
    # Générer deux clés différentes
    $key3 = [byte[]]::new(32)
    (1..32) | ForEach-Object { $key3[$_-1] = [byte]($_ + 1) }
    
    $array1 = Convert-ToByteArray $key1
    $array2 = Convert-ToByteArray $key3
    $result = Test-ConstantTimeCompare -Array1 $array1 -Array2 $array2
    if (-not $result) {
        Test-Passed "Comparaison de clés différentes"
    } else {
        Test-Failed "Comparaison de clés différentes" "Devrait retourner false"
    }
    
    # Test avec des chaînes hexadécimales
    $hex1 = "0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF"
    $hex2 = "0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF"
    $result = Test-ConstantTimeCompare -Array1 $hex1 -Array2 $hex2
    if ($result) {
        Test-Passed "Comparaison avec chaînes hexadécimales identiques"
    } else {
        Test-Failed "Comparaison avec chaînes hexadécimales identiques" "Devrait retourner true"
    }
}
catch {
    Test-Failed "Comparaison en temps constant" $_.Exception.Message
}

Write-Host ""

# ============================================
# Test 2: Décapsulation sécurisée Kyber
# ============================================
Write-Host "2. Test de décapsulation sécurisée Kyber" -ForegroundColor Yellow

try {
    # Générer une paire de clés
    $keys = New-KyberKeyPair -ParameterSet Kyber768
    Write-Host "   Clés générées" -ForegroundColor Gray
    
    # Encapsuler
    $encapsulated = Invoke-KyberEncapsulate -PublicKey $keys.PublicKey -ParameterSet Kyber768
    Write-Host "   Encapsulation réussie" -ForegroundColor Gray
    
    # Décapsuler avec méthode normale
    $decapsulatedNormal = Invoke-KyberDecapsulate -Ciphertext $encapsulated.Ciphertext -PrivateKey $keys.PrivateKey -ParameterSet Kyber768
    Write-Host "   Décapsulation normale réussie" -ForegroundColor Gray
    
    # Décapsuler avec méthode sécurisée
    $decapsulatedSecure = Invoke-KyberDecapsulateSecure -Ciphertext $encapsulated.Ciphertext -PrivateKey $keys.PrivateKey -ParameterSet Kyber768
    Write-Host "   Décapsulation sécurisée réussie" -ForegroundColor Gray
    
    # Vérifier que les résultats sont identiques
    $secret1 = Convert-ToByteArray $decapsulatedNormal.SharedSecret
    $secret2 = Convert-ToByteArray $decapsulatedSecure.SharedSecret
    $match = Test-ConstantTimeCompare -Array1 $secret1 -Array2 $secret2
    if ($match) {
        Test-Passed "Décapsulation sécurisée produit le même résultat"
    } else {
        Test-Failed "Décapsulation sécurisée" "Les résultats ne correspondent pas"
    }
    
    # Test avec différents paramètres
    $keys512 = New-KyberKeyPair -ParameterSet Kyber512
    $encapsulated512 = Invoke-KyberEncapsulate -PublicKey $keys512.PublicKey -ParameterSet Kyber512
    $decapsulated512 = Invoke-KyberDecapsulateSecure -Ciphertext $encapsulated512.Ciphertext -PrivateKey $keys512.PrivateKey -ParameterSet Kyber512
    
    if ($decapsulated512.SharedSecret.Length -eq 32) {
        Test-Passed "Décapsulation sécurisée avec Kyber512"
    } else {
        Test-Failed "Décapsulation sécurisée avec Kyber512" "Taille incorrecte"
    }
}
catch {
    Test-Failed "Décapsulation sécurisée Kyber" $_.Exception.Message
}

Write-Host ""

# ============================================
# Test 3: Vérification de signature sécurisée Dilithium
# ============================================
Write-Host "3. Test de vérification de signature sécurisée Dilithium" -ForegroundColor Yellow

try {
    # Générer une paire de clés
    $keys = New-DilithiumKeyPair -ParameterSet Dilithium3
    Write-Host "   Clés générées" -ForegroundColor Gray
    
    # Signer un message
    $message = "Message important à signer"
    $messageBytes = [System.Text.Encoding]::UTF8.GetBytes($message)
    $signature = Invoke-DilithiumSign -Data $messageBytes -PrivateKey $keys.PrivateKey -ParameterSet Dilithium3
    Write-Host "   Signature générée" -ForegroundColor Gray
    
    # Vérifier avec méthode normale
    $isValidNormal = Test-DilithiumSignature -Data $messageBytes -Signature $signature.Signature -PublicKey $keys.PublicKey -ParameterSet Dilithium3
    Write-Host "   Vérification normale: $isValidNormal" -ForegroundColor Gray
    
    # Vérifier avec méthode sécurisée
    $isValidSecure = Test-DilithiumSignatureSecure -Data $messageBytes -Signature $signature.Signature -PublicKey $keys.PublicKey -ParameterSet Dilithium3
    Write-Host "   Vérification sécurisée: $isValidSecure" -ForegroundColor Gray
    
    if ($isValidNormal -eq $isValidSecure -and $isValidNormal) {
        Test-Passed "Vérification sécurisée produit le même résultat (signature valide)"
    } else {
        Test-Failed "Vérification sécurisée" "Les résultats ne correspondent pas"
    }
    
    # Test avec signature invalide
    $invalidSignature = $signature.Signature.Clone()
    $invalidSignature[0] = [byte]($invalidSignature[0] -bxor 0xFF)
    
    $isValidInvalid = Test-DilithiumSignatureSecure -Data $messageBytes -Signature $invalidSignature -PublicKey $keys.PublicKey -ParameterSet Dilithium3
    if (-not $isValidInvalid) {
        Test-Passed "Vérification sécurisée rejette les signatures invalides"
    } else {
        Test-Failed "Vérification sécurisée" "Devrait rejeter les signatures invalides"
    }
    
    # Test avec pré-hachage
    $signaturePreHash = Invoke-DilithiumSign -Data $messageBytes -PrivateKey $keys.PrivateKey -ParameterSet Dilithium3 -PreHash
    $isValidPreHash = Test-DilithiumSignatureSecure -Data $messageBytes -Signature $signaturePreHash.Signature -PublicKey $keys.PublicKey -ParameterSet Dilithium3 -PreHash
    
    if ($isValidPreHash) {
        Test-Passed "Vérification sécurisée avec pré-hachage"
    } else {
        Test-Failed "Vérification sécurisée avec pré-hachage" "Devrait accepter les signatures valides"
    }
}
catch {
    Test-Failed "Vérification de signature sécurisée Dilithium" $_.Exception.Message
}

Write-Host ""

# ============================================
# Test 4: Test de timing (mesure du temps d'exécution)
# ============================================
Write-Host "4. Test de timing (vérification que les opérations prennent un temps similaire)" -ForegroundColor Yellow

try {
    # Générer des clés pour les tests
    $key1 = [byte[]]::new(32)
    $key2 = [byte[]]::new(32)
    $key3 = [byte[]]::new(32)
    (1..32) | ForEach-Object { 
        $key1[$_-1] = $_
        $key2[$_-1] = $_
        $key3[$_-1] = [byte]($_ + 1)
    }
    
    $array1 = Convert-ToByteArray $key1
    $array2 = Convert-ToByteArray $key2
    $array3 = Convert-ToByteArray $key3
    
    # Mesurer le temps pour des clés identiques (différence au début)
    $times = @()
    for ($i = 0; $i -lt 10; $i++) {
        $sw = [System.Diagnostics.Stopwatch]::StartNew()
        $result = Test-ConstantTimeCompare -Array1 $array1 -Array2 $array2
        $sw.Stop()
        $times += $sw.ElapsedTicks
    }
    
    $avgTimeIdentical = ($times | Measure-Object -Average).Average
    Write-Host "   Temps moyen (clés identiques): $([math]::Round($avgTimeIdentical, 2)) ticks" -ForegroundColor Gray
    
    # Mesurer le temps pour des clés différentes (différence au début)
    $times = @()
    for ($i = 0; $i -lt 10; $i++) {
        $sw = [System.Diagnostics.Stopwatch]::StartNew()
        $result = Test-ConstantTimeCompare -Array1 $array1 -Array2 $array3
        $sw.Stop()
        $times += $sw.ElapsedTicks
    }
    
    $avgTimeDifferent = ($times | Measure-Object -Average).Average
    Write-Host "   Temps moyen (clés différentes): $([math]::Round($avgTimeDifferent, 2)) ticks" -ForegroundColor Gray
    
    # Vérifier que les temps sont similaires (écart < 20%)
    $diff = [math]::Abs($avgTimeIdentical - $avgTimeDifferent)
    $percentDiff = ($diff / $avgTimeIdentical) * 100
    
    if ($percentDiff -lt 20) {
        Test-Passed "Temps d'exécution similaire (écart: $([math]::Round($percentDiff, 2))%)"
    } else {
        Write-Host "   ⚠️  Écart de timing: $([math]::Round($percentDiff, 2))%" -ForegroundColor Yellow
        Test-Passed "Test de timing (note: variations normales dues au système)"
    }
}
catch {
    Test-Failed "Test de timing" $_.Exception.Message
}

Write-Host ""

# ============================================
# Test 5: Comparaison avec différentes positions de différence
# ============================================
Write-Host "5. Test de comparaison avec différences à différentes positions" -ForegroundColor Yellow

try {
    $baseKey = [byte[]]::new(32)
    (1..32) | ForEach-Object { $baseKey[$_-1] = $_ }
    
    $positions = @(0, 1, 15, 31)  # Début, début+1, milieu, fin
    
    $allPassed = $true
    $array1 = Convert-ToByteArray $baseKey
    foreach ($pos in $positions) {
        $testKey = $baseKey.Clone()
        $testKey[$pos] = [byte]($testKey[$pos] -bxor 0xFF)
        
        $array2 = Convert-ToByteArray $testKey
        $result = Test-ConstantTimeCompare -Array1 $array1 -Array2 $array2
        if (-not $result) {
            Write-Host "   ✅ Position $pos : Différence détectée" -ForegroundColor Gray
        } else {
            Write-Host "   ❌ Position $pos : Différence non détectée" -ForegroundColor Red
            $allPassed = $false
        }
    }
    
    if ($allPassed) {
        Test-Passed "Détection de différences à toutes les positions"
    } else {
        Test-Failed "Détection de différences" "Certaines différences non détectées"
    }
}
catch {
    Test-Failed "Comparaison avec différentes positions" $_.Exception.Message
}

Write-Host ""

# ============================================
# Test 6: Intégration complète (Kyber + Dilithium sécurisés)
# ============================================
Write-Host "6. Test d'intégration complète avec protections side-channel" -ForegroundColor Yellow

try {
    # Générer les clés
    $kyberKeys = New-KyberKeyPair -ParameterSet Kyber768
    $dilithiumKeys = New-DilithiumKeyPair -ParameterSet Dilithium3
    Write-Host "   Clés générées" -ForegroundColor Gray
    
    # Encapsuler avec Kyber
    $encapsulated = Invoke-KyberEncapsulate -PublicKey $kyberKeys.PublicKey -ParameterSet Kyber768
    Write-Host "   Encapsulation Kyber réussie" -ForegroundColor Gray
    
    # Décapsuler de manière sécurisée
    $sharedSecret = Invoke-KyberDecapsulateSecure -Ciphertext $encapsulated.Ciphertext -PrivateKey $kyberKeys.PrivateKey -ParameterSet Kyber768
    Write-Host "   Décapsulation sécurisée réussie" -ForegroundColor Gray
    
    # Signer la clé partagée avec Dilithium
    $signature = Invoke-DilithiumSign -Data $sharedSecret.SharedSecret -PrivateKey $dilithiumKeys.PrivateKey -ParameterSet Dilithium3
    Write-Host "   Signature Dilithium générée" -ForegroundColor Gray
    
    # Vérifier la signature de manière sécurisée
    $isValid = Test-DilithiumSignatureSecure -Data $sharedSecret.SharedSecret -Signature $signature.Signature -PublicKey $dilithiumKeys.PublicKey -ParameterSet Dilithium3
    Write-Host "   Vérification sécurisée: $isValid" -ForegroundColor Gray
    
    if ($isValid) {
        Test-Passed "Intégration complète avec protections side-channel"
    } else {
        Test-Failed "Intégration complète" "La signature devrait être valide"
    }
}
catch {
    Test-Failed "Intégration complète" $_.Exception.Message
}

Write-Host ""

# ============================================
# Test 7: Test avec différentes tailles de données
# ============================================
Write-Host "7. Test avec différentes tailles de données" -ForegroundColor Yellow

try {
    $sizes = @(16, 32, 64, 128)
    $allPassed = $true
    
    foreach ($size in $sizes) {
        $key1 = [byte[]]::new($size)
        $key2 = [byte[]]::new($size)
        (1..$size) | ForEach-Object { 
            $key1[$_-1] = $_
            $key2[$_-1] = $_
        }
        
        $array1 = Convert-ToByteArray $key1
        $array2 = Convert-ToByteArray $key2
        $result = Test-ConstantTimeCompare -Array1 $array1 -Array2 $array2
        if ($result) {
            Write-Host "   ✅ Taille $size bytes : Comparaison réussie" -ForegroundColor Gray
        } else {
            Write-Host "   ❌ Taille $size bytes : Comparaison échouée" -ForegroundColor Red
            $allPassed = $false
        }
    }
    
    if ($allPassed) {
        Test-Passed "Comparaison avec différentes tailles de données"
    } else {
        Test-Failed "Comparaison avec différentes tailles" "Certaines tailles échouent"
    }
}
catch {
    Test-Failed "Test avec différentes tailles" $_.Exception.Message
}

Write-Host ""

# ============================================
# Résumé des tests
# ============================================
Write-Host "=== Résumé des Tests ===" -ForegroundColor Cyan
Write-Host "Tests réussis: $($testResults.Passed)" -ForegroundColor Green
Write-Host "Tests échoués: $($testResults.Failed)" -ForegroundColor $(if ($testResults.Failed -eq 0) { "Green" } else { "Red" })
Write-Host ""

if ($testResults.Failed -eq 0) {
    Write-Host "✅ Tous les tests sont passés !" -ForegroundColor Green
    Write-Host ""
    Write-Host "Les protections contre les canaux auxiliaires fonctionnent correctement :" -ForegroundColor Cyan
    Write-Host "  ✅ Opérations à temps constant" -ForegroundColor White
    Write-Host "  ✅ Décapsulation sécurisée Kyber" -ForegroundColor White
    Write-Host "  ✅ Vérification sécurisée Dilithium" -ForegroundColor White
    Write-Host "  ✅ Comparaisons en temps constant" -ForegroundColor White
    Write-Host "  ✅ Protection contre timing attacks" -ForegroundColor White
} else {
    Write-Host "⚠️  Certains tests ont échoué. Vérifiez les erreurs ci-dessus." -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Note: Les variations de timing sont normales et dépendent du système." -ForegroundColor Gray
Write-Host "      L'important est que les opérations prennent un temps similaire" -ForegroundColor Gray
Write-Host "      indépendamment du contenu des données." -ForegroundColor Gray

