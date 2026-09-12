// egresos-nuevo.js — Flujo OCR de 3 pasos para registro de egresos
// SistemaGestion Portal

'use strict';

// ── Estado global ────────────────────────────────────────────────────────────
let imagenFile     = null;    // File seleccionado
let datosOCR       = null;    // Respuesta cruda de la IA
let categorias     = [];      // Lista de CategoriasGasto
let filas          = [];      // Array de objetos {id, desc, idArt, cod, tipo, cant, pu, sub}
let filaIdSeq      = 0;       // Secuencia para IDs únicos de filas
let modalFilaId    = null;    // ID de fila que abrió el modal de búsqueda
let tasaBCV        = 0;
let configVision   = { backend: 'ollama', digitalizadorOnline: false, modeloListo: false, nombre: '', modelo: '' };

// ── Refs DOM ─────────────────────────────────────────────────────────────────
const dropzone     = document.getElementById('dropzone');
const fileInput    = document.getElementById('fileInput');
const dropContent  = document.getElementById('dropContent');
const previewWrap  = document.getElementById('previewWrap');
const previewImg   = document.getElementById('previewImg');
const btnClearImg  = document.getElementById('btnClearImg');
const btnExtraer   = document.getElementById('btnExtraer');
const loader1      = document.getElementById('loader1');
const error1       = document.getElementById('error1');

const panel1       = document.getElementById('panel1');
const panel2       = document.getElementById('panel2');
const panel3       = document.getElementById('panel3');
const stepsBar     = document.getElementById('stepsBar');

const fProveedor   = document.getElementById('fProveedor');
const fNroDoc      = document.getElementById('fNroDoc');
const fFecha       = document.getElementById('fFecha');
const fCategoria   = document.getElementById('fCategoria');
const fMoneda      = document.getElementById('fMoneda');
const fTasa        = document.getElementById('fTasa');
const fTotalUSD    = document.getElementById('fTotalUSD');
const fTotalBs     = document.getElementById('fTotalBs');
const fNotas       = document.getElementById('fNotas');
const btnAddLinea  = document.getElementById('btnAddLinea');
const tablaDetalles= document.getElementById('tablaDetalles');
const fPrecioPendiente = document.getElementById('fPrecioPendiente');
const fACredito    = document.getElementById('fACredito');
const fAbonado     = document.getElementById('fAbonado');
const fVence       = document.getElementById('fVence');
const zonaPago     = document.getElementById('zonaPago');
const zonaCredito  = document.getElementById('zonaCredito');
const btnVolver1   = document.getElementById('btnVolver1');
const btnIrPaso3   = document.getElementById('btnIrPaso3');
const error2       = document.getElementById('error2');

const resumenFinal = document.getElementById('resumenFinal');
const btnVolver2   = document.getElementById('btnVolver2');
const btnGuardar   = document.getElementById('btnGuardar');
const loader3      = document.getElementById('loader3');
const error3       = document.getElementById('error3');
const success3     = document.getElementById('success3');

const modalOverlay = document.getElementById('modalOverlay');
const modalBuscar  = document.getElementById('modalBuscar');
const modalResultados = document.getElementById('modalResultados');
const btnCerrarModal  = document.getElementById('btnCerrarModal');

// ── Utilidades ───────────────────────────────────────────────────────────────
function mostrarError(el, msg) {
    el.textContent = msg;
    el.style.display = 'block';
}
function ocultarError(el) { el.style.display = 'none'; }

function irPaso(n) {
    [panel1, panel2, panel3].forEach((p, i) => p.classList.toggle('active', i + 1 === n));
    document.querySelectorAll('.eg-step').forEach((s, i) => {
        const num = i + 1;
        s.classList.remove('active', 'done');
        if (num === n) s.classList.add('active');
        else if (num < n) s.classList.add('done');
    });
    window.scrollTo({ top: 0, behavior: 'smooth' });
}

function fmt2(n) {
    const v = parseFloat(n) || 0;
    return v.toLocaleString('es-VE', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
}

// ── Config de visión (digitalizador + backend) ───────────────────────────────
async function cargarConfigVision() {
    try {
        const r = await fetch('/api/egresos/config-vision', { credentials: 'include' });
        if (!r.ok) return;
        configVision = await r.json();
    } catch { /* silencioso */ }
    renderChipVision();
}

function renderChipVision() {
    const zona = document.getElementById('zonaChipVision');
    if (!zona) return;

    const esAdmin   = Auth.getUser()?.rol === 'Admin';
    const online    = configVision.digitalizadorOnline;
    const listo     = configVision.modeloListo;
    const backend   = configVision.backend || 'ollama';
    const nombre    = configVision.nombre  || 'Ollama local';
    const modelo    = configVision.modelo  || '';

    // Tres estados:
    //  ok      → servicio activo + modelo listo
    //  warning → servicio activo pero modelo no responde (Ollama no cargado, etc.)
    //  offline → proceso Python no está corriendo
    let chipClass, chipTxt;
    if (!online) {
        chipClass = 'vision-chip offline';
        chipTxt   = '⚠ Digitalizador offline';
    } else if (!listo) {
        chipClass = 'vision-chip warning';
        chipTxt   = `⚡ ${nombre}${modelo ? ' · ' + modelo : ''} — modelo no disponible`;
    } else {
        chipClass = 'vision-chip ok';
        chipTxt   = `✅ ${nombre}${modelo ? ' · ' + modelo : ''}`;
    }

    let selectorHtml = '';
    if (esAdmin) {
        const opOllama    = backend === 'ollama'     ? 'selected' : '';
        const opAnthropic = backend === 'anthropic'  ? 'selected' : '';
        const opGroq      = backend === 'groq'       ? 'selected' : '';
        selectorHtml = `
            <div class="vision-backend-row">
              <label class="vision-backend-label">Backend OCR</label>
              <select id="selectBackend" class="vision-backend-select">
                <option value="ollama"    ${opOllama}>Ollama (local)</option>
                <option value="anthropic" ${opAnthropic}>Anthropic (Claude)</option>
                <option value="groq"      ${opGroq}>Groq</option>
              </select>
              <span class="vision-admin-badge">Admin</span>
            </div>`;
    } else if (backend) {
        const nombres = { ollama: 'Ollama (local)', anthropic: 'Anthropic (Claude)', groq: 'Groq' };
        selectorHtml = `<div class="vision-backend-row">
            <label class="vision-backend-label">Backend OCR</label>
            <span class="vision-backend-readonly">${nombres[backend] || backend}</span>
          </div>`;
    }

    zona.innerHTML = `<div class="${chipClass}">${chipTxt}</div>${selectorHtml}`;

    // Evento del selector (solo admin)
    const sel = document.getElementById('selectBackend');
    if (sel) {
        sel.addEventListener('change', async () => {
            const nuevo = sel.value;
            try {
                const r = await fetch('/api/egresos/config-vision', {
                    method: 'PUT',
                    headers: { 'Content-Type': 'application/json' },
                    credentials: 'include',
                    body: JSON.stringify({ backend: nuevo })
                });
                if (r.ok) {
                    configVision.backend = nuevo;
                    // Actualizar nombre visible
                    const nombres = { ollama: 'Ollama local', anthropic: 'Anthropic', groq: 'Groq' };
                    configVision.nombre = nombres[nuevo] || nuevo;
                    renderChipVision();
                } else {
                    sel.value = configVision.backend; // revertir
                    const err = await r.json().catch(() => ({}));
                    mostrarError(error1, err.error || 'No se pudo cambiar el backend.');
                }
            } catch {
                sel.value = configVision.backend;
            }
        });
    }
}

// ── Paso 1: carga y OCR ──────────────────────────────────────────────────────
// Ícono PDF (data-URI) para previsualizar documentos PDF, que no se pueden
// mostrar en una etiqueta <img> como las imágenes.
const ICONO_PDF = 'data:image/svg+xml;utf8,' + encodeURIComponent(
    '<svg xmlns="http://www.w3.org/2000/svg" width="120" height="150" viewBox="0 0 120 150">' +
    '<rect x="4" y="4" width="112" height="142" rx="8" fill="#f4f4f5" stroke="#c0392b" stroke-width="3"/>' +
    '<text x="60" y="88" font-family="Segoe UI, Arial" font-size="30" font-weight="bold" fill="#c0392b" text-anchor="middle">PDF</text>' +
    '</svg>');

function seleccionarImagen(file) {
    const esPdf = file && (file.type === 'application/pdf' || /\.pdf$/i.test(file.name || ''));
    const esImg = file && file.type.startsWith('image/');
    if (!file || (!esImg && !esPdf)) {
        mostrarError(error1, 'El archivo debe ser una imagen (JPG, PNG, WEBP) o un PDF.');
        return;
    }
    if (file.size > 20 * 1024 * 1024) {
        mostrarError(error1, 'El archivo supera el límite de 20 MB.');
        return;
    }
    ocultarError(error1);
    imagenFile = file;
    // Los PDF no se pueden mostrar en <img>: usamos un ícono.
    previewImg.src = esPdf ? ICONO_PDF : URL.createObjectURL(file);
    previewImg.alt = esPdf ? (file.name || 'Documento PDF') : 'Vista previa';
    dropContent.style.display = 'none';
    previewWrap.style.display = 'block';
    btnExtraer.disabled = false;
}

fileInput.addEventListener('change', () => {
    if (fileInput.files[0]) seleccionarImagen(fileInput.files[0]);
});

dropzone.addEventListener('dragover', e => { e.preventDefault(); dropzone.classList.add('dragover'); });
dropzone.addEventListener('dragleave', () => dropzone.classList.remove('dragover'));
dropzone.addEventListener('drop', e => {
    e.preventDefault();
    dropzone.classList.remove('dragover');
    if (e.dataTransfer.files[0]) seleccionarImagen(e.dataTransfer.files[0]);
});

btnClearImg.addEventListener('click', e => {
    e.stopPropagation();
    imagenFile = null;
    previewImg.src = '';
    fileInput.value = '';
    previewWrap.style.display = 'none';
    dropContent.style.display = '';
    btnExtraer.disabled = true;
    ocultarError(error1);
});

btnExtraer.addEventListener('click', async () => {
    if (!imagenFile) return;
    if (!configVision.digitalizadorOnline) {
        mostrarError(error1, 'El servicio de digitalización no está activo. Ejecuta: python main.py en la carpeta digitalizador.');
        return;
    }
    ocultarError(error1);
    btnExtraer.disabled = true;
    loader1.style.display = 'flex';

    try {
        const formData = new FormData();
        formData.append('imagen', imagenFile);

        const resp = await fetch('/api/egresos/extraer-documento', {
            method: 'POST',
            body: formData,
            credentials: 'include'
        });

        if (resp.status === 429) throw new Error('Límite de extracciones alcanzado. Espera un minuto e intenta de nuevo.');
        if (!resp.ok) {
            const err = await resp.json().catch(() => ({}));
            throw new Error(err.error || err.title || `Error ${resp.status}`);
        }

        const data = await resp.json();
        datosOCR = data.crudo ? null : data;  // si vino crudo, no parsear
        poblarPaso2(datosOCR || {});
        irPaso(2);
    } catch (ex) {
        mostrarError(error1, ex.message);
        btnExtraer.disabled = false;
    } finally {
        loader1.style.display = 'none';
    }
});

// ── Paso 2: poblar con datos OCR ─────────────────────────────────────────────
async function cargarCategorias() {
    try {
        const r = await fetch('/api/categorias-gasto', { credentials: 'include' });
        if (!r.ok) return;
        categorias = await r.json();
        fCategoria.innerHTML = '<option value="">— selecciona —</option>' +
            categorias.map(c =>
                `<option value="${c.id}">${c.nombre}${c.esMateriaPrima ? ' ⚙' : ''}</option>`
            ).join('');
    } catch { /* silencioso */ }
}

async function cargarTasaBCV() {
    try {
        const r = await fetch('/api/dashboard', { credentials: 'include' });
        if (!r.ok) return;
        const d = await r.json();
        // La tasa viene en d.tasaCambio.tasaBCV
        tasaBCV = parseFloat(d.tasaCambio?.tasaBCV || 0) || 0;
        if (tasaBCV > 0) fTasa.value = tasaBCV.toFixed(2);
    } catch { /* silencioso */ }
}

function poblarPaso2(ocr) {
    // Mostrar aviso si el modelo indicó que no encontró datos de factura
    // o si los campos clave vienen todos vacíos / en cero
    const sinDatos = ocr.encontrado === false
        || (!ocr.proveedor && !ocr.nroDocumento
            && (!ocr.total || ocr.total === 0)
            && (!Array.isArray(ocr.items) || ocr.items.length === 0));
    const avisoEl = document.getElementById('ocr-aviso');
    if (avisoEl) avisoEl.style.display = sinDatos ? 'block' : 'none';

    fProveedor.value  = ocr.proveedor    || '';
    fNroDoc.value     = ocr.nroDocumento || '';
    fFecha.value      = ocr.fecha        || new Date().toISOString().slice(0, 10);
    fTotalUSD.value   = parseFloat(ocr.total || 0).toFixed(2);
    fTotalBs.value    = '';
    fMoneda.value     = ocr.moneda === 'VES' ? 'VES' : 'USD';

    // Calcular conversión básica
    if (tasaBCV > 0) {
        if (fMoneda.value === 'USD' && ocr.total > 0) {
            fTotalBs.value = (parseFloat(ocr.total) * tasaBCV).toFixed(2);
        } else if (fMoneda.value === 'VES' && ocr.total > 0) {
            fTotalUSD.value = (parseFloat(ocr.total) / tasaBCV).toFixed(2);
            fTotalBs.value  = parseFloat(ocr.total).toFixed(2);
        }
    }

    // Construir filas a partir de items OCR
    filas = [];
    const items = Array.isArray(ocr.items) ? ocr.items : [];
    if (items.length > 0) {
        items.forEach(it => agregarFila({
            desc : it.descripcion || '',
            cant : parseFloat(it.cantidad || 1),
            pu   : parseFloat(it.precioUnitario || 0),
            sub  : parseFloat(it.subtotal || (it.cantidad * it.precioUnitario) || 0)
        }));
    } else {
        agregarFila({});
    }
    renderTabla();
}

// ── Gestión de filas ─────────────────────────────────────────────────────────
function agregarFila(datos = {}) {
    const id = ++filaIdSeq;
    filas.push({
        id,
        desc  : datos.desc  || '',
        idArt : datos.idArt || null,
        artNombre: datos.artNombre || '',
        cod   : datos.cod   || '',
        tipo  : datos.tipo  || '',
        cant  : datos.cant  || 1,
        pu    : datos.pu    || 0,
        sub   : datos.sub   || 0
    });
    return id;
}

function eliminarFila(id) {
    filas = filas.filter(f => f.id !== id);
    renderTabla();
}

function actualizarSubtotal(id) {
    const f = filas.find(f => f.id === id);
    if (!f) return;
    f.sub = Math.round(f.cant * f.pu * 100) / 100;
    const elSub = document.getElementById(`sub_${id}`);
    if (elSub) elSub.value = f.sub.toFixed(2);
}

function renderTabla() {
    tablaDetalles.innerHTML = '';

    // Encabezado
    const hdr = document.createElement('div');
    hdr.className = 'eg-detalle-header';
    hdr.innerHTML = `
        <span>Descripción / Artículo</span>
        <span>Cant.</span>
        <span>P. Unit.</span>
        <span>Subtotal</span>
        <span>Catálogo</span>
        <span></span>`;
    tablaDetalles.appendChild(hdr);

    filas.forEach(f => {
        const row = document.createElement('div');
        row.className = 'eg-detalle-row';
        row.id = `fila_${f.id}`;

        const tipoTag = f.tipo === 'Materia prima'
            ? `<span class="eg-articulo-tag mp">${f.tipo}</span>`
            : f.tipo
                ? `<span class="eg-articulo-tag">${f.tipo}</span>`
                : '';

        row.innerHTML = `
            <div>
              <input type="text" id="desc_${f.id}" value="${escHtml(f.desc)}"
                     placeholder="Descripción del ítem">
              ${tipoTag}
            </div>
            <input type="number" id="cant_${f.id}" value="${f.cant}" min="0" step="0.001">
            <input type="number" id="pu_${f.id}"   value="${f.pu}"   min="0" step="0.01">
            <input type="number" id="sub_${f.id}"   value="${f.sub}"  min="0" step="0.01" readonly>
            <div>
              ${f.idArt
                  ? `<span class="eg-articulo-tag${f.tipo === 'Materia prima' ? ' mp' : ''}"
                           title="${escHtml(f.artNombre)}">${escHtml(f.artNombre.slice(0, 16))}…</span>
                     <button class="eg-btn-link-articulo" data-id="${f.id}">cambiar</button>`
                  : `<button class="eg-btn-link-articulo" data-id="${f.id}">buscar en catálogo</button>`
              }
            </div>
            <button class="eg-btn-icon btn-del-fila" data-id="${f.id}" title="Eliminar línea">🗑</button>`;

        // Eventos de fila
        row.querySelector(`#desc_${f.id}`).addEventListener('input', e => {
            const fi = filas.find(x => x.id === f.id);
            if (fi) fi.desc = e.target.value;
        });
        row.querySelector(`#cant_${f.id}`).addEventListener('input', e => {
            const fi = filas.find(x => x.id === f.id);
            if (fi) { fi.cant = parseFloat(e.target.value) || 0; actualizarSubtotal(fi.id); }
        });
        row.querySelector(`#pu_${f.id}`).addEventListener('input', e => {
            const fi = filas.find(x => x.id === f.id);
            if (fi) { fi.pu = parseFloat(e.target.value) || 0; actualizarSubtotal(fi.id); }
        });
        row.querySelector(`#sub_${f.id}`).addEventListener('change', e => {
            const fi = filas.find(x => x.id === f.id);
            if (fi) fi.sub = parseFloat(e.target.value) || 0;
        });
        row.querySelector('.btn-del-fila').addEventListener('click', () => {
            if (filas.length > 1) eliminarFila(f.id);
        });
        const btnBuscar = row.querySelector('.eg-btn-link-articulo');
        if (btnBuscar) btnBuscar.addEventListener('click', () => abrirModal(f.id));

        tablaDetalles.appendChild(row);
    });
}

btnAddLinea.addEventListener('click', () => {
    agregarFila({});
    renderTabla();
});

// ── Sync totales al cambiar moneda / tasa ────────────────────────────────────
fMoneda.addEventListener('change', sincronizarTotales);
fTasa.addEventListener('input', sincronizarTotales);
fTotalUSD.addEventListener('input', () => {
    const usd = parseFloat(fTotalUSD.value) || 0;
    const t   = parseFloat(fTasa.value)     || 0;
    if (t > 0) fTotalBs.value = (usd * t).toFixed(2);
});
fTotalBs.addEventListener('input', () => {
    const bs  = parseFloat(fTotalBs.value) || 0;
    const t   = parseFloat(fTasa.value)    || 0;
    if (t > 0) fTotalUSD.value = (bs / t).toFixed(2);
});

function sincronizarTotales() {
    const t = parseFloat(fTasa.value) || 0;
    const usd = parseFloat(fTotalUSD.value) || 0;
    if (t > 0 && usd > 0) fTotalBs.value = (usd * t).toFixed(2);
}

// ── Pago / crédito / precio pendiente ────────────────────────────────────────
function aplicarPrecioPendiente() {
    const pp = fPrecioPendiente.checked;
    // Con precio pendiente no se conocen los montos: se deshabilitan.
    zonaPago.style.opacity = pp ? '.5' : '1';
    [fACredito, fAbonado, fVence].forEach(el => { if (el) el.disabled = pp; });
    [fTotalUSD, fTotalBs, fTasa].forEach(el => { if (el) el.disabled = pp; });
    if (pp) { fTotalUSD.value = '0'; fTotalBs.value = '0'; fACredito.checked = false; zonaCredito.style.display = 'none'; }
}
function aplicarCredito() {
    zonaCredito.style.display = fACredito.checked ? 'grid' : 'none';
}
fPrecioPendiente.addEventListener('change', aplicarPrecioPendiente);
fACredito.addEventListener('change', aplicarCredito);

// ── Validación y paso 3 ──────────────────────────────────────────────────────
btnIrPaso3.addEventListener('click', () => {
    ocultarError(error2);
    const precioPendiente = fPrecioPendiente.checked;
    if (!fFecha.value) return mostrarError(error2, 'La fecha es obligatoria.');
    if (!precioPendiente && parseFloat(fTotalUSD.value) <= 0 && parseFloat(fTotalBs.value) <= 0)
        return mostrarError(error2, 'Ingresa el monto total del documento.');
    if (filas.length === 0)
        return mostrarError(error2, 'Agrega al menos una línea de detalle.');
    if (filas.some(f => !f.desc.trim()))
        return mostrarError(error2, 'Todas las líneas deben tener descripción.');
    if (precioPendiente && filas.some(f => (f.cant || 0) <= 0))
        return mostrarError(error2, 'Con precio pendiente, cada línea debe indicar la cantidad recibida.');

    renderResumen();
    irPaso(3);
});

btnVolver1.addEventListener('click', () => irPaso(1));
btnVolver2.addEventListener('click', () => irPaso(2));

// Botón "Ingresar manualmente" — salta al formulario sin imagen
const btnManual = document.getElementById('btnManual');
if (btnManual) {
    btnManual.addEventListener('click', () => {
        fFecha.value = fFecha.value || new Date().toISOString().slice(0,10);
        irPaso(2);
    });
}

// ── Resumen ──────────────────────────────────────────────────────────────────
function renderResumen() {
    const catSel = categorias.find(c => c.id == fCategoria.value);
    const tUSD   = parseFloat(fTotalUSD.value) || 0;
    const tBs    = parseFloat(fTotalBs.value)  || 0;

    let html = `
        <h3>Resumen del egreso</h3>
        <div class="eg-form-grid" style="margin-bottom:.8rem">
          <div><strong>Proveedor:</strong><br>${escHtml(fProveedor.value || '—')}</div>
          <div><strong>Documento:</strong><br>${escHtml(fNroDoc.value || '—')}</div>
          <div><strong>Fecha:</strong><br>${fFecha.value}</div>
          <div><strong>Categoría:</strong><br>${catSel ? escHtml(catSel.nombre) : '—'}</div>
        </div>
        <table>
          <thead>
            <tr>
              <th>Descripción</th>
              <th style="text-align:right">Cant.</th>
              <th style="text-align:right">P. Unit.</th>
              <th style="text-align:right">Subtotal</th>
              <th>Tipo</th>
            </tr>
          </thead>
          <tbody>`;

    filas.forEach(f => {
        html += `<tr>
          <td>${escHtml(f.desc)}${f.idArt ? ` <em style="color:#888;font-size:.78rem">[catálogo]</em>` : ''}</td>
          <td style="text-align:right">${fmt2(f.cant)}</td>
          <td style="text-align:right">${fmt2(f.pu)}</td>
          <td style="text-align:right">${fmt2(f.sub)}</td>
          <td>${f.tipo ? escHtml(f.tipo) : '<em style="color:#aaa">libre</em>'}</td>
        </tr>`;
    });

    let pagoInfo = '';
    if (fPrecioPendiente.checked) {
        pagoInfo = `<div class="eg-warning" style="margin-top:.6rem">⏳ <strong>Precio pendiente</strong>: se registra la mercancía sin montos; el administrador completará el precio luego.</div>`;
    } else if (fACredito.checked) {
        const ab = parseFloat(fAbonado.value) || 0;
        pagoInfo = `<div style="margin-top:.6rem">Pago a crédito — Abonado: <strong>$${fmt2(ab)}</strong> · Saldo (CxP): <strong>$${fmt2(Math.max(0, tUSD - ab))}</strong>${fVence.value ? ` · Vence: ${fVence.value}` : ''}</div>`;
    } else {
        pagoInfo = `<div style="margin-top:.6rem">Pago completo</div>`;
    }

    html += `</tbody></table>
        <div class="eg-resumen-totales">
          ${fPrecioPendiente.checked ? '<span>Total: <strong>— (pendiente)</strong></span>' : `<span>Total USD: <strong>$${fmt2(tUSD)}</strong></span>`}
          ${!fPrecioPendiente.checked && tBs > 0 ? `<span>Total Bs: <strong>Bs ${fmt2(tBs)}</strong></span>` : ''}
        </div>
        ${pagoInfo}`;

    resumenFinal.innerHTML = html;
}

// ── Guardar ──────────────────────────────────────────────────────────────────
btnGuardar.addEventListener('click', async () => {
    ocultarError(error3);
    success3.style.display = 'none';
    btnGuardar.disabled    = true;
    loader3.style.display  = 'flex';

    const totalUSD = parseFloat(fTotalUSD.value) || 0;
    const totalBs  = parseFloat(fTotalBs.value)  || 0;
    const tasa     = parseFloat(fTasa.value)      || 0;

    const payload = {
        idCategoria   : parseInt(fCategoria.value) || null,
        idInsumo      : null,
        descripcion   : fProveedor.value
                          ? `Compra a ${fProveedor.value}`
                          : (filas[0]?.desc || 'Compra'),
        proveedor     : fProveedor.value || null,
        nroDocumento  : fNroDoc.value    || null,
        fechaGasto    : fFecha.value     || new Date().toISOString().slice(0, 10),
        cantidadTotal : filas.reduce((a, f) => a + f.cant, 0),
        montoUSD      : totalUSD,
        montoBs       : totalBs,
        tasaBCV       : tasa,
        rutaComprobante: null,
        notas         : fNotas.value || null,
        // Fase 3: pago a crédito / precio pendiente
        precioPendiente : fPrecioPendiente.checked,
        aCredito        : fACredito.checked,
        pagadoUSD       : fACredito.checked ? (parseFloat(fAbonado.value) || 0) : 0,
        fechaVencimiento: (fACredito.checked || fPrecioPendiente.checked) && fVence.value ? fVence.value : null,
        detalles      : filas.map(f => ({
            descripcion   : f.desc,
            idArticulo    : f.idArt,
            codigoInterno : f.cod  || null,
            tipoInsumo    : f.tipo || null,
            cantidad      : f.cant,
            precioUnitario: f.pu,
            subtotal      : f.sub
        }))
    };

    try {
        const resp = await fetch('/api/egresos', {
            method : 'POST',
            headers: { 'Content-Type': 'application/json' },
            credentials: 'include',
            body   : JSON.stringify(payload)
        });

        if (!resp.ok) {
            const err = await resp.json().catch(() => ({}));
            throw new Error(err.error || err.detail || `Error ${resp.status}`);
        }

        const data = await resp.json();
        success3.textContent = data.precioPendiente
            ? `Egreso #${data.id} guardado con PRECIO PENDIENTE. El administrador lo completará luego.`
            : `Egreso #${data.id} guardado correctamente.`;
        success3.style.display = 'block';
        btnGuardar.style.display = 'none';
        
        setTimeout(() => { location.href = '/pages/egresos.html'; }, 1800);

    } catch (ex) {
        mostrarError(error3, ex.message);
        btnGuardar.disabled = false;
    } finally {
        loader3.style.display = 'none';
    }
});

// ── Modal búsqueda catálogo ──────────────────────────────────────────────────
function abrirModal(filaId) {
    modalFilaId = filaId;
    modalBuscar.value = '';
    modalResultados.innerHTML = '';
    modalOverlay.style.display = 'flex';
    setTimeout(() => modalBuscar.focus(), 50);
}

function cerrarModal() {
    modalOverlay.style.display = 'none';
    modalFilaId = null;
}

btnCerrarModal.addEventListener('click', cerrarModal);
modalOverlay.addEventListener('click', e => {
    if (e.target === modalOverlay) cerrarModal();
});

let buscarTimer = null;
modalBuscar.addEventListener('input', () => {
    clearTimeout(buscarTimer);
    buscarTimer = setTimeout(buscarArticulos, 300);
});

async function buscarArticulos() {
    const q = modalBuscar.value.trim();
    if (q.length < 2) { modalResultados.innerHTML = ''; return; }

    modalResultados.innerHTML =
        '<div class="eg-modal-buscando">Buscando…</div>';
    try {
        const r = await fetch(
            `/api/articulos?q=${encodeURIComponent(q)}`,
            { credentials: 'include' }
        );
        if (!r.ok) throw new Error();
        const items = await r.json();

        if (items.length === 0) {
            modalResultados.innerHTML =
                '<div class="eg-modal-buscando">Sin resultados.</div>';
            return;
        }

        modalResultados.innerHTML = items.map(it => `
            <div class="eg-modal-result-item"
                 data-id="${it.id}"
                 data-nombre="${escHtml(it.nombre)}"
                 data-cod="${escHtml(it.codigoInterno)}"
                 data-tipo="${escHtml(it.tipoInsumo)}"
                 data-pu="${it.precioUnitario}">
              <div style="display:flex;align-items:center;gap:.4rem;flex-wrap:wrap">
                <span style="font-weight:600">${escHtml(it.nombre)}</span>
                ${it.tipoInsumo
                    ? `<span class="eg-articulo-tag${it.tipoInsumo === 'Materia prima' ? ' mp' : ''}">
                         ${escHtml(it.tipoInsumo)}</span>`
                    : ''}
              </div>
              <div style="font-size:.78rem;color:var(--texto-dim);margin-top:.15rem">
                ${escHtml(it.codigoInterno) || '—'} · Stock: ${it.stockActual}
                · $${(parseFloat(it.precioUnitario)||0).toFixed(2)}/u
              </div>
            </div>`).join('');

        modalResultados.querySelectorAll('.eg-modal-result-item').forEach(el => {
            el.addEventListener('click', () => {
                const fila = filas.find(f => f.id === modalFilaId);
                if (fila) {
                    fila.idArt     = parseInt(el.dataset.id);
                    fila.artNombre = el.dataset.nombre;
                    fila.cod       = el.dataset.cod;
                    fila.tipo      = el.dataset.tipo;
                    fila.pu        = parseFloat(el.dataset.pu) || 0;
                    actualizarSubtotal(fila.id);
                    renderTabla();
                }
                cerrarModal();
            });
        });
    } catch {
        modalResultados.innerHTML =
            '<div class="eg-modal-buscando" style="color:var(--rojo)">Error al buscar.</div>';
    }
}

// ── Escape HTML ───────────────────────────────────────────────────────────────
function escHtml(str) {
    if (!str) return '';
    return String(str)
        .replace(/&/g, '&amp;').replace(/</g, '&lt;')
        .replace(/>/g, '&gt;').replace(/"/g, '&quot;');
}

// ── Arranque ──────────────────────────────────────────────────────────────────
if (!Auth.requireAuth()) throw new Error('No autenticado');

(async () => {
    await Promise.all([
        cargarConfigVision(),
        cargarCategorias(),
        cargarTasaBCV()
    ]);
})();
