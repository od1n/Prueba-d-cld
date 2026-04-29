# `FrmGastos` — Gestión de Gastos (legacy)

> ⚠ **Pantalla legacy.** Reemplazada por **`FrmEgresos`** (*Transacciones → Egresos*), que maneja categorías más amplias, vinculación con proveedores, doble moneda, IVA y formas de pago. Se mantiene sólo para consultar registros antiguos.

## Cómo llegar

- **Ruta**: *Menú principal → Reportes → Gastos* (en versiones viejas también desde *Transacciones*).
- **Permiso requerido**: `Contabilidad` (o `Admin`).

## Para qué se usa

Lista simple de gastos con categoría, monto, fecha, proveedor y estado (`Pendiente` / `Pagado`).

> Para registrar gastos **nuevos**, usá **`FrmEgresos`**. Esta ficha sirve solo para consultar/editar gastos antiguos cargados con la pantalla legacy.

## Qué ves en la pantalla

```
┌─────────────────────────────────────────────────────────────┐
│ Gestion de Gastos                  [+ Nuevo][Editar][Elim.] │
├─────────────────────────────────────────────────────────────┤
│ Id │ Categoria │ Descripcion │ Monto │ Fecha │ Prov. │ Est. │
│ 12 │ Renta     │ Alquiler abr│ $300  │ 02/04 │ Inmob.│Pagado│
├─────────────────────────────────────────────────────────────┤
│ Total: $X | Pagados: $Y | Pendientes: $Z              [Cer] │
└─────────────────────────────────────────────────────────────┘
```

### Botones

| Botón | Qué hace |
|---|---|
| **+ Nuevo** | Abre el diálogo para registrar un gasto. |
| **Editar** | Edita el gasto seleccionado. |
| **Eliminar** | Borra (pide confirmación `Eliminar gasto #N?`). |
| **Cerrar** | Sale de la pantalla. |

### Tabla

| Columna | Significado |
|---|---|
| **Categoria** | `Servicios` · `Nomina` · `Suministros` · `Renta` · `Mantenimiento` · `Otros` |
| **Descripcion** | Texto libre |
| **Monto** | En USD |
| **Fecha** | Fecha del gasto |
| **Proveedor** | Texto libre |
| **Estado** | `Pendiente` · `Pagado` |

### Resumen al pie

```
Total: $X | Pagados: $Y | Pendientes: $Z
```

### Diálogo Nuevo / Editar gasto

| Campo | Detalle |
|---|---|
| **Categoria** | Lista fija (`Servicios`, `Nomina`, `Suministros`, `Renta`, `Mantenimiento`, `Otros`). |
| **Descripcion** | Obligatorio. Texto libre. |
| **Monto ($)** | Hasta 9.999.999. |
| **Fecha** | Por defecto hoy. |
| **Proveedor** | Texto libre. |
| **Estado** | `Pendiente` (default) o `Pagado`. |

## Casos de uso

### Caso 1 — Registrar un gasto (legacy)

1. **+ Nuevo**.
2. Elegir **Categoria**.
3. Llenar **Descripcion**, **Monto**, **Fecha**, **Proveedor**.
4. Elegir **Estado**.
5. **Guardar**.

> Para registrar gastos **nuevos**, lo recomendado es ir a **`FrmEgresos`** (más completo).

### Caso 2 — Editar / eliminar un gasto

1. Seleccionar la fila.
2. **Editar** o **Eliminar** (confirmación `Eliminar gasto #N?`).

## Validaciones y errores frecuentes

| Mensaje en pantalla | Qué significa | Cómo se resuelve |
|---|---|---|
| `Ingrese descripcion.` | Falta la descripción | Llenarla |
| `Eliminar gasto #N?` | Confirmación al borrar | Sí / No |

## Consejos

- **Migrá tus registros nuevos a `FrmEgresos`** — más categorías, formas de pago, IVA, doble moneda y vinculación con proveedores formales.
- Esta pantalla **no aparece en el Libro IVA** porque no maneja base imponible/IVA por separado. Si el gasto debe declararse, registralo en `FrmEgresos`.

## Pantallas relacionadas

- **`FrmEgresos`** — reemplazo recomendado.
- **`FrmCuentasPagar`** — seguimiento de los gastos a pagar.
- **`FrmFlujoCaja`** — donde se proyectan los egresos.
