# `FrmTasaCambio` — Tasa de cambio (registro y consulta)

> Pantalla principal del módulo de tasa de cambio. Permite cargar la tasa BCV y paralelo del día manualmente, ver el histórico de los últimos 60 días y abrir la trazabilidad de qué facturas usaron qué tasa.

## Cómo llegar

- **Ruta**: *Menú principal → barra inferior → $ Tasa de cambio*.
- **Atajo**: ninguno.
- **Permiso requerido**: `TasaConsulta` (Ventas, Almacén, Contabilidad y Admin lo tienen). Para guardar una tasa nueva se requiere `TasaIngreso`; para sobrescribir una ya registrada, `TasaModificar` (solo Contabilidad y Admin).

## Para qué se usa

- **Cargar manualmente** la tasa BCV y paralelo de un día (hoy o anterior).
- **Consultar** el histórico de tasas registradas (últimos 60 días).
- Abrir la **Trazabilidad**: qué facturas usaron cada tasa.

> ⚠ Para la **carga automática** del día (consulta a dolarapi.com / BCV) usar `FrmActualizarTasa`. Esta pantalla es para registro manual o consulta histórica.

## Antes de empezar

- Conocer la tasa **BCV oficial** y **paralelo** del día (de la fuente que el negocio acepte: BCV.org.ve, prensa, dolarapi).

## Qué ves en la pantalla

```
┌──────────────────────────────────────────────────────────────┐
│ Tasa de Cambio — BCV y Paralelo                              │  ← header verde
├──────────────────────────────────────────────────────────────┤
│ Fecha: [28/04/2026]  Tasa BCV: [36,50]  Paralelo: [41,20]    │
│                                            [ Guardar tasa ]  │
├──────────────────────────────────────────────────────────────┤
│   Tasa vigente (28/04/2026): BCV = 36,50 Bs/$  |  PAR = 41,20│  ← banner verde
├──────────────────────────────────────────────────────────────┤
│  Fecha       TasaBCV    TasaParalelo    Usuario              │  ← grilla
│  28/04/2026  36,50      41,20           admin                │
│  27/04/2026  36,40      41,00           admin                │
│  ...                                                         │
├──────────────────────────────────────────────────────────────┤
│ [▤ Trazabilidad]                                  [ Cerrar ] │
└──────────────────────────────────────────────────────────────┘
```

### Panel de entrada (arriba)

| Campo / botón | Para qué sirve |
|---|---|
| **Fecha** | Fecha de la tasa (por defecto, hoy). Se puede cambiar para registrar una tasa atrasada. |
| **Tasa BCV** | Valor BCV oficial en Bs/USD. **Obligatorio mayor a cero.** |
| **Tasa Paralelo** | Valor del mercado paralelo en Bs/USD (puede ser 0 si no aplica). |
| **Guardar tasa** | Inserta o actualiza la tasa para esa fecha. |

### Banner verde (tasa vigente)

Muestra la tasa más reciente registrada con su fecha. Si no hay ninguna, dice `No hay tasa registrada aún.`

### Grilla (histórico)

Lista las tasas de los últimos 60 días con: Fecha · TasaBCV · TasaParalelo · Usuario que la cargó.

> **Tip:** doble clic en una fila precarga sus valores en el panel de entrada para revisar o editarlos.

### Botones inferiores

| Botón | Qué hace |
|---|---|
| **▤ Trazabilidad** | Abre `FrmTrazabilidadTasa` con el histórico detallado y las facturas afectadas por cada tasa. |
| **Cerrar** | Cierra la ventana. |

## Casos de uso

### Caso 1 — Cargar la tasa de hoy manualmente

**Cuándo**: el sistema no consultó automática (sin internet) o querés revisar antes de aceptar.

**Pasos**:

1. Verificar que la **Fecha** sea la de hoy.
2. Escribir la **Tasa BCV** (ej. `36,50`).
3. Escribir la **Tasa Paralelo** (ej. `41,20`).
4. **Guardar tasa**.

**Resultado**: aparece en la grilla y en el banner *Tasa vigente*. Todas las facturas que se emitan hoy usarán este valor para conversión Bs↔USD.

### Caso 2 — Cargar una tasa atrasada (de un día sin registro)

**Pasos**:

1. Cambiar **Fecha** al día que falta.
2. Cargar BCV y paralelo.
3. **Guardar tasa**.

**Resultado**: la tasa se inserta en el día indicado. No afecta facturas ya emitidas (mantienen la tasa con la que se guardaron originalmente).

### Caso 3 — Editar una tasa ya registrada (sólo Contabilidad/Admin)

**Pasos**:

1. **Doble clic** en la fila de la grilla.
2. Modificar **Tasa BCV** o **Tasa Paralelo**.
3. **Guardar tasa**.

**Resultado**: el sistema sobrescribe el valor (queda en auditoría con el usuario que lo cambió). **Las facturas que ya se emitieron con la tasa anterior no se recalculan**.

### Caso 4 — Ver qué facturas usaron una tasa

**Pasos**:

1. Hacer clic en **▤ Trazabilidad** (en el footer).

**Resultado**: se abre `FrmTrazabilidadTasa` (ver su ficha).

## Validaciones y errores frecuentes

| Mensaje en pantalla | Qué significa | Cómo se resuelve |
|---|---|---|
| `La tasa BCV debe ser mayor que cero.` | Dejaste el campo BCV en 0 | Ingresar el valor real |
| Mensaje de error genérico al guardar | Falla en BD o falta de permiso | Si decís permiso, pedir al admin que asigne `TasaIngreso` o `TasaModificar`. Si es BD, reintentar y avisar. |

## Consejos

- **Cargá la tasa al inicio del día**, antes de emitir facturas. Si no, las primeras del día quedan con la tasa de ayer (o sin tasa, si era el primer día).
- Si dudás del valor, **`Buscar automática`** en `FrmActualizarTasa` consulta dolarapi.com — más confiable y deja registro `AUTO_API`.
- **Editar una tasa ya usada** es una operación sensible: revisar antes la trazabilidad para entender el impacto. Toda edición queda en auditoría.

## Pantallas relacionadas

- **`FrmActualizarTasa`** — diálogo automático que se abre al primer login del día si no hay tasa.
- **`FrmTrazabilidadTasa`** — auditoría detallada (origen de cada tasa y facturas afectadas).
- **`FrmFacturas`** — usa la tasa BCV vigente para conversiones Bs↔USD.
- **`MenuPrincipalV2`** — la franja superior muestra la tasa BCV/PAR y un botón **↻ Tasa** para abrir esta pantalla.
