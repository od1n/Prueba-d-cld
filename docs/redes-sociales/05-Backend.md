# Esquema de backend — Estudio de Contenido

**Versión:** 1.0 · **Fecha:** 2026-08-16 · **Estado:** Borrador
**Referencias:** [TRD](02-TRD.md) · [App Flow](04-AppFlow.md)

Diseñado para encajar en el sistema actual: SQLite única en OLIMPO, migración incremental
`MigrarEsquema` con `PRAGMA user_version`, acceso por SQL directo (`Db.Conn()` en la API),
columnas en PascalCase, endpoints Minimal API como métodos de extensión con
`RequireRoleFilter`. **Nueva versión de esquema: v30.**

---

## 1. Modelo entidad-relación (resumen)

```
Marcas 1───n CuentasRed
  │ 1
  ├───n IdeasContenido 1───n Posts
  │ 1                         │ 1
  ├───n PromptsPlantilla      ├───n PostAssets
  │ 1                         └───n Publicaciones (una por plataforma/intento)
  └───n ContenidoConfig (por marca o global)
Usuarios (existente) ───► Posts.CreadoPor / Posts.AprobadoPor  (auditoría)
```

---

## 2. Tablas (DDL SQLite, migración v30)

> Todas idempotentes (`CREATE TABLE IF NOT EXISTS`), coherentes con los helpers existentes
> (`TablaExiste`, `ColumnaExiste`, `AlterTableSafe`). Fechas en texto ISO-8601
> (`YYYY-MM-DD HH:MM:SS`, hora local America/Caracas, igual que el resto del sistema).

```sql
-- v30.1 · Marcas
CREATE TABLE IF NOT EXISTS Marcas (
    Id            INTEGER PRIMARY KEY AUTOINCREMENT,
    Nombre        TEXT    NOT NULL,
    Handle        TEXT,                      -- p.ej. "ehpsaddles"
    Idiomas       TEXT    NOT NULL DEFAULT 'es',   -- csv: "es" | "es,en"
    VozTono       TEXT,                      -- guía de voz (texto largo, se inyecta en prompts)
    GuiaEstilo    TEXT,                      -- destilado de posts base (se actualiza)
    Paleta        TEXT,                      -- json: ["#RRGGBB", ...]
    LogoRuta      TEXT,                      -- ruta a logo para overlays
    HashtagsBase  TEXT,                      -- csv o json de hashtags recurrentes
    Pilares       TEXT,                      -- json: ["producto","detras","educativo",...]
    Activa        INTEGER NOT NULL DEFAULT 1,
    FechaCreacion TEXT    NOT NULL DEFAULT (datetime('now','localtime'))
);

-- v30.2 · Cuentas de red por marca
CREATE TABLE IF NOT EXISTS CuentasRed (
    Id            INTEGER PRIMARY KEY AUTOINCREMENT,
    MarcaId       INTEGER NOT NULL REFERENCES Marcas(Id),
    Plataforma    TEXT    NOT NULL,          -- "instagram" | "facebook" | (futuro) "tiktok"...
    NombreCuenta  TEXT,                      -- @handle o nombre de Página
    IgUserId      TEXT,                      -- id de IG Business (Fase 2)
    PageId        TEXT,                      -- id de Página FB (Fase 2)
    TokenRef      TEXT,                      -- REFERENCIA a env var/credencial n8n, NO el token
    ModoPublicacion TEXT NOT NULL DEFAULT 'asistida', -- "asistida" | "auto"
    Estado        TEXT    NOT NULL DEFAULT 'desconectada', -- "conectada" | "desconectada" | "error"
    Activa        INTEGER NOT NULL DEFAULT 1
);

-- v30.3 · Banco de ideas
CREATE TABLE IF NOT EXISTS IdeasContenido (
    Id            INTEGER PRIMARY KEY AUTOINCREMENT,
    MarcaId       INTEGER NOT NULL REFERENCES Marcas(Id),
    Titulo        TEXT    NOT NULL,
    Descripcion   TEXT,
    Pilar         TEXT,                      -- "producto" | "detras" | ...
    Origen        TEXT    NOT NULL DEFAULT 'manual', -- "manual" | "ia" | "base"
    Estado        TEXT    NOT NULL DEFAULT 'nueva',  -- "nueva" | "en_proceso" | "usada" | "descartada"
    RefBaseUrl    TEXT,                      -- si origen=base: enlace/imagen de referencia
    FechaCreacion TEXT    NOT NULL DEFAULT (datetime('now','localtime')),
    CreadoPor     INTEGER                    -- Usuarios.Id
);

-- v30.4 · Plantillas de prompt reutilizables
CREATE TABLE IF NOT EXISTS PromptsPlantilla (
    Id            INTEGER PRIMARY KEY AUTOINCREMENT,
    MarcaId       INTEGER REFERENCES Marcas(Id),   -- NULL = global
    Nombre        TEXT    NOT NULL,
    Tipo          TEXT    NOT NULL,          -- "imagen" | "caption" | "hashtags" | "video"
    Categoria     TEXT,                      -- pilar o formato
    Texto         TEXT    NOT NULL,          -- el prompt (puede tener placeholders {producto})
    Veces         INTEGER NOT NULL DEFAULT 0,-- contador de uso
    FechaCreacion TEXT    NOT NULL DEFAULT (datetime('now','localtime'))
);

-- v30.5 · Posts (contenido, agnóstico de plataforma)
CREATE TABLE IF NOT EXISTS Posts (
    Id            INTEGER PRIMARY KEY AUTOINCREMENT,
    MarcaId       INTEGER NOT NULL REFERENCES Marcas(Id),
    IdeaId        INTEGER REFERENCES IdeasContenido(Id),
    Formato       TEXT    NOT NULL DEFAULT 'imagen', -- "imagen"|"carrusel"|"reel"|"story"
    Titulo        TEXT,                      -- interno, para listar
    CaptionEs     TEXT,
    CaptionEn     TEXT,
    Hashtags      TEXT,                      -- csv o json
    PromptImagen  TEXT,                      -- prompt usado (para reproducir/guardar plantilla)
    ModeloUsado   TEXT,                      -- p.ej. "gemini-nano-banana"
    PlataformasDestino TEXT NOT NULL DEFAULT 'instagram', -- csv: "instagram,facebook"
    PrimerComentarioHashtags INTEGER NOT NULL DEFAULT 0,
    Estado        TEXT    NOT NULL DEFAULT 'borrador',
        -- "generando"|"borrador"|"en_revision"|"aprobado"|"programado"|"publicado"|"error"|"archivado"
    FechaProgramada TEXT,                    -- ISO local; NULL = sin programar
    JobId         TEXT,                      -- correlación con n8n (idempotencia)
    MensajeError  TEXT,
    CreadoPor     INTEGER,                   -- Usuarios.Id
    AprobadoPor   INTEGER,                   -- Usuarios.Id (auditoría)
    FechaAprobacion TEXT,
    FechaCreacion TEXT    NOT NULL DEFAULT (datetime('now','localtime')),
    FechaActualizacion TEXT
);

-- v30.6 · Assets de un post (imágenes/videos)
CREATE TABLE IF NOT EXISTS PostAssets (
    Id            INTEGER PRIMARY KEY AUTOINCREMENT,
    PostId        INTEGER NOT NULL REFERENCES Posts(Id) ON DELETE CASCADE,
    Orden         INTEGER NOT NULL DEFAULT 1,
    Tipo          TEXT    NOT NULL DEFAULT 'imagen', -- "imagen" | "video"
    RutaArchivo   TEXT    NOT NULL,          -- C:\SistemaGestion\contenido\{marca}\{postId}\...
    UrlPublica    TEXT,                      -- /media/contenido/{postId}/{slug} (para Meta)
    Ancho         INTEGER,
    Alto          INTEGER,
    Origen        TEXT    NOT NULL DEFAULT 'ia', -- "ia" | "subida" | "editada"
    ModeloGen     TEXT,
    PromptGen     TEXT,
    EsFinal       INTEGER NOT NULL DEFAULT 0,  -- 1 = incluido en la publicación
    FechaCreacion TEXT    NOT NULL DEFAULT (datetime('now','localtime'))
);

-- v30.7 · Publicaciones (una fila por plataforma/intento) — separa contenido de publicación
CREATE TABLE IF NOT EXISTS Publicaciones (
    Id            INTEGER PRIMARY KEY AUTOINCREMENT,
    PostId        INTEGER NOT NULL REFERENCES Posts(Id) ON DELETE CASCADE,
    CuentaRedId   INTEGER REFERENCES CuentasRed(Id),
    Plataforma    TEXT    NOT NULL,          -- "instagram" | "facebook"
    Estado        TEXT    NOT NULL DEFAULT 'pendiente', -- "pendiente"|"publicada"|"error"
    IdExterno     TEXT,                      -- MetaMediaId / post id
    Permalink     TEXT,
    RespuestaApi  TEXT,                       -- json de respuesta/error (diagnóstico)
    FechaIntento  TEXT,
    FechaPublicacion TEXT
);

-- v30.8 · Configuración del módulo (motores IA, webhooks) — solo referencias, no secretos
CREATE TABLE IF NOT EXISTS ContenidoConfig (
    Clave         TEXT PRIMARY KEY,          -- p.ej. "motor.imagen.primario"
    Valor         TEXT NOT NULL,             -- p.ej. "gemini-nano-banana" (NO claves)
    Descripcion   TEXT
);

-- Índices útiles
CREATE INDEX IF NOT EXISTS IX_Posts_Estado       ON Posts(Estado);
CREATE INDEX IF NOT EXISTS IX_Posts_Marca         ON Posts(MarcaId);
CREATE INDEX IF NOT EXISTS IX_Posts_Programada     ON Posts(FechaProgramada);
CREATE INDEX IF NOT EXISTS IX_Ideas_MarcaEstado    ON IdeasContenido(MarcaId, Estado);
CREATE INDEX IF NOT EXISTS IX_Assets_Post          ON PostAssets(PostId);
CREATE INDEX IF NOT EXISTS IX_Pub_Post             ON Publicaciones(PostId);
```

**Semilla mínima** (en la migración, si no existen filas): dos marcas
(EHP Saddles `idiomas='es,en'`, El Hato Pascher Plásticos `idiomas='es'`), pilares por
defecto, y filas de `ContenidoConfig` con motores primarios/fallback.

### Rol nuevo
En el esquema de usuarios/roles existente, agregar el rol **`Marketing`**. Los endpoints de
aprobación y config exigen `Admin` (o un flag `PuedeAprobar` en el usuario, análogo al PIN de
bot). Reutilizar el patrón de `RequireRoleFilter`.

---

## 3. Endpoints de API

Base: `/api/contenido`. Auth: JWT cookie + `RequireRoleFilter`. Roles: `M` = Marketing,
`A` = Aprobador, `X` = Admin. Los hooks usan `X-Internal-Token` (n8n), sin JWT.

### 3.1 Marcas y cuentas (config — Admin)
| Método | Ruta | Rol | Descripción |
|---|---|---|---|
| GET | `/marcas` | M/A/X | Lista de marcas |
| POST/PUT | `/marcas` `/marcas/{id}` | X | Crear/editar marca (voz, paleta, pilares, hashtags) |
| GET | `/marcas/{id}/cuentas` | M/A/X | Cuentas de red de la marca |
| POST/PUT | `/cuentas` `/cuentas/{id}` | X | Crear/editar cuenta (plataforma, IDs, modo) |
| GET | `/config` | X | Estado de motores/keys (configurada ✔ / falta) |
| PUT | `/config` | X | Setear motor primario/fallback por función |

### 3.2 Ideas
| Método | Ruta | Rol | Descripción |
|---|---|---|---|
| GET | `/ideas?marca=&estado=&pilar=` | M/A/X | Banco de ideas (filtros) |
| POST | `/ideas` | M/X | Crear idea manual |
| PUT | `/ideas/{id}` | M/X | Editar / cambiar estado (kanban) |
| POST | `/ideas/generar` | M/X | Dispara **WF1**; body: `{marcaId, pilares[], cantidad, usarBase}` → `202 {jobId}` |
| POST | `/hook/ideas` | (token) | Callback de WF1: inserta ideas generadas |

### 3.3 Posts
| Método | Ruta | Rol | Descripción |
|---|---|---|---|
| GET | `/posts?marca=&estado=&desde=&hasta=` | M/A/X | Lista/calendario |
| GET | `/posts/{id}` | M/A/X | Detalle con assets y publicaciones |
| POST | `/posts` | M/X | Crea borrador (desde idea o vacío) → `{postId}` |
| POST | `/posts/{id}/generar` | M/X | Dispara **WF2**; body: `{modo, prompt, formato, nImagenes}` → `202 {jobId}` |
| POST | `/posts/{id}/assets` | M/X | Subir foto propia (multipart) |
| PUT | `/posts/{id}` | M/X | Editar caption/hashtags/orden/formato/plataformas/fecha |
| PUT | `/posts/{id}/estado` | M/A/X | Cambiar estado (validado por transición y rol) |
| POST | `/posts/{id}/aprobar` | A/X | Aprueba (registra AprobadoPor+fecha); opcional programar |
| POST | `/posts/{id}/rechazar` | A/X | Rechaza con nota → vuelve a borrador + notifica |
| DELETE | `/posts/{id}` | M/X | Archiva (soft) |
| POST | `/hook/resultado` | (token) | Callback de WF2: guarda assets, caption, hashtags |
| POST | `/hook/publicacion` | (token) | Callback de WF3: marca publicado / error por plataforma |

### 3.4 Media pública (para Meta y descarga del paquete)
| Método | Ruta | Auth | Descripción |
|---|---|---|---|
| GET | `/media/contenido/{postId}/{slug}` | público (acotado) | Sirve el asset final; solo si el post está aprobado/programado/publicado |
| GET | `/api/contenido/posts/{id}/paquete` | M/A/X | ZIP con imágenes finales + caption + hashtags (Fase 1 asistida) |

### 3.5 Contratos con n8n
Sobre de disparo (API→n8n) y callback (n8n→API): definidos en [TRD §4.2–4.3](02-TRD.md).
Todos llevan `jobId` para idempotencia; el callback valida que exista un job pendiente con
ese `jobId` antes de aplicar cambios.

---

## 4. Reglas de negocio (validaciones en la API)

1. **Transiciones de estado** válidas (rechaza saltos inválidos):
   `borrador→en_revision→aprobado→programado→publicado`; `en_revision→borrador` (rechazo);
   `*→archivado`. `generando` lo setea solo el disparo de WF2 y lo limpia el callback.
2. **Enviar a revisión** exige: ≥1 `PostAssets.EsFinal=1`, `CaptionEs` no vacío, y `CaptionEn`
   no vacío **si** la marca incluye `en` en `Idiomas`.
3. **Aprobar** exige rol Aprobador/Admin y setea auditoría; si hay `FechaProgramada`→`programado`.
4. **Publicar (auto)** solo desde `programado`/`aprobado` y solo si la cuenta está `conectada`
   y en `ModoPublicacion='auto'`; si no, va por el flujo asistido.
5. **Media pública**: el endpoint solo entrega assets de posts en estado ≥ `aprobado`; el
   `slug` lleva componente aleatorio (no enumerable).
6. **Idempotencia**: un `jobId` ya procesado devuelve el resultado previo sin duplicar.
7. **Límite de publicación (Fase 2)**: respeta el tope de Meta por 24 h; si se excede, encola.

---

## 5. Archivos de código (dónde vive cada cosa)

| Archivo (nuevo) | Contenido |
|---|---|
| `SistemaGestion.Api/Endpoints/ContenidoEndpoints.cs` | Todos los endpoints `/api/contenido/*` y `/hook/*` |
| `SistemaGestion.Api/Endpoints/MediaContenidoEndpoints.cs` | `/media/contenido/*` (público acotado) |
| `SistemaGestion.Api/Db_Contenido.cs` (o dentro de `Db.cs` API) | Acceso SQLite del módulo |
| `DB_Contenido.cs` (WinForms) | *Solo si* se decidiera exponer algo en escritorio (no en MVP) |
| Migración v30 | En `MigrarEsquema` (API `DB_Deploy.cs` y/o WinForms `DB.cs`), idempotente |
| `wwwroot/pages/contenido/*.html` + `wwwroot/js/pages/contenido/*.js` | UI del módulo |
| `n8n-workflows/contenido-ideas.json` · `contenido-generar.json` · `contenido-publicar.json` | Workflows |

> **Nota de migración (regla del proyecto):** el `DB.cs` de WinForms es enorme (>5700 líneas).
> Como este módulo es solo web, la migración v30 puede vivir en el lado de la API
> (`DB_Deploy.cs`), evitando editar el `DB.cs` grande. Si se toca `DB.cs`, usar edición
> binaria en Python (no la herramienta Edit) y verificar conteo de llaves, según la regla
> establecida.

---

## 6. Consideraciones de datos

- **Zona horaria**: `FechaProgramada` y comparaciones de "vence ahora" en America/Caracas.
- **Respaldo**: la carpeta `C:\SistemaGestion\contenido\` (binarios) se respalda aparte del
  `.db.enc` (los binarios inflan el backup cifrado). La BD solo guarda rutas/URLs.
- **Retención**: los assets no finales (variantes descartadas) se pueden purgar tras N días
  (cron n8n opcional) para no llenar disco.
- **Migración v30 out-of-order**: seguir el patrón incremental existente; no reordenar
  migraciones previas.
