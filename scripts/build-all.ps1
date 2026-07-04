param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$solutionRoot = Resolve-Path (Join-Path $scriptRoot "..")

Write-Host "==> Build global KyberModule ($Configuration)" -ForegroundColor Cyan

$projects = @(
    "KyberDomain/KyberDomain.csproj",
    "KyberLibrary/KyberLibrary.csproj",
    "KyberShared/KyberShared.csproj",
    "KyberDaemon/KyberDaemon.csproj",
    "KyberCLI/KyberCLI.csproj"
)

$results = @()

foreach ($project in $projects)
{
    $fullPath = Join-Path $solutionRoot $project
    Write-Host "-- Compilation : $project" -ForegroundColor Yellow
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    dotnet build $fullPath -c $Configuration | Write-Host
    $sw.Stop()
    $results += [PSCustomObject]@{
        Project = $project
        Duration = "{0:N2} s" -f $sw.Elapsed.TotalSeconds
    }
}

Write-Host "`n==> Résumé" -ForegroundColor Green
$results | Format-Table -AutoSize

Write-Host "Build global terminé avec succès." -ForegroundColor Green
