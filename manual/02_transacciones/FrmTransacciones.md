# `FrmTransacciones` — Submenú Transacciones

> Lanzador de las pantallas del módulo *Transacciones*: facturas, cobros, precios, egresos, cuentas por pagar y registro diario de producción.

## Cómo llegar

- **Ruta**: *Menú principal → Transacciones*
- **Atajo**: ninguno (se abre como ventana normal del menú)
- **Permiso requerido**: `Transacciones` (o `Admin`)

## Para qué se usa

Es la pantalla puente del módulo. No registra datos por sí misma — sólo agrupa los seis accesos de las pantallas operativas que mueven el dinero del día (ventas, cobros, pagos, gastos, ajuste de precios y producción). Funciona como un **panel de tarjetas**.

## Qué ves en la pantalla

Encabezado azul oscuro con el título *"Transacciones"* y el subtítulo *"Operaciones de venta, cobros, precios y egresos"*. Debajo, una grilla de tarjetas que se reordenan automáticamente al cambiar el ancho de la ventana. En el pie, un botón **Cerrar**.

### Tarjetas disponibles

| Tarjeta | Subtítulo | Abre |
|---|---|---|
| **Facturas** | Emitir y consultar | `FrmFacturas` |
| **Cuentas por Cobrar** | Saldos y vencimientos | `FrmCuentasCobrar` |
| **Cambio de Precios** | Actualizar precios y costos | `FrmCambioPrecios` |
| **Egresos** | Compras, gastos y MP | `FrmEgresos` |
| **Cuentas por Pagar** | Obligaciones pendientes | `FrmCuentasPagar` |
| **Ingreso Producción** | Registro diario | `FrmRegistroProduccionDia` |

Cada tarjeta tiene un ícono, color distintivo y al pasar el mouse muestra una breve descripción.

## Casos de uso

### Caso 1 — Abrir una pantalla del módulo

**Pasos**:

1. Hacer **clic** en la tarjeta del módulo deseado.
2. Se abre la pantalla correspondiente como ventana modal.
3. Al cerrarla, volvés al submenú.

## Consejos

- Es la entrada habitual al día de operación: comenzar por **Cuentas por Cobrar** para revisar saldos vencidos, luego **Facturas** para emitir las del día.
- El submenú **no muestra alertas** — para ver pendientes consolidados (CxC vencidas, CxP por vencer, stock bajo), abrir el *Dashboard* o *Alertas*.

## Pantallas relacionadas

- **`FrmFacturas`** — emisión de documentos de venta.
- **`FrmCuentasCobrar`** — seguimiento de cobros a clientes.
- **`FrmCambioPrecios`** — ajuste de precios de productos.
- **`FrmEgresos`** — registro de gastos y compras.
- **`FrmCuentasPagar`** — obligaciones con proveedores.
- **`FrmRegistroProduccionDia`** — producción diaria.
