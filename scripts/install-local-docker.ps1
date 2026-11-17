<#
.SYNOPSIS
    Downloads and installs EnjoyEveryday containers from GHCR into local Docker Desktop
    WITHOUT disturbing your existing local PostgreSQL database.

.EXAMPLE
    .\scripts\install-local-docker.ps1
    .\scripts\install-local-docker.ps1 -Tag 819ed040 -GitHubUser baskarkannappan -GitHubToken <PAT>
#>
[CmdletBinding()]
param(
    [string]$Tag = "latest",
    [string]$Owner = "baskarkannappan",
    [string]$GitHubUser = "",
    [string]$GitHubToken = "",
    [switch]$LocalBuild,
    [int]$HealthTimeoutSeconds = 60
)

$ErrorActionPreference = "Stop"
$RepoRoot = Split-Path -Parent $PSScriptRoot
Push-Location $RepoRoot

function Test-PortListening([string]$HostName, [int]$Port) {
    try {
        $client = New-Object System.Net.Sockets.TcpClient
        $iar = $client.BeginConnect($HostName, $Port, $null, $null)
        $ok = $iar.AsyncWaitHandle.WaitOne(1000, $false)
        if ($ok -and $client.Connected) {
            $client.EndConnect($iar)
            $client.Close()
            return $true
        }
        $client.Close()
    } catch { }
    return $false
}

function Set-EnvValue([string]$FilePath, [string]$Key, [string]$Value) {
    $lines = if (Test-Path $FilePath) { Get-Content $FilePath } else { @() }
    $found = $false
    $updated = foreach ($line in $lines) {
        if ($line -match "^$Key=") {
            $found = $true
            "$Key=$Value"
        } else {
            $line
        }
    }
    if (-not $found) { $updated += "$Key=$Value" }
    $updated | Set-Content -Path $FilePath -Encoding UTF8
}

try {
    Write-Host "=== EnjoyEveryday Local Docker Desktop Installer ===" -ForegroundColor Cyan

    # 1. Verify Docker Desktop is running
    docker info *> $null
    if ($LASTEXITCODE -ne 0) {
        throw "Docker Desktop is not running. Please start Docker Desktop and re-run this script."
    }

    # 2. Prepare .env without overwriting existing user secrets
    $envFile = Join-Path $RepoRoot ".env"
    if (-not (Test-Path $envFile)) {
        Copy-Item (Join-Path $RepoRoot ".env.compose.example") $envFile
        Write-Host "[OK] Created .env from .env.compose.example (add your GEMINI_API_KEY in .env if needed)." -ForegroundColor Green
    } else {
        Write-Host "[OK] Preserving existing .env configuration." -ForegroundColor Green
    }

    Set-EnvValue -FilePath $envFile -Key "GHCR_OWNER" -Value $Owner
    Set-EnvValue -FilePath $envFile -Key "AI_TAG" -Value $Tag
    Set-EnvValue -FilePath $envFile -Key "API_TAG" -Value $Tag
    Set-EnvValue -FilePath $envFile -Key "UI_TAG" -Value $Tag

    # 3. Detect existing local PostgreSQL on port 5433 or stopped 'db' container and NEVER recreate it
    $dbAlreadyRunningOn5433 = Test-PortListening -HostName "127.0.0.1" -Port 5433
    $composeDbId = (docker compose ps -a -q db 2>$null).Trim()

    if ($dbAlreadyRunningOn5433 -and [string]::IsNullOrWhiteSpace($composeDbId)) {
        Write-Host "[OK] Existing PostgreSQL detected on localhost:5433 — leaving it untouched." -ForegroundColor Yellow
        Write-Host "     Routing containers to host.docker.internal:5433." -ForegroundColor Yellow
        Set-EnvValue -FilePath $envFile -Key "DB_HOST" -Value "host.docker.internal"
        Set-EnvValue -FilePath $envFile -Key "DB_PORT" -Value "5433"
    } elseif (-not [string]::IsNullOrWhiteSpace($composeDbId)) {
        $dbRunning = (docker inspect --format "{{.State.Running}}" $composeDbId 2>$null).Trim()
        if ($dbRunning -ne "true") {
            Write-Host "[OK] Starting existing Compose 'db' container without recreating it..." -ForegroundColor Yellow
            docker start $composeDbId | Out-Null
        } else {
            Write-Host "[OK] Existing Compose 'db' container is already running — leaving it untouched." -ForegroundColor Yellow
        }
        Set-EnvValue -FilePath $envFile -Key "DB_HOST" -Value "db"
        Set-EnvValue -FilePath $envFile -Key "DB_PORT" -Value "5432"
    } else {
        Write-Host "[INFO] No local PostgreSQL detected on 5433. Starting Compose 'db' once..." -ForegroundColor Cyan
        Set-EnvValue -FilePath $envFile -Key "DB_HOST" -Value "db"
        Set-EnvValue -FilePath $envFile -Key "DB_PORT" -Value "5432"
        docker compose up -d db
    }

    # 4. Optional GHCR login if credentials are supplied (for private packages)
    if (-not [string]::IsNullOrWhiteSpace($GitHubToken)) {
        $loginUser = if ([string]::IsNullOrWhiteSpace($GitHubUser)) { $Owner } else { $GitHubUser }
        Write-Host "==> Logging into ghcr.io as $loginUser..." -ForegroundColor Cyan
        $GitHubToken | docker login ghcr.io -u $loginUser --password-stdin
    }

    # 5. Pull from GHCR or build locally, then start app containers with --no-deps
    $appServices = @("ai-service", "api", "web", "admin-web", "parent-web")
    if ($LocalBuild) {
        Write-Host "==> Building & starting application containers locally (leaving DB untouched)..." -ForegroundColor Cyan
        docker compose -f docker-compose.yml -f docker-compose.local.yml up -d --build --no-deps @appServices
    } else {
        Write-Host "==> Pulling application images (tag: $Tag) from ghcr.io/$Owner..." -ForegroundColor Cyan
        docker compose pull @appServices
        if ($LASTEXITCODE -ne 0) {
            throw "Failed to pull images from ghcr.io. If packages are private, pass -GitHubUser <user> -GitHubToken <PAT with read:packages>, or re-run with -LocalBuild."
        }

        Write-Host "==> Starting application containers in Docker Desktop..." -ForegroundColor Cyan
        docker compose up -d --no-deps @appServices
    }

    # 7. Wait for ai-service healthcheck
    Write-Host "==> Waiting for ai-service healthcheck..." -ForegroundColor Cyan
    $deadline = (Get-Date).AddSeconds($HealthTimeoutSeconds)
    $healthy = $false
    while ((Get-Date) -lt $deadline) {
        $cid = (docker compose ps -q ai-service).Trim()
        if ($cid) {
            $status = (docker inspect --format "{{.State.Health.Status}}" $cid 2>$null).Trim()
            if ($status -eq "healthy") { $healthy = $true; break }
        }
        Start-Sleep -Seconds 3
    }
    if (-not $healthy) {
        throw "ai-service did not become healthy within $HealthTimeoutSeconds seconds. Check: docker compose logs ai-service"
    }

    # 8. Verify internal .NET API -> FastAPI communication
    Write-Host "==> Verifying .NET API -> FastAPI internal connection..." -ForegroundColor Cyan
    docker compose exec -T api curl -sf http://ai-service:8000/health | Out-Null

    Write-Host "`n=== Deployment Complete (Database Untouched) ===" -ForegroundColor Green
    docker compose ps
    Write-Host "`nURLs:" -ForegroundColor Cyan
    Write-Host "  Daycare Web : http://localhost:8081"
    Write-Host "  REST API    : http://localhost:8082"
    Write-Host "  Admin Web   : http://localhost:8084"
    Write-Host "  Parent Web  : http://localhost:8085"
    Write-Host "  AI Service  : internal only (http://ai-service:8000)"
}
finally {
    Pop-Location
}

