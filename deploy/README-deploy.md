# Deploy — SistemaGestion

## Arquitectura

```
[PC Admin]  ─── WinForms + API LAN ─── SQLite BD local
                       │
                  Red LAN (WiFi/Ethernet)
                       │
[PC Oficina] ─── Portal web (navegador) → http://IP-ADMIN:5050
                 (WinForms opcional con BD compartida en red)
```

## Requisitos (ambas PCs)

- Windows 10/11 x64
- .NET 8 Runtime: https://dotnet.microsoft.com/download/dotnet/8
- Conectadas a la misma red LAN

---

## Opción A — Instalación rápida con script (recomendado)

### PC Admin

Abre PowerShell **como Administrador** y ejecuta:

```powershell
Set-ExecutionPolicy -Scope Process Bypass
cd "C:\ruta\del\repositorio\deploy"
.\instalar-sistema.ps1 -Rol Admin
```

Esto compila y publica la API, crea los accesos directos en el escritorio, y comparte automáticamente la carpeta de datos en la red.

---

### PC Oficina

```powershell
Set-ExecutionPolicy -Scope Process Bypass
cd "C:\ruta\del\repositorio\deploy"
.\instalar-sistema.ps1 -Rol Oficina -AdminIP 192.168.1.10
```

Reemplaza `192.168.1.10` con la IP del PC Admin (la puedes ver ejecutando `ipconfig` en el Admin).

---

## Opción B — Configuración manual

### 1. Compilar la API (en el Admin)

```powershell
cd "C:\ruta\repo\SistemaGestion.Api"
dotnet publish -c Release -r win-x64 --self-contained false -o "C:\SgPublish\Api"
```

### 2. Configurar variables de entorno

Crea `C:\SgPublish\Api\sg-env.cmd`:

```cmd
@echo off
rem -- PC Admin --
set SG_DB_PATH=C:\Users\TuUsuario\AppData\Roaming\SistemaGestion\sistema_gestion.db
set ASPNETCORE_URLS=http://0.0.0.0:5050
```

Para la PC Oficina (con BD compartida):

```cmd
@echo off
rem -- PC Oficina --
set SG_DB_PATH=\\NOMBRE-PC-ADMIN\SistemaGestion\sistema_gestion.db
set ASPNETCORE_URLS=http://localhost:5050
```

### 3. Compartir la carpeta de BD (en el Admin)

1. Abre el Explorador de Windows
2. Ve a `%AppData%\SistemaGestion`
3. Clic derecho → Propiedades → Pestaña Compartir
4. Compartir con nombre `SistemaGestion`
5. Dar permisos de **Lectura/Escritura** al usuario del PC Oficina

### 4. Iniciar la API

```cmd
cd C:\SgPublish\Api
call sg-env.cmd
SistemaGestion.Api.exe
```

---

## Firewall

Si el PC Oficina no puede conectar al portal del Admin, permite el puerto en el firewall del Admin:

```powershell
# Ejecutar como Administrador en el PC Admin
New-NetFirewallRule -DisplayName "SistemaGestion API" `
    -Direction Inbound -Protocol TCP -LocalPort 5050 -Action Allow
```

---

## Instalar como servicio de Windows (opcional, para Admin)

Para que la API inicie automáticamente con Windows:

```powershell
.\instalar-sistema.ps1 -Rol Admin -ComoServicio
```

Para detener/eliminar el servicio:

```powershell
Stop-Service SistemaGestionApi
sc.exe delete SistemaGestionApi
```

---

## Acceder al portal desde el PC Oficina

Abre el navegador y ve a:
```
http://192.168.1.10:5050
```
(Reemplaza la IP con la del PC Admin)

---

## Solución de problemas

| Problema | Solución |
|---|---|
| "BD no encontrada" | Verificar que `SG_DB_PATH` apunta a la ruta correcta |
| El Oficina no ve el portal | Verificar firewall en el Admin (puerto 5050) |
| "Access denied" en la BD | Verificar permisos de la carpeta compartida |
| Error al compilar | Asegurarse de tener .NET 8 SDK instalado |
| La API no arranca como servicio | Ejecutar PowerShell como Administrador |

---

## Variables de entorno disponibles

| Variable | Descripción | Ejemplo |
|---|---|---|
| `SG_DB_PATH` | Ruta absoluta a la BD SQLite | `C:\...\sistema_gestion.db` o `\\PC\Share\sg.db` |
| `ASPNETCORE_URLS` | URLs de escucha de la API | `http://0.0.0.0:5050` |
| `SG_JWT_SECRET` | Clave secreta JWT (producción) | Cadena aleatoria larga |
| `SG_HTTPS_PFX` | Ruta a certificado HTTPS .pfx | `C:\certs\sg.pfx` |
| `SG_HTTPS_PWD` | Contraseña del .pfx | — |
