# HANDOFF — SistemaGestion (ERP de El Hato Pascher)

> Documento de traspaso para que cualquier agente de IA (Claude, Codex, Cursor, Gemini, etc.) o persona retome el proyecto sin el historial de chat.
> Respaldo preparado el 9 de noviembre de 2026.

| Dato | Valor |
|---|---|
| Repositorio | https://github.com/od1n/Prueba-d-cld (privado; espejo: `Prueba-d-cld-mirror`) |
| Rama | `master` |
| Último commit | `c4baffa` (7 nov 2026) — Portal: pantalla de Cuentas por Pagar y corrección de abonos |
| Carpeta de desarrollo | `C:\Users\Toor\source\repos\Prueba d cld` (equipo CHAOS, único que hace push) |
| Producción | OLIMPO (`C:\SistemaGestion`, IP 192.168.68.132) y SERVO (`C:\SistemaGestion`, solo API) |
| **Documento maestro** | `C:\Users\Toor\source\repos\Prueba d cld\PROYECTO_CONTEXTO.md` — LEER PRIMERO |

## 1. Qué es

ERP de escritorio para la fábrica de fustes de El Hato Pascher C.A. (Villa de Cura, Venezuela): ventas y facturación con IVA, inventario y kardex valorizado, producción diaria con bono por producto, nómina venezolana (LOTTT, IVSS), compras, egresos, cuentas por cobrar y por pagar, libros de IVA del SENIAT, ISLR, punto de equilibrio, reportes PDF, cámaras EZVIZ y un portal web en la red local. Incluye un digitalizador OCR de planillas y facturas.

## 2. Tecnología

- Aplicación de escritorio: .NET 8, Windows Forms, C#. Base SQLite (`Microsoft.Data.Sqlite`), PDF con QuestPDF, gráficas GDI+.
- API y portal web en red local: ASP.NET Core Minimal API (`SistemaGestion.Api`), HTTP 5050 → HTTPS 5443; páginas en `SistemaGestion.Api\wwwroot`.
- Pruebas de integración: `SistemaGestion.Api.Tests`.
- Digitalizador: Python FastAPI en OLIMPO, puerto 8080, visión con Ollama (`gemma4:31b-cloud`). Insights/narración con el Ollama local.
- Empaquetado: Inno Setup; actualizadores PowerShell en `deploy\`.

## 3. Mapa del repositorio

- `DB.cs` (núcleo `DatabaseService`, migraciones en `MigrarEsquema` y bloque defensivo en `Inicializar()`), `DB_*.cs` por módulo, `Frm*.cs` pantallas, `PdfService_*.cs` reportes.
- `SistemaGestion.Api\` API y portal; `digitalizador\` servicio OCR; `deploy\` actualizadores (`actualizar-olimpo.ps1`, `actualizar-servo.ps1`, `actualizar-bonos-productos.ps1`); `Scripts\` lanzadores `.cmd`.
- `docs\manuales`, `docs\sistema`, `docs\redes-sociales` (Estudio de Contenido, base del módulo de redes de Vínculo).
- Manuales PDF en la raíz: `Manual-App-Escritorio-SistemaGestion.pdf`, `Manual-Portal-Web-SistemaGestion.pdf`, `Manual-Tecnico-SistemaGestion.pdf`.
- Otros: `n8n-dashboard-financiero\` y `n8n-fixes\` (flujos del servidor n8n), `ehp-bot\`, `winforms-patterns\`.

## 4. Cómo compilar y ejecutar (CHAOS)

```powershell
Set-Location 'C:\Users\Toor\source\repos\Prueba d cld'
dotnet build 'C:\Users\Toor\source\repos\Prueba d cld\SistemaGestion.csproj'
dotnet run --project 'C:\Users\Toor\source\repos\Prueba d cld\SistemaGestion.csproj'
dotnet run --project 'C:\Users\Toor\source\repos\Prueba d cld\SistemaGestion.Api\SistemaGestion.Api.csproj'
dotnet test 'C:\Users\Toor\source\repos\Prueba d cld\SistemaGestion.Api.Tests\SistemaGestion.Api.Tests.csproj'
```

Usuario inicial de una base nueva: `admin` / `admin123` (cambiarlo de inmediato).

## 5. Despliegue

1. CHAOS: commit y push (lo hace Jacques).
2. OLIMPO: `pwsh -File 'C:\SistemaGestion\deploy\actualizar-olimpo.ps1'` (git pull → cierra API/digitalizador/app → publica API en `%LOCALAPPDATA%\SistemaGestion\publish\Api` y escritorio en `C:\SistemaGestion\publish\App` → relanza).
3. SERVO: `pwsh -File 'C:\SistemaGestion\deploy\actualizar-servo.ps1'` (solo API en `C:\SistemaGestion\publish\Api`). SERVO usa la base de OLIMPO por red: `\\192.168.68.132\SistemaGestion\sistema_gestion.db`.
4. Tras cambios de esquema, abrir una vez la app de escritorio en OLIMPO para que corra `Inicializar()`.
5. Si `dotnet publish` falla con NU1301 tras reiniciar, revisar DNS (`Resolve-DnsName api.nuget.org`) o reiniciar de nuevo.

## 6. Secretos (dónde viven, nunca sus valores)

- `SG_INTERNAL_TOKEN`: idéntico en 4 lugares (digitalizador OLIMPO, API OLIMPO, API SERVO y n8n). Vive en `sg-env.cmd` de cada carpeta de publicación (`%LOCALAPPDATA%\SistemaGestion\publish\Api\sg-env.cmd` en OLIMPO, `C:\SistemaGestion\publish\Api\sg-env.cmd` en SERVO). `dotnet publish` puede borrarlo: si aparece un error 403, recrearlo.
- Respaldos cifrados de la base: ventana "Gestión de Respaldos" (solo Admin), archivos `.db.enc`.
- **Pendiente de seguridad:** rotar la clave de Gemini y el token interno (quedaron expuestos en un chat en agosto de 2026).

## 7. Reglas técnicas aprendidas (no romperlas)

- Columnas agregadas con `ALTER TABLE`: leer SIEMPRE por nombre, nunca por posición en `SELECT *`.
- Arreglos de esquema para bases ya en la versión tope (29) van en `DatabaseService.Inicializar()`, nunca dentro de `MigrarEsquema` (retorna antes).
- `DetallesFactura.IdFactura` = `Facturas.rowid` (no `NumeroF`).
- En `.cmd`, `set VAR=valor && cmd` mete un espacio al final del valor: usar `set "VAR=valor"`.
- Kardex: cada movimiento de producción lleva `Referencia = "PROD-{fecha}"` para ser idempotente.
- Nómina: bonos por producto con historial fechado (`TarifaProductoProduccion/Venta`), conversión con tasa BCV; el valor del bono solo lo cambia Admin; el sueldo fijo solo Admin, Ventas, Compras o Contabilidad, con auditoría en `LogAuditoria`.

## 8. Estado y pendientes

Hecho recientemente: portal de Cuentas por Pagar y abonos (7 nov), análisis de ventas por producto, cédula/RIF única en clientes, producción 2026 con plantilla real de obreros, nómina por tipo de empleado (Obrero, Oficina, Taller, Administrativo, Temporal).

Pendiente:
- Egresos/compras: Fase 2 (compras sin precio + alerta al admin y WhatsApp), Fase 3 (endpoint web de egreso con la misma lógica del escritorio), Fase 4 (OCR en ambos).
- Pagar una cuenta por pagar no sincroniza de vuelta `Gastos.Estado/PagadoUSD`.
- Kardex: la producción no genera ENTRADAS valorizadas (salidas con costo 0); investigar el camino producción → kardex.
- Columnas de combos en grillas (`DataGridViewComboBoxColumn`) sin búsqueda al escribir.
- Clase `FrmTarifasBonoProduccion` obsoleta (sin acceso desde el menú).

## 9. Cómo retomarlo con otro agente

Indica al agente: "Lee `HANDOFF.md`, `AGENTS.md` y `PROYECTO_CONTEXTO.md` del repositorio `C:\Users\Toor\source\repos\Prueba d cld`, luego dime el estado y qué propones hacer con el pendiente X."
