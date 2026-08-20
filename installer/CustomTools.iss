; Inno Setup script for CustomTools.
;
; Compiled with: ISCC.exe installer\CustomTools.iss /DMyAppVersion=X.Y
; (MyAppVersion falls back to a dev placeholder below if not passed via /D,
; so a plain `ISCC.exe installer\CustomTools.iss` still works for local testing.)
;
; NOTE on [Files] below: CTRegistryTree.csproj only redirects its Debug output
; into CustomTools\bin\Debug\Plugins\ (see CLAUDE.md). Release builds do NOT
; get that redirect - CTRegistryTree.dll lands in its own CTRegistryTree\bin\Release\,
; so it has to be sourced from there explicitly, not swept up by the CustomTools
; output copy below.

#ifndef MyAppVersion
  #define MyAppVersion "0.0.0-dev"
#endif

#define MyAppName "CustomTools"
#define MyAppPublisher "Adam Skowronski"
#define MyAppURL "https://github.com/askowron/CustomTools"
#define MyAppExeName "CustomTools.exe"

[Setup]
AppId={{7B7E2C7A-6A0B-4C7E-9C3C-9C6E9E3A9B10}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
PrivilegesRequired=admin
ArchitecturesInstallIn64BitMode=x64compatible
AppMutex=CustomToolsSingleInstance
CloseApplications=yes
RestartApplications=yes
OutputDir=Output
OutputBaseFilename=CustomToolsSetup
Compression=lzma
SolidCompression=yes
WizardStyle=modern

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Files]
Source: "..\CustomTools\bin\Release\*"; DestDir: "{app}"; Excludes: "*.pdb,*.xml,*.vshost.*"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\CTRegistryTree\bin\Release\CTRegistryTree.dll"; DestDir: "{app}\Plugins"; Flags: ignoreversion
Source: "..\CTRegistryTree\bin\Release\pl\CTRegistryTree.resources.dll"; DestDir: "{app}\Plugins\pl"; Flags: ignoreversion
Source: "..\CTRegistryTree\bin\Release\de\CTRegistryTree.resources.dll"; DestDir: "{app}\Plugins\de"; Flags: ignoreversion
Source: "..\CTRegistryTree\bin\Release\es\CTRegistryTree.resources.dll"; DestDir: "{app}\Plugins\es"; Flags: ignoreversion
Source: "..\CTRegistryTree\bin\Release\it\CTRegistryTree.resources.dll"; DestDir: "{app}\Plugins\it"; Flags: ignoreversion

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\Uninstall {#MyAppName}"; Filename: "{uninstallexe}"

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Launch {#MyAppName}"; Flags: nowait postinstall skipifsilent
