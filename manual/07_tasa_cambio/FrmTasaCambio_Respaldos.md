# `FrmTasaCambio_Respaldos` — Tasa de cambio (versión alterna)

> Variante alterna del módulo de tasa de cambio. Funcionalidad equivalente a `FrmTasaCambio`. Esta versión vive en el archivo `FrmTasaCambio_Respaldos.cs` y comparte el mismo nombre de clase, por lo que el menú principal puede invocar a una u otra según la configuración del proyecto.

## Cómo llegar

- **Ruta**: *Menú principal → $ Tasa de cambio* (cuando el proyecto compila con esta variante activa).
- **Atajo**: ninguno.
- **Permiso requerido**: `TasaConsulta` para ver, `TasaIngreso` o `TasaModificar` para guardar.

## Diferencias con `FrmTasaCambio`

Esta variante es funcionalmente la misma pantalla — registra la tasa BCV/Paralelo del día, mantiene el histórico de 60 días, permite editar con doble clic y abre la **Trazabilidad** desde el footer. La única diferencia práctica es que se distribuye como respaldo del archivo principal y puede tener pequeños ajustes visuales.

> Para el uso operativo, **referite a la ficha de `FrmTasaCambio`** — todos los pasos y validaciones son idénticos.

## Para qué se usa

Idéntico a `FrmTasaCambio`:

- **Cargar manualmente** la tasa BCV y paralelo de un día.
- **Consultar** el histórico (últimos 60 días).
- **Editar** una tasa con doble clic en la fila.
- Abrir **▤ Trazabilidad** para ver qué facturas usaron qué tasa.

## Antes de empezar

- Conocer la tasa BCV oficial y paralelo del día.
- Tener al menos el rol `TasaIngreso` para guardar (Ventas, Contabilidad y Admin lo tienen).

## Qué ves en la pantalla

Layout idéntico a `FrmTasaCambio`:

- **Header verde** con el título "Tasa de Cambio — BCV y Paralelo".
- **Panel de entrada** (Fecha · Tasa BCV · Tasa Paralelo · botón **Guardar tasa**).
- **Banner verde** con la "Tasa vigente" del día más reciente.
- **Grilla** con las tasas históricas.
- **Footer** con **▤ Trazabilidad** y **Cerrar**.

## Casos de uso

Ver `FrmTasaCambio.md`. Mismos casos:

1. Cargar tasa de hoy.
2. Cargar tasa de un día atrasado.
3. Editar una tasa registrada (doble clic).
4. Abrir Trazabilidad.

## Validaciones y errores frecuentes

| Mensaje en pantalla | Qué significa | Cómo se resuelve |
|---|---|---|
| `La tasa BCV debe ser mayor que cero.` | Dejaste BCV en 0 | Ingresar el valor real |
| Error al guardar | Permiso insuficiente o BD | Si es permiso, pedir `TasaIngreso` o `TasaModificar` al admin |

## Consejos

- Si el menú abre esta variante (`FrmTasaCambio_Respaldos.cs`), todo lo que sabés de `FrmTasaCambio` aplica idéntico.
- Para confirmar cuál variante está activa, mirá el título de la ventana — ambas dicen *"Tasa de Cambio"*. La diferencia es interna y no afecta al usuario final.

## Pantallas relacionadas

- **`FrmTasaCambio`** — versión principal con la documentación detallada.
- **`FrmActualizarTasa`** — diálogo automático del primer login.
- **`FrmTrazabilidadTasa`** — auditoría detallada.
