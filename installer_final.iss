; Inno Setup Script for WPronto - v5.0

[Setup]
AppId=WPronto
AppName=WPronto — Local WP Environment
AppVersion=5.0
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
OutputBaseFilename=WPronto_v5.0
SetupIconFile=C:\WPronto\src\WPLaunchGUI\app.ico
Compression=lzma
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayIcon={app}\WProntoGUI.exe

[Languages]
Name: "en"; MessagesFile: "compiler:Default.isl"
Name: "uk"; MessagesFile: "compiler:Languages\Ukrainian.isl"

[CustomMessages]
; Англійська
en.SelectLanguage=Select Language
en.SelectLanguageDesc=Please select the language for installation
en.CreateDesktopIcon=Create a desktop shortcut
en.AdditionalIcons=Additional icons:
en.LaunchWPronto=Launch WPronto
en.UninstallProgram=Uninstall WPronto
en.DotNetRequired=.NET 8.0 Runtime is required to run WPronto.
en.DotNetDesc=This is needed for the application to work properly.
en.DotNetDownload=Do you want to download .NET 8.0 Runtime now?
en.DotNetInstall=Please install .NET 8.0 Runtime and then run WPronto Setup again.
en.DotNetContinue=The setup will now continue, but WPronto may not work correctly.
en.DotNetLater=WPronto requires .NET 8.0 Runtime to run.
en.DotNetLaterDesc=The application may not work correctly without it.
en.DotNetLaterLink=You can download it later from: https://dotnet.microsoft.com/

; Українська
uk.SelectLanguage=Виберіть мову
uk.SelectLanguageDesc=Будь ласка, оберіть мову для встановлення
uk.CreateDesktopIcon=Створити ярлик на робочому столі
uk.AdditionalIcons=Додаткові іконки:
uk.LaunchWPronto=Запустити WPronto
uk.UninstallProgram=Видалити WPronto
uk.DotNetRequired=Для роботи WPronto потрібен .NET 8.0 Runtime.
uk.DotNetDesc=Це необхідно для коректної роботи програми.
uk.DotNetDownload=Бажаєте завантажити .NET 8.0 Runtime зараз?
uk.DotNetInstall=Будь ласка, встановіть .NET 8.0 Runtime та запустіть WPronto Setup знову.
uk.DotNetContinue=Встановлення продовжиться, але WPronto може працювати некоректно.
uk.DotNetLater=Для роботи WPronto потрібен .NET 8.0 Runtime.
uk.DotNetLaterDesc=Програма може працювати некоректно без нього.
uk.DotNetLaterLink=Ви можете завантажити його пізніше: https://dotnet.microsoft.com/

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"

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
Source: "C:\WPronto\tools\*"; DestDir: "{app}\tools"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "C:\WPronto\help.txt"; DestDir: "{app}"; Flags: ignoreversion
Source: "C:\WPronto\shortcut.png"; DestDir: "{app}"; Flags: ignoreversion
Source: "C:\WPronto\license.txt"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\WPronto"; Filename: "{app}\WProntoGUI.exe"; IconFilename: "{app}\app.ico"
Name: "{group}\{cm:UninstallProgram}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\WPronto"; Filename: "{app}\WProntoGUI.exe"; IconFilename: "{app}\app.ico"; Tasks: desktopicon

[Run]
Filename: "{app}\WProntoGUI.exe"; Description: "{cm:LaunchWPronto}"; Flags: postinstall nowait skipifsilent runascurrentuser

[UninstallDelete]
Type: filesandordirs; Name: "{app}\logs"
Type: filesandordirs; Name: "{app}\tmp"
Type: filesandordirs; Name: "{app}\tools"
Type: dirifempty; Name: "{app}"

[Code]
var
  ErrorCode: Integer;

// Перевірка наявності .NET
function IsDotNetInstalled: Boolean;
var
  Key: string;
begin
  Key := 'SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedfx\Microsoft.NETCore.App';
  Result := RegKeyExists(HKLM, Key);
  if not Result then
    Result := RegKeyExists(HKLM, 'SOFTWARE\WOW6432Node\dotnet\Setup\InstalledVersions\x86\sharedfx\Microsoft.NETCore.App');
end;

// Перевірка наявності .NET під час встановлення
procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssInstall then
  begin
    if not IsDotNetInstalled then
    begin
      if MsgBox(
        CustomMessage('DotNetRequired') + #13#13 + 
        CustomMessage('DotNetDesc') + #13#13 +
        CustomMessage('DotNetDownload'),
        mbConfirmation, MB_YESNO) = IDYES then
      begin
        ShellExec('open', 'https://dotnet.microsoft.com/en-us/download/dotnet/8.0', '', '', SW_SHOWNORMAL, ewNoWait, ErrorCode);
        MsgBox(
          CustomMessage('DotNetInstall') + #13#13 +
          CustomMessage('DotNetContinue'),
          mbInformation, MB_OK);
      end
      else
      begin
        MsgBox(
          CustomMessage('DotNetLater') + #13#13 +
          CustomMessage('DotNetLaterDesc') + #13#13 +
          CustomMessage('DotNetLaterLink'),
          mbInformation, MB_OK);
      end;
    end;
  end;
end;