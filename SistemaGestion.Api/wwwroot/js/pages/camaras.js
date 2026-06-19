// camaras.js — visor de cámaras RTSP vía snapshots proxy
if (!Auth.requireAuth()) { /* redirige */ }

const app = document.getElementById('app');
let refreshTimer = null;
let camaras = [];
let intervaloSeg = 3;  // refresco por defecto

async function render() {
    app.innerHTML = `
        ${renderTopbar('Cámaras')}
        <div class="content" id="content">
            <div class="loading-msg"><div class="spinner"></div></div>
        </div>
        ${renderNav('camaras')}`;
    await cargarCamaras();
}

async function cargarCamaras() {
    const content = document.getElementById('content');
    try {
        camaras = await apiGet('/api/camaras');
        if (!camaras) return;

        if (camaras.length === 0) {
            content.innerHTML = `
                <div class="empty-state">
                    <span style="font-size:48px">📷</span>
                    <p>No tienes cámaras asignadas.</p>
                </div>`;
            return;
        }

        content.innerHTML = `
            <div class="flex-between mb-8">
                <span class="text-dim text-sm">${camaras.length} cámara${camaras.length > 1 ? 's' : ''}</span>
                <div style="display:flex;align-items:center;gap:8px">
                    <label class="text-dim text-sm" for="selIntervalo">Refresco:</label>
                    <select id="selIntervalo" class="input-field"
                            style="width:auto;padding:4px 8px;font-size:12px"
                            onchange="cambiarIntervalo(this.value)">
                        <option value="2">2s</option>
                        <option value="3" selected>3s</option>
                        <option value="5">5s</option>
                        <option value="10">10s</option>
                        <option value="0">Pausar</option>
                    </select>
                </div>
            </div>
            <div class="cam-grid" id="camGrid">
                ${camaras.map(c => tarjetaCamara(c)).join('')}
            </div>`;

        // Primer snapshot inmediato para todas
        camaras.forEach(c => refrescarSnapshot(c.id));
        iniciarAutoRefresco();

    } catch (err) {
        content.innerHTML = `<div class="msg show error">${esh(err.message)}</div>`;
    }
}

function tarjetaCamara(cam) {
    return `
    <div class="cam-card" id="cam-${cam.id}" onclick="ampliar(${cam.id})">
        <div class="cam-img-wrap">
            <img class="cam-img" id="img-${cam.id}"
                 alt="${eshA(cam.nombre)}"
                 src="data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='320' height='180'%3E%3Crect fill='%23192a40' width='320' height='180'/%3E%3Ctext x='160' y='96' fill='%23446' font-size='14' text-anchor='middle'%3ECargando...%3C/text%3E%3C/svg%3E">
            <span class="cam-status" id="status-${cam.id}">●</span>
        </div>
        <div class="cam-info">
            <span class="cam-nombre">${esh(cam.nombre)}</span>
            <span class="cam-modelo text-dim text-sm">${esh(cam.modelo)}</span>
        </div>
    </div>`;
}

async function refrescarSnapshot(id) {
    const img = document.getElementById(`img-${id}`);
    const dot = document.getElementById(`status-${id}`);
    if (!img) return;

    try {
        const res = await fetch(`/api/camaras/${id}/snapshot`, { credentials: 'include' });
        if (!res.ok) throw new Error(res.status);

        const blob = await res.blob();
        const url  = URL.createObjectURL(blob);

        // Liberar URL anterior
        if (img._blobUrl) URL.revokeObjectURL(img._blobUrl);
        img._blobUrl = url;
        img.src = url;

        if (dot) { dot.style.color = 'var(--verde)'; dot.title = 'Conectada'; }
    } catch {
        if (dot) { dot.style.color = 'var(--rojo, #e44)'; dot.title = 'Sin conexión'; }
    }
}

function iniciarAutoRefresco() {
    detenerAutoRefresco();
    if (intervaloSeg <= 0) return;
    refreshTimer = setInterval(() => {
        camaras.forEach(c => refrescarSnapshot(c.id));
    }, intervaloSeg * 1000);
}

function detenerAutoRefresco() {
    if (refreshTimer) { clearInterval(refreshTimer); refreshTimer = null; }
}

function cambiarIntervalo(val) {
    intervaloSeg = parseInt(val) || 0;
    iniciarAutoRefresco();
}

// ── Vista ampliada ──────────────────────────────────────────────────────
function ampliar(id) {
    const cam = camaras.find(c => c.id === id);
    if (!cam) return;

    // Crear overlay
    const overlay = document.createElement('div');
    overlay.className = 'modal-overlay open';
    overlay.id = 'modalCam';
    overlay.innerHTML = `
        <div class="modal-sheet" style="max-width:95vw;max-height:95vh;padding:12px">
            <div class="flex-between mb-8">
                <strong>${esh(cam.nombre)}</strong>
                <button class="btn-close" onclick="cerrarAmpliar()"
                        aria-label="Cerrar">✕</button>
            </div>
            <img id="img-full-${id}"
                 style="width:100%;border-radius:8px;background:#111"
                 alt="${eshA(cam.nombre)}">
        </div>`;
    document.body.appendChild(overlay);
    document.body.style.overflow = 'hidden';

    // Snapshot en grande
    refrescarSnapshotFull(id);
    overlay._timer = setInterval(() => refrescarSnapshotFull(id),
        Math.max(intervaloSeg, 2) * 1000);

    // Cerrar al tocar overlay
    overlay.addEventListener('click', e => {
        if (e.target === overlay) cerrarAmpliar();
    });
}

async function refrescarSnapshotFull(id) {
    const img = document.getElementById(`img-full-${id}`);
    if (!img) return;
    try {
        const res = await fetch(`/api/camaras/${id}/snapshot`, { credentials: 'include' });
        if (!res.ok) return;
        const blob = await res.blob();
        const url  = URL.createObjectURL(blob);
        if (img._blobUrl) URL.revokeObjectURL(img._blobUrl);
        img._blobUrl = url;
        img.src = url;
    } catch {}
}

function cerrarAmpliar() {
    const modal = document.getElementById('modalCam');
    if (modal) {
        if (modal._timer) clearInterval(modal._timer);
        modal.remove();
    }
    document.body.style.overflow = '';
}

// Limpiar al salir de la página
window.addEventListener('beforeunload', detenerAutoRefresco);

render();
