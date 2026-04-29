using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace SistemaGestion
{
    // ════════════════════════════════════════════════════════════════════════
    //  FrmTasaCambio — Registro diario de tasa BCV y Paralelo
    //  Solo Admin. Se abre desde el botón "Tasa $" del menú principal.
    // ════════════════════════════════════════════════════════════════════════
    public class FrmTasaCambio : FormBase
    {
        private DataGridView dgv;
        private Label        lblTasaActual;

        private static readonly Color Verde = Color.FromArgb(20, 90, 50);
        private static readonly Color Gris  = Color.FromArgb(100, 100, 120);

        public FrmTasaCambio()
        {
            Text          = "Tasa de Cambio";
            Size          = new Size(820, 520);
            StartPosition = FormStartPosition.CenterParent;
            BackColor     = Color.FromArgb(245, 250, 245);

            // ── Header ───────────────────────────────────────────────────
            var hdr = new Panel { Dock = DockStyle.Top, Height = 48, BackColor = Verde };
            hdr.Controls.Add(new Label {
                Text = "Tasa de Cambio — BCV y Paralelo",
                ForeColor = Color.White, AutoSize = true,
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                Location = new Point(12, 13) });

            // ── Panel de entrada rápida ──────────────────────────────────
            var pnlEntrada = new Panel {
                Dock = DockStyle.Top, Height = 68,
                BackColor = Color.FromArgb(228, 248, 235) };

            pnlEntrada.Controls.Add(Lbl("Fecha:", 10, 22));
            var dtp = new DateTimePicker {
                Location = new Point(65, 18), Size = new Size(120, 26),
                Format = DateTimePickerFormat.Short, Value = DateTime.Today };
            pnlEntrada.Controls.Add(dtp);

            pnlEntrada.Controls.Add(Lbl("Tasa BCV:", 200, 22));
            var numBCV = new NumericUpDown {
                Location = new Point(275, 18), Size = new Size(110, 26),
                Minimum = 0, Maximum = 9999999, DecimalPlaces = 2, Value = 0 };
            numBCV.Enter += (s, e) => numBCV.Select(0, numBCV.Text.Length);
            pnlEntrada.Controls.Add(numBCV);

            pnlEntrada.Controls.Add(Lbl("Tasa Paralelo:", 400, 22));
            var numPar = new NumericUpDown {
                Location = new Point(495, 18), Size = new Size(110, 26),
                Minimum = 0, Maximum = 9999999, DecimalPlaces = 2, Value = 0 };
            numPar.Enter += (s, e) => numPar.Select(0, numPar.Text.Length);
            pnlEntrada.Controls.Add(numPar);

            var btnGuardar = new Button {
                Text = "Guardar tasa", Location = new Point(620, 16),
                Size = new Size(130, 34), BackColor = Verde,
                ForeColor = Color.White, FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            btnGuardar.FlatAppearance.BorderSize = 0;
            btnGuardar.Click += (s, e) => {
                if (numBCV.Value <= 0)
                { MessageBox.Show("La tasa BCV debe ser mayor que cero.", "Validación",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                try
                {
                    DatabaseService.GuardarTasa(new TasaCambio {
                        Fecha        = dtp.Value.Date,
                        TasaBCV      = numBCV.Value,
                        TasaParalelo = numPar.Value,
                        Usuario      = Sesion.NombreUsuario
                    });
                    Cargar();
                }
                catch (Exception ex)
                { MessageBox.Show(ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error); }
            };
            pnlEntrada.Controls.Add(btnGuardar);

            // ── Tasa vigente ─────────────────────────────────────────────
            var pnlTasa = new Panel {
                Dock = DockStyle.Top, Height = 32,
                BackColor = Color.FromArgb(240, 255, 240) };
            lblTasaActual = new Label {
                AutoSize = true, Location = new Point(12, 8),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Verde };
            pnlTasa.Controls.Add(lblTasaActual);

            // ── Grid historial ───────────────────────────────────────────
            dgv = new DataGridView {
                Name = "dgv",
                Dock = DockStyle.Fill, ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false, AllowUserToAddRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
                BackgroundColor = Color.White, BorderStyle = BorderStyle.None,
                RowHeadersVisible = false, Font = new Font("Segoe UI", 9.5f),
                RowTemplate = { Height = 26 } };
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Verde;
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(235, 252, 240);
            dgv.EnableHeadersVisualStyles = false;

            // Doble clic → precargar en el formulario para editar
            dgv.DoubleClick += (s, e) => {
                if (dgv.CurrentRow == null) return;
                if (DateTime.TryParse(dgv.CurrentRow.Cells["Fecha"].Value?.ToString(), out var f))
                    dtp.Value = f;
                if (dgv.CurrentRow.Cells["TasaBCV"].Value is decimal bcv)
                    numBCV.Value = bcv;
                if (dgv.CurrentRow.Cells["TasaParalelo"].Value is decimal par)
                    numPar.Value = par;
            };

            // ── Footer ───────────────────────────────────────────────────
            var footer = new Panel { Dock = DockStyle.Bottom, Height = 38,
                BackColor = Color.FromArgb(210, 235, 218) };

            // Botón Trazabilidad (auditoría: qué facturas usaron qué tasa)
            var btnTraza = new Button {
                Text = "▤  Trazabilidad", Size = new Size(140, 26),
                Location = new Point(12, 6),
                BackColor = Color.FromArgb(80, 50, 150),
                ForeColor = Color.White, FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnTraza.FlatAppearance.BorderSize = 0;
            btnTraza.Click += (s, e) =>
            {
                try { using var f = new FrmTrazabilidadTasa(); f.ShowDialog(this); }
                catch (Exception ex)
                {
                    MessageBox.Show(this, ex.Message, "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            footer.Controls.Add(btnTraza);

            var btnCerrar = new Button {
                Text = "Cerrar", Size = new Size(90, 26),
                Location = new Point(710, 6), BackColor = Gris,
                ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnCerrar.FlatAppearance.BorderSize = 0;
            btnCerrar.Click += (s, e) => Close();
            footer.Controls.Add(btnCerrar);

            Controls.AddRange(new Control[] { dgv, footer, pnlTasa, pnlEntrada, hdr });

            VentanaPrefs.Restaurar(this);
            Cargar();

            // Precargar tasa del día si ya existe
            var hoy = DatabaseService.ObtenerTasaHoy();
            if (hoy != null && hoy.Fecha.Date == DateTime.Today)
            {
                numBCV.Value = hoy.TasaBCV;
                numPar.Value = hoy.TasaParalelo;
            }
        }

        private void Cargar()
        {
            var lista = DatabaseService.ObtenerHistorialTasas(60);
            dgv.DataSource = null;
            dgv.DataSource = lista.Select(t => new {
                Fecha        = t.Fecha,
                TasaBCV      = t.TasaBCV,
                TasaParalelo = t.TasaParalelo,
                t.Usuario
            }).ToList();

            if (dgv.Columns.Count > 0)
            {
                if (dgv.Columns.Contains("Fecha"))
                {
                    dgv.Columns["Fecha"].Width = 120;
                    dgv.Columns["Fecha"].DefaultCellStyle.Format = "dd/MM/yyyy";
                }
                if (dgv.Columns.Contains("TasaBCV"))
                {
                    dgv.Columns["TasaBCV"].Width = 140;
                    dgv.Columns["TasaBCV"].DefaultCellStyle.Format = "N2";
                }
                if (dgv.Columns.Contains("TasaParalelo"))
                {
                    dgv.Columns["TasaParalelo"].Width = 160;
                    dgv.Columns["TasaParalelo"].DefaultCellStyle.Format = "N2";
                }
                if (dgv.Columns.Contains("Usuario"))
                    dgv.Columns["Usuario"].Width = 120;
            }
            VentanaPrefs.AplicarAnchos(this, dgv);

            var vigente = lista.FirstOrDefault();
            lblTasaActual.Text = vigente != null
                ? $"  Tasa vigente ({vigente.Fecha:dd/MM/yyyy}):   " +
                  $"BCV = {vigente.TasaBCV:N2} Bs/$   |   " +
                  $"Paralelo = {vigente.TasaParalelo:N2} Bs/$"
                : "  No hay tasa registrada aún.";
        }

        private static Label Lbl(string t, int x, int y) =>
            new Label { Text = t, Location = new Point(x, y + 3),
                AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
    }
}
