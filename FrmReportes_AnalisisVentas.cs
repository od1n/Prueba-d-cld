using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SistemaGestion
{
    // ══════════════════════════════════════════════════════════════════
    //  ANÁLISIS DE VENTAS — productos marcados + rango de fechas:
    //  precio promedio (ponderado y simple), mínimo, máximo, mediana,
    //  moda, desviación, costo promedio, utilidad, margen y markup.
    // ══════════════════════════════════════════════════════════════════
    public partial class FrmReportes
    {
        private TabPage TabAnalisisVentas()
        {
            var tab = new TabPage("Análisis Ventas");
            var pnl = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            var colorBase = Color.FromArgb(20, 90, 120);

            // ── Barra superior ───────────────────────────────────────────
            var bar = new Panel { Dock = DockStyle.Top, Height = 48, BackColor = Color.FromArgb(226, 238, 244) };
            var dtpD = DTP(bar, "Desde:", 8, new DateTime(DateTime.Today.Year, 1, 1), out int fx);
            var dtpH = DTP(bar, "Hasta:", fx, DateTime.Today, out fx);
            bar.Controls.Add(Lbl("Documentos:", fx, 14)); fx += 82;
            var cmbTipo = new ComboBox { Location = new Point(fx, 10), Size = new Size(190, 26),
                DropDownStyle = ComboBoxStyle.DropDownList };
            cmbTipo.Items.AddRange(new object[] { "Ambas", "Solo legales (con IVA)", "Solo notas de entrega" });
            cmbTipo.SelectedIndex = 0;
            bar.Controls.Add(cmbTipo); fx += 200;
            var btnCalc = BtnAccion("Calcular", colorBase, new Point(fx, 8)); fx += 94;
            var btnXls  = BtnAccion("Excel", Color.FromArgb(30, 120, 60), new Point(fx, 8)); fx += 94;
            bar.Controls.AddRange(new Control[] { btnCalc, btnXls });

            // ── Panel izquierdo: productos con casillas ─────────────────
            var pnlIzq = new Panel { Dock = DockStyle.Left, Width = 300, BackColor = Color.White, Padding = new Padding(6) };
            var lblHdr = new Label { Text = "Productos (marca uno o varios):", Dock = DockStyle.Top, Height = 22,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            var txtBuscar = new TextBox { Dock = DockStyle.Top, PlaceholderText = "Buscar producto..." };
            var pnlBtns = new Panel { Dock = DockStyle.Top, Height = 32 };
            var btnTodos   = new Button { Text = "Marcar visibles", Location = new Point(0, 3), Size = new Size(130, 26), FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 8.5f) };
            var btnNinguno = new Button { Text = "Desmarcar todos", Location = new Point(136, 3), Size = new Size(130, 26), FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 8.5f) };
            pnlBtns.Controls.AddRange(new Control[] { btnTodos, btnNinguno });
            var clb = new CheckedListBox { Dock = DockStyle.Fill, CheckOnClick = true,
                Font = new Font("Segoe UI", 8.5f), IntegralHeight = false };
            var lblMarcados = new Label { Dock = DockStyle.Bottom, Height = 20, Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(80, 80, 100), Text = "0 productos marcados" };

            var todosProd = DatabaseService.ObtenerInventario()
                .Select(i => (Cod: i.Codigo, Txt: i.Codigo + " — " + i.Descripcion))
                .OrderBy(x => x.Cod, StringComparer.Ordinal).ToList();
            var marcados = new HashSet<string>();
            bool cargando = false;

            void ActualizarContador() => lblMarcados.Text = marcados.Count + " productos marcados";
            void CargarLista(string filtro)
            {
                string f = (filtro ?? "").Trim().ToLower();
                cargando = true;
                clb.BeginUpdate();
                clb.Items.Clear();
                foreach (var p in todosProd)
                    if (f.Length == 0 || p.Txt.ToLower().Contains(f))
                        clb.Items.Add(p.Txt, marcados.Contains(p.Cod));
                clb.EndUpdate();
                cargando = false;
            }
            clb.ItemCheck += (s, e) =>
            {
                if (cargando) return;
                string cod = clb.Items[e.Index].ToString().Split('—')[0].Trim();
                if (e.NewValue == CheckState.Checked) marcados.Add(cod); else marcados.Remove(cod);
                BeginInvoke(new Action(ActualizarContador));
            };
            txtBuscar.TextChanged += (s, e) => CargarLista(txtBuscar.Text);
            btnTodos.Click += (s, e) =>
            {
                foreach (var it in clb.Items) marcados.Add(it.ToString().Split('—')[0].Trim());
                CargarLista(txtBuscar.Text); ActualizarContador();
            };
            btnNinguno.Click += (s, e) => { marcados.Clear(); CargarLista(txtBuscar.Text); ActualizarContador(); };
            CargarLista("");

            pnlIzq.Controls.Add(clb);
            pnlIzq.Controls.Add(lblMarcados);
            pnlIzq.Controls.Add(pnlBtns);
            pnlIzq.Controls.Add(txtBuscar);
            pnlIzq.Controls.Add(lblHdr);

            // ── Indicadores ──────────────────────────────────────────────
            var pnlKpi = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 74, BackColor = Color.FromArgb(245, 248, 251),
                Padding = new Padding(6, 6, 6, 0), WrapContents = false, AutoScroll = true };
            Label Kpi(string titulo, Color col)
            {
                var p = new Panel { Size = new Size(172, 60), Margin = new Padding(4), BackColor = col };
                p.Controls.Add(new Label { Text = titulo, Location = new Point(8, 6), AutoSize = true,
                    ForeColor = Color.White, Font = new Font("Segoe UI", 8.5f) });
                var v = new Label { Text = "—", Location = new Point(8, 26), AutoSize = true,
                    ForeColor = Color.White, Font = new Font("Segoe UI", 13f, FontStyle.Bold) };
                p.Controls.Add(v);
                pnlKpi.Controls.Add(p);
                return v;
            }
            var kVentas   = Kpi("Ventas",                Color.FromArgb(30, 120, 70));
            var kUnid     = Kpi("Unidades",              Color.FromArgb(20, 90, 120));
            var kPrecio   = Kpi("Precio promedio",       Color.FromArgb(70, 70, 150));
            var kCosto    = Kpi("Costo promedio",        Color.FromArgb(150, 90, 30));
            var kUtil     = Kpi("Utilidad",              Color.FromArgb(30, 120, 70));
            var kMargen   = Kpi("Margen sobre venta",    Color.FromArgb(110, 60, 140));
            var kFact     = Kpi("Facturas / clientes",   Color.FromArgb(90, 90, 100));

            // ── Resultados en pestañas ───────────────────────────────────
            var tabsRes = new TabControl { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 9f) };
            var tpProd  = new TabPage("Por producto");
            var tpMes   = new TabPage("Por mes");
            var tpDet   = new TabPage("Detalle de ventas");
            var dgvProd = Grid(colorBase); var dgvMes = Grid(colorBase); var dgvDet = Grid(colorBase);
            foreach (var g in new[] { dgvProd, dgvMes, dgvDet })
            {
                g.Dock = DockStyle.Fill;
                g.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
                g.ScrollBars = ScrollBars.Both;
            }
            tpProd.Controls.Add(dgvProd); tpMes.Controls.Add(dgvMes); tpDet.Controls.Add(dgvDet);
            tabsRes.TabPages.AddRange(new[] { tpProd, tpMes, tpDet });
            var lblNota = new Label { Dock = DockStyle.Bottom, Height = 34, Padding = new Padding(8, 2, 8, 2),
                Font = new Font("Segoe UI", 8f), ForeColor = Color.FromArgb(80, 80, 100),
                Text = "Precio promedio = ventas ÷ unidades (ponderado). Promedio simple, mínimo, máximo, mediana, moda y desviación " +
                       "se calculan sobre el precio de cada línea de factura. Costo = costo promedio del Kardex al momento de cada venta. " +
                       "Margen = utilidad ÷ ventas; markup = utilidad ÷ costo. Montos en USD." };

            var pnlDer = new Panel { Dock = DockStyle.Fill };
            pnlDer.Controls.Add(tabsRes);
            pnlDer.Controls.Add(lblNota);
            pnlDer.Controls.Add(pnlKpi);

            // ── Formato de grillas ───────────────────────────────────────
            string M(decimal v) => v.ToString("C2");
            string P(decimal v) => v.ToString("N1") + " %";
            string N(decimal v) => v == Math.Round(v) ? v.ToString("N0") : v.ToString("N2");

            void Formatear(DataGridView g, Dictionary<string, int> anchos)
            {
                foreach (DataGridViewColumn c in g.Columns)
                {
                    c.Width = anchos.TryGetValue(c.Name, out var w) ? w : 95;
                    if (c.Name != "Código" && c.Name != "Descripción" && c.Name != "Mes" && c.Name != "Cliente"
                        && c.Name != "Documento" && c.Name != "Tipo" && c.Name != "Fecha"
                        && c.Name != "Primera" && c.Name != "Última")
                        c.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                }
                if (g.Columns.Count > 0) g.Columns[0].Frozen = true;
            }
            void ResaltarTotal(DataGridView g)
            {
                if (g.Rows.Count == 0) return;
                var row = g.Rows[g.Rows.Count - 1];
                row.DefaultCellStyle.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
                row.DefaultCellStyle.BackColor = Color.FromArgb(222, 234, 244);
            }

            List<LineaAnalisisVenta> ultLineas = new();
            List<EstadisticaVentaProducto> ultStats = new();
            EstadisticaVentaProducto ultTotal = null;

            btnCalc.Click += async (s, e) =>
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
                    var d = dtpD.Value.Date; var h = dtpH.Value.Date;
                    string modo = cmbTipo.SelectedIndex == 1 ? "IVA" : cmbTipo.SelectedIndex == 2 ? "NOTA" : "TODAS";
                    var lineas = await EjecutarConCarga(
                        () => DatabaseService.ObtenerLineasAnalisisVentas(cods, d, h, modo), btnCalc, btnXls);
                    ultLineas = lineas;
                    if (lineas.Count == 0)
                    {
                        dgvProd.DataSource = null; dgvMes.DataSource = null; dgvDet.DataSource = null;
                        foreach (var k in new[] { kVentas, kUnid, kPrecio, kCosto, kUtil, kMargen, kFact }) k.Text = "—";
                        MessageBox.Show("No hay ventas de esos productos en el rango indicado.",
                            "Sin datos", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }

                    decimal totalVentas = lineas.Sum(l => l.Venta);
                    var stats = lineas.GroupBy(l => l.Codigo)
                        .Select(gp => DatabaseService.CalcularEstadisticaVentas(gp, gp.Key, gp.First().Descripcion, totalVentas))
                        .OrderByDescending(x => x.Ventas).ToList();
                    var total = DatabaseService.CalcularEstadisticaVentas(lineas, "TOTAL", $"{stats.Count} productos", totalVentas);
                    ultStats = stats; ultTotal = total;

                    // Indicadores
                    kVentas.Text = M(total.Ventas);
                    kUnid.Text   = N(total.Unidades);
                    kPrecio.Text = M(total.PrecioPromPond);
                    kCosto.Text  = M(total.CostoProm);
                    kUtil.Text   = M(total.Utilidad);
                    kMargen.Text = P(total.MargenPct);
                    kFact.Text   = $"{total.Facturas} / {total.Clientes}";

                    // Por producto (+ fila de total)
                    var filasProd = stats.Concat(new[] { total }).Select(x => new {
                        Código = x.Codigo, Descripción = x.Descripcion,
                        Unidades = N(x.Unidades), Facturas = x.Facturas, Clientes = x.Clientes,
                        Ventas = M(x.Ventas), Participación = P(x.PctVentas),
                        PrecioPromedio = M(x.PrecioPromPond), PromedioSimple = M(x.PrecioPromSimple),
                        Mínimo = M(x.PrecioMin), Máximo = M(x.PrecioMax), Mediana = M(x.PrecioMediana),
                        Moda = M(x.PrecioModa), Desviación = M(x.DesvEstandar), Variación = P(x.CoefVariacion),
                        ÚltimoPrecio = M(x.UltimoPrecio),
                        CostoPromedio = M(x.CostoProm), CostoTotal = M(x.CostoTotal),
                        Utilidad = M(x.Utilidad), UtilidadUnidad = M(x.UtilidadUnidad),
                        Margen = P(x.MargenPct), Markup = P(x.MarkupPct),
                        UnidPorFactura = N(Math.Round(x.UnidadesPorFactura, 2)),
                        Primera = x.PrimeraVenta.ToString("dd/MM/yyyy"), Última = x.UltimaVenta.ToString("dd/MM/yyyy"),
                        SinCostoKardex = x.LineasSinCostoKardex
                    }).ToList();
                    dgvProd.DataSource = filasProd;
                    Formatear(dgvProd, new Dictionary<string, int> { ["Código"] = 90, ["Descripción"] = 230 });
                    var enc = new Dictionary<string, string> {
                        ["PrecioPromedio"] = "Precio prom.", ["PromedioSimple"] = "Prom. simple",
                        ["ÚltimoPrecio"] = "Último precio", ["CostoPromedio"] = "Costo prom.",
                        ["CostoTotal"] = "Costo total", ["UtilidadUnidad"] = "Utilidad x unidad",
                        ["UnidPorFactura"] = "Unid. x factura", ["SinCostoKardex"] = "Líneas sin costo Kardex",
                        ["Variación"] = "Coef. variación", ["Primera"] = "Primera venta", ["Última"] = "Última venta" };
                    foreach (var kv in enc) if (dgvProd.Columns.Contains(kv.Key)) dgvProd.Columns[kv.Key].HeaderText = kv.Value;
                    ResaltarTotal(dgvProd);

                    // Por mes y producto
                    var filasMes = lineas.GroupBy(l => new { Mes = new DateTime(l.Fecha.Year, l.Fecha.Month, 1), l.Codigo })
                        .OrderBy(gp => gp.Key.Mes).ThenByDescending(gp => gp.Sum(x => x.Venta))
                        .Select(gp => DatabaseService.CalcularEstadisticaVentas(gp, gp.Key.Codigo, gp.First().Descripcion, totalVentas)
                            is var x ? new {
                                Mes = gp.Key.Mes.ToString("MM/yyyy"), Código = x.Codigo, Descripción = x.Descripcion,
                                Unidades = N(x.Unidades), Facturas = x.Facturas, Ventas = M(x.Ventas),
                                PrecioPromedio = M(x.PrecioPromPond), Mínimo = M(x.PrecioMin), Máximo = M(x.PrecioMax),
                                Mediana = M(x.PrecioMediana), CostoPromedio = M(x.CostoProm),
                                Utilidad = M(x.Utilidad), Margen = P(x.MargenPct) } : null)
                        .ToList();
                    dgvMes.DataSource = filasMes;
                    Formatear(dgvMes, new Dictionary<string, int> { ["Mes"] = 70, ["Código"] = 90, ["Descripción"] = 230 });
                    if (dgvMes.Columns.Contains("PrecioPromedio")) dgvMes.Columns["PrecioPromedio"].HeaderText = "Precio prom.";
                    if (dgvMes.Columns.Contains("CostoPromedio"))  dgvMes.Columns["CostoPromedio"].HeaderText  = "Costo prom.";

                    // Detalle
                    dgvDet.DataSource = lineas.Select(l => new {
                        Fecha = l.Fecha.ToString("dd/MM/yyyy"), Documento = l.NumeroF, Tipo = l.TipoDoc,
                        Cliente = l.Cliente, Código = l.Codigo, Descripción = l.Descripcion,
                        Cantidad = N(l.Cantidad), Precio = M(l.Precio), CostoUnit = M(l.CostoUnit),
                        Venta = M(l.Venta), Costo = M(l.Costo), Utilidad = M(l.Utilidad),
                        Margen = P(l.Venta != 0 ? l.Utilidad / l.Venta * 100 : 0),
                        OrigenCosto = l.CostoDeKardex ? "Kardex" : "Costo actual"
                    }).ToList();
                    Formatear(dgvDet, new Dictionary<string, int> { ["Fecha"] = 80, ["Documento"] = 90, ["Tipo"] = 100,
                        ["Cliente"] = 200, ["Código"] = 90, ["Descripción"] = 220 });
                    if (dgvDet.Columns.Contains("CostoUnit"))   dgvDet.Columns["CostoUnit"].HeaderText   = "Costo unit.";
                    if (dgvDet.Columns.Contains("OrigenCosto")) dgvDet.Columns["OrigenCosto"].HeaderText = "Origen del costo";
                }
                catch (Exception ex) { MsgErr(ex); }
            };

            btnXls.Click += (s, e) =>
            {
                try
                {
                    if (ultStats.Count == 0 || ultTotal == null)
                    {
                        MessageBox.Show("Primero presiona \"Calcular\".", "Sin datos",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }
                    ExportarAnalisisVentas(ultStats, ultTotal, ultLineas, dtpD.Value.Date, dtpH.Value.Date,
                        cmbTipo.SelectedItem?.ToString() ?? "");
                }
                catch (Exception ex) { MsgErr(ex); }
            };

            pnl.Controls.Add(pnlDer);
            pnl.Controls.Add(pnlIzq);
            pnl.Controls.Add(bar);
            tab.Controls.Add(pnl);
            return tab;
        }

        // ── Exportar el análisis a CSV (se abre en Excel) ───────────────
        private static void ExportarAnalisisVentas(List<EstadisticaVentaProducto> stats,
            EstadisticaVentaProducto total, List<LineaAnalisisVenta> lineas,
            DateTime desde, DateTime hasta, string documentos)
        {
            using var dlg = new SaveFileDialog
            {
                Title    = "Exportar análisis de ventas a Excel",
                Filter   = "Excel CSV (*.csv)|*.csv|Todos los archivos (*.*)|*.*",
                FileName = $"AnalisisVentas_{desde:yyyyMMdd}_{hasta:yyyyMMdd}.csv"
            };
            if (dlg.ShowDialog() != DialogResult.OK) return;

            string T(string s) => (s ?? "").Replace(";", ",");
            var sb = new System.Text.StringBuilder();
            sb.Append('﻿');
            sb.AppendLine("Análisis de ventas por producto (USD)");
            sb.AppendLine($"Período;{desde:dd/MM/yyyy};al;{hasta:dd/MM/yyyy};Documentos;{T(documentos)}");
            sb.AppendLine($"Generado;{DateTime.Now:dd/MM/yyyy HH:mm}");
            sb.AppendLine();
            sb.AppendLine("Código;Descripción;Unidades;Facturas;Clientes;Ventas;Participación %;Precio promedio;" +
                          "Promedio simple;Mínimo;Máximo;Mediana;Moda;Desviación;Coef. variación %;Último precio;" +
                          "Costo promedio;Costo total;Utilidad;Utilidad x unidad;Margen %;Markup %;Unid. x factura;" +
                          "Primera venta;Última venta");
            foreach (var x in stats.Concat(new[] { total }))
                sb.AppendLine(string.Join(";", new[] {
                    T(x.Codigo), T(x.Descripcion), x.Unidades.ToString("N2"), x.Facturas.ToString(), x.Clientes.ToString(),
                    x.Ventas.ToString("N2"), x.PctVentas.ToString("N1"), x.PrecioPromPond.ToString("N2"),
                    x.PrecioPromSimple.ToString("N2"), x.PrecioMin.ToString("N2"), x.PrecioMax.ToString("N2"),
                    x.PrecioMediana.ToString("N2"), x.PrecioModa.ToString("N2"), x.DesvEstandar.ToString("N2"),
                    x.CoefVariacion.ToString("N1"), x.UltimoPrecio.ToString("N2"), x.CostoProm.ToString("N2"),
                    x.CostoTotal.ToString("N2"), x.Utilidad.ToString("N2"), x.UtilidadUnidad.ToString("N2"),
                    x.MargenPct.ToString("N1"), x.MarkupPct.ToString("N1"), x.UnidadesPorFactura.ToString("N2"),
                    x.PrimeraVenta.ToString("dd/MM/yyyy"), x.UltimaVenta.ToString("dd/MM/yyyy") }));
            sb.AppendLine();
            sb.AppendLine("Detalle de ventas");
            sb.AppendLine("Fecha;Documento;Tipo;Cliente;Código;Descripción;Cantidad;Precio;Costo unit.;Venta;Costo;Utilidad;Origen del costo");
            foreach (var l in lineas)
                sb.AppendLine(string.Join(";", new[] {
                    l.Fecha.ToString("dd/MM/yyyy"), T(l.NumeroF), T(l.TipoDoc), T(l.Cliente), T(l.Codigo), T(l.Descripcion),
                    l.Cantidad.ToString("N2"), l.Precio.ToString("N2"), l.CostoUnit.ToString("N2"),
                    l.Venta.ToString("N2"), l.Costo.ToString("N2"), l.Utilidad.ToString("N2"),
                    l.CostoDeKardex ? "Kardex" : "Costo actual" }));

            System.IO.File.WriteAllText(dlg.FileName, sb.ToString(), new System.Text.UTF8Encoding(false));
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(dlg.FileName) { UseShellExecute = true });
            }
            catch { /* si no hay Excel instalado, el archivo queda guardado igual */ }
        }
    }
}
