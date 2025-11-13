# Test simple pour vérifier que les cmdlets sont chargés

Write-Host "=== Test de chargement des cmdlets ===" -ForegroundColor Cyan

# Décharger le module si présent
Remove-Module KyberModule -ErrorAction SilentlyContinue
Start-Sleep -Milliseconds 500

# Importer le module
Write-Host "Import du module..." -ForegroundColor Yellow
Import-Module .\KyberModule.psd1 -Force

# Lister tous les cmdlets
Write-Host "`nCmdlets disponibles:" -ForegroundColor Yellow
Get-Command -Module KyberModule | Select-Object Name | Format-Table -AutoSize

# Vérifier les cmdlets sécurisés
Write-Host "`nCmdlets sécurisés recherchés:" -ForegroundColor Yellow
$secureCmdlets = @(
    'Invoke-KyberDecapsulateSecure',
    'Test-DilithiumSignatureSecure',
    'Test-ConstantTimeCompare'
)

foreach ($cmdlet in $secureCmdlets) {
    $cmd = Get-Command $cmdlet -ErrorAction SilentlyContinue
    if ($cmd) {
        Write-Host "  ✅ $cmdlet" -ForegroundColor Green
    } else {
        Write-Host "  ❌ $cmdlet - NON TROUVÉ" -ForegroundColor Red
    }
}

