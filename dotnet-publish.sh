#!/usr/bin/env bash
set -euo pipefail

APP_NAME="OpenGrid"
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
SRC_DIR="$SCRIPT_DIR/src"
PUBLISH_DIR="$SCRIPT_DIR/_build/publish"

print_step() { echo -e "\n[\033[1;34m*\033[0m] $1"; }

print_step "Publishing $APP_NAME for linux-x64..."
dotnet publish "$SRC_DIR/OpenGrid/OpenGrid.csproj" \
    -c Release \
    -r linux-x64 \
    --self-contained true \
    -p:DebugType=embedded \
    -p:PublishTrimmed=false \
    -o "$PUBLISH_DIR" \
    --nologo

print_step "Publishing OpenGridBridge for win-x64..."
dotnet publish "$SRC_DIR/OpenGridBridge/OpenGridBridge.csproj" \
    -c Release \
    -r win-x64 \
    --self-contained true \
    -p:DebugType=embedded \
    -o "$PUBLISH_DIR" \
    --nologo

# Clean up debug symbols
rm -f "$PUBLISH_DIR"/*.pdb "$PUBLISH_DIR"/*.dbg 2>/dev/null || true

echo ""
echo "Binaries published to: $PUBLISH_DIR"
