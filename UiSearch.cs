using System.Windows.Forms;

namespace SistemaGestion
{
    /// <summary>
    /// Búsqueda tipo "type-ahead" para ComboBox: al escribir letra a letra, filtra y
    /// sugiere entre los ítems de la lista, para no tener que hacer scroll en listas largas
    /// (productos, clientes, moldes, insumos, etc.).
    /// </summary>
    public static class UiSearch
    {
        /// <summary>Convierte un ComboBox en buscable (autocompletado por lista).</summary>
        public static void Enable(ComboBox cb)
        {
            if (cb == null) return;
            // El autocompletado de WinForms solo funciona con estilo editable (DropDown),
            // no con DropDownList. Con SuggestAppend filtra la lista y completa el texto.
            cb.DropDownStyle      = ComboBoxStyle.DropDown;
            cb.AutoCompleteMode   = AutoCompleteMode.SuggestAppend;
            cb.AutoCompleteSource = AutoCompleteSource.ListItems;
        }

        /// <summary>Aplica Enable a todos los ComboBox dentro de un contenedor (recursivo).</summary>
        public static void EnableAll(Control root)
        {
            if (root == null) return;
            foreach (Control ctl in root.Controls)
            {
                if (ctl is ComboBox cb) Enable(cb);
                if (ctl.HasChildren) EnableAll(ctl);
            }
        }

        /// <summary>
        /// Devuelve el código que aparece antes del " — " en el texto del combo
        /// (funciona haya elegido de la lista o escrito con autocompletado).
        /// Devuelve null si está vacío o es un placeholder tipo "-- ... --".
        /// </summary>
        public static string CodigoDe(ComboBox cb)
        {
            var t = cb?.Text?.Trim();
            if (string.IsNullOrEmpty(t) || t.StartsWith("--")) return null;
            var code = t.Split('—')[0].Trim();
            return string.IsNullOrEmpty(code) ? null : code;
        }
    }
}
