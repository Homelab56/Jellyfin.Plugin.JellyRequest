#!/bin/bash

# JellyRequest Build Script for Bash (Linux/Mac)
# This script builds the plugin and creates a distributable package

set -e  # Exit on any error

# Default values
CONFIGURATION="Release"
OUTPUT_PATH="dist"
CLEAN=false
SKIP_TESTS=false

# Script variables
PROJECT_NAME="JellyRequest"
PROJECT_PATH="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
DIST_PATH="$PROJECT_PATH/$OUTPUT_PATH"
TEMP_PATH="$PROJECT_PATH/temp"
ZIP_PATH="$DIST_PATH/$PROJECT_NAME.zip"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
GRAY='\033[0;37m'
WHITE='\033[1;37m'
NC='\033[0m' # No Color

# Function to print colored output
print_color() {
    local color=$1
    shift
    echo -e "${color}$*${NC}"
}

# Function to clean build artifacts
clean_build_artifacts() {
    print_color "$CYAN" "Cleaning build artifacts..."
    
    if [ -d "$DIST_PATH" ]; then
        rm -rf "$DIST_PATH"
        print_color "$GRAY" "Removed distribution directory"
    fi
    
    if [ -d "$TEMP_PATH" ]; then
        rm -rf "$TEMP_PATH"
        print_color "$GRAY" "Removed temporary directory"
    fi
    
    # Clean dotnet build cache
    dotnet clean "$PROJECT_PATH" --configuration "$CONFIGURATION" --verbosity quiet
    print_color "$GRAY" "Cleaned dotnet build cache"
}

# Function to restore NuGet packages
restore_packages() {
    print_color "$CYAN" "Restoring NuGet packages..."
    dotnet restore "$PROJECT_PATH" --verbosity quiet
    print_color "$GREEN" "Packages restored successfully"
}

# Function to build the project
build_project() {
    print_color "$CYAN" "Building project..."
    dotnet build "$PROJECT_PATH" --configuration "$CONFIGURATION" --no-restore --verbosity quiet
    print_color "$GREEN" "Build completed successfully"
}

# Function to run tests
test_project() {
    if [ "$SKIP_TESTS" = true ]; then
        print_color "$YELLOW" "Skipping tests..."
        return
    fi
    
    print_color "$CYAN" "Running tests..."
    dotnet test "$PROJECT_PATH" --configuration "$CONFIGURATION" --no-build --verbosity quiet --logger "console;verbosity=minimal"
    print_color "$GREEN" "All tests passed"
}

# Function to publish the plugin
publish_plugin() {
    print_color "$CYAN" "Publishing plugin..."
    
    # Create output directories
    mkdir -p "$DIST_PATH"
    mkdir -p "$TEMP_PATH"
    
    # Publish the project
    dotnet publish "$PROJECT_PATH" \
        --configuration "$CONFIGURATION" \
        --output "$TEMP_PATH" \
        --no-build \
        --verbosity quiet
    
    print_color "$GREEN" "Plugin published successfully"
}

# Function to create distribution package
create_package() {
    print_color "$CYAN" "Creating distribution package..."
    
    # Copy only necessary files
    local files_to_copy=(
        "$PROJECT_NAME.dll"
        "$PROJECT_NAME.deps.json"
        "$PROJECT_NAME.pdb"
        "$PROJECT_NAME.runtimeconfig.json"
        "appsettings.json"
    )
    
    local plugin_dist_path="$DIST_PATH/$PROJECT_NAME"
    mkdir -p "$plugin_dist_path"
    
    for file in "${files_to_copy[@]}"; do
        local source_file="$TEMP_PATH/$file"
        if [ -f "$source_file" ]; then
            cp "$source_file" "$plugin_dist_path/"
            print_color "$GRAY" "Copied: $file"
        fi
    done
    
    # Copy Web assets
    local web_source_path="$PROJECT_PATH/Web"
    local web_dest_path="$plugin_dist_path/Web"
    if [ -d "$web_source_path" ]; then
        cp -r "$web_source_path" "$web_dest_path"
        print_color "$GRAY" "Copied Web assets"
    fi
    
    # Copy Configuration page
    local config_source_path="$PROJECT_PATH/Configuration"
    local config_dest_path="$plugin_dist_path/Configuration"
    if [ -d "$config_source_path" ]; then
        cp -r "$config_source_path" "$config_dest_path"
        print_color "$GRAY" "Copied Configuration assets"
    fi
    
    # Create ZIP package
    cd "$DIST_PATH"
    zip -r "$PROJECT_NAME.zip" "$PROJECT_NAME/"
    cd "$PROJECT_PATH"
    
    # Create checksum
    sha256sum "$ZIP_PATH" > "$ZIP_PATH.sha256"
    print_color "$GRAY" "Created SHA256 checksum"
    
    print_color "$GREEN" "Created ZIP package: $ZIP_PATH"
}

# Function to display build summary
show_summary() {
    echo ""
    print_color "$GREEN" "=== Build Summary ==="
    print_color "$WHITE" "Plugin: $PROJECT_NAME"
    print_color "$WHITE" "Configuration: $CONFIGURATION"
    print_color "$WHITE" "Output: $DIST_PATH"
    echo ""
    
    if [ -f "$ZIP_PATH" ]; then
        local size=$(du -h "$ZIP_PATH" | cut -f1)
        print_color "$GREEN" "Package created: $ZIP_PATH"
        print_color "$GREEN" "Package size: $size"
        echo ""
        print_color "$YELLOW" "Installation:"
        print_color "$WHITE" "1. Extract $PROJECT_NAME.zip to your Jellyfin plugins directory"
        print_color "$WHITE" "2. Restart Jellyfin service"
        print_color "$WHITE" "3. Configure plugin in Jellyfin dashboard"
    fi
    
    echo ""
}

# Function to display help
show_help() {
    echo "JellyRequest Build Script"
    echo ""
    echo "Usage: $0 [OPTIONS]"
    echo ""
    echo "Options:"
    echo "  -c, --configuration CONFIG  Build configuration (default: Release)"
    echo "  -o, --output PATH           Output directory (default: dist)"
    echo "  --clean                     Clean build artifacts and exit"
    echo "  --skip-tests                Skip running tests"
    echo "  -h, --help                 Show this help message"
    echo ""
    echo "Examples:"
    echo "  $0                          # Build with default settings"
    echo "  $0 -c Debug                 # Build with Debug configuration"
    echo "  $0 --clean                  # Clean build artifacts"
    echo "  $0 -o /tmp/build --skip-tests  # Custom output, skip tests"
}

# Parse command line arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        -c|--configuration)
            CONFIGURATION="$2"
            shift 2
            ;;
        -o|--output)
            OUTPUT_PATH="$2"
            DIST_PATH="$PROJECT_PATH/$OUTPUT_PATH"
            ZIP_PATH="$DIST_PATH/$PROJECT_NAME.zip"
            shift 2
            ;;
        --clean)
            CLEAN=true
            shift
            ;;
        --skip-tests)
            SKIP_TESTS=true
            shift
            ;;
        -h|--help)
            show_help
            exit 0
            ;;
        *)
            print_color "$RED" "Unknown option: $1"
            show_help
            exit 1
            ;;
    esac
done

# Main execution
main() {
    print_color "$GREEN" "=== JellyRequest Build Script ==="
    print_color "$YELLOW" "Configuration: $CONFIGURATION"
    print_color "$YELLOW" "Output Path: $OUTPUT_PATH"
    echo ""
    
    # Clean if requested
    if [ "$CLEAN" = true ]; then
        clean_build_artifacts
        if [ $# -eq 0 ]; then
            print_color "$GREEN" "Clean completed. Exiting..."
            exit 0
        fi
    fi
    
    # Build process
    restore_packages
    build_project
    test_project
    publish_plugin
    create_package
    show_summary
    
    print_color "$GREEN" "Build completed successfully!"
}

# Error handling
trap 'print_color "$RED" "Build failed on line $LINENO. Exiting..."; exit 1' ERR

# Run main function
main

# Cleanup temporary files
if [ -d "$TEMP_PATH" ]; then
    rm -rf "$TEMP_PATH"
fi

exit 0
