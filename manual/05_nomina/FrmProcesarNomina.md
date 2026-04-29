# `FrmProcesarNomina` — Procesar Nómina Semanal

> Centro del proceso de nómina: crea el período de la semana, calcula sueldo devengado y deducciones por empleado, calcula aportes patronales (IVSS, RPVH, INCES, cesta) y genera recibos, planilla IVSS y resumen de aportes en PDF.

## Cómo llegar

- **Ruta**: *Menú principal → Nómina → Procesar Nómina*.
- **Permiso requerido**: `Nomina` (o `Admin`).

## Para qué se usa

Cubre todo el ciclo semanal:

- **Crear** el período (fechas desde/hasta + tasa BCV).
- **Editar** la línea de cada empleado (días trabajados, faltas, piezas producidas, ventas, otros ingresos, deducciones).
- **Calcular aportes patronales** (IVSS, RPVH/FAOV, INCES, cesta ticket).
- **Cerrar** el período (queda definitivo, no se puede modificar).
- Generar **recibo de pago PDF**, **planilla IVSS** y **resumen de aportes**.

## Antes de empezar

- Tener empleados activos cargados en *Nómina → Empleados*.
- Tener la **tasa BCV** del día del cierre (la pide al crear el período).
- Si hay **préstamos activos**, las cuotas se descuentan automáticamente al precargar el período.

## Qué ves en la pantalla

```
┌──────────────────────────────────────────────────────────────────────────┐
│ Nomina Semanal — USD     [+Nuevo periodo][Cerrar periodo]                │
│                          [Recibo PDF][Plan. IVSS][Aportes PDF][? Ayuda]  │
├──────────────────────────────────────────────────────────────────────────┤
│ Periodo: [Semana 28/04 – 04/05  [Abierto] ▼]   Tasa BCV: Bs 36,50/USD    │
├──────────────────────────────────────────────────────────────────────────┤
│ ┌─ Nomina detallada ─┬─ Aportes Patronales ────────────────────────────┐ │
│ │                                                                       │ │
│ │ Id │ Empleado │ Tipo │ Dias │ Faltas │ Piezas │ Ventas │ Dev.Sue │... │ │
│ │ 12 │ Pérez J  │ Prod │  5   │   0    │  500   │   —    │ $50,00  │... │ │
│ │ 14 │ Marin M  │ Ofi  │  5   │   0    │   —    │   3    │ $75,00  │... │ │
│ │                                                                       │ │
│ │ TOTAL DEV $1.250  |  TOTAL DED $185  |  NETO A PAGAR $1.065 = Bs 38k  │ │
│ └───────────────────────────────────────────────────────────────────────┘│
└──────────────────────────────────────────────────────────────────────────┘
```

### Botones (cabecera)

| Botón | Qué hace |
|---|---|
| **+ Nuevo periodo** | Crea un período nuevo. |
| **Cerrar periodo** | Cierra el período actual. **No se puede revertir.** |
| **Recibo PDF** | Genera el PDF con la planilla / recibos del período. |
| **Plan. IVSS** | Genera la planilla IVSS del período. |
| **Aportes PDF** | Genera el resumen PDF de aportes patronales (requiere haberlos calculado primero). |
| **? Ayuda** | Abre esta ficha del manual. |

### Selector de período

Lista todos los períodos cargados. Al lado muestra `Tasa BCV: Bs X,XX/USD | Estado: Abierto/Cerrado`.

### Pestaña 1 — Nómina detallada

Una fila por empleado. Doble clic abre el detalle editable.

| Columna | Significado |
|---|---|
| **Empleado** | Nombre completo |
| **Tipo** | `Produccion` (verde) · `Oficina` (azul) · `Fijo` (blanco) |
| **Dias trab** | Días trabajados (editable solo si período `Abierto`) |
| **Faltas** | Días de inasistencia |
| **Piezas** | Piezas producidas (sólo Producción) |
| **Ventas** | Ventas cerradas (sólo Oficina) |
| **Dev.Sueldo** | Sueldo devengado por días trabajados |
| **Dev.Prod** | Devengado por piezas + ventas |
| **TOTAL DEV** | Total devengado |
| **IVSS 4%** | Deducción legal trabajador |
| **RPVH 1%** | Deducción legal trabajador (FAOV) |
| **Desc.Falta** | Descuento por faltas |
| **Prestamo** | Cuota semanal del préstamo activo |
| **TOTAL DED** | Total deducciones |
| **NETO USD** | Neto en dólares (resaltado en negrita) |
| **NETO Bs** | Neto en bolívares (al cambio del período) |

Al pie:

```
Empleados: 8 | Total devengado: $X | Total deducciones: $Y | NETO A PAGAR: $Z = Bs A
```

### Pestaña 2 — Aportes Patronales

Sub-toolbar con tres parámetros y un botón:

| Control | Detalle |
|---|---|
| **IVSS patron %** | Entre 9 y 11. Default 9,0%. Es lo que paga el patrono al IVSS. |
| **Valor UT (Bs)** | Valor actual de la Unidad Tributaria, usado para la cesta ticket. |
| **Calcular aportes** | Calcula y guarda los aportes para el período. |

Aviso informativo: `IVSS patron 9-11% | RPVH/FAOV 2% | INCES 2% | CESTA 0.5 UT x dia laboral`

Tabla con una fila por empleado:

| Columna | Significado |
|---|---|
| **Empleado** | Nombre |
| **Salario Bs** | Salario normal mensual en bolívares |
| **% IVSS** | Porcentaje aplicado |
| **IVSS Patron Bs** | Aporte IVSS del patrono |
| **RPVH/FAOV Bs** | Aporte 2% RPVH/FAOV |
| **INCES Bs** | Aporte 2% INCES |
| **Dias lab.** | Días laborales del período |
| **Valor UT Bs** | UT cargada |
| **CESTA Bs** | Cesta ticket = 0,5 UT × días laborales |
| **TOTAL Bs** | Suma de aportes en bolívares (negrita) |
| **TOTAL USD** | Mismo total en dólares (negrita) |

Resumen al pie:

```
Empleados: N | IVSS patron: Bs X | RPVH: Bs Y | INCES: Bs Z | CESTA: Bs A |
TOTAL APORTES: Bs T | Costo total nomina: Bs C
```

### Diálogo: Nuevo período

| Campo | Detalle |
|---|---|
| **Desde / Hasta** | Por defecto la semana actual (lunes a domingo). |
| **Tasa BCV (Bs/USD)** | Mínimo 1. **Obligatoria.** |
| **Observaciones** | Texto libre. |

### Diálogo: Editar línea de nómina (doble clic en una fila)

Solo se puede editar si el período está **Abierto**.

| Campo | Detalle |
|---|---|
| **Días trabajados** | 0–7 |
| **Días inasistencia** | 0–7 |
| **Piezas producidas** | sólo si tipo Producción |
| **Ventas cerradas** | sólo si tipo Oficina |
| **Otros ingresos ($)** | Bonificaciones extras |
| **IVSS 4% / RPVH 1%** | Solo lectura, calculado |
| **Cuota préstamo ($)** | Editable |
| **Otras deducciones ($)** | Editable |
| **NETO A PAGAR** | Solo lectura, recalculado al instante en USD y Bs |

## Casos de uso

### Caso 1 — Crear el período de la semana y calcular nómina

**Cuándo**: cada lunes (o cuando corresponda al cierre del período).

**Pasos**:

1. **+ Nuevo periodo**.
2. Verificar las fechas (lunes a domingo de la semana ya transcurrida).
3. Ingresar la **Tasa BCV** del día.
4. **Crear periodo**.
5. El sistema **precarga** automáticamente todos los empleados activos con días estándar (5 días) y las cuotas de préstamos activos.
6. Doble clic en cada empleado para ajustar **piezas / ventas / faltas / otros ingresos**.
7. Guardar el detalle. Repetir por cada empleado que tenga cambios.

### Caso 2 — Calcular aportes patronales

1. Asegurarse de tener los empleados con su **Salario normal mensual** en *Nómina → Empleados*.
2. Ir a la pestaña **Aportes Patronales**.
3. Ajustar el **% IVSS Patrón** (default 9,0%).
4. Cargar el **Valor UT (Bs)** vigente.
5. Hacer clic en **Calcular aportes**.

**Resultado**: la tabla se llena con los aportes por empleado y el resumen al pie.

### Caso 3 — Cerrar el período

**Cuándo**: cuando ya está todo revisado y no hay que tocar más.

**Pasos**:

1. Hacer clic en **Cerrar periodo**.
2. Leer el aviso: `Cerrar el periodo <descripcion>? Una vez cerrado no podras modificar los datos de nomina.`
3. Confirmar `Sí`.

**Resultado**: el período pasa a `Cerrado` y se bloquea la edición.

### Caso 4 — Generar PDFs

- **Recibo PDF** — planilla con un recibo por empleado del período.
- **Plan. IVSS** — formato compatible con la planilla del IVSS.
- **Aportes PDF** — resumen de aportes patronales (requiere haber hecho **Calcular aportes** antes).

Cada uno se guarda con el nombre `<Tipo>_AAAA-MM-DD.pdf` y se abre automáticamente.

## Validaciones y errores frecuentes

| Mensaje en pantalla | Qué significa | Cómo se resuelve |
|---|---|---|
| `Ingresa la tasa BCV.` | Falta la tasa al crear el período | Cargar la tasa |
| `La fecha 'Hasta' no puede ser anterior a 'Desde'.` | Fechas invertidas | Corregir |
| `Las fechas se solapan con el periodo existente: <descripcion>` | El nuevo período pisa fechas de uno ya creado | Ajustar fechas para que no haya superposición |
| `Cerrar el periodo <descripcion>?` | Confirmación al cerrar | Sí / No (no reversible) |
| `No hay aportes calculados para este periodo.` | Se intentó exportar PDF sin calcular | Ir a la pestaña *Aportes Patronales* y presionar **Calcular aportes** |
| `Error al generar PDF: …` | Falla técnica | Reintentar; si persiste, llamar al administrador |

## Consejos

- **Cargá los empleados con su Salario normal mensual USD** antes de calcular aportes — sin ese dato el aporte sale en cero.
- **Las cuotas de préstamo se abonan automáticamente** al guardar la línea de nómina del empleado: el saldo del préstamo baja en *Nómina → Préstamos*.
- **No cierres el período hasta confirmar** que todo está correcto. Una vez cerrado, hay que crear ajustes manuales si hubo error.
- **Los pagos en bolívares** usan la **tasa cargada en el período**, no la del día. Esto fija el equivalente al momento del cierre.
- Para el **IVSS** el porcentaje patrón es 9–11% según el riesgo de la empresa (consultalo con el contador).

## Pantallas relacionadas

- **`FrmEmpleados`** — origen de la lista de empleados activos.
- **`FrmPrestamos`** — los préstamos activos generan cuotas que se descuentan aquí.
- **`FrmBeneficiosLOTTT`** — utilidades, vacaciones y prestaciones (proceso anual, no semanal).
- **`FrmTarifasBonoProduccion`** — parámetros que pueden afectar el bono de producción.
