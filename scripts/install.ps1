# GoToWebinar CLI Installation Script for Windows
# This script downloads and installs the GoToWebinar CLI tool

param(
    [string]$InstallDir = "$env:LOCALAPPDATA\Programs\GoToWebinarCLI",
    [string]$BinaryName = "gotowebinar",
    [string]$Version = "",
    [switch]$AddToPath = $true,
    [switch]$Help
)

$ErrorActionPreference = "Stop"

# Configuration
$Repo = "stannardlabs/gotowebinar-cli"
$Platform = "win-x64"

# Colors for output
function Write-Info {
    param([string]$Message)
    Write-Host "[INFO] " -ForegroundColor Green -NoNewline
    Write-Host $Message
}

function Write-Warn {
    param([string]$Message)
    Write-Host "[WARN] " -ForegroundColor Yellow -NoNewline
    Write-Host $Message
}

function Write-Error {
    param([string]$Message)
    Write-Host "[ERROR] " -ForegroundColor Red -NoNewline
    Write-Host $Message
    exit 1
}

# Show help
function Show-Help {
    @"
GoToWebinar CLI Installer for Windows

Usage: .\install.ps1 [options]

Options:
  -InstallDir <path>    Installation directory (default: $env:LOCALAPPDATA\Programs\GoToWebinarCLI)
  -BinaryName <name>    Binary name (default: gotowebinar)
  -Version <version>    Specific version to install (default: latest)
  -AddToPath           Add to system PATH (default: true)
  -Help                Show this help message

Examples:
  .\install.ps1
  .\install.ps1 -InstallDir "C:\Tools" -BinaryName "gtw"
  .\install.ps1 -Version "v1.0.0"
"@
    exit 0
}

# Get the latest release version
function Get-LatestVersion {
    Write-Info "Fetching latest version..."
    
    try {
        $releases = Invoke-RestMethod -Uri "https://api.github.com/repos/$Repo/releases/latest"
        $latestVersion = $releases.tag_name
        
        if ([string]::IsNullOrEmpty($latestVersion)) {
            Write-Error "Unable to determine latest version"
        }
        
        Write-Info "Latest version: $latestVersion"
        return $latestVersion
    }
    catch {
        Write-Error "Failed to fetch latest version: $_"
    }
}

# Download the binary
function Download-Binary {
    param(
        [string]$Version,
        [string]$Platform
    )
    
    $downloadUrl = "https://github.com/$Repo/releases/download/$Version/gotowebinar-$Platform.exe.zip"
    $tempDir = [System.IO.Path]::GetTempPath()
    $downloadFile = Join-Path $tempDir "gotowebinar.zip"
    
    Write-Info "Downloading from: $downloadUrl"
    
    try {
        # Use TLS 1.2 for GitHub
        [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
        
        $progressPreference = 'silentlyContinue'
        Invoke-WebRequest -Uri $downloadUrl -OutFile $downloadFile
        $progressPreference = 'Continue'
        
        Write-Info "Extracting binary..."
        
        # Extract zip file
        $extractPath = Join-Path $tempDir "gotowebinar-extract"
        if (Test-Path $extractPath) {
            Remove-Item -Path $extractPath -Recurse -Force
        }
        
        Expand-Archive -Path $downloadFile -DestinationPath $extractPath -Force
        
        $binaryPath = Join-Path $extractPath "gotowebinar.exe"
        if (-not (Test-Path $binaryPath)) {
            Write-Error "Binary not found in archive"
        }
        
        return $binaryPath
    }
    catch {
        Write-Error "Failed to download binary: $_"
    }
    finally {
        # Clean up download file
        if (Test-Path $downloadFile) {
            Remove-Item -Path $downloadFile -Force
        }
    }
}

# Install the binary
function Install-Binary {
    param(
        [string]$SourcePath,
        [string]$InstallDir,
        [string]$BinaryName
    )
    
    Write-Info "Installing to $InstallDir\$BinaryName.exe..."
    
    # Create installation directory if it doesn't exist
    if (-not (Test-Path $InstallDir)) {
        New-Item -ItemType Directory -Path $InstallDir -Force | Out-Null
    }
    
    # Copy binary
    $targetPath = Join-Path $InstallDir "$BinaryName.exe"
    Copy-Item -Path $SourcePath -Destination $targetPath -Force
    
    # Verify installation
    if (Test-Path $targetPath) {
        Write-Info "Installation successful!"
        
        # Test the binary
        try {
            $version = & $targetPath --version 2>&1
            Write-Info "Installed version: $version"
        }
        catch {
            Write-Warn "Could not verify binary version"
        }
    }
    else {
        Write-Error "Installation failed"
    }
    
    return $targetPath
}

# Add to PATH
function Add-ToPath {
    param([string]$Directory)
    
    Write-Info "Adding to PATH..."
    
    # Get current PATH
    $currentPath = [Environment]::GetEnvironmentVariable("Path", [EnvironmentVariableTarget]::User)
    
    # Check if already in PATH
    if ($currentPath -split ';' | Where-Object { $_ -eq $Directory }) {
        Write-Info "Directory already in PATH"
        return
    }
    
    # Add to PATH
    try {
        $newPath = "$currentPath;$Directory"
        [Environment]::SetEnvironmentVariable("Path", $newPath, [EnvironmentVariableTarget]::User)
        
        # Update current session
        $env:Path = "$env:Path;$Directory"
        
        Write-Info "Added to PATH successfully"
        Write-Warn "You may need to restart your terminal for PATH changes to take effect"
    }
    catch {
        Write-Warn "Failed to add to PATH automatically. Please add manually: $Directory"
    }
}

# Clean up temporary files
function Clean-Up {
    $tempDir = [System.IO.Path]::GetTempPath()
    $extractPath = Join-Path $tempDir "gotowebinar-extract"
    
    if (Test-Path $extractPath) {
        Remove-Item -Path $extractPath -Recurse -Force
    }
}

# Main installation flow
function Main {
    Write-Host "================================" -ForegroundColor Cyan
    Write-Host "GoToWebinar CLI Installer" -ForegroundColor Cyan
    Write-Host "================================" -ForegroundColor Cyan
    Write-Host ""
    
    if ($Help) {
        Show-Help
    }
    
    # Get version
    if ([string]::IsNullOrEmpty($Version)) {
        $Version = Get-LatestVersion
    }
    
    # Download binary
    $binaryPath = Download-Binary -Version $Version -Platform $Platform
    
    try {
        # Install binary
        $installedPath = Install-Binary -SourcePath $binaryPath -InstallDir $InstallDir -BinaryName $BinaryName
        
        # Add to PATH if requested
        if ($AddToPath) {
            Add-ToPath -Directory $InstallDir
        }
        
        Write-Host ""
        Write-Info "Installation complete!"
        Write-Info "Run '$BinaryName --help' to get started"
        
        if (-not $AddToPath) {
            Write-Info "Binary location: $installedPath"
        }
    }
    finally {
        # Clean up
        Clean-Up
    }
}

# Run main function
Main