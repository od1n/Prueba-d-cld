using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using System.Windows.Forms;

namespace SistemaGestion
{
    // ════════════════════════════════════════════════════════════════════════
    //  FrmKardex — v2
    //  Dos pestañas: Kardex B (Productos Terminados) | Kardex A (Insumos)
    //  Costo promedio ponderado en entradas.
    //  Ayuda paso a paso integrada. Exportar CSV. Planilla imprimible.
    // ════════════════════════════════════════════════════════════════════════
    public class FrmKardex : FormBase
    {
        // Pestaña B — Productos
        private DataGridView dgvB;
        private ComboBox     cmbProducto;
        private DateTimePicker dtpDesdeB, dtpHastaB;
        private Label        lblResumenB;

        // Pestaña A — Insumos
        private DataGridView dgvA;
        private ComboBox     cmbInsumo;
        private DateTimePicker dtpDesdeA, dtpHastaA;
        private Label        lblResumenA;

        // Selector de vista
        private Panel        pnlContenido;
        private Panel        _tabB, _tabA;
        private bool         _mostrandoB = true;

        // ── Colores ──────────────────────────────────────────────────────────
        private static readonly Color C1    = Color.FromArgb(26, 74, 138);
        private static readonly Color CFondo= Color.FromArgb(240, 244, 250);
        private static readonly Color CBar  = Color.FromArgb(232, 240, 250);
        private static readonly Color CVerde= Color.FromArgb(26, 110, 58);
        private static readonly Color CRojo = Color.FromArgb(180, 50, 50);

        public FrmKardex()
        {
            Text        = "Kardex — Control de Inventario";
            Size        = new Size(1200, 680);
            MinimumSize = new Size(1000, 560);
            StartPosition = FormStartPosition.CenterParent;
            BackColor   = CFondo;

            // ── Header ────────────────────────────────────────────────────
            var hdr = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = C1 };
            hdr.Controls.Add(new Label {
                Text = "Kardex — Control de Inventario",
                ForeColor = Color.White, Font = new Font("Segoe UI", 14f, FontStyle.Bold),
                AutoSize = true, Location = new Point(14, 13) });

            var pnlHdrRight = new FlowLayoutPanel {
                Dock = DockStyle.Right, AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false, BackColor = Color.Transparent,
                Padding = new Padding(0, 9, 8, 0)
            };
            var btnAyuda    = HdrBtn("? Ayuda",   Color.FromArgb(80, 50, 150));
            var btnPlanilla = HdrBtn("⊟ Planilla", Color.FromArgb(60, 100, 180));
            btnAyuda.Click    += (s, e) => MostrarAyuda();
            btnPlanilla.Click += (s, e) => ImprimirPlanilla();
            pnlHdrRight.Controls.AddRange(new Control[] { btnAyuda, btnPlanilla });
            hdr.Controls.Add(pnlHdrRight);

            // ── Selector de Kardex (barra debajo del header) ──────────────
            var pnlSelector = new Panel {
                Dock = DockStyle.Top, Height = 48,
                BackColor = Color.FromArgb(15, 55, 110) // azul más oscuro
            };

            // Botón Kardex B — Productos
            var btnSelB = new Button {
                Text = "📦  Kardex B — Productos Terminados",
                Size = new Size(300, 38),
                Location = new Point(8, 5),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleCenter
            };
            btnSelB.FlatAppearance.BorderSize = 2;

            // Botón Kardex A — Insumos
            var btnSelA = new Button {
                Text = "🧪  Kardex A — Insumos / Mat. Primas",
                Size = new Size(300, 38),
                Location = new Point(316, 5),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleCenter
            };
            btnSelA.FlatAppearance.BorderSize = 2;

            // Estado activo/inactivo de los botones selector
            void ActualizarSelector(bool mostrarB)
            {
                // Activo: blanco con borde blanco, texto azul
                // Inactivo: transparente con borde semitransparente, texto blanco semiopaco
                if (mostrarB)
                {
                    btnSelB.BackColor = Color.White;
                    btnSelB.ForeColor = C1;
                    btnSelB.FlatAppearance.BorderColor = Color.White;

                    btnSelA.BackColor = Color.FromArgb(40, 255, 255, 255);
                    btnSelA.ForeColor = Color.FromArgb(200, 255, 255, 255);
                    btnSelA.FlatAppearance.BorderColor = Color.FromArgb(100, 255, 255, 255);
                }
                else
                {
                    btnSelA.BackColor = Color.White;
                    btnSelA.ForeColor = Color.FromArgb(100, 60, 20);
                    btnSelA.FlatAppearance.BorderColor = Color.White;

                    btnSelB.BackColor = Color.FromArgb(40, 255, 255, 255);
                    btnSelB.ForeColor = Color.FromArgb(200, 255, 255, 255);
                    btnSelB.FlatAppearance.BorderColor = Color.FromArgb(100, 255, 255, 255);
                }
                pnlContenido.Controls.Clear();
                if (mostrarB)
                    pnlContenido.Controls.Add(_tabB);
                else
                    pnlContenido.Controls.Add(_tabA);
            }

            pnlSelector.Controls.AddRange(new Control[] { btnSelB, btnSelA });

            // ── Panel contenedor (reemplaza TabControl) ───────────────────
            pnlContenido = new Panel { Dock = DockStyle.Fill };

            _tabB = BuildTabB();
            _tabA = BuildTabA();
            _tabB.Dock = DockStyle.Fill;
            _tabA.Dock = DockStyle.Fill;

            btnSelB.Click += (s, e) => { _mostrandoB = true;  ActualizarSelector(true);  };
            btnSelA.Click += (s, e) => { _mostrandoB = false; ActualizarSelector(false); };

            Controls.AddRange(new Control[] { pnlContenido, pnlSelector, hdr });

            // Mostrar Kardex B por defecto
            ActualizarSelector(true);

            VentanaPrefs.Restaurar(this);
            CargarDatosB();
            CargarDatosA();
        }

        // ════════════════════════════════════════════════════════════════
        //  PESTAÑA B — PRODUCTOS TERMINADOS
        // ════════════════════════════════════════════════════════════════
        private Panel BuildTabB()
        {
            var tab = new Panel { BackColor = CFondo };

            // Barra de acciones
            var barAcc = new Panel { Dock = DockStyle.Top, Height = 42, BackColor = C1 };
            var pnlAcc = new FlowLayoutPanel {
                Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false, BackColor = Color.Transparent,
                Padding = new Padding(6, 5, 6, 0)
            };
            var btnEntB = AccBtn("▲ Entrada",  CVerde);
            var btnSalB = AccBtn("▼ Salida",   CRojo);
            var btnAjuB = AccBtn("⚖ Ajuste",   Color.FromArgb(60, 100, 180));
            var btnExpB = AccBtn("↓ CSV",      Color.FromArgb(80, 80, 100));
            btnEntB.Click += (s, e) => AbrirEntradaProducto();
            btnSalB.Click += (s, e) => AbrirSalidaProducto();
            btnAjuB.Click += (s, e) => AbrirAjuste();
            btnExpB.Click += (s, e) => ExportarCSV("B");
            pnlAcc.Controls.AddRange(new Control[] { btnEntB, btnSalB, btnAjuB, btnExpB });
            barAcc.Controls.Add(pnlAcc);

            // Barra de filtros
            var barF = new Panel { Dock = DockStyle.Top, Height = 42, BackColor = CBar };
            int fx = 8;
            barF.Controls.Add(Lbl("Producto:", fx, 13)); fx += 72;
            cmbProducto = new ComboBox {
                Location = new Point(fx, 9), Size = new Size(270, 24),
                DropDownStyle = ComboBoxStyle.DropDownList };
            cmbProducto.Items.Add("— Todos —");
            DatabaseService.ObtenerInventario()
                .ForEach(i => cmbProducto.Items.Add($"{i.Codigo}  {i.Descripcion}"));
            cmbProducto.SelectedIndex = 0;
            barF.Controls.Add(cmbProducto); fx += 278;
            barF.Controls.Add(Lbl("Desde:", fx, 13)); fx += 54;
            dtpDesdeB = new DateTimePicker {
                Location = new Point(fx, 9), Size = new Size(115, 24),
                Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddMonths(-1) };
            barF.Controls.Add(dtpDesdeB); fx += 123;
            barF.Controls.Add(Lbl("Hasta:", fx, 13)); fx += 50;
            dtpHastaB = new DateTimePicker {
                Location = new Point(fx, 9), Size = new Size(115, 24),
                Format = DateTimePickerFormat.Short, Value = DateTime.Today };
            barF.Controls.Add(dtpHastaB); fx += 123;
            var btnFilB = FiltBtn("Filtrar"); btnFilB.Location = new Point(fx, 8);
            btnFilB.Click += (s, e) => CargarDatosB(); barF.Controls.Add(btnFilB); fx += 90;
            var btnLimB = FiltBtn("Limpiar"); btnLimB.Location = new Point(fx, 8);
            btnLimB.BackColor = Color.FromArgb(100, 100, 120);
            btnLimB.Click += (s, e) => { cmbProducto.SelectedIndex=0; dtpDesdeB.Value=DateTime.Today.AddMonths(-1); dtpHastaB.Value=DateTime.Today; CargarDatosB(); };
            barF.Controls.Add(btnLimB);

            // Grid
            dgvB = Grilla(C1);
            dgvB.Name = "dgvB";

            // Footer
            var footB = new Panel { Dock = DockStyle.Bottom, Height = 36, BackColor = CFondo };
            footB.Paint += (s, e) => e.Graphics.DrawLine(new Pen(Color.FromArgb(220,230,245)), 0,0,footB.Width,0);
            lblResumenB = new Label { AutoSize = true, Location = new Point(12, 9),
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 70, 130) };
            footB.Controls.Add(lblResumenB);

            tab.Controls.AddRange(new Control[] { dgvB, footB, barF, barAcc });
            return tab;
        }

        // ════════════════════════════════════════════════════════════════
        //  PESTAÑA A — INSUMOS / MATERIAS PRIMAS
        // ════════════════════════════════════════════════════════════════
        private Panel BuildTabA()
        {
            var tab = new Panel { BackColor = CFondo };

            // Barra de acciones
            var barAcc = new Panel { Dock = DockStyle.Top, Height = 42, BackColor = Color.FromArgb(100, 60, 20) };
            var pnlAcc = new FlowLayoutPanel {
                Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false, BackColor = Color.Transparent,
                Padding = new Padding(6, 5, 6, 0)
            };
            var colorA = Color.FromArgb(120, 70, 20);
            var btnEntA = AccBtn("▲ Entrada",    CVerde);
            var btnSalA = AccBtn("▼ Salida",     CRojo);
            var btnAjuA = AccBtn("⚖ Ajuste",    Color.FromArgb(60, 100, 180));
            var btnExpA = AccBtn("↓ CSV",        Color.FromArgb(80, 80, 100));
            btnEntA.Click += (s, e) => AbrirEntradaInsumo();
            btnSalA.Click += (s, e) => AbrirSalidaInsumo();
            btnAjuA.Click += (s, e) => AbrirAjusteInsumo();
            btnExpA.Click += (s, e) => ExportarCSV("A");
            pnlAcc.Controls.AddRange(new Control[] { btnEntA, btnSalA, btnAjuA, btnExpA });
            barAcc.Controls.Add(pnlAcc);

            // Barra de filtros
            var barF = new Panel { Dock = DockStyle.Top, Height = 42, BackColor = CBar };
            int fx = 8;
            barF.Controls.Add(Lbl("Insumo:", fx, 13)); fx += 62;
            cmbInsumo = new ComboBox {
                Location = new Point(fx, 9), Size = new Size(300, 24),
                DropDownStyle = ComboBoxStyle.DropDownList };
            cmbInsumo.Items.Add("— Todos —");
            DatabaseService.ObtenerProductosProveedor()
                .ForEach(i => cmbInsumo.Items.Add($"{i.CodigoInterno}  {i.Descripcion}"));
            cmbInsumo.SelectedIndex = 0;
            barF.Controls.Add(cmbInsumo); fx += 308;
            barF.Controls.Add(Lbl("Desde:", fx, 13)); fx += 54;
            dtpDesdeA = new DateTimePicker {
                Location = new Point(fx, 9), Size = new Size(115, 24),
                Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddMonths(-1) };
            barF.Controls.Add(dtpDesdeA); fx += 123;
            barF.Controls.Add(Lbl("Hasta:", fx, 13)); fx += 50;
            dtpHastaA = new DateTimePicker {
                Location = new Point(fx, 9), Size = new Size(115, 24),
                Format = DateTimePickerFormat.Short, Value = DateTime.Today };
            barF.Controls.Add(dtpHastaA); fx += 123;
            var btnFilA = FiltBtn("Filtrar"); btnFilA.Location = new Point(fx, 8);
            btnFilA.Click += (s, e) => CargarDatosA(); barF.Controls.Add(btnFilA); fx += 90;
            var btnLimA = FiltBtn("Limpiar"); btnLimA.Location = new Point(fx, 8);
            btnLimA.BackColor = Color.FromArgb(100, 100, 120);
            btnLimA.Click += (s, e) => { cmbInsumo.SelectedIndex=0; dtpDesdeA.Value=DateTime.Today.AddMonths(-1); dtpHastaA.Value=DateTime.Today; CargarDatosA(); };
            barF.Controls.Add(btnLimA);

            // Grid
            dgvA = Grilla(Color.FromArgb(120, 70, 20));
            dgvA.Name = "dgvA";

            // Footer
            var footA = new Panel { Dock = DockStyle.Bottom, Height = 36, BackColor = CFondo };
            footA.Paint += (s, e) => e.Graphics.DrawLine(new Pen(Color.FromArgb(220,230,245)), 0,0,footA.Width,0);
            lblResumenA = new Label { AutoSize = true, Location = new Point(12, 9),
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 60, 10) };
            footA.Controls.Add(lblResumenA);

            tab.Controls.AddRange(new Control[] { dgvA, footA, barF, barAcc });
            return tab;
        }

        // ════════════════════════════════════════════════════════════════
        //  CARGA DE DATOS
        // ════════════════════════════════════════════════════════════════
        private void CargarDatosB()
        {
            string cod = cmbProducto.SelectedIndex > 0
                ? cmbProducto.SelectedItem.ToString().Split(' ')[0].Trim() : null;

            var lista = DatabaseService.ObtenerKardex(cod, dtpDesdeB.Value, dtpHastaB.Value);

            dgvB.DataSource = null;
            dgvB.DataSource = lista.Select(m => new {
                Fecha         = m.FechaHora.ToString("dd/MM/yy HH:mm"),
                Código        = m.CodigoProducto,
                Tipo          = m.TipoMovimiento,
                m.Concepto,
                Cantidad      = m.Cantidad.ToString("N2"),
                SaldoAnt      = m.SaldoAnterior.ToString("N2"),
                NuevoSaldo    = m.NuevoSaldo.ToString("N2"),
                CostoUnit     = Sesion.EsAdmin ? m.CostoUnitario.ToString("N4") : "—",
                ValorTotal    = Sesion.EsAdmin ? (m.Cantidad * m.CostoUnitario).ToString("N2") : "—",
                m.Referencia,
                m.Usuario
            }).ToList();

            ColorearFilas(dgvB);

            decimal entradas = lista.Where(m=>m.TipoMovimiento=="ENTRADA").Sum(m=>m.Cantidad);
            decimal salidas  = lista.Where(m=>m.TipoMovimiento=="SALIDA").Sum(m=>m.Cantidad);
            lblResumenB.Text = $"  {lista.Count} registros  |  " +
                $"ENTRADAS: {entradas:N2}  |  SALIDAS: {salidas:N2}  |  Neto: {entradas-salidas:N2}";
        }

        private void CargarDatosA()
        {
            string cod = cmbInsumo.SelectedIndex > 0
                ? cmbInsumo.SelectedItem.ToString().Split(' ')[0].Trim() : null;

            var lista = DatabaseService.ObtenerKardexInsumos(cod, dtpDesdeA.Value, dtpHastaA.Value);

            dgvA.DataSource = null;
            dgvA.DataSource = lista.Select(m => new {
                Fecha         = m.FechaHora.ToString("dd/MM/yy HH:mm"),
                Código        = m.CodigoProducto,
                Tipo          = m.TipoMovimiento,
                m.Concepto,
                Cantidad      = m.Cantidad.ToString("N2"),
                SaldoAnt      = m.SaldoAnterior.ToString("N2"),
                NuevoSaldo    = m.NuevoSaldo.ToString("N2"),
                CostoUnit     = Sesion.EsAdmin ? m.CostoUnitario.ToString("N4") : "—",
                CostoPP       = Sesion.EsAdmin ? (m.NuevoSaldo > 0 ? "Ver insumo" : "—") : "—",
                m.Referencia,
                m.Usuario
            }).ToList();

            ColorearFilas(dgvA);

            decimal entradas = lista.Where(m=>m.TipoMovimiento=="ENTRADA").Sum(m=>m.Cantidad);
            decimal salidas  = lista.Where(m=>m.TipoMovimiento=="SALIDA").Sum(m=>m.Cantidad);
            lblResumenA.Text = $"  {lista.Count} registros  |  " +
                $"ENTRADAS: {entradas:N2} kg  |  SALIDAS: {salidas:N2} kg  |  Neto: {entradas-salidas:N2} kg";
        }

        private static void ColorearFilas(DataGridView dgv)
        {
            foreach (DataGridViewRow row in dgv.Rows)
            {
                var tipo = row.Cells["Tipo"].Value?.ToString();
                if (tipo == "ENTRADA")
                    row.DefaultCellStyle.ForeColor = Color.FromArgb(20, 120, 50);
                else if (tipo == "SALIDA")
                    row.DefaultCellStyle.ForeColor = Color.FromArgb(160, 40, 40);
                var concepto = row.Cells["Concepto"].Value?.ToString() ?? "";
                if (concepto.StartsWith("AJUSTE") || concepto.StartsWith("Ajuste"))
                    row.DefaultCellStyle.BackColor = Color.FromArgb(255, 250, 210);
            }
        }

        // ════════════════════════════════════════════════════════════════
        //  MOVIMIENTOS KARDEX B
        // ════════════════════════════════════════════════════════════════
        private void AbrirEntradaProducto()
        {
            using var dlg = new DlgMovimientoV2("ENTRADA", false);
            if (dlg.ShowDialog() != DialogResult.OK) return;
            try {
                DatabaseService.RegistrarMovimiento(
                    dlg.Codigo, "ENTRADA", dlg.Cantidad, dlg.Concepto,
                    dlg.Referencia, Sesion.NombreUsuario, dlg.CostoUnitario);
                CargarDatosB();
            } catch (Exception ex) { Err(ex); }
        }

        private void AbrirSalidaProducto()
        {
            using var dlg = new DlgMovimientoV2("SALIDA", false);
            if (dlg.ShowDialog() != DialogResult.OK) return;
            try {
                DatabaseService.RegistrarMovimiento(
                    dlg.Codigo, "SALIDA", dlg.Cantidad, dlg.Concepto,
                    dlg.Referencia, Sesion.NombreUsuario, 0);
                CargarDatosB();
            } catch (Exception ex) { Err(ex); }
        }

        private void AbrirAjuste()
        {
            using var dlg = new DlgAjuste();
            if (dlg.ShowDialog() != DialogResult.OK) return;
            try {
                DatabaseService.RegistrarCorreccion(
                    dlg.CodigoProducto, dlg.NuevoStock, dlg.Motivo, Sesion.NombreUsuario);
                CargarDatosB();
            } catch (Exception ex) { Err(ex); }
        }

        // ════════════════════════════════════════════════════════════════
        //  MOVIMIENTOS KARDEX A
        // ════════════════════════════════════════════════════════════════
        private void AbrirEntradaInsumo()
        {
            using var dlg = new DlgMovimientoV2("ENTRADA", true);
            if (dlg.ShowDialog() != DialogResult.OK) return;
            try {
                DatabaseService.RegistrarEntradaInsumo(
                    dlg.Codigo, dlg.Cantidad, dlg.CostoUnitario,
                    dlg.Concepto, dlg.Referencia, Sesion.NombreUsuario);
                CargarDatosA();
            } catch (Exception ex) { Err(ex); }
        }

        private void AbrirSalidaInsumo()
        {
            using var dlg = new DlgMovimientoV2("SALIDA", true);
            if (dlg.ShowDialog() != DialogResult.OK) return;
            try {
                DatabaseService.RegistrarSalidaInsumo(
                    dlg.Codigo, dlg.Cantidad, dlg.Concepto,
                    dlg.Referencia, Sesion.NombreUsuario);
                CargarDatosA();
            } catch (Exception ex) { Err(ex); }
        }

        private void AbrirAjusteInsumo()
        {
            using var dlg = new DlgAjusteInsumo();
            if (dlg.ShowDialog() != DialogResult.OK) return;
            try {
                // Registrar como salida o entrada según la diferencia
                var ins = DatabaseService.ObtenerProductosProveedor()
                    .Find(i => i.CodigoInterno == dlg.Codigo);
                if (ins == null) return;
                decimal diff = dlg.NuevoStock - ins.StockActual;
                if (diff > 0)
                    DatabaseService.RegistrarEntradaInsumo(dlg.Codigo, diff, 0,
                        "AJUSTE — " + dlg.Motivo, "", Sesion.NombreUsuario);
                else if (diff < 0)
                    DatabaseService.RegistrarSalidaInsumo(dlg.Codigo, Math.Abs(diff),
                        "AJUSTE — " + dlg.Motivo, "", Sesion.NombreUsuario);
                CargarDatosA();
                MessageBox.Show("Ajuste registrado.", "OK",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            } catch (Exception ex) { Err(ex); }
        }

        // ════════════════════════════════════════════════════════════════
        //  EXPORTAR CSV
        // ════════════════════════════════════════════════════════════════
        private void ExportarCSV(string kardex)
        {
            using var dlg = new SaveFileDialog {
                Title = $"Exportar Kardex {kardex}",
                Filter = "CSV UTF-8 (*.csv)|*.csv",
                FileName = $"Kardex{kardex}_{DateTime.Today:yyyyMMdd}.csv"
            };
            if (dlg.ShowDialog() != DialogResult.OK) return;
            try {
                var lista = kardex == "B"
                    ? DatabaseService.ObtenerKardex()
                    : DatabaseService.ObtenerKardexInsumos();

                var lines = new System.Collections.Generic.List<string> {
                    "\uFEFFID,Fecha,Codigo,Tipo,Concepto,Cantidad,SaldoAnt,NuevoSaldo,CostoUnit,Referencia,Usuario"
                };
                foreach (var m in lista)
                    lines.Add($"{m.IdMovimiento},{m.FechaHora:dd/MM/yyyy HH:mm}," +
                        $"{m.CodigoProducto},{m.TipoMovimiento},\"{m.Concepto}\"," +
                        $"{m.Cantidad:N2},{m.SaldoAnterior:N2},{m.NuevoSaldo:N2}," +
                        $"{m.CostoUnitario:N4},{m.Referencia},{m.Usuario}");

                System.IO.File.WriteAllLines(dlg.FileName, lines,
                    new System.Text.UTF8Encoding(true));
                System.Diagnostics.Process.Start(
                    new System.Diagnostics.ProcessStartInfo { FileName = dlg.FileName, UseShellExecute = true });
                MessageBox.Show($"Exportado: {lista.Count} registros.", "OK",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            } catch (Exception ex) { Err(ex); }
        }

        // ════════════════════════════════════════════════════════════════
        //  PLANILLA IMPRIMIBLE
        // ════════════════════════════════════════════════════════════════
        private void ImprimirPlanilla()
        {
            bool esInsumo = !_mostrandoB;
            var pd = new PrintDocument();
            pd.DefaultPageSettings.Landscape = true;
            pd.DefaultPageSettings.Margins = new Margins(40, 40, 50, 50);

            pd.PrintPage += (s, e) => {
                var g  = e.Graphics;
                var fn = new Font("Segoe UI", 10f, FontStyle.Bold);
                var fs = new Font("Segoe UI", 8.5f);
                var fc = new Font("Segoe UI", 8f);
                int y  = e.MarginBounds.Top;
                int x  = e.MarginBounds.Left;
                int W  = e.MarginBounds.Width;

                // Título
                g.FillRectangle(new SolidBrush(C1),
                    new Rectangle(x, y, W, 30));
                g.DrawString(
                    esInsumo ? "PLANILLA KARDEX A — INSUMOS / MATERIAS PRIMAS"
                             : "PLANILLA KARDEX B — PRODUCTOS TERMINADOS",
                    new Font("Segoe UI", 14f, FontStyle.Bold), Brushes.White,
                    new RectangleF(x + 8, y + 6, W, 24));
                y += 35;

                // Info empresa
                g.DrawString($"Fecha: ________________________    Responsable: ________________________    Período: ____________ al ____________",
                    fs, Brushes.Black, x, y); y += 20;
                g.DrawString(esInsumo
                    ? "Insumo (código): ________________________    Descripción: ________________________________________________    Unidad: ______"
                    : "Producto (código): ______________________    Descripción: ________________________________________________    Unidad: ______",
                    fs, Brushes.Black, x, y); y += 22;

                // Encabezados tabla
                string[] hdrs = esInsumo
                    ? new[]{"Fecha","Tipo","Concepto / Origen","N° Factura","Cant.\nEntrada","Cant.\nSalida","Saldo\nAnterior","Nuevo\nSaldo","Costo\nUnit. $","Costo\nPP $","Valor\nTotal $","Observación"}
                    : new[]{"Fecha","Tipo","Concepto / Destino","Referencia","Cant.\nEntrada","Cant.\nSalida","Saldo\nAnterior","Nuevo\nSaldo","Costo\nUnit. $","Costo\nPP $","Valor\nTotal $","Observación"};

                int[] colW = { 60, 55, 130, 80, 55, 55, 60, 60, 60, 60, 60, 85 };
                int xc = x;
                g.FillRectangle(new SolidBrush(C1), new Rectangle(x, y, W, 24));
                for (int i = 0; i < hdrs.Length; i++) {
                    g.DrawString(hdrs[i].Replace("\n", " "),
                        new Font("Segoe UI", 7.5f, FontStyle.Bold), Brushes.White,
                        new RectangleF(xc + 2, y + 3, colW[i] - 4, 20));
                    xc += colW[i];
                }
                y += 26;

                // 18 filas vacías
                for (int row = 0; row < 18; row++) {
                    Color bg = row % 2 == 0 ? Color.White : Color.FromArgb(245, 248, 255);
                    g.FillRectangle(new SolidBrush(bg), new Rectangle(x, y, W, 20));
                    xc = x;
                    for (int i = 0; i < colW.Length; i++) {
                        g.DrawRectangle(Pens.LightGray, new Rectangle(xc, y, colW[i], 20));
                        xc += colW[i];
                    }
                    y += 20;
                }
                y += 8;

                // Sección de totales
                g.FillRectangle(new SolidBrush(Color.FromArgb(232, 240, 250)),
                    new Rectangle(x, y, W, 22));
                g.DrawString("TOTALES DEL PERÍODO:    Entradas: ______________    Salidas: ______________    Saldo final: ______________    Valor inventario final: ______________",
                    new Font("Segoe UI", 8.5f, FontStyle.Bold), Brushes.Black, x + 6, y + 4);
                y += 28;

                // Nota CPP
                g.DrawString(
                    "* Costo PP = Costo Promedio Ponderado = (Stock ant. × Costo ant. + Cant. entrada × Costo nuevo) ÷ Nuevo saldo",
                    new Font("Segoe UI", 7.5f, FontStyle.Italic),
                    Brushes.Gray, x, y); y += 14;

                // Firmas
                g.DrawString(
                    "_______________________________          _______________________________          _______________________________",
                    fs, Brushes.Black, x + 30, y + 30);
                g.DrawString(
                    "       Encargado de Almacén                      Supervisor / Contabilidad                        Gerencia / Admin",
                    fc, Brushes.Black, x + 30, y + 50);
            };

            using var ppd = new PrintPreviewDialog { Document = pd, WindowState = FormWindowState.Maximized };
            ppd.ShowDialog(this);
        }

        // ════════════════════════════════════════════════════════════════
        //  AYUDA PASO A PASO
        // ════════════════════════════════════════════════════════════════
        private void MostrarAyuda()
        {
            bool esInsumo = !_mostrandoB;
            var frm = new Form {
                Text          = esInsumo ? "Ayuda — Kardex A (Insumos)" : "Ayuda — Kardex B (Productos)",
                Size          = new Size(800, 700),
                MinimumSize   = new Size(640, 500),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.Sizable,
                BackColor     = Color.White
            };

            // Header
            var hdr = new Panel { Dock = DockStyle.Top, Height = 52, BackColor = C1 };
            hdr.Controls.Add(new Label {
                Text = esInsumo ? "📋  Guía paso a paso — Kardex A: Insumos / Materias Primas"
                                : "📋  Guía paso a paso — Kardex B: Productos Terminados",
                ForeColor = Color.White, Font = new Font("Segoe UI", 14f, FontStyle.Bold),
                AutoSize = true, Location = new Point(14, 14) });

            // RichTextBox con formato enriquecido
            var rtf = new RichTextBox {
                Dock = DockStyle.Fill, ReadOnly = true, BorderStyle = BorderStyle.None,
                BackColor = Color.White, ScrollBars = RichTextBoxScrollBars.Vertical,
                Padding = new Padding(12)
            };

            // Helpers de formato
            var fTitulo = new Font("Segoe UI", 16f, FontStyle.Bold);
            var fSeccion = new Font("Segoe UI", 14f, FontStyle.Bold);
            var fNormal  = new Font("Segoe UI", 11f);
            var fNeg     = new Font("Segoe UI", 13f, FontStyle.Bold);
            var fMono    = new Font("Courier New", 11f);

            void Titulo(string t, Color color)
            {
                rtf.SelectionStart = rtf.TextLength;
                rtf.SelectionFont  = fTitulo;
                rtf.SelectionColor = color;
                rtf.AppendText("\r\n" + t + "\r\n");
                rtf.SelectionFont  = new Font("Segoe UI", 5f);
                rtf.SelectionColor = color;
                rtf.AppendText("────────────────────────────────────────────────────\r\n\r\n");
            }
            void Seccion(string t)
            {
                rtf.SelectionStart = rtf.TextLength;
                rtf.SelectionFont  = fSeccion;
                rtf.SelectionColor = C1;
                rtf.AppendText(t + "\r\n");
                rtf.SelectionFont  = new Font("Segoe UI", 4f);
                rtf.AppendText("\r\n");
            }
            void ParrafoN(string normal, string negrita = "")
            {
                rtf.SelectionStart = rtf.TextLength;
                if (!string.IsNullOrEmpty(negrita)) {
                    rtf.SelectionFont  = fNeg;
                    rtf.SelectionColor = Color.FromArgb(30, 41, 59);
                    rtf.AppendText(negrita);
                }
                rtf.SelectionFont  = fNormal;
                rtf.SelectionColor = Color.FromArgb(55, 65, 81);
                rtf.AppendText(normal + "\r\n");
            }
            void Paso(int n, string t)
            {
                rtf.SelectionStart = rtf.TextLength;
                rtf.SelectionFont  = fNeg;
                rtf.SelectionColor = C1;
                rtf.AppendText($"  {n}.  ");
                rtf.SelectionFont  = fNormal;
                rtf.SelectionColor = Color.FromArgb(55, 65, 81);
                rtf.AppendText(t + "\r\n");
            }
            void Bul(string t, Color? c2 = null)
            {
                rtf.SelectionStart = rtf.TextLength;
                rtf.SelectionFont  = fNeg;
                rtf.SelectionColor = c2 ?? Color.FromArgb(26, 110, 58);
                rtf.AppendText("  •  ");
                rtf.SelectionFont  = fNormal;
                rtf.SelectionColor = Color.FromArgb(55, 65, 81);
                rtf.AppendText(t + "\r\n");
            }
            void Formula(string t)
            {
                rtf.SelectionStart = rtf.TextLength;
                rtf.SelectionFont  = fMono;
                rtf.SelectionColor = Color.FromArgb(100, 60, 20);
                rtf.SelectionBackColor = Color.FromArgb(255, 248, 235);
                rtf.AppendText("  " + t + "\r\n");
                rtf.SelectionBackColor = Color.White;
            }
            void Espacio() { rtf.SelectionFont = new Font("Segoe UI", 6f); rtf.AppendText("\r\n"); }

            if (!esInsumo)
            {
                Titulo("Kardex B — Productos Terminados", C1);

                Seccion("¿Qué es?");
                ParrafoN("Registro histórico de todas las entradas y salidas del inventario de " +
                         "productos fabricados. Cada movimiento actualiza el stock en tiempo real.");
                Espacio();

                Seccion("Costo Promedio Ponderado (CPP)");
                ParrafoN("Al registrar una ENTRADA con precio, el sistema recalcula automáticamente:");
                Espacio();
                Formula("Nuevo CPP = (Stock ant. × Costo ant. + Cantidad × Costo nuevo)");
                Formula("            ÷ (Stock ant. + Cantidad nueva)");
                Espacio();
                ParrafoN("", "Ejemplo:  ");
                ParrafoN("  Stock: 100 und × $3.00 = $300    |    Entrada: 50 und × $3.60 = $180");
                Formula("  Nuevo CPP = ($300 + $180) ÷ 150 = $3.20 por unidad");
                Espacio();

                Seccion("▲  Cuándo registrar una ENTRADA");
                Bul("Al recibir devolución de un cliente");
                Bul("Al corregir diferencia de inventario (usar Ajuste)");
                Bul("La producción del día se registra AUTOMÁTICAMENTE", Color.FromArgb(80, 80, 100));
                Espacio();

                Seccion("Paso a paso — Registrar Entrada:");
                Paso(1, "Clic en [▲ Entrada]");
                Paso(2, "Seleccionar el producto del combo desplegable");
                Paso(3, "Verificar el stock actual mostrado");
                Paso(4, "Ingresar la cantidad en unidades");
                Paso(5, "Ingresar costo unitario (para actualizar el CPP)");
                Paso(6, "Concepto: ej. 'Devolución cliente 512', 'Ajuste inicial'");
                Paso(7, "Referencia: número de orden o factura");
                Paso(8, "Clic en [✓ Registrar Entrada]");
                Espacio();

                Seccion("▼  Cuándo registrar una SALIDA");
                Bul("Al enviar muestra a un cliente", CRojo);
                Bul("Al registrar producto dañado o pérdida", CRojo);
                Bul("Las ventas por factura se descuentan AUTOMÁTICAMENTE", Color.FromArgb(80, 80, 100));
                Espacio();

                Seccion("Paso a paso — Registrar Salida:");
                Paso(1, "Clic en [▼ Salida]");
                Paso(2, "Seleccionar el producto");
                Paso(3, "Verificar el stock disponible mostrado");
                Paso(4, "Ingresar la cantidad a descontar");
                Paso(5, "Concepto: ej. 'Muestra cliente', 'Producto dañado'");
                Paso(6, "Clic en [✓ Registrar Salida]");
                Espacio();

                Seccion("⚖  Ajuste por conteo físico");
                Paso(1, "Contar el stock físico real");
                Paso(2, "Clic en [⚖ Ajuste]");
                Paso(3, "Seleccionar el producto");
                Paso(4, "Ingresar el stock REAL contado");
                Paso(5, "Motivo: 'Conteo físico', 'Diferencia inventario'");
                Paso(6, "Clic en [Aplicar]");
                Espacio();

                Seccion("✓  Mantenimiento recomendado");
                Bul("Semanal: verificar saldo sistema vs. físico");
                Bul("Mensual: exportar CSV para contabilidad");
                Bul("Mensual: imprimir planilla para archivo físico");
            }
            else
            {
                Titulo("Kardex A — Insumos / Materias Primas", Color.FromArgb(120, 70, 20));

                Seccion("¿Qué es?");
                ParrafoN("Control de stock de las materias primas: polietileno (soplado, tobo, etc.) " +
                         "y demás insumos comprados a proveedores. Las salidas por producción son " +
                         "automáticas; solo se registran manualmente las compras.");
                Espacio();

                Seccion("Costo Promedio Ponderado (CPP)");
                ParrafoN("Cada vez que llega material nuevo con precio distinto, el sistema " +
                         "recalcula el costo promedio automáticamente:");
                Espacio();
                Formula("Nuevo CPP = (Stock ant. × Costo ant. + Kg × Costo/kg)");
                Formula("            ÷ (Stock ant. + Kg nuevos)");
                Espacio();
                ParrafoN("", "Ejemplo:  ");
                ParrafoN("  Stock: 200 kg × $2.50/kg = $500    |    Entrada: 300 kg × $2.80/kg = $840");
                Formula("  Nuevo CPP = ($500 + $840) ÷ 500 = $2.68 por kg");
                ParrafoN("Las SALIDAS siempre usan el CPP vigente sin recalcular.");
                Espacio();

                Seccion("▲  Paso a paso — Registrar llegada de material");
                ParrafoN("Tener a mano: ", "factura del proveedor, peso en kg, precio por kg.");
                Espacio();
                Paso(1, "Clic en [▲ Entrada]");
                Paso(2, "Seleccionar el insumo  (ej: 6200B — Soplado natural)");
                Paso(3, "Verificar el stock actual y CPP mostrados");
                Paso(4, "Cantidad: kilogramos recibidos según báscula/factura");
                Paso(5, "Costo unitario: precio por kg de la factura del proveedor");
                Paso(6, "Concepto: 'Compra soplado', 'Recepción pedido Proveedor X'");
                Paso(7, "Referencia: número de factura del proveedor");
                Paso(8, "Clic en [✓ Registrar Entrada]  →  el sistema actualiza CPP");
                Espacio();

                Seccion("▼  Salida manual de insumo");
                ParrafoN("Las salidas por producción son AUTOMÁTICAS. Solo usar salida manual para:");
                Bul("Mermas extra no registradas por producción", CRojo);
                Bul("Muestras entregadas al proveedor", CRojo);
                Bul("Diferencias de pesaje detectadas", CRojo);
                Espacio();
                Paso(1, "Clic en [▼ Salida]");
                Paso(2, "Seleccionar el insumo  →  verificar stock");
                Paso(3, "Cantidad en kg");
                Paso(4, "Concepto: 'Merma extra', 'Muestra proveedor'");
                Paso(5, "Clic en [✓ Registrar Salida]");
                Espacio();

                Seccion("⚖  Ajuste por conteo físico");
                Paso(1, "Pesar TODOS los sacos del almacén en la báscula");
                Paso(2, "Clic en [⚖ Ajuste]  →  seleccionar insumo");
                Paso(3, "Ingresar el peso REAL pesado");
                Paso(4, "Motivo: 'Conteo físico semana 15', 'Diferencia báscula'");
                Paso(5, "Clic en [Aplicar]");
                Espacio();

                Seccion("✓  Rutina semanal recomendada");
                Bul("Lunes: registrar entradas pendientes de la semana anterior");
                Bul("Viernes: pesar sacos y comparar con sistema");
                Bul("Si hay diferencia > 2%: registrar ajuste con motivo");
                Bul("Mensual: exportar CSV para contabilidad de costos");
                Bul("Mensual: imprimir planilla para archivo físico firmado");
            }

            rtf.SelectionStart = 0; rtf.ScrollToCaret();

            var btnCerrar = new Button {
                Text = "Cerrar", Dock = DockStyle.Bottom, Height = 40,
                BackColor = C1, ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10f, FontStyle.Bold)
            };
            btnCerrar.FlatAppearance.BorderSize = 0;
            btnCerrar.Click += (s, e) => frm.Close();

            frm.Controls.AddRange(new Control[] { rtf, btnCerrar, hdr });

            // VentanaPrefs — recuerda posición y tamaño
            VentanaPrefs.Restaurar(frm);

            frm.ShowDialog(this);
            frm.Dispose();
        }

        // ════════════════════════════════════════════════════════════════
        //  HELPERS UI
        // ════════════════════════════════════════════════════════════════
        private static DataGridView Grilla(Color headerColor)
        {
            var g = new DataGridView {
                Dock = DockStyle.Fill, ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false, AllowUserToAddRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White, BorderStyle = BorderStyle.None,
                RowHeadersVisible = false, Font = new Font("Segoe UI", 8.5f),
                RowTemplate = { Height = 22 }
            };
            g.ColumnHeadersDefaultCellStyle.BackColor = headerColor;
            g.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            g.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            g.EnableHeadersVisualStyles = false;
            g.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(245, 248, 255);
            g.DataBindingComplete += (s, e) => ColorearFilas(g);
            return g;
        }

        private static Button HdrBtn(string t, Color c)
        {
            var b = new Button { Text = t, Size = new Size(90, 30),
                BackColor = c, ForeColor = Color.White, FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 4, 0) };
            b.FlatAppearance.BorderSize = 0; return b;
        }

        private static Button AccBtn(string t, Color c)
        {
            var b = new Button { Text = t, Size = new Size(90, 30),
                BackColor = c, ForeColor = Color.White, FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), Cursor = Cursors.Hand,
                Margin = new Padding(2, 2, 2, 2) };
            b.FlatAppearance.BorderSize = 0; return b;
        }

        private static Button FiltBtn(string t)
        {
            var b = new Button { Text = t, Size = new Size(80, 26),
                BackColor = Color.FromArgb(26, 74, 138), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) };
            b.FlatAppearance.BorderSize = 0; return b;
        }

        private static Label Lbl(string t, int x, int y) =>
            new Label { Text = t, Location = new Point(x, y), AutoSize = true,
                Font = new Font("Segoe UI", 9f) };

        private static void Err(Exception ex) =>
            MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  DlgMovimientoV2 — entrada/salida para PRODUCTOS e INSUMOS
    //  Incluye campo de costo unitario en entradas.
    // ════════════════════════════════════════════════════════════════════════
    public class DlgMovimientoV2 : FormBase
    {
        public string  Codigo       { get; private set; }
        public decimal Cantidad     { get; private set; }
        public decimal CostoUnitario{ get; private set; }
        public string  Concepto     { get; private set; }
        public string  Referencia   { get; private set; }

        public DlgMovimientoV2(string tipo, bool esInsumo)
        {
            bool esEntrada = tipo == "ENTRADA";
            Text = esInsumo
                ? (esEntrada ? "Entrada de Insumo" : "Salida de Insumo")
                : (esEntrada ? "Entrada de Producto" : "Salida de Producto");
            Size = new Size(480, esEntrada ? 380 : 330);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = true; MinimizeBox = false;
            BackColor = esEntrada ? Color.FromArgb(240, 252, 244) : Color.FromArgb(252, 242, 242);

            var colorAcc = esEntrada ? Color.FromArgb(26, 110, 58) : Color.FromArgb(160, 50, 50);
            var banner = new Panel { Dock = DockStyle.Top, Height = 44, BackColor = colorAcc };
            banner.Controls.Add(new Label {
                Text = esEntrada
                    ? (esInsumo ? "▲  Registrar Entrada de Insumo" : "▲  Registrar Entrada de Producto")
                    : (esInsumo ? "▼  Registrar Salida de Insumo"  : "▼  Registrar Salida de Producto"),
                ForeColor = Color.White, Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                AutoSize = true, Location = new Point(14, 11) });
            Controls.Add(banner);

            int y = 56;
            // Aviso CPP en entradas de insumos
            if (esEntrada && esInsumo) {
                var lCpp = new Label {
                    Text = "ℹ  Se calculará el Costo Promedio Ponderado automáticamente.",
                    Location = new Point(14, y), AutoSize = true,
                    ForeColor = Color.FromArgb(26, 74, 138),
                    Font = new Font("Segoe UI", 8f, FontStyle.Italic) };
                Controls.Add(lCpp); y += 22;
            }

            L("Artículo:", y);
            var cmb = new ComboBox { Location = new Point(160, y),
                Size = new Size(295, 24), DropDownStyle = ComboBoxStyle.DropDownList };
            if (esInsumo)
                DatabaseService.ObtenerProductosProveedor()
                    .ForEach(i => cmb.Items.Add($"{i.CodigoInterno} — {i.Descripcion}"));
            else
                DatabaseService.ObtenerInventario()
                    .ForEach(i => cmb.Items.Add($"{i.Codigo} — {i.Descripcion}"));
            if (cmb.Items.Count > 0) cmb.SelectedIndex = 0;
            Controls.Add(cmb); y += 34;

            // Stock actual
            var lblStock = new Label { Location = new Point(160, y), AutoSize = true,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 100, 50) };
            Controls.Add(new Label { Text = "Stock actual:", Location = new Point(14, y + 3),
                AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Bold) });
            Controls.Add(lblStock); y += 30;

            void ActStock() {
                if (cmb.SelectedIndex < 0) return;
                var cod2 = cmb.SelectedItem.ToString().Split('—')[0].Trim();
                if (esInsumo) {
                    var ins2 = DatabaseService.ObtenerProductosProveedor().Find(i => i.CodigoInterno == cod2);
                    lblStock.Text = ins2 != null ? $"{ins2.StockActual:N2} kg  (CPP: ${ins2.CostoUnitario:N4}/kg)" : "—";
                } else {
                    var inv2 = DatabaseService.ObtenerInventario().Find(i => i.Codigo == cod2);
                    lblStock.Text = inv2 != null ? $"{inv2.Existencia:N2} {inv2.Unidad}" : "—";
                }
            }
            cmb.SelectedIndexChanged += (s, e) => ActStock();
            ActStock();

            L("Cantidad:", y);
            var numCant = new NumericUpDown { Location = new Point(160, y),
                Size = new Size(150, 24), Minimum = 0.001m, Maximum = 9999999,
                Value = 1, DecimalPlaces = 2 };
            numCant.Enter += (s, e) => numCant.Select(0, numCant.Text.Length);
            Controls.Add(numCant); y += 34;

            NumericUpDown numCosto = null;
            if (esEntrada) {
                L("Costo unitario $:", y);
                numCosto = new NumericUpDown { Location = new Point(160, y),
                    Size = new Size(150, 24), Minimum = 0, Maximum = 9999999,
                    Value = 0, DecimalPlaces = 4 };
                numCosto.Enter += (s2, e2) => numCosto.Select(0, numCosto.Text.Length);
                Controls.Add(numCosto);
                Controls.Add(new Label {
                    Text = esInsumo ? "($/kg)" : "($/und)",
                    Location = new Point(315, y + 3), AutoSize = true,
                    ForeColor = Color.Gray, Font = new Font("Segoe UI", 8.5f) });
                y += 34;
            }

            L("Concepto:", y);
            var txtConc = new TextBox { Location = new Point(160, y), Size = new Size(295, 24) };
            txtConc.PlaceholderText = esEntrada
                ? (esInsumo ? "ej: Compra soplado Proveedor X" : "ej: Producción lunes")
                : (esInsumo ? "ej: Consumo producción" : "ej: Muestra cliente");
            txtConc.Enter += (s, e) => txtConc.SelectAll();
            Controls.Add(txtConc); y += 34;

            L("Referencia:", y);
            var txtRef = new TextBox { Location = new Point(160, y), Size = new Size(295, 24),
                PlaceholderText = "N° Factura, Orden, etc." };
            txtRef.Enter += (s, e) => txtRef.SelectAll();
            Controls.Add(txtRef); y += 42;

            var bOk = new Button {
                Text = esEntrada ? "✓  Registrar Entrada" : "✓  Registrar Salida",
                Location = new Point(60, y), Size = new Size(180, 34),
                BackColor = colorAcc, ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };
            bOk.FlatAppearance.BorderSize = 0;
            var bCan = new Button { Text = "Cancelar", DialogResult = DialogResult.Cancel,
                Location = new Point(260, y), Size = new Size(120, 34),
                BackColor = Color.FromArgb(120, 120, 130), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat };
            bCan.FlatAppearance.BorderSize = 0;

            bOk.Click += (s, e) => {
                if (cmb.SelectedIndex < 0) {
                    MessageBox.Show("Seleccione un artículo.", "Validación",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                if (string.IsNullOrWhiteSpace(txtConc.Text)) {
                    MessageBox.Show("Ingrese el concepto.", "Validación",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                Codigo        = cmb.SelectedItem.ToString().Split('—')[0].Trim();
                Cantidad      = numCant.Value;
                CostoUnitario = numCosto?.Value ?? 0;
                Concepto      = txtConc.Text.Trim();
                Referencia    = txtRef.Text.Trim();
                DialogResult  = DialogResult.OK;
            };
            
            // Recordar posición y tamaño
            VentanaPrefs.Restaurar(this);
            Controls.AddRange(new Control[] { bOk, bCan });
        }

        private void L(string t, int y) =>
            Controls.Add(new Label { Text = t, Location = new Point(14, y + 3),
                AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Bold) });
    }

    // ════════════════════════════════════════════════════════════════════════
    //  DlgAjusteInsumo
    // ════════════════════════════════════════════════════════════════════════
    public class DlgAjusteInsumo : FormBase
    {
        public string  Codigo    { get; private set; }
        public decimal NuevoStock{ get; private set; }
        public string  Motivo    { get; private set; }

        public DlgAjusteInsumo()
        {
            Text = "Ajuste de Insumo"; Size = new Size(440, 280);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = true; MinimizeBox = false;
            BackColor = Color.FromArgb(240, 244, 250);

            var banner = new Panel { Dock = DockStyle.Top, Height = 40,
                BackColor = Color.FromArgb(26, 74, 138) };
            banner.Controls.Add(new Label {
                Text = "⚖  Ajuste / Corrección de Stock — Insumos",
                ForeColor = Color.White, Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                AutoSize = true, Location = new Point(12, 10) });
            Controls.Add(banner);

            int y = 52;
            void L(string t) => Controls.Add(new Label { Text = t,
                Location = new Point(14, y+3), AutoSize = true,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold) });

            L("Insumo:");
            var cmb = new ComboBox { Location = new Point(170, y), Size = new Size(240, 24),
                DropDownStyle = ComboBoxStyle.DropDownList };
            DatabaseService.ObtenerProductosProveedor()
                .ForEach(i => cmb.Items.Add($"{i.CodigoInterno} — {i.Descripcion}"));
            if (cmb.Items.Count > 0) cmb.SelectedIndex = 0;
            Controls.Add(cmb); y += 32;

            L("Stock en sistema:");
            var lblS = new Label { Location = new Point(170, y), AutoSize = true,
                Font = new Font("Segoe UI", 9f, FontStyle.Italic), ForeColor = Color.Gray };
            Controls.Add(lblS);
            cmb.SelectedIndexChanged += (s, e) => {
                var cod2 = cmb.SelectedItem?.ToString().Split('—')[0].Trim();
                var ins2 = DatabaseService.ObtenerProductosProveedor().Find(i => i.CodigoInterno == cod2);
                lblS.Text = ins2 != null ? $"{ins2.StockActual:N2} kg" : "";
            };
            if (cmb.SelectedIndex >= 0) cmb.SelectedIndex = cmb.SelectedIndex; // trigger
            y += 28;

            L("Nuevo stock real (kg):");
            var numN = new NumericUpDown { Location = new Point(170, y), Size = new Size(240, 24),
                Maximum = 9999999, DecimalPlaces = 2 };
            numN.Enter += (s, e) => numN.Select(0, numN.Text.Length);
            Controls.Add(numN); y += 32;

            L("Motivo:");
            var txtM = new TextBox { Location = new Point(170, y), Size = new Size(240, 24),
                PlaceholderText = "ej: Conteo físico semana 15" };
            txtM.Enter += (s, e) => txtM.SelectAll();
            Controls.Add(txtM); y += 40;

            var bOk = new Button { Text = "Aplicar Ajuste", Location = new Point(60, y),
                Size = new Size(150, 32), BackColor = Color.FromArgb(30, 130, 70),
                ForeColor = Color.White, FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            bOk.FlatAppearance.BorderSize = 0;
            var bCan = new Button { Text = "Cancelar", DialogResult = DialogResult.Cancel,
                Location = new Point(230, y), Size = new Size(110, 32),
                BackColor = Color.FromArgb(120, 120, 130), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat };
            bCan.FlatAppearance.BorderSize = 0;
            bOk.Click += (s, e) => {
                if (string.IsNullOrWhiteSpace(txtM.Text)) {
                    MessageBox.Show("Ingrese el motivo.", "Validación",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                Codigo     = cmb.SelectedItem.ToString().Split('—')[0].Trim();
                NuevoStock = numN.Value;
                Motivo     = txtM.Text.Trim();
                DialogResult = DialogResult.OK;
            };
            
            
            var _dlgTip = AyudaService.CrearTooltip();
            if (numN != null) AyudaService.Tip(_dlgTip, numN, "Ingresa el peso REAL en kg según báscula");
            if (txtM != null) AyudaService.Tip(_dlgTip, txtM, "Motivo del ajuste (ej: Diferencia de báscula)");
            // Recordar posición y tamaño
            VentanaPrefs.Restaurar(this);
            Controls.AddRange(new Control[] { bOk, bCan });
        }
    }

    // Conservar DlgAjuste original para productos terminados
    public class DlgAjuste : FormBase
    {
        public string  CodigoProducto { get; private set; }
        public decimal NuevoStock     { get; private set; }
        public string  Motivo         { get; private set; }

        public DlgAjuste()
        {
            Text = "Ajuste de Inventario"; Size = new Size(420, 280);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = true; MinimizeBox = false;
            BackColor = Color.FromArgb(240, 244, 250);
            var banner = new Panel { Dock = DockStyle.Top, Height = 40,
                BackColor = Color.FromArgb(26, 74, 138) };
            banner.Controls.Add(new Label { Text = "⚖  Ajuste / Corrección de Stock — Productos",
                ForeColor = Color.White, Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                AutoSize = true, Location = new Point(12, 10) });
            Controls.Add(banner);
            int y = 52;
            void L2(string t) => Controls.Add(new Label { Text = t,
                Location = new Point(15, y+3), AutoSize = true,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold) });
            L2("Producto:");
            var cmb2 = new ComboBox { Location = new Point(160, y), Size = new Size(230, 24),
                DropDownStyle = ComboBoxStyle.DropDownList };
            DatabaseService.ObtenerInventario()
                .ForEach(i => cmb2.Items.Add($"{i.Codigo} — {i.Descripcion}"));
            if (cmb2.Items.Count > 0) cmb2.SelectedIndex = 0;
            Controls.Add(cmb2); y += 32;
            L2("Stock en sistema:");
            var lblS2 = new Label { Location = new Point(160, y), AutoSize = true,
                Font = new Font("Segoe UI", 9f, FontStyle.Italic), ForeColor = Color.Gray };
            Controls.Add(lblS2);
            cmb2.SelectedIndexChanged += (s, e) => {
                var cod3 = cmb2.SelectedItem?.ToString().Split('—')[0].Trim();
                var it3  = DatabaseService.ObtenerInventario().Find(i => i.Codigo == cod3);
                lblS2.Text = it3 != null ? $"{it3.Existencia:N2} {it3.Unidad}" : "";
            };
            y += 28;
            L2("Nuevo stock real:");
            var numN2 = new NumericUpDown { Location = new Point(160, y),
                Size = new Size(230, 24), Maximum = 9999999, DecimalPlaces = 2 };
            numN2.Enter += (s, e) => numN2.Select(0, numN2.Text.Length);
            Controls.Add(numN2); y += 32;
            L2("Motivo:");
            var txtM2 = new TextBox { Location = new Point(160, y), Size = new Size(230, 24) };
            txtM2.Enter += (s, e) => txtM2.SelectAll();
            Controls.Add(txtM2); y += 40;
            var bOk2 = new Button { Text = "Aplicar Ajuste", Location = new Point(60, y),
                Size = new Size(140, 32), BackColor = Color.FromArgb(60, 100, 180),
                ForeColor = Color.White, FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            bOk2.FlatAppearance.BorderSize = 0;
            var bCan2 = new Button { Text = "Cancelar", DialogResult = DialogResult.Cancel,
                Location = new Point(220, y), Size = new Size(110, 32),
                BackColor = Color.FromArgb(120, 120, 130), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat };
            bCan2.FlatAppearance.BorderSize = 0;
            bOk2.Click += (s, e) => {
                if (string.IsNullOrWhiteSpace(txtM2.Text)) {
                    MessageBox.Show("Ingrese el motivo.", "Validación",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                CodigoProducto = cmb2.SelectedItem.ToString().Split('—')[0].Trim();
                NuevoStock     = numN2.Value;
                Motivo         = txtM2.Text.Trim();
                DialogResult   = DialogResult.OK;
            };
            
            
            var _dlgTip = AyudaService.CrearTooltip();
            if (numN2 != null) AyudaService.Tip(_dlgTip, numN2, "Ingresa el stock REAL contado físicamente");
            if (txtM2 != null) AyudaService.Tip(_dlgTip, txtM2, "Motivo del ajuste (ej: Conteo físico semana 15)");
            // Recordar posición y tamaño
            VentanaPrefs.Restaurar(this);
            Controls.AddRange(new Control[] { bOk2, bCan2 });
        }
    }
}
