# Dashboard financiero en n8n — guía paso a paso

Son 3 pasos. Nada rompe el dashboard actual: la URL y su usuario/clave (Basic Auth) quedan igual.

**URLs que vas a usar:**
- Lista de workflows de n8n: `https://automatizacion-n8n.ru6xga.easypanel.host/home/workflows`
- Dashboard (resultado): `https://automatizacion-n8n.ru6xga.easypanel.host/webhook/dashboard`

**Archivos que vas a usar** — ya están guardados en tu equipo, en la carpeta `C:\Users\Toor\source\repos\Prueba d cld\n8n-dashboard-financiero\` (no hace falta descargar nada del chat):
- `C:\Users\Toor\source\repos\Prueba d cld\n8n-dashboard-financiero\Setup - Migracion gastos.json`
- `C:\Users\Toor\source\repos\Prueba d cld\n8n-dashboard-financiero\query-gastos.sql`
- `C:\Users\Toor\source\repos\Prueba d cld\n8n-dashboard-financiero\dashboard-code-node.js`
- `C:\Users\Toor\source\repos\Prueba d cld\n8n-dashboard-financiero\Registrar gasto.json`
- `C:\Users\Toor\source\repos\Prueba d cld\n8n-dashboard-financiero\01-migracion-gastos.sql` (alternativa manual, solo si no importas el workflow)

> Verificar que están en la carpeta (opcional, en **PowerShell 7.6.5**):
> ```powershell
> Get-ChildItem -Path 'C:\Users\Toor\source\repos\Prueba d cld\n8n-dashboard-financiero\Setup - Migracion gastos.json','C:\Users\Toor\source\repos\Prueba d cld\n8n-dashboard-financiero\query-gastos.sql','C:\Users\Toor\source\repos\Prueba d cld\n8n-dashboard-financiero\dashboard-code-node.js','C:\Users\Toor\source\repos\Prueba d cld\n8n-dashboard-financiero\Registrar gasto.json','C:\Users\Toor\source\repos\Prueba d cld\n8n-dashboard-financiero\01-migracion-gastos.sql' | Select-Object Name, Length, LastWriteTime
> ```

---

## PASO 1 — Migrar la tabla `gastos` (una sola vez)

Esto es solo **importar y ejecutar un mini-workflow** que agrega las columnas nuevas (tipo, periodicidad, próximo cobro, ámbito) y carga tus 10 gastos reales. Es idempotente (si lo corres dos veces, no duplica).

**Dónde y cómo:**

1. Abre en el navegador: `https://automatizacion-n8n.ru6xga.easypanel.host/home/workflows`
2. Arriba a la derecha, haz clic en el botón naranja **"Create Workflow"**. Se abre un editor de workflow vacío (lienzo en blanco).
3. En ese editor vacío, arriba a la derecha, haz clic en el ícono de **tres puntos (⋮)** (está a la derecha del botón "Save" y del interruptor "Inactive/Active"). Se despliega un menú.
4. En ese menú, haz clic en **"Import from File…"**. Se abre el explorador de archivos de Windows.
5. En el explorador, ve a la carpeta `C:\Users\Toor\source\repos\Prueba d cld\n8n-dashboard-financiero\`, selecciona **`Setup - Migracion gastos.json`** y haz clic en **Abrir**.
6. Aparecen 2 nodos en el lienzo: **"Ejecutar una vez"** → **"Migrar tabla gastos"**.
7. Revisa el nodo **"Migrar tabla gastos"**: si muestra un aviso rojo de credencial, haz doble clic en él, en el campo **"Credential to connect with"** elige **"Postgres account"** y luego haz clic en la flecha **‹ Back to canvas** (arriba a la izquierda).
8. Haz clic en el botón **"Execute workflow"** (barra inferior, al centro del lienzo). Si los dos nodos quedan con un **check verde**, la migración terminó.
9. Ya cumplió su función: puedes **borrar** este workflow (menú ⋮ → **Delete**) o dejarlo, da igual.

> Alternativa sin importar: si tienes la consola SQL de tu base (Neon/EasyPanel), abre `C:\Users\Toor\source\repos\Prueba d cld\n8n-dashboard-financiero\01-migracion-gastos.sql`, copia todo su contenido, pégalo en la consola y ejecútalo. Es exactamente lo mismo.

---

## PASO 2 — Actualizar el workflow "Dashboard - Gastos y Propuestas"

**No lo reimportes** (perderías la URL). Solo editas 2 nodos dentro de él.

**Dónde y cómo:**

1. Abre `https://automatizacion-n8n.ru6xga.easypanel.host/home/workflows` y haz clic en el workflow **"Dashboard - Gastos y Propuestas"** para abrirlo.

2. **Editar el nodo "Query gastos":**
   - Haz doble clic en el nodo **"Query gastos"**.
   - En el campo grande **"Query"**, haz clic dentro, selecciona todo con **Ctrl+A** y borra con **Supr**.
   - Abre `C:\Users\Toor\source\repos\Prueba d cld\n8n-dashboard-financiero\query-gastos.sql`, copia **todo** su contenido y pégalo en ese campo con **Ctrl+V**.
   - Haz clic en la flecha **‹ Back to canvas** (arriba a la izquierda).

3. **Editar el nodo "Construir HTML del dashboard":**
   - Haz doble clic en el nodo **"Construir HTML del dashboard"**.
   - En el campo de código **"JavaScript"**, haz clic dentro, **Ctrl+A** para seleccionar todo y **Supr** para borrar.
   - Abre `C:\Users\Toor\source\repos\Prueba d cld\n8n-dashboard-financiero\dashboard-code-node.js`, copia **todo** su contenido y pégalo con **Ctrl+V**.
   - Haz clic en la flecha **‹ Back to canvas**.

4. Arriba a la derecha, haz clic en **"Save"** para guardar el workflow.

5. Abre `https://automatizacion-n8n.ru6xga.easypanel.host/webhook/dashboard` y recarga con **Ctrl+F5**. Ya verás la vista financiera (tarjetas de KPIs, recurrentes con próximo cobro, únicos, gasto por proyecto e ingresos vs. gastos).

---

## PASO 3 — Formulario "Registrar gasto" (para cargar gastos futuros)

**Dónde y cómo:**

1. Abre `https://automatizacion-n8n.ru6xga.easypanel.host/home/workflows`.
2. Botón naranja **"Create Workflow"** → en el editor vacío, ícono **tres puntos (⋮)** arriba a la derecha → **"Import from File…"**.
3. En el explorador, ve a `C:\Users\Toor\source\repos\Prueba d cld\n8n-dashboard-financiero\`, selecciona **`Registrar gasto.json`** y **Abrir**.
4. Revisa credenciales (si alguna está en rojo, haz doble clic en el nodo y selecciónala):
   - Nodo **"Execute a SQL query"** → credencial **"Postgres account"**.
   - Nodo **"HTTP Request"** → credencial **"Evolution API - apikey"**.
5. Arriba a la derecha, haz clic en **"Save"** y luego activa el interruptor **"Inactive" → "Active"**.
6. **Obtener el enlace de tu planilla de gastos:** el formulario que llenas para cargar un gasto vive en una dirección web. Para copiarla, haz doble clic en el nodo **"On form submission"**; n8n muestra dos enlaces: **"Test URL"** (solo funciona mientras pruebas) y **"Production URL"** (funciona siempre, con el workflow en **Active**). Copia la **"Production URL"** y guárdala en tus marcadores del navegador. Se ve así (con `/form/`, no `/form-test/`):
   `https://automatizacion-n8n.ru6xga.easypanel.host/form/e88204b3-7c05-42e7-9721-c889735340c2`
   Ese es el enlace de tu planilla: cada vez que tengas un gasto (compra de Connects, un bid, una suscripción nueva, la factura de Hetzner, etc.) abres ese enlace, llenas el formulario y el gasto aparece solo en el dashboard, además de avisarte por WhatsApp.

Campos del formulario: Categoría · Descripción · Monto · Moneda · **Tipo** (unico/recurrente) · **Periodicidad** (mensual/anual/consumo) · **Próximo cobro** · **Ámbito** (este/general/motoapp/ehp/personal) · Notas.

---

## Notas sobre los datos cargados

- **Recurrentes:** Upwork Plus ($19.99, este), Anthropic Max ($106.35, todos), Google One ($2.49, general), Paramount+ ($5.99, personal), Cloudflare dominio ($22.25/año, EHP), Hetzner CX23 (monto **por confirmar**), Vercel/Neon (**por consumo**, MotoApp).
- **Únicos (agosto):** 100 Connects ($15), créditos API Anthropic ($20.20 y $5.05).
- **Ingresos:** el dashboard cuenta como ingreso solo las propuestas con estado `aceptada`, `en_desarrollo` o `completado` en USD. Hoy: 0 ganadas. Cuando ganes un trabajo, cámbiale el estado y sumará como ingreso.
- **Actualizar el monto de Hetzner** cuando facturen (el día 28): regístralo por el formulario "Registrar gasto", o edita esa fila en la base.
