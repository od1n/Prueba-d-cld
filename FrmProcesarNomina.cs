using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace SistemaGestion
{
    // ══════════════════════════════════════════════════════════════════════════
    //  PROCESAR NOMINA SEMANAL
    // ══════════════════════════════════════════════════════════════════════════
    public class FrmProcesarNomina : FormBase
    {
        private DataGridView   dgv;
        private DataGridView   dgvAportes;
        private Label          lblPeriodo, lblTotales, lblResAportes;
        private ComboBox       cmbPeriodo;
        private TabControl     tabMain;
        private PeriodoNomina  _periodoActual;
        private List<DetalleNomina>  _detalles = new();
        private List<AportePatronal> _aportes  = new();

        public FrmProcesarNomina()
        {
            Text = "Procesar Nomina Semanal";
            Size = new Size(1280, 660);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.FromArgb(240, 244, 250);

            BuildUI();
            CargarPeriodos();
        }

        private void BuildUI()
        {
            // ── Header ────────────────────────────────────────────────────
            var hdr = new Panel { Dock = DockStyle.Top, Height = 50,
                BackColor = Color.FromArgb(26, 74, 138) };
            hdr.Controls.Add(new Label
            {
                Text = "Nomina Semanal — USD", ForeColor = Color.White,
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                AutoSize = true, Location = new Point(14, 13)
            });
            var btnNuevoPeriodo = Btn("+ Nuevo periodo", Color.FromArgb(40, 170, 80),  130);
            var btnCerrarPer    = Btn("Cerrar periodo",  Color.FromArgb(160, 100, 20), 110);
            var btnRecibo       = Btn("Recibo PDF",      Color.FromArgb(180, 50, 30),  100);
            var btnPlanilla     = Btn("Plan. IVSS",      Color.FromArgb(40, 80, 160),   90);
            var btnAportePdf    = Btn("Aportes PDF",     Color.FromArgb(100, 40, 150),  90);

            var pnlHdrRight = new FlowLayoutPanel {
                Dock = DockStyle.Right, AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false, BackColor = Color.Transparent,
                Padding = new Padding(0, 9, 8, 0)
            };
            var _btnAyN = new Button {
                Text = "? Ayuda", Size = new Size(80, 30),
                BackColor = Color.FromArgb(80, 50, 150), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Cursor = Cursors.Hand, Margin = new Padding(0, 0, 4, 0)
            };
            _btnAyN.FlatAppearance.BorderSize = 0;
            _btnAyN.Click += (s, e) => AyudaService.Mostrar(this, "FrmProcesarNomina");
            pnlHdrRight.Controls.AddRange(new Control[]
                { btnNuevoPeriodo, btnCerrarPer, btnRecibo, btnPlanilla, btnAportePdf, _btnAyN });
            hdr.Controls.Add(pnlHdrRight);

            var _tipN = AyudaService.CrearTooltip();
            AyudaService.Tip(_tipN, _btnAyN,       "Abrir guía de ayuda de nómina");
            AyudaService.Tip(_tipN, btnNuevoPeriodo,"Crear un nuevo período de nómina para la semana actual");
            AyudaService.Tip(_tipN, btnCerrarPer,   "Cerrar el período — los montos quedan definitivos");
            AyudaService.Tip(_tipN, btnRecibo,       "Generar recibo de pago PDF del empleado seleccionado");
            AyudaService.Tip(_tipN, btnPlanilla,     "Generar planilla IVSS del período");
            AyudaService.Tip(_tipN, btnAportePdf,    "Generar resumen de aportes patronales en PDF");

            // ── Barra de periodo ──────────────────────────────────────────
            var pnlPer = new Panel { Dock = DockStyle.Top, Height = 44,
                BackColor = Color.FromArgb(225, 240, 228) };
            pnlPer.Controls.Add(new Label { Text = "Periodo:", Location = new Point(10, 13),
                AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Bold) });
            cmbPeriodo = new ComboBox { Location = new Point(75, 10), Size = new Size(280, 26),
                DropDownStyle = ComboBoxStyle.DropDownList };
            cmbPeriodo.SelectedIndexChanged += (s, e) => CargarDetallePeriodo();
            lblPeriodo = new Label { Location = new Point(370, 13), AutoSize = true,
                Font = new Font("Segoe UI", 9f, FontStyle.Italic),
                ForeColor = Color.FromArgb(60, 100, 70) };
            pnlPer.Controls.AddRange(new Control[] { cmbPeriodo, lblPeriodo });

            // ── TabControl ────────────────────────────────────────────────
            tabMain = new TabControl { Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9.5f) };

            // Tab 1: Nomina detallada
            var tabNomina = new TabPage("Nomina detallada");
            dgv = MkGrid(Color.FromArgb(26, 74, 138));
            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(235, 248, 237);
            dgv.CellDoubleClick += (s, e) => EditarLinea();

            var footerNomina = new Panel { Dock = DockStyle.Bottom, Height = 40,
                BackColor = Color.FromArgb(215, 238, 220) };
            lblTotales = new Label { AutoSize = true, Location = new Point(12, 10),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(20, 80, 40) };
            footerNomina.Controls.Add(lblTotales);
            tabNomina.Controls.Add(dgv);
            tabNomina.Controls.Add(footerNomina);

            // Tab 2: Aportes patronales
            var tabAportes = new TabPage("Aportes Patronales");

            // Sub-toolbar de aportes
            var pnlAportesBar = new Panel { Dock = DockStyle.Top, Height = 48,
                BackColor = Color.FromArgb(245, 238, 255) };

            pnlAportesBar.Controls.Add(new Label
            {
                Text = "IVSS patron %:",
                Location = new Point(10, 15), AutoSize = true,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            });
            var numPctIVSS = new NumericUpDown
            {
                Location = new Point(110, 12), Size = new Size(70, 26),
                Minimum = 9, Maximum = 11, DecimalPlaces = 1, Value = 9.0m
            };
            pnlAportesBar.Controls.Add(numPctIVSS);

            pnlAportesBar.Controls.Add(new Label
            {
                Text = "Valor UT (Bs):",
                Location = new Point(200, 15), AutoSize = true,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            });
            var numUT = new NumericUpDown
            {
                Location = new Point(295, 12), Size = new Size(120, 26),
                Maximum = 99999999, DecimalPlaces = 2, Minimum = 0
            };
            pnlAportesBar.Controls.Add(numUT);

            var btnCalcAportes = new Button
            {
                Text = "Calcular aportes", Location = new Point(430, 10),
                Size = new Size(150, 28),
                BackColor = Color.FromArgb(100, 40, 150), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };
            btnCalcAportes.FlatAppearance.BorderSize = 0;
            pnlAportesBar.Controls.Add(btnCalcAportes);

            // Nota informativa
            pnlAportesBar.Controls.Add(new Label
            {
                Text = "IVSS patron 9-11%  |  RPVH/FAOV 2%  |  INCES 2%  |  CESTA 0.5 UT x dia laboral",
                Location = new Point(600, 15), AutoSize = true,
                Font = new Font("Segoe UI", 8f, FontStyle.Italic),
                ForeColor = Color.FromArgb(100, 50, 150)
            });

            // Grid aportes
            dgvAportes = MkGrid(Color.FromArgb(100, 40, 150));
            dgvAportes.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 242, 255);

            // Footer resumen aportes
            var footerAportes = new Panel { Dock = DockStyle.Bottom, Height = 50,
                BackColor = Color.FromArgb(235, 225, 255) };
            lblResAportes = new Label { AutoSize = true, Location = new Point(12, 10),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(70, 30, 130) };
            footerAportes.Controls.Add(lblResAportes);

            tabAportes.Controls.Add(dgvAportes);
            tabAportes.Controls.Add(footerAportes);
            tabAportes.Controls.Add(pnlAportesBar);

            tabMain.TabPages.Add(tabNomina);
            tabMain.TabPages.Add(tabAportes);

            // ── Footer global ─────────────────────────────────────────────
            var footer = new Panel { Dock = DockStyle.Bottom, Height = 36,
                BackColor = Color.FromArgb(200, 220, 200) };
            var btnCerrar = new Button { Text = "Cerrar", Size = new Size(90, 26),
                Location = new Point(1170, 5), BackColor = Color.FromArgb(100, 100, 120),
                ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnCerrar.FlatAppearance.BorderSize = 0;
            btnCerrar.Click += (s, e) => Close();
            footer.Controls.Add(btnCerrar);

            Controls.AddRange(new Control[] { tabMain, pnlPer, hdr, footer });

            // ── Eventos ───────────────────────────────────────────────────
            btnNuevoPeriodo.Click += (s, e) => NuevoPeriodo();
            btnCerrarPer.Click   += (s, e) => CerrarPeriodo();
            btnRecibo.Click      += (s, e) => GenerarRecibos();
            btnPlanilla.Click    += (s, e) => GenerarPlanillaIVSS();
            btnAportePdf.Click   += (s, e) => GenerarPdfAportes();

            btnCalcAportes.Click += (s, e) =>
            {
                if (_periodoActual == null) return;
                _aportes = DatabaseService.CalcularAportesPatronales(
                    _periodoActual.Id,
                    numPctIVSS.Value,
                    numUT.Value);
                DatabaseService.GuardarAportesPatronales(_aportes);
                BindGridAportes();
            };

            // Cargar aportes existentes cuando se cambia de tab
            tabMain.SelectedIndexChanged += (s, e) =>
            {
                if (tabMain.SelectedIndex == 1 && _periodoActual != null)
                {
                    _aportes = DatabaseService.ObtenerAportesPatronales(_periodoActual.Id);
                    BindGridAportes();
                }
            };

            VentanaPrefs.Restaurar(this);
            var _tp = new System.Windows.Forms.Timer { Interval = 800 };
            _tp.Tick        += (s, e) => { _tp.Stop(); VentanaPrefs.Guardar(this); };
            SizeChanged     += (s, e) => { _tp.Stop(); _tp.Start(); };
            LocationChanged += (s, e) => { _tp.Stop(); _tp.Start(); };
            FormClosed      += (s, e) => { _tp.Stop(); _tp.Dispose(); };
        }

        private DataGridView MkGrid(Color h)
        {
            var g = new DataGridView
            {
                Dock = DockStyle.Fill, ReadOnly = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false, AllowUserToAddRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White, BorderStyle = BorderStyle.None,
                RowHeadersVisible = false, Font = new Font("Segoe UI", 9f),
                RowTemplate = { Height = 24 }
            };
            g.ColumnHeadersDefaultCellStyle.BackColor = h;
            g.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            g.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            g.EnableHeadersVisualStyles = false;
            return g;
        }

        private void CargarPeriodos()
        {
            var periodos = DatabaseService.ObtenerPeriodos();
            cmbPeriodo.Items.Clear();
            foreach (var p in periodos)
                cmbPeriodo.Items.Add(new PeriodoItem(p));
            if (cmbPeriodo.Items.Count > 0)
                cmbPeriodo.SelectedIndex = 0;
        }

        private void CargarDetallePeriodo()
        {
            if (cmbPeriodo.SelectedItem is not PeriodoItem pi) return;
            _periodoActual = pi.Periodo;

            lblPeriodo.Text =
                "Tasa BCV: Bs " + _periodoActual.TasaBCV.ToString("N2") +
                "/USD   |   Estado: " + _periodoActual.Estado;

            _detalles = DatabaseService.ObtenerDetallesNomina(_periodoActual.Id);

            // Si el periodo esta abierto y no tiene detalle, pre-cargar empleados
            if (_detalles.Count == 0 && _periodoActual.Estado == "Abierto")
                PrecargarEmpleados();

            BindGrid();
            ActualizarTotales();
        }

        private void PrecargarEmpleados()
        {
            var empleados = DatabaseService.ObtenerEmpleados(soloActivos: true);
            int diasSemana = (_periodoActual.FechaHasta - _periodoActual.FechaDesde).Days + 1;
            // Dias habiles por defecto = 5 (lunes a viernes)
            decimal diasHab = Math.Min(5, diasSemana);

            foreach (var emp in empleados)
            {
                var cuotas = DatabaseService.ObtenerPrestamos(emp.Id)
                    .Where(p => p.Estado == "Activo")
                    .Sum(p => p.CuotaSemanalUSD);

                var det = DatabaseService.CalcularDetalle(
                    emp, _periodoActual, diasHab, 0, 0, 0, cuotas);
                DatabaseService.GuardarDetalleNomina(det);
                _detalles.Add(det);
            }
        }

        private void BindGrid()
        {
            bool editable = _periodoActual?.Estado == "Abierto";

            dgv.DataSource = null;
            dgv.Columns.Clear();

            // Columnas configuradas manualmente para control de edicion
            void Col(string name, string header, bool ro = true, int w = -1)
            {
                var c = new DataGridViewTextBoxColumn
                    { Name = name, HeaderText = header, ReadOnly = ro };
                if (w > 0) c.Width = w;
                dgv.Columns.Add(c);
            }

            Col("IdEmpleado",   "Id",        true,  40);
            Col("Nombre",       "Empleado",  true);
            Col("Tipo",         "Tipo",      true,  80);
            Col("Dias",         "Dias trab", !editable, 70);
            Col("Faltas",       "Faltas",    !editable, 60);
            Col("Piezas",       "Piezas",    !editable, 70);
            Col("Ventas",       "Ventas",    !editable, 60);
            Col("DevSueldo",    "Dev.Sueldo",true,  90);
            Col("DevProd",      "Dev.Prod",  true,  80);
            Col("TotalDev",     "TOTAL DEV", true,  90);
            Col("IVSS",         "IVSS 4%",   true,  70);
            Col("RPVH",         "RPVH 1%",   true,  60);
            Col("Faltas$",      "Desc.Falta",true,  80);
            Col("Prestamo",     "Prestamo",  true,  75);
            Col("TotalDed",     "TOTAL DED", true,  85);
            Col("NetoUSD",      "NETO USD",  true,  90);
            Col("NetoBs",       "NETO Bs",   true,  90);

            foreach (var d in _detalles)
            {
                int idx = dgv.Rows.Add();
                var row = dgv.Rows[idx];
                row.Cells["IdEmpleado"].Value = d.IdEmpleado;
                row.Cells["Nombre"].Value     = d.NombreEmpleado;
                row.Cells["Tipo"].Value       = d.TipoEmpleado;
                row.Cells["Dias"].Value       = d.DiasTrabajados.ToString("N1");
                row.Cells["Faltas"].Value     = d.DiasInasistencia.ToString("N1");
                row.Cells["Piezas"].Value     = d.PiezasProducidas.ToString("N0");
                row.Cells["Ventas"].Value     = d.VentasCerradas.ToString("N0");
                row.Cells["DevSueldo"].Value  = d.DevengadoSueldo.ToString("C2");
                row.Cells["DevProd"].Value    = (d.DevengadoPiezas + d.DevengadoVentas).ToString("C2");
                row.Cells["TotalDev"].Value   = d.TotalDevengado.ToString("C2");
                row.Cells["IVSS"].Value       = d.DeduccionIVSS.ToString("C2");
                row.Cells["RPVH"].Value       = d.DeduccionRPVH.ToString("C2");
                row.Cells["Faltas$"].Value    = d.DescuentoFaltas.ToString("C2");
                row.Cells["Prestamo"].Value   = d.CuotaPrestamo.ToString("C2");
                row.Cells["TotalDed"].Value   = d.TotalDeducciones.ToString("C2");
                row.Cells["NetoUSD"].Value    = d.NetoUSD.ToString("C2");
                row.Cells["NetoBs"].Value     = d.NetoBs.ToString("N2");

                // Color por tipo
                row.DefaultCellStyle.BackColor =
                    d.TipoEmpleado == "Produccion" ? Color.FromArgb(240, 255, 240) :
                    d.TipoEmpleado == "Oficina"    ? Color.FromArgb(240, 245, 255) :
                    Color.White;
            }

            // Resaltar columnas de neto
            if (dgv.Columns.Contains("NetoUSD"))
                dgv.Columns["NetoUSD"].DefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
        }

        private void ActualizarTotales()
        {
            if (_detalles.Count == 0) { lblTotales.Text = "  Sin datos"; return; }
            decimal totalDev = _detalles.Sum(d => d.TotalDevengado);
            decimal totalDed = _detalles.Sum(d => d.TotalDeducciones);
            decimal totalNet = _detalles.Sum(d => d.NetoUSD);
            decimal tasa     = _periodoActual?.TasaBCV ?? 1;

            lblTotales.Text =
                "  Empleados: " + _detalles.Count +
                "   |   Total devengado: $" + totalDev.ToString("N2") +
                "   |   Total deducciones: $" + totalDed.ToString("N2") +
                "   |   NETO A PAGAR: $" + totalNet.ToString("N2") +
                "   = Bs " + (totalNet * tasa).ToString("N2");
        }

        private void EditarLinea()
        {
            if (_periodoActual?.Estado != "Abierto") return;
            if (dgv.CurrentRow == null) return;

            int idEmp = (int)dgv.CurrentRow.Cells["IdEmpleado"].Value;
            var emp   = DatabaseService.ObtenerEmpleado(idEmp);
            var det   = _detalles.Find(d => d.IdEmpleado == idEmp);
            if (emp == null || det == null) return;

            using var dlg = new DlgDetalleNomina(emp, det, _periodoActual);
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                DatabaseService.GuardarDetalleNomina(dlg.Resultado);
                // Abonar a prestamos si tiene cuota
                if (dlg.Resultado.CuotaPrestamo > 0)
                {
                    var prestamos = DatabaseService.ObtenerPrestamos(idEmp)
                        .Where(p => p.Estado == "Activo").ToList();
                    decimal restante = dlg.Resultado.CuotaPrestamo;
                    foreach (var pr in prestamos)
                    {
                        decimal abono = Math.Min(restante, pr.CuotaSemanalUSD);
                        if (abono > 0) DatabaseService.AbonarPrestamo(pr.Id, abono);
                        restante -= abono;
                        if (restante <= 0) break;
                    }
                }
                CargarDetallePeriodo();
            }
        }

        private void NuevoPeriodo()
        {
            using var dlg = new DlgPeriodoNomina();
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                DatabaseService.InsertarPeriodo(dlg.Resultado);
                CargarPeriodos();
                cmbPeriodo.SelectedIndex = 0;
            }
        }

        private void CerrarPeriodo()
        {
            if (_periodoActual == null || _periodoActual.Estado != "Abierto") return;
            if (MessageBox.Show(
                "Cerrar el periodo " + _periodoActual.DescPeriodo + "?\n\n" +
                "Una vez cerrado no podras modificar los datos de nomina.",
                "Confirmar cierre", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                DatabaseService.CerrarPeriodo(_periodoActual.Id);
                CargarPeriodos();
            }
        }

        private void GenerarRecibos()
        {
            if (_periodoActual == null || _detalles.Count == 0) return;
            try
            {
                var pdf = PdfService.GenerarPlanillaNomina(_detalles, _periodoActual);
                PdfService.GuardarYAbrir(pdf, "Nomina_" +
                    _periodoActual.FechaDesde.ToString("yyyy-MM-dd") + ".pdf");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al generar PDF:\n" + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void GenerarPlanillaIVSS()
        {
            if (_periodoActual == null || _detalles.Count == 0) return;
            try
            {
                var pdf = PdfService.GenerarPlanillaIVSS(_detalles, _periodoActual);
                PdfService.GuardarYAbrir(pdf, "IVSS_" +
                    _periodoActual.FechaDesde.ToString("yyyy-MM-dd") + ".pdf");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al generar planilla IVSS:\n" + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BindGridAportes()
        {
            dgvAportes.DataSource = null;
            dgvAportes.Columns.Clear();

            void Col(string name, string hdr, int w = -1)
            {
                var c = new DataGridViewTextBoxColumn
                    { Name = name, HeaderText = hdr, ReadOnly = true };
                if (w > 0) c.Width = w;
                dgvAportes.Columns.Add(c);
            }

            Col("Empleado",    "Empleado");
            Col("SalBs",       "Salario Bs",     100);
            Col("PctIVSS",     "% IVSS",          55);
            Col("IVSSPat",     "IVSS Patron Bs",  110);
            Col("RPVHPat",     "RPVH/FAOV Bs",    110);
            Col("INCES",       "INCES Bs",        100);
            Col("Dias",        "Dias lab.",         70);
            Col("ValUT",       "Valor UT Bs",      90);
            Col("Cesta",       "CESTA Bs",        100);
            Col("TotalBs",     "TOTAL Bs",        110);
            Col("TotalUSD",    "TOTAL USD",       100);

            foreach (var a in _aportes)
            {
                int idx = dgvAportes.Rows.Add();
                var row = dgvAportes.Rows[idx];
                row.Cells["Empleado"].Value = a.NombreEmpleado;
                row.Cells["SalBs"].Value    = a.SalarioNormalBs.ToString("N2");
                row.Cells["PctIVSS"].Value  = a.PorcentajeIVSS.ToString("N1") + "%";
                row.Cells["IVSSPat"].Value  = a.IVSSPatronBs.ToString("N2");
                row.Cells["RPVHPat"].Value  = a.RPVHPatronBs.ToString("N2");
                row.Cells["INCES"].Value    = a.INCESBs.ToString("N2");
                row.Cells["Dias"].Value     = a.DiasLaborales.ToString("N1");
                row.Cells["ValUT"].Value    = a.ValorUTBs.ToString("N2");
                row.Cells["Cesta"].Value    = a.CestaBs.ToString("N2");

                // Resaltar columnas de totales
                row.Cells["TotalBs"].Value  = a.TotalAportesBs.ToString("N2");
                row.Cells["TotalBs"].Style.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
                row.Cells["TotalBs"].Style.ForeColor = Color.FromArgb(80, 30, 120);

                row.Cells["TotalUSD"].Value = a.TotalAportesUSD.ToString("C2");
                row.Cells["TotalUSD"].Style.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
                row.Cells["TotalUSD"].Style.ForeColor = Color.FromArgb(40, 80, 160);

                // Alternar color por tipo (CESTA en azul suave si hay UT configurado)
                if (a.CestaBs > 0)
                    row.DefaultCellStyle.BackColor = Color.FromArgb(245, 240, 255);
            }

            // Actualizar resumen
            if (_aportes.Count > 0 && _periodoActual != null)
            {
                var res = DatabaseService.ObtenerResumenAportes(_periodoActual.Id);
                lblResAportes.Text =
                    "  Empleados: " + res.NumEmpleados +
                    "   |   IVSS patron: Bs " + res.IVSSPatronBs.ToString("N2") +
                    "   |   RPVH: Bs " + res.RPVHPatronBs.ToString("N2") +
                    "   |   INCES: Bs " + res.INCESBs.ToString("N2") +
                    "   |   CESTA: Bs " + res.CestaBs.ToString("N2") +
                    "   |   TOTAL APORTES: Bs " + res.TotalAportesBs.ToString("N2") +
                    "   |   Costo total nomina: Bs " + res.CostoTotalBs.ToString("N2");
            }
            else
            {
                lblResAportes.Text = "  Presiona 'Calcular aportes' para generar los aportes patronales del periodo.";
            }
        }

        private void GenerarPdfAportes()
        {
            if (_periodoActual == null) return;
            _aportes = DatabaseService.ObtenerAportesPatronales(_periodoActual.Id);
            if (_aportes.Count == 0)
            {
                MessageBox.Show(
                    "No hay aportes calculados para este periodo." +
                    "Ve a la pestana 'Aportes Patronales' y presiona 'Calcular aportes' primero.",
                    "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            try
            {
                var res = DatabaseService.ObtenerResumenAportes(_periodoActual.Id);
                var pdf = PdfService.GenerarPlanillaAportesPatronales(_aportes, _periodoActual, res);
                PdfService.GuardarYAbrir(pdf,
                    "AportesPatronales_" + _periodoActual.FechaDesde.ToString("yyyy-MM-dd") + ".pdf");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al generar PDF:" + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private Button Btn(string t, Color c, Point p, int w = 90)
        {
            var b = new Button { Text = t, Size = new Size(w, 33), Location = p, BackColor = c,
                ForeColor = Color.White, FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8f, FontStyle.Bold), Cursor = Cursors.Hand };
            b.FlatAppearance.BorderSize = 0; return b;
        }
        private Button Btn(string t, Color c, int w = 90)
        {
            var b = new Button { Text = t, Size = new Size(w, 32), BackColor = c,
                ForeColor = Color.White, FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8f, FontStyle.Bold), Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 4, 0) };
            b.FlatAppearance.BorderSize = 0; return b;
        }

        private class PeriodoItem
        {
            public PeriodoNomina Periodo { get; }
            public PeriodoItem(PeriodoNomina p) { Periodo = p; }
            public override string ToString() =>
                Periodo.DescPeriodo + " [" + Periodo.Estado + "]";
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  DIALOGO: NUEVO PERIODO
    // ══════════════════════════════════════════════════════════════════════════
    public class DlgPeriodoNomina : Form
    {
        public PeriodoNomina Resultado { get; private set; }

        public DlgPeriodoNomina()
        {
            Text = "Nuevo Periodo de Nomina";
            Size = new Size(420, 300); StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = MinimizeBox = false;
            BackColor = Color.FromArgb(240, 244, 250);

            var banner = new Panel { Dock = DockStyle.Top, Height = 38,
                BackColor = Color.FromArgb(26, 74, 138) };
            banner.Controls.Add(new Label { Text = Text, ForeColor = Color.White,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold), AutoSize = true, Location = new Point(12, 9) });
            Controls.Add(banner);

            // Calcular semana automatica (lunes a domingo)
            var hoy   = DateTime.Today;
            var lunes = hoy.AddDays(-(int)hoy.DayOfWeek + (int)DayOfWeek.Monday);
            if (hoy.DayOfWeek == DayOfWeek.Sunday) lunes = hoy.AddDays(-6);
            var domingo = lunes.AddDays(6);

            int y = 55;
            void L(string t, int yy) => Controls.Add(new Label { Text = t,
                Location = new Point(16, yy + 3), AutoSize = true,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold) });

            L("Desde:", y);
            var dtpD = new DateTimePicker { Location = new Point(165, y), Size = new Size(220, 26),
                Format = DateTimePickerFormat.Short, Value = lunes };
            Controls.Add(dtpD); y += 36;

            L("Hasta:", y);
            var dtpH = new DateTimePicker { Location = new Point(165, y), Size = new Size(220, 26),
                Format = DateTimePickerFormat.Short, Value = domingo };
            Controls.Add(dtpH); y += 36;

            L("Tasa BCV (Bs/USD):", y);
            var numTasa = new NumericUpDown { Location = new Point(165, y), Size = new Size(220, 26),
                Maximum = 99999999, DecimalPlaces = 2, Minimum = 1, Value = 1 };
            Controls.Add(numTasa); y += 36;

            L("Observaciones:", y);
            var txtObs = new TextBox { Location = new Point(165, y), Size = new Size(220, 24) };
            Controls.Add(txtObs); y += 44;

            var bOk = new Button { Text = "Crear periodo", Location = new Point(80, y), Size = new Size(150, 33),
                BackColor = Color.FromArgb(26, 74, 138), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            bOk.FlatAppearance.BorderSize = 0;
            var bCan = new Button { Text = "Cancelar", DialogResult = DialogResult.Cancel,
                Location = new Point(248, y), Size = new Size(100, 33),
                BackColor = Color.FromArgb(120, 120, 130), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            bCan.FlatAppearance.BorderSize = 0;

            bOk.Click += (s, e) =>
            {
                if (numTasa.Value < 1)
                { MessageBox.Show("Ingresa la tasa BCV.", "Validacion", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

                // ── Validar que las fechas sean coherentes
                if (dtpH.Value < dtpD.Value)
                {
                    MessageBox.Show("La fecha 'Hasta' no puede ser anterior a 'Desde'.",
                        "Validacion", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    dtpH.Focus();
                    return;
                }

                // ── Validar solapamiento con periodos existentes
                var conflicto = DatabaseService.ValidarSolapamientoPeriodo(
                    dtpD.Value.Date, dtpH.Value.Date);

                if (conflicto != null)
                {
                    MessageBox.Show(
                        $"Las fechas se solapan con el periodo existente:\n\n" +
                        $"  {conflicto.DescPeriodo}  ({conflicto.Estado})\n\n" +
                        $"Ajusta las fechas para que no haya superposicion.",
                        "Solapamiento de periodos",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    dtpD.Focus();
                    return;
                }

                Resultado = new PeriodoNomina
                {
                    FechaDesde = dtpD.Value, FechaHasta = dtpH.Value,
                    TasaBCV = numTasa.Value, Estado = "Abierto",
                    Observaciones = txtObs.Text.Trim()
                };
                DialogResult = DialogResult.OK;
            };
            Controls.AddRange(new Control[] { bOk, bCan });
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  DIALOGO: EDITAR LINEA DE NOMINA
    // ══════════════════════════════════════════════════════════════════════════
    public class DlgDetalleNomina : Form
    {
        public DetalleNomina Resultado { get; private set; }

        private NumericUpDown numDias, numFaltas, numPiezas, numVentas, numOtrosIng, numPrestamo, numOtrasDed;
        private Label         lblDevSueldo, lblDevProd, lblIVSS, lblRPVH, lblFaltas, lblNeto, lblNetoBs;

        private readonly Empleado       _emp;
        private readonly PeriodoNomina  _per;
        private readonly DetalleNomina  _original;

        public DlgDetalleNomina(Empleado emp, DetalleNomina det, PeriodoNomina per)
        {
            _emp = emp; _per = per; _original = det;

            Text = "Nomina — " + emp.NombreCompleto;
            Size = new Size(560, 580); StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = MinimizeBox = false;
            BackColor = Color.FromArgb(240, 244, 250);

            var banner = new Panel { Dock = DockStyle.Top, Height = 50,
                BackColor = Color.FromArgb(26, 74, 138) };
            banner.Controls.Add(new Label { Text = emp.NombreCompleto, ForeColor = Color.White,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold), AutoSize = true, Location = new Point(12, 8) });
            banner.Controls.Add(new Label
            {
                Text = emp.TipoEmpleado + "  |  Sueldo/dia: $" + emp.SueldoDiarioUSD.ToString("N2") +
                    (emp.TipoEmpleado == "Produccion" ? "  |  Tarifa pieza: $" + emp.TarifaPiezaUSD.ToString("N4") : "") +
                    (emp.TipoEmpleado == "Oficina"    ? "  |  Tarifa venta: $" + emp.TarifaVentaUSD.ToString("N2")  : ""),
                ForeColor = Color.FromArgb(200, 230, 210), AutoSize = true, Location = new Point(12, 30),
                Font = new Font("Segoe UI", 8f)
            });
            Controls.Add(banner);

            int y = 60;
            void L(string t, int yy) => Controls.Add(new Label { Text = t,
                Location = new Point(16, yy + 3), AutoSize = true,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold) });
            NumericUpDown N(decimal v, decimal max = 9999, int dec = 1)
            {
                var n = new NumericUpDown { Location = new Point(210, y), Size = new Size(150, 26),
                    Maximum = max, DecimalPlaces = dec, Value = v };
                n.ValueChanged += (s, e) => Recalcular();
                Controls.Add(n); return n;
            }
            Label Lbl(string v, int col = 400)
            {
                var l = new Label { Text = v, Location = new Point(col, y + 3),
                    AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                    ForeColor = Color.FromArgb(40, 100, 60) };
                Controls.Add(l); return l;
            }

            L("Dias trabajados:", y); numDias   = N(det.DiasTrabajados, 7); lblDevSueldo = Lbl(""); y += 34;
            L("Dias inasistencia:", y); numFaltas = N(det.DiasInasistencia, 7); lblFaltas = Lbl(""); y += 34;

            if (emp.TipoEmpleado == "Produccion")
            { L("Piezas producidas:", y); numPiezas = N(det.PiezasProducidas, 99999, 0); lblDevProd = Lbl(""); y += 34; }
            else
            { numPiezas = new NumericUpDown(); lblDevProd = new Label(); }

            if (emp.TipoEmpleado == "Oficina")
            { L("Ventas cerradas:", y); numVentas = N(det.VentasCerradas, 9999, 0); lblDevProd = Lbl(""); y += 34; }
            else
            { numVentas = new NumericUpDown(); }

            L("Otros ingresos ($):", y); numOtrosIng = N(det.OtrosIngresos, 9999, 2); y += 34;

            // Deducciones calculadas
            var sepDed = new Label { Text = "Deducciones legales:",
                Location = new Point(16, y), AutoSize = true,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(140, 40, 40) };
            Controls.Add(sepDed); y += 28;

            L("IVSS 4%:", y);   lblIVSS = Lbl(""); y += 28;
            L("RPVH 1%:", y);   lblRPVH = Lbl(""); y += 28;

            L("Cuota prestamo ($):", y); numPrestamo = N(det.CuotaPrestamo, 9999, 2); y += 34;
            L("Otras deducc. ($):", y);  numOtrasDed = N(det.OtrasDeduccion, 9999, 2); y += 44;

            // Neto en negrita
            var lblN = new Label { Text = "NETO A PAGAR:", Location = new Point(16, y),
                AutoSize = true, Font = new Font("Segoe UI", 11f, FontStyle.Bold) };
            lblNeto  = new Label { Location = new Point(210, y), AutoSize = true,
                Font = new Font("Segoe UI", 13f, FontStyle.Bold), ForeColor = Color.FromArgb(20, 100, 40) };
            lblNetoBs= new Label { Location = new Point(360, y), AutoSize = true,
                Font = new Font("Segoe UI", 9f), ForeColor = Color.FromArgb(80, 80, 100) };
            Controls.AddRange(new Control[] { lblN, lblNeto, lblNetoBs }); y += 50;

            var bOk = new Button { Text = "Guardar", Location = new Point(120, y),
                Size = new Size(150, 33), BackColor = Color.FromArgb(26, 74, 138),
                ForeColor = Color.White, FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold) };
            bOk.FlatAppearance.BorderSize = 0;
            var bCan = new Button { Text = "Cancelar", DialogResult = DialogResult.Cancel,
                Location = new Point(288, y), Size = new Size(110, 33),
                BackColor = Color.FromArgb(120, 120, 130), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            bCan.FlatAppearance.BorderSize = 0;
            Controls.AddRange(new Control[] { bOk, bCan });

            bOk.Click += (s, e) => Guardar();
            Recalcular();
        }

        private void Recalcular()
        {
            var calc = DatabaseService.CalcularDetalle(
                _emp, _per,
                numDias?.Value ?? 0,
                numFaltas?.Value ?? 0,
                numPiezas?.Value ?? 0,
                numVentas?.Value ?? 0,
                numPrestamo?.Value ?? 0,
                numOtrosIng?.Value ?? 0,
                numOtrasDed?.Value ?? 0);

            if (lblDevSueldo != null) lblDevSueldo.Text = "= $" + calc.DevengadoSueldo.ToString("N2");
            if (lblFaltas    != null) lblFaltas.Text    = "- $" + calc.DescuentoFaltas.ToString("N2");
            if (lblDevProd   != null) lblDevProd.Text   = "= $" + (calc.DevengadoPiezas + calc.DevengadoVentas).ToString("N2");
            if (lblIVSS      != null) lblIVSS.Text      = "- $" + calc.DeduccionIVSS.ToString("N2");
            if (lblRPVH      != null) lblRPVH.Text      = "- $" + calc.DeduccionRPVH.ToString("N2");
            if (lblNeto      != null) lblNeto.Text      = "$" + calc.NetoUSD.ToString("N2");
            if (lblNetoBs    != null) lblNetoBs.Text    = "= Bs " + calc.NetoBs.ToString("N2");
        }

        private void Guardar()
        {
            Resultado = DatabaseService.CalcularDetalle(
                _emp, _per,
                numDias?.Value ?? 0,
                numFaltas?.Value ?? 0,
                numPiezas?.Value ?? 0,
                numVentas?.Value ?? 0,
                numPrestamo?.Value ?? 0,
                numOtrosIng?.Value ?? 0,
                numOtrasDed?.Value ?? 0);
            Resultado.Id = _original.Id;
            DialogResult = DialogResult.OK;
        }
    }
}
