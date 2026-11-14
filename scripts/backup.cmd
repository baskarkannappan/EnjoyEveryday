@echo off
setlocal enabledelayedexpansion

set PGPASSWORD=postgres
set PG_BIN=C:\Program Files\PostgreSQL\18\bin
set DB_NAME=enjoyeveryday
set DB_HOST=localhost
set DB_PORT=5433
set DB_USER=postgres

for /f %%I in ('powershell -NoProfile -Command "Get-Date -Format 'yyyyMMdd_HHmm'"') do set TIMESTAMP=%%I

if not exist "database\backups" mkdir "database\backups"
if not exist "database_backups" mkdir "database_backups"

echo [1/3] Creating plain SQL backup: database\backups\%DB_NAME%_backup_%TIMESTAMP%.sql ...
"%PG_BIN%\pg_dump.exe" -h %DB_HOST% -p %DB_PORT% -U %DB_USER% -d %DB_NAME% --clean --if-exists -f "database\backups\%DB_NAME%_backup_%TIMESTAMP%.sql"
if %ERRORLEVEL% neq 0 (
    echo Error: pg_dump plain SQL failed!
    exit /b %ERRORLEVEL%
)

echo [2/3] Creating binary dump: database\backups\%DB_NAME%_backup_%TIMESTAMP%.dump ...
"%PG_BIN%\pg_dump.exe" -h %DB_HOST% -p %DB_PORT% -U %DB_USER% -d %DB_NAME% -F c -f "database\backups\%DB_NAME%_backup_%TIMESTAMP%.dump"
if %ERRORLEVEL% neq 0 (
    echo Error: pg_dump custom format failed!
    exit /b %ERRORLEVEL%
)

echo [3/3] Updating tracked database_backups\%DB_NAME%_latest.sql ...
copy /Y "database\backups\%DB_NAME%_backup_%TIMESTAMP%.sql" "database_backups\%DB_NAME%_latest.sql" >nul

echo Backup completed successfully at %TIMESTAMP%.
