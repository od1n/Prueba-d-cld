// compras.js — Compras de materia prima
if (!Auth.requireAuth()) {}
const app = document.getElementById('app');

let todasCompras = [];
let itemsCompra  = [];
let timerBusq    = null;

// ── Render principal ──────────────────────────────────────────────────────
async function render() {
    app.innerHTML = `
    ${renderTopbar('Compras M.Prima')}
    <div class="content">
      <div class="search-wrap">
        <span class="search-icon">⌕</span>
        <input class="input-field" id="buscarC" type="search"
          placeholder="N° orden, proveedor…" autocomplete="off" oninput="filtrarCompras(this.value)">
      </div>
      <div style="display:flex;gap:8px;padding:0 16px 10px;flex-wrap:wrap">
        <input type="date" id="cDesde" class="input-field" style="flex:1;min-width:120px"
          title="Desde" onchange="cargarCompras()">
        <input type="date" id="cHasta" class="input-field" style="flex:1;min-width:120px"
          title="Hasta" onchange="cargarCompras()">
        <button class="btn btn-primary" style="white-space:nowrap"
          onclick="abrirNuevaCompra()">＋ Nueva</button>
      </div>
      <div id="listaCompras"><div class="loading-msg"><div class="spinner"></div></div></div>
    </div>

    <!-- Modal detalle -->
    <div class="modal-overlay" id="modalDetC">
      <div class="modal-drawer" id="drawerDetC"></div>
    </div>

    <!-- Modal formulario -->
    <div class="modal-overlay" id="modalFormC">
      <div class="modal-drawer" id="drawerFormC" style="max-height:95vh;overflow-y:auto"></div>
    </div>

    ${renderNav('compras')}`;

    await cargarCompras();
}

// ── Carga ─────────────────────────────────────────────────────────────────
async function cargarCompras() {
    const desde = document.getElementById('cDesde')?.value || '';
    const hasta = document.getElementById('cHasta')?.value || '';
    const q     = document.getElementById('buscarC')?.value || '';
    let url = '/api/compras';
    const ps = [];
    if (q)     ps.push('q='+encodeURIComponent(q));
    if (desde) ps.push('desde='+encodeURIComponent(desde));
    if (hasta) ps.push('hasta='+encodeURIComponent(hasta));
    if (ps.length) url += '?'+ps.join('&');
    try {
        todasCompras = await apiGet(url) || [];
        renderListaCompras(todasCompras);
    } catch(err) {
        document.getElementById('listaCompras').innerHTML =
            `<div class="alert-banner">Error: ${esh(err.message)}</div>`;
    }
}

function filtrarCompras(q) {
    clearTimeout(timerBusq);
    timerBusq = setTimeout(() => cargarCompras(), 400);
}

function renderListaCompras(lista) {
    const div = document.getElementById('listaCompras');
    if (!div) return;
    if (!lista.length) {
        div.innerHTML = `<div class="empty"><div class="empty-icon">🛒</div>
            <div class="empty-text">Sin compras registradas</div></div>`;
        return;
    }
    div.innerHTML = `<div class="card" style="padding:0">
        ${lista.map((c,ix) => {
            const pagTag = (c.saldo||0) <= 0
                ? '<span class="tag tag-verde">Pagada</span>'
                : '<span class="tag tag-naranja">Pendiente</span>';
            return `
            <button type="button" class="list-item js-comp-btn" data-idx="${ix}"
              style="padding:12px 16px;background:none;border:none;border-bottom:1px solid var(--borde);
                     text-align:left;width:100%;cursor:pointer;color:inherit;font:inherit">
              <div class="list-item-main">
                <div class="list-item-title">${esh(c.proveedor||c.codigoP||'(sin proveedor)')}</div>
                <div class="list-item-sub">${esh(c.compraN)} · ${formatFecha(c.fecha)} · ${esh(c.formaDePago||'')}</div>
              </div>
              <div style="text-align:right;flex-shrink:0;margin-left:8px">
                <div class="mono" style="font-size:13pt">${formatUSD(c.total||0)}</div>
                ${pagTag}
              </div>
            </button>`;
        }).join('')}
    </div>`;
    div.querySelectorAll('.js-comp-btn').forEach(btn => {
        btn.addEventListener('click', () => verDetalleCompra(lista[+btn.dataset.idx].compraN));
    });
}

// ── Ver detalle ───────────────────────────────────────────────────────────
async function verDetalleCompra(id) {
    const drawer = document.getElementById('drawerDetC');
    drawer.innerHTML = `<div class="modal-handle"></div>
        <div class="modal-title">${esh(id)}</div><div class="spinner"></div>`;
    openModal('modalDetC');
    try {
        const resp = await apiGet(`/api/compras/${encodeURIComponent(id)}`);
        // El endpoint devuelve { compra: {...}, lineas: [...] }
        const c = resp.compra || resp;
        const lineas = resp.lineas || [];
        const pagTag = (c.saldo||0) <= 0
            ? '<span class="tag tag-verde">Pagada</span>'
            : '<span class="tag tag-naranja">Pendiente</span>';
        drawer.innerHTML = `
        <div class="modal-handle"></div>
        <div class="flex-between mb-8">
          <div class="modal-title" style="margin-bottom:0">${esh(c.compraN)}</div>
          ${pagTag}
        </div>
        <div class="text-dim text-sm mb-16">
          ${esh(c.proveedor||c.codigoP||'')} · ${formatFecha(c.fecha||c.fechaC)} · ${esh(c.formaDePago||'')}
        </div>
        <div class="card" style="margin-bottom:12px;padding:0">
          ${lineas.map(l=>`
            <div class="list-item" style="padding:10px 14px;border-bottom:1px solid var(--borde)">
              <div class="list-item-main">
                <div class="list-item-title">${esh(l.descripcion||l.codigoI||l.codigo)}</div>
                <div class="list-item-sub">${formatNum(l.cantidad||0,2)} × ${formatUSD(l.precioUnit||l.precioC||0)}</div>
              </div>
              <div class="list-item-value">${formatUSD(l.subtotal||(l.cantidad||0)*(l.precioUnit||l.precioC||0))}</div>
            </div>`).join('')}
        </div>
        ${(c.descuento||0)>0?`<div class="flex-between mb-4">
          <span class="text-dim">Descuento</span>
          <span class="mono" style="color:var(--naranja)">-${formatUSD(c.descuentoMonto||0)}</span>
        </div>`:''}
        <div class="flex-between mb-4">
          <span class="text-dim">IVA</span><span class="mono">${formatUSD(c.iva||c.ivaC||0)}</span>
        </div>
        <div class="flex-between bold mb-8">
          <span>Total</span><span class="mono text-cyan">${formatUSD(c.total||c.totalC||0)}</span>
        </div>
        ${(c.saldo||0)>0?`<div class="flex-between mb-8">
          <span class="text-dim">Saldo</span>
          <span class="mono" style="color:var(--naranja)">${formatUSD(c.saldo)}</span>
        </div>`:''}
        ${c.observaciones?`<div class="text-dim text-sm mb-12">${esh(c.observaciones)}</div>`:''}
        <div style="display:flex;flex-direction:column;gap:8px;margin-top:12px">
          <a href="/api/compras/${encodeURIComponent(id)}/imprimir" target="_blank"
            class="btn btn-ghost btn-full">🖨 Orden de compra / PDF</a>
          <button class="btn btn-ghost btn-full" onclick="closeModal('modalDetC')">Cerrar</button>
        </div>`;
    } catch(err) {
        drawer.innerHTML = `<div class="modal-handle"></div>
            <div class="alert-banner">Error: ${esh(err.message)}</div>
            <button class="btn btn-ghost btn-full" onclick="closeModal('modalDetC')">Cerrar</button>`;
    }
}

// ── Nueva compra ──────────────────────────────────────────────────────────
function abrirNuevaCompra() {
    itemsCompra = [];
    const hoy = new Date().toISOString().slice(0,10);
    const drawer = document.getElementById('drawerFormC');
    drawer.innerHTML = `
    <div class="modal-handle"></div>
    <div class="modal-title">Nueva Orden de Compra</div>

    <div class="form-group mb-8" style="position:relative">
      <label class="form-label">Proveedor</label>
      <input id="cProvInput" class="input-field" placeholder="Buscar proveedor…"
        autocomplete="off" oninput="dropProveedores(this.value)">
      <input type="hidden" id="cProvCod">
      <div id="ddProv" class="autocomplete-drop" style="display:none"></div>
    </div>

    <div class="form-grid-2 mb-8">
      <div class="form-group">
        <label class="form-label">Fecha</label>
        <input id="cFecha" type="date" class="input-field" value="${hoy}">
      </div>
      <div class="form-group">
        <label class="form-label">Días crédito</label>
        <input id="cDias" type="number" class="input-field" value="0" min="0">
      </div>
    </div>
    <div class="form-grid-2 mb-12">
      <div class="form-group">
        <label class="form-label">Forma de pago</label>
        <select id="cFormaPago" class="input-field">
          <option>Contado</option><option>Crédito</option>
          <option>Transferencia</option><option>Zelle</option>
          <option>Efectivo USD</option><option>Efectivo Bs</option>
        </select>
      </div>
      <div class="form-group">
        <label class="form-label">IVA %</label>
        <input id="cIva" type="number" class="input-field" value="16" min="0" max="100"
          oninput="recalcCompra()">
      </div>
    </div>
    <div class="form-group mb-12">
      <label class="form-label">Descuento %</label>
      <input id="cDesc" type="number" class="input-field" value="0" min="0" max="100"
        oninput="recalcCompra()">
    </div>

    <div class="section-header">Artículos / Insumos</div>
    <div style="display:flex;gap:6px;margin-bottom:6px;flex-wrap:wrap;position:relative">
      <input id="cArtInput" class="input-field" style="flex:2;min-width:130px"
        placeholder="Buscar artículo…" autocomplete="off" oninput="dropArtCompra(this.value)">
      <div id="ddArtC" class="autocomplete-drop" style="display:none;left:0;top:100%"></div>
      <input id="cArtCant" type="number" class="input-field" style="width:70px"
        placeholder="Cant" value="1" min="0.001" step="0.001">
      <input id="cArtPrecio" type="number" class="input-field" style="width:90px"
        placeholder="Precio" min="0" step="0.01">
      <button class="btn btn-sm btn-primary" onclick="addItemCompra()">＋</button>
    </div>
    <div id="tablaCompraItems" style="margin-bottom:12px"></div>

    <div class="card" style="padding:12px;margin-bottom:12px;font-size:10pt">
      <div class="flex-between mb-4"><span>Subtotal</span><span id="cSub" class="mono">0.00</span></div>
      <div class="flex-between mb-4"><span>Descuento</span><span id="cDsc" class="mono">-0.00</span></div>
      <div class="flex-between mb-4"><span>IVA</span><span id="cIva2" class="mono">0.00</span></div>
      <div class="flex-between bold"><span>TOTAL USD</span>
        <span id="cTot" class="mono text-cyan" style="font-size:14pt">0.00</span></div>
    </div>

    <div class="form-group mb-16">
      <label class="form-label">Observaciones</label>
      <textarea id="cObs" class="input-field" rows="2"></textarea>
    </div>

    <div style="display:flex;gap:8px;flex-direction:column">
      <button class="btn btn-primary btn-full" onclick="guardarCompra()">Guardar compra</button>
      <button class="btn btn-ghost btn-full" onclick="closeModal('modalFormC')">Cancelar</button>
    </div>`;
    openModal('modalFormC');
    renderTablaCompraItems();
}

// ── Autocomplete proveedores ──────────────────────────────────────────────
let timerProv = null;
function dropProveedores(q) {
    clearTimeout(timerProv);
    const dd = document.getElementById('ddProv');
    if (q.length < 2) { dd.style.display='none'; return; }
    timerProv = setTimeout(async () => {
        try {
            const lista = await apiGet(`/api/proveedores?q=${encodeURIComponent(q)}`) || [];
            if (!lista.length) { dd.style.display='none'; return; }
            dd.innerHTML = lista.slice(0,8).map(p =>
                `<div class="ac-item" onclick="selProv('${esh(p.codigo)}','${esh(p.nombre)}')">
                    <strong>${esh(p.nombre)}</strong>
                    <span class="text-dim"> · ${esh(p.rif||'')} · ${esh(p.telefono||'')}</span>
                </div>`).join('');
            dd.style.display = 'block';
        } catch {}
    }, 350);
}

function selProv(codigo, nombre) {
    document.getElementById('cProvInput').value = nombre;
    document.getElementById('cProvCod').value   = codigo;
    document.getElementById('ddProv').style.display = 'none';
}

// ── Autocomplete artículos (inventario) ───────────────────────────────────
let timerArtC = null;
function dropArtCompra(q) {
    clearTimeout(timerArtC);
    const dd = document.getElementById('ddArtC');
    if (q.length < 2) { dd.style.display='none'; return; }
    timerArtC = setTimeout(async () => {
        try {
            const lista = await apiGet(`/api/inventario?q=${encodeURIComponent(q)}`) || [];
            if (!lista.length) { dd.style.display='none'; return; }
            dd.innerHTML = lista.slice(0,8).map(a =>
                `<div class="ac-item" onclick="selArtCompra('${esh(a.codigo)}','${esh(a.descripcion)}',${a.costoActual||0})">
                    <strong>${esh(a.descripcion)}</strong>
                    <span class="text-dim"> · ${esh(a.codigo)} · Stock: ${formatNum(a.stock||0,0)}</span>
                </div>`).join('');
            dd.style.display = 'block';
        } catch {}
    }, 350);
}

function selArtCompra(codigo, desc, costo) {
    const inp = document.getElementById('cArtInput');
    inp.value = desc; inp.dataset.codigo = codigo;
    document.getElementById('cArtPrecio').value = costo || '';
    document.getElementById('ddArtC').style.display = 'none';
}

// ── Items compra ──────────────────────────────────────────────────────────
function addItemCompra() {
    const inp    = document.getElementById('cArtInput');
    const codigo = inp.dataset.codigo || '';
    const desc   = inp.value.trim();
    const cant   = parseFloat(document.getElementById('cArtCant').value) || 0;
    const precio = parseFloat(document.getElementById('cArtPrecio').value) || 0;
    if (!codigo && !desc) { alert('Selecciona un artículo.'); return; }
    if (cant <= 0)  { alert('Cantidad debe ser mayor a 0.'); return; }
    if (precio <= 0){ alert('Precio debe ser mayor a 0.'); return; }
    const idx = itemsCompra.findIndex(i => i.codigo === codigo && codigo !== '');
    if (idx >= 0) { itemsCompra[idx].cantidad += cant; }
    else { itemsCompra.push({ codigo, descripcion: desc, cantidad: cant, precio }); }
    inp.value = ''; inp.dataset.codigo = '';
    document.getElementById('cArtCant').value = '1';
    document.getElementById('cArtPrecio').value = '';
    renderTablaCompraItems();
    recalcCompra();
}

function quitarItemCompra(ix) {
    itemsCompra.splice(ix, 1);
    renderTablaCompraItems();
    recalcCompra();
}

function renderTablaCompraItems() {
    const div = document.getElementById('tablaCompraItems');
    if (!div) return;
    if (!itemsCompra.length) {
        div.innerHTML = `<div class="text-dim text-sm" style="padding:8px 0">Sin artículos aún.</div>`;
        return;
    }
    div.innerHTML = `<div class="card" style="padding:0">
        ${itemsCompra.map((it,ix) => `
        <div class="list-item" style="padding:8px 12px;border-bottom:1px solid var(--borde)">
          <div class="list-item-main">
            <div class="list-item-title">${esh(it.descripcion||it.codigo)}</div>
            <div class="list-item-sub">${formatNum(it.cantidad,3)} × ${formatUSD(it.precio)}</div>
          </div>
          <div style="display:flex;align-items:center;gap:10px">
            <span class="mono">${formatUSD(it.cantidad*it.precio)}</span>
            <button class="btn-icon-del" onclick="quitarItemCompra(${ix})" title="Quitar">✕</button>
          </div>
        </div>`).join('')}
    </div>`;
}

function recalcCompra() {
    const sub  = itemsCompra.reduce((s,it)=>s+it.cantidad*it.precio, 0);
    const pctD = parseFloat(document.getElementById('cDesc')?.value) || 0;
    const pctI = parseFloat(document.getElementById('cIva')?.value) || 0;
    const desc = sub * pctD / 100;
    const base = sub - desc;
    const iva  = base * pctI / 100;
    const tot  = base + iva;
    const set  = (id,v) => { const el=document.getElementById(id); if(el) el.textContent=formatUSD(v); };
    set('cSub', sub); set('cDsc', desc); set('cIva2', iva); set('cTot', tot);
}

// ── Guardar compra ────────────────────────────────────────────────────────
async function guardarCompra() {
    const prov = document.getElementById('cProvCod')?.value?.trim();
    if (!prov) { alert('Selecciona un proveedor.'); return; }
    if (!itemsCompra.length) { alert('Agrega al menos un artículo.'); return; }
    const body = {
        compraN:      null,
        fechaC:       document.getElementById('cFecha')?.value || null,
        codigoP:      prov,
        formaDePago:  document.getElementById('cFormaPago')?.value || 'Contado',
        descuentoC:   parseFloat(document.getElementById('cDesc')?.value) || 0,
        pctIva:       parseFloat(document.getElementById('cIva')?.value) || 16,
        diasDeCred:   parseInt(document.getElementById('cDias')?.value) || 0,
        observaciones:document.getElementById('cObs')?.value?.trim() || '',
        items: itemsCompra.map(it => ({
            codigo: it.codigo, cantidad: it.cantidad, precioUnit: it.precio
        }))
    };
    const btn = document.querySelector('#drawerFormC .btn-primary');
    if (btn) { btn.disabled=true; btn.textContent='Guardando…'; }
    try {
        await apiPost('/api/compras', body);
        closeModal('modalFormC');
        await cargarCompras();
    } catch(err) {
        if (btn) { btn.disabled=false; btn.textContent='Guardar compra'; }
        alert('Error al guardar: ' + err.message);
    }
}

render();
