# Glosario

> Términos contables, fiscales y operativos que aparecen en el sistema. Definiciones cortas pensadas para usuarios operativos.

## Monedas y tasas de cambio

- **Bs (Bolívar)** — Moneda oficial de Venezuela. Todos los precios y montos se almacenan en bolívares y se muestran junto a su equivalente en dólares según la tasa vigente.
- **USD ($)** — Dólar de Estados Unidos. Moneda de referencia para precios "duros" en el sistema (catálogos, reportes financieros).
- **BCV** — Banco Central de Venezuela. La **tasa BCV** es la tasa oficial publicada cada día hábil; la usa el sistema para conversiones legales y reportes fiscales.
- **Paralelo** — Tasa del mercado cambiario informal/no oficial (típicamente más alta que la BCV). El sistema la registra pero los reportes fiscales usan la BCV.
- **Tasa de cambio del día** — Combinación BCV + Paralelo registrada para una fecha concreta. Determina la conversión Bs↔USD de las facturas y reportes de ese día.

## Documentos fiscales

- **Factura (FAC)** — Documento de venta con IVA y valor fiscal. Se reporta al SENIAT en el libro de IVA.
- **Factura de exportación (EXP)** — Factura con IVA 0% legal, emitida a clientes extranjeros. Cuenta como factura legal pero con IVA cero.
- **Nota de entrega (NE)** — Documento que acompaña una entrega de mercancía pero **no tiene valor fiscal** y no se declara al SENIAT. Por defecto se excluye de reportes y totales (un Admin puede activar la "Vista NE" para incluirlas).
- **Anulación** — Marca una factura como anulada con motivo registrado. **No la borra** (rompería el correlativo SENIAT). El correlativo no se reutiliza.
- **Correlativo SENIAT** — Numeración secuencial obligatoria de las facturas. No puede saltar números ni reutilizar números de facturas anuladas.

## Impuestos y entes legales

- **IVA** — Impuesto al Valor Agregado. En Venezuela suele ser 16% sobre el subtotal de la factura.
- **SENIAT** — Servicio Nacional Integrado de Administración Aduanera y Tributaria. Ente recaudador venezolano. El sistema genera el **libro de IVA** para cumplir con sus exigencias.
- **LOTTT** — Ley Orgánica del Trabajo, los Trabajadores y las Trabajadoras. Define las prestaciones, utilidades, vacaciones y bono vacacional que el sistema calcula en el módulo Nómina.
- **RIF** — Registro de Información Fiscal. Identificación tributaria de personas jurídicas y naturales (ej. `J-12345678-9`, `V-12345678`).

## Inventario y producción

- **Kardex** — Registro de movimientos de inventario (entradas, salidas, ajustes). Cada producto tiene su tarjeta kardex con saldo y costo.
- **Stock mínimo** — Cantidad por debajo de la cual el producto entra en alerta de "Stock Bajo".
- **Merma** — Pérdida de inventario por daño, vencimiento, robo o producción fallida. Se registra para deducir del stock y reflejar en reportes.
- **Molde** — Herramienta de producción (típicamente para inyección de plástico, fundición, etc.) con un programa de mantenimiento preventivo.
- **Mantenimiento de molde** — Servicio periódico al molde para evitar fallas. El sistema genera alertas cuando un mantenimiento está vencido.
- **Orden de producción** — Documento que autoriza la fabricación de un lote. Describe producto, cantidad, materias primas y estado.
- **Control de calidad** — Inspección de un lote producido con puntaje (0-5) y observaciones.

## Cuentas comerciales

- **CxC (Cuentas por Cobrar)** — Plata que los clientes nos deben por facturas a crédito. Se gestiona en *Transacciones → Cuentas por Cobrar*.
- **CxP (Cuentas por Pagar)** — Plata que le debemos a los proveedores por compras a crédito. Se gestiona en *Transacciones → Cuentas por Pagar*.
- **Conciliación** — Proceso de cruzar los pagos registrados con los movimientos del banco/efectivo para detectar diferencias.
- **Saldo** — Diferencia entre `Total - Pagado` de una cuenta. Si es positivo, queda algo por cobrar/pagar.

## Reportes financieros

- **P&L (Profit & Loss)** — Estado de resultados. Reporte que muestra ingresos, costos y utilidad de un período. En el sistema está en *Reportes → Rentabilidad*.
- **Punto de equilibrio** — Nivel de ventas (en unidades o dinero) donde los ingresos igualan a los costos. Ni se gana ni se pierde. Se calcula en *Reportes → Punto de Equilibrio*.
- **Flujo de caja** — Movimientos de dinero proyectados o históricos: cuánto entra, cuánto sale, cuándo. *Reportes → Flujo de Caja*.
- **Libro IVA** — Reporte fiscal mensual exigido por el SENIAT. Lista todas las facturas legales del período con base imponible e IVA.
- **Comparativo** — Reporte que compara dos períodos (ventas, costos, utilidad) para ver tendencias.

## Otros términos del sistema

- **Tasa BCV "fresca"** — Tasa del día actual. El sistema la marca con un sello: `● actualizada hoy` (verde), `1 día atrás` (ámbar), `varios días atrás` (rojo).
- **Auditoría** — Registro automático e inmutable de las acciones del sistema (login, anulaciones, cambios). Sólo Admin puede consultarla.
- **Respaldo** — Copia de seguridad cifrada de la base de datos. Se hace manual o automática (al cerrar sesión). Retención por defecto: 30 días.
- **Permiso** — Autorización puntual asociada a un rol (ej. `FacturasAnular`, `TasaModificar`). Determina qué botones se habilitan.
- **Sesión** — Datos del usuario activo (id, rol, nombre). Se establece al login y se cierra al salir.
