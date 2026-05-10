#!/usr/bin/env bash
set -euo pipefail

APP_ID="io.github.radekbocon.SimLab"
APP_NAME="SimLab"
APP_DIRNAME="simlab"
LINUX_DIR="$(cd "$(dirname "$0")" && pwd)"
REPO_DIR="$(dirname "$LINUX_DIR")"
SRC_DIR="$REPO_DIR/src"

BUILD_DIR="$REPO_DIR/_build/appimage"
PUBLISH_DIR="$BUILD_DIR/publish"
APPDIR="$BUILD_DIR/$APP_NAME-x86_64.AppDir"
OUTPUT_DIR="$REPO_DIR/_build"

# appimagetool download URL
APPIMAGETOOL_URL="https://github.com/AppImage/AppImageKit/releases/download/continuous/appimagetool-x86_64.AppImage"

# ------------------------------------------------------------------
print_step() { echo -e "\n[\033[1;34m*\033[0m] $1"; }

# ------------------------------------------------------------------
check_deps() {
    print_step "Checking prerequisites..."
    local missing=()

    if ! command -v dotnet &>/dev/null; then
        missing+=("dotnet SDK (>= 10.0)")
    fi

    if ! command -v wget &>/dev/null && ! command -v curl &>/dev/null; then
        missing+=("wget or curl")
    fi

    if [ ${#missing[@]} -gt 0 ]; then
        echo "Missing prerequisites:"
        for m in "${missing[@]}"; do echo "  - $m"; done
        exit 1
    fi

    echo "All prerequisites satisfied."
}

# ------------------------------------------------------------------
publish_app() {
    print_step "Publishing $APP_NAME for linux-x64..."
    dotnet publish "$SRC_DIR/SimLab/SimLab.csproj" \
        -c Release \
        -r linux-x64 \
        --self-contained true \
        -p:DebugType=embedded \
        -p:PublishTrimmed=false \
        -o "$PUBLISH_DIR" \
        --nologo

    print_step "Publishing SimLabBridge for win-x64..."
    dotnet publish "$SRC_DIR/SimLabBridge/SimLabBridge.csproj" \
        -c Release \
        -r win-x64 \
        --self-contained true \
        -p:DebugType=embedded \
        -o "$PUBLISH_DIR" \
        --nologo

    # Clean up debug symbols
    rm -f "$PUBLISH_DIR"/*.pdb "$PUBLISH_DIR"/*.dbg 2>/dev/null || true
}

# ------------------------------------------------------------------
create_appdir() {
    print_step "Creating AppDir structure..."

    rm -rf "$APPDIR"
    mkdir -p "$APPDIR/usr/lib/$APP_DIRNAME"

    # Copy published files
    cp -r "$PUBLISH_DIR"/* "$APPDIR/usr/lib/$APP_DIRNAME/"
    chmod +x "$APPDIR/usr/lib/$APP_DIRNAME/$APP_NAME"

    # Install desktop file
    install -Dm644 "$LINUX_DIR/$APP_ID.desktop" "$APPDIR/$APP_ID.desktop"

    # Install icon
    install -Dm644 "$LINUX_DIR/$APP_ID.png" "$APPDIR/$APP_ID.png"

    # Install metainfo (both modern and legacy filenames)
    install -Dm644 "$LINUX_DIR/$APP_ID.metainfo.xml" \
        "$APPDIR/usr/share/metainfo/$APP_ID.metainfo.xml"
    install -Dm644 "$LINUX_DIR/$APP_ID.metainfo.xml" \
        "$APPDIR/usr/share/metainfo/$APP_ID.appdata.xml"

    # Create AppRun entry point
    cat > "$APPDIR/AppRun" <<APPRUN_EOF
#!/bin/bash
HERE="\$(dirname "\$(readlink -f "\$0")")"
export DOTNET_ROOT="\$HERE/usr/lib/$APP_DIRNAME"
export DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1
exec "\$HERE/usr/lib/$APP_DIRNAME/$APP_NAME" "\$@"
APPRUN_EOF
    chmod +x "$APPDIR/AppRun"

    # Symlink .DirIcon for compatibility
    ln -sf "$APP_ID.png" "$APPDIR/.DirIcon"
}

# ------------------------------------------------------------------
build_appimage() {
    print_step "Building AppImage..."

    mkdir -p "$OUTPUT_DIR"

    # Download appimagetool if not already present
    APPIMAGETOOL="$BUILD_DIR/appimagetool"
    if [ ! -x "$APPIMAGETOOL" ]; then
        echo "Downloading appimagetool..."
        if command -v wget &>/dev/null; then
            wget -qO "$APPIMAGETOOL" "$APPIMAGETOOL_URL"
        else
            curl -sSL -o "$APPIMAGETOOL" "$APPIMAGETOOL_URL"
        fi
        chmod +x "$APPIMAGETOOL"

        # If FUSE is not available, extract the static binary
        if ! "$APPIMAGETOOL" --help &>/dev/null 2>&1; then
            echo "Extracting static appimagetool (FUSE not available)..."
            "$APPIMAGETOOL" --appimage-extract 2>/dev/null || true
            if [ -f squashfs-root/AppRun ]; then
                mv squashfs-root/AppRun "$APPIMAGETOOL"
                rm -rf squashfs-root
                chmod +x "$APPIMAGETOOL"
            fi
        fi
    fi

    # Build the AppImage
    ARCH=x86_64 "$APPIMAGETOOL" -n "$APPDIR" "$OUTPUT_DIR/$APP_NAME-x86_64.AppImage"

    echo ""
    echo "AppImage created: $OUTPUT_DIR/$APP_NAME-x86_64.AppImage"
}

# ------------------------------------------------------------------
clean() {
    print_step "Cleaning build artifacts..."
    rm -rf "$BUILD_DIR"
}

# ------------------------------------------------------------------
usage() {
    cat <<EOF
Usage: $0 [command]

Commands:
  build       Build the AppImage (default)
  clean       Remove build artifacts
  deps        Check prerequisites only

Environment:
  ARCH        Target architecture (default: x86_64)
EOF
}

# ------------------------------------------------------------------
case "${1:-build}" in
    build)
        check_deps
        publish_app
        create_appdir
        build_appimage
        ;;
    clean)
        clean
        ;;
    deps)
        check_deps
        ;;
    --help|-h)
        usage
        ;;
    *)
        echo "Unknown command: $1"
        usage
        exit 1
        ;;
esac
