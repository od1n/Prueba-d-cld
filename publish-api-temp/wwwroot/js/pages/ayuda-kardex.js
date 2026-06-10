// ═══════════════════════════════════════════════════════════════
//  ayuda-kardex.js — Walkthrough: módulo Kardex (Inventario)
//  Pasos 1-2: screenshot real del escritorio con highlight
//  Pasos 3-6: simulación CSS animada
// ═══════════════════════════════════════════════════════════════

// ── Definición de pasos ──────────────────────────────────────────
// tipo: 'real' → usa screenshot con highlight overlay
// tipo: 'css'  → usa renderFn para simulación CSS
const PASOS = [
    {
        tipo:        'real',
        titulo:      'Abrir el módulo Producción',
        descripcion: 'En el menú principal, haz clic en la tarjeta <strong>Producción</strong> (segunda fila, izquierda).',
        img:         'menu-principal.png',
        dimLeft:     '0%',
        // Tarjeta Producción: fila inferior, columna izquierda
        // Calibrado sobre captura 1280×720 del 15/05/2026
        hl: [{ t: '53.2%', l: '0.8%', w: '32.8%', h: '9.6%', txt: 'Clic → Producción' }],
        duracion:    3000
    },
    {
        tipo:        'real',
        titulo:      'Vista del Kardex — Inventario',
        descripcion: 'El módulo Kardex muestra el inventario completo. Usa los filtros para buscar un producto y el botón <strong>Nuevo</strong> para registrar un movimiento.',
        img:         'kardex-lista.png',
        dimLeft:     '13%',   // atenuar iconos del escritorio (izquierda 13%)
        // Ventana "Kardex — Control de Inventario" flota en el centro
        // Título de la ventana + grilla de datos
        hl: [{ t: '8.5%', l: '16.5%', w: '42.5%', h: '30%', txt: 'Kardex — Control de Inventario' }],
        duracion:    2800
    },
    {
        tipo:        'css',
        titulo:      'Buscar un producto',
        descripcion: 'Escribe el código o nombre del producto en el campo de búsqueda y pulsa <strong>Buscar</strong>.',
        pantalla:    renderPantallaBusqueda,
        duracion:    2800
    },
    {
        tipo:        'css',
        titulo:      'Revisar el stock actual',
        descripcion: 'Verás el stock actual, el mínimo y el historial de movimientos recientes del producto.',
        pantalla:    renderPantallaStock,
        duracion:    2500
    },
    {
        tipo:        'css',
        titulo:      'Registrar un movimiento',
        descripcion: 'Toca <strong>+ Movimiento</strong> y completa el formulario: tipo (Entrada/Salida), cantidad y motivo.',
        pantalla:    renderPantallaFormulario,
        duracion:    3000
    },
    {
        tipo:        'css',
        titulo:      'Confirmar y guardar',
        descripcion: 'Verifica los datos y toca <strong>Confirmar</strong>. El stock se actualiza automáticamente.',
        pantalla:    renderPantallaConfirmar,
        duracion:    2500
    }
];

// ── Estado ───────────────────────────────────────────────────────
let pasoActual = 0;
let autoTimer  = null;
let modoAuto   = false;

// ── Render principal ─────────────────────────────────────────────
function render() {
    const app = document.getElementById('app');
    app.innerHTML = `
    <div class="ayuda-wrap">
      <div class="ayuda-header">
        <button class="back-btn" id="btn-back" aria-label="Volver">
          <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M19 12H5M12 5l-7 7 7 7"/></svg>
        </button>
        <div class="titulo-bloque">
          <span class="modo-badge">🎬 Demostración animada</span>
          <h1>Cómo usar el Kardex</h1>
        </div>
      </div>

      <div id="pantalla-wrap"></div>

      <div class="wk-step-lista" id="step-lista"></div>

      <div class="wk-controls">
        <button class="btn-ctrl icon-only" id="btn-prev" aria-label="Paso anterior">
          <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M15 18l-6-6 6-6"/></svg>
        </button>
        <button class="btn-ctrl primario" id="btn-auto">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="currentColor"><polygon points="5,3 19,12 5,21"/></svg>
          <span id="auto-label">Reproducir demo</span>
        </button>
        <button class="btn-ctrl icon-only" id="btn-next" aria-label="Paso siguiente">
          <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M9 18l6-6-6-6"/></svg>
        </button>
      </div>
      <div id="auto-info" style="text-align:center;margin-top:6px;font-size:12px;color:var(--texto-dim);">
        Navega manualmente o presiona Reproducir demo
      </div>
    </div>`;

    document.getElementById('btn-back').addEventListener('click', () => history.back());
    document.getElementById('btn-prev').addEventListener('click', irAnterior);
    document.getElementById('btn-next').addEventListener('click', irSiguiente);
    document.getElementById('btn-auto').addEventListener('click', toggleAuto);

    actualizarVista();
}

function actualizarVista() {
    renderStepLista();
    renderPantalla();
    actualizarBotonesNav();
}

function renderStepLista() {
    const lista = document.getElementById('step-lista');
    if (!lista) return;
    lista.innerHTML = PASOS.map((p, i) => {
        let cls = '';
        if (i < pasoActual)  cls = 'completo';
        if (i === pasoActual) cls = 'activo';
        return `
          <div class="wk-step-item ${cls}">
            <div class="wk-step-num">${i < pasoActual ? '✓' : i + 1}</div>
            <div class="wk-step-body">
              <h3>${p.titulo}</h3>
              ${i === pasoActual ? `<p>${p.descripcion}</p>` : ''}
            </div>
          </div>`;
    }).join('');
}

// ── Render pantalla: screenshot real o simulación CSS ────────────
function renderPantalla() {
    const wrap = document.getElementById('pantalla-wrap');
    if (!wrap) return;
    const paso = PASOS[pasoActual];

    if (paso.tipo === 'real') {
        renderScreenshotReal(paso, wrap);
    } else {
        // Contenedor simulado estilo portal móvil
        wrap.innerHTML = `
          <div class="wk-pantalla" style="margin-bottom:20px;">
            <div class="wk-topbar">
              <span class="logo-mini">SG</span>
              <span class="modulo-label">SistemaGestion · Portal</span>
            </div>
            <div id="wk-nav-wrap"></div>
            <div class="wk-cuerpo" id="wk-cuerpo"></div>
          </div>`;
        paso.pantalla();
    }
}

function renderScreenshotReal(paso, wrap) {
    const dimHtml = paso.dimLeft && paso.dimLeft !== '0%'
        ? `<div class="screen-dim" style="width:${paso.dimLeft}"></div>`
        : '';

    const hlHtml = (paso.hl || []).map(h => `
        <div class="hl-real" style="top:${h.t};left:${h.l};width:${h.w};height:${h.h}">
          <span class="hl-label">${h.txt}</span>
        </div>`).join('');

    wrap.innerHTML = `
      <div style="margin-bottom:20px;">
        <div class="real-screen-chip">
          <span class="dot"></span>
          Pantalla real — SistemaGestion escritorio
        </div>
        <div class="wk-real-screen">
          <img src="../img/ayuda/${paso.img}" alt="${paso.titulo}">
          ${dimHtml}
          ${hlHtml}
        </div>
      </div>`;
}

function actualizarBotonesNav() {
    const prev = document.getElementById('btn-prev');
    const next = document.getElementById('btn-next');
    if (prev) prev.disabled = pasoActual === 0;
    if (next) next.disabled = pasoActual === PASOS.length - 1;
}

// ── Navegación manual ────────────────────────────────────────────
function irAnterior() { detenerAuto(); if (pasoActual > 0) { pasoActual--; actualizarVista(); } }
function irSiguiente() { detenerAuto(); if (pasoActual < PASOS.length - 1) { pasoActual++; actualizarVista(); } }

// ── Auto-play ────────────────────────────────────────────────────
function toggleAuto() { modoAuto ? detenerAuto() : iniciarAuto(); }

function iniciarAuto() {
    modoAuto = true;
    actualizarBtnAuto();
    avanzarAuto();
}

function avanzarAuto() {
    if (!modoAuto) return;
    autoTimer = setTimeout(() => {
        if (!modoAuto) return;
        if (pasoActual < PASOS.length - 1) {
            pasoActual++;
            actualizarVista();
            avanzarAuto();
        } else {
            modoAuto = false;
            actualizarBtnAuto();
            const info = document.getElementById('auto-info');
            if (info) info.textContent = 'Demo completado ✓ — Toca Reproducir para verlo de nuevo';
            setTimeout(() => { pasoActual = 0; actualizarVista(); }, 2000);
        }
    }, PASOS[pasoActual].duracion);
}

function detenerAuto() {
    modoAuto = false;
    if (autoTimer) { clearTimeout(autoTimer); autoTimer = null; }
    actualizarBtnAuto();
    const info = document.getElementById('auto-info');
    if (info) info.textContent = 'Navega manualmente o presiona Reproducir demo';
}

function actualizarBtnAuto() {
    const btn = document.getElementById('btn-auto');
    if (!btn) return;
    if (modoAuto) {
        btn.innerHTML = `
          <svg width="14" height="14" viewBox="0 0 24 24" fill="currentColor"><rect x="6" y="4" width="4" height="16"/><rect x="14" y="4" width="4" height="16"/></svg>
          <span id="auto-label">Detener</span>`;
        const info = document.getElementById('auto-info');
        if (info) info.textContent = 'Reproduciendo automáticamente…';
    } else {
        btn.innerHTML = `
          <svg width="14" height="14" viewBox="0 0 24 24" fill="currentColor"><polygon points="5,3 19,12 5,21"/></svg>
          <span id="auto-label">Reproducir demo</span>`;
    }
}

// ══════════════════════════════════════════════════════════════════
//  RENDERIZADO DE PASOS CSS (simulación del portal)
// ══════════════════════════════════════════════════════════════════

function renderNav(activo) {
    const navWrap = document.getElementById('wk-nav-wrap');
    if (!navWrap) return;
    const items = ['Inventario', 'Kardex', 'Clientes', 'Facturas'];
    navWrap.innerHTML = `
      <div class="wk-nav">
        ${items.map(it => `<div class="wk-nav-item ${it === activo ? 'activo' : ''}">${it}</div>`).join('')}
      </div>`;
}

// Paso 3 — Buscador con tipeo animado
function renderPantallaBusqueda() {
    renderNav('Kardex');
    const cuerpo = document.getElementById('wk-cuerpo');
    if (!cuerpo) return;
    cuerpo.innerHTML = `
      <div class="wk-buscador">
        <input type="text" id="wk-input-busq" placeholder="Código o descripción…" value="">
        <button>Buscar</button>
      </div>
      <div style="font-size:12px;color:var(--texto-dim);text-align:center;margin-top:20px;">
        Escribe el nombre del producto para buscarlo
      </div>`;

    const input = document.getElementById('wk-input-busq');
    if (!input) return;
    const texto = 'BOLSA 50x70';
    let idx = 0;
    setTimeout(() => { if (input) input.classList.add('enfocado'); }, 300);
    const timer = setInterval(() => {
        if (!document.getElementById('wk-input-busq')) { clearInterval(timer); return; }
        if (idx < texto.length) { input.value += texto[idx++]; }
        else { clearInterval(timer); }
    }, 100);
}

// Paso 4 — Stock actual con animación de conteo
function renderPantallaStock() {
    renderNav('Kardex');
    const cuerpo = document.getElementById('wk-cuerpo');
    if (!cuerpo) return;
    cuerpo.innerHTML = `
      <div style="font-size:13px;font-weight:600;color:var(--texto-dim);margin-bottom:10px;text-transform:uppercase;letter-spacing:.05em;">
        BOL-50x70 · Bolsa polietileno 50×70
      </div>
      <div class="wk-stock-row">
        <div class="wk-stock-card" id="sc-actual">
          <div class="sk-num">—</div>
          <div class="sk-label">Stock actual</div>
        </div>
        <div class="wk-stock-card">
          <div class="sk-num">500</div>
          <div class="sk-label">Stock mínimo</div>
        </div>
        <div class="wk-stock-card">
          <div class="sk-num">12</div>
          <div class="sk-label">Movimientos</div>
        </div>
      </div>
      <div style="font-size:12px;color:var(--texto-dim);margin-top:4px;border-top:1px solid var(--borde);padding-top:10px;">
        <div style="display:flex;justify-content:space-between;padding:5px 0;border-bottom:1px solid var(--borde);">
          <span>15/05/2026 — Entrada</span><span style="color:var(--verde);">+2.000 u.</span>
        </div>
        <div style="display:flex;justify-content:space-between;padding:5px 0;">
          <span>12/05/2026 — Salida</span><span style="color:var(--rojo);">-500 u.</span>
        </div>
      </div>`;

    setTimeout(() => {
        const sc = document.getElementById('sc-actual');
        if (!sc) return;
        let n = 0; const target = 3450; const step = Math.ceil(target / 20);
        const t = setInterval(() => {
            if (!document.getElementById('sc-actual')) { clearInterval(t); return; }
            n = Math.min(n + step, target);
            sc.querySelector('.sk-num').textContent = n.toLocaleString('es');
            if (n >= target) { clearInterval(t); sc.classList.add('destacado'); }
        }, 60);
    }, 400);
}

// Paso 5 — Formulario nuevo movimiento con llenado animado
function renderPantallaFormulario() {
    renderNav('Kardex');
    const cuerpo = document.getElementById('wk-cuerpo');
    if (!cuerpo) return;
    cuerpo.innerHTML = `
      <div class="wk-form">
        <div class="wk-form-titulo">+ Nuevo Movimiento</div>
        <div class="wk-form-campo">
          <label>Tipo</label>
          <select id="wk-tipo"><option>Entrada</option><option>Salida</option></select>
        </div>
        <div class="wk-form-campo">
          <label>Cantidad</label>
          <input type="number" id="wk-cant" placeholder="0" value="">
        </div>
        <div class="wk-form-campo">
          <label>Motivo / Referencia</label>
          <input type="text" id="wk-motivo" placeholder="Ej: OC-2025-048" value="">
        </div>
        <button class="wk-btn-confirmar" id="wk-confirmar">Confirmar movimiento</button>
      </div>`;

    setTimeout(() => {
        const cant = document.getElementById('wk-cant');
        if (cant) { cant.value = '1000'; cant.classList.add('enfocado'); }
    }, 500);
    setTimeout(() => {
        const mot = document.getElementById('wk-motivo');
        if (mot) { mot.value = 'OC-2026-031'; mot.classList.add('enfocado'); }
    }, 1200);
    setTimeout(() => {
        const btn = document.getElementById('wk-confirmar');
        if (btn) btn.classList.add('visible');
    }, 2000);
}

// Paso 6 — Confirmación exitosa
function renderPantallaConfirmar() {
    renderNav('Kardex');
    const cuerpo = document.getElementById('wk-cuerpo');
    if (!cuerpo) return;
    cuerpo.innerHTML = `
      <div class="wk-success-overlay" id="wk-success">
        <div class="check-circle">✓</div>
        <strong>Movimiento registrado</strong>
        <p style="margin-top:6px;">
          Entrada de <strong>1.000 unidades</strong> registrada.<br>
          Stock actualizado: <strong style="color:var(--cyan);">4.450 u.</strong>
        </p>
      </div>`;

    setTimeout(() => {
        const el = document.getElementById('wk-success');
        if (el) el.classList.add('visible');
    }, 300);
}

// ── Punto de entrada (al final, después de todas las declaraciones) ──
if (!Auth.requireAuth()) { /* redirige al login */ }
else render();
