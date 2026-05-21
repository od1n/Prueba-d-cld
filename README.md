# SistemaGestion — Sistema de Gestión Empresarial

Aplicación de escritorio Windows Forms para gestión empresarial, orientada a empresas venezolanas que operan en USD con cálculos de aportes y nómina en Bs (IVSS, LOTTT).

## Stack

| Componente | Tecnología |
|---|---|
| Plataforma | .NET 8 · Windows Forms |
| Base de datos | SQLite (`Microsoft.Data.Sqlite 8.0.0`) |
| Reportes PDF | QuestPDF 2024.3.4 (licencia Community) |
| Gráficas | GDI+ puro (sin dependencias externas) |
| Cámaras | LibVLCSharp 3.9.7 + VideoLAN.LibVLC.Windows |
| API LAN | ASP.NET Core Minimal API (net8.0) |

## Credenciales por defecto

- Usuario: `admin` / Contraseña: `admin123`
- Se crean automáticamente en el primer arranque.

## Cómo compilar y ejecutar

```bash
# Compilar la aplicación de escritorio
dotnet build SistemaGestion.csproj

# Ejecutar
dotnet run --project SistemaGestion.csproj

# Compilar y ejecutar la API LAN (portal web en red local)
dotnet run --project SistemaGestion.Api/SistemaGestion.Api.csproj
# HTTP  → http://0.0.0.0:5050  (redirige a HTTPS)
# HTTPS → https://0.0.0.0:5443

# Correr los tests de integración de la API (sin intervención del usuario)
dotnet test SistemaGestion.Api.Tests/SistemaGestion.Api.Tests.csproj
```

## Estructura del proyecto

```
SistemaGestion/
│
├── SistemaGestion.csproj           ← Proyecto WinForms (.NET 8)
├── SistemaGestion.sln              ← Solución (3 proyectos)
├── Program.cs                      ← Arranque: Security → DB → Login → Alertas → Run
│
├── ── Datos ──────────────────────────────────────────────────────────────
├── DB.cs                           ← DatabaseService (partial): núcleo ~3000 líneas
├── DB_Camaras.cs                   ← Catálogo EZVIZ + permisos por usuario
├── DB_CuentasPagar.cs
├── DB_Deploy.cs                    ← Migraciones y TasaCambio
├── DB_Egresos.cs
├── DB_Kardex.cs / DB_LibroIVA.cs / DB_Mermas.cs
├── DB_ProductosProveedor.cs
├── DB_ProduccionDiaria.cs / DB_RegistroProduccion.cs
├── DB_TarifasBono.cs / DB_Usuarios.cs / DB_ValidacionNomina.cs
│
├── ── Servicios ───────────────────────────────────────────────────────────
├── SecurityService.cs              ← DPAPI, AES-256, NTFS ACL, AppData
├── BackupService.cs                ← Respaldo automático diario cifrado AES-256
├── PdfService.cs                   ← Reportes QuestPDF (base)
├── PdfService_*.cs                 ← Comparacion, CuentasPagar, LibroIVA, Mermas, PE, Reportes
├── TasaCambioService.cs            ← BCV/Paralelo desde dolarapi.com (async, con fallback)
├── AlertasService.cs               ← Stock bajo + CxC vencidas al inicio de sesión
├── AyudaService.cs                 ← Ayuda contextual por formulario (F1)
├── OllamaService.cs                ← Cliente HTTP para Ollama local (preparado, sin UI aún)
│
├── ── UI compartida ───────────────────────────────────────────────────────
├── FormBase.cs / UICards.cs / UIHelper.cs / UITheme.cs
├── ValidacionInline.cs / BusquedaHelper.cs
│
├── ── Formularios ─────────────────────────────────────────────────────────
├── FrmLogin.cs
├── MenuPrincipalV2.cs              ← Menú principal activo
├── FrmDashboardV2.cs               ← Dashboard con KPIs y gráficas GDI+
├── FrmAlertas.cs / FrmBuscador.cs (F3)
├── FrmFacturas.cs / FrmCuentasCobrar.cs / FrmCuentasPagar.cs / FrmCambioPrecios.cs
├── FrmInventario.cs / FrmKardex.cs / FrmMermas.cs
├── FrmCompras.cs / FrmProveedores.cs / FrmProductosProveedor.cs / FrmClientes.cs
├── FrmOrdenesProduccion.cs / FrmControlCalidad.cs / FrmMantenimientoMoldes.cs
├── FrmRegistroProduccionDia.cs / FrmIngresoProduccion.cs
├── FrmNomina.cs / FrmProcesarNomina.cs / FrmPrestamos.cs / FrmBeneficiosLOTTT.cs
├── FrmTarifasBonoProduccion.cs
├── FrmEgresos.cs / FrmGastos.cs / FrmGastoEditor.cs
├── FrmFlujoCaja.cs / FrmPuntoEquilibrio.cs
├── FrmRentabilidad.cs / FrmComparacion.cs / FrmLibroIVA.cs
├── FrmCamaras.cs / FrmCamarasAdmin.cs / DlgCamara.cs
├── FrmActualizarTasa.cs / FrmTrazabilidadTasa.cs
├── FrmReportes.cs / FrmArchivos.cs / FrmSistema.cs
├── FrmLogAuditoria.cs / FrmRespaldos.cs / FrmUsuarios.cs / FrmPerfil.cs
│
├── SistemaGestion.Api/             ← Portal LAN (ASP.NET Core Minimal API, net8.0)
│   ├── Program.cs
│   ├── Db.cs                       ← Acceso directo a la misma BD SQLite del escritorio
│   ├── Auth/TokenService.cs        ← JWT con clave cifrada DPAPI
│   ├── Auth/SesionesActivas.cs     ← Revocación de tokens en BD
│   ├── Auth/CertService.cs         ← Cert HTTPS auto-firmado (5 años, persistido)
│   └── Endpoints/                  ← Auth, Dashboard, Inventario, Kardex, Clientes, Facturas
│
├── SistemaGestion.Api.Tests/       ← Tests de integración (xUnit + WebApplicationFactory)
│   ├── ApiFactory.cs               ← BD SQLite temporal con semilla
│   └── ApiTests.cs                 ← 18 tests, sin intervención del usuario
│
├── manual/                         ← Manual operativo (50 fichas en Markdown)
└── Scripts/                        ← SQL auxiliares (carga de clientes)
```

## Base de datos

**Ruta en producción:** `%AppData%\SistemaGestion\sistema_gestion.db`

La BD se migra automáticamente desde el directorio del ejecutable al primer arranque. Los respaldos diarios (AES-256) se guardan en `%AppData%\SistemaGestion\Respaldos\` y se retienen 30 días.

### Tablas principales (35+)

| Módulo | Tablas |
|---|---|
| Inventario | `Inventario`, `MovimientosInventario` |
| Ventas | `Facturas`, `DetallesFactura`, `Clientes`, `CuentasCobrar`, `Abonos` |
| Compras | `Compras`, `DetallesCompra`, `Proveedores`, `ProductosProveedor` |
| Producción | `IngresoProduccion`, `OrdenesProduccion`, `LineasOrden`, `ProduccionDelDiaEnLinea` |
| Calidad | `InspeccionesCalidad`, `DefectosCalidad` |
| Moldes | `TbMoldes`, `ProgMantenimiento`, `RegistroMantenimiento` |
| Nómina | `Empleados`, `PeriodosNomina`, `DetallesNomina`, `Prestamos`, `AportesPatronales` |
| Contabilidad | `CuentasPagar`, `PagosCuentasPagar`, `Egresos`, `Gastos`, `TasaCambio` |
| Cámaras | `Camaras`, `PermisosCamara` |
| Sistema | `Usuarios`, `LogAuditoria`, `SesionesActivas`, `ConfiguracionSistema` |

## Módulos

| Módulo | Estado |
|---|---|
| Seguridad (login SHA-256, roles, DPAPI, NTFS, respaldo AES, auditoría) | ✅ |
| Ventas: Facturas → Kardex → Inventario (transacción atómica) | ✅ |
| Cuentas por Cobrar y Cuentas por Pagar | ✅ |
| Compras y catálogo de productos por proveedor | ✅ |
| Inventario y Kardex (productos y materias primas) | ✅ |
| Órdenes de Producción + cierre proporcional de insumos | ✅ |
| Control de Calidad · Mantenimiento de Moldes · Mermas | ✅ |
| Nómina Venezuela (USD + IVSS/RPVH calculados en Bs) | ✅ |
| Beneficios LOTTT · Aportes Patronales · Préstamos | ✅ |
| Flujo de Caja 30/60/90 días · Punto de Equilibrio | ✅ |
| Tasa de cambio BCV/Paralelo automática (dolarapi.com + fallback) | ✅ |
| Libro de IVA formato SENIAT · Rentabilidad · Comparación | ✅ |
| Dashboard V2 con KPIs y gráficas GDI+ · Alertas al inicio | ✅ |
| Buscador global F3 (8 tablas, debounce 300ms) | ✅ |
| Cámaras EZVIZ en vivo (RTSP + LibVLCSharp, permisos por usuario) | ✅ |
| API LAN HTTPS (JWT, rate limiting, HSTS, revocación) | ✅ parcial (6 endpoints) |
| ISLR Estimado | 🔲 pendiente |

## Roles y permisos

| Rol | Acceso |
|---|---|
| `Admin` | Todo |
| `Ventas` | Facturas, CxC, Clientes, Reportes |
| `Almacen` | Inventario, Kardex, Compras, Proveedores, Producción, Moldes |
| `Contabilidad` | Reportes, Facturas, Compras, Gastos, Precios |

## Notas técnicas

**DPAPI solo en Windows.** `SecurityService`, `CertService` y `TokenService` usan DPAPI para cifrar claves en reposo. El proyecto no es portable a Linux por diseño.

**Nómina en USD con IVSS en Bs.** Los aportes legales venezolanos se calculan sobre el salario convertido a Bs con la tasa BCV del período.

**GDI+ en lugar de librerías de gráficas.** Evita dependencias adicionales; funciona en .NET 8 sin configuración.

**`OllamaService.cs`** es un cliente HTTP para un servidor Ollama local. Está preparado para integración futura (asistente contextual o análisis en dashboard); actualmente no está conectado a ningún formulario.

**Sin acentos en literales de código.** El compilador de Windows puede fallar con caracteres especiales en string literals; se usan sin acento en el código fuente y con acento solo en la capa UI.
