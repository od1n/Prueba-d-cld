// ════════════════════════════════════════════════════════════════════════
// ARCHIVO: UINeo.cs  →  raíz del proyecto
//
// Tema "neumórfico a color" (variante aprobada en el mockup).
// Principio clave: la SUPERFICIE es del MISMO color que el FONDO; el relieve
// lo produce solo la doble sombra (oscura abajo-derecha + luz arriba-izq).
// El color de EHP entra como acento (íconos, barras, números, botón primario).
//
// Paleta tomada del mockup:
//   --app-bg / --surf : #dde5f0     sombra oscura : #bfc9d8
//   tinta            : #26374f      luz           : #f6faff
//   tinta suave      : #6a7890
// ════════════════════════════════════════════════════════════════════════

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace SistemaGestion
{
    public static class UINeo
    {
        // ── Paleta neumórfica ────────────────────────────────────────────────
        public static readonly Color Ground     = Color.FromArgb(221, 229, 240); // #dde5f0
        public static readonly Color Surface     = Color.FromArgb(221, 229, 240); // = fondo (neumorfismo)
        public static readonly Color SurfaceHi    = Color.FromArgb(231, 238, 247); // hover / realce
        public static readonly Color ShadowDark   = Color.FromArgb(163, 176, 196); // sombra (más contraste)
        public static readonly Color ShadowLight  = Color.FromArgb(250, 253, 255); // luz
        public static readonly Color Ink          = Color.FromArgb( 38,  55,  79); // #26374f
        public static readonly Color InkSoft      = Color.FromArgb(106, 120, 144); // #6a7890

        /// <summary>Tinte suave de un color de acento sobre la superficie neumórfica.</summary>
        public static Color Tinte(Color acento, float pesoSuperficie = 0.82f)
            => UICards.Mezclar(acento, Surface, pesoSuperficie);

        // ── Relieve (tarjeta elevada) ───────────────────────────────────────
        /// <summary>
        /// Pinta la doble sombra neumórfica de un elemento "elevado" ubicado en
        /// cardRect, sobre el Graphics del CONTENEDOR (detrás del elemento).
        /// El elemento debe ser del mismo color que el fondo y tener margen
        /// suficiente para que la sombra no se recorte (≥ 8 px por lado).
        /// </summary>
        public static void SombraRelieve(Graphics g, Rectangle cardRect, int radio)
            => SombraDoble(g, cardRect, radio, ShadowDark, ShadowLight, 96, 80);

        /// <summary>Relieve cuyo tono es una variación del color del propio elemento (botones de color).</summary>
        public static void SombraRelieveColor(Graphics g, Rectangle rect, int radio, Color color)
        {
            Color dk = UICards.Mezclar(color, Color.Black, 0.34f);  // tono más oscuro
            Color lt = UICards.Mezclar(color, Color.White, 0.55f);  // tono más claro
            SombraDoble(g, rect, radio, dk, lt, 150, 120);
        }

        private static void SombraDoble(Graphics g, Rectangle cardRect, int radio,
                                        Color oscura, Color clara, int alphaOsc, int alphaClara)
        {
            if (cardRect.Width <= 0 || cardRect.Height <= 0) return;
            var smAnt = g.SmoothingMode;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            // Sombra oscura (abajo-derecha) — de lejos (tenue) a cerca (fuerte)
            for (int o = 8; o >= 1; o--)
            {
                int a = (int)(alphaOsc * (1.0 - (o - 1) / 8.0)); if (a < 6) a = 6;
                using var br = new SolidBrush(Color.FromArgb(Math.Min(a, 255), oscura));
                using var p = UICards.RoundRect(
                    new Rectangle(cardRect.X + o, cardRect.Y + o, cardRect.Width, cardRect.Height), radio);
                g.FillPath(br, p);
            }
            // Luz (arriba-izquierda)
            for (int o = 8; o >= 1; o--)
            {
                int a = (int)(alphaClara * (1.0 - (o - 1) / 8.0)); if (a < 7) a = 7;
                using var br = new SolidBrush(Color.FromArgb(Math.Min(a, 255), clara));
                using var p = UICards.RoundRect(
                    new Rectangle(cardRect.X - o, cardRect.Y - o, cardRect.Width, cardRect.Height), radio);
                g.FillPath(br, p);
            }
            g.SmoothingMode = smAnt;
        }

        /// <summary>Elige relieve gris (superficie) o de color (acento) según el fondo del control.</summary>
        public static void SombraAuto(Graphics g, Control c, int radio)
        {
            if (EsAccento(c.BackColor)) SombraRelieveColor(g, c.Bounds, radio, c.BackColor);
            else                        SombraRelieve(g, c.Bounds, radio);
        }

        private static bool EsAccento(Color bg)
            => !(Cerca(bg, Surface) || Cerca(bg, Ground) || Cerca(bg, SurfaceHi));
        private static bool Cerca(Color a, Color b)
            => Math.Abs(a.R - b.R) + Math.Abs(a.G - b.G) + Math.Abs(a.B - b.B) <= 24;

        /// <summary>
        /// Recorre los hijos de un contenedor y pinta el relieve detrás de los
        /// marcados como neumórficos (Tag contiene "neo"). Llamar desde el
        /// Paint del contenedor.
        /// </summary>
        public static void SombrasHijos(Graphics g, Control contenedor, int radio)
        {
            foreach (Control c in contenedor.Controls)
                if (c.Visible && EsNeo(c))
                    SombraAuto(g, c, radio);
        }

        private static bool EsNeo(Control c) => (c.Tag as string)?.Contains("neo") == true;
        private static string TagNeo(object? actual)
        {
            string s = actual as string ?? "";
            return s.Contains("neo") ? s : (s.Length == 0 ? "neo" : s + " neo");
        }

        // ── Redondeo de región ──────────────────────────────────────────────
        public static void Redondear(Control c, int radio)
        {
            void redib() { if (c.Width > 0 && c.Height > 0) {
                using var p = UICards.RoundRect(new Rectangle(0, 0, c.Width, c.Height), radio);
                c.Region = new Region(p); } }
            c.Resize += (s, e) => redib();
            redib();
        }

        // ── Botones neumórficos ─────────────────────────────────────────────
        /// <summary>Botón "suave": mismo color que el fondo + relieve (lo pinta el padre) + texto tinta.</summary>
        public static void BotonSuave(Button b, Color? tinta = null, int radio = 12)
        {
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 0;
            b.BackColor = Surface;
            b.ForeColor = tinta ?? Ink;
            b.FlatAppearance.MouseOverBackColor = SurfaceHi;
            b.FlatAppearance.MouseDownBackColor = Color.FromArgb(210, 219, 232);
            b.Tag = TagNeo(b.Tag);
            Redondear(b, radio);
        }

        /// <summary>Botón de acento: relleno de color + texto blanco + relieve.</summary>
        public static void BotonAccento(Button b, Color fondo, Color hover, int radio = 12)
        {
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 0;
            b.BackColor = fondo;
            b.ForeColor = Color.White;
            b.FlatAppearance.MouseOverBackColor = hover;
            b.Tag = TagNeo(b.Tag);
            Redondear(b, radio);
        }

        // ── Pozo hundido (para gráficas / datos) ────────────────────────────
        public static void PozoHundido(Graphics g, Rectangle r, int radio)
        {
            if (r.Width <= 0 || r.Height <= 0) return;
            var smAnt = g.SmoothingMode;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (var path = UICards.RoundRect(r, radio))
            {
                using var br = new SolidBrush(Surface);
                g.FillPath(br, path);
            }
            var clip = g.Clip;
            using (var path = UICards.RoundRect(r, radio))
            {
                g.SetClip(path);
                for (int o = 1; o <= 6; o++)
                {
                    int a = 40 - o * 5; if (a < 5) a = 5;
                    using var penD = new Pen(Color.FromArgb(a, ShadowDark), 2f);
                    g.DrawLine(penD, r.X, r.Y + o, r.Right, r.Y + o);
                    g.DrawLine(penD, r.X + o, r.Y, r.X + o, r.Bottom);
                    using var penL = new Pen(Color.FromArgb(a, ShadowLight), 2f);
                    g.DrawLine(penL, r.X, r.Bottom - o, r.Right, r.Bottom - o);
                    g.DrawLine(penL, r.Right - o, r.Y, r.Right - o, r.Bottom);
                }
                g.Clip = clip;
            }
            g.SmoothingMode = smAnt;
        }
    }
}
