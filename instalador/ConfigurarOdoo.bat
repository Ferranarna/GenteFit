@echo off
setlocal
cd /d "%~dp0odoo"
set COMPOSE=docker compose -p gentefit

echo Configurando Odoo para GenteFit...
docker --version >nul 2>&1
if errorlevel 1 goto sin_docker
docker info >nul 2>&1
if errorlevel 1 goto docker_parado

echo.
echo [1/4] Arrancando la base de datos. La primera vez descarga cerca de 1 GB...
%COMPOSE% up -d db
if errorlevel 1 goto fallo
%COMPOSE% stop odoo >nul 2>&1

echo [2/4] Creando la base de datos e instalando el modulo GenteFit...
%COMPOSE% run --rm odoo odoo -d gentefit -i gentefit --load-language=es_ES --without-demo=all --stop-after-init --no-http
if errorlevel 1 goto fallo

echo [3/4] Poniendo idioma y zona horaria...
echo env.ref('base.user_admin').write({'lang': 'es_ES', 'tz': 'Europe/Madrid'}); env.cr.commit() | %COMPOSE% run --rm -T odoo odoo shell -d gentefit --no-http
if errorlevel 1 goto fallo

echo [4/4] Arrancando Odoo...
%COMPOSE% up -d
if errorlevel 1 goto fallo

echo.
echo Odoo esta listo en http://localhost:8069
echo Usuario: admin    Contrasena: admin
goto fin

:sin_docker
echo.
echo No se encuentra Docker. Instala Docker Desktop y despues ejecuta
echo "Configurar Odoo" desde el menu Inicio.
goto fin

:docker_parado
echo.
echo Docker Desktop no esta en marcha. Abrelo, espera a "Engine running"
echo y ejecuta "Configurar Odoo" desde el menu Inicio.
goto fin

:fallo
echo.
echo No se ha podido configurar Odoo. Revisa el mensaje de arriba.
echo Comprueba que hay conexion a Internet y que el puerto 8069 esta libre.

:fin
echo.
pause