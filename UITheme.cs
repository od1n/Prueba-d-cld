// ════════════════════════════════════════════════════════════════════════
// ARCHIVO: UITheme.cs  →  raíz del proyecto
//
// CAPA DE COMPATIBILIDAD. El control real del tema vive en Tema.cs.
// Aquí solo se reenvían los nombres antiguos (UITheme.Azul, UITheme.Rojo,
// UITheme.Aplicar, etc.) a los tokens y métodos de Tema, para que las
// ~50 ventanas que ya referencian UITheme sigan el punto único de control.
//
// Para cambiar colores/tipografía/botones del sistema: edita Tema.cs.
// ════════════════════════════════════════════════════════════════════════

using System;
using System.Drawing;
using System.Windows.Forms;

namespace SistemaGestion
{
    public static class UITheme
    {
        // ── Colores (reenvían a Tema) ────────────────────────────────────────
        public static Color Azul        => Tema.Primario;
        public static Color AzulOscuro  => Tema.Oscurecer(Tema.Primario, 0.28f);
        public static Color AzulHover   => Tema.PrimarioHover;
        public static Color Cyan        => Tema.Acento;
        public static Color Texto       => Tema.Tinta;
        public static Color TextoTenue  => Tema.TintaSuave;
        public static Color Fondo       => Tema.Fondo;
        public static Color FondoCard   => Color.White;
        public static Color Borde       => Color.FromArgb(215, 222, 232);
        public static Color Verde       => Tema.Ok;
        public static Color Rojo        => Tema.Peligro;
        public static Color Naranja     => Tema.Advertencia;

        // ── Tipografía (reenvían a Tema) ─────────────────────────────────────
        public static Font  Body        => Tema.Body;
        public static Font  BodyBold    => Tema.BodyBold;
        public static Font  Pequeno     => Tema.Pequeno;
        public static Font  H1          => Tema.H1;
        public static Font  H2          => Tema.H2;
        public static Font  H3          => Tema.H3;
        public static Font  Mono        => Tema.Mono;
        public static Font  Boton       => Tema.FuenteBoton;

        // ── Métricas ─────────────────────────────────────────────────────────
        public static int   Padding     => Tema.Espaciado;
        public static int   AlturaInput => Tema.AlturaInput;
        public static int   AlturaBoton => Tema.AlturaBoton;
        public static int   Radio       => Tema.RadioChico;

        // ── Aplicación (delegada a Tema) ─────────────────────────────────────
        public static void Aplicar(Form form)                 => Tema.Aplicar(form);
        public static void EstilizarBoton(Button b)           => Tema.EstilizarBoton(b);
        public static void EstilizarInput(TextBox t)          => Tema.EstilizarInput(t);
        public static void EstilizarGrid(DataGridView dgv)    => Tema.EstilizarGrid(dgv);

        // ── Helpers de creación rápida (mantienen la API antigua) ────────────
        public static Button BotonPrimario(string texto)   => Tema.Boton(texto, Tema.TipoBoton.Primario);
        public static Button BotonSecundario(string texto) => Tema.Boton(texto, Tema.TipoBoton.Secundario);
        public static Button BotonPeligro(string texto)    => Tema.Boton(texto, Tema.TipoBoton.Peligro);
    }
}
