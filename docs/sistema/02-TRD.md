# 02 — TRD · Requisitos técnicos y arquitectura

## 1. Panorama de arquitectura

```
                         ┌──────────────────────────────────────────┐
                         │                OLIMPO (prod)             │
   App escritorio  ┌────▶│  BD SQLite única  sistema_gestion.db     │
   (WinForms)      │     │  ────────────────────────────────────    │
   solo OLIMPO ────┘     │  API ASP.NET 8 (Kestrel :5050/:5443)      │
                         │  Digitalizador OCR (Python :8080)         │
   SERVO (API) ─SMB────▶ │  cloudflared (túnel)                      │
   por red      ┌──HTTP─▶│  wwwroot SPA (HTML+JS)                    │
                │        └───────────────┬──────────────────────────┘
   Navegadores ─┘                        │ Cloudflare Tunnel
   LAN (portal)                          │ api-sg.hato-pascher.com
                                         ▼
                        n8n (EasyPanel VPS) ──▶ Evolution API ──▶ WhatsApp
                        n8n ──▶ Gemini (imágenes/texto/video)   Cámaras EZVIZ (LAN)
```

- **App de escritorio** (WinForms, .NET 8, `net8.0-windows`): gestión completa, acceso directo a la BD. Corre **solo en OLIMPO**.
- **Portal web**: API ASP.NET 8 Minimal API + SPA en HTML/JS vanilla en `wwwroot`. Corre en OLIMPO y SERVO.
- **Base de datos**: SQLite, **un solo archivo** en OLIMPO (`C:\SistemaGestion\sistema_gestion.db`), compartido a SERVO por SMB.
- **Digitalizador OCR**: servidor Python (FastAPI/Uvicorn) en OLIMPO, puerto 8080.
- **Integraciones externas**: n8n + Evolution API (WhatsApp) y Gemini (contenido), alcanzables desde/hacia OLIMPO por el túnel Cloudflare.
- **CCTV**: cámaras EZVIZ en la LAN; la API hace de proxy de snapshots.

## 2. Stack tecnológico

| Componente | Tecnología |
|---|---|
| App escritorio | C# WinForms .NET 8 |
| API web | ASP.NET 8 Minimal API (Kestrel) |
| Frontend web | HTML + CSS + JS vanilla (SPA con router propio) |
| Base de datos | SQLite (Microsoft.Data.Sqlite), archivo único, SQL directo (sin EF Core) |
| PDF | QuestPDF (licencia Community) |
| Gráficas escritorio | GDI+ puro (System.Drawing), sin librerías externas |
| Auth API | JWT en cookie HttpOnly; PBKDF2-SHA256 (600k iteraciones, sal por usuario) |
| Auth escritorio | PBKDF2-SHA256 (formato `v2:SALT_HEX:HASH_HEX`), migración desde SHA-256 legacy |
| HTTPS | Certificado autofirmado generado/persistido por `CertService` (5 años) |
| OCR | Python + visión por IA (Ollama local / Claude Vision, configurable) |
| Contenido (IA) | Gemini (texto, imágenes, video Veo) vía n8n y llamadas directas de la API |
| Video (ensamblado) | ffmpeg + ffprobe en el servidor |
| Bot | Evolution API + n8n (EasyPanel) |
| Túnel | Cloudflare Tunnel (`api-sg.hato-pascher.com` → OLIMPO :5443) |
| CI | GitHub Actions |

## 3. Máquinas y rutas

### CHAOS — desarrollo (único que hace push)
- Repo: `C:\Users\Toor\source\repos\Prueba d cld` · remoto `github.com/od1n/Prueba-d-cld`, rama `master`.
- No tiene BD de producción ni impresora. **Todo el desarrollo se hace aquí.**

### OLIMPO — producción/admin (192.168.68.132, usuario `PC`)
- Repo: `C:\SistemaGestion` (solo `git pull`).
- **BD única**: `C:\SistemaGestion\sistema_gestion.db` (env `SG_DB_PATH`, Machine scope, ruta local).
- App de escritorio: `C:\SistemaGestion\publish\App` (self-contained; el acceso directo apunta ahí).
- API: `%LOCALAPPDATA%\SistemaGestion\publish\Api` (canónica) — arranca con `iniciar-api.cmd` (publica + ejecuta) y `sg-env.cmd` (variables).
- Digitalizador: `C:\SistemaGestion\digitalizador` (Python, :8080).
- cloudflared como servicio Windows.
- Respaldos: `C:\Users\PC\AppData\Roaming\SistemaGestion\Respaldos\` (`.db.enc`, AES-256).

### SERVO — oficina/usuario
- Repo: `C:\SistemaGestion` (solo `git pull`). API y (frontend) en `C:\SistemaGestion\publish\{Api,App}` — pero **solo se corre la API**.
- BD por red: `\\192.168.68.132\SistemaGestion\sistema_gestion.db` (SMB). Variables en `sg-env.cmd` (`SG_DB_PATH` UNC, `SG_DIG_URL` a OLIMPO, etc.).
- Puertos: HTTP 5050 (redirige a HTTPS), HTTPS 5443. Impresora EPSON L1250.
- **Nunca** se publica WinForms en SERVO.

## 4. Reglas de despliegue (críticas)

1. **Push solo desde CHAOS**; OLIMPO y SERVO solo hacen pull.
2. WinForms se publica **siempre** con `--self-contained -r win-x64` a `C:\SistemaGestion\publish\App`. Sin `--self-contained` la app no arranca ("You must install or update .NET"); publicar en otra ruta hace que "no se vean los cambios".
3. La API corre desde su carpeta *publish*; `git pull` solo trae código — **lo que activa los cambios es `actualizar-olimpo.ps1` / `actualizar-servo.ps1`** (pull → publish API + App → relanzar). El republish copia `wwwroot` a la carpeta de la API.
4. Publicar cada `.csproj` por separado, nunca la solución con `--output`.
5. Cachés del túnel: `.js/.css` no-cache, `.html` no-store; los `<script>` se versionan con `?v=YYYYMMDD` para forzar recarga.
6. `.git/index.lock` colgado bloquea commits en silencio; en Windows se borra con `Remove-Item ".git\index.lock" -Force`.

## 5. Base de datos

- SQLite, archivo único; acceso por `DatabaseService`/`Db.Conn()` con SQL directo y **transacciones explícitas** (commit/rollback) en operaciones multi-tabla.
- **Migraciones incrementales** versionadas (SCHEMA_VERSION) que se aplican al arrancar. El núcleo llegó a v29+; el módulo de contenido agrega sus tablas vía `DbContenido.EnsureContenido()`.
- Regla de oro: **una sola BD**. Cualquier `.db` en AppData u otra ruta es un error. El share SMB `SistemaGestion` debe apuntar a `C:\SistemaGestion\`.
- Resolución de ruta: `SG_DB_PATH` (Machine) → `SG_DB_PATH` (User/Process) → fallback `%APPDATA%\SistemaGestion\...`. Detalle de tablas en [05-Backend](05-Backend.md).

## 6. Integraciones

### 6.1 Digitalizador OCR
- API C# expone `POST /api/facturas/extraer-ocr` que hace de **proxy** al digitalizador Python (`POST /api/extraer`), autenticado con header `X-Internal-Token` (= `SG_INTERNAL_TOKEN`).
- Backend de visión configurable (`VISION_BACKEND`): Ollama local o Claude Vision.
- SERVO llega al digitalizador de OLIMPO vía `SG_DIG_URL=http://192.168.68.132:8080`.

### 6.2 Bot de WhatsApp
- WhatsApp → Evolution API → webhook n8n → API SistemaGestion (por el túnel) → respuesta.
- Endpoints `/api/wa/*` autenticados con `X-Internal-Token` (sin JWT). PIN de operador (PBKDF2, 6 dígitos, bloqueo por intentos) para datos sensibles (estado de cuenta, abonos).
- Notificación de pedidos por email (SMTP Gmail con App Password, variables `SG_SMTP_*`).

### 6.3 Cámaras / CCTV (EZVIZ)
- Cámaras **H8** (snapshot HTTP `/onvif/snapshot` con Digest auth; RTSP :554) y **C6N** (RTSP deshabilitado por firmware).
- La API sirve snapshots como proxy (`/api/camaras/{id}/snapshot`) con las credenciales del lado del servidor; el navegador nunca las ve.
- Credenciales de cámara **cifradas con AES-256-CBC** usando `SG_CAM_KEY` (Machine scope, misma en OLIMPO y SERVO) — reemplaza DPAPI para permitir descifrado entre máquinas.
- Permisos por usuario (tabla `PermisosCamara`): admin ve todas; otros, solo las asignadas.

### 6.4 Estudio de Contenido (redes)
- API `/api/contenido/*` (rol Redes) → n8n → Gemini para generación de imágenes/texto; llamadas directas de la API a Gemini para clasificación de bandeja, video Veo, ideas de animación y generación de texto.
- Publicación a Meta con Graph API v21.0 (imágenes como foto/carrusel, video como Reel). Marca de agua obligatoria del RIF sobre todo lo que sale (ver §7). Detalle en [`docs/redes-sociales/`](../redes-sociales/00-README.md).

## 7. Cumplimiento y marca de agua RIF

Por ley venezolana, toda publicación en redes debe llevar el RIF de la empresa (**J-30571871-7**). El sistema lo garantiza estampando una marca de agua **en la salida** (no sobre el original): al descargar el ZIP del post y al servir la imagen/video que Meta descarga para publicar. Se implementa con ffmpeg (imágenes y video), centrada arriba por defecto (configurable con `SG_RIF_POS`), con caché versionada. El texto es fijo en código (`RIF_EMPRESA`), no lo altera la IA.

## 8. Seguridad

- HTTPS con certificado autofirmado (5 años); JWT en cookie HttpOnly/Secure/SameSite=Strict; revocación por `jti`.
- Contraseñas con **PBKDF2-SHA256 600k iteraciones** y sal por usuario (migración automática desde SHA-256 legacy).
- Rate limiting en login (10/min) y en validación de PIN del bot (5/15 min por IP; bloqueo tras 5 intentos por 30 min).
- Cabeceras: CSP, X-Frame-Options DENY, HSTS, nosniff. Restricción a red local (IPs privadas/loopback).
- Autorización por rol en escritura (`RequireRoleFilter`): Admin para Nómina; Admin/Contabilidad para Compras/Egresos; Admin/Ventas para Facturas; Admin/Almacén para Producción; Redes para Contenido.
- Cifrado en reposo: respaldos AES-256 (clave DPAPI), credenciales de cámara AES-256 (`SG_CAM_KEY`).
- Secretos **nunca** en el repo: variables de entorno *Machine scope* (`SG_DB_PATH`, `SG_INTERNAL_TOKEN`, `SG_DIG_URL`, `SG_CAM_KEY`, `SG_GEMINI_KEY`, `SG_SMTP_*`, etc.).
- System prompt del asistente IA fijado del lado del servidor (no controlable por el cliente).

## 9. Variables de entorno (Machine scope)

| Variable | Máquina | Uso |
|---|---|---|
| `SG_DB_PATH` | OLIMPO, SERVO | Ruta canónica de la BD |
| `SG_INTERNAL_TOKEN` | OLIMPO, SERVO | Auth API↔digitalizador y API↔n8n (callbacks) |
| `SG_DIG_URL` | SERVO | URL del digitalizador en OLIMPO |
| `SG_CAM_KEY` | OLIMPO, SERVO | Clave AES-256 (base64) para credenciales de cámara |
| `SG_GEMINI_KEY` | OLIMPO (y SERVO) | Gemini para bandeja, OCR de madera, Veo, ideas y texto de contenido |
| `SG_SMTP_EMAIL` / `SG_SMTP_PASS` / `SG_PEDIDO_EMAIL` | OLIMPO | Email de pedidos del bot |
| `SG_RIF` / `SG_RIF_POS` | (opcional) | Override del texto/posición de la marca de agua |
| `SG_HTTPS_PFX` / `SG_HTTPS_PWD` | (opcional) | Certificado HTTPS externo (si no, autofirmado) |

## 10. Requisitos del servidor

- Windows con .NET 8 SDK/runtime (WinForms self-contained no lo requiere en cliente).
- Python + dependencias del digitalizador (OLIMPO).
- **ffmpeg + ffprobe** en el PATH (reel armado, video-check y marca de agua RIF).
- cloudflared (servicio) en OLIMPO.
- SMB configurado para compartir la BD a SERVO.
