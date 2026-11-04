#!/bin/bash
set -euo pipefail

root="$(cd "$(dirname "$0")/.." && pwd)"
runtime="${1:-osx-arm64}"
case "$runtime" in osx-arm64|osx-x64) ;; *) echo 'Use osx-arm64 or osx-x64.' >&2; exit 1 ;; esac
[[ "$(uname -s)" == Darwin ]] || { echo 'Run packaging on macOS (or GitHub macOS runners).' >&2; exit 1; }
version="$(tr -d '\r\n' < "$root/VERSION")"
[[ "$version" =~ ^[0-9]+\.[0-9]+\.[0-9]+$ ]] || { echo 'Invalid VERSION.' >&2; exit 1; }

dotnet run --project "$root/tests/CoreTests.csproj" -c Release
output="$root/release/macos-$runtime"
app="$output/Reminder Buddy.app"
# Each architecture has an isolated output directory; never clear other release artifacts.
mkdir -p "$output"
rm -rf "$app"
mkdir -p "$app/Contents/MacOS" "$app/Contents/Resources"
# Bundle managed assemblies so Apple's signing check does not treat loose .NET DLLs as unsigned nested code.
dotnet publish "$root/src/ReminderBuddy.csproj" -c Release -r "$runtime" --self-contained true -p:UseAppHost=true -p:PublishSingleFile=true -o "$app/Contents/MacOS"
if [[ -n "$(find "$app/Contents/MacOS" -type f -name '*.dll' -print -quit)" ]]; then
    echo 'Mac publish left loose managed DLLs in Contents/MacOS; refusing to sign an invalid bundle.' >&2
    exit 1
fi
sed "s/__VERSION__/$version/g" "$root/installer/macos/Info.plist" > "$app/Contents/Info.plist"
chmod +x "$app/Contents/MacOS/ReminderBuddy"
plutil -lint "$app/Contents/Info.plist"

iconset="$output/ReminderBuddy.iconset"
mkdir -p "$iconset"
# sips decodes the existing multi-resolution ICO; no new artwork or external tools needed.
sips -s format png "$root/assets/ReminderBuddy.ico" --out "$output/icon.png" >/dev/null
for size in 16 32 128 256 512; do
    sips -z "$size" "$size" "$output/icon.png" --out "$iconset/icon_${size}x${size}.png" >/dev/null
    double=$((size * 2))
    sips -z "$double" "$double" "$output/icon.png" --out "$iconset/icon_${size}x${size}@2x.png" >/dev/null
done
iconutil -c icns "$iconset" -o "$app/Contents/Resources/ReminderBuddy.icns"

# Ad-hoc signing is needed for local Apple Silicon execution. It is NOT Developer ID signing or notarization.
while IFS= read -r -d '' file; do
    if file -b "$file" | grep -q 'Mach-O'; then codesign --force --sign - "$file"; fi
done < <(find "$app/Contents/MacOS" -type f -print0)
codesign --force --sign - "$app"
codesign --verify --strict --verbose=2 "$app"

archive="$root/release/ReminderBuddy-macOS-$runtime-$version-beta.zip"
ditto -c -k --sequesterRsrc --keepParent "$app" "$archive"
shasum -a 256 "$archive" | sed "s|$root/release/||" > "$root/release/SHA256SUMS-$runtime.txt"
echo "Created beta: $archive"
