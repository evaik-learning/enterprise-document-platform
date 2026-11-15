<#
.SYNOPSIS
  Runs the whole platform on this machine without containers: Azurite, the database, 9 services, the Gateway and the web app.

.DESCRIPTION
  * Storage: Azurite (npx azurite).
  * Database: SQL Server LocalDB by default; pass -SqlConnection to use any other SQL Server.
  * Messaging: not started. Without a Service Bus connection string the services use a no-op publisher, so the UI works
    but cross-service notifications/audit events are not delivered. Use `docker compose up` for the full event flow.
  * Sign-in: Gateway "DevAuth" demo users (Development only). Nothing here touches Azure or Entra ID.
  * Secrets: a random JWT signing key is generated once into the git-ignored .local folder.
  Logs and process ids are written to .local\logs and .local\pids.json. Stop with scripts\local\Stop-Local.ps1.

.EXAMPLE
  .\scripts\local\Start-Local.ps1
  .\scripts\local\Start-Local.ps1 -SqlConnection "Server=localhost,1433;Database=EdpDb;User Id=sa;Password=...;TrustServerCertificate=True"
#>
[CmdletBinding()]
param(
    [string] $SqlConnection = 'Server=(localdb)\MSSQLLocalDB;Database=EdpDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True',
    [string] $SigningDbConnection = 'Server=(localdb)\MSSQLLocalDB;Database=EdpSigningDb;Trusted_Connection=True;TrustServerCertificate=True',
    [switch] $SkipBuild,
    [switch] $SkipWeb
)

$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$localDir = Join-Path $repoRoot '.local'
$logDir = Join-Path $localDir 'logs'
New-Item -ItemType Directory -Force -Path $logDir, (Join-Path $localDir 'azurite') | Out-Null

function Test-Command([string] $name, [string] $hint) {
    if (-not (Get-Command $name -ErrorAction SilentlyContinue)) { throw "'$name' was not found. $hint" }
}

Write-Host '== Checking prerequisites' -ForegroundColor Cyan
Test-Command dotnet 'Install the .NET SDK version in global.json.'
Test-Command node 'Install Node.js 22 or newer.'
Test-Command npm 'Install Node.js 22 or newer.'
if (-not $SkipWeb -and -not (Test-Path (Join-Path $repoRoot 'src\Web\Edp.Web\node_modules'))) {
    Write-Host 'Installing web dependencies (npm ci)...'
    Push-Location (Join-Path $repoRoot 'src\Web\Edp.Web'); try { npm ci; if ($LASTEXITCODE) { throw 'npm ci failed.' } } finally { Pop-Location }
}
& dotnet ef --version *> $null
if ($LASTEXITCODE -ne 0) { Write-Host 'Installing dotnet-ef (one-time)...'; dotnet tool install --global dotnet-ef | Out-Null }

# Per-machine signing key shared by the Gateway (issues demo tokens) and the services (validate them).
$keyFile = Join-Path $localDir 'jwt-dev-key.txt'
if (-not (Test-Path $keyFile)) {
    $bytes = New-Object byte[] 48
    $generator = [System.Security.Cryptography.RandomNumberGenerator]::Create()
    $generator.GetBytes($bytes)
    $generator.Dispose()
    [Convert]::ToBase64String($bytes) | Set-Content -Path $keyFile -NoNewline
}
$jwtKey = (Get-Content $keyFile -Raw).Trim()

$services = [ordered]@{
    Identity         = @{ Port = 59038; Project = 'src\Services\Identity\Edp.Identity.Api' }
    Organization     = @{ Port = 59050; Project = 'src\Services\Organization\Edp.Organization.Api' }
    Template         = @{ Port = 53954; Project = 'src\Services\Template\Edp.Template.Api' }
    Document         = @{ Port = 59042; Project = 'src\Services\Document\Edp.Document.Api' }
    Workflow         = @{ Port = 59048; Project = 'src\Services\Workflow\Edp.Workflow.Api' }
    Storage          = @{ Port = 59047; Project = 'src\Services\Storage\Edp.Storage.Api' }
    Notification     = @{ Port = 59044; Project = 'src\Services\Notification\Edp.Notification.Api' }
    Audit            = @{ Port = 59040; Project = 'src\Services\Audit\Edp.Audit.Api' }
    DigitalSignature = @{ Port = 54735; Project = 'src\Services\DigitalSignature\Edp.DigitalSignature.Api' }
}
$gatewayPort = 5200
$webPort = 5173
$azurite = 'UseDevelopmentStorage=true'

# Environment inherited by every child process started below.
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:ConnectionStrings__EdpDb = $SqlConnection
$env:ConnectionStrings__SigningDb = $SigningDbConnection
$env:ConnectionStrings__BlobStorage = $azurite
$env:ConnectionStrings__Storage = $azurite
$env:Database__ApplyMigrations = 'false'
$env:Jwt__Key = $jwtKey
$env:Jwt__Issuer = 'edp-local'
$env:Jwt__Audience = 'edp-api'
$env:Jwt__Authority = ' '   # blank on purpose: validate the local HS256 key instead of an OIDC authority
$env:Gateway__FrontendBaseUrl = "http://localhost:$webPort"
$env:Gateway__PublicBaseUrl = "http://localhost:$webPort"
$env:Gateway__DevAuth__Enabled = 'true'
$env:Gateway__DevAuth__SigningKey = $jwtKey
$env:Gateway__DevAuth__Issuer = 'edp-local'
$env:Gateway__DevAuth__Audience = 'edp-api'
foreach ($name in $services.Keys) {
    Set-Item -Path "Env:Gateway__ServiceCommunication__DownstreamServices__$name" -Value "http://localhost:$($services[$name].Port)"
}
$env:VITE_DEV_GATEWAY_URL = "http://localhost:$gatewayPort"

$pids = [ordered]@{}
function Start-Background([string] $name, [string] $file, [string[]] $arguments, [string] $workingDirectory) {
    $log = Join-Path $logDir "$name.log"
    # Start-Process joins arguments without quoting, so quote anything containing whitespace (e.g. repo paths).
    $arguments = @($arguments | ForEach-Object { if ($_ -match '\s') { '"' + $_ + '"' } else { $_ } })
    $process = Start-Process -FilePath $file -ArgumentList $arguments -WorkingDirectory $workingDirectory -WindowStyle Hidden -PassThru `
        -RedirectStandardOutput $log -RedirectStandardError (Join-Path $logDir "$name.err.log")
    $script:pids[$name] = $process.Id
    Write-Host ("  started {0,-17} pid {1}" -f $name, $process.Id)
}

try {
    Write-Host '== Starting Azurite (blob/queue/table storage)' -ForegroundColor Cyan
    $azuriteCmd = (Get-Command azurite -ErrorAction SilentlyContinue)
    if ($azuriteCmd) {
        Start-Background 'azurite' 'cmd.exe' @('/c', 'azurite', '--silent', '--location', (Join-Path $localDir 'azurite'), '--skipApiVersionCheck', '--loose') $repoRoot
    } else {
        Test-Command npx 'Install Node.js (npx) or run: npm install -g azurite'
        Start-Background 'azurite' 'cmd.exe' @('/c', 'npx', '--yes', 'azurite', '--silent', '--location', (Join-Path $localDir 'azurite'), '--skipApiVersionCheck', '--loose') $repoRoot
    }

    Write-Host '== Applying database migrations' -ForegroundColor Cyan
    if ($SqlConnection -like '*(localdb)*') { sqllocaldb start MSSQLLocalDB | Out-Null }
    & (Join-Path $repoRoot 'scripts\database\Apply-EdpMigrations.ps1') -ConnectionString $SqlConnection
    $env:EdpDb_Connection = $null

    if (-not $SkipBuild) {
        Write-Host '== Building services (first run takes a few minutes)' -ForegroundColor Cyan
        foreach ($service in $services.Values) {
            dotnet build (Join-Path $repoRoot $service.Project) -c Debug --nologo -v q
            if ($LASTEXITCODE -ne 0) { throw "Build failed for $($service.Project)" }
        }
        dotnet build (Join-Path $repoRoot 'src\Gateway\Edp.Gateway') -c Debug --nologo -v q
        if ($LASTEXITCODE -ne 0) { throw 'Gateway build failed.' }
    }

    Write-Host '== Starting services' -ForegroundColor Cyan
    foreach ($name in $services.Keys) {
        $service = $services[$name]
        Start-Background $name 'dotnet' @('run', '--no-build', '--no-launch-profile', '--project', (Join-Path $repoRoot $service.Project), '--urls', "http://localhost:$($service.Port)") $repoRoot
    }
    Start-Background 'Gateway' 'dotnet' @('run', '--no-build', '--no-launch-profile', '--project', (Join-Path $repoRoot 'src\Gateway\Edp.Gateway'), '--urls', "http://localhost:$gatewayPort") $repoRoot

    if (-not $SkipWeb) {
        Write-Host '== Starting web app' -ForegroundColor Cyan
        Start-Background 'Web' 'cmd.exe' @('/c', 'npm', 'run', 'dev', '--', '--host', 'localhost', '--port', "$webPort", '--strictPort') (Join-Path $repoRoot 'src\Web\Edp.Web')
    }
} catch {
    $pids | ConvertTo-Json | Set-Content (Join-Path $localDir 'pids.json')
    Write-Host "Startup failed: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host 'Stopping anything that was started...'
    & (Join-Path $PSScriptRoot 'Stop-Local.ps1')
    exit 1
}

$pids | ConvertTo-Json | Set-Content (Join-Path $localDir 'pids.json')

Write-Host '== Waiting for the Gateway' -ForegroundColor Cyan
$deadline = (Get-Date).AddSeconds(120)
$ready = $false
while ((Get-Date) -lt $deadline -and -not $ready) {
    try { $ready = (Invoke-WebRequest "http://localhost:$gatewayPort/health/live" -UseBasicParsing -TimeoutSec 3).StatusCode -eq 200 } catch { Start-Sleep -Seconds 2 }
}
if (-not $ready) { Write-Warning "The Gateway did not become healthy in time. Check $logDir" }

Write-Host ''
Write-Host "Ready: http://localhost:$webPort   (sign in with a demo user)" -ForegroundColor Green
Write-Host "Gateway: http://localhost:$gatewayPort | Logs: $logDir | Stop: scripts\local\Stop-Local.ps1 | Check: scripts\Smoke-Test.ps1 -BaseUrl http://localhost:5173"
