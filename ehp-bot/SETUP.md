# EHP Bot (Telegram) — Instalación y deploy

Bot de Telegram **espejo del bot de WhatsApp** de SistemaGestion. Usa los mismos
endpoints `/api/wa/*` del ERP (catálogo, producto, estado de cuenta, abonos, pedido),
con el mismo `X-Internal-Token`. No tiene catálogo ni precios propios: todo en vivo.

## Requisitos

| Cuenta | Para qué |
|---|---|
| Telegram + @BotFather | Crear el bot y obtener su token |
| SistemaGestion accesible | Por el túnel `api-sg.hato-pascher.com` (o IP en la LAN) |
| `SG_INTERNAL_TOKEN` | El mismo token interno de la API / n8n |
| Railway (o cualquier host Python) | Correr el bot 24/7 |

## Comandos del bot (iguales al de WhatsApp)
- `catalogo` — lista de productos con precio.
- `buscar <texto>` — busca productos.
- `precio <codigo>` — ficha y precio de un producto.
- `estado <PIN> <CLIENTE>` — estado de cuenta (requiere PIN de operador).
- `abonos <PIN> <CLIENTE>` — últimos abonos (requiere PIN de operador).
- `pedido Nombre, Teléfono, productos` — registra el pedido y envía el email.

Funcionan como texto o como `/comando`. Los atajos numéricos 1–5 también sirven.

## Paso 1 — Crear el bot en Telegram
1. En Telegram, abre **@BotFather** → `/newbot`.
2. Elige nombre y username (termina en `bot`).
3. Copia el **token** que te da.

## Paso 2 — (opcional) ID del operador
Abre **@userinfobot** → `/start` → copia tu ID numérico (para recibir aviso de pedidos).

## Paso 3 — Variables de entorno
Copia `.env.example` a `.env` y completa:

```
TELEGRAM_TOKEN     = (token de BotFather)
OPERATOR_CHAT_ID   = (tu ID de Telegram, opcional)
SG_API_URL         = https://api-sg.hato-pascher.com
SG_INTERNAL_TOKEN  = (el MISMO SG_INTERNAL_TOKEN de la API / n8n)
```

## Paso 4 — Correr
Local:
```bash
pip install -r requirements.txt
python bot.py
```
Railway: New Project → Deploy from GitHub repo → agregar las Variables de arriba →
Railway detecta el `Procfile` (`worker: python bot.py`) y arranca.

## Notas
- El bot llama a `/api/wa/*` con `X-Internal-Token`; si el token o la URL están mal,
  responde "No autorizado" o "No se pudo conectar con SistemaGestion".
- Los comandos `estado` y `abonos` exigen un **PIN de operador** válido (Admin/Ventas),
  con bloqueo tras 5 intentos — igual que en WhatsApp.
- Requiere que la API de SistemaGestion (OLIMPO) esté accesible desde donde corra el bot
  (por eso se usa el túnel de Cloudflare si el bot vive en la nube).
