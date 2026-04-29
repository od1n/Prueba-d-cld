# `FrmReportes` — Submenú de Reportes

> Pantalla con todas las consultas y reportes en pestañas: clientes, ventas, abonos, cuentas por cobrar, producción, inventario, compras, moldes, rentabilidad, P&L, flujo de caja, libro IVA, mermas y comparación de períodos.

## Cómo llegar

- **Ruta**: *Menú principal → Reportes* (barra inferior).
- **Permiso requerido**: `Reportes` (o `Admin`).

## Para qué se usa

Es el centro de consulta. Cada **pestaña** es un reporte distinto, casi todos con filtros por fecha y la opción de exportar a **PDF** y/o **Excel**. Las consultas pesadas muestran **cursor de espera** y se ejecutan en segundo plano para que el sistema no se congele.

## Qué ves en la pantalla

```
┌────────────────────────────────────────────────────────────────────────┐
│ Reportes del Sistema                                                   │
├────────────────────────────────────────────────────────────────────────┤
│ ┌Resumen┬Clientes┬Ventas┬VentasCli┬Abonos┬CxC┬Produccion┬Inventario┐  │
│ │ Patrones│Compras│Moldes│Rentabilidad│P&L│FlujoCaja│LibroIVA│Mermas│  │
│ │ Comparacion ...                                                  │  │
│ └────────────────────────────────────────────────────────────────────┘ │
│                                                                        │
│  ... contenido de la pestaña activa ...                                │
│                                                                        │
├────────────────────────────────────────────────────────────────────────┤
│                                                          [ Cerrar ]    │
└────────────────────────────────────────────────────────────────────────┘
```

### Pestañas disponibles

| Pestaña | Para qué sirve |
|---|---|
| **Resumen** | Tarjetas con totales globales: Total Facturado, Total Compras, Clientes, Proveedores, Productos, Bajo Mínimo, Moldes, Mant. Vencido. |
| **Clientes** | Lista de clientes con buscador y exportación PDF. |
| **Ventas** | Líneas de venta del período, vista General/Agrupado/Gráfico, exporta PDF y Excel. |
| **VentasCli** | Ventas agrupadas por cliente. |
| **Abonos** | Cobros recibidos en el período. |
| **CuentasCobrar** | Cuentas por cobrar pendientes. |
| **Producción** | Resumen de producción por fechas. |
| **Inventario** | Stock actual con filtros. |
| **Patrones** | Patrones de venta (estacionalidad, frecuencia). |
| **Compras** | Compras por período. |
| **Moldes** | Uso y mantenimiento de moldes. |
| **Rentabilidad** | Margen por producto (también disponible en pantalla aparte). |
| **P&L** | Estado de resultados (ventas vs egresos). |
| **Flujo de caja** | Proyección 30/60/90 días. |
| **Libro IVA** | Libro fiscal SENIAT (ventas y compras). |
| **Mermas** | Mermas del período. |
| **Comparación** | Comparativo entre dos períodos. |

> Algunas pestañas como **Rentabilidad**, **Flujo de caja**, **Libro IVA**, **Mermas**, **Comparación** y **Punto de equilibrio** también están disponibles como **pantallas independientes** (con más detalle y exportación dedicada). Ver fichas individuales en este capítulo.

## Casos de uso

### Caso 1 — Ver el resumen general

Abrir **Reportes** → primera pestaña **Resumen**. Las tarjetas se cargan en segundo plano (mientras carga, dice `Cargando resumen...`).

### Caso 2 — Sacar el listado de ventas del mes

1. Pestaña **Ventas**.
2. Ajustar **Desde / Hasta** (por defecto último mes → hoy).
3. (Opcional) Filtrar por **Producto**.
4. Hacer clic en **General** (tabla detallada), **Agrupado** (resumido), **Gráfico** o **PDF / Excel**.

### Caso 3 — Comparar dos períodos

Pestaña **Comparación** o pantalla independiente `FrmComparacion` (ver ficha).

## Consejos

- Si una pestaña tarda en cargar, esperá: el cursor cambia a reloj de arena y los botones se deshabilitan automáticamente para evitar doble clic.
- Para análisis profundos, usá las **pantallas independientes** del módulo (Rentabilidad, Flujo de caja, Libro IVA, etc.) — tienen más opciones, gráficos y exportaciones.

## Pantallas relacionadas

- **`FrmRentabilidad`** — versión completa del análisis de rentabilidad.
- **`FrmFlujoCaja`** — flujo de caja con KPIs y gráfico.
- **`FrmLibroIVA`** — libro IVA con totales.
- **`FrmMermas`** — reporte detallado de mermas.
- **`FrmComparacion`** — comparativo profundo entre períodos.
- **`FrmPuntoEquilibrio`** — análisis de punto de equilibrio (no incluido aquí).
