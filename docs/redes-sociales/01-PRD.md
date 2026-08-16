# PRD — Estudio de Contenido (SistemaGestion)

**Versión:** 1.0 · **Fecha:** 2026-08-16 · **Autor:** Claude (para Jacques / EL HATO PASCHER C.A.)
**Estado:** Borrador para aprobación

---

## 1. Visión

Dar a EL HATO PASCHER una fábrica de contenido semi-automática: un lugar único dentro del
portal que ya usan a diario donde nacen ideas, se convierten en posts (imagen/carrusel/reel
+ caption + hashtags) generados con IA a partir del estilo de sus posts actuales de
`@hatopascher`, y se aprueban antes de salir a Instagram y Facebook. El objetivo no es
"publicar solo con IA", sino **eliminar el trabajo repetitivo** (redactar, diseñar,
adaptar por idioma, programar) y dejar a la persona la parte de criterio: aprobar, ajustar y
mantener la voz de marca.

### El problema hoy

Un taller artesanal no tiene equipo de marketing. Publicar de forma constante compite con la
producción. El resultado típico: publicaciones esporádicas, sin calendario, con esfuerzo
manual alto cada vez (pensar el tema, escribir, diseñar en Canva, adaptar a inglés para el
público export, subir a IG y a FB por separado). El contenido de `@hatopascher` demuestra
que hay material y estilo; falta un motor que lo sostenga en el tiempo.

### La solución

Un módulo **Estudio de Contenido** que:

1. Mantiene un **banco de ideas** alimentado manualmente y por IA (a partir de los posts
   base de `@hatopascher` y de generadores de ideas por "pilar de contenido").
2. Convierte una idea en un **borrador de post** completo: imagen/carrusel generado o
   editado por IA, caption en el idioma de la marca (ES, y ES+EN para export), hashtags
   sugeridos y formato destino.
3. Pasa cada borrador por una **cola de aprobación** (revisión humana obligatoria).
4. Una vez aprobado, lo **programa y publica** en IG/FB (manual asistido al inicio;
   automático vía Meta Graph API en una fase posterior).
5. Guarda todo en una **biblioteca** reutilizable (assets, prompts que funcionaron, posts
   publicados) para acelerar el siguiente ciclo.

---

## 2. Objetivos y métricas de éxito

| Objetivo | Métrica | Meta inicial |
|---|---|---|
| Publicar de forma constante | Posts publicados por semana por marca | ≥ 3 |
| Reducir el esfuerzo por post | Minutos de persona por post (idea→publicado) | ≤ 10 min |
| Mantener calidad/voz de marca | % de borradores aprobados sin re-generar | ≥ 60 % |
| Costo controlado | Gasto mensual en APIs de IA | $0 en Fase 1; ≤ $50 en opción de bajo costo |
| Cobertura bilingüe (export) | % de posts de EHP Saddles con caption ES+EN | 100 % |

### No-objetivos (fuera de alcance, al menos por ahora)

- No es una herramienta de **analítica avanzada** de redes (seguidores, alcance por hora,
  A/B testing). Se puede integrar después.
- No gestiona **pauta pagada** (Meta Ads).
- No responde **DMs ni comentarios** — eso ya lo cubren ManyChat + Make + el bot de
  Telegram existentes. Este módulo es sobre contenido *saliente*.
- No genera **video local con GPU** (ComfyUI + Wan) en Fase 1: el hardware no está
  disponible; el video llega vía API cloud cuando haya presupuesto.

---

## 3. Usuarios y roles

El portal ya tiene roles (Admin, Contabilidad, Ventas, Almacén). Se añade un rol nuevo:

| Rol | Puede | Descripción |
|---|---|---|
| **Marketing** (nuevo) | Crear ideas, generar y editar borradores, programar | Community manager / encargado de redes. No aprueba lo suyo por defecto (configurable). |
| **Aprobador** (Admin, o marca en el usuario) | Aprobar / rechazar borradores, publicar | Típicamente Jacques o un dueño. Control de calidad y voz de marca. |
| **Admin** | Todo, más configurar marcas, cuentas de red, claves de API | Ya existe. |

> En un taller chico probablemente sea **una sola persona** con rol Admin haciendo todo. El
> flujo de aprobación sigue existiendo como "checkpoint" incluso si el aprobador es el mismo
> que generó (un clic de confirmación), porque es lo que evita que salga un post con un
> error de la IA.

### Personas

- **Jacques (dueño/dev, Admin)**: configura el sistema, define marcas y voz, aprueba lo que
  sale. Quiere que "casi todo se haga solo" y solo intervenir para el visto bueno.
- **Encargado de redes (rol Marketing)**: entra, ve ideas propuestas, genera 3–4 posts de
  la semana en minutos, los deja en cola. Puede ser la misma persona que aprueba.

---

## 4. Modelo de contenido

### 4.1 Marcas

El sistema modela **dos marcas** desde el día uno, porque `@hatopascher` mezcla ambos mundos:

| Marca | Mercado | Idiomas | Voz / tono | Tipo de producto |
|---|---|---|---|---|
| **EHP Saddles** | Internacional (export) | Español **+ Inglés** | Artesanal, técnico, orgullo "hecho en Venezuela desde 1983", confianza | Árboles de montura en polietileno de alta densidad |
| **El Hato Pascher Plásticos** | Local (Venezuela) | Español | Cercano, práctico, industrial, precio/servicio | Productos plásticos de la fábrica |

Cada marca tiene: handle, idioma(s), guía de voz/tono, paleta de colores, logo, hashtags
base y cuentas de red asociadas (IG y FB). El generador usa estos datos para que el
contenido salga "en marca" sin reescribir el prompt cada vez.

### 4.2 Pilares de contenido

Plantillas de tema que alimentan el banco de ideas (configurables por marca). Ejemplos:

- **Producto / catálogo**: modelo destacado, medidas, usos.
- **Detrás de cámara**: proceso de fabricación, taller, gente.
- **Educativo / tips**: cómo elegir, cuidados, comparativas.
- **Testimonio / caso**: cliente, jinete, uso real.
- **Historia / marca**: "desde 1983", valores, hitos.
- **Estacional / efeméride**: fechas, ferias equinas, temporadas.

### 4.3 Estados de un post

```
Idea → Borrador (generado) → En revisión → Aprobado → Programado → Publicado
                                   ↓ (rechazado)
                              Vuelve a Borrador / Archivado
```

### 4.4 Uso de los posts base de `@hatopascher`

Los posts actuales de la cuenta sirven como **referencia de estilo** de dos maneras:

1. **Semilla de ideas**: se importan (manual o vía captura) sus captions/temas para que el
   generador de ideas proponga variaciones y continúe líneas que ya funcionaron.
2. **Guía visual/voz**: se destilan a una "guía de estilo" por marca (tono, largo de
   caption, uso de emojis, hashtags recurrentes) que se inyecta en cada prompt.

> Nota de factibilidad: Instagram no permite exportar el feed de terceros por API sin
> permisos. Para la **cuenta propia** `@hatopascher`, una vez conectada como cuenta Business,
> sí se pueden leer los medios propios vía Graph API. Al inicio se cargan como semilla de
> forma manual/semi-asistida (copiar caption + imagen), que es suficiente para arrancar.

---

## 5. Requisitos funcionales

Prioridad: **M** = must (MVP), **S** = should, **C** = could (después).

### 5.1 Banco de ideas
- **[M]** Crear idea manual (título, descripción, marca, pilar).
- **[M]** Generar ideas con IA: elegir marca + pilar + cantidad → n8n devuelve N ideas al banco.
- **[S]** Importar posts base de `@hatopascher` como semilla (caption + imagen de referencia).
- **[S]** Vista tipo tablero (kanban) por estado y por marca.

### 5.2 Generador de prompts e ideas
- **[M]** A partir de una idea, sugerir automáticamente: prompt de imagen, ángulo de caption
  y hashtags, usando la guía de voz de la marca.
- **[M]** Biblioteca de **plantillas de prompt** reutilizables (por pilar / formato).
- **[S]** "Regenerar variante" para pedir otra propuesta sin empezar de cero.

### 5.3 Generación de assets (imagen/carrusel; video en fase posterior)
- **[M]** Generar imagen desde prompt (motor gratis por defecto: Gemini/Nano Banana o Qwen).
- **[M]** Subir imagen propia (foto real del producto) y usarla tal cual o **editarla con IA**
  (quitar fondo, poner en escena, mejorar) — clave para un taller que ya tiene fotos.
- **[M]** Carrusel: varias imágenes ordenadas.
- **[S]** Overlay de texto/plantilla de marca sobre la imagen (logo, franja, precio).
- **[C]** Generar reel/video corto vía API cloud (opción de bajo costo).

### 5.4 Editor de post
- **[M]** Editar caption (con contador de caracteres por plataforma), en ES y ES+EN según marca.
- **[M]** Editar/seleccionar hashtags (sugeridos + guardados por marca).
- **[M]** Elegir formato (imagen / carrusel / reel / story) y plataformas destino (IG, FB).
- **[M]** Previsualización tipo "cómo se verá en IG/FB".
- **[S]** Primer comentario automático (bloque de hashtags como primer comentario).

### 5.5 Aprobación
- **[M]** Cola de aprobación con vista previa completa.
- **[M]** Aprobar / rechazar con comentario. Rechazo devuelve a borrador.
- **[M]** Registro de quién aprobó y cuándo (auditoría).

### 5.6 Programación y publicación
- **[M]** Programar fecha/hora (zona America/Caracas).
- **[M]** Calendario editorial por marca.
- **[M]** **Fase 1 — publicación asistida**: al aprobar, generar un "paquete de publicación"
  (imágenes finales + caption + hashtags listos para copiar/pegar) y avisar por Telegram/email.
- **[S]** **Fase 2 — publicación automática** vía Meta Graph API (crear contenedor + publicar)
  tras la aprobación, respetando el horario programado.
- **[C]** Integración con scheduler externo (Metricool/Publer) como alternativa a Meta API.

### 5.7 Biblioteca y reutilización
- **[M]** Biblioteca de assets generados/subidos (buscable por marca, pilar, fecha).
- **[S]** Guardar prompts que funcionaron como plantillas.
- **[S]** Duplicar un post publicado para adaptarlo (p. ej., versión en inglés).

### 5.8 Configuración
- **[M]** CRUD de marcas (voz, paleta, hashtags base, logo).
- **[M]** CRUD de cuentas de red por marca (IG/FB) y su estado de conexión.
- **[M]** Gestión de claves/servicios de IA (referencias, no secretos en claro — ver TRD).

---

## 6. Requisitos no funcionales

- **Idioma de la interfaz**: español neutro. Contenido generado según la marca (ES / ES+EN).
- **Seguridad**: reutiliza el auth del portal (JWT en cookie HttpOnly, roles). Las claves de
  API viven en variables de entorno Machine scope en OLIMPO, nunca en git ni en la BD en claro.
- **Costo**: Fase 1 en $0 (tiers gratis). La arquitectura debe permitir cambiar de motor de
  IA sin tocar la UI (motores intercambiables detrás de n8n).
- **Rendimiento**: la generación es asíncrona (n8n) — la UI no se bloquea; el post aparece
  como "generando…" y se actualiza al terminar.
- **Portabilidad de plataforma**: el modelo de datos separa "post" (contenido) de
  "publicación por plataforma", de modo que sumar TikTok/LinkedIn/X después no rompe nada.
- **Zona horaria**: toda programación en America/Caracas (UTC-4).

---

## 7. Suposiciones y riesgos

| Tema | Suposición / riesgo | Mitigación |
|---|---|---|
| Cuentas | `@hatopascher` (IG) y la Página FB son Business y el usuario tiene acceso admin | Verificar en Fase 0; ManyChat ya asume esta conexión |
| Meta Graph API | Publicar automático requiere app de Meta + verificación de negocio + revisión (semanas) | Fase 1 no depende de esto (publicación asistida); se tramita en paralelo para Fase 2 |
| IA gratis | Los tiers gratis tienen límites de cuota y pueden cambiar | Motores intercambiables; fallback entre modelos (ya usado en el bot) |
| Privacidad | Qwen/servicios CN reciben los prompts | No enviar datos sensibles; usar para borradores creativos, no datos internos |
| Hosting de media | Meta API exige URL pública para la imagen/video | Servir desde la API de OLIMPO vía Cloudflare Tunnel (ya montado) o R2 |
| Una sola persona | El "aprobador" y el "creador" son el mismo | El checkpoint sigue siendo útil; se permite auto-aprobar con un clic |

---

## 8. Fases (resumen; detalle en el Plan de implementación)

- **Fase 0 — Preparación**: cuentas, claves gratis, decisión sobre Meta API.
- **Fase 1 — Base**: BD (migración v30), API CRUD, esqueleto del módulo en el portal, menú.
- **Fase 2 — Generación IA**: workflows n8n de ideas + imagen + caption/hashtags.
- **Fase 3 — Editor + aprobación**: editor de post, previsualización, cola de aprobación.
- **Fase 4 — Publicación**: paquete asistido → luego Meta Graph API automático.
- **Fase 5 — Calendario, recurrencia y biblioteca**: calendario editorial, plantillas, semillas.

---

## 9. Criterios de aceptación del MVP (Fases 1–4 asistidas)

1. Puedo crear/generar una idea para una marca y verla en el banco.
2. Desde una idea, genero un borrador con imagen + caption (ES, y ES+EN para EHP Saddles) +
   hashtags, en menos de un par de minutos, sin escribir el prompt a mano.
3. Puedo editar el borrador y previsualizar cómo se verá.
4. Puedo aprobarlo; queda registrado quién y cuándo.
5. Al aprobar, recibo el paquete de publicación listo para IG y FB y un aviso por Telegram/email.
6. Todo corre con costo $0 en APIs.
