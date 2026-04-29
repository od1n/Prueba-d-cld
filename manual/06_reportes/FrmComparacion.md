# `FrmComparacion` — Comparación entre dos períodos

> Compara dos períodos cualesquiera en métricas clave: ventas, compras, gastos, utilidad, margen y nómina. Muestra la diferencia, la variación porcentual y la tendencia (↑ ↓ →) con un gráfico de barras lado a lado.

## Cómo llegar

- **Ruta**: *Menú principal → Reportes → Comparación*.
- **Permiso requerido**: `Reportes` (o `Admin`).

## Para qué se usa

Responder preguntas tipo "¿cómo nos fue este mes vs el mes anterior?" o "¿cómo va este trimestre vs el mismo del año pasado?". El sistema arma una tabla de métricas y un gráfico comparativo.

Por defecto carga **mes actual vs mes anterior**.

## Qué ves en la pantalla

```
┌──────────────────────────────────────────────────────────────────────────┐
│ Comparacion de Periodos — Ventas, Compras y Gastos                       │
├──────────────────────────────────────────────────────────────────────────┤
│ ┌Periodo 1 (actual)─────────┐ ┌Periodo 2 (comparar con)──┐               │
│ │ Desde [01/04] Hasta [28/04]│ │ Desde [01/03] Hasta [31/03]│   [Comparar]│
│ └─────────────────────────────┘ └─────────────────────────────┘  [PDF]   │
├──────────────────────────────────────────────────────────────────────────┤
│  Comparacion por Categorias (USD)                                        │
│   ████ █  Ventas                                  ■ Periodo 1            │
│   ███  █  Compras                                 ■ Periodo 2            │
│    █  █   Gastos                                                         │
├──────────────────────────────────────────────────────────────────────────┤
│ Periodo 1: 01/04 – 28/04   Periodo 2: 01/03 – 31/03                      │
│ Metrica         │   P1     │   P2     │ Diferencia │ Variacion │ ↑↓→     │
│ ── VENTAS                                                                │
│ Total Facturado │ $12.500  │ $10.000  │ +$2.500    │ +25,0%    │ ↑       │
│ N° Facturas     │   85     │   72     │ +13        │ +18,0%    │ ↑       │
│ Ticket Promedio │ $147,06  │ $138,89  │ +$8,17     │ +5,9%     │ ↑       │
│ ── COMPRAS                                                               │
│ ── GASTOS                                                                │
│ ── RESULTADO                                                             │
│ Utilidad Bruta  │ $4.250   │ $3.500   │ +$750      │ +21,4%    │ ↑       │
│ ── NOMINA                                                                │
└──────────────────────────────────────────────────────────────────────────┘
```

### Toolbar

| Control | Detalle |
|---|---|
| **Periodo 1 (actual)** — Desde / Hasta | Por defecto desde el día 1 del mes actual hasta hoy. |
| **Periodo 2 (comparar con)** — Desde / Hasta | Por defecto el mes anterior completo. |
| **Comparar** | Recalcula. |
| **Exportar PDF** | Guarda el comparativo en PDF. |
| **? Ayuda** | Abre esta ficha del manual. |

### Gráfico (mitad superior)

Cinco grupos de barras: **Ventas**, **Compras**, **Gastos**, **Utilidad**, **Nómina**. Azul = Periodo 1, rojo = Periodo 2.

### Tabla (mitad inferior)

Filas agrupadas por sección:

| Sección | Métricas |
|---|---|
| **VENTAS** | Total Facturado, N° de Facturas, Ticket Promedio, IVA Ventas |
| **COMPRAS** | Total Compras, N° de Compras, IVA Compras |
| **GASTOS** | Total Gastos |
| **RESULTADO** | Utilidad Bruta, Margen Bruto % |
| **NOMINA** | Total Nómina Pagada, Períodos Procesados |

| Columna | Significado |
|---|---|
| **Métrica** | Nombre del KPI |
| **Periodo 1 / Periodo 2** | Valores en cada período (USD si aplica) |
| **Diferencia** | P1 − P2 |
| **Variación** | Porcentaje, formato `+X,X%` o `-X,X%` |
| **Tendencia** | `↑` mejor · `↓` peor · `→` igual. Color verde si la tendencia es buena, rojo si es mala. |

> **Para ventas y utilidad**, "más" es bueno (verde si ↑). **Para compras, gastos y nómina**, "menos" es bueno (verde si ↓).

## Casos de uso

### Caso 1 — Mes actual vs mes anterior (default)

1. Abrir la pantalla — ya carga este comparativo automáticamente.
2. Mirar la columna **Tendencia** para identificar mejoras o retrocesos.

### Caso 2 — Mismo trimestre del año pasado

1. **Periodo 1**: 01/01/2026 al 31/03/2026.
2. **Periodo 2**: 01/01/2025 al 31/03/2025.
3. **Comparar**.

### Caso 3 — Exportar PDF

Hacer clic en **Exportar PDF**. Guarda con nombre `Comparacion_AAAAMMDD.pdf` y se abre automáticamente.

## Validaciones y errores frecuentes

| Mensaje en pantalla | Qué significa | Cómo se resuelve |
|---|---|---|
| `Error al comparar: …` | Falla técnica al consultar la BD | Reintentar; si persiste, llamar al administrador |
| `Error al generar PDF: …` | Falla al armar el PDF | Reintentar |

## Consejos

- **Períodos del mismo largo** (mes vs mes, trimestre vs trimestre) hacen la comparación más justa. Si comparás 30 días vs 15 días, las diferencias absolutas no son comparables.
- **Tendencia verde** no siempre significa "todo bien": una caída en compras puede deberse a desabastecimiento, no a eficiencia.
- Los **separadores en gris** (── VENTAS, ── COMPRAS…) son sólo visuales para agrupar.
- El **margen bruto %** se compara, pero recordá que es un porcentaje — no usar la "diferencia absoluta" como dólares.

## Pantallas relacionadas

- **`FrmRentabilidad`** — análisis profundo del Periodo 1 a nivel de producto.
- **`FrmFlujoCaja`** — proyección hacia adelante (no comparativa).
- **`FrmDashboardV2`** — KPIs globales del día.
