# Security Policy

DEVI Validate is used to check the integrity of digital evidence, so security and correctness reports are taken seriously and handled first.

## Supported versions

| Version | Supported |
| --- | --- |
| 1.0.x | Yes |
| 0.1.x (preview) | No. Please update to the latest 1.0 release. |

## Reporting a vulnerability

Please report vulnerabilities privately. Do not open a public issue.

- **Email:** admin@deviops.app, with "DEVI Validate security" in the subject line.
- **GitHub:** use **Report a vulnerability** on the Security tab of this repository to open a private advisory.

Include the DEVI Validate version, the operating system, and the steps or input needed to reproduce the problem. **Do not include real evidence, case material, or personal data.** A synthetic file or a description of the input is enough.

We aim to reply within 3 business days and to keep you informed while a fix is prepared. Once a fix is released, we will credit you in the release notes unless you would rather not be named.

## In scope

Reports are especially welcome for anything that could:

- change, rename, or change timestamps on evidence;
- produce a wrong hash, verdict, or record hash;
- write a report inside the evidence location;
- make hashing or verification use the network;
- get around the signature or SHA-256 checks in the update process.

## Security design

- Evidence is opened with read access only. The tool does not write, rename, or set timestamps in the evidence location. On Linux it requests `O_NOATIME`. If the kernel denies that flag, access time follows the mount policy, and the process still never calls a timestamp-setting function.
- Reports are refused inside the evidence location. The check uses the full path and resolves existing symbolic links.
- Symbolic links are not followed while hashing.
- Parser assemblies are never loaded from disk.
- No telemetry is collected or sent.
- The only network feature is the optional update check. It is off by default and runs only when you ask, or when you turn on "Check for updates when the app opens". It is one HTTPS GET of a signed version file from the DEVI download host, with no machine identifier and no query string. A download starts only after you confirm it, and only after its SHA-256 matches the signed version file. The installer is never started silently. See [docs/UPDATES.md](docs/UPDATES.md).

## Code signing

The update feed signature is separate from Authenticode code signing. Release packages and the DEVI program files are Authenticode-signed through Microsoft Artifact Signing, with a timestamp. Signing happens only on a maintainer's computer; no signing material is in this repository or in GitHub Actions. Windows SmartScreen can still warn about a newly signed release. Check downloads against the published SHA-256 values.

## No certification claim

This policy does not claim a certification, an accreditation, or a particular laboratory method. Each lab or agency should validate the build it uses under its own procedures.
