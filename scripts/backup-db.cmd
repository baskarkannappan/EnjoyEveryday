@echo off
setlocal enabledelayedexpansion

set PGPASSWORD=postgres
set PG_BIN=C:\Program Files\PostgreSQL\18\bin
set BACKUP_DIR=%~dp0..\database\backups

if not exist "%BACKUP_DIR%" mkdir "%BACKUP_DIR%"

for /f "tokens=2 delims==" %%I in ('wmic os get localdatetime /value 2^>nul') do set DT=%%I
if "%DT%"=="" (
    set STAMP=%DATE:~10,4%%DATE:~4,2%%DATE:~7,2%_%TIME:~0,2%%TIME:~3,2%%TIME:~6,2%
    set STAMP=!STAMP: =0!
) else (
    set STAMP=%DT:~0,8%_%DT:~8,6%
)

set DUMP_FILE=%BACKUP_DIR%\enjoyeveryday_backup_%STAMP%.dump
set SQL_FILE=%BACKUP_DIR%\enjoyeveryday_backup_%STAMP%.sql

echo ============================================================
echo   EnjoyEveryday - PostgreSQL Local Safety Backup
echo ============================================================
echo Database: enjoyeveryday (Port 5433)
echo Target:   %BACKUP_DIR%
echo Timestamp: %STAMP%
echo.

echo [1/2] Creating compressed binary dump (.dump)...
"%PG_BIN%\pg_dump.exe" -h localhost -p 5433 -U postgres -F c -b -f "%DUMP_FILE%" enjoyeveryday
if %ERRORLEVEL% neq 0 (
    echo [ERROR] Failed to create binary dump!
    exit /b %ERRORLEVEL%
)
echo      -> Successfully created: %DUMP_FILE%

echo [2/2] Creating plain SQL script (.sql)...
"%PG_BIN%\pg_dump.exe" -h localhost -p 5433 -U postgres -F p -b -f "%SQL_FILE%" enjoyeveryday
if %ERRORLEVEL% neq 0 (
    echo [ERROR] Failed to create SQL dump!
    exit /b %ERRORLEVEL%
)
echo      -> Successfully created: %SQL_FILE%

echo.
echo ============================================================
echo   Backup Completed Successfully!
echo ============================================================
dir "%BACKUP_DIR%\enjoyeveryday_backup_%STAMP%.*"
