# JellyRequest Build Script for PowerShell
# This script builds the plugin and creates a distributable package

param(
    [string]$Configuration = "Release",
    [string]$OutputPath = "dist",
    [switch]$Clean = $false,
    [switch]$SkipTests = $false
)

# Script variables
$ProjectName = "JellyRequest"
$ProjectPath = $PSScriptRoot
$DistPath = Join-Path $ProjectPath $OutputPath
$TempPath = Join-Path $ProjectPath "temp"
$ZipPath = Join-Path $DistPath "$ProjectName.zip"

Write-Host "=== JellyRequest Build Script ===" -ForegroundColor Green
Write-Host "Configuration: $Configuration" -ForegroundColor Yellow
Write-Host "Output Path: $OutputPath" -ForegroundColor Yellow
Write-Host ""

# Function to clean build artifacts
function Clean-BuildArtifacts {
    Write-Host "Cleaning build artifacts..." -ForegroundColor Cyan
    
    if (Test-Path $DistPath) {
        Remove-Item $DistPath -Recurse -Force
        Write-Host "Removed distribution directory" -ForegroundColor Gray
    }
    
    if (Test-Path $TempPath) {
        Remove-Item $TempPath -Recurse -Force
        Write-Host "Removed temporary directory" -ForegroundColor Gray
    }
    
    # Clean dotnet build cache
    dotnet clean $ProjectPath --configuration $Configuration --verbosity quiet
    Write-Host "Cleaned dotnet build cache" -ForegroundColor Gray
}

# Function to restore NuGet packages
function Restore-Packages {
    Write-Host "Restoring NuGet packages..." -ForegroundColor Cyan
    dotnet restore $ProjectPath --verbosity quiet
    if ($LASTEXITCODE -ne 0) {
        throw "Package restore failed"
    }
    Write-Host "Packages restored successfully" -ForegroundColor Green
}

# Function to build the project
function Build-Project {
    Write-Host "Building project..." -ForegroundColor Cyan
    dotnet build $ProjectPath --configuration $Configuration --no-restore --verbosity quiet
    if ($LASTEXITCODE -ne 0) {
        throw "Build failed"
    }
    Write-Host "Build completed successfully" -ForegroundColor Green
}

# Function to run tests
function Test-Project {
    if ($SkipTests) {
        Write-Host "Skipping tests..." -ForegroundColor Yellow
        return
    }
    
    Write-Host "Running tests..." -ForegroundColor Cyan
    dotnet test $ProjectPath --configuration $Configuration --no-build --verbosity quiet --logger "console;verbosity=minimal"
    if ($LASTEXITCODE -ne 0) {
        throw "Tests failed"
    }
    Write-Host "All tests passed" -ForegroundColor Green
}

# Function to publish the plugin
function Publish-Plugin {
    Write-Host "Publishing plugin..." -ForegroundColor Cyan
    
    # Create output directories
    New-Item -ItemType Directory -Path $DistPath -Force | Out-Null
    New-Item -ItemType Directory -Path $TempPath -Force | Out-Null
    
    # Publish the project
    dotnet publish $ProjectPath `
        --configuration $Configuration `
        --output $TempPath `
        --no-build `
        --verbosity quiet
    
    if ($LASTEXITCODE -ne 0) {
        throw "Publish failed"
    }
    
    Write-Host "Plugin published successfully" -ForegroundColor Green
}

# Function to create distribution package
function Create-Package {
    Write-Host "Creating distribution package..." -ForegroundColor Cyan
    
    # Copy only necessary files
    $FilesToCopy = @(
        "$ProjectName.dll",
        "$ProjectName.deps.json",
        "$ProjectName.pdb",
        "$ProjectName.runtimeconfig.json",
        "appsettings.json"
    )
    
    $PluginDistPath = Join-Path $DistPath $ProjectName
    New-Item -ItemType Directory -Path $PluginDistPath -Force | Out-Null
    
    foreach ($File in $FilesToCopy) {
        $SourceFile = Join-Path $TempPath $File
        if (Test-Path $SourceFile) {
            Copy-Item $SourceFile $PluginDistPath
            Write-Host "Copied: $File" -ForegroundColor Gray
        }
    }
    
    # Copy Web assets
    $WebSourcePath = Join-Path $ProjectPath "Web"
    $WebDestPath = Join-Path $PluginDistPath "Web"
    if (Test-Path $WebSourcePath) {
        Copy-Item $WebSourcePath $WebDestPath -Recurse
        Write-Host "Copied Web assets" -ForegroundColor Gray
    }
    
    # Copy Configuration page
    $ConfigSourcePath = Join-Path $ProjectPath "Configuration"
    $ConfigDestPath = Join-Path $PluginDistPath "Configuration"
    if (Test-Path $ConfigSourcePath) {
        Copy-Item $ConfigSourcePath $ConfigDestPath -Recurse
        Write-Host "Copied Configuration assets" -ForegroundColor Gray
    }
    
    # Create ZIP package
    Compress-Archive -Path $PluginDistPath -DestinationPath $ZipPath -Force
    Write-Host "Created ZIP package: $ZipPath" -ForegroundColor Green
    
    # Create checksum
    $Checksum = (Get-FileHash -Path $ZipPath -Algorithm SHA256).Hash
    $Checksum | Out-File -FilePath "$ZipPath.sha256" -Encoding UTF8
    Write-Host "Created SHA256 checksum" -ForegroundColor Gray
}

# Function to display build summary
function Show-Summary {
    Write-Host ""
    Write-Host "=== Build Summary ===" -ForegroundColor Green
    Write-Host "Plugin: $ProjectName" -ForegroundColor White
    Write-Host "Configuration: $Configuration" -ForegroundColor White
    Write-Host "Output: $DistPath" -ForegroundColor White
    Write-Host ""
    
    if (Test-Path $ZipPath) {
        $Size = (Get-Item $ZipPath).Length / 1MB
        Write-Host "Package created: $ZipPath" -ForegroundColor Green
        Write-Host "Package size: $([math]::Round($Size, 2)) MB" -ForegroundColor Green
        Write-Host ""
        Write-Host "Installation:" -ForegroundColor Yellow
        Write-Host "1. Extract $ProjectName.zip to your Jellyfin plugins directory" -ForegroundColor White
        Write-Host "2. Restart Jellyfin service" -ForegroundColor White
        Write-Host "3. Configure plugin in Jellyfin dashboard" -ForegroundColor White
    }
    
    Write-Host ""
}

# Main execution
try {
    # Clean if requested
    if ($Clean) {
        Clean-BuildArtifacts
        if (-not $PSBoundParameters.ContainsKey("Configuration")) {
            Write-Host "Clean completed. Exiting..." -ForegroundColor Green
            exit 0
        }
    }
    
    # Build process
    Restore-Packages
    Build-Project
    Test-Project
    Publish-Plugin
    Create-Package
    Show-Summary
    
    Write-Host "Build completed successfully!" -ForegroundColor Green
    exit 0
}
catch {
    Write-Host ""
    Write-Host "=== Build Failed ===" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host ""
    Write-Host "Please check the error messages above and try again." -ForegroundColor Yellow
    exit 1
}
finally {
    # Cleanup temporary files
    if (Test-Path $TempPath) {
        Remove-Item $TempPath -Recurse -Force
    }
}
