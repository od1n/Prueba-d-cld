"""
EHP Bot (Telegram) — Configuracion centralizada via variables de entorno.

Este bot es un ESPEJO del bot de WhatsApp (n8n): usa los mismos endpoints
/api/wa/* de SistemaGestion. No tiene catalogo ni precios propios; todo sale
en vivo de la API del ERP.
"""
import os
from dotenv import load_dotenv

load_dotenv()

# ── Telegram ──────────────────────────────────────────────────────────────────
TELEGRAM_TOKEN: str   = os.environ["TELEGRAM_TOKEN"]          # de @BotFather
# ID del operador que recibe avisos (opcional; vacio = no notifica)
OPERATOR_CHAT_ID: str = os.getenv("OPERATOR_CHAT_ID", "").strip()

# ── SistemaGestion (los mismos endpoints /api/wa/* que usa el bot de WhatsApp) ─
# Base del API por el tunel de Cloudflare (o IP local en la LAN).
SG_API_URL: str       = os.getenv("SG_API_URL", "https://api-sg.hato-pascher.com").rstrip("/")
# Token interno: DEBE ser el mismo SG_INTERNAL_TOKEN de la API/n8n/digitalizador.
SG_INTERNAL_TOKEN: str = os.environ["SG_INTERNAL_TOKEN"]
# Timeout de las llamadas HTTP (segundos)
SG_TIMEOUT: float     = float(os.getenv("SG_TIMEOUT", "12"))
