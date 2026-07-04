# Script pour inspecter les champs de KyberPrivateKeyParameters

$ModulePath = Split-Path -Parent $MyInvocation.MyCommand.Path
Import-Module (Join-Path $ModulePath "KyberModule.psd1") -Force

# Charger les types nécessaires
Add-Type -Path (Join-Path $ModulePath "BouncyCastle.Crypto.dll")
Add-Type -Path (Join-Path $ModulePath "KyberLibrary.dll")

$kyberType = [Org.BouncyCastle.Pqc.Crypto.Crystals.Kyber.KyberPrivateKeyParameters]
$allFields = $kyberType.GetFields([System.Reflection.BindingFlags]::NonPublic -bor [System.Reflection.BindingFlags]::Public -bor [System.Reflection.BindingFlags]::Instance)

Write-Host "Champs trouvés dans KyberPrivateKeyParameters:" -ForegroundColor Cyan
foreach ($field in $allFields)
{
    Write-Host "  - $($field.Name) : $($field.FieldType.Name)" -ForegroundColor Yellow
}

# Générer une clé pour tester
$wrapper = New-Object KyberLibrary.KyberWrapper
$keys = $wrapper.GenerateKeyPair()

Write-Host "`nTaille clé privée générée: $($keys.PrivateKey.Length) bytes" -ForegroundColor Cyan
Write-Host "Premiers 8 bytes: $([BitConverter]::ToString($keys.PrivateKey[0..7]))" -ForegroundColor Yellow

