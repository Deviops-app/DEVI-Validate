<p align="center">
  <img src="assets/brand/devi-readme-banner.svg" alt="DEVI" width="600">
</p>

<h1 align="center">DEVI Validate</h1>

<p align="center">
  Independent, read-only hash verification for digital forensic examiners.
</p>

<p align="center">
  <a href="https://deviops.app/tools/devi-validate/">Download</a> ·
  <a href="CHANGELOG.md">Changelog</a> ·
  <a href="SECURITY.md">Security</a> ·
  <a href="https://deviops.app">deviops.app</a>
</p>

---

DEVI Validate re-checks the hash values that an acquisition or analysis tool reported for your evidence. It reads the evidence again with its own open-source code, compares the result with the values the other tool wrote down, and saves a verification record as HTML, PDF, and JSON. Anyone can repeat the check with standard tools.

DEVI Validate is part of **DEVI**, a set of free tools built by experienced digital forensic examiners for examiners. It does not carry a court, standards-body, or laboratory certification. Each lab or agency should validate it under its own procedures before relying on it in casework. [docs/VALIDATION-CHECKLIST.md](docs/VALIDATION-CHECKLIST.md) is a checklist for doing that.

## What it does

- Hashes a file or a folder with SHA-256 (the default), SHA-1, or MD5.
- Streams each file in 1 MiB reads, so a large disk image does not have to fit in memory. Progress is shown while it reads, and the run can be canceled.
- Walks folders and records each file's relative path, size, and hash. Symbolic links are not followed.
- Compares the result with a pasted hash, a sum file, a CSV or TSV, an FTK Imager text log, or a DEVI Validate JSON record.
- Reports each file as **Match**, **Mismatch**, **Missing**, or **Extra**, plus one overall verdict. The wording states what differed. It does not assign a cause.
- Writes a print-friendly HTML record, a PDF, and a JSON record, named after the hash that was verified.
- Explains in plain language what the record is, what the results mean, and how to check it again.
- Runs a built-in self-test against published SHA-256, SHA-1, and MD5 test vectors.
- Creates and checks a validation package: one folder (and zip) holding the record, the PDF, and a manifest of SHA-256 values, with a QR code and a short verification code.

## Offline and read-only

- **Read-only.** Evidence is opened with read access only. DEVI Validate never writes, renames, or changes timestamps in the evidence location. On Linux it asks for `O_NOATIME`.
- **Reports stay outside the evidence.** It refuses to write a report inside the evidence location, after resolving the full path and any symbolic links.
- **Offline.** Hashing and verification never use the network. No telemetry is collected or sent, and no plugins are loaded from disk.
- **Repeatable.** Every result can be reproduced with `sha256sum`, `certutil -hashfile`, or `Get-FileHash`.

### The one network feature: an optional update check

- It is **off by default**. It runs only when you choose **Check for updates**, run `devi-validate update`, or turn on "Check for updates when the app opens".
- It is one HTTPS request for a **signed** version file. It sends the product name and version and nothing else: no machine identifier, and never any evidence, file names, hashes, or case details.
- A download starts only after you confirm it, and only after its SHA-256 matches the signed version file. Nothing is installed silently.

See [docs/UPDATES.md](docs/UPDATES.md) for details.

## Download

The current release is **DEVI Validate 1.0.3** for Windows x64. Download it from <https://deviops.app/tools/devi-validate/>. The installer and the portable zip are published only there. GitHub releases carry the release notes and the SHA-256 values below, not the files.

| Package | SHA-256 |
| --- | --- |
| `DEVI-Validate-Setup-1.0.3-win-x64.exe` (installer) | `f9359d1e126ad72bbc8cda8e8cfc9407d15258c0d905e922cacf780e5c865ecc` |
| `DEVI-Validate-1.0.3-win-x64.zip` (portable) | `42a990c2071ea71851213f6906e963cdb506b2bd065a92c50ce1bed9b2127ba8` |

Both packages include the .NET runtime, so nothing else needs to be installed. In the portable zip, `app\DEVI-Validate.exe` is the desktop app and `cli\devi-validate.exe` is the command line.

### Check your download

Before you run a download, confirm its SHA-256 matches the table above. In PowerShell:

```powershell
Get-FileHash .\DEVI-Validate-Setup-1.0.3-win-x64.exe -Algorithm SHA256
```

In Command Prompt:

```bat
certutil -hashfile DEVI-Validate-Setup-1.0.3-win-x64.exe SHA256
```

On Linux or macOS:

```bash
sha256sum DEVI-Validate-Setup-1.0.3-win-x64.exe
```

The value must match this README, the GitHub release notes, and the DEVI website. If it does not match, do not run the file.

The installer, its uninstaller, the desktop app, and the command line are Authenticode-signed through Microsoft Artifact Signing, with a timestamp. In PowerShell, `Get-AuthenticodeSignature .\DEVI-Validate-Setup-1.0.3-win-x64.exe` should report `Valid`. A valid signature does not replace the SHA-256 check. Windows SmartScreen can still warn about a newly signed release.

## Desktop application

The Windows app is a WPF program in `src/DeviValidate.Desktop`. It calls the same library as the command line.

The window uses the shared DEVI design system (`shared/Devi.Theme`). Hashing does not use a network connection or a cloud service. Check for updates is in Settings and in the More menu.

- The flow reads top to bottom: 1 Case details, 2 Evidence, 3 Expected hashes, 4 Verify, then the verdict, the counts, the results table, and Exports and tools at the bottom.
- Case details is open by default. Examiner and case number are required before Verify or Hash only runs; the hint beside Verify says what is missing, and pressing a disabled button opens Case details at the first empty field. Agency, Validated by, and Lab procedure are optional. The validation date fills in with today's date and stays editable. Lab procedure is an editable list of expected-hash sources, and a recognized FTK Imager log, sum file, or CSV picks the matching source.
- The top-right icons open the More menu (Verify a package, Run self-test, Check for updates, About), How to verify (F1), and Settings (Ctrl+,).
- Settings holds the examiner profile shared by every DEVI app on the computer (%LOCALAPPDATA%\DEVI\profile.json), the Validate defaults (%LOCALAPPDATA%\DEVI\Validate\settings.json), and the update check. The case number is never saved.
- Evidence and Expected hashes each take one or more files or folders. Every item shows as its own chip, and the X on a chip removes only that item.
- Step 2 is the file or folder to hash again. Step 3 is the hash another tool already wrote down, and it is required for a comparison. Hash only leaves it empty and does not verify.
- A verdict banner explains what a match or a mismatch can and cannot mean. The Match, Mismatch, Missing, and Extra counts appear after a run. The results table can be filtered by status, and hashes can be copied.

## Command-line usage

```bash
devi-validate hash <path> [--algorithm sha256|sha1|md5] [--output <manifest.json>]
devi-validate verify <path> --expected <file> [--output-dir <folder>]
devi-validate verify <file> --hash <hex>
devi-validate selftest [--output-dir <folder>]
devi-validate report <record.json> --output-dir <folder>
devi-validate check <record.json>
devi-validate verify-package <folder|package.zip> [--code <DV1-code>]
devi-validate update
```

`--examiner` and `--case` are optional labels stored in the record. They are blank unless you set them. `--validated-by`, `--validation-date`, and `--lab-procedure` are also optional. When they are blank, the record prints "Not recorded".

```bash
devi-validate hash ./extraction -o ../reports/manifest.json --examiner "A. Examiner" --case "2026-0142"
devi-validate verify ./extraction --expected ../reports/manifest.json --output-dir ../reports --examiner "A. Examiner" --case "2026-0142"
devi-validate verify ./image.dd --hash 9f86d081884c7d659a2feaa0c55ad015a3bf4f1b2b0b822cd15d6c15b0f00a08
devi-validate verify ./extraction --expected hashes.sha256 --output-dir ../reports
devi-validate selftest --output-dir ../reports --lab-procedure "SOP-HASH-1"
devi-validate report ../reports/<record>.json --output-dir ../reports
devi-validate check ../reports/<record>.json
```

`verify --output-dir` writes `<hash>.html`, `<hash>.json`, and `<hash>.pdf`. For one file, `<hash>` is the computed hash of that file. For a folder, it is the set hash. `--pdf <file>` writes an extra copy of the PDF. If a name is already in use, the tool writes `<hash>_2`, then `<hash>_3`, and does not replace the existing file.

`selftest` writes `selftest_<version>_<date>.pdf`, with matching HTML and JSON. The date is the local date of the run.

Relative paths are relative to the folder you pass. The folder's own name is not part of the path. A sum file created from inside that folder matches. For a single file, the recorded path is the file name.

Exit codes:

| Code | Meaning |
| --- | --- |
| 0 | The hash finished with nothing skipped, every compared file matched and nothing extra was present, the record hash matched, or every self-test check passed. |
| 1 | The command could not run. |
| 2 | A mismatch, a missing path, an extra file, a path that was not hashed, an integrity difference, or a failed self-test check was reported. |
| 130 | The run was canceled. |

`devi-validate --help` lists every option, including CSV column mapping (`--path-column`, `--hash-column`) and `--format`.

## How verification works

1. The evidence path is opened read-only and hashed. Folders are walked depth-first. Each regular file is streamed once.
2. Expected hashes are parsed from the source you pass. When `--algorithm` is omitted, the algorithm is inferred from the hash length. Mixed lengths are refused unless you choose one. An FTK Imager log that contains more than one algorithm uses SHA-256, then SHA-1, then MD5.
3. Paths are compared exactly after slash direction is normalized. `..` is rejected. A unique file name is used when the expected path is only a file name, or when it is absolute (for example a Windows path in an FTK log). Different directories are not paired by file name.
4. Each expected path is **Match** or **Mismatch**. An expected path that was not hashed is **Missing**. A hashed file that was not expected is **Extra**.
5. The verdict is one of:
   - All files match the expected hashes.
   - All expected files match. Additional files were found that are not in the expected list.
   - One or more files do not match the expected hashes.
   - One or more expected files were not found.
   - One or more files do not match the expected hashes, and one or more expected files were not found.

Results are ordered with mismatches first, then missing paths, then extra files, then matches.

### Expected hash sources

| Source | Notes |
| --- | --- |
| `--hash` | One hash, one evidence file. |
| GNU sum | `hash  path` or `hash *path`. Extensions `.md5`, `.sha1`, and `.sha256` are typical. |
| BSD sum | `SHA256 (path) = hash`, and the MD5 and SHA-1 forms. |
| Bare hash | A file that is only the hex digest, or `SHA256: digest`. |
| CSV / TSV | A header such as `path` and `sha256` is detected. Otherwise pass `--path-column` and `--hash-column` by name or 1-based index. |
| DEVI Validate JSON | A hash manifest, or a previous verification record. The record hash must match or the file is refused. Extra rows in an old verification record are not treated as expected. |
| FTK Imager text log | Lines such as `MD5 checksum: <hex> : verified`. The hex value is the expected hash. The word after the colon is ignored, and the file is hashed again. |

See [docs/vendor-formats.md](docs/vendor-formats.md) for Cellebrite, Magnet AXIOM, and E01 embedded hashes. Those are not parsed in this version.

An FTK Imager image log records the hash of the acquired data. Compare it with a single raw image. This version will not compare that hash with an E01 or L01 container, and it will not treat a multi-segment set as one file.

### Read-only output

Evidence is opened with read access only. The tool does not write, rename, or set timestamps inside the evidence location. On Linux it asks for `O_NOATIME` so a successful open does not update the access time. If the kernel refuses that flag, the file is still opened read-only, and the access time follows the mount policy. The tool never writes an access time or a write time itself.

Reports go to a folder you choose. The tool refuses to write that output inside the evidence location. For a single file, the evidence location is the folder that contains the file, so a report cannot be written next to that file. Symbolic links are resolved before the check, including a link whose target is inside the evidence folder.

### File names and the set hash

A verification export is named after the hash that was checked.

- One file: the file name is the lowercase computed hash, for example `ba7816bf....pdf`.
- A folder: the file name is the set hash. The set hash is the same algorithm applied to a canonical list of the files that were read. Each line is the relative path, a tab, the lowercase hash, and a line feed. The lines are sorted by path using ordinal order. UTF-8, no byte-order mark. The same files in any order produce the same set hash. Paths that were expected but not read are not included.

The HTML, JSON, and PDF for one run share that name. If `<hash>.pdf` already exists, the tool writes `<hash>_2.pdf` (and the same suffix for the HTML and JSON) and leaves the existing file in place.

### How to read the record

The PDF and HTML include this explanation after the results.

**What this record is.** This record independently recomputes hash values and compares them with values recorded by another tool or process. It is a hash verification record from DEVI Validate, an independent, third-party tool. The expected hashes came from the source named on the first page, such as an FTK Imager log, a sum file, a DEVI Validate manifest, or a value entered manually.

**What you do to verify.** Bring the hash the other tool already recorded. That may be a physical analyzer or other forensic report, an FTK Imager text log, a sum file, a CSV, or one pasted hash. This program does not re-image the drive and does not connect to the analyzer. For an FTK Imager log, compare the checksum with the single raw image, not with an E01 or L01 container. Hash only does not compare anything. A prosecutor, a defense attorney, or another examiner can repeat the check with `certutil -hashfile`, `Get-FileHash`, or `sha256sum`.

**How it works.** A cryptographic hash is a digital fingerprint of a file. The same bytes always produce the same hash. A different byte produces a different hash. DEVI Validate read the files and did not modify them. It used the algorithm named on the record. It did not use a network connection.

For a folder, the set hash is that same algorithm applied to a canonical list of the files that were read. Each line is the relative path, a tab, the lowercase hash, and a line feed. The lines are sorted by path. The same files in any order produce the same set hash. Paths that were expected but not read are not part of the set hash.

**What the results mean.** Match means the new hash is the same as the hash written down before. The file we read is the same file that hash describes. Anyone can check the file again with `sha256sum`, `certutil -hashfile`, or `Get-FileHash`. This check verifies the bytes only. It does not show who made the file, what the file means, or whether a chain of custody is complete. Match is not shown in red.

Mismatch is shown in red. The new hash is not the same as the hash written down before. The file we read is not the same as the file that hash describes. A red result can mean the file changed after the first hash was made, that this is a different file, that the expected hash was copied wrong or taken from another file, or that the first tool and this tool were pointed at different copies. A red result does not show who changed the file, when it changed, or why. It does not show that someone tampered with the file or deleted it. It only shows that the two hashes are not the same.

Missing means a path was on the expected list, but that file was not among the files we read, so its hash was not checked. Extra means we read a file that was not on the expected list. Missing and Extra are not shown in red.

**How to independently verify.** Anyone can recompute the file hashes with a standard tool and compare them with this record. Examples include `sha256sum`, `certutil -hashfile`, and `Get-FileHash`. To check this record itself, recompute the SHA-256 of its canonical JSON with the integrity hash set to an empty string, or run `devi-validate check` on the JSON file. The record hash covers the JSON record. It does not cover the HTML or PDF bytes.

**Validation and use.** DEVI Validate is open source. Its built-in self-test uses published test vectors. Each lab or agency should validate it under its own procedures before relying on it in casework. This record does not certify the tool, and it is not a statement of court acceptance, NIST approval, or CJIS compliance. The fields Validated by, Validation date, and Lab procedure/reference show what was entered when the tool was run. When a field is empty, the record prints "Not recorded".

**Limits.** This tool does not check what a file means, whether metadata is accurate, or whether a chain of custody is complete.

### Self-test

`devi-validate selftest` hashes published test vectors and runs a small read-only check.

- SHA-256 and SHA-1 use the empty string, the ASCII text `abc`, and, for SHA-256, the FIPS 180-4 one-block message.
- MD5 uses the empty string and `abc` from RFC 1321.
- The read-only check writes a synthetic file, hashes it through the same read-only path as evidence, and requires the contents, last write time, and access time to be unchanged.

The tool validation record includes the tool version, the SHA-256 of the core assembly when that file can be read, the operating system, the date and time with time zone, each check, and the overall result. It is not an evidence verification, and a passing result does not certify the tool for casework.

### Record hash

The HTML page, the PDF, and the JSON file all show a SHA-256. That hash covers the canonical JSON of the record with `integrity.hash` set to an empty string. Canonical JSON is UTF-8, compact, without a byte-order mark, in the property order this version writes. It does not cover the HTML or PDF bytes. Whitespace in the pretty JSON file is not part of the hash.

`devi-validate check <record.json>` recomputes it. A DEVI Validate JSON file used as an expected source is accepted only when this hash matches. The same check works on a tool validation record.

## Build from source

You need the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (8.0.100 or later, see `global.json`). The desktop app builds on Windows. The library, the command line, and the tests build on Windows, Linux, and macOS.

```bash
git clone https://github.com/Deviops-app/DEVI-Validate.git
cd DEVI-Validate

# Build everything and run the tests
dotnet test DeviValidate.sln --configuration Release

# Command line (framework-dependent; needs the .NET 8 runtime to run)
dotnet publish src/DeviValidate.Cli/DeviValidate.Cli.csproj -c Release -o ./publish

# Windows desktop app
dotnet build src/DeviValidate.Desktop/DeviValidate.Desktop.csproj -c Release
```

`publish/devi-validate` is the command (`devi-validate.exe` on Windows). On Linux and macOS the desktop project compiles as a Windows-targeted build so `dotnet test` still runs. The WPF window itself runs on Windows only.

To make the same self-contained installer and portable zip as an official release, see [BUILDING.md](BUILDING.md).

## Testing

The xUnit suite uses only synthetic files created during the run. It covers hashing against independent tools, parser edge cases, every result kind, set-hash ordering, read-only guarantees, the output-path guard, cancellation, record integrity, exit codes, and scale. GitHub Actions runs it on Windows for every push and pull request. [TESTING.md](TESTING.md) describes what the suite does and does not cover.

## Repository layout

| Path | Contents |
| --- | --- |
| `src/DeviValidate.Core` | Verification library: hashing, expected-hash parsers, verifier, records, packages, and self-test |
| `src/DeviValidate.Cli` | The `devi-validate` command line |
| `src/DeviValidate.Desktop` | The Windows WPF app |
| `shared/Devi.Theme` | DEVI design system for the Windows app (colors, type, controls, window frame) |
| `shared/Devi.Updates` | Signed update-check client |
| `shared/Devi.Brand` | Print colors and header used on every DEVI PDF |
| `shared/brand` | The DEVI Windows app icon and its generator |
| `tests/DeviValidate.Core.Tests` | xUnit test suite |
| `installer` | Release build scripts, Inno Setup script, layout and launch checks |
| `docs` | Update check, validation checklist, and vendor format notes |
| `assets/brand` | DEVI brand assets used by the app and the records |

## Contributing

Bug reports, documentation fixes, and parser contributions are welcome. Please read [CONTRIBUTING.md](CONTRIBUTING.md) and the [Code of Conduct](CODE_OF_CONDUCT.md) first. Report security issues privately as described in [SECURITY.md](SECURITY.md). Never attach real evidence, case material, or personal data to an issue or pull request.

## License

Copyright 2026 The DEVI Validate authors.

Licensed under the [Apache License, Version 2.0](LICENSE). See [NOTICE](NOTICE) and [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md) for third-party components.

As Section 6 of the license states, it does not grant permission to use the DEVI name or logo, except as needed to describe where the software came from.
