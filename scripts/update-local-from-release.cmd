@echo off
setlocal
echo ====================================================================
echo  EnjoyEveryday: Pull and Update Local Docker Desktop from Release
echo ====================================================================

powershell -NoProfile -ExecutionPolicy Bypass -Command ".\scripts\deploy.ps1 -Service all -Tag release"
if %ERRORLEVEL% neq 0 (
    echo [ERROR] Deployment failed. Check Docker Desktop status.
    exit /b %ERRORLEVEL%
)

echo.
echo ====================================================================
echo  [SUCCESS] All containers updated from release branch!
echo  Daycare Portal:    http://localhost:8081
echo  Platform Admin:    http://localhost:8084
echo  Parent Portal:     http://localhost:8085
echo  Backend REST API:  http://localhost:8082/swagger
echo ====================================================================
