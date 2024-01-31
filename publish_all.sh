#!/bin/bash

# Set variables
PROJECT_DIR="$(pwd)"
CONFIG_FILE="src/config.xml"
TARGET_DIR="$PROJECT_DIR/output"
ARCHIVE_DIR="$PROJECT_DIR/archives"

# Check and install .NET SDK 6.0+
function check_and_install_dotnet() {
    local OS="$1"
    local SDK_VERSION="6.0"

    local current_dotnet_version=$(dotnet --version | cut -d'v' -f2 | cut -d'.' -f1-2)
    local required_dotnet_version=$(echo $SDK_VERSION | cut -d'.' -f1-2)

    if (( $(echo "$current_dotnet_version < $required_dotnet_version" | bc -l) )); then
        echo "Installing .NET SDK $SDK_VERSION..."
        if [ "$OS" == "Ubuntu" ]; then
            sudo apt-get update
            sudo apt-get install -y apt-transport-https
            sudo apt-get install -y dotnet-sdk-$SDK_VERSION
        elif [ "$OS" == "RHEL" ]; then
            sudo rpm -Uvh https://packages.microsoft.com/config/rhel/7/packages-microsoft-prod.rpm
            sudo yum install -y dotnet-sdk-$SDK_VERSION
        else
            echo "Unsupported OS: $OS"
            exit 1
        fi
    fi
}

# Detect OS
if [ -f /etc/os-release ]; then
    source /etc/os-release
    OS=$ID
elif type lsb_release >/dev/null 2>&1; then
    OS=$(lsb_release -si)
else
    echo "Cannot determine OS"
    exit 1
fi

# Install .NET SDK if required
check_and_install_dotnet "$OS"

# Create directories
mkdir -p "$TARGET_DIR"
mkdir -p "$ARCHIVE_DIR"

# Build and publish
function build_and_publish() {
    local RID="$1"
    local SELF_CONTAINED="$2"
    local OUTPUT_DIR="$TARGET_DIR/$RID"
    local FILE_SUFFIX="Framework-dependent"

    if [ "$SELF_CONTAINED" = "true" ]; then
        FILE_SUFFIX="Self-contained"
    fi

    local ARCHIVE_NAME="$ARCHIVE_DIR/$RID-$FILE_SUFFIX.zip"

    echo "Building and publishing for $RID with Self-contained: $SELF_CONTAINED..."
    mkdir -p "$OUTPUT_DIR"
    cp "$CONFIG_FILE" "$OUTPUT_DIR"
    dotnet publish -c Release -r "$RID" --self-contained "$SELF_CONTAINED" -p:PublishSingleFile=true -o "$OUTPUT_DIR"

    # Compress the output directory
    if [ "$SELF_CONTAINED" = "true" ]; then
        zip -r "$ARCHIVE_NAME" "$OUTPUT_DIR"
    else
        tar -czvf "$ARCHIVE_NAME" -C "$OUTPUT_DIR" .
    fi
}

# Build for all required configurations
build_and_publish "win-x64" "false"
build_and_publish "linux-x64" "false"
build_and_publish "win-x64" "true"
build_and_publish "linux-x64" "true"

echo "Build and publish completed."