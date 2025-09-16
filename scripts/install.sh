#!/bin/bash

set -e

# GoToWebinar CLI Installation Script
# This script downloads and installs the GoToWebinar CLI tool

REPO="Aaronontheweb/gotowebinar-cli"
INSTALL_DIR="${INSTALL_DIR:-${HOME}/.local/bin}"
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

    info "Installing to ${install_dir}/${binary_name}..."

    # Create install directory if it doesn't exist (no sudo needed for user directory)
    mkdir -p "$install_dir"
    cp "$source_path" "${install_dir}/${binary_name}"
    chmod +x "${install_dir}/${binary_name}"

    # Check if install dir is in PATH
    if [[ ":$PATH:" != *":${install_dir}:"* ]]; then
        warn "${install_dir} is not in your PATH"
        echo ""
        echo "Add it to your PATH by adding this line to your shell profile:"
        echo ""

        if [ -n "$ZSH_VERSION" ]; then
            echo "  echo 'export PATH=\"\$PATH:${install_dir}\"' >> ~/.zshrc"
            echo "  source ~/.zshrc"
        elif [ -n "$BASH_VERSION" ]; then
            echo "  echo 'export PATH=\"\$PATH:${install_dir}\"' >> ~/.bashrc"
            echo "  source ~/.bashrc"
        else
            echo "  export PATH=\"\$PATH:${install_dir}\""
        fi
        echo ""
        info "Or run directly: ${install_dir}/${binary_name}"
    else
        info "Installation successful!"
        info "Run '${binary_name} --help' to get started"
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
                echo "  --install-dir DIR    Installation directory (default: ~/.local/bin)"
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