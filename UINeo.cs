// ════════════════════════════════════════════════════════════════════════
// ARCHIVO: UINeo.cs  →  raíz del proyecto
//
// Tema "neumórfico a color" para el rediseño de la app de escritorio.
// Base neumórfica (relieve suave, mismo tono) sobre un fondo gris-azulado,
// con acentos de color de EHP donde ayudan a leer (íconos, barras, datos).
//
// Reutiliza UICards.RoundRect y UICards.Mezclar para no duplicar lógica.
// Piloto: MenuPrincipalV2 y FrmDashboardV2.
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
        public static readonly Color Ground   = Color.FromArgb(223, 230, 241); // fondo gris-azulado
        public static readonly Color Surface   = Color.FromArgb(234, 239, 247); // tarjeta "elevada"
        public static readonly Color SurfaceHi  = Color.FromArgb(243, 247, 252); // hover / realce
        public static readonly Color ShadowDark = Color.FromArgb( 38,  60,  96); // sombra (con alpha)
        public static readonly Color ShadowLight= Color.FromArgb(255, 255, 255); // luz (con alpha)
        public static readonly Color Ink        = Color.FromArgb( 30,  42,  63);
        public static readonly Color InkSoft     = Color.FromArgb( 96, 110, 132);

        /// <summary>Tinte suave de un color de acento sobre la superficie neumórfica.</summary>
        public static Color Tinte(Color acento, float pesoSuperficie = 0.82f)
            => UICards.Mezclar(acento, Surface, pesoSuperficie);

        /// <summary>
        /// Pinta la sombra neumórfica DOBLE de una tarjeta "elevada" ubicada en
        /// cardRect, directamente sobre el Graphics del contenedor (detrás de la
        /// tarjeta). Sombra oscura abajo-derecha + luz arriba-izquierda.
        /// </summary>
        public static void SombraRelieve(Graphics g, Rectangle cardRect, int radio)
        {
            if (cardRect.Width <= 0 || cardRect.Height <= 0) return;
            var smAnt = g.SmoothingMode;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // Sombra oscura (abajo-derecha)
            for (int off = 1; off <= 6; off++)
            {
                int a = 11 - off; if (a < 2) a = 2;
                using var br = new SolidBrush(Color.FromArgb(a, ShadowDark));
                using var p = UICards.RoundRect(
                    new Rectangle(cardRect.X + off, cardRect.Y + off + 1, cardRect.Width, cardRect.Height), radio);
                g.FillPath(br, p);
            }
            // Luz (arriba-izquierda)
            for (int off = 1; off <= 6; off++)
            {
                int a = 26 - off * 3; if (a < 3) a = 3;
                using var br = new SolidBrush(Color.FromArgb(a, ShadowLight));
                using var p = UICards.RoundRect(
                    new Rectangle(cardRect.X - off, cardRect.Y - off, cardRect.Width, cardRect.Height), radio);
                g.FillPath(br, p);
            }
            g.SmoothingMode = smAnt;
        }

        /// <summary>
        /// Pinta un "pozo" hundido (inset) — para contenedores de datos/gráficas:
        /// sombra interior arriba-izquierda + luz interior abajo-derecha.
        /// </summary>
        /// <summary>Convierte un botón plano en "pastilla" redondeada, conservando su color de acento.</summary>
        public static void PastillaBoton(Button b, int radio = 10)
        {
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 0;
            void redib() { if (b.Width > 0 && b.Height > 0) {
                using var p = UICards.RoundRect(new Rectangle(0, 0, b.Width, b.Height), radio);
                b.Region = new Region(p); } }
            b.Resize += (s, e) => redib();
            redib();
        }

        /// <summary>Dibuja un borde claro redondeado en el borde del control (realce de tarjeta elevada).</summary>
        public static void BordeClaro(Graphics g, Rectangle r, int radio)
        {
            var sm = g.SmoothingMode; g.SmoothingMode = SmoothingMode.AntiAlias;
            using var pen = new Pen(Color.FromArgb(252, 253, 255), 1.4f);
            using var p = UICards.RoundRect(new Rectangle(r.X, r.Y, r.Width - 1, r.Height - 1), radio);
            g.DrawPath(pen, p);
            g.SmoothingMode = sm;
        }

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
                for (int off = 1; off <= 5; off++)
                {
                    int a = 16 - off * 2; if (a < 2) a = 2;
                    using var penD = new Pen(Color.FromArgb(a, ShadowDark), 2f);
                    g.DrawArc(penD, r.X - off, r.Y - off, radio * 2 + off, radio * 2 + off, 150, 120);
                    g.DrawLine(penD, r.X, r.Y + off, r.Right, r.Y + off);
                    g.DrawLine(penD, r.X + off, r.Y, r.X + off, r.Bottom);
                }
                g.Clip = clip;
            }
            g.SmoothingMode = smAnt;
        }
    }
}
