# Testing

DEVI Validate is checked with xUnit against synthetic files created during the run. The tests do not use case evidence or personal data. The tests do not call an external host.

```bash
dotnet test DeviValidate.sln --configuration Release
```

On a Linux build machine that command finishes with 71 passed and 3 skipped. The three skipped tests create a Windows directory junction. They are marked skipped on Linux. They are not counted as passed. On Windows they run, and the symbolic-link tests run only when the account can create a symbolic link. If it cannot, those tests skip with a message that names Developer Mode. They do not pass silently.

GitHub Actions runs the same `dotnet test` on `windows-latest` for every push and pull request, and also builds the WPF project.

## What the suite checks

`tests/DeviValidate.Core.Tests/ExaminerSuiteTests.cs` is the examiner-facing set. The other test files cover parsers, the verifier, exports, the self-test, the output-path guard, and symbolic links.

| Check | What it compares |
| --- | --- |
| Independent hashes | Empty file, 1 byte, 64 bytes, 4096 bytes, 1 MiB, and a file whose name contains `café`. SHA-256, SHA-1, and MD5. On Linux the result must match `sha256sum` / `sha1sum` / `md5sum`, `openssl dgst`, and Python `hashlib`, and those three must match each other. On Windows, files with at least one byte must match `certutil -hashfile` and `Get-FileHash`. |
| Sum and list parsing | A UTF-8 byte-order mark and uppercase hex on a GNU line, a trailing space on a path, a short hash, a `..` path, duplicate expected paths, a case-insensitive path collision, a broken CSV line, and JSON that is not a DEVI Validate record. |
| Result kinds | One match, one mismatch, one missing path, and one extra file in the same folder. |
| Set hash | The same files in a different order produce the same set hash. Swapping the hashes changes it. |
| Read-only | Bytes, last write time, access time, and the read-only attribute are unchanged after a hash. The content is read back only after those timestamps are compared, because a later read can update the access time. |
| Output location | A relative report path whose full path lands inside the evidence folder is refused. |
| Unreadable and changing files | A file deleted after the scan, and a file rewritten before it is opened, are listed as skipped. The run does not throw. The HTML names both reasons and does not call them symbolic links. |
| Cancel | Canceling while a 2 MiB file is read leaves the bytes and the last write time unchanged. |
| Records | JSON reloads and its record hash matches. HTML contains the verdict and the encoded file name. The PDF starts with `%PDF-` and contains the verdict. |
| Exit codes | `verify` returns 0 for a match, 2 for a mismatch, and 1 when neither `--hash` nor `--expected` is passed. `check` returns 0 for an intact record and 2 after a field is edited. |
| Scale | A path longer than 260 characters still hashes. A folder of 100,000 small files hashes in under 3 minutes. |

The published vectors in the self-test (`abc`, the empty string, and the SHA-256 one-block message) are checked separately in `ExportAndSelfTestTests`.

On Windows, `certutil -hashfile` fails on a 0-byte file with `0x800703ee ERROR_FILE_INVALID`. That is a certutil limitation. The empty-file check skips certutil and compares the DEVI Validate hash with `Get-FileHash` and the published empty-input constants: SHA-256 `e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855`, SHA-1 `da39a3ee5e6b4b0d3255bfef95601890afd80709`, and MD5 `d41d8cd98f00b204e9800998ecf8427e`. Use `Get-FileHash` for an empty file.

## Bugs found by this pass

These were fixed in the same change as the suite.

- A GNU sum line that started with a UTF-8 byte-order mark was rejected, even when the hash was valid. The reader now strips that mark. Uppercase hex was already accepted once the mark was gone.
- A file that could not be read, or that changed during the read, was stored on the manifest, but the HTML, the PDF, and the command output described every skipped path as a symbolic link. They now list each path with its own reason. The verdict says how many paths were not hashed. `hash` and `verify` exit 2 when any path was skipped, so a partial read is not reported as a clean pass. The Windows results table shows a Skipped row for each path.

## What a Linux run does not cover

- FlaUI, or any click-through of the WPF window. A Linux machine has no Windows desktop session, and the window is code-behind rather than a separate view model. The desktop project compiles on Linux as a Windows-targeted build (`EnableWindowsTargeting=true`). The window itself was not opened.
- `certutil` and `Get-FileHash`. The test calls them when `OperatingSystem.IsWindows()` is true. On Linux the Linux branch runs instead. The Windows CI job is where those two tools are compared.
- The three directory-junction tests. They skip on Linux. Windows CI runs them.
- A multi-gigabyte image. The largest streamed file in the suite is 2 MiB for cancellation, plus a 1 MiB hash compared with the external tools. Streaming uses 1 MiB reads, but a 2 GB image is not hashed by the suite.
- A permission-denied file created with a mode that this account cannot read. The unreadable-file check deletes the file after the scan, which produces the same I/O failure the hasher reports.

On a Linux build machine the full `dotnet test` run, including creation and hashing of the 100,000-file folder, takes one to two minutes.
