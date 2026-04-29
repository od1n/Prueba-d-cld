# `FrmCompras` — Compras (deprecada)

> ⚠ **Pantalla deprecada.** Las compras de **materia prima** ahora se registran en **`FrmEgresos`** (*Transacciones → Egresos*) con la categoría **"Materia Prima"** o equivalente. Esta pantalla se mantiene sólo para consultar y editar compras antiguas.

## Cómo llegar

- **Ruta**: *Menú principal → Reportes → Compras* (en algunas versiones también desde *Transacciones*).
- **Permiso requerido**: `Contabilidad` (o `Admin`).

## Para qué se usa

Lista de **compras a proveedores** registradas con la pantalla legacy. Cada compra tiene número correlativo (`C00001`), proveedor, fecha, productos, descuento, IVA, total y días de crédito.

> Para registrar compras **nuevas**, usá **`FrmEgresos`** con la categoría adecuada. Esta pantalla **no debería usarse para nuevos registros**.

## Qué ves en la pantalla

```
┌────────────────────────────────────────────────────────────────────────┐
│ Registro de Compras       [? Ayuda][+ Nueva][Editar][Eliminar]         │
├────────────────────────────────────────────────────────────────────────┤
│ CompraN │ Fecha │ Proveedor │ FormaDePago │ Desc │ IVA   │ Total │ Días│
│ C00125  │ 28/04 │ PROV-001  │ Credito     │  0%  │ $48   │ $348  │ 30  │
├────────────────────────────────────────────────────────────────────────┤
│ Compras: 24 | Total comprado: $12.500 | Abonado: $9.800        [Cerrar]│
└────────────────────────────────────────────────────────────────────────┘
```

### Botones

| Botón | Qué hace |
|---|---|
| **+ Nueva** | Abre el diálogo para registrar una compra. |
| **Editar** | Edita la compra seleccionada (también doble clic). |
| **Eliminar** | Borra la compra seleccionada (confirmación `Eliminar compra <num>?`). |
| **? Ayuda** | Abre esta ficha del manual. |
| **Cerrar** | Sale de la pantalla. |

### Tabla

Cabecera **clickeable** para ordenar por `CompraN`, `Fecha`, `Proveedor`, `Total`.

| Columna | Significado |
|---|---|
| **CompraN** | Número correlativo (`C00001`) |
| **Fecha** | Fecha de la compra |
| **Proveedor** | Código del proveedor |
| **FormaDePago** | Texto libre (Contado, Crédito, Cheque…) |
| **Descuento** | Porcentaje |
| **IVA** | Monto en USD |
| **Total** | Total en USD |
| **DiasDeCred** | Días de crédito |

### Resumen al pie

```
Compras: N | Total comprado: $X | Abonado: $Y
```

### Diálogo Nueva / Editar compra

**Panel izquierdo (cabecera)**:

| Campo | Detalle |
|---|---|
| **No. Compra** | Generado automáticamente (`C00001`, `C00002`…), no editable. |
| **Proveedor** | Desplegable de proveedores. **Obligatorio.** |
| **Fecha** | Por defecto hoy. |
| **Forma de Pago** | Texto libre. |
| **Descuento (%)** | 0–100. |
| **Abono** | Monto pagado. |
| **IVA** | Desplegable: `0%` o porcentaje configurado (default 16%). |
| **Días Crédito** | Entero. |
| **Total** | Total en USD. |
| **Transporte** | Texto libre. |
| **Observaciones** | Texto libre multilínea. |

**Panel derecho (productos de la compra)**:

| Columna | Significado |
|---|---|
| **Producto (Insumo)** | Desplegable con productos del proveedor activos |
| **Cantidad** | Cuánto se compró |
| **Precio C/U** | Precio unitario |

Botones: **+ Agregar** suma una fila, **Quitar** elimina la seleccionada. **Obligatorio** al menos un producto.

## Casos de uso

### Caso 1 — Consultar compras antiguas

1. Abrir la pantalla — la tabla muestra todas las compras ordenadas por `CompraN` descendente (más reciente primero).
2. Hacer clic en cabecera de **Fecha**, **Proveedor** o **Total** para reordenar.
3. **Doble clic** en una fila abre la compra en el diálogo de edición.

### Caso 2 — Editar una compra antigua

1. Seleccionar la fila → **Editar** (o doble clic).
2. Modificar lo necesario.
3. **Guardar**.

### Caso 3 — Eliminar una compra

1. Seleccionar la fila → **Eliminar**.
2. Confirmar `Eliminar compra <numero>?`.

## Validaciones y errores frecuentes

| Mensaje en pantalla | Qué significa | Cómo se resuelve |
|---|---|---|
| `Selecciona un proveedor.` | No se eligió proveedor | Seleccionar en el desplegable |
| `Agrega al menos un producto en el detalle.` | Faltan ítems en el detalle | Hacer clic en **+ Agregar** y completar |
| `Eliminar compra <num>?` | Confirmación | Sí / No |

## Consejos

- **Migrar registros nuevos a `FrmEgresos`** — más completo, doble moneda, integración con cuentas por pagar y libro IVA automático.
- **El número de compra `CXXXXX` no se reutiliza** aunque se borre.
- Si la compra es de **materia prima** que tiene que entrar a inventario, registrala en `FrmEgresos` y luego un movimiento de entrada en *Kardex*. Esta pantalla **no actualiza inventario** automáticamente.

## Pantallas relacionadas

- **`FrmEgresos`** — reemplazo recomendado para registrar compras y gastos.
- **`FrmProveedores`** — proveedores que aparecen en el desplegable.
- **`FrmProductosProveedor`** — productos que aparecen en el detalle.
- **`FrmCuentasPagar`** — seguimiento de las compras a crédito.
- **`FrmLibroIVA`** — libro de compras (no incluye los registros de esta pantalla legacy si no tienen base/IVA correctamente cargados).
