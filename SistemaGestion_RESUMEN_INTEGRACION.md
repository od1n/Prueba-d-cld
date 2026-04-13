# Sistema de Gestion Empresarial — Resumen de Integracion
## Para uso en conversaciones futuras con Claude

---

## 1. DATOS DEL PROYECTO

- **Nombre:** SistemaGestion
- **Tipo:** Aplicacion de escritorio Windows Forms
- **Framework:** .NET 8 (net8.0-windows)
- **Lenguaje:** C#
- **Base de datos:** SQLite via Microsoft.Data.Sqlite 8.0.0
- **PDF:** QuestPDF 2024.3.4 (licencia Community)
- **Graficas:** GDI+ puro (System.Drawing) — sin dependencias externas
- **Directorio local del usuario:** `C:\Users\Toor\source\repos\Prueba d cld\`
- **Outputs generados por Claude:** `/mnt/user-data/outputs/SistemaGestion/`

### Compilar y ejecutar
```bash
cd "C:\Users\Toor\source\repos\Prueba d cld"
dotnet build
dotnet run
```

### Credenciales por defecto
- Usuario: `admin` / Contrasena: `admin123`
- Se crea automaticamente si no existe ningun usuario

---

## 2. ESTRUCTURA DE ARCHIVOS

```
SistemaGestion/
├── SistemaGestion.csproj
├── Program.cs                          # Arranque: Security -> DB -> Login -> Backup -> Run
├── MenuPrincipal.cs                    # Menu principal con 5 botones + barra de sesion
│
├── Models/
│   └── DB.cs                          # ~3469 lineas — TODOS los modelos + DatabaseService
│
├── Services/
│   ├── SecurityService.cs             # DPAPI, AES-256, NTFS ACL, rutas AppData
│   ├── BackupService.cs               # Respaldo automatico diario cifrado AES
│   └── PdfService.cs                  # ~1260 lineas — todos los reportes QuestPDF
│
└── Forms/
    ├── FrmLogin.cs                     # Login SHA-256, bloqueo 5 intentos
    ├── FrmUsuarios.cs + DlgUsuario    # CRUD usuarios con roles
    ├── FrmDashboard.cs                # 6 KPIs + 4 graficas GDI+ + boton Rentabilidad
    ├── FrmFacturas.cs                 # Facturas + DlgFactura + DlgSelProducto
    ├── FrmInventario.cs               # CRUD inventario
    ├── FrmKardex.cs + DlgMovimiento + DlgAjuste
    ├── FrmClientes.cs + DlgCliente
    ├── FrmProveedores.cs + DlgProveedor
    ├── FrmCompras.cs + DlgCompra
    ├── FrmMoldes.cs + DlgMolde
    ├── FrmGastos.cs + DlgGasto
    ├── FrmTransacciones.cs            # Submenu: Facturas, OP, CxC, Precios
    ├── FrmOrdenesProduccion.cs        # OP + DlgOrdenProduccion + DlgCantidadReal
    ├── FrmIngresoProduccion.cs        # Ingreso produccion + boton Calidad
    ├── FrmControlCalidad.cs           # Inspecciones + DlgSelIngreso + DlgInspeccion
    ├── FrmMantenimientoMoldes.cs      # Programa mant. + DlgPrograma + DlgRegistroMant
    ├── FrmCuentasCobrar.cs + DlgCuenta + DlgPago
    ├── FrmCambioPrecios.cs + DlgProducto
    ├── FrmRentabilidad.cs             # Reporte rentabilidad + grafica barras horizontales
    ├── FrmRespaldos.cs                # Visor respaldos + verificacion integridad
    ├── FrmBuscador.cs                 # Buscador global con debounce 300ms
    ├── FrmLogAuditoria.cs             # Visor log auditoria + graficas actividad
    ├── FrmPerfil.cs                   # 3 tabs: info personal, contrasena, preferencias
    ├── FrmNomina.cs                   # Submenu nomina + FrmEmpleados + DlgEmpleado
    ├── FrmProcesarNomina.cs           # Nomina semanal + DlgPeriodoNomina + DlgDetalleNomina
    ├── FrmPrestamos.cs                # Prestamos + DlgNuevoPrestamo + DlgAbonoManual
    ├── FrmBeneficiosLOTTT.cs          # Utilidades, vacaciones, prestaciones (LOTTT)
    └── FrmOtros.cs                    # FrmReportes, FrmArchivos, FrmSistema (en un archivo)
```

---

## 3. BASE DE DATOS — 32 TABLAS SQLITE

### Ruta de la BD
`%AppData%\SistemaGestion\sistema_gestion.db`
(migrada automaticamente desde el directorio del exe al primer arranque)

### Cadena de conexion
```csharp
SecurityService.CadenaConexion  // => "Data Source=%AppData%\SistemaGestion\sistema_gestion.db;"
```

### Tablas por modulo

| Modulo | Tablas |
|--------|--------|
| Inventario | Inventario, MovimientosInventario |
| Ventas | Facturas, DetallesFactura, Clientes, CuentasCobrar, Abonos, Cheques |
| Compras | Compras, DetallesCompra, Proveedores |
| Produccion | Produccion, LineaProducDia, ProduccionDelDiaEnLinea, IngresoProduccion |
| Ordenes | OrdenesProduccion, LineasOrden |
| Calidad | InspeccionesCalidad, DefectosCalidad |
| Mantenimiento | TbMoldes, RegistrosMantenimiento, ProgramasMantenimiento |
| Finanzas | Gastos, ProductosPrecios |
| Nomina | Empleados, PeriodosNomina, DetallesNomina, PrestamosEmpleado, BeneficiosAnuales |
| Sistema | Usuarios, LogAuditoria, PreferenciasUsuario |

---

## 4. MODELOS (clases C#)

Todos viven en `Models/DB.cs` como clases independientes del namespace `SistemaGestion`:

```
Inventario, DetalleFactura, DetalleCompra, Produccion
Factura, Compra, Cliente, Proveedor
Abono, Cheque, TbMolde, LineaProducDia, ProduccionDelDiaEnLinea
LineaFactura          ← IMPORTANTE: clase independiente, NO anidada en DatabaseService
MovimientoInventario, IngresoProduccion, CuentaCobrar, ProductoPrecio
Gasto, ArchivoSistema
RentabilidadProducto
OrdenProduccion, LineaOrden
InspeccionCalidad, DefectoCalidad
RegistroMantenimiento, ProgramaMant
PreferenciasUsuario
Empleado, PeriodoNomina, DetalleNomina, PrestamoEmpleado, BeneficioAnual
DatabaseService.ResultadoBusqueda  ← esta si es anidada dentro de DatabaseService
Sesion (static), PermisosRol (static), Usuario
```

---

## 5. ARQUITECTURA Y PATRONES DE CODIGO

### DatabaseService (estatico, parcial)
- Clase `public static partial class DatabaseService` en `DB.cs`
- Metodo `Conn()` abre SqliteConnection con `ConnStr` (property, no const)
- Helpers: `S(reader, col)` para strings, `D(reader, col)` para decimals
- `Exec(conn, sql, params...)` para comandos simples
- Todas las operaciones criticas usan transacciones SQLite explicitas con ROLLBACK

### Patron de formularios
```csharp
// Lista principal
public class FrmXxx : Form {
    private DataGridView dgv;
    // Boton Nueva -> DlgXxx(null)
    // Boton Editar -> DlgXxx(itemSeleccionado)
    // CargarDatos() -> dgv.DataSource = lista.Select(anonimo).ToList()
}

// Dialogo nueva/editar
public class DlgXxx : Form {
    public Xxx Resultado { get; private set; }
    // Al guardar: Resultado = original ?? new Xxx(); ... DialogResult = OK
}
```

### Transacciones atomicas — patron clave
```csharp
using var conn = new SqliteConnection(ConnStr);
conn.Open();
using var tx = conn.BeginTransaction();
try {
    // operaciones multiples
    tx.Commit();
} catch {
    tx.Rollback();
    throw;
}
```
Usado en: `GuardarDetallesYKardex`, `CerrarOrden`, `InsertarRegistroMant`

### Graficas GDI+
- Todos los graficos se dibujan en el evento `Paint` de paneles (`Panel.Paint += PaintXxx`)
- `e.Graphics.SmoothingMode = SmoothingMode.AntiAlias`
- Invalidar con `panel.Invalidate()` para redibujar
- Tipos implementados: barras verticales, linea con area, donut, barras horizontales, radar (4 ejes), timeline lineal

### Seguridad en capas
```
Capa 1: NTFS ACL  -> solo el usuario Windows puede leer la carpeta
Capa 2: DPAPI     -> clave AES cifrada con credenciales del usuario Windows
Capa 3: AES-256-CBC -> respaldos cifrados, IV aleatorio por archivo
Capa 4: SHA-256 hash -> verificacion de integridad de respaldos
Login: SHA-256 + salt, bloqueo 5 intentos, registro en LogAuditoria
```

### QuestPDF
```csharp
QuestPDF.Settings.License = LicenseType.Community;  // en PdfService.Inicializar()
// Patron Document.Create -> container.Page -> page.Header/Content/Footer
// Tablas con table.ColumnsDefinition + table.Header + foreach rows
```

---

## 6. FLUJOS CRITICOS IMPLEMENTADOS

### Factura -> Kardex -> Inventario
`GuardarDetallesYKardex(idFactura, numeroF, lineas, esNueva)`:
- Por cada linea: lee stock, valida, inserta DetallesFactura, inserta MovimientosInventario (SALIDA), actualiza Inventario.Existencia
- ROLLBACK completo si cualquier producto no tiene stock suficiente

### Cierre de Orden de Produccion -> Inventario
`CerrarOrden(op, cantidadReal)`:
- Descuenta cada materia prima proporcional a (cantidadReal / cantidadPlan)
- Agrega producto terminado como ENTRADA en Kardex
- Actualiza estado orden a 'Completada'
- Todo en una sola transaccion

### Nomina semanal (USD, Venezuela)
`CalcularDetalle(emp, periodo, dias, faltas, piezas, ventas, prestamo, otros, otrasded)`:
- Devengado = (sueldo/dia * dias) + (piezas * tarifapieza) + (ventas * tarifaventa) + otros
- IVSS 4% y RPVH 1% se calculan sobre el devengado en bolivares (* TasaBCV) y se convierten de vuelta a USD
- Descuento faltas = sueldo/dia * diasInasistencia
- NetoUSD = TotalDevengado - TotalDeducciones
- NetoBs = NetoUSD * TasaBCV

### Beneficios LOTTT Venezuela
`CalcularBeneficios(emp, anio, tasa)`:
- Utilidades: 15 dias * salario normal diario (Art. 131)
- Vacaciones: (15 + min(aniosServicio,15)) dias (Art. 190)
- Bono Vacacional: (7 + min(aniosServicio,21)) dias (Art. 192)
- Prestaciones: 60 dias/anio garantia (Art. 142)

### Respaldo automatico
`BackupService.Iniciar()` (llamado post-login):
- Verifica si existe respaldo del dia de hoy
- Si no existe, crea copia cifrada AES-256 en %AppData%\SistemaGestion\Respaldos\
- Timer cada hora verifica cambio de dia
- Limpia respaldos > 30 dias

---

## 7. SEGURIDAD Y ROLES

### Roles definidos en PermisosRol
- **Admin**: acceso total a todo
- **Ventas**: Facturas, CuentasCobrar, Clientes, Reportes
- **Almacen**: Inventario, Kardex, Compras, Proveedores, IngresoProduccion, Moldes
- **Contabilidad**: Reportes, Facturas, Compras, Gastos, CambioPrecios

### Sesion (clase estatica global)
```csharp
Sesion.UsuarioId       // int
Sesion.NombreUsuario   // string login
Sesion.NombreCompleto  // string nombre
Sesion.Rol             // "Admin" | "Ventas" | "Almacen" | "Contabilidad"
Sesion.EsAdmin         // bool
Sesion.TienePermiso("modulo") // bool via PermisosRol
```

---

## 8. ACCESOS AL MENU — NAVEGACION

```
MenuPrincipal (barra de sesion)
├── [Mi Perfil]     -> FrmPerfil (todos)
├── [Auditoria]     -> FrmLogAuditoria (solo Admin)
├── [Usuarios]      -> FrmUsuarios (solo Admin)
├── [Buscar F3]     -> FrmBuscador (todos)
└── [Cerrar sesion]

MenuPrincipal (botones principales)
├── Dashboard       -> FrmDashboard
│   ├── Rentabilidad -> FrmRentabilidad
│   └── Ver Reportes -> FrmReportes
├── Archivos del sistema -> FrmArchivos
│   ├── Inventario, Clientes, Proveedores, Moldes
│   ├── Kardex, Ingreso Produccion -> [Calidad] -> FrmControlCalidad
│   ├── Ordenes OP -> FrmOrdenesProduccion
│   ├── Control Cal. -> FrmControlCalidad
│   ├── Mant.Moldes -> FrmMantenimientoMoldes
│   └── Respaldos (Admin) -> FrmRespaldos
├── Transacciones -> FrmTransacciones
│   ├── Facturas -> FrmFacturas
│   ├── Ordenes de Produccion -> FrmOrdenesProduccion
│   ├── Cuentas por Cobrar -> FrmCuentasCobrar
│   └── Cambio de Precios -> FrmCambioPrecios
├── Compras -> FrmCompras
│   (incluye Gastos en FrmArchivos)
└── Nomina -> FrmNomina
    ├── Empleados -> FrmEmpleados
    ├── Procesar Nomina Semanal -> FrmProcesarNomina
    ├── Prestamos Internos -> FrmPrestamos
    └── Beneficios LOTTT -> FrmBeneficiosLOTTT

Sistema (boton en panel inferior)
├── Mi Perfil y preferencias -> FrmPerfil
├── Ver Respaldos -> FrmRespaldos
└── Ver log de auditoria (Admin) -> FrmLogAuditoria
```

---

## 9. DECISIONES IMPORTANTES TOMADAS

| Decision | Razon |
|----------|-------|
| `LineaFactura` como clase independiente (NO anidada) | Evita `CS0426` — el compilador no puede referenciar clases anidadas en clases estaticas desde otros archivos facilmente |
| Toda la logica de BD en un solo archivo `DB.cs` | Simplicidad de navegacion; `partial class` permite dividirlo si crece demasiado |
| `ConnStr` como property `=>` en vez de `const` | Permite que SecurityService.CadenaConexion sea evaluado en tiempo de ejecucion (ruta dinamica a AppData) |
| GDI+ en vez de LiveCharts/chart libraries | Sin dependencias adicionales, funciona en .NET 8 sin configuracion extra |
| `SplitContainer` reemplazado por paneles con Dock | `SplitterDistance` falla si se asigna antes de que el form tenga tamaño real |
| Nomina en USD con IVSS calculado en Bs | Realidad venezolana: aportes legales se calculan en bolivares aunque el salario sea en USD |
| TasaBCV manual por periodo de nomina | No existe API publica confiable para tasa BCV; campo obligatorio al crear cada periodo |
| `INSERT OR REPLACE` para DetallesNomina | Permite editar lineas multiples veces sin duplicar registros |
| Strings sin acentos en literales de codigo | El compilador de Windows puede fallar con caracteres especiales en string literals; usar sin acento y mostrar con acento solo en la UI |

---

## 10. PROBLEMAS RESUELTOS (errores comunes en este proyecto)

```
CS0101 "ya contiene una definicion"  -> Borrar archivos _1.cs duplicados del proyecto
CS0426 "LineaFactura no existe"       -> Mover clase fuera de DatabaseService al namespace
CS0104 "Timer ambiguo"                -> Calificar: System.Threading.Timer
SplitterDistance error               -> Asignar en evento Load o usar paneles Dock en su lugar
Strings con acentos en literales     -> Eliminar acentos de string literals en codigo C#
```

---

## 11. ESTADO ACTUAL — COMPLETADO

### Fase 1 — Seguridad
- [x] Login SHA-256 + salt + bloqueo 5 intentos
- [x] Roles y permisos por modulo
- [x] BD migrada a %AppData% con permisos NTFS
- [x] Clave AES-256 protegida con DPAPI
- [x] Respaldo automatico diario cifrado AES-256
- [x] Verificacion de integridad SHA-256
- [x] Visor de log de auditoria con graficas

### Fase 2 — Ciclo de ventas
- [x] Facturas con lineas de productos
- [x] Factura -> Kardex -> Inventario (transaccion atomica)
- [x] Validacion de stock antes de guardar
- [x] Exportacion PDF de facturas individuales
- [x] Cuentas por cobrar y abonos

### Fase 3 — Reportes e inteligencia
- [x] Dashboard con 4 graficas GDI+ + 6 KPIs
- [x] Reporte de rentabilidad por producto
- [x] Exportacion PDF de reportes
- [x] Buscador global F3 (8 tablas, debounce 300ms)

### Fase 4 — Produccion y calidad
- [x] Ordenes de produccion vinculadas al inventario
- [x] Cierre de OP con descuento proporcional de materias primas
- [x] Control de calidad en ingreso de produccion
- [x] Programa de mantenimiento de moldes con timeline GDI+

### Fase 5 — Experiencia de usuario
- [x] Buscador global con tarjetas agrupadas
- [x] Alertas de stock bajo en dashboard
- [x] Perfil de usuario (info, contrasena con indicador fuerza, preferencias)
- [x] Visor de auditoria con filtros y graficas

### Modulo Nomina (Venezuela, USD)
- [x] Catalogo de empleados (Produccion / Oficina / Fijo)
- [x] Periodos semanales con tasa BCV manual
- [x] Calculo automatico: sueldo + piezas/comisiones - IVSS 4% - RPVH 1%
- [x] Prestamos internos con descuento automatico en nomina
- [x] Beneficios LOTTT: utilidades, vacaciones, bono vacacional, prestaciones
- [x] Planilla de nomina PDF + Planilla IVSS PDF
- [x] Reporte anual de beneficios PDF

---

### Modulo Nomina — Aportes Patronales (agregado)
- [x] Tabla `AportesPatronales` en SQLite
- [x] Modelos `AportePatronal` y `ResumenAportesPatronales`
- [x] `CalcularAportesPatronales(idPeriodo, pctIVSS=9%, valorUT)` — calcula los 4 conceptos
- [x] Pestana "Aportes Patronales" en `FrmProcesarNomina` con grid detallado y resumen
- [x] Campo % IVSS patron configurable (9%, 10% u 11% segun nivel de riesgo empresa)
- [x] Campo Valor UT en Bs (manual, se actualiza segun Gaceta Oficial)
- [x] PDF "Aportes Patron" con planilla completa y nota de base legal
- [x] `GenerarPlanillaAportesPatronales()` en PdfService

---

## 13. MODULOS PENDIENTES SUGERIDOS

- [ ] **Cuentas por Pagar** (proveedores) — cierra el ciclo contable
- [ ] **Flujo de Caja** — proyeccion 30/60/90 dias (CxC vs CxP vs nomina)
- [ ] **Punto de Equilibrio** — calculo automatico con datos de costos y ventas
- [ ] **Control de Mermas** — diferencia entre consumo teorico y real en produccion
- [ ] **Gestion de Cheques** — modelo Cheque existe en BD pero sin formulario completo
- [ ] **ISLR Estimado** — estimacion de impuesto sobre la renta para anticipos fiscales
- [ ] **Alertas al inicio de sesion** — stock bajo + cobros vencidos al abrir el sistema
