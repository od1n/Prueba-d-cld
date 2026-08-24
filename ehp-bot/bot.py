"""
EHP Bot (Telegram) — ESPEJO del bot de WhatsApp (n8n).

Mismos comandos y datos que el bot de WhatsApp, servidos por los endpoints
/api/wa/* de SistemaGestion. No hay catalogo ni precios locales: todo en vivo.

Comandos (funcionan como texto o como /comando):
  catalogo                      -> lista de productos con precio
  buscar <texto>                -> busca productos
  precio <codigo>               -> ficha de un producto
  estado <PIN> <CLIENTE>        -> estado de cuenta (requiere PIN de operador)
  abonos <PIN> <CLIENTE>        -> ultimos abonos (requiere PIN de operador)
  pedido Nombre, Telefono, productos   -> registra pedido y envia email
"""
import asyncio
import logging

from telegram import Update
from telegram.constants import ParseMode
from telegram.ext import (
    Application, CommandHandler, MessageHandler, ContextTypes, filters,
)

import config
import sg_api
from sg_api import SGError

logging.basicConfig(
    format="%(asctime)s | %(levelname)s | %(name)s | %(message)s",
    level=logging.INFO,
)
log = logging.getLogger(__name__)

MENU = (
    "*EHP · SistemaGestion*\n"
    "Escribe una opcion o el comando:\n\n"
    "1) `catalogo` — lista de productos\n"
    "2) `buscar <texto>` — buscar producto\n"
    "3) `precio <codigo>` — detalle y precio\n"
    "4) `estado <PIN> <CLIENTE>` — estado de cuenta (operador)\n"
    "5) `pedido Nombre, Telefono, productos` — registrar pedido\n\n"
    "Ej: `buscar tubo` · `precio TUB001` · `pedido Juan, 0414-1234567, 2xTUB001`"
)


def _money(v) -> str:
    try:
        return f"${float(v):,.2f}"
    except Exception:
        return str(v)


# ── Acciones (compartidas por texto y por slash-comando) ──────────────────────

async def do_catalogo(update: Update, q: str | None) -> None:
    try:
        data = await sg_api.catalogo(q)
    except SGError as e:
        await update.message.reply_text(f"⚠️ {e}")
        return
    prods = data.get("productos", [])
    if not prods:
        await update.message.reply_text("No se encontraron productos." + (f" (busqueda: {q})" if q else ""))
        return
    lineas = [f"*Catalogo* ({data.get('total', len(prods))})" + (f" · _{q}_" if q else ""), ""]
    for p in prods[:30]:
        lineas.append(f"`{p['codigo']}` — {p['descripcion']}\n   {_money(p['precio'])} / {p.get('unidad','') or 'u'}")
    lineas.append("\nPara detalle: `precio <codigo>`")
    await update.message.reply_text("\n".join(lineas), parse_mode=ParseMode.MARKDOWN)


async def do_precio(update: Update, codigo: str) -> None:
    if not codigo:
        await update.message.reply_text("Uso: `precio <codigo>`", parse_mode=ParseMode.MARKDOWN)
        return
    try:
        p = await sg_api.producto(codigo.strip())
    except SGError as e:
        await update.message.reply_text(f"⚠️ {e}")
        return
    if p.get("_status") == 404 or not p.get("codigo"):
        await update.message.reply_text(f"Producto `{codigo}` no encontrado.", parse_mode=ParseMode.MARKDOWN)
        return
    txt = (
        f"*{p['descripcion']}*\n"
        f"`{p['codigo']}`\n"
        f"Departamento: {p.get('departamento','') or '—'}\n"
        f"Unidad: {p.get('unidad','') or '—'}\n"
        f"💵 *{_money(p['precio'])}*"
    )
    await update.message.reply_text(txt, parse_mode=ParseMode.MARKDOWN)


async def do_estado(update: Update, args: list[str]) -> None:
    if len(args) < 2:
        await update.message.reply_text("Uso: `estado <PIN> <CLIENTE>`", parse_mode=ParseMode.MARKDOWN)
        return
    pin, cliente = args[0], args[1]
    try:
        d = await sg_api.estado_cuenta(pin, cliente)
    except SGError as e:
        await update.message.reply_text(f"⚠️ {e}")
        return
    if d.get("_status") == 403 or d.get("error"):
        await update.message.reply_text(f"🔒 {d.get('error', 'PIN incorrecto.')}")
        return
    cli = d.get("cliente", {})
    lineas = [
        "*Estado de cuenta*",
        f"Cliente: {cli.get('nombre','—')} (`{cli.get('codigo','')}`)",
        f"Deuda total: *{_money(d.get('totalDeuda',0))}*",
        f"Abonos: {_money(d.get('totalAbonos',0))} · A favor: {_money(d.get('saldoFavor',0))}",
    ]
    fac = d.get("facturasPendientes", [])
    if fac:
        lineas.append("\n*Pendientes:*")
        for f in fac[:15]:
            venc = " ⚠️VENCIDA" if f.get("vencida") else ""
            lineas.append(f"· {f.get('concepto','')} — saldo {_money(f.get('saldo',0))} (vence {f.get('vence','')}){venc}")
    else:
        lineas.append("\nSin facturas pendientes.")
    await update.message.reply_text("\n".join(lineas), parse_mode=ParseMode.MARKDOWN)


async def do_abonos(update: Update, args: list[str]) -> None:
    if len(args) < 2:
        await update.message.reply_text("Uso: `abonos <PIN> <CLIENTE>`", parse_mode=ParseMode.MARKDOWN)
        return
    pin, cliente = args[0], args[1]
    try:
        d = await sg_api.abonos(pin, cliente, 10)
    except SGError as e:
        await update.message.reply_text(f"⚠️ {e}")
        return
    if d.get("_status") == 403 or d.get("error"):
        await update.message.reply_text(f"🔒 {d.get('error', 'PIN incorrecto.')}")
        return
    ab = d.get("abonos", [])
    if not ab:
        await update.message.reply_text("Sin abonos registrados.")
        return
    lineas = [f"*Ultimos abonos* ({d.get('total', len(ab))})", ""]
    for a in ab:
        lineas.append(f"· {a.get('fecha','')} — {_money(a.get('monto',0))} ({a.get('metodo','')}) {a.get('estado','')}")
    await update.message.reply_text("\n".join(lineas), parse_mode=ParseMode.MARKDOWN)


async def do_pedido(update: Update, resto: str) -> None:
    # Formato: pedido Nombre, Telefono, productos
    partes = [x.strip() for x in resto.split(",")]
    if len(partes) < 3 or not partes[0]:
        await update.message.reply_text(
            "Uso: `pedido Nombre, Telefono, productos`\n"
            "Ej: `pedido Juan Perez, 0414-1234567, 2xTUB001, 5xSLA002`",
            parse_mode=ParseMode.MARKDOWN,
        )
        return
    nombre = partes[0]
    tel = partes[1]
    items = ", ".join(partes[2:])
    wa = str(update.effective_user.id)
    try:
        d = await sg_api.pedido(nombre, tel, items, wa)
    except SGError as e:
        await update.message.reply_text(f"⚠️ {e}")
        return
    msg = d.get("mensaje", "Pedido registrado.")
    await update.message.reply_text(f"✅ {msg}")
    if config.OPERATOR_CHAT_ID:
        try:
            await update.get_bot().send_message(
                chat_id=config.OPERATOR_CHAT_ID,
                text=(f"📦 *Pedido (Telegram)*\n{nombre} — {tel}\n{items}\nTG id: `{wa}`"),
                parse_mode=ParseMode.MARKDOWN,
            )
        except Exception as exc:
            log.warning("No se pudo avisar al operador: %s", exc)


# ── Slash-comandos ────────────────────────────────────────────────────────────

async def cmd_start(update, ctx): await update.message.reply_text(MENU, parse_mode=ParseMode.MARKDOWN)
async def h_catalogo(update, ctx): await do_catalogo(update, " ".join(ctx.args) or None)
async def h_buscar(update, ctx):   await do_catalogo(update, " ".join(ctx.args) or None)
async def h_precio(update, ctx):   await do_precio(update, " ".join(ctx.args))
async def h_estado(update, ctx):   await do_estado(update, ctx.args)
async def h_abonos(update, ctx):   await do_abonos(update, ctx.args)
async def h_pedido(update, ctx):   await do_pedido(update, " ".join(ctx.args))


# ── Texto libre: mapea la primera palabra al comando ──────────────────────────

async def h_texto(update: Update, ctx: ContextTypes.DEFAULT_TYPE) -> None:
    texto = (update.message.text or "").strip()
    if not texto:
        return
    palabras = texto.split()
    primera = palabras[0].lower()
    resto = texto[len(palabras[0]):].strip()

    if primera in ("catalogo", "catálogo", "1"):
        await do_catalogo(update, resto or None)
    elif primera in ("menu", "menú"):
        await update.message.reply_text(MENU, parse_mode=ParseMode.MARKDOWN)
    elif primera in ("buscar", "2"):
        await do_catalogo(update, resto or None)
    elif primera in ("precio", "3"):
        await do_precio(update, resto)
    elif primera in ("estado", "4"):
        await do_estado(update, resto.split())
    elif primera in ("abonos",):
        await do_abonos(update, resto.split())
    elif primera in ("pedido", "5"):
        await do_pedido(update, resto)
    else:
        await update.message.reply_text("No entendi. " + MENU, parse_mode=ParseMode.MARKDOWN)


# ── Arranque ──────────────────────────────────────────────────────────────────

async def main() -> None:
    log.info("Iniciando EHP Bot (espejo de WhatsApp)…")

    async def post_init(app: Application) -> None:
        from telegram import BotCommand
        await app.bot.set_my_commands([
            BotCommand("start",    "Menu / ayuda"),
            BotCommand("catalogo", "Lista de productos"),
            BotCommand("buscar",   "Buscar producto"),
            BotCommand("precio",   "Detalle y precio de un producto"),
            BotCommand("estado",   "Estado de cuenta (PIN operador)"),
            BotCommand("abonos",   "Ultimos abonos (PIN operador)"),
            BotCommand("pedido",   "Registrar un pedido"),
        ])

    app = Application.builder().token(config.TELEGRAM_TOKEN).post_init(post_init).build()

    app.add_handler(CommandHandler("start", cmd_start))
    app.add_handler(CommandHandler("catalogo", h_catalogo))
    app.add_handler(CommandHandler("buscar", h_buscar))
    app.add_handler(CommandHandler("precio", h_precio))
    app.add_handler(CommandHandler("estado", h_estado))
    app.add_handler(CommandHandler("abonos", h_abonos))
    app.add_handler(CommandHandler("pedido", h_pedido))
    app.add_handler(MessageHandler(filters.TEXT & ~filters.COMMAND, h_texto))

    log.info("Bot activo. Ctrl+C para detener.")
    async with app:
        await app.initialize()
        await app.start()
        await app.updater.start_polling(allowed_updates=Update.ALL_TYPES)
        await asyncio.Event().wait()


if __name__ == "__main__":
    asyncio.run(main())
