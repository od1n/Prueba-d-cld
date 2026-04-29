# `FrmPrestamos` — Préstamos Internos a Empleados

> Lista de préstamos otorgados a empleados, con cuota semanal que se descuenta automáticamente al procesar la nómina. Permite registrar nuevos, abonar manualmente o cancelar.

## Cómo llegar

- **Ruta**: *Menú principal → Nómina → Préstamos*.
- **Permiso requerido**: `Nomina` para registrar y abonar; `Admin` para **cancelar**.

## Para qué se usa

- **Registrar** un préstamo nuevo a un empleado activo.
- **Abonar manualmente** (cuando el empleado paga fuera de la nómina).
- **Cancelar** un préstamo (sólo Admin).
- Ver el saldo pendiente y cuántas semanas faltan para terminarlo.

> Las cuotas se descuentan **solas** al procesar la nómina semanal. Esta pantalla no se usa para las cuotas regulares.

## Antes de empezar

- Tener empleados **activos** (sin activos no se puede abrir el alta de préstamo).

## Qué ves en la pantalla

```
┌────────────────────────────────────────────────────────────────────────┐
│ Prestamos a Empleados        [? Ayuda][+ Nuevo][Abonar][Cancelar]      │
├────────────────────────────────────────────────────────────────────────┤
│ Empleado │ Otorgado │ Total │ CuotaSem │ Saldo │ Semanas │ Estado │ ...│
│ Pérez J  │ 02/03/26 │ $300  │  $25,00  │ $150  │ 6 sem.  │ Activo │ ...│
│ Marin M  │ 12/01/26 │ $500  │  $50,00  │  $0   │   —     │Saldado │ ...│
├────────────────────────────────────────────────────────────────────────┤
│ Prestamos activos: 6  |  Saldo total pendiente: $1.245,00  |  Saldados: 14│
└────────────────────────────────────────────────────────────────────────┘
```

### Botones

| Botón | Qué hace |
|---|---|
| **+ Nuevo** | Abre el diálogo para otorgar un préstamo. |
| **Abonar** | Registra un abono manual al préstamo seleccionado. |
| **Cancelar** | Cancela el préstamo seleccionado. *Requiere `Admin`.* |
| **? Ayuda** | Abre esta ficha del manual. |
| **Cerrar** | Sale de la pantalla. |

### Tabla

| Columna | Significado |
|---|---|
| **Empleado** | Nombre completo |
| **Otorgado** | Fecha en que se otorgó |
| **Total** | Monto original en USD |
| **CuotaSem** | Cuota semanal en USD |
| **Saldo** | Saldo pendiente |
| **Semanas** | Cuántas semanas faltan para liquidar (al ritmo de la cuota) |
| **Estado** | `Activo` (blanco) · `Saldado` (verde) · `Cancelado` (rojo) |
| **Descripcion** | Motivo del préstamo |

### Diálogo: Nuevo préstamo

| Campo | Detalle |
|---|---|
| **Empleado** | Lista de empleados activos. |
| **Monto total USD** | Monto del préstamo. **Obligatorio (>0).** |
| **Cuota semanal USD** | Cuánto se descontará cada semana en la nómina. |
| **Descripción** | Texto libre con el motivo. |

### Diálogo: Abono manual

Muestra el **saldo pendiente** del préstamo y permite ingresar un **monto a abonar** (no puede superar el saldo). Por defecto trae la cuota semanal.

## Casos de uso

### Caso 1 — Otorgar un préstamo

1. Hacer clic en **+ Nuevo**.
2. Elegir el **Empleado** del desplegable.
3. Cargar **Monto total USD** y **Cuota semanal USD**.
4. (Opcional) **Descripción** del motivo.
5. **Registrar prestamo**.

**Resultado**: el préstamo aparece en la lista en `Activo`. La próxima nómina semanal traerá la cuota descontada automáticamente.

### Caso 2 — Registrar un abono manual

**Cuándo**: el empleado pagó por fuera de la nómina (efectivo, transferencia).

**Pasos**:

1. Seleccionar el préstamo en la tabla.
2. Hacer clic en **Abonar**.
3. Ajustar el **Monto a abonar** (por defecto = cuota semanal).
4. **Registrar abono**.

**Resultado**: el saldo pendiente baja. Si llega a 0, el préstamo pasa a `Saldado`.

### Caso 3 — Cancelar un préstamo (sólo Admin)

1. Seleccionar el préstamo.
2. Hacer clic en **Cancelar**.
3. Confirmar el aviso `¿Cancelar este préstamo?`.

**Resultado**: el préstamo pasa a `Cancelado`. Si el usuario no es Admin, aparece: `Solo el administrador puede cancelar préstamos.`

## Validaciones y errores frecuentes

| Mensaje en pantalla | Qué significa | Cómo se resuelve |
|---|---|---|
| `No hay empleados activos registrados.` | No se puede registrar un préstamo sin empleados activos | Dar de alta empleados primero |
| `Ingresa el monto.` | Monto en cero al crear el préstamo | Llenar el monto |
| `¿Cancelar este préstamo?` | Confirmación de cancelación | Sí / No |
| `Solo el administrador puede cancelar préstamos.` | Usuario sin permiso | Pedir al admin que cancele |
| `Error al cancelar: …` | Falla técnica | Reintentar; si persiste, llamar al administrador |

## Consejos

- **La cuota semanal** es la que se descuenta en la nómina automáticamente. Si querés que no se descuente, ponela en 0 (el préstamo seguirá vigente y se irá saldando con abonos manuales).
- **Cancelar** un préstamo **no devuelve** el dinero descontado: simplemente cierra el saldo. Para condonar formalmente, dejá una observación clara en *Auditoría*.
- Doble pago: si el empleado paga manual y la cuota también se descuenta, el saldo va a llegar antes a 0. Después de "Saldado", la cuota deja de aplicarse.
- Las cuotas se aplican **al editar la línea de nómina** del empleado en `FrmProcesarNomina`.

## Pantallas relacionadas

- **`FrmEmpleados`** — empleados activos que pueden recibir préstamos.
- **`FrmProcesarNomina`** — descuenta automáticamente la cuota semanal.
