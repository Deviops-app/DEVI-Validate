# Updates

DEVI Validate can check for a newer Windows build. Hashing and verification never do this. The update check is the only network feature in the app. It is off by default and runs only when you ask for it.

## What the check does

- Start it from **Settings > Check for updates now**, from the **More** menu (**Check for updates**), or with `devi-validate update` on the command line.
- "Check for updates when the app opens" is in Settings and is off unless you turn it on. It is stored in `%LOCALAPPDATA%\DEVI\Validate\settings.json`. A check at launch stays quiet when it fails and opens a window only when a newer version is published.
- The check is one HTTPS GET of a signed version file. It sends the product name and version in the User-Agent header and nothing else: no machine identifier, no query string, and never any evidence, file names, hashes, or case details.
- If the check fails or the computer is offline, the app keeps working. Hashing and verification are not affected.

## Before anything is downloaded

1. The version file must carry a valid ECDSA P-256 signature from the DEVI release key. The public half of that key is built into the app (`UpdateTrust.PublicKeySpkiBase64` in `shared/Devi.Updates/UpdateTrust.cs`). A file with a missing or bad signature is rejected.
2. The window shows this copy's version, the published version, the release notes, and the SHA-256 of the installer and the portable zip.
3. A download starts only after you confirm it, and only from an allowed DEVI download host over HTTPS.
4. The downloaded file is checked against the SHA-256 in the signed version file. A file that does not match is thrown away.
5. Nothing is installed silently. The installer opens with its own screens. A portable zip is unpacked into a new folder beside the running copy after the app closes, and the current folder is left in place.

`devi-validate update` does the same check. It exits 0 when this copy is current or newer, and 2 when a newer version is published. `--download <folder>` saves the installer after the SHA-256 matches and does not start it. `--portable` saves the zip instead.

## Feed hosts

The app asks `downloads.deviops.app` for the signed version file. If that host does not return a feed (DNS failure, timeout, or HTTP error), it tries the same file on the content delivery network behind that host. A feed that comes back but fails the signature check is rejected and is never replaced by the fallback. The allowed hosts are listed in `shared/Devi.Updates/UpdateTrust.cs`.

## Signing

Version files are signed offline with `devi-validate sign-update`. The private signing key is held by the DEVI maintainers and is not in this repository. `devi-validate update-key` can make a new key pair for testing, but a build only trusts the public key compiled into it.

The feed signature protects the update channel. It is separate from the Authenticode signature on the installer and the program files. Windows SmartScreen can still warn about a newly signed release. Check the SHA-256 of any download against the value on <https://deviops.app/tools/devi-validate/> and in the GitHub release notes.
