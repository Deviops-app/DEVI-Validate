# Building DEVI Validate releases

DEVI Validate builds with the .NET 8 SDK (8.0.100 or later, see `global.json`). Release packages are self-contained for `win-x64` and include the .NET runtime, so nothing else needs to be installed on the examiner's computer.

For a normal developer build, see "Build from source" in the [README](README.md). This page covers the official installer and portable zip.

## Build scripts

- Windows: `.\installer\build-release.ps1` (needs the .NET 8 SDK and Inno Setup 6)
- Linux: `./installer/build-release.sh` (needs the .NET 8 SDK, python3, zip, and Inno Setup 6 under Wine; set `INNO_PREFIX` to the Wine prefix that holds Inno Setup)

Both scripts produce:

- `out/DEVI-Validate-<version>-win-x64.zip` (portable)
- `out/DEVI-Validate-Setup-<version>-win-x64.exe` (installer)
- a `.sha256` file for each

## Package layout

- `app\` holds the Windows app (`DEVI-Validate.exe`).
- `cli\` holds the command line (`devi-validate.exe`).

Windows file names are not case-sensitive, so `DEVI-Validate.exe` and `devi-validate.exe` must never share a folder. `installer/check-layout.py` fails the build if any folder holds two names that differ only by case, if `app\DEVI-Validate.exe` is not a Windows GUI program, or if the app declares loose WPF content files.

## Signing

Official releases are Authenticode-signed through Microsoft Artifact Signing. Signing happens only on a DEVI maintainer's computer. No signing key, account, or secret is stored in this repository or in GitHub Actions.

After `build-release.sh`, a maintainer signed in with `az` runs:

```bash
DEVI_SIGN_ENDPOINT=<endpoint host> DEVI_SIGN_PROFILE=<account>/<certificate profile> ./installer/sign-release.sh
```

It signs `app\DEVI-Validate.exe`, `cli\devi-validate.exe`, and the DEVI assemblies with [jsign](https://github.com/ebourg/jsign) and an RFC 3161 timestamp, rebuilds the zip, compiles the installer with Inno Setup signing the uninstaller and Setup, and rewrites the `.sha256` files. Microsoft runtime files already carry Microsoft's signature. Third-party assemblies are left as published.

To compare a signed release with your own build, remove the signature first (`osslsigncode remove-signature -in signed.dll -out plain.dll`). Only the PE checksum field then differs.

## Before shipping a Windows build

Run the launch check on a Windows desktop:

```powershell
.\installer\smoke-launch.ps1 -Exe <path>\app\DEVI-Validate.exe
```

It confirms that a window appears, that a second launch hands off to the first and exits, and that only one process is left.

Two rules come from the 0.1.3 launch failure and must be kept:

- The main window is created in `App.OnStartup`. Do not add `StartupUri` back to `App.xaml`.
- The window icon is an embedded resource. Keep the shortcut icon (`devi-validate.ico`) as a `None` item. A `Content` item makes WPF look for a loose file that is not shipped, and the main window fails to load.

## Publishing a release

1. Update `<Version>` in `Directory.Build.props`, the version strings in the app, and add a `CHANGELOG.md` entry.
2. Build with one of the scripts above and run the launch check on Windows.
3. Sign, publish the installer and zip to the DEVI download host, sign the update feed offline, and create a GitHub release tagged `v<version>` whose notes link to the download page and list both SHA-256 values. The files themselves are not attached to the GitHub release.
