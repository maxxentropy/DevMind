#!/usr/bin/env pwsh
# Test script for DevMind solution

param(
    [Parameter(Mandatory = False)]
    [string] = "Debug",
    
    [Parameter(Mandatory = False)]
    [string] = "",
    
    [Parameter(Mandatory = False)]
    [switch],
    
    [Parameter(Mandatory = False)]
    [switch]
)

Stop = "Stop"

C:\Users\sean_\source\DevMind\DevMind.sln = "DevMind.sln"
 = Split-Path C:\Users\sean_\source

Write-Host "Running DevMind tests..." -ForegroundColor Green
Write-Host "Configuration: " -ForegroundColor Yellow

Set-Location 

try {
     = @(
        "test", C:\Users\sean_\source\DevMind\DevMind.sln,
        "--configuration", ,
        "--verbosity", "normal"
    )

    if () {
         += "--filter", 
    }

    if () {
         += "--collect", "XPlat Code Coverage"
         += "--results-directory", "./artifacts/coverage"
    }

    if () {
         += "--watch"
    }

    & dotnet @testArgs

    if ( -and !) {
        Write-Host "Generating coverage report..." -ForegroundColor Yellow
        
        # Install reportgenerator if not already installed
        dotnet tool install --global dotnet-reportgenerator-globaltool --ignore-failed-sources

        # Generate HTML report
        reportgenerator 
            "-reports:./artifacts/coverage/**/coverage.cobertura.xml" 
            "-targetdir:./artifacts/coverage/report" 
            "-reporttypes:Html;Cobertura"

        Write-Host "Coverage report generated at ./artifacts/coverage/report/index.html" -ForegroundColor Green
    }

    Write-Host "Tests completed successfully!" -ForegroundColor Green
}
catch {
    Write-Host "Tests failed: " -ForegroundColor Red
    exit 1
}
