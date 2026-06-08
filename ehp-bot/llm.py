"""
EHP Bot — LLM vía OpenRouter con fallback automático entre modelos gratuitos.
Si el modelo principal está saturado, prueba el siguiente de la lista.
"""
import logging
from openai import AsyncOpenAI
from config import OPENROUTER_API_KEY, LLM_MODEL, LLM_TEMPERATURE, LLM_MAX_TOKENS
from catalog import obtener_contexto_catalogo

log = logging.getLogger(__name__)

_client = AsyncOpenAI(
    api_key=OPENROUTER_API_KEY,
    base_url="https://openrouter.ai/api/v1",
    max_retries=0,   # manejamos reintentos nosotros con fallback
)

# Modelos gratuitos en orden de preferencia.
# Si el primero está saturado, se intenta el siguiente.
FALLBACK_MODELS: list[str] = [
    LLM_MODEL,                                    # modelo del .env (configurable)
    "deepseek/deepseek-v4-flash:free",
    "meta-llama/llama-3.2-3b-instruct:free",
    "liquid/lfm-2.5-1.2b-instruct:free",
    "nvidia/nemotron-nano-9b-v2:free",
]
# Eliminar duplicados manteniendo el orden
seen: set[str] = set()
FALLBACK_MODELS = [m for m in FALLBACK_MODELS if not (m in seen or seen.add(m))]

SYSTEM_PROMPT = f"""Eres el asistente de ventas de El Hato Pascher (EHP Saddles), \
taller venezolano de árboles de montura de polietileno de alta densidad.

REGLAS ESTRICTAS — DEBES SEGUIRLAS SIN EXCEPCIÓN:
1. NUNCA menciones cifras de precio. Ni rangos, ni estimaciones, ni "aproximadamente". \
   Cuando te pregunten precio, di EXACTAMENTE: "El precio lo confirma un asesor del taller \
   según modelo, cantidad y destino. ¿Me das tu nombre y país para ponerlos en contacto contigo?"
2. Responde en el idioma del cliente (español o inglés).
3. Respuestas cortas, 3–5 líneas máximo salvo que pidan más detalle.
4. Si el cliente quiere comprar o cotizar, recoge: nombre, país/ciudad, modelo, cantidad. \
   Luego dile que el taller lo contactará pronto.
5. No prometas descuentos ni condiciones especiales.
6. Solo usa datos que estén en el catálogo. No inventes medidas, pesos ni especificaciones.
7. Si la pregunta no tiene que ver con monturas o el taller, redirige amablemente.

CATÁLOGO DISPONIBLE:
{obtener_contexto_catalogo()}
"""


async def responder(historial: list[dict]) -> str:
    """
    Llama al LLM con fallback automático si el modelo preferido está saturado.
    historial: lista de dicts con keys 'role' ('user'/'assistant') y 'content' (str).
    """
    messages = [{"role": "system", "content": SYSTEM_PROMPT}] + historial
    last_error: Exception | None = None

    for model in FALLBACK_MODELS:
        log.info("Intentando modelo: %s", model)
        try:
            response = await _client.chat.completions.create(
                model=model,
                messages=messages,
                temperature=LLM_TEMPERATURE,
                max_tokens=LLM_MAX_TOKENS,
            )
            texto = response.choices[0].message.content.strip()
            log.info("OK con %s (%d chars)", model, len(texto))
            return texto
        except Exception as exc:
            log.warning("Falló %s: %s", model, exc)
            last_error = exc
            continue   # probar el siguiente

    log.error("Todos los modelos fallaron. Último error: %s", last_error)
    return (
        "Lo siento, el servicio de respuestas está temporalmente sobrecargado. "
        "Por favor intenta de nuevo en un momento, o escríbenos directamente al taller."
    )
