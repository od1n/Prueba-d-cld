@echo off
cd /d "%~dp0"
echo.
echo  ============================================
echo   Digitalizador de Planillas - SistemaGestion
echo  ============================================
echo.

:: Verificar Python
where python >nul 2>&1
if errorlevel 1 (
    echo  ERROR: Python no encontrado. Instala Python 3.11+
    pause & exit /b 1
)

:: Instalar dependencias si faltan
python -c "import fastapi" 2>nul
if errorlevel 1 (
    echo  Instalando dependencias...
    pip install -r requirements.txt --quiet
)

:: Verificar Ollama
curl -s http://localhost:11434/api/tags >nul 2>&1
if errorlevel 1 (
    echo  AVISO: Ollama no parece estar corriendo en localhost:11434
    echo  Asegurate de tener Ollama abierto y el modelo llama3.2-vision descargado.
    echo.
    echo  Para descargar el modelo:  ollama pull llama3.2-vision
    echo.
    pause
)

echo  Abriendo navegador...
timeout /t 2 /nobreak >nul
start http://localhost:8080

echo  Servidor iniciado. Ctrl+C para detener.
echo.
python main.py
pause
