# Exportar el manual a PDF

> Cómo convertir todos los archivos `.md` del manual en un único PDF para distribuir o imprimir.

## Estructura del manual

El manual está organizado en carpetas numeradas dentro de `manual/`:

```
manual/
├── 00_introduccion.md
├── 00_introduccion/
│   ├── FrmLogin.md
│   ├── FrmCambioContrasena.md
│   ├── FrmAlertas.md
│   └── FrmBuscador.md
├── 01_dashboard/
│   ├── FrmDashboardV2.md
│   └── FrmDashboard.md
├── 02_transacciones/
│   └── FrmFacturas.md
├── 04_produccion/
├── 05_nomina/
├── 06_reportes/
├── 07_tasa_cambio/
│   ├── FrmTasaCambio.md
│   ├── FrmActualizarTasa.md
│   ├── FrmTrazabilidadTasa.md
│   └── FrmTasaCambio_Respaldos.md
├── 08_sistema/
│   ├── FrmSistema.md
│   ├── FrmRespaldos.md
│   ├── FrmPerfil.md
│   ├── FrmLogAuditoria.md
│   └── FrmUsuarios.md
├── 99_anexos/
│   ├── roles_permisos.md
│   ├── atajos.md
│   ├── glosario.md
│   └── exportar_pdf.md
├── INVENTARIO.md
├── PLANTILLA.md
└── README.md
```

Para que el PDF salga ordenado, los archivos se concatenan **en orden alfabético/numérico**: `00 → 01 → 02 → ... → 99`.

---

## Opción A — Pandoc (recomendada para PDF unificado)

[Pandoc](https://pandoc.org/) es la herramienta más potente para convertir Markdown a PDF. Soporta tablas, encabezados, código, etc.

### Requisitos

- Instalar **Pandoc**: <https://pandoc.org/installing.html>
- Instalar un motor LaTeX (recomendado: **MikTeX** en Windows): <https://miktex.org/download>

### Comando concreto (Windows / PowerShell)

Desde la carpeta `manual/`:

```powershell
pandoc `
  00_introduccion.md `
  (Get-ChildItem 00_introduccion/*.md   | Sort-Object Name) `
  (Get-ChildItem 01_dashboard/*.md      | Sort-Object Name) `
  (Get-ChildItem 02_transacciones/*.md  | Sort-Object Name) `
  (Get-ChildItem 03_archivos/*.md       | Sort-Object Name) `
  (Get-ChildItem 04_produccion/*.md     | Sort-Object Name) `
  (Get-ChildItem 05_nomina/*.md         | Sort-Object Name) `
  (Get-ChildItem 06_reportes/*.md       | Sort-Object Name) `
  (Get-ChildItem 07_tasa_cambio/*.md    | Sort-Object Name) `
  (Get-ChildItem 08_sistema/*.md        | Sort-Object Name) `
  (Get-ChildItem 99_anexos/*.md         | Sort-Object Name) `
  -o Manual_SistemaGestion.pdf `
  --toc --toc-depth=2 `
  -V geometry:margin=2cm -V fontsize=10pt -V documentclass=report
```

### Comando equivalente en bash / Git Bash

```bash
pandoc \
  00_introduccion.md \
  00_introduccion/*.md \
  01_dashboard/*.md \
  02_transacciones/*.md \
  04_produccion/*.md \
  05_nomina/*.md \
  06_reportes/*.md \
  07_tasa_cambio/*.md \
  08_sistema/*.md \
  99_anexos/*.md \
  -o Manual_SistemaGestion.pdf \
  --toc --toc-depth=2 \
  -V geometry:margin=2cm -V fontsize=10pt
```

### Opciones útiles

| Opción | Para qué sirve |
|---|---|
| `--toc` | Genera índice automático |
| `--toc-depth=2` | Hasta nivel H2 en el índice |
| `-V geometry:margin=2cm` | Márgenes de 2 cm |
| `-V fontsize=10pt` | Tamaño base de la fuente |
| `-V mainfont="Calibri"` | Fuente principal (requiere XeLaTeX o LuaLaTeX: agregar `--pdf-engine=xelatex`) |
| `--pdf-engine=xelatex` | Motor LaTeX que soporta tildes y emojis mejor |
| `-V documentclass=report` | Usa la clase "report" (con capítulos) |

> Si no querés instalar LaTeX, usá `-o Manual.html` para generar HTML, y desde el navegador *Imprimir → Guardar como PDF*.

---

## Opción B — Typora o MarkText (más visual)

Editores Markdown gráficos que tienen "Exportar a PDF" integrado.

### Typora ([typora.io](https://typora.io/))

1. Abrir Typora.
2. **File → Open File**: abrir cada `.md` o crear un único `.md` consolidado (ver Opción C).
3. **File → Export → PDF**.

### MarkText ([marktext.app](https://www.marktext.app/))

1. Abrir MarkText (gratuito y código abierto).
2. Abrir el archivo `.md` deseado.
3. **File → Export → Export to PDF**.

> Estos editores son ideales para revisar **una pantalla concreta** o exportar un capítulo. Para el manual completo conviene Pandoc.

---

## Opción C — Concatenar y luego imprimir

Si querés un PDF rápido sin instalar Pandoc:

### 1. Concatenar todo en un único `.md`

**PowerShell:**

```powershell
$carpetas = @("00_introduccion","01_dashboard","02_transacciones","03_archivos",
              "04_produccion","05_nomina","06_reportes","07_tasa_cambio",
              "08_sistema","99_anexos")
$archivos = @("00_introduccion.md") +
  ($carpetas | ForEach-Object { Get-ChildItem "$_\*.md" | Sort-Object Name } | Select-Object -ExpandProperty FullName)
Get-Content $archivos | Set-Content Manual_completo.md
```

**Bash / Git Bash:**

```bash
cat 00_introduccion.md \
    00_introduccion/*.md \
    01_dashboard/*.md \
    02_transacciones/*.md \
    07_tasa_cambio/*.md \
    08_sistema/*.md \
    99_anexos/*.md \
    > Manual_completo.md
```

### 2. Abrir en navegador y imprimir

1. Abrir `Manual_completo.md` con un visor de Markdown:
   - **VS Code** (con vista previa): `Ctrl + Shift + V`.
   - **Navegador** con extensión Markdown Viewer.
   - **Typora / MarkText**.
2. **Imprimir** (`Ctrl + P`).
3. **Destino**: *Microsoft Print to PDF* (Windows) o *Guardar como PDF*.
4. **Guardar**.

---

## Recomendación

| Caso | Mejor opción |
|---|---|
| PDF profesional para distribuir | **Pandoc** con LaTeX (Opción A) |
| Revisar / imprimir un capítulo | **Typora** o **MarkText** (Opción B) |
| PDF rápido sin instalar nada extra | **Concatenar + Imprimir** (Opción C) |

## Tips

- **Verificá tildes y caracteres especiales** — Pandoc por defecto usa pdflatex que puede tener problemas con `ñ`, `á`, etc. Si pasa, agregá `--pdf-engine=xelatex` al comando.
- **Tamaño del PDF**: el manual completo suele quedar entre 60 y 120 páginas dependiendo del motor.
- **Imágenes**: si en el futuro agregás imágenes a las fichas, asegurate que las rutas sean **relativas** desde la carpeta `manual/` para que Pandoc las encuentre.
- **Versionado**: dejá el comando completo guardado en un script (`exportar_manual.ps1` o `.sh`) para regenerar el PDF cada vez que actualices las fichas.
