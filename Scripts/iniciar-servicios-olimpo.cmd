@echo off
title SistemaGestion - Servicios OLIMPO
echo.
echo  ============================================
echo   SistemaGestion - Inicio de servicios OLIMPO
echo  ============================================
echo.

:: ── 0. Generar token interno compartido ─────────────────────────
:: Ambos servicios deben usar el mismo token para comunicarse.
:: El token FIJO se toma de sg-env.cmd (SG_INTERNAL_TOKEN=<valor>).
:: defínelo en sg-env.cmd como SG_INTERNAL_TOKEN=<valor>.
:: Token FIJO desde sg-env.cmd (debe coincidir con n8n; NO se genera aleatorio)
call "C:\Users\PC\AppData\Local\SistemaGestion\publish\Api\sg-env.cmd"
echo  Token interno: %SG_INTERNAL_TOKEN:~0,8%...
echo.

:: ── 1. Digitalizador (Python, puerto 8080) ──────────────────────
echo [1/3] Iniciando Digitalizador...
start "Digitalizador" cmd /c "cd /d C:\SistemaGestion\digitalizador && python main.py"
timeout /t 3 /nobreak >nul

:: ── 2. API (.NET, puertos 5050/5443) ────────────────────────────
echo [2/3] Iniciando API...
start "SistemaGestion API" cmd /c "cd /d C:\Users\PC\AppData\Local\SistemaGestion\publish\Api && call sg-env.cmd && dotnet SistemaGestion.Api.dll"
timeout /t 2 /nobreak >nul

:: -- 3. Tunel Cloudflare (servicio Windows "cloudflared") --------------
:: Si el servicio esta detenido, lo arranca. Si ya corre, "sc start" solo
:: devuelve un aviso que se descarta. Requiere haber corrido una vez
:: deploy\configurar-tunel-olimpo.ps1 (da permiso al usuario para iniciarlo).
echo [3/3] Verificando tunel Cloudflare...
sc start cloudflared >nul 2>&1

echo.
echo  Servicios iniciados:
echo    - Digitalizador  http://0.0.0.0:8080
echo    - API             http://0.0.0.0:5050 / https://0.0.0.0:5443
echo    - Tunel           https://api-sg.hato-pascher.com (servicio cloudflared)
echo.
echo  Esta ventana se puede cerrar.
timeout /t 5
