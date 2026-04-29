# `FrmFacturas` — Facturas

> Lista, emite, anula y reimprime las facturas de venta. Es la pantalla diaria del personal de ventas.

## Cómo llegar

- **Ruta**: *Menú principal → Transacciones → Facturas*
- **Atajo**: `F3 → "Facturas"` desde cualquier pantalla
- **Permiso requerido**: `Ventas` (o `Admin`)

## Para qué se usa

Esta pantalla concentra todo lo relacionado con **documentos de venta**: facturas con IVA, facturas de exportación (IVA 0% legal) y notas de entrega (sin valor fiscal). Se usa para:

- Emitir un **nuevo documento** de venta a un cliente.
- Consultar, **imprimir** o exportar a PDF documentos ya emitidos.
- **Anular** una factura con motivo registrado en auditoría.
- Generar el **reporte PDF** del listado del período.

## Antes de empezar

- Tener cargada la **tasa BCV del día** (la usa para convertir Bs↔USD).
- Tener al menos un **cliente activo** registrado en *Archivos → Clientes*.
- Tener stock disponible de los productos a facturar (si descuenta inventario).

## Qué ves en la pantalla

```
┌────────────────────────────────────────────────────────────────────────┐
│ Facturas         [? Ayuda][+ Nueva][Ver detalle][PDF][Imprimir]        │  ← cabecera azul
│                  [Reporte PDF][Corregir][Anular]                       │
├────────────────────────────────────────────────────────────────────────┤
│ Tipo │ NumeroF │ Cliente │ Fecha │ Forma │ Desc │ IVA │ Total │ Abono │  ← tabla
│ FAC  │  00125  │ J.Pérez │ 28/04 │ Cont. │  0%  │ ... │  ...  │  ...  │
│ NE   │  00126  │ M&M     │ 28/04 │ Créd. │  0%  │  —  │  ...  │  ...  │
├────────────────────────────────────────────────────────────────────────┤
│ Documentos: 124  |  Facturas legales: 118  |  Total: …  |  [Cerrar]    │  ← pie con totales
└────────────────────────────────────────────────────────────────────────┘
```

### Cabecera (botonera)

| Botón | Qué hace |
|---|---|
| **+ Nueva** | Abre el diálogo de creación de un documento de venta. |
| **Ver detalle** | Muestra los datos y líneas del documento seleccionado en modo solo lectura. |
| **Exportar PDF** | Genera el PDF de la factura seleccionada y lo abre. |
| **Imprimir** | Manda directo a la impresora la factura seleccionada. |
| **Reporte PDF** | Genera el reporte PDF del listado actual. |
| **Corregir** | Sólo para facturas con fecha inválida (anuladas por la migración v21). |
| **Anular** | Anula la factura seleccionada con motivo. *Requiere permiso `FacturasAnular` o Admin.* |
| **? Ayuda** | Abre esta ficha del manual. |

### Tabla (columnas)

| Columna | Significado |
|---|---|
| **Tipo** | `FAC` factura con IVA · `EXP` factura de exportación · `NE` nota de entrega |
| **NumeroF** | Número correlativo del documento (asignado por el operador) |
| **Cliente** | Código del cliente |
| **Fecha** | Fecha de emisión (o `(fecha inválida)` si está rota) |
| **FormaPago** | Contado / Crédito / Cheque / Transferencia / Mixto |
| **Desc** | Descuento global aplicado (porcentaje) |
| **IVA** | Monto de IVA (`—` para notas de entrega) |
| **Total** | Total del documento |
| **Abono** | Pagado hasta la fecha (`—` si está anulada · `CANCELADA` si fue cancelada legacy) |
| **Saldo** | `Total − Abono` |
| **Estado** | `Pendiente` · `Saldada` · `Anulada` · `Cancelada` |

> **Tip:** podés ordenar por **NumeroF**, **Cliente**, **Fecha** o **Total** haciendo clic en el encabezado de la columna. Un segundo clic invierte el orden.

### Pie (resumen)

Muestra agregados del listado: total de documentos, cuántos están anulados, total de facturas legales, total emitido, total cobrado y saldo pendiente. Si hay notas de entrega, también muestra cuántas son y por qué monto.

> Las **facturas anuladas** y **notas de entrega** no se cuentan en los totales de "Facturas legales" — son excluidas porque no son ventas declarables al SENIAT.

---

## Casos de uso

### Caso 1 — Emitir una factura con IVA al contado

**Cuándo**: el cliente paga en el momento, con factura formal.

**Pasos**:

1. Hacer clic en **+ Nueva**.
2. En el panel izquierdo:
   - **Cliente**: seleccionar el cliente del desplegable (o registrarlo antes en *Archivos → Clientes*).
   - **Número**: ingresar el número correlativo de la factura.
   - **Fecha**: dejar la del día (por defecto).
   - **Forma de pago**: `Contado`.
   - **IVA ($)**: ingresar el monto de IVA (suele ser 16% del subtotal).
   - **Abono ($)**: ingresar el monto pagado (igual al total para contado).
   - **Total ($)**: se calcula automáticamente con las líneas.
3. En el panel derecho hacer clic en **+ Agregar producto** y seleccionar el producto del catálogo. Repetir por cada línea.
4. Ajustar **Cantidad** y **Precio** en el grid de líneas (el subtotal se recalcula).
5. El banner inferior debe mostrar `✓ Factura con IVA — documento fiscal` (verde).
6. Hacer clic en **Guardar**.

**Resultado**: el documento queda en la lista con estado `Saldada`. El stock se descuenta automáticamente del kardex. Para imprimir, seleccionar la fila y **Imprimir** o **Exportar PDF**.

### Caso 2 — Emitir una factura a crédito

**Cuándo**: el cliente se lleva la mercancía y paga después.

**Pasos**:

1. **+ Nueva**.
2. Llenar igual que el caso 1, pero:
   - **Forma de pago**: `Credito`.
   - **Abono ($)**: `0` (o el adelanto si el cliente pagó parte).
   - **Días crédito**: cantidad de días para el cobro (ej. 30, 60).
3. **Guardar**.

**Resultado**: el documento queda con estado `Pendiente`. Aparece automáticamente en *Transacciones → Cuentas por Cobrar* para el seguimiento.

### Caso 3 — Emitir una nota de entrega (sin valor fiscal)

**Cuándo**: se entrega mercancía a un consignatario, sucursal o cliente que aún no recibe la factura formal.

**Pasos**:

1. **+ Nueva**.
2. Llenar igual que el caso 1, pero:
   - **IVA ($)**: `0`.
   - Dejar **Es exportación** *desmarcado*.
3. El banner inferior debe mostrar `⚠ NOTA DE ENTREGA — sin valor fiscal` (ámbar).
4. **Guardar**.

**Resultado**: el documento se guarda con tipo `NE`. **No suma a los totales fiscales** ni aparece en el libro de IVA. Por defecto el dashboard y los reportes la excluyen — un Admin puede activar **"Vista NE: ON"** desde el menú principal para incluirlas temporalmente.

### Caso 4 — Emitir una factura de exportación (IVA 0%)

**Cuándo**: venta a cliente extranjero con tasa 0% legal.

**Pasos**:

1. **+ Nueva**.
2. Llenar igual que el caso 1, pero:
   - **IVA ($)**: `0`.
   - Marcar **Es exportación (IVA 0% legal)**.
   - Aparece el campo **Destino exp.** — ingresar el país o destino.
3. El banner inferior muestra `✓ Factura de Exportación — IVA 0% legal` (azul).
4. **Guardar**.

**Resultado**: documento tipo `EXP`. Cuenta como factura legal en reportes pero con IVA 0.

### Caso 5 — Reimprimir o exportar a PDF una factura existente

**Pasos**:

1. Buscar la factura en la tabla (ordenar por **NumeroF** o **Fecha** ayuda).
2. Hacer **clic** en la fila para seleccionarla.
3. Hacer clic en **Exportar PDF** (abre el PDF) o **Imprimir** (envía a la impresora predeterminada).

### Caso 6 — Anular una factura

**Cuándo**: la factura fue emitida con un error, el cliente devolvió el pedido o se duplicó. *Requiere permiso `FacturasAnular` o Admin.*

**Pasos**:

1. Seleccionar la factura en la tabla.
2. Hacer clic en **Anular**.
3. Se abre el diálogo **Anular factura**: leer el aviso (la operación queda en auditoría) e ingresar el **Motivo** (mínimo 5 caracteres, p. ej. *"Cliente devolvió pedido completo"*).
4. Hacer clic en **Anular factura** (rojo).
5. Confirmar el segundo aviso de seguridad.

**Resultado**: la factura cambia a estado `Anulada` y queda excluida de totales y CxC, pero **permanece en la base de datos** para auditoría fiscal. El correlativo SENIAT *no se reutiliza*.

> ⚠ **El stock NO se revierte automáticamente.** Si la factura ya había descontado inventario, hay que registrar manualmente la entrada en *Archivos → Kardex* (ajuste de entrada).

### Caso 7 — Corregir la fecha de una factura anulada por fecha inválida (migración v21)

**Cuándo**: una factura tiene `(fecha inválida)` en la columna Fecha o fue anulada con motivo "Fecha". *Requiere permiso `FacturasAnular` o Admin.*

**Pasos**:

1. Seleccionar la factura.
2. Hacer clic en **Corregir**.
3. En el diálogo **Corregir fecha**, elegir la **Nueva fecha** (rango: 2000 – año actual + 1).
4. **Guardar fecha**.
5. Si la factura estaba anulada, el sistema pregunta si se quiere **reactivar**: responder `Sí` para volver al estado anterior.

**Resultado**: la fecha queda corregida y, si se reactivó, el documento vuelve a contar en totales y reportes.

> Esta opción **no aplica** a facturas normales — para editar otros campos hay que anular y emitir una nueva.

---

## Validaciones y errores frecuentes

| Mensaje en pantalla | Qué significa | Cómo se resuelve |
|---|---|---|
| `Indica el numero de factura` | El campo Número está vacío | Ingresar el correlativo |
| `Selecciona un cliente valido` | No hay cliente seleccionado o el código es inválido | Elegir un cliente del desplegable |
| `Agrega al menos un producto` | No hay líneas en el grid | Hacer clic en **+ Agregar producto** |
| `La cantidad debe ser mayor que cero` | Una línea tiene cantidad 0 o negativa | Editar la cantidad en el grid |
| `El precio unitario debe ser mayor que cero` | Una línea tiene precio 0 | Editar el precio en el grid |
| `Stock insuficiente para: XXX` | El producto XXX no tiene unidades disponibles | Reducir cantidad, comprar/fabricar primero, o anular la operación |
| `No hay lineas validas para guardar` | Todas las líneas tienen errores | Revisar cantidades y precios |
| `Selecciona una factura primero` | Se intentó imprimir/anular sin elegir fila | Hacer clic en una fila de la tabla |
| `No tienes permiso para anular facturas` | El usuario no es Admin ni tiene `FacturasAnular` | Solicitar permiso al administrador |
| `La factura X ya está anulada` | Se intentó anular dos veces | Ya fue anulada; el motivo aparece en el aviso |
| `Esta opción es solo para facturas con fecha inválida` | Se usó **Corregir** en una factura normal | Para editar una factura normal, anular y emitir nueva |
| `Error al guardar la factura: …` | Falla al guardar (ej. error de BD o kardex) | Reintentar; si persiste, llamar al administrador |

---

## Consejos

- **Verificar la tasa BCV** antes de emitir cualquier factura (en la franja superior del menú principal). Si la tasa está en rojo o ámbar, actualizarla primero.
- **No reutilizar números de factura anulada** — el sistema bloquea el correlativo SENIAT por compliance fiscal.
- **Notas de entrega**: usalas sólo para entregas internas o consignación. Si el cliente final paga, hay que emitir luego una factura formal.
- **Anular una factura no devuelve stock**. Acordate de hacer el ajuste en Kardex si corresponde.
- **Doble clic** en una fila abre el detalle directamente (equivalente a **Ver detalle**).

---

## Pantallas relacionadas

- **`FrmClientes`** — alta y consulta de clientes que aparecen en el desplegable.
- **`FrmInventario`** — productos disponibles para facturar y sus precios base.
- **`FrmCambioPrecios`** — ajustar precios de venta antes de facturar.
- **`FrmCuentasCobrar`** — seguimiento y cobro de las facturas a crédito generadas aquí.
- **`FrmKardex`** — registrar la entrada manual cuando se anula una factura con stock descontado.
- **`FrmLibroIVA`** — reporte fiscal que consolida estas facturas para SENIAT.
- **`DlgAnularFactura`** — diálogo emergente del Caso 6.
