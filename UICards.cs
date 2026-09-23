// ════════════════════════════════════════════════════════════════════════
// ARCHIVO: UICards.cs  →  raíz del proyecto
//
// Helpers para crear tarjetas de módulo con el estilo unificado de
// MenuPrincipalV2: wash de color del módulo (~92% blanco + 8% color),
// borde saturado, barra lateral de color, hover más intenso, esquinas
// redondeadas. Centralizado aquí para que FrmTransacciones, FrmArchivos
// y cualquier otra pantalla con grid de tarjetas use el mismo lenguaje
// visual.
// ════════════════════════════════════════════════════════════════════════

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace SistemaGestion
{
    public static class UICards
    {
        private static readonly Color CTexto    = Color.FromArgb(30, 41, 59);
        private static readonly Color CTextoTen = Color.FromArgb(100, 116, 139);

        /// <summary>
        /// Mezcla dos colores. ratio = peso del SEGUNDO color (0..1).
        /// Mezclar(azul, blanco, 0.92) = blanco con 8% de azul = wash muy claro.
        /// </summary>
        public static Color Mezclar(Color c1, Color c2, float ratio)
        {
            ratio = Math.Max(0f, Math.Min(1f, ratio));
            float a = 1f - ratio;
            return Color.FromArgb(
                (int)(c1.R * a + c2.R * ratio),
                (int)(c1.G * a + c2.G * ratio),
                (int)(c1.B * a + c2.B * ratio));
        }

        public static GraphicsPath RoundRect(Rectangle r, int rad)
        {
            var p = new GraphicsPath();
            p.AddArc(r.X, r.Y, rad * 2, rad * 2, 180, 90);
            p.AddArc(r.Right - rad * 2, r.Y, rad * 2, rad * 2, 270, 90);
            p.AddArc(r.Right - rad * 2, r.Bottom - rad * 2, rad * 2, rad * 2, 0, 90);
            p.AddArc(r.X, r.Bottom - rad * 2, rad * 2, rad * 2, 90, 90);
            p.CloseFigure();
            return p;
        }

        /// <summary>
        /// Crea una tarjeta de módulo con el estilo unificado.
        /// </summary>
        public static Panel CrearTarjeta(
            string icono, string titulo, string subtitulo,
            Color colorAcento, Action accion, bool tieneAcceso = true,
            ToolTip tip = null, string textoTooltip = null)
        {
            // Neumórfico a color: superficie = fondo; relieve por sombra; acento en barra/ícono/texto.
            Color surf  = tieneAcceso ? Tema.Superficie : Color.FromArgb(224, 228, 235);
            Color borde = tieneAcceso ? Color.FromArgb(250, 252, 255) : Color.FromArgb(214, 218, 226);
            Color hover = Tema.SuperficieAlt;

            var card = new Panel
            {
                BackColor = surf,
                Cursor = tieneAcceso ? Cursors.Hand : Cursors.Default
            };
            card.Resize += (s, e) =>
            {
                if (card.Width <= 0 || card.Height <= 0) return;
                using var path = RoundRect(new Rectangle(0, 0, card.Width, card.Height), 14);
                card.Region = new Region(path);
            };
            card.Paint += (s, e) =>
            {
                var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
                using var pen = new Pen(borde, 1.4f);
                using var path = RoundRect(new Rectangle(0, 0, card.Width - 1, card.Height - 1), 14);
                g.DrawPath(pen, path);
            };

            // Relieve neumórfico: lo pinta el contenedor detrás de la tarjeta.
            bool hooked = false;
            card.ParentChanged += (s, e) =>
            {
                if (hooked || card.Parent == null) return;
                hooked = true;
                var par = card.Parent;
                par.Paint += (ss, ee) =>
                {
                    if (card.IsDisposed || !card.Visible) return;
                    ee.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    UINeo.SombraRelieve(ee.Graphics, card.Bounds, 14);
                };
                par.Invalidate();
            };
            card.LocationChanged += (s, e) => card.Parent?.Invalidate();

            var bar = new Panel { Dock = DockStyle.Left, Width = 4,
                BackColor = tieneAcceso ? colorAcento : CTextoTen };

            var pnlIco = new Panel { Size = new Size(48, 48), Location = new Point(18, 18), BackColor = surf };
            Color fondoIco = tieneAcceso ? Mezclar(colorAcento, surf, 0.80f) : Color.FromArgb(226, 230, 236);
            pnlIco.Paint += (s, e) =>
            {
                var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
                using var br = new SolidBrush(fondoIco);
                g.FillEllipse(br, 0, 0, pnlIco.Width - 1, pnlIco.Height - 1);
            };
            var lblIco = new Label
            {
                Text = icono, Font = new Font("Segoe UI", 18f, FontStyle.Bold),
                ForeColor = tieneAcceso ? colorAcento : CTextoTen, Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter, BackColor = Color.Transparent
            };
            pnlIco.Controls.Add(lblIco);

            var lblT = new Label
            {
                Text = titulo, Font = new Font("Segoe UI", 11.5f, FontStyle.Bold),
                ForeColor = tieneAcceso ? Tema.Tinta : CTextoTen, AutoSize = false,
                Location = new Point(78, 22), Size = new Size(260, 22), BackColor = surf
            };
            var lblS = new Label
            {
                Text = subtitulo, Font = new Font("Segoe UI", 9f), ForeColor = Tema.TintaSuave,
                AutoSize = false, Location = new Point(78, 46), Size = new Size(260, 18), BackColor = surf
            };

            var pnlFooter = new Panel { Dock = DockStyle.Bottom, Height = 30, BackColor = surf };
            var lblAbrir = new Label
            {
                Text = tieneAcceso ? "Abrir  →" : "  Sin acceso  ",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = tieneAcceso ? colorAcento : CTextoTen,
                AutoSize = true, Location = new Point(20, 8), BackColor = surf
            };
            pnlFooter.Controls.Add(lblAbrir);

            card.Controls.Add(pnlFooter);
            card.Controls.Add(lblS);
            card.Controls.Add(lblT);
            card.Controls.Add(pnlIco);
            card.Controls.Add(bar);

            if (tip != null && !string.IsNullOrEmpty(textoTooltip))
            {
                tip.SetToolTip(card, textoTooltip);
                tip.SetToolTip(lblT, textoTooltip);
                tip.SetToolTip(lblS, textoTooltip);
                tip.SetToolTip(lblIco, textoTooltip);
            }

            if (tieneAcceso)
            {
                void Pintar(Control c, Color bg)
                {
                    if (c == bar || c == pnlIco) return;
                    if (c.BackColor != Color.Transparent && (c is Label || c is Panel))
                        c.BackColor = bg;
                    foreach (Control h in c.Controls) Pintar(h, bg);
                }
                EventHandler enter = (s, e) => Pintar(card, hover);
                EventHandler leave = (s, e) => Pintar(card, surf);
                EventHandler clk   = (s, e) => {
                    try { accion(); }
                    catch (Exception ex) { MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
                };
                void Enganchar(Control c)
                {
                    c.MouseEnter += enter; c.MouseLeave += leave; c.Click += clk;
                    foreach (Control h in c.Controls) Enganchar(h);
                }
                Enganchar(card);
            }

            return card;
        }

        /// <summary>
        /// Distribuye una lista de tarjetas en un panel contenedor en grid
        /// responsive. El número de columnas se adapta al ancho disponible
        /// para que cada tarjeta tenga al menos minAncho píxeles. Las tarjetas
        /// tienen alto fijo. El bloque se centra verticalmente si sobra espacio.
        /// </summary>
        public static void DistribuirGrid(Panel contenedor, List<Panel> tarjetas,
                                          int minAncho = 240, int alto = 116, int gap = 14)
        {
            void Reposicionar()
            {
                if (tarjetas.Count == 0) return;
                int totalAncho = contenedor.ClientSize.Width  - 4;
                int totalAlto  = contenedor.ClientSize.Height - 4;
                if (totalAncho <= 0) return;

                int cols = Math.Max(1, (totalAncho + gap) / (minAncho + gap));
                cols = Math.Min(cols, tarjetas.Count);
                int w = (totalAncho - (cols - 1) * gap) / cols;
                int filas = (tarjetas.Count + cols - 1) / cols;
                int alturaUsada = filas * alto + (filas - 1) * gap;
                int offY = Math.Max(0, (totalAlto - alturaUsada) / 2);

                contenedor.SuspendLayout();
                for (int i = 0; i < tarjetas.Count; i++)
                {
                    int col = i % cols, row = i / cols;
                    tarjetas[i].SetBounds(col * (w + gap), offY + row * (alto + gap), w, alto);
                }
                contenedor.ResumeLayout();
            }
            contenedor.SizeChanged  += (s, e) => Reposicionar();
            contenedor.HandleCreated += (s, e) => Reposicionar();
        }

        /// <summary>
        /// Versión scrollable: se pone arriba (sin centrar) y la altura mínima
        /// crece con las filas para que el scroll funcione.
        /// </summary>
        public static void DistribuirGridScrollable(Panel contenedor, List<Panel> tarjetas,
                                                    int minAncho = 240, int alto = 116, int gap = 14,
                                                    int padX = 4, int padY = 4)
        {
            void Reposicionar()
            {
                if (tarjetas.Count == 0) return;
                int totalAncho = contenedor.ClientSize.Width - padX * 2;
                if (totalAncho <= 0) return;

                int cols = Math.Max(1, (totalAncho + gap) / (minAncho + gap));
                cols = Math.Min(cols, tarjetas.Count);
                int w = (totalAncho - (cols - 1) * gap) / cols;
                int filas = (tarjetas.Count + cols - 1) / cols;

                contenedor.SuspendLayout();
                for (int i = 0; i < tarjetas.Count; i++)
                {
                    int col = i % cols, row = i / cols;
                    tarjetas[i].SetBounds(padX + col * (w + gap),
                                          padY + row * (alto + gap), w, alto);
                }
                contenedor.AutoScrollMinSize =
                    new Size(0, padY * 2 + filas * alto + (filas - 1) * gap);
                contenedor.ResumeLayout();
            }
            contenedor.SizeChanged   += (s, e) => Reposicionar();
            contenedor.HandleCreated += (s, e) => Reposicionar();
        }
    }
}
