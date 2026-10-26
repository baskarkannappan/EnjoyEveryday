<#
.SYNOPSIS
    Restores the PostgreSQL database backup for EnjoyEveryday.
.DESCRIPTION
    Restores the complete database (Schema + Data) from 01_FullDatabaseBackup_WithData.sql
    or a specified SQL backup file into the PostgreSQL container or instance.
.PARAMETER ContainerName
    The Docker container name (defaults to 'enjoyeveryday_db').
.PARAMETER DatabaseName
    The target database name (defaults to 'enjoyeveryday').
.PARAMETER Username
    The PostgreSQL username (defaults to 'postgres').
.PARAMETER BackupFile
    Path to the SQL backup file. Defaults to '01_FullDatabaseBackup_WithData.sql' in the same folder.
#>

[CmdletBinding()]
param(
    [string]$ContainerName = "enjoyeveryday_db",
    [string]$DatabaseName = "enjoyeveryday",
    [string]$Username = "postgres",
    [string]$BackupFile = ""
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($BackupFile)) {
    $BackupFile = Join-Path $PSScriptRoot "01_FullDatabaseBackup_WithData.sql"
}

if (-not (Test-Path $BackupFile)) {
    Write-Error "Backup file not found: $BackupFile"
    exit 1
}

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " EnjoyEveryday - PostgreSQL Database Restore              " -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "Target Database : $DatabaseName" -ForegroundColor Gray
Write-Host "Docker Container: $ContainerName" -ForegroundColor Gray
Write-Host "Source Backup   : $BackupFile" -ForegroundColor Gray
Write-Host ""

$confirmation = Read-Host "Are you sure you want to restore into '$DatabaseName'? This may overwrite existing data. (y/N)"
if ($confirmation -ne 'y' -and $confirmation -ne 'Y') {
    Write-Host "Restore operation cancelled." -ForegroundColor Yellow
    exit 0
}

Write-Host "Restoring database from $BackupFile ..." -ForegroundColor Yellow

# Copy file into container or stream via stdin
Get-Content -Path $BackupFile -Encoding utf8 -Raw | docker exec -i $ContainerName psql -U $Username -d $DatabaseName

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "==========================================================" -ForegroundColor Green
    Write-Host " Database Restored Successfully!                          " -ForegroundColor Green
    Write-Host "==========================================================" -ForegroundColor Green
} else {
    Write-Host "Warning: psql exited with code $LASTEXITCODE. Check any warnings above." -ForegroundColor Yellow
}
