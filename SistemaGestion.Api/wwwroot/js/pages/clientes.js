// clientes.js — CRUD + CxC + Importar CSV
if (!Auth.requireAuth()) {}
const app = document.getElementById('app');

let todosClientes = [];
let timerBusq = null;

async function render() {
    app.innerHTML = `
    ${renderTopbar('Clientes')}
    <div class="content">
      <div class="search-wrap">
        <span class="search-icon">&#8981;</span>
        <input class="input-field" id="buscarCli" type="search"
          placeholder="Nombre, RIF, codigo..." autocomplete="off" oninput="filtrarClientes(this.value)">
      </div>
      <div style="display:flex;gap:8px;padding:0 16px 10px;flex-wrap:wrap">
        <button class="btn btn-primary" onclick="abrirNuevo()">+ Nuevo cliente</button>
        <button class="btn btn-ghost" onclick="abrirImportCsv()">&#128196; Importar CSV</button>
      </div>
      <div id="listaCli"><div class="loading-msg"><div class="spinner"></div></div></div>
    </div>
    <div class="modal-overlay" id="modalCliente">
      <div class="modal-drawer" id="drawerCliente" style="max-height:95vh;overflow-y:auto"></div>
    </div>
    <div class="modal-overlay" id="modalCxc">
      <div class="modal-drawer" id="drawerCxc"></div>
    </div>
    <div class="modal-overlay" id="modalImport">
      <div class="modal-drawer" id="drawerImport" style="max-height:95vh;overflow-y:auto"></div>
    </div>
    ${renderNav('clientes')}`;
    await cargarClientes();
}

async function cargarClientes() {
    try {
        todosClientes = await apiGet('/api/clientes') || [];
        renderListaClientes(todosClientes);
    } catch(err) {
        document.getElementById('listaCli').innerHTML =
            `<div class="alert-banner">Error: ${esh(err.message)}</div>`;
    }
}

function filtrarClientes(q) {
    clearTimeout(timerBusq);
    timerBusq = setTimeout(() => {
        if (!q.trim()) { renderListaClientes(todosClientes); return; }
        const lc = q.toLowerCase();
        const filtrados = todosClientes.filter(c =>
            (c.nombre||'').toLowerCase().includes(lc) ||
            (c.ciRif||'').toLowerCase().includes(lc) ||
            (c.codigo||'').toLowerCase().includes(lc));
        renderListaClientes(filtrados);
    }, 300);
}

function renderListaClientes(lista) {
    const div = document.getElementById('listaCli');
    if (!div) return;
    if (lista.length === 0) {
        div.innerHTML = `<div class="empty"><div class="empty-icon">&#128100;</div>
            <div class="empty-text">Sin clientes</div></div>`;
        return;
    }
    div.innerHTML = `<div class="card" style="padding:0">
        ${lista.map((c,ix) => `
        <button type="button" class="list-item js-cli-btn" data-idx="${ix}"
          style="padding:12px 16px;background:none;border:none;border-bottom:1px solid var(--borde);
                 text-align:left;width:100%;cursor:pointer;color:inherit;font:inherit">
          <div class="list-item-main">
            <div class="list-item-title">${esh(c.nombre||'(sin nombre)')}</div>
            <div class="list-item-sub">${esh(c.ciRif||'')} &middot; ${esh(c.codigo||'')}</div>
          </div>
          <div style="text-align:right;flex-shrink:0">
            ${c.telefono ? '<div class="text-dim text-sm">'+esh(c.telefono)+'</div>' : ''}
            ${(c.saldoPendiente||0) > 0 ? '<div class="mono" style="color:var(--naranja);font-size:10pt">Debe: '+formatUSD(c.saldoPendiente)+'</div>' : ''}
          </div>
        </button>`).join('')}
    </div>`;
    div.querySelectorAll('.js-cli-btn').forEach(btn => {
        btn.addEventListener('click', () => abrirDetalle(lista[+btn.dataset.idx]));
    });
}

// -- Detalle cliente (ver/editar) -----------------------------------------
function abrirDetalle(c) {
    renderFormCliente(c, false);
}
function abrirNuevo() {
    renderFormCliente(null, true);
}

function renderFormCliente(c, esNuevo) {
    const drawer = document.getElementById('drawerCliente');
    const titulo = esNuevo ? 'Nuevo Cliente' : 'Editar Cliente';
    drawer.innerHTML = `
    <div class="modal-handle"></div>
    <div class="modal-title">${titulo}</div>
    <div class="form-group mb-8">
      <label class="form-label">Codigo</label>
      <input id="cCodigo" class="input-field" value="${esh(c?.codigo||'')}" ${esNuevo?'':'readonly'}>
    </div>
    <div class="form-group mb-8">
      <label class="form-label">Nombre / Razon social</label>
      <input id="cNombre" class="input-field" value="${esh(c?.nombre||'')}">
    </div>
    <div class="form-group mb-8">
      <label class="form-label">CI / RIF</label>
      <input id="cRif" class="input-field" value="${esh(c?.ciRif||'')}">
    </div>
    <div class="form-group mb-8">
      <label class="form-label">Telefono</label>
      <input id="cTelefono" class="input-field" value="${esh(c?.telefono||'')}">
    </div>
    <div class="form-group mb-8">
      <label class="form-label">Direccion</label>
      <textarea id="cDireccion" class="input-field" rows="2">${esh(c?.direccion||'')}</textarea>
    </div>
    <div class="form-group mb-8">
      <label class="form-label">Email</label>
      <input id="cEmail" class="input-field" type="email" value="${esh(c?.email||'')}">
    </div>
    <div class="form-group mb-12">
      <label class="form-label">Persona de contacto</label>
      <input id="cContacto" class="input-field" value="${esh(c?.contacto||'')}">
    </div>
    <div id="msgCli"></div>
    <div style="display:flex;flex-direction:column;gap:8px">
      <button class="btn btn-primary btn-full" onclick="guardarCliente(${esNuevo?'true':'false'})">Guardar</button>
      ${!esNuevo ? `<button class="btn btn-ghost btn-full" onclick="verCxc('${esh(c?.codigo||'')}')">Ver cuentas por cobrar</button>` : ''}
      ${!esNuevo ? `<button class="btn btn-full" style="background:var(--rojo);color:#fff" onclick="eliminarCliente('${esh(c?.codigo||'')}')">Eliminar</button>` : ''}
      <button class="btn btn-ghost btn-full" onclick="closeModal('modalCliente')">Cancelar</button>
    </div>`;
    openModal('modalCliente');
}

async function guardarCliente(esNuevo) {
    const body = {
        codigo:    document.getElementById('cCodigo')?.value?.trim() || '',
        nombre:    document.getElementById('cNombre')?.value?.trim() || '',
        ciRif:     document.getElementById('cRif')?.value?.trim() || '',
        telefono:  document.getElementById('cTelefono')?.value?.trim() || '',
        direccion: document.getElementById('cDireccion')?.value?.trim() || '',
        email:     document.getElementById('cEmail')?.value?.trim() || '',
        contacto:  document.getElementById('cContacto')?.value?.trim() || ''
    };
    if (!body.codigo || !body.nombre) { alert('Codigo y nombre son obligatorios.'); return; }
    try {
        if (esNuevo) {
            await apiPost('/api/clientes', body);
        } else {
            await apiPost(`/api/clientes/${encodeURIComponent(body.codigo)}`, body, 'PUT');
        }
        closeModal('modalCliente');
        await cargarClientes();
        alert(esNuevo ? 'Cliente creado.' : 'Cliente actualizado.');
    } catch(err) { alert('Error: '+err.message); }
}

async function eliminarCliente(codigo) {
    if (!confirm('Eliminar cliente '+codigo+'? Esta accion no se puede deshacer.')) return;
    try {
        await apiPost(`/api/clientes/${encodeURIComponent(codigo)}`, {}, 'DELETE');
        closeModal('modalCliente');
        await cargarClientes();
        alert('Cliente eliminado.');
    } catch(err) { alert('Error: '+err.message); }
}

// -- CxC del cliente ------------------------------------------------------
async function verCxc(codigo) {
    closeModal('modalCliente');
    const drawer = document.getElementById('drawerCxc');
    drawer.innerHTML = `<div class="modal-handle"></div>
        <div class="modal-title">Cuentas por cobrar</div><div class="spinner"></div>`;
    openModal('modalCxc');
    try {
        const cxc = await apiGet(`/api/cxc?cliente=${encodeURIComponent(codigo)}`) || [];
        if (cxc.length === 0) {
            drawer.innerHTML = `<div class="modal-handle"></div>
                <div class="modal-title">Cuentas por cobrar</div>
                <div class="empty-text text-dim">Sin cuentas pendientes</div>
                <button class="btn btn-ghost btn-full" style="margin-top:12px" onclick="closeModal('modalCxc')">Cerrar</button>`;
            return;
        }
        const totalDeuda = cxc.reduce((s,c)=>s+(c.saldo||0),0);
        drawer.innerHTML = `<div class="modal-handle"></div>
            <div class="modal-title">Cuentas por cobrar</div>
            <div class="card mb-12" style="padding:12px">
              <div class="flex-between">
                <span class="text-dim">Total deuda</span>
                <span class="mono bold" style="color:var(--naranja)">${formatUSD(totalDeuda)}</span>
              </div>
            </div>
            <div class="card" style="padding:0">
              ${cxc.map(c => `
              <div class="list-item" style="padding:10px 14px;border-bottom:1px solid var(--borde)">
                <div class="list-item-main">
                  <div class="list-item-title">${esh(c.numeroFactura||'')}</div>
                  <div class="list-item-sub">${formatFecha(c.fecha)} &middot; Vence: ${formatFecha(c.fechaVence)}</div>
                </div>
                <div style="text-align:right;flex-shrink:0">
                  <div class="mono">${formatUSD(c.monto||0)}</div>
                  <div class="text-dim text-sm">Saldo: ${formatUSD(c.saldo||0)}</div>
                  ${estaVencida(c.fechaVence) ? '<span class="tag tag-rojo" style="font-size:8pt">Vencida</span>' : ''}
                </div>
              </div>`).join('')}
            </div>
            <button class="btn btn-ghost btn-full" style="margin-top:12px" onclick="closeModal('modalCxc')">Cerrar</button>`;
    } catch(err) {
        drawer.innerHTML = `<div class="modal-handle"></div>
            <div class="alert-banner">Error: ${esh(err.message)}</div>
            <button class="btn btn-ghost btn-full" onclick="closeModal('modalCxc')">Cerrar</button>`;
    }
}

// -- Importar CSV ---------------------------------------------------------
function abrirImportCsv() {
    const drawer = document.getElementById('drawerImport');
    drawer.innerHTML = `
    <div class="modal-handle"></div>
    <div class="modal-title">Importar Clientes (CSV)</div>
    <div class="text-dim text-sm mb-12">
      Sube un archivo CSV exportado desde Excel con las columnas:<br>
      <strong>Codigo, Nombre, CiRif, Telefono, Direccion, Email, Contacto</strong><br>
      La primera fila debe ser encabezados.
    </div>
    <div class="form-group mb-12">
      <input type="file" id="csvFile" accept=".csv,.txt" class="input-field"
        onchange="cargarArchivoCsv(this)">
    </div>
    <div id="csvPreview"></div>
    <div id="csvActions" style="display:none;flex-direction:column;gap:8px;margin-top:12px">
      <button class="btn btn-primary btn-full" id="btnImportar" onclick="importarCsv()">Importar</button>
    </div>
    <button class="btn btn-ghost btn-full" style="margin-top:8px" onclick="closeModal('modalImport')">Cancelar</button>`;
    openModal('modalImport');
}

let csvData = [];
function cargarArchivoCsv(input) {
    const file = input.files[0];
    if (!file) return;
    const reader = new FileReader();
    reader.onload = (e) => previsualizarCsv(e.target.result);
    reader.readAsText(file, 'UTF-8');
}

function previsualizarCsv(texto) {
    const lineas = texto.split(/\r?\n/).filter(l => l.trim());
    if (lineas.length < 2) { alert('El archivo debe tener al menos 2 lineas (encabezado + datos).'); return; }
    const headers = parseCsvLine(lineas[0]).map(h => h.trim().toLowerCase());
    csvData = [];
    for (let i = 1; i < lineas.length; i++) {
        const vals = parseCsvLine(lineas[i]);
        const obj = {};
        headers.forEach((h,j) => obj[h] = (vals[j]||'').trim());
        csvData.push({
            codigo:    obj.codigo    || obj['código']   || obj.code   || '',
            nombre:    obj.nombre    || obj.name        || obj['razón social'] || obj['razon social'] || '',
            ciRif:     obj.cirif     || obj.rif         || obj.ci     || obj['ci/rif'] || '',
            telefono:  obj.telefono  || obj['teléfono'] || obj.phone  || '',
            direccion: obj.direccion || obj['dirección']|| obj.address|| '',
            email:     obj.email     || obj.correo      || '',
            contacto:  obj.contacto  || obj.contact     || ''
        });
    }
    csvData = csvData.filter(c => c.codigo || c.nombre);
    const prev = document.getElementById('csvPreview');
    prev.innerHTML = `
    <div class="text-dim text-sm mb-8">${csvData.length} clientes encontrados:</div>
    <div class="card" style="padding:0;max-height:250px;overflow-y:auto">
      ${csvData.slice(0,20).map(c => `
      <div class="list-item" style="padding:8px 12px;border-bottom:1px solid var(--borde)">
        <div class="list-item-main">
          <div class="list-item-title">${esh(c.nombre)}</div>
          <div class="list-item-sub">${esh(c.codigo)} &middot; ${esh(c.ciRif)}</div>
        </div>
      </div>`).join('')}
      ${csvData.length > 20 ? `<div class="text-dim text-sm" style="padding:8px 12px">...y ${csvData.length-20} mas</div>` : ''}
    </div>`;
    document.getElementById('csvActions').style.display = 'flex';
}

function parseCsvLine(line) {
    const result = []; let current = ''; let inQuotes = false;
    for (let i = 0; i < line.length; i++) {
        const ch = line[i];
        if (inQuotes) {
            if (ch === '"' && line[i+1] === '"') { current += '"'; i++; }
            else if (ch === '"') { inQuotes = false; }
            else { current += ch; }
        } else {
            if (ch === '"') { inQuotes = true; }
            else if (ch === ',' || ch === ';') { result.push(current); current = ''; }
            else { current += ch; }
        }
    }
    result.push(current);
    return result;
}

async function importarCsv() {
    if (!csvData.length) { alert('No hay datos para importar.'); return; }
    const btn = document.getElementById('btnImportar');
    if (btn) { btn.disabled = true; btn.textContent = 'Importando...'; }
    try {
        const resp = await apiPost('/api/clientes/importar-csv', { clientes: csvData });
        closeModal('modalImport');
        await cargarClientes();
        alert(`Importacion completada. ${resp.importados||0} importados, ${resp.errores||0} errores.`);
    } catch(err) {
        if (btn) { btn.disabled=false; btn.textContent='Importar'; }
        alert('Error: '+err.message);
    }
}

render();
