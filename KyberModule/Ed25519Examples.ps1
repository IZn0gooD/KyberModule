# Exemples d'utilisation d'Ed25519 pour la signature numérique

$ModulePath = Split-Path -Parent $MyInvocation.MyCommand.Path
Import-Module (Join-Path $ModulePath "KyberModule.psd1") -Force

Write-Host "`n=== Exemple 1: Génération de clés Ed25519 ===" -ForegroundColor Cyan

# Générer une paire de clés Ed25519
$ed25519Keys = New-Ed25519KeyPair

Write-Host "Clé publique (hex): $($ed25519Keys.PublicKeyHex)" -ForegroundColor Green
Write-Host "Taille clé publique: $($ed25519Keys.PublicKey.Length) bytes" -ForegroundColor Yellow
Write-Host "Clé privée (hex): $($ed25519Keys.PrivateKeyHex)" -ForegroundColor Green
Write-Host "Taille clé privée: $($ed25519Keys.PrivateKey.Length) bytes" -ForegroundColor Yellow

Write-Host "`n=== Exemple 2: Signature et vérification ===" -ForegroundColor Cyan

# Message à signer
$message = "Hello, World! This is a test message."
Write-Host "Message à signer: $message" -ForegroundColor Yellow

# Signer le message
$signature = Invoke-Ed25519Sign -Data $message -PrivateKey $ed25519Keys.PrivateKey
Write-Host "Signature générée (hex): $($signature.SignatureHex)" -ForegroundColor Green
Write-Host "Taille signature: $($signature.Signature.Length) bytes" -ForegroundColor Yellow

# Vérifier la signature
$isValid = Test-Ed25519Signature -Data $message -Signature $signature.Signature -PublicKey $ed25519Keys.PublicKey

if ($isValid)
{
    Write-Host "✓ Signature valide !" -ForegroundColor Green
}
else
{
    Write-Host "✗ Signature invalide !" -ForegroundColor Red
}

Write-Host "`n=== Exemple 3: Signature avec données modifiées (devrait échouer) ===" -ForegroundColor Cyan

# Modifier le message
$modifiedMessage = "Hello, World! This is a MODIFIED message."

# Vérifier avec le message modifié
$isValidModified = Test-Ed25519Signature -Data $modifiedMessage -Signature $signature.Signature -PublicKey $ed25519Keys.PublicKey

if ($isValidModified)
{
    Write-Host "✗ Signature valide (ERREUR : ne devrait pas être valide) !" -ForegroundColor Red
}
else
{
    Write-Host "✓ Signature invalide (comme attendu) !" -ForegroundColor Green
}

Write-Host "`n=== Exemple 4: Export/Import de clés Ed25519 ===" -ForegroundColor Cyan

# Exporter les clés
$ed25519KeyPath = Join-Path $ModulePath "ed25519_keys.txt"
Export-Ed25519KeyPair -KeyPair $ed25519Keys -Path $ed25519KeyPath -Format Text -Force
Write-Host "✓ Clés exportées vers: $ed25519KeyPath" -ForegroundColor Green

# Importer les clés
$importedEd25519Keys = Import-Ed25519KeyPair -Path $ed25519KeyPath
Write-Host "✓ Clés importées depuis: $ed25519KeyPath" -ForegroundColor Green

# Vérifier que les clés correspondent
if ($ed25519Keys.PublicKeyHex -eq $importedEd25519Keys.PublicKeyHex -and 
    $ed25519Keys.PrivateKeyHex -eq $importedEd25519Keys.PrivateKeyHex)
{
    Write-Host "✓ Les clés correspondent parfaitement !" -ForegroundColor Green
}

# Signer avec les clés importées
$signature2 = Invoke-Ed25519Sign -Data $message -PrivateKey $importedEd25519Keys.PrivateKey
$isValid2 = Test-Ed25519Signature -Data $message -Signature $signature2.Signature -PublicKey $importedEd25519Keys.PublicKey

if ($isValid2)
{
    Write-Host "✓ Signature avec les clés importées valide !" -ForegroundColor Green
}

Write-Host "`n=== Exemple 5: Utilisation combinée Kyber + Ed25519 ===" -ForegroundColor Cyan

# 1. Générer des clés Kyber pour l'échange de clés
$kyberKeys = New-KyberKeyPair -ParameterSet Kyber768

# 2. Générer des clés Ed25519 pour la signature
$signingKeys = New-Ed25519KeyPair

# 3. Encapsuler une clé partagée avec Kyber
$encapsulated = Invoke-KyberEncapsulate -PublicKey $kyberKeys.PublicKey -ParameterSet Kyber768

# 4. Signer le ciphertext avec Ed25519
$signedCiphertext = Invoke-Ed25519Sign -Data $encapsulated.Ciphertext -PrivateKey $signingKeys.PrivateKey

Write-Host "✓ Ciphertext Kyber signé avec Ed25519" -ForegroundColor Green
Write-Host "  Ciphertext: $($encapsulated.CiphertextHex)" -ForegroundColor Yellow
Write-Host "  Signature: $($signedCiphertext.SignatureHex)" -ForegroundColor Yellow

# 5. Vérifier la signature du ciphertext
$ciphertextValid = Test-Ed25519Signature -Data $encapsulated.Ciphertext -Signature $signedCiphertext.Signature -PublicKey $signingKeys.PublicKey

if ($ciphertextValid)
{
    Write-Host "✓ Signature du ciphertext valide !" -ForegroundColor Green
}

Write-Host "`n=== Nettoyage ===" -ForegroundColor Cyan
if (Test-Path $ed25519KeyPath)
{
    Remove-Item $ed25519KeyPath -Force
    Write-Host "  Fichier de test supprimé" -ForegroundColor Gray
}

Write-Host "`n=== Tous les exemples Ed25519 terminés ===" -ForegroundColor Cyan

