# `FrmCuentasPagar` — Cuentas por Pagar

> Lista las obligaciones pendientes con proveedores, registra pagos y consulta el historial. Es la pantalla diaria del responsable de pagos.

## Cómo llegar

- **Ruta**: *Menú principal → Transacciones → Cuentas por Pagar*
- **Permiso requerido**: `Contabilidad` (o `Admin`)

## Para qué se usa

Permite controlar **cuánto le debemos a cada proveedor** y administrar los pagos. Sirve para:

- Cargar facturas de proveedor pendientes de pago.
- Registrar pagos parciales o totales (con tasa, fecha, referencia).
- Consultar el historial de pagos de cada cuenta.
- Filtrar por estado (Pendiente, Parcial, Vencida, Pagada).
- Exportar el listado a PDF.

> Las cuentas se marcan automáticamente como **Vencida** cuando pasa la fecha de vencimiento sin estar pagadas.

## Qué ves en la pantalla

- **Cabecera morada** con el título *"Cuentas por Pagar"*.
- **Toolbar**: filtro de estado (`Todas`, `Pendiente`, `Parcial`, `Vencida`, `Pagada`) y botones **+ Nueva**, **Editar**, **Registrar Pago**, **Ver Pagos**, **Eliminar**, **PDF**.
- **Panel de KPIs** (4 tarjetas): Saldo Total Pendiente · Saldo Vencido · Cuentas Vencidas · Cuentas Pendientes.
- **Tabla central** con las cuentas. Las filas cambian de color por estado:
  - **Rojo claro** → Vencida
  - **Verde claro** → Pagada
  - **Amarillo claro** → Parcial
  - **Blanco** → Pendiente
- **Pie** con botón **Cerrar**.

### Columnas

| Columna | Significado |
|---|---|
| **Proveedor** | Nombre del proveedor |
| **Concepto** | Descripción de la cuenta |
| **N° Compra** | Número de factura del proveedor |
| **Emision** | Fecha de emisión de la factura |
| **Vence** | Fecha de vencimiento |
| **Monto (USD)** | Total de la deuda |
| **Pagado (USD)** | Acumulado abonado |
| **Saldo (USD)** | `Monto − Pagado` |
| **Estado** | Pendiente · Parcial · Vencida · Pagada |

## Casos de uso

### Caso 1 — Cargar una cuenta por pagar nueva

**Cuándo**: llegó una factura del proveedor a crédito.

**Pasos**:

1. Hacer clic en **+ Nueva**.
2. Llenar **Proveedor**, **Concepto**, **N° Compra**, **Monto** (USD), **Fecha emisión** y **Fecha vencimiento**.
3. **Guardar**.

**Resultado**: la cuenta aparece en estado **Pendiente** y suma al KPI de saldo total.

### Caso 2 — Registrar un pago

**Cuándo**: se le pagó al proveedor (transferencia, cheque, efectivo).

**Pasos**:

1. Seleccionar la cuenta en la tabla.
2. Hacer clic en **Registrar Pago**.
3. Llenar el monto pagado, la fecha y la referencia/observación.
4. **Guardar**.

**Resultado**: el saldo se reduce. Si el pago cubre el total, la cuenta pasa a **Pagada** (verde). Si es parcial, queda en **Parcial** (amarillo).

### Caso 3 — Ver el historial de pagos de una cuenta

**Pasos**:

1. Seleccionar la cuenta.
2. Hacer clic en **Ver Pagos**.

**Resultado**: se abre un detalle con todos los pagos registrados (fechas, montos, referencias).

### Caso 4 — Eliminar una cuenta

**Cuándo**: se cargó una cuenta por error.

**Pasos**:

1. Seleccionar la cuenta.
2. Hacer clic en **Eliminar**.
3. Confirmar el aviso `Eliminar cuenta '...' de [proveedor]? Se eliminaran todos sus pagos registrados.`

> ⚠ **Esto borra también los pagos asociados.** No usarlo si la cuenta ya tiene pagos válidos — preferir editarla.

### Caso 5 — Exportar a PDF

**Pasos**:

1. Aplicar el filtro deseado (ej. `Vencida`).
2. Hacer clic en **PDF**.
3. Elegir ubicación y nombre — el sistema genera y abre el archivo.

## Validaciones y errores frecuentes

| Mensaje | Qué significa | Cómo se resuelve |
|---|---|---|
| `Ingrese el concepto.` | Falta concepto al guardar | Llenar el campo |
| `El monto debe ser mayor a cero.` | Monto en 0 o negativo | Ingresar un monto > 0 |
| `Esta cuenta ya esta completamente pagada.` | Se intentó pagar una cuenta saldada | Verificar la fila correcta |
| `No hay datos para exportar.` | El listado filtrado está vacío | Cambiar el filtro |
| `Error al generar PDF: …` | Falla al generar el archivo | Reintentar; si persiste, verificar permisos de la carpeta destino |

## Consejos

- Filtrar por **Vencida** al inicio del día para ver qué hay que pagar urgente.
- Doble clic en una fila también abre el editor.
- Los KPIs en la franja superior cambian de **rojo** cuando hay deuda vencida — atención visual rápida.

## Pantallas relacionadas

- **`FrmProveedores`** — directorio de proveedores.
- **`FrmEgresos`** — egresos pagados; cuando se carga un egreso como pendiente aparece también acá.
- **`FrmFlujoCaja`** — proyección de salidas de dinero.
