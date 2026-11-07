// ═══════════════════════════════════════════════════════════════
//  SistemaGestion Portal Móvil — app.js
//  Maneja: auth, fetch con token, navegación entre páginas
// ═══════════════════════════════════════════════════════════════

const API = '';  // mismo origen — la API sirve el HTML

// ── Sanitización XSS ────────────────────────────────────────────────────
//   Cualquier dato proveniente del backend debe pasar por esh() antes de
//   inyectarse via innerHTML. Sólo se omite cuando el valor es numérico
//   formateado por funciones controladas (formatUSD, formatNum, formatFecha).
function esh(v) {
    if (v === null || v === undefined) return '';
    return String(v)
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;')
        .replace(/'/g, '&#39;');
}
// Atajo para HTML attributes (mismo escape — los caracteres peligrosos
// para atributos son los mismos cuando van entre comillas dobles).
const eshA = esh;

// ── Auth ─────────────────────────────────────────────────────────────────
const Auth = {
    _user: null,

    async login(usuario, contrasena) {
        const res = await fetch(`${API}/api/login`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            credentials: 'include',
            body: JSON.stringify({ usuario, contrasena })
        });
        if (res.status === 429) {
            throw new Error('Demasiados intentos. Espera un minuto.');
        }
        if (!res.ok) {
            const err = await res.json().catch(() => ({}));
            throw new Error(err.error || 'Usuario o contraseña incorrectos.');
        }
        const data = await res.json();
        this._user = data;
        // ⚠ Sólo nombre/rol viven en sessionStorage (no el token).
        // Cierra al cerrar la pestaña → mejor higiene de sesión.
        sessionStorage.setItem('sg_user', JSON.stringify({
            nombreUsuario:  data.nombreUsuario,
            nombreCompleto: data.nombreCompleto,
            rol:            data.rol,
            esAdmin:        !!data.esAdmin
        }));
        return data;
    },

    async logout() {
        await fetch(`${API}/api/logout`, {
            method: 'POST', credentials: 'include'
        }).catch(() => {});
        sessionStorage.removeItem('sg_user');
        // Limpieza retro-compatible
        try { localStorage.removeItem('sg_user'); } catch {}
        this._user = null;
        window.location.href = '/index.html';
    },

    getUser() {
        if (this._user) return this._user;
        const stored = sessionStorage.getItem('sg_user')
                    || localStorage.getItem('sg_user'); // legacy
        if (stored) {
            try { this._user = JSON.parse(stored); return this._user; }
            catch { return null; }
        }
        return null;
    },

    isLoggedIn() { return !!this.getUser(); },

    requireAuth() {
        if (!this.isLoggedIn()) {
            window.location.href = '/index.html';
            return false;
        }
        return true;
    }
};

// ── Fetch con credenciales (cookie JWT automática) ────────────────────────
async function apiFetch(url, options = {}) {
    const res = await fetch(`${API}${url}`, {
        ...options,
        credentials: 'include',
        headers: {
            'Content-Type': 'application/json',
            ...(options.headers || {})
        }
    });

    if (res.status === 401) {
        // Token expirado — redirigir al login
        Auth.logout();
        return null;
    }

    if (res.status === 403) {
        showMsg('No tienes permiso para esta operación.', 'error');
        return null;
    }

    return res;
}

async function apiGet(url) {
    const res = await apiFetch(url);
    if (!res) return null;
    if (!res.ok) {
        const err = await res.json().catch(() => ({}));
        throw new Error(err.error || `Error ${res.status}`);
    }
    return res.json();
}

async function apiPost(url, body, method = 'POST') {
    const res = await apiFetch(url, {
        method,
        body: method === 'DELETE' ? undefined : JSON.stringify(body)
    });
    if (!res) return null;
    if (!res.ok) {
        const err = await res.json().catch(() => ({}));
        const e = new Error(err.error || `Error ${res.status}`);
        e.status = res.status; e.data = err;   // datos extra (p. ej. cliente ya existente)
        throw e;
    }
    try { return await res.json(); } catch { return {}; }
}

// ── UI helpers ────────────────────────────────────────────────────────────
function showMsg(texto, tipo = 'error', selector = '.msg') {
    const el = document.querySelector(selector);
    if (!el) return;
    el.textContent = texto;
    el.className = `msg show ${tipo}`;
    if (tipo === 'ok') setTimeout(() => el.classList.remove('show'), 3000);
}

function hideMsg(selector = '.msg') {
    const el = document.querySelector(selector);
    if (el) el.classList.remove('show');
}

function setLoading(btn, loading) {
    if (!btn) return;
    btn.disabled = loading;
    btn._originalText = btn._originalText || btn.textContent;
    btn.textContent = loading
        ? '...'
        : btn._originalText;
}

function formatUSD(n) {
    return '$' + Number(n || 0).toLocaleString('es-VE', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
}

function formatNum(n, dec = 0) {
    return Number(n || 0).toLocaleString('es-VE', { minimumFractionDigits: dec, maximumFractionDigits: dec });
}

function formatFecha(str) {
    if (!str) return '—';
    const d = new Date(str.includes('T') ? str : str + 'T00:00:00');
    return d.toLocaleDateString('es-VE', { day: '2-digit', month: '2-digit', year: '2-digit' });
}

function estaVencida(fechaVence) {
    if (!fechaVence) return false;
    const fv = new Date(fechaVence.includes('T') ? fechaVence : fechaVence + 'T00:00:00');
    return fv < new Date();
}

// ── Topbar + nav ─────────────────────────────────────────────────────────
function renderTopbar(titulo) {
    const user = Auth.getUser();
    const nom  = esh(user?.nombreCompleto || user?.nombreUsuario || '');
    const rol  = esh(user?.rol || '');
    return `
    <header class="topbar" role="banner">
        <span class="topbar-title">⚙ ${esh(titulo)}</span>
        <div class="topbar-user">
            <span>${nom}</span>
            <span class="topbar-rol">${rol}</span>
            <button class="topbar-logout"
                    onclick="Auth.logout()"
                    aria-label="Cerrar sesión"
                    title="Cerrar sesión">⏻</button>
        </div>
    </header>`;
}

function renderNav(activa) {
    const _rolNav = (Auth.getUser() || {}).rol || '';
    const items = [
        { id: 'dashboard',  icon: '◉',  label: 'Inicio',   href: '/pages/dashboard.html' },
        { id: 'inventario', icon: '▦',  label: 'Stock',    href: '/pages/inventario.html' },
        { id: 'kardex',     icon: '⇅',  label: 'Kardex',   href: '/pages/kardex.html' },
        { id: 'clientes',   icon: '◎',  label: 'Clientes', href: '/pages/clientes.html' },
        { id: 'facturas',   icon: '▤',  label: 'Facturas', href: '/pages/facturas.html' },
        { id: 'egresos',    icon: '⊟',  label: 'Egresos',  href: '/pages/egresos.html' },
        { id: 'compras',    icon: '🛒', label: 'Compras',  href: '/pages/compras.html' },
        { id: 'reportes',   icon: '📊', label: 'Reportes', href: '/pages/reportes.html' },
        { id: 'nomina',     icon: '💰', label: 'Nómina',   href: '/pages/nomina.html' },
        { id: 'produccion', icon: '🏭', label: 'Producción', href: '/pages/digitalizador.html' },
        { id: 'camaras',    icon: '📷', label: 'Cámaras',    href: '/pages/camaras.html' },
        { id: 'abonos',     icon: '💳', label: 'Abonos',     href: '/pages/abonos.html' },
        { id: 'cxp',        icon: '🧾', label: 'Por pagar',  href: '/pages/cxp.html' },
        { id: 'contenido',  icon: '📣', label: 'Redes',      href: '/pages/contenido.html' },
        { id: 'insights',   icon: '📈', label: 'Insights',   href: '/pages/insights.html' },
    ];
    return `
    <nav class="bottom-nav" role="navigation" aria-label="Navegación principal">
        ${items.filter(i => _rolNav !== 'Redes' || i.id === 'contenido').map(i => `
            <a href="${i.href}" class="nav-btn ${activa === i.id ? 'active' : ''}"
               ${activa === i.id ? 'aria-current="page"' : ''}>
                <span class="icon" aria-hidden="true">${i.icon}</span>
                <span>${i.label}</span>
            </a>`).join('')}
    </nav>`;
}

// ── Modal drawer helpers ───────────────────────────────────────────────────
function openModal(id) {
    document.getElementById(id)?.classList.add('open');
    document.body.style.overflow = 'hidden';
}

function closeModal(id) {
    document.getElementById(id)?.classList.remove('open');
    document.body.style.overflow = '';
}

// Cerrar modal al tocar el overlay
document.addEventListener('click', e => {
    if (e.target.classList.contains('modal-overlay')) {
        e.target.classList.remove('open');
        document.body.style.overflow = '';
    }
});

// Cerrar modal con tecla Escape (accesibilidad / WCAG 2.1.2)
document.addEventListener('keydown', e => {
    if (e.key === 'Escape') {
        const abierto = document.querySelector('.modal-overlay.open');
        if (abierto) {
            abierto.classList.remove('open');
            document.body.style.overflow = '';
        }
    }
});

// ── Kardex helpers ────────────────────────────────────────


// ── Rol "Redes": confinar la navegacion al area de contenido ─────────────
(function () {
    try {
        var u = Auth.getUser();
        if (u && u.rol === 'Redes') {
            var p = window.location.pathname;
            var permitido = p.endsWith('/contenido.html') || p.endsWith('/index.html') || p === '/';
            if (!permitido) window.location.replace('/pages/contenido.html');
        }
    } catch (e) {}
})();

// ════════════════════════════════════════════════════════════════════════
//  Búsqueda "type-ahead" en dropdowns largos (productos, clientes, moldes,
//  insumos, etc.): al escribir, filtra las opciones para no hacer scroll.
//  Envuelve el <select> nativo (que queda oculto como fuente de verdad, así
//  no se rompe el código que lee .value ni los listeners 'change').
//  Se aplica solo a los <select> con más de UMBRAL opciones y también a los
//  que se renderizan dinámicamente. Para excluir uno: <select data-nosearch>.
// ════════════════════════════════════════════════════════════════════════
(function () {
  const UMBRAL = 8;

  function inyectarEstilos() {
    if (document.getElementById('sg-search-style')) return;
    const st = document.createElement('style');
    st.id = 'sg-search-style';
    st.textContent =
      '.sg-search{position:relative;display:inline-block}' +
      '.sg-search>input.sg-search-input{width:100%;box-sizing:border-box;padding:6px 8px;' +
      'background:var(--fondo-inp,#fff);color:var(--texto,#111);border:1px solid var(--borde,#ccc);' +
      'border-radius:6px;font:inherit}' +
      '.sg-search-panel{position:absolute;z-index:9999;left:0;right:0;top:100%;max-height:260px;' +
      'overflow-y:auto;margin-top:2px;background:var(--fondo-inp,#fff);border:1px solid var(--borde,#ccc);' +
      'border-radius:6px;box-shadow:0 6px 20px rgba(0,0,0,.18)}' +
      '.sg-search-panel .it{padding:6px 9px;cursor:pointer;font-size:9.5pt;white-space:nowrap;' +
      'overflow:hidden;text-overflow:ellipsis}' +
      '.sg-search-panel .it:hover,.sg-search-panel .it.act{background:var(--cyan-dim,rgba(0,150,200,.18))}' +
      '.sg-search-panel .vacio{padding:8px 9px;color:var(--texto-dim,#888);font-size:9pt}';
    document.head.appendChild(st);
  }

  const textoDe = opt => (opt.textContent || '').trim();

  function enhanceSelect(sel) {
    if (!sel || sel.multiple || sel.dataset.sgsearch === '1' || sel.hasAttribute('data-nosearch')) return;
    if (sel.options.length <= UMBRAL) return;
    sel.dataset.sgsearch = '1';

    const wrap = document.createElement('span');
    wrap.className = 'sg-search';
    if (sel.offsetWidth) wrap.style.width = sel.offsetWidth + 'px';

    const input = document.createElement('input');
    input.type = 'text'; input.className = 'sg-search-input';
    input.autocomplete = 'off'; input.spellcheck = false; input.placeholder = 'Buscar…';

    const panel = document.createElement('div');
    panel.className = 'sg-search-panel'; panel.hidden = true;

    sel.parentNode.insertBefore(wrap, sel);
    wrap.appendChild(input); wrap.appendChild(panel); wrap.appendChild(sel);
    sel.style.display = 'none';

    const opcActual = () => sel.options[sel.selectedIndex] || null;
    const sincronizar = () => { const o = opcActual(); input.value = o ? textoDe(o) : ''; };
    sincronizar();

    let items = [], idx = -1;
    function construir(filtro) {
      const f = (filtro || '').toLowerCase();
      panel.innerHTML = ''; idx = -1; items = [];
      for (const opt of sel.options) {
        const t = textoDe(opt);
        if (f && !t.toLowerCase().includes(f)) continue;
        const div = document.createElement('div');
        div.className = 'it'; div.textContent = t || '(vacío)';
        div.addEventListener('mousedown', ev => { ev.preventDefault(); elegir(opt); });
        panel.appendChild(div); items.push(div);
      }
      if (!items.length) {
        const v = document.createElement('div'); v.className = 'vacio';
        v.textContent = 'Sin coincidencias'; panel.appendChild(v);
      }
    }
    const abrir  = () => { construir(''); panel.hidden = false; };
    const cerrar = () => { panel.hidden = true; idx = -1; };
    function elegir(opt) {
      sel.value = opt.value;
      sel.dispatchEvent(new Event('change', { bubbles: true }));
      input.value = textoDe(opt); cerrar();
    }

    input.addEventListener('focus', () => { input.select(); abrir(); });
    input.addEventListener('input', () => { construir(input.value); panel.hidden = false; });
    input.addEventListener('blur',  () => { setTimeout(() => { cerrar(); sincronizar(); }, 150); });
    input.addEventListener('keydown', e => {
      if (e.key === 'ArrowDown' || e.key === 'ArrowUp') {
        e.preventDefault();
        if (panel.hidden) { abrir(); return; }
        idx += (e.key === 'ArrowDown' ? 1 : -1);
        if (idx < 0) idx = items.length - 1;
        if (idx >= items.length) idx = 0;
        items.forEach((el, i) => el.classList.toggle('act', i === idx));
        if (items[idx]) items[idx].scrollIntoView({ block: 'nearest' });
      } else if (e.key === 'Enter') {
        if (!panel.hidden && idx >= 0 && items[idx]) { e.preventDefault(); items[idx].dispatchEvent(new MouseEvent('mousedown')); }
      } else if (e.key === 'Escape') { cerrar(); sincronizar(); }
    });
    sel.addEventListener('change', sincronizar);
  }

  function enhanceSelects(root) {
    inyectarEstilos();
    (root || document).querySelectorAll('select:not([data-sgsearch])').forEach(enhanceSelect);
  }
  window.enhanceSelects = enhanceSelects;

  let t = null;
  const obs = new MutationObserver(() => { clearTimeout(t); t = setTimeout(() => enhanceSelects(document), 120); });
  function init() { enhanceSelects(document); obs.observe(document.body, { childList: true, subtree: true }); }
  if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', init);
  else init();
})();
