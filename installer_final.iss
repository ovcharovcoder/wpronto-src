; Inno Setup Script for WPronto - ENGLISH ONLY

[Setup]
AppId=WPronto
AppName=WPronto — Local WP Environment
AppVersion=2.0
AppPublisher=Andrii Ovcharov
AppPublisherURL=https://github.com/yourusername/WPronto
AppSupportURL=https://github.com/yourusername/WPronto
AppUpdatesURL=https://github.com/yourusername/WPronto
DefaultDirName={autopf}\WPronto
DefaultGroupName=WPronto
LicenseFile=C:\WPronto\license.txt
OutputDir=C:\WPronto\Installer
OutputBaseFilename=WPronto
SetupIconFile=C:\\WPronto\\src\\WPLaunchGUI\\app.ico
Compression=lzma
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayIcon={app}\WProntoGUI.exe

; ===== ВИБІР ДИСКА (ПРАВИЛЬНІ ЗНАЧЕННЯ) =====
DisableDirPage=auto
AllowRootDirectory=yes
AllowNetworkDrive=yes
UsePreviousAppDir=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional icons:"

[Files]
Source: "C:\WPronto\publish\WProntoGUI.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "C:\WPronto\publish\app.ico"; DestDir: "{app}"; Flags: ignoreversion
Source: "C:\WPronto\publish\WProntoGUI.deps.json"; DestDir: "{app}"; Flags: ignoreversion
Source: "C:\WPronto\publish\WProntoGUI.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "C:\WPronto\publish\WProntoGUI.runtimeconfig.json"; DestDir: "{app}"; Flags: ignoreversion
Source: "C:\WPronto\core\*"; DestDir: "{app}\core"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "C:\WPronto\config\*"; DestDir: "{app}\config"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "C:\WPronto\data\*"; DestDir: "{app}\data"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "C:\WPronto\template\*"; DestDir: "{app}\template"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "C:\WPronto\www\*"; DestDir: "{app}\www"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "C:\WPronto\about.txt"; DestDir: "{app}"; Flags: ignoreversion
Source: "C:\WPronto\license.txt"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\WPronto"; Filename: "{app}\WProntoGUI.exe"; IconFilename: "{app}\app.ico"
Name: "{group}\Uninstall WPronto"; Filename: "{uninstallexe}"
Name: "{autodesktop}\WPronto"; Filename: "{app}\WProntoGUI.exe"; IconFilename: "{app}\app.ico"; Tasks: desktopicon

[Run]
Filename: "{app}\WProntoGUI.exe"; Description: "WPronto — Local WP Environment"; Flags: postinstall nowait skipifsilent runascurrentuser

[UninstallDelete]
Type: filesandordirs; Name: "{app}\logs"
Type: filesandordirs; Name: "{app}\tmp"

[Code]
function IsDotNetInstalled: Boolean;
begin
  Result := RegKeyExists(HKLM, 'SOFTWARE\Microsoft\NET Framework Setup\NDP\v8.0');
  if not Result then
    Result := RegKeyExists(HKLM, 'SOFTWARE\WOW6432Node\Microsoft\NET Framework Setup\NDP\v8.0');
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  ErrorCode: Integer;
begin
  if CurStep = ssInstall then
  begin
    if not IsDotNetInstalled then
    begin
      if MsgBox('.NET 8.0 Runtime is required to run WPronto.' #13#13 'Do you want to download it now?', mbConfirmation, MB_YESNO) = IDYES then
      begin
        ShellExec('open', 'https://dotnet.microsoft.com/en-us/download/dotnet/8.0', '', '', SW_SHOWNORMAL, ewNoWait, ErrorCode);
      end;
    end;
  end;
end;