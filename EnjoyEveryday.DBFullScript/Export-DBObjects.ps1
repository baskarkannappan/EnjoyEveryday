<#
.SYNOPSIS
    Exports all PostgreSQL database objects and data from EnjoyEveryday into structured SQL backup files.
.DESCRIPTION
    This script connects to the PostgreSQL container or local PostgreSQL instance,
    extracts the complete schema DDL, modular object scripts (Tables, Functions, Triggers,
    Sequences, Extensions, Indexes), and full data backups for disaster recovery.
.PARAMETER ContainerName
    The Docker container name (defaults to 'enjoyeveryday_db').
.PARAMETER DatabaseName
    The target database name (defaults to 'enjoyeveryday').
.PARAMETER Username
    The PostgreSQL username (defaults to 'postgres').
.PARAMETER OutputDir
    The target directory where scripts are exported. Defaults to the folder containing this script.
#>

[CmdletBinding()]
param(
    [string]$ContainerName = "enjoyeveryday_db",
    [string]$DatabaseName = "enjoyeveryday",
    [string]$Username = "postgres",
    [string]$OutputDir = $PSScriptRoot
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($OutputDir)) {
    $OutputDir = $PSScriptRoot
}

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " EnjoyEveryday - PostgreSQL Database Full Object Backup   " -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "Target Database : $DatabaseName" -ForegroundColor Gray
Write-Host "Docker Container: $ContainerName" -ForegroundColor Gray
Write-Host "Output Directory: $OutputDir" -ForegroundColor Gray
Write-Host ""

# Ensure directory structure
$subdirs = @(
    "Tables",
    "Functions",
    "Triggers",
    "Indexes",
    "Sequences",
    "Extensions",
    "Constraints"
)

foreach ($dir in $subdirs) {
    $fullPath = Join-Path $OutputDir $dir
    if (-not (Test-Path $fullPath)) {
        New-Item -ItemType Directory -Path $fullPath -Force | Out-Null
    }
}

# Helper to execute command in docker container
function Invoke-PgSql {
    param([string]$Sql)
    $res = docker exec $ContainerName psql -U $Username -d $DatabaseName -t -A -c $Sql
    return $res
}

# 1. Export Complete Consolidated Schema DDL
Write-Host "[1/8] Exporting full consolidated schema DDL..." -ForegroundColor Yellow
$fullSchemaFile = Join-Path $OutputDir "00_FullSchema.sql"
docker exec $ContainerName pg_dump -U $Username -d $DatabaseName --schema-only --no-owner --no-privileges | Out-File -FilePath $fullSchemaFile -Encoding utf8
Write-Host "  -> Created $fullSchemaFile" -ForegroundColor Green

# 2. Export Full Database Backup (Schema + Data for Disaster Recovery)
Write-Host "[2/8] Exporting complete database backup (Schema + Data)..." -ForegroundColor Yellow
$fullBackupFile = Join-Path $OutputDir "01_FullDatabaseBackup_WithData.sql"
docker exec $ContainerName pg_dump -U $Username -d $DatabaseName --clean --if-exists --no-owner --no-privileges | Out-File -FilePath $fullBackupFile -Encoding utf8
Write-Host "  -> Created $fullBackupFile" -ForegroundColor Green

# 3. Export Data Only (INSERT format)
Write-Host "[3/8] Exporting data-only script (INSERT format)..." -ForegroundColor Yellow
$fullDataFile = Join-Path $OutputDir "02_FullDataOnly.sql"
docker exec $ContainerName pg_dump -U $Username -d $DatabaseName --data-only --inserts --no-owner --no-privileges | Out-File -FilePath $fullDataFile -Encoding utf8
Write-Host "  -> Created $fullDataFile" -ForegroundColor Green

# 4. Export Extensions
Write-Host "[4/8] Exporting extensions..." -ForegroundColor Yellow
$extFile = Join-Path $OutputDir "Extensions\01_extensions.sql"
$extSql = @"
-- EnjoyEveryday Database Extensions
-- Generated on $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')

CREATE EXTENSION IF NOT EXISTS pgcrypto WITH SCHEMA public;
CREATE EXTENSION IF NOT EXISTS "uuid-ossp" WITH SCHEMA public;
"@
$extSql | Out-File -FilePath $extFile -Encoding utf8
Write-Host "  -> Created $extFile" -ForegroundColor Green

# 5. Export Individual Table Schemas
Write-Host "[5/8] Exporting individual table definitions (130+ tables)..." -ForegroundColor Yellow
$tableNamesRaw = Invoke-PgSql "SELECT table_name FROM information_schema.tables WHERE table_schema = 'public' AND table_type = 'BASE TABLE' ORDER BY table_name;"
$tableNames = $tableNamesRaw -split "`r?`n" | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }

$tablesCount = 0
foreach ($table in $tableNames) {
    $table = $table.Trim()
    if ([string]::IsNullOrWhiteSpace($table)) { continue }
    $tableFile = Join-Path $OutputDir "Tables\$table.sql"
    docker exec $ContainerName pg_dump -U $Username -d $DatabaseName -t "public.$table" --schema-only --no-owner --no-privileges | Out-File -FilePath $tableFile -Encoding utf8
    $tablesCount++
}
Write-Host "  -> Successfully exported $tablesCount individual table files to Tables/" -ForegroundColor Green

# 6. Export Custom Functions & Procedures
Write-Host "[6/8] Exporting custom functions..." -ForegroundColor Yellow
$funcQuery = @"
SELECT proname, pg_get_functiondef(oid)
FROM pg_proc
WHERE pronamespace = 'public'::regnamespace
  AND proname NOT IN (
    'digest', 'hmac', 'crypt', 'gen_salt', 'encrypt', 'decrypt', 'encrypt_iv',
    'decrypt_iv', 'gen_random_bytes', 'gen_random_uuid', 'pgp_sym_encrypt',
    'pgp_sym_encrypt_bytea', 'pgp_sym_decrypt', 'pgp_sym_decrypt_bytea',
    'pgp_pub_encrypt', 'pgp_pub_encrypt_bytea', 'pgp_pub_decrypt',
    'pgp_pub_decrypt_bytea', 'pgp_key_id', 'armor', 'dearmor', 'pgp_armor_headers'
  )
ORDER BY proname;
"@

$funcNamesRaw = Invoke-PgSql "SELECT DISTINCT proname FROM pg_proc WHERE pronamespace = 'public'::regnamespace AND proname NOT IN ('digest', 'hmac', 'crypt', 'gen_salt', 'encrypt', 'decrypt', 'encrypt_iv', 'decrypt_iv', 'gen_random_bytes', 'gen_random_uuid', 'pgp_sym_encrypt', 'pgp_sym_encrypt_bytea', 'pgp_sym_decrypt', 'pgp_sym_decrypt_bytea', 'pgp_pub_encrypt', 'pgp_pub_encrypt_bytea', 'pgp_pub_decrypt', 'pgp_pub_decrypt_bytea', 'pgp_key_id', 'armor', 'dearmor', 'pgp_armor_headers') ORDER BY proname;"
$funcNames = $funcNamesRaw -split "`r?`n" | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }

$funcCount = 0
foreach ($func in $funcNames) {
    $func = $func.Trim()
    if ([string]::IsNullOrWhiteSpace($func)) { continue }
    $funcDef = Invoke-PgSql "SELECT pg_get_functiondef(oid) || ';' FROM pg_proc WHERE pronamespace = 'public'::regnamespace AND proname = '$func' LIMIT 1;"
    $funcFile = Join-Path $OutputDir "Functions\$func.sql"
    $header = "-- Function: public.$func`n-- Backup generated on $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')`n`n"
    ($header + $funcDef) | Out-File -FilePath $funcFile -Encoding utf8
    $funcCount++
}
Write-Host "  -> Successfully exported $funcCount custom functions to Functions/" -ForegroundColor Green

# 7. Export Triggers
Write-Host "[7/8] Exporting triggers..." -ForegroundColor Yellow
$trigNamesRaw = Invoke-PgSql "SELECT DISTINCT tgname FROM pg_trigger WHERE tgname NOT LIKE 'pg_%' AND NOT tgisinternal ORDER BY tgname;"
$trigNames = $trigNamesRaw -split "`r?`n" | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }

$trigCount = 0
foreach ($trig in $trigNames) {
    $trig = $trig.Trim()
    if ([string]::IsNullOrWhiteSpace($trig)) { continue }
    $trigDef = Invoke-PgSql "SELECT pg_get_triggerdef(oid) || ';' FROM pg_trigger WHERE tgname = '$trig' LIMIT 1;"
    $trigFile = Join-Path $OutputDir "Triggers\$trig.sql"
    $header = "-- Trigger: $trig`n-- Backup generated on $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')`n`n"
    ($header + $trigDef) | Out-File -FilePath $trigFile -Encoding utf8
    $trigCount++
}
Write-Host "  -> Successfully exported $trigCount triggers to Triggers/" -ForegroundColor Green

# 8. Export Indexes, Sequences & Constraints summary
Write-Host "[8/8] Exporting indexes and sequences..." -ForegroundColor Yellow
$indexesDef = Invoke-PgSql "SELECT pg_get_indexdef(i.indexrelid) || ';' FROM pg_index i JOIN pg_class c ON c.oid = i.indexrelid JOIN pg_namespace n ON n.oid = c.relnamespace WHERE n.nspname = 'public' ORDER BY c.relname;"
$idxFile = Join-Path $OutputDir "Indexes\all_indexes.sql"
("-- All Indexes Backup`n-- Generated on $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')`n`n" + $indexesDef) | Out-File -FilePath $idxFile -Encoding utf8

$seqDef = Invoke-PgSql "SELECT sequence_name FROM information_schema.sequences WHERE sequence_schema = 'public';"
$seqFile = Join-Path $OutputDir "Sequences\sequences.sql"
("-- Sequences Backup`n-- Generated on $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')`n`n" + $seqDef) | Out-File -FilePath $seqFile -Encoding utf8

Write-Host ""
Write-Host "==========================================================" -ForegroundColor Green
Write-Host " DB Objects Export Completed Successfully!                " -ForegroundColor Green
Write-Host " Target Folder: $OutputDir" -ForegroundColor Green
Write-Host "==========================================================" -ForegroundColor Green
