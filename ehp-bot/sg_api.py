"""
EHP Bot (Telegram) — Cliente de la API de SistemaGestion (/api/wa/*).

Mismos endpoints y contrato que usa el bot de WhatsApp (n8n):
  GET  /api/wa/catalogo?q=              -> {total, productos:[{codigo,descripcion,unidad,precio}]}
  GET  /api/wa/producto/{codigo}        -> {codigo,descripcion,departamento,unidad,precio} | 404
  POST /api/wa/validar-pin              -> {valido, operador, ...} | {valido:false, error}
  POST /api/wa/estado-cuenta           -> {cliente, saldoFavor, totalAbonos, totalDeuda, facturasPendientes[]}
  POST /api/wa/abonos                  -> {total, abonos[]}
  GET  /api/wa/pedido?nombre=&tel=&items=&wa=  -> {ok, enviado, mensaje}

Auth: header X-Internal-Token (SG_INTERNAL_TOKEN), igual que n8n.
"""
import httpx

from config import SG_API_URL, SG_INTERNAL_TOKEN, SG_TIMEOUT

_HEADERS = {"X-Internal-Token": SG_INTERNAL_TOKEN}


class SGError(Exception):
    """Error al hablar con SistemaGestion (red, 401, etc.)."""


async def _get(path: str, params: dict | None = None) -> dict:
    url = f"{SG_API_URL}{path}"
    try:
        async with httpx.AsyncClient(timeout=SG_TIMEOUT) as client:
            r = await client.get(url, params=params, headers=_HEADERS)
    except Exception as exc:
        raise SGError(f"No se pudo conectar con SistemaGestion: {exc}") from exc
    if r.status_code == 401:
        raise SGError("No autorizado (revisa SG_INTERNAL_TOKEN).")
    if r.status_code == 404:
        return {"_status": 404, **_safe_json(r)}
    if r.status_code >= 400:
        raise SGError(f"Error {r.status_code} de SistemaGestion.")
    return _safe_json(r)


async def _post(path: str, body: dict) -> dict:
    url = f"{SG_API_URL}{path}"
    try:
        async with httpx.AsyncClient(timeout=SG_TIMEOUT) as client:
            r = await client.post(url, json=body, headers=_HEADERS)
    except Exception as exc:
        raise SGError(f"No se pudo conectar con SistemaGestion: {exc}") from exc
    if r.status_code == 401:
        raise SGError("No autorizado (revisa SG_INTERNAL_TOKEN).")
    # 200/403 traen JSON util (validar-pin devuelve 200 con valido:false; estado 403 con error)
    return {"_status": r.status_code, **_safe_json(r)}


def _safe_json(r: httpx.Response) -> dict:
    try:
        data = r.json()
        return data if isinstance(data, dict) else {"data": data}
    except Exception:
        return {}


# ── Endpoints ────────────────────────────────────────────────────────────────

async def catalogo(q: str | None = None) -> dict:
    return await _get("/api/wa/catalogo", {"q": q} if q else None)


async def producto(codigo: str) -> dict:
    return await _get(f"/api/wa/producto/{codigo}")


async def validar_pin(pin: str) -> dict:
    return await _post("/api/wa/validar-pin", {"pin": pin})


async def estado_cuenta(pin: str, codigo_cliente: str) -> dict:
    return await _post("/api/wa/estado-cuenta", {"pin": pin, "codigoCliente": codigo_cliente})


async def abonos(pin: str, codigo_cliente: str, ultimos: int = 10) -> dict:
    return await _post("/api/wa/abonos", {"pin": pin, "codigoCliente": codigo_cliente, "ultimos": ultimos})


async def pedido(nombre: str, tel: str, items: str, wa: str | None = None) -> dict:
    params = {"nombre": nombre, "tel": tel, "items": items}
    if wa:
        params["wa"] = wa
    return await _get("/api/wa/pedido", params)
