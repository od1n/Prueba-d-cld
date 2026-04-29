# `FrmEgresos` — Egresos del negocio

> Hub de **compras, gastos y materia prima**. Permite registrar todo lo que sale del negocio (efectivo o crédito), ver KPIs por categoría, marcar pagos y administrar las categorías de gasto.

## Cómo llegar

- **Ruta**: *Menú principal → Transacciones → Egresos*
- **Permiso requerido**: `EgresosVer` (o `Admin`). El tab de **Categorías** además requiere `EgresosCategorias` o ser Admin.

> Si tu rol no tiene permiso, al abrir aparece el aviso: `Tu rol (...) no tiene permiso para ver el módulo de Egresos.` y la ventana se cierra sola.

## Para qué se usa

Centraliza el registro de **todo gasto del negocio**: compras de materia prima, servicios, mantenimiento, alquiler, papelería, sueldos pagados por fuera, etc. Cada egreso se carga en su moneda original (USD o Bs) y el sistema lo convierte automáticamente con la tasa BCV o paralelo del día.

Lo usan típicamente el contador y los responsables de compras.

## Antes de empezar

- Tener las **categorías de gasto** definidas (ver tab *Categorías*).
- Tener cargada la **tasa BCV/paralelo del día** si vas a registrar pagos en bolívares.
- Si es una **compra de materia prima**, tener el insumo dado de alta en *Archivos → Insumos Proveed.*

## Qué ves en la pantalla

- **Cabecera azul oscuro** con el título y subtítulo *"Compras de materia prima · servicios · mantenimiento · oficina · cuentas por pagar"*.
- **Barra de filtros**: rango **Desde / Hasta** (por defecto, mes actual), botón **↻ Aplicar filtro** y botón **+ Nuevo egreso** (verde).
- **Pestañas (tabs)**:
  - **📊 Resumen** — KPIs del período + gráfico horizontal por categoría.
  - **📦 Materia Prima** — egresos de categorías marcadas como MP.
  - **💼 Gastos Operativos** — egresos del resto de categorías.
  - **⏰ Por Pagar (CxP)** — egresos en estado *Pendiente* con vencimiento; doble clic marca pagado.
  - **🏷️ Categorías** — *(solo Admin / EgresosCategorias)* CRUD de categorías de gasto.
- **Pie** con resumen del tab activo y botón **Cerrar**.

### Tab Resumen — KPIs

| KPI | Significado |
|---|---|
| **Egresos del Período** | Total en USD del rango seleccionado |
| **# de Egresos** | Cantidad de registros |
| **Categorías Usadas** | Cuántas categorías tuvieron movimientos |
| **CxP Pendientes** | Cuentas por pagar registradas como egreso pendiente |

Debajo, un listado con **barra horizontal por categoría**: cuanto más largo, mayor gasto.

### Tab Materia Prima / Gastos Operativos — columnas

| Columna | Significado |
|---|---|
| **Fecha** | Fecha del egreso |
| **Categoria** | Categoría asignada |
| **Concepto** | Descripción del gasto |
| **Proveedor** | A quién se le pagó |
| **Pago** | Tipo de pago: `USD`, `Bs(BCV)`, `Bs(Paralelo)`, `Bs(custom)` |
| **Original** | Monto en su moneda original |
| **USD** | Equivalente en dólares |
| **Estado** | Pagado · Pendiente |

Doble clic en una fila → editar.

### Tab Por Pagar (CxP)

Lista los egresos pendientes con **fecha de vencimiento**. Las filas se colorean:

- **Rojo claro** → vencido (días < 0)
- **Amarillo claro** → vence en los próximos 7 días

Doble clic muestra el aviso `¿Marcar este egreso como PAGADO hoy (dd/MM/yyyy)?`. Confirmar marca el pago.

## Casos de uso

### Caso 1 — Registrar un gasto operativo pagado en USD

**Pasos**:

1. Hacer clic en **+ Nuevo egreso**.
2. En el editor (`FrmGastoEditor`) elegir **Categoría** (ej. *Mantenimiento*), llenar **Concepto**, **Proveedor**.
3. **Tipo de pago**: `USD_DIRECTO → Pagué en USD efectivo`.
4. Ingresar el **Monto** en USD.
5. Dejar **PENDIENTE de pago** desmarcado (queda como pagado hoy).
6. Adjuntar comprobante (opcional).
7. **Guardar egreso**.

### Caso 2 — Registrar una compra de materia prima en Bs (BCV)

**Pasos**:

1. **+ Nuevo egreso**.
2. Elegir una categoría marcada como **Materia Prima** (ej. *Insumos plásticos*).
3. Aparece el **Panel azul de Materia Prima**: seleccionar el **Insumo del catálogo**, ingresar **Cantidad** y **Precio unitario (USD)** — el monto se calcula automáticamente.
4. **Tipo de pago**: `BCV → Pagué en Bs a tasa oficial BCV` (la tasa se carga sola).
5. **Moneda original**: `BS — Bolívares`. Ingresar el monto en Bs.
6. **Guardar egreso**.

**Resultado**: el sistema actualiza **stock + kardex** del insumo automáticamente y registra el gasto en USD equivalente.

### Caso 3 — Registrar un gasto a crédito (CxP)

**Cuándo**: el proveedor da plazo de pago.

**Pasos**:

1. **+ Nuevo egreso**.
2. Llenar todos los datos como un gasto normal.
3. Marcar **PENDIENTE de pago (CxP)**.
4. Elegir la **Vence el** (fecha límite).
5. **Guardar egreso**.

**Resultado**: el egreso aparece en el tab **Por Pagar (CxP)**. Cuando se le pague, ir a ese tab y hacer doble clic para marcarlo como pagado.

### Caso 4 — Ver el resumen del mes

**Pasos**:

1. Ajustar **Desde / Hasta** al período deseado.
2. **↻ Aplicar filtro**.
3. Ir al tab **Resumen**.

**Resultado**: se ven los 4 KPIs y el gráfico horizontal por categoría.

### Caso 5 — Editar un egreso existente

**Pasos**:

1. Ir al tab **Materia Prima** o **Gastos Operativos**.
2. Buscar la fila.
3. Hacer **doble clic** sobre ella → se abre el editor.
4. Modificar y **Guardar egreso**.

## Validaciones y errores frecuentes

| Mensaje | Qué significa | Cómo se resuelve |
|---|---|---|
| `Tu rol (...) no tiene permiso para ver el módulo de Egresos.` | Falta el permiso `EgresosVer` | Pedir al administrador que lo asigne |
| `Concepto requerido` | Faltó descripción | Llenar el campo |
| `Selecciona una categoría` | No se eligió categoría | Elegir del desplegable |
| `El monto debe ser mayor que cero.` | Monto en 0 o negativo | Corregir el monto |
| `Selecciona el insumo del catálogo para esta compra de MP.` | Es categoría MP pero no se eligió insumo | Seleccionar del catálogo o cambiar la categoría |
| `La cantidad debe ser mayor que cero.` | Cantidad de MP en 0 | Ingresar cantidad > 0 |

## Consejos

- **Aplicar filtro** después de cambiar fechas — no recarga solo.
- Para ver lo que vence pronto, mirar el tab **Por Pagar (CxP)** y filtrar mentalmente las filas amarillas/rojas.
- **Adjuntar comprobante** en cada egreso (foto o PDF) — queda guardado en `comprobantes` para auditoría.
- Si la categoría es **Materia Prima**, el sistema actualiza el stock del insumo y deja huella en el kardex automáticamente — **no hay que hacer la entrada manual**.

## Pantallas relacionadas

- **`FrmGastoEditor`** — diálogo de carga/edición de un egreso.
- **`FrmCategoriaEditor`** — gestión de categorías de gasto.
- **`FrmCuentasPagar`** — vista alterna de los pendientes con proveedores.
- **`FrmProductosProveedor`** — alta y consulta de insumos para compras de MP.
- **`FrmKardex`** — historial de movimientos de inventario (las compras MP aparecen aquí).
