# Changelog

## 1.0.3 - 2026-10-07

Evidence and Expected hashes take several items, and text fields share one focus border. No change to how hashes are computed or compared.

- Evidence and Expected hashes each take more than one file or folder. Every selected item is its own chip, and the X on that chip removes only that item. A pasted hash stays beside the chosen hash files. Escape clears the field that has focus and leaves the other field as it is. Choosing or dropping several items at once adds all of them. Clearing an expected hash also drops that item's suggested lab procedure when no remaining item suggests one, the parsed hashes from the removed item, and the previous match, so the next verification does not reuse them.
- A focused text field shows one border: a 2px blue-to-purple gradient with an even rounded corner, the same accent stops as deviops.app.
- Records, reports, packages, and the update check's user agent carry version 1.0.3. Installed 1.0.0, 1.0.1, and 1.0.2 copies are offered 1.0.3 through Check for updates.

## 1.0.2 - 2026-10-06

Case details come first and are required for hashing in the window. No change to how hashes are computed or compared.

- The window reads top to bottom: 1 Case details, 2 Evidence, 3 Expected hashes, 4 Verify, then the verdict, the results, and Exports and tools at the bottom. The step badges and the instructions use the new numbers.
- Examiner and case number are required before Verify or Hash only runs in the window. Each is marked Required. The hint beside Verify names what is missing, and pressing a disabled button opens Case details, marks the empty field in red, and puts the cursor in it. The command line, Run self-test, and Verify a package do not require them.
- Case details is open by default. The validation date fills in with today's date on launch and on Start new. It stays editable, shows in the computer's date format, and is written to records as an ISO date (YYYY-MM-DD).
- New Agency or department field in Case details, printed on the PDF, HTML, JSON record, and validation package README and manifest. Records without an agency are byte-for-byte the same shape as 1.0.1 records, so their integrity hashes do not change. A record that includes an agency needs DEVI Validate 1.0.2 or later to read it.
- Lab procedure/reference is now a list you can pick from or type in, naming where the expected hash came from: "Expected hash from" Autopsy, Belkasoft X Forensic, Cellebrite Inseyets Physical Analyzer (Inseyets.PA), Cellebrite Inseyets UFED (Inseyets.UFED), dc3dd or dd log, Exterro FTK (Forensic Toolkit), Exterro FTK Imager, FTK Imager log (AccessData era), Guymager, Magnet Axiom, Magnet Graykey, MSAB XRY, OpenText Forensic (formerly EnCase), OpenText Tableau forensic imager (hardware), Oxygen Forensic Detective, or X-Ways Forensics, then Provider return hash (warrant return), Sum file (sha256sum / md5sum), CSV hash list, and Other (type your own). Product names identify the source only; DEVI is not affiliated with these vendors. A recognized FTK Imager log, sum file, or CSV picks the matching entry when the field is empty. Whatever is shown is written to the PDF, HTML, JSON, and package exactly as typed. It stays optional, and free text still works.
- The "Read this before you verify" banner is gone. A round help icon at the top right (How to verify, also F1) opens the full instructions.
- A Settings icon (Ctrl+,) opens Settings: the examiner profile (examiner name, agency or department, and optional unit, title or badge, and email or phone), default Validated by, default lab procedure, default algorithm, and the update check (check when the app opens, and Check for updates now). The profile fills Examiner and Agency on each new run, so only the case number is left to type. The case number is never saved. The profile is stored at %LOCALAPPDATA%\DEVI\profile.json and is shared with the other DEVI apps on the computer; Validate's defaults are at %LOCALAPPDATA%\DEVI\Validate\settings.json. Nothing is sent over the network and no evidence data is stored. Clear saved profile removes both. The update choice from 1.0.0 and 1.0.1 carries over.
- The first examiner name and agency typed in Case details are remembered when no profile exists yet.
- Verify a package, Run self-test, Check for updates, and About moved to a More menu beside the help icon. Verify is the one primary button.
- Quieter layout: plain evidence and expected-hash panels without dashed borders or helper subtitles, the algorithm beside the Expected hashes title, the paste box beside Choose file, and the Match, Mismatch, Missing, and Extra counts hidden until a run.
- Collapsible sections (Case details, Exports and tools) have a full-width clickable header with a Show/Hide label and chevron, a hover state, a hand cursor, and a keyboard focus ring.
- Export HTML, Export JSON, and Start new (formerly Clear) sit with Export PDF and Validation package in Exports and tools, which opens after a run.
- New app icon shared by every DEVI Windows app: a rounded-square tile in the DEVI colors with the white DEVI mark, drawn separately for small sizes so it stays sharp at 16 px. It is the program, window, taskbar, Start menu, desktop shortcut, Setup, and Add/Remove Programs icon. Setup tells Windows to refresh its icon cache so existing shortcuts pick it up.
- Records, reports, packages, and the update check's user agent carry version 1.0.2. Installed 1.0.0 and 1.0.1 copies are offered 1.0.2 through Check for updates.

## 1.0.1 - 2026-10-06

Brings the Windows app in line with DEVI Decrypt and DEVI Registry. No change to hashing, verification, reports, or validation packages.

- The Match, Mismatch, Missing, and Extra counts sit on the shared bordered card (`StepCard`), the same surface as the panels in DEVI Decrypt and DEVI Registry, in place of tinted color blocks. The counts keep their status colors.
- Check for updates uses the shared card styles: the result band is a `CalloutCard` with a status-colored edge, the version, installer, and portable zip details sit on `StepCard`s, and the ready-to-install notice has an amber edge in place of an amber fill.
- About follows the DEVI Decrypt and DEVI Registry layout: the DEVI wordmark, the tool name and version beneath it, and a copyright line.
- The Case details heading uses `Text.Heading`, the same as Exports and tools.
- `app.manifest` declares Windows 10 and 11 support and `asInvoker`, as the other two apps do.
- The installer opens DEVI Validate when setup finishes. The option can be cleared on the last page.
- Records, reports, packages, and the update check's user agent carry version 1.0.1. The command-line update check reads the version from `ToolInfo` instead of a fixed string.
- Installed 1.0.0 copies are offered 1.0.1 through Check for updates.

## 1.0.0 - 2026-10-05

Rebuilt in place on 6 October 2026 (version unchanged):

- Validation package: Create validation package writes one folder (and a zip of it) with the validation record as JSON, the PDF report, a README, and package-manifest.json listing the SHA-256 of every file, the tool version and executable SHA-256, the examiner and case details entered, and UTC and local times. The PDF carries a QR code and a short verification code. With a code-signing certificate from the Windows certificate store, the PDF and the manifest are also digitally signed (CMS).
- Verify a package re-checks a package folder or zip offline: every file hash, the manifest integrity hash, the verification code, and the signature when present. The CLI has the same check: `devi-validate verify-package <folder|zip> [--code DV1-...]`.
- The window, dialogs, and PDF use the shared DEVI design system that DEVI Decrypt and DEVI Registry also use: near-black canvas, thin borders, a restrained blue accent, and the same type and spacing as deviops.app.

First working release of the Windows app. It replaces 0.1.3, whose window never opened.

- 0.1.3 started and stayed running in the background, but no window ever appeared. Each launch from the shortcut or the zip left another hidden `DEVI-Validate.exe` process. The 0.1.3 sources were rebuilt from the 0.1.1 assemblies, and that rebuild cannot recover the line in `App.xaml` that tells Windows which window to open first (`StartupUri`). The app now creates and shows its main window itself.
- The window itself also could not load in 0.1.3. The project marked the window icon (`Assets\devi.ico`) as a loose file next to the program, so Windows looked for a file that was never shipped and the main window failed with "Could not find a part of the path ...\Assets\devi.ico". The icon is embedded again, and the release check now fails a build that expects loose files.
- Only one copy runs per Windows session. Opening DEVI Validate again brings the existing window to the front instead of starting another process.
- If no window has appeared 30 seconds after launch, the app writes the reason to the log and closes, so a failed start never leaves a hidden process behind. If a hung copy is still holding the single-instance lock, the next launch closes it and opens normally.
- The log under `%LocalAppData%\DEVI\Validate\logs` now records each startup step, the window handle, position, and screen area, and how long the window took to appear. A window that would open off-screen is moved onto the screen.
- `installer/smoke-launch.ps1` checks a build on Windows: a window appears, a second launch exits, and only one process is left.
- The installer, its uninstaller, `app\DEVI-Validate.exe`, `cli\devi-validate.exe`, and the DEVI assemblies are Authenticode-signed through Microsoft Artifact Signing, with an RFC 3161 timestamp. The 1.0.0 files were re-published signed. The program code is unchanged, and the version stays 1.0.0.

## 0.1.3 - 2026-10-05

Fixes the 0.1.1 and 0.1.2 Windows app opening for a moment and then closing.

- Root cause: the 0.1.1 package put the command-line tool (`devi-validate.exe`, `.dll`, `.deps.json`, `.runtimeconfig.json`) in the same `app` folder as the window (`DEVI-Validate.exe`, ...). Windows treats those names as the same file, so unzipping or installing could replace the window program with the command-line tool. Double-clicking then opened a console that closed at once.
- The zip and the installer now keep the window in `app` and the command-line tool in `cli`, as 0.1.0 did. The build fails if any folder holds two names that differ only by case, or if `app\DEVI-Validate.exe` is not a Windows GUI program.
- The installer removes the mixed 0.1.1/0.1.2 files from the install folder before it copies the new ones, so an upgrade in place repairs a broken install.
- The app now logs startup and any unexpected error to `%LocalAppData%\DEVI\Validate\logs` and shows a message with the log path instead of closing silently.
- The optional update check on open can no longer close the app. Any failure is logged and ignored.
- Double-clicking `cli\devi-validate.exe` keeps its window open and says how to open the app.
- Installer and portable zip carry the same version (0.1.3).

## 0.1.1 - 2026-10-05

First packaged Windows build that includes the optional update check, UTC and local times on the record, and the export-location dialog.

- Check for updates shows this copy, the published version, the release notes, and the SHA-256 of the installer and the portable zip before a download starts.
- The signed version file is published on the DEVI download host next to the release files. It is signed offline with the DEVI release key.
- Ship a self-contained `app` folder in `DEVI-Validate-0.1.1-win-x64.zip`. `devi-validate.exe` is in that folder with the window. The Inno Setup script writes `DEVI-Validate-Setup-0.1.1-win-x64.exe`.

## 0.1.0 - 2026-10-04

First public release.

- Hash a file or a folder with SHA-256, SHA-1, or MD5, streaming each file and reporting progress.
- Verify against a pasted hash, GNU and BSD sum files, CSV or TSV, an FTK Imager text log, or a DEVI Validate JSON record.
- Report Match, Mismatch, Missing, and Extra, with one overall verdict.
- Write a print-friendly HTML record, a PDF, and a JSON manifest, including a SHA-256 of the canonical JSON.
- Name verification exports with the computed hash, or with a set hash when the evidence is a folder. Do not overwrite an existing export.
- State, in plain language, that the record independently recomputes hashes and compares them with values from another tool or process.
- Record optional validation fields. Blank fields print "Not recorded".
- Run `devi-validate selftest` against published SHA-256, SHA-1, and MD5 vectors and a read-only timestamp check.
- Show a hash mismatch in red, with a plain explanation of what a red result can and cannot mean.
- Explain a match in the same plain language, including what the check can and cannot verify.
- Use the DEVI blue accent on the PDF and HTML records. The PDF stays light and print-friendly.
- Restyle the Windows app as a dark window: centered on the work area, with labeled fields, drop zones, a verdict banner, and a results table.
- Explain, in the Windows app and on the verification record, where the comparison hash must come from and what a match or a mismatch can prove.
- Use the official DEVI wordmark and D mark on the window, the PDF, and the HTML record.
- Skip symbolic-link tests when the account cannot create them, and cover the same reparse guard with a Windows directory junction. Run the tests on Windows in CI.
- State that the tool is offline, read-only, and does not collect data. Security reports go to contact@deviops.app. Third-party licenses are listed in THIRD-PARTY-NOTICES.md.
- Report a file that cannot be read, or that changes while it is read, instead of failing the whole run. The record, the command output, and the Windows results table list each skipped path and the reason. A hash or verify run that skips a path exits 2.
- Read a GNU sum line that begins with a UTF-8 byte-order mark. Uppercase hexadecimal digits are accepted.
- Add examiner tests in `ExaminerSuiteTests`, with `TESTING.md` and `docs/VALIDATION-CHECKLIST.md`.
- On Windows, compare an empty file with `Get-FileHash`. `certutil -hashfile` rejects a 0-byte file. Test cleanup removes a directory junction before it deletes the temp folder.
- Hide button access-key underscores until Alt is held. Keep the version in the title bar, and open the window at 1200 by 860 so the results stay in view.
- Widen the path column on the verification record. Break a path at a slash, and break a long segment at a dot, hyphen, or underscore. Keep the size on one line. The HTML record uses the same breaks.
- Show the verification time in UTC and in the local time zone, on the record and in the window. If a report would be saved inside the evidence folder, a dialog explains that the file would change the folder hash and suggests a folder outside the evidence. After a save, the window shows the full path and can open the file or its folder. A shorter window scrolls. A full window still fits the steps and the results.
- Add an optional update check for Windows. Check for updates, or `devi-validate update`, fetches a signed version file. The check is off at launch unless you turn it on. A failed check leaves hashing offline. A download is verified with SHA-256 and is not installed until you confirm it. The same feed can later list DEVI Decrypt.
- Refuse to write reports inside the evidence location. Open evidence read-only and do not follow symbolic links.
- Ship a .NET 8 library, a `devi-validate` command, and a Windows WPF application.
- Leave Cellebrite, Magnet AXIOM, and E01 embedded hashes as an in-process parser extension point.
