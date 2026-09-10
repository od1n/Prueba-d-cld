// nomina.js — Gestión completa de nómina
if (!Auth.requireAuth()) {}
const app = document.getElementById('app');

// ── Estado ────────────────────────────────────────────────────────────────
let periodoActivo = null;   // objeto del período seleccionado
let detallesActivos = [];   // líneas del período abierto
let empleadoEditar = null;  // empleado en formulario
let _empleadosCargados = []; // lista completa para filtro client-side

// ── Tabs ──────────────────────────────────────────────────────────────────
const TABS = [
    { id: 'periodos',   label: '📅 Períodos'   },
    { id: 'empleados',  label: '👤 Empleados'  },
    { id: 'aportes',    label: '🏛 Aportes'    },
    { id: 'beneficios', label: '📋 Beneficios' },
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
    if (tabActiva === 'periodos')        await renderTabPeriodos();
    else if (tabActiva === 'empleados')  await renderTabEmpleados();
    else if (tabActiva === 'aportes')    await renderTabAportes();
    else if (tabActiva === 'beneficios') await renderTabBeneficios();
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
    const totalPagoReal = detalles.reduce((s,d)=>s+(d.pagoRealUSD ?? d.netoUSD ?? 0),0);

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
      <div class="kpi"><div class="kpi-label">Pago total USD</div>
        <div class="kpi-value text-cyan">${formatUSD(totalPagoReal)}</div></div>
      <div class="kpi"><div class="kpi-label">Pago total Bs</div>
        <div class="kpi-value">${formatNum(totalPagoReal*p.tasa,0)}</div></div>
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
          <div class="mono" style="color:var(--cyan)">${formatUSD(d.pagoRealUSD ?? d.netoUSD ?? 0)}</div>
          <div class="text-dim" style="font-size:8.5pt">${formatNum((d.pagoRealUSD ?? d.netoUSD ?? 0)*(p.tasa||1),0)} Bs</div>
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
let _detEditing = null;  // detalle actualmente en edición (para inasistencias)
let _perEditing = null;  // período actual en edición

function editarDetalle(det, periodo) {
    _detEditing = det;
    _perEditing = periodo;
    const cerrado = periodo.estado === 'Cerrado';
    const esProd  = det.tipo === 'Produccion';
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
      ${esProd ? `<div class="form-group">
        <label class="form-label">Inasistencias</label>
        <div class="input-field" style="background:var(--fondo-card);cursor:default" id="dInasCount">—</div>
        <input type="hidden" id="dInas" value="${det.diasInasistencia||0}">
      </div>` : `<div class="form-group">
        <label class="form-label">Inasistencias</label>
        <input id="dInas" type="number" class="input-field" value="${det.diasInasistencia||0}" min="0" step="0.5"
          ${cerrado?'disabled':''} oninput="recalcDetalle()">
      </div>`}
    </div>
    ${esProd ? `<div id="secInasistencias" class="mb-8" style="border:1px solid var(--borde);border-radius:8px;padding:8px">
      <div class="flex-between mb-4">
        <span class="form-label" style="margin:0">Faltas registradas</span>
        ${!cerrado?`<button class="btn btn-sm" style="font-size:9pt" onclick="agregarInasistencia()">＋ Agregar</button>`:''}
      </div>
      <div id="listaInasistencias"><div class="text-dim text-sm">Cargando…</div></div>
    </div>`:''}
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
    // Cargar inasistencias para empleados de producción
    if (esProd) cargarInasistencias(det.idEmpleado, periodo);
}

function recalcDetalle() {
    const clamp = id => Math.max(0, parseFloat(document.getElementById(id)?.value) || 0);
    const dias   = clamp('dDias');
    const inas   = clamp('dInas');
    const sd     = clamp('dSDiario');
    const devP   = clamp('dDevP');
    const devV   = clamp('dDevV');
    const otros  = clamp('dOtrosI');
    const prest  = clamp('dPrest');
    const otrasD = clamp('dOtrasD');
    const tasa   = Math.max(0.01, parseFloat(document.getElementById('dTasa')?.value) || 1);

    const devS     = sd * dias;
    const totDev   = devS + devP + devV + otros;
    // Producción: usa el descuento pre-calculado por el servidor (guardado en _descFaltasProd)
    const esProd   = _detEditing?.tipo === 'Produccion';
    const faltas   = esProd ? (_descFaltasProd || 0) : (sd * inas);
    const dedReal  = faltas + prest + otrasD;
    const pagoReal = Math.max(0, totDev - dedReal);

    const div = document.getElementById('resCalc');
    if (div) div.innerHTML = renderResCalcValues({
        devSueldo:devS, devPiezas:devP, devVentas:devV, otrosIngresos:otros,
        totalDevengado:totDev, faltas, prestamo:prest, otrasDeduccion:otrasD,
        pagoRealUSD:pagoReal, tasa
    });
}

function renderResCalc(d) { return renderResCalcValues(d); }

function renderResCalcValues(d) {
    const pagoReal = d.pagoRealUSD ?? d.netoUSD ?? 0;
    const tasa = d.tasa || 1;
    const hayDed = (d.faltas||0) > 0 || (d.prestamo||0) > 0 || (d.otrasDeduccion||0) > 0;
    return `
    <div class="flex-between mb-4 text-sm">
      <span>Sueldo</span><span class="mono">${formatUSD(d.devSueldo||0)}</span>
    </div>
    ${(d.devPiezas||0)>0?`<div class="flex-between mb-4 text-sm">
      <span>Bono producción</span><span class="mono">${formatUSD(d.devPiezas)}</span></div>`:''}
    ${(d.devVentas||0)>0?`<div class="flex-between mb-4 text-sm">
      <span>Bono ventas</span><span class="mono">${formatUSD(d.devVentas)}</span></div>`:''}
    ${(d.otrosIngresos||0)>0?`<div class="flex-between mb-4 text-sm">
      <span>Otros ingresos</span><span class="mono">${formatUSD(d.otrosIngresos)}</span></div>`:''}
    ${hayDed?`
    <div class="flex-between mb-4 text-sm" style="border-top:1px solid var(--borde);padding-top:4px">
      <span>Total devengado</span><span class="mono">${formatUSD(d.totalDevengado||0)}</span>
    </div>
    ${(d.faltas||0)>0?`<div class="flex-between mb-4 text-sm" style="color:var(--naranja)">
      <span>Desc. inasistencias</span><span class="mono">-${formatUSD(d.faltas)}</span></div>`:''}
    ${(d.prestamo||0)>0?`<div class="flex-between mb-4 text-sm" style="color:var(--naranja)">
      <span>Cuota préstamo</span><span class="mono">-${formatUSD(d.prestamo)}</span></div>`:''}
    ${(d.otrasDeduccion||0)>0?`<div class="flex-between mb-4 text-sm" style="color:var(--naranja)">
      <span>Otras deducciones</span><span class="mono">-${formatUSD(d.otrasDeduccion)}</span></div>`:''}
    `:''}
    <div class="flex-between bold" style="border-top:1px solid var(--borde);padding-top:6px">
      <span>TOTAL A PAGAR USD</span><span class="mono text-cyan" style="font-size:13pt">${formatUSD(pagoReal)}</span>
    </div>
    <div class="flex-between text-dim" style="font-size:9pt">
      <span>Total Bs</span><span class="mono">${formatNum(pagoReal*tasa,2)}</span>
    </div>`;
}

// ── Inasistencias (producción) ─────────────────────────────────────────────
let _descFaltasProd = 0;  // monto en USD calculado por inasistencias

async function cargarInasistencias(idEmpleado, periodo) {
    const div = document.getElementById('listaInasistencias');
    if (!div) return;
    try {
        const list = await apiGet(`/api/nomina/inasistencias?empleado=${idEmpleado}&desde=${periodo.desde}&hasta=${periodo.hasta}`);
        const tasa = parseFloat(document.getElementById('dTasa')?.value) || periodo.tasa || 1;
        const cerrado = periodo.estado === 'Cerrado';
        // Calcular descuento total (solo injustificadas)
        const injust = list.filter(i => !i.justificada);
        const totalBs = injust.reduce((s,i) => s + (i.montoDescuentoBs||0), 0);
        _descFaltasProd = tasa > 0 ? Math.round(totalBs / tasa * 100) / 100 : 0;
        // Actualizar contador
        const countEl = document.getElementById('dInasCount');
        if (countEl) countEl.textContent = `${injust.length} falta${injust.length!==1?'s':''} (${formatUSD(_descFaltasProd)})`;
        // Actualizar hidden
        const hiddenInas = document.getElementById('dInas');
        if (hiddenInas) hiddenInas.value = injust.length;

        if (list.length === 0) {
            div.innerHTML = '<div class="text-dim text-sm">Sin inasistencias registradas</div>';
        } else {
            div.innerHTML = list.map(i => `
              <div class="flex-between" style="padding:4px 0;border-bottom:1px solid var(--borde);font-size:9pt">
                <div>
                  <span class="mono">${i.fecha}</span>
                  <span style="color:${i.justificada?'var(--verde)':'var(--naranja)'}">
                    ${i.justificada?'✓ Justificada':'✗ No justificada'}</span>
                  <span class="text-dim"> · Bs ${formatNum(i.montoDescuentoBs,2)}</span>
                </div>
                ${!cerrado?`<div style="display:flex;gap:4px">
                  <button class="btn btn-sm" style="font-size:8pt;padding:2px 6px"
                    onclick="toggleInasistencia(${i.id},${!i.justificada})">${i.justificada?'Injust.':'Justif.'}</button>
                  <button class="btn btn-sm" style="font-size:8pt;padding:2px 6px;color:var(--naranja)"
                    onclick="eliminarInasistencia(${i.id})">✕</button>
                </div>`:''}
              </div>`).join('');
        }
        recalcDetalle();
    } catch(err) {
        div.innerHTML = `<div class="text-dim text-sm">Error: ${err.message}</div>`;
    }
}

async function agregarInasistencia() {
    if (!_detEditing || !_perEditing) return;
    const fecha = prompt(`Fecha de inasistencia (YYYY-MM-DD)\nPeríodo: ${_perEditing.desde} al ${_perEditing.hasta}`);
    if (!fecha || !/^\d{4}-\d{2}-\d{2}$/.test(fecha)) { if (fecha) alert('Formato inválido. Use YYYY-MM-DD'); return; }
    const tasa = parseFloat(document.getElementById('dTasa')?.value) || _perEditing.tasa || 1;
    try {
        await apiPost('/api/nomina/inasistencias', {
            idEmpleado: _detEditing.idEmpleado,
            fecha, justificada: false,
            observaciones: '', tasaBCV: tasa
        });
        await cargarInasistencias(_detEditing.idEmpleado, _perEditing);
    } catch(err) { alert('Error: ' + err.message); }
}

async function toggleInasistencia(id, justificada) {
    try {
        await apiPost(`/api/nomina/inasistencias/${id}`, { justificada, observaciones: '' }, 'PUT');
        if (_detEditing && _perEditing) await cargarInasistencias(_detEditing.idEmpleado, _perEditing);
    } catch(err) { alert('Error: ' + err.message); }
}

async function eliminarInasistencia(id) {
    if (!confirm('¿Eliminar esta inasistencia?')) return;
    try {
        await apiPost(`/api/nomina/inasistencias/${id}`, {}, 'DELETE');
        if (_detEditing && _perEditing) await cargarInasistencias(_detEditing.idEmpleado, _perEditing);
    } catch(err) { alert('Error: ' + err.message); }
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
    div.innerHTML = `<div style="padding:12px 16px;display:flex;flex-wrap:wrap;justify-content:flex-end;gap:8px;align-items:center">
        <input type="text" id="buscarEmpleado" class="input-field" placeholder="Buscar nombre o cédula…"
          style="flex:1;min-width:140px;max-width:260px" oninput="filtrarEmpleados()">
        <label class="text-dim" style="display:flex;align-items:center;gap:6px;cursor:pointer;white-space:nowrap">
          <input type="checkbox" id="soloActivos" checked onchange="renderTabEmpleados()"> Solo activos
        </label>
        <button class="btn btn-primary btn-sm" onclick="abrirFormEmpleado(null)">＋ Nuevo</button>
    </div>
    <div id="listaEmpleados"><div class="loading-msg"><div class="spinner"></div></div></div>`;
    try {
        const solo = document.getElementById('soloActivos')?.checked ?? true;
        const lista = await apiGet(`/api/nomina/empleados?soloActivos=${solo}`) || [];
        _empleadosCargados = lista;
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

function filtrarEmpleados() {
    const q = (document.getElementById('buscarEmpleado')?.value || '').trim().toLowerCase();
    if (!q) { renderListaEmpleados(_empleadosCargados); return; }
    const filtrada = _empleadosCargados.filter(e => {
        const nombre = (e.nombreCompleto || e.nombre || '').toLowerCase();
        const cedula = (e.cedula || '').toLowerCase();
        return nombre.includes(q) || cedula.includes(q);
    });
    renderListaEmpleados(filtrada);
}

// ── Formulario empleado ────────────────────────────────────────────────────
const TIPOS_EMP = ['Obrero','Oficina','Taller','Administrativo','Temporal'];
const FREQ_POR_TIPO = { Obrero:'Ninguna', Oficina:'Mensual', Taller:'Semanal', Administrativo:'Quincenal', Temporal:'Semanal' };

// Compatibilidad con datos viejos (Fijo/Produccion/Oficina/Ventas)
function mapTipoEmpleado(t) {
    if (t === 'Produccion') return 'Obrero';
    if (t === 'Ventas' || t === 'Oficina') return 'Oficina';
    if (t === 'Fijo') return 'Taller';
    return TIPOS_EMP.includes(t) ? t : 'Obrero';
}
function labelSueldoBase(tipo) {
    return tipo === 'Oficina'        ? 'Sueldo fijo mensual (USD)'
         : tipo === 'Administrativo' ? 'Sueldo quincenal (USD)'
         : tipo === 'Taller'         ? 'Sueldo semanal (USD)'
         : tipo === 'Temporal'       ? 'Sueldo semanal acordado (USD)'
         : 'Sueldo base (USD)';
}
// Devuelve el HTML de la sección de pago según el tipo. emp solo se usa si su tipo coincide.
function pagoEmpleadoHTML(tipo, emp) {
    const base = emp?.sueldoBaseUSD ?? 0;
    const sn   = emp?.salarioNormalUSD ?? 0;
    const bp   = emp ? !!emp.eligibleBonoProd  : (tipo === 'Obrero');
    const bv   = emp ? !!emp.eligibleBonoVenta : (tipo === 'Oficina');
    if (tipo === 'Obrero') {
        return `<div style="padding:10px 12px;background:var(--fondo-inp);border-radius:8px;color:var(--texto-dim);font-size:9pt">
            Cobra por <strong>bono de producción</strong> (tarifa por producto, igual para todos los obreros).
            No lleva sueldo base ni tarifa por persona.</div>`;
    }
    let h = `<div class="form-group mb-8">
        <label class="form-label">${labelSueldoBase(tipo)}</label>
        <input id="eSueldoBase" type="number" class="input-field" value="${base}" min="0" step="0.01">
    </div>`;
    if (tipo === 'Oficina') {
        h += `<label style="display:flex;gap:8px;align-items:center;margin-bottom:10px">
            <input type="checkbox" id="eBonoVenta" ${bv?'checked':''}> Cobra bono de ventas (tarifa por producto)</label>`;
    }
    if (tipo === 'Administrativo') {
        h += `<label style="display:flex;gap:8px;align-items:center;margin-bottom:6px">
            <input type="checkbox" id="eBonoProd" ${bp?'checked':''}> Bono de producción (por producto)</label>
        <label style="display:flex;gap:8px;align-items:center;margin-bottom:10px">
            <input type="checkbox" id="eBonoVenta" ${bv?'checked':''}> Bono de ventas (por producto)</label>`;
    }
    if (tipo !== 'Temporal') {
        h += `<div class="form-group mb-8">
            <label class="form-label">Salario normal USD (para IVSS / prestaciones)</label>
            <input id="eSNormal" type="number" class="input-field" value="${sn}" min="0" step="0.01">
        </div>`;
    }
    return h;
}
function onTipoEmpleadoChange() {
    const tipo = document.getElementById('eTipo')?.value || 'Obrero';
    const cont = document.getElementById('ePago');
    if (!cont) return;
    const emp = (empleadoEditar && mapTipoEmpleado(empleadoEditar.tipoEmpleado) === tipo) ? empleadoEditar : null;
    cont.innerHTML = pagoEmpleadoHTML(tipo, emp);
}

function abrirFormEmpleado(emp) {
    empleadoEditar = emp;
    const nuevo = !emp;
    const tipoInicial = mapTipoEmpleado(emp?.tipoEmpleado || 'Obrero');
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
        <select id="eTipo" class="input-field" onchange="onTipoEmpleadoChange()">
          ${TIPOS_EMP.map(t=>`<option ${tipoInicial===t?'selected':''}>${t}</option>`).join('')}
        </select>
      </div>
      <div class="form-group">
        <label class="form-label">Fecha ingreso</label>
        <input id="eFecha" type="date" class="input-field"
          value="${emp?.fechaIngreso||new Date().toISOString().slice(0,10)}">
      </div>
    </div>
    <div id="ePago" class="mb-8">${pagoEmpleadoHTML(tipoInicial, emp)}</div>
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
    const tipo = document.getElementById('eTipo')?.value || 'Obrero';
    const body = {
        cedula:         document.getElementById('eCedula')?.value?.trim(),
        nombre:         document.getElementById('eNombre')?.value?.trim(),
        apellido:       document.getElementById('eApellido')?.value?.trim() || '',
        cargo:          document.getElementById('eCargo')?.value?.trim() || '',
        tipoEmpleado:   tipo,
        fechaIngreso:   document.getElementById('eFecha')?.value || '',
        telefono:       document.getElementById('eTel')?.value?.trim() || '',
        email:          document.getElementById('eEmail')?.value?.trim() || '',
        frecuenciaPago:   FREQ_POR_TIPO[tipo] || 'Semanal',
        sueldoBaseUSD:    parseFloat(document.getElementById('eSueldoBase')?.value) || 0,
        eligibleBonoProd: tipo === 'Obrero' ? true : !!document.getElementById('eBonoProd')?.checked,
        eligibleBonoVenta: !!document.getElementById('eBonoVenta')?.checked,
        salarioNormalUSD: parseFloat(document.getElementById('eSNormal')?.value) || 0,
        // Compatibilidad: ya no se usan tarifas por persona (el bono es por producto)
        sueldoDiarioUSD: 0, tarifaPiezaUSD: 0, tarifaVentaUSD: 0
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

// ══════════════════════════════════════════════════════════════════════════
//  TAB APORTES PATRONALES (#4)
// ══════════════════════════════════════════════════════════════════════════
async function renderTabAportes() {
    const div = document.getElementById('tabContent');
    div.innerHTML = `<div style="padding:16px">
      <h3 style="color:var(--texto);margin:0 0 8px">Aportes Patronales</h3>
      <p style="color:var(--texto-sec);font-size:13px;margin-bottom:12px">
        Seleccione un período cerrado para ver sus aportes patronales calculados.
      </p>
      <div id="aportesSelPeriodo"></div>
      <div id="aportesContenido"></div>
    </div>`;

    try {
        const periodos = await apiGet('/api/nomina/periodos') || [];
        const cerrados = periodos.filter(p => p.estado === 'Cerrado');
        const selDiv = document.getElementById('aportesSelPeriodo');
        if (cerrados.length === 0) {
            selDiv.innerHTML = '<p style="color:var(--texto-sec)">No hay períodos cerrados con aportes calculados.</p>';
            return;
        }
        selDiv.innerHTML = `<select id="selAportePeriodo" class="input-field" style="max-width:320px;margin-bottom:12px">
            <option value="">— Seleccionar período —</option>
            ${cerrados.map(p => `<option value="${p.id}">${p.desde} al ${p.hasta} (Tasa ${(p.tasa||0).toFixed(2)})</option>`).join('')}
        </select>`;
        document.getElementById('selAportePeriodo').addEventListener('change', async function() {
            const id = this.value;
            if (!id) { document.getElementById('aportesContenido').innerHTML = ''; return; }
            await cargarAportes(id);
        });
    } catch(err) { div.innerHTML = `<p style="color:red">Error: ${err.message}</p>`; }
}

async function cargarAportes(idPeriodo) {
    const cont = document.getElementById('aportesContenido');
    cont.innerHTML = '<div class="loading-msg"><div class="spinner"></div></div>';
    try {
        const data = await apiGet(`/api/nomina/periodos/${idPeriodo}/aportes`);
        const lista = data.aportes || [];
        if (lista.length === 0) {
            cont.innerHTML = '<p style="color:var(--texto-sec)">Sin aportes calculados para este período.</p>';
            return;
        }
        const fmt = n => (n||0).toFixed(2);
        cont.innerHTML = `
        <div style="overflow-x:auto">
          <table class="data-table" style="font-size:12px;width:100%">
            <thead><tr>
              <th>Empleado</th><th style="text-align:right">Salario Bs</th>
              <th style="text-align:right">IVSS %</th><th style="text-align:right">IVSS Bs</th>
              <th style="text-align:right">RPVH Bs</th><th style="text-align:right">INCES Bs</th>
              <th style="text-align:right">Cesta Bs</th><th style="text-align:right">Total Bs</th>
              <th style="text-align:right">Total USD</th>
            </tr></thead>
            <tbody>
              ${lista.map(a => `<tr>
                <td>${a.nombre}</td><td style="text-align:right">${fmt(a.salarioBs)}</td>
                <td style="text-align:right">${fmt(a.pctIVSS)}%</td><td style="text-align:right">${fmt(a.ivss)}</td>
                <td style="text-align:right">${fmt(a.rpvh)}</td><td style="text-align:right">${fmt(a.inces)}</td>
                <td style="text-align:right">${fmt(a.cesta)}</td>
                <td style="text-align:right;font-weight:600">${fmt(a.totalBs)}</td>
                <td style="text-align:right;font-weight:600">${fmt(a.totalUSD)}</td>
              </tr>`).join('')}
            </tbody>
            <tfoot><tr style="font-weight:700;border-top:2px solid var(--borde)">
              <td>TOTAL</td><td colspan="6"></td>
              <td style="text-align:right">${fmt(data.totalAportesBs)}</td>
              <td style="text-align:right">${fmt(lista.reduce((s,a)=>s+(a.totalUSD||0),0))}</td>
            </tr></tfoot>
          </table>
        </div>`;
    } catch(err) { cont.innerHTML = `<p style="color:red">Error: ${err.message}</p>`; }
}

// ══════════════════════════════════════════════════════════════════════════
//  TAB BENEFICIOS LOTTT
// ══════════════════════════════════════════════════════════════════════════
async function renderTabBeneficios() {
    const cont = document.getElementById('tabContent');
    cont.innerHTML = `
    <div style="padding:12px 0">
      <div class="flex-between mb-12">
        <h3 style="margin:0">Beneficios LOTTT</h3>
        <button class="btn btn-sm btn-primary" onclick="abrirCalcBeneficio()">⚙ Calcular</button>
      </div>
      <div style="display:flex;gap:8px;margin-bottom:12px;flex-wrap:wrap">
        <select id="selBenAnio" class="input" style="width:120px" onchange="cargarBeneficios()">
          ${[...Array(5)].map((_,i)=>{const a=new Date().getFullYear()-i; return `<option value="${a}">${a}</option>`;}).join('')}
        </select>
      </div>
      <div id="listaBeneficios"><div class="text-dim text-sm" style="text-align:center;padding:16px 0">Cargando...</div></div>
    </div>`;
    await cargarBeneficios();
}

async function cargarBeneficios() {
    const anio = document.getElementById('selBenAnio')?.value || new Date().getFullYear();
    const div = document.getElementById('listaBeneficios');
    try {
        const data = await apiGet(`/api/nomina/beneficios?anio=${anio}`) || [];
        if (!data.length) { div.innerHTML = `<div class="text-dim text-sm" style="text-align:center;padding:16px 0">Sin beneficios registrados para ${anio}.</div>`; return; }

        // Agrupar por empleado
        const byEmp = {};
        data.forEach(b => {
            if (!byEmp[b.idEmpleado]) byEmp[b.idEmpleado] = { nombre: b.nombre, items: [] };
            byEmp[b.idEmpleado].items.push(b);
        });

        div.innerHTML = Object.values(byEmp).map(emp => `
        <div class="card" style="margin-bottom:10px">
          <div class="card-header" style="font-weight:600;padding:8px 12px;background:var(--fondo-card);border-bottom:1px solid var(--borde)">
            ${esh(emp.nombre)}
          </div>
          <table style="width:100%;font-size:9pt">
            <thead><tr>
              <th style="text-align:left;padding:6px 8px">Tipo</th>
              <th style="text-align:right;padding:6px 4px">Días</th>
              <th style="text-align:right;padding:6px 4px">Monto Legal</th>
              <th style="text-align:right;padding:6px 4px">Monto Gerencia</th>
              <th style="text-align:right;padding:6px 4px">Diferencia</th>
              <th style="text-align:center;padding:6px 4px">Estado</th>
              <th style="text-align:center;padding:6px 4px"></th>
            </tr></thead>
            <tbody>
              ${emp.items.map(b => {
                const dif = (b.montoGerenciaUSD||0) - (b.montoLegalUSD||0);
                const difColor = dif >= 0 ? 'var(--verde,green)' : 'var(--naranja,orange)';
                return `<tr>
                  <td style="padding:4px 8px">${esh(b.tipoBeneficio)}</td>
                  <td style="text-align:right;padding:4px">${formatNum(b.diasCorresponden,0)}</td>
                  <td style="text-align:right;padding:4px">${formatUSD(b.montoLegalUSD||0)}</td>
                  <td style="text-align:right;padding:4px;font-weight:600">${formatUSD(b.montoGerenciaUSD||0)}</td>
                  <td style="text-align:right;padding:4px;color:${difColor}">${dif>=0?'+':''}${formatUSD(dif)}</td>
                  <td style="text-align:center;padding:4px"><span class="tag ${b.estado==='Pagado'?'tag-dim':'tag-verde'}">${esh(b.estado)}</span></td>
                  <td style="text-align:center;padding:4px">
                    <button class="btn btn-sm btn-ghost" style="padding:2px 6px" onclick="editarMontoGerencia(${b.id},${b.montoGerenciaUSD||0},'${esh(b.observaciones||'')}')">✏</button>
                  </td>
                </tr>`;
              }).join('')}
            </tbody>
          </table>
        </div>`).join('');
    } catch(err) { div.innerHTML = `<p style="color:red">Error: ${err.message}</p>`; }
}

async function abrirCalcBeneficio() {
    const drawer = document.getElementById('drawerNom');
    openModal('modalNom');
    // Cargar empleados para el select
    try {
        const emps = await apiGet('/api/nomina/empleados?soloActivos=true') || [];
        drawer.innerHTML = `<div class="modal-handle"></div>
        <div class="modal-title">Calcular Beneficios LOTTT</div>
        <div class="form-group"><label>Empleado</label>
          <select id="benEmp" class="input">${emps.map(e=>`<option value="${e.id}">${esh(e.nombreCompleto)}</option>`).join('')}</select>
        </div>
        <div class="form-group"><label>Año</label>
          <input id="benAnio" type="number" class="input" value="${new Date().getFullYear()}" min="2020" max="2030">
        </div>
        <div class="form-group"><label>Tasa BCV</label>
          <input id="benTasa" type="number" class="input" value="36.5" step="0.01" min="0.01">
        </div>
        <button class="btn btn-primary btn-full" onclick="ejecutarCalcBeneficio()">Calcular</button>
        <div id="resCalcBen" style="margin-top:12px"></div>`;
    } catch(err) { drawer.innerHTML = `<div class="alert-banner">Error: ${err.message}</div>`; }
}

async function ejecutarCalcBeneficio() {
    const idEmp = parseInt(document.getElementById('benEmp')?.value);
    const anio  = parseInt(document.getElementById('benAnio')?.value);
    const tasa  = parseFloat(document.getElementById('benTasa')?.value) || 36.5;
    const div   = document.getElementById('resCalcBen');

    try {
        const data = await apiPost('/api/nomina/beneficios/calcular',
            {idEmpleado:idEmp, anio, tasaBCV:tasa});
        if (!data) { div.innerHTML = `<div class="alert-banner">Error al calcular</div>`; return; }

        div.innerHTML = `
        <div class="card" style="padding:12px">
          <div class="text-sm text-dim mb-8">${esh(data.empleado)} · ${data.aniosServicio} años de servicio · Sal. diario: ${formatUSD(data.salarioDiarioUSD)}</div>
          ${data.beneficios.map(b => `
          <div style="display:flex;justify-content:space-between;align-items:center;padding:6px 0;border-bottom:1px solid var(--borde)">
            <div>
              <div style="font-weight:600">${esh(b.tipo)}</div>
              <div class="text-dim text-sm">${esh(b.obs)} · ${formatNum(b.dias,0)} días</div>
            </div>
            <div style="text-align:right">
              <div class="text-sm text-dim">Legal: ${formatUSD(b.montoLegalUSD)}</div>
              <input type="number" class="input" style="width:100px;text-align:right;font-weight:600"
                value="${b.montoLegalUSD.toFixed(2)}" step="0.01" min="0"
                id="benMG_${b.tipo.replace(/\s/g,'_')}">
              <div class="text-sm text-dim">Monto gerencia</div>
            </div>
          </div>`).join('')}
          <button class="btn btn-primary btn-full" style="margin-top:12px" onclick="guardarBeneficios(${idEmp},'${esh(data.empleado)}',${anio},${tasa})">
            Registrar beneficios
          </button>
        </div>`;
    } catch(err) { div.innerHTML = `<p style="color:red">${err.message}</p>`; }
}

async function guardarBeneficios(idEmp, nombre, anio, tasa) {
    const tipos = ['Utilidades','Vacaciones','Bono_Vacacional','Prestaciones_Sociales'];
    const tiposReales = ['Utilidades','Vacaciones','Bono Vacacional','Prestaciones Sociales'];
    try {
        // Leer cálculo de nuevo para obtener los montos legales
        const calcData = await apiPost('/api/nomina/beneficios/calcular',
            {idEmpleado:idEmp, anio, tasaBCV:tasa});

        for (let i = 0; i < tipos.length; i++) {
            const inp = document.getElementById('benMG_' + tipos[i]);
            const montoGerencia = parseFloat(inp?.value) || 0;
            const ben = calcData.beneficios[i];
            await apiPost('/api/nomina/beneficios', {
                idEmpleado: idEmp, nombreEmpleado: nombre, anio,
                tipoBeneficio: tiposReales[i], diasCorresponden: ben.dias,
                salarioDiarioUSD: calcData.salarioDiarioUSD,
                montoLegalUSD: ben.montoLegalUSD, montoGerenciaUSD: montoGerencia,
                tasaBCV: tasa, observaciones: ben.obs
            });
        }
        closeModal('modalNom');
        await cargarBeneficios();
    } catch(err) { alert('Error: ' + err.message); }
}

async function editarMontoGerencia(id, montoActual, obs) {
    const drawer = document.getElementById('drawerNom');
    openModal('modalNom');
    drawer.innerHTML = `<div class="modal-handle"></div>
    <div class="modal-title">Editar monto gerencia</div>
    <div class="form-group"><label>Monto gerencia (USD)</label>
      <input id="editMG" type="number" class="input" value="${montoActual}" step="0.01" min="0">
    </div>
    <div class="form-group"><label>Observaciones</label>
      <input id="editMGObs" type="text" class="input" value="${obs}">
    </div>
    <button class="btn btn-primary btn-full" onclick="guardarMontoGerencia(${id})">Guardar</button>`;
}

async function guardarMontoGerencia(id) {
    const monto = parseFloat(document.getElementById('editMG')?.value) || 0;
    const obs   = document.getElementById('editMGObs')?.value || '';
    try {
        await apiPost(`/api/nomina/beneficios/${id}`,
            { montoGerenciaUSD: monto, observaciones: obs }, 'PUT');
        closeModal('modalNom');
        await cargarBeneficios();
    } catch(err) { alert('Error: ' + err.message); }
}

render();
