# Contributing to DEVI Validate

Thank you for your interest in DEVI Validate. It is a small .NET 8 codebase: a verification library, a command line, and a Windows WPF app. Every change must keep evidence read-only and keep hashing and verification offline.

Please follow the [Code of Conduct](CODE_OF_CONDUCT.md).

## Reporting bugs and asking for features

- **Bugs:** open an issue with the bug report form. Include the DEVI Validate version (`devi-validate --version` or the About window), the operating system, the steps to reproduce, and the relevant lines from `%LocalAppData%\DEVI\Validate\logs`.
- **Features:** open an issue with the feature request form.
- **Security issues:** do not open a public issue. Follow [SECURITY.md](SECURITY.md).

Never attach real evidence, case material, or personal data to an issue or pull request.

## Build and test

```bash
dotnet test DeviValidate.sln --configuration Release
```

That builds the library, the command line, the shared code, the tests, and, on Linux and macOS, a Windows-targeted build of the desktop project. Build and run the WPF app on Windows:

```bash
dotnet build src/DeviValidate.Desktop/DeviValidate.Desktop.csproj -c Release
```

The desktop window and the `devi-validate` command must both call `VerificationWorkflow` so they apply the same rules. See [TESTING.md](TESTING.md) for what the suite covers and [BUILDING.md](BUILDING.md) for release packaging.

## Tests

Use xUnit. Create synthetic files inside the test and delete them afterward. Do not commit a fixture that cannot be published.

- Hash tests include a known answer, such as the SHA-256 of the ASCII string `abc`.
- Parser tests use a short synthetic sample of the format.
- Verdict tests cover Match, Mismatch, Missing, and Extra.
- Guard tests show that a report path inside the evidence location is refused and that no file is created.

## Vendor parsers

Cellebrite, Magnet AXIOM, and hashes stored inside E01 files are not parsed yet. The extension point is `IVendorReportParser`, registered from `VendorParserRegistry.RegisterBuiltIns`. Plugins are never loaded from disk. Add a parser only when the format is publicly documented or can be tested against known synthetic input, and update [docs/vendor-formats.md](docs/vendor-formats.md) in the same change.

## Report wording

Result text states what the tool observed: the hashes match, the hashes differ, a path was not found, or a file was not in the expected list. It never describes a cause. Defaults must not contain an organization name or logo.

The record hash covers the canonical JSON described in the README. If a change alters the JSON property order or timestamp encoding, say so in the changelog, because older records will no longer match `devi-validate check`.

## Pull requests

1. One logical change per pull request. Keep refactoring, behavior changes, and release packaging separate.
2. Fill in the pull request template: what changed, why, how you tested it, and anything that affects record formats, exit codes, network behavior, or the update check.
3. Every behavior change adds a line to `CHANGELOG.md` under an "Unreleased" heading.
4. The CI build and tests must pass on Windows.
5. A DEVI maintainer reviews and merges every change.

The repository never contains credentials, signing keys, internal infrastructure details, personal information, or case data.

## License

By contributing, you agree that your contributions are licensed under the Apache License, Version 2.0. See [LICENSE](LICENSE).
