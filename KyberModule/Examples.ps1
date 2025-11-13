# Exemples d'utilisation du module PowerShell KyberModule

# Importer le module
$ModulePath = Split-Path -Parent $MyInvocation.MyCommand.Path
Import-Module (Join-Path $ModulePath "KyberModule.psd1") -Force

Write-Host "`n=== Exemple 1: Génération basique de clés ===" -ForegroundColor Cyan

# Générer une paire de clés avec Kyber768 (par défaut)
$keys = New-KyberKeyPair -ParameterSet Kyber768

Write-Host "Clé publique (hex): $($keys.PublicKeyHex)" -ForegroundColor Green
Write-Host "Taille clé publique: $($keys.PublicKey.Length) bytes" -ForegroundColor Yellow
Write-Host "Clé privée (hex): $($keys.PrivateKeyHex)" -ForegroundColor Green
Write-Host "Taille clé privée: $($keys.PrivateKey.Length) bytes" -ForegroundColor Yellow

Write-Host "`n=== Exemple 2: Échange de clés partagées (Alice et Bob) ===" -ForegroundColor Cyan

# Alice génère une paire de clés
$aliceKeys = New-KyberKeyPair -ParameterSet Kyber768
Write-Host "Alice a généré sa paire de clés" -ForegroundColor Green

# Bob utilise la clé publique d'Alice pour créer une clé partagée
$bobEncapsulated = Invoke-KyberEncapsulate -PublicKey $aliceKeys.PublicKey -ParameterSet Kyber768
Write-Host "Bob a encapsulé une clé partagée avec la clé publique d'Alice" -ForegroundColor Green
Write-Host "Ciphertext (hex): $($bobEncapsulated.CiphertextHex)" -ForegroundColor Yellow

# Alice décapsule pour obtenir la même clé partagée
$aliceSharedSecret = Invoke-KyberDecapsulate -Ciphertext $bobEncapsulated.Ciphertext -PrivateKey $aliceKeys.PrivateKey -ParameterSet Kyber768
Write-Host "Alice a décapsulé la clé partagée" -ForegroundColor Green

# Vérifier que les clés partagées sont identiques
$sharedSecretMatch = $true
if ($bobEncapsulated.SharedSecret.Length -ne $aliceSharedSecret.SharedSecret.Length)
{
    $sharedSecretMatch = $false
}
else
{
    for ($i = 0; $i -lt $bobEncapsulated.SharedSecret.Length; $i++)
    {
        if ($bobEncapsulated.SharedSecret[$i] -ne $aliceSharedSecret.SharedSecret[$i])
        {
            $sharedSecretMatch = $false
            break
        }
    }
}

if ($sharedSecretMatch)
{
    Write-Host "✓ Les clés partagées correspondent !" -ForegroundColor Green
    Write-Host "Clé partagée (hex): $($bobEncapsulated.SharedSecretHex)" -ForegroundColor Yellow
}
else
{
    Write-Host "✗ Les clés partagées ne correspondent pas !" -ForegroundColor Red
}

Write-Host "`n=== Exemple 3: Utilisation avec des chaînes hexadécimales ===" -ForegroundColor Cyan

# Générer des clés
$keys3 = New-KyberKeyPair -ParameterSet Kyber512

# Utiliser directement les chaînes hex
$encapsulated3 = Invoke-KyberEncapsulate -PublicKey $keys3.PublicKeyHex -ParameterSet Kyber512

# Décapsuler avec les chaînes hex
$sharedSecret3 = Invoke-KyberDecapsulate -Ciphertext $encapsulated3.CiphertextHex -PrivateKey $keys3.PrivateKeyHex -ParameterSet Kyber512

Write-Host "Clé partagée décapsulée (hex): $($sharedSecret3.SharedSecretHex)" -ForegroundColor Green

Write-Host "`n=== Exemple 4: Comparaison des paramètres de sécurité ===" -ForegroundColor Cyan

foreach ($paramSet in @('Kyber512', 'Kyber768', 'Kyber1024'))
{
    $keysTest = New-KyberKeyPair -ParameterSet $paramSet
    
    Write-Host "$paramSet :" -ForegroundColor Yellow
    Write-Host "  Clé publique: $($keysTest.PublicKey.Length) bytes" -ForegroundColor White
    Write-Host "  Clé privée: $($keysTest.PrivateKey.Length) bytes" -ForegroundColor White
    
    $encapsTest = Invoke-KyberEncapsulate -PublicKey $keysTest.PublicKey -ParameterSet $paramSet
    Write-Host "  Ciphertext: $($encapsTest.Ciphertext.Length) bytes" -ForegroundColor White
    Write-Host "  Clé partagée: $($encapsTest.SharedSecret.Length) bytes" -ForegroundColor White
    Write-Host ""
}

Write-Host "=== Tous les exemples terminés ===" -ForegroundColor Cyan

