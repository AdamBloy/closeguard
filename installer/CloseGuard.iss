; ============================================================
; Close Guard installer script (Inno Setup 6)
; Paths are relative to THIS file's folder (the "installer" folder).
; ============================================================

[Setup]
; Unique ID. Keep the double opening brace "{{", it's how Inno escapes a single brace.
AppId={{94de1e1f-b9bf-45e8-bda0-e14406026db3}
AppName=Close Guard
AppVersion=1.0.1
AppPublisher=AJB Media
; Optional: a website shown in Add/Remove Programs
; AppPublisherURL=https://github.com/AdamBloy

; {autopf} = Program Files. Needs admin to modify, which is safer (see the security notes earlier).
DefaultDirName={autopf}\Close Guard
DefaultGroupName=Close Guard

; Install for 64-bit Windows only (we published win-x64)
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

; Installing to Program Files requires admin, so Windows shows a UAC prompt.
PrivilegesRequired=admin

; Where the finished installer goes, and what it's called
OutputDir=output
OutputBaseFilename=CloseGuard-Setup

; Compression: slow to build, but a much smaller installer
Compression=lzma2
SolidCompression=yes

; ----- Custom look -----
; Icon of the Setup.exe file and installer window
SetupIconFile=..\app.ico
; Icon shown in Windows Settings > Apps
UninstallDisplayIcon={app}\CloseGuard.exe
; Modern flat wizard style
WizardStyle=modern
; Your custom images (delete these two lines to use Inno's defaults)
WizardImageFile=wizard-large.bmp
WizardSmallImageFile=wizard-small.bmp

; If Close Guard is running during install/uninstall, ask the user to close it.
; This must match the mutex name in Program.cs.
AppMutex=CloseGuard.SingleInstance

; Optional extra pages. Create these text files next to this script and uncomment:
; LicenseFile=..\LICENSE.txt       ; page where the user must accept a licence
; InfoBeforeFile=readme-install.txt ; a "please read" page, e.g. what the app does and doesn't do

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

; Custom wording for the welcome page. [name/ver] is replaced automatically.
[Messages]
WelcomeLabel1=Welcome to Close Guard
WelcomeLabel2=This will install [name/ver] on your computer.%n%nClose Guard asks you to confirm before a Chrome window is closed with the mouse. It listens to mouse clicks only, never records keystrokes, and uses no internet connection.%n%nClick Next to continue.

; Checkboxes on the "Select Additional Tasks" page
[Tasks]
Name: "startup"; Description: "Start Close Guard automatically when I sign in to Windows"; GroupDescription: "Options:"

[Files]
; Copy everything from the publish folder into the install folder.
; Excludes: skip debug symbol files the user doesn't need.
Source: "..\publish\*"; DestDir: "{app}"; Excludes: "*.pdb"; Flags: ignoreversion recursesubdirs createallsubdirs

; Bundled .NET runtime installer. It's extracted to a temp folder, only if needed,
; and deleted automatically after install.
Source: "redist\windowsdesktop-runtime-win-x64.exe"; DestDir: "{tmp}"; Flags: deleteafterinstall; Check: NeedsDotNet

[Icons]
; Start Menu shortcut
Name: "{group}\Close Guard"; Filename: "{app}\CloseGuard.exe"

[Registry]
; Only written if the user ticked the "startup" checkbox.
; The value name "CloseGuard" matches Startup.cs, so the tray toggle sees and controls it.
; uninsdeletevalue = remove it again on uninstall.
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "CloseGuard"; ValueData: """{app}\CloseGuard.exe"""; Tasks: startup; Flags: uninsdeletevalue

[Run]
; 1) Install the .NET runtime silently if it's missing.
;    /install /quiet /norestart = no windows, no reboot prompt.
Filename: "{tmp}\windowsdesktop-runtime-win-x64.exe"; Parameters: "/install /quiet /norestart"; StatusMsg: "Installing .NET 8 Desktop Runtime (this can take a minute)..."; Flags: waituntilterminated; Check: NeedsDotNet

; 2) Offer to launch Close Guard when setup finishes.
Filename: "{app}\CloseGuard.exe"; Description: "Start Close Guard now"; Flags: nowait postinstall skipifsilent

[Code]

// Returns True if a .NET 8 Desktop Runtime is already installed.
// Windows records every installed runtime version as a value name under this registry key,
// e.g. "8.0.11". We look for any value starting with "8.".
function IsDotNet8DesktopInstalled(): Boolean;
var
  Versions: TArrayOfString;  // the list of value names found under the key
  I: Integer;
begin
  Result := False;

  if RegGetValueNames(HKLM,
       'SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedfx\Microsoft.WindowsDesktop.App',
       Versions) then
  begin
    for I := 0 to GetArrayLength(Versions) - 1 do
    begin
      if Pos('8.', Versions[I]) = 1 then   // Pos = 1 means the text starts with "8."
      begin
        Result := True;
        Exit;                               // found one, no need to keep looking
      end;
    end;
  end;
end;

// Used by the Check: parameters above. True means "install the runtime".
function NeedsDotNet(): Boolean;
begin
  Result := not IsDotNet8DesktopInstalled();
end;