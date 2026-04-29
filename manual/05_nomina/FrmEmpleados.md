# `FrmEmpleados` — Catálogo de Empleados

> Catálogo maestro del personal. Aquí se cargan los empleados con su cédula, cargo, sueldo, tarifa por pieza/venta y estado. Es el origen de los datos que usa la nómina.

## Cómo llegar

- **Ruta**: *Menú principal → Nómina → Empleados*.
- **Permiso requerido**: `Nomina` (o `Admin`).

## Para qué se usa

Mantener la lista del personal:

- **Alta** de un empleado nuevo.
- **Editar** datos personales o de remuneración.
- **Dar de baja** (desactivar) — el registro se conserva, pero deja de aparecer en cálculos de nómina.

Los datos cargados aquí son los que la nómina usa para calcular sueldo devengado, descuentos legales y prestaciones.

## Qué ves en la pantalla

```
┌───────────────────────────────────────────────────────────────────────┐
│ Empleados                          [+ Nuevo][Editar][Dar de baja]     │
├───────────────────────────────────────────────────────────────────────┤
│ Filtrar: [✓] Solo activos                                             │
├───────────────────────────────────────────────────────────────────────┤
│ Cedula │ Nombre completo │ Cargo │ Tipo │ Ingreso │ Antig. │ ...      │
│ V-12.. │ Juan Pérez      │ Oper. │ Prod │ 02/01/22│ 4 anos │ ...      │
├───────────────────────────────────────────────────────────────────────┤
│ Empleados: 12 activos | 7 produccion | 3 oficina | 2 fijo             │
└───────────────────────────────────────────────────────────────────────┘
```

### Botones

| Botón | Qué hace |
|---|---|
| **+ Nuevo** | Abre el diálogo para crear un empleado. |
| **Editar** | Edita el empleado seleccionado (también doble clic). |
| **Dar de baja** | Activa/desactiva el empleado (alterna el estado). |
| **Cerrar** | Sale de la pantalla. |

### Filtro

| Control | Qué hace |
|---|---|
| **Solo activos** | Si está marcado, oculta a los empleados inactivos. |

### Tabla

| Columna | Significado |
|---|---|
| **Cedula** | Documento de identidad |
| **NombreCompleto** | Nombre y apellido |
| **Cargo** | Puesto |
| **TipoEmpleado** | `Produccion` · `Oficina` · `Fijo` |
| **Ingreso** | Fecha de ingreso |
| **Antiguedad** | Años de servicio |
| **SueldoDia** | Sueldo diario en USD |
| **TarifaPieza** | Pago por pieza producida (sólo Producción; `—` si no aplica) |
| **TarifaVenta** | Pago por venta cerrada (sólo Oficina; `—` si no aplica) |
| **Estado** | `Activo` · `Inactivo` |

### Resumen al pie

Muestra el conteo total de activos y la distribución por tipo (`Produccion`, `Oficina`, `Fijo`).

### Diálogo Nuevo / Editar empleado

Campos:

| Campo | Detalle |
|---|---|
| **Cedula** | Obligatorio. |
| **Nombre / Apellido** | Obligatorios. |
| **Cargo** | Texto libre. |
| **Tipo empleado** | `Produccion` · `Oficina` · `Fijo`. Cambia qué tarifa aparece visible (ver abajo). |
| **Fecha ingreso** | Por defecto hoy. |
| **Sueldo diario USD** | Base de cálculo del devengado por día trabajado. |
| **Sal. normal mensual USD** | Salario normal mensual (se usa para aportes patronales en bolívares). |
| **Tarifa por pieza USD** | Visible **solo si Tipo = Producción**. Pago por pieza de primera calidad. |
| **Tarifa por venta USD** | Visible **solo si Tipo = Oficina**. Pago por venta cerrada. |
| **Telefono / Email** | Opcionales. |
| **Empleado activo** | Casilla. Si se desmarca, queda inactivo (no aparece en nómina). |

## Casos de uso

### Caso 1 — Dar de alta un empleado

1. Hacer clic en **+ Nuevo**.
2. Llenar cédula, nombre, apellido, cargo.
3. Elegir el **Tipo empleado**:
   - `Produccion` — operario de planta. Activa el campo **Tarifa por pieza**.
   - `Oficina` — vendedor / administrativo. Activa el campo **Tarifa por venta**.
   - `Fijo` — empleado con sueldo fijo sin tarifas variables.
4. Cargar **Sueldo diario USD** y, si corresponde, **Sal. normal mensual USD** (este último se usa para los aportes patronales en bolívares).
5. Verificar que **Empleado activo** esté marcado.
6. Hacer clic en **Guardar empleado**.

### Caso 2 — Editar un empleado

1. Seleccionar la fila (o doble clic).
2. Hacer clic en **Editar**.
3. Modificar los campos.
4. **Guardar empleado**.

### Caso 3 — Dar de baja un empleado

1. Seleccionar la fila.
2. Hacer clic en **Dar de baja**.
3. Confirmar el aviso `Dar de baja a <nombre>?`.

**Resultado**: el empleado pasa a `Inactivo`. Si se vuelve a apretar el botón con la fila inactiva seleccionada, lo reactiva.

## Validaciones y errores frecuentes

| Mensaje en pantalla | Qué significa | Cómo se resuelve |
|---|---|---|
| `Ingresa la cedula.` | Campo cédula vacío | Llenarlo |
| `Ingresa nombre y apellido.` | Faltan datos personales | Llenar nombre y apellido |
| `Dar de baja a <nombre>?` | Confirmación de baja | Sí / No |

## Consejos

- **Usá `Fijo`** para empleados con sueldo mensual sin variable (administrativos, gerencia). El cálculo de nómina toma sólo el sueldo diario sin tarifas.
- **Tarifa por pieza** se carga con 4 decimales (es un valor pequeño por unidad). Por ejemplo $0,0125 por pieza.
- Cuando se da **baja** a un empleado, los datos históricos quedan: ya no figura en la nómina nueva, pero los recibos antiguos siguen disponibles.
- **Doble clic** en una fila abre el editor.

## Pantallas relacionadas

- **`FrmProcesarNomina`** — usa estos empleados para precargar la planilla semanal.
- **`FrmPrestamos`** — los préstamos se asocian a empleados.
- **`FrmBeneficiosLOTTT`** — los beneficios anuales se calculan por empleado activo.
- **`FrmTarifasBonoProduccion`** — parámetros de bonos por unidad producida.
