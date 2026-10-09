// ════════════════════════════════════════════════════════════════════════
// ARCHIVO: DlgPrecioBcv.cs  →  raíz del proyecto
//
// Calculadora "precio a tasa paralela → precio a registrar a tasa BCV"
// para el diálogo de factura (DlgFactura en FrmFacturas.cs).
//
// La factura se guarda en USD y se imprime en Bs a la tasa BCV vigente
// (snapshot TasaBCVUsada). Para que el monto en Bs de la factura sea el
// mismo que costaría el artículo a tasa paralela:
//
//     Bs a cobrar        = precio USD × tasa paralela
//     Precio a registrar = Bs a cobrar ÷ tasa BCV
//
// Ejemplo: fuste a 25 USD, paralela 1.000 Bs/USD → 25.000,00 Bs.
//          Con BCV a 400 Bs/USD → precio a registrar 62,50 USD
//          (62,50 × 400 = 25.000,00 Bs en la factura).
//
// Resultado: PrecioBcv (para la línea seleccionada) y Factor
// (paralela ÷ BCV, para ajustar todas las líneas). AplicarATodas indica
// cuál de los dos botones se pulsó.
// ════════════════════════════════════════════════════════════════════════

using System;
using System.Drawing;
using System.Windows.Forms;

namespace SistemaGestion
{
    public class DlgPrecioBcv : FormBase
    {
        public decimal PrecioBcv     { get; private set; }
        public decimal Factor        { get; private set; }
        public bool    AplicarATodas { get; private set; }

        private readonly NumericUpDown numPrecio, numParalela, numBcv;
        private readonly Label lblBs, lblPrecioBcv, lblCheck;
        private readonly Button btnLinea, btnTodas;

        public DlgPrecioBcv(decimal precioInicial, DateTime fechaFactura,
                            string descripcionLinea, int lineasPendientes)
        {
            Text            = "Precio a tasa BCV";
            ClientSize      = new Size(500, 430);
            StartPosition   = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox     = false; MinimizeBox = false;
            BackColor       = UITheme.Fondo;
            Font            = UITheme.Body;

            var lblTitulo = new Label
            {
                Text = "Precio a tasa paralela → precio a registrar a tasa BCV",
                Font = UITheme.BodyBold, ForeColor = UITheme.Texto,
                Location = new Point(20, 16), AutoSize = true
            };
            Controls.Add(lblTitulo);

            var lblLinea = new Label
            {
                Text = string.IsNullOrWhiteSpace(descripcionLinea)
                    ? "Sin línea seleccionada"
                    : "Línea: " + descripcionLinea,
                ForeColor = UITheme.TextoTenue,
                Location = new Point(20, 40), Size = new Size(460, 20),
                AutoEllipsis = true
            };
            Controls.Add(lblLinea);

            int y = 72;
            numPrecio   = Campo("Precio del artículo (USD a tasa paralela):", ref y, 2);
            numParalela = Campo("Tasa paralela (Bs por USD):",              ref y, 2);
            numBcv      = Campo("Tasa BCV (Bs por USD):",                   ref y, 4);

            // Tasa vigente para la fecha de la factura (misma regla que el
            // snapshot al guardar: última tasa con Fecha <= fecha factura).
            string origenTasa = "Sin tasa registrada para la fecha: escribe las tasas a mano.";
            try
            {
                var t = DatabaseService.ObtenerTasaPorFecha(fechaFactura);
                if (t != null)
                {
                    if (t.TasaParalelo > 0) numParalela.Value = Acotar(numParalela, t.TasaParalelo);
                    if (t.TasaBCV      > 0) numBcv.Value      = Acotar(numBcv, t.TasaBCV);
                    origenTasa = "Tasas del " + t.Fecha.ToString("dd/MM/yyyy") +
                                 " (vigentes para la fecha de la factura).";
                }
            }
            catch { /* el usuario puede escribir las tasas a mano */ }
            if (precioInicial > 0) numPrecio.Value = Acotar(numPrecio, precioInicial);

            var lblOrigen = new Label
            {
                Text = origenTasa, ForeColor = UITheme.TextoTenue,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Italic),
                Location = new Point(20, y), Size = new Size(460, 18)
            };
            Controls.Add(lblOrigen);
            y += 28;

            lblBs        = Resultado("Monto en Bs (precio × tasa paralela):", ref y, false);
            lblPrecioBcv = Resultado("Precio a registrar en la factura (USD):", ref y, true);
            lblCheck     = Resultado("Comprobación (precio a registrar × BCV):", ref y, false);

            var lblNota = new Label
            {
                Text = "La factura se imprime en Bs a tasa BCV. Con este precio, el monto en Bs " +
                       "de la factura es el mismo que costaría el artículo a tasa paralela.",
                ForeColor = UITheme.TextoTenue, Font = new Font("Segoe UI", 8.5f),
                Location = new Point(20, y + 4), Size = new Size(460, 34)
            };
            Controls.Add(lblNota);

            // Botonera
            btnLinea = BotonAccion("Aplicar a la línea", new Point(20, 380), 150, UITheme.Azul);
            btnTodas = BotonAccion("Ajustar todas (" + lineasPendientes + ")", new Point(178, 380), 170, UITheme.Verde);
            btnLinea.Enabled = !string.IsNullOrWhiteSpace(descripcionLinea);
            btnTodas.Enabled = lineasPendientes > 0;
            var tip = new ToolTip();
            tip.SetToolTip(btnLinea, "Pone el precio calculado en la línea seleccionada");
            tip.SetToolTip(btnTodas, "Multiplica por (paralela ÷ BCV) el precio de todas las líneas que aún no fueron ajustadas");

            var btnCerrar = new Button
            {
                Text = "Cerrar", Size = new Size(120, 36), Location = new Point(360, 380),
                FlatStyle = FlatStyle.Flat, BackColor = UITheme.FondoCard,
                ForeColor = UITheme.TextoTenue, Font = UITheme.Boton,
                Cursor = Cursors.Hand, DialogResult = DialogResult.Cancel
            };
            btnCerrar.FlatAppearance.BorderSize  = 1;
            btnCerrar.FlatAppearance.BorderColor = UITheme.Borde;
            Controls.Add(btnCerrar);
            CancelButton = btnCerrar;

            numPrecio.ValueChanged   += (s, e) => Calcular();
            numParalela.ValueChanged += (s, e) => Calcular();
            numBcv.ValueChanged      += (s, e) => Calcular();

            btnLinea.Click += (s, e) => { if (Valido(false)) { AplicarATodas = false; Cerrar(); } };
            btnTodas.Click += (s, e) => { if (Valido(true))  { AplicarATodas = true;  Cerrar(); } };

            Calcular();
        }

        private bool Valido(bool todas)
        {
            if (numParalela.Value > 0 && numBcv.Value > 0 && (todas || numPrecio.Value > 0))
                return true;
            MessageBox.Show(this, todas
                    ? "Indica la tasa paralela y la tasa BCV."
                    : "Indica el precio, la tasa paralela y la tasa BCV.",
                "Datos incompletos", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return false;
        }

        private void Cerrar()
        {
            Calcular();
            DialogResult = DialogResult.OK;
            Close();
        }

        private void Calcular()
        {
            decimal precio = numPrecio.Value, par = numParalela.Value, bcv = numBcv.Value;
            if (par <= 0 || bcv <= 0)
            {
                lblBs.Text = lblPrecioBcv.Text = lblCheck.Text = "—";
                PrecioBcv = 0; Factor = 0;
                return;
            }
            Factor = par / bcv;
            decimal bs = precio * par;
            PrecioBcv  = Math.Round(bs / bcv, 2, MidpointRounding.AwayFromZero);
            lblBs.Text        = bs.ToString("N2") + " Bs";
            lblPrecioBcv.Text = PrecioBcv.ToString("N2") + " USD";
            lblCheck.Text     = (PrecioBcv * bcv).ToString("N2") + " Bs";
        }

        // ── Helpers de construcción ─────────────────────────────────────────
        private NumericUpDown Campo(string texto, ref int y, int decimales)
        {
            Controls.Add(new Label
            {
                Text = texto, Location = new Point(20, y + 4),
                Size = new Size(290, 22), ForeColor = UITheme.Texto
            });
            var n = new NumericUpDown
            {
                Location = new Point(320, y), Size = new Size(160, 26),
                DecimalPlaces = decimales, Minimum = 0, Maximum = 1000000000m,
                Increment = 1, ThousandsSeparator = true,
                TextAlign = HorizontalAlignment.Right
            };
            Controls.Add(n);
            y += 34;
            return n;
        }

        private Label Resultado(string texto, ref int y, bool destacado)
        {
            Controls.Add(new Label
            {
                Text = texto, Location = new Point(20, y + 2),
                Size = new Size(290, 22),
                Font = destacado ? UITheme.BodyBold : UITheme.Body,
                ForeColor = UITheme.Texto
            });
            var l = new Label
            {
                Text = "—", Location = new Point(320, y), Size = new Size(160, 24),
                TextAlign = ContentAlignment.MiddleRight,
                Font = destacado ? new Font("Segoe UI", 11f, FontStyle.Bold) : UITheme.Body,
                ForeColor = destacado ? UITheme.Azul : UITheme.Texto
            };
            Controls.Add(l);
            y += destacado ? 32 : 28;
            return l;
        }

        private Button BotonAccion(string texto, Point p, int ancho, Color color)
        {
            var b = new Button
            {
                Text = texto, Size = new Size(ancho, 36), Location = p,
                FlatStyle = FlatStyle.Flat, BackColor = color, ForeColor = Color.White,
                Font = UITheme.Boton, Cursor = Cursors.Hand
            };
            b.FlatAppearance.BorderSize = 0;
            Controls.Add(b);
            return b;
        }

        private static decimal Acotar(NumericUpDown n, decimal v) =>
            Math.Max(n.Minimum, Math.Min(n.Maximum, Math.Round(v, n.DecimalPlaces)));
    }
}
