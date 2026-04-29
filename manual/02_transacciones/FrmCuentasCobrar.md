# `FrmCuentasCobrar` — Cuentas por Cobrar

> Lista, edita y registra pagos de las cuentas por cobrar a clientes. Es la pantalla de seguimiento del cobrador o del responsable de ventas.

## Cómo llegar

- **Ruta**: *Menú principal → Transacciones → Cuentas por Cobrar*
- **Permiso requerido**: `Ventas` (o `Admin`)

## Para qué se usa

Concentra los **saldos pendientes de cobro** del negocio. Sirve para:

- Ver de un vistazo cuánto se nos debe y cuánto está vencido.
- Registrar **pagos** parciales o totales que llegan por transferencia, efectivo o cheque.
- Cargar manualmente cuentas que no provienen de una factura (ej. servicios prestados sin documento).
- Editar datos de una cuenta existente (concepto, fechas, monto).

> Las **facturas a crédito** emitidas en `FrmFacturas` aparecen automáticamente acá si así fue cargado el módulo de cobros. Las cuentas se actualizan a estado **Vencida** automáticamente cuando llega su fecha de vencimiento.

## Antes de empezar

- Tener identificado al cliente que va a pagar (nombre o código).
- Si se va a cargar una cuenta nueva, conocer monto, fecha de emisión y plazo.

## Qué ves en la pantalla

- **Cabecera azul oscuro** con el título *"Cuentas por Cobrar"* y los botones de acción a la derecha: **? Ayuda**, **+ Nueva**, **Editar**, **Reg. Pago**.
- **Tabla central** con todas las cuentas. Las filas se colorean según estado:
  - **Verde claro** → Saldada
  - **Rojo claro** → Vencida
  - **Amarillo claro** → Vigente (pendiente, no vencida)
- **Pie**: resumen *"Cuentas: N | Total cargado: $X | Saldo pendiente: $Y | Vencidas: Z"* y botón **Cerrar**.

### Columnas de la tabla

| Columna | Significado |
|---|---|
| **Cliente** | Nombre del cliente |
| **Concepto** | Descripción de la deuda |
| **Monto** | Total de la cuenta (USD) |
| **Pagado** | Lo abonado hasta la fecha |
| **Saldo** | `Monto − Pagado` |
| **Emision** | Fecha en que se generó la cuenta |
| **Vence** | Fecha límite de pago |
| **Estado** | Vigente · Vencida · Saldada |

## Casos de uso

### Caso 1 — Cargar una cuenta por cobrar manualmente

**Cuándo**: hay una deuda que no proviene de una factura (servicio, anticipo, etc.).

**Pasos**:

1. Hacer clic en **+ Nueva**.
2. En el diálogo **Nueva Cuenta**:
   - **Cliente**: nombre completo.
   - **Concepto**: descripción breve.
   - **Monto Total** y **Monto Pagado** (puede ser 0).
   - **Fecha Emisión** y **Fecha Vence** (por defecto +30 días).
   - **Estado**: dejar `Vigente`.
3. Hacer clic en **Guardar**.

**Resultado**: la cuenta aparece en la tabla con su saldo y fecha de vencimiento.

### Caso 2 — Registrar un pago

**Cuándo**: el cliente paga total o parcialmente.

**Pasos**:

1. Hacer clic en la fila de la cuenta a cobrar.
2. Hacer clic en **Reg. Pago**.
3. En el diálogo **Registrar Pago**, ingresar el **Monto a pagar** (por defecto se sugiere el saldo completo).
4. Hacer clic en **Registrar**.

**Resultado**: el sistema descuenta el monto del saldo. Si el pago cubre todo lo pendiente, la cuenta pasa a estado **Saldada** y se colorea verde. Aparece el aviso `"Pago de $X registrado."`.

### Caso 3 — Editar datos de una cuenta

**Pasos**:

1. Seleccionar la fila.
2. Hacer clic en **Editar**.
3. Modificar campos (cliente, concepto, fechas, monto, estado).
4. **Guardar**.

## Validaciones y errores frecuentes

| Mensaje | Qué significa | Cómo se resuelve |
|---|---|---|
| `Ingrese cliente.` | El campo *Cliente* está vacío | Llenar el nombre del cliente |
| `Esta cuenta ya esta saldada.` | Se intentó registrar un pago en una cuenta cerrada | No es posible cobrar dos veces; verificar la fila correcta |

## Consejos

- Revisar esta pantalla **al inicio de cada día** para identificar las cuentas vencidas que requieren llamada o cobro.
- Los pagos parciales son válidos — el sistema sigue calculando el saldo.
- Si el cliente paga **más** de lo que debe, el sistema topa el pagado al monto total y marca la cuenta como saldada.

## Pantallas relacionadas

- **`FrmFacturas`** — facturas a crédito que generan cuentas.
- **`FrmClientes`** — directorio de clientes para identificar quién paga.
- **`FrmFlujoCaja`** — proyección de los cobros esperados.
