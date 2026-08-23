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
                <button class="btn btn-sm" onclick="abrirRefsVideo()" title="Bancos de video de referencia (productos reales / creatividad)">📹 Videos ref</button>
                <button class="btn btn-sm" onclick="abrirMetricasCuenta()" title="Seguidores e interacción de las cuentas">📊 Métricas</button>
                <button class="btn btn-sm" onclick="abrirBandeja()" title="Mensajes y comentarios de la gente en las redes">💬 Bandeja</button>
                ${esAdmin() ? `<button class="btn btn-sm" onclick="abrirCuentas()" title="Configurar Instagram/Facebook para publicar">⚙ Cuentas</button>` : ''}
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
        <div class="modal-overlay" id="modalRefs"></div>
        <div class="modal-overlay" id="modalCuentas"></div>
        <div class="modal-overlay" id="modalMetricas"></div>
        <div class="modal-overlay" id="modalBandeja"></div>
        <div class="modal-overlay" id="modalRefVid"></div>`;
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

// Crea un post EN BLANCO para usar un video hecho fuera (sin generar imagen con IA).
window.nuevoPostVideo = async () => {
    try {
        const res = await apiPost('/api/contenido/posts', { marcaId: State.marcaId, formato: 'video', titulo: 'Post de video' });
        State.tab = 'posts';
        await cargarPosts();
        await abrirPost(res.id);
    } catch (err) { showMsg(err.message, 'error'); }
};

// ── POSTS ──────────────────────────────────────────────────────────────
async function cargarPosts() {
    const cont = document.getElementById('tabContent');
    try {
        const posts = await apiGet(`/api/contenido/posts?marca=${State.marcaId}`) || [];
        if (!posts.length) {
            cont.innerHTML = `<div class="empty-state"><span style="font-size:48px">🗂️</span>
                <p>Todavía no hay posts. Genera uno desde una idea o crea uno de video.</p>
                <button class="btn btn-sm btn-cyan" style="margin-top:10px" onclick="nuevoPostVideo()">➕ Post de video</button></div>`;
            return;
        }
        cont.innerHTML = `<div class="flex-between mb-8"><span class="text-dim text-sm">${posts.length} post(s)</span>
            <div style="display:flex;gap:6px">
              <button class="btn btn-sm btn-cyan" onclick="nuevoPostVideo()">➕ Post de video</button>
              <button class="btn btn-sm" onclick="limpiarPosts()">🗑 Limpiar posts</button>
            </div></div>
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
                    <button class="btn btn-sm" onclick="abrirAjuste(${id},${a.id})" title="Ajustar esta imagen con IA (cambiar un detalle, color, etc.)">✨</button>
                    <button class="btn btn-sm" onclick="eliminarAssetPost(${id},${a.id})" title="Eliminar">🗑</button>
                </span>
            </div>
            ${a.tipo === 'video'
                ? `<video src="/api/contenido/posts/${id}/asset/${a.id}?t=${cb}" controls playsinline style="width:100%;border-radius:8px;background:#000;max-height:360px"></video>`
                : `<img src="/api/contenido/posts/${id}/asset/${a.id}?t=${cb}" alt="asset ${idx + 1}">`}
        </div>`).join('')
        : `<p class="text-dim text-sm">Sin imagen todavía.</p>`;
    const fechaVal = (p.fechaProgramada || '').replace(' ', 'T').slice(0, 16);
    const puedeAprobar = esAdmin();
    const esVideoPost = assets.some(a => a.tipo === 'video');
    const puedePublicar = ['aprobado', 'programado', 'publicado'].includes(p.estado);
    const seccionPublicar = puedePublicar ? `
        <div class="acciones-gen" style="flex-direction:column;align-items:stretch;gap:10px">
          <strong>📢 Publicar ${p.estado === 'publicado' ? '<span class="badge badge-publicado">Publicado</span>' : ''}</strong>
          <div class="text-dim text-sm" style="background:#0d1117;border:1px solid var(--borde);border-radius:8px;padding:6px 8px">🔖 Toda publicación sale con la marca de agua del RIF (J30572871-7) aplicada automáticamente, tanto en el zip como al publicar en las redes.</div>
          <div>
            <div class="text-dim text-sm" style="margin-bottom:4px">Paso 1 · Descarga ${esVideoPost ? 'el video, las imágenes' : 'las imágenes'} y el texto</div>
            <button class="btn btn-sm" onclick="descargarZip(${id})">⬇ Descargar zip (${esVideoPost ? 'video + imágenes + texto' : 'imágenes + texto'})</button>
          </div>
          <div>
            <div class="text-dim text-sm" style="margin-bottom:4px">Paso 2 · Copia el texto al portapapeles</div>
            <div style="display:flex;flex-wrap:wrap;gap:6px">
              <button class="btn btn-sm btn-cyan" onclick="copiarContenido('todo',${id})">📋 Caption + hashtags</button>
              <button class="btn btn-sm" onclick="copiarContenido('es',${id})">Solo caption ES</button>
              ${conEn ? `<button class="btn btn-sm" onclick="copiarContenido('en',${id})">Caption EN</button>` : ''}
              <button class="btn btn-sm" onclick="copiarContenido('ht',${id})">Solo hashtags</button>
            </div>
          </div>
          <div>
            <div class="text-dim text-sm" style="margin-bottom:4px">Paso 3 · Abre la red, pega el texto y sube las imágenes del zip</div>
            <div style="display:flex;flex-wrap:wrap;gap:6px">
              <button class="btn btn-sm" onclick="window.open('https://www.instagram.com/','_blank')">Abrir Instagram ↗</button>
              <button class="btn btn-sm" onclick="window.open('https://business.facebook.com/latest/composer','_blank')">Abrir Facebook ↗</button>
            </div>
          </div>
          <div>
            <div class="text-dim text-sm" style="margin-bottom:4px">Paso 4 · Cuando ya quedó publicado en la red</div>
            ${p.estado !== 'publicado'
              ? `<button class="btn btn-sm" style="background:#16643a;color:#fff" onclick="marcarPublicado(${id})">✅ Marcar como publicado</button>`
              : `<span class="text-dim text-sm">Este post ya está marcado como publicado.</span>`}
          </div>
          <div style="border-top:1px solid var(--borde);padding-top:8px;margin-top:2px">
            <div class="text-dim text-sm" style="margin-bottom:4px">O publica automáticamente (requiere cuentas configuradas en ⚙ Cuentas)${esVideoPost ? ' · este post se publicará como <strong>Reel/Video</strong>' : ''}:</div>
            <button class="btn btn-sm" style="background:#7c3aed;color:#fff" onclick="publicarAhora(${id},'${p.plataformasDestino || 'instagram'}',${esVideoPost})">🚀 Publicar ${esVideoPost ? 'reel' : 'ahora'} (auto)</button>
          </div>
          <div style="border-top:1px solid var(--borde);padding-top:8px;margin-top:2px">
            <div class="text-dim text-sm" style="margin-bottom:4px">Seguimiento · visualizaciones e interacción</div>
            <div style="display:flex;flex-wrap:wrap;gap:6px">
              <button class="btn btn-sm" onclick="verMetricasPost(${id})">📊 Ver métricas de este post</button>
              <button class="btn btn-sm" onclick="vincularPost(${id})" title="Para posts que subiste a mano: engancha el post publicado para poder ver sus métricas">🔗 Vincular post publicado a mano</button>
            </div>
          </div>
        </div>` : '';
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
        <div style="border:1px solid var(--borde);border-radius:10px;padding:10px;margin-bottom:10px">
          <strong style="font-size:13px">🎞️ Video externo + texto con IA</strong>
          <p class="text-dim text-sm" style="margin:4px 0 8px">Sube un video hecho fuera de la app y/o escribe la idea; la IA arma el texto del post (puede analizar el video).</p>
          <div class="asset-add" style="margin:0 0 8px">
            <input type="file" id="vidExtFile" accept="video/mp4,video/quicktime,video/webm">
            <button class="btn btn-sm" id="btnVidExt" onclick="subirVideoExterno(${id})">⬆ Subir video</button>
          </div>
          <textarea class="input-field" id="pIdea" rows="2" placeholder="Idea del post (ej: mostramos el fuste montado, resaltar resistencia y liviandad)"></textarea>
          <label style="display:flex;align-items:center;gap:6px;margin:6px 0;font-size:13px" class="text-dim">
            <input type="checkbox" id="pAnalizarVid" ${esVideoPost ? 'checked' : ''}> Analizar el video del post
          </label>
          <button class="btn btn-sm btn-cyan" id="btnGenTexto" onclick="generarTextoPost(${id})">✍️ Generar texto del post</button>
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
          <button class="btn btn-sm" onclick="abrirVideo(${id})">🎬 Crear video</button>
        </div>
        <div style="display:flex;flex-wrap:wrap;gap:8px;margin-top:8px">
          <button class="btn btn-sm" onclick="guardarPost(${id})">💾 Guardar</button>
          <button class="btn btn-sm btn-cyan" onclick="enviarRevision(${id})">Enviar a revisión →</button>
          ${puedeAprobar ? `<button class="btn btn-sm" style="background:#16643a;color:#fff" onclick="aprobarPost(${id})">✔ Aprobar</button>
                            <button class="btn btn-sm" onclick="rechazarPost(${id})">✖ Rechazar</button>` : ''}
          <button class="btn btn-sm" style="color:#ef4444;border-color:#ef4444" onclick="eliminarPost(${id})">🗑 Eliminar</button>
        </div>
        ${seccionPublicar}
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

// ── Publicación asistida (Fase 4a) ───────────────────────────────────────
window.descargarZip = (id) => {
    const a = document.createElement('a');
    a.href = `/api/contenido/posts/${id}/paquete.zip?t=${Date.now()}`;
    a.download = `post-${id}.zip`;
    document.body.appendChild(a); a.click(); a.remove();
};

window.copiarContenido = async (tipo, id) => {
    const es = document.getElementById('pCapEs') ? document.getElementById('pCapEs').value : '';
    const en = document.getElementById('pCapEn') ? document.getElementById('pCapEn').value : '';
    const ht = document.getElementById('pHash') ? document.getElementById('pHash').value : '';
    let txt = tipo === 'es' ? es : tipo === 'en' ? en : tipo === 'ht' ? ht
        : [es, ht].filter(Boolean).join('\n\n');
    try {
        await navigator.clipboard.writeText(txt);
        showMsg('Copiado al portapapeles.', 'ok', '#msgPost');
    } catch (e) {
        showMsg('No se pudo copiar automáticamente; selecciona el texto y cópialo manual.', 'error', '#msgPost');
    }
};

window.marcarPublicado = async (id) => {
    if (!confirm('¿Marcar este post como PUBLICADO? Registra que ya lo publicaste manualmente en las redes.')) return;
    await guardarSilencioso(id);
    try {
        await apiPost(`/api/contenido/posts/${id}/marcar-publicado`, {});
        closeModal('modalPost'); cargarPosts();
    } catch (err) { showMsg(err.message, 'error', '#msgPost'); }
};

window.publicarAhora = async (id, plataformasStr, esVideo) => {
    const msgConf = esVideo
        ? '¿Publicar AHORA el video como Reel en las redes configuradas? El procesamiento del video puede tardar hasta ~4 min.'
        : '¿Publicar AHORA automáticamente en las redes configuradas? Se subirá la imagen y el texto.';
    if (!confirm(msgConf)) return;
    await guardarSilencioso(id);
    showMsg(esVideo ? 'Publicando reel (procesando video, puede tardar)…' : 'Publicando…', 'ok', '#msgPost');
    const plats = (plataformasStr || 'instagram').split(',').map(s => s.trim()).filter(Boolean);
    try {
        const res = await apiPost(`/api/contenido/posts/${id}/publicar`, { plataformas: plats });
        const lineas = (res.resultados || []).map(r => `${r.plataforma}: ${r.ok ? 'OK ' + (r.permalink || '') : 'ERROR: ' + (r.error || '')}`).join(' · ');
        if (res.ok) { showMsg('Publicado. ' + lineas, 'ok', '#msgPost'); setTimeout(() => { closeModal('modalPost'); cargarPosts(); }, 2000); }
        else showMsg('No se pudo publicar. ' + lineas, 'error', '#msgPost');
    } catch (err) { showMsg(err.message, 'error', '#msgPost'); }
};

// ── Métricas / Insights (Fase 4c) ────────────────────────────────────────
function metricasSheet(inner) {
    return `<div class="modal-sheet" style="max-width:560px">
        <div class="flex-between mb-8"><strong>📊 Métricas</strong>
        <button class="btn-close" onclick="closeModal('modalMetricas')">✕</button></div>
        ${inner}</div>`;
}
function fmtNum(n) {
    n = Number(n) || 0;
    if (n >= 1000000) return (n / 1000000).toFixed(1) + 'M';
    if (n >= 1000) return (n / 1000).toFixed(n >= 10000 ? 0 : 1) + 'k';
    return String(n);
}
function metricTile(label, val) {
    const n = (val === null || val === undefined) ? '—' : fmtNum(val);
    return `<div class="metric-tile"><div class="metric-num">${n}</div><div class="metric-lbl">${esh(label)}</div></div>`;
}

window.verMetricasPost = async (id) => {
    const modal = document.getElementById('modalMetricas');
    modal.innerHTML = metricasSheet(`<div class="loading-msg"><div class="spinner"></div></div>`);
    openModal('modalMetricas');
    let data;
    try { data = await apiGet(`/api/contenido/posts/${id}/metricas`); }
    catch (e) { modal.innerHTML = metricasSheet(`<div class="msg show error">${esh(e.message)}</div>`); return; }
    const plats = data.plataformas || [];
    let body;
    if (!plats.length) {
        body = `<p class="text-dim text-sm">Este post no tiene publicaciones registradas todavía. Publícalo (🚀 auto) o usa "🔗 Vincular" para engancharlo si lo subiste a mano.</p>`;
    } else {
        body = plats.map(pl => {
            const icon = pl.plataforma === 'facebook' ? '📘 Facebook' : '📸 Instagram';
            if (!pl.ok) return `<div class="card-mini"><strong>${icon}</strong><div class="msg show error" style="margin-top:6px">${esh(pl.error || 'Sin datos')}</div></div>`;
            const m = pl.metricas || {};
            const tiles = pl.plataforma === 'facebook'
                ? [['Alcance', m.alcance], ['Impresiones', m.impresiones], ['Me gusta', m.likes], ['Comentarios', m.comentarios], ['Compartidos', m.compartidos]]
                : [['Alcance', m.alcance], ['Me gusta', m.likes], ['Comentarios', m.comentarios], ['Guardados', m.guardados], ['Interacción', m.interacciones]];
            const link = pl.permalink && /^https?:/.test(pl.permalink) ? `<a href="${esh(pl.permalink)}" target="_blank" class="text-sm">Ver ↗</a>` : '';
            return `<div class="card-mini"><div class="flex-between"><strong>${icon}</strong>${link}</div>
                <div class="metricas-grid">${tiles.map(t => metricTile(t[0], t[1])).join('')}</div></div>`;
        }).join('');
    }
    modal.innerHTML = metricasSheet(`<h3 style="margin:0 0 4px;font-size:15px">Métricas del post</h3>
      <p class="text-dim text-sm" style="margin:0 0 10px">Datos en vivo desde Instagram / Facebook.</p>${body}`);
};

window.vincularPost = async (id, plataforma) => {
    const modal = document.getElementById('modalMetricas');
    if (!plataforma) {
        modal.innerHTML = metricasSheet(`<h3 style="margin:0 0 8px;font-size:15px">🔗 Vincular post publicado a mano</h3>
          <p class="text-dim text-sm" style="margin:0 0 8px">¿En qué red lo publicaste?</p>
          <div style="display:flex;gap:8px">
            <button class="btn btn-sm btn-cyan" onclick="vincularPost(${id},'instagram')">📸 Instagram</button>
            <button class="btn btn-sm" onclick="vincularPost(${id},'facebook')">📘 Facebook</button>
          </div>`);
        openModal('modalMetricas'); return;
    }
    modal.innerHTML = metricasSheet(`<div class="loading-msg"><div class="spinner"></div></div>`);
    openModal('modalMetricas');
    let medios;
    try { medios = await apiGet(`/api/contenido/marcas/${State.marcaId}/medios-recientes?plataforma=${plataforma}`); }
    catch (e) { modal.innerHTML = metricasSheet(`<div class="msg show error">${esh(e.message)}</div>`); return; }
    if (!medios || !medios.length) { modal.innerHTML = metricasSheet(`<p class="text-dim text-sm">No se encontraron publicaciones recientes en la cuenta.</p>`); return; }
    const items = medios.map(md => {
        const txt = (md.texto || '').slice(0, 60);
        const img = md.imagen ? `<img src="${esh(md.imagen)}" style="width:52px;height:52px;object-fit:cover;border-radius:8px;background:#111">` : '';
        const fecha = (md.fecha || '').slice(0, 10);
        return `<div class="card-mini" style="display:flex;gap:8px;align-items:center;cursor:pointer" onclick="confirmarVinculo(${id},'${plataforma}','${esh(md.id)}','${encodeURIComponent(md.enlace || '')}')">
            ${img}<div style="flex:1;min-width:0"><div class="text-sm" style="white-space:nowrap;overflow:hidden;text-overflow:ellipsis">${esh(txt || '(sin texto)')}</div>
            <div class="text-dim text-sm">${fecha}</div></div></div>`;
    }).join('');
    modal.innerHTML = metricasSheet(`<h3 style="margin:0 0 6px;font-size:15px">🔗 Elige la publicación</h3>
      <p class="text-dim text-sm" style="margin:0 0 8px">Toca el post que corresponde a este contenido.</p>${items}`);
};

window.confirmarVinculo = async (id, plataforma, idExterno, enlaceEnc) => {
    try {
        await apiPost(`/api/contenido/posts/${id}/vincular`, { plataforma, idExterno, enlace: decodeURIComponent(enlaceEnc || '') });
        closeModal('modalMetricas');
        showMsg('Post vinculado. Ya puedes ver sus métricas.', 'ok', '#msgPost');
        cargarPosts();
    } catch (e) { showMsg(e.message, 'error', '#msgPost'); }
};

window.abrirMetricasCuenta = async () => {
    const modal = document.getElementById('modalMetricas');
    modal.innerHTML = metricasSheet(`<div class="loading-msg"><div class="spinner"></div></div>`);
    openModal('modalMetricas');
    let data;
    try { data = await apiGet(`/api/contenido/marcas/${State.marcaId}/metricas-cuenta`); }
    catch (e) { modal.innerHTML = metricasSheet(`<div class="msg show error">${esh(e.message)}</div>`); return; }
    let body;
    if (!data || !data.length) {
        body = `<p class="text-dim text-sm">Sin datos de cuenta todavía. Aparecerán cuando la cuenta esté conectada con token (tras la verificación de Meta) en ⚙ Cuentas.</p>`;
    } else {
        body = data.map(d => {
            const icon = d.plataforma === 'facebook' ? '📘 Facebook' : '📸 Instagram';
            const tiles = [['Seguidores', d.seguidores]];
            if (d.publicaciones !== null && d.publicaciones !== undefined) tiles.push(['Publicaciones', d.publicaciones]);
            return `<div class="card-mini"><div class="flex-between"><strong>${icon}</strong><span class="text-dim text-sm">${esh(d.usuario || '')}</span></div>
              <div class="metricas-grid">${tiles.map(t => metricTile(t[0], t[1])).join('')}</div></div>`;
        }).join('');
    }
    modal.innerHTML = metricasSheet(`<h3 style="margin:0 0 6px;font-size:15px">Métricas de la cuenta</h3>
      <p class="text-dim text-sm" style="margin:0 0 10px">Resumen de las cuentas conectadas de esta marca.</p>${body}`);
};

// ── Bandeja de mensajes / comentarios (Fase 5) ───────────────────────────
function canalLabel(c) {
    return c === 'ig_comment' ? '📸 IG · comentario'
        : c === 'ig_dm' ? '📸 IG · DM'
        : c === 'fb_comment' ? '📘 FB · comentario'
        : c === 'fb_msg' ? '📘 Messenger' : c;
}
function estadoBadgeMsg(e) {
    const map = { nuevo: ['Nuevo', '#38bdf8'], auto_respondido: ['IA respondió', '#22c55e'],
        escalado: ['Requiere humano', '#eab308'], respondido: ['Respondido', '#a3e635'], cerrado: ['Cerrado', '#94a3b8'] };
    const m = map[e] || [e, '#94a3b8'];
    return `<span class="badge" style="color:${m[1]};border-color:${m[1]}">${m[0]}</span>`;
}
function bandejaSheet(inner) {
    return `<div class="modal-sheet" style="max-width:680px">
        <div class="flex-between mb-8"><strong>💬 Bandeja de mensajes</strong>
          <span style="display:flex;gap:6px">
            <button class="btn btn-sm" onclick="simularMsg()" title="Probar la lógica con un mensaje de ejemplo (sin Meta)">＋ Simular</button>
            <button class="btn btn-sm" onclick="abrirBandejaConfig()" title="Horario, base de conocimiento y respuestas">⚙ Config</button>
            <button class="btn-close" onclick="closeModal('modalBandeja')">✕</button>
          </span></div>
        <div class="msg" id="msgBandeja"></div>
        ${inner}</div>`;
}

window.abrirBandeja = async (estado) => {
    estado = estado || '';
    const modal = document.getElementById('modalBandeja');
    modal.innerHTML = bandejaSheet(`<div class="loading-msg"><div class="spinner"></div></div>`);
    openModal('modalBandeja');
    let lista;
    try { lista = await apiGet(`/api/contenido/marcas/${State.marcaId}/mensajes${estado ? `?estado=${estado}` : ''}`) || []; }
    catch (e) { modal.innerHTML = bandejaSheet(`<div class="msg show error">${esh(e.message)}</div>`); return; }
    const filtros = [['', 'Todos'], ['escalado', 'Requieren humano'], ['nuevo', 'Nuevos'],
        ['auto_respondido', 'IA'], ['respondido', 'Respondidos'], ['cerrado', 'Cerrados']]
        .map(([v, l]) => `<button class="seg-btn ${estado === v ? 'active' : ''}" onclick="abrirBandeja('${v}')">${l}</button>`).join('');
    let body;
    if (!lista.length) {
        body = `<p class="text-dim text-sm" style="padding:12px 0">No hay mensajes en esta vista. Usa "＋ Simular" para probar cómo responde el sistema, o espera a conectar Meta.</p>`;
    } else {
        body = lista.map(m => {
            const suger = m.respuestaIa ? `<div class="text-dim text-sm" style="margin-top:4px"><b>${m.estado === 'auto_respondido' ? (m.respuestaEnviada ? 'IA respondió:' : 'IA sugiere (aún sin enviar):') : 'Enviado al cliente:'}</b> ${esh(m.respuestaIa)}</div>` : '';
            const link = m.permalink && /^https?:/.test(m.permalink) ? ` · <a href="${esh(m.permalink)}" target="_blank">ver</a>` : '';
            const acciones = m.estado !== 'cerrado' ? `<div style="display:flex;gap:6px;margin-top:6px;flex-wrap:wrap">
                <button class="btn btn-sm btn-cyan" onclick="responderMsg(${m.id})">✍ Responder</button>
                <button class="btn btn-sm" onclick="cerrarMsg(${m.id})">✓ Cerrar</button></div>` : '';
            return `<div class="card-mini" style="border-left:3px solid ${m.estado === 'escalado' ? '#eab308' : 'var(--borde)'}">
                <div class="flex-between"><span class="text-sm"><b>${esh(m.autor || 'Anónimo')}</b> · ${canalLabel(m.canal)}${link}</span>${estadoBadgeMsg(m.estado)}</div>
                <div style="margin-top:4px">${esh(m.texto || '')}</div>
                ${suger}${acciones}</div>`;
        }).join('');
    }
    modal.innerHTML = bandejaSheet(`<div class="seg" style="flex-wrap:wrap;margin-bottom:8px">${filtros}</div>${body}`);
};

window.responderMsg = async (id) => {
    const txt = prompt('Escribe la respuesta que se enviará al cliente:');
    if (txt === null || !txt.trim()) return;
    try {
        const r = await apiPost(`/api/contenido/mensajes/${id}/responder`, { texto: txt.trim() });
        showMsg(r.enviado ? 'Respuesta enviada.' : 'Registrada. (No se envió por la red porque falta el token de Meta.)', 'ok', '#msgBandeja');
        setTimeout(() => abrirBandeja(), 500);
    } catch (e) { showMsg(e.message, 'error', '#msgBandeja'); }
};

window.cerrarMsg = async (id) => {
    try { await apiPost(`/api/contenido/mensajes/${id}/estado`, { estado: 'cerrado' }); abrirBandeja(); }
    catch (e) { showMsg(e.message, 'error', '#msgBandeja'); }
};

window.simularMsg = async () => {
    const texto = prompt('Escribe un mensaje de ejemplo (como si fuera un cliente):', '¿Cuál es el horario de atención?');
    if (!texto) return;
    try {
        await apiPost(`/api/contenido/marcas/${State.marcaId}/mensajes/simular`, { canal: 'ig_comment', texto });
        showMsg('Mensaje simulado. Mira cómo lo clasificó el sistema.', 'ok', '#msgBandeja');
        setTimeout(() => abrirBandeja(), 800);
    } catch (e) { showMsg(e.message, 'error', '#msgBandeja'); }
};

window.abrirBandejaConfig = async () => {
    const modal = document.getElementById('modalBandeja');
    modal.innerHTML = bandejaSheet(`<div class="loading-msg"><div class="spinner"></div></div>`);
    let cfg = {};
    try { cfg = await apiGet(`/api/contenido/marcas/${State.marcaId}/bandeja-config`) || {}; } catch (e) { }
    modal.innerHTML = bandejaSheet(`
        <h3 style="margin:0 0 8px;font-size:15px">⚙ Configuración de la bandeja</h3>
        <div style="display:flex;gap:8px">
          <div class="input-group" style="flex:1"><label class="input-label">Horario inicio (HH:MM)</label><input class="input-field" id="cfgIni" value="${eshA(cfg.horarioInicio || '08:00')}"></div>
          <div class="input-group" style="flex:1"><label class="input-label">Horario fin (HH:MM)</label><input class="input-field" id="cfgFin" value="${eshA(cfg.horarioFin || '17:00')}"></div>
        </div>
        <div class="input-group"><label class="input-label">Días laborales (1=Lun … 7=Dom)</label><input class="input-field" id="cfgDias" value="${eshA(cfg.diasLaborales || '1,2,3,4,5')}"></div>
        <div class="input-group"><label class="input-label">Base de conocimiento — lo que la IA puede responder (horario, ubicación, productos, precios públicos, formas de pago…)</label>
          <textarea class="input-field" id="cfgKb" rows="6" placeholder="Ej: Horario: Lun–Vie 8am a 5pm. Estamos en Villa de Cura, Aragua. Vendemos fustes de plástico HDPE. Aceptamos pago móvil y transferencia…">${esh(cfg.baseConocimiento || '')}</textarea></div>
        <div class="input-group"><label class="input-label">Mensaje de acuse (se envía fuera de horario cuando hace falta una persona)</label>
          <textarea class="input-field" id="cfgAcuse" rows="2">${esh(cfg.plantillaAcuse || '')}</textarea></div>
        <div class="input-group"><label class="input-label">Palabras que SIEMPRE van a una persona (separadas por coma)</label>
          <input class="input-field" id="cfgPal" value="${eshA(cfg.palabrasHumano || '')}"></div>
        <label style="display:flex;gap:6px;align-items:center;margin-bottom:10px"><input type="checkbox" id="cfgAuto" ${cfg.autoRespuesta !== false ? 'checked' : ''}> La IA responde automáticamente lo que puede</label>
        <div style="display:flex;gap:8px">
          <button class="btn btn-cyan" onclick="guardarBandejaConfig()">💾 Guardar</button>
          <button class="btn" onclick="abrirBandeja()">← Volver a la bandeja</button>
        </div>`);
};

window.guardarBandejaConfig = async () => {
    const g = id => { const e = document.getElementById(id); return e ? e.value : null; };
    const body = {
        horarioInicio: g('cfgIni'), horarioFin: g('cfgFin'), diasLaborales: g('cfgDias'),
        baseConocimiento: g('cfgKb'), plantillaAcuse: g('cfgAcuse'), palabrasHumano: g('cfgPal'),
        autoRespuesta: document.getElementById('cfgAuto') ? document.getElementById('cfgAuto').checked : true
    };
    try {
        await apiPost(`/api/contenido/marcas/${State.marcaId}/bandeja-config`, body, 'PUT');
        showMsg('Configuración guardada.', 'ok', '#msgBandeja');
    } catch (e) { showMsg(e.message, 'error', '#msgBandeja'); }
};

// ── Configuración de cuentas Meta (Admin) ────────────────────────────────
window.abrirCuentas = async () => {
    const modal = document.getElementById('modalCuentas');
    modal.innerHTML = `<div class="modal-sheet"><div class="loading-msg"><div class="spinner"></div></div></div>`;
    openModal('modalCuentas');
    let cuentas = [];
    try { cuentas = await apiGet(`/api/contenido/marcas/${State.marcaId}/cuentas`) || []; } catch (e) { }
    for (const pl of ['instagram', 'facebook']) {
        if (!cuentas.find(x => (x.plataforma || '').toLowerCase() === pl)) {
            try { await apiPost('/api/contenido/cuentas', { marcaId: State.marcaId, plataforma: pl }); } catch (e) { }
        }
    }
    cuentas = await apiGet(`/api/contenido/marcas/${State.marcaId}/cuentas`) || [];
    const ig = cuentas.find(x => (x.plataforma || '').toLowerCase() === 'instagram') || {};
    const fb = cuentas.find(x => (x.plataforma || '').toLowerCase() === 'facebook') || {};
    const badge = (c) => c.estado === 'conectada' ? '<span class="badge badge-publicado">conectada</span>' : '<span class="badge">sin token</span>';
    modal.innerHTML = `
      <div class="modal-sheet" style="max-width:560px">
        <div class="flex-between mb-8"><strong>⚙ Cuentas Meta — ${esh(marcaActual().nombre || '')}</strong>
          <button class="btn-close" onclick="closeModal('modalCuentas')">✕</button></div>
        <div class="msg" id="msgCuentas"></div>
        <div style="border:1px solid var(--borde);border-radius:10px;padding:10px;margin-bottom:10px">
          <strong>Instagram ${badge(ig)}</strong>
          <div class="input-group"><label class="input-label">ID de la cuenta de Instagram Business</label>
            <input class="input-field" id="igId" value="${eshA(ig.igUserId || '')}"></div>
          <div class="input-group"><label class="input-label">Token de acceso de Página (largo)</label>
            <input class="input-field" id="igTk" type="password" placeholder="${ig.estado === 'conectada' ? 'ya guardado — deja vacío para no cambiar' : 'pega el token'}"></div>
          <button class="btn btn-cyan btn-sm" onclick="guardarCuenta(${ig.id},'instagram')">Guardar Instagram</button>
        </div>
        <div style="border:1px solid var(--borde);border-radius:10px;padding:10px">
          <strong>Facebook ${badge(fb)}</strong>
          <div class="input-group"><label class="input-label">ID de la Página de Facebook</label>
            <input class="input-field" id="fbId" value="${eshA(fb.pageId || '')}"></div>
          <div class="input-group"><label class="input-label">Token de acceso de Página (largo)</label>
            <input class="input-field" id="fbTk" type="password" placeholder="${fb.estado === 'conectada' ? 'ya guardado — deja vacío para no cambiar' : 'pega el token'}"></div>
          <button class="btn btn-cyan btn-sm" onclick="guardarCuenta(${fb.id},'facebook')">Guardar Facebook</button>
        </div>
        <p class="text-dim text-sm" style="margin-top:8px">El token se guarda en el servidor. Instagram usa el ID de la cuenta Business; Facebook, el ID de la Página. Ambos usan un token de acceso de Página de larga duración (te guío para obtenerlo).</p>
      </div>`;
};

window.guardarCuenta = async (id, plataforma) => {
    const idVal = (plataforma === 'instagram' ? document.getElementById('igId').value : document.getElementById('fbId').value).trim();
    const tk = (plataforma === 'instagram' ? document.getElementById('igTk').value : document.getElementById('fbTk').value);
    const body = { modoPublicacion: 'auto' };
    if (plataforma === 'instagram') body.igUserId = idVal; else body.pageId = idVal;
    if (tk && tk.trim()) body.token = tk.trim();
    try {
        await apiPost(`/api/contenido/cuentas/${id}`, body, 'PUT');
        showMsg('Guardado.', 'ok', '#msgCuentas'); await abrirCuentas();
    } catch (err) { showMsg(err.message, 'error', '#msgCuentas'); }
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

window.subirVideoExterno = async (id) => {
    const inp = document.getElementById('vidExtFile');
    if (!inp.files || !inp.files.length) { showMsg('Elige un video.', 'error', '#msgPost'); return; }
    const btn = document.getElementById('btnVidExt'); setLoading(btn, true);
    try {
        const fd = new FormData(); fd.append('file', inp.files[0]);
        const res = await fetch(`/api/contenido/posts/${id}/video-externo`, { method: 'POST', credentials: 'include', body: fd });
        if (!res.ok) { const j = await res.json().catch(() => ({})); throw new Error(j.error || 'No se pudo subir el video.'); }
        showMsg('Video subido.', 'ok', '#msgPost');
        await abrirPost(id);
    } catch (err) { showMsg(err.message, 'error', '#msgPost'); setLoading(btn, false); }
};

window.generarTextoPost = async (id) => {
    const idea = (document.getElementById('pIdea')?.value || '').trim();
    const analizar = document.getElementById('pAnalizarVid')?.checked || false;
    if (!idea && !analizar) { showMsg('Escribe una idea o marca "Analizar el video".', 'error', '#msgPost'); return; }
    const btn = document.getElementById('btnGenTexto'); setLoading(btn, true);
    showMsg(analizar ? 'Generando texto (analizando video)…' : 'Generando texto…', 'ok', '#msgPost');
    try {
        const res = await apiPost(`/api/contenido/posts/${id}/texto`, { idea, analizarVideo: analizar });
        const capEs = document.getElementById('pCapEs'); if (capEs) capEs.value = res.captionEs || '';
        const capEn = document.getElementById('pCapEn'); if (capEn && res.captionEn) capEn.value = res.captionEn;
        const hash = document.getElementById('pHash'); if (hash) hash.value = res.hashtags || '';
        const extra = res.videoAnalizado ? ' (con análisis de video)' : '';
        showMsg('Texto generado' + extra + '. Revísalo y guarda.' + (res.aviso ? ' ' + res.aviso : ''), 'ok', '#msgPost');
    } catch (err) { showMsg(err.message, 'error', '#msgPost'); }
    setLoading(btn, false);
};

window.eliminarAssetPost = async (id, assetId) => {
    if (!confirm('¿Eliminar este elemento del post?')) return;
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

// ── Video / Reel (Fase 6) ────────────────────────────────────────────────
window.abrirVideo = async (id) => {
    const modal = document.getElementById('modalGenPost');
    closeModal('modalPost');
    modal.innerHTML = `
      <div class="modal-sheet" style="max-width:560px">
        <div class="flex-between mb-8"><strong>🎬 Crear video (reel 9:16)</strong>
            <button class="btn-close" onclick="closeModal('modalGenPost')">✕</button></div>
        <div class="msg" id="msgGenPost"></div>
        <div class="input-group"><label class="input-label">Tipo</label>
          <select class="input-field" id="vidTipo" onchange="vidTipoChange()">
            <option value="armado">Armado (imágenes + texto) — sin costo</option>
            <option value="ia">IA anima la imagen (Veo) — de pago</option>
          </select></div>
        <div id="vidArmado">
          <div class="input-group"><label class="input-label">Segundos por imagen</label>
            <input class="input-field" id="vidSeg" type="number" min="1" max="10" step="0.5" value="3"></div>
          <div class="input-group"><label class="input-label">Texto de gancho (opcional)</label>
            <input class="input-field" id="vidGancho" placeholder="Ej: Fustes que no se rompen"></div>
        </div>
        <div id="vidIA" style="display:none">
          <div class="input-group"><label class="input-label">Motor</label>
            <select class="input-field" id="vidMotor">
              <option value="veo-fast">Veo rápido (~US$0.15/seg)</option>
              <option value="veo-standard">Veo calidad (~US$0.40/seg)</option>
            </select></div>
          <div class="input-group"><label class="input-label">¿Cómo animar la imagen? (cámara / producto / luz)</label>
            <textarea class="input-field" id="vidPrompt" rows="3" placeholder="Ej: dolly-in lento revelando la textura del fuste; luz cálida que barre de izquierda a derecha"></textarea></div>
          <button class="btn btn-sm" id="btnIdea" onclick="ideaAnimacion(${id})">💡 Idea de animación</button>
          <div id="vidIdeas" style="margin-top:6px"></div>
          <p class="text-dim text-sm" style="margin-top:6px">Veo anima la imagen base del post en un clip de ~5–8s (tarda ~1–2 min). Tiene costo por segundo en tu cuenta de Google.</p>
        </div>
        <button class="btn btn-cyan" id="btnVideo" onclick="generarVideo(${id})" style="margin-top:8px">Generar video</button>
        <p class="text-dim text-sm" style="margin-top:8px">Publicar el Reel en IG/FB dependerá del token de Meta.</p>
      </div>`;
    openModal('modalGenPost');
};

window.vidTipoChange = () => {
    const t = document.getElementById('vidTipo').value;
    document.getElementById('vidArmado').style.display = t === 'armado' ? '' : 'none';
    document.getElementById('vidIA').style.display = t === 'ia' ? '' : 'none';
};

window.ideaAnimacion = async (id) => {
    const btn = document.getElementById('btnIdea'); setLoading(btn, true);
    try {
        const r = await apiPost(`/api/contenido/posts/${id}/idea-animacion`, {});
        const cont = document.getElementById('vidIdeas');
        cont.innerHTML = (r.ideas || []).map(idea =>
            `<div class="card-mini" style="cursor:pointer;padding:6px 8px" data-i="${eshA(idea)}" onclick="document.getElementById('vidPrompt').value=this.dataset.i">💡 ${esh(idea)}</div>`).join('');
    } catch (e) { showMsg(e.message, 'error', '#msgGenPost'); }
    setLoading(btn, false);
};

window.generarVideo = async (id) => {
    const tipo = document.getElementById('vidTipo').value;
    const btn = document.getElementById('btnVideo'); setLoading(btn, true);
    try {
        if (tipo === 'ia') {
            const motor = document.getElementById('vidMotor').value;
            const prompt = document.getElementById('vidPrompt').value.trim();
            if (!prompt) { showMsg('Escribe cómo animar la imagen (o usa 💡 Idea de animación).', 'error', '#msgGenPost'); setLoading(btn, false); return; }
            showMsg('Enviando a Veo… tarda ~1–2 min. No cierres esta ventana.', 'ok', '#msgGenPost');
            await apiPost(`/api/contenido/posts/${id}/video`, { tipo: 'ia', motor, prompt });
            pollVideoIA(id, 0);
        } else {
            const seg = parseFloat(document.getElementById('vidSeg').value) || 3;
            const gancho = document.getElementById('vidGancho').value.trim();
            showMsg('Generando el video…', 'ok', '#msgGenPost');
            await apiPost(`/api/contenido/posts/${id}/video`, { tipo: 'armado', segundosPorImagen: seg, gancho });
            closeModal('modalGenPost'); showMsg('Video generado. Míralo en el carrusel del post.', 'ok', '#msgPost');
            abrirPost(id);
        }
    } catch (err) { showMsg(err.message, 'error', '#msgGenPost'); setLoading(btn, false); }
};

async function pollVideoIA(id, tries) {
    if (tries > 45) { showMsg('Veo está tardando más de lo normal; vuelve a abrir el post en un rato para ver si quedó.', 'error', '#msgGenPost'); setLoading(document.getElementById('btnVideo'), false); return; }
    try {
        const r = await apiPost(`/api/contenido/posts/${id}/video-check`, {});
        if (r.estado === 'listo') { closeModal('modalGenPost'); showMsg('Video de Veo listo. Míralo en el carrusel.', 'ok', '#msgPost'); abrirPost(id); return; }
        if (r.estado === 'error') { showMsg('Veo: ' + (r.error || 'error'), 'error', '#msgGenPost'); setLoading(document.getElementById('btnVideo'), false); return; }
        setTimeout(() => pollVideoIA(id, tries + 1), 8000);
    } catch (e) { setTimeout(() => pollVideoIA(id, tries + 1), 8000); }
}

// Modal de opciones de generación de imagen (prompt + elegir referencias)
window.abrirGenerarPost = async (id, modo) => {
    const modal = document.getElementById('modalGenPost');
    modal.innerHTML = `<div class="modal-sheet"><div class="loading-msg"><div class="spinner"></div></div></div>`;
    closeModal('modalPost');
    openModal('modalGenPost');
    let post = {}, prod = [], crea = [];
    try {
        const d = await apiGet(`/api/contenido/posts/${id}`); post = d.post || {};
        prod = await apiGet(`/api/contenido/marcas/${post.marcaId}/referencias?categoria=producto`) || [];
        crea = await apiGet(`/api/contenido/marcas/${post.marcaId}/referencias?categoria=creatividad`) || [];
    } catch (e) { /* sigue con lo que haya */ }

    const seccionPick = (cat, files, cls, marcadas, titulo, ayuda, vacio) => {
        const thumbs = files.length ? files.map(f => `
            <label class="ref-pick" title="${eshA(f)}">
                <input type="checkbox" class="${cls}" value="${eshA(f)}" onchange="actualizarRefCount()" ${marcadas ? 'checked' : ''}>
                <img src="/api/contenido/marcas/${post.marcaId}/referencias/${encodeURIComponent(f)}?categoria=${cat}">
            </label>`).join('') : `<p class="text-dim text-sm">${vacio}</p>`;
        return `
          <div style="margin-top:8px">
            <div class="flex-between" style="align-items:flex-end;gap:8px">
              <label class="input-label" style="margin:0">${titulo} <span id="cnt_${cls}" class="text-dim"></span></label>
              ${files.length ? `<span style="display:flex;gap:6px;white-space:nowrap">
                <button type="button" class="btn btn-sm" onclick="marcarRefs('.${cls}',true)">\u2713 Todas</button>
                <button type="button" class="btn btn-sm" onclick="marcarRefs('.${cls}',false)">\u2717 Ninguna</button></span>` : ''}
            </div>
            <p class="text-dim text-sm" style="margin:2px 0">${ayuda}</p>
            <div class="ref-grid">${thumbs}</div>
          </div>`;
    };

    const titulo = modo === 'imagen' ? '\ud83d\uddbc\ufe0f Regenerar imagen' : '\ud83c\udfb2 Regenerar post completo';
    modal.innerHTML = `
      <div class="modal-sheet" style="max-width:620px">
        <div class="flex-between mb-8"><strong>${titulo}</strong>
            <button class="btn-close" onclick="closeModal('modalGenPost')">\u2715</button></div>
        <div class="msg" id="msgGenPost"></div>
        <div class="input-group"><label class="input-label" for="genPrompt">Prompt de imagen (qu\u00e9 debe mostrar)</label>
            <textarea class="input-field" id="genPrompt" rows="3" placeholder="Ej: fuste pl\u00e1stico en primer plano sobre fondo neutro">${esh(post.promptImagen || post.titulo || '')}</textarea></div>
        ${seccionPick('producto', prod, 'genRef', true, '\ud83c\udfed Productos reales (base)',
            'Mantiene forma y color reales (blanco hueso). Marcadas por defecto.',
            'No hay fotos de producto. S\u00fabelas en \ud83d\udcce Referencias.')}
        ${seccionPick('creatividad', crea, 'genRefCrea', false, '\ud83c\udfa8 Creatividad / comparaci\u00f3n (opcional)',
            'Solo inspiraci\u00f3n/comparaci\u00f3n; no se copia su color ni forma. Desmarcadas por defecto.',
            'No hay im\u00e1genes de creatividad.')}
        <button class="btn btn-cyan" id="btnGenPost" onclick="confirmarGenerarPost(${id},'${modo}')" style="margin-top:12px">Generar</button>
        <p class="text-dim text-sm" style="margin-top:8px">Regla: los fustes reales son blanco hueso; nunca cambiar su color. Nunca fustes de madera (competencia). El cuero s\u00ed est\u00e1 permitido.</p>
      </div>`;
    actualizarRefCount();
};

window.marcarRefs = (sel, v) => {
    document.querySelectorAll(sel).forEach(c => { c.checked = v; });
    actualizarRefCount();
};
function actualizarRefCount() {
    [['.genRef', 'cnt_genRef'], ['.genRefCrea', 'cnt_genRefCrea']].forEach(([sel, cntId]) => {
        const total = document.querySelectorAll(sel).length;
        const s = document.querySelectorAll(sel + ':checked').length;
        const el = document.getElementById(cntId);
        if (el) el.textContent = total ? `(${s}/${total})` : '';
    });
}

window.confirmarGenerarPost = async (id, modo) => {
    const btn = document.getElementById('btnGenPost'); setLoading(btn, true);
    const promptImagen = document.getElementById('genPrompt').value.trim();
    const referencias = Array.from(document.querySelectorAll('.genRef:checked')).map(c => c.value);
    const referenciasCreatividad = Array.from(document.querySelectorAll('.genRefCrea:checked')).map(c => c.value);
    try {
        await apiPost(`/api/contenido/posts/${id}/generar`, { modo, promptImagen: promptImagen || null, referencias, referenciasCreatividad });
        closeModal('modalGenPost'); showMsg('Generando con IA\u2026', 'ok');
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

// ── Ajuste puntual de imagen (edición por instrucción, conserva el resto) ──
window.abrirAjuste = async (id, assetId) => {
    const modal = document.getElementById('modalGenPost');
    closeModal('modalPost');
    modal.innerHTML = `
      <div class="modal-sheet" style="max-width:560px">
        <div class="flex-between mb-8"><strong>✨ Ajustar imagen</strong>
            <button class="btn-close" onclick="closeModal('modalGenPost')">✕</button></div>
        <div class="msg" id="msgGenPost"></div>
        <p class="text-dim text-sm" style="margin:0 0 8px">La IA toma esta imagen como base y aplica <b>solo</b> el cambio que pidas, conservando el resto (composición, fondo, iluminación). El resultado se agrega como una <b>imagen nueva</b> — la actual no se borra.</p>
        <img src="/api/contenido/posts/${id}/asset/${assetId}?t=${Date.now()}" style="width:100%;max-height:240px;object-fit:contain;border:1px solid var(--borde);border-radius:10px;background:#111;margin-bottom:10px" alt="imagen base">
        <div class="input-group"><label class="input-label" for="ajusteInstr">¿Qué cambio quieres?</label>
            <textarea class="input-field" id="ajusteInstr" rows="3" placeholder="Ej: cambia el color del fuste a negro mate · haz el fondo blanco liso · quita el objeto de la esquina · aclara la iluminación"></textarea></div>
        <button class="btn btn-cyan" id="btnAjuste" onclick="confirmarAjuste(${id},${assetId})">Aplicar cambio</button>
        <p class="text-dim text-sm" style="margin-top:8px">Consejo: pide un solo cambio a la vez y sé específico. Si el resultado no gusta, la imagen original sigue ahí para volver a intentar.</p>
      </div>`;
    openModal('modalGenPost');
    setTimeout(() => { const t = document.getElementById('ajusteInstr'); if (t) t.focus(); }, 50);
};

window.confirmarAjuste = async (id, assetId) => {
    const el = document.getElementById('ajusteInstr');
    const instr = (el ? el.value : '').trim();
    if (!instr) { showMsg('Escribe qué quieres cambiar.', 'error', '#msgGenPost'); return; }
    const btn = document.getElementById('btnAjuste'); setLoading(btn, true);
    try {
        await apiPost(`/api/contenido/posts/${id}/generar`, { modo: 'ajuste', assetIdBase: assetId, promptImagen: instr });
        closeModal('modalGenPost'); showMsg('Ajustando la imagen con IA…', 'ok');
        State.tab = 'posts'; renderContenido();
    } catch (err) { showMsg(err.message, 'error', '#msgGenPost'); setLoading(btn, false); }
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
    let prod = [], crea = [];
    try {
        prod = await apiGet(`/api/contenido/marcas/${State.marcaId}/referencias?categoria=producto`) || [];
        crea = await apiGet(`/api/contenido/marcas/${State.marcaId}/referencias?categoria=creatividad`) || [];
    } catch (err) { modal.innerHTML = `<div class="modal-sheet"><div class="msg show error">${esh(err.message)}</div></div>`; return; }

    const seccion = (cat, files, titulo, ayuda, vacio) => {
        const otra = cat === 'producto' ? 'creatividad' : 'producto';
        const etiqOtra = cat === 'producto' ? '\u2192 Creatividad' : '\u2192 Productos';
        const thumbs = files.map(f => `
            <div style="position:relative;display:flex;flex-direction:column;gap:3px;align-items:center;width:100px">
                <img src="/api/contenido/marcas/${State.marcaId}/referencias/${encodeURIComponent(f)}?categoria=${cat}"
                     style="width:100px;height:100px;object-fit:cover;border-radius:8px;background:#111">
                <button class="btn btn-sm" style="font-size:10px;padding:1px 5px;width:100%"
                        onclick="moverRef('${cat}','${otra}','${f}')" title="Mover al otro banco">${etiqOtra}</button>
                <button class="btn-close" style="position:absolute;top:2px;right:2px;background:rgba(0,0,0,.6);border-radius:6px;padding:0 6px"
                        onclick="eliminarRef('${cat}','${f}')" title="Eliminar">\u2715</button>
            </div>`).join('') || `<p class="text-dim text-sm">${vacio}</p>`;
        return `
          <div style="border:1px solid var(--borde);border-radius:10px;padding:10px;margin-bottom:10px">
            <strong>${titulo}</strong>
            <p class="text-dim text-sm" style="margin:4px 0">${ayuda}</p>
            <div style="display:flex;flex-wrap:wrap;gap:8px;margin:8px 0">${thumbs}</div>
            <input type="file" id="refFile_${cat}" accept="image/png,image/jpeg,image/webp" multiple style="margin-bottom:6px">
            <button class="btn btn-cyan btn-sm" id="btnSubir_${cat}" onclick="subirRef('${cat}')">Subir fotos</button>
          </div>`;
    };

    modal.innerHTML = `
      <div class="modal-sheet" style="max-width:600px">
        <div class="flex-between mb-8">
          <strong>\ud83d\udcce Referencias \u2014 ${esh(marcaActual().nombre || '')}</strong>
          <button class="btn-close" onclick="closeModal('modalRefs')">\u2715</button>
        </div>
        <div class="msg" id="msgRefs"></div>
        ${seccion('producto', prod, '\ud83c\udfed Productos reales (base)',
            'La IA los usa como BASE y mantiene su forma y color reales (blanco hueso). Sube aqu\u00ed SOLO fotos reales de los fustes de la marca.',
            'A\u00fan no hay fotos de producto. Sube fotos reales de los fustes pl\u00e1sticos (blanco hueso).')}
        ${seccion('creatividad', crea, '\ud83c\udfa8 Creatividad / comparaci\u00f3n',
            'Solo inspiraci\u00f3n de estilo o ejemplos de comparaci\u00f3n (p. ej. competencia). La IA NO copia su color ni forma. Opcionales al generar.',
            'A\u00fan no hay im\u00e1genes de creatividad/comparaci\u00f3n.')}
        <p class="text-dim text-sm">PNG/JPG/WEBP, m\u00e1x 8 MB por imagen.</p>
      </div>`;
}

window.subirRef = async (cat) => {
    const inp = document.getElementById(`refFile_${cat}`);
    if (!inp.files || !inp.files.length) { showMsg('Elige una o m\u00e1s im\u00e1genes.', 'error', '#msgRefs'); return; }
    const btn = document.getElementById(`btnSubir_${cat}`); setLoading(btn, true);
    let ok = 0, fail = 0;
    for (const f of inp.files) {
        try {
            const fd = new FormData(); fd.append('file', f);
            const res = await fetch(`/api/contenido/marcas/${State.marcaId}/referencias?categoria=${cat}`, { method: 'POST', credentials: 'include', body: fd });
            if (res.ok) ok++; else fail++;
        } catch { fail++; }
    }
    setLoading(btn, false);
    showMsg(`Subidas ${ok}${fail ? ', ' + fail + ' con error' : ''}.`, fail ? 'error' : 'ok', '#msgRefs');
    await cargarRefs();
};

window.moverRef = async (desde, hacia, name) => {
    try {
        await apiPost(`/api/contenido/marcas/${State.marcaId}/referencias/${encodeURIComponent(name)}/mover?desde=${desde}&hacia=${hacia}`, {});
        await cargarRefs();
    } catch (err) { showMsg(err.message, 'error', '#msgRefs'); }
};

window.eliminarRef = async (cat, name) => {
    try {
        await apiFetch(`/api/contenido/marcas/${State.marcaId}/referencias/${encodeURIComponent(name)}?categoria=${cat}`, { method: 'DELETE' });
        await cargarRefs();
    } catch (err) { showMsg(err.message, 'error', '#msgRefs'); }
};

// ── BANCOS DE VIDEO DE REFERENCIA (productos reales / creatividad) ───────
window.abrirRefsVideo = async () => {
    const modal = document.getElementById('modalRefVid');
    modal.innerHTML = `<div class="modal-sheet"><div class="loading-msg"><div class="spinner"></div></div></div>`;
    openModal('modalRefVid');
    await cargarRefsVideo();
};

async function cargarRefsVideo() {
    const modal = document.getElementById('modalRefVid');
    let prod = [], crea = [];
    try {
        prod = await apiGet(`/api/contenido/marcas/${State.marcaId}/referencias-video?categoria=producto`) || [];
        crea = await apiGet(`/api/contenido/marcas/${State.marcaId}/referencias-video?categoria=creatividad`) || [];
    } catch (err) { modal.innerHTML = `<div class="modal-sheet"><div class="msg show error">${esh(err.message)}</div></div>`; return; }

    const seccion = (cat, files, titulo, ayuda, vacio) => {
        const otra = cat === 'producto' ? 'creatividad' : 'producto';
        const etiqOtra = cat === 'producto' ? '→ Creatividad' : '→ Productos';
        const thumbs = files.map(f => `
            <div style="position:relative;display:flex;flex-direction:column;gap:3px;align-items:center;width:120px">
                <video src="/api/contenido/marcas/${State.marcaId}/referencias-video/${encodeURIComponent(f)}?categoria=${cat}"
                       controls preload="metadata"
                       style="width:120px;height:120px;object-fit:cover;border-radius:8px;background:#111"></video>
                <button class="btn btn-sm" style="font-size:10px;padding:1px 5px;width:100%"
                        onclick="moverRefVideo('${cat}','${otra}','${f}')" title="Mover al otro banco">${etiqOtra}</button>
                <button class="btn-close" style="position:absolute;top:2px;right:2px;background:rgba(0,0,0,.6);border-radius:6px;padding:0 6px"
                        onclick="eliminarRefVideo('${cat}','${f}')" title="Eliminar">✕</button>
            </div>`).join('') || `<p class="text-dim text-sm">${vacio}</p>`;
        return `
          <div style="border:1px solid var(--borde);border-radius:10px;padding:10px;margin-bottom:10px">
            <strong>${titulo}</strong>
            <p class="text-dim text-sm" style="margin:4px 0">${ayuda}</p>
            <div style="display:flex;flex-wrap:wrap;gap:8px;margin:8px 0">${thumbs}</div>
            <input type="file" id="refVidFile_${cat}" accept="video/mp4,video/quicktime,video/webm" multiple style="margin-bottom:6px">
            <button class="btn btn-cyan btn-sm" id="btnSubirVid_${cat}" onclick="subirRefVideo('${cat}')">Subir videos</button>
          </div>`;
    };

    modal.innerHTML = `
      <div class="modal-sheet" style="max-width:600px">
        <div class="flex-between mb-8">
          <strong>📹 Videos de referencia — ${esh(marcaActual().nombre || '')}</strong>
          <button class="btn-close" onclick="closeModal('modalRefVid')">✕</button>
        </div>
        <div class="msg" id="msgRefVid"></div>
        ${seccion('producto', prod, '🏭 Productos reales (base)',
            'Videos reales de los fustes de la marca. Sirven de referencia de estilo y movimiento para los videos que se generen.',
            'Aún no hay videos de producto. Sube clips reales de los fustes plásticos (blanco hueso).')}
        ${seccion('creatividad', crea, '🎨 Creatividad / comparación',
            'Solo inspiración de estilo, transiciones o ejemplos de comparación. No se copia color ni forma del producto.',
            'Aún no hay videos de creatividad/comparación.')}
        <p class="text-dim text-sm">MP4/MOV/WEBM, máx 60 MB por video.</p>
      </div>`;
}

window.subirRefVideo = async (cat) => {
    const inp = document.getElementById(`refVidFile_${cat}`);
    if (!inp.files || !inp.files.length) { showMsg('Elige uno o más videos.', 'error', '#msgRefVid'); return; }
    const btn = document.getElementById(`btnSubirVid_${cat}`); setLoading(btn, true);
    let ok = 0, fail = 0;
    for (const f of inp.files) {
        try {
            const fd = new FormData(); fd.append('file', f);
            const res = await fetch(`/api/contenido/marcas/${State.marcaId}/referencias-video?categoria=${cat}`, { method: 'POST', credentials: 'include', body: fd });
            if (res.ok) ok++; else fail++;
        } catch { fail++; }
    }
    setLoading(btn, false);
    showMsg(`Subidos ${ok}${fail ? ', ' + fail + ' con error' : ''}.`, fail ? 'error' : 'ok', '#msgRefVid');
    await cargarRefsVideo();
};

window.moverRefVideo = async (desde, hacia, name) => {
    try {
        await apiPost(`/api/contenido/marcas/${State.marcaId}/referencias-video/${encodeURIComponent(name)}/mover?desde=${desde}&hacia=${hacia}`, {});
        await cargarRefsVideo();
    } catch (err) { showMsg(err.message, 'error', '#msgRefVid'); }
};

window.eliminarRefVideo = async (cat, name) => {
    try {
        await apiFetch(`/api/contenido/marcas/${State.marcaId}/referencias-video/${encodeURIComponent(name)}?categoria=${cat}`, { method: 'DELETE' });
        await cargarRefsVideo();
    } catch (err) { showMsg(err.message, 'error', '#msgRefVid'); }
};
render();
