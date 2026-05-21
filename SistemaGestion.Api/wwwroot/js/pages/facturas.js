// facturas.js
if (!Auth.requireAuth()) {}
const app = document.getElementById('app');

async function render() {
    app.innerHTML = `
        ${renderTopbar('Facturas recientes')}
        <div class="content">
            <div class="search-wrap">
                <span class="search-icon">⌕</span>
                <input class="input-field" id="buscar" type="search"
                    placeholder="Buscar por cliente o número..."
                    autocomplete="off" oninput="filtrar(this.value)">
            </div>
            <div id="lista"><div class="loading-msg"><div class="spinner"></div></div></div>
        </div>
        <div class="modal-overlay" id="modalFact">
            <div class="modal-drawer" id="drawerFact"></div>
        </div>
        ${renderNav('facturas')}`;
    await cargar();
}

let todas = [];
async function cargar() {
    try {
        todas = await apiGet('/api/facturas') || [];
        renderLista(todas);
    } catch (err) {
        document.getElementById('lista').innerHTML =
            `<div class="alert-banner">Error: ${esh(err.message)}</div>`;
    }
}

function filtrar(q) {
    if (!q.trim()) { renderLista(todas); return; }
    const qL = q.toLowerCase();
    renderLista(todas.filter(f =>
        f.numeroF?.toLowerCase().includes(qL) ||
        f.cliente?.toLowerCase().includes(qL)));
}

function renderLista(lista) {
    const div = document.getElementById('lista');
    if (lista.length === 0) {
        div.innerHTML = `<div class="empty"><div class="empty-icon">▤</div><div class="empty-text">Sin facturas</div></div>`;
        return;
    }
    div.innerHTML = `<div class="card" style="padding:0">
        ${lista.map((f, ix) => `
        <button type="button" class="list-item js-fact" data-idx="${ix}"
             style="padding:12px 16px;cursor:pointer;background:none;border:none;
                    border-bottom:1px solid var(--borde);text-align:left;width:100%;color:inherit;font:inherit;">
            <div class="list-item-main">
                <div class="list-item-title">${esh(f.cliente || '(sin cliente)')}</div>
                <div class="list-item-sub">${esh(f.numeroF)} · ${formatFecha(f.fecha)}</div>
            </div>
            <div style="text-align:right;flex-shrink:0;margin-left:10px">
                <div class="mono">${formatUSD(f.total)}</div>
                <span class="tag ${f.pagada?'tag-verde':estaVencida(f.fecha)?'tag-rojo':'tag-naranja'}">
                    ${f.pagada?'Pagada':f.saldo>0?'Pendiente':''}
                </span>
            </div>
        </button>`).join('')}
    </div>`;
    div.querySelectorAll('.js-fact').forEach(btn => {
        btn.addEventListener('click', () => verDetalle(lista[+btn.dataset.idx].numeroF));
    });
}

async function verDetalle(numero) {
    const drawer = document.getElementById('drawerFact');
    drawer.innerHTML = `<div class="modal-handle"></div><div class="modal-title">${esh(numero)}</div><div class="spinner" role="status" aria-label="Cargando"></div>`;
    openModal('modalFact');
    try {
        const f = await apiGet(`/api/facturas/${encodeURIComponent(numero)}`);
        if (!f) return;
        drawer.innerHTML = `
            <div class="modal-handle"></div>
            <div class="flex-between mb-8">
                <div class="modal-title" style="margin-bottom:0">${esh(f.numeroF)}</div>
                <span class="tag ${f.pagada?'tag-verde':f.saldo>0?'tag-naranja':'tag-dim'}">
                    ${f.pagada?'Pagada':f.saldo>0?'Pendiente':''}
                </span>
            </div>
            <div class="text-dim text-sm mb-16">${esh(f.cliente)} · ${formatFecha(f.fecha)} · ${esh(f.formaDePago||'')}</div>
            <div class="card" style="margin-bottom:12px">
                ${f.lineas.map(l => `
                <div class="list-item">
                    <div class="list-item-main">
                        <div class="list-item-title">${esh(l.descripcion||l.codigoItem)}</div>
                        <div class="list-item-sub">${formatNum(l.cantidad,2)} × ${formatUSD(l.precioUnit)}</div>
                    </div>
                    <div class="list-item-value">${formatUSD(l.subtotal)}</div>
                </div>`).join('')}
            </div>
            <div class="flex-between mb-8">
                <span class="text-dim">IVA</span>
                <span class="mono">${formatUSD(f.iva)}</span>
            </div>
            <div class="flex-between bold">
                <span>Total</span>
                <span class="mono text-cyan">${formatUSD(f.total)}</span>
            </div>
            ${f.saldo > 0 ? `
            <div class="flex-between" style="margin-top:6px">
                <span class="text-dim">Saldo pendiente</span>
                <span class="mono" style="color:var(--naranja)">${formatUSD(f.saldo)}</span>
            </div>` : ''}
            <button class="btn btn-ghost btn-full" style="margin-top:16px" onclick="closeModal('modalFact')">Cerrar</button>`;
    } catch (err) {
        drawer.innerHTML = `<div class="modal-handle"></div>
            <div class="alert-banner" role="alert">Error: ${esh(err.message)}</div>
            <button class="btn btn-ghost btn-full" onclick="closeModal('modalFact')">Cerrar</button>`;
    }
}
render();
