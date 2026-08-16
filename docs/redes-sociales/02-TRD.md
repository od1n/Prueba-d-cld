# TRD — Estudio de Contenido (Requisitos técnicos)

**Versión:** 1.0 · **Fecha:** 2026-08-16 · **Estado:** Borrador
**Referencias:** [PRD](01-PRD.md) · [Backend](05-Backend.md) · [App Flow](04-AppFlow.md)

---

## 1. Principios de arquitectura

1. **Montar sobre lo que ya existe.** n8n, Evolution API, Cloudflare Tunnel, la API de
   SistemaGestion y el acceso a LLMs ya están operativos. El módulo los reutiliza.
2. **Motores de IA intercambiables.** La UI y la API nunca llaman directo a un proveedor de
   IA. n8n es la capa de abstracción: cambiar de Gemini a Ideogram, o de gratis a pago, no
   toca ni la UI ni la BD. Igual que el bot de Telegram ya hace fallback entre modelos.
3. **Generación asíncrona.** El portal dispara un trabajo y sigue; n8n genera y hace
   callback a la API cuando termina. La UI refleja estados (`generando`, `listo`, `error`).
4. **Separar contenido de publicación.** El "post" es agnóstico de plataforma; cada
   "publicación" es por plataforma/cuenta. Sumar plataformas es agregar filas, no reescribir.
5. **Aprobación humana como puerta.** Ningún contenido llega a una red sin pasar por el
   estado `Aprobado`. La publicación automática (Fase 2) se dispara *después* de esa puerta.
6. **Secretos fuera de git y fuera de la BD en claro.** Claves de API en variables de
   entorno Machine scope en OLIMPO (mismo patrón que `SG_*` actuales).

---

## 2. Vista de componentes

```
┌──────────────────────────────────────────────────────────────────────────┐
│  PORTAL WEB SistemaGestion (SPA, HTML+JS vanilla, wwwroot)                  │
│  Módulo "Estudio de Contenido": banco de ideas, generador, editor,         │
│  cola de aprobación, calendario, biblioteca, config                        │
└───────────────┬────────────────────────────────────────────────────────────┘
                │ HTTPS (JWT cookie, roles)  — mismo origen que el portal
                ▼
┌──────────────────────────────────────────────────────────────────────────┐
│  API SistemaGestion (ASP.NET 8 Minimal API, OLIMPO / SERVO)                 │
│  Endpoints /api/contenido/*  (auth JWT + RequireRoleFilter)                 │
│  Endpoints /api/contenido/hook/*  (auth X-Internal-Token, para n8n)         │
│  /media/contenido/{...}  (público, para que Meta lea la imagen/video)       │
│  Persistencia: SQLite  C:\SistemaGestion\sistema_gestion.db                 │
└───────┬───────────────────────────────────────┬────────────────────────────┘
        │ dispara trabajo (webhook saliente)     │ callback (resultado)
        ▼                                        ▲
┌──────────────────────────────────────────────────────────────────────────┐
│  n8n (VPS EasyPanel) — orquestador de IA                                    │
│  WF1 Generar ideas · WF2 Generar post · WF3 Publicar · WF4 Ingesta semilla │
│  Llega a la API de OLIMPO por Cloudflare Tunnel (api-sg.hato-pascher.com)   │
└───┬───────────────┬───────────────┬───────────────┬────────────────────────┘
    ▼               ▼               ▼               ▼
 LLM texto      Imagen IA        Video IA        Meta Graph API
 (ideas,        (Gemini/Nano,    (Seedance/Wan,  (IG/FB publish,
  captions,      Qwen, Ideogram,  Kling — fase    Fase 2)
  hashtags)      GPT Image)       de pago)
```

### ¿Por qué n8n en el medio y no llamadas directas desde C#?

- n8n ya está montado y el equipo ya lo conoce (bot de WhatsApp).
- Permite cambiar proveedores de IA visualmente, sin recompilar ni redeployar la app .NET
  en OLIMPO (que es un proceso con pasos delicados: publish self-contained, limpiar obj/bin).
- Reintentos, fallback entre modelos y rate limiting quedan en n8n, no en el ERP.
- La API de C# se mantiene simple: CRUD + disparar/recibir trabajos.

---

## 3. Stack de IA — Fase 1 (gratis) y opción de bajo costo

### 3.1 Fase 1 — costo $0

| Función | Servicio (gratis) | Notas |
|---|---|---|
| Ideas, captions, hashtags, traducción ES↔EN | **Google Gemini** (API free tier) o **OpenRouter** modelos `:free` / **Groq** | Ya usan OpenRouter/Groq en el bot; reutilizar clave |
| Imagen (texto→imagen y edición) | **Gemini 2.x "Nano Banana"** (free tier en Google AI Studio) | Bueno para poner el producto en escena y editar fotos reales |
| Imagen alternativa / respaldo | **Qwen-Image** (Qwen free) | Reparo de privacidad: solo prompts creativos, no datos internos |
| Overlay de marca (logo, franjas, precio) | Composición programática **HTML→imagen** (Playwright/`node-html-to-image`) en n8n | Consistencia de marca sin costo por imagen |
| Orquestación y programación | **n8n** (ya lo tienen) | — |
| Hosting de media público | **API OLIMPO + Cloudflare Tunnel** (ya montado) | Sirve `/media/contenido/{id}` para que Meta lo lea |
| Publicación | **Asistida** (paquete + aviso Telegram/email) | Sin costo ni trámite de Meta |

> Los tiers gratis tienen cuotas diarias. n8n aplica el mismo patrón de **fallback** que el
> bot: si el motor primario responde error/cuota, pasa al siguiente. Los límites reales de
> cuota se confirman en Fase 0 (cambian seguido); el diseño no depende de un número fijo.

### 3.2 Opción de bajo costo (~$20–50/mes) — presentada como upgrade

| Función | Servicio | Costo aprox. | Por qué |
|---|---|---|---|
| Imagen con texto perfecto (promos, precios) | **Ideogram 3** | ~$0.03–0.10/img o $15/mes plan | El mejor renderizando texto dentro del gráfico |
| Imagen fotorealista / edición fina | **GPT Image 2** | por uso (API) | Máxima fidelidad de prompt y fotorealismo |
| Video corto (reels) | **Seedance 2** (barato) o **Wan** (rápido) | ~$0.022–0.07/seg | Reels con IA sin GPU local |
| Publicación automática | **Meta Graph API** | $0 de API, pero trámite | Requiere verificación de negocio + revisión de app |
| Alternativa a Meta API | **Metricool / Publer** (scheduler) | free–$9/mes | Evita el trámite de Meta; programa desde un panel externo |

**Recomendación de gasto**: si se sube de tier, el primer dólar rinde más en **Ideogram**
(gráficos promocionales con precio/texto legible) que en video. El video se agrega cuando
haya ritmo de publicación estable.

### 3.3 Sobre generación local (el reel de ComfyUI + Wan)

Es viable técnicamente pero requiere GPU de 16–24 GB VRAM dedicada, que hoy no está en
OLIMPO/SERVO/CHAOS confirmada. **No entra en el diseño de Fase 1.** Si en el futuro hay una
máquina con GPU, se añade como "un motor más" detrás de n8n (un endpoint ComfyUI local),
sin cambios en la UI ni la BD — exactamente por el principio de motores intercambiables.

---

## 4. Integraciones y contratos

### 4.1 Portal → API (mismo origen, JWT)
Endpoints REST bajo `/api/contenido/*`. Auth por cookie JWT + `RequireRoleFilter`
(roles Admin / Marketing / Aprobador). Ver [Backend](05-Backend.md) §Endpoints.

### 4.2 API → n8n (disparar trabajo)
La API hace `POST` al **webhook de n8n** (URL en env var `SG_N8N_WEBHOOK_BASE`) con un
sobre firmado:

```jsonc
// POST {SG_N8N_WEBHOOK_BASE}/generar-post
{
  "jobId": "uuid",             // idempotencia y correlación
  "tipo": "generar_post",       // generar_ideas | generar_post | publicar
  "marca": { "id": 1, "handle": "ehpsaddles", "idiomas": ["es","en"], "voz": "...", "hashtagsBase": ["#..."] },
  "idea": { "id": 12, "titulo": "...", "descripcion": "...", "pilar": "producto" },
  "prompt": { "imagen": "...", "estiloCaption": "...", "formato": "carrusel", "nImagenes": 3 },
  "callbackUrl": "https://api-sg.hato-pascher.com/api/contenido/hook/resultado",
  "callbackToken": "X-Internal-Token"
}
```

### 4.3 n8n → API (callback con resultado)
n8n devuelve, autenticado con `X-Internal-Token`:

```jsonc
// POST /api/contenido/hook/resultado
{
  "jobId": "uuid",
  "estado": "ok",              // ok | error
  "postId": 34,                 // si aplica
  "assets": [                   // imágenes/videos generados, ya subidos o en base64/URL temporal
    { "orden": 1, "tipo": "imagen", "url": "https://...", "modelo": "gemini-nano-banana", "prompt": "..." }
  ],
  "caption": { "es": "...", "en": "..." },
  "hashtags": ["#...", "#..."],
  "error": null
}
```

La API descarga/guarda los assets en disco de OLIMPO
(`C:\SistemaGestion\contenido\{marca}\{postId}\`), les asigna URL pública
`/media/contenido/{postId}/{n}` y actualiza el post a estado `En revisión`.

### 4.4 n8n → Meta Graph API (Fase 2)
Publicación en dos pasos por plataforma (patrón oficial):
1. **Crear contenedor**: `POST /{ig-user-id}/media` con `image_url` (o `video_url`, `media_type=REELS`) + `caption`. Para carrusel: crear cada hijo, luego un contenedor `CAROUSEL` con `children`.
2. **Publicar**: `POST /{ig-user-id}/media_publish` con `creation_id`.
Para Facebook Page: `POST /{page-id}/photos` o `/feed`.

**Requisitos** (confirmados 2026): cuenta **Instagram Business/Creator** vinculada a una
**Página de Facebook**; app de Meta con permisos `instagram_content_publish`,
`pages_manage_posts`, etc.; **revisión de app** aprobada para operar fuera de usuarios de
prueba; la **media debe estar en una URL pública** (de ahí el endpoint `/media/contenido`).
Límite de publicación: tope separado por 24 h (del orden de decenas de posts/día — de sobra
para un taller). Estos pasos viven en un workflow de n8n con nodos HTTP; los tokens de larga
duración se refrescan y se guardan como credenciales de n8n / env vars, no en la BD.

### 4.5 Ingesta de semilla `@hatopascher`
- **Cuenta propia** como Business → Graph API permite leer *medios propios*
  (`GET /{ig-user-id}/media?fields=caption,media_url,media_type,timestamp`). WF4 los trae y
  los guarda como `IdeasContenido origen=base` + guía de estilo.
- Mientras no esté la app de Meta, ingesta **manual asistida**: pegar caption + subir imagen;
  suficiente para arrancar el banco de ideas.

---

## 5. Hosting de media y almacenamiento

- **Archivos**: en disco de OLIMPO bajo `C:\SistemaGestion\contenido\{marcaId}\{postId}\`
  (imágenes finales, variantes, video). Respaldados por el mismo proceso de backup si se
  incluye la carpeta (decisión de Fase 1: incluir o no en el `.db.enc` — los binarios pesan;
  se recomienda respaldo aparte de la carpeta `contenido`).
- **URL pública para Meta**: endpoint `GET /media/contenido/{postId}/{asset}` servido por la
  API y expuesto por el Cloudflare Tunnel existente (`api-sg.hato-pascher.com`). Solo sirve
  assets de posts en estado `Aprobado`/`Programado`/`Publicado` (no expone borradores).
- **Alternativa**: Cloudflare **R2** (bucket, free tier 10 GB) si se prefiere no servir
  binarios desde OLIMPO. El diseño soporta ambas vía la columna `UrlPublica`.

---

## 6. Seguridad

- **Auth de usuario**: reutiliza JWT en cookie HttpOnly del portal. Nuevos endpoints con
  `RequireRoleFilter`: lectura para Marketing/Aprobador/Admin; escritura de config solo Admin;
  aprobación solo Aprobador/Admin.
- **Auth n8n↔API**: header `X-Internal-Token` (misma variable `SG_INTERNAL_TOKEN` que ya usan
  el digitalizador y el bot de WhatsApp), sin JWT. El endpoint de callback valida `jobId`
  contra un trabajo pendiente (evita callbacks falsos).
- **Claves de IA / tokens de Meta**: en variables de entorno Machine scope en OLIMPO
  (`SG_GEMINI_KEY`, `SG_OPENROUTER_KEY`, `SG_META_TOKEN`, …) o como credenciales de n8n.
  Nunca en la BD en claro ni en archivos trackeados por git. La BD guarda solo *referencias*
  (qué servicio, qué modelo), no el secreto.
- **Media pública**: solo assets de posts aprobados; nombres con componente aleatorio
  (no adivinables) para evitar enumeración; sin datos sensibles en imágenes.
- **Filtro LAN existente**: las llamadas de n8n entran por el túnel como localhost y pasan
  `EsLan()`; el endpoint `/media/contenido` debe permitir origen público (excepción explícita
  y acotada a ese path, como ya se hace para el caso del túnel).
- **Rate limiting**: en n8n para las APIs de IA; en la API para los hooks (reusar el patrón de
  rate limiting del login/bot).

---

## 7. Rendimiento y resiliencia

- Generación **asíncrona** con `jobId`; timeouts y reintentos en n8n; fallback entre modelos.
- La UI hace *polling* corto (o SSE si se quiere después) al endpoint de estado del post.
- Si n8n cae, los trabajos quedan en estado `generando`; un reintento manual o un cron de
  n8n reprocesa pendientes. La API nunca bloquea esperando a la IA.
- Idempotencia por `jobId`: un callback repetido no duplica assets.

---

## 8. Tecnologías (alineadas al stack actual)

| Capa | Tecnología | Ya en uso |
|---|---|---|
| UI módulo | HTML + JS vanilla (SPA con router propio del portal) | Sí |
| API | ASP.NET 8 Minimal API, endpoints como métodos de extensión | Sí |
| BD | SQLite (`Db.Conn()`, SQL directo), migración incremental | Sí (v29→v30) |
| Orquestación IA | n8n | Sí |
| Túnel público | Cloudflare Tunnel | Sí |
| Imagen/overlay | APIs de IA + HTML→imagen (Playwright en n8n) | Nuevo |
| Notificación | Telegram (bot existente) / SMTP Gmail (ya configurado) | Sí |

---

## 9. Impacto en el sistema actual

- **BD**: nueva migración **v30** con tablas `Marcas`, `CuentasRed`, `IdeasContenido`,
  `PromptsPlantilla`, `Posts`, `PostAssets`, `Publicaciones`, `ContenidoConfig`. Idempotente,
  con `CREATE TABLE IF NOT EXISTS`, siguiendo el patrón `MigrarEsquema`.
- **API**: nuevo archivo `ContenidoEndpoints.cs` (+ `Db` helpers). No toca endpoints existentes.
- **Portal**: nuevas páginas bajo `wwwroot/pages/contenido/*` y `wwwroot/js/pages/contenido/*`;
  nueva entrada de menú y permiso de rol. No toca módulos existentes.
- **WinForms**: **sin cambios** — este módulo es solo web (coherente con que SERVO/portal es
  el canal multi-equipo).
- **Roles**: agregar rol `Marketing` al esquema de usuarios/permisos.
- **Deploy**: mismo flujo (push CHAOS → pull+publish OLIMPO y SERVO). La API se publica
  self-contained como siempre; el módulo web viaja en `wwwroot`.
