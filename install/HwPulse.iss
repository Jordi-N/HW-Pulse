; Instalador de HW Pulse (Inno Setup 6). Lo compila el workflow de releases:
;
;   dotnet publish src\HwPulse.App\HwPulse.App.csproj -c Release -o publish
;   ISCC /DVersion=1.2.3 install\HwPulse.iss
;
; Espera PawnIO_setup.exe en esta carpeta: el driver va dentro del instalador porque Windows Server
; no trae winget. El resultado sale en dist\.

#ifndef Version
  #define Version "0.0.0"
#endif
#define PublishDir "..\publish"
#define TaskName "HW Pulse"

[Setup]
AppId={{E692A479-B614-4B3A-974D-B2FF62D43317}
AppName=HW Pulse
AppVersion={#Version}
AppPublisher=Jordi-N
AppCopyright=Copyright (c) 2026 Jordi-N. MIT License.
VersionInfoProductName=HW Pulse
VersionInfoVersion={#Version}
LicenseFile=..\LICENSE
AppPublisherURL=https://github.com/Jordi-N/HW-Pulse
DefaultDirName={autopf}\HW Pulse
DisableProgramGroupPage=yes
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
OutputDir=..\dist
OutputBaseFilename=HwPulse-Setup-{#Version}
SetupIconFile=..\src\HwPulse.App\Assets\HwPulse.ico
UninstallDisplayIcon={app}\HwPulse.exe
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern

[Languages]
Name: "es"; MessagesFile: "compiler:Languages\Spanish.isl"

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: recursesubdirs ignoreversion
Source: "PawnIO_setup.exe"; DestDir: "{tmp}"; Flags: deleteafterinstall; Check: not PawnIoInstalled

; El acceso lanza la tarea y no el .exe: la tarea ya tiene los privilegios, así que no salta UAC.
; Si el panel ya está en marcha, la nueva ejecución solo lo muestra.
[Icons]
Name: "{autoprograms}\HW Pulse"; Filename: "{sys}\schtasks.exe"; Parameters: "/Run /TN ""{#TaskName}"""; IconFilename: "{app}\HwPulse.exe"; Flags: runminimized

[Run]
Filename: "{tmp}\PawnIO_setup.exe"; Parameters: "-install -silent"; StatusMsg: "Instalando el driver PawnIO..."; Flags: waituntilterminated; Check: not PawnIoInstalled
Filename: "schtasks.exe"; Parameters: "/Create /TN ""{#TaskName}"" /XML ""{tmp}\task.xml"" /F"; BeforeInstall: WriteTaskXml; StatusMsg: "Registrando el arranque al iniciar sesión..."; Flags: runhidden waituntilterminated
Filename: "schtasks.exe"; Parameters: "/Run /TN ""{#TaskName}"""; Flags: runhidden waituntilterminated

[UninstallRun]
Filename: "schtasks.exe"; Parameters: "/End /TN ""{#TaskName}"""; Flags: runhidden; RunOnceId: "EndTask"
Filename: "taskkill.exe"; Parameters: "/F /IM HwPulse.exe"; Flags: runhidden; RunOnceId: "StopApp"
Filename: "schtasks.exe"; Parameters: "/Delete /TN ""{#TaskName}"" /F"; Flags: runhidden; RunOnceId: "DeleteTask"

[Code]
// La clave que deja el propio instalador de PawnIO: si está, no se vuelve a instalar.
function PawnIoInstalled: Boolean;
begin
  Result := RegKeyExists(HKLM64, 'SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\PawnIO');
end;

// El panel en marcha bloquea sus ficheros: se para antes de copiar la versión nueva.
function PrepareToInstall(var NeedsRestart: Boolean): String;
var
  ResultCode: Integer;
begin
  Exec('schtasks.exe', '/End /TN "{#TaskName}"', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  Exec('taskkill.exe', '/F /IM HwPulse.exe', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  Result := '';
end;

function XmlEscape(const Value: String): String;
begin
  Result := Value;
  StringChangeEx(Result, '&', '&amp;', True);
  StringChangeEx(Result, '<', '&lt;', True);
  StringChangeEx(Result, '>', '&gt;', True);
end;

// La tarea de inicio, como XML para schtasks: es la única forma de quitarle el límite de 72 horas
// que Windows pone por defecto. Arranca al iniciar sesión quien instala, con privilegios más altos,
// así el panel abre sin el aviso de UAC.
procedure WriteTaskXml;
var
  User, App: String;
  Lines: TArrayOfString;
begin
  User := XmlEscape(GetEnv('USERDOMAIN') + '\' + GetUserNameString);
  App := XmlEscape(ExpandConstant('{app}'));
  SetArrayLength(Lines, 1);
  Lines[0] :=
    '<?xml version="1.0" encoding="UTF-8"?>' +
    '<Task version="1.2" xmlns="http://schemas.microsoft.com/windows/2004/02/mit/task">' +
    '<Triggers><LogonTrigger><Enabled>true</Enabled><UserId>' + User + '</UserId></LogonTrigger></Triggers>' +
    '<Principals><Principal id="Author"><UserId>' + User + '</UserId>' +
    '<LogonType>InteractiveToken</LogonType><RunLevel>HighestAvailable</RunLevel></Principal></Principals>' +
    '<Settings><MultipleInstancesPolicy>Parallel</MultipleInstancesPolicy>' +
    '<DisallowStartIfOnBatteries>false</DisallowStartIfOnBatteries>' +
    '<StopIfGoingOnBatteries>false</StopIfGoingOnBatteries>' +
    '<ExecutionTimeLimit>PT0S</ExecutionTimeLimit><Enabled>true</Enabled></Settings>' +
    '<Actions Context="Author"><Exec><Command>"' + App + '\HwPulse.exe"</Command>' +
    '<WorkingDirectory>' + App + '</WorkingDirectory></Exec></Actions>' +
    '</Task>';
  SaveStringsToUTF8File(ExpandConstant('{tmp}\task.xml'), Lines, False);
end;
