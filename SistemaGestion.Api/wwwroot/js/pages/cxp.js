// cxp.js — Portal: Cuentas por Pagar (deudas con proveedores) y registro de pagos
if (!Auth.requireAuth()) {}
const app = document.getElementById('app');

let cuentas = [];
let cuentaActual = null;
let tBusqueda = null;

const fmt = n => '$ ' + Number(n || 0).toLocaleString('es-VE', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
const fFecha = f => { if (!f || f.length < 10) return f || '—'; const [y, m, d] = f.substring(0, 10).split('-'); return `${d}/${m}/${y}`; };
const hoyISO = () => { const d = new Date(); d.setMinutes(d.getMinutes() - d.getTimezoneOffset()); return d.toISOString().split('T')[0]; };

function badgeEstado(c) {
    if (c.estado === 'Pagada') return '<span class="badge badge-ok">Pagada</span>';
    if (c.vencida) return '<span class="badge badge-err">Vencida</span>';
    if (c.estado === 'Parcial') return '<span class="badge badge-warn">Parcial</span>';
    return '<span class="badge badge-warn">Pendiente</span>';
}

async function render() {
    app.innerHTML = `
    ${renderTopbar('Cuentas por Pagar')}
    <div class="content">
      <div class="form-row" style="padding:0 16px 10px;gap:8px;flex-wrap:wrap">
        <select id="filtEstado" class="input-field" style="width:170px" onchange="cargarCuentas()">
          <option value="Abiertas">Por pagar (abiertas)</option>
          <option value="Vencida">Vencidas</option>
          <option value="Pendiente">Pendientes</option>
          <option value="Parcial">Con pago parcial</option>
          <option value="Pagada">Pagadas</option>
          <option value="Todas">Todas</option>
        </select>
        <input type="search" id="filtTexto" class="input-field" style="flex:1;min-width:160px"
               placeholder="Buscar proveedor, concepto o factura" oninput="buscarDiferido()">
      </div>
      <div class="msg" style="margin:0 16px"></div>
      <div id="totalBar" class="stat-bar" style="padding:4px 16px 8px;font-weight:600;color:var(--primary)"></div>
      <div id="listaCxp"><div class="loading-msg"><div class="spinner"></div></div></div>
    </div>

    <div class="modal-overlay" id="modalDetalle">
      <div class="modal-drawer" id="drawerDetalle" style="max-height:95vh;overflow-y:auto"></div>
    </div>
    <div class="modal-overlay" id="modalPago">
      <div class="modal-drawer" id="drawerPago" style="max-height:95vh;overflow-y:auto"></div>
    </div>
    ${renderNav('cxp')}`;
    await cargarCuentas();
}

function buscarDiferido() {
    clearTimeout(tBusqueda);
    tBusqueda = setTimeout(cargarCuentas, 350);
}

async function cargarCuentas() {
    const estado = document.getElementById('filtEstado')?.value || 'Abiertas';
    const q = document.getElementById('filtTexto')?.value || '';
    let url = `/api/cxp?estado=${encodeURIComponent(estado)}`;
    if (q.trim()) url += `&q=${encodeURIComponent(q.trim())}`;
    try {
        const res = await apiGet(url) || {};
        cuentas = res.cuentas || [];
        document.getElementById('totalBar').textContent = cuentas.length
            ? `Saldo por pagar: ${fmt(res.totalSaldo)} · Vencido: ${fmt(res.totalVencido)} · ${cuentas.length} cuenta(s)`
            : '';
        renderLista();
    } catch (err) {
        document.getElementById('listaCxp').innerHTML = err.status === 403
            ? `<div class="alert-banner">Tu usuario no tiene permiso para Cuentas por Pagar (solo Admin y Contabilidad).</div>`
            : `<div class="alert-banner">Error: ${esh(err.message)}</div>`;
    }
}

function renderLista() {
    const div = document.getElementById('listaCxp');
    if (!cuentas.length) {
        div.innerHTML = `<div class="empty"><div class="empty-icon">🧾</div>
            <div class="empty-text">No hay cuentas por pagar con ese filtro</div></div>`;
        return;
    }
    div.innerHTML = cuentas.map(c => `
        <div class="card" onclick="verDetalle(${c.id})" style="cursor:pointer">
          <div class="card-row">
            <strong>${esh(c.proveedor || c.codigoProveedor || 'Sin proveedor')}</strong>
            ${badgeEstado(c)}
          </div>
          <div style="font-size:.85rem;color:var(--muted);margin-top:2px">
            ${esh(c.concepto)}${c.numFactura ? ' · Fact. ' + esh(c.numFactura) : ''}
          </div>
          <div class="card-row" style="margin-top:4px">
            <span style="color:var(--muted)">Vence ${esh(fFecha(c.fechaVence))} · Total ${fmt(c.monto)}</span>
            <strong style="font-size:1.05rem">${c.estado === 'Pagada' ? fmt(c.monto) : 'Saldo ' + fmt(c.saldo)}</strong>
          </div>
        </div>`).join('');
}

// ── Detalle ──────────────────────────────────────────────────────────────
async function verDetalle(id) {
    const drawer = document.getElementById('drawerDetalle');
    drawer.innerHTML = `<div class="loading-msg"><div class="spinner"></div></div>`;
    openModal('modalDetalle');
    let datos;
    try { datos = await apiGet(`/api/cxp/${id}`); }
    catch (err) {
        drawer.innerHTML = `<div class="drawer-body"><div class="alert-banner">Error: ${esh(err.message)}</div></div>`;
        return;
    }
    const c = datos.cuenta;
    const pagos = datos.pagos || [];
    cuentaActual = c;
    const vencida = c.estado !== 'Pagada' && c.fechaVence && c.fechaVence < hoyISO();
    drawer.innerHTML = `
      <div class="drawer-header">
        <h2>Cuenta por pagar #${c.id}</h2>
        <button class="btn-close" onclick="closeModal('modalDetalle')">&times;</button>
      </div>
      <div class="drawer-body">
        <div class="field-group"><label>Proveedor</label><div>${esh(c.proveedor || '—')} ${c.codigoProveedor ? '[' + esh(c.codigoProveedor) + ']' : ''}</div></div>
        <div class="field-group"><label>Concepto</label><div>${esh(c.concepto || '—')}</div></div>
        <div class="field-group"><label>N.º de factura</label><div>${esh(c.numFactura || '—')}</div></div>
        <div class="field-group"><label>Emisión / Vencimiento</label><div>${esh(fFecha(c.fechaEmision))} → ${esh(fFecha(c.fechaVence))} ${badgeEstado({ ...c, vencida })}</div></div>
        <div class="field-group"><label>Monto total</label><div>${fmt(c.monto)}</div></div>
        <div class="field-group"><label>Pagado</label><div>${fmt(c.pagado)}</div></div>
        <div class="field-group"><label>Saldo pendiente</label><div><strong style="font-size:1.15rem">${fmt(c.saldo)}</strong></div></div>
        ${c.idEgreso ? `<div class="field-group"><label>Egreso de origen</label><div>Egreso #${c.idEgreso} (se actualiza solo al pagar)</div></div>` : ''}
        ${c.observaciones ? `<div class="field-group"><label>Observaciones</label><div>${esh(c.observaciones)}</div></div>` : ''}
        ${c.estado !== 'Pagada' ? `
        <button class="btn btn-primary" id="btnAbrirPago" onclick="abrirPago()" style="width:100%;margin-top:8px">
          Registrar pago
        </button>` : ''}
        <h3 style="margin:18px 0 8px;font-size:1rem">Historial de pagos</h3>
        ${pagos.length ? pagos.map(p => `
          <div class="card" style="margin:0 0 8px">
            <div class="card-row">
              <span>${esh(fFecha(p.fecha))} · ${esh(p.formaPago || '—')}</span>
              <strong>${fmt(p.monto)}</strong>
            </div>
            ${p.referencia ? `<div style="font-size:.85rem;color:var(--muted)">Ref: ${esh(p.referencia)}</div>` : ''}
            ${p.observaciones ? `<div style="font-size:.85rem;color:var(--muted)">${esh(p.observaciones)}</div>` : ''}
            ${p.tieneComprobante ? `<a href="/api/cxp/pagos/${p.id}/comprobante" target="_blank" rel="noopener"
                 style="font-size:.85rem;color:var(--primary)">📎 Ver comprobante</a>` : ''}
          </div>`).join('') : '<div style="color:var(--muted)">Todavía no hay pagos registrados.</div>'}
      </div>`;
}

// ── Registrar pago ───────────────────────────────────────────────────────
function abrirPago() {
    const c = cuentaActual;
    if (!c) return;
    const drawer = document.getElementById('drawerPago');
    drawer.innerHTML = `
      <div class="drawer-header">
        <h2>Registrar pago</h2>
        <button class="btn-close" onclick="closeModal('modalPago')">&times;</button>
      </div>
      <div class="drawer-body">
        <div style="margin-bottom:12px">
          <strong>${esh(c.proveedor || 'Sin proveedor')}</strong><br>
          <span style="color:var(--muted)">${esh(c.concepto)}${c.numFactura ? ' · Fact. ' + esh(c.numFactura) : ''}</span><br>
          Saldo pendiente: <strong>${fmt(c.saldo)}</strong>
        </div>
        <div class="field-group"><label>Fecha del pago *</label>
          <input type="date" id="pagFecha" class="input-field" value="${hoyISO()}" max="${hoyISO()}">
        </div>
        <div class="field-group"><label>Monto pagado ($) *</label>
          <input type="number" id="pagMonto" class="input-field" step="0.01" min="0.01" max="${c.saldo}" placeholder="0.00">
          <button type="button" class="btn btn-ghost" style="margin-top:6px" onclick="pagarTodo()">Pagar saldo completo (${fmt(c.saldo)})</button>
        </div>
        <div class="field-group"><label>Forma de pago *</label>
          <select id="pagForma" class="input-field">
            <option value="Efectivo">Efectivo</option>
            <option value="Zelle">Zelle</option>
            <option value="Transferencia">Transferencia</option>
            <option value="Pago Móvil">Pago Móvil</option>
            <option value="Cheque">Cheque</option>
            <option value="Deposito">Depósito</option>
            <option value="Otro">Otro</option>
          </select>
        </div>
        <div class="field-group"><label>Referencia</label>
          <input type="text" id="pagRef" class="input-field" placeholder="N.º de confirmación, cheque o transferencia">
        </div>
        <div class="field-group"><label>Comprobante (foto o PDF)</label>
          <input type="file" id="pagImg" accept="image/*,.pdf" class="input-field">
          <div id="pagPreview" style="margin-top:8px"></div>
        </div>
        <div class="field-group"><label>Observaciones</label>
          <textarea id="pagObs" class="input-field" rows="2"></textarea>
        </div>
        <div class="msg" style="margin:12px 0"></div>
        <button class="btn btn-primary" id="btnGuardarPago" onclick="guardarPago()" style="width:100%;margin-top:8px">
          Guardar Pago
        </button>
      </div>`;
    document.getElementById('pagImg')?.addEventListener('change', e => {
        const f = e.target.files[0];
        const prev = document.getElementById('pagPreview');
        if (f && f.type.startsWith('image/')) {
            prev.innerHTML = `<img src="${URL.createObjectURL(f)}" style="max-width:100%;max-height:200px;border-radius:8px">`;
        } else prev.innerHTML = f ? `📄 ${esh(f.name)}` : '';
    });
    openModal('modalPago');
}

function pagarTodo() {
    if (cuentaActual) document.getElementById('pagMonto').value = Number(cuentaActual.saldo).toFixed(2);
}

async function guardarPago() {
    const c = cuentaActual;
    const sel = '#drawerPago .msg';
    const fecha = document.getElementById('pagFecha')?.value || '';
    const montoTxt = (document.getElementById('pagMonto')?.value || '').replace(',', '.');
    const monto = parseFloat(montoTxt);
    const archivo = document.getElementById('pagImg')?.files[0];

    if (!fecha) { showMsg('Indica la fecha del pago.', 'error', sel); return; }
    if (!(monto > 0)) { showMsg('Escribe el monto pagado.', 'error', sel); return; }
    if (monto > c.saldo + 0.01) {
        showMsg(`El monto es mayor que el saldo pendiente (${fmt(c.saldo)}).`, 'error', sel);
        return;
    }
    if (archivo && archivo.size > 15 * 1024 * 1024) {
        showMsg('El comprobante pesa más de 15 MB.', 'error', sel);
        return;
    }

    const fd = new FormData();
    fd.append('fecha', fecha);
    fd.append('monto', monto.toFixed(2));
    fd.append('formaPago', document.getElementById('pagForma')?.value || 'Efectivo');
    fd.append('referencia', document.getElementById('pagRef')?.value || '');
    fd.append('observaciones', document.getElementById('pagObs')?.value || '');
    if (archivo) fd.append('comprobante', archivo);

    const btn = document.getElementById('btnGuardarPago');
    setLoading(btn, true);
    try {
        const res = await fetch(`/api/cxp/${c.id}/pagos`, { method: 'POST', credentials: 'include', body: fd });
        const data = await res.json().catch(() => ({}));
        if (!res.ok) throw new Error(data.error || data.detail || `Error ${res.status}`);
        closeModal('modalPago');
        closeModal('modalDetalle');
        showMsg(data.mensaje || 'Pago registrado.', 'ok');
        await cargarCuentas();
    } catch (err) {
        showMsg('Error: ' + err.message, 'error', sel);
    } finally {
        setLoading(btn, false);
    }
}

render();
