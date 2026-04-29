# `FrmRentabilidad` — Reporte de Rentabilidad por Producto

> Reporte detallado del margen de cada producto en el período: ingresos, costo, utilidad bruta, margen %, unidades vendidas, stock y rotación. Muestra los KPIs globales y un gráfico de barras horizontal con el top 10.

## Cómo llegar

- **Ruta**: *Menú principal → Reportes → Rentabilidad* (también accesible desde la pestaña *Rentabilidad* de `FrmReportes`).
- **Permiso requerido**: `Reportes` (o `Admin`).

## Para qué se usa

Saber **cuánto deja cada producto** en un período. Identificar:

- Los productos más rentables (top utilidad bruta).
- Los de mayor margen porcentual.
- Los que no tuvieron movimiento.
- Los que vendieron a pérdida.

## Antes de empezar

- Tener cargados los **costos** de los productos en *Archivos → Inventario* (sin costo, el margen no se calcula).
- Tener facturas emitidas en el período de análisis.

## Qué ves en la pantalla

```
┌──────────────────────────────────────────────────────────────────────────┐
│ Rentabilidad por Producto                  [? Ayuda][Exportar PDF][X]    │
├──────────────────────────────────────────────────────────────────────────┤
│ Desde [01/02] Hasta [28/04] Depto [Todos ▼] Orden [Utilidad bruta ▼]     │
│ Buscar [_____]   [✓] Solo con ventas    [Filtrar]                        │
├──────────────────────────────────────────────────────────────────────────┤
│ ┌Ingresos┐┌Utilidad┐┌Margen┐┌Top   ┐┌Mejor ┐┌Sin    ┐                    │
│ │ totales││ bruta  ││ prom ││ prod ││margen││movim. │                    │
│ │  $42k  ││  $14k  ││ 33%  ││P-101 ││P-205 ││  4    │                    │
│ └────────┘└────────┘└──────┘└──────┘└──────┘└───────┘                    │
├──────────────────────────────────────────────────┬───────────────────────┤
│ Codigo │ Descripcion │ Costo │ Precio │ ...      │  Top 10 — Utilidad    │
│ P-101  │ Pote 1L Tap │ $2,50 │ $4,80  │ ...      │  ▓▓▓▓▓▓▓ P-101  $4k   │
│ P-205  │ Botella 5L  │ $1,20 │ $2,90  │ ...      │  ▓▓▓▓▓▓ P-205   $3,2k │
└──────────────────────────────────────────────────┴───────────────────────┘
   Productos: 124 | Click en cabecera de columna para ordenar
```

### Filtros (toolbar)

| Control | Detalle |
|---|---|
| **Desde / Hasta** | Por defecto últimos 3 meses → hoy. |
| **Depto** | Filtra por departamento (categoría) del producto. |
| **Orden** | `Utilidad bruta (desc)` · `Margen % (desc)` · `Ingresos (desc)` · `Unidades vendidas (desc)` · `Descripcion (asc)`. |
| **Buscar** | Por código o descripción. |
| **Solo con ventas** | Casilla. Oculta los que no tuvieron venta en el período. |
| **Filtrar** | Recarga los datos desde la BD. |

### Tarjetas KPI

- **Ingresos totales**.
- **Utilidad bruta** (precio − costo × cantidad vendida).
- **Margen promedio %**.
- **Top producto** (por utilidad).
- **Mejor margen** (`código MM%`).
- **Sin movimiento** (cuántos no tuvieron venta).

### Tabla central

Cabecera con **clic ordenable** (hace ascendente la primera vez, segunda vez descendente).

| Columna | Significado |
|---|---|
| **Codigo / Descripcion / Departamento** | Identificación del producto |
| **Costo** | Costo unitario actual |
| **Precio** | Precio de venta actual |
| **Margen_U** | Margen unitario en USD |
| **Margen_Pct** | Margen %: verde ≥ 30%, ámbar ≥ 10%, rojo < 10% |
| **Vendidas** | Unidades vendidas en el período |
| **Ingresos** | Ingresos totales del producto |
| **Costo_Total** | Costo total |
| **Utilidad** | Utilidad bruta (resaltada en rojo si es negativa) |
| **Stock** | Existencia actual |
| **ValInventario** | Valor del stock al costo |
| **Rotacion** | Meses para rotar el stock al ritmo de venta |

Productos sin movimiento aparecen en gris.

### Gráfico (panel derecho)

**Top 10 — Utilidad bruta** en barras horizontales. La barra del producto seleccionado en la tabla se resalta. Colores:

- Verde (margen ≥ 30%)
- Verde claro (margen ≥ 15%)
- Ámbar (margen < 15%)
- Rojo (utilidad negativa)

## Casos de uso

### Caso 1 — Ver los productos más rentables del trimestre

1. Ajustar **Desde / Hasta** al trimestre.
2. **Filtrar**.
3. Mirar las tarjetas KPI y el gráfico de top 10.
4. (Opcional) Hacer clic en **Utilidad** en la cabecera de la tabla para ordenar.

### Caso 2 — Identificar productos con margen bajo

1. Cambiar **Orden** a `Margen % (desc)`.
2. Bajar al fondo de la tabla — los rojos son los más urgentes.
3. Investigar costo o precio según corresponda.

### Caso 3 — Productos sin movimiento

1. Marcar `Solo con ventas` y desmarcar después para ver los que NO tuvieron ventas.
2. La tarjeta **Sin movimiento** muestra el conteo.

### Caso 4 — Exportar PDF

Hacer clic en **Exportar PDF**. Se guarda con el nombre `Rentabilidad_AAAA-MM-DD.pdf` y se abre.

## Validaciones y errores frecuentes

| Mensaje en pantalla | Qué significa | Cómo se resuelve |
|---|---|---|
| `No hay datos para exportar.` | El filtro no devolvió productos | Ampliar el rango / quitar filtros |
| `Error al generar PDF: …` | Falla técnica al armar el PDF | Reintentar |

## Consejos

- **Mantené los costos actualizados** en *Archivos → Inventario*. Sin costo, la utilidad y el margen no son confiables.
- **Click en cabecera** ordena por esa columna (primera vez ascendente, segunda descendente).
- **Productos en gris** no tuvieron venta — pueden ser candidatos a rebaja, descuento o discontinuación.
- **Utilidad en rojo** = vendiste a pérdida. Revisar precio o costo del producto.

## Pantallas relacionadas

- **`FrmInventario`** — donde se cargan costo y precio.
- **`FrmCambioPrecios`** — para ajustar precios de venta tras analizar el reporte.
- **`FrmPuntoEquilibrio`** — análisis de punto de equilibrio del negocio.
