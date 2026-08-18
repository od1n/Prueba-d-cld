// ═══════════════════════════════════════════════════════════════
//  contenido.js — Estudio de Contenido (Fase 3)
//  Marca · Ideas · Posts · Calendario editorial
//  Post: carrusel (subir/reordenar/eliminar) · caption ES/EN · hashtags
//  Generación IA: todo / solo texto / solo imagen · elegir referencias
// ═══════════════════════════════════════════════════════════════
if (!Auth.requireAuth()) { /* redirige */ }

const app = document.getElementById('app');

const State = {
    marcas: [],
    marcaId: null,
    tab: 'ideas',
    pollActivo: false,
    postActual: null,   // { id, assets } del post abierto (para reordenar)
    calRef: null,       // Date del mes mostrado en el calendario
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

const MESES = ['Enero','Febrero','Marzo','Abril','Mayo','Junio','Julio',
    'Agosto','Septiembre','Octubre','Noviembre','Diciembre'];

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
                <button class="seg-btn ${State.tab === 'calendario' ? 'active' : ''}" onclick="cambiarTab('calendario')">📅 Calendario</button>
            </div>
        </div>
        <div id="tabContent"><div class="loading-msg"><div class="spinner"></div></div></div>
        <div class="modal-overlay" id="modalIdea"></div>
        <div class="modal-overlay" id="modalGen"></div>
        <div class="modal-overlay" id="modalPost"></div>
        <div class="modal-overlay" id="modalGenPost"></div>
        <div class="modal-overlay" id="modalRefs"></div>`;
    if (State.tab === 'ideas') cargarIdeas();
    else if (State.tab === 'posts') cargarPosts();
    else cargarCalendario();
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
                    <button class="btn btn-sm" onclick="limpiarIdeas()">🗑 Limpiar</button>
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

// Crear post desde idea Y disparar generación IA (un clic, referencias por defecto)
window.crearPostDesdeIdea = async (ideaId) => {
    try {
        const idea = (State.ideas || []).find(i => i.id === ideaId);
        const res = await apiPost('/api/contenido/posts', { marcaId: State.marcaId, ideaId, titulo: idea ? idea.titulo : null, formato: 'imagen' });
        await apiPost(`/api/contenido/posts/${res.id}/generar`, { modo: 'todo' });
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
        cont.innerHTML = `<div class="flex-between mb-8"><span class="text-dim text-sm">${posts.length} post(s)</span>
            <button class="btn btn-sm" onclick="limpiarPosts()">🗑 Limpiar posts</button></div>
            <div class="lista">${posts.map(tarjetaPost).join('')}</div>`;
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
    return `<div class="card-mini" onclick="abrirPost(${p.id})" style="cursor:pointer">
        <div class="flex-between">
            <strong>${esh(p.titulo || '(sin título)')}</strong>
            ${badgeEstado(p.estado)}
        </div>
        <div class="text-dim text-sm" style="margin-top:4px">
            ${esh(p.formato)} · ${esh(p.plataformasDestino)} · ${p.numAssets} img
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
    State.postActual = { id, assets };
    const conEn = (marcaActual().idiomas || 'es').indexOf('en') >= 0;
    const cb = Date.now();
    const imgs = assets.length ? assets.map((a, idx) => `
        <div class="asset-item">
            <div class="asset-bar">
                <span class="text-dim text-sm">#${idx + 1}${a.origen === 'manual' ? ' · subida' : (a.modeloGen ? ' · IA' : '')}</span>
                <span style="display:flex;gap:4px">
                    <button class="btn btn-sm" onclick="moverAsset(${id},${a.id},-1)" ${idx === 0 ? 'disabled' : ''}>◀</button>
                    <button class="btn btn-sm" onclick="moverAsset(${id},${a.id},1)" ${idx === assets.length - 1 ? 'disabled' : ''}>▶</button>
                    <button class="btn btn-sm" onclick="eliminarAssetPost(${id},${a.id})" title="Eliminar">🗑</button>
                </span>
            </div>
            <img src="/api/contenido/posts/${id}/asset/${a.id}?t=${cb}" alt="asset ${idx + 1}">
        </div>`).join('')
        : `<p class="text-dim text-sm">Sin imagen todavía.</p>`;
    const fechaVal = (p.fechaProgramada || '').replace(' ', 'T').slice(0, 16);
    const puedeAprobar = esAdmin();
    modal.innerHTML = `
      <div class="modal-sheet" style="max-width:640px">
        <div class="flex-between mb-8">
          <strong>${esh(p.titulo || 'Post')} ${badgeEstado(p.estado)}</strong>
          <button class="btn-close" onclick="closeModal('modalPost')">✕</button>
        </div>
        <div class="msg" id="msgPost"></div>
        ${p.mensajeError ? `<div class="msg show error">${esh(p.mensajeError)}</div>` : ''}
        <div class="carrusel">${imgs}</div>
        <div class="asset-add">
            <input type="file" id="assetFile" accept="image/png,image/jpeg,image/webp">
            <button class="btn btn-sm" id="btnAsset" onclick="subirAssetPost(${id})">＋ Agregar imagen</button>
        </div>
        <div class="input-group"><label class="input-label">Caption (ES)</label>
            <textarea class="input-field" id="pCapEs" rows="4">${esh(p.captionEs || '')}</textarea></div>
        ${conEn ? `<div class="input-group"><label class="input-label">Caption (EN)</label>
            <textarea class="input-field" id="pCapEn" rows="4">${esh(p.captionEn || '')}</textarea></div>` : ''}
        <div class="input-group"><label class="input-label">Hashtags</label>
            <input class="input-field" id="pHash" value="${eshA(p.hashtags || '')}"></div>
        <div class="input-group"><label class="input-label">Programar publicación (opcional)</label>
            <input class="input-field" id="pFecha" type="datetime-local" value="${eshA(fechaVal)}"></div>
        <div class="acciones-gen">
          <button class="btn btn-sm" onclick="regenTodo(${id})">🎲 Regenerar todo</button>
          <button class="btn btn-sm" onclick="regenTexto(${id})">📝 Solo texto</button>
          <button class="btn btn-sm" onclick="regenImagen(${id})">🖼️ Solo imagen</button>
        </div>
        <div style="display:flex;flex-wrap:wrap;gap:8px;margin-top:8px">
          <button class="btn btn-sm" onclick="guardarPost(${id})">💾 Guardar</button>
          <button class="btn btn-sm btn-cyan" onclick="enviarRevision(${id})">Enviar a revisión →</button>
          ${puedeAprobar ? `<button class="btn btn-sm" style="background:#16643a;color:#fff" onclick="aprobarPost(${id})">✔ Aprobar</button>
                            <button class="btn btn-sm" onclick="rechazarPost(${id})">✖ Rechazar</button>` : ''}
          <button class="btn btn-sm" style="color:#ef4444;border-color:#ef4444" onclick="eliminarPost(${id})">🗑 Eliminar</button>
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

// Guarda sin toast (usado antes de regenerar)
async function guardarSilencioso(id) {
    try { await apiPost(`/api/contenido/posts/${id}`, recolectarPost(), 'PUT'); } catch (e) { /* ignore */ }
}

window.guardarPost = async (id) => {
    try { await apiPost(`/api/contenido/posts/${id}`, recolectarPost(), 'PUT');
        showMsg('Guardado.', 'ok', '#msgPost'); }
    catch (err) { showMsg(err.message, 'error', '#msgPost'); }
};

// ── Carrusel: subir / reordenar / eliminar imágenes ──────────────────────
window.subirAssetPost = async (id) => {
    const inp = document.getElementById('assetFile');
    if (!inp.files || !inp.files.length) { showMsg('Elige una imagen.', 'error', '#msgPost'); return; }
    const btn = document.getElementById('btnAsset'); setLoading(btn, true);
    try {
        const fd = new FormData(); fd.append('file', inp.files[0]);
        const res = await fetch(`/api/contenido/posts/${id}/assets`, { method: 'POST', credentials: 'include', body: fd });
        if (!res.ok) { const j = await res.json().catch(() => ({})); throw new Error(j.error || 'No se pudo subir.'); }
        await abrirPost(id);
    } catch (err) { showMsg(err.message, 'error', '#msgPost'); setLoading(btn, false); }
};

window.eliminarAssetPost = async (id, assetId) => {
    if (!confirm('¿Eliminar esta imagen del post?')) return;
    try {
        await apiFetch(`/api/contenido/posts/${id}/assets/${assetId}`, { method: 'DELETE' });
        await abrirPost(id);
    } catch (err) { showMsg(err.message, 'error', '#msgPost'); }
};

window.moverAsset = async (id, assetId, delta) => {
    const cur = State.postActual;
    if (!cur || cur.id !== id) return;
    const ids = cur.assets.map(a => a.id);
    const i = ids.indexOf(assetId);
    const j = i + delta;
    if (i < 0 || j < 0 || j >= ids.length) return;
    [ids[i], ids[j]] = [ids[j], ids[i]];
    try {
        await apiPost(`/api/contenido/posts/${id}/assets/orden`, { ids }, 'PUT');
        await abrirPost(id);
    } catch (err) { showMsg(err.message, 'error', '#msgPost'); }
};

// ── Regeneración con IA (todo / texto / imagen) ──────────────────────────
window.regenTexto = async (id) => {
    await guardarSilencioso(id);
    try {
        await apiPost(`/api/contenido/posts/${id}/generar`, { modo: 'texto' });
        closeModal('modalPost'); showMsg('Regenerando texto con IA…', 'ok');
        State.tab = 'posts'; renderContenido();
    } catch (err) { showMsg(err.message, 'error', '#msgPost'); }
};
window.regenTodo   = async (id) => { await guardarSilencioso(id); abrirGenerarPost(id, 'todo'); };
window.regenImagen = async (id) => { await guardarSilencioso(id); abrirGenerarPost(id, 'imagen'); };

// Modal de opciones de generación de imagen (prompt + elegir referencias)
window.abrirGenerarPost = async (id, modo) => {
    const modal = document.getElementById('modalGenPost');
    modal.innerHTML = `<div class="modal-sheet"><div class="loading-msg"><div class="spinner"></div></div></div>`;
    closeModal('modalPost');
    openModal('modalGenPost');
    let post = {}, refs = [];
    try {
        const d = await apiGet(`/api/contenido/posts/${id}`); post = d.post || {};
        refs = await apiGet(`/api/contenido/marcas/${post.marcaId}/referencias`) || [];
    } catch (e) { /* sigue con lo que haya */ }
    const refThumbs = refs.length ? refs.map(f => `
        <label class="ref-pick" title="${eshA(f)}">
            <input type="checkbox" class="genRef" value="${eshA(f)}" onchange="actualizarRefCount()" checked>
            <img src="/api/contenido/marcas/${post.marcaId}/referencias/${encodeURIComponent(f)}">
        </label>`).join('')
        : `<p class="text-dim text-sm">No hay fotos de referencia. Súbelas con 📎 Referencias para que la imagen se base en el fuste real.</p>`;
    const titulo = modo === 'imagen' ? '🖼️ Regenerar imagen' : '🎲 Regenerar post completo';
    modal.innerHTML = `
      <div class="modal-sheet" style="max-width:600px">
        <div class="flex-between mb-8"><strong>${titulo}</strong>
            <button class="btn-close" onclick="closeModal('modalGenPost')">✕</button></div>
        <div class="msg" id="msgGenPost"></div>
        <div class="input-group"><label class="input-label" for="genPrompt">Prompt de imagen (qué debe mostrar)</label>
            <textarea class="input-field" id="genPrompt" rows="3" placeholder="Ej: fuste plástico HDPE en primer plano sobre fondo neutro">${esh(post.promptImagen || post.titulo || '')}</textarea></div>
        <div class="flex-between" style="align-items:flex-end;gap:8px">
          <label class="input-label" style="margin:0">Fotos de referencia a usar (conserva textura y forma real) <span id="refCount" class="text-dim"></span></label>
          ${refs.length > 1 ? `<span style="display:flex;gap:6px;white-space:nowrap">
            <button type="button" class="btn btn-sm" onclick="marcarRefs(true)">✓ Todas</button>
            <button type="button" class="btn btn-sm" onclick="marcarRefs(false)">✗ Ninguna</button></span>` : ''}
        </div>
        <div class="ref-grid">${refThumbs}</div>
        <button class="btn btn-cyan" id="btnGenPost" onclick="confirmarGenerarPost(${id},'${modo}')" style="margin-top:10px">Generar</button>
        <p class="text-dim text-sm" style="margin-top:8px">Recuerda: nunca fustes de madera (es la competencia). El cuero sí está permitido.</p>
      </div>`;
    actualizarRefCount();
};

window.marcarRefs = (v) => {
    document.querySelectorAll('.genRef').forEach(c => { c.checked = v; });
    actualizarRefCount();
};
function actualizarRefCount() {
    const total = document.querySelectorAll('.genRef').length;
    const sel = document.querySelectorAll('.genRef:checked').length;
    const el = document.getElementById('refCount');
    if (el) el.textContent = total ? `(${sel}/${total})` : '';
}

window.confirmarGenerarPost = async (id, modo) => {
    const btn = document.getElementById('btnGenPost'); setLoading(btn, true);
    const promptImagen = document.getElementById('genPrompt').value.trim();
    const referencias = Array.from(document.querySelectorAll('.genRef:checked')).map(c => c.value);
    try {
        await apiPost(`/api/contenido/posts/${id}/generar`, { modo, promptImagen: promptImagen || null, referencias });
        closeModal('modalGenPost'); showMsg('Generando con IA…', 'ok');
        State.tab = 'posts'; renderContenido();
    } catch (err) { showMsg(err.message, 'error', '#msgGenPost'); setLoading(btn, false); }
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

window.limpiarIdeas = async () => {
    if (!confirm('¿Borrar TODAS las ideas de esta marca? No se puede deshacer.')) return;
    try { await apiFetch(`/api/contenido/marcas/${State.marcaId}/ideas`, { method: 'DELETE' }); cargarIdeas(); }
    catch (err) { showMsg(err.message, 'error'); }
};
window.limpiarPosts = async () => {
    if (!confirm('¿Borrar TODOS los posts de esta marca (con sus imágenes)? No se puede deshacer.')) return;
    try { await apiFetch(`/api/contenido/marcas/${State.marcaId}/posts`, { method: 'DELETE' }); cargarPosts(); }
    catch (err) { showMsg(err.message, 'error'); }
};
window.eliminarPost = async (id) => {
    if (!confirm('¿Eliminar este post y sus imágenes? No se puede deshacer.')) return;
    try { await apiFetch(`/api/contenido/posts/${id}`, { method: 'DELETE' }); closeModal('modalPost'); cargarPosts(); }
    catch (err) { showMsg(err.message, 'error', '#msgPost'); }
};

window.rechazarPost = async (id) => {
    const nota = prompt('¿Qué ajustar? (nota para quien creó el post)') || '';
    try { await apiPost(`/api/contenido/posts/${id}/rechazar`, { nota });
        closeModal('modalPost'); cargarPosts(); }
    catch (err) { showMsg(err.message, 'error', '#msgPost'); }
};

// ── CALENDARIO EDITORIAL ─────────────────────────────────────────────────
async function cargarCalendario() {
    const cont = document.getElementById('tabContent');
    const ref = State.calRef || new Date();
    State.calRef = new Date(ref.getFullYear(), ref.getMonth(), 1);
    const y = State.calRef.getFullYear(), m = State.calRef.getMonth();
    const last = new Date(y, m + 1, 0);
    const mm = String(m + 1).padStart(2, '0');
    const desde = `${y}-${mm}-01 00:00:00`;
    const hasta = `${y}-${mm}-${String(last.getDate()).padStart(2, '0')} 23:59:59`;
    let posts = [];
    try {
        posts = await apiGet(`/api/contenido/posts?marca=${State.marcaId}&desde=${encodeURIComponent(desde)}&hasta=${encodeURIComponent(hasta)}`) || [];
    } catch (err) { cont.innerHTML = `<div class="msg show error">${esh(err.message)}</div>`; return; }

    const byDay = {};
    posts.forEach(p => {
        if (!p.fechaProgramada) return;
        const d = p.fechaProgramada.slice(0, 10);
        (byDay[d] = byDay[d] || []).push(p);
    });

    const primerDiaSem = new Date(y, m, 1).getDay(); // 0=Dom
    const dias = last.getDate();
    const hoyStr = fechaISOLocal(new Date());
    let celdas = '';
    for (let i = 0; i < primerDiaSem; i++) celdas += `<div class="cal-cell cal-empty"></div>`;
    for (let d = 1; d <= dias; d++) {
        const key = `${y}-${mm}-${String(d).padStart(2, '0')}`;
        const chips = (byDay[key] || []).map(p =>
            `<div class="cal-chip badge-${esh(p.estado)}" onclick="abrirPost(${p.id})" title="${eshA(p.titulo || '')}">${esh((p.titulo || 'post').slice(0, 18))}</div>`
        ).join('');
        celdas += `<div class="cal-cell ${key === hoyStr ? 'cal-hoy' : ''}">
            <div class="cal-num">${d}</div>${chips}</div>`;
    }
    const sinProg = posts.filter(p => !p.fechaProgramada).length;
    cont.innerHTML = `
        <div class="flex-between mb-8">
            <div style="display:flex;align-items:center;gap:8px">
                <button class="btn btn-sm" onclick="calNav(-1)">‹</button>
                <strong>${MESES[m]} ${y}</strong>
                <button class="btn btn-sm" onclick="calNav(1)">›</button>
                <button class="btn btn-sm" onclick="calHoy()">Hoy</button>
            </div>
            <span class="text-dim text-sm">${posts.filter(p=>p.fechaProgramada).length} programado(s)${sinProg ? ` · ${sinProg} sin fecha` : ''}</span>
        </div>
        <div class="cal-head">
            <span>Dom</span><span>Lun</span><span>Mar</span><span>Mié</span><span>Jue</span><span>Vie</span><span>Sáb</span>
        </div>
        <div class="cal-grid">${celdas}</div>
        <p class="text-dim text-sm" style="margin-top:10px">Programa un post desde su detalle (campo “Programar publicación”). Al aprobarlo con fecha, pasa a “Programado”.</p>`;
}

function fechaISOLocal(dt) {
    return `${dt.getFullYear()}-${String(dt.getMonth() + 1).padStart(2, '0')}-${String(dt.getDate()).padStart(2, '0')}`;
}
window.calNav = (delta) => {
    const r = State.calRef || new Date();
    State.calRef = new Date(r.getFullYear(), r.getMonth() + delta, 1);
    cargarCalendario();
};
window.calHoy = () => { State.calRef = new Date(); cargarCalendario(); };

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
        <p class="text-dim text-sm">La IA usa estas fotos reales como base y conserva la textura, material, forma y color del fuste plástico. PNG/JPG/WEBP, máx 8 MB.</p>
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
