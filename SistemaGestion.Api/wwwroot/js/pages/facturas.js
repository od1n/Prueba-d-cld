// facturas.js — CRUD completo + impresion
if (!Auth.requireAuth()) {}
const app = document.getElementById('app');

// -- Estado ---------------------------------------------------------------
let todasFact = [];
let itemsFactura = [];
let timerBusqFact = null;
let timerBusqCli  = null;
let timerBusqArt  = null;

// -- Render principal -----------------------------------------------------
async function render() {
    app.innerHTML = `
    ${renderTopbar('Facturas')}
    <div class="content">
      <div class="search-wrap">
        <span class="search-icon">&#8981;</span>
        <input class="input-field" id="buscarF" type="search"
          placeholder="Numero, cliente..." autocomplete="off" oninput="filtrarFact(this.value)">
      </div>
      <div style="display:flex;gap:8px;padding:0 16px 10px;flex-wrap:wrap">
        <input type="date" id="fDesde" class="input-field" style="flex:1;min-width:120px"
          title="Desde" onchange="cargarFacturas()">
        <input type="date" id="fHasta" class="input-field" style="flex:1;min-width:120px"
          title="Hasta" onchange="cargarFacturas()">
        <button class="btn btn-primary" style="white-space:nowrap"
          onclick="abrirNueva()">+ Nueva</button>
      </div>
      <div id="statsBar" style="display:none;padding:0 16px 10px;gap:8px;display:flex;flex-wrap:wrap"></div>
      <div id="listaFact"><div class="loading-msg"><div class="spinner"></div></div></div>
    </div>
    <div class="modal-overlay" id="modalDetalle">
      <div class="modal-drawer" id="drawerDetalle"></div>
    </div>
    <div class="modal-overlay" id="modalForm">
      <div class="modal-drawer" id="drawerForm" style="max-height:95vh;overflow-y:auto"></div>
    </div>
    <div class="modal-overlay" id="modalAbono">
      <div class="modal-drawer" id="drawerAbono"></div>
    </div>
    ${renderNav('facturas')}`;
    await cargarFacturas();
}

// -- Carga y filtrado -----------------------------------------------------
async function cargarFacturas() {
    const desde = document.getElementById('fDesde')?.value || '';
    const hasta = document.getElementById('fHasta')?.value || '';
    const q     = document.getElementById('buscarF')?.value || '';
    let url = '/api/facturas';
    const ps = [];
    if (q)     ps.push('q='+encodeURIComponent(q));
    if (desde) ps.push('desde='+encodeURIComponent(desde));
    if (hasta) ps.push('hasta='+encodeURIComponent(hasta));
    if (ps.length) url += '?'+ps.join('&');
    try {
        todasFact = await apiGet(url) || [];
        renderListaFact(todasFact);
        renderStats(todasFact);
    } catch(err) {
        document.getElementById('listaFact').innerHTML =
            `<div class="alert-banner">Error cargando facturas: ${esh(err.message)}</div>`;
    }
}

function filtrarFact(q) {
    clearTimeout(timerBusqFact);
    timerBusqFact = setTimeout(() => cargarFacturas(), 400);
}

function renderStats(lista) {
    const bar = document.getElementById('statsBar');
    if (!bar || lista.length === 0) { if(bar) bar.style.display='none'; return; }
    const total = lista.reduce((s,f)=>s+(f.total||0),0);
    const pend  = lista.filter(f=>!f.pagada).reduce((s,f)=>s+(f.saldo||0),0);
    bar.style.display = 'flex';
    bar.innerHTML = `
      <div class="kpi" style="flex:1"><div class="kpi-label">Facturas</div>
        <div class="kpi-value">${lista.length}</div></div>
      <div class="kpi" style="flex:1"><div class="kpi-label">Total USD</div>
        <div class="kpi-value text-cyan">${formatUSD(total)}</div></div>
      <div class="kpi" style="flex:1"><div class="kpi-label">Por cobrar</div>
        <div class="kpi-value" style="color:var(--naranja)">${formatUSD(pend)}</div></div>`;
}

function renderListaFact(lista) {
    const div = document.getElementById('listaFact');
    if (!div) return;
    if (lista.length === 0) {
        div.innerHTML = `<div class="empty"><div class="empty-icon">&#9636;</div>
            <div class="empty-text">Sin facturas</div></div>`;
        return;
    }
    div.innerHTML = `<div class="card" style="padding:0">
        ${lista.map((f,ix) => {
            const pagTag = f.pagada
                ? '<span class="tag tag-verde">Pagada</span>'
                : estaVencida(f.fecha)
                    ? '<span class="tag tag-rojo">Vencida</span>'
                    : '<span class="tag tag-naranja">Pendiente</span>';
            return `
            <button type="button" class="list-item js-fact-btn" data-idx="${ix}"
              style="padding:12px 16px;background:none;border:none;border-bottom:1px solid var(--borde);
                     text-align:left;width:100%;cursor:pointer;color:inherit;font:inherit">
              <div class="list-item-main">
                <div class="list-item-title">${esh(f.cliente || '(sin cliente)')}</div>
                <div class="list-item-sub">${esh(f.numeroF)} &middot; ${formatFecha(f.fecha)} &middot; ${esh(f.formaDePago || '')}</div>
              </div>
              <div style="text-align:right;flex-shrink:0;margin-left:8px">
                <div class="mono" style="font-size:13pt">${formatUSD(f.total||0)}</div>
                ${(f.saldo||0) > 0 && !f.pagada ? '<div class="text-dim text-sm">Saldo: '+formatUSD(f.saldo)+'</div>' : ''}
                ${pagTag}
              </div>
            </button>`;
        }).join('')}
    </div>`;
    div.querySelectorAll('.js-fact-btn').forEach(btn => {
        btn.addEventListener('click', () => verDetalle(lista[+btn.dataset.idx]));
    });
}

// -- Ver detalle ----------------------------------------------------------
async function verDetalle(fact) {
    const drawer = document.getElementById('drawerDetalle');
    drawer.innerHTML = `<div class="modal-handle"></div>
        <div class="modal-title">${esh(fact.numeroF)}</div><div class="spinner"></div>`;
    openModal('modalDetalle');
    try {
        const resp = await apiGet(`/api/facturas/${encodeURIComponent(fact.numeroF)}`);
        const f = resp.factura || resp;
        const lineas = resp.lineas || [];
        const nombreCli = resp.nombreCliente || f.cliente || '';
        const pagTag = f.pagada
            ? '<span class="tag tag-verde">Pagada</span>'
            : estaVencida(f.fecha)
                ? '<span class="tag tag-rojo">Vencida</span>'
                : '<span class="tag tag-naranja">Pendiente</span>';
        drawer.innerHTML = `
        <div class="modal-handle"></div>
        <div class="flex-between mb-8">
          <div class="modal-title" style="margin-bottom:0">${esh(f.numeroF)}</div>
          ${pagTag}
        </div>
        <div class="text-dim text-sm mb-16">
          ${esh(nombreCli)} &middot; ${formatFecha(f.fecha)} &middot; ${esh(f.formaDePago || '')}
          ${f.diasCredito > 0 ? ' &middot; '+f.diasCredito+' dias credito' : ''}
        </div>
        ${lineas.length ? `
        <div class="card" style="margin-bottom:12px;padding:0">
          ${lineas.map(l => `
            <div class="list-item" style="padding:10px 14px;border-bottom:1px solid var(--borde)">
              <div class="list-item-main">
                <div class="list-item-title">${esh(l.descripcion || l.codigoItem)}</div>
                <div class="list-item-sub">${formatNum(l.cantidad||0,2)} x ${formatUSD(l.precioUnit||0)}</div>
              </div>
              <div class="list-item-value">${formatUSD(l.subtotal || (l.cantidad||0)*(l.precioUnit||0))}</div>
            </div>`).join('')}
        </div>` : ''}
        ${(f.descuento||0) > 0 ? `<div class="flex-between mb-4">
          <span class="text-dim">Descuento (${formatNum(f.descuento,0)}%)</span>
          <span class="mono" style="color:var(--naranja)">-${formatUSD((f.total - f.iva) * f.descuento / (100 - f.descuento))}</span>
        </div>` : ''}
        <div class="flex-between mb-4">
          <span class="text-dim">IVA</span><span class="mono">${formatUSD(f.iva||0)}</span>
        </div>
        <div class="flex-between bold mb-8">
          <span>Total</span><span class="mono text-cyan">${formatUSD(f.total||0)}</span>
        </div>
        ${!f.pagada && (f.saldo||0) > 0 ? `<div class="flex-between mb-4">
          <span class="text-dim">Abonado</span><span class="mono">${formatUSD(f.abono||0)}</span>
        </div>
        <div class="flex-between mb-8">
          <span class="text-dim">Saldo pendiente</span>
          <span class="mono" style="color:var(--naranja)">${formatUSD(f.saldo||0)}</span>
        </div>` : ''}
        ${f.observaciones ? `<div class="text-dim text-sm mb-12">${esh(f.observaciones)}</div>` : ''}
        <div style="display:flex;flex-direction:column;gap:8px;margin-top:12px">
          <a href="/api/facturas/${encodeURIComponent(f.numeroF)}/imprimir" target="_blank"
            class="btn btn-ghost btn-full">&#128424; Imprimir / PDF</a>
          ${!f.pagada ? `<button class="btn btn-cyan btn-full" onclick="closeModal('modalDetalle');abonarFactura('${esh(f.numeroF)}',${f.saldo||0})">Registrar abono</button>` : ''}
          <button class="btn btn-ghost btn-full" onclick="closeModal('modalDetalle')">Cerrar</button>
        </div>`;
    } catch(err) {
        drawer.innerHTML = `<div class="modal-handle"></div>
            <div class="alert-banner">Error: ${esh(err.message)}</div>
            <button class="btn btn-ghost btn-full" onclick="closeModal('modalDetalle')">Cerrar</button>`;
    }
}

// -- Abono ----------------------------------------------------------------
function abonarFactura(numero, saldoPendiente) {
    const drawer = document.getElementById('drawerAbono');
    drawer.innerHTML = `
    <div class="modal-handle"></div>
    <div class="modal-title">Registrar Abono</div>
    <div class="text-dim text-sm mb-12">Factura: <strong>${esh(numero)}</strong></div>
    <div class="card mb-12" style="padding:12px">
      <div class="flex-between">
        <span class="text-dim">Saldo pendiente</span>
        <span class="mono bold" style="color:var(--naranja)">${formatUSD(saldoPendiente)}</span>
      </div>
    </div>
    <div class="form-group mb-12">
      <label class="form-label">Monto del abono (USD)</label>
      <input id="abonoMonto" type="number" class="input-field" min="0.01"
        max="${saldoPendiente}" step="0.01" value="${saldoPendiente}">
    </div>
    <div id="abonoMsg"></div>
    <div style="display:flex;flex-direction:column;gap:8px;margin-top:12px">
      <button class="btn btn-primary btn-full" id="btnGuardarAbono"
        onclick="guardarAbono('${esh(numero)}')">Confirmar abono</button>
      <button class="btn btn-ghost btn-full" onclick="closeModal('modalAbono')">Cancelar</button>
    </div>`;
    openModal('modalAbono');
    document.getElementById('abonoMonto')?.focus();
}

async function guardarAbono(numero) {
    const monto = parseFloat(document.getElementById('abonoMonto')?.value) || 0;
    if (monto <= 0) { alert('El monto debe ser mayor a 0.'); return; }
    const btn = document.getElementById('btnGuardarAbono');
    if (btn) { btn.disabled = true; btn.textContent = 'Procesando...'; }
    try {
        const resp = await apiPost(`/api/facturas/${encodeURIComponent(numero)}/pago`, { monto });
        closeModal('modalAbono');
        await cargarFacturas();
        if (resp.pagada) alert('Factura '+numero+' pagada en su totalidad.');
        else alert('Abono registrado. Saldo restante: $'+(resp.saldo||0).toFixed(2));
    } catch(err) {
        if (btn) { btn.disabled=false; btn.textContent='Confirmar abono'; }
        alert('Error: '+err.message);
    }
}

// -- Nueva factura --------------------------------------------------------

// Carga el IVA configurado desde la API y lo aplica al campo del formulario
async function cargarIvaConfig() {
    try {
        const cfg = await apiGet('/api/config/legal');
        const raw = cfg?.IVA_Porcentaje;
        const pct = (raw !== null && raw !== undefined && raw !== '') ? parseFloat(raw) : 16;
        const el  = document.getElementById('fIva');
        if (el) {
            el.innerHTML = `<option value="${pct}">${pct}%</option><option value="0">0% (Nota de Entrega)</option>`;
            el.value = pct;
            recalcFactura();
        }
    } catch (_) { /* si falla queda el default 16 del HTML */ }
}

function abrirNueva() {
    itemsFactura = [];
    const hoy = new Date().toISOString().slice(0,10);
    const drawer = document.getElementById('drawerForm');
    drawer.innerHTML = `
    <div class="modal-handle"></div>
    <div class="modal-title">Nueva Factura</div>
    <div class="form-group mb-8" style="position:relative">
      <label class="form-label">Cliente</label>
      <input id="fCliInput" class="input-field" placeholder="Buscar cliente..."
        autocomplete="off" oninput="dropClientes(this.value)" style="padding-right:42px">
      <input type="hidden" id="fCliCod">
      <div id="ddCli" class="autocomplete-drop" style="display:none"></div>
      <button type="button" onclick="crearClienteRapido()" class="btn btn-sm"
        style="position:absolute;right:4px;top:28px;padding:2px 8px;background:#10a37f;color:#fff;border:none;border-radius:4px;cursor:pointer;font-weight:700;line-height:1.4"
        title="Crear cliente nuevo">+</button>
    </div>
    <div class="form-grid-2 mb-8">
      <div class="form-group">
        <label class="form-label">Fecha</label>
        <input id="fFecha" type="date" class="input-field" value="${hoy}">
      </div>
      <div class="form-group">
        <label class="form-label">Forma de pago</label>
        <select id="fFormaPago" class="input-field" onchange="onFormaPagoChange()">
          <option value="Contado">Contado</option>
          <option value="Credito 15">Credito 15 dias</option>
          <option value="Credito 30">Credito 30 dias</option>
          <option value="Credito 60">Credito 60 dias</option>
        </select>
      </div>
    </div>
    <div class="form-grid-2 mb-12">
      <div class="form-group">
        <label class="form-label">IVA %</label>
        <select id="fIva" class="input-field" onchange="recalcFactura()">
          <option value="16">16%</option>
          <option value="0">0% (Nota de Entrega)</option>
        </select>
      </div>
      <div class="form-group">
        <label class="form-label">Descuento %</label>
        <input id="fDesc" type="number" class="input-field" value="0" min="0" max="100" oninput="recalcFactura()">
      </div>
    </div>
    <div class="form-group mb-12" id="diasCreditoWrap" style="display:none">
      <label class="form-label">Dias de credito</label>
      <input id="fDiasCredito" type="number" class="input-field" value="30" min="1">
    </div>
    <div class="section-header">Articulos</div>
    <div style="display:flex;gap:6px;margin-bottom:6px;flex-wrap:wrap;position:relative">
      <input id="fArtInput" class="input-field" style="flex:2;min-width:130px"
        placeholder="Buscar articulo..." autocomplete="off" oninput="dropArticulos(this.value)">
      <div id="ddArt" class="autocomplete-drop" style="display:none"></div>
      <input id="fArtCant" type="number" class="input-field" style="width:70px" placeholder="Cant" value="1" min="0.001" step="0.001">
      <input id="fArtPrecio" type="number" class="input-field" style="width:90px" placeholder="Precio" min="0" step="0.01">
      <button class="btn btn-sm btn-primary" onclick="addItemFactura()">+</button>
    </div>
    <div id="tablaFactItems" style="margin-bottom:12px"></div>
    <div class="card" style="padding:12px;margin-bottom:12px;font-size:10pt">
      <div class="flex-between mb-4"><span>Subtotal</span><span id="fSub" class="mono">0.00</span></div>
      <div class="flex-between mb-4"><span>Descuento</span><span id="fDsc" class="mono">-0.00</span></div>
      <div class="flex-between mb-4"><span>IVA</span><span id="fIva2" class="mono">0.00</span></div>
      <div class="flex-between bold"><span>TOTAL USD</span>
        <span id="fTot" class="mono text-cyan" style="font-size:14pt">0.00</span></div>
    </div>
    <div class="form-group mb-16">
      <label class="form-label">Observaciones</label>
      <textarea id="fObs" class="input-field" rows="2"></textarea>
    </div>
    <div style="display:flex;gap:8px;flex-direction:column">
      <button class="btn btn-primary btn-full" id="btnGuardarFact" onclick="guardarFactura()">Guardar factura</button>
      <button class="btn btn-ghost btn-full" onclick="closeModal('modalForm')">Cancelar</button>
    </div>`;
    openModal('modalForm');
    renderTablaFactItems();
    onFormaPagoChange();
    cargarIvaConfig(); // aplica IVA configurado (async, no bloquea)
}

function onFormaPagoChange() {
    const fp = document.getElementById('fFormaPago')?.value || '';
    const wrap = document.getElementById('diasCreditoWrap');
    if (!wrap) return;
    if (fp.startsWith('Credito')) {
        wrap.style.display = 'block';
        const dias = parseInt(fp.replace(/\D/g,'')) || 30;
        const inp = document.getElementById('fDiasCredito');
        if (inp) inp.value = dias;
    } else { wrap.style.display = 'none'; }
}

// -- Autocomplete clientes ------------------------------------------------
function dropClientes(q) {
    clearTimeout(timerBusqCli);
    const dd = document.getElementById('ddCli');
    if (q.length < 2) { dd.style.display='none'; return; }
    timerBusqCli = setTimeout(async () => {
        try {
            const lista = await apiGet(`/api/clientes?q=${encodeURIComponent(q)}`) || [];
            if (!lista.length) { dd.style.display='none'; return; }
            dd.innerHTML = lista.slice(0,8).map(c =>
                `<div class="ac-item" onmouseover="this.style.background='var(--borde)'" onmouseout="this.style.background=''"
                  onclick="selCliente('${esh(c.codigo)}','${esh(c.nombre)}')">
                    <strong>${esh(c.nombre)}</strong>
                    <span class="text-dim"> &middot; ${esh(c.ciRif||'')}</span>
                </div>`).join('');
            dd.style.display = 'block';
        } catch {}
    }, 350);
}
function selCliente(codigo, nombre) {
    document.getElementById('fCliInput').value = nombre;
    document.getElementById('fCliCod').value = codigo;
    document.getElementById('ddCli').style.display = 'none';
}

// -- Autocomplete articulos -----------------------------------------------
function dropArticulos(q) {
    clearTimeout(timerBusqArt);
    const dd = document.getElementById('ddArt');
    if (q.length < 2) { dd.style.display='none'; return; }
    timerBusqArt = setTimeout(async () => {
        try {
            const lista = await apiGet(`/api/inventario?q=${encodeURIComponent(q)}`) || [];
            if (!lista.length) { dd.style.display='none'; return; }
            dd.innerHTML = lista.slice(0,8).map(a =>
                `<div class="ac-item" onmouseover="this.style.background='var(--borde)'" onmouseout="this.style.background=''"
                  onclick="selArticulo('${esh(a.codigo)}','${esh(a.descripcion)}',${a.precioActual||a.precio||0})">
                    <strong>${esh(a.descripcion)}</strong>
                    <span class="text-dim"> &middot; ${esh(a.codigo)} &middot; Stock: ${formatNum(a.stock||a.existencia||0,0)}</span>
                </div>`).join('');
            dd.style.display = 'block';
        } catch {}
    }, 350);
}
function selArticulo(codigo, desc, precio) {
    const inp = document.getElementById('fArtInput');
    inp.value = desc; inp.dataset.codigo = codigo;
    document.getElementById('fArtPrecio').value = precio || '';
    document.getElementById('ddArt').style.display = 'none';
}

// -- Items factura --------------------------------------------------------
function addItemFactura() {
    const inp = document.getElementById('fArtInput');
    const codigo = inp.dataset.codigo || '';
    const desc = inp.value.trim();
    const cant = parseFloat(document.getElementById('fArtCant').value) || 0;
    const precio = parseFloat(document.getElementById('fArtPrecio').value) || 0;
    if (!codigo && !desc) { alert('Selecciona un articulo.'); return; }
    if (cant <= 0) { alert('Cantidad debe ser mayor a 0.'); return; }
    if (precio <= 0) { alert('Precio debe ser mayor a 0.'); return; }
    const idx = itemsFactura.findIndex(i => i.codigo === codigo && codigo !== '');
    if (idx >= 0) { itemsFactura[idx].cantidad += cant; }
    else { itemsFactura.push({ codigo, descripcion: desc, cantidad: cant, precio }); }
    inp.value = ''; inp.dataset.codigo = '';
    document.getElementById('fArtCant').value = '1';
    document.getElementById('fArtPrecio').value = '';
    renderTablaFactItems();
    recalcFactura();
}
function quitarItemFactura(ix) { itemsFactura.splice(ix,1); renderTablaFactItems(); recalcFactura(); }

function renderTablaFactItems() {
    const div = document.getElementById('tablaFactItems');
    if (!div) return;
    if (!itemsFactura.length) {
        div.innerHTML = `<div class="text-dim text-sm" style="padding:8px 0">Sin articulos aun.</div>`;
        return;
    }
    div.innerHTML = `<div class="card" style="padding:0">
        ${itemsFactura.map((it,ix) => `
        <div class="list-item" style="padding:8px 12px;border-bottom:1px solid var(--borde)">
          <div class="list-item-main">
            <div class="list-item-title">${esh(it.descripcion||it.codigo)}</div>
            <div class="list-item-sub">${formatNum(it.cantidad,3)} x ${formatUSD(it.precio)}</div>
          </div>
          <div style="display:flex;align-items:center;gap:10px">
            <span class="mono">${formatUSD(it.cantidad*it.precio)}</span>
            <button style="background:none;border:none;color:var(--rojo);cursor:pointer;font-size:14pt;padding:2px 6px"
              onclick="quitarItemFactura(${ix})">&#10005;</button>
          </div>
        </div>`).join('')}
    </div>`;
}

function recalcFactura() {
    const sub = itemsFactura.reduce((s,it)=>s+it.cantidad*it.precio, 0);
    const pctD = parseFloat(document.getElementById('fDesc')?.value) || 0;
    const pctI = parseFloat(document.getElementById('fIva')?.value) || 0;
    const desc = sub * pctD / 100;
    const base = sub - desc;
    const iva = base * pctI / 100;
    const tot = base + iva;
    const set = (id,v) => { const el=document.getElementById(id); if(el) el.textContent=formatUSD(v); };
    set('fSub', sub); set('fDsc', desc); set('fIva2', iva); set('fTot', tot);
}

// -- Guardar factura ------------------------------------------------------
async function guardarFactura() {
    const cli = document.getElementById('fCliCod')?.value?.trim();
    if (!cli) { alert('Selecciona un cliente.'); return; }
    if (!itemsFactura.length) { alert('Agrega al menos un articulo.'); return; }
    const fp = document.getElementById('fFormaPago')?.value || 'Contado';
    let diasCredito = 0;
    if (fp.startsWith('Credito')) diasCredito = parseInt(document.getElementById('fDiasCredito')?.value) || 30;
    const body = {
        numeroF: null,
        fecha: document.getElementById('fFecha')?.value || null,
        codigoCliente: cli,
        formaDePago: fp,
        pctIva: parseFloat(document.getElementById('fIva')?.value ?? '16'),
        descuento: parseFloat(document.getElementById('fDesc')?.value) || 0,
        diasCredito: diasCredito,
        observaciones: document.getElementById('fObs')?.value?.trim() || '',
        items: itemsFactura.map(it => ({ codigo: it.codigo, cantidad: it.cantidad, precioUnit: it.precio }))
    };
    const btn = document.getElementById('btnGuardarFact');
    if (btn) { btn.disabled=true; btn.textContent='Guardando...'; }
    try {
        const resp = await apiPost('/api/facturas', body);
        closeModal('modalForm');
        await cargarFacturas();
        alert('Factura '+(resp.numeroF||'')+' creada. Total: $'+(resp.total||0).toFixed(2));
    } catch(err) {
        if (btn) { btn.disabled=false; btn.textContent='Guardar factura'; }
        alert('Error: '+err.message);
    }
}

// -- Crear cliente rápido -------------------------------------------------
async function crearClienteRapido() {
    // Construir modal inline
    const modalId = 'modalCrearCli';
    let existing = document.getElementById(modalId);
    if (existing) existing.remove();

    const overlay = document.createElement('div');
    overlay.id = modalId;
    overlay.style.cssText = 'position:fixed;inset:0;background:rgba(0,0,0,.45);z-index:9999;display:flex;align-items:center;justify-content:center';
    overlay.innerHTML = `
      <div style="background:var(--fondo,#1e1e2e);border:1px solid var(--borde,#333);border-radius:10px;padding:24px;width:360px;max-width:95vw;font-size:14px">
        <div style="font-size:16px;font-weight:700;margin-bottom:16px">+ Nuevo Cliente</div>
        <div class="form-group mb-8">
          <label class="form-label">Código *</label>
          <input id="ccCodigo" class="input-field" placeholder="Ej: 101">
        </div>
        <div class="form-group mb-8">
          <label class="form-label">Nombre *</label>
          <input id="ccNombre" class="input-field" placeholder="Nombre o razón social">
        </div>
        <div class="form-group mb-8">
          <label class="form-label">CI / RIF</label>
          <input id="ccRif" class="input-field" placeholder="J-12345678-9">
        </div>
        <div class="form-group mb-16">
          <label class="form-label">Teléfono</label>
          <input id="ccTel" class="input-field" placeholder="0414-0000000">
        </div>
        <div style="display:flex;gap:8px;justify-content:flex-end">
          <button class="btn" onclick="document.getElementById('${modalId}').remove()">Cancelar</button>
          <button class="btn btn-primary" id="btnGuardarCli" onclick="guardarClienteRapido()">Guardar</button>
        </div>
      </div>`;
    document.body.appendChild(overlay);
    overlay.addEventListener('click', e => { if (e.target === overlay) overlay.remove(); });
    document.getElementById('ccNombre')?.focus();
}

async function guardarClienteRapido() {
    const cod  = document.getElementById('ccCodigo')?.value?.trim();
    const nom  = document.getElementById('ccNombre')?.value?.trim();
    const rif  = document.getElementById('ccRif')?.value?.trim() || '';
    const tel  = document.getElementById('ccTel')?.value?.trim() || '';

    if (!cod || !nom) { alert('Código y nombre son obligatorios.'); return; }

    const btn = document.getElementById('btnGuardarCli');
    if (btn) { btn.disabled = true; btn.textContent = 'Guardando...'; }

    try {
        const resp = await apiPost('/api/clientes', { codigo: cod, descripcion: nom, ciRif: rif, telefono: tel });
        document.getElementById('modalCrearCli')?.remove();
        // Seleccionar el nuevo cliente en el formulario
        document.getElementById('fCliInput').value = nom;
        document.getElementById('fCliCod').value = resp.codigo || cod;
    } catch(err) {
        if (btn) { btn.disabled = false; btn.textContent = 'Guardar'; }
        alert('Error: ' + err.message);
    }
}

render();
