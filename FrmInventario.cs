using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace SistemaGestion
{
    public class FrmInventario : FormBase
    {
        private DataGridView dgv;
        private Label        lblResumen, lblModo;
        private TextBox      txtBuscar;
        private Button       btnModoEdicion;
        private bool         _modoEdicion = false;

        private string           _sortCol = "Codigo";
        private bool             _sortAsc = true;
        private List<Inventario> _todos   = new();

        private readonly Dictionary<(int, int), object> _valoresPrevios = new();

        public FrmInventario()
        {
            Text          = "Inventario";
            Size          = new Size(1150, 620);
            StartPosition = FormStartPosition.CenterParent;
            BackColor     = Color.FromArgb(240, 244, 250);

            // ── Header ────────────────────────────────────────────────────
            var hdr = new Panel { Dock = DockStyle.Top, Height = 50,
                BackColor = Color.FromArgb(26, 74, 138) };
            hdr.Controls.Add(new Label {
                Text = "Inventario de Productos", ForeColor = Color.White,
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                AutoSize = true, Location = new Point(12, 13) });

            var btnImprimir = Btn("Imprimir",     Color.FromArgb(80, 60, 140));
            var btnNuevo  = Btn("+ Nuevo",      Color.FromArgb(20, 140, 70));
            var btnEditar = Btn("Editar",        Color.FromArgb(60, 100, 180));
            var btnElim   = Btn("Archivar",      Color.FromArgb(140, 80, 20));
            btnModoEdicion = Btn("Modo Edición", Color.FromArgb(100, 60, 20));
            btnModoEdicion.Width   = 120;
            btnNuevo.Visible       = Sesion.EsAdmin;
            btnElim.Visible        = Sesion.EsAdmin;
            btnModoEdicion.Visible = Sesion.EsAdmin;

            
            var _btnAyuda = new Button {
                Text = "? Ayuda", Size = new Size(80, 32),
                BackColor = Color.FromArgb(80, 50, 150), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Cursor = Cursors.Hand, Margin = new Padding(0, 0, 4, 0)
            };
            _btnAyuda.FlatAppearance.BorderSize = 0;
            _btnAyuda.Click += (s, e) => AyudaService.Mostrar(this, "FrmInventario");
            var pnlHdrRight = new FlowLayoutPanel {
                Dock = DockStyle.Right, AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false, BackColor = Color.Transparent,
                Padding = new Padding(0, 9, 8, 0)
            };
            pnlHdrRight.Controls.AddRange(new Control[] { _btnAyuda, btnImprimir, btnNuevo, btnEditar, btnElim, btnModoEdicion });
            hdr.Controls.Add(pnlHdrRight);
            // ── ToolTips ─────────────────────────────────────────────────
            var _tip = AyudaService.CrearTooltip();
            AyudaService.Tip(_tip, _btnAyuda, "Abrir la guía de ayuda de este módulo");
            AyudaService.Tip(_tip, btnImprimir, "Imprimir el listado de inventario actual");
            AyudaService.Tip(_tip, btnNuevo, "Crear un nuevo producto en el catálogo de inventario");
            AyudaService.Tip(_tip, btnEditar, "Modificar los datos del producto seleccionado en la lista");
            AyudaService.Tip(_tip, btnElim, "Archivar el producto — no se elimina, solo se desactiva");
            AyudaService.Tip(_tip, btnModoEdicion, "Activar edición directa en la tabla (doble clic en celda)");

            // ── Barra busqueda ─────────────────────────────────────────────
            var barSearch = new Panel { Dock = DockStyle.Top, Height = 38,
                BackColor = Color.FromArgb(232, 240, 250) };
            barSearch.Controls.Add(new Label { Text = "Buscar:",
                Location = new Point(10, 10), AutoSize = true,
                Font = new Font("Segoe UI", 9f) });
            txtBuscar = new TextBox { Location = new Point(65, 7), Size = new Size(280, 24) };
            txtBuscar.TextChanged += (s, e) => Filtrar();
            barSearch.Controls.Add(txtBuscar);

            lblModo = new Label { Text = "", Location = new Point(360, 10),
                AutoSize = true, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(140, 60, 0) };
            barSearch.Controls.Add(lblModo);
            barSearch.Controls.Add(new Label {
                Text = "* Rojo = sin stock  |  Amarillo = bajo mínimo",
                Location = new Point(750, 10), AutoSize = true,
                ForeColor = Color.FromArgb(120, 60, 0),
                Font = new Font("Segoe UI", 8.5f, FontStyle.Italic) });

            // ── Grid ──────────────────────────────────────────────────────
            dgv = CrearGrid(Color.FromArgb(26, 74, 138));
            dgv.Name = "dgv";
            dgv.DataBindingComplete     += (s, e) => ColorearStock();
            dgv.ColumnHeaderMouseClick  += DgvColumnHeaderMouseClick;
            dgv.CellBeginEdit           += Dgv_CellBeginEdit;
            dgv.CellEndEdit             += Dgv_CellEndEdit;
            dgv.CellDoubleClick         += (s, e) => { if (!_modoEdicion) Editar(); };

            // ── Footer ─────────────────────────────────────────────────────
            var footer = new Panel { Dock = DockStyle.Bottom, Height = 40,
                BackColor = Color.FromArgb(240, 244, 250) };
            lblResumen = new Label { AutoSize = true, Location = new Point(12, 10),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(20, 80, 40) };
            var btnCerrar = BtnFooter("Cerrar", footer, new Point(1020, 6));
            btnCerrar.Click += (s, e) => Close();
            footer.Controls.AddRange(new Control[] { lblResumen, btnCerrar });

            Controls.AddRange(new Control[] { dgv, barSearch, hdr, footer });

            btnImprimir.Click    += (s, e) => ImprimirInventario();
            btnNuevo.Click       += (s, e) => Nuevo();
            btnEditar.Click      += (s, e) => Editar();
            btnElim.Click        += (s, e) => Eliminar();
            btnModoEdicion.Click += (s, e) => ToggleModoEdicion();

            VentanaPrefs.Restaurar(this);

            var timerPrefs = new System.Windows.Forms.Timer { Interval = 800 };
            timerPrefs.Tick     += (s, e) => { timerPrefs.Stop(); VentanaPrefs.Guardar(this); };
            SizeChanged         += (s, e) => { timerPrefs.Stop(); timerPrefs.Start(); };
            LocationChanged     += (s, e) => { timerPrefs.Stop(); timerPrefs.Start(); };
            FormClosed          += (s, e) => { timerPrefs.Stop(); timerPrefs.Dispose(); };

            CargarDatos();
        }

        // ══════════════════════════════════════════════════════════════════
        //  CRUD
        // ══════════════════════════════════════════════════════════════════
        private void Nuevo()
        {
            using var d = new DlgInventario(null, _todos);
            if (d.ShowDialog() == DialogResult.OK)
            { DatabaseService.InsertarInventario(d.Resultado); CargarDatos(); }
        }

        private void Editar()
        {
            if (dgv.CurrentRow == null) return;
            var cod  = dgv.CurrentRow.Cells["Codigo"].Value?.ToString();
            var item = _todos.Find(x => x.Codigo == cod);
            if (item == null) return;
            using var d = new DlgInventario(item, _todos);
            if (d.ShowDialog() == DialogResult.OK)
            { DatabaseService.ActualizarInventario(d.Resultado); CargarDatos(); }
        }

        private void Eliminar()
        {
            if (dgv.CurrentRow == null) return;
            var cod = dgv.CurrentRow.Cells["Codigo"].Value?.ToString();
            if (MessageBox.Show($"Eliminar producto {cod}?", "Confirmar",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            { DatabaseService.EliminarInventario(cod); CargarDatos(); }
        }

        // ══════════════════════════════════════════════════════════════════
        //  MODO EDICION INLINE (solo admin)
        // ══════════════════════════════════════════════════════════════════
        private void ToggleModoEdicion()
        {
            if (!Sesion.EsAdmin)
            {
                MessageBox.Show("Solo el administrador puede usar el modo de edicion.",
                    "Acceso restringido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _modoEdicion = !_modoEdicion;

            if (_modoEdicion)
            {
                Filtrar(); // BindGrid en modo edicion configura todo

                btnModoEdicion.BackColor = Color.FromArgb(180, 60, 20);
                btnModoEdicion.Text      = "Salir Edicion";
                lblModo.Text             = "⚠  MODO EDICION — haz clic en una celda para modificar";
            }
            else
            {
                dgv.ReadOnly      = true;
                dgv.EditMode      = DataGridViewEditMode.EditProgrammatically;
                dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

                btnModoEdicion.BackColor = Color.FromArgb(100, 60, 20);
                btnModoEdicion.Text      = "Modo Edicion";
                lblModo.Text             = "";

                CargarDatos();
            }
        } // end ToggleModoEdicion

        private void Dgv_CellBeginEdit(object sender, DataGridViewCellCancelEventArgs e)
        {
            if (!_modoEdicion) { e.Cancel = true; return; }
            _valoresPrevios[(e.RowIndex, e.ColumnIndex)] =
                dgv.Rows[e.RowIndex].Cells[e.ColumnIndex].Value;
        }

        private void Dgv_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (!_modoEdicion || e.RowIndex < 0 || e.ColumnIndex < 0) return;

            string col        = dgv.Columns[e.ColumnIndex].Name;
            var    fila       = dgv.Rows[e.RowIndex];
            string codigo     = fila.Cells["Codigo"].Value?.ToString();
            string valorNuevo = fila.Cells[e.ColumnIndex].Value?.ToString() ?? "";

            // Columnas de solo lectura — no guardar
            if (col == "Codigo" || col == "Actualizado") return;

            // Validacion: campos numericos no negativos
            if (new[] { "CostoActual","Precio","Existencia","Min","Max" }.Contains(col))
            {
                if (!decimal.TryParse(valorNuevo,
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.CurrentCulture,
                    out decimal num) || num < 0)
                {
                    Revertir(e.RowIndex, e.ColumnIndex,
                        $"El campo '{col}' debe ser un número mayor o igual a 0.");
                    return;
                }
            }

            if (!DatabaseService.ActualizarCampoInventario(
                    codigo, col, valorNuevo, out var error))
                Revertir(e.RowIndex, e.ColumnIndex,
                    $"No se pudo guardar '{col}'.\n{error}");
        }

        private void Revertir(int row, int col, string msg)
        {
            if (_valoresPrevios.TryGetValue((row, col), out var previo))
                dgv.Rows[row].Cells[col].Value = previo;
            MessageBox.Show(msg, "Error de edicion",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        // ══════════════════════════════════════════════════════════════════
        //  ORDENAMIENTO
        // ══════════════════════════════════════════════════════════════════
        private void DgvColumnHeaderMouseClick(object sender,
            DataGridViewCellMouseEventArgs e)
        {
            string col = dgv.Columns[e.ColumnIndex].Name;
            var permitidas = new[] {
                "Codigo","Descripcion","Departamento","Unidad",
                "CostoActual","Precio","Existencia","Min","Max","Proveedor"
            };
            if (!permitidas.Contains(col)) return;
            if (_sortCol == col) _sortAsc = !_sortAsc;
            else { _sortCol = col; _sortAsc = true; }
            Filtrar();
        }

        private List<Inventario> Ordenar(List<Inventario> lista) => _sortCol switch
        {
            "Descripcion"  => _sortAsc ? lista.OrderBy(i => i.Descripcion).ToList()
                                       : lista.OrderByDescending(i => i.Descripcion).ToList(),
            "Departamento" => _sortAsc ? lista.OrderBy(i => i.Departamento).ToList()
                                       : lista.OrderByDescending(i => i.Departamento).ToList(),
            "Unidad"       => _sortAsc ? lista.OrderBy(i => i.Unidad).ToList()
                                       : lista.OrderByDescending(i => i.Unidad).ToList(),
            "CostoActual"  => _sortAsc ? lista.OrderBy(i => i.CostoActual).ToList()
                                       : lista.OrderByDescending(i => i.CostoActual).ToList(),
            "Precio"       => _sortAsc ? lista.OrderBy(i => i.Precio).ToList()
                                       : lista.OrderByDescending(i => i.Precio).ToList(),
            "Existencia"   => _sortAsc ? lista.OrderBy(i => i.Existencia).ToList()
                                       : lista.OrderByDescending(i => i.Existencia).ToList(),
            "Min"          => _sortAsc ? lista.OrderBy(i => i.ExistenMin).ToList()
                                       : lista.OrderByDescending(i => i.ExistenMin).ToList(),
            "Max"          => _sortAsc ? lista.OrderBy(i => i.ExistenMax).ToList()
                                       : lista.OrderByDescending(i => i.ExistenMax).ToList(),
            "Proveedor"    => _sortAsc ? lista.OrderByNatural(i => i.CodProveedor).ToList()
                                       : lista.OrderByDescendingNatural(i => i.CodProveedor).ToList(),
            // Default: orden NATURAL por código (3 antes de 10, "03" como 3)
            _              => _sortAsc ? lista.OrderByNatural(i => i.Codigo).ToList()
                                       : lista.OrderByDescendingNatural(i => i.Codigo).ToList()
        };

        // ══════════════════════════════════════════════════════════════════
        //  CARGA / FILTRADO / BIND
        // ══════════════════════════════════════════════════════════════════
        private void CargarDatos()
        {
            _todos = DatabaseService.ObtenerInventario();
            Filtrar();
            lblResumen.Text =
                $"  Productos: {_todos.Count}   |   " +
                $"Bajo mínimo: {_todos.Count(x => x.Existencia < x.ExistenMin)}   |   " +
                $"Sin existencia: {_todos.Count(x => x.Existencia == 0)}";
        }

        private void Filtrar()
        {
            var q = txtBuscar.Text;
            // Búsqueda flexible (tolera ceros a la izquierda en códigos numéricos)
            var lista = string.IsNullOrWhiteSpace(q) ? _todos
                : _todos.Where(i => BusquedaHelper.ContieneFlexibleEnAlguno(q,
                        i.Codigo, i.Descripcion, i.Departamento,
                        i.CodProveedor)).ToList();

            lista = Ordenar(lista);
            BindGrid(lista);
        }

        private void BindGrid(List<Inventario> lista)
        {
            if (_modoEdicion)
            {
                // Modo edicion: grid NO enlazado (unbound) — evita restricciones de DataSource
                dgv.DataSource = null;
                dgv.Columns.Clear();

                // Definir columnas manualmente
                var cols = new[]
                {
                    ("Codigo",       "Codigo",        true,  80),
                    ("Descripcion",  "Descripcion",   false, 160),
                    ("Departamento", "Departamento",  false, 100),
                    ("Unidad",       "Unidad",        false, 60),
                    ("CostoActual",  "Costo Actual",  false, 80),
                    ("Precio",       "Precio",        false, 80),
                    ("Existencia",   "Existencia",    false, 80),
                    ("Min",          "Min",           false, 60),
                    ("Max",          "Max",           false, 60),
                    ("PesoKg",       "Peso (kg)",     false, 70),
                    ("BonoProdBs",   "Bono Prod (Bs)",true,  90),
                    ("BonoVentaBs",  "Bono Venta (Bs)",true, 90),
                    ("Proveedor",    "Proveedor",     false, 80),
                    ("Actualizado",  "Actualizado",   true,  90),
                };

                foreach (var (name, hdr, ro, w) in cols)
                {
                    var col = new DataGridViewTextBoxColumn {
                        Name = name, HeaderText = hdr, ReadOnly = ro,
                        SortMode = DataGridViewColumnSortMode.Programmatic
                    };
                    // Resaltar columnas editables con fondo diferente
                    if (!ro)
                        col.DefaultCellStyle.BackColor = Color.FromArgb(255, 255, 240);
                    dgv.Columns.Add(col);
                }

                dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                dgv.ReadOnly      = false;
                dgv.EditMode      = DataGridViewEditMode.EditOnKeystrokeOrF2;
                dgv.SelectionMode = DataGridViewSelectionMode.CellSelect;

                foreach (var i in lista)
                    dgv.Rows.Add(
                        i.Codigo, i.Descripcion, i.Departamento, i.Unidad,
                        i.CostoActual, i.Precio, i.Existencia,
                        i.ExistenMin, i.ExistenMax, i.PesoKg,
                        i.BonoProduccionBs, i.BonoVentaBs, i.CodProveedor,
                        i.UltimaActualz.ToShortDateString());
            }
            else
            {
                // Modo consulta: tipo anonimo enlazado (readonly)
                dgv.Columns.Clear();
                dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                dgv.DataSource = null;
                dgv.DataSource = lista.Select(i => new {
                    i.Codigo, i.Descripcion, i.Departamento, i.Unidad,
                    CostoActual = i.CostoActual.ToString("N2"),
                    Precio      = i.Precio.ToString("N2"),
                    Existencia  = i.Existencia.ToString("N2"),
                    Min         = i.ExistenMin.ToString("N2"),
                    Max         = i.ExistenMax.ToString("N2"),
                    PesoKg      = i.PesoKg.ToString("N4"),
                    BonoProdBs  = i.BonoProduccionBs.ToString("N2"),
                    BonoVentaBs = i.BonoVentaBs.ToString("N2"),
                    Proveedor   = i.CodProveedor,
                    Actualizado = i.UltimaActualz.ToShortDateString()
                }).ToList();
                if (dgv.Columns.Contains("BonoProdBs"))
                    dgv.Columns["BonoProdBs"].HeaderText = "Bono Prod (Bs)";
                if (dgv.Columns.Contains("BonoVentaBs"))
                    dgv.Columns["BonoVentaBs"].HeaderText = "Bono Venta (Bs)";
                VentanaPrefs.AplicarAnchos(this, dgv);
            }

            // Glyphs de ordenamiento
            foreach (DataGridViewColumn col in dgv.Columns)
                col.HeaderCell.SortGlyphDirection = SortOrder.None;
            if (dgv.Columns.Contains(_sortCol))
                dgv.Columns[_sortCol].HeaderCell.SortGlyphDirection =
                    _sortAsc ? SortOrder.Ascending : SortOrder.Descending;
        }

        private void ColorearStock()
        {
            foreach (DataGridViewRow row in dgv.Rows)
            {
                if (row.Cells["Existencia"].Value == null) continue;
                decimal.TryParse(row.Cells["Existencia"].Value?.ToString(), out decimal ex);
                decimal.TryParse(row.Cells["Min"].Value?.ToString(), out decimal min);
                if (ex == 0)       row.DefaultCellStyle.BackColor = Color.FromArgb(255, 210, 210);
                else if (ex < min) row.DefaultCellStyle.BackColor = Color.FromArgb(255, 245, 190);
            }
        }

        // ══════════════════════════════════════════════════════════════════
        //  IMPRIMIR
        // ══════════════════════════════════════════════════════════════════
        private void ImprimirInventario()
        {
            // Datos visibles (respeta filtro de búsqueda)
            var q = txtBuscar.Text;
            var lista = string.IsNullOrWhiteSpace(q) ? _todos
                : _todos.Where(i => BusquedaHelper.ContieneFlexibleEnAlguno(q,
                    i.Codigo, i.Descripcion, i.Departamento, i.CodProveedor)).ToList();
            lista = Ordenar(lista);

            if (lista.Count == 0)
            {
                MessageBox.Show("No hay productos para imprimir.", "Imprimir",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            int filasPorPag = 38;
            int totalPags   = (int)Math.Ceiling(lista.Count / (double)filasPorPag);
            int pagActual   = 0;

            var pd = new PrintDocument();
            pd.DefaultPageSettings.Landscape = true;
            pd.DefaultPageSettings.Margins   = new Margins(30, 30, 40, 40);

            pd.PrintPage += (s, e) =>
            {
                var g = e.Graphics;
                int y = e.MarginBounds.Top;
                int x = e.MarginBounds.Left;
                int W = e.MarginBounds.Width;

                var C1 = Color.FromArgb(26, 74, 138);

                // Título
                g.FillRectangle(new SolidBrush(C1), new Rectangle(x, y, W, 28));
                g.DrawString("INVENTARIO DE PRODUCTOS",
                    new Font("Segoe UI", 12f, FontStyle.Bold), Brushes.White,
                    new RectangleF(x + 8, y + 5, W, 22));
                g.DrawString($"Pág {pagActual + 1}/{totalPags}  —  {DateTime.Now:dd/MM/yyyy HH:mm}",
                    new Font("Segoe UI", 8f), Brushes.White,
                    new RectangleF(x + W - 220, y + 8, 220, 16),
                    new StringFormat { Alignment = StringAlignment.Far });
                y += 32;

                if (!string.IsNullOrWhiteSpace(q))
                {
                    g.DrawString($"Filtro: \"{q}\"  ({lista.Count} resultados)",
                        new Font("Segoe UI", 8f, FontStyle.Italic), Brushes.Gray, x, y);
                    y += 16;
                }

                // Encabezados
                string[] hdrs = { "Código", "Descripción", "Depto", "Unidad", "Costo", "Precio", "Exist.", "Mín", "Máx", "Proveedor" };
                int[] colW    = { 65, 200, 90, 55, 75, 75, 60, 55, 55, 75 };

                g.FillRectangle(new SolidBrush(C1), new Rectangle(x, y, W, 20));
                int xc = x;
                var fHdr = new Font("Segoe UI", 7.5f, FontStyle.Bold);
                for (int i = 0; i < hdrs.Length; i++)
                {
                    g.DrawString(hdrs[i], fHdr, Brushes.White,
                        new RectangleF(xc + 2, y + 3, colW[i] - 4, 16));
                    xc += colW[i];
                }
                y += 22;

                // Filas
                var fDato  = new Font("Segoe UI", 7.5f);
                int inicio = pagActual * filasPorPag;
                int fin    = Math.Min(inicio + filasPorPag, lista.Count);

                for (int r = inicio; r < fin; r++)
                {
                    var it = lista[r];
                    Color bg = (r - inicio) % 2 == 0 ? Color.White : Color.FromArgb(240, 248, 255);
                    if (it.Existencia == 0)       bg = Color.FromArgb(255, 220, 220);
                    else if (it.Existencia < it.ExistenMin) bg = Color.FromArgb(255, 248, 200);

                    g.FillRectangle(new SolidBrush(bg), new Rectangle(x, y, W, 16));

                    string[] vals = {
                        it.Codigo, it.Descripcion, it.Departamento, it.Unidad,
                        it.CostoActual.ToString("N2"), it.Precio.ToString("N2"),
                        it.Existencia.ToString("N2"),
                        it.ExistenMin.ToString("N0"), it.ExistenMax.ToString("N0"),
                        it.CodProveedor ?? ""
                    };
                    xc = x;
                    for (int i = 0; i < vals.Length; i++)
                    {
                        g.DrawString(vals[i], fDato, Brushes.Black,
                            new RectangleF(xc + 2, y + 1, colW[i] - 4, 14));
                        xc += colW[i];
                    }
                    y += 16;
                }

                // Resumen en última página
                if (pagActual == totalPags - 1)
                {
                    y += 6;
                    g.FillRectangle(new SolidBrush(Color.FromArgb(232, 240, 250)),
                        new Rectangle(x, y, W, 20));
                    g.DrawString(
                        $"Total productos: {lista.Count}   |   " +
                        $"Sin existencia: {lista.Count(p => p.Existencia == 0)}   |   " +
                        $"Bajo mínimo: {lista.Count(p => p.Existencia > 0 && p.Existencia < p.ExistenMin)}",
                        new Font("Segoe UI", 8f, FontStyle.Bold), Brushes.Black, x + 6, y + 3);
                }

                pagActual++;
                e.HasMorePages = pagActual < totalPags;
            };

            using var ppd = new PrintPreviewDialog { Document = pd, WindowState = FormWindowState.Maximized };
            ppd.ShowDialog(this);
        }

        // ══════════════════════════════════════════════════════════════════
        //  HELPERS UI
        // ══════════════════════════════════════════════════════════════════
        private DataGridView CrearGrid(Color headerColor)
        {
            var g = new DataGridView {
                Dock = DockStyle.Fill, ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false, AllowUserToAddRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
                BackgroundColor = Color.White, BorderStyle = BorderStyle.None,
                RowHeadersVisible = false, Font = new Font("Segoe UI", 9f),
                EditMode = DataGridViewEditMode.EditProgrammatically
            };
            g.ColumnHeadersDefaultCellStyle.BackColor = headerColor;
            g.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            g.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            g.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(240, 250, 243);
            g.EnableHeadersVisualStyles = false;
            return g;
        }

        private Button Btn(string t, Color c, Point p)
        {
            var b = new Button { Text = t, Size = new Size(90, 33), Location = p,
                BackColor = c, ForeColor = Color.White, FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), Cursor = Cursors.Hand };
            b.FlatAppearance.BorderSize = 0; return b;
        }

        private Button Btn(string t, Color c, Color? fore = null)
        {
            var b = new Button { Text = t, Size = new Size(90, 32),
                BackColor = c, ForeColor = fore ?? Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 4, 0) };
            b.FlatAppearance.BorderSize = 0; return b;
        }

        private Button BtnFooter(string t, Panel par, Point p)
        {
            var b = new Button { Text = t, Size = new Size(90, 27), Location = p,
                BackColor = Color.FromArgb(100, 100, 120), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat };
            b.FlatAppearance.BorderSize = 0; par.Controls.Add(b); return b;
        }
    }

    // ════════════════════════════════════════════════════════════════════════
    //  DIALOGO INVENTARIO — con validaciones completas
    // ════════════════════════════════════════════════════════════════════════
    public class DlgInventario : FormBase
    {
        public Inventario Resultado { get; private set; }

        private TextBox       txtCodigo, txtDesc, txtDepto, txtUnidad, txtEnser, txtProv;
        private NumericUpDown numPeso;
        private NumericUpDown numCostoAct, numCostoAnt, numCostoPro, numPrecio;
        private NumericUpDown numExist, numMin, numMax;
        private NumericUpDown numBonoProd, numBonoVenta;

        private readonly Inventario       _original;
        private readonly List<Inventario> _todos;

        // Letras, numeros, espacios y simbolos comunes de descripcion/medidas.
        // Incluye comillas dobles (pulgadas: 2") y simples (pies: 3'), # ° + x % & : ; * _
        // El guardado en BD es parametrizado, asi que estos simbolos son seguros.
        private static readonly Regex _rxDesc =
            new Regex(@"^[a-zA-ZáéíóúÁÉÍÓÚüÜñÑ0-9\s\-\.\,\/\(\)""'#°ºª+×%&:;*_]+$");

        public DlgInventario(Inventario i, List<Inventario> todos)
        {
            _original = i;
            _todos    = todos ?? new List<Inventario>();

            Text = i == null ? "Nuevo Producto" : "Editar Producto";
            Size = new Size(500, 690);
            AutoScroll = true;
            StartPosition   = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = true; MinimizeBox = false;
            BackColor   = Color.FromArgb(240, 244, 250);

            int y = 15, lx = 15, fx = 165, w = 145;

            L("Codigo *:",        lx, y); txtCodigo   = T(fx, y, w);      y += 35;
            L("Descripcion *:",   lx, y); txtDesc      = T(fx, y, w * 2); y += 35;
            L("Departamento:",    lx, y); txtDepto     = T(fx, y, w);     y += 35;
            L("Unidad *:",        lx, y); txtUnidad    = T(fx, y, w);     y += 35;
            L("Enser:",           lx, y); txtEnser     = T(fx, y, w);     y += 35;
            L("Costo Actual:",    lx, y); numCostoAct  = N(fx, y, w);     y += 35;
            L("Costo Anterior:",  lx, y); numCostoAnt  = N(fx, y, w);     y += 35;
            L("Costo Promedio:",  lx, y); numCostoPro  = N(fx, y, w);     y += 35;
            L("Precio Venta *:",  lx, y); numPrecio    = N(fx, y, w);     y += 35;
            L("Existencia:",      lx, y); numExist     = N(fx, y, w);
            L("Min:",            340, y); numMin       = N(385, y, 80);   y += 35;
            L("Exist. Max:",      lx, y); numMax       = N(fx, y, w);     y += 35;
            L("Peso (kg/und):",   lx, y); numPeso      = N(fx, y, w); numPeso.DecimalPlaces = 4; numPeso.Maximum = 9999; y += 35;
            L("Cod.Proveedor:",   lx, y); txtProv      = T(fx, y, w);     y += 38;

            Controls.Add(new Label {
                Text = "Bono por producto (Bs) — pago por unidad producida / vendida (solo Admin)",
                Location = new Point(lx, y), AutoSize = true,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(26, 74, 138) });
            y += 24;
            L("Bono produccion Bs:", lx, y); numBonoProd  = N(fx, y, w); numBonoProd.Maximum  = 1000000000; y += 35;
            L("Bono venta Bs:",      lx, y); numBonoVenta = N(fx, y, w); numBonoVenta.Maximum = 1000000000; y += 40;
            // El VALOR de los bonos solo lo puede cambiar un administrador.
            if (!Sesion.EsAdmin)
            {
                numBonoProd.Enabled = numBonoVenta.Enabled = false;
                Controls.Add(new Label {
                    Text = "(Solo un administrador puede cambiar el valor de los bonos)",
                    Location = new Point(lx, y - 6), AutoSize = true,
                    Font = new Font("Segoe UI", 8f, FontStyle.Italic),
                    ForeColor = Color.FromArgb(150, 60, 0) });
                y += 20;
            }

            // Nota campos obligatorios
            Controls.Add(new Label {
                Text = "* Campos obligatorios",
                Location = new Point(lx, y), AutoSize = true,
                Font = new Font("Segoe UI", 8f, FontStyle.Italic),
                ForeColor = Color.FromArgb(120, 60, 0) });
            y += 25;

            if (i != null)
            {
                txtCodigo.Text    = i.Codigo;      txtCodigo.ReadOnly = true;
                txtDesc.Text      = i.Descripcion; txtDepto.Text      = i.Departamento;
                txtUnidad.Text    = i.Unidad;      txtEnser.Text      = i.Enser;
                numCostoAct.Value = i.CostoActual; numCostoAnt.Value  = i.CostoAnt;
                numCostoPro.Value = i.CostoPro;    numPrecio.Value    = i.Precio;
                numExist.Value    = i.Existencia;  numMin.Value       = i.ExistenMin;
                numMax.Value      = i.ExistenMax;  txtProv.Text       = i.CodProveedor;
                numBonoProd.Value = i.BonoProduccionBs; numBonoVenta.Value = i.BonoVentaBs;
            }

            var bOk  = BBtn("Guardar",  Color.FromArgb(30, 130, 70),  new Point(100, y));
            var bCan = BBtn("Cancelar", Color.FromArgb(160, 50, 50),  new Point(270, y));
            bCan.DialogResult = DialogResult.Cancel;
            bOk.Click += BtnOk_Click;
            
            
            var _dlgTip = AyudaService.CrearTooltip();
            if (txtCodigo != null) AyudaService.Tip(_dlgTip, txtCodigo, "Código único del producto (ej: SL-001)");
            if (txtDesc != null) AyudaService.Tip(_dlgTip, txtDesc, "Nombre completo del producto");
            if (txtUnidad != null) AyudaService.Tip(_dlgTip, txtUnidad, "Unidad de medida (und, kg, m, etc.)");
            if (numCostoAct != null) AyudaService.Tip(_dlgTip, numCostoAct, "Costo actual del producto en USD");
            if (numPrecio != null) AyudaService.Tip(_dlgTip, numPrecio, "Precio de venta al público en USD");
            if (numMin != null) AyudaService.Tip(_dlgTip, numMin, "Cantidad mínima en stock — genera alerta si baja de este valor");
            // Recordar posición y tamaño
            VentanaPrefs.Restaurar(this);
            Controls.AddRange(new Control[] { bOk, bCan });

            // Tab con seleccion
            foreach (var tb in new TextBox[] {
                txtCodigo, txtDesc, txtDepto, txtUnidad, txtEnser, txtProv })
                tb.Enter += (s, e) => ((TextBox)s).SelectAll();

            foreach (var n in new NumericUpDown[] {
                numCostoAct, numCostoAnt, numCostoPro, numPrecio,
                numExist, numMin, numMax })
                n.Enter += (s, e) => ((NumericUpDown)s).Select(
                    0, ((NumericUpDown)s).Text.Length);
        
            VentanaPrefs.Restaurar(this);
        }

        private void BtnOk_Click(object sender, EventArgs e)
        {
            // ── Validaciones ──────────────────────────────────────────────

            // Codigo obligatorio
            string cod = txtCodigo.Text.Trim();
            if (string.IsNullOrWhiteSpace(cod))
            { Warn("El código es obligatorio."); return; }

            // Codigo no duplicado (solo al crear)
            if (_original == null && _todos.Any(i => i.Codigo == cod))
            { Warn($"El código '{cod}' ya existe en el inventario."); return; }

            // Descripcion obligatoria
            string desc = txtDesc.Text.Trim();
            if (string.IsNullOrWhiteSpace(desc))
            { Warn("La descripción es obligatoria."); return; }

            // Descripcion: caracteres validos
            if (!_rxDesc.IsMatch(desc))
            { Warn("La descripción contiene caracteres no permitidos.\nEvite símbolos raros o de control; se permiten letras, números, espacios, comillas \" ' y - . , / ( ) # ° + × % & : ;"); return; }

            // Unidad obligatoria
            if (string.IsNullOrWhiteSpace(txtUnidad.Text.Trim()))
            { Warn("La unidad de medida es obligatoria (ej: Und, Kg, L)."); return; }

            // Precio mayor a cero
            if (numPrecio.Value <= 0)
            { Warn("El precio de venta debe ser mayor a 0."); return; }

            // Costo actual no negativo (puede ser 0 si aún no se tiene)
            if (numCostoAct.Value < 0)
            { Warn("El costo actual no puede ser negativo."); return; }

            // Existencia Min no puede ser mayor que Max (si Max > 0)
            if (numMax.Value > 0 && numMin.Value > numMax.Value)
            { Warn("La existencia mínima no puede ser mayor que la máxima."); return; }

            // Existencia actual no puede ser negativa
            if (numExist.Value < 0)
            { Warn("La existencia no puede ser negativa."); return; }

            // ── Armar resultado ───────────────────────────────────────────
            Resultado = _original ?? new Inventario();
            Resultado.Codigo        = cod;
            Resultado.Descripcion   = desc;
            Resultado.Departamento  = txtDepto.Text.Trim();
            Resultado.Unidad        = txtUnidad.Text.Trim();
            Resultado.Enser         = txtEnser.Text.Trim();
            Resultado.CostoActual   = numCostoAct.Value;
            Resultado.CostoAnt      = numCostoAnt.Value;
            Resultado.CostoPro      = numCostoPro.Value;
            Resultado.Precio        = numPrecio.Value;
            Resultado.Existencia    = numExist.Value;
            Resultado.ExistenMin    = numMin.Value;
            Resultado.ExistenMax    = numMax.Value;
            Resultado.PesoKg        = numPeso.Value;
            Resultado.CodProveedor  = txtProv.Text.Trim();
            Resultado.BonoProduccionBs = numBonoProd.Value;
            Resultado.BonoVentaBs      = numBonoVenta.Value;
            Resultado.UltimaActualz = DateTime.Today;
            DialogResult = DialogResult.OK;
        }

        private void Warn(string msg) =>
            MessageBox.Show(msg, "Validacion",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);

        private void L(string t, int x, int y) =>
            Controls.Add(new Label { Text = t, Location = new Point(x, y + 3),
                AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Bold) });

        private TextBox T(int x, int y, int w)
        {
            var tb = new TextBox { Location = new Point(x, y), Size = new Size(w, 24) };
            Controls.Add(tb); return tb;
        }
        private NumericUpDown N(int x, int y, int w)
        {
            var n = new NumericUpDown { Location = new Point(x, y), Size = new Size(w, 24),
                Maximum = 9999999, DecimalPlaces = 2 };
            Controls.Add(n); return n;
        }
        private Button BBtn(string t, Color c, Point p)
        {
            var b = new Button { Text = t, Location = p, Size = new Size(120, 32),
                BackColor = c, ForeColor = Color.White, FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            b.FlatAppearance.BorderSize = 0; Controls.Add(b); return b;
        }
    }
}
