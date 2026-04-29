#nullable enable
// ════════════════════════════════════════════════════════════════════════
// ARCHIVO: ValidacionInline.cs  →  raíz del proyecto
//
// Helper para validar campos sin abrir MessageBox.Show. Usa ErrorProvider
// para mostrar el icono "!" junto al control que falla. Esto:
//   - No interrumpe el flujo del usuario (mucho más rápido).
//   - Permite ver TODOS los errores a la vez (no uno por diálogo).
//   - Es el patrón estándar en aplicaciones empresariales modernas.
//
// USO TÍPICO:
//   var v = new ValidacionInline(this);
//   v.Requerido(txtUsuario, "Ingresa el nombre de usuario.");
//   v.Requerido(txtPassword, "Ingresa la contraseña.");
//   v.MinLongitud(txtPassword, 8, "La contraseña debe tener al menos 8 caracteres.");
//   v.RegEx(txtCorreo, @"^[^@\s]+@[^@\s]+\.[^@\s]+$", "Correo inválido.");
//   if (!v.EsValido) return;   // los errores ya están visibles en pantalla
//
// ════════════════════════════════════════════════════════════════════════

using System;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace SistemaGestion
{
    public sealed class ValidacionInline : IDisposable
    {
        private readonly ErrorProvider _ep;
        private readonly Form _host;
        private bool _esValido = true;

        public ValidacionInline(Form host)
        {
            _host = host ?? throw new ArgumentNullException(nameof(host));
            _ep   = new ErrorProvider
            {
                ContainerControl = host,
                BlinkStyle       = ErrorBlinkStyle.NeverBlink,  // sin parpadeo molesto
                Icon             = SystemIcons.Warning
            };
        }

        public bool EsValido => _esValido;

        public void Limpiar()
        {
            _ep.Clear();
            _esValido = true;
        }

        public ValidacionInline Requerido(Control c, string mensaje = "Este campo es requerido.")
        {
            string val = (c?.Text ?? "").Trim();
            if (string.IsNullOrEmpty(val)) MarcarError(c, mensaje);
            else                            QuitarError(c);
            return this;
        }

        public ValidacionInline MinLongitud(Control c, int min, string? mensaje = null)
        {
            string val = c?.Text ?? "";
            if (val.Length > 0 && val.Length < min)
                MarcarError(c, mensaje ?? $"Debe tener al menos {min} caracteres.");
            return this;
        }

        public ValidacionInline MaxLongitud(Control c, int max, string? mensaje = null)
        {
            string val = c?.Text ?? "";
            if (val.Length > max)
                MarcarError(c, mensaje ?? $"No puede exceder {max} caracteres.");
            return this;
        }

        public ValidacionInline RegEx(Control c, string patron, string mensaje)
        {
            string val = c?.Text ?? "";
            if (val.Length > 0 && !Regex.IsMatch(val, patron))
                MarcarError(c, mensaje);
            return this;
        }

        public ValidacionInline EsNumerico(Control c, string mensaje = "Debe ser un número válido.")
        {
            string val = (c?.Text ?? "").Trim();
            if (val.Length > 0 && !decimal.TryParse(val, out _))
                MarcarError(c, mensaje);
            return this;
        }

        public ValidacionInline RangoNumerico(Control c, decimal min, decimal max, string? mensaje = null)
        {
            string val = (c?.Text ?? "").Trim();
            if (decimal.TryParse(val, out var d) && (d < min || d > max))
                MarcarError(c, mensaje ?? $"Debe estar entre {min} y {max}.");
            return this;
        }

        public ValidacionInline EsCorreo(Control c, string mensaje = "Correo electrónico inválido.")
        {
            return RegEx(c, @"^[^@\s]+@[^@\s]+\.[^@\s]+$", mensaje);
        }

        /// <summary>Validación libre con un predicado.</summary>
        public ValidacionInline Custom(Control c, bool condicion, string mensaje)
        {
            if (!condicion) MarcarError(c, mensaje);
            return this;
        }

        private void MarcarError(Control? c, string mensaje)
        {
            if (c is null) return;
            _ep.SetError(c, mensaje);
            _esValido = false;
            // Tinte sutil rojo en el borde del control para reforzar visualmente.
            try
            {
                if (c is TextBox tb) tb.BackColor = Color.FromArgb(255, 248, 248);
            }
            catch { }
        }

        private void QuitarError(Control? c)
        {
            if (c is null) return;
            _ep.SetError(c, "");
            try { if (c is TextBox tb) tb.BackColor = Color.White; } catch { }
        }

        public void Dispose() => _ep.Dispose();
    }
}
