// ════════════════════════════════════════════════════════════════════════
// ARCHIVO: FrmCamaras.cs  →  raíz del proyecto
//
// Visor en vivo de cámaras EZVIZ por RTSP en LAN.
//   - Una sola cámara visible a la vez (single-stream con switcher).
//   - Lista lateral con cámaras visibles para el usuario actual.
//   - Reconexión automática cada 10s cuando el stream cae.
//   - Tras 3 fallos consecutivos en una cámara, registra ERROR_CAMARA en
//     el log de auditoría (una vez por cámara y sesión).
//
// Tecnología:
//   - LibVLCSharp.WinForms (VideoView) sobre libvlc nativa.
//   - Core.Initialize() debe estar llamado antes (Program.cs lo hace).
//   - Opciones RTSP: --rtsp-tcp + network-caching=300 → menor latencia y
//     más estabilidad en LAN con NAT/firewall que UDP.
// ════════════════════════════════════════════════════════════════════════

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using LibVLCSharp.Shared;
using LibVLCSharp.WinForms;

namespace SistemaGestion
{
    public class FrmCamaras : FormBase
    {
        // ── VLC ──────────────────────────────────────────────────────────────
        private LibVLC                _libVlc;
        private MediaPlayer           _player;
        private VideoView             _videoView;

        // ── UI ───────────────────────────────────────────────────────────────
        private ListView              _lvCamaras;
        private Label                 _lblOverlay;       // mensaje sobre el video
        private Panel                 _panelOverlay;
        private Label                 _lblBarraInf;
        private Button                _btnFullscreen;
        private Button                _btnMute;
        private Button                _btnReconectar;
        private FormBorderStyle       _bordeOriginal;
        private FormWindowState       _estadoOriginal;
        private bool                  _enFullscreen;

        // ── Estado ───────────────────────────────────────────────────────────
        private List<Camara>          _camaras;
        private Camara                _camaraActual;
        private System.Windows.Forms.Timer _timerReintento;
        private int                   _fallosConsecutivos;
        private bool                  _alertaFallosRegistrada;

        public FrmCamaras()
        {
            Text            = "Cámaras de vigilancia";
            Size            = new Size(1280, 800);
            MinimumSize     = new Size(900, 600);
            StartPosition   = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.Sizable;
            KeyPreview      = true;

            ConstruirUI();
            CargarCamaras();
            InicializarVlc();
            ConfigurarTimerReintento();

            // Auto-conectar a la primera cámara si el usuario tiene alguna
            if (_camaras.Count > 0)
            {
                _lvCamaras.Items[0].Selected = true;
                _lvCamaras.Items[0].Focused  = true;
            }
            else
            {
                MostrarOverlay("No tenés cámaras asignadas.\n" +
                               "Pedí al administrador que te dé acceso.");
            }
        }

        // ── Construcción de la UI ────────────────────────────────────────────
        private void ConstruirUI()
        {
            // Panel lateral
            var panelLateral = new Panel
            {
                Dock      = DockStyle.Left,
                Width     = 260,
                BackColor = Color.FromArgb(245, 247, 250),
                Padding   = new Padding(8)
            };

            var lblTitulo = new Label
            {
                Text      = "Cámaras",
                Dock      = DockStyle.Top,
                Height    = 28,
                Font      = new Font("Segoe UI Semibold", 11f),
                ForeColor = Color.FromArgb(40, 40, 40)
            };

            _lvCamaras = new ListView
            {
                Dock          = DockStyle.Fill,
                View          = View.Details,
                FullRowSelect = true,
                MultiSelect   = false,
                HideSelection = false,
                GridLines     = false,
                BorderStyle   = BorderStyle.FixedSingle
            };
            _lvCamaras.Columns.Add("Nombre",    150);
            _lvCamaras.Columns.Add("Ubicación", 95);
            _lvCamaras.SelectedIndexChanged += (s, e) =>
            {
                if (_lvCamaras.SelectedItems.Count == 0) return;
                if (_lvCamaras.SelectedItems[0].Tag is Camara cam)
                    Conectar(cam);
            };

            panelLateral.Controls.Add(_lvCamaras);
            panelLateral.Controls.Add(lblTitulo);

            // Panel central: VideoView + overlay + barra inferior
            var panelCentral = new Panel { Dock = DockStyle.Fill, BackColor = Color.Black };

            _videoView = new VideoView
            {
                Dock      = DockStyle.Fill,
                BackColor = Color.Black
            };

            // Overlay encima del VideoView para el texto de estado
            _panelOverlay = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = Color.Transparent
            };
            _lblOverlay = new Label
            {
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.White,
                BackColor = Color.FromArgb(120, 0, 0, 0),
                Font      = new Font("Segoe UI", 14f, FontStyle.Bold),
                Visible   = true,
                Text      = "Selecciona una cámara"
            };
            _panelOverlay.Controls.Add(_lblOverlay);

            // Barra inferior con controles
            var barraInf = new Panel
            {
                Dock      = DockStyle.Bottom,
                Height    = 40,
                BackColor = Color.FromArgb(30, 30, 30),
                Padding   = new Padding(8, 4, 8, 4)
            };
            _btnFullscreen = CrearBoton("Pantalla completa", (s, e) => ToggleFullscreen());
            _btnMute       = CrearBoton("Silenciar audio",   (s, e) => ToggleMute());
            _btnReconectar = CrearBoton("Reconectar",        (s, e) =>
            {
                if (_camaraActual != null) Conectar(_camaraActual);
            });
            _lblBarraInf = new Label
            {
                Dock      = DockStyle.Right,
                AutoSize  = false,
                Width     = 320,
                TextAlign = ContentAlignment.MiddleRight,
                ForeColor = Color.LightGray,
                Font      = new Font("Segoe UI", 9f),
                Text      = ""
            };
            // Orden: agregamos derecha primero (Dock derecho se apila)
            barraInf.Controls.Add(_lblBarraInf);
            barraInf.Controls.Add(_btnReconectar);
            barraInf.Controls.Add(_btnMute);
            barraInf.Controls.Add(_btnFullscreen);

            // El orden de Add importa con Fill: el último agregado queda debajo
            panelCentral.Controls.Add(_videoView);
            panelCentral.Controls.Add(_panelOverlay);
            _panelOverlay.BringToFront();
            panelCentral.Controls.Add(barraInf);

            Controls.Add(panelCentral);
            Controls.Add(panelLateral);
        }

        private Button CrearBoton(string texto, EventHandler onClick)
        {
            var b = new Button
            {
                Text      = texto,
                Dock      = DockStyle.Left,
                Width     = 130,
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                BackColor = Color.FromArgb(60, 60, 60),
                Margin    = new Padding(0, 0, 6, 0),
                Font      = new Font("Segoe UI", 9f)
            };
            b.FlatAppearance.BorderColor = Color.FromArgb(80, 80, 80);
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(80, 80, 80);
            b.Click += onClick;
            return b;
        }

        // ── Datos ────────────────────────────────────────────────────────────
        private void CargarCamaras()
        {
            try
            {
                _camaras = DatabaseService.ObtenerCamarasVisibles(
                    Sesion.UsuarioId, Sesion.EsAdmin);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error cargando cámaras: " + ex.Message,
                    "Cámaras", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _camaras = new List<Camara>();
            }

            _lvCamaras.Items.Clear();
            foreach (var cam in _camaras)
            {
                var item = new ListViewItem(cam.Nombre);
                item.SubItems.Add(cam.Ubicacion ?? "");
                item.Tag = cam;
                _lvCamaras.Items.Add(item);
            }
        }

        // ── VLC ──────────────────────────────────────────────────────────────
        private void InicializarVlc()
        {
            // Opciones globales para libvlc (-vv = verbose útil para debug,
            // omitir en prod si molesta el ruido en stderr).
            _libVlc = new LibVLC(
                "--no-osd",                       // sin overlay de tiempo
                "--no-snapshot-preview",          // sin flash al snapshot
                "--no-stats");

            _player = new MediaPlayer(_libVlc);
            _videoView.MediaPlayer = _player;

            // Eventos llegan en hilo de VLC → marshalling al UI thread.
            _player.Playing          += (s, e) => InvokeIfRequired(() => OnConexionExitosa());
            _player.EncounteredError += (s, e) => InvokeIfRequired(() => OnFalloConexion("error"));
            _player.EndReached       += (s, e) => InvokeIfRequired(() => OnFalloConexion("stream cortado"));
        }

        private void ConfigurarTimerReintento()
        {
            _timerReintento = new System.Windows.Forms.Timer { Interval = 10_000 };
            _timerReintento.Tick += (s, e) =>
            {
                // Solo reintenta si hay una cámara seleccionada y el player no está reproduciendo
                if (_camaraActual == null) return;
                if (_player != null && _player.IsPlaying) return;
                Conectar(_camaraActual);
            };
        }

        private void Conectar(Camara cam)
        {
            if (cam == null) return;
            // Cambio de cámara → resetear contadores
            if (_camaraActual?.Id != cam.Id)
            {
                _fallosConsecutivos     = 0;
                _alertaFallosRegistrada = false;
            }
            _camaraActual = cam;

            MostrarOverlay($"Conectando a {cam.Nombre}...");
            _timerReintento.Stop();
            _lblBarraInf.Text = cam.Nombre + " — " + cam.IpLocal;

            try
            {
                if (_player.IsPlaying) _player.Stop();

                using var media = new Media(_libVlc, new Uri(cam.ConstruirUrlRtsp()));
                media.AddOption(":network-caching=300");
                media.AddOption(":rtsp-tcp");                  // TCP > UDP en LAN
                media.AddOption(":live-caching=300");

                _player.Play(media);
            }
            catch (Exception ex)
            {
                OnFalloConexion("excepción: " + ex.Message);
            }
        }

        private void OnConexionExitosa()
        {
            _fallosConsecutivos     = 0;
            _alertaFallosRegistrada = false;
            OcultarOverlay();
            _timerReintento.Stop();
        }

        private void OnFalloConexion(string motivo)
        {
            if (_camaraActual == null) return;

            _fallosConsecutivos++;
            MostrarOverlay($"{_camaraActual.Nombre}: cámara no responde\n" +
                           $"(reintentando en 10s — fallo #{_fallosConsecutivos})");

            // Después de 3 fallos consecutivos, registrar en auditoría una vez
            // por sesión y por cámara para no inundar el log.
            if (_fallosConsecutivos >= 3 && !_alertaFallosRegistrada)
            {
                try
                {
                    DatabaseService.RegistrarAuditoria("CAMARA_OFFLINE", "Camaras",
                        $"Cámara '{_camaraActual.Nombre}' (Id={_camaraActual.Id}, " +
                        $"IP={_camaraActual.IpLocal}) sin respuesta — {motivo}");
                }
                catch { /* la auditoría no debe romper el visor */ }
                _alertaFallosRegistrada = true;
            }

            _timerReintento.Stop();
            _timerReintento.Start();
        }

        // ── Helpers de UI ────────────────────────────────────────────────────
        private void MostrarOverlay(string texto)
        {
            InvokeIfRequired(() =>
            {
                _lblOverlay.Text   = texto;
                _panelOverlay.Visible = true;
                _lblOverlay.BringToFront();
            });
        }

        private void OcultarOverlay()
        {
            InvokeIfRequired(() => _panelOverlay.Visible = false);
        }

        private void InvokeIfRequired(Action a)
        {
            if (IsDisposed) return;
            if (InvokeRequired) BeginInvoke(a);
            else a();
        }

        private void ToggleMute()
        {
            if (_player == null) return;
            _player.Mute = !_player.Mute;
            _btnMute.Text = _player.Mute ? "Activar audio" : "Silenciar audio";
        }

        private void ToggleFullscreen()
        {
            if (!_enFullscreen)
            {
                _bordeOriginal  = FormBorderStyle;
                _estadoOriginal = WindowState;
                FormBorderStyle = FormBorderStyle.None;
                WindowState     = FormWindowState.Maximized;
                TopMost         = true;
                _enFullscreen   = true;
            }
            else
            {
                TopMost         = false;
                FormBorderStyle = _bordeOriginal;
                WindowState     = _estadoOriginal;
                _enFullscreen   = false;
            }
        }

        // Esc cierra el form salvo que esté en fullscreen — en fullscreen, sale del fullscreen primero.
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape && _enFullscreen)
            {
                ToggleFullscreen();
                return true;
            }
            if (keyData == Keys.F11)
            {
                ToggleFullscreen();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        // ── Cleanup ──────────────────────────────────────────────────────────
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);

            try { _timerReintento?.Stop(); _timerReintento?.Dispose(); } catch { }

            // Orden importa: detener antes de disponer; player antes que LibVLC.
            try { if (_player != null && _player.IsPlaying) _player.Stop(); } catch { }
            try { _videoView.MediaPlayer = null; } catch { }
            try { _player?.Dispose(); }   catch { }
            try { _libVlc?.Dispose(); }   catch { }

            _player  = null;
            _libVlc  = null;
        }
    }
}
