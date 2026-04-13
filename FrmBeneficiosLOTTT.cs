using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace SistemaGestion
{
    public class FrmBeneficiosLOTTT : Form
    {
        private DataGridView dgv;
        private Label        lblRes;
        private ComboBox     cmbAnio, cmbTipo;

        public FrmBeneficiosLOTTT()
        {
            Text = "Beneficios Anuales — LOTTT Venezuela";
            Size = new Size(1080, 580); StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.FromArgb(245, 245, 255);

            var hdr = new Panel { Dock = DockStyle.Top, Height = 50,
                BackColor = Color.FromArgb(100, 40, 150) };
            hdr.Controls.Add(new Label { Text = "Beneficios Anuales — LOTTT",
                ForeColor = Color.White, Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                AutoSize = true, Location = new Point(12, 13) });

            var btnCalc   = Btn("Calcular beneficios", Color.FromArgb(40, 160, 80),  new Point(560, 8), 150);
            var btnPagar  = Btn("Marcar pagado",        Color.FromArgb(60, 110, 190), new Point(720, 8), 120);
            var btnElim   = Btn("Eliminar",             Color.FromArgb(180, 50, 50),  new Point(850, 8));
            var btnPdf    = Btn("Reporte PDF",          Color.FromArgb(180, 60, 20),  new Point(948, 8), 100);
            hdr.Controls.AddRange(new Control[] { btnCalc, btnPagar, btnElim, btnPdf });

            // Filtros
            var pnlF = new Panel { Dock = DockStyle.Top, Height = 42,
                BackColor = Color.FromArgb(232, 228, 248) };
            pnlF.Controls.Add(new Label { Text = "Anio:", Location = new Point(10, 13),
                AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Bold) });
            cmbAnio = new ComboBox { Location = new Point(50, 9), Size = new Size(100, 26),
                DropDownStyle = ComboBoxStyle.DropDownList };
            for (int y = DateTime.Today.Year; y >= DateTime.Today.Year - 5; y--)
                cmbAnio.Items.Add(y);
            cmbAnio.SelectedIndex = 0;
            cmbAnio.SelectedIndexChanged += (s, e) => Cargar();

            pnlF.Controls.Add(new Label { Text = "Tipo:", Location = new Point(165, 13),
                AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Bold) });
            cmbTipo = new ComboBox { Location = new Point(205, 9), Size = new Size(160, 26),
                DropDownStyle = ComboBoxStyle.DropDownList };
            cmbTipo.Items.AddRange(new object[]
                { "Todos", "Utilidades", "Vacaciones", "Bono Vacacional", "Prestaciones Sociales" });
            cmbTipo.SelectedIndex = 0;
            cmbTipo.SelectedIndexChanged += (s, e) => Cargar();
            pnlF.Controls.AddRange(new Control[] { cmbAnio, cmbTipo });

            // Info LOTTT
            var pnlInfo = new Panel { Dock = DockStyle.Top, Height = 60,
                BackColor = Color.FromArgb(230, 225, 248), Padding = new Padding(10, 8, 10, 4) };
            pnlInfo.Controls.Add(new Label
            {
                Text = "LOTTT Art. 131 — Utilidades: min. 15 dias de salario normal (pago en diciembre)\n" +
                       "LOTTT Art. 190 — Vacaciones: 15 dias + 1 dia/anio adicional (en aniversario de ingreso)\n" +
                       "LOTTT Art. 192 — Bono vacacional: 7 dias + 1 dia/anio  |  Art. 142 — Prestaciones: 15 dias x trimestre",
                AutoSize = true, Location = new Point(10, 6),
                Font = new Font("Segoe UI", 8f), ForeColor = Color.FromArgb(60, 40, 120)
            });

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
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(100, 40, 150);
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgv.ColumnHeadersDefaultCellStyle.Font      = new Font("Segoe UI", 9f, FontStyle.Bold);
            dgv.EnableHeadersVisualStyles = false;
            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(244, 240, 255);
            dgv.DataBindingComplete += (s, e) => ColorearFilas();

            var footer = new Panel { Dock = DockStyle.Bottom, Height = 38,
                BackColor = Color.FromArgb(225, 218, 245) };
            lblRes = new Label { AutoSize = true, Location = new Point(12, 10),
                Font = new Font("Segoe UI", 9f), ForeColor = Color.FromArgb(60, 30, 110) };
            var btnCerrar = new Button { Text = "Cerrar", Size = new Size(90, 26),
                Location = new Point(970, 6), BackColor = Color.FromArgb(100, 100, 120),
                ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnCerrar.FlatAppearance.BorderSize = 0;
            btnCerrar.Click += (s, e) => Close();
            footer.Controls.AddRange(new Control[] { lblRes, btnCerrar });

            Controls.AddRange(new Control[] { dgv, pnlInfo, pnlF, hdr, footer });

            btnCalc.Click  += (s, e) => CalcularBeneficios();
            btnPagar.Click += (s, e) => MarcarPagado();
            btnElim.Click  += (s, e) => Eliminar();
            btnPdf.Click   += (s, e) => ExportarPdf();

            Cargar();
        }

        private void Cargar()
        {
            int anio   = (int)cmbAnio.SelectedItem;
            string tipo = cmbTipo.SelectedItem?.ToString();

            var lista = DatabaseService.ObtenerBeneficios(null, anio);
            if (tipo != "Todos")
                lista = lista.Where(b => b.TipoBeneficio == tipo).ToList();

            dgv.DataSource = null;
            dgv.DataSource = lista.Select(b => new
            {
                b.Id,
                Empleado   = b.NombreEmpleado,
                b.TipoBeneficio,
                Dias       = b.DiasCorresponden.ToString("N0"),
                SalDia     = b.SalarioDiarioUSD.ToString("C2"),
                MontoUSD   = b.MontoUSD.ToString("C2"),
                TasaBCV    = b.TasaBCV.ToString("N2"),
                MontoBs    = b.MontoBs.ToString("N2"),
                FechaPago  = b.FechaPago.ToShortDateString(),
                b.Estado,
                b.Observaciones
            }).ToList();

            decimal totalUSD = lista.Sum(b => b.MontoUSD);
            decimal pendUSD  = lista.Where(b => b.Estado == "Pendiente").Sum(b => b.MontoUSD);
            lblRes.Text =
                "  Registros: " + lista.Count +
                "   |   Total comprometido: $" + totalUSD.ToString("N2") +
                "   |   Pendiente de pago: $" + pendUSD.ToString("N2") +
                "   |   Pagados: " + lista.Count(b => b.Estado == "Pagado");
        }

        private void ColorearFilas()
        {
            foreach (DataGridViewRow row in dgv.Rows)
            {
                var tipo = row.Cells["TipoBeneficio"].Value?.ToString();
                var est  = row.Cells["Estado"].Value?.ToString();

                row.DefaultCellStyle.BackColor = tipo switch
                {
                    "Utilidades"           => Color.FromArgb(255, 252, 225),
                    "Vacaciones"           => Color.FromArgb(228, 248, 255),
                    "Bono Vacacional"      => Color.FromArgb(228, 255, 240),
                    "Prestaciones Sociales"=> Color.FromArgb(248, 235, 255),
                    _                      => Color.White
                };
                if (est == "Pagado")
                    row.DefaultCellStyle.ForeColor = Color.FromArgb(100, 100, 110);
            }
        }

        private void CalcularBeneficios()
        {
            var empleados = DatabaseService.ObtenerEmpleados(soloActivos: true);
            if (empleados.Count == 0)
            { MessageBox.Show("No hay empleados activos.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }

            int anio = (int)cmbAnio.SelectedItem;

            // Verificar si ya existen calculos para este anio
            var existentes = DatabaseService.ObtenerBeneficios(null, anio);
            if (existentes.Count > 0)
            {
                if (MessageBox.Show(
                    "Ya existen " + existentes.Count + " registros para " + anio + ".\n" +
                    "Deseas recalcular y agregar los faltantes?",
                    "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
                    return;
            }

            // Pedir tasa BCV
            decimal tasa = PedirTasaBCV();
            if (tasa <= 0) return;

            int nuevos = 0;
            foreach (var emp in empleados)
            {
                // Solo calcular si no existe ya para este empleado y anio
                bool yaExiste = existentes.Any(b => b.IdEmpleado == emp.Id);
                if (yaExiste) continue;

                var (util, vac, bono, prest) = DatabaseService.CalcularBeneficios(emp, anio, tasa);

                DatabaseService.InsertarBeneficio(util);
                DatabaseService.InsertarBeneficio(vac);
                DatabaseService.InsertarBeneficio(bono);
                DatabaseService.InsertarBeneficio(prest);
                nuevos++;
            }

            Cargar();
            MessageBox.Show(
                "Calculo completado.\n" +
                "Empleados procesados: " + nuevos + "\n" +
                "4 beneficios por empleado (Utilidades, Vacaciones, Bono, Prestaciones)",
                "Calculo LOTTT", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private decimal PedirTasaBCV()
        {
            using var dlg = new Form
            {
                Text = "Tasa BCV actual", Size = new Size(340, 170),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false
            };
            dlg.Controls.Add(new Label { Text = "Ingresa la tasa BCV vigente (Bs/USD):",
                Location = new Point(20, 20), AutoSize = true });
            var num = new NumericUpDown { Location = new Point(20, 50), Size = new Size(280, 28),
                Maximum = 99999999, DecimalPlaces = 2, Minimum = 1, Value = 1 };
            dlg.Controls.Add(num);
            var bOk = new Button { Text = "Aceptar", Location = new Point(60, 90), Size = new Size(100, 30),
                BackColor = Color.FromArgb(40, 80, 160), ForeColor = Color.White, FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.OK };
            bOk.FlatAppearance.BorderSize = 0;
            dlg.Controls.Add(bOk);
            dlg.AcceptButton = bOk;
            return dlg.ShowDialog() == DialogResult.OK ? num.Value : 0;
        }

        private void MarcarPagado()
        {
            if (dgv.CurrentRow == null) return;
            int id = (int)dgv.CurrentRow.Cells["Id"].Value;
            decimal tasa = PedirTasaBCV();
            if (tasa <= 0) return;
            DatabaseService.MarcarBeneficioPagado(id, tasa);
            Cargar();
        }

        private void Eliminar()
        {
            if (dgv.CurrentRow == null) return;
            int id = (int)dgv.CurrentRow.Cells["Id"].Value;
            if (MessageBox.Show("Eliminar este registro de beneficio?", "Confirmar",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                using var c = new Microsoft.Data.Sqlite.SqliteConnection(SecurityService.CadenaConexion);
                c.Open();
                var cmd = c.CreateCommand();
                cmd.CommandText = "DELETE FROM BeneficiosAnuales WHERE Id=$id";
                cmd.Parameters.AddWithValue("$id", id);
                cmd.ExecuteNonQuery();
                Cargar();
            }
        }

        private void ExportarPdf()
        {
            int anio = (int)cmbAnio.SelectedItem;
            var lista = DatabaseService.ObtenerBeneficios(null, anio);
            if (lista.Count == 0) return;
            try
            {
                var pdf = PdfService.GenerarReporteBeneficios(lista, anio);
                PdfService.GuardarYAbrir(pdf, "BeneficiosLOTTT_" + anio + ".pdf");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error PDF:\n" + ex.Message, "Error",
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
    }
}
