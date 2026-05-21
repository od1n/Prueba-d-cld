@echo off
:: Lanzador — doble clic aquí para arrancar el sistema.
:: Llama al script PowerShell con bypass de política de ejecución.
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0iniciar_servidores.ps1"
