<#
.SYNOPSIS
    Setup script for BookingService development environment.

.DESCRIPTION
    This script prepares the development environment by:
    - Checking .NET SDK version
    - Installing Aspire workload if not present
    - Restoring NuGet packages
    - Building the solution

.EXAMPLE
    .\setup.ps1
    
.EXAMPLE
    .\setup.ps1 -SkipBuild
#>

param(
    [switch]$SkipBuild
)

$ErrorActionPreference = "Stop"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  BookingService Setup Script" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Check .NET SDK
Write-Host "[1/4] Checking .NET SDK..." -ForegroundColor Yellow
$dotnetVersion = dotnet --version
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: .NET SDK not found. Please install .NET 9.0 SDK from https://dotnet.microsoft.com/download" -ForegroundColor Red
    exit 1
}
Write-Host "  Found .NET SDK version: $dotnetVersion" -ForegroundColor Green

$majorVersion = [int]($dotnetVersion.Split('.')[0])
if ($majorVersion -lt 9) {
    Write-Host "WARNING: .NET 9.0 or later is recommended. Current version: $dotnetVersion" -ForegroundColor Yellow
}

# Check and install Aspire workload
Write-Host ""
Write-Host "[2/4] Checking Aspire workload..." -ForegroundColor Yellow
$workloads = dotnet workload list
if ($workloads -notmatch "aspire") {
    Write-Host "  Aspire workload not found. Installing..." -ForegroundColor Yellow
    dotnet workload install aspire
    if ($LASTEXITCODE -ne 0) {
        Write-Host "ERROR: Failed to install Aspire workload. Try running as Administrator." -ForegroundColor Red
        exit 1
    }
    Write-Host "  Aspire workload installed successfully." -ForegroundColor Green
} else {
    Write-Host "  Aspire workload is already installed." -ForegroundColor Green
}

# Check Docker
Write-Host ""
Write-Host "[3/4] Checking Docker..." -ForegroundColor Yellow
$dockerRunning = $false
try {
    $dockerInfo = docker info 2>&1
    if ($LASTEXITCODE -eq 0) {
        $dockerRunning = $true
        Write-Host "  Docker is running." -ForegroundColor Green
    }
} catch {
    # Docker command failed
}

if (-not $dockerRunning) {
    Write-Host "  WARNING: Docker is not running or not installed." -ForegroundColor Yellow
    Write-Host "  Docker is required to run with Aspire (containerized SQL Server)." -ForegroundColor Yellow
    Write-Host "  You can still run without Docker using LocalDB." -ForegroundColor Yellow
}

# Restore and build
Write-Host ""
Write-Host "[4/4] Restoring packages and building..." -ForegroundColor Yellow
dotnet restore
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Package restore failed." -ForegroundColor Red
    exit 1
}
Write-Host "  Packages restored successfully." -ForegroundColor Green

if (-not $SkipBuild) {
    dotnet build --no-restore
    if ($LASTEXITCODE -ne 0) {
        Write-Host "ERROR: Build failed." -ForegroundColor Red
        exit 1
    }
    Write-Host "  Build completed successfully." -ForegroundColor Green
}

# Summary
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Setup Complete!" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "To start the application:" -ForegroundColor White
Write-Host ""

if ($dockerRunning) {
    Write-Host "  With Aspire (recommended):" -ForegroundColor Green
    Write-Host "    cd BookingService.AppHost" -ForegroundColor Gray
    Write-Host "    dotnet run" -ForegroundColor Gray
    Write-Host ""
}

Write-Host "  Without Docker (LocalDB):" -ForegroundColor Green
Write-Host "    cd BookingService.Api" -ForegroundColor Gray
Write-Host "    `$env:Jwt__Key = 'YourSecretKey_MinimumLength32Characters!'" -ForegroundColor Gray
Write-Host "    dotnet run" -ForegroundColor Gray
Write-Host ""

Write-Host "See GETTING-STARTED.md for detailed documentation." -ForegroundColor White
Write-Host ""
