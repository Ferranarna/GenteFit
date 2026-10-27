[Setup]
AppName=GenteFit
AppVersion=1.0
DefaultDirName={autopf}\GenteFit
DefaultGroupName=GenteFit
OutputDir=..\salida
OutputBaseFilename=setup
PrivilegesRequired=admin
DisableDirPage=yes
ArchitecturesInstallIn64BitMode=x64compatible

[Languages]
Name: "es"; MessagesFile: "compiler:Languages\Spanish.isl"

[Files]
Source: "..\publicar\*"; DestDir: "{app}"; Excludes: "GenteFit.bat"; Flags: recursesubdirs
Source: "GenteFit.bat"; DestDir: "{app}"

[Icons]
Name: "{group}\GenteFit"; Filename: "{app}\GenteFit.bat"; WorkingDir: "{app}"
Name: "{commondesktop}\GenteFit"; Filename: "{app}\GenteFit.bat"; WorkingDir: "{app}"

[Run]
Filename: "{app}\GenteFit.bat"; WorkingDir: "{app}"; Description: "Abrir GenteFit"; Flags: postinstall nowait skipifsilent shellexec

[UninstallRun]
Filename: "{sys}\taskkill.exe"; Parameters: "/f /im GenteFit.Web.exe"; Flags: runhidden; RunOnceId: "CerrarGenteFit"

[UninstallDelete]
Type: filesandordirs; Name: "{app}"

[Code]
var
  PaginaServidor: TInputQueryWizardPage;

procedure InitializeWizard;
begin
  PaginaServidor := CreateInputQueryPage(wpSelectDir, 'Base de datos', 'Servidor de SQL Server',
    'Indica el servidor de SQL Server al que se conectara GenteFit.');
  PaginaServidor.Add('Servidor:', False);
  PaginaServidor.Values[0] := 'localhost';
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
var
  CodigoSalida: Integer;
begin
  Exec(ExpandConstant('{sys}\taskkill.exe'), '/f /im GenteFit.Web.exe', '', SW_HIDE, ewWaitUntilTerminated, CodigoSalida);
  Result := '';
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  Servidor, Cadena: string;
begin
  if CurStep = ssPostInstall then
  begin
    Servidor := PaginaServidor.Values[0];
    StringChangeEx(Servidor, '\', '\\', True);
    Cadena := '{"ConnectionStrings":{"GenteFit":"Server=' + Servidor +
      ';Database=GenteFit;Trusted_Connection=True;TrustServerCertificate=True;"},' +
      '"Urls":"http://localhost:5173",' +
      '"Logging":{"LogLevel":{"Default":"Information","Microsoft.AspNetCore":"Warning","Microsoft.EntityFrameworkCore":"Warning"}},' +
      '"AllowedHosts":"*"}';
    SaveStringToFile(ExpandConstant('{app}\appsettings.json'), Cadena, False);
  end;
end;