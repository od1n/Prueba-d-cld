#nullable enable
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace SistemaGestion
{
    // ════════════════════════════════════════════════════════════════════════
    //  FrmISLR — ISLR Estimado (Tarifa 3, Persona Juridica)
    //
    //  4 tabs:
    //    1. Parametros   — año fiscal, UT, % rebaja, ISLR año anterior
    //    2. Calculo      — resumen de ingresos/costos y calculo por tramos
    //    3. Anticipos    — tabla mensual de anticipos del ejercicio
    //    4. Retenciones  — CRUD de retenciones recibidas de clientes
    // ════════════════════════════════════════════════════════════════════════
    public class FrmISLR : FormBase
    {
        // Paleta
        private static readonly Color CFondo  = Color.FromArgb(244, 246, 250);
        private static readonly Color CAzul   = Color.FromArgb(26, 74, 138);
        private static readonly Color CText   = Color.FromArgb(30, 34, 51);
        private static readonly Color CTenue  = Color.FromArgb(122, 131, 149);
        private static readonly Color CVerde  = Color.FromArgb(15, 110, 86);
        private static readonly Color CRojo   = Color.FromArgb(198, 40, 40);
        private static readonly Color CAmarillo = Color.FromArgb(210, 153, 34);

        private int           _anio = DateTime.Today.Year;
        private ConfigISLR    _cfg  = new();
        private ResultadoISLR _res  = new();

        // Controles pestaña Parametros
        private NumericUpDown numAnio  = null!, numUT    = null!,
                              numRebaja= null!, numISLRAnt= null!, numRetAnt = null!;

        // Controles pestaña Calculo
        private Panel pnlCalculo = null!;

        // Controles pestaña Anticipos
        private DataGridView dgvAnt = null!;

        // Controles pestaña Retenciones
        private DataGridView dgvRet = null!;
        private List<RetencionISLR> _retenciones = new();

        public FrmISLR()
        {
            Text          = "ISLR Estimado — Tarifa 3 (Persona Juridica)";
            Size          = new Size(980, 700);
            MinimumSize   = new Size(860, 580);
            StartPosition = FormStartPosition.CenterParent;
            BackColor     = CFondo;

            BuildUI();
            CargarAnio(_anio);
        }

        // ─────────────────────────────────────────────────────────────────
        // UI
        // ─────────────────────────────────────────────────────────────────
        private void BuildUI()
        {
            // Header
            var hdr = new Panel { Dock = DockStyle.Top, Height = 56, BackColor = CAzul };
            hdr.Controls.Add(new Label
            {
                Text = "ISLR Estimado — Tarifa 3", ForeColor = Color.White,
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                AutoSize = true, Location = new Point(16, 14)
            });
            Controls.Add(hdr);

            // Toolbar superior
            var tool = new Panel { Dock = DockStyle.Top, Height = 44, BackColor = Color.White,
                Padding = new Padding(8, 6, 8, 6) };
            tool.Controls.Add(new Label { Text = "Ejercicio:", AutoSize = true,
                Font = new Font("Segoe UI", 9f), Location = new Point(8, 12) });
            var nudAnioSel = new NumericUpDown
            {
                Minimum = 2020, Maximum = 2099, Value = _anio,
                Font = new Font("Segoe UI", 9f), Location = new Point(70, 8), Width = 70
            };
            nudAnioSel.ValueChanged += (s, e) => CargarAnio((int)nudAnioSel.Value);
            tool.Controls.Add(nudAnioSel);

            var btnCalc = Btn("Calcular", CAzul);
            btnCalc.Location = new Point(155, 7);
            btnCalc.Click    += (s, e) => Calcular();
            tool.Controls.Add(btnCalc);

            var btnPdf = Btn("Exportar PDF", Color.FromArgb(140, 50, 20));
            btnPdf.Location = new Point(275, 7);
            btnPdf.Click    += (s, e) => ExportarPdf();
            tool.Controls.Add(btnPdf);

            var btnAyuda = Btn("? Ayuda", CTenue);
            btnAyuda.Location = new Point(870, 7);
            btnAyuda.Click    += (s, e) => AyudaService.Mostrar(this, "FrmISLR");
            tool.Controls.Add(btnAyuda);

            Controls.Add(tool);

            // TabControl
            var tab = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9f)
            };
            tab.TabPages.Add(BuildTabParametros());
            tab.TabPages.Add(BuildTabCalculo());
            tab.TabPages.Add(BuildTabAnticipos());
            tab.TabPages.Add(BuildTabRetenciones());
            Controls.Add(tab);
        }

        // ── Tab 1: Parametros ──────────────────────────────────────────────
        private TabPage BuildTabParametros()
        {
            var tp = new TabPage("  Parámetros  ") { BackColor = CFondo, Padding = new Padding(16) };

            var grp = new GroupBox
            {
                Text = "Configuración del ejercicio fiscal",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Location = new Point(20, 20), Size = new Size(500, 290),
                ForeColor = CAzul
            };

            int y = 28;
            numAnio   = AddField(grp, "Año fiscal:",          ref y, 2020, 2099, _anio);
            numUT     = AddField(grp, "Valor UT (Bs):",       ref y, 0, 9999999, 0,   decimals: 2);
            numRebaja = AddField(grp, "% Rebaja Art. 57:",    ref y, 0, 100, 10,       decimals: 1);
            numISLRAnt= AddField(grp, "ISLR año anterior (Bs):", ref y, 0, 99999999, 0, decimals: 2);
            numRetAnt = AddField(grp, "Retenciones año ant. (Bs):", ref y, 0, 99999999, 0, decimals: 2);

            var btnGuardar = new Button
            {
                Text = "Guardar parámetros", BackColor = CAzul, ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Location = new Point(12, y + 10), Size = new Size(180, 34)
            };
            btnGuardar.Click += GuardarParametros;
            grp.Controls.Add(btnGuardar);

            tp.Controls.Add(grp);

            // Nota informativa
            var nota = new Label
            {
                Text = "NOTA: El valor de la UT lo fija el SENIAT mediante Gaceta Oficial.\n" +
                       "La rebaja del 10% (Art. 57) aplica a actividades industriales, agropecuarias\n" +
                       "y de construccion. Verifique con su asesor fiscal.\n\n" +
                       "Los anticipos se calculan como:\n" +
                       "  (ISLR año anterior - Retenciones año anterior) / 12\n" +
                       "y se pagan durante los primeros 15 dias de cada mes (Art. 83 LISR).",
                Location = new Point(540, 20), AutoSize = false,
                Size = new Size(380, 180),
                ForeColor = CTenue, Font = new Font("Segoe UI", 8.5f),
                BackColor = Color.White, Padding = new Padding(10)
            };
            tp.Controls.Add(nota);

            return tp;
        }

        // ── Tab 2: Calculo ─────────────────────────────────────────────────
        private TabPage BuildTabCalculo()
        {
            var tp = new TabPage("  Cálculo  ") { BackColor = CFondo };
            pnlCalculo = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
            tp.Controls.Add(pnlCalculo);
            return tp;
        }

        private void RenderizarCalculo()
        {
            pnlCalculo.Controls.Clear();
            var r = _res;

            int y = 16, x = 20;

            // ── Bloque ingresos y costos ──────────────────────────────────
            y = Seccion(pnlCalculo, "Ingresos y Costos del Ejercicio", x, y, 900);
            y = Fila(pnlCalculo, "Ingresos brutos (ventas)",    r.IngresosBrutos,  "USD", x, y);
            y = Fila(pnlCalculo, "Costos de compras",           r.CostosCompras,   "USD", x, y, negativo: true);
            y = Fila(pnlCalculo, "Gastos operativos y egresos", r.CostosGastos,    "USD", x, y, negativo: true);
            y = Fila(pnlCalculo, "Nómina pagada",               r.CostosNomina,    "USD", x, y, negativo: true);
            y = FilaTotal(pnlCalculo, "Enriquecimiento Neto",   r.EnriquecimientoNeto, "USD", x, y,
                positivo: r.EnriquecimientoNeto >= 0);
            y += 8;

            // ── Conversion a Bs y UT ──────────────────────────────────────
            y = Seccion(pnlCalculo, "Conversión a Unidades Tributarias", x, y, 900);
            y = Fila(pnlCalculo, $"Tasa BCV ({_anio}-12-31 o ultima disponible)", r.TasaBCV, "Bs/USD", x, y);
            y = Fila(pnlCalculo, "Enriquecimiento Neto en Bs",     r.EnriquecimientoBS, "Bs", x, y);
            y = Fila(pnlCalculo, $"Valor UT",                      r.ValorUT,           "Bs", x, y);
            y = FilaTotal(pnlCalculo, "Enriquecimiento Neto en UT", r.EnriquecimientoUT, "UT", x, y,
                positivo: r.EnriquecimientoUT >= 0);
            y += 8;

            // ── Calculo impuesto Tarifa 3 ──────────────────────────────────
            y = Seccion(pnlCalculo, "Impuesto según Tarifa 3 (Art. 52 LISR)", x, y, 900);
            if (r.EnriquecimientoUT > 0)
            {
                if (r.EnriquecimientoUT <= 2000)
                    y = Fila(pnlCalculo, $"{r.EnriquecimientoUT:N2} UT × 15%",
                        r.EnriquecimientoUT * 0.15m, "UT", x, y);
                else if (r.EnriquecimientoUT <= 3000)
                {
                    y = Fila(pnlCalculo, "2.000 UT × 15%",                 300m, "UT", x, y);
                    y = Fila(pnlCalculo, $"{r.EnriquecimientoUT - 2000:N2} UT × 22%",
                        (r.EnriquecimientoUT - 2000m) * 0.22m, "UT", x, y);
                }
                else
                {
                    y = Fila(pnlCalculo, "2.000 UT × 15%",  300m, "UT", x, y);
                    y = Fila(pnlCalculo, "1.000 UT × 22%",  220m, "UT", x, y);
                    y = Fila(pnlCalculo, $"{r.EnriquecimientoUT - 3000:N2} UT × 34%",
                        (r.EnriquecimientoUT - 3000m) * 0.34m, "UT", x, y);
                }
            }
            y = FilaTotal(pnlCalculo, "Impuesto calculado",  r.ImpuestoUT, "UT", x, y, positivo: false);
            y = Fila(pnlCalculo,      "Impuesto en Bs",      r.ImpuestoBS, "Bs", x, y);
            y += 8;

            // ── Ajustes ───────────────────────────────────────────────────
            y = Seccion(pnlCalculo, "Ajustes y Determinación del ISLR a Pagar", x, y, 900);
            y = Fila(pnlCalculo, "Impuesto bruto",                  r.ImpuestoBS,    "Bs", x, y);
            y = Fila(pnlCalculo, $"Rebaja Art. 57 ({r.PctRebaja}%)", r.RebajaBS,     "Bs", x, y, negativo: true);
            y = Fila(pnlCalculo, "Retenciones recibidas",           r.RetencionesBS, "Bs", x, y, negativo: true);
            y = FilaTotal(pnlCalculo, "ISLR Neto a Pagar (Bs)",     r.ISLRNetoBS, "Bs", x, y,
                positivo: r.ISLRNetoBS == 0);
            y = FilaTotal(pnlCalculo, "ISLR Neto a Pagar (USD)",    r.ISLRNetoUSD, "USD", x, y,
                positivo: r.ISLRNetoUSD == 0);

            pnlCalculo.AutoScrollMinSize = new Size(0, y + 40);
        }

        // ── Tab 3: Anticipos ───────────────────────────────────────────────
        private TabPage BuildTabAnticipos()
        {
            var tp = new TabPage("  Anticipos  ") { BackColor = CFondo, Padding = new Padding(12) };

            var lbl = new Label
            {
                Text = "Anticipos mensuales (Art. 83 LISR) — " +
                       "Pagaderos los primeros 15 días de cada mes",
                Font = new Font("Segoe UI", 9f, FontStyle.Italic),
                ForeColor = CTenue, AutoSize = true, Location = new Point(12, 12)
            };
            tp.Controls.Add(lbl);

            dgvAnt = BuildGrid(new[] { "Mes", "Vencimiento", "Monto (Bs)", "Estado" });
            dgvAnt.Location = new Point(12, 40);
            dgvAnt.Size     = new Size(680, 420);
            dgvAnt.ReadOnly = true;
            tp.Controls.Add(dgvAnt);

            return tp;
        }

        private void RenderizarAnticipos()
        {
            dgvAnt.Rows.Clear();
            decimal mensual = _res.AnticipoMensualBS;
            var meses = new[] { "Enero","Febrero","Marzo","Abril","Mayo","Junio",
                                "Julio","Agosto","Septiembre","Octubre","Noviembre","Diciembre" };
            for (int m = 1; m <= 12; m++)
            {
                var venc = new DateTime(_anio, m, 15);
                string estado = venc < DateTime.Today ? "Vencido" :
                                venc.Month == DateTime.Today.Month &&
                                venc.Year  == DateTime.Today.Year  ? "Este mes" : "Pendiente";
                int idx = dgvAnt.Rows.Add(meses[m - 1], venc.ToString("dd/MM/yyyy"),
                    mensual.ToString("N2"), estado);
                dgvAnt.Rows[idx].DefaultCellStyle.ForeColor =
                    estado == "Vencido" ? CRojo :
                    estado == "Este mes" ? CAmarillo : CText;
            }
        }

        // ── Tab 4: Retenciones ─────────────────────────────────────────────
        private TabPage BuildTabRetenciones()
        {
            var tp = new TabPage("  Retenciones recibidas  ") { BackColor = CFondo };

            var bar = new Panel { Dock = DockStyle.Top, Height = 42, BackColor = Color.White,
                Padding = new Padding(8, 6, 8, 6) };
            var btnNueva = Btn("Nueva retención", CVerde);
            btnNueva.Location = new Point(8, 6);
            btnNueva.Click    += (s, e) => EditarRetencion(null);
            bar.Controls.Add(btnNueva);

            var btnEditar = Btn("Editar", CAzul);
            btnEditar.Location = new Point(150, 6);
            btnEditar.Click    += (s, e) => {
                if (dgvRet.CurrentRow?.Tag is RetencionISLR r) EditarRetencion(r);
            };
            bar.Controls.Add(btnEditar);

            var btnElim = Btn("Eliminar", CRojo);
            btnElim.Location = new Point(240, 6);
            btnElim.Click    += (s, e) => EliminarRetencionSeleccionada();
            bar.Controls.Add(btnElim);

            tp.Controls.Add(bar);

            dgvRet = BuildGrid(new[] { "Fecha", "Cliente", "N° Factura",
                "Monto Base (Bs)", "% Ret.", "Monto Retenido (Bs)", "Comprobante" });
            dgvRet.Dock = DockStyle.Fill;
            tp.Controls.Add(dgvRet);

            return tp;
        }

        // ─────────────────────────────────────────────────────────────────
        // Logica
        // ─────────────────────────────────────────────────────────────────
        private void CargarAnio(int anio)
        {
            _anio = anio;
            _cfg  = DatabaseService.ObtenerConfigISLR(anio);

            numAnio.Value    = anio;
            numUT.Value      = (decimal)Math.Min((double)_cfg.ValorUT, (double)numUT.Maximum);
            numRebaja.Value  = (decimal)Math.Min((double)_cfg.PctRebaja, (double)numRebaja.Maximum);
            numISLRAnt.Value = (decimal)Math.Min((double)_cfg.ISLRAnioAnterior, (double)numISLRAnt.Maximum);
            numRetAnt.Value  = (decimal)Math.Min((double)_cfg.RetencionesAnio, (double)numRetAnt.Maximum);

            CargarRetenciones();
            Calcular();
        }

        private void GuardarParametros(object? sender, EventArgs e)
        {
            _cfg.Anio             = (int)numAnio.Value;
            _cfg.ValorUT          = numUT.Value;
            _cfg.PctRebaja        = numRebaja.Value;
            _cfg.ISLRAnioAnterior = numISLRAnt.Value;
            _cfg.RetencionesAnio  = numRetAnt.Value;
            DatabaseService.GuardarConfigISLR(_cfg);
            MessageBox.Show("Parámetros guardados.", "ISLR", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void Calcular()
        {
            // Leer valores actuales del formulario (pueden no estar guardados aun)
            _cfg.ValorUT          = numUT.Value;
            _cfg.PctRebaja        = numRebaja.Value;
            _cfg.ISLRAnioAnterior = numISLRAnt.Value;
            _cfg.RetencionesAnio  = numRetAnt.Value;

            try
            {
                _res = DatabaseService.CalcularISLR(_anio, _cfg);
                RenderizarCalculo();
                RenderizarAnticipos();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al calcular: " + ex.Message, "ISLR",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CargarRetenciones()
        {
            _retenciones = DatabaseService.ObtenerRetencionesISLR(_anio);
            dgvRet.Rows.Clear();
            foreach (var r in _retenciones)
            {
                int i = dgvRet.Rows.Add(r.Fecha.ToString("dd/MM/yyyy"), r.Cliente,
                    r.NumFactura, r.MontoBase.ToString("N2"),
                    r.PctRetencion.ToString("N1") + "%",
                    r.MontoRetenido.ToString("N2"), r.Comprobante);
                dgvRet.Rows[i].Tag = r;
            }
        }

        private void EditarRetencion(RetencionISLR? ret)
        {
            var dlg = new DlgRetencionISLR(ret ?? new RetencionISLR { Anio = _anio, PctRetencion = 3 });
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                DatabaseService.GuardarRetencionISLR(dlg.Retencion);
                CargarRetenciones();
                Calcular();
            }
        }

        private void EliminarRetencionSeleccionada()
        {
            if (dgvRet.CurrentRow?.Tag is not RetencionISLR r) return;
            if (MessageBox.Show($"Eliminar retención de {r.Cliente}?", "Confirmar",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            DatabaseService.EliminarRetencionISLR(r.Id);
            CargarRetenciones();
            Calcular();
        }

        private void ExportarPdf()
        {
            if (_cfg.ValorUT <= 0)
            {
                MessageBox.Show("Configure el valor de la UT antes de exportar.",
                    "ISLR", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            try
            {
                string ruta = PdfService.GuardarRuta($"ISLR_{_anio}.pdf");
                if (string.IsNullOrEmpty(ruta)) return;
                PdfService_ISLR.Generar(_res, _retenciones, ruta);
                PdfService.Abrir(ruta);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al generar PDF: " + ex.Message, "ISLR",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ─────────────────────────────────────────────────────────────────
        // Helpers UI
        // ─────────────────────────────────────────────────────────────────
        private Button Btn(string text, Color back)
        {
            return new Button
            {
                Text = text, BackColor = back, ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Size = new Size(115, 28), Cursor = Cursors.Hand
            };
        }

        private NumericUpDown AddField(GroupBox grp, string label, ref int y,
            decimal min, decimal max, decimal val, int decimals = 0)
        {
            grp.Controls.Add(new Label
            {
                Text = label, AutoSize = true, ForeColor = CText,
                Font = new Font("Segoe UI", 8.5f), Location = new Point(12, y + 3)
            });
            var nud = new NumericUpDown
            {
                Minimum = min, Maximum = max, Value = Math.Min(val, max),
                DecimalPlaces = decimals, ThousandsSeparator = true,
                Font = new Font("Segoe UI", 9f),
                Location = new Point(240, y), Size = new Size(180, 26)
            };
            grp.Controls.Add(nud);
            y += 34;
            return nud;
        }

        private DataGridView BuildGrid(string[] cols)
        {
            var dgv = new DataGridView
            {
                ReadOnly = true, AllowUserToAddRows = false,
                RowHeadersVisible = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor = Color.White, BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 8.5f)
            };
            foreach (var col in cols)
                dgv.Columns.Add(col.Replace(" ", "_"), col);
            dgv.Columns[dgv.Columns.Count - 1].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            return dgv;
        }

        private int Seccion(Panel p, string texto, int x, int y, int w)
        {
            var lbl = new Label
            {
                Text = texto, Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = CAzul, Location = new Point(x, y),
                AutoSize = false, Size = new Size(w, 22)
            };
            p.Controls.Add(lbl);
            var line = new Panel
            {
                Location = new Point(x, y + 22), Size = new Size(w, 1),
                BackColor = Color.FromArgb(215, 222, 230)
            };
            p.Controls.Add(line);
            return y + 30;
        }

        private int Fila(Panel p, string etiqueta, decimal valor, string unidad,
            int x, int y, bool negativo = false)
        {
            p.Controls.Add(new Label
            {
                Text = etiqueta, Font = new Font("Segoe UI", 8.5f),
                ForeColor = CText, Location = new Point(x + 10, y),
                AutoSize = false, Size = new Size(380, 20)
            });
            string txt = (negativo && valor > 0 ? "- " : "") + valor.ToString("N2") + " " + unidad;
            p.Controls.Add(new Label
            {
                Text = txt, Font = new Font("Segoe UI", 8.5f),
                ForeColor = negativo ? CRojo : CText,
                Location = new Point(x + 400, y),
                AutoSize = false, Size = new Size(200, 20),
                TextAlign = ContentAlignment.MiddleRight
            });
            return y + 24;
        }

        private int FilaTotal(Panel p, string etiqueta, decimal valor, string unidad,
            int x, int y, bool positivo)
        {
            var bg = new Panel
            {
                Location = new Point(x, y), Size = new Size(620, 26),
                BackColor = Color.FromArgb(240, 244, 252)
            };
            p.Controls.Add(bg);
            bg.Controls.Add(new Label
            {
                Text = etiqueta, Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = CAzul, Location = new Point(10, 4),
                AutoSize = false, Size = new Size(380, 20)
            });
            bg.Controls.Add(new Label
            {
                Text = valor.ToString("N2") + " " + unidad,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = positivo ? CVerde : CRojo,
                Location = new Point(400, 4),
                AutoSize = false, Size = new Size(200, 20),
                TextAlign = ContentAlignment.MiddleRight
            });
            return y + 32;
        }
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Dialogo: Nueva / Editar retencion
    // ════════════════════════════════════════════════════════════════════════
    public class DlgRetencionISLR : Form
    {
        public RetencionISLR Retencion { get; }

        private DateTimePicker dtp = null!;
        private TextBox txtCliente = null!, txtFactura = null!,
                        txtComprobante = null!, txtObs = null!;
        private NumericUpDown numBase = null!, numPct = null!, numRetenido = null!;

        public DlgRetencionISLR(RetencionISLR ret)
        {
            Retencion = ret;
            Text = ret.Id == 0 ? "Nueva Retención ISLR" : "Editar Retención ISLR";
            Size = new Size(480, 360);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition   = FormStartPosition.CenterParent;
            MaximizeBox = MinimizeBox = false;
            BackColor = Color.FromArgb(244, 246, 250);

            int y = 16;
            dtp         = (DateTimePicker)AddRow("Fecha:", new DateTimePicker { Format = DateTimePickerFormat.Short, Value = ret.Fecha }, ref y)!;
            txtCliente  = AddTxt("Cliente:",      ret.Cliente,    ref y);
            txtFactura  = AddTxt("N° Factura:",   ret.NumFactura, ref y);
            numBase     = AddNum("Monto Base (Bs):",   ret.MontoBase,    9999999, 2, ref y);
            numPct      = AddNum("% Retención:",       ret.PctRetencion, 100,     1, ref y);
            numRetenido = AddNum("Monto Retenido (Bs):", ret.MontoRetenido, 9999999, 2, ref y);
            txtComprobante = AddTxt("N° Comprobante:", ret.Comprobante, ref y);
            txtObs      = AddTxt("Observaciones:",ret.Observaciones, ref y);

            numBase.ValueChanged += RecalcularRetenido;
            numPct.ValueChanged  += RecalcularRetenido;

            var btnOk = new Button
            {
                Text = "Guardar", DialogResult = DialogResult.OK,
                BackColor = Color.FromArgb(26, 74, 138), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Location = new Point(260, y + 8), Size = new Size(90, 30)
            };
            btnOk.Click += (s, e) => {
                Retencion.Fecha        = dtp!.Value.Date;
                Retencion.Cliente      = txtCliente.Text.Trim();
                Retencion.NumFactura   = txtFactura.Text.Trim();
                Retencion.MontoBase    = numBase.Value;
                Retencion.PctRetencion = numPct.Value;
                Retencion.MontoRetenido= numRetenido.Value;
                Retencion.Comprobante  = txtComprobante.Text.Trim();
                Retencion.Observaciones= txtObs.Text.Trim();
            };
            Controls.Add(btnOk);
            Controls.Add(new Button { Text = "Cancelar", DialogResult = DialogResult.Cancel,
                Location = new Point(160, y + 8), Size = new Size(90, 30),
                FlatStyle = FlatStyle.Flat });
            AcceptButton = btnOk;
        }

        private void RecalcularRetenido(object? s, EventArgs e)
        {
            numRetenido.Value = Math.Round(numBase.Value * numPct.Value / 100m, 2);
        }

        private Control AddRow(string lbl, Control ctrl, ref int y)
        {
            Controls.Add(new Label { Text = lbl, Location = new Point(16, y + 3), AutoSize = true,
                Font = new Font("Segoe UI", 8.5f) });
            ctrl.Location = new Point(170, y); ctrl.Size = new Size(260, 24);
            Controls.Add(ctrl);
            y += 30; return ctrl;
        }
        private TextBox AddTxt(string lbl, string val, ref int y)
        {
            var tb = new TextBox { Text = val, Font = new Font("Segoe UI", 9f) };
            AddRow(lbl, tb, ref y); return tb;
        }
        private NumericUpDown AddNum(string lbl, decimal val, decimal max, int dec, ref int y)
        {
            var nud = new NumericUpDown { Minimum = 0, Maximum = max, Value = Math.Min(val, max),
                DecimalPlaces = dec, ThousandsSeparator = true, Font = new Font("Segoe UI", 9f) };
            AddRow(lbl, nud, ref y); return nud;
        }
    }
}
