# SistemaGestion — Contexto de Proyecto para Sesiones de Desarrollo

## Prioridades de comunicación
- Español neutro (NO argentino)
- Siempre entregar comandos PowerShell con rutas completas
- No estar de acuerdo por defecto: buscar el resultado óptimo, cuestionar si algo no tiene sentido
- Investigar causas raíz antes de hacer cambios, no ensayo y error

---

## 1. ¿Qué es SistemaGestion?

ERP para una fábrica venezolana de plásticos (EL HATO PASCHER C.A., RIF J-30571871-7, Villa de Cura, Aragua).

Dos interfaces:
- **App de escritorio** (WinForms .NET 8, solo en OLIMPO): gestión completa, acceso directo a SQLite
- **Portal web** (ASP.NET 8 Minimal API + HTML/JS): acceso desde cualquier equipo en la LAN, impresión de facturas

Módulos: Dashboard, Facturación, Clientes, Inventario, Kardex, Producción, Nómina, Egresos/Compras, Reportes, Digitalizador OCR, Ayuda IA.

Credenciales: admin / admin1234

---

## 2. Máquinas y rutas

### CHAOS (desarrollo) — donde se edita código
- **SO**: Windows, usuario `Toor`
- **Repo**: `C:\Users\Toor\source\repos\Prueba d cld`
- **Git remote**: `https://github.com/od1n/Prueba-d-cld.git`, rama `develop`
- **No tiene impresora ni base de datos de producción**

### OLIMPO (producción — admin) — IP: 192.168.68.132
- **SO**: Windows, usuario `PC`
- **Repo**: `C:\SistemaGestion`
- **Publish WinForms**: `C:\Users\PC\AppData\Local\SistemaGestion\publish\App`
- **Publish API**: `C:\Users\PC\AppData\Local\SistemaGestion\publish\Api`
- **BD SQLite**: `C:\SistemaGestion\sistema_gestion.db` (única BD, compartida por red como `\\192.168.68.132\SistemaGestion\sistema_gestion.db`)
- **Env var `SG_DB_PATH`**: `C:\SistemaGestion\sistema_gestion.db` (User scope — OBLIGATORIA, sin ella la app busca en AppData una BD inexistente)
- **Tablas config**: `ConfigSistema` (legacy) y `ConfiguracionSistema` (v16+, para parámetros legales IVA/IVSS/etc.)
- **Digitalizador Python**: `C:\SistemaGestion\digitalizador`
- **Funciones**: BD maestra, app escritorio, API, digitalizador OCR
- **Después de pull**:
  - WinForms: `dotnet publish SistemaGestion.csproj -c Release -r win-x64 --self-contained -o "C:\Users\PC\AppData\Local\SistemaGestion\publish\App"`
  - API: `dotnet publish SistemaGestion.Api -c Release -o "C:\Users\PC\AppData\Local\SistemaGestion\publish\Api"`
  - Luego reiniciar ambos

### SERVO (oficina — usuario) — conecta a OLIMPO por red
- **Repo**: `C:\SistemaGestion` (raíz, NO subcarpeta repo)
- **Publish API**: `C:\Users\naos\AppData\Local\SistemaGestion\publish\Api`
- **Content root en ejecución**: `C:\SistemaGestion\publish\Api` (verificado: la API corre desde publish, NO desde bin/Debug)
- **BD**: `\\192.168.68.132\SistemaGestion\sistema_gestion.db` (red, misma BD de OLIMPO)
- **Digitalizador**: usa el de OLIMPO via `http://192.168.68.132:8080`
- **Funciones**: SOLO API web (NO WinForms). Impresión de facturas sobre formularios preimpresos
- **Script inicio**: `C:\SistemaGestion\Scripts\iniciar-servicios-servo.cmd`
- **Puertos**: HTTP 5050 (redirige a HTTPS), HTTPS 5443
- **Cert HTTPS**: CN=SistemaGestion-LAN, O=SistemaGestion, OU=Portal (expira 2031-06-09)
- **Impresora**: EPSON L1250 (soporta tamaños de papel personalizados)
- **IMPORTANTE**: NO crear subcarpetas repo/ ni Prueba-d-cld-develop/ dentro de C:\SistemaGestion — causan duplicación de archivos .cs y errores de compilación
- **IMPORTANTE**: Después de pull hay que **republicar** (`dotnet publish`) y reiniciar la API. Solo `dotnet build` NO es suficiente — la API ejecuta desde la carpeta publish

---

## 3. Flujo de deploy

**Ramas activas**: `master` y `develop`. Verificar siempre en cuál rama se hizo el commit (git commitea en la rama activa). Ambas deben mantenerse sincronizadas.

```powershell
# 1. En CHAOS — commit y push
cd "C:\Users\Toor\source\repos\Prueba d cld"
git add -A
git commit -m "descripción"
git push origin master
# Si también se usa develop: git checkout develop && git merge master && git push origin develop

# 2. En OLIMPO — pull, publish WinForms + API por separado, reiniciar
cd C:\Users\PC\AppData\Local\SistemaGestion\repo
git pull origin master
dotnet publish SistemaGestion.csproj -c Release -o C:\Users\PC\AppData\Local\SistemaGestion\publish\App --self-contained
dotnet publish SistemaGestion.Api\SistemaGestion.Api.csproj -c Release -o C:\Users\PC\AppData\Local\SistemaGestion\publish\Api --self-contained
# Reiniciar: cerrar ventana de la API y ejecutar:
C:\Users\PC\AppData\Local\SistemaGestion\repo\Scripts\iniciar-servicios-olimpo.cmd

# 3. En SERVO — pull, publish SOLO API (NO WinForms), reiniciar API
cd C:\SistemaGestion
git pull origin master
dotnet publish SistemaGestion.Api -c Release -o C:\SistemaGestion\publish\Api
# Detener la API (Ctrl+C en la ventana) y reiniciar:
C:\SistemaGestion\Scripts\iniciar-servicios-servo.cmd
```

**CRÍTICO — SERVO**: `dotnet build` compila en `bin/Debug` pero la API corre desde `C:\SistemaGestion\publish\Api`. Siempre usar `dotnet publish` después de cada pull, de lo contrario los cambios no se reflejan.

**IMPORTANTE**: publicar CADA proyecto por separado (no la solución). `dotnet publish` a nivel de .sln con `--output` mezcla salidas y causa errores.

**IMPORTANTE**: en SERVO NUNCA publicar WinForms — solo la API. SERVO no tiene app de escritorio.

**Nota**: el servicio Windows `SistemaGestionApi` NO está instalado actualmente. La API se ejecuta manualmente via los scripts `.cmd`.

---

## 4. Estructura del repositorio

```
Prueba d cld/
├── SistemaGestion.sln                    # Solución VS con 3 proyectos
├── SistemaGestion.csproj                 # WinForms (.NET 8 Windows)
├── *.cs                                  # ~60 formularios WinForms (raíz)
│   ├── Frm*.cs                           # Formularios (FrmFacturas, FrmClientes, etc.)
│   ├── DB*.cs                            # Acceso a datos (DB.cs principal + DB_*.cs parciales)
│   ├── Pdf*.cs                           # Generación de PDFs
│   └── *Service.cs                       # Servicios (alertas, backup, tasa cambio, etc.)
│
├── SistemaGestion.Api/                   # API Web (.NET 8 Minimal API)
│   ├── Program.cs                        # Configuración Kestrel, auth JWT, CORS, rate limiting
│   ├── Db.cs                             # Acceso SQLite (distinto al DB.cs de WinForms)
│   ├── Auth/                             # JWT tokens, certificados HTTPS auto-firmados
│   │   ├── CertService.cs
│   │   ├── TokenService.cs
│   │   └── SesionesActivas.cs
│   ├── Endpoints/                        # Minimal API endpoints
│   │   ├── AuthEndpoints.cs              # Login/logout
│   │   ├── ClientesEndpoints.cs          # CRUD clientes + IMPRESIÓN facturas/notas
│   │   ├── ComprasEndpoints.cs
│   │   ├── DashboardEndpoints.cs
│   │   ├── EgresosEndpoints.cs
│   │   ├── InventarioEndpoints.cs
│   │   ├── KardexEndpoints.cs
│   │   ├── NominaEndpoints.cs
│   │   ├── ProduccionEndpoints.cs
│   │   └── ReportesEndpoints.cs
│   ├── wwwroot/                          # Frontend del portal web
│   │   ├── index.html                    # SPA shell
│   │   ├── css/                          # Estilos
│   │   ├── js/app.js                     # Router SPA + auth
│   │   ├── js/pages/*.js                 # Un JS por módulo
│   │   └── pages/*.html                  # Templates HTML por módulo
│   └── appsettings.json
│
├── SistemaGestion.Api.Tests/             # Tests xUnit
│
├── digitalizador/                        # OCR Python (Anthropic Claude Vision)
│   ├── main.py                           # FastAPI server :8080
│   ├── extractor.py                      # Llama Claude Vision para extraer datos
│   ├── db_writer.py                      # Escribe en SQLite
│   └── requirements.txt
│
├── ehp-bot/                              # Bot de Telegram/WhatsApp (catálogo de productos)
│
├── Scripts/
│   ├── crear-datos-prueba.ps1            # Crea cliente y facturas de prueba via API
│   ├── iniciar-servicios-olimpo.cmd      # Inicia digitalizador + API en OLIMPO
│   ├── iniciar-servicios-servo.cmd       # Inicia API en SERVO (espera red primero)
│   ├── sg-env-olimpo.cmd                 # Variables de entorno OLIMPO
│   ├── sg-env-servo.cmd                  # Variables de entorno SERVO
│   ├── hardening-smb-olimpo.ps1          # Seguridad SMB
│   └── InsertarPrueba/                   # Tool consola para insertar productos de prueba
│
├── deploy/
│   ├── instalar-sistema.ps1              # Instalador automatizado (Admin u Oficina)
│   └── README-deploy.md
│
├── manual/                               # Documentación Markdown por módulo
└── .gitignore
```

---

## 5. Tecnologías

| Componente | Tecnología |
|---|---|
| App escritorio | C# WinForms .NET 8 |
| API web | ASP.NET 8 Minimal API |
| Frontend web | HTML + JS vanilla (SPA con router propio) |
| Base de datos | SQLite (archivo único compartido por red SMB) |
| Auth | JWT en cookie HttpOnly + bcrypt |
| HTTPS | Cert auto-firmado generado por CertService |
| OCR | Python + Claude Vision API (Anthropic) |
| Bot | Python (Telegram/WhatsApp) |
| CI | GitHub Actions (.github/workflows/ci.yml) |

---

## 6. Patrones de código importantes

### API (Minimal API)
- Endpoints definidos como métodos de extensión: `app.MapGet("/api/clientes", ...)` 
- Acceso a BD via `Db.Conn()` que retorna `SqliteConnection` (no EF Core, SQL directo)
- Auth: `TokenService.PuedeVerCxC(ctx.User)` para verificar permisos por claims
- Sin inyección de dependencias compleja — todo estático/simple

### Impresión de facturas (CRÍTICO — trabajo en curso)
- Archivo: `SistemaGestion.Api/Endpoints/ClientesEndpoints.cs`
- Endpoint: `GET /api/facturas/{id}/imprimir`
- Genera HTML completo con CSS inline para impresión
- **Concepto clave**: los formularios son preimpresos en papel carbonato. El sistema imprime SOLO los datos variables (nombre cliente, montos, items) posicionados para alinearse con los campos del formulario preimpreso
- Clase CSS `pp` (pre-printed): `visibility:hidden` en `@media print` — estos elementos se ven en pantalla pero NO se imprimen
- `@media print{.pp{visibility:hidden}*{border-color:transparent!important;background:none!important}}`

### Dos formatos de impresión vía API (portal web — formularios preimpresos):
1. **Factura Fiscal** (con IVA): 154mm × 305mm, recuadro interior 135mm ancho
   - `@page{size:154mm 305mm;margin:8mm 10mm}`
   - Elementos `pp`: logo empresa, dirección, N° factura, N° control, labels de campos, encabezados de tabla, forma de pago labels, firmas
   - Elementos que SÍ imprimen: fecha, nombre cliente, RIF, dirección, teléfono, items (cant/cod/desc/precio/total), totales numéricos, checkmarks de forma de pago
2. **Nota de Entrega** (sin IVA) vía API: 140mm × 210mm
   - `@page{size:140mm 210mm;margin:6mm}`
   - Misma lógica pp/datos

### Impresión desde WinForms (QuestPDF):
- **Factura Fiscal**: tamaño carta, diseño completo con empresa, tabla coloreada, totales desglosados
- **Nota de Entrega**: 140×210mm, diseño COMPLETO en papel normal (no preimpreso). Incluye encabezado empresa (EL HATO PASCHER C.A., RIF, dirección, teléfonos), datos del cliente (RIF, dirección, teléfono), tabla de productos con colores, totales. Archivo: `PdfService.cs` método `GenerarFactura()`.
- **Inventario**: impresión paginada desde FrmInventario y Reportes > Inventario (PrintDocument + PrintPreviewDialog)
- **Pronóstico de reorden**: impresión desde Reportes > Reorden MP

### Filas vacías para llenar tabla:
```csharp
int filasVaciasFact = Math.Max(0, 28 - nItems);  // Factura: max 28 filas
int filasVaciasNota = Math.Max(0, 18 - nItems);  // Nota: max 18 filas
```

### WinForms
- `FormBase.cs`: clase base con tema oscuro, menú lateral, buscador
- `UITheme.cs`: colores y estilos consistentes
- `UICards.cs`: tarjetas de dashboard
- DB acceso directo a SQLite con `Microsoft.Data.Sqlite`

---

## 7. Estado actual del desarrollo (v1.1 — Junio 2026)

### Funcionando
- App de escritorio completa en OLIMPO (WinForms)
- API web con todos los módulos en OLIMPO y SERVO
- Portal web SPA funcional
- Auth JWT + bcrypt + rate limiting + headers de seguridad
- Digitalizador OCR (Python + Claude Vision)
- Tests automatizados
- Deploy en las 3 máquinas
- Impresión desde WinForms (QuestPDF) y portal web (HTML + CSS @media print)

### Kardex dual (NUEVO v1.1)
- **Kardex A** (MATERIA_PRIMA): se alimenta automáticamente de compras (ENTRADA) y producción (SALIDA de insumos)
- **Kardex B** (PRODUCTO): se alimenta automáticamente de producción (ENTRADA de productos) y ventas (SALIDA)
- Archivos clave: `DB_Compras.cs`, `DB_RegistroProduccion.cs`, `DB.cs` (GuardarDetallesYKardex)
- Campos: TipoKardex, CostoUnitario, SaldoAnterior, NuevoSaldo en tabla MovimientosInventario
- NO requiere carga manual — todo se registra desde compras, producción y ventas

### Pronóstico de reorden (NUEVO v1.1)
- Archivo: `DB_PronosticoReorden.cs`
- UI: FrmReportes > tab "Reorden MP"
- Calcula consumo promedio diario, días de cobertura, cantidad sugerida de compra
- Estados: URGENTE (rojo), PRONTO (amarillo), OK (verde)
- API: `GET /api/inventario/pronostico-reorden?diasAnalisis=90&leadTime=7&coberturaMeta=30`

### API ComprasEndpoints actualizada (v1.1)
- `POST /api/compras`: guarda detalles + stock + Kardex A (MATERIA_PRIMA) con CostoUnitario
- `PUT /api/compras/{id}`: NUEVO — revierte stock anterior, re-inserta con Kardex
- `GET /api/inventario/pronostico-reorden`: NUEVO — pronóstico de reorden

### Nota de Entrega (actualizada v1.1)
- Desde WinForms: diseño COMPLETO en papel normal 140×210mm (QuestPDF), con encabezado empresa y datos del cliente
- Desde portal web: sigue usando lógica pp/datos sobre formulario preimpreso

### Manuales actualizados a v1.1
- `Manual-App-Escritorio-SistemaGestion.pdf` (24 páginas): +3 secciones (Kardex, Reorden, Impresión)
- `Manual-Tecnico-SistemaGestion.pdf` (25 páginas): +3 secciones (Kardex dual, Pronóstico, API actualizada)

### Mejoras v1.2 (Junio 2026)

#### Nómina
- Fórmula pago real: `PagoReal = TotalDevengado - DeduccionesReales`
- `DeduccionesReales = DescuentoFaltas + CuotaPrestamo + OtrasDeduccion`
- IVSS (4%) y RPVH (1%) son informativos, NO se descuentan del pago
- Porcentajes IVSS/RPVH ahora config-driven (tabla `ConfiguracionSistema`), no hardcodeados
- Descuento por falta basado en bono real del día (producción) o sueldo diario × tasa (otros)
- Cache de `CalcularBonoProduccion` al registrar N faltas del mismo día
- Validación: fecha de falta debe pertenecer a un período de nómina existente (retorna -2 si no)

#### Panel de Configuración Legal (admin)
- `FrmConfigLegal.cs` — WinForms con 5 secciones: Facturación, Nómina Empleado, Nómina Patronal, Valores Legales, Crédito
- API: `GET/PUT /api/config/legal` en `ConfigEndpoints.cs`
- `EnsureConfigLegal()` en ambas capas siembra valores default al iniciar
- Claves: IVA_Porcentaje, IVSS_Empleado, RPVH_Empleado, IVSS_Patronal, RPVH_Patronal, INCES_Patronal, UT_Bs, SalarioMinimo_Bs, DiasCredito_Default

#### Facturación
- Botón "+ Cliente" en formulario de factura (WinForms: `FrmFacturas.cs`, Web: `facturas.js`)
- CxC automática al facturar a crédito (usa `DiasCredito_Default` de ConfiguracionSistema)
- Verificación de morosidad antes de extender crédito (`VerificarMorosidadCliente`)

#### Alertas
- CxC próximas a vencer (3 días) en `AlertasService.cs` y `FrmAlertas.cs`
- Badge en tab "CxC Por Vencer" con grid de cuentas próximas

#### Planilla de producción
- WinForms (`PdfService_PlanillaProduccion.cs`): 12 filas vacías, columna Molde en blanco
- API (`GET /api/produccion/planilla`): 12 filas vacías HTML, columna Molde en blanco
- El operario escribe a mano los moldes en uso ese día (máx 12-14 diferentes)
- Eliminado el diálogo de selección de moldes en WinForms (ya no lista todos los moldes del catálogo)

#### Esquema BD
- Versión actual: 26
- Índices en InasistenciasEmpleado: `idx_inas_emp(IdEmpleado)`, `idx_inas_fecha(Fecha)`, `idx_inas_emp_fecha(IdEmpleado,Fecha)` UNIQUE

### Pendiente futuro
- Registrar la API como servicio de Windows (actualmente se ejecuta manual)
- Fine-tuning de impresión de factura fiscal sobre formularios preimpresos (calibrar posiciones)
- Costo Promedio Ponderado (CPP) completo en Kardex

---

## 8. Seguridad aplicada
- HTTPS con cert auto-firmado (renovable, 5 años)
- JWT en cookie HttpOnly, Secure, SameSite=Strict
- Bcrypt para passwords (migración desde SHA-256 legacy)
- Rate limiting en login (10/min) y endpoints IA
- Headers: CSP, X-Frame-Options DENY, HSTS, nosniff
- Restricción a red local (IPs privadas/loopback)
- SMB hardening en OLIMPO (script aplicado)
- Revocación de tokens por jti

---

## 9. Archivos clave para modificar

| Tarea | Archivo |
|---|---|
| Templates impresión (portal web) | `SistemaGestion.Api/Endpoints/ClientesEndpoints.cs` |
| Impresión WinForms (QuestPDF) | `PdfService.cs` (GenerarFactura, Imprimir) |
| Compras + Kardex A (WinForms) | `DB_Compras.cs` (InsertarCompraConDetallesYKardex, etc.) |
| Compras + Kardex A (API) | `SistemaGestion.Api/Endpoints/ComprasEndpoints.cs` |
| Producción + Kardex A/B | `DB_RegistroProduccion.cs` (RegistrarMovimientoTx) |
| Ventas + Kardex B | `DB.cs` (GuardarDetallesYKardex) |
| Pronóstico de reorden | `DB_PronosticoReorden.cs` + `FrmReportes.cs` (tab Reorden MP) |
| Inventario UI | `FrmInventario.cs` (incluye botón Imprimir) |
| Endpoints facturación | `SistemaGestion.Api/Endpoints/ClientesEndpoints.cs` |
| Frontend facturas | `SistemaGestion.Api/wwwroot/js/pages/facturas.js` |
| Auth/login | `SistemaGestion.Api/Endpoints/AuthEndpoints.cs` + `Auth/TokenService.cs` |
| Config API | `SistemaGestion.Api/Program.cs` |
| BD schema + migraciones | `DB.cs` (WinForms, MigrarEsquema) / `Db.cs` (API) |
| Config legal (admin) | `FrmConfigLegal.cs` (WinForms) / `ConfigEndpoints.cs` (API) |
| Alertas sistema | `AlertasService.cs` + `FrmAlertas.cs` |
| Planilla producción PDF | `PdfService_PlanillaProduccion.cs` (WinForms QuestPDF) |
| Planilla producción HTML | `ProduccionEndpoints.cs` → `GET /api/produccion/planilla` |
| CxC / Morosidad | `DB.cs` → `VerificarMorosidadCliente` / `ClientesEndpoints.cs` |
| Variables de entorno | `Scripts/sg-env-olimpo.cmd` / `Scripts/sg-env-servo.cmd` |

---

## 10. Errores conocidos y soluciones

| Error | Causa | Solución |
|---|---|---|
| `git index.lock` en sandbox Linux | Sandbox no puede borrar el lock | Ejecutar `Remove-Item "C:\Users\Toor\source\repos\Prueba d cld\.git\index.lock" -Force` en CHAOS |
| `dotnet publish -o` falla en PowerShell | PS interpreta `-o` como `-OutVariable` | Usar `--output` en vez de `-o` |
| GitHub rechaza push por archivo >100MB | Archivos binarios grandes en staging | Agregar a .gitignore y `git rm --cached` |
| HTTP 401 en `/imprimir` | No hay sesión activa | Login primero en `https://localhost:5443` |
| InsertarPrueba "BD no encontrada" en SERVO | No tiene la ruta de red en su lista | Usar `crear-datos-prueba.ps1` (via API) en su lugar |
| `Stop-Service SistemaGestionApi` falla | No está instalado como servicio | Cerrar ventana manualmente y re-ejecutar script .cmd |
| MSB3027 "file locked" en publish | App o API corriendo bloquea el DLL | Cerrar la app WinForms y/o detener la API antes de publicar |

---

## 11. Datos de la empresa (para facturas)
- **Razón social**: EL HATO PASCHER C.A.
- **RIF**: J-30571871-7
- **Dirección**: Ctra. Villa de Cura - San Francisco de Asís, Local N° S/N, Sector Los Colorados - Villa de Cura - Estado Aragua
- **Teléfonos**: 0244 386.10.13 - 386.33.04
- **Formulario preimpreso Factura Fiscal**: 154mm × 305mm (recuadro interior 135mm ancho) — usado en portal web
- **Nota de Entrega**: 140mm × 210mm — en WinForms se imprime diseño completo en papel normal (no preimpreso)

---

## 12. Reglas para el asistente

- Español neutro (NUNCA argentino)
- Comandos PowerShell SIEMPRE con ruta completa entre comillas si tiene espacios
- Publicar CADA proyecto .csproj por separado, NUNCA la solución con --output
- SERVO solo corre API, NUNCA publicar WinForms ahí
- Buscar la solución óptima, NO estar de acuerdo con el usuario por defecto
- Evaluar bien antes de dar múltiples soluciones: dar UNA, la mejor
- Si la conversación se alarga y se pierde información, AVISAR
- Verificar rutas y estructura antes de dar comandos — no asumir
- Repo OLIMPO: `C:\Users\PC\AppData\Local\SistemaGestion\repo`
- Repo SERVO: `C:\SistemaGestion` (raíz directa, sin subcarpetas adicionales)
