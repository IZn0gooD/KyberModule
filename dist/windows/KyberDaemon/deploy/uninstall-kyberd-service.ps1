param(
    [string]$ServiceName = "KyberDaemon"
)

if (-not (Get-Service -Name $ServiceName -ErrorAction SilentlyContinue)) {
    Write-Warning "Service $ServiceName introuvable."
    return
}

Write-Host "Arrêt du service $ServiceName" -ForegroundColor Cyan
try {
    Stop-Service -Name $ServiceName -ErrorAction Stop
}
catch {
    Write-Warning "Impossible d'arrêter le service : $_"
}

Write-Host "Suppression du service $ServiceName" -ForegroundColor Cyan
sc.exe delete $ServiceName | Out-Null

Write-Host "Service $ServiceName supprimé." -ForegroundColor Green
