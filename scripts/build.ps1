#!/usr/bin/env pwsh
# Build script for DevMind solution

param(
    [Parameter(Mandatory = False)]
    [string] = "Debug",
    
    [Parameter(Mandatory = False)]
    [switch],
    
    [Parameter(Mandatory = False)]
    [switch],
    
    [Parameter(Mandatory = False)]
    [switch],
    
    [Parameter(Mandatory = False)]
    [switch]
)

Stop = "Stop"

C:\Users\sean_\source\DevMind\DevMind.sln = "DevMind.sln"
 = Split-Path C:\Users\sean_\source

Write-Host "Building DevMind solution..." -ForegroundColor Green
Write-Host "Configuration: " -ForegroundColor Yellow
Write-Host "Solution Root: " -ForegroundColor Yellow

Set-Location 

try {
    if () {
        Write-Host "Cleaning solution..." -ForegroundColor Yellow
        dotnet clean C:\Users\sean_\source\DevMind\DevMind.sln --configuration 
    }

    if () {
        Write-Host "Restoring packages..." -ForegroundColor Yellow
        dotnet restore C:\Users\sean_\source\DevMind\DevMind.sln
    }

    Write-Host "Building solution..." -ForegroundColor Yellow
    dotnet build C:\Users\sean_\source\DevMind\DevMind.sln --configuration  --no-restore:True

    if () {
        Write-Host "Running tests..." -ForegroundColor Yellow
        dotnet test C:\Users\sean_\source\DevMind\DevMind.sln --configuration  --no-build --verbosity normal
    }

    if () {
        Write-Host "Creating packages..." -ForegroundColor Yellow
        dotnet pack C:\Users\sean_\source\DevMind\DevMind.sln --configuration  --no-build --output ./artifacts/packages
    }

    Write-Host "Build completed successfully!" -ForegroundColor Green
}
catch {
    Write-Host "Build failed: " -ForegroundColor Red
    exit 1
}
