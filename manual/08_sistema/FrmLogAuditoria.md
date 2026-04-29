# `FrmLogAuditoria` — Visor de log de auditoría

> Pantalla de auditoría completa. Lista todas las acciones registradas por el sistema (logins, creaciones, anulaciones, errores) con filtros por fecha, usuario, acción y texto libre. Incluye gráficas de actividad por hora y top de acciones, y permite exportar a CSV.

## Cómo llegar

- **Ruta**: *Menú principal → ⚙ Sistema → Ver log de auditoria*.
- **Atajo**: ninguno.
- **Permiso requerido**: 🔒 **Admin**.

## Para qué se usa

Es la **bitácora del sistema**. Sirve para:

- Investigar qué pasó en una fecha (¿quién anuló esa factura? ¿a qué hora?).
- Detectar intentos de login fallidos (posibles intrusiones).
- Auditar cambios sensibles (tasas, precios, usuarios).
- Detectar errores recurrentes para mejorar la operación.

Cumple además con el requisito legal de trazabilidad fiscal.

## Antes de empezar

- Tener rol Admin.
- Saber qué buscás (rango de fechas, usuario sospechoso, tipo de acción).

## Qué ves en la pantalla

```
┌──────────────────────────────────────────────────────────────────────────┐
│ Log de Auditoria del Sistema              [ Exportar CSV ] [ Cerrar ]    │  ← header
├──────────────────────────────────────────────────────────────────────────┤
│ Desde: [01/04/26]  Hasta: [28/04/26]  Usuario: [Todos ▼]                 │
│ Accion: [Todas ▼]  Buscar: [_________]  [ Filtrar ]                      │
├──────────────────────────────────────────────────────────────────────────┤
│ ┌─────────────┐ ┌─────────────┐ ┌─────────────┐ ┌─────────────┐          │
│ │ Registros   │ │     Hoy     │ │  Errores    │ │ Usuario más │          │
│ │   1.247     │ │     38      │ │      2      │ │  juan       │          │
│ └─────────────┘ └─────────────┘ └─────────────┘ └─────────────┘          │  ← KPIs
├──────────────────────────────────────────────────────┬───────────────────┤
│ FechaHora           Usuario  Accion        Tabla     │ Actividad por hora│
│ 28/04 14:10:23      admin    BACKUP_MANUAL Sistema   │  ▁▂▆▇█▆▅▃▂▁       │
│ 28/04 13:45:00      juan     LOGIN         Usuarios  │                   │
│ 28/04 13:44:55      juan     LOGIN_FALLIDO Usuarios  │ Top acciones      │
│ ...                                                  │  LOGIN     ████12 │
│                                                      │  NUEVA_FACT ███ 8 │
├──────────────────────────────────────────────────────┴───────────────────┤
│   Mostrando 1247 registros  |  Solo Admin puede ver este log             │
└──────────────────────────────────────────────────────────────────────────┘
```

### Filtros

| Filtro | Para qué sirve |
|---|---|
| **Desde / Hasta** | Rango de fechas (default: últimos 30 días). |
| **Usuario** | `Todos` o un usuario específico. |
| **Acción** | `Todas` o una acción concreta (LOGIN, NUEVA_FACTURA, etc.). |
| **Buscar** | Texto libre — busca en usuario, acción, tabla y detalle. |
| **Filtrar** | Aplica los filtros y refresca la tabla. |

### KPIs (4 tarjetas)

| KPI | Significado |
|---|---|
| **Registros** | Total de eventos en el filtro actual. |
| **Hoy** | Cuántos son de hoy. |
| **Errores** | Cuántos empiezan con `ERROR`. |
| **Usuario más activo** | El que más eventos generó en el filtro. |

### Tabla central

| Columna | Significado |
|---|---|
| **Id** | Identificador interno. |
| **FechaHora** | Día y hora exactos. |
| **Usuario** | Quién generó la acción. |
| **Accion** | Tipo de evento (LOGIN, NUEVA_FACTURA, BACKUP_MANUAL, etc.). |
| **Tabla** | Módulo afectado. |
| **Detalle** | Información específica (rol, monto, motivo, etc.). |

**Color de fila:**

- 🔴 Rojo claro: errores (la acción empieza con `ERROR`).
- 🔵 Azul claro: eventos de sesión (LOGIN, LOGOUT).
- 🟢 Verde claro: creaciones (NUEVA, INSERTAR, BACKUP).
- 🟠 Naranja claro: cancelaciones / eliminaciones (CANCEL, ELIMINAR).

### Panel derecho — Gráficas

- **Actividad por hora** — barras 0-23h con la cantidad de eventos por hora del día. Las horas laborales (8-18) se pintan más oscuras.
- **Top acciones** — barras horizontales con las 8 acciones más frecuentes en el filtro.

### Acciones registradas más comunes

| Código | Significado |
|---|---|
| `LOGIN` / `LOGOUT` | Entrada / salida del sistema. |
| `LOGIN_FALLIDO` | Intento de login con credenciales incorrectas. |
| `NUEVA_FACTURA`, `FACTURA_ANULADA` | Movimientos de facturación. |
| `NUEVO_CLIENTE`, `EDITAR_CLIENTE` | Catálogo de clientes. |
| `KARDEX_AJUSTE` | Ajuste de inventario. |
| `BACKUP_MANUAL`, `BACKUP_AUTO` | Respaldos. |
| `BUSQUEDA` | Uso del buscador global. |
| `TASA_GUARDADA`, `TASA_EDITADA` | Cambios de tasa de cambio. |
| `NE_VISTA_ACTIVADA` / `NE_VISTA_DESACTIVADA` | Toggle de visibilidad de notas de entrega. |
| `AUDITORIA_EXPORTADA` | Cuando alguien exporta este mismo log a CSV. |
| `ERROR_*` | Errores capturados (reemplazo de `*` por el contexto). |

## Casos de uso

### Caso 1 — ¿Quién anuló esta factura?

**Pasos**:

1. Filtro **Acción**: `FACTURA_ANULADA`.
2. **Buscar**: el número de la factura (ej. `00125`).
3. **Filtrar**.

**Resultado**: aparece la fila con usuario, hora y motivo.

### Caso 2 — Detectar intentos de intrusión

**Pasos**:

1. **Acción**: `LOGIN_FALLIDO`.
2. **Desde / Hasta**: el rango sospechoso.
3. **Filtrar**.

**Resultado**: si aparecen muchos intentos del mismo origen anonimizado (ej. `j***`), considerar resetear esa contraseña y revisar.

### Caso 3 — Exportar a Excel para análisis externo

**Pasos**:

1. Aplicar los filtros que querés.
2. Hacer clic en **Exportar CSV** (cabecera).
3. Elegir nombre y carpeta — por defecto `AuditoriaLog_AAAA-MM-DD.csv`.
4. **Guardar**.

**Resultado**: el archivo se abre automáticamente con el visor predeterminado (Excel / Bloc de notas). La acción queda registrada como `AUDITORIA_EXPORTADA`.

### Caso 4 — Investigar un error

**Pasos**:

1. **Acción**: elegir el `ERROR_*` correspondiente, o
2. **Buscar**: `ERROR` (texto libre encuentra en cualquier campo).

**Resultado**: las filas en rojo permiten ver el detalle exacto del error.

## Validaciones y errores frecuentes

| Mensaje en pantalla | Qué significa | Cómo se resuelve |
|---|---|---|
| `No hay datos para exportar.` | Filtro vacío al pulsar Exportar CSV | Ampliar el filtro y reintentar |
| `Error al exportar: …` | Falla al escribir el archivo (permisos, disco lleno) | Elegir otra carpeta; verificar espacio |
| `Mostrando 0 registros` | Los filtros no tienen coincidencias | Ampliar rango de fechas o limpiar el campo Buscar |

## Consejos

- **Hacé Filtrar después de cambiar las fechas** — el sistema sólo recarga al pulsar el botón o al cambiar usuario/acción.
- **El campo Buscar es muy potente** — busca en todos los campos. Útil para encontrar referencias específicas (números de factura, RIFs, etc.).
- Las **gráficas** son útiles para detectar patrones: si ves actividad a las 3 AM, probablemente sea un acceso no autorizado.
- **Exportá periódicamente** (mensual) y guardá los CSV en un disco externo — el log puede crecer mucho y eventualmente es purgado.
- Esta pantalla **no permite editar ni borrar registros** — es de sólo lectura, como debe ser una auditoría confiable.

## Pantallas relacionadas

- **`FrmSistema`** — desde donde se abre.
- **`FrmUsuarios`** — para resetear contraseñas tras detectar accesos sospechosos.
- **`FrmTrazabilidadTasa`** — visión más enfocada para los cambios de tasa.
