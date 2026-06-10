// facturas.js — CRUD completo + impresión
if (!Auth.requireAuth()) {}
const app = document.getElementById('app');

// ── Estado ────────────────────────────────────────────────────────────────
let todasFact = [];
let itemsFactura = [];   // líneas del formulario actual
let timerBusqFact = null;
let timerBusqCli  = null;
let timerBusqArt  = null;

// ── Render principal ──────────────────────────────────────────────────────
async function render() {
    app.innerHTML = `
    ${renderTopbar('Facturas')}
    <div class="content">
      <!-- Filtros -->
      <div class="search-wrap">
        <span class="search-icon">⌕</span>
        <input class="input-field" id="buscarF" type="search"
          placeholder="Número, cliente…" autocomplete="off" oninput="filtrarFact(this.value)">
      </div>
      <div style="display:flex;gap:8px;padding:0 16px 10px;flex-wrap:wrap">
        <input type="date" id="fDesde" class="input-field" style="flex:1;min-width:120px"
          title="Desde" onchange="cargarFacturas()">
        <input type="date" id="fHasta" class="input-field" style="flex:1;min-width:120px"
          title="Hasta" onchange="cargarFacturas()">
        <button class="btn btn-primary" style="white-space:nowrap"
          onclick="abrirNueva()">＋ Nueva</button>
      </div>
      <!-- Resumen rápido -->
      <div id="statsBar" style="display:none;padding:0 16px 10px;gap:8px;display:flex;flex-wrap:wrap"></div>
      <!-- Lista -->
      <div id="listaFact"><div class="loading-msg"><div class="spinner"></div></div></div>
    </div>

    <!-- Modal detalle/acciones -->
    <div class="modal-overlay" id="modalDetalle">
      <div class="modal-drawer" id="drawerDetalle"></div>
    </div>

    <!-- Modal formulario nueva factura -->
    <div class="modal-overlay" id="modalForm">
      <div class="modal-drawer" id="drawerForm" style="max-height:95vh;overflow-y:auto"></div>
    </div>

    <!-- Modal abono -->
    <div class="modal-overlay" id="modalAbono">
      <div class="modal-drawer" id="drawerAbono"></div>
    </div>

    ${renderNav('facturas')}`;

    await cargarFacturas();
}

// ── Carga y filtrado ──────────────────────────────────────────────────────
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
        div.innerHTML = `<div class="empty"><div class="empty-icon">▤</div>
            <div class="empty-text">Sin facturas</div></div>`;
        return;
    }
    div.innerHTML = `<div class="card" style="padding:0">
        ${lista.map((f,ix) => {
            const pagTag = f.pagada
                ? '<span class="tag tag-verde">Pagada</span>'
                : estaVencida(f.fecha, f.diasCredito)
                    ? '<span class="tag tag-rojo">Vencida</span>'
                    : '<span class="tag tag-naranja">Pendiente</span>';
            return `
            <button type="button" class="list-item js-fact-btn" data-idx="${ix}"
              style="padding:12px 16px;background:none;border:none;border-bottom:1px solid var(--borde);
                     text-align:left;width:100%;cursor:pointer;color:inherit;font:inherit">
        