# Windows one-command rebuild of the DEVI Validate release (same layout as build-release.sh).
# Run from the repo root in PowerShell:  .\installer\build-release.ps1
# Needs .NET 8 SDK and Inno Setup 6 (ISCC.exe on PATH or in the default install folder).
$ErrorActionPreference = 'Stop'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'; $env:DOTNET_NOLOGO = '1'
Set-Location (Split-Path $PSScriptRoot -Parent)
$version = ([xml](Get-Content Directory.Build.props)).Project.PropertyGroup.Version | Select-Object -First 1
$name = "DEVI-Validate-$version-win-x64"; $out = Join-Path 'out' $name
if (Test-Path out) { Remove-Item out -Recurse -Force }
New-Item -ItemType Directory $out | Out-Null
$common = @('-c','Release','-r','win-x64','--self-contained','true','-p:DebugType=none','-p:GenerateDocumentationFile=false','-p:SatelliteResourceLanguages=en')
# Separate folders: devi-validate.exe and DEVI-Validate.exe are the same name on Windows.
dotnet publish src\DeviValidate.Cli\DeviValidate.Cli.csproj @common -o (Join-Path $out 'cli'); if ($LASTEXITCODE) { throw 'CLI publish failed' }
dotnet publish src\DeviValidate.Desktop\DeviValidate.Desktop.csproj @common -o (Join-Path $out 'app'); if ($LASTEXITCODE) { throw 'Desktop publish failed' }
Get-ChildItem $out -Recurse -Filter *.pdb | Remove-Item -Force
foreach ($f in 'LICENSE','NOTICE','THIRD-PARTY-NOTICES.md','README.md','TESTING.md','CHANGELOG.md','SECURITY.md') { Copy-Item $f $out }
New-Item -ItemType Directory (Join-Path $out 'docs') | Out-Null
Copy-Item docs\UPDATES.md, docs\VALIDATION-CHECKLIST.md (Join-Path $out 'docs')
Copy-Item release-kit\QUICKSTART.txt, release-kit\DOTNET-RUNTIME-LICENSE.txt, release-kit\DOTNET-RUNTIME-THIRD-PARTY-NOTICES.txt $out
# Guard: no CLI files beside the app, and the app EXE must be the GUI program.
$bad = Get-ChildItem (Join-Path $out 'app') -Filter 'devi-validate.*' | Where-Object { $_.Name -cne 'devi-validate.ico' -and $_.Name -clike 'devi-validate.*' }
if ($bad) { throw "CLI files in app\: $($bad.Name -join ', ')" }
if (-not (Test-Path (Join-Path $out 'app\DEVI-Validate.exe')) -or -not (Test-Path (Join-Path $out 'cli\devi-validate.exe'))) { throw 'exe missing' }
Add-Type -AssemblyName System.IO.Compression, System.IO.Compression.FileSystem
$zip = Join-Path (Resolve-Path out) "$name.zip"; $root = (Resolve-Path $out).Path
$za = [System.IO.Compression.ZipFile]::Open($zip, 'Create')
try { foreach ($f in Get-ChildItem $root -Recurse -File) { [void][System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($za, $f.FullName, $name + '/' + ($f.FullName.Substring($root.Length + 1) -replace '\\','/'), 'Optimal') } } finally { $za.Dispose() }
$iscc = (Get-Command ISCC.exe -ErrorAction SilentlyContinue).Source
if (-not $iscc) { $iscc = "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe" }
& $iscc /Q "/DAppVersion=$version" "/DSrcDir=$root" installer\DEVI-Validate.iss; if ($LASTEXITCODE) { throw 'ISCC failed' }
foreach ($f in "$name.zip", "DEVI-Validate-Setup-$version-win-x64.exe") {
  $h = (Get-FileHash (Join-Path out $f) -Algorithm SHA256).Hash.ToLower()
  Set-Content -NoNewline -Encoding ascii (Join-Path out "$f.sha256") "$h  $f`n"; "$h  $f"
}
