# Validation checklist

Use this list to check a DEVI Validate build on your own machine before anyone relies on it. Use files you created for the check. Do not use case evidence for the first run.

This checklist does not certify the tool, and it is not a statement of court acceptance or laboratory accreditation. Record what you did, on which build, and whether each box passed.

Fill in the header, then mark each box. A failed box means do not use that build for casework until the failure is understood.

## Build under test

| Field | Value |
| --- | --- |
| Date | |
| Examiner | |
| Machine | |
| Operating system | |
| DEVI Validate version | |
| How the program was built or obtained | |
| Lab procedure or reference | |

Version comes from `devi-validate --version`, from the About window, or from the first line of a self-test record.

## Automated checks

- [ ] `dotnet test DeviValidate.sln --configuration Release` finished with no failures.
- [ ] On Windows, the three junction tests ran, or the log shows they ran. A skip that says the test is not on Windows is a failure if you are on Windows.
- [ ] Symbolic-link tests either passed or skipped with the Developer Mode message. A skip is not a pass. If they skipped, create one symbolic link by hand in the manual section below.

## Self-test

- [ ] `devi-validate selftest --output-dir <folder outside any evidence>` printed that every check passed, and the process exited 0.
- [ ] The PDF, HTML, and JSON are named `selftest_<version>_<date>` and sit outside any evidence folder.
- [ ] `devi-validate check` on that JSON exits 0.

## Known file

Create a text file whose only bytes are `abc` (no line ending). The SHA-256 of those bytes is:

`ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad`

- [ ] `devi-validate hash` on that file prints the SHA-256 above.
- [ ] On Windows, `certutil -hashfile <file> SHA256` prints the same value. This file has bytes in it, so certutil can read it.
- [ ] On Windows, `(Get-FileHash -Algorithm SHA256 -LiteralPath <file>).Hash` prints the same value.
- [ ] On Windows, create a separate 0-byte file and hash it with `Get-FileHash`, not with certutil. `certutil -hashfile` fails on a 0-byte file with `0x800703ee ERROR_FILE_INVALID`. The empty SHA-256 is `e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855`.
- [ ] On Linux or macOS, `sha256sum` and `openssl dgst -sha256` print the same value.
- [ ] `devi-validate verify <file> --hash ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad --output-dir <folder outside the file's folder>` exits 0.
- [ ] The HTML and PDF say the file matches, and the result is not shown as a red mismatch.
- [ ] The report folder is not inside the folder that contains the file. Writing the report beside the file must be refused.

## A difference you introduce

Copy the file and change one byte.

- [ ] Verify the changed file against the SHA-256 above. The result is a red mismatch, and the process exits 2.
- [ ] The record says the two hashes are not the same. It does not name a person or a cause.
- [ ] Restore or discard the changed copy. Do not leave it next to a known-good file you might hash later.

## A folder

Make a folder with two files you created. Hash it once and keep the JSON manifest outside the folder. Add a third file. Verify the folder against the saved manifest.

- [ ] The two original files match.
- [ ] The third file is Extra.
- [ ] Exit code is 2.
- [ ] Repeat the verify after renaming one expected file. That path is Missing, and exit code is 2.

## Read-only

Pick a file you can afford to inspect. Note its size, last write time, and SHA-256 from `certutil`, `Get-FileHash`, or `sha256sum` before you start.

- [ ] After `devi-validate hash` or `verify`, the size and SHA-256 from the independent tool are unchanged.
- [ ] The last write time is unchanged.
- [ ] On Linux, if the open was allowed to use `O_NOATIME`, the access time is unchanged. If the kernel refused that flag, say so in the notes. The tool must still not set a timestamp itself.

## Links

- [ ] A symbolic link inside a folder is not followed. The record or the command output lists it as a symbolic link, and the target's bytes are not hashed as if they were the link.
- [ ] Passing the symbolic link itself as the evidence path is refused.

## Sum file

- [ ] A `sha256sum` file produced for your known file verifies with exit code 0.
- [ ] The same line with the hash in uppercase still verifies.
- [ ] A path that contains `..` is refused.

## Record hash

- [ ] `devi-validate check` on a verification JSON you just wrote exits 0.
- [ ] After you change one character in a copy of that JSON, `check` exits 2. Discard the copy.

## Notes

Write what failed, what you skipped, and which independent tool you used.

| Item | Pass or fail | Notes |
| --- | --- | --- |
| Automated tests | | |
| Self-test | | |
| Known SHA-256 | | |
| Independent tool (`certutil`, `Get-FileHash`, `sha256sum`, or `openssl`) | | |
| Mismatch | | |
| Extra and missing | | |
| Read-only | | |
| Symbolic link | | |
| Record hash | | |

Examiner signature and date:
