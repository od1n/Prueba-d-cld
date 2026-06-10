@echo off
title SistemaGestion - Servicios SERVO
echo.
echo  ============================================
echo   SistemaGestion - Inicio de servicios SERVO
echo  ============================================
echo.

:: ── Esperar red (la BD está en OLIMPO por SMB) ─────────────────
echo Esperando conexion de red con OLIMPO...
:esperar_red
ping -n 1 192.168.68.132 >nul 2>&1
if errorlevel 1 (
    timeout /t 5 /nobreak >nul
    goto esperar_red
)
echo  Red OK - OLIMPO accesible.

:: ── API (.NET, puertos 5050/5443) ────────────────────────────────
echo Iniciando API...
cd /d "C:\Users\naos\AppData\Local\SistemaGestion\publish\Api"
call sg-env.cmd
dotnet SistemaGestion.Api.dll
pause
