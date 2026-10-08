#!/usr/bin/env bash
# Authenticode-sign a staged DEVI Validate release from Linux, then rebuild the zip and the installer.
# Run after build-release.sh, which leaves the staged folder in out/DEVI-Validate-<ver>-win-x64.
#
# Requires: az (signed in, with the Artifact Signing Certificate Profile Signer role on the account),
# jsign 7+, java, osslsigncode, python3, zip, wine + Inno Setup 6 (ISCC) in $INNO_PREFIX.
#
# Signs app\DEVI-Validate.exe, cli\devi-validate.exe and the DEVI assemblies, rebuilds the zip,
# then compiles the installer with ISCC signing the uninstaller and Setup. Microsoft runtime files
# already carry Microsoft's signature. Third-party assemblies are left as published.
set -euo pipefail
cd "$(dirname "$0")/.."
VERSION=$(sed -n 's:.*<Version>\(.*\)</Version>.*:\1:p' Directory.Build.props)
NAME="DEVI-Validate-$VERSION-win-x64"
OUT="out/$NAME"
INNO_PREFIX=${INNO_PREFIX:-$HOME/.wine-devi-inno}
# Maintainer only. The signing account and certificate profile are not part of this repository.
ENDPOINT=${DEVI_SIGN_ENDPOINT:?set DEVI_SIGN_ENDPOINT to the Artifact Signing endpoint host}
PROFILE=${DEVI_SIGN_PROFILE:?set DEVI_SIGN_PROFILE to <account>/<certificate profile>}
TSA=${DEVI_SIGN_TSA:-http://timestamp.acs.microsoft.com}
[ -d "$OUT/app" ] && [ -d "$OUT/cli" ] || { echo "run build-release.sh first ($OUT missing)"; exit 1; }

sign() {
  local token
  token=$(az account get-access-token --resource https://codesigning.azure.net --query accessToken -o tsv)
  DEVI_SIGN_TOKEN="$token" jsign --storetype TRUSTEDSIGNING --keystore "$ENDPOINT" \
    --storepass env:DEVI_SIGN_TOKEN --alias "$PROFILE" --alg SHA-256 \
    --tsaurl "$TSA" --tsmode RFC3161 --replace "$@"
}

sign "$OUT/app/DEVI-Validate.exe" "$OUT/app/DEVI-Validate.dll" "$OUT/app/DeviValidate.Core.dll" "$OUT/app/Devi.Theme.dll" \
     "$OUT/cli/devi-validate.exe" "$OUT/cli/devi-validate.dll" "$OUT/cli/DeviValidate.Core.dll"
python3 installer/check-layout.py "$OUT"

rm -f "out/$NAME.zip"
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

# ISCC runs under wine and cannot wait on a Linux process, so its SignTool is a batch file that
# hands each path to this script through out/.sign and waits for done or fail.
BRIDGE="$(pwd)/out/.sign"
rm -rf "$BRIDGE" && mkdir -p "$BRIDGE"
WBRIDGE="Z:$(echo "$BRIDGE" | sed 's:/:\\:g')"
printf '%s\r\n' '@echo off' \
  "if exist \"$WBRIDGE\\done\" del \"$WBRIDGE\\done\"" \
  "if exist \"$WBRIDGE\\fail\" del \"$WBRIDGE\\fail\"" \
  "echo %~1> \"$WBRIDGE\\req.tmp\"" \
  "move /y \"$WBRIDGE\\req.tmp\" \"$WBRIDGE\\req\" >nul" \
  ':wait' \
  "if exist \"$WBRIDGE\\done\" goto ok" \
  "if exist \"$WBRIDGE\\fail\" goto bad" \
  'ping -n 2 127.0.0.1 >nul' \
  'goto wait' \
  ':ok' "del \"$WBRIDGE\\done\"" 'exit /b 0' \
  ':bad' "del \"$WBRIDGE\\fail\"" 'exit /b 1' > "$BRIDGE/sign.bat"
(
  while [ ! -f "$BRIDGE/stop" ]; do
    if [ -f "$BRIDGE/req" ]; then
      p=$(tr -d '\r\n' < "$BRIDGE/req" | sed 's/[[:space:]]*$//'); rm -f "$BRIDGE/req"
      case "$p" in [Zz]:*) u="${p:2}" ;; [Cc]:*) u="$INNO_PREFIX/drive_c${p:2}" ;; *) u="$p" ;; esac
      if sign "${u//\\//}"; then touch "$BRIDGE/done"; else touch "$BRIDGE/fail"; fi
    fi
    sleep 0.3
  done
) &
WATCHER=$!
trap 'touch "$BRIDGE/stop"; wait $WATCHER 2>/dev/null || true; rm -rf "$BRIDGE"' EXIT
rm -f "out/DEVI-Validate-Setup-$VERSION-win-x64.exe"
WINEPREFIX="$INNO_PREFIX" WINEDEBUG=-all wine "$INNO_PREFIX/drive_c/InnoSetup/ISCC.exe" /Q /DSign \
  "/Sdevisign=cmd.exe /c $WBRIDGE\\sign.bat \$f" \
  "/DAppVersion=$VERSION" "/DSrcDir=Z:$(pwd | sed 's:/:\\:g')\\out\\$NAME" "Z:$(pwd | sed 's:/:\\:g')\\installer\\DEVI-Validate.iss"

cd out
for f in "$NAME.zip" "DEVI-Validate-Setup-$VERSION-win-x64.exe"; do
  sha256sum "$f" | awk -v n="$f" '{print $1"  "n}' > "$f.sha256"; cat "$f.sha256"
done
for f in "DEVI-Validate-Setup-$VERSION-win-x64.exe" "$NAME/app/DEVI-Validate.exe" "$NAME/cli/devi-validate.exe"; do
  sig=$(mktemp -u)
  osslsigncode extract-signature -in "$f" -out "$sig" >/dev/null 2>&1 || { echo "not signed: $f"; exit 1; }
  rm -f "$sig"
done
echo "signed. Verify on Windows with Get-AuthenticodeSignature, or with osslsigncode verify and the Microsoft Identity Verification Root CA 2020."
