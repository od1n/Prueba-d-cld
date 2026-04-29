# `FrmBeneficiosLOTTT` — Beneficios anuales LOTTT

> Calcula y registra los beneficios laborales obligatorios de la LOTTT venezolana: utilidades, vacaciones, bono vacacional y prestaciones sociales. Permite marcar como pagado y exportar el reporte en PDF.

## Cómo llegar

- **Ruta**: *Menú principal → Nómina → Beneficios LOTTT*.
- **Permiso requerido**: `Nomina` para calcular y marcar pagado; `Admin` para **eliminar** registros.

## Para qué se usa

Es el módulo legal de la nómina. Cubre los beneficios anuales que la LOTTT obliga a pagar:

| Beneficio | Norma | Cuándo se paga |
|---|---|---|
| **Utilidades** | LOTTT Art. 131 | Diciembre, mínimo 15 días de salario normal |
| **Vacaciones** | LOTTT Art. 190 | Aniversario de ingreso, 15 días + 1 día por año adicional |
| **Bono Vacacional** | LOTTT Art. 192 | Aniversario, 7 días + 1 día por año |
| **Prestaciones Sociales** | LOTTT Art. 142 | Acumulación 15 días por trimestre |

## Antes de empezar

- Tener **empleados activos** con su fecha de ingreso y sueldo diario cargados (de ahí salen los días que corresponden y el salario base).
- Tener a mano la **tasa BCV vigente** (la pide al calcular y al marcar pagado).

## Qué ves en la pantalla

```
┌──────────────────────────────────────────────────────────────────────────┐
│ Beneficios Anuales — LOTTT       [? Ayuda][Calcular beneficios]          │
│                                  [Marcar pagado][Eliminar][Reporte PDF]  │
├──────────────────────────────────────────────────────────────────────────┤
│ Anio: [2026 ▼]   Tipo: [Todos ▼]                                         │
├──────────────────────────────────────────────────────────────────────────┤
│ LOTTT Art. 131 — Utilidades: min. 15 dias de salario normal (diciembre)  │
│ LOTTT Art. 190 — Vacaciones: 15 dias + 1 dia/anio adicional              │
│ LOTTT Art. 192 — Bono vacacional 7+1 | Art. 142 Prestaciones 15/trim     │
├──────────────────────────────────────────────────────────────────────────┤
│ Empleado │ Tipo       │ Dias │ SalDia  │ MontoUSD │ TasaBCV │ MontoBs ...│
│ Pérez J  │ Utilidades │ 18   │ $20,00  │ $360     │ 36,50   │ Bs 13.140  │
│ Pérez J  │ Vacaciones │ 19   │ $20,00  │ $380     │ 36,50   │ Bs 13.870  │
├──────────────────────────────────────────────────────────────────────────┤
│ Registros: 48 | Total comprometido: $X | Pendiente: $Y | Pagados: 12     │
└──────────────────────────────────────────────────────────────────────────┘
```

### Botones

| Botón | Qué hace |
|---|---|
| **Calcular beneficios** | Calcula los 4 beneficios para todos los empleados activos del año seleccionado. |
| **Marcar pagado** | Marca como `Pagado` el registro seleccionado (pide la tasa BCV del día). |
| **Eliminar** | Borra el registro seleccionado. *Requiere `Admin`.* |
| **Reporte PDF** | Genera el PDF con todos los beneficios del año. |
| **? Ayuda** | Abre esta ficha del manual. |
| **Cerrar** | Sale de la pantalla. |

### Filtros

| Control | Qué hace |
|---|---|
| **Año** | Año al que aplica el cálculo (los últimos 5 años + actual). |
| **Tipo** | `Todos` · `Utilidades` · `Vacaciones` · `Bono Vacacional` · `Prestaciones Sociales`. |

### Tabla

| Columna | Significado |
|---|---|
| **Empleado** | Nombre completo |
| **Tipo** | El tipo de beneficio (color de fondo según tipo) |
| **Dias** | Días que corresponden por LOTTT |
| **SalDia** | Salario diario USD |
| **MontoUSD** | Monto en USD |
| **TasaBCV** | Tasa al momento del cálculo o pago |
| **MontoBs** | Equivalente en bolívares al momento del cálculo |
| **FechaPago** | Cuándo se pagó (si está pagado) |
| **Estado** | `Pendiente` · `Pagado` (gris) |
| **Obs** | Observaciones |

### Colores de fila

- **Amarillo claro** — Utilidades.
- **Celeste** — Vacaciones.
- **Verde claro** — Bono vacacional.
- **Lila claro** — Prestaciones sociales.
- Texto **gris** si está `Pagado`.

### Resumen al pie

```
Registros: N | Total comprometido: $X | Pendiente de pago: $Y | Pagados: M
```

## Casos de uso

### Caso 1 — Calcular los beneficios del año

**Cuándo**: una vez al año (típicamente al cierre o cuando corresponde el aniversario / pago de utilidades).

**Pasos**:

1. Seleccionar el **Año** del filtro.
2. Hacer clic en **Calcular beneficios**.
3. Si ya existen registros, el sistema avisa: `Ya existen N registros para AAAA. Deseas recalcular y agregar los faltantes?` — `Sí` agrega los empleados que aún no tienen cálculo (no rehace los que ya están).
4. Aparece el diálogo **Tasa BCV actual** — ingresar la tasa Bs/USD vigente.
5. **Aceptar**.

**Resultado**: aparece el aviso `Calculo completado. Empleados procesados: N. 4 beneficios por empleado (Utilidades, Vacaciones, Bono, Prestaciones)`.

### Caso 2 — Marcar un beneficio como pagado

1. Seleccionar la fila en la tabla.
2. Hacer clic en **Marcar pagado**.
3. Ingresar la **tasa BCV del día del pago** (no la del cálculo).
4. **Aceptar**.

**Resultado**: la fila pasa a `Pagado` (texto en gris). El monto en bolívares queda recalculado con la tasa del pago.

### Caso 3 — Generar el PDF anual

1. Elegir el **Año**.
2. Hacer clic en **Reporte PDF**.

Se guarda con el nombre `BeneficiosLOTTT_AAAA.pdf` y se abre.

### Caso 4 — Eliminar un registro (sólo Admin)

1. Seleccionar la fila.
2. **Eliminar**.
3. Confirmar el aviso `¿Eliminar este registro de beneficio?`.

Si el usuario no es Admin, aparece: `Solo el administrador puede eliminar registros de beneficios.`

## Validaciones y errores frecuentes

| Mensaje en pantalla | Qué significa | Cómo se resuelve |
|---|---|---|
| `No hay empleados activos.` | No se puede calcular sin empleados | Dar de alta empleados primero |
| `Ya existen N registros para AAAA. Deseas recalcular…?` | Ya hubo cálculo previo | `Sí` agrega faltantes; `No` cancela |
| `¿Eliminar este registro de beneficio?` | Confirmación al eliminar | Sí / No |
| `Solo el administrador puede eliminar registros de beneficios.` | Usuario sin permiso | Pedir al admin |
| `Error al eliminar: …` | Falla técnica | Reintentar |
| `No se pudieron cargar los beneficios.` | Falla al consultar la BD | Reintentar; si persiste, llamar al administrador |

## Consejos

- **Las utilidades se pagan típicamente en diciembre**, después del cálculo anual. Calculá temprano (noviembre) para tenerlas listas.
- **Vacaciones y bono vacacional** se calculan según el aniversario individual de cada empleado, pero el sistema corre el cálculo masivo del año.
- **La tasa BCV del cálculo y la del pago suelen ser distintas**. La columna `MontoBs` se actualiza al marcar pagado.
- **Recalcular** **NO duplica** registros: solo agrega los empleados que aún no tienen cálculo en ese año.
- Para corregir un cálculo erróneo: eliminar el registro (Admin) y volver a correr el cálculo.

## Pantallas relacionadas

- **`FrmEmpleados`** — fecha de ingreso y sueldo diario que alimentan el cálculo.
- **`FrmProcesarNomina`** — proceso semanal (no anual). Estos beneficios son aparte.
- **`FrmActualizarTasa`** — fuente recomendada de la tasa BCV (aunque acá se ingresa manual).
