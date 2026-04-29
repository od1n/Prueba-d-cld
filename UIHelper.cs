// ════════════════════════════════════════════════════════════════════════
// ARCHIVO: UIHelper.cs  →  Services\
//
// Helper global para:
// 1. Tab con selección completa del contenido del campo
// 2. Máximo 2 decimales en todos los campos de moneda
//
// USO en cualquier Form o Dialog:
//   UIHelper.ConfigurarFormulario(this);
//
// Llámalo al final del constructor, después de agregar todos los controles.
// ════════════════════════════════════════════════════════════════════════

using System;
using System.Windows.Forms;

namespace SistemaGestion
{
    public static class UIHelper
    {
        // Columnas de dinero — siempre 2 decimales
        private static readonly string[] _camposMoneda = {
            "Credito","AFavor","Descuento","Precio","PrecioVenta",
            "CostoActual","CostoAnt","CostoPro","PrecioUltCompra",
            "PrecioPromedio","Total","TotalC","TotalF","IvaC","IvaF",
            "AbonoC","AbonoF","Monto","Salario","Sueldo","Tarifa",
            "TarifaBs","Bono","Deduccion","Cuota","Aporte",
            "MontoTotal","SaldoPendiente","CuotaSemanal"
        };

        /// <summary>
        /// Recorre todos los controles del formulario y configura:
        /// - TextBox.Enter → SelectAll()
        /// - NumericUpDown.Enter → Select(0, length)
        /// - NumericUpDown cuyo nombre contenga palabra de moneda → DecimalPlaces = 2
        /// </summary>
        public static void ConfigurarFormulario(Control root)
        {
            foreach (Control ctrl in ObtenerTodos(root))
            {
                if (ctrl is TextBox tb && !tb.ReadOnly)
                    tb.Enter += (s, e) => ((TextBox)s).SelectAll();

                if (ctrl is NumericUpDown nud)
                {
                    nud.Enter += (s, e) => {
                        var n = (NumericUpDown)s;
                        n.Select(0, n.Text.Length);
                    };
                    // Forzar 2 decimales si es campo de moneda
                    if (EsCampoMoneda(nud.Name))
                        nud.DecimalPlaces = 2;
                }
            }
        }

        /// <summary>
        /// Configura un NumericUpDown específico como campo de moneda:
        /// 2 decimales, separador de miles, máximo razonable.
        /// </summary>
        public static void ConfigurarMoneda(NumericUpDown nud,
            decimal maximo = 99_999_999m)
        {
            nud.DecimalPlaces       = 2;
            nud.ThousandsSeparator  = true;
            nud.Maximum             = maximo;
            nud.Minimum             = 0;
            nud.Enter += (s, e) => {
                var n = (NumericUpDown)s;
                n.Select(0, n.Text.Length);
            };
        }

        /// <summary>
        /// Configura múltiples NumericUpDown como campos de moneda.
        /// </summary>
        public static void ConfigurarMoneda(params NumericUpDown[] nuds)
        {
            foreach (var n in nuds) ConfigurarMoneda(n);
        }

        // ── Helpers privados ──────────────────────────────────────────────

        private static bool EsCampoMoneda(string nombre)
        {
            if (string.IsNullOrEmpty(nombre)) return false;
            string lower = nombre.ToLower();
            foreach (var campo in _camposMoneda)
                if (lower.Contains(campo.ToLower())) return true;
            return false;
        }

        private static System.Collections.Generic.IEnumerable<Control>
            ObtenerTodos(Control parent)
        {
            foreach (Control c in parent.Controls)
            {
                yield return c;
                foreach (var sub in ObtenerTodos(c))
                    yield return sub;
            }
        }
    }
}
