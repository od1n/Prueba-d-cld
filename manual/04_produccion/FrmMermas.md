# `FrmMermas` — Control de Mermas y Desperdicios

> Reporte que compara el consumo **teórico** de materia prima vs el **real**, para todas las órdenes de producción completadas en el período. Identifica las órdenes y materias con mayor desperdicio.

## Cómo llegar

- **Ruta**: *Menú principal → Producción → Mermas* (también desde *Reportes*).
- **Permiso requerido**: `Almacén` o `Reportes` (o `Admin`).

## Para qué se usa

Permite responder preguntas como:

- ¿Cuál fue la merma global del último trimestre?
- ¿Qué órdenes tuvieron desperdicio crítico (>15%)?
- ¿Qué materia prima es la que más se desperdicia?
- Generar el **PDF de mermas** del período para reuniones operativas.

## Antes de empezar

- Tener órdenes de producción ya **completadas** en *Producción → Órdenes de Producción*. La merma se calcula con base a esas órdenes.

## Qué ves en la pantalla

```
┌────────────────────────────────────────────────────────────────────────┐
│ Control de Mermas y Desperdicios — Ordenes Completadas                 │
├────────────────────────────────────────────────────────────────────────┤
│ Desde: [01/02]  Hasta: [28/04]   [Consultar]   [Exportar PDF]          │
├────────────────────────────────────────────────────────────────────────┤
│  ┌─────────────┐ ┌──────────────┐ ┌──────────────┐ ┌────────────────┐  │
│  │ Ordenes     │ │ Merma Total  │ │ % Merma      │ │ Ordenes        │  │
│  │ Analizadas  │ │              │ │ Global       │ │ Criticas (>15%)│  │
│  │     14      │ │   245,30     │ │     7,2%     │ │      2         │  │
│  └─────────────┘ └──────────────┘ └──────────────┘ └────────────────┘  │
├────────────────────────────────────────────────────────────────────────┤
│ Ordenes de Produccion Completadas:                                     │
│ N°Orden │ Producto │ Cierre │ Plan │ Real │ Merma │ % │ Materiales     │
│ OP-0019 │ Pote 5L  │ 04/04  │ 200  │ 198  │ 12,5  │7,5│      4         │
│ OP-0021 │ Tapa 1L  │ 12/04  │ 500  │ 480  │ 35,0  │15,4│     3         │ ←rojo
├────────────────────────────────────────────────────────────────────────┤
│ Detalle de Materias Primas — Teorico vs Real:                          │
│ Codigo │ Material │ Teorico │ Real │ Merma │ % │ Nivel                  │
│ MP-001 │ Resina A │  120,00 │ 132  │ 12,00 │10%│ MEDIO                  │
└────────────────────────────────────────────────────────────────────────┘
```

### Barra superior

| Control | Qué hace |
|---|---|
| **Desde / Hasta** | Filtra el rango de fechas (por defecto últimos 3 meses → hoy). |
| **Consultar** | Recarga los datos. |
| **Exportar PDF** | Genera el PDF del reporte y lo abre. |
| **Cerrar** | Sale de la pantalla. |

### Tarjetas KPI

- **Órdenes Analizadas** — cantidad de órdenes completadas en el rango.
- **Merma Total** — suma de la merma absoluta (en unidades de la materia).
- **% Merma Global** — porcentaje sobre el consumo teórico total.
  - Verde si < 5%, ámbar 5–10%, rojo > 10%.
- **Órdenes Críticas (>15%)** — cuántas órdenes tienen más de 15% de merma.

### Tabla superior — Órdenes completadas

Una fila por orden. Coloreada según la merma:

| Color | Umbral |
|---|---|
| Verde claro | merma ≤ 5% |
| Ámbar claro | merma entre 5% y 15% |
| Rojo claro | merma > 15% |

| Columna | Significado |
|---|---|
| **N° Orden** | Número de la orden |
| **Producto** | Producto fabricado |
| **Fecha Cierre** | Cuándo se completó |
| **Cant. Plan** | Lo planificado |
| **Cant. Real** | Lo realmente producido |
| **Merma Total** | Cantidad mermada (suma de todas las materias) |
| **% Merma** | Porcentaje sobre el teórico |
| **Materiales** | Cuántas materias distintas hubo en la orden |

### Tabla inferior — Detalle por materia

Aparece al **seleccionar una orden** arriba. Para cada materia prima de esa orden:

| Columna | Significado |
|---|---|
| **Codigo / Material** | Identificación de la materia |
| **Consumo Teórico** | Lo que debería haber consumido según el cálculo |
| **Consumo Real** | Lo que efectivamente consumió |
| **Merma** | Diferencia |
| **% Merma** | Porcentaje |
| **Nivel** | `BAJO` (verde) · `MEDIO` (amarillo) · `ALTO` (ámbar) · `CRITICO` (rojo) |

## Casos de uso

### Caso 1 — Consultar la merma del último mes

1. Ajustar **Desde** y **Hasta** al rango deseado.
2. Hacer clic en **Consultar**.
3. Mirar las tarjetas KPI para tener el resumen.
4. Hacer clic en una orden de la tabla superior para ver el desglose por materia abajo.

### Caso 2 — Identificar órdenes problemáticas

1. Las órdenes con fila **roja** son las críticas (>15%).
2. Hacer clic para ver qué materia/s la causaron.
3. Investigar con planta y ajustar el proceso.

### Caso 3 — Generar PDF para reunión operativa

1. Seleccionar el rango.
2. **Consultar**.
3. Hacer clic en **Exportar PDF**.
4. Guardar con el nombre sugerido (`Mermas_AAAAMMDD.pdf`). Se abre automáticamente al guardar.

## Validaciones y errores frecuentes

| Mensaje en pantalla | Qué significa | Cómo se resuelve |
|---|---|---|
| `No hay datos para exportar.` | El rango no tiene órdenes completadas | Ampliar el rango o cerrar más órdenes |
| `Error al consultar: …` | Falla técnica al traer datos | Reintentar; si persiste, llamar al administrador |
| `Error al generar PDF: …` | Falla al armar el archivo | Reintentar; si persiste, llamar al administrador |
| `PDF generado correctamente.` | OK, el archivo se guardó | — |

## Consejos

- Mirar la tabla detalle ayuda a detectar cuál materia es siempre la "culpable" (resina, pigmento, etiqueta…).
- Si una merma global >10%, revisar también el **registro diario de producción**: a veces la merma real se registra ahí y se confirma al cerrar.
- Esta pantalla **es de solo consulta**; no genera ni edita registros de producción.

## Pantallas relacionadas

- **`FrmOrdenesProduccion`** — fuente de los datos (órdenes completadas).
- **`FrmRegistroProduccionDia`** — registro diario que también afecta el kardex de insumos.
- **`FrmInventario`** — pesos por unidad usados para el cálculo teórico.
