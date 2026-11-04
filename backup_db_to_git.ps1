param(
    [string]$BackupDir = "database_backups",
    [string]$DbHost = "localhost",
    [int]$DbPort = 5433,
    [string]$DbName = "enjoyeveryday",
    [string]$DbUser = "postgres"
)

$ErrorActionPreference = "Stop"

# Ensure target directory exists
if (-not (Test-Path $BackupDir)) {
    New-Item -ItemType Directory -Path $BackupDir -Force | Out-Null
}

$pgDumpPath = "C:\Program Files\PostgreSQL\18\bin\pg_dump.exe"
if (-not (Test-Path $pgDumpPath)) {
    # Fallback to PATH if specific version path is not present
    $pgDumpPath = "pg_dump"
}

$env:PGPASSWORD = "postgres"
$backupFile = Join-Path $BackupDir "enjoyeveryday_latest.sql"

Write-Host "Creating database dump..." -ForegroundColor Cyan
& $pgDumpPath -h $DbHost -p $DbPort -U $DbUser -d $DbName --clean --if-exists -f $backupFile

if ($LASTEXITCODE -ne 0) {
    Write-Error "Database dump failed!"
    exit 1
}

Write-Host "Database dumped successfully to $backupFile" -ForegroundColor Green

# Add, commit, and push to Git
Write-Host "Staging and committing to Git..." -ForegroundColor Cyan
git add $backupFile

$status = git status --porcelain $backupFile
if ($status) {
    $timestamp = (Get-Date).ToString("yyyy-MM-dd HH:mm:ss")
    git commit -m "chore(backup): auto-backup database snapshot - $timestamp"
    Write-Host "Pushing backup to GitHub..." -ForegroundColor Cyan
    git push origin master
    Write-Host "Backup successfully pushed to GitHub!" -ForegroundColor Green
} else {
    Write-Host "No changes detected in database snapshot. Nothing to commit." -ForegroundColor Yellow
}
