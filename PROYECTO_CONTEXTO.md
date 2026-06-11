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
- **Repo**: `C:\Users\PC\AppData\Local\SistemaGestion\repo`
- **Publish API**: `C:\Users\PC\AppData\Local\SistemaGestion\publish\Api`
- **BD SQLite**: `C:\Users\PC\AppData\Roaming\SistemaGestion\sistema_gestion.db`
- **Env vars**: `C:\Users\PC\AppData\Local\SistemaGestion\publish\Api\sg-env.cmd`
- **Digitalizador Python**: `C:\SistemaGestion\digitalizador`
- **Funciones**: BD maestra, app escritorio, API, digitalizador OCR
- **Script inicio**: `C:\Users\PC\AppData\Local\SistemaGestion\repo\Scripts\iniciar-servicios-olimpo.cmd`

### SERVO (oficina — usuario) — conecta a OLIMPO por red
- **Repo**: `C:\SistemaGestion\repo`
- **Publish API**: `C:\SistemaGestion\publish\Api`
- **BD**: `\\192.168.68.132\SistemaGestion\sistema_gestion.db` (red, misma BD de OLIMPO)
- **Digitalizador**: usa el de OLIMPO via `http://192.168.68.132:8080`
- **Env vars**: `C:\SistemaGestion\publish\Api\sg-env.cmd`
- **Funciones**: solo API web, impresión de facturas sobre formularios preimpresos
- **Script inicio**: `C:\SistemaGestion\repo\Scripts\iniciar-servicios-servo.cmd`
- **Impresora**: EPSON L1250 (soporta tamaños de papel personalizados en SERVO)

---

## 3. Flujo de deploy

```powershell
# 1. En CHAOS — commit y push
cd "C:\Users\Toor\source\repos\Prueba d cld"
git add <archivos>
git commit -m "descripción"
git push origin develop

# 2. En OLIMPO — pull, publish, reiniciar
cd C:\Users\PC\AppData\Local\SistemaGestion\repo
git pull origin develop
dotnet publish SistemaGestion.Api\SistemaGestion.Api.csproj --configuration Release --output C:\Users\PC\AppData\Local\SistemaGestion\publish\Api
# Reiniciar: cerrar ventana de la API y ejecutar:
C:\Users\PC\AppData\Local\SistemaGestion\repo\Scripts\iniciar-servicios-olimpo.cmd

# 3. En SERVO — pull, publish, reiniciar
cd C:\SistemaGestion\repo
git pull origin develop
dotnet publish SistemaGestion.Api\SistemaGestion.Api.csproj --configuration Release --output C:\SistemaGestion\publish\Api
# Reiniciar: cerrar ventana de la API y ejecutar:
C:\SistemaGestion\repo\Scripts\iniciar-servicios-servo.cmd
```

**Nota importante**: usar `--output` (no `-o`) en dotnet publish — PowerShell interpreta `-o` como parámetro propio.

**Nota**: el servicio Windows `SistemaGestionApi` NO está instalado actualmente. La API se ejecuta manualmente via los scripts `.cmd`. Si se necesita como servicio, usar `instalar-sistema.ps1 -ComoServicio`.

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

### Dos formatos de impresión:
1. **Factura Fiscal** (con IVA): 154mm × 305mm, recuadro interior 135mm ancho
   - `@page{size:154mm 305mm;margin:8mm 10mm}`
   - Elementos `pp`: logo empresa, dirección, N° factura, N° control, labels de campos, encabezados de tabla, forma de pago labels, firmas
   - Elementos que SÍ imprimen: fecha, nombre cliente, RIF, dirección, teléfono, items (cant/cod/desc/precio/total), totales numéricos, checkmarks de forma de pago
2. **Nota de Entrega** (sin IVA): 140mm × 210mm, contenido 128mm
   - `@page{size:140mm 210mm;margin:6mm}`
   - Misma lógica pp/datos

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

## 7. Estado actual del desarrollo

### Funcionando
- App de escritorio completa en OLIMPO
- API web con todos los módulos (dashboard, facturación, clientes, inventario, kardex, producción, nómina, egresos, compras, reportes)
- Portal web SPA funcional
- Auth JWT + bcrypt + rate limiting + headers de seguridad
- Digitalizador OCR (Python + Claude Vision)
- Tests automatizados
- Deploy automatizado en OLIMPO y SERVO
- Impresión: templates generan HTML correcto, elementos preimpresos ocultos con `pp`

### EN CURSO — Impresión sobre formularios preimpresos
**Estado**: los templates generan el HTML correcto con `visibility:hidden` para elementos preimpresos. En SERVO los tamaños de papel personalizados SÍ aparecen en Chrome.

**Problema pendiente**: la factura fiscal se imprime "muy larga" — el contenido no cuadra verticalmente con el formulario preimpreso de 154mm × 305mm. 

**Configuración de impresión verificada**:
- Tamaño de papel: Factura Fiscal (154×305mm) — NO es Carta
- Escala: 100%
- Márgenes: por defecto

**Posibles causas a investigar**:
- Las 28 filas vacías de relleno (height:5mm cada una = 140mm) más flex:1 podrían estar forzando la tabla más alta de lo necesario
- Los márgenes de `@page` (8mm arriba/abajo, 10mm lados) podrían no coincidir con los márgenes físicos del formulario preimpreso
- La altura del encabezado empresa (div.empresa con `pp`) ocupa espacio via `visibility:hidden` — si el formulario preimpreso tiene un encabezado de distinto alto, todo se desplaza
- La posición vertical exacta de cada sección (fecha, cliente, items, totales) necesita calibrarse contra el formulario físico real

**Próximo paso**: medir en el formulario preimpreso la posición exacta (en mm desde el borde superior) de cada zona (fecha, datos cliente, inicio tabla items, inicio totales/firmas) y ajustar el CSS para que coincida.

### Pendiente futuro
- Registrar la API como servicio de Windows (actualmente se ejecuta manual)
- Fine-tuning de posiciones de impresión contra formularios preimpresos reales
- Nota de entrega: misma calibración que factura fiscal

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
| Templates de impresión | `SistemaGestion.Api/Endpoints/ClientesEndpoints.cs` (líneas ~555-743) |
| Endpoints de facturación | `SistemaGestion.Api/Endpoints/ClientesEndpoints.cs` |
| Frontend facturas | `SistemaGestion.Api/wwwroot/js/pages/facturas.js` |
| Auth/login | `SistemaGestion.Api/Endpoints/AuthEndpoints.cs` + `Auth/TokenService.cs` |
| Config API | `SistemaGestion.Api/Program.cs` |
| BD schema | `SistemaGestion.Api/Db.cs` (API) o `DB.cs` (WinForms) |
| Variables de entorno | `Scripts/sg-env-olimpo.cmd` / `Scripts/sg-env-servo.cmd` |
| Datos de prueba | `Scripts/crear-datos-prueba.ps1` |

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
| MSB3027 "file locked" en publish | API corriendo bloquea el DLL | Detener la API antes de publicar |

---

## 11. Datos de la empresa (para facturas)
- **Razón social**: EL HATO PASCHER C.A.
- **RIF**: J-30571871-7
- **Dirección**: Ctra. Villa de Cura - San Francisco de Asís, Local N° S/N, Sector Los Colorados - Villa de Cura - Estado Aragua
- **Teléfonos**: 0244 386.10.13 - 386.33.04
- **Formulario preimpreso Factura Fiscal**: 154mm × 305mm (recuadro interior 135mm ancho)
- **Formulario preimpreso Nota de Entrega**: 140mm × 210mm (contenido 128mm ancho)
