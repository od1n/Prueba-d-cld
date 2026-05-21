# ════════════════════════════════════════════════════════════════════════
# extractor.py — Extrae datos de imágenes usando el backend configurado
#
# Backend    Velocidad    Costo          Calidad manuscrito
# ─────────────────────────────────────────────────────────
# ollama     lento        gratis/local   buena (depende GPU)
# groq       ~2-4 seg     gratis (tier)  buena (llama-vision)
# anthropic  ~3-6 seg     ~$0.001/foto   excelente (haiku)
#
# Configura copiando .env.example a .env y editando VISION_BACKEND
# ════════════════════════════════════════════════════════════════════════

from __future__ import annotations

import base64
import json
import os
import re
from pathlib import Path

import requests

# Cargar .env si existe (python-dotenv ya viene instalado con uvicorn[standard])
_env_file = Path(__file__).parent / ".env"
if _env_file.exists():
    from dotenv import load_dotenv
    load_dotenv(_env_file)

# ── Configuración desde entorno ───────────────────────────────────────────
BACKEND          = os.getenv("VISION_BACKEND", "ollama").lower()

OLLAMA_URL       = os.getenv("OLLAMA_URL",   "http://localhost:11434")
OLLAMA_MODEL     = os.getenv("OLLAMA_MODEL", "gemma4:31b-cloud")

GROQ_API_KEY     = os.getenv("GROQ_API_KEY",  "")
GROQ_MODEL       = os.getenv("GROQ_MODEL",    "llama-3.2-11b-vision-preview")
GROQ_URL         = "https://api.groq.com/openai/v1/chat/completions"

ANTHROPIC_API_KEY = os.getenv("ANTHROPIC_API_KEY", "")
ANTHROPIC_MODEL   = os.getenv("ANTHROPIC_MODEL",   "claude-haiku-4-5")
ANTHROPIC_URL     = "https://api.anthropic.com/v1/messages"


# ── Prompts ───────────────────────────────────────────────────────────────
PROMPT_PLANILLA = """Eres un asistente especializado en extraer datos de planillas de producción de una planta de fustería PVC venezolana.

La planilla tiene este formato:
- ENCABEZADO: Fecha (dd/mm/aaaa), Material (Virgen o Reciclado), Operario, Supervisor, Turno, Vueltas estimadas.
- TABLA DE MOLDES: # | Molde/Descripción | [Vueltas] | [1ª Cal.] | [2ª Cal.] | Defect. | Total Buenas | Observaciones.
  Si el material es Reciclado la columna de producción se llama "Reciclado" en lugar de "1ª Cal."
- TABLA DE MATERIALES: Insumo/Resina | Sacos | Kg (×25) | Lote/Observaciones.
  Resinas típicas: Soplado 6200B, Tobo 2706.
- OBSERVACIONES GENERALES al final.

Extrae TODOS los datos visibles. Campos vacíos o ilegibles → null. Cantidades → números.

Responde ÚNICAMENTE con el JSON, sin explicaciones ni bloques de código:
{
  "fecha": "DD/MM/YYYY",
  "material": "Virgen",
  "operario": null,
  "supervisor": null,
  "turno": null,
  "vueltas_estimadas": null,
  "moldes": [
    {
      "descripcion": "texto del molde",
      "vueltas": null,
      "primera": 0,
      "segunda": 0,
      "reciclado": 0,
      "defectuosas": 0,
      "observaciones": null
    }
  ],
  "materiales": [
    {
      "insumo": "Soplado 6200B",
      "sacos": 0,
      "kg": 0,
      "lote": null
    }
  ],
  "observaciones": null
}"""

PROMPT_FACTURA = """Extrae los datos de esta factura, nota de entrega o recibo de compra.
Si la imagen NO es una factura o no contiene datos legibles, usa "encontrado": false y el resto en null / 0.
Campos ausentes → null. Responde ÚNICAMENTE con el JSON, sin bloques de código:
{
  "encontrado": true,
  "fecha": "DD/MM/YYYY",
  "numero_factura": null,
  "proveedor": null,
  "rif_proveedor": null,
  "items": [
    {"descripcion": "nombre", "cantidad": 0, "unidad": "kg",
     "precio_unitario": 0.0, "total": 0.0}
  ],
  "subtotal": 0.0,
  "iva": 0.0,
  "total": 0.0,
  "moneda": "USD",
  "observaciones": null
}"""


# ── Utilidades ────────────────────────────────────────────────────────────
def _a_base64(imagen_bytes: bytes) -> str:
    return base64.b64encode(imagen_bytes).decode("utf-8")

def _limpiar_json(texto: str) -> str:
    """Extrae el primer objeto JSON de la respuesta del modelo."""
    texto = re.sub(r"```(?:json)?", "", texto).strip()
    inicio = texto.find("{")
    if inicio == -1:
        raise ValueError("El modelo no devolvió un JSON válido.")
    nivel = 0
    for i, c in enumerate(texto[inicio:], start=inicio):
        if c == "{":   nivel += 1
        elif c == "}": nivel -= 1
        if nivel == 0: return texto[inicio: i + 1]
    raise ValueError("JSON incompleto en la respuesta del modelo.")


# ── Backends ──────────────────────────────────────────────────────────────

def _extraer_ollama(imagen_bytes: bytes, prompt: str) -> dict:
    payload = {
        "model":  OLLAMA_MODEL,
        "prompt": prompt,
        "images": [_a_base64(imagen_bytes)],
        "stream": False,
        "options": {"temperature": 0.1, "num_predict": 1024},
    }
    resp = requests.post(
        f"{OLLAMA_URL}/api/generate", json=payload, timeout=120
    )
    resp.raise_for_status()
    return json.loads(_limpiar_json(resp.json().get("response", "")))


def _extraer_groq(imagen_bytes: bytes, prompt: str) -> dict:
    if not GROQ_API_KEY:
        raise ValueError("GROQ_API_KEY no configurada en .env")
    b64 = _a_base64(imagen_bytes)
    # Detectar tipo MIME básico por magic bytes
    mime = "image/jpeg"
    if imagen_bytes[:4] == b'\x89PNG': mime = "image/png"
    elif imagen_bytes[:4] == b'RIFF':  mime = "image/webp"

    payload = {
        "model": GROQ_MODEL,
        "temperature": 0.1,
        "max_tokens": 1024,
        "messages": [{
            "role": "user",
            "content": [
                {"type": "image_url",
                 "image_url": {"url": f"data:{mime};base64,{b64}"}},
                {"type": "text", "text": prompt},
            ]
        }]
    }
    resp = requests.post(
        GROQ_URL,
        json=payload,
        headers={"Authorization": f"Bearer {GROQ_API_KEY}",
                 "Content-Type": "application/json"},
        timeout=60,
    )
    resp.raise_for_status()
    texto = resp.json()["choices"][0]["message"]["content"]
    return json.loads(_limpiar_json(texto))


def _extraer_anthropic(imagen_bytes: bytes, prompt: str) -> dict:
    if not ANTHROPIC_API_KEY:
        raise ValueError("ANTHROPIC_API_KEY no configurada en .env")
    b64 = _a_base64(imagen_bytes)
    mime = "image/jpeg"
    if imagen_bytes[:4] == b'\x89PNG': mime = "image/png"
    elif imagen_bytes[:4] == b'RIFF':  mime = "image/webp"

    payload = {
        "model": ANTHROPIC_MODEL,
        "max_tokens": 1024,
        "messages": [{
            "role": "user",
            "content": [
                {"type": "image",
                 "source": {"type": "base64", "media_type": mime, "data": b64}},
                {"type": "text", "text": prompt},
            ]
        }]
    }
    resp = requests.post(
        ANTHROPIC_URL,
        json=payload,
        headers={
            "x-api-key":         ANTHROPIC_API_KEY,
            "anthropic-version": "2023-06-01",
            "content-type":      "application/json",
        },
        timeout=60,
    )
    resp.raise_for_status()
    texto = resp.json()["content"][0]["text"]
    return json.loads(_limpiar_json(texto))


# ── Interfaz pública ──────────────────────────────────────────────────────

def _extraer(imagen_bytes: bytes, prompt: str, backend_override: str = "") -> dict:
    """Extrae datos usando el backend configurado o el indicado en backend_override."""
    backend = (backend_override.lower().strip() or BACKEND)
    if backend == "groq":
        return _extraer_groq(imagen_bytes, prompt)
    elif backend == "anthropic":
        return _extraer_anthropic(imagen_bytes, prompt)
    else:
        return _extraer_ollama(imagen_bytes, prompt)


def extraer_planilla(imagen_bytes: bytes, backend_override: str = "") -> dict:
    return _extraer(imagen_bytes, PROMPT_PLANILLA, backend_override)

def extraer_factura(imagen_bytes: bytes, backend_override: str = "") -> dict:
    return _extraer(imagen_bytes, PROMPT_FACTURA, backend_override)


def modelo_disponible() -> bool:
    """Verifica que el backend esté operativo."""
    try:
        if BACKEND == "groq":
            if not GROQ_API_KEY:
                return False
            r = requests.get(
                "https://api.groq.com/openai/v1/models",
                headers={"Authorization": f"Bearer {GROQ_API_KEY}"},
                timeout=8,
            )
            return r.status_code == 200

        elif BACKEND == "anthropic":
            if not ANTHROPIC_API_KEY:
                return False
            # Llamada mínima para verificar la clave
            r = requests.post(
                ANTHROPIC_URL,
                json={"model": ANTHROPIC_MODEL, "max_tokens": 1,
                      "messages": [{"role": "user", "content": "hi"}]},
                headers={"x-api-key": ANTHROPIC_API_KEY,
                         "anthropic-version": "2023-06-01",
                         "content-type": "application/json"},
                timeout=8,
            )
            return r.status_code in (200, 400)  # 400 = bad request, pero la clave funciona

        else:  # ollama
            # Solo verificamos que Ollama responda — no comprobamos el modelo
            # exacto porque el nombre puede variar (con/sin :latest, mayúsculas, etc.)
            # Si Ollama no está corriendo, get() lanzará una excepción y retornamos False.
            r = requests.get(f"{OLLAMA_URL}/api/tags", timeout=5)
            return r.status_code == 200

    except Exception:
        return False


def info_backend() -> dict:
    """Retorna info del backend activo para mostrar en el chip de estado."""
    nombres = {"groq": "Groq", "anthropic": "Anthropic", "ollama": "Ollama local"}
    modelos = {"groq": GROQ_MODEL, "anthropic": ANTHROPIC_MODEL, "ollama": OLLAMA_MODEL}
    return {
        "backend": BACKEND,
        "nombre":  nombres.get(BACKEND, BACKEND),
        "modelo":  modelos.get(BACKEND, ""),
    }
