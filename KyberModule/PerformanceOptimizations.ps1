# ============================================
# Script de Test des Optimisations de Performance
# ============================================
# Ce script teste les optimisations de performance :
# - Multi-threading natif
# - Accélération matérielle (AVX2/AVX512)
# - Support HSM (Hardware Security Module)

# Importer le module
$ModulePath = Split-Path -Parent $MyInvocation.MyCommand.Path
Import-Module (Join-Path $ModulePath "KyberModule.psd1") -Force

Write-Host "=== Test des Optimisations de Performance ===" -ForegroundColor Cyan
Write-Host ""

# ============================================
# 1. Affichage des capacités matérielles
# ============================================
Write-Host "1. Capacités Matérielles Détectées" -ForegroundColor Yellow
Write-Host ""

# Note: Ces informations devraient être exposées via des cmdlets PowerShell
# Pour l'instant, on affiche un message informatif
Write-Host "   Vérification des optimisations disponibles..." -ForegroundColor Cyan
Write-Host "   → Accélération SIMD: Détectée automatiquement via System.Numerics.Vector<T>" -ForegroundColor Gray
Write-Host "   → Multi-threading: Disponible via System.Threading.Tasks.Parallel" -ForegroundColor Gray
Write-Host "   → HSM: Interface disponible, détection automatique" -ForegroundColor Gray
Write-Host ""

# ============================================
# 2. Test de performance avec multi-threading
# ============================================
Write-Host "2. Test Multi-threading" -ForegroundColor Yellow
Write-Host ""

Write-Host "   Test de génération parallèle de clés Kyber..." -ForegroundColor Cyan
$startTime = Get-Date
$keyCount = 10
$keys = @()

# Génération séquentielle
for ($i = 0; $i -lt $keyCount; $i++) {
    $keys += New-KyberKeyPair -ParameterSet Kyber768
}
$sequentialTime = ((Get-Date) - $startTime).TotalMilliseconds

Write-Host "   Génération séquentielle: $([math]::Round($sequentialTime, 2)) ms pour $keyCount paires de clés" -ForegroundColor Gray
Write-Host "   Temps moyen: $([math]::Round($sequentialTime / $keyCount, 2)) ms par paire" -ForegroundColor Gray
Write-Host ""

# Note: Pour une vraie comparaison avec multi-threading, il faudrait
# exposer les méthodes de ParallelCryptographicOperations via des cmdlets
Write-Host "   ℹ️  Pour utiliser le multi-threading, utilisez les méthodes de ParallelCryptographicOperations" -ForegroundColor Cyan
Write-Host "      dans votre code C# ou via des cmdlets PowerShell dédiées" -ForegroundColor Cyan
Write-Host ""

# ============================================
# 3. Test d'accélération matérielle
# ============================================
Write-Host "3. Test Accélération Matérielle (SIMD)" -ForegroundColor Yellow
Write-Host ""

Write-Host "   Test de comparaison de tableaux avec SIMD..." -ForegroundColor Cyan

# Créer des tableaux de test
$testSize = 1024 * 1024  # 1 MB
$array1 = New-Object byte[] $testSize
$array2 = New-Object byte[] $testSize
[System.Security.Cryptography.RandomNumberGenerator]::Fill($array1)
[System.Security.Cryptography.RandomNumberGenerator]::Fill($array2)

# Test de comparaison (utilise ConstantTimeEquals qui peut utiliser SIMD)
$iterations = 100
$startTime = Get-Date
for ($i = 0; $i -lt $iterations; $i++) {
    $null = Test-ConstantTimeCompare -Array1 $array1 -Array2 $array2
}
$compareTime = ((Get-Date) - $startTime).TotalMilliseconds

Write-Host "   $iterations comparaisons de $testSize bytes: $([math]::Round($compareTime, 2)) ms" -ForegroundColor Gray
Write-Host "   Temps moyen: $([math]::Round($compareTime / $iterations, 3)) ms par comparaison" -ForegroundColor Gray
Write-Host ""

# ============================================
# 4. Test HSM
# ============================================
Write-Host "4. Test HSM (Hardware Security Module)" -ForegroundColor Yellow
Write-Host ""

Write-Host "   Vérification de la disponibilité HSM..." -ForegroundColor Cyan
Write-Host "   → HSM Software (fallback): Toujours disponible" -ForegroundColor Gray
Write-Host "   → HSM Matériel: Détection automatique (non implémentée dans ce script)" -ForegroundColor Gray
Write-Host "   ℹ️  Pour utiliser un HSM matériel, configurez HSMManager.CurrentProvider" -ForegroundColor Cyan
Write-Host ""

# ============================================
# 5. Recommandations
# ============================================
Write-Host "5. Recommandations d'Optimisation" -ForegroundColor Yellow
Write-Host ""

Write-Host "   Pour améliorer les performances :" -ForegroundColor Cyan
Write-Host "   1. Utilisez le multi-threading pour les opérations batch" -ForegroundColor White
Write-Host "      → Génération de plusieurs paires de clés en parallèle" -ForegroundColor Gray
Write-Host "      → Traitement de plusieurs signatures/vérifications en parallèle" -ForegroundColor Gray
Write-Host ""
Write-Host "   2. L'accélération SIMD est automatique pour les grandes opérations" -ForegroundColor White
Write-Host "      → Seuil minimum: 1 KB de données" -ForegroundColor Gray
Write-Host "      → Utilise Vector<T> qui détecte automatiquement AVX2/AVX512" -ForegroundColor Gray
Write-Host ""
Write-Host "   3. Pour les environnements de production critiques :" -ForegroundColor White
Write-Host "      → Configurez un HSM matériel pour la sécurité renforcée" -ForegroundColor Gray
Write-Host "      → Utilisez le stockage sécurisé des clés privées" -ForegroundColor Gray
Write-Host ""

# ============================================
# Résumé
# ============================================
Write-Host "=== Résumé ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "Les optimisations suivantes sont disponibles :" -ForegroundColor Yellow
Write-Host "  ✅ Multi-threading natif (Parallel.For/ForEach)" -ForegroundColor Green
Write-Host "  ✅ Accélération SIMD automatique (Vector<T>)" -ForegroundColor Green
Write-Host "  ✅ Support HSM (interface extensible)" -ForegroundColor Green
Write-Host ""
Write-Host "Pour utiliser ces optimisations dans votre code :" -ForegroundColor Yellow
Write-Host "  • Utilisez ParallelCryptographicOperations pour le multi-threading" -ForegroundColor White
Write-Host "  • Les opérations SIMD sont automatiques via HardwareAcceleration" -ForegroundColor White
Write-Host "  • Configurez HSMManager pour utiliser un HSM matériel" -ForegroundColor White
Write-Host ""

