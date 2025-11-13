# ============================================
# Exemples d'utilisation TLS 1.3 Post-Quantum Hybride
# ============================================

# Importer le module
$ModulePath = Split-Path -Parent $MyInvocation.MyCommand.Path
Import-Module (Join-Path $ModulePath "KyberModule.psd1") -Force

Write-Host "=== Exemples TLS 1.3 Post-Quantum Hybride ===" -ForegroundColor Cyan
Write-Host ""

# ============================================
# Exemple 1: Générer une configuration TLS hybride par défaut
# ============================================
Write-Host "1. Générer une configuration TLS hybride par défaut" -ForegroundColor Yellow
Write-Host "   (ECDSA-P256 + Kyber768 + Dilithium3)" -ForegroundColor Gray

$hybridConfig = New-TLS13HybridConfig
Write-Host "   ✅ Configuration générée" -ForegroundColor Green
Write-Host "   Cipher Suite: $(Get-TLS13HybridCipherSuite -Config $hybridConfig)" -ForegroundColor Cyan

# ============================================
# Exemple 2: Générer une configuration avec RSA
# ============================================
Write-Host "`n2. Générer une configuration avec RSA-2048" -ForegroundColor Yellow

$rsaConfig = New-TLS13HybridConfig -ClassicalAlgorithm RSA_2048 -PostQuantumKemAlgorithm Kyber768 -PostQuantumSigAlgorithm Dilithium3
Write-Host "   ✅ Configuration RSA générée" -ForegroundColor Green
Write-Host "   Cipher Suite: $(Get-TLS13HybridCipherSuite -Config $rsaConfig)" -ForegroundColor Cyan

# ============================================
# Exemple 3: Générer une configuration haute sécurité
# ============================================
Write-Host "`n3. Générer une configuration haute sécurité" -ForegroundColor Yellow
Write-Host "   (ECDSA-P384 + Kyber1024 + Dilithium5)" -ForegroundColor Gray

$highSecConfig = New-TLS13HybridConfig -ClassicalAlgorithm ECDSA_P384 -PostQuantumKemAlgorithm Kyber1024 -PostQuantumSigAlgorithm Dilithium5
Write-Host "   ✅ Configuration haute sécurité générée" -ForegroundColor Green
Write-Host "   Cipher Suite: $(Get-TLS13HybridCipherSuite -Config $highSecConfig)" -ForegroundColor Cyan

# ============================================
# Exemple 4: Exporter une configuration TLS hybride
# ============================================
Write-Host "`n4. Exporter une configuration TLS hybride" -ForegroundColor Yellow

$exportPath = "tls13_hybrid_config.txt"
Export-TLS13HybridConfig -Config $hybridConfig -Path $exportPath -Force
Write-Host "   ✅ Configuration exportée: $exportPath" -ForegroundColor Green

# Afficher un aperçu
if (Test-Path $exportPath)
{
    Write-Host "`n   Aperçu de la configuration:" -ForegroundColor Gray
    Get-Content $exportPath | Select-Object -First 5 | ForEach-Object { Write-Host "   $_" -ForegroundColor DarkGray }
}

# ============================================
# Exemple 5: Comparer différentes configurations
# ============================================
Write-Host "`n5. Comparer différentes configurations" -ForegroundColor Yellow

$configs = @(
    @{ Name = "Standard"; Config = New-TLS13HybridConfig -ClassicalAlgorithm ECDSA_P256 -PostQuantumKemAlgorithm Kyber768 -PostQuantumSigAlgorithm Dilithium3 },
    @{ Name = "Haute sécurité"; Config = New-TLS13HybridConfig -ClassicalAlgorithm ECDSA_P384 -PostQuantumKemAlgorithm Kyber1024 -PostQuantumSigAlgorithm Dilithium5 },
    @{ Name = "RSA"; Config = New-TLS13HybridConfig -ClassicalAlgorithm RSA_2048 -PostQuantumKemAlgorithm Kyber768 -PostQuantumSigAlgorithm Dilithium3 }
)

Write-Host "`n   Configurations générées:" -ForegroundColor Gray
foreach ($item in $configs)
{
    $cipherSuite = Get-TLS13HybridCipherSuite -Config $item.Config
    Write-Host "   - $($item.Name): $cipherSuite" -ForegroundColor White
}

# ============================================
# Résumé
# ============================================
Write-Host "`n=== Résumé ===" -ForegroundColor Cyan
Write-Host "Fonctionnalités TLS 1.3 Post-Quantum Hybride:" -ForegroundColor Yellow
Write-Host "  ✅ Génération de configurations hybrides (classique + post-quantique)" -ForegroundColor White
Write-Host "  ✅ Support ECDSA (P-256, P-384) et RSA (2048, 3072)" -ForegroundColor White
Write-Host "  ✅ Support Kyber (ML-KEM) pour l'échange de clés" -ForegroundColor White
Write-Host "  ✅ Support Dilithium (ML-DSA) pour les signatures" -ForegroundColor White
Write-Host "  ✅ Export de configurations pour intégration TLS" -ForegroundColor White
Write-Host ""
Write-Host "Avantages de l'hybridation:" -ForegroundColor Yellow
Write-Host "  🔐 Sécurité classique éprouvée (compatibilité)" -ForegroundColor White
Write-Host "  🛡️ Sécurité post-quantique (résistance aux ordinateurs quantiques)" -ForegroundColor White
Write-Host "  ✅ Transition progressive vers la cryptographie post-quantique" -ForegroundColor White
Write-Host ""
Write-Host "Note: Ces configurations peuvent être utilisées avec des bibliothèques TLS" -ForegroundColor Gray
Write-Host "      supportant les extensions post-quantiques (ex: OQS-BoringSSL)." -ForegroundColor Gray

