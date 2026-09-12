using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Printing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SistemaGestion
{
    // ══════════════════════════════════════════════════════════════
    //  REPORTES
    // ══════════════════════════════════════════════════════════════
    public class FrmReportes : FormBase
    {
        // ── Helper async compartido por todos los tabs pesados ─────────────
        // Ejecuta `trabajo` en thread pool con WaitCursor + bloqueo de los
        // controles indicados. Garantiza que el UI nunca quede congelado y
        // que los botones se rehabilitan incluso si el trabajo lanza.
        private async Task<T> EjecutarConCarga<T>(Func<T> trabajo, params Control[] aBloquear)
        {
            this.UseWaitCursor = true;
            Cursor.Current     = Cursors.WaitCursor;
            foreach (var c in aBloquear) if (c != null) c.Enabled = false;
            try
            {
                return await Task.Run(trabajo);
            }
            finally
            {
                this.UseWaitCursor = false;
                Cursor.Current     = Cursors.Default;
                foreach (var c in aBloquear) if (c != null) c.Enabled = true;
            }
        }

        public FrmReportes()
        {
            Text = "Reportes"; Size = new Size(980, 680);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.FromArgb(240, 244, 250);

            var hdr = new Panel { Dock = DockStyle.Top, Height = 50,
                BackColor = Color.FromArgb(26, 74, 138) };
            hdr.Controls.Add(new Label { Text = "Reportes del Sistema", ForeColor = Color.White,
                Font = new Font("Segoe UI", 12f, FontStyle.Bold), AutoSize = true,
                Location = new Point(12, 13) });

            var tabs = new TabControl { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 9f) };
            tabs.TabPages.Add(TabResumen());
            tabs.TabPages.Add(TabClientes());
            tabs.TabPages.Add(TabVentas());
            tabs.TabPages.Add(TabPrecios());
            tabs.TabPages.Add(TabVentasCliente());
            tabs.TabPages.Add(TabAbonos());
            tabs.TabPages.Add(TabCuentasCobrar());
            tabs.TabPages.Add(TabProduccion());
            tabs.TabPages.Add(TabInventario());
            tabs.TabPages.Add(TabPatronesVentas());
            tabs.TabPages.Add(TabCompras());
            tabs.TabPages.Add(TabMoldes());
            tabs.TabPages.Add(TabRentabilidad());
            tabs.TabPages.Add(TabPyL());     // P&L: Ventas vs Egresos
            tabs.TabPages.Add(TabFlujoCaja());
            tabs.TabPages.Add(TabLibroIVA());
            tabs.TabPages.Add(TabMermas());
            tabs.TabPages.Add(TabReorden());
            tabs.TabPages.Add(TabComparacion());
            if (Sesion.EsAdmin || Sesion.TieneRol("Contabilidad"))
                tabs.TabPages.Add(TabISLR());

            var footer = new Panel { Dock = DockStyle.Bottom, Height = 40,
                BackColor = Color.FromArgb(220, 220, 230) };
            var btnCerrar = new Button { Text = "Cerrar", Size = new Size(90, 26),
                Location = new Point(860, 7), BackColor = Color.FromArgb(100, 100, 120),
                ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnCerrar.FlatAppearance.BorderSize = 0;
            btnCerrar.Click += (s, e) => Close();
            footer.Controls.Add(btnCerrar);

            
            VentanaPrefs.Restaurar(this);
            var _tp = new System.Windows.Forms.Timer { Interval = 800 };
            _tp.Tick        += (s, e) => { _tp.Stop(); VentanaPrefs.Guardar(this); };
            SizeChanged     += (s, e) => { _tp.Stop(); _tp.Start(); };
            LocationChanged += (s, e) => { _tp.Stop(); _tp.Start(); };
            FormClosed      += (s, e) => { _tp.Stop(); _tp.Dispose(); };

Controls.AddRange(new Control[] { tabs, hdr, footer });
        }

        // ══════════════════════════════════════════════════════════
        //  RESUMEN (existente)
        // ══════════════════════════════════════════════════════════
        private TabPage TabResumen()
        {
            var tab   = new TabPage("Resumen");
            var panel = new FlowLayoutPanel { Dock = DockStyle.Fill,
                Padding = new Padding(20), BackColor = Color.White };

            // Cargar cards en placeholder mientras corre la query.
            var lblCargando = new Label
            {
                Text = "Cargando resumen...",
                Font = new Font("Segoe UI", 10f, FontStyle.Italic),
                ForeColor = Color.FromArgb(100, 100, 120),
                AutoSize = true, Margin = new Padding(8, 8, 8, 8)
            };
            panel.Controls.Add(lblCargando);
            tab.Controls.Add(panel);

            // OPTIMIZACIÓN: una sola query (10 sub-queries SUM/COUNT) en
            // lugar de traer 6 listas completas a memoria y agregar en LINQ.
            tab.HandleCreated += async (s, e) =>
            {
                try
                {
                    var rsm = await EjecutarConCarga(
                        () => DatabaseService.ObtenerResumenGlobal());
                    panel.Controls.Remove(lblCargando);
                    panel.Controls.Add(Card("Total Facturado",  rsm.TotalFact.ToString("C2"), Color.FromArgb(40, 160, 80)));
                    panel.Controls.Add(Card("Total Compras",    rsm.TotalComp.ToString("C2"), Color.FromArgb(26, 74, 138)));
                    panel.Controls.Add(Card("Clientes",         rsm.Clientes.ToString(),      Color.FromArgb(26, 74, 138)));
                    panel.Controls.Add(Card("Proveedores",      rsm.Proveedores.ToString(),   Color.FromArgb(160, 90, 20)));
                    panel.Controls.Add(Card("Productos",        rsm.Productos.ToString(),     Color.FromArgb(30, 120, 80)));
                    panel.Controls.Add(Card("Bajo Minimo",      rsm.BajoMin.ToString(),       Color.FromArgb(200, 80, 0)));
                    panel.Controls.Add(Card("Moldes",           rsm.Moldes.ToString(),        Color.FromArgb(20, 100, 140)));
                    panel.Controls.Add(Card("Mant. Vencido",    rsm.MantVencido.ToString(),   Color.FromArgb(180, 40, 60)));
                }
                catch (Exception ex)
                {
                    lblCargando.Text = "Error al cargar: " + ex.Message;
                    lblCargando.ForeColor = Color.FromArgb(180, 40, 40);
                }
            };

            return tab;
        }

        // ══════════════════════════════════════════════════════════
        //  CLIENTES
        // ══════════════════════════════════════════════════════════
        private TabPage TabClientes()
        {
            var tab = new TabPage("Clientes");
            var pnl = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };

            var bar      = new Panel { Dock = DockStyle.Top, Height = 44,
                BackColor = Color.FromArgb(228, 242, 232) };
            var txtBuscar = new TextBox { Location = new Point(10, 10), Size = new Size(200, 26),
                PlaceholderText = "Buscar..." };
            var btnPdf   = BtnAccion("Exportar PDF", Color.FromArgb(180, 60, 30), new Point(220, 8));
            bar.Controls.AddRange(new Control[] { txtBuscar, btnPdf });

            var dgv   = Grid(Color.FromArgb(26, 74, 138));
            var todos = DatabaseService.ObtenerClientes();

            void Cargar(string filtro = "")
            {
                var lista = string.IsNullOrWhiteSpace(filtro) ? todos
                    : todos.Where(c => BusquedaHelper.ContieneFlexibleEnAlguno(filtro,
                            c.Codigo, c.Descripcion, c.CiRif, c.Telefono, c.Ciudad)).ToList();
                dgv.DataSource = lista.Select(c => new {
                    c.Codigo, c.Descripcion, c.Ciudad, c.Estado,
                    c.Telefono, c.CiRif,
                    Credito = c.Credito.ToString("C2"),
                    AFavor  = c.AFavor.ToString("C2")
                }).ToList();
            }
            Cargar();

            txtBuscar.TextChanged += (s, e) => Cargar(txtBuscar.Text.ToLower());
            btnPdf.Click += (s, e) =>
            {
                try
                {
                    var pdf = PdfService.GenerarReporteClientes(DatabaseService.ObtenerClientes());
                    PdfService.GuardarYAbrir(pdf, "Clientes_" + DateTime.Today.ToString("yyyy-MM-dd") + ".pdf");
                }
                catch (Exception ex) { MsgErr(ex); }
            };

            pnl.Controls.Add(dgv);
            pnl.Controls.Add(bar);
            tab.Controls.Add(pnl);
            return tab;
        }

        // ══════════════════════════════════════════════════════════
        //  VENTAS
        // ══════════════════════════════════════════════════════════
        private TabPage TabVentas()
        {
            var tab = new TabPage("Ventas");
            var pnl = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };

            var bar = new Panel { Dock = DockStyle.Top, Height = 48,
                BackColor = Color.FromArgb(228, 242, 232) };
            int fx = 8;
            bar.Controls.Add(Lbl("Desde:", fx, 14)); fx += 52;
            var dtpD = new DateTimePicker { Location = new Point(fx, 10), Size = new Size(110, 26),
                Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddMonths(-1) };
            bar.Controls.Add(dtpD); fx += 118;
            bar.Controls.Add(Lbl("Hasta:", fx, 14)); fx += 50;
            var dtpH = new DateTimePicker { Location = new Point(fx, 10), Size = new Size(110, 26),
                Format = DateTimePickerFormat.Short, Value = DateTime.Today };
            bar.Controls.Add(dtpH); fx += 118;
            bar.Controls.Add(Lbl("Producto:", fx, 14)); fx += 68;
            var cmbProd = new ComboBox { Location = new Point(fx, 10), Size = new Size(180, 26),
                DropDownStyle = ComboBoxStyle.DropDownList };
            cmbProd.Items.Add("Todos");
            DatabaseService.ObtenerInventario()
                .ForEach(i => cmbProd.Items.Add(i.Codigo + " — " + i.Descripcion));
            cmbProd.SelectedIndex = 0;
            bar.Controls.Add(cmbProd); fx += 188;

            var btnGeneral  = BtnAccion("General",  Color.FromArgb(30, 100, 60),  new Point(fx, 8)); fx += 94;
            var btnAgrupado = BtnAccion("Agrupado", Color.FromArgb(22, 55, 120),  new Point(fx, 8)); fx += 94;
            var btnGrafico  = BtnAccion("Grafico",  Color.FromArgb(100, 60, 160), new Point(fx, 8)); fx += 94;
            var btnPdf      = BtnAccion("PDF",      Color.FromArgb(180, 60, 30),  new Point(fx, 8)); fx += 94;
            var btnExcel    = BtnAccion("Excel",    Color.FromArgb(30, 130, 60),  new Point(fx, 8));
            bar.Controls.AddRange(new Control[] { btnGeneral, btnAgrupado, btnGrafico, btnPdf, btnExcel });

            var pnlContenido = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            pnl.Controls.Add(pnlContenido);
            pnl.Controls.Add(bar);

            var dgv  = Grid(Color.FromArgb(30, 100, 60)); dgv.Dock = DockStyle.Fill;
            var pnlGraf = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Visible = false };
            pnlContenido.Controls.Add(dgv);
            pnlContenido.Controls.Add(pnlGraf);

            // Helper local: codigo del producto seleccionado o null
            string CodProd() => cmbProd.SelectedIndex <= 0 ? null
                : cmbProd.SelectedItem.ToString().Split('—')[0].Trim();

            // ── OPTIMIZADO: 1 sola query SQL en lugar de N+1 ────────────────
            //   Antes: ObtenerFacturas() + ObtenerDetalles por cada factura
            //   = ~10.000 queries para un mes de ventas → "(No responde)".
            //   Ahora: una sola query JOIN con filtro de fecha en SQL.
            //   Adapter convierte el DTO plano al modelo legacy LineaVentaReporte
            //   para no romper PdfService.GenerarReporteVentas / Excel.
            List<LineaVentaReporte> ObtenerLineasSync()
            {
                var raw = DatabaseService.ObtenerLineasVentaPorPeriodo(
                    dtpD.Value.Date, dtpH.Value.Date, CodProd());
                return raw.Select(l => new LineaVentaReporte
                {
                    NumFactura   = l.NumeroF,
                    Fecha        = l.Fecha,
                    Codigo       = l.Codigo,
                    Descripcion  = l.Descripcion,
                    Cantidad     = l.Cantidad,
                    PrecioUnit   = l.PrecioUnit,
                    Total        = l.Subtotal,
                    TasaBCV      = l.TasaBCV,
                    TasaParalelo = l.TasaParalelo
                }).ToList();
            }

            // Wrapper async + WaitCursor + bloqueo de botones para que el UI
            // no se congele ("(No responde)") durante la query.
            async Task<List<LineaVentaReporte>> ObtenerLineasAsync()
            {
                Cursor.Current = Cursors.WaitCursor;
                this.UseWaitCursor = true;
                btnGeneral.Enabled = btnAgrupado.Enabled = btnGrafico.Enabled =
                    btnPdf.Enabled = btnExcel.Enabled = false;
                try
                {
                    return await Task.Run(() => ObtenerLineasSync());
                }
                finally
                {
                    this.UseWaitCursor = false;
                    Cursor.Current = Cursors.Default;
                    btnGeneral.Enabled = btnAgrupado.Enabled = btnGrafico.Enabled =
                        btnPdf.Enabled = btnExcel.Enabled = true;
                }
            }

            List<(string Nombre, decimal Valor)> _grafDatos = new();

            btnGeneral.Click += async (s, e) =>
            {
                try
                {
                    var lineas = await ObtenerLineasAsync();
                    pnlGraf.Visible = false; dgv.Visible = true;
                    dgv.DataSource = lineas.Select(l => new {
                        Factura     = l.NumFactura,
                        Fecha       = l.Fecha.ToString("dd/MM/yyyy"),
                        l.Codigo, l.Descripcion,
                        Cantidad    = l.Cantidad.ToString("N2"),
                        Precio      = l.PrecioUnit.ToString("C2"),
                        Total       = l.Total.ToString("C2")
                    }).ToList();
                }
                catch (Exception ex) { MsgErr(ex); }
            };

            btnAgrupado.Click += async (s, e) =>
            {
                try
                {
                    var lineas = await ObtenerLineasAsync();
                    pnlGraf.Visible = false; dgv.Visible = true;

                    // Total vendido del período (para calcular la participación % de cada producto)
                    decimal granTotal = lineas.Sum(x => x.Total);

                    dgv.DataSource = lineas
                        .GroupBy(l => new { l.Codigo, l.Descripcion })
                        .Select(g => new {
                            g.Key.Codigo, g.Key.Descripcion,
                            Unidades    = g.Sum(x => x.Cantidad),
                            Total       = g.Sum(x => x.Total),
                            PrecioMin   = g.Min(x => x.PrecioUnit),
                            PrecioMax   = g.Max(x => x.PrecioUnit),
                            Facturas    = g.Select(x => x.NumFactura).Distinct().Count(),
                            UltimaVenta = g.Max(x => x.Fecha)
                        })
                        .OrderByDescending(x => x.Total)
                        .Select(x => new {
                            x.Codigo, x.Descripcion,
                            Unidades     = x.Unidades.ToString("N2"),
                            TotalVendido = x.Total.ToString("C2"),
                            // Precio promedio ponderado = total vendido / unidades vendidas
                            PrecioProm   = (x.Unidades != 0 ? x.Total / x.Unidades : 0).ToString("C2"),
                            PrecioMin    = x.PrecioMin.ToString("C2"),
                            PrecioMax    = x.PrecioMax.ToString("C2"),
                            Facturas     = x.Facturas.ToString("N0"),
                            PctTotal     = (granTotal != 0 ? x.Total / granTotal * 100m : 0).ToString("N1") + " %",
                            UltimaVenta  = x.UltimaVenta.ToString("dd/MM/yyyy")
                        }).ToList();

                    // Encabezados legibles
                    void Enc(string col, string txt) {
                        if (dgv.Columns.Contains(col)) dgv.Columns[col].HeaderText = txt;
                    }
                    Enc("Unidades",     "Unidades");
                    Enc("TotalVendido", "Total Vendido");
                    Enc("PrecioProm",   "Precio Prom.");
                    Enc("PrecioMin",    "Precio Mín.");
                    Enc("PrecioMax",    "Precio Máx.");
                    Enc("Facturas",     "N.° Facturas");
                    Enc("PctTotal",     "% del Total");
                    Enc("UltimaVenta",  "Última Venta");
                }
                catch (Exception ex) { MsgErr(ex); }
            };

            btnGrafico.Click += async (s, e) =>
            {
                try
                {
                    var lineas = await ObtenerLineasAsync();
                    dgv.Visible = false; pnlGraf.Visible = true;
                    _grafDatos = lineas
                        .GroupBy(l => l.Descripcion)
                        .Select(g => (g.Key, g.Sum(x => x.Total)))
                        .OrderByDescending(x => x.Item2)
                        .Take(10).ToList();
                    pnlGraf.Invalidate();
                }
                catch (Exception ex) { MsgErr(ex); }
            };

            pnlGraf.Paint += (s, e) => PintarTorta(e.Graphics, _grafDatos, pnlGraf.Size);

            btnPdf.Click += async (s, e) =>
            {
                try
                {
                    var lineas = await ObtenerLineasAsync();
                    if (lineas.Count == 0)
                    {
                        MessageBox.Show("No hay datos para el período seleccionado.",
                            "Sin datos", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }
                    var pdf = await Task.Run(() => PdfService.GenerarReporteVentas(
                        lineas, dtpD.Value, dtpH.Value, CodProd()));
                    PdfService.GuardarYAbrir(pdf, "Ventas_" + DateTime.Today.ToString("yyyy-MM-dd") + ".pdf");
                }
                catch (Exception ex) { MsgErr(ex); }
            };

            btnExcel.Click += async (s, e) =>
            {
                try
                {
                    var lineas = await ObtenerLineasAsync();
                    if (lineas.Count == 0)
                    {
                        MessageBox.Show("No hay datos para exportar en el período seleccionado.",
                            "Sin datos", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }
                    await Task.Run(() => ExportarVentasExcel(lineas, dtpD.Value, dtpH.Value));
                }
                catch (Exception ex) { MsgErr(ex); }
            };

            tab.Controls.Add(pnl);
            return tab;
        }

        // ══════════════════════════════════════════════════════════
        //  PRECIOS — evolución del precio de uno o varios productos
        // ══════════════════════════════════════════════════════════
        private TabPage TabPrecios()
        {
            var tab = new TabPage("Precios");
            var pnl = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };

            // ── Barra superior: fechas, filtro con IVA, botones ──────────────
            var bar = new Panel { Dock = DockStyle.Top, Height = 48,
                BackColor = Color.FromArgb(228, 242, 232) };
            int fx = 8;
            bar.Controls.Add(Lbl("Desde:", fx, 14)); fx += 52;
            var dtpD = new DateTimePicker { Location = new Point(fx, 10), Size = new Size(110, 26),
                Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddMonths(-3) };
            bar.Controls.Add(dtpD); fx += 118;
            bar.Controls.Add(Lbl("Hasta:", fx, 14)); fx += 50;
            var dtpH = new DateTimePicker { Location = new Point(fx, 10), Size = new Size(110, 26),
                Format = DateTimePickerFormat.Short, Value = DateTime.Today };
            bar.Controls.Add(dtpH); fx += 124;

            bar.Controls.Add(Lbl("Documentos:", fx, 14)); fx += 78;
            var cmbTipo = new ComboBox { Location = new Point(fx, 10), Size = new Size(190, 26),
                DropDownStyle = ComboBoxStyle.DropDownList };
            cmbTipo.Items.AddRange(new object[]
                { "Ambas", "Solo legales (con IVA)", "Solo notas de entrega" });
            cmbTipo.SelectedIndex = 0;
            bar.Controls.Add(cmbTipo); fx += 200;

            var btnGraf    = BtnAccion("Graficar", Color.FromArgb(100, 60, 160), new Point(fx, 8)); fx += 94;
            var btnLimpiar = BtnAccion("Limpiar",  Color.FromArgb(120, 120, 130), new Point(fx, 8));
            bar.Controls.AddRange(new Control[] { btnGraf, btnLimpiar });

            // ── Panel izquierdo: buscador + lista de productos con casillas ──
            var pnlIzq = new Panel { Dock = DockStyle.Left, Width = 300, BackColor = Color.White,
                Padding = new Padding(6) };
            var clb = new CheckedListBox { Dock = DockStyle.Fill, CheckOnClick = true,
                Font = new Font("Segoe UI", 8.5f), IntegralHeight = false };
            var txtBuscar = new TextBox { Dock = DockStyle.Top };
            var lblHdr = new Label { Text = "Productos (marca uno o varios):", Dock = DockStyle.Top,
                Height = 22, Font = new Font("Segoe UI", 9f, FontStyle.Bold) };

            var todosProd = DatabaseService.ObtenerInventario()
                .Select(i => (Cod: i.Codigo, Txt: i.Codigo + " — " + i.Descripcion))
                .OrderBy(x => x.Cod, StringComparer.Ordinal).ToList();
            var marcados = new HashSet<string>();

            void CargarLista(string filtro)
            {
                string f = (filtro ?? "").Trim().ToLower();
                clb.BeginUpdate();
                clb.Items.Clear();
                foreach (var p in todosProd)
                    if (f.Length == 0 || p.Txt.ToLower().Contains(f))
                        clb.Items.Add(p.Txt, marcados.Contains(p.Cod));
                clb.EndUpdate();
            }
            clb.ItemCheck += (s, e) =>
            {
                string txt = clb.Items[e.Index].ToString();
                string cod = txt.Split('—')[0].Trim();
                if (e.NewValue == CheckState.Checked) marcados.Add(cod);
                else marcados.Remove(cod);
            };
            txtBuscar.TextChanged += (s, e) => CargarLista(txtBuscar.Text);
            CargarLista("");

            pnlIzq.Controls.Add(clb);
            pnlIzq.Controls.Add(txtBuscar);
            pnlIzq.Controls.Add(lblHdr);

            // ── Panel derecho: gráfico ───────────────────────────────────────
            var pnlGraf = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            pnlGraf.Resize += (s, e) => pnlGraf.Invalidate();

            var series = new List<(string Cod, string Desc, Color Col, decimal Prom, List<(DateTime F, decimal P)> Pts)>();
            pnlGraf.Paint += (s, e) => PintarPrecios(e.Graphics, series, pnlGraf.Size);

            btnGraf.Click += async (s, e) =>
            {
                try
                {
                    var cods = marcados.ToList();
                    if (cods.Count == 0)
                    {
                        MessageBox.Show("Marca al menos un producto en la lista de la izquierda.",
                            "Sin productos", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }
                    if (cods.Count > 10)
                    {
                        MessageBox.Show("Marca hasta 10 productos a la vez para que el gráfico sea legible.",
                            "Demasiados productos", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }

                    Cursor.Current = Cursors.WaitCursor; this.UseWaitCursor = true;
                    btnGraf.Enabled = false;
                    try
                    {
                        var d = dtpD.Value.Date; var h = dtpH.Value.Date;
                        string modo = cmbTipo.SelectedIndex == 1 ? "IVA"
                                    : cmbTipo.SelectedIndex == 2 ? "NOTA" : "TODAS";
                        var datos = await Task.Run(() =>
                            DatabaseService.ObtenerPreciosProductoPorPeriodo(cods, d, h, modo));

                        series.Clear();
                        int ci = 0;
                        foreach (var grupo in datos.GroupBy(x => x.Codigo))
                        {
                            var pts = grupo.OrderBy(x => x.Fecha)
                                .Select(x => (F: x.Fecha, P: x.Precio)).ToList();
                            string desc = grupo.First().Descripcion;
                            // Promedio del período: ponderado por cantidad (= total vendido / total unidades)
                            decimal totCant = grupo.Sum(x => x.Cantidad);
                            decimal prom = totCant != 0
                                ? grupo.Sum(x => x.Precio * x.Cantidad) / totCant
                                : 0;
                            series.Add((grupo.Key, desc, _paleta[ci % _paleta.Length], prom, pts));
                            ci++;
                        }
                        pnlGraf.Invalidate();

                        if (series.Count == 0)
                        {
                            string suf = modo == "IVA"  ? " (solo facturas con IVA)."
                                       : modo == "NOTA" ? " (solo notas de entrega)."
                                       : ".";
                            MessageBox.Show("No hay ventas de esos productos en el rango indicado" + suf,
                                "Sin datos", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                    finally
                    {
                        this.UseWaitCursor = false; Cursor.Current = Cursors.Default;
                        btnGraf.Enabled = true;
                    }
                }
                catch (Exception ex) { MsgErr(ex); }
            };

            btnLimpiar.Click += (s, e) =>
            {
                marcados.Clear();
                CargarLista(txtBuscar.Text);
                series.Clear();
                pnlGraf.Invalidate();
            };

            // Orden de agregado para el docking: contenido, izquierdo, barra
            pnl.Controls.Add(pnlGraf);
            pnl.Controls.Add(pnlIzq);
            pnl.Controls.Add(bar);
            tab.Controls.Add(pnl);
            return tab;
        }

        // ══════════════════════════════════════════════════════════
        //  VENTAS POR CLIENTE
        // ══════════════════════════════════════════════════════════
        private TabPage TabVentasCliente()
        {
            var tab = new TabPage("Ventas x Cliente");
            var pnl = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };

            // Construir barra de filtros directamente (sin out params en lambdas)
            var bar = new Panel { Dock = DockStyle.Top, Height = 48,
                BackColor = Color.FromArgb(228, 242, 232) };
            var dtpD = DTP(bar, "Desde:", 8, DateTime.Today.AddMonths(-1), out int fx1);
            var dtpH = DTP(bar, "Hasta:", fx1, DateTime.Today, out int fx2);
            var cmbCliente = Cmb(bar, "Cliente:", fx2, out int fx3);
            var btnVer = BtnAccion("Ver", Color.FromArgb(30, 100, 60),  new Point(fx3, 8)); fx3 += 94;
            var btnPdf = BtnAccion("PDF", Color.FromArgb(180, 60, 30),  new Point(fx3, 8));
            bar.Controls.AddRange(new Control[] { btnVer, btnPdf });

            var dgv = Grid(Color.FromArgb(30, 100, 60)); dgv.Dock = DockStyle.Fill;

            // OPTIMIZADO: filtrado en SQL + async + WaitCursor.
            string CodCli() => cmbCliente.SelectedIndex <= 0 ? null
                : cmbCliente.SelectedItem.ToString().Split('—')[0].Trim();

            btnVer.Click += async (s, e) =>
            {
                try
                {
                    var facturas = await EjecutarConCarga(
                        () => DatabaseService.ObtenerFacturasPorPeriodo(
                            dtpD.Value.Date, dtpH.Value.Date, CodCli()),
                        btnVer, btnPdf);
                    dgv.DataSource = facturas.Select(f => new {
                        f.NumeroF, Fecha = f.Fecha.ToString("dd/MM/yyyy"),
                        Cliente = f.Codigo, f.FormaDePago,
                        IVA = f.IvaF.ToString("C2"), Total = f.TotalF.ToString("C2")
                    }).ToList();
                }
                catch (Exception ex) { MsgErr(ex); }
            };
            btnPdf.Click += async (s, e) =>
            {
                try
                {
                    string cod = CodCli();
                    var facturas = await EjecutarConCarga(
                        () => DatabaseService.ObtenerFacturasPorPeriodo(
                            dtpD.Value.Date, dtpH.Value.Date, cod),
                        btnVer, btnPdf);
                    var pdf = await Task.Run(() => PdfService.GenerarReporteVentasPorCliente(
                        facturas, dtpD.Value, dtpH.Value, cod));
                    PdfService.GuardarYAbrir(pdf, "VentasCliente_" + DateTime.Today.ToString("yyyy-MM-dd") + ".pdf");
                }
                catch (Exception ex) { MsgErr(ex); }
            };

            pnl.Controls.Add(dgv);
            pnl.Controls.Add(bar);
            tab.Controls.Add(pnl);
            return tab;
        }

        // ══════════════════════════════════════════════════════════
        //  ABONOS
        // ══════════════════════════════════════════════════════════
        private TabPage TabAbonos()
        {
            var tab = new TabPage("Abonos");
            var pnl = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };

            var bar = new Panel { Dock = DockStyle.Top, Height = 48,
                BackColor = Color.FromArgb(228, 242, 232) };
            var dtpD       = DTP(bar, "Desde:", 8, DateTime.Today.AddMonths(-1), out int fx1);
            var dtpH       = DTP(bar, "Hasta:", fx1, DateTime.Today, out int fx2);
            var cmbCliente = Cmb(bar, "Cliente:", fx2, out int fx3);
            var btnVer     = BtnAccion("Ver", Color.FromArgb(40, 140, 140), new Point(fx3, 8)); fx3 += 94;
            var btnPdf     = BtnAccion("PDF", Color.FromArgb(180, 60, 30),  new Point(fx3, 8));
            bar.Controls.AddRange(new Control[] { btnVer, btnPdf });

            var dgv = Grid(Color.FromArgb(40, 140, 140)); dgv.Dock = DockStyle.Fill;

            // OPTIMIZADO: filtrado en SQL + async + WaitCursor.
            string CodCli() => cmbCliente.SelectedIndex <= 0 ? null
                : cmbCliente.SelectedItem.ToString().Split('—')[0].Trim();

            btnVer.Click += async (s, e) =>
            {
                try
                {
                    var abonos = await EjecutarConCarga(
                        () => DatabaseService.ObtenerAbonosPorPeriodo(
                            dtpD.Value.Date, dtpH.Value.Date, CodCli()),
                        btnVer, btnPdf);
                    dgv.DataSource = abonos.Select(a => new {
                        a.NumAbono, a.NumeroF,
                        Fecha   = a.Fecha.ToString("dd/MM/yyyy"),
                        Cliente = a.Cliente,
                        Monto   = a.Monto.ToString("C2")
                    }).ToList();
                }
                catch (Exception ex) { MsgErr(ex); }
            };
            btnPdf.Click += async (s, e) =>
            {
                try
                {
                    string cod = CodCli();
                    var abonos = await EjecutarConCarga(
                        () => DatabaseService.ObtenerAbonosPorPeriodo(
                            dtpD.Value.Date, dtpH.Value.Date, cod),
                        btnVer, btnPdf);
                    var pdf = await Task.Run(() => PdfService.GenerarReporteAbonos(
                        abonos, dtpD.Value, dtpH.Value, cod));
                    PdfService.GuardarYAbrir(pdf, "Abonos_" + DateTime.Today.ToString("yyyy-MM-dd") + ".pdf");
                }
                catch (Exception ex) { MsgErr(ex); }
            };

            pnl.Controls.Add(dgv);
            pnl.Controls.Add(bar);
            tab.Controls.Add(pnl);
            return tab;
        }

        // ══════════════════════════════════════════════════════════
        //  CUENTAS POR COBRAR
        // ══════════════════════════════════════════════════════════
        private TabPage TabCuentasCobrar()
        {
            var tab = new TabPage("Ctas. x Cobrar");
            var pnl = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };

            var bar = new Panel { Dock = DockStyle.Top, Height = 48,
                BackColor = Color.FromArgb(228, 242, 232) };
            bar.Controls.Add(Lbl("Cliente:", 8, 14));
            var cmbCliente = new ComboBox { Location = new Point(68, 10), Size = new Size(260, 26),
                DropDownStyle = ComboBoxStyle.DropDownList };
            cmbCliente.Items.Add("Todos los clientes");
            DatabaseService.ObtenerClientes()
                .ForEach(c => cmbCliente.Items.Add(c.Codigo + " — " + c.Descripcion));
            cmbCliente.SelectedIndex = 0;
            bar.Controls.Add(cmbCliente);

            var chkPend = new CheckBox { Text = "Solo pendientes", Location = new Point(340, 13),
                AutoSize = true, Font = new Font("Segoe UI", 9f), Checked = true };
            bar.Controls.Add(chkPend);

            var btnVer = BtnAccion("Ver", Color.FromArgb(40, 140, 140), new Point(480, 8));
            var btnPdf = BtnAccion("PDF", Color.FromArgb(180, 60, 30),  new Point(576, 8));
            bar.Controls.AddRange(new Control[] { btnVer, btnPdf });

            var dgv = Grid(Color.FromArgb(40, 140, 140)); dgv.Dock = DockStyle.Fill;

            // OPTIMIZADO: filtrado en SQL + async + WaitCursor.
            string CodCli() => cmbCliente.SelectedIndex <= 0 ? null
                : cmbCliente.SelectedItem.ToString().Split('—')[0].Trim();

            btnVer.Click += async (s, e) =>
            {
                try
                {
                    var cuentas = await EjecutarConCarga(
                        () => DatabaseService.ObtenerCuentasFiltrado(CodCli(), chkPend.Checked),
                        btnVer, btnPdf);
                    dgv.DataSource = cuentas.Select(c => new {
                        c.Id, c.Cliente, c.Concepto,
                        Monto  = c.Monto.ToString("C2"),
                        Pagado = c.Pagado.ToString("C2"),
                        Saldo  = c.Saldo.ToString("C2"),
                        Vence  = c.FechaVence.ToString("dd/MM/yyyy"),
                        c.Estado
                    }).ToList();
                }
                catch (Exception ex) { MsgErr(ex); }
            };
            btnPdf.Click += async (s, e) =>
            {
                try
                {
                    string cod = CodCli();
                    var cuentas = await EjecutarConCarga(
                        () => DatabaseService.ObtenerCuentasFiltrado(cod, chkPend.Checked),
                        btnVer, btnPdf);
                    var pdf = await Task.Run(() => PdfService.GenerarReporteCuentasCobrar(cuentas, cod));
                    PdfService.GuardarYAbrir(pdf, "CuentasCobrar_" + DateTime.Today.ToString("yyyy-MM-dd") + ".pdf");
                }
                catch (Exception ex) { MsgErr(ex); }
            };

            pnl.Controls.Add(dgv);
            pnl.Controls.Add(bar);
            tab.Controls.Add(pnl);
            return tab;
        }

        // ══════════════════════════════════════════════════════════
        //  PRODUCCION
        // ══════════════════════════════════════════════════════════
        private TabPage TabProduccion()
        {
            var tab = new TabPage("Produccion");
            var pnl = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };

            var bar = new Panel { Dock = DockStyle.Top, Height = 48,
                BackColor = Color.FromArgb(228, 242, 232) };
            int fx = 8;
            bar.Controls.Add(Lbl("Desde:", fx, 14)); fx += 52;
            var dtpD = new DateTimePicker { Location = new Point(fx, 10), Size = new Size(110, 26),
                Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddMonths(-1) };
            bar.Controls.Add(dtpD); fx += 118;
            bar.Controls.Add(Lbl("Hasta:", fx, 14)); fx += 50;
            var dtpH = new DateTimePicker { Location = new Point(fx, 10), Size = new Size(110, 26),
                Format = DateTimePickerFormat.Short, Value = DateTime.Today };
            bar.Controls.Add(dtpH); fx += 118;
            bar.Controls.Add(Lbl("Producto:", fx, 14)); fx += 68;
            var cmbProd = new ComboBox { Location = new Point(fx, 10), Size = new Size(180, 26),
                DropDownStyle = ComboBoxStyle.DropDownList };
            cmbProd.Items.Add("Todos");
            DatabaseService.ObtenerInventario()
                .ForEach(i => cmbProd.Items.Add(i.Codigo + " — " + i.Descripcion));
            cmbProd.SelectedIndex = 0;
            bar.Controls.Add(cmbProd); fx += 188;

            var chkDet = new CheckBox { Text = "Detallado", Location = new Point(fx, 13),
                AutoSize = true, Font = new Font("Segoe UI", 9f), Checked = true };
            bar.Controls.Add(chkDet); fx += 90;

            var btnVer = BtnAccion("Ver", Color.FromArgb(20, 120, 160), new Point(fx, 8)); fx += 94;
            var btnPdf = BtnAccion("PDF", Color.FromArgb(180, 60, 30),  new Point(fx, 8));
            bar.Controls.AddRange(new Control[] { btnVer, btnPdf });

            var dgv = Grid(Color.FromArgb(20, 120, 160)); dgv.Dock = DockStyle.Fill;

            // OPTIMIZADO: la query ya filtra por SQL; añadimos async + WaitCursor.
            string CodProd() => cmbProd.SelectedIndex <= 0 ? null
                : cmbProd.SelectedItem.ToString().Split('—')[0].Trim();

            btnVer.Click += async (s, e) =>
            {
                try
                {
                    string cod = CodProd();
                    var datos = await EjecutarConCarga(
                        () => DatabaseService.ObtenerProduccionDiariaRango(dtpD.Value, dtpH.Value)
                              .Where(p => cod == null || p.Codigo == cod).ToList(),
                        btnVer, btnPdf);

                    if (chkDet.Checked)
                        dgv.DataSource = datos.Select(p => new {
                            Fecha      = p.Fecha.ToString("dd/MM/yyyy"),
                            p.Codigo, p.Descripcion,
                            Cantidad   = p.Cantidad.ToString("N2"),
                            CostoUnit  = p.CostoUnitario.ToString("C2"),
                            CostoTotal = p.CostoTotal.ToString("C2"),
                            p.Observacion
                        }).ToList();
                    else
                        dgv.DataSource = datos
                            .GroupBy(p => new { p.Codigo, p.Descripcion })
                            .Select(g => new {
                                g.Key.Codigo, g.Key.Descripcion,
                                TotalUnidades = g.Sum(x => x.Cantidad).ToString("N2"),
                                CostoTotal    = g.Sum(x => x.CostoTotal).ToString("C2")
                            }).ToList();
                }
                catch (Exception ex) { MsgErr(ex); }
            };
            btnPdf.Click += async (s, e) =>
            {
                try
                {
                    string cod = CodProd();
                    var datos = await EjecutarConCarga(
                        () => DatabaseService.ObtenerProduccionDiariaRango(dtpD.Value, dtpH.Value)
                              .Where(p => cod == null || p.Codigo == cod).ToList(),
                        btnVer, btnPdf);
                    var pdf = await Task.Run(() => PdfService.GenerarReporteProduccion(
                        datos, dtpD.Value, dtpH.Value, cod, chkDet.Checked));
                    PdfService.GuardarYAbrir(pdf, "Produccion_" + DateTime.Today.ToString("yyyy-MM-dd") + ".pdf");
                }
                catch (Exception ex) { MsgErr(ex); }
            };

            pnl.Controls.Add(dgv);
            pnl.Controls.Add(bar);
            tab.Controls.Add(pnl);
            return tab;
        }

        // ══════════════════════════════════════════════════════════
        //  INVENTARIO
        // ══════════════════════════════════════════════════════════
        private TabPage TabInventario()
        {
            var tab = new TabPage("Inventario");
            var pnl = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };

            var bar    = new Panel { Dock = DockStyle.Top, Height = 44,
                BackColor = Color.FromArgb(228, 242, 232) };
            var btnPdf     = BtnAccion("Exportar PDF", Color.FromArgb(180, 60, 30), new Point(8, 8));
            var btnImprimir = BtnAccion("Imprimir",    Color.FromArgb(80, 60, 140), new Point(102, 8));
            bar.Controls.AddRange(new Control[] { btnPdf, btnImprimir });

            var dgv   = Grid(Color.FromArgb(30, 100, 60)); dgv.Dock = DockStyle.Fill;
            var lista = DatabaseService.ObtenerInventario();
            dgv.DataSource = lista.Select(i => new {
                i.Codigo, i.Descripcion, i.Departamento,
                Costo  = i.CostoActual.ToString("C2"),
                Precio = i.Precio.ToString("C2"),
                i.Existencia, i.ExistenMin, i.ExistenMax
            }).ToList();
            dgv.DataBindingComplete += (s, e) =>
            {
                for (int idx = 0; idx < dgv.Rows.Count && idx < lista.Count; idx++)
                {
                    if (lista[idx].Existencia == 0)
                        dgv.Rows[idx].DefaultCellStyle.BackColor = Color.FromArgb(255, 210, 210);
                    else if (lista[idx].Existencia < lista[idx].ExistenMin)
                        dgv.Rows[idx].DefaultCellStyle.BackColor = Color.FromArgb(255, 245, 190);
                }
            };

            btnPdf.Click += (s, e) =>
            {
                try
                {
                    var pdf = PdfService.GenerarReporteInventario(lista);
                    PdfService.GuardarYAbrir(pdf, "Inventario_" + DateTime.Today.ToString("yyyy-MM-dd") + ".pdf");
                }
                catch (Exception ex) { MsgErr(ex); }
            };

            btnImprimir.Click += (s, e) => ImprimirListaInventario(lista);

            pnl.Controls.Add(dgv);
            pnl.Controls.Add(bar);
            tab.Controls.Add(pnl);
            return tab;
        }

        private void ImprimirListaInventario(List<Inventario> lista)
        {
            if (lista == null || lista.Count == 0)
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

                var C1 = Color.FromArgb(30, 100, 60);

                // Título
                g.FillRectangle(new SolidBrush(C1), new Rectangle(x, y, W, 28));
                g.DrawString("REPORTE DE INVENTARIO",
                    new Font("Segoe UI", 12f, FontStyle.Bold), Brushes.White,
                    new RectangleF(x + 8, y + 5, W, 22));
                g.DrawString($"Pág {pagActual + 1}/{totalPags}  —  {DateTime.Now:dd/MM/yyyy HH:mm}",
                    new Font("Segoe UI", 8f), Brushes.White,
                    new RectangleF(x + W - 220, y + 8, 220, 16),
                    new StringFormat { Alignment = StringAlignment.Far });
                y += 32;

                // Encabezados
                string[] hdrs = { "Código", "Descripción", "Depto", "Costo", "Precio", "Exist.", "Mín", "Máx" };
                int[] colW    = { 70, 230, 100, 85, 85, 65, 60, 60 };

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
                    Color bg = (r - inicio) % 2 == 0 ? Color.White : Color.FromArgb(232, 248, 238);
                    if (it.Existencia == 0)       bg = Color.FromArgb(255, 220, 220);
                    else if (it.Existencia < it.ExistenMin) bg = Color.FromArgb(255, 248, 200);

                    g.FillRectangle(new SolidBrush(bg), new Rectangle(x, y, W, 16));

                    string[] vals = {
                        it.Codigo, it.Descripcion, it.Departamento,
                        it.CostoActual.ToString("C2"), it.Precio.ToString("C2"),
                        it.Existencia.ToString("N2"),
                        it.ExistenMin.ToString("N0"), it.ExistenMax.ToString("N0")
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
                    g.FillRectangle(new SolidBrush(Color.FromArgb(220, 240, 225)),
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

        // ══════════════════════════════════════════════════════════
        //  PATRONES DE VENTAS
        // ══════════════════════════════════════════════════════════
        private TabPage TabPatronesVentas()
        {
            var tab = new TabPage("Patrones Ventas");
            var pnl = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };

            var bar = new Panel { Dock = DockStyle.Top, Height = 48,
                BackColor = Color.FromArgb(228, 242, 232) };
            bar.Controls.Add(Lbl("Anos (ej: 2022,2023,2024):", 8, 14));
            var txtAnos = new TextBox { Location = new Point(210, 10), Size = new Size(200, 26),
                Text = (DateTime.Today.Year-2) + "," + (DateTime.Today.Year-1) + "," + DateTime.Today.Year };
            bar.Controls.Add(txtAnos);

            var btnAnalizar = BtnAccion("Analizar", Color.FromArgb(100, 60, 160), new Point(422, 8));
            var btnPdf      = BtnAccion("PDF",      Color.FromArgb(180, 60, 30),  new Point(518, 8));
            bar.Controls.AddRange(new Control[] { btnAnalizar, btnPdf });

            var pnlGraf = new Panel { Dock = DockStyle.Top, Height = 260, BackColor = Color.White };
            var dgv     = Grid(Color.FromArgb(100, 60, 160)); dgv.Dock = DockStyle.Fill;

            List<(int Ano, int Mes, decimal Total)> _datos = new();

            pnlGraf.Paint += (s, e) => PintarLineasAnuales(e.Graphics, _datos, pnlGraf.Size);

            // OPTIMIZADO: agregacion (SUM, GROUP BY ano/mes) hecha en SQL.
            // Antes: ObtenerFacturas trae N filas, LINQ groupby en C#.
            // Ahora: SQLite agrega y devuelve directamente 12*N filas (max 36
            // para 3 anos), todo el calculo en motor relacional.
            int[] ParsearAnos() => txtAnos.Text.Split(',')
                .Select(a => int.TryParse(a.Trim(), out int v) ? v : 0)
                .Where(a => a > 0).ToArray();

            async Task AnalizarAsync()
            {
                var anos = ParsearAnos();
                if (anos.Length == 0) return;

                _datos = await EjecutarConCarga(
                    () => DatabaseService.ObtenerVentasMensualesPorAnos(anos),
                    btnAnalizar, btnPdf);
                pnlGraf.Invalidate();

                dgv.DataSource = _datos.GroupBy(d => d.Ano).Select(g =>
                {
                    decimal[] meses = Enumerable.Range(1, 12)
                        .Select(m => g.FirstOrDefault(x => x.Mes == m).Total).ToArray();
                    return new {
                        Ano = g.Key,
                        Ene=meses[0].ToString("C0"), Feb=meses[1].ToString("C0"),
                        Mar=meses[2].ToString("C0"), Abr=meses[3].ToString("C0"),
                        May=meses[4].ToString("C0"), Jun=meses[5].ToString("C0"),
                        Jul=meses[6].ToString("C0"), Ago=meses[7].ToString("C0"),
                        Sep=meses[8].ToString("C0"), Oct=meses[9].ToString("C0"),
                        Nov=meses[10].ToString("C0"),Dic=meses[11].ToString("C0"),
                        Total=g.Sum(x=>x.Total).ToString("C0")
                    };
                }).ToList();
            }

            btnAnalizar.Click += async (s, e) =>
            {
                try { await AnalizarAsync(); }
                catch (Exception ex) { MsgErr(ex); }
            };
            btnPdf.Click += async (s, e) =>
            {
                try
                {
                    if (_datos.Count == 0) await AnalizarAsync();
                    var pdf = await Task.Run(() =>
                        PdfService.GenerarReportePatronesVentas(_datos, txtAnos.Text));
                    PdfService.GuardarYAbrir(pdf, "PatronesVentas_" + DateTime.Today.ToString("yyyy-MM-dd") + ".pdf");
                }
                catch (Exception ex) { MsgErr(ex); }
            };

            pnl.Controls.Add(dgv);
            pnl.Controls.Add(pnlGraf);
            pnl.Controls.Add(bar);
            tab.Controls.Add(pnl);
            return tab;
        }

        // ══════════════════════════════════════════════════════════
        //  TABS EXISTENTES
        // ══════════════════════════════════════════════════════════
        private TabPage TabCompras()
        {
            var tab = new TabPage("Compras");
            var dgv = Grid(Color.FromArgb(100, 50, 160));
            dgv.DataSource = DatabaseService.ObtenerCompras().Select(c => new {
                c.CompraN, Fecha=c.FechaC.ToShortDateString(), Proveedor=c.CodigoP,
                c.FormaDePago, Total=c.TotalC.ToString("C2") }).ToList();
            tab.Controls.Add(dgv);
            return tab;
        }

        private TabPage TabMoldes()
        {
            var tab = new TabPage("Moldes");
            var dgv = Grid(Color.FromArgb(20, 100, 140));
            dgv.DataSource = DatabaseService.ObtenerMoldes().Select(m => new {
                m.CodigoMold, m.Descripcion, m.ModeloQfabrica,
                UltMant = m.FechaLastMant.ToShortDateString(),
                Dias    = (DateTime.Today - m.FechaLastMant).Days,
                m.CantMatXunid }).ToList();
            tab.Controls.Add(dgv);
            return tab;
        }

        private TabPage TabRentabilidad()
        {
            var tab = new TabPage("Rentabilidad");
            var pnl = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            var btn = BtnCentrado("Abrir reporte completo de rentabilidad",
                Color.FromArgb(30, 100, 60), pnl);
            btn.Click += (s, e) => new FrmRentabilidad().ShowDialog();
            tab.Controls.Add(pnl); return tab;
        }

        // ══════════════════════════════════════════════════════════
        //  P&L — GANANCIAS Y PÉRDIDAS  (Ventas USD vs Egresos USD)
        //  Cierra el loop del módulo Egresos: muestra el resultado
        //  neto del negocio mes a mes en una sola pantalla.
        // ══════════════════════════════════════════════════════════
        private TabPage TabPyL()
        {
            var tab = new TabPage("Ganancias / Pérdidas");
            var pnl = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };

            var bar = new Panel { Dock = DockStyle.Top, Height = 50,
                BackColor = Color.FromArgb(228, 235, 244) };
            int fx = 8;
            bar.Controls.Add(Lbl("Desde:", fx, 16)); fx += 52;
            var dtpD = new DateTimePicker { Location = new Point(fx, 12), Size = new Size(110, 26),
                Format = DateTimePickerFormat.Short,
                Value = new DateTime(DateTime.Today.Year, 1, 1) };
            bar.Controls.Add(dtpD); fx += 118;
            bar.Controls.Add(Lbl("Hasta:", fx, 16)); fx += 50;
            var dtpH = new DateTimePicker { Location = new Point(fx, 12), Size = new Size(110, 26),
                Format = DateTimePickerFormat.Short, Value = DateTime.Today };
            bar.Controls.Add(dtpH); fx += 130;

            var btnCalc = BtnAccion("Calcular", Color.FromArgb(30, 100, 60), new Point(fx, 10));
            fx += 110;
            var btnPdf = BtnAccion("PDF", Color.FromArgb(180, 60, 30), new Point(fx, 10));
            bar.Controls.AddRange(new Control[] { btnCalc, btnPdf });

            // Cards de KPIs
            var pnlKpi = new TableLayoutPanel { Dock = DockStyle.Top, Height = 90,
                ColumnCount = 4, RowCount = 1, BackColor = Color.White,
                Padding = new Padding(8, 8, 8, 4) };
            for (int i = 0; i < 4; i++)
                pnlKpi.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));

            Label lblKpiV = null!, lblKpiE = null!, lblKpiU = null!, lblKpiM = null!;
            pnlKpi.Controls.Add(KpiCardLocal("VENTAS USD", out lblKpiV, "$ 0,00",
                Color.FromArgb(30, 100, 60)), 0, 0);
            pnlKpi.Controls.Add(KpiCardLocal("EGRESOS USD", out lblKpiE, "$ 0,00",
                Color.FromArgb(192, 57, 43)), 1, 0);
            pnlKpi.Controls.Add(KpiCardLocal("UTILIDAD NETA", out lblKpiU, "$ 0,00",
                Color.FromArgb(26, 74, 138)), 2, 0);
            pnlKpi.Controls.Add(KpiCardLocal("MARGEN %", out lblKpiM, "0%",
                Color.FromArgb(83, 74, 183)), 3, 0);

            // Split: tabla + gráfico
            var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal,
                BackColor = Color.White };
            split.HandleCreated += (s, e) =>
            {
                try { split.SplitterDistance = (int)(split.Height * 0.45); } catch { }
            };

            var dgv = Grid(Color.FromArgb(26, 74, 138));
            dgv.Dock = DockStyle.Fill;
            split.Panel1.Controls.Add(dgv);

            var pnlGraf = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            split.Panel2.Controls.Add(pnlGraf);

            // Datos cacheados para el gráfico y PDF
            List<(string Mes, decimal V, decimal E, decimal U)> _datos = new();

            pnlGraf.Paint += (s, e) => PintarPyL(e.Graphics, _datos, pnlGraf.Size);

            void Calcular()
            {
                try
                {
                    Cursor.Current = Cursors.WaitCursor;
                    var d = dtpD.Value.Date; var h = dtpH.Value.Date;
                    var ventas  = DatabaseService.ObtenerVentasMensualesUSDPorPeriodo(d, h);
                    var egresos = DatabaseService.ObtenerEgresosMensualesPorPeriodo(d, h);

                    // Indexar por (Año, Mes) para join
                    var dictV = ventas.ToDictionary(x => (x.Ano, x.Mes), x => x.VentasUSD);
                    var dictE = egresos.ToDictionary(x => (x.Ano, x.Mes),
                        x => (MP: x.MateriaPrima, Op: x.Operativos));

                    var clavesUni = dictV.Keys.Union(dictE.Keys)
                        .OrderBy(k => k.Ano).ThenBy(k => k.Mes).ToList();

                    string[] mn = { "Ene","Feb","Mar","Abr","May","Jun",
                                    "Jul","Ago","Sep","Oct","Nov","Dic" };
                    decimal totalV = 0, totalE = 0;
                    var datosTabla = new List<object>();
                    _datos.Clear();
                    foreach (var (ano, mes) in clavesUni)
                    {
                        decimal v = dictV.TryGetValue((ano, mes), out var vv) ? vv : 0;
                        decimal mp = 0, op = 0;
                        if (dictE.TryGetValue((ano, mes), out var ee))
                        { mp = ee.MP; op = ee.Op; }
                        decimal e = mp + op;
                        decimal u = v - e;
                        decimal margen = v > 0 ? (u / v * 100) : 0;
                        totalV += v; totalE += e;
                        string lblMes = mn[mes - 1] + " " + (ano % 100).ToString("00");
                        datosTabla.Add(new {
                            Mes        = lblMes,
                            VentasUSD  = "$ " + v.ToString("N2"),
                            MateriaPrima = "$ " + mp.ToString("N2"),
                            Operativos = "$ " + op.ToString("N2"),
                            EgresosUSD = "$ " + e.ToString("N2"),
                            Utilidad   = "$ " + u.ToString("N2"),
                            Margen     = margen.ToString("N1") + "%"
                        });
                        _datos.Add((lblMes, v, e, u));
                    }

                    dgv.DataSource = datosTabla;
                    decimal totalU = totalV - totalE;
                    decimal totalMargen = totalV > 0 ? (totalU / totalV * 100) : 0;

                    lblKpiV.Text = "$ " + totalV.ToString("N2");
                    lblKpiE.Text = "$ " + totalE.ToString("N2");
                    lblKpiU.Text = "$ " + totalU.ToString("N2");
                    lblKpiU.ForeColor = totalU >= 0 ? Color.FromArgb(15, 110, 86)
                                                    : Color.FromArgb(192, 57, 43);
                    lblKpiM.Text = totalMargen.ToString("N1") + "%";
                    lblKpiM.ForeColor = totalMargen >= 0 ? Color.FromArgb(15, 110, 86)
                                                         : Color.FromArgb(192, 57, 43);

                    pnlGraf.Invalidate();
                }
                catch (Exception ex) { MsgErr(ex); }
                finally { Cursor.Current = Cursors.Default; }
            }

            btnCalc.Click += (s, e) => Calcular();
            btnPdf.Click  += (s, e) =>
            {
                if (_datos.Count == 0) Calcular();
                MessageBox.Show("Exportación PDF del P&L pendiente de implementar.\n" +
                    "Por ahora puedes exportar los datos copiando desde la tabla.",
                    "PDF", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };

            pnl.Controls.Add(split);
            pnl.Controls.Add(pnlKpi);
            pnl.Controls.Add(bar);
            tab.Controls.Add(pnl);

            // Auto-calcular al abrir
            tab.HandleCreated += (s, e) => Calcular();
            return tab;
        }

        private Panel KpiCardLocal(string titulo, out Label lblValor, string valorIni, Color barra)
        {
            var p = new Panel { Dock = DockStyle.Fill, BackColor = Color.White,
                Margin = new Padding(0, 0, 8, 0), Padding = new Padding(8) };
            p.Paint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(215, 222, 230), 1f);
                e.Graphics.DrawRectangle(pen, 0, 0, p.Width - 1, p.Height - 1);
            };
            var bar = new Panel { Dock = DockStyle.Left, Width = 4, BackColor = barra };
            var lblTit = new Label
            {
                Text = "  " + titulo,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(122, 131, 149),
                Dock = DockStyle.Top, Height = 20,
                TextAlign = ContentAlignment.MiddleLeft
            };
            var lblVal = new Label
            {
                Text = "  " + valorIni,
                Font = new Font("Segoe UI", 17f, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 34, 51),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };
            lblValor = lblVal;
            p.Controls.Add(lblVal); p.Controls.Add(lblTit); p.Controls.Add(bar);
            return p;
        }

        private void PintarPyL(Graphics g,
            List<(string Mes, decimal V, decimal E, decimal U)> datos, Size size)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.White);
            if (datos.Count == 0)
            {
                using var brT = new SolidBrush(Color.FromArgb(122, 131, 149));
                g.DrawString("Pulsa 'Calcular' para ver el gráfico",
                    new Font("Segoe UI", 10f, FontStyle.Italic), brT,
                    size.Width / 2 - 130, size.Height / 2 - 8);
                return;
            }
            int W = size.Width, H = size.Height;
            int padL = 60, padR = 130, padT = 16, padB = 36;
            decimal maxAbs = Math.Max(1m, datos.Max(d => Math.Max(d.V, d.E)));
            float maxV = (float)maxAbs;
            int n = datos.Count;
            float barW = (W - padL - padR) / (float)(n * 3f);

            using var brV = new SolidBrush(Color.FromArgb(30, 130, 80));
            using var brE = new SolidBrush(Color.FromArgb(192, 57, 43));
            using var penU = new Pen(Color.FromArgb(26, 74, 138), 2.5f);
            using var brLbl = new SolidBrush(Color.FromArgb(122, 131, 149));
            using var brTxt = new SolidBrush(Color.FromArgb(30, 34, 51));
            using var penGrid = new Pen(Color.FromArgb(240, 242, 246), 1);

            // Grid
            for (int i = 0; i <= 4; i++)
            {
                int y = padT + (H - padT - padB) * i / 4;
                g.DrawLine(penGrid, padL, y, W - padR, y);
                float val = maxV * (4 - i) / 4f;
                g.DrawString("$ " + val.ToString("N0"),
                    new Font("Segoe UI", 8f), brLbl, 4, y - 6);
            }

            var puntosU = new PointF[n];
            for (int i = 0; i < n; i++)
            {
                float xCentro = padL + (W - padL - padR) * (i + 0.5f) / n;
                float xV = xCentro - barW;
                float xE = xCentro;
                float hV = (H - padT - padB) * (float)datos[i].V / maxV;
                float hE = (H - padT - padB) * (float)datos[i].E / maxV;
                g.FillRectangle(brV, xV, H - padB - hV, barW * 0.9f, hV);
                g.FillRectangle(brE, xE, H - padB - hE, barW * 0.9f, hE);
                g.DrawString(datos[i].Mes, new Font("Segoe UI", 8f), brLbl,
                    xCentro - 14, H - padB + 4);
                // Línea de utilidad: sigue el centro vertical de cada mes
                float yU = padT + (H - padT - padB) * (1 - (float)datos[i].U / maxV);
                if (yU < padT) yU = padT;
                if (yU > H - padB) yU = H - padB;
                puntosU[i] = new PointF(xCentro - 5, yU);
            }
            if (n >= 2) g.DrawLines(penU, puntosU);
            for (int i = 0; i < n; i++)
                g.FillEllipse(new SolidBrush(Color.FromArgb(26, 74, 138)),
                    puntosU[i].X - 3, puntosU[i].Y - 3, 6, 6);

            // Leyenda
            int lx = W - padR + 14, ly = padT + 4;
            void Leyenda(int dy, Color c, string txt)
            {
                using var b = new SolidBrush(c);
                g.FillRectangle(b, lx, ly + dy, 12, 12);
                g.DrawString(txt, new Font("Segoe UI", 9f), brTxt, lx + 18, ly + dy - 2);
            }
            Leyenda(0,  Color.FromArgb(30, 130, 80),  "Ventas");
            Leyenda(20, Color.FromArgb(192, 57, 43),  "Egresos");
            Leyenda(40, Color.FromArgb(26, 74, 138),  "Utilidad");
        }

        private TabPage TabFlujoCaja()
        {
            var tab = new TabPage("Flujo Caja");
            var pnl = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            var btn = BtnCentrado("Abrir proyeccion de flujo de caja 30/60/90 dias",
                Color.FromArgb(20, 70, 130), pnl);
            btn.Click += (s, e) => new FrmFlujoCaja().ShowDialog();
            tab.Controls.Add(pnl); return tab;
        }

        private TabPage TabLibroIVA()
        {
            var tab = new TabPage("Libro IVA");
            var pnl = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            var btn = BtnCentrado("Abrir Libro de Ventas y Compras IVA (SENIAT)",
                Color.FromArgb(140, 20, 20), pnl);
            btn.Click += (s, e) => new FrmLibroIVA().ShowDialog();
            tab.Controls.Add(pnl); return tab;
        }

        private TabPage TabMermas()
        {
            var tab = new TabPage("Mermas");
            var pnl = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            var btn = BtnCentrado("Abrir Control de Mermas y Desperdicios",
                Color.FromArgb(100, 50, 10), pnl);
            btn.Click += (s, e) => new FrmMermas().ShowDialog();
            tab.Controls.Add(pnl); return tab;
        }

        // ══════════════════════════════════════════════════════════
        //  PRONÓSTICO DE REORDEN
        // ══════════════════════════════════════════════════════════
        private TabPage TabReorden()
        {
            var tab = new TabPage("Reorden MP");
            var pnl = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };

            var bar = new Panel { Dock = DockStyle.Top, Height = 48,
                BackColor = Color.FromArgb(248, 235, 220) };
            bar.Controls.Add(Lbl("Ventana (días):", 8, 14));
            var nudDias = new NumericUpDown { Location = new Point(120, 10),
                Size = new Size(60, 26), Minimum = 7, Maximum = 365, Value = 90 };
            bar.Controls.Add(nudDias);
            bar.Controls.Add(Lbl("Lead time:", 190, 14));
            var nudLead = new NumericUpDown { Location = new Point(270, 10),
                Size = new Size(50, 26), Minimum = 1, Maximum = 90, Value = 7 };
            bar.Controls.Add(nudLead);
            bar.Controls.Add(Lbl("Cobertura meta:", 330, 14));
            var nudMeta = new NumericUpDown { Location = new Point(440, 10),
                Size = new Size(50, 26), Minimum = 7, Maximum = 180, Value = 30 };
            bar.Controls.Add(nudMeta);

            var btnCalc = BtnAccion("Calcular", Color.FromArgb(180, 80, 20), new Point(510, 10));
            var btnPrint = BtnAccion("Imprimir", Color.FromArgb(80, 60, 140), new Point(604, 10));
            bar.Controls.AddRange(new Control[] { btnCalc, btnPrint });

            var dgv = Grid(Color.FromArgb(180, 80, 20)); dgv.Dock = DockStyle.Fill;
            List<PronosticoReorden> _datos = new();

            async System.Threading.Tasks.Task CalcularAsync()
            {
                _datos = await EjecutarConCarga(
                    () => DatabaseService.CalcularPronosticoReorden(
                        (int)nudDias.Value, (int)nudLead.Value, (int)nudMeta.Value),
                    btnCalc, btnPrint);

                dgv.DataSource = _datos.Select(p => new {
                    p.CodigoInsumo,
                    p.Descripcion,
                    p.Unidad,
                    Stock    = p.StockActual.ToString("N2"),
                    Min      = p.StockMinimo.ToString("N2"),
                    ConsDia  = p.ConsumoPromDiario.ToString("N4"),
                    DíasCob  = p.DiasCobertura > 9000 ? "∞" : p.DiasCobertura.ToString("N0"),
                    Agotam   = p.FechaAgotamiento?.ToString("dd/MM/yy") ?? "—",
                    Sugerido = p.CantidadSugerida.ToString("N2"),
                    CostoEst = p.CostoEstimado.ToString("C2"),
                    p.Estado
                }).ToList();

                dgv.DataBindingComplete += (s2, e2) =>
                {
                    foreach (DataGridViewRow row in dgv.Rows)
                    {
                        string est = row.Cells["Estado"].Value?.ToString() ?? "";
                        if (est == "URGENTE")
                            row.DefaultCellStyle.BackColor = Color.FromArgb(255, 200, 200);
                        else if (est == "PRONTO")
                            row.DefaultCellStyle.BackColor = Color.FromArgb(255, 245, 190);
                    }
                };
                dgv.Invalidate();
            }

            btnCalc.Click += async (s, e) =>
            {
                try { await CalcularAsync(); }
                catch (Exception ex) { MsgErr(ex); }
            };

            btnPrint.Click += (s, e) => ImprimirPronosticoReorden(_datos);

            pnl.Controls.Add(dgv);
            pnl.Controls.Add(bar);
            tab.Controls.Add(pnl);
            return tab;
        }

        private void ImprimirPronosticoReorden(List<PronosticoReorden> datos)
        {
            if (datos == null || datos.Count == 0)
            {
                MessageBox.Show("Calcula primero el pronóstico.", "Imprimir",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            int filasPorPag = 35;
            int totalPags   = (int)Math.Ceiling(datos.Count / (double)filasPorPag);
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

                var C1 = Color.FromArgb(180, 80, 20);
                g.FillRectangle(new SolidBrush(C1), new Rectangle(x, y, W, 28));
                g.DrawString("PRONÓSTICO DE REORDEN — MATERIAS PRIMAS",
                    new Font("Segoe UI", 12f, FontStyle.Bold), Brushes.White,
                    new RectangleF(x + 8, y + 5, W, 22));
                g.DrawString($"Pág {pagActual + 1}/{totalPags}  —  {DateTime.Now:dd/MM/yyyy}",
                    new Font("Segoe UI", 8f), Brushes.White,
                    new RectangleF(x + W - 200, y + 8, 200, 16),
                    new StringFormat { Alignment = StringAlignment.Far });
                y += 32;

                string[] hdrs = { "Código", "Descripción", "Unid", "Stock", "Cons/Día",
                                  "Días Cob", "Agotam.", "Sugerido", "Costo Est.", "Estado" };
                int[] colW    = { 65, 180, 45, 65, 65, 60, 70, 70, 80, 60 };

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

                var fDato = new Font("Segoe UI", 7.5f);
                int inicio = pagActual * filasPorPag;
                int fin = Math.Min(inicio + filasPorPag, datos.Count);

                for (int r = inicio; r < fin; r++)
                {
                    var p = datos[r];
                    Color bg = p.Estado == "URGENTE" ? Color.FromArgb(255, 210, 210)
                             : p.Estado == "PRONTO"  ? Color.FromArgb(255, 248, 200)
                             : (r - inicio) % 2 == 0 ? Color.White
                             : Color.FromArgb(255, 248, 240);

                    g.FillRectangle(new SolidBrush(bg), new Rectangle(x, y, W, 16));
                    string[] vals = {
                        p.CodigoInsumo, p.Descripcion, p.Unidad,
                        p.StockActual.ToString("N2"), p.ConsumoPromDiario.ToString("N3"),
                        p.DiasCobertura > 9000 ? "∞" : p.DiasCobertura.ToString("N0"),
                        p.FechaAgotamiento?.ToString("dd/MM/yy") ?? "—",
                        p.CantidadSugerida.ToString("N2"),
                        p.CostoEstimado.ToString("C2"), p.Estado
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

                pagActual++;
                e.HasMorePages = pagActual < totalPags;
            };

            using var ppd = new PrintPreviewDialog { Document = pd, WindowState = FormWindowState.Maximized };
            ppd.ShowDialog(this);
        }

        private TabPage TabComparacion()
        {
            var tab = new TabPage("Comparacion");
            var pnl = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            var btn = BtnCentrado("Abrir Comparacion de Periodos",
                Color.FromArgb(50, 50, 120), pnl);
            btn.Click += (s, e) => new FrmComparacion().ShowDialog();
            tab.Controls.Add(pnl); return tab;
        }

        private TabPage TabISLR()
        {
            var tab = new TabPage("ISLR Estimado");
            var pnl = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            var btn = BtnCentrado("Abrir ISLR Estimado — Tarifa 3 (Persona Juridica)",
                Color.FromArgb(26, 74, 138), pnl);
            btn.Click += (s, e) => new FrmISLR().ShowDialog();
            tab.Controls.Add(pnl); return tab;
        }

        // ══════════════════════════════════════════════════════════
        //  GRAFICAS GDI+
        // ══════════════════════════════════════════════════════════
        private static readonly Color[] _paleta =
        {
            Color.FromArgb(30,120,60),  Color.FromArgb(22,55,120),  Color.FromArgb(100,60,160),
            Color.FromArgb(160,100,20), Color.FromArgb(20,110,130), Color.FromArgb(180,60,30),
            Color.FromArgb(60,130,190), Color.FromArgb(140,40,100), Color.FromArgb(80,140,40),
            Color.FromArgb(120,80,20)
        };

        private void PintarTorta(Graphics g,
            List<(string Nombre, decimal Valor)> datos, Size sz)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            if (datos == null || datos.Count == 0)
            {
                g.DrawString("Presiona 'Grafico' para ver", new Font("Segoe UI", 10f), Brushes.Gray, 20, 20);
                return;
            }
            decimal total = datos.Sum(d => d.Valor);
            if (total == 0) return;

            int r = Math.Min(sz.Width / 2 - 100, sz.Height / 2 - 30);
            int cx = r + 20, cy = sz.Height / 2;
            var rect = new RectangleF(cx - r, cy - r, r * 2, r * 2);
            float ang = -90f;

            for (int i = 0; i < datos.Count; i++)
            {
                float sweep = (float)(datos[i].Valor / total * 360);
                using var br = new SolidBrush(_paleta[i % _paleta.Length]);
                g.FillPie(br, rect, ang, sweep);
                g.DrawPie(Pens.White, rect, ang, sweep);
                ang += sweep;
            }

            int ly = 10;
            using var fnt = new Font("Segoe UI", 8f);
            for (int i = 0; i < datos.Count; i++)
            {
                g.FillRectangle(new SolidBrush(_paleta[i % _paleta.Length]),
                    cx + r + 20, ly, 12, 12);
                g.DrawString($"{datos[i].Nombre}  {datos[i].Valor/total:P1}",
                    fnt, Brushes.Black, cx + r + 36, ly - 1);
                ly += 18;
            }
        }

        private void PintarLineasAnuales(Graphics g,
            List<(int Ano, int Mes, decimal Total)> datos, Size sz)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            if (datos == null || datos.Count == 0)
            {
                g.DrawString("Presiona 'Analizar' para ver el grafico",
                    new Font("Segoe UI", 10f), Brushes.Gray, 20, 20);
                return;
            }

            int padL=60, padR=80, padT=20, padB=36;
            int w = sz.Width-padL-padR, h = sz.Height-padT-padB;
            decimal maxVal = datos.Max(d => d.Total); if (maxVal==0) maxVal=1;
            var anos = datos.Select(d=>d.Ano).Distinct().OrderBy(a=>a).ToList();
            string[] nm = {"Ene","Feb","Mar","Abr","May","Jun","Jul","Ago","Sep","Oct","Nov","Dic"};

            using var penEje = new Pen(Color.FromArgb(180,180,190));
            g.DrawLine(penEje, padL, padT, padL, padT+h);
            g.DrawLine(penEje, padL, padT+h, padL+w, padT+h);

            using var fntS = new Font("Segoe UI", 7f);
            for (int m=0; m<12; m++)
                g.DrawString(nm[m], fntS, Brushes.Gray,
                    padL + m*(w/11f) - 8, padT+h+5);

            for (int ai=0; ai<anos.Count; ai++)
            {
                var color = _paleta[ai % _paleta.Length];
                using var pen = new Pen(color, 2f);
                PointF? prev = null;
                for (int m=1; m<=12; m++)
                {
                    decimal val = datos.FirstOrDefault(d=>d.Ano==anos[ai]&&d.Mes==m).Total;
                    float x = padL + (m-1)*(w/11f);
                    float y = padT + h - (float)(val/maxVal*h);
                    var pt = new PointF(x, y);
                    if (prev.HasValue) g.DrawLine(pen, prev.Value, pt);
                    g.FillEllipse(new SolidBrush(color), x-3, y-3, 6, 6);
                    prev = pt;
                }
                g.FillRectangle(new SolidBrush(color), padL+w+5, padT+ai*16, 12, 10);
                g.DrawString(anos[ai].ToString(), fntS,
                    new SolidBrush(color), padL+w+20, padT+ai*16-1);
            }
        }

        // Gráfico de evolución de precios: una línea por producto, eje X por fecha,
        // más una línea punteada de referencia con el promedio del período por producto.
        private void PintarPrecios(Graphics g,
            List<(string Cod, string Desc, Color Col, decimal Prom, List<(DateTime F, decimal P)> Pts)> series,
            Size sz)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            if (series == null || series.Count == 0 || series.All(s => s.Pts.Count == 0))
            {
                g.DrawString("Marca uno o varios productos a la izquierda y presiona 'Graficar'.",
                    new Font("Segoe UI", 10f), Brushes.Gray, 20, 20);
                return;
            }

            var todos = series.SelectMany(s => s.Pts).ToList();
            DateTime minF = todos.Min(p => p.F), maxF = todos.Max(p => p.F);
            decimal minP = todos.Min(p => p.P), maxP = todos.Max(p => p.P);
            double spanDias = (maxF - minF).TotalDays; if (spanDias <= 0) spanDias = 1;
            decimal rango = maxP - minP;
            decimal margen = rango > 0 ? rango * 0.10m : (maxP > 0 ? maxP * 0.10m : 1m);
            decimal yMin = Math.Max(0m, minP - margen), yMax = maxP + margen;
            if (yMax <= yMin) yMax = yMin + 1m;

            int padL = 70, padR = 195, padT = 24, padB = 46;
            int w = sz.Width - padL - padR, h = sz.Height - padT - padB;
            if (w < 60 || h < 60) return;

            using var penEje  = new Pen(Color.FromArgb(180, 180, 190));
            using var penGrid = new Pen(Color.FromArgb(232, 232, 238));
            using var fntS = new Font("Segoe UI", 7.5f);
            using var fntL = new Font("Segoe UI", 8f);

            // Ejes
            g.DrawLine(penEje, padL, padT, padL, padT + h);
            g.DrawLine(penEje, padL, padT + h, padL + w, padT + h);
            g.DrawString("Precio", fntS, Brushes.Gray, 6, padT - 16);

            // Eje Y: 5 líneas guía con etiqueta de precio
            const int nY = 5;
            for (int i = 0; i <= nY; i++)
            {
                float y = padT + h - i * (h / (float)nY);
                decimal val = yMin + (yMax - yMin) * i / nY;
                g.DrawLine(penGrid, padL, y, padL + w, y);
                g.DrawString(val.ToString("N2"), fntS, Brushes.Gray, 4, y - 7);
            }

            // Eje X: hasta 6 marcas de fecha
            int nX = Math.Min(6, Math.Max(1, (int)spanDias));
            for (int i = 0; i <= nX; i++)
            {
                float x = padL + i * (w / (float)nX);
                var fecha = minF.AddDays(spanDias * i / nX);
                g.DrawString(fecha.ToString("dd/MM/yy"), fntS, Brushes.Gray, x - 22, padT + h + 6);
            }

            float MapX(DateTime f) => padL + (float)((f - minF).TotalDays / spanDias * w);
            float MapY(decimal p) => padT + h - (float)((double)((p - yMin) / (yMax - yMin)) * h);

            for (int si = 0; si < series.Count; si++)
            {
                var s = series[si];
                if (s.Pts.Count == 0) continue;
                using var pen = new Pen(s.Col, 2f);
                using var br  = new SolidBrush(s.Col);
                var pts = s.Pts.OrderBy(p => p.F)
                    .Select(p => new PointF(MapX(p.F), MapY(p.P))).ToArray();

                // Línea de referencia: promedio del período (punteada, mismo color)
                using (var penProm = new Pen(Color.FromArgb(150, s.Col), 1.4f)
                       { DashStyle = DashStyle.Dash })
                {
                    float yp = MapY(s.Prom);
                    g.DrawLine(penProm, padL, yp, padL + w, yp);
                }

                if (pts.Length >= 2) g.DrawLines(pen, pts);
                foreach (var pt in pts) g.FillEllipse(br, pt.X - 3, pt.Y - 3, 6, 6);

                // Leyenda: nombre + promedio del período
                int ly = padT + si * 30;
                g.FillRectangle(br, padL + w + 14, ly + 1, 12, 10);
                string nombre = s.Cod + " " + s.Desc;
                if (nombre.Length > 20) nombre = nombre.Substring(0, 19) + "…";
                g.DrawString(nombre, fntL, Brushes.Black, padL + w + 30, ly - 2);
                using var brProm = new SolidBrush(s.Col);
                g.DrawString("prom período: " + s.Prom.ToString("N2"),
                    fntS, brProm, padL + w + 30, ly + 11);
            }
        }

        // ══════════════════════════════════════════════════════════
        //  HELPERS UI
        // ══════════════════════════════════════════════════════════
        private static List<LineaFactura> ObtenerDetallesPorNumero(string numeroF)
        {
            var todas = DatabaseService.ObtenerFacturas();
            int idx   = todas.FindIndex(f => f.NumeroF == numeroF);
            return idx < 0 ? new List<LineaFactura>()
                           : DatabaseService.ObtenerDetallesFactura(idx + 1);
        }

        // Helper: DateTimePicker con label en barra, devuelve siguiente x
        private DateTimePicker DTP(Panel bar, string label, int x, DateTime val, out int nextX)
        {
            bar.Controls.Add(Lbl(label, x, 14)); x += label.Length * 6 + 4;
            var dtp = new DateTimePicker { Location = new Point(x, 10), Size = new Size(110, 26),
                Format = DateTimePickerFormat.Short, Value = val };
            bar.Controls.Add(dtp);
            nextX = x + 118;
            return dtp;
        }

        // Helper: ComboBox de clientes
        private ComboBox Cmb(Panel bar, string label, int x, out int nextX)
        {
            bar.Controls.Add(Lbl(label, x, 14)); x += label.Length * 6 + 4;
            var cmb = new ComboBox { Location = new Point(x, 10), Size = new Size(240, 26),
                DropDownStyle = ComboBoxStyle.DropDownList };
            cmb.Items.Add("Todos los clientes");
            DatabaseService.ObtenerClientes()
                .ForEach(c => cmb.Items.Add(c.Codigo + " — " + c.Descripcion));
            cmb.SelectedIndex = 0;
            bar.Controls.Add(cmb);
            nextX = x + 248;
            return cmb;
        }

        private Button BtnAccion(string texto, Color color, Point loc) => new Button
        {
            Text = texto, Location = loc, Size = new Size(88, 28),
            BackColor = color, ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };

        private Button BtnCentrado(string texto, Color color, Panel padre)
        {
            var btn = new Button { Text = texto, Size = new Size(380, 44),
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                BackColor = color, ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btn.FlatAppearance.BorderSize = 0;
            padre.Controls.Add(btn);
            padre.Resize += (s, e) => btn.Location = new Point(
                (padre.Width - btn.Width) / 2, (padre.Height - btn.Height) / 2);
            return btn;
        }

        private Panel Card(string titulo, string valor, Color color)
        {
            var p = new Panel { Size = new Size(200, 100), Margin = new Padding(8), BackColor = color };
            p.Controls.Add(new Label { Text = titulo, Location = new Point(10, 10), AutoSize = true,
                ForeColor = Color.White, Font = new Font("Segoe UI", 9f) });
            p.Controls.Add(new Label { Text = valor, Location = new Point(10, 40), AutoSize = true,
                ForeColor = Color.White, Font = new Font("Segoe UI", 18f, FontStyle.Bold) });
            return p;
        }

        private DataGridView Grid(Color h)
        {
            var g = new DataGridView { Dock = DockStyle.Fill, ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AllowUserToAddRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White, BorderStyle = BorderStyle.None,
                RowHeadersVisible = false, Font = new Font("Segoe UI", 8.5f) };
            g.ColumnHeadersDefaultCellStyle.BackColor = h;
            g.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            g.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            g.EnableHeadersVisualStyles = false;
            g.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(245, 250, 247);
            return g;
        }

        private Label Lbl(string t, int x, int y) =>
            new Label { Text = t, Location = new Point(x, y), AutoSize = true,
                Font = new Font("Segoe UI", 9f) };

        private void MsgErr(Exception ex) =>
            MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

        // ── Exportar ventas a CSV compatible con Excel ────────────────────
        private static void ExportarVentasExcel(
            List<LineaVentaReporte> lineas, DateTime desde, DateTime hasta)
        {
            using var dlg = new SaveFileDialog
            {
                Title    = "Exportar ventas a Excel",
                Filter   = "Excel CSV (*.csv)|*.csv|Todos los archivos (*.*)|*.*",
                FileName = $"Ventas_{desde:yyyyMMdd}_{hasta:yyyyMMdd}.csv"
            };
            if (dlg.ShowDialog() != DialogResult.OK) return;

            var sb = new System.Text.StringBuilder();

            // BOM UTF-8 para que Excel lo reconozca correctamente
            sb.Append('\uFEFF');

            // Encabezado
            sb.AppendLine("Reporte de Ventas");
            sb.AppendLine($"Período;{desde:dd/MM/yyyy};al;{hasta:dd/MM/yyyy}");
            sb.AppendLine($"Generado;{DateTime.Now:dd/MM/yyyy HH:mm};;");
            sb.AppendLine();

            // Detalle por línea
            sb.AppendLine("Factura;Fecha;Código;Descripción;Cantidad;Precio Unit.;Total");
            decimal totalGeneral = 0;
            foreach (var l in lineas.OrderBy(l => l.Fecha).ThenBy(l => l.NumFactura))
            {
                sb.AppendLine(
                    $"{l.NumFactura};" +
                    $"{l.Fecha:dd/MM/yyyy};" +
                    $"{l.Codigo};" +
                    $"{l.Descripcion.Replace(";", ",")};" +
                    $"{l.Cantidad:N2};" +
                    $"{l.PrecioUnit:N2};" +
                    $"{l.Total:N2}");
                totalGeneral += l.Total;
            }

            sb.AppendLine();
            sb.AppendLine($"TOTAL GENERAL;;;;;; {totalGeneral:N2}");
            sb.AppendLine();

            // Resumen agrupado por producto
            sb.AppendLine("Resumen por Producto");
            sb.AppendLine("Código;Descripción;Unidades vendidas;Total vendido");
            foreach (var g in lineas
                .GroupBy(l => new { l.Codigo, l.Descripcion })
                .OrderByDescending(g => g.Sum(x => x.Total)))
            {
                sb.AppendLine(
                    $"{g.Key.Codigo};" +
                    $"{g.Key.Descripcion.Replace(";", ",")};" +
                    $"{g.Sum(x => x.Cantidad):N2};" +
                    $"{g.Sum(x => x.Total):N2}");
            }

            System.IO.File.WriteAllText(dlg.FileName, sb.ToString(),
                new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: true));

            // Abrir el archivo en Excel directamente
            try
            {
                System.Diagnostics.Process.Start(
                    new System.Diagnostics.ProcessStartInfo
                    { FileName = dlg.FileName, UseShellExecute = true });
            }
            catch { /* si Excel no está instalado, el archivo igual se guardó */ }

            MessageBox.Show(
                $"Exportado correctamente:\n{System.IO.Path.GetFileName(dlg.FileName)}\n\n" +
                $"{lineas.Count} líneas  |  Total: {totalGeneral:N2}",
                "Exportación completada", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }  // fin clase FrmReportes
}
