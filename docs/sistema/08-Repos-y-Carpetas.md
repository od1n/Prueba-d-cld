# 08 — Repositorios y carpetas · qué es real y qué sobra

Diagnóstico del estado de carpetas en **CHAOS** (máquina de desarrollo) a agosto 2026, para responder "¿cuál es la real?" y ordenar el desorden de instancias duplicadas.

## 1. La carpeta REAL (fuente única de verdad)

**`C:\Users\Toor\source\repos\Prueba d cld`** — este es el repositorio activo del ERP y **el único desde el que se hace push**.

- Git remoto `origin` → `github.com/od1n/Prueba-d-cld.git` (+ un `mirror`). Es el que tiene el último commit.
- Es un **monorepo**: contiene TODO el sistema junto y ordenado:
  - Raíz: la app **WinForms** (98 `.cs`: formularios `Frm*`, datos `DB_*`, servicios `*Service`).
  - `SistemaGestion.Api/` — la **API web** real (Minimal API: `Auth/`, `Endpoints/`, `Db.cs`, `DbContenido.cs`, `wwwroot/`).
  - `SistemaGestion.Api.Tests/` — los **tests** reales (xUnit).
  - `digitalizador/` — el **OCR** en Python.
  - `deploy/`, `Scripts/` — despliegue y configuración.
  - `n8n-workflows/` — workflows del bot / contenido.
  - `Descifrar/` — herramienta de respaldos.
  - `docs/` — documentación (incluye `docs/redes-sociales/` y `docs/sistema/`).
  - `manual/`, `recursos grafiacos/`, `winforms-patterns/`, `ehp-bot/`.

> Conclusión: **el sistema YA está consolidado** en `Prueba d cld`. No hay que "unir" el ERP: WinForms + API + Tests + OCR + scripts + deploy + docs ya viven juntos aquí.

## 2. Carpetas que SOBRAN en CHAOS (restos a limpiar)

| Carpeta | Qué es | Veredicto |
|---|---|---|
| `C:\Users\Toor\source\repos\SistemaGestion.Api` | **Plantilla vieja** de ASP.NET (contiene `WeatherForecastController.cs`, `Controllers/`). No es git. mtime abril 2026. | **Resto muerto** — la API real está dentro de `Prueba d cld`. Eliminar. |
| `C:\Users\Toor\source\repos\SistemaGestion.Tests` | Tests sueltos antiguos. No es git. mtime abril 2026. | **Resto muerto** — los tests reales son `Prueba d cld\SistemaGestion.Api.Tests`. Eliminar. |
| `C:\SistemaGestion\` (en CHAOS) | Solo contiene `publish\Api` (salida de compilación vieja, mtime junio 2026). | **Resto de deploy** — en CHAOS no se despliega; `C:\SistemaGestion` es la ruta de OLIMPO/SERVO, no de desarrollo. Eliminar en CHAOS. |
| `Prueba d cld\_gitlock_trash\` | Carpeta creada durante los líos de `.git\index.lock`. | Revisar y **eliminar** si está vacía / sin nada útil. |
| `Prueba d cld\publish-api-temp\` | Salida temporal de publish. | **Resto** — eliminar (se regenera). |
| `Prueba d cld\bin\`, `\obj\` | Artefactos de compilación. | Normales; se ignoran por `.gitignore` (no versionar). |

> Nota: `device_bash` del puente **no puede borrar** (permiso denegado). La limpieza se hace en Windows (PowerShell) por el usuario, o moviendo a una carpeta `_a_eliminar\` para revisar antes de borrar.

## 3. Proyecto SEPARADO (correctamente aparte)

**`C:\Users\Toor\source\repos\ehp web`** — la web pública de EHP Saddles (repo propio `od1n/ehp-web`). Es otro producto (sitio estático de marketing), con su propio deploy. **No se fusiona** con el ERP. Ver [07-EHP-Web](07-EHP-Web.md). (Tiene un `SistemaGestion.db` de 0 bytes suelto que se puede borrar por no pertenecer ahí.)

## 4. Otras carpetas en `repos`
- `claude-skills`, `MasterClaude` — utilidades de trabajo con Claude, ajenas al ERP y a la web. Se dejan como están.

## 5. Estructura ordenada propuesta

Opción recomendada (mínimo riesgo): **dejar el monorepo como está** y solo limpiar los restos.

```
C:\Users\Toor\source\repos\
├── Prueba d cld\      ← ERP completo (WinForms + API + Tests + OCR + docs)   [ÚNICO, real]
├── ehp web\           ← Web pública EHP (repo aparte)                          [separado, ok]
├── claude-skills\     ← utilidades Claude
└── MasterClaude\      ← utilidades Claude
# (se eliminan: SistemaGestion.Api, SistemaGestion.Tests, y en C:\  la carpeta C:\SistemaGestion)
```

Opción cosmética (más riesgo, requiere ajustar rutas): renombrar `Prueba d cld` a algo como `SistemaGestion`. **No recomendado sin cuidado**: el nombre `Prueba d cld` está cableado en la conexión del puente, en scripts y en la costumbre de trabajo; renombrar rompe rutas y referencias. Si se quiere, se hace aparte y con checklist.

## 6. Higiene de seguridad detectada
- El remoto `origin` de `Prueba d cld` tiene el **token de GitHub embebido en la URL** (`https://<token>@github.com/...`), que queda en texto plano en `.git\config`. Recomendado: quitarlo y usar el **Administrador de credenciales de Windows** o un `credential.helper`, para no exponer el token. (Coincide con la regla del proyecto: no guardar secretos en el repo.)

## 7. Plan de limpieza (a confirmar antes de ejecutar)
Nada de esto se ejecuta sin tu OK. Todo corre en **CHAOS**, en PowerShell:

```powershell
# 1) Revisar que no haya nada útil en los restos (por si acaso)
Get-ChildItem "C:\Users\Toor\source\repos\SistemaGestion.Api" -Recurse | Measure-Object
Get-ChildItem "C:\Users\Toor\source\repos\SistemaGestion.Tests" -Recurse | Measure-Object
Get-ChildItem "C:\SistemaGestion" -Recurse | Measure-Object

# 2) Mover a una carpeta de cuarentena (más seguro que borrar directo)
$q = "C:\Users\Toor\_a_eliminar"; New-Item -ItemType Directory -Force $q | Out-Null
Move-Item "C:\Users\Toor\source\repos\SistemaGestion.Api"   "$q\SistemaGestion.Api-template"
Move-Item "C:\Users\Toor\source\repos\SistemaGestion.Tests" "$q\SistemaGestion.Tests-viejo"
Move-Item "C:\SistemaGestion"                                "$q\C-SistemaGestion-publish"

# 3) Restos dentro del repo (regenerables)
Remove-Item "C:\Users\Toor\source\repos\Prueba d cld\publish-api-temp" -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item "C:\Users\Toor\source\repos\Prueba d cld\_gitlock_trash"   -Recurse -Force -ErrorAction SilentlyContinue

# 4) Tras confirmar que todo sigue funcionando, borrar la cuarentena
# Remove-Item "C:\Users\Toor\_a_eliminar" -Recurse -Force
```

> Ojo: `C:\SistemaGestion` en **OLIMPO** y **SERVO** SÍ es la carpeta real de producción — esta limpieza es **solo para CHAOS**, donde esa ruta es un resto.
