// ═══════════════════════════════════════════════════════════════
//  insights.js — Agente de Insights + RCA (portal)
//  Capa 1/2: serie con meses-evento · Capa 3: drivers · Capa 4: narración
// ═══════════════════════════════════════════════════════════════
if (!Auth.requireAuth()) {}
const app = document.getElementById('app');

let serie   = null;   // resultado de /api/insights/serie
let mesSel  = null;   // mes seleccionado para el desglose

// Ventana por defecto: la validada contra la base real (36 meses, 7 eventos).
const DEF_DESDE = '2023-01-01';
const DEF_HASTA = '2025-12-31';

function pctBadge(dpct) {
    if (dpct === null || dpct === undefined) return '';
    const signo = dpct >= 0 ? '+' : '';
    return `${signo}${(dpct * 100).toFixed(0)}%`;
}

// ── Render principal ──────────────────────────────────────────────────────
function render() {
    app.innerHTML = `
    ${renderTopbar('Insights')}
    <div class="content">

      <div style="display:flex;gap:8px;padding:10px 16px;flex-wrap:wrap;align-items:flex-end">
        <div style="flex:1;min-width:120px">
          <label class="kpi-label" style="display:block;margin-bottom:3px">Desde</label>
          <input type="date" id="iDesde" class="input-field" style="width:100%" value="${DEF_DESDE}">
        </div>
        <div style="flex:1;min-width:120px">
          <label class="kpi-label" style="display:block;margin-bottom:3px">Hasta</label>
          <input type="date" id="iHasta" class="input-field" style="width:100%" value="${DEF_HASTA}">
        </div>
        <button class="btn btn-primary" onclick="cargarSerie()">Analizar</button>
      </div>

      <div class="msg"></div>

      <div id="serieWrap">
        <div class="empty"><div class="empty-icon">📈</div>
          <div class="empty-text">Elige un período y presiona Analizar</div>
        </div>
      </div>

      <div id="detalleWrap"></div>

    </div>
    ${renderNav('insights')}`;
}

// ── Capa 1 + 2 — serie mensual con eventos ─────────────────────────────────
async function cargarSerie() {
    const desde = document.getElementById('iDesde').value || DEF_DESDE;
    const hasta = document.getElementById('iHasta').value || DEF_HASTA;
    const wrap  = document.getElementById('serieWrap');
    document.getElementById('detalleWrap').innerHTML = '';
    mesSel = null;
    wrap.innerHTML = `<div class="empty"><div class="empty-text">Calculando…</div></div>`;

    try {
        serie = await apiGet(`/api/insights/serie?desde=${encodeURIComponent(desde)}&hasta=${encodeURIComponent(hasta)}`);
    } catch (e) {
        showMsg(e.message || 'No se pudo calcular la serie.', 'error');
        wrap.innerHTML = '';
        return;
    }
    if (!serie || !serie.meses || !serie.meses.length) {
        wrap.innerHTML = `<div class="empty"><div class="empty-text">No hay facturas en ese período.</div></div>`;
        return;
    }

    const eventos = serie.meses.filter(m => m.esEvento);
    const maxV = Math.max(...serie.meses.map(m => Number(m.ventasTotales) || 0), 1);

    wrap.innerHTML = `
      <div class="kpi-grid" style="padding:0 16px 4px">
        <div class="kpi"><div class="kpi-label">Meses</div><div class="kpi-value">${serie.meses.length}</div></div>
        <div class="kpi"><div class="kpi-label">Eventos</div><div class="kpi-value" style="color:var(--naranja)">${eventos.length}</div></div>
        <div class="kpi"><div class="kpi-label">Umbral</div><div class="kpi-value">${(serie.umbralPct * 100).toFixed(0)}%</div></div>
      </div>
      <div class="section-header" style="padding:6px 16px 4px">Ventas por mes <span style="color:var(--texto-dim);font-weight:normal">· toca un mes marcado para ver por qué</span></div>
      <div class="card" style="margin:0 16px 12px;padding:6px 0">
        ${serie.meses.map(m => filaMes(m, maxV)).join('')}
      </div>`;

    // Click en meses-evento
    wrap.querySelectorAll('.js-mes').forEach(el => {
        el.addEventListener('click', () => seleccionarMes(el.dataset.mes));
    });
}

function filaMes(m, maxV) {
    const w = Math.max(2, (Number(m.ventasTotales) / maxV) * 100);
    const esEv = m.esEvento;
    const barColor = esEv ? 'var(--naranja)' : 'var(--azul-claro)';
    const dp = m.deltaPct;
    const dpColor = dp === null ? 'var(--texto-dim)' : (dp >= 0 ? 'var(--verde)' : 'var(--rojo)');
    return `
    <div class="${esEv ? 'js-mes' : ''}" data-mes="${esh(m.mes)}"
         style="padding:7px 14px;border-bottom:1px solid var(--borde);${esEv ? 'cursor:pointer' : ''}">
      <div style="display:flex;justify-content:space-between;align-items:center;gap:8px">
        <span style="font-family:var(--font-mono);font-size:9.5pt">${esh(m.mes)}${esEv ? ' <span style="color:var(--naranja)">● evento</span>' : ''}</span>
        <span style="font-weight:600">${formatUSD(m.ventasTotales)}</span>
      </div>
      <div style="display:flex;align-items:center;gap:8px;margin-top:4px">
        <div style="flex:1;height:6px;background:var(--fondo-inp);border-radius:3px;overflow:hidden">
          <div style="height:100%;width:${w}%;background:${barColor}"></div>
        </div>
        <span style="font-size:8.5pt;min-width:44px;text-align:right;color:${dpColor}">${pctBadge(dp)}</span>
      </div>
    </div>`;
}

// ── Capa 3 + 4 — desglose + narración de un mes ────────────────────────────
async function seleccionarMes(mes) {
    mesSel = mes;
    const wrap = document.getElementById('detalleWrap');
    wrap.innerHTML = `<div class="empty" style="padding:14px"><div class="empty-text">Descomponiendo ${esh(mes)}…</div></div>`;
    wrap.scrollIntoView({ behavior: 'smooth', block: 'start' });

    let dg;
    try {
        dg = await apiGet(`/api/insights/desglose?mes=${encodeURIComponent(mes)}`);
    } catch (e) {
        wrap.innerHTML = `<div class="card" style="margin:0 16px 12px;padding:12px;color:var(--rojo)">${esh(e.message || 'Error al descomponer el mes.')}</div>`;
        return;
    }

    const maxAbs = Math.max(...dg.topProductos.map(p => Math.abs(Number(p.delta))), 1);
    const dpColor = dg.deltaPct >= 0 ? 'var(--verde)' : 'var(--rojo)';

    wrap.innerHTML = `
    <div class="card" style="margin:0 16px 12px;padding:12px 14px">
      <div class="section-header" style="padding:0 0 6px">Evento ${esh(dg.mes)}</div>
      <div style="display:flex;justify-content:space-between;align-items:baseline;gap:8px;flex-wrap:wrap">
        <span style="color:var(--texto-dim);font-size:9pt">${formatUSD(dg.valorAnterior)} → <strong style="color:var(--texto)">${formatUSD(dg.valorActual)}</strong></span>
        <span style="font-size:13pt;font-weight:700;color:${dpColor}">${pctBadge(dg.deltaPct)}</span>
      </div>
      <div style="color:var(--texto-dim);font-size:9pt;margin-top:2px">Δ ${formatUSD(dg.deltaAbs)} respecto al mes anterior</div>
    </div>

    <div class="section-header" style="padding:0 16px 6px">Productos que más explican el cambio</div>
    <div class="card" style="margin:0 16px 12px;padding:8px 0">
      ${dg.topProductos.map(p => filaProducto(p, maxAbs)).join('')}
    </div>

    <div class="section-header" style="padding:0 16px 6px">Principales clientes del mes</div>
    <div class="card" style="margin:0 16px 12px;padding:6px 0">
      ${dg.topClientes.map(c => `
      <div style="display:flex;justify-content:space-between;padding:7px 14px;border-bottom:1px solid var(--borde)">
        <span>${esh(c.nombre || c.codigo)}</span>
        <span style="font-weight:600">${formatUSD(c.ventaTotalMes)}</span>
      </div>`).join('')}
    </div>

    <div style="margin:0 16px 4px;color:var(--texto-dim);font-size:8.5pt">Categoría: ${esh(dg.categoria)}</div>

    <div class="section-header" style="padding:8px 16px 6px">Explicación 🅰 <span style="color:var(--texto-dim);font-weight:normal">· generada, sobre estos números</span></div>
    <div class="card" id="narracionBox" style="margin:0 16px 16px;padding:12px 14px;line-height:1.5">
      <span style="color:var(--texto-dim)">Generando explicación…</span>
    </div>`;

    narrar(mes);
}

function filaProducto(p, maxAbs) {
    const delta = Number(p.delta);
    const w = Math.max(2, (Math.abs(delta) / maxAbs) * 100);
    const color = delta >= 0 ? 'var(--verde)' : 'var(--rojo)';
    const contrib = (Number(p.contribPct) * 100).toFixed(0);
    return `
    <div style="padding:7px 14px;border-bottom:1px solid var(--borde)">
      <div style="display:flex;justify-content:space-between;gap:8px">
        <span style="font-size:9.5pt">${esh(p.descripcion || p.codigo)}</span>
        <span style="font-weight:600;color:${color}">${delta >= 0 ? '+' : ''}${formatUSD(delta)}</span>
      </div>
      <div style="display:flex;align-items:center;gap:8px;margin-top:4px">
        <div style="flex:1;height:6px;background:var(--fondo-inp);border-radius:3px;overflow:hidden">
          <div style="height:100%;width:${w}%;background:${color}"></div>
        </div>
        <span style="font-size:8.5pt;min-width:52px;text-align:right;color:var(--texto-dim)">${contrib}% del Δ</span>
      </div>
    </div>`;
}

// ── Capa 4 — narración con Claude ──────────────────────────────────────────
async function narrar(mes) {
    const box = document.getElementById('narracionBox');
    try {
        const r = await apiPost('/api/insights/narrar', { mes });
        if (box) box.innerHTML = `<div>${esh(r.narracion || '').replace(/\n/g, '<br>')}</div>`;
    } catch (e) {
        if (box) box.innerHTML = `<span style="color:var(--rojo)">${esh(e.message || 'No se pudo generar la explicación.')}</span>
          <div style="color:var(--texto-dim);font-size:8.5pt;margin-top:4px">Los números de arriba siguen siendo válidos; solo falló la narración.</div>`;
    }
}

render();
