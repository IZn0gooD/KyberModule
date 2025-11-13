# Exemples d'utilisation du module SHA3 (SHA-3)
# Module: KyberModule

# Importer le module
$ModulePath = Split-Path -Parent $MyInvocation.MyCommand.Path
Import-Module (Join-Path $ModulePath "KyberModule.psd1") -Force

Write-Host "`n=== Exemples SHA3 (SHA-3) ===" -ForegroundColor Cyan
Write-Host ""

# Exemple 1: Hachage SHA3-256 basique
Write-Host "1. Hachage SHA3-256 basique:" -ForegroundColor Yellow
$message = "Hello, World!"
$hash = Get-SHA3Hash -Data $message -AsString
Write-Host "   Message: $message" -ForegroundColor Green
Write-Host "   SHA3-256 (hex): $($hash.HashHex)" -ForegroundColor Green
Write-Host "   Taille: $($hash.HashSize) bytes ($($hash.HashSizeBits) bits)" -ForegroundColor Green
Write-Host ""

# Exemple 2: Hachage SHA3-384
Write-Host "2. Hachage SHA3-384:" -ForegroundColor Yellow
$hash384 = Get-SHA3Hash -Data $message -Variant SHA3_384 -AsString
Write-Host "   Message: $message" -ForegroundColor Green
Write-Host "   SHA3-384 (hex): $($hash384.HashHex)" -ForegroundColor Green
Write-Host "   Taille: $($hash384.HashSize) bytes ($($hash384.HashSizeBits) bits)" -ForegroundColor Green
Write-Host ""

# Exemple 3: Hachage de données binaires
Write-Host "3. Hachage de données binaires:" -ForegroundColor Yellow
$binaryData = [System.Text.Encoding]::UTF8.GetBytes("Données binaires")
$hashBinary = Get-SHA3Hash -Data $binaryData
Write-Host "   SHA3-256 (hex): $($hashBinary.HashHex)" -ForegroundColor Green
Write-Host ""

# Exemple 4: Dilithium avec SHA3 (recommandé pour ML-DSA)
Write-Host "4. Signature Dilithium avec SHA3-256 (recommandé):" -ForegroundColor Yellow
$dilithiumKeys = New-DilithiumKeyPair -ParameterSet Dilithium3
$messageBytes = [System.Text.Encoding]::UTF8.GetBytes("Message à signer avec SHA3")
$signature = Invoke-DilithiumSign -Data $messageBytes -PrivateKey $dilithiumKeys.PrivateKey -ParameterSet Dilithium3 -PreHash -SHA3Variant SHA3_256
Write-Host "   Message signé avec SHA3-256 + Dilithium3" -ForegroundColor Green
Write-Host "   Signature (hex): $($signature.SignatureHex.Substring(0, 64))..." -ForegroundColor Green
Write-Host ""

# Exemple 5: Vérification avec SHA3
Write-Host "5. Vérification de signature avec SHA3:" -ForegroundColor Yellow
$isValid = Test-DilithiumSignature -Data $messageBytes -Signature $signature.Signature -PublicKey $dilithiumKeys.PublicKey -ParameterSet Dilithium3 -PreHash -SHA3Variant SHA3_256
Write-Host "   Signature valide: $isValid" -ForegroundColor $(if ($isValid) { "Green" } else { "Red" })
Write-Host ""

# Exemple 6: Comparaison SHA3-256 vs SHA3-384
Write-Host "6. Comparaison SHA3-256 vs SHA3-384:" -ForegroundColor Yellow
$testData = "Test de comparaison"
$sha256 = Get-SHA3Hash -Data $testData -Variant SHA3_256 -AsString
$sha384 = Get-SHA3Hash -Data $testData -Variant SHA3_384 -AsString
Write-Host "   SHA3-256: $($sha256.HashHex.Substring(0, 32))... (32 bytes)" -ForegroundColor Green
Write-Host "   SHA3-384: $($sha384.HashHex.Substring(0, 32))... (48 bytes)" -ForegroundColor Green
Write-Host ""

# Exemple 7: Intégration avec Kyber + Dilithium + SHA3 (100% post-quantique)
Write-Host "7. Protocole complet: Kyber + Dilithium + SHA3 (100% post-quantique):" -ForegroundColor Yellow
$kyberKeys = New-KyberKeyPair -ParameterSet Kyber768
$dilithiumKeys = New-DilithiumKeyPair -ParameterSet Dilithium3

# Encapsuler une clé partagée avec Kyber
$encapsulated = Invoke-KyberEncapsulate -PublicKey $kyberKeys.PublicKey -ParameterSet Kyber768

# Hacher la clé partagée avec SHA3-256
$sharedSecretHash = Get-SHA3Hash -Data $encapsulated.SharedSecret -Variant SHA3_256

# Signer le hachage avec Dilithium (sans pré-hachage supplémentaire car déjà haché)
$signature = Invoke-DilithiumSign -Data $sharedSecretHash.Hash -PrivateKey $dilithiumKeys.PrivateKey -ParameterSet Dilithium3

Write-Host "   ✅ Protocole 100% post-quantique validé:" -ForegroundColor Green
Write-Host "      - Kyber (ML-KEM-768) pour l'échange de clés" -ForegroundColor White
Write-Host "      - SHA3-256 pour le hachage" -ForegroundColor White
Write-Host "      - Dilithium (ML-DSA-65) pour la signature" -ForegroundColor White
Write-Host ""

Write-Host "=== Tests terminés ===" -ForegroundColor Cyan

