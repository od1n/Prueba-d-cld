// ════════════════════════════════════════════════════════════════════════
// ARCHIVO: Tema.cs  →  raíz del proyecto
//
// ░░ ÚNICO PUNTO DE CONTROL del ambiente visual y de comportamiento ░░
//
// Todo el sistema (colores, tipografías, botones, encabezados, tarjetas,
// tablas, mensajes, medidas) se gobierna DESDE AQUÍ. Cambia un token en
// esta sección y cambia en TODAS las ventanas.
//
// Cómo funciona:
//   - FormBase.OnLoad llama Tema.Aplicar(form).
//   - Aplicar recorre cada control y lo RE-MAPEA a los tokens de abajo,
//     incluso si el formulario tenía el color "quemado" en su código.
//   - UINeo y UITheme reenvían sus colores a estos tokens.
//
// Paleta base: "Neumórfico a color" (superficie = fondo; relieve por sombra).
// ════════════════════════════════════════════════════════════════════════

#nullable enable
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Collections.Generic;
using System.Windows.Forms;

namespace SistemaGestion
{
    public static class Tema
    {
        // ══════════════════════════════════════════════════════════════════
        //  1) TOKENS  — el "un solo sitio". Edita aquí para cambiar todo.
        // ══════════════════════════════════════════════════════════════════

        // Base neumórfica
        public static Color Fondo         = Color.FromArgb(221, 229, 240);
        public static Color Superficie    = Color.FromArgb(221, 229, 240); // = fondo (neumorfismo)
        public static Color SuperficieAlt = Color.FromArgb(231, 238, 247); // hover / fila alterna
        public static Color Sombra        = Color.FromArgb(163, 176, 196);
        public static Color Luz           = Color.FromArgb(250, 253, 255);
        public static Color Tinta         = Color.FromArgb( 38,  55,  79);
        public static Color TintaSuave    = Color.FromArgb(106, 120, 144);
        public static Color BordeClaro    = Color.FromArgb(255, 255, 255);

        // Acentos semánticos
        public static Color Primario      = Color.FromArgb( 26,  74, 138);
        public static Color PrimarioHover = Color.FromArgb( 45, 108, 192);
        public static Color Acento        = Color.FromArgb(  0, 167, 236); // cian
        public static Color Ok            = Color.FromArgb( 25, 125,  85);
        public static Color Peligro       = Color.FromArgb(198,  60,  55);
        public static Color Advertencia   = Color.FromArgb(196, 138,  40);
        public static Color Morado        = Color.FromArgb( 95,  80, 175);

        // Encabezado de ventana (claro, como navbar del Menú)
        public static Color HeaderFondo   => Fondo;
        public static Color HeaderTitulo  => Primario;
        public static Color HeaderSub     => TintaSuave;

        // Tablas (DataGridView)
        public static Color GridHeaderFondo => Primario;
        public static Color GridHeaderTinta => Color.White;
        public static Color GridFilaAlterna => SuperficieAlt;

        // Tipografía
        public static Font Body     = new("Segoe UI", 10f,   FontStyle.Regular);
        public static Font BodyBold = new("Segoe UI", 10f,   FontStyle.Bold);
        public static Font Pequeno  = new("Segoe UI", 9f,    FontStyle.Regular);
        public static Font H1       = new("Segoe UI", 17f,   FontStyle.Bold);
        public static Font H2       = new("Segoe UI", 13f,   FontStyle.Bold);
        public static Font H3       = new("Segoe UI", 11.5f, FontStyle.Bold);
        public static Font Mono     = new("Consolas",  10.5f, FontStyle.Regular);
        public static Font FuenteBoton = new("Segoe UI", 10f, FontStyle.Bold);

        // Medidas
        public static int Radio       = 12;
        public static int RadioChico  = 9;
        public static int AlturaBoton = 38;
        public static int AlturaInput = 34;
        public static int Espaciado   = 12;

        // Interruptor: si algo se ve mal con el remapeo agresivo, ponlo en false
        // y las ventanas quedan solo con fondo + botones (sin tocar headers/tiles).
        public static bool NormalizarAgresivo = true;

        // ══════════════════════════════════════════════════════════════════
        //  2) APLICADOR GLOBAL  — reestila cualquier formulario.
        // ══════════════════════════════════════════════════════════════════

        private static bool _paneles = true;

        public static void Aplicar(Form form) => Aplicar(form, true);

        public static void Aplicar(Form form, bool normalizarPaneles)
        {
            if (form is null) return;
            _paneles = normalizarPaneles && NormalizarAgresivo;
            try
            {
                form.Font       = Body;
                form.BackColor  = Fondo;
                form.ForeColor  = Tinta;
                form.AutoScaleMode       = AutoScaleMode.Dpi;
                form.AutoScaleDimensions = new SizeF(96f, 96f);
                form.AutoScroll = true;   // si el contenido no cabe, aparece scroll (no se corta)
            }
            catch { }
            Recorrer(form);
        }

        private static readonly HashSet<Control> _enganchados = new();

        private static void Recorrer(Control parent)
        {
            EngancharDinamico(parent);
            foreach (Control c in parent.Controls)
            {
                try { Estilizar(c); } catch { /* nunca romper una ventana por estilo */ }
                if (c.HasChildren) Recorrer(c);
            }
            try { RepararAnclas(parent); } catch { }
        }

        // Centraliza el COMPORTAMIENTO de redimensionado: los controles que
        // quedaron con el ancla por defecto (Top|Left) y están pegados a un
        // borde se re-anclan a ese borde, para que sigan a la ventana en vez
        // de desaparecer o quedar fuera. Solo toca lo que el formulario no
        // definió explícitamente.
        private static void RepararAnclas(Control cont)
        {
            int W = cont.ClientSize.Width, H = cont.ClientSize.Height;
            if (W < 60 || H < 60) return;
            const AnchorStyles PorDefecto = AnchorStyles.Top | AnchorStyles.Left;
            foreach (Control c in cont.Controls)
            {
                if (c.Dock != DockStyle.None) continue;
                if (c.Anchor != PorDefecto) continue;   // el form ya lo definió: respetar

                if (c is DataGridView || c is ListView || c is TreeView ||
                    c is TabControl || c is RichTextBox)
                {
                    c.Anchor = AnchorStyles.Top | AnchorStyles.Bottom |
                               AnchorStyles.Left | AnchorStyles.Right;
                    continue;
                }
                if (c is Button || c is TextBox || c is ComboBox ||
                    c is DateTimePicker || c is NumericUpDown)
                {
                    // SOLO re-anclar lo que está realmente pegado a un borde
                    // (evita mover controles de layouts absolutos a media ventana).
                    int gr = W - c.Right, gb = H - c.Bottom;
                    AnchorStyles a = AnchorStyles.Top | AnchorStyles.Left;
                    if (gr <= 44 && c.Left > W * 0.30)
                        a = (a & ~AnchorStyles.Left) | AnchorStyles.Right;   // pegado a la derecha
                    if (gb <= 44 && c.Top > H * 0.45)
                        a = (a & ~AnchorStyles.Top) | AnchorStyles.Bottom;   // pegado abajo
                    if ((c is TextBox || c is ComboBox) && c.Width > W * 0.45)
                        a |= AnchorStyles.Left | AnchorStyles.Right;         // caja ancha: estira
                    if (a != (AnchorStyles.Top | AnchorStyles.Left))
                        c.Anchor = a;
                }
            }
        }

        // Aplica el tema tambien a los controles que se crean DESPUES de cargar
        // (p. ej. tarjetas de Reportes que se agregan al terminar una consulta).
        private static void EngancharDinamico(Control cont)
        {
            if (!_enganchados.Add(cont)) return;
            cont.ControlAdded += (s, e) =>
            {
                try { Estilizar(e.Control); } catch { }
                if (e.Control.HasChildren) Recorrer(e.Control);
                EngancharDinamico(e.Control);
            };
            cont.Disposed += (s, e) => _enganchados.Remove(cont);
        }

        private static void Estilizar(Control c)
        {
            switch (c)
            {
                case Button b:        EstilizarBoton(b);   break;
                case TextBox t:       EstilizarInput(t);   break;
                case ComboBox cb:     cb.FlatStyle = FlatStyle.Flat; cb.Font = Body;
                                      cb.BackColor = Color.White;    break;
                case DataGridView g:  EstilizarGrid(g);    break;
                case GroupBox gb:     gb.Font = BodyBold; gb.ForeColor = Tinta; break;
                case Label l:         EstilizarLabel(l);   break;
                case Panel p:         EstilizarPanel(p);   break;
                case CheckBox ck:     ck.Font = Body;      break;
                case RadioButton rb:  rb.Font = Body;      break;
            }
        }

        // ── Paneles: header oscuro → claro; tile de color → redondeado; claro → fondo
        private static void EstilizarPanel(Panel p)
        {
            if (!_paneles) return;
            if ((p.Tag as string) == "tema-hecho") return;

            Color bg = p.BackColor;
            double lum = Luma(bg);
            bool top = p.Dock == DockStyle.Top;
            Color? acc = MapearAcento(bg);

            // ENCABEZADO (barra superior oscura o azul/morada) -> CLARO + titulo oscuro
            if (top && (lum < 132 || acc == Primario || acc == Morado))
            {
                p.BackColor = Fondo;
                p.Tag = "tema-hecho";
                var pnl = p;
                pnl.Paint += (s, e) =>
                {
                    using var pen = new Pen(Color.FromArgb(120, Sombra), 1f);
                    e.Graphics.DrawLine(pen, 0, pnl.Height - 1, pnl.Width, pnl.Height - 1);
                };
                pnl.Invalidate();
                foreach (Control h in Descendientes(p))
                    if (h is Label lb)
                        lb.ForeColor = lb.Font.Bold ? Tinta : TintaSuave;
            }
            // TILE de color (tarjeta con número) -> mismo color mapeado + redondeado
            else if (acc != null && p.Dock == DockStyle.None && lum < 200)
            {
                p.BackColor = acc.Value;
                p.Tag = "tema-hecho";
                Redondear(p, RadioChico);
            }
            // Contenedor claro/neutro -> fondo del sistema
            else if (lum > 200 || bg == SystemColors.Control)
            {
                p.BackColor = Fondo;
            }
        }

        private static IEnumerable<Control> Descendientes(Control c)
        {
            foreach (Control h in c.Controls)
            {
                yield return h;
                if (h.HasChildren)
                    foreach (var n in Descendientes(h)) yield return n;
            }
        }

        // ── Labels: texto claro sobre fondo claro → tinta; ajuste de fuente
        private static void EstilizarLabel(Label l)
        {
            if (l.Font.Size < 8.5f) l.Font = Body;
        }

        // ══════════════════════════════════════════════════════════════════
        //  3) BOTONES, INPUTS, GRID  — un solo estilo para todo el sistema.
        // ══════════════════════════════════════════════════════════════════

        public static void EstilizarBoton(Button b)
        {
            if (b.Tag is "no-theme") return;
            if ((b.Tag as string)?.Contains("neo") == true) return; // ya estilizado a mano

            b.Cursor      = Cursors.Hand;
            b.Font        = FuenteBoton;
            b.MinimumSize = new Size(0, AlturaBoton);

            // TODOS los botones: pastilla neumórfica (relieve propio) con el
            // color semántico SOLO en el texto. Sin rellenos de color.
            Color bg = b.BackColor;
            Color tinta;
            if (bg == SystemColors.Control) tinta = Primario;
            else
            {
                Color? acc = MapearAcento(bg);
                if      (acc == Peligro)     tinta = Peligro;
                else if (acc == Ok)          tinta = Ok;
                else if (acc == Morado)      tinta = Morado;
                else if (acc == Advertencia) tinta = Advertencia;
                else if (acc == Acento)      tinta = Primario;   // cian: poco contraste
                else if (acc == Primario)    tinta = Primario;
                else if (Luma(bg) > 205)     tinta = Tinta;      // secundario neutro
                else                         tinta = Primario;
            }
            PastillaSuave(b, tinta);
        }

        // Botón neumórfico ÚNICO: mismo tono del fondo + bisel de relieve
        // (luz arriba-izq, sombra abajo-der) + color semántico en el texto.
        public static void PastillaSuave(Button b, Color? tinta = null)
        {
            int radio = RadioChico;
            b.FlatStyle = FlatStyle.Flat;
            b.BackColor = Fondo;
            b.ForeColor = tinta ?? Primario;
            b.FlatAppearance.BorderSize = 0;
            b.FlatAppearance.MouseOverBackColor = SuperficieAlt;
            b.FlatAppearance.MouseDownBackColor = Color.FromArgb(206, 216, 230);
            b.Tag = MarcaNeo(b.Tag);
            Redondear(b, radio);
            b.Paint += (s, e) =>
                BevelNeo(e.Graphics, new Rectangle(0, 0, b.Width, b.Height), radio);
        }

        // Alias por compatibilidad (ya no existen rellenos de color)
        public static void PastillaAccento(Button b, Color fondo, Color hover)
            => PastillaSuave(b, fondo);

        /// <summary>Bisel neumórfico DENTRO del control: funciona en cualquier layout.</summary>
        public static void BevelNeo(Graphics g, Rectangle r, int radio)
        {
            if (r.Width < 6 || r.Height < 6) return;
            var sm = g.SmoothingMode; g.SmoothingMode = SmoothingMode.AntiAlias;
            int d = radio * 2;
            if (d > r.Width) d = r.Width;
            if (d > r.Height) d = r.Height;
            int rr = d / 2;
            using var penL = new Pen(Color.FromArgb(235, Luz), 1.8f);
            using var penD = new Pen(Color.FromArgb(150, Sombra), 1.8f);
            g.DrawArc(penL, r.X + 1, r.Y + 1, d, d, 180, 90);
            g.DrawLine(penL, r.X + rr, r.Y + 1, r.Right - rr, r.Y + 1);
            g.DrawLine(penL, r.X + 1, r.Y + rr, r.X + 1, r.Bottom - rr);
            g.DrawArc(penD, r.Right - 2 - d, r.Bottom - 2 - d, d, d, 0, 90);
            g.DrawLine(penD, r.X + rr, r.Bottom - 2, r.Right - rr, r.Bottom - 2);
            g.DrawLine(penD, r.Right - 2, r.Y + rr, r.Right - 2, r.Bottom - rr);
            g.SmoothingMode = sm;
        }

        public static void EstilizarInput(TextBox t)
        {
            t.Font        = Body;
            t.BorderStyle = BorderStyle.FixedSingle;
            if (!t.Multiline) t.MinimumSize = new Size(0, AlturaInput);
            if (!t.ReadOnly) t.BackColor = Color.White;
        }

        public static void EstilizarGrid(DataGridView dgv)
        {
            dgv.BorderStyle               = BorderStyle.None;
            dgv.BackgroundColor           = Color.White;
            dgv.GridColor                 = Color.FromArgb(215, 222, 232);
            dgv.RowHeadersVisible         = false;
            dgv.AllowUserToAddRows        = false;
            dgv.AllowUserToResizeRows     = false;
            dgv.SelectionMode             = DataGridViewSelectionMode.FullRowSelect;
            dgv.MultiSelect               = false;
            dgv.EnableHeadersVisualStyles = false;
            dgv.Font                      = Body;
            dgv.ColumnHeadersDefaultCellStyle.BackColor = GridHeaderFondo;
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = GridHeaderTinta;
            dgv.ColumnHeadersDefaultCellStyle.Font      = BodyBold;
            dgv.ColumnHeadersHeight = 34;
            dgv.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            dgv.DefaultCellStyle.SelectionBackColor = Primario;
            dgv.DefaultCellStyle.SelectionForeColor = Color.White;
            dgv.AlternatingRowsDefaultCellStyle.BackColor = GridFilaAlterna;
            dgv.RowTemplate.Height = 32;
        }

        // ══════════════════════════════════════════════════════════════════
        //  4) COMPONENTES  — para ventanas nuevas o reescritas (opcional).
        // ══════════════════════════════════════════════════════════════════

        public enum TipoBoton { Primario, Secundario, Peligro, Exito, Acento }

        public static Button Boton(string texto, TipoBoton tipo = TipoBoton.Primario)
        {
            var b = new Button { Text = texto, AutoSize = false, Height = AlturaBoton };
            switch (tipo)
            {
                case TipoBoton.Secundario: PastillaSuave(b, Tinta); break;
                case TipoBoton.Peligro:    PastillaSuave(b, Peligro); break;
                case TipoBoton.Exito:      PastillaSuave(b, Ok); break;
                case TipoBoton.Acento:     PastillaSuave(b, Primario); break;
                default:                   PastillaSuave(b, Primario); break;
            }
            return b;
        }

        /// <summary>Encabezado claro estándar (título + subtítulo) con divisor.</summary>
        public static Panel Header(string titulo, string? subtitulo = null, int alto = 56)
        {
            var p = new Panel { Dock = DockStyle.Top, Height = alto, BackColor = HeaderFondo, Tag = "tema-hecho" };
            p.Paint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(120, Sombra), 1f);
                e.Graphics.DrawLine(pen, 0, p.Height - 1, p.Width, p.Height - 1);
            };
            var lblT = new Label { Text = titulo, Font = H2, ForeColor = HeaderTitulo,
                AutoSize = false, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(16, subtitulo == null ? 0 : 6, 0, 0) };
            p.Controls.Add(lblT);
            if (subtitulo != null)
            {
                var lblS = new Label { Text = subtitulo, Font = Pequeno, ForeColor = HeaderSub,
                    AutoSize = false, Dock = DockStyle.Bottom, Height = 18,
                    TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(18, 0, 0, 6) };
                p.Controls.Add(lblS);
            }
            return p;
        }

        // ══════════════════════════════════════════════════════════════════
        //  5) MENSAJES  — cuadros de diálogo consistentes en todo el sistema.
        // ══════════════════════════════════════════════════════════════════

        public static void Info(IWin32Window? o, string msg, string titulo = "Información")
            => MessageBox.Show(o, msg, titulo, MessageBoxButtons.OK, MessageBoxIcon.Information);
        public static void Exito(IWin32Window? o, string msg, string titulo = "Listo")
            => MessageBox.Show(o, msg, titulo, MessageBoxButtons.OK, MessageBoxIcon.Information);
        public static void Advertir(IWin32Window? o, string msg, string titulo = "Atención")
            => MessageBox.Show(o, msg, titulo, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        public static void Error(IWin32Window? o, string msg, string titulo = "Error")
            => MessageBox.Show(o, msg, titulo, MessageBoxButtons.OK, MessageBoxIcon.Error);
        public static bool Confirmar(IWin32Window? o, string msg, string titulo = "Confirmar")
            => MessageBox.Show(o, msg, titulo, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;

        // ══════════════════════════════════════════════════════════════════
        //  6) UTILIDADES internas
        // ══════════════════════════════════════════════════════════════════

        public static void Redondear(Control c, int radio)
        {
            void redib() { if (c.Width > 0 && c.Height > 0) {
                using var p = RoundRect(new Rectangle(0, 0, c.Width, c.Height), radio);
                c.Region = new Region(p); } }
            c.Resize += (s, e) => redib();
            redib();
        }

        internal static double Luma(Color c) => 0.299 * c.R + 0.587 * c.G + 0.114 * c.B;
        internal static int Satur(Color c) => Math.Max(c.R, Math.Max(c.G, c.B)) - Math.Min(c.R, Math.Min(c.G, c.B));

        /// <summary>Mapea un color arbitrario al token de acento más cercano, o null si es neutro.</summary>
        internal static Color? MapearAcento(Color c)
        {
            int r = c.R, g = c.G, b = c.B;
            if (Satur(c) < 24) return null;                                   // gris/neutro
            if (r > 140 && r >= g + 30 && r >= b + 30) return Peligro;         // rojo
            if (g >= 105 && g >= r + 12 && g >= b + 12) return Ok;             // verde
            if (r >= 105 && b >= 120 && (r + b) - 2 * g > 45) return Morado;   // púrpura
            if (b >= 110 && b >= r && b >= g - 10)                             // azul/cian
                return (g >= 150 && r <= 95) ? Acento : Primario;
            if (r >= 140 && g >= 85 && b <= 90) return Advertencia;            // naranja/marrón
            return null;
        }

        internal static Color Oscurecer(Color c, float f)
            => Color.FromArgb(
                (int)(c.R * (1 - f)), (int)(c.G * (1 - f)), (int)(c.B * (1 - f)));


        private static string MarcaNeo(object? actual)
        {
            string s = actual as string ?? "";
            return s.Contains("neo") ? s : (s.Length == 0 ? "neo" : s + " neo");
        }

        internal static GraphicsPath RoundRect(Rectangle r, int rad)
        {
            var p = new GraphicsPath();
            if (rad <= 0) { p.AddRectangle(r); return p; }
            int d = rad * 2;
            if (d > r.Width) d = r.Width;
            if (d > r.Height) d = r.Height;
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }
    }
}
