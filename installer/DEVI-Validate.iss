; DEVI Validate installer (Inno Setup 6).
; Build with installer/build-release.sh, which stages the release folder first:
;   <SrcDir>\app\  self-contained Windows app (DEVI-Validate.exe)
;   <SrcDir>\cli\  self-contained command-line tool (devi-validate.exe)
; app\ and cli\ MUST stay separate: Windows file names are case-insensitive, so
; devi-validate.exe and DEVI-Validate.exe in one folder overwrite each other
; (that was the 0.1.1/0.1.2 "opens then closes" bug).

#ifndef AppVersion
  #define AppVersion "1.0.3"
#endif
#ifndef SrcDir
  #define SrcDir "..\out\DEVI-Validate-" + AppVersion + "-win-x64"
#endif
#define AppName "DEVI Validate"
#define AppExeName "DEVI-Validate.exe"

[Setup]
; Same AppId as the 0.1.2 installer so upgrades replace it in place.
AppId={{7C4E9A2B-6F15-4D8E-9A33-1B6E5D0C4A18}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher=DEVI
AppPublisherURL=https://deviops.app
AppSupportURL=https://deviops.app/tools/devi-validate/
AppUpdatesURL=https://deviops.app/tools/devi-validate/
AppCopyright=Copyright 2026 The DEVI Validate authors
VersionInfoVersion={#AppVersion}.0
VersionInfoProductName={#AppName}
VersionInfoProductVersion={#AppVersion}
VersionInfoCompany=DEVI
VersionInfoDescription={#AppName} Setup
; Per-user by default (no admin): {autopf} = %LocalAppData%\Programs.
; The dialog still allows an all-users install into Program Files. The app never
; writes beside its EXE (settings, logs, and downloads live under the user profile),
; so a read-only Program Files folder is fine.
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
UsePreviousAppDir=yes
OutputDir=..\out
OutputBaseFilename=DEVI-Validate-Setup-{#AppVersion}-win-x64
SetupIconFile=..\src\DeviValidate.Desktop\Assets\devi.ico
UninstallDisplayIcon={app}\{#AppExeName}
UninstallDisplayName={#AppName} {#AppVersion}
; The app icon changed in this release. Windows caches shortcut and Add/Remove Programs icons;
; ChangesAssociations makes Setup call SHChangeNotify(SHCNE_ASSOCCHANGED) when it finishes so the
; shell redraws them with the new icon.
ChangesAssociations=yes
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.14393
CloseApplications=yes
RestartApplications=no
#ifdef Sign
; Set by installer/sign-release.sh, which passes /DSign and /Sdevisign=<command>.
; ISCC signs the uninstaller and then Setup itself.
SignTool=devisign
SignedUninstaller=yes
#endif

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[InstallDelete]
; Repair 0.1.1/0.1.2 installs: those put the command-line tool beside the app, and on
; Windows devi-validate.* and DEVI-Validate.* are the same files. Remove the mixed set
; before copying, so the app files below are guaranteed to be the window program.
Type: files; Name: "{app}\devi-validate.exe"
Type: files; Name: "{app}\devi-validate.dll"
Type: files; Name: "{app}\devi-validate.deps.json"
Type: files; Name: "{app}\devi-validate.runtimeconfig.json"
Type: files; Name: "{app}\devi-validate.pdb"

[Files]
; Window app at the install root (shortcuts point here).
Source: "{#SrcDir}\app\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
; Command-line tool in its own subfolder (never the same folder as DEVI-Validate.exe).
Source: "{#SrcDir}\cli\*"; DestDir: "{app}\cli"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "{#SrcDir}\QUICKSTART.txt"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SrcDir}\LICENSE"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SrcDir}\NOTICE"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SrcDir}\THIRD-PARTY-NOTICES.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#SrcDir}\CHANGELOG.md"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
; Explicit WorkingDir + IconFilename so the shortcut does not depend on Explorer
; extracting the icon from an unsigned EXE.
Name: "{autoprograms}\{#AppName}"; Filename: "{app}\{#AppExeName}"; WorkingDir: "{app}"; IconFilename: "{app}\devi-validate.ico"; IconIndex: 0
; Current user's desktop ({userdesktop}), regardless of install folder or elevation.
Name: "{userdesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"; WorkingDir: "{app}"; IconFilename: "{app}\devi-validate.ico"; IconIndex: 0; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExeName}"; WorkingDir: "{app}"; Description: "{cm:LaunchProgram,{#AppName}}"; Flags: nowait postinstall skipifsilent
