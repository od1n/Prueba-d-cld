# `FrmFlujoCaja` — Flujo de Caja (Proyección 30/60/90 días)

> Proyección del efectivo del negocio para los próximos 90 días: ingresos esperados (cobranzas, ventas) vs egresos (compras, nómina), con saldo acumulado proyectado y tres KPIs por tramo.

## Cómo llegar

- **Ruta**: *Menú principal → Reportes → Flujo de Caja*.
- **Permiso requerido**: `Reportes` (o `Admin`).

## Para qué se usa

Saber si vas a tener plata para pagar las facturas y la nómina del mes siguiente. Permite:

- Ver los **ingresos proyectados** (vencimientos de cuentas por cobrar, facturas de cliente).
- Ver los **egresos proyectados** (vencimientos de cuentas por pagar, nómina del último período proyectada).
- Ajustar el **saldo inicial** (lo que tenés en caja hoy).
- Agregar **movimientos manuales** que no estén registrados en el sistema (ej. un préstamo bancario futuro).
- Exportar a **PDF** para llevar a reuniones.

## Antes de empezar

- Tener facturas a crédito y compras a crédito con sus **fechas de vencimiento**.
- Tener cargado al menos un período de nómina reciente (se usa para proyectar la del próximo período).

## Qué ves en la pantalla

```
┌──────────────────────────────────────────────────────────────────────────┐
│ Flujo de Caja — Proyeccion 30 / 60 / 90 dias            [PDF][Cerrar]    │
├──────────────────────────────────────────────────────────────────────────┤
│ Saldo inicial USD: [5.000,00]  [Recalcular]                              │
│ Mostrar: [✓]Ingresos [✓]Egresos [✓]Nomina   Tramo: [Todos (90d) ▼]  [+M]│
├──────────────────────────────────────────────────────────────────────────┤
│ ┌─0–30 dias────┐ ┌─31–60 dias───┐ ┌─61–90 dias───┐                       │
│ │Ingresos:$8k  │ │Ingresos:$5k  │ │Ingresos:$3k  │                       │
│ │Egresos: $6k  │ │Egresos: $4k  │ │Egresos: $4k  │                       │
│ │Neto:   +$2k  │ │Neto:   +$1k  │ │Neto:   -$1k  │                       │
│ │Saldo:  $7k   │ │Saldo:  $8k   │ │Saldo:  $7k   │                       │
│ └──────────────┘ └──────────────┘ └──────────────┘                       │
├──────────────────────────────────────────────────────────┬───────────────┤
│ Fecha │ Días │ Tipo │ Categ. │ Concepto │ Ingreso │Egreso│ Saldo acumul. │
│ 30/04 │  2d  │Egreso│ Nomina │ Sem 28/04│         │$1.0k │   ↗ ─ ─       │
│ 02/05 │  4d  │Ingres│ CxC    │ F-00125  │ $750    │      │  Hoy +30 +60  │
└──────────────────────────────────────────────────────────┴───────────────┘
   Los montos proyectados se basan en fechas de vencimiento de facturas, compras,
   cuentas por cobrar y nomina del ultimo periodo.
```

### Barra de parámetros

| Control | Detalle |
|---|---|
| **Saldo inicial USD** | Plata en caja hoy. **Editable**, defecto 0. |
| **Recalcular** | Recalcula con el nuevo saldo / movimientos manuales. |
| **Ingresos / Egresos / Nómina** | Tres casillas para mostrar/ocultar tipos de movimiento. |
| **Tramo** | `Todos (90d)` · `30 dias` · `60 dias` · `90 dias`. |
| **+ Manual** | Abre un diálogo para sumar un movimiento puntual (ver Caso 3). |

### KPIs por tramo (3 tarjetas)

Para cada tramo (0–30, 31–60, 61–90 días):

```
Ingresos: $X
Egresos:  $Y
Neto: $X-Y
Saldo acum: $Saldo final del tramo
```

### Tabla central

| Columna | Significado |
|---|---|
| **Fecha** | Cuándo ocurre el movimiento |
| **Días** | Distancia desde hoy (`+5d`, `-2d`) |
| **Tipo** | `Ingreso` (verde) · `Egreso` (rojo / ámbar / amarillo según categoría) |
| **Categoría** | `CxC` · `Compra` · `Nomina` · `Manual`, etc. |
| **Concepto** | Descripción |
| **Ingreso** | Monto si es ingreso |
| **Egreso** | Monto si es egreso |
| **Origen** | `Real` (registrado en sistema) · `Proyectado` (estimado) |

### Gráficos (panel derecho)

**1. Saldo acumulado proyectado** — línea con 4 puntos: Hoy, +30d, +60d, +90d. Verde si positivo, rojo si negativo. Área de fondo rellena.

**2. Ingresos vs egresos por tramo** — barras (verde ingresos, rojo egresos) por cada tramo.

### Diálogo: agregar movimiento manual

| Campo | Detalle |
|---|---|
| **Tipo** | `Ingreso` o `Egreso`. |
| **Fecha** | Por defecto +7 días. |
| **Concepto** | Texto libre (default `Movimiento manual`). |
| **Monto USD** | **Obligatorio (>0).** |

## Casos de uso

### Caso 1 — Ver el flujo proyectado para los próximos 30 días

1. Ingresar el **Saldo inicial** (lo que hay en caja realmente).
2. Hacer clic en **Recalcular**.
3. Mirar la tarjeta **0–30 dias** para ver el saldo al final del primer mes.
4. (Opcional) Cambiar el **Tramo** a `30 dias` para ver solo esa columna en la tabla.

### Caso 2 — Filtrar para ver solo ingresos

1. Desmarcar **Egresos** y **Nómina**.
2. La tabla queda solo con cobranzas / ingresos esperados.

### Caso 3 — Agregar un préstamo bancario futuro

1. Hacer clic en **+ Manual**.
2. Tipo: `Ingreso`.
3. Fecha: la del crédito esperado.
4. Concepto: `Préstamo banco`.
5. Monto: el monto del préstamo.
6. **Agregar**.

**Resultado**: el movimiento aparece en la tabla con origen `Proyectado` y categoría `Manual`. El saldo acumulado y los KPIs se recalculan.

### Caso 4 — Exportar PDF

Hacer clic en **Exportar PDF**. Se guarda con el nombre `FlujoCaja_AAAA-MM-DD.pdf` y se abre.

## Validaciones y errores frecuentes

| Mensaje en pantalla | Qué significa | Cómo se resuelve |
|---|---|---|
| `Ingresa un monto.` | Falta el monto del movimiento manual | Ingresar un valor > 0 |
| `Error al generar PDF: …` | Falla técnica | Reintentar |

## Consejos

- **El saldo inicial NO se guarda**: hay que volver a cargarlo cada vez que se abre la pantalla. Tomalo como una proyección "what-if".
- **Los movimientos manuales se borran al cerrar la pantalla** — son solo para el análisis del momento.
- **Saldo final negativo** indica que vas a tener que conseguir financiamiento o adelantar cobranzas.
- La proyección **se basa en lo registrado**: si tenés facturas a crédito sin cargar, no aparecen.
- **La nómina** se proyecta tomando el último período pagado; si no hay nómina cargada, la proyección sale incompleta.

## Pantallas relacionadas

- **`FrmCuentasCobrar`** — vencimientos de los ingresos proyectados.
- **`FrmCuentasPagar`** — vencimientos de los egresos proyectados.
- **`FrmProcesarNomina`** — la base para proyectar la nómina del próximo período.
- **`FrmFacturas`**, **`FrmEgresos`** — donde se cargan las fechas de vencimiento.
