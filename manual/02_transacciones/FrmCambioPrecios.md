# `FrmCambioPrecios` — Cambio de Precios y Costos

> Edita masivamente o uno por uno los **precios de venta** y **costos** de los productos. Permite preparar nuevos precios y aplicarlos en bloque cuando se decide.

## Cómo llegar

- **Ruta**: *Menú principal → Transacciones → Cambio de Precios*
- **Permiso requerido**: `Ventas` (o `Admin`)

## Para qué se usa

Sirve para **mantener actualizados los precios** del catálogo, especialmente útil cuando:

- Sube el costo de un insumo y hay que ajustar el precio de venta.
- Hay devaluación y se necesita un aumento global por porcentaje.
- Se cambian precios uno por uno para algunos productos puntuales.

La lógica es **proponer-aplicar**: primero se cargan los **precios nuevos** sin tocar los actuales (las filas se marcan en amarillo claro). Cuando todos los nuevos precios están listos, se hace clic en **Aplicar** y los nuevos pasan a ser los vigentes.

> Esta lista es independiente del catálogo de inventario. Sirve para escenarios de **planificación de precios**.

## Qué ves en la pantalla

- **Cabecera azul oscuro** con el título y los botones **? Ayuda**, **+ Producto**, **Editar**, **Aplicar**, **Eliminar**.
- **Barra de filtros** (color crema): desplegable **Filtrar categoría** (`Todas`, `Alimentos`, `Electronicos`, `Ropa`, `Servicios`, `Materia Prima`, `Otros`), campo **% Ajuste global** (numérico, −99 a +999) y botón **Aplicar %**.
- **Tabla** con cabecera ámbar. Las filas que tienen **precio nuevo cargado** se pintan amarillo claro.
- **Pie**: resumen *"Productos: N | Con precio nuevo: X | Pendientes aplicar: Y"* y botón **Cerrar**.

### Columnas

| Columna | Significado |
|---|---|
| **Codigo** | Código del producto |
| **Nombre** | Descripción |
| **Categoria** | Categoría asignada |
| **CostoActual** | Costo vigente |
| **PrecioActual** | Precio de venta vigente |
| **CostoNuevo** | Costo propuesto (`-` si no hay) |
| **PrecioNuevo** | Precio propuesto (`-` si no hay) |
| **Margen** | % calculado como `(PrecioActual − CostoActual) / PrecioActual × 100` |
| **Actualizado** | Última fecha de actualización |

## Casos de uso

### Caso 1 — Subir un 10% el precio de todos los productos

1. (Opcional) Filtrar por categoría si querés afectar sólo una.
2. En **% Ajuste global** ingresar `10`.
3. Hacer clic en **Aplicar %**.
4. Aparece el aviso `Aplicar 10.0% a precios de N productos?` — confirmar **Sí**.

**Resultado**: el sistema calcula `PrecioNuevo = PrecioActual × 1.10` para cada producto y los carga en la columna *PrecioNuevo*. **No reemplaza todavía** los precios actuales — quedan como propuesta.

5. Revisar las filas amarillas. Si todo está OK, hacer clic en **Aplicar** (cabecera).
6. Confirmar el aviso `Aplicar nuevos precios a N productos?`.

**Resultado**: los precios nuevos pasan a ser los vigentes y el campo *Actualizado* se pone en hoy. Aparece `Precios actualizados en N productos.`

### Caso 2 — Cambiar el precio de un solo producto

1. Buscar la fila.
2. Hacer clic en **Editar**.
3. En el diálogo, modificar **Precio Nuevo** (y opcionalmente **Costo Nuevo**).
4. **Guardar**.
5. Volver a la lista, seleccionar la fila y hacer clic en **Aplicar** (o esperar a aplicar varios juntos).

### Caso 3 — Agregar un producto a la lista

1. **+ Producto**.
2. Llenar **Código**, **Nombre**, **Categoría** (`Alimentos`, `Electronicos`, `Ropa`, `Servicios`, `Materia Prima`, `Otros`).
3. Llenar **Costo Actual**, **Precio Actual**, **Costo Nuevo**, **Precio Nuevo**.
4. **Guardar**.

### Caso 4 — Eliminar un producto de la lista

1. Seleccionar la fila.
2. **Eliminar**.
3. Confirmar `Eliminar producto #N?`.

## Validaciones y errores frecuentes

| Mensaje | Qué significa | Cómo se resuelve |
|---|---|---|
| `Ingrese el nombre.` | Falta el nombre al crear/editar | Llenar el campo |
| `No hay precios nuevos pendientes de aplicar.` | Se hizo clic en **Aplicar** sin tener precios nuevos cargados | Cargar primero los nuevos precios o usar **Aplicar %** |

## Consejos

- El indicador de **Margen** cambia de color en el editor: verde ≥ 20%, naranja 0–19%, rojo si es negativo.
- **Aplicar %** afecta a *todos* los productos del filtro vigente, no sólo al seleccionado.
- Una vez aplicados, los **CostoNuevo / PrecioNuevo** se vacían automáticamente. Hay que volver a cargarlos si se quiere otra ronda.
- Esta pantalla no descuenta stock ni emite facturas — sólo cambia precios.

## Pantallas relacionadas

- **`FrmInventario`** — alta y consulta del catálogo principal de productos.
- **`FrmFacturas`** — usa los precios resultantes al facturar.
