#!/bin/bash

# Set variables
PROJECT_NAME="PalWorldServerMG"
CONFIG_FILE_NAME="config.xml"
CONFIG_FILE_PATH="src/$CONFIG_FILE_NAME"
PUBLISH_DIR_BASE="bin/Release/net6.0"
TARGET_DIR="deployments"

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

# Function to publish and organize files
publish_and_organize() {
    local RID="$1"
    local SELF_CONTAINED="$2"
    local PUBLISH_DIR="$PUBLISH_DIR_BASE/$RID/publish"
    local DEPLOYMENT_SUBDIR="$RID-$( [ "$SELF_CONTAINED" = "true" ] && echo 'self-contained' || echo 'framework-dependent')"
    local DEPLOYMENT_DIR="$TARGET_DIR/$DEPLOYMENT_SUBDIR"

    # Publish the project
    dotnet publish -c Release -r "$RID" --self-contained "$SELF_CONTAINED" -p:PublishSingleFile=true

    # Create the deployment directory
    mkdir -p "$DEPLOYMENT_DIR"

    # Copy the build output to the deployment directory
    cp -r "$PUBLISH_DIR/$PROJECT_NAME"* "$DEPLOYMENT_DIR"

    # Copy the config file to the deployment directory
    cp "$PUBLISH_DIR/$CONFIG_FILE_PATH" "$DEPLOYMENT_DIR"

    echo "Deployment for $RID created at $DEPLOYMENT_DIR"
}

# Create base target directory
mkdir -p "$TARGET_DIR"

# Publish and organize for Linux (framework-dependent)
publish_and_organize "linux-x64" "false"

# Publish and organize for Windows (framework-dependent)
publish_and_organize "win-x64" "false"

# Publish and organize for Linux (self-contained)
publish_and_organize "linux-x64" "true"

# Publish and organize for Windows (self-contained)
publish_and_organize "win-x64" "true"

echo "All builds have been published and organized."
