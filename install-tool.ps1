#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Installs or updates the MAUI CLI global tool
.DESCRIPTION
    Builds the MAUI CLI project and installs it as a global .NET tool
#>

Write-Host "Installing MAUI CLI..." -ForegroundColor Cyan

# Build in release mode
Write-Host "Building..." -ForegroundColor Yellow
dotnet build src\Maui.Cli\Maui.Cli.csproj -c Release
if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed" -ForegroundColor Red
    exit 1
}

# Pack the tool
Write-Host "Packing..." -ForegroundColor Yellow
dotnet pack src\Maui.Cli\Maui.Cli.csproj -c Release --no-build -o .\artifacts
if ($LASTEXITCODE -ne 0) {
    Write-Host "Pack failed" -ForegroundColor Red
    exit 1
}

# Uninstall existing version
Write-Host "Uninstalling existing version..." -ForegroundColor Yellow
dotnet tool uninstall -g Maui.Cli 2>$null

# Install new version
Write-Host "Installing new version..." -ForegroundColor Yellow
$nupkgPath = Get-ChildItem -Path .\artifacts -Filter "Maui.Cli.*.nupkg" | Select-Object -First 1 -ExpandProperty FullName
if (-not $nupkgPath) {
    Write-Host "Could not find package in artifacts folder" -ForegroundColor Red
    exit 1
}
dotnet tool install -g Maui.Cli --add-source .\artifacts

if ($LASTEXITCODE -eq 0) {
    Write-Host "`nMAUI CLI installed successfully!" -ForegroundColor Green
    Write-Host "Run 'maui --help' to get started" -ForegroundColor Cyan
} else {
    Write-Host "`nInstallation failed" -ForegroundColor Red
    exit 1
}
