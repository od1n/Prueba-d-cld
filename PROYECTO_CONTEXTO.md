# SistemaGestion — Contexto de Proyecto para Sesiones de Desarrollo

## Prioridades de comunicación
- Español neutro (NO argentino)
- Siempre entregar comandos PowerShell con rutas completas
- No estar de acuerdo por defecto: buscar el resultado óptimo, cuestionar si algo no tiene sentido
- Investigar causas raíz antes de hacer cambios, no ensayo y error
- Dar UNA solución bien pensada, no múltiples opciones que no funcionan

---

## 1. ¿Qué es SistemaGestion?

ERP para una fábrica venezolana de plásticos (EL HATO PASCHER C.A., RIF J-30571871-7, Villa de Cura, Aragua).

Dos interfaces:
- **App de escritorio** (WinForms .NET 8, solo en OLIMPO): gestión completa, acceso directo a SQLite
- **Portal web** (ASP.NET 8 Minimal API + HTML/JS): acceso desde cualquier equipo en la LAN, impresión de facturas

Módulos: Dashboard, Facturación, Clientes, Inventario, Kardex, Producción, Nómina, Egresos/Compras, Reportes, Digitalizador OCR, Ayuda IA.

Credenciales portal: admin / admin1234 (admin), PC / catiremia (usuario normal)

---

## 2. Máquinas y rutas

### CHAOS (desarrollo) — donde se edita código y se hace push
- **SO**: Windows, usuario `Toor`
- **Repo**: `C:\Users\Toor\source\repos\Prueba d cld`
- **Git remote**: `https://github.com/od1n/Prueba-d-cld.git`, rama `master`
- **IMPORTANTE**: TODO el desarrollo se hace aquí. Push SOLO desde CHAOS. Las otras máquinas solo hacen pull.
- **No tiene impresora ni base de datos de producción**

### OLIMPO (producción — admin) — IP: 192.168.68.132
- **SO**: Windows, usuario `PC`
- **Repo**: `C:\SistemaGestion` (aquí se hace `git pull`, NO push)
- **BD SQLite (ÚNICA)**: `C:\SistemaGestion\sistema_gestion.db`
- **Share SMB**: `\\192.168.68.132\SistemaGestion\` → apunta a `C:\SistemaGestion\` (VERIFICAR con `Get-SmbShare` si hay dudas)
- **Env var `SG_DB_PATH` (Machine scope)**: `C:\SistemaGestion\sistema_gestion.db` (ruta LOCAL, no UNC)
- **Publish WinForms**: `C:\Users\PC\AppData\Local\SistemaGestion\publish\App`
  - Comando: `dotnet publish SistemaGestion.csproj -c Release -r win-x64 --self-contained -o "C:\Users\PC\AppData\Local\SistemaGestion\publish\App"`
- **Publish API** (OLIMPO también puede correr la API): `C:\SistemaGestion\publish\Api`
  - Comando: `dotnet publish SistemaGestion.Api -c Release -o C:\SistemaGestion\publish\Api`
- **Tablas config**: `ConfigSistema` (legacy) y `ConfiguracionSistema` (v16+, parámetros legales IVA/IVSS/etc.)
- **Respaldos automáticos**: `C:\Users\PC\AppData\Roaming\SistemaGestion\Respaldos\` (archivos .db.enc cifrados con AES-256, clave DPAPI en sg.key)
- **Digitalizador Python**: `C:\SistemaGestion\digitalizador`
- **Funciones**: BD maestra, app escritorio, API, digitalizador OCR

### SERVO (oficina — usuario) — conecta a OLIMPO por red
- **Repo**: `C:\SistemaGestion` (aquí se hace `git pull`, NO push)
- **Publish API**: `C:\SistemaGestion\publish\Api`
  - Comando: `dotnet publish SistemaGestion.Api -c Release -o C:\SistemaGestion\publish\Api`
- **BD**: accede a OLIMPO via `\\192.168.68.132\SistemaGestion\sistema_gestion.db` (red SMB)
- **Env var `SG_DB_PATH` (Machine scope)**: `\\192.168.68.132\SistemaGestion\sistema_gestion.db` (ruta UNC porque la BD está en OLIMPO)
- **Funciones**: SOLO API web (NO WinForms). Impresión de facturas
- **Puertos**: HTTP 5050 (redirige a HTTPS), HTTPS 5443
- **Impresora**: EPSON L1250
- **IMPORTANTE**: NUNCA publicar WinForms en SERVO — solo la API

---

## 3. Flujo de deploy

```powershell
# 1. CHAOS — commit y push (ÚNICO lugar donde se hace push)
cd "C:\Users\Toor\source\repos\Prueba d cld"
git add -A
git commit -m "descripción"
git push origin master

# 2. OLIMPO — pull y publish
cd C:\SistemaGestion
git pull origin master
# WinForms (solo OLIMPO):
dotnet publish SistemaGestion.csproj -c Release -r win-x64 --self-contained -o "C:\Users\PC\AppData\Local\SistemaGestion\publish\App"
# API (si corre en OLIMPO):
dotnet publish SistemaGestion.Api -c Release -o C:\SistemaGestion\publish\Api

# 3. SERVO — pull y publish SOLO API
cd C:\SistemaGestion
git pull origin master
dotnet publish SistemaGestion.Api -c Release -o C:\SistemaGestion\publish\Api
# Reiniciar la API: cd C:\SistemaGestion\publish\Api && dotnet SistemaGestion.Api.dll
```

**CRÍTICO**: `dotnet build` NO es suficiente — la API ejecuta desde la carpeta publish. Siempre `dotnet publish`.

---

## 4. Base de datos — REGLA FUNDAMENTAL

**SOLO DEBE EXISTIR UNA BD**: `C:\SistemaGestion\sistema_gestion.db` en OLIMPO.

- OLIMPO accede directamente (ruta local)
- SERVO accede por red SMB (`\\192.168.68.132\SistemaGestion\sistema_gestion.db`)
- Si aparece una BD en `AppData\Roaming\SistemaGestion\` o `AppData\Local\SistemaGestion\` es un ERROR — eliminarla
- El share SMB `SistemaGestion` DEBE apuntar a `C:\SistemaGestion\` — verificar con `Get-SmbShare -Name SistemaGestion`
- La tabla `ConfiguracionSistema` debe existir (parámetros legales). Si falta: `CREATE TABLE IF NOT EXISTS ConfiguracionSistema (Clave TEXT PRIMARY KEY, Valor TEXT NOT NULL);`

### Respaldos
- Ubicación: `C:\Users\PC\AppData\Roaming\SistemaGestion\Respaldos\`
- Formato: archivos `.db.enc` (AES-256-CBC, IV en primeros 16 bytes)
- Clave: `sg.key` en `%AppData%\SistemaGestion\` (cifrada con DPAPI, DataProtectionScope.CurrentUser del usuario PC)
- Código de cifrado/descifrado: `SecurityService.cs` → `CifrarRespaldo()` / `DescifrarRespaldo()`
- Herramienta de descifrado: proyecto `Descifrar/` en el repo (standalone, requiere `System.Security.Cryptography.ProtectedData` NuGet)
- **NOTA**: el WinForms NO tiene botón de restaurar respaldo. Solo lista y crea. La restauración se hace manual o con la herramienta Descifrar.

---

## 5. Estructura del repositorio

```
Prueba d cld/
├── SistemaGestion.sln
├── SistemaGestion.csproj                 # WinForms (.NET 8 Windows)
├── *.cs                                  # ~60 formularios WinForms (raíz)
│   ├── Frm*.cs                           # Formularios
│   ├── DB*.cs                            # Acceso a datos
│   ├── Pdf*.cs                           # Generación de PDFs (QuestPDF)
│   ├── SecurityService.cs                # DPAPI, AES cifrado respaldos, rutas BD
│   ├── BackupService.cs                  # Respaldo automático diario
│   └── *Service.cs                       # Otros servicios
│
├── SistemaGestion.Api/                   # API Web (.NET 8 Minimal API)
│   ├── Program.cs                        # Kestrel, auth JWT, CORS, rate limiting
│   ├── Db.cs                             # Acceso SQLite (distinto al DB.cs de WinForms)
│   ├── Auth/                             # JWT, cert HTTPS, sesiones
│   ├── Endpoints/                        # Minimal API endpoints
│   └── wwwroot/                          # Frontend SPA (HTML + JS vanilla)
│
├── SistemaGestion.Api.Tests/             # Tests xUnit
├── Descifrar/                            # Herramienta para descifrar respaldos (standalone)
├── digitalizador/                        # OCR Python (Anthropic Claude Vision)
├── Scripts/                              # Scripts de deploy y configuración
├── deploy/                               # Instalador automatizado
└── manual/                               # Documentación
```

**IMPORTANTE**: `SistemaGestion.csproj` excluye `Descifrar/**` para que no compile los archivos del subproyecto.

---

## 6. Tecnologías

| Componente | Tecnología |
|---|---|
| App escritorio | C# WinForms .NET 8 |
| API web | ASP.NET 8 Minimal API |
| Frontend web | HTML + JS vanilla (SPA con router propio) |
| Base de datos | SQLite (archivo único compartido por red SMB) |
| Auth API | JWT en cookie HttpOnly, PBKDF2-SHA256 (600k iter) con sal por usuario |
| Auth WinForms | PBKDF2-SHA256 (mismos parámetros, formato `v2:SALT_HEX:HASH_HEX`) |
| HTTPS | Cert auto-firmado generado por CertService |
| OCR | Python + Claude Vision API (Anthropic) |
| CI | GitHub Actions |

---

## 7. Patrones de código importantes

### IVA en facturas
- Solo dos valores posibles: el porcentaje del sistema (`ConfiguracionSistema.IVA_Porcentaje`, default 16%) o 0% (Nota de Entrega)
- Frontend: `<select>` con dos opciones, cargado dinámicamente desde `/api/config/legal`
- WinForms: `ComboBox` DropDownList con dos opciones, valor leído de `DatabaseService.ObtenerConfig("IVA_Porcentaje", "16")`
- **CUIDADO con JavaScript**: `parseFloat("0") || 16` devuelve 16 porque 0 es falsy. Usar checks explícitos de null/undefined/empty.

### Vista previa de factura
- **Web** (`facturas.js`): modal overlay antes de guardar con `mostrarVistaPrevia()` → `confirmarGuardarFactura()`
- **WinForms** (`FrmFacturas.cs`): `MessageBox` con resumen antes de `Resultado = new Factura {...}`

### Impresión
- Portal web: HTML + CSS `@media print`, formularios preimpresos (clase `pp` = no imprime)
- WinForms: QuestPDF, diseño completo en papel normal

### API
- Endpoints como métodos de extensión: `app.MapGet("/api/...", ...)`
- BD via `Db.Conn()` (SQL directo, no EF Core)
- Auth: `TokenService.PuedeVerCxC(ctx.User)` para permisos

---

## 8. Seguridad
- HTTPS con cert auto-firmado (5 años)
- JWT en cookie HttpOnly, Secure, SameSite=Strict
- PBKDF2-SHA256 600k iteraciones para passwords (migración automática desde SHA-256 legacy)
- Rate limiting en login (10/min)
- Headers: CSP, X-Frame-Options DENY, HSTS, nosniff
- Restricción a red local (IPs privadas/loopback)
- Revocación de tokens por jti

---

## 9. Archivos clave

| Tarea | Archivo |
|---|---|
| Facturas (WinForms) | `FrmFacturas.cs` |
| Facturas (web frontend) | `SistemaGestion.Api/wwwroot/js/pages/facturas.js` |
| Facturas (API backend) | `SistemaGestion.Api/Endpoints/ClientesEndpoints.cs` |
| Compras + Kardex | `DB_Compras.cs` / `ComprasEndpoints.cs` |
| Config legal | `FrmConfigLegal.cs` / `ConfigEndpoints.cs` |
| Auth/login | `AuthEndpoints.cs` + `Auth/TokenService.cs` |
| BD schema + migraciones | `DB.cs` (WinForms) / `Db.cs` + `DB_Deploy.cs` (API) |
| Respaldos | `BackupService.cs` + `SecurityService.cs` |
| Impresión QuestPDF | `PdfService.cs` |
| Impresión portal web | `ClientesEndpoints.cs` |
| Variables entorno | `Scripts/sg-env-olimpo.cmd` / `sg-env-servo.cmd` |

---

## 10. Errores conocidos y soluciones

| Error | Causa | Solución |
|---|---|---|
| Login falla pero IntentosHoy=0 | API leyendo otra BD (share SMB apunta a carpeta incorrecta) | Verificar `Get-SmbShare -Name SistemaGestion` apunta a `C:\SistemaGestion\` |
| `no such table: ConfiguracionSistema` | BD no tiene la tabla v16+ | `CREATE TABLE IF NOT EXISTS ConfiguracionSistema (...)` |
| `parseFloat("0") \|\| 16` = 16 | JavaScript: 0 es falsy | Usar check explícito de null/undefined/empty |
| WinForms no refleja cambios | Se hizo `dotnet build` en vez de `dotnet publish` | Siempre `dotnet publish` con `--self-contained -r win-x64` |
| BD vacía / datos desaparecidos | Múltiples copias de BD en distintas rutas | Verificar SG_DB_PATH y share SMB. Solo una BD en `C:\SistemaGestion\` |
| `Padding is invalid` al descifrar backup | Posible incompatibilidad de clave DPAPI | Descifrar DEBE correr como usuario PC en OLIMPO (DPAPI CurrentUser) |
| Push desde máquina incorrecta | `fatal: not a git repository` | Push SOLO desde CHAOS. OLIMPO y SERVO solo hacen pull. |

---

## 11. Datos de la empresa
- **Razón social**: EL HATO PASCHER C.A.
- **RIF**: J-30571871-7
- **Dirección**: Ctra. Villa de Cura - San Francisco de Asís, Local N° S/N, Sector Los Colorados - Villa de Cura - Estado Aragua
- **Teléfonos**: 0244 386.10.13 - 386.33.04

---

## 12. PROBLEMA ACTIVO — Restaurar BD de producción (Junio 18, 2026)

### Qué pasó
El share SMB `SistemaGestion` en OLIMPO apuntaba a `C:\Users\PC\AppData\Roaming\SistemaGestion\` (donde estaba la BD real con todos los datos). Al corregir la configuración para que apunte a `C:\SistemaGestion\`, la BD en esa ruta estaba vacía (solo tenía estructura, sin clientes/productos/facturas).

### Estado actual
- `C:\SistemaGestion\sistema_gestion.db` — BD vacía (0 clientes, 0 productos, 1 usuario)
- La BD original con datos ya no existe como archivo .db
- Hay respaldos encriptados en `C:\Users\PC\AppData\Roaming\SistemaGestion\Respaldos\` (12 archivos .db.enc, el más reciente del 18/06/2026 00:04, 614 KB)
- Clave de descifrado: `C:\Users\PC\AppData\Roaming\SistemaGestion\sg.key` (DPAPI)

### Lo que se intentó (TODO FALLÓ con "Padding is invalid and cannot be removed")
1. Herramienta standalone `Descifrar/` (proyecto .NET en el repo) — reimplementa AES-256-CBC → padding error
2. Herramienta standalone usando ProjectReference a SistemaGestion.csproj → conflictos de compilación
3. Botón "Restaurar" agregado a `FrmRespaldos.cs` que llama `SecurityService.DescifrarRespaldo()` directamente → MISMO padding error

**Conclusión**: el error NO es de reimplementación. `SecurityService.DescifrarRespaldo()` ejecutándose dentro del propio WinForms (mismo usuario PC, mismo DPAPI, mismo código) también falla. Esto indica que:
- La clave AES en `sg.key` fue regenerada en algún momento DESPUÉS de crear los respaldos, O
- Los archivos .enc están corruptos, O
- Hay un problema con DPAPI (la clave se descifra a 256 bits pero el valor podría ser incorrecto si el perfil Windows cambió)

### Lo que falta investigar
1. **Verificar integridad de los .enc**: usar el botón "Verificar" en FrmRespaldos — si los SHA256 coinciden, los archivos no están corruptos
2. **Verificar si la clave fue regenerada**: buscar en `SecurityService.cs` cuándo se genera `sg.key` (método `GenerarClave` o similar). Si se genera al no encontrar el archivo, podría haberse regenerado accidentalmente
3. **Probar respaldos más antiguos**: si TODOS fallan, el problema es la clave. Si solo los recientes fallan, la clave se regeneró en cierta fecha
4. **Buscar BD en otros lugares**: quizás hay una copia no cifrada en algún respaldo manual, en la papelera de reciclaje, o en Shadow Copies de Windows (`vssadmin list shadows`)
5. **DPAPI recovery**: si la contraseña del usuario Windows PC cambió, DPAPI puede no descifrar correctamente. Verificar con `dpapi::masterkey` o similar

### Si no se puede recuperar
- La BD tiene estructura correcta (tablas creadas por migraciones). Solo faltan los DATOS.
- Habría que reingresar clientes, productos e inventario manualmente
- Las facturas históricas se perderían

### Pendientes adicionales (después de restaurar BD)
- Vista previa de factura en WinForms: ya implementada en código (`FrmFacturas.cs`), falta probar
- El botón Restaurar ya está en `FrmRespaldos.cs` — funcionará cuando se resuelva el problema de la clave
- Recrear usuario oficina01 (ofi001, rol Ventas)
- Reiniciar API en SERVO y verificar login + datos

---

## 13. Reglas para el asistente

- Español neutro (NUNCA argentino)
- Push SOLO desde CHAOS. OLIMPO y SERVO solo hacen pull.
- Publicar CADA proyecto .csproj por separado, NUNCA la solución con --output
- SERVO solo corre API, NUNCA publicar WinForms ahí
- Dar UNA solución probada, no múltiples intentos
- Verificar rutas y estructura antes de dar comandos — no asumir
- Si no se puede verificar que algo funciona, decirlo claramente
- NUNCA debe haber más de una BD. Si aparece alguna en AppData o en otra ruta, es un error.
