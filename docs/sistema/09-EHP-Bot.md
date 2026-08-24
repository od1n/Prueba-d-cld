# 09 — EHP Bot (Telegram) · espejo del bot de WhatsApp

`ehp-bot/` es un bot de **Telegram** que forma parte de SistemaGestion. Está reescrito como **espejo del bot de WhatsApp (n8n)**: usa los mismos endpoints `/api/wa/*` del ERP, con el mismo `X-Internal-Token`. **No tiene catálogo ni precios propios** — todo sale en vivo de la API de SistemaGestion. Se despliega aparte (Railway u otro host Python).

> Antes era un bot de ventas de EHP Saddles con catálogo fijo de fustes y un flujo de cotización; se reemplazó por el espejo del bot de WhatsApp (2026-08-23), a pedido. Los archivos viejos quedaron en `ehp-bot/_old/`.

## 1. Qué hace (mismos comandos que WhatsApp)
- `catalogo` — lista de productos con precio (inventario real del ERP).
- `buscar <texto>` — busca productos.
- `precio <codigo>` — ficha y precio de un producto.
- `estado <PIN> <CLIENTE>` — estado de cuenta del cliente (requiere **PIN de operador**).
- `abonos <PIN> <CLIENTE>` — últimos abonos (requiere PIN de operador).
- `pedido Nombre, Teléfono, productos` — registra el pedido y dispara el email.

Funcionan como texto o como `/comando`; los atajos numéricos 1–5 también.

## 2. Arquitectura
```
Telegram  ──▶  ehp-bot (Python, python-telegram-bot)  ──HTTPS──▶  SistemaGestion API
                        sg_api.py (httpx)                          /api/wa/*  (X-Internal-Token)
                                                                   → inventario, clientes, CxC, abonos, pedido→email
```
Es el mismo backend que atiende al bot de WhatsApp (que llega por Evolution API + n8n). El bot de Telegram es simplemente **otro canal** sobre los mismos endpoints.

## 3. Archivos
```
ehp-bot/
├── bot.py            Handlers de Telegram (comandos + texto libre)
├── sg_api.py         Cliente httpx de /api/wa/* (catalogo, producto, validar-pin, estado-cuenta, abonos, pedido)
├── config.py         Variables de entorno
├── requirements.txt  python-telegram-bot==21.6, httpx, python-dotenv
├── Procfile          worker: python bot.py  (Railway)
├── railway.toml
├── SETUP.md          Guía de instalación/deploy
├── .env.example      Plantilla de variables
└── _old/             Versión anterior (catálogo de fustes + Groq), conservada por referencia
```

## 4. Variables de entorno
| Variable | Para qué |
|---|---|
| `TELEGRAM_TOKEN` | Token del bot (de @BotFather) |
| `OPERATOR_CHAT_ID` | ID de Telegram del operador que recibe aviso de pedidos (opcional) |
| `SG_API_URL` | Base de la API (túnel `https://api-sg.hato-pascher.com` o IP de la LAN) |
| `SG_INTERNAL_TOKEN` | **El mismo** `SG_INTERNAL_TOKEN` de la API / n8n / digitalizador |
| `SG_TIMEOUT` | Timeout HTTP en segundos (opcional, default 12) |

## 5. Deploy
Local: `pip install -r requirements.txt` → `python bot.py`.
Railway: New Project → Deploy from GitHub repo → agregar las Variables → arranca por el `Procfile`.
Requiere que la API de SistemaGestion (OLIMPO) sea accesible desde donde corra el bot (por eso el túnel de Cloudflare si vive en la nube).

## 6. Seguridad
- El bot porta `SG_INTERNAL_TOKEN` (misma confianza que n8n). Debe guardarse en variables de entorno, nunca en el repo.
- `estado` y `abonos` exigen **PIN de operador** válido (Admin/Ventas), con bloqueo tras 5 intentos — igual que WhatsApp. La API es la que valida el PIN.

## 7. Nota de alcance
Al ser espejo del bot de WhatsApp, el bot de Telegram sirve el **inventario y los estados de cuenta del ERP** (El Hato Pascher), no el catálogo de marketing de fustes de EHP Saddles. Si en el futuro se quiere un bot público de EHP Saddles (fustes) por Telegram, se puede recuperar la lógica de `ehp-bot/_old/`.
