@echo off
title Migracion ElHato2K

echo.
echo ====================================================
echo   Migracion ElHato2K.mdb a SistemaGestion SQLite
echo ====================================================
echo.

:: AJUSTAR ESTAS VARIABLES
set MDB_PATH=C:\Users\Public\Documents\ElHato2K.mdb
set MDW_PATH=C:\Users\Public\Documents\Protegida.mdw
set DB_USER=Jacques Pascher
set DB_PASS=elcatire#mia

set SQLITE_PATH=%APPDATA%\SistemaGestion\sistema_gestion.db
set SCRIPT_PATH=%~dp0migrar_ElHato2K.py

echo MDB    : %MDB_PATH%
echo MDW    : %MDW_PATH%
echo SQLite : %SQLITE_PATH%
echo Script : %SCRIPT_PATH%
echo.

if not exist "%MDB_PATH%" (
    echo ERROR: No se encontro el archivo MDB: %MDB_PATH%
    pause & exit /b 1
)
if not exist "%MDW_PATH%" (
    echo ERROR: No se encontro el archivo MDW: %MDW_PATH%
    pause & exit /b 1
)
if not exist "%SQLITE_PATH%" (
    echo ERROR: No se encontro la BD: %SQLITE_PATH%
    pause & exit /b 1
)
if not exist "%SCRIPT_PATH%" (
    echo ERROR: No se encontro migrar_ElHato2K.py en la misma carpeta.
    pause & exit /b 1
)

python --version >nul 2>&1
if errorlevel 1 (
    echo ERROR: Python no esta en el PATH.
    pause & exit /b 1
)

python -c "import pyodbc" >nul 2>&1
if errorlevel 1 (
    echo Instalando pyodbc...
    pip install pyodbc
)

echo [1/3] Haciendo respaldo...
copy "%SQLITE_PATH%" "%APPDATA%\SistemaGestion\Respaldos\antes_migracion.db"
if errorlevel 1 (
    echo ERROR: No se pudo hacer el respaldo.
    pause & exit /b 1
)
echo       OK - Respaldo guardado.
echo.

echo [2/3] Ejecutando migracion...
echo.
python "%SCRIPT_PATH%" "%MDB_PATH%" "%MDW_PATH%" "%DB_USER%" "%DB_PASS%" "%SQLITE_PATH%"
if errorlevel 1 (
    echo.
    echo ERROR: La migracion fallo.
    pause & exit /b 1
)

echo.
echo [3/3] Completado. Abre FrmInventario y verifica los 28 productos.
echo.
pause