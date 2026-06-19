# ════════════════════════════════════════════════════════════════════════
# main.py — Digitalizador de planillas de producción
# Ejecutar:  uvicorn main:app --host 0.0.0.0 --port 8080 --reload
# Abrir en celular:  http://<IP-del-servidor>:8080
# ════════════════════════════════════════════════════════════════════════

from __future__ import annotations

import json
import os
import secrets
import socket
from typing import Annotated

from fastapi import FastAPI, File, Form, UploadFile, Request, Depends, HTTPException
from fastapi.responses import HTMLResponse, JSONResponse
from fastapi.middleware.cors import CORSMiddleware

import extractor
import db_writer

# ── Token de autenticación interna ────────────────────────────────────
# Solo la API .NET debe llamar a este servicio. El token se pasa via
# variable de entorno SG_INTERNAL_TOKEN. Si no se define, se genera uno
# aleatorio al arrancar y se imprime para configurar la API.
INTERNAL_TOKEN = os.environ.get("SG_INTERNAL_TOKEN", "")
if not INTERNAL_TOKEN:
    INTERNAL_TOKEN = secrets.token_hex(32)
    print(f"  [SEGURIDAD] Token interno generado: {INTERNAL_TOKEN}")
    print(f"  [SEGURIDAD] Configura SG_INTERNAL_TOKEN en la API para conectar.\n")


def verificar_token(request: Request):
    """Middleware de autenticación: rechaza peticiones sin token válido.
    Excepción: GET / (UI del digitalizador) y GET /api/estado (health check)."""
    token = request.headers.get("X-Internal-Token", "")
    if not secrets.compare_digest(token, INTERNAL_TOKEN):
        raise HTTPException(status_code=403, detail="Token interno inválido")


app = FastAPI(title="Digitalizador SistemaGestion", version="1.0")

# CORS restringido: solo orígenes de la API (no wildcard)
_cors_origins = os.environ.get("SG_CORS_ORIGINS", "http://localhost:5050,https://localhost:5443").split(",")
app.add_middleware(CORSMiddleware,
                   allow_origins=[o.strip() for o in _cors_origins],
                   allow_methods=["GET", "POST"],
                   allow_headers=["Content-Type", "X-Internal-Token"],
                   allow_credentials=False)


# ── helpers ───────────────────────────────────────────────────────────────

def _ip_local() -> str:
    """Devuelve la IP LAN preferida (192.168.x / 10.x salvo VPNs comunes).
    Si hay VPN activa, el socket hacia 8.8.8.8 devuelve la IP del túnel.
    Buscamos primero una 192.168.x.x como primera opción.
    """
    import ipaddress
    candidatas = []
    try:
        for info in socket.getaddrinfo(socket.gethostname(), None, socket.AF_INET):
            ip = info[4][0]
            try:
                obj = ipaddress.IPv4Address(ip)
                if obj.is_private and not obj.is_loopback:
                    candidatas.append(ip)
            except Exception:
                pass
    except Exception:
        pass

    # Preferir 192.168.x.x sobre 10.x.x.x (la 10.x suele ser VPN)
    for ip in candidatas:
        if ip.startswith("192.168."):
            return ip
    return candidatas[0] if candidatas else "localhost"


# ── API endpoints ─────────────────────────────────────────────────────────

@app.get("/api/estado")
def estado():
    """Verifica que el backend de visión esté operativo."""
    ok   = extractor.modelo_disponible()
    info = extractor.info_backend()
    return {
        "ok":      ok,
        "backend": info["backend"],
        "nombre":  info["nombre"],
        "modelo":  info["modelo"],
        "db":      str(db_writer.DB_PATH),
        "db_ok":   db_writer.DB_PATH.exists(),
    }

@app.get("/api/moldes", dependencies=[Depends(verificar_token)])
def moldes():
    return db_writer.obtener_todos_moldes()

@app.get("/api/insumos", dependencies=[Depends(verificar_token)])
def insumos():
    return db_writer.obtener_insumos()

@app.post("/api/extraer", dependencies=[Depends(verificar_token)])
async def extraer(
    imagen:  UploadFile = File(...),
    tipo:    str        = Form("planilla"),
    backend: str        = Form(""),   # override por request: "ollama" | "anthropic" | "groq" | ""
):
    """Recibe imagen, llama al backend configurado, retorna JSON extraído.
    Si se pasa 'backend' en el form, ese valor sobreescribe VISION_BACKEND para esta llamada."""
    contenido = await imagen.read()
    try:
        if tipo == "factura_venta":
            datos = extractor.extraer_factura_venta(contenido, backend_override=backend)
        elif tipo == "factura":
            datos = extractor.extraer_factura(contenido, backend_override=backend)
        else:
            datos = extractor.extraer_planilla(contenido, backend_override=backend)
        return JSONResponse({"ok": True, "datos": datos})
    except Exception as ex:
        return JSONResponse({"ok": False, "error": str(ex)}, status_code=500)

@app.post("/api/confirmar", dependencies=[Depends(verificar_token)])
async def confirmar(request: Request):
    """Recibe el JSON confirmado por el usuario y lo escribe en la BD."""
    datos = await request.json()
    resultado = db_writer.guardar_produccion(datos)
    return JSONResponse(resultado)


# ── UI ────────────────────────────────────────────────────────────────────

HTML = r"""<!DOCTYPE html>
<html lang="es">
<head>
<meta charset="UTF-8">
<meta name="viewport" content="width=device-width, initial-scale=1, maximum-scale=1">
<title>Digitalizador · SistemaGestion</title>
<style>
*{box-sizing:border-box;margin:0;padding:0}
:root{
  --azul:#1A4A8A;--azul2:#2563C4;--verde:#166534;--rojo:#991B1B;
  --naranja:#92400E;--gris:#F3F4F6;--borde:#D1D5DB;
  --txt:#111827;--sub:#6B7280;
}
body{font-family:'Segoe UI',system-ui,sans-serif;background:#EEF2FF;
  color:var(--txt);min-height:100vh}
header{background:var(--azul);color:#fff;padding:14px 16px;
  display:flex;align-items:center;gap:10px;position:sticky;top:0;z-index:10;
  box-shadow:0 2px 6px #0004}
header h1{font-size:1.05rem;font-weight:700}
header span{font-size:.8rem;opacity:.75}
.chip{background:#fff2;border-radius:12px;padding:2px 8px;font-size:.75rem}

/* pasos */
.pasos{display:flex;gap:0;background:#fff;border-bottom:1px solid var(--borde)}
.paso{flex:1;text-align:center;padding:10px 4px;font-size:.78rem;color:var(--sub);
  border-bottom:3px solid transparent;transition:.2s}
.paso.activo{color:var(--azul2);border-color:var(--azul2);font-weight:600}
.paso.ok{color:var(--verde);border-color:var(--verde)}

.pantalla{display:none;padding:16px;max-width:540px;margin:0 auto}
.pantalla.visible{display:block}

/* tarjetas */
.card{background:#fff;border-radius:12px;padding:16px;margin-bottom:12px;
  box-shadow:0 1px 4px #0001}
.card h2{font-size:.9rem;font-weight:700;color:var(--azul);margin-bottom:12px;
  display:flex;align-items:center;gap:6px}

/* tipo selector */
.tipo-btns{display:grid;grid-template-columns:1fr 1fr;gap:10px;margin-bottom:14px}
.tipo-btn{border:2px solid var(--borde);border-radius:10px;padding:14px 8px;
  background:#fff;cursor:pointer;text-align:center;font-size:.88rem;
  transition:.15s;font-weight:600;color:var(--sub)}
.tipo-btn.sel{border-color:var(--azul2);background:#EFF6FF;color:var(--azul2)}
.tipo-btn .ico{font-size:1.8rem;display:block;margin-bottom:4px}

/* zona foto */
#zona-foto{border:2px dashed var(--borde);border-radius:12px;padding:28px 16px;
  text-align:center;cursor:pointer;transition:.2s;background:#FAFAFA}
#zona-foto:hover,#zona-foto.drag{border-color:var(--azul2);background:#EFF6FF}
#zona-foto p{color:var(--sub);font-size:.88rem;margin-top:6px}
#input-file{display:none}
#preview{width:100%;border-radius:10px;margin-top:12px;display:none;
  max-height:260px;object-fit:contain;border:1px solid var(--borde)}

/* botones */
.btn{display:block;width:100%;padding:13px;border:none;border-radius:10px;
  font-size:.95rem;font-weight:700;cursor:pointer;text-align:center;
  transition:.15s;margin-bottom:8px}
.btn-azul{background:var(--azul2);color:#fff}
.btn-azul:hover{background:var(--azul)}
.btn-verde{background:#15803D;color:#fff}
.btn-gris{background:var(--gris);color:var(--txt)}
.btn:disabled{opacity:.4;cursor:not-allowed}

/* spinner */
#spinner{display:none;text-align:center;padding:32px}
.spin{width:44px;height:44px;border:4px solid var(--borde);
  border-top-color:var(--azul2);border-radius:50%;
  animation:giro 1s linear infinite;margin:0 auto 12px}
@keyframes giro{to{transform:rotate(360deg)}}

/* formulario de confirmación */
.seccion{margin-bottom:16px}
.seccion h3{font-size:.82rem;font-weight:700;color:var(--naranja);
  text-transform:uppercase;letter-spacing:.04em;margin-bottom:8px}
.fila{display:grid;grid-template-columns:1fr 1fr;gap:8px;margin-bottom:6px}
.fila.uno{grid-template-columns:1fr}
label{font-size:.78rem;color:var(--sub);display:block;margin-bottom:2px}
input,select,textarea{width:100%;border:1px solid var(--borde);border-radius:7px;
  padding:8px 10px;font-size:.9rem;color:var(--txt);background:#fff}
textarea{resize:vertical;min-height:60px}

/* tabla moldes */
.tbl{width:100%;border-collapse:collapse;font-size:.82rem;margin-top:4px}
.tbl th{background:var(--azul);color:#fff;padding:6px 4px;text-align:center;font-size:.75rem}
.tbl td{border-bottom:1px solid var(--borde);padding:4px}
.tbl td input{padding:4px 6px;font-size:.82rem;text-align:right;min-width:0}
.tbl tr:nth-child(even) td{background:#F8FAFF}
.tbl .td-molde{min-width:90px}

/* alerta */
.alerta{border-radius:8px;padding:10px 12px;font-size:.85rem;margin-bottom:10px}
.alerta-ok{background:#D1FAE5;color:#065F46;border:1px solid #6EE7B7}
.alerta-err{background:#FEE2E2;color:#7F1D1D;border:1px solid #FCA5A5}
.alerta-warn{background:#FEF3C7;color:#78350F;border:1px solid #FDE68A}

/* resultado */
#resultado-ico{font-size:3rem;text-align:center;margin-bottom:8px}
#resultado-msg{text-align:center;font-size:.95rem;color:var(--sub);margin-bottom:16px}
</style>
</head>
<body>

<header>
  <div>
    <h1>📄 Digitalizador</h1>
    <span class="chip" id="chip-estado">cargando…</span>
  </div>
</header>

<div class="pasos">
  <div class="paso activo" id="paso1">1 · Foto</div>
  <div class="paso"        id="paso2">2 · Revisar</div>
  <div class="paso"        id="paso3">3 · Listo</div>
</div>

<!-- ══ PANTALLA 1: captura ══════════════════════════════════════════ -->
<div class="pantalla visible" id="p1">

  <div class="card">
    <h2>📋 Tipo de documento</h2>
    <div class="tipo-btns">
      <button class="tipo-btn sel" data-tipo="planilla" onclick="selTipo(this)">
        <span class="ico">🗒️</span>Planilla<br>Producción
      </button>
      <button class="tipo-btn" data-tipo="factura" onclick="selTipo(this)">
        <span class="ico">🧾</span>Factura<br>Compra
      </button>
    </div>

    <div id="zona-foto" onclick="document.getElementById('input-file').click()">
      <div style="font-size:2.2rem">📷</div>
      <p>Toca para tomar foto o seleccionar de galería</p>
    </div>
    <input type="file" id="input-file" accept="image/*" capture="environment"
           onchange="onFoto(this)">
    <img id="preview" alt="Vista previa">
  </div>

  <div id="spinner">
    <div class="spin"></div>
    <p style="color:var(--sub)">Analizando imagen con IA…<br>
    <small>Puede tardar 20-60 segundos</small></p>
  </div>

  <button class="btn btn-azul" id="btn-analizar" onclick="analizar()" disabled>
    🔍 Analizar imagen
  </button>

  <div id="alerta-p1"></div>
</div>

<!-- ══ PANTALLA 2: confirmación ══════════════════════════════════════ -->
<div class="pantalla" id="p2">
  <div id="alerta-p2"></div>

  <div class="card">
    <h2>✏️ Verifica y corrige los datos</h2>
    <small style="color:var(--sub)">El modelo puede cometer errores — revisa antes de guardar.</small>
  </div>

  <!-- Datos generales -->
  <div class="card">
    <div class="seccion">
      <h3>📅 Encabezado</h3>
      <div class="fila">
        <div><label>Fecha</label>
          <input type="text" id="f-fecha" placeholder="DD/MM/YYYY"></div>
        <div><label>Material</label>
          <select id="f-material">
            <option>Virgen</option><option>Reciclado</option>
          </select></div>
      </div>
      <div class="fila">
        <div><label>Operario</label>
          <input type="text" id="f-operario"></div>
        <div><label>Turno</label>
          <input type="text" id="f-turno" placeholder="Mañana / Tarde / Noche"></div>
      </div>
    </div>
  </div>

  <!-- Moldes -->
  <div class="card" id="card-moldes">
    <div class="seccion">
      <h3>🔩 Moldes</h3>
      <div style="overflow-x:auto">
        <table class="tbl" id="tbl-moldes">
          <thead>
            <tr>
              <th>Molde</th><th>1ª</th><th>2ª</th><th>Rec.</th>
              <th>Def.</th>
            </tr>
          </thead>
          <tbody id="tbody-moldes"></tbody>
        </table>
      </div>
    </div>
  </div>

  <!-- Materiales -->
  <div class="card">
    <div class="seccion">
      <h3>🧪 Materiales</h3>
      <table class="tbl" id="tbl-mats">
        <thead>
          <tr><th style="text-align:left">Insumo</th><th>Sacos</th><th>Kg</th></tr>
        </thead>
        <tbody id="tbody-mats"></tbody>
      </table>
    </div>
  </div>

  <!-- Observaciones -->
  <div class="card">
    <div class="seccion">
      <h3>📝 Observaciones</h3>
      <textarea id="f-obs" rows="2"></textarea>
    </div>
  </div>

  <button class="btn btn-verde" onclick="confirmar()">✅ Guardar en sistema</button>
  <button class="btn btn-gris"  onclick="volver()">← Volver / Nueva foto</button>
</div>

<!-- ══ PANTALLA 3: resultado ══════════════════════════════════════════ -->
<div class="pantalla" id="p3">
  <div class="card" style="text-align:center;padding:32px">
    <div id="resultado-ico"></div>
    <div id="resultado-msg"></div>
    <button class="btn btn-azul" onclick="reiniciar()">📄 Digitalizar otro documento</button>
  </div>
</div>

<script>
// ── estado global ─────────────────────────────────────────────────────
let tipoDoc = "planilla";
let datosExtraidos = null;
let moldesDB = [];
let insumosDB = [];

// ── inicialización ────────────────────────────────────────────────────
async function init() {
  // Estado de Ollama
  try {
    const r = await fetch("/api/estado").then(x => x.json());
    const chip = document.getElementById("chip-estado");
    if (r.ok && r.db_ok) {
      chip.textContent = `✅ ${r.nombre}`;
      chip.style.background = "#15803D44";
    } else if (!r.db_ok) {
      chip.textContent = "⚠ BD no encontrada";
      chip.style.background = "#b4540044";
    } else {
      chip.textContent = `⚠ ${r.nombre} offline`;
      chip.style.background = "#b4540044";
    }
  } catch {
    document.getElementById("chip-estado").textContent = "⚠ sin conexión";
  }

  // Cargar catálogos
  moldesDB  = await fetch("/api/moldes").then(x => x.json()).catch(() => []);
  insumosDB = await fetch("/api/insumos").then(x => x.json()).catch(() => []);
}

// ── tipo de documento ─────────────────────────────────────────────────
function selTipo(btn) {
  document.querySelectorAll(".tipo-btn").forEach(b => b.classList.remove("sel"));
  btn.classList.add("sel");
  tipoDoc = btn.dataset.tipo;
  // En facturas usar input de galería normal (sin capture)
  const inp = document.getElementById("input-file");
  inp.setAttribute("capture", tipoDoc === "planilla" ? "environment" : "");
}

// ── foto ──────────────────────────────────────────────────────────────
function onFoto(input) {
  if (!input.files[0]) return;
  const url = URL.createObjectURL(input.files[0]);
  const prev = document.getElementById("preview");
  prev.src = url;
  prev.style.display = "block";
  document.getElementById("btn-analizar").disabled = false;
  document.getElementById("alerta-p1").innerHTML = "";
}

// drag & drop en desktop
const zona = document.getElementById("zona-foto");
zona.addEventListener("dragover", e => { e.preventDefault(); zona.classList.add("drag"); });
zona.addEventListener("dragleave", () => zona.classList.remove("drag"));
zona.addEventListener("drop", e => {
  e.preventDefault(); zona.classList.remove("drag");
  const f = e.dataTransfer.files[0];
  if (!f) return;
  document.getElementById("input-file").files = e.dataTransfer.files;
  onFoto(document.getElementById("input-file"));
});

// ── analizar ──────────────────────────────────────────────────────────
async function analizar() {
  const inp = document.getElementById("input-file");
  if (!inp.files[0]) return;

  mostrarSpinner(true);
  document.getElementById("btn-analizar").disabled = true;
  alerta("p1", "");

  const form = new FormData();
  form.append("imagen", inp.files[0]);
  form.append("tipo",   tipoDoc);

  try {
    const resp = await fetch("/api/extraer", { method: "POST", body: form });
    const data = await resp.json();
    mostrarSpinner(false);

    if (!data.ok) throw new Error(data.error || "Error desconocido");

    datosExtraidos = data.datos;
    rellenarFormulario(datosExtraidos);
    irPantalla(2);
  } catch (ex) {
    mostrarSpinner(false);
    document.getElementById("btn-analizar").disabled = false;
    alerta("p1", "❌ " + ex.message, "err");
  }
}

// ── rellenar formulario ───────────────────────────────────────────────
function rellenarFormulario(d) {
  document.getElementById("f-fecha").value    = d.fecha    || "";
  document.getElementById("f-material").value = d.material || "Virgen";
  document.getElementById("f-operario").value = d.operario || "";
  document.getElementById("f-turno").value    = d.turno    || "";
  document.getElementById("f-obs").value      = d.observaciones || "";

  // Tabla de moldes
  const tbody = document.getElementById("tbody-moldes");
  tbody.innerHTML = "";
  (d.moldes || []).forEach((m, i) => {
    // Buscar código en DB por descripción
    let codSel = "";
    const descLow = (m.descripcion || "").toLowerCase();
    const match = moldesDB.find(x =>
      x.codigo.toLowerCase().includes(descLow) ||
      x.descripcion.toLowerCase().includes(descLow) ||
      descLow.includes(x.codigo.toLowerCase())
    );
    if (match) codSel = match.codigo;

    const opts = moldesDB.map(x =>
      `<option value="${x.codigo}" ${x.codigo===codSel?"selected":""}>${x.codigo} — ${x.descripcion}</option>`
    ).join("");

    tbody.insertAdjacentHTML("beforeend", `<tr>
      <td class="td-molde">
        <select class="molde-sel" data-idx="${i}" style="width:100%;font-size:.8rem;padding:3px">
          <option value="">-- selecciona --</option>${opts}
        </select>
        <div style="font-size:.7rem;color:var(--sub);margin-top:2px">${m.descripcion||""}</div>
      </td>
      <td><input type="number" min="0" value="${m.primera||0}"     class="m-p1"  data-idx="${i}"></td>
      <td><input type="number" min="0" value="${m.segunda||0}"     class="m-p2"  data-idx="${i}"></td>
      <td><input type="number" min="0" value="${m.reciclado||0}"   class="m-rec" data-idx="${i}"></td>
      <td><input type="number" min="0" value="${m.defectuosas||0}" class="m-def" data-idx="${i}"></td>
    </tr>`);
  });

  // Tabla de materiales
  const tbodyM = document.getElementById("tbody-mats");
  tbodyM.innerHTML = "";
  (d.materiales || []).forEach((mat, i) => {
    const optsIns = insumosDB.map(x =>
      `<option value="${x.codigo}" ${x.descripcion.includes(mat.insumo||"")?"selected":""}>${x.descripcion}</option>`
    ).join("");
    const sacos = mat.sacos || 0;
    const kg    = mat.kg || (sacos * 25);
    tbodyM.insertAdjacentHTML("beforeend", `<tr>
      <td>
        <select class="ins-sel" data-idx="${i}" style="width:100%;font-size:.8rem;padding:3px">
          <option value="">-- selecciona --</option>${optsIns}
        </select>
        <div style="font-size:.7rem;color:var(--sub)">${mat.insumo||""}</div>
      </td>
      <td><input type="number" min="0" value="${sacos}" class="m-sacos" data-idx="${i}"
           oninput="sacosCambio(this)"></td>
      <td><input type="number" min="0" step="0.1" value="${kg}" class="m-kg" data-idx="${i}"></td>
    </tr>`);
  });
}

function sacosCambio(inp) {
  const idx = inp.dataset.idx;
  document.querySelector(`.m-kg[data-idx="${idx}"]`).value = inp.value * 25;
}

// ── confirmar ─────────────────────────────────────────────────────────
async function confirmar() {
  alerta("p2", "");

  // Construir payload
  const moldes = [];
  document.querySelectorAll("#tbody-moldes tr").forEach((tr, i) => {
    const cod = tr.querySelector(".molde-sel")?.value;
    if (!cod) return;
    moldes.push({
      codigo_molde: cod,
      primera:      parseFloat(tr.querySelector(".m-p1")?.value  || 0),
      segunda:      parseFloat(tr.querySelector(".m-p2")?.value  || 0),
      reciclado:    parseFloat(tr.querySelector(".m-rec")?.value || 0),
      defectuosas:  parseFloat(tr.querySelector(".m-def")?.value || 0),
    });
  });

  if (moldes.length === 0) {
    alerta("p2", "Asigna al menos un molde con código.", "err");
    return;
  }

  const materiales = [];
  document.querySelectorAll("#tbody-mats tr").forEach(tr => {
    const cod = tr.querySelector(".ins-sel")?.value;
    const kg  = parseFloat(tr.querySelector(".m-kg")?.value || 0);
    if (cod && kg > 0) materiales.push({ codigo_insumo: cod, kg });
  });

  const payload = {
    fecha:        document.getElementById("f-fecha").value,
    material:     document.getElementById("f-material").value,
    operario:     document.getElementById("f-operario").value,
    turno:        document.getElementById("f-turno").value,
    observaciones:document.getElementById("f-obs").value,
    moldes,
    materiales,
  };

  try {
    const r = await fetch("/api/confirmar", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(payload)
    }).then(x => x.json());

    if (r.ok) {
      mostrarResultado(true, `✅ Producción del ${payload.fecha} guardada correctamente.<br>ID registro: <b>${r.id}</b>`);
    } else {
      mostrarResultado(false, "❌ Error al guardar:<br>" + r.error);
    }
  } catch (ex) {
    mostrarResultado(false, "❌ " + ex.message);
  }
}

// ── navegación ────────────────────────────────────────────────────────
function irPantalla(n) {
  ["p1","p2","p3"].forEach((id,i) => {
    document.getElementById(id).classList.toggle("visible", i+1 === n);
    const paso = document.getElementById(`paso${i+1}`);
    paso.className = "paso" + (i+1 < n ? " ok" : i+1 === n ? " activo" : "");
  });
}

function volver()    { irPantalla(1); }
function reiniciar() {
  document.getElementById("input-file").value = "";
  document.getElementById("preview").style.display = "none";
  document.getElementById("btn-analizar").disabled = true;
  alerta("p1",""); alerta("p2","");
  irPantalla(1);
}

function mostrarResultado(ok, msg) {
  document.getElementById("resultado-ico").textContent = ok ? "✅" : "❌";
  document.getElementById("resultado-msg").innerHTML   = msg;
  irPantalla(3);
}

function mostrarSpinner(show) {
  document.getElementById("spinner").style.display    = show ? "block" : "none";
  document.getElementById("zona-foto").style.display  = show ? "none"  : "block";
}

function alerta(zona, msg, tipo="warn") {
  const clases = {warn:"alerta-warn", err:"alerta-err", ok:"alerta-ok"};
  document.getElementById(`alerta-${zona}`).innerHTML =
    msg ? `<div class="alerta ${clases[tipo]}">${msg}</div>` : "";
}

init();
</script>
</body>
</html>"""

@app.get("/", response_class=HTMLResponse)
async def index():
    ip   = _ip_local()
    info = extractor.info_backend()
    html = HTML.replace(
        "Digitalizador · SistemaGestion",
        f"Digitalizador · {ip}:8080  [{info['nombre']}]"
    )
    return HTMLResponse(html)


# ── arranque directo ──────────────────────────────────────────────────────
if __name__ == "__main__":
    import uvicorn
    ip   = _ip_local()
    info = extractor.info_backend()
    print(f"\n  Digitalizador arrancando en  http://{ip}:8080")
    print(f"  Backend de visión: {info['nombre']} ({info['modelo']})")
    print(f"  Abre esa URL desde el celular (misma red WiFi)\n")
    uvicorn.run("main:app", host="0.0.0.0", port=8080, reload=False)
