# `FrmPuntoEquilibrio` — Punto de Equilibrio (Break-Even)

> Calcula automáticamente cuánto necesita facturar el negocio para cubrir todos sus costos (punto de equilibrio). Muestra ingresos, costos fijos y variables, margen de contribución, margen de seguridad y un diagrama gráfico.

## Cómo llegar

- **Ruta**: *Menú principal → Reportes → Punto de Equilibrio*.
- **Permiso requerido**: `Reportes` (o `Admin`).

## Para qué se usa

Saber cuál es la **facturación mínima mensual** necesaria para no tener pérdidas:

- El sistema toma automáticamente los **ingresos del período seleccionado** (último mes / 3 meses / 6 meses / año), los costos variables (productos vendidos) y los costos fijos (gastos recurrentes).
- Permite **agregar conceptos manuales** (alquiler, sueldos administrativos, etc.) que no estén en la BD.
- Calcula y dibuja el punto donde ingresos = costos totales.

## Qué ves en la pantalla

```
┌──────────────────────────────────────────────────────────────────────────┐
│ Punto de Equilibrio Automatico   Periodo: [Ultimos 3 meses ▼]            │
│                                  [Recalcular][Exportar PDF][? Ayuda]     │
├──────────────────────────────────────────────────────────────────────────┤
│ ┌Ingresos/Mes─┐┌Cost. Var ─┐┌Cost. Fijos┐┌Punto Equil.┐┌Margen Contr┐┌MS┐│
│ │   $12.500   ││   $4.500  ││   $3.200  ││   $7.111   ││   64,0%    ││43%│
│ └─────────────┘└───────────┘└───────────┘└────────────┘└────────────┘└──┘│
├──────────────────────────────────────────────────────────────────────────┤
│  Diagrama de Punto de Equilibrio (USD mensuales)                         │
│   ↗ ─ ─ Ingresos                                                         │
│   ── Costos Totales                            (zona verde = utilidad)   │
│   - - Costos Fijos                                                       │
│   ●  Punto Equilibrio                                                    │
├──────────────────────────────────────────────────────────────────────────┤
│ Conceptos de Costo (editables):    [+ Agregar][Editar][Eliminar]         │
│ Concepto       │ Tipo     │ Monto Mensual │ Origen                       │
│ Alquiler       │ Fijo     │  $1.200,00    │ Manual                       │
│ Materia prima  │ Variable │  $4.500,00    │ Automatico                   │
│ Sueldos        │ Fijo     │  $1.800,00    │ Automatico                   │
└──────────────────────────────────────────────────────────────────────────┘
```

### Toolbar

| Control | Detalle |
|---|---|
| **Periodo** | `Ultimo mes` · `Ultimos 3 meses` (default) · `Ultimos 6 meses` · `Ultimo anio`. |
| **Recalcular** | Recarga datos automáticos desde la BD y recalcula. |
| **Exportar PDF** | Guarda el análisis en PDF. |
| **? Ayuda** | Abre esta ficha del manual. |

### Tarjetas KPI

| KPI | Significado |
|---|---|
| **Ingresos/Mes** | Promedio mensual del período |
| **Costos Variables** | Costo de los productos vendidos (varía con la venta) |
| **Costos Fijos** | Suma de los conceptos fijos (no varían con la venta) |
| **Punto de Equilibrio** | Facturación mensual necesaria para no perder |
| **Margen Contribución** | `(Ingresos − Costos Variables) / Ingresos`. Cuánto queda por cada $ vendido para pagar fijos. |
| **Margen Seguridad** | Cuánto puede caer la facturación antes de entrar en pérdida. **Verde si ≥ 15%, rojo si < 15%.** |

### Diagrama central (gráfico)

Eje X: ventas mensuales (en USD). Eje Y: ingresos / costos.

- **Línea verde diagonal** — ingresos.
- **Línea ámbar** — costos totales (fijos + variables proporcionales).
- **Línea roja punteada horizontal** — costos fijos.
- **Punto azul** — punto de equilibrio.
- **Zona izquierda** (rojo claro) — pérdida.
- **Zona derecha** (verde claro) — utilidad.

### Tabla "Conceptos de Costo"

Lista los conceptos que entran en el cálculo, con color por tipo (`Fijo` rosa claro, `Variable` verde claro).

| Columna | Detalle |
|---|---|
| **Concepto** | Descripción |
| **Tipo** | `Fijo` o `Variable` |
| **Monto Mensual (USD)** | Lo que aporta al cálculo |
| **Origen** | `Automatico` (lo trae la BD) o `Manual` (cargado a mano acá) |

Botones: **+ Agregar**, **Editar**, **Eliminar**. Solo se pueden eliminar conceptos `Manual`.

### Diálogo de concepto

Ver ficha de **`DlgConceptoPE`**.

## Casos de uso

### Caso 1 — Calcular el punto de equilibrio del trimestre

1. Elegir **Periodo: Ultimos 3 meses**.
2. **Recalcular**.
3. Mirar la tarjeta **Punto de Equilibrio**: ese es el monto **mensual** que hace falta facturar.
4. Comparar con **Ingresos/Mes** — si está por encima, el negocio está en zona verde.

### Caso 2 — Agregar un costo fijo nuevo (ej. alquiler)

1. Hacer clic en **+ Agregar** en la tabla.
2. **Concepto**: `Alquiler`.
3. **Tipo**: `Fijo`.
4. **Monto/Mes**: el monto del alquiler.
5. **Guardar**.

**Resultado**: el concepto entra al cálculo como `Manual`, los KPIs y el diagrama se actualizan.

### Caso 3 — Editar un concepto manual

1. Seleccionarlo en la tabla.
2. **Editar** → modificar → **Guardar**.

> Los conceptos `Automaticos` se pueden ver pero **no se pueden eliminar** — para cambiarlos hay que ajustar la fuente original (ventas, costos de productos, etc.). El sistema avisa: `Solo se pueden eliminar conceptos agregados manualmente. Los datos automaticos se recalculan desde la base de datos.`

### Caso 4 — Exportar PDF

Hacer clic en **Exportar PDF**. Se guarda con nombre `PuntoEquilibrio_AAAAMMDD.pdf` y se abre.

## Validaciones y errores frecuentes

| Mensaje en pantalla | Qué significa | Cómo se resuelve |
|---|---|---|
| `Solo se pueden eliminar conceptos agregados manualmente. …` | Se intentó borrar un concepto automático | Sólo se borran los `Manual` |
| `Eliminar '<concepto>'?` | Confirmación al borrar | Sí / No |
| `Error al cargar datos: …` | Falla al traer datos desde la BD | Reintentar |
| `Sin datos suficientes para graficar.` | No hay ingresos en el período | Cambiar el período o cargar facturas |

## Consejos

- **Margen de seguridad < 15%** es zona de alerta — un mal mes puede llevar a pérdida.
- **Punto de equilibrio mensual** debe ser inferior a tus ingresos mensuales promedio para que el negocio sea sostenible.
- **Los conceptos manuales se reaplican** cada vez que se recalcula — no se pierden al cambiar el período.
- Si el cálculo automático no incluye ciertos costos (alquiler, servicios, etc.), agregalos manualmente para que el resultado refleje la realidad.

## Pantallas relacionadas

- **`DlgConceptoPE`** — diálogo para agregar / editar conceptos.
- **`FrmRentabilidad`** — cálculo del costo variable por producto.
- **`FrmEgresos`** — fuente de los costos fijos automáticos.
- **`FrmFlujoCaja`** — proyección a corto plazo (complementaria al PE).
