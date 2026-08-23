# 06 — Plan y estado

## 1. Estado por área (agosto 2026)

| Área | Estado | Notas |
|---|---|---|
| Seguridad (login, roles, auditoría, respaldos) | ✅ En uso | PBKDF2 600k, AES-256 respaldos, log de auditoría |
| Ciclo de ventas (facturas, CxC, abonos) | ✅ En uso | Factura→kardex→inventario transaccional |
| Inventario / Kardex | ✅ En uso | Ajustes, movimientos, existencias |
| Compras / Proveedores / Gastos | ✅ En uso | Entrada a kardex |
| Producción / Calidad / Moldes | ✅ En uso | Cierre de OP con descuento proporcional |
| Nómina (LOTTT, USD) | ✅ En uso | IVSS/RPVH, préstamos, aportes patronales, beneficios, planillas PDF |
| Dashboard / Reportes | ✅ En uso | KPIs + gráficas GDI+; fix gráfica de 1 solo día |
| Cambio de precios | ✅ En uso | Individual / grupo / masivo, sobre inventario real |
| Portal web (API + SPA) | ✅ En uso | Auth JWT, impresión, módulos operativos |
| Digitalizador OCR | ✅ Integrado | Factura venta/compra, planilla |
| Cámaras / CCTV (EZVIZ) | ✅ Integrado | Snapshots por proxy, permisos por usuario |
| Bot de WhatsApp | ✅ Integrado | Catálogo, estado de cuenta (PIN), pedidos por email |
| Estudio de Contenido (redes) | ✅ Construido | Publicación automática a Meta **bloqueada** por Verificación de Negocio |
| Marca de agua RIF | ✅ En uso | J-30571871-7, centrada arriba, en ZIP y publicación |

## 2. Versionado
- **Núcleo**: SCHEMA_VERSION incremental (v29+); migraciones v1→vN al arrancar.
- **Contenido**: tablas gestionadas por `DbContenido.EnsureContenido()`.
- **Assets del portal**: versionados con `?v=YYYYMMDD` para invalidar caché del túnel.

## 3. Bloqueos y dependencias externas
- **Meta Business Verification** (pendiente de la empresa): es la única barrera para publicar en vivo, ver métricas reales y recibir la bandeja de Meta. El código ya está listo; solo falta el token tras la verificación.
- **SERVO**: falta confiar el certificado HTTPS local y desplegar los últimos cambios (sin acceso al momento de escribir esto).

## 4. Pendientes de operación / cierre
- **Rotar `SG_INTERNAL_TOKEN`** al cerrar fases (quedó expuesto durante depuración): actualizar en `sg-env.cmd` y en los nodos de n8n; reiniciar la API.
- **Auditoría de seguridad** completa al cierre (túnel, tokens, roles, JWT, permisos, BD).
- Asignar PINs de operador; reimportar el workflow de n8n cuando cambie.

## 5. Módulos sugeridos a futuro (no comprometidos)
- Cuentas por Pagar (proveedores) para cerrar el ciclo contable.
- Flujo de caja (proyección 30/60/90), punto de equilibrio, control de mermas, gestión de cheques, ISLR estimado.
- Alertas al inicio de sesión (stock bajo + cobros vencidos).
- Redes: publicación recurrente/programada y "aprender el movimiento" de un video de referencia para alimentar a Veo.

## 6. Cómo se despliega (recordatorio)
1. **CHAOS**: `git add` + `commit` + `push` (único lugar de push; revisar que `git status` no deje ` M`; si hay `.git\index.lock`, borrarlo).
2. **OLIMPO**: `git pull` + `& "C:\SistemaGestion\deploy\actualizar-olimpo.ps1"` (publica API + WinForms y relanza).
3. **SERVO**: `git pull` + `actualizar-servo.ps1` (solo API).

## 7. Riesgos conocidos y mitigaciones
- **Múltiples BD** → descuadres: regla de una sola BD + verificación del share SMB.
- **Publicar en ruta incorrecta / sin self-contained** → "no cambia nada" o app que no arranca: usar los `actualizar-*.ps1`.
- **Secretos en el repo**: prohibido; todo en variables de entorno *Machine scope*.
- **ffmpeg/ffprobe ausentes** → falla reel/video y marca de agua: instalar y reiniciar la API.
