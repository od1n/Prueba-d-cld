@echo off
title SistemaGestion - Servicios SERVO
echo.
echo  ============================================
echo   SistemaGestion - Inicio de servicios SERVO
echo  ============================================
echo.

:: -- Esperar red (la BD esta en OLIMPO por SMB) -----------------
echo Esperando conexion de red con OLIMPO...
:esperar_red
ping -n 1 192.168.68.132 >nul 2>&1
if errorlevel 1 (
    timeout /t 5 /nobreak >nul
    goto esperar_red
)
echo  Red OK - OLIMPO accesible.

:: -- API (.NET, puertos 5050/5443) -------------------------------
:: Se inicia en SU PROPIA ventana (no bloquea), igual que en OLIMPO,
:: para que el actualizador pueda relanzar y volver sin quedarse pegado.
:: UNICA ubicacion canonica en SERVO: C:\SistemaGestion\publish\Api
echo Iniciando API...
start "SistemaGestion API SERVO" cmd /c "cd /d C:\SistemaGestion\publish\Api && call sg-env.cmd && dotnet SistemaGestion.Api.dll"
timeout /t 3 /nobreak >nul

echo.
echo  API iniciada en http://localhost:5050  (y https 5443)
echo  Esta ventana se puede cerrar.
timeout /t 4
