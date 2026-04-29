# `FrmTrazabilidadTasa` — Trazabilidad de tasas de cambio

> Pantalla de auditoría. Muestra el histórico de tasas registradas (últimos 60 días) con su origen (automática, manual, editada) y, al seleccionar una tasa, lista las facturas que la usaron.

## Cómo llegar

- **Ruta**: *Menú principal → $ Tasa de cambio → ▤ Trazabilidad* (botón en el footer).
- **Atajo**: ninguno.
- **Permiso requerido**: `TasaConsulta` (Ventas, Almacén, Contabilidad y Admin lo tienen).

## Para qué se usa

Responde dos preguntas comunes:

1. **"¿Quién y cómo se cargó esta tasa?"** — distingue tasas obtenidas de la API, ingresadas a mano o editadas.
2. **"¿Qué facturas se emitieron con esta tasa?"** — al seleccionar una fila, muestra las facturas afectadas y el total impactado.

Es la herramienta de control para entender impactos cuando se ajusta una tasa o se discute un valor con un cliente.

## Antes de empezar

- Tener al menos algunas tasas y facturas registradas (si no, las grillas aparecen vacías).

## Qué ves en la pantalla

```
┌──────────────────────────────────────────────────────────────────────────┐
│  Trazabilidad de Tasas de Cambio                                         │  ← header
│  Histórico, origen y facturas afectadas por cada tasa.                   │
├────────────────────────────────┬─────────────────────────────────────────┤
│ Histórico de tasas             │ Facturas que usaron esta tasa           │  ← cards
│ (últimos 60 días)              │ Selecciona una tasa para ver las...     │
│                                │                                         │
│ Fecha     BCV    Paralelo  Origen      Usuario  Registrada               │
│ 28/04/26  36,50  41,20  ● Automático   admin    28/04 08:14              │
│ 27/04/26  36,40  41,00  ◐ API editado  juan     27/04 09:20              │
│ 26/04/26  36,30  40,80  ○ Manual       maria    26/04 11:00              │
│ ...                            │ Numero  Fecha       Cliente  Total      │
│                                │ 00125   28/04/26    J.Pérez  $ 1.200    │
│                                │ 00126   28/04/26    M&M      $   450    │
├────────────────────────────────┴─────────────────────────────────────────┤
│  Tasa seleccionada: ID 18 · 23 facturas afectadas · Total: $ 12.480,00   │  ← resumen
│                                                              [ Cerrar ]  │
└──────────────────────────────────────────────────────────────────────────┘
```

### Panel izquierdo — Histórico de tasas

| Columna | Significado |
|---|---|
| **Fecha** | Día que cubre la tasa |
| **BCV** | Valor BCV registrado |
| **Paralelo** | Valor paralelo registrado |
| **Origen** | Cómo se cargó (ver tabla abajo) |
| **Usuario** | Quién la registró |
| **Registrada** | Fecha y hora exacta del registro |

### Origen (símbolos)

| Símbolo | Significado |
|---|---|
| `● Automático (API)` | La tasa vino de dolarapi.com y se aceptó tal cual |
| `◐ API editado` | La API respondió pero el usuario modificó algún valor antes de guardar |
| `○ Manual` | Sin consulta a API (sin internet o consulta directa); ingresada a mano |
| `▤ Importado` | Importada desde otro sistema o respaldo (raro) |

### Panel derecho — Facturas que usaron la tasa

Aparece tras seleccionar una fila del panel izquierdo. Columnas:

| Columna | Significado |
|---|---|
| **Numero** | Número de factura |
| **Fecha** | Fecha de emisión |
| **Cliente** | Código del cliente |
| **Total** | Total del documento (USD) |

### Resumen inferior

Muestra: ID de la tasa seleccionada · cantidad de facturas afectadas · total económico impactado.

## Casos de uso

### Caso 1 — Auditar quién cargó la tasa de un día

**Pasos**:

1. Buscar la fecha en el histórico (panel izquierdo).
2. Mirar las columnas **Origen**, **Usuario** y **Registrada**.

**Resultado**: sabés si fue automática o manual, quién la metió y a qué hora.

### Caso 2 — Ver el impacto de una tasa en facturación

**Pasos**:

1. Hacer clic en la fila de la tasa.
2. El panel derecho carga las facturas que la usaron.
3. La barra inferior muestra el total económico afectado.

**Resultado**: tenés un número claro: "esta tasa afectó N facturas por $X".

### Caso 3 — Identificar tasas sospechosas

**Pasos**:

1. Recorrer el histórico buscando filas con origen `◐ API editado`.
2. Para cada una, comparar el valor con el del día anterior y siguiente.
3. Si un valor se ve muy distinto, abrir `FrmLogAuditoria` y filtrar por usuario y acciones de tasa para ver el detalle.

## Validaciones y errores frecuentes

| Mensaje en pantalla | Qué significa | Cómo se resuelve |
|---|---|---|
| `Tu rol (X) no tiene permiso para consultar el histórico de tasas.` | Tu rol no tiene `TasaConsulta` | Pedir al admin que asigne el permiso |
| `Error al cargar tasas: …` | Falla en BD | Reintentar; si persiste, llamar al admin |
| `Error al cargar facturas: …` | Falla al traer facturas asociadas | Reintentar |
| `Selecciona una tasa para ver las facturas asociadas.` | El panel derecho está vacío | Hacer clic en una fila del histórico |

## Consejos

- **Las tasas con origen `◐ API editado`** son las que requieren más atención: alguien decidió no aceptar el valor de la API, conviene saber por qué.
- Si el resumen inferior dice **"0 facturas afectadas"**, esa tasa no impactó ninguna venta (o las facturas se anularon).
- Para investigar más a fondo, complementar con `FrmLogAuditoria` filtrado por las acciones `TASA_GUARDADA` y `TASA_EDITADA`.

## Pantallas relacionadas

- **`FrmTasaCambio`** — desde donde se abre esta pantalla (botón ▤ Trazabilidad).
- **`FrmActualizarTasa`** — registro automático que produce los `● Automático (API)` y `◐ API editado`.
- **`FrmLogAuditoria`** — log completo del sistema con detalle adicional.
- **`FrmFacturas`** — facturas que aparecen en el panel derecho al seleccionar una tasa.
