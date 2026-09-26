param (
    [string]$ConnectionString = "Host=localhost;Port=5433;Database=enjoyeveryday;Username=postgres;Password=postgres"
)

Write-Host "==========================================" -ForegroundColor Cyan
Write-Host " EnjoyEveryday Startup Script" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "This script will start both the Web UI and the API backend."
Write-Host "Ensure you have a PostgreSQL database running at:"
Write-Host "$ConnectionString" -ForegroundColor Yellow
Write-Host ""

# Ensure the DB connection string is set via environment variable so the app can pick it up
$env:ConnectionStrings__DefaultConnection = $ConnectionString

# Build the entire solution first sequentially to prevent DLL locking
Write-Host "Building the solution..." -ForegroundColor Green
dotnet build "$PSScriptRoot\EnjoyEveryday.slnx"
if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed! Please fix the errors before starting." -ForegroundColor Red
    exit $LASTEXITCODE
}

# Start the API Project in a new PowerShell window
Write-Host "Starting API Project..." -ForegroundColor Green
$ApiCmd = "cd '$PSScriptRoot\src\EnjoyEveryday.Api'; `$env:ConnectionStrings__DefaultConnection = '$ConnectionString'; dotnet run --no-build"
Start-Process powershell.exe -ArgumentList "-NoExit", "-Command", $ApiCmd

# Start the Daycare Web Project in a new PowerShell window
Write-Host "Starting Daycare Web Project..." -ForegroundColor Green
$WebCmd = "cd '$PSScriptRoot\src\EnjoyEveryday.Daycare.Web'; dotnet run --no-build"
Start-Process powershell.exe -ArgumentList "-NoExit", "-Command", $WebCmd

# # Start the Admin Web Project in a new PowerShell window
# Write-Host "Starting Admin Web Project..." -ForegroundColor Green
# $AdminCmd = "cd '$PSScriptRoot\src\EnjoyEveryday.Admin.Web'; dotnet run --no-build"
# Start-Process powershell.exe -ArgumentList "-NoExit", "-Command", $AdminCmd

# Start the Parent Web Project in a new PowerShell window
# Write-Host "Starting Parent Web Project..." -ForegroundColor Green
# $ParentCmd = "cd '$PSScriptRoot\src\EnjoyEveryday.Parent.Web'; dotnet run --no-build"
# Start-Process powershell.exe -ArgumentList "-NoExit", "-Command", $ParentCmd

Write-Host ""
Write-Host "All applications are starting up in new windows." -ForegroundColor Cyan
Write-Host "API:               https://localhost:7081 or http://localhost:5266"
Write-Host "Daycare Web UI:    https://localhost:7003 or http://localhost:5074"
Write-Host "Admin Web UI:      https://localhost:7004 or http://localhost:5075"
Write-Host "Parent Web UI:     https://localhost:7005 or http://localhost:5076"
Write-Host ""
Write-Host "Once the Web UI is running, open it in your browser and you can browse the Teacher Dashboard!" -ForegroundColor Cyan
