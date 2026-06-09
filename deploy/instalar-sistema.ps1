# ============================================================
#  instalar-sistema.ps1
#  Script de instalación y configuración — SistemaGestion
#  Ejecutar como Administrador en Windows 10/11 x64
#
#  Uso:
#    .\instalar-sistema.ps1 -Rol Admin
#    .\instalar-sistema.ps1 -Rol Oficina -AdminIP 192.168.1.10
# ============================================================
param(
    [ValidateSet("Admin","Oficina")]
    [string]$Rol = "Admin",

    # IP o nombre del equipo Admin (solo necesario en rol Oficina)
    [string]$AdminIP = "",

    # Ruta de red donde está la BD (solo rol Oficina si la BD es compartida)
    # Ejemplo: "\\ADMIN-PC\SistemaGestion\sistema_gestion.db"
    [string]$RutaBD = "",

    # Puerto del portal API (default 5050)
    [int]$PortHttp = 5050,

    # Si se indica, se instala como servicio de Windows
    [switch]$ComoServicio
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# ── Colores ───────────────────────────────────────────────────────────────
function Ok($msg)   { Write-Host "  [OK] $msg" -ForegroundColor Green }
function Info($msg) { Write-Host "  [..] $msg" -ForegroundColor Cyan }
function Warn($msg) { Write-Host "  [!!] $msg" -ForegroundColor Yellow }
function Fail($msg) { Write-Host "  [ERROR] $msg" -ForegroundColor Red; exit 1 }

Write-Host ""
Write-Host "  ============================================" -ForegroundColor Cyan
Write-Host "   SistemaGestion — Instalador ($Rol)" -ForegroundColor Cyan
Write-Host "  ============================================" -ForegroundColor Cyan
Write-Host ""

# ── 1. Verificar .NET 8 ───────────────────────────────────────────────────
Info "Verificando .NET 8..."
try {
    $dotnetVer = & dotnet --version 2>$null
    if ($dotnetVer -match "^8\." -or $dotnetVer -match "^9\.") {
        Ok ".NET $dotnetVer encontrado."
    } else {
        Warn ".NET $dotnetVer encontrado. Se recomienda .NET 8+."
    }
} catch {
    Warn "dotnet no encontrado. Descargando instalador de .NET 8..."
    $dotnetUrl = "https://dot.net/v1/dotnet-install.ps1"
    $dotnetScript = "$env:TEMP\dotnet-install.ps1"
    Invoke-WebRequest -Uri $dotnetUrl -OutFile $dotnetScript -UseBasicParsing
    & $dotnetScript -Channel 8.0 -Runtime dotnet
    $env:PATH = "$env:LOCALAPPDATA\Microsoft\dotnet;$env:PATH"
    Ok ".NET 8 instalado."
}

# ── 2. Rutas base ────────────────────────────────────────────────────────
$scriptDir   = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoDir     = Split-Path -Parent $scriptDir
$apiDir      = Join-Path $repoDir "SistemaGestion.Api"
$winFormsDir = $repoDir   # .csproj del WinForms está en la raíz
$publishDir  = Join-Path $env:LOCALAPPDATA "SistemaGestion\publish"
$apiPublish  = Join-Path $publishDir "Api"
$appPublish  = Join-Path $publishDir "App"

New-Item -ItemType Directory -Force -Path $publishDir | Out-Null
New-Item -ItemType Directory -Force -Path $apiPublish  | Out-Null

# ── 3. Publicar API ──────────────────────────────────────────────────────
Info "Compilando y publicando API..."
Push-Location $apiDir
try {
    & dotnet publish -c Release -r win-x64 --self-contained false `
        -o $apiPublish --nologo -v minimal
    if ($LASTEXITCODE -ne 0) { Fail "Falló la compilación de la API." }
    Ok "API publicada en $apiPublish"
} finally { Pop-Location }

# ── 4. Publicar WinForms (solo rol Admin) ────────────────────────────────
if ($Rol -eq "Admin") {
    Info "Compilando WinForms..."
    Push-Location $repoDir
    try {
        # Detectar el .csproj del WinForms
        $csproj = Get-ChildItem -Filter "*.csproj" | Where-Object { $_.Name -notmatch "Api|Test" } | Select-Object -First 1
        if (-not $csproj) { Warn "No se encontró .csproj WinForms, omitiendo." }
        else {
            & dotnet publish $csproj.Name -c Release -r win-x64 --self-contained false `
                -o $appPublish --nologo -v minimal
            if ($LASTEXITCODE -ne 0) { Warn "WinForms no compiló, continúa solo con la API." }
            else { Ok "WinForms publicado en $appPublish" }
        }
    } finally { Pop-Location }
}

# ── 5. Carpeta de datos y BD ─────────────────────────────────────────────
$datosDir = Join-Path $env:APPDATA "SistemaGestion"
New-Item -ItemType Directory -Force -Path $datosDir | Out-Null

if ($Rol -eq "Admin") {
    $bdPath = Join-Path $datosDir "sistema_gestion.db"
    Info "BD local: $bdPath"

    # Compartir carpeta de datos en la red (para que el equipo Oficina acceda)
    $shareExiste = Get-SmbShare -Name "SistemaGestion" -ErrorAction SilentlyContinue
    if (-not $shareExiste) {
        try {
            New-SmbShare -Name "SistemaGestion" -Path $datosDir `
                -ReadAccess "Everyone" -FullAccess "$env:USERDOMAIN\$env:USERNAME" | Out-Null
            Ok "Carpeta compartida: \\$env:COMPUTERNAME\SistemaGestion"
        } catch {
            Warn "No se pudo crear la carpeta compartida: $_"
            Warn "Hazlo manualmente: clic derecho en $datosDir > Propiedades > Compartir"
        }
    } else {
        Ok "Carpeta ya compartida: \\$env:COMPUTERNAME\SistemaGestion"
    }
}

# ── 6. Archivo de configuración de entorno ───────────────────────────────
$envFile = Join-Path $apiPublish "sg-env.cmd"

if ($Rol -eq "Admin") {
    $bdPath = Join-Path $datosDir "sistema_gestion.db"
    $envContent = "@echo off
rem ── Configuración SistemaGestion API ──────────────────────
rem Rol: ADMIN — BD local
set SG_DB_PATH=$bdPath
set ASPNETCORE_URLS=http://0.0.0.0:$PortHttp
"
    Set-Content -Path $envFile -Value $envContent -Encoding ASCII
    Ok "Configuración guardada en $envFile"
} else {
    # Oficina: apuntar a la BD del Admin
    if (-not $RutaBD -and $AdminIP) {
        $RutaBD = "\\$AdminIP\SistemaGestion\sistema_gestion.db"
    }
    if (-not $RutaBD) {
        Warn "No se especificó RutaBD ni AdminIP. Deberás editar $envFile manualmente."
        $RutaBD = "\\ADMIN-PC\SistemaGestion\sistema_gestion.db"
    }
    $envContent = "@echo off
rem ── Configuración SistemaGestion API ──────────────────────
rem Rol: OFICINA — BD remota (carpeta de red del Admin)
set SG_DB_PATH=$RutaBD
set ASPNETCORE_URLS=http://localhost:$PortHttp
"
    Set-Content -Path $envFile -Value $envContent -Encoding ASCII
    Ok "Configuración guardada en $envFile"
    Info "La API usará BD remota: $RutaBD"
    Warn "Asegúrate de que \\$AdminIP está accesible y la carpeta SistemaGestion está compartida."
}

# ── 7. Crear script de inicio ────────────────────────────────────────────
$iniciarScript = Join-Path $apiPublish "iniciar-api.cmd"
$exeName = "SistemaGestion.Api.exe"
Set-Content -Path $iniciarScript -Value "@echo off
title SistemaGestion API
cd /d `"%~dp0`"
call sg-env.cmd
echo Iniciando portal en http://localhost:$PortHttp ...
$exeName
pause
" -Encoding ASCII
Ok "Script de inicio: $iniciarScript"

# ── 8. Acceso directo en el escritorio ────────────────────────────────────
$shell     = New-Object -ComObject WScript.Shell
$escritorio = [System.Environment]::GetFolderPath("Desktop")

$lnkApi = $shell.CreateShortcut("$escritorio\SistemaGestion Portal.lnk")
$lnkApi.TargetPath       = $iniciarScript
$lnkApi.WorkingDirectory = $apiPublish
$lnkApi.Description      = "Iniciar portal SistemaGestion"
$lnkApi.Save()
Ok "Acceso directo creado: SistemaGestion Portal"

if ($Rol -eq "Admin" -and (Test-Path $appPublish)) {
    $appExe = Get-ChildItem $appPublish -Filter "*.exe" | Where-Object { $_.Name -notmatch "Api" } | Select-Object -First 1
    if ($appExe) {
        $lnkApp = $shell.CreateShortcut("$escritorio\SistemaGestion App.lnk")
        $lnkApp.TargetPath       = $appExe.FullName
        $lnkApp.WorkingDirectory = $appPublish
        $lnkApp.Description      = "SistemaGestion (escritorio)"
        $lnkApp.Save()
        Ok "Acceso directo creado: SistemaGestion App"
    }
}

# ── 9. Registrar como servicio Windows (opcional) ─────────────────────────
if ($ComoServicio) {
    Info "Registrando como servicio de Windows..."
    $svcName = "SistemaGestionApi"
    $svcExe  = Join-Path $apiPublish $exeName

    $existente = Get-Service -Name $svcName -ErrorAction SilentlyContinue
    if ($existente) {
        Stop-Service $svcName -Force -ErrorAction SilentlyContinue
        & sc.exe delete $svcName | Out-Null
        Start-Sleep 1
    }

    # Leer variables de entorno del archivo sg-env.cmd
    $sgDbPath = (Select-String -Path $envFile -Pattern "SG_DB_PATH=(.+)").Matches[0].Groups[1].Value.Trim()
    $sgUrls   = (Select-String -Path $envFile -Pattern "ASPNETCORE_URLS=(.+)").Matches[0].Groups[1].Value.Trim()

    & sc.exe create $svcName `
        binPath= "`"$svcExe`"" `
        DisplayName= "SistemaGestion API" `
        start= auto | Out-Null

    # Variables de entorno para el servicio
    & sc.exe config $svcName obj= LocalSystem | Out-Null
    $regPath = "HKLM:\SYSTEM\CurrentControlSet\Services\$svcName\Environment"
    New-Item -Path $regPath -Force | Out-Null
    Set-ItemProperty -Path $regPath -Name "(Default)" -Value @(
        "SG_DB_PATH=$sgDbPath",
        "ASPNETCORE_URLS=$sgUrls"
    )

    Start-Service $svcName
    Ok "Servicio '$svcName' registrado e iniciado."
    Info "Para detenerlo: Stop-Service $svcName"
    Info "Para removerlo: sc.exe delete $svcName"
}

# ── 10. Resumen final ────────────────────────────────────────────────────
Write-Host ""
Write-Host "  ============================================" -ForegroundColor Green
Write-Host "   INSTALACION COMPLETADA" -ForegroundColor Green
Write-Host "  ============================================" -ForegroundColor Green
Write-Host ""
if ($Rol -eq "Admin") {
    Write-Host "  Portal API:   http://localhost:$PortHttp"       -ForegroundColor Cyan
    Write-Host "               http://$env:COMPUTERNAME:$PortHttp (desde red LAN)" -ForegroundColor Cyan
    Write-Host "  BD:           $datosDir"  -ForegroundColor White
    Write-Host "  Red:          \\$env:COMPUTERNAME\SistemaGestion" -ForegroundColor White
} else {
    Write-Host "  Portal API:   http://localhost:$PortHttp"       -ForegroundColor Cyan
    Write-Host "  BD remota:    $RutaBD" -ForegroundColor White
}
Write-Host ""
Write-Host "  Para iniciar el portal doble-clic en:" -ForegroundColor Yellow
Write-Host "  '$escritorio\SistemaGestion Portal.lnk'" -ForegroundColor Yellow
Write-Host ""
