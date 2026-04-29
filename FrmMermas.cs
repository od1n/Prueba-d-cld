using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace SistemaGestion
{
    public class FrmMermas : FormBase
    {
        // Controles
        private DataGridView dgvOrdenes, dgvDetalle;
        private DateTimePicker dtpDesde, dtpHasta;
        private Button btnConsultar, btnExportarPdf;
        private Panel pnlKpis;

        // Datos
        private List<ResumenMerma> _mermas = new();

        public FrmMermas()
        {
            InitializeComponent();
            dtpDesde.Value = DateTime.Today.AddMonths(-3);
            dtpHasta.Value = DateTime.Today;
            Consultar();
        }

        private void InitializeComponent()
        {
            this.Text = "Control de Mermas / Desperdicios";
            this.Size = new Size(1050, 660);
            this.StartPosition = FormStartPosition.CenterParent;
            this.MinimumSize = new Size(850, 500);
            this.BackColor = Color.FromArgb(245, 247, 250);
            this.Font = new Font("Segoe UI", 9f);

            // --- Header ---
            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 50,
                BackColor = Color.FromArgb(100, 50, 10)
            };
            pnlHeader.Controls.Add(new Label
            {
                Text = "  Control de Mermas y Desperdicios — Ordenes Completadas",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(8, 14)
            });

            // --- Toolbar ---
            var pnlToolbar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 44,
                BackColor = Color.FromArgb(235, 237, 240),
                Padding = new Padding(8, 6, 8, 6)
            };

            pnlToolbar.Controls.Add(new Label { Text = "Desde:", AutoSize = true, Location = new Point(8, 14), ForeColor = Color.FromArgb(44,62,80) });
            dtpDesde = new DateTimePicker { Format = DateTimePickerFormat.Short, Width = 100, Location = new Point(55, 10) };
            pnlToolbar.Controls.Add(dtpDesde);

            pnlToolbar.Controls.Add(new Label { Text = "Hasta:", AutoSize = true, Location = new Point(168, 14), ForeColor = Color.FromArgb(44,62,80) });
            dtpHasta = new DateTimePicker { Format = DateTimePickerFormat.Short, Width = 100, Location = new Point(210, 10) };
            pnlToolbar.Controls.Add(dtpHasta);

            btnConsultar = new Button
            {
                Text = "Consultar", Size = new Size(90, 28), Location = new Point(324, 8),
                FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(41, 128, 185),
                ForeColor = Color.White, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                FlatAppearance = { BorderSize = 0 }, Cursor = Cursors.Hand
            };
            btnConsultar.Click += (s, e) => Consultar();
            pnlToolbar.Controls.Add(btnConsultar);

            btnExportarPdf = new Button
            {
                Text = "Exportar PDF", Size = new Size(105, 28), Location = new Point(424, 8),
                FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(192, 57, 43),
                ForeColor = Color.White, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                FlatAppearance = { BorderSize = 0 }, Cursor = Cursors.Hand
            };
            btnExportarPdf.Click += BtnExportarPdf_Click;
            pnlToolbar.Controls.Add(btnExportarPdf);

            // --- KPIs ---
            pnlKpis = new Panel
            {
                Dock = DockStyle.Top,
                Height = 70,
                BackColor = Color.FromArgb(245, 247, 250)
            };
            pnlKpis.Paint += PnlKpis_Paint;

            // --- Split: ordenes arriba, detalle abajo ---
            var split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                BackColor = Color.FromArgb(245, 247, 250),
                Panel1MinSize = 150,
                Panel2MinSize = 120
            };

            // Grid ordenes
            var lblOrd = new Label
            {
                Text = "Ordenes de Produccion Completadas:",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(44, 62, 80),
                AutoSize = true,
                Location = new Point(6, 4)
            };
            dgvOrdenes = CrearGrid(Color.FromArgb(100, 50, 10));
            dgvOrdenes.Location = new Point(0, 24);
            dgvOrdenes.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            ConfigurarColumnasOrdenes();
            dgvOrdenes.SelectionChanged += DgvOrdenes_SelectionChanged;

            split.Panel1.Controls.Add(lblOrd);
            split.Panel1.Controls.Add(dgvOrdenes);

            // Grid detalle materias primas
            var lblDet = new Label
            {
                Text = "Detalle de Materias Primas — Teorico vs Real:",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(44, 62, 80),
                AutoSize = true,
                Location = new Point(6, 4)
            };
            dgvDetalle = CrearGrid(Color.FromArgb(60, 90, 40));
            dgvDetalle.Location = new Point(0, 24);
            dgvDetalle.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            ConfigurarColumnasDetalle();

            split.Panel2.Controls.Add(lblDet);
            split.Panel2.Controls.Add(dgvDetalle);

            // Footer
            var pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom, Height = 36,
                BackColor = Color.FromArgb(235, 237, 240)
            };
            var btnCerrar = new Button
            {
                Text = "Cerrar", Size = new Size(90, 26), Location = new Point(940, 5),
                FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(44, 62, 80),
                ForeColor = Color.White, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                FlatAppearance = { BorderSize = 0 }
            };
            btnCerrar.Click += (s, e) => Close();
            pnlFooter.Controls.Add(btnCerrar);

            this.Controls.Add(split);
            this.Controls.Add(pnlKpis);
            this.Controls.Add(pnlToolbar);
            this.Controls.Add(pnlHeader);
            this.Controls.Add(pnlFooter);

            this.Load += (s, e) => {
                split.SplitterDistance = (int)(split.Height * 0.55);
                AjustarGrids(split.Panel1.ClientSize, split.Panel2.ClientSize);
            };
            split.SplitterMoved += (s, e) =>
                AjustarGrids(split.Panel1.ClientSize, split.Panel2.ClientSize);
            this.Resize += (s, e) =>
                AjustarGrids(split.Panel1.ClientSize, split.Panel2.ClientSize);
        }

        private void AjustarGrids(Size s1, Size s2)
        {
            dgvOrdenes.Size  = new Size(s1.Width, s1.Height - 28);
            dgvDetalle.Size  = new Size(s2.Width, s2.Height - 28);
        }

        // -------------------------------------------------------------------------
        // COLUMNAS
        // -------------------------------------------------------------------------

        private void ConfigurarColumnasOrdenes()
        {
            dgvOrdenes.Columns.Clear();
            dgvOrdenes.Columns.Add(new DataGridViewTextBoxColumn { Name = "colNumOrden",   HeaderText = "N° Orden",      FillWeight = 12 });
            dgvOrdenes.Columns.Add(new DataGridViewTextBoxColumn { Name = "colProducto",   HeaderText = "Producto",      FillWeight = 25 });
            dgvOrdenes.Columns.Add(new DataGridViewTextBoxColumn { Name = "colFechaFin",   HeaderText = "Fecha Cierre",  FillWeight = 12 });
            dgvOrdenes.Columns.Add(new DataGridViewTextBoxColumn { Name = "colPlan",       HeaderText = "Cant. Plan",    FillWeight = 10,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight } });
            dgvOrdenes.Columns.Add(new DataGridViewTextBoxColumn { Name = "colReal",       HeaderText = "Cant. Real",    FillWeight = 10,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight } });
            dgvOrdenes.Columns.Add(new DataGridViewTextBoxColumn { Name = "colMermaTot",   HeaderText = "Merma Total",   FillWeight = 12,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight } });
            dgvOrdenes.Columns.Add(new DataGridViewTextBoxColumn { Name = "colPctMerma",   HeaderText = "% Merma",       FillWeight = 10,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight } });
            dgvOrdenes.Columns.Add(new DataGridViewTextBoxColumn { Name = "colNMateriales",HeaderText = "Materiales",    FillWeight = 9,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight } });
        }

        private void ConfigurarColumnasDetalle()
        {
            dgvDetalle.Columns.Clear();
            dgvDetalle.Columns.Add(new DataGridViewTextBoxColumn { Name = "colMatCod",    HeaderText = "Codigo",         FillWeight = 12 });
            dgvDetalle.Columns.Add(new DataGridViewTextBoxColumn { Name = "colMatDesc",   HeaderText = "Material",       FillWeight = 28 });
            dgvDetalle.Columns.Add(new DataGridViewTextBoxColumn { Name = "colTeorico",   HeaderText = "Consumo Teorico",FillWeight = 15,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight, Format = "N2" } });
            dgvDetalle.Columns.Add(new DataGridViewTextBoxColumn { Name = "colReal2",     HeaderText = "Consumo Real",   FillWeight = 15,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight, Format = "N2" } });
            dgvDetalle.Columns.Add(new DataGridViewTextBoxColumn { Name = "colMerma",     HeaderText = "Merma",          FillWeight = 12,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight, Format = "N2" } });
            dgvDetalle.Columns.Add(new DataGridViewTextBoxColumn { Name = "colPctMat",    HeaderText = "% Merma",        FillWeight = 10,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight } });
            dgvDetalle.Columns.Add(new DataGridViewTextBoxColumn { Name = "colNivel",     HeaderText = "Nivel",          FillWeight = 8 });
        }

        // -------------------------------------------------------------------------
        // CONSULTAR
        // -------------------------------------------------------------------------

        private void Consultar()
        {
            try
            {
                _mermas = DatabaseService.ObtenerMermas(dtpDesde.Value, dtpHasta.Value);
                CargarGridOrdenes();
                dgvDetalle.Rows.Clear();
                pnlKpis.Invalidate();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al consultar: " + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CargarGridOrdenes()
        {
            dgvOrdenes.Rows.Clear();
            foreach (var m in _mermas.OrderByDescending(x => x.PctMermaTotal))
            {
                int row = dgvOrdenes.Rows.Add();
                dgvOrdenes.Rows[row].Cells["colNumOrden"].Value    = m.NumOrden;
                dgvOrdenes.Rows[row].Cells["colProducto"].Value    = m.ProductoDesc;
                dgvOrdenes.Rows[row].Cells["colFechaFin"].Value    = m.FechaFin.ToString("dd/MM/yyyy");
                dgvOrdenes.Rows[row].Cells["colPlan"].Value        = m.CantidadPlan.ToString("N2");
                dgvOrdenes.Rows[row].Cells["colReal"].Value        = m.CantidadReal.ToString("N2");
                dgvOrdenes.Rows[row].Cells["colMermaTot"].Value    = m.MermaTotal.ToString("N2");
                dgvOrdenes.Rows[row].Cells["colPctMerma"].Value    = m.PctMermaTotal.ToString("P1");
                dgvOrdenes.Rows[row].Cells["colNMateriales"].Value = m.Lineas.Count.ToString();
                dgvOrdenes.Rows[row].Tag = m;

                // Color segun nivel de merma
                dgvOrdenes.Rows[row].DefaultCellStyle.BackColor =
                    m.PctMermaTotal > 0.15m ? Color.FromArgb(255, 210, 210) :
                    m.PctMermaTotal > 0.05m ? Color.FromArgb(255, 243, 220) :
                                              Color.FromArgb(240, 255, 240);
            }
        }

        private void DgvOrdenes_SelectionChanged(object sender, EventArgs e)
        {
            dgvDetalle.Rows.Clear();
            if (dgvOrdenes.SelectedRows.Count == 0) return;
            var merma = dgvOrdenes.SelectedRows[0].Tag as ResumenMerma;
            if (merma == null) return;

            foreach (var l in merma.Lineas.OrderByDescending(x => x.PctMerma))
            {
                int row = dgvDetalle.Rows.Add();
                dgvDetalle.Rows[row].Cells["colMatCod"].Value  = l.MateriaCod;
                dgvDetalle.Rows[row].Cells["colMatDesc"].Value = l.MateriaDesc;
                dgvDetalle.Rows[row].Cells["colTeorico"].Value = l.ConsumoTeorico;
                dgvDetalle.Rows[row].Cells["colReal2"].Value   = l.ConsumoReal;
                dgvDetalle.Rows[row].Cells["colMerma"].Value   = l.Merma;
                dgvDetalle.Rows[row].Cells["colPctMat"].Value  = l.PctMerma.ToString("P1");
                dgvDetalle.Rows[row].Cells["colNivel"].Value   = l.Nivel;

                dgvDetalle.Rows[row].DefaultCellStyle.BackColor =
                    l.Nivel == "CRITICO" ? Color.FromArgb(255, 210, 210) :
                    l.Nivel == "ALTO"    ? Color.FromArgb(255, 235, 200) :
                    l.Nivel == "MEDIO"   ? Color.FromArgb(255, 250, 210) :
                                           Color.FromArgb(240, 255, 240);
            }
        }

        // -------------------------------------------------------------------------
        // KPIs
        // -------------------------------------------------------------------------

        private void PnlKpis_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var panel = sender as Panel;
            int w = panel.Width;
            int kpiW = (w - 20) / 4;

            decimal totalMerma   = _mermas.Sum(m => m.MermaTotal);
            decimal totalTeorico = _mermas.Sum(m => m.Lineas.Sum(l => l.ConsumoTeorico));
            decimal pctGlobal    = totalTeorico > 0 ? totalMerma / totalTeorico : 0;
            int     nOrdenes     = _mermas.Count;
            int     nCriticas    = _mermas.Count(m => m.PctMermaTotal > 0.15m);

            var kpis = new (string Label, string Valor, Color Color)[]
            {
                ("Ordenes Analizadas", nOrdenes.ToString(), Color.FromArgb(41, 128, 185)),
                ("Merma Total", totalMerma.ToString("N2"), Color.FromArgb(192, 57, 43)),
                ("% Merma Global", pctGlobal.ToString("P1"),
                    pctGlobal > 0.10m ? Color.FromArgb(192,57,43) :
                    pctGlobal > 0.05m ? Color.FromArgb(230,126,34) :
                                        Color.FromArgb(39,174,96)),
                ("Ordenes Criticas (>15%)", nCriticas.ToString(),
                    nCriticas > 0 ? Color.FromArgb(192,57,43) : Color.FromArgb(39,174,96))
            };

            var fontTit = new Font("Segoe UI", 7.5f);
            var fontVal = new Font("Segoe UI", 12f, FontStyle.Bold);
            var sfC = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };

            for (int i = 0; i < kpis.Length; i++)
            {
                int x = 10 + i * (kpiW + 2);
                var rect = new Rectangle(x, 6, kpiW, 58);

                using (var path = RoundedRect(rect, 6))
                    g.FillPath(Brushes.White, path);

                var barRect = new Rectangle(x, 6, kpiW, 3);
                using (var br = new SolidBrush(kpis[i].Color))
                    g.FillRectangle(br, barRect);

                using (var br = new SolidBrush(Color.FromArgb(100,120,140)))
                    g.DrawString(kpis[i].Label, fontTit, br, new RectangleF(x, 10, kpiW, 20), sfC);

                using (var br = new SolidBrush(kpis[i].Color))
                    g.DrawString(kpis[i].Valor, fontVal, br, new RectangleF(x, 28, kpiW, 34), sfC);
            }

            fontTit.Dispose(); fontVal.Dispose(); sfC.Dispose();
        }

        // -------------------------------------------------------------------------
        // PDF
        // -------------------------------------------------------------------------

        private void BtnExportarPdf_Click(object sender, EventArgs e)
        {
            if (_mermas.Count == 0)
            {
                MessageBox.Show("No hay datos para exportar.", "Sin datos",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            try
            {
                var bytes = PdfService.GenerarReporteMermas(_mermas, dtpDesde.Value, dtpHasta.Value);
                using var dlg = new SaveFileDialog
                {
                    Filter = "PDF|*.pdf",
                    FileName = $"Mermas_{DateTime.Now:yyyyMMdd}.pdf"
                };
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    System.IO.File.WriteAllBytes(dlg.FileName, bytes);
                    MessageBox.Show("PDF generado correctamente.", "Exito",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    { FileName = dlg.FileName, UseShellExecute = true });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al generar PDF: " + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // -------------------------------------------------------------------------
        // HELPERS
        // -------------------------------------------------------------------------

        private DataGridView CrearGrid(Color colorHeader)
        {
            var dgv = new DataGridView
            {
                ReadOnly = true, AllowUserToAddRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false, BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None, RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                Font = new Font("Segoe UI", 9f)
            };
            dgv.EnableHeadersVisualStyles = false;
            dgv.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = colorHeader, ForeColor = Color.White,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };
            dgv.ColumnHeadersHeight = 30;
            dgv.RowTemplate.Height = 24;
            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(249,251,253);
            dgv.DefaultCellStyle = new DataGridViewCellStyle
            {
                SelectionBackColor = Color.FromArgb(41,128,185),
                SelectionForeColor = Color.White
            };
            return dgv;
        }

        private static GraphicsPath RoundedRect(Rectangle rect, int r)
        {
            var path = new GraphicsPath();
            path.AddArc(rect.X, rect.Y, r*2, r*2, 180, 90);
            path.AddArc(rect.Right-r*2, rect.Y, r*2, r*2, 270, 90);
            path.AddArc(rect.Right-r*2, rect.Bottom-r*2, r*2, r*2, 0, 90);
            path.AddArc(rect.X, rect.Bottom-r*2, r*2, r*2, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
