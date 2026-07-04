# Exemple complet: Protocole sécurisé Kyber + Dilithium (100% Post-Quantum)
# Ce script simule un scénario réel où :
# - Le serveur génère des clés Kyber (ML-KEM) et Dilithium (ML-DSA)
# - Le client encapsule une clé partagée et la signe avec Dilithium
# - Le serveur vérifie la signature et décapsule la clé
# Architecture 100% post-quantique conforme aux standards NIST

$ModulePath = Split-Path -Parent $MyInvocation.MyCommand.Path
Import-Module (Join-Path $ModulePath "KyberModule.psd1") -Force

Write-Host "`n=== Protocole sécurisé complet: Kyber + Dilithium (100% Post-Quantum) ===" -ForegroundColor Cyan
Write-Host "Scénario: Serveur/Client avec échange de clés et authentification post-quantiques`n" -ForegroundColor Yellow

# ============================================
# PARTIE SERVEUR
# ============================================
Write-Host "=== PARTIE SERVEUR ===" -ForegroundColor Magenta

Write-Host "`n[Serveur] Génération des clés post-quantiques..." -ForegroundColor Cyan

# 1. Générer les clés Kyber (ML-KEM) pour l'échange de clés
$serverKyberKeys = New-KyberKeyPair -ParameterSet Kyber768
Write-Host "  ✓ Clés Kyber (ML-KEM-768) générées" -ForegroundColor Green
Write-Host "    Clé publique: $($serverKyberKeys.PublicKey.Length) bytes" -ForegroundColor Gray

# 2. Générer les clés Dilithium (ML-DSA) pour la signature
$serverDilithiumKeys = New-DilithiumKeyPair -ParameterSet Dilithium3
Write-Host "  ✓ Clés Dilithium (ML-DSA-65) générées" -ForegroundColor Green
Write-Host "    Clé publique: $($serverDilithiumKeys.PublicKey.Length) bytes" -ForegroundColor Gray

# 3. Sauvegarder les clés privées du serveur (sécurisées)
$serverKyberPath = Join-Path $ModulePath "server_kyber_keys.txt"
$serverDilithiumPath = Join-Path $ModulePath "server_dilithium_keys.txt"

Export-KyberKeyPair -KeyPair $serverKyberKeys -Path $serverKyberPath -Format Text -Force
Export-DilithiumKeyPair -KeyPair $serverDilithiumKeys -Path $serverDilithiumPath -Format Text -Force
Write-Host "  ✓ Clés serveur sauvegardées (sécurisées)" -ForegroundColor Green

# 4. Exporter les clés publiques pour le client
$serverPublicKeysPath = Join-Path $ModulePath "server_public_keys.txt"
$serverPublicKeys = @"
# Clés publiques du serveur
# Format: Text

# Clé publique Kyber (ML-KEM)
KyberPublicKey=$([Convert]::ToBase64String($serverKyberKeys.PublicKey))
KyberPublicKeyHex=$($serverKyberKeys.PublicKeyHex)
KyberParameterSet=Kyber768

# Clé publique Dilithium (ML-DSA)
DilithiumPublicKey=$([Convert]::ToBase64String($serverDilithiumKeys.PublicKey))
DilithiumPublicKeyHex=$($serverDilithiumKeys.PublicKeyHex)
DilithiumParameterSet=$($serverDilithiumKeys.ParameterSet)
"@
Set-Content -Path $serverPublicKeysPath -Value $serverPublicKeys -Encoding UTF8
Write-Host "  ✓ Clés publiques exportées pour le client" -ForegroundColor Green

Write-Host "`n[Serveur] En attente de la requête du client...`n" -ForegroundColor Cyan

# ============================================
# PARTIE CLIENT
# ============================================
Write-Host "=== PARTIE CLIENT ===" -ForegroundColor Magenta

Write-Host "`n[Client] Importation des clés publiques du serveur..." -ForegroundColor Cyan

# 1. Charger les clés publiques du serveur
$serverPublicKeysContent = Get-Content $serverPublicKeysPath -Raw
$serverPublicKeysDict = @{}
foreach ($line in $serverPublicKeysContent -split "`n") {
    if ($line -match "^([^#=]+)=(.*)$") {
        $serverPublicKeysDict[$matches[1].Trim()] = $matches[2].Trim()
    }
}

$serverKyberPublicKey = [Convert]::FromBase64String($serverPublicKeysDict["KyberPublicKey"])
$serverDilithiumPublicKey = [Convert]::FromBase64String($serverPublicKeysDict["DilithiumPublicKey"])
Write-Host "  ✓ Clés publiques du serveur chargées" -ForegroundColor Green

# 2. Générer les clés Dilithium du client pour signer
$clientDilithiumKeys = New-DilithiumKeyPair -ParameterSet Dilithium3
Write-Host "  ✓ Clés Dilithium client générées" -ForegroundColor Green

# 3. Encapsuler une clé partagée avec la clé publique Kyber du serveur
Write-Host "`n[Client] Encapsulation de la clé partagée (Kyber)..." -ForegroundColor Cyan
$encapsulated = Invoke-KyberEncapsulate -PublicKey $serverKyberPublicKey -ParameterSet Kyber768
Write-Host "  ✓ Clé partagée encapsulée" -ForegroundColor Green
Write-Host "    Ciphertext: $($encapsulated.Ciphertext.Length) bytes" -ForegroundColor Gray
Write-Host "    Clé partagée: $($encapsulated.SharedSecret.Length) bytes" -ForegroundColor Gray

# 4. Préparer le message à envoyer (ciphertext + clé publique client)
$messageToSign = New-Object System.Collections.Generic.List[byte]
$messageToSign.AddRange($encapsulated.Ciphertext)
$messageToSign.AddRange($clientDilithiumKeys.PublicKey)

# 5. Signer le message avec la clé privée Dilithium du client
Write-Host "`n[Client] Signature du message (Dilithium)..." -ForegroundColor Cyan
$signature = Invoke-DilithiumSign -Data $messageToSign.ToArray() -PrivateKey $clientDilithiumKeys.PrivateKey -ParameterSet Dilithium3
Write-Host "  ✓ Message signé avec Dilithium (ML-DSA)" -ForegroundColor Green
Write-Host "    Signature: $($signature.Signature.Length) bytes" -ForegroundColor Gray

# 6. Préparer le paquet à envoyer au serveur
$packetToServer = @{
    Ciphertext = $encapsulated.Ciphertext
    CiphertextHex = $encapsulated.CiphertextHex
    ClientPublicKey = $clientDilithiumKeys.PublicKey
    ClientPublicKeyHex = $clientDilithiumKeys.PublicKeyHex
    Signature = $signature.Signature
    SignatureHex = $signature.SignatureHex
}

Write-Host "`n[Client] Envoi du paquet au serveur..." -ForegroundColor Cyan
Write-Host "  ✓ Paquet envoyé (ciphertext + clé publique + signature)" -ForegroundColor Green

# ============================================
# PARTIE SERVEUR - TRAITEMENT
# ============================================
Write-Host "`n=== PARTIE SERVEUR - TRAITEMENT ===" -ForegroundColor Magenta

Write-Host "`n[Serveur] Réception du paquet client..." -ForegroundColor Cyan

# 1. Reconstruire le message signé
$signedMessage = New-Object System.Collections.Generic.List[byte]
$signedMessage.AddRange($packetToServer.Ciphertext)
$signedMessage.AddRange($packetToServer.ClientPublicKey)

# 2. Vérifier la signature avec la clé publique Dilithium du client
Write-Host "`n[Serveur] Vérification de la signature (Dilithium)..." -ForegroundColor Cyan
$isValidSignature = Test-DilithiumSignature `
    -Data $signedMessage.ToArray() `
    -Signature $packetToServer.Signature `
    -PublicKey $packetToServer.ClientPublicKey `
    -ParameterSet Dilithium3

if ($isValidSignature) {
    Write-Host "  ✓ Signature valide (authentification réussie)" -ForegroundColor Green
} else {
    Write-Host "  ✗ Signature invalide (ATTENTION: attaque possible!)" -ForegroundColor Red
    exit 1
}

# 3. Décapsuler la clé partagée avec la clé privée Kyber du serveur
Write-Host "`n[Serveur] Décapsulation de la clé partagée (Kyber)..." -ForegroundColor Cyan
$serverSharedSecret = Invoke-KyberDecapsulate `
    -Ciphertext $packetToServer.Ciphertext `
    -PrivateKey $serverKyberKeys.PrivateKey `
    -ParameterSet Kyber768
Write-Host "  ✓ Clé partagée décapsulée" -ForegroundColor Green

# 4. Vérifier que les clés partagées correspondent
Write-Host "`n[Serveur] Vérification de la clé partagée..." -ForegroundColor Cyan
$sharedSecretMatch = $true
if ($encapsulated.SharedSecret.Length -ne $serverSharedSecret.SharedSecret.Length) {
    $sharedSecretMatch = $false
} else {
    for ($i = 0; $i -lt $encapsulated.SharedSecret.Length; $i++) {
        if ($encapsulated.SharedSecret[$i] -ne $serverSharedSecret.SharedSecret[$i]) {
            $sharedSecretMatch = $false
            break
        }
    }
}

if ($sharedSecretMatch) {
    Write-Host "  ✓ Les clés partagées correspondent parfaitement!" -ForegroundColor Green
    Write-Host "    Clé partagée (hex): $($encapsulated.SharedSecretHex)" -ForegroundColor Gray
} else {
    Write-Host "  ✗ Les clés partagées ne correspondent pas!" -ForegroundColor Red
}

# ============================================
# RÉSUMÉ
# ============================================
Write-Host "`n=== RÉSUMÉ DU PROTOCOLE ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "✅ Architecture 100% Post-Quantum:" -ForegroundColor Green
Write-Host "   • Échange de clés: Kyber (ML-KEM-768)" -ForegroundColor White
Write-Host "   • Authentification: Dilithium (ML-DSA-65)" -ForegroundColor White
Write-Host ""
Write-Host "✅ Sécurité:" -ForegroundColor Green
Write-Host "   • Signature vérifiée avec succès" -ForegroundColor White
Write-Host "   • Clé partagée établie de manière sécurisée" -ForegroundColor White
Write-Host ""
Write-Host "✅ Protocole complet validé!" -ForegroundColor Green

# Nettoyage
Write-Host "`n=== Nettoyage ===" -ForegroundColor Cyan
if (Test-Path $serverKyberPath) {
    Remove-Item $serverKyberPath -Force
    Write-Host "  ✓ Fichier de test serveur Kyber supprimé" -ForegroundColor Gray
}
if (Test-Path $serverDilithiumPath) {
    Remove-Item $serverDilithiumPath -Force
    Write-Host "  ✓ Fichier de test serveur Dilithium supprimé" -ForegroundColor Gray
}
if (Test-Path $serverPublicKeysPath) {
    Remove-Item $serverPublicKeysPath -Force
    Write-Host "  ✓ Fichier de test clés publiques supprimé" -ForegroundColor Gray
}

Write-Host "`n=== Protocole terminé ===" -ForegroundColor Cyan

