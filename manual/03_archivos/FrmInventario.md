# `FrmInventario` — Catálogo de Productos

> Lista, da de alta, edita y archiva los **productos terminados** del catálogo. Incluye un modo de edición masiva celda a celda para administradores.

## Cómo llegar

- **Ruta**: *Menú principal → Archivos → Inventario*
- **Permiso requerido**: `Almacén` (o `Admin`). Los botones **+ Nuevo**, **Archivar** y **Modo Edición** sólo aparecen para `Admin`.

## Para qué se usa

Mantener al día el catálogo de productos: códigos, descripciones, costos, precios, existencias, mínimos/máximos, peso unitario y proveedor asociado. Es la base que usa toda la operación: facturación, kardex, producción, dashboard.

## Antes de empezar

- Conocer los códigos internos de los productos (no los inventa el sistema).
- Para alta de productos, tener listo: código, descripción, unidad de medida, precio de venta.
- Idealmente, tener también el costo y el código del proveedor asociado.

## Qué ves en la pantalla

- **Cabecera azul oscuro**: título *"Inventario de Productos"*, botones **? Ayuda**, **+ Nuevo** (verde), **Editar**, **Archivar** (naranja), **Modo Edición** (marrón).
- **Barra de búsqueda** (color celeste claro): campo **Buscar** y leyenda *"* Rojo = sin stock | Amarillo = bajo mínimo"*.
- **Tabla central** con las columnas del inventario. Las filas se colorean:
  - **Rojo claro** → existencia 0
  - **Amarillo claro** → existencia por debajo del mínimo
- **Pie**: resumen *"Productos: N | Bajo mínimo: X | Sin existencia: Y"* y botón **Cerrar**.

### Columnas

| Columna | Significado |
|---|---|
| **Codigo** | Código del producto |
| **Descripcion** | Nombre del producto |
| **Departamento** | Categoría/área |
| **Unidad** | Unidad de medida (Und, Kg, L, m, …) |
| **CostoActual** | Costo vigente (USD) |
| **Precio** | Precio de venta (USD) |
| **Existencia** | Stock disponible |
| **Min** / **Max** | Niveles de alerta |
| **PesoKg** | Peso por unidad (4 decimales) |
| **Proveedor** | Código del proveedor |
| **Actualizado** | Última fecha de modificación |

> **Tip**: clic en un encabezado para ordenar; segundo clic invierte el sentido. La búsqueda tolera ceros a la izquierda (escribir `03` encuentra el código `3`).

## Casos de uso

### Caso 1 — Dar de alta un producto nuevo *(Admin)*

1. **+ Nuevo**.
2. En el diálogo:
   - **Código** *(obligatorio)*.
   - **Descripción** *(obligatoria)*.
   - **Departamento**, **Unidad** *(obligatoria, ej. Und/Kg/L)*, **Enser**.
   - **Costo Actual**, **Costo Anterior**, **Costo Promedio**.
   - **Precio Venta** *(obligatorio, > 0)*.
   - **Existencia**, **Min**, **Exist. Max**.
   - **Peso (kg/und)**.
   - **Cod. Proveedor**.
3. **Guardar**.

### Caso 2 — Editar un producto

1. Seleccionar la fila (o hacer doble clic).
2. **Editar**.
3. Modificar y **Guardar**. El **Código** queda bloqueado para no romper trazabilidad.

### Caso 3 — Archivar un producto *(Admin)*

1. Seleccionar la fila.
2. **Archivar**.
3. Confirmar `Eliminar producto XXX?`.

> El producto deja de aparecer pero los movimientos viejos del kardex y facturas ya emitidas se conservan.

### Caso 4 — Editar varias celdas rápido (modo masivo) *(solo Admin)*

1. Hacer clic en **Modo Edición**. El botón cambia a rojo y aparece la leyenda *"⚠ MODO EDICION — haz clic en una celda para modificar"*.
2. Hacer clic en una celda editable (Descripción, Departamento, Unidad, Costo Actual, Precio, Existencia, Min, Max, Peso, Proveedor) y escribir el nuevo valor.
3. Salir de la celda → se guarda.
4. Cuando termines, hacer clic en **Salir Edicion**.

> **Cuidado**: en este modo cada cambio se guarda al salir de la celda. No hay confirmación.

### Caso 5 — Buscar un producto

1. Escribir en el campo **Buscar** parte del código, descripción, departamento o proveedor.
2. La tabla filtra al instante.

## Validaciones y errores frecuentes

| Mensaje | Qué significa | Cómo se resuelve |
|---|---|---|
| `El código es obligatorio.` | Faltó el código al crear | Llenarlo |
| `El código 'XXX' ya existe en el inventario.` | Código duplicado al crear | Usar otro código |
| `La descripción es obligatoria.` | Faltó la descripción | Llenarla |
| `La descripción contiene caracteres no permitidos. Use letras, números, espacios y: - . , / ( )` | Caracteres no aceptados | Quitar símbolos especiales |
| `La unidad de medida es obligatoria (ej: Und, Kg, L).` | Falta unidad | Llenarla |
| `El precio de venta debe ser mayor a 0.` | Precio en 0 | Ingresar precio > 0 |
| `El costo actual no puede ser negativo.` | Costo < 0 | Corregirlo |
| `La existencia mínima no puede ser mayor que la máxima.` | Min > Max | Ajustar valores |
| `La existencia no puede ser negativa.` | Existencia < 0 | Corregirlo |
| `Solo el administrador puede usar el modo de edicion.` | Usuario no Admin intentó modo edición | Pedir el cambio a un admin |
| `El campo 'XXX' debe ser un número mayor o igual a 0.` | Edición masiva con valor inválido | El sistema revierte el cambio; corregir |
| `No se pudo guardar 'XXX'. ...` | Error de BD al guardar | El sistema revierte; reintentar |

## Consejos

- Mantener los **mínimos** bien ajustados: las alertas del Dashboard usan estos valores.
- Si un producto se reemplaza, mejor **archivarlo** y crear el nuevo, no reutilizar el código.
- En modo edición masiva, no se puede cambiar el **Código** ni la fecha **Actualizado** (son sólo lectura).
- La búsqueda flexible permite encontrar `J-12345` aunque esté guardado como `J12345`.

## Pantallas relacionadas

- **`FrmKardex`** — movimientos de inventario (entradas, salidas, ajustes).
- **`FrmCambioPrecios`** — ajuste planificado de precios.
- **`FrmFacturas`** — usa este catálogo al emitir facturas.
- **`FrmProveedores`** — proveedores que se referencian en *Cod. Proveedor*.
