"""
EHP Bot — Envío de emails de pedido con anti-abuso.
Usa Gmail SMTP con App Password (no requiere OAuth).
"""
import html as html_lib
import logging
import smtplib
from datetime import datetime, timedelta
from email.mime.multipart import MIMEMultipart
from email.mime.text import MIMEText
from config import (
    SMTP_USER, SMTP_PASSWORD, SMTP_HOST, SMTP_PORT,
    ORDER_RECIPIENTS, MAX_EMAILS_PER_DAY, ORDER_COOLDOWN_HOURS,
)

log = logging.getLogger(__name__)

# ── Rate limiting en memoria ──────────────────────────────────────────────────
_emails_hoy: int = 0
_fecha_contador: str = ""           # "2026-05-24"
_ultimo_pedido_por_usuario: dict[int, datetime] = {}


def _reset_diario() -> None:
    global _emails_hoy, _fecha_contador
    hoy = datetime.now().strftime("%Y-%m-%d")
    if hoy != _fecha_contador:
        _emails_hoy = 0
        _fecha_contador = hoy


def puede_enviar(user_id: int) -> tuple[bool, str]:
    """
    Verifica si se puede enviar un email de pedido.
    Devuelve (True, "") o (False, "motivo").
    """
    _reset_diario()

    if _emails_hoy >= MAX_EMAILS_PER_DAY:
        return False, (
            "⚠️ El sistema de pedidos está temporalmente pausado por alta demanda. "
            "Escribe directamente a info@ehp-saddles.com o intenta mañana."
        )

    ultimo = _ultimo_pedido_por_usuario.get(user_id)
    if ultimo:
        espera = timedelta(hours=ORDER_COOLDOWN_HOURS)
        restante = (ultimo + espera) - datetime.now()
        if restante.total_seconds() > 0:
            horas = int(restante.total_seconds() // 3600)
            minutos = int((restante.total_seconds() % 3600) // 60)
            return False, (
                f"⚠️ Ya enviaste una solicitud de cotización recientemente. "
                f"Puedes enviar otra en {horas}h {minutos}m. "
                f"Si es urgente escribe a info@ehp-saddles.com."
            )

    return True, ""


def registrar_envio(user_id: int) -> None:
    global _emails_hoy
    _reset_diario()
    _emails_hoy += 1
    _ultimo_pedido_por_usuario[user_id] = datetime.now()


def enviar_pedido(pedido: dict) -> bool:
    """
    Envía el email de pedido a todos los destinatarios.
    pedido: { nombre, ubicacion, prod_nombre, prod_codigo, cantidad, contacto,
               user_id, username, ts }
    Devuelve True si se envió con éxito.
    """
    # Escapar todos los campos de usuario para evitar inyección HTML
    def e(v) -> str:
        return html_lib.escape(str(v))

    prod_nombre = e(pedido['prod_nombre'])
    prod_codigo = e(pedido['prod_codigo'])
    nombre      = e(pedido['nombre'])
    ubicacion   = e(pedido['ubicacion'])
    cantidad    = e(pedido['cantidad'])
    contacto    = e(pedido['contacto'])
    username    = e(pedido.get('username', '—'))
    user_id     = e(pedido['user_id'])
    ts          = e(pedido['ts'])

    asunto = f"[EHP Bot] Nuevo pedido — {pedido['prod_nombre']} — {pedido['nombre']}"

    cuerpo_html = f"""
<html><body style="font-family:Arial,sans-serif;color:#222;max-width:600px">
<h2 style="background:#8B4513;color:#fff;padding:12px 20px;border-radius:4px">
  🐴 Nuevo pedido — EHP Saddles Bot
</h2>
<table style="border-collapse:collapse;width:100%">
  <tr><td style="padding:8px;border-bottom:1px solid #eee;width:40%"><strong>Producto</strong></td>
      <td style="padding:8px;border-bottom:1px solid #eee">{prod_nombre} ({prod_codigo})</td></tr>
  <tr><td style="padding:8px;border-bottom:1px solid #eee"><strong>Cantidad</strong></td>
      <td style="padding:8px;border-bottom:1px solid #eee">{cantidad}</td></tr>
  <tr><td style="padding:8px;border-bottom:1px solid #eee"><strong>Cliente</strong></td>
      <td style="padding:8px;border-bottom:1px solid #eee">{nombre}</td></tr>
  <tr><td style="padding:8px;border-bottom:1px solid #eee"><strong>Ubicación</strong></td>
      <td style="padding:8px;border-bottom:1px solid #eee">{ubicacion}</td></tr>
  <tr><td style="padding:8px;border-bottom:1px solid #eee"><strong>Contacto</strong></td>
      <td style="padding:8px;border-bottom:1px solid #eee">{contacto}</td></tr>
  <tr><td style="padding:8px;border-bottom:1px solid #eee"><strong>Telegram</strong></td>
      <td style="padding:8px;border-bottom:1px solid #eee">
        @{username} (ID: <code>{user_id}</code>)
      </td></tr>
  <tr><td style="padding:8px"><strong>Fecha/hora</strong></td>
      <td style="padding:8px">{ts}</td></tr>
</table>
<p style="margin-top:20px;color:#666;font-size:12px">
  Responder directamente al cliente por Telegram: envía un mensaje al ID {user_id}
  o escríbele a @{username}.
</p>
</body></html>
"""

    msg = MIMEMultipart("alternative")
    msg["Subject"] = asunto
    msg["From"]    = SMTP_USER
    msg["To"]      = ", ".join(ORDER_RECIPIENTS)
    msg.attach(MIMEText(cuerpo_html, "html", "utf-8"))

    try:
        with smtplib.SMTP(SMTP_HOST, SMTP_PORT, timeout=15) as server:
            server.ehlo()
            server.starttls()
            server.login(SMTP_USER, SMTP_PASSWORD)
            server.sendmail(SMTP_USER, ORDER_RECIPIENTS, msg.as_string())
        log.info("Email de pedido enviado a %s", ORDER_RECIPIENTS)
        return True
    except Exception as exc:
        log.error("Error al enviar email: %s", exc)
        return False
