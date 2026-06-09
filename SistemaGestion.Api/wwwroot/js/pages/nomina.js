// nomina.js — Gestión completa de nómina
if (!Auth.requireAuth()) {}
const app = document.getElementById('app');

// ── Estado ────────────────────────────────────────────────────────────────
let periodoActivo = null;   // objeto del período seleccionado
let detallesActivos = [];   // líneas del período abierto
let empleadoEditar = null;  // empleado en formulario

// ── Tabs ──────────────────────────────────────────────────────────────────
const TABS = [
    { id: 'periodos',  label: '📅 Períodos'  },
    { id: 'empleados', label: '👤 Empleados' },
];
let tabActiva = 'periodos';

// ── Render principal ──────────────────────────────────────────────────────
async function render() {
    app.innerHTML = `
    ${renderTopbar('Nómina')}
    <div class="content">
      <div style="display:flex;gap:0;border-bottom:2px solid var(--borde);margin-bottom:0">
        ${TABS.map(t=>`
        <button type="button" class="js-tab" data-id="${t.id}"
          style="flex:1;padding:10px;background:none;border:none;cursor:pointer;
                 font:inherit;color:${t.id===tabActiva?'var(--cyan)':'var(--texto-sec)'};
                 border-bottom:${t.id===tabActiva?'2px solid var(--cyan)':'2px solid transparent'};
                 margin-bottom:-2px">${t.label}</button>`).join('')}
      </div>
      <div id="tabContent" style="padding-top:0"></div>
    </div>

    <!-- Modales -->
    <div class="modal-overlay" id="modalNom">
      <div class="modal-drawer" id="drawerNom" style="max-height:95vh;overflow-y:auto"></div>
    </div>

    ${renderNav('nomina')}`;

    document.querySelectorAll('.js-tab').forEach(btn => {
        btn.addEventListener('click', () => {
            tabActiva = btn.dataset.id;
            document.querySelectorAll('.js-tab').forEach(b => {
                const a = b.dataset.id === tabActiva;
                b.style.color = a ? 'var(--cyan)' : 'var(--texto-sec)';
                b.style.borderBottom = a ? '2px solid var(--cyan)' : '2px solid transparent';
            });
            renderTab();
        });
    });
    renderTab();
}

async function renderTab() {
    if (tabActiva === 'periodos')  await renderTabPeriodos();
    else                            await renderTabEmpleados();
}

// ══════════════════════════════════════════════════════════════════════════
//  TAB PERÍODOS
// ══════════════════════════════════════════════════════════════════════════
async function renderTabPeriodos() {
    const div = document.getElementById('tabContent');
    div.innerHTML = `<div style="padding:12px 16px;display:flex;justify-content:flex-end">
        <button class="btn btn-primary btn-sm" onclick="abrirNuevoPeriodo()">＋ Nuevo período</button>
    </div>
    <div id="listaPeriodos"><div class="loading-msg"><div class="spinner"></div></div></div>`;
    try {
        const lista = await apiGet('/api/nomina/periodos') || [];
        renderListaPeriodos(lista);
    } catch(err) {
        document.getElementById('listaPeriodos').innerHTML =
            `<div class="alert-banner">Error: ${esh(err.message)}</div>`;
    }
}

function renderListaPeriodos(lista) {
    const div = document.getElementById('listaPeriodos');
    if (!lista.length) {
        div.innerHTML = `<div class="empty"><div class="empty-icon">📅</div>
            <div class="empty-text">Sin períodos aún</div></div>`;
        return;
    }
    div.innerHTML = `<div class="card" style="padding:0;margin:0 16px">
        ${lista.map((p,ix) => `
        <button type="button" class="list-item js-periodo" data-ix="${ix}"
          style="padding:12px 16px;background:none;border:none;border-bottom:1px solid var(--borde);
                 text-align:left;width:100%;cursor:pointer;color:inherit;font:inherit">
          <div class="list-item-main">
            <div class="list-item-title">${formatFecha(p.desde)} — ${formatFecha(p.hasta)}</div>
            <div class="list-item-sub">Tasa BCV: ${formatNum(p.tasa,2)} · ${esh(p.obs||'')}</div>
          </div>
          <div style="text-align:right;flex-shrink:0">
            <span class="tag ${p.estado==='Cerrado'?'tag-dim':'tag-verde'}">${esh(p.estado)}</span>
          </div>
        </button>`).join('')}
    </div>`;
    div.querySelectorAll('.js-periodo').forEach(btn => {
        btn.addEventListener('click', () => abrirPeriodo(lista[+btn.dataset.ix]));
    });
}

// ── Nuevo período ──────────────────────────────────────────────────────────
function abrirNuevoPeriodo() {
    const hoy = new Date().toISOString().slice(0,10);
    const lunes = getLunes(new Date());
    const drawer = document.getElementById('drawerNom');
    drawer.innerHTML = `
    <div class="modal-handle"></div>
    <div class="modal-title">Nuevo Período de Nómina</div>
    <div class="form-grid-2 mb-8">
      <div class="form-group">
        <label class="form-label">Desde</label>
        <input id="pDesde" type="date" class="input-field" value="${lunes}">
      </div>
      <div class="form-group">
        <label class="form-label">Hasta</label>
        <input id="pHasta" type="date" class="input-field" value="${hoy}">
      </div>
    </div>
    <div class="form-group mb-8">
      <label class="form-label">Tasa BCV (0 = tomar automáticamente)</label>
      <input id="pTasa" type="number" class="input-field" value="0" min="0" step="0.01">
    </div>
    <div class="form-group mb-16">
      <label class="form-label">Observaciones</label>
      <textarea id="pObs" class="input-field" rows="2"></textarea>
    </div>
    <div style="display:flex;flex-direction:column;gap:8px">
      <button class="btn btn-primary btn-full" onclick="guardarPeriodo()">Crear período</button>
      <button class="btn btn-ghost btn-full" onclick="closeModal('modalNom')">Cancelar</button>
    </div>`;
    openModal('modalNom');
}

async function guardarPeriodo() {
    const body = {
        desde: document.getElementById('pDesde')?.value,
        hasta: document.getElementById('pHasta')?.value,
        tasaBCV: parseFloat(document.getElementById('pTasa')?.value) || 0,
        observaciones: document.getElementById('pObs')?.value?.trim() || ''
    };
    if (!body.desde || !body.hasta) { alert('Ingresa ambas fechas.'); return; }
    const btn = document.querySelector('#drawerNom .btn-primary');
    if (btn) { btn.disabled=true; btn.textContent='Creando…'; }
    try {
        await apiPost('/api/nomina/periodos', body);
        closeModal('modalNom');
        await renderTabPeriodos();
    } catch(err) {
        if (btn) { btn.disabled=false; btn.textContent='Crear período'; }
        alert('Error: ' + err.message);
    }
}

// ── Abrir período — ver detalles + procesar ────────────────────────────────
async function abrirPeriodo(periodo) {
    periodoActivo = periodo;
    const drawer = document.getElementById('drawerNom');
    drawer.innerHTML = `<div class="modal-handle"></div>
        <div class="modal-title">Cargando…</div><div class="spinner"></div>`;
    openModal('modalNom');
    try {
        const data = await apiGet(`/api/nomina/periodos/${periodo.id}/detalles`);
        detallesActivos = data.detalles || [];
        renderDetallesPeriodo(data.periodo, detallesActivos);
    } catch(err) {
        drawer.innerHTML = `<div class="modal-handle"></div>
            <div class="alert-banner">Error: ${esh(err.message)}</div>
            <button class="btn btn-ghost btn-full" onclick="closeModal('modalNom')">Cerrar</button>`;
    }
}

function renderDetallesPeriodo(p, detalles) {
    const drawer = document.getElementById('drawerNom');
    const cerrado = p.estado === 'Cerrado';
    const totalNeto = detalles.reduce((s,d)=>s+(d.netoUSD||0),0);

    drawer.innerHTML = `
    <div class="modal-handle"></div>
    <div class="flex-between mb-8">
      <div class="modal-title" style="margin-bottom:0">
        ${formatFecha(p.desde)} — ${formatFecha(p.hasta)}
      </div>
      <span class="tag ${cerrado?'tag-dim':'tag-verde'}">${esh(p.estado)}</span>
    </div>
    <div class="text-dim text-sm mb-12">Tasa BCV: ${formatNum(p.tasa,2)} Bs/USD</div>

    <!-- KPIs -->
    <div class="kpi-grid" style="gap:8px;margin-bottom:12px">
      <div class="kpi"><div class="kpi-label">Empleados</div>
        <div class="kpi-value">${detalles.length}</div></div>
      <div class="kpi"><div class="kpi-label">Neto total USD</div>
        <div class="kpi-value text-cyan">${formatUSD(totalNeto)}</div></div>
      <div class="kpi"><div class="kpi-label">Neto Bs</div>
        <div class="kpi-value">${formatNum(totalNeto*p.tasa,0)}</div></div>
    </div>

    <!-- Acciones -->
    ${!cerrado ? `
    <div style="display:flex;gap:6px;margin-bottom:12px;flex-wrap:wrap">
      <button class="btn btn-sm btn-primary" style="flex:1"
        onclick="procesarPeriodo(${p.id})">⚙ Calcular / Actualizar</button>
      <button class="btn btn-sm" style="flex:1;background:var(--verde,#1a7a40);color:#fff"
        onclick="confirmarCerrar(${p.id})">🔒 Cerrar período</button>
      <a href="/api/nomina/periodos/${p.id}/recibos" target="_blank"
        class="btn btn-sm btn-ghost" style="flex:1">🖨 Recibos</a>
    </div>` : `
    <div style="margin-bottom:12px">
      <a href="/api/nomina/periodos/${p.id}/recibos" target="_blank"
        class="btn btn-ghost btn-full">🖨 Imprimir recibos</a>
    </div>`}

    <!-- Lista empleados -->
    ${!detalles.length ? `
    <div class="text-dim text-sm" style="text-align:center;padding:16px 0">
      Sin datos — presiona "Calcular" para generar los cálculos.
    </div>` : `
    <div class="card" style="padding:0">
      ${detalles.map((d,ix) => `
      <button type="button" class="list-item js-det-btn" data-ix="${ix}"
        style="padding:10px 14px;background:none;border:none;border-bottom:1px solid var(--borde);
               text-align:left;width:100%;cursor:pointer;color:inherit;font:inherit">
        <div class="list-item-main">
          <div class="list-item-title">${esh(d.nombre)}</div>
          <div class="list-item-sub">${esh(d.tipo)} · ${formatNum(d.diasTrabajados||0,0)} días
          ${d.diasInasistencia>0?` · <span style="color:var(--naranja)">${formatNum(d.diasInasistencia,0)} inasis.</span>`:''}
          </div>
        </div>
        <div style="text-align:right;flex-shrink:0">
          <div class="mono" style="color:var(--cyan)">${formatUSD(d.netoUSD||0)}</div>
          <div class="text-dim" style="font-size:8.5pt">${formatNum((d.netoUSD||0)*(p.tasa||1),0)} Bs</div>
        </div>
      </button>`).join('')}
    </div>`}

    <button class="btn btn-ghost btn-full" style="margin-top:12px"
      onclick="closeModal('modalNom')">Cerrar</button>`;

    drawer.querySelectorAll('.js-det-btn').forEach(btn => {
        btn.addEventListener('click', () =>
            editarDetalle(detallesActivos[+btn.dataset.ix], p));
    });
}

async function procesarPeriodo(id) {
    const btn = document.querySelector('#drawerNom .btn-primary');
    if (btn) { btn.disabled=true; btn.textContent='Calculando…'; }
    try {
        const res = await apiPost(`/api/nomina/periodos/${id}/procesar`, {});
        const data = await apiGet(`/api/nomina/periodos/${id}/detalles`);
        detallesActivos = data.detalles || [];
        renderDetallesPeriodo(data.periodo, detallesActivos);
        // pequeño banner de confirmación
        const div = document.getElementById('drawerNom');
        const msg = document.createElement('div');
        msg.className='alert-banner';
        msg.style.cssText='background:#1a5c2a;color:#fff;margin:8px 0;border-radius:4px;padding:8px 12px';
        msg.textContent=`✓ Se procesaron ${res.procesados} empleados (${formatNum(res.diasPeriodo,0)} días)`;
        div.insertBefore(msg, div.children[1]);
        setTimeout(()=>msg.remove(), 4000);
    } catch(err) {
        if (btn) { btn.disabled=false; btn.textContent='⚙ Calcular / Actualizar'; }
        alert('Error: ' + err.message);
    }
}

async function confirmarCerrar(id) {
    if (!confirm('¿Cerrar este período? No podrás editarlo después.')) return;
    try {
        await apiPost(`/api/nomina/periodos/${id}/cerrar`, {}, 'PUT');
        const data = await apiGet(`/api/nomina/periodos/${id}/detalles`);
        renderDetallesPeriodo(data.periodo, data.detalles||[]);
    } catch(err) { alert('Error: '+err.message); }
}

// ── Editar línea de detalle ────────────────────────────────────────────────
function editarDetalle(det, periodo) {
    const cerrado = periodo.estado === 'Cerrado';
    const drawer  = document.getElementById('drawerNom');
    drawer.innerHTML = `
    <div class="modal-handle"></div>
    <div class="modal-title">${esh(det.nombre)}</div>
    <div class="text-dim text-sm mb-16">${esh(det.tipo)} — Tasa: ${formatNum(det.tasa||periodo.tasa,2)}</div>

    <div class="form-grid-2 mb-8">
      <div class="form-group">
        <label class="form-label">Días trabajados</label>
        <input id="dDias" type="number" class="input-field" value="${det.diasTrabajados||0}" min="0" step="0.5"
          ${cerrado?'disabled':''} oninput="recalcDetalle()">
      </div>
      <div class="form-group">
        <label class="form-label">Inasistencias</label>
        <input id="dInas" type="number" class="input-field" value="${det.diasInasistencia||0}" min="0" step="0.5"
          ${cerrado?'disabled':''} oninput="recalcDetalle()">
      </div>
    </div>
    <div class="form-grid-2 mb-8">
      <div class="form-group">
        <label class="form-label">Sueldo diario (USD)</label>
        <input id="dSDiario" type="number" class="input-field" value="${det.sueldoDiario||0}" min="0" step="0.01"
          ${cerrado?'disabled':''} oninput="recalcDetalle()">
      </div>
      <div class="form-group">
        <label class="form-label">Piezas producidas</label>
        <input id="dPiezas" type="number" class="input-field" value="${det.piezasProducidas||0}" min="0"
          ${cerrado?'disabled':''}>
      </div>
    </div>
    <div class="form-grid-2 mb-8">
      <div class="form-group">
        <label class="form-label">Bono piezas (USD)</label>
        <input id="dDevP" type="number" class="input-field" value="${det.devPiezas||0}" min="0" step="0.01"
          ${cerrado?'disabled':''} oninput="recalcDetalle()">
      </div>
      <div class="form-group">
        <label class="form-label">Bono ventas (USD)</label>
        <input id="dDevV" type="number" class="input-field" value="${det.devVentas||0}" min="0" step="0.01"
          ${cerrado?'disabled':''} oninput="recalcDetalle()">
      </div>
    </div>
    <div class="form-grid-2 mb-8">
      <div class="form-group">
        <label class="form-label">Otros ingresos (USD)</label>
        <input id="dOtrosI" type="number" class="input-field" value="${det.otrosIngresos||0}" min="0" step="0.01"
          ${cerrado?'disabled':''} oninput="recalcDetalle()">
      </div>
      <div class="form-group">
        <label class="form-label">Cuota préstamo (USD)</label>
        <input id="dPrest" type="number" class="input-field" value="${det.prestamo||0}" min="0" step="0.01"
          ${cerrado?'disabled':''} oninput="recalcDetalle()">
      </div>
    </div>
    <div class="form-group mb-8">
      <label class="form-label">Otras deducciones (USD)</label>
      <input id="dOtrasD" type="number" class="input-field" value="${det.otrasDeduccion||0}" min="0" step="0.01"
        ${cerrado?'disabled':''} oninput="recalcDetalle()">
    </div>
    <div class="form-group mb-8">
      <label class="form-label">Tasa BCV</label>
      <input id="dTasa" type="number" class="input-field" value="${det.tasa||periodo.tasa||1}" min="0.01" step="0.01"
        ${cerrado?'disabled':''} oninput="recalcDetalle()">
    </div>
    <div class="form-group mb-8">
      <label class="form-label">Observaciones</label>
      <textarea id="dObs" class="input-field" rows="2" ${cerrado?'disabled':''}>${esh(det.obs||'')}</textarea>
    </div>

    <!-- Resumen calculado -->
    <div class="card" id="resCalc" style="padding:12px;margin-bottom:12px;font-size:10pt">
      ${renderResCalc(det)}
    </div>

    <div style="display:flex;flex-direction:column;gap:8px">
      ${!cerrado?`<button class="btn btn-primary btn-full" onclick="guardarDetalle(${det.id})">Guardar cambios</button>`:''}
      <button class="btn btn-ghost btn-full" onclick="abrirPeriodo(periodoActivo)">‹ Volver al período</button>
    </div>`;
}

function recalcDetalle() {
    const dias   = parseFloat(document.getElementById('dDias')?.value) || 0;
    const inas   = parseFloat(document.getElementById('dInas')?.value) || 0;
    const sd     = parseFloat(document.getElementById('dSDiario')?.value) || 0;
    const devP   = parseFloat(document.getElementById('dDevP')?.value) || 0;
    const devV   = parseFloat(document.getElementById('dDevV')?.value) || 0;
    const otros  = parseFloat(document.getElementById('dOtrosI')?.value) || 0;
    const prest  = parseFloat(document.getElementById('dPrest')?.value) || 0;
    const otrasD = parseFloat(document.getElementById('dOtrasD')?.value) || 0;
    const tasa   = parseFloat(document.getElementById('dTasa')?.value) || 1;

    const devS    = sd * dias;
    const totDev  = devS + devP + devV + otros;
    const totBs   = totDev * tasa;
    const ivss    = Math.round(totBs * 0.04 / tasa * 100) / 100;
    const rpvh    = Math.round(totBs * 0.01 / tasa * 100) / 100;
    const faltas  = sd * inas;
    const totDed  = ivss + rpvh + faltas + prest + otrasD;
    const neto    = totDev - totDed;

    const div = document.getElementById('resCalc');
    if (div) div.innerHTML = renderResCalcValues({
        devSueldo:devS, devPiezas:devP, devVentas:devV, otrosIngresos:otros,
        totalDevengado:totDev, ivss, rpvh, faltas, prestamo:prest, otrasDeduccion:otrasD,
        totalDeducciones:totDed, netoUSD:neto, tasa, netoBs:neto*tasa
    });
}

function renderResCalc(d) { return renderResCalcValues(d); }

function renderResCalcValues(d) {
    return `
    <div class="flex-between mb-4 text-sm">
      <span>Dev. Sueldo</span><span class="mono">${formatUSD(d.devSueldo||0)}</span>
    </div>
    ${(d.devPiezas||0)>0?`<div class="flex-between mb-4 text-sm">
      <span>Dev. Piezas</span><span class="mono">${formatUSD(d.devPiezas)}</span></div>`:''}
    ${(d.devVentas||0)>0?`<div class="flex-between mb-4 text-sm">
      <span>Dev. Ventas</span><span class="mono">${formatUSD(d.devVentas)}</span></div>`:''}
    ${(d.otrosIngresos||0)>0?`<div class="flex-between mb-4 text-sm">
      <span>Otros ingresos</span><span class="mono">${formatUSD(d.otrosIngresos)}</span></div>`:''}
    <div class="flex-between mb-4 text-sm bold">
      <span>Total devengado</span><span class="mono">${formatUSD(d.totalDevengado||0)}</span>
    </div>
    <div class="flex-between mb-4 text-sm" style="color:var(--naranja)">
      <span>IVSS 4%</span><span class="mono">-${formatUSD(d.ivss||0)}</span>
    </div>
    <div class="flex-between mb-4 text-sm" style="color:var(--naranja)">
      <span>RPVH 1%</span><span class="mono">-${formatUSD(d.rpvh||0)}</span>
    </div>
    ${(d.faltas||0)>0?`<div class="flex-between mb-4 text-sm" style="color:var(--naranja)">
      <span>Desc. faltas</span><span class="mono">-${formatUSD(d.faltas)}</span></div>`:''}
    ${(d.prestamo||0)>0?`<div class="flex-between mb-4 text-sm" style="color:var(--naranja)">
      <span>Cuota préstamo</span><span class="mono">-${formatUSD(d.prestamo)}</span></div>`:''}
    ${(d.otrasDeduccion||0)>0?`<div class="flex-between mb-4 text-sm" style="color:var(--naranja)">
      <span>Otras deducciones</span><span class="mono">-${formatUSD(d.otrasDeduccion)}</span></div>`:''}
    <div class="flex-between bold" style="border-top:1px solid var(--borde);padding-top:6px">
      <span>NETO USD</span><span class="mono text-cyan" style="font-size:13pt">${formatUSD(d.netoUSD||0)}</span>
    </div>
    <div class="flex-between text-dim" style="font-size:9pt">
      <span>Neto Bs</span><span class="mono">${formatNum((d.netoUSD||0)*(d.tasa||1),2)}</span>
    </div>`;
}

async function guardarDetalle(id) {
    const body = {
        diasTrabajados:   parseFloat(document.getElementById('dDias')?.value) || 0,
        diasInasistencia: parseFloat(document.getElementById('dInas')?.value) || 0,
        piezasProducidas: parseFloat(document.getElementById('dPiezas')?.value) || 0,
        ventasCerradas:   0,
        sueldoDiarioUSD:  parseFloat(document.getElementById('dSDiario')?.value) || 0,
        devengadoPiezas:  parseFloat(document.getElementById('dDevP')?.value) || 0,
        devengadoVentas:  parseFloat(document.getElementById('dDevV')?.value) || 0,
        otrosIngresos:    parseFloat(document.getElementById('dOtrosI')?.value) || 0,
        cuotaPrestamo:    parseFloat(document.getElementById('dPrest')?.value) || 0,
        otrasDeduccion:   parseFloat(document.getElementById('dOtrasD')?.value) || 0,
        tasaBCV:          parseFloat(document.getElementById('dTasa')?.value) || 1,
        observaciones:    document.getElementById('dObs')?.value?.trim() || ''
    };
    const btn = document.querySelector('#drawerNom .btn-primary');
    if (btn) { btn.disabled=true; btn.textContent='Guardando…'; }
    try {
        await apiPost(`/api/nomina/detalles/${id}`, body, 'PUT');
        await abrirPeriodo(periodoActivo);
    } catch(err) {
        if (btn) { btn.disabled=false; btn.textContent='Guardar cambios'; }
        alert('Error: '+err.message);
    }
}

// ══════════════════════════════════════════════════════════════════════════
//  TAB EMPLEADOS
// ══════════════════════════════════════════════════════════════════════════
async function renderTabEmpleados() {
    const div = document.getElementById('tabContent');
    div.innerHTML = `<div style="padding:12px 16px;display:flex;justify-content:flex-end;gap:8px">
        <label class="text-dim" style="display:flex;align-items:center;gap:6px;cursor:pointer">
          <input type="checkbox" id="soloActivos" checked onchange="renderTabEmpleados()"> Solo activos
        </label>
        <button class="btn btn-primary btn-sm" onclick="abrirFormEmpleado(null)">＋ Nuevo</button>
    </div>
    <div id="listaEmpleados"><div class="loading-msg"><div class="spinner"></div></div></div>`;
    try {
        const solo = true; // estado del checkbox al momento de carga
        const lista = await apiGet('/api/nomina/empleados?soloActivos=true') || [];
        renderListaEmpleados(lista);
    } catch(err) {
        document.getElementById('listaEmpleados').innerHTML =
            `<div class="alert-banner">Error: ${esh(err.message)}</div>`;
    }
}

function renderListaEmpleados(lista) {
    const div = document.getElementById('listaEmpleados');
    if (!lista.length) {
        div.innerHTML = `<div class="empty"><div class="empty-icon">👤</div>
            <div class="empty-text">Sin empleados registrados</div></div>`;
        return;
    }
    div.innerHTML = `<div class="card" style="padding:0;margin:0 16px">
        ${lista.map((e,ix) => `
        <button type="button" class="list-item js-emp-btn" data-ix="${ix}"
          style="padding:12px 16px;background:none;border:none;border-bottom:1px solid var(--borde);
                 text-align:left;width:100%;cursor:pointer;color:inherit;font:inherit">
          <div class="list-item-main">
            <div class="list-item-title">${esh(e.nombreCompleto||e.nombre)}</div>
            <div class="list-item-sub">${esh(e.cargo||'')} · ${esh(e.tipoEmpleado||'')} · C.I. ${esh(e.cedula||'')}</div>
          </div>
          <div style="text-align:right;flex-shrink:0">
            <div class="mono text-dim">${formatUSD(e.sueldoDiarioUSD||0)}/día</div>
            <span class="tag ${e.activo?'tag-verde':'tag-dim'}">${e.activo?'Activo':'Inactivo'}</span>
          </div>
        </button>`).join('')}
    </div>`;
    div.querySelectorAll('.js-emp-btn').forEach(btn => {
        btn.addEventListener('click', () => abrirFormEmpleado(lista[+btn.dataset.ix]));
    });
}

// ── Formulario empleado ────────────────────────────────────────────────────
function abrirFormEmpleado(emp) {
    empleadoEditar = emp;
    const nuevo = !emp;
    const drawer = document.getElementById('drawerNom');
    drawer.innerHTML = `
    <div class="modal-handle"></div>
    <div class="modal-title">${nuevo?'Nuevo Empleado':'Editar Empleado'}</div>

    <div class="form-grid-2 mb-8">
      <div class="form-group">
        <label class="form-label">Cédula *</label>
        <input id="eCedula" class="input-field" value="${esh(emp?.cedula||'')}" placeholder="V-XXXXXXXX">
      </div>
      <div class="form-group">
        <label class="form-label">Cargo</label>
        <input id="eCargo" class="input-field" value="${esh(emp?.cargo||'')}">
      </div>
    </div>
    <div class="form-grid-2 mb-8">
      <div class="form-group">
        <label class="form-label">Nombre *</label>
        <input id="eNombre" class="input-field" value="${esh(emp?.nombre||'')}">
      </div>
      <div class="form-group">
        <label class="form-label">Apellido</label>
        <input id="eApellido" class="input-field" value="${esh(emp?.apellido||'')}">
      </div>
    </div>
    <div class="form-grid-2 mb-8">
      <div class="form-group">
        <label class="form-label">Tipo empleado</label>
        <select id="eTipo" class="input-field">
          ${['Fijo','Produccion','Oficina','Ventas'].map(t=>
              `<option ${(emp?.tipoEmpleado||'Fijo')===t?'selected':''}>${t}</option>`).join('')}
        </select>
      </div>
      <div class="form-group">
        <label class="form-label">Fecha ingreso</label>
        <input id="eFecha" type="date" class="input-field"
          value="${emp?.fechaIngreso||new Date().toISOString().slice(0,10)}">
      </div>
    </div>
    <div class="form-grid-2 mb-8">
      <div class="form-group">
        <label class="form-label">Sueldo diario USD</label>
        <input id="eSDiario" type="number" class="input-field" value="${emp?.sueldoDiarioUSD||0}" min="0" step="0.01">
      </div>
      <div class="form-group">
        <label class="form-label">Tarifa pieza USD</label>
        <input id="eTarPieza" type="number" class="input-field" value="${emp?.tarifaPiezaUSD||0}" min="0" step="0.001">
      </div>
    </div>
    <div class="form-grid-2 mb-8">
      <div class="form-group">
        <label class="form-label">Tarifa venta USD</label>
        <input id="eTarVenta" type="number" class="input-field" value="${emp?.tarifaVentaUSD||0}" min="0" step="0.01">
      </div>
      <div class="form-group">
        <label class="form-label">Salario normal USD</label>
        <input id="eSNormal" type="number" class="input-field" value="${emp?.salarioNormalUSD||0}" min="0" step="0.01">
      </div>
    </div>
    <div class="form-grid-2 mb-16">
      <div class="form-group">
        <label class="form-label">Teléfono</label>
        <input id="eTel" class="input-field" value="${esh(emp?.telefono||'')}">
      </div>
      <div class="form-group">
        <label class="form-label">Email</label>
        <input id="eEmail" type="email" class="input-field" value="${esh(emp?.email||'')}">
      </div>
    </div>

    <div style="display:flex;flex-direction:column;gap:8px">
      <button class="btn btn-primary btn-full" onclick="guardarEmpleado()">Guardar empleado</button>
      ${!nuevo?`<button class="btn btn-ghost btn-full" style="color:var(--naranja)"
        onclick="desactivarEmpleado(${emp.id})">Dar de baja</button>`:''}
      <button class="btn btn-ghost btn-full" onclick="closeModal('modalNom')">Cancelar</button>
    </div>`;
    openModal('modalNom');
}

async function guardarEmpleado() {
    const body = {
        cedula:         document.getElementById('eCedula')?.value?.trim(),
        nombre:         document.getElementById('eNombre')?.value?.trim(),
        apellido:       document.getElementById('eApellido')?.value?.trim() || '',
        cargo:          document.getElementById('eCargo')?.value?.trim() || '',
        tipoEmpleado:   document.getElementById('eTipo')?.value || 'Fijo',
        fechaIngreso:   document.getElementById('eFecha')?.value || '',
        telefono:       document.getElementById('eTel')?.value?.trim() || '',
        email:          document.getElementById('eEmail')?.value?.trim() || '',
        sueldoDiarioUSD: parseFloat(document.getElementById('eSDiario')?.value) || 0,
        tarifaPiezaUSD:  parseFloat(document.getElementById('eTarPieza')?.value) || 0,
        tarifaVentaUSD:  parseFloat(document.getElementById('eTarVenta')?.value) || 0,
        salarioNormalUSD:parseFloat(document.getElementById('eSNormal')?.value) || 0
    };
    if (!body.cedula) { alert('La cédula es requerida.'); return; }
    if (!body.nombre) { alert('El nombre es requerido.'); return; }
    const btn = document.querySelector('#drawerNom .btn-primary');
    if (btn) { btn.disabled=true; btn.textContent='Guardando…'; }
    try {
        if (empleadoEditar) {
            await apiPost(`/api/nomina/empleados/${empleadoEditar.id}`, body, 'PUT');
        } else {
            await apiPost('/api/nomina/empleados', body);
        }
        closeModal('modalNom');
        await renderTabEmpleados();
    } catch(err) {
        if (btn) { btn.disabled=false; btn.textContent='Guardar empleado'; }
        alert('Error: '+err.message);
    }
}

async function desactivarEmpleado(id) {
    if (!confirm('¿Dar de baja a este empleado? Se marcará como inactivo.')) return;
    try {
        await apiPost(`/api/nomina/empleados/${id}`, {}, 'DELETE');
        closeModal('modalNom');
        await renderTabEmpleados();
    } catch(err) { alert('Error: '+err.message); }
}

// ── Helpers ───────────────────────────────────────────────────────────────
function getLunes(d) {
    const day = d.getDay();
    const diff = d.getDate() - day + (day===0?-6:1);
    const lunes = new Date(d.setDate(diff));
    return lunes.toISOString().slice(0,10);
}

// apiPost con método extendido ya está en app.js (soporta PUT/DELETE)

render();
