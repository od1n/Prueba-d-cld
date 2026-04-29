# `FrmAlertas` — Alertas pendientes al inicio de sesión

> Ventana que se abre justo después del login con el resumen de los items que requieren atención: stock bajo, cuentas por cobrar vencidas y mantenimientos de moldes vencidos. Permite ignorar cada categoría hasta el día siguiente.

## Cómo llegar

- **Ruta**: aparece automáticamente tras un login exitoso si hay alertas activas. También se puede reabrir desde la franja de información del menú principal (botón **ALERTAS**).
- **Atajo**: ninguno.
- **Permiso requerido**: ninguno (la ven todos los usuarios).

## Para qué se usa

Resume en un vistazo qué temas operativos requieren acción hoy. Es la primera pantalla del día tras el login. Si los items se resolvieron, podés cerrar y seguir al menú principal. Si no son urgentes hoy, podés **ignorar la alerta hasta mañana**.

## Antes de empezar

- Tener cargada la información de inventario, cuentas por cobrar y moldes (catálogos del sistema). Si las tablas están vacías la pantalla muestra los mensajes "✓ Sin productos bajo el stock minimo" y similares.

## Qué ves en la pantalla

```
┌──────────────────────────────────────────────────────────────┐
│ ⚠  Alertas Pendientes                            28/04/2026  │  ← header oscuro
├──────────────────────────────────────────────────────────────┤
│  [ 12 ] Stock Bajo    [ 4 ] CxC Vencidas    [ 2 ] Mantenim.  │  ← badges
├──────────────────────────────────────────────────────────────┤
│ [ Stock Bajo (12) ][ CxC Vencidas (4) ][ Mantenimiento (2) ] │  ← pestañas
│                                                              │
│  Codigo   Descripción       Departamento  Existencia  Min.   │  ← grilla
│  P0001    Tornillo 3/8"      Ferretería       2,0      10,0  │
│  P0042    Tubo PVC 1/2"      Plomería         0,0       5,0  │
│  ...                                                         │
├──────────────────────────────────────────────────────────────┤
│ Tip: "Ignorar hoy" oculta la alerta hasta mañana.            │
│                  [ Ignorar esta alerta hoy ] [ Entendido — Cerrar ]│
└──────────────────────────────────────────────────────────────┘
```

### Pestañas (tabs)

| Pestaña | Qué muestra | Columnas |
|---|---|---|
| **Stock Bajo** | Productos con existencia ≤ stock mínimo configurado | Código · Descripción · Departamento · Existencia · Mínimo |
| **CxC Vencidas** | Facturas a crédito ya vencidas | Cliente · Concepto · Saldo (USD) · Vencio · Días Vencida |
| **Mantenimiento** | Moldes con mantenimiento programado pasado de fecha | Molde · Descripción · Tipo Mant. · Último Mant. · Días Vencido · Estado |

> **Color de fila** — rojo claro: situación crítica (stock 0, deuda > 30 días, mantenimiento > 7 días vencido). Amarillo claro: situación de atención.

### Badges (resumen superior)

Tres tarjetas con el conteo total por categoría. Si una categoría fue ignorada, aparece con un ✓ verde y el texto "(ignorada)".

## Casos de uso

### Caso 1 — Revisar y cerrar

**Pasos**:

1. Hacer clic en cada pestaña para revisar el detalle.
2. Tomar nota mental o resolver los items críticos (registrar entrada en *Kardex*, llamar al cliente moroso, programar mantenimiento).
3. Hacer clic en **Entendido — Cerrar**.

**Resultado**: la ventana se cierra y aparece el menú principal. Los items siguen apareciendo hasta que se resuelvan en sus respectivos módulos.

### Caso 2 — Ignorar una alerta hasta mañana

**Cuándo**: la alerta está bajo control pero no se va a resolver hoy (ej. "ya hablé con el cliente, paga mañana").

**Pasos**:

1. Seleccionar la pestaña que querés ignorar.
2. Hacer clic en **Ignorar esta alerta hoy**.
3. Confirmar el aviso `Ignorar la alerta "Stock Bajo" por hoy? Volvera a aparecer manana si sigue pendiente.`

**Resultado**: la pestaña se marca con un ✓, el badge se vuelve gris y el contenido se reemplaza por el mensaje "✓ Alerta ignorada hoy". Si ignorás las **tres** categorías, aparece `Todas las alertas han sido ignoradas por hoy.` y la ventana se cierra sola.

> El silencio dura **sólo hasta la medianoche**. Mañana las alertas vuelven a aparecer si los items siguen pendientes.

## Validaciones y errores frecuentes

| Mensaje en pantalla | Qué significa | Cómo se resuelve |
|---|---|---|
| `Esta alerta ya fue ignorada hoy.` | Volviste a hacer clic en "Ignorar" sobre una pestaña ya silenciada | Es informativo, no es un error |
| `Todas las alertas han sido ignoradas por hoy.` | Silenciaste las 3 categorías | La ventana se cierra; el menú principal sigue mostrando el conteo en la franja de información |

## Consejos

- **Stock 0 (rojo intenso)** suele indicar que se descontó stock al facturar pero no se registró la entrada de producción/compra. Ir a *Archivos → Kardex* y revisar.
- **CxC con > 30 días** son de alta prioridad: gestionar el cobro o evaluar la incobrabilidad.
- **Mantenimientos vencidos > 7 días** pueden frenar la producción si el molde se daña. Programá el mantenimiento desde *Archivos → Mantenimiento de Moldes*.
- "Ignorar hoy" **no resuelve** el problema, sólo lo oculta. Asegurate de no perder de vista los items críticos.

## Pantallas relacionadas

- **`FrmInventario`** / **`FrmKardex`** — para resolver alertas de stock.
- **`FrmCuentasCobrar`** — para cobrar las CxC vencidas.
- **`FrmMantenimientoMoldes`** — para registrar mantenimientos.
- **`MenuPrincipalV2`** — la franja superior muestra el conteo de alertas y permite reabrir esta ventana con el botón **ALERTAS**.
