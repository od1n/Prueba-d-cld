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

            // Botón importar OCR — llama al digitalizador directamente (sin navegador
            // ni API web) y abre el editor de egreso ya precargado con los datos.
            var btnOcr = new Button { Text = "📷 Importar OCR",
                FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(21, 101, 192),
                ForeColor = Color.White, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand, Size = new Size(150, 32) };
            btnOcr.FlatAppearance.BorderSize = 0;
            btnOcr.Click += async (s, e) => await ImportarPorOcr();
            p.Controls.Add(btnOcr);

            // Botón nuevo gasto (manual)
            var btnNuevo = new Button { Text = "+ Nuevo egreso",
                FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(15, 110, 86),
                ForeColor = Color.White, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand, Size = new Size(160, 32) };
            btnNuevo.FlatAppearance.BorderSize = 0;
            btnNuevo.Click += (s, e) => NuevoGasto();
            p.Controls.Add(btnNuevo);

            // Los dos botones de la derecha se colocan juntos con un espacio fijo
            // y se reubican al cambiar el tamaño, para que NUNCA se superpongan.
            void PosicionarBotonesDerecha()
            {
                btnNuevo.Location = new Point(Math.Max(x, p.Width - btnNuevo.Width - 16), y - 2);
                btnOcr.Location   = new Point(btnNuevo.Left - btnOcr.Width - 12,          y - 2);
            }
            PosicionarBotonesDerecha();
            p.Resize += (s, e) => PosicionarBotonesDerecha();

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
            var pend     = DatabaseService.ObtenerCuentasPagar()
                             .Where(c => c.Estado != "Pagada" && c.Saldo > 0.01m).ToList();

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
                pend.Count.ToString() + " · $" + pend.Sum(c => c.Saldo).ToString("N0"),
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
                Pago      = g.PrecioPendiente ? "—" : TipoTasaCorta(g.TipoTasa),
                Original  = g.PrecioPendiente ? "—"
                             : (g.MonedaOriginal == "USD" ? "$ " : "Bs ") + g.MontoOriginal.ToString("N2"),
                USD       = g.PrecioPendiente ? "—" : "$ " + g.MontoUSD.ToString("N2"),
                Estado    = g.PrecioPendiente ? "PRECIO PENDIENTE" : g.Estado
            }).ToList();
            if (dgv.Columns.Count > 0) dgv.Columns[0].Visible = false;

            // Resaltar egresos con precio pendiente (naranja suave).
            dgv.RowPrePaint += (s, e) =>
            {
                if (e.RowIndex < 0 || e.RowIndex >= lista.Count) return;
                if (lista[e.RowIndex].PrecioPendiente)
                    dgv.Rows[e.RowIndex].DefaultCellStyle.BackColor = Color.FromArgb(255, 244, 224);
            };

            dgv.DoubleClick += (s, e) =>
            {
                if (dgv.CurrentRow == null) return;
                int id = (int)dgv.CurrentRow.Cells["Id"].Value;
                var g = lista.FirstOrDefault(x => x.Id == id);
                if (g != null && g.PrecioPendiente) CompletarPrecio(g);
                else EditarGasto(id);
            };
            tab.Controls.Add(dgv);

            decimal totalUSD = lista.Sum(g => g.MontoUSD);
            int pendPrecio = lista.Count(g => g.PrecioPendiente);
            _lblResumenTotal.Text = "  " + lista.Count + " egresos · Total: $ " +
                totalUSD.ToString("N2") + " (" + (soloMP ? "Materia Prima" : "Gastos Operativos") +
                ")" + (pendPrecio > 0 ? " · " + pendPrecio + " con PRECIO PENDIENTE (doble clic para completar)"
                                       : " · doble click para editar");
        }

        // ── Tab Por Pagar ───────────────────────────────────────────────────
        private void RenderPorPagar(TabPage tab)
        {
            // Unificado: la lista de "por pagar" es la de Cuentas por Pagar (incluye
            // las generadas automáticamente desde egresos a crédito y las manuales).
            var lista = DatabaseService.ObtenerCuentasPagar()
                            .Where(c => c.Estado != "Pagada" && c.Saldo > 0.01m)
                            .OrderBy(c => c.FechaVence).ToList();
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
            dgv.DataSource = lista.Select(c => new {
                c.Id,
                c.Proveedor,
                c.Concepto,
                Referencia = c.NumFactura,
                Emision = c.FechaEmision.ToString("dd/MM/yyyy"),
                Vence   = c.FechaVence.ToString("dd/MM/yyyy"),
                Dias    = (c.FechaVence.Date - hoy).Days + " d",
                Monto   = "$ " + c.Monto.ToString("N2"),
                Pagado  = "$ " + c.Pagado.ToString("N2"),
                Saldo   = "$ " + c.Saldo.ToString("N2"),
                c.Estado
            }).ToList();
            if (dgv.Columns.Contains("Id")) dgv.Columns["Id"].Visible = false;

            // Color a filas vencidas/próximas a vencer
            dgv.RowPrePaint += (s, e) =>
            {
                if (e.RowIndex < 0 || e.RowIndex >= lista.Count) return;
                var c = lista[e.RowIndex];
                int dias = (c.FechaVence.Date - hoy).Days;
                if (dias < 0) dgv.Rows[e.RowIndex].DefaultCellStyle.BackColor = Color.FromArgb(253, 235, 235);
                else if (dias <= 7) dgv.Rows[e.RowIndex].DefaultCellStyle.BackColor = Color.FromArgb(255, 248, 220);
            };

            // Doble clic → registrar un pago (parcial o total) de la cuenta
            dgv.DoubleClick += (s, e) =>
            {
                if (dgv.CurrentRow == null) return;
                int id = (int)dgv.CurrentRow.Cells["Id"].Value;
                var c = lista.FirstOrDefault(x => x.Id == id);
                if (c == null) return;
                try
                {
                    using var dlg = new DlgPagoCxP(c);
                    if (dlg.ShowDialog(this) == DialogResult.OK)
                    {
                        DatabaseService.RegistrarPagoCxP(c.Id, dlg.Resultado);
                        RecargarTab();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, ex.Message, "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            tab.Controls.Add(dgv);

            _lblResumenTotal.Text = "  " + lista.Count + " cuentas por pagar · Saldo total: $ " +
                lista.Sum(c => c.Saldo).ToString("N2") + " · doble clic → registrar pago";
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

        // ── Importar por OCR (imagen/PDF) — nativo, vía digitalizador ────────
        private async System.Threading.Tasks.Task ImportarPorOcr()
        {
            using var ofd = new OpenFileDialog
            {
                Title  = "Selecciona la factura o nota de entrega (imagen o PDF)",
                Filter = "Documentos (imagen o PDF)|*.jpg;*.jpeg;*.png;*.webp;*.pdf|" +
                         "Imágenes|*.jpg;*.jpeg;*.png;*.webp|PDF|*.pdf|Todos|*.*"
            };
            if (ofd.ShowDialog(this) != DialogResult.OK) return;
            string ruta = ofd.FileName;

            Cursor = Cursors.WaitCursor;
            try
            {
                if (!await OcrDigitalizadorService.ServicioActivoAsync())
                {
                    Cursor = Cursors.Default;
                    MessageBox.Show(this,
                        "El digitalizador no responde en " + OcrDigitalizadorService.UrlBase + ".\n\n" +
                        "Inícialo con run.bat en la carpeta 'digitalizador' (o revisa la variable SG_DIG_URL) " +
                        "y vuelve a intentar.",
                        "Digitalizador no disponible", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var r = await OcrDigitalizadorService.ExtraerAsync(ruta);
                Cursor = Cursors.Default;

                if (!r.Encontrado)
                {
                    var seguir = MessageBox.Show(this,
                        "El modelo no reconoció datos claros en el documento.\n" +
                        "¿Abrir el formulario igual para completarlo a mano?",
                        "Sin datos legibles", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (seguir != DialogResult.Yes) return;
                }

                var eg = ConstruirEgresoDesdeOcr(r, ruta);
                using var f = new FrmGastoEditor(eg);
                if (f.ShowDialog(this) == DialogResult.OK) RecargarTab();
            }
            catch (Exception ex)
            {
                Cursor = Cursors.Default;
                MessageBox.Show(this, ex.Message, "Error de OCR",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally { Cursor = Cursors.Default; }
        }

        private static Egreso ConstruirEgresoDesdeOcr(OcrResultado r, string rutaComprobante)
        {
            bool esBs = r.Moneda == "BS";

            // Construir una línea por producto de la factura, emparejando con el
            // catálogo (6200B, 2706, …). Las que no matchean quedan como texto libre.
            var detalles = new List<DetalleEgreso>();
            bool algunMP = false;
            foreach (var it in r.Items)
            {
                var ins = DatabaseService.BuscarInsumoOcr(it.Descripcion);
                decimal precio = it.PrecioUnitario > 0 ? it.PrecioUnitario
                                 : (it.Cantidad > 0 && it.Total > 0 ? Math.Round(it.Total / it.Cantidad, 4) : 0m);
                if (ins != null)
                {
                    algunMP = true;
                    detalles.Add(new DetalleEgreso
                    {
                        IdArticulo     = ins.Id,
                        CodigoInterno  = ins.CodigoInterno ?? "",
                        Descripcion    = ins.Descripcion ?? it.Descripcion,
                        TipoInsumo     = ins.TipoInsumo ?? "Materia prima",
                        EsMateriaPrima = true,
                        Cantidad       = it.Cantidad,
                        PrecioUnitario = precio
                    });
                }
                else
                {
                    detalles.Add(new DetalleEgreso
                    {
                        IdArticulo     = 0,
                        Descripcion    = it.Descripcion,
                        EsMateriaPrima = false,
                        Cantidad       = it.Cantidad,
                        PrecioUnitario = precio
                    });
                }
            }

            // Precio pendiente si la nota no trae montos (ni total ni precios de línea).
            bool sinPrecios = r.Total <= 0m && detalles.All(d => d.PrecioUnitario <= 0m);

            var eg = new Egreso
            {
                Fecha           = OcrDigitalizadorService.ParseFecha(r.Fecha) ?? DateTime.Today,
                Proveedor       = r.Proveedor ?? "",
                Concepto        = !string.IsNullOrWhiteSpace(r.Proveedor)
                                    ? "Compra a " + r.Proveedor
                                    : (!string.IsNullOrWhiteSpace(r.NumeroFactura)
                                        ? "Compra " + r.NumeroFactura : "Compra"),
                MonedaOriginal  = esBs ? "BS" : "USD",
                TipoTasa        = esBs ? "BCV" : "USD_DIRECTO",
                MontoOriginal   = r.Total,
                RutaComprobante = rutaComprobante,
                PrecioPendiente = sinPrecios,
                Observaciones   = ObsDesdeOcr(r),
                Detalles        = detalles
            };

            // Si hay al menos un insumo reconocido, preseleccionar una categoría de
            // materia prima para que el editor muestre el panel de líneas.
            if (algunMP)
            {
                var catMP = DatabaseService.ObtenerCategoriasGasto().FirstOrDefault(c => c.EsMateriaPrima);
                if (catMP != null) eg.IdCategoria = catMP.Id;
            }
            return eg;
        }

        private static string ObsDesdeOcr(OcrResultado r)
        {
            var sb = new System.Text.StringBuilder();
            if (!string.IsNullOrWhiteSpace(r.NumeroFactura)) sb.Append("Doc: ").Append(r.NumeroFactura);
            if (r.Iva > 0) { if (sb.Length > 0) sb.Append(" | "); sb.Append("IVA: ").Append(r.Iva.ToString("N2")); }
            if (r.Items != null && r.Items.Count > 0)
            {
                if (sb.Length > 0) sb.Append('\n');
                sb.Append("Líneas OCR:");
                foreach (var it in r.Items)
                    sb.Append("\n • ").Append(it.Descripcion)
                      .Append("  x").Append(it.Cantidad.ToString("0.##"))
                      .Append(it.PrecioUnitario > 0 ? " @ " + it.PrecioUnitario.ToString("0.####") : "");
            }
            return sb.ToString();
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

        // ── Completar precio de una nota de entrega (Fase 2) ────────────────
        private void CompletarPrecio(Egreso g)
        {
            try
            {
                using var dlg = new DlgCompletarPrecio(g);
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                if (dlg.EsMultilinea)
                    DatabaseService.CompletarPrecioEgresoLineas(
                        idEgreso: g.Id,
                        lineas: dlg.Lineas,
                        ivaPct: dlg.IvaPct,
                        monedaOriginal: dlg.Moneda,
                        tipoTasa: dlg.TipoTasa,
                        tasaUsada: dlg.Tasa,
                        pagadoUSD: dlg.PagadoUSD,
                        vence: dlg.Vence);
                else
                    DatabaseService.CompletarPrecioEgreso(
                        idEgreso: g.Id,
                        montoOriginal: dlg.MontoOriginal,
                        monedaOriginal: dlg.Moneda,
                        tipoTasa: dlg.TipoTasa,
                        tasaUsada: dlg.Tasa,
                        pagadoUSD: dlg.PagadoUSD,
                        vence: dlg.Vence);
                MessageBox.Show(this,
                    "Precio completado. Se actualizaron los montos, el Kardex y la Cuenta por Pagar.",
                    "Listo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                RecargarTab();
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
    //  DlgCompletarPrecio — completa el precio de una nota de entrega.
    //  Multilínea: si el egreso tiene líneas (DetallesGasto), se ingresa el
    //  precio unitario por línea; si no, se ingresa una base imponible única.
    // ════════════════════════════════════════════════════════════════════════
    public class DlgCompletarPrecio : Form
    {
        public bool               EsMultilinea { get; private set; }
        public List<DetalleEgreso> Lineas      { get; private set; } = new();
        public decimal            IvaPct       { get; private set; } = 16m;
        public decimal   MontoOriginal { get; private set; }
        public string    Moneda        { get; private set; } = "USD";
        public string    TipoTasa      { get; private set; } = "USD_DIRECTO";
        public decimal   Tasa          { get; private set; }
        public decimal   PagadoUSD     { get; private set; }
        public DateTime? Vence         { get; private set; }

        private ComboBox _cmbMoneda = null!, _cmbTipoTasa = null!;
        private NumericUpDown _numTasa = null!, _numBase = null!, _numIva = null!, _numPagado = null!;
        private Label _lblBase = null!, _lblTotal = null!;
        private CheckBox _chkCredito = null!;
        private DateTimePicker _dtpVence = null!;
        private DataGridView _grid = null!;
        private readonly List<DetalleEgreso> _lineas;
        private decimal _tasaBCV, _tasaParalelo;

        public DlgCompletarPrecio(Egreso g)
        {
            _lineas = DatabaseService.ObtenerDetallesEgreso(g.Id);
            EsMultilinea = _lineas.Count > 0;

            Text = "Completar precio — " + g.Concepto;
            ClientSize = EsMultilinea ? new Size(560, 520) : new Size(460, 430);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.Sizable;
            MinimumSize = EsMultilinea ? new Size(576, 559) : new Size(476, 469);
            MaximizeBox = false; MinimizeBox = false;
            BackColor = Color.FromArgb(245, 247, 250);
            Font = new Font("Segoe UI", 9.5f);

            try
            {
                var t = DatabaseService.ObtenerTasaPorFecha(g.Fecha);
                if (t != null) { _tasaBCV = t.TasaBCV; _tasaParalelo = t.TasaParalelo; }
            }
            catch { }

            var hdr = new Panel { Dock = DockStyle.Top, Height = 52, BackColor = Color.FromArgb(150, 90, 0) };
            hdr.Controls.Add(new Label
            {
                Text = "Nota de entrega #" + g.Id + "  ·  " + g.Proveedor,
                ForeColor = Color.White, Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter
            });
            Controls.Add(hdr);

            int panelW = EsMultilinea ? 520 : 420;
            var pnl = new Panel { Location = new Point(20, 64), Size = new Size(panelW, EsMultilinea ? 392 : 300) };
            int y = 0;
            Label L(string t) { var l = new Label { Text = t, Location = new Point(0, y + 4), AutoSize = true, ForeColor = Color.FromArgb(44, 62, 80) }; return l; }

            int colX = EsMultilinea ? 130 : 150;   // x de los campos

            // ── Multilínea: grilla de líneas con precio unitario editable ────
            if (EsMultilinea)
            {
                pnl.Controls.Add(new Label
                {
                    Text = "Ingresa el precio unitario (USD) de cada línea:",
                    Location = new Point(0, y), AutoSize = true, ForeColor = Color.FromArgb(44, 62, 80),
                    Font = new Font("Segoe UI", 9f, FontStyle.Bold)
                });
                y += 22;
                _grid = new DataGridView
                {
                    Location = new Point(0, y), Size = new Size(panelW, 120),
                    AllowUserToAddRows = false, AllowUserToResizeRows = false, RowHeadersVisible = false,
                    BackgroundColor = Color.White, BorderStyle = BorderStyle.FixedSingle, MultiSelect = false,
                    AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, Font = new Font("Segoe UI", 9f),
                    EditMode = DataGridViewEditMode.EditOnEnter
                };
                var cDesc = new DataGridViewTextBoxColumn { HeaderText = "Descripción", FillWeight = 46, ReadOnly = true };
                var cCant = new DataGridViewTextBoxColumn { HeaderText = "Cant.",       FillWeight = 16, ReadOnly = true,
                    DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight } };
                var cPu   = new DataGridViewTextBoxColumn { HeaderText = "P. Unit (USD)", FillWeight = 20,
                    DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight } };
                var cSub  = new DataGridViewTextBoxColumn { HeaderText = "Subtotal",    FillWeight = 18, ReadOnly = true,
                    DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight } };
                _grid.Columns.AddRange(new DataGridViewColumn[] { cDesc, cCant, cPu, cSub });
                foreach (var l in _lineas)
                    _grid.Rows.Add(l.Descripcion, l.Cantidad.ToString("N2"),
                        l.PrecioUnitario > 0 ? l.PrecioUnitario.ToString("0.####") : "0",
                        "$ " + l.Subtotal.ToString("N2"));
                pnl.Controls.Add(_grid);
                y += 128;
            }
            else
            {
                _lblBase = L("Base imponible:");
                pnl.Controls.Add(_lblBase);
                _numBase = new NumericUpDown { Location = new Point(colX, y), Size = new Size(panelW - colX, 24),
                    Maximum = 99_999_999m, DecimalPlaces = 2, ThousandsSeparator = true };
                pnl.Controls.Add(_numBase); y += 34;
            }

            pnl.Controls.Add(L("IVA %:"));
            _numIva = new NumericUpDown { Location = new Point(colX, y), Size = new Size(120, 24),
                Maximum = 100m, DecimalPlaces = 2, Value = 16m };
            pnl.Controls.Add(_numIva); y += 34;

            pnl.Controls.Add(L("Moneda:"));
            _cmbMoneda = new ComboBox { Location = new Point(colX, y), Size = new Size(panelW - colX, 24), DropDownStyle = ComboBoxStyle.DropDownList };
            _cmbMoneda.Items.AddRange(new object[] { "USD — Dólares", "BS — Bolívares" });
            _cmbMoneda.SelectedIndex = 0;
            pnl.Controls.Add(_cmbMoneda); y += 34;

            pnl.Controls.Add(L("Tipo de tasa:"));
            _cmbTipoTasa = new ComboBox { Location = new Point(colX, y), Size = new Size(panelW - colX, 24), DropDownStyle = ComboBoxStyle.DropDownList };
            _cmbTipoTasa.Items.AddRange(new object[] { "USD directo", "BCV", "Paralelo", "Personalizada" });
            _cmbTipoTasa.SelectedIndex = 0;
            pnl.Controls.Add(_cmbTipoTasa); y += 34;

            pnl.Controls.Add(L("Tasa Bs/USD:"));
            _numTasa = new NumericUpDown { Location = new Point(colX, y), Size = new Size(panelW - colX, 24),
                Maximum = 1_000_000m, DecimalPlaces = 4, Enabled = false };
            pnl.Controls.Add(_numTasa); y += 34;

            pnl.Controls.Add(L("Total:"));
            _lblTotal = new Label { Location = new Point(colX, y + 2), AutoSize = true,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold), ForeColor = Color.FromArgb(15, 110, 86),
                Text = "0,00" };
            pnl.Controls.Add(_lblTotal); y += 34;

            _chkCredito = new CheckBox { Text = "A crédito (abono parcial)", Location = new Point(0, y),
                AutoSize = true, ForeColor = Color.FromArgb(44, 62, 80) };
            pnl.Controls.Add(_chkCredito); y += 28;

            pnl.Controls.Add(L("Abonado (USD):"));
            _numPagado = new NumericUpDown { Location = new Point(colX, y), Size = new Size(120, 24),
                Maximum = 99_999_999m, DecimalPlaces = 2, ThousandsSeparator = true, Enabled = false };
            pnl.Controls.Add(_numPagado);
            _dtpVence = new DateTimePicker { Location = new Point(colX + 130, y), Size = new Size(panelW - colX - 130, 24),
                Format = DateTimePickerFormat.Short, Value = g.FechaVencimiento ?? DateTime.Today.AddDays(30),
                Enabled = false };
            pnl.Controls.Add(_dtpVence); y += 34;

            Controls.Add(pnl);

            int btnY = (EsMultilinea ? 520 : 430) - 44;
            var btnCancelar = new Button { Text = "Cancelar", Size = new Size(95, 32), Location = new Point((EsMultilinea ? 560 : 460) - 210, btnY),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(149, 165, 166), ForeColor = Color.White,
                FlatAppearance = { BorderSize = 0 }, DialogResult = DialogResult.Cancel };
            var btnOk = new Button { Text = "Completar", Size = new Size(95, 32), Location = new Point((EsMultilinea ? 560 : 460) - 110, btnY),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(39, 174, 96), ForeColor = Color.White,
                FlatAppearance = { BorderSize = 0 } };
            Controls.AddRange(new Control[] { btnCancelar, btnOk });
            // Asegurar que los botones queden por encima del panel de campos.
            btnCancelar.BringToFront(); btnOk.BringToFront();
            AcceptButton = btnOk; CancelButton = btnCancelar;

            decimal BaseActual()
            {
                if (EsMultilinea) return _lineas.Sum(l => l.Subtotal);
                return _numBase?.Value ?? 0m;
            }
            void Recalc()
            {
                decimal total = Math.Round(BaseActual() * (1m + _numIva.Value / 100m), 2);
                _lblTotal.Text = (_cmbMoneda.SelectedIndex == 1 ? "Bs " : "$ ") + total.ToString("N2");
            }
            void AplicarTipoTasa()
            {
                int i = _cmbTipoTasa.SelectedIndex;
                _numTasa.Enabled = i != 0;
                if (i == 1 && _tasaBCV > 0) _numTasa.Value = Math.Min(_numTasa.Maximum, _tasaBCV);
                else if (i == 2 && _tasaParalelo > 0) _numTasa.Value = Math.Min(_numTasa.Maximum, _tasaParalelo);
            }
            if (EsMultilinea)
            {
                _grid.CellEndEdit += (s, e) =>
                {
                    if (e.RowIndex < 0 || e.RowIndex >= _lineas.Count || e.ColumnIndex != 2) return;
                    decimal pu = 0m;
                    decimal.TryParse((_grid.Rows[e.RowIndex].Cells[2].Value ?? "0").ToString(),
                        System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.CurrentCulture, out pu);
                    if (pu < 0) pu = 0;
                    _lineas[e.RowIndex].PrecioUnitario = pu;
                    _grid.Rows[e.RowIndex].Cells[3].Value = "$ " + _lineas[e.RowIndex].Subtotal.ToString("N2");
                    Recalc();
                };
            }
            else { _numBase.ValueChanged += (s, e) => Recalc(); }
            _numIva.ValueChanged  += (s, e) => Recalc();
            _cmbMoneda.SelectedIndexChanged += (s, e) => Recalc();
            _cmbTipoTasa.SelectedIndexChanged += (s, e) => AplicarTipoTasa();
            _chkCredito.CheckedChanged += (s, e) =>
            {
                _numPagado.Enabled = _chkCredito.Checked;
                _dtpVence.Enabled  = _chkCredito.Checked;
            };
            Recalc();

            btnOk.Click += (s, e) =>
            {
                if (BaseActual() <= 0)
                {
                    MessageBox.Show(this, EsMultilinea
                        ? "Ingresa el precio de al menos una línea."
                        : "Ingresa la base imponible (mayor que cero).", "Validación",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                string[] tt = { "USD_DIRECTO", "BCV", "PARALELO", "PERSONALIZADA" };
                TipoTasa = tt[_cmbTipoTasa.SelectedIndex];
                Moneda   = _cmbMoneda.SelectedIndex == 1 ? "BS" : "USD";
                Tasa     = _numTasa.Value;
                if (TipoTasa != "USD_DIRECTO" && Tasa <= 0)
                {
                    MessageBox.Show(this, "La tasa debe ser mayor que cero para el tipo seleccionado.",
                        "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                IvaPct        = _numIva.Value;
                Lineas        = _lineas;
                MontoOriginal = Math.Round(BaseActual() * (1m + _numIva.Value / 100m), 2);
                if (_chkCredito.Checked)
                {
                    PagadoUSD = _numPagado.Value;
                    Vence     = _dtpVence.Value.Date;
                }
                else
                {
                    PagadoUSD = MontoOriginal;   // pago completo; el back-end lo topa al MontoUSD
                    Vence     = null;
                }
                DialogResult = DialogResult.OK;
            };
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
