# `FrmDashboardV2` — Dashboard

> Pantalla de resumen del negocio. Muestra KPIs clave (ventas, stock, CxC, BCV), alertas activas, una gráfica rotativa con cuatro vistas y la lista de últimas facturas.

## Cómo llegar

- **Ruta**: *Menú principal → Dashboard* (tarjeta grande del centro).
- **Atajo**: ninguno.
- **Permiso requerido**: `Reportes` o `Admin`.

## Para qué se usa

Es la pantalla que abre el dueño o el administrador a primera hora para ver, en un solo vistazo:

- Cuánto se vendió **hoy** y en el **mes**.
- Cuántas facturas están **pendientes de cobro** y cuánta plata representan.
- Cuántos productos están con **stock bajo**.
- La **tasa BCV / Paralelo** del día y si está actualizada.
- Las **alertas activas** del sistema.
- Las **últimas facturas** emitidas.

## Antes de empezar

- Tener **datos cargados**: facturas, productos, tasa del día. Sin tasa, los KPIs en USD muestran `—`.
- Tener permiso **`Reportes`** asignado por el administrador (o ser Admin).

## Qué ves en la pantalla

```
┌──────────────────────────────────────────────────────────────────────┐
│ ⚙ SistemaGestion           Juan Pérez · Admin    [← Volver al menú]  │  ← navbar
├──────────────────────────────────────────────────────────────────────┤
│ VENTAS HOY  MES   PEND  STK   CxC      BCV  36,50  PAR  41,20        │  ← strip KPIs
│ $ 0,00     $ 0,00  0     0    $ 0,00   ● actualizada hoy   [↻]       │
├───────────┬──────────────────────────────────────────────────────────┤
│ ALERTAS   │  Ventas diarias del mes              ● ○ ○ ○  [📌 Fijar] │  ← gráfica
│ ACTIVAS   │                                                          │
│           │     ╱╲                                                   │
│ ▌Stock    │    ╱  ╲___╱╲___╱╲                                        │
│ Tornillo  │___╱                                                      │
│ existencia│                                                          │
│ 0/10      │                                                          │
│           ├──────────────────────────────────────────────────────────┤
│ ▌CxC      │  Últimas facturas                                        │
│ Pérez J.  │  Numero  Cliente      Monto       Estado                 │
│ vence 28d │  00125   Juan Pérez   $ 1.200,00  Pendiente              │
│           │  00124   M&M C.A.     $   450,00  Saldada                │
└───────────┴──────────────────────────────────────────────────────────┘
   Tip: ↻ Actualizar refresca todos los datos · 📌 Fija/libera la gráfica   [Cerrar]
```

### Strip de KPIs (franja superior)

| KPI | Qué muestra |
|---|---|
| **VENTAS HOY** | Total facturado hoy (en USD). |
| **MES** | Total facturado en el mes en curso. |
| **PEND** | Cantidad de facturas con estado *Pendiente*. |
| **STK** | Cantidad de productos por debajo del stock mínimo. |
| **CxC** | Saldo total por cobrar (USD). |
| **BCV / PAR** | Tasa BCV y paralelo del día. Sello de frescura: `● actualizada hoy` (verde) · `● 1 día(s) atrás` (ámbar) · `● 3 días atrás` (rojo) · `sin tasa registrada` (rojo). |

> Al lado del bloque BCV/PAR está el botón **↻ Actualizar** que recarga todos los datos.

### Panel de alertas (izquierda)

Lista vertical de alertas activas. Cada alerta muestra:

- **Categoría** (Stock, CxC, Mantenimiento) en color codificado.
- **Texto principal** (qué pasó).
- **Detalle** (datos específicos).

Severidad: 🔴 alta · 🟠 media · ⚪ baja.

### Gráfica rotativa (centro derecha)

Muestra **una de cuatro gráficas** que rotan automáticamente día a día:

| # | Gráfica | Lo que muestra |
|---|---|---|
| 1 | **Ventas diarias del mes** | Línea con el total facturado por día del mes en curso. |
| 2 | **Ventas mensuales** (último año) | Barras verticales por mes. |
| 3 | **Top 10 productos vendidos** | Barras horizontales con los productos más vendidos. |
| 4 | **Estado de facturas / CxC** | Donut con la distribución entre Saldada / Pendiente / Anulada. |

**Controles** (sobre la gráfica):

| Control | Qué hace |
|---|---|
| **‹ ›** | Cambia a la gráfica anterior / siguiente. |
| **● ○ ○ ○** | Indicador de qué gráfica está visible. |
| **📌 Fijar** | "Pin" — fija la gráfica para que no rote más. Volvé a hacer clic para liberar (`📌 Fijada` cambia a `📌 Fijar`). |

### Últimas facturas (centro inferior)

Tabla con los últimos documentos emitidos:

| Columna | Significado |
|---|---|
| **Numero** | Correlativo de la factura |
| **Cliente** | Cliente al que se le emitió |
| **Monto** | Total del documento |
| **Estado** | Saldada · Pendiente · Anulada |

## Casos de uso

### Caso 1 — Revisión matinal

**Pasos**:

1. Abrir **Dashboard** desde el menú principal.
2. Mirar la franja de KPIs:
   - Ventas Hoy: ¿es 0 a media mañana? Probable problema de facturación.
   - STK: ¿subió respecto a ayer? Revisar el panel de alertas.
   - BCV: si dice `sin tasa` o `2 días atrás`, ir a *Tasa de cambio → Actualizar*.
3. Revisar el panel de **Alertas activas** — items críticos primero.
4. Cerrar.

### Caso 2 — Cambiar la gráfica visible

**Pasos**:

1. Hacer clic en **›** sobre la gráfica para pasar a la siguiente (o **‹** para la anterior).
2. Si querés que se quede en esa vista los próximos días, hacer clic en **📌 Fijar**.

**Resultado**: la elección se recuerda entre sesiones. Para volver a la rotación automática, clic de nuevo en el botón (vuelve a decir `📌 Fijar`).

### Caso 3 — Refrescar manualmente

**Cuándo**: acabás de emitir una factura o cargar la tasa y querés ver el dato actualizado sin reabrir.

**Pasos**:

1. Hacer clic en **↻ Actualizar** (arriba a la derecha del strip).

**Resultado**: todos los KPIs, alertas, gráfica y tabla se recargan.

## Validaciones y errores frecuentes

| Mensaje en pantalla | Qué significa | Cómo se resuelve |
|---|---|---|
| `Error al cargar dashboard: …` | Falla al consultar los datos (BD inaccesible, etc.) | Reintentar con **↻ Actualizar**. Si persiste, llamar al administrador. |
| `sin tasa registrada` (en el sello BCV) | No hay registro de tasa hoy ni días anteriores recientes | Ir a *Tasa de cambio → Actualizar* y cargarla. |
| `Cargando…` (en la gráfica) | Aún no llegaron los datos del backend | Esperar; si tarda más de 10 segundos, **↻ Actualizar**. |
| `Sin alertas activas` (en el panel) | No hay items pendientes — situación deseable | Es informativo. |

## Consejos

- **El Dashboard se abre en su propia ventana** — el menú principal sigue detrás. Para cerrar, **Cerrar** o ← **Volver al menú**.
- **No descontés en USD** si el sello BCV está en rojo: la conversión queda incompleta.
- La rotación de la gráfica **avanza una posición por día** automáticamente. Si querés ver siempre la misma (p. ej. Top productos), fijala con 📌.

## Pantallas relacionadas

- **`FrmAlertas`** — fuente de los items del panel de alertas.
- **`FrmFacturas`** — origen de las "Últimas facturas".
- **`FrmActualizarTasa`** — para cargar la tasa cuando el sello está rojo.
- **`FrmRentabilidad`** / **`FrmFlujoCaja`** / **`FrmLibroIVA`** — reportes detallados que complementan los KPIs.
