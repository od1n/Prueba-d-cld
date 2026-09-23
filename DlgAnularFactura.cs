// ════════════════════════════════════════════════════════════════════════
// ARCHIVO: DlgAnularFactura.cs  →  raíz del proyecto
//
// Diálogos de soporte para FrmFacturas:
//
//   • DlgAnularFactura  — pide motivo de anulación (mín. 5 chars).
//                         Botón Confirmar deshabilitado hasta que el
//                         motivo cumpla el mínimo. Confirma con doble
//                         check para evitar anulaciones accidentales.
//
//   • DlgCorregirFecha  — pide nueva fecha para una factura cuya
//                         fecha original era inválida. Muestra el
//                         motivo de anulación original como contexto.
//                         Validación: año entre 2000 y año_actual+1.
// ════════════════════════════════════════════════════════════════════════

using System;
using System.Drawing;
using System.Windows.Forms;

namespace SistemaGestion
{
    public class DlgAnularFactura : FormBase
    {
        public string Motivo { get; private set; } = "";

        public DlgAnularFactura(string numeroFactura, decimal totalFactura)
        {
            Text            = "Anular factura " + numeroFactura;
            ClientSize      = new Size(520, 320);
            StartPosition   = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox     = false; MinimizeBox = false;
            BackColor       = UITheme.Fondo;
            Font            = UITheme.Body;

            // Banner rojo de advertencia
            var banner = new Panel { Dock = DockStyle.Top, Height = 64,
                BackColor = UITheme.Rojo };
            banner.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using var fIco = new Font("Segoe UI", 22f, FontStyle.Bold);
                e.Graphics.DrawString("⚠", fIco, Brushes.White, 16, 16);
                using var fTit = new Font("Segoe UI", 13f, FontStyle.Bold);
                e.Graphics.DrawString("Anular factura " + numeroFactura,
                    fTit, Brushes.White, 56, 14);
                using var fSub = new Font("Segoe UI", 9.5f);
                using var brSub = new SolidBrush(Color.FromArgb(255, 220, 220));
                e.Graphics.DrawString("Total: " + totalFactura.ToString("C2") +
                    " — esta operación queda registrada en auditoría.",
                    fSub, brSub, 58, 38);
            };
            Controls.Add(banner);

            // Texto explicativo
            var lblInfo = new Label
            {
                Text = "La factura quedará en la base de datos para auditoría " +
                       "fiscal (no se eliminará), pero será excluida de reportes, " +
                       "totales y cuentas por cobrar. El correlativo SENIAT no se " +
                       "reutiliza.",
                Location = new Point(20, 80), Size = new Size(480, 50),
                ForeColor = UITheme.Texto, AutoEllipsis = false
            };
            Controls.Add(lblInfo);

            // Campo motivo
            var lblMotivo = new Label
            {
                Text = "Motivo de anulación:",
                Font = UITheme.BodyBold,
                Location = new Point(20, 140), AutoSize = true
            };
            Controls.Add(lblMotivo);

            var txtMotivo = new TextBox
            {
                Location = new Point(20, 162), Size = new Size(480, 60),
                Multiline = true, BorderStyle = BorderStyle.FixedSingle,
                Font = UITheme.Body
            };
            Controls.Add(txtMotivo);

            var lblHint = new Label
            {
                Text = "Mínimo 5 caracteres. Sé específico (ej: \"Cliente devolvió, cambio de pedido\").",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Italic),
                ForeColor = UITheme.TextoTenue,
                Location = new Point(20, 226), AutoSize = true
            };
            Controls.Add(lblHint);

            // Botonera
            var btnCancelar = new Button
            {
                Text = "Cancelar", Width = 120, Height = 36,
                Location = new Point(260, 268),
                FlatStyle = FlatStyle.Flat,
                BackColor = UITheme.FondoCard, ForeColor = UITheme.TextoTenue,
                Font = UITheme.Boton, Cursor = Cursors.Hand,
                DialogResult = DialogResult.Cancel
            };
            btnCancelar.FlatAppearance.BorderSize = 1;
            btnCancelar.FlatAppearance.BorderColor = UITheme.Borde;
            Controls.Add(btnCancelar);

            var btnConfirmar = new Button
            {
                Text = "Anular factura", Width = 140, Height = 36,
                Location = new Point(388, 268),
                FlatStyle = FlatStyle.Flat,
                BackColor = UITheme.Rojo, ForeColor = Color.White,
                Font = UITheme.Boton, Cursor = Cursors.Hand,
                Enabled = false
            };
            btnConfirmar.FlatAppearance.BorderSize = 0;
            btnConfirmar.FlatAppearance.MouseOverBackColor = Color.FromArgb(150, 30, 30);
            Controls.Add(btnConfirmar);

            // Habilitar Confirmar solo cuando el motivo tenga >= 5 chars
            txtMotivo.TextChanged += (s, e) =>
            {
                btnConfirmar.Enabled = txtMotivo.Text.Trim().Length >= 5;
            };

            btnConfirmar.Click += (s, e) =>
            {
                Motivo = txtMotivo.Text.Trim();
                // Confirmación doble — anular es irreversible para usuarios no-Admin
                var resp = MessageBox.Show(this,
                    "¿Confirmas la anulación de la factura " + numeroFactura + "?\n\n" +
                    "Esta acción queda registrada en la auditoría con tu usuario.",
                    "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (resp == DialogResult.Yes)
                {
                    DialogResult = DialogResult.OK;
                    Close();
                }
            };

            CancelButton = btnCancelar;
            Shown += (s, e) => txtMotivo.Focus();
        }
    }

    // ── Diálogo de corrección de fecha ─────────────────────────────────────
    public class DlgCorregirFecha : FormBase
    {
        public DateTime NuevaFecha { get; private set; }

        public DlgCorregirFecha(string numeroFactura, string motivoOriginal)
        {
            Text            = "Corregir fecha — Factura " + numeroFactura;
            ClientSize      = new Size(520, 290);
            StartPosition   = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox     = false; MinimizeBox = false;
            BackColor       = UITheme.Fondo;
            Font            = UITheme.Body;

            // Banner ámbar
            var banner = new Panel { Dock = DockStyle.Top, Height = 64,
                BackColor = UITheme.Naranja };
            banner.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using var fIco = new Font("Segoe UI", 22f, FontStyle.Bold);
                e.Graphics.DrawString("📅", fIco, Brushes.White, 16, 14);
                using var fTit = new Font("Segoe UI", 13f, FontStyle.Bold);
                e.Graphics.DrawString("Corregir fecha — Factura " + numeroFactura,
                    fTit, Brushes.White, 56, 14);
                using var fSub = new Font("Segoe UI", 9.5f);
                using var brSub = new SolidBrush(Color.FromArgb(255, 240, 220));
                e.Graphics.DrawString("Indica la fecha correcta de emisión.",
                    fSub, brSub, 58, 38);
            };
            Controls.Add(banner);

            // Contexto: motivo original
            var lblCtx = new Label
            {
                Text = "Estado actual: " + motivoOriginal,
                Location = new Point(20, 80), Size = new Size(480, 50),
                ForeColor = UITheme.TextoTenue,
                Font = new Font("Segoe UI", 9f, FontStyle.Italic)
            };
            Controls.Add(lblCtx);

            // Selector de fecha
            var lblF = new Label
            {
                Text = "Nueva fecha:",
                Font = UITheme.BodyBold,
                Location = new Point(20, 145), AutoSize = true
            };
            Controls.Add(lblF);

            int anioMax = DateTime.Today.Year + 1;
            var dtp = new DateTimePicker
            {
                Location = new Point(140, 142),
                Width    = 200, Height = 28,
                Format   = DateTimePickerFormat.Short,
                MinDate  = new DateTime(2000, 1, 1),
                MaxDate  = new DateTime(anioMax, 12, 31),
                Value    = DateTime.Today,
                Font     = UITheme.Body
            };
            Controls.Add(dtp);

            var lblHint = new Label
            {
                Text = $"Rango permitido: 2000 — {anioMax}",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Italic),
                ForeColor = UITheme.TextoTenue,
                Location = new Point(140, 175), AutoSize = true
            };
            Controls.Add(lblHint);

            // Botonera
            var btnCancelar = new Button
            {
                Text = "Cancelar", Width = 120, Height = 36,
                Location = new Point(260, 230),
                FlatStyle = FlatStyle.Flat,
                BackColor = UITheme.FondoCard, ForeColor = UITheme.TextoTenue,
                Font = UITheme.Boton, Cursor = Cursors.Hand,
                DialogResult = DialogResult.Cancel
            };
            btnCancelar.FlatAppearance.BorderSize = 1;
            btnCancelar.FlatAppearance.BorderColor = UITheme.Borde;
            Controls.Add(btnCancelar);

            var btnGuardar = new Button
            {
                Text = "Guardar fecha", Width = 140, Height = 36,
                Location = new Point(388, 230),
                FlatStyle = FlatStyle.Flat,
                BackColor = UITheme.Azul, ForeColor = Color.White,
                Font = UITheme.Boton, Cursor = Cursors.Hand
            };
            btnGuardar.FlatAppearance.BorderSize = 0;
            btnGuardar.FlatAppearance.MouseOverBackColor = UITheme.AzulHover;
            Controls.Add(btnGuardar);

            btnGuardar.Click += (s, e) =>
            {
                NuevaFecha = dtp.Value.Date;
                DialogResult = DialogResult.OK;
                Close();
            };

            AcceptButton = btnGuardar;
            CancelButton = btnCancelar;
        }
    }
}
