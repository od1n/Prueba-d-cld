# 03 — UI/UX · Las dos interfaces

SistemaGestion tiene dos interfaces sobre la misma base de datos: la **app de escritorio** (WinForms, gestión completa) y el **portal web** (SPA, acceso desde la red e impresión). Comparten conceptos (roles, sesión, módulos) pero cada una está optimizada para su contexto.

## 1. App de escritorio (WinForms)

### Arranque y sesión
Secuencia: seguridad → BD → **login** → respaldo automático → menú principal. El login usa PBKDF2 con bloqueo tras 5 intentos y registra en auditoría. La sesión global (`Sesion`) guarda usuario, rol y permisos.

### Menú principal
Barra de sesión (Mi Perfil, Auditoría [Admin], Usuarios [Admin], Buscar F3, Cerrar sesión) y botones principales:

```
Dashboard ──▶ FrmDashboard (KPIs + gráficas)
              ├─ Rentabilidad ─▶ FrmRentabilidad
              └─ Ver Reportes ─▶ FrmReportes
Archivos del sistema ─▶ FrmArchivos
              ├─ Inventario · Clientes · Proveedores · Moldes
              ├─ Kardex · Ingreso Producción ─▶ [Calidad] ─▶ FrmControlCalidad
              ├─ Órdenes OP ─▶ FrmOrdenesProduccion
              ├─ Control Calidad ─▶ FrmControlCalidad
              ├─ Mantenimiento Moldes ─▶ FrmMantenimientoMoldes
              └─ Respaldos [Admin] ─▶ FrmRespaldos
Transacciones ─▶ FrmTransacciones
              ├─ Facturas ─▶ FrmFacturas
              ├─ Órdenes de Producción ─▶ FrmOrdenesProduccion
              ├─ Cuentas por Cobrar ─▶ FrmCuentasCobrar
              └─ Cambio de Precios ─▶ FrmCambioPrecios
Compras ─▶ FrmCompras (+ Gastos en Archivos)
Nómina ─▶ FrmNomina
              ├─ Empleados ─▶ FrmEmpleados
              ├─ Procesar Nómina Semanal ─▶ FrmProcesarNomina
              ├─ Préstamos Internos ─▶ FrmPrestamos
              └─ Beneficios LOTTT ─▶ FrmBeneficiosLOTTT
```

### Patrón de pantallas
- **Lista** (`FrmXxx`): `DataGridView` con la data del módulo y botones Nueva/Editar/Eliminar; `CargarDatos()` refresca el grid.
- **Diálogo** (`DlgXxx`): formulario nueva/editar; al guardar produce `Resultado` y cierra con `DialogResult.OK`.
- **Gráficas**: dibujadas a mano en el evento `Paint` de paneles con GDI+ (barras, línea con área, dona, barras horizontales, radar, timeline). Antialias activado; se redibujan con `Invalidate()`.
- **Impresión**: QuestPDF (diseño completo en papel normal).

### Pantallas clave
- **FrmDashboard**: 6 KPIs + 4 gráficas + acceso a Rentabilidad. (La versión FrmDashboardV2 corrige el dibujo de la gráfica de línea cuando el mes tiene un solo día.)
- **FrmFacturas / DlgFactura**: cabecera + líneas, selección de producto, IVA (16%/0%), vista previa por `MessageBox` antes de guardar.
- **FrmInventario / FrmKardex**: catálogo y movimientos; la descripción admite comillas y símbolos de medida (`"`, `°`, etc.).
- **FrmCambioPrecios**: sobre el inventario real — búsqueda, filtro por Departamento, precio nuevo editable por fila, % a seleccionados/filtro/todos, redondeo y margen; doble clic en un producto abre un diálogo rápido que cambia y guarda ese precio al instante.
- **Nómina** (FrmProcesarNomina): períodos semanales, cálculo automático, pestaña de Aportes Patronales, planillas PDF.
- **FrmRespaldos**: lista y verifica integridad de respaldos (la restauración es manual / herramienta Descifrar).
- **FrmBuscador (F3)**: buscador global con *debounce* y resultados agrupados por tabla.
- **FrmLogAuditoria**: visor con filtros y gráficas de actividad.

## 2. Portal web (SPA)

### Naturaleza
SPA en HTML/CSS/JS **vanilla** con router propio, servida desde `wwwroot` por la API. Diseño *mobile-first* (se usa también desde el teléfono). Autenticación por cookie JWT; los módulos visibles dependen del rol.

### Estilo
- Paleta y componentes propios en `css/app.css` (variables de color, tarjetas, badges, modales centrados sobre overlay).
- Impresión de facturas con HTML + `@media print` (formularios preimpresos: la clase `pp` no se imprime).
- Versionado de assets con `?v=YYYYMMDD` para saltar la caché del túnel.

### Módulos del portal
- **Facturación e impresión**: alta de factura con vista previa (modal) antes de guardar; impresión para formularios preimpresos.
- **Consulta operativa**: según el rol, acceso a datos de ventas, inventario, cuentas por cobrar, etc.
- **Digitalizar factura**: `digitalizar-factura.html/.js` — el usuario toma una foto, la API la manda al OCR y devuelve un formulario prellenado para revisión y guardado.
- **Cámaras**: `camaras.html/.js` — grilla de snapshots con refresco automático configurable; solo muestra las cámaras permitidas al usuario.
- **Estudio de Contenido**: `contenido.html/.js` — el módulo de redes (ver abajo).

### Estudio de Contenido (resumen de UX)
Un módulo rico dentro del portal (rol Redes). Pestañas **Ideas / Posts / Calendario** por marca, con:
- Cabecera con acciones: 📎 Referencias (bancos de imágenes), 📹 Videos ref (bancos de video), ⚙ Cuentas.
- **Ideas → ✨ Generar post** (crea y genera con IA) y **➕ Post de video** (crea un post en blanco para usar un video externo).
- **Detalle del post**: carrusel de imágenes/video, edición de caption (ES/EN) y hashtags, regeneración (texto/imagen/todo), ✨ ajuste puntual de imagen, 🎬 crear video (armado / IA Veo), 🎞️ subir video externo + generar texto con IA (analiza el video o la idea).
- **Publicar**: descargar ZIP (imágenes/video + texto), copiar texto, o publicar automático (imagen/carrusel o Reel). Aviso visible de que toda publicación sale con la marca de agua del RIF.
- **📊 Métricas** por post y por cuenta; **💬 Bandeja** de mensajes/comentarios con respuesta IA y escalado a humano según horario.

El detalle completo de estas pantallas está en [`docs/redes-sociales/03-UIUX.md`](../redes-sociales/03-UIUX.md).

## 3. Principios de UX transversales
- **Español neutro** en toda la interfaz.
- **Confirmación antes de acciones irreversibles** (guardar factura, publicar, eliminar).
- **Feedback inmediato**: mensajes de estado, *spinners*, badges de estado (borrador, en revisión, aprobado, publicado, etc.).
- **Permisos visibles**: cada usuario solo ve lo que su rol permite.
- **Continuidad escritorio↔web**: los mismos datos y reglas (IVA, kardex, cobros) se comportan igual en ambas interfaces.
