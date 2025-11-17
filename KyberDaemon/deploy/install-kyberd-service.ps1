param(
    [string]$Runtime = "win-x64",
    [string]$Configuration = "C:\ProgramData\KyberDaemon\kyberd.conf",
    [string]$PublishDirectory = "$PSScriptRoot\..\publish\$Runtime",
    [string]$ServiceName = "KyberDaemon",
    [string]$DisplayName = "KyberDaemon Post-Quantum Service"
)

if (!(Test-Path $PublishDirectory)) {
    Write-Host "Compilation de KyberDaemon en Release ($Runtime)..." -ForegroundColor Cyan
    dotnet publish "$PSScriptRoot\..\KyberDaemon.csproj" -c Release -r $Runtime --self-contained false -o $PublishDirectory | Out-Null
}

$dllPath = Join-Path $PublishDirectory "KyberDaemon.dll"
$exePath = Join-Path $PublishDirectory "KyberDaemon.exe"

if (Test-Path $exePath) {
    $binaryPath = "`"$exePath`""
} elseif (Test-Path $dllPath) {
    try {
        $dotnetPath = (Get-Command dotnet -ErrorAction Stop).Source
    } catch {
        throw "dotnet introuvable. Installez le runtime .NET ou publiez en mode self-contained."
    }
    $binaryPath = "`"$dotnetPath`" `"$dllPath`""
} else {
    throw "Aucun binaire KyberDaemon trouvé dans $PublishDirectory"
}

if (!(Test-Path $Configuration)) {
    Write-Host "Création du dossier de configuration et copie des fichiers par défaut" -ForegroundColor Yellow
    $configDir = Split-Path $Configuration -Parent
    if (!(Test-Path $configDir)) {
        New-Item -ItemType Directory -Path $configDir -Force | Out-Null
    }
    # Copier kyberd.conf et adapter les chemins relatifs
    $kyberd = Get-Content "$PSScriptRoot\..\config\kyberd.conf"
    $kyberd | ForEach-Object {
        $_ -replace 'auth_config\s*=\s*.*', "auth_config = $(Join-Path $configDir 'auth.conf')" `
           -replace 'keys_directory\s*=\s*.*', "keys_directory = $(Join-Path $configDir 'keys')" `
           -replace 'key_encryption_mode\s*=\s*.*', 'key_encryption_mode = dpapi          # dpapi (Windows) ou passphrase' `
           -replace 'key_encryption_passphrase\s*=\s*.*', 'key_encryption_passphrase =                   # obligatoire si key_encryption_mode=passphrase'
    } | Set-Content $Configuration

    # Copier auth.conf et créer le dossier keys (vide)
    $authSource = "$PSScriptRoot\..\config\auth.conf"
    if (Test-Path $authSource) {
        Copy-Item $authSource -Destination (Join-Path $configDir "auth.conf") -Force
    }
    $keysDir = Join-Path $configDir "keys"
    if (!(Test-Path $keysDir)) {
        New-Item -ItemType Directory -Path $keysDir -Force | Out-Null
    }
}

Write-Host "Binaire utilisé : $binaryPath" -ForegroundColor DarkGray
Write-Host "Configuration : $Configuration" -ForegroundColor DarkGray

$binPath = ('{0} --config "{1}"' -f $binaryPath, $Configuration)

Write-Host "Command line : $binPath" -ForegroundColor DarkGray

Write-Host "Création du service Windows $ServiceName" -ForegroundColor Cyan
sc.exe create $ServiceName binPath= "$binPath" DisplayName= "$DisplayName" start= auto | Out-Null