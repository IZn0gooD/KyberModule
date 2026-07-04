# Exemples d'utilisation des formats PKCS#8 et X.509
# Supporte les clés post-quantiques (Kyber, Dilithium)

Import-Module "$PSScriptRoot\KyberModule.psd1" -Force

Write-Host "=== Exemples PKCS#8 et X.509 ===" -ForegroundColor Cyan
Write-Host ""

# ============================================
# Exemple 1: Export PKCS#8 - Dilithium
# ============================================
Write-Host "1. Export PKCS#8 - Clé privée Dilithium" -ForegroundColor Yellow

# Générer une paire de clés Dilithium
$dilithiumKeys = New-DilithiumKeyPair -ParameterSet Dilithium3
Write-Host "   Paire de clés Dilithium générée" -ForegroundColor Green

# Exporter la clé privée en PKCS#8 (format PEM)
$pkcs8Path = Join-Path $PSScriptRoot "dilithium_private_key.pem"
Export-PrivateKeyPKCS8 -PrivateKey $dilithiumKeys.PrivateKey `
    -Path $pkcs8Path `
    -KeyType Dilithium `
    -ParameterSet Dilithium3 `
    -Format PEM `
    -Force

Write-Host "   Clé privée exportée en PKCS#8 (PEM): $pkcs8Path" -ForegroundColor Green

# ============================================
# Exemple 2: Import PKCS#8
# ============================================
Write-Host "`n2. Import PKCS#8" -ForegroundColor Yellow

$importedPrivateKey = Import-PrivateKeyPKCS8 -Path $pkcs8Path
Write-Host "   Clé privée importée depuis PKCS#8" -ForegroundColor Green
Write-Host "   Taille de la clé importée: $($importedPrivateKey.Length) bytes" -ForegroundColor Gray

# Vérifier que la clé importée correspond à la clé originale
if ([System.Linq.Enumerable]::SequenceEqual($dilithiumKeys.PrivateKey, $importedPrivateKey))
{
    Write-Host "   ✅ Clé importée correspond à la clé originale" -ForegroundColor Green
}
else
{
    Write-Host "   ❌ Clé importée différente de la clé originale" -ForegroundColor Red
}

# ============================================
# Exemple 3: Export PKCS#8 - Kyber (format DER)
# ============================================
Write-Host "`n3. Export PKCS#8 - Clé privée Kyber (format DER)" -ForegroundColor Yellow

# Générer une paire de clés Kyber
$kyberKeys = New-KyberKeyPair -ParameterSet Kyber768
Write-Host "   Paire de clés Kyber générée" -ForegroundColor Green

# Exporter la clé privée en PKCS#8 (format DER)
$pkcs8DerPath = Join-Path $PSScriptRoot "kyber_private_key.der"
Export-PrivateKeyPKCS8 -PrivateKey $kyberKeys.PrivateKey `
    -Path $pkcs8DerPath `
    -KeyType Kyber `
    -ParameterSet Kyber768 `
    -Format DER `
    -Force

Write-Host "   Clé privée exportée en PKCS#8 (DER): $pkcs8DerPath" -ForegroundColor Green

# ============================================
# Exemple 4: Créer un certificat X.509 auto-signé avec Dilithium
# ============================================
Write-Host "`n4. Créer un certificat X.509 auto-signé (Dilithium)" -ForegroundColor Yellow

# Utiliser la paire de clés Dilithium générée précédemment
$certPath = Join-Path $PSScriptRoot "dilithium_certificate.pem"
$subjectName = "CN=Test Post-Quantum Certificate, O=My Organization, C=FR"

New-X509Certificate -SubjectName $subjectName `
    -PublicKey $dilithiumKeys.PublicKey `
    -PrivateKey $dilithiumKeys.PrivateKey `
    -Path $certPath `
    -KeyType Dilithium `
    -ParameterSet Dilithium3 `
    -ValidityDays 365 `
    -Format PEM `
    -Force

Write-Host "   Certificat X.509 créé: $certPath" -ForegroundColor Green

# Afficher le contenu du certificat (premières lignes)
Write-Host "`n   Aperçu du certificat:" -ForegroundColor Gray
Get-Content $certPath | Select-Object -First 5 | ForEach-Object { Write-Host "   $_" -ForegroundColor DarkGray }

# ============================================
# Exemple 5: Vérifier un certificat X.509
# ============================================
Write-Host "`n5. Vérifier le certificat X.509" -ForegroundColor Yellow

$isValid = Test-X509Certificate -Path $certPath
if ($isValid)
{
    Write-Host "   ✅ Certificat valide" -ForegroundColor Green
}
else
{
    Write-Host "   ❌ Certificat invalide ou expiré" -ForegroundColor Red
}

# ============================================
# Résumé
# ============================================
Write-Host "`n=== Résumé ===" -ForegroundColor Cyan
Write-Host "Fichiers créés:" -ForegroundColor Yellow
Write-Host "  - PKCS#8 (PEM): $pkcs8Path" -ForegroundColor White
Write-Host "  - PKCS#8 (DER): $pkcs8DerPath" -ForegroundColor White
Write-Host "  - Certificat X.509: $certPath" -ForegroundColor White
Write-Host ""
Write-Host "Formats supportés:" -ForegroundColor Yellow
Write-Host "  - PKCS#8: Format standard pour les clés privées (DER/PEM)" -ForegroundColor White
Write-Host "  - X.509: Format standard pour les certificats (DER/PEM)" -ForegroundColor White
Write-Host ""
Write-Host "Note: CMS (Cryptographic Message Syntax) est en cours de développement." -ForegroundColor Gray

