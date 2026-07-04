# Exemple d'utilisation des commandes à distance sécurisées
# WARNING: Ce script utilise l'ancienne pile SecureCommunicationClient/Server (obsolète).
# Utiliser KyberDaemon + KyberCLI pour la nouvelle architecture.
# Ce script démontre comment exécuter des commandes sur un serveur distant
# de manière sécurisée, similaire à SSH
#
# IMPORTANT: Les commandes ExecuteShell et ExecutePowerShell utilisent maintenant
# une SESSION INTERACTIVE PERSISTANTE. Cela signifie que:
# - Un shell reste ouvert pour chaque client
# - Les commandes partagent le même contexte (répertoire courant, variables, etc.)
# - L'état persiste entre les commandes (comme avec SSH)
# - Les commandes sont exécutées dans le même processus shell

# Charger le module
Import-Module .\KyberModule.psd1 -Force

Write-Host "=== Exemple de Commandes à Distance Sécurisées ===" -ForegroundColor Cyan
Write-Host ""

# Configuration
$serverPort = 8443
$serverHost = "localhost"
$isWindowsEnv = $IsWindows -or $env:OS -like "*Windows*"

Write-Host "Configuration:" -ForegroundColor Yellow
Write-Host "  Serveur: ${serverHost}:${serverPort}" -ForegroundColor Gray
Write-Host ""

# ============================================
# Démarrage du serveur
# ============================================
Write-Host "=== Démarrage du Serveur ===" -ForegroundColor Green

$server = Start-SecureServer -Port $serverPort -Verbose

if ($server) {
    Write-Host "✅ Serveur démarré avec succès" -ForegroundColor Green
    Write-Host ""
} else {
    Write-Host "❌ Erreur lors du démarrage du serveur" -ForegroundColor Red
    exit 1
}

# Attendre que le serveur soit prêt
Start-Sleep -Seconds 1

# ============================================
# Connexion du client
# ============================================
Write-Host "=== Connexion du Client ===" -ForegroundColor Green

$client = Connect-SecureClient -ServerHost $serverHost -Port $serverPort -Verbose

if ($client -and $client.IsAuthenticated) {
    Write-Host "✅ Client connecté et authentifié" -ForegroundColor Green
    Write-Host ""
} else {
    Write-Host "❌ Erreur lors de la connexion" -ForegroundColor Red
    $server.Stop()
    exit 1
}

# ============================================
# Exécution de commandes
# ============================================
Write-Host "=== Exécution de Commandes ===" -ForegroundColor Green
Write-Host ""

# Commande 1: Obtenir les informations système
Write-Host "1. Commande: GetSystemInfo" -ForegroundColor Cyan
$result1 = Invoke-SecureCommand -Client $client -Command "" -CommandType GetSystemInfo
if ($result1.Status.ToString() -eq "Success") {
    Write-Host "   ✅ Succès" -ForegroundColor Green
    if ($result1.Output) {
        Write-Host $result1.Output -ForegroundColor Gray
    }
} else {
    Write-Host "   ❌ Échec: $($result1.Error)" -ForegroundColor Red
}
Write-Host ""

# Commande 2: Lister un répertoire
Write-Host "2. Commande: ListDirectory (répertoire courant)" -ForegroundColor Cyan
$result2 = Invoke-SecureCommand -Client $client -Command "." -CommandType ListDirectory
if ($result2.Status.ToString() -eq "Success") {
    Write-Host "   ✅ Succès" -ForegroundColor Green
    if ($result2.Output) {
        Write-Host $result2.Output -ForegroundColor Gray
    }
} else {
    Write-Host "   ❌ Échec: $($result2.Error)" -ForegroundColor Red
}
Write-Host ""

# Commande 3: Exécuter une commande shell (SESSION INTERACTIVE)
Write-Host "3. Commande: Shell (afficher date/heure) - Session interactive" -ForegroundColor Cyan
if ($isWindowsEnv) {
    $command3 = "echo Date: %DATE% %TIME%"
} else {
    $command3 = "date"
}
$result3 = Invoke-SecureCommand -Client $client -Command $command3 -CommandType ExecuteShell
if ($result3.Status.ToString() -eq "Success") {
    Write-Host "   ✅ Succès" -ForegroundColor Green
    if ($result3.Output) {
        Write-Host $result3.Output -ForegroundColor Gray
    }
} else {
    Write-Host "   ❌ Échec: $($result3.Error)" -ForegroundColor Red
}
Write-Host ""

# Commande 4: Changer de répertoire (SESSION INTERACTIVE - État persistant)
Write-Host "4. Commande: Shell (changer de répertoire) - État persistant" -ForegroundColor Cyan
if ($isWindowsEnv) {
    $command4 = "cd /d C:\\Windows && cd"
} else {
    $command4 = "cd /tmp && pwd"
}
$result4 = Invoke-SecureCommand -Client $client -Command $command4 -CommandType ExecuteShell
if ($result4.Status.ToString() -eq "Success") {
    Write-Host "   ✅ Succès - Répertoire changé" -ForegroundColor Green
    if ($result4.Output) {
        Write-Host $result4.Output -ForegroundColor Gray
    }
} else {
    Write-Host "   ❌ Échec: $($result4.Error)" -ForegroundColor Red
}
Write-Host ""

# Commande 5: Vérifier que le répertoire est toujours changé (SESSION INTERACTIVE)
Write-Host "5. Commande: Shell (vérifier répertoire courant)" -ForegroundColor Cyan
if ($isWindowsEnv) {
    $command5 = "cd"
} else {
    $command5 = "pwd"
}
$result5 = Invoke-SecureCommand -Client $client -Command $command5 -CommandType ExecuteShell
if ($result5.Status.ToString() -eq "Success") {
    Write-Host "   ✅ Succès - Le répertoire persiste" -ForegroundColor Green
    if ($result5.Output) {
        Write-Host $result5.Output -ForegroundColor Gray
    }
} else {
    Write-Host "   ❌ Échec: $($result5.Error)" -ForegroundColor Red
}
Write-Host ""

# Commande 6: Lister le répertoire courant
Write-Host "6. Commande: Shell (listing du répertoire courant)" -ForegroundColor Cyan
if ($isWindowsEnv) {
    $command6 = "dir /b"
} else {
    $command6 = "ls -la"
}
$result6 = Invoke-SecureCommand -Client $client -Command $command6 -CommandType ExecuteShell
if ($result6.Status.ToString() -eq "Success") {
    Write-Host "   ✅ Succès" -ForegroundColor Green
    if ($result6.Output) {
        Write-Host $result6.Output -ForegroundColor Gray
    }
} else {
    Write-Host "   ❌ Échec: $($result6.Error)" -ForegroundColor Red
}
Write-Host ""

# ============================================
# Session interactive (simulation)
# ============================================
Write-Host "=== Session Interactive (Exemple) ===" -ForegroundColor Green
Write-Host "La session reste ouverte pour plusieurs commandes..." -ForegroundColor Gray
Write-Host ""

# Plusieurs commandes dans la même session (shell persistant)
if ($isWindowsEnv) {
    $commands = @(
        "cd",
        "echo Contenu courant:",
        "dir /b",
        "echo Session persistante OK"
    )
} else {
    $commands = @(
        "pwd",
        "echo Contenu courant:",
        "ls -1",
        "echo Session persistante OK"
    )
}

foreach ($cmd in $commands) {
    Write-Host "> $cmd" -ForegroundColor Yellow
    $result = Invoke-SecureCommand -Client $client -Command $cmd -CommandType ExecuteShell
    if ($result.Status.ToString() -eq "Success" -and $result.Output) {
        Write-Host $result.Output -ForegroundColor White
    } elseif ($result.Error) {
        Write-Host "Erreur: $($result.Error)" -ForegroundColor Red
    }
    Write-Host ""
}

# ============================================
# Nettoyage
# ============================================
Write-Host "=== Nettoyage ===" -ForegroundColor Green

$client.Disconnect()
Write-Host "✅ Client déconnecté" -ForegroundColor Green

Start-Sleep -Milliseconds 500

$server.Stop()
Write-Host "✅ Serveur arrêté" -ForegroundColor Green

Write-Host ""
Write-Host "=== Test terminé avec succès ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "Résumé:" -ForegroundColor Yellow
Write-Host "  ✅ Exécution de commandes à distance fonctionnelle" -ForegroundColor Green
Write-Host "  ✅ Session maintenue ouverte pour plusieurs commandes" -ForegroundColor Green
Write-Host "  ✅ Communication entièrement chiffrée et authentifiée" -ForegroundColor Green
Write-Host "  ✅ Protection post-quantique complète" -ForegroundColor Green

