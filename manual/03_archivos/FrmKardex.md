# `FrmKardex` — Kardex (Movimientos de Inventario)

> Historial de **movimientos de inventario** (entradas, salidas, ajustes) tanto de **Productos Terminados** (Kardex B) como de **Insumos / Materias Primas** (Kardex A). Calcula costo promedio ponderado y permite imprimir planilla.

## Cómo llegar

- **Ruta**: *Menú principal → Archivos → Kardex*
- **Permiso requerido**: `Almacén` (o `Admin`)

## Para qué se usa

Llevar la trazabilidad detallada de cada movimiento de inventario. Cada movimiento queda registrado con fecha, tipo, cantidad, costo, motivo y usuario. Sirve para:

- Ver entradas, salidas y ajustes de un producto/insumo en un período.
- Identificar quién hizo cada movimiento (auditoría).
- Imprimir o exportar la planilla del producto.
- Hacer ajustes manuales cuando hay diferencias entre stock real y sistema.

> Cuando se carga una compra de **Materia Prima** desde *Egresos* o se anula una factura, el sistema genera el movimiento automáticamente. Esta pantalla también permite hacer movimientos **manuales**.

## Qué ves en la pantalla

- **Cabecera azul oscuro** con el título *"Kardex — Control de Inventario"*, botones **? Ayuda** y **⊟ Planilla** (imprimir).
- **Selector de Kardex** (franja azul oscuro debajo): dos botones grandes:
  - **📦 Kardex B — Productos Terminados** (activo por defecto)
  - **🧪 Kardex A — Insumos / Mat. Primas**
- **Barra de acciones** del kardex activo: **▲ Entrada**, **▼ Salida**, **⚖ Ajuste**, **↓ CSV**.
- **Barra de filtros**: desplegable **Producto / Insumo**, **Desde**, **Hasta**, botones **Filtrar** y **Limpiar**.
- **Tabla** con los movimientos.
- **Pie** con resumen del filtro aplicado.

### Columnas típicas (Kardex B y A)

- **Fecha**, **Tipo** (Entrada / Salida / Ajuste), **Producto/Insumo**, **Cantidad**, **CostoUnit**, **CostoTotal**, **SaldoCant**, **SaldoMonto**, **Motivo**, **Usuario**.

## Casos de uso

### Caso 1 — Ver el historial de un producto

1. Asegurarse de estar en **Kardex B — Productos Terminados** (botón blanco activo).
2. En el desplegable **Producto** elegir el producto deseado (o `— Todos —`).
3. Ajustar **Desde** y **Hasta**.
4. **Filtrar**.

### Caso 2 — Registrar una entrada manual

**Cuándo**: ingreso de mercadería que no se registró por una compra (ej. devolución de cliente).

1. **▲ Entrada**.
2. En el diálogo elegir el artículo, ingresar cantidad, costo unitario, motivo.
3. **Guardar**.

> El stock se incrementa y el costo promedio se recalcula con la fórmula ponderada.

### Caso 3 — Registrar una salida manual

**Cuándo**: salida que no proviene de una factura (ej. uso interno, donación).

1. **▼ Salida**.
2. Ingresar artículo, cantidad, motivo.
3. **Guardar**.

### Caso 4 — Hacer un ajuste de inventario

**Cuándo**: el conteo físico no coincide con el sistema.

1. **⚖ Ajuste**.
2. Ingresar artículo, cantidad final correcta, motivo (mínimo requerido).
3. **Guardar**.

**Resultado**: el sistema deja registrado el ajuste con el motivo y el usuario. Aparece `Ajuste registrado.`

### Caso 5 — Exportar a CSV

Hacer clic en **↓ CSV**. Se guarda el archivo con el filtro vigente y aparece `Exportado: N registros.`

### Caso 6 — Imprimir planilla de un producto

1. Seleccionar el producto/insumo en el filtro.
2. Hacer clic en **⊟ Planilla** (cabecera).
3. El sistema arma el documento imprimible.

### Caso 7 — Ver el kardex de insumos

Hacer clic en **🧪 Kardex A — Insumos / Mat. Primas**. La barra de acciones cambia a marrón y se ven los movimientos de materias primas.

## Validaciones y errores frecuentes

| Mensaje | Qué significa | Cómo se resuelve |
|---|---|---|
| `Seleccione un artículo.` | Falta elegir producto/insumo en el diálogo | Elegir uno |
| `Ingrese el concepto.` | Falta motivo/concepto del movimiento | Llenarlo |
| `Ingrese el motivo.` | Falta motivo (en ajustes) | Llenarlo |

## Consejos

- Cuando se anula una factura, **el stock NO se devuelve automáticamente** — hacer aquí una **Entrada manual** con el motivo *"Anulación factura N°…"*.
- Los **ajustes** deben ser excepcionales y siempre con motivo claro — quedan en auditoría.
- Si necesitás ver todos los movimientos de un período sin filtrar producto, dejá el desplegable en `— Todos —`.
- **Limpiar** restablece el filtro a último mes y todos los productos.

## Pantallas relacionadas

- **`FrmInventario`** — catálogo de productos terminados.
- **`FrmProductosProveedor`** — catálogo de insumos.
- **`FrmEgresos`** — las compras MP generan entradas automáticas en Kardex A.
- **`FrmFacturas`** — las ventas generan salidas automáticas en Kardex B.
- **`FrmRegistroProduccionDia`** / **`FrmIngresoProduccion`** — la producción genera entradas de productos terminados y salidas de insumos.
