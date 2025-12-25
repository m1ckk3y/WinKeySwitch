; WinKeySwitch installer (Inno Setup)
; Build: iscc installer\WinKeySwitch.iss

#define AppName "WinKeySwitch"
#define AppPublisher "m1ckk3y"
#define AppURL "https://github.com/m1ckk3y/WinKeySwitch"
#define AppExeName "WinKeySwitch.exe"

; Prefer a self-contained publish output (scripts\publish.ps1).
; Fallback to build output if publish folder doesn't exist.
#define PublishDir "..\\publish\\win-x64"
#define BuildDir "..\\bin\\Release\\net8.0-windows"

[Setup]
AppId={{F7F4F17E-0C34-4B11-9D44-4D27A1ED6E2E}
AppName={#AppName}
AppVersion=1.2.0
AppPublisher={#AppPublisher}
AppPublisherURL={#AppURL}
AppSupportURL={#AppURL}
AppUpdatesURL={#AppURL}
DefaultDirName={autopf}\\{#AppName}
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
OutputDir=..\\dist
OutputBaseFilename=WinKeySwitchSetup
Compression=lzma
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

[Languages]
Name: "en"; MessagesFile: "compiler:Default.isl"
Name: "ru"; MessagesFile: "compiler:Languages\\Russian.isl"

[CustomMessages]
en.TaskAutostart=Start WinKeySwitch automatically when I sign in
en.GroupOptions=Options:
en.RunLaunch=Launch WinKeySwitch
ru.TaskAutostart=Запускать WinKeySwitch автоматически при входе
ru.GroupOptions=Параметры:
ru.RunLaunch=Запустить WinKeySwitch

[Tasks]
Name: "autostart"; Description: "{cm:TaskAutostart}"; GroupDescription: "{cm:GroupOptions}"; Flags: unchecked

[Files]
; Use published output if present (publish.ps1 uses PublishSingleFile, so there may be only .exe)
Source: "{#PublishDir}\\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs; Excludes: "*.pdb"; Check: DirExists(ExpandConstant('{#PublishDir}'))

; Fallback to framework-dependent build output
Source: "{#BuildDir}\\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs; Excludes: "*.pdb"; Check: not DirExists(ExpandConstant('{#PublishDir}'))

[Icons]
Name: "{autoprograms}\\{#AppName}"; Filename: "{app}\\{#AppExeName}"
Name: "{autoprograms}\\{#AppName} (Uninstall)"; Filename: "{uninstallexe}"

[Registry]
; Autostart checkbox → HKCU Run
Root: HKCU; Subkey: "Software\\Microsoft\\Windows\\CurrentVersion\\Run"; ValueType: string; ValueName: "WinKeySwitch"; ValueData: """{app}\{#AppExeName}"""; Tasks: autostart; Flags: uninsdeletevalue

[Run]
Filename: "{app}\\{#AppExeName}"; Description: "{cm:RunLaunch}"; Flags: nowait postinstall skipifsilent
