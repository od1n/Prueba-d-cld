# `FrmControlCalidad` — Control de Calidad

> Registra y consulta las **inspecciones de calidad** de los lotes producidos. Muestra KPIs (% aprobado, defectos críticos, nivel general) y un gráfico de tendencia.

## Cómo llegar

- **Ruta**: *Menú principal → Archivos → Control Calidad*
- **Acceso alterno**: *Menú principal → Producción → Control Calidad*
- **Permiso requerido**: `Almacén` (o `Admin`)

## Para qué se usa

Llevar el **historial de inspecciones** sobre los lotes que salen de producción: cantidad inspeccionada, aprobada, rechazada, en retrabajo, defectos detectados, puntaje promedio. Si un lote es rechazado, el sistema marca también el **ingreso de producción asociado** como rechazado.

## Qué ves en la pantalla

- **Cabecera azul** con el título *"Control de Calidad — Inspecciones"* y botones **? Ayuda**, **+ Nueva insp.**, **Ver detalle**, **Eliminar**, **Reporte PDF**.
- **Franja de KPIs** (6 indicadores):
  - **Inspecciones**, **% Aprobado**, **% Rechazado**, **Defectos críticos**, **Defecto frecuente** (top), **Nivel calidad** (Excelente ≥95% / Bueno ≥85% / Aceptable ≥70% / Deficiente).
- **Tabla central** con todas las inspecciones, filas coloreadas:
  - **Verde claro** → Aprobado
  - **Rojo claro** → Rechazado
  - **Amarillo claro** → Retrabajo
- **Panel derecho**: gráfica de tendencia de los últimos lotes.
- **Pie**: resumen *"Inspecciones: N | Aprobadas: A | Rechazadas: R | Retrabajo: T"* y botón **Cerrar**.

### Columnas

| Columna | Significado |
|---|---|
| **Lote** | Identificador del lote inspeccionado |
| **Producto** | Código del producto |
| **Fecha** | Fecha de inspección |
| **Inspector** | Quién inspeccionó |
| **Total** | Cantidad inspeccionada |
| **Aprobadas** / **Rechazadas** / **Retrabajo** | Cantidades por categoría |
| **Pct_Ap** | % aprobado |
| **Puntaje** | Puntaje promedio sobre 5 |
| **Resultado** | Aprobado · Rechazado · Retrabajo |

> Doble clic en una fila → ver detalle.

## Casos de uso

### Caso 1 — Registrar una nueva inspección

1. **+ Nueva insp.**
2. Aparece primero el diálogo **Seleccionar Ingreso de Producción**: elegir el ingreso al que pertenece la inspección.
3. Se abre el diálogo **Inspección**: llenar Lote, Inspector, Fecha, Cantidad inspeccionada, Aprobada, Rechazada, Retrabajo, Puntaje, Resultado, Defectos detectados.
4. **Guardar**.

**Resultado**: queda registrada y suma a las estadísticas. Si el resultado es **Rechazado**, el ingreso de producción asociado pasa también a estado *Rechazado* en el módulo de producción.

### Caso 2 — Ver el detalle de una inspección

1. Doble clic en la fila (o **Ver detalle**).
2. Se abre el diálogo **en modo solo lectura** mostrando todos los datos y los defectos asociados.

### Caso 3 — Eliminar una inspección

1. Seleccionar.
2. **Eliminar**.
3. Confirmar `Eliminar inspeccion del lote XXX?`.

### Caso 4 — Generar reporte PDF

1. Hacer clic en **Reporte PDF**.
2. El sistema genera el PDF con todas las inspecciones y lo guarda con nombre `Calidad_AAAA-MM-DD.pdf`.

> Si no hay inspecciones, aparece `No hay inspecciones para exportar.`

## Validaciones y errores frecuentes

| Mensaje | Qué significa | Cómo se resuelve |
|---|---|---|
| `No hay inspecciones para exportar.` | Lista vacía | Registrar al menos una primero |
| `Error al generar PDF: …` | Falla técnica | Reintentar; verificar permisos del directorio destino |

## Consejos

- El KPI **Nivel calidad** es una mirada rápida del estado general. Si baja a *Aceptable* o *Deficiente*, conviene revisar moldes y procesos.
- El KPI **Defecto frecuente** se trunca a 10 caracteres en el panel — para ver el nombre completo, abrir el detalle de inspecciones recientes.
- Una inspección **Rechazada** afecta el estado del ingreso de producción asociado — coordinarse con producción al rechazar lotes.

## Pantallas relacionadas

- **`FrmRegistroProduccionDia`** / **`FrmIngresoProduccion`** — registran los lotes que se inspeccionan.
- **`FrmMermas`** — los rechazos suelen alimentar mermas para reproceso.
