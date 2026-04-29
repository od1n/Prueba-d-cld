# `FrmGastoEditor` — Editor de Egreso

> Diálogo emergente para **registrar o editar un egreso individual**. Soporta multi-moneda (USD / Bs a tasa BCV / Paralelo / Personalizada) con conversión automática.

## Cómo llegar

- Desde **`FrmEgresos` → + Nuevo egreso** (crear).
- Desde **`FrmEgresos` → doble clic en una fila de Materia Prima o Gastos Operativos** (editar).
- **Permiso requerido**: el mismo de `FrmEgresos` (`EgresosVer` o `Admin`).

## Para qué se usa

Cargar la información completa de un gasto: fecha, categoría, concepto, proveedor, moneda, tasa, monto, estado (pagado o pendiente), comprobante adjunto y observaciones. Cuando la categoría es de **Materia Prima**, además captura el insumo y la cantidad para actualizar el stock automáticamente.

## Qué ves en la pantalla

- **Banner azul oscuro** con el título *"Registrar egreso"* o *"Editar egreso #N"*.
- **Área scrollable** con los campos del egreso, en este orden:
  - **Fecha** y **Categoría** (desplegable).
  - **Concepto / descripción**.
  - **Proveedor / pagado a**.
  - **Panel "Materia Prima"** (azul claro, sólo si la categoría está marcada como MP): Insumo del catálogo, Cantidad, Precio unitario (USD).
  - **Sección "Pago — moneda y tasa"**:
    - **Tipo de pago**: `USD_DIRECTO`, `BCV`, `PARALELO`, `PERSONALIZADA`.
    - **Moneda original**: `USD — Dólares` o `BS — Bolívares`.
    - **Monto** y **Tasa Bs/USD aplicada**.
    - Líneas de equivalencia *"= USD x"* y *"= Bs y"* (informativas).
  - **Estado** *(checkbox "Marcar como PENDIENTE de pago (CxP)")* y **Vence el** (sólo si pendiente).
  - **Comprobante (foto/PDF, opcional)** con botones **📎 Adjuntar** y **Ver**.
  - **Observaciones** (texto libre).
- **Botonera inferior**: **Cancelar** (gris) y **Guardar egreso** (azul).

### Tipo de pago — comportamiento

| Tipo | Tasa | Moneda |
|---|---|---|
| **USD_DIRECTO** | bloqueada en 0 | forzada a USD |
| **BCV** | se carga la tasa BCV del día (bloqueada) | sugiere Bs (editable) |
| **PARALELO** | se carga la tasa paralelo del día (bloqueada) | sugiere Bs (editable) |
| **PERSONALIZADA** | editable (sirve para tasa específica del proveedor) | sugiere Bs |

## Casos de uso

### Caso 1 — Cargar un gasto pagado en USD

1. **Fecha**: hoy.
2. **Categoría**: la que corresponda (ej. *Servicios*).
3. **Concepto**: descripción breve.
4. **Proveedor**: a quién se le pagó.
5. **Tipo de pago**: `USD_DIRECTO`.
6. **Monto**: el valor en dólares.
7. Dejar **PENDIENTE** desmarcado.
8. **Guardar egreso**.

### Caso 2 — Cargar una compra de materia prima en Bs

1. **Categoría**: una marcada como Materia Prima (aparece el panel azul claro).
2. **Insumo del catálogo**: elegir.
3. **Cantidad** y **Precio unit. (USD)** → el sistema calcula `Cantidad × Precio = Monto`.
4. **Tipo de pago**: `BCV` (la tasa se llena sola).
5. **Moneda original**: `BS — Bolívares`. **Monto**: ingresar el monto en bolívares.
6. **Guardar egreso**.

> Esto **actualiza stock y kardex** del insumo automáticamente.

### Caso 3 — Adjuntar un comprobante

1. Hacer clic en **📎 Adjuntar**.
2. Elegir un archivo `.jpg`, `.png`, `.bmp` o `.pdf`.
3. El archivo se copia a `%AppData%\SistemaGestion\comprobantes` con nombre único.
4. Para abrirlo después, hacer clic en **Ver**.

### Caso 4 — Marcar como cuenta por pagar (CxP)

1. Llenar todo lo anterior.
2. Marcar el checkbox **PENDIENTE de pago (CxP)**.
3. Elegir **Vence el**.
4. **Guardar egreso**.

**Resultado**: el egreso aparece en el tab *Por Pagar (CxP)* de `FrmEgresos`.

## Validaciones y errores frecuentes

| Mensaje | Qué significa | Cómo se resuelve |
|---|---|---|
| `Concepto requerido` | El concepto está vacío | Llenar el campo |
| `Selecciona una categoría` | No se eligió categoría | Elegir del desplegable |
| `El monto debe ser mayor que cero.` | Monto inválido | Ingresar un monto > 0 |
| `Selecciona el insumo del catálogo para esta compra de MP.` | Categoría MP sin insumo | Elegir el insumo o cambiar la categoría |
| `La cantidad debe ser mayor que cero.` | Cantidad de MP en 0 | Corregir |
| `No se pudo adjuntar: …` | Falla al copiar el archivo | Verificar que el origen existe y hay permisos |
| `No hay comprobante adjunto.` | Se hizo clic en **Ver** sin haber adjuntado | Adjuntar primero |
| `Permiso denegado: …` | El usuario no puede guardar egresos | Pedir permiso al admin |

## Consejos

- **Pulsar Enter** dispara **Guardar egreso** (es el botón por defecto).
- **Pulsar Esc** cancela.
- Si el proveedor no aparece en el catálogo de insumos al cargar MP, abrir antes *Archivos → Insumos Proveed.* y darlo de alta.
- La línea verde *"= USD x"* y la morada *"= Bs y"* son **sólo informativas** — no se editan.
- Para gastos recurrentes (alquiler, internet) sugerencia: dejar la observación con un código fijo (ej. `ALQ-MAY`) para encontrarlos rápido al filtrar.

## Pantallas relacionadas

- **`FrmEgresos`** — listado y filtros de egresos.
- **`FrmCategoriaEditor`** — para crear nuevas categorías antes de cargar.
- **`FrmProductosProveedor`** — catálogo de insumos para compras MP.
