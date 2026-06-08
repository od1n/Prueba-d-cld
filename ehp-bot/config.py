"""
EHP Bot — Configuración centralizada vía variables de entorno.
"""
import os
from dotenv import load_dotenv

load_dotenv()

# ── Telegram ──────────────────────────────────────────────────────────────────
TELEGRAM_TOKEN: str         = os.environ["TELEGRAM_TOKEN"]
OPERATOR_CHAT_ID: str       = os.environ["OPERATOR_CHAT_ID"]

# ── OpenRouter (fallback LLM para FAQ libre) ──────────────────────────────────
OPENROUTER_API_KEY: str     = os.environ["OPENROUTER_API_KEY"]
LLM_MODEL: str              = os.getenv("LLM_MODEL", "meta-llama/llama-3.3-70b-instruct:free")
LLM_TEMPERATURE: float      = float(os.getenv("LLM_TEMPERATURE", "0.3"))
LLM_MAX_TOKENS: int         = int(os.getenv("LLM_MAX_TOKENS", "400"))

# ── Email (Gmail SMTP con App Password) ───────────────────────────────────────
# hatopascher@gmail.com → Seguridad → Verificación en 2 pasos → Contraseñas de app
SMTP_USER: str              = os.environ["SMTP_USER"]          # hatopascher@gmail.com
SMTP_PASSWORD: str          = os.environ["SMTP_PASSWORD"]      # app password 16 chars
SMTP_HOST: str              = os.getenv("SMTP_HOST", "smtp.gmail.com")
SMTP_PORT: int              = int(os.getenv("SMTP_PORT", "587"))
# Destinatarios de pedidos (separados por coma en el .env)
ORDER_RECIPIENTS: list[str] = [
    r.strip() for r in os.getenv(
        "ORDER_RECIPIENTS",
        "info@ehp-saddles.com,hatopascher@gmail.com"
    ).split(",") if r.strip()
]

# ── Anti-abuso ────────────────────────────────────────────────────────────────
# Máximo de emails de pedido por día (global, todos los usuarios)
MAX_EMAILS_PER_DAY: int     = int(os.getenv("MAX_EMAILS_PER_DAY", "20"))
# Horas de espera entre pedidos del mismo usuario
ORDER_COOLDOWN_HOURS: int   = int(os.getenv("ORDER_COOLDOWN_HOURS", "24"))

# ── SistemaGestion (precios en tiempo real via Cloudflare Tunnel) ─────────────
# Dejar vacío si no está configurado — el bot usará "precio a consultar"
SISTEMA_GESTION_URL: str    = os.getenv("SISTEMA_GESTION_URL", "")
# Header de autenticación que SistemaGestion espera
SISTEMA_GESTION_SECRET: str = os.getenv("SISTEMA_GESTION_SECRET", "")
