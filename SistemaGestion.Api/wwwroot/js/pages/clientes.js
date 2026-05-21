// clientes.js
if (!Auth.requireAuth()) {}
const app = document.getElementById('app');

async function render() {
    app.innerHTML = `
        ${renderTopbar('Clientes / CxC')}
        <div class="content">
            <div class="search-wrap">
                <span class="search-icon">⌕</span>
                <input class="input-field" id="buscar" type="search"
                    placeholder="Buscar por nombre, RIF, código..."
                    autocomplete="off" oninput="buscar(this.value)">
            </div>
            <div id="lista"><div class="empty"><div class="empty-icon">◎</div>
                <div class="empty-text">Busca un cliente para ver su CxC</div></div>
            </div>
        </div>
        <div class="modal-overlay" id="modalCxc">
            <div class="modal-drawer" id="drawerCxc"></div>
        </div>
        ${renderNav('clientes')}`;
}

let timer = null;
async function buscar(q) {
    clearTimeout(timer);
    if (q.length < 1) {
        document.getElementById('lista').innerHTML =
            `<div class="empty"><div class="empty-icon">◎</div><div class="empty-text">Escribe para buscar</div></div>`;
        return;
    }
    timer = setTimeout(async () => {
        try {
            const lista = await apiGet(`/api/clientes?q=${encodeURIComponent(q)}`) || [];
            const div = document.getElementById('lista');
            if (lista.length === 0) {
                div.innerHTML = `<div class="empty"><div class="empty-text">Sin resultados</div></div>`;
                return;
            }
            div.innerHTML = `<div class="card" style="padding:0">
                ${lista.map((c, ix) => `
                <button type="button" class="list-item js-cliente"
                     data-idx="${ix}"
                     style="padding:12px 16px;cursor:pointer;background:none;border:none;
                            border-bottom:1px solid var(--borde);text-align:left;width:100%;color:inherit;font:inherit;">
                    <div class="list-item-main">
                        <div class="list-item-title">${esh(c.nombre)}</div>
                        <div class="list-item-sub">${esh(c.ciRif||'')} · ${esh(c.ciudad||'')}</div>
                    </div>
                    <span style="color:var(--cyan);font-size:20px" aria-hidden="true">›</span>
                </button>`).join('')}
            </div>`;
            div.querySelectorAll('.js-cliente').forEach(btn => {
                btn.addEventListener('click', () => {
                    const c = lista[+btn.dataset.idx];
                    verCxc(c.codigo, c.nombre);
                });
            });
        } catch (err) {
            document.getElementById('lista').innerHTML =
                `<div class="alert-banner">Error: ${esh(err.message)}</div>`;
        }
    }, 400);
}

async function verCxc(codigo, nombre) {
    const drawer = document.getElementById('drawerCxc');
    const nomEsc = esh(nombre);
    drawer.innerHTML = `<div class="modal-handle"></div><div class="modal-title">${nomEsc}</div><div class="spinner" role="status" aria-label="Cargando"></div>`;
    openModal('modalCxc');
    try {
        const data = await apiGet(`/api/clientes/${encodeURIComponent(codigo)}/cxc`);
        if (!data) return;
        const hayVencido = data.vencido > 0;
        drawer.innerHTML = `
            <div class="modal-handle"></div>
            <div class="modal-title">${nomEsc}</div>
            <div class="kpi-grid" style="gap:8px;margin-bottom:14px">
                <div class="kpi">
                    <div class="kpi-label">Saldo total</div>
                    <div class="kpi-value" style="font-size:18px">${formatUSD(data.saldoTotal)}</div>
                </div>
                <div class="kpi">
                    <div class="kpi-label">Vencido</div>
                    <div class="kpi-value" style="font-size:18px;${hayVencido?'color:var(--rojo)':''}">${formatUSD(data.vencido)}</div>
                </div>
            </div>
            ${data.cuentas.length === 0
                ? '<div class="text-dim text-sm" style="text-align:center;padding:16px">Sin cuentas pendientes ✓</div>'
                : data.cuentas.map(c => `
                <div class="list-item">
                    <div class="list-item-main">
                        <div class="list-item-title">${esh(c.concepto)}</div>
                        <div class="list-item-sub">Vence ${formatFecha(c.vence)}</div>
                    </div>
                    <div style="text-align:right">
                        <div class="mono ${c.estaVencida?'':'text-cyan'}" style="${c.estaVencida?'color:var(--rojo)':''}">
                            ${formatUSD(c.saldo)}
                        </div>
                        <span class="tag ${c.estado==='Pagada'?'tag-verde':c.estaVencida?'tag-rojo':'tag-naranja'}">
                            ${esh(c.estado)}
                        </span>
                    </div>
                </div>`).join('')}
            <button class="btn btn-ghost btn-full" style="margin-top:16px" onclick="closeModal('modalCxc')">Cerrar</button>`;
    } catch (err) {
        drawer.innerHTML = `<div class="modal-handle"></div>
            <div class="alert-banner">Error: ${esh(err.message)}</div>
            <button class="btn btn-ghost btn-full" onclick="closeModal('modalCxc')">Cerrar</button>`;
    }
}
render();
