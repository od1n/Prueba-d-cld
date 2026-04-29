# `DlgConceptoPE` — Diálogo: Concepto de Punto de Equilibrio

> Diálogo emergente para **agregar** o **editar** un concepto de costo manual dentro del análisis de Punto de Equilibrio.

## Cómo llegar

- Se abre desde **`FrmPuntoEquilibrio`** al hacer clic en **+ Agregar** o **Editar** sobre la tabla "Conceptos de Costo".
- **Permiso requerido**: el mismo que `FrmPuntoEquilibrio` (`Reportes` o `Admin`).

## Para qué se usa

Cargar costos que el sistema **no detecta automáticamente** (porque no están en la base de datos como gasto formal):

- Alquiler.
- Sueldos administrativos no nominados en el sistema.
- Honorarios profesionales.
- Servicios públicos no registrados como egreso.
- Cualquier costo recurrente que afecte el punto de equilibrio.

## Qué ves en la pantalla

```
┌────────────────────────────────────────┐
│       Agregar Concepto                 │
├────────────────────────────────────────┤
│ Concepto:        [_______________]     │
│ Tipo:            [Fijo ▼]              │
│ Monto/Mes (USD): [______]              │
│                                        │
│                  [ Cancelar ][Guardar] │
└────────────────────────────────────────┘
```

### Campos

| Campo | Detalle |
|---|---|
| **Concepto** | Texto libre, máximo 100 caracteres. **Obligatorio.** Ej. "Alquiler local", "Internet", "Honorarios contador". |
| **Tipo** | `Fijo` (default) o `Variable`. **Fijo**: no cambia con la venta (alquiler, sueldos). **Variable**: cambia con la venta (comisiones, materia prima). |
| **Monto/Mes (USD)** | Monto mensual en USD, hasta 9.999.999, dos decimales. |

### Botones

| Botón | Qué hace |
|---|---|
| **Guardar** | Valida y guarda el concepto. Lo marca como `Origen: Manual`. |
| **Cancelar** | Descarta y cierra. |

## Casos de uso

### Caso 1 — Agregar el alquiler mensual

1. Desde **`FrmPuntoEquilibrio`**, hacer clic en **+ Agregar**.
2. **Concepto**: `Alquiler local`.
3. **Tipo**: `Fijo`.
4. **Monto/Mes**: el alquiler en USD.
5. **Guardar**.

**Resultado**: aparece en la tabla con `Origen: Manual`. El cálculo del punto de equilibrio se actualiza solo.

### Caso 2 — Editar un concepto existente

1. Seleccionar la fila en la tabla.
2. Hacer clic en **Editar**.
3. Modificar concepto / tipo / monto.
4. **Guardar**.

## Validaciones y errores frecuentes

| Mensaje en pantalla | Qué significa | Cómo se resuelve |
|---|---|---|
| `Ingrese un concepto.` | Campo concepto vacío | Llenar el campo |

## Consejos

- **Costos fijos** son los que pagás aunque no vendas nada (alquiler, sueldos administrativos, internet). **Variables** son los que aumentan con la venta (materia prima, comisiones).
- Si dudás del tipo, ponelo como **Fijo** — el cálculo será más conservador (mayor PE) y más seguro.
- El monto debe ser **mensual**. Si pagás trimestral, dividilo por 3.

## Pantallas relacionadas

- **`FrmPuntoEquilibrio`** — pantalla que abre este diálogo.
