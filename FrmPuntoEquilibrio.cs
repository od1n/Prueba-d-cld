using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace SistemaGestion
{
    public class FrmPuntoEquilibrio : Form
    {
        // --- Controles principales ---
        private Panel pnlKpis;
        private Panel pnlDiagrama;
        private DataGridView dgvConceptos;
        private ComboBox cmbMeses;
        private Button btnCargar, btnAgregar, btnEditar, btnEliminar, btnExportarPdf;
        private Label lblTitulo;
        private Panel pnlToolbar;

        // Los KPIs se pintan directamente en el evento Paint del panel

        // --- Datos ---
        private List<DatoPE> _datos = new();
        private ResultadoPE _resultado = new();
        private int _meses = 3;

        public FrmPuntoEquilibrio()
        {
            InitializeComponent();
            CargarDesdeDB();
        }

        private void InitializeComponent()
        {
            this.Text = "Punto de Equilibrio";
            this.Size = new Size(1100, 720);
            this.StartPosition = FormStartPosition.CenterParent;
            this.MinimumSize = new Size(900, 600);
            this.BackColor = Color.FromArgb(245, 247, 250);
            this.Font = new Font("Segoe UI", 9f);

            // --- Toolbar superior ---
            pnlToolbar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 52,
                BackColor = Color.FromArgb(30, 58, 95),
                Padding = new Padding(10, 0, 10, 0)
            };

            lblTitulo = new Label
            {
                Text = "  Punto de Equilibrio Automatico",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                AutoSize = false,
                Width = 300,
                Height = 52,
                TextAlign = ContentAlignment.MiddleLeft,
                Location = new Point(0, 0)
            };

            var lblPeriodo = new Label
            {
                Text = "Periodo:",
                ForeColor = Color.FromArgb(180, 210, 240),
                AutoSize = true,
                Location = new Point(320, 17)
            };

            cmbMeses = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 130,
                Location = new Point(380, 14),
                BackColor = Color.White
            };
            cmbMeses.Items.AddRange(new object[] {
                "Ultimo mes", "Ultimos 3 meses", "Ultimos 6 meses", "Ultimo anio"
            });
            cmbMeses.SelectedIndex = 1;
            cmbMeses.SelectedIndexChanged += (s, e) => {
                _meses = cmbMeses.SelectedIndex == 0 ? 1
                       : cmbMeses.SelectedIndex == 1 ? 3
                       : cmbMeses.SelectedIndex == 2 ? 6 : 12;
                CargarDesdeDB();
            };

            btnCargar = CrearBotonToolbar("Recalcular", Color.FromArgb(39, 174, 96));
            btnCargar.Location = new Point(530, 12);
            btnCargar.Click += (s, e) => CargarDesdeDB();

            btnExportarPdf = CrearBotonToolbar("Exportar PDF", Color.FromArgb(192, 57, 43));
            btnExportarPdf.Location = new Point(640, 12);
            btnExportarPdf.Click += BtnExportarPdf_Click;

            pnlToolbar.Controls.AddRange(new Control[] {
                lblTitulo, lblPeriodo, cmbMeses, btnCargar, btnExportarPdf
            });

            // --- Panel KPIs ---
            pnlKpis = new Panel
            {
                Dock = DockStyle.Top,
                Height = 90,
                BackColor = Color.FromArgb(245, 247, 250),
                Padding = new Padding(10, 8, 10, 8)
            };
            pnlKpis.Paint += PnlKpis_Paint;

            // --- Splitter horizontal: diagrama arriba, grid abajo ---
            var split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                BackColor = Color.FromArgb(245, 247, 250),
                Panel1MinSize = 200,
                Panel2MinSize = 140
            };

            // Diagrama GDI+
            pnlDiagrama = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Margin = new Padding(10),
                Padding = new Padding(10)
            };
            pnlDiagrama.Paint += PnlDiagrama_Paint;

            // Panel inferior: toolbar grid + grid
            var pnlGrid = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(245, 247, 250) };

            var pnlGridToolbar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 38,
                BackColor = Color.FromArgb(245, 247, 250),
                Padding = new Padding(6, 4, 6, 4)
            };

            var lblGrid = new Label
            {
                Text = "Conceptos de Costo (editables):",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(6, 10),
                ForeColor = Color.FromArgb(44, 62, 80)
            };

            btnAgregar = CrearBotonGrid("+ Agregar", Color.FromArgb(39, 174, 96));
            btnAgregar.Location = new Point(240, 5);
            btnAgregar.Click += BtnAgregar_Click;

            btnEditar = CrearBotonGrid("Editar", Color.FromArgb(41, 128, 185));
            btnEditar.Location = new Point(330, 5);
            btnEditar.Click += BtnEditar_Click;

            btnEliminar = CrearBotonGrid("Eliminar", Color.FromArgb(192, 57, 43));
            btnEliminar.Location = new Point(405, 5);
            btnEliminar.Click += BtnEliminar_Click;

            pnlGridToolbar.Controls.AddRange(new Control[] { lblGrid, btnAgregar, btnEditar, btnEliminar });

            dgvConceptos = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                Font = new Font("Segoe UI", 9f)
            };
            EstilarDGV(dgvConceptos);

            dgvConceptos.Columns.Add(new DataGridViewTextBoxColumn { Name = "colConcepto", HeaderText = "Concepto", FillWeight = 40 });
            dgvConceptos.Columns.Add(new DataGridViewTextBoxColumn { Name = "colTipo", HeaderText = "Tipo", FillWeight = 20 });
            dgvConceptos.Columns.Add(new DataGridViewTextBoxColumn { Name = "colMonto", HeaderText = "Monto Mensual (USD)", FillWeight = 20, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight, Format = "N2" } });
            dgvConceptos.Columns.Add(new DataGridViewTextBoxColumn { Name = "colOrigen", HeaderText = "Origen", FillWeight = 20 });

            pnlGrid.Controls.Add(dgvConceptos);
            pnlGrid.Controls.Add(pnlGridToolbar);

            split.Panel1.Controls.Add(pnlDiagrama);
            split.Panel2.Controls.Add(pnlGrid);

            this.Controls.Add(split);
            this.Controls.Add(pnlKpis);
            this.Controls.Add(pnlToolbar);

            this.Load += (s, e) => {
                split.SplitterDistance = (int)(split.Height * 0.55);
            };
        }

        // -------------------------------------------------------------------------
        // LOGICA DE DATOS
        // -------------------------------------------------------------------------

        private void CargarDesdeDB()
        {
            try
            {
                // Obtener datos automaticos de la BD + manuales guardados
                var manuales = _datos.Where(d => d.Origen == "Manual").ToList();
                _datos = DatabaseService.ObtenerDatosPE(_meses, manuales);
                _resultado = DatabaseService.CalcularPuntoEquilibrio(_datos);
                RefrescarGrid();
                RefrescarKpis();
                pnlDiagrama.Invalidate();
                pnlKpis.Invalidate();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar datos: " + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RefrescarGrid()
        {
            dgvConceptos.Rows.Clear();
            foreach (var d in _datos.OrderBy(x => x.Tipo).ThenBy(x => x.Concepto))
            {
                int row = dgvConceptos.Rows.Add();
                dgvConceptos.Rows[row].Cells["colConcepto"].Value = d.Concepto;
                dgvConceptos.Rows[row].Cells["colTipo"].Value = d.Tipo;
                dgvConceptos.Rows[row].Cells["colMonto"].Value = d.MontoMensual;
                dgvConceptos.Rows[row].Cells["colOrigen"].Value = d.Origen;

                // Color segun tipo
                if (d.Tipo == "Fijo")
                    dgvConceptos.Rows[row].DefaultCellStyle.BackColor = Color.FromArgb(255, 245, 245);
                else if (d.Tipo == "Variable")
                    dgvConceptos.Rows[row].DefaultCellStyle.BackColor = Color.FromArgb(245, 255, 245);
                else
                    dgvConceptos.Rows[row].DefaultCellStyle.BackColor = Color.FromArgb(245, 250, 255);

                // Tag para referencia
                dgvConceptos.Rows[row].Tag = d;
            }
        }

        private void RefrescarKpis()
        {
            // Los valores se pintan en el evento Paint del panel
        }

        // -------------------------------------------------------------------------
        // ACCIONES GRID
        // -------------------------------------------------------------------------

        private void BtnAgregar_Click(object sender, EventArgs e)
        {
            using var dlg = new DlgConceptoPE(null);
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                dlg.Resultado.Origen = "Manual";
                _datos.Add(dlg.Resultado);
                _resultado = DatabaseService.CalcularPuntoEquilibrio(_datos);
                RefrescarGrid();
                pnlDiagrama.Invalidate();
                pnlKpis.Invalidate();
            }
        }

        private void BtnEditar_Click(object sender, EventArgs e)
        {
            if (dgvConceptos.SelectedRows.Count == 0) return;
            var dato = dgvConceptos.SelectedRows[0].Tag as DatoPE;
            if (dato == null) return;

            using var dlg = new DlgConceptoPE(dato);
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                var idx = _datos.IndexOf(dato);
                if (idx >= 0) _datos[idx] = dlg.Resultado;
                _resultado = DatabaseService.CalcularPuntoEquilibrio(_datos);
                RefrescarGrid();
                pnlDiagrama.Invalidate();
                pnlKpis.Invalidate();
            }
        }

        private void BtnEliminar_Click(object sender, EventArgs e)
        {
            if (dgvConceptos.SelectedRows.Count == 0) return;
            var dato = dgvConceptos.SelectedRows[0].Tag as DatoPE;
            if (dato == null) return;
            if (dato.Origen != "Manual")
            {
                MessageBox.Show("Solo se pueden eliminar conceptos agregados manualmente.\nLos datos automaticos se recalculan desde la base de datos.",
                    "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (MessageBox.Show($"Eliminar '{dato.Concepto}'?", "Confirmar",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                _datos.Remove(dato);
                _resultado = DatabaseService.CalcularPuntoEquilibrio(_datos);
                RefrescarGrid();
                pnlDiagrama.Invalidate();
                pnlKpis.Invalidate();
            }
        }

        private void BtnExportarPdf_Click(object sender, EventArgs e)
        {
            try
            {
                var bytes = PdfService.GenerarPuntoEquilibrio(_datos, _resultado, _meses);
                using var dlg = new SaveFileDialog
                {
                    Filter = "PDF|*.pdf",
                    FileName = $"PuntoEquilibrio_{DateTime.Now:yyyyMMdd}.pdf"
                };
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    System.IO.File.WriteAllBytes(dlg.FileName, bytes);
                    MessageBox.Show("PDF generado correctamente.", "Exito",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = dlg.FileName,
                        UseShellExecute = true
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al generar PDF: " + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // -------------------------------------------------------------------------
        // PINTURA KPIs
        // -------------------------------------------------------------------------

        private void PnlKpis_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var panel = sender as Panel;
            int w = panel.Width;
            int kpiW = (w - 20) / 6;
            int y = 8;

            var kpis = new[] {
                ("Ingresos/Mes", $"${_resultado.IngresosMensuales:N0}", Color.FromArgb(39,174,96)),
                ("Costos Variables", $"${_resultado.CostosVariables:N0}", Color.FromArgb(230,126,34)),
                ("Costos Fijos", $"${_resultado.CostosFijos:N0}", Color.FromArgb(192,57,43)),
                ("Punto de Equilibrio", $"${_resultado.PuntoEquilibrio:N0}", Color.FromArgb(41,128,185)),
                ("Margen Contribucion", $"{_resultado.MargenContribucion:P1}", Color.FromArgb(142,68,173)),
                ("Margen Seguridad", $"{_resultado.MargenSeguridad:P1}",
                    _resultado.MargenSeguridad >= 0.15m ? Color.FromArgb(39,174,96) : Color.FromArgb(192,57,43))
            };

            var fontTit = new Font("Segoe UI", 7.5f, FontStyle.Regular);
            var fontVal = new Font("Segoe UI", 11f, FontStyle.Bold);
            var sfC = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };

            for (int i = 0; i < kpis.Length; i++)
            {
                int x = 10 + i * (kpiW + 2);
                var rect = new Rectangle(x, y, kpiW, 72);

                // Fondo tarjeta
                using (var path = RoundedRect(rect, 8))
                using (var bg = new SolidBrush(Color.White))
                    g.FillPath(bg, path);

                // Barra color superior
                var barRect = new Rectangle(x, y, kpiW, 4);
                using (var barBrush = new SolidBrush(kpis[i].Item3))
                    g.FillRectangle(barBrush, barRect);

                // Titulo
                var titRect = new Rectangle(x, y + 6, kpiW, 20);
                using (var br = new SolidBrush(Color.FromArgb(100, 120, 140)))
                    g.DrawString(kpis[i].Item1, fontTit, br, titRect, sfC);

                // Valor
                var valRect = new Rectangle(x, y + 26, kpiW, 38);
                using (var br = new SolidBrush(kpis[i].Item3))
                    g.DrawString(kpis[i].Item2, fontVal, br, valRect, sfC);
            }

            fontTit.Dispose(); fontVal.Dispose(); sfC.Dispose();
        }

        // -------------------------------------------------------------------------
        // PINTURA DIAGRAMA GDI+
        // -------------------------------------------------------------------------

        private void PnlDiagrama_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            var panel = sender as Panel;
            int pw = panel.Width - 20;
            int ph = panel.Height - 20;
            int ox = 70, oy = 14;
            int cw = pw - ox - 20;
            int ch = ph - oy - 50;

            if (_resultado.IngresosMensuales == 0 || ch < 50 || cw < 50)
            {
                g.DrawString("Sin datos suficientes para graficar.", new Font("Segoe UI", 10f),
                    Brushes.Gray, new RectangleF(ox, oy + ch / 2, cw, 30));
                return;
            }

            // Escala: mostrar de 0 hasta 1.5x los ingresos
            decimal maxVal = _resultado.IngresosMensuales * 1.5m;
            if (maxVal <= 0) maxVal = 1;

            Func<decimal, float> toX = (unidades) => ox + (float)(unidades / maxVal * cw);
            Func<decimal, float> toY = (valor) => oy + ch - (float)(valor / maxVal * ch);

            // --- Fondo zonas ---
            decimal peX = _resultado.PuntoEquilibrio;

            // Zona perdida (izquierda del PE)
            if (peX > 0 && peX < maxVal)
            {
                float pexF = toX(peX);
                using var brLoss = new SolidBrush(Color.FromArgb(30, 192, 57, 43));
                g.FillRectangle(brLoss, ox, oy, pexF - ox, ch);

                using var brProfit = new SolidBrush(Color.FromArgb(30, 39, 174, 96));
                g.FillRectangle(brProfit, pexF, oy, ox + cw - pexF, ch);
            }

            // --- Ejes ---
            using var penEje = new Pen(Color.FromArgb(160, 160, 160), 1.5f);
            g.DrawLine(penEje, ox, oy, ox, oy + ch);
            g.DrawLine(penEje, ox, oy + ch, ox + cw, oy + ch);

            // --- Grid horizontal ---
            int nLines = 5;
            using var penGrid = new Pen(Color.FromArgb(230, 230, 230), 1f) { DashStyle = DashStyle.Dash };
            var fontEje = new Font("Segoe UI", 7.5f);
            for (int i = 1; i <= nLines; i++)
            {
                decimal val = maxVal * i / nLines;
                float yy = toY(val);
                g.DrawLine(penGrid, ox, yy, ox + cw, yy);
                g.DrawString($"${val:N0}", fontEje, Brushes.Gray, 2, yy - 7);
            }

            // --- Etiquetas eje X ---
            int nTicksX = 6;
            for (int i = 0; i <= nTicksX; i++)
            {
                decimal val = maxVal * i / nTicksX;
                float xx = toX(val);
                g.DrawLine(penGrid, xx, oy, xx, oy + ch);
                g.DrawString($"${val:N0}", fontEje, Brushes.Gray, xx - 15, oy + ch + 4);
            }

            // --- Linea costos fijos (horizontal) ---
            float yCF = toY(_resultado.CostosFijos);
            using var penFijo = new Pen(Color.FromArgb(192, 57, 43), 2f) { DashStyle = DashStyle.Dash };
            g.DrawLine(penFijo, ox, yCF, ox + cw, yCF);

            // --- Linea costos totales (fijos + variables proporcionales) ---
            // CT(x) = CostosFijos + x * (CostosVariables / IngresosMensuales)
            decimal cvRatio = _resultado.IngresosMensuales > 0
                ? _resultado.CostosVariables / _resultado.IngresosMensuales : 0;

            var ptsCT = new List<PointF>();
            for (int i = 0; i <= 100; i++)
            {
                decimal x = maxVal * i / 100;
                decimal ct = _resultado.CostosFijos + x * cvRatio;
                ptsCT.Add(new PointF(toX(x), toY(ct)));
            }
            using var penCT = new Pen(Color.FromArgb(230, 126, 34), 2.5f);
            g.DrawLines(penCT, ptsCT.ToArray());

            // --- Linea ingresos (diagonal desde 0) ---
            var ptsIng = new[] {
                new PointF(toX(0), toY(0)),
                new PointF(toX(maxVal), toY(maxVal))
            };
            using var penIng = new Pen(Color.FromArgb(39, 174, 96), 2.5f);
            g.DrawLines(penIng, ptsIng);

            // --- Punto de equilibrio ---
            float peXf = toX(peX);
            float peYf = toY(peX); // ingresos en PE = PE (porque PE = ventas necesarias)
            using var penPE = new Pen(Color.FromArgb(41, 128, 185), 2f) { DashStyle = DashStyle.Dot };
            g.DrawLine(penPE, peXf, oy, peXf, oy + ch);
            g.FillEllipse(new SolidBrush(Color.FromArgb(41, 128, 185)), peXf - 6, peYf - 6, 12, 12);
            g.DrawEllipse(new Pen(Color.White, 2f), peXf - 6, peYf - 6, 12, 12);

            // Etiqueta PE
            var fontPE = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            string txtPE = $" PE: ${peX:N0}";
            var szPE = g.MeasureString(txtPE, fontPE);
            float lx = peXf + 4;
            if (lx + szPE.Width > ox + cw) lx = peXf - szPE.Width - 4;
            g.DrawString(txtPE, fontPE, new SolidBrush(Color.FromArgb(41, 128, 185)), lx, peYf - 18);

            // --- Leyenda ---
            int legy = oy + 8;
            int legx = ox + cw - 200;
            DrawLeyenda(g, legx, legy, "Ingresos", Color.FromArgb(39, 174, 96));
            DrawLeyenda(g, legx, legy + 18, "Costos Totales", Color.FromArgb(230, 126, 34));
            DrawLeyenda(g, legx, legy + 36, "Costos Fijos", Color.FromArgb(192, 57, 43));
            DrawLeyenda(g, legx, legy + 54, "Punto Equilibrio", Color.FromArgb(41, 128, 185));

            // Titulo
            var fontTit = new Font("Segoe UI", 10f, FontStyle.Bold);
            g.DrawString("Diagrama de Punto de Equilibrio (USD mensuales)",
                fontTit, new SolidBrush(Color.FromArgb(44, 62, 80)), ox + 4, oy - 2);

            fontEje.Dispose(); fontPE.Dispose(); fontTit.Dispose();
        }

        private void DrawLeyenda(Graphics g, int x, int y, string texto, Color color)
        {
            g.FillRectangle(new SolidBrush(color), x, y + 3, 18, 10);
            g.DrawString(texto, new Font("Segoe UI", 7.5f), Brushes.DimGray, x + 22, y);
        }

        // -------------------------------------------------------------------------
        // HELPERS UI
        // -------------------------------------------------------------------------

        private Button CrearBotonToolbar(string texto, Color color)
        {
            return new Button
            {
                Text = texto,
                Size = new Size(105, 28),
                FlatStyle = FlatStyle.Flat,
                BackColor = color,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                FlatAppearance = { BorderSize = 0 }
            };
        }

        private Button CrearBotonGrid(string texto, Color color)
        {
            return new Button
            {
                Text = texto,
                Size = new Size(75, 26),
                FlatStyle = FlatStyle.Flat,
                BackColor = color,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                FlatAppearance = { BorderSize = 0 }
            };
        }

        private void EstilarDGV(DataGridView dgv)
        {
            dgv.EnableHeadersVisualStyles = false;
            dgv.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(44, 62, 80),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Alignment = DataGridViewContentAlignment.MiddleLeft
            };
            dgv.ColumnHeadersHeight = 32;
            dgv.DefaultCellStyle = new DataGridViewCellStyle
            {
                SelectionBackColor = Color.FromArgb(41, 128, 185),
                SelectionForeColor = Color.White,
                Padding = new Padding(4, 2, 4, 2)
            };
            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(249, 251, 253);
            dgv.RowTemplate.Height = 28;
            dgv.GridColor = Color.FromArgb(220, 228, 235);
        }

        private static GraphicsPath RoundedRect(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            path.AddArc(rect.X, rect.Y, radius * 2, radius * 2, 180, 90);
            path.AddArc(rect.Right - radius * 2, rect.Y, radius * 2, radius * 2, 270, 90);
            path.AddArc(rect.Right - radius * 2, rect.Bottom - radius * 2, radius * 2, radius * 2, 0, 90);
            path.AddArc(rect.X, rect.Bottom - radius * 2, radius * 2, radius * 2, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
