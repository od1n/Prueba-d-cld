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
            rol:            data.rol
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
        throw new Error(err.error || `Error ${res.status}`);
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
        { id: 'contenido',  icon: '📣', label: 'Redes',      href: '/pages/contenido.html' },
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
