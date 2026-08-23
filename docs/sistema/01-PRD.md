# 01 — PRD · SistemaGestion (sistema completo)

## 1. Visión

SistemaGestion es el sistema de gestión empresarial (ERP) de **EL HATO PASCHER C.A.**, una fábrica venezolana de productos plásticos. Cubre de punta a punta la operación: ventas y cuentas por cobrar, inventario y kardex, compras, producción y calidad, nómina bajo la ley venezolana (LOTTT), reportes, seguridad y respaldos. Sobre ese núcleo se integran capacidades adicionales: digitalización de documentos por OCR, videovigilancia (cámaras EZVIZ), atención por WhatsApp y un estudio de contenido para redes sociales asistido por IA.

El objetivo es que **una sola fuente de datos** (una base de datos SQLite) alimente dos interfaces —una app de escritorio para gestión completa y un portal web para acceso desde cualquier equipo de la red— manteniendo control de acceso por rol, trazabilidad (auditoría) y cumplimiento legal venezolano (IVA, IVSS, RPVH, LOTTT, RIF en publicaciones).

## 2. Usuarios y roles

| Rol | Qué hace | Acceso principal |
|---|---|---|
| **Admin** | Control total, configuración, usuarios, auditoría, respaldos | Todo |
| **Ventas** | Facturación, cuentas por cobrar, clientes, reportes | Ciclo de ventas |
| **Almacén** | Inventario, kardex, compras, proveedores, producción, moldes | Ciclo de inventario/producción |
| **Contabilidad** | Reportes, facturas, compras, gastos, cambio de precios | Ciclo contable |
| **Redes** | Módulo Estudio de Contenido (crear/aprobar/publicar posts) | Redes sociales |
| **Operador WhatsApp** | Consultas por el bot (catálogo, estado de cuenta) con PIN | Bot WhatsApp |

Un mismo usuario del sistema tiene un rol; el rol determina los módulos visibles y los permisos de escritura. Los operadores de WhatsApp se autentican con un PIN numérico (no con el login del sistema).

## 3. Módulos y capacidades

### Núcleo ERP (app de escritorio + portal web)
- **Dashboard**: KPIs del negocio (ventas del día/mes, pendientes, stock, cuentas por cobrar), gráficas y tasas BCV/paralelo. Alertas de stock bajo y cobros vencidos.
- **Facturación**: facturas y notas de entrega con líneas de producto, IVA configurable (16% o 0%), vista previa, impresión (PDF de escritorio / HTML del portal) y descuento automático de inventario.
- **Clientes** y **Cuentas por Cobrar**: catálogo de clientes, estados de cuenta, abonos, cheques.
- **Inventario** y **Kardex**: catálogo de productos, existencias, movimientos (entradas/salidas), ajustes.
- **Compras** y **Proveedores**: registro de compras con actualización de kardex; gastos/egresos.
- **Producción**: órdenes de producción vinculadas al inventario (descuento proporcional de materia prima al cerrar), ingreso de producción, control de calidad (inspecciones/defectos), moldes y su mantenimiento programado.
- **Nómina (Venezuela, USD)**: empleados, períodos semanales con tasa BCV manual, cálculo de devengado y deducciones (IVSS 4%, RPVH 1%), préstamos internos, aportes patronales y beneficios LOTTT (utilidades, vacaciones, bono vacacional, prestaciones). Planillas PDF (nómina, IVSS, aportes patronales).
- **Reportes**: rentabilidad por producto y otros reportes, con exportación a PDF.
- **Cambio de precios**: actualización de precios individual, por grupo/filtro o masiva, con margen y redondeo.
- **Sistema**: usuarios y roles, perfil y preferencias, log de auditoría, respaldos cifrados, buscador global.

### Capacidades integradas
- **Digitalizador OCR**: toma una foto de una factura/planilla y extrae los datos estructurados con visión por IA, para revisión y guardado. Tipos: factura de venta, factura de compra, planilla de producción.
- **Cámaras / CCTV (EZVIZ)**: portal de cámaras con snapshots en vivo servidos por la API (credenciales del lado del servidor), con permisos por usuario.
- **Bot de WhatsApp**: consultas de catálogo, búsqueda y precio de productos, y —con PIN de operador— estado de cuenta y registro de pedidos (con notificación por email).
- **Estudio de Contenido (redes sociales)**: diseño, generación asistida por IA, aprobación humana, calendario, métricas, bandeja de mensajes, y creación de imágenes/videos (reel armado, video IA con Veo, video externo) para Instagram y Facebook de las marcas del negocio. Toda publicación lleva marca de agua obligatoria con el RIF.

## 4. Requisitos funcionales (resumen)

1. **Una sola base de datos** compartida; ningún módulo debe crear copias paralelas.
2. **Doble interfaz** con la misma data: escritorio (gestión completa, solo OLIMPO) y web (acceso LAN, impresión, módulos operativos).
3. **Control de acceso por rol** en lectura y escritura, con autorización server-side en la API.
4. **Trazabilidad**: acciones críticas quedan en el log de auditoría.
5. **Integridad transaccional**: operaciones que tocan varias tablas (factura→kardex→inventario, cierre de OP, nómina) son atómicas (commit/rollback).
6. **Cumplimiento legal VE**: IVA configurable, cálculos IVSS/RPVH/LOTTT, aportes patronales, y RIF en todas las publicaciones de redes.
7. **Respaldos automáticos** cifrados y verificables.
8. **Seguridad**: contraseñas con hashing fuerte, HTTPS, JWT, restricción a red local, cifrado de credenciales sensibles (cámaras, respaldos).
9. **Aprobación humana** antes de publicar en redes; la IA propone, una persona aprueba.

## 5. Requisitos no funcionales

- **Disponibilidad local**: el sistema opera en la LAN de la fábrica; la BD vive en OLIMPO y SERVO la usa por red. El portal es además accesible por túnel (Cloudflare) para integraciones (WhatsApp/redes).
- **Simplicidad de despliegue**: un comando por máquina (`actualizar-*.ps1`) hace pull + publish + relanzar.
- **Sin dependencias pesadas** donde se pueda: gráficas con GDI+ puro, SQLite sin ORM, SPA en JS vanilla.
- **Costo controlado**: se prioriza IA gratuita/econsubmica (Gemini) y herramientas ya contratadas (n8n, Evolution API).
- **Español neutro** en toda la interfaz.

## 6. Métricas de éxito

- Cierre correcto del ciclo de ventas (factura → cobro) y de producción (OP → inventario) sin descuadres.
- Nómina semanal calculada conforme a la ley, con planillas listas para firmar.
- Respaldo diario íntegro y recuperable.
- Publicaciones de redes con RIF y aprobación humana, sin publicar contenido no autorizado.
- Cero incidencias por "múltiples bases de datos" o despliegues en ruta incorrecta.

## 7. Estado (alto nivel)

El núcleo ERP está **completo y en uso** (ventas, inventario, compras, producción, calidad, nómina, reportes, seguridad, respaldos). Están integrados y operativos el **OCR**, las **cámaras**, el **bot de WhatsApp** y el **Estudio de Contenido** (con publicación automática a Meta pendiente solo de la Verificación de Negocio de Meta). El detalle por fase está en [06-Plan](06-Plan.md).

## 8. Fuera de alcance / decisiones

- No se usa OpenAI por ahora; la IA de contenido y OCR va con Gemini/Claude según el caso.
- SERVO **nunca** corre la app de escritorio, solo la API.
- Los fustes de EHP son de **plástico** (HDPE), color blanco hueso; la línea de marca prohíbe representar madera (competencia) en el contenido generado.
