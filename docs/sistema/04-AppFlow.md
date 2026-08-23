# 04 — App Flow · Flujos end-to-end

Flujos de los procesos clave, tal como están implementados. Los flujos "críticos" (factura, OP, nómina) son **transaccionales**: si algo falla, se revierte todo.

## 1. Ciclo de ventas: Factura → Kardex → Inventario
1. El usuario crea una factura (o nota de entrega) con sus líneas de producto y el IVA correspondiente (16% o 0%).
2. Vista previa (modal en web / `MessageBox` en escritorio) para confirmar.
3. Al guardar, en **una sola transacción** (`GuardarDetallesYKardex`): por cada línea se valida stock, se inserta el detalle, se registra un movimiento de **salida** en el kardex y se descuenta la existencia del inventario.
4. Si algún producto no tiene stock suficiente → **rollback** total (no se guarda nada).
5. La factura queda disponible para impresión (PDF de escritorio / HTML del portal) y, si es a crédito, genera/alimenta la cuenta por cobrar.

## 2. Cobros: Cuentas por Cobrar y Abonos
1. Las facturas a crédito aparecen en Cuentas por Cobrar con su saldo.
2. Se registran **abonos** (y cheques) que reducen el saldo y quedan en el historial.
3. El estado de cuenta de un cliente puede consultarse también por el bot de WhatsApp (con PIN de operador).

## 3. Compras y Kardex
1. Se registra una compra a un proveedor con sus líneas.
2. Cada línea entra como **entrada** en el kardex y aumenta la existencia del inventario.
3. Los gastos/egresos se registran aparte para el control contable.

## 4. Producción: Orden → cierre → Inventario
1. Se crea una **Orden de Producción** (OP) con las materias primas planificadas.
2. Al **cerrar** la OP con la cantidad real (`CerrarOrden`): se descuenta cada materia prima proporcional a `cantidadReal / cantidadPlan`, se agrega el producto terminado como **entrada** al kardex y la orden pasa a *Completada*. Todo en una transacción.
3. El **ingreso de producción** permite pasar por **control de calidad** (inspecciones y defectos).
4. Los **moldes** tienen un programa de mantenimiento con registros y una vista de *timeline*.

## 5. Nómina semanal (Venezuela, USD)
1. Se crea un **período** semanal con la **tasa BCV** (manual, obligatoria).
2. Por empleado se calcula el **devengado** = sueldo/día × días + piezas × tarifa + ventas × tarifa + otros.
3. **Deducciones**: IVSS 4% y RPVH 1% se calculan sobre el devengado en **bolívares** (× tasa BCV) y se convierten de vuelta a USD; se restan faltas y préstamos.
4. **Neto USD** = devengado − deducciones; **Neto Bs** = Neto USD × tasa BCV.
5. **Aportes patronales** (IVSS patrón 9/10/11% según riesgo, etc.) se calculan sobre el período con el Valor UT vigente.
6. **Beneficios LOTTT** (utilidades, vacaciones, bono vacacional, prestaciones) según los artículos aplicables.
7. Se emiten planillas PDF: nómina, IVSS, aportes patronales y reporte anual de beneficios.

## 6. Digitalización de documentos (OCR)
1. En el portal, el usuario toma una foto de la factura/planilla → `POST /api/facturas/extraer-ocr`.
2. La API hace de **proxy** al digitalizador Python (`/api/extraer`) con el `tipo` (factura_venta / factura_compra / planilla), autenticado con `X-Internal-Token`.
3. El digitalizador usa visión por IA y devuelve los datos estructurados (JSON).
4. El portal muestra un **formulario de revisión** prellenado; el usuario corrige y guarda con el flujo normal (p. ej. `POST /api/facturas`).

## 7. Bot de WhatsApp
1. El cliente escribe por WhatsApp → Evolution API → webhook de **n8n**.
2. n8n llama a la API de SistemaGestion (por el túnel Cloudflare) con `X-Internal-Token`.
3. Comandos (atajos 1–5): catálogo, buscar, precio (públicos); estado de cuenta y pedido (requieren **PIN de operador**).
4. Un **pedido** (`pedido Nombre, Tel, productos`) se registra y dispara una notificación por **email** (SMTP).
5. El PIN se valida con bloqueo por intentos; el Admin lo genera/regenera desde el módulo de usuarios (se muestra una sola vez).

## 8. Cámaras / CCTV
1. El portal de cámaras pide la lista permitida (`GET /api/camaras`, filtrada por `PermisosCamara`).
2. Por cada cámara, el navegador pide `GET /api/camaras/{id}/snapshot`; la API descarga el snapshot de la cámara EZVIZ con las **credenciales del servidor** (cifradas con `SG_CAM_KEY`) y lo devuelve como JPEG.
3. La grilla refresca automáticamente según el intervalo configurado. (Las C6N no exponen RTSP; las H8 sí, además del snapshot.)

## 9. Estudio de Contenido (redes)
Flujo resumido (detalle en [`docs/redes-sociales/04-AppFlow.md`](../redes-sociales/04-AppFlow.md)):
1. **Idea → post**: se genera un post con IA (imagen + texto vía n8n/Gemini) o se crea un **post de video** en blanco.
2. **Edición**: ajustar imagen, regenerar texto/imagen, subir un **video externo** y generar el texto con IA (analizando el video o desde la idea), o crear un **video** (reel armado con ffmpeg / IA con Veo).
3. **Aprobación humana**: enviar a revisión → aprobar (rol correspondiente).
4. **Publicación**: manual (descargar ZIP + subir en la red) o automática por Graph API (foto/carrusel o **Reel**). Toda salida lleva la **marca de agua del RIF** (J-30571871-7).
5. **Post-publicación**: métricas por post/cuenta y **bandeja** de mensajes/comentarios con respuesta IA y escalado a un humano según el horario de atención.

## 10. Respaldo automático
1. Tras el login, `BackupService` verifica si ya hay respaldo del día.
2. Si no, crea una copia **cifrada AES-256** en la carpeta de respaldos y calcula su hash para verificación.
3. Un *timer* revisa el cambio de día; se limpian los respaldos de más de 30 días.
4. La restauración es manual (o con la herramienta `Descifrar/`), corriendo como el usuario `PC` en OLIMPO.
