# Third-party notices

DEVI Validate is licensed under the Apache License, Version 2.0. See `LICENSE`. Every component below has a license that is compatible with Apache-2.0.

## Shipped with the application

These NuGet packages are copied into the published app and command line.

| Package | Version | License | Source |
| --- | --- | --- | --- |
| PDFsharp | 6.2.1 | MIT | <https://github.com/empira/PDFsharp> |
| QRCoder | 1.6.0 | MIT | <https://github.com/codebude/QRCoder> |
| Microsoft.Extensions.DependencyInjection | 8.0.1 | MIT | <https://github.com/dotnet/runtime> |
| Microsoft.Extensions.DependencyInjection.Abstractions | 8.0.2 | MIT | <https://github.com/dotnet/runtime> |
| Microsoft.Extensions.Logging | 8.0.1 | MIT | <https://github.com/dotnet/runtime> |
| Microsoft.Extensions.Logging.Abstractions | 8.0.2 | MIT | <https://github.com/dotnet/runtime> |
| Microsoft.Extensions.Options | 8.0.2 | MIT | <https://github.com/dotnet/runtime> |
| Microsoft.Extensions.Primitives | 8.0.0 | MIT | <https://github.com/dotnet/runtime> |
| System.Security.Cryptography.Pkcs | 8.0.1 | MIT | <https://github.com/dotnet/runtime> |
| System.Drawing.Common (Windows app only) | 6.0.0 | MIT | <https://github.com/dotnet/runtime> |
| Microsoft.Win32.SystemEvents (Windows app only) | 6.0.0 | MIT | <https://github.com/dotnet/runtime> |

PDFsharp writes the PDF record. QRCoder draws the QR code on validation packages. The Microsoft.Extensions and System packages come in through PDFsharp and the .NET libraries.

The release packages are self-contained and include the .NET 8 runtime and Windows Desktop runtime (MIT). Their license and third-party notices ship in every release as `DOTNET-RUNTIME-LICENSE.txt` and `DOTNET-RUNTIME-THIRD-PARTY-NOTICES.txt` (see `release-kit/`).

## Used only to build and test

These packages are not copied into the published application.

| Package | Version | License |
| --- | --- | --- |
| xunit (and xunit.core, xunit.assert, xunit.abstractions, xunit.extensibility.*) | 2.9.2 | Apache-2.0 |
| xunit.analyzers | 1.16.0 | Apache-2.0 |
| xunit.runner.visualstudio | 2.8.2 | Apache-2.0 |
| Microsoft.NET.Test.Sdk, Microsoft.TestPlatform.*, Microsoft.CodeCoverage | 17.12.0 | MIT |
| Newtonsoft.Json (test host) | 13.0.1 | MIT |
| System.Reflection.Metadata (test host) | 1.6.0 | MIT |

## Build tools

Release packages are built with Inno Setup (Inno Setup License) and signed with jsign (Apache-2.0). Neither is included in this repository or in the published application.
