# EHP Saddles Bot — Guía de instalación y deploy

## Requisitos previos

| Cuenta gratuita | Para qué |
|---|---|
| [Telegram](https://telegram.org) | Tener la app y acceso a BotFather |
| [Groq](https://console.groq.com) | API key gratuita para el LLM |
| [Railway](https://railway.app) | Hosting gratuito para el bot |

---

## Paso 1 — Crear el bot en Telegram

1. Abre Telegram y busca **@BotFather**
2. Envía `/newbot`
3. Elige un nombre para mostrar, por ejemplo: `EHP Saddles`
4. Elige un username (debe terminar en `bot`), por ejemplo: `EHPSaddlesBot`
5. Copia el **token** que te da BotFather — lo necesitarás más adelante
6. Opcional: personaliza el bot con `/setdescription`, `/setuserpic`, `/setabouttext`

## Paso 2 — Obtener tu ID de operador

1. Abre Telegram y busca **@userinfobot**
2. Envía `/start`
3. Te responde con tu ID numérico — por ejemplo `987654321`

## Paso 3 — Obtener API key de Groq (gratis)

1. Ve a [console.groq.com](https://console.groq.com)
2. Crea una cuenta gratuita
3. En **API Keys** → **Create API Key**
4. Copia la key (empieza con `gsk_...`)

## Paso 4 — Deploy en Railway

### Opción A — Desde GitHub (recomendado)

1. Sube la carpeta `ehp-bot/` a un repositorio GitHub
2. Ve a [railway.app](https://railway.app) → **New Project** → **Deploy from GitHub repo**
3. Selecciona el repo
4. En **Variables**, agrega:

```
TELEGRAM_TOKEN    = (el token de BotFather)
OPERATOR_CHAT_ID  = (tu ID numérico de Telegram)
GROQ_API_KEY      = (la key de Groq)
GROQ_MODEL        = llama-3.1-8b-instant
```

5. Railway detecta el `Procfile` y arranca el bot automáticamente

### Opción B — Railway CLI

```bash
npm install -g @railway/cli
railway login
railway init
railway up
railway variables set TELEGRAM_TOKEN=xxx OPERATOR_CHAT_ID=xxx GROQ_API_KEY=xxx
```

---

## Paso 5 — Actualizar el username del bot en el sitio web

Una vez registrado el username del bot, busca y reemplaza `EHPSaddlesBot` por tu username real en todos los archivos HTML:

```bash
# En la carpeta ehp web/
grep -rn "EHPSaddlesBot" . --include="*.html"
# Luego reemplazar con el username real
```

---

## Comandos del bot

| Comando | Función |
|---|---|
| `/start` | Inicia la conversación, reinicia el historial |
| `/catalogo` | Lista todos los modelos y códigos |
| `/cotizar` | Guía al usuario para pedir una cotización |
| `/reset` | Reinicia el historial de chat |

---

## Actualizar precios

Abre `catalog.py` y edita el diccionario `PRECIOS`:

```python
PRECIOS: dict[str, float | str] = {
    "EHP-W14":  85.0,    # McCleallan 14" → $85 USD
    "EHP-W12S": 75.0,    # McCleallan 12" pequeño → $75 USD
    # etc.
}
```

Haz commit y Railway vuelve a deployar automáticamente.

---

## Desarrollo local

```bash
cd ehp-bot
python -m venv .venv
source .venv/bin/activate      # Windows: .venv\Scripts\activate
pip install -r requirements.txt
cp .env.example .env            # Completar con tus keys
python bot.py
```

---

## Fase 2 — Instagram y Facebook (futuro)

Requiere aprobación de Meta Business y un servidor con IP pública para el webhook.
El bot ya está diseñado para conectarse a esos canales sin cambios en la lógica de respuesta.
