# `FrmTarifasBonoProduccion` — Tarifas de Bono (Producción y Ventas)

> Configura las **tarifas de bono por unidad** que reciben los empleados. Tiene dos pestañas: *Bono Producción* (Bs por unidad producida) y *Bono Ventas* (Bs por unidad vendida).

## Cómo llegar

- **Ruta**: *Menú principal → Archivos → Tarifas Bono*
- **Permiso requerido**: 🔒 **Admin únicamente**. La pantalla muestra en el título *"Configuracion de Tarifas de Bono (solo Admin)"*.

## Para qué se usa

Definir cuánto bono adicional (en bolívares) recibe cada empleado por cada unidad producida o vendida. Estas tarifas alimentan el **cálculo de la nómina** de los empleados de producción y de oficina/ventas.

Cada tarifa tiene una **fecha de vigencia desde** y opcionalmente una **fecha de vencimiento**. Cuando se "cierra" una tarifa, queda como histórico y deja de aplicarse a nuevos cálculos.

## Qué ves en la pantalla

- **Cabecera morada** con el título *"Configuracion de Tarifas de Bono (solo Admin)"*.
- **Pestañas**:
  - **Bono Produccion** (cabecera tabla morada) — *"Bs por unidad producida — aplica a empleados tipo Produccion"*.
  - **Bono Ventas** (cabecera tabla azul) — *"Bs por unidad vendida — aplica a empleados tipo Oficina/Ventas"*.
- En cada pestaña:
  - **Barra de filtros**: desplegable **Empleado** (`Todos` o uno específico), checkbox **Solo vigentes** (marcado por defecto), botones **+ Nueva**, **Editar**, **Cerrar tar.**, **Eliminar**.
  - **Tabla** con las tarifas. Las cerradas aparecen en gris.
  - **Pie** con descripción y conteo *"N tarifas | M vigentes"*.
- Botón **Cerrar** para salir.

### Columnas

| Columna | Significado |
|---|---|
| **Empleado** | Nombre completo |
| **Producto** | Código y descripción del producto |
| **Tarifa** | Bs/und con dos decimales |
| **Desde** | Fecha de inicio de vigencia |
| **Hasta** | Fecha de fin (`Vigente` si no tiene) |
| **Estado** | `Activa` (verde) · `Cerrada` (gris) |

> Doble clic en una fila abre el editor.

## Casos de uso

### Caso 1 — Crear una tarifa nueva

1. Elegir la pestaña adecuada (**Bono Produccion** o **Bono Ventas**).
2. **+ Nueva**.
3. En el diálogo **Nueva Tarifa Bono ...**:
   - **Empleado**: elegir del desplegable (sólo aparecen activos).
   - **Producto**: elegir del desplegable.
   - **Tarifa (Bs/und)**: ingresar el monto.
   - **Vigente desde**: fecha (por defecto hoy).
   - **Sin fecha de vencimiento** (marcado por defecto) o desmarcar y elegir **Vence**.
4. Aparece debajo una vista previa: *"Por cada unidad producida/vendida, el empleado recibirá X Bs adicionales en nómina."*
5. **Guardar**.

### Caso 2 — Editar una tarifa

1. Doble clic (o **Editar**).
2. Modificar y **Guardar**.

### Caso 3 — Cerrar una tarifa (sin eliminar)

**Cuándo**: la tarifa deja de aplicarse pero queremos guardar el histórico.

1. Seleccionar la tarifa.
2. **Cerrar tar.**
3. Confirmar `Cerrar tarifa de [empleado] para [producto]?`.

**Resultado**: pasa a estado **Cerrada** (texto gris). Ya no se aplica al cálculo de nómina.

### Caso 4 — Eliminar definitivamente

1. Seleccionar.
2. **Eliminar**.
3. Confirmar `Eliminar tarifa de [empleado]?`.

> ⚠ **Preferir Cerrar** sobre Eliminar para conservar trazabilidad histórica.

### Caso 5 — Filtrar por empleado

1. En el desplegable **Empleado** elegir uno específico.
2. La tabla queda con sólo sus tarifas.

## Validaciones y errores frecuentes

| Mensaje | Qué significa | Cómo se resuelve |
|---|---|---|
| `Selecciona un empleado.` | Faltó empleado | Elegir del desplegable |
| `Selecciona un producto.` | Faltó producto | Elegir del desplegable |
| `La tarifa debe ser mayor a 0.` | Tarifa en 0 | Ingresar monto > 0 |
| `Esta tarifa ya esta cerrada.` | Se intentó cerrar una ya cerrada | Verificar la fila |

## Consejos

- Las tarifas son **por (empleado × producto)** — un empleado puede tener tarifas distintas para distintos productos.
- Cuando cambia la tarifa de un empleado, **cerrar la vieja** y crear una nueva con la fecha de hoy. Eso preserva la trazabilidad: lo producido antes del cambio se paga con la tarifa anterior.
- La **tarifa en bolívares** se aplica directamente al multiplicar por unidades — no se convierte a USD ni se ajusta por tasa.

## Pantallas relacionadas

- **`FrmEmpleados`** — empleados que reciben bono.
- **`FrmInventario`** — productos asociados.
- **`FrmProcesarNomina`** — usa estas tarifas al calcular la nómina semanal.
- **`FrmRegistroProduccionDia`** / **`FrmIngresoProduccion`** — registra las unidades producidas que disparan el bono.
