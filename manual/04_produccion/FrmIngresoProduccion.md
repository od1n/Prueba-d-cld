# `FrmIngresoProduccion` — Ingreso de producción (legacy)

> ⚠ **Pantalla legacy.** Reemplazada por **`FrmRegistroProduccionDia`** (*Producción → Registro diario*), que maneja moldes, materiales y merma. Se mantiene sólo por compatibilidad con registros antiguos.

## Cómo llegar

- **Ruta**: *Menú principal → Producción → Ingreso de Producción*.
- **Permiso requerido**: `IngresoProduccion` (o `Admin`).

## Para qué se usa

Pantalla simple de **alta manual** de un lote producido (producto, lote, cantidad, responsable, estado). No diferencia primera/segunda calidad, no calcula merma ni vincula moldes.

> Para uso normal, usá **`FrmRegistroProduccionDia`**. Esta ficha sirve solo si tu empresa todavía registra producción de la forma antigua o necesita consultar lotes ya cargados.

## Qué ves en la pantalla

```
┌────────────────────────────────────────────────────────┐
│ Ingreso de Produccion           [+ Nuevo][Calidad]     │
│                                 [Editar][Eliminar]     │
├────────────────────────────────────────────────────────┤
│ Id │ Producto │ Lote │ Cant │ Unidad │ Fecha │ Resp │ E│
│ 12 │ Tapa A   │ L-01 │  500 │ Piezas │ 28/04 │ Juan │ ✓│
├────────────────────────────────────────────────────────┤
│ Registros: 12   Unidades: 6.500   Completados: 9   ... │
└────────────────────────────────────────────────────────┘
```

### Botones

| Botón | Qué hace |
|---|---|
| **+ Nuevo** | Abre el diálogo para registrar un lote. |
| **Calidad** | Abre la pantalla de **Control de Calidad**. |
| **Editar** | Edita el registro seleccionado. |
| **Eliminar** | Borra el registro seleccionado (sin confirmación adicional al "¿Eliminar registro #N?"). |
| **Cerrar** | Sale de la pantalla. |

### Tabla

| Columna | Significado |
|---|---|
| **Id** | Número interno del registro |
| **Producto** | Texto libre con el nombre del producto |
| **Lote** | Identificador de lote |
| **Cantidad** | Unidades producidas (entero) |
| **Unidad** | `Piezas`, `Kg`, `Litros`, `Cajas`, `Metros`, `Toneladas` |
| **Fecha** | Fecha de producción |
| **Responsable** | Quién produjo |
| **Estado** | `En proceso` (amarillo) · `Completado` (verde) · `Rechazado` (rojo) |

### Diálogo Nuevo / Editar Ingreso

Campos:

- **Producto** (obligatorio).
- **Lote**.
- **Cantidad** (mínimo 1).
- **Unidad** — desplegable: Piezas, Kg, Litros, Cajas, Metros, Toneladas.
- **Fecha**.
- **Responsable**.
- **Estado** — desplegable: En proceso, Completado, Rechazado.
- **Observación** — texto libre.

## Casos de uso

### Caso 1 — Registrar un lote nuevo (legacy)

1. Hacer clic en **+ Nuevo**.
2. Llenar **Producto**, **Lote**, **Cantidad**, **Unidad**, **Fecha**, **Responsable**, **Estado**.
3. Hacer clic en **Guardar**.

**Resultado**: el lote queda en la tabla. Si el **Producto** coincide con un código de inventario, se intenta registrar una entrada en el **Kardex** con motivo `Produccion - <Lote>`. Si el producto no existe en inventario, se omite el movimiento sin error.

### Caso 2 — Editar o eliminar un lote

1. Seleccionar la fila.
2. **Editar** abre el diálogo con los datos cargados; **Eliminar** pide confirmación (`Eliminar registro #N?`).

## Validaciones y errores frecuentes

| Mensaje en pantalla | Qué significa | Cómo se resuelve |
|---|---|---|
| `Ingrese el producto.` | El campo Producto quedó vacío | Llenar el campo |
| `Eliminar registro #N?` | Confirmación al borrar | Sí / No |

## Consejos

- **Migrá tus registros nuevos a `FrmRegistroProduccionDia`** — separa primera/segunda, vincula con moldes y calcula merma automáticamente.
- Esta pantalla **no descuenta materia prima**. Si querés trazabilidad de insumos consumidos, usá la pantalla nueva.

## Pantallas relacionadas

- **`FrmRegistroProduccionDia`** — reemplazo recomendado.
- **`FrmControlCalidad`** — accesible desde el botón **Calidad**.
- **`FrmKardex`** — donde aparece la entrada generada por el alta de un lote (si el producto existe en inventario).
