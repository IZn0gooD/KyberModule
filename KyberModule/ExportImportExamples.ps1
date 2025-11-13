# Exemples d'exportation et d'importation de clés Kyber

$ModulePath = Split-Path -Parent $MyInvocation.MyCommand.Path
Import-Module (Join-Path $ModulePath "KyberModule.psd1") -Force

Write-Host "`n=== Exemple 1: Export/Import en format Text (Key=Value) ===" -ForegroundColor Cyan

# Générer une paire de clés
$keys = New-KyberKeyPair -ParameterSet Kyber768

# Exporter en format Text (sécurisé, sans dépendances JSON)
$textPath = Join-Path $ModulePath "alice_keys.txt"
Export-KyberKeyPair -KeyPair $keys -Path $textPath -Format Text -Force
Write-Host "✓ Clés exportées vers: $textPath" -ForegroundColor Green

# Importer depuis le fichier Text
$importedKeys = Import-KyberKeyPair -Path $textPath
Write-Host "✓ Clés importées depuis: $textPath" -ForegroundColor Green

# Vérifier que les clés correspondent
if ($keys.PublicKeyHex -eq $importedKeys.PublicKeyHex -and 
    $keys.PrivateKeyHex -eq $importedKeys.PrivateKeyHex)
{
    Write-Host "✓ Les clés correspondent parfaitement !" -ForegroundColor Green
}

Write-Host "`n=== Exemple 2: Export/Import en format Base64/PEM ===" -ForegroundColor Cyan

# Exporter en format Base64 (compatible avec SSH-like)
$base64Path = Join-Path $ModulePath "bob_keys.pem"
Export-KyberKeyPair -KeyPair $keys -Path $base64Path -Format Base64 -Force
Write-Host "✓ Clés exportées vers: $base64Path" -ForegroundColor Green

# Afficher le contenu
Write-Host "`nContenu du fichier PEM:" -ForegroundColor Yellow
Get-Content $base64Path | Select-Object -First 5

# Importer depuis le fichier Base64
$importedKeys2 = Import-KyberKeyPair -Path $base64Path
Write-Host "✓ Clés importées depuis: $base64Path" -ForegroundColor Green

Write-Host "`n=== Exemple 3: Export uniquement de la clé publique ===" -ForegroundColor Cyan

# Exporter uniquement la clé publique (pour la partager)
$publicKeyPath = Join-Path $ModulePath "public_key.pem"
Export-KyberPublicKey -PublicKey $keys.PublicKey -Path $publicKeyPath -Format Base64 -Force
Write-Host "✓ Clé publique exportée vers: $publicKeyPath" -ForegroundColor Green

# Exporter en format hexadécimal
$publicKeyHexPath = Join-Path $ModulePath "public_key.hex"
Export-KyberPublicKey -PublicKey $keys.PublicKeyHex -Path $publicKeyHexPath -Format Hex -Force
Write-Host "✓ Clé publique (hex) exportée vers: $publicKeyHexPath" -ForegroundColor Green

Write-Host "`n=== Exemple 4: Workflow complet pour un protocole (type SSH) ===" -ForegroundColor Cyan

# 1. Générer les clés du serveur
Write-Host "`n1. Génération des clés serveur..." -ForegroundColor Yellow
$serverKeys = New-KyberKeyPair -ParameterSet Kyber768
$serverPublicKeyPath = Join-Path $ModulePath "server_public.pem"
$serverPrivateKeyPath = Join-Path $ModulePath "server_private.txt"

Export-KyberPublicKey -PublicKey $serverKeys.PublicKey -Path $serverPublicKeyPath -Format Base64 -Force
Export-KyberKeyPair -KeyPair $serverKeys -Path $serverPrivateKeyPath -Format Text -Force

Write-Host "   ✓ Clé publique serveur: $serverPublicKeyPath" -ForegroundColor Green
Write-Host "   ✓ Clé privée serveur: $serverPrivateKeyPath" -ForegroundColor Green

# 2. Client se connecte avec la clé publique du serveur
Write-Host "`n2. Client encapsule une clé partagée avec la clé publique du serveur..." -ForegroundColor Yellow
$serverPublicKeyBytes = Import-KyberPublicKey -Path $serverPublicKeyPath
$clientEncapsulated = Invoke-KyberEncapsulate -PublicKey $serverPublicKeyBytes -ParameterSet Kyber768

Write-Host "   ✓ Clé partagée générée: $($clientEncapsulated.SharedSecretHex)" -ForegroundColor Green
Write-Host "   ✓ Ciphertext à envoyer au serveur" -ForegroundColor Green

# 3. Serveur décapsule avec sa clé privée
Write-Host "`n3. Serveur décapsule avec sa clé privée..." -ForegroundColor Yellow
$serverKeysReloaded = Import-KyberKeyPair -Path $serverPrivateKeyPath
$serverSharedSecret = Invoke-KyberDecapsulate -Ciphertext $clientEncapsulated.Ciphertext -PrivateKey $serverKeysReloaded.PrivateKey -ParameterSet Kyber768

Write-Host "   ✓ Clé partagée obtenue: $($serverSharedSecret.SharedSecretHex)" -ForegroundColor Green

# 4. Vérifier que les clés correspondent
Write-Host "`n4. Vérification..." -ForegroundColor Yellow
$match = $true
for ($i = 0; $i -lt $clientEncapsulated.SharedSecret.Length; $i++)
{
    if ($clientEncapsulated.SharedSecret[$i] -ne $serverSharedSecret.SharedSecret[$i])
    {
        $match = $false
        break
    }
}

if ($match)
{
    Write-Host "   ✓ Les clés partagées correspondent ! Communication sécurisée établie." -ForegroundColor Green
}
else
{
    Write-Host "   ✗ Les clés partagées ne correspondent pas !" -ForegroundColor Red
}

Write-Host "`n=== Nettoyage des fichiers de test ===" -ForegroundColor Cyan
$testFiles = @($textPath, $base64Path, $publicKeyPath, $publicKeyHexPath, $serverPublicKeyPath, $serverPrivateKeyPath)
foreach ($file in $testFiles)
{
    if (Test-Path $file)
    {
        Remove-Item $file -Force
        Write-Host "  Supprimé: $file" -ForegroundColor Gray
    }
}

Write-Host "`n=== Tous les exemples terminés ===" -ForegroundColor Cyan

