@echo off
title SistemaGestion - Servicios OLIMPO
echo.
echo  ============================================
echo   SistemaGestion - Inicio de servicios OLIMPO
echo  ============================================
echo.

:: ── 0. Generar token interno compartido ─────────────────────────
:: Ambos servicios deben usar el mismo token para comunicarse.
:: Se genera un valor aleatorio cada inicio; si prefieres uno fijo,
:: defínelo en sg-env.cmd como SG_INTERNAL_TOKEN=<valor>.
if not defined SG_INTERNAL_TOKEN (
    for /f "delims=" %%T in ('powershell -NoProfile -Command "[System.Guid]::NewGuid().ToString('N') + [System.Guid]::NewGuid().ToString('N')"') do set SG_INTERNAL_TOKEN=%%T
)
echo  Token interno: %SG_INTERNAL_TOKEN:~0,8%...
echo.

:: ── 1. Digitalizador (Python, puerto 8080) ──────────────────────
echo [1/2] Iniciando Digitalizador...
start "Digitalizador" cmd /c "cd /d C:\SistemaGestion\digitalizador && set SG_INTERNAL_TOKEN=%SG_INTERNAL_TOKEN% && python main.py"
timeout /t 3 /nobreak >nul

:: ── 2. API (.NET, puertos 5050/5443) ────────────────────────────
echo [2/2] Iniciando API...
start "SistemaGestion API" cmd /c "cd /d C:\Users\PC\AppData\Local\SistemaGestion\publish\Api && call sg-env.cmd && set SG_INTERNAL_TOKEN=%SG_INTERNAL_TOKEN% && dotnet SistemaGestion.Api.dll"
timeout /t 2 /nobreak >nul

echo.
echo  Servicios iniciados:
echo    - Digitalizador  http://0.0.0.0:8080
echo    - API             http://0.0.0.0:5050 / https://0.0.0.0:5443
echo.
echo  Esta ventana se puede cerrar.
timeout /t 5
