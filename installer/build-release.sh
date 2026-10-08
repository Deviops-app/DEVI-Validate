#!/usr/bin/env bash
# Build DEVI Validate release artifacts (portable zip + Inno installer) from Linux.
# Requires: .NET 8 SDK, python3, zip, wine + Inno Setup 6 (ISCC) in $INNO_PREFIX.
set -euo pipefail
cd "$(dirname "$0")/.."
VERSION=$(sed -n 's:.*<Version>\(.*\)</Version>.*:\1:p' Directory.Build.props)
NAME="DEVI-Validate-$VERSION-win-x64"
OUT="out/$NAME"
INNO_PREFIX=${INNO_PREFIX:-$HOME/.wine-devi-inno}
export DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1
rm -rf out && mkdir -p "$OUT"
COMMON=(-c Release -r win-x64 --self-contained true -p:DebugType=none -p:GenerateDocumentationFile=false -p:SatelliteResourceLanguages=en)
# Separate folders: devi-validate.exe and DEVI-Validate.exe are the same name on Windows.
dotnet publish src/DeviValidate.Cli/DeviValidate.Cli.csproj "${COMMON[@]}" -o "$OUT/cli"
dotnet publish src/DeviValidate.Desktop/DeviValidate.Desktop.csproj "${COMMON[@]}" -o "$OUT/app"
find "$OUT" -name '*.pdb' -delete
for f in LICENSE NOTICE THIRD-PARTY-NOTICES.md README.md TESTING.md CHANGELOG.md SECURITY.md; do cp "$f" "$OUT/"; done
mkdir -p "$OUT/docs" && cp docs/UPDATES.md docs/VALIDATION-CHECKLIST.md "$OUT/docs/"
cp release-kit/QUICKSTART.txt release-kit/DOTNET-RUNTIME-LICENSE.txt release-kit/DOTNET-RUNTIME-THIRD-PARTY-NOTICES.txt "$OUT/"
python3 installer/check-layout.py "$OUT"
# Portable zip: entries under $NAME/ with forward slashes.
python3 - "$NAME" <<'PY'
import os, sys, zipfile
name = sys.argv[1]
os.chdir('out')
with zipfile.ZipFile(name + '.zip', 'w', zipfile.ZIP_DEFLATED, compresslevel=9) as z:
    for d, _, files in sorted(os.walk(name)):
        for fn in sorted(files):
            p = os.path.join(d, fn)
            z.write(p, p.replace(os.sep, '/'))
PY
# Installer
WINEPREFIX="$INNO_PREFIX" WINEDEBUG=-all wine "$INNO_PREFIX/drive_c/InnoSetup/ISCC.exe" /Q \
  "/DAppVersion=$VERSION" "/DSrcDir=Z:$(pwd | sed 's:/:\\:g')\\out\\$NAME" "Z:$(pwd | sed 's:/:\\:g')\\installer\\DEVI-Validate.iss"
cd out
for f in "$NAME.zip" "DEVI-Validate-Setup-$VERSION-win-x64.exe"; do
  sha256sum "$f" | awk -v n="$f" '{print $1"  "n}' > "$f.sha256"; cat "$f.sha256"
done
ls -la
