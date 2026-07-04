# Exemples d'utilisation du chiffrement symétrique authentifié
# Module: KyberModule
# Supports: ChaCha20-Poly1305 et AES-GCM

# Importer le module
$ModulePath = Split-Path -Parent $MyInvocation.MyCommand.Path
Import-Module (Join-Path $ModulePath "KyberModule.psd1") -Force

Write-Host "`n=== Exemples de Chiffrement Symétrique Authentifié ===" -ForegroundColor Cyan
Write-Host ""

# ============================================
# ChaCha20-Poly1305
# ============================================
Write-Host "=== ChaCha20-Poly1305 ===" -ForegroundColor Yellow

# Exemple 1: Chiffrement basique
Write-Host "`n1. Chiffrement basique ChaCha20-Poly1305:" -ForegroundColor Cyan
$message = "Message secret à chiffrer"
$encrypted = Protect-ChaCha20Poly1305 -Plaintext $message
Write-Host "   Message original: $message" -ForegroundColor Green
Write-Host "   Ciphertext (hex): $($encrypted.CiphertextWithTagHex.Substring(0, 64))..." -ForegroundColor Green
Write-Host "   Clé générée (hex): $($encrypted.KeyHex.Substring(0, 32))..." -ForegroundColor Green
Write-Host "   Nonce généré (hex): $($encrypted.NonceHex)" -ForegroundColor Green
Write-Host "   Taille plaintext: $($encrypted.PlaintextLength) bytes" -ForegroundColor Gray
Write-Host "   Taille ciphertext (avec tag): $($encrypted.CiphertextLength) bytes" -ForegroundColor Gray

# Déchiffrement
Write-Host "`n2. Déchiffrement ChaCha20-Poly1305:" -ForegroundColor Cyan
$decrypted = Unprotect-ChaCha20Poly1305 -CiphertextWithTag $encrypted.CiphertextWithTag -Key $encrypted.Key -Nonce $encrypted.Nonce
$decryptedText = [System.Text.Encoding]::UTF8.GetString($decrypted)
Write-Host "   Message déchiffré: $decryptedText" -ForegroundColor Green
Write-Host "   ✅ Déchiffrement réussi!" -ForegroundColor Green

# Exemple 3: Avec données associées (AAD)
Write-Host "`n3. Chiffrement avec données associées (AAD):" -ForegroundColor Cyan
$plaintext = "Données sensibles"
$aad = "Métadonnées importantes"
$encryptedWithAAD = Protect-ChaCha20Poly1305 -Plaintext $plaintext -AssociatedData $aad
Write-Host "   Plaintext: $plaintext" -ForegroundColor Green
Write-Host "   AAD: $aad" -ForegroundColor Green
Write-Host "   ✅ Chiffré avec AAD" -ForegroundColor Green

$decryptedWithAAD = Unprotect-ChaCha20Poly1305 -CiphertextWithTag $encryptedWithAAD.CiphertextWithTag -Key $encryptedWithAAD.Key -Nonce $encryptedWithAAD.Nonce -AssociatedData $aad
$decryptedTextAAD = [System.Text.Encoding]::UTF8.GetString($decryptedWithAAD)
Write-Host "   ✅ Déchiffré avec AAD correct: $decryptedTextAAD" -ForegroundColor Green

# ============================================
# AES-GCM
# ============================================
Write-Host "`n=== AES-GCM ===" -ForegroundColor Yellow

# Exemple 4: AES-GCM basique (AES-256 par défaut)
Write-Host "`n4. Chiffrement AES-GCM (AES-256):" -ForegroundColor Cyan
$messageAES = "Message chiffré avec AES-GCM"
$encryptedAES = Protect-AESGCM -Plaintext $messageAES -KeySize AES256
Write-Host "   Message original: $messageAES" -ForegroundColor Green
Write-Host "   Ciphertext (hex): $($encryptedAES.CiphertextWithTagHex.Substring(0, 64))..." -ForegroundColor Green
Write-Host "   Clé générée (hex): $($encryptedAES.KeyHex.Substring(0, 32))..." -ForegroundColor Green
Write-Host "   Taille clé: $($encryptedAES.KeySize) bits" -ForegroundColor Gray

# Déchiffrement AES-GCM
Write-Host "`n5. Déchiffrement AES-GCM:" -ForegroundColor Cyan
$decryptedAES = Unprotect-AESGCM -CiphertextWithTag $encryptedAES.CiphertextWithTag -Key $encryptedAES.Key -Nonce $encryptedAES.Nonce
$decryptedTextAES = [System.Text.Encoding]::UTF8.GetString($decryptedAES)
Write-Host "   Message déchiffré: $decryptedTextAES" -ForegroundColor Green
Write-Host "   ✅ Déchiffrement réussi!" -ForegroundColor Green

# Exemple 6: AES-128
Write-Host "`n6. Chiffrement AES-GCM (AES-128):" -ForegroundColor Cyan
$encryptedAES128 = Protect-AESGCM -Plaintext $messageAES -KeySize AES128
Write-Host "   Taille clé: $($encryptedAES128.KeySize) bits" -ForegroundColor Green
Write-Host "   ✅ Chiffrement AES-128 réussi" -ForegroundColor Green

# ============================================
# Intégration avec Kyber (scénario complet)
# ============================================
Write-Host "`n=== Scénario Complet: Kyber + Chiffrement Symétrique ===" -ForegroundColor Yellow

Write-Host "`n7. Protocole hybride: Kyber + ChaCha20-Poly1305:" -ForegroundColor Cyan

# Générer les clés Kyber
$kyberKeys = New-KyberKeyPair -ParameterSet Kyber768

# Encapsuler une clé partagée
$encapsulated = Invoke-KyberEncapsulate -PublicKey $kyberKeys.PublicKey -ParameterSet Kyber768

# Utiliser la clé partagée pour chiffrer avec ChaCha20-Poly1305
# Note: La clé partagée Kyber fait 32 bytes, parfait pour ChaCha20-Poly1305!
$messageToEncrypt = "Message très secret"
$encryptedWithSharedKey = Protect-ChaCha20Poly1305 -Plaintext $messageToEncrypt -Key $encapsulated.SharedSecret

Write-Host "   ✅ Clé partagée Kyber utilisée pour ChaCha20-Poly1305" -ForegroundColor Green
Write-Host "   Clé partagée (hex): $($encapsulated.SharedSecretHex.Substring(0, 32))..." -ForegroundColor Gray

# Décapsuler la clé partagée côté récepteur
$decapsulated = Invoke-KyberDecapsulate -Ciphertext $encapsulated.Ciphertext -PrivateKey $kyberKeys.PrivateKey -ParameterSet Kyber768

# Déchiffrer avec la clé partagée
$decryptedWithSharedKey = Unprotect-ChaCha20Poly1305 -CiphertextWithTag $encryptedWithSharedKey.CiphertextWithTag -Key $decapsulated.SharedSecret -Nonce $encryptedWithSharedKey.Nonce
$finalDecryptedText = [System.Text.Encoding]::UTF8.GetString($decryptedWithSharedKey)
Write-Host "   Message déchiffré: $finalDecryptedText" -ForegroundColor Green
Write-Host "   ✅ Protocole hybride réussi!" -ForegroundColor Green

Write-Host "`n=== Tests terminés ===" -ForegroundColor Cyan

