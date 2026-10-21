@echo off
rem =========================================================
rem  Acompañar - levanta backend y frontend en esta ventana
rem  Uso: doble clic, o "iniciar.bat" desde la carpeta cuida
rem  Para apagar todo: Ctrl+C (y responder S si pregunta)
rem =========================================================

chcp 65001 >nul
title Acompañar - backend + frontend
cd /d "%~dp0"

echo.
echo  ==============================================
echo    Acompañar - iniciando el sistema completo
echo  ==============================================
echo.

rem --- Cierra un backend que haya quedado abierto (bloquea la compilacion)
taskkill /im Cuida.Api.exe /f >nul 2>&1

rem --- Verificaciones previas
if not exist "backend\Cuida.Api\appsettings.Local.json" (
    echo  [ERROR] Falta backend\Cuida.Api\appsettings.Local.json
    echo          Copia appsettings.Local.example.json con ese nombre
    echo          y pone una clave JWT de al menos 32 caracteres.
    echo.
    pause
    exit /b 1
)

if not exist "frontend\node_modules" (
    echo  Instalando dependencias del frontend, la primera vez tarda un poco...
    pushd frontend
    call npm install
    popd
    echo.
)

rem --- 1. Backend en segundo plano, en esta misma ventana
echo  [1/2] Iniciando backend en http://localhost:5282 ...
start "" /b cmd /c "cd /d backend\Cuida.Api && dotnet run --launch-profile http"

rem --- Espera hasta que la API responda (maximo 2 minutos)
set /a intentos=0
:esperar_backend
curl -s -o nul http://localhost:5282/swagger/index.html && goto backend_listo
set /a intentos+=1
if %intentos% geq 120 (
    echo.
    echo  [ERROR] El backend no respondio despues de 2 minutos.
    echo          Revisa los mensajes de arriba.
    goto apagar
)
ping -n 2 127.0.0.1 >nul
goto esperar_backend

:backend_listo
echo.
echo  [OK] Backend listo. Swagger: http://localhost:5282/swagger
echo.

rem --- 2. Frontend en primer plano (abre el navegador solo)
echo  [2/2] Iniciando frontend en http://localhost:5173 ...
echo        Para apagar todo apreta Ctrl+C.
echo.
cd frontend
if /i "%~1"=="sinabrir" (
    call npm run dev
) else (
    call npm run dev -- --open
)

:apagar
cd /d "%~dp0"
echo.
echo  Apagando el backend...
taskkill /im Cuida.Api.exe /f >nul 2>&1
echo  Listo.
