# Diseño UI/UX — Estudio de Contenido

**Versión:** 1.0 · **Fecha:** 2026-08-16 · **Estado:** Borrador
**Referencias:** [PRD](01-PRD.md) · [TRD](02-TRD.md) · [App Flow](04-AppFlow.md)

---

## 1. Principios de diseño

1. **Se ve parte del portal**, no un pegote. Reutiliza el router, la tipografía, los colores
   y los componentes (cards, tablas, modales) del portal actual (`wwwroot`, JS vanilla).
2. **Flujo de izquierda a derecha = madurez del contenido**: Ideas → Generar → Editar →
   Aprobar → Programar. El usuario avanza en una dirección.
3. **Menos escritura, más selección.** El valor es que la IA proponga; el usuario elige y
   ajusta. Botones "Generar" y "Regenerar variante" en todos lados.
4. **Todo lleva marca e idioma visibles.** Un selector de marca global arriba fija el
   contexto (EHP Saddles / El Hato Pascher Plásticos); nada es ambiguo.
5. **La aprobación se siente como un checkpoint, no un trámite.** Vista previa fiel a IG/FB
   y dos botones claros.

---

## 2. Navegación

Entrada de menú nueva en el portal: **Redes → Estudio de Contenido**, con sub-secciones
(tabs o menú lateral del módulo):

```
Estudio de Contenido
├── 📊 Panel            (resumen + calendario de la semana)
├── 💡 Ideas            (banco de ideas, kanban)
├── ✨ Crear            (generador: idea → prompt → post)
├── ✅ Aprobaciones     (cola de revisión)   ← badge con pendientes
├── 🗓️  Calendario      (vista mensual editorial)
├── 🖼️  Biblioteca      (assets, plantillas de prompt, posts publicados)
└── ⚙️  Configuración   (marcas, cuentas de red, servicios de IA)  [Admin]
```

Barra superior del módulo (persistente): **selector de marca** + **selector de idioma de
vista** + botón rápido **“+ Nuevo post”**.

---

## 3. Pantallas (wireframes)

> Wireframes en ASCII para comunicar layout; el estilo visual final hereda del portal.

### 3.1 Panel

```
┌───────────────────────────────────────────────────────────────────────────┐
│  Estudio de Contenido        [Marca ▾ EHP Saddles] [Idioma ▾ ES] [+ Nuevo]  │
├───────────────────────────────────────────────────────────────────────────┤
│  [ 3 ] En revisión   [ 5 ] Programados   [ 12 ] Publicados este mes         │
│                                                                             │
│  Esta semana                                     Ideas frescas (IA)         │
│  ┌───────────────────────────────────┐          ┌───────────────────────┐  │
│  │ Lun ─ · ─                          │          │ • “Cómo elegir el     │  │
│  │ Mar ▣ IG+FB  10:00  Modelo W14     │          │   fuste correcto”     │  │
│  │ Mié ─                              │          │ • “Taller: fabricación│  │
│  │ Jue ▣ IG    18:00  Detrás cámara   │          │   paso a paso”        │  │
│  │ Vie ▣ IG+FB 12:00  Testimonio      │          │ [ Generar más ideas ] │  │
│  └───────────────────────────────────┘          └───────────────────────┘  │
└───────────────────────────────────────────────────────────────────────────┘
```

### 3.2 Ideas (kanban)

```
┌───────────────────────────────────────────────────────────────────────────┐
│  Ideas — EHP Saddles         [+ Idea manual]  [✨ Generar ideas con IA]     │
├──────────────┬──────────────┬──────────────┬───────────────────────────────┤
│  Nuevas      │  En proceso  │  Descartadas │  (filtros: pilar ▾ / origen ▾) │
│ ┌──────────┐ │ ┌──────────┐ │              │                               │
│ │ Elegir   │ │ │ Testim.  │ │  ...         │  Cada tarjeta:                 │
│ │ fuste    │ │ │ jinete   │ │              │   título · pilar · origen(IA/  │
│ │ #product │ │ │ #caso    │ │              │   manual/base) · [→ Crear post]│
│ │[→ Crear] │ │ └──────────┘ │              │                               │
│ └──────────┘ │              │              │                               │
└──────────────┴──────────────┴──────────────┴───────────────────────────────┘
```

Modal **“Generar ideas con IA”**: marca (heredada) · pilar(es) · cantidad (p. ej. 5) ·
“inspirarse en posts base ✔” → botón **Generar** → las ideas caen en la columna *Nuevas*.

### 3.3 Crear (generador) — el corazón del módulo

Asistente de 3 pasos en una sola pantalla (columnas), con preview a la derecha:

```
┌───────────────────────────────────────────────────────────────────────────┐
│  Crear post — EHP Saddles                                    [Guardar borr.]│
├───────────────────────────────┬───────────────────────────────────────────┤
│ 1) Idea / tema                 │  Vista previa                              │
│  [ Elegir fuste correcto  ▾ ]  │   ┌───────────────────────────┐           │
│  Pilar: Producto               │   │  @ehpsaddles              │           │
│                                │   │  ┌─────────────────────┐  │           │
│ 2) Formato                     │   │  │                     │  │           │
│  ( ) Imagen  (•) Carrusel      │   │  │   [ imagen IA ]     │  │           │
│  ( ) Reel    ( ) Story         │   │  │                     │  │           │
│  Nº imágenes: [ 3 ]            │   │  └─────────────────────┘  │           │
│                                │   │  ◧ ◧ ◧  (carrusel)        │           │
│ 3) Estilo de imagen (prompt)   │   │  ❤  💬  ➤                  │           │
│  [ prompt sugerido por IA... ] │   │  caption ES ▾             │           │
│  [🎲 Regenerar prompt]         │   │  “Elegir el fuste…”       │           │
│  ── o ──                       │   │  #EHPSaddles #…           │           │
│  [ ⬆ Subir foto real ]         │   └───────────────────────────┘           │
│  [☐ Editar con IA: poner en    │                                           │
│     escena / quitar fondo]     │   Plataformas: [✔ IG] [✔ FB]              │
│                                │                                           │
│         [ ✨ Generar post ]     │                                           │
└───────────────────────────────┴───────────────────────────────────────────┘
```

Al pulsar **Generar post** la preview muestra estado *“generando…”* (skeleton) y se
completa cuando n8n hace callback. Luego se pasa al **Editor**.

### 3.4 Editor de post

```
┌───────────────────────────────────────────────────────────────────────────┐
│  Editar post #34 — EHP Saddles · Carrusel                    Estado: Borrador│
├───────────────────────────────┬───────────────────────────────────────────┤
│  Assets                        │  Caption                                   │
│  ┌────┐┌────┐┌────┐ [+ añadir] │   [ ES | EN ]  (tabs; EN solo si marca export)│
│  │ 1  ││ 2  ││ 3  │            │   ┌───────────────────────────────────────┐│
│  └────┘└────┘└────┘            │   │ Elegir el fuste correcto marca la      ││
│  arrastrar para reordenar      │   │ diferencia entre… 2190/2200            ││
│  [🎲 Regenerar img seleccionada]│   └───────────────────────────────────────┘│
│  [✎ Editar con IA] [🗑]         │   Hashtags:  #EHPSaddles #monturas #…       ││
│                                │   [+ sugerir hashtags]  [primer comentario☑]│
│                                │                                            │
│  Vista previa IG  |  Vista FB   │   Programar: [📅 mié 20/08 10:00]  zona VE  │
├───────────────────────────────┴───────────────────────────────────────────┤
│         [Guardar]        [Enviar a aprobación →]        [Descartar]           │
└───────────────────────────────────────────────────────────────────────────┘
```

Detalles:
- **Contador de caracteres** por plataforma (IG caption ~2.200; FB mayor). Aviso si excede.
- **Tabs ES/EN**: en EHP Saddles ambas obligatorias; botón *“traducir con IA”* rellena EN.
- **Editar con IA** abre un mini-modal: instrucción libre (“quita el fondo”, “ponlo sobre
  pasto con luz de tarde”) → regenera ese asset.

### 3.5 Aprobaciones (cola)

```
┌───────────────────────────────────────────────────────────────────────────┐
│  Aprobaciones (3 pendientes)                    [Marca ▾ Todas]             │
├───────────────────────────────────────────────────────────────────────────┤
│  ┌─────────────────────────────────────────────────────────────────────┐  │
│  │ [thumb] EHP Saddles · Carrusel · programado mié 10:00 · IG+FB        │  │
│  │  “Elegir el fuste correcto…”   por: marketing · hace 2 h             │  │
│  │  [ 👁 Revisar ]   [ ✔ Aprobar ]   [ ✖ Rechazar (con nota) ]          │  │
│  └─────────────────────────────────────────────────────────────────────┘  │
│  ┌─────────────────────────────────────────────────────────────────────┐  │
│  │ [thumb] Plásticos · Imagen · sin programar · IG                      │  │
│  │  ...                                                                  │  │
│  └─────────────────────────────────────────────────────────────────────┘  │
└───────────────────────────────────────────────────────────────────────────┘
```

**Revisar** abre la vista previa fiel (IG y FB) a pantalla grande con caption ES/EN,
hashtags y assets a tamaño real. Aprobar registra usuario+fecha; rechazar pide nota y
regresa el post a *Borrador*.

### 3.6 Calendario editorial

Vista mensual; cada día muestra chips de posts (color por marca, ícono por plataforma).
Arrastrar un chip cambia la fecha programada. Clic abre el post. Filtros por marca/estado.

### 3.7 Biblioteca

Tres pestañas:
- **Assets**: grilla de imágenes/videos generados y subidos, filtrable por marca/pilar/fecha;
  reutilizar en un post nuevo.
- **Plantillas de prompt**: prompts que funcionaron, guardados por pilar/formato; “usar”.
- **Publicados**: histórico con enlace al post real (cuando haya Meta API) y botón
  “duplicar para adaptar” (p. ej., sacar la versión EN o repostear estacional).

### 3.8 Configuración (Admin)

- **Marcas**: nombre, handle, idiomas, guía de voz/tono (texto), paleta (colores), logo,
  hashtags base, pilares de contenido.
- **Cuentas de red**: por marca, IG y FB — estado de conexión, IDs, “Conectar / Reconectar”
  (OAuth Meta en Fase 2), o marca “publicación asistida” en Fase 1.
- **Servicios de IA**: qué motor usar por función (texto/imagen/video), orden de fallback;
  las claves se guardan como variables de entorno (la UI solo muestra “configurada ✔ / falta”).

---

## 4. Componentes reutilizables (nuevos)

| Componente | Uso |
|---|---|
| `MarcaSelector` | Barra superior; fija el contexto de marca en todo el módulo |
| `PostPreviewCard` | Render fiel de un post como se ve en IG y en FB (reutilizado en crear/editar/aprobar) |
| `AssetGrid` | Grilla de assets con reordenar (drag), seleccionar, regenerar |
| `PromptBox` | Campo de prompt con “sugerir/regenerar” y guardar como plantilla |
| `EstadoBadge` | Chip de estado (Borrador/En revisión/Aprobado/Programado/Publicado) con color |
| `CaptionEditor` | Editor con tabs de idioma, contador por plataforma, sugerir hashtags |
| `MiniCalendario` / `CalendarioMes` | Programación y vista editorial |

Todos siguiendo el sistema visual del portal (mismos tokens de color/tipografía que
`UITheme`/CSS actual).

---

## 5. Estados de UI y feedback

- **Generando**: skeleton + spinner en la preview; el post existe en estado `generando`.
- **Listo**: la preview se completa; toast “Post generado”.
- **Error de IA**: banner no bloqueante “No se pudo generar (motor X); reintentar / cambiar
  motor”. El borrador se conserva.
- **Cuota agotada**: aviso claro “Se agotó la cuota gratis de hoy del motor X; se usó el
  respaldo Y” (transparencia sobre el fallback).
- **Aprobado**: el post sale de la cola; si está programado, aparece en el calendario.
- **Publicado (Fase 2)**: chip verde + enlace al post real.

---

## 6. Accesibilidad y responsive

- Funciona en desktop (uso principal) y de forma utilizable en tablet/móvil (aprobar sobre
  la marcha desde el teléfono es un caso real para el dueño).
- Contraste según el tema del portal; foco visible; navegación por teclado en la cola de
  aprobación (aprobar/rechazar con atajos).

---

## 7. Micro-copys clave (español neutro)

- Botón principal del generador: **“Generar post”** / al regenerar: **“Regenerar variante”**.
- Aprobación: **“Aprobar”** / **“Rechazar”** (con campo *“¿Qué ajustar?”*).
- Publicación asistida (Fase 1): **“Descargar paquete para publicar”** + **“Ya lo publiqué”**.
- Vacío del banco de ideas: *“Todavía no hay ideas. Genera algunas con IA o crea una a mano.”*
