// kardex.js
if (!Auth.requireAuth()) {}

const app   = document.getElementById('app');
const user  = Auth.getUser();
const puedeEscribir = user?.puedeEscribirKardex ?? false;
const puedeAjustar  = user?.puedeAjustarKardex  ?? false;

let kardexTipo  = 'B';
let itemSelec   = null;
let listaItems  = [];

async function render() {
    const me = await apiGet('/api/me').catch(() => null);
    const puedoEscribir = me?.puedeEscribirKardex ?? false;
    const puedoAjustar  = me?.puedeAjustarKardex  ?? false;

    app.innerHTML = `
        ${renderTopbar('Kardex')}
        <div class="content">
            <div class="tab-selector">
                <button class="tab-btn ${kardexTipo==='B'?'active':''}" onclick="cambiarKardex('B')">
                    📦 Productos
                </button>
                <button class="tab-btn ${kardexTipo==='A'?'active':''}" onclick="cambiarKardex('A')">
                    🧪 Insumos
                </button>
            </div>

            <div class="search-wrap">
                <span class="search-icon">⌕</span>
                <input class="input-field" id="searchItem" type="search"
                    placeholder="${kardexTipo==='B' ? 'Buscar producto...' : 'Buscar insumo...'}"
                    autocomplete="off" oninput="buscarItem(this.value)">
            </div>

            <div id="resultadosBusqueda" class="card hidden"></div>

            <div id="itemSeleccionado" class="hidden">
                <div class="card" id="cardItem"></div>
                <div class="card">
                    <div class="card-title" id="tituloMovs">Movimientos recientes</div>
                    <div id="listaMovs"><div class="spinner"></div></div>
                </div>
                ${puedoEscribir ? `
                <div class="btn-group" id="botonesAccion">
                    <button class="btn btn-verde" onclick="abrirFormMovimiento('ENTRADA')">▲ Entrada</button>
                    <button class="btn btn-rojo" onclick="abrirFormMovimiento('SALIDA')">▼ Salida</button>
                    ${puedoAjustar ? `<button class="btn btn-ghost" onclick="abrirFormMovimiento('AJUSTE')">⚖ Ajuste</button>` : ''}
                </div>` : ''}
            </div>

            <div id="emptyState" class="empty ${itemSelec ? 'hidden' : ''}">
                <div class="empty-icon">⇅</div>
                <div class="empty-text">Busca un ${kardexTipo==='B'?'producto':'insumo'}<br>para ver sus movimientos</div>
            </div>
        </div>
        ${renderNav('kardex')}

        <div class="modal-overlay" id="modalMov">
            <div class="modal-drawer">
                <div class="modal-handle"></div>
                <div class="modal-title" id="modalMovTitulo">Registrar movimiento</div>
                <div class="msg" id="msgMov"></div>
                <div id="formMovContenido"></div>
            </div>
        </div>`;

    if (itemSelec) await seleccionarItem(itemSelec);
}

function cambiarKardex(tipo) {
    kardexTipo = tipo;
    itemSelec  = null;
    listaItems = [];
    render();
}

let buscarTimer = null;
async function buscarItem(q) {
    clearTimeout(buscarTimer);
    if (q.length < 1) {
        document.getElementById('resultadosBusqueda')?.classList.add('hidden');
        return;
    }
    buscarTimer = setTimeout(async () => {
        try {
            const endpoint = kardexTipo === 'B'
                ? `/api/inventario?q=${encodeURIComponent(q)}`
                : `/api/insumos?q=${encodeURIComponent(q)}`;
            listaItems = await apiGet(endpoint) || [];
            renderResultados(listaItems.slice(0, 8));
        } catch {}
    }, 300);
}

function renderResultados(items) {
    const div = document.getElementById('resultadosBusqueda');
    if (!div) return;
    if (items.length === 0) {
        div.innerHTML = '<div class="text-dim text-sm" style="padding:8px">Sin resultados</div>';
    } else {
        div.innerHTML = items.map((i, ix) => `
            <button type="button" class="list-item js-pick" data-idx="${ix}"
                    style="cursor:pointer;background:none;border:none;border-bottom:1px solid var(--borde);
                           text-align:left;width:100%;color:inherit;font:inherit;padding:10px 12px;">
                <div class="list-item-main">
                    <div class="list-item-title">${esh(i.descripcion)}</div>
                    <div class="list-item-sub">${esh(kardexTipo==='B' ? i.codigo : i.codigoInterno)}</div>
                </div>
                <div class="list-item-value ${i.bajMin ? 'rojo' : ''}">${formatNum(i.stock, 2)}</div>
            </button>`).join('');
        div.querySelectorAll('.js-pick').forEach(btn => {
            btn.addEventListener('click', () => seleccionarItem(items[+btn.dataset.idx]));
        });
    }
    div.classList.remove('hidden');
}

async function seleccionarItem(item) {
    itemSelec = item;
    document.getElementById('resultadosBusqueda')?.classList.add('hidden');
    document.getElementById('emptyState')?.classList.add('hidden');
    document.getElementById('itemSeleccionado')?.classList.remove('hidden');

    const es = item.bajMin ? '<span class="tag tag-rojo">BAJO MÍN</span>' : '<span class="tag tag-verde">OK</span>';
    const cardItem = document.getElementById('cardItem');
    if (cardItem) {
        cardItem.innerHTML = `
            <div class="flex-between mb-8">
                <div>
                    <div class="bold">${esh(item.descripcion)}</div>
                    <div class="text-dim text-sm">${esh(kardexTipo==='B'?item.codigo:item.codigoInterno)} · ${esh(item.unidad)}</div>
                </div>
                ${es}
            </div>
            <div class="kpi-grid" style="gap:8px">
                <div class="kpi">
                    <div class="kpi-label">Stock actual</div>
                    <div class="kpi-value ${item.bajMin?'rojo':''}">${formatNum(item.stock, 2)}</div>
                </div>
                ${kardexTipo==='B' ? `
                <div class="kpi">
                    <div class="kpi-label">Precio</div>
                    <div class="kpi-value text-sm" style="font-size:16px">${formatUSD(item.precioActual)}</div>
                </div>` : `
                <div class="kpi">
                    <div class="kpi-label">CPP/kg</div>
                    <div class="kpi-value text-sm" style="font-size:16px">${formatUSD(item.costoPromedio)}</div>
                </div>`}
            </div>`;
    }

    await cargarMovimientos(item);
}

async function cargarMovimientos(item) {
    const div = document.getElementById('listaMovs');
    if (!div) return;
    div.innerHTML = '<div class="spinner"></div>';
    try {
        const codigo = kardexTipo === 'B' ? item.codigo : item.codigoInterno;
        const endpoint = kardexTipo === 'B'
            ? `/api/kardex/b?codigo=${encodeURIComponent(codigo)}`
            : `/api/kardex/a?codigo=${encodeURIComponent(codigo)}`;
        const movs = await apiGet(endpoint) || [];

        if (movs.length === 0) {
            div.innerHTML = '<div class="empty-text text-dim" style="padding:12px">Sin movimientos registrados</div>';
            return;
        }

        div.innerHTML = movs.slice(0, 15).map(m => `
            <div class="list-item">
                <div class="list-item-main">
                    ${tagTipo(m.tipo)}
                    <div class="list-item-sub" style="margin-top:4px">${esh(m.concepto || '—')}</div>
                    <div class="text-dim" style="font-size:11px">${esh(m.fecha)} · ${esh(m.usuario || '')}</div>
                </div>
                <div style="text-align:right;flex-shrink:0;margin-left:12px">
                    <div class="mono ${m.tipo==='ENTRADA'?'text-cyan':m.tipo==='SALIDA'?'rojo':''}"
                         style="${m.tipo==='SALIDA'?'color:var(--rojo)':''}">
                        ${m.tipo==='SALIDA'?'−':'+'}${formatNum(m.cantidad, 2)}
                    </div>
                    <div class="text-dim text-sm">→ ${formatNum(m.saldo, 2)}</div>
                </div>
            </div>`).join('');
    } catch (err) {
        div.innerHTML = `<div class="text-dim text-sm">Error: ${esh(err.message)}</div>`;
    }
}

function abrirFormMovimiento(tipo) {
    const titulo = tipo === 'ENTRADA' ? '▲ Registrar Entrada'
                 : tipo === 'SALIDA'  ? '▼ Registrar Salida'
                 : '⚖ Registrar Ajuste';

    document.getElementById('modalMovTitulo').textContent = titulo;

    const item   = itemSelec;
    const esB    = kardexTipo === 'B';
    const codigo = esB ? item.codigo : item.codigoInterno;
    const idIns  = item.id || 0;

    const needCosto = tipo === 'ENTRADA';
    const labelCantidad = esB ? 'Cantidad (unidades)' : 'Cantidad (kg)';
    const labelAjuste   = esB ? 'Nuevo stock real (unidades)' : 'Nuevo stock real (kg)';

    document.getElementById('formMovContenido').innerHTML = `
        <div class="card-title text-dim mb-8">${esh(item.descripcion)}</div>

        <div class="input-group">
            <label class="input-label">${tipo === 'AJUSTE' ? labelAjuste : labelCantidad}</label>
            <input class="input-field" id="movCantidad" type="number" min="0.01" step="0.01"
                placeholder="0.00" inputmode="decimal">
        </div>

        ${needCosto ? `
        <div class="input-group">
            <label class="input-label">${esB ? 'Costo unitario ($)' : 'Costo por kg ($)'}</label>
            <input class="input-field" id="movCosto" type="number" min="0" step="0.01"
                placeholder="0.00" inputmode="decimal">
        </div>` : ''}

        <div class="input-group">
            <label class="input-label">Concepto</label>
            <input class="input-field" id="movConcepto" type="text"
                placeholder="${tipo === 'AJUSTE' ? 'Motivo del ajuste' : 'Descripción del movimiento'}">
        </div>

        <div class="input-group">
            <label class="input-label">Referencia (opcional)</label>
            <input class="input-field" id="movReferencia" type="text"
                placeholder="N° factura, orden, etc.">
        </div>

        <div class="msg" id="msgMov"></div>

        <div class="btn-group" style="margin-top:8px">
            <button class="btn btn-ghost" onclick="closeModal('modalMov')">Cancelar</button>
            <button class="btn btn-cyan" id="btnGuardarMov" type="button">Registrar</button>
        </div>`;

    document.getElementById('btnGuardarMov').addEventListener('click', () => {
        guardarMovimiento(tipo, codigo, idIns, esB);
    });

    document.getElementById('movCantidad')?.focus();
    openModal('modalMov');
}

async function guardarMovimiento(tipo, codigo, idInsumo, esB) {
    const cantInput  = document.getElementById('movCantidad');
    const costoInput = document.getElementById('movCosto');
    const concepto   = document.getElementById('movConcepto')?.value?.trim() || '';
    const referencia = document.getElementById('movReferencia')?.value?.trim() || '';
    const btn        = document.getElementById('btnGuardarMov');

    const cantidad = parseFloat(cantInput?.value);
    const costo    = parseFloat(costoInput?.value || '0') || 0;

    if (!cantidad || cantidad <= 0) {
        showMsg('La cantidad debe ser mayor que cero.', 'error', '#msgMov');
        return;
    }

    setLoading(btn, true);
    hideMsg('#msgMov');

    try {
        const endpoint = esB ? '/api/kardex/b/movimiento' : '/api/kardex/a/movimiento';
        const body = esB
            ? { codigo, tipo, cantidad, concepto, referencia, costoUnitario: costo }
            : { idInsumo, tipo, cantidad, concepto, referencia, costoUnitario: costo };

        const res = await apiPost(endpoint, body);
        if (!res) return;

        closeModal('modalMov');

        if (res.nuevoSaldo !== undefined) {
            itemSelec.stock = res.nuevoSaldo;
            if (res.nuevoCpp) itemSelec.costoPromedio = res.nuevoCpp;
        }

        await seleccionarItem(itemSelec);
        showMsg(res.mensaje || 'Movimiento registrado.', 'ok', '.msg');

    } catch (err) {
        showMsg(err.message, 'error', '#msgMov');
    } finally {
        setLoading(btn, false);
    }
}

function tagTipo(tipo) {
    if (tipo === 'ENTRADA') return '<span class="tag tag-verde">ENTRADA</span>';
    if (tipo === 'SALIDA')  return '<span class="tag tag-rojo">SALIDA</span>';
    if (tipo === 'AJUSTE')  return '<span class="tag tag-naranja">AJUSTE</span>';
    return '<span class="tag tag-dim">' + esh(tipo) + '</span>';
}

render();
