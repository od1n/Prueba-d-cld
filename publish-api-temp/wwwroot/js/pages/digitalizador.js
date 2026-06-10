// digitalizador.js
if (!Auth.requireAuth()) { /* redirige */ }

const app = document.getElementById('app');

// ── Estado global ─────────────────────────────────────────────
let pasoActual = 1;
let archivoSel = null;
let datosExtra  = null;
let moldesDB    = [];
let insumosDB   = [];
let tipoDoc     = 'planilla';

// ── Render principal ──────────────────────────────────────────
function render() {
    app.innerHTML = `
        ${renderTopbar('Digitalizar planilla')}
        <div class="content" id="content" style="padding-bottom:80px">
            <div class="pasos">
                <div class="paso ${pasoActual>=1?'activo':''} ${pasoActual>1?'ok':''}" id="p1">1 · Foto</div>
                <div class="paso ${pasoActual>=2?'activo':''} ${pasoActual>2?'ok':''}" id="p2">2 · Revisar</div>
                <div class="paso ${pasoActual>=3?'activo':''}" id="p3">3 · Listo</div>
            </div>
            <div id="zona-paso"></div>
        </div>
        ${renderNav('produccion')}`;
    mostrarPaso(pasoActual);
}

function mostrarPaso(n) {
    pasoActual = n;
    ['p1','p2','p3'].forEach((id,i)=>{
        const el = document.getElementById(id);
        if (!el) return;
        el.className = 'paso' + (i+1 < n ? ' ok' : i+1 === n ? ' activo' : '');
    });
    const zona = document.getElementById('zona-paso');
    if (n === 1) zona.innerHTML = htmlPaso1();
    else if (n === 2) zona.innerHTML = htmlPaso2();
    else zona.innerHTML = htmlPaso3();

    if (n === 1) initPaso1();
    if (n === 2) initPaso2();
    if (n === 3) initPaso3();
}

// ════════════════════════════════════════════════════════
// PASO 1 — Elegir foto y extraer
// ════════════════════════════════════════════════════════
function htmlPaso1() {
    return `
    <div class="card">
        <div class="card-title">Tipo de documento</div>
        <div class="tipo-grid">
            <button class="tipo-btn ${tipoDoc==='planilla'?'sel':''}" id="btn-tipo-planilla">
                <span class="ico">▦</span>Planilla prod.
            </button>
            <a class="tipo-btn" id="btn-tipo-egresos" href="/pages/egresos-nuevo.html"
               style="text-decoration:none;display:flex;flex-direction:column;align-items:center;justify-content:center">
                <span class="ico">⊟</span>Egresos
            </a>
        </div>
    </div>
    <div class="card">
        <div class="card-title">Foto del documento</div>
        <div id="zona-foto">
            <span style="font-size:2rem">⊡</span>
            <p>Toca para tomar foto o seleccionar imagen</p>
            <p style="font-size:.75rem;margin-top:4px">JPG · PNG · WEBP</p>
            <img id="preview">
        </div>
        <input type="file" id="input-file" accept="image/*" capture="environment">
        <div id="alerta-p1" style="margin-top:8px"></div>
    </div>
    <div id="chip-zona" style="margin-bottom:8px;text-align:right"></div>
    <div id="spinner" style="display:none">
        <div class="spinner" style="margin:0 auto 10px"></div>
        <p style="color:var(--texto-dim);font-size:.85rem;text-align:center">Analizando imagen…</p>
    </div>
    <button class="btn btn-cyan" id="btn-extraer" disabled>
        Extraer datos →
    </button>`;
}

function initPaso1() {
    cargarEstadoBackend();

    document.getElementById('zona-foto').addEventListener('click', () =>
        document.getElementById('input-file').click());
    document.getElementById('zona-foto').addEventListener('dragover', ev => {
        ev.preventDefault();
        document.getElementById('zona-foto').classList.add('drag');
    });
    document.getElementById('zona-foto').addEventListener('dragleave', () =>
        document.getElementById('zona-foto').classList.remove('drag'));
    document.getElementById('zona-foto').addEventListener('drop', onDrop);
    document.getElementById('input-file').addEventListener('change', e => onFileChange(e.target));
    document.getElementById('btn-extraer').addEventListener('click', extraer);
    document.getElementById('btn-tipo-planilla').addEventListener('click', () => setTipo('planilla'));
}

function setTipo(t) {
    tipoDoc = t;
    document.getElementById('btn-tipo-planilla').classList.toggle('sel', t === 'planilla');
}

function onFileChange(inp) {
    if (!inp.files || !inp.files[0]) return;
    archivoSel = inp.files[0];
    mostrarPreview(archivoSel);
}

function onDrop(ev) {
    ev.preventDefault();
    document.getElementById('zona-foto').classList.remove('drag');
    const f = ev.dataTransfer.files[0];
    if (!f) return;
    archivoSel = f;
    mostrarPreview(f);
}

function mostrarPreview(f) {
    const img = document.getElementById('preview');
    img.src = URL.createObjectURL(f);
    img.style.display = 'block';
    document.getElementById('btn-extraer').disabled = false;
    document.getElementById('alerta-p1').innerHTML = '';
}

async function cargarEstadoBackend() {
    const chipZona = document.getElementById('chip-zona');
    if (!chipZona) return;
    try {
        const d = await apiGet('/api/produccion/estado');
        const ok = d?.ok;
        chipZona.innerHTML = `
            <span class="chip-backend">
                <span class="chip-dot ${ok?'':'off'}"></span>
                ${esh(d?.nombre||'Desconocido')} · ${esh(d?.modelo||'')}
            </span>`;
    } catch {
        chipZona.innerHTML = `<span class="chip-backend"><span class="chip-dot off"></span>Digitalizador offline</span>`;
    }
}

async function extraer() {
    if (!archivoSel) return;
    document.getElementById('btn-extraer').disabled = true;
    document.getElementById('zona-foto').style.display = 'none';
    document.getElementById('spinner').style.display = 'block';
    document.getElementById('alerta-p1').innerHTML = '';

    try {
        const fd = new FormData();
        fd.append('imagen', archivoSel);
        fd.append('tipo', tipoDoc);

        const resp = await fetch('/api/produccion/extraer', {
            method: 'POST',
            credentials: 'include',
            body: fd
        });
        const data = await resp.json();
        if (!data.ok) throw new Error(data.error || 'Error en extracción');

        datosExtra = data.datos;
        mostrarPaso(2);
    } catch (err) {
        document.getElementById('zona-foto').style.display = 'block';
        document.getElementById('spinner').style.display = 'none';
        document.getElementById('btn-extraer').disabled = false;
        document.getElementById('alerta-p1').innerHTML =
            `<div class="alerta-err">⚠ ${esh(err.message)}</div>`;
    }
}

// ════════════════════════════════════════════════════════
// PASO 2 — Revisar y confirmar
// ════════════════════════════════════════════════════════
function htmlPaso2() {
    if (!datosExtra) return '<div class="card"><p>Sin datos</p></div>';
    const d = datosExtra;
    const esPlanilla = tipoDoc === 'planilla';

    let contenido = `
    <div class="card">
        <div class="card-title">Encabezado</div>
        <div class="input-group">
            <label class="input-label">Fecha</label>
            <input class="input-field" id="f-fecha" value="${esh(d.fecha||'')}">
        </div>`;
    if (esPlanilla) {
        contenido += `
        <div class="input-group">
            <label class="input-label">Material</label>
            <select class="input-field" id="f-material">
                <option ${d.material==='Virgen'?'selected':''}>Virgen</option>
                <option ${d.material==='Reciclado'?'selected':''}>Reciclado</option>
            </select>
        </div>
        <div class="input-group">
            <label class="input-label">Operario</label>
            <input class="input-field" id="f-operario" value="${esh(d.operario||'')}">
        </div>
        <div class="input-group">
            <label class="input-label">Turno</label>
            <input class="input-field" id="f-turno" value="${esh(d.turno||'')}">
        </div>`;
    } else {
        contenido += `
        <div class="input-group">
            <label class="input-label">N° Factura</label>
            <input class="input-field" id="f-nfactura" value="${esh(d.numero_factura||'')}">
        </div>
        <div class="input-group">
            <label class="input-label">Proveedor</label>
            <input class="input-field" id="f-proveedor" value="${esh(d.proveedor||'')}">
        </div>`;
    }
    contenido += `</div>`;

    if (esPlanilla) {
        const opsMolde = moldesDB.map(m =>
            `<option value="${esh(m.codigo)}">${esh(m.codigo)} — ${esh(m.descripcion)}</option>`
        ).join('');

        const filasMolde = (d.moldes || []).map((m, i) => {
            const codSel = moldesDB.find(x =>
                x.descripcion?.toLowerCase().includes((m.descripcion||'').toLowerCase()) ||
                x.codigo === m.descripcion)?.codigo || '';
            return `<tr>
                <td><select class="molde-sel" data-idx="${i}" style="min-width:90px">
                    <option value="">— selecciona —</option>${opsMolde.replace(
                        `value="${esh(codSel)}"`, `value="${esh(codSel)}" selected`)}
                </select></td>
                <td><input type="number" class="m-p1" data-idx="${i}" min="0" value="${+(m.primera||0)}"></td>
                <td><input type="number" class="m-p2" data-idx="${i}" min="0" value="${+(m.segunda||0)}"></td>
                <td><input type="number" class="m-rec" data-idx="${i}" min="0" value="${+(m.reciclado||0)}"></td>
                <td><input type="number" class="m-def" data-idx="${i}" min="0" value="${+(m.defectuosas||0)}" style="color:var(--rojo)"></td>
            </tr>`;
        }).join('');

        contenido += `
        <div class="card">
            <div class="card-title">Moldes</div>
            <div style="overflow-x:auto">
            <table class="tbl">
                <thead><tr>
                    <th>Molde</th><th>1ª Cal.</th><th>2ª Cal.</th><th>Recic.</th><th style="color:var(--rojo)">Def.</th>
                </tr></thead>
                <tbody>${filasMolde}</tbody>
            </table>
            </div>
        </div>`;

        const opsIns = insumosDB.map(x =>
            `<option value="${esh(x.codigo)}">${esh(x.descripcion)}</option>`
        ).join('');
        const filasMat = (d.materiales || []).map((m, i) => {
            const kg = +(m.kg || (m.sacos ? m.sacos * 25 : 0));
            return `<tr>
                <td><select class="ins-sel" data-idx="${i}" style="min-width:110px">
                    <option value="">— selecciona —</option>${opsIns}
                </select></td>
                <td><input type="number" class="mat-kg" data-idx="${i}" min="0" step="0.1" value="${kg}"></td>
            </tr>`;
        }).join('');

        if (filasMat) {
            contenido += `
            <div class="card">
                <div class="card-title">Materiales</div>
                <div style="overflow-x:auto">
                <table class="tbl">
                    <thead><tr><th>Insumo</th><th>Kg</th></tr></thead>
                    <tbody>${filasMat}</tbody>
                </table>
                </div>
            </div>`;
        }

        contenido += `
        <div class="card">
            <div class="card-title">Observaciones</div>
            <textarea class="input-field" id="f-obs" rows="2" style="resize:vertical">${esh(d.observaciones||'')}</textarea>
        </div>`;
    }

    contenido += `
    <div id="alerta-p2" style="margin-bottom:8px"></div>
    <div style="display:flex;gap:8px">
        <button class="btn btn-ghost" style="flex:1" id="btn-volver">← Volver</button>
        <button class="btn btn-cyan" style="flex:2" id="btn-confirmar">Guardar →</button>
    </div>`;

    return contenido;
}

async function initPaso2() {
    try {
        const [ms, ins] = await Promise.all([
            apiGet('/api/produccion/moldes').catch(() => []),
            apiGet('/api/produccion/insumos').catch(() => []),
        ]);
        moldesDB = ms || [];
        insumosDB = ins || [];
        document.getElementById('zona-paso').innerHTML = htmlPaso2();
    } catch { /* sin catálogos, igual funciona */ }

    document.getElementById('btn-volver')?.addEventListener('click', () => mostrarPaso(1));
    document.getElementById('btn-confirmar')?.addEventListener('click', confirmar);
}

async function confirmar() {
    const btn = document.getElementById('btn-confirmar');
    if (btn) { btn.disabled = true; btn.textContent = 'Guardando…'; }

    try {
        const fecha = document.getElementById('f-fecha')?.value?.trim() || datosExtra.fecha;

        let payload;
        if (tipoDoc === 'planilla') {
            const moldes = [];
            document.querySelectorAll('.molde-sel').forEach((sel, i) => {
                moldes.push({
                    codigo_molde: sel.value,
                    primera:      +document.querySelector(`.m-p1[data-idx="${i}"]`)?.value || 0,
                    segunda:      +document.querySelector(`.m-p2[data-idx="${i}"]`)?.value || 0,
                    reciclado:    +document.querySelector(`.m-rec[data-idx="${i}"]`)?.value || 0,
                    defectuosas:  +document.querySelector(`.m-def[data-idx="${i}"]`)?.value || 0,
                });
            });

            const materiales = [];
            document.querySelectorAll('.ins-sel').forEach((sel, i) => {
                const kg = +document.querySelector(`.mat-kg[data-idx="${i}"]`)?.value || 0;
                if (sel.value && kg > 0)
                    materiales.push({ codigo_insumo: sel.value, kg });
            });

            payload = {
                fecha,
                material:      document.getElementById('f-material')?.value || 'Virgen',
                operario:      document.getElementById('f-operario')?.value || null,
                turno:         document.getElementById('f-turno')?.value    || null,
                observaciones: document.getElementById('f-obs')?.value      || null,
                moldes,
                materiales,
            };
        } else {
            payload = {
                ...datosExtra,
                fecha,
                numero_factura: document.getElementById('f-nfactura')?.value || null,
                proveedor:      document.getElementById('f-proveedor')?.value || null,
            };
        }

        const resp = await fetch('/api/produccion/confirmar', {
            method: 'POST',
            credentials: 'include',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload),
        });
        const data = await resp.json();
        if (!data.ok) throw new Error(data.error || 'Error al guardar');

        mostrarPaso(3);
    } catch (err) {
        document.getElementById('alerta-p2').innerHTML =
            `<div class="alerta-err">⚠ ${esh(err.message)}</div>`;
        if (btn) { btn.disabled = false; btn.textContent = 'Guardar →'; }
    }
}

// ════════════════════════════════════════════════════════
// PASO 3 — Éxito
// ════════════════════════════════════════════════════════
function htmlPaso3() {
    return `
    <div class="exito">
        <span class="ico">✓</span>
        <h2>¡Guardado correctamente!</h2>
        <p>Los datos quedaron registrados en el sistema.</p>
        <br>
        <button class="btn btn-cyan" id="btn-reiniciar" style="max-width:240px;margin:0 auto">
            + Digitalizar otro
        </button>
    </div>`;
}

function reiniciar() {
    archivoSel = null;
    datosExtra  = null;
    tipoDoc     = 'planilla';
    mostrarPaso(1);
}

// initPaso3 conecta el botón al reiniciar (ya no hay onclick inline)
function initPaso3() {
    document.getElementById('btn-reiniciar')?.addEventListener('click', reiniciar);
}

// ── Arranque ──────────────────────────────────────────────
render();
