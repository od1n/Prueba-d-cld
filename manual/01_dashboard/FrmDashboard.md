# `FrmDashboard` — Dashboard (versión legacy)

> ⚠ **Reemplazado por `FrmDashboardV2`** — esta es la versión anterior del dashboard. Se mantiene por compatibilidad, pero el menú principal ya enlaza al nuevo. Documentado por si encontrás un acceso a la versión antigua.

## Cómo llegar

- **Ruta**: ya **no es la ruta por defecto**. El menú principal abre `FrmDashboardV2`.
- **Atajo**: ninguno.
- **Permiso requerido**: `Reportes` o `Admin`.

## Diferencias con la versión actual

| Aspecto | `FrmDashboard` (legacy) | `FrmDashboardV2` (actual) |
|---|---|---|
| **Layout** | Sidebar lateral con accesos rápidos a reportes + KPIs en grilla | Strip horizontal denso de KPIs + panel de alertas + gráfica rotativa |
| **Modo oscuro** | Soporta tema oscuro (configurable en `TemaOscuro`) | Tema claro corporativo unificado |
| **Refresco automático** | Cada 60 segundos | Manual (botón ↻) |
| **Accesos a reportes** | Botones directos a Pto. Equilibrio, Flujo de caja, Rentabilidad, Reportes | Sólo dashboard, sin botones de reporte |
| **Gráfica** | Cuatro paneles fijos (barras, donut, línea, stock) | Una gráfica rotativa que cambia día a día (4 vistas) con opción de fijar |
| **Botón Ayuda** | Sí (botón **? Ayuda** en la barra superior) | No directo (la ayuda está en cada módulo) |

## Para qué se usaba

Mismo objetivo que la versión actual: vista de resumen del negocio. Tenía la ventaja de ofrecer accesos rápidos a los reportes principales desde la misma pantalla.

## Qué ves en la pantalla (referencia histórica)

- **TopBar oscura** con título *"Dashboard"*, fecha del día, tasa BCV resumida y los botones **? Ayuda**, **Pto. Equilibrio**, **Flujo de Caja**, **Rentabilidad**, **Ver Reportes**.
- **Sidebar izquierda** con la lista de KPIs apilados (Ventas mes, Ventas hoy, Facturas pendientes, Stock bajo, Clientes, Top producto).
- **Centro** con cuatro paneles fijos: barras de ventas, donut de productos, línea de tendencia y barras horizontales de stock.

## Recomendación

**Usar `FrmDashboardV2`**. La versión actual:

- Es más rápida.
- Tiene un panel de alertas integrado (sin tener que abrir otra ventana).
- Maneja mejor el sello de frescura de la tasa BCV.
- Está alineada al diseño del menú principal.

Si por algún motivo necesitás la vista clásica, podés invocar `FrmDashboard` directamente desde el código, pero **no se recomienda** para uso diario.

## Pantallas relacionadas

- **`FrmDashboardV2`** — la versión activa. **Usar esta.**
- **`FrmRentabilidad`**, **`FrmFlujoCaja`**, **`FrmPuntoEquilibrio`**, **`FrmReportes`** — reportes que la versión legacy enlazaba directamente.
