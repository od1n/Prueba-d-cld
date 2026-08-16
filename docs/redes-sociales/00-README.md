# Estudio de Contenido — Documentación de diseño

Módulo nuevo dentro del portal web **SistemaGestion** para diseñar, editar, crear y
programar contenido de redes sociales (Instagram y Facebook al inicio, extensible a
otras plataformas) para las dos marcas del negocio, con generación asistida por IA y
aprobación humana antes de publicar.

## Decisiones que enmarcan este diseño (confirmadas por Jacques)

| Decisión | Elección |
|---|---|
| Marcas / mercado | **Ambas**: EHP Saddles (export, ES + EN) y El Hato Pascher Plásticos (local VE, ES) |
| Publicación | **Aprobación humana** antes de publicar — n8n genera, el portal aprueba |
| Presupuesto IA | **Arranque con herramientas gratis**; se documenta también la opción de bajo costo (~$20–50/mes) |
| Dónde vive | **Módulo nuevo en el portal SistemaGestion**, reutilizando auth/roles; n8n hace el trabajo pesado detrás |

## Documentos

| # | Documento | Qué responde |
|---|---|---|
| 1 | [PRD](01-PRD.md) | Qué se construye y por qué: visión, usuarios, alcance, requisitos funcionales, métricas, fases |
| 2 | [TRD](02-TRD.md) | Cómo se construye: arquitectura, stack (gratis y bajo costo), integraciones, seguridad, hosting de media |
| 3 | [Diseño UI/UX](03-UIUX.md) | Pantallas, componentes, wireframes, alineación con el portal actual |
| 4 | [App Flow](04-AppFlow.md) | Flujos end-to-end y workflows de n8n paso a paso |
| 5 | [Esquema de backend](05-Backend.md) | Tablas SQLite (migración v30), endpoints de API, contratos con n8n |
| 6 | [Plan de implementación](06-Plan.md) | Hoja de ruta por fases; qué hago yo vs qué necesita tu intervención |

## Nombre interno del módulo

**Estudio de Contenido** (menú del portal: *Redes → Estudio de Contenido*).

## Resumen de la evaluación previa (contexto)

- El sistema **ya tiene n8n** (VPS EasyPanel), Evolution API, Cloudflare Tunnel
  (`api-sg.hato-pascher.com`), API de SistemaGestion en OLIMPO y acceso a varios LLMs
  (OpenRouter, Groq, Anthropic, Ollama). El módulo se monta encima de esto, no reinventa
  infraestructura.
- **Reels analizados**: el de Qwen es real pero con titular exagerado (útil como opción
  gratis de borrador, con reparo de privacidad por servidores en China); el de generación
  local de video (ComfyUI + Wan) es cierto pero exige GPU cara (16–24 GB VRAM); el tercer
  reel (`raymelian_`) quedó restringido por login y no se pudo verificar.
- **Mejores herramientas 2026** para lo que queremos: imágenes → Gemini "Nano Banana"
  (gratis), Ideogram 3 (texto perfecto), GPT Image 2 (fotorealismo), FLUX.2 (self-host);
  video → Seedance/Wan/Kling/Veo (cloud de pago); orquestación → n8n; publicación →
  Meta Graph API o scheduler externo.
