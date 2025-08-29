#!/bin/bash

set -e

# GoToWebinar CLI Installation Script
# This script downloads and installs the GoToWebinar CLI tool

REPO="stannardlabs/gotowebinar-cli"
INSTALL_DIR="/usr/local/bin"
BINARY_NAME="gotowebinar"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Logging functions
info() {
    echo -e "${GREEN}[INFO]${NC} $1"
}

warn() {
    echo -e "${YELLOW}[WARN]${NC} $1"
}

error() {
    echo -e "${RED}[ERROR]${NC} $1"
    exit 1
}

# Detect OS and architecture
detect_platform() {
    OS="$(uname -s)"
    ARCH="$(uname -m)"
    
    case "$OS" in
        Linux*)
            PLATFORM="linux"
            ;;
        Darwin*)
            PLATFORM="osx"
            ;;
        *)
            error "Unsupported operating system: $OS"
            ;;
    esac
    
    case "$ARCH" in
        x86_64|amd64)
            ARCH="x64"
            ;;
        *)
            error "Unsupported architecture: $ARCH"
            ;;
    esac
    
    PLATFORM_STRING="${PLATFORM}-${ARCH}"
    info "Detected platform: $PLATFORM_STRING"
}

# Check for required tools
check_requirements() {
    if ! command -v curl &> /dev/null; then
        error "curl is required but not installed. Please install curl and try again."
    fi
    
    if ! command -v tar &> /dev/null; then
        error "tar is required but not installed. Please install tar and try again."
    fi
}

# Get the latest release version
get_latest_version() {
    info "Fetching latest version..."
    
    LATEST_VERSION=$(curl -s "https://api.github.com/repos/${REPO}/releases/latest" | grep '"tag_name":' | sed -E 's/.*"([^"]+)".*/\1/')
    
    if [ -z "$LATEST_VERSION" ]; then
        error "Unable to determine latest version"
    fi
    
    info "Latest version: $LATEST_VERSION"
}

# Download the binary
download_binary() {
    local version=$1
    local platform=$2
    
    DOWNLOAD_URL="https://github.com/${REPO}/releases/download/${version}/gotowebinar-${platform}.tar.gz"
    TEMP_DIR=$(mktemp -d)
    DOWNLOAD_FILE="${TEMP_DIR}/gotowebinar.tar.gz"
    
    info "Downloading from: $DOWNLOAD_URL"
    
    if ! curl -L -o "$DOWNLOAD_FILE" "$DOWNLOAD_URL"; then
        error "Failed to download binary"
    fi
    
    info "Extracting binary..."
    tar -xzf "$DOWNLOAD_FILE" -C "$TEMP_DIR"
    
    if [ ! -f "${TEMP_DIR}/gotowebinar" ]; then
        error "Binary not found in archive"
    fi
    
    BINARY_PATH="${TEMP_DIR}/gotowebinar"
}

# Install the binary
install_binary() {
    local source_path=$1
    local install_dir=$2
    local binary_name=$3
    
    # Check if we need sudo
    if [ -w "$install_dir" ]; then
        SUDO=""
    else
        SUDO="sudo"
        warn "Installation requires sudo privileges"
    fi
    
    info "Installing to ${install_dir}/${binary_name}..."
    
    $SUDO mkdir -p "$install_dir"
    $SUDO cp "$source_path" "${install_dir}/${binary_name}"
    $SUDO chmod +x "${install_dir}/${binary_name}"
    
    # Verify installation
    if command -v "$binary_name" &> /dev/null; then
        info "Installation successful!"
        info "Run '${binary_name} --help' to get started"
    else
        warn "Binary installed but not in PATH. Add ${install_dir} to your PATH"
        warn "You can run it directly: ${install_dir}/${binary_name}"
    fi
}

# Clean up temporary files
cleanup() {
    if [ -n "$TEMP_DIR" ] && [ -d "$TEMP_DIR" ]; then
        rm -rf "$TEMP_DIR"
    fi
}

# Main installation flow
main() {
    echo "================================"
    echo "GoToWebinar CLI Installer"
    echo "================================"
    echo
    
    # Parse command line arguments
    while [[ $# -gt 0 ]]; do
        case $1 in
            --install-dir)
                INSTALL_DIR="$2"
                shift 2
                ;;
            --binary-name)
                BINARY_NAME="$2"
                shift 2
                ;;
            --version)
                VERSION="$2"
                shift 2
                ;;
            --help)
                echo "Usage: $0 [options]"
                echo "Options:"
                echo "  --install-dir DIR    Installation directory (default: /usr/local/bin)"
                echo "  --binary-name NAME   Binary name (default: gotowebinar)"
                echo "  --version VERSION    Specific version to install (default: latest)"
                echo "  --help              Show this help message"
                exit 0
                ;;
            *)
                error "Unknown option: $1"
                ;;
        esac
    done
    
    # Set up trap to clean up on exit
    trap cleanup EXIT
    
    check_requirements
    detect_platform
    
    if [ -z "$VERSION" ]; then
        get_latest_version
        VERSION=$LATEST_VERSION
    fi
    
    download_binary "$VERSION" "$PLATFORM_STRING"
    install_binary "$BINARY_PATH" "$INSTALL_DIR" "$BINARY_NAME"
    
    echo
    info "Installation complete!"
    
    # Show version
    "${INSTALL_DIR}/${BINARY_NAME}" --version 2>/dev/null || true
}

# Run main function
main "$@"