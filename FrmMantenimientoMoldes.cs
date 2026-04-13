using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace SistemaGestion
{
    // ══════════════════════════════════════════════════════════════════════════
    //  PROGRAMA DE MANTENIMIENTO DE MOLDES
    // ══════════════════════════════════════════════════════════════════════════
    public class FrmMantenimientoMoldes : Form
    {
        private TabControl tabs;

        // Tab Programa
        private DataGridView dgvPrograma;
        private Panel        pnlCalendario;
        private Label        lblResPrograma;

        // Tab Historial
        private DataGridView dgvHistorial;
        private Label        lblResHistorial;
        private ComboBox     cmbFiltroMolde;

        // KPIs
        private Label lblVencidos, lblUrgentes, lblProximos, lblOk;
        private Label lblCostoTotal, lblMoldesActivos;

        public FrmMantenimientoMoldes()
        {
            Text          = "Programa de Mantenimiento — Moldes";
            Size          = new Size(1200, 700);
            MinimumSize   = new Size(900, 580);
            StartPosition = FormStartPosition.CenterParent;
            BackColor     = Color.FromArgb(245, 248, 252);

            BuildHeader();
            BuildKpis();
            BuildTabs();
            CargarTodo();
        }

        // ══════════════════════════════════════════════════════════════════
        //  CONSTRUCCION UI
        // ══════════════════════════════════════════════════════════════════
        private void BuildHeader()
        {
            var hdr = new Panel { Dock = DockStyle.Top, Height = 50,
                BackColor = Color.FromArgb(20, 80, 120) };
            hdr.Controls.Add(new Label
            {
                Text = "Mantenimiento de Moldes", ForeColor = Color.White,
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                AutoSize = true, Location = new Point(14, 12)
            });

            var btnPdf = new Button
            {
                Text = "Exportar PDF", Location = new Point(1060, 10), Size = new Size(110, 30),
                BackColor = Color.FromArgb(180, 60, 20), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold)
            };
            btnPdf.FlatAppearance.BorderSize = 0;
            btnPdf.Click += (s, e) => ExportarPdf();

            var btnCerrar = new Button
            {
                Text = "Cerrar", Location = new Point(1080, 10), Size = new Size(80, 30),
                BackColor = Color.FromArgb(100, 100, 120), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnCerrar.FlatAppearance.BorderSize = 0;
            btnCerrar.Click += (s, e) => Close();
            hdr.Controls.AddRange(new Control[] { btnPdf, btnCerrar });
            Controls.Add(hdr);
        }

        private void BuildKpis()
        {
            var pnlK = new Panel { Dock = DockStyle.Top, Height = 72,
                BackColor = Color.FromArgb(232, 240, 248) };

            lblVencidos    = Kpi(pnlK, "VENCIDOS",      Color.FromArgb(180, 40, 40),   0);
            lblUrgentes    = Kpi(pnlK, "URGENTES <7d",  Color.FromArgb(200, 110, 20),  1);
            lblProximos    = Kpi(pnlK, "PROXIMOS <30d", Color.FromArgb(160, 140, 20),  2);
            lblOk          = Kpi(pnlK, "AL DIA",        Color.FromArgb(30, 130, 60),   3);
            lblCostoTotal  = Kpi(pnlK, "Costo historico",Color.FromArgb(40, 80, 160),  4);
            lblMoldesActivos=Kpi(pnlK, "Moldes en prog.",Color.FromArgb(60, 100, 150), 5);

            Controls.Add(pnlK);
        }

        private void BuildTabs()
        {
            tabs = new TabControl { Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9.5f) };
            tabs.TabPages.Add(BuildTabPrograma());
            tabs.TabPages.Add(BuildTabHistorial());
            Controls.Add(tabs);
        }

        // ── Tab: Programa de mantenimiento ────────────────────────────────
        private TabPage BuildTabPrograma()
        {
            var tab = new TabPage("Programa de Mantenimiento");

            // Toolbar
            var toolbar = new Panel { Dock = DockStyle.Top, Height = 42,
                BackColor = Color.FromArgb(225, 238, 248) };

            var btnNuevoProg = BtnT("+ Agregar molde al programa", Color.FromArgb(30, 130, 60),  new Point(8,  7), 200);
            var btnEditProg  = BtnT("Editar frecuencia",           Color.FromArgb(40, 100, 180), new Point(218, 7), 130);
            var btnElimProg  = BtnT("Quitar del programa",         Color.FromArgb(160, 50, 50),  new Point(358, 7), 130);
            var btnRegistrar = BtnT("Registrar mantenimiento",     Color.FromArgb(160, 80, 20),  new Point(498, 7), 180);

            toolbar.Controls.AddRange(new Control[]
                { btnNuevoProg, btnEditProg, btnElimProg, btnRegistrar });

            // Grid programa
            dgvPrograma = Grid(Color.FromArgb(20, 80, 120));
            dgvPrograma.DataBindingComplete += (s, e) => ColorearPrograma();
            dgvPrograma.SelectionChanged    += (s, e) => pnlCalendario?.Invalidate();

            // Panel derecho: mini-calendario de vencimientos
            pnlCalendario = new Panel { Dock = DockStyle.Right, Width = 280,
                BackColor = Color.White };
            pnlCalendario.Paint += PaintCalendario;

            var sep = new Panel { Dock = DockStyle.Right, Width = 3,
                BackColor = Color.FromArgb(200, 210, 225) };

            // Footer
            var footer = new Panel { Dock = DockStyle.Bottom, Height = 34,
                BackColor = Color.FromArgb(215, 232, 245) };
            lblResPrograma = new Label { AutoSize = true, Location = new Point(10, 8),
                Font = new Font("Segoe UI", 9f), ForeColor = Color.FromArgb(20, 60, 100) };
            footer.Controls.Add(lblResPrograma);

            var pnlGrid = new Panel { Dock = DockStyle.Fill };
            pnlGrid.Controls.Add(dgvPrograma);
            pnlGrid.Controls.Add(sep);
            pnlGrid.Controls.Add(pnlCalendario);

            tab.Controls.Add(pnlGrid);
            tab.Controls.Add(footer);
            tab.Controls.Add(toolbar);

            // Eventos
            btnNuevoProg.Click += (s, e) => AgregarAlPrograma();
            btnEditProg.Click  += (s, e) => EditarPrograma();
            btnElimProg.Click  += (s, e) => EliminarPrograma();
            btnRegistrar.Click += (s, e) => RegistrarMantenimiento();

            return tab;
        }

        // ── Tab: Historial ────────────────────────────────────────────────
        private TabPage BuildTabHistorial()
        {
            var tab = new TabPage("Historial de Mantenimientos");

            var toolbar = new Panel { Dock = DockStyle.Top, Height = 42,
                BackColor = Color.FromArgb(225, 238, 248) };
            toolbar.Controls.Add(new Label { Text = "Filtrar molde:", Location = new Point(10, 13), AutoSize = true });
            cmbFiltroMolde = new ComboBox { Location = new Point(100, 9), Size = new Size(200, 26),
                DropDownStyle = ComboBoxStyle.DropDownList };
            cmbFiltroMolde.Items.Add("Todos");
            foreach (var m in DatabaseService.ObtenerMoldes())
                cmbFiltroMolde.Items.Add(m.CodigoMold + " — " + m.Descripcion);
            cmbFiltroMolde.SelectedIndex = 0;
            cmbFiltroMolde.SelectedIndexChanged += (s, e) => CargarHistorial();
            toolbar.Controls.Add(cmbFiltroMolde);

            var btnElimH = BtnT("Eliminar registro", Color.FromArgb(160, 50, 50), new Point(315, 7), 140);
            toolbar.Controls.Add(btnElimH);

            dgvHistorial = Grid(Color.FromArgb(20, 80, 120));
            dgvHistorial.DataBindingComplete += (s, e) => ColorearHistorial();

            var footer = new Panel { Dock = DockStyle.Bottom, Height = 34,
                BackColor = Color.FromArgb(215, 232, 245) };
            lblResHistorial = new Label { AutoSize = true, Location = new Point(10, 8),
                Font = new Font("Segoe UI", 9f), ForeColor = Color.FromArgb(20, 60, 100) };
            footer.Controls.Add(lblResHistorial);

            tab.Controls.Add(dgvHistorial);
            tab.Controls.Add(footer);
            tab.Controls.Add(toolbar);

            btnElimH.Click += (s, e) =>
            {
                if (dgvHistorial.CurrentRow == null) return;
                int id = (int)dgvHistorial.CurrentRow.Cells["Id"].Value;
                if (MessageBox.Show("Eliminar este registro de mantenimiento?", "Confirmar",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    DatabaseService.EliminarRegistroMant(id);
                    CargarHistorial();
                }
            };

            return tab;
        }

        // ══════════════════════════════════════════════════════════════════
        //  CARGA DE DATOS
        // ══════════════════════════════════════════════════════════════════
        private void CargarTodo()
        {
            CargarPrograma();
            CargarHistorial();
            ActualizarKpis();
        }

        private void CargarPrograma()
        {
            var lista = DatabaseService.ObtenerProgramas();
            dgvPrograma.DataSource = null;
            dgvPrograma.DataSource = lista.Select(p => new
            {
                p.CodigoMolde,
                Molde         = p.DescMolde,
                p.TipoMant,
                Frecuencia    = p.FrecuenciaDias + " dias",
                UltMant       = p.UltimoMant.ToShortDateString(),
                ProxMant      = p.ProximoMant.ToShortDateString(),
                DiasRest      = p.DiasRestantes,
                p.Estado,
                Activo        = p.Activo ? "Si" : "No"
            }).ToList();

            var counts = lista.GroupBy(p => p.Estado).ToDictionary(g => g.Key, g => g.Count());
            lblResPrograma.Text =
                "  Moldes en programa: " + lista.Count +
                "   |   Vencidos: "  + (counts.GetValueOrDefault("VENCIDO", 0)) +
                "   |   Urgentes: "  + (counts.GetValueOrDefault("URGENTE", 0)) +
                "   |   Proximos: "  + (counts.GetValueOrDefault("PROXIMO", 0)) +
                "   |   Al dia: "    + (counts.GetValueOrDefault("OK", 0));
        }

        private void CargarHistorial()
        {
            string filtro = cmbFiltroMolde.SelectedItem?.ToString();
            string cod    = (filtro == null || filtro == "Todos") ? null
                          : filtro.Split('—')[0].Trim();

            var lista = DatabaseService.ObtenerRegistrosMant(cod);
            dgvHistorial.DataSource = null;
            dgvHistorial.DataSource = lista.Select(r => new
            {
                r.Id,
                r.CodigoMolde,
                Fecha     = r.FechaRealizada.ToShortDateString(),
                r.TipoMant,
                r.Tecnico,
                r.Descripcion,
                Costo     = r.CostoMant.ToString("C2"),
                Horas     = r.DuracionHoras,
                r.Resultado,
                ProxMant  = r.ProximoMant.ToShortDateString(),
                r.Observaciones
            }).ToList();

            decimal costoTotal = lista.Sum(r => r.CostoMant);
            lblResHistorial.Text =
                "  Registros: " + lista.Count +
                "   |   Costo total historico: " + costoTotal.ToString("C2") +
                "   |   Completados: " + lista.Count(r => r.Resultado == "Completado");
        }

        private void ActualizarKpis()
        {
            var prog = DatabaseService.ObtenerProgramas(soloActivos: true);
            int venc = prog.Count(p => p.Estado == "VENCIDO");
            int urg  = prog.Count(p => p.Estado == "URGENTE");
            int prox = prog.Count(p => p.Estado == "PROXIMO");
            int ok   = prog.Count(p => p.Estado == "OK");

            var hist = DatabaseService.ObtenerRegistrosMant();
            decimal costo = hist.Sum(r => r.CostoMant);

            lblVencidos.Text     = venc.ToString();
            lblUrgentes.Text     = urg.ToString();
            lblProximos.Text     = prox.ToString();
            lblOk.Text           = ok.ToString();
            lblCostoTotal.Text   = costo >= 1000 ? (costo / 1000).ToString("F1") + "k" : costo.ToString("C0");
            lblMoldesActivos.Text= prog.Count.ToString();
        }

        // ══════════════════════════════════════════════════════════════════
        //  COLOREADO
        // ══════════════════════════════════════════════════════════════════
        private void ColorearPrograma()
        {
            foreach (DataGridViewRow row in dgvPrograma.Rows)
            {
                var est = row.Cells["Estado"].Value?.ToString();
                row.DefaultCellStyle.BackColor = est switch
                {
                    "VENCIDO" => Color.FromArgb(255, 215, 215),
                    "URGENTE" => Color.FromArgb(255, 238, 200),
                    "PROXIMO" => Color.FromArgb(255, 252, 210),
                    _         => Color.FromArgb(225, 255, 228)
                };
                row.Cells["Estado"].Style.ForeColor = est switch
                {
                    "VENCIDO" => Color.FromArgb(160, 20, 20),
                    "URGENTE" => Color.FromArgb(160, 80, 10),
                    "PROXIMO" => Color.FromArgb(130, 110, 10),
                    _         => Color.FromArgb(20, 120, 40)
                };
                row.Cells["Estado"].Style.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            }
        }

        private void ColorearHistorial()
        {
            foreach (DataGridViewRow row in dgvHistorial.Rows)
            {
                var res = row.Cells["Resultado"].Value?.ToString();
                if (res == "Completado")
                    row.DefaultCellStyle.BackColor = Color.FromArgb(225, 255, 228);
                else if (res == "Cancelado")
                    row.DefaultCellStyle.BackColor = Color.FromArgb(255, 225, 225);
            }
        }

        // ══════════════════════════════════════════════════════════════════
        //  CALENDARIO DE VENCIMIENTOS (GDI+)
        // ══════════════════════════════════════════════════════════════════
        private void PaintCalendario(object sender, PaintEventArgs e)
        {
            var g   = e.Graphics;
            var pnl = (Panel)sender;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // Titulo
            g.FillRectangle(new SolidBrush(Color.FromArgb(20, 80, 120)),
                0, 0, pnl.Width, 28);
            using var fntT = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            g.DrawString("Proximos 90 dias", fntT, Brushes.White, 10, 7);

            var prog = DatabaseService.ObtenerProgramas(soloActivos: true)
                .OrderBy(p => p.DiasRestantes)
                .Take(12)
                .ToList();

            if (prog.Count == 0)
            {
                g.DrawString("Sin programas activos", new Font("Segoe UI", 9f),
                    Brushes.Gray, 20, 50);
                return;
            }

            // Linea de tiempo: 0 a 90 dias
            int padL = 110, padR = 50, padT = 40, rowH = 22;
            int w = pnl.Width - padL - padR;

            // Marcadores de dias (0, 30, 60, 90)
            using var fntS  = new Font("Segoe UI", 7f);
            using var penG  = new Pen(Color.FromArgb(220, 225, 235), 1f) { DashStyle = DashStyle.Dash };
            using var penEje= new Pen(Color.FromArgb(180, 190, 210), 1f);

            foreach (int dia in new[] { 0, 30, 60, 90 })
            {
                float x = padL + (float)w * dia / 90;
                g.DrawLine(penG, x, padT, x, padT + prog.Count * rowH + 10);
                g.DrawString(dia == 0 ? "Hoy" : dia + "d", fntS,
                    Brushes.Gray, x - 8, padT - 14);
            }

            // Fila por molde
            for (int i = 0; i < prog.Count; i++)
            {
                var p  = prog[i];
                int y  = padT + i * rowH;

                // Nombre del molde
                g.DrawString(p.CodigoMolde, fntS, Brushes.DimGray,
                    new RectangleF(2, y + 2, padL - 6, rowH - 2),
                    new StringFormat { Alignment = StringAlignment.Far,
                                       LineAlignment = StringAlignment.Center });

                // Color segun estado
                Color barColor = p.Estado switch
                {
                    "VENCIDO" => Color.FromArgb(200, 50, 50),
                    "URGENTE" => Color.FromArgb(210, 110, 20),
                    "PROXIMO" => Color.FromArgb(180, 160, 20),
                    _         => Color.FromArgb(30, 140, 60)
                };

                // Barra que indica el dia de vencimiento
                int diasClamped = Math.Max(-10, Math.Min(p.DiasRestantes, 90));
                float xVenc = padL + (float)w * Math.Max(0, diasClamped) / 90;

                if (diasClamped <= 0)
                {
                    // Vencido: barra desde 0 hacia la izquierda (indicador)
                    g.FillRectangle(new SolidBrush(Color.FromArgb(200, 50, 50)),
                        padL - 6, y + 5, 6, rowH - 10);
                }
                else
                {
                    // Barra desde hoy hasta vencimiento
                    g.FillRectangle(new SolidBrush(Color.FromArgb(60, barColor)),
                        padL, y + 6, xVenc - padL, rowH - 12);
                }

                // Marcador de vencimiento
                using var brMark = new SolidBrush(barColor);
                g.FillEllipse(brMark, xVenc - 5, y + 5, 10, rowH - 10);

                // Texto de dias
                string diasStr = p.DiasRestantes < 0
                    ? Math.Abs(p.DiasRestantes) + "d atras"
                    : p.DiasRestantes + "d";
                g.DrawString(diasStr, fntS,
                    p.DiasRestantes < 0 ? Brushes.DarkRed : Brushes.DimGray,
                    xVenc + 7, y + 4);
            }

            // Leyenda
            int ly = padT + prog.Count * rowH + 16;
            int[] lColors = { Color.FromArgb(200, 50, 50).ToArgb(),
                              Color.FromArgb(210, 110, 20).ToArgb(),
                              Color.FromArgb(180, 160, 20).ToArgb(),
                              Color.FromArgb(30, 140, 60).ToArgb() };
            string[] lTxt = { "Vencido", "Urgente", "Proximo", "Al dia" };
            for (int i = 0; i < 4 && ly + 14 < pnl.Height; i++)
            {
                g.FillRectangle(new SolidBrush(Color.FromArgb(lColors[i])),
                    padL + i * 60, ly, 8, 8);
                g.DrawString(lTxt[i], fntS, Brushes.Gray, padL + i * 60 + 10, ly);
            }
        }

        // ══════════════════════════════════════════════════════════════════
        //  ACCIONES
        // ══════════════════════════════════════════════════════════════════
        private void AgregarAlPrograma()
        {
            using var dlg = new DlgPrograma(null);
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                DatabaseService.GuardarPrograma(dlg.Resultado);
                CargarTodo();
            }
        }

        private void EditarPrograma()
        {
            if (dgvPrograma.CurrentRow == null) return;
            var cod = dgvPrograma.CurrentRow.Cells["CodigoMolde"].Value?.ToString();
            var prog = DatabaseService.ObtenerProgramas().Find(p => p.CodigoMolde == cod);
            if (prog == null) return;
            using var dlg = new DlgPrograma(prog);
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                prog = dlg.Resultado;
                DatabaseService.GuardarPrograma(prog);
                CargarTodo();
            }
        }

        private void EliminarPrograma()
        {
            if (dgvPrograma.CurrentRow == null) return;
            var cod = dgvPrograma.CurrentRow.Cells["CodigoMolde"].Value?.ToString();
            var prog = DatabaseService.ObtenerProgramas().Find(p => p.CodigoMolde == cod);
            if (prog == null) return;
            if (MessageBox.Show("Quitar " + cod + " del programa de mantenimiento?",
                "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                DatabaseService.EliminarPrograma(prog.Id);
                CargarTodo();
            }
        }

        private void RegistrarMantenimiento()
        {
            // Si hay molde seleccionado en el programa, pre-seleccionarlo
            string codPre = null;
            if (dgvPrograma.CurrentRow != null)
                codPre = dgvPrograma.CurrentRow.Cells["CodigoMolde"].Value?.ToString();

            using var dlg = new DlgRegistroMant(codPre);
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    DatabaseService.InsertarRegistroMant(dlg.Resultado);
                    CargarTodo();
                    MessageBox.Show(
                        "Mantenimiento registrado.\n" +
                        "Proximo mantenimiento: " + dlg.Resultado.ProximoMant.ToString("dd/MM/yyyy"),
                        "Registrado", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: " + ex.Message, "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void ExportarPdf()
        {
            try
            {
                var prog = DatabaseService.ObtenerProgramas();
                var hist = DatabaseService.ObtenerRegistrosMant();
                var pdf  = PdfService.GenerarReporteMantenimiento(prog, hist);
                PdfService.GuardarYAbrir(pdf,
                    "Mantenimiento_Moldes_" + DateTime.Today.ToString("yyyy-MM-dd") + ".pdf");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al generar PDF:\n" + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ══════════════════════════════════════════════════════════════════
        //  HELPERS UI
        // ══════════════════════════════════════════════════════════════════
        private Label Kpi(Panel p, string titulo, Color color, int idx)
        {
            int w = (p.Width - 12) / 6;
            var card = new Panel
            {
                Location = new Point(6 + idx * w, 4), Size = new Size(w - 4, 64),
                BackColor = color
            };
            card.Controls.Add(new Label
            {
                Text = titulo, Font = new Font("Segoe UI", 7.5f),
                ForeColor = Color.FromArgb(210, 230, 255),
                Location = new Point(7, 5), Size = new Size(w - 14, 16)
            });
            var lbl = new Label
            {
                Text = "—", Font = new Font("Segoe UI", 16f, FontStyle.Bold),
                ForeColor = Color.White, Location = new Point(7, 21),
                Size = new Size(w - 14, 34)
            };
            card.Controls.Add(lbl);
            p.Controls.Add(card);
            p.Resize += (s, e) =>
            {
                int cw = (p.Width - 12) / 6;
                card.SetBounds(6 + idx * cw, 4, cw - 4, 64);
                lbl.Width = cw - 14;
            };
            return lbl;
        }

        private DataGridView Grid(Color h)
        {
            var g = new DataGridView
            {
                Dock = DockStyle.Fill, ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false, AllowUserToAddRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White, BorderStyle = BorderStyle.None,
                RowHeadersVisible = false, Font = new Font("Segoe UI", 9f),
                RowTemplate = { Height = 24 }
            };
            g.ColumnHeadersDefaultCellStyle.BackColor = h;
            g.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            g.ColumnHeadersDefaultCellStyle.Font      = new Font("Segoe UI", 9f, FontStyle.Bold);
            g.EnableHeadersVisualStyles = false;
            g.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(238, 244, 250);
            return g;
        }

        private Button BtnT(string t, Color c, Point p, int w = 90)
        {
            var b = new Button
            {
                Text = t, Location = p, Size = new Size(w, 28), BackColor = c,
                ForeColor = Color.White, FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8f, FontStyle.Bold), Cursor = Cursors.Hand
            };
            b.FlatAppearance.BorderSize = 0;
            return b;
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  DIALOGO: AGREGAR / EDITAR PROGRAMA DE MANTENIMIENTO
    // ══════════════════════════════════════════════════════════════════════════
    public class DlgPrograma : Form
    {
        public ProgramaMant Resultado { get; private set; }

        public DlgPrograma(ProgramaMant p)
        {
            bool esNuevo = p == null;
            Text = esNuevo ? "Agregar molde al programa" : "Editar programa — " + p.CodigoMolde;
            Size = new Size(420, 320); StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = MinimizeBox = false;
            BackColor = Color.FromArgb(245, 248, 252);

            var banner = new Panel { Dock = DockStyle.Top, Height = 38,
                BackColor = Color.FromArgb(20, 80, 120) };
            banner.Controls.Add(new Label { Text = Text, ForeColor = Color.White,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold), AutoSize = true,
                Location = new Point(12, 9) });

            int y = 52;
            void L(string t, int yy) => Controls.Add(new Label
            {
                Text = t, Location = new Point(16, yy + 3), AutoSize = true,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            });

            L("Molde:", y);
            var cmbMolde = new ComboBox { Location = new Point(155, y), Size = new Size(235, 26),
                DropDownStyle = ComboBoxStyle.DropDownList, Enabled = esNuevo };
            foreach (var m in DatabaseService.ObtenerMoldes())
                cmbMolde.Items.Add(m.CodigoMold + " — " + m.Descripcion);
            if (!esNuevo) cmbMolde.Text = p.CodigoMolde + " — " + p.DescMolde;
            else if (cmbMolde.Items.Count > 0) cmbMolde.SelectedIndex = 0;
            Controls.Add(cmbMolde); y += 36;

            L("Tipo mantenimiento:", y);
            var cmbTipo = new ComboBox { Location = new Point(155, y), Size = new Size(235, 26),
                DropDownStyle = ComboBoxStyle.DropDownList };
            cmbTipo.Items.AddRange(new object[] { "Preventivo", "Predictivo", "Correctivo" });
            cmbTipo.SelectedItem = p?.TipoMant ?? "Preventivo";
            Controls.Add(cmbTipo); y += 36;

            L("Frecuencia (dias):", y);
            var numFrec = new NumericUpDown { Location = new Point(155, y), Size = new Size(235, 26),
                Minimum = 1, Maximum = 365, Value = p?.FrecuenciaDias ?? 90 };
            Controls.Add(numFrec); y += 36;

            L("Ultimo mantenimiento:", y);
            var dtp = new DateTimePicker { Location = new Point(155, y), Size = new Size(235, 26),
                Format = DateTimePickerFormat.Short,
                Value = p?.UltimoMant ?? DateTime.Today };
            Controls.Add(dtp); y += 36;

            var chkActivo = new CheckBox { Text = "Activo", Location = new Point(155, y),
                AutoSize = true, Checked = p?.Activo ?? true };
            Controls.Add(chkActivo); y += 44;

            Controls.Add(banner);
            var bOk = new Button { Text = "Guardar", Location = new Point(90, y), Size = new Size(120, 33),
                BackColor = Color.FromArgb(20, 80, 120), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            bOk.FlatAppearance.BorderSize = 0;
            var bCan = new Button { Text = "Cancelar", DialogResult = DialogResult.Cancel,
                Location = new Point(225, y), Size = new Size(100, 33),
                BackColor = Color.FromArgb(120, 120, 130), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat };
            bCan.FlatAppearance.BorderSize = 0;

            bOk.Click += (s, e) =>
            {
                if (cmbMolde.SelectedItem == null)
                { MessageBox.Show("Selecciona un molde.", "Validacion", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

                string sel  = cmbMolde.SelectedItem.ToString();
                string cod  = sel.Split('—')[0].Trim();
                string desc = sel.Contains("—") ? sel.Split('—')[1].Trim() : "";

                Resultado = p ?? new ProgramaMant();
                Resultado.CodigoMolde    = cod;
                Resultado.DescMolde      = desc;
                Resultado.TipoMant       = cmbTipo.SelectedItem?.ToString() ?? "Preventivo";
                Resultado.FrecuenciaDias = (int)numFrec.Value;
                Resultado.UltimoMant     = dtp.Value;
                Resultado.Activo         = chkActivo.Checked;
                DialogResult = DialogResult.OK;
            };

            Controls.AddRange(new Control[] { bOk, bCan });
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  DIALOGO: REGISTRAR MANTENIMIENTO REALIZADO
    // ══════════════════════════════════════════════════════════════════════════
    public class DlgRegistroMant : Form
    {
        public RegistroMantenimiento Resultado { get; private set; }

        public DlgRegistroMant(string codigoPre = null)
        {
            Text = "Registrar Mantenimiento Realizado";
            Size = new Size(460, 500); StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = MinimizeBox = false;
            BackColor = Color.FromArgb(245, 248, 252);

            var banner = new Panel { Dock = DockStyle.Top, Height = 38,
                BackColor = Color.FromArgb(20, 80, 120) };
            banner.Controls.Add(new Label { Text = Text, ForeColor = Color.White,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold), AutoSize = true,
                Location = new Point(12, 9) });
            Controls.Add(banner);

            int y = 52;
            void L(string t, int yy) => Controls.Add(new Label
            {
                Text = t, Location = new Point(16, yy + 3), AutoSize = true,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            });
            TextBox T(int yy, string v = "") { var tb = new TextBox
                { Text = v, Location = new Point(175, yy), Size = new Size(255, 24) };
                Controls.Add(tb); return tb; }

            L("Molde:", y);
            var cmbMolde = new ComboBox { Location = new Point(175, y), Size = new Size(255, 26),
                DropDownStyle = ComboBoxStyle.DropDownList };
            foreach (var m in DatabaseService.ObtenerMoldes())
                cmbMolde.Items.Add(m.CodigoMold + " — " + m.Descripcion);
            if (codigoPre != null)
            {
                for (int i = 0; i < cmbMolde.Items.Count; i++)
                    if (cmbMolde.Items[i].ToString().StartsWith(codigoPre))
                    { cmbMolde.SelectedIndex = i; break; }
            }
            else if (cmbMolde.Items.Count > 0) cmbMolde.SelectedIndex = 0;
            Controls.Add(cmbMolde); y += 36;

            L("Fecha realizada:", y);
            var dtpFecha = new DateTimePicker { Location = new Point(175, y), Size = new Size(255, 24),
                Format = DateTimePickerFormat.Short, Value = DateTime.Today };
            Controls.Add(dtpFecha); y += 36;

            L("Tipo:", y);
            var cmbTipo = new ComboBox { Location = new Point(175, y), Size = new Size(255, 24),
                DropDownStyle = ComboBoxStyle.DropDownList };
            cmbTipo.Items.AddRange(new object[] { "Preventivo", "Correctivo", "Predictivo" });
            cmbTipo.SelectedIndex = 0;
            Controls.Add(cmbTipo); y += 36;

            L("Tecnico:", y);         var txtTec  = T(y); y += 36;
            L("Descripcion:", y);     var txtDesc = T(y); y += 36;

            L("Costo ($):", y);
            var numCosto = new NumericUpDown { Location = new Point(175, y), Size = new Size(255, 24),
                Maximum = 999999, DecimalPlaces = 2 };
            Controls.Add(numCosto); y += 36;

            L("Duracion (horas):", y);
            var numHoras = new NumericUpDown { Location = new Point(175, y), Size = new Size(255, 24),
                Maximum = 9999, DecimalPlaces = 0, Minimum = 0 };
            Controls.Add(numHoras); y += 36;

            L("Proximo mant.:", y);
            var dtpProximo = new DateTimePicker { Location = new Point(175, y), Size = new Size(255, 24),
                Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddDays(90) };
            Controls.Add(dtpProximo); y += 36;

            L("Resultado:", y);
            var cmbRes = new ComboBox { Location = new Point(175, y), Size = new Size(255, 24),
                DropDownStyle = ComboBoxStyle.DropDownList };
            cmbRes.Items.AddRange(new object[] { "Completado", "Pendiente", "Cancelado" });
            cmbRes.SelectedIndex = 0;
            Controls.Add(cmbRes); y += 44;

            var bOk = new Button { Text = "Guardar mantenimiento", Location = new Point(60, y),
                Size = new Size(190, 33), BackColor = Color.FromArgb(20, 80, 120),
                ForeColor = Color.White, FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            bOk.FlatAppearance.BorderSize = 0;
            var bCan = new Button { Text = "Cancelar", DialogResult = DialogResult.Cancel,
                Location = new Point(268, y), Size = new Size(110, 33),
                BackColor = Color.FromArgb(120, 120, 130), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat };
            bCan.FlatAppearance.BorderSize = 0;

            bOk.Click += (s, e) =>
            {
                if (cmbMolde.SelectedItem == null)
                { MessageBox.Show("Selecciona un molde.", "Validacion", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

                string sel = cmbMolde.SelectedItem.ToString();
                Resultado = new RegistroMantenimiento
                {
                    CodigoMolde    = sel.Split('—')[0].Trim(),
                    FechaRealizada = dtpFecha.Value,
                    TipoMant       = cmbTipo.SelectedItem?.ToString() ?? "Preventivo",
                    Tecnico        = txtTec.Text.Trim(),
                    Descripcion    = txtDesc.Text.Trim(),
                    CostoMant      = numCosto.Value,
                    DuracionHoras  = (int)numHoras.Value,
                    Resultado      = cmbRes.SelectedItem?.ToString() ?? "Completado",
                    ProximoMant    = dtpProximo.Value
                };
                DialogResult = DialogResult.OK;
            };

            Controls.AddRange(new Control[] { bOk, bCan });
        }
    }
}
