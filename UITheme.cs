// ════════════════════════════════════════════════════════════════════════
// ARCHIVO: UITheme.cs  →  raíz del proyecto
//
// Paleta y tipografía centralizadas para los formularios del sistema.
// Reemplaza los Color.FromArgb() esparcidos por cada Frm*.cs.
//
// Aplicación automática: FormBase.OnLoad invoca UITheme.Aplicar(this) y
// recorre los controles aplicando estilos consistentes.
// ════════════════════════════════════════════════════════════════════════

using System;
using System.Drawing;
using System.Windows.Forms;

namespace SistemaGestion
{
    public static class UITheme
    {
        // ── Paleta corporativa ───────────────────────────────────────────────
        // Mismos tonos que el portal web (app.css) para coherencia cross-canal.
        public static readonly Color Azul        = Color.FromArgb(26,  74, 138);  // #1A4A8A
        public static readonly Color AzulOscuro  = Color.FromArgb(15,  45,  85);  // #0F2D55
        public static readonly Color AzulHover   = Color.FromArgb(45, 108, 192);  // #2D6CC0
        public static readonly Color Cyan        = Color.FromArgb(  0, 180, 216); // #00B4D8
        public static readonly Color Texto       = Color.FromArgb( 30,  30,  45);
        public static readonly Color TextoTenue  = Color.FromArgb(100, 110, 125);
        public static readonly Color Fondo       = Color.FromArgb(221, 229, 240); // = UINeo.Ground
        public static readonly Color FondoCard   = Color.White;
        public static readonly Color Borde       = Color.FromArgb(215, 222, 230);
        public static readonly Color Verde       = Color.FromArgb( 46, 125,  50);
        public static readonly Color Rojo        = Color.FromArgb(180,  40,  40);
        public static readonly Color Naranja     = Color.FromArgb(210, 153,  34);

        // ── Tipografía ──────────────────────────────────────────────────────
        // Segoe UI siempre disponible en Windows 7+. Tamaños subidos +0.5pt
        // en abril 2026 a pedido de usuario para mejor legibilidad general.
        public static readonly Font  Body        = new("Segoe UI", 10f,   FontStyle.Regular);
        public static readonly Font  BodyBold    = new("Segoe UI", 10f,   FontStyle.Bold);
        public static readonly Font  Pequeno     = new("Segoe UI", 9f,    FontStyle.Regular);
        public static readonly Font  H1          = new("Segoe UI", 17f,   FontStyle.Bold);
        public static readonly Font  H2          = new("Segoe UI", 13f,   FontStyle.Bold);
        public static readonly Font  H3          = new("Segoe UI", 11.5f, FontStyle.Bold);
        public static readonly Font  Mono        = new("Consolas", 10.5f, FontStyle.Regular);
        public static readonly Font  Boton       = new("Segoe UI", 10.5f, FontStyle.Bold);

        // ── Métricas ────────────────────────────────────────────────────────
        public const int    Padding         = 12;
        public const int    AlturaInput     = 34;   // +2 para dar aire al texto 10pt
        public const int    AlturaBoton     = 38;
        public const int    Radio           = 6;

        /// <summary>
        /// Aplica el tema al formulario y a todos sus controles. Idempotente.
        /// Se invoca automáticamente desde FormBase.OnLoad.
        /// </summary>
        public static void Aplicar(Form form)
        {
            if (form is null) return;
            form.Font      = Body;
            form.BackColor = UINeo.Ground;
            form.ForeColor = UINeo.Ink;
            // AutoScaleMode = Dpi: el form se reajusta al DPI del monitor.
            form.AutoScaleMode      = AutoScaleMode.Dpi;
            form.AutoScaleDimensions = new SizeF(96f, 96f);
            AplicarRecursivo(form);
        }

        private static void AplicarRecursivo(Control parent)
        {
            foreach (Control c in parent.Controls)
            {
                switch (c)
                {
                    case Button b:
                        EstilizarBoton(b);
                        break;
                    case TextBox t when !t.ReadOnly:
                        EstilizarInput(t);
                        break;
                    case ComboBox cb:
                        cb.FlatStyle = FlatStyle.Flat;
                        cb.Font      = Body;
                        cb.BackColor = Color.White;
                        break;
                    case Label l:
                        if (l.Font.Size < 8.5f) l.Font = Body;
                        break;
                    case Panel p when p.BackColor == SystemColors.Control:
                        p.BackColor = UINeo.Ground;
                        break;
                    case GroupBox g:
                        g.Font      = BodyBold;
                        g.ForeColor = AzulOscuro;
                        break;
                    case DataGridView dgv:
                        EstilizarGrid(dgv);
                        break;
                }
                if (c.HasChildren) AplicarRecursivo(c);
            }
        }

        public static void EstilizarBoton(Button b)
        {
            if (b.Tag is "no-theme") return;
            if ((b.Tag as string)?.Contains("neo") == true) return; // ya estilizado a mano

            b.Cursor      = Cursors.Hand;
            b.Font        = Boton;
            b.MinimumSize = new Size(0, AlturaBoton);

            Color bg = b.BackColor;
            if (bg == SystemColors.Control)                 // por defecto = primario azul
                UINeo.BotonAccento(b, Azul, AzulHover);
            else if (EsRojizo(bg))                          // peligro / eliminar
                UINeo.BotonAccento(b, Rojo, Color.FromArgb(150, 30, 30));
            else if (EsVerdoso(bg))                         // confirmar / ok
                UINeo.BotonAccento(b, Verde, Color.FromArgb(30, 95, 45));
            else if (EsCyan(bg))                            // acento cian
                UINeo.BotonAccento(b, Cyan, Color.FromArgb(0, 150, 183));
            else if (EsClaro(bg))                           // secundario claro -> suave
                UINeo.BotonSuave(b, Azul);
            else                                            // azulado u otro -> primario
                UINeo.BotonAccento(b, Azul, AzulHover);
        }

        private static bool EsRojizo(Color c) => c.R > 150 && c.G < 110 && c.B < 110;
        private static bool EsVerdoso(Color c) => c.G > 110 && c.G > c.R + 20 && c.G > c.B + 20;
        private static bool EsCyan(Color c)    => c.B > 150 && c.G > 140 && c.R < 110;
        private static bool EsClaro(Color c)   => c.R > 205 && c.G > 205 && c.B > 205;

        public static void EstilizarInput(TextBox t)
        {
            t.Font        = Body;
            t.BorderStyle = BorderStyle.FixedSingle;
            t.MinimumSize = new Size(0, AlturaInput);
        }

        public static void EstilizarGrid(DataGridView dgv)
        {
            dgv.BorderStyle               = BorderStyle.None;
            dgv.BackgroundColor           = FondoCard;
            dgv.GridColor                 = Borde;
            dgv.RowHeadersVisible         = false;
            dgv.AllowUserToAddRows        = false;
            dgv.AllowUserToResizeRows     = false;
            dgv.SelectionMode             = DataGridViewSelectionMode.FullRowSelect;
            dgv.MultiSelect               = false;
            dgv.EnableHeadersVisualStyles = false;
            dgv.Font                      = Body;

            dgv.ColumnHeadersDefaultCellStyle.BackColor = AzulOscuro;
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgv.ColumnHeadersDefaultCellStyle.Font      = BodyBold;
            dgv.ColumnHeadersHeight = 34;
            dgv.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;

            dgv.DefaultCellStyle.SelectionBackColor = Azul;
            dgv.DefaultCellStyle.SelectionForeColor = Color.White;
            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 253);
            dgv.RowTemplate.Height = 32;
        }

        // ── Helpers de creación rápida ──────────────────────────────────────
        /// <summary>Crea un botón estilizado como acción primaria (cyan).</summary>
        public static Button BotonPrimario(string texto)
        {
            var b = new Button { Text = texto };
            EstilizarBoton(b);
            b.BackColor = Cyan;
            b.ForeColor = AzulOscuro;
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(0, 150, 183);
            return b;
        }

        /// <summary>Crea un botón estilizado como acción secundaria (gris).</summary>
        public static Button BotonSecundario(string texto)
        {
            var b = new Button { Text = texto };
            EstilizarBoton(b);
            b.BackColor = FondoCard;
            b.ForeColor = TextoTenue;
            b.FlatAppearance.BorderSize = 1;
            b.FlatAppearance.BorderColor = Borde;
            b.FlatAppearance.MouseOverBackColor = Borde;
            return b;
        }

        /// <summary>Crea un botón estilizado como acción peligrosa (rojo).</summary>
        public static Button BotonPeligro(string texto)
        {
            var b = new Button { Text = texto };
            EstilizarBoton(b);
            b.BackColor = Rojo;
            b.ForeColor = Color.White;
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(150, 30, 30);
            return b;
        }
    }
}
