# `FrmActualizarTasa` — Actualizar tasa de cambio (carga automática)

> Diálogo modal que consulta automáticamente la tasa BCV y paralelo del día desde dolarapi.com / bcv.org.ve. Permite revisar, editar manualmente y guardar. Se abre solo al primer login del día si la tasa no está cargada.

## Cómo llegar

- **Ruta automática**: aparece al primer login del día si no hay tasa de hoy registrada y no fue *pospuesta*.
- **Ruta manual**: *Menú principal → franja superior → ↻ Actualizar tasa* (botón al lado del BCV/PAR).
- **Atajo**: ninguno.
- **Permiso requerido**: `TasaIngreso` (Ventas, Contabilidad y Admin lo tienen). Si la tasa de hoy ya existe y se quiere sobrescribir, requiere `TasaModificar`.

## Para qué se usa

Es la forma **rápida y recomendada** de cargar la tasa del día:

1. El sistema consulta dolarapi.com (BCV oficial + paralelo) en cuanto se abre el diálogo.
2. Si responde, **rellena los valores automáticamente**.
3. El usuario revisa, ajusta si quiere, y confirma con **Aceptar y guardar**.
4. Si no hay internet o la API falla, podés **ingresar las tasas manualmente** sin cerrar el diálogo.

## Antes de empezar

- Conexión a internet (para la consulta automática).
- Si no hay internet, conocer la tasa de la fuente alternativa que use el negocio.

## Qué ves en la pantalla

```
┌────────────────────────────────────────────────────────────┐
│ Tasa de cambio diaria                                      │  ← banner azul
├────────────────────────────────────────────────────────────┤
│ Se consultó la tasa oficial del BCV y la del mercado       │
│ paralelo. Revisa los valores y confirma.                   │
│                                                            │
│ Tasas obtenidas. Revisa y confirma. (08:14:23)             │  ← estado
│                                                            │
│ ┌─────────────────────┐  ┌─────────────────────┐           │
│ │ BCV OFICIAL         │  │ PARALELO            │           │
│ │ [   36,5000   ]     │  │ [   41,2000   ]     │           │
│ │ Bs/USD              │  │ Bs/USD              │           │
│ │ Anterior: 36,4000   │  │ Anterior: 41,0000   │           │
│ │ (27/04)             │  │ (27/04)             │           │
│ │ Fuente: dolarapi.com│  │ Fuente: dolarapi.com│           │
│ └─────────────────────┘  └─────────────────────┘           │
│                                                            │
│ [ ↻ Reintentar consulta ]                                  │
│                                                            │
│ [ Aceptar y guardar ] [ Posponer hasta mañana ] [Cancelar] │
└────────────────────────────────────────────────────────────┘
```

### Tarjetas de tasa

| Tarjeta | Qué muestra |
|---|---|
| **BCV OFICIAL** | Valor traído de la API (o 0 si falló). Editable. Muestra la tasa anterior con fecha. |
| **PARALELO** | Igual, para el mercado paralelo. Editable. |

Bajo cada tarjeta se ve la **Fuente** (`dolarapi.com`, `bcv.org.ve`, etc.) o el aviso `No se pudo obtener BCV` si la consulta falló.

### Estado (línea de mensaje)

| Mensaje | Significado |
|---|---|
| `Consultando dolarapi.com…` | Buscando los valores |
| `Tasas obtenidas. Revisa y confirma. (HH:mm:ss)` | Éxito |
| `(error) — puedes ingresar las tasas manualmente.` | Falla de red u origen; el diálogo sigue abierto y editable |

### Botones

| Botón | Qué hace |
|---|---|
| **↻ Reintentar consulta** | Vuelve a llamar a la API (útil tras restaurar internet). |
| **Aceptar y guardar** | Guarda la tasa para hoy y cierra. |
| **Posponer hasta mañana** | Marca la tasa como pospuesta — no volverá a preguntar hoy. |
| **Cancelar** | Cierra sin guardar. |

### Origen del registro (auditoría)

El sistema clasifica internamente cómo se cargó la tasa. Aparece en `FrmTrazabilidadTasa`:

| Origen | Cuándo se aplica |
|---|---|
| **AUTO_API** | La API respondió y guardaste sin tocar los valores. |
| **EDITADO** | La API respondió pero modificaste BCV o PAR antes de guardar. |
| **MANUAL** | Sin respuesta de API; ingresaste todo a mano. |

## Casos de uso

### Caso 1 — Aceptación rápida (lo más común)

**Cuándo**: tenés internet, la API responde y los valores parecen correctos.

**Pasos**:

1. Esperar a que aparezca `Tasas obtenidas. Revisa y confirma.` (1-3 segundos).
2. Verificar que las tarjetas tienen los valores razonables.
3. Hacer clic en **Aceptar y guardar**.

**Resultado**: se registra la tasa con origen `AUTO_API`. El menú principal muestra la tasa actualizada con sello `● actualizada hoy` (verde).

### Caso 2 — Editar antes de aceptar

**Cuándo**: el valor de la API no coincide con la fuente que el negocio prefiere.

**Pasos**:

1. Esperar la consulta automática.
2. Modificar el valor de **BCV** o **Paralelo** con las flechas o escribiendo.
3. **Aceptar y guardar**.

**Resultado**: la tasa se guarda con origen `EDITADO`.

### Caso 3 — Sin internet, carga manual

**Pasos**:

1. Esperar — aparece el aviso `(error) — puedes ingresar las tasas manualmente.`
2. Escribir los valores BCV y Paralelo a mano.
3. **Aceptar y guardar**.

**Resultado**: tasa registrada con origen `MANUAL`.

### Caso 4 — Posponer hasta mañana

**Cuándo**: no podés cargar la tasa ahora pero querés seguir trabajando (no recomendado: los reportes en USD quedan inexactos).

**Pasos**:

1. Hacer clic en **Posponer hasta mañana**.

**Resultado**: el sistema marca la fecha y **no vuelve a preguntar hoy**. El menú principal mostrará el sello `sin tasa registrada` (rojo). Mañana volverá a aparecer el diálogo en el primer login.

## Validaciones y errores frecuentes

| Mensaje en pantalla | Qué significa | Cómo se resuelve |
|---|---|---|
| `La tasa BCV es obligatoria y debe ser mayor que cero.` | Dejaste BCV en 0 | Ingresar el valor real |
| `No se pudo obtener BCV` (en la tarjeta) | La API no devolvió ese valor | Ingresar a mano o **↻ Reintentar consulta** |
| `Permiso denegado` | Tu rol no puede guardar tasas (raro) | Pedirle al admin que te asigne `TasaIngreso` |
| `No se pudo guardar la tasa.` (con detalle) | Error de BD | Reintentar; si persiste, llamar al administrador |
| `(red) — puedes ingresar las tasas manualmente.` | Sin internet o API caída | Cargar manual o reintentar más tarde |

## Consejos

- **Confiá en la API**: dolarapi.com es estable y entrega el BCV oficial + paralelo. Solo editá si tenés un motivo claro.
- **Posponer es la última opción**: los reportes y el dashboard quedan con datos inexactos.
- Si la tasa cambió a media jornada (caso raro), el Admin/Contabilidad puede **sobrescribir** desde `FrmTasaCambio` con `TasaModificar`. Las facturas ya emitidas no se recalculan.
- El **botón ↻ del menú principal** abre este mismo diálogo en cualquier momento.

## Pantallas relacionadas

- **`FrmTasaCambio`** — registro manual y consulta histórica (sin consulta API).
- **`FrmTrazabilidadTasa`** — auditoría: ver el origen (AUTO_API / EDITADO / MANUAL) de cada tasa.
- **`MenuPrincipalV2`** — desde la franja superior, botón **↻ Tasa**.
- **`FrmFacturas`** — toma esta tasa para convertir Bs↔USD.
