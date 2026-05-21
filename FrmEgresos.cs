// ════════════════════════════════════════════════════════════════════════
// ARCHIVO: FrmEgresos.cs  →  raíz del proyecto
//
// Hub principal del módulo de Egresos. Tabs:
//   - Resumen          : KPIs del mes + gráfico por categoría
//   - Materia Prima    : gastos de la(s) categoría(s) marcada(s) como MP
//   - Gastos Operativos: gastos del resto de categorías
//   - Por Pagar (CxP)  : gastos pendientes con vencimiento
//   - Categorías       : CRUD de CategoriasGasto (solo Admin/Contabilidad)
// ════════════════════════════════════════════════════════════════════════

#nullable enable
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace SistemaGestion
{
    public class FrmEgresos : FormBase
    {
        private TabControl _tabs = null!;
        private DateTimePicker _dtpDesde = null!, _dtpHasta = null!;
        private Label _lblResumenTotal = null!;

        public FrmEgresos()
        {
            // Verificación de acceso
            if (!Sesion.EsAdmin && !Sesion.TienePermiso("EgresosVer"))
            {
                MessageBox.Show(
                    "Tu rol (" + Sesion.Rol + ") no tiene permiso para ver el módulo de Egresos.",
                    "Acceso denegado", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                Load += (s, e) => Close();
                return;
            }

            Text = "Egresos — Compras, gastos y materia prima";
            Size = new Size(1180, 760);
            MinimumSize = new Size(960, 600);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = UITheme.Fondo;
            ConstruirUI();
            Load += (s, e) => RecargarTab();
        }

        private void ConstruirUI()
        {
            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4,
                BackColor = UITheme.Fondo
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 60f));   // header
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 60f));   // filtros
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));   // tabs
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 50f));   // footer

            root.Controls.Add(ConstruirHeader(),  0, 0);
            root.Controls.Add(ConstruirFiltros(), 0, 1);
            root.Controls.Add(ConstruirTabs(),    0, 2);
            root.Controls.Add(ConstruirFooter(),  0, 3);
            Controls.Add(root);
        }

        private Panel ConstruirHeader()
        {
            var p = new Panel { Dock = DockStyle.Fill, BackColor = UITheme.AzulOscuro };
            p.Controls.Add(new Label
            {
                Text = "  Egresos del negocio",
                Font = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor = Color.White, BackColor = UITheme.AzulOscuro,
                Dock = DockStyle.Fill, AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(20, 6, 0, 0)
            });
            p.Controls.Add(new Label
            {
                Text = "  Compras de materia prima · servicios · mantenimiento · oficina · cuentas por pagar",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Italic),
                ForeColor = Color.FromArgb(168, 177, 187), BackColor = UITheme.AzulOscuro,
                Dock = DockStyle.Bottom, Height = 22,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(20, 0, 0, 4)
            });
            return p;
        }

        private Panel ConstruirFiltros()
        {
            var p = new Panel { Dock = DockStyle.Fill, BackColor = UITheme.FondoCard };
            p.Paint += (s, e) => e.Graphics.DrawLine(new Pen(UITheme.Borde),
                0, p.Height - 1, p.Width, p.Height - 1);

            int x = 16, y = 16;
            p.Controls.Add(new Label { Text = "Desde:", Font = UITheme.Body,
                Location = new Point(x, y + 4), AutoSize = true });
            x += 50;
            _dtpDesde = new DateTimePicker { Location = new Point(x, y),
                Size = new Size(120, 28), Format = DateTimePickerFormat.Short,
                Value = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1) };
            p.Controls.Add(_dtpDesde);
            x += 130;

            p.Controls.Add(new Label { Text = "Hasta:", Font = UITheme.Body,
                Location = new Point(x, y + 4), AutoSize = true });
            x += 50;
            _dtpHasta = new DateTimePicker { Location = new Point(x, y),
                Size = new Size(120, 28), Format = DateTimePickerFormat.Short,
                Value = DateTime.Today };
            p.Controls.Add(_dtpHasta);
            x += 140;

            var btnRefr = new Button { Text = "↻ Aplicar filtro",
                Location = new Point(x, y - 2), Size = new Size(140, 32),
                FlatStyle = FlatStyle.Flat, BackColor = UITheme.Azul,
                ForeColor = Color.White, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand };
            btnRefr.FlatAppearance.BorderSize = 0;
            btnRefr.Click += (s, e) => RecargarTab();
            p.Controls.Add(btnRefr);
            x += 150;

            // Botón importar OCR (abre el portal web en el navegador)
            var btnOcr = new Button { Text = "↗ Importar OCR",
                FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(21, 101, 192),
                ForeColor = Color.White, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand, Size = new Size(150, 32),
                Anchor = AnchorStyles.Top | AnchorStyles.Right };
            btnOcr.Location = new Point(p.Width - 350, y - 2);
            btnOcr.FlatAppearance.BorderSize = 0;
            btnOcr.Click += (s, e) =>
            {
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName        = "https://localhost:5443/pages/egresos-nuevo.html",
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show("No se pudo abrir el portal: " + ex.Message,
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            };
            p.Controls.Add(btnOcr);
            p.Resize += (s, e) => btnOcr.Location = new Point(p.Width - 350, y - 2);

            // Botón nuevo gasto (manual)
            var btnNuevo = new Button { Text = "+ Nuevo egreso",
                FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(15, 110, 86),
                ForeColor = Color.White, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand, Size = new Size(160, 32),
                Anchor = AnchorStyles.Top | AnchorStyles.Right };
            btnNuevo.Location = new Point(p.Width - 180, y - 2);
            btnNuevo.FlatAppearance.BorderSize = 0;
            btnNuevo.Click += (s, e) => NuevoGasto();
            p.Controls.Add(btnNuevo);

            return p;
        }

        private TabControl ConstruirTabs()
        {
            _tabs = new TabControl { Dock = DockStyle.Fill, Font = UITheme.Body };
            _tabs.TabPages.Add(new TabPage("📊  Resumen")           { Tag = "resumen" });
            _tabs.TabPages.Add(new TabPage("📦  Materia Prima")     { Tag = "mp" });
            _tabs.TabPages.Add(new TabPage("💼  Gastos Operativos") { Tag = "op" });
            _tabs.TabPages.Add(new TabPage("⏰  Por Pagar (CxP)")   { Tag = "cxp" });
            if (Sesion.EsAdmin || Sesion.TienePermiso("EgresosCategorias"))
                _tabs.TabPages.Add(new TabPage("🏷️  Categorías")    { Tag = "cat" });
            _tabs.SelectedIndexChanged += (s, e) => RecargarTab();
            return _tabs;
        }

        private Panel ConstruirFooter()
        {
            // Barra inferior CLARA con buen contraste (estilo unificado)
            var p = new Panel { Dock = DockStyle.Fill, BackColor = UITheme.FondoCard };
            p.Paint += (s, e) =>
            {
                using var pen = new Pen(UITheme.Borde, 1f);
                e.Graphics.DrawLine(pen, 0, 0, p.Width, 0);
            };

            _lblResumenTotal = new Label
            {
                Text = "  Selecciona un período y aplica filtro.",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = UITheme.Texto,
                BackColor = UITheme.FondoCard,
                Dock = DockStyle.Fill, AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(20, 0, 0, 0)
            };

            var btnCerrar = new Button
            {
                Text = "Cerrar", FlatStyle = FlatStyle.Flat,
                BackColor = UITheme.Azul,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Cursor = Cursors.Hand, Width = 130, Height = 36,
                Margin = new Padding(0, 7, 16, 7)
            };
            btnCerrar.FlatAppearance.BorderSize = 0;
            btnCerrar.FlatAppearance.MouseOverBackColor = UITheme.AzulHover;
            btnCerrar.Click += (s, e) => Close();

            var flowR = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                FlowDirection = FlowDirection.RightToLeft,
                BackColor = UITheme.FondoCard,
                Width = 170,
                WrapContents = false,
                Padding = new Padding(0)
            };
            flowR.Controls.Add(btnCerrar);

            // Orden importante: Dock=Fill primero, Dock=Right al final
            // (WinForms docking procesa en orden inverso al z-index)
            p.Controls.Add(_lblResumenTotal);
            p.Controls.Add(flowR);
            return p;
        }

        // ── Recarga del tab activo ──────────────────────────────────────────
        private void RecargarTab()
        {
            try
            {
                var tab = _tabs.SelectedTab;
                if (tab == null) return;
                tab.Controls.Clear();

                var desde = _dtpDesde.Value.Date;
                var hasta = _dtpHasta.Value.Date;

                switch (tab.Tag as string)
                {
                    case "resumen": RenderResumen(tab, desde, hasta); break;
                    case "mp":      RenderLista(tab, desde, hasta, true,  null); break;
                    case "op":      RenderLista(tab, desde, hasta, false, null); break;
                    case "cxp":     RenderPorPagar(tab); break;
                    case "cat":     RenderCategorias(tab); break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Error: " + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ── Tab Resumen ─────────────────────────────────────────────────────
        private void RenderResumen(TabPage tab, DateTime d, DateTime h)
        {
            var totalUSD = DatabaseService.TotalEgresosUSDPorPeriodo(d, h);
            var porCat   = DatabaseService.ResumenEgresosPorCategoria(d, h);
            var pend     = DatabaseService.ObtenerEgresosPorPagar();

            var pnl = new Panel { Dock = DockStyle.Fill, BackColor = UITheme.FondoCard,
                Padding = new Padding(20) };

            var pnlKpi = new TableLayoutPanel { Dock = DockStyle.Top, Height = 100,
                ColumnCount = 4, RowCount = 1, BackColor = UITheme.FondoCard };
            for (int i = 0; i < 4; i++)
                pnlKpi.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            pnlKpi.Controls.Add(KpiCard("EGRESOS DEL PERÍODO",
                "$ " + totalUSD.ToString("N2"), UITheme.AzulOscuro), 0, 0);
            pnlKpi.Controls.Add(KpiCard("# DE EGRESOS",
                porCat.Sum(x => x.Cantidad).ToString(), UITheme.Azul), 1, 0);
            pnlKpi.Controls.Add(KpiCard("CATEGORÍAS USADAS",
                porCat.Count(x => x.TotalUSD > 0).ToString(), Color.FromArgb(15, 110, 86)), 2, 0);
            pnlKpi.Controls.Add(KpiCard("CxP PENDIENTES",
                pend.Count.ToString() + " · $" + pend.Sum(g => g.MontoUSD).ToString("N0"),
                Color.FromArgb(210, 153, 34)), 3, 0);
            pnl.Controls.Add(pnlKpi);

            // Lista por categoría con barras horizontales
            var pnlCat = new Panel { Dock = DockStyle.Fill, BackColor = UITheme.FondoCard,
                Padding = new Padding(0, 16, 0, 0), AutoScroll = true };
            int y = 0;
            decimal max = porCat.Count == 0 ? 1m : Math.Max(1m, porCat.Max(x => x.TotalUSD));
            pnlCat.Controls.Add(new Label
            {
                Text = "Egresos por categoría",
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = UITheme.AzulOscuro, AutoSize = true,
                Location = new Point(0, y)
            });
            y += 30;
            foreach (var (id, nombre, color, totalCat, cant) in porCat.Where(x => x.TotalUSD > 0))
            {
                var lbl = new Label
                {
                    Text = nombre, Font = UITheme.BodyBold,
                    Location = new Point(0, y), AutoSize = false, Size = new Size(220, 20)
                };
                var lblVal = new Label
                {
                    Text = "$ " + totalCat.ToString("N2") + "  (" + cant + ")",
                    Font = UITheme.Body, ForeColor = UITheme.TextoTenue,
                    Location = new Point(220, y), AutoSize = false, Size = new Size(200, 20)
                };
                var barTrack = new Panel
                {
                    BackColor = Color.FromArgb(230, 234, 240),
                    Location = new Point(440, y + 2), Size = new Size(380, 16)
                };
                var barFill = new Panel
                {
                    BackColor = ParseColor(color),
                    Location = new Point(0, 0),
                    Size = new Size((int)(380 * (double)(totalCat / max)), 16)
                };
                barTrack.Controls.Add(barFill);
                pnlCat.Controls.AddRange(new Control[] { lbl, lblVal, barTrack });
                y += 26;
            }
            pnl.Controls.Add(pnlCat);
            tab.Controls.Add(pnl);

            _lblResumenTotal.Text = "  Período " + d.ToString("dd/MM") + " → " +
                h.ToString("dd/MM") + " · Total: $ " + totalUSD.ToString("N2") +
                " · " + porCat.Sum(x => x.Cantidad) + " egreso(s)";
        }

        private Panel KpiCard(string titulo, string valor, Color colorBarra)
        {
            var p = new Panel { Dock = DockStyle.Fill, BackColor = UITheme.FondoCard,
                Margin = new Padding(0, 0, 12, 0), Padding = new Padding(8) };
            p.Paint += (s, e) =>
            {
                using var pen = new Pen(UITheme.Borde, 1f);
                e.Graphics.DrawRectangle(pen, 0, 0, p.Width - 1, p.Height - 1);
            };
            var bar = new Panel { Dock = DockStyle.Left, Width = 4, BackColor = colorBarra };
            p.Controls.Add(new Label
            {
                Text = "  " + valor,
                Font = new Font("Segoe UI", 16f, FontStyle.Bold),
                ForeColor = UITheme.Texto, Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            });
            p.Controls.Add(new Label
            {
                Text = "  " + titulo,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = UITheme.TextoTenue, Dock = DockStyle.Top, Height = 20,
                TextAlign = ContentAlignment.MiddleLeft
            });
            p.Controls.Add(bar);
            return p;
        }

        // ── Tab Materia Prima / Gastos Operativos ───────────────────────────
        private void RenderLista(TabPage tab, DateTime d, DateTime h, bool soloMP, string? estado)
        {
            var lista = DatabaseService.ObtenerEgresosPorPeriodo(d, h, null, estado, soloMP);
            // Filtrar SOLO materia prima vs SOLO operativos según el flag
            var cats = DatabaseService.ObtenerCategoriasGasto();
            var idsMP = cats.Where(c => c.EsMateriaPrima).Select(c => c.Id).ToHashSet();
            lista = soloMP
                ? lista.Where(g => idsMP.Contains(g.IdCategoria)).ToList()
                : lista.Where(g => !idsMP.Contains(g.IdCategoria)).ToList();

            var dgv = new DataGridView
            {
                Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false,
                AllowUserToResizeRows = false, RowHeadersVisible = false,
                BackgroundColor = UITheme.FondoCard, BorderStyle = BorderStyle.None,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false
            };
            UITheme.EstilizarGrid(dgv);
            dgv.DataSource = lista.Select(g => new {
                g.Id,
                Fecha     = g.Fecha.ToString("dd/MM/yyyy"),
                Categoria = g.NombreCategoria,
                g.Concepto,
                g.Proveedor,
                Pago      = TipoTasaCorta(g.TipoTasa),
                Original  = (g.MonedaOriginal == "USD" ? "$ " : "Bs ") + g.MontoOriginal.ToString("N2"),
                USD       = "$ " + g.MontoUSD.ToString("N2"),
                Estado    = g.Estado
            }).ToList();
            if (dgv.Columns.Count > 0) dgv.Columns[0].Visible = false;
            dgv.DoubleClick += (s, e) =>
            {
                if (dgv.CurrentRow == null) return;
                int id = (int)dgv.CurrentRow.Cells["Id"].Value;
                EditarGasto(id);
            };
            tab.Controls.Add(dgv);

            decimal totalUSD = lista.Sum(g => g.MontoUSD);
            _lblResumenTotal.Text = "  " + lista.Count + " egresos · Total: $ " +
                totalUSD.ToString("N2") + " (" + (soloMP ? "Materia Prima" : "Gastos Operativos") +
                ") · doble click para editar";
        }

        // ── Tab Por Pagar ───────────────────────────────────────────────────
        private void RenderPorPagar(TabPage tab)
        {
            var lista = DatabaseService.ObtenerEgresosPorPagar();
            var dgv = new DataGridView
            {
                Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false,
                AllowUserToResizeRows = false, RowHeadersVisible = false,
                BackgroundColor = UITheme.FondoCard, BorderStyle = BorderStyle.None,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false
            };
            UITheme.EstilizarGrid(dgv);
            DateTime hoy = DateTime.Today;
            dgv.DataSource = lista.Select(g => new {
                g.Id,
                Fecha     = g.Fecha.ToString("dd/MM/yyyy"),
                Categoria = g.NombreCategoria,
                g.Concepto,
                g.Proveedor,
                Vence     = g.FechaVencimiento?.ToString("dd/MM/yyyy") ?? "—",
                Dias      = g.FechaVencimiento.HasValue
                            ? ((g.FechaVencimiento.Value.Date - hoy).Days + " d")
                            : "—",
                USD       = "$ " + g.MontoUSD.ToString("N2")
            }).ToList();
            if (dgv.Columns.Count > 0) dgv.Columns[0].Visible = false;

            // Color a filas vencidas/próximas a vencer
            dgv.RowPrePaint += (s, e) =>
            {
                if (e.RowIndex < 0 || e.RowIndex >= lista.Count) return;
                var g = lista[e.RowIndex];
                if (g.FechaVencimiento.HasValue)
                {
                    int dias = (g.FechaVencimiento.Value.Date - hoy).Days;
                    if (dias < 0) dgv.Rows[e.RowIndex].DefaultCellStyle.BackColor = Color.FromArgb(253, 235, 235);
                    else if (dias <= 7) dgv.Rows[e.RowIndex].DefaultCellStyle.BackColor = Color.FromArgb(255, 248, 220);
                }
            };

            // Doble click para marcar pagado
            dgv.DoubleClick += (s, e) =>
            {
                if (dgv.CurrentRow == null) return;
                int id = (int)dgv.CurrentRow.Cells["Id"].Value;
                if (MessageBox.Show(this,
                    "¿Marcar este egreso como PAGADO hoy (" + hoy.ToString("dd/MM/yyyy") + ")?",
                    "Confirmar pago",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
                try
                {
                    DatabaseService.MarcarEgresoPagado(id, hoy);
                    RecargarTab();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, ex.Message, "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            tab.Controls.Add(dgv);

            _lblResumenTotal.Text = "  " + lista.Count + " cuentas por pagar · Total: $ " +
                lista.Sum(g => g.MontoUSD).ToString("N2") + " · doble click → marcar pagado";
        }

        // ── Tab Categorías ──────────────────────────────────────────────────
        private void RenderCategorias(TabPage tab)
        {
            var pnl = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0),
                BackColor = UITheme.FondoCard };

            var dgv = new DataGridView
            {
                Dock = DockStyle.Fill, AllowUserToAddRows = false,
                AllowUserToResizeRows = false, RowHeadersVisible = false,
                BackgroundColor = UITheme.FondoCard, BorderStyle = BorderStyle.None,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false
            };
            UITheme.EstilizarGrid(dgv);
            var cats = DatabaseService.ObtenerCategoriasGasto(incluirInactivas: true);
            dgv.DataSource = cats.Select(c => new {
                c.Id, c.Nombre, c.Color, c.Orden,
                MateriaPrima = c.EsMateriaPrima ? "Sí" : "",
                Activa = c.Activa ? "Sí" : "INACTIVA"
            }).ToList();
            if (dgv.Columns.Count > 0) dgv.Columns[0].Visible = false;
            pnl.Controls.Add(dgv);

            // Botonera arriba
            var btnAdd = new Button { Text = "+ Nueva categoría",
                FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(15, 110, 86),
                ForeColor = Color.White, Font = UITheme.BodyBold, Cursor = Cursors.Hand,
                Dock = DockStyle.Top, Height = 36 };
            btnAdd.FlatAppearance.BorderSize = 0;
            btnAdd.Click += (s, e) =>
            {
                using var f = new FrmCategoriaEditor();
                if (f.ShowDialog(this) == DialogResult.OK) RecargarTab();
            };
            pnl.Controls.Add(btnAdd);

            dgv.DoubleClick += (s, e) =>
            {
                if (dgv.CurrentRow == null) return;
                int id = (int)dgv.CurrentRow.Cells["Id"].Value;
                var cat = cats.FirstOrDefault(c => c.Id == id);
                if (cat == null) return;
                using var f = new FrmCategoriaEditor(cat);
                if (f.ShowDialog(this) == DialogResult.OK) RecargarTab();
            };

            tab.Controls.Add(pnl);
            _lblResumenTotal.Text = "  " + cats.Count + " categorías (" +
                cats.Count(c => c.Activa) + " activas) · doble click para editar";
        }

        // ── Acciones ────────────────────────────────────────────────────────
        private void NuevoGasto()
        {
            try
            {
                using var f = new FrmGastoEditor();
                if (f.ShowDialog(this) == DialogResult.OK) RecargarTab();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void EditarGasto(int id)
        {
            try
            {
                var g = DatabaseService.ObtenerEgresosPorPeriodo(
                    DateTime.Today.AddYears(-10), DateTime.Today.AddYears(1))
                    .FirstOrDefault(x => x.Id == id);
                if (g == null) return;
                using var f = new FrmGastoEditor(g);
                if (f.ShowDialog(this) == DialogResult.OK) RecargarTab();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ── Helpers ─────────────────────────────────────────────────────────
        private static string TipoTasaCorta(string t) => t switch
        {
            "USD_DIRECTO"   => "USD",
            "BCV"           => "Bs(BCV)",
            "PARALELO"      => "Bs(Paralelo)",
            "PERSONALIZADA" => "Bs(custom)",
            _ => t
        };

        private static Color ParseColor(string hex)
        {
            try
            {
                if (hex.StartsWith("#") && hex.Length == 7)
                {
                    int r = Convert.ToInt32(hex.Substring(1, 2), 16);
                    int g = Convert.ToInt32(hex.Substring(3, 2), 16);
                    int b = Convert.ToInt32(hex.Substring(5, 2), 16);
                    return Color.FromArgb(r, g, b);
                }
            }
            catch { }
            return UITheme.Azul;
        }
    }

    // ════════════════════════════════════════════════════════════════════════
    //  FrmCategoriaEditor — diálogo simple para CRUD de CategoriasGasto
    // ════════════════════════════════════════════════════════════════════════
    public class FrmCategoriaEditor : FormBase
    {
        private CategoriaGasto _cat;
        private TextBox _txtNombre = null!, _txtColor = null!;
        private NumericUpDown _numOrden = null!;
        private CheckBox _chkActiva = null!, _chkMP = null!;

        public FrmCategoriaEditor(CategoriaGasto? existente = null)
        {
            _cat = existente ?? new CategoriaGasto();
            Text = _cat.Id == 0 ? "Nueva categoría de gasto" : "Editar categoría";
            ClientSize = new Size(440, 320);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false; MinimizeBox = false;
            BackColor = UITheme.Fondo;
            ConstruirUI();
        }

        private void ConstruirUI()
        {
            int y = 16;
            void Lbl(string txt) {
                Controls.Add(new Label { Text = txt, Font = UITheme.BodyBold,
                    ForeColor = UITheme.AzulOscuro,
                    Location = new Point(20, y), AutoSize = true });
                y += 22;
            }
            Lbl("Nombre");
            _txtNombre = new TextBox { Location = new Point(20, y),
                Size = new Size(400, 28), Font = UITheme.Body, Text = _cat.Nombre };
            Controls.Add(_txtNombre); y += 38;

            Lbl("Color (hex, ej: #1A4A8A)");
            _txtColor = new TextBox { Location = new Point(20, y),
                Size = new Size(400, 28), Font = UITheme.Mono, Text = _cat.Color };
            Controls.Add(_txtColor); y += 38;

            Lbl("Orden de aparición");
            _numOrden = new NumericUpDown { Location = new Point(20, y),
                Size = new Size(120, 28), Font = UITheme.Body,
                Minimum = 0, Maximum = 999, Value = _cat.Orden };
            Controls.Add(_numOrden); y += 38;

            _chkActiva = new CheckBox { Text = "Activa (visible al crear gastos)",
                Font = UITheme.Body, AutoSize = true,
                Location = new Point(20, y), Checked = _cat.Activa };
            Controls.Add(_chkActiva); y += 26;
            _chkMP = new CheckBox { Text = "Es categoría de Materia Prima (insumos del producto)",
                Font = UITheme.Body, AutoSize = true,
                Location = new Point(20, y), Checked = _cat.EsMateriaPrima };
            Controls.Add(_chkMP);

            var btnG = new Button { Text = "Guardar", Location = new Point(220, 270),
                Size = new Size(100, 34), FlatStyle = FlatStyle.Flat,
                BackColor = UITheme.Azul, ForeColor = Color.White,
                Font = UITheme.BodyBold, Cursor = Cursors.Hand };
            btnG.FlatAppearance.BorderSize = 0;
            btnG.Click += (s, e) => Guardar();
            var btnC = new Button { Text = "Cancelar", Location = new Point(330, 270),
                Size = new Size(90, 34), FlatStyle = FlatStyle.Flat,
                BackColor = UITheme.FondoCard, ForeColor = UITheme.Texto,
                Font = UITheme.Body, Cursor = Cursors.Hand };
            btnC.FlatAppearance.BorderSize = 1;
            btnC.FlatAppearance.BorderColor = UITheme.Borde;
            btnC.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            Controls.AddRange(new Control[] { btnG, btnC });
        }

        private void Guardar()
        {
            if (string.IsNullOrWhiteSpace(_txtNombre.Text))
            {
                MessageBox.Show(this, "El nombre es requerido.",
                    "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            try
            {
                _cat.Nombre = _txtNombre.Text.Trim();
                _cat.Color  = string.IsNullOrWhiteSpace(_txtColor.Text) ? "#1A4A8A" : _txtColor.Text.Trim();
                _cat.Orden  = (int)_numOrden.Value;
                _cat.Activa = _chkActiva.Checked;
                _cat.EsMateriaPrima = _chkMP.Checked;
                DatabaseService.GuardarCategoriaGasto(_cat);
                DialogResult = DialogResult.OK; Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
