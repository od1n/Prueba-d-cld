# `FrmArchivos` — Submenú Archivos

> Lanzador del módulo *Archivos*. Reúne en tarjetas todos los **catálogos maestros** y herramientas de configuración del sistema.

## Cómo llegar

- **Ruta**: *Menú principal → Archivos*
- **Permiso requerido**: `Archivos` (o `Admin`). Algunas tarjetas además requieren `Admin`.

## Para qué se usa

Centro de acceso a las pantallas de **datos maestros**: productos, clientes, proveedores, moldes, kardex, control de calidad, además de respaldos, tarifas de bono y usuarios para administradores.

## Qué ves en la pantalla

Cabecera azul oscuro con el título *"Archivos del Sistema"* y el subtítulo *"Catálogos maestros, inventario, accesos y configuración"*. Debajo, un área scrollable con tarjetas de colores. Las tarjetas marcadas con *(solo Admin)* sólo aparecen si el usuario actual es administrador.

### Tarjetas disponibles

| Tarjeta | Subtítulo | Abre |
|---|---|---|
| **Inventario** | Productos terminados | `FrmInventario` |
| **Clientes** | Directorio de clientes | `FrmClientes` |
| **Proveedores** | Directorio de proveedores | `FrmProveedores` |
| **Moldes** | Catálogo de moldes | `FrmMoldes` |
| **Mant. Moldes** | Mantenimiento preventivo | `FrmMantenimientoMoldes` |
| **Kardex** | Movimientos inventario | `FrmKardex` |
| **Insumos Proveed.** | Materias primas | `FrmProductosProveedor` |
| **Control Calidad** | Inspecciones | `FrmControlCalidad` |
| **Respaldos** 🔒 | Copias de seguridad (solo Admin) | `FrmRespaldos` |
| **Tarifas Bono** 🔒 | Producción y ventas (solo Admin) | `FrmTarifasBonoProduccion` |
| **Usuarios** 🔒 | Gestión de accesos (solo Admin) | `FrmUsuarios` |

Cada tarjeta tiene un ícono, color distintivo y un tooltip explicativo al pasar el mouse.

## Casos de uso

### Caso 1 — Abrir un catálogo

1. Hacer clic en la tarjeta deseada.
2. Se abre la pantalla como ventana modal.
3. Al cerrarla, volvés al submenú.

## Consejos

- Las tarjetas **se reordenan automáticamente** al cambiar el ancho de la ventana.
- Si no ves la tarjeta de **Respaldos**, **Tarifas Bono** o **Usuarios**, es porque tu rol no es Admin.

## Pantallas relacionadas

- Todas las pantallas listadas arriba.
- **`MenuPrincipalV2`** — el menú general que abre este submenú.
