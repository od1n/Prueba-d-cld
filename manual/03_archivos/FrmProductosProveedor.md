# `FrmProductosProveedor` — Insumos / Productos por Proveedor

> Catálogo de **insumos y materias primas** que se compran a proveedores. Mantiene precio última compra, precio promedio, stock y proveedor principal de cada uno.

## Cómo llegar

- **Ruta**: *Menú principal → Archivos → Insumos Proveed.*
- **Permiso requerido**: `Contabilidad` (o `Admin`)

## Para qué se usa

Llevar el control de los insumos comprados: códigos internos, descripción, tipo de insumo, unidad, proveedor principal, precios y stock. Esta lista alimenta:

- El editor de **Egresos** cuando se carga una compra de Materia Prima.
- El **Kardex A** (insumos) para movimientos de entrada/salida.

## Qué ves en la pantalla

- **Cabecera azul**: título *"Productos de Proveedores — Insumos y Materiales"* con botones **+ Nuevo**, **Editar**, **Eliminar**.
- **Barra de filtros** (color celeste claro):
  - **Buscar** (texto libre).
  - **Tipo**: `Todos los tipos` o uno específico (Materia prima, Empaque, Servicio, etc., según el catálogo).
  - **Solo activos** (checkbox marcado por defecto).
- **Tabla** con los insumos. Las filas:
  - **Amarillo claro** → bajo mínimo
  - **Texto gris** → inactivo
- **Pie**: resumen *"Insumos: N | Bajo mínimo: X | Inactivos: Y"* y botón **Cerrar**.

### Columnas

| Columna | Significado |
|---|---|
| **CodigoInterno** | Código del insumo en el sistema (autogenerado al crear) |
| **Descripcion** | Nombre del insumo |
| **TipoInsumo** | Categoría (Materia prima, Empaque, etc.) |
| **Unidad** | kg, L, und, m… |
| **Proveedor** | Nombre del proveedor principal |
| **PrecioUltCompra** | Último precio pagado (USD) |
| **PrecioPromedio** | Promedio ponderado (USD) |
| **StockActual** | Existencia |
| **StockMinimo** | Nivel de alerta |
| **Estado** | Activo · Inactivo |

> Doble clic en una fila abre el editor.

## Casos de uso

### Caso 1 — Alta de un insumo

1. **+ Nuevo**.
2. **Código interno**: se autogenera al crear (no editable).
3. Llenar:
   - **Cód. proveedor** (referencia opcional del proveedor).
   - **Descripción** *(obligatoria)*.
   - **Tipo insumo** *(obligatorio)*: elegir del desplegable.
   - **Unidad** *(obligatoria, ej. kg, L, und, m)*.
   - **Proveedor principal**: elegir del desplegable o `-- Sin proveedor --`.
   - **Precio últ. compra** y **Precio promedio**.
   - **Stock actual** y **Stock mínimo**.
   - **Activo** (marcado por defecto).
   - **Observaciones** (texto libre).
4. **Guardar**.

### Caso 2 — Editar un insumo

1. Doble clic (o **Editar**).
2. Modificar (el **Código interno** queda bloqueado).
3. **Guardar**.

### Caso 3 — Dar de baja sin eliminar

1. Editar el insumo.
2. Desmarcar **Activo**.
3. **Guardar**.

> Pasa a estado *Inactivo* (texto gris). Sigue apareciendo en historiales pero no en el catálogo de compras nuevas.

### Caso 4 — Eliminar un insumo

1. Seleccionar.
2. **Eliminar**.
3. Confirmar `Eliminar insumo 'XXX' (CODIGO)?`.

### Caso 5 — Filtrar por tipo

1. En la barra, abrir **Tipo** y elegir el deseado (ej. `Materia prima`).
2. La tabla filtra al instante.

## Validaciones y errores frecuentes

| Mensaje | Qué significa | Cómo se resuelve |
|---|---|---|
| `La descripción es obligatoria.` | Faltó la descripción | Llenarla |
| `La descripción contiene caracteres no permitidos.` | Símbolos no aceptados | Usar sólo letras, números, espacios y `- . , / ( ) " +` |
| `La unidad de medida es obligatoria (ej: kg, L, und).` | Falta unidad | Llenarla |
| `El stock mínimo es mayor al stock actual. ¿Continuar?` | Aviso, no bloqueo | Confirmar **Sí** o ajustar |

## Consejos

- Mantener actualizado el **Stock mínimo** — el dashboard usa ese valor para alertas.
- Cuando se compra un insumo desde **Egresos** con categoría Materia Prima, el **stock** y el **precio última compra** se actualizan automáticamente — no hace falta editar manualmente.
- Si un proveedor cambia, basta editar el insumo y elegir el nuevo proveedor principal — los movimientos viejos quedan intactos.

## Pantallas relacionadas

- **`FrmProveedores`** — proveedores asociados.
- **`FrmEgresos`** — compras de materia prima que actualizan stock.
- **`FrmKardex`** — pestaña *Kardex A* (insumos) muestra los movimientos.
