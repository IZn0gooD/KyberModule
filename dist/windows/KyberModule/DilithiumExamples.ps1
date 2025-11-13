# Exemples d'utilisation du module Dilithium (ML-DSA)
# Module: KyberModule

# Importer le module
$ModulePath = Split-Path -Parent $MyInvocation.MyCommand.Path
Import-Module (Join-Path $ModulePath "KyberModule.psd1") -Force

Write-Host "`n=== Exemples Dilithium (ML-DSA) ===" -ForegroundColor Cyan
Write-Host ""

# Exemple 1: Génération de clés Dilithium
Write-Host "1. Génération de paire de clés Dilithium3:" -ForegroundColor Yellow
$dilithiumKeys = New-DilithiumKeyPair -ParameterSet Dilithium3
Write-Host "   Clé publique (hex): $($dilithiumKeys.PublicKeyHex.Substring(0, 64))..." -ForegroundColor Green
Write-Host "   Clé privée (hex): $($dilithiumKeys.PrivateKeyHex.Substring(0, 64))..." -ForegroundColor Green
Write-Host "   Paramètre: $($dilithiumKeys.ParameterSet)" -ForegroundColor Green
Write-Host ""

# Exemple 2: Signature de données
Write-Host "2. Signature de données:" -ForegroundColor Yellow
$message = "Message à signer avec Dilithium"
$messageBytes = [System.Text.Encoding]::UTF8.GetBytes($message)
$signature = Invoke-DilithiumSign -Data $messageBytes -PrivateKey $dilithiumKeys.PrivateKey -ParameterSet Dilithium3
Write-Host "   Message: $message" -ForegroundColor Green
Write-Host "   Signature (hex): $($signature.SignatureHex.Substring(0, 64))..." -ForegroundColor Green
Write-Host ""

# Exemple 3: Vérification de signature
Write-Host "3. Vérification de signature:" -ForegroundColor Yellow
$isValid = Test-DilithiumSignature -Data $messageBytes -Signature $signature.Signature -PublicKey $dilithiumKeys.PublicKey -ParameterSet Dilithium3
Write-Host "   Signature valide: $isValid" -ForegroundColor $(if ($isValid) { "Green" } else { "Red" })
Write-Host ""

# Exemple 4: Test avec données modifiées
Write-Host "4. Test avec données modifiées (doit échouer):" -ForegroundColor Yellow
$modifiedMessage = "Message modifié"
$modifiedBytes = [System.Text.Encoding]::UTF8.GetBytes($modifiedMessage)
$isValidModified = Test-DilithiumSignature -Data $modifiedBytes -Signature $signature.Signature -PublicKey $dilithiumKeys.PublicKey -ParameterSet Dilithium3
Write-Host "   Signature valide: $isValidModified (doit être False)" -ForegroundColor $(if ($isValidModified) { "Red" } else { "Green" })
Write-Host ""

# Exemple 5: Export/Import de clés
Write-Host "5. Export/Import de clés:" -ForegroundColor Yellow
$exportPath = ".\dilithium_keys.txt"
Export-DilithiumKeyPair -KeyPair $dilithiumKeys -Path $exportPath -Format Text -Force
Write-Host "   Clés exportées vers: $exportPath" -ForegroundColor Green

$importedKeys = Import-DilithiumKeyPair -Path $exportPath
Write-Host "   Clés importées depuis: $exportPath" -ForegroundColor Green

# Vérifier que les clés importées fonctionnent
$testSignature = Invoke-DilithiumSign -Data $messageBytes -PrivateKey $importedKeys.PrivateKey -ParameterSet Dilithium3
$testValid = Test-DilithiumSignature -Data $messageBytes -Signature $testSignature.Signature -PublicKey $importedKeys.PublicKey -ParameterSet Dilithium3
Write-Host "   Vérification avec clés importées: $testValid" -ForegroundColor $(if ($testValid) { "Green" } else { "Red" })
Write-Host ""

# Exemple 6: Différents paramètres de sécurité
Write-Host "6. Test avec différents paramètres de sécurité:" -ForegroundColor Yellow
$dilithium2Keys = New-DilithiumKeyPair -ParameterSet Dilithium2
$dilithium5Keys = New-DilithiumKeyPair -ParameterSet Dilithium5
Write-Host "   Dilithium2 (ML-DSA-44): Clé publique $(($dilithium2Keys.PublicKey.Length)) bytes" -ForegroundColor Green
Write-Host "   Dilithium3 (ML-DSA-65): Clé publique $(($dilithiumKeys.PublicKey.Length)) bytes" -ForegroundColor Green
Write-Host "   Dilithium5 (ML-DSA-87): Clé publique $(($dilithium5Keys.PublicKey.Length)) bytes" -ForegroundColor Green
Write-Host ""

Write-Host "=== Tests terminés ===" -ForegroundColor Cyan

