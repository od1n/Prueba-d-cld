// abonos.js — Portal: registro, listado y estado de cuenta de abonos
if (!Auth.requireAuth()) {}
const app = document.getElementById('app');

let abonosList = [];
let clientesList = [];
let cuentasList = [];

// Fecha local en formato yyyy-MM-dd (toISOString usa UTC y después de las 8 p. m.
// en Venezuela devolvía el día siguiente).
function fechaLocalISO(d = new Date()) {
    const x = new Date(d); x.setMinutes(x.getMinutes() - x.getTimezoneOffset());
    return x.toISOString().split('T')[0];
}

async function render() {
    app.innerHTML = `
    ${renderTopbar('Abonos')}
    <div class="content">
      <div class="form-row" style="padding:0 16px 10px;gap:8px;flex-wrap:wrap">
        <select id="filtCliente" class="input-field" style="flex:1;min-width:160px" onchange="cargarAbonos()">
          <option value="">Todos los clientes</option>
        </select>
        <input type="date" id="filtDesde" class="input-field" style="width:130px" onchange="cargarAbonos()">
        <input type="date" id="filtHasta" class="input-field" style="width:130px" onchange="cargarAbonos()">
      </div>
      <div style="display:flex;gap:8px;padding:0 16px 10px;flex-wrap:wrap">
        <button class="btn btn-primary" onclick="abrirNuevoAbono()">+ Nuevo Abono</button>
        <button class="btn btn-ghost" onclick="verEstadoCuenta()">Estado de Cuenta</button>
      </div>
      <div class="msg" style="margin:0 16px"></div>
      <div id="totalBar" class="stat-bar" style="padding:4px 16px;font-weight:600;color:var(--primary)"></div>
      <div id="listaAbonos"><div class="loading-msg"><div class="spinner"></div></div></div>
    </div>

    <div class="modal-overlay" id="modalAbono">
      <div class="modal-drawer" id="drawerAbono" style="max-height:95vh;overflow-y:auto"></div>
    </div>
    <div class="modal-overlay" id="modalDetalle">
      <div class="modal-drawer" id="drawerDetalle" style="max-height:95vh;overflow-y:auto"></div>
    </div>
    <div class="modal-overlay" id="modalEdoCta">
      <div class="modal-drawer" id="drawerEdoCta" style="max-height:95vh;overflow-y:auto"></div>
    </div>
    ${renderNav('abonos')}`;

    const hoy = new Date();
    document.getElementById('filtHasta').value = fechaLocalISO(hoy);
    document.getElementById('filtDesde').value = fechaLocalISO(new Date(hoy.getFullYear(), 0, 1));

    await Promise.all([cargarClientes(), cargarCuentas()]);
    await cargarAbonos();
}

async function cargarClientes() {
    try {
        clientesList = await apiGet('/api/clientes') || [];
        const sel = document.getElementById('filtCliente');
        clientesList.forEach(c => {
            const op = document.createElement('option');
            op.value = c.codigo;
            op.textContent = `${c.nombre} [${c.codigo}]`;
            sel.appendChild(op);
        });
    } catch (err) { console.error('Error cargando clientes:', err); }
}

async function cargarCuentas() {
    try { cuentasList = await apiGet('/api/abonos/cuentas') || []; }
    catch (err) { console.error('Error cargando cuentas:', err); }
}

async function cargarAbonos() {
    const cl = document.getElementById('filtCliente')?.value || '';
    const desde = document.getElementById('filtDesde')?.value || '';
    const hasta = document.getElementById('filtHasta')?.value || '';
    let url = '/api/abonos?';
    if (cl) url += `cliente=${encodeURIComponent(cl)}&`;
    if (desde) url += `desde=${desde}&`;
    if (hasta) url += `hasta=${hasta}&`;

    try {
        abonosList = await apiGet(url) || [];
        renderLista();
    } catch (err) {
        document.getElementById('listaAbonos').innerHTML =
            `<div class="alert-banner">Error: ${esh(err.message)}</div>`;
    }
}

function renderLista() {
    const div = document.getElementById('listaAbonos');
    if (!abonosList.length) {
        div.innerHTML = `<div class="empty"><div class="empty-icon">💳</div>
            <div class="empty-text">Sin abonos registrados</div></div>`;
        document.getElementById('totalBar').textContent = '';
        return;
    }

    const confirmados = abonosList.filter(a => a.estado === 'Confirmado');
    const total = confirmados.reduce((s, a) => s + a.monto, 0);
    document.getElementById('totalBar').textContent =
        `Total confirmados: $ ${total.toFixed(2)} (${abonosList.length} registros)`;

    div.innerHTML = abonosList.map(a => {
        const cuenta = cuentasList.find(c => c.id === a.idCuenta);
        const estadoClass = a.estado === 'Confirmado' ? 'badge-ok'
            : a.estado === 'Pendiente' ? 'badge-warn' : 'badge-err';
        return `
        <div class="card" onclick="verDetalle(${a.id})" style="cursor:pointer">
          <div class="card-row">
            <strong>${esh(a.clienteNombre)}</strong>
            <span class="badge ${estadoClass}">${esh(a.estado)}</span>
          </div>
          <div class="card-row" style="margin-top:4px">
            <span style="color:var(--muted)">${esh(a.fecha)} · ${esh(a.metodoPago)}${a.numeroF ? ' · <strong>' + esh(a.numeroF) + '</strong>' : ''}</span>
            <strong style="font-size:1.05rem">$ ${a.monto.toFixed(2)}</strong>
          </div>
          ${a.referencia ? `<div style="font-size:.85rem;color:var(--muted)">Ref: ${esh(a.referencia)} ${cuenta ? '· ' + esh(cuenta.nombre) : ''}</div>` : ''}
          ${a.rutaComprobante ? '<div style="font-size:.8rem;color:var(--primary)">📎 Comprobante adjunto</div>' : ''}
        </div>`;
    }).join('');
}

// ── Detalle ──────────────────────────────────────────────────────────────
function verDetalle(id) {
    const a = abonosList.find(x => x.id === id);
    if (!a) return;
    const cuenta = cuentasList.find(c => c.id === a.idCuenta);
    const drawer = document.getElementById('drawerDetalle');
    drawer.innerHTML = `
      <div class="drawer-header">
        <h2>Abono #${a.id}</h2>
        <button class="btn-close" onclick="closeModal('modalDetalle')">&times;</button>
      </div>
      <div class="drawer-body">
        <div class="field-group"><label>Cliente</label><div>${esh(a.clienteNombre)} [${esh(a.cliente)}]</div></div>
        ${a.numeroF ? `<div class="field-group"><label>Factura</label><div><strong>${esh(a.numeroF)}</strong></div></div>` : ''}
        <div class="field-group"><label>Fecha</label><div>${esh(a.fecha)}</div></div>
        <div class="field-group"><label>Monto</label><div><strong>$ ${a.monto.toFixed(2)}</strong></div></div>
        <div class="field-group"><label>Método</label><div>${esh(a.metodoPago)}</div></div>
        <div class="field-group"><label>Referencia</label><div>${esh(a.referencia || '—')}</div></div>
        <div class="field-group"><label>Remitente</label><div>${esh(a.remitente || '—')}</div></div>
        <div class="field-group"><label>Cuenta destino</label><div>${cuenta ? esh(cuenta.nombre) : '—'}</div></div>
        <div class="field-group"><label>Estado</label><div>${esh(a.estado)}</div></div>
        <div class="field-group"><label>Observaciones</label><div>${esh(a.observaciones || '—')}</div></div>
        ${a.rutaComprobante ? `<div class="field-group"><label>Comprobante</label>
            <img src="/api/abonos/${a.id}/comprobante" style="max-width:100%;border-radius:8px;margin-top:4px" onerror="this.style.display='none'">
        </div>` : ''}
        ${a.estado === 'Pendiente' ? `
        <div style="display:flex;gap:8px;margin-top:16px">
          <button class="btn btn-primary" onclick="cambiarEstado(${a.id},'Confirmado')">Confirmar</button>
          <button class="btn btn-danger" onclick="cambiarEstado(${a.id},'Rechazado')">Rechazar</button>
        </div>` : ''}
      </div>`;
    openModal('modalDetalle');
}

async function cambiarEstado(id, estado) {
    try {
        await apiPost(`/api/abonos/${id}/estado`, { estado }, 'PUT');
        closeModal('modalDetalle');
        showMsg('Estado actualizado.', 'ok');
        await cargarAbonos();
    } catch (err) {
        showMsg('Error: ' + err.message, 'error');
    }
}

// ── Nuevo Abono ──────────────────────────────────────────────────────────
let cxcPendientesCliente = [];

function abrirNuevoAbono() {
    const cliPre = document.getElementById('filtCliente')?.value || '';
    const drawer = document.getElementById('drawerAbono');
    drawer.innerHTML = `
      <div class="drawer-header">
        <h2>Nuevo Abono</h2>
        <button class="btn-close" onclick="closeModal('modalAbono')">&times;</button>
      </div>
      <div class="drawer-body">
        <div class="field-group"><label>Cliente *</label>
          <select id="aboCliente" class="input-field" onchange="cargarCxcClienteAbono()">
            ${clientesList.map(c => `<option value="${eshA(c.codigo)}" ${c.codigo === cliPre ? 'selected' : ''}>${esh(c.nombre)} [${esh(c.codigo)}]</option>`).join('')}
          </select>
        </div>
        <div class="field-group"><label>Aplicar a Cuenta por Cobrar</label>
          <select id="aboCxc" class="input-field" onchange="cxcAbonoChanged()">
            <option value="auto">Automático (más antigua primero — FIFO)</option>
            <option value="none">Solo abono general (sin descontar CxC)</option>
          </select>
          <div id="aboCxcHint" style="font-size:.8rem;color:var(--muted);margin-top:4px"></div>
        </div>
        <div class="field-group"><label>Fecha</label>
          <input type="date" id="aboFecha" class="input-field" value="${fechaLocalISO()}">
        </div>
        <div class="field-group"><label>Monto ($) *</label>
          <input type="number" id="aboMonto" class="input-field" step="0.01" min="0.01" placeholder="0.00">
        </div>
        <div class="field-group"><label>Método de pago *</label>
          <select id="aboMetodo" class="input-field" onchange="metodoChanged()">
            <option value="Efectivo">Efectivo</option>
            <option value="Zelle">Zelle</option>
            <option value="TransfNac">Transferencia Nacional</option>
            <option value="PagoMovil">Pago Móvil</option>
          </select>
        </div>
        <div class="field-group"><label>Cuenta destino</label>
          <select id="aboCuenta" class="input-field" disabled>
            <option value="0">— No especificada —</option>
            ${cuentasList.filter(c => c.activa).map(c => `<option value="${c.id}">${esh(c.nombre)} (${esh(c.titular)})</option>`).join('')}
          </select>
        </div>
        <div class="field-group"><label>Referencia</label>
          <input type="text" id="aboRef" class="input-field" placeholder="Nro. de confirmación">
        </div>
        <div class="field-group"><label>Remitente</label>
          <input type="text" id="aboRemit" class="input-field" placeholder="Nombre de quien envía">
        </div>
        <div class="field-group"><label>Comprobante (foto)</label>
          <input type="file" id="aboImg" accept="image/*,.pdf" class="input-field">
          <div id="aboPreview" style="margin-top:8px"></div>
        </div>
        <div class="field-group"><label>Observaciones</label>
          <textarea id="aboObs" class="input-field" rows="2"></textarea>
        </div>
        <div class="msg" style="margin:12px 0"></div>
        <button class="btn btn-primary" id="btnGuardarAbono" onclick="guardarAbono()" style="width:100%;margin-top:8px">
          Guardar Abono
        </button>
      </div>`;

    // Preview imagen
    document.getElementById('aboImg')?.addEventListener('change', e => {
        const f = e.target.files[0];
        const prev = document.getElementById('aboPreview');
        if (f && f.type.startsWith('image/')) {
            const url = URL.createObjectURL(f);
            prev.innerHTML = `<img src="${url}" style="max-width:100%;max-height:200px;border-radius:8px">`;
        } else prev.innerHTML = '';
    });

    openModal('modalAbono');
    cargarCxcClienteAbono();
}

async function cargarCxcClienteAbono() {
    const cli = document.getElementById('aboCliente')?.value;
    const sel = document.getElementById('aboCxc');
    const hint = document.getElementById('aboCxcHint');
    if (!sel) return;
    cxcPendientesCliente = [];
    sel.innerHTML = '<option value="auto">Cargando cuentas por cobrar...</option>';
    if (hint) hint.textContent = '';
    if (!cli) return;
    try {
        cxcPendientesCliente = await apiGet(`/api/cxc?cliente=${encodeURIComponent(cli)}&soloPendientes=true`) || [];
        const totalDeuda = cxcPendientesCliente.reduce((s, c) => s + (c.saldo || 0), 0);
        let opts = `<option value="auto">Automático (más antigua primero — FIFO)</option>`;
        for (const c of cxcPendientesCliente) {
            const tag = c.estaVencida ? ' [VENCIDA]' : '';
            opts += `<option value="${c.id}">${esh(c.concepto)} — Saldo: $${Number(c.saldo || 0).toFixed(2)} (Vence ${formatFecha(c.fechaVence)})${tag}</option>`;
        }
        opts += `<option value="none">Solo abono general (sin descontar CxC)</option>`;
        sel.innerHTML = opts;
        if (hint) {
            hint.textContent = cxcPendientesCliente.length > 0
                ? `${cxcPendientesCliente.length} cuenta(s) pendiente(s) — Deuda total: $${totalDeuda.toFixed(2)}`
                : 'Este cliente no tiene CxC pendientes.';
        }
    } catch {
        sel.innerHTML = `<option value="auto">Automático (más antigua primero — FIFO)</option>
                         <option value="none">Solo abono general (sin descontar CxC)</option>`;
    }
}

function cxcAbonoChanged() {
    const val = document.getElementById('aboCxc')?.value;
    const id = parseInt(val, 10);
    if (!isNaN(id) && id > 0) {
        const c = cxcPendientesCliente.find(x => x.id === id);
        const inpMonto = document.getElementById('aboMonto');
        if (c && inpMonto) inpMonto.value = Number(c.saldo || 0).toFixed(2);
    }
}

function metodoChanged() {
    const met = document.getElementById('aboMetodo')?.value;
    const cuenta = document.getElementById('aboCuenta');
    if (cuenta) {
        cuenta.disabled = (met === 'Efectivo');
        if (met === 'Efectivo') cuenta.value = '0';
    }
}

async function guardarAbono() {
    const cliente = document.getElementById('aboCliente')?.value;
    const cxcSel = document.getElementById('aboCxc')?.value || 'auto';
    const fecha = document.getElementById('aboFecha')?.value;
    const monto = document.getElementById('aboMonto')?.value;
    const metodo = document.getElementById('aboMetodo')?.value;
    const cuenta = document.getElementById('aboCuenta')?.value || '0';
    const ref = document.getElementById('aboRef')?.value || '';
    const remit = document.getElementById('aboRemit')?.value || '';
    const obs = document.getElementById('aboObs')?.value || '';
    const archivo = document.getElementById('aboImg')?.files[0];

    if (!cliente || !monto || parseFloat(monto) <= 0) {
        showMsg('Cliente y monto son requeridos.', 'error', '#drawerAbono .msg');
        return;
    }

    const fd = new FormData();
    fd.append('cliente', cliente);
    fd.append('fecha', fecha);
    fd.append('monto', monto);
    fd.append('metodoPago', metodo);
    fd.append('idCuenta', cuenta);
    fd.append('referencia', ref);
    fd.append('remitente', remit);
    fd.append('observaciones', obs);
    fd.append('estado', 'Confirmado');
    if (cxcSel === 'none') {
        fd.append('aplicarCxC', 'false');
    } else if (cxcSel !== 'auto' && parseInt(cxcSel, 10) > 0) {
        fd.append('idCuentaCobrar', cxcSel);
        fd.append('aplicarCxC', 'true');
    } else {
        fd.append('aplicarCxC', 'true');
    }
    if (archivo) fd.append('comprobante', archivo);

    const btn = document.getElementById('btnGuardarAbono');
    setLoading(btn, true);
    try {
        const res = await fetch('/api/abonos', {
            method: 'POST', credentials: 'include', body: fd
        });
        const data = await res.json().catch(() => ({}));
        if (!res.ok) {
            throw new Error(data.error || `Error ${res.status}`);
        }
        closeModal('modalAbono');
        showMsg(data.mensaje || 'Abono registrado.', 'ok');
        await cargarAbonos();
    } catch (err) {
        showMsg('Error: ' + err.message, 'error', '#drawerAbono .msg');
    } finally {
        setLoading(btn, false);
    }
}

// ── Estado de Cuenta ─────────────────────────────────────────────────────
async function verEstadoCuenta() {
    const cli = document.getElementById('filtCliente')?.value;
    if (!cli) {
        showMsg('Selecciona un cliente para ver su estado de cuenta.', 'error');
        return;
    }
    try {
        const data = await apiGet(`/api/abonos/estado-cuenta/${encodeURIComponent(cli)}`);
        if (!data) return;
        const drawer = document.getElementById('drawerEdoCta');
        const confirmados = data.abonos.filter(a => a.estado === 'Confirmado');

        let tblAbonos = '';
        if (confirmados.length) {
            tblAbonos = `<table class="tbl-compact">
              <thead><tr><th>Fecha</th><th>Método</th><th>Ref.</th><th style="text-align:right">Monto</th></tr></thead>
              <tbody>
              ${confirmados.map(a => `<tr>
                <td>${esh(a.fecha)}</td><td>${esh(a.metodo)}</td>
                <td>${esh(a.referencia || '—')}</td>
                <td style="text-align:right">$ ${a.monto.toFixed(2)}</td>
              </tr>`).join('')}
              </tbody>
            </table>`;
        } else tblAbonos = '<p style="color:var(--muted)">Sin abonos confirmados.</p>';

        let tblFact = '';
        if (data.facturasPendientes?.length) {
            tblFact = `<table class="tbl-compact">
              <thead><tr><th>Concepto</th><th style="text-align:right">Total</th>
                <th style="text-align:right">Pagado</th><th style="text-align:right">Saldo</th></tr></thead>
              <tbody>
              ${data.facturasPendientes.map(f => `<tr>
                <td>${esh(f.concepto)}</td>
                <td style="text-align:right">$ ${f.total.toFixed(2)}</td>
                <td style="text-align:right">$ ${f.pagado.toFixed(2)}</td>
                <td style="text-align:right;font-weight:600">$ ${f.saldo.toFixed(2)}</td>
              </tr>`).join('')}
              </tbody>
            </table>`;
        } else tblFact = '<p style="color:var(--muted)">Sin facturas pendientes.</p>';

        drawer.innerHTML = `
          <div class="drawer-header">
            <h2>Estado de Cuenta</h2>
            <button class="btn-close" onclick="closeModal('modalEdoCta')">&times;</button>
          </div>
          <div class="drawer-body">
            <div style="margin-bottom:16px">
              <h3 style="margin:0">${esh(data.cliente.nombre)}</h3>
              <div style="color:var(--muted)">RIF: ${esh(data.cliente.ciRif)} | Cod: ${esh(data.cliente.codigo)}</div>
            </div>
            <div class="stat-row" style="display:flex;gap:12px;flex-wrap:wrap;margin-bottom:16px">
              <div class="stat-card" style="flex:1">
                <div style="font-size:.85rem;color:var(--muted)">Total abonos</div>
                <div style="font-size:1.2rem;font-weight:700;color:var(--primary)">$ ${data.totalAbonos.toFixed(2)}</div>
              </div>
              <div class="stat-card" style="flex:1">
                <div style="font-size:.85rem;color:var(--muted)">Total deuda</div>
                <div style="font-size:1.2rem;font-weight:700;color:var(--danger,#B42828)">$ ${data.totalDeuda.toFixed(2)}</div>
              </div>
              <div class="stat-card" style="flex:1">
                <div style="font-size:.85rem;color:var(--muted)">Saldo a favor</div>
                <div style="font-size:1.2rem;font-weight:700;color:var(--success,#2E7D32)">$ ${data.saldoFavor.toFixed(2)}</div>
              </div>
            </div>
            <h3>Abonos confirmados</h3>
            ${tblAbonos}
            <h3 style="margin-top:16px">Facturas pendientes</h3>
            ${tblFact}
            <div style="text-align:center;margin-top:16px;font-size:.8rem;color:var(--muted)">
              EL HATO PASCHER C.A. — RIF J-30571871-7<br>
              Generado: ${new Date().toLocaleDateString('es-VE')}
            </div>
          </div>`;
        openModal('modalEdoCta');
    } catch (err) {
        showMsg('Error: ' + err.message, 'error');
    }
}

render();
