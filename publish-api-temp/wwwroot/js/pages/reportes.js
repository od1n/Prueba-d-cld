// reportes.js — 4 tipos de reportes
if (!Auth.requireAuth()) {}
const app = document.getElementById('app');

let tipoActivo = 'ventas';
let datosReporte = null;

const TIPOS = [
    { id: 'ventas',     label: 'Ventas',      icon: '📈', end: '/api/reportes/ventas'     },
    { id: 'produccion', label: 'Producción',   icon: '🏭', end: '/api/reportes/produccion' },
    { id: 'inventario', label: 'Inventario',   icon: '📦', end: '/api/reportes/inventario' },
    { id: 'egresos',    label: 'Egresos',      icon: '💸', end: '/api/reportes/egresos'    },
];

// ── Render principal ──────────────────────────────────────────────────────
async function render() {
    const hoy = new Date().toISOString().slice(0,10);
    const mesInicio = hoy.slice(0,8) + '01';

    app.innerHTML = `
    ${renderTopbar('Reportes')}
    <div class="content">

      <!-- Selector de tipo -->
      <div style="display:flex;gap:6px;padding:8px 16px;overflow-x:auto">
        ${TIPOS.map(t => `
        <button type="button" class="btn-tipo js-tipo" data-id="${t.id}"
          style="white-space:nowrap;padding:7px 14px;border-radius:20px;border:1px solid var(--borde);
                 background:${t.id===tipoActivo?'var(--azul)':'var(--superficie)'};
                 color:${t.id===tipoActivo?'#fff':'var(--texto)'};cursor:pointer;font:inherit">
          ${t.icon} ${t.label}
        </button>`).join('')}
      </div>

      <!-- Filtros de fecha (no para inventario) -->
      <div id="filtrosFecha" style="display:flex;gap:8px;padding:4px 16px 10px;flex-wrap:wrap">
        <input type="date" id="rDesde" class="input-field" style="flex:1;min-width:120px" value="${mesInicio}" title="Desde">
        <input type="date" id="rHasta" class="input-field" style="flex:1;min-width:120px" value="${hoy}" title="Hasta">
        <button class="btn btn-primary" onclick="generarReporte()">Generar</button>
        <button class="btn btn-ghost" onclick="imprimirReporte()" title="Imprimir / PDF">🖨</button>
      </div>

      <!-- Contenido del reporte -->
      <div id="reporteContenido">
        <div class="empty"><div class="empty-icon">📊</div>
          <div class="empty-text">Selecciona un tipo y presiona Generar</div>
        </div>
      </div>
    </div>
    ${renderNav('reportes')}`;

    // Eventos de selector de tipo
    document.querySelectorAll('.js-tipo').forEach(btn => {
        btn.addEventListener('click', () => {
            tipoActivo = btn.dataset.id;
            document.querySelectorAll('.js-tipo').forEach(b => {
                const activo = b.dataset.id === tipoActivo;
                b.style.background = activo ? 'var(--azul)' : 'var(--superficie)';
                b.style.color      = activo ? '#fff' : 'var(--texto)';
            });
            // Ocultar fechas para inventario
            const filtros = document.getElementById('filtrosFecha');
            if (filtros) filtros.style.display = tipoActivo === 'inventario' ? 'none' : 'flex';
        });
    });
}

// ── Generar reporte ───────────────────────────────────────────────────────
async function generarReporte() {
    const div = document.getElementById('reporteContenido');
    div.innerHTML = `<div class="loading-msg"><div class="spinner"></div> Generando reporte…</div>`;
    const desde = document.getElementById('rDesde')?.value || '';
    const hasta = document.getElementById('rHasta')?.value || '';

    const tipo = TIPOS.find(t => t.id === tipoActivo);
    let url = tipo.end;
    if (tipoActivo !== 'inventario') {
        const ps = [];
        if (desde) ps.push('desde='+encodeURIComponent(desde));
        if (hasta) ps.push('hasta='+encodeURIComponent(hasta));
        if (ps.length) url += '?'+ps.join('&');
    }
    try {
        datosReporte = await apiGet(url);
        renderReporte(tipoActivo, datosReporte);
    } catch(err) {
        div.innerHTML = `<div class="alert-banner">Error: ${esh(err.message)}</div>`;
    }
}

// ── Imprimir ──────────────────────────────────────────────────────────────
function imprimirReporte() {
    const desde = document.getElementById('rDesde')?.value || '';
    const hasta = document.getElementById('rHasta')?.value || '';
    let url = `/api/reportes/imprimir?tipo=${tipoActivo}`;
    if (desde) url += '&desde='+encodeURIComponent(desde);
    if (hasta) url += '&hasta='+encodeURIComponent(hasta);
    window.open(url, '_blank');
}

// ── Renderizar según tipo ─────────────────────────────────────────────────
function renderReporte(tipo, data) {
    const div = document.getElementById('reporteContenido');
    if (!data) { div.innerHTML = `<div class="empty"><div class="empty-text">Sin datos</div></div>`; return; }

    switch(tipo) {
        case 'ventas':     div.innerHTML = renderVentas(data);     break;
        case 'produccion': div.innerHTML = renderProduccion(data); break;
        case 'inventario': div.innerHTML = renderInventario(data); break;
        case 'egresos':    div.innerHTML = renderEgresos(data);    break;
    }
}

// ── Ventas ────────────────────────────────────────────────────────────────
function renderVentas(d) {
    const res = d.resumen || {};
    const porCli = d.porCliente || [];
    const facts  = d.facturas  || [];
    return `
    <!-- KPIs -->
    <div class="kpi-grid" style="padding:0 16px 12px;gap:8px">
      <div class="kpi"><div class="kpi-label">Facturas</div>
        <div class="kpi-value">${res.totalFacturas||0}</div></div>
      <div class="kpi"><div class="kpi-label">Total USD</div>
        <div class="kpi-value text-cyan">${formatUSD(res.totalVentas||0)}</div></div>
      <div class="kpi"><div class="kpi-label">Por cobrar</div>
        <div class="kpi-value" style="color:var(--naranja)">${formatUSD(res.totalSaldo||0)}</div></div>
    </div>

    <!-- Por cliente -->
    ${porCli.length ? `
    <div class="section-header" style="padding:0 16px 6px">Por cliente</div>
    <div class="card" style="margin:0 16px 12px;padding:0">
      ${porCli.slice(0,10).map(c=>`
      <div class="list-item" style="padding:10px 14px;border-bottom:1px solid var(--borde)">
        <div class="list-item-main">
          <div class="list-item-title">${esh(c.cliente||'Sin nombre')}</div>
          <div class="list-item-sub">${c.cantFacturas||0} factura(s)</div>
        </div>
        <div class="list-item-value">${formatUSD(c.totalUSD||0)}</div>
      </div>`).join('')}
    </div>` : ''}

    <!-- Listado facturas -->
    ${facts.length ? `
    <div class="section-header" style="padding:0 16px 6px">Facturas del período</div>
    <div class="card" style="margin:0 16px 12px;padding:0">
      ${facts.map(f=>`
      <div class="list-item" style="padding:8px 14px;border-bottom:1px solid var(--borde)">
        <div class="list-item-main">
          <div class="list-item-title" style="font-size:9.5pt">${esh(f.numeroF)} — ${esh(f.cliente||'')}</div>
          <div class="list-item-sub">${formatFecha(f.fecha)} · ${esh(f.formaDePago||'')}</div>
        </div>
        <div style="text-align:right;flex-shrink:0">
          <div class="mono" style="font-size:10pt">${formatUSD(f.total||0)}</div>
          <span class="tag ${f.pagada?'tag-verde':'tag-naranja'}" style="font-size:8pt">
            ${f.pagada?'Pagada':'Pendiente'}
          </span>
        </div>
      </div>`).join('')}
    </div>` : ''}`;
}

// ── Producción ────────────────────────────────────────────────────────────
function renderProduccion(d) {
    const res = d.resumen || {};
    const porMolde = d.porMolde || [];
    const regs = d.registros || [];
    return `
    <div class="kpi-grid" style="padding:0 16px 12px;gap:8px">
      <div class="kpi"><div class="kpi-label">Total piezas</div>
        <div class="kpi-value">${formatNum(res.totalPiezas||0,0)}</div></div>
      <div class="kpi"><div class="kpi-label">1ª calidad</div>
        <div class="kpi-value text-cyan">${formatNum(res.totalPrimera||0,0)}</div></div>
      <div class="kpi"><div class="kpi-label">Defectuosas</div>
        <div class="kpi-value" style="color:var(--naranja)">${formatNum(res.totalDefecto||0,0)}</div></div>
    </div>

    ${porMolde.length ? `
    <div class="section-header" style="padding:0 16px 6px">Por molde</div>
    <div class="card" style="margin:0 16px 12px;padding:0">
      ${porMolde.map(m=>`
      <div class="list-item" style="padding:10px 14px;border-bottom:1px solid var(--borde)">
        <div class="list-item-main">
          <div class="list-item-title">${esh(m.molde||'Sin molde')}</div>
          <div class="list-item-sub">${m.registros||0} registro(s)</div>
        </div>
        <div class="list-item-value">${formatNum(m.total||0,0)} pzas</div>
      </div>`).join('')}
    </div>` : ''}

    ${regs.length ? `
    <div class="section-header" style="padding:0 16px 6px">Registros del período</div>
    <div class="card" style="margin:0 16px 12px;padding:0">
      ${regs.map(r=>`
      <div class="list-item" style="padding:8px 14px;border-bottom:1px solid var(--borde)">
        <div class="list-item-main">
          <div class="list-item-title" style="font-size:9.5pt">${formatFecha(r.fecha)} — ${esh(r.operario||'')}</div>
          <div class="list-item-sub">${esh(r.molde||'')} · ${esh(r.turno||'')}</div>
        </div>
        <div style="text-align:right;flex-shrink:0;font-size:9.5pt">
          <div>${formatNum(r.cantPrimera||0,0)} / ${formatNum(r.cantSegunda||0,0)} / <span style="color:var(--naranja)">${formatNum(r.cantDefecto||0,0)}</span></div>
          <div class="text-dim" style="font-size:8.5pt">1ª / 2ª / def</div>
        </div>
      </div>`).join('')}
    </div>` : ''}`;
}

// ── Inventario ────────────────────────────────────────────────────────────
function renderInventario(d) {
    const items = d.items || [];
    const vTotal = d.valorTotalInventario || 0;
    return `
    <div class="kpi-grid" style="padding:0 16px 12px;gap:8px">
      <div class="kpi"><div class="kpi-label">Artículos</div>
        <div class="kpi-value">${items.length}</div></div>
      <div class="kpi" style="grid-column:span 2"><div class="kpi-label">Valor total inventario</div>
        <div class="kpi-value text-cyan">${formatUSD(vTotal)}</div></div>
    </div>

    <div class="card" style="margin:0 16px 12px;padding:0">
      ${items.map(i => {
        const bajo = (i.stock||0) < (i.stockMinimo||0);
        return `
        <div class="list-item" style="padding:10px 14px;border-bottom:1px solid var(--borde)">
          <div class="list-item-main">
            <div class="list-item-title">${esh(i.descripcion||i.codigo)}</div>
            <div class="list-item-sub">${esh(i.codigo)} · ${esh(i.categoria||'')}</div>
          </div>
          <div style="text-align:right;flex-shrink:0">
            <div class="mono">${formatNum(i.stock||0,2)} ${esh(i.unidad||'')}</div>
            <span class="tag ${bajo?'tag-rojo':'tag-verde'}" style="font-size:8pt">
              ${bajo?'Stock bajo':'OK'}
            </span>
          </div>
        </div>`;
      }).join('')}
    </div>`;
}

// ── Egresos ───────────────────────────────────────────────────────────────
function renderEgresos(d) {
    const rg = d.resGastos  || {};
    const rc = d.resCompras || {};
    const cats = d.porCategoria || [];
    const gastos = d.gastos || [];
    return `
    <div class="kpi-grid" style="padding:0 16px 12px;gap:8px">
      <div class="kpi"><div class="kpi-label">Gastos</div>
        <div class="kpi-value" style="color:var(--naranja)">${formatUSD(rg.totalUSD||0)}</div></div>
      <div class="kpi"><div class="kpi-label">Compras MP</div>
        <div class="kpi-value" style="color:var(--naranja)">${formatUSD(rc.totalUSD||0)}</div></div>
      <div class="kpi"><div class="kpi-label">Total egresos</div>
        <div class="kpi-value" style="color:var(--rojo,#e04)">${formatUSD((rg.totalUSD||0)+(rc.totalUSD||0))}</div></div>
    </div>

    ${cats.length ? `
    <div class="section-header" style="padding:0 16px 6px">Por categoría</div>
    <div class="card" style="margin:0 16px 12px;padding:0">
      ${cats.map(c=>`
      <div class="list-item" style="padding:10px 14px;border-bottom:1px solid var(--borde)">
        <div class="list-item-main">
          <div class="list-item-title">${esh(c.categoria||'Sin categoría')}</div>
          <div class="list-item-sub">${c.cantidad||0} registro(s)</div>
        </div>
        <div class="list-item-value" style="color:var(--naranja)">${formatUSD(c.totalUSD||0)}</div>
      </div>`).join('')}
    </div>` : ''}

    ${gastos.length ? `
    <div class="section-header" style="padding:0 16px 6px">Gastos del período</div>
    <div class="card" style="margin:0 16px 12px;padding:0">
      ${gastos.map(g=>`
      <div class="list-item" style="padding:8px 14px;border-bottom:1px solid var(--borde)">
        <div class="list-item-main">
          <div class="list-item-title" style="font-size:9.5pt">${esh(g.descripcion||g.tipoGasto||'')}</div>
          <div class="list-item-sub">${formatFecha(g.fecha)} · ${esh(g.categoria||'')} · ${esh(g.proveedor||'')}</div>
        </div>
        <div class="list-item-value" style="color:var(--naranja)">${formatUSD(g.montoUSD||0)}</div>
      </div>`).join('')}
    </div>` : ''}`;
}

render();
