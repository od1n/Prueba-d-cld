# `FrmRegistroProduccionDia` — Registro diario de producción

> Pantalla principal de planta: registra cuántas piezas se hicieron por molde y cuánto material se consumió en el día. Calcula la merma estimada y la confirma para descontarla de inventario.

## Cómo llegar

- **Ruta**: *Menú principal → Producción → Registro diario* (también accesible desde *Transacciones*).
- **Permiso requerido**: `IngresoProduccion` (o `Admin`).

## Para qué se usa

Es la pantalla operativa **del día** para el personal de planta. Cada jornada se registra:

- Qué moldes trabajaron y cuántas piezas salieron de cada uno (separadas en **primera** y **segunda** calidad si es material **virgen**, o en **reciclado** si se trabaja con material reciclado).
- Cuánto material (insumo en kg) se cargó a la línea.
- Una observación opcional del turno.

Al guardar, el sistema calcula la **merma estimada** (kg de material consumido vs. peso teórico de las piezas producidas) y propone registrarla automáticamente.

## Antes de empezar

- Tener cargados los **moldes** del día en *Archivos → Moldes* y, sobre todo, sus **productos asociados** en *Productos por molde* (1ra, 2da, reciclado). Sin esto, no se puede guardar.
- Tener registrados los **insumos / materia prima** que se usan (resina virgen, reciclado, etc.) como productos activos en *Archivos → Productos del proveedor*.
- Saber qué tipo de material se va a registrar en el turno: **Virgen** o **Reciclado**.

## Qué ves en la pantalla

```
┌──────────────────────────────────────────────────────────────────────────┐
│ Registro de Produccion Diaria                          [? Ayuda][Cerrar] │
├──────────────────────────────────────────────────────────────────────────┤
│ Fecha [28/04]   Material ( ) Virgen  ( ) Reciclado                       │
│ Observacion: [_________________]              [Guardar y Confirmar Merma]│
├──────────────────────────────────────────────────────────────────────────┤
│ Resumen: Primera 240   Segunda 30   Peso teórico 27,5 kg   Merma 1,8 kg  │
├──────────────────────────────────┬───────────────────────────────────────┤
│  Producción por Molde   [+ Molde]│  Material Utilizado (mín. 1)          │
│  [Quitar]                        │                                       │
│  Molde │ Primera │ Segunda │ ... │  Insumo 1 *: [______]   [__]  kg      │
│  ──────┼─────────┼─────────┼───  │  Insumo 2  : [______]   [__]  kg      │
│  M01 — Tapa     │  120 │ 12 │... │  Insumo 3  : [______]   [__]  kg      │
│  M02 — Pote     │  120 │ 18 │... │                                       │
└──────────────────────────────────┴───────────────────────────────────────┘
   Tip: Configura los productos de cada molde en Tabla de Moldes → Productos Molde
```

### Barra superior

| Control | Qué hace |
|---|---|
| **Fecha** | Día que se está registrando. Si ya hay un registro guardado de ese día, se carga para edición. |
| **Virgen / Reciclado** | Selecciona el tipo de material del turno. Cambia las columnas visibles del grid (1ra/2da en Virgen, Reciclado en Reciclado). |
| **Observación** | Texto libre del turno. |
| **Guardar y Confirmar Merma** | Guarda la producción y abre el diálogo de confirmación de merma. |

### Panel izquierdo — Producción por molde

| Columna | Significado |
|---|---|
| **Molde** | Desplegable con los moldes registrados. |
| **Primera** | Piezas de **primera calidad** producidas (sólo en material virgen). |
| **Segunda** | Piezas de **segunda calidad** producidas (sólo en material virgen). |
| **Reciclado** | Piezas hechas con material reciclado (sólo en material reciclado). |
| **Productos** | Información del producto que sale del molde (`1a:COD  2a:COD` o `Rec:COD`). Aparece `⚠ sin configurar` si al molde le faltan productos asociados. |

Botones: **+ Molde** agrega una fila, **Quitar** elimina la fila seleccionada.

### Panel derecho — Material Utilizado

Hasta **3 insumos** por turno. **El primero es obligatorio.** Para cada uno se elige el insumo del desplegable y se ingresa la cantidad en **kg**.

### Banda de resumen

Muestra los totales del turno y la **merma estimada**:

- En verde si la merma es positiva (esperable).
- En rojo si la merma es negativa (el peso teórico es mayor que el material cargado, indicando un error de pesos en inventario).

## Casos de uso

### Caso 1 — Registrar la producción del día (material virgen)

**Cuándo**: al cierre del turno productivo, con material virgen.

**Pasos**:

1. Verificar la **fecha** (por defecto hoy).
2. Asegurarse de que **Virgen** esté marcado.
3. (Opcional) Llenar **Observación** del turno.
4. En el panel izquierdo, hacer clic en **+ Molde** y seleccionar el molde que trabajó. Repetir por cada molde.
5. Para cada fila de molde, ingresar **Primera** y **Segunda** (cantidades en piezas).
6. En el panel derecho, seleccionar el **Insumo 1** y poner la cantidad consumida en **kg**. Si se usaron más insumos, llenar Insumo 2 e Insumo 3.
7. Verificar la franja de resumen: revisar **Peso teórico** y **Merma est.**.
8. Hacer clic en **Guardar y Confirmar Merma**.
9. En el diálogo emergente leer el detalle de **materiales consumidos** vs **peso teórico** y la **merma**:
   - Si hay merma positiva: clic en **Sí, registrar merma** para descontarla automáticamente del inventario, o **No registrar** si se va a ajustar manual.
   - Si la merma es 0 o negativa: solo aparece **Aceptar**.

**Resultado**: el día queda guardado, las piezas se suman al kardex (Kardex A entra producto terminado, Kardex B sale insumo).

### Caso 2 — Registrar producción con material reciclado

**Pasos**: idénticos al Caso 1, pero seleccionando **Reciclado** en la barra superior. El grid pasa a mostrar la columna **Reciclado** en vez de Primera/Segunda.

### Caso 3 — Corregir el registro de un día anterior

**Pasos**:

1. Cambiar la **fecha** al día que se quiere editar.
2. La pantalla se carga con el registro existente.
3. Modificar moldes, cantidades o materiales.
4. **Guardar y Confirmar Merma** otra vez.

## Validaciones y errores frecuentes

| Mensaje en pantalla | Qué significa | Cómo se resuelve |
|---|---|---|
| `Ingresa una cantidad válida (≥ 0).` | Se escribió un valor no numérico o negativo en una columna de cantidad | Corregir el número |
| `Agrega al menos un molde.` | Se intentó guardar sin filas de molde | Hacer clic en **+ Molde** y completar |
| `Ingresa al menos un material con cantidad mayor a 0.` | El bloque de material está vacío | Seleccionar Insumo 1 y poner kg |
| `El molde 'XXX' no tiene productos configurados.` | El molde no tiene definidos sus productos terminados | Ir a *Archivos → Moldes → Productos Molde* y asignarlos |
| `Error al guardar: …` | Falla técnica al guardar (BD u otra) | Reintentar; si persiste, llamar al administrador |
| `⚠ Merma negativa: el peso teórico supera el material registrado` | Los pesos por unidad de los productos en inventario están mal cargados | Revisar el peso (kg) de los productos en *Archivos → Inventario* |

## Consejos

- **Configurá los productos de cada molde una sola vez** en *Archivos → Moldes → Productos Molde*. Esto evita el error "sin productos configurados".
- **Verificá los pesos por pieza** del catálogo de inventario. De ahí sale el peso teórico y, por lo tanto, la merma.
- **Confirmá la merma al guardar** — si no se confirma, queda registrado el día pero sin descontar la merma del kardex de insumos.
- Cambiar la **fecha** trae automáticamente el registro guardado de ese día (si existe), permitiendo correcciones.

## Pantallas relacionadas

- **`FrmMoldes`** — alta de moldes y configuración de sus productos terminados.
- **`FrmInventario`** — peso por pieza usado para el cálculo de merma.
- **`FrmKardex`** — donde se ven las entradas/salidas que genera este registro.
- **`FrmMermas`** — reporte consolidado de mermas por período.
- **`FrmIngresoProduccion`** — pantalla legacy más simple, ya no recomendada.
