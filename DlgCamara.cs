// ════════════════════════════════════════════════════════════════════════
// ARCHIVO: DlgCamara.cs  →  raíz del proyecto
//
// Diálogo para crear o editar una cámara EZVIZ.
// La clave (código de verificación del sticker) se cifra con DPAPI antes
// de pasar al modelo (ClaveCifrada). Si se está editando y el campo de
// clave queda vacío → se conserva la clave anterior sin tocarla.
// ════════════════════════════════════════════════════════════════════════

using System;
using System.Drawing;
using System.Net;
using System.Windows.Forms;

namespace SistemaGestion
{
    public class DlgCamara : FormBase
    {
        public Camara Resultado { get; private set; }
        private readonly Camara _original;

        private TextBox       _txtNombre, _txtUbicacion, _txtIp, _txtUsuario,
                              _txtClave, _txtRuta, _txtModelo;
        private NumericUpDown _numPuerto, _numOrden;
        private CheckBox      _chkActiva, _chkMostrarClave;

        public DlgCamara(Camara existente = null)
        {
            _original = existente;

            Text            = existente == null ? "Nueva cámara" : "Editar cámara";
            Size            = new Size(520, 540);
            StartPosition   = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox     = false;
            MinimizeBox     = false;
            BackColor       = Color.FromArgb(245, 247, 250);
            Font            = new Font("Segoe UI", 9.5f);

            ConstruirUI();

            if (existente != null) Cargar(existente);
        }

        private void ConstruirUI()
        {
            int y = 18;
            const int xLabel = 20, xField = 175, wField = 310;

            void Lbl(string txt, int yy) =>
                Controls.Add(new Label
                {
                    Text = txt,
                    Location = new Point(xLabel, yy + 4),
                    AutoSize = true,
                    Font = new Font("Segoe UI", 9.5f)
                });

            Lbl("Nombre:", y);
            _txtNombre = new TextBox { Location = new Point(xField, y), Width = wField };
            Controls.Add(_txtNombre); y += 32;

            Lbl("Ubicación:", y);
            _txtUbicacion = new TextBox { Location = new Point(xField, y), Width = wField };
            Controls.Add(_txtUbicacion); y += 32;

            Lbl("Modelo (opcional):", y);
            _txtModelo = new TextBox
            {
                Location  = new Point(xField, y),
                Width     = wField,
                PlaceholderText = "CS-C6N, CS-H8…"
            };
            Controls.Add(_txtModelo); y += 32;

            Lbl("IP en LAN:", y);
            _txtIp = new TextBox
            {
                Location        = new Point(xField, y),
                Width           = 200,
                PlaceholderText = "192.168.1.50"
            };
            Controls.Add(_txtIp); y += 32;

            Lbl("Puerto RTSP:", y);
            _numPuerto = new NumericUpDown
            {
                Location = new Point(xField, y),
                Width    = 100,
                Minimum  = 1,
                Maximum  = 65535,
                Value    = 554
            };
            Controls.Add(_numPuerto); y += 32;

            Lbl("Usuario:", y);
            _txtUsuario = new TextBox
            {
                Location = new Point(xField, y),
                Width    = 200,
                Text     = "admin"
            };
            Controls.Add(_txtUsuario); y += 32;

            Lbl("Clave (cód. verificación):", y);
            _txtClave = new TextBox
            {
                Location     = new Point(xField, y),
                Width        = 200,
                UseSystemPasswordChar = true
            };
            Controls.Add(_txtClave);
            _chkMostrarClave = new CheckBox
            {
                Text     = "Mostrar",
                Location = new Point(xField + 210, y + 2),
                AutoSize = true
            };
            _chkMostrarClave.CheckedChanged += (s, e) =>
                _txtClave.UseSystemPasswordChar = !_chkMostrarClave.Checked;
            Controls.Add(_chkMostrarClave); y += 32;

            Lbl("Ruta stream:", y);
            _txtRuta = new TextBox
            {
                Location        = new Point(xField, y),
                Width           = wField,
                Text            = "/Streaming/Channels/101",
                PlaceholderText = "/Streaming/Channels/101 ó /H.264"
            };
            Controls.Add(_txtRuta); y += 32;

            // Hint sobre la ruta
            Controls.Add(new Label
            {
                Text      = "Modelos antiguos (C6N firmware viejo) usan /H.264.\n" +
                            "Modelos nuevos (H8) usan /Streaming/Channels/101.",
                Location  = new Point(xField, y),
                Width     = wField,
                Height    = 30,
                ForeColor = Color.FromArgb(110, 110, 110),
                Font      = new Font("Segoe UI", 8.5f, FontStyle.Italic)
            }); y += 36;

            Lbl("Orden:", y);
            _numOrden = new NumericUpDown
            {
                Location = new Point(xField, y),
                Width    = 80,
                Minimum  = 0,
                Maximum  = 9999,
                Value    = 0
            };
            Controls.Add(_numOrden);

            _chkActiva = new CheckBox
            {
                Text     = "Activa",
                Location = new Point(xField + 100, y + 2),
                AutoSize = true,
                Checked  = true
            };
            Controls.Add(_chkActiva); y += 38;

            // Botones
            var btnGuardar = new Button
            {
                Text      = "Guardar",
                Location  = new Point(xField + wField - 220, y + 4),
                Size      = new Size(100, 34),
                BackColor = Color.FromArgb(60, 130, 200),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };
            btnGuardar.FlatAppearance.BorderSize = 0;
            btnGuardar.Click += BtnGuardar_Click;

            var btnCancelar = new Button
            {
                Text      = "Cancelar",
                Location  = new Point(xField + wField - 110, y + 4),
                Size      = new Size(100, 34),
                BackColor = Color.FromArgb(180, 180, 185),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                DialogResult = DialogResult.Cancel
            };
            btnCancelar.FlatAppearance.BorderSize = 0;

            Controls.Add(btnGuardar);
            Controls.Add(btnCancelar);
            CancelButton = btnCancelar;
        }

        private void Cargar(Camara c)
        {
            _txtNombre.Text     = c.Nombre;
            _txtUbicacion.Text  = c.Ubicacion;
            _txtModelo.Text     = c.Modelo;
            _txtIp.Text         = c.IpLocal;
            _numPuerto.Value    = c.PuertoRtsp;
            _txtUsuario.Text    = c.UsuarioCamara;
            _txtClave.Text      = "";   // no exponemos la clave existente al editar
            _txtClave.PlaceholderText = "(dejar vacío para conservar la actual)";
            _txtRuta.Text       = c.RutaStream;
            _numOrden.Value     = c.Orden;
            _chkActiva.Checked  = c.Activa;
        }

        private void BtnGuardar_Click(object sender, EventArgs e)
        {
            // Validaciones
            string nombre = _txtNombre.Text?.Trim() ?? "";
            string ip     = _txtIp.Text?.Trim() ?? "";

            if (string.IsNullOrEmpty(nombre))
            {
                MessageBox.Show("El nombre es obligatorio.", "Validación",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _txtNombre.Focus();
                return;
            }
            if (!IPAddress.TryParse(ip, out _))
            {
                MessageBox.Show("La IP no tiene un formato válido.\n" +
                    "Ejemplo: 192.168.1.50", "Validación",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _txtIp.Focus();
                return;
            }

            string clave = _txtClave.Text ?? "";
            string claveCifrada;

            if (_original == null)
            {
                // Nueva → la clave es obligatoria
                if (string.IsNullOrEmpty(clave))
                {
                    MessageBox.Show("La clave (código de verificación) es obligatoria.",
                        "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    _txtClave.Focus();
                    return;
                }
                claveCifrada = SecurityService.CamCifrar(clave);
            }
            else
            {
                // Edit → si dejaron vacío, mantener la cifrada original
                claveCifrada = string.IsNullOrEmpty(clave)
                    ? _original.ClaveCifrada
                    : SecurityService.CamCifrar(clave);
            }

            Resultado = _original ?? new Camara();
            Resultado.Nombre        = nombre;
            Resultado.Ubicacion     = _txtUbicacion.Text?.Trim() ?? "";
            Resultado.Modelo        = _txtModelo.Text?.Trim()    ?? "";
            Resultado.IpLocal       = ip;
            Resultado.PuertoRtsp    = (int)_numPuerto.Value;
            Resultado.UsuarioCamara = string.IsNullOrWhiteSpace(_txtUsuario.Text)
                                        ? "admin"
                                        : _txtUsuario.Text.Trim();
            Resultado.ClaveCifrada  = claveCifrada;
            Resultado.RutaStream    = string.IsNullOrWhiteSpace(_txtRuta.Text)
                                        ? "/Streaming/Channels/101"
                                        : _txtRuta.Text.Trim();
            Resultado.Activa        = _chkActiva.Checked;
            Resultado.Orden         = (int)_numOrden.Value;

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
