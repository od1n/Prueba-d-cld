# 05 — Backend · Base de datos, API y servicios

## 1. Acceso a datos

- **SQLite** con SQL directo (sin ORM). En escritorio, `DatabaseService` (clase `static partial`, helpers `S`/`D`/`Exec`, transacciones explícitas). En la API, `Db.Conn()` (+ `DbContenido` para el módulo de redes).
- **Migraciones** incrementales versionadas al arrancar (núcleo v29+; contenido vía `DbContenido.EnsureContenido()`). Fixes defensivos crean columnas/tablas faltantes al iniciar.
- **Una sola BD**: `C:\SistemaGestion\sistema_gestion.db` en OLIMPO; SERVO por SMB. Resolución de ruta por `SG_DB_PATH`.

## 2. Tablas por módulo

### Núcleo ERP
| Módulo | Tablas |
|---|---|
| Inventario | `Inventario`, `MovimientosInventario` |
| Ventas | `Facturas`, `DetallesFactura`, `Clientes`, `CuentasCobrar`, `Abonos`, `Cheques` |
| Compras | `Compras`, `DetallesCompra`, `Proveedores` |
| Producción | `Produccion`, `LineaProducDia`, `ProduccionDelDiaEnLinea`, `IngresoProduccion` |
| Órdenes | `OrdenesProduccion`, `LineasOrden` |
| Calidad | `InspeccionesCalidad`, `DefectosCalidad` |
| Mantenimiento | `TbMoldes`, `RegistrosMantenimiento`, `ProgramasMantenimiento` |
| Finanzas | `Gastos`, `ProductosPrecios` |
| Nómina | `Empleados`, `PeriodosNomina`, `DetallesNomina`, `PrestamosEmpleado`, `BeneficiosAnuales`, `AportesPatronales` |
| Sistema | `Usuarios`, `LogAuditoria`, `PreferenciasUsuario`, `ConfiguracionSistema` (parámetros legales), `ConfigSistema` (legacy) |

### Cámaras / CCTV
- `Camaras` (datos y credencial cifrada), `PermisosCamara` (qué usuario ve qué cámara).

### Estudio de Contenido (redes)
- `Marcas`, `IdeasContenido`, `Posts`, `PostAssets`, `Publicaciones`, `CuentasRed`, `MensajesRed`, `BandejaConfig`, `VideoJobs` (creadas por `DbContenido`).

> El conteo histórico del núcleo era ~32 tablas; con cámaras y contenido el total es mayor. La fuente de verdad es el esquema en `DB.cs` (escritorio) y `Db.cs` + `DbContenido.cs` (API).

## 3. API — organización

Minimal API. Los endpoints se registran por archivo en `SistemaGestion.Api/Endpoints/` (≈157 rutas). Autorización por rol con `RequireRoleFilter`; auth con JWT en cookie (salvo los canales internos, ver §5).

| Archivo | Área | Notas |
|---|---|---|
| `AuthEndpoints.cs` | Login/logout, sesión, cambio de contraseña | JWT en cookie; PBKDF2; rate limit |
| `DashboardEndpoints.cs` | KPIs, series para gráficas, tasas | Lectura |
| `ClientesEndpoints.cs` | Clientes, **Facturas**, impresión, **OCR** (`/api/facturas/extraer-ocr`) | Escritura: Admin/Ventas |
| `AbonosEndpoints.cs` | Cuentas por cobrar, abonos, cheques | |
| `ComprasEndpoints.cs` | Compras + kardex de entrada | Admin/Contabilidad |
| `EgresosEndpoints.cs` | Gastos/egresos | Admin/Contabilidad |
| `InventarioEndpoints.cs` | Inventario, precios | Admin/Almacén |
| `KardexEndpoints.cs` | Movimientos, ajustes | Admin/Almacén |
| `ProduccionEndpoints.cs` | Órdenes de producción, ingreso, calidad, moldes | Admin/Almacén |
| `NominaEndpoints.cs` | Empleados, períodos, cálculo, aportes, beneficios | Admin |
| `ReportesEndpoints.cs` | Reportes (rentabilidad, etc.) | Lectura por rol |
| `ConfigEndpoints.cs` | Configuración legal (`/api/config/legal`: IVA, etc.) | Admin |
| `AyudaEndpoints.cs` | Asistente IA (system prompt server-side) | |
| `WhatsAppEndpoints.cs` | Canal del bot (`/api/wa/*`) | `X-Internal-Token` |
| `CamarasEndpoints.cs` | Cámaras y snapshots | Proxy server-side |
| `ContenidoEndpoints.cs` | Estudio de Contenido (`/api/contenido/*`) | Rol Redes + canales internos |

### 3.1 WhatsApp (`/api/wa/*`, auth `X-Internal-Token`)
`GET /api/wa/catalogo?q=`, `GET /api/wa/producto/{codigo}`, `POST /api/wa/validar-pin`, `POST /api/wa/estado-cuenta`, `POST /api/wa/abonos`, `GET /api/wa/pedido?nombre=&tel=&items=&wa=`.

### 3.2 Cámaras (`/api/camaras`)
`GET /api/camaras` (lista según permisos), `GET /api/camaras/{id}/snapshot` (proxy JPEG con credenciales del servidor), `GET /api/camaras/count` (badge).

### 3.3 Contenido (`/api/contenido/*`, rol Redes)
Áreas principales (detalle en [`docs/redes-sociales/05-Backend.md`](../redes-sociales/05-Backend.md)):
- Marcas, ideas, posts y assets; referencias (imágenes y video) en dos bancos por marca.
- Generación: `/posts/{id}/generar` (todo/texto/imagen/ajuste), `/posts/{id}/texto` (texto desde idea/video), `/posts/{id}/video` (armado / IA Veo), `/posts/{id}/video-externo`, `/posts/{id}/idea-animacion`, `/posts/{id}/video-check`.
- Publicación: `/posts/{id}/paquete.zip` (ZIP con marca de agua RIF), `/pub/{token}/{idx}` (media pública que Meta descarga, con marca de agua), `/posts/{id}/publicar` (foto/carrusel o Reel), `/posts/{id}/marcar-publicado`, `/cuentas/{id}`.
- Métricas: `/posts/{id}/metricas`, `/marcas/{id}/metricas-cuenta`, `/marcas/{id}/medios-recientes`, `/posts/{id}/vincular`.
- Bandeja: webhook `/api/contenido/webhook/meta` (sin auth, verificación por token), `/marcas/{id}/mensajes`, `/mensajes/simular`, `/avisos`, `/mensajes/{id}/responder|estado`, `/marcas/{id}/bandeja-config`.

## 4. Servicios (escritorio)
- `SecurityService`: rutas de BD, NTFS ACL, DPAPI, AES-256 (respaldos y cámaras), cadena de conexión.
- `BackupService`: respaldo automático diario cifrado + verificación de integridad + limpieza (>30 días).
- `PdfService`: todos los reportes con QuestPDF (facturas, nómina, IVSS, aportes patronales, beneficios, rentabilidad).

## 5. Autenticación y canales
- **Portal**: JWT en cookie HttpOnly/Secure/SameSite=Strict; PBKDF2-SHA256 600k; revocación por `jti`; rate limit en login.
- **Canales internos** (no JWT): `X-Internal-Token` (= `SG_INTERNAL_TOKEN`) para API↔digitalizador, API↔n8n (WhatsApp y callbacks de contenido). El webhook de Meta se excluye de la auth y se valida con su propio token de verificación.
- **PIN de operador** (bot): PBKDF2 (formato `v2:SALT:HASH`), 6 dígitos, bloqueo por intentos, rate limit.

## 6. Integración OCR (contrato)
- `POST /api/facturas/extraer-ocr` (API C#) → `POST /api/extraer` (Python, `tipo=factura_venta|factura_compra|planilla`), header `X-Internal-Token`.
- Respuesta: JSON con los campos extraídos para prellenar el formulario de revisión.
- Config: `VISION_BACKEND` (Ollama/Claude), `SG_DIG_URL` (desde SERVO), `SG_INTERNAL_TOKEN` (compartido).

## 7. Notas de esquema y compatibilidad
- Fixes defensivos al arrancar la API crean columnas faltantes (p. ej. `PrecioUnit`, `Posts.PublicToken`) para tolerar BD que aún no corrieron la migración de escritorio.
- `ConfiguracionSistema` (parámetros legales, IVA, etc.) debe existir; si falta se crea.
- Convención histórica: literales de código sin acentos (se muestran con acento solo en la UI) para evitar problemas del compilador en Windows.
