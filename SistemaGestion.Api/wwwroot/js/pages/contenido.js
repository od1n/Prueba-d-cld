// ═══════════════════════════════════════════════════════════════
//  contenido.js — Estudio de Contenido (Fase 1)
//  Selector de marca · Banco de ideas · Lista de posts (borradores)
//  La generación con IA y el editor completo llegan en fases 2–3.
// ═══════════════════════════════════════════════════════════════
if (!Auth.requireAuth()) { /* redirige */ }

const app = document.getElementById('app');

const State = {
    marcas: [],
    marcaId: null,
    tab: 'ideas',   // 'ideas' | 'posts'
};

const PILARES = [
    { v: 'producto',   t: 'Producto / catálogo' },
    { v: 'detras',     t: 'Detrás de cámara' },
    { v: 'educativo',  t: 'Educativo / tips' },
    { v: 'testimonio', t: 'Testimonio / caso' },
    { v: 'historia',   t: 'Historia / marca' },
    { v: 'estacional', t: 'Estacional / efeméride' },
];

const ESTADO_LABEL = {
    generando: 'Generando', borrador: 'Borrador', en_revision: 'En revisión',
    aprobado: 'Aprobado', programado: 'Programado', publicado: 'Publicado',
    error: 'Error', archivado: 'Archivado',
};

// ── Arranque ────────────────────────────────────────────────────────────
async function render() {
    app.innerHTML = `
        ${renderTopbar('Estudio de Contenido')}
        <div class="content" id="content">
            <div class="loading-msg"><div class="spinner"></div></div>
        </div>
        ${renderNav('contenido')}`;
    try {
        State.marcas = await apiGet('/api/contenido/marcas') || [];
        if (State.marcas.length && !State.marcaId)
            State.marcaId = State.marcas[0].id;
        renderContenido();
    } catch (err) {
        document.getElementById('content').innerHTML =
            `<div class="msg show error">${esh(err.message)}</div>`;
    }
}

function renderContenido() {
    const content = document.getElementById('content');
    const opciones = State.marcas.map(m =>
        `<option value="${m.id}" ${m.id === State.marcaId ? 'selected' : ''}>${esh(m.nombre)}</option>`
    ).join('');

    content.innerHTML = `
        <div class="flex-between mb-8" style="gap:8px;flex-wrap:wrap">
            <div style="display:flex;align-items:center;gap:8px">
                <label class="text-dim text-sm" for="selMarca">Marca:</label>
                <select id="selMarca" class="input-field" style="width:auto"
                        onchange="cambiarMarca(this.value)">${opciones}</select>
            </div>
            <div class="seg">
                <button class="seg-btn ${State.tab === 'ideas' ? 'active' : ''}"
                        onclick="cambiarTab('ideas')">💡 Ideas</button>
                <button class="seg-btn ${State.tab === 'posts' ? 'active' : ''}"
                        onclick="cambiarTab('posts')">🗂️ Posts</button>
            </div>
        </div>
        <div id="tabContent"><div class="loading-msg"><div class="spinner"></div></div></div>
        <div class="modal-overlay" id="modalIdea"></div>`;

    if (State.tab === 'ideas') cargarIdeas();
    else cargarPosts();
}

window.cambiarMarca = (v) => { State.marcaId = parseInt(v); renderContenido(); };
window.cambiarTab   = (t) => { State.tab = t; renderContenido(); };

// ── IDEAS ────────────────────────────────────────────────────────────────
async function cargarIdeas() {
    const cont = document.getElementById('tabContent');
    try {
        const ideas = await apiGet(`/api/contenido/ideas?marca=${State.marcaId}`) || [];
        const porEstado = { nueva: [], en_proceso: [], descartada: [] };
        ideas.forEach(i => (porEstado[i.estado] || porEstado.nueva).push(i));

        cont.innerHTML = `
            <div class="flex-between mb-8">
                <span class="text-dim text-sm">${ideas.length} idea(s)</span>
                <div style="display:flex;gap:8px">
                    <button class="btn btn-sm" title="Próximamente (Fase 2)" disabled>✨ Generar con IA</button>
                    <button class="btn btn-cyan btn-sm" onclick="abrirModalIdea()">+ Idea</button>
                </div>
            </div>
            <div class="kanban">
                ${columnaIdeas('Nuevas', porEstado.nueva)}
                ${columnaIdeas('En proceso', porEstado.en_proceso)}
                ${columnaIdeas('Descartadas', porEstado.descartada)}
            </div>`;
    } catch (err) {
        cont.innerHTML = `<div class="msg show error">${esh(err.message)}</div>`;
    }
}

function columnaIdeas(titulo, items) {
    const cards = items.length ? items.map(tarjetaIdea).join('')
        : `<p class="text-dim text-sm" style="padding:8px">—</p>`;
    return `
        <div class="kanban-col">
            <h4 class="kanban-title">${esh(titulo)} <span class="text-dim">(${items.length})</span></h4>
            ${cards}
        </div>`;
}

function tarjetaIdea(i) {
    const pilar = PILARES.find(p => p.v === i.pilar)?.t || i.pilar || '';
    const origen = i.origen === 'ia' ? '🤖 IA' : i.origen === 'base' ? '📎 base' : '✍️ manual';
    return `
    <div class="card-mini">
        <strong>${esh(i.titulo)}</strong>
        ${i.descripcion ? `<p class="text-dim text-sm">${esh(i.descripcion)}</p>` : ''}
        <div class="flex-between text-sm text-dim" style="margin-top:6px">
            <span>${esh(pilar)}</span><span>${origen}</span>
        </div>
        <div style="display:flex;gap:6px;margin-top:8px">
            ${i.estado !== 'descartada'
                ? `<button class="btn btn-sm btn-cyan" onclick="crearPostDesdeIdea(${i.id})">→ Crear post</button>
                   <button class="btn btn-sm" onclick="descartarIdea(${i.id})">Descartar</button>`
                : `<button class="btn btn-sm" onclick="reactivarIdea(${i.id})">Reactivar</button>`}
        </div>
    </div>`;
}

window.abrirModalIdea = () => {
    const modal = document.getElementById('modalIdea');
    const pilarOpts = PILARES.map(p => `<option value="${p.v}">${esh(p.t)}</option>`).join('');
    modal.innerHTML = `
        <div class="modal-sheet">
            <div class="flex-between mb-8">
                <strong>Nueva idea</strong>
                <button class="btn-close" onclick="closeModal('modalIdea')">✕</button>
            </div>
            <div class="msg" id="msgIdea"></div>
            <div class="input-group">
                <label class="input-label" for="ideaTitulo">Título</label>
                <input class="input-field" id="ideaTitulo" placeholder="Ej: Cómo elegir el fuste correcto">
            </div>
            <div class="input-group">
                <label class="input-label" for="ideaDesc">Descripción (opcional)</label>
                <textarea class="input-field" id="ideaDesc" rows="3"></textarea>
            </div>
            <div class="input-group">
                <label class="input-label" for="ideaPilar">Pilar</label>
                <select class="input-field" id="ideaPilar">${pilarOpts}</select>
            </div>
            <button class="btn btn-cyan" id="btnGuardarIdea" onclick="guardarIdea()">Guardar idea</button>
        </div>`;
    openModal('modalIdea');
};

window.guardarIdea = async () => {
    const btn = document.getElementById('btnGuardarIdea');
    const titulo = document.getElementById('ideaTitulo').value.trim();
    if (!titulo) { showMsg('El título es requerido.', 'error', '#msgIdea'); return; }
    setLoading(btn, true);
    try {
        await apiPost('/api/contenido/ideas', {
            marcaId: State.marcaId,
            titulo,
            descripcion: document.getElementById('ideaDesc').value.trim(),
            pilar: document.getElementById('ideaPilar').value,
            origen: 'manual',
        });
        closeModal('modalIdea');
        cargarIdeas();
    } catch (err) {
        showMsg(err.message, 'error', '#msgIdea');
    } finally { setLoading(btn, false); }
};

window.descartarIdea = async (id) => {
    try { await apiPost(`/api/contenido/ideas/${id}`, { marcaId: State.marcaId, estado: 'descartada' }, 'PUT'); cargarIdeas(); }
    catch (err) { showMsg(err.message, 'error'); }
};
window.reactivarIdea = async (id) => {
    try { await apiPost(`/api/contenido/ideas/${id}`, { marcaId: State.marcaId, estado: 'nueva' }, 'PUT'); cargarIdeas(); }
    catch (err) { showMsg(err.message, 'error'); }
};

window.crearPostDesdeIdea = async (ideaId) => {
    try {
        const res = await apiPost('/api/contenido/posts', { marcaId: State.marcaId, ideaId, formato: 'imagen' });
        showMsg('Borrador creado (id ' + res.id + '). El editor completo llega en la Fase 3.', 'ok');
        State.tab = 'posts';
        renderContenido();
    } catch (err) { showMsg(err.message, 'error'); }
};

// ── POSTS ──────────────────────────────────────────────────────────────
async function cargarPosts() {
    const cont = document.getElementById('tabContent');
    try {
        const posts = await apiGet(`/api/contenido/posts?marca=${State.marcaId}`) || [];
        if (!posts.length) {
            cont.innerHTML = `<div class="empty-state"><span style="font-size:48px">🗂️</span>
                <p>Todavía no hay posts. Crea uno desde una idea.</p></div>`;
            return;
        }
        cont.innerHTML = `
            <div class="mb-8"><span class="text-dim text-sm">${posts.length} post(s)</span></div>
            <div class="lista">${posts.map(tarjetaPost).join('')}</div>`;
    } catch (err) {
        cont.innerHTML = `<div class="msg show error">${esh(err.message)}</div>`;
    }
}

function tarjetaPost(p) {
    return `
    <div class="card-mini">
        <div class="flex-between">
            <strong>${esh(p.titulo || '(sin título)')}</strong>
            ${badgeEstado(p.estado)}
        </div>
        <div class="text-dim text-sm" style="margin-top:4px">
            ${esh(p.formato)} · ${esh(p.plataformasDestino)} · ${p.numAssets} asset(s)
            ${p.fechaProgramada ? ' · 📅 ' + esh(p.fechaProgramada) : ''}
        </div>
    </div>`;
}

function badgeEstado(e) {
    return `<span class="badge badge-${esh(e)}">${esh(ESTADO_LABEL[e] || e)}</span>`;
}

render();
