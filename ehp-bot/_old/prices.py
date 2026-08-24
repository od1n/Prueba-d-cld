"""
EHP Bot — Precios en tiempo real desde SistemaGestion.
Si el túnel Cloudflare no está configurado o falla, devuelve "consultar".
"""
import logging
import httpx
from config import SISTEMA_GESTION_URL, SISTEMA_GESTION_SECRET

log = logging.getLogger(__name__)

# Cache en memoria para no golpear SistemaGestion en cada mensaje
_cache: dict[str, str] = {}
_cache_ts: float = 0.0
CACHE_TTL_SECONDS = 300   # 5 minutos


async def obtener_precios() -> dict[str, str]:
    """
    Devuelve dict { codigo_EHP: precio_str }
    Ejemplo: { "EHP-W14": "$85", "EHP-W12S": "$75" }
    Si no hay conexión, devuelve dict vacío.

    SistemaGestion debe exponer:
      GET {SISTEMA_GESTION_URL}/api/bot/precios
      Header: X-Bot-Secret: {SISTEMA_GESTION_SECRET}
      Response: { "EHP-W14": 85.0, "EHP-W12S": 75.0, ... }
    """
    import time
    global _cache, _cache_ts

    if not SISTEMA_GESTION_URL:
        return {}

    ahora = time.time()
    if _cache and (ahora - _cache_ts) < CACHE_TTL_SECONDS:
        return _cache

    try:
        headers = {}
        if SISTEMA_GESTION_SECRET:
            headers["X-Bot-Secret"] = SISTEMA_GESTION_SECRET

        async with httpx.AsyncClient(timeout=5.0) as client:
            r = await client.get(
                f"{SISTEMA_GESTION_URL.rstrip('/')}/api/bot/precios",
                headers=headers,
            )
            r.raise_for_status()
            data: dict = r.json()

        _cache = {k: f"${v:,.0f} USD" if isinstance(v, (int, float)) else str(v)
                  for k, v in data.items()}
        _cache_ts = ahora
        log.info("Precios actualizados desde SistemaGestion (%d modelos)", len(_cache))
        return _cache

    except Exception as exc:
        log.warning("No se pudo consultar SistemaGestion: %s", exc)
        return _cache or {}


async def precio_de(codigo: str) -> str:
    """Precio formateado de un producto, o 'Precio a consultar'."""
    precios = await obtener_precios()
    return precios.get(codigo, "Precio a consultar")
