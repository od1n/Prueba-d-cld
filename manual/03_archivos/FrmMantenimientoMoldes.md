# `FrmMantenimientoMoldes` — Programa de Mantenimiento de Moldes

> Programa los **mantenimientos preventivos** de cada molde, registra los realizados y consulta el historial. Muestra alertas visuales para los vencidos.

## Cómo llegar

- **Ruta**: *Menú principal → Archivos → Mant. Moldes*
- **Acceso alterno**: desde `FrmMoldes` → botón **Mantenimiento**.
- **Permiso requerido**: `Almacén` (o `Admin`)

## Para qué se usa

- Llevar un **programa preventivo**: para cada molde, fecha del último mantenimiento, frecuencia (días) y próximo vencimiento.
- **Registrar** cada mantenimiento que se hace (fecha, técnico, costo, descripción, resultado).
- **Consultar el historial** de todos los mantenimientos pasados.
- Exportar todo a PDF.

## Qué ves en la pantalla

- **Cabecera azul oscuro** con el título *"Mantenimiento de Moldes"* y botones **Exportar PDF** y **Cerrar**.
- **Franja de KPIs** (6 indicadores):
  - **VENCIDOS** (rojo)
  - **URGENTES <7d** (naranja)
  - **PROXIMOS <30d** (amarillo)
  - **AL DIA** (verde)
  - **Costo histórico** (azul)
  - **Moldes en programa** (azul claro)
- **Pestañas**:
  - **Programa de Mantenimiento**: tabla de moldes programados + mini-calendario de los próximos 90 días.
  - **Historial de Mantenimientos**: tabla con todos los mantenimientos realizados.

### Tab Programa — toolbar

| Botón | Acción |
|---|---|
| **+ Agregar molde al programa** | Inscribe un molde con su frecuencia |
| **Editar frecuencia** | Cambia la frecuencia del molde seleccionado |
| **Quitar del programa** | Saca el molde del programa preventivo |
| **Registrar mantenimiento** | Carga un mantenimiento realizado |

### Columnas — tab Programa

| Columna | Significado |
|---|---|
| **CodigoMolde** | Código del molde |
| **Molde** | Descripción |
| **TipoMant** | Tipo (preventivo, correctivo, etc.) |
| **Frecuencia** | Días entre mantenimientos |
| **UltMant** | Último realizado |
| **ProxMant** | Próximo según la frecuencia |
| **DiasRest** | Días restantes (negativo si vencido) |
| **Estado** | `VENCIDO` · `URGENTE` · `PROXIMO` · `OK` |
| **Activo** | Si está incluido en el programa |

Filas coloreadas: rojo claro vencido, amarillo urgente, crema próximo, verde claro al día.

### Columnas — tab Historial

| Columna | Significado |
|---|---|
| **CodigoMolde** | Molde |
| **Fecha** | Fecha realizada |
| **TipoMant** | Tipo |
| **Tecnico** | Quién lo hizo |
| **Descripcion** | Detalle |
| **Costo** | Costo (USD) |
| **Horas** | Duración en horas |
| **Resultado** | `Completado`, `Cancelado`, etc. |
| **ProxMant** | Próximo programado |

Filas: verde claro Completado, rojo claro Cancelado.

## Casos de uso

### Caso 1 — Inscribir un molde en el programa

1. Tab **Programa** → **+ Agregar molde al programa**.
2. Elegir el molde, tipo de mantenimiento y **frecuencia (días)**.
3. **Guardar**.

### Caso 2 — Registrar un mantenimiento realizado

1. Seleccionar el molde en la tabla.
2. **Registrar mantenimiento**.
3. Llenar: Fecha realizada, Técnico, Descripción, Costo, Duración (horas), Resultado, Observaciones.
4. **Guardar**.

**Resultado**: queda en el historial. La columna **UltMant** del programa se actualiza, y el **ProxMant** se recalcula sumando la frecuencia.

### Caso 3 — Cambiar la frecuencia de un molde

1. Seleccionar el molde.
2. **Editar frecuencia**.
3. Modificar.
4. **Guardar**.

### Caso 4 — Quitar un molde del programa

1. Seleccionar.
2. **Quitar del programa**.
3. Confirmar `Quitar XXX del programa de mantenimiento?`.

### Caso 5 — Filtrar el historial por molde

1. Tab **Historial**.
2. En el desplegable **Filtrar molde** elegir uno o `Todos`.

### Caso 6 — Eliminar un registro del historial

1. Tab **Historial**, seleccionar la fila.
2. **Eliminar registro**.
3. Confirmar `Eliminar este registro de mantenimiento?`.

### Caso 7 — Exportar a PDF

Hacer clic en **Exportar PDF** (cabecera). El sistema genera y guarda el archivo.

## Consejos

- Revisar los **KPIs** al inicio de la semana: si **VENCIDOS** está en rojo, hay urgencia.
- El **mini-calendario** del lateral derecho del tab Programa muestra los próximos 12 mantenimientos en orden cronológico.
- El **costo histórico** del KPI permite ver cuánto se gasta en mantenimiento sumando todos los registros.

## Pantallas relacionadas

- **`FrmMoldes`** — catálogo de moldes; las filas rojas allí indican mantenimiento vencido y el botón **Mantenimiento** lleva acá.
