// digitalizar-factura.js — Digitalizar facturas/notas de entrega existentes
if (!Auth.requireAuth()) { /* redirige */ }

const app = document.getElementById('app');

let paso = 1;          // 1=foto, 2=revisar, 3=resultado
let archivoSel = null;
let datosOCR = null;   // lo que devuelve el digitalizador
let inventario = [];   // cache de productos para mapear códigos
let clientesDB = [];   // cache de clientes

// ── Cargar catálogos ─────────────────────────────────────────
async function cargarCatalogos() {
    try {
        const [inv, cli] = await Promise.all([
            apiGet('/api/inventario'),
            apiGet('/api/clientes')
        ]);
        inventario = inv || [];
        clientesDB = cli || [];
    } catch { /* silencioso — se puede usar sin catálogos */ }
}
cargarCatalogos();

// ── Render ────────────────────────────────────────────────────
function render() {
    app.innerHTML = `
        ${renderTopbar('Digitalizar Factura')}
        <div class="content" style="padding-bottom:80px">
          <div class="msg"></div>
          <div id="pasoContent"></div>
        </div>
        ${renderNav('facturas')}`;
    renderPaso();
}

function renderPaso() {
    const el = document.getElementById('pasoContent');
    if (!el) return;
    if (paso === 1) renderPaso1(el);
    else if (paso === 2) renderPaso2(el);
    else renderPaso3(el);
}

// ── PASO 1: Tomar foto ───────────────────────────────────────
function renderPaso1(el) {
    el.innerHTML = `
        <div class="card" style="text-align:center;padding:24px 16px">
            <h3 style="margin:0 0 8px">📷 Fotografiar factura o nota de entrega</h3>
            <p style="color:var(--texto-dim);font-size:.85rem;margin:0 0 16px">
                Toma una foto clara del documento completo. El sistema extraerá los datos automáticamente.
            </p>
            <div style="margin:16px 0">
                <label class="btn btn-primary" style="cursor:pointer;display:inline-block;padding:12px 24px;font-size:1rem">
                    📸 Tomar foto
                    <input type="file" accept="image/*" capture="environment"
                           style="display:none" id="inputFoto">
                </label>
            </div>
            <div style="margin:8px 0">
                <label class="btn btn-ghost" style="cursor:pointer;display:inline-block">
                    📁 Seleccionar imagen
                    <input type="file" accept="image/*"
                           style="display:none" id="inputGaleria">
                </label>
            </div>
            ${archivoSel ? `
                <div style="margin-top:16px">
                    <p style="color:var(--cyan);font-size:.85rem">✓ ${esh(archivoSel.name)}</p>
                    <img id="preview" style="max-width:100%;max-height:300px;border-radius:8px;margin:8px 0"
                         alt="Vista previa">
                    <div>
                        <button class="btn btn-primary" id="btnExtraer" style="margin-top:12px;padding:10px 32px">
                            🔍 Extraer datos
                        </button>
                    </div>
                </div>
            ` : ''}
        </div>`;

    document.getElementById('inputFoto')?.addEventListener('change', onFileSelect);
    document.getElementById('inputGaleria')?.addEventListener('change', onFileSelect);
    document.getElementById('btnExtraer')?.addEventListener('click', extraerDatos);

    if (archivoSel) {
        const reader = new FileReader();
        reader.onload = e => {
            const img = document.getElementById('preview');
            if (img) img.src = e.target.result;
        };
        reader.readAsDataURL(archivoSel);
    }
}

function onFileSelect(e) {
    const f = e.target.files[0];
    if (!f) return;
    archivoSel = f;
    render();
}

async function extraerDatos() {
    if (!archivoSel) return;
    const btn = document.getElementById('btnExtraer');
    if (btn) { btn.disabled = true; btn.textContent = '⏳ Procesando...'; }

    try {
        const form = new FormData();
        form.append('imagen', archivoSel);

        const res = await fetch('/api/facturas/extraer-ocr', {
            method: 'POST',
            credentials: 'include',
            body: form
        });
        const data = await res.json();
        if (!data.ok) throw new Error(data.error || 'Error al extraer');

        datosOCR = data.datos;
        if (!datosOCR.encontrado) {
            showMsg('No se detectó una factura en la imagen. Intenta con otra foto.', 'error');
            return;
        }

        // Intentar mapear descripciones a códigos de inventario
        mapearCodigos();
        paso = 2;
        render();
    } catch (ex) {
        showMsg(ex.message, 'error');
        if (btn) { btn.disabled = false; btn.textContent = '🔍 Extraer datos'; }
    }
}

function mapearCodigos() {
    if (!datosOCR?.items || !inventario.length) return;
    for (const item of datosOCR.items) {
        if (item.codigo) continue; // ya tiene código
        const desc = (item.descripcion || '').toLowerCase();
        // Buscar coincidencia por descripción
        const match = inventario.find(p =>
            p.descripcion.toLowerCase().includes(desc) ||
            desc.includes(p.descripcion.toLowerCase()) ||
            desc.includes(p.codigo.toLowerCase())
        );
        if (match) item.codigo = match.codigo;
    }
}

// ── PASO 2: Revisar y corregir ───────────────────────────────
function renderPaso2(el) {
    const d = datosOCR;
    const tipoDoc = d.tipo_documento === 'nota_entrega' ? 'Nota de Entrega' : 'Factura';
    const pctIva = d.tipo_documento === 'nota_entrega' ? 0 : (d.pct_iva ?? 16);

    // Buscar cliente por nombre
    let clienteMatch = '';
    if (d.cliente && clientesDB.length) {
        const nom = d.cliente.toLowerCase();
        const found = clientesDB.find(c =>
            (c.nombre || '').toLowerCase().includes(nom) ||
            nom.includes((c.nombre || '').toLowerCase())
        );
        if (found) clienteMatch = found.codigo;
    }

    el.innerHTML = `
        <div class="card" style="padding:16px">
            <h3 style="margin:0 0 12px">📝 Revisar datos extraídos</h3>
            <p style="color:var(--texto-dim);font-size:.82rem;margin:0 0 12px">
                Verifica y corrige los datos antes de guardar. Los campos en rojo necesitan atención.
            </p>

            <div style="display:grid;grid-template-columns:1fr 1fr;gap:8px;margin-bottom:12px">
                <div>
                    <label class="lbl">Tipo</label>
                    <select id="fTipo" class="input">
                        <option value="factura" ${d.tipo_documento !== 'nota_entrega' ? 'selected' : ''}>Factura con IVA</option>
                        <option value="nota_entrega" ${d.tipo_documento === 'nota_entrega' ? 'selected' : ''}>Nota de Entrega</option>
                    </select>
                </div>
                <div>
                    <label class="lbl">Número</label>
                    <input class="input" id="fNumero" value="${esh(d.numero || '')}">
                </div>
                <div>
                    <label class="lbl">Fecha</label>
                    <input class="input" type="date" id="fFecha" value="${formatearFechaInput(d.fecha)}">
                </div>
                <div>
                    <label class="lbl">% IVA</label>
                    <input class="input" type="number" id="fIva" value="${pctIva}" min="0" max="99">
                </div>
            </div>

            <div style="display:grid;grid-template-columns:1fr 1fr;gap:8px;margin-bottom:12px">
                <div>
                    <label class="lbl">Cliente (nombre)</label>
                    <input class="input" id="fClienteNom" value="${esh(d.cliente || '')}" list="dlClientes">
                    <datalist id="dlClientes">
                        ${clientesDB.map(c => `<option value="${eshA(c.nombre || c.codigo)}">`).join('')}
                    </datalist>
                </div>
                <div>
                    <label class="lbl">Código cliente</label>
                    <input class="input" id="fClienteCod" value="${esh(clienteMatch)}" list="dlClientesCod"
                           style="${clienteMatch ? '' : 'border-color:var(--warn)'}">
                    <datalist id="dlClientesCod">
                        ${clientesDB.map(c => `<option value="${eshA(c.codigo)}">${esh(c.nombre)}</option>`).join('')}
                    </datalist>
                </div>
            </div>

            <div style="display:grid;grid-template-columns:1fr 1fr;gap:8px;margin-bottom:12px">
                <div>
                    <label class="lbl">Forma de pago</label>
                    <select id="fFormaPago" class="input">
                        <option value="Contado" ${(d.forma_pago||'').toLowerCase().includes('contado')?'selected':''}>Contado</option>
                        <option value="Credito 15" ${(d.forma_pago||'').includes('15')?'selected':''}>Crédito 15 días</option>
                        <option value="Credito 30" ${(d.forma_pago||'').includes('30')?'selected':''}>Crédito 30 días</option>
                        <option value="Credito 45" ${(d.forma_pago||'').includes('45')?'selected':''}>Crédito 45 días</option>
                        <option value="Credito 60" ${(d.forma_pago||'').includes('60')?'selected':''}>Crédito 60 días</option>
                    </select>
                </div>
                <div>
                    <label class="lbl">Descuento %</label>
                    <input class="input" type="number" id="fDescuento" value="${d.descuento_pct || 0}" min="0" max="100">
                </div>
            </div>

            <h4 style="margin:16px 0 8px;border-bottom:1px solid var(--borde);padding-bottom:4px">Productos</h4>
            <div style="overflow-x:auto">
                <table style="width:100%;font-size:.82rem;border-collapse:collapse">
                    <thead>
                        <tr style="background:var(--fondo-card)">
                            <th style="text-align:left;padding:6px">Código</th>
                            <th style="text-align:left;padding:6px">Descripción</th>
                            <th style="text-align:right;padding:6px">Cant</th>
                            <th style="text-align:right;padding:6px">Precio</th>
                            <th style="text-align:center;padding:6px">×</th>
                        </tr>
                    </thead>
                    <tbody id="tbItems">
                        ${(d.items || []).map((it, i) => renderItemRow(it, i)).join('')}
                    </tbody>
                </table>
            </div>
            <button class="btn btn-ghost btn-sm" id="btnAddItem" style="margin-top:8px">+ Agregar producto</button>

            <div style="margin-top:12px">
                <label class="lbl">Observaciones</label>
                <input class="input" id="fObs" value="${esh(d.observaciones || '')}">
            </div>

            <div style="display:flex;gap:8px;margin-top:16px">
                <button class="btn btn-ghost" id="btnVolver" style="flex:1">← Volver</button>
                <button class="btn btn-primary" id="btnGuardar" style="flex:2">💾 Guardar factura</button>
            </div>
        </div>`;

    document.getElementById('btnVolver')?.addEventListener('click', () => { paso = 1; render(); });
    document.getElementById('btnGuardar')?.addEventListener('click', guardarFactura);
    document.getElementById('btnAddItem')?.addEventListener('click', agregarItem);
    document.getElementById('fClienteNom')?.addEventListener('change', buscarCliente);
    document.getElementById('fTipo')?.addEventListener('change', e => {
        document.getElementById('fIva').value = e.target.value === 'nota_entrega' ? '0' : '16';
    });

    // Botones eliminar item
    document.querySelectorAll('.btn-del-item').forEach(btn => {
        btn.addEventListener('click', () => {
            const idx = parseInt(btn.dataset.idx);
            datosOCR.items.splice(idx, 1);
            renderPaso2(el);
        });
    });
}

function renderItemRow(it, i) {
    const hasCod = it.codigo && inventario.some(p => p.codigo === it.codigo);
    return `<tr>
        <td style="padding:4px"><input class="input item-cod" data-idx="${i}" value="${esh(it.codigo || '')}"
            list="dlProductos" style="width:80px;font-size:.8rem;${hasCod ? '' : 'border-color:var(--warn)'}"></td>
        <td style="padding:4px"><input class="input item-desc" data-idx="${i}" value="${esh(it.descripcion || '')}"
            style="font-size:.8rem"></td>
        <td style="padding:4px"><input class="input item-cant" data-idx="${i}" type="number" step="any"
            value="${it.cantidad || ''}" style="width:60px;text-align:right;font-size:.8rem"></td>
        <td style="padding:4px"><input class="input item-precio" data-idx="${i}" type="number" step="any"
            value="${it.precio_unitario || ''}" style="width:80px;text-align:right;font-size:.8rem"></td>
        <td style="padding:4px;text-align:center">
            <button class="btn-del-item" data-idx="${i}" style="background:none;border:none;color:var(--rojo);cursor:pointer;font-size:1rem">✕</button>
        </td>
    </tr>
    <datalist id="dlProductos">
        ${inventario.map(p => `<option value="${eshA(p.codigo)}">${esh(p.descripcion)}</option>`).join('')}
    </datalist>`;
}

function agregarItem() {
    datosOCR.items.push({ codigo: null, descripcion: '', cantidad: 0, precio_unitario: 0 });
    const el = document.getElementById('pasoContent');
    renderPaso2(el);
}

function buscarCliente() {
    const nom = document.getElementById('fClienteNom')?.value?.toLowerCase() || '';
    const found = clientesDB.find(c =>
        (c.nombre || '').toLowerCase() === nom ||
        (c.nombre || '').toLowerCase().includes(nom)
    );
    if (found) {
        document.getElementById('fClienteCod').value = found.codigo;
        document.getElementById('fClienteCod').style.borderColor = '';
    }
}

function leerItemsDelForm() {
    const items = [];
    document.querySelectorAll('.item-cod').forEach(el => {
        const i = parseInt(el.dataset.idx);
        const cod = el.value.trim();
        const cant = parseFloat(document.querySelector(`.item-cant[data-idx="${i}"]`)?.value) || 0;
        const precio = parseFloat(document.querySelector(`.item-precio[data-idx="${i}"]`)?.value) || 0;
        if (cod && cant > 0) {
            items.push({ Codigo: cod, Cantidad: cant, PrecioUnit: precio });
        }
    });
    return items;
}

async function guardarFactura() {
    const btn = document.getElementById('btnGuardar');
    const items = leerItemsDelForm();

    if (items.length === 0) {
        showMsg('Debe haber al menos un producto con código y cantidad.', 'error');
        return;
    }

    const codigoCliente = document.getElementById('fClienteCod')?.value?.trim();
    if (!codigoCliente) {
        showMsg('Selecciona un código de cliente válido.', 'error');
        return;
    }

    // Verificar que todos los códigos existen
    const codInvalidos = items.filter(it => !inventario.some(p => p.codigo === it.Codigo));
    if (codInvalidos.length > 0) {
        showMsg(`Códigos no encontrados en inventario: ${codInvalidos.map(i=>i.Codigo).join(', ')}`, 'error');
        return;
    }

    const tipo = document.getElementById('fTipo')?.value;
    const pctIva = parseFloat(document.getElementById('fIva')?.value) || 0;
    const formaPago = document.getElementById('fFormaPago')?.value || 'Contado';
    const descuento = parseFloat(document.getElementById('fDescuento')?.value) || 0;

    // Extraer días de crédito del formaPago
    let diasCredito = 0;
    const match = formaPago.match(/\d+/);
    if (match) diasCredito = parseInt(match[0]);

    const body = {
        NumeroF: document.getElementById('fNumero')?.value?.trim() || null,
        Fecha: document.getElementById('fFecha')?.value || null,
        CodigoCliente: codigoCliente,
        FormaDePago: formaPago,
        PctIva: tipo === 'nota_entrega' ? 0 : pctIva,
        Descuento: descuento,
        DiasCredito: diasCredito,
        Observaciones: (document.getElementById('fObs')?.value || '') +
                       ' [Digitalizada]',
        Items: items
    };

    if (btn) { btn.disabled = true; btn.textContent = '⏳ Guardando...'; }

    try {
        const res = await apiPost('/api/facturas', body);
        if (!res) throw new Error('Error al guardar');
        datosOCR._resultado = res;
        paso = 3;
        render();
    } catch (ex) {
        showMsg(ex.message, 'error');
        if (btn) { btn.disabled = false; btn.textContent = '💾 Guardar factura'; }
    }
}

// ── PASO 3: Resultado ────────────────────────────────────────
function renderPaso3(el) {
    const r = datosOCR?._resultado || {};
    el.innerHTML = `
        <div class="card" style="text-align:center;padding:24px 16px">
            <div style="font-size:3rem;margin-bottom:8px">✅</div>
            <h3 style="margin:0 0 8px">Factura registrada</h3>
            <p style="font-size:1.1rem;color:var(--cyan);font-weight:600">${esh(r.numeroF || '')}</p>
            <p style="color:var(--texto-dim);font-size:.9rem">
                Total: ${formatUSD(r.total || 0)} — IVA: ${formatUSD(r.iva || 0)}
            </p>
            ${r.cxcCreada ? '<p style="color:var(--warn);font-size:.85rem">📋 Cuenta por cobrar creada</p>' : ''}
            ${r.avisoMorosidad ? '<p style="color:var(--rojo);font-size:.85rem">⚠ Cliente tiene facturas vencidas</p>' : ''}

            <div style="display:flex;gap:8px;margin-top:20px;justify-content:center">
                <button class="btn btn-primary" id="btnOtra">📷 Digitalizar otra</button>
                <a href="/pages/facturas.html" class="btn btn-ghost">Ver facturas</a>
            </div>
        </div>`;

    document.getElementById('btnOtra')?.addEventListener('click', () => {
        paso = 1; archivoSel = null; datosOCR = null;
        render();
    });
}

// ── Utilidades ───────────────────────────────────────────────
function eshA(v) { return esh(v).replace(/"/g, '&quot;'); }

function formatearFechaInput(fechaStr) {
    if (!fechaStr) return new Date().toISOString().slice(0, 10);
    // DD/MM/YYYY → YYYY-MM-DD
    const parts = fechaStr.split('/');
    if (parts.length === 3) return `${parts[2]}-${parts[1].padStart(2,'0')}-${parts[0].padStart(2,'0')}`;
    return fechaStr;
}

// ── Init ─────────────────────────────────────────────────────
render();
