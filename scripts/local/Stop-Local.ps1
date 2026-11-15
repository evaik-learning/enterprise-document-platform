<#
.SYNOPSIS
  Stops everything started by Start-Local.ps1 (including child processes such as Vite and the dotnet hosts).
#>
$ErrorActionPreference = 'SilentlyContinue'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$pidFile = Join-Path $repoRoot '.local\pids.json'
if (-not (Test-Path $pidFile)) { Write-Host 'Nothing to stop.'; return }

$entries = Get-Content $pidFile -Raw | ConvertFrom-Json
foreach ($property in $entries.PSObject.Properties) {
    # /T kills the whole tree (cmd -> npm -> node, dotnet run -> host).
    taskkill /PID $property.Value /T /F *> $null
    Write-Host "stopped $($property.Name)"
}
Remove-Item $pidFile -Force
