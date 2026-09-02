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
let tabActiva   = 'digitalizar';

// Filas dinámicas para carga manual
let manualMoldes = [{ codigoMolde:'', primera:0, segunda:0, defectuosas:0 }];
let manualMateriales = [];
let manualObreros = [];              // [{id, nombre}] empleados de producción
let manualParticipantes = new Set(); // ids de obreros marcados
let manualMaterial = 'Virgen';

// Chips de obreros reutilizables (tab manual y revisión del digitalizador)
function obrerosChipsHTML() {
    if (!manualObreros.length)
        return '<div class="input-label" style="color:var(--texto-dim)">No hay empleados de producción activos. Agrégalos en Nómina.</div>';
    return `<div style="display:flex;flex-wrap:wrap;gap:8px">` + manualObreros.map(o => {
        const on = manualParticipantes.has(Number(o.id));
        return `<label class="chk-obrero" data-id="${o.id}" style="display:flex;align-items:center;gap:6px;padding:6px 10px;border:1px solid var(--borde);border-radius:20px;cursor:pointer;background:${on?'var(--cyan-dim)':'var(--fondo-inp)'}">
            <input type="checkbox" class="ob-chk" value="${o.id}" ${on?'checked':''} style="width:16px;height:16px">
            <span>${esh(o.nombre)}</span></label>`;
    }).join('') + `</div>`;
}

// ── Render principal ──────────────────────────────────────────
function render() {
    app.innerHTML = `
        ${renderTopbar('Producción')}
        <div class="content" style="padding-bottom:80px">
          <div style="display:flex;gap:0;border-bottom:2px solid var(--borde);margin-bottom:0">
            <button type="button" class="js-tab" data-id="digitalizar"
              style="flex:1;padding:10px;border:none;cursor:pointer;font-size:.82rem;font-weight:600;font-family:inherit;
                     background:${tabActiva==='digitalizar'?'var(--fondo-card)':'transparent'};
                     color:${tabActiva==='digitalizar'?'var(--cyan)':'var(--texto-dim)'};
                     border-bottom:3px solid ${tabActiva==='digitalizar'?'var(--cyan)':'transparent'};transition:.15s">📷 Digitalizar</button>
            <button type="button" class="js-tab" data-id="manual"
              style="flex:1;padding:10px;border:none;cursor:pointer;font-size:.82rem;font-weight:600;font-family:inherit;
                     background:${tabActiva==='manual'?'var(--fondo-card)':'transparent'};
                     color:${tabActiva==='manual'?'var(--cyan)':'var(--texto-dim)'};
                     border-bottom:3px solid ${tabActiva==='manual'?'var(--cyan)':'transparent'};transition:.15s">✏ Carga Manual</button>
            <button type="button" class="js-tab" data-id="historial"
              style="flex:1;padding:10px;border:none;cursor:pointer;font-size:.82rem;font-weight:600;font-family:inherit;
                     background:${tabActiva==='historial'?'var(--fondo-card)':'transparent'};
                     color:${tabActiva==='historial'?'var(--cyan)':'var(--texto-dim)'};
                     border-bottom:3px solid ${tabActiva==='historial'?'var(--cyan)':'transparent'};transition:.15s">📋 Historial</button>
          </div>
          <div style="padding:8px 16px;text-align:right">
            <a href="/api/produccion/planilla" target="_blank" class="btn btn-ghost btn-sm">🖨 Imprimir planilla</a>
          </div>
          <div id="tabContent"></div>
        </div>
        ${renderNav('produccion')}`;

    setupTabs();
    renderActiveTab();
}

function setupTabs() {
    document.querySelectorAll('.js-tab').forEach(btn => {
        btn.addEventListener('click', () => {
            tabActiva = btn.dataset.id;
            render();
        });
    });
}

function renderActiveTab() {
    const zona = document.getElementById('tabContent');
    if (!zona) return;
    if (tabActiva === 'digitalizar') renderTabDigitalizar(zona);
    else if (tabActiva === 'manual') renderTabManual(zona);
    else if (tabActiva === 'historial') renderTabHistorial(zona);
}

// ════════════════════════════════════════════════════════════════
// TAB: DIGITALIZAR (flujo OCR existente)
// ════════════════════════════════════════════════════════════════
function renderTabDigitalizar(zona) {
    zona.innerHTML = `
        <div class="pasos">
            <div class="paso ${pasoActual>=1?'activo':''} ${pasoActual>1?'ok':''}" id="p1">1 · Foto</div>
            <div class="paso ${pasoActual>=2?'activo':''} ${pasoActual>2?'ok':''}" id="p2">2 · Revisar</div>
            <div class="paso ${pasoActual>=3?'activo':''}" id="p3">3 · Listo</div>
        </div>
        <div id="zona-paso"></div>`;
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
    if (!zona) return;
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
        contenido += `
        <div class="card">
            <div class="card-title">Obreros que participaron</div>
            <div id="p2-obreros">${obrerosChipsHTML()}</div>
        </div>`;
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
        const [ms, ins, obr, ult] = await Promise.all([
            apiGet('/api/produccion/moldes').catch(() => []),
            apiGet('/api/produccion/insumos').catch(() => []),
            apiGet('/api/produccion/obreros').catch(() => []),
            apiGet('/api/produccion/ultimo').catch(() => null),
        ]);
        moldesDB = ms || [];
        insumosDB = ins || [];
        manualObreros = obr || [];
        manualParticipantes = new Set(((ult && ult.participantes) || []).map(Number));
        document.getElementById('zona-paso').innerHTML = htmlPaso2();
    } catch { /* sin catálogos, igual funciona */ }

    document.getElementById('p2-obreros')?.addEventListener('change', e => {
        if (!e.target.classList.contains('ob-chk')) return;
        const id = Number(e.target.value);
        if (e.target.checked) { manualParticipantes.add(id); e.target.closest('.chk-obrero').style.background = 'var(--cyan-dim)'; }
        else { manualParticipantes.delete(id); e.target.closest('.chk-obrero').style.background = 'var(--fondo-inp)'; }
    });
    document.getElementById('btn-volver')?.addEventListener('click', () => mostrarPaso(1));
    document.getElementById('btn-confirmar')?.addEventListener('click', confirmar);
}

async function confirmar() {
    const btn = document.getElementById('btn-confirmar');
    if (btn) { btn.disabled = true; btn.textContent = 'Guardando…'; }

    try {
        const fecha = document.getElementById('f-fecha')?.value?.trim() || datosExtra.fecha;

        if (tipoDoc === 'planilla') {
            // La planilla revisada se guarda por el MISMO endpoint canónico que la carga manual:
            // así el kardex valorizado y el bono por participantes son idénticos por cualquier vía.
            const material = document.getElementById('f-material')?.value || 'Virgen';
            const moldes = [];
            document.querySelectorAll('.molde-sel').forEach((sel, i) => {
                if (!sel.value) return;
                const p1  = +document.querySelector(`.m-p1[data-idx="${i}"]`)?.value || 0;
                const p2  = +document.querySelector(`.m-p2[data-idx="${i}"]`)?.value || 0;
                const rec = +document.querySelector(`.m-rec[data-idx="${i}"]`)?.value || 0;
                const def = +document.querySelector(`.m-def[data-idx="${i}"]`)?.value || 0;
                // Modelo canónico: un material por día. En día Reciclado la "primera" lleva el conteo de reciclado.
                const primera = material === 'Reciclado' ? (rec > 0 ? rec : p1) : p1;
                moldes.push({ codigoMolde: sel.value, primera, segunda: p2, defectuosas: def });
            });
            if (moldes.length === 0) throw new Error('Selecciona al menos un molde.');

            const materiales = [];
            document.querySelectorAll('.ins-sel').forEach((sel, i) => {
                const kg = +document.querySelector(`.mat-kg[data-idx="${i}"]`)?.value || 0;
                if (sel.value && kg > 0) materiales.push({ codigoInsumo: sel.value, kg });
            });

            const participantes = Array.from(manualParticipantes);
            if (participantes.length === 0) throw new Error('Marca al menos un obrero que participó.');

            const payload = {
                fecha, material,
                operario:      document.getElementById('f-operario')?.value || null,
                turno:         document.getElementById('f-turno')?.value    || null,
                observaciones: document.getElementById('f-obs')?.value      || null,
                moldes, materiales, participantes,
            };
            const data = await apiPost('/api/produccion/registro', payload);
            if (data.error) throw new Error(data.error);
            mostrarPaso(3);
            return;
        }

        // Factura de compra: sigue por el digitalizador (OCR de factura, no producción).
        const payload = {
            ...datosExtra,
            fecha,
            numero_factura: document.getElementById('f-nfactura')?.value || null,
            proveedor:      document.getElementById('f-proveedor')?.value || null,
        };
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
    pasoActual  = 1;
    mostrarPaso(1);
}

function initPaso3() {
    document.getElementById('btn-reiniciar')?.addEventListener('click', reiniciar);
}

// ════════════════════════════════════════════════════════════════
// TAB: CARGA MANUAL
// ════════════════════════════════════════════════════════════════
async function renderTabManual(zona) {
    zona.innerHTML = '<div class="loading-msg"><div class="spinner"></div> Cargando…</div>';
    let ultimo = { material:'Virgen', participantes:[], moldes:[] };
    try {
        const [ms, ins, obr, ult] = await Promise.all([
            apiGet('/api/produccion/moldes').catch(()=>[]),
            apiGet('/api/produccion/insumos').catch(()=>[]),
            apiGet('/api/produccion/obreros').catch(()=>[]),
            apiGet('/api/produccion/ultimo').catch(()=>null),
        ]);
        moldesDB = ms || [];
        insumosDB = ins || [];
        manualObreros = obr || [];
        if (ult) ultimo = ult;
    } catch {}

    // Preselección desde el último registro
    manualMaterial = ultimo.material || 'Virgen';
    manualParticipantes = new Set((ultimo.participantes||[]).map(Number));
    if (ultimo.moldes && ultimo.moldes.length)
        manualMoldes = ultimo.moldes.map(cod => ({ codigoMolde: cod, primera:0, segunda:0, defectuosas:0 }));
    else if (!manualMoldes.length)
        manualMoldes = [{ codigoMolde:'', primera:0, segunda:0, defectuosas:0 }];

    const hoy = new Date().toISOString().slice(0,10);
    const opsMolde = moldesDB.map(m => {
        const prod = m.codigoProducto ? ` [${esh(m.codigoProducto)}]` : '';
        return `<option value="${esh(m.codigo)}">${esh(m.codigo)} — ${esh(m.descripcion)}${prod}</option>`;
    }).join('');
    const opsIns = insumosDB.map(x => `<option value="${esh(x.codigo)}">${esh(x.descripcion)}</option>`).join('');

    function selMolde(cod, idx) {
        return `<select class="mm-molde" data-idx="${idx}" style="min-width:130px">
            <option value="">— selecciona —</option>${opsMolde.replace(
                cod ? `value="${esh(cod)}"` : '###NONE###',
                cod ? `value="${esh(cod)}" selected` : '###NONE###')}
        </select>`;
    }
    function filasMoldeHTML() {
        return manualMoldes.map((mm,i)=>`<tr>
            <td>${selMolde(mm.codigoMolde,i)}</td>
            <td><input type="number" class="mm-p1" data-idx="${i}" min="0" value="${mm.primera||0}" style="width:60px"></td>
            <td><input type="number" class="mm-p2" data-idx="${i}" min="0" value="${mm.segunda||0}" style="width:60px"></td>
            <td><input type="number" class="mm-def" data-idx="${i}" min="0" value="${mm.defectuosas||0}" style="width:60px;color:var(--rojo)"></td>
            <td><button type="button" class="mm-del btn btn-ghost btn-sm" data-idx="${i}" ${manualMoldes.length<=1?'disabled':''} style="padding:2px 6px">✕</button></td>
        </tr>`).join('');
    }
    function filasMatHTML() {
        return manualMateriales.map((mm,i)=>`<tr>
            <td><select class="mmat-ins" data-idx="${i}" style="min-width:110px">
                <option value="">— selecciona —</option>${opsIns.replace(
                    mm.codigoInsumo ? `value="${esh(mm.codigoInsumo)}"` : '###NONE###',
                    mm.codigoInsumo ? `value="${esh(mm.codigoInsumo)}" selected` : '###NONE###')}
            </select></td>
            <td><input type="number" class="mmat-kg" data-idx="${i}" min="0" step="0.1" value="${mm.kg||0}" style="width:80px"></td>
            <td><button type="button" class="mmat-del btn btn-ghost btn-sm" data-idx="${i}" style="padding:2px 6px">✕</button></td>
        </tr>`).join('');
    }
    function obrerosHTML() {
        if (!manualObreros.length)
            return '<div class="input-label" style="color:var(--texto-dim)">No hay empleados de producción activos. Agrégalos en Nómina.</div>';
        return `<div style="display:flex;flex-wrap:wrap;gap:8px">` + manualObreros.map(o => {
            const on = manualParticipantes.has(Number(o.id));
            return `<label class="chk-obrero" data-id="${o.id}" style="display:flex;align-items:center;gap:6px;padding:6px 10px;border:1px solid var(--borde);border-radius:20px;cursor:pointer;background:${on?'var(--cyan-dim)':'var(--fondo-inp)'}">
                <input type="checkbox" class="ob-chk" value="${o.id}" ${on?'checked':''} style="width:16px;height:16px">
                <span>${esh(o.nombre)}</span></label>`;
        }).join('') + `</div>`;
    }

    zona.innerHTML = `
    <div class="card">
        <div class="card-title">Datos del día</div>
        <div class="form-grid-2">
            <div class="input-group">
                <label class="input-label">Fecha</label>
                <input class="input-field" type="date" id="man-fecha" value="${hoy}">
            </div>
            <div class="input-group">
                <label class="input-label">Material usado</label>
                <select class="input-field" id="man-material">
                    <option value="Virgen" ${manualMaterial==='Virgen'?'selected':''}>Virgen</option>
                    <option value="Reciclado" ${manualMaterial==='Reciclado'?'selected':''}>Reciclado</option>
                </select>
            </div>
            <div class="input-group">
                <label class="input-label">Turno</label>
                <select class="input-field" id="man-turno">
                    <option value="">— Seleccionar —</option>
                    <option value="Mañana">Mañana</option>
                    <option value="Tarde">Tarde</option>
                    <option value="Noche">Noche</option>
                </select>
            </div>
        </div>
    </div>

    <div class="card">
        <div class="card-title">Obreros que participaron</div>
        <div id="man-obreros">${obrerosHTML()}</div>
    </div>

    <div class="card">
        <div class="flex-between" style="margin-bottom:8px">
            <div class="card-title" style="margin:0">Moldes producidos</div>
            <button type="button" class="btn btn-ghost btn-sm" id="man-add-molde">+ Agregar molde</button>
        </div>
        <div style="overflow-x:auto">
        <table class="tbl">
            <thead><tr><th>Molde [producto]</th><th>1ª Cal.</th><th>2ª Cal.</th><th style="color:var(--rojo)">Def.</th><th></th></tr></thead>
            <tbody id="man-moldes-body">${filasMoldeHTML()}</tbody>
        </table>
        </div>
    </div>

    <div class="card">
        <div class="flex-between" style="margin-bottom:8px">
            <div class="card-title" style="margin:0">Materiales consumidos</div>
            <button type="button" class="btn btn-ghost btn-sm" id="man-add-mat">+ Agregar material</button>
        </div>
        <div style="overflow-x:auto">
        <table class="tbl">
            <thead><tr><th>Insumo</th><th>Kg</th><th></th></tr></thead>
            <tbody id="man-mat-body">${filasMatHTML()}</tbody>
        </table>
        </div>
    </div>

    <div class="card">
        <div class="input-group">
            <label class="input-label">Observaciones</label>
            <textarea class="input-field" id="man-obs" rows="2" style="resize:vertical" placeholder="Opcional"></textarea>
        </div>
    </div>

    <div id="alerta-manual" style="margin-bottom:8px"></div>
    <button class="btn btn-cyan btn-full" id="man-guardar">Guardar producción del día</button>`;

    function syncMoldesFromDOM() {
        manualMoldes = [];
        document.querySelectorAll('.mm-molde').forEach((sel,i)=>{
            manualMoldes.push({
                codigoMolde: sel.value,
                primera: +(document.querySelector(`.mm-p1[data-idx="${i}"]`)?.value)||0,
                segunda: +(document.querySelector(`.mm-p2[data-idx="${i}"]`)?.value)||0,
                defectuosas: +(document.querySelector(`.mm-def[data-idx="${i}"]`)?.value)||0,
            });
        });
    }
    function syncMatFromDOM() {
        manualMateriales = [];
        document.querySelectorAll('.mmat-ins').forEach((sel,i)=>{
            manualMateriales.push({ codigoInsumo: sel.value, kg:+(document.querySelector(`.mmat-kg[data-idx="${i}"]`)?.value)||0 });
        });
    }
    function refreshMoldes(){ document.getElementById('man-moldes-body').innerHTML=filasMoldeHTML(); bindMolde(); }
    function refreshMat(){ document.getElementById('man-mat-body').innerHTML=filasMatHTML(); bindMat(); }
    function bindMolde(){
        document.querySelectorAll('.mm-del').forEach(b=>b.addEventListener('click',()=>{ syncMoldesFromDOM(); const idx=+b.dataset.idx; if(manualMoldes.length>1) manualMoldes.splice(idx,1); refreshMoldes(); }));
    }
    function bindMat(){
        document.querySelectorAll('.mmat-del').forEach(b=>b.addEventListener('click',()=>{ syncMatFromDOM(); manualMateriales.splice(+b.dataset.idx,1); refreshMat(); }));
    }
    document.getElementById('man-obreros').addEventListener('change', e=>{
        if(!e.target.classList.contains('ob-chk')) return;
        const id=Number(e.target.value);
        if(e.target.checked){ manualParticipantes.add(id); e.target.closest('.chk-obrero').style.background='var(--cyan-dim)'; }
        else { manualParticipantes.delete(id); e.target.closest('.chk-obrero').style.background='var(--fondo-inp)'; }
    });
    document.getElementById('man-material').addEventListener('change', e=>{ manualMaterial=e.target.value; });
    document.getElementById('man-add-molde').addEventListener('click',()=>{ syncMoldesFromDOM(); manualMoldes.push({codigoMolde:'',primera:0,segunda:0,defectuosas:0}); refreshMoldes(); });
    document.getElementById('man-add-mat').addEventListener('click',()=>{ syncMatFromDOM(); manualMateriales.push({codigoInsumo:'',kg:0}); refreshMat(); });
    bindMolde(); bindMat();
    document.getElementById('man-guardar').addEventListener('click', guardarManual);
}

async function guardarManual() {
    const btn = document.getElementById('man-guardar');
    const alerta = document.getElementById('alerta-manual');
    alerta.innerHTML='';
    const moldes=[];
    document.querySelectorAll('.mm-molde').forEach((sel,i)=>{
        moldes.push({ codigoMolde: sel.value,
            primera:+(document.querySelector(`.mm-p1[data-idx="${i}"]`)?.value)||0,
            segunda:+(document.querySelector(`.mm-p2[data-idx="${i}"]`)?.value)||0,
            defectuosas:+(document.querySelector(`.mm-def[data-idx="${i}"]`)?.value)||0 });
    });
    const materiales=[];
    document.querySelectorAll('.mmat-ins').forEach((sel,i)=>{
        const kg=+(document.querySelector(`.mmat-kg[data-idx="${i}"]`)?.value)||0;
        if(sel.value && kg>0) materiales.push({ codigoInsumo:sel.value, kg });
    });
    const validMoldes = moldes.filter(m=>m.codigoMolde);
    if(validMoldes.length===0){ alerta.innerHTML='<div class="alerta-err">Debe seleccionar al menos un molde.</div>'; return; }
    const participantes = Array.from(manualParticipantes);
    if(participantes.length===0){ alerta.innerHTML='<div class="alerta-err">Marca al menos un obrero que participó.</div>'; return; }
    const payload = {
        fecha: document.getElementById('man-fecha')?.value || null,
        material: document.getElementById('man-material')?.value || 'Virgen',
        turno: document.getElementById('man-turno')?.value || null,
        observaciones: document.getElementById('man-obs')?.value || null,
        moldes: validMoldes, materiales, participantes,
    };
    btn.disabled=true; btn.textContent='Guardando…';
    try {
        const data = await apiPost('/api/produccion/registro', payload);
        if(data.error) throw new Error(data.error);
        alerta.innerHTML = `<div class="alerta-ok">✓ Guardado: ${esh(String(data.insertados||0))} molde(s) y ${esh(String(participantes.length))} obrero(s) para ${esh(data.fecha||'')}</div>`;
        btn.textContent='Guardar producción del día'; btn.disabled=false;
    } catch(err){
        alerta.innerHTML = `<div class="alerta-err">⚠ ${esh(err.message)}</div>`;
        btn.textContent='Guardar producción del día'; btn.disabled=false;
    }
}

// ════════════════════════════════════════════════════════════════
// TAB: HISTORIAL
// ════════════════════════════════════════════════════════════════
async function renderTabHistorial(zona) {
    const hoy = new Date().toISOString().slice(0,10);
    const hace7 = new Date(Date.now() - 7*86400000).toISOString().slice(0,10);

    zona.innerHTML = `
    <div class="card">
        <div class="card-title">Filtrar registros</div>
        <div class="form-grid-2">
            <div class="input-group">
                <label class="input-label">Desde</label>
                <input class="input-field" type="date" id="hist-desde" value="${hace7}">
            </div>
            <div class="input-group">
                <label class="input-label">Hasta</label>
                <input class="input-field" type="date" id="hist-hasta" value="${hoy}">
            </div>
        </div>
        <div class="input-group" style="margin-top:8px">
            <label class="input-label">Buscar por operario o molde</label>
            <input class="input-field" type="text" id="hist-q" placeholder="Ej: Juan, MOLDE-01...">
        </div>
        <button class="btn btn-cyan btn-sm" id="hist-buscar" style="margin-top:8px">Buscar</button>
    </div>
    <div id="hist-lista"><div class="loading-msg"><div class="spinner"></div> Cargando registros...</div></div>`;

    document.getElementById('hist-buscar').addEventListener('click', cargarHistorial);
    cargarHistorial();
}

async function cargarHistorial() {
    const desde = document.getElementById('hist-desde')?.value || '';
    const hasta = document.getElementById('hist-hasta')?.value || '';
    const q     = document.getElementById('hist-q')?.value?.trim() || '';
    const listaDiv = document.getElementById('hist-lista');
    if (!listaDiv) return;

    listaDiv.innerHTML = '<div class="loading-msg"><div class="spinner"></div> Cargando...</div>';

    try {
        const params = new URLSearchParams();
        if (desde) params.set('desde', desde);
        if (hasta) params.set('hasta', hasta);
        if (q)     params.set('q', q);
        const registros = await apiGet('/api/produccion/registros?' + params.toString());

        if (!registros || registros.length === 0) {
            listaDiv.innerHTML = `
                <div class="empty">
                    <div class="empty-icon">📋</div>
                    <div class="empty-text">No hay registros en este rango de fechas</div>
                </div>`;
            return;
        }

        let html = '';
        let fechaActual = '';
        for (const r of registros) {
            if (r.fecha !== fechaActual) {
                fechaActual = r.fecha;
                html += `<div class="section-header" style="margin-top:12px">${esh(formatFecha(r.fecha))}</div>`;
            }
            const total = (r.primera||0) + (r.segunda||0) + (r.defectuosas||0);
            html += `
            <div class="list-item">
                <div class="list-item-main">
                    <div class="list-item-title">${esh(r.molde)}</div>
                    <div class="list-item-sub">
                        ${esh(r.operario||'—')} · ${esh(r.turno||'—')} · ${esh(r.material||'')}
                    </div>
                </div>
                <div style="text-align:right;font-size:.8rem">
                    <span class="tag tag-verde">1ª ${r.primera||0}</span>
                    <span class="tag tag-naranja">2ª ${r.segunda||0}</span>
                    <span class="tag tag-rojo">Def ${r.defectuosas||0}</span>
                    <div class="text-dim text-sm" style="margin-top:2px">Total: <span class="bold">${total}</span></div>
                </div>
            </div>`;
        }
        listaDiv.innerHTML = html;
    } catch (err) {
        listaDiv.innerHTML = `<div class="alerta-err">⚠ ${esh(err.message)}</div>`;
    }
}

// ── Arranque ──────────────────────────────────────────────
render();
