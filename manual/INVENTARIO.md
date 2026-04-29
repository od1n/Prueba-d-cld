# Inventario de pantallas

Lista maestra de todas las pantallas del sistema, agrupadas por el módulo desde el que se acceden en el menú principal. Cada nombre de pantalla enlaza a su ficha.

> **Nota:** los formularios marcados con **🔒** sólo son visibles/usables para el rol `Admin`. Los marcados con un permiso (`Ventas`, `Almacén`, `Contabilidad`) requieren que el usuario tenga ese permiso asignado por un administrador. Detalle completo en [`99_anexos/roles_permisos.md`](99_anexos/roles_permisos.md).

## Roles del sistema

| Rol | Para qué se usa |
|---|---|
| **Admin** | Acceso total, gestión de usuarios, configuración, respaldos, auditoría |
| **Ventas** | Facturación, clientes, cuentas por cobrar, cambio de precios |
| **Almacén** | Inventario, kardex, producción, moldes, control de calidad |
| **Contabilidad** | Egresos, cuentas por pagar, libro IVA, reportes financieros |

---

## 00 · Arranque y navegación

Capítulo introductorio: [`00_introduccion.md`](00_introduccion.md)

| Pantalla | Propósito | Permiso |
|---|---|---|
| [`FrmLogin`](00_introduccion/FrmLogin.md) | Inicio de sesión | — |
| [`FrmCambioContrasena`](00_introduccion/FrmCambioContrasena.md) | Cambio de contraseña (forzado o voluntario) | — |
| [`FrmAlertas`](00_introduccion/FrmAlertas.md) | Resumen de alertas al iniciar sesión | — |
| [`FrmBuscador`](00_introduccion/FrmBuscador.md) | Buscador global (atajo **F3**) | — |

## 01 · Dashboard

| Pantalla | Propósito | Permiso |
|---|---|---|
| [`FrmDashboardV2`](01_dashboard/FrmDashboardV2.md) | KPIs y resumen del negocio (activo) | `Reportes` |
| [`FrmDashboard`](01_dashboard/FrmDashboard.md) | Dashboard legacy *(reemplazado por V2)* | `Reportes` |

## 02 · Transacciones

Acceso: *Menú principal → Transacciones*. Flujos de plata diaria.

| Pantalla | Propósito | Permiso |
|---|---|---|
| [`FrmTransacciones`](02_transacciones/FrmTransacciones.md) | Submenú del módulo (lanzador) | `Transacciones` |
| [`FrmFacturas`](02_transacciones/FrmFacturas.md) | Emisión y consulta de facturas / notas de entrega | `Ventas` |
| [`FrmCuentasCobrar`](02_transacciones/FrmCuentasCobrar.md) | Cuentas por cobrar a clientes y registro de pagos | `Ventas` |
| [`FrmCambioPrecios`](02_transacciones/FrmCambioPrecios.md) | Ajuste masivo o individual de precios | `Ventas` |
| [`FrmEgresos`](02_transacciones/FrmEgresos.md) | Registro de egresos / gastos (incluye compras de MP) | `EgresosVer` |
| [`FrmGastoEditor`](02_transacciones/FrmGastoEditor.md) | Diálogo para crear/editar un egreso individual | `EgresosVer` |
| [`FrmCategoriaEditor`](02_transacciones/FrmCategoriaEditor.md) | Diálogo de gestión de categorías de gasto | `EgresosCategorias` |
| [`FrmCuentasPagar`](02_transacciones/FrmCuentasPagar.md) | Cuentas por pagar a proveedores y registro de pagos | `Contabilidad` |

## 03 · Archivos

Acceso: *Menú principal → Archivos*. Catálogos maestros del sistema.

| Pantalla | Propósito | Permiso |
|---|---|---|
| [`FrmArchivos`](03_archivos/FrmArchivos.md) | Submenú del módulo (lanzador) | `Archivos` |
| [`FrmInventario`](03_archivos/FrmInventario.md) | Catálogo de productos terminados y materia prima | `Almacén` |
| [`FrmClientes`](03_archivos/FrmClientes.md) | Catálogo de clientes | `Ventas` |
| [`FrmProveedores`](03_archivos/FrmProveedores.md) | Catálogo de proveedores | `Contabilidad` |
| [`FrmProductosProveedor`](03_archivos/FrmProductosProveedor.md) | Productos / insumos por proveedor | `Contabilidad` |
| [`FrmMoldes`](03_archivos/FrmMoldes.md) | Catálogo de moldes de producción | `Almacén` |
| [`FrmMantenimientoMoldes`](03_archivos/FrmMantenimientoMoldes.md) | Programa e historial de mantenimientos | `Almacén` |
| [`FrmKardex`](03_archivos/FrmKardex.md) | Movimientos de inventario (entradas, salidas, ajustes) | `Almacén` |
| [`FrmControlCalidad`](03_archivos/FrmControlCalidad.md) | Inspecciones de calidad | `Almacén` |
| [`FrmTarifasBonoProduccion`](03_archivos/FrmTarifasBonoProduccion.md) | Tarifas de bono por unidad producida | 🔒 Admin |

## 04 · Producción

Acceso: *Menú principal → Producción*. Operaciones de planta.

| Pantalla | Propósito | Permiso |
|---|---|---|
| [`FrmRegistroProduccionDia`](04_produccion/FrmRegistroProduccionDia.md) | Registro diario por turno/empleado (activo) | `IngresoProduccion` |
| [`FrmIngresoProduccion`](04_produccion/FrmIngresoProduccion.md) | Ingreso de producción legacy | `IngresoProduccion` |
| [`FrmOrdenesProduccion`](04_produccion/FrmOrdenesProduccion.md) | Órdenes de producción planificadas | `IngresoProduccion` |
| [`FrmMermas`](04_produccion/FrmMermas.md) | Registro y consulta de mermas | `Almacén` |

## 05 · Nómina

Acceso: *Menú principal → Nómina*.

| Pantalla | Propósito | Permiso |
|---|---|---|
| [`FrmNomina`](05_nomina/FrmNomina.md) | Submenú del módulo (lanzador) | `Nomina` |
| [`FrmEmpleados`](05_nomina/FrmEmpleados.md) | Catálogo de empleados | `Nomina` |
| [`FrmProcesarNomina`](05_nomina/FrmProcesarNomina.md) | Cálculo y cierre de la nómina semanal | `Nomina` |
| [`FrmPrestamos`](05_nomina/FrmPrestamos.md) | Préstamos a empleados (descuentos en nómina) | `Nomina` |
| [`FrmBeneficiosLOTTT`](05_nomina/FrmBeneficiosLOTTT.md) | Utilidades, vacaciones, prestaciones (LOTTT) | `Nomina` |

## 06 · Reportes

Acceso: *Menú principal → Reportes* (barra inferior).

| Pantalla | Propósito | Permiso |
|---|---|---|
| [`FrmReportes`](06_reportes/FrmReportes.md) | Submenú de reportes (lanzador) | `Reportes` |
| [`FrmRentabilidad`](06_reportes/FrmRentabilidad.md) | P&L (estado de resultados) por período | `Reportes` |
| [`FrmFlujoCaja`](06_reportes/FrmFlujoCaja.md) | Flujo de caja proyectado/histórico | `Reportes` |
| [`FrmLibroIVA`](06_reportes/FrmLibroIVA.md) | Libro de Ventas/Compras (SENIAT) | `Reportes` |
| [`FrmComparacion`](06_reportes/FrmComparacion.md) | Comparativo entre dos períodos | `Reportes` |
| [`FrmPuntoEquilibrio`](06_reportes/FrmPuntoEquilibrio.md) | Cálculo de punto de equilibrio | `Reportes` |
| [`DlgConceptoPE`](06_reportes/DlgConceptoPE.md) | Diálogo auxiliar del punto de equilibrio | `Reportes` |
| [`FrmGastos`](06_reportes/FrmGastos.md) | Gastos legacy *(reemplazado por FrmEgresos)* | `Contabilidad` |
| [`FrmCompras`](06_reportes/FrmCompras.md) | Compras legacy *(deprecada — ahora vía FrmEgresos)* | `Contabilidad` |

## 07 · Tasa de cambio

Acceso: *Menú principal → Tasa de cambio* (barra inferior). También se dispara automáticamente al primer login del día si no hay tasa cargada.

| Pantalla | Propósito | Permiso |
|---|---|---|
| [`FrmTasaCambio`](07_tasa_cambio/FrmTasaCambio.md) | Submenú del módulo (lanzador) | — |
| [`FrmActualizarTasa`](07_tasa_cambio/FrmActualizarTasa.md) | Carga manual o automática de la tasa BCV/paralelo | — |
| [`FrmTrazabilidadTasa`](07_tasa_cambio/FrmTrazabilidadTasa.md) | Historial de cambios de tasa con autor | — |
| [`FrmTasaCambio_Respaldos`](07_tasa_cambio/FrmTasaCambio_Respaldos.md) | Variante alterna del submenú de tasa | 🔒 Admin |

## 08 · Sistema

Acceso: *Menú principal → Sistema* (barra inferior).

| Pantalla | Propósito | Permiso |
|---|---|---|
| [`FrmSistema`](08_sistema/FrmSistema.md) | Submenú del módulo (lanzador) | — |
| [`FrmRespaldos`](08_sistema/FrmRespaldos.md) | Crear, restaurar y configurar respaldos | 🔒 Admin |
| [`FrmPerfil`](08_sistema/FrmPerfil.md) | Datos del usuario y cambio de contraseña | — |
| [`FrmLogAuditoria`](08_sistema/FrmLogAuditoria.md) | Auditoría de acciones del sistema | 🔒 Admin |
| [`FrmUsuarios`](08_sistema/FrmUsuarios.md) | Gestión de usuarios y roles | 🔒 Admin |

## 99 · Anexos

| Documento | Contenido |
|---|---|
| [`roles_permisos.md`](99_anexos/roles_permisos.md) | Tabla detallada de permisos por rol (extraída de `PermisosRol`) |
| [`atajos.md`](99_anexos/atajos.md) | Atajos de teclado del sistema |
| [`glosario.md`](99_anexos/glosario.md) | Términos contables y operativos |
| [`exportar_pdf.md`](99_anexos/exportar_pdf.md) | Cómo convertir el manual a un PDF único |

---

## Resumen

| Concepto | Cantidad |
|---|---:|
| Fichas de formularios | 50 |
| Anexos | 4 |
| Archivos índice (README, INVENTARIO, PLANTILLA, intro) | 4 |
| **Total de archivos** | **58** |
