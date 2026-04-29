# `FrmOrdenesProduccion` — Órdenes de Producción

> Planifica, ejecuta y cierra órdenes de producción. Cada orden indica qué producto se va a fabricar, cuántas unidades y qué materias primas requiere — al completarla, descuenta automáticamente las materias y suma el producto al inventario.

## Cómo llegar

- **Ruta**: *Menú principal → Producción → Órdenes de Producción*.
- **Permiso requerido**: `IngresoProduccion` (o `Admin`).

## Para qué se usa

Es el centro de planificación de planta. Sirve para:

- Crear una **orden** (OP) con producto a fabricar, cantidad planificada, fechas de inicio/fin, responsable y materias primas requeridas.
- Verificar **stock** de materias primas antes de arrancar (la pantalla marca en rojo lo que falta).
- **Completar** la orden cuando se termina la fabricación: el sistema pide la cantidad real producida, descuenta los insumos y suma el producto terminado al kardex.
- **Cancelar** o **eliminar** órdenes que ya no aplican.

## Antes de empezar

- Tener cargado el **producto a fabricar** en *Archivos → Inventario*.
- Tener cargadas las **materias primas** que se van a consumir, también en inventario, con su existencia actual.

## Qué ves en la pantalla

```
┌────────────────────────────────────────────────────────────────────────┐
│ Ordenes de Produccion       [+ Nueva OP][Abrir][Completar OP]          │
│                              [Cancelar OP][Eliminar]                   │
├────────────────────────────────────────────────────────────────────────┤
│ Estado: [Todos ▼]                                                      │
├────────────────────────────────────────────────────────────────────────┤
│ NumOrden│Producto│Plan│Producido│Inicio│Fin│Estado│Responsable│Creada  │
│ OP-0021 │Tapa 1L │ 500│   480   │28/04 │30 │Completada│ Juan  │28/04...│
│ OP-0022 │Pote 5L │ 200│    —    │29/04 │05 │En proceso│ Maria │29/04...│
├────────────────────────────────────────────────────────────────────────┤
│ Total: 22  Borrador: 1  En proceso: 5  Completadas: 14  Canceladas: 2  │
└────────────────────────────────────────────────────────────────────────┘
```

### Botones

| Botón | Qué hace |
|---|---|
| **+ Nueva OP** | Abre el diálogo para crear una orden nueva. |
| **Abrir** | Abre la orden seleccionada para edición / consulta. (También doble clic en la fila). |
| **Completar OP** | Cierra la orden con la cantidad real producida. |
| **Cancelar OP** | Marca la orden como `Cancelada`. |
| **Eliminar** | Borra la orden permanentemente (solo si está en `Borrador` o `Cancelada`). |
| **Cerrar** | Sale de la pantalla. |

### Filtro

El desplegable **Estado** filtra entre `Todos`, `Borrador`, `En proceso`, `Completada`, `Cancelada`.

### Colores de fila

- **Verde claro** — `Completada`.
- **Amarillo claro** — `En proceso`.
- **Rojo claro** — `Cancelada`.
- **Blanco** — `Borrador`.

### Diálogo Nueva / Abrir orden

Tres bloques:

**Cabecera (panel izquierdo)**

| Campo | Detalle |
|---|---|
| **No. Orden** | Generado automáticamente por el sistema (no se puede editar). |
| **Producto a fabricar** | Botón **...** abre selector de inventario. |
| **Cantidad planificada** | Mínimo 1. Recalcula los totales requeridos en el grid de materias. |
| **Fecha inicio / Fecha fin** | Por defecto hoy y hoy + 7 días. |
| **Responsable** | Texto libre. |
| **Estado** | `Borrador`, `En proceso`, `Completada`, `Cancelada`. |
| **Observaciones** | Texto libre multilínea. |

**Materias primas requeridas (panel derecho)**

| Columna | Significado |
|---|---|
| **Codigo** | Código de la materia prima |
| **Descripcion** | Nombre |
| **Cant/Unidad** | Cantidad necesaria por unidad fabricada (editable) |
| **Total req.** | Calculado: `Cant/Unidad × Cantidad planificada` |
| **En stock** | Existencia actual en inventario |
| **OK?** | `Si` (verde) si hay stock suficiente, `NO` (rojo) si falta |

Botones: **+ Agregar materia** abre selector de inventario; **Quitar** quita la fila seleccionada. Al pie aparece la alerta `N materia(s) con stock insuficiente — se marcaron en rojo` si corresponde.

## Casos de uso

### Caso 1 — Crear una nueva orden de producción

1. Hacer clic en **+ Nueva OP**.
2. Llenar el panel izquierdo: hacer clic en **...** para elegir el **producto a fabricar**, luego ingresar la **cantidad planificada**, fechas y responsable. Dejar el **Estado** en `Borrador` o `En proceso`.
3. En el panel derecho hacer clic en **+ Agregar materia** por cada materia prima necesaria.
4. Para cada materia, ajustar la columna **Cant/Unidad** (cuánto se consume por unidad fabricada). El **Total req.** y la columna **OK?** se recalculan solas.
5. Si todo está en verde, hacer clic en **Guardar Orden**.

**Resultado**: la orden queda creada con su número correlativo (`OP-NNNN`).

### Caso 2 — Completar (cerrar) una orden

**Cuándo**: terminó la fabricación y hay que registrar la producción real.

1. Seleccionar la orden en la lista.
2. Hacer clic en **Completar OP**.
3. Si hay materias con stock insuficiente, aparece el aviso:
   - `ATENCION — Stock insuficiente en: <materia: requiere X, disponible Y>` y la pregunta `¿Deseas completar de todas formas con el stock disponible?`. Responder según corresponda.
4. Aparece el diálogo **Completar Orden** con el dato `Cantidad planificada: N`.
5. Ingresar la **Cantidad producida** real (puede ser distinta de la planificada).
6. Hacer clic en **Completar orden**.

**Resultado**:

- La orden cambia a `Completada`.
- El **producto terminado** se suma al kardex (entrada).
- Las **materias primas** se descuentan del kardex (salidas).
- Aparece el aviso: `Orden OP-NNNN completada. Producido: N unidades. El inventario fue actualizado automaticamente.`

### Caso 3 — Cancelar una orden

1. Seleccionar la orden.
2. Hacer clic en **Cancelar OP** y confirmar.

**Resultado**: la orden queda en `Cancelada`. Una orden ya **`Completada`** no se puede cancelar (`No se puede cancelar una orden completada.`).

### Caso 4 — Eliminar una orden

Solo se puede eliminar si está en `Borrador` o `Cancelada`. Si está `En proceso` o `Completada`, aparece: `Solo se pueden eliminar ordenes en Borrador o Canceladas.`

## Validaciones y errores frecuentes

| Mensaje en pantalla | Qué significa | Cómo se resuelve |
|---|---|---|
| `Selecciona el producto a fabricar.` | No se eligió producto | Hacer clic en **...** y elegir |
| `Esta orden ya esta completada.` / `cancelada.` | Se intentó completar una orden ya cerrada | Sin acción |
| `ATENCION — Stock insuficiente en: …` | Faltan materias primas para la cantidad planificada | Comprar/recibir materia o aceptar producir parcialmente |
| `No se puede cancelar una orden completada.` | Una OP ya cerrada no se reabre | Generar otra orden si hay que rectificar |
| `Solo se pueden eliminar ordenes en Borrador o Canceladas.` | Eliminación no permitida | Cancelar primero, luego eliminar |
| `Cancelar orden OP-NNNN?` / `Eliminar orden OP-NNNN permanentemente?` | Confirmación | Sí / No |
| `Error al completar la orden: …` | Falla técnica (BD/kardex) | Reintentar; si persiste, llamar al administrador |

## Consejos

- **Doble clic** en una fila abre la orden directamente.
- Cuando una materia aparece en **rojo (NO)**, podés decidir comprar/fabricar primero o cerrar la orden con la producción posible — el sistema avisa y pide confirmación.
- **Crear primero la orden en Borrador** te permite ir ajustando materias y cantidades. Cambiá a `En proceso` cuando arranque la fabricación, y completá al final.
- Una vez **completada**, no se puede modificar la cantidad real. Si te equivocaste, registrá un ajuste manual de inventario en *Kardex*.

## Pantallas relacionadas

- **`FrmInventario`** — productos terminados y materias primas que aparecen en los selectores.
- **`FrmKardex`** — movimientos generados al completar la orden.
- **`FrmRegistroProduccionDia`** — registro diario complementario (por turno y por molde).
- **`FrmMermas`** — reporte de mermas que se nutre de las órdenes completadas.
- **`FrmControlCalidad`** — inspección de los lotes producidos.
