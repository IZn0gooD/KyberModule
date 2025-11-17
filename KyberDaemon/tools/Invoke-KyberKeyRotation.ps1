param(
    [ValidateSet('List','Rotate','Export')]
    [string]$Action = 'List',
    [string]$Config = 'config/kyberd.conf',
    [string]$Name = 'session',
    [int]$Days = 0,
    [string]$Output,
    [string]$Passphrase,
    [ValidateSet('dpapi','passphrase')]
    [string]$Mode,
    [string]$CliPath
)

function Resolve-CliBinary {
    param([string]$Path)

    if ($Path) {
        return (Resolve-Path -Path $Path).Path
    }

    $possible = @(
        Join-Path $PSScriptRoot '..' '..' 'KyberCLI' 'publish' 'win-x64' 'KyberCLI.exe'),
        Join-Path $PSScriptRoot '..' '..' 'KyberCLI' 'publish' 'linux-x64' 'KyberCLI',
        Join-Path $PSScriptRoot '..' '..' 'KyberCLI' 'publish' 'win-x64' 'KyberCLI.dll',
        Join-Path $PSScriptRoot '..' '..' 'KyberCLI' 'bin' 'Release' 'net8.0' 'publish' 'KyberCLI.dll'
    )

    foreach ($candidate in $possible) {
        if (Test-Path $candidate) {
            return (Resolve-Path $candidate).Path
        }
    }

    throw "Impossible de localiser KyberCLI (utilisez --CliPath pour préciser le chemin)."
}

$cliBinary = Resolve-CliBinary -Path $CliPath

$arguments = @('keys', $Action.ToLowerInvariant())
if ($Config) { $arguments += @('--config', $Config) }
if ($Name) { $arguments += @('--name', $Name) }
if ($Days -gt 0) { $arguments += @('--days', $Days.ToString()) }
if ($Output) { $arguments += @('--output', $Output) }
if ($Passphrase) { $arguments += @('--passphrase', $Passphrase) }
if ($Mode) { $arguments += @('--mode', $Mode.ToLowerInvariant()) }

Write-Host "Exécution de KyberCLI $($arguments -join ' ')" -ForegroundColor Cyan

if ($cliBinary -like '*.dll') {
    $dotnet = (Get-Command dotnet -ErrorAction Stop).Source
    & $dotnet $cliBinary @arguments
} else {
    & $cliBinary @arguments
}

if ($LASTEXITCODE -ne 0) {
    throw "KyberCLI a retourné le code $LASTEXITCODE"
}
