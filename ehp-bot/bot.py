"""
EHP Saddles — Bot de Telegram (menú estructurado con botones)
─────────────────────────────────────────────────────────────
Flujo: categoría → producto → ficha + precio → pedido → email
Sin LLM para el flujo de ventas; confiable, sin alucinaciones.
"""
import asyncio
import logging
from datetime import datetime

from telegram import Update, InlineKeyboardButton, InlineKeyboardMarkup
from telegram.ext import (
    Application, CommandHandler, CallbackQueryHandler,
    MessageHandler, ConversationHandler, ContextTypes, filters,
)
from telegram.constants import ParseMode

import config
import email_sender
from catalog import PRODUCTOS, buscar_producto
from prices import precio_de

logging.basicConfig(
    format="%(asctime)s | %(levelname)s | %(name)s | %(message)s",
    level=logging.INFO,
)
log = logging.getLogger(__name__)

# ── Estados del ConversationHandler ──────────────────────────────────────────
(
    MENU,
    SHOW_CAT,
    SHOW_PROD,
    ASK_NAME,
    ASK_LOCATION,
    ASK_QUANTITY,
    ASK_CONTACT,
    CONFIRM,
) = range(8)

# ── Categorías ─────────────────────────────────────────────────────────────────
CATS = {
    "trabajo":    "🔧 Trabajo (McCleallan)",
    "deporte":    "🏇 Fustes con Pico",
    "recreativa": "🌄 Recreativa / Paseo",
    "accesorios": "🪜 Accesorios",
}


# ── Teclados ──────────────────────────────────────────────────────────────────

def kb_menu() -> InlineKeyboardMarkup:
    btns = [[InlineKeyboardButton(label, callback_data=f"cat:{key}")]
            for key, label in CATS.items()]
    btns.append([InlineKeyboardButton("📋 Ver catálogo completo", callback_data="cat:all")])
    return InlineKeyboardMarkup(btns)


def kb_productos(categoria: str) -> InlineKeyboardMarkup:
    if categoria == "all":
        prods = PRODUCTOS
    else:
        prods = [p for p in PRODUCTOS if p["categoria"] == categoria]
    btns = [[InlineKeyboardButton(p["nombre"], callback_data=f"prod:{p['codigo']}")]
            for p in prods]
    btns.append([InlineKeyboardButton("⬅️ Volver", callback_data="back:menu")])
    return InlineKeyboardMarkup(btns)


def kb_producto(codigo: str) -> InlineKeyboardMarkup:
    prod = next((p for p in PRODUCTOS if p["codigo"] == codigo), None)
    cat = prod["categoria"] if prod else "all"
    return InlineKeyboardMarkup([
        [InlineKeyboardButton("📦 Solicitar cotización", callback_data=f"order:{codigo}")],
        [InlineKeyboardButton("⬅️ Volver", callback_data=f"cat:{cat}")],
    ])


def kb_confirmar() -> InlineKeyboardMarkup:
    return InlineKeyboardMarkup([
        [InlineKeyboardButton("✅ Confirmar pedido", callback_data="confirm:yes")],
        [InlineKeyboardButton("❌ Cancelar",          callback_data="confirm:no")],
    ])


def kb_volver_menu() -> InlineKeyboardMarkup:
    return InlineKeyboardMarkup([
        [InlineKeyboardButton("🏠 Menú principal", callback_data="back:menu")],
    ])


# ── Texto de ficha de producto ─────────────────────────────────────────────────

async def texto_producto(prod: dict) -> str:
    precio = await precio_de(prod["codigo"])
    medidas_str = ""
    if prod.get("medidas"):
        medidas_str = "\n\n📏 *Medidas:*\n" + "\n".join(
            f"  · {k}: {v}" for k, v in prod["medidas"].items()
        )
    return (
        f"*{prod['nombre']}*\n"
        f"`{prod['codigo']}` · {prod['peso']}\n\n"
        f"{prod['descripcion']}"
        f"{medidas_str}\n\n"
        f"💵 *{precio}*"
    )


# ── Handlers ──────────────────────────────────────────────────────────────────

async def cmd_start(update: Update, ctx: ContextTypes.DEFAULT_TYPE) -> int:
    ctx.user_data.clear()
    nombre = update.effective_user.first_name or "Hola"
    await update.message.reply_text(
        f"👋 Bienvenido a *EHP Saddles*, {nombre}.\n\n"
        "Fabricamos árboles de montura en polietileno de alta densidad "
        "— resistentes, livianos y terminados a mano.\n\n"
        "¿Qué categoría te interesa?",
        parse_mode=ParseMode.MARKDOWN,
        reply_markup=kb_menu(),
    )
    return MENU


async def cb_menu(update: Update, ctx: ContextTypes.DEFAULT_TYPE) -> int:
    q = update.callback_query
    await q.answer()
    await q.edit_message_text(
        "Selecciona una categoría:",
        reply_markup=kb_menu(),
    )
    return MENU


async def cb_categoria(update: Update, ctx: ContextTypes.DEFAULT_TYPE) -> int:
    q = update.callback_query
    await q.answer()
    _, cat = q.data.split(":", 1)
    # Validar que la categoría sea conocida
    if cat not in CATS and cat != "all":
        await q.answer("Categoría no válida.", show_alert=True)
        return MENU
    ctx.user_data["cat"] = cat
    label = "Catálogo completo" if cat == "all" else CATS.get(cat, cat)
    await q.edit_message_text(
        f"*{label}*\n\nSelecciona un modelo:",
        parse_mode=ParseMode.MARKDOWN,
        reply_markup=kb_productos(cat),
    )
    return SHOW_CAT


async def cb_producto(update: Update, ctx: ContextTypes.DEFAULT_TYPE) -> int:
    q = update.callback_query
    await q.answer()
    _, codigo = q.data.split(":", 1)
    # Validar que el código sea alfanumérico corto (evita inyección en callback_data)
    if not codigo.replace("-", "").replace("_", "").isalnum() or len(codigo) > 30:
        await q.answer("Producto no válido.", show_alert=True)
        return MENU
    prod = next((p for p in PRODUCTOS if p["codigo"] == codigo), None)
    if not prod:
        await q.edit_message_text("Producto no encontrado.", reply_markup=kb_volver_menu())
        return MENU

    ctx.user_data["prod_codigo"] = codigo
    ctx.user_data["prod_nombre"] = prod["nombre"]
    texto = await texto_producto(prod)
    await q.edit_message_text(
        texto,
        parse_mode=ParseMode.MARKDOWN,
        reply_markup=kb_producto(codigo),
    )
    return SHOW_PROD


async def cb_order(update: Update, ctx: ContextTypes.DEFAULT_TYPE) -> int:
    q = update.callback_query
    await q.answer()
    _, codigo = q.data.split(":", 1)
    prod = next((p for p in PRODUCTOS if p["codigo"] == codigo), None)
    if prod:
        ctx.user_data["prod_codigo"] = codigo
        ctx.user_data["prod_nombre"] = prod["nombre"]

    # Verificar rate limit antes de iniciar el flujo
    ok, motivo = email_sender.puede_enviar(update.effective_user.id)
    if not ok:
        await q.edit_message_text(motivo, reply_markup=kb_volver_menu())
        return MENU

    await q.edit_message_text(
        f"📦 *Cotización — {ctx.user_data.get('prod_nombre', '')}*\n\n"
        "Necesito algunos datos para preparar tu cotización.\n\n"
        "¿Cuál es tu *nombre completo*?",
        parse_mode=ParseMode.MARKDOWN,
    )
    return ASK_NAME


async def ask_location(update: Update, ctx: ContextTypes.DEFAULT_TYPE) -> int:
    ctx.user_data["nombre"] = update.message.text.strip()[:150]
    await update.message.reply_text(
        f"Gracias, {ctx.user_data['nombre']}.\n\n¿En qué *país y ciudad* estás?"
        " (Ej: Venezuela, Barinas / Colombia, Bogotá)",
        parse_mode=ParseMode.MARKDOWN,
    )
    return ASK_LOCATION


async def ask_quantity(update: Update, ctx: ContextTypes.DEFAULT_TYPE) -> int:
    ctx.user_data["ubicacion"] = update.message.text.strip()[:150]
    await update.message.reply_text(
        "¿Qué *cantidad* de unidades necesitas aproximadamente?",
        parse_mode=ParseMode.MARKDOWN,
    )
    return ASK_QUANTITY


async def ask_contact(update: Update, ctx: ContextTypes.DEFAULT_TYPE) -> int:
    texto = update.message.text.strip()
    # Validación básica de cantidad
    if not any(c.isdigit() for c in texto):
        await update.message.reply_text(
            "Por favor indica una cantidad numérica (Ej: 1, 5, 20)."
        )
        return ASK_QUANTITY
    ctx.user_data["cantidad"] = texto[:30]
    await update.message.reply_text(
        "¿Cuál es tu *teléfono o email* de contacto para que el taller te responda?",
        parse_mode=ParseMode.MARKDOWN,
    )
    return ASK_CONTACT


async def mostrar_confirmacion(update: Update, ctx: ContextTypes.DEFAULT_TYPE) -> int:
    ctx.user_data["contacto"] = update.message.text.strip()[:150]
    d = ctx.user_data
    resumen = (
        f"📋 *Resumen de tu solicitud:*\n\n"
        f"🔹 Producto: {d.get('prod_nombre','—')} (`{d.get('prod_codigo','—')}`)\n"
        f"🔹 Cantidad: {d.get('cantidad','—')}\n"
        f"🔹 Nombre: {d.get('nombre','—')}\n"
        f"🔹 Ubicación: {d.get('ubicacion','—')}\n"
        f"🔹 Contacto: {d.get('contacto','—')}\n\n"
        "¿Confirmas el envío de esta solicitud al taller?"
    )
    await update.message.reply_text(
        resumen,
        parse_mode=ParseMode.MARKDOWN,
        reply_markup=kb_confirmar(),
    )
    return CONFIRM


async def cb_confirmar(update: Update, ctx: ContextTypes.DEFAULT_TYPE) -> int:
    q = update.callback_query
    await q.answer()
    _, resp = q.data.split(":", 1)

    if resp == "no":
        await q.edit_message_text(
            "Solicitud cancelada. ¿En qué más puedo ayudarte?",
            reply_markup=kb_volver_menu(),
        )
        return MENU

    user = update.effective_user
    d = ctx.user_data

    # Verificar rate limit de nuevo al confirmar
    ok, motivo = email_sender.puede_enviar(user.id)
    if not ok:
        await q.edit_message_text(motivo, reply_markup=kb_volver_menu())
        return MENU

    pedido = {
        "prod_nombre":   d.get("prod_nombre", "—"),
        "prod_codigo":   d.get("prod_codigo", "—"),
        "cantidad":      d.get("cantidad", "—"),
        "nombre":        d.get("nombre", "—"),
        "ubicacion":     d.get("ubicacion", "—"),
        "contacto":      d.get("contacto", "—"),
        "user_id":       user.id,
        "username":      user.username or "—",
        "ts":            datetime.now().strftime("%Y-%m-%d %H:%M UTC"),
    }

    # Enviar email (en hilo aparte para no bloquear)
    loop = asyncio.get_running_loop()
    exito = await loop.run_in_executor(None, email_sender.enviar_pedido, pedido)

    if exito:
        email_sender.registrar_envio(user.id)
        # Notificar al operador por Telegram también
        resumen_tg = (
            f"📦 *Nuevo pedido — EHP Bot*\n"
            f"──────────────────────\n"
            f"🔹 {pedido['prod_nombre']} × {pedido['cantidad']}\n"
            f"👤 {pedido['nombre']} — {pedido['ubicacion']}\n"
            f"📞 {pedido['contacto']}\n"
            f"🆔 @{pedido['username']} (`{pedido['user_id']}`)\n"
            f"🕐 {pedido['ts']}"
        )
        try:
            await q.get_bot().send_message(
                chat_id=config.OPERATOR_CHAT_ID,
                text=resumen_tg,
                parse_mode=ParseMode.MARKDOWN,
            )
        except Exception as exc:
            log.error("No se pudo notificar al operador: %s", exc)

        await q.edit_message_text(
            "✅ *¡Solicitud enviada!*\n\n"
            "El taller recibirá tu cotización y te contactará por el medio que indicaste. "
            "Normalmente respondemos el mismo día en horario hábil.\n\n"
            "¿Necesitas algo más?",
            parse_mode=ParseMode.MARKDOWN,
            reply_markup=kb_volver_menu(),
        )
    else:
        await q.edit_message_text(
            "❌ Hubo un problema al enviar tu solicitud. "
            "Por favor escribe directamente a info@ehp-saddles.com "
            "o a hatopascher@gmail.com con tu consulta.",
            reply_markup=kb_volver_menu(),
        )

    return MENU


async def cmd_cancelar(update: Update, ctx: ContextTypes.DEFAULT_TYPE) -> int:
    ctx.user_data.clear()
    await update.message.reply_text(
        "Proceso cancelado. ¿En qué puedo ayudarte?",
        reply_markup=kb_menu(),
    )
    return MENU


async def msg_fuera_de_flujo(update: Update, ctx: ContextTypes.DEFAULT_TYPE) -> None:
    """Mensajes de texto fuera del flujo de pedido."""
    await update.message.reply_text(
        "Usa los botones del menú para navegar el catálogo y hacer una solicitud. "
        "Escribe /start para comenzar.",
        reply_markup=kb_menu(),
    )


# ── Arranque ──────────────────────────────────────────────────────────────────

async def main() -> None:
    log.info("Iniciando EHP Bot (menú estructurado)…")

    async def post_init(app: Application) -> None:
        from telegram import BotCommand
        await app.bot.set_my_commands([
            BotCommand("start",    "Inicio / Menú principal"),
            BotCommand("cancelar", "Cancelar proceso actual"),
        ])

    app = (
        Application.builder()
        .token(config.TELEGRAM_TOKEN)
        .post_init(post_init)
        .build()
    )

    conv = ConversationHandler(
        entry_points=[CommandHandler("start", cmd_start)],
        states={
            MENU: [
                CallbackQueryHandler(cb_categoria, pattern=r"^cat:"),
                CallbackQueryHandler(cb_menu,      pattern=r"^back:menu$"),
            ],
            SHOW_CAT: [
                CallbackQueryHandler(cb_producto, pattern=r"^prod:"),
                CallbackQueryHandler(cb_menu,     pattern=r"^back:menu$"),
                CallbackQueryHandler(cb_categoria, pattern=r"^cat:"),
            ],
            SHOW_PROD: [
                CallbackQueryHandler(cb_order,    pattern=r"^order:"),
                CallbackQueryHandler(cb_categoria, pattern=r"^cat:"),
                CallbackQueryHandler(cb_menu,     pattern=r"^back:menu$"),
            ],
            ASK_NAME:     [MessageHandler(filters.TEXT & ~filters.COMMAND, ask_location)],
            ASK_LOCATION: [MessageHandler(filters.TEXT & ~filters.COMMAND, ask_quantity)],
            ASK_QUANTITY: [MessageHandler(filters.TEXT & ~filters.COMMAND, ask_contact)],
            ASK_CONTACT:  [MessageHandler(filters.TEXT & ~filters.COMMAND, mostrar_confirmacion)],
            CONFIRM: [
                CallbackQueryHandler(cb_confirmar, pattern=r"^confirm:"),
                CallbackQueryHandler(cb_menu,      pattern=r"^back:menu$"),
            ],
        },
        fallbacks=[CommandHandler("cancelar", cmd_cancelar)],
        allow_reentry=True,
    )

    app.add_handler(conv)
    app.add_handler(MessageHandler(filters.TEXT & ~filters.COMMAND, msg_fuera_de_flujo))

    log.info("Bot activo. Ctrl+C para detener.")
    async with app:
        await app.initialize()
        await app.start()
        await app.updater.start_polling(allowed_updates=Update.ALL_TYPES)
        await asyncio.Event().wait()
        await app.updater.stop()
        await app.stop()


if __name__ == "__main__":
    asyncio.run(main())
