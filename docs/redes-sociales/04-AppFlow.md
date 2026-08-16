# App Flow — Estudio de Contenido

**Versión:** 1.0 · **Fecha:** 2026-08-16 · **Estado:** Borrador
**Referencias:** [PRD](01-PRD.md) · [TRD](02-TRD.md) · [Backend](05-Backend.md)

Este documento describe los flujos de punta a punta y los workflows de n8n. Los diagramas
usan notación simple (texto) para que sean editables sin herramientas.

---

## 1. Mapa de flujos

```
   [Ideas] ──► [Crear/Generar] ──► [Editar] ──► [Aprobar] ──► [Programar] ──► [Publicar]
      ▲              │                                                            │
      │              └── n8n WF2 (imagen+caption+hashtags)                        │
      └── n8n WF1 (ideas) / WF4 (semilla @hatopascher)          n8n WF3 (Meta) ───┘
```

Estado del post a lo largo del recorrido:
`idea → generando → en_revision → aprobado → programado → publicado`
(ramas: `rechazado→borrador`, `error→borrador`, `archivado`).

---

## 2. Flujo A — Generar ideas (asistido por IA)

**Actor:** Marketing/Admin. **Disparador:** botón “Generar ideas con IA” o cron semanal.

```
Usuario (portal)                API (OLIMPO)                 n8n WF1              LLM texto
      │  elige marca+pilar+N          │                         │                    │
      │─── POST /ideas/generar ──────►│                         │                    │
      │                               │  crea Job(generando)    │                    │
      │                               │─── POST webhook/WF1 ───►│                    │
      │◄── 202 { jobId } ─────────────│                         │  arma prompt con   │
      │  (toast: “generando ideas…”)  │                         │  voz de marca +    │
      │                               │                         │  posts base ──────►│
      │                               │                         │◄── N ideas (JSON) ─│
      │                               │◄─ POST hook/resultado ──│                    │
      │                               │  inserta N IdeasContenido                    │
      │  polling / refresco           │  (estado=nueva, origen=ia)                   │
      │◄── ideas nuevas en el banco ──│                         │                    │
```

Salida: N tarjetas en la columna *Nuevas* del banco de ideas, cada una con título,
descripción corta y pilar.

---

## 3. Flujo B — Crear post desde una idea (núcleo)

**Actor:** Marketing/Admin. **Disparador:** “Generar post”.

```
Usuario (portal)                       API (OLIMPO)                 n8n WF2
   │ idea + formato + prompt/foto             │                        │
   │── POST /posts (crea borrador) ──────────►│  Post(estado=generando)│
   │◄─ { postId } ────────────────────────────│                        │
   │── POST /posts/{id}/generar ─────────────►│── POST webhook/WF2 ────►│
   │◄─ 202 (preview “generando…”) ────────────│   { marca, idea,       │
   │                                          │     prompt, formato }  │
   │                                          │                        │  paso 1: LLM → caption ES(+EN) + hashtags
   │                                          │                        │  paso 2: motor imagen → N imágenes
   │                                          │                        │           (o edición de foto subida)
   │                                          │                        │  paso 3: overlay de marca (opcional)
   │                                          │◄── POST hook/resultado ─│  { assets[], caption{es,en}, hashtags[] }
   │                                          │  guarda assets en disco,│
   │                                          │  asigna URL pública,    │
   │                                          │  Post(estado=en_revision│
   │                                          │  … o borrador editable) │
   │  polling                                 │                        │
   │◄── preview completa ─────────────────────│                        │
   │  → pasa al Editor                         │                        │
```

**Subida de foto real (caso frecuente en el taller):**
```
Usuario sube foto ─► POST /posts/{id}/assets (multipart) ─► guarda original
   └─ si “editar con IA”: /posts/{id}/generar con modo=editar_imagen
       └─ n8n WF2 (rama edición): motor imagen recibe la foto + instrucción
          (“quitar fondo”, “poner en escena”) ─► devuelve variante editada
```

---

## 4. Flujo C — Editar y enviar a aprobación

**Actor:** Marketing/Admin. Sin IA obligatoria (todo local salvo “regenerar”).

```
Editor: ajustar caption ES/EN, hashtags, orden de assets, formato, plataformas, fecha
   │
   ├─ [🎲 Regenerar imagen] ─► /posts/{id}/generar (modo=regenerar_asset, assetId)
   ├─ [traducir EN]         ─► /posts/{id}/traducir  ─► WF2 rama traducción
   ├─ [sugerir hashtags]    ─► /posts/{id}/hashtags   ─► WF2 rama hashtags
   │
   └─ [Enviar a aprobación] ─► PUT /posts/{id}/estado {en_revision}
                                 (valida: ≥1 asset final, caption no vacío,
                                  EN presente si marca export)
```

---

## 5. Flujo D — Aprobación

**Actor:** Aprobador/Admin.

```
Cola de aprobaciones ─► Revisar (preview fiel IG/FB)
   │
   ├─ [Aprobar]  ─► PUT /posts/{id}/estado {aprobado}
   │                 registra AprobadoPor + FechaAprobacion (auditoría)
   │                 ├─ si tiene fecha programada → estado=programado
   │                 └─ dispara Flujo E (publicación)
   │
   └─ [Rechazar] ─► PUT /posts/{id}/estado {borrador} + nota
                     notifica al creador (Telegram/email)
```

> Nota: si el aprobador es la misma persona que creó (taller de una persona), Aprobar es un
> único clic de confirmación. El registro de auditoría se llena igual.

---

## 6. Flujo E — Publicación

### 6.1 Fase 1 — Publicación asistida (sin Meta API, costo $0)

```
Post aprobado (+ programado)
   │
   ├─ genera “paquete de publicación”:
   │    • imágenes finales (URLs públicas / zip descargable)
   │    • caption en el idioma correcto (ES y EN si aplica)
   │    • bloque de hashtags (o primer comentario)
   │    • recordatorio de plataformas y hora
   │
   ├─ a la hora programada (cron n8n WF3-asistido):
   │    envía por Telegram/email al operador el paquete + botones
   │    [Descargar] [Marcar como publicado]
   │
   └─ operador publica manual en IG/FB ─► pulsa “Ya lo publiqué”
        └─ PUT /posts/{id}/estado {publicado} (+ marca plataformas)
```

### 6.2 Fase 2 — Publicación automática (Meta Graph API)

```
Post aprobado+programado ── a la hora (cron n8n WF3) ──► por cada plataforma/cuenta:

 Instagram (imagen/carrusel/reel):
   1) POST /{ig-user-id}/media { image_url|video_url, caption, media_type }
        (carrusel: crear cada hijo → contenedor CAROUSEL con children)
   2) POST /{ig-user-id}/media_publish { creation_id }
   3) guarda MetaMediaId + permalink

 Facebook Page:
   1) POST /{page-id}/photos { url, caption }  (o /feed para link/texto)

 Resultado ─► POST hook/resultado ─► Post(estado=publicado) + Publicaciones(log por plataforma)
 Error     ─► Publicaciones(log estado=error, respuesta) + aviso al operador (reintento manual)
```

**Precondiciones Fase 2** (ver TRD §4.4): IG Business + Página FB vinculada, app de Meta con
`instagram_content_publish` aprobada, verificación de negocio, media en URL pública
(endpoint `/media/contenido`), tokens de larga duración refrescados.

---

## 7. Flujo F — Ingesta de semilla `@hatopascher` (WF4)

```
Opción manual (Fase 1):
   Config → Importar semilla → pegar caption + subir imagen ─► IdeasContenido(origen=base)
   + acumula “guía de estilo” por marca (tono, largo, emojis, hashtags recurrentes)

Opción API (cuando exista app de Meta, cuenta propia):
   cron/n8n WF4: GET /{ig-user-id}/media?fields=caption,media_url,media_type,timestamp
     ─► normaliza ─► IdeasContenido(origen=base) + refresca guía de estilo
```

La guía de estilo destilada se inyecta como contexto en WF1 (ideas) y WF2 (generación),
para que lo nuevo suene y se vea como lo que ya publica la cuenta.

---

## 8. Workflows de n8n — detalle de nodos

### WF1 · Generar ideas
```
Webhook(/generar-ideas)
  → Function (arma system prompt: voz de marca + guía de estilo + pilar + ejemplos base)
  → LLM (Gemini/OpenRouter :free)  [con nodo de fallback a modelo 2/3]
  → Function (parse JSON a lista de ideas; valida esquema)
  → HTTP POST (API /hook/resultado, X-Internal-Token)
  → (si error) Error Trigger → HTTP POST resultado{estado:error}
```

### WF2 · Generar post (con ramas)
```
Webhook(/generar-post) con { modo }
  ├─ modo=completo:
  │    → LLM (caption ES; si export, también EN) + hashtags
  │    → Switch(formato): imagen | carrusel(loop N) | reel(fase pago)
  │    → HTTP motor imagen (Gemini/Qwen)  [fallback → Ideogram/GPT si configurado]
  │    → (opcional) HTML→imagen: overlay logo/franja/precio
  │    → HTTP POST resultado{assets, caption, hashtags}
  ├─ modo=editar_imagen:  motor imagen (foto + instrucción) → resultado{asset}
  ├─ modo=regenerar_asset: motor imagen (mismo prompt, seed nueva) → resultado{asset}
  ├─ modo=traducir:       LLM ES→EN → resultado{caption.en}
  └─ modo=hashtags:       LLM → resultado{hashtags}
```

### WF3 · Publicar
```
Cron (cada 5–10 min) → HTTP GET API /posts?estado=programado&vence=ahora
  → por post: Switch(cuenta.plataforma)
       IG: crear contenedor(es) → media_publish
       FB: photos|feed
  → HTTP POST resultado{estado, metaMediaId, permalink}
  (Fase 1: en vez de Meta, nodo Telegram/Email envía el “paquete asistido”)
```

### WF4 · Ingesta semilla (opcional/Fase 5)
```
Cron semanal (o manual) → Graph API GET /{ig-user}/media (cuenta propia)
  → Function normaliza → HTTP POST API /hook/semilla → IdeasContenido(origen=base)
```

---

## 9. Manejo de errores y reintentos (transversal)

| Situación | Comportamiento |
|---|---|
| Motor de IA sin cuota | n8n hace fallback al siguiente motor; UI avisa qué motor se usó |
| Todos los motores fallan | Post vuelve a `borrador`; banner “reintentar / cambiar motor” |
| Callback duplicado (mismo jobId) | Idempotente: no duplica assets ni ideas |
| n8n caído | Job queda `generando`; cron de reproceso o botón “reintentar” |
| Meta API error (Fase 2) | `Publicaciones` log estado=error + aviso; reintento manual; no marca publicado |
| Media no pública | Validación previa en WF3: no publica si la URL no responde 200 |

---

## 10. Diagrama de estados (post)

```
                 generar            enviar a           aprobar
   [idea] ─────► [generando] ─────► revisión ─────►  [aprobado] ──► [programado] ──► [publicado]
                    │  error           ▲  rechazar        │ (sin fecha = publicar ya / manual)
                    └──► [borrador] ◄──┘                  └──► [publicado] (Fase 2 auto / Fase 1 manual)
   cualquier estado ──► [archivado]
```
