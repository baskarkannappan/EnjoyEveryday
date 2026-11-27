<#
.SYNOPSIS
    Enterprise deployment, health-verified rollout, and instant rollback helper for EnjoyEveryday.

.EXAMPLE
    .\scripts\deploy.ps1 -Service ai-service -Tag sha-1a2b3c4
    .\scripts\deploy.ps1 -Service ai-service -Rollback
    .\scripts\deploy.ps1 -Status
    .\scripts\deploy.ps1 -Verify
#>
[CmdletBinding()]
param(
    [ValidateSet("ai-service", "api", "web", "admin-web", "parent-web", "all")]
    [string]$Service,

    [string]$Tag,

    [switch]$Rollback,

    [switch]$Status,

    [switch]$Verify,

    [int]$HealthTimeoutSeconds = 60
)

$ErrorActionPreference = "Stop"
$RepoRoot = Split-Path -Parent $PSScriptRoot
Push-Location $RepoRoot

$HistoryFile = Join-Path $RepoRoot ".deploy-history.json"
$EnvFile = Join-Path $RepoRoot ".env"

$ServiceTagVars = @{
    "ai-service" = "AI_TAG"
    "api"        = "API_TAG"
    "web"        = "UI_TAG"
    "admin-web"  = "ADMIN_UI_TAG"
    "parent-web" = "PARENT_UI_TAG"
}

function Get-DeployHistory {
    if (Test-Path $HistoryFile) {
        $raw = Get-Content $HistoryFile -Raw
        if (-not [string]::IsNullOrWhiteSpace($raw)) {
            $ht = @{}
            try {
                $parsed = $raw | ConvertFrom-Json
                foreach ($prop in $parsed.PSObject.Properties) {
                    $ht[$prop.Name] = @{
                        current   = $prop.Value.current
                        previous  = $prop.Value.previous
                        updatedAt = $prop.Value.updatedAt
                    }
                }
            } catch {
                # Fallback if unparseable
            }
            return $ht
        }
    }
    return @{}
}

function Save-DeployHistory([hashtable]$History) {
    $History | ConvertTo-Json -Depth 5 | Set-Content -Path $HistoryFile -Encoding UTF8
}

function Set-EnvTag([string]$VarName, [string]$Value) {
    [Environment]::SetEnvironmentVariable($VarName, $Value, "Process")
    if (Test-Path $EnvFile) {
        $lines = Get-Content $EnvFile
        $updated = $false
        $newLines = foreach ($line in $lines) {
            if ($line -match "^$VarName=") {
                $updated = $true
                "$VarName=$Value"
            } else {
                $line
            }
        }
        if (-not $updated) {
            $newLines += "$VarName=$Value"
        }
        $newLines | Set-Content -Path $EnvFile -Encoding UTF8
    }
}

function Wait-ServiceHealthy([string]$Svc, [int]$TimeoutSec) {
    $deadline = (Get-Date).AddSeconds($TimeoutSec)
    while ((Get-Date) -lt $deadline) {
        $cid = (docker compose ps -q $Svc 2>$null).Trim()
        if (-not [string]::IsNullOrWhiteSpace($cid)) {
            $state = (docker inspect --format "{{if .State.Health}}{{.State.Health.Status}}{{else}}{{.State.Status}}{{end}}" $cid 2>$null).Trim()
            if ($state -eq "healthy" -or $state -eq "running") {
                # If container defines a healthcheck, require 'healthy'
                $hasHealth = (docker inspect --format "{{if .State.Health}}yes{{else}}no{{end}}" $cid 2>$null).Trim()
                if ($hasHealth -eq "no" -or $state -eq "healthy") {
                    return $true
                }
            }
            if ($state -eq "exited" -or $state -eq "dead") {
                return $false
            }
        }
        Start-Sleep -Seconds 3
    }
    return $false
}

try {
    if ($Status) {
        docker compose ps
        exit 0
    }

    if ($Verify) {
        Write-Host "==> Checking container status..." -ForegroundColor Cyan
        docker compose ps

        Write-Host "==> Verifying internal .NET (api) -> FastAPI (ai-service) /health..." -ForegroundColor Cyan
        docker compose exec -T api curl -sf http://ai-service:8000/health
        Write-Host ""

        Write-Host "==> Verifying safe AI endpoint round-trip (/api/ideas)..." -ForegroundColor Cyan
        $payload = '{"idea":"Autumn Leaf Sensory Table"}'
        docker compose exec -T api curl -sf -X POST http://ai-service:8000/api/ideas `
            -H "Content-Type: application/json" `
            -H "X-Tenant-Api-Key: DEFAULT_AI_KEY" `
            -H "X-Tenant-Id: 10000000-0000-0000-0000-000000000001" `
            -d $payload | Out-Null

        Write-Host "ALL VERIFICATION CHECKS PASSED." -ForegroundColor Green
        exit 0
    }

    if (-not $Service) {
        throw "Specify -Service <name> with -Tag <tag> or -Rollback, or use -Status / -Verify."
    }

    if ($Service -eq "all") {
        if ($Rollback) {
            throw "Specify an individual service when using -Rollback."
        }
        $targetTag = if ([string]::IsNullOrWhiteSpace($Tag)) { "latest" } else { $Tag }
        Write-Host "==> Ensuring PostgreSQL (db) is running and healthy..." -ForegroundColor Cyan
        docker compose up -d db
        if (-not (Wait-ServiceHealthy -Svc "db" -TimeoutSec $HealthTimeoutSeconds)) {
            throw "PostgreSQL (db) failed health check."
        }
        foreach ($svc in @("ai-service", "api", "web", "admin-web", "parent-web")) {
            $var = $ServiceTagVars[$svc]
            Set-EnvTag -VarName $var -Value $targetTag
            Write-Host "==> Deploying '$svc' ($targetTag)..." -ForegroundColor Cyan
            docker compose pull $svc
            docker compose up -d --no-deps $svc
            if (-not (Wait-ServiceHealthy -Svc $svc -TimeoutSec $HealthTimeoutSeconds)) {
                throw "Deployment of '$svc' ($targetTag) failed health check."
            }
            $prev = if ($history.ContainsKey($svc)) { $history[$svc].current } else { "latest" }
            $history[$svc] = @{
                current   = $targetTag
                previous  = $prev
                updatedAt = (Get-Date).ToString("o")
            }
        }
        Save-DeployHistory $history
        Write-Host "Full stack deployment succeeded on tag '$targetTag'." -ForegroundColor Green
        exit 0
    }

    $tagVar = $ServiceTagVars[$Service]
    $history = Get-DeployHistory
    $entry = if ($history.ContainsKey($Service)) { $history[$Service] } else { @{ current = "latest"; previous = $null } }

    if ($Rollback) {
        if ([string]::IsNullOrWhiteSpace($entry.previous)) {
            throw "No previous tag recorded for '$Service' in $HistoryFile."
        }
        $targetTag = $entry.previous
        Write-Host "==> Rolling back '$Service' from '$($entry.current)' to '$targetTag'..." -ForegroundColor Yellow
        Set-EnvTag -VarName $tagVar -Value $targetTag
        docker compose pull $Service
        docker compose up -d --no-deps $Service

        if (-not (Wait-ServiceHealthy -Svc $Service -TimeoutSec $HealthTimeoutSeconds)) {
            throw "Rollback of '$Service' to '$targetTag' failed health check."
        }

        $history[$Service] = @{
            current   = $targetTag
            previous  = $entry.current
            updatedAt = (Get-Date).ToString("o")
        }
        Save-DeployHistory $history
        Write-Host "Rollback succeeded: '$Service' is healthy on tag '$targetTag'." -ForegroundColor Green
        exit 0
    }

    if ([string]::IsNullOrWhiteSpace($Tag)) {
        throw "Specify -Tag <git-sha-or-tag> when deploying '$Service'."
    }

    $previousTag = $entry.current
    Write-Host "==> Deploying '$Service' with tag '$Tag' (previous: '$previousTag')..." -ForegroundColor Cyan
    Set-EnvTag -VarName $tagVar -Value $Tag
    docker compose pull $Service
    docker compose up -d --no-deps $Service

    if (-not (Wait-ServiceHealthy -Svc $Service -TimeoutSec $HealthTimeoutSeconds)) {
        Write-Warning "Health check failed for '$Service' on tag '$Tag'. Auto-rolling back to '$previousTag'..."
        Set-EnvTag -VarName $tagVar -Value $previousTag
        docker compose up -d --no-deps $Service
        throw "Deployment of '$Service' ($Tag) failed health check and was rolled back to '$previousTag'."
    }

    $history[$Service] = @{
        current   = $Tag
        previous  = $previousTag
        updatedAt = (Get-Date).ToString("o")
    }
    Save-DeployHistory $history
    Write-Host "Deployment succeeded: '$Service' is healthy on tag '$Tag' (rollback target: '$previousTag')." -ForegroundColor Green
}
finally {
    Pop-Location
}

