@echo off
cd /d "%~dp0"
echo Iniciando GenteFit, un momento...
start "" /min "%~dp0GenteFit.Web.exe"
set /a intentos=0
:espera
curl -s -o nul http://localhost:5173
if not errorlevel 1 goto listo
set /a intentos+=1
if %intentos% geq 90 goto listo
timeout /t 1 >nul
goto espera
:listo
start "" http://localhost:5173
