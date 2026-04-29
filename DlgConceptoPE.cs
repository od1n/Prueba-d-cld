using System;
using System.Drawing;
using System.Windows.Forms;

namespace SistemaGestion
{
    public class DlgConceptoPE : FormBase
    {
        public DatoPE Resultado { get; private set; }

        private TextBox txtConcepto;
        private ComboBox cmbTipo;
        private NumericUpDown numMonto;
        private Button btnGuardar, btnCancelar;
        private readonly DatoPE _original;

        public DlgConceptoPE(DatoPE original)
        {
            _original = original;
            InitializeComponent();
            if (_original != null) CargarDatos();
        }

        private void InitializeComponent()
        {
            this.Text = _original == null ? "Agregar Concepto" : "Editar Concepto";
            this.Size = new Size(380, 240);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(245, 247, 250);
            this.Font = new Font("Segoe UI", 9.5f);

            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 44,
                BackColor = Color.FromArgb(30, 58, 95)
            };
            var lblHeader = new Label
            {
                Text = this.Text,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };
            pnlHeader.Controls.Add(lblHeader);

            var pnlForm = new Panel
            {
                Location = new Point(20, 58),
                Size = new Size(330, 130),
                BackColor = Color.Transparent
            };

            // Concepto
            pnlForm.Controls.Add(new Label { Text = "Concepto:", Location = new Point(0, 2), AutoSize = true, ForeColor = Color.FromArgb(44,62,80) });
            txtConcepto = new TextBox
            {
                Location = new Point(110, 0),
                Size = new Size(220, 24),
                MaxLength = 100
            };
            pnlForm.Controls.Add(txtConcepto);

            // Tipo
            pnlForm.Controls.Add(new Label { Text = "Tipo:", Location = new Point(0, 38), AutoSize = true, ForeColor = Color.FromArgb(44,62,80) });
            cmbTipo = new ComboBox
            {
                Location = new Point(110, 36),
                Size = new Size(220, 24),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbTipo.Items.AddRange(new object[] { "Fijo", "Variable" });
            cmbTipo.SelectedIndex = 0;
            pnlForm.Controls.Add(cmbTipo);

            // Monto
            pnlForm.Controls.Add(new Label { Text = "Monto/Mes (USD):", Location = new Point(0, 74), AutoSize = true, ForeColor = Color.FromArgb(44,62,80) });
            numMonto = new NumericUpDown
            {
                Location = new Point(110, 72),
                Size = new Size(220, 24),
                Minimum = 0,
                Maximum = 9999999,
                DecimalPlaces = 2,
                ThousandsSeparator = true
            };
            pnlForm.Controls.Add(numMonto);

            // Botones
            btnCancelar = new Button
            {
                Text = "Cancelar",
                Size = new Size(95, 32),
                Location = new Point(160, 175),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(149, 165, 166),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                FlatAppearance = { BorderSize = 0 },
                DialogResult = DialogResult.Cancel
            };

            btnGuardar = new Button
            {
                Text = "Guardar",
                Size = new Size(95, 32),
                Location = new Point(265, 175),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(39, 174, 96),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                FlatAppearance = { BorderSize = 0 }
            };
            btnGuardar.Click += BtnGuardar_Click;

            this.Controls.AddRange(new Control[] { pnlHeader, pnlForm, btnCancelar, btnGuardar });
            this.AcceptButton = btnGuardar;
            this.CancelButton = btnCancelar;
        }

        private void CargarDatos()
        {
            txtConcepto.Text = _original.Concepto;
            cmbTipo.SelectedItem = _original.Tipo;
            numMonto.Value = _original.MontoMensual;
        }

        private void BtnGuardar_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtConcepto.Text))
            {
                MessageBox.Show("Ingrese un concepto.", "Validacion",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtConcepto.Focus();
                return;
            }

            Resultado = new DatoPE
            {
                Concepto = txtConcepto.Text.Trim(),
                Tipo = cmbTipo.SelectedItem?.ToString() ?? "Fijo",
                MontoMensual = numMonto.Value,
                Origen = "Manual"
            };

            this.DialogResult = DialogResult.OK;
        }
    }
}
