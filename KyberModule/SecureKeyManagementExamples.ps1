# ============================================
# Exemples d'utilisation de la Gestion Sécurisée des Clés
# ============================================

# Importer le module
$ModulePath = Split-Path -Parent $MyInvocation.MyCommand.Path
Import-Module (Join-Path $ModulePath "KyberModule.psd1") -Force

Write-Host "=== Exemples de Gestion Sécurisée des Clés ===" -ForegroundColor Cyan
Write-Host ""

# ============================================
# Exemple 1: Générer une paire de clés Kyber sécurisée
# ============================================
Write-Host "1. Génération sécurisée d'une paire de clés Kyber" -ForegroundColor Yellow

$secureKeys = New-SecureKyberKeyPair -ParameterSet Kyber768
Write-Host "   ✅ Paire de clés générée avec gestion sécurisée" -ForegroundColor Green
Write-Host "   Clé publique (hex, premiers 32 caractères): $($secureKeys.PublicKeyHex.Substring(0, 32))..." -ForegroundColor Gray

# Utiliser les clés
$publicKey = $secureKeys.PublicKey
$privateKey = $secureKeys.PrivateKey

# Encapsuler avec la clé publique
$encapsulated = Invoke-KyberEncapsulate -PublicKey $publicKey -ParameterSet Kyber768
Write-Host "   ✅ Encapsulation réussie" -ForegroundColor Green

# Décapsuler avec la clé privée
$decapsulated = Invoke-KyberDecapsulate -Ciphertext $encapsulated.Ciphertext -PrivateKey $privateKey -ParameterSet Kyber768
Write-Host "   ✅ Décapsulation réussie" -ForegroundColor Green

# Vérifier que les clés partagées correspondent
if ($encapsulated.SharedSecretHex -eq $decapsulated.SharedSecretHex) {
    Write-Host "   ✅ Clés partagées identiques" -ForegroundColor Green
}

# Nettoyer manuellement la clé privée (optionnel, sera nettoyée automatiquement à la fin)
$secureKeys.ZeroizePrivateKey()
Write-Host "   ✅ Clé privée nettoyée manuellement" -ForegroundColor Cyan

# Vérifier que la clé est nettoyée
$isZeroized = Test-ZeroizedKey -Key $privateKey
Write-Host "   Clé privée nettoyée: $isZeroized" -ForegroundColor Gray

Write-Host ""

# ============================================
# Exemple 2: Générer une paire de clés Dilithium sécurisée
# ============================================
Write-Host "2. Génération sécurisée d'une paire de clés Dilithium" -ForegroundColor Yellow

$secureDilithiumKeys = New-SecureDilithiumKeyPair -ParameterSet Dilithium3
Write-Host "   ✅ Paire de clés Dilithium générée avec gestion sécurisée" -ForegroundColor Green

# Signer un message
$message = "Message important à signer"
$messageBytes = [System.Text.Encoding]::UTF8.GetBytes($message)
$signature = Invoke-DilithiumSign -Data $messageBytes -PrivateKey $secureDilithiumKeys.PrivateKey -ParameterSet Dilithium3
Write-Host "   ✅ Message signé" -ForegroundColor Green

# Vérifier la signature
$isValid = Test-DilithiumSignature -Data $messageBytes -Signature $signature.Signature -PublicKey $secureDilithiumKeys.PublicKey -ParameterSet Dilithium3
Write-Host "   Signature valide: $isValid" -ForegroundColor $(if ($isValid) { "Green" } else { "Red" })

# Nettoyer la clé privée
$secureDilithiumKeys.ZeroizePrivateKey()
Write-Host "   ✅ Clé privée Dilithium nettoyée" -ForegroundColor Cyan

Write-Host ""

# ============================================
# Exemple 3: Générer une paire de clés Ed25519 sécurisée
# ============================================
Write-Host "3. Génération sécurisée d'une paire de clés Ed25519" -ForegroundColor Yellow

$secureEd25519Keys = New-SecureEd25519KeyPair
Write-Host "   ✅ Paire de clés Ed25519 générée avec gestion sécurisée" -ForegroundColor Green

# Signer un message
$signature = Invoke-Ed25519Sign -Data $message -PrivateKey $secureEd25519Keys.PrivateKey
Write-Host "   ✅ Message signé avec Ed25519" -ForegroundColor Green

# Vérifier la signature
$isValid = Test-Ed25519Signature -Data $message -Signature $signature.Signature -PublicKey $secureEd25519Keys.PublicKey
Write-Host "   Signature valide: $isValid" -ForegroundColor $(if ($isValid) { "Green" } else { "Red" })

# Nettoyer la clé privée
$secureEd25519Keys.ZeroizePrivateKey()
Write-Host "   ✅ Clé privée Ed25519 nettoyée" -ForegroundColor Cyan

Write-Host ""

# ============================================
# Exemple 4: Nettoyer manuellement une clé avec Clear-SecureKey
# ============================================
Write-Host "4. Nettoyage manuel d'une clé avec Clear-SecureKey" -ForegroundColor Yellow

# Générer une clé normale
$normalKeys = New-KyberKeyPair -ParameterSet Kyber768
$privateKeyToClean = $normalKeys.PrivateKey

Write-Host "   Clé avant nettoyage (premiers bytes): $($privateKeyToClean[0]), $($privateKeyToClean[1]), $($privateKeyToClean[2])" -ForegroundColor Gray

# Nettoyer avec une seule passe
$cleaned = Clear-SecureKey -Key $privateKeyToClean -Passes 1
Write-Host "   Clé nettoyée (1 passe): $cleaned" -ForegroundColor $(if ($cleaned) { "Green" } else { "Red" })

# Vérifier que la clé est nettoyée
$isZeroized = Test-ZeroizedKey -Key $privateKeyToClean
Write-Host "   Clé est nettoyée: $isZeroized" -ForegroundColor $(if ($isZeroized) { "Green" } else { "Red" })

Write-Host ""

# ============================================
# Exemple 5: Nettoyage multi-passes (sécurité renforcée)
# ============================================
Write-Host "5. Nettoyage multi-passes pour sécurité renforcée" -ForegroundColor Yellow

$keyToCleanMulti = New-KyberKeyPair -ParameterSet Kyber768
$privateKeyMulti = $keyToCleanMulti.PrivateKey

Write-Host "   Nettoyage avec 3 passes (protection contre cold boot attacks)..." -ForegroundColor Gray
$cleaned = Clear-SecureKey -Key $privateKeyMulti -Passes 3 -Nullify
Write-Host "   Clé nettoyée (3 passes): $cleaned" -ForegroundColor Green

Write-Host ""

# ============================================
# Exemple 6: Vérifier si une clé est nettoyée
# ============================================
Write-Host "6. Vérification du nettoyage d'une clé" -ForegroundColor Yellow

# Clé non nettoyée
$keys1 = New-KyberKeyPair -ParameterSet Kyber768
$isZeroized1 = Test-ZeroizedKey -Key $keys1.PrivateKey
Write-Host "   Clé non nettoyée: $isZeroized1" -ForegroundColor $(if (-not $isZeroized1) { "Green" } else { "Red" })

# Clé nettoyée
$keys2 = New-KyberKeyPair -ParameterSet Kyber768
Clear-SecureKey -Key $keys2.PrivateKey | Out-Null
$isZeroized2 = Test-ZeroizedKey -Key $keys2.PrivateKey
Write-Host "   Clé nettoyée: $isZeroized2" -ForegroundColor $(if ($isZeroized2) { "Green" } else { "Red" })

Write-Host ""

# ============================================
# Exemple 7: Utilisation dans un bloc try-finally pour nettoyage garanti
# ============================================
Write-Host "7. Pattern try-finally pour nettoyage garanti" -ForegroundColor Yellow

$keys = New-KyberKeyPair -ParameterSet Kyber768
try {
    Write-Host "   Utilisation de la clé..." -ForegroundColor Gray
    $encapsulated = Invoke-KyberEncapsulate -PublicKey $keys.PublicKey -ParameterSet Kyber768
    Write-Host "   ✅ Opération réussie" -ForegroundColor Green
}
finally {
    # Nettoyer la clé privée dans tous les cas
    Clear-SecureKey -Key $keys.PrivateKey -Passes 3 | Out-Null
    Write-Host "   ✅ Clé privée nettoyée dans le bloc finally" -ForegroundColor Cyan
}

Write-Host ""

# ============================================
# Exemple 8: Comparaison clés normales vs sécurisées
# ============================================
Write-Host "8. Comparaison: Clés normales vs Clés sécurisées" -ForegroundColor Yellow

Write-Host "   Clés normales (New-KyberKeyPair):" -ForegroundColor Gray
Write-Host "   - Clés stockées en mémoire comme byte[]" -ForegroundColor Gray
Write-Host "   - Pas de nettoyage automatique" -ForegroundColor Gray
Write-Host "   - Nettoyage manuel requis avec Clear-SecureKey" -ForegroundColor Gray

Write-Host "   Clés sécurisées (New-SecureKyberKeyPair):" -ForegroundColor Gray
Write-Host "   - Wrapper sécurisé avec IDisposable" -ForegroundColor Gray
Write-Host "   - Nettoyage automatique lors de la destruction" -ForegroundColor Gray
Write-Host "   - Méthode ZeroizePrivateKey() pour nettoyage immédiat" -ForegroundColor Gray
Write-Host "   - Protection contre les fuites de mémoire" -ForegroundColor Gray

Write-Host ""

# ============================================
# Résumé
# ============================================
Write-Host "=== Résumé ===" -ForegroundColor Cyan
Write-Host "Fonctionnalités de gestion sécurisée:" -ForegroundColor Yellow
Write-Host "  ✅ Génération sécurisée de clés (Kyber, Dilithium, Ed25519)" -ForegroundColor White
Write-Host "  ✅ Nettoyage automatique lors de la destruction" -ForegroundColor White
Write-Host "  ✅ Nettoyage manuel avec Clear-SecureKey" -ForegroundColor White
Write-Host "  ✅ Nettoyage multi-passes pour sécurité renforcée" -ForegroundColor White
Write-Host "  ✅ Vérification du nettoyage avec Test-ZeroizedKey" -ForegroundColor White
Write-Host ""
Write-Host "Bonnes pratiques:" -ForegroundColor Yellow
Write-Host "  🔐 Utiliser New-Secure*KeyPair pour les clés sensibles" -ForegroundColor White
Write-Host "  🔐 Appeler ZeroizePrivateKey() après utilisation" -ForegroundColor White
Write-Host "  🔐 Utiliser Clear-SecureKey avec -Passes 3 pour sécurité renforcée" -ForegroundColor White
Write-Host "  🔐 Utiliser try-finally pour garantir le nettoyage" -ForegroundColor White
Write-Host ""
Write-Host "Note: Les clés sécurisées sont automatiquement nettoyées par le garbage collector" -ForegroundColor Gray
Write-Host "      mais le nettoyage manuel est recommandé pour un contrôle immédiat." -ForegroundColor Gray

