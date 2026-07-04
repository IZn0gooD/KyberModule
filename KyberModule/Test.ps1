# Script de test pour le module KyberModule

$ModulePath = Split-Path -Parent $MyInvocation.MyCommand.Path
$ModuleManifest = Join-Path $ModulePath "KyberModule.psd1"

Write-Host "Import du module depuis: $ModuleManifest" -ForegroundColor Cyan
Import-Module $ModuleManifest -Force

Write-Host "`n=== Test 1: Génération de clés ===" -ForegroundColor Cyan
$keys = New-KyberKeyPair -ParameterSet Kyber768
Write-Host "✓ Clés générées avec succès !" -ForegroundColor Green
Write-Host "  Clé publique: $($keys.PublicKey.Length) bytes" -ForegroundColor Yellow
Write-Host "  Clé privée: $($keys.PrivateKey.Length) bytes" -ForegroundColor Yellow

Write-Host "`n=== Test 2: Encapsulation/Décapsulation ===" -ForegroundColor Cyan
$encapsulated = Invoke-KyberEncapsulate -PublicKey $keys.PublicKey -ParameterSet Kyber768
Write-Host "✓ Encapsulation réussie !" -ForegroundColor Green
Write-Host "  Ciphertext: $($encapsulated.Ciphertext.Length) bytes" -ForegroundColor Yellow
Write-Host "  Clé partagée: $($encapsulated.SharedSecret.Length) bytes" -ForegroundColor Yellow

$decapsulated = Invoke-KyberDecapsulate -Ciphertext $encapsulated.Ciphertext -PrivateKey $keys.PrivateKey -ParameterSet Kyber768
Write-Host "✓ Décapsulation réussie !" -ForegroundColor Green
Write-Host "  Clé partagée: $($decapsulated.SharedSecret.Length) bytes" -ForegroundColor Yellow

# Vérifier que les clés partagées correspondent
$match = $true
for ($i = 0; $i -lt $encapsulated.SharedSecret.Length; $i++)
{
    if ($encapsulated.SharedSecret[$i] -ne $decapsulated.SharedSecret[$i])
    {
        $match = $false
        break
    }
}

if ($match)
{
    Write-Host "✓ Les clés partagées correspondent !" -ForegroundColor Green
}
else
{
    Write-Host "✗ Les clés partagées ne correspondent pas !" -ForegroundColor Red
}

Write-Host "`n=== Tous les tests terminés ===" -ForegroundColor Cyan

