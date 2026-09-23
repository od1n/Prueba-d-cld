using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace SistemaGestion
{
    // ══════════════════════════════════════════════════════════════════════════
    //  LISTA DE ORDENES DE PRODUCCION
    // ══════════════════════════════════════════════════════════════════════════
    public class FrmOrdenesProduccion : FormBase
    {
        private DataGridView dgv;
        private Label        lblResumen;
        private ComboBox     cmbFiltroEstado;

        public FrmOrdenesProduccion()
        {
            Text          = "Ordenes de Produccion";
            Size          = new Size(1100, 580);
            StartPosition = FormStartPosition.CenterParent;
            BackColor     = Color.FromArgb(245, 248, 245);

            // ── Header ────────────────────────────────────────────────────
            var hdr = new Panel { Dock = DockStyle.Top, Height = 50,
                BackColor = Color.FromArgb(60, 120, 40) };
            hdr.Controls.Add(new Label
            {
                Text = "Ordenes de Produccion", ForeColor = Color.White,
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                AutoSize = true, Location = new Point(12, 13)
            });

            var btnNueva   = Btn("+ Nueva OP",   Color.FromArgb(40, 160, 60),  new Point(600, 8));
            var btnAbrir   = Btn("Abrir",         Color.FromArgb(40, 100, 180), new Point(706, 8));
            var btnCerrar2 = Btn("Completar OP",  Color.FromArgb(160, 100, 20), new Point(804, 8), 100);
            var btnCancelar= Btn("Cancelar OP",   Color.FromArgb(180, 50, 50),  new Point(912, 8), 100);
            var btnElim    = Btn("Eliminar",       Color.FromArgb(120, 40, 40),  new Point(1020, 8));
            hdr.Controls.AddRange(new Control[]
                { btnNueva, btnAbrir, btnCerrar2, btnCancelar, btnElim });

            // ── Barra de filtros ──────────────────────────────────────────
            var pnlFiltro = new Panel
            {
                Dock = DockStyle.Top, Height = 36,
                BackColor = Color.FromArgb(228, 240, 225)
            };
            pnlFiltro.Controls.Add(new Label
                { Text = "Estado:", Location = new Point(12, 10), AutoSize = true });
            cmbFiltroEstado = new ComboBox
            {
                Location = new Point(65, 7), Size = new Size(130, 24),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbFiltroEstado.Items.AddRange(new object[]
                { "Todos", "Borrador", "En proceso", "Completada", "Cancelada" });
            cmbFiltroEstado.SelectedIndex = 0;
            cmbFiltroEstado.SelectedIndexChanged += (s, e) => CargarDatos();
            pnlFiltro.Controls.Add(cmbFiltroEstado);

            // ── Grid ──────────────────────────────────────────────────────
            dgv = new DataGridView
            {
                Dock = DockStyle.Fill, ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false, AllowUserToAddRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White, BorderStyle = BorderStyle.None,
                RowHeadersVisible = false, Font = new Font("Segoe UI", 9f),
                RowTemplate = { Height = 24 }
            };
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(60, 120, 40);
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgv.ColumnHeadersDefaultCellStyle.Font      = new Font("Segoe UI", 9f, FontStyle.Bold);
            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(238, 248, 235);
            dgv.EnableHeadersVisualStyles = false;
            dgv.DataBindingComplete += (s, e) => ColorearFilas();
            dgv.DoubleClick         += (s, e) => AbrirOrden();

            // ── Footer ────────────────────────────────────────────────────
            var footer = new Panel
            {
                Dock = DockStyle.Bottom, Height = 40,
                BackColor = Color.FromArgb(215, 235, 215)
            };
            lblResumen = new Label
            {
                AutoSize = true, Location = new Point(12, 10),
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(30, 80, 20)
            };
            var btnCerrar = new Button
            {
                Text = "Cerrar", Size = new Size(90, 27), Location = new Point(990, 6),
                BackColor = Color.FromArgb(100, 100, 120), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnCerrar.FlatAppearance.BorderSize = 0;
            btnCerrar.Click += (s, e) => Close();
            footer.Controls.AddRange(new Control[] { lblResumen, btnCerrar });

            Controls.AddRange(new Control[] { dgv, pnlFiltro, hdr, footer });

            // ── Eventos ───────────────────────────────────────────────────
            btnNueva.Click    += (s, e) => NuevaOrden();
            btnAbrir.Click    += (s, e) => AbrirOrden();
            btnCerrar2.Click  += (s, e) => CompletarOrden();
            btnCancelar.Click += (s, e) => CancelarOrden();
            btnElim.Click     += (s, e) => EliminarOrden();

            CargarDatos();
        }

        // ── Carga ─────────────────────────────────────────────────────────
        private void CargarDatos()
        {
            string estado = cmbFiltroEstado.SelectedItem?.ToString();
            if (estado == "Todos") estado = null;

            var lista = DatabaseService.ObtenerOrdenes(estado);
            dgv.DataSource = null;
            dgv.DataSource = lista.Select(op => new
            {
                op.Id, op.NumOrden,
                Producto    = op.ProductoCod + " — " + op.ProductoDesc,
                Planificado = op.CantidadPlan.ToString("N0"),
                Producido   = op.CantidadReal > 0 ? op.CantidadReal.ToString("N0") : "—",
                Inicio      = op.FechaInicio.ToShortDateString(),
                Fin         = op.FechaFin.ToShortDateString(),
                op.Estado,
                op.Responsable,
                Creada      = op.FechaCreacion.ToString("dd/MM/yy HH:mm")
            }).ToList();

            var all = DatabaseService.ObtenerOrdenes();
            lblResumen.Text =
                "  Total: " + all.Count +
                "   |   Borrador: " + all.Count(o => o.Estado == "Borrador") +
                "   |   En proceso: " + all.Count(o => o.Estado == "En proceso") +
                "   |   Completadas: " + all.Count(o => o.Estado == "Completada") +
                "   |   Canceladas: " + all.Count(o => o.Estado == "Cancelada");
        }

        private void ColorearFilas()
        {
            foreach (DataGridViewRow row in dgv.Rows)
            {
                var est = row.Cells["Estado"].Value?.ToString();
                row.DefaultCellStyle.BackColor = est switch
                {
                    "Completada" => Color.FromArgb(225, 255, 225),
                    "En proceso" => Color.FromArgb(255, 250, 210),
                    "Cancelada"  => Color.FromArgb(255, 230, 230),
                    _            => Color.White
                };
            }
        }

        private OrdenProduccion GetSeleccionada()
        {
            if (dgv.CurrentRow == null) return null;
            int id = (int)dgv.CurrentRow.Cells["Id"].Value;
            return DatabaseService.ObtenerOrdenes().Find(o => o.Id == id);
        }

        // ── Acciones ──────────────────────────────────────────────────────
        private void NuevaOrden()
        {
            using var dlg = new DlgOrdenProduccion(null);
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                DatabaseService.InsertarOrden(dlg.Resultado);
                DatabaseService.GuardarLineasOrden(dlg.Resultado.Id, dlg.Lineas);
                DatabaseService.RegistrarAuditoria("ORDEN_NUEVA", "OrdenesProduccion",
                    dlg.Resultado.NumOrden);
                CargarDatos();
            }
        }

        private void AbrirOrden()
        {
            var op = GetSeleccionada();
            if (op == null) return;
            var lineas = DatabaseService.ObtenerLineasOrden(op.Id);
            using var dlg = new DlgOrdenProduccion(op, lineas);
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                DatabaseService.ActualizarOrden(dlg.Resultado);
                DatabaseService.GuardarLineasOrden(dlg.Resultado.Id, dlg.Lineas);
                CargarDatos();
            }
        }

        private void CompletarOrden()
        {
            var op = GetSeleccionada();
            if (op == null) return;

            if (op.Estado == "Completada" || op.Estado == "Cancelada")
            {
                MessageBox.Show("Esta orden ya esta " + op.Estado.ToLower() + ".",
                    "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Verificar disponibilidad de materias primas
            var lineas = DatabaseService.ObtenerLineasOrden(op.Id);
            var faltantes = lineas.Where(l => !l.Disponible).ToList();

            if (faltantes.Count > 0)
            {
                string msg = "ATENCION — Stock insuficiente en:\n\n";
                foreach (var f in faltantes)
                    msg += $"  {f.MateriaCod}: requiere {f.CantidadTot:N2}, " +
                           $"disponible {f.StockActual:N2}\n";
                msg += "\n¿Deseas completar de todas formas con el stock disponible?";

                if (MessageBox.Show(msg, "Stock insuficiente",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.No)
                    return;
            }

            // Pedir cantidad real producida
            using var dlgCant = new DlgCantidadReal(op.CantidadPlan);
            if (dlgCant.ShowDialog() != DialogResult.OK) return;

            try
            {
                DatabaseService.CerrarOrden(op, dlgCant.CantidadReal);
                CargarDatos();
                MessageBox.Show(
                    "Orden " + op.NumOrden + " completada.\n" +
                    "Producido: " + dlgCant.CantidadReal + " unidades.\n\n" +
                    "El inventario fue actualizado automaticamente.",
                    "Orden completada", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al completar la orden:\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CancelarOrden()
        {
            var op = GetSeleccionada();
            if (op == null) return;
            if (op.Estado == "Completada")
            {
                MessageBox.Show("No se puede cancelar una orden completada.",
                    "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (MessageBox.Show("Cancelar orden " + op.NumOrden + "?", "Confirmar",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                op.Estado = "Cancelada";
                DatabaseService.ActualizarOrden(op);
                DatabaseService.RegistrarAuditoria("ORDEN_CANCELADA", "OrdenesProduccion",
                    op.NumOrden);
                CargarDatos();
            }
        }

        private void EliminarOrden()
        {
            var op = GetSeleccionada();
            if (op == null) return;
            if (op.Estado == "Completada" || op.Estado == "En proceso")
            {
                MessageBox.Show("Solo se pueden eliminar ordenes en Borrador o Canceladas.",
                    "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (MessageBox.Show("Eliminar orden " + op.NumOrden + " permanentemente?",
                "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                DatabaseService.EliminarOrden(op.Id);
                CargarDatos();
            }
        }

        private Button Btn(string t, Color c, Point p, int w = 95)
        {
            var b = new Button { Text = t, Size = new Size(w, 33), Location = p,
                BackColor = c, ForeColor = Color.White, FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8f, FontStyle.Bold), Cursor = Cursors.Hand };
            b.FlatAppearance.BorderSize = 0;
            return b;
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  DIALOGO NUEVA / EDITAR ORDEN
    // ══════════════════════════════════════════════════════════════════════════
    public class DlgOrdenProduccion : FormBase
    {
        public OrdenProduccion      Resultado { get; private set; }
        public List<LineaOrden>     Lineas    { get; private set; } = new();

        // Cabecera
        private TextBox       txtNumOrden, txtResponsable, txtObs;
        private Label         lblProducto;
        private DateTimePicker dtpInicio, dtpFin;
        private NumericUpDown  numCantPlan;
        private ComboBox       cmbEstado;
        private Button         btnSelProducto;

        // Grid materias primas
        private DataGridView   dgvLineas;
        private Label          lblAlerta;

        private Inventario _productoSel;

        public DlgOrdenProduccion(OrdenProduccion op, List<LineaOrden> lineas = null)
        {
            bool esNueva = op == null;
            Text            = esNueva ? "Nueva Orden de Produccion"
                                       : "Orden " + op.NumOrden;
            Size            = new Size(920, 640);
            StartPosition   = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox     = MinimizeBox = false;
            BackColor       = Color.FromArgb(245, 248, 245);

            // ── Banner ────────────────────────────────────────────────────
            var banner = new Panel
            {
                Dock = DockStyle.Top, Height = 40,
                BackColor = Color.FromArgb(60, 120, 40)
            };
            banner.Controls.Add(new Label
            {
                Text = Text, ForeColor = Color.White,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                AutoSize = true, Location = new Point(12, 10)
            });

            // ── Panel izquierdo: cabecera ─────────────────────────────────
            var panelL = new Panel
            {
                Location = new Point(0, 42), Size = new Size(370, 480),
                BackColor = Color.FromArgb(245, 248, 245)
            };

            int y = 12;
            void L(string t, int yy) => panelL.Controls.Add(new Label
            {
                Text = t, Location = new Point(12, yy + 3), AutoSize = true,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            });
            TextBox T(int yy, string v = "") { var tb = new TextBox
                { Location = new Point(145, yy), Size = new Size(210, 24), Text = v };
                panelL.Controls.Add(tb); return tb; }

            L("No. Orden:", y);
            txtNumOrden = T(y, esNueva ? DatabaseService.GenerarNumOrden() : op.NumOrden);
            txtNumOrden.ReadOnly = true;
            txtNumOrden.BackColor = Color.FromArgb(235, 240, 235); y += 34;

            L("Producto a fabricar:", y);
            lblProducto = new Label
            {
                Location = new Point(145, y), Size = new Size(150, 24),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 100, 50),
                Text = esNueva ? "— seleccionar —" : op.ProductoCod + " " + op.ProductoDesc,
                BorderStyle = BorderStyle.FixedSingle,
                TextAlign = ContentAlignment.MiddleLeft
            };
            panelL.Controls.Add(lblProducto);
            btnSelProducto = new Button
            {
                Text = "...", Location = new Point(300, y), Size = new Size(35, 24),
                BackColor = Color.FromArgb(60, 120, 40), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };
            btnSelProducto.FlatAppearance.BorderSize = 0;
            btnSelProducto.Click += (s, e) => SeleccionarProducto();
            panelL.Controls.Add(btnSelProducto); y += 34;

            L("Cantidad planificada:", y);
            numCantPlan = new NumericUpDown
            {
                Location = new Point(145, y), Size = new Size(150, 24),
                Minimum = 1, Maximum = 999999, Value = esNueva ? 1 : op.CantidadPlan,
                DecimalPlaces = 2
            };
            numCantPlan.ValueChanged += (s, e) => RecalcularLineas();
            panelL.Controls.Add(numCantPlan); y += 34;

            L("Fecha inicio:", y);
            dtpInicio = new DateTimePicker
            {
                Location = new Point(145, y), Size = new Size(210, 24),
                Format = DateTimePickerFormat.Short,
                Value  = esNueva ? DateTime.Today : op.FechaInicio
            };
            panelL.Controls.Add(dtpInicio); y += 34;

            L("Fecha fin:", y);
            dtpFin = new DateTimePicker
            {
                Location = new Point(145, y), Size = new Size(210, 24),
                Format = DateTimePickerFormat.Short,
                Value  = esNueva ? DateTime.Today.AddDays(7) : op.FechaFin
            };
            panelL.Controls.Add(dtpFin); y += 34;

            L("Responsable:", y);  txtResponsable = T(y, esNueva ? "" : op.Responsable); y += 34;

            L("Estado:", y);
            cmbEstado = new ComboBox
            {
                Location = new Point(145, y), Size = new Size(210, 24),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbEstado.Items.AddRange(new object[]
                { "Borrador", "En proceso", "Completada", "Cancelada" });
            cmbEstado.SelectedItem = esNueva ? "Borrador" : op.Estado;
            panelL.Controls.Add(cmbEstado); y += 34;

            L("Observaciones:", y);
            txtObs = new TextBox
            {
                Location = new Point(145, y), Size = new Size(210, 55),
                Multiline = true, Text = esNueva ? "" : op.Observaciones
            };
            panelL.Controls.Add(txtObs);

            // ── Panel derecho: materias primas ────────────────────────────
            var panelR = new Panel
            {
                Location = new Point(374, 42), Size = new Size(530, 490),
                BackColor = Color.FromArgb(240, 248, 240)
            };

            panelR.Controls.Add(new Label
            {
                Text = "Materias Primas Requeridas",
                Location = new Point(8, 8), AutoSize = true,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(40, 100, 30)
            });

            var btnAddMat = new Button
            {
                Text = "+ Agregar materia", Location = new Point(8, 30), Size = new Size(140, 26),
                BackColor = Color.FromArgb(40, 140, 60), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 8f, FontStyle.Bold)
            };
            btnAddMat.FlatAppearance.BorderSize = 0;
            btnAddMat.Click += (s, e) => AgregarMateria();

            var btnQuitarMat = new Button
            {
                Text = "Quitar", Location = new Point(158, 30), Size = new Size(70, 26),
                BackColor = Color.FromArgb(180, 50, 50), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 8f, FontStyle.Bold)
            };
            btnQuitarMat.FlatAppearance.BorderSize = 0;
            btnQuitarMat.Click += (s, e) =>
            {
                if (dgvLineas.CurrentRow != null)
                {
                    dgvLineas.Rows.RemoveAt(dgvLineas.CurrentRow.Index);
                    ActualizarAlerta();
                }
            };

            dgvLineas = new DataGridView
            {
                Location = new Point(8, 64), Size = new Size(514, 340),
                ReadOnly = false, AllowUserToAddRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White, BorderStyle = BorderStyle.FixedSingle,
                RowHeadersVisible = false, Font = new Font("Segoe UI", 8.5f),
                RowTemplate = { Height = 24 }
            };
            dgvLineas.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(60, 120, 40);
            dgvLineas.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvLineas.ColumnHeadersDefaultCellStyle.Font      = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            dgvLineas.EnableHeadersVisualStyles = false;

            dgvLineas.Columns.Add(new DataGridViewTextBoxColumn
                { Name = "MateriaCod",  HeaderText = "Codigo",     ReadOnly = true, Width = 70 });
            dgvLineas.Columns.Add(new DataGridViewTextBoxColumn
                { Name = "MateriaDesc", HeaderText = "Descripcion",ReadOnly = true });
            dgvLineas.Columns.Add(new DataGridViewTextBoxColumn
                { Name = "CantPorU",    HeaderText = "Cant/Unidad",Width = 80 });
            dgvLineas.Columns.Add(new DataGridViewTextBoxColumn
                { Name = "CantTotal",   HeaderText = "Total req.", ReadOnly = true, Width = 80 });
            dgvLineas.Columns.Add(new DataGridViewTextBoxColumn
                { Name = "Stock",       HeaderText = "En stock",   ReadOnly = true, Width = 75 });
            dgvLineas.Columns.Add(new DataGridViewTextBoxColumn
                { Name = "Ok",          HeaderText = "OK?",        ReadOnly = true, Width = 40 });

            dgvLineas.CellEndEdit += (s, e) =>
            {
                if (dgvLineas.Columns[e.ColumnIndex].Name == "CantPorU")
                    RecalcularFila(e.RowIndex);
            };

            lblAlerta = new Label
            {
                Location = new Point(8, 412), Size = new Size(514, 50),
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(180, 50, 50),
                Text = ""
            };

            panelR.Controls.AddRange(new Control[]
                { btnAddMat, btnQuitarMat, dgvLineas, lblAlerta });

            // ── Botones inferiores ────────────────────────────────────────
            var panelBtns = new Panel
            {
                Dock = DockStyle.Bottom, Height = 50,
                BackColor = Color.FromArgb(220, 235, 220)
            };
            var bOk = new Button
            {
                Text = "Guardar Orden", Location = new Point(320, 10), Size = new Size(150, 32),
                BackColor = Color.FromArgb(40, 130, 50), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10f, FontStyle.Bold)
            };
            bOk.FlatAppearance.BorderSize = 0;
            bOk.Click += (s, e) => Guardar(op);

            var bCan = new Button
            {
                Text = "Cancelar", DialogResult = DialogResult.Cancel,
                Location = new Point(490, 10), Size = new Size(110, 32),
                BackColor = Color.FromArgb(120, 120, 130), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            bCan.FlatAppearance.BorderSize = 0;
            panelBtns.Controls.AddRange(new Control[] { bOk, bCan });

            Controls.AddRange(new Control[] { banner, panelL, panelR, panelBtns });

            // ── Cargar lineas existentes ───────────────────────────────────
            if (lineas != null)
                foreach (var l in lineas)
                    AgregarFilaLinea(l.MateriaCod, l.MateriaDesc, l.CantidadReq,
                        l.CantidadTot, l.StockActual);

            if (!esNueva)
            {
                _productoSel = DatabaseService.ObtenerInventario()
                    .Find(i => i.Codigo == op.ProductoCod);
            }

            RecalcularLineas();
        }

        // ── Seleccion de producto fabricado ───────────────────────────────
        private void SeleccionarProducto()
        {
            using var dlg = new DlgSelProducto();
            if (dlg.ShowDialog() != DialogResult.OK) return;
            _productoSel = dlg.ProductoSeleccionado;
            lblProducto.Text = _productoSel.Codigo + " — " +
                (_productoSel.Descripcion ?? "").Substring(0,
                Math.Min((_productoSel.Descripcion ?? "").Length, 18));
        }

        // ── Agregar materia prima ─────────────────────────────────────────
        private void AgregarMateria()
        {
            using var dlg = new DlgSelProducto();
            if (dlg.ShowDialog() != DialogResult.OK) return;
            var inv = dlg.ProductoSeleccionado;

            // Evitar duplicados
            foreach (DataGridViewRow row in dgvLineas.Rows)
                if (row.Cells["MateriaCod"].Value?.ToString() == inv.Codigo) return;

            AgregarFilaLinea(inv.Codigo, inv.Descripcion, 1,
                (decimal)numCantPlan.Value * 1, inv.Existencia);
            ActualizarAlerta();
        }

        private void AgregarFilaLinea(string cod, string desc,
            decimal cantPorU, decimal cantTot, decimal stock)
        {
            int idx = dgvLineas.Rows.Add();
            var row = dgvLineas.Rows[idx];
            row.Cells["MateriaCod"].Value  = cod;
            row.Cells["MateriaDesc"].Value = desc;
            row.Cells["CantPorU"].Value    = cantPorU.ToString("F4");
            row.Cells["CantTotal"].Value   = cantTot.ToString("N2");
            row.Cells["Stock"].Value       = stock.ToString("N2");
            bool ok = stock >= cantTot;
            row.Cells["Ok"].Value          = ok ? "Si" : "NO";
            row.Cells["Ok"].Style.ForeColor= ok ? Color.DarkGreen : Color.DarkRed;
            if (!ok) row.DefaultCellStyle.BackColor = Color.FromArgb(255, 235, 235);
        }

        private void RecalcularFila(int rowIndex)
        {
            if (rowIndex < 0 || rowIndex >= dgvLineas.Rows.Count) return;
            var row = dgvLineas.Rows[rowIndex];
            if (!decimal.TryParse(row.Cells["CantPorU"].Value?.ToString(), out decimal cpu))
                return;
            decimal plan     = numCantPlan.Value;
            decimal cantTot  = cpu * plan;
            decimal.TryParse(row.Cells["Stock"].Value?.ToString(), out decimal stock);
            bool ok          = stock >= cantTot;

            row.Cells["CantTotal"].Value    = cantTot.ToString("N2");
            row.Cells["Ok"].Value           = ok ? "Si" : "NO";
            row.Cells["Ok"].Style.ForeColor = ok ? Color.DarkGreen : Color.DarkRed;
            row.DefaultCellStyle.BackColor  = ok ? Color.White : Color.FromArgb(255, 235, 235);
            ActualizarAlerta();
        }

        private void RecalcularLineas()
        {
            for (int i = 0; i < dgvLineas.Rows.Count; i++)
                RecalcularFila(i);
        }

        private void ActualizarAlerta()
        {
            int faltantes = 0;
            foreach (DataGridViewRow row in dgvLineas.Rows)
                if (row.Cells["Ok"].Value?.ToString() == "NO") faltantes++;

            lblAlerta.Text = faltantes > 0
                ? faltantes + " materia(s) con stock insuficiente — se marcaron en rojo"
                : "";
        }

        // ── Guardar ───────────────────────────────────────────────────────
        private void Guardar(OrdenProduccion original)
        {
            if (_productoSel == null && original == null)
            {
                MessageBox.Show("Selecciona el producto a fabricar.",
                    "Validacion", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Resultado = original ?? new OrdenProduccion();
            Resultado.NumOrden      = txtNumOrden.Text.Trim();
            Resultado.ProductoCod   = _productoSel?.Codigo ?? original.ProductoCod;
            Resultado.ProductoDesc  = _productoSel?.Descripcion ?? original.ProductoDesc;
            Resultado.CantidadPlan  = numCantPlan.Value;
            Resultado.CantidadReal  = original?.CantidadReal ?? 0;
            Resultado.FechaInicio   = dtpInicio.Value;
            Resultado.FechaFin      = dtpFin.Value;
            Resultado.Estado        = cmbEstado.SelectedItem?.ToString() ?? "Borrador";
            Resultado.Responsable   = txtResponsable.Text.Trim();
            Resultado.Observaciones = txtObs.Text.Trim();
            Resultado.FechaCreacion = original?.FechaCreacion ?? DateTime.Now;

            Lineas.Clear();
            foreach (DataGridViewRow row in dgvLineas.Rows)
            {
                if (!decimal.TryParse(row.Cells["CantPorU"].Value?.ToString(), out decimal cpu))
                    continue;
                decimal.TryParse(row.Cells["CantTotal"].Value?.ToString(), out decimal ct);
                Lineas.Add(new LineaOrden
                {
                    IdOrden     = Resultado.Id,
                    MateriaCod  = row.Cells["MateriaCod"].Value?.ToString() ?? "",
                    MateriaDesc = row.Cells["MateriaDesc"].Value?.ToString() ?? "",
                    CantidadReq = cpu,
                    CantidadTot = ct
                });
            }

            DialogResult = DialogResult.OK;
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  DIALOGO CANTIDAD REAL PRODUCIDA (al cerrar orden)
    // ══════════════════════════════════════════════════════════════════════════
    public class DlgCantidadReal : FormBase
    {
        public decimal CantidadReal { get; private set; }

        public DlgCantidadReal(decimal cantidadPlan)
        {
            Text            = "Completar Orden";
            Size            = new Size(360, 210);
            StartPosition   = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox     = MinimizeBox = false;
            BackColor       = Color.FromArgb(245, 248, 245);

            var banner = new Panel
            {
                Dock = DockStyle.Top, Height = 38,
                BackColor = Color.FromArgb(60, 120, 40)
            };
            banner.Controls.Add(new Label
            {
                Text = "Registrar produccion real", ForeColor = Color.White,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                AutoSize = true, Location = new Point(12, 9)
            });

            Controls.Add(new Label
            {
                Text = "Cantidad planificada: " + cantidadPlan.ToString("N0"),
                Location = new Point(20, 55), AutoSize = true,
                Font = new Font("Segoe UI", 9f), ForeColor = Color.Gray
            });

            Controls.Add(new Label
            {
                Text = "Cantidad producida:",
                Location = new Point(20, 90), AutoSize = true,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            });

            var num = new NumericUpDown
            {
                Location = new Point(180, 87), Size = new Size(140, 28),
                Minimum = 0, Maximum = 9999999,
                Value   = cantidadPlan, DecimalPlaces = 2,
                Font    = new Font("Segoe UI", 10f, FontStyle.Bold)
            };
            Controls.Add(num);

            var bOk = new Button
            {
                Text = "Completar orden", Location = new Point(50, 135), Size = new Size(150, 34),
                BackColor = Color.FromArgb(40, 130, 50), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };
            bOk.FlatAppearance.BorderSize = 0;
            bOk.Click += (s, e) => { CantidadReal = num.Value; DialogResult = DialogResult.OK; };

            var bCan = new Button
            {
                Text = "Cancelar", DialogResult = DialogResult.Cancel,
                Location = new Point(215, 135), Size = new Size(100, 34),
                BackColor = Color.FromArgb(120, 120, 130), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            bCan.FlatAppearance.BorderSize = 0;

            Controls.AddRange(new Control[] { banner, bOk, bCan });
        }
    }
}
