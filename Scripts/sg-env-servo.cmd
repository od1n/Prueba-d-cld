@echo off
rem Configuracion SistemaGestion API — SERVO (oficina, BD remota + digitalizador en OLIMPO)
rem SG_DB_PATH, SG_DIG_URL y SG_INTERNAL_TOKEN se leen de variables Machine scope.
rem Configurar via: setx /M SG_DB_PATH "\\192.168.68.132\SistemaGestion\sistema_gestion.db"
rem                 setx /M SG_DIG_URL "http://192.168.68.132:8080"
rem                 setx /M SG_INTERNAL_TOKEN "<token-seguro>"
rem NO hardcodear secrets en archivos trackeados por git.
