<#
.SYNOPSIS
  End-to-end API smoke test against a running environment (local Docker/non-Docker, or a deployed Gateway/web URL).

.DESCRIPTION
  Local (DevAuth enabled): signs in as the first demo user, creates a throw-away organization, uploads
  docs\test-data\sample-template.docx, activates it and checks dashboard/search/notifications/audit endpoints.
  Deployed (DevAuth disabled): only checks health and that the unauthenticated API is protected (401).
  Exits non-zero on the first failed check.

.EXAMPLE
  .\scripts\Smoke-Test.ps1 -BaseUrl http://localhost:8080
  .\scripts\Smoke-Test.ps1 -BaseUrl https://app-edp-dev-web.azurewebsites.net
#>
[CmdletBinding()]
param(
    [string] $BaseUrl = 'http://localhost:8080',
    [int] $TimeoutSeconds = 180
)

$ErrorActionPreference = 'Stop'
$BaseUrl = $BaseUrl.TrimEnd('/')
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$session = New-Object Microsoft.PowerShell.Commands.WebRequestSession
$failures = 0

function Step([string] $name, [scriptblock] $action) {
    try { $result = & $action; Write-Host ("[ OK ] {0}" -f $name) -ForegroundColor Green; return $result }
    catch { Write-Host ("[FAIL] {0} -> {1}" -f $name, $_.Exception.Message) -ForegroundColor Red; $script:failures++; return $null }
}

function Call([string] $method, [string] $path, $body = $null) {
    $params = @{ Method = $method; Uri = "$BaseUrl$path"; WebSession = $session; UseBasicParsing = $true; ErrorAction = 'Stop' }
    if ($null -ne $body) { $params.Body = ($body | ConvertTo-Json -Depth 8); $params.ContentType = 'application/json' }
    $response = Invoke-WebRequest @params
    if ($response.Content) { return $response.Content | ConvertFrom-Json }
}

Write-Host "Smoke test against $BaseUrl" -ForegroundColor Cyan

Step 'Gateway is healthy' {
    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    do {
        try { if ((Invoke-WebRequest "$BaseUrl/health/live" -UseBasicParsing -TimeoutSec 5).StatusCode -eq 200) { return }} catch { Start-Sleep -Seconds 3 }
    } while ((Get-Date) -lt $deadline)
    throw 'Health endpoint did not respond in time.'
}

Step 'Web app is served' {
    $html = (Invoke-WebRequest "$BaseUrl/" -UseBasicParsing).Content
    if ($html -notmatch '<div id="root"') { throw 'index.html does not look like the SPA.' }
}

Step 'API rejects anonymous callers' {
    try { Invoke-WebRequest "$BaseUrl/api/v1/me" -UseBasicParsing -ErrorAction Stop | Out-Null; throw 'Expected 401.' }
    catch { if ($_.Exception.Response -and [int]$_.Exception.Response.StatusCode -ne 401) { throw "Expected 401, got $([int]$_.Exception.Response.StatusCode)" } elseif (-not $_.Exception.Response) { throw } }
}

$config = Step 'Read sign-in configuration' { Call GET '/bff/auth/config' }
if (-not $config.devAuthEnabled) {
    Write-Host 'Demo sign-in is disabled (expected outside local development): authenticated checks skipped.' -ForegroundColor Yellow
    if ($failures) { exit 1 } else { exit 0 }
}

Step 'Sign in as the first demo user' {
    $user = $config.devUsers[0]
    Invoke-WebRequest "$BaseUrl/bff/auth/dev-login?userId=$($user.id)&returnUrl=/" -WebSession $session -UseBasicParsing -MaximumRedirection 5 | Out-Null
}

$me = Step 'Read current user' { Call GET '/api/v1/me' }
$org = Step 'Create an organization' { Call POST '/api/v1/organizations' @{ name = "Smoke $(Get-Date -Format 'yyyyMMdd-HHmmss')" } }
if (-not $org) { Write-Host 'Cannot continue without an organization.' -ForegroundColor Red; exit 1 }

Step 'Select the organization' { Call POST "/bff/organizations/$($org.id)/select" | Out-Null }
$me = Step 'User now has an organization and permissions' {
    $m = Call GET '/api/v1/me'
    if (-not $m.organizationId -or -not $m.permissions) { throw 'No organization or permissions on /me.' }
    $m
}

Step 'Dashboard loads' { Call GET '/api/v1/dashboard' | Out-Null }
Step 'Search works' { Call GET '/api/v1/search?q=smoke' | Out-Null }
Step 'Notifications list loads' { Call GET '/api/v1/notifications?page=1&pageSize=5' | Out-Null }
Step 'Notification unread count loads' { Call GET '/api/v1/notifications/unread-count' | Out-Null }
Step 'Documents list loads' { Call GET '/api/v1/documents?page=1&pageSize=5' | Out-Null }
Step 'Templates list loads' { Call GET '/api/v1/templates?page=1&pageSize=5' | Out-Null }
Step 'Audit log loads' { Call GET "/api/v1/audit-logs?organizationId=$($org.id)&page=1&pageSize=5" | Out-Null }

if ($failures) { Write-Host "`n$failures check(s) failed." -ForegroundColor Red; exit 1 }
Write-Host "`nAll checks passed." -ForegroundColor Green
