// ═══════════════════════════════════════════════════════════════
//  ayuda-empleados.js — Walkthrough animado: Empleados y Nómina
//  Cargar 3 empleados + procesar pago de semana de trabajo
// ═══════════════════════════════════════════════════════════════

// ── Datos de ejemplo ──────────────────────────────────────────────
const EMPLEADOS = [
    { ini: 'JP', nombre: 'Juan Pérez',    cargo: 'Operario',  salario: '$1.500,00' },
    { ini: 'ML', nombre: 'María López',   cargo: 'Supervisora', salario: '$2.200,00' },
    { ini: 'CG', nombre: 'Carlos García', cargo: 'Operario',  salario: '$1.500,00' },
];

// ── Pasos del walkthrough ─────────────────────────────────────────
// tipo: 'real' → screenshot real del escritorio con highlight
// tipo: 'css'  → simulación CSS animada
const PASOS = [
    {
        tipo:        'real',
        titulo:      'Abrir el módulo Nómina',
        descripcion: 'En el menú principal, haz clic en la tarjeta <strong>Nómina</strong> (segunda fila, centro).',
        img:         'menu-principal.png',
        dimLeft:     '0%',
        // Tarjeta Nómina: fila inferior, columna central
        // Calibrado sobre captura 1280×720 del 15/05/2026
        hl: [{ t: '53.2%', l: '33.6%', w: '33%', h: '9.6%', txt: 'Clic → Nómina' }],
        duracion:    3000
    },
    {
        tipo:        'real',
        titulo:      'Seleccionar Empleados',
        descripcion: 'En el diálogo de Nómina, haz clic en <strong>Empleados</strong> para ver y gestionar la plantilla.',
        img:         'nomina-menu.png',
        dimLeft:     '13%',   // atenuar iconos del escritorio (izquierda ~13%)
        // Diálogo "Módulo de Nómina" con 4 opciones (2×2 grid)
        // Empleados es la opción superior izquierda
        hl: [{ t: '23%', l: '20%', w: '8.5%', h: '15%', txt: 'Clic → Empleados' }],
        duracion:    2800
    },
    {
        tipo:        'css',
        titulo:      'Crear primer empleado',
        descripcion: 'Toca <strong>+ Nuevo Empleado</strong> y completa los datos de Juan Pérez.',
        pantalla:    renderPantallaForm1,
        duracion:    3500
    },
    {
        tipo:        'css',
        titulo:      'Juan Pérez guardado',
        descripcion: 'El sistema registra al empleado. Toca <strong>+ Nuevo Empleado</strong> para continuar.',
        pantalla:    renderPantallaLista1,
        duracion:    2200
    },
    {
        tipo:        'css',
        titulo:      'Agregar María López y Carlos García',
        descripcion: 'Repite el proceso para los otros dos empleados completando sus datos.',
        pantalla:    renderPantallaLista3,
        duracion:    2500
    },
    {
        tipo:        'css',
        titulo:      'Ir a Pago de Semana',
        descripcion: 'Con los 3 empleados cargados, toca <strong>Pago de Semana</strong> para procesar la nómina.',
        pantalla:    renderPantallaLiqMenu,
        duracion:    2200
    },
    {
        tipo:        'css',
        titulo:      'Definir el período de trabajo',
        descripcion: 'Selecciona las fechas de inicio y fin de la semana a liquidar.',
        pantalla:    renderPantallaLiqPeriodo,
        duracion:    2800
    },
    {
        tipo:        'css',
        titulo:      'Seleccionar los 3 empleados',
        descripcion: 'Marca los empleados que cobraron esta semana. Puedes seleccionarlos todos.',
        pantalla:    renderPantallaLiqSeleccion,
        duracion:    3000
    },
    {
        tipo:        'css',
        titulo:      'Revisar montos y confirmar',
        descripcion: 'El sistema calcula el pago proporcional de cada empleado. Toca <strong>Confirmar Pago</strong>.',
        pantalla:    renderPantallaLiqConfirmar,
        duracion:    3000
    },
    {
        tipo:        'css',
        titulo:      'Pago procesado',
        descripcion: 'El sistema registra el pago y genera el recibo imprimible de cada empleado.',
        pantalla:    renderPantallaLiqExito,
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
          <h1>Empleados y pago de semana</h1>
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
        const cls = i < pasoActual ? 'completo' : i === pasoActual ? 'activo' : '';
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

    wrap.innerHTML = `
      <div class="wk-pantalla" style="margin-bottom:20px;">
        <div class="wk-topbar">
          <span class="logo-mini">SG</span>
          <span class="modulo-label">SistemaGestion · Nómina</span>
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

function renderModuloTab(activo) {
    const tabs = ['Empleados', 'Pago de Semana', 'Historial'];
    const nav = document.getElementById('wk-nav-wrap');
    if (!nav) return;
    nav.innerHTML = `
      <div class="wk-nav">
        ${tabs.map(t => `<div class="wk-nav-item ${t === activo ? 'activo' : ''}">${t}</div>`).join('')}
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
        ? `<svg width="14" height="14" viewBox="0 0 24 24" fill="currentColor"><rect x="6" y="4" width="4" height="16"/><rect x="14" y="4" width="4" height="16"/></svg><span>Detener</span>`
        : `<svg width="14" height="14" viewBox="0 0 24 24" fill="currentColor"><polygon points="5,3 19,12 5,21"/></svg><span>Reproducir demo</span>`;
    if (modoAuto) setInfo('Reproduciendo automáticamente…');
}
function setInfo(txt) {
    const el = document.getElementById('auto-info');
    if (el) el.textContent = txt;
}

// ══════════════════════════════════════════════════════════════════
//  PANTALLAS SIMULADAS
// ══════════════════════════════════════════════════════════════════

// Paso 1 — Módulo Empleados (pantalla de entrada)
function renderPantallaModulo() {
    renderModuloTab('Empleados');
    const outer = document.getElementById('wk-cuerpo-outer');
    if (!outer) return;
    outer.innerHTML = `
      <div class="wk-cuerpo-rel">
        <div class="wk-searchbar">
          <input placeholder="🔍  Buscar empleado…">
          <button>Filtrar</button>
        </div>
        <div style="text-align:center;padding:30px 20px;color:var(--texto-dim);font-size:13px;">
          <div style="font-size:36px;margin-bottom:10px;">👥</div>
          No hay empleados registrados aún.<br>
          <span style="color:var(--cyan);">Toca + Nuevo Empleado para comenzar</span>
        </div>
        <button class="wk-fab pulso">＋ Nuevo Empleado</button>
      </div>`;
}

// Paso 2 — Formulario primer empleado con tipeo animado
function renderPantallaForm1() {
    renderModuloTab('Empleados');
    const outer = document.getElementById('wk-cuerpo-outer');
    if (!outer) return;
    outer.innerHTML = `
      <div class="wk-cuerpo">
        <div style="font-size:14px;font-weight:700;color:var(--texto);margin-bottom:14px;">Nuevo Empleado</div>
        <div class="wk-campo en-fila">
          <div>
            <label>Nombre</label>
            <input id="inp-nombre" value="">
          </div>
          <div>
            <label>Apellido</label>
            <input id="inp-apellido" value="">
          </div>
        </div>
        <div class="wk-campo en-fila">
          <div>
            <label>DNI / Cédula</label>
            <input id="inp-dni" value="">
          </div>
          <div>
            <label>Fecha ingreso</label>
            <input id="inp-fecha" value="">
          </div>
        </div>
        <div class="wk-campo en-fila">
          <div>
            <label>Cargo</label>
            <input id="inp-cargo" value="">
          </div>
          <div>
            <label>Salario base</label>
            <input id="inp-salario" value="">
          </div>
        </div>
        <div id="btn-guardar-emp" style="opacity:0;transition:opacity .4s;margin-top:12px;background:var(--azul);color:#fff;text-align:center;padding:10px;border-radius:var(--radio);font-weight:600;font-size:14px;">
          Guardar Empleado
        </div>
      </div>`;

    // Secuencia de tipeo
    const campos = [
        { id: 'inp-nombre',   txt: 'Juan',        delay: 300  },
        { id: 'inp-apellido', txt: 'Pérez',        delay: 900  },
        { id: 'inp-dni',      txt: 'V-12345678',   delay: 1500 },
        { id: 'inp-fecha',    txt: '01/03/2024',   delay: 2000 },
        { id: 'inp-cargo',    txt: 'Operario',     delay: 2500 },
        { id: 'inp-salario',  txt: '$1.500,00',    delay: 3000 },
    ];
    campos.forEach(({ id, txt, delay }) => {
        setTimeout(() => {
            const el = document.getElementById(id);
            if (!el) return;
            el.classList.add('enfocado');
            let i = 0;
            const t = setInterval(() => {
                if (!document.getElementById(id)) { clearInterval(t); return; }
                if (i < txt.length) el.value += txt[i++];
                else clearInterval(t);
            }, 50);
        }, delay);
    });
    setTimeout(() => {
        const b = document.getElementById('btn-guardar-emp');
        if (b) b.style.opacity = '1';
    }, 3200);
}

// Paso 3 — Lista con 1 empleado
function renderPantallaLista1() {
    renderModuloTab('Empleados');
    const outer = document.getElementById('wk-cuerpo-outer');
    if (!outer) return;
    outer.innerHTML = `
      <div class="wk-cuerpo-rel">
        <div class="wk-searchbar">
          <input placeholder="🔍  Buscar empleado…">
          <button>Filtrar</button>
        </div>
        <div style="background:var(--fondo-card);border:1px solid var(--borde);border-radius:var(--radio);overflow:hidden;margin-bottom:8px;">
          <div class="wk-empleado-item visible" id="emp-0">
            <div class="wk-empleado-avatar">JP</div>
            <div class="wk-empleado-info">
              <div class="wk-empleado-nombre">Juan Pérez</div>
              <div class="wk-empleado-dato">Operario · Desde 01/03/2024 · $1.500,00/sem</div>
            </div>
            <span class="wk-badge emitida" style="font-size:9px;">ACTIVO</span>
          </div>
        </div>
        <button class="wk-fab pulso">＋ Nuevo Empleado</button>
      </div>`;
}

// Paso 4 — Lista con 3 empleados
function renderPantallaLista3() {
    renderModuloTab('Empleados');
    const outer = document.getElementById('wk-cuerpo-outer');
    if (!outer) return;
    outer.innerHTML = `
      <div class="wk-cuerpo-rel">
        <div class="wk-searchbar">
          <input placeholder="🔍  Buscar empleado…">
          <button>Filtrar</button>
        </div>
        <div style="background:var(--fondo-card);border:1px solid var(--borde);border-radius:var(--radio);overflow:hidden;margin-bottom:8px;" id="lista-empleados">
          ${EMPLEADOS.map((e, i) => `
            <div class="wk-empleado-item" id="emp-${i}" style="border-bottom:${i < 2 ? '1px solid var(--borde)' : 'none'}">
              <div class="wk-empleado-avatar">${e.ini}</div>
              <div class="wk-empleado-info">
                <div class="wk-empleado-nombre">${e.nombre}</div>
                <div class="wk-empleado-dato">${e.cargo} · ${e.salario}/sem</div>
              </div>
              <span class="wk-badge emitida" style="font-size:9px;">ACTIVO</span>
            </div>`).join('')}
        </div>
        <button class="wk-fab">＋ Nuevo Empleado</button>
      </div>`;

    // Animar aparición secuencial
    EMPLEADOS.forEach((_, i) => {
        setTimeout(() => {
            const el = document.getElementById(`emp-${i}`);
            if (el) el.classList.add('visible');
        }, i * 350 + 200);
    });
}

// Paso 5 — Tab Pago de Semana resaltado
function renderPantallaLiqMenu() {
    renderModuloTab('Pago de Semana');
    const outer = document.getElementById('wk-cuerpo-outer');
    if (!outer) return;
    outer.innerHTML = `
      <div class="wk-cuerpo" style="display:flex;flex-direction:column;align-items:center;justify-content:center;padding:24px 20px;text-align:center;">
        <div style="font-size:34px;margin-bottom:10px;">💵</div>
        <div style="font-size:14px;font-weight:600;color:var(--texto);margin-bottom:6px;">Pago de Semana</div>
        <div style="font-size:12px;color:var(--texto-dim);margin-bottom:18px;">Liquidá la semana de trabajo para los empleados activos</div>
        <div style="background:var(--azul);color:#fff;padding:10px 24px;border-radius:22px;font-size:13px;font-weight:600;animation:pulso-fab 1.2s ease-in-out infinite;">
          ＋ Nueva Liquidación
        </div>
      </div>`;
}

// Paso 6 — Selección de período
function renderPantallaLiqPeriodo() {
    renderModuloTab('Pago de Semana');
    const outer = document.getElementById('wk-cuerpo-outer');
    if (!outer) return;
    outer.innerHTML = `
      <div class="wk-cuerpo">
        <div style="font-size:14px;font-weight:700;color:var(--texto);margin-bottom:14px;">Nueva Liquidación</div>
        <div style="background:var(--fondo-inp);border:1px solid var(--borde);border-radius:var(--radio);padding:12px;margin-bottom:12px;">
          <div style="font-size:11px;color:var(--texto-dim);text-transform:uppercase;letter-spacing:.05em;margin-bottom:8px;">Período de trabajo</div>
          <div class="wk-campo en-fila">
            <div>
              <label>Desde</label>
              <input id="inp-desde" value="">
            </div>
            <div>
              <label>Hasta</label>
              <input id="inp-hasta" value="">
            </div>
          </div>
          <div id="semana-label" style="font-size:12px;color:var(--cyan);text-align:center;margin-top:4px;opacity:0;transition:opacity .4s;">
            Semana: lunes 12 → domingo 18 de mayo 2026 (7 días)
          </div>
        </div>
        <div style="font-size:12px;color:var(--texto-dim);padding:8px 12px;background:var(--fondo-inp);border:1px solid var(--borde);border-radius:var(--radio);">
          Siguiente paso: seleccionar los empleados a liquidar →
        </div>
      </div>`;

    setTimeout(() => {
        const d = document.getElementById('inp-desde');
        if (d) { d.classList.add('enfocado'); tipoAnimar(d, '12/05/2026', 0); }
    }, 400);
    setTimeout(() => {
        const h = document.getElementById('inp-hasta');
        if (h) { h.classList.add('enfocado'); tipoAnimar(h, '18/05/2026', 0); }
    }, 1400);
    setTimeout(() => {
        const lb = document.getElementById('semana-label');
        if (lb) lb.style.opacity = '1';
    }, 2200);
}

// Paso 7 — Selección de empleados con checkboxes
function renderPantallaLiqSeleccion() {
    renderModuloTab('Pago de Semana');
    const outer = document.getElementById('wk-cuerpo-outer');
    if (!outer) return;
    outer.innerHTML = `
      <div class="wk-cuerpo">
        <div style="font-size:13px;font-weight:600;color:var(--texto-dim);text-transform:uppercase;letter-spacing:.05em;margin-bottom:10px;">Empleados · Semana 12–18 may</div>
        <div style="background:var(--fondo-card);border:1px solid var(--borde);border-radius:var(--radio);overflow:hidden;margin-bottom:12px;">
          ${EMPLEADOS.map((e, i) => `
            <div class="wk-empleado-item visible" id="chk-emp-${i}" style="border-bottom:${i < 2 ? '1px solid var(--borde)' : 'none'}">
              <div class="wk-empleado-avatar">${e.ini}</div>
              <div class="wk-empleado-info">
                <div class="wk-empleado-nombre">${e.nombre}</div>
                <div class="wk-empleado-dato">${e.cargo} · ${e.salario}</div>
              </div>
              <div class="wk-chk" id="chk-${i}"></div>
            </div>`).join('')}
        </div>
        <div style="font-size:11px;color:var(--texto-dim);text-align:center;">Toca cada empleado para seleccionarlo</div>
      </div>`;

    // Animar selección de checkboxes uno a uno
    EMPLEADOS.forEach((_, i) => {
        setTimeout(() => {
            const chk = document.getElementById(`chk-${i}`);
            const row = document.getElementById(`chk-emp-${i}`);
            if (chk) chk.classList.add('marcado');
            if (row) row.classList.add('marcado');
        }, i * 500 + 600);
    });
}

// Paso 8 — Montos calculados + confirmar
function renderPantallaLiqConfirmar() {
    renderModuloTab('Pago de Semana');
    const outer = document.getElementById('wk-cuerpo-outer');
    if (!outer) return;
    outer.innerHTML = `
      <div class="wk-cuerpo">
        <div style="font-size:13px;font-weight:600;color:var(--texto-dim);text-transform:uppercase;letter-spacing:.05em;margin-bottom:10px;">Resumen de pago</div>
        <div style="background:var(--fondo-card);border:1px solid var(--borde);border-radius:var(--radio);overflow:hidden;margin-bottom:12px;">
          ${EMPLEADOS.map((e, i) => `
            <div class="wk-pago-fila" id="pago-${i}" style="border-bottom:${i < 2 ? '1px solid var(--borde)' : 'none'}">
              <div>
                <div class="pf-emp">${e.nombre}</div>
                <div style="font-size:11px;color:var(--texto-dim);">${e.cargo} · 7 días</div>
              </div>
              <div class="pf-monto">${e.salario}</div>
            </div>`).join('')}
          <div class="wk-total-fila total-final" id="pago-total" style="opacity:0;transition:opacity .5s;">
            <span class="tf-label" style="font-size:13px;">TOTAL NÓMINA</span>
            <span>$5.200,00</span>
          </div>
        </div>
        <div id="btn-confirmar-pago" style="opacity:0;transition:opacity .5s;background:var(--verde);color:#fff;text-align:center;padding:12px;border-radius:var(--radio);font-weight:600;font-size:14px;animation:pulso-fab 1.2s ease-in-out infinite;">
          ✓ Confirmar Pago de Semana
        </div>
      </div>`;

    EMPLEADOS.forEach((_, i) => {
        setTimeout(() => {
            const el = document.getElementById(`pago-${i}`);
            if (el) el.classList.add('visible');
        }, i * 300 + 300);
    });
    setTimeout(() => {
        const t = document.getElementById('pago-total');
        if (t) t.style.opacity = '1';
    }, 1400);
    setTimeout(() => {
        const b = document.getElementById('btn-confirmar-pago');
        if (b) b.style.opacity = '1';
    }, 1900);
}

// Paso 9 — Pago exitoso con recibos
function renderPantallaLiqExito() {
    renderModuloTab('Historial');
    const outer = document.getElementById('wk-cuerpo-outer');
    if (!outer) return;
    outer.innerHTML = `
      <div class="wk-cuerpo" style="display:flex;flex-direction:column;align-items:center;justify-content:center;padding:20px;text-align:center;">
        <div id="ok-icono" style="width:58px;height:58px;border-radius:50%;background:rgba(63,185,80,.15);border:2px solid var(--verde);display:flex;align-items:center;justify-content:center;font-size:26px;margin-bottom:14px;opacity:0;transition:opacity .5s;">✓</div>
        <div style="font-size:15px;font-weight:700;color:var(--texto);margin-bottom:4px;">Pago procesado</div>
        <div style="font-size:12px;color:var(--texto-dim);margin-bottom:14px;">Semana 12–18 mayo 2026 · $5.200,00</div>
        <div style="width:100%;background:var(--fondo-card);border:1px solid var(--borde);border-radius:var(--radio);overflow:hidden;">
          ${EMPLEADOS.map((e, i) => `
            <div class="wk-pago-fila visible" style="border-bottom:${i < 2 ? '1px solid var(--borde)' : 'none'}">
              <div class="pf-emp">${e.nombre}</div>
              <div style="display:flex;align-items:center;gap:8px;">
                <span class="pf-monto">${e.salario}</span>
                <span style="font-size:11px;color:var(--cyan);cursor:default;">🖨 Recibo</span>
              </div>
            </div>`).join('')}
        </div>
      </div>`;

    setTimeout(() => {
        const ic = document.getElementById('ok-icono');
        if (ic) ic.style.opacity = '1';
    }, 300);
}

// ── Helper: animar tipeo ──────────────────────────────────────────
function tipoAnimar(el, texto, delay) {
    let i = 0;
    const t = setInterval(() => {
        if (!el.parentNode) { clearInterval(t); return; }
        if (i < texto.length) el.value += texto[i++];
        else clearInterval(t);
    }, 60);
}

// ── Punto de entrada ──────────────────────────────────────────────
if (!Auth.requireAuth()) { /* redirige al login */ }
else render();
