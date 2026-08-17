// ═══════════════════════════════════════════════════════════════
//  contenido.js — Estudio de Contenido (Fase 2)
//  Selector de marca · Banco de ideas (+ generar IA) · Posts (+ generar IA)
//  Detalle de post: imagen + caption ES/EN + hashtags + aprobación
// ═══════════════════════════════════════════════════════════════
if (!Auth.requireAuth()) { /* redirige */ }

const app = document.getElementById('app');

const State = {
    marcas: [],
    marcaId: null,
    tab: 'ideas',
    pollActivo: false,
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
    generando: 'Generando…', borrador: 'Borrador', en_revision: 'En revisión',
    aprobado: 'Aprobado', programado: 'Programado', publicado: 'Publicado',
    error: 'Error', archivado: 'Archivado',
};

function esAdmin() { return ((Auth.getUser() || {}).rol) === 'Admin'; }

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
        if (State.marcas.length && !State.marcaId) State.marcaId = State.marcas[0].id;
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
                <button class="btn btn-sm" onclick="abrirReferencias()" title="Fotos reales del producto que la IA usa como base">📎 Referencias</button>
            </div>
            <div class="seg">
                <button class="seg-btn ${State.tab === 'ideas' ? 'active' : ''}" onclick="cambiarTab('ideas')">💡 Ideas</button>
                <button class="seg-btn ${State.tab === 'posts' ? 'active' : ''}" onclick="cambiarTab('posts')">🗂️ Posts</button>
            </div>
        </div>
        <div id="tabContent"><div class="loading-msg"><div class="spinner"></div></div></div>
        <div class="modal-overlay" id="modalIdea"></div>
        <div class="modal-overlay" id="modalGen"></div>
        <div class="modal-overlay" id="modalPost"></div>
        <div class="modal-overlay" id="modalRefs"></div>`;
    if (State.tab === 'ideas') cargarIdeas(); else cargarPosts();
}

window.cambiarMarca = (v) => { State.marcaId = parseInt(v); renderContenido(); };
window.cambiarTab   = (t) => { State.tab = t; renderContenido(); };

function marcaActual() { return State.marcas.find(m => m.id === State.marcaId) || {}; }

// ── IDEAS ────────────────────────────────────────────────────────────────
async function cargarIdeas() {
    const cont = document.getElementById('tabContent');
    try {
        const ideas = await apiGet(`/api/contenido/ideas?marca=${State.marcaId}`) || [];
        State.ideas = ideas;
        const porEstado = { nueva: [], en_proceso: [], descartada: [] };
        ideas.forEach(i => (porEstado[i.estado] || porEstado.nueva).push(i));
        cont.innerHTML = `
            <div class="flex-between mb-8">
                <span class="text-dim text-sm">${ideas.length} idea(s)</span>
                <div style="display:flex;gap:8px">
                    <button class="btn btn-sm" onclick="abrirGenerarIdeas()">✨ Generar con IA</button>
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
    return `<div class="kanban-col">
        <h4 class="kanban-title">${esh(titulo)} <span class="text-dim">(${items.length})</span></h4>
        ${cards}</div>`;
}

function tarjetaIdea(i) {
    const pilar = PILARES.find(p => p.v === i.pilar)?.t || i.pilar || '';
    const origen = i.origen === 'ia' ? '🤖 IA' : i.origen === 'base' ? '📎 base' : '✍️ manual';
    return `<div class="card-mini">
        <strong>${esh(i.titulo)}</strong>
        ${i.descripcion ? `<p class="text-dim text-sm">${esh(i.descripcion)}</p>` : ''}
        <div class="flex-between text-sm text-dim" style="margin-top:6px"><span>${esh(pilar)}</span><span>${origen}</span></div>
        <div style="display:flex;gap:6px;margin-top:8px">
            ${i.estado !== 'descartada'
                ? `<button class="btn btn-sm btn-cyan" onclick="crearPostDesdeIdea(${i.id})">✨ Generar post</button>
                   <button class="btn btn-sm" onclick="descartarIdea(${i.id})">Descartar</button>`
                : `<button class="btn btn-sm" onclick="reactivarIdea(${i.id})">Reactivar</button>`}
        </div></div>`;
}

window.abrirModalIdea = () => {
    const pilarOpts = PILARES.map(p => `<option value="${p.v}">${esh(p.t)}</option>`).join('');
    document.getElementById('modalIdea').innerHTML = `
        <div class="modal-sheet">
            <div class="flex-between mb-8"><strong>Nueva idea</strong>
                <button class="btn-close" onclick="closeModal('modalIdea')">✕</button></div>
            <div class="msg" id="msgIdea"></div>
            <div class="input-group"><label class="input-label" for="ideaTitulo">Título</label>
                <input class="input-field" id="ideaTitulo" placeholder="Ej: Cómo elegir el fuste correcto"></div>
            <div class="input-group"><label class="input-label" for="ideaDesc">Descripción (opcional)</label>
                <textarea class="input-field" id="ideaDesc" rows="3"></textarea></div>
            <div class="input-group"><label class="input-label" for="ideaPilar">Pilar</label>
                <select class="input-field" id="ideaPilar">${pilarOpts}</select></div>
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
            marcaId: State.marcaId, titulo,
            descripcion: document.getElementById('ideaDesc').value.trim(),
            pilar: document.getElementById('ideaPilar').value, origen: 'manual',
        });
        closeModal('modalIdea'); cargarIdeas();
    } catch (err) { showMsg(err.message, 'error', '#msgIdea'); }
    finally { setLoading(btn, false); }
};

// Generar ideas con IA
window.abrirGenerarIdeas = () => {
    const pilarOpts = PILARES.map(p => `<option value="${p.v}">${esh(p.t)}</option>`).join('');
    document.getElementById('modalGen').innerHTML = `
        <div class="modal-sheet">
            <div class="flex-between mb-8"><strong>✨ Generar ideas con IA — ${esh(marcaActual().nombre || '')}</strong>
                <button class="btn-close" onclick="closeModal('modalGen')">✕</button></div>
            <div class="msg" id="msgGen"></div>
            <div class="input-group"><label class="input-label" for="genPilar">Pilar (opcional)</label>
                <select class="input-field" id="genPilar"><option value="">Variados</option>${pilarOpts}</select></div>
            <div class="input-group"><label class="input-label" for="genCant">Cantidad</label>
                <input class="input-field" id="genCant" type="number" value="5" min="1" max="15"></div>
            <button class="btn btn-cyan" id="btnGen" onclick="generarIdeas()">Generar</button>
            <p class="text-dim text-sm" style="margin-top:8px">La IA propone las ideas y aparecen en unos segundos.</p>
        </div>`;
    openModal('modalGen');
};

window.generarIdeas = async () => {
    const btn = document.getElementById('btnGen');
    setLoading(btn, true);
    const pilar = document.getElementById('genPilar').value;
    const cantidad = parseInt(document.getElementById('genCant').value) || 5;
    try {
        await apiPost('/api/contenido/ideas/generar', {
            marcaId: State.marcaId, pilares: pilar ? [pilar] : [], cantidad, usarBase: true,
        });
        closeModal('modalGen');
        showMsg('Generando ideas con IA…', 'ok');
        // Sondear la lista unas cuantas veces
        let tries = 0;
        const iv = setInterval(async () => {
            tries++;
            if (State.tab === 'ideas') await cargarIdeas();
            if (tries >= 8) clearInterval(iv);
        }, 4000);
    } catch (err) { showMsg(err.message, 'error', '#msgGen'); }
    finally { setLoading(btn, false); }
};

window.descartarIdea = async (id) => {
    try { await apiPost(`/api/contenido/ideas/${id}`, { marcaId: State.marcaId, estado: 'descartada' }, 'PUT'); cargarIdeas(); }
    catch (err) { showMsg(err.message, 'error'); }
};
window.reactivarIdea = async (id) => {
    try { await apiPost(`/api/contenido/ideas/${id}`, { marcaId: State.marcaId, estado: 'nueva' }, 'PUT'); cargarIdeas(); }
    catch (err) { showMsg(err.message, 'error'); }
};

// Crear post desde idea Y disparar generación IA
window.crearPostDesdeIdea = async (ideaId) => {
    try {
        const idea = (State.ideas || []).find(i => i.id === ideaId);
        const res = await apiPost('/api/contenido/posts', { marcaId: State.marcaId, ideaId, titulo: idea ? idea.titulo : null, formato: 'imagen' });
        await apiPost(`/api/contenido/posts/${res.id}/generar`, {});
        showMsg('Generando post con IA… aparecerá en Posts.', 'ok');
        State.tab = 'posts'; renderContenido();
    } catch (err) { showMsg(err.message, 'error'); }
};

// ── POSTS ──────────────────────────────────────────────────────────────
async function cargarPosts() {
    const cont = document.getElementById('tabContent');
    try {
        const posts = await apiGet(`/api/contenido/posts?marca=${State.marcaId}`) || [];
        if (!posts.length) {
            cont.innerHTML = `<div class="empty-state"><span style="font-size:48px">🗂️</span>
                <p>Todavía no hay posts. Genera uno desde una idea.</p></div>`;
            return;
        }
        cont.innerHTML = `<div class="mb-8"><span class="text-dim text-sm">${posts.length} post(s)</span></div>
            <div class="lista">${posts.map(tarjetaPost).join('')}</div>`;
        // Sondeo automático mientras haya posts generándose
        const generando = posts.some(p => p.estado === 'generando');
        if (generando && !State.pollActivo) {
            State.pollActivo = true;
            setTimeout(() => { State.pollActivo = false; if (State.tab === 'posts') cargarPosts(); }, 5000);
        }
    } catch (err) {
        cont.innerHTML = `<div class="msg show error">${esh(err.message)}</div>`;
    }
}

function tarjetaPost(p) {
    const clickable = p.estado !== 'generando';
    return `<div class="card-mini" ${clickable ? `onclick="abrirPost(${p.id})" style="cursor:pointer"` : ''}>
        <div class="flex-between">
            <strong>${esh(p.titulo || '(sin título)')}</strong>
            ${badgeEstado(p.estado)}
        </div>
        <div class="text-dim text-sm" style="margin-top:4px">
            ${esh(p.formato)} · ${esh(p.plataformasDestino)} · ${p.numAssets} asset(s)
            ${p.fechaProgramada ? ' · 📅 ' + esh(p.fechaProgramada) : ''}
        </div>
        ${p.captionEs ? `<p class="text-dim text-sm" style="margin-top:4px">${esh((p.captionEs||'').slice(0,120))}${(p.captionEs||'').length>120?'…':''}</p>` : ''}
    </div>`;
}

function badgeEstado(e) { return `<span class="badge badge-${esh(e)}">${esh(ESTADO_LABEL[e] || e)}</span>`; }

// Detalle de post
window.abrirPost = async (id) => {
    const modal = document.getElementById('modalPost');
    modal.innerHTML = `<div class="modal-sheet"><div class="loading-msg"><div class="spinner"></div></div></div>`;
    openModal('modalPost');
    let data;
    try { data = await apiGet(`/api/contenido/posts/${id}`); }
    catch (err) { modal.innerHTML = `<div class="modal-sheet"><div class="msg show error">${esh(err.message)}</div></div>`; return; }
    const p = data.post, assets = data.assets || [];
    const conEn = (marcaActual().idiomas || 'es').indexOf('en') >= 0;
    const imgs = assets.map(a =>
        `<img src="/api/contenido/posts/${id}/asset/${a.id}" alt="asset"
              style="width:100%;border-radius:8px;background:#111;margin-bottom:8px">`).join('')
        || `<p class="text-dim text-sm">Sin imagen todavía.</p>`;
    const puedeAprobar = esAdmin();
    modal.innerHTML = `
      <div class="modal-sheet" style="max-width:640px">
        <div class="flex-between mb-8">
          <strong>${esh(p.titulo || 'Post')} ${badgeEstado(p.estado)}</strong>
          <button class="btn-close" onclick="closeModal('modalPost')">✕</button>
        </div>
        <div class="msg" id="msgPost"></div>
        ${p.mensajeError ? `<div class="msg show error">${esh(p.mensajeError)}</div>` : ''}
        ${imgs}
        <div class="input-group"><label class="input-label">Caption (ES)</label>
            <textarea class="input-field" id="pCapEs" rows="4">${esh(p.captionEs || '')}</textarea></div>
        ${conEn ? `<div class="input-group"><label class="input-label">Caption (EN)</label>
            <textarea class="input-field" id="pCapEn" rows="4">${esh(p.captionEn || '')}</textarea></div>` : ''}
        <div class="input-group"><label class="input-label">Hashtags</label>
            <input class="input-field" id="pHash" value="${eshA(p.hashtags || '')}"></div>
        <div class="input-group"><label class="input-label">Programar (opcional)</label>
            <input class="input-field" id="pFecha" type="datetime-local"></div>
        <div style="display:flex;flex-wrap:wrap;gap:8px;margin-top:8px">
          <button class="btn btn-sm" onclick="guardarPost(${id})">💾 Guardar</button>
          <button class="btn btn-sm" onclick="regenerarPost(${id})">🎲 Regenerar</button>
          <button class="btn btn-sm btn-cyan" onclick="enviarRevision(${id})">Enviar a revisión →</button>
          ${puedeAprobar ? `<button class="btn btn-sm" style="background:#16643a;color:#fff" onclick="aprobarPost(${id})">✔ Aprobar</button>
                            <button class="btn btn-sm" onclick="rechazarPost(${id})">✖ Rechazar</button>` : ''}
        </div>
      </div>`;
};

function recolectarPost() {
    const capEn = document.getElementById('pCapEn');
    const fecha = document.getElementById('pFecha').value;
    return {
        captionEs: document.getElementById('pCapEs').value,
        captionEn: capEn ? capEn.value : null,
        hashtags: document.getElementById('pHash').value,
        fechaProgramada: fecha ? fecha.replace('T', ' ') + ':00' : null,
    };
}

window.guardarPost = async (id) => {
    try { await apiPost(`/api/contenido/posts/${id}`, recolectarPost(), 'PUT');
        showMsg('Guardado.', 'ok', '#msgPost'); }
    catch (err) { showMsg(err.message, 'error', '#msgPost'); }
};

window.regenerarPost = async (id) => {
    try { await apiPost(`/api/contenido/posts/${id}`, recolectarPost(), 'PUT');
        await apiPost(`/api/contenido/posts/${id}/generar`, {});
        closeModal('modalPost'); showMsg('Regenerando con IA…', 'ok');
        State.tab = 'posts'; renderContenido();
    } catch (err) { showMsg(err.message, 'error', '#msgPost'); }
};

window.enviarRevision = async (id) => {
    try {
        await apiPost(`/api/contenido/posts/${id}`, recolectarPost(), 'PUT');
        await apiPost(`/api/contenido/posts/${id}/enviar-revision`, {});
        closeModal('modalPost'); cargarPosts();
    } catch (err) { showMsg(err.message, 'error', '#msgPost'); }
};

window.aprobarPost = async (id) => {
    try {
        await apiPost(`/api/contenido/posts/${id}`, recolectarPost(), 'PUT');
        await apiPost(`/api/contenido/posts/${id}/aprobar`, {});
        closeModal('modalPost'); cargarPosts();
    } catch (err) { showMsg(err.message, 'error', '#msgPost'); }
};

window.rechazarPost = async (id) => {
    const nota = prompt('¿Qué ajustar? (nota para quien creó el post)') || '';
    try { await apiPost(`/api/contenido/posts/${id}/rechazar`, { nota });
        closeModal('modalPost'); cargarPosts(); }
    catch (err) { showMsg(err.message, 'error', '#msgPost'); }
};

// ── REFERENCIAS DE MARCA (fotos reales del producto) ────────────────────
window.abrirReferencias = async () => {
    const modal = document.getElementById('modalRefs');
    modal.innerHTML = `<div class="modal-sheet"><div class="loading-msg"><div class="spinner"></div></div></div>`;
    openModal('modalRefs');
    await cargarRefs();
};

async function cargarRefs() {
    const modal = document.getElementById('modalRefs');
    let files = [];
    try { files = await apiGet(`/api/contenido/marcas/${State.marcaId}/referencias`) || []; }
    catch (err) { modal.innerHTML = `<div class="modal-sheet"><div class="msg show error">${esh(err.message)}</div></div>`; return; }
    const thumbs = files.map(f => `
        <div style="position:relative">
            <img src="/api/contenido/marcas/${State.marcaId}/referencias/${encodeURIComponent(f)}"
                 style="width:100px;height:100px;object-fit:cover;border-radius:8px;background:#111">
            <button class="btn-close" style="position:absolute;top:2px;right:2px;background:rgba(0,0,0,.6);border-radius:6px;padding:0 6px"
                    onclick="eliminarRef('${f}')" title="Eliminar">✕</button>
        </div>`).join('') || `<p class="text-dim text-sm">Aún no hay fotos de referencia. Sube fotos reales de los fustes plásticos.</p>`;
    modal.innerHTML = `
      <div class="modal-sheet">
        <div class="flex-between mb-8">
          <strong>📎 Referencias — ${esh(marcaActual().nombre || '')}</strong>
          <button class="btn-close" onclick="closeModal('modalRefs')">✕</button>
        </div>
        <p class="text-dim text-sm">La IA usa estas fotos reales como base para generar las imágenes (respeta material, forma y color). PNG/JPG/WEBP, máx 8 MB.</p>
        <div class="msg" id="msgRefs"></div>
        <div style="display:flex;flex-wrap:wrap;gap:8px;margin:10px 0">${thumbs}</div>
        <input type="file" id="refFile" accept="image/png,image/jpeg,image/webp" multiple style="margin-bottom:8px">
        <button class="btn btn-cyan btn-sm" id="btnSubirRef" onclick="subirRef()">Subir foto</button>
      </div>`;
}

window.subirRef = async () => {
    const inp = document.getElementById('refFile');
    if (!inp.files || !inp.files.length) { showMsg('Elige una o más imágenes.', 'error', '#msgRefs'); return; }
    const btn = document.getElementById('btnSubirRef'); setLoading(btn, true);
    let ok = 0, fail = 0;
    for (const f of inp.files) {
        try {
            const fd = new FormData(); fd.append('file', f);
            const res = await fetch(`/api/contenido/marcas/${State.marcaId}/referencias`, { method: 'POST', credentials: 'include', body: fd });
            if (res.ok) ok++; else fail++;
        } catch { fail++; }
    }
    setLoading(btn, false);
    showMsg(`Subidas ${ok}${fail ? ', ' + fail + ' con error' : ''}.`, fail ? 'error' : 'ok', '#msgRefs');
    await cargarRefs();
};

window.eliminarRef = async (name) => {
    try {
        await apiFetch(`/api/contenido/marcas/${State.marcaId}/referencias/${encodeURIComponent(name)}`, { method: 'DELETE' });
        await cargarRefs();
    } catch (err) { showMsg(err.message, 'error', '#msgRefs'); }
};

render();
