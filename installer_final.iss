; Inno Setup Script for WPronto - v2.0

[Setup]
AppId=WPronto
AppName=WPronto — Local WP Environment
AppVersion=2.0
AppPublisher=Andrii Ovcharov
AppPublisherURL=https://github.com/ovcharovcoder/wpronto
AppSupportURL=https://github.com/ovcharovcoder/wpronto
AppUpdatesURL=https://github.com/ovcharovcoder/wpronto
DefaultDirName={sd}\WPronto
DefaultGroupName=WPronto
DisableDirPage=no
AllowRootDirectory=yes
AllowNetworkDrive=yes
UsePreviousAppDir=yes

; Ліцензія - буде показана першою
LicenseFile=C:\WPronto\license.txt

; Вимікаємо вікно привітання
DisableWelcomePage=yes

OutputDir=C:\WPronto\Installer
OutputBaseFilename=WPronto_v2.0
SetupIconFile=C:\WPronto\src\WPLaunchGUI\app.ico
Compression=lzma
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayIcon={app}\WProntoGUI.exe

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
Filename: "{app}\WProntoGUI.exe"; Description: "Launch WPronto"; Flags: postinstall nowait skipifsilent runascurrentuser

[UninstallDelete]
Type: filesandordirs; Name: "{app}\logs"
Type: filesandordirs; Name: "{app}\tmp"
Type: dirifempty; Name: "{app}"

[Code]
function IsDotNetInstalled: Boolean;
var
  Key: string;
begin
  Key := 'SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedfx\Microsoft.NETCore.App';
  Result := RegKeyExists(HKLM, Key);
  if not Result then
    Result := RegKeyExists(HKLM, 'SOFTWARE\WOW6432Node\dotnet\Setup\InstalledVersions\x86\sharedfx\Microsoft.NETCore.App');
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  ErrorCode: Integer;
  Message: string;
begin
  if CurStep = ssInstall then
  begin
    if not IsDotNetInstalled then
    begin
      Message := '.NET 8.0 Runtime is required to run WPronto.' #13#13 
                 'This is needed for the application to work properly.' #13#13
                 'Do you want to download .NET 8.0 Runtime now?';
      
      if MsgBox(Message, mbConfirmation, MB_YESNO) = IDYES then
      begin
        ShellExec('open', 'https://dotnet.microsoft.com/en-us/download/dotnet/8.0', '', '', SW_SHOWNORMAL, ewNoWait, ErrorCode);
        MsgBox('Please install .NET 8.0 Runtime and then run WPronto Setup again.' #13#13 
               'The setup will now continue, but WPronto may not work correctly.',
               mbInformation, MB_OK);
      end
      else
      begin
        MsgBox('WPronto requires .NET 8.0 Runtime to run.' #13#13 
               'The application may not work correctly without it.' #13#13
               'You can download it later from: https://dotnet.microsoft.com/',
               mbInformation, MB_OK);
      end;
    end;
  end;
end;
