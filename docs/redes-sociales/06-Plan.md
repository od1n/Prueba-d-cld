# Plan de implementación — Estudio de Contenido

**Versión:** 1.0 · **Fecha:** 2026-08-16 · **Estado:** Borrador
**Referencias:** [PRD](01-PRD.md) · [TRD](02-TRD.md) · [UI/UX](03-UIUX.md) · [App Flow](04-AppFlow.md) · [Backend](05-Backend.md)

Hoja de ruta por fases. Cada fase indica **qué hago yo** (código, workflows, docs — sin tu
intervención) y **qué necesita de ti** (crear cuentas, claves, aprobar, correr el deploy en
OLIMPO/SERVO). El objetivo es llegar rápido a un MVP útil con costo $0 y luego crecer.

> Regla operativa: yo dejo todo listo y probado hasta donde pueda sin tocar tus máquinas de
> producción. El **push se hace desde CHAOS** y el **pull+publish en OLIMPO/SERVO** los
> ejecutas tú (o me confirmas para dártelos en bloque, siempre indicando la máquina).

---

## Fase 0 — Preparación (tú + yo)

**Yo:**
- Dejar estos documentos en el repo (`docs/redes-sociales/`).
- Preparar el checklist exacto de cuentas/claves y dónde va cada variable de entorno.

**Tú (una sola vez):**
- [ ] Confirmar que `@hatopascher` (IG) es cuenta **Business/Creator** y está vinculada a la
      Página de Facebook, con tu usuario como **Administrador** (ManyChat ya lo asumía).
- [ ] Crear/obtener claves **gratis**:
      - Google **Gemini** (AI Studio) → `SG_GEMINI_KEY`
      - (ya tienes) **OpenRouter** / **Groq** → reutilizar `SG_OPENROUTER_KEY` / `SG_GROQ_KEY`
- [ ] Decidir sobre **Meta Graph API** (Fase 4b): si quieres publicación automática, iniciar
      la **verificación de negocio** en Meta (tarda; conviene arrancarla ya en paralelo).
- [ ] Decir si el respaldo debe incluir la carpeta `contenido\` o va aparte.

**Criterio de salida:** claves listas como variables de entorno Machine en OLIMPO; decisión
Meta tomada (sí/no auto en Fase 4).

---

## Fase 1 — Base: BD + API + esqueleto del módulo (yo)

**Yo:**
- Migración **v30** (tablas del [Backend](05-Backend.md)) en el lado API (`DB_Deploy.cs`),
  idempotente, con semilla de las 2 marcas y pilares.
- Agregar rol **`Marketing`** al esquema de usuarios/permisos.
- `ContenidoEndpoints.cs`: CRUD de marcas, cuentas, ideas y posts + estados (sin IA todavía).
- `MediaContenidoEndpoints.cs`: servir `/media/contenido/*` (acotado a posts aprobados).
- Esqueleto del módulo en el portal: entrada de menú **Redes → Estudio de Contenido**,
  páginas vacías con el layout ([UI/UX](03-UIUX.md)) y el `MarcaSelector`.
- Tests xUnit de los endpoints CRUD y de las transiciones de estado.

**Tú:**
- [ ] `git pull` + publish en **OLIMPO** (WinForms self-contained + API) y **SERVO** (API),
      según el flujo estándar. Yo te entrego los 3 bloques de comandos (CHAOS/OLIMPO/SERVO).
- [ ] Verificar que el menú aparece y puedes crear una marca/idea manual.

**Criterio de salida:** puedo crear marcas, cuentas (modo asistido), ideas y borradores desde
el portal; los estados cambian correctamente; nada de esto toca módulos existentes.

---

## Fase 2 — Generación con IA (yo, con tus claves)

**Yo:**
- Workflows n8n **WF1 (ideas)** y **WF2 (generar post)** con fallback entre modelos
  (mismo patrón del bot). Motor de imagen primario: Gemini/Nano Banana (gratis).
- Rama de **edición de foto real** en WF2 (subir foto → poner en escena / quitar fondo).
- Overlay de marca opcional (HTML→imagen en n8n) con logo/paleta.
- Endpoints `/ideas/generar`, `/posts/{id}/generar` y los `/hook/*` de callback.
- Estado `generando` + polling en la UI.

**Tú:**
- [ ] Cargar las claves en n8n (o confirmarme para dejar la config de credenciales).
- [ ] `pull`+publish del backend actualizado (te doy los bloques).
- [ ] Probar: generar 5 ideas y generar 1 post; decirme si la voz/estilo pega con la marca
      (ajusto el prompt/guía de estilo con tu feedback).

**Criterio de salida:** desde una idea sale un borrador con imagen + caption (ES, y ES+EN en
EHP Saddles) + hashtags, en un par de minutos, con costo $0.

---

## Fase 3 — Editor + aprobación (yo)

**Yo:**
- Editor de post completo ([UI/UX §3.4](03-UIUX.md)): assets (reordenar, regenerar, editar),
  caption ES/EN con contador, hashtags, formato, plataformas, programación.
- `PostPreviewCard` fiel a IG y FB.
- Cola de **Aprobaciones** con revisar/aprobar/rechazar + auditoría + notificación (Telegram/
  email reutilizando lo existente).

**Tú:**
- [ ] `pull`+publish.
- [ ] Recorrer el flujo idea→borrador→revisión→aprobado y confirmar que el checkpoint te sirve.

**Criterio de salida:** cumplimos los **criterios de aceptación del MVP** del PRD §9
(publicación aún asistida).

---

## Fase 4 — Publicación

### 4a — Asistida (yo; costo $0, sin trámite)
**Yo:**
- Endpoint `/posts/{id}/paquete` (ZIP: imágenes finales + caption + hashtags).
- WF3 en modo asistido: a la hora programada, Telegram/email con el paquete + botones
  “Descargar” / “Ya lo publiqué”.
- Botón “Marcar como publicado” en el portal.

**Tú:**
- [ ] Publicar manual en IG/FB con el paquete y marcar como publicado (hasta tener 4b).

### 4b — Automática vía Meta Graph API (yo; requiere tu trámite)
**Yo:**
- WF3 completo: crear contenedor + publicar (imagen/carrusel/reel) en IG y foto/feed en FB;
  callback `/hook/publicacion` con permalink; log por plataforma en `Publicaciones`.
- Manejo de errores/reintentos y respeto del tope de 24 h.

**Tú:**
- [ ] Completar **verificación de negocio** en Meta y la **revisión de app** con permisos
      `instagram_content_publish` / `pages_manage_posts` (esto lo aprueba Meta, no yo).
- [ ] Conectar las cuentas (OAuth) y guardar el token de larga duración como credencial n8n.

**Criterio de salida:** un post aprobado y programado se publica solo a la hora indicada, con
enlace al post real registrado.

---

## Fase 5 — Calendario, recurrencia, biblioteca y semilla (yo)

**Yo:**
- Calendario editorial mensual con arrastrar-para-reprogramar.
- Biblioteca de assets + plantillas de prompt + posts publicados (“duplicar para adaptar”).
- WF1 recurrente (cron semanal) para mantener el banco de ideas lleno.
- WF4 de ingesta de semilla de `@hatopascher` (manual asistida; y por API si Meta está listo).
- (Opcional) primer motor de **video** por API si decides subir al tier de bajo costo.

**Tú:**
- [ ] Definir cadencia (p. ej. “3 posts/semana por marca”) para el generador recurrente.
- [ ] Decidir si activamos video (Seedance/Wan) y su tope de gasto.

---

## Qué necesita tu intervención (resumen)

| Tema | Por qué solo tú | Fase |
|---|---|---|
| Cuentas IG Business + Página FB | Acceso de administrador | 0 |
| Claves de IA gratis (Gemini, etc.) | Se crean con tu cuenta | 0 |
| Variables de entorno en OLIMPO | Máquina de producción | 0–2 |
| `pull` + publish en OLIMPO/SERVO | Producción; push solo desde CHAOS | cada fase |
| Verificación de negocio / revisión de app Meta | Lo aprueba Meta a tu nombre | 4b |
| Aprobar el contenido | Es el checkpoint humano por diseño | continuo |
| Feedback de voz de marca / cadencia | Criterio del negocio | 2, 5 |

Todo lo demás (código, workflows, pruebas, ajustes de prompt, documentación) lo hago yo.

---

## Estimación relativa (no compromiso de fechas)

| Fase | Tamaño | Depende de ti |
|---|---|---|
| 0 Preparación | chico | sí (cuentas/claves) |
| 1 Base | mediano | deploy |
| 2 Generación IA | mediano | claves + feedback |
| 3 Editor + aprobación | mediano-grande | deploy + prueba |
| 4a Publicación asistida | chico | — |
| 4b Publicación Meta | mediano | trámite Meta |
| 5 Calendario/biblioteca/semilla | mediano | cadencia + decisión video |

---

## Siguiente paso propuesto

Arrancar **Fase 1** de inmediato (no necesita nada tuyo salvo el deploy al final): creo la
migración v30, el rol Marketing, los endpoints CRUD y el esqueleto del módulo en el portal,
con sus tests. En paralelo tú avanzas el checklist de **Fase 0** (cuentas + claves gratis).
Cuando confirmes, te entrego los bloques de comandos de deploy (CHAOS → OLIMPO → SERVO) y
seguimos con la Fase 2.
