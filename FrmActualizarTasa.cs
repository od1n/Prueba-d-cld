// ════════════════════════════════════════════════════════════════════════
// ARCHIVO: FrmActualizarTasa.cs  →  raíz del proyecto
//
// Dialogo modal que muestra al usuario la tasa BCV y paralelo obtenidas
// automaticamente desde dolarapi.com / bcv.org.ve y le permite:
//   - Aceptar y guardar (registra el TasaCambio del dia en BD)
//   - Editar manualmente antes de guardar
//   - Posponer (marcar "no preguntar mas hoy")
//   - Cancelar (no guardar nada)
//
// Diseno visual: alineado al UITheme (paleta clara corporativa).
// ════════════════════════════════════════════════════════════════════════

#nullable enable
using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SistemaGestion
{
    public class FrmActualizarTasa : FormBase
    {
        private NumericUpDown numBcv  = null!;
        private NumericUpDown numPara = null!;
        private Label         lblBcvAnt = null!, lblParaAnt = null!;
        private Label         lblBcvOrig = null!, lblParaOrig = null!;
        private Label         lblEstado  = null!;
        private Button        btnAceptar = null!, btnPosponer = null!, btnCancelar = null!;
        private Button        btnRecargar = null!;
        private TasaCambio?   _anterior;

        // ── Tracking del origen para auditoría ──────────────────────────────
        // Snapshot de los valores que la API devolvió (si los devolvió). Se
        // compara con lo que el usuario tiene al pulsar "Aceptar". Si ambos
        // coinciden → Origen=AUTO_API. Si difieren → Origen=EDITADO. Si la
        // API nunca respondió → Origen=MANUAL.
        private decimal _bcvDeApi  = 0m;
        private decimal _paraDeApi = 0m;
        private bool    _huboRespuestaApi = false;

        public FrmActualizarTasa(TasaCambio? anterior)
        {
            _anterior = anterior;
            ConstruirUI();
            _ = ConsultarTasasAsync();
        }

        private void ConstruirUI()
        {
            Text            = "Actualizar tasa de cambio";
            ClientSize      = new Size(540, 440);
            StartPosition   = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox     = false; MinimizeBox = false;
            BackColor       = UITheme.Fondo;

            // Banner
            var banner = new Panel { Dock = DockStyle.Top, Height = 64,
                BackColor = UITheme.AzulOscuro, Tag = "no-theme" };
            banner.Controls.Add(new Label
            {
                Text = "Tasa de cambio diaria",
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.White, BackColor = UITheme.AzulOscuro,
                AutoSize = false, TextAlign = ContentAlignment.MiddleLeft,
                Dock = DockStyle.Fill, Padding = new Padding(20, 0, 0, 0)
            });

            // Subtitulo
            var lblSub = new Label
            {
                Text = "Se consulto la tasa oficial del BCV y la del mercado paralelo. " +
                       "Revisa los valores y confirma para registrarlos como tasa del dia.",
                Font = UITheme.Body, ForeColor = UITheme.TextoTenue,
                Location = new Point(20, 80), Size = new Size(500, 40)
            };

            // Estado / Spinner
            lblEstado = new Label
            {
                Text = "Consultando dolarapi.com…",
                Font = new Font("Segoe UI", 9f, FontStyle.Italic),
                ForeColor = UITheme.AzulHover,
                Location = new Point(20, 124), Size = new Size(500, 20)
            };

            // ── Tarjeta BCV ────────────────────────────────────────────────
            var cardBcv = new Panel
            {
                Location = new Point(20, 156), Size = new Size(240, 130),
                BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle
            };
            var barraBcv = new Panel
            {
                Location = new Point(0, 0), Size = new Size(4, 130),
                BackColor = UITheme.Azul, Tag = "no-theme"
            };
            cardBcv.Controls.Add(barraBcv);
            cardBcv.Controls.Add(new Label
            {
                Text = "BCV OFICIAL", Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                ForeColor = UITheme.TextoTenue, Location = new Point(14, 10), AutoSize = true
            });
            numBcv = new NumericUpDown
            {
                Location = new Point(14, 30), Size = new Size(160, 32),
                Font = new Font("Segoe UI", 14f, FontStyle.Bold),
                Maximum = 1_000_000m, Minimum = 0m, DecimalPlaces = 4,
                ThousandsSeparator = true
            };
            cardBcv.Controls.Add(numBcv);
            cardBcv.Controls.Add(new Label
            {
                Text = "Bs/USD", Font = new Font("Segoe UI", 8.5f),
                ForeColor = UITheme.TextoTenue, Location = new Point(180, 40), AutoSize = true
            });
            lblBcvAnt = new Label
            {
                Text = "", Font = new Font("Segoe UI", 8.5f),
                ForeColor = UITheme.TextoTenue, Location = new Point(14, 76), AutoSize = true
            };
            cardBcv.Controls.Add(lblBcvAnt);
            lblBcvOrig = new Label
            {
                Text = "", Font = new Font("Segoe UI", 8f, FontStyle.Italic),
                ForeColor = UITheme.AzulHover, Location = new Point(14, 100), AutoSize = true
            };
            cardBcv.Controls.Add(lblBcvOrig);

            // ── Tarjeta Paralelo ───────────────────────────────────────────
            var cardPara = new Panel
            {
                Location = new Point(280, 156), Size = new Size(240, 130),
                BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle
            };
            var barraPar = new Panel
            {
                Location = new Point(0, 0), Size = new Size(4, 130),
                BackColor = Color.FromArgb(83, 74, 183), Tag = "no-theme"
            };
            cardPara.Controls.Add(barraPar);
            cardPara.Controls.Add(new Label
            {
                Text = "PARALELO", Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                ForeColor = UITheme.TextoTenue, Location = new Point(14, 10), AutoSize = true
            });
            numPara = new NumericUpDown
            {
                Location = new Point(14, 30), Size = new Size(160, 32),
                Font = new Font("Segoe UI", 14f, FontStyle.Bold),
                Maximum = 1_000_000m, Minimum = 0m, DecimalPlaces = 4,
                ThousandsSeparator = true
            };
            cardPara.Controls.Add(numPara);
            cardPara.Controls.Add(new Label
            {
                Text = "Bs/USD", Font = new Font("Segoe UI", 8.5f),
                ForeColor = UITheme.TextoTenue, Location = new Point(180, 40), AutoSize = true
            });
            lblParaAnt = new Label
            {
                Text = "", Font = new Font("Segoe UI", 8.5f),
                ForeColor = UITheme.TextoTenue, Location = new Point(14, 76), AutoSize = true
            };
            cardPara.Controls.Add(lblParaAnt);
            lblParaOrig = new Label
            {
                Text = "", Font = new Font("Segoe UI", 8f, FontStyle.Italic),
                ForeColor = Color.FromArgb(83, 74, 183), Location = new Point(14, 100), AutoSize = true
            };
            cardPara.Controls.Add(lblParaOrig);

            btnRecargar = new Button
            {
                Text = "↻ Reintentar consulta", Location = new Point(20, 304),
                Size = new Size(180, 32), Font = UITheme.Body,
                FlatStyle = FlatStyle.Flat, BackColor = Color.White,
                ForeColor = UITheme.AzulOscuro, Cursor = Cursors.Hand
            };
            btnRecargar.FlatAppearance.BorderColor = UITheme.Borde;
            btnRecargar.Click += async (s, e) => await ConsultarTasasAsync();

            // ── Botones ────────────────────────────────────────────────────
            btnAceptar = UITheme.BotonPrimario("Aceptar y guardar");
            btnAceptar.Location = new Point(20, 360);
            btnAceptar.Size     = new Size(220, 44);
            btnAceptar.Click += (s, e) => Aceptar();

            btnPosponer = UITheme.BotonSecundario("Posponer hasta mañana");
            btnPosponer.Location = new Point(252, 360);
            btnPosponer.Size     = new Size(170, 44);
            btnPosponer.Click += (s, e) => Posponer();

            btnCancelar = UITheme.BotonSecundario("Cancelar");
            btnCancelar.Location = new Point(434, 360);
            btnCancelar.Size     = new Size(86, 44);
            btnCancelar.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            Controls.AddRange(new Control[] {
                lblSub, lblEstado, cardBcv, cardPara,
                btnRecargar, btnAceptar, btnPosponer, btnCancelar, banner
            });

            // Mostrar tasa anterior si existe
            if (_anterior != null)
            {
                lblBcvAnt.Text  = "Anterior: " + _anterior.TasaBCV.ToString("N4")
                                + " (" + _anterior.Fecha.ToString("dd/MM") + ")";
                lblParaAnt.Text = "Anterior: " + _anterior.TasaParalelo.ToString("N4")
                                + " (" + _anterior.Fecha.ToString("dd/MM") + ")";
            }
        }

        private async Task ConsultarTasasAsync()
        {
            btnAceptar.Enabled = btnRecargar.Enabled = false;
            lblEstado.Text     = "Consultando dolarapi.com…";
            lblEstado.ForeColor = UITheme.AzulHover;
            _huboRespuestaApi = false;
            _bcvDeApi = _paraDeApi = 0m;
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(12));
                var r = await TasaCambioService.ObtenerTasasActualesAsync(cts.Token);

                if (r.HayBCV)
                {
                    numBcv.Value     = Math.Min(numBcv.Maximum, r.BCV);
                    lblBcvOrig.Text  = "Fuente: " + r.OrigenBCV;
                    _bcvDeApi = r.BCV;
                    _huboRespuestaApi = true;
                }
                else lblBcvOrig.Text = "No se pudo obtener BCV";

                if (r.HayParalelo)
                {
                    numPara.Value     = Math.Min(numPara.Maximum, r.Paralelo);
                    lblParaOrig.Text  = "Fuente: " + r.OrigenPar;
                    _paraDeApi = r.Paralelo;
                    _huboRespuestaApi = true;
                }
                else lblParaOrig.Text = "No se pudo obtener paralelo";

                if (r.Error != null)
                {
                    // Sin internet: el usuario PUEDE seguir ingresando tasa
                    // manualmente. El diálogo permanece abierto editable.
                    lblEstado.Text     = r.Error +
                        " — puedes ingresar las tasas manualmente.";
                    lblEstado.ForeColor = UITheme.Rojo;
                }
                else
                {
                    lblEstado.Text =
                        "Tasas obtenidas. Revisa y confirma. (" +
                        DateTime.Now.ToString("HH:mm:ss") + ")";
                    lblEstado.ForeColor = UITheme.Verde;
                }
            }
            catch (Exception ex)
            {
                lblEstado.Text     = "Error: " + ex.Message +
                    " — puedes ingresar las tasas manualmente.";
                lblEstado.ForeColor = UITheme.Rojo;
            }
            finally
            {
                btnAceptar.Enabled = btnRecargar.Enabled = true;
            }
        }

        private void Aceptar()
        {
            if (numBcv.Value <= 0)
            {
                MessageBox.Show(this,
                    "La tasa BCV es obligatoria y debe ser mayor que cero.",
                    "Tasa invalida", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                numBcv.Focus(); return;
            }

            // Determinar Origen del registro:
            //   • Sin respuesta API → MANUAL (sin red, ingreso a mano)
            //   • Con respuesta API y valores intactos → AUTO_API
            //   • Con respuesta API pero el usuario modificó algún valor → EDITADO
            string origen;
            if (!_huboRespuestaApi)
                origen = "MANUAL";
            else if (numBcv.Value == _bcvDeApi && numPara.Value == _paraDeApi)
                origen = "AUTO_API";
            else
                origen = "EDITADO";

            try
            {
                var t = new TasaCambio
                {
                    Fecha         = DateTime.Today,
                    TasaBCV       = numBcv.Value,
                    TasaParalelo  = numPara.Value,
                    Usuario       = Sesion.NombreUsuario ?? "auto",
                    Origen        = origen,
                    FechaRegistro = DateTime.Now
                };
                // GuardarTasa valida permisos por rol y audita el cambio.
                // Si el usuario no tiene permiso lanzará UnauthorizedAccessException.
                DatabaseService.GuardarTasa(t);
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (UnauthorizedAccessException ex)
            {
                MessageBox.Show(this, ex.Message,
                    "Permiso denegado", MessageBoxButtons.OK, MessageBoxIcon.Stop);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this,
                    "No se pudo guardar la tasa.\n\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Posponer()
        {
            try
            {
                // Marcador: "no volver a preguntar hoy"
                DatabaseService.GuardarConfig(
                    "TasaPospuestaHasta",
                    DateTime.Today.AddDays(1).ToString("yyyy-MM-dd"));
            }
            catch { }
            DialogResult = DialogResult.Ignore;
            Close();
        }
    }
}
