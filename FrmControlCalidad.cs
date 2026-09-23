using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace SistemaGestion
{
    // ══════════════════════════════════════════════════════════════════════════
    //  CONTROL DE CALIDAD — HISTORIAL DE INSPECCIONES
    // ══════════════════════════════════════════════════════════════════════════
    public class FrmControlCalidad : FormBase
    {
        private DataGridView dgv;
        private Label        lblResumen;
        private Panel        pnlGrafica;
        private List<InspeccionCalidad> _lista = new();

        public FrmControlCalidad()
        {
            Text          = "Control de Calidad";
            Size          = new Size(1200, 640);
            StartPosition = FormStartPosition.CenterParent;
            BackColor     = Color.FromArgb(245, 248, 252);

            // ── Header ────────────────────────────────────────────────────
            var hdr = new Panel
            {
                Dock = DockStyle.Top, Height = 50,
                BackColor = Color.FromArgb(40, 80, 160)
            };
            hdr.Controls.Add(new Label
            {
                Text = "Control de Calidad — Inspecciones",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                AutoSize = true, Location = new Point(12, 13)
            });

            var btnNueva  = Btn("+ Nueva insp.",  Color.FromArgb(40, 160, 80));
            var btnVer    = Btn("Ver detalle",     Color.FromArgb(60, 110, 190));
            var btnElim   = Btn("Eliminar",        Color.FromArgb(180, 50, 50));
            var btnReporte= Btn("Reporte PDF",     Color.FromArgb(160, 60, 20));
            
            var _tpF2 = AyudaService.CrearTooltip();
            var _btnA2 = new Button {
                Text = "? Ayuda", Size = new Size(80, 32),
                BackColor = Color.FromArgb(80, 50, 150), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Cursor = Cursors.Hand, Margin = new Padding(0, 0, 4, 0)
            };
            _btnA2.FlatAppearance.BorderSize = 0;
            _btnA2.Click += (s, e) => AyudaService.Mostrar(this, "FrmControlCalidad");
            AyudaService.Tip(_tpF2, _btnA2, "Abrir guía de ayuda");
            AyudaService.Tip(_tpF2, btnNueva, "Registrar una nueva inspección de control de calidad");
            AyudaService.Tip(_tpF2, btnVer, "Ver el detalle completo de la inspección seleccionada");
            AyudaService.Tip(_tpF2, btnElim, "Eliminar la inspección seleccionada");
            AyudaService.Tip(_tpF2, btnReporte, "Generar reporte PDF de inspecciones");
            var _pnlHF = new FlowLayoutPanel {
                Dock = DockStyle.Right, AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false, BackColor = Color.Transparent,
                Padding = new Padding(0, 9, 8, 0)
            };
            _pnlHF.Controls.AddRange(new Control[] { _btnA2, btnNueva, btnVer, btnElim, btnReporte });
            hdr.Controls.Add(_pnlHF);

            // ── KPIs ──────────────────────────────────────────────────────
            var pnlKpis = new Panel
            {
                Dock = DockStyle.Top, Height = 70,
                BackColor = Color.FromArgb(235, 240, 255)
            };
            var lblTotalInsp  = Kpi(pnlKpis, "Inspecciones",     Color.FromArgb(40, 80, 160),   0);
            var lblPctAp      = Kpi(pnlKpis, "% Aprobado",       Color.FromArgb(30, 130, 60),   1);
            var lblPctRe      = Kpi(pnlKpis, "% Rechazado",      Color.FromArgb(180, 50, 50),   2);
            var lblCriticos   = Kpi(pnlKpis, "Defectos criticos",Color.FromArgb(160, 80, 20),   3);
            var lblTopDefecto = Kpi(pnlKpis, "Defecto frecuente",Color.FromArgb(80, 50, 150),   4);
            var lblNivel      = Kpi(pnlKpis, "Nivel calidad",    Color.FromArgb(20, 110, 130),  5);

            // ── Layout principal ──────────────────────────────────────────
            var pnlMain = new Panel { Dock = DockStyle.Fill };

            pnlGrafica = new Panel
            {
                Dock = DockStyle.Right, Width = 320,
                BackColor = Color.White
            };
            pnlGrafica.Paint += PaintGrafica;

            var sep = new Panel
            {
                Dock = DockStyle.Right, Width = 3,
                BackColor = Color.FromArgb(210, 215, 230)
            };

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
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(40, 80, 160);
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgv.ColumnHeadersDefaultCellStyle.Font      = new Font("Segoe UI", 9f, FontStyle.Bold);
            dgv.EnableHeadersVisualStyles = false;
            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(238, 242, 255);
            dgv.DataBindingComplete += (s, e) => ColorearFilas();
            dgv.SelectionChanged    += (s, e) => pnlGrafica.Invalidate();
            dgv.DoubleClick         += (s, e) => VerDetalle();

            pnlMain.Controls.Add(dgv);
            pnlMain.Controls.Add(sep);
            pnlMain.Controls.Add(pnlGrafica);

            // ── Footer ────────────────────────────────────────────────────
            var footer = new Panel
            {
                Dock = DockStyle.Bottom, Height = 38,
                BackColor = Color.FromArgb(218, 225, 248)
            };
            lblResumen = new Label
            {
                AutoSize = true, Location = new Point(12, 10),
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(40, 50, 120)
            };
            var btnCerrar = new Button
            {
                Text = "Cerrar", Size = new Size(90, 26), Location = new Point(1085, 6),
                BackColor = Color.FromArgb(100, 100, 120), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnCerrar.FlatAppearance.BorderSize = 0;
            btnCerrar.Click += (s, e) => Close();
            footer.Controls.AddRange(new Control[] { lblResumen, btnCerrar });

            Controls.AddRange(new Control[] { pnlMain, footer, pnlKpis, hdr });

            // ── Eventos ───────────────────────────────────────────────────
            btnNueva.Click   += (s, e) => NuevaInspeccion();
            btnVer.Click     += (s, e) => VerDetalle();
            btnElim.Click    += (s, e) => Eliminar();
            btnReporte.Click += (s, e) => ExportarPdf();

            
            VentanaPrefs.Restaurar(this);
            var _vpT = new System.Windows.Forms.Timer { Interval = 800 };
            _vpT.Tick        += (s, e) => { _vpT.Stop(); VentanaPrefs.Guardar(this); };
            SizeChanged     += (s, e) => { _vpT.Stop(); _vpT.Start(); };
            LocationChanged += (s, e) => { _vpT.Stop(); _vpT.Start(); };
            FormClosed      += (s, e) => { _vpT.Stop(); _vpT.Dispose(); };

            CargarDatos();

            // Actualizar KPIs una vez cargado
            var stats = DatabaseService.EstadisticasCalidad();
            lblTotalInsp.Text  = stats.TotalInsp.ToString();
            lblPctAp.Text      = stats.PctAprobado + "%";
            lblPctRe.Text      = stats.PctRechazado + "%";
            lblCriticos.Text   = stats.DefectosCriticos.ToString();
            lblTopDefecto.Text = stats.DefectoMasFrecuente.Length > 10
                ? stats.DefectoMasFrecuente.Substring(0, 10) : stats.DefectoMasFrecuente;
            decimal pctNivel   = stats.PctAprobado;
            lblNivel.Text      = pctNivel >= 95 ? "Excelente"
                               : pctNivel >= 85 ? "Bueno"
                               : pctNivel >= 70 ? "Aceptable" : "Deficiente";
        }

        // ── Carga ─────────────────────────────────────────────────────────
        private void CargarDatos()
        {
            _lista = DatabaseService.ObtenerInspecciones();
            dgv.DataSource = null;
            dgv.DataSource = _lista.Select(i => new
            {
                i.Id,
                i.Lote,
                Producto   = i.ProductoCod,
                Fecha      = i.FechaInspeccion.ToShortDateString(),
                i.Inspector,
                Total      = i.CantidadTotal.ToString("N0"),
                Aprobadas  = i.CantidadAprobada.ToString("N0"),
                Rechazadas = i.CantidadRechazada.ToString("N0"),
                Retrabajo  = i.CantidadRetrabajo.ToString("N0"),
                Pct_Ap     = i.PctAprobado + "%",
                Puntaje    = i.PuntajePromedio.ToString("F1") + "/5",
                i.Resultado
            }).ToList();

            int ap  = _lista.Count(i => i.Resultado == "Aprobado");
            int re  = _lista.Count(i => i.Resultado == "Rechazado");
            int ret = _lista.Count(i => i.Resultado == "Retrabajo");
            lblResumen.Text =
                "  Inspecciones: " + _lista.Count +
                "   |   Aprobadas: " + ap +
                "   |   Rechazadas: " + re +
                "   |   Retrabajo: " + ret;
        }

        private void ColorearFilas()
        {
            foreach (DataGridViewRow row in dgv.Rows)
            {
                var res = row.Cells["Resultado"].Value?.ToString();
                row.DefaultCellStyle.BackColor = res switch
                {
                    "Aprobado"   => Color.FromArgb(225, 255, 228),
                    "Rechazado"  => Color.FromArgb(255, 228, 228),
                    "Retrabajo"  => Color.FromArgb(255, 248, 215),
                    _            => Color.White
                };
            }
        }

        // ── Acciones ──────────────────────────────────────────────────────
        private void NuevaInspeccion()
        {
            // Pedir seleccionar el ingreso de produccion relacionado
            using var dlgSel = new DlgSelIngreso();
            if (dlgSel.ShowDialog() != DialogResult.OK) return;

            using var dlg = new DlgInspeccion(null, dlgSel.IngresoSeleccionado);
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                DatabaseService.InsertarInspeccion(dlg.Resultado);
                DatabaseService.GuardarDefectos(dlg.Resultado.Id, dlg.Defectos);
                DatabaseService.RegistrarAuditoria("INSPECCION_NUEVA", "ControlCalidad",
                    "Lote: " + dlg.Resultado.Lote + " — " + dlg.Resultado.Resultado);

                // Si fue rechazado, actualizar estado del ingreso
                if (dlg.Resultado.Resultado == "Rechazado")
                {
                    var ingreso = DatabaseService.ObtenerIngresos()
                        .Find(i => i.Id == dlg.Resultado.IdIngreso);
                    if (ingreso != null)
                    {
                        ingreso.Estado = "Rechazado";
                        DatabaseService.ActualizarIngreso(ingreso);
                    }
                }

                CargarDatos();
            }
        }

        private void VerDetalle()
        {
            var ins = GetSeleccionada();
            if (ins == null) return;
            var defectos = DatabaseService.ObtenerDefectos(ins.Id);
            using var dlg = new DlgInspeccion(ins, null, defectos, soloLectura: true);
            dlg.ShowDialog();
        }

        private void Eliminar()
        {
            var ins = GetSeleccionada();
            if (ins == null) return;
            if (MessageBox.Show(
                "Eliminar inspeccion del lote " + ins.Lote + "?",
                "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                DatabaseService.EliminarInspeccion(ins.Id);
                CargarDatos();
            }
        }

        private void ExportarPdf()
        {
            if (_lista.Count == 0)
            {
                MessageBox.Show("No hay inspecciones para exportar.", "Aviso",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            try
            {
                var pdf = PdfService.GenerarReporteCalidad(_lista);
                PdfService.GuardarYAbrir(pdf,
                    "Calidad_" + DateTime.Today.ToString("yyyy-MM-dd") + ".pdf");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al generar PDF:\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private InspeccionCalidad GetSeleccionada()
        {
            if (dgv.CurrentRow == null) return null;
            int id = (int)dgv.CurrentRow.Cells["Id"].Value;
            return _lista.Find(i => i.Id == id);
        }

        // ── Grafica radial de puntajes ────────────────────────────────────
        private void PaintGrafica(object sender, PaintEventArgs e)
        {
            var g   = e.Graphics;
            var pnl = (Panel)sender;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // Titulo
            g.FillRectangle(new SolidBrush(Color.FromArgb(40, 80, 160)), 0, 0, pnl.Width, 28);
            using var fntT = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            g.DrawString("Puntaje de inspeccion", fntT, Brushes.White, 10, 7);

            var ins = GetSeleccionada();
            if (ins == null)
            {
                g.DrawString("Selecciona una inspeccion", new Font("Segoe UI", 9f),
                    Brushes.Gray, new RectangleF(10, 50, pnl.Width - 20, 40),
                    new StringFormat { Alignment = StringAlignment.Center });
                return;
            }

            // Radar chart (4 dimensiones)
            int cx = pnl.Width / 2;
            int cy = pnl.Height / 2 + 20;
            int rMax = Math.Min(cx, cy) - 50;

            var nombres = new[] { "Dimensiones", "Apariencia", "Funcional", "Etiquetado" };
            var valores  = new[]
            {
                ins.PuntajeDimensiones / 5.0,
                ins.PuntajeApariencia  / 5.0,
                ins.PuntajeFuncional   / 5.0,
                ins.PuntajeEtiquetado  / 5.0
            };

            // Grid circles
            using var penGrid = new Pen(Color.FromArgb(220, 222, 235), 1f) { DashStyle = DashStyle.Dash };
            for (int r = 1; r <= 5; r++)
            {
                int rad = rMax * r / 5;
                g.DrawEllipse(penGrid, cx - rad, cy - rad, rad * 2, rad * 2);
            }

            // Ejes
            using var penEje = new Pen(Color.FromArgb(190, 195, 215), 1f);
            for (int i = 0; i < 4; i++)
            {
                double ang = Math.PI * 2 * i / 4 - Math.PI / 2;
                g.DrawLine(penEje, cx, cy,
                    cx + (int)(rMax * Math.Cos(ang)),
                    cy + (int)(rMax * Math.Sin(ang)));
            }

            // Area de valores
            var pts = new PointF[4];
            for (int i = 0; i < 4; i++)
            {
                double ang = Math.PI * 2 * i / 4 - Math.PI / 2;
                pts[i] = new PointF(
                    cx + (float)(rMax * valores[i] * Math.Cos(ang)),
                    cy + (float)(rMax * valores[i] * Math.Sin(ang)));
            }

            using var brArea = new SolidBrush(Color.FromArgb(60, 40, 80, 160));
            g.FillPolygon(brArea, pts);
            using var penArea = new Pen(Color.FromArgb(40, 80, 160), 2f);
            g.DrawPolygon(penArea, pts);

            // Puntos
            foreach (var pt in pts)
                g.FillEllipse(Brushes.RoyalBlue, pt.X - 4, pt.Y - 4, 8, 8);

            // Etiquetas de ejes
            using var fntL = new Font("Segoe UI", 7.5f, FontStyle.Bold);
            using var fntV = new Font("Segoe UI", 7f);
            var scores = new[] { ins.PuntajeDimensiones, ins.PuntajeApariencia,
                                 ins.PuntajeFuncional,   ins.PuntajeEtiquetado };
            for (int i = 0; i < 4; i++)
            {
                double ang = Math.PI * 2 * i / 4 - Math.PI / 2;
                float lx   = cx + (float)((rMax + 22) * Math.Cos(ang));
                float ly   = cy + (float)((rMax + 22) * Math.Sin(ang));
                g.DrawString(nombres[i], fntL, Brushes.DimGray,
                    new RectangleF(lx - 40, ly - 10, 80, 20),
                    new StringFormat { Alignment = StringAlignment.Center,
                                       LineAlignment = StringAlignment.Center });
                g.DrawString(scores[i] + "/5", fntV, Brushes.RoyalBlue,
                    new RectangleF(lx - 20, ly + 8, 40, 14),
                    new StringFormat { Alignment = StringAlignment.Center });
            }

            // Puntaje promedio central
            using var fntP = new Font("Segoe UI", 13f, FontStyle.Bold);
            g.DrawString(ins.PuntajePromedio.ToString("F1"),
                fntP, Brushes.RoyalBlue,
                new RectangleF(cx - 24, cy - 16, 48, 28),
                new StringFormat { Alignment = StringAlignment.Center,
                                   LineAlignment = StringAlignment.Center });
            g.DrawString("/ 5.0", fntV, Brushes.Gray,
                new RectangleF(cx - 20, cy + 12, 40, 14),
                new StringFormat { Alignment = StringAlignment.Center });

            // Resultado
            string res    = ins.Resultado;
            Color resCol  = res == "Aprobado"   ? Color.FromArgb(30, 140, 60)
                          : res == "Rechazado"  ? Color.FromArgb(180, 40, 40)
                          : Color.FromArgb(180, 130, 20);
            int ry        = pnl.Height - 55;
            g.FillRectangle(new SolidBrush(resCol), 20, ry, pnl.Width - 40, 26);
            using var fntR = new Font("Segoe UI", 10f, FontStyle.Bold);
            g.DrawString(res.ToUpper(), fntR, Brushes.White,
                new RectangleF(20, ry, pnl.Width - 40, 26),
                new StringFormat { Alignment = StringAlignment.Center,
                                   LineAlignment = StringAlignment.Center });

            // Cantidades
            using var fntC = new Font("Segoe UI", 8f);
            g.DrawString(
                "Aprobadas: " + ins.CantidadAprobada.ToString("N0") +
                "  Rechazadas: " + ins.CantidadRechazada.ToString("N0") +
                "  Retrabajo: " + ins.CantidadRetrabajo.ToString("N0"),
                fntC, Brushes.DimGray,
                new RectangleF(10, ry + 30, pnl.Width - 20, 18),
                new StringFormat { Alignment = StringAlignment.Center });
        }

        private Label Kpi(Panel p, string titulo, Color color, int idx)
        {
            int w = (p.Width - 12) / 6;
            var card = new Panel
            {
                Location = new Point(6 + idx * w, 4),
                Size     = new Size(w - 4, 62),
                BackColor= color
            };
            card.Controls.Add(new Label
            {
                Text = titulo, Font = new Font("Segoe UI", 7.5f),
                ForeColor = Color.FromArgb(200, 225, 255),
                Location = new Point(7, 5), Size = new Size(w - 14, 16)
            });
            var lbl = new Label
            {
                Text = "—", Font = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor = Color.White, Location = new Point(7, 21),
                Size = new Size(w - 14, 32)
            };
            card.Controls.Add(lbl);
            p.Controls.Add(card);
            p.Resize += (s, e) =>
            {
                int cw = (p.Width - 12) / 6;
                card.SetBounds(6 + idx * cw, 4, cw - 4, 62);
            };
            return lbl;
        }

        private Button Btn(string t, Color c, Point p, int w = 90)
        {
            var b = new Button
            {
                Text = t, Size = new Size(w, 33), Location = p, BackColor = c,
                ForeColor = Color.White, FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8f, FontStyle.Bold), Cursor = Cursors.Hand
            };
            b.FlatAppearance.BorderSize = 0;
            return b;
        }
        private Button Btn(string t, Color c, int w = 90)
        {
            var b = new Button
            {
                Text = t, Size = new Size(w, 33), BackColor = c,
                ForeColor = Color.White, FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8f, FontStyle.Bold), Cursor = Cursors.Hand
            };
            b.FlatAppearance.BorderSize = 0;
            return b;
        }

    }

    // ══════════════════════════════════════════════════════════════════════════
    //  SELECTOR DE INGRESO DE PRODUCCION
    // ══════════════════════════════════════════════════════════════════════════
    public class DlgSelIngreso : FormBase
    {
        public IngresoProduccion IngresoSeleccionado { get; private set; }
        private DataGridView dgv;

        public DlgSelIngreso()
        {
            Text = "Seleccionar ingreso de produccion";
            Size = new Size(700, 420); StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.Sizable; MaximizeBox = true; MinimizeBox = false;
            BackColor = Color.FromArgb(245, 250, 245);

            var hdr = new Panel { Dock = DockStyle.Top, Height = 38, BackColor = Color.FromArgb(60, 120, 40) };
            hdr.Controls.Add(new Label { Text = "Selecciona el lote a inspeccionar",
                ForeColor = Color.White, Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                AutoSize = true, Location = new Point(12, 9) });

            dgv = new DataGridView
            {
                Dock = DockStyle.Fill, ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false, AllowUserToAddRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White, BorderStyle = BorderStyle.None,
                RowHeadersVisible = false, Font = new Font("Segoe UI", 9f)
            };
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(60, 120, 40);
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgv.ColumnHeadersDefaultCellStyle.Font      = new Font("Segoe UI", 9f, FontStyle.Bold);
            dgv.EnableHeadersVisualStyles = false;
            dgv.DoubleClick += (s, e) => Seleccionar();

            var footer = new Panel { Dock = DockStyle.Bottom, Height = 44,
                BackColor = Color.FromArgb(218, 238, 218) };
            var btnSel = new Button { Text = "Seleccionar", Location = new Point(300, 8), Size = new Size(130, 28),
                BackColor = Color.FromArgb(40, 130, 50), ForeColor = Color.White, FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            btnSel.FlatAppearance.BorderSize = 0; btnSel.Click += (s, e) => Seleccionar();
            var btnCan = new Button { Text = "Cancelar", DialogResult = DialogResult.Cancel,
                Location = new Point(440, 8), Size = new Size(100, 28),
                BackColor = Color.FromArgb(120, 120, 130), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnCan.FlatAppearance.BorderSize = 0;
            footer.Controls.AddRange(new Control[] { btnSel, btnCan });

            Controls.AddRange(new Control[] { hdr, dgv, footer });

            var ingresos = DatabaseService.ObtenerIngresos();
            dgv.DataSource = ingresos.Select(i => new
            {
                i.Id, i.Producto, i.Lote,
                Cantidad = i.Cantidad.ToString("N0"),
                i.Unidad,
                Fecha = i.Fecha.ToShortDateString(),
                i.Estado, i.Responsable
            }).ToList();
        
            VentanaPrefs.Restaurar(this);
        }

        private void Seleccionar()
        {
            if (dgv.CurrentRow == null) return;
            int id = (int)dgv.CurrentRow.Cells["Id"].Value;
            IngresoSeleccionado = DatabaseService.ObtenerIngresos().Find(i => i.Id == id);
            if (IngresoSeleccionado != null) DialogResult = DialogResult.OK;
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  DIALOGO NUEVA / VER INSPECCION
    // ══════════════════════════════════════════════════════════════════════════
    public class DlgInspeccion : FormBase
    {
        public InspeccionCalidad    Resultado { get; private set; }
        public List<DefectoCalidad> Defectos  { get; private set; } = new();

        // Cabecera
        private TextBox       txtInspector, txtObs, txtAccion;
        private DateTimePicker dtpFecha;
        private NumericUpDown  numTotal, numAprobadas, numRechazadas, numRetrabajo;
        private ComboBox       cmbResultado;

        // Puntajes (TrackBar)
        private TrackBar trkDim, trkAp, trkFunc, trkEtiq;
        private Label    lblDim, lblAp, lblFunc, lblEtiq;

        // Defectos
        private DataGridView dgvDefectos;

        private readonly bool _soloLectura;

        public DlgInspeccion(InspeccionCalidad ins,
                              IngresoProduccion ingreso,
                              List<DefectoCalidad> defectos = null,
                              bool soloLectura = false)
        {
            _soloLectura = soloLectura;
            bool esNueva = ins == null;

            Text = esNueva ? "Nueva Inspeccion de Calidad"
                 : soloLectura ? "Detalle Inspeccion — Lote " + ins.Lote
                 : "Editar Inspeccion";
            Size = new Size(920, 660);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = true; MinimizeBox = false;
            BackColor = Color.FromArgb(245, 248, 252);

            // Banner
            var banner = new Panel { Dock = DockStyle.Top, Height = 40,
                BackColor = Color.FromArgb(26, 74, 138) };
            banner.Controls.Add(new Label { Text = Text, ForeColor = Color.White,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold), AutoSize = true,
                Location = new Point(12, 10) });

            // ── Panel izquierdo: datos generales ──────────────────────────
            var pL = new Panel { Location = new Point(0, 42), Size = new Size(380, 520),
                BackColor = Color.FromArgb(245, 248, 252) };
            int y = 10;

            // Info del ingreso
            if (ingreso != null)
            {
                var lblInfo = new Label
                {
                    Text = "Lote: " + ingreso.Lote + "  |  Producto: " + ingreso.Producto +
                           "\nCantidad: " + ingreso.Cantidad + " " + ingreso.Unidad,
                    Location = new Point(10, y), Size = new Size(360, 38),
                    Font = new Font("Segoe UI", 9f), BackColor = Color.FromArgb(220, 235, 255),
                    BorderStyle = BorderStyle.FixedSingle
                };
                pL.Controls.Add(lblInfo); y += 46;
            }

            void L2(string t, int yy) => pL.Controls.Add(new Label
            {
                Text = t, Location = new Point(10, yy + 3), AutoSize = true,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            });
            TextBox T2(int yy, string v = "") { var tb = new TextBox
                { Text = v, Location = new Point(155, yy), Size = new Size(215, 24),
                  ReadOnly = soloLectura }; pL.Controls.Add(tb); return tb; }
            NumericUpDown N2(int yy, decimal max, decimal val = 0)
            {
                var n = new NumericUpDown { Location = new Point(155, yy), Size = new Size(215, 24),
                    Maximum = max, DecimalPlaces = 0, Value = val, Enabled = !soloLectura };
                pL.Controls.Add(n); return n;
            }

            L2("Inspector:", y);      txtInspector = T2(y, ins?.Inspector ?? Sesion.NombreCompleto); y += 32;
            L2("Fecha:", y);
            dtpFecha = new DateTimePicker { Location = new Point(155, y), Size = new Size(215, 24),
                Format = DateTimePickerFormat.Short, Value = ins?.FechaInspeccion ?? DateTime.Today,
                Enabled = !soloLectura };
            pL.Controls.Add(dtpFecha); y += 32;

            L2("Total unidades:", y);    numTotal     = N2(y, 999999, ins?.CantidadTotal ?? (ingreso?.Cantidad ?? 0)); y += 32;
            L2("Aprobadas:", y);         numAprobadas = N2(y, 999999, ins?.CantidadAprobada ?? 0); y += 32;
            L2("Rechazadas:", y);        numRechazadas= N2(y, 999999, ins?.CantidadRechazada ?? 0); y += 32;
            L2("Retrabajo:", y);         numRetrabajo = N2(y, 999999, ins?.CantidadRetrabajo ?? 0); y += 32;

            L2("Resultado:", y);
            cmbResultado = new ComboBox { Location = new Point(155, y), Size = new Size(215, 24),
                DropDownStyle = ComboBoxStyle.DropDownList, Enabled = !soloLectura };
            cmbResultado.Items.AddRange(new object[] { "Aprobado", "Rechazado", "Retrabajo" });
            cmbResultado.SelectedItem = ins?.Resultado ?? "Aprobado";
            pL.Controls.Add(cmbResultado); y += 32;

            L2("Observaciones:", y);
            txtObs = new TextBox { Location = new Point(155, y), Size = new Size(215, 42),
                Multiline = true, Text = ins?.Observaciones ?? "", ReadOnly = soloLectura };
            pL.Controls.Add(txtObs); y += 50;

            L2("Accion correctiva:", y);
            txtAccion = new TextBox { Location = new Point(155, y), Size = new Size(215, 42),
                Multiline = true, Text = ins?.AccionCorrectiva ?? "", ReadOnly = soloLectura };
            pL.Controls.Add(txtAccion);

            // ── Panel central: puntajes ───────────────────────────────────
            var pC = new Panel { Location = new Point(384, 42), Size = new Size(240, 520),
                BackColor = Color.FromArgb(240, 243, 255) };

            pC.Controls.Add(new Label { Text = "Criterios de calidad (1-5)",
                Location = new Point(8, 8), AutoSize = true,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(40, 60, 140) });

            int py = 34;
            (trkDim,  lblDim)  = TrackPanel(pC, "Dimensiones",  ins?.PuntajeDimensiones ?? 3, py); py += 70;
            (trkAp,   lblAp)   = TrackPanel(pC, "Apariencia",   ins?.PuntajeApariencia  ?? 3, py); py += 70;
            (trkFunc, lblFunc) = TrackPanel(pC, "Funcional",     ins?.PuntajeFuncional   ?? 3, py); py += 70;
            (trkEtiq, lblEtiq) = TrackPanel(pC, "Etiquetado",   ins?.PuntajeEtiquetado  ?? 3, py);

            if (soloLectura)
            {
                trkDim.Enabled = trkAp.Enabled = trkFunc.Enabled = trkEtiq.Enabled = false;
            }

            // ── Panel derecho: defectos ───────────────────────────────────
            var pR = new Panel { Location = new Point(628, 42), Size = new Size(278, 520),
                BackColor = Color.FromArgb(255, 248, 242) };

            pR.Controls.Add(new Label { Text = "Registro de defectos",
                Location = new Point(8, 8), AutoSize = true,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(140, 60, 20) });

            var btnAddDef = new Button
            {
                Text = "+ Agregar defecto", Location = new Point(8, 30), Size = new Size(140, 26),
                BackColor = Color.FromArgb(180, 80, 20), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                Visible = !soloLectura
            };
            btnAddDef.FlatAppearance.BorderSize = 0;

            var btnQuitarDef = new Button
            {
                Text = "Quitar", Location = new Point(158, 30), Size = new Size(70, 26),
                BackColor = Color.FromArgb(130, 50, 50), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                Visible = !soloLectura
            };
            btnQuitarDef.FlatAppearance.BorderSize = 0;

            dgvDefectos = new DataGridView
            {
                Location = new Point(8, 64), Size = new Size(262, 440),
                ReadOnly = soloLectura, AllowUserToAddRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White, BorderStyle = BorderStyle.FixedSingle,
                RowHeadersVisible = false, Font = new Font("Segoe UI", 8.5f),
                RowTemplate = { Height = 22 }
            };
            dgvDefectos.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(180, 80, 20);
            dgvDefectos.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvDefectos.ColumnHeadersDefaultCellStyle.Font      = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            dgvDefectos.EnableHeadersVisualStyles = false;

            dgvDefectos.Columns.Add(new DataGridViewTextBoxColumn
                { Name = "TipoDefecto", HeaderText = "Tipo", ReadOnly = soloLectura });
            dgvDefectos.Columns.Add(new DataGridViewTextBoxColumn
                { Name = "Cantidad", HeaderText = "Cant", Width = 45 });
            var colSev = new DataGridViewComboBoxColumn
            {
                Name = "Severidad", HeaderText = "Severidad",
                Width = 75, ReadOnly = soloLectura
            };
            colSev.Items.AddRange("Menor", "Mayor", "Critico");
            dgvDefectos.Columns.Add(colSev);
            dgvDefectos.Columns.Add(new DataGridViewTextBoxColumn
                { Name = "Descripcion", HeaderText = "Descripcion" });

            // Cargar defectos existentes
            if (defectos != null)
                foreach (var d in defectos)
                {
                    int idx = dgvDefectos.Rows.Add();
                    dgvDefectos.Rows[idx].Cells["TipoDefecto"].Value = d.TipoDefecto;
                    dgvDefectos.Rows[idx].Cells["Cantidad"].Value    = d.Cantidad;
                    dgvDefectos.Rows[idx].Cells["Severidad"].Value   = d.Severidad;
                    dgvDefectos.Rows[idx].Cells["Descripcion"].Value = d.Descripcion;
                }

            btnAddDef.Click += (s, e) =>
            {
                int idx = dgvDefectos.Rows.Add();
                dgvDefectos.Rows[idx].Cells["TipoDefecto"].Value = "Nuevo defecto";
                dgvDefectos.Rows[idx].Cells["Cantidad"].Value    = 1;
                dgvDefectos.Rows[idx].Cells["Severidad"].Value   = "Menor";
                dgvDefectos.Rows[idx].Cells["Descripcion"].Value = "";
                dgvDefectos.CurrentCell = dgvDefectos.Rows[idx].Cells["TipoDefecto"];
                dgvDefectos.BeginEdit(true);
            };
            btnQuitarDef.Click += (s, e) =>
            {
                if (dgvDefectos.CurrentRow != null)
                    dgvDefectos.Rows.RemoveAt(dgvDefectos.CurrentRow.Index);
            };

            pR.Controls.AddRange(new Control[] { btnAddDef, btnQuitarDef, dgvDefectos });

            // ── Botones inferiores ────────────────────────────────────────
            var pBtns = new Panel { Dock = DockStyle.Bottom, Height = 50,
                BackColor = Color.FromArgb(220, 225, 245) };

            if (!soloLectura)
            {
                var bOk = new Button { Text = "Guardar inspeccion",
                    Location = new Point(280, 10), Size = new Size(180, 32),
                    BackColor = Color.FromArgb(30, 130, 70), ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10f, FontStyle.Bold) };
                bOk.FlatAppearance.BorderSize = 0;
                bOk.Click += (s, e) => Guardar(ins, ingreso);
                pBtns.Controls.Add(bOk);
            }

            var bCan = new Button { Text = soloLectura ? "Cerrar" : "Cancelar",
                DialogResult = DialogResult.Cancel,
                Location = new Point(480, 10), Size = new Size(110, 32),
                BackColor = Color.FromArgb(120, 120, 130), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat };
            bCan.FlatAppearance.BorderSize = 0;
            pBtns.Controls.Add(bCan);

            
            // Recordar posición y tamaño
            VentanaPrefs.Restaurar(this);
            Controls.AddRange(new Control[] { banner, pL, pC, pR, pBtns });
        
            VentanaPrefs.Restaurar(this);
        }

        private (TrackBar, Label) TrackPanel(Panel parent, string titulo, int val, int y)
        {
            parent.Controls.Add(new Label { Text = titulo,
                Location = new Point(10, y), AutoSize = true,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) });

            var lbl = new Label { Text = val + "/5",
                Location = new Point(190, y), Size = new Size(40, 18),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = val >= 4 ? Color.DarkGreen : val >= 3 ? Color.DarkGoldenrod : Color.DarkRed,
                TextAlign = ContentAlignment.MiddleRight };

            var trk = new TrackBar { Location = new Point(10, y + 20), Size = new Size(220, 38),
                Minimum = 1, Maximum = 5, Value = val, TickFrequency = 1,
                SmallChange = 1, LargeChange = 1 };

            trk.ValueChanged += (s, e) =>
            {
                lbl.Text = trk.Value + "/5";
                lbl.ForeColor = trk.Value >= 4 ? Color.DarkGreen
                              : trk.Value >= 3 ? Color.DarkGoldenrod : Color.DarkRed;
            };

            parent.Controls.Add(trk);
            parent.Controls.Add(lbl);
            return (trk, lbl);
        }

        private void Guardar(InspeccionCalidad original, IngresoProduccion ingreso)
        {
            Resultado = original ?? new InspeccionCalidad();
            Resultado.IdIngreso        = ingreso?.Id ?? original?.IdIngreso ?? 0;
            Resultado.Lote             = ingreso?.Lote ?? original?.Lote ?? "";
            Resultado.ProductoCod      = ingreso?.Producto ?? original?.ProductoCod ?? "";
            Resultado.FechaInspeccion  = dtpFecha.Value;
            Resultado.Inspector        = txtInspector.Text.Trim();
            Resultado.CantidadTotal    = numTotal.Value;
            Resultado.CantidadAprobada = numAprobadas.Value;
            Resultado.CantidadRechazada= numRechazadas.Value;
            Resultado.CantidadRetrabajo= numRetrabajo.Value;
            Resultado.PuntajeDimensiones = trkDim.Value;
            Resultado.PuntajeApariencia  = trkAp.Value;
            Resultado.PuntajeFuncional   = trkFunc.Value;
            Resultado.PuntajeEtiquetado  = trkEtiq.Value;
            Resultado.Resultado          = cmbResultado.SelectedItem?.ToString() ?? "Aprobado";
            Resultado.Observaciones      = txtObs.Text.Trim();
            Resultado.AccionCorrectiva   = txtAccion.Text.Trim();

            Defectos.Clear();
            foreach (DataGridViewRow row in dgvDefectos.Rows)
            {
                string tipo = row.Cells["TipoDefecto"].Value?.ToString() ?? "";
                if (string.IsNullOrWhiteSpace(tipo)) continue;
                int.TryParse(row.Cells["Cantidad"].Value?.ToString(), out int cant);
                Defectos.Add(new DefectoCalidad
                {
                    IdInspeccion = Resultado.Id,
                    TipoDefecto  = tipo,
                    Cantidad     = cant > 0 ? cant : 1,
                    Severidad    = row.Cells["Severidad"].Value?.ToString() ?? "Menor",
                    Descripcion  = row.Cells["Descripcion"].Value?.ToString() ?? ""
                });
            }

            DialogResult = DialogResult.OK;
        }
    }
}
