[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string] $ConnectionString,

    [switch] $ScriptOnly
)

$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$project = Join-Path $repoRoot 'src\Shared\Edp.Persistence\Edp.Persistence.csproj'

if (-not (Test-Path -LiteralPath $project)) {
    throw "Edp.Persistence project was not found at $project"
}

$env:EdpDb_Connection = $ConnectionString
$artifacts = Join-Path $repoRoot 'artifacts'
New-Item -ItemType Directory -Path $artifacts -Force | Out-Null
$commonArguments = @(
    'ef', 'database', 'update',
    '--project', $project,
    '--context', 'Edp.Persistence.EdpDbContext'
)

if ($ScriptOnly) {
    & dotnet ef migrations script --idempotent --project $project --context Edp.Persistence.EdpDbContext --output (Join-Path $artifacts 'EdpDb-idempotent.sql')
    if ($LASTEXITCODE -ne 0) { throw "Failed to generate the idempotent migration script." }
    Write-Host 'Generated artifacts\EdpDb-idempotent.sql. Review it before applying.'
    exit 0
}

Write-Host 'Applying the single EdpDbContext migration set...'
& dotnet @commonArguments
if ($LASTEXITCODE -ne 0) { throw "EdpDbContext migration failed." }
Write-Host 'EdpDbContext migrations applied successfully.'
