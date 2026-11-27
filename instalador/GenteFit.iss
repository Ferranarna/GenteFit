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

[Dirs]
Name: "{code:CarpetaIntercambio}"; Permissions: users-modify

[Files]
Source: "..\publicar\*"; DestDir: "{app}"; Excludes: "GenteFit.bat"; Flags: recursesubdirs
Source: "GenteFit.bat"; DestDir: "{app}"
Source: "ConfigurarOdoo.bat"; DestDir: "{app}"
Source: "..\odoo\docker-compose.yml"; DestDir: "{app}\odoo"
Source: "..\odoo\addons\*"; DestDir: "{app}\odoo\addons"; Excludes: "__pycache__,*.pyc"; Flags: recursesubdirs
Source: "..\odoo\xsd\*"; DestDir: "{app}\odoo\xsd"

[Icons]
Name: "{group}\GenteFit"; Filename: "{app}\GenteFit.bat"; WorkingDir: "{app}"
Name: "{group}\Configurar Odoo"; Filename: "{app}\ConfigurarOdoo.bat"; WorkingDir: "{app}"
Name: "{commondesktop}\GenteFit"; Filename: "{app}\GenteFit.bat"; WorkingDir: "{app}"

[Run]
Filename: "{app}\ConfigurarOdoo.bat"; WorkingDir: "{app}"; Description: "Configurar Odoo (necesita Docker Desktop abierto)"; Flags: postinstall waituntilterminated skipifsilent
Filename: "{app}\GenteFit.bat"; WorkingDir: "{app}"; Description: "Abrir GenteFit"; Flags: postinstall nowait skipifsilent shellexec

[UninstallRun]
Filename: "{sys}\taskkill.exe"; Parameters: "/f /im GenteFit.Web.exe"; Flags: runhidden; RunOnceId: "CerrarGenteFit"
Filename: "{cmd}"; Parameters: "/c docker compose -p gentefit -f ""{app}\odoo\docker-compose.yml"" down"; Flags: runhidden; RunOnceId: "PararOdoo"

[UninstallDelete]
Type: filesandordirs; Name: "{app}"

[Code]
var
  PaginaServidor, PaginaOdoo: TInputQueryWizardPage;

procedure InitializeWizard;
begin
  PaginaServidor := CreateInputQueryPage(wpSelectDir, 'Base de datos', 'Servidor de SQL Server',
    'Indica el servidor de SQL Server al que se conectara GenteFit.');
  PaginaServidor.Add('Servidor:', False);
  PaginaServidor.Values[0] := 'localhost';

  PaginaOdoo := CreateInputQueryPage(PaginaServidor.ID, 'Intercambio con Odoo', 'Carpeta y servidor',
    'Indica la carpeta donde se guardaran los ficheros XML y la direccion de Odoo.');
  PaginaOdoo.Add('Carpeta de intercambio:', False);
  PaginaOdoo.Add('Direccion de Odoo:', False);
  PaginaOdoo.Values[0] := ExpandConstant('{commonappdata}\GenteFit\lotes');
  PaginaOdoo.Values[1] := 'http://localhost:8069';
end;

function NextButtonClick(CurPageID: Integer): Boolean;
begin
  Result := True;
  if (CurPageID = PaginaOdoo.ID) and
     ((Trim(PaginaOdoo.Values[0]) = '') or (Trim(PaginaOdoo.Values[1]) = '')) then
  begin
    MsgBox('Rellena la carpeta de intercambio y la direccion de Odoo.', mbError, MB_OK);
    Result := False;
  end;
end;

function CarpetaIntercambio(Param: String): String;
begin
  Result := Trim(PaginaOdoo.Values[0]);
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
  Servidor, Carpeta, Url, Cadena: string;
begin
  if CurStep = ssPostInstall then
  begin
    Servidor := PaginaServidor.Values[0];
    StringChangeEx(Servidor, '\', '\\', True);
    Carpeta := CarpetaIntercambio('');
    StringChangeEx(Carpeta, '\', '\\', True);
    Url := Trim(PaginaOdoo.Values[1]);
    Cadena := '{"ConnectionStrings":{"GenteFit":"Server=' + Servidor +
      ';Database=GenteFit;Trusted_Connection=True;TrustServerCertificate=True;"},' +
      '"Odoo":{"Url":"' + Url + '","CarpetaLotes":"' + Carpeta + '"},' +
      '"Urls":"http://localhost:5173",' +
      '"Logging":{"LogLevel":{"Default":"Information","Microsoft.AspNetCore":"Warning","Microsoft.EntityFrameworkCore":"Warning"}},' +
      '"AllowedHosts":"*"}';
    SaveStringToFile(ExpandConstant('{app}\appsettings.json'), Cadena, False);
  end;
end;