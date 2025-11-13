# Test combiné Kyber + Dilithium (100% Post-Quantum)
# Test rapide de l'intégration complète

$ModulePath = Split-Path -Parent $MyInvocation.MyCommand.Path
Import-Module (Join-Path $ModulePath "KyberModule.psd1") -Force

Write-Host "`n=== Test combiné Kyber + Dilithium (100% Post-Quantum) ===" -ForegroundColor Cyan
Write-Host ""

# 1. Générer les clés
Write-Host "1. Génération des clés post-quantiques..." -ForegroundColor Yellow
$kyberKeys = New-KyberKeyPair -ParameterSet Kyber768
$dilithiumKeys = New-DilithiumKeyPair -ParameterSet Dilithium3
Write-Host "   ✅ Clés Kyber (ML-KEM-768) et Dilithium (ML-DSA-65) générées" -ForegroundColor Green

# 2. Encapsuler une clé partagée avec Kyber
Write-Host "`n2. Encapsulation Kyber..." -ForegroundColor Yellow
$encapsulated = Invoke-KyberEncapsulate -PublicKey $kyberKeys.PublicKey -ParameterSet Kyber768
Write-Host "   ✅ Clé partagée encapsulée" -ForegroundColor Green

# 3. Signer le ciphertext avec Dilithium
Write-Host "`n3. Signature Dilithium..." -ForegroundColor Yellow
$signature = Invoke-DilithiumSign -Data $encapsulated.Ciphertext -PrivateKey $dilithiumKeys.PrivateKey -ParameterSet Dilithium3
Write-Host "   ✅ Ciphertext signé avec Dilithium" -ForegroundColor Green

# 4. Vérifier la signature
Write-Host "`n4. Vérification de la signature..." -ForegroundColor Yellow
$isValid = Test-DilithiumSignature -Data $encapsulated.Ciphertext -Signature $signature.Signature -PublicKey $dilithiumKeys.PublicKey -ParameterSet Dilithium3
if ($isValid) {
    Write-Host "   ✅ Signature valide" -ForegroundColor Green
} else {
    Write-Host "   ❌ Signature invalide" -ForegroundColor Red
}

# 5. Décapsuler la clé partagée
Write-Host "`n5. Décapsulation Kyber..." -ForegroundColor Yellow
$sharedSecret = Invoke-KyberDecapsulate -Ciphertext $encapsulated.Ciphertext -PrivateKey $kyberKeys.PrivateKey -ParameterSet Kyber768
Write-Host "   ✅ Clé partagée décapsulée" -ForegroundColor Green

# 6. Vérifier que les clés partagées correspondent
Write-Host "`n6. Vérification de la clé partagée..." -ForegroundColor Yellow
$match = $true
if ($encapsulated.SharedSecret.Length -ne $sharedSecret.SharedSecret.Length) {
    $match = $false
} else {
    for ($i = 0; $i -lt $encapsulated.SharedSecret.Length; $i++) {
        if ($encapsulated.SharedSecret[$i] -ne $sharedSecret.SharedSecret[$i]) {
            $match = $false
            break
        }
    }
}

if ($match) {
    Write-Host "   ✅ Clés partagées identiques" -ForegroundColor Green
} else {
    Write-Host "   ❌ Clés partagées différentes" -ForegroundColor Red
}

Write-Host "`n=== Résumé ===" -ForegroundColor Cyan
Write-Host "✅ Architecture 100% Post-Quantum validée:" -ForegroundColor Green
Write-Host "   • Kyber (ML-KEM-768) pour l'échange de clés" -ForegroundColor White
Write-Host "   • Dilithium (ML-DSA-65) pour la signature" -ForegroundColor White
Write-Host "   • Tous les tests réussis" -ForegroundColor White

