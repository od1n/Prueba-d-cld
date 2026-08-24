"""
EHP Bot — Catálogo de productos.
Fuente: assets/products.js (sitio ehp-saddles.com).
Actualizar precios aquí o conectar al endpoint de SistemaGestion.
"""

# ── Precios en USD (actualizar manualmente o via /actualizar_precios) ─────────
# Formato: { codigo_EHP: precio_USD }
PRECIOS: dict[str, float | str] = {
    # Serie Trabajo — McCleallan
    "EHP-W14":     "consultar",   # McCleallan 14"
    "EHP-W12S":    "consultar",   # McCleallan 12" Bocado Pequeño
    "EHP-W12L":    "consultar",   # McCleallan 12" Bocado Grande
    "EHP-W11":     "consultar",   # McCleallan 11"
    "EHP-W10":     "consultar",   # McCleallan 10"
    "EHP-SB":      "consultar",   # Saca Botas
    "EHP-EST":     "consultar",   # Estribos (par)
    # Serie Deporte
    "EHP-S-PCO":   "consultar",   # Pico Colombiano
    "EHP-S-PCA":   "consultar",   # Pico Colombiano con Aleta
    "EHP-S-MEX":   "consultar",   # Mexicano
    "EHP-S-CHA":   "consultar",   # Charro
    "EHP-S-CHA-AL":"consultar",   # Charro Pico Aluminio
    # Serie Recreativa
    "EHP-L-WES":   "consultar",   # Western
    "EHP-L-TEJ":   "consultar",   # Tejano
    "EHP-L-AUS":   "consultar",   # Australiano
    # Moldes a medida
    "EHP-CUSTOM":  "consultar",   # Molde personalizado
}

# ── Catálogo completo ─────────────────────────────────────────────────────────
PRODUCTOS: list[dict] = [
    # ── Trabajo / Work ────────────────────────────────────────────────────────
    {
        "id": "mccleallan-14",
        "nombre": "McCleallan 14\"",
        "codigo": "EHP-W14",
        "categoria": "trabajo",
        "peso": "3,2 kg",
        "descripcion": (
            "La McCleallan más grande de la serie. Diseñada para jinetes adultos "
            "y jornadas largas de trabajo. Árbol moldeado en una sola pieza de "
            "polietileno de alta densidad, forrado en cuero y terminado a mano."
        ),
        "medidas": {
            "Largo superior asiento": "42 cm",
            "Ancho frontal": "29 cm",
            "Ancho total": "35 cm",
            "Largo total": "53 cm",
            "Altura central": "16 cm",
            "Altura total": "14 cm",
        },
    },
    {
        "id": "mccleallan-12-pequeno",
        "nombre": "McCleallan 12\" Bocado y Hueco Pequeño",
        "codigo": "EHP-W12S",
        "categoria": "trabajo",
        "peso": "2,8 kg",
        "descripcion": (
            "Apero de trabajo compacto y ligero. Ideal para jinetes de complexión "
            "media en faena de sabana y hacienda. Polietileno moldeado en una pieza."
        ),
        "medidas": {
            "Largo superior asiento": "37 cm",
            "Ancho frontal": "27,5 cm",
            "Ancho total": "33,1 cm",
            "Largo total": "48,5 cm",
            "Altura central": "15,5 cm",
            "Altura total": "13 cm",
        },
    },
    {
        "id": "mccleallan-12-grande",
        "nombre": "McCleallan 12\" Bocado y Hueco Grande",
        "codigo": "EHP-W12L",
        "categoria": "trabajo",
        "peso": "2,9 kg",
        "descripcion": (
            "Misma base que la 12\" pequeña pero con bocado y hueco grandes, "
            "para jinetes que prefieren mayor ajuste en el pomo. "
            "Polietileno moldeado, resistente a humedad y sol del llano."
        ),
        "medidas": {
            "Largo superior asiento": "37 cm",
            "Ancho frontal": "28 cm",
            "Ancho total": "33,5 cm",
            "Largo total": "48,5 cm",
            "Altura central": "16 cm",
            "Altura total": "14 cm",
        },
    },
    {
        "id": "mccleallan-11",
        "nombre": "McCleallan 11\"",
        "codigo": "EHP-W11",
        "categoria": "trabajo",
        "peso": "2,5 kg",
        "descripcion": (
            "McCleallan compacta para jinete juvenil o de complexión ligera. "
            "Conserva todas las ventajas de la serie en un tamaño más contenido."
        ),
        "medidas": {
            "Largo superior asiento": "34 cm",
            "Ancho frontal": "25 cm",
            "Ancho total": "31 cm",
            "Largo total": "45 cm",
            "Altura central": "14,5 cm",
            "Altura total": "12 cm",
        },
    },
    {
        "id": "mccleallan-10",
        "nombre": "McCleallan 10\"",
        "codigo": "EHP-W10",
        "categoria": "trabajo",
        "peso": "2,3 kg",
        "descripcion": (
            "La McCleallan más pequeña. Para jinetes jóvenes que están empezando. "
            "Ligera, balanceada y virtualmente indestructible."
        ),
        "medidas": {
            "Largo superior asiento": "31 cm",
            "Ancho frontal": "23 cm",
            "Ancho total": "28,5 cm",
            "Largo total": "42 cm",
            "Altura central": "13,5 cm",
            "Altura total": "11 cm",
        },
    },
    {
        "id": "saca-botas",
        "nombre": "Saca Botas",
        "codigo": "EHP-SB",
        "categoria": "accesorios",
        "peso": "0,9 kg",
        "descripcion": (
            "Saca botas de polietileno moldeado en una pieza. "
            "Resistente, imposible de partir, no se raja con el sol. "
            "Un clásico del corredor del hato."
        ),
        "medidas": {},
    },
    {
        "id": "estribos",
        "nombre": "Estribos (par)",
        "codigo": "EHP-EST",
        "categoria": "accesorios",
        "peso": "1,4 kg (par)",
        "descripcion": (
            "Estribos moldeados en polietileno de alta densidad. "
            "Compatibles con toda la línea EHP. Base plana con pisa antideslizante, "
            "armazón de una sola pieza."
        ),
        "medidas": {},
    },
    # ── Deporte / Sport ───────────────────────────────────────────────────────
    {
        "id": "pico-colombiano",
        "nombre": "Pico Colombiano",
        "codigo": "EHP-S-PCO",
        "categoria": "deporte",
        "peso": "4,1 kg",
        "descripcion": (
            "Montura de pico alto al estilo de las cabalgatas colombianas. "
            "Asiento reforzado para tranco largo, pomo elevado para firmeza en subidas."
        ),
        "medidas": {
            "Largo superior asiento": "47 cm",
            "Ancho frontal": "31 cm",
            "Ancho total": "39 cm",
            "Largo total": "61 cm",
            "Altura central": "18 cm",
            "Altura total": "18 cm",
        },
    },
    {
        "id": "pico-colombiano-aleta",
        "nombre": "Pico Colombiano con Aleta",
        "codigo": "EHP-S-PCA",
        "categoria": "deporte",
        "peso": "4,4 kg",
        "descripcion": (
            "Variante del Pico Colombiano con aleta lateral extendida. "
            "Mayor protección para la pierna del jinete y los flancos del caballo."
        ),
        "medidas": {
            "Largo superior asiento": "47 cm",
            "Ancho frontal": "31 cm",
            "Ancho total": "39 cm",
            "Largo total": "61 cm",
            "Altura central": "18 cm",
            "Altura total": "18 cm",
        },
    },
    {
        "id": "mexicano",
        "nombre": "Mexicano",
        "codigo": "EHP-S-MEX",
        "categoria": "deporte",
        "peso": "5,0 kg",
        "descripcion": (
            "Montura estilo mexicano. Asiento profundo, pomo de cabeza ancha, "
            "borrén trasero elevado. Para trabajo ganadero y competencia ecuestre."
        ),
        "medidas": {
            "Largo superior asiento": "49 cm",
            "Ancho frontal": "33 cm",
            "Ancho total": "41 cm",
            "Largo total": "63 cm",
            "Altura central": "19 cm",
            "Altura total": "21 cm",
        },
    },
    {
        "id": "charro",
        "nombre": "Charro",
        "codigo": "EHP-S-CHA",
        "categoria": "deporte",
        "peso": "5,2 kg",
        "descripcion": (
            "Montura charra mexicana con pomo alto de cuerno clásico. "
            "Ideal para exhibición y deporte charro."
        ),
        "medidas": {
            "Largo superior asiento": "50 cm",
            "Ancho frontal": "33 cm",
            "Ancho total": "41 cm",
            "Largo total": "64 cm",
            "Altura central": "20 cm",
            "Altura total": "22 cm",
        },
    },
    {
        "id": "charro-aluminio",
        "nombre": "Charro Pico Aluminio",
        "codigo": "EHP-S-CHA-AL",
        "categoria": "deporte",
        "peso": "5,3 kg",
        "descripcion": (
            "Charro con pomo de aluminio fundido. Mayor rigidez y brillo "
            "en el cuerno, buscado en competencia."
        ),
        "medidas": {
            "Largo superior asiento": "50 cm",
            "Ancho frontal": "33 cm",
            "Ancho total": "41 cm",
            "Largo total": "64 cm",
            "Altura central": "20 cm",
            "Altura total": "22 cm",
        },
    },
    # ── Recreativa / Leisure ──────────────────────────────────────────────────
    {
        "id": "western",
        "nombre": "Western",
        "codigo": "EHP-L-WES",
        "categoria": "recreativa",
        "peso": "5,8 kg",
        "descripcion": (
            "Montura western clásica. Pomo de cuerno, faldones rectangulares, "
            "asiento acolchado. Para paseo y trail largo."
        ),
        "medidas": {
            "Largo superior asiento": "51 cm",
            "Ancho frontal": "35 cm",
            "Ancho total": "43 cm",
            "Largo total": "66 cm",
            "Altura central": "19 cm",
            "Altura total": "20 cm",
        },
    },
    {
        "id": "tejano",
        "nombre": "Tejano",
        "codigo": "EHP-L-TEJ",
        "categoria": "recreativa",
        "peso": "5,5 kg",
        "descripcion": (
            "Montura texana para paseo y trail. Asiento cómodo, faldones amplios "
            "y pomo bajo. Acabado en cuero curtido."
        ),
        "medidas": {},
    },
    {
        "id": "australiano",
        "nombre": "Australiano",
        "codigo": "EHP-L-AUS",
        "categoria": "recreativa",
        "peso": "5,0 kg",
        "descripcion": (
            "Montura australiana de asiento profundo, con bloque delantero "
            "para mayor seguridad en terreno difícil. Muy popular para trail y senderismo."
        ),
        "medidas": {},
    },
]


def obtener_contexto_catalogo() -> str:
    """Genera un texto de contexto del catálogo para el prompt del LLM."""
    lineas = [
        "CATÁLOGO EHP SADDLES (El Hato Pascher — Acarigua, Venezuela)",
        "Material base: polietileno de alta densidad (HDPE) moldeado en una sola pieza.",
        "Todas las monturas llevan forrado en cuero y terminado a mano.",
        "Fabricamos por encargo. Tiempo de entrega típico: 2–4 semanas.",
        "Exportamos a Colombia, Ecuador, México y EE.UU. Precios en USD.",
        "",
    ]
    for p in PRODUCTOS:
        precio = PRECIOS.get(p["codigo"], "consultar")
        precio_str = f"${precio} USD" if isinstance(precio, (int, float)) else "precio a consultar"
        lineas.append(f"— {p['nombre']} ({p['codigo']}) | {p['categoria'].upper()} | {p['peso']} | {precio_str}")
        lineas.append(f"  {p['descripcion']}")
        if p["medidas"]:
            med = ", ".join(f"{k}: {v}" for k, v in p["medidas"].items())
            lineas.append(f"  Medidas: {med}")
        lineas.append("")
    return "\n".join(lineas)


def buscar_producto(query: str) -> list[dict]:
    """Busca productos por nombre, código o categoría."""
    q = query.lower()
    return [
        p for p in PRODUCTOS
        if q in p["nombre"].lower()
        or q in p["codigo"].lower()
        or q in p["categoria"].lower()
        or q in p["descripcion"].lower()
    ]
