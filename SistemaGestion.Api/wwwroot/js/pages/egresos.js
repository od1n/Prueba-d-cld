'use strict';
// egresos.js — Historial de egresos / compras
// SistemaGestion Portal

let categorias   = [];
let listaEgresos = [];

// ── Init ─────────────────────────────────────────────────────────────────────
async function init() {
    if (!Auth.requireAuth()) return;

    document.getElementById('app').innerHTML = buildShell();
    renderTopbar('Egresos / Compras');
    document.getElementById('app').insertAdjacentHTML('beforeend', renderNav('egresos'));

    await cargarCategorias();
    await cargar();

    // Eventos filtros
    document.getElementById('fDesde').addEventListener('change', cargar);
    document.getElementById('fHasta').addEventListener('change', cargar);
    document.getElementById('fCat').addEventListener('change',   cargar);
    document.getElementById('btnNuevo').addEventListener('click',
        () => location.href = '/pages/egresos-nuevo.html');
    document.getElementById('btnBuscar').addEventListener('click', cargar);
    document.getElementById('fBuscar').addEventListener('keydown', e => {
        if (e.key === 'Enter') cargar();
    });

    // Cerrar modal
    document.getElementById('modalOverlay').addEventListener('click', e => {
        if (e.target === document.getElementById('modalOverlay')) cerrarModal();
    });
    document.getElementById('btnCerrarModal').addEventListener('click', cerrarModal);

    // Cerrar modal completar precio
    document.getElementById('cpOverlay').addEventListener('click', e => {
        if (e.target === document.getElementById('cpOverlay')) cerrarCompletar();
    });
    document.getElementById('cpCerrar').addEventListener('click', cerrarCompletar);
}

// ── HTML base ─────────────────────────────────────────────────────────────────
function buildShell() {
    // Fechas por defecto: primer día del mes actual hasta hoy
    const hoy    = new Date();
    const desde  = `${hoy.getFullYear()}-${String(hoy.getMonth()+1).padStart(2,'0')}-01`;
    const hasta  = hoy.toISOString().slice(0,10);

    return `
    <div style="padding:1rem 1rem 5rem">
      <!-- Filtros -->
      <div class="el-filtros">
        <div>
          <label>Desde</label>
          <input type="date" id="fDesde" value="${desde}">
        </div>
        <div>
          <label>Hasta</label>
          <input type="date" id="fHasta" value="${hasta}">
        </div>
        <div>
          <label>Buscar</label>
          <input type="text" id="fBuscar" placeholder="Concepto, proveedor, documento…" style="min-width:200px">
        </div>
        <div>
          <label>Categoría</label>
          <select id="fCat">
            <option value="">Todas</option>
          </select>
        </div>
        <button class="btn btn-ghost" id="btnBuscar" style="align-self:flex-end">Buscar</button>
        <button class="el-btn-nuevo" id="btnNuevo">+ Nuevo egreso</button>
      </div>

      <!-- KPIs -->
      <div class="el-kpis" id="kpis">
        <div class="el-kpi"><div class="el-kpi-label">Registros</div><div class="el-kpi-val" id="kpiCount">—</div></div>
        <div class="el-kpi"><div class="el-kpi-label">Total USD</div><div class="el-kpi-val usd" id="kpiUSD">—</div></div>
        <div class="el-kpi"><div class="el-kpi-label">Total Bs</div><div class="el-kpi-val" id="kpiBs">—</div></div>
      </div>

      <!-- Egresos con precio pendiente (nota de entrega sin montos) -->
      <div id="pendientesWrap" style="margin-bottom:1rem"></div>

      <!-- Análisis por categoría -->
      <div class="el-tabla-wrap" style="margin-bottom:1rem;padding:1rem" id="analisisWrap">
        <div class="el-kpi-label" style="margin-bottom:.8rem">Gasto por categoría</div>
        <div id="analisisContenido" style="color:var(--texto-dim);font-size:.85rem">Cargando…</div>
      </div>

      <!-- Tabla -->
      <div class="el-tabla-wrap">
        <div id="tablaContenido" class="el-empty">
          <div class="spinner" style="margin:0 auto 10px"></div>
          Cargando…
        </div>
      </div>
    </div>

    <!-- Modal detalle -->
    <div class="el-modal-overlay" id="modalOverlay" style="display:none">
      <div class="el-modal">
        <div class="el-modal-header">
          <h3 id="modalTitulo">Detalle del egreso</h3>
          <button class="el-modal-close" id="btnCerrarModal">✕</button>
        </div>
        <div class="el-modal-body" id="modalBody">
          <div class="el-empty"><div class="spinner" style="margin:0 auto 8px"></div>Cargando…</div>
        </div>
      </div>
    </div>

    <!-- Modal completar precio -->
    <div class="el-modal-overlay" id="cpOverlay" style="display:none">
      <div class="el-modal">
        <div class="el-modal-header">
          <h3 id="cpTitulo">Completar precio</h3>
          <button class="el-modal-close" id="cpCerrar">✕</button>
        </div>
        <div class="el-modal-body" id="cpBody"></div>
      </div>
    </div>`;
}

// ── Categorías ────────────────────────────────────────────────────────────────
async function cargarCategorias() {
    try {
        const r = await apiGet('/api/categorias-gasto');
        categorias = r;
        const sel = document.getElementById('fCat');
        categorias.forEach(c => {
            const o = document.createElement('option');
            o.value = c.id;
            o.textContent = c.nombre;
            sel.appendChild(o);
        });
    } catch { /* silencioso */ }
}

// ── Cargar lista ──────────────────────────────────────────────────────────────
async function cargar() {
    const desde = document.getElementById('fDesde').value;
    const hasta = document.getElementById('fHasta').value;
    const cat   = document.getElementById('fCat').value;
    const q     = document.getElementById('fBuscar').value.trim();

    const contenedor = document.getElementById('tablaContenido');
    contenedor.innerHTML = '<div class="el-empty"><div class="spinner" style="margin:0 auto 8px"></div>Cargando…</div>';

    const params = new URLSearchParams();
    if (desde) params.set('desde', desde);
    if (hasta) params.set('hasta', hasta);
    if (cat)   params.set('categoriaId', cat);
    if (q)     params.set('q', q);

    try {
        const [lista] = await Promise.all([
            apiGet(`/api/egresos?${params}`),
            cargarAnalisis(desde, hasta),
            cargarPendientes()
        ]);
        listaEgresos = lista;
        renderTabla();
        actualizarKPIs();
    } catch (ex) {
        contenedor.innerHTML = `<div class="el-empty" style="color:var(--rojo)">⚠ Error al cargar: ${esc(ex.message)}</div>`;
    }
}

// ── Render tabla ──────────────────────────────────────────────────────────────
function renderTabla() {
    const cont = document.getElementById('tablaContenido');

    if (listaEgresos.length === 0) {
        cont.innerHTML = '<div class="el-empty">No hay egresos en el período seleccionado.</div>';
        return;
    }

    const filas = listaEgresos.map(e => {
        const catTag  = e.categoria ? `<span class="el-cat-tag">${esc(e.categoria)}</span>` : '—';
        const nroDoc  = e.nroDocumento ? `<br><small style="color:var(--texto-dim)">${esc(e.nroDocumento)}</small>` : '';
        const montoUSD = e.montoUSD > 0 ? `$${fmt2(e.montoUSD)}` : '—';
        const montoBs  = e.montoBs  > 0 ? `<div class="el-monto-bs">Bs ${fmt2(e.montoBs)}</div>` : '';

        return `<tr>
          <td style="color:var(--texto-dim);font-size:.82rem;white-space:nowrap">${e.fecha}</td>
          <td>
            <div class="el-td-concepto" title="${esc(e.concepto)}">${esc(e.concepto)}</div>
            ${e.proveedor ? `<div class="el-td-proveedor">${esc(e.proveedor)}${nroDoc}</div>` : nroDoc}
          </td>
          <td>${catTag}</td>
          <td class="el-monto">${montoUSD}${montoBs}</td>
          <td>
            <button class="el-btn-det" data-id="${e.id}">Ver →</button>
          </td>
        </tr>`;
    }).join('');

    cont.innerHTML = `
      <table class="el-tabla">
        <thead>
          <tr>
            <th>Fecha</th>
            <th>Concepto / Proveedor</th>
            <th>Categoría</th>
            <th style="text-align:right">Monto</th>
            <th></th>
          </tr>
        </thead>
        <tbody>${filas}</tbody>
      </table>`;

    cont.querySelectorAll('.el-btn-det').forEach(btn =>
        btn.addEventListener('click', () => abrirDetalle(parseInt(btn.dataset.id)))
    );
}

// ── Análisis por categoría ────────────────────────────────────────────────────
async function cargarAnalisis(desde, hasta) {
    const cont = document.getElementById('analisisContenido');
    try {
        const params = new URLSearchParams();
        if (desde) params.set('desde', desde);
        if (hasta) params.set('hasta', hasta);
        const data = await apiGet(`/api/egresos/analisis?${params}`);

        if (!data || data.length === 0) {
            cont.innerHTML = '<span style="color:var(--texto-dim)">Sin datos en el período.</span>';
            return;
        }

        const maxUSD = Math.max(...data.map(d => d.totalUSD), 1);
        const COLORES = ['#2196f3','#43a047','#fb8c00','#9c27b0','#e53935','#00acc1','#f4511e'];

        cont.innerHTML = data.map((d, i) => {
            const pct  = Math.round((d.totalUSD / maxUSD) * 100);
            const col  = d.color || COLORES[i % COLORES.length];
            return `
            <div style="margin-bottom:.55rem">
              <div style="display:flex;justify-content:space-between;font-size:.82rem;margin-bottom:.2rem">
                <span style="font-weight:600;color:var(--texto)">${esc(d.categoria)}</span>
                <span style="color:var(--texto-dim)">$${fmt2(d.totalUSD)} · ${d.cantidad} egreso(s)</span>
              </div>
              <div style="background:var(--borde);border-radius:4px;height:10px;overflow:hidden">
                <div style="background:${col};width:${pct}%;height:100%;border-radius:4px;transition:width .4s"></div>
              </div>
            </div>`;
        }).join('');
    } catch {
        cont.innerHTML = '<span style="color:var(--texto-dim)">Error al cargar análisis.</span>';
    }
}

// ── KPIs ──────────────────────────────────────────────────────────────────────
function actualizarKPIs() {
    const totalUSD = listaEgresos.reduce((s, e) => s + (e.montoUSD || 0), 0);
    const totalBs  = listaEgresos.reduce((s, e) => s + (e.montoBs  || 0), 0);
    document.getElementById('kpiCount').textContent = listaEgresos.length;
    document.getElementById('kpiUSD').textContent   = `$${fmt2(totalUSD)}`;
    document.getElementById('kpiBs').textContent    = `Bs ${fmt2(totalBs)}`;
}

// ── Detalle modal ─────────────────────────────────────────────────────────────
async function abrirDetalle(id) {
    document.getElementById('modalOverlay').style.display = 'flex';
    document.getElementById('modalBody').innerHTML =
        '<div class="el-empty"><div class="spinner" style="margin:0 auto 8px"></div>Cargando…</div>';

    try {
        const d = await apiGet(`/api/egresos/${id}`);
        document.getElementById('modalTitulo').textContent = `Egreso #${d.id}`;
        document.getElementById('modalBody').innerHTML = buildDetalle(d);
    } catch (ex) {
        document.getElementById('modalBody').innerHTML =
            `<div class="el-empty" style="color:var(--rojo)">⚠ ${esc(ex.message)}</div>`;
    }
}

function buildDetalle(d) {
    const cat   = d.categoria || '—';
    const notas = d.notas     || '—';

    const lineas = (d.detalles || []).map(l => {
        const tipoTag = l.tipoInsumo
            ? `<span style="font-size:.7rem;background:#1a3a5c;color:var(--cyan);border-radius:3px;padding:1px 4px">${esc(l.tipoInsumo)}</span>`
            : '';
        return `<tr>
          <td>${esc(l.descripcion)} ${tipoTag}</td>
          <td style="text-align:right">${fmt2(l.cantidad)}</td>
          <td style="text-align:right">$${fmt2(l.precioUnitario)}</td>
          <td style="text-align:right">$${fmt2(l.subtotal)}</td>
        </tr>`;
    }).join('');

    return `
      <div class="el-det-grid">
        <div class="el-det-field"><label>Fecha</label><span>${d.fecha}</span></div>
        <div class="el-det-field"><label>Categoría</label><span>${esc(cat)}</span></div>
        <div class="el-det-field"><label>Proveedor</label><span>${esc(d.proveedor || '—')}</span></div>
        <div class="el-det-field"><label>N° Documento</label><span>${esc(d.nroDocumento || '—')}</span></div>
        <div class="el-det-field"><label>Total USD</label><span style="color:var(--cyan);font-weight:700">$${fmt2(d.montoUSD)}</span></div>
        <div class="el-det-field"><label>Total Bs</label><span>${d.montoBs > 0 ? 'Bs ' + fmt2(d.montoBs) : '—'}</span></div>
        ${d.tasaBCV > 0 ? `<div class="el-det-field"><label>Tasa BCV</label><span>${fmt2(d.tasaBCV)}</span></div>` : ''}
        <div class="el-det-field" style="grid-column:1/-1"><label>Notas</label><span>${esc(notas)}</span></div>
      </div>

      ${lineas ? `
      <div style="font-size:.78rem;font-weight:700;color:var(--texto-dim);text-transform:uppercase;
                  letter-spacing:.04em;margin-bottom:.4rem">Líneas del documento</div>
      <table class="el-det-lineas">
        <thead><tr>
          <th>Descripción</th>
          <th style="text-align:right">Cant.</th>
          <th style="text-align:right">P. Unit.</th>
          <th style="text-align:right">Subtotal</th>
        </tr></thead>
        <tbody>${lineas}</tbody>
      </table>
      <div class="el-det-total">
        <span>Total USD: <span style="color:var(--cyan)">$${fmt2(d.montoUSD)}</span></span>
        ${d.montoBs > 0 ? `<span>Total Bs: Bs ${fmt2(d.montoBs)}</span>` : ''}
      </div>` : ''}`;
}

function cerrarModal() {
    document.getElementById('modalOverlay').style.display = 'none';
}

// ── Precio pendiente (nota de entrega sin montos) ──────────────────────────────
async function cargarPendientes() {
    const wrap = document.getElementById('pendientesWrap');
    if (!wrap) return;
    try {
        const lista = await apiGet('/api/egresos/precio-pendiente');
        if (!lista || lista.length === 0) { wrap.innerHTML = ''; return; }
        const filas = lista.map(e => `
            <div style="display:flex;align-items:center;justify-content:space-between;gap:.6rem;
                        padding:.5rem .7rem;border-top:1px solid rgba(0,0,0,.06)">
              <div style="min-width:0">
                <div style="font-weight:600">${esc(e.concepto)}</div>
                <div style="font-size:.8rem;color:var(--texto-dim)">
                  ${e.fecha} · ${esc(e.proveedor || 'sin proveedor')}${e.cantidad > 0 ? ' · Cant: ' + fmt2(e.cantidad) : ''}
                </div>
              </div>
              <button class="btn btn-ghost cp-btn" data-id="${e.id}">Completar precio →</button>
            </div>`).join('');
        wrap.innerHTML = `
          <div class="el-tabla-wrap" style="padding:.7rem .9rem;border-left:4px solid #d29922">
            <div style="font-weight:700;color:#d29922">⏳ ${lista.length} egreso(s) con precio pendiente</div>
            <div style="font-size:.82rem;color:var(--texto-dim);margin-bottom:.3rem">
              Notas de entrega registradas sin montos. Completá el precio cuando llegue la factura.
            </div>
            ${filas}
          </div>`;
        wrap.querySelectorAll('.cp-btn').forEach(b =>
            b.addEventListener('click', () => abrirCompletar(parseInt(b.dataset.id))));
    } catch { wrap.innerHTML = ''; }
}

async function abrirCompletar(id) {
    const ov = document.getElementById('cpOverlay');
    const body = document.getElementById('cpBody');
    document.getElementById('cpTitulo').textContent = `Completar precio — Egreso #${id}`;
    ov.style.display = 'flex';
    body.innerHTML = '<div class="el-empty"><div class="spinner" style="margin:0 auto 8px"></div>Cargando…</div>';

    let d;
    try { d = await apiGet(`/api/egresos/${id}`); }
    catch (ex) { body.innerHTML = `<div class="el-empty" style="color:var(--rojo)">⚠ ${esc(ex.message)}</div>`; return; }

    const lineas = (d.detalles || []).map((l, i) => `
        <tr>
          <td>${esc(l.descripcion)}</td>
          <td style="text-align:right">${fmt2(l.cantidad)}</td>
          <td style="text-align:right">
            <input type="number" min="0" step="0.0001" class="cp-pu" data-i="${i}"
                   data-art="${l.idArticulo || ''}" data-desc="${esc(l.descripcion)}"
                   data-cant="${l.cantidad}" value="${l.precioUnitario || 0}"
                   style="width:110px;text-align:right">
          </td>
          <td style="text-align:right" class="cp-sub" data-i="${i}">$0.00</td>
        </tr>`).join('');

    body.innerHTML = `
      <p style="font-size:.85rem;color:var(--texto-dim)">
        Ingresá el precio unitario en USD de cada línea. El costo del Kardex se ajusta con estos valores.
      </p>
      <table class="el-det-lineas">
        <thead><tr>
          <th>Descripción</th><th style="text-align:right">Cant.</th>
          <th style="text-align:right">P. Unit (USD)</th><th style="text-align:right">Subtotal</th>
        </tr></thead>
        <tbody>${lineas || '<tr><td colspan="4" style="color:var(--texto-dim)">Sin líneas de detalle.</td></tr>'}</tbody>
      </table>

      <div class="el-det-grid" style="margin-top:.8rem">
        <div class="el-det-field"><label>Base (USD)</label><span id="cpBase">$0.00</span></div>
        <div class="el-det-field"><label>IVA %</label><input type="number" id="cpIva" min="0" step="0.01" value="16"></div>
        <div class="el-det-field"><label>Total (USD)</label><span id="cpTotal" style="color:var(--cyan);font-weight:700">$0.00</span></div>
        <div class="el-det-field"><label>Tasa BCV (opcional)</label><input type="number" id="cpTasa" min="0" step="0.01" value="${d.tasaBCV || 0}"></div>
      </div>

      <div class="el-det-field" style="margin-top:.4rem">
        <label style="display:flex;align-items:center;gap:.5rem;cursor:pointer">
          <input type="checkbox" id="cpCredito" style="width:auto"> A crédito (abono parcial)
        </label>
      </div>
      <div class="el-det-grid" id="cpZonaCredito" style="display:none">
        <div class="el-det-field"><label>Abonado (USD)</label><input type="number" id="cpAbonado" min="0" step="0.01" value="0"></div>
        <div class="el-det-field"><label>Vence</label><input type="date" id="cpVence"></div>
      </div>

      <div id="cpError" style="color:var(--rojo);font-size:.85rem;margin-top:.5rem;display:none"></div>
      <div style="display:flex;gap:.6rem;justify-content:flex-end;margin-top:1rem">
        <button class="btn btn-ghost" id="cpCancelar">Cancelar</button>
        <button class="btn" id="cpGuardar" data-id="${id}">Completar</button>
      </div>`;

    const recalc = () => {
        let base = 0;
        body.querySelectorAll('.cp-pu').forEach(inp => {
            const cant = parseFloat(inp.dataset.cant) || 0;
            const pu = parseFloat(inp.value) || 0;
            const sub = cant * pu;
            base += sub;
            body.querySelector(`.cp-sub[data-i="${inp.dataset.i}"]`).textContent = '$' + fmt2(sub);
        });
        const iva = parseFloat(document.getElementById('cpIva').value) || 0;
        const total = base * (1 + iva / 100);
        document.getElementById('cpBase').textContent = '$' + fmt2(base);
        document.getElementById('cpTotal').textContent = '$' + fmt2(total);
    };
    body.querySelectorAll('.cp-pu').forEach(inp => inp.addEventListener('input', recalc));
    document.getElementById('cpIva').addEventListener('input', recalc);
    document.getElementById('cpCredito').addEventListener('change', e =>
        document.getElementById('cpZonaCredito').style.display = e.target.checked ? 'grid' : 'none');
    document.getElementById('cpCancelar').addEventListener('click', cerrarCompletar);
    document.getElementById('cpGuardar').addEventListener('click', () => guardarCompletar(id, body));
    recalc();
}

async function guardarCompletar(id, body) {
    const err = document.getElementById('cpError');
    err.style.display = 'none';

    const detalles = [];
    let base = 0;
    body.querySelectorAll('.cp-pu').forEach(inp => {
        const cant = parseFloat(inp.dataset.cant) || 0;
        const pu = parseFloat(inp.value) || 0;
        base += cant * pu;
        detalles.push({
            idArticulo: inp.dataset.art ? parseInt(inp.dataset.art) : null,
            descripcion: inp.dataset.desc,
            cantidad: cant,
            precioUnitario: pu,
            subtotal: cant * pu
        });
    });

    if (base <= 0) { err.textContent = 'Ingresá el precio de al menos una línea.'; err.style.display = 'block'; return; }

    const iva = parseFloat(document.getElementById('cpIva').value) || 0;
    const totalUSD = base * (1 + iva / 100);
    const tasa = parseFloat(document.getElementById('cpTasa').value) || 0;
    const aCredito = document.getElementById('cpCredito').checked;
    const abonado = aCredito ? (parseFloat(document.getElementById('cpAbonado').value) || 0) : 0;
    const vence = document.getElementById('cpVence') ? document.getElementById('cpVence').value : '';

    const payload = {
        montoUSD: totalUSD,
        montoBs: tasa > 0 ? totalUSD * tasa : 0,
        tasaBCV: tasa,
        aCredito: aCredito,
        pagadoUSD: abonado,
        fechaVencimiento: aCredito && vence ? vence : null,
        detalles: detalles
    };

    const btn = document.getElementById('cpGuardar');
    btn.disabled = true; btn.textContent = 'Guardando…';
    try {
        const resp = await fetch(`/api/egresos/${id}/completar-precio`, {
            method: 'PUT',
            headers: { 'Content-Type': 'application/json' },
            credentials: 'include',
            body: JSON.stringify(payload)
        });
        if (!resp.ok) {
            const e = await resp.json().catch(() => ({}));
            throw new Error(e.error || e.detail || `Error ${resp.status}`);
        }
        cerrarCompletar();
        cargar();
    } catch (ex) {
        err.textContent = ex.message; err.style.display = 'block';
        btn.disabled = false; btn.textContent = 'Completar';
    }
}

function cerrarCompletar() {
    document.getElementById('cpOverlay').style.display = 'none';
}

// ── Helpers ───────────────────────────────────────────────────────────────────
function fmt2(n) {
    return (parseFloat(n) || 0).toLocaleString('es-VE',
        { minimumFractionDigits: 2, maximumFractionDigits: 2 });
}
function esc(str) {
    if (!str) return '';
    return String(str)
        .replace(/&/g,'&amp;').replace(/</g,'&lt;')
        .replace(/>/g,'&gt;').replace(/"/g,'&quot;');
}

init();
