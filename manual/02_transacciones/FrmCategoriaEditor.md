# `FrmCategoriaEditor` — Editor de Categoría de Gasto

> Diálogo simple para **crear o editar una categoría de gasto**. Las categorías agrupan los egresos para reportes y para distinguir Materia Prima de gastos operativos.

## Cómo llegar

- Desde **`FrmEgresos` → tab Categorías → + Nueva categoría** (crear).
- Desde **`FrmEgresos` → tab Categorías → doble clic en una fila** (editar).
- **Permiso requerido**: `Admin` o `EgresosCategorias`. El tab Categorías sólo aparece para usuarios con uno de esos permisos.

## Para qué se usa

Definir las categorías que después se usan al cargar un egreso. Cada categoría tiene:

- **Nombre** visible al elegir en el desplegable.
- **Color** (hex, ej. `#1A4A8A`) — usado en el gráfico del Resumen.
- **Orden de aparición** en la lista.
- **Activa** — si está desmarcada, no aparece al crear nuevos egresos.
- **Es categoría de Materia Prima** — clave: las categorías marcadas así muestran el panel especial de insumo + cantidad al cargar un egreso, y suman al tab *Materia Prima* en el listado.

## Qué ves en la pantalla

Diálogo modal pequeño (440 × 320) con título *"Nueva categoría de gasto"* o *"Editar categoría"*. Campos:

| Campo | Descripción |
|---|---|
| **Nombre** | Texto que aparece en los desplegables |
| **Color (hex, ej: #1A4A8A)** | Código de color HTML (6 dígitos) |
| **Orden de aparición** | Número 0–999, define el orden en la lista |
| **Activa (visible al crear gastos)** | Checkbox |
| **Es categoría de Materia Prima (insumos del producto)** | Checkbox |

Botones **Guardar** (azul) y **Cancelar** (gris).

## Casos de uso

### Caso 1 — Crear una categoría operativa

1. Tab Categorías → **+ Nueva categoría**.
2. **Nombre**: ej. `Mantenimiento`.
3. **Color**: ej. `#D29922`.
4. **Orden**: ej. `10`.
5. Dejar **Activa** marcado, **Materia Prima** desmarcado.
6. **Guardar**.

### Caso 2 — Crear una categoría de Materia Prima

1. **+ Nueva categoría**.
2. **Nombre**: ej. `Insumos plásticos`.
3. Marcar **Es categoría de Materia Prima**.
4. **Guardar**.

> A partir de ahora, al cargar un egreso con esta categoría, el editor muestra el panel azul claro para elegir insumo y cantidad, y la compra actualiza el stock.

### Caso 3 — Desactivar una categoría sin eliminarla

1. Doble clic en la categoría.
2. Desmarcar **Activa**.
3. **Guardar**.

**Resultado**: la categoría sigue existiendo (los egresos viejos siguen mostrándola) pero **no aparece** al crear egresos nuevos.

## Validaciones y errores frecuentes

| Mensaje | Qué significa | Cómo se resuelve |
|---|---|---|
| `El nombre es requerido.` | Faltó el nombre | Llenar el campo |
| `Error: ...` (al guardar) | Algún problema interno | Reintentar; si persiste, llamar al administrador |

## Consejos

- Si el campo **Color** queda vacío, se asigna `#1A4A8A` (azul) por defecto.
- Cambiar una categoría de operativa a **Materia Prima** afecta sólo a egresos *futuros* — los ya cargados no migran de tab automáticamente.
- Mantener nombres cortos (≤ 25 caracteres) para que se lean bien en el gráfico del Resumen.

## Pantallas relacionadas

- **`FrmEgresos`** — donde se usan las categorías al cargar gastos y se ve el resumen por categoría.
- **`FrmGastoEditor`** — selector de categoría al cargar un egreso.
