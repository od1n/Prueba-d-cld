# Estado Completo del Proyecto — Sincronización entre equipos
> Generado: 2026-06-08  
> Cubre **todos** los proyectos trabajados en estas sesiones:  
> 1. SistemaGestion (C# WinForms + API LAN + Digitalizador)  
> 2. EHP Saddles (sitio web estático + bot Telegram)

---

## ÍNDICE RÁPIDO

| Sección | Estado |
|---|---|
| [SistemaGestion — Módulos completados](#1-sistemagestion--módulos-completados) | ✅ |
| [Cámaras EZVIZ — implementado, falta configurar IPs](#2-cámaras-ezviz-rtsp) | ⚠️ Pendiente configuración |
| [OCR Digitalizador de planillas](#3-ocr--digitalizador-de-planillas-de-producción) | ✅ |
| [OCR Egresos / Recibos](#4-ocr-egresos--recibos) | ✅ |
| [API LAN Portal web](#5-api-lan--portal-web) | ✅ |
| [Ayuda contextual F1](#6-ayuda-contextual-f1) | ✅ |
| [EHP Saddles — Sitio web](#7-ehp-saddles--sitio-web) | ✅ |
| [EHP Bot Telegram](#8-ehp-bot--telegram) | ✅ |
| [Pendientes globales](#9-pendientes-globales) | — |

---

## 1. SistemaGestion — Módulos completados

### Stack técnico
| Componente | Tecnología |
|---|---|
| Plataforma | .NET 8 · Windows Forms |
| Base de datos | SQLite (`Microsoft.Data.Sqlite`) |
| Reportes PDF | QuestPDF 2024.3.4 (licencia Community) |
| Gráficas | GDI+ puro |
| Cámaras | **LibVLCSharp 3.9.7 + VideoLAN.LibVLC.Windows** |
| API LAN | ASP.NET Core Minimal API (net8.0) |
| IA local | OllamaService.cs → integrado en Dashboard, AyudaService y MenuPrincipal |

### Todos los módulos implementados

| Módulo | Archivos principales | Estado |
|---|---|---|
| Login + seguridad (SHA-256, DPAPI, NTFS, auditoría) | `FrmLogin.cs`, `SecurityService.cs` | ✅ |
| Menú principal | `MenuPrincipalV2.cs` (única versión activa) | ✅ |
| Dashboard V2 (KPIs, gráficas GDI+, chat IA) | `FrmDashboardV2.cs` | ✅ |
| Facturas, CxC, CxP, Cambio de Precios | `FrmFacturas.cs`, `FrmCuentasCobrar.cs`, `FrmCuentasPagar.cs` | ✅ |
| Inventario, Kardex, Mermas | `FrmInventario.cs`, `FrmKardex.cs`, `FrmMermas.cs` | ✅ |
| Compras, Proveedores, Catálogo | `FrmCompras.cs`, `FrmProveedores.cs`, `FrmProductosProveedor.cs` | ✅ |
| Órdenes de Producción + cierre proporcional | `FrmOrdenesProduccion.cs` | ✅ |
| Ingreso de Producción del día + modo vueltas | `FrmIngresoProduccion.cs`, `FrmRegistroProduccionDia.cs` | ✅ |
| Control de Calidad (campo CantDefectuosas) | `FrmControlCalidad.cs` | ✅ |
| Mantenimiento de Moldes | `FrmMantenimientoMoldes.cs` | ✅ |
| Nómina Venezuela (USD + IVSS/RPVH en Bs) | `FrmNomina.cs`, `FrmProcesarNomina.cs` | ✅ |
| Beneficios LOTTT, Préstamos, Aportes Patronales | `FrmBeneficiosLOTTT.cs`, `FrmPrestamos.cs` | ✅ |
| Tarifas Bono de Producción | `FrmTarifasBonoProduccion.cs` | ✅ |
| Egresos / Gastos (multi-moneda, CxP, comprobante) | `FrmEgresos.cs`, `FrmGastos.cs`, `FrmGastoEditor.cs`, `DB_Egresos.cs` | ✅ |
| Flujo de Caja 30/60/90 días | `FrmFlujoCaja.cs` | ✅ |
| Punto de Equilibrio | `FrmPuntoEquilibrio.cs` | ✅ |
| Rentabilidad, Comparación, Libro IVA SENIAT | `FrmRentabilidad.cs`, `FrmComparacion.cs`, `FrmLibroIVA.cs` | ✅ |
| ISLR Estimado (Tarifa 3 — persona jurídica) | `FrmISLR.cs`, `DB_ISLR.cs` | ✅ |
| Tasa BCV/Paralelo automática (dolarapi.com) | `TasaCambioService.cs`, `FrmActualizarTasa.cs` | ✅ |
| Alertas críticas al iniciar sesión | `AlertasService.cs`, `FrmAlertas.cs` | ✅ |
| Buscador global F3 (8 tablas, debounce 300ms) | `FrmBuscador.cs`, `BusquedaHelper.cs` | ✅ |
| Respaldo AES-256 automático diario | `BackupService.cs`, `FrmRespaldos.cs` | ✅ |
| Log de auditoría | `FrmLogAuditoria.cs` | ✅ |
| Usuarios y roles | `FrmUsuarios.cs`, `DB_Usuarios.cs` | ✅ |
| **Cámaras EZVIZ RTSP** | `FrmCamaras.cs`, `FrmCamarasAdmin.cs`, `DlgCamara.cs`, `DB_Camaras.cs` | ✅ (falta configurar IPs) |
| Ayuda contextual F1 (3 modos) | `AyudaService.cs` + páginas web | ✅ |
| Planilla PDF de producción imprimible | `PdfService_PlanillaProduccion.cs` | ✅ |

### Dependencias NuGet clave (SistemaGestion.csproj)
```xml
<PackageReference Include="Microsoft.Data.Sqlite" Version="8.0.0" />
<PackageReference Include="QuestPDF" Version="2024.3.4" />
<PackageReference Include="LibVLCSharp.WinForms" Version="3.9.7" />
<PackageReference Include="VideoLAN.LibVLC.Windows" Version="3.0.21" />
```

---

## 2. Cámaras EZVIZ (RTSP)

### Qué está implementado (100% en código)

| Archivo | Contenido |
|---|---|
| `DB_Camaras.cs` | Modelo `Camara`, CRUD completo, tabla `PermisosCamara` (N:N por usuario), migración v23 |
| `FrmCamaras.cs` | Visor RTSP con LibVLCSharp, lista lateral, reconexión automática cada 10s, fullscreen (F11), mute, registro de errores en auditoría |
| `FrmCamarasAdmin.cs` | Panel Admin: tab Cámaras (CRUD) + tab Permisos (asignar cámaras por usuario) |
| `DlgCamara.cs` | Diálogo crear/editar cámara: IP, puerto, usuario, clave cifrada DPAPI, ruta stream, modelo |

### Diseño de seguridad
- La clave (código de verificación del sticker EZVIZ) se cifra con **DPAPI** antes de guardarse en SQLite (`SecurityService.DpapiCifrar`)
- Solo se descifra en runtime para construir la URL RTSP (`ConstruirUrlRtsp()`)
- La URL visible en logs/UI usa `***` en lugar de la clave (`ConstruirUrlVisible()`)
- Admin ve todas las cámaras activas. Otros usuarios solo ven las que Admin les asigna

### Esquema de base de datos creado (migración v23)
```sql
CREATE TABLE Camaras (
    Id            INTEGER PRIMARY KEY AUTOINCREMENT,
    Nombre        TEXT NOT NULL,
    Ubicacion     TEXT NOT NULL DEFAULT '',
    IpLocal       TEXT NOT NULL,
    PuertoRtsp    INTEGER NOT NULL DEFAULT 554,
    UsuarioCamara TEXT NOT NULL DEFAULT 'admin',
    ClaveCifrada  TEXT NOT NULL DEFAULT '',      -- base64 DPAPI
    RutaStream    TEXT NOT NULL DEFAULT '/Streaming/Channels/101',
    Modelo        TEXT NOT NULL DEFAULT '',
    Activa        INTEGER NOT NULL DEFAULT 1,
    Orden         INTEGER NOT NULL DEFAULT 0,
    FechaCreacion TEXT NOT NULL
);

CREATE TABLE PermisosCamara (
    IdUsuario INTEGER NOT NULL,
    IdCamara  INTEGER NOT NULL,
    PRIMARY KEY (IdUsuario, IdCamara)
);
```

### Rutas RTSP según modelo EZVIZ
| Modelo | Ruta principal | Ruta secundaria |
|---|---|---|
| Modelos nuevos (C3W, C6, etc.) | `/Streaming/Channels/101` | `/Streaming/Channels/102` |
| Modelos viejos | `/H.264` | — |

### ⚠️ LO QUE FALTA PARA QUE FUNCIONE

El código está completo. Solo falta **ingresar las cámaras reales** en el sistema:

1. Abrir el sistema → menú Herramientas → **Administración de cámaras** (`FrmCamarasAdmin`)
2. Tab **Cámaras** → botón **Nueva**
3. Completar el formulario `DlgCamara`:
   - **Nombre**: ej. "Entrada principal"
   - **IP local**: la IP de la cámara en la red LAN (ej. `192.168.1.101`)
   - **Puerto RTSP**: 554 (default en todas las EZVIZ)
   - **Usuario**: `admin`
   - **Clave**: el código de verificación del sticker en la cámara (ej. `ABCD1234`)
   - **Ruta stream**: `/Streaming/Channels/101` (probar con `/H.264` si no conecta)
   - **Modelo**: opcional, para referencia
4. Tab **Permisos** → asignar cámaras a cada usuario que deba verlas
5. Los usuarios no-admin abren el módulo y ven solo sus cámaras asignadas

**Para obtener la IP de cada cámara EZVIZ:**
- Opción A: Abrir EZVIZ Studio → clic derecho en la cámara → propiedades → IP
- Opción B: Entrar al router y ver la tabla de dispositivos DHCP
- Opción C: Con la cámara en la misma red, usar `arp -a` en CMD y buscar el MAC de EZVIZ

---

## 3. OCR — Digitalizador de planillas de producción

### Arquitectura
```
[Celular / tablet en LAN]
        ↓  foto de la planilla
[digitalizador/main.py — FastAPI puerto 8080]
        ↓  imagen + prompt
[Backend de visión: Ollama local | Groq Cloud | Anthropic]
        ↓  JSON extraído
[db_writer.py → SQLite (misma BD del sistema)]
        ↓
[SistemaGestion ve los datos en FrmIngresoProduccion]
```

### Archivos
| Archivo | Función |
|---|---|
| `digitalizador/main.py` | API FastAPI. Endpoints: `/api/estado`, `/api/extraer`, `/api/guardar`, UI web servida desde `templates/` |
| `digitalizador/extractor.py` | Llama al backend de visión (Ollama / Groq / Anthropic). Prompt estructurado → devuelve JSON con los campos de la planilla |
| `digitalizador/db_writer.py` | Escribe los registros extraídos directamente en la BD SQLite del sistema |
| `digitalizador/requirements.txt` | `fastapi`, `uvicorn`, `python-dotenv`, `requests`, `pillow` |
| `digitalizador/run.bat` | Arranca el servidor: `uvicorn main:app --host 0.0.0.0 --port 8080` |

### Backends de visión disponibles
| Backend | Velocidad | Costo | Calidad manuscrito | Config |
|---|---|---|---|---|
| `ollama` | Lento (depende GPU) | Gratis/local | Buena | `OLLAMA_URL` + `OLLAMA_MODEL` |
| `groq` | 2–4 seg | Gratis (tier free) | Buena | `GROQ_API_KEY` |
| `anthropic` | 3–6 seg | ~$0.001/foto | Excelente | `ANTHROPIC_API_KEY` |

Configurar en `digitalizador/.env` (copiar de `.env.example`):
```env
VISION_BACKEND=groq
GROQ_API_KEY=gsk_...
GROQ_MODEL=llama-3.2-11b-vision-preview
```

### Integración con la API LAN
- La API LAN (`SistemaGestion.Api`) tiene el endpoint `GET /api/egresos/config-vision` que verifica si el digitalizador está online
- El portal web del digitalizador se accede desde la misma red LAN con el celular: `http://<IP-servidor>:8080`
- Flujo: foto → revisión → confirmar → datos en BD

---

## 4. OCR Egresos / Recibos

### Qué está implementado

**Endpoint API LAN** (`SistemaGestion.Api/Endpoints/EgresosEndpoints.cs`):

| Endpoint | Método | Función |
|---|---|---|
| `/api/articulos` | GET | Busca artículos del catálogo de ProductosProveedor |
| `/api/egresos/config-vision` | GET | Estado del digitalizador + backend activo |
| `/api/egresos/config-vision` | PUT | Cambiar backend (solo Admin) |
| `/api/egresos/extraer` | POST | Recibe imagen de recibo → llama al digitalizador → devuelve JSON extraído |
| `/api/egresos` | POST | Guarda el egreso en la BD |
| `/api/egresos/{id}` | GET | Obtiene un egreso por ID |

**Portal web** (`SistemaGestion.Api/wwwroot/pages/egresos-nuevo`):
- Flujo 3 pasos: Paso 1 Foto → Paso 2 Revisión/corrección del OCR → Paso 3 Confirmar
- Selector de backend OCR visible en el paso 1 (con verificación de estado online/offline)

**Base de datos** (`DB_Egresos.cs`):
- Tabla `Gastos`: multi-moneda (USD/Bs), tipo de tasa (BCV/Paralelo/Directa/Personalizada), snapshot de tasa, estado (Pagado/Pendiente/Anulado), ruta de comprobante
- Tabla `CategoriasGasto`: con flag `EsMateriaPrima` para separar materia prima de gastos operativos

**Migración DB**: tabla `DetallesGasto` agregada en `SistemaGestion.Api/Db.cs`

---

## 5. API LAN — Portal web

### Arquitectura
```
SistemaGestion.Api/
├── Program.cs          ← Arranque, middlewares, rate limiting, HSTS
├── Db.cs               ← Acceso directo a la misma BD SQLite del escritorio
├── Auth/
│   ├── TokenService.cs ← JWT con clave cifrada DPAPI
│   ├── SesionesActivas.cs ← Revocación de tokens en BD
│   └── CertService.cs  ← Cert HTTPS auto-firmado (5 años, persistido)
└── Endpoints/
    ├── AuthEndpoints.cs        ← Login, logout, refresh
    ├── DashboardEndpoints.cs   ← KPIs
    ├── InventarioEndpoints.cs  ← Stock, búsqueda
    ├── KardexEndpoints.cs      ← Movimientos
    ├── ClientesEndpoints.cs    ← CRUD clientes
    ├── EgresosEndpoints.cs     ← OCR + CRUD egresos
    ├── ProduccionEndpoints.cs  ← Planillas de producción
    └── AyudaEndpoints.cs       ← Proxy a Claude Opus (rate-limited)
```

### URLs de servicio
- HTTP: `http://0.0.0.0:5050` (redirige a HTTPS)
- HTTPS: `https://0.0.0.0:5443`
- Acceso LAN: `https://<IP-servidor>:5443`

### Seguridad implementada
- JWT con revocación en BD (`SesionesActivas`)
- Rate limiting por IP (configurable)
- HSTS
- Certificado HTTPS auto-firmado de 5 años (persistido en DPAPI)
- Todos los endpoints requieren autenticación (`RequireAuthorization()`)

### Tests de integración
- `SistemaGestion.Api.Tests/ApiTests.cs`: 18 tests sin intervención de usuario
- BD SQLite temporal con semilla (`ApiFactory.cs`)
- Correr: `dotnet test SistemaGestion.Api.Tests/`

---

## 6. Ayuda contextual F1

### Tres modalidades implementadas (portal web LAN)

| Modo | Archivo | Descripción |
|---|---|---|
| **Slides narrados** | `ayuda-factura.html` | Walkthrough animado de facturas con Web Speech API (TTS) |
| **Chat IA** | `ayuda-empleados.html` | Chat interactivo con Claude Opus vía proxy `AyudaEndpoints.cs` |
| **Walkthrough CSS** | `ayuda-kardex.html` | Pasos animados con screenshots reales del sistema |

### Captura de screenshots
- `capturar_ayuda.ps1`: script PowerShell v3 que captura ventanas sin mostrar la consola (`GetConsoleWindow` + `SW_HIDE`)
- Los screenshots reales se usan en los walkthroughs CSS de ayuda-kardex

### AyudaEndpoints.cs
- Proxy a Claude Opus con rate limiting (protege la API key de Anthropic)
- Requiere autenticación JWT

---

## 7. EHP Saddles — Sitio web

### Arquitectura
- Sitio **estático**: HTML + CSS + JS vanilla. Sin framework, sin build step.
- Hospedado en **Banahosting** (cPanel / Apache)
- Dominio: `https://www.ehp-saddles.com`
- Ruta local: `C:\Users\Toor\source\repos\ehp web\`
- Ruta servidor: `public_html/`

### Cambios aplicados (todos verificados en repo)

#### `.htaccess` (NUEVO)
Creado con: `Options -Indexes`, HTTP→HTTPS, headers de seguridad (X-Frame-Options, X-Content-Type-Options, Referrer-Policy, Permissions-Policy, CSP), GZIP, caché 1 año para imágenes, MIME type WebP, bloqueo de `.env`/`.git`/`.log`.

#### `productos.html` — bug crítico corregido
El archivo estaba **truncado** en línea 593: faltaban `render('all')` y `b.classList.add('active'); render(b.dataset.cat)`. Efecto: el catálogo no mostraba productos y los filtros no respondían. Corregido con script Python.

Otros cambios en `productos.html`:
- Botón WhatsApp → "Consultar por Telegram"
- "6 semanas" / "6 sem" → "¡Consúltenos!" / "Ask us!"

#### `producto.html` — página de detalle
- Lead time: "Tiempo de producción · 3 a 6 semanas" → "Producción por encargo · ¡Consúltenos!"
- Diagrama de medidas condicional: excluido para categoría `farm` y producto `estribos`
- Facebook link corregido

#### Facebook link — corregido en 12 archivos
De `facebook.com/jac.pascher` (perfil personal) a `facebook.com/elhatopascher` (página empresa).  
Archivos: `productos.html`, `producto.html`, `404.html`, `index.html`, `moldes-a-medida.html`, `nosotros.html`, `glosario.html`, `blog.html`, `blog/como-medir-caballo.html`, `blog/fuste-flexible-columna.html`, `blog/hdpe-vs-madera.html`, `blog/historia-fuste-madera-polietileno.html`.

#### WhatsApp → Telegram (todos los archivos)
Incluye menciones de texto en artículos del blog que no eran links:
- `blog/como-medir-caballo.html`: 3 menciones en texto del artículo
- `blog/hdpe-vs-madera.html`: 1 mención en texto del artículo

**Verificado:** ningún archivo HTML del sitio menciona WhatsApp.

#### `assets/products.js` — datos técnicos corregidos
Fuente: catálogos EHP 2018 + imágenes con medidas en `recursos grafiacos/`

| Modelo | Corrección aplicada |
|---|---|
| McClellan 14" | weight=3kg, LS=40,5cm, AF=27cm, AT=32,3cm, LA=54,3cm |
| Pico Colombiano | weight=3kg, LS=40cm, AF=27cm, AT=32cm, LA=49,5cm, **AC=16cm** (antes 20), **ATh=24cm** (antes 16) |
| Western (= "Cutting" en docs 2018) | weight=3kg, LS=38,2cm, AF=30,5cm, AT=30,5cm, LA=49cm, AC=17,5cm, ATh=16cm |
| Tejano | weight=3kg |
| Mexicano | weight=4,1kg |
| Charro | weight=4,4kg |

**Nota importante:** el Pico Colombiano tenía AC y ATh **invertidos** — en el "pico" colombiano la pala trasera (ATh=24cm) es más alta que el borrén delantero (AC=16cm), lo cual es correcto anatómicamente.

#### Deploy en Banahosting — procedimiento
1. ZIP del sitio → subir a `public_html/` vía cPanel File Manager → Extract
2. **CRÍTICO después de extraer**: permisos 0644 (archivos) y 0755 (carpetas), incluyendo subcarpetas `assets/` y `blog/` por separado si el File Manager no lo aplica recursivamente
3. Sin esto: 403 Forbidden en todas las páginas

---

## 8. EHP Bot — Telegram

### Ubicación
`C:\Users\Toor\source\repos\Prueba d cld\ehp-bot\`

### Archivos
| Archivo | Función |
|---|---|
| `bot.py` | Lógica principal: ConversationHandler, menú con botones, pedido guiado |
| `email_sender.py` | Email vía Gmail SMTP con template HTML |
| `catalog.py` | Datos del catálogo (sincronizados con products.js del sitio) |
| `config.py` | Variables de entorno |
| `llm.py` | Llamadas a Groq API |
| `prices.py` | Precios de referencia |
| `requirements.txt` | `python-telegram-bot==21.6`, `groq`, etc. |
| `Procfile` | Para Railway: `web: python bot.py` |
| `railway.toml` | Configuración de Railway |
| `SETUP.md` | Guía completa de instalación y deploy |

### Correcciones de seguridad aplicadas

**`email_sender.py`:**
- Bug 1 — KeyError: `pedido['producto_nombre']` → `pedido['prod_nombre']` (nombre del campo incorrecto)
- Bug 2 — HTML injection: todos los campos de usuario se escapan con `html.escape()` antes de insertarse en el template HTML

**`bot.py`:**
- Límites de longitud: `nombre/ubicacion/contacto[:150]`, `cantidad[:30]`
- Validación de callbacks: categorías validadas contra dict `CATS`; códigos de producto solo alfanuméricos ≤30 chars
- `asyncio.get_event_loop()` → `asyncio.get_running_loop()` (corrige deprecation en Python 3.10+)

**Anti-abuso (ya existía):**
- Cooldown 24h por `chat_id` (en memoria)
- Cap global: 20 emails/día

### Deploy — Railway
Variables de entorno requeridas:
```
TELEGRAM_TOKEN    = (token de BotFather)
OPERATOR_CHAT_ID  = (ID numérico del operador — obtener hablando con @userinfobot en Telegram)
GROQ_API_KEY      = (desde console.groq.com — cuenta gratuita)
GROQ_MODEL        = llama-3.1-8b-instant
```
El nombre del bot en Telegram es: `@El_Hato_Fustes`  
Ver `SETUP.md` para el procedimiento paso a paso.

---

## 9. Guía ManyChat + Make

**Archivo:** `Prueba d cld/ehp-mensajes-ig-fb-telegram.docx`

Documento Word que cubre:
1. **ManyChat** — respuesta automática en Instagram/Facebook DM (avisa al cliente que el taller responde por Telegram y da link al bot)
2. **Make (Integromat)** — escenario que reenvía cada mensaje nuevo de Instagram/Facebook al chat de Telegram del operador

Pendiente de implementación por el usuario. Solo necesita su `OPERATOR_CHAT_ID` (obtenible hablando con `@userinfobot` en Telegram).

---

## 10. Pendientes globales

| # | Proyecto | Tarea | Qué se necesita |
|---|---|---|---|
| 1 | SistemaGestion | **Configurar cámaras EZVIZ** | Abrir `FrmCamarasAdmin` → ingresar IPs, usuario `admin`, clave del sticker, ruta stream. Ver sección 2 de este doc. |
| 2 | EHP Bot | **Deploy a Railway** | El bot está listo. Ver `ehp-bot/SETUP.md`. Requiere cuenta Railway + secrets. |
| 3 | EHP Saddles | **Subir blog a Banahosting** | `blog/como-medir-caballo.html` y `blog/hdpe-vs-madera.html` tienen correcciones nuevas (WhatsApp→Telegram). Subir a `public_html/blog/`. |
| 4 | EHP Bot + IG/FB | **Implementar ManyChat + Make** | Seguir `ehp-mensajes-ig-fb-telegram.docx`. |
| 5 | SistemaGestion | **README desactualizado** | El `README.md` dice ISLR pendiente y Ollama sin conectar, pero ambos ya están implementados (tasks 8, 9, 10, 11, 13). Actualizar el README. |

---

## 11. Verificación de estado — archivos críticos

### SistemaGestion
| Archivo | Verificar que exista y tenga |
|---|---|
| `FrmCamaras.cs` | Clase `FrmCamaras`, usa `LibVLCSharp`, timer de reconexión, lista lateral |
| `DB_Camaras.cs` | Clase `Camara`, método `ConstruirUrlRtsp()`, `EjecutarMigracionV23()` |
| `FrmCamarasAdmin.cs` | Dos tabs: Cámaras y Permisos |
| `DlgCamara.cs` | Campos: IP, puerto, usuario, clave, ruta stream |
| `digitalizador/main.py` | FastAPI app, endpoint `/api/estado` y `/api/extraer` |
| `digitalizador/extractor.py` | Soporte para backends: `ollama`, `groq`, `anthropic` |
| `SistemaGestion.Api/Endpoints/EgresosEndpoints.cs` | Constante `DIG_URL = "http://localhost:8080"`, endpoints de OCR |

### EHP Saddles
| Archivo | Verificar que contenga |
|---|---|
| `ehp web/.htaccess` | `Options -Indexes`, `X-Frame-Options`, `ExpiresByType image/webp` |
| `ehp web/productos.html` | Al final del script: `render('all');` antes de `</script>` |
| `ehp web/assets/products.js` | Pico Colombiano: `AC:'16 cm'`, `ATh:'24 cm'` |
| `ehp web/blog/como-medir-caballo.html` | Sin WhatsApp, con Telegram en 3 lugares |
| `Prueba d cld/ehp-bot/email_sender.py` | `import html as html_lib`, uso de `e(v)` en todos los campos |
| `Prueba d cld/ehp-bot/bot.py` | `asyncio.get_running_loop()`, `[:150]` en inputs, validación `CATS` |

---

## 12. Cómo correr todo en el equipo nuevo

### SistemaGestion (escritorio)
```bash
cd "Prueba d cld"
dotnet run --project SistemaGestion.csproj
# Credenciales iniciales: admin / admin123
```

### API LAN
```bash
cd "Prueba d cld/SistemaGestion.Api"
dotnet run
# HTTPS en puerto 5443, HTTP en 5050
```

### Digitalizador OCR
```bash
cd "Prueba d cld/digitalizador"
# Copiar .env.example a .env y configurar VISION_BACKEND
pip install -r requirements.txt
run.bat
# Acceder desde celular en LAN: http://<IP>:8080
```

### EHP Bot
```bash
cd "Prueba d cld/ehp-bot"
pip install -r requirements.txt
# Crear .env con las variables (ver SETUP.md)
python bot.py
```

### EHP Sitio web
- Es estático, no requiere servidor local. Editar archivos en `ehp web/` y subir a Banahosting.
- Ver sección 7 para procedimiento de deploy.
