// ═══════════════════════════════════════════════════════════════
//  ayuda-factura.js — Walkthrough animado: Crear factura completa
//  Replica la UI real del portal SistemaGestion paso a paso
// ═══════════════════════════════════════════════════════════════

// ── Pasos del walkthrough ────────────────────────────────────────
// tipo: 'real' → screenshot real del escritorio con highlight
// tipo: 'css'  → simulación CSS animada (portal web)
const PASOS = [
    {
        tipo:        'real',
        titulo:      'Abrir el módulo Transacciones',
        descripcion: 'En el menú principal, haz clic en la tarjeta <strong>Transacciones</strong> (primera fila, centro).',
        img:         'menu-principal.png',
        dimLeft:     '0%',
        // Tarjeta Transacciones: fila superior, columna central
        // Calibrado sobre captura 1280×720 del 15/05/2026
        hl: [{ t: '42.4%', l: '33.6%', w: '33%', h: '9.7%', txt: 'Clic → Transacciones' }],
        duracion:    3000
    },
    {
        tipo:        'real',
        titulo:      'Seleccionar Facturas',
        descripcion: 'En el diálogo de Transacciones, haz clic en <strong>Facturas</strong> para ver la lista y crear nuevas.',
        img:         'transacciones-menu.png',
        dimLeft:     '13%',   // atenuar iconos del escritorio (izquierda ~13%)
        // Diálogo flotante "Transacciones" con 6 opciones (2 filas × 3 cols)
        // Facturas es la opción superior izquierda (col 1, fila 1)
        hl: [{ t: '25%', l: '23%', w: '11.5%', h: '19%', txt: 'Clic → Facturas' }],
        duracion:    2800
    },
    {
        tipo:        'css',
        titulo:      'Pulsar "+ Nueva Factura"',
        descripcion: 'En la lista de facturas, toca el botón <strong>+ Nueva Factura</strong> que aparece en la esquina inferior derecha.',
        pantalla:    renderPantallaLista,
        duracion:    2800
    },
    {
        tipo:        'css',
        titulo:      'Buscar el cliente',
        descripcion: 'Escribe el nombre o número del cliente en el campo de búsqueda. El sistema mostrará coincidencias en tiempo real.',
        pantalla:    renderPantallaBuscarCliente,
        duracion:    3200
    },
    {
        tipo:        'css',
        titulo:      'Seleccionar el cliente',
        descripcion: 'Toca el cliente correcto de la lista de resultados para vincularlo a la factura.',
        pantalla:    renderPantallaSeleccionarCliente,
        duracion:    2200
    },
    {
        tipo:        'css',
        titulo:      'Agregar productos / servicios',
        descripcion: 'Toca <strong>+ Agregar ítem</strong> y completa la descripción, cantidad y precio de cada producto.',
        pantalla:    renderPantallaAgregarItems,
        duracion:    3000
    },
    {
        tipo:        'css',
        titulo:      'Revisar totales',
        descripcion: 'El sistema calcula automáticamente el subtotal, el IVA y el total a cobrar. Verifica que los montos sean correctos.',
        pantalla:    renderPantallaTotales,
        duracion:    2500
    },
    {
        tipo:        'css',
        titulo:      'Guardar la factura',
        descripcion: 'Toca <strong>Guardar Factura</strong>. El sistema asignará el número correlativo y registrará la operación.',
        pantalla:    renderPantallaGuardar,
        duracion:    2500
    },
    {
        tipo:        'css',
        titulo:      'Imprimir o compartir PDF',
        descripcion: 'Desde el detalle de la factura, toca <strong>Imprimir PDF</strong> para generar el documento e imprimirlo o compartirlo.',
        pantalla:    renderPantallaImprimir,
        duracion:    2500
    }
];

// ── Estado ────────────────────────────────────────────────────────
let pasoActual = 0;
let autoTimer  = null;
let modoAuto   = false;

// ── Render principal ──────────────────────────────────────────────
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
          <h1>Crear una factura</h1>
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
        let cls = i < pasoActual ? 'completo' : i === pasoActual ? 'activo' : '';
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

function renderPantalla() {
    const wrap = document.getElementById('pantalla-wrap');
    if (!wrap) return;
    const paso = PASOS[pasoActual];

    if (paso.tipo === 'real') {
        renderScreenshotReal(paso, wrap);
        return;
    }

    // Simulación CSS
    wrap.innerHTML = `
      <div class="wk-pantalla" style="margin-bottom:20px;">
        <div class="wk-topbar">
          <span class="logo-mini">SG</span>
          <span class="modulo-label">SistemaGestion · Portal</span>
        </div>
        <div id="wk-nav-wrap"></div>
        <div id="wk-cuerpo-outer"></div>
      </div>`;
    paso.pantalla();
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

function irAnterior() {
    detenerAuto();
    if (pasoActual > 0) { pasoActual--; actualizarVista(); }
}
function irSiguiente() {
    detenerAuto();
    if (pasoActual < PASOS.length - 1) { pasoActual++; actualizarVista(); }
}

// ── Auto-play ─────────────────────────────────────────────────────
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
            setInfo('Demo completo ✓ — Toca Reproducir para verlo de nuevo');
            setTimeout(() => { pasoActual = 0; actualizarVista(); }, 2000);
        }
    }, PASOS[pasoActual].duracion);
}
function detenerAuto() {
    modoAuto = false;
    if (autoTimer) { clearTimeout(autoTimer); autoTimer = null; }
    actualizarBtnAuto();
    setInfo('Navega manualmente o presiona Reproducir demo');
}
function actualizarBtnAuto() {
    const btn = document.getElementById('btn-auto');
    if (!btn) return;
    btn.innerHTML = modoAuto
        ? `<svg width="14" height="14" viewBox="0 0 24 24" fill="currentColor"><rect x="6" y="4" width="4" height="16"/><rect x="14" y="4" width="4" height="16"/></svg><span id="auto-label">Detener</span>`
        : `<svg width="14" height="14" viewBox="0 0 24 24" fill="currentColor"><polygon points="5,3 19,12 5,21"/></svg><span id="auto-label">Reproducir demo</span>`;
    if (modoAuto) setInfo('Reproduciendo automáticamente…');
}
function setInfo(txt) {
    const el = document.getElementById('auto-info');
    if (el) el.textContent = txt;
}

// ══════════════════════════════════════════════════════════════════
//  PANTALLAS SIMULADAS
// ══════════════════════════════════════════════════════════════════

function renderNavSimulada(activo) {
    const items = [
        { id: 'dashboard', ic: '◉', lab: 'Inicio' },
        { id: 'inventario', ic: '▦', lab: 'Stock' },
        { id: 'kardex',    ic: '⇅', lab: 'Kardex' },
        { id: 'clientes',  ic: '◎', lab: 'Clientes' },
        { id: 'facturas',  ic: '▤', lab: 'Facturas' },
        { id: 'scan',      ic: '⊡', lab: 'Scan' },
    ];
    const nav = document.getElementById('wk-nav-wrap');
    if (!nav) return;
    nav.innerHTML = `
      <div style="display:flex;border-bottom:2px solid var(--borde);background:var(--fondo-card);">
        ${items.map(it => `
          <div style="flex:1;text-align:center;padding:7px 4px 6px;font-size:10px;
               color:${it.id === activo ? 'var(--cyan)' : 'var(--texto-dim)'};
               border-bottom:${it.id === activo ? '2px solid var(--cyan)' : '2px solid transparent'};
               margin-bottom:-2px;transition:all .3s;">
            <div style="font-size:15px;margin-bottom:2px;">${it.ic}</div>
            ${it.lab}
          </div>`).join('')}
      </div>`;
}

// Paso 1 — Menú con Facturas resaltado y spinner de carga
function renderPantallaNav() {
    renderNavSimulada('facturas');
    const outer = document.getElementById('wk-cuerpo-outer');
    if (!outer) return;
    outer.innerHTML = `
      <div class="wk-cuerpo" style="display:flex;flex-direction:column;align-items:center;justify-content:center;padding:30px 20px;">
        <div style="font-size:32px;margin-bottom:10px;">▤</div>
        <div style="font-weight:600;color:var(--cyan);margin-bottom:6px;">Facturas</div>
        <div style="font-size:13px;color:var(--texto-dim);margin-bottom:16px;">Cargando registros…</div>
        <div class="spinner-mini"></div>
      </div>`;
}

// Paso 2 — Lista de facturas con FAB pulsando
function renderPantallaLista() {
    renderNavSimulada('facturas');
    const outer = document.getElementById('wk-cuerpo-outer');
    if (!outer) return;
    outer.innerHTML = `
      <div class="wk-cuerpo-rel">
        <div class="wk-searchbar" style="margin-bottom:10px;">
          <input placeholder="🔍  Buscar factura…">
          <button>Filtros</button>
        </div>
        <div style="background:var(--fondo-card);border:1px solid var(--borde);border-radius:var(--radio);overflow:hidden;">
          <div class="wk-fila">
            <div class="wk-fila-main" style="flex:1;">
              <div style="font-size:13px;font-weight:500;">FAC-0042</div>
              <div class="wk-fila" style="padding:0;border:none;font-size:11px;color:var(--texto-dim);gap:6px;margin-top:2px;">
                Carlos Ruiz · 14/05/2026
              </div>
            </div>
            <span class="wk-badge emitida">EMITIDA</span>
            <span style="font-family:var(--font-mono);font-size:13px;color:var(--texto);">$1.250</span>
          </div>
          <div class="wk-fila">
            <div style="flex:1;">
              <div style="font-size:13px;font-weight:500;">FAC-0041</div>
              <div style="font-size:11px;color:var(--texto-dim);margin-top:2px;">María López · 12/05/2026</div>
            </div>
            <span class="wk-badge pendiente">PENDIENTE</span>
            <span style="font-family:var(--font-mono);font-size:13px;color:var(--texto);">$890</span>
          </div>
          <div class="wk-fila">
            <div style="flex:1;">
              <div style="font-size:13px;font-weight:500;">FAC-0040</div>
              <div style="font-size:11px;color:var(--texto-dim);margin-top:2px;">J. Distribuidora · 10/05/2026</div>
            </div>
            <span class="wk-badge emitida">EMITIDA</span>
            <span style="font-family:var(--font-mono);font-size:13px;color:var(--texto);">$3.410</span>
          </div>
        </div>
        <button class="wk-fab pulso" id="fab-nueva">＋ Nueva Factura</button>
      </div>`;
}

// Paso 3 — Formulario, campo cliente con tipeo animado
function renderPantallaBuscarCliente() {
    renderNavSimulada('facturas');
    const outer = document.getElementById('wk-cuerpo-outer');
    if (!outer) return;
    outer.innerHTML = `
      <div class="wk-cuerpo">
        <div style="display:flex;align-items:center;gap:8px;margin-bottom:14px;">
          <span style="font-size:11px;color:var(--texto-dim);">◄ Volver</span>
          <span style="font-size:14px;font-weight:700;color:var(--texto);">Nueva Factura</span>
        </div>
        <div class="wk-campo">
          <label>Cliente</label>
          <input id="inp-cliente" placeholder="Buscar cliente…" value="">
        </div>
        <div class="wk-dropdown" id="dropdown-cliente"></div>
        <div class="wk-campo" style="margin-top:12px;">
          <label>Fecha</label>
          <input value="15/05/2026">
        </div>
      </div>`;

    // Animar tipeo
    const inp = document.getElementById('inp-cliente');
    if (!inp) return;
    const texto = 'Juan García';
    let idx = 0;
    setTimeout(() => inp.classList.add('enfocado'), 300);
    const t = setInterval(() => {
        if (!document.getElementById('inp-cliente')) { clearInterval(t); return; }
        if (idx < texto.length) {
            inp.value += texto[idx++];
        } else {
            clearInterval(t);
            // Mostrar dropdown
            setTimeout(() => {
                const dd = document.getElementById('dropdown-cliente');
                if (!dd) return;
                dd.innerHTML = `
                  <div class="wk-dropdown-item">
                    Juan García Pérez
                    <div class="dd-sub">RIF: J-123456789</div>
                  </div>
                  <div class="wk-dropdown-item">
                    Juan García López
                    <div class="dd-sub">RIF: V-88654321</div>
                  </div>`;
                dd.classList.add('visible');
            }, 300);
        }
    }, 110);
}

// Paso 4 — Cliente seleccionado
function renderPantallaSeleccionarCliente() {
    renderNavSimulada('facturas');
    const outer = document.getElementById('wk-cuerpo-outer');
    if (!outer) return;
    outer.innerHTML = `
      <div class="wk-cuerpo">
        <div style="display:flex;align-items:center;gap:8px;margin-bottom:14px;">
          <span style="font-size:11px;color:var(--texto-dim);">◄ Volver</span>
          <span style="font-size:14px;font-weight:700;color:var(--texto);">Nueva Factura</span>
        </div>
        <div style="background:rgba(0,180,216,.08);border:1px solid rgba(0,180,216,.3);border-radius:var(--radio);padding:10px 12px;margin-bottom:14px;display:flex;align-items:center;gap:10px;">
          <span style="font-size:20px;">◎</span>
          <div>
            <div style="font-size:13px;font-weight:600;color:var(--texto);">Juan García Pérez</div>
            <div style="font-size:11px;color:var(--texto-dim);">RIF: J-123456789 · Crédito 30 días</div>
          </div>
          <span style="margin-left:auto;color:var(--verde);font-size:16px;">✓</span>
        </div>
        <div class="wk-campo">
          <label>Fecha</label>
          <input value="15/05/2026">
        </div>
        <div style="margin-top:12px;padding:10px 12px;background:var(--fondo-inp);border:1px solid var(--borde);border-radius:var(--radio);font-size:12px;color:var(--texto-dim);">
          Sin ítems aún — toca <strong style="color:var(--cyan);">+ Agregar ítem</strong> para continuar
        </div>
      </div>`;
}

// Paso 5 — Agregar ítems con animación
function renderPantallaAgregarItems() {
    renderNavSimulada('facturas');
    const outer = document.getElementById('wk-cuerpo-outer');
    if (!outer) return;
    outer.innerHTML = `
      <div class="wk-cuerpo">
        <div style="font-size:13px;font-weight:600;color:var(--texto-dim);text-transform:uppercase;letter-spacing:.05em;margin-bottom:10px;">
          Ítems de la factura
        </div>
        <div class="wk-item-linea" id="il-1">
          <div class="il-desc">Bolsas polietileno 50×70 cm</div>
          <div class="il-meta">
            <span>10 u. × $120,00</span>
            <span class="il-sub">$1.200,00</span>
          </div>
        </div>
        <div class="wk-item-linea" id="il-2">
          <div class="il-desc">Bolsas polietileno 40×60 cm</div>
          <div class="il-meta">
            <span>5 u. × $90,00</span>
            <span class="il-sub">$450,00</span>
          </div>
        </div>
        <div style="text-align:center;margin-top:8px;">
          <span style="font-size:12px;color:var(--cyan);cursor:default;">＋ Agregar otro ítem</span>
        </div>
      </div>`;

    // Animar aparición de ítems
    setTimeout(() => {
        const il1 = document.getElementById('il-1');
        if (il1) il1.classList.add('visible');
    }, 300);
    setTimeout(() => {
        const il2 = document.getElementById('il-2');
        if (il2) il2.classList.add('visible');
    }, 900);
}

// Paso 6 — Totales
function renderPantallaTotales() {
    renderNavSimulada('facturas');
    const outer = document.getElementById('wk-cuerpo-outer');
    if (!outer) return;
    outer.innerHTML = `
      <div class="wk-cuerpo">
        <div class="wk-item-linea visible">
          <div class="il-desc">Bolsas polietileno 50×70 cm</div>
          <div class="il-meta"><span>10 u. × $120,00</span><span class="il-sub">$1.200,00</span></div>
        </div>
        <div class="wk-item-linea visible">
          <div class="il-desc">Bolsas polietileno 40×60 cm</div>
          <div class="il-meta"><span>5 u. × $90,00</span><span class="il-sub">$450,00</span></div>
        </div>
        <div class="wk-totales" id="totales-wrap" style="opacity:0;transition:opacity .5s;">
          <div class="wk-total-fila">
            <span class="tf-label">Subtotal</span>
            <span>$1.650,00</span>
          </div>
          <div class="wk-total-fila">
            <span class="tf-label">IVA (16%)</span>
            <span>$264,00</span>
          </div>
          <div class="wk-total-fila total-final">
            <span class="tf-label" style="font-size:14px;">TOTAL</span>
            <span>$1.914,00</span>
          </div>
        </div>
        <div id="btn-guardar-wrap" style="opacity:0;transition:opacity .5s;margin-top:12px;">
          <div style="background:var(--azul);color:#fff;text-align:center;padding:11px;border-radius:var(--radio);font-weight:600;font-size:14px;animation:pulso-fab 1.2s ease-in-out infinite;">
            💾 Guardar Factura
          </div>
        </div>
      </div>`;

    setTimeout(() => {
        const t = document.getElementById('totales-wrap');
        if (t) t.style.opacity = '1';
    }, 500);
    setTimeout(() => {
        const b = document.getElementById('btn-guardar-wrap');
        if (b) b.style.opacity = '1';
    }, 1200);
}

// Paso 7 — Factura guardada con número asignado
function renderPantallaGuardar() {
    renderNavSimulada('facturas');
    const outer = document.getElementById('wk-cuerpo-outer');
    if (!outer) return;
    outer.innerHTML = `
      <div class="wk-cuerpo" style="display:flex;flex-direction:column;align-items:center;justify-content:center;padding:24px 20px;text-align:center;">
        <div id="success-icono" style="width:58px;height:58px;border-radius:50%;background:rgba(63,185,80,.15);border:2px solid var(--verde);display:flex;align-items:center;justify-content:center;font-size:26px;margin-bottom:14px;opacity:0;transition:opacity .5s;">✓</div>
        <div style="font-size:16px;font-weight:700;color:var(--texto);margin-bottom:4px;">Factura guardada</div>
        <div style="font-size:22px;font-weight:700;color:var(--cyan);font-family:var(--font-mono);margin-bottom:8px;" id="num-fac" style="opacity:0;">FAC-0043</div>
        <div style="font-size:12px;color:var(--texto-dim);">Juan García Pérez · $1.914,00</div>
        <div style="margin-top:14px;display:flex;gap:8px;">
          <div style="background:var(--fondo-inp);border:1px solid var(--borde);border-radius:var(--radio);padding:9px 14px;font-size:13px;color:var(--texto-dim);">Ver detalle</div>
          <div style="background:var(--azul);border-radius:var(--radio);padding:9px 14px;font-size:13px;color:#fff;font-weight:600;">🖨 Imprimir PDF</div>
        </div>
      </div>`;

    setTimeout(() => {
        const ic = document.getElementById('success-icono');
        if (ic) ic.style.opacity = '1';
    }, 300);
}

// Paso 8 — Imprimir PDF resaltado
function renderPantallaImprimir() {
    renderNavSimulada('facturas');
    const outer = document.getElementById('wk-cuerpo-outer');
    if (!outer) return;
    outer.innerHTML = `
      <div class="wk-cuerpo">
        <div style="font-size:11px;font-weight:600;color:var(--texto-dim);text-transform:uppercase;letter-spacing:.06em;margin-bottom:12px;">FAC-0043 · Detalle</div>
        <div style="background:var(--fondo-inp);border:1px solid var(--borde);border-radius:var(--radio);padding:12px;margin-bottom:10px;">
          <div style="font-size:13px;color:var(--texto-dim);margin-bottom:4px;">Cliente</div>
          <div style="font-size:14px;font-weight:600;color:var(--texto);">Juan García Pérez</div>
          <div style="font-size:11px;color:var(--texto-dim);margin-top:2px;">RIF J-123456789 · 15/05/2026</div>
        </div>
        <div style="background:var(--fondo-inp);border:1px solid var(--borde);border-radius:var(--radio);overflow:hidden;margin-bottom:12px;">
          <div class="wk-total-fila"><span class="tf-label">Bolsas 50×70 (×10)</span><span>$1.200,00</span></div>
          <div class="wk-total-fila"><span class="tf-label">Bolsas 40×60 (×5)</span><span>$450,00</span></div>
          <div class="wk-total-fila"><span class="tf-label">IVA 16%</span><span>$264,00</span></div>
          <div class="wk-total-fila total-final"><span class="tf-label" style="font-size:14px;">TOTAL</span><span>$1.914,00</span></div>
        </div>
        <div id="btn-imprimir" style="background:var(--azul);color:#fff;text-align:center;padding:12px;border-radius:var(--radio);font-weight:600;font-size:14px;opacity:0;transition:opacity .5s;animation:pulso-fab 1.2s ease-in-out infinite;">
          🖨 Imprimir PDF
        </div>
        <div style="text-align:center;font-size:11px;color:var(--texto-dim);margin-top:8px;">
          En móvil: compartí el PDF por WhatsApp o email directamente
        </div>
      </div>`;

    setTimeout(() => {
        const b = document.getElementById('btn-imprimir');
        if (b) b.style.opacity = '1';
    }, 600);
}

// ── Punto de entrada ──────────────────────────────────────────────
if (!Auth.requireAuth()) { /* redirige al login */ }
else render();
