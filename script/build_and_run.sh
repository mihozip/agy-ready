#!/usr/bin/env bash
set -euo pipefail

MODE="${1:-run}"
APP_NAME="AGYReadyMac"
BUNDLE_ID="tw.edu.mlc.yjps.agyreadymac"
ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
PACKAGE_DIR="$ROOT_DIR/macos"
APP_BUNDLE="$ROOT_DIR/dist/macos/$APP_NAME.app"
CONTENTS="$APP_BUNDLE/Contents"
APP_BINARY="$CONTENTS/MacOS/$APP_NAME"

pkill -x "$APP_NAME" >/dev/null 2>&1 || true
swift build -c release --package-path "$PACKAGE_DIR"
BUILD_DIR="$(swift build -c release --package-path "$PACKAGE_DIR" --show-bin-path)"
rm -rf "$APP_BUNDLE"
mkdir -p "$CONTENTS/MacOS" "$CONTENTS/Resources"
cp "$BUILD_DIR/$APP_NAME" "$APP_BINARY"
cp "$PACKAGE_DIR/Resources/AGYReadyMac.icns" "$CONTENTS/Resources/AGYReadyMac.icns"
chmod +x "$APP_BINARY"
cat > "$CONTENTS/Info.plist" <<'PLIST'
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0"><dict>
  <key>CFBundleExecutable</key><string>AGYReadyMac</string>
  <key>CFBundleIdentifier</key><string>tw.edu.mlc.yjps.agyreadymac</string>
  <key>CFBundleName</key><string>AGYReadyMac</string>
  <key>CFBundleDisplayName</key><string>AGY CLI-CLASP 安裝小幫手（標標版）</string>
  <key>CFBundlePackageType</key><string>APPL</string>
  <key>CFBundleIconFile</key><string>AGYReadyMac.icns</string>
  <key>CFBundleShortVersionString</key><string>0.1.0</string>
  <key>CFBundleVersion</key><string>1</string>
  <key>LSMinimumSystemVersion</key><string>14.0</string>
  <key>NSPrincipalClass</key><string>NSApplication</string>
  <key>NSHighResolutionCapable</key><true/>
</dict></plist>
PLIST

open_app() { /usr/bin/open -n "$APP_BUNDLE"; }
case "$MODE" in
  run) open_app ;;
  --build-only) echo "$APP_BUNDLE" ;;
  --debug|debug) lldb -- "$APP_BINARY" ;;
  --logs|logs)
    open_app
    /usr/bin/log stream --info --style compact --predicate "process == \"$APP_NAME\""
    ;;
  --telemetry|telemetry)
    open_app
    /usr/bin/log stream --info --style compact --predicate "subsystem == \"$BUNDLE_ID\""
    ;;
  --verify|verify)
    open_app
    sleep 2
    pgrep -x "$APP_NAME" >/dev/null
    ;;
  *) echo "usage: $0 [run|--build-only|--debug|--logs|--telemetry|--verify]" >&2; exit 2 ;;
esac
